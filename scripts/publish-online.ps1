param(
  [string]$Server = '175.178.80.66',
  [string]$User = 'root',
  [string]$RemoteDir = '/root/app/furitDefense',
  [string]$KeyPath = "$HOME\.ssh\id_ed25519",
  [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe',
  [switch]$SkipBuild,
  [switch]$Execute
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$projectRoot = Split-Path -Parent $PSScriptRoot
$releaseBranch = 'oper'
$commonScript = Join-Path $PSScriptRoot 'pipeline-common.ps1'
$localBuildScript = Join-Path $PSScriptRoot 'build-local.ps1'
$deployScript = Join-Path $projectRoot 'deploy.ps1'
$localManifestPath = Join-Path $projectRoot 'Builds\Pipeline\local-build-manifest.json'
$publishManifestPath = Join-Path $projectRoot 'Builds\Pipeline\online-publish-manifest.json'
$deploymentTransitionPath = Join-Path $projectRoot 'Builds\Pipeline\deployment-transition.json'
$webEntryPath = Join-Path $projectRoot 'Builds\WebGL\index.html'

if (-not (Test-Path -LiteralPath $commonScript -PathType Leaf)) {
  throw "Pipeline helper not found: $commonScript"
}
. $commonScript

function Get-ValidatedWebEvidence {
  param(
    [Parameter(Mandatory = $true)][string]$ManifestPath,
    [Parameter(Mandatory = $true)][string]$ExpectedRevision,
    [Parameter(Mandatory = $true)][string]$WebEntryPath
  )

  if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Local build manifest not found: $ManifestPath"
  }
  $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
  if ($manifest.schemaVersion -ne 3 -or $manifest.pipeline -ne 'local-build') {
    throw "Unsupported local build manifest: $ManifestPath"
  }
  if ($manifest.gitRevision -ne $ExpectedRevision) {
    throw "Local Web build revision $($manifest.gitRevision) does not match current revision $ExpectedRevision"
  }
  if ([bool]$manifest.dirtyBeforeBuild) {
    throw 'Local Web build manifest records a dirty source tree and cannot be published.'
  }

  $webEvidence = @(@($manifest.targets) | Where-Object { $_.target -eq 'Web' })
  if ($webEvidence.Count -ne 1) {
    throw 'Local build manifest does not contain exactly one successful Web target.'
  }
  if (-not (Test-Path -LiteralPath $WebEntryPath -PathType Leaf)) {
    throw "Web build entry not found: $WebEntryPath"
  }

  $currentHash = Get-FruitDefenseFileHash -Path $WebEntryPath
  if ($currentHash -ne $webEvidence[0].primarySha256) {
    throw 'Current WebGL index hash does not match the local build manifest.'
  }
  if ([string]$webEvidence[0].determinism.result -ne 'pass') {
    throw 'Local Web build manifest does not contain a passed deterministic comparison.'
  }
  $currentPayloads = Get-FruitDefenseWebPayloadEvidence -EntryPath $WebEntryPath
  foreach ($role in $currentPayloads.Keys) {
    $evidenceProperty = $webEvidence[0].payloads.PSObject.Properties[$role]
    if ($null -eq $evidenceProperty) {
      throw "Local Web build manifest is missing the $role payload."
    }
    $evidencePayload = $evidenceProperty.Value
    foreach ($field in @('fileName', 'version', 'sha256', 'sizeBytes')) {
      if ([string]$evidencePayload.$field -cne [string]$currentPayloads[$role][$field]) {
        throw "Current WebGL $role $field does not match the local build manifest."
      }
    }
  }

  return $webEvidence[0]
}

$gitState = Get-FruitDefenseGitState -ProjectRoot $projectRoot
$mode = if ($Execute) { 'execute' } else { 'plan' }
$plan = [ordered]@{
  pipeline = 'online-publish'
  mode = $mode
  target = 'ordinary-webgl'
  server = $Server
  user = $User
  remoteDir = $RemoteDir
  keyPath = $KeyPath
  releaseBranch = $releaseBranch
  currentBranch = $gitState.branch
  gitRevision = $gitState.revision
  dirty = $gitState.dirty
  skipBuild = [bool]$SkipBuild
  webOutput = (Join-Path $projectRoot 'Builds\WebGL')
  localManifest = $localManifestPath
  delegatedWorkflow = $deployScript
  gates = @(
    'explicit -Execute authorization',
    "required Git branch '$releaseBranch' and clean working tree",
    'SSH key path exists',
    'P0-validated Web build manifest matches current revision and index hash',
    'local portrait acceptance',
    'remote upload, service health, cache/header checks, and deployed acceptance'
  )
  miniGameRelease = 'unavailable; this pipeline publishes ordinary WebGL only'
}

Write-Host ($plan | ConvertTo-Json -Depth 6)
if (-not $Execute) {
  Write-Host "FRUIT_DEFENSE_ONLINE_PUBLISH_PLAN_OK target=ordinary-webgl server=$Server branch=$releaseBranch"
  return
}

if ($gitState.branch -ne $releaseBranch) {
  throw "Online publication requires release branch '$releaseBranch'; current branch is '$($gitState.branch)'."
}
if ($gitState.dirty) {
  throw 'Online publication requires a clean working tree.'
}
if (-not (Test-Path -LiteralPath $KeyPath -PathType Leaf)) {
  throw "SSH private key not found: $KeyPath"
}
if (-not (Test-Path -LiteralPath $localBuildScript -PathType Leaf)) {
  throw "Local build pipeline not found: $localBuildScript"
}
if (-not (Test-Path -LiteralPath $deployScript -PathType Leaf)) {
  throw "Deployment workflow not found: $deployScript"
}

if (-not $SkipBuild) {
  & $localBuildScript -Target Web -UnityPath $UnityPath
}

$webEvidence = Get-ValidatedWebEvidence `
  -ManifestPath $localManifestPath `
  -ExpectedRevision $gitState.revision `
  -WebEntryPath $webEntryPath

$publishStartedAt = [DateTimeOffset]::UtcNow
& $deployScript `
  -Server $Server `
  -User $User `
  -RemoteDir $RemoteDir `
  -KeyPath $KeyPath `
  -UnityPath $UnityPath `
  -SkipBuild
$publishFinishedAt = [DateTimeOffset]::UtcNow

if (-not (Test-Path -LiteralPath $deploymentTransitionPath -PathType Leaf)) {
  throw "Deployment transition evidence not found: $deploymentTransitionPath"
}
$deploymentTransition = Get-Content -LiteralPath $deploymentTransitionPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($deploymentTransition.schemaVersion -ne 1 -or
    $deploymentTransition.evidenceType -ne 'webgl-release-transition' -or
    -not [bool]$deploymentTransition.accepted) {
  throw "Invalid deployment transition evidence: $deploymentTransitionPath"
}

$publishManifest = [ordered]@{
  schemaVersion = 3
  pipeline = 'online-publish'
  target = 'ordinary-webgl'
  gitRevision = $gitState.revision
  gitBranch = $gitState.branch
  server = $Server
  user = $User
  remoteDir = $RemoteDir
  publicUrl = "http://${Server}:3000/"
  webPayloads = $webEvidence.payloads
  deterministicBuild = $webEvidence.determinism
  webEntrySha256 = $webEvidence.primarySha256
  webSizeBytes = $webEvidence.sizeBytes
  localManifestPath = $localManifestPath
  startedAtUtc = $publishStartedAt.ToString('o')
  finishedAtUtc = $publishFinishedAt.ToString('o')
  durationSeconds = [math]::Round(($publishFinishedAt - $publishStartedAt).TotalSeconds, 3)
  releaseTransition = $deploymentTransition.releaseTransition
  deploymentTransitionPath = $deploymentTransitionPath
  deployedAcceptancePath = $deploymentTransition.candidateManifestPath
  miniGameRelease = $false
}
Write-FruitDefenseJson -Value $publishManifest -Path $publishManifestPath

Write-Host "FRUIT_DEFENSE_ONLINE_PUBLISH_OK target=ordinary-webgl url=http://${Server}:3000/ manifest=$publishManifestPath"
