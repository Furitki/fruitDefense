# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Test-SettlementOutcomeFillPixel {
  param([object]$Pixel)
  return [Math]::Abs([int]$Pixel.R - 139) -le 18 -and
    [Math]::Abs([int]$Pixel.G - 94) -le 18 -and
    [Math]::Abs([int]$Pixel.B - 60) -le 18 -and $Pixel.A -gt 200
}

function Test-SettlementOutcomeOutlinePixel {
  param([object]$Pixel)
  return [Math]::Abs([int]$Pixel.R - 255) -le 12 -and
    [Math]::Abs([int]$Pixel.G - 246) -le 16 -and
    [Math]::Abs([int]$Pixel.B - 224) -le 18 -and $Pixel.A -gt 200
}

function Test-SettlementOutcomeFillSupportPixel {
  param([object]$Pixel)
  if ($Pixel.A -le 200) { return $false }

  # The fill is anti-aliased over the already-painted outline. Recognize that
  # finite blend segment, then keep only components connected to strict fill.
  $deltaR = 116.0
  $deltaG = 152.0
  $deltaB = 164.0
  $red = [double]$Pixel.R - 139.0
  $green = [double]$Pixel.G - 94.0
  $blue = [double]$Pixel.B - 60.0
  $denominator = $deltaR * $deltaR + $deltaG * $deltaG + $deltaB * $deltaB
  $projection = ($red * $deltaR + $green * $deltaG + $blue * $deltaB) /
    $denominator
  if ($projection -lt -0.15 -or $projection -gt 0.75) { return $false }

  $clamped = [Math]::Max(0.0, [Math]::Min(1.0, $projection))
  $residualR = $red - $clamped * $deltaR
  $residualG = $green - $clamped * $deltaG
  $residualB = $blue - $clamped * $deltaB
  return $residualR * $residualR + $residualG * $residualG +
    $residualB * $residualB -le 18.0 * 18.0
}

