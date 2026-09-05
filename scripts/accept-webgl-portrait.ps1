param(
  [string]$Url,
  [switch]$ServeLocal,
  [string]$BuildRoot = "$(Split-Path $PSScriptRoot)\Builds\WebGL-Acceptance",
  [string]$OutputRoot = "$(Split-Path $PSScriptRoot)\Logs\visual-acceptance",
  [string]$ChromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe',
  [ValidateRange(1, 16384)]
  [int]$Width = 402,
  [ValidateRange(1, 16384)]
  [int]$Height = 874,
  [ValidateRange(0, 16384)]
  [int]$SafeTop = 0,
  [ValidateRange(0, 16384)]
  [int]$SafeBottom = 0,
  [ValidateSet('orchard-01', 'orchard-02', 'orchard-03')]
  [string]$LevelId = 'orchard-01',
  [int]$TimeoutSeconds = 45,
  [switch]$Flow,
  [ValidateSet('victory', 'defeat')]
  [string]$SettlementOutcome = 'victory',
  [ValidateSet('victory', 'defeat')]
  [string]$BattleTerminalOutcome = 'victory',
  [switch]$ShellVisual,
  [switch]$HubVisual,
  [switch]$HubStates,
  [switch]$HubLoop,
  [switch]$InteractionPolishEvidence,
  [switch]$CompactControlEvidence,
  [switch]$CombatFeedbackEvidence,
  [switch]$DragFeedbackEvidence,
  [switch]$ShellError,
  [string]$ErrorLevelId = '__missing-ui-acceptance__',
  [ValidateRange(1, 20)]
  [int]$BootstrapCpuThrottlingRate = 8,
  [string]$ProfilePath,
  [switch]$CacheSeedOnly,
  [string]$CacheSeedManifestPath,
  [switch]$ReleaseDelivery,
  [string]$OutputDirectory,
  [switch]$SelfCheck
)

$ErrorActionPreference = 'Stop'
$profileProbeScript = Join-Path $PSScriptRoot 'webgl-build-profile-probe.ps1'
. $profileProbeScript
$referenceWidth = 402.0
$referenceHeight = 874.0
if ($SafeTop + $SafeBottom -ge $Height) {
  throw "Safe-area insets must leave positive content height: height=$Height top=$SafeTop bottom=$SafeBottom"
}
$safeContentHeight = $Height - $SafeTop - $SafeBottom
$referenceScale = [Math]::Min($Width / $referenceWidth, $safeContentHeight / $referenceHeight)
$referenceOffsetX = ($Width - $referenceWidth * $referenceScale) / 2.0
$referenceOffsetY = $SafeTop + ($safeContentHeight - $referenceHeight * $referenceScale) / 2.0
$shellContentWidth = [Math]::Min(370.0 * $referenceScale, $Width - 32.0 * $referenceScale)
$shellContentX = ($Width - $shellContentWidth) / 2.0
$shellContentY = $SafeTop + 18.0 * $referenceScale
$safeAreaEvidence = [ordered]@{
  top = $SafeTop
  bottom = $SafeBottom
  left = 0
  right = 0
  coordinateSpace = 'css-pixel/top-left'
  contentRect = [ordered]@{ x = 0; y = $SafeTop; width = $Width; height = $safeContentHeight }
  designReference = [ordered]@{ width = [int]$referenceWidth; height = [int]$referenceHeight }
  designScale = $referenceScale
  designOffset = [ordered]@{ x = $referenceOffsetX; y = $referenceOffsetY }
  shellContentRect = [ordered]@{
    x = $shellContentX
    y = $shellContentY
    width = $shellContentWidth
    height = [Math]::Max(0, $Height - $SafeBottom - $shellContentY - 18.0 * $referenceScale)
  }
}
$projectRoot = Split-Path $PSScriptRoot
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$outputDir = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
  Join-Path $OutputRoot $timestamp
} else {
  [IO.Path]::GetFullPath($OutputDirectory)
}
$ownsProfile = [string]::IsNullOrWhiteSpace($ProfilePath)
$profileDir = if ($ownsProfile) {
  Join-Path $env:TEMP "fruit-defense-cdp-$([Guid]::NewGuid().ToString('N'))"
} else {
  [IO.Path]::GetFullPath($ProfilePath)
}
$serverProcess = $null
$chromeProcess = $null
$socket = $null
$script:CdpId = 0
$verifiedBuildProfile = $null

