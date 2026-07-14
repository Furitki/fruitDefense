param(
  [string]$OutputPath = "$(Split-Path $PSScriptRoot)\docs\platform\douyin-compatibility-report.json",
  [switch]$RequireGreen
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$observedAt = [DateTimeOffset]::UtcNow.ToString('o')
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check {
  param(
    [string]$Id,
    [ValidateSet('Green', 'Yellow', 'Red')][string]$Status,
    [bool]$Blocking,
    [string]$Evidence,
    [string]$NextAction
  )

  $checks.Add([ordered]@{
    id = $Id
    status = $Status
    blocking = $Blocking
    evidence = $Evidence
    observedAtUtc = $observedAt
    nextAction = $NextAction
  })
}

function Test-EnvironmentPresence {
  param([string[]]$Names)
  foreach ($name in $Names) {
    if (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
      return $true
    }
  }
  return $false
}

$projectVersionPath = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
$projectVersionText = if (Test-Path $projectVersionPath) { Get-Content -Raw $projectVersionPath } else { '' }
$unityVersion = if ($projectVersionText -match 'm_EditorVersion:\s*(?<version>\S+)') { $Matches.version } else { 'unknown' }
$unityExe = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
$webglModule = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Data\PlaybackEngines\WebGLSupport"

if ($unityVersion -eq '6000.3.19f1' -and (Test-Path $unityExe)) {
  Add-Check 'unity-editor' 'Green' $true "Unity $unityVersion is installed through Unity Hub." 'Retain this exact editor until the platform spike approves a different version.'
}
elseif (Test-Path $unityExe) {
  Add-Check 'unity-editor' 'Yellow' $true "Project editor $unityVersion is installed but differs from the planned baseline." 'Reconcile the project lock and planned Unity version before conversion.'
}
else {
  Add-Check 'unity-editor' 'Red' $true "The project requests Unity $unityVersion but its Hub editor was not found." 'Install the exact editor before running conversion.'
}

if (Test-Path $webglModule) {
  Add-Check 'unity-webgl-module' 'Green' $true 'The WebGL playback-engine module is installed for the project editor.' 'Keep the module installed on developer and build machines.'
}
else {
  Add-Check 'unity-webgl-module' 'Red' $true 'The WebGL playback-engine module was not found for the project editor.' 'Install Unity WebGL Build Support.'
}

$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
$nodeVersion = if ($nodeCommand) { (& node --version 2>$null) } else { $null }
if ($nodeVersion) {
  Add-Check 'node-runtime' 'Green' $false "Node $nodeVersion is available for platform and release tooling." 'Pin a project tool version when the Douyin converter requires one.'
}
else {
  Add-Check 'node-runtime' 'Yellow' $false 'Node was not found.' 'Install the converter-supported Node release before platform automation.'
}

$manifestPath = Join-Path $projectRoot 'Packages\manifest.json'
$manifestText = if (Test-Path $manifestPath) { Get-Content -Raw $manifestPath } else { '' }
$ttsdkMatch = [regex]::Match($manifestText, '"(?<name>[^"]*(?:ttsdk|stark)[^"]*)"\s*:\s*"(?<version>[^"]+)"', 'IgnoreCase')
$ttsdkAsset = Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -match 'com\.bytedance\.ttsdk|StarkSDK|TTSDK' } |
  Select-Object -First 1
$ttsdkVersion = if ($ttsdkMatch.Success) { $ttsdkMatch.Groups['version'].Value } else { $null }
if ($ttsdkMatch.Success -or $ttsdkAsset) {
  $ttsdkEvidence = if ($ttsdkVersion) { "TTSDK package reference $ttsdkVersion is present." } else { 'TTSDK/StarkSDK assets are present but not version-pinned in Packages/manifest.json.' }
  Add-Check 'ttsdk' 'Yellow' $true $ttsdkEvidence 'Compile and convert the exact SDK with Unity 6000.3.19f1 before marking Green.'
}
else {
  Add-Check 'ttsdk' 'Yellow' $true 'No TTSDK or StarkSDK dependency is installed.' 'Select and review an official SDK version in an isolated branch.'
}

$addressablesMatch = [regex]::Match($manifestText, '"com\.unity\.addressables"\s*:\s*"(?<version>[^"]+)"', 'IgnoreCase')
if ($addressablesMatch.Success) {
  Add-Check 'addressables' 'Yellow' $true "Addressables $($addressablesMatch.Groups['version'].Value) is installed but has no Douyin provider evidence." 'Exercise the official TTAssetBundle provider and UnityWebRequest fallback.'
}
else {
  Add-Check 'addressables' 'Yellow' $true 'Addressables is not installed.' 'Pin Addressables only after the platform provider compatibility check.'
}

$developerToolCandidates = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($env:DOUYIN_DEVELOPER_TOOLS)) { $developerToolCandidates.Add($env:DOUYIN_DEVELOPER_TOOLS) }
foreach ($root in @("$env:LOCALAPPDATA\Programs", "$env:LOCALAPPDATA\ByteDance", "$env:APPDATA\ByteDance")) {
  if (Test-Path $root) {
    Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -match 'Douyin|ByteDance|Stark|抖音' } |
      ForEach-Object { $developerToolCandidates.Add($_.FullName) }
  }
}
$developerToolsInstalled = $developerToolCandidates.Count -gt 0
if ($developerToolsInstalled) {
  Add-Check 'douyin-developer-tools' 'Yellow' $true 'A developer-tool installation candidate exists but its version and login session are unverified.' 'Record the exact version and complete simulator conversion.'
}
else {
  Add-Check 'douyin-developer-tools' 'Yellow' $true 'Douyin developer tools were not found in configured or standard locations.' 'Install a reviewed stable release and set DOUYIN_DEVELOPER_TOOLS if needed.'
}

