# Dot-sourced by accept-webgl-portrait.ps1. Keep this module scoped to the acceptance runner.

$hubMorningDewItemId = 'item.growth.morning-dew'
$hubStarterReceiptId = 'receipt.activity.orchard.starter-supplies'
$hubStarterEquipmentId = 'growth-equipment.sunleaf-emblem'
$hubOffenseSlotId = 'growth-slot.offense'
$hubVitalRootsId = 'cultivation.vital-roots'
$hubOrchard02PolicyId = 'growth-policy.orchard-02'

function Get-HubItemQuantity {
  param([object]$Telemetry, [string]$ItemId)
  $matches = @($Telemetry.itemBalances | Where-Object {
      [string]$_.itemId -ceq $ItemId
    })
  if ($matches.Count -eq 0) { return [long]0 }
  if ($matches.Count -ne 1) {
    throw "Hub telemetry contains duplicate item balances for '$ItemId'."
  }
  return [long]$matches[0].quantity
}

function Get-HubEquipmentRank {
  param([object]$Telemetry, [string]$EquipmentId)
  $matches = @($Telemetry.growthEquipment | Where-Object {
      [string]$_.growthEquipmentId -ceq $EquipmentId
    })
  if ($matches.Count -eq 0) { return $null }
  if ($matches.Count -ne 1) {
    throw "Hub telemetry contains duplicate growth equipment for '$EquipmentId'."
  }
  return [int]$matches[0].rank
}

function Get-HubCultivationRank {
  param([object]$Telemetry, [string]$NodeId)
  $matches = @($Telemetry.cultivation | Where-Object {
      [string]$_.cultivationNodeId -ceq $NodeId
    })
  if ($matches.Count -eq 0) { return [int]0 }
  if ($matches.Count -ne 1) {
    throw "Hub telemetry contains duplicate cultivation nodes for '$NodeId'."
  }
  return [int]$matches[0].rank
}

function Test-HubEquipmentEquipped {
  param([object]$Telemetry, [string]$SlotId, [string]$EquipmentId)
  return @($Telemetry.loadout | Where-Object {
      [string]$_.slotId -ceq $SlotId -and
      [string]$_.growthEquipmentId -ceq $EquipmentId
    }).Count -eq 1
}

function Save-HubLoopCapture {
  param(
    [string]$Name,
    [object]$Telemetry,
    [switch]$RequireHud
  )
  Move-CanvasPointerOut
  $capture = Save-StableScreenshot -Name $Name -RequireHud:$RequireHud
  if ($capture.Metrics.width -ne $Width -or
      $capture.Metrics.height -ne $Height -or
      -not (Test-StableFrameMetrics -Metrics $capture.Metrics)) {
    throw "Hub loop state '$Name' produced an invalid frame: $($capture.Metrics | ConvertTo-Json -Compress)"
  }
  return [ordered]@{
    screenshot = $capture.Path
    screenshotSha256 = (Get-FileHash -LiteralPath $capture.Path -Algorithm SHA256).Hash
    imageMetrics = $capture.Metrics
    routeIdentity = Get-AcceptanceIdentity
    hubTelemetry = $Telemetry
  }
}

function Complete-HubLoopSettlement {
  param([string]$StagePrefix)

  Invoke-AcceptanceFlowCommand -Command $SettlementOutcome
  Wait-SettlementOutcomeRevealState -State settled-hidden | Out-Null
  Release-SettlementOutcomeReveal
  Wait-AppRoute -Route 2
  $identity = Wait-AcceptanceIdentity `
    -Route 2 -Stage "$StagePrefix-settlement" -SessionMode Required
  Wait-SettlementOutcomeRevealState -State stable | Out-Null
  $telemetry = Wait-HubAcceptanceTelemetry `
    -FixtureMode Forbidden -Route 2 -Stage "$StagePrefix-settlement-hub"
  return [ordered]@{ identity = $identity; telemetry = $telemetry }
}