function Get-SettlementOutcomeInkEvidence {
  param([object]$Bitmap, [object]$ReferenceBitmap, [object]$Region)
  $xMin = [Math]::Max(0, [int][Math]::Floor($Region.xMin))
  $yMin = [Math]::Max(0, [int][Math]::Floor($Region.yMin))
  $xMax = [Math]::Min($Bitmap.Width, [int][Math]::Ceiling($Region.xMax))
  $yMax = [Math]::Min($Bitmap.Height, [int][Math]::Ceiling($Region.yMax))
  $sampleWidth = [Math]::Max(0, $xMax - $xMin)
  $sampleHeight = [Math]::Max(0, $yMax - $yMin)
  if ($sampleWidth -eq 0 -or $sampleHeight -eq 0) {
    throw 'Settlement outcome ink sample region is empty.'
  }
  if ($null -eq $ReferenceBitmap -or
      $ReferenceBitmap.Width -ne $Bitmap.Width -or
      $ReferenceBitmap.Height -ne $Bitmap.Height) {
    throw 'Settlement outcome final-ink reference must match the stable bitmap dimensions.'
  }

  $mapLength = $sampleWidth * $sampleHeight
  $fillCoreMap = [bool[]]::new($mapLength)
  $fillSupportCandidateMap = [bool[]]::new($mapLength)
  $outlineCandidateMap = [bool[]]::new($mapLength)
  $finalInkMap = [bool[]]::new($mapLength)
  $minimumFinalInkChannelDelta = 8
  $fillCorePixels = 0
  $outlineCandidatePixels = 0
  $finalInkPixels = 0
  $finalInkXMin = [int]::MaxValue
  $finalInkYMin = [int]::MaxValue
  $finalInkXMax = [int]::MinValue
  $finalInkYMax = [int]::MinValue
  $finalInkTouchesSampleBoundary = $false
  for ($y = $yMin; $y -lt $yMax; $y++) {
    for ($x = $xMin; $x -lt $xMax; $x++) {
      $pixel = $Bitmap.GetPixel($x, $y)
      $referencePixel = $ReferenceBitmap.GetPixel($x, $y)
      $index = (($y - $yMin) * $sampleWidth) + ($x - $xMin)
      if (Test-SettlementOutcomeFillPixel -Pixel $pixel) {
        $fillCoreMap[$index] = $true
        $fillCorePixels++
      }
      $fillSupportCandidateMap[$index] =
        Test-SettlementOutcomeFillSupportPixel -Pixel $pixel
      if (Test-SettlementOutcomeOutlinePixel -Pixel $pixel) {
        $outlineCandidateMap[$index] = $true
        $outlineCandidatePixels++
      }
      $maximumChannelDelta = @(
        [Math]::Abs([int]$pixel.R - [int]$referencePixel.R),
        [Math]::Abs([int]$pixel.G - [int]$referencePixel.G),
        [Math]::Abs([int]$pixel.B - [int]$referencePixel.B),
        [Math]::Abs([int]$pixel.A - [int]$referencePixel.A)
      ) | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
      if ($maximumChannelDelta -gt $minimumFinalInkChannelDelta) {
        $finalInkMap[$index] = $true
        $finalInkPixels++
        $finalInkXMin = [Math]::Min($finalInkXMin, $x)
        $finalInkYMin = [Math]::Min($finalInkYMin, $y)
        $finalInkXMax = [Math]::Max($finalInkXMax, $x + 1)
        $finalInkYMax = [Math]::Max($finalInkYMax, $y + 1)
        if ($x -eq $xMin -or $x -eq $xMax - 1 -or
            $y -eq $yMin -or $y -eq $yMax - 1) {
          $finalInkTouchesSampleBoundary = $true
        }
      }
    }
  }
  if ($fillCorePixels -eq 0) {
    throw 'Settlement outcome fill ink was not found in the final raster.'
  }
  if ($finalInkPixels -eq 0) {
    throw 'Settlement outcome final ink did not differ from its hidden reference.'
  }
  if ($finalInkTouchesSampleBoundary) {
    throw 'Settlement outcome independent final-ink mask touches the sample boundary.'
  }

  $fillSupportMap = [bool[]]::new($mapLength)
  $fillQueue = [System.Collections.Generic.Queue[int]]::new()
  for ($index = 0; $index -lt $mapLength; $index++) {
    if (-not $fillCoreMap[$index]) { continue }
    $fillSupportMap[$index] = $true
    $fillQueue.Enqueue($index)
  }
  while ($fillQueue.Count -gt 0) {
    $index = $fillQueue.Dequeue()
    $relativeY = [int][Math]::Floor($index / [double]$sampleWidth)
    $relativeX = $index % $sampleWidth
    for ($offsetY = -1; $offsetY -le 1; $offsetY++) {
      for ($offsetX = -1; $offsetX -le 1; $offsetX++) {
        if ($offsetX -eq 0 -and $offsetY -eq 0) { continue }
        $candidateX = $relativeX + $offsetX
        $candidateY = $relativeY + $offsetY
        if ($candidateX -lt 0 -or $candidateX -ge $sampleWidth -or
            $candidateY -lt 0 -or $candidateY -ge $sampleHeight) { continue }
        $candidateIndex = $candidateY * $sampleWidth + $candidateX
        if ($fillSupportMap[$candidateIndex] -or
            -not $fillSupportCandidateMap[$candidateIndex]) { continue }
        $fillSupportMap[$candidateIndex] = $true
        $fillQueue.Enqueue($candidateIndex)
      }
    }
  }

  $outlineMap = [bool[]]::new($mapLength)
  $outlineQueue = [System.Collections.Generic.Queue[int]]::new()
  for ($index = 0; $index -lt $mapLength; $index++) {
    if (-not $outlineCandidateMap[$index]) { continue }
    $relativeY = [int][Math]::Floor($index / [double]$sampleWidth)
    $relativeX = $index % $sampleWidth
    $touchesFill = $false
    for ($offsetY = -1; $offsetY -le 1 -and -not $touchesFill; $offsetY++) {
      for ($offsetX = -1; $offsetX -le 1; $offsetX++) {
        if ($offsetX -eq 0 -and $offsetY -eq 0) { continue }
        $candidateX = $relativeX + $offsetX
        $candidateY = $relativeY + $offsetY
        if ($candidateX -lt 0 -or $candidateX -ge $sampleWidth -or
            $candidateY -lt 0 -or $candidateY -ge $sampleHeight) { continue }
        if ($fillSupportMap[$candidateY * $sampleWidth + $candidateX]) {
          $touchesFill = $true
          break
        }
      }
    }
    if (-not $touchesFill) { continue }
    $outlineMap[$index] = $true
    $outlineQueue.Enqueue($index)
  }
  while ($outlineQueue.Count -gt 0) {
    $index = $outlineQueue.Dequeue()
    $relativeY = [int][Math]::Floor($index / [double]$sampleWidth)
    $relativeX = $index % $sampleWidth
    for ($offsetY = -1; $offsetY -le 1; $offsetY++) {
      for ($offsetX = -1; $offsetX -le 1; $offsetX++) {
        if ($offsetX -eq 0 -and $offsetY -eq 0) { continue }
        $candidateX = $relativeX + $offsetX
        $candidateY = $relativeY + $offsetY
        if ($candidateX -lt 0 -or $candidateX -ge $sampleWidth -or
            $candidateY -lt 0 -or $candidateY -ge $sampleHeight) { continue }
        $candidateIndex = $candidateY * $sampleWidth + $candidateX
        if ($outlineMap[$candidateIndex] -or
            -not $outlineCandidateMap[$candidateIndex]) { continue }
        $outlineMap[$candidateIndex] = $true
        $outlineQueue.Enqueue($candidateIndex)
      }
    }
  }

  $fillIndexes = [System.Collections.Generic.List[int]]::new()
  $outlineIndexes = [System.Collections.Generic.List[int]]::new()
  $fillXMin = [int]::MaxValue
  $fillYMin = [int]::MaxValue
  $fillXMax = [int]::MinValue
  $fillYMax = [int]::MinValue
  $outlineXMin = [int]::MaxValue
  $outlineYMin = [int]::MaxValue
  $outlineXMax = [int]::MinValue
  $outlineYMax = [int]::MinValue
  $touchesSampleBoundary = $false
  for ($index = 0; $index -lt $mapLength; $index++) {
    $relativeY = [int][Math]::Floor($index / [double]$sampleWidth)
    $relativeX = $index % $sampleWidth
    $x = $xMin + $relativeX
    $y = $yMin + $relativeY
    if ($fillSupportMap[$index]) {
      $fillIndexes.Add($index)
      $fillXMin = [Math]::Min($fillXMin, $x)
      $fillYMin = [Math]::Min($fillYMin, $y)
      $fillXMax = [Math]::Max($fillXMax, $x + 1)
      $fillYMax = [Math]::Max($fillYMax, $y + 1)
    }
    if ($outlineMap[$index]) {
      $outlineIndexes.Add($index)
      $outlineXMin = [Math]::Min($outlineXMin, $x)
      $outlineYMin = [Math]::Min($outlineYMin, $y)
      $outlineXMax = [Math]::Max($outlineXMax, $x + 1)
      $outlineYMax = [Math]::Max($outlineYMax, $y + 1)
      if ($relativeX -eq 0 -or $relativeX -eq $sampleWidth - 1 -or
          $relativeY -eq 0 -or $relativeY -eq $sampleHeight - 1) {
        $touchesSampleBoundary = $true
      }
    }
  }
  if ($outlineIndexes.Count -eq 0) {
    throw 'Settlement outcome has no distinct light outline adjacent to its fill ink.'
  }
  if ($touchesSampleBoundary) {
    throw 'Settlement outcome connected outline touches the sample boundary; maximum thickness cannot be proven.'
  }

  $expectedOutlineThickness = [Math]::Max(1,
    [int][Math]::Round(2.0 * $referenceScale, 0,
      [MidpointRounding]::ToEven))
  $maximumPossibleThickness = [Math]::Max($sampleWidth, $sampleHeight)
  $distanceMap = [int[]]::new($mapLength)
  $directionMap = [byte[]]::new($mapLength)
  $ringCounts = [int[]]::new($maximumPossibleThickness + 1)
  $linkedRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $leftRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $topRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $rightRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $bottomRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $leftCardinalRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $topCardinalRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $rightCardinalRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $bottomCardinalRingCounts = [int[]]::new($maximumPossibleThickness + 1)
  $minimumDirectCardinalRingFraction = 0.08
  $minimumPreviousRingRetentionFraction = 0.20
  $maximumConnectedThickness = 0
  foreach ($outlineIndex in $outlineIndexes) {
    $outlineY = [int][Math]::Floor($outlineIndex / [double]$sampleWidth)
    $outlineX = $outlineIndex % $sampleWidth
    $nearestDistance = [int]::MaxValue
    $directions = [byte]0
    foreach ($fillIndex in $fillIndexes) {
      $fillY = [int][Math]::Floor($fillIndex / [double]$sampleWidth)
      $fillX = $fillIndex % $sampleWidth
      $distance = [Math]::Max(
        [Math]::Abs($outlineX - $fillX),
        [Math]::Abs($outlineY - $fillY))
      if ($distance -gt $nearestDistance) { continue }
      if ($distance -lt $nearestDistance) {
        $nearestDistance = $distance
        $directions = [byte]0
      }
      if ($outlineX -lt $fillX) { $directions = $directions -bor 1 }
      if ($outlineY -lt $fillY) { $directions = $directions -bor 2 }
      if ($outlineX -gt $fillX) { $directions = $directions -bor 4 }
      if ($outlineY -gt $fillY) { $directions = $directions -bor 8 }
    }
    if ($nearestDistance -le 0 -or $nearestDistance -eq [int]::MaxValue) {
      throw 'Settlement outcome outline distance from fill support is invalid.'
    }
    $distanceMap[$outlineIndex] = $nearestDistance
    $directionMap[$outlineIndex] = $directions
    $ringCounts[$nearestDistance]++
    $maximumConnectedThickness = [Math]::Max(
      $maximumConnectedThickness, $nearestDistance)
  }

  foreach ($outlineIndex in $outlineIndexes) {
    $ring = $distanceMap[$outlineIndex]
    $relativeY = [int][Math]::Floor($outlineIndex / [double]$sampleWidth)
    $relativeX = $outlineIndex % $sampleWidth
    $linksToInnerRing = $false
    for ($offsetY = -1; $offsetY -le 1 -and -not $linksToInnerRing; $offsetY++) {
      for ($offsetX = -1; $offsetX -le 1; $offsetX++) {
        if ($offsetX -eq 0 -and $offsetY -eq 0) { continue }
        $candidateX = $relativeX + $offsetX
        $candidateY = $relativeY + $offsetY
        if ($candidateX -lt 0 -or $candidateX -ge $sampleWidth -or
            $candidateY -lt 0 -or $candidateY -ge $sampleHeight) { continue }
        $candidateIndex = $candidateY * $sampleWidth + $candidateX
        if (($ring -eq 1 -and $fillSupportMap[$candidateIndex]) -or
            ($ring -gt 1 -and $outlineMap[$candidateIndex] -and
              $distanceMap[$candidateIndex] -eq $ring - 1)) {
          $linksToInnerRing = $true
          break
        }
      }
    }
    if (-not $linksToInnerRing) { continue }
    $linkedRingCounts[$ring]++
    $directions = $directionMap[$outlineIndex]
    if (($directions -band 1) -ne 0) { $leftRingCounts[$ring]++ }
    if (($directions -band 2) -ne 0) { $topRingCounts[$ring]++ }
    if (($directions -band 4) -ne 0) { $rightRingCounts[$ring]++ }
    if (($directions -band 8) -ne 0) { $bottomRingCounts[$ring]++ }
    if ($relativeX + $ring -lt $sampleWidth -and
        $fillSupportMap[$relativeY * $sampleWidth + $relativeX + $ring]) {
      $leftCardinalRingCounts[$ring]++
    }
    if ($relativeY + $ring -lt $sampleHeight -and
        $fillSupportMap[($relativeY + $ring) * $sampleWidth + $relativeX]) {
      $topCardinalRingCounts[$ring]++
    }
    if ($relativeX - $ring -ge 0 -and
        $fillSupportMap[$relativeY * $sampleWidth + $relativeX - $ring]) {
      $rightCardinalRingCounts[$ring]++
    }
    if ($relativeY - $ring -ge 0 -and
        $fillSupportMap[($relativeY - $ring) * $sampleWidth + $relativeX]) {
      $bottomCardinalRingCounts[$ring]++
    }
  }

  if ($maximumConnectedThickness -ne $expectedOutlineThickness) {
    throw (
      'Settlement outcome connected outline thickness does not equal the runtime rounded width: ' +
      "actual=$maximumConnectedThickness expected=$expectedOutlineThickness")
  }
  $outlineRings = @()
  for ($ring = 1; $ring -le $expectedOutlineThickness; $ring++) {
    $sideCounts = [ordered]@{
      left = $leftRingCounts[$ring]
      top = $topRingCounts[$ring]
      right = $rightRingCounts[$ring]
      bottom = $bottomRingCounts[$ring]
    }
    $cardinalSideCounts = [ordered]@{
      left = $leftCardinalRingCounts[$ring]
      top = $topCardinalRingCounts[$ring]
      right = $rightCardinalRingCounts[$ring]
      bottom = $bottomCardinalRingCounts[$ring]
    }
    $minimumDirectCardinalSidePixels = [Math]::Max(
      2, [int][Math]::Ceiling($ringCounts[$ring] * $minimumDirectCardinalRingFraction))
    $cardinalSideFractionsOfRing = [ordered]@{
      left = $cardinalSideCounts.left / [double]$ringCounts[$ring]
      top = $cardinalSideCounts.top / [double]$ringCounts[$ring]
      right = $cardinalSideCounts.right / [double]$ringCounts[$ring]
      bottom = $cardinalSideCounts.bottom / [double]$ringCounts[$ring]
    }
    if ($ringCounts[$ring] -eq 0 -or $linkedRingCounts[$ring] -eq 0 -or
        $sideCounts.left -eq 0 -or $sideCounts.top -eq 0 -or
        $sideCounts.right -eq 0 -or $sideCounts.bottom -eq 0) {
      throw (
        "Settlement outcome outline ring $ring is not continuously linked on all four sides: " +
        ($sideCounts | ConvertTo-Json -Compress))
    }
    if ($cardinalSideCounts.left -lt $minimumDirectCardinalSidePixels -or
        $cardinalSideCounts.top -lt $minimumDirectCardinalSidePixels -or
        $cardinalSideCounts.right -lt $minimumDirectCardinalSidePixels -or
        $cardinalSideCounts.bottom -lt $minimumDirectCardinalSidePixels) {
      throw (
        "Settlement outcome outline ring $ring lacks minimum direct-cardinal coverage on all four sides: " +
        ([ordered]@{
          minimumPixels = $minimumDirectCardinalSidePixels
          minimumRingFraction = $minimumDirectCardinalRingFraction
          actual = $cardinalSideCounts
        } | ConvertTo-Json -Compress))
    }
    $minimumPreviousRingSidePixels = $null
    $previousRingRetentionFractions = $null
    if ($ring -gt 1) {
      $minimumPreviousRingSidePixels = [ordered]@{
        left = [int][Math]::Ceiling(
          $leftCardinalRingCounts[$ring - 1] * $minimumPreviousRingRetentionFraction)
        top = [int][Math]::Ceiling(
          $topCardinalRingCounts[$ring - 1] * $minimumPreviousRingRetentionFraction)
        right = [int][Math]::Ceiling(
          $rightCardinalRingCounts[$ring - 1] * $minimumPreviousRingRetentionFraction)
        bottom = [int][Math]::Ceiling(
          $bottomCardinalRingCounts[$ring - 1] * $minimumPreviousRingRetentionFraction)
      }
      $previousRingRetentionFractions = [ordered]@{
        left = $cardinalSideCounts.left / [double]$leftCardinalRingCounts[$ring - 1]
        top = $cardinalSideCounts.top / [double]$topCardinalRingCounts[$ring - 1]
        right = $cardinalSideCounts.right / [double]$rightCardinalRingCounts[$ring - 1]
        bottom = $cardinalSideCounts.bottom / [double]$bottomCardinalRingCounts[$ring - 1]
      }
      if ($cardinalSideCounts.left -lt $minimumPreviousRingSidePixels.left -or
          $cardinalSideCounts.top -lt $minimumPreviousRingSidePixels.top -or
          $cardinalSideCounts.right -lt $minimumPreviousRingSidePixels.right -or
          $cardinalSideCounts.bottom -lt $minimumPreviousRingSidePixels.bottom) {
        throw (
          "Settlement outcome outline ring $ring retains too little of an inner ring on one or more sides: " +
          ([ordered]@{
            minimumPreviousRingFraction = $minimumPreviousRingRetentionFraction
            minimumPixels = $minimumPreviousRingSidePixels
            actual = $cardinalSideCounts
          } | ConvertTo-Json -Compress))
      }
    }
    $outlineRings += [ordered]@{
      distanceFromFillCapturePixels = $ring
      pixels = $ringCounts[$ring]
      linkedToInnerRingPixels = $linkedRingCounts[$ring]
      linkedSidePixels = $sideCounts
      directCardinalSidePixels = $cardinalSideCounts
      directCardinalSideFractionsOfRing = $cardinalSideFractionsOfRing
      minimumDirectCardinalSidePixels = $minimumDirectCardinalSidePixels
      minimumDirectCardinalRingFraction = $minimumDirectCardinalRingFraction
      minimumPreviousRingRetentionFraction = if ($ring -gt 1) {
        $minimumPreviousRingRetentionFraction
      } else { $null }
      minimumPreviousRingSidePixels = $minimumPreviousRingSidePixels
      previousRingRetentionFractions = $previousRingRetentionFractions
    }
  }

  $fillBounds = [ordered]@{
    xMin = $fillXMin; yMin = $fillYMin; xMax = $fillXMax; yMax = $fillYMax
    width = $fillXMax - $fillXMin; height = $fillYMax - $fillYMin
  }
  $finalBounds = [ordered]@{
    xMin = $finalInkXMin; yMin = $finalInkYMin
    xMax = $finalInkXMax; yMax = $finalInkYMax
    width = $finalInkXMax - $finalInkXMin
    height = $finalInkYMax - $finalInkYMin
  }
  $connectedOutlinePixelsCovered = 0
  $candidateOutlinePixelsCovered = 0
  for ($index = 0; $index -lt $mapLength; $index++) {
    if ($outlineMap[$index] -and $finalInkMap[$index]) {
      $connectedOutlinePixelsCovered++
    }
    if ($outlineCandidateMap[$index] -and $finalInkMap[$index]) {
      $candidateOutlinePixelsCovered++
    }
  }
  if ($connectedOutlinePixelsCovered -ne $outlineIndexes.Count -or
      $candidateOutlinePixelsCovered -ne $outlineCandidatePixels -or
      $outlineIndexes.Count -ne $outlineCandidatePixels) {
    throw (
      'Settlement outcome contains outline candidates that are disconnected from fill or excluded from final ink: ' +
      ([ordered]@{
        candidates = $outlineCandidatePixels
        connected = $outlineIndexes.Count
        connectedCoveredByIndependentFinalInk = $connectedOutlinePixelsCovered
        coveredByFinalInk = $candidateOutlinePixelsCovered
      } | ConvertTo-Json -Compress))
  }
  return [ordered]@{
    sampleRegion = [ordered]@{
      xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax
    }
    fill = [ordered]@{
      corePixels = $fillCorePixels
      pixels = $fillIndexes.Count
      bounds = $fillBounds
    }
    outline = [ordered]@{
      candidatePixels = $outlineCandidatePixels
      pixels = $outlineIndexes.Count
      bounds = [ordered]@{
        xMin = $outlineXMin; yMin = $outlineYMin
        xMax = $outlineXMax; yMax = $outlineYMax
        width = $outlineXMax - $outlineXMin
        height = $outlineYMax - $outlineYMin
      }
      expansionFromFill = [ordered]@{
        left = $fillXMin - $outlineXMin; top = $fillYMin - $outlineYMin
        right = $outlineXMax - $fillXMax; bottom = $outlineYMax - $fillYMax
      }
      expectedThicknessCapturePixels = $expectedOutlineThickness
      maximumConnectedThicknessCapturePixels = $maximumConnectedThickness
      touchesSampleBoundary = $touchesSampleBoundary
      rings = $outlineRings
    }
    finalInk = [ordered]@{
      pixels = $finalInkPixels
      evidenceKind = 'stable-versus-hidden-channel-delta'
      referenceFrame = 'settlement-hidden-before-reveal'
      minimumChannelDeltaExclusive = $minimumFinalInkChannelDelta
      touchesSampleBoundary = $finalInkTouchesSampleBoundary
      connectedOutlinePixelsCovered = $connectedOutlinePixelsCovered
      outlineCandidatePixelsCovered = $candidateOutlinePixelsCovered
      bounds = $finalBounds
    }
  }
}

