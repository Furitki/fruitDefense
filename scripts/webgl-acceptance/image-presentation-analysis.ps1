function Get-ImagePixelSample {
  param([string]$Path, [int]$X, [int]$Y)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $pixel = $bitmap.GetPixel(
      [Math]::Min([Math]::Max(0, $X), $bitmap.Width - 1),
      [Math]::Min([Math]::Max(0, $Y), $bitmap.Height - 1))
    return [ordered]@{ r = [int]$pixel.R; g = [int]$pixel.G; b = [int]$pixel.B; a = [int]$pixel.A }
  }
  finally { $bitmap.Dispose() }
}

function Get-ShellSurfaceSamples {
  param([string]$Path)
  $safeBaseY = [Math]::Floor($Height - $SafeBottom - 72 * $referenceScale)
  $samples = [ordered]@{
    safeBase = Get-ImagePixelSample -Path $Path -X ([Math]::Floor($Width / 2)) -Y $safeBaseY
  }
  if ($SafeTop -gt 0) {
    $samples.edge = Get-ImagePixelSample -Path $Path `
      -X ([Math]::Floor($Width / 2)) -Y ([Math]::Floor($SafeTop / 2))
  }
  elseif ($SafeBottom -gt 0) {
    $samples.edge = Get-ImagePixelSample -Path $Path `
      -X ([Math]::Floor($Width / 2)) -Y ($Height - [Math]::Ceiling($SafeBottom / 2))
  }
  return $samples
}

function Test-ShellSurfaceSamples {
  param([object]$Samples)
  foreach ($name in @('safeBase', 'edge')) {
    if (-not $Samples.Contains($name)) { continue }
    $sample = $Samples[$name]
    if ([int]$sample.a -lt 250) { return $false }
    $luma = Get-SrgbRelativeLuminance -R ([int]$sample.r) -G ([int]$sample.g) -B ([int]$sample.b)
    if ($luma -lt 0.08) { return $false }
  }
  return $true
}

function Get-SrgbRelativeLuminance {
  param([int]$R, [int]$G, [int]$B)
  $linear = foreach ($channel in @($R, $G, $B)) {
    $value = $channel / 255.0
    if ($value -le 0.04045) { $value / 12.92 }
    else { [Math]::Pow(($value + 0.055) / 1.055, 2.4) }
  }
  return 0.2126 * $linear[0] + 0.7152 * $linear[1] + 0.0722 * $linear[2]
}

