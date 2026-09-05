# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Get-HubCapture {
  param([string]$Name, [string]$Stage, [object]$HubTelemetry)

  Wait-AppRoute -Route 0
  $identity = Get-AcceptanceIdentity
  if ($null -eq $identity -or [int]$identity.route -ne 0 -or
      -not [string]::IsNullOrEmpty([string]$identity.sessionId) -or
      [int]$identity.seed -ne 0) {
    throw "Hub state '$Stage' changed the Lobby route/session identity: $($identity | ConvertTo-Json -Compress)"
  }

  Move-CanvasPointerOut
  $capture = Save-StableScreenshot -Name $Name -RequireHud $false
  if ($capture.Metrics.width -ne $Width -or $capture.Metrics.height -ne $Height -or
      -not (Test-StableFrameMetrics -Metrics $capture.Metrics)) {
    throw "Hub state '$Stage' produced an invalid frame: $($capture.Metrics | ConvertTo-Json -Compress)"
  }

  return [pscustomobject]@{
    Stage = $Stage
    Path = $capture.Path
    Metrics = $capture.Metrics
    Sha256 = (Get-FileHash -LiteralPath $capture.Path -Algorithm SHA256).Hash
    Identity = $identity
    HubTelemetry = $HubTelemetry
  }
}

function Get-HubNamedStateCatalog {
  return @(
    [pscustomobject]@{ id = 'home-fresh'; page = 'home'; growthPage = ''; kind = 'StaticState'; resolved = 'fresh' }
    [pscustomobject]@{ id = 'home-policy-preview'; page = 'home'; growthPage = ''; kind = 'StaticState'; resolved = 'applied-suppressed' }
    [pscustomobject]@{ id = 'activity-claimable'; page = 'activity'; growthPage = ''; kind = 'StaticState'; resolved = 'claimable' }
    [pscustomobject]@{ id = 'activity-claiming'; page = 'activity'; growthPage = ''; kind = 'StaticState'; resolved = 'claiming'; busy = $true }
    [pscustomobject]@{ id = 'activity-claimed'; page = 'activity'; growthPage = ''; kind = 'StaticState'; resolved = 'claimed' }
    [pscustomobject]@{ id = 'activity-error'; page = 'activity'; growthPage = ''; kind = 'StaticState'; resolved = 'error'; status = 'InvalidProfile' }
    [pscustomobject]@{ id = 'activity-save-failure'; page = 'activity'; growthPage = ''; kind = 'PersistenceFailure'; resolved = 'error'; status = 'PersistenceFailed' }
    [pscustomobject]@{ id = 'equipment-owned'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'owned' }
    [pscustomobject]@{ id = 'equipment-selected'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'upgradeable' }
    [pscustomobject]@{ id = 'equipment-locked'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'locked' }
    [pscustomobject]@{ id = 'equipment-insufficient'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'insufficient' }
    [pscustomobject]@{ id = 'equipment-maximum'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'maximum' }
    [pscustomobject]@{ id = 'equipment-loading'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'loading'; busy = $true }
    [pscustomobject]@{ id = 'equipment-error'; page = 'growth'; growthPage = 'equipment'; kind = 'StaticState'; resolved = 'error'; status = 'InvalidProfile' }
    [pscustomobject]@{ id = 'equipment-save-failure'; page = 'growth'; growthPage = 'equipment'; kind = 'PersistenceFailure'; resolved = 'error'; status = 'PersistenceFailed' }
    [pscustomobject]@{ id = 'cultivation-selected'; page = 'growth'; growthPage = 'cultivation'; kind = 'StaticState'; resolved = 'upgradeable' }
    [pscustomobject]@{ id = 'cultivation-locked'; page = 'growth'; growthPage = 'cultivation'; kind = 'StaticState'; resolved = 'locked' }
    [pscustomobject]@{ id = 'cultivation-insufficient'; page = 'growth'; growthPage = 'cultivation'; kind = 'StaticState'; resolved = 'insufficient' }
    [pscustomobject]@{ id = 'cultivation-maximum'; page = 'growth'; growthPage = 'cultivation'; kind = 'StaticState'; resolved = 'maximum' }
    [pscustomobject]@{ id = 'cultivation-loading'; page = 'growth'; growthPage = 'cultivation'; kind = 'StaticState'; resolved = 'loading'; busy = $true }
    [pscustomobject]@{ id = 'cultivation-error'; page = 'growth'; growthPage = 'cultivation'; kind = 'StaticState'; resolved = 'error'; status = 'InvalidProfile' }
    [pscustomobject]@{ id = 'cultivation-save-failure'; page = 'growth'; growthPage = 'cultivation'; kind = 'PersistenceFailure'; resolved = 'error'; status = 'PersistenceFailed' }
  )
}

