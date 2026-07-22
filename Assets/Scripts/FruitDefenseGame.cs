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
        private static readonly Rect BattleSurfaceRect = new Rect(0f, 72f, 402f, 798f);
        private static readonly Rect BoardRect = new Rect(0f, 72f, 402f, 500f);
        private static readonly Rect ToolTrayRect = new Rect(8f, 580f, 386f, 68f);
        private static readonly Rect NurseryTrayRect = new Rect(8f, 656f, 386f, 80f);
        private static readonly Rect RefreshActionRect = new Rect(8f, 744f, 386f, 44f);
        private static readonly Rect DetailRect = new Rect(8f, 796f, 386f, 70f);
        private static readonly Rect ModalRect = new Rect(36f, 300f, 330f, 244f);
        private const float MergeHintMinWidth = 92f;
        private const float MergeHintMaxWidth = 160f;
        private const float MergeHintHeight = 24f;

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
        [SerializeField] private BattlefieldTerrainPalette[] battlefieldTerrainPalettes =
            Array.Empty<BattlefieldTerrainPalette>();
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
        public ResolvedLevelDefinition ActiveLevel { get { return _game == null ? null : _game.ActiveLevel; } }
        public BattleLaunchRequest CurrentRequest { get { return _currentRequest; } }
        public bool IsInitialized { get { return _isInitialized; } }
        public bool HasSubmittedResult { get { return _resultSubmitted; } }
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
        public string LastResultSubmissionError { get; private set; } = string.Empty;
        public static int ActiveSessionHostCount { get; private set; }

        public void ConfigureBattlefieldTerrain(IEnumerable<BattlefieldTerrainPalette> palettes)
        {
            battlefieldTerrainPalettes = (palettes ?? Enumerable.Empty<BattlefieldTerrainPalette>()).ToArray();
        }

        public bool ValidateBattlefieldTerrain(out string reason)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var palette in BattlefieldTerrainPalettes)
            {
                if (!BattlefieldDualGridTerrain.Validate(palette, out reason)) return false;
                if (!ids.Add(palette.PaletteId))
                {
                    reason = "Battlefield terrain palette identity is duplicated: " + palette.PaletteId;
                    return false;
                }
            }
            var required = BundledLevelCatalogFactory.CreateSource().TerrainPaletteIds;
            if (required.Any(id => !ids.Contains(id)))
            {
                reason = "A bundled level theme references an unregistered battlefield terrain palette.";
                return false;
            }
            reason = "ok";
            return true;
        }

        public bool TryResolveBattlefieldTerrainPalette(string paletteId,
            out BattlefieldTerrainPalette palette, out string reason)
        {
            palette = BattlefieldTerrainPalettes.FirstOrDefault(value => value != null
                && string.Equals(value.PaletteId, paletteId, StringComparison.Ordinal));
            if (palette == null)
            {
                reason = "Battlefield terrain palette is not registered: " + (paletteId ?? string.Empty);
                return false;
            }
            return BattlefieldDualGridTerrain.Validate(palette, out reason);
        }

        private DualGridTileSet DefaultTerrainTileSet(string surfaceId)
        {
            if (BattlefieldTerrainPalettes.Count == 0 || BattlefieldTerrainPalettes[0] == null) return null;
            DualGridTileSet tileSet;
            return BattlefieldTerrainPalettes[0].TryGetTileSet(surfaceId, out tileSet) ? tileSet : null;
        }

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

            var plantHint = new PlantDropStatus(true, PlantDropAction.Plant, "可种植");
            var moveHint = new PlantDropStatus(true, PlantDropAction.Move, "可移动");
            var invalidHint = new PlantDropStatus(false, PlantDropAction.Invalid, "无效目标");
            var mergeHint = new PlantDropStatus(true, PlantDropAction.Merge, "可合成为 2 星");
            if (ShouldShowMergeHint(DragPayloadType.Plant, plantHint)
                || ShouldShowMergeHint(DragPayloadType.Plant, moveHint)
                || ShouldShowMergeHint(DragPayloadType.Plant, invalidHint)
                || ShouldShowMergeHint(DragPayloadType.Weapon, mergeHint)
                || !ShouldShowMergeHint(DragPayloadType.Plant, mergeHint))
            {
                reason = "floating drag hint is not limited to legal plant merges";
                return false;
            }

            var compactHint = MergeHintRect(new Rect(180f, 180f, 48f, 48f), 118f);
            if (compactHint.width < MergeHintMinWidth || compactHint.width > MergeHintMaxWidth
                || compactHint.width >= 280f || compactHint.height != MergeHintHeight
                || compactHint.height >= 30f || compactHint.xMin < 8f || compactHint.xMax > DesignWidth - 8f)
            {
                reason = "merge hint frame is not compact or contained";
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
            var topLevelRegions = new[] { HeaderRect, BattleSurfaceRect };
            foreach (var region in topLevelRegions)
            {
                if (!ContainsRect(design, region))
                {
                    reason = "region outside design bounds: " + region;
                    return false;
                }
            }

            if (HeaderRect.yMax > BattleSurfaceRect.yMin)
            {
                reason = "header overlaps the embedded battle surface";
                return false;
            }

            var embeddedRegions = new[]
            {
                BoardRect, ToolTrayRect, NurseryTrayRect, RefreshRect(), DetailRect,
            };
            foreach (var region in embeddedRegions)
            {
                if (!ContainsRect(BattleSurfaceRect, region))
                {
                    reason = "embedded region leaves the battle surface: " + region;
                    return false;
                }
            }

            for (var index = 1; index < embeddedRegions.Length; index++)
            {
                if (embeddedRegions[index - 1].yMax > embeddedRegions[index].yMin)
                {
                    reason = "embedded battle regions overlap vertically";
                    return false;
                }
            }

            if (BoardRect.width < DesignWidth || BoardRect.height <= 398f)
            {
                reason = "battlefield did not reach the enlarged full-width target";
                return false;
            }

            var projection = new BattlefieldProjection(GameConfig.DefaultBattlefield, BoardRect);
            var legacyProjection = new BattlefieldProjection(GameConfig.DefaultBattlefield,
                new Rect(4f, 76f, 394f, 398f));
            if (!projection.ValidatePlantingGeometry(out reason)) return false;
            if (!projection.ValidateControlInset(out reason)) return false;
            if (projection.TileSize <= legacyProjection.TileSize)
            {
                reason = "enlarged battlefield did not increase projected tile size";
                return false;
            }
            if (GameConfig.DefaultBattlefield.PlantableCells.Count != 35)
            {
                reason = "default battlefield does not expose 35 plantable cells";
                return false;
            }
            var sampleCell = GameConfig.DefaultBattlefield.PlantableCells[0];
            var hitRect = projection.PotHitRect(sampleCell);
            var visualRect = projection.PotVisualRect(sampleCell);
            if (Mathf.Abs(visualRect.width / hitRect.width - BattlefieldProjection.PotVisualRatio) > .001f
                || BattlefieldProjection.PotVisualRatio < .85f
                || BattlefieldProjection.PotVisualRatio > .92f
                || visualRect.xMin < hitRect.xMin || visualRect.yMin < hitRect.yMin
                || visualRect.xMax > hitRect.xMax || visualRect.yMax > hitRect.yMax)
            {
                reason = "frameless flowerpot visual is not near-cell-size or contained";
                return false;
            }
            var nurseryCell = NurseryRect(0);
            var nurseryIcon = FramelessSlotIconRect(nurseryCell);
            var potToolCell = PotToolRect();
            var potToolIcon = PotToolIconRect();
            if (!ContainsRect(nurseryCell, nurseryIcon)
                || nurseryIcon.width / nurseryCell.width < .9f
                || nurseryIcon.height / nurseryCell.height < .9f
                || !ContainsRect(potToolCell, potToolIcon)
                || potToolIcon.height / potToolCell.height < .9f)
            {
                reason = "frameless tray icon does not nearly fill its logical cell";
                return false;
            }
            var offsetSample = new Rect(100f, 100f, 40f, 40f);
            var zeroOffset = ApplyPlantVisualHeight(offsetSample, 0f);
            var unitOffset = ApplyPlantVisualHeight(offsetSample, 1f);
            if (zeroOffset.center != offsetSample.center
                || Mathf.Abs(unitOffset.center.x - offsetSample.center.x) > .001f
                || Mathf.Abs(unitOffset.center.y - (offsetSample.center.y - 1f)) > .001f)
            {
                reason = "plant visual height does not preserve exact zero/unit center semantics";
                return false;
            }
            foreach (var routePoint in projection.RoutePoints)
            {
                if (!projection.GridRect.Contains(routePoint))
                {
                    reason = "projected route leaves the square tile grid";
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
                DetailCloseRect(),
                NurseryRect(0), NurseryRect(1), NurseryRect(2), NurseryRect(3), NurseryRect(4),
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
            if (!TryValidateInitialization(request, navigator, resultSink, out var failure)) return failure;

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

            return CompleteInitialization(request, navigator, resultSink, simulation);
        }

        public BattleSessionInitializationResult Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            ResolvedLevelDefinition resolvedLevel)
        {
            if (!TryValidateInitialization(request, navigator, resultSink, out var failure)) return failure;
            if (resolvedLevel == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.ResolvedLevelRequired);
            if (!string.Equals(request.LevelId, resolvedLevel.Identity.LevelId, StringComparison.Ordinal))
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.ResolvedLevelMismatch);
            if (resolvedLevel.BattleContent == null
                || !string.Equals(request.ContentVersion,
                    resolvedLevel.BattleContent.Header.contentVersion, StringComparison.Ordinal))
            {
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.ResolvedContentMismatch);
            }

            GameSimulation simulation;
            try
            {
                simulation = new GameSimulation(resolvedLevel, request.Seed);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.SimulationConstructionFailed);
            }

            return CompleteInitialization(request, navigator, resultSink, simulation);
        }

        private bool TryValidateInitialization(BattleLaunchRequest request,
            IAppNavigator navigator, IBattleResultSink resultSink,
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

            failure = default;
            return true;
        }

        private BattleSessionInitializationResult CompleteInitialization(BattleLaunchRequest request,
            IAppNavigator navigator, IBattleResultSink resultSink, GameSimulation simulation)
        {

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

        public BattleSnapshotV2 ExportCurrentSessionSnapshotV2(CompiledLevelCatalog levelCatalog)
        {
            if (!_isInitialized || _game == null)
                throw new InvalidOperationException(SessionNotInitialized);
            return _game.ExportSnapshotV2(levelCatalog);
        }

        public BattleSnapshotRestoreResult RestoreCurrentSessionSnapshotV2(
            BattleSnapshotV2 snapshot, CompiledLevelCatalog levelCatalog)
        {
            if (!_isInitialized || _game == null)
                return new BattleSnapshotRestoreResult(BattleSnapshotRestoreCode.InvalidPayload,
                    "session", SessionNotInitialized);

            var result = _game.RestoreSnapshotV2(snapshot, levelCatalog);
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
            _game.Reset(20260714);
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
                    GetAcceptanceAdjacentCells(out var firstAdjacent, out var secondAdjacent);
                    AddAcceptancePot(firstAdjacent, PlantKind.Pea);
                    AddAcceptancePot(secondAdjacent, PlantKind.Watermelon);
                    break;
                case "drag-target":
                    AddAcceptancePot(GetAcceptanceCell(0));
                    _game.State.Plants.Add(new Plant
                    {
                        Id = _game.State.NextId++, Kind = PlantKind.Pea, Star = 1, NurseryIndex = 0,
                    });
                    break;
                case "selection-inspection":
                    AddAcceptancePot(GetAcceptanceCell(0), PlantKind.Pea);
                    AddAcceptancePot(GetAcceptanceCell(1));
                    _game.State.Plants.Add(new Plant
                    {
                        Id = _game.State.NextId++, Kind = PlantKind.Sunflower, Star = 1, NurseryIndex = 0,
                    });
                    break;
                case "active-wave":
                    AddAcceptancePot(GetAcceptanceCell(0), PlantKind.Pea);
                    AddAcceptancePot(GetAcceptanceCell(_game.Map.PlantableCells.Count - 1), PlantKind.Watermelon);
                    _game.StartWave(out _);
                    break;
                case "between-wave":
                    AddAcceptancePot(GetAcceptanceCell(0), PlantKind.Pea);
                    AddAcceptancePot(GetAcceptanceCell(_game.Map.PlantableCells.Count - 1), PlantKind.Watermelon);
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
                    for (var index = 0; index < Mathf.Min(8, _game.Map.PlantableCells.Count); index++)
                        AddAcceptancePot(GetAcceptanceCell(index));
                    break;
            }
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
            var safeArea = RuntimeSafeAreaResolver.ResolveCurrent();
            var viewportLayout = BattlefieldProjection.CalculateViewportLayout(
                Screen.width, Screen.height, safeArea, DesignWidth, DesignHeight);
            GUI.matrix = Matrix4x4.identity;
            var background = ThemeColor(theme => theme.BackgroundColor, new Color(.91f, .86f, .75f));
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), background);
            GUI.matrix = viewportLayout.GuiMatrix;
            HandleDragInput(Event.current);
            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight), background);
            DrawHeader();
            DrawBoard();
            DrawEmbeddedBattleControls();
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
                    var rect = PotHitRect(pot);
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
                var status = PlantDragTargetStatus(session, target);
                return new InteractionStatus(status.Legal, status.Reason);
            }
            if (session.Type == DragPayloadType.Weapon)
                return target.Type == DropTargetType.Plant
                    ? _game.GetWeaponInstallStatus(target.Id, session.Weapon)
                    : new InteractionStatus(false, "请拖到一株植物上");
            return new InteractionStatus(target.Type == DropTargetType.Expansion && _game.CanExpand(target.Cell),
                target.Type == DropTargetType.Expansion && _game.CanExpand(target.Cell) ? "松开添加花盆" : "请拖到绿色扩建格");
        }

        private PlantDropStatus PlantDragTargetStatus(DragSession session, DropTarget target)
        {
            if (session == null || session.Type != DragPayloadType.Plant)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不能放置水果");
            return target.Type == DropTargetType.Pot
                ? _game.GetPlantDropStatus(session.PlantId, target.Id)
                : target.Type == DropTargetType.Nursery
                    ? _game.GetNurseryDropStatus(session.PlantId, target.Slot)
                    : new PlantDropStatus(false, PlantDropAction.Invalid, "这里不能放置水果");
        }

        private static bool ShouldShowMergeHint(DragPayloadType payloadType, PlantDropStatus status)
        {
            return payloadType == DragPayloadType.Plant
                && status.Legal
                && status.Action == PlantDropAction.Merge;
        }

        private static Rect MergeHintRect(Rect dragRect, float labelWidth)
        {
            var width = Mathf.Clamp(labelWidth + 20f, MergeHintMinWidth, MergeHintMaxWidth);
            var x = Mathf.Clamp(dragRect.center.x - width * .5f, 8f, DesignWidth - 8f - width);
            return new Rect(x, dragRect.yMax + 4f, width, MergeHintHeight);
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
            var width = (NurseryTrayRect.width - 16f - gap * 4f) / 5f;
            return new Rect(NurseryTrayRect.x + 8f + slot * (width + gap),
                NurseryTrayRect.y + 18f, width, NurseryTrayRect.height - 22f);
        }

        private static Rect WeaponToolRect(WeaponKind weapon)
        {
            var index = weapon == WeaponKind.Gatling ? 0 : weapon == WeaponKind.Ice ? 1 : 2;
            return ToolRect(index);
        }

        private static Rect PotToolRect() { return ToolRect(3); }

        private static Rect PotToolIconRect()
        {
            var rect = PotToolRect();
            var size = rect.height - 2f;
            return new Rect(rect.x + 1f, rect.y + 1f, size, size);
        }

        private static Rect ToolRect(int index)
        {
            const float gap = 5f;
            var width = (ToolTrayRect.width - 16f - gap * 3f) / 4f;
            return new Rect(ToolTrayRect.x + 8f + index * (width + gap),
                ToolTrayRect.y + 18f, width, ToolTrayRect.height - 22f);
        }

        private static Rect RefreshRect() { return RefreshActionRect; }

        private static Rect DetailCloseRect()
        {
            return new Rect(DetailRect.xMax - 48f, DetailRect.y + 4f, 44f, 44f);
        }

        private BattlefieldProjection Projection
        {
            get
            {
                if (_projection == null) _projection = new BattlefieldProjection(_game.Map, BoardRect);
                return _projection;
            }
        }

        private Rect ExpansionRect(Vector2Int cell) { return Projection.PotHitRect(cell); }

        private Rect PotHitRect(Pot pot)
        {
            return Projection.PotHitRect(pot.Cell);
        }
        private Rect PotVisualRect(Pot pot)
        {
            return Projection.PotVisualRect(pot.Cell);
        }
        private Rect PlantSourceRect(Plant plant)
        {
            if (plant.PotId >= 0)
            {
                var pot = _game.PotById(plant.PotId);
                return pot == null ? new Rect() : PotHitRect(pot);
            }
            return plant.NurseryIndex >= 0 ? NurseryRect(plant.NurseryIndex) : new Rect();
        }
        private static Rect Grow(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static Rect FramelessSlotIconRect(Rect rect) { return Grow(rect, -2f); }

        private static bool ContainsRect(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }

        private void DrawHeader()
        {
            DrawPanel(HeaderRect, new Color(.99f, .97f, .88f));
            GUI.Label(new Rect(16f, 12f, 118f, 24f), "水果塔防", _title);
            GUI.Label(new Rect(16f, 38f, 88f, 24f), "阳光 <b>" + _game.State.Sun + "</b>", _body);
            GUI.Label(new Rect(104f, 38f, 82f, 24f), "核心 <b>" + _game.State.Lives + "</b>", _body);
            GUI.Label(new Rect(186f, 38f, 76f, 24f), "波次 <b>" + _game.State.WaveIndex
                + "/" + _game.MaxWaves + "</b>", _body);
            if (ColoredButton(new Rect(274f, 16f, 52f, 44f), _game.State.Paused ? "继续" : "暂停", new Color(.96f, .84f, .48f)))
                _game.TogglePause();
            if (ColoredButton(new Rect(334f, 16f, 52f, 44f), _game.State.Speed + " 倍", new Color(.83f, .91f, .67f)))
                _game.SetSpeed(_game.State.Speed == 1 ? 2 : 1);
        }

        private void DrawBoard()
        {
            var ground = ThemeColor(theme => theme.GroundColor, new Color(.46f, .68f, .28f));
            DrawPanel(BattleSurfaceRect, Color.Lerp(ground, Color.black, .18f));
            var texturedTerrain = DrawBattlefieldTerrain();
            if (!texturedTerrain)
                DrawRect(new Rect(BoardRect.x + 6f, BoardRect.y + 6f,
                    BoardRect.width - 12f, BoardRect.height - 12f), ground);
            DrawRouteTiles(texturedTerrain);
            DrawCore();
            DrawPlantingCells(texturedTerrain);
            DrawInspectedAttackRange();
            if (_potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot)) DrawExpansionCandidates();
            DrawPotsAndPlants();
            DrawProjectiles();
            DrawZombies();
            DrawCombatEffects();
            DrawFeedback();
            DrawBoardStatus();
        }

        private bool DrawBattlefieldTerrain()
        {
            string reason;
            var theme = _game == null ? null : _game.Theme;
            BattlefieldTerrainPalette palette;
            if (theme == null || !TryResolveBattlefieldTerrainPalette(
                    theme.TerrainPaletteId, out palette, out reason)) return false;

            var map = _game.Map;
            var grid = Projection.GridRect;
            GUI.BeginGroup(grid);
            var previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(
                new Rect(0f, 0f, grid.width, grid.height),
                palette.SoilBaseTexture,
                BattlefieldDualGridTerrain.BaseTextureUv(
                    map, palette.ReferenceTileSet, palette.SoilBaseTexture),
                true);

            foreach (var binding in palette.SurfaceBindings)
                DrawBattlefieldTerrainLayer(map, grid, binding.TileSet, binding.SurfaceId);

            GUI.color = previous;
            GUI.EndGroup();
            return true;
        }

        private void DrawBattlefieldTerrainLayer(BattlefieldMapDefinition map, Rect grid,
            DualGridTileSet tileSet, string surfaceId)
        {
            for (var vertexY = 0; vertexY <= map.GridHeight; vertexY++)
            for (var vertexX = 0; vertexX <= map.GridWidth; vertexX++)
            {
                var mask = BattlefieldDualGridTerrain.ResolveMask(
                    map, vertexX, vertexY, surfaceId);
                Sprite sprite;
                if (mask == DualGridMask.Empty || !tileSet.TryGetSprite(mask, out sprite)) continue;
                var rect = BattlefieldDualGridTerrain.VisualTileRect(
                    Projection, vertexX, vertexY);
                rect.position -= grid.position;
                GUI.DrawTextureWithTexCoords(rect, sprite.texture,
                    BattlefieldDualGridTerrain.SpriteUv(sprite), true);
            }
        }

        private void DrawCore()
        {
            var rect = Projection.CoreRect;
            DrawRect(rect, ThemeColor(theme => theme.CoreColor, new Color(.71f, .79f, .45f)));
            GUI.Label(new Rect(rect.x, rect.y + rect.height * .08f, rect.width, rect.height * .55f), "♣",
                Style(17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(.19f, .38f, .16f)));
            GUI.Label(new Rect(rect.x, rect.center.y + rect.height * .08f, rect.width, 18f), "核心", _tiny);
        }

        private void DrawRouteTiles(bool texturedRoute)
        {
            var routeEdge = ThemeColor(theme => theme.RouteEdgeColor, new Color(.41f, .43f, .24f));
            var route = ThemeColor(theme => theme.RouteColor, new Color(.88f, .73f, .43f));
            var accent = ThemeColor(theme => theme.AccentColor, new Color(.9f, .38f, .24f));
            foreach (var descriptor in _game.Map.RouteTileDescriptors)
            {
                var rect = Projection.RouteTileRect(descriptor.Cell);
                if (!texturedRoute)
                {
                    DrawRect(rect, Color.Lerp(routeEdge, Color.black, .12f));
                    DrawRouteTileLayer(rect, descriptor.Connections, .62f, routeEdge);
                    DrawRouteTileLayer(rect, descriptor.Connections, .43f, route);
                }
                if (descriptor.Kind != BattlefieldRouteTileKind.Entry
                    && descriptor.Kind != BattlefieldRouteTileKind.Exit) continue;
                var markerSize = rect.width * .28f;
                var marker = new Rect(rect.center.x - markerSize * .5f, rect.center.y - markerSize * .5f,
                    markerSize, markerSize);
                DrawRect(marker, descriptor.Kind == BattlefieldRouteTileKind.Entry
                    ? new Color(.42f, .82f, .32f)
                    : accent);
            }
        }

        private static void DrawRouteTileLayer(
            Rect tile, BattlefieldRouteConnections connections, float widthRatio, Color color)
        {
            var width = tile.width * widthRatio;
            var half = width * .5f;
            var center = tile.center;
            DrawRect(new Rect(center.x - half, center.y - half, width, width), color);
            if ((connections & BattlefieldRouteConnections.North) != 0)
                DrawRect(new Rect(center.x - half, tile.yMin, width, center.y - tile.yMin), color);
            if ((connections & BattlefieldRouteConnections.East) != 0)
                DrawRect(new Rect(center.x, center.y - half, tile.xMax - center.x, width), color);
            if ((connections & BattlefieldRouteConnections.South) != 0)
                DrawRect(new Rect(center.x - half, center.y, width, tile.yMax - center.y), color);
            if ((connections & BattlefieldRouteConnections.West) != 0)
                DrawRect(new Rect(tile.xMin, center.y - half, center.x - tile.xMin, width), color);
        }

        private void DrawPlantingCells(bool texturedTerrain)
        {
            var expansionActive = _potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot);
            var plantable = ThemeColor(theme => theme.PlantableColor, new Color(.58f, .72f, .36f));
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
                    : new Color(plantable.r, plantable.g, plantable.b, .42f);
                if (texturedTerrain)
                {
                    border = active
                        ? new Color(.42f, .29f, .16f, .72f)
                        : expansionActive && legal
                            ? new Color(.72f, .9f, .32f, .95f)
                            : new Color(.25f, .28f, .25f, .32f);
                    fill = active
                        ? new Color(1f, 1f, 1f, .025f)
                        : expansionActive && legal
                            ? new Color(.58f, .78f, .32f, .2f)
                            : new Color(1f, 1f, 1f, .025f);
                }
                if (texturedTerrain)
                    DrawOutline(Grow(rect, .5f), 1f, border);
                else
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
                var hitRect = PotHitRect(pot);
                var rect = PotVisualRect(pot);
                var plant = _game.PlantAtPot(pot.Id);
                var selected = plant != null && plant.Id == _inspectedPlantId;
                var border = selected ? new Color(1f, .84f, .23f) : new Color(.58f, .42f, .24f);
                var target = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon && plant != null
                    ? new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = hitRect }
                    : new DropTarget { Type = DropTargetType.Pot, Id = pot.Id, Rect = hitRect };
                if (IsCurrentDropTarget(target)) border = DropHighlightColor(target);
                if (plant != null && plant.Id == _returnPulsePlantId && Time.unscaledTime < _returnPulseUntil)
                    border = Color.Lerp(new Color(1f, .96f, .35f), Color.white, Mathf.PingPong(Time.unscaledTime * 8f, 1f));
                var returning = plant != null && plant.Id == _returnPulsePlantId && Time.unscaledTime < _returnPulseUntil;
                if (selected || IsCurrentDropTarget(target) || returning)
                    DrawOutline(Grow(hitRect, -1f), 2f, border);
                DrawTempSprite(rect, plant == null ? TempSprite.EmptyPot : TempSprite.OccupiedPot);
                if (plant == null)
                {
                    if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none)) HandlePotClick(pot.Id);
                    continue;
                }
                if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none)) HandlePlantClick(plant);
                DrawAnimatedPlant(Grow(rect, 1f), plant);
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
                var visualRect = Projection.PotVisualRect(cell);
                var target = new DropTarget { Type = DropTargetType.Expansion, Cell = cell, Rect = rect };
                if (IsCurrentDropTarget(target))
                    DrawOutline(Grow(rect, -1f), 3f, legal ? new Color(.95f, .83f, .2f) : new Color(.9f, .25f, .2f));
                else
                    DrawOutline(Grow(rect, -1f), 1f,
                        legal ? new Color(.32f, .69f, .28f, .72f) : new Color(.42f, .36f, .34f, .72f));
                DrawTempSprite(visualRect, legal ? TempSprite.ExpansionPot : TempSprite.LockedPot);
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
                GUI.Label(BattlefieldFeedbackRect(Projection.GridRect, point), feedback.Text, style);
            }
        }

        public static Rect BattlefieldFeedbackRect(Rect gridRect, Vector2 point)
        {
            var width = Mathf.Min(90f, Mathf.Max(0f, gridRect.width));
            var height = Mathf.Min(18f, Mathf.Max(0f, gridRect.height));
            var x = Mathf.Clamp(point.x - width * .5f, gridRect.xMin, gridRect.xMax - width);
            var y = Mathf.Clamp(point.y - 28f, gridRect.yMin, gridRect.yMax - height);
            return new Rect(x, y, width, height);
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

        private void DrawEmbeddedBattleControls()
        {
            DrawRect(ToolTrayRect, new Color(.97f, .94f, .82f, .94f));
            GUI.Label(new Rect(ToolTrayRect.x + 8f, ToolTrayRect.y, 180f, 18f), "构筑栏", _small);
            DrawTools();
            DrawRect(NurseryTrayRect, new Color(.97f, .94f, .82f, .94f));
            GUI.Label(new Rect(NurseryTrayRect.x + 8f, NurseryTrayRect.y, 180f, 18f), "刷新栏", _small);
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
            if (_potToolSelected || draggingPot) DrawOutline(potRect, 2f, potColor);
            DrawTempSprite(PotToolIconRect(), TempSprite.EmptyPot);
            GUI.Label(new Rect(potRect.x + 47f, potRect.y + 3f, potRect.width - 49f, 44f), "花盆\n×" + _game.State.Inventory.Pots, _small);
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
            for (var slot = 0; slot < 5; slot++)
            {
                var rect = NurseryRect(slot);
                var plant = _game.PlantAtNursery(slot);
                var target = new DropTarget { Type = DropTargetType.Nursery, Slot = slot, Rect = rect };
                var targetHovered = IsCurrentDropTarget(target);
                if (plant == null)
                {
                    if (targetHovered) DrawOutline(Grow(rect, -1f), 2f, DropHighlightColor(target));
                    var showingPotReward = Time.unscaledTime < _nurseryRollDisplayUntil
                        && _game.LastNurseryPotSlots.Contains(slot);
                    if (showingPotReward)
                    {
                        DrawTempSprite(FramelessSlotIconRect(rect), TempSprite.EmptyPot);
                        GUI.Label(new Rect(rect.x + 2f, rect.yMax - 18f,
                            rect.width - 4f, 16f), "花盆入库", _tiny);
                    }
                    else
                        GUI.Label(rect, "空位", _small);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                        SetStatus(false, DestinationDragGuidance(_game.PlantById(_inspectedPlantId), true));
                    continue;
                }
                var selected = plant.Id == _inspectedPlantId;
                var showOutline = selected;
                var outline = new Color(1f, .83f, .31f);
                if (targetHovered)
                {
                    showOutline = true;
                    outline = DropHighlightColor(target);
                }
                if (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon)
                {
                    var plantTarget = new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = rect };
                    if (IsCurrentDropTarget(plantTarget))
                    {
                        showOutline = true;
                        outline = DropHighlightColor(plantTarget);
                    }
                }
                if (plant.Id == _returnPulsePlantId && Time.unscaledTime < _returnPulseUntil)
                {
                    showOutline = true;
                    outline = Color.Lerp(new Color(1f, .96f, .35f), Color.white, Mathf.PingPong(Time.unscaledTime * 8f, 1f));
                }
                if (showOutline) DrawOutline(Grow(rect, -1f), 2f, outline);
                DrawTempSprite(FramelessSlotIconRect(rect), PlantSprite(plant.Kind));
                GUI.Label(new Rect(rect.x + 2f, rect.yMax - 18f,
                    rect.width - 4f, 16f), new string('★', plant.Star), _tiny);
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
            if (plant == null) return;
            DrawPanel(DetailRect, new Color(.93f, .89f, .78f));
            var stats = GameConfig.Plant(plant.Kind);
            GUI.Label(new Rect(DetailRect.x + 8f, DetailRect.y + 4f,
                DetailRect.width - 64f, 22f), stats.Name + " · " + plant.Star + " 星", _heading);
            var effectiveRange = EffectiveAttackRange(plant);
            var rangeText = effectiveRange > .0001f
                ? Mathf.RoundToInt(GameConfig.LegacyDistance(effectiveRange)).ToString()
                : "无攻击范围";
            GUI.Label(new Rect(DetailRect.x + 8f, DetailRect.y + 28f,
                DetailRect.width - 64f, 34f),
                "伤害 " + Mathf.RoundToInt(stats.Damage * GameConfig.StarDamage(plant.Star))
                + " · 范围 " + rangeText
                + " · 装备 " + GameConfig.WeaponName(plant.Weapon), _small);
            if (ColoredButton(DetailCloseRect(), "关闭", new Color(.85f, .78f, .65f)))
                _inspectedPlantId = -1;
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
                var status = PlantDragTargetStatus(_drag, target);
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
            var rect = DragGeometry.PreviewRect(_drag.Current);
            if (currentTarget.Type != DropTargetType.None)
                rect.center = Vector2.Lerp(rect.center, currentTarget.Rect.center, .42f);
            rect.center = new Vector2(
                Mathf.Clamp(rect.center.x, 24f, DesignWidth - 24f),
                Mathf.Clamp(rect.center.y, 24f, DesignHeight - 24f));
            var border = currentTarget.Type == DropTargetType.None
                ? new Color(.78f, .4f, .3f)
                : DropHighlightColor(currentTarget);
            DrawOutline(rect, 2f, new Color(border.r, border.g, border.b, .95f));

            if (_drag.Type == DragPayloadType.Plant)
            {
                var plant = _game.PlantById(_drag.PlantId);
                if (plant != null) DrawTempSprite(rect, PlantSprite(plant.Kind));
            }
            else if (_drag.Type == DragPayloadType.Weapon)
                DrawTempSprite(rect, WeaponSprite(_drag.Weapon));
            else
                DrawTempSprite(rect, TempSprite.EmptyPot);

            var mergeStatus = _drag.Type == DragPayloadType.Plant
                ? PlantDragTargetStatus(_drag, currentTarget)
                : default(PlantDropStatus);
            if (!ShouldShowMergeHint(_drag.Type, mergeStatus)) return;

            var hintStyle = Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, .86f, .45f));
            var labelWidth = hintStyle.CalcSize(new GUIContent(mergeStatus.Reason)).x;
            var hintRect = MergeHintRect(rect, labelWidth);
            DrawRect(hintRect, new Color(.13f, .1f, .07f, .86f));
            GUI.Label(hintRect, mergeStatus.Reason, hintStyle);
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
                DrawModal("果园守住了！", "成功抵御全部 " + _game.MaxWaves + " 波僵尸",
                    "重新开始", RestartRun);
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
            rect = ApplyPlantVisualHeight(rect, PlantVisualHeightOffset(plant));
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

        private float PlantVisualHeightOffset(Plant plant)
        {
            if (plant == null || _game == null || _game.Content == null) return 0f;
            var contentId = string.IsNullOrEmpty(plant.ContentId)
                ? LegacyBattleContentIds.Plant(plant.Kind)
                : plant.ContentId;
            PlantDefinitionDto definition;
            return _game.Content.Plants.TryGetValue(contentId, out definition)
                ? definition.potVisualHeightOffset
                : 0f;
        }

        private static Rect ApplyPlantVisualHeight(Rect rect, float height)
        {
            var center = rect.center;
            center.y -= Mathf.Max(0f, height);
            rect.center = center;
            return rect;
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

        private static void DrawOutline(Rect rect, float thickness, Color color)
        {
            thickness = Mathf.Max(1f, Mathf.Min(thickness, Mathf.Min(rect.width, rect.height) * .5f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y + thickness, thickness, rect.height - thickness * 2f), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y + thickness, thickness, rect.height - thickness * 2f), color);
        }

        private Color ThemeColor(Func<LevelPresentationThemeDefinition, string> select,
            Color fallback)
        {
            var theme = _game == null ? null : _game.Theme;
            if (theme == null || select == null) return fallback;
            return ColorUtility.TryParseHtmlString(select(theme), out var color) ? color : fallback;
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
