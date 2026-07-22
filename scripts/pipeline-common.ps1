Set-StrictMode -Version 2.0

$script:FruitDefenseUnityVersion = '6000.3.19f1'
$script:FruitDefensePipelineMutexName = 'Local\FruitDefenseUnityPipeline'

function Assert-ProjectUnityVersion {
  param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot
  )

  $versionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
  if (-not (Test-Path -LiteralPath $versionFile)) {
    throw "Unity project version file not found: $versionFile"
  }

  $versionLine = Get-Content -LiteralPath $versionFile -Encoding UTF8 |
    Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
    Select-Object -First 1
  if (-not $versionLine -or $versionLine -notmatch '^m_EditorVersion:\s*(.+)$') {
    throw "Unity project version is unreadable: $versionFile"
  }

  $actualVersion = $Matches[1].Trim()
  if ($actualVersion -ne $script:FruitDefenseUnityVersion) {
    throw "Unity project version mismatch: expected $script:FruitDefenseUnityVersion, found $actualVersion"
  }

  return $actualVersion
}

function Assert-UnityEnvironment {
  param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$UnityPath,
    [Parameter(Mandatory = $true)][string[]]$Targets
  )

  $version = Assert-ProjectUnityVersion -ProjectRoot $ProjectRoot
  if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity editor not found: $UnityPath"
  }

  if ($UnityPath -notmatch [regex]::Escape($version)) {
    throw "Unity editor path does not identify required version ${version}: $UnityPath"
  }

  $editorRoot = Split-Path -Parent $UnityPath
  $playbackRoot = Join-Path $editorRoot 'Data\PlaybackEngines'
  if ($Targets -contains 'Web') {
    $webModule = Join-Path $playbackRoot 'WebGLSupport'
    if (-not (Test-Path -LiteralPath $webModule -PathType Container)) {
      throw "Unity WebGL Build Support is not installed: $webModule"
    }
  }
  if ($Targets -contains 'PC') {
    $windowsModule = Join-Path $playbackRoot 'WindowsStandaloneSupport'
    if (-not (Test-Path -LiteralPath $windowsModule -PathType Container)) {
      throw "Unity Windows Build Support is not installed: $windowsModule"
    }
  }

  return $version
}

function Enter-FruitDefensePipelineLock {
  $mutex = New-Object System.Threading.Mutex($false, $script:FruitDefensePipelineMutexName)
  $acquired = $false
  try {
    $acquired = $mutex.WaitOne(0)
  }
  catch [System.Threading.AbandonedMutexException] {
    $acquired = $true
  }

  if (-not $acquired) {
    $mutex.Dispose()
    throw 'Another FruitDefense Unity pipeline is already running.'
  }

  return $mutex
}

function Exit-FruitDefensePipelineLock {
  param(
    [Parameter(Mandatory = $true)][System.Threading.Mutex]$Mutex
  )

  try {
    $Mutex.ReleaseMutex()
  }
  finally {
    $Mutex.Dispose()
  }
}

function Invoke-FruitDefenseUnityBatch {
  param(
    [Parameter(Mandatory = $true)][string]$UnityPath,
    [Parameter(Mandatory = $true)][string[]]$Arguments,
    [Parameter(Mandatory = $true)][string]$LogPath,
    [Parameter(Mandatory = $true)][string]$SuccessPattern,
    [Parameter(Mandatory = $true)][string]$StepName
  )

  $logDirectory = Split-Path -Parent $LogPath
  if (-not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
  }
  Remove-Item -LiteralPath $LogPath -Force -ErrorAction SilentlyContinue

  Write-Host "[$StepName] starting"
  $startedAt = [DateTimeOffset]::UtcNow
  $process = Start-Process -FilePath $UnityPath -ArgumentList $Arguments -PassThru -WindowStyle Hidden
  $process.WaitForExit()
  $finishedAt = [DateTimeOffset]::UtcNow

  if ($process.ExitCode -ne 0) {
    throw "$StepName failed with Unity exit code $($process.ExitCode). See $LogPath"
  }
  if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
    throw "$StepName did not create its log: $LogPath"
  }
  if (-not (Select-String -LiteralPath $LogPath -Pattern $SuccessPattern -Quiet)) {
    throw "$StepName did not emit expected marker '$SuccessPattern'. See $LogPath"
  }

  Write-Host "[$StepName] passed"
  return [pscustomobject]@{
    step = $StepName
    logPath = $LogPath
    startedAtUtc = $startedAt.ToString('o')
    finishedAtUtc = $finishedAt.ToString('o')
    durationSeconds = [math]::Round(($finishedAt - $startedAt).TotalSeconds, 3)
    exitCode = $process.ExitCode
    successPattern = $SuccessPattern
  }
}

function Get-FruitDefenseGitState {
  param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot
  )

  $revision = (& git -C $ProjectRoot rev-parse HEAD 2>$null)
  if ($LASTEXITCODE -ne 0 -or -not $revision) {
    throw "Unable to resolve Git revision for $ProjectRoot"
  }
  $branch = (& git -C $ProjectRoot branch --show-current 2>$null)
  if ($LASTEXITCODE -ne 0 -or -not $branch) {
    throw "Unable to resolve Git branch for $ProjectRoot"
  }
  $statusLines = @(& git -C $ProjectRoot status --porcelain --untracked-files=all 2>$null)
  if ($LASTEXITCODE -ne 0) {
    throw "Unable to resolve Git working-tree status for $ProjectRoot"
  }

  return [pscustomobject]@{
    revision = ([string]$revision).Trim()
    branch = ([string]$branch).Trim()
    dirty = $statusLines.Count -gt 0
    status = @($statusLines)
  }
}

function Get-FruitDefenseDirectorySize {
  param(
    [Parameter(Mandatory = $true)][string]$Path
  )

  if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
    throw "Artifact directory not found: $Path"
  }
  $measurement = Get-ChildItem -LiteralPath $Path -Recurse -File | Measure-Object -Property Length -Sum
  return [int64]$measurement.Sum
}

function Get-FruitDefenseFileHash {
  param(
    [Parameter(Mandatory = $true)][string]$Path
  )

  if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Artifact file not found: $Path"
  }
  return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Write-FruitDefenseJson {
  param(
    [Parameter(Mandatory = $true)]$Value,
    [Parameter(Mandatory = $true)][string]$Path
  )

  $directory = Split-Path -Parent $Path
  if (-not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
  }
  $json = $Value | ConvertTo-Json -Depth 12
  Set-Content -LiteralPath $Path -Value $json -Encoding UTF8
}