function Assert-HubNamedStateTelemetry {
  param([object]$Definition, [object]$Telemetry)

  if ([string]$Telemetry.evidenceKind -cne [string]$Definition.kind) {
    throw "Hub named state '$($Definition.id)' evidence kind mismatch: $($Telemetry.evidenceKind)"
  }
  $expectedFixtureId = "acceptance-hub/$($Definition.id)/v1"
  if ([string]$Telemetry.fixtureId -cne $expectedFixtureId) {
    throw "Hub named state '$($Definition.id)' fixture identity mismatch: expected=$expectedFixtureId actual=$($Telemetry.fixtureId)"
  }
  if ([string]$Telemetry.resolvedState -cne [string]$Definition.resolved) {
    throw "Hub named state '$($Definition.id)' resolved state mismatch: expected=$($Definition.resolved) actual=$($Telemetry.resolvedState)"
  }
  $expectedBusy = $null -ne $Definition.PSObject.Properties['busy'] -and
    [bool]$Definition.busy
  if ([bool]$Telemetry.commandInProgress -ne $expectedBusy) {
    throw "Hub named state '$($Definition.id)' command-in-progress mismatch: expected=$expectedBusy actual=$($Telemetry.commandInProgress)"
  }
  if ($null -ne $Definition.PSObject.Properties['status'] -and
      [string]$Telemetry.lastCommandStatus -cne [string]$Definition.status) {
    throw "Hub named state '$($Definition.id)' command status mismatch: expected=$($Definition.status) actual=$($Telemetry.lastCommandStatus)"
  }
  if ([string]$Definition.id -ceq 'home-policy-preview' -and
      ([int]$Telemetry.appliedSourceCount -lt 1 -or
        [int]$Telemetry.suppressedSourceCount -lt 1)) {
    throw "Hub policy preview fixture lacks applied/suppressed sources: $($Telemetry | ConvertTo-Json -Compress -Depth 8)"
  }
}

function Assert-HubNamedStateSharedChrome {
  param([string]$StateId, [string]$ScreenshotPath)

  # The primary navigation is drawn after page content. A page-local IMGUI
  # exception can still leave a stable, correctly-sized screenshot while
  # aborting the frame before shared chrome is drawn. Sample an unselected
  # navigation-paper point to prove the complete frame reached that owner.
  # Probe the fixed paper gutter between Home and Activity. It stays outside
  # every selected-item surface, icon, and label for all three selected pages.
  $samplePoint = Convert-ReferencePoint -X 138 -Y 805
  $sample = Get-ImagePixelSample -Path $ScreenshotPath `
    -X ([Math]::Round($samplePoint.x)) `
    -Y ([Math]::Round($samplePoint.y))
  if ([int]$sample.a -lt 250 -or [int]$sample.r -lt 220 -or
      [int]$sample.g -lt 220 -or [int]$sample.b -lt 200) {
    throw "Hub named state '$StateId' did not complete the shared bottom navigation frame: $($sample | ConvertTo-Json -Compress)"
  }
  return $sample
}

