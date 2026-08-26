# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Get-ImageMetrics {
  param([string]$Path)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $darkPixels = 0
    $lightPixels = 0
    $blackSamples = 0
    $nearBlackSamples = 0
    $invalidSamples = 0
    $sampleCount = 0
    [double]$lumaSum = 0
    $formerActionColorPixels = 0
    $formerActionColorXMin = [int]::MaxValue
    $formerActionColorXMax = [int]::MinValue
    $sampleStep = 4
    $maxNearBlackRunSamples = 0
    # These design-space regions are projected through the same safe-content transform as input controls.
    for ($y = $headerSampleRegion.yMin; $y -lt $headerSampleRegion.yMax; $y++) {
      for ($x = $headerSampleRegion.xMin; $x -lt $headerSampleRegion.xMax; $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        if ($pixel.A -gt 128 -and $luma -lt .48) { $darkPixels++ }
        if ($pixel.A -gt 128 -and $luma -gt .75) { $lightPixels++ }
      }
    }
    $frameSampleYMin = [Math]::Max(0, $SafeTop)
    $frameSampleYMax = [Math]::Min($bitmap.Height, $bitmap.Height - $SafeBottom)
    for ($y = $frameSampleYMin; $y -lt $frameSampleYMax; $y += $sampleStep) {
      $nearBlackRunSamples = 0
      for ($x = 0; $x -lt $bitmap.Width; $x += $sampleStep) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        $isNearBlack = $pixel.A -gt 128 -and $luma -lt $nearBlackLumaThreshold
        if ($isNearBlack) {
          $nearBlackSamples++
          $nearBlackRunSamples++
          $maxNearBlackRunSamples = [Math]::Max($maxNearBlackRunSamples, $nearBlackRunSamples)
        }
        else { $nearBlackRunSamples = 0 }
        $sampleCount++
        $lumaSum += $luma
        if ($pixel.A -gt 128 -and $luma -lt .025) { $blackSamples++ }
        if ($pixel.A -le 128 -or $luma -lt .025) { $invalidSamples++ }
      }
    }
    $maxNearBlackRunFraction = [Math]::Min(
      1.0, $maxNearBlackRunSamples * $sampleStep / [double]$bitmap.Width)
    # The removed persistent action row occupied reference rect x=8..394, y=760..810.
    for ($y = $formerActionRegion.yMin; $y -lt [Math]::Min($formerActionRegion.yMax, $bitmap.Height); $y++) {
      for ($x = $formerActionRegion.xMin; $x -lt [Math]::Min($formerActionRegion.xMax, $bitmap.Width); $x++) {
        $pixel = $bitmap.GetPixel($x, $y)
        $looksLikeOldOrange = $pixel.R -gt 190 -and $pixel.G -gt 90 -and $pixel.G -lt 190 -and $pixel.B -lt 90
        $looksLikeOldRed = $pixel.R -gt 180 -and $pixel.G -lt 115 -and $pixel.B -lt 110
        if ($looksLikeOldOrange -or $looksLikeOldRed) {
          $formerActionColorPixels++
          $formerActionColorXMin = [Math]::Min($formerActionColorXMin, $x)
          $formerActionColorXMax = [Math]::Max($formerActionColorXMax, $x)
        }
      }
    }
    return [ordered]@{
      width = $bitmap.Width
      height = $bitmap.Height
      headerDarkPixels = $darkPixels
      headerLightPixels = $lightPixels
      blackFraction = if ($sampleCount -gt 0) { $blackSamples / [double]$sampleCount } else { 1.0 }
      nearBlackFraction = if ($sampleCount -gt 0) { $nearBlackSamples / [double]$sampleCount } else { 1.0 }
      maxNearBlackHorizontalRunFraction = $maxNearBlackRunFraction
      invalidFraction = if ($sampleCount -gt 0) { $invalidSamples / [double]$sampleCount } else { 1.0 }
      averageLuma = if ($sampleCount -gt 0) { $lumaSum / [double]$sampleCount } else { 0.0 }
      formerActionColorPixels = $formerActionColorPixels
      formerActionColorSpanPixels = if ($formerActionColorPixels -gt 0) {
        $formerActionColorXMax - $formerActionColorXMin + 1
      } else { 0 }
      sampledRegions = [ordered]@{
        header = $headerSampleRegion
        formerAction = $formerActionRegion
        frameContent = [ordered]@{
          xMin = 0
          yMin = $frameSampleYMin
          xMax = $bitmap.Width
          yMax = $frameSampleYMax
        }
      }
    }
  }
  finally { $bitmap.Dispose() }
}

