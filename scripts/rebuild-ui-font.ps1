param(
  [string]$SourceUrl = 'https://raw.githubusercontent.com/google/fonts/2894aab31764f10f29c421bdfd2340d3b382d384/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf',
  [string]$OutputPath = 'Assets/Resources/Fonts/NotoSansSC-UI.ttf',
  [string]$FontToolsVersion = '4.63.0'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$resolvedOutput = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputPath))
$fontRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Assets/Resources/Fonts'))
if (-not $resolvedOutput.StartsWith($fontRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
  throw "Font output must stay under $fontRoot"
}

$characters = [Collections.Generic.SortedSet[int]]::new()
foreach ($codePoint in 32..126) { $null = $characters.Add($codePoint) }
foreach ($codePoint in @(0x2605, 0x2665, 0x2600, 0x2663, 0x00D7, 0x00B7, 0x2713,
    0x2026, 0xFF1A, 0xFF0C, 0x3002, 0xFF01, 0xFF1F, 0xFF08, 0xFF09, 0x3010, 0x3011, 0x300A, 0x300B)) {
  $null = $characters.Add($codePoint)
}

$sourceFiles = @(
  Get-ChildItem (Join-Path $projectRoot 'Assets/Scripts') -Recurse -File -Filter '*.cs'
  Get-ChildItem (Join-Path $projectRoot 'Assets/Resources/Content') -Recurse -File -Filter '*.json'
)
foreach ($file in $sourceFiles) {
  $text = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
  foreach ($character in $text.ToCharArray()) {
    $codePoint = [int]$character
    if ($codePoint -gt 126) { $null = $characters.Add($codePoint) }
  }
}

$unicodeSpec = (($characters | ForEach-Object { 'U+{0:X4}' -f $_ }) -join ',')
$toolsRoot = Join-Path $projectRoot 'Library/FontTools'
$temporaryRoot = Join-Path $env:TEMP "fruit-defense-font-$([Guid]::NewGuid().ToString('N'))"
$sourceFont = Join-Path $temporaryRoot 'NotoSansSC.ttf'
$generatedFont = Join-Path $temporaryRoot 'NotoSansSC-UI.ttf'
New-Item -ItemType Directory -Force -Path $temporaryRoot | Out-Null

try {
  if (-not (Test-Path -LiteralPath (Join-Path $toolsRoot 'fontTools'))) {
    New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null
    & python -m pip install --disable-pip-version-check --no-warn-script-location --target $toolsRoot "fonttools==$FontToolsVersion"
    if ($LASTEXITCODE -ne 0) { throw 'fontTools installation failed.' }
  }

  Invoke-WebRequest -Uri $SourceUrl -OutFile $sourceFont
  if ((Get-Item -LiteralPath $sourceFont).Length -lt 10000000) {
    throw 'Downloaded Noto Sans SC source font is unexpectedly small.'
  }

  $previousPythonPath = $env:PYTHONPATH
  try {
    $env:PYTHONPATH = $toolsRoot
    & python -m fontTools.subset $sourceFont `
      "--unicodes=$unicodeSpec" `
      "--output-file=$generatedFont" `
      '--layout-features=*' `
      '--glyph-names' `
      '--symbol-cmap' `
      '--legacy-cmap' `
      '--notdef-glyph' `
      '--notdef-outline' `
      '--recommended-glyphs' `
      '--name-IDs=*' `
      '--name-legacy' `
      '--name-languages=*' `
      '--drop-tables+=DSIG'
    if ($LASTEXITCODE -ne 0) { throw 'Noto Sans SC subsetting failed.' }
  }
  finally {
    $env:PYTHONPATH = $previousPythonPath
  }

  if ((Get-Item -LiteralPath $generatedFont).Length -lt 100000) {
    throw 'Generated UI font is unexpectedly small.'
  }
  Move-Item -LiteralPath $generatedFont -Destination $resolvedOutput -Force
  Write-Host "FRUIT_DEFENSE_UI_FONT_OK glyphs=$($characters.Count) bytes=$((Get-Item -LiteralPath $resolvedOutput).Length)"
}
finally {
  if (Test-Path -LiteralPath $temporaryRoot) {
    Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
  }
}