function Get-HubPaletteSamples {
  param([string]$HomePath)

  $edge = Convert-ReferencePoint -X 3 -Y 420
  $paper = Convert-ReferencePoint -X 20 -Y 112
  $selection = Convert-ReferencePoint -X 360 -Y 132
  $primaryAction = Convert-ReferencePoint -X 100 -Y 735
  return [ordered]@{
    edgeBackground = Get-ImagePixelSample -Path $HomePath `
      -X ([Math]::Round($edge.x)) -Y ([Math]::Round($edge.y))
    basePaper = Get-ImagePixelSample -Path $HomePath `
      -X ([Math]::Round($paper.x)) -Y ([Math]::Round($paper.y))
    selectionAccent = Get-ImagePixelSample -Path $HomePath `
      -X ([Math]::Round($selection.x)) -Y ([Math]::Round($selection.y))
    primaryAction = Get-ImagePixelSample -Path $HomePath `
      -X ([Math]::Round($primaryAction.x)) -Y ([Math]::Round($primaryAction.y))
  }
}

function Get-HubGeometryEvidence {
  $safeBounds = [ordered]@{
    xMin = 0
    yMin = $SafeTop
    xMax = $Width
    yMax = $Height - $SafeBottom
  }
  $rects = [ordered]@{
    topBar = Convert-ReferenceRect -X 7 -Y 15 -Width 388 -Height 80
    pageHost = Convert-ReferenceRect -X 11 -Y 103 -Width 386 -Height 690
    bottomNavigation = Convert-ReferenceRect -X 0 -Y 794 -Width 402 -Height 80
    home = Convert-ReferenceRect -X 16 -Y 794 -Width 118 -Height 80
    activity = Convert-ReferenceRect -X 142 -Y 794 -Width 118 -Height 80
    growth = Convert-ReferenceRect -X 268 -Y 794 -Width 118 -Height 80
    equipmentTab = Convert-ReferenceRect -X 24 -Y 106 -Width 173 -Height 52
    cultivationTab = Convert-ReferenceRect -X 205 -Y 106 -Width 173 -Height 52
    start = Convert-ReferenceRect -X 57 -Y 700 -Width 289 -Height 56
    activityClaim = Convert-ReferenceRect -X 66 -Y 641 -Width 270 -Height 57
    growthEntry = Convert-ReferenceRect -X 27 -Y 174 -Width 351 -Height 116
    equipmentPrimary = Convert-ReferenceRect -X 109 -Y 707 -Width 184 -Height 55
    cultivationPrimary = Convert-ReferenceRect -X 115 -Y 723 -Width 171 -Height 51
  }
  foreach ($name in $rects.Keys) {
    $rect = $rects[$name]
    if ($rect.xMin -lt $safeBounds.xMin -or $rect.yMin -lt $safeBounds.yMin -or
        $rect.xMax -gt $safeBounds.xMax -or $rect.yMax -gt $safeBounds.yMax -or
        $rect.xMax -le $rect.xMin -or $rect.yMax -le $rect.yMin) {
      throw "Hub geometry '$name' escapes the safe content: $($rect | ConvertTo-Json -Compress)"
    }
  }
  $logicalBoundaries = [ordered]@{
    topBarBottom = $referenceOffsetY + 95.0 * $referenceScale
    pageHostTop = $referenceOffsetY + 103.0 * $referenceScale
    pageHostBottom = $referenceOffsetY + 793.0 * $referenceScale
    bottomNavigationTop = $referenceOffsetY + 794.0 * $referenceScale
  }
  if ($logicalBoundaries.topBarBottom -gt $logicalBoundaries.pageHostTop -or
      $logicalBoundaries.pageHostBottom -gt
        $logicalBoundaries.bottomNavigationTop) {
    throw 'Hub top bar, page host, and bottom navigation overlap.'
  }
  $minimumTarget = [Math]::Min(44.0, 44.0 * $referenceScale)
  $targets = [ordered]@{
    home = $rects.home
    activity = $rects.activity
    growth = $rects.growth
    equipmentTab = $rects.equipmentTab
    cultivationTab = $rects.cultivationTab
    start = $rects.start
    activityClaim = $rects.activityClaim
    growthEntry = $rects.growthEntry
    equipmentPrimary = $rects.equipmentPrimary
    cultivationPrimary = $rects.cultivationPrimary
  }
  $targetOutcomes = [ordered]@{}
  foreach ($name in $targets.Keys) {
    $rect = $targets[$name]
    $shortest = [Math]::Min(
      [double]$rect.xMax - [double]$rect.xMin,
      [double]$rect.yMax - [double]$rect.yMin)
    if ($shortest + 0.001 -lt $minimumTarget) {
      throw "Hub target '$name' is below the projected 44-point minimum: $shortest < $minimumTarget"
    }
    $targetOutcomes[$name] = [ordered]@{
      rect = $rect
      shortestCapturePixels = $shortest
      minimumProjectedCapturePixels = $minimumTarget
      minimumLogicalPoints = 44
      passed = $true
    }
  }
  return [ordered]@{
    safeBounds = $safeBounds
    designReference = [ordered]@{ width = 402; height = 874 }
    scale = $referenceScale
    rects = $rects
    logicalBoundaries = $logicalBoundaries
    targets = $targetOutcomes
    checks = [ordered]@{
      safeAreaContainment = 'pass'
      sharedChromeNonOverlap = 'pass'
      pageClosesAboveNavigation = 'pass'
      projectedMinimum44PointTargets = 'pass'
    }
  }
}

