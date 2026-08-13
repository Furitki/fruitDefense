param(
    [string]$OutputPath = (Join-Path $PSScriptRoot `
        '../Assets/LayeredTerrain/GrassSoil/Square/Topology/DualGridSwastikaReference.png'),
    [int]$TileSize = 256,
    [int]$GridLineWidth = 4,
    [string]$TerrainColor = '#FF4779',
    [string]$GridColor = '#3F3D88',
    [string]$BackgroundColor = '#FFFFFF'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($TileSize -le 0 -or ($TileSize % 2) -ne 0) {
    throw 'TileSize must be a positive even integer so every corner owns one exact quadrant.'
}

if ($GridLineWidth -le 0 -or $GridLineWidth -ge $TileSize `
    -or ($GridLineWidth % 2) -ne 0) {
    throw 'GridLineWidth must be a positive even integer smaller than TileSize.'
}

# FruitDefense Dual-Grid corner bits: NW=1, NE=2, SE=4, SW=8.
# The visual order is the canonical 16-mask swastika layout. It is a permutation
# of every mask from 0 through 15, so each valid corner state appears exactly once.
$maskRows = @(
    @(8, 6, 13, 12),
    @(5, 14, 15, 11),
    @(2, 3, 7, 9),
    @(0, 4, 10, 1)
)

$orderedMasks = @($maskRows | ForEach-Object { $_ } | Sort-Object)
if (($orderedMasks -join ',') -ne ((0..15) -join ',')) {
    throw 'The swastika layout must contain every Dual-Grid mask exactly once.'
}

Add-Type -AssemblyName System.Drawing

$atlasSize = 4 * $TileSize
$halfTile = [int]($TileSize / 2)
$halfLine = [int]($GridLineWidth / 2)
$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$temporaryPath = $outputFullPath + '.tmp.png'

$bitmap = [System.Drawing.Bitmap]::new(
    $atlasSize,
    $atlasSize,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$terrainBrush = $null
$gridBrush = $null

try {
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::None
    $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml($BackgroundColor))

    $terrainBrush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.ColorTranslator]::FromHtml($TerrainColor))
    $gridBrush = [System.Drawing.SolidBrush]::new(
        [System.Drawing.ColorTranslator]::FromHtml($GridColor))

    for ($row = 0; $row -lt 4; $row++) {
        for ($column = 0; $column -lt 4; $column++) {
            $mask = [int]$maskRows[$row][$column]
            $originX = $column * $TileSize
            $originY = $row * $TileSize

            $quadrants = @(
                @{ Bit = 1; X = $originX;             Y = $originY },
                @{ Bit = 2; X = $originX + $halfTile; Y = $originY },
                @{ Bit = 4; X = $originX + $halfTile; Y = $originY + $halfTile },
                @{ Bit = 8; X = $originX;             Y = $originY + $halfTile }
            )

            foreach ($quadrant in $quadrants) {
                if (($mask -band $quadrant.Bit) -ne 0) {
                    $graphics.FillRectangle(
                        $terrainBrush,
                        [int]$quadrant.X,
                        [int]$quadrant.Y,
                        $halfTile,
                        $halfTile)
                }
            }
        }
    }

    # Draw exact cell boundaries last. Filled rectangles are used instead of
    # stroked paths so the reference contains no antialiasing or subpixel drift.
    $graphics.FillRectangle($gridBrush, 0, 0, $GridLineWidth, $atlasSize)
    $graphics.FillRectangle(
        $gridBrush, $atlasSize - $GridLineWidth, 0, $GridLineWidth, $atlasSize)
    $graphics.FillRectangle($gridBrush, 0, 0, $atlasSize, $GridLineWidth)
    $graphics.FillRectangle(
        $gridBrush, 0, $atlasSize - $GridLineWidth, $atlasSize, $GridLineWidth)

    for ($boundary = 1; $boundary -lt 4; $boundary++) {
        $coordinate = $boundary * $TileSize - $halfLine
        $graphics.FillRectangle($gridBrush, $coordinate, 0, $GridLineWidth, $atlasSize)
        $graphics.FillRectangle($gridBrush, 0, $coordinate, $atlasSize, $GridLineWidth)
    }

    $bitmap.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    if ($terrainBrush) { $terrainBrush.Dispose() }
    if ($gridBrush) { $gridBrush.Dispose() }
    $graphics.Dispose()
    $bitmap.Dispose()
}

Move-Item -LiteralPath $temporaryPath -Destination $outputFullPath -Force

$hash = (Get-FileHash -LiteralPath $outputFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output ('DUAL_GRID_SWASTIKA_REFERENCE_OK path={0} size={1}x{1} sha256={2}' `
    -f $outputFullPath, $atlasSize, $hash)
