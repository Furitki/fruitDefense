# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Invoke-FlowMode {
    Wait-AppRoute -Route 0
    $flowScreenshots = [ordered]@{}
    $flowMetrics = [ordered]@{}
    $flowIdentities = [ordered]@{}

    Invoke-CanvasClick -X $controls[$levelCardControlName].x -Y $controls[$levelCardControlName].y
    $flowIdentities.lobby = Wait-AcceptanceIdentity `
      -Route 0 -Stage 'selected-lobby' -SessionMode Cleared
    $flowScreenshots.lobby = (Save-StableScreenshot -Name '01-lobby' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    Wait-AppRoute -Route 1
    $flowIdentities.battle = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'battle' -SessionMode Required
    $flowScreenshots.battle = (Save-StableScreenshot -Name '02-battle' -RequireHud $true).Path

    Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{
      rate = $BootstrapCpuThrottlingRate
    } | Out-Null
    try {
      Invoke-AcceptanceFlowCommand -Command $SettlementOutcome
      $hiddenTelemetry = Wait-SettlementOutcomeRevealState -State settled-hidden
      Move-CanvasPointerOut
      $flowScreenshots.settlementHidden = Save-Screenshot `
        -Name "03a-settlement-hidden-$SettlementOutcome"
      $hiddenInk = Get-SettlementOutcomeInkCounts `
        -Path $flowScreenshots.settlementHidden
      # The warm-paper banner intentionally reuses pixels near both emphasis
      # colors. Hidden-state absence is therefore proven by route-bound reveal
      # telemetry plus the later paired hidden/stable final-ink delta, not by
      # treating raw palette matches in the static banner as glyph pixels.
      Release-SettlementOutcomeReveal
    }
    finally {
      Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{ rate = 1 } | Out-Null
    }
    Wait-AppRoute -Route 2
    $flowIdentities.settlement = Wait-AcceptanceIdentity `
      -Route 2 -Stage 'settlement' -SessionMode Required
    Assert-SameSession -Expected $flowIdentities.battle `
      -Actual $flowIdentities.settlement -Stage 'settlement'
    Move-CanvasPointerOut
    $stableTelemetry = Wait-SettlementOutcomeRevealState -State stable
    $flowScreenshots.settlement = (Save-StableScreenshot `
      -Name "03-settlement-$SettlementOutcome" -RequireHud $false).Path
    $settlementOpticalEvidence = Get-SettlementOpticalEvidence `
      -Path $flowScreenshots.settlement `
      -ReferencePath $flowScreenshots.settlementHidden
    $stableInk = Get-SettlementOutcomeInkCounts -Path $flowScreenshots.settlement
    $historyEntries = @($stableTelemetry.history)
    $historyTail = if ($historyEntries.Count -ge 4) {
      @($historyEntries[($historyEntries.Count - 4)..($historyEntries.Count - 1)])
    } else { $historyEntries }
    $historyStates = @($historyTail | ForEach-Object { [string]$_.state })
    $historyTailIsRouteAndSessionBound = @($historyTail | Where-Object {
      [int]$_.route -eq 2 -and
        [string]$_.sessionId -ceq [string]$stableTelemetry.identitySessionId
    }).Count -eq $historyTail.Count
    if (($historyStates -join ',') -cne 'hidden,settled-hidden,appearing,stable' -or
        -not $historyTailIsRouteAndSessionBound) {
      throw (
        'Settlement outcome reveal history is not a route/session-bound hidden-to-appearing-to-stable sequence: ' +
        ($stableTelemetry | ConvertTo-Json -Depth 8 -Compress))
    }
    $settlementRevealEvidence = [ordered]@{
      state = 'hidden-and-stable-captured'
      telemetry = [ordered]@{
        hidden = $hiddenTelemetry
        stable = $stableTelemetry
      }
      ink = [ordered]@{
        hidden = $hiddenInk
        stable = $stableInk
      }
    }

    Invoke-CanvasClick -X $controls.settlementReturn.x -Y $controls.settlementReturn.y
    Wait-AppRoute -Route 0
    $flowIdentities.returnedLobby = Wait-AcceptanceIdentity `
      -Route 0 -Stage 'returned-lobby' -SessionMode Cleared
    $flowScreenshots.returnedLobby = (Save-StableScreenshot -Name '04-returned-lobby' -RequireHud $false).Path

    Invoke-CanvasClick -X $controls.lobbyStart.x -Y $controls.lobbyStart.y
    Wait-AppRoute -Route 1
    $flowIdentities.secondBattle = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'second-battle' -SessionMode Required
    if ([string]$flowIdentities.secondBattle.sessionId -ceq [string]$flowIdentities.battle.sessionId) {
      throw 'Returning to Lobby and starting again reused the completed session ID.'
    }
    if ($InteractionPolishEvidence) {
      Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{
        rate = $BootstrapCpuThrottlingRate
      } | Out-Null
      try {
        Invoke-AcceptanceFlowCommand -Command $SettlementOutcome
        Wait-SettlementOutcomeRevealState -State settled-hidden | Out-Null
        Release-SettlementOutcomeReveal
        $appearingTelemetry = Wait-SettlementOutcomeRevealState -State appearing
        $flowScreenshots.settlementMotion = Save-Screenshot `
          -Name "03a-settlement-motion-$SettlementOutcome"
        $appearingInk = Get-SettlementOutcomeInkCounts `
          -Path $flowScreenshots.settlementMotion
      }
      finally {
        Invoke-Cdp -Method 'Emulation.setCPUThrottlingRate' -Params @{ rate = 1 } | Out-Null
      }
    } else {
      Invoke-AcceptanceFlowCommand -Command $SettlementOutcome
      Wait-SettlementOutcomeRevealState -State settled-hidden | Out-Null
      Release-SettlementOutcomeReveal
    }
    Wait-AppRoute -Route 2
    $flowIdentities.secondSettlement = Wait-AcceptanceIdentity `
      -Route 2 -Stage 'second-settlement' -SessionMode Required
    Assert-SameSession -Expected $flowIdentities.secondBattle `
      -Actual $flowIdentities.secondSettlement -Stage 'second-settlement'
    if ($InteractionPolishEvidence) {
      $secondStableTelemetry = Wait-SettlementOutcomeRevealState -State stable
      $historyEntries = @($secondStableTelemetry.history)
      $historyTail = if ($historyEntries.Count -ge 4) {
        @($historyEntries[($historyEntries.Count - 4)..($historyEntries.Count - 1)])
      } else { $historyEntries }
      $historyStates = @($historyTail | ForEach-Object { [string]$_.state })
      $historyTailIsRouteAndSessionBound = @($historyTail | Where-Object {
        [int]$_.route -eq 2 -and
          [string]$_.sessionId -ceq [string]$secondStableTelemetry.identitySessionId
      }).Count -eq $historyTail.Count
      if (($historyStates -join ',') -cne 'hidden,settled-hidden,appearing,stable' -or
          -not $historyTailIsRouteAndSessionBound) {
        throw (
          'Second settlement reveal history is not a route/session-bound hidden-to-appearing-to-stable sequence: ' +
          ($secondStableTelemetry | ConvertTo-Json -Depth 8 -Compress))
      }
      $settlementRevealEvidence.state = 'captured-across-two-deterministic-settlement-cycles'
      $settlementRevealEvidence.telemetry.appearing = $appearingTelemetry
      $settlementRevealEvidence.telemetry.secondStable = $secondStableTelemetry
      $settlementRevealEvidence.ink.appearing = $appearingInk
    }
    Invoke-CanvasClick -X $controls.settlementRetry.x -Y $controls.settlementRetry.y
    Wait-AppRoute -Route 1
    $flowIdentities.retryBattle = Wait-AcceptanceIdentity `
      -Route 1 -Stage 'retry-battle' -SessionMode Required
    Assert-FreshSession -Previous $flowIdentities.secondSettlement `
      -Actual $flowIdentities.retryBattle -Stage 'retry-battle'
    $flowScreenshots.retryBattle = (Save-StableScreenshot -Name '05-retry-battle' -RequireHud $true).Path

    foreach ($state in $flowScreenshots.Keys) {
      $flowMetrics[$state] = Get-ImageMetrics -Path $flowScreenshots[$state]
      if ($flowMetrics[$state].width -ne $Width -or $flowMetrics[$state].height -ne $Height) {
        throw "Unexpected flow screenshot dimensions for ${state}: $($flowMetrics[$state].width)x$($flowMetrics[$state].height)"
      }
      if (-not (Test-StableFrameMetrics -Metrics $flowMetrics[$state])) {
        throw (
          "Invalid flow frame for ${state}: black=$($flowMetrics[$state].blackFraction) " +
          "nearBlack=$($flowMetrics[$state].nearBlackFraction) " +
          "maxNearBlackRun=$($flowMetrics[$state].maxNearBlackHorizontalRunFraction) " +
          "invalid=$($flowMetrics[$state].invalidFraction)")
      }
    }
    $settlementMotionDifference = $null
    if ($InteractionPolishEvidence) {
      $settlementMotionDifference = Get-ImageDifferenceMetrics `
        -ReferencePath $flowScreenshots.settlementHidden `
        -CandidatePath $flowScreenshots.settlementMotion `
        -Region $settlementOutcomeInkRegion
      if ($settlementMotionDifference.changedPixels -lt 40) {
        throw (
          'Settlement outcome-only appearing checkpoint lacks a material difference from hidden: ' +
          ($settlementMotionDifference | ConvertTo-Json -Compress))
      }
    }

    $flowManifest = [ordered]@{
      accepted = $true
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      verifiedBuildProfile = $verifiedBuildProfile
      levelId = $LevelId
      settlementOutcome = $SettlementOutcome
      expectedCompositeIdentity = $expectedLevelIdentity
      viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
      safeArea = $safeAreaEvidence
      browser = $browserEvidence
      canvas = $readiness
      runtimeUi = $runtimeUiIdentity
      checks = [ordered]@{
        lobbyToBattle = 'pass'
        battleToSettlement = 'pass'
        settlementReturn = 'pass'
        settlementRetry = 'pass'
        selectedLevelLaunch = 'pass'
        compositeIdentityPerRoute = 'pass'
        settlementSessionPreserved = 'pass'
        returnSelectionPreserved = 'pass'
        retryFreshSessionAndSeed = 'pass'
        settlementMotionCheckpoint = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
        settlementOutcomeHiddenBeforeReveal = 'pass'
        settlementOutcomeFillOutlineAppearTogether = if ($InteractionPolishEvidence) { 'pass' } else { 'not-requested' }
        settlementOutcomeStableComplete = 'pass'
        settlementOutcomeOpticalContainment = 'pass'
        settlementOutcomeTrueOutline = 'pass'
        settlementOutcomeExactOutlineThickness = 'pass'
        settlementOutcomeCompleteFinalInk = 'pass'
        settlementOutcomeFinalInkHeight = 'pass'
        settlementOutcomeVerticalOccupancy = 'pass'
        settlementOutcomeFourSidePadding = 'pass'
        settlementOutcomeTopBottomBalance = 'pass'
        settlementReadOnlyMetricsBorderless = 'pass'
        requestedViewportAndCanvas = 'pass'
        safeAreaQueryApplied = 'pass'
        noBlackOrTransparentFrames = 'pass'
        noLargeNearBlackRegions = 'pass'
        perAssetContentVersions = 'pass'
        strongContentEtags = 'pass'
        warmCacheReuse = 'pass'
        crossReleaseCacheReuse = if ($null -eq $releaseTransition) { 'not-requested' } else { 'pass' }
      }
      delivery = $delivery
      routeIdentities = $flowIdentities
      screenshots = $flowScreenshots
      imageMetrics = $flowMetrics
      opticalMeasurements = [ordered]@{
        settlement = $settlementOpticalEvidence
        settlementReveal = $settlementRevealEvidence
      }
      interactionPolishEvidence = if ($InteractionPolishEvidence) {
        [ordered]@{
          settlementMotionDifference = $settlementMotionDifference
          settlementReveal = $settlementRevealEvidence
        }
      } else {
        [ordered]@{ state = 'not-requested' }
      }
      pixelThresholds = [ordered]@{
        framePixels = $framePixelThresholds
        hudDarkPixels = $hudDarkPixelThreshold
        hudLightPixels = $hudLightPixelThreshold
      }
      controls = $controls
      referenceControls = $referenceControls
    }
    $flowManifestPath = Join-Path $outputDir 'flow-acceptance.json'
    $flowManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $flowManifestPath -Encoding UTF8
    Write-Host "FRUIT_DEFENSE_FLOW_ACCEPTANCE_OK manifest=$flowManifestPath"
    return
}