function Invoke-HubVisualMode {
  Wait-AppRoute -Route 0
  Set-HubAcceptanceState -State 'reward-to-battle'

  $captures = [ordered]@{}
  $homeTelemetry = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'home' -FixtureMode Forbidden `
    -Route 0 -Stage 'hub-visual-home'
  $captures.home = Get-HubCapture -Name '01-hub-home' -Stage 'home' `
    -HubTelemetry $homeTelemetry

  Invoke-CanvasClick -X $controls.hubNavActivity.x -Y $controls.hubNavActivity.y
  $activityTelemetry = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'activity' -FixtureMode Forbidden `
    -Route 0 -Stage 'hub-visual-activity'
  $captures.activity = Get-HubCapture -Name '02-hub-activity' -Stage 'activity' `
    -HubTelemetry $activityTelemetry

  Invoke-CanvasClick -X $controls.hubNavGrowth.x -Y $controls.hubNavGrowth.y
  $equipmentTelemetry = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'growth' -GrowthPage 'equipment' `
    -FixtureMode Forbidden -Route 0 -Stage 'hub-visual-equipment'
  $captures.equipment = Get-HubCapture -Name '03-hub-growth-equipment' `
    -Stage 'growth-equipment' -HubTelemetry $equipmentTelemetry

  Invoke-CanvasClick -X $controls.hubGrowthCultivation.x -Y $controls.hubGrowthCultivation.y
  $cultivationTelemetry = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'growth' -GrowthPage 'cultivation' `
    -FixtureMode Forbidden -Route 0 -Stage 'hub-visual-cultivation'
  $captures.cultivation = Get-HubCapture -Name '04-hub-growth-cultivation' `
    -Stage 'growth-cultivation' -HubTelemetry $cultivationTelemetry

  $distinctHashes = @($captures.Values | ForEach-Object { $_.Sha256 } |
      Sort-Object -Unique).Count
  if ($distinctHashes -ne $captures.Count) {
    $capturedHashes = @($captures.Values | ForEach-Object { $_.Sha256 }) -join ','
    throw "Hub navigation did not produce four distinct visual states: $capturedHashes"
  }

  Invoke-CanvasClick -X $controls.hubNavHome.x -Y $controls.hubNavHome.y
  Wait-AppRoute -Route 0
  $returnTelemetry = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'home' -FixtureMode Forbidden `
    -Route 0 -Stage 'hub-visual-return-home'
  $returnIdentity = Get-AcceptanceIdentity
  if ($null -eq $returnIdentity -or [int]$returnIdentity.route -ne 0 -or
      -not [string]::IsNullOrEmpty([string]$returnIdentity.sessionId)) {
    throw "Hub navigation did not return to Home without a scene/session transition: $($returnIdentity | ConvertTo-Json -Compress)"
  }

  $startRect = Convert-ReferenceRect -X 57 -Y 700 -Width 289 -Height 56
  $startContrast = Get-ActionContentContrast -Path $captures.home.Path `
    -Rect $startRect -Polarity DarkOnLight -MinimumContrast 4.5
  if (-not $startContrast.passed) {
    throw "Hub Start action contrast is below 4.5: $($startContrast | ConvertTo-Json -Compress)"
  }

  $screenshots = [ordered]@{}
  $metrics = [ordered]@{}
  $identities = [ordered]@{}
  $hubTelemetry = [ordered]@{}
  foreach ($name in $captures.Keys) {
    $screenshots[$name] = $captures[$name].Path
    $metrics[$name] = $captures[$name].Metrics
    $identities[$name] = $captures[$name].Identity
    $hubTelemetry[$name] = $captures[$name].HubTelemetry
  }

  $manifest = [ordered]@{
    schemaVersion = 1
    evidenceType = 'outgame-hub-webgl-visual'
    accepted = $true
    completion = 'four-live-hub-pages-captured'
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    url = $Url
    verifiedBuildProfile = $verifiedBuildProfile
    viewport = [ordered]@{
      width = $Width
      height = $Height
      coordinateSpace = 'css-pixel/top-left'
    }
    safeArea = $safeAreaEvidence
    geometry = Get-HubGeometryEvidence
    runtimeUi = $runtimeUiIdentity
    browser = $browserEvidence
    canvas = $readiness
    controls = [ordered]@{
      home = $controls.hubNavHome
      activity = $controls.hubNavActivity
      growth = $controls.hubNavGrowth
      equipment = $controls.hubGrowthEquipment
      cultivation = $controls.hubGrowthCultivation
      start = $controls.hubStart
    }
    referenceControls = [ordered]@{
      home = $referenceControls.hubNavHome
      activity = $referenceControls.hubNavActivity
      growth = $referenceControls.hubNavGrowth
      equipment = $referenceControls.hubGrowthEquipment
      cultivation = $referenceControls.hubGrowthCultivation
      start = $referenceControls.hubStart
    }
    routeIdentities = $identities
    hubTelemetry = $hubTelemetry
    returnIdentity = $returnIdentity
    returnHubTelemetry = $returnTelemetry
    screenshots = $screenshots
    screenshotSha256 = [ordered]@{
      home = $captures.home.Sha256
      activity = $captures.activity.Sha256
      equipment = $captures.equipment.Sha256
      cultivation = $captures.cultivation.Sha256
    }
    imageMetrics = $metrics
    paletteTargets = [ordered]@{
      edgeBackground = '#73C9F4'
      basePaper = '#F9EFDA'
      raisedPaper = '#FBE8AA'
      selectionAccent = '#FBCF52'
      primaryAction = '#A0C73D'
      primaryText = '#6B3F12'
      actionContent = '#4B2A13'
      disabledSurface = '#E4DCCD'
      disabledContent = '#7C746B'
    }
    paletteSamples = Get-HubPaletteSamples -HomePath $captures.home.Path
    startActionContrast = $startContrast
    checks = [ordered]@{
      releaseRuntimeUiIdentity = 'pass'
      fourDistinctLiveStates = 'pass'
      navigationStayedInLobby = 'pass'
      returnHomeWithoutSceneLoad = 'pass'
      requestedViewportAndCanvas = 'pass'
      safeAreaGeometry = 'pass'
      minimum44PointTargets = 'pass'
      screenshotDimensions = 'pass'
      noBlackTransparentOrLargeNearBlackRegions = 'pass'
      startActionContrast = 'pass'
      paletteTargetsRecorded = 'pass'
      paletteDeltaE = 'manual-score-required'
      noSyntheticRewardOrGrowthData = 'manual-screenshot-review-required'
      fixtureDataAbsent = 'pass'
      exactHubTelemetryRecorded = 'pass'
    }
  }

  $manifestPath = Join-Path $outputDir 'hub-visual-evidence.json'
  $manifest | ConvertTo-Json -Depth 12 |
    Set-Content -LiteralPath $manifestPath -Encoding UTF8
  Write-Host "FRUIT_DEFENSE_HUB_VISUAL_OK manifest=$manifestPath"
}

