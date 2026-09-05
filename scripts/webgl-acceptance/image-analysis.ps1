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
      sampledRegions = [ordered]@{
        header = $headerSampleRegion
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
  if ($Pixel.A -le 200) { return $false }
  $darkOrSoilOutline = $Pixel.R -ge 70 -and $Pixel.R -lt 195 -and
    $Pixel.G -lt 160 -and $Pixel.B -lt 125 -and
    $Pixel.R -gt ($Pixel.G + 8)
  # Warm-paper cards use a light amber one-pixel rail instead of the soil
  # frame's dark terracotta rail. Keep this range below the cream fill so the
  # structural detector still measures the actual edge, not panel interiors.
  $warmPaperOutline = if ($referenceScale -lt .95) {
    $Pixel.R -ge 180 -and $Pixel.R -lt 250 -and
      $Pixel.G -ge 145 -and $Pixel.G -lt 235 -and
      $Pixel.B -ge 95 -and $Pixel.B -lt 215 -and
      $Pixel.R -gt ($Pixel.G + 8) -and $Pixel.G -gt ($Pixel.B + 15)
  }
  else {
    $Pixel.R -ge 180 -and $Pixel.R -lt 245 -and
      $Pixel.G -ge 145 -and $Pixel.G -lt 225 -and
      $Pixel.B -ge 95 -and $Pixel.B -lt 205 -and
      $Pixel.R -gt ($Pixel.G + 10) -and $Pixel.G -gt ($Pixel.B + 15)
  }
  return $darkOrSoilOutline -or $warmPaperOutline
}

