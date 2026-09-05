param(
  [string]$ReadingSourceUrl = 'https://raw.githubusercontent.com/google/fonts/2894aab31764f10f29c421bdfd2340d3b382d384/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf',
  [string]$ReadingSourceSha256 = 'a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da',
  [string]$DisplayArchiveUrl = 'https://github.com/atelier-anchor/smiley-sans/releases/download/v2.0.1/smiley-sans-v2.0.1.zip',
  [string]$DisplayArchiveSha256 = '299c0be6c960ae37361762eca76f7d0cd516615435bb96c0d4b98a1e70178a07',
  [string]$DisplaySourceSha256 = 'b447d7e781f08bc95c4c9f23ba71ed2b8ebb639aa7184485c71c4ca5afcd25c4',
  [string]$ReadingOutputPath = 'Assets/Resources/Fonts/NotoSansSC-Reading-400.ttf',
  [string]$DisplayOutputPath = 'Assets/Resources/Fonts/FruitDefense-OrchardDisplay-400.ttf',
  [string]$FontToolsVersion = '4.63.0'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$fontRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets/Resources/Fonts'))

function Resolve-FontOutput([string]$relativePath) {
  $resolved = [IO.Path]::GetFullPath((Join-Path $projectRoot $relativePath))
  if (-not $resolved.StartsWith(
      $fontRoot + [IO.Path]::DirectorySeparatorChar,
      [StringComparison]::OrdinalIgnoreCase)) {
    throw "Font output must stay under $fontRoot"
  }
  return $resolved
}

function Invoke-CheckedPython([string[]]$arguments, [string]$failureMessage) {
  & python @arguments
  if ($LASTEXITCODE -ne 0) { throw $failureMessage }
}

function Invoke-PinnedDownload([string]$url, [string]$outputPath) {
  foreach ($attempt in 1..4) {
    try {
      Invoke-WebRequest -Uri $url -OutFile $outputPath
      return
    }
    catch {
      if ($attempt -eq 4) { throw }
      Start-Sleep -Seconds 1
    }
  }
}

$resolvedReadingOutput = Resolve-FontOutput $ReadingOutputPath
$resolvedDisplayOutput = Resolve-FontOutput $DisplayOutputPath
if ([string]::Equals($resolvedReadingOutput, $resolvedDisplayOutput,
    [StringComparison]::OrdinalIgnoreCase)) {
  throw 'Reading and display font outputs must be distinct files.'
}

$characters = [Collections.Generic.SortedSet[int]]::new()
foreach ($codePoint in 32..126) { $null = $characters.Add($codePoint) }

$glyphAuthorityPath = Join-Path $projectRoot `
  'Assets/Editor/Tools/RuntimeUiChineseGlyphCoverage.cs'
$glyphAuthority = Get-Content -LiteralPath $glyphAuthorityPath -Raw -Encoding UTF8
$initializer = [regex]::Match($glyphAuthority,
  '(?s)private const string FixedRequiredGlyphs\s*=\s*(?<expression>.*?);')
if (-not $initializer.Success) {
  throw 'Runtime UI glyph authority could not be parsed.'
}
$literalMatches = [regex]::Matches(
  $initializer.Groups['expression'].Value, '"(?<value>[^"\\]*)"')
if ($literalMatches.Count -eq 0) {
  throw 'Runtime UI glyph authority does not contain literal glyph groups.'
}
foreach ($literal in $literalMatches) {
  foreach ($character in $literal.Groups['value'].Value.ToCharArray()) {
    $null = $characters.Add([int]$character)
  }
}

# The canonical bundled outgame JSON owns all player-visible catalog names and
# descriptions. Keep this traversal aligned with
# RuntimeUiChineseGlyphCoverage.ReadBundledOutgameVisibleCopy so font generation
# and Unity validation close over the same content instead of a copied glyph list.
$outgameContentPath = Join-Path $projectRoot `
  'Assets/Resources/Content/outgame-content-bundled.v1.json'
if (-not (Test-Path -LiteralPath $outgameContentPath)) {
  throw "Bundled outgame content is missing: $outgameContentPath"
}
$outgameContent = Get-Content -LiteralPath $outgameContentPath -Raw -Encoding UTF8 |
  ConvertFrom-Json
$visibleCopy = @()
foreach ($definition in $outgameContent.items) {
  $visibleCopy += [string]$definition.displayName
  $visibleCopy += [string]$definition.description
}
foreach ($definition in $outgameContent.activities) {
  $visibleCopy += [string]$definition.displayName
  $visibleCopy += [string]$definition.description
}
foreach ($definition in $outgameContent.growthEquipment) {
  $visibleCopy += [string]$definition.displayName
  $visibleCopy += [string]$definition.description
}
foreach ($definition in $outgameContent.cultivationNodes) {
  $visibleCopy += [string]$definition.displayName
  $visibleCopy += [string]$definition.description
}
foreach ($definition in $outgameContent.growthPolicies) {
  $visibleCopy += [string]$definition.displayName
}
foreach ($copy in $visibleCopy) {
  foreach ($character in $copy.ToCharArray()) {
    if (-not [char]::IsControl($character)) {
      $null = $characters.Add([int]$character)
    }
  }
}

$unicodeSpec = (($characters | ForEach-Object { 'U+{0:X4}' -f $_ }) -join ',')
$toolsRoot = Join-Path $projectRoot "Library/FontTools/$FontToolsVersion"
$temporaryRoot = Join-Path $env:TEMP `
  "fruit-defense-font-$([Guid]::NewGuid().ToString('N'))"
$readingSourceFont = Join-Path $temporaryRoot 'NotoSansSC-variable.ttf'
$displayArchive = Join-Path $temporaryRoot 'smiley-sans-v2.0.1.zip'
$displaySourceRoot = Join-Path $temporaryRoot 'smiley-sans-v2.0.1'
$displaySourceFont = Join-Path $displaySourceRoot 'SmileySans-Oblique.ttf'
New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null

try {
  if (-not (Test-Path -LiteralPath (Join-Path $toolsRoot 'fontTools'))) {
    New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null
    & python -m pip install --disable-pip-version-check --no-warn-script-location `
      --target $toolsRoot "fonttools==$FontToolsVersion"
    if ($LASTEXITCODE -ne 0) { throw 'fontTools installation failed.' }
  }

  Invoke-PinnedDownload $ReadingSourceUrl $readingSourceFont
  if ((Get-Item -LiteralPath $readingSourceFont).Length -ne 17772300) {
    throw 'Downloaded Noto Sans SC source font has an unexpected byte length.'
  }
  $actualReadingSourceHash = (Get-FileHash -LiteralPath $readingSourceFont -Algorithm SHA256).Hash.ToLowerInvariant()
  if (-not [string]::Equals($actualReadingSourceHash, $ReadingSourceSha256,
      [StringComparison]::Ordinal)) {
    throw "Downloaded Noto Sans SC source hash mismatch: $actualReadingSourceHash"
  }

  Invoke-PinnedDownload $DisplayArchiveUrl $displayArchive
  if ((Get-Item -LiteralPath $displayArchive).Length -ne 5781344) {
    throw 'Downloaded Smiley Sans release archive has an unexpected byte length.'
  }
  $actualDisplayArchiveHash = (Get-FileHash -LiteralPath $displayArchive -Algorithm SHA256).Hash.ToLowerInvariant()
  if (-not [string]::Equals($actualDisplayArchiveHash, $DisplayArchiveSha256,
      [StringComparison]::Ordinal)) {
    throw "Downloaded Smiley Sans archive hash mismatch: $actualDisplayArchiveHash"
  }
  Expand-Archive -LiteralPath $displayArchive -DestinationPath $displaySourceRoot
  if ((Get-Item -LiteralPath $displaySourceFont).Length -ne 2629764) {
    throw 'Smiley Sans display source has an unexpected byte length.'
  }
  $actualDisplaySourceHash = (Get-FileHash -LiteralPath $displaySourceFont -Algorithm SHA256).Hash.ToLowerInvariant()
  if (-not [string]::Equals($actualDisplaySourceHash, $DisplaySourceSha256,
      [StringComparison]::Ordinal)) {
    throw "Smiley Sans display source hash mismatch: $actualDisplaySourceHash"
  }

  $previousPythonPath = $env:PYTHONPATH
  try {
    $env:PYTHONPATH = $toolsRoot
    $readingInstance = Join-Path $temporaryRoot 'reading-400-instance.ttf'
    $generatedReading = Join-Path $temporaryRoot 'reading-400-subset.ttf'
    Invoke-CheckedPython @(
      '-m', 'fontTools.varLib.instancer', $readingSourceFont,
      'wght=400', '--static', '--update-name-table',
      '--no-recalc-timestamp',
      '--output', $readingInstance
    ) 'Noto Sans SC weight 400 instancing failed.'

    $subsetInputs = @(
      @{ Source = $readingInstance; Generated = $generatedReading; Label = 'Noto Sans SC reading'; MinimumBytes = 100000 },
      @{ Source = $displaySourceFont; Generated = (Join-Path $temporaryRoot 'orchard-display-400-subset.ttf'); Label = 'Orchard display'; MinimumBytes = 50000 }
    )
    foreach ($subsetInput in $subsetInputs) {
      Invoke-CheckedPython @(
        '-m', 'fontTools.subset', $subsetInput.Source,
        "--unicodes=$unicodeSpec",
        "--output-file=$($subsetInput.Generated)",
        '--layout-features=*',
        '--glyph-names',
        '--symbol-cmap',
        '--legacy-cmap',
        '--notdef-glyph',
        '--notdef-outline',
        '--recommended-glyphs',
        '--name-IDs=*',
        '--name-legacy',
        '--name-languages=*',
        '--drop-tables+=DSIG',
        '--no-recalc-timestamp'
      ) "$($subsetInput.Label) subsetting failed."
      if ((Get-Item -LiteralPath $subsetInput.Generated).Length -lt $subsetInput.MinimumBytes) {
        throw "$($subsetInput.Label) UI font is unexpectedly small."
      }
    }

    $generatedDisplay = $subsetInputs[1].Generated
    Invoke-CheckedPython @(
      (Join-Path $PSScriptRoot 'rename-font-family.py'),
      $generatedDisplay,
      'Fruit Defense Orchard Display',
      'FruitDefenseOrchardDisplay-Regular'
    ) 'Orchard display reserved-family rename failed.'
    Move-Item -LiteralPath $generatedReading -Destination $resolvedReadingOutput -Force
    Move-Item -LiteralPath $generatedDisplay -Destination $resolvedDisplayOutput -Force
  }
  finally {
    $env:PYTHONPATH = $previousPythonPath
  }

  $readingHash = (Get-FileHash -LiteralPath $resolvedReadingOutput -Algorithm SHA256).Hash.ToLowerInvariant()
  $displayHash = (Get-FileHash -LiteralPath $resolvedDisplayOutput -Algorithm SHA256).Hash.ToLowerInvariant()
  $successMessage = 'FRUIT_DEFENSE_UI_FONTS_OK glyphs={0} readingBytes={1} ' +
    'readingSha256={2} displayBytes={3} displaySha256={4}'
  Write-Host ($successMessage -f `
    $characters.Count,
    (Get-Item -LiteralPath $resolvedReadingOutput).Length,
    $readingHash,
    (Get-Item -LiteralPath $resolvedDisplayOutput).Length,
    $displayHash)
}
finally {
  if (Test-Path -LiteralPath $temporaryRoot) {
    Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
  }
}
