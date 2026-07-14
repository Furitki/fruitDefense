param(
  [string]$OutputPath = "$(Split-Path $PSScriptRoot)\docs\platform\douyin-compatibility-report.json",
  [string]$ToolchainPinPath = "$(Split-Path $PSScriptRoot)\docs\platform\douyin-toolchain-pin.json",
  [string]$EvidenceManifestPath = "$(Split-Path $PSScriptRoot)\docs\platform\douyin-evidence\manifest.json",
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

function Test-AllFieldsPresent {
  param(
    [object]$Value,
    [string[]]$Names
  )

  if ($null -eq $Value) { return $false }
  foreach ($name in $Names) {
    $property = $Value.PSObject.Properties[$name]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
      return $false
    }
  }
  return $true
}

function Test-ToolchainPinEvidenceFields {
  param([object]$Evidence)

  if ($null -eq $Evidence) { return $false }
  foreach ($name in @('unityCompileLogSha256', 'webglBuildLogSha256', 'conversionLogSha256', 'convertedProjectManifestSha256')) {
    $property = $Evidence.PSObject.Properties[$name]
    if ($null -eq $property -or [string]$property.Value -notmatch '^[0-9A-Fa-f]{64}$') { return $false }
  }

  $validatedAt = [DateTimeOffset]::MinValue
  return [DateTimeOffset]::TryParse([string]$Evidence.validatedAtUtc, [ref]$validatedAt)
}

function Test-EvidenceArtifacts {
  param(
    [object]$Row,
    [string]$ManifestDirectory
  )

  if ($null -eq $Row) { return $false }
  $artifacts = @($Row.artifacts)
  if ($artifacts.Count -eq 0) { return $false }

  $root = [IO.Path]::GetFullPath($ManifestDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
  foreach ($artifact in $artifacts) {
    $relativePath = [string]$artifact.path
    $expectedHash = ([string]$artifact.sha256).Trim().ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($relativePath) -or [IO.Path]::IsPathRooted($relativePath) -or $expectedHash -notmatch '^[0-9A-F]{64}$') {
      return $false
    }

    $artifactPath = [IO.Path]::GetFullPath((Join-Path $ManifestDirectory $relativePath))
    if (-not $artifactPath.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
      return $false
    }

    $actualHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -ne $expectedHash) { return $false }
  }
  return $true
}

function Get-EvidenceRowStatus {
  param(
    [object]$Row,
    [string]$ManifestDirectory
  )

  if ($null -eq $Row) { return 'Yellow' }
  $artifactsValid = Test-EvidenceArtifacts $Row $ManifestDirectory
  if ([string]$Row.status -eq 'Red' -and $artifactsValid) { return 'Red' }
  if ([string]$Row.status -ne 'Green' -or -not $artifactsValid -or $null -eq $Row.checks) { return 'Yellow' }

  $checkProperties = @($Row.checks.PSObject.Properties)
  if ($checkProperties.Count -eq 0 -or ($checkProperties | Where-Object { $_.Value -ne $true })) { return 'Yellow' }
  return 'Green'
}

$toolchainPin = $null
$toolchainPinError = $null
if (Test-Path -LiteralPath $ToolchainPinPath -PathType Leaf) {
  try { $toolchainPin = Get-Content -Raw -LiteralPath $ToolchainPinPath | ConvertFrom-Json }
  catch { $toolchainPinError = $_.Exception.GetType().Name }
}

$pinFields = @('unityEditor', 'ttsdk', 'douyinDeveloperTools', 'douyinBaseLibrary', 'addressables', 'node', 'webglConverter', 'wasmSplitCli', 'androidHostApp', 'iosHostApp')
$pinEvidenceFields = @('unityCompileLogSha256', 'webglBuildLogSha256', 'conversionLogSha256', 'convertedProjectManifestSha256', 'validatedAtUtc')
$toolchainPinValidated = $null -ne $toolchainPin -and
  [string]$toolchainPin.validationStatus -eq 'validated' -and
  (Test-AllFieldsPresent $toolchainPin $pinFields) -and
  (Test-AllFieldsPresent $toolchainPin.evidence $pinEvidenceFields) -and
  (Test-ToolchainPinEvidenceFields $toolchainPin.evidence)

