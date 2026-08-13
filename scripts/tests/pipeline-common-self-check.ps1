$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$scriptsRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $scriptsRoot 'pipeline-common.ps1')

function New-SyntheticPayloadMap {
  param([string]$DataSha256 = ('b' * 64))

  return [ordered]@{
    loader = [ordered]@{ role = 'loader'; fileName = 'WebGL.loader.js'; version = ('a' * 12); sha256 = ('a' * 64); sizeBytes = 10 }
    data = [ordered]@{ role = 'data'; fileName = 'WebGL.data.unityweb'; version = $DataSha256.Substring(0, 12); sha256 = $DataSha256; sizeBytes = 20 }
    framework = [ordered]@{ role = 'framework'; fileName = 'WebGL.framework.js.unityweb'; version = ('c' * 12); sha256 = ('c' * 64); sizeBytes = 30 }
    wasm = [ordered]@{ role = 'wasm'; fileName = 'WebGL.wasm.unityweb'; version = ('d' * 12); sha256 = ('d' * 64); sizeBytes = 40 }
  }
}

$first = New-SyntheticPayloadMap
$matching = New-SyntheticPayloadMap
$matchResult = Compare-FruitDefenseWebPayloadEvidence -First $first -Second $matching
if ($matchResult.result -ne 'pass' -or $matchResult.differences.Count -ne 0) {
  throw 'Matching Web payload evidence did not pass.'
}

$mismatching = New-SyntheticPayloadMap -DataSha256 ('e' * 64)
$mismatching.data.sizeBytes = 21
$mismatchResult = Compare-FruitDefenseWebPayloadEvidence -First $first -Second $mismatching
if ($mismatchResult.result -ne 'fail' -or $mismatchResult.differences.Count -ne 1 -or
    $mismatchResult.differences[0].role -ne 'data') {
  throw 'Mismatching Web payload evidence did not identify the data role.'
}

Write-Host 'FRUIT_DEFENSE_PIPELINE_COMMON_SELF_CHECK_OK'
