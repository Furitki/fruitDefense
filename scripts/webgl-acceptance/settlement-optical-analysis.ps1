# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Get-SettlementOutcomeInkCounts {
  param([string]$Path)

  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $xMin = [Math]::Max(0, [int][Math]::Floor($settlementOutcomeInkRegion.xMin))
    $yMin = [Math]::Max(0, [int][Math]::Floor($settlementOutcomeInkRegion.yMin))
    $xMax = [Math]::Min($bitmap.Width, [int][Math]::Ceiling($settlementOutcomeInkRegion.xMax))
    $yMax = [Math]::Min($bitmap.Height, [int][Math]::Ceiling($settlementOutcomeInkRegion.yMax))
    $fillPixels = 0
    $outlinePixels = 0
    for ($y = $yMin; $y -lt $yMax; $y++) {
      for ($x = $xMin; $x -lt $xMax; $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        if (Test-SettlementOutcomeFillPixel -Pixel $pixel) { $fillPixels++ }
        if (Test-SettlementOutcomeOutlinePixel -Pixel $pixel) { $outlinePixels++ }
      }
    }
    return [ordered]@{
      region = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
      fillPixels = $fillPixels
      outlinePixels = $outlinePixels
    }
  }
  finally { $bitmap.Dispose() }
}

function Get-SettlementOutcomeRevealTelemetry {
  $json = Invoke-JavaScript -Expression @'
JSON.stringify({
  state: window.fruitDefenseSettlementOutcomeRevealState ?? null,
  identityState: window.fruitDefenseAcceptanceIdentity?.settlementOutcomeRevealState ?? null,
  appRoute: window.fruitDefenseAppRoute ?? -1,
  identityRoute: window.fruitDefenseAcceptanceIdentity?.route ?? -1,
  identityRouteName: window.fruitDefenseAcceptanceIdentity?.routeName ?? null,
  identitySessionId: window.fruitDefenseAcceptanceIdentity?.sessionId ?? null,
  history: window.fruitDefenseSettlementOutcomeRevealHistory ?? []
})
'@
  if ([string]::IsNullOrWhiteSpace([string]$json)) { return $null }
  return $json | ConvertFrom-Json
}

function Wait-SettlementOutcomeRevealState {
  param(
    [ValidateSet('hidden', 'appearing', 'stable')]
    [string]$State
  )

  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $telemetry = $null
  do {
    $telemetry = Get-SettlementOutcomeRevealTelemetry
    $history = if ($null -eq $telemetry) { @() } else { @($telemetry.history) }
    $lastHistory = if ($history.Count -eq 0) { $null } else { $history[-1] }
    if ($null -ne $telemetry -and [string]$telemetry.state -ceq $State -and
        [string]$telemetry.identityState -ceq $State -and
        [int]$telemetry.appRoute -eq 2 -and [int]$telemetry.identityRoute -eq 2 -and
        [string]$telemetry.identityRouteName -ceq 'settlement' -and
        -not [string]::IsNullOrWhiteSpace([string]$telemetry.identitySessionId) -and
        $null -ne $lastHistory -and [string]$lastHistory.state -ceq $State -and
        [int]$lastHistory.route -eq 2 -and
        [string]$lastHistory.sessionId -ceq [string]$telemetry.identitySessionId) {
      return $telemetry
    }
    Start-Sleep -Milliseconds 10
  } while ((Get-Date) -lt $deadline)
  throw (
    "Settlement outcome reveal did not reach '$State': " +
    ($telemetry | ConvertTo-Json -Depth 8 -Compress))
}

