using System;
using FruitDefense.App;
using UnityEngine;

namespace FruitDefense.Shell
{
    public static class ShellFlowValidation
    {
        public static void SmokeValidate()
        {
            Validate();
            Debug.Log("FRUIT_DEFENSE_SHELL_OK");
        }

        public static void Validate()
        {
            ShellLayoutValidation.ValidateReferenceGeometry();
            ValidateHitTesting();
            ValidateLobbyStartAndDuplicateGuard();
            ValidateSettlementDisplay();
            ValidateReturnAndRetry();
            ValidateInvalidResultRecovery();
        }

        private static void ValidateHitTesting()
        {
            var safeArea = new Rect(0f, 0f, 402f, 874f);
            var lobby = PortraitShellLayout.CreateLobby(402f, 874f, safeArea);
            Assert(PortraitShellLayout.HitTest(lobby, lobby.StartButton.center, false) == ShellHitTarget.Start,
                "Lobby hits Start through its drawing rectangle");
            Assert(PortraitShellLayout.HitTest(lobby, lobby.StartButton.center, true) == ShellHitTarget.None,
                "Lobby ignores Start while transitioning");
            Assert(PortraitShellLayout.HitTest(lobby, lobby.LevelCard.center, false) == ShellHitTarget.None
                && PortraitShellLayout.HitTest(lobby, lobby.GrowthCard.center, false) == ShellHitTarget.None
                && PortraitShellLayout.HitTest(lobby, lobby.SettingsCard.center, false) == ShellHitTarget.None,
                "reserved cards never produce actions");

            var settlement = PortraitShellLayout.CreateSettlement(402f, 874f, safeArea);
            Assert(PortraitShellLayout.HitTest(settlement, settlement.RetryButton.center, false) == ShellHitTarget.Retry,
                "Settlement hits Retry through its drawing rectangle");
            Assert(PortraitShellLayout.HitTest(settlement, settlement.ReturnButton.center, false) == ShellHitTarget.Return,
                "Settlement hits Return through its drawing rectangle");
            Assert(PortraitShellLayout.HitTest(settlement, settlement.ResultCard.center, false) == ShellHitTarget.None,
                "result display never produces an action");
        }

        private static void ValidateLobbyStartAndDuplicateGuard()
        {
            var firstContext = FakeShellFlowContext.AtLobby("builtin-test-v1");
            var first = CreatePresenter<LobbyPresenter>("LobbyValidationFirst");
            try
            {
                first.Initialize(firstContext);
                Assert(first.TryStart(), "idle Lobby accepts Start");
                Assert(firstContext.StartCount == 1, "Start issues exactly one flow command");
                Assert(firstContext.StartLevelId == LobbyPresenter.DefaultLevelId,
                    "Start uses orchard-01");
                Assert(Guid.TryParse(firstContext.StartSessionId, out _), "Start uses a GUID session ID");
                Assert(firstContext.StartSeed != 0, "Start uses a nonzero seed");
                Assert(firstContext.StartContentVersion == "builtin-test-v1",
                    "Start uses the current bundled content version");
                Assert(!first.TryStart() && firstContext.StartCount == 1,
                    "duplicate Start is ignored while navigation loads");

                var layout = PortraitShellLayout.CreateLobby(402f, 874f, new Rect(0f, 0f, 402f, 874f));
                Assert(!first.TryActivateAt(layout.LevelCard.center, 402f, 874f, new Rect(0f, 0f, 402f, 874f))
                    && firstContext.StartCount == 1,
                    "reserved-card pointer input does not start battle");
            }
            finally
            {
                DestroyPresenter(first);
            }

            var secondContext = FakeShellFlowContext.AtLobby("builtin-test-v1");
            var second = CreatePresenter<LobbyPresenter>("LobbyValidationSecond");
            try
            {
                second.Initialize(secondContext);
                Assert(second.TryStart(), "a later clean Lobby can start");
                Assert(secondContext.StartSessionId != firstContext.StartSessionId,
                    "separate starts create new session identities");
            }
            finally
            {
                DestroyPresenter(second);
            }
        }

