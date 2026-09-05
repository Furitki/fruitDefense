using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if FRUIT_DEFENSE_ACCEPTANCE
using System.Runtime.InteropServices;
#endif
using FruitDefense.App.Services;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Shell;
using FruitDefense.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using FruitDefense.Development.GmStress;
#endif

namespace FruitDefense.App
{
    public enum ProfileStartupDisposition
    {
        Interactive = 0,
        UnsupportedSchema = 1,
        Unavailable = 2,
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class AppFlowCoordinator : MonoBehaviour, IShellFlowContext,
        ILevelSelectionFlowContext, IHubProgressionReadContext,
        IHubProgressionCommandContext, IProfileRecoveryCommandContext,
        IBattleResultSink
    {
        public const string BootstrapScene = "Bootstrap";
        public const string LobbyScene = "Lobby";
        public const string BattleScene = "Battle";
        public const string SettlementScene = "Settlement";

        public const string FlowNotReady = "app-flow-not-ready";
        public const string SceneUnavailable = "app-scene-unavailable";
        public const string SceneLoadFailed = "app-scene-load-failed";
        public const string BattleHostMissing = "battle-host-missing";
        public const string LobbyHubPresenterMissing = "lobby-hub-presenter-missing";
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
        public const string ProfileLoadUnavailable = "local-profile-unavailable";
        public const string ProfileSchemaUnsupported = "local-profile-schema-unsupported";
        public const string ProfileResetFailed = "local-profile-reset-failed";
        public const string RuntimeUiThemeInvalid = "runtime-ui-theme-invalid";
        public const string BattleGrowthProjectionInvalid =
            "battle-growth-projection-invalid";

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
        private PlayerProgressionService _progressionService;
        private IRemoteConfigService _remoteConfigService;
        private PlayerProfile _profile;
        private RuntimeConfigV1 _runtimeConfig;
        private CompiledLevelCatalog _levelCatalog;
        private CompiledOutgameContentCatalog _outgameCatalog;
        private BattleGrowthResolution _currentGrowthPreview;
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
        private bool _hubCommandRoutineActive;
        private bool _profileRecoveryRoutineActive;
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
        internal PlayerProfile CurrentProfile => _profile;
        public RuntimeConfigV1 CurrentRuntimeConfig => _runtimeConfig;
        public CompiledLevelCatalog CurrentLevelCatalog => _levelCatalog;
        public CompiledOutgameContentCatalog CurrentOutgameCatalog => _outgameCatalog;
        public CompiledOutgameContentCatalog OutgameContent => _outgameCatalog;
        public PlayerProgressionProjection Progression => _progressionService?.Current;
        public bool ProgressionCommandInProgress => _hubCommandRoutineActive
            || _profileSaveRoutineActive
            || (_progressionService != null
                && _progressionService.CommandInProgress);
        public bool ProfileRecoveryRequired => HasErrorCode(_blockingError,
            ProfileSchemaUnsupported) || HasErrorCode(_blockingError,
            ProfileResetFailed);
        public bool ProfileRecoveryInProgress => _profileRecoveryRoutineActive;
        public BattleGrowthResolution CurrentGrowthPreview => _currentGrowthPreview;
        public ResolvedLevelDefinition CurrentResolvedLevel => _currentResolvedLevel;
        public IReadOnlyList<LevelDefinition> PlayableLevels => _levelCatalog == null
            ? Array.Empty<LevelDefinition>()
            : _levelCatalog.PlayableLevels;
        public string SelectedLevelId => _selectedLevelId;
        public RuntimeUiTheme RuntimeUiTheme => runtimeUiTheme;

#if FRUIT_DEFENSE_ACCEPTANCE && UNITY_WEBGL && !UNITY_EDITOR
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

            if (!BundledGameContentLoader.TryLoadBundle(out var contentBundle,
                    out var contentValidation))
            {
                if (contentValidation != null && !contentValidation.IsValid
                    && contentValidation.Issues.Count > 0)
                {
                    _blockingError = BundledContentInvalid + ":" + contentValidation.Issues[0].code;
                }
                else
                {
                    _blockingError = BundledContentInvalid + ":unknown";
                }
                _startupRoutineActive = false;
                yield break;
            }

            var levelSource = BundledLevelCatalogFactory.CreateSource();
            if (!LevelCatalogCompiler.TryCompile(levelSource, contentBundle.Battle,
                    out _levelCatalog, out var levelValidation))
            {
                var issue = levelValidation != null && levelValidation.Issues.Count > 0
                    ? levelValidation.Issues[0].Code
                    : "unknown";
                _blockingError = BundledLevelCatalogInvalid + ":" + issue;
                _startupRoutineActive = false;
                yield break;
            }
            _outgameCatalog = contentBundle.Outgame;

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

            _profileStore = LocalProfileStoreFactory.CreateDefault(_outgameCatalog);
            ProfileLoadResult profileResult = null;
            yield return _profileStore.Load(value => profileResult = value);
            var profileDisposition = ClassifyProfileLoad(profileResult);
            if (profileDisposition == ProfileStartupDisposition.UnsupportedSchema)
            {
                _blockingError = ProfileSchemaUnsupported + ":"
                    + (profileResult?.Error ?? string.Empty);
                _startupRoutineActive = false;
                yield break;
            }
            if (profileDisposition == ProfileStartupDisposition.Unavailable)
            {
                _blockingError = ProfileLoadUnavailable + ":"
                    + (profileResult?.Error ?? string.Empty);
                _startupRoutineActive = false;
                yield break;
            }

            _profile = profileResult.Profile;
            if (profileResult.Status == ProfileLoadStatus.StorageError)
                _lastRecoverableError = new ShellFlowError("local-profile-storage-degraded", profileResult.Error);

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

            ReplaceProgressionService(_profile);
            TryRefreshSelectedGrowthPreview(out _);
            _compositionReady = true;
            _startupRoutineActive = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldEnterGmStressBattle())
            {
                if (!TryStartGmStressBattle(
                        "gm-stress-" + Guid.NewGuid().ToString("N"),
                        20260826,
                        out var gmError))
                    _blockingError = gmError.Code + ":" + gmError.Detail;
                yield break;
            }
#endif

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

#if FRUIT_DEFENSE_ACCEPTANCE
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
#endif

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