function Get-PausedModalOpticalEvidence {
  param([string]$Path)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $title = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseTitleInkRegion -Mask 'title-ink'
    $hintIcon = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseHintIconRegion -Mask 'hint-icon'
    $hintCopy = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseHintCopyRegion -Mask 'hint-copy'
    $primary = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseContinueRect -Mask 'primary-surface'
    $danger = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $pauseRestartRect -Mask 'danger-surface'

    $titleOwnerCenterY = ($pauseTitleRect.yMin + $pauseTitleRect.yMax) * .5
    $titleCenterDeltaLogical = [Math]::Abs(
      $title.centroid.y - $titleOwnerCenterY) / $referenceScale
    $hintCenterDeltaLogical = [Math]::Abs(
      $hintIcon.centroid.y - $hintCopy.centroid.y) / $referenceScale
    $hintUnion = [ordered]@{
      xMin = [Math]::Min($hintIcon.bounds.xMin, $hintCopy.bounds.xMin)
      yMin = [Math]::Min($hintIcon.bounds.yMin, $hintCopy.bounds.yMin)
      xMax = [Math]::Max($hintIcon.bounds.xMax, $hintCopy.bounds.xMax)
      yMax = [Math]::Max($hintIcon.bounds.yMax, $hintCopy.bounds.yMax)
    }
    $hintOwnerCenterX = ($pauseHintRect.xMin + $pauseHintRect.xMax) * .5
    $hintOwnerCenterY = ($pauseHintRect.yMin + $pauseHintRect.yMax) * .5
    $hintGroupCenterDeltaLogical = [ordered]@{
      x = [Math]::Abs((($hintUnion.xMin + $hintUnion.xMax) * .5) -
        $hintOwnerCenterX) / $referenceScale
      y = [Math]::Abs((($hintUnion.yMin + $hintUnion.yMax) * .5) -
        $hintOwnerCenterY) / $referenceScale
    }
    $primaryLocal = [ordered]@{
      left = $primary.bounds.xMin - $pauseContinueRect.xMin
      top = $primary.bounds.yMin - $pauseContinueRect.yMin
      right = $pauseContinueRect.xMax - $primary.bounds.xMax
      bottom = $pauseContinueRect.yMax - $primary.bounds.yMax
    }
    $dangerLocal = [ordered]@{
      left = $danger.bounds.xMin - $pauseRestartRect.xMin
      top = $danger.bounds.yMin - $pauseRestartRect.yMin
      right = $pauseRestartRect.xMax - $danger.bounds.xMax
      bottom = $pauseRestartRect.yMax - $danger.bounds.yMax
    }
    $pairedMaximumEdgeDelta = @(
      [Math]::Abs($primaryLocal.left - $dangerLocal.left),
      [Math]::Abs($primaryLocal.top - $dangerLocal.top),
      [Math]::Abs($primaryLocal.right - $dangerLocal.right),
      [Math]::Abs($primaryLocal.bottom - $dangerLocal.bottom)
    ) | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum

    if ($titleCenterDeltaLogical -gt 2.0) {
      throw "Paused title final-raster center delta exceeds 2 logical points: $titleCenterDeltaLogical"
    }
    if ($hintCenterDeltaLogical -gt 2.0 -or
        $hintGroupCenterDeltaLogical.x -gt 2.0 -or
        $hintGroupCenterDeltaLogical.y -gt 2.0) {
      throw (
        'Paused hint final-raster optical alignment exceeds 2 logical points: ' +
        "iconCopyY=$hintCenterDeltaLogical group=" +
        ($hintGroupCenterDeltaLogical | ConvertTo-Json -Compress))
    }
    # Deep leaf and terracotta anti-alias into the warm modal at different
    # luminances, so their hue masks may diverge by two capture pixels while
    # the authoritative button rectangles and source alpha geometry remain equal.
    if ($pairedMaximumEdgeDelta -gt 2 -or
        [Math]::Abs($primary.bounds.width - $danger.bounds.width) -gt 2 -or
        [Math]::Abs($primary.bounds.height - $danger.bounds.height) -gt 2) {
      throw (
        'Paused paired action final-raster envelopes differ by more than two capture pixels: ' +
        "edge=$pairedMaximumEdgeDelta primary=" +
        ($primary.bounds | ConvertTo-Json -Compress) + ' danger=' +
        ($danger.bounds | ConvertTo-Json -Compress))
    }

    return [ordered]@{
      thresholds = [ordered]@{
        titleCenterLogical = 2.0
        hintCenterLogical = 2.0
        pairedActionCapturePixels = 2
      }
      title = $title
      titleOwner = $pauseTitleRect
      titleCenterDeltaLogical = $titleCenterDeltaLogical
      hintIcon = $hintIcon
      hintCopy = $hintCopy
      hintOwner = $pauseHintRect
      hintUnion = $hintUnion
      hintIconCopyCenterDeltaLogical = $hintCenterDeltaLogical
      hintGroupCenterDeltaLogical = $hintGroupCenterDeltaLogical
      primarySurface = $primary
      dangerSurface = $danger
      primaryLocalInsets = $primaryLocal
      dangerLocalInsets = $dangerLocal
      pairedMaximumEdgeDeltaCapturePixels = $pairedMaximumEdgeDelta
    }
  }
  finally { $bitmap.Dispose() }
}

