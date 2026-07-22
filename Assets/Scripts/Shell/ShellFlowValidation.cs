using System;
using System.Collections.Generic;
using FruitDefense.App;
using FruitDefense.Content;
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
            ValidateThreeCardSelectionAndSelectedStart();
            ValidateUnavailableProfileRecoveryAndLegacyCompatibility();
            ValidateSettlementDisplay();
            ValidateReturnAndRetry();
            ValidateInvalidResultRecovery();
        }

        private static void ValidateThreeCardSelectionAndSelectedStart()
        {
            var context = FakeShellFlowContext.AtLobby("builtin-test-v1",
                LobbyPresenter.Orchard01LevelId);
            var presenter = CreatePresenter<LobbyPresenter>("LobbyMultiLevelValidation");
            try
            {
                presenter.Initialize(context);
                Assert(presenter.SelectedLevelId == LobbyPresenter.Orchard01LevelId,
                    "Lobby visibly restores the context selection");

                var safeArea = new Rect(0f, 0f, 402f, 874f);
                var layout = PortraitShellLayout.CreateLobby(402f, 874f, safeArea);
                Assert(presenter.TryActivateAt(layout.Orchard02Card.center, 402f, 874f, safeArea),
                    "orchard-02 drawn card accepts input");
                Assert(context.SelectionCount == 1
                    && context.SelectedLevelId == LobbyPresenter.Orchard02LevelId
                    && presenter.SelectedLevelId == LobbyPresenter.Orchard02LevelId,
                    "card selection updates both persisted context and visible selection");
                Assert(context.StartCount == 0
                    && context.Navigator.TransitionState == AppTransitionState.Idle,
                    "selecting a card does not navigate");

                Assert(presenter.TryActivateAt(layout.StartButton.center, 402f, 874f, safeArea),
                    "Start drawn rectangle accepts input");
                Assert(context.StartCount == 1
                    && context.StartLevelId == LobbyPresenter.Orchard02LevelId,
                    "Start submits only the visibly selected orchard-02 ID");
                Assert(Guid.TryParse(context.StartSessionId, out _)
                    && context.StartSeed != 0
                    && context.StartContentVersion == "builtin-test-v1",
                    "Start creates a valid session identity, seed, and content identity");
                Assert(!presenter.TryStart() && context.StartCount == 1,
                    "duplicate Start is ignored while navigation loads");
                Assert(!presenter.TrySelectLevel(LobbyPresenter.Orchard03LevelId)
                    && context.SelectionCount == 1,
                    "selection is also guarded during transition");
            }
            finally
            {
                DestroyPresenter(presenter);
            }

            var strictContext = FakeShellFlowContext.AtLobby("builtin-test-v1",
                LobbyPresenter.Orchard03LevelId);
            var strictPresenter = CreatePresenter<LobbyPresenter>("LobbyStrictSelectionValidation");
            try
            {
                strictPresenter.Initialize(strictContext);
                Assert(!strictPresenter.TrySelectLevel("orchard-missing")
                    && strictPresenter.SelectedLevelId == LobbyPresenter.Orchard03LevelId,
                    "unknown selection is rejected without changing or defaulting the visible level");
                Assert(strictPresenter.TryStart()
                    && strictContext.StartLevelId == LobbyPresenter.Orchard03LevelId,
                    "a rejected selection cannot silently launch orchard-01");
            }
            finally
            {
                DestroyPresenter(strictPresenter);
            }
        }

        private static void ValidateUnavailableProfileRecoveryAndLegacyCompatibility()
        {
            var recovered = FakeShellFlowContext.AtRecoveredLobby(
                "builtin-test-v1", "orchard-removed", LobbyPresenter.Orchard01LevelId);
            var presenter = CreatePresenter<LobbyPresenter>("LobbyProfileRecoveryValidation");
            try
            {
                presenter.Initialize(recovered);
                Assert(recovered.RecoveredUnavailableLevelId == "orchard-removed"
                    && presenter.SelectedLevelId == LobbyPresenter.Orchard01LevelId,
                    "unavailable stored identity remains observable while safe UI default is selected");
                Assert(presenter.TryStart()
                    && recovered.StartLevelId == LobbyPresenter.Orchard01LevelId,
                    "profile recovery starts only the declared visible default");
            }
            finally
            {
                DestroyPresenter(presenter);
            }

            var legacy = new LegacyLobbyContext("builtin-test-v1");
            var legacyPresenter = CreatePresenter<LobbyPresenter>("LobbyLegacyContextValidation");
            try
            {
                legacyPresenter.Initialize(legacy);
                Assert(legacyPresenter.SelectedLevelId == LobbyPresenter.Orchard01LevelId
                    && legacyPresenter.TryStart()
                    && legacy.StartLevelId == LobbyPresenter.Orchard01LevelId,
                    "base shell contexts retain orchard-01 compatibility");
            }
            finally
            {
                DestroyPresenter(legacyPresenter);
            }
        }

        private static void ValidateSettlementDisplay()
        {
            var context = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyPresenter.Orchard03LevelId, true, 12, 3));
            var presenter = CreatePresenter<SettlementPresenter>("SettlementDisplayValidation");
            try
            {
                presenter.Initialize(context);
                Assert(presenter.HasViewData, "valid Settlement binds view data");
                Assert(presenter.ViewData.LevelId == LobbyPresenter.Orchard03LevelId
                    && presenter.ViewData.Victory
                    && presenter.ViewData.ReachedWave == 12
                    && presenter.ViewData.RemainingLives == 3,
                    "Settlement displays completed level, outcome, wave, and lives exactly");
            }
            finally
            {
                DestroyPresenter(presenter);
            }
        }

        private static void ValidateReturnAndRetry()
        {
            var returnContext = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyPresenter.Orchard03LevelId, false, 7, 0));
            var returnPresenter = CreatePresenter<SettlementPresenter>("SettlementReturnValidation");
            try
            {
                returnPresenter.Initialize(returnContext);
                Assert(returnPresenter.TryReturn(), "Return command is accepted");
                Assert(returnContext.ReturnCount == 1 && returnContext.ClearedBeforeReturn,
                    "Return clears completed session/result before navigation");
                Assert(returnContext.SelectedLevelId == LobbyPresenter.Orchard03LevelId
                    && returnContext.PersistedSelectedLevelId == LobbyPresenter.Orchard03LevelId,
                    "Return restores the completed level as the Lobby selection");
                Assert(returnContext.Navigator.HasPendingRoute
                    && returnContext.Navigator.PendingRoute == AppRoute.Lobby,
                    "Return requests Lobby");
                Assert(!returnPresenter.TryReturn() && returnContext.ReturnCount == 1,
                    "duplicate Return is ignored while navigation loads");
            }
            finally
            {
                DestroyPresenter(returnPresenter);
            }

            var retryContext = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyPresenter.Orchard03LevelId, false, 9, 0));
            var completedSessionId = retryContext.CompletedSessionId;
            var completedSeed = retryContext.CompletedSeed;
            var retryPresenter = CreatePresenter<SettlementPresenter>("SettlementRetryValidation");
            try
            {
                retryPresenter.Initialize(retryContext);
                Assert(retryPresenter.TryRetry(), "Retry command is accepted");
                Assert(retryContext.RetryCount == 1,
                    "Retry issues exactly one flow command");
                Assert(retryContext.RetrySessionId != completedSessionId
                    && Guid.TryParse(retryContext.RetrySessionId, out _),
                    "Retry creates a fresh session identity");
                Assert(retryContext.RetrySeed != 0 && retryContext.RetrySeed != completedSeed,
                    "Retry creates a fresh nonzero seed");
                Assert(retryContext.RetryLevelId == LobbyPresenter.Orchard03LevelId
                    && retryContext.RetryContentVersion == "builtin-test-v1",
                    "Retry retains the completed level and content version");
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
            ValidateRecovery(true, "settlement-result-level-mismatch");
        }

        private static void ValidateRecovery(bool mismatch, string expectedErrorCode)
        {
            var context = FakeShellFlowContext.AtSettlement(
                new SettlementViewData(LobbyPresenter.Orchard02LevelId, true, 1, 1));
            context.HasSettlementResult = false;
            context.ResultMismatch = mismatch;
            var presenter = CreatePresenter<SettlementPresenter>(
                mismatch ? "SettlementMismatchValidation" : "SettlementMissingValidation");
            try
            {
                presenter.Initialize(context);
                Assert(!presenter.HasViewData,
                    "invalid Settlement does not bind fabricated view data");
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

        private sealed class FakeShellFlowContext : IShellFlowContext, ILevelSelectionFlowContext
        {
            private static readonly IReadOnlyList<LevelDefinition> Levels = Array.AsReadOnly(new[]
            {
                new LevelDefinition("orchard-01", "map-01", "waves-01", "rules-01", "theme-01"),
                new LevelDefinition("orchard-02", "map-02", "waves-02", "rules-02", "theme-02"),
                new LevelDefinition("orchard-03", "map-03", "waves-03", "rules-03", "theme-03"),
            });

            private readonly AppNavigator _navigator;
            private SettlementViewData _settlementViewData;

            private FakeShellFlowContext(AppNavigator navigator, string bundledContentVersion,
                string selectedLevelId)
            {
                _navigator = navigator;
                BundledContentVersion = bundledContentVersion;
                SelectedLevelId = selectedLevelId;
                PersistedSelectedLevelId = selectedLevelId;
                CompletedSessionId = Guid.NewGuid().ToString("N");
                CompletedSeed = 301;
                CompletedLevelId = selectedLevelId;
                CompletedContentVersion = bundledContentVersion;
            }

            public IAppNavigator Navigator => _navigator;
            public string BundledContentVersion { get; }
            public IReadOnlyList<LevelDefinition> PlayableLevels => Levels;
            public string SelectedLevelId { get; private set; }
            public string PersistedSelectedLevelId { get; private set; }
            public string RecoveredUnavailableLevelId { get; private set; }
            public int SelectionCount { get; private set; }
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

            public static FakeShellFlowContext AtLobby(string bundledContentVersion,
                string selectedLevelId)
            {
                return new FakeShellFlowContext(new AppNavigator(), bundledContentVersion,
                    selectedLevelId);
            }

            public static FakeShellFlowContext AtRecoveredLobby(string bundledContentVersion,
                string unavailableLevelId, string defaultLevelId)
            {
                var context = AtLobby(bundledContentVersion, defaultLevelId);
                context.RecoveredUnavailableLevelId = unavailableLevelId;
                return context;
            }

            public static FakeShellFlowContext AtSettlement(SettlementViewData viewData)
            {
                var navigator = new AppNavigator();
                Transition(navigator, AppRoute.Battle);
                Transition(navigator, AppRoute.Settlement);
                var levelId = string.IsNullOrEmpty(viewData.LevelId)
                    ? LobbyPresenter.Orchard01LevelId
                    : viewData.LevelId;
                return new FakeShellFlowContext(navigator, "builtin-test-v1", levelId)
                {
                    _settlementViewData = viewData,
                    HasSettlementResult = true,
                    CompletedLevelId = levelId,
                };
            }

            public bool TrySelectLevel(string levelId, out ShellFlowError error)
            {
                if (_navigator.TransitionState != AppTransitionState.Idle)
                {
                    error = new ShellFlowError("app-transition-in-progress");
                    return false;
                }
                if (!ContainsLevel(levelId))
                {
                    error = new ShellFlowError("battle-level-resolution-failed", levelId);
                    return false;
                }

                SelectedLevelId = levelId;
                PersistedSelectedLevelId = levelId;
                SelectionCount++;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryStartDefaultBattle(
                string levelId,
                string sessionId,
                int seed,
                string contentVersion,
                out ShellFlowError error)
            {
                if (!ContainsLevel(levelId)
                    || !string.Equals(levelId, SelectedLevelId, StringComparison.Ordinal))
                {
                    error = new ShellFlowError("battle-level-resolution-failed", levelId);
                    return false;
                }
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

            public bool TryGetSettlementViewData(out SettlementViewData viewData,
                out ShellFlowError error)
            {
                if (ResultMismatch)
                {
                    viewData = default;
                    error = new ShellFlowError("settlement-result-level-mismatch");
                    return false;
                }

                if (!HasSettlementResult)
                {
                    viewData = default;
                    error = new ShellFlowError(SettlementPresenter.MissingResult);
                    return false;
                }
                if (!string.Equals(_settlementViewData.LevelId, CompletedLevelId,
                        StringComparison.Ordinal))
                {
                    viewData = default;
                    error = new ShellFlowError("settlement-result-level-mismatch");
                    return false;
                }

                viewData = _settlementViewData;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryReturnToLobby(out ShellFlowError error)
            {
                if (_navigator.TransitionState != AppTransitionState.Idle)
                {
                    error = new ShellFlowError("app-transition-in-progress");
                    return false;
                }

                ReturnCount++;
                if (!string.IsNullOrEmpty(CompletedLevelId))
                {
                    SelectedLevelId = CompletedLevelId;
                    PersistedSelectedLevelId = CompletedLevelId;
                }
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
                if (_navigator.TransitionState != AppTransitionState.Idle)
                {
                    error = new ShellFlowError("app-transition-in-progress");
                    return false;
                }

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
                RetrySessionId = sessionId == previousSession
                    ? Guid.NewGuid().ToString("N")
                    : sessionId;
                RetrySeed = seed;
                HasSettlementResult = false;
                error = ShellFlowError.None;
                return true;
            }

            public void ReportRecoverableError(ShellFlowError error)
            {
                ReportedError = error;
            }

            private static bool ContainsLevel(string levelId)
            {
                for (var i = 0; i < Levels.Count; i++)
                {
                    if (string.Equals(Levels[i].LevelId, levelId, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }

            private static void Transition(AppNavigator navigator, AppRoute route)
            {
                if (!navigator.TryBeginTransition(route, out var error)
                    || !navigator.TryCompleteTransition(out error))
                    throw new InvalidOperationException("Fake navigation setup failed: " + error);
            }
        }

        private sealed class LegacyLobbyContext : IShellFlowContext
        {
            private readonly AppNavigator _navigator = new AppNavigator();

            public LegacyLobbyContext(string contentVersion)
            {
                BundledContentVersion = contentVersion;
            }

            public IAppNavigator Navigator => _navigator;
            public string BundledContentVersion { get; }
            public string StartLevelId { get; private set; }

            public bool TryStartDefaultBattle(string levelId, string sessionId, int seed,
                string contentVersion, out ShellFlowError error)
            {
                if (!_navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                {
                    error = new ShellFlowError(navigationError);
                    return false;
                }
                StartLevelId = levelId;
                error = ShellFlowError.None;
                return true;
            }

            public bool TryGetSettlementViewData(out SettlementViewData viewData,
                out ShellFlowError error)
            {
                viewData = default;
                error = new ShellFlowError(SettlementPresenter.MissingResult);
                return false;
            }

            public bool TryReturnToLobby(out ShellFlowError error)
            {
                error = new ShellFlowError("unsupported");
                return false;
            }

            public bool TryRetryBattle(out ShellFlowError error)
            {
                error = new ShellFlowError("unsupported");
                return false;
            }

            public void ReportRecoverableError(ShellFlowError error)
            {
            }
        }
    }
}
