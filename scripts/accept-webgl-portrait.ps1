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
  [switch]$InteractionPolishEvidence,
  [switch]$CompactControlEvidence,
  [switch]$CombatFeedbackEvidence,
  [switch]$ShellError,
  [string]$ErrorLevelId = '__missing-ui-acceptance__',
  [ValidateRange(1, 20)]
  [int]$BootstrapCpuThrottlingRate = 8,
  [string]$ProfilePath,
  [switch]$CacheSeedOnly,
  [string]$CacheSeedManifestPath,
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
if ((@($Flow, $ShellVisual, $ShellError, $CombatFeedbackEvidence) | Where-Object { $_ }).Count -gt 1) {
  throw '-Flow, -ShellVisual, -ShellError, and -CombatFeedbackEvidence are distinct acceptance modes and cannot be combined.'
}
if ($ShellVisual -and $LevelId -eq 'orchard-01') {
  throw '-ShellVisual requires -LevelId orchard-02 or orchard-03 for alternate-selection evidence.'
}
if ($InteractionPolishEvidence -and $ShellError) {
  throw '-InteractionPolishEvidence is not available with -ShellError.'
}
if ($CompactControlEvidence -and ($Flow -or $ShellVisual -or $ShellError)) {
  throw '-CompactControlEvidence is available only with the direct Battle acceptance mode.'
}
if ($CombatFeedbackEvidence -and ($InteractionPolishEvidence -or $CompactControlEvidence)) {
  throw '-CombatFeedbackEvidence owns its raw-frame interaction checkpoint and cannot be combined with other evidence switches.'
}
if ($ShellError -and [string]::IsNullOrWhiteSpace($ErrorLevelId)) {
  throw '-ShellError requires a non-empty -ErrorLevelId.'
}


$acceptanceRunnerCommandPath = $PSCommandPath
$acceptanceModuleRoot = Join-Path $PSScriptRoot 'webgl-acceptance'
$acceptanceModules = @(
  'geometry.ps1',
  'transport.ps1',
  'evidence-helpers.ps1',
  'image-analysis.ps1',
  'settlement-ink-analysis.ps1',
  'settlement-optical-analysis.ps1',
  'self-check.ps1',
  'run-shell.ps1',
  'run-flow.ps1',
  'run-combat.ps1',
  'run-direct.ps1',
  'run-cache.ps1')
foreach ($acceptanceModule in $acceptanceModules) {
  . (Join-Path $acceptanceModuleRoot $acceptanceModule)
}