function Get-SettlementMetricBorderEvidence {
  param([object]$Bitmap, [object]$Region)
  $xMin = [Math]::Max(0, [int][Math]::Floor($Region.xMin))
  $yMin = [Math]::Max(0, [int][Math]::Floor($Region.yMin))
  $xMax = [Math]::Min($Bitmap.Width, [int][Math]::Ceiling($Region.xMax))
  $yMax = [Math]::Min($Bitmap.Height, [int][Math]::Ceiling($Region.yMax))
  $band = [Math]::Max(2, [int][Math]::Ceiling(7 * $referenceScale))
  $maxHorizontalRun = 0
  $maxVerticalRun = 0

  foreach ($range in @(
    [ordered]@{ yMin = $yMin; yMax = [Math]::Min($yMax, $yMin + $band) },
    [ordered]@{ yMin = [Math]::Max($yMin, $yMax - $band); yMax = $yMax })) {
    for ($y = $range.yMin; $y -lt $range.yMax; $y++) {
      $run = 0
      for ($x = $xMin; $x -lt $xMax; $x++) {
        $pixel = $Bitmap.GetPixel($x, $y)
        $border = $pixel.A -gt 200 -and $pixel.R -ge 70 -and
          $pixel.R -lt 195 -and $pixel.G -lt 160 -and $pixel.B -lt 125 -and
          $pixel.R -gt ($pixel.G + 8)
        if ($border) {
          $run++
          $maxHorizontalRun = [Math]::Max($maxHorizontalRun, $run)
        }
        else { $run = 0 }
      }
    }
  }

  foreach ($range in @(
    [ordered]@{ xMin = $xMin; xMax = [Math]::Min($xMax, $xMin + $band) },
    [ordered]@{ xMin = [Math]::Max($xMin, $xMax - $band); xMax = $xMax })) {
    for ($x = $range.xMin; $x -lt $range.xMax; $x++) {
      $run = 0
      for ($y = $yMin; $y -lt $yMax; $y++) {
        $pixel = $Bitmap.GetPixel($x, $y)
        $border = $pixel.A -gt 200 -and $pixel.R -ge 70 -and
          $pixel.R -lt 195 -and $pixel.G -lt 160 -and $pixel.B -lt 125 -and
          $pixel.R -gt ($pixel.G + 8)
        if ($border) {
          $run++
          $maxVerticalRun = [Math]::Max($maxVerticalRun, $run)
        }
        else { $run = 0 }
      }
    }
  }

  $width = [Math]::Max(1, $xMax - $xMin)
  $height = [Math]::Max(1, $yMax - $yMin)
  return [ordered]@{
    region = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
    edgeBandPixels = $band
    maximumHorizontalBrownRunPixels = $maxHorizontalRun
    maximumVerticalBrownRunPixels = $maxVerticalRun
    maximumHorizontalRunFraction = $maxHorizontalRun / [double]$width
    maximumVerticalRunFraction = $maxVerticalRun / [double]$height
  }
}