function Get-PanelRailThickness {
  param(
    [object]$Bitmap,
    [int]$StartX,
    [int]$StartY,
    [int]$StepX,
    [int]$StepY,
    [ValidateRange(4, 32)][int]$MaximumDepth = 16)
  $samples = @()
  for ($index = 0; $index -le $MaximumDepth; $index++) {
    $x = $StartX + $StepX * $index
    $y = $StartY + $StepY * $index
    if ($x -lt 0 -or $x -ge $Bitmap.Width -or
        $y -lt 0 -or $y -ge $Bitmap.Height) { break }
    $samples += $Bitmap.GetPixel($x, $y)
  }
  if ($samples.Count -lt 4) {
    throw 'Panel rail sampling did not reach a stable interior.'
  }
  for ($index = 1; $index -le ($samples.Count - 3); $index++) {
    $first = $samples[$index]
    $second = $samples[$index + 1]
    $third = $samples[$index + 2]
    $firstDelta = [Math]::Abs([int]$first.R - [int]$second.R) +
      [Math]::Abs([int]$first.G - [int]$second.G) +
      [Math]::Abs([int]$first.B - [int]$second.B)
    $secondDelta = [Math]::Abs([int]$second.R - [int]$third.R) +
      [Math]::Abs([int]$second.G - [int]$third.G) +
      [Math]::Abs([int]$second.B - [int]$third.B)
    if ($firstDelta -le 18 -and $secondDelta -le 18) {
      return $index
    }
  }
  throw 'Panel rail did not transition to a stable interior within 16 capture pixels.'
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
    # Measure the composited rail to its first stable interior run. This keeps
    # soil fill from being mistaken for hundreds of pixels of dark outline and
    # works for both the heavy stage frame and light warm-paper card rails.
    $leftThickness = Get-PanelRailThickness -Bitmap $bitmap `
      -StartX ([int]$left[0].Key) -StartY $sideSampleY -StepX 1 -StepY 0
    $rightThickness = Get-PanelRailThickness -Bitmap $bitmap `
      -StartX ([int]$right[0].Key) -StartY $sideSampleY -StepX -1 -StepY 0
    $topSampleX = $xMin + [int][Math]::Round(($xMax - $xMin - 1) * .5)
    $topThickness = Get-PanelRailThickness -Bitmap $bitmap `
      -StartX $topSampleX -StartY ([int]$top[0].Key) -StepX 0 -StepY 1
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
  $header = Get-PanelFrameEvidence -Path $Path -Region $headerPanelRect -Name 'Header' -SideSampleFraction .5
  $pageShell = Get-PanelFrameEvidence -Path $Path -Region $pageShellRect -Name 'PageShell' -SideSampleFraction .5
  $stage = Get-PanelFrameEvidence -Path $Path -Region $gameplayStageRect -Name 'GameplayStage' -SideSampleFraction .9
  $context = Get-PanelFrameEvidence -Path $Path -Region $contextTrayRect -Name 'ContextTray' -SideSampleFraction .5
  $nursery = Get-PanelFrameEvidence -Path $Path -Region $nurseryTrayRect -Name 'NurseryTray' -SideSampleFraction .5
  $edgeTolerance = 1
  $leftDelta = [Math]::Abs($header.visibleEdges.left - $pageShell.visibleEdges.left)
  $rightDelta = [Math]::Abs($header.visibleEdges.right - $pageShell.visibleEdges.right)
  $stageSideBands = @($stage.outlineBands.left, $stage.outlineBands.right)
  $standardBands = @(
    $pageShell.outlineBands.left, $pageShell.outlineBands.right, $pageShell.outlineBands.top,
    $context.outlineBands.left, $context.outlineBands.right, $context.outlineBands.top,
    $nursery.outlineBands.left, $nursery.outlineBands.right, $nursery.outlineBands.top)
  $minimumStageSideBand = $stageSideBands | Measure-Object -Minimum | Select-Object -ExpandProperty Minimum
  $maximumStageSideBand = $stageSideBands | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
  $stageTopBand = $stage.outlineBands.top
  $maximumStandardBand = $standardBands | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
  $minimumStageSideBandRequired = if ($referenceScale -lt .95) { 4 } else { 5 }
  $maximumStandardBandAllowed = if ($referenceScale -lt .95) { 3 } else { 2 }
  $stageHeightFraction = ($stage.owner.yMax - $stage.owner.yMin) / [double]($mappedDesignBounds.yMax - $mappedDesignBounds.yMin)
  $stageLeftInsetLogical = ($stage.owner.xMin - $pageShell.owner.xMin) / $referenceScale
  $stageRightInsetLogical = ($pageShell.owner.xMax - $stage.owner.xMax) / $referenceScale
  if ($header.owner.xMin -ne $pageShell.owner.xMin -or
      $header.owner.xMax -ne $pageShell.owner.xMax -or
      $leftDelta -gt $edgeTolerance -or $rightDelta -gt $edgeTolerance -or
      $stageLeftInsetLogical -lt 6 -or $stageLeftInsetLogical -gt 10 -or
      $stageRightInsetLogical -lt 6 -or $stageRightInsetLogical -gt 10 -or
      $minimumStageSideBand -lt $minimumStageSideBandRequired -or
      $maximumStageSideBand -gt 8 -or
      $stageTopBand -lt 1 -or $stageTopBand -gt 3 -or
      $maximumStandardBand -gt $maximumStandardBandAllowed -or
      $stageHeightFraction -lt .38 -or $stageHeightFraction -gt .43 -or
      $stage.owner.yMax -gt $phaseWaveRowRect.yMin -or
      $phaseWaveRowRect.yMax -gt $context.owner.yMin) {
    throw ('Battle structural hierarchy mismatch: ' +
      ([ordered]@{ header = $header; pageShell = $pageShell; gameplayStage = $stage; phaseWaveRow = $phaseWaveRowRect;
        contextTray = $context; nurseryTray = $nursery; leftDelta = $leftDelta; rightDelta = $rightDelta;
        stageHeightFraction = $stageHeightFraction; stageLeftInsetLogical = $stageLeftInsetLogical; stageRightInsetLogical = $stageRightInsetLogical;
        minimumStageSideBand = $minimumStageSideBand; maximumStageSideBand = $maximumStageSideBand;
        stageTopBand = $stageTopBand;
        maximumStandardBand = $maximumStandardBand;
        minimumStageSideBandRequired = $minimumStageSideBandRequired;
        maximumStandardBandAllowed = $maximumStandardBandAllowed } |
        ConvertTo-Json -Depth 8 -Compress))
  }
  $refreshLowerMarginLogical = [Math]::Round((($mappedDesignBounds.yMax - $refreshActionRect.yMax) / $referenceScale), 3)
  $pageShellLowerMarginLogical = [Math]::Round((($pageShellRect.yMax - $refreshActionRect.yMax) / $referenceScale), 3)
  if ($refreshLowerMarginLogical -lt 8 -or $refreshLowerMarginLogical -gt 40) {
    throw "Battle refresh lower margin is outside 8..40 logical points: $refreshLowerMarginLogical"
  }
  if ($pageShellLowerMarginLogical -lt 12 -or $pageShellLowerMarginLogical -gt 16) {
    throw "Battle PageShell closeout is outside 12..16 logical points: $pageShellLowerMarginLogical"
  }
  return [ordered]@{
    passed = $true; edgeToleranceCapturePixels = $edgeTolerance; alignedOwnerTrack = $true
    leftEdgeDeltaCapturePixels = $leftDelta; rightEdgeDeltaCapturePixels = $rightDelta
    stageInsetLogical = [ordered]@{ left = $stageLeftInsetLogical; right = $stageRightInsetLogical }
    stageSideSoilBandRangeCapturePixels = [ordered]@{
      minimum = $minimumStageSideBandRequired; maximum = 8
    }
    stageTopSoilBandRangeCapturePixels = [ordered]@{ minimum = 1; maximum = 3 }
    standardFrameBandMaximumCapturePixels = $maximumStandardBandAllowed
    structuralFrameCount = 3
    pageShellDetected = $true; legacyFlatFrameDetected = $false; stageHeightFraction = $stageHeightFraction
    refreshLowerMarginLogical = $refreshLowerMarginLogical; pageShellLowerMarginLogical = $pageShellLowerMarginLogical
    header = $header; pageShell = $pageShell; gameplayStage = $stage
    phaseWaveRow = $phaseWaveRowRect; contextTray = $context; nurseryTray = $nursery
  }
}

function Test-UiTextInkPixel {
  param([object]$Pixel, [ValidateSet('brown-ink', 'inverse-light')][string]$Palette)
  if ($Palette -eq 'inverse-light') {
    return $Pixel.A -gt 200 -and $Pixel.R -ge 225 -and
      $Pixel.G -ge 220 -and $Pixel.B -ge 200 -and
      ([Math]::Max($Pixel.R, [Math]::Max($Pixel.G, $Pixel.B)) -
       [Math]::Min($Pixel.R, [Math]::Min($Pixel.G, $Pixel.B))) -le 45
  }
  $coreBrown = $Pixel.A -gt 200 -and $Pixel.R -ge 60 -and $Pixel.R -le 160 -and
    $Pixel.G -ge 35 -and $Pixel.G -le 130 -and $Pixel.B -ge 15 -and
    $Pixel.B -le 90 -and $Pixel.R -gt ($Pixel.G + 10) -and
    $Pixel.G -gt ($Pixel.B + 5)
  # Brown display/readout antialiasing blends the text token into light surfaces.
  # Keep the blended band above darker structural rails.
  $blendedBrown = $Pixel.A -gt 200 -and $Pixel.R -ge 145 -and $Pixel.R -le 225 -and
    $Pixel.G -ge 115 -and $Pixel.G -le 190 -and $Pixel.B -ge 85 -and
    $Pixel.B -le 160 -and $Pixel.R -gt ($Pixel.G + 15) -and
    $Pixel.G -gt ($Pixel.B + 10)
  return $coreBrown -or $blendedBrown
}

function Get-TextOwnerEvidence {
  param([string]$Path, [object]$Owner, [string]$Name,
    [int]$MinimumInsidePixels = 2,
    [ValidateSet('brown-ink', 'inverse-light')][string]$Palette = 'brown-ink')
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
    [ordered]@{ path = $ReadyPath; name = 'phase-status'; rect = $phaseStatusOwner; minimum = 4 },
    [ordered]@{ path = $ReadyPath; name = 'tool-title'; rect = $toolTitleOwner; minimum = 8 },
    [ordered]@{ path = $ReadyPath; name = 'nursery-title'; rect = $nurseryTitleOwner; minimum = 8 },
    [ordered]@{ path = $ReadyPath; name = 'refresh-action'; rect = $refreshTextOwner; minimum = 12; palette = 'inverse-light' },
    [ordered]@{ path = $DetailPath; name = 'detail-title'; rect = $detailTitleOwner; minimum = 8; palette = 'brown-ink' },
    [ordered]@{ path = $DetailPath; name = 'detail-body'; rect = $detailBodyOwner; minimum = 2; palette = 'brown-ink' }
  )
  $measurements = @()
  foreach ($owner in $owners) {
    $measurements += Get-TextOwnerEvidence -Path $owner.path -Owner $owner.rect `
      -Name $owner.name -MinimumInsidePixels $owner.minimum `
      -Palette $(if ($owner.palette) { $owner.palette } else { 'brown-ink' })
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
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $phaseWaveRowRect -Name 'phase-wave-row'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $contextTrayRect -Name 'context-tray-tools'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $nurseryTrayRect -Name 'nursery-tray'),
    (Get-OccupiedRegionEvidence -Path $ReadyPath -Region $refreshActionRect -Name 'refresh-action'),
    (Get-OccupiedRegionEvidence -Path $DetailPath -Region $detailRegion -Name 'contextual-detail')
  )
  $chromeAnatomy = @()
  $anatomyCatalog = @(
    [ordered]@{ rects = $headerMetricRects; prefix = 'metric-capsule' },
    [ordered]@{ rects = $headerCompactControlRects; prefix = 'yellow-header-control' },
    [ordered]@{ rects = $toolRecipeRects; prefix = 'recipe-card' },
    [ordered]@{ rects = $nurserySlotRects; prefix = 'dashed-nursery-slot' })
  foreach ($catalog in $anatomyCatalog) {
    for ($index = 0; $index -lt $catalog.rects.Count; $index++) {
      $chromeAnatomy += Get-OccupiedRegionEvidence -Path $ReadyPath -Region $catalog.rects[$index] -Name "$($catalog.prefix)-$index"
    }
  }
  if ($headerMetricRects.Count -ne 3 -or
      $headerCompactControlRects.Count -ne 2 -or
      $toolRecipeRects.Count -ne 4 -or
      $nurserySlotRects.Count -ne 5) {
    throw 'Battle chrome anatomy catalog must remain 3 metrics / 2 controls / 4 recipes / 5 nursery slots.'
  }
  $occupiedSpan = ($refreshActionRect.yMax - $headerPanelRect.yMin) /
    [double]($mappedDesignBounds.yMax - $mappedDesignBounds.yMin)
  if ($occupiedSpan -lt .90) {
    throw "Battle occupied vertical span is below 90%: $occupiedSpan"
  }
  return [ordered]@{
    passed = $true
    requiredVerticalSpanFraction = .90
    occupiedVerticalSpanFraction = $occupiedSpan
    stateCoverage = @('ready', 'plant-detail')
    contextModes = [ordered]@{ ready = 'tools'; plantDetail = 'selected-detail' }
    regions = $regions
    chromeAnatomy = [ordered]@{ metricCapsules = 3; yellowHeaderControls = 2;
      recipeCards = 4; lineFreeNurserySlots = 5; evidence = $chromeAnatomy }
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

function Get-BattleStageBackdropFitEvidence {
  param(
    [string]$Path,
    [object]$StageRect
  )
  Add-Type -AssemblyName System.Drawing
  $bitmap = [Drawing.Bitmap]::FromFile($Path)
  try {
    $stageXMin = [Math]::Max(0, [int]$StageRect.xMin)
    $stageYMin = [Math]::Max(0, [int]$StageRect.yMin)
    $stageXMax = [Math]::Min($bitmap.Width, [int]$StageRect.xMax)
    $stageYMax = [Math]::Min($bitmap.Height, [int]$StageRect.yMax)
    $railDepth = [Math]::Max(1, [int][Math]::Ceiling(8 * $referenceScale))
    $gridGutterDepth = [Math]::Max($railDepth + 1,
      [int][Math]::Ceiling(14 * $referenceScale))
    $sampleXMin = $stageXMin + [int][Math]::Floor(($stageXMax - $stageXMin) * .25)
    $sampleXMax = $stageXMin + [int][Math]::Ceiling(($stageXMax - $stageXMin) * .75)
    $measureBand = {
      param([int]$yMin, [int]$yMax)
      $terrainPixels = 0
      $totalPixels = 0
      for ($y = $yMin; $y -lt $yMax; $y++) {
        for ($x = $sampleXMin; $x -lt $sampleXMax; $x++) {
          $pixel = $bitmap.GetPixel($x, $y)
          $totalPixels++
          $warmTerrain = $pixel.A -gt 200 -and
            $pixel.R -ge 55 -and $pixel.R -le 185 -and
            $pixel.G -ge 25 -and $pixel.G -le 145 -and
            $pixel.B -ge 10 -and $pixel.B -le 110 -and
            $pixel.R -gt ($pixel.G + 8) -and
            $pixel.G -gt ($pixel.B + 3)
          if ($warmTerrain) { $terrainPixels++ }
        }
      }
      $minimumPixels = [Math]::Max(1, [int][Math]::Floor($totalPixels * .35))
      return [ordered]@{
        yMin = $yMin
        yMax = $yMax
        totalPixels = $totalPixels
        terrainPixels = $terrainPixels
        minimumTerrainPixels = $minimumPixels
        passed = $terrainPixels -ge $minimumPixels
      }
    }
    $top = & $measureBand ($stageYMin + $railDepth) `
      ($stageYMin + $gridGutterDepth)
    $bottom = & $measureBand ($stageYMax - $gridGutterDepth) `
      ($stageYMax - $railDepth)
    return [ordered]@{
      passed = $top.passed -and $bottom.passed
      policy = 'base terrain fills both vertical aspect-ratio gutters up to the 8pt stage-mask opening'
      sampleXMin = $sampleXMin
      sampleXMax = $sampleXMax
      railDepthCapturePixels = $railDepth
      gridGutterDepthCapturePixels = $gridGutterDepth
      top = $top
      bottom = $bottom
    }
  }
  finally {
    $bitmap.Dispose()
  }
}

function Get-BattleStageContainmentEvidence {
  param(
    [string]$ReferencePath,
    [string]$CandidatePath,
    [object]$StageRect,
    [object]$ConnectorSource,
    [object]$ConnectorTarget,
    [int]$ChannelThreshold = 10
  )
  Add-Type -AssemblyName System.Drawing
  $reference = [Drawing.Bitmap]::FromFile($ReferencePath)
  $candidate = [Drawing.Bitmap]::FromFile($CandidatePath)
  try {
    if ($reference.Width -ne $candidate.Width -or
        $reference.Height -ne $candidate.Height) {
      throw "Battle stage containment dimensions do not match: $ReferencePath / $CandidatePath"
    }
    $stageXMin = [Math]::Max(0, [int]$StageRect.xMin)
    $stageYMin = [Math]::Max(0, [int]$StageRect.yMin)
    $stageXMax = [Math]::Min($reference.Width, [int]$StageRect.xMax)
    $stageYMax = [Math]::Min($reference.Height, [int]$StageRect.yMax)
    $railDepth = [Math]::Max(1, [int][Math]::Ceiling(8 * $referenceScale))
    $openingBandDepth = [Math]::Max(1, [int][Math]::Ceiling(2 * $referenceScale))
    $guardDepth = [Math]::Max(4, [int][Math]::Ceiling(16 * $referenceScale))
    $connectorRadius = [Math]::Max(3, [int][Math]::Ceiling(6 * $referenceScale))
    $scanXMin = [Math]::Max(0, $stageXMin - $guardDepth)
    $scanYMin = [Math]::Max(0, $stageYMin - $guardDepth)
    $scanXMax = [Math]::Min($reference.Width, $stageXMax + $guardDepth)
    $scanYMax = [Math]::Min($reference.Height, $stageYMax + $guardDepth)
    [double]$segmentX = $ConnectorTarget.x - $ConnectorSource.x
    [double]$segmentY = $ConnectorTarget.y - $ConnectorSource.y
    [double]$segmentLengthSquared = $segmentX * $segmentX + $segmentY * $segmentY
    [double]$connectorRadiusSquared = $connectorRadius * $connectorRadius
    $railChangedPixels = 0
    $approvedConnectorRailPixels = 0
    $openingContactChangedPixels = 0
    $stageInteriorChangedPixels = 0
    $outsideStageChangedPixels = 0
    $approvedConnectorPixels = 0
    $unexpectedOutsidePixels = 0
    for ($y = $scanYMin; $y -lt $scanYMax; $y++) {
      for ($x = $scanXMin; $x -lt $scanXMax; $x++) {
        $left = $reference.GetPixel($x, $y)
        $right = $candidate.GetPixel($x, $y)
        $delta = [Math]::Max([Math]::Abs([int]$left.R - [int]$right.R),
          [Math]::Max([Math]::Abs([int]$left.G - [int]$right.G),
            [Math]::Abs([int]$left.B - [int]$right.B)))
        if ($delta -le $ChannelThreshold) { continue }
        [double]$pointX = $x + .5
        [double]$pointY = $y + .5
        [double]$segmentT = if ($segmentLengthSquared -le .0001) { 0.0 } else {
          (($pointX - $ConnectorSource.x) * $segmentX +
            ($pointY - $ConnectorSource.y) * $segmentY) / $segmentLengthSquared
        }
        $segmentT = [Math]::Max(0.0, [Math]::Min(1.0, $segmentT))
        [double]$nearestX = $ConnectorSource.x + $segmentT * $segmentX
        [double]$nearestY = $ConnectorSource.y + $segmentT * $segmentY
        [double]$distanceX = $pointX - $nearestX
        [double]$distanceY = $pointY - $nearestY
        $insideConnectorCorridor = $distanceX * $distanceX + $distanceY * $distanceY `
          -le $connectorRadiusSquared
        $insideStage = $x -ge $stageXMin -and $x -lt $stageXMax -and
          $y -ge $stageYMin -and $y -lt $stageYMax
        if ($insideStage) {
          $edgeDepth = [Math]::Min($x - $stageXMin,
            [Math]::Min(($stageXMax - 1) - $x,
              [Math]::Min($y - $stageYMin, ($stageYMax - 1) - $y)))
          $insideRail = $x -lt $stageXMin + $railDepth -or
            $x -ge $stageXMax - $railDepth -or
            $y -lt $stageYMin + $railDepth -or
            $y -ge $stageYMax - $railDepth
          if ($insideRail -and $insideConnectorCorridor) {
            $approvedConnectorRailPixels++
          }
          elseif ($insideRail) { $railChangedPixels++ }
          else {
            $stageInteriorChangedPixels++
            if (-not $insideConnectorCorridor -and
                $edgeDepth -lt $railDepth + $openingBandDepth) {
              $openingContactChangedPixels++
            }
          }
          continue
        }

        $outsideStageChangedPixels++
        if ($insideConnectorCorridor) {
          $approvedConnectorPixels++
        }
        else {
          $unexpectedOutsidePixels++
        }
      }
    }
    $noiseAllowance = [Math]::Max(1,
      [int][Math]::Ceiling(4 * $referenceScale * $referenceScale))
    $minimumInteriorChange = [Math]::Max(8,
      [int][Math]::Floor(24 * $referenceScale * $referenceScale))
    $minimumConnectorChange = [Math]::Max(2,
      [int][Math]::Floor(8 * $referenceScale * $referenceScale))
    $minimumOpeningContact = 1
    $passed = $railChangedPixels -le $noiseAllowance -and
      $unexpectedOutsidePixels -le $noiseAllowance -and
      $stageInteriorChangedPixels -ge $minimumInteriorChange -and
      $openingContactChangedPixels -ge $minimumOpeningContact -and
      $approvedConnectorPixels -ge $minimumConnectorChange
    return [ordered]@{
      passed = $passed
      policy = 'final pixels protect the 8pt visible stage rail, touch its opening band, and permit only the cross-region connector outside the stage'
      channelThreshold = $ChannelThreshold
      stage = [ordered]@{ xMin = $stageXMin; yMin = $stageYMin; xMax = $stageXMax; yMax = $stageYMax }
      scan = [ordered]@{ xMin = $scanXMin; yMin = $scanYMin; xMax = $scanXMax; yMax = $scanYMax }
      railDepthCapturePixels = $railDepth
      openingBandDepthCapturePixels = $openingBandDepth
      guardDepthCapturePixels = $guardDepth
      connectorRadiusCapturePixels = $connectorRadius
      railChangedPixels = $railChangedPixels
      approvedConnectorRailPixels = $approvedConnectorRailPixels
      openingContactChangedPixels = $openingContactChangedPixels
      stageInteriorChangedPixels = $stageInteriorChangedPixels
      outsideStageChangedPixels = $outsideStageChangedPixels
      approvedConnectorPixels = $approvedConnectorPixels
      unexpectedOutsidePixels = $unexpectedOutsidePixels
      noiseAllowancePixels = $noiseAllowance
      minimumInteriorChangePixels = $minimumInteriorChange
      minimumConnectorChangePixels = $minimumConnectorChange
      minimumOpeningContactPixels = $minimumOpeningContact
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
          [Math]::Abs([int]$pixel.R - 107) -le 18 -and
            [Math]::Abs([int]$pixel.G - 63) -le 18 -and
            [Math]::Abs([int]$pixel.B - 18) -le 18
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
            $pixel.G -gt $pixel.R
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