$hasAppId = Test-EnvironmentPresence @('DOUYIN_APP_ID', 'TT_APP_ID')
$hasDeveloperSession = Test-EnvironmentPresence @('DOUYIN_DEVELOPER_SESSION')
$hasUploadCredential = Test-EnvironmentPresence @('DOUYIN_UPLOAD_TOKEN', 'DOUYIN_PRIVATE_KEY')
if ($hasAppId -and $hasDeveloperSession) {
  Add-Check 'douyin-account' 'Yellow' $true 'AppID and developer-session presence flags are set; values are intentionally omitted.' 'Use an authorized interactive session for simulator and preview evidence.'
}
else {
  Add-Check 'douyin-account' 'Yellow' $true 'Required AppID or developer-session presence flags are missing.' 'Provide authorized platform access without committing credentials.'
}

$buildRoot = Join-Path $projectRoot 'Builds\WebGL'
$webglBuildBytes = 0L
if (Test-Path (Join-Path $buildRoot 'index.html')) {
  $webglBuildBytes = (Get-ChildItem $buildRoot -Recurse -File | Measure-Object Length -Sum).Sum
  Add-Check 'baseline-webgl-build' 'Green' $true "A WebGL build exists and contains $webglBuildBytes bytes before Douyin conversion." 'Regenerate after merging each P0 wave and before conversion.'
}
else {
  Add-Check 'baseline-webgl-build' 'Yellow' $true 'No generated WebGL entry page was found.' 'Run FruitDefense.Editor.WebBuild.Build before conversion.'
}

foreach ($manualCheck in @(
  @{ id = 'douyin-conversion'; evidence = 'No converted Douyin mini-game artifact has been recorded.'; action = 'Convert the pinned Unity build in the reviewed TTSDK/toolchain.' },
  @{ id = 'douyin-simulator'; evidence = 'No simulator evidence has been recorded.'; action = 'Verify launch, input, audio, lifecycle, HTTPS/cache, update, and content behavior.' },
  @{ id = 'android-device'; evidence = 'No Android physical-device matrix has been recorded.'; action = 'Run cold/warm launch, battle, lifecycle, update, and 30-minute stability.' },
  @{ id = 'ios-device'; evidence = 'No iOS physical-device matrix has been recorded.'; action = 'Run cold/warm launch, battle, lifecycle, update, and 30-minute stability.' },
  @{ id = 'code-package-update'; evidence = 'No UpdateManager check/download/ready/failure/restart evidence has been recorded.'; action = 'Exercise code-package update callbacks and restart outside battle.' },
  @{ id = 'remote-content'; evidence = 'No TTAssetBundle/Addressables cache and fallback evidence has been recorded.'; action = 'Exercise target, last-known-good, and bundled content paths.' },
  @{ id = 'wasm-splitting'; evidence = 'No Android+iOS function-collection evidence has been recorded.'; action = 'Collect Bootstrap, Lobby, first battle, lifecycle, and update UI functions.' },
  @{ id = 'stability'; evidence = 'No 30-minute Android and iOS stability evidence has been recorded.'; action = 'Record crash, OOM, memory, and repeated-battle results on both systems.' }
)) {
  Add-Check $manualCheck.id 'Yellow' $true $manualCheck.evidence $manualCheck.action
}

$gitRevision = 'unknown'
try {
  $gitRevision = (& git -C $projectRoot rev-parse --short HEAD 2>$null)
  if ([string]::IsNullOrWhiteSpace($gitRevision)) { $gitRevision = 'unknown' }
}
catch { $gitRevision = 'unknown' }

$blockingChecks = @($checks | Where-Object { $_.blocking })
$overallStatus = if ($blockingChecks | Where-Object { $_.status -eq 'Red' }) {
  'Red'
}
elseif ($blockingChecks | Where-Object { $_.status -eq 'Yellow' }) {
  'Yellow'
}
else {
  'Green'
}

$report = [ordered]@{
  schemaVersion = 1
  platform = 'douyin-minigame'
  generatedAtUtc = $observedAt
  overallStatus = $overallStatus
  project = [ordered]@{
    unityVersion = $unityVersion
    gitRevision = [string]$gitRevision
    webglBuildBytes = [long]$webglBuildBytes
  }
  pinnedVersions = [ordered]@{
    ttsdk = $ttsdkVersion
    developerTools = $null
    hostBaseline = $null
  }
  credentialPresence = [ordered]@{
    appId = [bool]$hasAppId
    developerSession = [bool]$hasDeveloperSession
    uploadCredential = [bool]$hasUploadCredential
  }
  officialLimits = [ordered]@{
    mainPackageMb = 4
    totalCodePackageMb = 20
    singleSubpackageMb = 20
  }
  checks = $checks
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path $resolvedOutput
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resolvedOutput -Encoding UTF8

Write-Host "FRUIT_DEFENSE_DOUYIN_PREFLIGHT status=$overallStatus report=$resolvedOutput"
if ($RequireGreen -and $overallStatus -ne 'Green') { exit 2 }
