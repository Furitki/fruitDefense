[CmdletBinding()]
param(
  [switch]$Execute
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceBranch = 'main'
$releaseBranch = 'oper'
$remoteName = 'origin'
$remoteReleaseRef = "refs/remotes/$remoteName/$releaseBranch"

function Invoke-GitText {
  param(
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string[]]$Arguments
  )

  $previousErrorActionPreference = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    $output = @(& git -C $WorkingDirectory @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
  }
  finally {
    $ErrorActionPreference = $previousErrorActionPreference
  }
  if ($exitCode -ne 0) {
    $detail = ($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
    throw "git $($Arguments -join ' ') failed with exit code $exitCode.$([Environment]::NewLine)$detail"
  }

  return (($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine).Trim()
}

function Invoke-GitCommand {
  param(
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string[]]$Arguments
  )

  $previousErrorActionPreference = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    $output = @(& git -C $WorkingDirectory @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
  }
  finally {
    $ErrorActionPreference = $previousErrorActionPreference
  }
  foreach ($line in $output) {
    Write-Host ([string]$line)
  }
  if ($exitCode -ne 0) {
    throw "git $($Arguments -join ' ') failed with exit code $exitCode."
  }
}

function Get-GitTextOrNull {
  param(
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string[]]$Arguments
  )

  $previousErrorActionPreference = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    $output = @(& git -C $WorkingDirectory @Arguments 2>$null)
    $exitCode = $LASTEXITCODE
  }
  finally {
    $ErrorActionPreference = $previousErrorActionPreference
  }
  if ($exitCode -ne 0) {
    return $null
  }

  return (($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine).Trim()
}

function Test-GitAncestor {
  param(
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string]$Ancestor,
    [Parameter(Mandatory = $true)][string]$Descendant
  )

  $previousErrorActionPreference = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    & git -C $WorkingDirectory merge-base --is-ancestor $Ancestor $Descendant 2>$null
    $exitCode = $LASTEXITCODE
  }
  finally {
    $ErrorActionPreference = $previousErrorActionPreference
  }
  if ($exitCode -eq 0) {
    return $true
  }
  if ($exitCode -eq 1) {
    return $false
  }

  throw "git merge-base --is-ancestor failed with exit code $exitCode."
}

function Get-GitWorktrees {
  param(
    [Parameter(Mandatory = $true)][string]$WorkingDirectory
  )

  $worktreeText = Invoke-GitText -WorkingDirectory $WorkingDirectory -Arguments @('worktree', 'list', '--porcelain')
  $lines = @($worktreeText -split "`r?`n")
  $records = @()
  $current = $null

  foreach ($line in $lines) {
    if ($line.StartsWith('worktree ', [StringComparison]::Ordinal)) {
      if ($null -ne $current) {
        $records += [pscustomobject]$current
      }
      $current = [ordered]@{
        path = $line.Substring('worktree '.Length)
        branch = $null
      }
      continue
    }

    if ($null -ne $current -and $line.StartsWith('branch ', [StringComparison]::Ordinal)) {
      $current.branch = $line.Substring('branch '.Length)
    }
  }

  if ($null -ne $current) {
    $records += [pscustomobject]$current
  }

  return $records
}

function Get-PromotionState {
  param(
    [Parameter(Mandatory = $true)][string]$RepositoryRoot
  )

  $currentBranch = Invoke-GitText -WorkingDirectory $RepositoryRoot -Arguments @('branch', '--show-current')
  $sourceRevision = Invoke-GitText -WorkingDirectory $RepositoryRoot -Arguments @('rev-parse', 'HEAD')
  $sourceStatus = Invoke-GitText -WorkingDirectory $RepositoryRoot -Arguments @('status', '--porcelain=v1', '--untracked-files=all')
  $sourceDirty = -not [string]::IsNullOrWhiteSpace($sourceStatus)

  $releaseWorktrees = @(Get-GitWorktrees -WorkingDirectory $RepositoryRoot | Where-Object {
    $_.branch -eq "refs/heads/$releaseBranch"
  })
  $releaseWorktree = $null
  $releaseRevision = $null
  $releaseDirty = $null
  if ($releaseWorktrees.Count -eq 1) {
    $releaseWorktree = [string]$releaseWorktrees[0].path
    $releaseRevision = Invoke-GitText -WorkingDirectory $releaseWorktree -Arguments @('rev-parse', 'HEAD')
    $releaseStatus = Invoke-GitText -WorkingDirectory $releaseWorktree -Arguments @('status', '--porcelain=v1', '--untracked-files=all')
    $releaseDirty = -not [string]::IsNullOrWhiteSpace($releaseStatus)
  }

  $remoteRevision = Get-GitTextOrNull -WorkingDirectory $RepositoryRoot -Arguments @('rev-parse', '--verify', $remoteReleaseRef)
  $localContained = $false
  if ($null -ne $releaseRevision) {
    $localContained = Test-GitAncestor -WorkingDirectory $RepositoryRoot -Ancestor $releaseRevision -Descendant $sourceRevision
  }
  $remoteContained = $false
  if ($null -ne $remoteRevision) {
    $remoteContained = Test-GitAncestor -WorkingDirectory $RepositoryRoot -Ancestor $remoteRevision -Descendant $sourceRevision
  }

  $blockers = [System.Collections.Generic.List[string]]::new()
  if ($currentBranch -ne $sourceBranch) {
    $blockers.Add("source checkout must be on '$sourceBranch'; current branch is '$currentBranch'")
  }
  if ($releaseWorktrees.Count -ne 1) {
    $blockers.Add("expected exactly one worktree on '$releaseBranch'; found $($releaseWorktrees.Count)")
  }
  elseif ([bool]$releaseDirty) {
    $blockers.Add("release worktree is dirty: $releaseWorktree")
  }
  if ($null -eq $remoteRevision) {
    $blockers.Add("remote release ref is unavailable: $remoteReleaseRef")
  }
  if ($null -ne $releaseRevision -and -not $localContained) {
    $blockers.Add("local '$releaseBranch' revision is not an ancestor of source revision $sourceRevision")
  }
  if ($null -ne $remoteRevision -and -not $remoteContained) {
    $blockers.Add("'$remoteReleaseRef' is not an ancestor of source revision $sourceRevision")
  }

  return [pscustomobject][ordered]@{
    pipeline = 'oper-release-promotion'
    mode = if ($Execute) { 'execute' } else { 'plan' }
    sourceBranch = $sourceBranch
    currentBranch = $currentBranch
    sourceRevision = $sourceRevision
    sourceDirty = $sourceDirty
    uncommittedChangesIncluded = $false
    releaseBranch = $releaseBranch
    releaseWorktree = $releaseWorktree
    releaseRevision = $releaseRevision
    releaseDirty = $releaseDirty
    remote = $remoteName
    remoteReleaseRevision = $remoteRevision
    localReleaseContainedBySource = $localContained
    remoteReleaseContainedBySource = $remoteContained
    ready = $blockers.Count -eq 0
    blockers = @($blockers)
    nextStep = if ($Execute) {
      'fast-forward oper, push origin/oper, and verify the exact remote revision'
    }
    else {
      'rerun with -Execute to authorize branch promotion; no build or publication is performed'
    }
  }
}

