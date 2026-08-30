# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

function Get-CombatFeedbackTelemetry {
  $json = Invoke-JavaScript -Expression @'
JSON.stringify(window.fruitDefenseCombatFeedbackTelemetry ?? null)
'@
  if ([string]::IsNullOrWhiteSpace([string]$json) -or $json -eq 'null') { return $null }
  return $json | ConvertFrom-Json
}

function Wait-CombatFeedbackTelemetry {
  param([string]$State, [Nullable[int]]$Speed = $null)
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
  $actual = $null
  do {
    $actual = Get-CombatFeedbackTelemetry
    if ($null -ne $actual -and [string]$actual.state -ceq $State -and
        ($null -eq $Speed -or [int]$actual.battleSpeed -eq [int]$Speed)) {
      return $actual
    }
    Start-Sleep -Milliseconds 40
  } while ((Get-Date) -lt $deadline)
  $actualJson = if ($null -eq $actual) { 'null' } else { $actual | ConvertTo-Json -Compress -Depth 8 }
  throw "Timed out waiting for combat-feedback telemetry state=$State speed=$Speed actual=$actualJson"
}

function Wait-CombatFeedbackProfile {
  param([string]$State)
  $deadline = (Get-Date).AddSeconds([Math]::Max($TimeoutSeconds, 60))
  $actual = $null
  do {
    $actual = Get-CombatFeedbackTelemetry
    if ($null -ne $actual -and [string]$actual.state -ceq $State -and
        ([bool]$actual.profileCompleted -or
         (-not [bool]$actual.profileSupported -and -not [bool]$actual.profileActive))) {
      return $actual
    }
    Start-Sleep -Milliseconds 40
  } while ((Get-Date) -lt $deadline)
  $actualJson = if ($null -eq $actual) { 'null' } else { $actual | ConvertTo-Json -Compress -Depth 4 }
  throw "Timed out waiting for combat-feedback Sync CPU profile: $actualJson"
}

function Test-FiniteNumber {
  param([object]$Value)
  try { $number = [double]$Value } catch { return $false }
  return -not [double]::IsNaN($number) -and -not [double]::IsInfinity($number)
}

function Test-CombatFeedbackGeometryEqual {
  param([object]$First, [object]$Second)
  if ($null -eq $First -or $null -eq $Second) { return $false }
  foreach ($field in @(
      'headerX', 'headerY', 'headerWidth', 'headerHeight',
      'boardX', 'boardY', 'boardWidth', 'boardHeight',
      'potHitX', 'potHitY', 'potHitWidth', 'potHitHeight')) {
    if (-not (Test-FiniteNumber $First.$field) -or
        -not (Test-FiniteNumber $Second.$field) -or
        [Math]::Abs([double]$First.$field - [double]$Second.$field) -gt .0001) {
      return $false
    }
  }
  return $true
}