if ($toolchainPinError) {
  Add-Check 'toolchain-pin' 'Yellow' $true "The toolchain pin file could not be parsed ($toolchainPinError)." 'Replace it from the template and record only directly observed versions.'
}
elseif ($toolchainPinValidated) {
  Add-Check 'toolchain-pin' 'Green' $true 'The validated toolchain pin contains independent SDK, IDE, converter, dependency, host, and evidence fields.' 'Retain the pin and its hashed evidence for release reproduction.'
}
else {
  Add-Check 'toolchain-pin' 'Yellow' $true 'No fully validated toolchain pin exists; the repository template intentionally contains null external versions.' 'Copy the template only after installing reviewed tools, then fill versions from direct observation and attach hashes.'
}

$evidenceManifest = $null
$evidenceManifestError = $null
$evidenceManifestDirectory = Split-Path ([IO.Path]::GetFullPath($EvidenceManifestPath))
if (Test-Path -LiteralPath $EvidenceManifestPath -PathType Leaf) {
  try { $evidenceManifest = Get-Content -Raw -LiteralPath $EvidenceManifestPath | ConvertFrom-Json }
  catch { $evidenceManifestError = $_.Exception.GetType().Name }
}

if ($evidenceManifestError) {
  Add-Check 'evidence-manifest' 'Yellow' $false "The evidence manifest could not be parsed ($evidenceManifestError)." 'Replace it from the template and keep artifact paths relative to the manifest.'
}
elseif ($null -eq $evidenceManifest) {
  Add-Check 'evidence-manifest' 'Yellow' $false 'No simulator/device evidence manifest exists; only the non-passing template is present.' 'Copy the template to docs/platform/douyin-evidence/manifest.json and attach verifiable relative artifacts.'
}
else {
  Add-Check 'evidence-manifest' 'Yellow' $false 'An evidence manifest exists; individual rows remain authoritative only when all checks and artifact hashes verify.' 'Complete every blocking row before requesting Green.'
}

$toolchainEvidenceStatus = Get-EvidenceRowStatus $evidenceManifest.rows.toolchain $evidenceManifestDirectory
$simulatorEvidenceStatus = Get-EvidenceRowStatus $evidenceManifest.rows.simulator $evidenceManifestDirectory
$androidEvidenceStatus = Get-EvidenceRowStatus $evidenceManifest.rows.android $evidenceManifestDirectory
$iosEvidenceStatus = Get-EvidenceRowStatus $evidenceManifest.rows.ios $evidenceManifestDirectory
$remoteContentEvidenceStatus = Get-EvidenceRowStatus $evidenceManifest.rows.remoteContent $evidenceManifestDirectory
$wasmEvidenceStatus = Get-EvidenceRowStatus $evidenceManifest.rows.wasmSplitting $evidenceManifestDirectory

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
$ttsdkEnvironmentRootPresent = @('TTSDK_ROOT', 'STARKSDK_ROOT') | Where-Object {
  $candidate = [Environment]::GetEnvironmentVariable($_)
  -not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate)
} | Select-Object -First 1
$ttsdkVersion = if ($ttsdkMatch.Success) { $ttsdkMatch.Groups['version'].Value } else { $null }
if ($ttsdkMatch.Success -or $ttsdkAsset -or $ttsdkEnvironmentRootPresent) {
  $ttsdkEvidence = if ($ttsdkVersion) { "TTSDK package reference $ttsdkVersion is present." } else { 'TTSDK/StarkSDK assets are present but not version-pinned in Packages/manifest.json.' }
  $ttsdkStatus = if ($toolchainPinValidated -and $toolchainEvidenceStatus -eq 'Green') { 'Green' } else { 'Yellow' }
  Add-Check 'ttsdk' $ttsdkStatus $true $ttsdkEvidence 'Compile and convert the exact SDK with Unity 6000.3.19f1 before marking Green.'
}
else {
  Add-Check 'ttsdk' 'Yellow' $true 'No TTSDK or StarkSDK dependency is installed.' 'Select and review an official SDK version in an isolated branch.'
}

$addressablesMatch = [regex]::Match($manifestText, '"com\.unity\.addressables"\s*:\s*"(?<version>[^"]+)"', 'IgnoreCase')
if ($addressablesMatch.Success) {
  $addressablesStatus = if ($toolchainPinValidated -and $remoteContentEvidenceStatus -eq 'Green') { 'Green' } else { 'Yellow' }
  Add-Check 'addressables' $addressablesStatus $true "Addressables $($addressablesMatch.Groups['version'].Value) is installed; provider evidence status is $remoteContentEvidenceStatus." 'Exercise the official TTAssetBundle provider and UnityWebRequest fallback.'
}
else {
  Add-Check 'addressables' 'Yellow' $true 'Addressables is not installed.' 'Pin Addressables only after the platform provider compatibility check.'
}

