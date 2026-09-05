[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$promotionScript = Join-Path $PSScriptRoot 'promote-oper.ps1'
$powerShellPath = (Get-Command powershell.exe -ErrorAction Stop).Source
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("fruit-defense-oper-promotion-{0}" -f [Guid]::NewGuid().ToString('N'))
$mainRoot = Join-Path $testRoot 'main'
$operRoot = Join-Path $testRoot 'oper'
$remoteRoot = Join-Path $testRoot 'remote.git'

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

function Invoke-Promotion {
  param(
    [switch]$ExecutePromotion
  )

  $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $mainRoot 'scripts\promote-oper.ps1'))
  if ($ExecutePromotion) {
    $arguments += '-Execute'
  }
  $previousErrorActionPreference = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    $output = @(& $powerShellPath @arguments 2>&1)
    $exitCode = $LASTEXITCODE
  }
  finally {
    $ErrorActionPreference = $previousErrorActionPreference
  }
  return [pscustomobject]@{
    exitCode = $exitCode
    text = (($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
  }
}

function Assert-Equal {
  param(
    [Parameter(Mandatory = $true)]$Actual,
    [Parameter(Mandatory = $true)]$Expected,
    [Parameter(Mandatory = $true)][string]$Message
  )

  if ($Actual -ne $Expected) {
    throw "$Message Expected '$Expected', got '$Actual'."
  }
}

function Assert-Match {
  param(
    [Parameter(Mandatory = $true)][string]$Text,
    [Parameter(Mandatory = $true)][string]$Pattern,
    [Parameter(Mandatory = $true)][string]$Message
  )

  if ($Text -notmatch $Pattern) {
    throw "$Message Pattern '$Pattern' was not found.$([Environment]::NewLine)$Text"
  }
}

function Assert-Failed {
  param(
    [Parameter(Mandatory = $true)][pscustomobject]$Result,
    [Parameter(Mandatory = $true)][string]$Message
  )

  if ($Result.exitCode -eq 0) {
    throw "$Message Command unexpectedly succeeded.$([Environment]::NewLine)$($Result.text)"
  }
  if ($Result.text -match 'FRUIT_DEFENSE_OPER_PROMOTION_OK') {
    throw "$Message Failure output incorrectly reported promotion success."
  }
}

function Get-RemoteOperRevision {
  $line = Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('ls-remote', '--exit-code', 'origin', 'refs/heads/oper')
  return (($line -split '\s+')[0]).Trim()
}

try {
  if (-not (Test-Path -LiteralPath $promotionScript -PathType Leaf)) {
    throw "Promotion script not found: $promotionScript"
  }

  New-Item -ItemType Directory -Path $testRoot, $mainRoot -Force | Out-Null
  Invoke-GitText -WorkingDirectory $testRoot -Arguments @('init', '--bare', $remoteRoot) | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('init', '-b', 'main') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('config', 'user.name', 'FruitDefense Promotion Test') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('config', 'user.email', 'promotion-test@example.invalid') | Out-Null

  New-Item -ItemType Directory -Path (Join-Path $mainRoot 'scripts') -Force | Out-Null
  Copy-Item -LiteralPath $promotionScript -Destination (Join-Path $mainRoot 'scripts\promote-oper.ps1')
  Set-Content -LiteralPath (Join-Path $mainRoot 'release.txt') -Value 'base' -Encoding UTF8
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('add', '.') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('commit', '-m', 'base') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('remote', 'add', 'origin', $remoteRoot) | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('push', '-u', 'origin', 'main') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('branch', 'oper') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('push', '-u', 'origin', 'oper') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('worktree', 'add', $operRoot, 'oper') | Out-Null
  Invoke-GitText -WorkingDirectory $operRoot -Arguments @('config', 'user.name', 'FruitDefense Promotion Test') | Out-Null
  Invoke-GitText -WorkingDirectory $operRoot -Arguments @('config', 'user.email', 'promotion-test@example.invalid') | Out-Null

  Set-Content -LiteralPath (Join-Path $mainRoot 'release.txt') -Value 'candidate-1' -Encoding UTF8
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('add', 'release.txt') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('commit', '-m', 'candidate 1') | Out-Null
  $candidateOne = Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('rev-parse', 'HEAD')
  Set-Content -LiteralPath (Join-Path $mainRoot 'uncommitted.txt') -Value 'excluded' -Encoding UTF8

  $operBeforePlan = Invoke-GitText -WorkingDirectory $operRoot -Arguments @('rev-parse', 'HEAD')
  $remoteBeforePlan = Get-RemoteOperRevision
  $planResult = Invoke-Promotion
  Assert-Equal -Actual $planResult.exitCode -Expected 0 -Message 'Plan mode failed.'
  Assert-Match -Text $planResult.text -Pattern 'FRUIT_DEFENSE_OPER_PROMOTION_PLAN_OK' -Message 'Plan marker is missing.'
  Assert-Match -Text $planResult.text -Pattern '"sourceDirty"\s*:\s*true' -Message 'Dirty source was not reported.'
  Assert-Match -Text $planResult.text -Pattern '"uncommittedChangesIncluded"\s*:\s*false' -Message 'Uncommitted exclusion was not reported.'
  Assert-Equal -Actual (Invoke-GitText -WorkingDirectory $operRoot -Arguments @('rev-parse', 'HEAD')) -Expected $operBeforePlan -Message 'Plan mode changed local oper.'
  Assert-Equal -Actual (Get-RemoteOperRevision) -Expected $remoteBeforePlan -Message 'Plan mode changed remote oper.'

  $executeResult = Invoke-Promotion -ExecutePromotion
  Assert-Equal -Actual $executeResult.exitCode -Expected 0 -Message 'Eligible promotion failed.'
  Assert-Match -Text $executeResult.text -Pattern 'Uncommitted files are excluded' -Message 'Dirty-source execution warning is missing.'
  Assert-Match -Text $executeResult.text -Pattern 'FRUIT_DEFENSE_OPER_PROMOTION_OK' -Message 'Promotion success marker is missing.'
  Assert-Equal -Actual (Invoke-GitText -WorkingDirectory $operRoot -Arguments @('rev-parse', 'HEAD')) -Expected $candidateOne -Message 'Local oper did not reach candidate 1.'
  Assert-Equal -Actual (Get-RemoteOperRevision) -Expected $candidateOne -Message 'Remote oper did not reach candidate 1.'

  $idempotentResult = Invoke-Promotion -ExecutePromotion
  Assert-Equal -Actual $idempotentResult.exitCode -Expected 0 -Message 'Idempotent promotion failed.'
  Assert-Match -Text $idempotentResult.text -Pattern 'FRUIT_DEFENSE_OPER_PROMOTION_OK' -Message 'Idempotent success marker is missing.'
  Assert-Equal -Actual (Get-RemoteOperRevision) -Expected $candidateOne -Message 'Idempotent execution changed the selected revision.'

  Set-Content -LiteralPath (Join-Path $mainRoot 'release.txt') -Value 'candidate-2' -Encoding UTF8
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('add', 'release.txt') | Out-Null
  Invoke-GitText -WorkingDirectory $mainRoot -Arguments @('commit', '-m', 'candidate 2') | Out-Null
  Set-Content -LiteralPath (Join-Path $operRoot 'target-dirty.txt') -Value 'block promotion' -Encoding UTF8
  $dirtyTargetResult = Invoke-Promotion -ExecutePromotion
  Assert-Failed -Result $dirtyTargetResult -Message 'Dirty target gate failed.'
  Assert-Match -Text $dirtyTargetResult.text -Pattern "requires a clean 'oper' worktree" -Message 'Dirty target failure is unclear.'
  Assert-Equal -Actual (Invoke-GitText -WorkingDirectory $operRoot -Arguments @('rev-parse', 'HEAD')) -Expected $candidateOne -Message 'Dirty target rejection changed local oper.'
  Assert-Equal -Actual (Get-RemoteOperRevision) -Expected $candidateOne -Message 'Dirty target rejection changed remote oper.'

  Remove-Item -LiteralPath (Join-Path $operRoot 'target-dirty.txt') -Force
  Set-Content -LiteralPath (Join-Path $operRoot 'oper-only.txt') -Value 'divergent' -Encoding UTF8
  Invoke-GitText -WorkingDirectory $operRoot -Arguments @('add', 'oper-only.txt') | Out-Null
  Invoke-GitText -WorkingDirectory $operRoot -Arguments @('commit', '-m', 'oper-only divergence') | Out-Null
  $divergentOper = Invoke-GitText -WorkingDirectory $operRoot -Arguments @('rev-parse', 'HEAD')
  $divergentResult = Invoke-Promotion -ExecutePromotion
  Assert-Failed -Result $divergentResult -Message 'Non-fast-forward gate failed.'
  Assert-Match -Text $divergentResult.text -Pattern "local 'oper' revision is not an ancestor" -Message 'Non-fast-forward failure is unclear.'
  Assert-Equal -Actual (Invoke-GitText -WorkingDirectory $operRoot -Arguments @('rev-parse', 'HEAD')) -Expected $divergentOper -Message 'Non-fast-forward rejection changed local oper.'
  Assert-Equal -Actual (Get-RemoteOperRevision) -Expected $candidateOne -Message 'Non-fast-forward rejection changed remote oper.'

  Write-Host 'FRUIT_DEFENSE_OPER_PROMOTION_TESTS_OK'
}
finally {
  if (Test-Path -LiteralPath $testRoot) {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $safePrefix = "fruit-defense-oper-promotion-"
    if (-not $resolvedTestRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not ([IO.Path]::GetFileName($resolvedTestRoot)).StartsWith($safePrefix, [StringComparison]::Ordinal)) {
      throw "Refusing to remove unexpected test path: $resolvedTestRoot"
    }
    Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
  }
}
