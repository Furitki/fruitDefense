using System;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Core;
using FruitDefense.Platform;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleSessionHostSmoke
    {
        [MenuItem("Fruit Defense/Validate Battle Session Host")]
        public static void Run()
        {
            ValidateInitializationAndLifecycle();
            ValidateDefeatResult();
            Debug.Log("Fruit Defense battle session host validation passed.");
        }

        private static void ValidateInitializationAndLifecycle()
        {
            var baselineHostCount = FruitDefenseGame.ActiveSessionHostCount;
            var gameObject = new GameObject("BattleSessionHostSmoke");
            var host = gameObject.AddComponent<FruitDefenseGame>();
            var navigator = new TestNavigator(AppRoute.Battle);
            var sink = new RecordingResultSink();

            try
            {
                AssertFailure(
                    host.Initialize(null, navigator, sink),
                    BattleSessionInitializationResult.InvalidRequest,
                    "null launch request is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("", "orchard-01", 11, "builtin"), navigator, sink),
                    BattleSessionInitializationResult.InvalidSessionId,
                    "missing session id is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-a", "", 11, "builtin"), navigator, sink),
                    BattleSessionInitializationResult.InvalidLevelId,
                    "missing level id is rejected");
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-a", "orchard-01", 11, ""), navigator, sink),
                    BattleSessionInitializationResult.InvalidContentVersion,
                    "missing content version is rejected");

                var request = new BattleLaunchRequest("session-a", "orchard-01", 24680, "builtin-v1");
                AssertFailure(
                    host.Initialize(request, null, sink),
                    BattleSessionInitializationResult.NavigatorRequired,
                    "missing navigator is rejected");
                AssertFailure(
                    host.Initialize(request, navigator, null),
                    BattleSessionInitializationResult.ResultSinkRequired,
                    "missing result sink is rejected");

                var initialized = host.Initialize(request, navigator, sink);
                Assert(initialized.Success && host.IsInitialized, "valid request initializes the host");
                Assert(ReferenceEquals(host.CurrentRequest, request), "host retains the immutable request instance");
                Assert(host.Simulation != null && host.Simulation.State.RandomSeed == request.Seed,
                    "host constructs the simulation from the request seed");
                Assert(FruitDefenseGame.ActiveSessionHostCount == baselineHostCount + 1,
                    "initialized host is tracked exactly once");
                Assert(navigator.RouteSubscriptionCount == 1, "host subscribes to route changes exactly once");

                var originalSimulation = host.Simulation;
                AssertFailure(
                    host.Initialize(new BattleLaunchRequest("session-b", "orchard-01", 999, "builtin-v1"), navigator, sink),
                    BattleSessionInitializationResult.AlreadyInitialized,
                    "repeated initialization is rejected");
                Assert(ReferenceEquals(originalSimulation, host.Simulation)
                    && host.Simulation.State.RandomSeed == request.Seed,
                    "repeated initialization does not replace or reset the simulation");

                host.Simulation.AdvanceFrame(.04f);
                Assert(host.Simulation.FrameAccumulatorSeconds > 0d,
                    "host scenario has pending frame time before backgrounding");
                host.HandlePlatformVisibility(PlatformVisibility.Background);
                Assert(host.Simulation.State.Paused, "background pauses the active battle");
                Assert(Math.Abs(host.Simulation.FrameAccumulatorSeconds) < .0000001,
                    "background clears the fixed-step accumulator");
                host.HandlePlatformVisibility(PlatformVisibility.Foreground);
                Assert(host.Simulation.State.Paused, "foreground does not resume the battle");

                host.Simulation.State.WaveIndex = 7;
                host.Simulation.State.Sun = 333;
                host.Simulation.State.Lives = 2;
                host.Simulation.State.Zombies.Add(new Zombie { Id = 42, Hp = 1f, MaxHp = 1f });
                Assert(host.RestartCurrentSession(out var restartError) && string.IsNullOrEmpty(restartError),
                    "pause-menu local restart succeeds before settlement");
                Assert(host.Simulation.State.Phase == GamePhase.Ready
                    && !host.Simulation.State.Paused
                    && host.Simulation.State.WaveIndex == 0
                    && host.Simulation.State.Lives == 10
                    && host.Simulation.State.Zombies.Count == 0
                    && host.Simulation.State.RandomSeed == request.Seed,
                    "local restart creates a clean Ready state from the same request seed");
                Assert(sink.SubmissionCount == 0 && !host.HasSubmittedResult,
                    "local restart does not submit a settlement result");

                host.Simulation.State.Phase = GamePhase.Victory;
                host.Simulation.State.WaveIndex = 15;
                host.Simulation.State.Lives = 4;
                Assert(host.TrySubmitTerminalResult(), "first terminal frame submits a result");
                Assert(!host.TrySubmitTerminalResult() && sink.SubmissionCount == 1,
                    "repeated terminal frames cannot submit a second result");
                Assert(host.HasSubmittedResult
                    && sink.LastResult.SessionId == request.SessionId
                    && sink.LastResult.LevelId == request.LevelId
                    && sink.LastResult.Seed == request.Seed
                    && sink.LastResult.Outcome == BattleOutcome.Victory
                    && sink.LastResult.ReachedWave == 15
                    && sink.LastResult.RemainingLives == 4,
                    "submitted victory result is immutable session data");
                Assert(!host.RestartCurrentSession(out restartError)
                    && restartError == FruitDefenseGame.ResultAlreadySubmitted,
                    "a settled session cannot be locally restarted into another result");
            }
            finally
            {
                host.DisposeSession();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            Assert(FruitDefenseGame.ActiveSessionHostCount == baselineHostCount,
                "destroying the scene host releases active session state");
            Assert(navigator.RouteSubscriptionCount == 0,
                "destroying the scene host releases navigation callbacks");
        }

        private static void ValidateDefeatResult()
        {
            var gameObject = new GameObject("BattleSessionDefeatSmoke");
            var host = gameObject.AddComponent<FruitDefenseGame>();
            var navigator = new TestNavigator(AppRoute.Battle);
            var sink = new RecordingResultSink();
            try
            {
                var request = new BattleLaunchRequest("session-defeat", "orchard-01", 13579, "builtin-v1");
                Assert(host.Initialize(request, navigator, sink).Success, "defeat host initializes");
                host.Simulation.State.Phase = GamePhase.Defeat;
                host.Simulation.State.WaveIndex = 6;
                host.Simulation.State.Lives = 0;
                Assert(host.TrySubmitTerminalResult()
                    && sink.SubmissionCount == 1
                    && sink.LastResult.Outcome == BattleOutcome.Defeat
                    && sink.LastResult.ReachedWave == 6
                    && sink.LastResult.RemainingLives == 0,
                    "defeat submits the expected single result");
            }
            finally
            {
                host.DisposeSession();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

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