function Invoke-HubStatesMode {
  Wait-AppRoute -Route 0
  $definitions = @(Get-HubNamedStateCatalog)
  if ($definitions.Count -ne 22 -or
      @($definitions.id | Sort-Object -Unique).Count -ne 22) {
    throw 'Hub named-state runner must own exactly 22 unique static/failure states.'
  }

  $captures = [ordered]@{}
  $telemetry = [ordered]@{}
  $index = 0
  foreach ($definition in $definitions) {
    $index++
    Set-HubAcceptanceState -State $definition.id
    $condition = {
      param($actual)
      if ($null -ne $definition.PSObject.Properties['status']) {
        return [string]$actual.lastCommandStatus -ceq [string]$definition.status
      }
      if ($null -ne $definition.PSObject.Properties['busy']) {
        return [bool]$actual.commandInProgress -eq [bool]$definition.busy
      }
      return -not [bool]$actual.commandInProgress
    }.GetNewClosure()
    $stateTelemetry = Wait-HubAcceptanceTelemetry `
      -StateId $definition.id -Page $definition.page `
      -GrowthPage $definition.growthPage -FixtureMode Required -Route 0 `
      -Stage "hub-state-$($definition.id)" -Condition $condition
    Assert-HubNamedStateTelemetry -Definition $definition `
      -Telemetry $stateTelemetry
    $captureName = '{0:d2}-{1}' -f $index, $definition.id
    $capture = Get-HubCapture -Name $captureName -Stage $definition.id `
      -HubTelemetry $stateTelemetry
    $bottomNavigationSample = Assert-HubNamedStateSharedChrome `
      -StateId $definition.id -ScreenshotPath $capture.Path
    $captures[$definition.id] = [ordered]@{
      screenshot = $capture.Path
      screenshotSha256 = $capture.Sha256
      imageMetrics = $capture.Metrics
      routeIdentity = $capture.Identity
      bottomNavigationSample = $bottomNavigationSample
    }
    $telemetry[$definition.id] = $stateTelemetry
  }

  $manifest = [ordered]@{
    schemaVersion = 1
    evidenceType = 'outgame-hub-named-state-webgl'
    accepted = $true
    completion = 'all-static-and-persistence-failure-states-captured'
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    url = $Url
    verifiedBuildProfile = $verifiedBuildProfile
    viewport = [ordered]@{
      width = $Width
      height = $Height
      coordinateSpace = 'css-pixel/top-left'
    }
    safeArea = $safeAreaEvidence
    runtimeUi = $runtimeUiIdentity
    browser = $browserEvidence
    canvas = $readiness
    stateDefinitions = $definitions
    telemetry = $telemetry
    captures = $captures
    checks = [ordered]@{
      completeFiniteCatalog = 'pass'
      exactStateTelemetry = 'pass'
      fixtureIdentityExplicit = 'pass'
      fixtureActiveForEveryState = 'pass'
      realSequenceExcluded = 'pass'
      persistenceFailuresObserved = 'pass'
      screenshotDimensions = 'pass'
      noBlackTransparentOrLargeNearBlackRegions = 'pass'
      sharedBottomNavigationComplete = 'pass'
      manualFiniteCopyAndNonColorReview = 'manual-score-required'
    }
  }
  $manifestPath = Join-Path $outputDir 'hub-named-state-evidence.json'
  $manifest | ConvertTo-Json -Depth 14 |
    Set-Content -LiteralPath $manifestPath -Encoding UTF8
  Write-Host "FRUIT_DEFENSE_HUB_STATES_OK manifest=$manifestPath"
}
