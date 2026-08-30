# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Invoke-AcceptanceSelfCheck {
  $syntheticHealthyMetrics = [pscustomobject]@{
    invalidFraction = 0.0
    blackFraction = 0.0
    nearBlackFraction = 0.0005
    maxNearBlackHorizontalRunFraction = 0.01
  }
  $syntheticBlackBlockMetrics = [pscustomobject]@{
    invalidFraction = 0.02
    blackFraction = 0.02
    nearBlackFraction = 0.03
    maxNearBlackHorizontalRunFraction = 0.25
  }
  $syntheticNearBlackBlockMetrics = [pscustomobject]@{
    invalidFraction = 0.0
    blackFraction = 0.0
    nearBlackFraction = 0.06
    maxNearBlackHorizontalRunFraction = 0.25
  }
  if (-not (Test-StableFrameMetrics -Metrics $syntheticHealthyMetrics) -or
      (Test-StableFrameMetrics -Metrics $syntheticBlackBlockMetrics) -or
      (Test-StableFrameMetrics -Metrics $syntheticNearBlackBlockMetrics)) {
    throw 'Black-frame stability guard self-check failed.'
  }
  $syntheticOutlineEvidence = Get-SyntheticSettlementOutcomeOutlineEvidence
  if ($syntheticOutlineEvidence.outline.expectedThicknessCapturePixels -ne 2 -or
      $syntheticOutlineEvidence.outline.maximumConnectedThicknessCapturePixels -ne 2) {
    throw (
      'Settlement outline self-check requires and must accept an exact 2px synthetic outline: ' +
      ($syntheticOutlineEvidence.outline | ConvertTo-Json -Depth 8 -Compress))
  }
  $thickOutlineRejected = $false
  try {
    Get-SyntheticSettlementOutcomeOutlineEvidence -ThickOutline | Out-Null
  }
  catch {
    if ($_.Exception.Message -notmatch 'anti-alias fringe|thickness') {
      throw "Synthetic 3px outline failed for the wrong reason: $($_.Exception.Message)"
    }
    $thickOutlineRejected = $true
  }
  if (-not $thickOutlineRejected) {
    throw 'Settlement outline guard accepted a synthetic 3px outline.'
  }
  $rejectedThinOutlineSides = @()
  foreach ($thinSide in @('left', 'top', 'right', 'bottom')) {
    $rejected = $false
    try {
      Get-SyntheticSettlementOutcomeOutlineEvidence -ThinSide $thinSide | Out-Null
    }
    catch {
      if ($_.Exception.Message -notmatch 'direct-cardinal|retains too little') {
        throw "Synthetic 1px '$thinSide' side failed for the wrong reason: $($_.Exception.Message)"
      }
      $rejected = $true
    }
    if (-not $rejected) {
      throw "Settlement outline guard accepted a synthetic 1px '$thinSide' side."
    }
    $rejectedThinOutlineSides += $thinSide
  }
  $rejectedResidualOutlineSides = @()
  foreach ($residualSide in @('left', 'top', 'right', 'bottom')) {
    $rejected = $false
    try {
      Get-SyntheticSettlementOutcomeOutlineEvidence `
        -ResidualSide $residualSide | Out-Null
    }
    catch {
      if ($_.Exception.Message -notmatch 'direct-cardinal|retains too little') {
        throw "Synthetic residual '$residualSide' side failed for the wrong reason: $($_.Exception.Message)"
      }
      $rejected = $true
    }
    if (-not $rejected) {
      throw "Settlement outline guard accepted a local residual on the '$residualSide' side."
    }
    $rejectedResidualOutlineSides += $residualSide
  }
  $rejectedGappedOutlineSides = @()
  foreach ($gappedSide in @('left', 'top', 'right', 'bottom')) {
    $rejected = $false
    try {
      Get-SyntheticSettlementOutcomeOutlineEvidence `
        -GappedSide $gappedSide | Out-Null
    }
    catch {
      if ($_.Exception.Message -notmatch 'direct-cardinal|retains too little') {
        throw "Synthetic gapped '$gappedSide' side failed for the wrong reason: $($_.Exception.Message)"
      }
      $rejected = $true
    }
    if (-not $rejected) {
      throw "Settlement outline guard accepted a locally gapped '$gappedSide' side."
    }
    $rejectedGappedOutlineSides += $gappedSide
  }
  $detachedOutlineRejected = $false
  try {
    Get-SyntheticSettlementOutcomeOutlineEvidence -DetachedFragment | Out-Null
  }
  catch {
    if ($_.Exception.Message -notmatch 'disconnected from fill') {
      throw "Detached outline candidate failed for the wrong reason: $($_.Exception.Message)"
    }
    $detachedOutlineRejected = $true
  }
  if (-not $detachedOutlineRejected) {
    throw 'Settlement outline guard accepted a detached outline candidate fragment.'
  }
  $uncoveredOutlineCandidateRejected = $false
  try {
    Get-SyntheticSettlementOutcomeOutlineEvidence -UncoveredCandidate | Out-Null
  }
  catch {
    if ($_.Exception.Message -notmatch 'excluded from final ink') {
      throw "Uncovered outline candidate failed for the wrong reason: $($_.Exception.Message)"
    }
    $uncoveredOutlineCandidateRejected = $true
  }
  if (-not $uncoveredOutlineCandidateRejected) {
    throw 'Settlement outline guard accepted an outline candidate missing from the independent final-ink mask.'
  }
  foreach ($controlName in @(
      'lobbyLevelOrchard01', 'lobbyLevelOrchard02', 'lobbyLevelOrchard03',
      'lobbyStart', 'settlementRetry', 'settlementReturn')) {
    $point = $controls[$controlName]
    if ($point.x -lt 0 -or $point.x -gt $Width -or
        $point.y -lt $SafeTop -or $point.y -gt ($Height - $SafeBottom)) {
      throw "Mapped shell control escapes safe content: $controlName=$($point | ConvertTo-Json -Compress)"
    }
  }
  if (-not ($controls.lobbyLevelOrchard01.y -lt $controls.lobbyLevelOrchard02.y -and
      $controls.lobbyLevelOrchard02.y -lt $controls.lobbyLevelOrchard03.y -and
      $controls.lobbyLevelOrchard03.y -lt $controls.lobbyStart.y)) {
    throw 'Mapped Lobby card and Start controls are not ordered.'
  }
  if ($expectedLevelIdentity.Count -ne 5) {
    throw "Expected composite identity is incomplete for level '$LevelId'."
  }
  $syntheticBattleIdentity = [pscustomobject]@{
    route = 1; routeName = 'battle'; sessionId = 'session-a'; seed = 101
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  $syntheticSettlementIdentity = [pscustomobject]@{
    route = 2; routeName = 'settlement'; sessionId = 'session-a'; seed = 101
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  $syntheticLobbyIdentity = [pscustomobject]@{
    route = 0; routeName = 'lobby'; sessionId = ''; seed = 0
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  $syntheticRetryIdentity = [pscustomobject]@{
    route = 1; routeName = 'battle'; sessionId = 'session-b'; seed = 202
    levelId = $expectedLevelIdentity.levelId; mapId = $expectedLevelIdentity.mapId
    waveSetId = $expectedLevelIdentity.waveSetId; ruleSetId = $expectedLevelIdentity.ruleSetId
    themeId = $expectedLevelIdentity.themeId
  }
  Assert-AcceptanceIdentity -Actual $syntheticBattleIdentity `
    -Route 1 -Stage 'self-check-battle' -SessionMode Required | Out-Null
  Assert-AcceptanceIdentity -Actual $syntheticSettlementIdentity `
    -Route 2 -Stage 'self-check-settlement' -SessionMode Required | Out-Null
  Assert-AcceptanceIdentity -Actual $syntheticLobbyIdentity `
    -Route 0 -Stage 'self-check-lobby' -SessionMode Cleared | Out-Null
  Assert-SameSession -Expected $syntheticBattleIdentity `
    -Actual $syntheticSettlementIdentity -Stage 'self-check-settlement'
  Assert-FreshSession -Previous $syntheticSettlementIdentity `
    -Actual $syntheticRetryIdentity -Stage 'self-check-retry'
  $syntheticWarmRun = [ordered]@{
    totalPayloadTransferSize = 1200
    assets = [ordered]@{
      loader = [ordered]@{ transferSize = 300 }
      data = [ordered]@{ transferSize = 300 }
      framework = [ordered]@{ transferSize = 300 }
      wasm = [ordered]@{ transferSize = 300 }
    }
  }
  Assert-WarmCacheTransfer -WarmRun $syntheticWarmRun
  $syntheticWarmRun.assets.data.transferSize = $warmAssetTransferLimitBytes + 1
  $warmCacheGuardRejectedBody = $false
  try { Assert-WarmCacheTransfer -WarmRun $syntheticWarmRun }
  catch { $warmCacheGuardRejectedBody = $true }
  if (-not $warmCacheGuardRejectedBody) {
    throw 'Warm-cache transfer guard self-check failed.'
  }
  $syntheticSeedManifest = [pscustomobject]@{
    schemaVersion = 1
    evidenceType = 'webgl-cache-seed'
    url = 'http://example.test/'
    delivery = [pscustomobject]@{
      assetVersions = [pscustomobject]@{ loader = 'aaaaaaaaaaaa'; data = 'bbbbbbbbbbbb'; framework = 'cccccccccccc'; wasm = 'dddddddddddd' }
      assets = [pscustomobject]@{
        loader = [pscustomobject]@{ version = 'aaaaaaaaaaaa' }
        data = [pscustomobject]@{ version = 'bbbbbbbbbbbb' }
        framework = [pscustomobject]@{ version = 'cccccccccccc' }
        wasm = [pscustomobject]@{ version = 'dddddddddddd' }
      }
    }
  }
  $syntheticCandidateDelivery = [ordered]@{
    assetVersions = [ordered]@{ loader = 'aaaaaaaaaaaa'; data = 'eeeeeeeeeeee'; framework = 'cccccccccccc'; wasm = 'dddddddddddd' }
    assets = [ordered]@{
      loader = [ordered]@{ version = 'aaaaaaaaaaaa'; contentLength = 10 }
      data = [ordered]@{ version = 'eeeeeeeeeeee'; contentLength = 200 }
      framework = [ordered]@{ version = 'cccccccccccc'; contentLength = 30 }
      wasm = [ordered]@{ version = 'dddddddddddd'; contentLength = 40 }
    }
  }
  $syntheticCandidateRun = [ordered]@{
    totalPayloadTransferSize = 200
    assets = [ordered]@{
      loader = [ordered]@{ transferSize = 0 }
      data = [ordered]@{ transferSize = 200 }
      framework = [ordered]@{ transferSize = 0 }
      wasm = [ordered]@{ transferSize = 0 }
    }
  }
  $script:CacheSeedManifestPath = $acceptanceRunnerCommandPath
  $transition = Get-ReleaseTransitionEvidence `
    -SeedManifest $syntheticSeedManifest `
    -CandidateDelivery $syntheticCandidateDelivery `
    -CandidateRun $syntheticCandidateRun
  if ($transition.expectedDownloadBytes -ne 200 -or $transition.changedRoles.Count -ne 1 -or
      $transition.changedRoles[0] -ne 'data' -or $transition.reusedRoles.Count -ne 3) {
    throw 'Cross-release payload classification self-check failed.'
  }
  [ordered]@{
    levelId = $LevelId
    expectedCompositeIdentity = $expectedLevelIdentity
    viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
    safeArea = $safeAreaEvidence
    runtimeUi = $runtimeUiIdentity
    mappedDesignBounds = $mappedDesignBounds
    referenceControls = $referenceControls
    mappedControls = $controls
    sampledRegions = [ordered]@{ header = $headerSampleRegion }
    thresholds = [ordered]@{
      hudDarkPixels = $hudDarkPixelThreshold
      hudLightPixels = $hudLightPixelThreshold
      framePixels = $framePixelThresholds
    }
    blackFrameGuard = 'pass'
    settlementOutlineGuard = [ordered]@{
      exactTwoPixelOutline = 'pass'
      rejectedThreePixelOutline = $thickOutlineRejected
      rejectedOnePixelSides = $rejectedThinOutlineSides
      rejectedLocalResidualSides = $rejectedResidualOutlineSides
      rejectedLocallyGappedSides = $rejectedGappedOutlineSides
      rejectedDetachedCandidateFragment = $detachedOutlineRejected
      rejectedUncoveredCandidate = $uncoveredOutlineCandidateRejected
      finalInkEvidenceKind = $syntheticOutlineEvidence.finalInk.evidenceKind
      minimumFinalInkChannelDeltaExclusive =
        $syntheticOutlineEvidence.finalInk.minimumChannelDeltaExclusive
      outerRingDirectCardinalSidePixels =
        $syntheticOutlineEvidence.outline.rings[1].directCardinalSidePixels
      outerRingMinimumDirectCardinalSidePixels =
        $syntheticOutlineEvidence.outline.rings[1].minimumDirectCardinalSidePixels
      outerRingDirectCardinalSideFractionsOfRing =
        $syntheticOutlineEvidence.outline.rings[1].directCardinalSideFractionsOfRing
      outerRingPreviousRingRetentionFractions =
        $syntheticOutlineEvidence.outline.rings[1].previousRingRetentionFractions
    }
    shellControlMapping = 'pass'
    compositeIdentityContract = 'pass'
     sessionLifecycleContract = 'pass'
     warmCacheTransferGuard = 'pass'
     crossReleaseCacheGuard = 'pass'
  } | ConvertTo-Json -Depth 8
  Write-Host 'FRUIT_DEFENSE_ACCEPTANCE_SELF_CHECK_OK'
}
