param(
  [ValidateSet('Web', 'PC', 'All')]
  [string]$Target = 'All',
  [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$projectRoot = Split-Path -Parent $PSScriptRoot
$commonScript = Join-Path $PSScriptRoot 'pipeline-common.ps1'
if (-not (Test-Path -LiteralPath $commonScript -PathType Leaf)) {
  throw "Pipeline helper not found: $commonScript"
}
. $commonScript

$requestedTargets = switch ($Target) {
  'Web' { @('Web') }
  'PC' { @('PC') }
  'All' { @('Web', 'PC') }
}

$unityVersion = Assert-UnityEnvironment `
  -ProjectRoot $projectRoot `
  -UnityPath $UnityPath `
  -Targets $requestedTargets

$lock = Enter-FruitDefensePipelineLock
try {
  $pipelineStartedAt = [DateTimeOffset]::UtcNow
  $gitState = Get-FruitDefenseGitState -ProjectRoot $projectRoot
  $logsRoot = Join-Path $projectRoot 'Logs'
  $manifestPath = Join-Path $projectRoot 'Builds\Pipeline\local-build-manifest.json'

  $p0Log = Join-Path $logsRoot 'pipeline-local-p0.log'
  $p0Args = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $projectRoot,
    '-executeMethod', 'FruitDefense.Editor.P0ValidationSuite.Run',
    '-logFile', $p0Log
  )
  $p0Evidence = Invoke-FruitDefenseUnityBatch `
    -UnityPath $UnityPath `
    -Arguments $p0Args `
    -LogPath $p0Log `
    -SuccessPattern 'FRUIT_DEFENSE_P0_RELEASE_GATE_OK' `
    -StepName 'P0 release gate'

  $targetEvidence = @()
  foreach ($buildTarget in $requestedTargets) {
    if ($buildTarget -eq 'Web') {
      $webLog = Join-Path $logsRoot 'pipeline-local-web.log'
      $webArgs = @(
        '-batchmode', '-nographics', '-quit',
        '-projectPath', $projectRoot,
        '-executeMethod', 'FruitDefense.Editor.WebBuild.Build',
        '-logFile', $webLog
      )
      $webStep = Invoke-FruitDefenseUnityBatch `
        -UnityPath $UnityPath `
        -Arguments $webArgs `
        -LogPath $webLog `
        -SuccessPattern 'FRUIT_DEFENSE_WEB_BUILD_OK' `
        -StepName 'WebGL build'

      $webOutput = Join-Path $projectRoot 'Builds\WebGL'
      $webEntry = Join-Path $webOutput 'index.html'
      $webMarker = Select-String -LiteralPath $webLog -Pattern 'FRUIT_DEFENSE_WEB_BUILD_OK' |
        Select-Object -Last 1
      if (-not $webMarker -or $webMarker.Line -notmatch 'version=([0-9a-f]{12})') {
        throw "WebGL build content version is missing from $webLog"
      }

      $targetEvidence += [pscustomobject]@{
        target = 'Web'
        outputPath = $webOutput
        primaryArtifact = $webEntry
        primarySha256 = Get-FruitDefenseFileHash -Path $webEntry
        sizeBytes = Get-FruitDefenseDirectorySize -Path $webOutput
        contentVersion = $Matches[1]
        codeAssemblySha256 = $null
        logPath = $webStep.logPath
        durationSeconds = $webStep.durationSeconds
      }
      continue
    }

    $pcOutput = Join-Path $projectRoot 'Builds\Windows'
    $pcExecutable = Join-Path $pcOutput 'FruitDefense.exe'
    $pcAssembly = Join-Path $pcOutput 'FruitDefense_Data\Managed\Assembly-CSharp.dll'
    $pcLog = Join-Path $logsRoot 'pipeline-local-pc.log'
    $pcArgs = @(
      '-batchmode', '-nographics', '-quit',
      '-projectPath', $projectRoot,
      '-buildWindows64Player', $pcExecutable,
      '-logFile', $pcLog
    )
    $pcStep = Invoke-FruitDefenseUnityBatch `
      -UnityPath $UnityPath `
      -Arguments $pcArgs `
      -LogPath $pcLog `
      -SuccessPattern 'Build Finished, Result: Success\.' `
      -StepName 'Windows 64-bit build'

    $targetEvidence += [pscustomobject]@{
      target = 'PC'
      outputPath = $pcOutput
      primaryArtifact = $pcExecutable
      primarySha256 = Get-FruitDefenseFileHash -Path $pcExecutable
      sizeBytes = Get-FruitDefenseDirectorySize -Path $pcOutput
      contentVersion = $null
      codeAssemblySha256 = Get-FruitDefenseFileHash -Path $pcAssembly
      logPath = $pcStep.logPath
      durationSeconds = $pcStep.durationSeconds
    }
  }

  $pipelineFinishedAt = [DateTimeOffset]::UtcNow
  $manifest = [ordered]@{
    schemaVersion = 1
    pipeline = 'local-build'
    requestedTarget = $Target
    unityVersion = $unityVersion
    gitRevision = $gitState.revision
    gitBranch = $gitState.branch
    dirtyBeforeBuild = $gitState.dirty
    startedAtUtc = $pipelineStartedAt.ToString('o')
    finishedAtUtc = $pipelineFinishedAt.ToString('o')
    durationSeconds = [math]::Round(($pipelineFinishedAt - $pipelineStartedAt).TotalSeconds, 3)
    p0 = $p0Evidence
    targets = @($targetEvidence)
  }
  Write-FruitDefenseJson -Value $manifest -Path $manifestPath

  Write-Host "FRUIT_DEFENSE_LOCAL_BUILD_PIPELINE_OK target=$Target manifest=$manifestPath"
}
finally {
  Exit-FruitDefensePipelineLock -Mutex $lock
}