if ($CacheSeedOnly -and $ownsProfile) {
  throw 'Cache seed mode requires -ProfilePath so the browser cache survives this run.'
}
if (-not [string]::IsNullOrWhiteSpace($CacheSeedManifestPath) -and $ownsProfile) {
  throw 'Cross-release acceptance requires -ProfilePath from the cache seed run.'
}
if ($CacheSeedOnly -and -not [string]::IsNullOrWhiteSpace($CacheSeedManifestPath)) {
  throw 'Cache seed mode cannot consume another cache seed manifest.'
}
$releaseDeliveryConflicts = @(
  @($Flow, $ShellVisual, $HubVisual, $HubStates, $HubLoop, $ShellError,
    $CombatFeedbackEvidence, $DragFeedbackEvidence, $InteractionPolishEvidence,
    $CompactControlEvidence) |
    Where-Object { $_ }
)
if ($ReleaseDelivery -and $releaseDeliveryConflicts.Count -gt 0) {
  throw '-ReleaseDelivery is a release startup/cache check and cannot use acceptance interaction modes.'
}
$selectedAcceptanceModes = @(
  @($Flow, $ShellVisual, $HubVisual, $HubStates, $HubLoop,
    $ShellError, $CombatFeedbackEvidence, $DragFeedbackEvidence) |
    Where-Object { $_ }
)
if ($selectedAcceptanceModes.Count -gt 1) {
  throw '-Flow, -ShellVisual, -HubVisual, -HubStates, -HubLoop, -ShellError, -CombatFeedbackEvidence, and -DragFeedbackEvidence are distinct acceptance modes and cannot be combined.'
}
if ($ShellVisual -and $LevelId -eq 'orchard-01') {
  throw '-ShellVisual requires -LevelId orchard-02 or orchard-03 for alternate-selection evidence.'
}
if ($HubLoop -and $LevelId -cne 'orchard-02') {
  throw '-HubLoop requires -LevelId orchard-02 for the applied/suppressed policy proof.'
}
if ($InteractionPolishEvidence -and $ShellError) {
  throw '-InteractionPolishEvidence is not available with -ShellError.'
}
if ($CompactControlEvidence -and
    ($Flow -or $ShellVisual -or $HubVisual -or $HubStates -or $HubLoop -or $ShellError)) {
  throw '-CompactControlEvidence is available only with the direct Battle acceptance mode.'
}
if ($CombatFeedbackEvidence -and ($InteractionPolishEvidence -or $CompactControlEvidence)) {
  throw '-CombatFeedbackEvidence owns its raw-frame interaction checkpoint and cannot be combined with other evidence switches.'
}
if ($DragFeedbackEvidence -and ($InteractionPolishEvidence -or $CompactControlEvidence)) {
  throw '-DragFeedbackEvidence owns its held-drag checkpoints and cannot be combined with other evidence switches.'
}
if ($ShellError -and [string]::IsNullOrWhiteSpace($ErrorLevelId)) {
  throw '-ShellError requires a non-empty -ErrorLevelId.'
}


$acceptanceRunnerCommandPath = $PSCommandPath
$acceptanceModuleRoot = Join-Path $PSScriptRoot 'webgl-acceptance'
$acceptanceModules = @(
  'geometry.ps1',
  'hub-matrix.ps1',
  'transport.ps1',
  'evidence-helpers.ps1',
  'image-analysis.ps1',
  'image-presentation-analysis.ps1',
  'settlement-ink-analysis.ps1',
  'settlement-optical-analysis.ps1',
  'self-check.ps1',
  'run-hub.ps1',
  'run-hub-loop.ps1',
  'run-shell.ps1',
  'run-flow.ps1',
  'run-combat.ps1',
  'run-direct.ps1',
  'run-cache.ps1')
foreach ($acceptanceModule in $acceptanceModules) {
  . (Join-Path $acceptanceModuleRoot $acceptanceModule)
}