function Get-SyntheticSettlementOutcomeOutlineEvidence {
  param(
    [ValidateSet('none', 'left', 'top', 'right', 'bottom')]
    [string]$ThinSide = 'none',
    [ValidateSet('none', 'left', 'top', 'right', 'bottom')]
    [string]$ResidualSide = 'none',
    [ValidateSet('none', 'left', 'top', 'right', 'bottom')]
    [string]$GappedSide = 'none',
    [switch]$DetachedFragment,
    [switch]$UncoveredCandidate
  )

  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::new(48, 40)
  $referenceBitmap = [Drawing.Bitmap]::new(48, 40)
  $background = [Drawing.Color]::FromArgb(255, 24, 32, 28)
  $outline = [Drawing.Color]::FromArgb(255, 255, 246, 224)
  $fill = [Drawing.Color]::FromArgb(255, 139, 94, 60)
  try {
    for ($y = 0; $y -lt $bitmap.Height; $y++) {
      for ($x = 0; $x -lt $bitmap.Width; $x++) {
        $bitmap.SetPixel($x, $y, $background)
        $referenceBitmap.SetPixel($x, $y, $background)
      }
    }
    for ($y = 8; $y -le 31; $y++) {
      for ($x = 10; $x -le 37; $x++) {
        $bitmap.SetPixel($x, $y, $outline)
      }
    }
    for ($y = 10; $y -le 29; $y++) {
      for ($x = 12; $x -le 35; $x++) {
        $bitmap.SetPixel($x, $y, $fill)
      }
    }

    switch ($ThinSide) {
      'left' {
        for ($y = 10; $y -le 29; $y++) { $bitmap.SetPixel(10, $y, $background) }
      }
      'top' {
        for ($x = 12; $x -le 35; $x++) { $bitmap.SetPixel($x, 8, $background) }
      }
      'right' {
        for ($y = 10; $y -le 29; $y++) { $bitmap.SetPixel(37, $y, $background) }
      }
      'bottom' {
        for ($x = 12; $x -le 35; $x++) { $bitmap.SetPixel($x, 31, $background) }
      }
    }
    switch ($ResidualSide) {
      'left' {
        for ($y = 10; $y -le 29; $y++) { $bitmap.SetPixel(10, $y, $background) }
        $bitmap.SetPixel(10, 19, $outline)
      }
      'top' {
        for ($x = 12; $x -le 35; $x++) { $bitmap.SetPixel($x, 8, $background) }
        $bitmap.SetPixel(23, 8, $outline)
      }
      'right' {
        for ($y = 10; $y -le 29; $y++) { $bitmap.SetPixel(37, $y, $background) }
        $bitmap.SetPixel(37, 19, $outline)
      }
      'bottom' {
        for ($x = 12; $x -le 35; $x++) { $bitmap.SetPixel($x, 31, $background) }
        $bitmap.SetPixel(23, 31, $outline)
      }
    }
    switch ($GappedSide) {
      'left' {
        for ($y = 12; $y -le 27; $y++) { $bitmap.SetPixel(10, $y, $background) }
      }
      'top' {
        for ($x = 14; $x -le 33; $x++) { $bitmap.SetPixel($x, 8, $background) }
      }
      'right' {
        for ($y = 12; $y -le 27; $y++) { $bitmap.SetPixel(37, $y, $background) }
      }
      'bottom' {
        for ($x = 14; $x -le 33; $x++) { $bitmap.SetPixel($x, 31, $background) }
      }
    }
    if ($DetachedFragment) {
      $bitmap.SetPixel(42, 19, $outline)
      $bitmap.SetPixel(42, 20, $outline)
    }
    if ($UncoveredCandidate) {
      $referenceBitmap.SetPixel(10, 19, $outline)
    }

    return Get-SettlementOutcomeInkEvidence `
      -Bitmap $bitmap `
      -ReferenceBitmap $referenceBitmap `
      -Region ([ordered]@{ xMin = 0; yMin = 0; xMax = 48; yMax = 40 })
  }
  finally {
    $bitmap.Dispose()
    $referenceBitmap.Dispose()
  }
}
