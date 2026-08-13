param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Prepare', 'Finalize', 'Package', 'Repackage', 'Validate', 'Overview')]
    [string]$Stage,

    [Parameter(Mandatory = $true)]
    [string]$OutputRoot,

    [string]$Profile,
    [string]$RawImage,
    [string]$TopologyGuide,
    [string]$ModelCall,
    [string]$TrustedMaskRoot,
    [string]$SourceRoot,
    [string[]]$Candidate,
    [int]$Columns = 4,
    [string]$PythonPath = 'python',
    [switch]$AllowMissingModelCall,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$core = Join-Path $PSScriptRoot 'dual_grid_tile_pipeline.py'
if (-not (Test-Path -LiteralPath $core -PathType Leaf)) {
    throw "Dual-Grid pipeline core not found: $core"
}

$arguments = @($core, $Stage.ToLowerInvariant(), '--output-root', $OutputRoot)
if ($Stage -in @('Prepare', 'Finalize', 'Package', 'Repackage')) {
    if (-not $Profile) { throw "-$Stage requires -Profile." }
    $arguments += @('--profile', $Profile)
}
if ($Stage -eq 'Repackage') {
    if (-not $SourceRoot) { throw '-Repackage requires -SourceRoot.' }
    $arguments += @('--source-root', $SourceRoot)
}
if ($Stage -eq 'Overview') {
    if (-not $Candidate -or $Candidate.Count -eq 0) {
        throw '-Overview requires one or more -Candidate "ID=PATH" values.'
    }
    foreach ($item in $Candidate) { $arguments += @('--candidate', $item) }
    $arguments += @('--columns', $Columns)
}
if ($Stage -eq 'Finalize') {
    if (-not $RawImage) { throw '-Finalize requires -RawImage.' }
    $arguments += @('--raw-image', $RawImage)
    if ($TopologyGuide) { $arguments += @('--topology-guide', $TopologyGuide) }
    if ($ModelCall) { $arguments += @('--model-call', $ModelCall) }
    if ($TrustedMaskRoot) { $arguments += @('--trusted-mask-root', $TrustedMaskRoot) }
    if ($AllowMissingModelCall) { $arguments += '--allow-missing-model-call' }
}
if ($Force) { $arguments += '--force' }

& $PythonPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Dual-Grid pipeline stage '$Stage' failed with exit code $LASTEXITCODE."
}

Write-Output "DUAL_GRID_PIPELINE_OK stage=$Stage output=$OutputRoot"
