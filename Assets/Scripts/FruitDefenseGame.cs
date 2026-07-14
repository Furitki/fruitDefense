using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Core;
using FruitDefense.Platform;
using FruitDefense.Presentation;
using UnityEngine;

namespace FruitDefense
{
    public sealed class FruitDefenseGame : MonoBehaviour, IBattleSessionHost
    {
        public const string SessionNotInitialized = "battle-session-not-initialized";
        public const string ResultAlreadySubmitted = "battle-result-already-submitted";

        private enum DragPayloadType { Plant, Weapon, Pot }
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
            public WeaponKind Weapon;
            public Vector2 Start;
            public Vector2 Current;
            public bool Active;
        }

        private sealed class RestartPresentationState
        {
            public int InspectedPlantId = -1;
            public WeaponKind SelectedWeapon = WeaponKind.None;
            public bool PotToolSelected;
            public string Status = DefaultStatus;
            public float StatusUntil;
            public DragSession Drag;
            public int DragControlId;
            public int ReturnPulsePlantId = -1;
            public float ReturnPulseUntil;
            public float NurseryRollDisplayUntil;

            public bool IsClean()
            {
                return InspectedPlantId == -1
                    && SelectedWeapon == WeaponKind.None
                    && !PotToolSelected
                    && Status == DefaultStatus
                    && StatusUntil == 0f
                    && Drag == null
                    && DragControlId == 0
                    && ReturnPulsePlantId == -1
                    && ReturnPulseUntil == 0f
                    && NurseryRollDisplayUntil == 0f;
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

        // iPhone 17 logical portrait reference: 1206 x 2622 physical pixels at 3x.
        private const float DesignWidth = 402f;
        private const float DesignHeight = 874f;
        private const string BetweenWaveActionLabel = "\u7ACB\u5373\u5F00\u59CB\u4E0B\u4E00\u6CE2";
        private const string DefaultStatus = "点击水果查看信息；拖动完成种植、移动、返回与合成";
        private static readonly Rect HeaderRect = new Rect(8f, 8f, 386f, 60f);
        private static readonly Rect BoardRect = new Rect(4f, 76f, 394f, 398f);
        private static readonly Rect BuildRect = new Rect(8f, 482f, 386f, 220f);
        private static readonly Rect DetailRect = new Rect(8f, 710f, 386f, 92f);
        private static readonly Rect StatusRect = new Rect(8f, 810f, 386f, 56f);
        private static readonly Rect ModalRect = new Rect(36f, 300f, 330f, 244f);

        private GameSimulation _game;
        private readonly BattlePresentationBuffer _presentation = new BattlePresentationBuffer();
        private BattleLaunchRequest _currentRequest;
        private IAppNavigator _navigator;
        private IBattleResultSink _resultSink;
        private AppBootstrap _appBootstrap;
        private bool _hasInitialized;
        private bool _isInitialized;
        private bool _hasEnteredBattleRoute;
        private bool _resultSubmitted;
        private bool _sessionDisposed;
        private Font _font;
        private Texture2D _tempArtAtlas;
        private Texture2D _combatVfxAtlas;
        private Texture2D _attackRangeTexture;
        private BattlefieldProjection _projection;
        private GUIStyle _title;
        private GUIStyle _heading;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _center;
        private GUIStyle _button;
        private GUIStyle _entity;
        private GUIStyle _tiny;
        private int _inspectedPlantId = -1;
        private WeaponKind _selectedWeapon = WeaponKind.None;
        private bool _potToolSelected;
        private string _status = DefaultStatus;
        private float _statusUntil;
        private DragSession _drag;
        private int _dragControlId;
        private int _returnPulsePlantId = -1;
        private float _returnPulseUntil;
        private float _nurseryRollDisplayUntil;

        public GameSimulation Simulation { get { return _game; } }
        public BattleLaunchRequest CurrentRequest { get { return _currentRequest; } }
        public bool IsInitialized { get { return _isInitialized; } }
        public bool HasSubmittedResult { get { return _resultSubmitted; } }
        public string LastResultSubmissionError { get; private set; } = string.Empty;
        public static int ActiveSessionHostCount { get; private set; }

        public static bool ValidateInspectionOnlyInteraction(out string reason)
        {
            var simulation = new GameSimulation(5150);
            simulation.State.Plants.Clear();
            var firstPot = simulation.State.Pots[0];
            var secondPot = simulation.State.Pots[1];
            var first = new Plant
            {
                Id = 8101, Kind = PlantKind.Pea, Star = 1, PotId = firstPot.Id, NurseryIndex = -1,
            };
            var second = new Plant
            {
                Id = 8102, Kind = PlantKind.Pea, Star = 1, PotId = secondPot.Id, NurseryIndex = -1,
            };
            var nursery = new Plant
            {
                Id = 8103, Kind = PlantKind.Watermelon, Star = 1, PotId = -1, NurseryIndex = 0,
            };
            var support = new Plant
            {
                Id = 8104, Kind = PlantKind.Sunflower, Star = 1, PotId = firstPot.Id, NurseryIndex = -1,
            };
            simulation.State.Plants.Add(first);
            simulation.State.Plants.Add(second);
            simulation.State.Plants.Add(nursery);

            var inspected = InspectionPlantId(first);
            var firstPosition = first.PotId;
            var secondPosition = second.PotId;
            var plantCount = simulation.State.Plants.Count;
            var guidance = DestinationDragGuidance(first, false);
            inspected = InspectionPlantId(second);
            if (inspected != second.Id || first.PotId != firstPosition || second.PotId != secondPosition
                || first.Star != 1 || second.Star != 1 || simulation.State.Plants.Count != plantCount
                || string.IsNullOrEmpty(guidance))
            {
                reason = "passive plant and destination clicks changed the formation";
                return false;
            }

            inspected = InspectionPlantId(nursery);
            if (inspected != nursery.Id || nursery.PotId >= 0 || EffectiveAttackRange(support) > .0001f)
            {
                reason = "nursery or zero-range inspection contract failed";
                return false;
            }

            var projection = new BattlefieldProjection(simulation.Map, BoardRect);
            var center = simulation.PotPoint(firstPot);
            var range = EffectiveAttackRange(first);
            var centerOnScreen = projection.MapToScreen(center);
            var insideOnScreen = projection.MapToScreen(center + Vector2.right * (range - .001f));
            var outsideOnScreen = projection.MapToScreen(center + Vector2.right * (range + .001f));
            var projectedRadius = projection.MapDistanceToScreen(range);
            if (Vector2.Distance(centerOnScreen, insideOnScreen) > projectedRadius + .001f
                || Vector2.Distance(centerOnScreen, outsideOnScreen) <= projectedRadius)
            {
                reason = "shared projection does not separate deterministic in-range and out-of-range points";
                return false;
            }

            reason = "ok";
            return true;
        }

        public static bool ValidatePortraitLayout(out string reason)
        {
            var design = new Rect(0f, 0f, DesignWidth, DesignHeight);
            var regions = new[] { HeaderRect, BoardRect, BuildRect, DetailRect, StatusRect };
            foreach (var region in regions)
            {
                if (region.xMin < design.xMin || region.yMin < design.yMin
                    || region.xMax > design.xMax || region.yMax > design.yMax)
                {
                    reason = "region outside design bounds: " + region;
                    return false;
                }
            }

            for (var index = 1; index < regions.Length; index++)
            {
                if (regions[index - 1].yMax > regions[index].yMin)
                {
                    reason = "portrait regions overlap vertically";
                    return false;
                }
            }

            if (BoardRect.width <= 386f || BoardRect.height <= 320f || BoardRect.width < 390f)
            {
                reason = "battlefield was not enlarged to the nearly full-width target";
                return false;
            }

            var projection = new BattlefieldProjection(GameConfig.DefaultBattlefield, BoardRect);
            if (!projection.ValidatePlantingGeometry(out reason)) return false;
            if (!projection.ValidateControlInset(out reason)) return false;
            if (projection.PotSize + .01f < BattlefieldProjection.ReferencePotSize)
            {
                reason = "flowerpot target is not doubled from the legacy reference size";
                return false;
            }
            foreach (var routePoint in projection.RoutePoints)
            {
                if (!BoardRect.Contains(routePoint))
                {
                    reason = "projected route leaves the battlefield";
                    return false;
                }
            }

            var primaryTargets = new[]
            {
                new Rect(274f, 16f, 52f, 44f), new Rect(334f, 16f, 52f, 44f),
                WeaponToolRect(WeaponKind.Gatling), WeaponToolRect(WeaponKind.Ice),
                WeaponToolRect(WeaponKind.Chili), PotToolRect(),
                RefreshRect(),
                projection.WaveActionRect,
                new Rect(DetailRect.xMax - 52f, DetailRect.y + 4f, 44f, 44f),
            };
            foreach (var target in primaryTargets)
            {
                if (Mathf.Min(target.width, target.height) < 44f)
                {
                    reason = "primary target is smaller than 44 logical points: " + target;
                    return false;
                }
            }

            reason = "ok";
            return true;
        }

        public static bool ValidateSessionControlContract(out string reason)
        {
            var projection = new BattlefieldProjection(GameConfig.DefaultBattlefield, BoardRect);
            if (!projection.ValidateControlInset(out reason)) return false;

            if (!HasWaveAction(GamePhase.Ready, false)
                || WaveActionLabel(GamePhase.Ready) != "开始波次"
                || HasWaveAction(GamePhase.Playing, false)
                || !HasWaveAction(GamePhase.BetweenWaves, false)
                || WaveActionLabel(GamePhase.BetweenWaves) != "立即开始下一波"
                || HasWaveAction(GamePhase.Victory, false)
                || HasWaveAction(GamePhase.Defeat, false)
                || HasWaveAction(GamePhase.Ready, true))
            {
                reason = "phase-specific battlefield wave action contract failed";
                return false;
            }

            if (ModalActionCount(GamePhase.Ready, false) != 0
                || ModalActionCount(GamePhase.Playing, true) != 2
                || ModalActionCount(GamePhase.BetweenWaves, true) != 2
                || ModalActionCount(GamePhase.Victory, false) != 1
                || ModalActionCount(GamePhase.Defeat, false) != 1)
            {
                reason = "phase-specific modal action count failed";
                return false;
            }

            for (var count = 1; count <= 2; count++)
            {
                Rect? previous = null;
                for (var index = 0; index < count; index++)
                {
                    var target = ModalActionRect(index, count);
                    if (Mathf.Min(target.width, target.height) < 44f
                        || target.xMin < ModalRect.xMin || target.yMin < ModalRect.yMin
                        || target.xMax > ModalRect.xMax || target.yMax > ModalRect.yMax
                        || previous.HasValue && previous.Value.Overlaps(target))
                    {
                        reason = "modal action geometry failed for " + count + " actions";
                        return false;
                    }
                    previous = target;
                }
            }

            var ready = new GameSimulation(9101);
            if (!ready.StartWave(out _) || ready.State.Phase != GamePhase.Playing
                || ready.State.WaveIndex != 1 || ready.State.BetweenTimer != 0f)
            {
                reason = "ready wave action did not start wave one";
                return false;
            }
            if (HasWaveAction(ready.State.Phase, ready.State.Paused) || ready.StartWave(out _))
            {
                reason = "playing phase exposed or accepted a wave-start action";
                return false;
            }

            var between = new GameSimulation(9102);
            between.State.Phase = GamePhase.BetweenWaves;
            between.State.WaveIndex = 1;
            between.State.BetweenTimer = 9.5f;
            if (!between.StartWave(out _) || between.State.Phase != GamePhase.Playing
                || between.State.WaveIndex != 2 || between.State.BetweenTimer != 0f)
            {
                reason = "between-wave action did not skip the timer and start wave two";
                return false;
            }

            var restartSimulation = new GameSimulation(9103);
            restartSimulation.State.Paused = true;
            restartSimulation.State.Phase = GamePhase.Playing;
            restartSimulation.State.WaveIndex = 4;
            restartSimulation.State.Sun = 77;
            restartSimulation.State.Lives = 3;
            restartSimulation.State.Zombies.Add(new Zombie { Id = 991, Hp = 1f, MaxHp = 1f });
            restartSimulation.State.Projectiles.Add(new ProjectileFlash { Id = 992 });
            restartSimulation.RefreshNursery(out _);
            var presentation = new RestartPresentationState
            {
                InspectedPlantId = 88,
                SelectedWeapon = WeaponKind.Ice,
                PotToolSelected = true,
                Status = "stale",
                StatusUntil = 5f,
                Drag = new DragSession { Type = DragPayloadType.Plant, PlantId = 88, Active = true },
                DragControlId = 7,
                ReturnPulsePlantId = 88,
                ReturnPulseUntil = 5f,
                NurseryRollDisplayUntil = 5f,
            };
            ResetFullRun(restartSimulation, presentation, 9103);
            if (!presentation.IsClean()
                || restartSimulation.State.Phase != GamePhase.Ready || restartSimulation.State.Paused
                || restartSimulation.State.WaveIndex != 0 || restartSimulation.State.Sun != 10
                || restartSimulation.State.Lives != 10 || restartSimulation.State.Zombies.Count != 0
                || restartSimulation.State.Projectiles.Count != 0
                || restartSimulation.PendingPresentationEventCount != 1)
            {
                reason = "centralized restart did not clear simulation and presentation state";
                return false;
            }

            reason = "ok";
            return true;
        }

        public static bool HasWaveAction(GamePhase phase, bool paused)
        {
            return !paused && (phase == GamePhase.Ready || phase == GamePhase.BetweenWaves);
        }

        public static string WaveActionLabel(GamePhase phase)
        {
            if (phase == GamePhase.BetweenWaves) return BetweenWaveActionLabel;
            return phase == GamePhase.Ready ? "开始波次"
                : phase == GamePhase.BetweenWaves ? "立即开始下一波"
                : string.Empty;
        }

        // Temporary compatibility path for the current single Main scene. Once the final Bootstrap
        // scene is active it owns construction and this installer exits without creating a host.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallStandaloneCompatibilityHost()
        {
            if (AppBootstrap.Instance != null) return;

            var host = FindAnyObjectByType<FruitDefenseGame>();
            if (host == null)
                host = new GameObject("FruitDefenseGame-StandaloneCompatibility").AddComponent<FruitDefenseGame>();
            if (host.IsInitialized) return;

            var navigator = new AppNavigator();
            if (navigator.TryBeginTransition(AppRoute.Battle, out _))
                navigator.TryCompleteTransition(out _);
            var request = new BattleLaunchRequest(
                "standalone-" + Guid.NewGuid().ToString("N"),
                "orchard-01",
                0,
                "builtin");
            var result = host.Initialize(request, navigator, new StandaloneCompatibilityResultSink());
            if (!result.Success)
                Debug.LogError("Standalone battle compatibility initialization failed: " + result.ErrorCode);
        }

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            _font = Resources.Load<Font>("Fonts/NotoSansSC-UI");
            if (_font == null)
            {
                Debug.LogError("Bundled UI font is missing: Resources/Fonts/NotoSansSC-UI.ttf");
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            _font.RequestCharactersInTexture("开始波次立即开始下一波继续游戏重新开始", 15, FontStyle.Bold);
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
            BuildStyles();
            Application.targetFrameRate = 60;
        }

        private void OnDestroy()
        {
            DisposeSession();
            if (_attackRangeTexture != null) Destroy(_attackRangeTexture);
            _attackRangeTexture = null;
            _font = null;
            _tempArtAtlas = null;
            _combatVfxAtlas = null;
        }

        public BattleSessionInitializationResult Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            BattlefieldMapDefinition map = null)
        {
            if (_hasInitialized)
                return BattleSessionInitializationResult.Failed(BattleSessionInitializationResult.AlreadyInitialized);
            if (request == null)
                return BattleSessionInitializationResult.Failed(BattleSessionInitializationResult.InvalidRequest);
            if (!request.TryValidate(out var requestError))
                return BattleSessionInitializationResult.Failed(requestError);
            if (navigator == null)
                return BattleSessionInitializationResult.Failed(BattleSessionInitializationResult.NavigatorRequired);
            if (resultSink == null)
                return BattleSessionInitializationResult.Failed(BattleSessionInitializationResult.ResultSinkRequired);

            GameSimulation simulation;
            try
            {
                simulation = new GameSimulation(request.Seed, map);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.SimulationConstructionFailed);
            }

