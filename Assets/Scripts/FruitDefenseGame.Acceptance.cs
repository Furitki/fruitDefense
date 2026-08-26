using System;
using System.Collections.Generic;
using System.Linq;
#if FRUIT_DEFENSE_ACCEPTANCE
using System.Runtime.InteropServices;
#endif
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Platform;
using FruitDefense.Presentation;
using FruitDefense.Tilemaps;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense
{
    public sealed partial class FruitDefenseGame
    {
#if FRUIT_DEFENSE_ACCEPTANCE
        [Serializable]
        private sealed class CombatFeedbackAcceptanceRecord
        {
            public long sequence;
            public string role = string.Empty;
            public string semanticId = string.Empty;
            public int count;
            public float eventX;
            public float eventY;
            public float anchorX;
            public float anchorY;
            public float lifetimeProgress;
            public float detachedProgress;
            public float motionScale;
            public float motionOpacity;
            public bool followingTarget;
            public float finalScreenCenterX;
            public float finalScreenCenterY;
            public float anchorScreenX;
            public float anchorScreenY;
            public float anchorScreenError;
            public float finalScreenBoundsX;
            public float finalScreenBoundsY;
            public float finalScreenBoundsWidth;
            public float finalScreenBoundsHeight;
        }

        [Serializable]
        private sealed class CombatFeedbackAcceptanceGeometry
        {
            public float headerX;
            public float headerY;
            public float headerWidth;
            public float headerHeight;
            public float boardX;
            public float boardY;
            public float boardWidth;
            public float boardHeight;
            public float potHitX;
            public float potHitY;
            public float potHitWidth;
            public float potHitHeight;
        }

        [Serializable]
        private sealed class CombatFeedbackAcceptanceTelemetry
        {
            public int schemaVersion = 1;
            public string state = string.Empty;
            public string surface = string.Empty;
            public string phase = string.Empty;
            public int battleSpeed;
            public string activeRole = string.Empty;
            public string[] activeRoles = Array.Empty<string>();
            public string activeBeat = string.Empty;
            public float beatProgress;
            public float battlefieldOffsetX;
            public float battlefieldOffsetY;
            public float battlefieldFlash;
            public bool hasExpectedCentroid;
            public float expectedCentroidX;
            public float expectedCentroidY;
            public float eventCentroidError;
            public float anchorCentroidError;
            public CombatFeedbackAcceptanceGeometry geometryBefore;
            public CombatFeedbackAcceptanceGeometry geometryAfter;
            public bool authoritativeGeometryUnchanged;
            public int feedbackCount;
            public int ordinaryFeedbackCount;
            public int activePoolCount;
            public int poolCapacity;
            public int allocatedFeedbackCount;
            public int pooledFeedbackCount;
            public int missingProfileCount;
            public int atlasPageCount;
            public string atlasFormat = string.Empty;
            public int sharedMaterialCount;
            public int preparedAtlasDrawCount;
            public bool placementValid;
            public string placementFailure = string.Empty;
            public string renderMarker = string.Empty;
            public string performanceSampling = string.Empty;
            public string performanceScope = string.Empty;
            public string profileAllocationMetric = string.Empty;
            public bool profileSupported;
            public bool profileActive;
            public bool profileCompleted;
            public int profileWarmupCount;
            public int profileWarmupRequired;
            public int profileSampleCount;
            public int profileSampleRequired;
            public float[] profileSamplesMilliseconds = Array.Empty<float>();
            public float profileP95Milliseconds;
            public long profileAllocatedBytes;
            public float profileAllocatedBytesPerSecond;
            public float profileElapsedSeconds;
            public string profileP95Algorithm = string.Empty;
            public string profileFailure = string.Empty;
            public CombatFeedbackAcceptanceRecord[] feedback =
                Array.Empty<CombatFeedbackAcceptanceRecord>();
        }
        private bool _acceptanceTerminalPreview;
        private bool _acceptanceFeedbackFrozen;
        private string _acceptanceFeedbackState = string.Empty;
        private string _acceptanceFeedbackSurface = string.Empty;
        private string _acceptanceFeedbackPhase = string.Empty;
        private string _combatFeedbackAcceptanceTelemetryJson = string.Empty;
        private bool _acceptanceHasExpectedCentroid;
        private Vector2 _acceptanceExpectedCentroid;
        private CombatFeedbackAcceptanceGeometry _acceptanceGeometryBefore;
        private float[] _acceptanceProfileSamplesCache = Array.Empty<float>();
        private string _lastPublishedAcceptanceState = string.Empty;
        private int _lastPublishedAcceptanceSpeed = -1;
        private bool _lastPublishedAcceptanceProfileActive;
        private bool _lastPublishedAcceptanceProfileSupported;
        private bool _lastPublishedAcceptanceProfileCompleted;
        public string CombatFeedbackAcceptanceTelemetryJson
        {
            get { return _combatFeedbackAcceptanceTelemetryJson; }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void FruitDefensePublishCombatFeedbackTelemetry(
            string json);
#endif
        public void ConfigureAcceptanceState(string stateName)
        {
            var result = ConfigureAcceptanceState(stateName, Application.absoluteURL);
            if (!result.Succeeded) Debug.LogError(result.ErrorCode);
        }

        private AcceptanceCommandResult ConfigureAcceptanceState(
            string stateName, string absoluteUrl)
        {
            if (!AcceptanceLaunchQuery.IsEnabled(absoluteUrl))
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.LaunchRequired);
            return TryConfigureNamedState(stateName);
        }

        public AcceptanceCommandResult TryConfigureNamedState(string stateName)
        {
            if (!_isInitialized || _game == null)
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.SessionUnavailable);
            if (string.IsNullOrWhiteSpace(stateName))
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.NamedStateRequired);
            if (!IsKnownAcceptanceState(stateName))
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.NamedStateUnknown);
            if (_resultSubmitted)
                return AcceptanceCommandResult.Failure(ResultAlreadySubmitted);

            _acceptanceTerminalPreview = false;
            _acceptanceFeedbackFrozen = false;
            _acceptanceFeedbackState = string.Empty;
            _acceptanceFeedbackSurface = string.Empty;
            _acceptanceFeedbackPhase = string.Empty;
            _combatFeedbackAcceptanceTelemetryJson = string.Empty;
            _acceptanceHasExpectedCentroid = false;
            _acceptanceExpectedCentroid = Vector2.zero;
            _lastPublishedAcceptanceState = string.Empty;
            _lastPublishedAcceptanceSpeed = -1;
            _lastPublishedAcceptanceProfileActive = false;
            _lastPublishedAcceptanceProfileSupported = false;
            _lastPublishedAcceptanceProfileCompleted = false;
            _game.Reset(20260714);
            _game.DiscardPendingPresentationEvents();
            _presentation.Clear();
            _battleUiLayout = new BattleUiLayout(_game.Map);
            _acceptanceGeometryBefore = CaptureAcceptanceGeometry();
            _game.State.Pots.Clear();
            _game.State.Plants.Clear();
            _game.State.Zombies.Clear();
            _game.State.Projectiles.Clear();
            ResetInteractionState();

            if (!ConfigureCombatFeedbackAcceptance(stateName)) switch (stateName)
            {
                case "adjacent-pots":
                    GetAcceptanceAdjacentCells(out var firstAdjacent, out var secondAdjacent);
                    AddAcceptancePot(firstAdjacent, BattleContentIds.Plants.Pea);
                    AddAcceptancePot(secondAdjacent, BattleContentIds.Plants.Watermelon);
                    break;
                case "drag-target":
                    AddAcceptancePot(GetAcceptanceCell(0));
                    _game.State.Plants.Add(new Plant
                    {
                        Id = _game.State.NextId++, DefinitionId = BattleContentIds.Plants.Pea,
                        Star = 1, NurseryIndex = 0,
                    });
                    break;
                case "selection-inspection":
                    AddAcceptancePot(GetAcceptanceCell(0), BattleContentIds.Plants.Pea);
                    AddAcceptancePot(GetAcceptanceCell(1));
                    _game.State.Plants.Add(new Plant
                    {
                        Id = _game.State.NextId++, DefinitionId = BattleContentIds.Plants.Sunflower,
                        Star = 1, NurseryIndex = 0,
                    });
                    break;
                case "selected-tool":
                    AddAcceptancePot(GetAcceptanceCell(0), BattleContentIds.Plants.Pea);
                    _game.State.Inventory.Set(
                        BattleContentIds.Equipment.Gatling, 1);
                    break;
                case "terminal-victory":
                    _game.State.Phase = GamePhase.Victory;
                    _game.State.WaveIndex = _game.MaxWaves;
                    _game.State.Lives = 3;
                    _acceptanceTerminalPreview = true;
                    break;
                case "terminal-defeat":
                    _game.State.Phase = GamePhase.Defeat;
                    _game.State.WaveIndex = Math.Min(6, _game.MaxWaves);
                    _game.State.Lives = 0;
                    _acceptanceTerminalPreview = true;
                    break;
                case "active-wave":
                    AddAcceptancePot(GetAcceptanceCell(0), BattleContentIds.Plants.Pea);
                    AddAcceptancePot(GetAcceptanceCell(_game.Map.PlantableCells.Count - 1),
                        BattleContentIds.Plants.Watermelon);
                    _game.StartWave(out _);
                    break;
                case "between-wave":
                    AddAcceptancePot(GetAcceptanceCell(0), BattleContentIds.Plants.Pea);
                    AddAcceptancePot(GetAcceptanceCell(_game.Map.PlantableCells.Count - 1),
                        BattleContentIds.Plants.Watermelon);
                    _game.State.Phase = GamePhase.BetweenWaves;
                    _game.State.WaveIndex = 1;
                    _game.State.BetweenTimer = 9.5f;
                    break;
                case "dense-board":
                    var kind = 0;
                    var plantIds = BattlePresentationVisualCatalog.BundledPlantDefinitionIds;
                    foreach (var cell in _game.Map.PlantableCells)
                    {
                        AddAcceptancePot(cell, plantIds[kind % plantIds.Count]);
                        kind++;
                    }
                    break;
                default:
                    for (var index = 0; index < Mathf.Min(8, _game.Map.PlantableCells.Count); index++)
                        AddAcceptancePot(GetAcceptanceCell(index));
                    break;
            }
            _renderSamples.SnapTo(_game.State);
            if (_acceptanceFeedbackFrozen)
            {
                ResolveFloatingTextFollowAnchors();
                SyncFloatingTextOverlay();
                PublishCombatFeedbackAcceptanceTelemetry();
                if (stateName == "combat-feedback-profile")
                {
                    _acceptanceProfileSamplesCache = Array.Empty<float>();
                    _floatingTextOverlay.BeginAcceptanceSyncProfile();
                    PublishCombatFeedbackAcceptanceTelemetry();
                }
            }
            return AcceptanceCommandResult.Success();
        }

        public AcceptanceCommandResult TryConfigureTerminalFixture(
            AcceptanceTerminalFixture fixture)
        {
            if (!_isInitialized || _game == null)
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.SessionUnavailable);
            if (fixture != AcceptanceTerminalFixture.Victory
                && fixture != AcceptanceTerminalFixture.Defeat)
                return AcceptanceCommandResult.Failure(
                    AcceptanceCommandResult.TerminalFixtureUnknown);
            if (_resultSubmitted)
                return AcceptanceCommandResult.Failure(ResultAlreadySubmitted);

            switch (fixture)
            {
                case AcceptanceTerminalFixture.Victory:
                    _game.State.Phase = GamePhase.Victory;
                    _game.State.WaveIndex = _game.MaxWaves;
                    _game.State.Lives = 3;
                    break;
                case AcceptanceTerminalFixture.Defeat:
                    _game.State.Phase = GamePhase.Defeat;
                    _game.State.Lives = 0;
                    break;
            }

            _acceptanceTerminalPreview = false;
            if (!TrySubmitTerminalResult())
                return AcceptanceCommandResult.Failure(
                    string.IsNullOrEmpty(_lastResultSubmissionError)
                        ? SessionNotInitialized
                        : _lastResultSubmissionError);
            return string.IsNullOrEmpty(_lastResultSubmissionError)
                ? AcceptanceCommandResult.Success()
                : AcceptanceCommandResult.Failure(
                    _lastResultSubmissionError);
        }

        private static bool IsKnownAcceptanceState(string stateName)
        {
            switch (stateName)
            {
                case "initial":
                case "adjacent-pots":
                case "drag-target":
                case "selection-inspection":
                case "selected-tool":
                case "terminal-victory":
                case "terminal-defeat":
                case "active-wave":
                case "between-wave":
                case "dense-board":
                case "combat-feedback-role-grass":
                case "combat-feedback-role-route":
                case "combat-feedback-rebound-entry":
                case "combat-feedback-rebound-peak":
                case "combat-feedback-rebound-return":
                case "combat-feedback-rebound-hold":
                case "combat-feedback-dense-1x":
                case "combat-feedback-dense-2x":
                case "combat-feedback-profile":
                case "combat-feedback-beat-heavy":
                case "combat-feedback-beat-cluster":
                case "combat-feedback-beat-terminal":
                    return true;
                default:
                    return false;
            }
        }

#endif
    }
}