$referenceControls = [ordered]@{
  lobbyLevelOrchard01 = [ordered]@{ x = 203; y = 188 }
  lobbyLevelOrchard02 = [ordered]@{ x = 202.5; y = 329 }
  lobbyLevelOrchard03 = [ordered]@{ x = 202.5; y = 466 }
  lobbyStart = [ordered]@{ x = 201.5; y = 728 }
  hubNavHome = [ordered]@{ x = 75; y = 834 }
  hubNavActivity = [ordered]@{ x = 201; y = 834 }
  hubNavGrowth = [ordered]@{ x = 327; y = 834 }
  hubGrowthEquipment = [ordered]@{ x = 110.5; y = 132 }
  hubGrowthCultivation = [ordered]@{ x = 291.5; y = 132 }
  hubGrowthEntry = [ordered]@{ x = 202.5; y = 232 }
  hubEquipmentPrimary = [ordered]@{ x = 201; y = 734.5 }
  hubCultivationPrimary = [ordered]@{ x = 200.5; y = 748.5 }
  hubActivityClaim = [ordered]@{ x = 201; y = 669.5 }
  hubStart = [ordered]@{ x = 201.5; y = 728 }
  settlementRetry = [ordered]@{ x = 201; y = 674 }
  settlementReturn = [ordered]@{ x = 201; y = 754 }
  headerPause = [ordered]@{ x = 288; y = 74 }
  headerSpeed = [ordered]@{ x = 346; y = 74 }
  waveAction = [ordered]@{ x = 291; y = 544 }
  pauseContinue = [ordered]@{ x = 125; y = 492 }
  pauseRestart = [ordered]@{ x = 277; y = 492 }
  terminalRestart = [ordered]@{ x = 201; y = 536 }
  weaponGatling = [ordered]@{ x = 71; y = 634 }
  nurserySlot0 = [ordered]@{ x = 61; y = 732 }
  acceptanceCell0 = [ordered]@{ x = 46; y = 249 }
  acceptanceCell1 = [ordered]@{ x = 90; y = 249 }
  detailClose = [ordered]@{ x = 352; y = 604 }
}
$controls = [ordered]@{}
foreach ($name in $referenceControls.Keys) {
  $isShellControl = $name.StartsWith('settlement')
  $controls[$name] = if ($isShellControl) {
    Convert-ShellReferencePoint -X $referenceControls[$name].x -Y $referenceControls[$name].y
  }
  else {
    Convert-ReferencePoint -X $referenceControls[$name].x -Y $referenceControls[$name].y
  }
}
$levelCardControlName = 'lobbyLevel' + ($LevelId -replace '-', '').Replace('orchard', 'Orchard')
if (-not $controls.Contains($levelCardControlName)) {
  throw "No Lobby control is defined for level '$LevelId'."
}
$lobbyStartRect = Convert-ReferenceRect -X 57 -Y 700 -Width 289 -Height 56
$lobbyAlternateCardRect = switch ($LevelId) {
  'orchard-01' { Convert-ReferenceRect -X 28 -Y 122 -Width 350 -Height 132 }
  'orchard-02' { Convert-ReferenceRect -X 27 -Y 267 -Width 351 -Height 124 }
  'orchard-03' { Convert-ReferenceRect -X 27 -Y 404 -Width 351 -Height 124 }
  default { throw "No Lobby card rect is defined for level '$LevelId'." }
}
$headerSampleRegion = Convert-ReferenceRect -X 20 -Y 42 -Width 354 -Height 105
$headerPanelRect = Convert-ReferenceRect -X 14 -Y 36 -Width 374 -Height 114
$pageShellRect = Convert-ReferenceRect -X 14 -Y 154 -Width 374 -Height 698
$gameplayStageRect = Convert-ReferenceRect -X 22 -Y 168 -Width 358 -Height 338
$phaseWaveRowRect = Convert-ReferenceRect -X 24 -Y 518 -Width 354 -Height 52
$phaseStatusOwner = Convert-ReferenceRect -X 24 -Y 518 -Width 168 -Height 52
$headerTitleOwner = Convert-ReferenceRect -X 40 -Y 52 -Width 210 -Height 38
$headerMetricRowOwner = Convert-ReferenceRect -X 28 -Y 101 -Width 346 -Height 40
$headerMetricRects = @(
  (Convert-ReferenceRect -X 28 -Y 101 -Width 112 -Height 40),
  (Convert-ReferenceRect -X 145 -Y 101 -Width 112 -Height 40),
  (Convert-ReferenceRect -X 262 -Y 101 -Width 112 -Height 40)
)
$toolTitleOwner = Convert-ReferenceRect -X 32 -Y 582 -Width 120 -Height 24
$nurseryTitleOwner = Convert-ReferenceRect -X 32 -Y 678 -Width 120 -Height 24
$detailTitleOwner = Convert-ReferenceRect -X 32 -Y 582 -Width 290 -Height 24
$detailBodyOwner = Convert-ReferenceRect -X 32 -Y 614 -Width 290 -Height 22
$boardRegion = $gameplayStageRect
$contextTrayRect = Convert-ReferenceRect -X 24 -Y 578 -Width 354 -Height 88
$nurseryTrayRect = Convert-ReferenceRect -X 24 -Y 674 -Width 354 -Height 92
$detailRegion = $contextTrayRect
$waveActionRect = Convert-ReferenceRect -X 204 -Y 518 -Width 174 -Height 52
$refreshActionRect = Convert-ReferenceRect -X 24 -Y 774 -Width 354 -Height 64
$refreshTextOwner = Convert-ReferenceRect -X 56 -Y 792 -Width 290 -Height 28
$pauseCompactControlRect = Convert-ReferenceRect -X 264 -Y 50 -Width 48 -Height 48
$speedCompactControlRect = Convert-ReferenceRect -X 318 -Y 50 -Width 56 -Height 48
$headerCompactControlRects = @($pauseCompactControlRect, $speedCompactControlRect)
$detailCloseCompactControlRect = Convert-ReferenceRect -X 330 -Y 582 -Width 44 -Height 44
$toolRecipeRects = @(
  (Convert-ReferenceRect -X 32 -Y 610 -Width 78.5 -Height 48),
  (Convert-ReferenceRect -X 118.5 -Y 610 -Width 78.5 -Height 48),
  (Convert-ReferenceRect -X 205 -Y 610 -Width 78.5 -Height 48),
  (Convert-ReferenceRect -X 291.5 -Y 610 -Width 78.5 -Height 48)
)
$nurserySlotRects = @(
  (Convert-ReferenceRect -X 32 -Y 706 -Width 58 -Height 52),
  (Convert-ReferenceRect -X 102 -Y 706 -Width 58 -Height 52),
  (Convert-ReferenceRect -X 172 -Y 706 -Width 58 -Height 52),
  (Convert-ReferenceRect -X 242 -Y 706 -Width 58 -Height 52),
  (Convert-ReferenceRect -X 312 -Y 706 -Width 58 -Height 52)
)
$pauseTitleRect = Convert-ReferenceRect -X 52 -Y 326 -Width 298 -Height 52
$pauseTitleInkRegion = Convert-ReferenceRect -X 90 -Y 332 -Width 220 -Height 40
$pauseHintRect = Convert-ReferenceRect -X 60 -Y 390 -Width 282 -Height 52
$pauseHintIconRegion = Convert-ReferenceRect -X 102 -Y 398 -Width 26 -Height 36
$pauseHintCopyRegion = Convert-ReferenceRect -X 130 -Y 398 -Width 176 -Height 36
$pauseContinueRect = Convert-ReferenceRect -X 54 -Y 466 -Width 142 -Height 52
$pauseRestartRect = Convert-ReferenceRect -X 206 -Y 466 -Width 142 -Height 52
$pauseActionBandRect = Convert-ReferenceRect -X 36 -Y 454 -Width 330 -Height 70
$settlementResultBannerRect = Convert-ShellReferenceRect `
  -X 36 -Y 150 -Width 330 -Height 48
$settlementOutcomeInkRegion = Convert-ShellReferenceRect `
  -X 140 -Y 146 -Width 122 -Height 58