            _hasInitialized = true;
            _isInitialized = true;
            _sessionDisposed = false;
            _currentRequest = request;
            _navigator = navigator;
            _resultSink = resultSink;
            _game = simulation;
            _presentation.Clear();
            _projection = new BattlefieldProjection(_game.Map, BoardRect);
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

            var presentation = CaptureRestartPresentation();
            ResetFullRun(_game, presentation, _currentRequest.Seed);
            _presentation.Clear();
            ApplyRestartPresentation(presentation);
            errorCode = string.Empty;
            return true;
        }

        public BattleSnapshotRestoreResult RestoreCurrentSessionSnapshot(BattleSnapshotV1 snapshot)
        {
            if (!_isInitialized || _game == null)
                return new BattleSnapshotRestoreResult(BattleSnapshotRestoreCode.InvalidPayload,
                    "session", SessionNotInitialized);

            var result = _game.RestoreSnapshot(snapshot);
            if (!result.Succeeded) return result;

            _presentation.Clear();
            CancelTransientInteraction();
            return result;
        }

        public bool TrySubmitTerminalResult()
        {
            if (!_isInitialized || _game == null || _currentRequest == null || _resultSubmitted)
                return false;

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
                LastResultSubmissionError = string.IsNullOrWhiteSpace(errorCode)
                    ? "battle-result-submission-failed"
                    : errorCode;
                Debug.LogWarning("Battle result submission failed: " + LastResultSubmissionError);
            }
            else
            {
                LastResultSubmissionError = string.Empty;
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
            _navigator = null;
            _resultSink = null;
            _appBootstrap = null;
            _currentRequest = null;
            _game = null;
            _projection = null;
            _presentation.Clear();
            CancelTransientInteraction();
            ResetInteractionState();
        }

