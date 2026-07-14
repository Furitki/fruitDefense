using System;
using System.Collections;
using System.Runtime.InteropServices;
using FruitDefense.App.Services;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Shell;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.App
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class AppFlowCoordinator : MonoBehaviour, IShellFlowContext, IBattleResultSink
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

        private AppBootstrap _bootstrap;
        private IPlayerProfileStore _profileStore;
        private IRemoteConfigService _remoteConfigService;
        private PlayerProfileEnvelopeV1 _profile;
        private RuntimeConfigV1 _runtimeConfig;
        private BattleLaunchRequest _currentRequest;
        private BattleResult _currentResult;
        private IBattleSessionHost _activeBattleHost;
        private bool _compositionReady;
        private bool _startupRoutineActive;
        private string _blockingError = string.Empty;
        private ShellFlowError _lastRecoverableError;

        public IAppNavigator Navigator => _bootstrap == null ? null : _bootstrap.Navigator;
        public string BundledContentVersion { get; private set; } = string.Empty;
        public bool IsCompositionReady => _compositionReady;
        public string BlockingError => _blockingError;
        public ShellFlowError LastRecoverableError => _lastRecoverableError;
        public BattleLaunchRequest CurrentRequest => _currentRequest;
        public BattleResult CurrentResult => _currentResult;
        public PlayerProfileEnvelopeV1 CurrentProfile => _profile;
        public RuntimeConfigV1 CurrentRuntimeConfig => _runtimeConfig;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void FruitDefenseAcceptanceReady(int route);
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

            var bundledCatalog = BundledBattleContentFactory.Create();
            if (!BattleContentCompiler.TryCompile(bundledCatalog, out var compiled, out var validation))
            {
                _blockingError = BundledContentInvalid + ":" + validation.Issues[0].code;
                _startupRoutineActive = false;
                yield break;
            }
            BundledContentVersion = compiled.Header.contentVersion;
            if (!string.Equals(
                    BundledContentVersion,
                    _runtimeConfig.bundledContentVersion,
                    StringComparison.Ordinal))
            {
                _blockingError = BundledContentMismatch;
                _startupRoutineActive = false;
                yield break;
            }

            _compositionReady = true;
            _startupRoutineActive = false;

            if (ShouldEnterAcceptanceBattle())
            {
                TryStartDefaultBattle(
                    LobbyPresenter.DefaultLevelId,
                    "acceptance-" + Guid.NewGuid().ToString("N"),
                    20260714,
                    BundledContentVersion,
                    out _);
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
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = request;
            _currentResult = null;
            StartCoroutine(LoadBattle(request));
            error = ShellFlowError.None;
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

            var retry = new BattleLaunchRequest(
                Guid.NewGuid().ToString("N"),
                _currentRequest.LevelId,
                CreateNonzeroSeed(),
                _currentRequest.ContentVersion);
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = retry;
            _currentResult = null;
            _activeBattleHost = null;
            StartCoroutine(LoadBattle(retry));
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

        private IEnumerator LoadBattle(BattleLaunchRequest request)
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

                var initialization = host.Initialize(request, Navigator, this);
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
                presenter.Initialize(this);
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
            presenter.Initialize(this);
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

        public void ConfigureAcceptanceFlow(string command)
        {
            if (!IsAcceptanceLaunch() || string.IsNullOrWhiteSpace(command)) return;
            switch (command)
            {
                case "victory":
                    if (_activeBattleHost?.Simulation == null) return;
                    _activeBattleHost.Simulation.State.Phase = global::FruitDefense.Core.GamePhase.Victory;
                    _activeBattleHost.Simulation.State.WaveIndex = 15;
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
            if (IsAcceptanceLaunch()) FruitDefenseAcceptanceReady((int)route);
#endif
        }

        private static int CreateNonzeroSeed()
        {
            var seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & int.MaxValue;
            return seed == 0 ? 1 : seed;
        }

        private bool Fail(string code, out ShellFlowError error)
        {
            error = new ShellFlowError(code);
            ReportRecoverableError(error);
            return false;
        }

        private void OnGUI()
        {
            if (_compositionReady && string.IsNullOrEmpty(_blockingError))
            {
                if (!_lastRecoverableError.IsEmpty)
                    GUI.Label(new Rect(16f, Mathf.Max(8f, Screen.height - 44f), Screen.width - 32f, 32f),
                        "\u53ef\u6062\u590d\u9519\u8bef\uff1a" + _lastRecoverableError.Code);
                return;
            }

            var width = Mathf.Min(360f, Screen.width - 32f);
            var panel = new Rect((Screen.width - width) * .5f, Mathf.Max(24f, Screen.height * .3f), width, 190f);
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, panel.width - 40f, 54f),
                string.IsNullOrEmpty(_blockingError) ? "\u6b63\u5728\u542f\u52a8\u679c\u56ed\u9632\u7ebf" : "\u542f\u52a8\u5931\u8d25\uff1a" + _blockingError);

            if (!string.IsNullOrEmpty(_blockingError)
                && _bootstrap != null
                && _bootstrap.IsInitialized
                && !_bootstrap.InitializationResult.Success
                && GUI.Button(new Rect(panel.x + 20f, panel.y + 105f, panel.width - 40f, 52f), "\u91cd\u8bd5"))
            {
                if (_bootstrap.TryRetryInitialization())
                {
                    _blockingError = string.Empty;
                    BeginStartup();
                }
            }
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