$settlementMetricRects = @(
  (Convert-ShellReferenceRect -X 32 -Y 450 -Width 338 -Height 48),
  (Convert-ShellReferenceRect -X 32 -Y 506 -Width 338 -Height 48),
  (Convert-ShellReferenceRect -X 32 -Y 562 -Width 338 -Height 48)
)
$hudDarkPixelThreshold = [Math]::Max(1, [Math]::Floor(80 * $referenceScale * $referenceScale))
$hudLightPixelThreshold = [Math]::Max(1, [Math]::Floor(5000 * $referenceScale * $referenceScale))
# At a scaled safe-area canvas, the two-pixel held offset can land entirely
# inside anti-aliased button edges. Material pixel change plus changed-bounds
# containment remains authoritative; the retreat counter is retained at 1:1.
$pausePressRetreatedPixelThreshold = if ($referenceScale -ge .99) { 20 } else { 0 }
$nearBlackLumaThreshold = 0.08
$maxBlackFraction = 0.01
$maxNearBlackFraction = 0.05
$framePixelThresholds = [ordered]@{
  sampleStepPixels = 4
  nearBlackLuma = $nearBlackLumaThreshold
  maxBlackFraction = $maxBlackFraction
  maxNearBlackFraction = $maxNearBlackFraction
  maxInvalidFraction = 0.05
}