function Write-PromotionPlan {
  param(
    [Parameter(Mandatory = $true)][pscustomobject]$State
  )

  Write-Host ($State | ConvertTo-Json -Depth 6)
}

Get-Command git -ErrorAction Stop | Out-Null
$resolvedRoot = Invoke-GitText -WorkingDirectory $projectRoot -Arguments @('rev-parse', '--show-toplevel')
if (-not [string]::Equals(
    [IO.Path]::GetFullPath($resolvedRoot).TrimEnd([char[]]@('\', '/')),
    [IO.Path]::GetFullPath($projectRoot).TrimEnd([char[]]@('\', '/')),
    [StringComparison]::OrdinalIgnoreCase)) {
  throw "Promotion script must live under the source checkout root. Expected '$projectRoot', resolved '$resolvedRoot'."
}

$state = Get-PromotionState -RepositoryRoot $projectRoot
if (-not $Execute) {
  Write-PromotionPlan -State $state
  Write-Host "FRUIT_DEFENSE_OPER_PROMOTION_PLAN_OK revision=$($state.sourceRevision) ready=$(([string]$state.ready).ToLowerInvariant())"
  return
}

if ($state.currentBranch -ne $sourceBranch) {
  throw "Promotion must execute from '$sourceBranch'; current branch is '$($state.currentBranch)'."
}
if ([string]::IsNullOrWhiteSpace([string]$state.releaseWorktree)) {
  throw "Promotion requires exactly one worktree checked out on '$releaseBranch'."
}
if ([bool]$state.releaseDirty) {
  throw "Promotion requires a clean '$releaseBranch' worktree: $($state.releaseWorktree)"
}

Invoke-GitCommand `
  -WorkingDirectory $projectRoot `
  -Arguments @('fetch', $remoteName, "+refs/heads/${releaseBranch}:${remoteReleaseRef}")

$state = Get-PromotionState -RepositoryRoot $projectRoot
Write-PromotionPlan -State $state
if (-not $state.ready) {
  throw "Promotion gates failed:$([Environment]::NewLine)- $($state.blockers -join "$([Environment]::NewLine)- ")"
}
if ($state.sourceDirty) {
  Write-Warning "The '$sourceBranch' checkout is dirty. Uncommitted files are excluded; promoting commit $($state.sourceRevision) only."
}

$selectedRevision = [string]$state.sourceRevision
$releaseWorktree = [string]$state.releaseWorktree
Invoke-GitCommand -WorkingDirectory $releaseWorktree -Arguments @('merge', '--ff-only', $selectedRevision)

$localRevision = Invoke-GitText -WorkingDirectory $releaseWorktree -Arguments @('rev-parse', 'HEAD')
if ($localRevision -ne $selectedRevision) {
  throw "Local '$releaseBranch' resolved to $localRevision after merge; expected $selectedRevision."
}
$releaseStatusAfterMerge = Invoke-GitText -WorkingDirectory $releaseWorktree -Arguments @('status', '--porcelain=v1', '--untracked-files=all')
if (-not [string]::IsNullOrWhiteSpace($releaseStatusAfterMerge)) {
  throw "Release worktree became dirty after fast-forward: $releaseWorktree"
}

Invoke-GitCommand `
  -WorkingDirectory $releaseWorktree `
  -Arguments @('push', $remoteName, "${releaseBranch}:refs/heads/${releaseBranch}")

$remoteLine = Invoke-GitText `
  -WorkingDirectory $releaseWorktree `
  -Arguments @('ls-remote', '--exit-code', $remoteName, "refs/heads/$releaseBranch")
$remoteRevisionAfterPush = (($remoteLine -split '\s+')[0]).Trim()
if ($remoteRevisionAfterPush -ne $selectedRevision) {
  throw "Remote '$remoteName/$releaseBranch' resolved to $remoteRevisionAfterPush after push; expected $selectedRevision."
}

Write-Host "FRUIT_DEFENSE_OPER_PROMOTION_OK revision=$selectedRevision local=$localRevision remote=$remoteRevisionAfterPush"