$developerToolCandidates = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($env:DOUYIN_DEVELOPER_TOOLS) -and (Test-Path -LiteralPath $env:DOUYIN_DEVELOPER_TOOLS)) {
  $developerToolCandidates.Add($env:DOUYIN_DEVELOPER_TOOLS)
}
$developerToolPattern = 'Douyin|ByteDance|Stark|TTSDK|\u6296\u97F3'
foreach ($root in @("$env:LOCALAPPDATA\Programs", "$env:LOCALAPPDATA\ByteDance", "$env:APPDATA\ByteDance", "$env:ProgramFiles", "${env:ProgramFiles(x86)}")) {
  if (Test-Path $root) {
    Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -match $developerToolPattern } |
      ForEach-Object { $developerToolCandidates.Add($_.FullName) }
  }
}
$developerToolRegistration = @(
  'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
  'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
  'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
) | ForEach-Object {
  Get-ItemProperty $_ -ErrorAction SilentlyContinue
} | Where-Object { $_.DisplayName -match $developerToolPattern } | Select-Object -First 1
$developerToolVersion = if ($developerToolRegistration -and -not [string]::IsNullOrWhiteSpace([string]$developerToolRegistration.DisplayVersion)) {
  [string]$developerToolRegistration.DisplayVersion
}
else { $null }
$developerToolsInstalled = $developerToolCandidates.Count -gt 0 -or $null -ne $developerToolRegistration
if ($developerToolsInstalled) {
  $developerToolEvidence = if ($developerToolVersion) { "Douyin developer tools $developerToolVersion are registered, but conversion and login are unverified." } else { 'A developer-tool installation candidate exists, but its exact version, conversion, and login are unverified.' }
  $developerToolsStatus = if ($toolchainPinValidated -and $toolchainEvidenceStatus -eq 'Green' -and $simulatorEvidenceStatus -eq 'Green') { 'Green' } else { 'Yellow' }
  Add-Check 'douyin-developer-tools' $developerToolsStatus $true $developerToolEvidence 'Record the exact version and complete simulator conversion.'
}
else {
  Add-Check 'douyin-developer-tools' 'Yellow' $true 'Douyin developer tools were not found in configured or standard locations.' 'Install a reviewed stable release and set DOUYIN_DEVELOPER_TOOLS if needed.'
}

$tmgCommand = Get-Command tmg -ErrorAction SilentlyContinue
$wasmSplitCommand = Get-Command tt-wasmsplit-ci -ErrorAction SilentlyContinue
if ($tmgCommand -and $wasmSplitCommand) {
  $cliStatus = if ($toolchainPinValidated -and $toolchainEvidenceStatus -eq 'Green' -and $wasmEvidenceStatus -eq 'Green') { 'Green' } else { 'Yellow' }
  Add-Check 'douyin-cli-tooling' $cliStatus $true 'Both tmg and tt-wasmsplit-ci commands are discoverable; the gate still requires pinned versions and verified runs.' 'Record exact CLI versions in the pin file and attach conversion/splitting evidence.'
}
else {
  Add-Check 'douyin-cli-tooling' 'Yellow' $true 'The tmg and tt-wasmsplit-ci command pair is not available on PATH.' 'Install the reviewed platform CLI set after selecting the SDK/IDE candidate.'
}

$hasAppId = Test-EnvironmentPresence @('DOUYIN_APP_ID', 'TT_APP_ID')
$hasDeveloperSession = Test-EnvironmentPresence @('DOUYIN_DEVELOPER_SESSION')
$hasUploadCredential = Test-EnvironmentPresence @('DOUYIN_UPLOAD_TOKEN', 'DOUYIN_PRIVATE_KEY')
if ($hasAppId -and $hasDeveloperSession) {
  $accountStatus = if ($simulatorEvidenceStatus -eq 'Green' -and $androidEvidenceStatus -eq 'Green' -and $iosEvidenceStatus -eq 'Green') { 'Green' } else { 'Yellow' }
  Add-Check 'douyin-account' $accountStatus $true 'AppID and developer-session presence flags are set; values are intentionally omitted.' 'Use an authorized interactive session for simulator and preview evidence.'
}
elseif ($simulatorEvidenceStatus -eq 'Green' -and $androidEvidenceStatus -eq 'Green' -and $iosEvidenceStatus -eq 'Green') {
  Add-Check 'douyin-account' 'Green' $true 'Verified simulator and device artifacts prove an authorized interactive session was used; no credential value is retained.' 'Keep credentials outside reports and source control.'
}
else {
  Add-Check 'douyin-account' 'Yellow' $true 'Required AppID or developer-session presence flags are missing.' 'Provide authorized platform access without committing credentials.'
}