        private void CancelTransientInteraction()
        {
            if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
            _drag = null;
            _dragControlId = 0;
            _selectedWeapon = WeaponKind.None;
            _potToolSelected = false;
        }

        private sealed class StandaloneCompatibilityResultSink : IBattleResultSink
        {
            public bool TrySubmitResult(BattleResult result, out string errorCode)
            {
                Debug.Log("Standalone battle completed: " + result.Outcome);
                errorCode = string.Empty;
                return true;
            }
        }

        private void BuildStyles()
        {
            _title = Style(20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(.20f, .13f, .08f));
            _heading = Style(16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(.25f, .16f, .10f));
            _body = Style(15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(.26f, .18f, .11f));
            _small = Style(15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(.38f, .29f, .20f));
            _center = Style(15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.24f, .16f, .09f));
            _entity = Style(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            _tiny = Style(10, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            _button = Style(15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.24f, .16f, .09f));
            _body.wordWrap = true;
            _small.wordWrap = true;
            _button.wordWrap = false;
            _button.normal.background = Texture2D.whiteTexture;
            _button.hover.background = Texture2D.whiteTexture;
            _button.active.background = Texture2D.whiteTexture;
        }

        private GUIStyle Style(int size, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            return new GUIStyle
            {
                font = _font, fontSize = size, fontStyle = fontStyle,
                alignment = anchor, normal = { textColor = color }, richText = true,
            };
        }