$referenceControls = [ordered]@{
  lobbyLevelOrchard01 = [ordered]@{ x = 201; y = 206 }
  lobbyLevelOrchard02 = [ordered]@{ x = 201; y = 392 }
  lobbyLevelOrchard03 = [ordered]@{ x = 201; y = 578 }
  lobbyStart = [ordered]@{ x = 201; y = 746 }
  settlementRetry = [ordered]@{ x = 201; y = 674 }
  settlementReturn = [ordered]@{ x = 201; y = 754 }
  headerPause = [ordered]@{ x = 300; y = 38 }
  headerSpeed = [ordered]@{ x = 360; y = 38 }
  waveAction = [ordered]@{ x = 302; y = 570 }
  pauseContinue = [ordered]@{ x = 125; y = 492 }
  pauseRestart = [ordered]@{ x = 277; y = 492 }
  terminalRestart = [ordered]@{ x = 201; y = 536 }
  weaponGatling = [ordered]@{ x = 60; y = 654 }
  nurserySlot0 = [ordered]@{ x = 51; y = 745 }
  acceptanceCell0 = [ordered]@{ x = 32; y = 223 }
  acceptanceCell1 = [ordered]@{ x = 80; y = 223 }
  detailClose = [ordered]@{ x = 368; y = 628 }
}
$controls = [ordered]@{}
foreach ($name in $referenceControls.Keys) {
  $isShellControl = $name.StartsWith('lobby') -or $name.StartsWith('settlement')
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
$lobbyStartRect = [ordered]@{
  xMin = [Math]::Floor($shellContentX)
  yMin = [Math]::Floor($shellContentY + 690.0 * $referenceScale)
  xMax = [Math]::Ceiling($shellContentX + $shellContentWidth)
  yMax = [Math]::Ceiling($shellContentY + (690.0 + 76.0) * $referenceScale)
}
$lobbyLevelCardOffset = switch ($LevelId) {
  'orchard-01' { 100.0 }
  'orchard-02' { 286.0 }
  'orchard-03' { 472.0 }
  default { throw "No Lobby card rect is defined for level '$LevelId'." }
}
$lobbyAlternateCardRect = [ordered]@{
  # The approved selectable-card Sprite preserves transparent optical padding:
  # visible left/right outline ink begins 6/4 logical points inside the layout rect.
  xMin = [Math]::Floor($shellContentX + 6.0 * $referenceScale)
  yMin = [Math]::Floor($shellContentY + $lobbyLevelCardOffset * $referenceScale)
  xMax = [Math]::Ceiling(
    $shellContentX + $shellContentWidth - 4.0 * $referenceScale)
  yMax = [Math]::Ceiling(
    $shellContentY + ($lobbyLevelCardOffset + 176.0) * $referenceScale)
}
$headerSampleRegion = Convert-ReferenceRect -X 13 -Y 11 -Width 250 -Height 53
$formerActionRegion = Convert-ReferenceRect -X 8 -Y 842 -Width 386 -Height 24
$headerPanelRect = Convert-ReferenceRect -X 0 -Y 8 -Width 402 -Height 96
$gameplayStageRect = Convert-ReferenceRect -X 0 -Y 108 -Width 402 -Height 486
$headerTitleOwner = Convert-ReferenceRect -X 16 -Y 26 -Width 246 -Height 24
$headerMetricRowOwner = Convert-ReferenceRect -X 16 -Y 68 -Width 370 -Height 32
$toolTitleOwner = Convert-ReferenceRect -X 16 -Y 606 -Width 180 -Height 22
$nurseryTitleOwner = Convert-ReferenceRect -X 16 -Y 692 -Width 180 -Height 22
$detailTitleOwner = Convert-ReferenceRect -X 16 -Y 606 -Width 322 -Height 24
$detailBodyOwner = Convert-ReferenceRect -X 16 -Y 638 -Width 322 -Height 22
$boardRegion = $gameplayStageRect
$contextTrayRect = Convert-ReferenceRect -X 8 -Y 602 -Width 386 -Height 78
$nurseryTrayRect = Convert-ReferenceRect -X 8 -Y 688 -Width 386 -Height 88
$detailRegion = $contextTrayRect
$waveActionRect = Convert-ReferenceRect -X 210 -Y 548 -Width 184 -Height 44
$refreshActionRect = Convert-ReferenceRect -X 8 -Y 784 -Width 386 -Height 52
$refreshTextOwner = Convert-ReferenceRect -X 40 -Y 796 -Width 330 -Height 28
$pauseCompactControlRect = Convert-ReferenceRect -X 274 -Y 12 -Width 52 -Height 52
$speedCompactControlRect = Convert-ReferenceRect -X 334 -Y 12 -Width 52 -Height 52
$detailCloseCompactControlRect = Convert-ReferenceRect -X 346 -Y 606 -Width 44 -Height 44
$pauseTitleRect = Convert-ReferenceRect -X 52 -Y 326 -Width 298 -Height 52
$pauseTitleInkRegion = Convert-ReferenceRect -X 90 -Y 332 -Width 220 -Height 40
$pauseHintRect = Convert-ReferenceRect -X 60 -Y 390 -Width 282 -Height 52
$pauseHintIconRegion = Convert-ReferenceRect -X 102 -Y 398 -Width 26 -Height 36
$pauseHintCopyRegion = Convert-ReferenceRect -X 130 -Y 398 -Width 176 -Height 36
$pauseContinueRect = Convert-ReferenceRect -X 54 -Y 466 -Width 142 -Height 52
$pauseRestartRect = Convert-ReferenceRect -X 206 -Y 466 -Width 142 -Height 52
$pauseActionBandRect = Convert-ReferenceRect -X 36 -Y 454 -Width 330 -Height 70
$settlementResultBannerRect = Convert-ShellReferenceRect `
  -X 36 -Y 152 -Width 330 -Height 44
$settlementOutcomeInkRegion = Convert-ShellReferenceRect `
  -X 140 -Y 158 -Width 122 -Height 34
$settlementMetricRects = @(
  (Convert-ShellReferenceRect -X 32 -Y 450 -Width 338 -Height 48),
  (Convert-ShellReferenceRect -X 32 -Y 506 -Width 338 -Height 48),
  (Convert-ShellReferenceRect -X 32 -Y 562 -Width 338 -Height 48)
)
$hudDarkPixelThreshold = [Math]::Max(1, [Math]::Floor(80 * $referenceScale * $referenceScale))
$hudLightPixelThreshold = [Math]::Max(1, [Math]::Floor(5000 * $referenceScale * $referenceScale))
$formerActionPixelThreshold = [Math]::Max(12, [Math]::Ceiling(12 * $referenceScale * $referenceScale))
$formerActionSpanThreshold = [Math]::Max(24,
  [Math]::Ceiling(($formerActionRegion.xMax - $formerActionRegion.xMin) * 0.20))
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
      -ExpectedProfile acceptance `
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
  $Url = Set-AcceptanceQuery -TargetUrl $Url
  if (-not $ServeLocal) {
    $verifiedBuildProfile = Assert-WebGlBuildProfile `
      -ExpectedProfile acceptance `
      -Url $Url `
      -TimeoutSeconds $TimeoutSeconds
  }
  if (-not (Test-Path -LiteralPath $ChromePath)) { throw "Chrome not found: $ChromePath" }

  $pageResponse = Wait-Http -TargetUrl $Url -Seconds $TimeoutSeconds
  $delivery = Get-UnityDeliveryMetadata -PageUrl $Url -PageResponse $pageResponse

  Assert-AcceptanceBuildProfileVerified
  $debugPort = Get-FreeTcpPort
  $initialChromeUrl = if ($ShellVisual -or $ShellError) { 'about:blank' } else { $Url }
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
  elseif ($ShellError) {
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
    if ($readiness.canvas -and $readiness.loading -eq 'none' -and $readiness.acceptanceReady -and
        $viewportReady -and $canvasReady) { break }
    Start-Sleep -Milliseconds 400
  } while ((Get-Date) -lt $deadline)
  if (-not $readiness.canvas -or $readiness.loading -ne 'none' -or -not $readiness.acceptanceReady) {
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

  if ($ShellVisual) {
    Invoke-ShellVisualMode
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