$buildRoot = Join-Path $projectRoot 'Builds\WebGL'
$webglBuildBytes = 0L
if (Test-Path (Join-Path $buildRoot 'index.html')) {
  $webglBuildBytes = (Get-ChildItem $buildRoot -Recurse -File | Measure-Object Length -Sum).Sum
  $indexHashPrefix = (Get-FileHash -LiteralPath (Join-Path $buildRoot 'index.html') -Algorithm SHA256).Hash.Substring(0, 12)
  Add-Check 'baseline-webgl-build' 'Green' $true "A WebGL build exists and contains $webglBuildBytes bytes before Douyin conversion; index SHA-256 starts $indexHashPrefix." 'Regenerate after merging each P0 wave; pre-conversion bytes are not directly comparable to Douyin code-package limits.'
}
else {
  Add-Check 'baseline-webgl-build' 'Yellow' $true 'No generated WebGL entry page was found.' 'Run FruitDefense.Editor.WebBuild.Build before conversion.'
}

$updateEvidenceStatus = if ($simulatorEvidenceStatus -eq 'Red' -or $androidEvidenceStatus -eq 'Red' -or $iosEvidenceStatus -eq 'Red') { 'Red' }
  elseif ($simulatorEvidenceStatus -eq 'Green' -and $androidEvidenceStatus -eq 'Green' -and $iosEvidenceStatus -eq 'Green') { 'Green' }
  else { 'Yellow' }
$stabilityEvidenceStatus = if ($androidEvidenceStatus -eq 'Red' -or $iosEvidenceStatus -eq 'Red') { 'Red' }
  elseif ($androidEvidenceStatus -eq 'Green' -and $iosEvidenceStatus -eq 'Green') { 'Green' }
  else { 'Yellow' }

foreach ($manualCheck in @(
  @{ id = 'douyin-conversion'; status = $toolchainEvidenceStatus; action = 'Convert the pinned Unity build in the reviewed TTSDK/toolchain.' },
  @{ id = 'douyin-simulator'; status = $simulatorEvidenceStatus; action = 'Verify launch, input, audio, lifecycle, HTTPS/cache, update, and content behavior.' },
  @{ id = 'android-device'; status = $androidEvidenceStatus; action = 'Run cold/warm launch, battle, lifecycle, update, and 30-minute stability.' },
  @{ id = 'ios-device'; status = $iosEvidenceStatus; action = 'Run cold/warm launch, battle, lifecycle, update, and 30-minute stability.' },
  @{ id = 'code-package-update'; status = $updateEvidenceStatus; action = 'Exercise code-package update callbacks and restart outside battle.' },
  @{ id = 'remote-content'; status = $remoteContentEvidenceStatus; action = 'Exercise TTAssetBundle and UnityWebRequest provider, cache, unload, and fallback paths.' },
  @{ id = 'wasm-splitting'; status = $wasmEvidenceStatus; action = 'Collect Bootstrap, Lobby, first battle, lifecycle, and update UI functions on Android and iOS.' },
  @{ id = 'stability'; status = $stabilityEvidenceStatus; action = 'Record crash, OOM, memory, and repeated-battle results for 30 minutes on both systems.' }
)) {
  $manualEvidence = if ($manualCheck.status -eq 'Green') { 'All manifest checks are true and every relative artifact SHA-256 verifies.' }
    elseif ($manualCheck.status -eq 'Red') { 'The evidence manifest records a reproducible failure with verifiable artifacts.' }
    else { 'Required checks and verifiable artifacts are incomplete or absent.' }
  Add-Check $manualCheck.id $manualCheck.status $true $manualEvidence $manualCheck.action
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
    ttsdk = if ($toolchainPinValidated) { [string]$toolchainPin.ttsdk } else { $ttsdkVersion }
    developerTools = if ($toolchainPinValidated) { [string]$toolchainPin.douyinDeveloperTools } else { $null }
    hostBaseline = if ($toolchainPinValidated) { "$($toolchainPin.androidHostApp) / $($toolchainPin.iosHostApp)" } else { $null }
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