function Test-StableFrameMetrics {
  param([object]$Metrics)
  return $Metrics.invalidFraction -lt .05 -and
    $Metrics.blackFraction -lt $maxBlackFraction -and
    $Metrics.nearBlackFraction -lt $maxNearBlackFraction
}

function Test-PanelOutlinePixel {
  param([object]$Pixel)
  return $Pixel.A -gt 200 -and $Pixel.R -ge 70 -and
    $Pixel.R -lt 195 -and $Pixel.G -lt 160 -and $Pixel.B -lt 125 -and
    $Pixel.R -gt ($Pixel.G + 8)
}

function Get-PanelFrameEvidence {
  param([string]$Path, [object]$Region, [string]$Name,
    [ValidateRange(0.0, 1.0)][double]$SideSampleFraction = .5)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $xMin = [Math]::Max(0, [int]$Region.xMin)
    $yMin = [Math]::Max(0, [int]$Region.yMin)
    $xMax = [Math]::Min($bitmap.Width, [int]$Region.xMax)
    $yMax = [Math]::Min($bitmap.Height, [int]$Region.yMax)
    $edgeBandX = [Math]::Max(4, [int][Math]::Ceiling(($xMax - $xMin) * .08))
    $edgeBandY = [Math]::Max(4, [int][Math]::Ceiling(($yMax - $yMin) * .22))
    $sampleYMin = $yMin + [int][Math]::Floor(($yMax - $yMin) * .20)
    $sampleYMax = $yMin + [int][Math]::Ceiling(($yMax - $yMin) * .80)
    $sampleXMin = $xMin + [int][Math]::Floor(($xMax - $xMin) * .25)
    $sampleXMax = $xMin + [int][Math]::Ceiling(($xMax - $xMin) * .75)
    $minimumColumnPixels = [Math]::Max(2,
      [int][Math]::Ceiling(($sampleYMax - $sampleYMin) * .18))
    $minimumRowPixels = [Math]::Max(4,
      [int][Math]::Ceiling(($sampleXMax - $sampleXMin) * .18))
    $columnCounts = @{}
    foreach ($x in @($xMin..([Math]::Min($xMax - 1, $xMin + $edgeBandX)))) {
      $count = 0
      for ($y = $sampleYMin; $y -lt $sampleYMax; $y++) {
        if (Test-PanelOutlinePixel -Pixel ($bitmap.GetPixel($x, $y))) { $count++ }
      }
      $columnCounts[$x] = $count
    }
    foreach ($x in @(([Math]::Max($xMin, $xMax - $edgeBandX - 1))..($xMax - 1))) {
      if ($columnCounts.ContainsKey($x)) { continue }
      $count = 0
      for ($y = $sampleYMin; $y -lt $sampleYMax; $y++) {
        if (Test-PanelOutlinePixel -Pixel ($bitmap.GetPixel($x, $y))) { $count++ }
      }
      $columnCounts[$x] = $count
    }
    $left = @($columnCounts.GetEnumerator() |
      Where-Object { $_.Key -le ($xMin + $edgeBandX) -and $_.Value -ge $minimumColumnPixels } |
      Sort-Object Key | Select-Object -First 1)
    $right = @($columnCounts.GetEnumerator() |
      Where-Object { $_.Key -ge ($xMax - $edgeBandX - 1) -and $_.Value -ge $minimumColumnPixels } |
      Sort-Object Key -Descending | Select-Object -First 1)
    $rowCounts = @{}
    for ($y = $yMin; $y -le [Math]::Min($yMax - 1, $yMin + $edgeBandY); $y++) {
      $count = 0
      for ($x = $sampleXMin; $x -lt $sampleXMax; $x++) {
        if (Test-PanelOutlinePixel -Pixel ($bitmap.GetPixel($x, $y))) { $count++ }
      }
      $rowCounts[$y] = $count
    }
    $top = @($rowCounts.GetEnumerator() |
      Where-Object { $_.Value -ge $minimumRowPixels } |
      Sort-Object Key | Select-Object -First 1)
    if ($left.Count -eq 0 -or $right.Count -eq 0 -or $top.Count -eq 0) {
      throw "Structural panel outline was not found for '$Name' in $Path."
    }
    # Visible edge discovery aggregates a tall band so it survives texture noise.
    # Thickness must be sampled on a quiet horizontal rail; aggregating columns
    # would mistake Battle's dark board and nested panels for a 20+ px outline.
    $sideSampleY = [Math]::Min($yMax - 1, [Math]::Max($yMin,
      $yMin + [int][Math]::Round(($yMax - $yMin - 1) * $SideSampleFraction)))
    $leftThickness = 0
    for ($x = [int]$left[0].Key; $x -lt $xMax -and
        (Test-PanelOutlinePixel -Pixel ($bitmap.GetPixel($x, $sideSampleY))); $x++) {
      $leftThickness++
    }
    $rightThickness = 0
    for ($x = [int]$right[0].Key; $x -ge $xMin -and
        (Test-PanelOutlinePixel -Pixel ($bitmap.GetPixel($x, $sideSampleY))); $x--) {
      $rightThickness++
    }
    $topThickness = 0
    for ($y = [int]$top[0].Key; $rowCounts.ContainsKey($y) -and
        $rowCounts[$y] -ge $minimumRowPixels; $y++) { $topThickness++ }
    return [ordered]@{
      name = $Name
      owner = $Region
      visibleEdges = [ordered]@{
        left = [int]$left[0].Key; right = [int]$right[0].Key; top = [int]$top[0].Key
      }
      outlineBands = [ordered]@{
        left = $leftThickness; right = $rightThickness; top = $topThickness
      }
      thresholds = [ordered]@{
        minimumColumnPixels = $minimumColumnPixels; minimumRowPixels = $minimumRowPixels
        sideThicknessSampleY = $sideSampleY
      }
    }
  }
  finally { $bitmap.Dispose() }
}