function Assert-CombatFeedbackTelemetry {
  param(
    [object]$Actual,
    [string]$State,
    [string]$Surface,
    [string]$Phase,
    [int]$Speed,
    [string]$Beat,
    [int]$FeedbackCount,
    [string[]]$Roles,
    [string[]]$SemanticIds
  )
  if ($null -eq $Actual -or [int]$Actual.schemaVersion -ne 1 -or
      [string]$Actual.state -cne $State -or [string]$Actual.surface -cne $Surface -or
      [string]$Actual.phase -cne $Phase -or [int]$Actual.battleSpeed -ne $Speed -or
      [string]$Actual.activeBeat -cne $Beat) {
    throw "Combat-feedback telemetry identity mismatch for '$State': $($Actual | ConvertTo-Json -Compress -Depth 8)"
  }
  if ([int]$Actual.feedbackCount -ne $FeedbackCount -or
      [int]$Actual.activePoolCount -ne $FeedbackCount -or
      [int]$Actual.poolCapacity -ne 9999 -or [int]$Actual.atlasPageCount -ne 1 -or
      [string]$Actual.atlasFormat -cne 'RGBA32' -or
      [int]$Actual.sharedMaterialCount -ne 0 -or
      [int]$Actual.preparedAtlasDrawCount -lt $FeedbackCount -or
      [int]$Actual.preparedAtlasDrawCount -gt 192 -or
      -not [bool]$Actual.placementValid -or
      -not [string]::IsNullOrEmpty([string]$Actual.placementFailure) -or
      [int]$Actual.missingProfileCount -ne 0) {
    throw "Combat-feedback resource/capacity telemetry mismatch for '$State': $($Actual | ConvertTo-Json -Compress -Depth 8)"
  }
  foreach ($role in $Roles) {
    if (@($Actual.activeRoles) -cnotcontains $role) {
      throw "Combat-feedback role '$role' is missing from '$State'."
    }
  }
  $actualSemantics = @($Actual.feedback | ForEach-Object { [string]$_.semanticId } | Sort-Object)
  $expectedSemantics = @($SemanticIds | Sort-Object)
  if ($actualSemantics.Count -ne $expectedSemantics.Count -or
      ($actualSemantics -join "`n") -cne ($expectedSemantics -join "`n")) {
    throw "Combat-feedback semantic ID multiset mismatch for '$State': actual=$($actualSemantics -join ',') expected=$($expectedSemantics -join ',')"
  }
  if ([string]$Actual.renderMarker -cne 'FruitDefense.CombatFloatingText.Render' -or
      [string]$Actual.performanceScope -cne
        'CombatFloatingTextSdfOverlay command preparation plus final IMGUI-layer RGBA atlas glyph submissions; GPU raster excluded' -or
      [string]$Actual.profileAllocationMetric -cne
        'GC.GetAllocatedBytesForCurrentThread epoch-normalized into an acceptance-session cumulative managed-allocation counter') {
    throw "Combat-feedback measurement boundary is ambiguous for '$State'."
  }
  foreach ($number in @(
      $Actual.beatProgress, $Actual.battlefieldOffsetX,
      $Actual.battlefieldOffsetY, $Actual.battlefieldFlash,
      $Actual.expectedCentroidX, $Actual.expectedCentroidY,
      $Actual.eventCentroidError, $Actual.anchorCentroidError)) {
    if (-not (Test-FiniteNumber $number)) {
      throw "Combat-feedback telemetry contains a non-finite scalar for '$State'."
    }
  }
  foreach ($record in @($Actual.feedback)) {
    foreach ($number in @(
        $record.eventX, $record.eventY, $record.anchorX, $record.anchorY,
        $record.lifetimeProgress, $record.detachedProgress,
        $record.motionScale, $record.motionOpacity,
        $record.finalScreenCenterX, $record.finalScreenCenterY,
        $record.anchorScreenX, $record.anchorScreenY,
        $record.anchorScreenError, $record.finalScreenBoundsX,
        $record.finalScreenBoundsY, $record.finalScreenBoundsWidth,
        $record.finalScreenBoundsHeight)) {
      if (-not (Test-FiniteNumber $number)) {
        throw "Combat-feedback record contains a non-finite scalar for '$State'."
      }
    }
    $maximumAnchorScreenError = 20.0 * $referenceScale + .05
    if ([double]$record.anchorScreenError -lt 0 -or
        [double]$record.anchorScreenError -gt $maximumAnchorScreenError -or
        [double]$record.finalScreenBoundsWidth -le 0 -or
        [double]$record.finalScreenBoundsHeight -le 0) {
      $recordJson = $record | ConvertTo-Json -Compress -Depth 8
      throw "Combat-feedback screen placement violates its contact/bounds contract for '$State': maximumAnchorScreenError=$maximumAnchorScreenError record=$recordJson"
    }
  }
  if (-not [bool]$Actual.authoritativeGeometryUnchanged -or
      -not (Test-CombatFeedbackGeometryEqual `
        -First $Actual.geometryBefore -Second $Actual.geometryAfter)) {
    throw "Combat-feedback fixture changed authoritative HUD/Board/PotHitRect geometry for '$State'."
  }
  $offsetMagnitude = [Math]::Sqrt(
    [Math]::Pow([double]$Actual.battlefieldOffsetX, 2) +
    [Math]::Pow([double]$Actual.battlefieldOffsetY, 2))
  if (($Beat -ceq 'None' -and $offsetMagnitude -gt .0001) -or
      ($Beat -cne 'None' -and ($offsetMagnitude -le .0001 -or $offsetMagnitude -gt 3.0001))) {
    throw "Combat-feedback beat offset is not non-zero/bounded for '$State': magnitude=$offsetMagnitude beat=$Beat"
  }
  return $Actual
}

function Invoke-CombatFeedbackMode {
    $combatFeedbackScreenshots = [ordered]@{}
    $combatFeedbackTelemetry = [ordered]@{}
    $combatFeedbackCaptures = @()
    $allRoles = @(
      'NormalDamage', 'HeavyDamage', 'PeriodicDamage',
      'Resource', 'Control', 'Defeat')
    $roleSemantics = @(
      'ability.plant.pea.attack', 'ability.plant.watermelon.attack',
      'status.chili.burn', 'resource.sun', 'status.ice.freeze',
      'enemy.normal')
    $denseSemantics = @(
      'ability.plant.pea.attack', 'ability.plant.pea.attack',
      'ability.plant.pea.attack', 'ability.plant.banana.attack',
      'ability.plant.banana.attack', 'ability.plant.banana.attack',
      'status.chili.burn', 'status.chili.burn',
      'ability.plant.watermelon.attack', 'resource.sun',
      'status.ice.freeze', 'enemy.normal')
    $combatFeedbackStates = @(
      [pscustomobject]@{ State = 'combat-feedback-role-grass'; Surface = 'grass'; Phase = 'role-inventory'; Speed = 1; Beat = 'Heavy'; Count = 6; Roles = $allRoles; Semantics = $roleSemantics },
      [pscustomobject]@{ State = 'combat-feedback-role-route'; Surface = 'route'; Phase = 'role-inventory'; Speed = 1; Beat = 'Heavy'; Count = 6; Roles = $allRoles; Semantics = $roleSemantics },
      [pscustomobject]@{ State = 'combat-feedback-motion-start'; Surface = 'route'; Phase = 'start'; Speed = 1; Beat = 'Heavy'; Count = 1; Roles = @('HeavyDamage'); Semantics = @('ability.plant.watermelon.attack') },
      [pscustomobject]@{ State = 'combat-feedback-motion-early'; Surface = 'route'; Phase = 'early'; Speed = 1; Beat = 'Heavy'; Count = 1; Roles = @('HeavyDamage'); Semantics = @('ability.plant.watermelon.attack') },
      [pscustomobject]@{ State = 'combat-feedback-motion-settle'; Surface = 'route'; Phase = 'settle'; Speed = 1; Beat = 'None'; Count = 1; Roles = @('HeavyDamage'); Semantics = @('ability.plant.watermelon.attack') },
      [pscustomobject]@{ State = 'combat-feedback-motion-hold'; Surface = 'route'; Phase = 'hold'; Speed = 1; Beat = 'None'; Count = 1; Roles = @('HeavyDamage'); Semantics = @('ability.plant.watermelon.attack') },
      [pscustomobject]@{ State = 'combat-feedback-dense-1x'; Surface = 'route'; Phase = 'dense'; Speed = 1; Beat = 'Heavy'; Count = 12; Roles = $allRoles; Semantics = $denseSemantics },
      [pscustomobject]@{ State = 'combat-feedback-dense-2x'; Surface = 'route'; Phase = 'dense'; Speed = 2; Beat = 'Heavy'; Count = 12; Roles = $allRoles; Semantics = $denseSemantics },
      [pscustomobject]@{ State = 'combat-feedback-beat-heavy'; Surface = 'route'; Phase = 'impact-beat'; Speed = 1; Beat = 'Heavy'; Count = 1; Roles = @('HeavyDamage'); Semantics = @('ability.plant.watermelon.attack') },
      [pscustomobject]@{ State = 'combat-feedback-beat-cluster'; Surface = 'route'; Phase = 'impact-beat'; Speed = 1; Beat = 'Cluster'; Count = 1; Roles = @('Defeat'); Semantics = @('enemy.normal') },
      [pscustomobject]@{ State = 'combat-feedback-beat-terminal'; Surface = 'route'; Phase = 'impact-beat'; Speed = 1; Beat = 'Terminal'; Count = 1; Roles = @('Defeat'); Semantics = @('enemy.boss') }
    )
    if ($combatFeedbackStates.Count -ne 11) {
      throw "Combat-feedback visual fixture inventory must remain exactly 11; actual=$($combatFeedbackStates.Count)"
    }

    $captureIndex = 1
    foreach ($fixture in $combatFeedbackStates) {
      Set-AcceptanceState -State $fixture.State
      $telemetry = Wait-CombatFeedbackTelemetry `
        -State $fixture.State -Speed $fixture.Speed
      Assert-CombatFeedbackTelemetry -Actual $telemetry `
        -State $fixture.State -Surface $fixture.Surface -Phase $fixture.Phase `
        -Speed $fixture.Speed -Beat $fixture.Beat -FeedbackCount $fixture.Count `
        -Roles $fixture.Roles -SemanticIds $fixture.Semantics | Out-Null
      if ($fixture.Count -eq 12 -and [int]$telemetry.ordinaryFeedbackCount -ne 8) {
        throw "Dense combat-feedback fixture did not preserve its authored eight ordinary records."
      }
      $name = '{0:D2}-{1}' -f $captureIndex, $fixture.State
      $path = Save-Screenshot -Name $name
      $metricsForFixture = Get-ImageMetrics -Path $path
      if ($metricsForFixture.width -ne $Width -or $metricsForFixture.height -ne $Height) {
        throw "Unexpected combat-feedback screenshot dimensions for '$($fixture.State)'."
      }
      $combatFeedbackScreenshots[$fixture.State] = $path
      $combatFeedbackTelemetry[$fixture.State] = $telemetry
      $combatFeedbackCaptures += [pscustomobject]@{
        screenshot = $path
        state = $fixture.State
        roles = @($fixture.Roles)
        surface = $fixture.Surface
        speed = $fixture.Speed
        phase = $fixture.Phase
        beat = $fixture.Beat
      }
      $captureIndex++
    }

    $startScale = [double]$combatFeedbackTelemetry['combat-feedback-motion-start'].feedback[0].motionScale
    $earlyScale = [double]$combatFeedbackTelemetry['combat-feedback-motion-early'].feedback[0].motionScale
    $settleScale = [double]$combatFeedbackTelemetry['combat-feedback-motion-settle'].feedback[0].motionScale
    $holdScale = [double]$combatFeedbackTelemetry['combat-feedback-motion-hold'].feedback[0].motionScale
    if ($startScale -le $earlyScale -or $earlyScale -le $settleScale -or
        $settleScale -le $holdScale) {
      throw "Motion scale does not shrink start > early > settle > hold: $startScale/$earlyScale/$settleScale/$holdScale"
    }
    $denseOneRecords = @($combatFeedbackTelemetry['combat-feedback-dense-1x'].feedback)
    $denseTwoRecords = @($combatFeedbackTelemetry['combat-feedback-dense-2x'].feedback)
    if ($denseOneRecords.Count -ne $denseTwoRecords.Count) {
      throw 'Dense 1x/2x feedback record counts do not correspond.'
    }
    for ($index = 0; $index -lt $denseOneRecords.Count; $index++) {
      $oneRecord = $denseOneRecords[$index]
      $twoRecord = $denseTwoRecords[$index]
      $oneProgress = [double]$oneRecord.lifetimeProgress
      $twoProgress = [double]$twoRecord.lifetimeProgress
      $ratio = if ($oneProgress -le 0) { 0 } else { $twoProgress / $oneProgress }
      if ([string]$oneRecord.semanticId -cne [string]$twoRecord.semanticId -or
          [string]$oneRecord.role -cne [string]$twoRecord.role -or
          $twoProgress -le $oneProgress -or $ratio -lt 1.20 -or $ratio -gt 1.30) {
        throw "Dense 1x/2x record $index does not use the designed 1.25x display-clock rate: ratio=$ratio"
      }
    }
    $routeRecords = @($combatFeedbackTelemetry['combat-feedback-role-route'].feedback)
    $followedRouteRecord = $routeRecords |
      Where-Object { $_.followingTarget } | Select-Object -First 1
    if ($null -eq $followedRouteRecord) {
      throw 'Route role fixture did not expose a live target-follow anchor.'
    }
    $routeTargetRecord = $routeRecords |
      Where-Object { [string]$_.semanticId -ceq 'status.chili.burn' } |
      Select-Object -First 1
    if ($null -eq $routeTargetRecord) {
      throw 'Route role fixture did not expose the deterministic live target reference point.'
    }
    $followAnchorError = [Math]::Sqrt(
      [Math]::Pow([double]$followedRouteRecord.anchorX - [double]$routeTargetRecord.eventX, 2) +
      [Math]::Pow([double]$followedRouteRecord.anchorY - [double]$routeTargetRecord.eventY, 2))
    $followTravel = [Math]::Sqrt(
      [Math]::Pow([double]$followedRouteRecord.anchorX - [double]$followedRouteRecord.eventX, 2) +
      [Math]::Pow([double]$followedRouteRecord.anchorY - [double]$followedRouteRecord.eventY, 2))
    if ($followAnchorError -gt .0001 -or $followTravel -le .01) {
      throw "Route follow anchor is not the deterministic live route target: error=$followAnchorError travel=$followTravel"
    }
    $clusterTelemetry = $combatFeedbackTelemetry['combat-feedback-beat-cluster']
    $clusterRecord = $clusterTelemetry.feedback[0]
    $clusterEventError = [Math]::Sqrt(
      [Math]::Pow([double]$clusterRecord.eventX - [double]$clusterTelemetry.expectedCentroidX, 2) +
      [Math]::Pow([double]$clusterRecord.eventY - [double]$clusterTelemetry.expectedCentroidY, 2))
    $clusterAnchorError = [Math]::Sqrt(
      [Math]::Pow([double]$clusterRecord.anchorX - [double]$clusterTelemetry.expectedCentroidX, 2) +
      [Math]::Pow([double]$clusterRecord.anchorY - [double]$clusterTelemetry.expectedCentroidY, 2))
    if ([int]$clusterRecord.count -ne 3 -or
        -not [bool]$clusterTelemetry.hasExpectedCentroid -or
        [double]$clusterTelemetry.eventCentroidError -gt .0001 -or
        [double]$clusterTelemetry.anchorCentroidError -gt .0001 -or
        $clusterEventError -gt .0001 -or $clusterAnchorError -gt .0001) {
      throw 'Cluster beat fixture did not aggregate at the independently expected centroid.'
    }

    Set-AcceptanceState -State 'combat-feedback-profile'
    $profileTelemetry = Wait-CombatFeedbackProfile -State 'combat-feedback-profile'
    Assert-CombatFeedbackTelemetry -Actual $profileTelemetry `
      -State 'combat-feedback-profile' -Surface 'route' -Phase 'sync-cpu-profile' `
      -Speed 1 -Beat 'Heavy' -FeedbackCount 12 -Roles $allRoles `
      -SemanticIds $denseSemantics | Out-Null
    $profileSamples = @($profileTelemetry.profileSamplesMilliseconds |
      ForEach-Object { [double]$_ })
    $reportedP95 = [double]$profileTelemetry.profileP95Milliseconds
    $reportedAllocatedBytes = [long]$profileTelemetry.profileAllocatedBytes
    $reportedAllocatedBytesPerSecond =
      [double]$profileTelemetry.profileAllocatedBytesPerSecond
    $reportedElapsedSeconds = [double]$profileTelemetry.profileElapsedSeconds
    $performanceAccepted = [bool]$profileTelemetry.profileSupported -and
      [bool]$profileTelemetry.profileCompleted -and
      [int]$profileTelemetry.profileWarmupCount -eq 120 -and
      [int]$profileTelemetry.profileSampleCount -eq 600 -and
      $profileSamples.Count -eq 600 -and
      (Test-FiniteNumber $reportedP95) -and $reportedP95 -ge 0 -and
      $reportedAllocatedBytes -ge 0 -and
      (Test-FiniteNumber $reportedAllocatedBytesPerSecond) -and
      $reportedAllocatedBytesPerSecond -ge 0 -and
      (Test-FiniteNumber $reportedElapsedSeconds) -and
      $reportedElapsedSeconds -gt 0
    $independentP95 = $null
    if ($performanceAccepted) {
      foreach ($sample in $profileSamples) {
        if ([double]::IsNaN($sample) -or [double]::IsInfinity($sample) -or $sample -lt 0) {
          $performanceAccepted = $false
          break
        }
      }
    }
    if ($performanceAccepted) {
      $sortedSamples = @($profileSamples | Sort-Object)
      $p95Index = [Math]::Ceiling(.95 * $sortedSamples.Count) - 1
      $independentP95 = [double]$sortedSamples[$p95Index]
      if (-not (Test-FiniteNumber $independentP95) -or $independentP95 -lt 0 -or
          [Math]::Abs($independentP95 - $reportedP95) -gt .0001 -or
          $independentP95 -gt .5 -or
          $reportedAllocatedBytesPerSecond -gt 1024) {
        $performanceAccepted = $false
      }
    }
    $combatFeedbackTelemetry['combat-feedback-profile'] = $profileTelemetry
    $combatFeedbackScreenshots['combat-feedback-profile'] =
      Save-Screenshot -Name '12-combat-feedback-profile-active-12'
    $combatFeedbackCaptures += [pscustomobject]@{
      screenshot = $combatFeedbackScreenshots['combat-feedback-profile']
      state = 'combat-feedback-profile'
      roles = $allRoles
      surface = 'route'
      speed = 1
      phase = 'sync-cpu-profile'
      beat = 'Heavy'
    }

    Set-AcceptanceState -State 'combat-feedback-beat-heavy'
    $heavyBeforeInteraction = Wait-CombatFeedbackTelemetry `
      -State 'combat-feedback-beat-heavy' -Speed 1
    Invoke-CanvasClickImmediate -X $controls.headerSpeed.x -Y $controls.headerSpeed.y
    $heavyAfterInteraction = Wait-CombatFeedbackTelemetry `
      -State 'combat-feedback-beat-heavy' -Speed 2
    Assert-CombatFeedbackTelemetry -Actual $heavyAfterInteraction `
      -State 'combat-feedback-beat-heavy' -Surface 'route' -Phase 'impact-beat' `
      -Speed 2 -Beat 'Heavy' -FeedbackCount 1 -Roles @('HeavyDamage') `
      -SemanticIds @('ability.plant.watermelon.attack') | Out-Null
    $combatFeedbackScreenshots.activeBeatSpeedHitTest =
      Save-Screenshot -Name '13-combat-feedback-active-beat-speed-hit-test'
    $combatFeedbackTelemetry.activeBeatSpeedHitTest = $heavyAfterInteraction
    $combatFeedbackCaptures += [pscustomobject]@{
      screenshot = $combatFeedbackScreenshots.activeBeatSpeedHitTest
      state = 'combat-feedback-beat-heavy'
      roles = @('HeavyDamage')
      surface = 'route'
      speed = 2
      phase = 'active-beat-speed-hit-test'
      beat = 'Heavy'
    }
    $activeBeatHitTestPassed =
      [int]$heavyBeforeInteraction.battleSpeed -eq 1 -and
      [int]$heavyAfterInteraction.battleSpeed -eq 2 -and
      [string]$heavyAfterInteraction.activeBeat -ceq 'Heavy' -and
      [bool]$heavyBeforeInteraction.authoritativeGeometryUnchanged -and
      [bool]$heavyAfterInteraction.authoritativeGeometryUnchanged -and
      (Test-CombatFeedbackGeometryEqual `
        -First $heavyBeforeInteraction.geometryBefore `
        -Second $heavyAfterInteraction.geometryAfter)
    $machineCapturePassed = $activeBeatHitTestPassed
    $machineGatesPassed = $machineCapturePassed -and $performanceAccepted

    $combatFeedbackManifest = [ordered]@{
      accepted = $false
      visualAccepted = $false
      visualReviewStatus = 'pending-human-review'
      machineCapturePassed = $machineCapturePassed
      machineGatesPassed = $machineGatesPassed
      performanceAccepted = $performanceAccepted
      evidenceType = 'combat-feedback-webgl-visual-and-readonly-telemetry'
      visualFixtureCount = 11
      capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
      url = $Url
      verifiedBuildProfile = $verifiedBuildProfile
      levelId = $LevelId
      routeIdentity = $directBattleIdentity
      viewport = [ordered]@{ width = $Width; height = $Height; coordinateSpace = 'css-pixel/top-left' }
      capturePolicy = [ordered]@{
        stateSignal = 'window.fruitDefenseCombatFeedbackTelemetry.state'
        lifetimeControl = 'URL-gated presentation freeze at explicit phase'
        screenshots = 'raw DevTools Page.captureScreenshot; no stable-frame delay'
      }
      checks = [ordered]@{
        finiteRoleInventoryOnGrass = 'pass'
        finiteRoleInventoryOnRoute = 'pass'
        liveTargetFollowAnchor = 'pass'
        startEarlySettleHoldScale = 'pass'
        denseOneXCapacity = 'pass'
        denseTwoXReadabilityClock = 'pass'
        heavyBeat = 'pass'
        clusterBeatAndCentroid = 'pass'
        terminalBeat = 'pass'
        activeBeatHudHitTestGeometry = if ($activeBeatHitTestPassed) { 'pass' } else { 'fail' }
        atlasOnePageRgba32 = 'pass'
        noRuntimeMaterials = 'pass'
        poolCapacity9999WithTwelveRecordFixture = 'pass'
        syncCpuPerformanceGate = if ($performanceAccepted) { 'pass' } else { 'fail' }
      }
      telemetry = $combatFeedbackTelemetry
      screenshots = $combatFeedbackScreenshots
      captures = $combatFeedbackCaptures
      performance = [ordered]@{
        accepted = $performanceAccepted
        scope = 'CombatFloatingTextSdfOverlay command preparation plus final IMGUI-layer RGBA atlas glyph submissions; GPU raster excluded'
        marker = 'FruitDefense.CombatFloatingText.Render'
        requiredWarmupSamples = [int]$profileTelemetry.profileWarmupRequired
        observedWarmupSamples = [int]$profileTelemetry.profileWarmupCount
        requiredActiveSamples = [int]$profileTelemetry.profileSampleRequired
        observedActiveSamples = [int]$profileTelemetry.profileSampleCount
        requiredActiveCount = 12
        rawSamplesMilliseconds = $profileSamples
        p95Algorithm = [string]$profileTelemetry.profileP95Algorithm
        p95Milliseconds = [double]$profileTelemetry.profileP95Milliseconds
        independentlyRecomputedP95Milliseconds = $independentP95
        p95GateMilliseconds = .5
        totalAllocatedBytes = [long]$profileTelemetry.profileAllocatedBytes
        allocatedBytesPerSecond = [double]$profileTelemetry.profileAllocatedBytesPerSecond
        allocatedBytesPerSecondGate = 1024
        elapsedSeconds = [double]$profileTelemetry.profileElapsedSeconds
        supported = [bool]$profileTelemetry.profileSupported
        failure = [string]$profileTelemetry.profileFailure
        resourceTopologyBefore = [ordered]@{
          atlasPages = [int]$combatFeedbackTelemetry['combat-feedback-role-grass'].atlasPageCount
          sharedMaterials = [int]$combatFeedbackTelemetry['combat-feedback-role-grass'].sharedMaterialCount
          poolCapacity = [int]$combatFeedbackTelemetry['combat-feedback-role-grass'].poolCapacity
        }
        resourceTopologyAfter = [ordered]@{
          atlasPages = [int]$profileTelemetry.atlasPageCount
          sharedMaterials = [int]$profileTelemetry.sharedMaterialCount
          preparedAtlasDrawCount = [int]$profileTelemetry.preparedAtlasDrawCount
          poolCapacity = [int]$profileTelemetry.poolCapacity
          activeCount = [int]$profileTelemetry.activePoolCount
        }
        submissionEvidence = 'prepared fixed glyph commands are submitted from the final IMGUI layer against one RGBA atlas; GPU raster inspection is excluded'
      }
    }
    $combatFeedbackManifestPath = Join-Path $outputDir 'combat-feedback-evidence.json'
    $combatFeedbackManifest | ConvertTo-Json -Depth 16 |
      Set-Content -LiteralPath $combatFeedbackManifestPath -Encoding UTF8
    if (-not $machineGatesPassed) {
      Write-Host "FRUIT_DEFENSE_COMBAT_FEEDBACK_CAPTURE_FAILED manifest=$combatFeedbackManifestPath"
      throw "Combat-feedback machine capture failed closed. Manifest: $combatFeedbackManifestPath"
    }
    Write-Host "FRUIT_DEFENSE_COMBAT_FEEDBACK_CAPTURE_OK manifest=$combatFeedbackManifestPath visual=pending-human-review"
    return
}
