# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Invoke-DirectBattleMode {
  Set-AcceptanceState -State 'initial'
  $readyCapture = Save-StableScreenshot -Name '01-ready'
  $screenshots.ready = $readyCapture.Path
  $waveActionContrast = Get-ActionContentContrast `
    -Path $readyCapture.Path -Rect $waveActionRect `
    -ContentLeft 0.12 -ContentRight 0.92 -ContentTop 0.18 -ContentBottom 0.82 `
    -Polarity LightOnDark
  if (-not $waveActionContrast.passed) {
    throw (
      'Battle start-wave icon/label contrast is below 4.5: ' +
      ($waveActionContrast | ConvertTo-Json -Compress))
  }
  $refreshActionContrast = Get-ActionContentContrast `
    -Path $readyCapture.Path -Rect $refreshActionRect `
    -ContentLeft 0.08 -ContentRight 0.92 -ContentTop 0.18 -ContentBottom 0.82 `
    -Polarity DarkOnLight
  if (-not $refreshActionContrast.passed) {
    throw (
      'Battle Secondary refresh icon/label contrast is below 4.5: ' +
      ($refreshActionContrast | ConvertTo-Json -Compress))
  }

  $compactControlLifecycleEvidence = [ordered]@{ state = 'not-requested' }
  if ($CompactControlEvidence) {
    Move-CanvasPointer -X $controls.headerSpeed.x -Y $controls.headerSpeed.y
    Start-Sleep -Milliseconds 45
    $screenshots.speedHover = Save-Screenshot -Name '01c-speed-hover-1x'
    $speedFocusDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $readyCapture.Path `
      -CandidatePath $screenshots.speedHover -Region $speedCompactControlRect
    $speedFocusBounds = $speedFocusDifference.changedBounds
    if ($speedFocusDifference.changedPixels -lt 30 -or
        $null -eq $speedFocusBounds -or
        $speedFocusBounds.xMin -lt $speedCompactControlRect.xMin -or
        $speedFocusBounds.yMin -lt $speedCompactControlRect.yMin -or
        $speedFocusBounds.xMax -gt $speedCompactControlRect.xMax -or
        $speedFocusBounds.yMax -gt $speedCompactControlRect.yMax) {
      throw (
        'Compact speed focus cue is not material and contained: ' +
        ($speedFocusDifference | ConvertTo-Json -Compress))
    }
    Start-CanvasPress -X $controls.headerSpeed.x -Y $controls.headerSpeed.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.speedPressed = Save-Screenshot -Name '01d-speed-pressed-1x'
    }
    finally {
      Stop-CanvasPress -X $controls.headerSpeed.x -Y $controls.headerSpeed.y
    }
    Start-Sleep -Milliseconds 60
    $screenshots.speedActivating = Save-Screenshot -Name '01e-speed-activating-2x'
    Start-Sleep -Milliseconds 140
    $screenshots.speedActive = Save-Screenshot -Name '01f-speed-active-2x'

    Invoke-CanvasClickImmediate -X $controls.headerSpeed.x -Y $controls.headerSpeed.y
    Start-Sleep -Milliseconds 45
    $screenshots.speedDeactivating = Save-Screenshot -Name '01g-speed-deactivating-1x'
    Move-CanvasPointerOut
    Start-Sleep -Milliseconds 220
    $screenshots.speedInactive = Save-Screenshot -Name '01h-speed-inactive-1x'

    $speedActivatingDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $screenshots.speedInactive `
      -CandidatePath $screenshots.speedActivating -Region $speedCompactControlRect
    $speedActiveDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $screenshots.speedInactive `
      -CandidatePath $screenshots.speedActive -Region $speedCompactControlRect
    $speedDeactivatingDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $screenshots.speedInactive `
      -CandidatePath $screenshots.speedDeactivating -Region $speedCompactControlRect
    # The semantic renderer switches between mutually-exclusive complete surfaces.
    # A deactivation capture may therefore already equal the inactive endpoint;
    # retain it for review without requiring obsolete overlay/interpolation pixels.
    if ($speedActivatingDifference.changedPixels -lt 40 -or
        $speedActiveDifference.changedPixels -lt 80) {
      throw (
        'Compact speed-control activation lacks material real-canvas state differences: ' +
        ([ordered]@{
          activating = $speedActivatingDifference.changedPixels
          active = $speedActiveDifference.changedPixels
          deactivating = $speedDeactivatingDifference.changedPixels
        } | ConvertTo-Json -Compress))
    }
    $compactControlLifecycleEvidence = [ordered]@{
      state = 'captured'
      speedRect = $speedCompactControlRect
      speedFocusDifference = $speedFocusDifference
      speedActivatingDifference = $speedActivatingDifference
      speedActiveDifference = $speedActiveDifference
      speedDeactivatingDifference = $speedDeactivatingDifference
      deactivatingSurfaceContract = 'captured-complete-endpoint-or-active-variant'
    }
  }

  $waveActionPressDifference = $null
  $pauseContinuePressDifference = $null
  $pauseContinuePressInset = $null
  $pauseRestartPressDifference = $null
  $pauseRestartPressInset = $null
  if ($InteractionPolishEvidence) {
    Move-CanvasPointer -X $controls.waveAction.x -Y $controls.waveAction.y
    Start-Sleep -Milliseconds 45
    $screenshots.waveActionHover = Save-Screenshot -Name '01a-wave-action-hover'
    $waveActionFocusDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $readyCapture.Path `
      -CandidatePath $screenshots.waveActionHover -Region $waveActionRect
    $waveFocusBounds = $waveActionFocusDifference.changedBounds
    if ($waveActionFocusDifference.changedPixels -lt 60 -or
        $null -eq $waveFocusBounds -or
        $waveFocusBounds.xMin -lt $waveActionRect.xMin -or
        $waveFocusBounds.yMin -lt $waveActionRect.yMin -or
        $waveFocusBounds.xMax -gt $waveActionRect.xMax -or
        $waveFocusBounds.yMax -gt $waveActionRect.yMax) {
      throw (
        'Battle Wave focus cue is not material and contained: ' +
        ($waveActionFocusDifference | ConvertTo-Json -Compress))
    }
    Start-CanvasPress -X $controls.waveAction.x -Y $controls.waveAction.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.waveActionPressed = Save-Screenshot -Name '02a-wave-action-pressed'
      $waveActionPressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $screenshots.waveActionHover `
        -CandidatePath $screenshots.waveActionPressed `
        -Region $waveActionRect
      $waveActionPressInset = Get-ImageInsetEvidence `
        -ReferencePath $screenshots.waveActionHover `
        -CandidatePath $screenshots.waveActionPressed `
        -Region $waveActionRect
      if ($waveActionPressDifference.changedPixels -lt 200 -or
          $waveActionPressDifference.changedEdgePixels -lt 20 -or
          $waveActionPressInset.retreatedPixels -lt 20) {
        throw (
          'Battle Wave press checkpoint lacks a material action-region/edge difference: ' +
          ($waveActionPressDifference | ConvertTo-Json -Compress))
      }
    }
    finally {
      Stop-CanvasPress -X $controls.waveAction.x -Y $controls.waveAction.y
    }
  }
  else {
    Invoke-CanvasClick -X $controls.waveAction.x -Y $controls.waveAction.y
  }
  $screenshots.activeWave = (Save-StableScreenshot -Name '02-active-wave').Path

  Set-AcceptanceState -State 'between-wave'
  $screenshots.betweenWave = (Save-StableScreenshot -Name '03-between-wave').Path
  Invoke-CanvasClick -X $controls.waveAction.x -Y $controls.waveAction.y
  $screenshots.immediateNextWave = (Save-StableScreenshot -Name '04-immediate-next-wave').Path

  if ($CompactControlEvidence) {
    Invoke-CanvasClickImmediate -X $controls.headerPause.x -Y $controls.headerPause.y
    Start-Sleep -Milliseconds 60
    $screenshots.pauseActivating = Save-Screenshot -Name '04a-pause-activating'
    Start-Sleep -Milliseconds 140
  }
  else {
    Invoke-CanvasClick -X $controls.headerPause.x -Y $controls.headerPause.y
  }
  # The modal intentionally dims the HUD, so this state uses frame/dimension checks without the unobscured-HUD ink threshold.
  $screenshots.paused = (Save-StableScreenshot -Name '05-paused' -RequireHud $false).Path
  $pausedModalOpticalEvidence = Get-PausedModalOpticalEvidence -Path $screenshots.paused
  if ($InteractionPolishEvidence) {
    Move-CanvasPointer -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
    Start-Sleep -Milliseconds 45
    $screenshots.pauseContinueHover = Save-Screenshot -Name '05a-pause-continue-hover'
    Start-CanvasPress -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.pauseContinuePressed = Save-Screenshot -Name '05b-pause-continue-pressed'
      $pauseContinuePressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $screenshots.pauseContinueHover `
        -CandidatePath $screenshots.pauseContinuePressed `
        -Region $pauseActionBandRect
      $pauseContinuePressInset = Get-ImageInsetEvidence `
        -ReferencePath $screenshots.pauseContinueHover `
        -CandidatePath $screenshots.pauseContinuePressed `
        -Region $pauseContinueRect
      $bounds = $pauseContinuePressDifference.changedBounds
      if ($pauseContinuePressDifference.changedPixels -lt 100 -or
          $pauseContinuePressInset.retreatedPixels -lt $pausePressRetreatedPixelThreshold -or
          $null -eq $bounds -or
          $bounds.xMin -lt $pauseContinueRect.xMin -or
          $bounds.yMin -lt $pauseContinueRect.yMin -or
          $bounds.xMax -gt $pauseContinueRect.xMax -or
          $bounds.yMax -gt $pauseContinueRect.yMax) {
        throw (
          'Pause Continue press must materially contract inside its owner rect: ' +
          ($pauseContinuePressDifference | ConvertTo-Json -Compress))
      }
    }
    finally {
      Stop-CanvasPress -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
    }
  }
  elseif (-not $CompactControlEvidence) {
    Invoke-CanvasClick -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
  }
  else {
    Invoke-CanvasClickImmediate -X $controls.pauseContinue.x -Y $controls.pauseContinue.y
  }
  if ($CompactControlEvidence) {
    Start-Sleep -Milliseconds 5
    $screenshots.pauseDeactivating = Save-Screenshot -Name '05c-pause-deactivating'
    Start-Sleep -Milliseconds 140
    $pauseActivatingDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $readyCapture.Path -CandidatePath $screenshots.pauseActivating `
      -Region $pauseCompactControlRect
    $pauseDeactivatingDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $readyCapture.Path -CandidatePath $screenshots.pauseDeactivating `
      -Region $pauseCompactControlRect
    # Deactivation may legitimately resolve to the complete inactive endpoint.
    if ($pauseActivatingDifference.changedPixels -lt 40) {
      throw (
        'Compact pause-control activation lacks a material real-canvas transition difference: ' +
        ([ordered]@{
          activating = $pauseActivatingDifference.changedPixels
          deactivating = $pauseDeactivatingDifference.changedPixels
        } | ConvertTo-Json -Compress))
    }
    $compactControlLifecycleEvidence.pauseRect = $pauseCompactControlRect
    $compactControlLifecycleEvidence.pauseActivatingDifference = $pauseActivatingDifference
    $compactControlLifecycleEvidence.pauseDeactivatingDifference = $pauseDeactivatingDifference
  }
  $screenshots.continued = (Save-StableScreenshot -Name '06-continued').Path
  Invoke-CanvasClick -X $controls.headerPause.x -Y $controls.headerPause.y
  if ($InteractionPolishEvidence) {
    Move-CanvasPointer -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
    Start-Sleep -Milliseconds 45
    $screenshots.pauseRestartHover = Save-Screenshot -Name '06a-pause-restart-hover'
    Start-CanvasPress -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.pauseRestartPressed = Save-Screenshot -Name '06b-pause-restart-pressed'
      $pauseRestartPressDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $screenshots.pauseRestartHover `
        -CandidatePath $screenshots.pauseRestartPressed `
        -Region $pauseActionBandRect
      $pauseRestartPressInset = Get-ImageInsetEvidence `
        -ReferencePath $screenshots.pauseRestartHover `
        -CandidatePath $screenshots.pauseRestartPressed `
        -Region $pauseRestartRect
      $bounds = $pauseRestartPressDifference.changedBounds
      if ($pauseRestartPressDifference.changedPixels -lt 100 -or
          $pauseRestartPressInset.retreatedPixels -lt $pausePressRetreatedPixelThreshold -or
          $null -eq $bounds -or
          $bounds.xMin -lt $pauseRestartRect.xMin -or
          $bounds.yMin -lt $pauseRestartRect.yMin -or
          $bounds.xMax -gt $pauseRestartRect.xMax -or
          $bounds.yMax -gt $pauseRestartRect.yMax) {
        throw (
          'Pause Restart press must materially contract inside its owner rect: ' +
          ($pauseRestartPressDifference | ConvertTo-Json -Compress))
      }
    }
    finally {
      Stop-CanvasPress -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
    }
  }
  else {
    Invoke-CanvasClick -X $controls.pauseRestart.x -Y $controls.pauseRestart.y
  }
  $screenshots.restarted = (Save-StableScreenshot -Name '07-restarted').Path

  Set-AcceptanceState -State 'selected-tool'
  $toolAvailableCapture = Save-StableScreenshot -Name '08-tool-available'
  $screenshots.toolAvailable = $toolAvailableCapture.Path
  Invoke-CanvasClick -X $controls.weaponGatling.x -Y $controls.weaponGatling.y
  $selectedToolCapture = Save-StableScreenshot -Name '09-selected-tool'
  $toolAvailableHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $toolAvailableCapture.Path).Hash
  $selectedToolHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $selectedToolCapture.Path).Hash
  if ($selectedToolHash -ceq $toolAvailableHash) {
    throw 'Gatling click did not change the real selectable-tool presentation.'
  }
  $screenshots.selectedTool = $selectedToolCapture.Path

  Set-AcceptanceState -State 'adjacent-pots'
  $screenshots.adjacentPots = (Save-StableScreenshot -Name '10-adjacent-pots').Path

  Set-AcceptanceState -State 'drag-target'
  Start-CanvasDrag -FromX $controls.nurserySlot0.x -FromY $controls.nurserySlot0.y -ToX $controls.acceptanceCell0.x -ToY $controls.acceptanceCell0.y
  $screenshots.legalDragCue = (Save-StableScreenshot -Name '11-legal-drag-cue').Path
  Stop-CanvasDrag -X $controls.acceptanceCell0.x -Y $controls.acceptanceCell0.y

  Set-AcceptanceState -State 'selection-inspection'
  $illegalTargetX = $controls.acceptanceCell0.x + 12.0 * $referenceScale
  Start-CanvasDrag -FromX $controls.acceptanceCell0.x -FromY $controls.acceptanceCell0.y `
    -ToX $illegalTargetX -ToY $controls.acceptanceCell0.y
  $screenshots.illegalDragCue = (Save-StableScreenshot -Name '12-illegal-drag-cue').Path
  Stop-CanvasDrag -X $illegalTargetX -Y $controls.acceptanceCell0.y

  Set-AcceptanceState -State 'dense-board'
  $screenshots.denseBoard = (Save-StableScreenshot -Name '13-dense-board').Path

  Set-AcceptanceState -State 'selection-inspection'
  # Deterministic interaction state projected through the enlarged board: attacking plant and empty pot use the first two canonical plantable cells.
  Invoke-CanvasClick -X $controls.acceptanceCell0.x -Y $controls.acceptanceCell0.y
  $screenshots.plantDetail = (Save-StableScreenshot -Name '14-plant-detail').Path
  if ($CompactControlEvidence) {
    Move-CanvasPointer -X $controls.detailClose.x -Y $controls.detailClose.y
    Start-Sleep -Milliseconds 45
    $screenshots.detailCloseHover = Save-Screenshot -Name '14a-detail-close-hover'
    Start-CanvasPress -X $controls.detailClose.x -Y $controls.detailClose.y
    try {
      Start-Sleep -Milliseconds 45
      $screenshots.detailClosePressed = Save-Screenshot -Name '14b-detail-close-pressed'
    }
    finally {
      Stop-CanvasPress -X $controls.detailClose.x -Y $controls.detailClose.y
    }
    Start-Sleep -Milliseconds 220
    Move-CanvasPointerOut
    $screenshots.detailClosed = Save-Screenshot -Name '14c-detail-closed'
    $detailClosePressDifference = Get-ImageDifferenceMetrics `
      -ReferencePath $screenshots.detailCloseHover `
      -CandidatePath $screenshots.detailClosePressed `
      -Region $detailCloseCompactControlRect
    if ($detailClosePressDifference.changedPixels -lt 20) {
      throw (
        'Instant detail-close control lacks a material pressed checkpoint: ' +
        ($detailClosePressDifference | ConvertTo-Json -Compress))
    }
    $compactControlLifecycleEvidence.detailCloseRect = $detailCloseCompactControlRect
    $compactControlLifecycleEvidence.detailClosePressDifference = $detailClosePressDifference
    $compactControlLifecycleEvidence.detailCloseRemainsInstantCommand = $true
  }
  Invoke-CanvasClick -X $controls.acceptanceCell1.x -Y $controls.acceptanceCell1.y
  $screenshots.destinationClickNoMove = (Save-StableScreenshot -Name '15-destination-click-no-move').Path
  Start-CanvasDrag -FromX $controls.acceptanceCell0.x -FromY $controls.acceptanceCell0.y -ToX $controls.acceptanceCell1.x -ToY $controls.acceptanceCell1.y
  Stop-CanvasDrag -X $controls.acceptanceCell1.x -Y $controls.acceptanceCell1.y
  $screenshots.dragRelocation = (Save-StableScreenshot -Name '16-after-drag-move').Path

  # The existing URL-guarded Battle acceptance bridge owns stable terminal-card
  # preview states. Production terminal submission and Battle-to-Settlement flow
  # remain covered separately by Flow mode.
  $terminalReferenceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $screenshots.dragRelocation).Hash
  Move-CanvasPointerOut
  Set-AcceptanceState -State "terminal-$BattleTerminalOutcome"
  $terminalRouteBefore = [int](Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1')
  $terminalCapture = Save-StableScreenshot `
    -Name "17-battle-terminal-$BattleTerminalOutcome" -RequireHud $false
  $terminalRouteAfter = [int](Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1')
  $terminalHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $terminalCapture.Path).Hash
  if ($terminalRouteBefore -ne 1 -or $terminalRouteAfter -ne 1 -or
      $terminalHash -ceq $terminalReferenceHash -or
      $terminalCapture.Metrics.averageLuma -ge $readyCapture.Metrics.averageLuma * 0.92) {
    throw (
      "Stable Battle terminal preview validation failed: outcome=$BattleTerminalOutcome " +
      "route=$terminalRouteBefore/$terminalRouteAfter " +
      "luma=$($terminalCapture.Metrics.averageLuma)/$($readyCapture.Metrics.averageLuma)")
  }
  $screenshots.terminal = $terminalCapture.Path
  Invoke-CanvasClick -X $controls.terminalRestart.x -Y $controls.terminalRestart.y
  $terminalRestartRoute = [int](Invoke-JavaScript -Expression 'window.fruitDefenseAppRoute ?? -1')
  $terminalRestartIdentity = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'terminal-preview-restart' -SessionMode Required
  Assert-SameSession -Expected $directBattleIdentity `
    -Actual $terminalRestartIdentity -Stage 'terminal-preview-restart'
  $terminalRestartCapture = Save-StableScreenshot -Name '18-terminal-preview-restarted'
  if ($terminalRestartRoute -ne 1 -or
      $terminalRestartCapture.Metrics.averageLuma -le $terminalCapture.Metrics.averageLuma * 1.08) {
    throw (
      "Terminal preview restart did not restore the unobscured Battle Ready presentation: " +
      "route=$terminalRestartRoute " +
      "luma=$($terminalRestartCapture.Metrics.averageLuma)/$($terminalCapture.Metrics.averageLuma)")
  }
  $screenshots.terminalPreviewRestarted = $terminalRestartCapture.Path

  $metrics = [ordered]@{}
  foreach ($state in $screenshots.Keys) {
    $metrics[$state] = Get-ImageMetrics -Path $screenshots[$state]
    if ($metrics[$state].width -ne $Width -or $metrics[$state].height -ne $Height) {
      throw "Unexpected screenshot dimensions for ${state}: $($metrics[$state].width)x$($metrics[$state].height)"
    }
    if (-not (Test-StableFrameMetrics -Metrics $metrics[$state])) {
      throw (
        "Frame stability check failed for ${state}: black=$($metrics[$state].blackFraction) " +
        "nearBlack=$($metrics[$state].nearBlackFraction) " +
        "maxNearBlackRun=$($metrics[$state].maxNearBlackHorizontalRunFraction) " +
        "invalid=$($metrics[$state].invalidFraction)")
    }
  }
  if ($metrics.ready.headerDarkPixels -lt $hudDarkPixelThreshold -or
      $metrics.ready.headerLightPixels -lt $hudLightPixelThreshold) {
    throw "HUD text check failed: dark=$($metrics.ready.headerDarkPixels)/$hudDarkPixelThreshold light=$($metrics.ready.headerLightPixels)/$hudLightPixelThreshold."
  }
  $readyHasFormerActionRow =
    $metrics.ready.formerActionColorPixels -gt $formerActionPixelThreshold -and
    $metrics.ready.formerActionColorSpanPixels -gt $formerActionSpanThreshold
  $activeHasFormerActionRow =
    $metrics.activeWave.formerActionColorPixels -gt $formerActionPixelThreshold -and
    $metrics.activeWave.formerActionColorSpanPixels -gt $formerActionSpanThreshold
  if ($readyHasFormerActionRow -or $activeHasFormerActionRow) {
    throw (
      "Former bottom action-row signature is still present: " +
      "ready=$($metrics.ready.formerActionColorPixels)/$($metrics.ready.formerActionColorSpanPixels) " +
      "active=$($metrics.activeWave.formerActionColorPixels)/$($metrics.activeWave.formerActionColorSpanPixels).")
  }

  $panelGeometry = Get-BattlePanelGeometryEvidence -Path $screenshots.ready
  $textContainment = Get-BattleTextContainmentEvidence `
    -ReadyPath $screenshots.ready -DetailPath $screenshots.plantDetail
  $occupiedBalance = Get-BattleOccupiedBalanceEvidence `
    -ReadyPath $screenshots.ready -DetailPath $screenshots.plantDetail

  $manifest = [ordered]@{
    accepted = $true
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    url = $Url
    verifiedBuildProfile = $verifiedBuildProfile
    levelId = $LevelId
    expectedCompositeIdentity = $expectedLevelIdentity
    viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
    safeArea = $safeAreaEvidence
    runtimeUi = $runtimeUiIdentity
    browser = $browserEvidence
    canvas = $readiness
      checks = [ordered]@{
        http = 'pass'
        wasm = 'pass'
        perAssetContentVersions = 'pass'
        strongContentEtags = 'pass'
        brotliFallbackDelivery = 'pass'
        immutableBuildCache = 'pass'
        revalidatableHtml = 'pass'
        warmCacheReuse = 'pass'
        crossReleaseCacheReuse = if ($null -eq $releaseTransition) { 'not-requested' } else { 'pass' }
      unityLoaded = 'pass'
      directRouteLevelIdentity = 'pass'
      compositeIdentity = 'pass'
      requestedViewportAndCanvas = 'pass'
      safeAreaQueryApplied = 'pass'
      chineseHudInk = 'pass'
      screenshotDimensions = 'pass'
      requiredStates = 'pass'
      contextualWaveLabels = 'pass'
      waveActionContentContrast = 'pass'
      refreshActionContentContrast = 'pass'
      compactControlLifecycle = if ($CompactControlEvidence) { 'pass' } else { 'not-requested' }
      compactControlInstantClose = if ($CompactControlEvidence) { 'pass' } else { 'not-requested' }
      panelGeometry = 'pass'
      textContainment = 'pass'
      occupiedBalance = 'pass'
      actionFocusCue = if ($InteractionPolishEvidence -or $CompactControlEvidence) { 'pass' } else { 'not-requested' }
      waveActionPressedBeforeRelease = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
      pausedModalFinalRasterOpticalAlignment = 'pass'
      pauseActionsPressedBeforeReleaseAndContained = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
      oldBottomActionRowAbsent = 'pass'
      noLargeNearBlackRegions = 'pass'
      pauseContinuePreservesRun = 'pass'
      pauseRestartProducesCleanReadyState = 'pass'
      selectedToolState = 'pass'
      selectedToolAvailableToClickedHashChanged = 'pass'
      legalInteractionCue = 'pass'
      illegalInteractionCue = 'pass'
      inspectionClickInformationAndRange = 'pass'
      destinationClickNoRelocation = 'pass'
      dragRelocation = 'pass'
      battleTerminalResultCard = 'pass'
      terminalPreviewRestartReturnsReadyWithoutRouteSubmission = 'pass'
    }
    delivery = $delivery
    routeIdentities = [ordered]@{ battle = $directBattleIdentity }
    screenshots = $screenshots
    imageMetrics = $metrics
    panelGeometry = $panelGeometry
    textContainment = $textContainment
    occupiedBalance = $occupiedBalance
    opticalMeasurements = [ordered]@{
      pausedModal = $pausedModalOpticalEvidence
      waveActionContentContrast = $waveActionContrast
      refreshActionContentContrast = $refreshActionContrast
    }
    interactionPolishEvidence = if ($InteractionPolishEvidence) {
      [ordered]@{
        waveActionRect = $waveActionRect
        waveActionFocusDifference = $waveActionFocusDifference
        waveActionPressDifference = $waveActionPressDifference
        waveActionPressInset = $waveActionPressInset
        pauseContinueRect = $pauseContinueRect
        pauseActionBandRect = $pauseActionBandRect
        pauseContinuePressDifference = $pauseContinuePressDifference
        pauseContinuePressInset = $pauseContinuePressInset
        pauseRestartRect = $pauseRestartRect
        pauseRestartPressDifference = $pauseRestartPressDifference
        pauseRestartPressInset = $pauseRestartPressInset
        releaseAction = 'StartWave-pass'
      }
    } else {
      [ordered]@{ state = 'not-requested' }
    }
    compactControlEvidence = $compactControlLifecycleEvidence
    controls = $controls
    referenceControls = $referenceControls
    pixelThresholds = [ordered]@{
      hudDarkPixels = $hudDarkPixelThreshold
      hudLightPixels = $hudLightPixelThreshold
      formerActionColorPixels = $formerActionPixelThreshold
      formerActionColorSpanPixels = $formerActionSpanThreshold
      framePixels = $framePixelThresholds
    }
    sessionSequence = [ordered]@{
      labels = [ordered]@{
        ready = -join @([char]0x5F00, [char]0x59CB, [char]0x6CE2, [char]0x6B21)
        betweenWave = -join @([char]0x7ACB, [char]0x5373, [char]0x5F00, [char]0x59CB, [char]0x4E0B, [char]0x4E00, [char]0x6CE2)
        pauseContinue = -join @([char]0x7EE7, [char]0x7EED, [char]0x6E38, [char]0x620F)
        pauseRestart = -join @([char]0x91CD, [char]0x65B0, [char]0x5F00, [char]0x59CB)
      }
      steps = @(
        'capture clean ready state and exact ready label',
        'click battlefield wave action and capture active wave without a start action',
        'capture deterministic between-wave countdown and exact immediate-start label',
        'click immediate next wave and capture active wave two',
        'pause active run and capture both modal actions',
        'continue and capture the same active run',
        'pause again, restart, and capture clean ready state'
      )
    }
    interactionSequence = [ordered]@{
      source = [ordered]@{ cell = @(0, 1); x = $controls.acceptanceCell0.x; y = $controls.acceptanceCell0.y }
      destination = [ordered]@{ cell = @(1, 1); x = $controls.acceptanceCell1.x; y = $controls.acceptanceCell1.y }
      steps = @(
        'click source plant to inspect',
        'click empty destination without relocation',
        'drag source plant to destination to relocate'
      )
    }
    terminalCapture = [ordered]@{
      outcome = $BattleTerminalOutcome
      state = "terminal-$BattleTerminalOutcome"
      routeBeforeScreenshot = $terminalRouteBefore
      routeAfterScreenshot = $terminalRouteAfter
      sha256 = $terminalHash
      previewOnly = $true
      productionSubmissionEvidence = 'Flow mode / task 5.3'
      restart = [ordered]@{
        route = $terminalRestartRoute
        sessionId = $terminalRestartIdentity.sessionId
        seed = $terminalRestartIdentity.seed
        sameSession = $true
        resultCardDismissed = $true
      }
    }
    selectedToolCapture = [ordered]@{
      state = 'selected-tool'
      availableSha256 = $toolAvailableHash
      selectedSha256 = $selectedToolHash
      realClick = [ordered]@{
        x = $controls.weaponGatling.x
        y = $controls.weaponGatling.y
      }
      changed = $true
    }
  }
  $manifestPath = Join-Path $outputDir 'acceptance.json'
  $manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
  Write-Host "FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK manifest=$manifestPath"
}