            if (!string.Equals(contentVersion, BundledContentVersion, StringComparison.Ordinal))
                return Fail(BundledContentMismatch, out error);
            if (!TryResolveLevel(levelId, out var resolvedLevel, out error))
                return false;
            if (!TryResolveBattleGrowth(resolvedLevel, out var growth))
                return Fail(BattleGrowthProjectionInvalid,
                    growth.Code + ":" + growth.Path, out error);
            _currentGrowthPreview = growth;
            var request = new BattleLaunchRequest(sessionId, levelId, seed, contentVersion,
                BattleSessionMode.Standard, growth.Snapshot);
            if (!request.TryValidate(out var requestError))
                return Fail(requestError, out error);
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = request;
            _currentResolvedLevel = resolvedLevel;
            _currentResult = null;
            StartCoroutine(LoadBattle(request));
            error = ShellFlowError.None;
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool TryStartGmStressBattle(string sessionId, int seed,
            out ShellFlowError error)
        {
            if (!_compositionReady || Navigator == null)
                return Fail(FlowNotReady, out error);
            if (_currentRequest != null || _activeBattleHost != null)
                return Fail(BattleRequestActive, out error);

            var request = new BattleLaunchRequest(sessionId,
                GmStressBattleIds.LevelId, seed, BundledContentVersion,
                BattleSessionMode.GmStress, null);
            if (!request.TryValidate(out var requestError))
                return Fail(requestError, out error);
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = request;
            _currentResolvedLevel = null;
            _currentResult = null;
            StartCoroutine(LoadGmStressBattle(request));
            error = ShellFlowError.None;
            return true;
        }
#endif