$warmAssetTransferLimitBytes = 16KB
$warmTotalTransferLimitBytes = 64KB
$expectedLevelIdentities = [ordered]@{
  'orchard-01' = [ordered]@{
    levelId = 'orchard-01'; mapId = 'orchard-01'
    waveSetId = 'waves.orchard-01.teaching'; ruleSetId = 'rules.orchard-01.baseline'
    themeId = 'theme.orchard-01.day'
  }
  'orchard-02' = [ordered]@{
    levelId = 'orchard-02'; mapId = 'orchard-02'
    waveSetId = 'waves.orchard-02.coverage'; ruleSetId = 'rules.orchard-02.coverage'
    themeId = 'theme.orchard-02.creek'
  }
  'orchard-03' = [ordered]@{
    levelId = 'orchard-03'; mapId = 'orchard-03'
    waveSetId = 'waves.orchard-03.pressure'; ruleSetId = 'rules.orchard-03.pressure'
    themeId = 'theme.orchard-03.dusk'
  }
}
$expectedLevelIdentity = $expectedLevelIdentities[$LevelId]


$mappedDesignBounds = [ordered]@{
  xMin = $referenceOffsetX
  yMin = $referenceOffsetY
  xMax = $referenceOffsetX + $referenceWidth * $referenceScale
  yMax = $referenceOffsetY + $referenceHeight * $referenceScale
}
if ($mappedDesignBounds.xMin -lt -0.001 -or $mappedDesignBounds.yMin -lt ($SafeTop - 0.001) -or
    $mappedDesignBounds.xMax -gt ($Width + 0.001) -or
    $mappedDesignBounds.yMax -gt ($Height - $SafeBottom + 0.001)) {
  throw "Mapped 402x874 design viewport escapes safe content: $($mappedDesignBounds | ConvertTo-Json -Compress)"
}

$runtimeUiIdentity = Get-ReleaseRuntimeUiIdentity
$expectedBuildProfile = if ($ReleaseDelivery) { 'release' } else { 'acceptance' }


if ($SelfCheck) {
  Invoke-AcceptanceSelfCheck
  return
}