function Invoke-HubLoopMode {
  if ([string]$LevelId -cne 'orchard-02') {
    throw '-HubLoop requires -LevelId orchard-02 for the applied/suppressed policy proof.'
  }
  Wait-AppRoute -Route 0
  Set-HubAcceptanceState -State 'reward-to-battle'

  $captures = [ordered]@{}
  $identities = [ordered]@{}
  $telemetry = [ordered]@{}
  $freshCondition = {
    param($actual)
    [long]$actual.profileRevision -eq 0 -and
      [int]$actual.receiptCount -eq 0 -and
      [int]$actual.committedRewardRevisionCount -eq 0 -and
      [int]$actual.committedGrowthRevisionCount -eq 0 -and
      (Get-HubItemQuantity -Telemetry $actual -ItemId $hubMorningDewItemId) -eq 0 -and
      $null -eq (Get-HubEquipmentRank -Telemetry $actual -EquipmentId $hubStarterEquipmentId) -and
      (Get-HubCultivationRank -Telemetry $actual -NodeId $hubVitalRootsId) -eq 0
  }
  $telemetry.freshHome = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'home' -FixtureMode Forbidden `
    -Route 0 -Stage 'loop-fresh-home' -Condition $freshCondition
  $profileId = [string]$telemetry.freshHome.profileId
  $captures.freshHome = Save-HubLoopCapture -Name '01-loop-fresh-home' `
    -Telemetry $telemetry.freshHome

  Invoke-CanvasClick -X $controls.hubNavActivity.x -Y $controls.hubNavActivity.y
  $telemetry.activityClaimable = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'activity' -FixtureMode Forbidden `
    -Route 0 -Stage 'loop-activity-claimable' -Condition $freshCondition
  $captures.activityClaimable = Save-HubLoopCapture `
    -Name '02-loop-activity-claimable' -Telemetry $telemetry.activityClaimable

  Invoke-CanvasClickImmediate `
    -X $controls.hubActivityClaim.x -Y $controls.hubActivityClaim.y
  $claimedCondition = {
    param($actual)
    [string]$actual.profileId -ceq $profileId -and
      [long]$actual.profileRevision -eq 1 -and
      [int]$actual.receiptCount -eq 1 -and
      [int]$actual.committedRewardRevisionCount -eq 1 -and
      [int]$actual.committedGrowthRevisionCount -eq 0 -and
      [string]$actual.lastCommand -ceq 'ClaimActivity' -and
      [string]$actual.lastCommandStatus -ceq 'Success' -and
      -not [bool]$actual.commandInProgress -and
      (Get-HubItemQuantity -Telemetry $actual -ItemId $hubMorningDewItemId) -eq 6 -and
      (Get-HubEquipmentRank -Telemetry $actual -EquipmentId $hubStarterEquipmentId) -eq 0
  }
  $telemetry.activityClaimed = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'activity' -FixtureMode Forbidden `
    -Route 0 -Stage 'loop-activity-claimed' -Condition $claimedCondition
  $captures.activityClaimed = Save-HubLoopCapture `
    -Name '03-loop-activity-claimed' -Telemetry $telemetry.activityClaimed

  Invoke-CanvasClick -X $controls.hubNavGrowth.x -Y $controls.hubNavGrowth.y
  $telemetry.equipmentOwned = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'growth' -GrowthPage 'equipment' `
    -FixtureMode Forbidden -Route 0 -Stage 'loop-equipment-owned' `
    -Condition $claimedCondition
  $captures.equipmentOwned = Save-HubLoopCapture `
    -Name '04-loop-equipment-owned' -Telemetry $telemetry.equipmentOwned
  Invoke-CanvasClick -X $controls.hubGrowthEntry.x -Y $controls.hubGrowthEntry.y
  Invoke-CanvasClickImmediate `
    -X $controls.hubEquipmentPrimary.x -Y $controls.hubEquipmentPrimary.y
  $equippedCondition = {
    param($actual)
    [string]$actual.profileId -ceq $profileId -and
      [long]$actual.profileRevision -eq 2 -and
      [int]$actual.receiptCount -eq 1 -and
      [int]$actual.committedRewardRevisionCount -eq 1 -and
      [int]$actual.committedGrowthRevisionCount -eq 1 -and
      [string]$actual.lastCommand -ceq 'EquipGrowthEquipment' -and
      [string]$actual.lastCommandStatus -ceq 'Success' -and
      -not [bool]$actual.commandInProgress -and
      (Get-HubItemQuantity -Telemetry $actual -ItemId $hubMorningDewItemId) -eq 6 -and
      (Test-HubEquipmentEquipped -Telemetry $actual `
        -SlotId $hubOffenseSlotId -EquipmentId $hubStarterEquipmentId)
  }
  $telemetry.equipped = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'growth' -GrowthPage 'equipment' `
    -FixtureMode Forbidden -Route 0 -Stage 'loop-equipment-equipped' `
    -Condition $equippedCondition
  $captures.equipped = Save-HubLoopCapture `
    -Name '05-loop-equipment-equipped' -Telemetry $telemetry.equipped

  Invoke-CanvasClick `
    -X $controls.hubGrowthCultivation.x -Y $controls.hubGrowthCultivation.y
  $telemetry.cultivationReady = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'growth' -GrowthPage 'cultivation' `
    -FixtureMode Forbidden -Route 0 -Stage 'loop-cultivation-ready' `
    -Condition $equippedCondition
  $captures.cultivationReady = Save-HubLoopCapture `
    -Name '06-loop-cultivation-ready' -Telemetry $telemetry.cultivationReady
  Invoke-CanvasClick -X $controls.hubGrowthEntry.x -Y $controls.hubGrowthEntry.y
  Invoke-CanvasClickImmediate `
    -X $controls.hubCultivationPrimary.x -Y $controls.hubCultivationPrimary.y
  $cultivatedCondition = {
    param($actual)
    [string]$actual.profileId -ceq $profileId -and
      [long]$actual.profileRevision -eq 3 -and
      [int]$actual.receiptCount -eq 1 -and
      [int]$actual.committedRewardRevisionCount -eq 1 -and
      [int]$actual.committedGrowthRevisionCount -eq 2 -and
      [string]$actual.lastCommand -ceq 'UpgradeCultivation' -and
      [string]$actual.lastCommandStatus -ceq 'Success' -and
      -not [bool]$actual.commandInProgress -and
      (Get-HubItemQuantity -Telemetry $actual -ItemId $hubMorningDewItemId) -eq 0 -and
      (Get-HubCultivationRank -Telemetry $actual -NodeId $hubVitalRootsId) -eq 1 -and
      (Test-HubEquipmentEquipped -Telemetry $actual `
        -SlotId $hubOffenseSlotId -EquipmentId $hubStarterEquipmentId)
  }
  $telemetry.cultivated = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'growth' -GrowthPage 'cultivation' `
    -FixtureMode Forbidden -Route 0 -Stage 'loop-cultivation-upgraded' `
    -Condition $cultivatedCondition
  $captures.cultivated = Save-HubLoopCapture `
    -Name '07-loop-cultivation-upgraded' -Telemetry $telemetry.cultivated

  Invoke-CanvasClick -X $controls.hubNavHome.x -Y $controls.hubNavHome.y
  Invoke-CanvasClick `
    -X $controls.lobbyLevelOrchard02.x -Y $controls.lobbyLevelOrchard02.y
  $previewCondition = {
    param($actual)
    [string]$actual.profileId -ceq $profileId -and
      [long]$actual.profileRevision -eq 4 -and
      [int]$actual.receiptCount -eq 1 -and
      [int]$actual.committedRewardRevisionCount -eq 1 -and
      [int]$actual.committedGrowthRevisionCount -eq 2 -and
      (Get-HubItemQuantity -Telemetry $actual -ItemId $hubMorningDewItemId) -eq 0 -and
      (Get-HubCultivationRank -Telemetry $actual -NodeId $hubVitalRootsId) -eq 1 -and
      (Test-HubEquipmentEquipped -Telemetry $actual `
        -SlotId $hubOffenseSlotId -EquipmentId $hubStarterEquipmentId) -and
      [string]$actual.selectedLevelId -ceq 'orchard-02' -and
      [string]$actual.growthPolicyId -ceq $hubOrchard02PolicyId -and
      [int]$actual.appliedSourceCount -ge 1 -and
      [int]$actual.suppressedSourceCount -ge 1
  }
  $telemetry.policyPreview = Wait-HubAcceptanceTelemetry `
    -StateId 'reward-to-battle' -Page 'home' -FixtureMode Forbidden `
    -Route 0 -Stage 'loop-orchard-02-policy-preview' -Condition $previewCondition
  $previewFingerprint = [string]$telemetry.policyPreview.growthFingerprint
  $captures.policyPreview = Save-HubLoopCapture `
    -Name '08-loop-orchard-02-policy-preview' `
    -Telemetry $telemetry.policyPreview

  Invoke-CanvasClickImmediate -X $controls.hubStart.x -Y $controls.hubStart.y
  Wait-AppRoute -Route 1
  $identities.firstBattle = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'hub-loop-first-battle' -SessionMode Required
  $launchCondition = {
    param($actual)
    [string]$actual.profileId -ceq $profileId -and
      [long]$actual.profileRevision -eq 4 -and
      [long]$actual.launchGrowthProfileRevision -eq 4 -and
      [string]$actual.launchGrowthPolicyId -ceq $hubOrchard02PolicyId -and
      [string]$actual.launchGrowthFingerprint -ceq $previewFingerprint -and
      [string]$actual.growthFingerprint -ceq $previewFingerprint -and
      -not [bool]$actual.fixtureActive
  }
  $telemetry.firstBattle = Wait-HubAcceptanceTelemetry `
    -FixtureMode Forbidden -Route 1 -Stage 'loop-first-battle-growth' `
    -Condition $launchCondition
  $captures.firstBattle = Save-HubLoopCapture -Name '09-loop-first-battle' `
    -Telemetry $telemetry.firstBattle -RequireHud

  $firstSettlement = Complete-HubLoopSettlement -StagePrefix 'first'
  $identities.firstSettlement = $firstSettlement.identity
  Assert-SameSession -Expected $identities.firstBattle `
    -Actual $identities.firstSettlement -Stage 'hub-loop-first-settlement'
  $telemetry.firstSettlement = $firstSettlement.telemetry
  $captures.firstSettlement = Save-HubLoopCapture `
    -Name '10-loop-first-settlement' -Telemetry $telemetry.firstSettlement

  Invoke-CanvasClick -X $controls.settlementReturn.x -Y $controls.settlementReturn.y
  Wait-AppRoute -Route 0
  $identities.returnedHome = Wait-AcceptanceIdentity `
    -Route 0 -Stage 'hub-loop-returned-home' -SessionMode Cleared
  $returnedCondition = {
    param($actual)
    [string]$actual.profileId -ceq $profileId -and
      [long]$actual.profileRevision -eq 4 -and
      [int]$actual.receiptCount -eq 1 -and
      [string]$actual.selectedLevelId -ceq 'orchard-02' -and
      [string]$actual.growthFingerprint -ceq $previewFingerprint -and
      [int]$actual.appliedSourceCount -ge 1 -and
      [int]$actual.suppressedSourceCount -ge 1 -and
      -not [bool]$actual.fixtureActive
  }
  $telemetry.returnedHome = Wait-HubAcceptanceTelemetry `
    -Page 'home' -FixtureMode Forbidden -Route 0 `
    -Stage 'loop-returned-home-growth' -Condition $returnedCondition
  $captures.returnedHome = Save-HubLoopCapture -Name '11-loop-returned-home' `
    -Telemetry $telemetry.returnedHome

  Invoke-CanvasClickImmediate -X $controls.hubStart.x -Y $controls.hubStart.y
  Wait-AppRoute -Route 1
  $identities.secondBattle = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'hub-loop-second-battle' -SessionMode Required
  Assert-FreshSession -Previous $identities.firstSettlement `
    -Actual $identities.secondBattle -Stage 'hub-loop-second-battle'
  $telemetry.secondBattle = Wait-HubAcceptanceTelemetry `
    -FixtureMode Forbidden -Route 1 -Stage 'loop-second-battle-growth' `
    -Condition $launchCondition
  $captures.secondBattle = Save-HubLoopCapture -Name '12-loop-second-battle' `
    -Telemetry $telemetry.secondBattle -RequireHud

  $secondSettlement = Complete-HubLoopSettlement -StagePrefix 'second'
  $identities.secondSettlement = $secondSettlement.identity
  Assert-SameSession -Expected $identities.secondBattle `
    -Actual $identities.secondSettlement -Stage 'hub-loop-second-settlement'
  $telemetry.secondSettlement = $secondSettlement.telemetry
  $captures.secondSettlement = Save-HubLoopCapture `
    -Name '13-loop-second-settlement' -Telemetry $telemetry.secondSettlement

  Invoke-CanvasClickImmediate `
    -X $controls.settlementRetry.x -Y $controls.settlementRetry.y
  Wait-AppRoute -Route 1
  $identities.retryBattle = Wait-AcceptanceIdentity `
    -Route 1 -Stage 'hub-loop-retry-battle' -SessionMode Required
  Assert-FreshSession -Previous $identities.secondSettlement `
    -Actual $identities.retryBattle -Stage 'hub-loop-retry-battle'
  $telemetry.retryBattle = Wait-HubAcceptanceTelemetry `
    -FixtureMode Forbidden -Route 1 -Stage 'loop-retry-growth' `
    -Condition $launchCondition
  $captures.retryBattle = Save-HubLoopCapture -Name '14-loop-retry-battle' `
    -Telemetry $telemetry.retryBattle -RequireHud

  $manifest = [ordered]@{
    schemaVersion = 1
    evidenceType = 'outgame-reward-to-battle-webgl-loop'
    accepted = $true
    completion = 'fresh-profile-claim-equip-cultivation-preview-battle-return-retry'
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
    profileId = $profileId
    selectedLevelId = 'orchard-02'
    growthPolicyId = $hubOrchard02PolicyId
    previewGrowthFingerprint = $previewFingerprint
    routeIdentities = $identities
    telemetry = $telemetry
    captures = $captures
    checks = [ordered]@{
      freshBrowserProfile = 'pass'
      realCanvasInteractionsOnly = 'pass'
      fixtureDataAbsent = 'pass'
      oneRewardReceipt = 'pass'
      oneCommittedRewardRevision = 'pass'
      equipCommitted = 'pass'
      cultivationUpgradeCommitted = 'pass'
      exactItemDebit = 'pass'
      appliedAndSuppressedPreview = 'pass'
      launchGrowthMatchesPreview = 'pass'
      settlementReturnPreservesGrowth = 'pass'
      secondBattleFreshSessionAndSeed = 'pass'
      retryFreshSessionAndSeed = 'pass'
      retryReusesGrowthSnapshot = 'pass'
      noDuplicateReceiptOrDebit = 'pass'
      screenshotDimensions = 'pass'
      noBlackTransparentOrLargeNearBlackRegions = 'pass'
    }
  }
  $manifestPath = Join-Path $outputDir 'hub-loop-evidence.json'
  $manifest | ConvertTo-Json -Depth 16 |
    Set-Content -LiteralPath $manifestPath -Encoding UTF8
  Write-Host "FRUIT_DEFENSE_HUB_LOOP_OK manifest=$manifestPath"
}
