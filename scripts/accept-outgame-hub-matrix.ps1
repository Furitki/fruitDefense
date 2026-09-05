param(
  [string]$BuildRoot = "$(Split-Path $PSScriptRoot)\Builds\WebGL-Acceptance",
  [string]$OutputRoot = "$(Split-Path $PSScriptRoot)\Logs\outgame-hub-acceptance",
  [string]$ChromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe',
  [ValidateRange(5, 180)]
  [int]$TimeoutSeconds = 60,
  [switch]$SelfCheck
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$portraitRunner = Join-Path $PSScriptRoot 'accept-webgl-portrait.ps1'
$hostRunner = Join-Path $PSScriptRoot 'accept-webgl-host.ps1'
$hubMatrixModule = Join-Path $PSScriptRoot 'webgl-acceptance\hub-matrix.ps1'
. $hubMatrixModule
$hubRunModule = Join-Path $PSScriptRoot 'webgl-acceptance\run-hub.ps1'
. $hubRunModule

function Assert-HubRunnerSyntax {
  $paths = @(
    $PSCommandPath,
    $portraitRunner,
    $hostRunner,
    (Join-Path $PSScriptRoot 'webgl-acceptance\transport.ps1'),
    (Join-Path $PSScriptRoot 'webgl-acceptance\run-hub.ps1'),
    (Join-Path $PSScriptRoot 'webgl-acceptance\run-hub-loop.ps1'),
    $hubMatrixModule
  )
  foreach ($path in $paths) {
    $tokens = $null
    $errors = $null
    [Management.Automation.Language.Parser]::ParseFile(
      $path, [ref]$tokens, [ref]$errors) | Out-Null
    if ($errors.Count -gt 0) {
      throw "PowerShell syntax errors in ${path}: $($errors.Message -join '; ')"
    }
  }
  $portraitSource = Get-Content -Raw -LiteralPath $portraitRunner
  foreach ($token in @(
      '[switch]$HubVisual', '[switch]$HubStates', '[switch]$HubLoop',
      "'run-hub-loop.ps1'", 'Invoke-HubStatesMode', 'Invoke-HubLoopMode')) {
    if ($portraitSource.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
      throw "Portrait runner is missing Hub mode token: $token"
    }
  }
  $transportSource = Get-Content -Raw -LiteralPath (
    Join-Path $PSScriptRoot 'webgl-acceptance\transport.ps1')
  foreach ($token in @(
      "SendMessage('AppBootstrap', 'ConfigureAcceptanceHubState'",
      'window.fruitDefenseHubTelemetry', 'FixtureMode')) {
    if ($transportSource.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
      throw "Hub transport is missing token: $token"
    }
  }
  $loopSource = Get-Content -Raw -LiteralPath (
    Join-Path $PSScriptRoot 'webgl-acceptance\run-hub-loop.ps1')
  foreach ($token in @(
      'fixtureDataAbsent', 'committedRewardRevisionCount',
      'committedGrowthRevisionCount', 'launchGrowthFingerprint',
      'settlementReturnPreservesGrowth', 'retryReusesGrowthSnapshot')) {
    if ($loopSource.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
      throw "Hub loop runner is missing acceptance token: $token"
    }
  }
}

function Read-AcceptedEvidence {
  param([string]$Path, [string]$ExpectedType)
  if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Expected Hub evidence is missing: $Path"
  }
  $evidence = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
  if (-not [bool]$evidence.accepted -or
      [string]$evidence.evidenceType -cne $ExpectedType) {
    throw "Hub evidence is not accepted as '$ExpectedType': $Path"
  }
  return $evidence
}

function New-EvidenceReference {
  param([string]$Path, [object]$Evidence)
  return [ordered]@{
    path = (Resolve-Path -LiteralPath $Path).Path
    sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    evidenceType = [string]$Evidence.evidenceType
    viewport = $Evidence.viewport
    safeArea = $Evidence.safeArea
  }
}

Assert-HubRunnerSyntax
$matrix = @(Assert-OutgameHubPortraitMatrix)
$namedStates = @(Get-HubNamedStateCatalog)
if ($namedStates.Count -ne 22 -or
    @($namedStates.id | Sort-Object -Unique).Count -ne 22) {
  throw 'Hub named-state catalog self-check requires 22 unique fixture states.'
}
if ($SelfCheck) {
  [ordered]@{
    schemaVersion = 1
    accepted = $true
    syntax = 'pass'
    portraitMatrix = $matrix
    staticStateCount = $namedStates.Count
    staticStates = $namedStates
    loopStages = @(
      'fresh', 'claim', 'equip', 'cultivation', 'policy-preview',
      'battle', 'settlement-return', 'second-battle', 'settlement-retry')
    desktopHost = 'required'
  } | ConvertTo-Json -Depth 8
  Write-Host 'FRUIT_DEFENSE_OUTGAME_HUB_MATRIX_SELF_CHECK_OK'
  return
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runRoot = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) $timestamp
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$references = [ordered]@{
  portraitPages = [ordered]@{}
  namedStates = [ordered]@{}
  loops = [ordered]@{}
}