        private static void ValidateSettlementDisplay()
        {
            var context = FakeShellFlowContext.AtSettlement(new SettlementViewData(true, 12, 3));
            var presenter = CreatePresenter<SettlementPresenter>("SettlementDisplayValidation");
            try
            {
                presenter.Initialize(context);
                Assert(presenter.HasViewData, "valid Settlement binds view data");
                Assert(presenter.ViewData.Victory
                    && presenter.ViewData.ReachedWave == 12
                    && presenter.ViewData.RemainingLives == 3,
                    "Settlement displays outcome, reached wave, and remaining lives exactly");
            }
            finally
            {
                DestroyPresenter(presenter);
            }
        }

        private static void ValidateReturnAndRetry()
        {
            var returnContext = FakeShellFlowContext.AtSettlement(new SettlementViewData(false, 7, 0));
            var returnPresenter = CreatePresenter<SettlementPresenter>("SettlementReturnValidation");
            try
            {
                returnPresenter.Initialize(returnContext);
                Assert(returnPresenter.TryReturn(), "Return command is accepted");
                Assert(returnContext.ReturnCount == 1 && returnContext.ClearedBeforeReturn,
                    "Return clears completed session/result before navigation");
                Assert(returnContext.Navigator.HasPendingRoute
                    && returnContext.Navigator.PendingRoute == AppRoute.Lobby,
                    "Return requests Lobby");
            }
            finally
            {
                DestroyPresenter(returnPresenter);
            }

            var retryContext = FakeShellFlowContext.AtSettlement(new SettlementViewData(false, 9, 0));
            var completedSessionId = retryContext.CompletedSessionId;
            var completedSeed = retryContext.CompletedSeed;
            var retryPresenter = CreatePresenter<SettlementPresenter>("SettlementRetryValidation");
            try
            {
                retryPresenter.Initialize(retryContext);
                Assert(retryPresenter.TryRetry(), "Retry command is accepted");
                Assert(retryContext.RetryCount == 1, "Retry issues exactly one flow command");
                Assert(retryContext.RetrySessionId != completedSessionId
                    && Guid.TryParse(retryContext.RetrySessionId, out _),
                    "Retry creates a new session identity");
                Assert(retryContext.RetrySeed != 0 && retryContext.RetrySeed != completedSeed,
                    "Retry creates a new nonzero seed");
                Assert(retryContext.RetryLevelId == "orchard-01"
                    && retryContext.RetryContentVersion == "builtin-test-v1",
                    "Retry retains level and content version");
                Assert(!retryPresenter.TryRetry() && retryContext.RetryCount == 1,
                    "duplicate Retry is ignored while navigation loads");
            }
            finally
            {
                DestroyPresenter(retryPresenter);
            }
        }

        private static void ValidateInvalidResultRecovery()
        {
            ValidateRecovery(false, SettlementPresenter.MissingResult);
            ValidateRecovery(true, "settlement-result-session-mismatch");
        }

        private static void ValidateRecovery(bool mismatch, string expectedErrorCode)
        {
            var context = FakeShellFlowContext.AtSettlement(new SettlementViewData(true, 1, 1));
            context.HasSettlementResult = false;
            context.ResultMismatch = mismatch;
            var presenter = CreatePresenter<SettlementPresenter>(
                mismatch ? "SettlementMismatchValidation" : "SettlementMissingValidation");
            try
            {
                presenter.Initialize(context);
                Assert(!presenter.HasViewData, "invalid Settlement does not bind fabricated view data");
                Assert(context.ReportedError.Code == expectedErrorCode,
                    "invalid Settlement reports a structured recoverable error");
                Assert(context.ReturnCount == 1
                    && context.Navigator.HasPendingRoute
                    && context.Navigator.PendingRoute == AppRoute.Lobby,
                    "invalid Settlement safely requests Lobby once");
            }
            finally
            {
                DestroyPresenter(presenter);
            }
        }

        private static T CreatePresenter<T>(string name) where T : MonoBehaviour
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(false);
            return gameObject.AddComponent<T>();
        }

        private static void DestroyPresenter(MonoBehaviour presenter)
        {
            if (presenter != null) UnityEngine.Object.DestroyImmediate(presenter.gameObject);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Shell flow validation failed: " + message);
        }

        private sealed class FakeShellFlowContext : IShellFlowContext
        {
            private readonly AppNavigator _navigator;
            private SettlementViewData _settlementViewData;