function Get-ActionContentContrast {
  param(
    [string]$Path,
    [object]$Rect,
    [double]$MinimumContrast = 4.5,
    [double]$ContentLeft = 0.24,
    [double]$ContentRight = 0.76,
    [double]$ContentTop = 0.18,
    [double]$ContentBottom = 0.72,
    [ValidateSet('Auto', 'LightOnDark', 'DarkOnLight')]
    [string]$Polarity = 'Auto'
  )
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    # Inspect only the content-bearing interior. Callers select a label-only or
    # icon-plus-label band while excluding borders and shallow surface highlights.
    $width = $Rect.xMax - $Rect.xMin
    $height = $Rect.yMax - $Rect.yMin
    $xMin = [Math]::Max(0, [Math]::Floor($Rect.xMin + $width * $ContentLeft))
    $xMax = [Math]::Min($bitmap.Width, [Math]::Ceiling($Rect.xMin + $width * $ContentRight))
    $yMin = [Math]::Max(0, [Math]::Floor($Rect.yMin + $height * $ContentTop))
    $yMax = [Math]::Min($bitmap.Height, [Math]::Ceiling($Rect.yMin + $height * $ContentBottom))
    $clusters = @{}
    for ($y = $yMin; $y -lt $yMax; $y++) {
      for ($x = $xMin; $x -lt $xMax; $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        if ($pixel.A -lt 250) { continue }
        $key = "$([Math]::Floor($pixel.R / 16.0)),$([Math]::Floor($pixel.G / 16.0)),$([Math]::Floor($pixel.B / 16.0))"
        if (-not $clusters.ContainsKey($key)) {
          $clusters[$key] = [ordered]@{ count = 0; r = 0L; g = 0L; b = 0L }
        }
        $cluster = $clusters[$key]
        $cluster.count++
        $cluster.r += $pixel.R
        $cluster.g += $pixel.G
        $cluster.b += $pixel.B
      }
    }
    if ($clusters.Count -lt 2) { throw "Action contrast sample has insufficient colors: $Path" }

    $colors = foreach ($entry in $clusters.GetEnumerator()) {
      $r = [int][Math]::Round($entry.Value.r / [double]$entry.Value.count)
      $g = [int][Math]::Round($entry.Value.g / [double]$entry.Value.count)
      $b = [int][Math]::Round($entry.Value.b / [double]$entry.Value.count)
      [pscustomobject]@{
        r = $r; g = $g; b = $b; count = [int]$entry.Value.count
        luminance = Get-SrgbRelativeLuminance -R $r -G $g -B $b
      }
    }
    $background = $colors | Sort-Object count -Descending | Select-Object -First 1
    $minimumSolidGlyphPixels = [Math]::Max(6,
      [Math]::Floor(($xMax - $xMin) * ($yMax - $yMin) * 0.0008))
    $foreground = $colors |
      Where-Object {
        if ($_.count -lt $minimumSolidGlyphPixels) { return $false }
        if ($Polarity -eq 'LightOnDark') {
          return $_.luminance -gt $background.luminance
        }
        if ($Polarity -eq 'DarkOnLight') {
          return $_.luminance -lt $background.luminance
        }
        return [Math]::Abs($_.luminance - $background.luminance) -gt .02
      } |
      ForEach-Object {
        $_ | Add-Member -NotePropertyName contrast -NotePropertyValue `
          (([Math]::Max($background.luminance, $_.luminance) + 0.05) / `
          ([Math]::Min($background.luminance, $_.luminance) + 0.05)) -PassThru
      } |
      Sort-Object `
        @{ Expression = 'contrast'; Descending = $true },
        @{ Expression = 'count'; Descending = $true } |
      Select-Object -First 1
    if ($null -eq $foreground) {
      throw "Action contrast sample has no solid content color for polarity ${Polarity}: $Path"
    }
    $ratio = ([Math]::Max($background.luminance, $foreground.luminance) + 0.05) /
      ([Math]::Min($background.luminance, $foreground.luminance) + 0.05)
    return [ordered]@{
      background = [ordered]@{
        r = $background.r; g = $background.g; b = $background.b; pixels = $background.count
      }
      foreground = [ordered]@{
        r = $foreground.r; g = $foreground.g; b = $foreground.b; pixels = $foreground.count
      }
      ratio = $ratio
      minimum = $MinimumContrast
      polarity = $Polarity
      passed = $ratio -ge $MinimumContrast
      sampleRect = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
    }
  }
  finally { $bitmap.Dispose() }
}

function Save-StableShellScreenshot {
  param([string]$Name)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $attempts = 0
  $consecutiveStableFrames = 0
  $previousStableLuma = $null
  do {
    $attempts++
    $path = Save-Screenshot -Name $Name
    $metrics = Get-ImageMetrics -Path $path
    $surfaceSamples = Get-ShellSurfaceSamples -Path $path
    $actionContrast = Get-ActionContentContrast `
      -Path $path -Rect $lobbyStartRect -Polarity LightOnDark
    $metrics['surfaceSamples'] = $surfaceSamples
    $metrics['actionContrast'] = $actionContrast
    $passesFrameGuards = $metrics.width -eq $Width -and $metrics.height -eq $Height -and
        (Test-StableFrameMetrics -Metrics $metrics) -and
        (Test-ShellSurfaceSamples -Samples $surfaceSamples) -and
        $actionContrast.passed
    if ($passesFrameGuards) {
      if ($null -ne $previousStableLuma -and
          [Math]::Abs([double]$metrics.averageLuma - [double]$previousStableLuma) -le 0.006) {
        $consecutiveStableFrames++
      }
      else {
        $consecutiveStableFrames = 1
      }
      $previousStableLuma = [double]$metrics.averageLuma
      if ($consecutiveStableFrames -ge 3) {
        $metrics['consecutiveStableFrames'] = $consecutiveStableFrames
        return [pscustomobject]@{ Path = $path; Metrics = $metrics; Attempts = $attempts }
      }
    }
    else {
      $consecutiveStableFrames = 0
      $previousStableLuma = $null
    }
    Start-Sleep -Milliseconds 250
  } while ((Get-Date) -lt $deadline)
  throw (
    "Sunny Orchard shell surface/contrast did not stabilize for '$Name': " +
    "$($metrics | ConvertTo-Json -Depth 6 -Compress)")
}

function Save-StableScreenshot {
  param([string]$Name, [bool]$RequireHud = $true)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $attempts = 0
  do {
    $attempts++
    $path = Save-Screenshot -Name $Name
    $metrics = Get-ImageMetrics -Path $path
    $dimensionsOk = $metrics.width -eq $Width -and $metrics.height -eq $Height
    $frameOk = Test-StableFrameMetrics -Metrics $metrics
    $hudOk = -not $RequireHud -or (
      $metrics.headerDarkPixels -ge $hudDarkPixelThreshold -and
      $metrics.headerLightPixels -ge $hudLightPixelThreshold)
    if ($dimensionsOk -and $frameOk -and $hudOk) {
      return [pscustomobject]@{ Path = $path; Metrics = $metrics }
    }
    Write-Warning (
      "Retrying unstable screenshot '$Name' attempt=$attempts " +
      "dimensions=$($metrics.width)x$($metrics.height) " +
      "invalid=$($metrics.invalidFraction) black=$($metrics.blackFraction) " +
      "nearBlack=$($metrics.nearBlackFraction) " +
      "maxNearBlackRun=$($metrics.maxNearBlackHorizontalRunFraction)")
    Start-Sleep -Milliseconds 500
  } while ((Get-Date) -lt $deadline)
  throw "Stable screenshot timed out: $Name metrics=$($metrics | ConvertTo-Json -Compress)"
}