foreach ($case in $matrix) {
  $caseDirectory = Join-Path $runRoot "portrait-pages\$($case.id)"
  & $portraitRunner `
    -ServeLocal `
    -BuildRoot $BuildRoot `
    -OutputDirectory $caseDirectory `
    -ChromePath $ChromePath `
    -TimeoutSeconds $TimeoutSeconds `
    -Width $case.width `
    -Height $case.height `
    -SafeTop $case.safeTop `
    -SafeBottom $case.safeBottom `
    -HubVisual
  $manifestPath = Join-Path $caseDirectory 'hub-visual-evidence.json'
  $evidence = Read-AcceptedEvidence -Path $manifestPath `
    -ExpectedType 'outgame-hub-webgl-visual'
  if ([int]$evidence.viewport.width -ne [int]$case.width -or
      [int]$evidence.viewport.height -ne [int]$case.height -or
      [int]$evidence.safeArea.top -ne [int]$case.safeTop -or
      [int]$evidence.safeArea.bottom -ne [int]$case.safeBottom) {
    throw "Hub page evidence geometry mismatch: $($case.id)"
  }
  foreach ($page in @('home', 'activity', 'equipment', 'cultivation')) {
    if ([bool]$evidence.hubTelemetry.$page.fixtureActive) {
      throw "Live Hub page matrix used fixture data: $($case.id)/$page"
    }
  }
  $references.portraitPages[$case.id] = New-EvidenceReference `
    -Path $manifestPath -Evidence $evidence
}

foreach ($case in @($matrix | Where-Object {
    [int]$_.width -eq 402 -and [int]$_.height -eq 874
  })) {
  $stateDirectory = Join-Path $runRoot "named-states\$($case.id)"
  & $portraitRunner `
    -ServeLocal `
    -BuildRoot $BuildRoot `
    -OutputDirectory $stateDirectory `
    -ChromePath $ChromePath `
    -TimeoutSeconds $TimeoutSeconds `
    -Width $case.width `
    -Height $case.height `
    -SafeTop $case.safeTop `
    -SafeBottom $case.safeBottom `
    -HubStates
  $stateManifestPath = Join-Path $stateDirectory 'hub-named-state-evidence.json'
  $stateEvidence = Read-AcceptedEvidence -Path $stateManifestPath `
    -ExpectedType 'outgame-hub-named-state-webgl'
  if (@($stateEvidence.stateDefinitions).Count -ne 22 -or
      @($stateEvidence.telemetry.PSObject.Properties).Count -ne 22) {
    throw "Hub named-state evidence is incomplete: $($case.id)"
  }
  foreach ($property in $stateEvidence.telemetry.PSObject.Properties) {
    $expectedFixtureId = "acceptance-hub/$($property.Name)/v1"
    if (-not [bool]$property.Value.fixtureActive -or
        [string]$property.Value.fixtureId -cne $expectedFixtureId) {
      throw "Hub named-state fixture identity is invalid: $($case.id)/$($property.Name)"
    }
  }
  $references.namedStates[$case.id] = New-EvidenceReference `
    -Path $stateManifestPath -Evidence $stateEvidence

  $loopDirectory = Join-Path $runRoot "loops\$($case.id)"
  & $portraitRunner `
    -ServeLocal `
    -BuildRoot $BuildRoot `
    -OutputDirectory $loopDirectory `
    -ChromePath $ChromePath `
    -TimeoutSeconds $TimeoutSeconds `
    -Width $case.width `
    -Height $case.height `
    -SafeTop $case.safeTop `
    -SafeBottom $case.safeBottom `
    -LevelId orchard-02 `
    -HubLoop
  $loopManifestPath = Join-Path $loopDirectory 'hub-loop-evidence.json'
  $loopEvidence = Read-AcceptedEvidence -Path $loopManifestPath `
    -ExpectedType 'outgame-reward-to-battle-webgl-loop'
  foreach ($property in $loopEvidence.telemetry.PSObject.Properties) {
    if ([bool]$property.Value.fixtureActive) {
      throw "Real Hub loop used fixture data: $($case.id)/$($property.Name)"
    }
  }
  $references.loops[$case.id] = New-EvidenceReference `
    -Path $loopManifestPath -Evidence $loopEvidence
}

$desktopDirectory = Join-Path $runRoot 'desktop-host'
& $hostRunner `
  -Mode acceptance `
  -BuildRoot $BuildRoot `
  -OutputDirectory $desktopDirectory `
  -ChromePath $ChromePath `
  -TimeoutSeconds $TimeoutSeconds
$desktopManifestPath = Join-Path $desktopDirectory 'webgl-host-acceptance.json'
$desktopEvidence = Read-AcceptedEvidence -Path $desktopManifestPath `
  -ExpectedType 'desktop-webgl-host-acceptance'
$references.desktopHost = New-EvidenceReference `
  -Path $desktopManifestPath -Evidence $desktopEvidence

$aggregate = [ordered]@{
  schemaVersion = 1
  evidenceType = 'outgame-hub-complete-webgl-matrix'
  accepted = $true
  completedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
  buildRoot = (Resolve-Path -LiteralPath $BuildRoot).Path
  matrix = $matrix
  references = $references
  checks = [ordered]@{
    fourPagesAcrossEightPortraitCases = 'pass'
    fullAndInset402NamedStates = 'pass'
    fullAndInset402RealLoop = 'pass'
    desktopContainHost = 'pass'
    staticStatesUseExplicitFixtures = 'pass'
    realLoopsUseFreshProductionPersistence = 'pass'
    exactIdentityAndGrowthTelemetry = 'pass'
    manualVisualReview = 'manual-score-required'
  }
}
$aggregatePath = Join-Path $runRoot 'outgame-hub-matrix.json'
$aggregate | ConvertTo-Json -Depth 14 |
  Set-Content -LiteralPath $aggregatePath -Encoding UTF8
Write-Host "FRUIT_DEFENSE_OUTGAME_HUB_MATRIX_OK manifest=$aggregatePath"