        public bool TrySelectLevel(string levelId, out ShellFlowError error)
        {
            if (!_compositionReady || _levelCatalog == null)
                return Fail(FlowNotReady, out error);
            if (!TryResolveLevel(levelId, out var resolvedLevel, out error))
                return false;

            _selectedLevelId = resolvedLevel.Identity.LevelId;
            TryResolveBattleGrowth(resolvedLevel, out _currentGrowthPreview);
            if (_profile != null
                && !string.Equals(_profile.lastSelectedLevelId, _selectedLevelId,
                    StringComparison.Ordinal))
            {
                _profile.lastSelectedLevelId = _selectedLevelId;
                QueueProfileSelectionSave();
            }

            error = ShellFlowError.None;
#if FRUIT_DEFENSE_ACCEPTANCE
            if (Navigator != null
                && Navigator.CurrentRoute == AppRoute.Lobby
                && Navigator.TransitionState == AppTransitionState.Idle)
            {
                SignalAcceptanceRouteReady(AppRoute.Lobby);
            }
#endif
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

            var retry = BattleLaunchRequest.CreateRetry(_currentRequest,
                Guid.NewGuid().ToString("N"), retrySeed);
            var retryGrowthValidation = BattleGrowthSnapshotValidator.ValidateForLaunch(
                retry.GrowthSnapshot, retryLevel, _outgameCatalog);
            if (!retryGrowthValidation.Succeeded)
                return Fail(BattleGrowthProjectionInvalid,
                    retryGrowthValidation.Code + ":" + retryGrowthValidation.Path,
                    out error);
            if (!Navigator.TryBeginTransition(AppRoute.Battle, out var navigationError))
                return Fail(navigationError, out error);

            _currentRequest = retry;
            _currentResolvedLevel = retryLevel;
            _currentResult = null;
            _activeBattleHost = null;
            StartCoroutine(LoadBattle(retry));
            error = ShellFlowError.None;
            return true;
        }

        public bool TryRefreshSelectedGrowthPreview(
            out BattleGrowthResolution preview)
        {
            if (_levelCatalog == null || string.IsNullOrEmpty(_selectedLevelId)
                || !_levelCatalog.TryResolve(_selectedLevelId,
                    out var resolvedLevel, out _))
            {
                preview = default;
                _currentGrowthPreview = preview;
                return false;
            }
            var success = TryResolveBattleGrowth(resolvedLevel, out preview);
            _currentGrowthPreview = preview;
            return success;
        }

        private bool TryResolveBattleGrowth(ResolvedLevelDefinition resolvedLevel,
            out BattleGrowthResolution resolution)
        {
            if (_outgameCatalog == null || _progressionService == null
                || resolvedLevel == null)
            {
                resolution = default;
                return false;
            }
            try
            {
                resolution = BattleGrowthResolver.Resolve(_outgameCatalog,
                    resolvedLevel, _progressionService.Current);
                return resolution.Succeeded;
            }
            catch (Exception exception)
            {
                resolution = BattleGrowthResolution.Fail(
                    BattleGrowthResolveCode.ProfileRequired, "profile",
                    exception.Message);
                return false;
            }
        }

        public IEnumerator TryClaimActivity(string activityId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return RunProgressionCommand(
                PlayerProgressionCommandKind.ClaimActivity, activityId,
                string.Empty, completed);
        }

        public IEnumerator TryEquipGrowthEquipment(string growthEquipmentId,
            string slotId, Action<PlayerProgressionCommandResult> completed)
        {
            return RunProgressionCommand(
                PlayerProgressionCommandKind.EquipGrowthEquipment,
                growthEquipmentId, slotId, completed);
        }

        public IEnumerator TryUpgradeGrowthEquipment(string growthEquipmentId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return RunProgressionCommand(
                PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                growthEquipmentId, string.Empty, completed);
        }

