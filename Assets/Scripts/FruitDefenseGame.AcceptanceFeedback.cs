using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool ConfigureCombatFeedbackAcceptance(string stateName)
        {
            var stream = new BattlePresentationEventStream(64);
            var advanceSeconds = .06f;
            var speed = 1;
            var surface = "route";
            var phase = "hold";

            switch (stateName)
            {
                case "combat-feedback-role-grass":
                    surface = "grass";
                    phase = "role-inventory";
                    EmitAcceptanceRoleInventory(stream, AcceptanceGrassAnchors(), 0);
                    break;
                case "combat-feedback-role-route":
                    phase = "role-inventory";
                    var routeTarget = AddAcceptanceZombie(.42f);
                    EmitAcceptanceRoleInventory(
                        stream, AcceptanceRouteAnchors(), routeTarget.Id);
                    break;
                case "combat-feedback-rebound-entry":
                    phase = "entry";
                    advanceSeconds = AcceptanceHeavyLifetimeSeconds(.06f);
                    EmitAcceptanceHeavy(stream, AcceptanceRoutePoint(.48f));
                    break;
                case "combat-feedback-rebound-peak":
                    phase = "peak";
                    advanceSeconds = AcceptanceHeavyLifetimeSeconds(.12f);
                    EmitAcceptanceHeavy(stream, AcceptanceRoutePoint(.48f));
                    break;
                case "combat-feedback-rebound-return":
                    phase = "rebound";
                    advanceSeconds = AcceptanceHeavyLifetimeSeconds(.28f);
                    EmitAcceptanceHeavy(stream, AcceptanceRoutePoint(.48f));
                    break;
                case "combat-feedback-rebound-hold":
                    phase = "hold";
                    advanceSeconds = AcceptanceHeavyLifetimeSeconds(.50f);
                    EmitAcceptanceHeavy(stream, AcceptanceRoutePoint(.48f));
                    break;
                case "combat-feedback-dense-1x":
                    phase = "dense";
                    speed = 1;
                    EmitAcceptanceDense(stream);
                    break;
                case "combat-feedback-dense-2x":
                    phase = "dense";
                    speed = 2;
                    EmitAcceptanceDense(stream);
                    break;
                case "combat-feedback-profile":
                    phase = "sync-cpu-profile";
                    EmitAcceptanceDense(stream);
                    break;
                case "combat-feedback-beat-heavy":
                    phase = "impact-beat";
                    advanceSeconds = .035f;
                    EmitAcceptanceHeavy(stream, AcceptanceRoutePoint(.55f));
                    break;
                case "combat-feedback-beat-cluster":
                    phase = "impact-beat";
                    advanceSeconds = .035f;
                    _acceptanceExpectedCentroid = EmitAcceptanceCluster(stream);
                    _acceptanceHasExpectedCentroid = true;
                    break;
                case "combat-feedback-beat-terminal":
                    phase = "impact-beat";
                    advanceSeconds = .035f;
                    stream.EmitEntityDefeated(40,
                        BattleContentIds.Abilities.WatermelonAttack,
                        BattleContentIds.Enemies.Boss, 7001, 7101,
                        AcceptanceRoutePoint(.64f), Vector2.left, 50f);
                    break;
                default:
                    return false;
            }

            var events = new List<BattlePresentationEvent>(stream.PendingCount);
            stream.DrainTo(events);
            _game.State.Phase = GamePhase.Ready;
            _game.State.Paused = false;
            _game.SetSpeed(speed);
            _presentation.Consume(events);
            _presentation.RoutePendingAudio(SilentCombatAudioRouter.Instance);
            _presentation.Advance(advanceSeconds, false, speed);
            _acceptanceFeedbackState = stateName;
            _acceptanceFeedbackSurface = surface;
            _acceptanceFeedbackPhase = phase;
            _acceptanceFeedbackFrozen = true;
            return true;
        }

        private Vector2[] AcceptanceGrassAnchors()
        {
            var anchors = new Vector2[6];
            for (var index = 0; index < anchors.Length; index++)
            {
                var cell = GetAcceptanceCell(index);
                AddAcceptancePot(cell);
                anchors[index] = _game.Map.CellToMap(cell);
            }
            return anchors;
        }

        private Vector2[] AcceptanceRouteAnchors()
        {
            return new[]
            {
                AcceptanceRoutePoint(.22f), AcceptanceRoutePoint(.32f),
                AcceptanceRoutePoint(.42f), AcceptanceRoutePoint(.52f),
                AcceptanceRoutePoint(.62f), AcceptanceRoutePoint(.72f),
            };
        }

        private Vector2 AcceptanceRoutePoint(float normalizedProgress)
        {
            return _game.Map.SampleRoute(_game.Map.PrimaryRouteId,
                _game.Map.RouteLength(_game.Map.PrimaryRouteId)
                * Mathf.Clamp01(normalizedProgress));
        }

        private Zombie AddAcceptanceZombie(float normalizedProgress)
        {
            var zombie = new Zombie
            {
                Id = _game.State.NextId++,
                DefinitionId = BattleContentIds.Enemies.Normal,
                Hp = 100f,
                MaxHp = 100f,
                Speed = 0f,
                RouteId = _game.Map.PrimaryRouteId,
                PathProgress = _game.Map.RouteLength(_game.Map.PrimaryRouteId)
                    * Mathf.Clamp01(normalizedProgress),
                Reward = 1,
            };
            _game.State.Zombies.Add(zombie);
            return zombie;
        }

        private static float AcceptanceHeavyLifetimeSeconds(float progress)
        {
            var style = CombatFloatingTextStyleCatalog.CreateBundled().Resolve(
                CombatFloatingTextRole.HeavyDamage);
            return style.Duration * Mathf.Clamp01(progress);
        }

        private static void EmitAcceptanceRoleInventory(
            BattlePresentationEventStream stream, IReadOnlyList<Vector2> anchors,
            int routeTargetId)
        {
            stream.EmitDamageResolved(10, BattleContentIds.Abilities.PeaAttack,
                string.Empty, BattleContentIds.Plants.Pea,
                BattleContentIds.Enemies.Normal, 7001, routeTargetId,
                anchors[0], Vector2.left, 12f, false);
            stream.EmitDamageResolved(10,
                BattleContentIds.Abilities.WatermelonAttack, string.Empty,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Enemies.Normal, 7002, 0,
                anchors[1], Vector2.left, 42f, false);
            stream.EmitDamageResolved(10, string.Empty, string.Empty,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Enemies.Normal, 7003, 0,
                anchors[2], Vector2.left, 6f, false);
            stream.EmitResourceGranted(10,
                BattleContentIds.Abilities.SunflowerProduce,
                BattleContentIds.Resources.Sun, 7004, 0, anchors[3], 25f);
            stream.EmitStatusProcced(10, BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Statuses.IceFreeze, 7005, 0,
                anchors[4], Vector2.left, 1f);
            stream.EmitEntityDefeated(10,
                BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Enemies.Normal, 7006, 7106,
                anchors[5], Vector2.left, 1f);
        }

        private static void EmitAcceptanceHeavy(
            BattlePresentationEventStream stream, Vector2 point)
        {
            stream.EmitDamageResolved(20,
                BattleContentIds.Abilities.WatermelonAttack, string.Empty,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Enemies.Normal, 7001, 7101,
                point, Vector2.left, 48f, false);
        }

        private void EmitAcceptanceDense(BattlePresentationEventStream stream)
        {
            var anchors = AcceptanceRouteAnchors();
            for (var index = 0; index < 3; index++)
                stream.EmitDamageResolved(30,
                    BattleContentIds.Abilities.PeaAttack, string.Empty,
                    BattleContentIds.Plants.Pea,
                    BattleContentIds.Enemies.Normal, 7001, 7200 + index,
                    anchors[index], Vector2.left, 10f + index, false);
            for (var index = 0; index < 3; index++)
                stream.EmitDamageResolved(30,
                    BattleContentIds.Abilities.BananaAttack, string.Empty,
                    BattleContentIds.Plants.Banana,
                    BattleContentIds.Enemies.Normal, 7002, 7300 + index,
                    anchors[index + 3], Vector2.left, 14f + index, false);
            for (var index = 0; index < 2; index++)
                stream.EmitDamageResolved(30, string.Empty, string.Empty,
                    BattleContentIds.Statuses.ChiliBurn,
                    BattleContentIds.Enemies.Normal, 7003, 7400 + index,
                    AcceptanceRoutePoint(.37f + index * .12f), Vector2.left,
                    5f + index, false);

            stream.EmitDamageResolved(31,
                BattleContentIds.Abilities.WatermelonAttack, string.Empty,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Enemies.Armored, 7004, 7501,
                AcceptanceRoutePoint(.58f), Vector2.left, 52f, false);
            stream.EmitResourceGranted(31,
                BattleContentIds.Abilities.SunflowerProduce,
                BattleContentIds.Resources.Sun, 7005, 0,
                _game.Map.CellToMap(GetAcceptanceCell(3)), 25f);
            stream.EmitStatusProcced(31,
                BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Statuses.IceFreeze, 7006, 7502,
                AcceptanceRoutePoint(.47f), Vector2.left, 1f);
            stream.EmitEntityDefeated(31,
                BattleContentIds.Abilities.DurianAttack,
                BattleContentIds.Enemies.Normal, 7007, 7503,
                AcceptanceRoutePoint(.68f), Vector2.left, 1f);
        }

        private Vector2 EmitAcceptanceCluster(BattlePresentationEventStream stream)
        {
            var centroid = Vector2.zero;
            for (var index = 0; index < 3; index++)
            {
                var point = AcceptanceRoutePoint(.50f + index * .06f);
                centroid += point;
                stream.EmitEntityDefeated(40,
                    BattleContentIds.Abilities.DurianAttack,
                    BattleContentIds.Enemies.Normal, 7001, 7600 + index,
                    point, Vector2.left, 1f);
            }
            return centroid / 3f;
        }

        private CombatFeedbackAcceptanceGeometry CaptureAcceptanceGeometry()
        {
            var layout = BattleLayout;
            var header = layout.Header;
            var board = layout.Board;
            var potHit = layout.Battlefield.PotHitRect(GetAcceptanceCell(0));
            return new CombatFeedbackAcceptanceGeometry
            {
                headerX = header.x,
                headerY = header.y,
                headerWidth = header.width,
                headerHeight = header.height,
                boardX = board.x,
                boardY = board.y,
                boardWidth = board.width,
                boardHeight = board.height,
                potHitX = potHit.x,
                potHitY = potHit.y,
                potHitWidth = potHit.width,
                potHitHeight = potHit.height,
            };
        }

        private static bool AcceptanceGeometryEqual(
            CombatFeedbackAcceptanceGeometry first,
            CombatFeedbackAcceptanceGeometry second)
        {
            if (first == null || second == null) return false;
            return Mathf.Abs(first.headerX - second.headerX) <= .0001f
                && Mathf.Abs(first.headerY - second.headerY) <= .0001f
                && Mathf.Abs(first.headerWidth - second.headerWidth) <= .0001f
                && Mathf.Abs(first.headerHeight - second.headerHeight) <= .0001f
                && Mathf.Abs(first.boardX - second.boardX) <= .0001f
                && Mathf.Abs(first.boardY - second.boardY) <= .0001f
                && Mathf.Abs(first.boardWidth - second.boardWidth) <= .0001f
                && Mathf.Abs(first.boardHeight - second.boardHeight) <= .0001f
                && Mathf.Abs(first.potHitX - second.potHitX) <= .0001f
                && Mathf.Abs(first.potHitY - second.potHitY) <= .0001f
                && Mathf.Abs(first.potHitWidth - second.potHitWidth) <= .0001f
                && Mathf.Abs(first.potHitHeight - second.potHitHeight) <= .0001f;
        }

        private static bool AcceptanceFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void PublishCombatFeedbackAcceptanceTelemetry()
        {
            if (string.IsNullOrEmpty(_acceptanceFeedbackState)
                || _floatingTextOverlay == null) return;
            var profileActive = _floatingTextOverlay.AcceptanceProfileActive;
            var profileSupported = _floatingTextOverlay.AcceptanceProfileSupported;
            var profileCompleted = _floatingTextOverlay.AcceptanceProfileCompleted;
            if (string.Equals(_lastPublishedAcceptanceState,
                    _acceptanceFeedbackState, StringComparison.Ordinal)
                && _lastPublishedAcceptanceSpeed == _game.State.Speed
                && _lastPublishedAcceptanceProfileActive == profileActive
                && _lastPublishedAcceptanceProfileSupported == profileSupported
                && _lastPublishedAcceptanceProfileCompleted == profileCompleted)
                return;

            var records = new CombatFeedbackAcceptanceRecord[
                _presentation.Feedback.Count];
            var roles = new List<string>();
            var placementRecordsValid = _floatingTextOverlay.PlacementValid;
            var placementFailure = _floatingTextOverlay.PlacementFailure;
            for (var index = 0; index < _presentation.Feedback.Count; index++)
            {
                var feedback = _presentation.Feedback[index];
                var role = feedback.Role.ToString();
                if (!roles.Contains(role)) roles.Add(role);
                var style = _presentation.FloatingTextStyles.Resolve(feedback.Role);
                var motion = CombatFloatingTextStyleCatalog.Sample(
                    style, feedback.LifetimeProgress, feedback.DetachedProgress);
                Vector2 finalScreenCenter;
                Vector2 anchorScreen;
                float anchorScreenError;
                Rect finalScreenBounds;
                var hasPlacement = _floatingTextOverlay.TryGetScreenPlacement(
                    feedback.EventSequence, out finalScreenCenter,
                    out anchorScreen, out anchorScreenError,
                    out finalScreenBounds);
                if (!hasPlacement
                    || !AcceptanceFinite(finalScreenCenter.x)
                    || !AcceptanceFinite(finalScreenCenter.y)
                    || !AcceptanceFinite(anchorScreen.x)
                    || !AcceptanceFinite(anchorScreen.y)
                    || !AcceptanceFinite(anchorScreenError)
                    || !AcceptanceFinite(finalScreenBounds.x)
                    || !AcceptanceFinite(finalScreenBounds.y)
                    || !AcceptanceFinite(finalScreenBounds.width)
                    || !AcceptanceFinite(finalScreenBounds.height))
                {
                    placementRecordsValid = false;
                    if (string.IsNullOrEmpty(placementFailure))
                        placementFailure = "telemetry-screen-placement-invalid";
                }
                records[index] = new CombatFeedbackAcceptanceRecord
                {
                    sequence = feedback.EventSequence,
                    role = role,
                    semanticId = feedback.SemanticId,
                    count = feedback.Count,
                    eventX = feedback.EventPoint.x,
                    eventY = feedback.EventPoint.y,
                    anchorX = feedback.Point.x,
                    anchorY = feedback.Point.y,
                    lifetimeProgress = feedback.LifetimeProgress,
                    detachedProgress = feedback.DetachedProgress,
                    motionScale = motion.Scale,
                    motionOpacity = motion.Opacity,
                    followingTarget = feedback.IsFollowingTarget,
                    finalScreenCenterX = hasPlacement
                        ? finalScreenCenter.x : 0f,
                    finalScreenCenterY = hasPlacement
                        ? finalScreenCenter.y : 0f,
                    anchorScreenX = hasPlacement ? anchorScreen.x : 0f,
                    anchorScreenY = hasPlacement ? anchorScreen.y : 0f,
                    anchorScreenError = hasPlacement
                        ? anchorScreenError : 0f,
                    finalScreenBoundsX = hasPlacement
                        ? finalScreenBounds.x : 0f,
                    finalScreenBoundsY = hasPlacement
                        ? finalScreenBounds.y : 0f,
                    finalScreenBoundsWidth = hasPlacement
                        ? finalScreenBounds.width : 0f,
                    finalScreenBoundsHeight = hasPlacement
                        ? finalScreenBounds.height : 0f,
                };
            }
            var impactBeat = _presentation.ActiveImpactBeat;
            var offset = _presentation.BattlefieldOffset;
            var geometryAfter = CaptureAcceptanceGeometry();
            var centroidFeedback = _acceptanceHasExpectedCentroid
                    && _presentation.Feedback.Count > 0
                ? _presentation.Feedback[0]
                : null;
            if (profileCompleted
                && _acceptanceProfileSamplesCache.Length
                    != CombatFloatingTextSdfOverlay.AcceptanceActiveSampleCount)
                _acceptanceProfileSamplesCache =
                    _floatingTextOverlay.AcceptanceProfileSamplesMilliseconds.ToArray();
            var telemetry = new CombatFeedbackAcceptanceTelemetry
            {
                state = _acceptanceFeedbackState,
                surface = _acceptanceFeedbackSurface,
                phase = _acceptanceFeedbackPhase,
                battleSpeed = _game.State.Speed,
                activeRole = roles.Count == 0 ? "None" : roles[0],
                activeRoles = roles.ToArray(),
                activeBeat = impactBeat == null
                    ? CombatImpactBeatRole.None.ToString()
                    : impactBeat.Role.ToString(),
                beatProgress = impactBeat == null ? 0f : impactBeat.Progress,
                battlefieldOffsetX = offset.x,
                battlefieldOffsetY = offset.y,
                battlefieldFlash = _presentation.BattlefieldFlash,
                hasExpectedCentroid = _acceptanceHasExpectedCentroid,
                expectedCentroidX = _acceptanceExpectedCentroid.x,
                expectedCentroidY = _acceptanceExpectedCentroid.y,
                eventCentroidError = centroidFeedback == null
                    ? 0f
                    : Vector2.Distance(_acceptanceExpectedCentroid,
                        centroidFeedback.EventPoint),
                anchorCentroidError = centroidFeedback == null
                    ? 0f
                    : Vector2.Distance(_acceptanceExpectedCentroid,
                        centroidFeedback.Point),
                geometryBefore = _acceptanceGeometryBefore,
                geometryAfter = geometryAfter,
                authoritativeGeometryUnchanged = AcceptanceGeometryEqual(
                    _acceptanceGeometryBefore, geometryAfter),
                feedbackCount = _presentation.Feedback.Count,
                ordinaryFeedbackCount = _presentation.OrdinaryFeedbackCount,
                activePoolCount = _floatingTextOverlay.ActiveTextCount,
                poolCapacity = CombatFloatingTextSdfOverlay.PoolCapacity,
                allocatedFeedbackCount = _presentation.AllocatedFeedbackCount,
                pooledFeedbackCount = _presentation.PooledFeedbackCount,
                missingProfileCount = _presentation.MissingProfileCount,
                atlasPageCount = _floatingTextOverlay.Atlas == null ? 0 : 1,
                atlasFormat = _floatingTextOverlay.Atlas == null
                    ? string.Empty
                    : _floatingTextOverlay.Atlas.format.ToString(),
                sharedMaterialCount = CombatFloatingTextSdfOverlay.SharedMaterialCount,
                preparedAtlasDrawCount =
                    _floatingTextOverlay.PreparedAtlasDrawCount,
                placementValid = placementRecordsValid,
                placementFailure = placementFailure,
                renderMarker = "FruitDefense.CombatFloatingText.Render",
                performanceSampling = profileCompleted
                    ? "completed"
                    : profileActive
                        ? "collecting"
                        : profileSupported
                            ? "not-started"
                            : "unsupported",
                performanceScope =
                    CombatFloatingTextSdfOverlay.AcceptancePerformanceScope,
                profileAllocationMetric =
                    CombatFloatingTextSdfOverlay.AcceptanceAllocationMetric,
                profileSupported = profileSupported,
                profileActive = profileActive,
                profileCompleted = profileCompleted,
                profileWarmupCount = _floatingTextOverlay.AcceptanceProfileWarmupCount,
                profileWarmupRequired = CombatFloatingTextSdfOverlay.AcceptanceWarmupSampleCount,
                profileSampleCount = _floatingTextOverlay.AcceptanceProfileSampleCount,
                profileSampleRequired = CombatFloatingTextSdfOverlay.AcceptanceActiveSampleCount,
                profileSamplesMilliseconds = profileCompleted
                    ? _acceptanceProfileSamplesCache
                    : Array.Empty<float>(),
                profileP95Milliseconds =
                    _floatingTextOverlay.AcceptanceProfileP95Milliseconds,
                profileAllocatedBytes =
                    _floatingTextOverlay.AcceptanceProfileAllocatedBytes,
                profileAllocatedBytesPerSecond =
                    _floatingTextOverlay.AcceptanceProfileAllocatedBytesPerSecond,
                profileElapsedSeconds =
                    _floatingTextOverlay.AcceptanceProfileElapsedSeconds,
                profileP95Algorithm =
                    "nearest-rank ceil(0.95*N), N=600 after 120 warmup",
                profileFailure = _floatingTextOverlay.AcceptanceProfileFailure,
                feedback = records,
            };
            _combatFeedbackAcceptanceTelemetryJson = JsonUtility.ToJson(telemetry);
#if UNITY_WEBGL && !UNITY_EDITOR
            FruitDefensePublishCombatFeedbackTelemetry(
                _combatFeedbackAcceptanceTelemetryJson);
#endif
            _lastPublishedAcceptanceState = _acceptanceFeedbackState;
            _lastPublishedAcceptanceSpeed = _game.State.Speed;
            _lastPublishedAcceptanceProfileActive = profileActive;
            _lastPublishedAcceptanceProfileSupported = profileSupported;
            _lastPublishedAcceptanceProfileCompleted = profileCompleted;
        }

        private Vector2Int GetAcceptanceCell(int index)
        {
            var cells = _game.Map.PlantableCells;
            if (cells.Count == 0) throw new InvalidOperationException("Acceptance map has no plantable cells.");
            return cells[Mathf.Clamp(index, 0, cells.Count - 1)];
        }

        private void GetAcceptanceAdjacentCells(out Vector2Int first, out Vector2Int second)
        {
            var cells = _game.Map.PlantableCells;
            for (var firstIndex = 0; firstIndex < cells.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < cells.Count; secondIndex++)
                {
                    if (!_game.Map.Topology.AreCardinalNeighbors(cells[firstIndex], cells[secondIndex]))
                        continue;
                    first = cells[firstIndex];
                    second = cells[secondIndex];
                    return;
                }
            }

            first = GetAcceptanceCell(0);
            second = GetAcceptanceCell(Mathf.Min(1, cells.Count - 1));
        }

        private void AddAcceptancePot(Vector2Int cell, string plantDefinitionId = null)
        {
            var pot = new Pot { Id = _game.State.NextId++, Cell = cell, Active = true };
            _game.State.Pots.Add(pot);
            if (string.IsNullOrEmpty(plantDefinitionId)) return;
            _game.State.Plants.Add(new Plant
            {
                Id = _game.State.NextId++,
                DefinitionId = plantDefinitionId,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
            });
        }
#endif
    }
}
