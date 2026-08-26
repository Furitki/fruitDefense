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
    public sealed partial class FruitDefenseGame : MonoBehaviour, IBattleSessionHost
#if FRUIT_DEFENSE_ACCEPTANCE
        , IAcceptanceBattlePort
#endif
    {
        public const string SessionNotInitialized = "battle-session-not-initialized";
        public const string ResultAlreadySubmitted = "battle-result-already-submitted";
        public const string FeedbackCatalogInvalid = "battle-feedback-catalog-invalid";
        public const string CombatSdfOverlayInvalid = "battle-combat-sdf-overlay-invalid";

        private enum DragPayloadType { Plant, Equipment, Pot }
        private enum DropTargetType { None, Pot, Nursery, Plant, Expansion }
        private enum TempSprite
        {
            Pea, Watermelon, Banana, Durian,
            Sunflower, Zombie, Runner, Armored,
            Boss, Gatling, Ice, Chili,
            EmptyPot, OccupiedPot, ExpansionPot, LockedPot,
        }
        private enum CombatSprite
        {
            PeaProjectile, WatermelonProjectile, BananaProjectile, DurianDrop,
            PeaImpact, WatermelonBlast, DurianShockwave, SunBurst,
            GatlingMuzzle, IceImpact, FrozenAura, ChiliImpact,
            Burning, HitSpark, ShockwaveRing, SunCollectible,
        }

        private sealed class DragSession
        {
            public DragPayloadType Type;
            public int PlantId = -1;
            public string EquipmentId = string.Empty;
            public Vector2 Start;
            public Vector2 Current;
            public bool Active;
        }

        private sealed class RestartPresentationState
        {
            public int InspectedPlantId = -1;
            public string SelectedEquipmentId = string.Empty;
            public bool PotToolSelected;
            public string Status = DefaultStatus;
            public RuntimeUiInteractionState StatusState = RuntimeUiInteractionState.Normal;
            public RuntimeUiFeedbackPulse StatusPulse;
            public DragSession Drag;
            public int DragControlId;
            public int ReturnPulsePlantId = -1;
            public RuntimeUiFeedbackPulse ReturnPulse;
            public RuntimeUiFeedbackPulse NurseryRollDisplayPulse;
            public int SelectionPulseTarget;
            public RuntimeUiFeedbackPulse SelectionPulse;

            public bool IsClean()
            {
                return InspectedPlantId == -1
                    && string.IsNullOrEmpty(SelectedEquipmentId)
                    && !PotToolSelected
                    && Status == DefaultStatus
                    && StatusState == RuntimeUiInteractionState.Normal
                    && !StatusPulse.IsScheduled
                    && Drag == null
                    && DragControlId == 0
                    && ReturnPulsePlantId == -1
                    && !ReturnPulse.IsScheduled
                    && !NurseryRollDisplayPulse.IsScheduled
                    && SelectionPulseTarget == 0
                    && !SelectionPulse.IsScheduled;
            }
        }


        private struct DropTarget
        {
            public DropTargetType Type;
            public int Id;
            public int Slot;
            public Vector2Int Cell;
            public Rect Rect;
        }

        private static string DefaultStatus => RuntimeUiCopyCatalog.Get(
            RuntimeUiCopyId.BattleDefaultGuidance).Text;
        public static int AttackRangeTextureSize => 1024;

        private GameSimulation _game;
        private readonly BattlePresentationBuffer _presentation = new BattlePresentationBuffer();
        private readonly BattleRenderInterpolationSamples _renderSamples =
            new BattleRenderInterpolationSamples();
        private CombatFloatingTextSdfOverlay _floatingTextOverlay;
        private BattleLaunchRequest _currentRequest;
        private IAppNavigator _navigator;
        private IBattleResultSink _resultSink;
        private AppBootstrap _appBootstrap;
        private RuntimeUiTheme _runtimeUiTheme;
        private RuntimeUiDrawContext _runtimeUiDrawContext;
        private bool _hasInitialized;
        private bool _isInitialized;
        private bool _hasEnteredBattleRoute;
        private bool _resultSubmitted;
        private bool _sessionDisposed;
        private Texture2D _tempArtAtlas;
        private Texture2D _combatVfxAtlas;
        private Texture2D _attackRangeTexture;
        [SerializeField] private BattlefieldTerrainPalette[] battlefieldTerrainPalettes =
            Array.Empty<BattlefieldTerrainPalette>();
        private BattleUiLayout _battleUiLayout;
        private GUIStyle _worldLabelStyle;
        private GUIStyle _terrainFailureStyle;
        private int _inspectedPlantId = -1;
        private string _selectedEquipmentId = string.Empty;
        private bool _potToolSelected;
        private string _status = DefaultStatus;
        private RuntimeUiInteractionState _statusState = RuntimeUiInteractionState.Normal;
        private RuntimeUiFeedbackPulse _statusPulse;
        private string _preparedStatusSource = string.Empty;
        private float _preparedStatusWidth = -1f;
        private RuntimeUiStatusTextMode _preparedStatusTextMode =
            RuntimeUiStatusTextMode.SingleLine;
        private RuntimeUiStatusTextLines _preparedStatusTextLines;
        private DragSession _drag;
        private int _dragControlId;
        private int _returnPulsePlantId = -1;
        private RuntimeUiFeedbackPulse _returnPulse;
        private RuntimeUiFeedbackPulse _nurseryRollDisplayPulse;
        private int _selectionPulseTarget;
        private RuntimeUiFeedbackPulse _selectionPulse;
        private int _actionPressTarget;
        private RuntimeUiFeedbackPulse _actionPressPulse;
        private RuntimeUiCompactControlState _pauseCompactControlState;
        private RuntimeUiCompactControlState _speedCompactControlState;
        private RuntimeUiPressTracker _actionPressTracker;
        private int _observedSun;
        private int _observedLives;
        private int _observedWave;
        private RuntimeUiFeedbackPulse _sunPulse;
        private RuntimeUiFeedbackPulse _livesPulse;
        private RuntimeUiFeedbackPulse _wavePulse;
        private string _terrainPresentationError = string.Empty;
        private string _lastLoggedTerrainPresentationError = string.Empty;

        private const int PauseActionFeedbackTarget = 3101;
        private const int SpeedActionFeedbackTarget = 3102;
        private const int WaveActionFeedbackTarget = 3103;
        private const int RefreshActionFeedbackTarget = 3104;
        private const int ModalPrimaryFeedbackTarget = 3105;
        private const int ModalSecondaryFeedbackTarget = 3106;
        private const int DetailCloseFeedbackTarget = 3107;

        public BattleSessionStatus Status
        {
            get
            {
                return !_isInitialized || _game == null
                    ? BattleSessionStatus.Uninitialized
                    : new BattleSessionStatus(true, _game.State.Phase,
                        _game.State.WaveIndex, _game.State.Lives,
                        _game.State.Paused, _resultSubmitted);
            }
        }
        public CombatFloatingTextSdfOverlay FloatingTextOverlay
        {
            get { return _floatingTextOverlay; }
        }
        public IReadOnlyList<BattlefieldTerrainPalette> BattlefieldTerrainPalettes
        {
            get { return battlefieldTerrainPalettes ?? Array.Empty<BattlefieldTerrainPalette>(); }
        }
        public DualGridTileSet BattlefieldGrassTileSet { get { return DefaultTerrainTileSet(BattlefieldLayerIds.Surfaces.Grass); } }
        public DualGridTileSet BattlefieldRouteTileSet { get { return DefaultTerrainTileSet(BattlefieldLayerIds.Surfaces.StoneRoad); } }
        public Texture2D BattlefieldSoilBaseTexture
        {
            get { return BattlefieldTerrainPalettes.Count == 0 ? null : BattlefieldTerrainPalettes[0].SoilBaseTexture; }
        }
        private string _lastResultSubmissionError = string.Empty;
        public string TerrainPresentationError { get { return _terrainPresentationError; } }
        public bool IsTerrainPresentationAvailable
        {
            get { return string.IsNullOrEmpty(_terrainPresentationError); }
        }
        public static int ActiveSessionHostCount { get; private set; }


        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            _tempArtAtlas = Resources.Load<Texture2D>("TempArt/fruit-defense-temp-atlas");
            _combatVfxAtlas = Resources.Load<Texture2D>("TempArt/combat-vfx-atlas");
            _attackRangeTexture = CreateAttackRangeTexture();
            if (_tempArtAtlas != null)
            {
                _tempArtAtlas.filterMode = FilterMode.Bilinear;
                _tempArtAtlas.wrapMode = TextureWrapMode.Clamp;
            }
            if (_combatVfxAtlas != null)
            {
                _combatVfxAtlas.filterMode = FilterMode.Bilinear;
                _combatVfxAtlas.wrapMode = TextureWrapMode.Clamp;
            }
            Application.targetFrameRate = 60;
        }

        private void OnDestroy()
        {
            DisposeSession();
            if (_attackRangeTexture != null) Destroy(_attackRangeTexture);
            _attackRangeTexture = null;
            _tempArtAtlas = null;
            _combatVfxAtlas = null;
            _worldLabelStyle = null;
            _terrainFailureStyle = null;
        }

        public BattleSessionInitializationResult Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            RuntimeUiTheme runtimeUiTheme,
            CompiledLevelCatalog levelCatalog)
        {
            if (!TryValidateInitialization(
                    request, navigator, resultSink, runtimeUiTheme, out var failure)) return failure;
            if (levelCatalog == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.LevelCatalogRequired);
            var resolution = levelCatalog.Resolve(request.LevelId);
            if (!resolution.Succeeded || resolution.Value == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.LevelResolutionFailed);
            var resolvedLevel = resolution.Value;
            if (resolvedLevel.BattleContent == null
                || !string.Equals(request.ContentVersion,
                    resolvedLevel.BattleContent.Header.contentVersion, StringComparison.Ordinal))
            {
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.ContentVersionMismatch);
            }

            GameSimulation simulation;
            try
            {
                simulation = new GameSimulation(
                    levelCatalog, request.LevelId, request.Seed);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.SimulationConstructionFailed);
            }

            return CompleteInitialization(
                request, navigator, resultSink, runtimeUiTheme, simulation);
        }

        private bool TryValidateInitialization(BattleLaunchRequest request,
            IAppNavigator navigator, IBattleResultSink resultSink, RuntimeUiTheme runtimeUiTheme,
            out BattleSessionInitializationResult failure)
        {
            if (_hasInitialized)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.AlreadyInitialized);
                return false;
            }
            if (request == null)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.InvalidRequest);
                return false;
            }
            if (!request.TryValidate(out var requestError))
            {
                failure = BattleSessionInitializationResult.Failed(requestError);
                return false;
            }
            if (request.Mode != BattleSessionMode.Standard)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.SessionModeMismatch);
                return false;
            }
            if (navigator == null)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.NavigatorRequired);
                return false;
            }
            if (resultSink == null)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.ResultSinkRequired);
                return false;
            }
            if (runtimeUiTheme == null)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.RuntimeUiThemeRequired);
                return false;
            }
            var themeValidation = runtimeUiTheme.Validate();
            if (!themeValidation.IsValid)
            {
                failure = BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.RuntimeUiThemeInvalid + ":"
                    + themeValidation.Issues[0].Code);
                return false;
            }

            failure = default;
            return true;
        }

        private BattleSessionInitializationResult CompleteInitialization(BattleLaunchRequest request,
            IAppNavigator navigator, IBattleResultSink resultSink, RuntimeUiTheme runtimeUiTheme,
            GameSimulation simulation)
        {
            string feedbackCatalogError;
            if (!ValidateCombatFeedbackCatalog(
                    _presentation.Catalog, simulation.Content, out feedbackCatalogError))
            {
                Debug.LogError(FeedbackCatalogInvalid + ":" + feedbackCatalogError);
                return BattleSessionInitializationResult.Failed(
                    FeedbackCatalogInvalid + ":" + feedbackCatalogError);
            }

            string sdfOverlayError;
            CombatFloatingTextSdfOverlay sdfOverlay;
            if (!CombatFloatingTextSdfOverlay.TryCreate(
                    transform, out sdfOverlay, out sdfOverlayError))
            {
                Debug.LogError(CombatSdfOverlayInvalid + ":" + sdfOverlayError);
                return BattleSessionInitializationResult.Failed(
                    CombatSdfOverlayInvalid + ":" + sdfOverlayError);
            }
            _floatingTextOverlay = sdfOverlay;

            _hasInitialized = true;
            _isInitialized = true;
            _sessionDisposed = false;
#if FRUIT_DEFENSE_ACCEPTANCE
            _acceptanceTerminalPreview = false;
#endif
            _currentRequest = request;
            _navigator = navigator;
            _resultSink = resultSink;
            _runtimeUiTheme = runtimeUiTheme;
            BuildWorldRenderingStyles(runtimeUiTheme.PackagedChineseFont);
            _game = simulation;
            _observedSun = simulation.State.Sun;
            _observedLives = simulation.State.Lives;
            _observedWave = simulation.State.WaveIndex;
            _sunPulse = default;
            _livesPulse = default;
            _wavePulse = default;
            _actionPressTarget = 0;
            _actionPressPulse = default;
            _actionPressTracker.Cancel();
            RebindCompactControlPresentation();
            RefreshTerrainPresentationStatus();
            _presentation.Clear();
            _renderSamples.SnapTo(_game.State);
            _battleUiLayout = new BattleUiLayout(_game.Map);
            _hasEnteredBattleRoute = navigator.CurrentRoute == AppRoute.Battle;
            _navigator.RouteChanged += OnAppRouteChanged;

            _appBootstrap = AppBootstrap.Instance;
            if (_appBootstrap != null)
            {
                _appBootstrap.VisibilityChanged += HandlePlatformVisibility;
                HandlePlatformVisibility(_appBootstrap.CurrentVisibility);
            }

            ActiveSessionHostCount++;
            return BattleSessionInitializationResult.Succeeded();
        }

        public void HandlePlatformVisibility(PlatformVisibility visibility)
        {
            if (!_isInitialized || _game == null) return;
            if (visibility != PlatformVisibility.Background) return;

            _game.State.Paused = true;
            _game.ResetFrameAccumulator();
            CancelTransientInteraction();
        }

        public bool RestartCurrentSession(out string errorCode)
        {
            if (!_isInitialized || _game == null || _currentRequest == null)
            {
                errorCode = SessionNotInitialized;
                return false;
            }
            if (_resultSubmitted)
            {
                errorCode = ResultAlreadySubmitted;
                return false;
            }

#if FRUIT_DEFENSE_ACCEPTANCE
            _acceptanceTerminalPreview = false;
#endif
            var presentation = CaptureRestartPresentation();
            ResetFullRun(_game, presentation, _currentRequest.Seed);
            _presentation.Clear();
            _renderSamples.SnapTo(_game.State);
            ApplyRestartPresentation(presentation);
            RebindCompactControlPresentation();
            errorCode = string.Empty;
            return true;
        }

        public BattleSnapshotExportResult ExportCurrentSessionSnapshot()
        {
            if (!_isInitialized || _game == null)
                return BattleSnapshotExportResult.Unsupported(SessionNotInitialized);
            return _game.ExportSnapshot();
        }

        public BattleSnapshotRestoreResult RestoreCurrentSessionSnapshot(
            BattleSnapshot snapshot, CompiledLevelCatalog levelCatalog)
        {
            if (!_isInitialized || _game == null)
                return new BattleSnapshotRestoreResult(BattleSnapshotRestoreCode.InvalidPayload,
                    "session", SessionNotInitialized);

            var result = _game.RestoreSnapshot(snapshot, levelCatalog);
            if (!result.Succeeded) return result;

            _presentation.Clear();
            _renderSamples.SnapTo(_game.State);
            CancelTransientInteraction();
            RebindCompactControlPresentation();
            return result;
        }

        public bool TrySubmitTerminalResult()
        {
            if (!_isInitialized || _game == null || _currentRequest == null || _resultSubmitted)
                return false;
#if FRUIT_DEFENSE_ACCEPTANCE
            if (_acceptanceTerminalPreview) return false;
#endif

            var phase = _game.State.Phase;
            if (phase != GamePhase.Victory && phase != GamePhase.Defeat) return false;

            _resultSubmitted = true;
            var result = new BattleResult(
                _currentRequest.SessionId,
                _currentRequest.LevelId,
                _currentRequest.Seed,
                phase == GamePhase.Victory ? BattleOutcome.Victory : BattleOutcome.Defeat,
                _game.State.WaveIndex,
                _game.State.Lives);
            bool accepted;
            string errorCode;
            try
            {
                accepted = _resultSink.TrySubmitResult(result, out errorCode);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                accepted = false;
                errorCode = "battle-result-sink-threw";
            }

            if (!accepted)
            {
                _lastResultSubmissionError = string.IsNullOrWhiteSpace(errorCode)
                    ? "battle-result-submission-failed"
                    : errorCode;
                Debug.LogWarning("Battle result submission failed: "
                    + _lastResultSubmissionError);
            }
            else
            {
                _lastResultSubmissionError = string.Empty;
            }
            return true;
        }

        private void OnApplicationPause(bool paused)
        {
            if (_appBootstrap == null)
                HandlePlatformVisibility(paused ? PlatformVisibility.Background : PlatformVisibility.Foreground);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_appBootstrap == null)
                HandlePlatformVisibility(hasFocus ? PlatformVisibility.Foreground : PlatformVisibility.Background);
        }

        private void OnAppRouteChanged(AppRoute route)
        {
            if (route == AppRoute.Battle)
            {
                _hasEnteredBattleRoute = true;
                return;
            }

            if (_hasEnteredBattleRoute)
                Destroy(gameObject);
        }

        public void DisposeSession()
        {
            if (_sessionDisposed) return;
            _sessionDisposed = true;

            if (_navigator != null)
                _navigator.RouteChanged -= OnAppRouteChanged;
            if (_appBootstrap != null)
                _appBootstrap.VisibilityChanged -= HandlePlatformVisibility;
            if (_isInitialized && ActiveSessionHostCount > 0)
                ActiveSessionHostCount--;

            _isInitialized = false;
#if FRUIT_DEFENSE_ACCEPTANCE
            _acceptanceTerminalPreview = false;
#endif
            _navigator = null;
            _resultSink = null;
            _runtimeUiTheme = null;
            _runtimeUiDrawContext = null;
            _appBootstrap = null;
            _currentRequest = null;
            if (_floatingTextOverlay != null)
            {
                _floatingTextOverlay.Dispose();
                _floatingTextOverlay = null;
            }
            _game = null;
            _battleUiLayout = null;
            _presentation.Clear();
            CancelTransientInteraction();
            ResetInteractionState();
        }

        private void CancelTransientInteraction()
        {
            if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
            _drag = null;
            _dragControlId = 0;
            _selectedEquipmentId = string.Empty;
            _potToolSelected = false;
        }

        private void BuildWorldRenderingStyles(Font packagedChineseFont)
        {
            _worldLabelStyle = new GUIStyle
            {
                font = packagedChineseFont,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
            };
            _terrainFailureStyle = new GUIStyle
            {
                font = packagedChineseFont,
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true,
                normal = { textColor = new Color(1f, .82f, .72f, 1f) },
            };
        }

        private void Update()
        {
            if (!_isInitialized || _game == null) return;
            if (Input.GetKeyDown(KeyCode.Space)) _game.TogglePause();
            if (Input.GetKeyDown(KeyCode.Alpha1)) _game.SetSpeed(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _game.SetSpeed(2);
            var completedSteps = _game.AdvanceFrame(Time.unscaledDeltaTime);
#if FRUIT_DEFENSE_ACCEPTANCE
            if (!_acceptanceFeedbackFrozen)
#endif
                _presentation.Advance(Time.unscaledDeltaTime,
                    _game.State.Paused, _game.State.Speed);
            _renderSamples.Capture(_game, completedSteps);
            _presentation.Consume(_game);
            _presentation.RoutePendingAudio(SilentCombatAudioRouter.Instance);
            ResolveFloatingTextFollowAnchors();
            SyncFloatingTextOverlay();
#if FRUIT_DEFENSE_ACCEPTANCE
            PublishCombatFeedbackAcceptanceTelemetry();
#endif
            TrySubmitTerminalResult();
            if (_inspectedPlantId >= 0 && _game.PlantById(_inspectedPlantId) == null) _inspectedPlantId = -1;
        }

        private void ResolveFloatingTextFollowAnchors()
        {
            var interpolation = _game.PresentationInterpolationFraction;
            foreach (var feedback in _presentation.Feedback)
            {
                if (feedback == null || !feedback.IsFollowingTarget) continue;
                if (feedback.Kind == BattlePresentationEventKind.EntityDefeated)
                {
                    feedback.Point = feedback.EventPoint;
                    feedback.DetachFromTarget();
                    continue;
                }

                var zombie = _game.ZombieById(feedback.TargetEntityId);
                if (zombie != null && zombie.Hp > 0f)
                {
                    var progress = _renderSamples.EnemyPathProgress(
                        zombie.Id, zombie.PathProgress, interpolation);
                    feedback.UpdateFollowPoint(
                        _game.Map.SampleRoute(zombie.RouteId, progress));
                    continue;
                }

                var plant = _game.PlantById(feedback.TargetEntityId);
                if (plant != null)
                {
                    feedback.UpdateFollowPoint(plant.PotId < 0
                        ? _game.Map.Core
                        : _game.PotPoint(_game.PotById(plant.PotId)));
                    continue;
                }

                feedback.Point = feedback.EventPoint;
                feedback.DetachFromTarget();
            }
        }

        private void SyncFloatingTextOverlay()
        {
            if (_floatingTextOverlay == null) return;
            var layout = BattleLayout;
            var viewport = BattlefieldProjection.CalculateViewportLayout(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent(),
                BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
            _floatingTextOverlay.Sync(
                _presentation.Feedback, _presentation.FloatingTextStyles,
                layout.Battlefield, viewport, layout.BattleStage,
                _presentation.BattlefieldOffset);
        }


    }
}