        public IEnumerator TryUpgradeCultivation(string cultivationNodeId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return RunProgressionCommand(
                PlayerProgressionCommandKind.UpgradeCultivation,
                cultivationNodeId, string.Empty, completed);
        }

        private IEnumerator RunProgressionCommand(
            PlayerProgressionCommandKind kind, string identity,
            string secondaryIdentity,
            Action<PlayerProgressionCommandResult> completed)
        {
            if (_progressionService == null)
            {
                completed?.Invoke(new PlayerProgressionCommandResult(kind,
                    PlayerProgressionCommandStatus.InvalidProfile, identity,
                    Progression, message: "Player progression is unavailable."));
                yield break;
            }
            if (_hubCommandRoutineActive || _profileSaveRoutineActive)
            {
                completed?.Invoke(new PlayerProgressionCommandResult(kind,
                    PlayerProgressionCommandStatus.InProgress, identity,
                    Progression,
                    message: "Another profile command is persisting."));
                yield break;
            }

            _hubCommandRoutineActive = true;
            PlayerProgressionCommandResult result = null;
            IEnumerator routine;
            switch (kind)
            {
                case PlayerProgressionCommandKind.ClaimActivity:
                    routine = _progressionService.TryClaimActivity(identity,
                        value => result = value);
                    break;
                case PlayerProgressionCommandKind.EquipGrowthEquipment:
                    routine = _progressionService.TryEquip(identity,
                        secondaryIdentity, value => result = value);
                    break;
                case PlayerProgressionCommandKind.UpgradeGrowthEquipment:
                    routine = _progressionService.TryUpgradeGrowthEquipment(
                        identity, value => result = value);
                    break;
                case PlayerProgressionCommandKind.UpgradeCultivation:
                    routine = _progressionService.TryUpgradeCultivation(identity,
                        value => result = value);
                    break;
                default:
                    result = new PlayerProgressionCommandResult(kind,
                        PlayerProgressionCommandStatus.InvalidRequest, identity,
                        Progression, message: "Unsupported Hub command.");
                    routine = null;
                    break;
            }

            try
            {
                if (routine != null) yield return routine;
                if (result != null && result.Succeeded)
                {
                    _profile = _progressionService
                        .CreateCommittedProfileSnapshot();
                    TryRefreshSelectedGrowthPreview(out _);
                }
            }
            finally
            {
                _hubCommandRoutineActive = false;
            }
            completed?.Invoke(result ?? new PlayerProgressionCommandResult(kind,
                PlayerProgressionCommandStatus.PersistenceFailed, identity,
                Progression, message: "Profile command did not complete."));
        }

        private void ReplaceProgressionService(PlayerProfile profile)
        {
            _progressionService = new PlayerProgressionService(_profileStore,
                _outgameCatalog, profile);
        }

        public static ProfileStartupDisposition ClassifyProfileLoad(
            ProfileLoadResult result)
        {
            if (result != null
                && result.Status == ProfileLoadStatus.UnsupportedSchema)
                return ProfileStartupDisposition.UnsupportedSchema;
            return result != null && result.HasProfile
                ? ProfileStartupDisposition.Interactive
                : ProfileStartupDisposition.Unavailable;
        }

        public bool TryResetUnsupportedProfile(out ShellFlowError error)
        {
            if (!ProfileRecoveryRequired || _profileStore == null
                || _profileRecoveryRoutineActive)
            {
                error = new ShellFlowError(ProfileResetFailed,
                    "Profile reset is not currently available.");
                return false;
            }

            _profileRecoveryRoutineActive = true;
            StartCoroutine(ResetUnsupportedProfile());
            error = ShellFlowError.None;
            return true;
        }