function Get-BattlePanelGeometryEvidence {
  param([string]$Path)
  $header = Get-PanelFrameEvidence -Path $Path -Region $headerPanelRect `
    -Name 'Header' -SideSampleFraction .5
  $stage = Get-PanelFrameEvidence -Path $Path -Region $gameplayStageRect `
    -Name 'GameplayStage' -SideSampleFraction .9
  $context = Get-PanelFrameEvidence -Path $Path -Region $contextTrayRect `
    -Name 'ContextTray' -SideSampleFraction .5
  $nursery = Get-PanelFrameEvidence -Path $Path -Region $nurseryTrayRect `
    -Name 'NurseryTray' -SideSampleFraction .5
  $edgeTolerance = 1
  $leftDelta = [Math]::Abs($header.visibleEdges.left - $stage.visibleEdges.left)
  $rightDelta = [Math]::Abs($header.visibleEdges.right - $stage.visibleEdges.right)
  $stageBands = @($stage.outlineBands.left, $stage.outlineBands.right,
    $stage.outlineBands.top)
  $standardBands = @(
    $header.outlineBands.left, $header.outlineBands.right, $header.outlineBands.top,
    $context.outlineBands.left, $context.outlineBands.right, $context.outlineBands.top,
    $nursery.outlineBands.left, $nursery.outlineBands.right, $nursery.outlineBands.top)
  $minimumStageBand = $stageBands | Measure-Object -Minimum |
    Select-Object -ExpandProperty Minimum
  $maximumStageBand = $stageBands | Measure-Object -Maximum |
    Select-Object -ExpandProperty Maximum
  $maximumStandardBand = $standardBands | Measure-Object -Maximum |
    Select-Object -ExpandProperty Maximum
  if ($header.owner.xMin -ne $stage.owner.xMin -or
      $header.owner.xMax -ne $stage.owner.xMax -or
      $leftDelta -gt $edgeTolerance -or $rightDelta -gt $edgeTolerance -or
      $minimumStageBand -lt 3 -or $maximumStageBand -gt 5 -or
      $maximumStandardBand -gt 2) {
    throw ('Battle structural hierarchy mismatch: ' +
      ([ordered]@{ header = $header; gameplayStage = $stage; contextTray = $context;
        nurseryTray = $nursery; leftDelta = $leftDelta; rightDelta = $rightDelta;
        minimumStageBand = $minimumStageBand; maximumStageBand = $maximumStageBand;
        maximumStandardBand = $maximumStandardBand } |
        ConvertTo-Json -Depth 8 -Compress))
  }
  $refreshLowerMarginLogical = [Math]::Round(
    (($mappedDesignBounds.yMax - $refreshActionRect.yMax) / $referenceScale), 3)
  if ($refreshLowerMarginLogical -lt 8 -or $refreshLowerMarginLogical -gt 40) {
    throw "Battle refresh lower margin is outside 8..40 logical points: $refreshLowerMarginLogical"
  }
  return [ordered]@{
    passed = $true
    edgeToleranceCapturePixels = $edgeTolerance
    alignedOwnerTrack = $true
    leftEdgeDeltaCapturePixels = $leftDelta
    rightEdgeDeltaCapturePixels = $rightDelta
    heavyFrameBandRangeCapturePixels = [ordered]@{ minimum = 3; maximum = 5 }
    standardFrameBandMaximumCapturePixels = 2
    structuralFrameCount = 1
    legacyEnclosingFrameDetected = $false
    refreshLowerMarginLogical = $refreshLowerMarginLogical
    header = $header
    gameplayStage = $stage
    contextTray = $context
    nurseryTray = $nursery
  }
}

