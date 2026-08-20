using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FruitDefense.App.Services;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Shell;
using FruitDefense.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.App
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class AppFlowCoordinator : MonoBehaviour, IShellFlowContext,
        ILevelSelectionFlowContext, IBattleResultSink
    {
        public const string BootstrapScene = "Bootstrap";
        public const string LobbyScene = "Lobby";
        public const string BattleScene = "Battle";
        public const string SettlementScene = "Settlement";

        public const string FlowNotReady = "app-flow-not-ready";
        public const string SceneUnavailable = "app-scene-unavailable";
        public const string SceneLoadFailed = "app-scene-load-failed";
        public const string BattleHostMissing = "battle-host-missing";
        public const string LobbyPresenterMissing = "lobby-presenter-missing";
        public const string SettlementPresenterMissing = "settlement-presenter-missing";
        public const string BattleRequestActive = "battle-request-already-active";
        public const string BattleResultMissing = "battle-result-missing";
        public const string RuntimeConfigInvalid = "runtime-config-invalid";
        public const string BundledContentInvalid = "bundled-content-invalid";
        public const string BundledContentMismatch = "bundled-content-version-mismatch";
        public const string BundledLevelCatalogInvalid = "bundled-level-catalog-invalid";
        public const string LevelResolutionFailed = "battle-level-resolution-failed";
        public const string StoredLevelUnavailable = "stored-level-unavailable";
        public const string ProfileSelectionSaveFailed = "profile-selection-save-failed";
        public const string RuntimeUiThemeInvalid = "runtime-ui-theme-invalid";

        public readonly struct BootstrapPresentationLayout
        {
            public BootstrapPresentationLayout(Rect screen, Rect safeArea, Rect modal,
                Rect title, Rect status, Rect retryAction, Rect recoverableStatus, float scale)
            {
                Screen = screen;
                SafeArea = safeArea;
                Modal = modal;
                Title = title;
                Status = status;
                RetryAction = retryAction;
                RecoverableStatus = recoverableStatus;
                Scale = scale;
            }

            public Rect Screen { get; }
            public Rect SafeArea { get; }
            public Rect Modal { get; }
            public Rect Title { get; }
            public Rect Status { get; }
            public Rect RetryAction { get; }
            public Rect RecoverableStatus { get; }
            public float Scale { get; }
        }

        [SerializeField] private RuntimeUiTheme runtimeUiTheme;

        private AppBootstrap _bootstrap;
        private IPlayerProfileStore _profileStore;
        private IRemoteConfigService _remoteConfigService;
        private PlayerProfileEnvelopeV1 _profile;
        private RuntimeConfigV1 _runtimeConfig;
        private CompiledLevelCatalog _levelCatalog;
        private BattleLaunchRequest _currentRequest;
        private ResolvedLevelDefinition _currentResolvedLevel;
        private BattleResult _currentResult;
        private IBattleSessionHost _activeBattleHost;
        private bool _compositionReady;
        private bool _startupRoutineActive;
        private string _blockingError = string.Empty;
        private ShellFlowError _lastRecoverableError;
        private string _selectedLevelId = string.Empty;
        private bool _profileSaveRoutineActive;
        private bool _profileSavePending;
        private bool _runtimeUiPresentationReady;
        private RuntimeUiDrawContext _runtimeUiDrawContext;
        private RuntimeUiFeedbackPulse _bootstrapTransitionPulse;
        private RuntimeUiFeedbackPulse _bootstrapStatusPulse;
        private RuntimeUiFeedbackPulse _retryFocusPulse;
        private RuntimeUiFeedbackPulse _retryPressPulse;
        private string _observedBlockingError = string.Empty;
        private string _observedRecoverableError = string.Empty;

        public IAppNavigator Navigator => _bootstrap == null ? null : _bootstrap.Navigator;
        public string BundledContentVersion { get; private set; } = string.Empty;
        public bool IsCompositionReady => _compositionReady;
        public string BlockingError => _blockingError;
        public ShellFlowError LastRecoverableError => _lastRecoverableError;
        public BattleLaunchRequest CurrentRequest => _currentRequest;
        public BattleResult CurrentResult => _currentResult;
        public PlayerProfileEnvelopeV1 CurrentProfile => _profile;
        public RuntimeConfigV1 CurrentRuntimeConfig => _runtimeConfig;
        public CompiledLevelCatalog CurrentLevelCatalog => _levelCatalog;
        public ResolvedLevelDefinition CurrentResolvedLevel => _currentResolvedLevel;
        public IReadOnlyList<LevelDefinition> PlayableLevels => _levelCatalog == null
            ? Array.Empty<LevelDefinition>()
            : _levelCatalog.PlayableLevels;
        public string SelectedLevelId => _selectedLevelId;
        public RuntimeUiTheme RuntimeUiTheme => runtimeUiTheme;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void FruitDefenseAcceptanceReady(
            int route,
            string sessionId,
            int seed,
            string levelId,
            string mapId,
            string waveSetId,
            string ruleSetId,
            string themeId);
#endif

        private void Start()
        {
            BeginStartup();
        }

        private void BeginStartup()
        {
            if (_startupRoutineActive) return;
            StartCoroutine(InitializeAndRoute());
        }

        private IEnumerator InitializeAndRoute()
        {
            _startupRoutineActive = true;
            _compositionReady = false;
            _blockingError = string.Empty;
            _runtimeUiPresentationReady = false;
            _bootstrapTransitionPulse = default;

            var themeValidation = runtimeUiTheme == null ? null : runtimeUiTheme.Validate();
            if (themeValidation == null || !themeValidation.IsValid)
            {
                _blockingError = RuntimeUiThemeInvalid + ":" + (themeValidation == null
                    ? "theme-required"
                    : themeValidation.Issues[0].Code);
                _startupRoutineActive = false;
                yield break;
            }
            _runtimeUiPresentationReady = true;
            _bootstrapTransitionPulse = RuntimeUiFeedbackPulse.Begin(
                Time.unscaledTime, runtimeUiTheme.Feedback.UnscaledTransitionSeconds);

            while (AppBootstrap.Instance == null) yield return null;
            _bootstrap = AppBootstrap.Instance;
            while (_bootstrap.IsInitializing || !_bootstrap.IsInitialized) yield return null;

            if (!_bootstrap.IsReady)
            {
                _blockingError = _bootstrap.InitializationResult.ErrorCode;
                _startupRoutineActive = false;
                yield break;
            }

            _profileStore = LocalProfileStoreFactory.CreateDefault();
            _remoteConfigService = new BundledRemoteConfigService();

            RemoteConfigLoadResult configResult = null;
            yield return _remoteConfigService.Load(value => configResult = value);
            if (configResult == null || configResult.Status != RemoteConfigLoadStatus.Success)
            {
                _blockingError = RuntimeConfigInvalid;
                _startupRoutineActive = false;
                yield break;
            }
            _runtimeConfig = configResult.Config;

            ProfileLoadResult profileResult = null;
            yield return _profileStore.Load(value => profileResult = value);
            _profile = profileResult != null && profileResult.HasProfile
                ? profileResult.Profile
                : PlayerProfileEnvelopeV1.CreateDefault();
            if (profileResult == null || !profileResult.HasProfile)
                _lastRecoverableError = new ShellFlowError("local-profile-unavailable", profileResult?.Error);
            else if (profileResult.Status == ProfileLoadStatus.StorageError)
                _lastRecoverableError = new ShellFlowError("local-profile-storage-degraded", profileResult.Error);

            if (!BundledLevelCatalogFactory.TryCompile(out _levelCatalog,
                    out var levelValidation, out var contentValidation))
            {
                if (contentValidation != null && !contentValidation.IsValid
                    && contentValidation.Issues.Count > 0)
                {
                    _blockingError = BundledContentInvalid + ":" + contentValidation.Issues[0].code;
                }
                else
                {
                    var issue = levelValidation != null && levelValidation.Issues.Count > 0
                        ? levelValidation.Issues[0].Code
                        : "unknown";
                    _blockingError = BundledLevelCatalogInvalid + ":" + issue;
                }
                _startupRoutineActive = false;
                yield break;
            }
            BundledContentVersion = _levelCatalog.ContentVersion;
            if (!string.Equals(
                    BundledContentVersion,
                    _runtimeConfig.bundledContentVersion,
                    StringComparison.Ordinal))
            {
                _blockingError = BundledContentMismatch;
                _startupRoutineActive = false;
                yield break;
            }

            if (_levelCatalog.TryResolve(_profile.lastSelectedLevelId,
                    out var storedLevel, out var storedLevelError))
            {
                _selectedLevelId = storedLevel.Identity.LevelId;
            }
            else
            {
                var invalidStoredLevel = _profile.lastSelectedLevelId ?? string.Empty;
                _selectedLevelId = _levelCatalog.DefaultLevelId;
                _profile.lastSelectedLevelId = _selectedLevelId;
                _lastRecoverableError = new ShellFlowError(StoredLevelUnavailable,
                    invalidStoredLevel + ":" + (storedLevelError == null
                        ? "unknown"
                        : storedLevelError.ToString()));

                ProfileSaveResult recoverySave = null;
                yield return _profileStore.Save(_profile, value => recoverySave = value);
                if (recoverySave != null && recoverySave.Status == ProfileSaveStatus.Success)
                {
                    _profile = recoverySave.Profile;
                }
                else
                {
                    ReportRecoverableError(new ShellFlowError(ProfileSelectionSaveFailed,
                        recoverySave?.Error));
                }
            }

            _compositionReady = true;
            _startupRoutineActive = false;

            string publishedPlaytestLevelId;
            if (PublishedBattlefieldPlaytestRequest.TryConsume(out publishedPlaytestLevelId))
            {
                _selectedLevelId = publishedPlaytestLevelId;
                if (!TryStartDefaultBattle(
                    publishedPlaytestLevelId,
                    "map-editor-playtest-" + Guid.NewGuid().ToString("N"),
                    20260723,
                    BundledContentVersion,
                    out var playtestError))
                {
                    _blockingError = playtestError.Code + ":" + playtestError.Detail;
                }
                yield break;
            }

            if (ShouldEnterAcceptanceBattle())
            {
                var acceptanceLevelId = AcceptanceLevelId();
                if (!TryStartDefaultBattle(
                    acceptanceLevelId,
                    "acceptance-" + Guid.NewGuid().ToString("N"),
                    20260714,
                    BundledContentVersion,
                    out var acceptanceError))
                {
                    _blockingError = acceptanceError.Code + ":" + acceptanceError.Detail;
                }
                yield break;
            }

            yield return LoadInitialLobby();
        }

        public bool TryStartDefaultBattle(
            string levelId,
            string sessionId,
            int seed,
            string contentVersion,
            out ShellFlowError error)
        {
            if (!_compositionReady || Navigator == null)
                return Fail(FlowNotReady, out error);
            if (_currentRequest != null || _activeBattleHost != null)
                return Fail(BattleRequestActive, out error);

            var request = new BattleLaunchRequest(sessionId, levelId, seed, contentVersion);
            if (!request.TryValidate(out var requestError))
                return Fail(requestError, out error);
            if (!string.Equals(contentVersion, BundledContentVersion, StringComparison.Ordinal))
                return Fail(BundledContentMismatch, out error);
            if (!TryResolveLevel(levelId, out var resolvedLevel, out error))
                return false;
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = request;
            _currentResolvedLevel = resolvedLevel;
            _currentResult = null;
            StartCoroutine(LoadBattle(request, resolvedLevel));
            error = ShellFlowError.None;
            return true;
        }

        public bool TrySelectLevel(string levelId, out ShellFlowError error)
        {
            if (!_compositionReady || _levelCatalog == null)
                return Fail(FlowNotReady, out error);
            if (!TryResolveLevel(levelId, out var resolvedLevel, out error))
                return false;

            _selectedLevelId = resolvedLevel.Identity.LevelId;
            if (_profile != null
                && !string.Equals(_profile.lastSelectedLevelId, _selectedLevelId,
                    StringComparison.Ordinal))
            {
                _profile.lastSelectedLevelId = _selectedLevelId;
                QueueProfileSelectionSave();
            }

            error = ShellFlowError.None;
            if (Navigator != null
                && Navigator.CurrentRoute == AppRoute.Lobby
                && Navigator.TransitionState == AppTransitionState.Idle)
            {
                SignalAcceptanceRouteReady(AppRoute.Lobby);
            }
            return true;
        }

        public bool TrySubmitResult(BattleResult result, out string errorCode)
        {
            if (result == null)
            {
                errorCode = BattleResultMissing;
                return false;
            }
            if (_currentRequest == null)
            {
                errorCode = BattleResult.MissingRequest;
                return false;
            }
            if (!result.TryValidate(_currentRequest, out errorCode))
            {
                return false;
            }
            if (_currentResult != null)
            {
                errorCode = "battle-result-already-recorded";
                return false;
            }

            _currentResult = result;
            if (!Navigator.TryBeginTransition(AppRoute.Settlement, out errorCode))
            {
                ReportRecoverableError(new ShellFlowError(errorCode));
                StartCoroutine(RecoverToLobby(errorCode));
                return true;
            }

            StartCoroutine(LoadSettlement());
            errorCode = string.Empty;
            return true;
        }

        public bool TryGetSettlementViewData(out SettlementViewData viewData, out ShellFlowError error)
        {
            if (_currentRequest == null || _currentResult == null)
            {
                viewData = default;
                return Fail(BattleResultMissing, out error);
            }
            if (!_currentResult.TryValidate(_currentRequest, out var resultError))
            {
                viewData = default;
                return Fail(resultError, out error);
            }

            viewData = new SettlementViewData(
                _currentResult.LevelId,
                _currentResult.Outcome == BattleOutcome.Victory,
                _currentResult.ReachedWave,
                _currentResult.RemainingLives);
            error = ShellFlowError.None;
            return true;
        }

        public bool TryReturnToLobby(out ShellFlowError error)
        {
            if (!_compositionReady || Navigator == null)
                return Fail(FlowNotReady, out error);
            if (_currentResult != null
                && !TrySelectLevel(_currentResult.LevelId, out error))
            {
                return false;
            }
            if (!Navigator.TryBeginTransition(AppRoute.Lobby, out var navigationError))
                return Fail(navigationError, out error);

            StartCoroutine(LoadLobbyTransition());
            error = ShellFlowError.None;
            return true;
        }

        public bool TryRetryBattle(out ShellFlowError error)
        {
            if (!_compositionReady || Navigator == null)
                return Fail(FlowNotReady, out error);
            if (_currentRequest == null || _currentResult == null)
                return Fail(BattleResultMissing, out error);
            if (!_currentResult.TryValidate(_currentRequest, out var resultError))
                return Fail(resultError, out error);
            if (!TryResolveLevel(_currentResult.LevelId, out var retryLevel, out error))
                return false;

            var retrySeed = CreateNonzeroSeed();
            while (retrySeed == _currentRequest.Seed) retrySeed = CreateNonzeroSeed();

            var retry = new BattleLaunchRequest(
                Guid.NewGuid().ToString("N"),
                _currentResult.LevelId,
                retrySeed,
                _currentRequest.ContentVersion);
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = retry;
            _currentResolvedLevel = retryLevel;
            _currentResult = null;
            _activeBattleHost = null;
            StartCoroutine(LoadBattle(retry, retryLevel));
            error = ShellFlowError.None;
            return true;
        }

        public void ReportRecoverableError(ShellFlowError error)
        {
            if (!error.IsEmpty) _lastRecoverableError = error;
        }

        private IEnumerator LoadInitialLobby()
        {
            yield return LoadScene(LobbyScene, result =>
            {
                if (!result.Success)
                {
                    _blockingError = result.ErrorCode;
                    return;
                }
                if (BindLobbyPresenter()) SignalAcceptanceRouteReady(AppRoute.Lobby);
            });
        }

        private IEnumerator LoadLobbyTransition()
        {
            yield return LoadScene(LobbyScene, result =>
            {
                if (!result.Success)
                {
                    FailActiveTransition(result.ErrorCode);
                    if (Navigator is IAppRecoveryNavigator recoveryNavigator)
                        recoveryNavigator.TryRestoreCurrentRoute(result.ErrorCode, out _);
                    ReportRecoverableError(new ShellFlowError(result.ErrorCode));
                    return;
                }
                if (!Navigator.TryCompleteTransition(out var completeError))
                {
                    _blockingError = completeError;
                    return;
                }
                ClearCompletedSession();
                _lastRecoverableError = ShellFlowError.None;
                if (BindLobbyPresenter()) SignalAcceptanceRouteReady(AppRoute.Lobby);
            });
        }

        private IEnumerator LoadBattle(BattleLaunchRequest request,
            ResolvedLevelDefinition resolvedLevel)
        {
            yield return LoadScene(BattleScene, result =>
            {
                if (!result.Success)
                {
                    RecoverAfterRouteFailure(result.ErrorCode);
                    return;
                }

                var host = FindFirstObjectByType<FruitDefenseGame>();
                if (host == null)
                {
                    RecoverAfterRouteFailure(BattleHostMissing);
                    return;
                }

                if (!ReferenceEquals(_currentResolvedLevel, resolvedLevel))
                {
                    RecoverAfterRouteFailure(BattleSessionInitializationResult.ResolvedLevelMismatch);
                    return;
                }

                var initialization = host.Initialize(
                    request, Navigator, this, runtimeUiTheme, resolvedLevel);
                if (!initialization.Success)
                {
                    RecoverAfterRouteFailure(initialization.ErrorCode);
                    return;
                }

                _activeBattleHost = host;
                if (!Navigator.TryCompleteTransition(out var completeError))
                {
                    host.DisposeSession();
                    _activeBattleHost = null;
                    RecoverAfterRouteFailure(completeError);
                    return;
                }

                _lastRecoverableError = ShellFlowError.None;
                SignalAcceptanceRouteReady(AppRoute.Battle);
            });
        }

        private IEnumerator LoadSettlement()
        {
            _activeBattleHost = null;
            yield return LoadScene(SettlementScene, result =>
            {
                if (!result.Success)
                {
                    RecoverAfterRouteFailure(result.ErrorCode);
                    return;
                }
                if (!Navigator.TryCompleteTransition(out var completeError))
                {
                    RecoverAfterRouteFailure(completeError);
                    return;
                }

                var presenter = FindFirstObjectByType<SettlementPresenter>();
                if (presenter == null)
                {
                    ReportRecoverableError(new ShellFlowError(SettlementPresenterMissing));
                    StartCoroutine(RecoverToLobby(SettlementPresenterMissing));
                    return;
                }
                _lastRecoverableError = ShellFlowError.None;
                presenter.Initialize(this, runtimeUiTheme);
                SignalAcceptanceRouteReady(AppRoute.Settlement);
            });
        }

        private IEnumerator RecoverToLobby(string errorCode)
        {
            ReportRecoverableError(new ShellFlowError(errorCode));
            ClearCompletedSession();

            if (Navigator is IAppRecoveryNavigator recoveryNavigator)
            {
                recoveryNavigator.TryRecoverToLobby(errorCode, out _);
                yield return LoadInitialLobby();
                yield break;
            }

            if (Navigator != null && Navigator.TransitionState == AppTransitionState.Loading)
                Navigator.TryFailTransition(errorCode, out _);
            yield return LoadInitialLobby();
        }

        private IEnumerator LoadScene(string sceneName, Action<SceneLoadResult> completed)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                completed(SceneLoadResult.Failed(SceneUnavailable + ":" + sceneName));
                yield break;
            }

            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                completed(SceneLoadResult.Failed(SceneLoadFailed + ":" + exception.GetType().Name));
                yield break;
            }

            if (operation == null)
            {
                completed(SceneLoadResult.Failed(SceneLoadFailed + ":" + sceneName));
                yield break;
            }
            while (!operation.isDone) yield return null;
            completed(SceneLoadResult.Succeeded());
        }

        private bool BindLobbyPresenter()
        {
            var presenter = FindFirstObjectByType<LobbyPresenter>();
            if (presenter == null)
            {
                _blockingError = LobbyPresenterMissing;
                return false;
            }
            presenter.Initialize(this, runtimeUiTheme);
            return true;
        }

        private void RecoverAfterRouteFailure(string errorCode)
        {
            FailActiveTransition(errorCode);
            ReportRecoverableError(new ShellFlowError(errorCode));
            StartCoroutine(RecoverToLobby(errorCode));
        }

        private void FailActiveTransition(string errorCode)
        {
            if (Navigator != null && Navigator.TransitionState == AppTransitionState.Loading)
                Navigator.TryFailTransition(errorCode, out _);
        }

        private void ClearCompletedSession()
        {
            _activeBattleHost?.DisposeSession();
            _activeBattleHost = null;
            _currentRequest = null;
            _currentResolvedLevel = null;
            _currentResult = null;
        }

        private bool ShouldEnterAcceptanceBattle()
        {
            var launch = _bootstrap.PlatformAdapter?.LaunchContext;
            return launch != null
                && launch.TryGetQuery("acceptance", out _)
                && launch.TryGetQuery("route", out var route)
                && string.Equals(route, "battle", StringComparison.OrdinalIgnoreCase);
        }

        private string AcceptanceLevelId()
        {
            var launch = _bootstrap.PlatformAdapter?.LaunchContext;
            if (launch != null)
            {
                if (launch.TryGetQuery("level", out var levelId)) return levelId;
                if (launch.TryGetQuery("levelId", out levelId)) return levelId;
            }
            return _levelCatalog.DefaultLevelId;
        }

        public void ConfigureAcceptanceFlow(string command)
        {
            if (!IsAcceptanceLaunch() || string.IsNullOrWhiteSpace(command)) return;
            switch (command)
            {
                case "victory":
                    if (_activeBattleHost?.Simulation == null) return;
                    _activeBattleHost.Simulation.State.Phase = global::FruitDefense.Core.GamePhase.Victory;
                    _activeBattleHost.Simulation.State.WaveIndex =
                        _activeBattleHost.Simulation.MaxWaves;
                    _activeBattleHost.Simulation.State.Lives = 3;
                    _activeBattleHost.TrySubmitTerminalResult();
                    break;
                case "defeat":
                    if (_activeBattleHost?.Simulation == null) return;
                    _activeBattleHost.Simulation.State.Phase = global::FruitDefense.Core.GamePhase.Defeat;
                    _activeBattleHost.Simulation.State.Lives = 0;
                    _activeBattleHost.TrySubmitTerminalResult();
                    break;
            }
        }

        private bool IsAcceptanceLaunch()
        {
            var launch = _bootstrap?.PlatformAdapter?.LaunchContext;
            return launch != null && launch.TryGetQuery("acceptance", out _);
        }

        private void SignalAcceptanceRouteReady(AppRoute route)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsAcceptanceLaunch()) return;

            var resolvedLevel = _currentResolvedLevel;
            if (resolvedLevel == null && _levelCatalog != null
                && _levelCatalog.TryResolve(_selectedLevelId,
                    out var selectedLevel, out _))
            {
                resolvedLevel = selectedLevel;
            }

            var identity = resolvedLevel?.Identity;
            FruitDefenseAcceptanceReady(
                (int)route,
                _currentRequest?.SessionId ?? string.Empty,
                _currentRequest?.Seed ?? 0,
                identity?.LevelId ?? string.Empty,
                identity?.MapId ?? string.Empty,
                identity?.WaveSetId ?? string.Empty,
                identity?.RuleSetId ?? string.Empty,
                identity?.ThemeId ?? string.Empty);