        private IEnumerator ResetUnsupportedProfile()
        {
            ProfileLoadResult reset = null;
            try
            {
                yield return _profileStore.Reset(value => reset = value);
            }
            finally
            {
                _profileRecoveryRoutineActive = false;
            }
            if (reset == null || reset.Status != ProfileLoadStatus.ResetCreated
                || !reset.HasProfile)
            {
                _blockingError = ProfileResetFailed + ":"
                    + (reset?.Error ?? "Profile reset did not complete.");
                yield break;
            }

            _blockingError = string.Empty;
            _lastRecoverableError = ShellFlowError.None;
            BeginStartup();
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
                if (BindLobbyHubPresenter())
                {
#if FRUIT_DEFENSE_ACCEPTANCE
                    SignalAcceptanceRouteReady(AppRoute.Lobby);
#endif
                }
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
                if (BindLobbyHubPresenter())
                {
#if FRUIT_DEFENSE_ACCEPTANCE
                    SignalAcceptanceRouteReady(AppRoute.Lobby);
#endif
                }
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

                var initialization = host.Initialize(
                    request, Navigator, this, runtimeUiTheme, _levelCatalog,
                    _outgameCatalog);
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
#if FRUIT_DEFENSE_ACCEPTANCE
                SignalAcceptanceRouteReady(AppRoute.Battle);
#endif
            });
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private IEnumerator LoadGmStressBattle(BattleLaunchRequest request)
        {
            yield return LoadScene(BattleScene, result =>
            {
                if (!result.Success)
                {
                    RecoverAfterRouteFailure(result.ErrorCode);
                    return;
                }

                var releaseHost = FindFirstObjectByType<FruitDefenseGame>();
                var terrainPalette = releaseHost == null
                    ? null
                    : releaseHost.BattlefieldTerrainPalettes.FirstOrDefault(value =>
                        value != null && string.Equals(value.PaletteId,
                            GmStressBattleIds.TerrainPaletteId,
                            StringComparison.Ordinal));
                if (releaseHost != null) releaseHost.enabled = false;
                var root = new GameObject("GM Stress Battle Host");
                var host = root.AddComponent<GmStressBattlePresenter>();
                var initialization = host.InitializeGm(
                    request, Navigator, this, runtimeUiTheme, terrainPalette);
                if (!initialization.Success)
                {
                    Destroy(root);
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
            });
        }

        private bool ShouldEnterGmStressBattle()
        {
            if (GmStressBattleLaunchRequest.TryConsumeEditorOneShot()) return true;
            var launch = _bootstrap?.PlatformAdapter?.LaunchContext;
            if (launch == null
                || !launch.TryGetQuery(GmStressBattleLaunchRequest.QueryKey,
                    out var value)) return false;
            return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }
#endif

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
#if FRUIT_DEFENSE_ACCEPTANCE
                SignalAcceptanceRouteReady(AppRoute.Settlement);
#endif
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

        private bool BindLobbyHubPresenter()
        {
            var presenter = FindFirstObjectByType<LobbyHubPresenter>();
            if (presenter == null)
            {
                _blockingError = LobbyHubPresenterMissing;
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

#if FRUIT_DEFENSE_ACCEPTANCE
        private bool ShouldEnterAcceptanceBattle()
        {
            var launch = _bootstrap.PlatformAdapter?.LaunchContext;
            return launch != null
                && AcceptanceLaunchQuery.IsEnabled(launch.LaunchUrl)
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
            if (!IsAcceptanceLaunch()) return;
            if (string.IsNullOrWhiteSpace(command))
            {
                Debug.LogError(AcceptanceCommandResult.TerminalFixtureUnknown);
                return;
            }
            AcceptanceTerminalFixture fixture;
            switch (command)
            {
                case "victory":
                    fixture = AcceptanceTerminalFixture.Victory;
                    break;
                case "defeat":
                    fixture = AcceptanceTerminalFixture.Defeat;
                    break;
                default:
                    Debug.LogError(AcceptanceCommandResult.TerminalFixtureUnknown
                        + ":" + command);
                    return;
            }

            var acceptancePort = _activeBattleHost as IAcceptanceBattlePort;
            if (acceptancePort == null)
            {
                Debug.LogError(AcceptanceCommandResult.SessionUnavailable);
                return;
            }
            var result = acceptancePort.TryConfigureTerminalFixture(fixture);
            if (!result.Succeeded) Debug.LogError(result.ErrorCode);
        }

        private bool IsAcceptanceLaunch()
        {
            var launch = _bootstrap?.PlatformAdapter?.LaunchContext;
            return launch != null
                && AcceptanceLaunchQuery.IsEnabled(launch.LaunchUrl);
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
#endif

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
                while (_hubCommandRoutineActive) yield return null;
                _profileSavePending = false;
                var selectedAtSave = _selectedLevelId;
                var profileToSave = PlayerProfileCodec.Clone(_profile,
                    _outgameCatalog);
                profileToSave.lastSelectedLevelId = selectedAtSave;

                ProfileSaveResult saveResult = null;
                yield return _profileStore.Save(profileToSave, value => saveResult = value);
                if (saveResult != null && saveResult.Status == ProfileSaveStatus.Success)
                {
                    _profile = saveResult.Profile;
                    ReplaceProgressionService(_profile);
                    TryRefreshSelectedGrowthPreview(out _);
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

        private bool Fail(string code, string detail, out ShellFlowError error)
        {
            error = new ShellFlowError(code, detail);
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
                contentWidth, 60f * scale);
            var retryAction = hasRetryAction
                ? new Rect(contentX, modal.y + 124f * scale,
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

            if (HasErrorCode(rawError, ProfileSchemaUnsupported)
                || HasErrorCode(rawError, ProfileResetFailed))
            {
                return RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BootstrapProfileUnsupported).Text;
            }

            if (HasErrorCode(rawError, SceneUnavailable)
                || HasErrorCode(rawError, SceneLoadFailed)
                || HasErrorCode(rawError, BattleHostMissing)
                || HasErrorCode(rawError, LobbyHubPresenterMissing)
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
            var hasProfileRecovery = hasBlockingError && ProfileRecoveryRequired;
            var hasPlatformRetry = hasBlockingError
                && _bootstrap != null
                && _bootstrap.IsInitialized
                && !_bootstrap.InitializationResult.Success;
            var hasRetryAction = hasProfileRecovery || hasPlatformRetry;
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

            var presentationState = _profileRecoveryRoutineActive
                ? RuntimeUiInteractionState.Loading
                : hasBlockingError ? RuntimeUiInteractionState.Error
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
                var retryState = _profileRecoveryRoutineActive
                    ? RuntimeUiInteractionState.Loading
                    : retryPressed
                    ? RuntimeUiInteractionState.Pressed
                    : retryHovered || _retryFocusPulse.IsActive(unscaledTime)
                        ? RuntimeUiInteractionState.HoveredOrFocused
                        : RuntimeUiInteractionState.Normal;
                var retryCopy = RuntimeUiCopyCatalog.Get(hasProfileRecovery
                    ? _profileRecoveryRoutineActive
                        ? RuntimeUiCopyId.BootstrapProfileResetting
                        : RuntimeUiCopyId.BootstrapProfileReset
                    : RuntimeUiCopyId.BootstrapRetry);
                if (!RuntimeUiGui.DrawAction(_runtimeUiDrawContext,
                    layout.RetryAction,
                    retryCopy.Text,
                    new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                        RuntimeUiActionContentForm.IconLabel,
                        RuntimeUiActionBehavior.Instantaneous),
                    retryState,
                    RuntimeUiArtSlot.IconControlRetry))
                    return;

                _retryPressPulse = RuntimeUiFeedbackPulse.Begin(unscaledTime,
                    runtimeUiTheme.Feedback.UnscaledPressSeconds);
                if (hasProfileRecovery)
                {
                    TryResetUnsupportedProfile(out _);
                }
                else if (_bootstrap.TryRetryInitialization())
                {
                    _blockingError = string.Empty;
                    _bootstrapTransitionPulse = RuntimeUiFeedbackPulse.Begin(
                        unscaledTime,
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