            private FakeShellFlowContext(AppNavigator navigator, string bundledContentVersion)
            {
                _navigator = navigator;
                BundledContentVersion = bundledContentVersion;
                CompletedSessionId = Guid.NewGuid().ToString("N");
                CompletedSeed = 301;
                CompletedLevelId = "orchard-01";
                CompletedContentVersion = bundledContentVersion;
            }

            public IAppNavigator Navigator => _navigator;
            public string BundledContentVersion { get; }
            public bool HasSettlementResult { get; set; }
            public bool ResultMismatch { get; set; }
            public string CompletedSessionId { get; private set; }
            public int CompletedSeed { get; private set; }
            public string CompletedLevelId { get; private set; }
            public string CompletedContentVersion { get; private set; }
            public int StartCount { get; private set; }
            public string StartLevelId { get; private set; }
            public string StartSessionId { get; private set; }
            public int StartSeed { get; private set; }
            public string StartContentVersion { get; private set; }
            public int ReturnCount { get; private set; }
            public bool ClearedBeforeReturn { get; private set; }
            public int RetryCount { get; private set; }
            public string RetryLevelId { get; private set; }
            public string RetrySessionId { get; private set; }
            public int RetrySeed { get; private set; }
            public string RetryContentVersion { get; private set; }
            public ShellFlowError ReportedError { get; private set; }

            public static FakeShellFlowContext AtLobby(string bundledContentVersion)
            {
                return new FakeShellFlowContext(new AppNavigator(), bundledContentVersion);
            }

            public static FakeShellFlowContext AtSettlement(SettlementViewData viewData)
            {
                var navigator = new AppNavigator();
                Transition(navigator, AppRoute.Battle);
                Transition(navigator, AppRoute.Settlement);
                return new FakeShellFlowContext(navigator, "builtin-test-v1")
                {
                    _settlementViewData = viewData,
                    HasSettlementResult = true,
                };
            }

            public bool TryStartDefaultBattle(
                string levelId,
                string sessionId,
                int seed,
                string contentVersion,
                out ShellFlowError error)
            {
                if (!_navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }

                StartCount++;
                StartLevelId = levelId;
                StartSessionId = sessionId;
                StartSeed = seed;
                StartContentVersion = contentVersion;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryGetSettlementViewData(out SettlementViewData viewData, out ShellFlowError error)
            {
                if (ResultMismatch)
                {
                    viewData = default;
                    error = new ShellFlowError("settlement-result-session-mismatch");
                    return false;
                }

                if (!HasSettlementResult)
                {
                    viewData = default;
                    error = new ShellFlowError(SettlementPresenter.MissingResult);
                    return false;
                }

                viewData = _settlementViewData;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryReturnToLobby(out ShellFlowError error)
            {
                ReturnCount++;
                HasSettlementResult = false;
                CompletedSessionId = string.Empty;
                ClearedBeforeReturn = !HasSettlementResult && string.IsNullOrEmpty(CompletedSessionId);
                if (!_navigator.TryBeginTransition(AppRoute.Lobby, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }

                error = ShellFlowError.None;
                return true;
            }

            public bool TryRetryBattle(out ShellFlowError error)
            {
                var previousSession = CompletedSessionId;
                var previousSeed = CompletedSeed;
                var sessionId = Guid.NewGuid().ToString("N");
                var seed = LobbyPresenter.CreateNonzeroSeed();
                if (seed == previousSeed) seed = seed == int.MaxValue ? 1 : seed + 1;
                if (!_navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }

                RetryCount++;
                RetryLevelId = CompletedLevelId;
                RetryContentVersion = CompletedContentVersion;
                RetrySessionId = sessionId == previousSession ? Guid.NewGuid().ToString("N") : sessionId;
                RetrySeed = seed;
                HasSettlementResult = false;
                error = ShellFlowError.None;
                return true;
            }

            public void ReportRecoverableError(ShellFlowError error)
            {
                ReportedError = error;
            }

            private static void Transition(AppNavigator navigator, AppRoute route)
            {
                if (!navigator.TryBeginTransition(route, out var error)
                    || !navigator.TryCompleteTransition(out error))
                    throw new InvalidOperationException("Fake navigation setup failed: " + error);
            }
        }
    }
}