try {
  New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

  if ($ServeLocal) {
    if (-not [string]::IsNullOrWhiteSpace($Url)) {
      throw '-ServeLocal cannot be combined with -Url; choose one exact acceptance source.'
    }
    $verifiedBuildProfile = Assert-WebGlBuildProfile `
      -ExpectedProfile $expectedBuildProfile `
      -BuildRoot $BuildRoot `
      -TimeoutSeconds $TimeoutSeconds
    $serverPort = Get-FreeTcpPort
    $oldStaticRoot = $env:STATIC_ROOT
    $oldPort = $env:PORT
    try {
      $env:STATIC_ROOT = (Resolve-Path $BuildRoot).Path
      $env:PORT = [string]$serverPort
      $serverStart = @{
        FilePath = 'node'
        ArgumentList = 'deploy/server.mjs'
        WorkingDirectory = $projectRoot
        WindowStyle = 'Hidden'
        PassThru = $true
        RedirectStandardOutput = (Join-Path $outputDir 'server.stdout.log')
        RedirectStandardError = (Join-Path $outputDir 'server.stderr.log')
      }
      $serverProcess = Start-Process @serverStart
    }
    finally {
      $env:STATIC_ROOT = $oldStaticRoot
      $env:PORT = $oldPort
    }
    $Url = "http://127.0.0.1:$serverPort/"
  }
  elseif ([string]::IsNullOrWhiteSpace($Url)) {
    throw 'Provide -Url or use -ServeLocal.'
  }
  if (-not $ReleaseDelivery) {
    $Url = Set-AcceptanceQuery -TargetUrl $Url
  }
  if (-not $ServeLocal) {
    $verifiedBuildProfile = Assert-WebGlBuildProfile `
      -ExpectedProfile $expectedBuildProfile `
      -Url $Url `
      -TimeoutSeconds $TimeoutSeconds
  }
  if (-not (Test-Path -LiteralPath $ChromePath)) { throw "Chrome not found: $ChromePath" }

  $pageResponse = Wait-Http -TargetUrl $Url -Seconds $TimeoutSeconds
  $delivery = Get-UnityDeliveryMetadata -PageUrl $Url -PageResponse $pageResponse

  if ($ReleaseDelivery) {
    if ([string]$verifiedBuildProfile.verifiedProfile -cne 'release') {
      throw 'Release delivery mode requires a verified release build profile.'
    }
  } else {
    Assert-AcceptanceBuildProfileVerified
  }
  $debugPort = Get-FreeTcpPort
  $initialChromeUrl = if ($ShellVisual -or $ShellError -or $ReleaseDelivery) {
    'about:blank'
  } else {
    $Url
  }
  $chromeArgs = @(
    '--headless=new', '--no-first-run', '--disable-background-networking', '--disable-extensions',
    '--hide-scrollbars', '--use-angle=swiftshader', '--enable-webgl', '--ignore-gpu-blocklist',
    '--user-agent="Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1"',
    "--window-size=$Width,$Height", '--force-device-scale-factor=1',
    "--remote-debugging-port=$debugPort", "--user-data-dir=$profileDir", $initialChromeUrl
  )
  $coldStartedAt = [DateTimeOffset]::UtcNow
  $chromeProcess = Start-Process -FilePath $ChromePath -ArgumentList $chromeArgs -WindowStyle Hidden -PassThru

  $debugUrl = "http://127.0.0.1:$debugPort/json"
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    try {
      $rawTargets = Invoke-RestMethod -Uri $debugUrl -TimeoutSec 3
      $target = $null
      foreach ($candidate in $rawTargets) {
        $typeProperty = $candidate.PSObject.Properties['type']
        $urlProperty = $candidate.PSObject.Properties['url']
        if ($null -ne $typeProperty -and $null -ne $urlProperty -and
            [string]$typeProperty.Value -eq 'page' -and [string]$urlProperty.Value -eq $initialChromeUrl) {
          $target = $candidate
          break
        }
      }
      if ($target) { break }
    }
    catch { Start-Sleep -Milliseconds 300 }
  } while ((Get-Date) -lt $deadline)
  if (-not $target) { throw 'Chrome DevTools page target was not created.' }

  $socket = [Net.WebSockets.ClientWebSocket]::new()
  $connectCts = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(10))
  $debuggerProperty = $target.PSObject.Properties['webSocketDebuggerUrl']
  if ($null -eq $debuggerProperty -or
      [string]::IsNullOrWhiteSpace([string]$debuggerProperty.Value)) {
    throw 'Chrome DevTools page target has no websocket debugger URL.'
  }
  $debuggerUrl = [string]$debuggerProperty.Value
  try { $socket.ConnectAsync([Uri]$debuggerUrl, $connectCts.Token).GetAwaiter().GetResult() | Out-Null }
  finally { $connectCts.Dispose() }
  Invoke-Cdp -Method 'Page.enable' | Out-Null
  Invoke-Cdp -Method 'Runtime.enable' | Out-Null
  if ($ReleaseDelivery) {
    Invoke-Cdp -Method 'Page.addScriptToEvaluateOnNewDocument' -Params @{
      source = 'performance.setResourceTimingBufferSize(1000);'
    } | Out-Null
  }
  $browserVersion = Invoke-Cdp -Method 'Browser.getVersion'
  $browserEvidence = [ordered]@{
    product = $browserVersion.product
    userAgent = $browserVersion.userAgent
    protocolVersion = $browserVersion.protocolVersion
    jsVersion = $browserVersion.jsVersion
    executable = (Resolve-Path -LiteralPath $ChromePath).Path
    launchMode = 'external-headless-chrome-cdp'
  }
  Invoke-Cdp -Method 'Emulation.setDeviceMetricsOverride' -Params @{
    width = $Width
    height = $Height
    deviceScaleFactor = 1
    mobile = $false
    screenWidth = $Width
    screenHeight = $Height
  } | Out-Null

  $readinessExpression = @'