        private void Update()
        {
            if (!_isInitialized || _game == null) return;
            if (Input.GetKeyDown(KeyCode.Space)) _game.TogglePause();
            if (Input.GetKeyDown(KeyCode.Alpha1)) _game.SetSpeed(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _game.SetSpeed(2);
            _presentation.Advance(Time.unscaledDeltaTime);
            _game.Tick(Time.unscaledDeltaTime);
            _presentation.Consume(_game);
            TrySubmitTerminalResult();
            if (_inspectedPlantId >= 0 && _game.PlantById(_inspectedPlantId) == null) _inspectedPlantId = -1;
        }

        public void ConfigureAcceptanceState(string stateName)
        {
            if (!_isInitialized || _game == null) return;
            if (!Application.absoluteURL.Contains("acceptance=1")) return;
            _game = new GameSimulation(20260714);
            _game.DiscardPendingPresentationEvents();
            _presentation.Clear();
            _projection = new BattlefieldProjection(_game.Map, BoardRect);
            _game.State.Pots.Clear();
            _game.State.Plants.Clear();
            _game.State.Zombies.Clear();
            _game.State.Projectiles.Clear();
            ResetInteractionState();

            switch (stateName)
            {
                case "adjacent-pots":
                    AddAcceptancePot(new Vector2Int(3, 2), PlantKind.Pea);
                    AddAcceptancePot(new Vector2Int(4, 2), PlantKind.Watermelon);
                    break;
                case "drag-target":
                    AddAcceptancePot(new Vector2Int(3, 2));
                    _game.State.Plants.Add(new Plant
                    {
                        Id = _game.State.NextId++, Kind = PlantKind.Pea, Star = 1, NurseryIndex = 0,
                    });
                    break;
                case "selection-inspection":
                    AddAcceptancePot(new Vector2Int(3, 2), PlantKind.Pea);
                    AddAcceptancePot(new Vector2Int(4, 2));
                    _game.State.Plants.Add(new Plant
                    {
                        Id = _game.State.NextId++, Kind = PlantKind.Sunflower, Star = 1, NurseryIndex = 0,
                    });
                    break;
                case "active-wave":
                    AddAcceptancePot(new Vector2Int(2, 0), PlantKind.Pea);
                    AddAcceptancePot(new Vector2Int(5, 0), PlantKind.Watermelon);
                    _game.StartWave(out _);
                    break;
                case "between-wave":
                    AddAcceptancePot(new Vector2Int(2, 0), PlantKind.Pea);
                    AddAcceptancePot(new Vector2Int(5, 0), PlantKind.Watermelon);
                    _game.State.Phase = GamePhase.BetweenWaves;
                    _game.State.WaveIndex = 1;
                    _game.State.BetweenTimer = 9.5f;
                    break;
                case "dense-board":
                    var kind = 0;
                    foreach (var cell in _game.Map.PlantableCells)
                    {
                        AddAcceptancePot(cell, (PlantKind)(kind % 5));
                        kind++;
                    }
                    break;
                default:
                    AddAcceptancePot(new Vector2Int(1, 0));
                    AddAcceptancePot(new Vector2Int(3, 0));
                    AddAcceptancePot(new Vector2Int(5, 0));
                    AddAcceptancePot(new Vector2Int(7, 1));
                    AddAcceptancePot(new Vector2Int(7, 3));
                    AddAcceptancePot(new Vector2Int(1, 5));
                    AddAcceptancePot(new Vector2Int(4, 5));
                    AddAcceptancePot(new Vector2Int(6, 5));
                    break;
            }
        }

        private void AddAcceptancePot(Vector2Int cell, PlantKind? plantKind = null)
        {
            var pot = new Pot { Id = _game.State.NextId++, Cell = cell, Active = true };
            _game.State.Pots.Add(pot);
            if (!plantKind.HasValue) return;
            _game.State.Plants.Add(new Plant
            {
                Id = _game.State.NextId++,
                Kind = plantKind.Value,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
            });
        }

        private void OnGUI()
        {
            if (!_isInitialized || _game == null) return;
            var safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.matrix = Matrix4x4.identity;
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(.91f, .86f, .75f));
            var scale = Mathf.Min(safeArea.width / DesignWidth, safeArea.height / DesignHeight);
            var offsetX = safeArea.x + (safeArea.width - DesignWidth * scale) * .5f;
            var safeTop = Screen.height - safeArea.yMax;
            var offsetY = safeTop + (safeArea.height - DesignHeight * scale) * .5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            HandleDragInput(Event.current);
            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(.91f, .86f, .75f));
            DrawHeader();
            DrawBoard();
            DrawBuildPanel();
            DrawStatusPanel();
            DrawDragGhost();
            DrawOverlay();
        }

        private void HandleDragInput(Event evt)
        {
            if (_game == null) return;
            var controlId = GUIUtility.GetControlID(0x4F524348, FocusType.Passive);
            var ended = _game.State.Phase == GamePhase.Victory || _game.State.Phase == GamePhase.Defeat;
            if ((_game.State.Paused || ended) && _drag != null) CancelDrag("拖拽已取消，物品返回原位");

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && _drag != null)
            {
                CancelDrag("已取消拖拽，物品返回原位");
                evt.Use();
                return;
            }
            if (_game.State.Paused || ended) return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var source = FindDragSourceAt(evt.mousePosition);
                if (source == null) return;
                source.Start = evt.mousePosition;
                source.Current = evt.mousePosition;
                _drag = source;
                _dragControlId = controlId;
                GUIUtility.hotControl = controlId;
                evt.Use();
                return;
            }

            if (_drag == null) return;
            if (evt.type == EventType.MouseDrag)
            {
                _drag.Current = evt.mousePosition;
                if (!_drag.Active && Vector2.Distance(_drag.Start, _drag.Current) > 8f)
                {
                    _drag.Active = true;
                    _selectedWeapon = WeaponKind.None;
                    _potToolSelected = false;
                }
                if (_drag.Active) UpdateDragHoverStatus(_drag.Current);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp)
            {
                var session = _drag;
                session.Current = evt.mousePosition;
                if (session.Active) CompleteDrag(session, session.Current);
                else PerformSourceClick(session);
                _drag = null;
                if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
                _dragControlId = 0;
                evt.Use();
            }
        }

        private DragSession FindDragSourceAt(Vector2 point)
        {
            foreach (var plant in _game.State.Plants)
            {
                var rect = PlantSourceRect(plant);
                if (rect.width > 0f && rect.Contains(point))
                    return new DragSession { Type = DragPayloadType.Plant, PlantId = plant.Id };
            }
            if (_game.State.Inventory.Gatling > 0 && WeaponToolRect(WeaponKind.Gatling).Contains(point))
                return new DragSession { Type = DragPayloadType.Weapon, Weapon = WeaponKind.Gatling };
            if (_game.State.Inventory.Ice > 0 && WeaponToolRect(WeaponKind.Ice).Contains(point))
                return new DragSession { Type = DragPayloadType.Weapon, Weapon = WeaponKind.Ice };
            if (_game.State.Inventory.Chili > 0 && WeaponToolRect(WeaponKind.Chili).Contains(point))
                return new DragSession { Type = DragPayloadType.Weapon, Weapon = WeaponKind.Chili };
            if (_game.State.Inventory.Pots > 0 && PotToolRect().Contains(point))
                return new DragSession { Type = DragPayloadType.Pot };
            return null;
        }

        private DropTarget FindDropTargetAt(DragSession session, Vector2 cursor)
        {
            var targets = new List<DropTarget>();
            if (session.Type == DragPayloadType.Plant)
            {
                foreach (var pot in _game.State.Pots.Where(value => value.Active))
                {
                    var rect = PotRect(pot);
                    targets.Add(new DropTarget { Type = DropTargetType.Pot, Id = pot.Id, Rect = rect });
                }
                for (var slot = 0; slot < 5; slot++)
                {
                    var rect = NurseryRect(slot);
                    targets.Add(new DropTarget { Type = DropTargetType.Nursery, Slot = slot, Rect = rect });
                }
            }
            else if (session.Type == DragPayloadType.Weapon)
            {
                foreach (var plant in _game.State.Plants)
                {
                    var rect = PlantSourceRect(plant);
                    if (rect.width > 0f)
                        targets.Add(new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = rect });
                }
            }
            else
            {
                foreach (var cell in _game.Map.PlantableCells)
                {
                    if (_game.State.Pots.Any(pot => pot.Active && pot.Cell == cell)) continue;
                    var rect = ExpansionRect(cell);
                    targets.Add(new DropTarget { Type = DropTargetType.Expansion, Cell = cell, Rect = rect });
                }
            }

            var rects = targets.Select(target => target.Rect).ToList();
            var bestIndex = DragGeometry.BestOverlapIndex(DragGeometry.PreviewRect(cursor), rects);
            return bestIndex >= 0 ? targets[bestIndex] : new DropTarget { Type = DropTargetType.None };
        }

        private void PerformSourceClick(DragSession source)
        {
            if (source.Type == DragPayloadType.Weapon)
            {
                ToggleWeaponSelection(source.Weapon);
                return;
            }
            if (source.Type == DragPayloadType.Pot)
            {
                TogglePotTool();
                return;
            }
            var plant = _game.PlantById(source.PlantId);
            if (plant != null) HandlePlantClick(plant);
        }

        private void CompleteDrag(DragSession session, Vector2 point)
        {
            var target = FindDropTargetAt(session, point);
            if (session.Type == DragPayloadType.Plant)
            {
                if (target.Type == DropTargetType.Pot)
                {
                    var status = _game.GetPlantDropStatus(session.PlantId, target.Id);
                    if (!status.Legal) { CancelDrag(status.Reason); return; }
                    var targetPlant = _game.PlantAtPot(target.Id);
                    var selectedAfterDrop = targetPlant != null ? targetPlant.Id : session.PlantId;
                    var success = _game.MoveOrMergePlant(session.PlantId, target.Id, out var reason);
                    if (success) _inspectedPlantId = selectedAfterDrop;
                    SetStatus(success, reason);
                    return;
                }
                if (target.Type == DropTargetType.Nursery)
                {
                    var status = _game.GetNurseryDropStatus(session.PlantId, target.Slot);
                    if (!status.Legal) { CancelDrag(status.Reason); return; }
                    var success = _game.MoveToNursery(session.PlantId, target.Slot, out var reason);
                    if (success) _inspectedPlantId = -1;
                    SetStatus(success, reason);
                    return;
                }
                CancelDrag("未命中花盆或刷新栏，水果返回原位");
                return;
            }

            if (session.Type == DragPayloadType.Weapon)
            {
                if (target.Type != DropTargetType.Plant) { CancelDrag("未命中植物，武器返回库存"); return; }
                var status = _game.GetWeaponInstallStatus(target.Id, session.Weapon);
                if (!status.Legal) { CancelDrag(status.Reason); return; }
                var success = _game.InstallWeapon(target.Id, session.Weapon, out var reason);
                if (success) _inspectedPlantId = target.Id;
                SetStatus(success, reason);
                return;
            }

            if (target.Type != DropTargetType.Expansion || !_game.CanExpand(target.Cell))
            {
                CancelDrag("未命中绿色扩建格，花盆返回库存");
                return;
            }
            SetStatus(_game.ExpandPot(target.Cell, out var expandReason), expandReason);
        }

        private void CancelDrag(string reason)
        {
            if (_drag != null && _drag.Type == DragPayloadType.Plant)
            {
                _returnPulsePlantId = _drag.PlantId;
                _returnPulseUntil = Time.unscaledTime + .55f;
            }
            SetStatus(false, reason);
            _drag = null;
            if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
            _dragControlId = 0;
        }

        private void UpdateDragHoverStatus(Vector2 point)
        {
            var target = FindDropTargetAt(_drag, point);
            var status = DragTargetStatus(_drag, target);
            _status = (status.Legal ? "✓ " : "! ") + status.Reason;
            _statusUntil = Time.unscaledTime + .4f;
        }

        private InteractionStatus DragTargetStatus(DragSession session, DropTarget target)
        {
            if (session == null || target.Type == DropTargetType.None)
                return new InteractionStatus(false, "松开将取消，物品返回原位");
            if (session.Type == DragPayloadType.Plant)
            {
                var status = target.Type == DropTargetType.Pot
                    ? _game.GetPlantDropStatus(session.PlantId, target.Id)
                    : target.Type == DropTargetType.Nursery
                        ? _game.GetNurseryDropStatus(session.PlantId, target.Slot)
                        : new PlantDropStatus(false, PlantDropAction.Invalid, "这里不能放置水果");
                return new InteractionStatus(status.Legal, status.Reason);
            }
            if (session.Type == DragPayloadType.Weapon)
                return target.Type == DropTargetType.Plant
                    ? _game.GetWeaponInstallStatus(target.Id, session.Weapon)
                    : new InteractionStatus(false, "请拖到一株植物上");
            return new InteractionStatus(target.Type == DropTargetType.Expansion && _game.CanExpand(target.Cell),
                target.Type == DropTargetType.Expansion && _game.CanExpand(target.Cell) ? "松开添加花盆" : "请拖到绿色扩建格");
        }

        private void ToggleWeaponSelection(WeaponKind weapon)
        {
            _selectedWeapon = _selectedWeapon == weapon ? WeaponKind.None : weapon;
            _potToolSelected = false;
            SetStatus(true, _selectedWeapon == WeaponKind.None ? "已取消武器选择" : "拖动或点击植物安装" + GameConfig.WeaponName(weapon));
        }

        private void TogglePotTool()
        {
            _potToolSelected = !_potToolSelected;
            _selectedWeapon = WeaponKind.None;
            SetStatus(true, _potToolSelected ? "拖动花盆到绿色候选格，或点击扩建" : "已取消扩建");
        }

        private static Rect NurseryRect(int slot)
        {
            const float gap = 5f;
            var width = (BuildRect.width - 16f - gap * 4f) / 5f;
            return new Rect(BuildRect.x + 8f + slot * (width + gap), BuildRect.y + 106f, width, 62f);
        }

        private static Rect WeaponToolRect(WeaponKind weapon)
        {
            var index = weapon == WeaponKind.Gatling ? 0 : weapon == WeaponKind.Ice ? 1 : 2;
            return new Rect(BuildRect.x + 8f + index * 94f, BuildRect.y + 30f, 88f, 50f);
        }

        private static Rect PotToolRect() { return new Rect(BuildRect.x + 8f + 3f * 94f, BuildRect.y + 30f, 88f, 50f); }

        private static Rect RefreshRect() { return new Rect(BuildRect.x + 8f, BuildRect.y + 172f, BuildRect.width - 16f, 44f); }

        private BattlefieldProjection Projection
        {
            get
            {
                if (_projection == null) _projection = new BattlefieldProjection(_game.Map, BoardRect);
                return _projection;
            }
        }

        private Rect ExpansionRect(Vector2Int cell) { return Projection.CellRect(cell); }

        private Rect PotRect(Pot pot)
        {
            return Projection.PotRect(pot.Cell);
        }
        private Rect PlantSourceRect(Plant plant)
        {
            if (plant.PotId >= 0)
            {
                var pot = _game.PotById(plant.PotId);
                return pot == null ? new Rect() : PotRect(pot);
            }
            return plant.NurseryIndex >= 0 ? NurseryRect(plant.NurseryIndex) : new Rect();
        }
        private static Rect Grow(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private void DrawHeader()
        {
            DrawPanel(HeaderRect, new Color(.99f, .97f, .88f));
            GUI.Label(new Rect(16f, 12f, 118f, 24f), "水果塔防", _title);
            GUI.Label(new Rect(16f, 38f, 88f, 24f), "阳光 <b>" + _game.State.Sun + "</b>", _body);
            GUI.Label(new Rect(104f, 38f, 82f, 24f), "核心 <b>" + _game.State.Lives + "</b>", _body);
            GUI.Label(new Rect(186f, 38f, 76f, 24f), "波次 <b>" + _game.State.WaveIndex + "/15</b>", _body);
            if (ColoredButton(new Rect(274f, 16f, 52f, 44f), _game.State.Paused ? "继续" : "暂停", new Color(.96f, .84f, .48f)))
                _game.TogglePause();
            if (ColoredButton(new Rect(334f, 16f, 52f, 44f), _game.State.Speed + " 倍", new Color(.83f, .91f, .67f)))
                _game.SetSpeed(_game.State.Speed == 1 ? 2 : 1);
        }

        private void DrawBoard()
        {
            DrawPanel(BoardRect, new Color(.37f, .62f, .24f));
            DrawRect(new Rect(BoardRect.x + 6f, BoardRect.y + 6f, BoardRect.width - 12f, BoardRect.height - 12f), new Color(.46f, .68f, .28f));
            var points = Projection.RoutePoints;
            for (var index = 0; index < points.Count - 1; index++)
            {
                DrawLine(points[index], points[index + 1], Projection.MapDistanceToScreen(GameConfig.MapDistance(8f)), new Color(.41f, .43f, .24f));
                DrawLine(points[index], points[index + 1], Projection.MapDistanceToScreen(GameConfig.MapDistance(6f)), new Color(.88f, .73f, .43f));
            }
            DrawCore();
            DrawPlantingCells();
            DrawInspectedAttackRange();
            if (_potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot)) DrawExpansionCandidates();
            DrawPotsAndPlants();
            DrawProjectiles();
            DrawZombies();
            DrawCombatEffects();
            DrawFeedback();
            DrawBoardStatus();
        }

        private void DrawCore()
        {
            var rect = Projection.LegacyVisualRect(_game.Map.Core, 172f, 140f);
            DrawRect(rect, new Color(.71f, .79f, .45f));
            GUI.Label(new Rect(rect.x, rect.y + rect.height * .08f, rect.width, rect.height * .55f), "♣",
                Style(17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.19f, .38f, .16f)));
            GUI.Label(new Rect(rect.x, rect.center.y + rect.height * .08f, rect.width, 18f), "核心", _tiny);
        }

        private void DrawPlantingCells()
        {
            var expansionActive = _potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot);
            foreach (var cell in _game.Map.PlantableCells)
            {
                var rect = ExpansionRect(cell);
                var active = _game.State.Pots.Any(pot => pot.Active && pot.Cell == cell);
                var legal = !active && _game.CanExpand(cell);
                var border = active
                    ? new Color(.42f, .29f, .16f, .9f)
                    : expansionActive && legal
                        ? new Color(.72f, .9f, .32f, .95f)
                        : new Color(.34f, .48f, .22f, .8f);
                var fill = active
                    ? new Color(.46f, .31f, .17f, .7f)
                    : new Color(.58f, .72f, .36f, .42f);
                DrawRect(Grow(rect, .5f), border);
                DrawRect(rect, fill);
                if (!active)
                    GUI.Label(rect, expansionActive && legal ? "+" : "·", Style(11, FontStyle.Bold, TextAnchor.MiddleCenter,
                        expansionActive && legal ? new Color(.88f, 1f, .55f) : new Color(.31f, .43f, .2f, .9f)));
            }
        }

        private void DrawInspectedAttackRange()
        {
            var plant = _game.PlantById(_inspectedPlantId);
            if (plant == null || plant.PotId < 0) return;
            var pot = _game.PotById(plant.PotId);
            var range = EffectiveAttackRange(plant);
            if (pot == null || range <= .0001f) return;
            var rangeRect = Projection.MapRect(_game.PotPoint(pot), range * 2f, range * 2f);
            rangeRect.position -= BoardRect.position;
            GUI.BeginGroup(BoardRect);
            GUI.DrawTexture(rangeRect, _attackRangeTexture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }

        public static float EffectiveAttackRange(Plant plant)
        {
            return plant == null ? 0f : GameConfig.Plant(plant.Kind).Range * GameConfig.StarRange(plant.Star);
        }

        private void DrawPotsAndPlants()
        {
            foreach (var pot in _game.State.Pots.Where(value => value.Active))
            {
                var rect = PotRect(pot);
                var plant = _game.PlantAtPot(pot.Id);
                var selected = plant != null && plant.Id == _inspectedPlantId;
                var border = selected ? new Color(1f, .84f, .23f) : new Color(.58f, .42f, .24f);
                var target = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon && plant != null
                    ? new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = rect }
                    : new DropTarget { Type = DropTargetType.Pot, Id = pot.Id, Rect = rect };
                if (IsCurrentDropTarget(target)) border = DropHighlightColor(target);
                if (plant != null && plant.Id == _returnPulsePlantId && Time.unscaledTime < _returnPulseUntil)
                    border = Color.Lerp(new Color(1f, .96f, .35f), Color.white, Mathf.PingPong(Time.unscaledTime * 8f, 1f));
                DrawRect(Grow(rect, .5f), border);
                DrawRect(rect, plant == null ? new Color(.84f, .65f, .38f) : new Color(.64f, .43f, .23f));
                DrawTempSprite(rect, plant == null ? TempSprite.EmptyPot : TempSprite.OccupiedPot);
                if (plant == null)
                {
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) HandlePotClick(pot.Id);
                    continue;
                }
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) HandlePlantClick(plant);
                DrawAnimatedPlant(new Rect(rect.x + 2f, rect.y - 8f, rect.width - 4f, rect.height - 3f), plant);
                GUI.Label(new Rect(rect.x - 4f, rect.yMax - 1f, rect.width + 8f, 10f), new string('★', plant.Star), _tiny);
                if (plant.Weapon != WeaponKind.None)
                    DrawTempSprite(new Rect(rect.xMax - 10f, rect.y - 4f, 11f, 11f), WeaponSprite(plant.Weapon));
                if (plant.MoveCooldown > 0f)
                    GUI.Label(rect, plant.MoveCooldown.ToString("0.0"), _tiny);
            }
        }

        private void HandlePotClick(int potId)
        {
            var inspected = _game.PlantById(_inspectedPlantId);
            SetStatus(false, DestinationDragGuidance(inspected, false));
        }

        private void HandlePlantClick(Plant plant)
        {
            if (_selectedWeapon != WeaponKind.None)
            {
                var success = _game.InstallWeapon(plant.Id, _selectedWeapon, out var reason);
                _selectedWeapon = WeaponKind.None;
                if (success)
                {
                    _inspectedPlantId = plant.Id;
                    _potToolSelected = false;
                }
                SetStatus(success, reason);
                return;
            }
            InspectPlant(plant);
        }

        private void InspectPlant(Plant plant)
        {
            if (plant == null) return;
            _inspectedPlantId = InspectionPlantId(plant);
            _selectedWeapon = WeaponKind.None;
            _potToolSelected = false;
            var verb = plant.PotId >= 0 ? "拖动可移动或合成" : "拖动到花盆种植";
            SetStatus(true, "正在查看" + GameConfig.Plant(plant.Kind).Name + "；" + verb);
        }

        private static int InspectionPlantId(Plant plant)
        {
            return plant == null ? -1 : plant.Id;
        }

        private static string DestinationDragGuidance(Plant inspected, bool nursery)
        {
            if (nursery) return inspected == null
                ? "请把场上水果拖到空苗圃位"
                : "点击只查看信息；请把场上水果拖到空苗圃位";
            return inspected == null
                ? "请把苗圃水果拖到花盆种植"
                : "点击只查看信息；请拖动" + GameConfig.Plant(inspected.Kind).Name + "到花盆";
        }

        private void DrawExpansionCandidates()
        {
            foreach (var cell in _game.Map.PlantableCells)
            {
                if (_game.State.Pots.Any(pot => pot.Active && pot.Cell == cell)) continue;
                var legal = _game.CanExpand(cell);
                var rect = ExpansionRect(cell);
                var target = new DropTarget { Type = DropTargetType.Expansion, Cell = cell, Rect = rect };
                if (IsCurrentDropTarget(target))
                    DrawRect(Grow(rect, 6f), legal ? new Color(.95f, .83f, .2f) : new Color(.9f, .25f, .2f));
                DrawRect(rect, legal ? new Color(.32f, .69f, .28f, .42f) : new Color(.42f, .36f, .34f, .58f));
                DrawTempSprite(rect, legal ? TempSprite.ExpansionPot : TempSprite.LockedPot);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && legal)
                {
                    SetStatus(_game.ExpandPot(cell, out var reason), reason);
                    if (_game.State.Inventory.Pots <= 0) _potToolSelected = false;
                }
            }
        }

        private void DrawZombies()
        {
            foreach (var zombie in _game.State.Zombies)
            {
                var point = ToBoard(_game.Map.Route.Sample(zombie.PathProgress));
                var size = Projection.LegacyVisualSize(48f);
                var rect = new Rect(point.x - size * .5f, point.y - size * .5f, size, size);
                var frozen = zombie.FreezeUntil > _game.State.Elapsed;
                if (frozen) DrawVfxSprite(Grow(rect, 4f), CombatSprite.FrozenAura, new Color(1f, 1f, 1f, .82f));
                DrawRect(Grow(rect, 1f), frozen ? new Color(.45f, .8f, 1f) : new Color(.33f, .19f, .18f));
                DrawRect(rect, zombie.SlowUntil > _game.State.Elapsed ? new Color(.45f, .65f, .75f) : new Color(.52f, .34f, .26f));
                DrawTempSprite(rect, ZombieSprite(zombie.Kind), zombie.HitStunUntil > _game.State.Elapsed ? new Color(1f, 1f, 1f, .58f) : Color.white);
                if (zombie.Burns.Count > 0)
                    DrawVfxSprite(new Rect(rect.xMax - 5f, rect.y - 6f, 11f, 11f), CombatSprite.Burning);
                var healthRect = new Rect(point.x - size * .5f, rect.y - 4f, size, 2f);
                DrawRect(healthRect, new Color(.22f, .16f, .12f));
                healthRect.width *= Mathf.Clamp01(zombie.Hp / zombie.MaxHp);
                DrawRect(healthRect, new Color(.85f, .22f, .16f));
            }
        }

        private void DrawProjectiles()
        {
            foreach (var projectile in _game.State.Projectiles)
            {
                var point = ToBoard(projectile.Position);
                if (projectile.Kind == PlantKind.Pea)
                {
                    var origin = ToBoard(projectile.Origin);
                    DrawLine(origin, point, Mathf.Max(1f, Projection.LegacyVisualSize(3f)), new Color(.56f, .95f, .29f, .42f));
                    DrawVfxSprite(CenteredRect(point, 26f), CombatSprite.PeaProjectile);
                }
                else if (projectile.Kind == PlantKind.Watermelon)
                {
                    var size = Mathf.Lerp(30f, 40f, Mathf.Sin(projectile.Progress * Mathf.PI));
                    DrawVfxSprite(CenteredRect(point, size), CombatSprite.WatermelonProjectile);
                }
                else
                {
                    var angle = (_game.State.Elapsed * 900f) * (projectile.Returning ? -1f : 1f);
                    DrawRotatedVfx(CenteredRect(point, 38f), CombatSprite.BananaProjectile, angle,
                        projectile.Returning ? new Color(1f, .96f, .62f) : Color.white);
                }
            }
        }

        private void DrawCombatEffects()
        {
            foreach (var effect in _presentation.CombatEffects)
            {
                var point = ToBoard(effect.Position);
                var progress = effect.Duration <= 0f ? 1f : 1f - Mathf.Clamp01(effect.Ttl / effect.Duration);
                var fade = Mathf.Clamp01(1f - progress * .9f);
                switch (effect.Kind)
                {
                    case CombatEffectKind.PeaImpact:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(20f, 39f, progress)), CombatSprite.PeaImpact, new Color(1f, 1f, 1f, fade));
                        break;
                    case CombatEffectKind.WatermelonBlast:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(48f, 102f, progress)), CombatSprite.WatermelonBlast, new Color(1f, 1f, 1f, fade));
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(35f, 125f, progress)), CombatSprite.ShockwaveRing, new Color(1f, 1f, 1f, fade));
                        break;
                    case CombatEffectKind.DurianDrop:
                        var landing = Mathf.Clamp01(progress / .72f);
                        var dropPoint = point + Vector2.up * Mathf.Lerp(-Projection.LegacyVisualSize(110f), 0f, landing * landing);
                        DrawVfxSprite(CenteredRect(dropPoint, 60f), CombatSprite.DurianDrop);
                        if (progress > .55f)
                            DrawVfxSprite(CenteredRect(point + Vector2.up * Projection.LegacyVisualSize(13f), Mathf.Lerp(45f, 118f, (progress - .55f) / .45f)),
                                CombatSprite.DurianShockwave, new Color(1f, 1f, 1f, fade));
                        break;
                    case CombatEffectKind.SunBurst:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(38f, 78f, Mathf.Sin(progress * Mathf.PI))), CombatSprite.SunBurst,
                            new Color(1f, 1f, 1f, fade));
                        break;
                    case CombatEffectKind.GatlingMuzzle:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(34f, 20f, progress)), CombatSprite.GatlingMuzzle, new Color(1f, 1f, 1f, fade));
                        break;
                    case CombatEffectKind.IceImpact:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(30f, 58f, progress)), CombatSprite.IceImpact, new Color(1f, 1f, 1f, fade));
                        break;
                    case CombatEffectKind.ChiliImpact:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(34f, 62f, progress)), CombatSprite.ChiliImpact, new Color(1f, 1f, 1f, fade));
                        break;
                    default:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(18f, 34f, progress)), CombatSprite.HitSpark, new Color(1f, 1f, 1f, fade));
                        break;
                }
            }
        }

        private void DrawFeedback()
        {
            foreach (var feedback in _presentation.Feedback)
            {
                var point = ToBoard(feedback.Point);
                var style = Style(11, FontStyle.Bold, TextAnchor.MiddleCenter, feedback.Color);
                GUI.Label(new Rect(point.x - 45f, point.y - 28f, 90f, 18f), feedback.Text, style);
            }
        }

        private void DrawBoardStatus()
        {
            var state = _game.State;
            var text = state.Phase == GamePhase.Playing
                ? "第 " + state.WaveIndex + " 波 · " + state.Zombies.Count + " 个敌人"
                : state.Phase == GamePhase.BetweenWaves
                    ? "下一波倒计时 " + Mathf.CeilToInt(state.BetweenTimer) + " 秒"
                    : state.Phase == GamePhase.Ready ? "准备阶段"
                    : state.Phase == GamePhase.Victory ? "防守成功"
                    : "核心失守";
            var strip = Projection.ControlStripRect;
            DrawRect(strip, new Color(.98f, .95f, .83f, .94f));
            if (HasWaveAction(state.Phase, state.Paused))
            {
                var labelRect = new Rect(strip.x + 8f, strip.y, strip.width - Projection.WaveActionRect.width - 16f, strip.height);
                GUI.Label(labelRect, text, Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.24f, .16f, .09f)));
                if (ColoredButton(Projection.WaveActionRect, WaveActionLabel(state.Phase), new Color(.93f, .62f, .2f)))
                    SetStatus(_game.StartWave(out var reason), reason);
                return;
            }
            GUI.Label(strip, text, Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.24f, .16f, .09f)));
        }

        private void DrawBuildPanel()
        {
            DrawPanel(BuildRect, new Color(.99f, .97f, .89f));
            GUI.Label(new Rect(BuildRect.x + 8f, BuildRect.y + 5f, 180f, 20f), "构筑与扩建", _heading);
            DrawTools();
            DrawNursery();
            DrawSelectedPlant();
        }

        private void DrawTools()
        {
            DrawToolButton(WeaponToolRect(WeaponKind.Gatling), WeaponKind.Gatling, _game.State.Inventory.Gatling);
            DrawToolButton(WeaponToolRect(WeaponKind.Ice), WeaponKind.Ice, _game.State.Inventory.Ice);
            DrawToolButton(WeaponToolRect(WeaponKind.Chili), WeaponKind.Chili, _game.State.Inventory.Chili);
            var draggingPot = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot;
            var potColor = _potToolSelected || draggingPot ? new Color(1f, .83f, .32f) : new Color(.84f, .9f, .66f);
            var potRect = PotToolRect();
            DrawRect(potRect, potColor);
            DrawTempSprite(new Rect(potRect.x + 6f, potRect.y + 5f, 36f, 36f), TempSprite.EmptyPot);
            GUI.Label(new Rect(potRect.x + 43f, potRect.y + 3f, 41f, 44f), "花盆\n×" + _game.State.Inventory.Pots, _small);
            if (GUI.Button(potRect, GUIContent.none, GUIStyle.none) && _game.State.Inventory.Pots > 0)
            {
                TogglePotTool();
            }
        }

        private void DrawToolButton(Rect rect, WeaponKind weapon, int count)
        {
            var selected = _selectedWeapon == weapon;
            var dragging = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon && _drag.Weapon == weapon;
            var color = selected || dragging ? new Color(1f, .83f, .3f) : new Color(.82f, .89f, .7f);
            DrawRect(rect, color);
            DrawTempSprite(new Rect(rect.x + 6f, rect.y + 5f, 36f, 36f), WeaponSprite(weapon));
            GUI.Label(new Rect(rect.x + 43f, rect.y + 3f, 41f, 44f), "×" + count, _small);
            if (!GUI.Button(rect, GUIContent.none, GUIStyle.none) || count <= 0) return;
            ToggleWeaponSelection(weapon);
        }

        private void DrawNursery()
        {
            GUI.Label(new Rect(BuildRect.x + 8f, BuildRect.y + 84f, 180f, 20f), "刷新栏", _body);
            for (var slot = 0; slot < 5; slot++)
            {
                var rect = NurseryRect(slot);
                var plant = _game.PlantAtNursery(slot);
                var target = new DropTarget { Type = DropTargetType.Nursery, Slot = slot, Rect = rect };
                var targetHovered = IsCurrentDropTarget(target);
                if (plant == null)
                {
                    if (targetHovered) DrawRect(Grow(rect, 2f), DropHighlightColor(target));
                    DrawRect(rect, new Color(.91f, .87f, .77f));
                    var showingPotReward = Time.unscaledTime < _nurseryRollDisplayUntil
                        && _game.LastNurseryPotSlots.Contains(slot);
                    if (showingPotReward)
                    {
                        DrawTempSprite(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, 39f), TempSprite.EmptyPot);
                        GUI.Label(new Rect(rect.x + 2f, rect.y + 42f, rect.width - 4f, 18f), "花盆入库", _tiny);
                    }
                    else
                        GUI.Label(rect, "空位", _small);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        SetStatus(false, DestinationDragGuidance(_game.PlantById(_inspectedPlantId), true));
                    continue;
                }
                var selected = plant.Id == _inspectedPlantId;
                var color = selected ? new Color(1f, .83f, .31f) : new Color(.96f, .93f, .82f);
                if (targetHovered) DrawRect(Grow(rect, 2f), DropHighlightColor(target));
                if (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon)
                {
                    var plantTarget = new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = rect };
                    if (IsCurrentDropTarget(plantTarget)) DrawRect(Grow(rect, 2f), DropHighlightColor(plantTarget));
                }
                if (plant.Id == _returnPulsePlantId && Time.unscaledTime < _returnPulseUntil)
                    DrawRect(Grow(rect, 2f), Color.Lerp(new Color(1f, .96f, .35f), Color.white, Mathf.PingPong(Time.unscaledTime * 8f, 1f)));
                DrawRect(rect, color);
                DrawTempSprite(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, 42f), PlantSprite(plant.Kind));
                GUI.Label(new Rect(rect.x + 2f, rect.y + 44f, rect.width - 4f, 16f), new string('★', plant.Star), _tiny);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    HandlePlantClick(plant);
            }
            var cost = GameConfig.RefreshCost(_game.State.RefreshCount);
            if (ColoredButton(RefreshRect(), "刷新五株水果 · 消耗 " + cost + " 阳光", new Color(.47f, .7f, .32f)))
                RefreshNurseryFromUi();
        }

        private void RefreshNurseryFromUi()
        {
            var success = _game.RefreshNursery(out var reason);
            if (success)
            {
                _nurseryRollDisplayUntil = Time.unscaledTime + 1.8f;
                if (_inspectedPlantId >= 0 && _game.PlantById(_inspectedPlantId) == null) _inspectedPlantId = -1;
            }
            SetStatus(success, reason);
        }

        private void DrawSelectedPlant()
        {
            var plant = _game.PlantById(_inspectedPlantId);
            if (plant == null)
            {
                DrawRect(new Rect(DetailRect.x, DetailRect.y, DetailRect.width, 48f), new Color(.93f, .89f, .78f));
                GUI.Label(new Rect(DetailRect.x + 8f, DetailRect.y + 4f, DetailRect.width - 16f, 40f),
                    "点击水果查看信息和攻击范围；拖动完成种植、移动、返回与合成。", _body);
                return;
            }
            DrawPanel(DetailRect, new Color(.93f, .89f, .78f));
            var stats = GameConfig.Plant(plant.Kind);
            GUI.Label(new Rect(DetailRect.x + 8f, DetailRect.y + 4f, 270f, 22f), stats.Name + " · " + plant.Star + " 星", _heading);
            GUI.Label(new Rect(DetailRect.x + 8f, DetailRect.y + 27f, 315f, 20f), stats.Description, _body);
            var effectiveRange = EffectiveAttackRange(plant);
            var rangeText = effectiveRange > .0001f
                ? Mathf.RoundToInt(GameConfig.LegacyDistance(effectiveRange)).ToString()
                : "无攻击范围";
            GUI.Label(new Rect(DetailRect.x + 8f, DetailRect.y + 50f, 360f, 34f),
                "伤害 " + Mathf.RoundToInt(stats.Damage * GameConfig.StarDamage(plant.Star))
                + " · 范围 " + rangeText
                + " · 装备 " + GameConfig.WeaponName(plant.Weapon), _small);
            if (ColoredButton(new Rect(DetailRect.xMax - 52f, DetailRect.y + 4f, 44f, 44f), "关闭", new Color(.85f, .78f, .65f)))
                _inspectedPlantId = -1;
        }

        private void DrawStatusPanel()
        {
            DrawRect(StatusRect, new Color(.90f, .85f, .72f));
            GUI.Label(new Rect(StatusRect.x + 8f, StatusRect.y + 3f, 90f, 20f), "操作提示", _heading);
            var status = Time.unscaledTime < _statusUntil ? _status
                : "点击查看信息；拖动水果可种植、移动、返回苗圃或合成。";
            GUI.Label(new Rect(StatusRect.x + 8f, StatusRect.y + 22f, StatusRect.width - 16f, 24f), status, _body);
        }

        private bool IsCurrentDropTarget(DropTarget candidate)
        {
            if (_drag == null || !_drag.Active) return false;
            var current = FindDropTargetAt(_drag, _drag.Current);
            if (candidate.Type != current.Type) return false;
            switch (candidate.Type)
            {
                case DropTargetType.Pot:
                case DropTargetType.Plant: return candidate.Id == current.Id;
                case DropTargetType.Nursery: return candidate.Slot == current.Slot;
                case DropTargetType.Expansion: return candidate.Cell == current.Cell;
                default: return false;
            }
        }

        private Color DropHighlightColor(DropTarget target)
        {
            if (_drag == null) return new Color(.58f, .42f, .24f);
            if (_drag.Type == DragPayloadType.Plant)
            {
                var status = target.Type == DropTargetType.Pot
                    ? _game.GetPlantDropStatus(_drag.PlantId, target.Id)
                    : target.Type == DropTargetType.Nursery
                        ? _game.GetNurseryDropStatus(_drag.PlantId, target.Slot)
                        : new PlantDropStatus(false, PlantDropAction.Invalid, "无效目标");
                if (status.Action == PlantDropAction.Merge && status.Legal) return new Color(1f, .74f, .08f);
                if (status.Legal) return new Color(.28f, .85f, .28f);
                if (status.Action == PlantDropAction.Cancel) return new Color(.75f, .72f, .62f);
                return new Color(.94f, .25f, .2f);
            }
            var interaction = DragTargetStatus(_drag, target);
            return interaction.Legal ? new Color(.28f, .85f, .28f) : new Color(.94f, .25f, .2f);
        }

        private void DrawDragGhost()
        {
            if (_drag == null || !_drag.Active) return;
            var currentTarget = FindDropTargetAt(_drag, _drag.Current);
            var status = DragTargetStatus(_drag, currentTarget);
            var rect = DragGeometry.PreviewRect(_drag.Current);
            if (currentTarget.Type != DropTargetType.None)
                rect.center = Vector2.Lerp(rect.center, currentTarget.Rect.center, .42f);
            rect.center = new Vector2(
                Mathf.Clamp(rect.center.x, 24f, DesignWidth - 24f),
                Mathf.Clamp(rect.center.y, 24f, DesignHeight - 24f));
            var border = currentTarget.Type == DropTargetType.None
                ? new Color(.78f, .4f, .3f)
                : DropHighlightColor(currentTarget);
            DrawRect(Grow(rect, 3f), new Color(border.r, border.g, border.b, .95f));
            DrawRect(rect, new Color(.22f, .35f, .2f, .9f));

            if (_drag.Type == DragPayloadType.Plant)
            {
                var plant = _game.PlantById(_drag.PlantId);
                if (plant != null) DrawTempSprite(rect, PlantSprite(plant.Kind));
            }
            else if (_drag.Type == DragPayloadType.Weapon)
                DrawTempSprite(rect, WeaponSprite(_drag.Weapon));
            else
                DrawTempSprite(rect, TempSprite.EmptyPot);

            var hintRect = new Rect(Mathf.Clamp(rect.center.x - 140f, 8f, DesignWidth - 288f), rect.yMax + 6f, 280f, 30f);
            DrawRect(hintRect, new Color(.13f, .1f, .07f, .86f));
            GUI.Label(hintRect, status.Reason, Style(13, FontStyle.Bold, TextAnchor.MiddleCenter,
                status.Legal ? new Color(.76f, 1f, .66f) : new Color(1f, .72f, .65f)));
        }

        private void DrawOverlay()
        {
            var phase = _game.State.Phase;
            if (_game.State.Paused && phase != GamePhase.Victory && phase != GamePhase.Defeat)
            {
                DrawModal("游戏暂停", "按空格或选择操作", "继续游戏", () => _game.TogglePause(), "重新开始", RestartRun);
            }
            else if (phase == GamePhase.Victory)
            {
                DrawModal("果园守住了！", "成功抵御全部 15 波僵尸", "重新开始", RestartRun);
            }
            else if (phase == GamePhase.Defeat)
            {
                DrawModal("僵尸闯进果园了", "坚持到第 " + _game.State.WaveIndex + " 波", "重新开始", RestartRun);
            }
        }

        private void DrawModal(
            string title,
            string message,
            string primaryAction,
            Action primaryCallback,
            string secondaryAction = null,
            Action secondaryCallback = null)
        {
            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(.12f, .09f, .06f, .68f));
            DrawPanel(ModalRect, new Color(1f, .97f, .86f));
            GUI.Label(new Rect(52f, 326f, 298f, 52f), title,
                Style(24, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.24f, .15f, .08f)));
            GUI.Label(new Rect(60f, 390f, 282f, 52f), message, _center);
            var actionCount = secondaryCallback == null ? 1 : 2;
            if (ColoredButton(ModalActionRect(0, actionCount), primaryAction, new Color(.43f, .67f, .3f)))
                primaryCallback();
            if (actionCount == 2
                && ColoredButton(ModalActionRect(1, actionCount), secondaryAction, new Color(.86f, .35f, .3f)))
                secondaryCallback();
        }

        private static int ModalActionCount(GamePhase phase, bool paused)
        {
            if (phase == GamePhase.Victory || phase == GamePhase.Defeat) return 1;
            return paused ? 2 : 0;
        }

        private static Rect ModalActionRect(int index, int actionCount)
        {
            if (actionCount <= 1) return new Rect(92f, 466f, 218f, 52f);
            return new Rect(index == 0 ? 54f : 206f, 466f, 142f, 52f);
        }

        private void RestartRun()
        {
            if (!RestartCurrentSession(out var errorCode))
                SetStatus(false, errorCode);
        }

        private void ResetInteractionState()
        {
            ApplyRestartPresentation(new RestartPresentationState());
        }

        private RestartPresentationState CaptureRestartPresentation()
        {
            return new RestartPresentationState
            {
                InspectedPlantId = _inspectedPlantId,
                SelectedWeapon = _selectedWeapon,
                PotToolSelected = _potToolSelected,
                Status = _status,
                StatusUntil = _statusUntil,
                Drag = _drag,
                DragControlId = _dragControlId,
                ReturnPulsePlantId = _returnPulsePlantId,
                ReturnPulseUntil = _returnPulseUntil,
                NurseryRollDisplayUntil = _nurseryRollDisplayUntil,
            };
        }

        private void ApplyRestartPresentation(RestartPresentationState presentation)
        {
            _inspectedPlantId = presentation.InspectedPlantId;
            _selectedWeapon = presentation.SelectedWeapon;
            _potToolSelected = presentation.PotToolSelected;
            _status = presentation.Status;
            _statusUntil = presentation.StatusUntil;
            _drag = presentation.Drag;
            _dragControlId = presentation.DragControlId;
            _returnPulsePlantId = presentation.ReturnPulsePlantId;
            _returnPulseUntil = presentation.ReturnPulseUntil;
            _nurseryRollDisplayUntil = presentation.NurseryRollDisplayUntil;
        }

        private static void ResetFullRun(
            GameSimulation simulation,
            RestartPresentationState presentation,
            int seed)
        {
            simulation.Reset(seed);
            presentation.InspectedPlantId = -1;
            presentation.SelectedWeapon = WeaponKind.None;
            presentation.PotToolSelected = false;
            presentation.Status = DefaultStatus;
            presentation.StatusUntil = 0f;
            presentation.Drag = null;
            presentation.DragControlId = 0;
            presentation.ReturnPulsePlantId = -1;
            presentation.ReturnPulseUntil = 0f;
            presentation.NurseryRollDisplayUntil = 0f;
        }

        private static Texture2D CreateAttackRangeTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PlantAttackRange",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var normalized = new Vector2((x + .5f) / size * 2f - 1f, (y + .5f) / size * 2f - 1f);
                var distance = normalized.magnitude;
                if (distance > 1f) continue;
                var edge = Mathf.InverseLerp(.88f, 1f, distance);
                pixels[y * size + x] = new Color(.98f, .86f, .2f, Mathf.Lerp(.12f, .42f, edge));
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void DrawTempSprite(Rect rect, TempSprite sprite)
        {
            DrawAtlasSprite(_tempArtAtlas, rect, (int)sprite, Color.white);
        }

        private void DrawTempSprite(Rect rect, TempSprite sprite, Color tint)
        {
            DrawAtlasSprite(_tempArtAtlas, rect, (int)sprite, tint);
        }

        private void DrawVfxSprite(Rect rect, CombatSprite sprite)
        {
            DrawAtlasSprite(_combatVfxAtlas, rect, (int)sprite, Color.white);
        }

        private void DrawVfxSprite(Rect rect, CombatSprite sprite, Color tint)
        {
            DrawAtlasSprite(_combatVfxAtlas, rect, (int)sprite, tint);
        }

        private static void DrawAtlasSprite(Texture2D atlas, Rect rect, int index, Color tint)
        {
            if (atlas == null) return;
            const float cell = .25f;
            const float inset = .004f;
            var column = index % 4;
            var rowFromTop = index / 4;
            var uv = new Rect(
                column * cell + inset,
                1f - (rowFromTop + 1) * cell + inset,
                cell - inset * 2f,
                cell - inset * 2f);
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, atlas, uv, true);
            GUI.color = previous;
        }

        private void DrawAnimatedPlant(Rect rect, Plant plant)
        {
            var angle = 0f;
            if (plant.ActionUntil > _game.State.Elapsed && plant.ActionUntil > plant.ActionStartedAt)
            {
                var progress = Mathf.Clamp01((_game.State.Elapsed - plant.ActionStartedAt) / (plant.ActionUntil - plant.ActionStartedAt));
                var pulse = Mathf.Sin(progress * Mathf.PI);
                switch (plant.Kind)
                {
                    case PlantKind.Pea:
                        rect.position -= new Vector2(plant.Facing.x * 3f, plant.Facing.y * 2f) * pulse;
                        break;
                    case PlantKind.Watermelon:
                        rect = ScaleAroundCenter(rect, 1f + pulse * .16f, 1f - pulse * .11f);
                        rect.y += pulse * 2f;
                        break;
                    case PlantKind.Banana:
                        angle = Mathf.Lerp(-18f, 26f, progress);
                        rect = ScaleAroundCenter(rect, 1f + pulse * .1f, 1f + pulse * .1f);
                        break;
                    case PlantKind.Durian:
                        rect.y -= pulse * 4f;
                        rect = ScaleAroundCenter(rect, 1f + pulse * .12f, 1f - pulse * .08f);
                        break;
                    case PlantKind.Sunflower:
                        angle = Mathf.Sin(progress * Mathf.PI * 2f) * 8f;
                        rect = ScaleAroundCenter(rect, 1f + pulse * .2f, 1f + pulse * .2f);
                        break;
                }
            }
            var previousMatrix = GUI.matrix;
            if (Mathf.Abs(angle) > .01f) GUIUtility.RotateAroundPivot(angle, rect.center);
            DrawTempSprite(rect, PlantSprite(plant.Kind));
            GUI.matrix = previousMatrix;
        }

        private void DrawRotatedVfx(Rect rect, CombatSprite sprite, float angle, Color tint)
        {
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, rect.center);
            DrawVfxSprite(rect, sprite, tint);
            GUI.matrix = previousMatrix;
        }

        private Rect CenteredRect(Vector2 center, float size)
        {
            size = Projection.LegacyVisualSize(size);
            return new Rect(center.x - size * .5f, center.y - size * .5f, size, size);
        }

        private static Rect ScaleAroundCenter(Rect rect, float scaleX, float scaleY)
        {
            var center = rect.center;
            rect.width *= scaleX;
            rect.height *= scaleY;
            rect.center = center;
            return rect;
        }

        private static TempSprite PlantSprite(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Watermelon: return TempSprite.Watermelon;
                case PlantKind.Banana: return TempSprite.Banana;
                case PlantKind.Durian: return TempSprite.Durian;
                case PlantKind.Sunflower: return TempSprite.Sunflower;
                default: return TempSprite.Pea;
            }
        }

        private static TempSprite ZombieSprite(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Runner: return TempSprite.Runner;
                case ZombieKind.Armored: return TempSprite.Armored;
                case ZombieKind.Boss: return TempSprite.Boss;
                default: return TempSprite.Zombie;
            }
        }

        private static TempSprite WeaponSprite(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.Ice: return TempSprite.Ice;
                case WeaponKind.Chili: return TempSprite.Chili;
                default: return TempSprite.Gatling;
            }
        }

        private void SetStatus(bool success, string text)
        {
            _status = (success ? "✓ " : "! ") + text;
            _statusUntil = Time.unscaledTime + 2.6f;
        }

        private Vector2 ToBoard(Vector2 point)
        {
            return Projection.MapToScreen(point);
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            DrawRect(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), new Color(.34f, .25f, .15f));
            DrawRect(rect, color);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private bool ColoredButton(Rect rect, string text, Color color)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            var clicked = GUI.Button(rect, text, _button);
            GUI.backgroundColor = previous;
            return clicked;
        }

        private static void DrawLine(Vector2 start, Vector2 end, float width, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            var angle = Vector2.SignedAngle(Vector2.right, end - start);
            var length = Vector2.Distance(start, end);
            var pivot = (start + end) * .5f;
            var previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot);
            DrawRect(new Rect(pivot.x - length * .5f, pivot.y - width * .5f, length, width), color);
            GUI.matrix = previousMatrix;
        }
    }
}
