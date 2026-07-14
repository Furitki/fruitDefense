param(
  [string]$OutputPath = "$(Split-Path $PSScriptRoot)\docs\platform\wechat-compatibility-report.json",
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

function Measure-DirectoryBytes {
  param([string]$Path)
  if (-not (Test-Path -LiteralPath $Path)) { return 0L }
  $measurement = Get-ChildItem -LiteralPath $Path -Recurse -File -ErrorAction SilentlyContinue |
    Measure-Object Length -Sum
  if ($null -eq $measurement.Sum) { return 0L }
  return [long]$measurement.Sum
}

$projectVersionPath = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
$projectVersionText = if (Test-Path $projectVersionPath) { Get-Content -Raw $projectVersionPath } else { '' }
$unityVersion = if ($projectVersionText -match 'm_EditorVersion:\s*(?<version>\S+)') { $Matches.version } else { 'unknown' }
$unityExe = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
$webglModule = "C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Data\PlaybackEngines\WebGLSupport"

if ($unityVersion -eq '6000.3.19f1' -and (Test-Path $unityExe)) {
  Add-Check 'unity-editor' 'Green' $true "Unity $unityVersion is installed through Unity Hub." 'Retain this exact editor until an evidenced platform matrix approves a change.'
}
elseif (Test-Path $unityExe) {
  Add-Check 'unity-editor' 'Yellow' $true "Project editor $unityVersion is installed but differs from the planned baseline." 'Reconcile the project lock before conversion.'
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
  Add-Check 'node-runtime' 'Green' $false "Node $nodeVersion is available for release tooling." 'Pin a project tool version only if the selected converter or CI workflow requires it.'
}
else {
  Add-Check 'node-runtime' 'Yellow' $false 'Node was not found.' 'Install a reviewed Node release before adding CI tooling that requires it.'
}

$manifestPath = Join-Path $projectRoot 'Packages\manifest.json'
$manifestText = if (Test-Path $manifestPath) { Get-Content -Raw $manifestPath } else { '' }
$sdkMatch = [regex]::Match($manifestText, '"com\.qq\.weixin\.minigame"\s*:\s*"(?<reference>[^"]+)"', 'IgnoreCase')
$sdkReference = if ($sdkMatch.Success) { $sdkMatch.Groups['reference'].Value } else { $null }
$sdkCommit = $null
if ($sdkReference -and $sdkReference -match '#(?<commit>[0-9a-fA-F]{40})(?:$|[^0-9a-fA-F])') {
  $sdkCommit = $Matches.commit.ToLowerInvariant()
}

$sdkAssetRoots = @(
  (Join-Path $projectRoot 'Assets\WX-WASM-SDK'),
  (Join-Path $projectRoot 'Assets\WX-WASM-SDK-V2'),
  (Join-Path $projectRoot 'Assets\WXSDK')
)
$sdkAssetPresent = [bool]($sdkAssetRoots | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)

if ($sdkReference -and $sdkCommit) {
  Add-Check 'wxsdk-converter' 'Yellow' $true 'An immutable WXSDK package reference is present, but Unity 6000.3.19f1 compile/conversion evidence is absent.' 'Compile and convert with this exact commit before marking Green.'
}
elseif ($sdkReference) {
  Add-Check 'wxsdk-converter' 'Yellow' $true 'A WXSDK package reference is present but is not locked to a full immutable Git commit.' 'Pin the successfully tested official SDK commit in an isolated integration.'
}
elseif ($sdkAssetPresent) {
  Add-Check 'wxsdk-converter' 'Yellow' $true 'WXSDK assets are present without an immutable Packages/manifest.json reference.' 'Identify the source revision and pin the successfully tested official SDK commit.'
}
else {
  Add-Check 'wxsdk-converter' 'Yellow' $true 'No WXSDK or WeChat Unity conversion dependency is installed.' 'Review and pin an official adapter commit in an isolated branch.'
}

$addressablesMatch = [regex]::Match($manifestText, '"com\.unity\.addressables"\s*:\s*"(?<version>[^"]+)"', 'IgnoreCase')
if ($addressablesMatch.Success) {
  Add-Check 'remote-content-provider' 'Yellow' $true "Addressables $($addressablesMatch.Groups['version'].Value) is installed but has no WeChat cache/fallback evidence." 'Exercise the selected WXAssetBundle/provider path and UnityWebRequest fallback on devices.'
}
else {
  Add-Check 'remote-content-provider' 'Yellow' $true 'Addressables is not installed and no WXAssetBundle delivery evidence exists.' 'Select the content provider only after the official converter compatibility check.'
}

$developerToolCandidates = New-Object System.Collections.Generic.List[object]
if (-not [string]::IsNullOrWhiteSpace($env:WECHAT_DEVELOPER_TOOLS)) {
  $candidatePath = $env:WECHAT_DEVELOPER_TOOLS
  if (Test-Path -LiteralPath $candidatePath) {
    $developerToolCandidates.Add([ordered]@{ category = 'configured-environment'; path = $candidatePath })
  }
}

$programFiles = [Environment]::GetFolderPath('ProgramFiles')
$programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
$knownToolPaths = @(
  (Join-Path $programFilesX86 'Tencent\微信web开发者工具\微信开发者工具.exe'),
  (Join-Path $programFiles 'Tencent\微信web开发者工具\微信开发者工具.exe'),
  (Join-Path $env:LOCALAPPDATA '微信开发者工具\微信开发者工具.exe'),
  (Join-Path $env:LOCALAPPDATA 'Programs\微信开发者工具\微信开发者工具.exe')
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

foreach ($candidatePath in $knownToolPaths) {
  if (Test-Path -LiteralPath $candidatePath) {
    $developerToolCandidates.Add([ordered]@{ category = 'standard-installation'; path = $candidatePath })
  }
}

$developerToolsVersion = $null
if ($developerToolCandidates.Count -gt 0) {
  $candidate = $developerToolCandidates[0]
  $versionTarget = $candidate.path
  if ((Get-Item -LiteralPath $versionTarget).PSIsContainer) {
    $directExe = Join-Path $versionTarget '微信开发者工具.exe'
    if (Test-Path -LiteralPath $directExe) { $versionTarget = $directExe }
  }
  if (Test-Path -LiteralPath $versionTarget -PathType Leaf) {
    $versionInfo = (Get-Item -LiteralPath $versionTarget).VersionInfo
    $developerToolsVersion = if ($versionInfo.ProductVersion) { $versionInfo.ProductVersion } else { $versionInfo.FileVersion }
  }
  $versionEvidence = if ($developerToolsVersion) { " version $developerToolsVersion" } else { ' with no readable product version' }
  Add-Check 'wechat-developer-tools' 'Yellow' $true "A WeChat Developer Tools candidate was found in the $($candidate.category) category$versionEvidence; Stable edition and conversion are unverified." 'Confirm Stable edition, record the exact version, and complete conversion/simulator checks.'
}
else {
  Add-Check 'wechat-developer-tools' 'Yellow' $true 'WeChat Developer Tools were not found in the configured or standard installation categories.' 'Install a reviewed Stable release and set WECHAT_DEVELOPER_TOOLS if needed.'
}

$hasAppId = Test-EnvironmentPresence @('WECHAT_APP_ID', 'WX_APP_ID')
$hasDeveloperSession = Test-EnvironmentPresence @('WECHAT_DEVELOPER_SESSION', 'WX_DEVELOPER_SESSION')
$hasUploadPrivateKey = Test-EnvironmentPresence @('WECHAT_UPLOAD_PRIVATE_KEY', 'WECHAT_CI_PRIVATE_KEY', 'WX_UPLOAD_PRIVATE_KEY')
if ($hasAppId -and $hasDeveloperSession) {
  Add-Check 'wechat-account' 'Yellow' $true 'AppID and developer-session presence flags are set; values are intentionally omitted.' 'Use an authorized interactive session for simulator and device evidence.'
}
else {
  Add-Check 'wechat-account' 'Yellow' $true 'Required AppID or developer-session presence flags are missing.' 'Provide authorized platform access without committing credentials.'
}

$webglBuildRoot = Join-Path $projectRoot 'Builds\WebGL'
$webglBuildBytes = Measure-DirectoryBytes $webglBuildRoot
if (Test-Path (Join-Path $webglBuildRoot 'index.html')) {
  Add-Check 'baseline-webgl-build' 'Green' $true "A WebGL build exists and contains $webglBuildBytes bytes before WeChat conversion." 'Regenerate after relevant P0 integration changes and before conversion.'
}
else {
  Add-Check 'baseline-webgl-build' 'Yellow' $true 'No generated WebGL entry page was found.' 'Run FruitDefense.Editor.WebBuild.Build before conversion.'
}

$convertedCandidates = @(
  (Join-Path $projectRoot 'Builds\WeChatMiniGame'),
  (Join-Path $projectRoot 'Builds\WeixinMiniGame'),
  (Join-Path $projectRoot 'Builds\WeChat'),
  (Join-Path $projectRoot 'minigame')
)
$convertedRoot = $convertedCandidates | Where-Object { Test-Path (Join-Path $_ 'game.js') } | Select-Object -First 1
$convertedMiniGameBytes = if ($convertedRoot) { Measure-DirectoryBytes $convertedRoot } else { 0L }
if ($convertedRoot) {
  Add-Check 'wechat-conversion' 'Yellow' $true "A converted-project candidate with game.js exists and contains $convertedMiniGameBytes bytes, but its converter/version provenance and simulator run are unverified." 'Record exact converter provenance and pass static and simulator validation.'
}
else {
  Add-Check 'wechat-conversion' 'Yellow' $true 'No converted WeChat Mini Game artifact has been recorded.' 'Convert the pinned Unity export with the reviewed official SDK/toolchain.'
}

foreach ($manualCheck in @(
  @{ id = 'wechat-simulator'; evidence = 'No Stable Developer Tools simulator evidence has been recorded.'; action = 'Verify launch, input, audio, lifecycle, HTTPS/cache, UpdateManager, remote content, subpackages, and Wasm splitting.' },
  @{ id = 'android-device'; evidence = 'No Android physical-device matrix has been recorded.'; action = 'Run cold/warm launch, battle, lifecycle, update, cache, and 30-minute stability.' },
  @{ id = 'ios-device'; evidence = 'No iOS physical-device matrix has been recorded.'; action = 'Run cold/warm launch, battle, lifecycle, update, cache, and 30-minute stability.' },
  @{ id = 'code-package-update'; evidence = 'No wx.getUpdateManager callback and applyUpdate restart evidence has been recorded.'; action = 'Exercise update check, download, ready, failure, and lobby/settlement restart behavior.' },
  @{ id = 'remote-content-cache'; evidence = 'No WXAssetBundle/Addressables/UnityWebRequest cold-cache, warm-cache, and version-fallback evidence has been recorded.'; action = 'Exercise target, last-known-good, and bundled content paths on both device families.' },
  @{ id = 'ordinary-subpackages'; evidence = 'No wx.loadSubpackage package-layout and loading evidence has been recorded.'; action = 'Verify subpackage configuration and loading separately from content hot update.' },
  @{ id = 'wasm-splitting'; evidence = 'No Wasm-splitting startup and completeness evidence has been recorded.'; action = 'Exercise Bootstrap, Lobby, first battle, lifecycle, and update UI coverage on Android and iOS.' },
  @{ id = 'stability'; evidence = 'No 30-minute Android and iOS stability evidence has been recorded.'; action = 'Record crashes, OOM, memory behavior, and repeated-battle results on both systems.' }
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
  platform = 'wechat-minigame'
  generatedAtUtc = $observedAt
  overallStatus = $overallStatus
  project = [ordered]@{
    unityVersion = $unityVersion
    gitRevision = [string]$gitRevision
  }
  officialAdapterCandidate = [ordered]@{
    repository = 'https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git'
    observedCommit = 'ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228'
    changelogVersion = '0.1.33'
    packageManifestVersion = '0.1.1'
    retrievedAtUtc = '2026-07-15T00:00:00Z'
  }
  pinnedVersions = [ordered]@{
    wxsdkReference = $sdkReference
    wxsdkCommit = $sdkCommit
    developerTools = $developerToolsVersion
    wechatClient = $null
    baseLibrary = $null
    hostBaseline = $null
  }
  credentialPresence = [ordered]@{
    appId = [bool]$hasAppId
    developerSession = [bool]$hasDeveloperSession
    uploadPrivateKey = [bool]$hasUploadPrivateKey
  }
  artifactSizes = [ordered]@{
    webglBuildBytes = [long]$webglBuildBytes
    convertedMiniGameBytes = [long]$convertedMiniGameBytes
  }
  officialLimits = [ordered]@{
    mainCodePackageMb = $null
    totalCodePackageMb = $null
    singleSubpackageMb = $null
    firstResourcePackageMb = 30
    evidence = 'Official SDK changelog notes a 30 MB first-resource-package total for the subpackage mode on 2024-12-18; current code-package limits remain unverified.'
  }
  checks = $checks
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path $resolvedOutput
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resolvedOutput -Encoding UTF8

Write-Host "FRUIT_DEFENSE_WECHAT_PREFLIGHT status=$overallStatus report=$resolvedOutput"
if ($RequireGreen -and $overallStatus -ne 'Green') { exit 2 }