(() => {
  const canvas = document.querySelector('#unity-canvas');
  const canvasRect = canvas ? canvas.getBoundingClientRect() : null;
  const loading = document.querySelector('#unity-loading-bar');
  const warning = document.querySelector('#unity-warning');
  return JSON.stringify({
    href: location.href,
    title: document.title,
    canvas: !!canvas,
    width: canvas ? canvas.width : 0,
    height: canvas ? canvas.height : 0,
    cssWidth: canvasRect ? canvasRect.width : 0,
    cssHeight: canvasRect ? canvasRect.height : 0,
    innerWidth: window.innerWidth,
    innerHeight: window.innerHeight,
    devicePixelRatio: window.devicePixelRatio,
    timeOrigin: performance.timeOrigin,
    loading: loading ? getComputedStyle(loading).display : 'missing',
    warning: warning ? warning.textContent.trim() : '',
    acceptanceReady: !!window.fruitDefenseUnityInstance,
    appRoute: window.fruitDefenseAppRoute ?? -1,
    body: document.body ? document.body.innerText.slice(0, 240) : ''
  });
})()
'@
  $bootstrapCapture = [ordered]@{
    state = if ($ShellVisual) { 'not-captured' } else { 'not-requested' }
    reason = if ($ShellVisual) { 'Unity route became ready before a stable initializing frame was observed.' } else { '' }
    cpuThrottlingRate = if ($ShellVisual) { $BootstrapCpuThrottlingRate } else { 1 }
    attempts = 0
  }
  if ($ShellVisual) {
    $bootstrapProbePath = Join-Path $outputDir '00-bootstrap-probe.png'
    $bootstrapFinalPath = Join-Path $outputDir '00-bootstrap-initializing.png'
    foreach ($staleBootstrapPath in @($bootstrapProbePath, $bootstrapFinalPath)) {
      if (Test-Path -LiteralPath $staleBootstrapPath -PathType Leaf) {
        Remove-Item -LiteralPath $staleBootstrapPath -Force
      }
    }
    Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{
      rate = $BootstrapCpuThrottlingRate
    } | Out-Null
    $coldStartedAt = [DateTimeOffset]::UtcNow
    Invoke-Cdp -Method 'Page.navigate' -Params @{ url = $Url } | Out-Null

    $bootstrapDeadline = (Get-Date).AddSeconds([Math]::Min($TimeoutSeconds, 25))
    do {
      try {
        $bootstrapReadiness = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
        $bootstrapViewportReady = $bootstrapReadiness.innerWidth -eq $Width -and
          $bootstrapReadiness.innerHeight -eq $Height
        $bootstrapCanvasReady = $bootstrapReadiness.width -eq $Width -and
          $bootstrapReadiness.height -eq $Height -and
          [Math]::Abs([double]$bootstrapReadiness.cssWidth - $Width) -lt 0.51 -and
          [Math]::Abs([double]$bootstrapReadiness.cssHeight - $Height) -lt 0.51
        if ($bootstrapReadiness.canvas -and $bootstrapReadiness.loading -eq 'none' -and
            -not $bootstrapReadiness.acceptanceReady -and $bootstrapViewportReady -and
            $bootstrapCanvasReady) {
          $bootstrapCapture.attempts++
          $bootstrapPath = Save-Screenshot -Name '00-bootstrap-probe'
          $bootstrapMetrics = Get-ImageMetrics -Path $bootstrapPath
          # Reject Unity's dark splash frame; the Bootstrap presentation uses the
          # release Theme's light cream surfaces and is the application-owned evidence.
          if ((Test-StableFrameMetrics -Metrics $bootstrapMetrics) -and
              $bootstrapMetrics.averageLuma -gt 0.45) {
            $bootstrapCapture.state = 'captured'
            $bootstrapCapture.reason = ''
            Move-Item -LiteralPath $bootstrapPath -Destination $bootstrapFinalPath -Force
            $bootstrapCapture.screenshot = $bootstrapFinalPath
            $bootstrapCapture.imageMetrics = $bootstrapMetrics
            $bootstrapCapture.canvas = $bootstrapReadiness
            break
          }
        }
        if ($bootstrapReadiness.acceptanceReady) { break }
      }
      catch {
        # Navigation can temporarily destroy the Runtime execution context.
      }
      Start-Sleep -Milliseconds 35
    } while ((Get-Date) -lt $bootstrapDeadline)
    Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{ rate = 1 } | Out-Null
    if ($bootstrapCapture.state -ne 'captured' -and
        (Test-Path -LiteralPath $bootstrapProbePath -PathType Leaf)) {
      Remove-Item -LiteralPath $bootstrapProbePath -Force
    }
  }
  elseif ($ShellError -or $ReleaseDelivery) {
    $coldStartedAt = [DateTimeOffset]::UtcNow
    Invoke-Cdp -Method 'Page.navigate' -Params @{ url = $Url } | Out-Null
  }


  if ($ShellError) {
    Invoke-ShellErrorMode
    return
  }

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  do {
    $readiness = (Invoke-JavaScript -Expression $readinessExpression) | ConvertFrom-Json
    $viewportReady = $readiness.innerWidth -eq $Width -and $readiness.innerHeight -eq $Height
    $canvasReady = $readiness.width -eq $Width -and $readiness.height -eq $Height -and
      [Math]::Abs([double]$readiness.cssWidth - $Width) -lt 0.51 -and
      [Math]::Abs([double]$readiness.cssHeight - $Height) -lt 0.51
    $runtimeReady = $ReleaseDelivery -or $readiness.acceptanceReady
    if ($readiness.canvas -and $readiness.loading -eq 'none' -and $runtimeReady -and
        $viewportReady -and $canvasReady) { break }
    Start-Sleep -Milliseconds 400
  } while ((Get-Date) -lt $deadline)
  if (-not $readiness.canvas -or $readiness.loading -ne 'none' -or
      (-not $ReleaseDelivery -and -not $readiness.acceptanceReady)) {
    throw "Unity player did not finish loading. state=$($readiness | ConvertTo-Json -Compress)"
  }
  if ($readiness.warning) { throw "Unity player warning: $($readiness.warning)" }
  if (-not $viewportReady -or -not $canvasReady) {
    throw "Chrome viewport or Unity canvas did not resolve to ${Width}x${Height}. state=$($readiness | ConvertTo-Json -Compress)"
  }

  $coldCacheRun = Get-UnityResourceTiming `
    -Label 'cold' `
    -DeliveryAssets $delivery.assets `
    -StartedAt $coldStartedAt

  if ($CacheSeedOnly) {
    Invoke-AcceptanceCacheSeedMode
    return
  }

  $cacheContext = Complete-AcceptanceWarmCache
  $readiness = $cacheContext.readiness
  $delivery = $cacheContext.delivery
  $releaseTransition = $cacheContext.releaseTransition
  $screenshots = [ordered]@{}

  if ($ReleaseDelivery) {
    Invoke-ReleaseDeliveryMode
    return
  }

  if ($ShellVisual) {
    Invoke-ShellVisualMode
    return
  }
  if ($HubVisual) {
    Invoke-HubVisualMode
    return
  }
  if ($HubStates) {
    Invoke-HubStatesMode
    return
  }
  if ($HubLoop) {
    Invoke-HubLoopMode
    return
  }
  if ($Flow) {
    Invoke-FlowMode
    return
  }

  Wait-AppRoute -Route 1
  $directBattleIdentity = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'direct-battle' -SessionMode Required

  if ($CombatFeedbackEvidence) {
    Invoke-CombatFeedbackMode
    return
  }
  Invoke-DirectBattleMode
}

finally {
  Stop-OwnedChrome
  if ($socket) { $socket.Dispose() }
  if ($serverProcess -and -not $serverProcess.HasExited) {
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
  }
  $tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
  $resolvedProfile = [IO.Path]::GetFullPath($profileDir)
  if ($ownsProfile -and $resolvedProfile.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
  }
}
