using System;
using System.Linq;
using System.Reflection;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Platform;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleSessionHostSmoke
    {
#if FRUIT_DEFENSE_ACCEPTANCE
        [Serializable]
        private sealed class CombatFeedbackAcceptanceRecord
        {
            public int count;
            public string role;
            public string semanticId;
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
            public int schemaVersion;
            public string state;
            public string surface;
            public string phase;
            public int battleSpeed;
            public string[] activeRoles;
            public string activeBeat;
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
            public int atlasPageCount;
            public string atlasFormat;
            public int sharedMaterialCount;
            public int preparedAtlasDrawCount;
            public bool placementValid;
            public string placementFailure;
            public int missingProfileCount;
            public string performanceScope;
            public string profileAllocationMetric;
            public CombatFeedbackAcceptanceRecord[] feedback;
        }
#endif

        public static void Run()
        {
            var runtimeUiTheme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            var resolution = catalog.Resolve(
                BundledLevelCatalogIds.Levels.Orchard01);
            Assert(resolution.Succeeded && resolution.Value != null,
                "bundled host smoke level resolves");
            ValidateHostContract();
            ValidateResultContract();
            ValidateInitializationAndLifecycle(
                runtimeUiTheme, catalog, resolution.Value);
            ValidateDefeatResult(runtimeUiTheme, catalog, resolution.Value);
#if FRUIT_DEFENSE_ACCEPTANCE
            ValidateAcceptancePort(runtimeUiTheme, catalog, resolution.Value);
#endif
            Debug.Log("Fruit Defense battle session host validation passed.");
        }

        private static void ValidateHostContract()
        {
            var contract = typeof(IBattleSessionHost);
            var properties = contract.GetProperties(BindingFlags.Instance
                | BindingFlags.Public);
            Assert(properties.Length == 1
                && properties[0].Name == nameof(IBattleSessionHost.Status)
                && properties[0].PropertyType == typeof(BattleSessionStatus),
                "production host exposes one immutable observation value");
            Assert(contract.GetProperty("Simulation") == null
                && contract.GetProperty("CurrentRequest") == null
                && contract.GetProperty("ActiveLevel") == null,
                "production host exposes no mutable aggregate or source object");
            Assert(typeof(FruitDefenseGame).GetProperty("Simulation",
                    BindingFlags.Instance | BindingFlags.Public) == null,
                "release presenter has no concrete mutable compatibility accessor");
            var initialize = contract.GetMethod(nameof(IBattleSessionHost.Initialize));
            var initializeParameters = initialize == null
                ? Array.Empty<ParameterInfo>()
                : initialize.GetParameters();
            Assert(initializeParameters.Length == 5
                && initializeParameters[4].ParameterType == typeof(CompiledLevelCatalog)
                && initializeParameters.All(parameter =>
                    parameter.ParameterType != typeof(ResolvedLevelDefinition)),
                "host initialization receives one compiled catalog authority and no detached resolved level");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert(typeof(FruitDefense.Development.GmStress.GmStressBattlePresenter)
                    .GetProperty("Simulation", BindingFlags.Instance
                        | BindingFlags.Public) == null
                && typeof(FruitDefense.Development.GmStress.GmStressBattlePresenter)
                    .GetProperty("Controller", BindingFlags.Instance
                        | BindingFlags.Public) == null,
                "GM presenter cannot leak its simulation directly or indirectly");
#endif

            var statusType = typeof(BattleSessionStatus);
            Assert(statusType.IsValueType
                && statusType.GetFields(BindingFlags.Instance
                    | BindingFlags.Public).Length == 0
                && statusType.GetProperties(BindingFlags.Instance
                    | BindingFlags.Public).All(property => !property.CanWrite),
                "battle status is an immutable value with getter-only facts");
            var allowedTypes = new[]
            {
                typeof(bool), typeof(int), typeof(GamePhase),
            };
            Assert(statusType.GetProperties(BindingFlags.Instance
                    | BindingFlags.Public)
                    .All(property => allowedTypes.Contains(property.PropertyType)),
                "battle status cannot carry a reference or collection");
        }

        private static void ValidateResultContract()
        {
            var request = new BattleLaunchRequest("result-contract", "orchard-01", 31415,
                "1.0.0", BattleSessionMode.Standard);
            var valid = new BattleResult("result-contract", "orchard-01", 31415, BattleOutcome.Victory, 15, 3);
            Assert(valid.TryValidate(request, out var error) && string.IsNullOrEmpty(error),
                "matching result contract is accepted");
            Assert(!new BattleResult("result-contract", "orchard-01", 99, BattleOutcome.Victory, 15, 3)
                    .TryValidate(request, out error)
                && error == BattleResult.SeedMismatch,
                "result seed mismatch is rejected");
            Assert(!new BattleResult("result-contract", "orchard-01", 31415, BattleOutcome.Victory, -1, 3)
                    .TryValidate(request, out error)
                && error == BattleResult.InvalidReachedWave,
                "negative reached wave is rejected");
            Assert(!new BattleResult("result-contract", "orchard-01", 31415, BattleOutcome.Defeat, 2, -1)
                    .TryValidate(request, out error)
                && error == BattleResult.InvalidRemainingLives,
                "negative remaining lives are rejected");
        }

        private static void ValidateInitializationAndLifecycle(
            RuntimeUiTheme runtimeUiTheme, CompiledLevelCatalog catalog,
            ResolvedLevelDefinition resolvedLevel)
        {
            var baselineHostCount = FruitDefenseGame.ActiveSessionHostCount;
            var gameObject = new GameObject("BattleSessionHostSmoke");
            var host = gameObject.AddComponent<FruitDefenseGame>();
            var navigator = new TestNavigator(AppRoute.Battle);
            var sink = new RecordingResultSink();

            try
            {
                Assert(!host.Status.IsInitialized
                    && !host.Status.IsTerminal
                    && !host.Status.HasSubmittedResult,
                    "new host reports the canonical uninitialized status");
                Assert(!host.ExportCurrentSessionSnapshot().Succeeded,
                    "uninitialized host rejects snapshot export");
                AssertFailure(
                    host.Initialize(null, navigator, sink, runtimeUiTheme,
                        catalog),
                    BattleSessionInitializationResult.InvalidRequest,
                    "null launch request is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("", "orchard-01", 11, "builtin",
                            BattleSessionMode.Standard),
                        navigator, sink, runtimeUiTheme, catalog),
                    BattleSessionInitializationResult.InvalidSessionId,
                    "missing session id is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-a", "", 11, "builtin",
                            BattleSessionMode.Standard),
                        navigator, sink, runtimeUiTheme, catalog),
                    BattleSessionInitializationResult.InvalidLevelId,
                    "missing level id is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-a", "orchard-01", 11, "",
                            BattleSessionMode.Standard),
                        navigator, sink, runtimeUiTheme, catalog),
                    BattleSessionInitializationResult.InvalidContentVersion,
                    "missing content version is rejected");

                var request = new BattleLaunchRequest("session-a",
                    resolvedLevel.Identity.LevelId, 24680,
                    resolvedLevel.BattleContent.Header.contentVersion,
                    BattleSessionMode.Standard);
                AssertFailure(
                    host.Initialize(request, null, sink, runtimeUiTheme,
                        catalog),
                    BattleSessionInitializationResult.NavigatorRequired,
                    "missing navigator is rejected");
                AssertFailure(
                    host.Initialize(request, navigator, null, runtimeUiTheme,
                        catalog),
                    BattleSessionInitializationResult.ResultSinkRequired,
                    "missing result sink is rejected");

                AssertFailure(
                    host.Initialize(request, navigator, sink, null,
                        catalog),
                    BattleSessionInitializationResult.RuntimeUiThemeRequired,
                    "missing runtime UI theme is rejected");
                AssertFailure(
                    host.Initialize(request, navigator, sink, runtimeUiTheme,
                        null),
                    BattleSessionInitializationResult.LevelCatalogRequired,
                    "missing level catalog is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-unresolved",
                            "level.missing", request.Seed, request.ContentVersion,
                            BattleSessionMode.Standard),
                        navigator, sink, runtimeUiTheme, catalog),
                    BattleSessionInitializationResult.LevelResolutionFailed,
                    "unknown level identity is rejected through the catalog boundary");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-content-mismatch",
                            request.LevelId, request.Seed, "content.missing",
                            BattleSessionMode.Standard),
                        navigator, sink, runtimeUiTheme, catalog),
                    BattleSessionInitializationResult.ContentVersionMismatch,
                    "resolved content-version mismatch is rejected");

                var initialized = host.Initialize(request, navigator, sink,
                    runtimeUiTheme, catalog);
                Assert(initialized.Success
                    && host.Status.IsInitialized
                    && host.Status.Phase == GamePhase.Ready
                    && host.Status.WaveIndex == 0
                    && host.Status.Lives == 10
                    && !host.Status.IsPaused
                    && !host.Status.IsTerminal,
                    "valid request initializes an immutable Ready status");
                var initialExport = host.ExportCurrentSessionSnapshot();
                Assert(initialExport.Succeeded
                    && initialExport.Snapshot.randomSeed == request.Seed,
                    "bounded snapshot export retains the request seed");
                Assert(FruitDefenseGame.ActiveSessionHostCount == baselineHostCount + 1,
                    "initialized host is tracked exactly once");
                Assert(navigator.RouteSubscriptionCount == 1, "host subscribes to route changes exactly once");

                var beforeRepeatedInitialization = BattleSnapshotJson.Serialize(
                    initialExport.Snapshot);
                var beforeRepeatedStatus = host.Status;
                AssertFailure(
                    host.Initialize(
                        new BattleLaunchRequest("session-b",
                            resolvedLevel.Identity.LevelId, 999,
                            resolvedLevel.BattleContent.Header.contentVersion,
                            BattleSessionMode.Standard),
                        navigator, sink, runtimeUiTheme, catalog),
                    BattleSessionInitializationResult.AlreadyInitialized,
                    "repeated initialization is rejected");
                var afterRepeatedInitialization = host.ExportCurrentSessionSnapshot();
                Assert(afterRepeatedInitialization.Succeeded
                    && beforeRepeatedInitialization == BattleSnapshotJson.Serialize(
                        afterRepeatedInitialization.Snapshot)
                    && SameStatus(beforeRepeatedStatus, host.Status),
                    "repeated initialization cannot mutate authoritative state");

                host.HandlePlatformVisibility(PlatformVisibility.Background);
                Assert(host.Status.IsPaused,
                    "background pauses the active battle through a bounded command");
                host.HandlePlatformVisibility(PlatformVisibility.Foreground);
                Assert(host.Status.IsPaused, "foreground does not resume the battle");

                var external = BattleSnapshotSmoke.CreateScenario(catalog,
                    resolvedLevel.Identity.LevelId, request.Seed);
                external.State.Sun = 333;
                external.State.Lives = 2;
                var externalExport = external.ExportSnapshot();
                Assert(externalExport.Succeeded
                    && host.RestoreCurrentSessionSnapshot(
                        externalExport.Snapshot, catalog).Succeeded
                    && host.Status.Phase == GamePhase.Playing
                    && host.Status.WaveIndex == 1
                    && host.Status.Lives == 2,
                    "bounded restore replaces the session without exposing its aggregate");
                Assert(host.RestartCurrentSession(out var restartError) && string.IsNullOrEmpty(restartError),
                    "pause-menu local restart succeeds before settlement");
                var restarted = host.ExportCurrentSessionSnapshot();
                Assert(host.Status.Phase == GamePhase.Ready
                    && !host.Status.IsPaused
                    && host.Status.WaveIndex == 0
                    && host.Status.Lives == 10
                    && restarted.Succeeded
                    && restarted.Snapshot.enemies.Length == 0
                    && restarted.Snapshot.randomSeed == request.Seed,
                    "local restart creates a clean Ready state from the same request seed");
                Assert(sink.SubmissionCount == 0
                    && !host.Status.HasSubmittedResult,
                    "local restart does not submit a settlement result");

                var victory = CreateTerminalSnapshot(
                    catalog, request.LevelId, request.Seed,
                    BattleOutcome.Victory, 4);
                Assert(host.RestoreCurrentSessionSnapshot(victory, catalog).Succeeded
                    && host.Status.IsTerminal
                    && host.Status.Phase == GamePhase.Victory,
                    "terminal state enters only through bounded snapshot restore");
                Assert(host.TrySubmitTerminalResult(), "first terminal frame submits a result");
                Assert(!host.TrySubmitTerminalResult() && sink.SubmissionCount == 1,
                    "repeated terminal frames cannot submit a second result");
                Assert(host.Status.HasSubmittedResult
                    && sink.LastResult.SessionId == request.SessionId
                    && sink.LastResult.LevelId == request.LevelId
                    && sink.LastResult.Seed == request.Seed
                    && sink.LastResult.Outcome == BattleOutcome.Victory
                    && sink.LastResult.ReachedWave == resolvedLevel.OrderedWaves.Count
                    && sink.LastResult.RemainingLives == 4,
                    "submitted victory result is immutable session data");
                Assert(!host.RestartCurrentSession(out restartError)
                    && restartError == FruitDefenseGame.ResultAlreadySubmitted,
                    "a settled session cannot be locally restarted into another result");
            }
            finally
            {
                host.DisposeSession();
                Assert(!host.Status.IsInitialized
                    && !host.RestartCurrentSession(out var disposedError)
                    && disposedError == FruitDefenseGame.SessionNotInitialized
                    && !host.ExportCurrentSessionSnapshot().Succeeded,
                    "disposed host rejects bounded commands and exposes no stale status");
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            Assert(FruitDefenseGame.ActiveSessionHostCount == baselineHostCount,
                "destroying the scene host releases active session state");
            Assert(navigator.RouteSubscriptionCount == 0,
                "destroying the scene host releases navigation callbacks");
        }

        private static void ValidateDefeatResult(RuntimeUiTheme runtimeUiTheme,
            CompiledLevelCatalog catalog, ResolvedLevelDefinition resolvedLevel)
        {
            var gameObject = new GameObject("BattleSessionDefeatSmoke");
            var host = gameObject.AddComponent<FruitDefenseGame>();
            var navigator = new TestNavigator(AppRoute.Battle);
            var sink = new RecordingResultSink();
            try
            {
                var request = new BattleLaunchRequest("session-defeat",
                    resolvedLevel.Identity.LevelId, 13579,
                    resolvedLevel.BattleContent.Header.contentVersion,
                    BattleSessionMode.Standard);
                Assert(host.Initialize(request, navigator, sink,
                        runtimeUiTheme, catalog).Success,
                    "defeat host initializes");
                var defeat = CreateTerminalSnapshot(
                    catalog, request.LevelId, request.Seed,
                    BattleOutcome.Defeat, 0);
                Assert(host.RestoreCurrentSessionSnapshot(defeat, catalog).Succeeded
                    && host.Status.IsTerminal
                    && host.Status.Phase == GamePhase.Defeat,
                    "bounded restore applies a valid defeat state");
                Assert(host.TrySubmitTerminalResult()
                    && sink.SubmissionCount == 1
                    && sink.LastResult.Outcome == BattleOutcome.Defeat
                    && sink.LastResult.ReachedWave == 1
                    && sink.LastResult.RemainingLives == 0,
                    "defeat submits the expected single result");
            }
            finally
            {
                host.DisposeSession();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static BattleSnapshot CreateTerminalSnapshot(
            CompiledLevelCatalog catalog, string levelId, int seed,
            BattleOutcome outcome, int lives)
        {
            var resolution = catalog.Resolve(levelId);
            Assert(resolution.Succeeded && resolution.Value != null,
                "terminal snapshot fixture resolves through the supplied catalog");
            var resolvedLevel = resolution.Value;
            var simulation = new GameSimulation(catalog, levelId, seed);
            var victory = outcome == BattleOutcome.Victory;
            var waveIndex = victory ? resolvedLevel.OrderedWaves.Count : 1;
            var waveTotal = resolvedLevel.OrderedWaves[waveIndex - 1]
                .enemyIds.Length;
            simulation.State.Phase = victory
                ? GamePhase.Victory
                : GamePhase.Defeat;
            simulation.State.WaveIndex = waveIndex;
            simulation.State.WaveTotal = waveTotal;
            simulation.State.WaveSpawned = victory ? waveTotal : 0;
            simulation.State.Lives = lives;
            simulation.State.Paused = false;
            var export = simulation.ExportSnapshot();
            Assert(export.Succeeded,
                "external terminal fixture exports through the snapshot boundary");
            return export.Snapshot;
        }

        private static bool SameStatus(
            BattleSessionStatus first, BattleSessionStatus second)
        {
            return first.IsInitialized == second.IsInitialized
                && first.Phase == second.Phase
                && first.WaveIndex == second.WaveIndex
                && first.Lives == second.Lives
                && first.IsPaused == second.IsPaused
                && first.HasSubmittedResult == second.HasSubmittedResult
                && first.IsTerminal == second.IsTerminal;
        }

#if FRUIT_DEFENSE_ACCEPTANCE
        private static void ValidateAcceptancePort(RuntimeUiTheme runtimeUiTheme,
            CompiledLevelCatalog catalog, ResolvedLevelDefinition resolvedLevel)
        {
            var gameObject = new GameObject("BattleSessionAcceptancePortSmoke");
            var host = gameObject.AddComponent<FruitDefenseGame>();
            var navigator = new TestNavigator(AppRoute.Battle);
            var sink = new RecordingResultSink();
            try
            {
                var request = new BattleLaunchRequest(
                    "session-acceptance-port", resolvedLevel.Identity.LevelId,
                    86420, resolvedLevel.BattleContent.Header.contentVersion,
                    BattleSessionMode.Standard);
                Assert(host.Initialize(request, navigator, sink,
                        runtimeUiTheme, catalog).Success,
                    "acceptance-port host initializes");
                var port = (IAcceptanceBattlePort)host;

                var beforeUnknown = host.ExportCurrentSessionSnapshot();
                var beforeUnknownStatus = host.Status;
                var unknownNamed = port.TryConfigureNamedState(
                    "not-a-real-acceptance-state");
                var unknownTerminal = port.TryConfigureTerminalFixture(
                    (AcceptanceTerminalFixture)int.MaxValue);
                var afterUnknown = host.ExportCurrentSessionSnapshot();
                Assert(beforeUnknown.Succeeded && afterUnknown.Succeeded
                    && !unknownNamed.Succeeded
                    && unknownNamed.ErrorCode
                        == AcceptanceCommandResult.NamedStateUnknown
                    && !unknownTerminal.Succeeded
                    && unknownTerminal.ErrorCode
                        == AcceptanceCommandResult.TerminalFixtureUnknown
                    && BattleSnapshotJson.Serialize(beforeUnknown.Snapshot)
                        == BattleSnapshotJson.Serialize(afterUnknown.Snapshot)
                    && SameStatus(beforeUnknownStatus, host.Status)
                    && sink.SubmissionCount == 0,
                    "unknown acceptance commands fail without authoritative mutation");

                Assert(port.TryConfigureNamedState("terminal-victory").Succeeded
                    && host.Status.Phase == GamePhase.Victory
                    && host.Status.WaveIndex == resolvedLevel.OrderedWaves.Count
                    && host.Status.Lives == 3
                    && !host.TrySubmitTerminalResult()
                    && !host.Status.HasSubmittedResult
                    && sink.SubmissionCount == 0,
                    "acceptance victory preview suppresses only terminal submission");
                Assert(host.RestartCurrentSession(out var restartError)
                    && string.IsNullOrEmpty(restartError)
                    && host.Status.Phase == GamePhase.Ready,
                    "restart clears the acceptance terminal preview");

                Assert(port.TryConfigureNamedState("terminal-defeat").Succeeded
                    && host.Status.Phase == GamePhase.Defeat
                    && host.Status.Lives == 0
                    && !host.TrySubmitTerminalResult()
                    && sink.SubmissionCount == 0,
                    "acceptance defeat preview is stable and does not submit");

                Assert(port.TryConfigureNamedState("selected-tool").Succeeded,
                    "known named state is accepted through the dedicated port");
                var selectedTool = host.ExportCurrentSessionSnapshot();
                Assert(host.Status.Phase == GamePhase.Ready
                    && selectedTool.Succeeded
                    && selectedTool.Snapshot.equipment.Any(value =>
                        value.definitionId == BattleContentIds.Equipment.Gatling
                        && value.count == 1),
                    "selected-tool fixture exposes one real selectable Gatling");

                ValidateCombatFeedbackAcceptanceFixtures(port);

                Assert(port.TryConfigureNamedState("initial").Succeeded,
                    "known reset state clears the terminal preview");
                var terminal = port.TryConfigureTerminalFixture(
                    AcceptanceTerminalFixture.Victory);
                Assert(terminal.Succeeded
                    && host.Status.HasSubmittedResult
                    && host.Status.Phase == GamePhase.Victory
                    && sink.SubmissionCount == 1
                    && sink.LastResult.Outcome == BattleOutcome.Victory,
                    "terminal fixture mutates and submits only through the dedicated port");

                var beforeUnavailable = host.ExportCurrentSessionSnapshot();
                var unavailable = port.TryConfigureTerminalFixture(
                    AcceptanceTerminalFixture.Defeat);
                var afterUnavailable = host.ExportCurrentSessionSnapshot();
                Assert(!unavailable.Succeeded
                    && unavailable.ErrorCode == FruitDefenseGame.ResultAlreadySubmitted
                    && beforeUnavailable.Succeeded && afterUnavailable.Succeeded
                    && BattleSnapshotJson.Serialize(beforeUnavailable.Snapshot)
                        == BattleSnapshotJson.Serialize(afterUnavailable.Snapshot),
                    "unavailable terminal fixture fails before authoritative mutation");
            }
            finally
            {
                host.DisposeSession();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void ValidateCombatFeedbackAcceptanceFixtures(
            IAcceptanceBattlePort port)
        {
            var grass = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-role-grass", "grass", "role-inventory",
                1, "Heavy", 6);
            var route = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-role-route", "route", "role-inventory",
                1, "Heavy", 6);
            var requiredRoles = new[]
            {
                "NormalDamage", "HeavyDamage", "PeriodicDamage",
                "Resource", "Control", "Defeat",
            };
            Assert(requiredRoles.All(role => grass.activeRoles.Contains(role))
                && requiredRoles.All(role => route.activeRoles.Contains(role))
                && route.feedback.Any(value => value.followingTarget),
                "role fixtures cover the finite inventory on grass and a live route anchor");
            var roleSemantics = new[]
            {
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Resources.Sun,
                BattleContentIds.Statuses.IceFreeze,
                BattleContentIds.Enemies.Normal,
            };
            AssertSemanticMultiset(grass, roleSemantics, "grass role inventory");
            AssertSemanticMultiset(route, roleSemantics, "route role inventory");
            var followed = route.feedback.Single(value => value.followingTarget);
            var routeTargetAnchor = route.feedback.Single(value =>
                value.semanticId == BattleContentIds.Statuses.ChiliBurn);
            Assert(Vector2.Distance(
                    new Vector2(followed.anchorX, followed.anchorY),
                    new Vector2(routeTargetAnchor.eventX, routeTargetAnchor.eventY))
                    <= .0001f
                && Vector2.Distance(
                    new Vector2(followed.eventX, followed.eventY),
                    new Vector2(followed.anchorX, followed.anchorY)) > .01f,
                "route follow anchor resolves to the deterministic live target rather than only setting a flag");

            var entry = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-rebound-entry", "route", "entry",
                1, "Heavy", 1);
            var peak = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-rebound-peak", "route", "peak",
                1, "Heavy", 1);
            var rebound = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-rebound-return", "route", "rebound",
                1, "None", 1);
            var hold = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-rebound-hold", "route", "hold",
                1, "None", 1);
            Assert(entry.feedback[0].motionScale < peak.feedback[0].motionScale
                && rebound.feedback[0].motionScale < peak.feedback[0].motionScale
                && rebound.feedback[0].motionScale > hold.feedback[0].motionScale,
                "entry, peak, rebound, and hold expose distinct deterministic phases");
            foreach (var value in new[] { entry, peak, rebound, hold })
                AssertSemanticMultiset(value,
                    new[] { BattleContentIds.Abilities.WatermelonAttack },
                    "rebound phase");

            var denseOne = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-dense-1x", "route", "dense",
                1, "Heavy", 12);
            var denseTwo = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-dense-2x", "route", "dense",
                2, "Heavy", 12);
            Assert(denseOne.ordinaryFeedbackCount == 8
                && denseTwo.ordinaryFeedbackCount == 8,
                "1x and 2x dense fixtures reach the 8 ordinary / 12 total caps");
            var denseSemantics = new[]
            {
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.PeaAttack,
                BattleContentIds.Abilities.BananaAttack,
                BattleContentIds.Abilities.BananaAttack,
                BattleContentIds.Abilities.BananaAttack,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Statuses.ChiliBurn,
                BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Resources.Sun,
                BattleContentIds.Statuses.IceFreeze,
                BattleContentIds.Enemies.Normal,
            };
            AssertSemanticMultiset(denseOne, denseSemantics, "dense 1x");
            AssertSemanticMultiset(denseTwo, denseSemantics, "dense 2x");
            Assert(denseOne.feedback.Length == denseTwo.feedback.Length,
                "dense speed fixtures retain corresponding record counts");
            for (var index = 0; index < denseOne.feedback.Length; index++)
            {
                Assert(denseOne.feedback[index].semanticId
                        == denseTwo.feedback[index].semanticId
                    && denseOne.feedback[index].role
                        == denseTwo.feedback[index].role,
                    "dense speed fixtures retain corresponding semantic order");
                var ratio = denseTwo.feedback[index].lifetimeProgress
                    / denseOne.feedback[index].lifetimeProgress;
                Assert(ratio >= 1.20f && ratio <= 1.30f,
                    "every dense record uses the 1.25x 2x display-clock rate");
            }

            var heavy = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-beat-heavy", "route", "impact-beat",
                1, "Heavy", 1);
            var cluster = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-beat-cluster", "route", "impact-beat",
                1, "Cluster", 1);
            var terminal = ConfigureCombatFeedbackFixture(port,
                "combat-feedback-beat-terminal", "route", "impact-beat",
                1, "Terminal", 1);
            Assert(cluster.feedback[0].count == 3
                && !cluster.feedback[0].followingTarget
                && cluster.hasExpectedCentroid
                && cluster.eventCentroidError <= .0001f
                && cluster.anchorCentroidError <= .0001f
                && Vector2.Distance(
                    new Vector2(cluster.feedback[0].eventX,
                        cluster.feedback[0].eventY),
                    new Vector2(cluster.expectedCentroidX,
                        cluster.expectedCentroidY)) <= .0001f
                && Vector2.Distance(
                    new Vector2(cluster.feedback[0].anchorX,
                        cluster.feedback[0].anchorY),
                    new Vector2(cluster.expectedCentroidX,
                        cluster.expectedCentroidY)) <= .0001f,
                "Cluster uses one detached three-defeat record at the independently expected centroid");
            AssertSemanticMultiset(heavy,
                new[] { BattleContentIds.Abilities.WatermelonAttack },
                "Heavy beat");
            AssertSemanticMultiset(cluster,
                new[] { BattleContentIds.Enemies.Normal }, "Cluster beat");
            AssertSemanticMultiset(terminal,
                new[] { BattleContentIds.Enemies.Boss }, "Terminal beat");
        }

        private static CombatFeedbackAcceptanceTelemetry ConfigureCombatFeedbackFixture(
            IAcceptanceBattlePort port, string state,
            string surface, string phase, int speed, string beat, int count)
        {
            var result = port.TryConfigureNamedState(state);
            Assert(result.Succeeded,
                "known combat-feedback fixture is accepted: " + state);
            var telemetry = JsonUtility.FromJson<CombatFeedbackAcceptanceTelemetry>(
                port.CombatFeedbackAcceptanceTelemetryJson);
            Assert(telemetry != null
                && telemetry.schemaVersion == 1
                && telemetry.state == state
                && telemetry.surface == surface
                && telemetry.phase == phase
                && telemetry.battleSpeed == speed
                && telemetry.activeBeat == beat
                && telemetry.feedbackCount == count
                && telemetry.activePoolCount == count
                && telemetry.poolCapacity == 12
                && telemetry.atlasPageCount == 1
                && telemetry.atlasFormat == "RGBA32"
                && telemetry.sharedMaterialCount == 0
                && telemetry.preparedAtlasDrawCount >= telemetry.feedbackCount
                && telemetry.preparedAtlasDrawCount
                    <= CombatFloatingTextSdfOverlay.DrawCommandCapacity
                && telemetry.placementValid
                && string.IsNullOrEmpty(telemetry.placementFailure)
                && telemetry.missingProfileCount == 0
                && telemetry.performanceScope
                    == CombatFloatingTextSdfOverlay.AcceptancePerformanceScope
                && telemetry.profileAllocationMetric
                    == CombatFloatingTextSdfOverlay.AcceptanceAllocationMetric
                && telemetry.feedback != null
                && telemetry.feedback.Length == count
                && Finite(telemetry.beatProgress)
                && Finite(telemetry.battlefieldOffsetX)
                && Finite(telemetry.battlefieldOffsetY)
                && Finite(telemetry.battlefieldFlash)
                && Finite(telemetry.expectedCentroidX)
                && Finite(telemetry.expectedCentroidY)
                && Finite(telemetry.eventCentroidError)
                && Finite(telemetry.anchorCentroidError)
                && telemetry.feedback.All(value => value != null
                    && Finite(value.eventX) && Finite(value.eventY)
                    && Finite(value.anchorX) && Finite(value.anchorY)
                    && Finite(value.lifetimeProgress)
                    && Finite(value.detachedProgress)
                    && Finite(value.motionScale)
                    && Finite(value.motionOpacity)
                    && Finite(value.finalScreenCenterX)
                    && Finite(value.finalScreenCenterY)
                    && Finite(value.anchorScreenX)
                    && Finite(value.anchorScreenY)
                    && Finite(value.anchorScreenError)
                    && value.anchorScreenError >= 0f
                    && Finite(value.finalScreenBoundsX)
                    && Finite(value.finalScreenBoundsY)
                    && Finite(value.finalScreenBoundsWidth)
                    && value.finalScreenBoundsWidth > 0f
                    && Finite(value.finalScreenBoundsHeight)
                    && value.finalScreenBoundsHeight > 0f)
                && telemetry.authoritativeGeometryUnchanged
                && GeometryEqual(telemetry.geometryBefore, telemetry.geometryAfter),
                "combat-feedback fixture exports exact read-only telemetry: " + state);
            var beatOffset = new Vector2(
                telemetry.battlefieldOffsetX, telemetry.battlefieldOffsetY).magnitude;
            Assert(beat == "None"
                    ? beatOffset <= .0001f
                    : beatOffset > .0001f
                        && beatOffset <= CombatImpactBeatCatalog.MaximumAmplitude
                            + .0001f,
                "active beat exports a non-zero bounded offset: " + state);
            return telemetry;
        }

        private static void AssertSemanticMultiset(
            CombatFeedbackAcceptanceTelemetry telemetry,
            string[] expected, string label)
        {
            var actual = telemetry.feedback.Select(value => value.semanticId)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var orderedExpected = expected.OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
            Assert(actual.SequenceEqual(orderedExpected),
                label + " exports the exact semantic ID multiset");
        }

        private static bool GeometryEqual(
            CombatFeedbackAcceptanceGeometry first,
            CombatFeedbackAcceptanceGeometry second)
        {
            return first != null && second != null
                && Mathf.Abs(first.headerX - second.headerX) <= .0001f
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

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
#endif

        private static void AssertFailure(
            BattleSessionInitializationResult result,
            string expectedError,
            string message)
        {
            Assert(!result.Success && result.ErrorCode == expectedError, message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Battle session host validation failed: " + message);
        }

        private sealed class RecordingResultSink : IBattleResultSink
        {
            public int SubmissionCount { get; private set; }
            public BattleResult LastResult { get; private set; }

            public bool TrySubmitResult(BattleResult result, out string errorCode)
            {
                SubmissionCount++;
                LastResult = result;
                errorCode = string.Empty;
                return true;
            }
        }

        private sealed class TestNavigator : IAppNavigator
        {
            private Action<AppRoute> _routeChanged;

            public TestNavigator(AppRoute route)
            {
                CurrentRoute = route;
                PendingRoute = route;
            }

            public AppRoute CurrentRoute { get; private set; }
            public AppRoute PendingRoute { get; private set; }
            public bool HasPendingRoute { get; private set; }
            public AppTransitionState TransitionState { get; private set; }
            public string LastError { get; private set; } = string.Empty;
            public int RouteSubscriptionCount { get; private set; }

            public event Action<AppRoute> RouteChanged
            {
                add
                {
                    _routeChanged += value;
                    RouteSubscriptionCount++;
                }
                remove
                {
                    _routeChanged -= value;
                    RouteSubscriptionCount--;
                }
            }

            public event Action<AppTransitionState> TransitionStateChanged;

            public bool TryBeginTransition(AppRoute destination, out string errorCode)
            {
                PendingRoute = destination;
                HasPendingRoute = true;
                TransitionState = AppTransitionState.Loading;
                TransitionStateChanged?.Invoke(TransitionState);
                errorCode = string.Empty;
                return true;
            }

            public bool TryCompleteTransition(out string errorCode)
            {
                CurrentRoute = PendingRoute;
                HasPendingRoute = false;
                TransitionState = AppTransitionState.Idle;
                _routeChanged?.Invoke(CurrentRoute);
                TransitionStateChanged?.Invoke(TransitionState);
                errorCode = string.Empty;
                return true;
            }

            public bool TryFailTransition(string transitionErrorCode, out string errorCode)
            {
                HasPendingRoute = false;
                LastError = transitionErrorCode;
                TransitionState = AppTransitionState.Failed;
                TransitionStateChanged?.Invoke(TransitionState);
                errorCode = string.Empty;
                return true;
            }
        }
    }
}