function Get-SettlementOpticalEvidence {
  param([string]$Path, [string]$ReferencePath)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  $referenceBitmap = [Drawing.Bitmap]::FromFile($ReferencePath)
  try {
    $bannerSearchRegion = [ordered]@{
      xMin = $settlementResultBannerRect.xMin - 4 * $referenceScale
      yMin = $settlementResultBannerRect.yMin - 4 * $referenceScale
      xMax = $settlementResultBannerRect.xMax + 4 * $referenceScale
      yMax = $settlementResultBannerRect.yMax + 4 * $referenceScale
    }
    $banner = Get-ColorMaskEvidence -Bitmap $bitmap `
      -Region $bannerSearchRegion -Mask 'result-banner'
    $outcome = Get-SettlementOutcomeInkEvidence -Bitmap $bitmap `
      -ReferenceBitmap $referenceBitmap `
      -Region $settlementOutcomeInkRegion
    $finalInk = $outcome.finalInk
    $padding = [ordered]@{
      left = $finalInk.bounds.xMin - $banner.bounds.xMin
      top = $finalInk.bounds.yMin - $banner.bounds.yMin
      right = $banner.bounds.xMax - $finalInk.bounds.xMax
      bottom = $banner.bounds.yMax - $finalInk.bounds.yMax
    }
    $minimumPadding = [Math]::Max(1, [int][Math]::Floor(8 * $referenceScale))
    $minimumInkHeight = [Math]::Max(1,
      [int][Math]::Floor(28 * $referenceScale))
    $maximumInkHeight = [Math]::Max($minimumInkHeight,
      [int][Math]::Ceiling(32 * $referenceScale))
    $expectedOutlineThickness = [Math]::Max(1,
      [int][Math]::Round(2.0 * $referenceScale, 0,
        [MidpointRounding]::ToEven))
    $maximumPaddingImbalance = [Math]::Max(1,
      [int][Math]::Ceiling(2 * $referenceScale))
    $verticalOccupancy = $finalInk.bounds.height / [double]$banner.bounds.height
    if ($outcome.outline.maximumConnectedThicknessCapturePixels -ne
        $expectedOutlineThickness -or
        $outcome.outline.rings.Count -ne $expectedOutlineThickness) {
      throw (
        'Settlement outcome exact connected outline evidence is inconsistent: ' +
        ($outcome.outline | ConvertTo-Json -Depth 8 -Compress))
    }
    if ($outcome.finalInk.connectedOutlinePixelsCovered -ne
        $outcome.outline.candidatePixels -or
        $outcome.finalInk.outlineCandidatePixelsCovered -ne
        $outcome.outline.candidatePixels -or
        $outcome.outline.pixels -ne $outcome.outline.candidatePixels) {
      throw 'Settlement outcome has disconnected or uncovered outline candidates.'
    }
    if ($finalInk.bounds.height -lt $minimumInkHeight -or
        $finalInk.bounds.height -gt $maximumInkHeight) {
      throw (
        "Settlement outcome final ink height is outside 28-32 reference pixels: actual=$($finalInk.bounds.height) " +
        "allowed=$minimumInkHeight-$maximumInkHeight")
    }
    if ($verticalOccupancy -lt .52 -or $verticalOccupancy -gt .64) {
      throw (
        'Settlement outcome final ink does not occupy 52%-64% of the live banner: ' +
        $verticalOccupancy)
    }
    if ($padding.left -lt $minimumPadding -or
        $padding.top -lt $minimumPadding -or
        $padding.right -lt $minimumPadding -or
        $padding.bottom -lt $minimumPadding) {
      throw (
        'Settlement outcome glyphs are not contained by the banner optical pixels: ' +
        ($padding | ConvertTo-Json -Compress))
    }
    $paddingImbalance = [Math]::Abs($padding.top - $padding.bottom)
    if ($paddingImbalance -gt $maximumPaddingImbalance) {
      throw (
        "Settlement outcome top/bottom padding is imbalanced: actual=$paddingImbalance " +
        "maximum=$maximumPaddingImbalance")
    }

    $metricRows = @()
    foreach ($rect in $settlementMetricRects) {
      $row = Get-SettlementMetricBorderEvidence -Bitmap $bitmap -Region $rect
      if ($row.maximumHorizontalRunFraction -gt 0.45 -or
          $row.maximumVerticalRunFraction -gt 0.45) {
        throw (
          'Settlement read-only metric row still has a closed border signature: ' +
          ($row | ConvertTo-Json -Compress))
      }
      $metricRows += $row
    }

    return [ordered]@{
      thresholds = [ordered]@{
        minimumOutcomePaddingCapturePixels = $minimumPadding
        expectedOutlineThicknessCapturePixels = $expectedOutlineThickness
        minimumOutcomeInkHeightCapturePixels = $minimumInkHeight
        maximumOutcomeInkHeightCapturePixels = $maximumInkHeight
        minimumOutcomeVerticalOccupancy = .52
        maximumOutcomeVerticalOccupancy = .64
        maximumTopBottomPaddingImbalanceCapturePixels = $maximumPaddingImbalance
        maximumMetricBorderRunFraction = 0.45
      }
      banner = $banner
      expectedBannerOpticalRect = $settlementResultBannerRect
      outcome = $outcome
      finalInkVerticalOccupancy = $verticalOccupancy
      outcomePaddingCapturePixels = $padding
      topBottomPaddingImbalanceCapturePixels = $paddingImbalance
      metricRows = $metricRows
    }
  }
  finally {
    $bitmap.Dispose()
    $referenceBitmap.Dispose()
  }
}