function Test-UiTextInkPixel {
  param([object]$Pixel, [ValidateSet('light-surface', 'dark-surface')][string]$Palette)
  if ($Palette -eq 'dark-surface') {
    return $Pixel.A -gt 200 -and $Pixel.R -ge 60 -and $Pixel.R -le 160 -and
      $Pixel.G -ge 35 -and $Pixel.G -le 130 -and $Pixel.B -ge 15 -and
      $Pixel.B -le 90 -and $Pixel.R -gt ($Pixel.G + 10) -and
      $Pixel.G -gt ($Pixel.B + 5)
  }
  # Noto Sans antialiasing blends the brown text token into the cream surface.
  # Keep this band above the darker panel outline so structural rails cannot
  # masquerade as escaped glyph ink.
  return $Pixel.A -gt 200 -and $Pixel.R -ge 145 -and $Pixel.R -le 225 -and
    $Pixel.G -ge 115 -and $Pixel.G -le 190 -and $Pixel.B -ge 85 -and
    $Pixel.B -le 160 -and $Pixel.R -gt ($Pixel.G + 15) -and
    $Pixel.G -gt ($Pixel.B + 10)
}

function Get-TextOwnerEvidence {
  param([string]$Path, [object]$Owner, [string]$Name,
    [int]$MinimumInsidePixels = 2,
    [ValidateSet('light-surface', 'dark-surface')][string]$Palette = 'light-surface')
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $guard = [Math]::Max(2, [int][Math]::Ceiling(2 * $referenceScale))
    $inside = 0
    $outside = 0
    for ($y = [Math]::Max(0, $Owner.yMin - $guard);
         $y -lt [Math]::Min($bitmap.Height, $Owner.yMax + $guard); $y++) {
      for ($x = [Math]::Max(0, $Owner.xMin - $guard);
           $x -lt [Math]::Min($bitmap.Width, $Owner.xMax + $guard); $x++) {
        if (-not (Test-UiTextInkPixel -Pixel ($bitmap.GetPixel($x, $y)) `
              -Palette $Palette)) { continue }
        if ($x -ge $Owner.xMin -and $x -lt $Owner.xMax -and
            $y -ge $Owner.yMin -and $y -lt $Owner.yMax) { $inside++ }
        else { $outside++ }
      }
    }
    $outsideAllowance = [Math]::Max(4,
      [int][Math]::Ceiling(8 * $referenceScale * $referenceScale))
    if ($inside -lt $MinimumInsidePixels -or $outside -gt $outsideAllowance) {
      throw "Text containment failed for '$Name': inside=$inside/$MinimumInsidePixels outside=$outside/$outsideAllowance owner=$($Owner | ConvertTo-Json -Compress)"
    }
    return [ordered]@{
      name = $Name; owner = $Owner; palette = $Palette; insideInkPixels = $inside
      outsideGuardInkPixels = $outside; outsideAllowancePixels = $outsideAllowance
      guardPixels = $guard; passed = $true
    }
  }
  finally { $bitmap.Dispose() }
}

function Get-BattleTextContainmentEvidence {
  param([string]$ReadyPath, [string]$DetailPath)
  $owners = @(
    [ordered]@{ path = $ReadyPath; name = 'header-title'; rect = $headerTitleOwner; minimum = 8 },
    [ordered]@{ path = $ReadyPath; name = 'header-metric-row'; rect = $headerMetricRowOwner; minimum = 12 },
    [ordered]@{ path = $ReadyPath; name = 'tool-title'; rect = $toolTitleOwner; minimum = 8 },
    [ordered]@{ path = $ReadyPath; name = 'nursery-title'; rect = $nurseryTitleOwner; minimum = 8 },
    [ordered]@{ path = $ReadyPath; name = 'refresh-action'; rect = $refreshTextOwner; minimum = 12; palette = 'dark-surface' },
    [ordered]@{ path = $DetailPath; name = 'detail-title'; rect = $detailTitleOwner; minimum = 8; palette = 'dark-surface' },
    [ordered]@{ path = $DetailPath; name = 'detail-body'; rect = $detailBodyOwner; minimum = 2; palette = 'dark-surface' }
  )
  $measurements = @()
  foreach ($owner in $owners) {
    $measurements += Get-TextOwnerEvidence -Path $owner.path -Owner $owner.rect `
      -Name $owner.name -MinimumInsidePixels $owner.minimum `
      -Palette $(if ($owner.palette) { $owner.palette } else { 'light-surface' })
  }
  return [ordered]@{
    passed = $true
    policy = 'final-raster primary-text ink must remain inside declared owner guard'
    owners = $measurements
  }
}

function Get-OccupiedRegionEvidence {
  param([string]$Path, [object]$Region, [string]$Name)
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $pixels = 0
    $samples = 0
    for ($y = $Region.yMin; $y -lt $Region.yMax; $y += 2) {
      for ($x = $Region.xMin; $x -lt $Region.xMax; $x += 2) {
        $pixel = $bitmap.GetPixel($x, $y)
        $luma = (.2126 * $pixel.R + .7152 * $pixel.G + .0722 * $pixel.B) / 255.0
        if ($pixel.A -gt 200 -and ($luma -lt .70 -or
            ([Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B)) -
             [Math]::Min($pixel.R, [Math]::Min($pixel.G, $pixel.B))) -gt 55)) { $pixels++ }
        $samples++
      }
    }
    $fraction = if ($samples -gt 0) { $pixels / [double]$samples } else { 0 }
    if ($fraction -lt .002) {
      throw "Occupied-content region '$Name' is visually empty: fraction=$fraction."
    }
    return [ordered]@{ name = $Name; region = $Region; visualSamples = $pixels;
      totalSamples = $samples; visualFraction = $fraction; passed = $true }
  }
  finally { $bitmap.Dispose() }
}

function Get-BattleOccupiedBalanceEvidence {
  param([string]$ReadyPath, [string]$DetailPath)
  $regions = @(
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $headerPanelRect -Name 'header'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $boardRegion -Name 'board'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $contextTrayRect -Name 'context-tray-tools'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $nurseryTrayRect -Name 'nursery-tray'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $refreshActionRect -Name 'refresh-action'),
    (Get-OccupiedRegionEvidence -Path $DetailPath -Region $detailRegion -Name 'contextual-detail')
  )
  $occupiedSpan = ($refreshActionRect.yMax - $headerPanelRect.yMin) /
    [double]($mappedDesignBounds.yMax - $mappedDesignBounds.yMin)
  if ($occupiedSpan -lt .94) {
    throw "Battle occupied vertical span is below 94%: $occupiedSpan"
  }
  return [ordered]@{
    passed = $true
    requiredVerticalSpanFraction = .94
    occupiedVerticalSpanFraction = $occupiedSpan
    stateCoverage = @('ready', 'plant-detail')
    contextModes = [ordered]@{ ready = 'tools'; plantDetail = 'selected-detail' }
    regions = $regions
  }
}

function Get-ImageDifferenceMetrics {
  param(
    [string]$ReferencePath,
    [string]$CandidatePath,
    [object]$Region,
    [int]$ChannelThreshold = 8
  )
  Add-Type -AssemblyName System.Drawing
  $reference = [Drawing.Bitmap]::FromFile($ReferencePath)
  $candidate = [Drawing.Bitmap]::FromFile($CandidatePath)
  try {
    if ($reference.Width -ne $candidate.Width -or
        $reference.Height -ne $candidate.Height) {
      throw "Image difference dimensions do not match: $ReferencePath / $CandidatePath"
    }
    $xMin = [Math]::Max(0, [int]$Region.xMin)
    $yMin = [Math]::Max(0, [int]$Region.yMin)
    $xMax = [Math]::Min($reference.Width, [int]$Region.xMax)
    $yMax = [Math]::Min($reference.Height, [int]$Region.yMax)
    if ($xMax -le $xMin -or $yMax -le $yMin) {
      throw "Image difference region is empty: $($Region | ConvertTo-Json -Compress)"
    }
    $edgeBand = [Math]::Max(1, [Math]::Ceiling(3 * $referenceScale))
    $changedPixels = 0
    $changedEdgePixels = 0
    $edgePixels = 0
    $changedXMin = [int]::MaxValue
    $changedYMin = [int]::MaxValue
    $changedXMax = [int]::MinValue
    $changedYMax = [int]::MinValue
    for ($y = $yMin; $y -lt $yMax; $y++) {
      for ($x = $xMin; $x -lt $xMax; $x++) {
        $isEdge = $x -lt $xMin + $edgeBand -or $x -ge $xMax - $edgeBand -or
          $y -lt $yMin + $edgeBand -or $y -ge $yMax - $edgeBand
        if ($isEdge) { $edgePixels++ }
        $left = $reference.GetPixel($x, $y)
        $right = $candidate.GetPixel($x, $y)
        $delta = [Math]::Max([Math]::Abs([int]$left.R - [int]$right.R),
          [Math]::Max([Math]::Abs([int]$left.G - [int]$right.G),
            [Math]::Abs([int]$left.B - [int]$right.B)))
        if ($delta -le $ChannelThreshold) { continue }
        $changedPixels++
        if ($isEdge) { $changedEdgePixels++ }
        $changedXMin = [Math]::Min($changedXMin, $x)
        $changedYMin = [Math]::Min($changedYMin, $y)
        $changedXMax = [Math]::Max($changedXMax, $x)
        $changedYMax = [Math]::Max($changedYMax, $y)
      }
    }
    $regionPixels = ($xMax - $xMin) * ($yMax - $yMin)
    $bounds = if ($changedPixels -eq 0) { $null } else {
      [ordered]@{
        xMin = $changedXMin; yMin = $changedYMin
        xMax = $changedXMax + 1; yMax = $changedYMax + 1
      }
    }
    return [ordered]@{
      channelThreshold = $ChannelThreshold
      region = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
      regionPixels = $regionPixels
      changedPixels = $changedPixels
      changedFraction = $changedPixels / [double]$regionPixels
      edgeBandPixels = $edgeBand
      edgePixels = $edgePixels
      changedEdgePixels = $changedEdgePixels
      changedBounds = $bounds
    }
  }
  finally {
    $reference.Dispose()
    $candidate.Dispose()
  }
}

function Get-ImageInsetEvidence {
  param(
    [string]$ReferencePath,
    [string]$CandidatePath,
    [object]$Region,
    [int]$DistanceMargin = 8
  )
  Add-Type -AssemblyName System.Drawing
  $reference = [Drawing.Bitmap]::FromFile($ReferencePath)
  $candidate = [Drawing.Bitmap]::FromFile($CandidatePath)
  try {
    if ($reference.Width -ne $candidate.Width -or
        $reference.Height -ne $candidate.Height) {
      throw "Image inset dimensions do not match: $ReferencePath / $CandidatePath"
    }
    $xMin = [Math]::Max(1, [int]$Region.xMin)
    $yMin = [Math]::Max(0, [int]$Region.yMin)
    $xMax = [Math]::Min($reference.Width - 1, [int]$Region.xMax)
    $yMax = [Math]::Min($reference.Height, [int]$Region.yMax)
    $edgeBand = [Math]::Max(2, [Math]::Ceiling(3 * $referenceScale))
    $sampleYMin = $yMin + $edgeBand
    $sampleYMax = $yMax - $edgeBand
    if ($xMax - $xMin -lt $edgeBand * 2 -or $sampleYMax -le $sampleYMin) {
      throw "Image inset region is too small: $($Region | ConvertTo-Json -Compress)"
    }
    $samples = 0
    $retreatedPixels = 0
    for ($y = $sampleYMin; $y -lt $sampleYMax; $y++) {
      $leftBackground = $candidate.GetPixel($xMin - 1, $y)
      $rightBackground = $candidate.GetPixel($xMax, $y)
      for ($offset = 0; $offset -lt $edgeBand; $offset++) {
        foreach ($probe in @(
          [ordered]@{ x = $xMin + $offset; background = $leftBackground },
          [ordered]@{ x = $xMax - 1 - $offset; background = $rightBackground })) {
          $referencePixel = $reference.GetPixel($probe.x, $y)
          $candidatePixel = $candidate.GetPixel($probe.x, $y)
          $background = $probe.background
          $referenceDistance = [Math]::Max(
            [Math]::Abs([int]$referencePixel.R - [int]$background.R),
            [Math]::Max([Math]::Abs([int]$referencePixel.G - [int]$background.G),
              [Math]::Abs([int]$referencePixel.B - [int]$background.B)))
          $candidateDistance = [Math]::Max(
            [Math]::Abs([int]$candidatePixel.R - [int]$background.R),
            [Math]::Max([Math]::Abs([int]$candidatePixel.G - [int]$background.G),
              [Math]::Abs([int]$candidatePixel.B - [int]$background.B)))
          $samples++
          if ($candidateDistance + $DistanceMargin -lt $referenceDistance) {
            $retreatedPixels++
          }
        }
      }
    }
    return [ordered]@{
      region = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
      edgeBandPixels = $edgeBand
      distanceMargin = $DistanceMargin
      samples = $samples
      retreatedPixels = $retreatedPixels
      retreatedFraction = if ($samples -eq 0) { 0.0 } else {
        $retreatedPixels / [double]$samples
      }
    }
  }
  finally {
    $reference.Dispose()
    $candidate.Dispose()
  }
}

function Get-ColorMaskEvidence {
  param(
    [object]$Bitmap,
    [object]$Region,
    [ValidateSet('title-ink', 'hint-icon', 'hint-copy', 'primary-surface', 'danger-surface', 'result-banner')]
    [string]$Mask
  )
  $xMin = [Math]::Max(0, [int]$Region.xMin)
  $yMin = [Math]::Max(0, [int]$Region.yMin)
  $xMax = [Math]::Min($Bitmap.Width, [int]$Region.xMax)
  $yMax = [Math]::Min($Bitmap.Height, [int]$Region.yMax)
  $count = 0
  [double]$sumX = 0
  [double]$sumY = 0
  $visibleXMin = [int]::MaxValue
  $visibleYMin = [int]::MaxValue
  $visibleXMax = [int]::MinValue
  $visibleYMax = [int]::MinValue
  for ($y = $yMin; $y -lt $yMax; $y++) {
    for ($x = $xMin; $x -lt $xMax; $x++) {
      $pixel = $Bitmap.GetPixel($x, $y)
      $matches = switch ($Mask) {
        'title-ink' {
          [Math]::Abs([int]$pixel.R - 139) -le 18 -and
            [Math]::Abs([int]$pixel.G - 94) -le 18 -and
            [Math]::Abs([int]$pixel.B - 60) -le 18
          break
        }
        'hint-icon' {
          (($pixel.R - $pixel.G) -gt 30 -or ($pixel.G - $pixel.R) -gt 15) -and
            ($pixel.R -lt 240 -or $pixel.G -lt 220) -and $pixel.B -lt 150
          break
        }
        'hint-copy' {
          $pixel.R -lt 235 -and $pixel.G -lt 215 -and $pixel.B -lt 185
          break
        }
        'primary-surface' {
          $pixel.G -gt ($pixel.R + 18) -and
            $pixel.G -gt ($pixel.B + 35) -and $pixel.G -gt 65
          break
        }
        'danger-surface' {
          $pixel.R -gt ($pixel.G + 55) -and
            $pixel.R -gt ($pixel.B + 55) -and $pixel.R -gt 145
          break
        }
        'result-banner' {
          $warmRibbon = $pixel.R -gt 170 -and $pixel.G -gt 85 -and
            $pixel.G -lt 235 -and $pixel.B -lt 170 -and
            $pixel.R -gt ($pixel.B + 35)
          $greenFoliage = $pixel.G -gt 70 -and
            $pixel.G -gt ($pixel.B + 25) -and
            $pixel.G -gt ($pixel.R - 30)
          $pixel.A -gt 200 -and ($warmRibbon -or $greenFoliage)
          break
        }
      }
      if (-not $matches) { continue }
      $count++
      $sumX += $x + .5
      $sumY += $y + .5
      $visibleXMin = [Math]::Min($visibleXMin, $x)
      $visibleYMin = [Math]::Min($visibleYMin, $y)
      $visibleXMax = [Math]::Max($visibleXMax, $x + 1)
      $visibleYMax = [Math]::Max($visibleYMax, $y + 1)
    }
  }
  if ($count -eq 0) {
    throw "Final-raster optical mask '$Mask' found no matching pixels."
  }
  return [ordered]@{
    mask = $Mask
    sampleRegion = [ordered]@{ xMin = $xMin; yMin = $yMin; xMax = $xMax; yMax = $yMax }
    pixels = $count
    bounds = [ordered]@{
      xMin = $visibleXMin; yMin = $visibleYMin
      xMax = $visibleXMax; yMax = $visibleYMax
      width = $visibleXMax - $visibleXMin
      height = $visibleYMax - $visibleYMin
      centerX = ($visibleXMin + $visibleXMax) * .5
      centerY = ($visibleYMin + $visibleYMax) * .5
    }
    centroid = [ordered]@{ x = $sumX / $count; y = $sumY / $count }
  }
}

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