#endif
        }

        private static int CreateNonzeroSeed()
        {
            var seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & int.MaxValue;
            return seed == 0 ? 1 : seed;
        }

        private bool TryResolveLevel(string levelId, out ResolvedLevelDefinition resolvedLevel,
            out ShellFlowError error)
        {
            resolvedLevel = null;
            if (_levelCatalog == null) return Fail(FlowNotReady, out error);
            var resolution = _levelCatalog.Resolve(levelId);
            if (!resolution.Succeeded)
            {
                error = new ShellFlowError(LevelResolutionFailed,
                    resolution.Error == null ? "unknown" : resolution.Error.ToString());
                ReportRecoverableError(error);
                return false;
            }

            resolvedLevel = resolution.Value;
            error = ShellFlowError.None;
            return true;
        }

        private void QueueProfileSelectionSave()
        {
            _profileSavePending = true;
            if (!_profileSaveRoutineActive) StartCoroutine(SaveProfileSelectionLoop());
        }

        private IEnumerator SaveProfileSelectionLoop()
        {
            _profileSaveRoutineActive = true;
            while (_profileSavePending)
            {
                _profileSavePending = false;
                var selectedAtSave = _selectedLevelId;
                var profileToSave = PlayerProfileCodec.Clone(_profile);
                profileToSave.lastSelectedLevelId = selectedAtSave;

                ProfileSaveResult saveResult = null;
                yield return _profileStore.Save(profileToSave, value => saveResult = value);
                if (saveResult != null && saveResult.Status == ProfileSaveStatus.Success)
                {
                    _profile = saveResult.Profile;
                    if (!string.Equals(_selectedLevelId, selectedAtSave, StringComparison.Ordinal))
                    {
                        _profile.lastSelectedLevelId = _selectedLevelId;
                        _profileSavePending = true;
                    }
                }
                else
                {
                    ReportRecoverableError(new ShellFlowError(ProfileSelectionSaveFailed,
                        saveResult?.Error));
                }
            }
            _profileSaveRoutineActive = false;
        }

        private bool Fail(string code, out ShellFlowError error)
        {
            error = new ShellFlowError(code);
            ReportRecoverableError(error);
            return false;
        }

        public static BootstrapPresentationLayout CreateBootstrapPresentationLayout(
            float viewportWidth, float viewportHeight, Rect screenSafeArea,
            bool hasRetryAction = false)
        {
            if (viewportWidth <= 0f || viewportHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(viewportWidth),
                    "Viewport dimensions must be positive.");

            var screen = new Rect(0f, 0f, viewportWidth, viewportHeight);
            var xMin = Mathf.Max(screen.xMin, screenSafeArea.xMin);
            var yMin = Mathf.Max(screen.yMin, screenSafeArea.yMin);
            var xMax = Mathf.Min(screen.xMax, screenSafeArea.xMax);
            var yMax = Mathf.Min(screen.yMax, screenSafeArea.yMax);
            var safeArea = Rect.MinMaxRect(
                xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = screen;
            safeArea = PortraitShellLayout.ToGuiSafeArea(viewportHeight, safeArea);

            var scale = Mathf.Max(.001f, Mathf.Min(
                safeArea.width / PortraitShellLayout.ReferenceWidth,
                safeArea.height / PortraitShellLayout.ReferenceHeight));
            var horizontalMargin = 16f * scale;
            var width = Mathf.Min(360f * scale,
                Mathf.Max(0f, safeArea.width - horizontalMargin * 2f));
            var modalHeight = (hasRetryAction ? 190f : 142f) * scale;
            var modalY = safeArea.y + 262f * scale;
            modalHeight = Mathf.Min(modalHeight,
                Mathf.Max(0f, safeArea.yMax - modalY - 8f * scale));
            var modal = new Rect(
                safeArea.x + (safeArea.width - width) * .5f,
                modalY,
                width,
                modalHeight);

            var contentX = modal.x + 20f * scale;
            var contentWidth = Mathf.Max(0f, modal.width - 40f * scale);
            var title = new Rect(contentX, modal.y + 16f * scale,
                contentWidth, 34f * scale);
            var status = new Rect(contentX, modal.y + 56f * scale,
                contentWidth, 45f * scale);
            var retryAction = hasRetryAction
                ? new Rect(contentX, modal.y + 105f * scale,
                    contentWidth, 52f * scale)
                : default;
            var recoverableStatus = new Rect(
                safeArea.x + horizontalMargin,
                safeArea.yMax - 60f * scale,
                Mathf.Max(0f, safeArea.width - horizontalMargin * 2f),
                52f * scale);

            return new BootstrapPresentationLayout(screen, safeArea, modal,
                title, status, retryAction, recoverableStatus, scale);
        }

        public static string FormatBootstrapBlockingError(string rawError)
        {
            if (HasErrorCode(rawError, LevelResolutionFailed))
                return RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapLevelUnavailable).Text;

            if (HasErrorCode(rawError, RuntimeConfigInvalid)
                || HasErrorCode(rawError, RuntimeUiThemeInvalid))
            {
                return RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapConfigurationUnavailable).Text;
            }

            if (HasErrorCode(rawError, BundledContentInvalid)
                || HasErrorCode(rawError, BundledLevelCatalogInvalid)
                || HasErrorCode(rawError, BundledContentMismatch))
            {
                return RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapContentUnavailable).Text;
            }

            if (HasErrorCode(rawError, SceneUnavailable)
                || HasErrorCode(rawError, SceneLoadFailed)
                || HasErrorCode(rawError, BattleHostMissing)
                || HasErrorCode(rawError, LobbyPresenterMissing)
                || HasErrorCode(rawError, SettlementPresenterMissing))
            {
                return RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapPageUnavailable).Text;
            }

            return RuntimeUiCopyCatalog.Get(
                RuntimeUiCopyId.BootstrapUnknownFailure).Text;
        }

        private static bool HasErrorCode(string rawError, string code)
        {
            return string.Equals(rawError, code, StringComparison.Ordinal)
                || (!string.IsNullOrEmpty(rawError)
                    && rawError.StartsWith(code + ":", StringComparison.Ordinal));
        }

        private void OnGUI()
        {
            if (!_runtimeUiPresentationReady) return;

            var unscaledTime = Time.unscaledTime;
            RefreshBootstrapFeedback(unscaledTime);

            var hasBlockingError = !string.IsNullOrEmpty(_blockingError);
            var hasRetryAction = hasBlockingError
                && _bootstrap != null
                && _bootstrap.IsInitialized
                && !_bootstrap.InitializationResult.Success;
            var layout = CreateBootstrapPresentationLayout(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent(),
                hasRetryAction);
            _runtimeUiDrawContext = RuntimeUiGui.RequireContext(
                _runtimeUiDrawContext, runtimeUiTheme, layout.Scale);

            if (_compositionReady && string.IsNullOrEmpty(_blockingError))
            {
                if (!_lastRecoverableError.IsEmpty)
                {
                    var recoverableCopy = RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.BootstrapRecoverableError);
                    RuntimeUiGui.DrawStatus(_runtimeUiDrawContext,
                        layout.RecoverableStatus,
                        RuntimeUiCopyCatalog.FormatBootstrapRecoverableError(
                            _lastRecoverableError.Code),
                        RuntimeUiInteractionState.Warning,
                        recoverableCopy.Role,
                        RuntimeUiCopyCatalog.StatusTextMode(recoverableCopy),
                        _bootstrapStatusPulse.IsActive(unscaledTime));
                }
                return;
            }

            var presentationState = hasBlockingError
                ? RuntimeUiInteractionState.Error
                : RuntimeUiInteractionState.Loading;
            RuntimeUiGui.DrawScreenBackground(_runtimeUiDrawContext, layout.Screen);
            RuntimeUiGui.DrawSafeArea(_runtimeUiDrawContext, layout.SafeArea);
            RuntimeUiGui.DrawScreenCorners(_runtimeUiDrawContext, layout.SafeArea);
            RuntimeUiGui.DrawBlockingModal(_runtimeUiDrawContext,
                layout.Screen, layout.Modal, RuntimeUiInteractionState.Normal);
            var titleCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.ProductTitle);
            RuntimeUiGui.DrawSingleLineText(_runtimeUiDrawContext, layout.Title,
                titleCopy.Text, titleCopy.Role, titleCopy.Tone,
                titleCopy.Alignment,
                presentationState);
            var statusCopy = RuntimeUiCopyCatalog.Get(hasBlockingError
                ? RuntimeUiCopyId.BootstrapUnknownFailure
                : RuntimeUiCopyId.BootstrapLoading);
            RuntimeUiGui.DrawStatus(_runtimeUiDrawContext, layout.Status,
                hasBlockingError
                    ? FormatBootstrapBlockingError(_blockingError)
                    : statusCopy.Text,
                presentationState, statusCopy.Role,
                RuntimeUiCopyCatalog.StatusTextMode(statusCopy),
                hasBlockingError
                    ? _bootstrapStatusPulse.IsActive(unscaledTime)
                    : _bootstrapTransitionPulse.IsActive(unscaledTime));

            if (hasRetryAction)
            {
                var retryHovered = ContainsPointer(layout.RetryAction);
                if (retryHovered)
                {
                    _retryFocusPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                        runtimeUiTheme.Feedback.UnscaledFocusSeconds);
                }
                var retryPressed = IsPointerPress(layout.RetryAction)
                    || _retryPressPulse.IsActive(unscaledTime);
                var retryState = retryPressed
                    ? RuntimeUiInteractionState.Pressed
                    : retryHovered || _retryFocusPulse.IsActive(unscaledTime)
                        ? RuntimeUiInteractionState.HoveredOrFocused
                        : RuntimeUiInteractionState.Normal;
                var retryCopy = RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapRetry);
                if (!RuntimeUiGui.DrawAction(_runtimeUiDrawContext,
                    layout.RetryAction,
                    retryCopy.Text,
                    RuntimeUiActionKind.Primary,
                    retryState,
                    RuntimeUiArtSlot.IconControlRetry))
                    return;

                _retryPressPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                    runtimeUiTheme.Feedback.UnscaledPressSeconds);
                if (_bootstrap.TryRetryInitialization())
                {
                    _blockingError = string.Empty;
                    _bootstrapTransitionPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                        runtimeUiTheme.Feedback.UnscaledTransitionSeconds);
                    BeginStartup();
                }
            }
        }

        private void RefreshBootstrapFeedback(float unscaledTime)
        {
            if (!string.Equals(_observedBlockingError, _blockingError,
                    StringComparison.Ordinal))
            {
                _observedBlockingError = _blockingError ?? string.Empty;
                if (!string.IsNullOrEmpty(_observedBlockingError))
                {
                    _bootstrapStatusPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                        runtimeUiTheme.Feedback.UnscaledStatusSeconds);
                }
            }

            var recoverableError = _lastRecoverableError.Code ?? string.Empty;
            if (string.Equals(_observedRecoverableError, recoverableError,
                    StringComparison.Ordinal))
                return;
            _observedRecoverableError = recoverableError;
            if (!string.IsNullOrEmpty(recoverableError))
            {
                _bootstrapStatusPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                    runtimeUiTheme.Feedback.UnscaledStatusSeconds);
            }
        }

        private static bool ContainsPointer(Rect rect)
        {
            return Event.current != null && rect.Contains(Event.current.mousePosition);
        }

        private static bool IsPointerPress(Rect rect)
        {
            return ContainsPointer(rect) && Event.current.button == 0
                && (Event.current.rawType == EventType.MouseDown
                    || Event.current.rawType == EventType.MouseDrag);
        }

        private readonly struct SceneLoadResult
        {
            private SceneLoadResult(bool success, string errorCode)
            {
                Success = success;
                ErrorCode = errorCode ?? string.Empty;
            }

            public bool Success { get; }
            public string ErrorCode { get; }
            public static SceneLoadResult Succeeded() => new SceneLoadResult(true, string.Empty);
            public static SceneLoadResult Failed(string errorCode) => new SceneLoadResult(false, errorCode);
        }
    }
}
