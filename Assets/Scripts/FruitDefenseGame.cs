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
            public bool StatusSuccess = true;
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
                    && SelectedWeapon == WeaponKind.None
                    && !PotToolSelected
                    && Status == DefaultStatus
                    && StatusSuccess
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
        private const float TerrainTileSeamOverlap = .75f;
        public static int AttackRangeTextureSize => 1024;

        private GameSimulation _game;
        private readonly BattlePresentationBuffer _presentation = new BattlePresentationBuffer();
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
        private bool _acceptanceTerminalPreview;
        private Texture2D _tempArtAtlas;
        private Texture2D _combatVfxAtlas;
        private Texture2D _attackRangeTexture;
        [SerializeField] private BattlefieldTerrainPalette[] battlefieldTerrainPalettes =
            Array.Empty<BattlefieldTerrainPalette>();
        private BattleUiLayout _battleUiLayout;
        private GUIStyle _worldLabelStyle;
        private GUIStyle _terrainFailureStyle;
        private int _inspectedPlantId = -1;
        private WeaponKind _selectedWeapon = WeaponKind.None;
        private bool _potToolSelected;
        private string _status = DefaultStatus;
        private bool _statusSuccess = true;
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
        public string TerrainPresentationError { get { return _terrainPresentationError; } }
        public bool IsTerrainPresentationAvailable
        {
            get { return string.IsNullOrEmpty(_terrainPresentationError); }
        }
        public static int ActiveSessionHostCount { get; private set; }

        public void ConfigureBattlefieldTerrain(IEnumerable<BattlefieldTerrainPalette> palettes)
        {
            battlefieldTerrainPalettes = (palettes ?? Enumerable.Empty<BattlefieldTerrainPalette>()).ToArray();
            if (_isInitialized) RefreshTerrainPresentationStatus();
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
            var catalog = BundledLevelCatalogFactory.CreateSource();
            foreach (var level in catalog.Levels)
            {
                var map = catalog.Maps.FirstOrDefault(value => value != null
                    && string.Equals(value.MapId, level.MapId, StringComparison.Ordinal));
                var theme = catalog.Themes.FirstOrDefault(value => value != null
                    && string.Equals(value.ThemeId, level.ThemeId, StringComparison.Ordinal));
                var palette = theme == null ? null : BattlefieldTerrainPalettes.FirstOrDefault(value =>
                    value != null && string.Equals(value.PaletteId, theme.TerrainPaletteId,
                        StringComparison.Ordinal));
                if (map == null)
                {
                    reason = "A bundled level references an unavailable battlefield map.";
                    return false;
                }
                if (palette == null)
                {
                    reason = "A bundled level theme references an unavailable battlefield terrain palette.";
                    return false;
                }
                if (!BattlefieldDualGridTerrain.Validate(map, palette, out reason)) return false;
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

        public bool ValidateActiveTerrainPresentation(out string reason)
        {
            if (_game == null || _game.Map == null)
            {
                reason = "The active battle map is unavailable for terrain presentation.";
                return false;
            }
            var theme = _game.Theme;
            if (theme == null)
            {
                reason = "The active battle theme has no terrain palette identity.";
                return false;
            }
            BattlefieldTerrainPalette palette;
            if (!TryResolveBattlefieldTerrainPalette(
                    theme.TerrainPaletteId, out palette, out reason)) return false;
            return BattlefieldDualGridTerrain.Validate(_game.Map, palette, out reason);
        }

        private DualGridTileSet DefaultTerrainTileSet(string surfaceId)
        {
            if (BattlefieldTerrainPalettes.Count == 0 || BattlefieldTerrainPalettes[0] == null) return null;
            DualGridTileSet tileSet;
            return BattlefieldTerrainPalettes[0].TryGetLandformTileSet(surfaceId,
                BattlefieldLayerIds.ContourStyles.Square, out tileSet) ? tileSet : null;
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

            var layout = new BattleUiLayout(simulation.Map);
            var compactHint = layout.MergeHint(new Rect(180f, 180f, 48f, 48f), 118f);
            if (compactHint.width < BattleUiLayout.MergeHintMinimumWidth
                || compactHint.width > BattleUiLayout.MergeHintMaximumWidth
                || compactHint.width >= 280f
                || compactHint.height != BattleUiLayout.MergeHintHeight
                || compactHint.height >= 30f || compactHint.xMin < 8f
                || compactHint.xMax > BattleUiLayout.DesignWidth - 8f)
            {
                reason = "merge hint frame is not compact or contained";
                return false;
            }

            var projection = layout.Battlefield;
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

        public static bool ValidatePlantPresentationResources(out string reason)
        {
            if (AttackRangeTextureSize < 1024)
            {
                reason = "attack range texture is below the portrait clarity baseline";
                return false;
            }

            foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
            {
                var plant = new Plant { Kind = kind, Weapon = WeaponKind.None };
                if (PlantSprite(plant) == PlantSprite(kind)) continue;
                reason = "unequipped plant does not resolve its base resource: " + kind;
                return false;
            }

            var evolutionSprites = new HashSet<TempSprite>();
            foreach (WeaponKind weapon in Enum.GetValues(typeof(WeaponKind)))
            {
                if (weapon == WeaponKind.None) continue;
                var plant = new Plant { Kind = PlantKind.Pea, Weapon = weapon };
                var resolved = PlantSprite(plant);
                if (resolved != WeaponSprite(weapon) || !evolutionSprites.Add(resolved))
                {
                    reason = "equipment evolution resource mapping is missing or duplicated: " + weapon;
                    return false;
                }
            }

            reason = "ok";
            return true;
        }

        public static bool ValidatePortraitLayout(out string reason)
        {
            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            var design = layout.Design;
            var topLevelRegions = new[] { layout.Header, layout.BattleSurface };
            foreach (var region in topLevelRegions)
            {
                if (!ContainsRect(design, region))
                {
                    reason = "region outside design bounds: " + region;
                    return false;
                }
            }

            if (layout.Header.yMax > layout.BattleSurface.yMin)
            {
                reason = "header overlaps the embedded battle surface";
                return false;
            }

            var embeddedRegions = new[]
            {
                layout.Board, layout.ToolTray, layout.NurseryTray,
                layout.RefreshAction, layout.Detail,
            };
            foreach (var region in embeddedRegions)
            {
                if (!ContainsRect(layout.BattleSurface, region))
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

            if (layout.Board.width < BattleUiLayout.DesignWidth || layout.Board.height <= 398f)
            {
                reason = "battlefield did not reach the enlarged full-width target";
                return false;
            }

            var projection = layout.Battlefield;
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
            var nurseryCell = layout.NurserySlot(0);
            var nurseryIcon = BattleUiLayout.FramelessSlotIcon(nurseryCell);
            var potToolCell = layout.PotTool;
            var potToolIcon = layout.PotToolIcon;
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
                layout.PauseAction, layout.SpeedAction,
                layout.WeaponTool(WeaponKind.Gatling), layout.WeaponTool(WeaponKind.Ice),
                layout.WeaponTool(WeaponKind.Chili), layout.PotTool,
                layout.RefreshAction, layout.WaveAction, layout.DetailCloseAction,
                layout.NurserySlot(0), layout.NurserySlot(1), layout.NurserySlot(2),
                layout.NurserySlot(3), layout.NurserySlot(4),
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
            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            var projection = layout.Battlefield;
            if (!projection.ValidateControlInset(out reason)) return false;

            var readyUi = BattleUiPresentationState.Create(GamePhase.Ready, false);
            var playingUi = BattleUiPresentationState.Create(GamePhase.Playing, false);
            var betweenUi = BattleUiPresentationState.Create(GamePhase.BetweenWaves, false);
            var victoryUi = BattleUiPresentationState.Create(GamePhase.Victory, false);
            var defeatUi = BattleUiPresentationState.Create(GamePhase.Defeat, false);
            var pausedUi = BattleUiPresentationState.Create(GamePhase.Ready, true);
            if (!readyUi.ShowsWaveAction || readyUi.WaveActionLabel != "开始波次"
                || playingUi.ShowsWaveAction
                || !betweenUi.ShowsWaveAction
                || betweenUi.WaveActionLabel != "立即开始下一波"
                || victoryUi.ShowsWaveAction
                || defeatUi.ShowsWaveAction
                || pausedUi.ShowsWaveAction)
            {
                reason = "phase-specific battlefield wave action contract failed";
                return false;
            }

            if (readyUi.ModalActionCount != 0
                || BattleUiPresentationState.Create(GamePhase.Playing, true).ModalActionCount != 2
                || BattleUiPresentationState.Create(GamePhase.BetweenWaves, true).ModalActionCount != 2
                || victoryUi.ModalActionCount != 1
                || defeatUi.ModalActionCount != 1)
            {
                reason = "phase-specific modal action count failed";
                return false;
            }

            for (var count = 1; count <= 2; count++)
            {
                Rect? previous = null;
                for (var index = 0; index < count; index++)
                {
                    var target = layout.ModalAction(index, count);
                    var owner = count == 1 ? layout.TerminalModal : layout.Modal;
                    if (Mathf.Min(target.width, target.height) < 44f
                        || target.xMin < owner.xMin || target.yMin < owner.yMin
                        || target.xMax > owner.xMax || target.yMax > owner.yMax
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
            if (BattleUiPresentationState.Create(
                    ready.State.Phase, ready.State.Paused).ShowsWaveAction || ready.StartWave(out _))
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
                StatusSuccess = false,
                StatusPulse = RuntimeUiFeedbackPulse.Begin(0f, 5f),
                Drag = new DragSession { Type = DragPayloadType.Plant, PlantId = 88, Active = true },
                DragControlId = 7,
                ReturnPulsePlantId = 88,
                ReturnPulse = RuntimeUiFeedbackPulse.Begin(0f, 5f),
                NurseryRollDisplayPulse = RuntimeUiFeedbackPulse.Begin(0f, 5f),
                SelectionPulseTarget = 2,
                SelectionPulse = RuntimeUiFeedbackPulse.Begin(0f, 5f),
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
            BattlefieldMapDefinition map = null)
        {
            if (!TryValidateInitialization(
                    request, navigator, resultSink, runtimeUiTheme, out var failure)) return failure;

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

            return CompleteInitialization(
                request, navigator, resultSink, runtimeUiTheme, simulation);
        }

        public BattleSessionInitializationResult Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            RuntimeUiTheme runtimeUiTheme,
            ResolvedLevelDefinition resolvedLevel)
        {
            if (!TryValidateInitialization(
                    request, navigator, resultSink, runtimeUiTheme, out var failure)) return failure;
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

            _hasInitialized = true;
            _isInitialized = true;
            _sessionDisposed = false;
            _acceptanceTerminalPreview = false;
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
            RefreshTerrainPresentationStatus();
            _presentation.Clear();
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

            _acceptanceTerminalPreview = false;
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
            if (_acceptanceTerminalPreview) return false;

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
            _acceptanceTerminalPreview = false;
            _navigator = null;
            _resultSink = null;
            _runtimeUiTheme = null;
            _runtimeUiDrawContext = null;
            _appBootstrap = null;
            _currentRequest = null;
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
            _selectedWeapon = WeaponKind.None;
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
            _presentation.Advance(Time.unscaledDeltaTime);
            _game.Tick(Time.unscaledDeltaTime);
            _presentation.Consume(_game);
            TrySubmitTerminalResult();
            if (_inspectedPlantId >= 0 && _game.PlantById(_inspectedPlantId) == null) _inspectedPlantId = -1;
        }

        public void ConfigureAcceptanceState(string stateName)
        {
            ConfigureAcceptanceState(stateName, Application.absoluteURL);
        }

        private void ConfigureAcceptanceState(string stateName, string absoluteUrl)
        {
            if (!_isInitialized || _game == null) return;
            if (string.IsNullOrEmpty(absoluteUrl) || !absoluteUrl.Contains("acceptance=1")) return;
            _acceptanceTerminalPreview = false;
            _game.Reset(20260714);
            _game.DiscardPendingPresentationEvents();
            _presentation.Clear();
            _battleUiLayout = new BattleUiLayout(_game.Map);
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
                case "selected-tool":
                    AddAcceptancePot(GetAcceptanceCell(0), PlantKind.Pea);
                    _game.State.Inventory.Gatling = 1;
                    break;
                case "terminal-victory":
                    _game.State.Phase = GamePhase.Victory;
                    _game.State.WaveIndex = _game.MaxWaves;
                    _game.State.Lives = 3;
                    _acceptanceTerminalPreview = true;
                    break;
                case "terminal-defeat":
                    _game.State.Phase = GamePhase.Defeat;
                    _game.State.WaveIndex = Math.Min(6, _game.MaxWaves);
                    _game.State.Lives = 0;
                    _acceptanceTerminalPreview = true;
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
            var layout = BattleLayout;
            _runtimeUiDrawContext = RuntimeUiGui.RequireContext(
                _runtimeUiDrawContext, _runtimeUiTheme, 1f);
            var safeArea = RuntimeSafeAreaResolver.ResolveCurrent();
            var viewportLayout = BattlefieldProjection.CalculateViewportLayout(
                Screen.width, Screen.height, safeArea,
                BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
            GUI.matrix = Matrix4x4.identity;
            RuntimeUiGui.DrawScreenBackground(_runtimeUiDrawContext,
                new Rect(0f, 0f, Screen.width, Screen.height));
            GUI.matrix = viewportLayout.GuiMatrix;
            HandleDragInput(Event.current, layout);
            var currentDropTarget = CurrentDropTarget(layout);
            var currentDropCue = ResolveDropCue(currentDropTarget);
            DrawHeader(layout, _runtimeUiDrawContext);
            DrawBoard(layout, _runtimeUiDrawContext, currentDropTarget, currentDropCue);
            DrawEmbeddedBattleControls(
                layout, _runtimeUiDrawContext, currentDropTarget, currentDropCue);
            DrawDragGhost(
                layout, _runtimeUiDrawContext, currentDropTarget, currentDropCue);
            DrawOverlay(layout, _runtimeUiDrawContext);
        }

        private void HandleDragInput(Event evt, BattleUiLayout layout)
        {
            if (_game == null) return;
            var controlId = GUIUtility.GetControlID(0x4F524348, FocusType.Passive);
            var viewState = BattleUiPresentationState.Create(
                _game.State.Phase, _game.State.Paused);
            if (viewState.BlocksDrag && _drag != null) CancelDrag("拖拽已取消，物品返回原位");

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && _drag != null)
            {
                CancelDrag("已取消拖拽，物品返回原位");
                evt.Use();
                return;
            }
            if (viewState.BlocksDrag) return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var source = FindDragSourceAt(evt.mousePosition, layout);
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
                if (_drag.Active) UpdateDragHoverStatus(_drag.Current, layout);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp)
            {
                var session = _drag;
                session.Current = evt.mousePosition;
                if (session.Active) CompleteDrag(session, session.Current, layout);
                else PerformSourceClick(session);
                _drag = null;
                if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
                _dragControlId = 0;
                evt.Use();
            }
        }

        private DragSession FindDragSourceAt(Vector2 point, BattleUiLayout layout)
        {
            foreach (var plant in _game.State.Plants)
            {
                var rect = PlantSourceRect(plant, layout);
                if (rect.width > 0f && rect.Contains(point))
                    return new DragSession { Type = DragPayloadType.Plant, PlantId = plant.Id };
            }
            if (_game.State.Inventory.Gatling > 0
                && layout.WeaponTool(WeaponKind.Gatling).Contains(point))
                return new DragSession { Type = DragPayloadType.Weapon, Weapon = WeaponKind.Gatling };
            if (_game.State.Inventory.Ice > 0
                && layout.WeaponTool(WeaponKind.Ice).Contains(point))
                return new DragSession { Type = DragPayloadType.Weapon, Weapon = WeaponKind.Ice };
            if (_game.State.Inventory.Chili > 0
                && layout.WeaponTool(WeaponKind.Chili).Contains(point))
                return new DragSession { Type = DragPayloadType.Weapon, Weapon = WeaponKind.Chili };
            if (_game.State.Inventory.Pots > 0 && layout.PotTool.Contains(point))
                return new DragSession { Type = DragPayloadType.Pot };
            return null;
        }

        private DropTarget FindDropTargetAt(
            DragSession session, Vector2 cursor, BattleUiLayout layout)
        {
            var targets = new List<DropTarget>();
            if (session.Type == DragPayloadType.Plant)
            {
                foreach (var pot in _game.State.Pots.Where(value => value.Active))
                {
                    var rect = layout.Battlefield.PotHitRect(pot.Cell);
                    targets.Add(new DropTarget { Type = DropTargetType.Pot, Id = pot.Id, Rect = rect });
                }
                for (var slot = 0; slot < 5; slot++)
                {
                    var rect = layout.NurserySlot(slot);
                    targets.Add(new DropTarget { Type = DropTargetType.Nursery, Slot = slot, Rect = rect });
                }
            }
            else if (session.Type == DragPayloadType.Weapon)
            {
                foreach (var plant in _game.State.Plants)
                {
                    var rect = PlantSourceRect(plant, layout);
                    if (rect.width > 0f)
                        targets.Add(new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = rect });
                }
            }
            else
            {
                foreach (var cell in _game.Map.PlantableCells)
                {
                    if (_game.State.Pots.Any(pot => pot.Active && pot.Cell == cell)) continue;
                    var rect = layout.Battlefield.PotHitRect(cell);
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

        private void CompleteDrag(DragSession session, Vector2 point, BattleUiLayout layout)
        {
            var target = FindDropTargetAt(session, point, layout);
            if (session.Type == DragPayloadType.Plant)
            {
                if (target.Type == DropTargetType.Pot)
                {
                    var status = _game.GetPlantDropStatus(session.PlantId, target.Id);
                    if (!status.Legal) { CancelDrag(status.Reason); return; }
                    var targetPlant = _game.PlantAtPot(target.Id);
                    var selectedAfterDrop = status.Action == PlantDropAction.Merge && targetPlant != null
                        ? targetPlant.Id
                        : session.PlantId;
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
                _returnPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
            }
            SetStatus(false, reason);
            _drag = null;
            if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
            _dragControlId = 0;
        }

        private void UpdateDragHoverStatus(Vector2 point, BattleUiLayout layout)
        {
            var target = FindDropTargetAt(_drag, point, layout);
            var status = DragTargetStatus(_drag, target);
            _status = (status.Legal ? "✓ " : "! ") + status.Reason;
            _statusPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledFocusSeconds);
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

        private DropTarget CurrentDropTarget(BattleUiLayout layout)
        {
            return _drag != null && _drag.Active
                ? FindDropTargetAt(_drag, _drag.Current, layout)
                : new DropTarget { Type = DropTargetType.None };
        }

        private BattleUiDropCue ResolveDropCue(DropTarget target)
        {
            if (_drag == null || !_drag.Active) return BattleUiDropCue.None;
            if (_drag.Type == DragPayloadType.Plant)
            {
                var status = PlantDragTargetStatus(_drag, target);
                return BattleUiPresentationState.ResolveDropCue(status.Legal,
                    status.Action == PlantDropAction.Merge,
                    status.Action == PlantDropAction.Swap);
            }

            var interaction = DragTargetStatus(_drag, target);
            return BattleUiPresentationState.ResolveDropCue(
                interaction.Legal, false, false);
        }

        private static bool MatchesDropTarget(DropTarget candidate, DropTarget current)
        {
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

        private static void DrawDropCue(RuntimeUiDrawContext drawContext,
            Rect target, BattleUiDropCue cue)
        {
            if (cue == BattleUiDropCue.None) return;
            RuntimeUiGui.DrawIndicator(drawContext, BattleUiLayout.CueBadge(target),
                BattleUiPresentationState.DropIndicatorKind(cue));
        }

        private static bool ShouldShowMergeHint(DragPayloadType payloadType, PlantDropStatus status)
        {
            return payloadType == DragPayloadType.Plant
                && status.Legal
                && status.Action == PlantDropAction.Merge;
        }

        private void ToggleWeaponSelection(WeaponKind weapon)
        {
            _selectedWeapon = _selectedWeapon == weapon ? WeaponKind.None : weapon;
            _potToolSelected = false;
            if (_selectedWeapon == WeaponKind.None)
            {
                _selectionPulseTarget = 0;
                _selectionPulse = default;
            }
            else
            {
                _selectionPulseTarget = (int)_selectedWeapon;
                _selectionPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledSelectionSeconds);
            }
            SetStatus(true, _selectedWeapon == WeaponKind.None ? "已取消武器选择" : "拖动或点击植物安装" + GameConfig.WeaponName(weapon));
        }

        private void TogglePotTool()
        {
            _potToolSelected = !_potToolSelected;
            _selectedWeapon = WeaponKind.None;
            _selectionPulseTarget = _potToolSelected ? -1 : 0;
            _selectionPulse = _potToolSelected
                ? RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledSelectionSeconds)
                : default;
            SetStatus(true, _potToolSelected ? "拖动花盆到绿色候选格，或点击扩建" : "已取消扩建");
        }

        private BattleUiLayout BattleLayout
        {
            get
            {
                if (_battleUiLayout == null)
                    throw new InvalidOperationException("Battle UI layout is unavailable before initialization.");
                return _battleUiLayout;
            }
        }

        private BattlefieldProjection Projection => BattleLayout.Battlefield;

        private static Rect ExpansionRect(Vector2Int cell, BattleUiLayout layout)
        {
            return layout.Battlefield.PotHitRect(cell);
        }

        private static Rect PotHitRect(Pot pot, BattleUiLayout layout)
        {
            return layout.Battlefield.PotHitRect(pot.Cell);
        }

        private static Rect PotVisualRect(Pot pot, BattleUiLayout layout)
        {
            return layout.Battlefield.PotVisualRect(pot.Cell);
        }

        private Rect PlantSourceRect(Plant plant, BattleUiLayout layout)
        {
            if (plant.PotId >= 0)
            {
                var pot = _game.PotById(plant.PotId);
                return pot == null ? new Rect() : PotHitRect(pot, layout);
            }
            return plant.NurseryIndex >= 0
                ? layout.NurserySlot(plant.NurseryIndex)
                : new Rect();
        }
        private static Rect Grow(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static Rect TransformChild(Rect child, Rect sourceParent,
            Rect visualParent)
        {
            if (sourceParent.width <= 0f || sourceParent.height <= 0f) return child;
            var scaleX = visualParent.width / sourceParent.width;
            var scaleY = visualParent.height / sourceParent.height;
            return new Rect(
                visualParent.x + (child.x - sourceParent.x) * scaleX,
                visualParent.y + (child.y - sourceParent.y) * scaleY,
                child.width * scaleX,
                child.height * scaleY);
        }

        private static bool ContainsRect(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
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

        private void BeginBattleActionPress(int target)
        {
            _actionPressTarget = target;
            _actionPressPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledPressSeconds);
        }

        private RuntimeUiMotionSample BattleActionMotion(int target)
        {
            if (_actionPressTarget != target)
                return RuntimeUiMotionSample.Rest;
            return RuntimeUiMotion.Evaluate(_actionPressPulse, Time.unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Press);
        }

        private RuntimeUiPressResult TrackBattleAction(int target, Rect rect,
            bool enabled = true)
        {
            return _actionPressTracker.Update(target, rect, enabled,
                RuntimeUiPointerSample.FromEvent(Event.current),
                _runtimeUiTheme.Feedback.DragCancelDistance);
        }

        private static bool DrawSharedHitTarget(RuntimeUiDrawContext drawContext,
            Rect rect, RuntimeUiInteractionState state)
        {
            var enabled = GUI.enabled;
            GUI.enabled = enabled && state != RuntimeUiInteractionState.Disabled
                && state != RuntimeUiInteractionState.Loading;
            try
            {
                return GUI.Button(rect, GUIContent.none, drawContext.Styles.HitTarget);
            }
            finally
            {
                GUI.enabled = enabled;
            }
        }

        private void DrawHeader(BattleUiLayout layout, RuntimeUiDrawContext drawContext)
        {
            var viewState = BattleUiPresentationState.Create(
                _game.State.Phase, _game.State.Paused);
            RefreshHeaderMetricMotion();
            RuntimeUiGui.DrawRaisedPanel(drawContext, layout.Header);
            var titleCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleTitle);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.HeaderTitle,
                titleCopy.Text,
                titleCopy.Role, titleCopy.Tone, titleCopy.Alignment);
            RuntimeUiGui.DrawMetric(drawContext, layout.SunMetric,
                RuntimeUiArtSlot.IconResourceSun,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleSun).Text,
                _game.State.Sun.ToString(), compactInline: true,
                compactIconSize: BattleUiLayout.HeaderMetricIconSize,
                motion: RuntimeUiMotion.Evaluate(_sunPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop));
            RuntimeUiGui.DrawMetric(drawContext, layout.LivesMetric,
                RuntimeUiArtSlot.IconResourceCore,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleCore).Text,
                _game.State.Lives.ToString(), compactInline: true,
                compactIconSize: BattleUiLayout.HeaderMetricIconSize,
                motion: RuntimeUiMotion.Evaluate(_livesPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.StrongPop));
            RuntimeUiGui.DrawMetric(drawContext, layout.WaveMetric,
                RuntimeUiArtSlot.IconResourceWave,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleWave).Text,
                _game.State.WaveIndex.ToString(),
                compactInline: true,
                compactIconSize: BattleUiLayout.HeaderMetricIconSize,
                motion: RuntimeUiMotion.Evaluate(_wavePulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop));
            RuntimeUiGui.DrawMetricDivider(drawContext, layout.FirstMetricDivider);
            RuntimeUiGui.DrawMetricDivider(drawContext, layout.SecondMetricDivider);

            var pausePress = TrackBattleAction(
                PauseActionFeedbackTarget, layout.PauseAction);
            var pauseState = BattleUiPresentationState.ResolveActionState(
                viewState.IsPaused, pausePress.Hovered, pausePress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, layout.PauseAction, string.Empty,
                RuntimeUiActionKind.Quiet, pauseState, viewState.PauseActionIcon,
                motion: BattleActionMotion(PauseActionFeedbackTarget));
            if (pausePress.Activated)
            {
                BeginBattleActionPress(PauseActionFeedbackTarget);
                _game.TogglePause();
            }

            var speedPress = TrackBattleAction(
                SpeedActionFeedbackTarget, layout.SpeedAction);
            var speedState = BattleUiPresentationState.ResolveActionState(
                _game.State.Speed != 1, speedPress.Hovered, speedPress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, layout.SpeedAction,
                _game.State.Speed + "×", RuntimeUiActionKind.Quiet, speedState,
                RuntimeUiArtSlot.IconControlSpeed,
                RuntimeUiTypographyRole.Supplemental,
                motion: BattleActionMotion(SpeedActionFeedbackTarget));
            if (speedPress.Activated)
            {
                BeginBattleActionPress(SpeedActionFeedbackTarget);
                _game.SetSpeed(_game.State.Speed == 1 ? 2 : 1);
            }
        }

        private void RefreshHeaderMetricMotion()
        {
            var duration = _runtimeUiTheme.Feedback.UnscaledTransitionSeconds
                + _runtimeUiTheme.Feedback.UnscaledSelectionSeconds;
            if (_observedSun != _game.State.Sun)
            {
                _observedSun = _game.State.Sun;
                _sunPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime, duration);
            }
            if (_observedLives != _game.State.Lives)
            {
                _observedLives = _game.State.Lives;
                _livesPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime, duration);
            }
            if (_observedWave != _game.State.WaveIndex)
            {
                _observedWave = _game.State.WaveIndex;
                _wavePulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime, duration);
            }
        }

        private void DrawBoard(BattleUiLayout layout, RuntimeUiDrawContext drawContext,
            DropTarget currentDropTarget, BattleUiDropCue currentDropCue)
        {
            RuntimeUiGui.DrawStandardPanel(drawContext, layout.BattleSurface);
            var texturedTerrain = DrawBattlefieldTerrain();
            if (!texturedTerrain)
                DrawTerrainPresentationFailure(layout);
            DrawRouteTiles();
            DrawCore();
            DrawPlantingCells(texturedTerrain, layout);
            DrawInspectedAttackRange(layout);
            if (_potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot))
                DrawExpansionCandidates(
                    layout, drawContext, currentDropTarget, currentDropCue);
            DrawPotsAndPlants(layout, drawContext, currentDropTarget, currentDropCue);
            DrawProjectiles();
            DrawZombies();
            DrawCombatEffects();
            DrawFeedback(layout);
            DrawBoardStatus(layout, drawContext, currentDropCue);
        }

        private bool DrawBattlefieldTerrain()
        {
            string reason;
            if (!ValidateActiveTerrainPresentation(out reason))
            {
                SetTerrainPresentationError(reason);
                return false;
            }
            SetTerrainPresentationError(string.Empty);
            var theme = _game.Theme;
            BattlefieldTerrainPalette palette;
            if (!TryResolveBattlefieldTerrainPalette(theme.TerrainPaletteId,
                    out palette, out reason))
            {
                SetTerrainPresentationError(reason);
                return false;
            }
            var map = _game.Map;
            var grid = Projection.GridRect;
            GUI.BeginGroup(grid);
            var previous = GUI.color;
            GUI.color = Color.white;
            DrawBattlefieldBaseLayer(map, grid, palette);

            foreach (var binding in palette.LandformBindings)
                if (binding != null && binding.TileSet != null)
                    DrawBattlefieldTerrainLayer(map, grid, binding.TileSet, binding.SurfaceId,
                        binding.ContourStyleId);

            foreach (var binding in palette.EdgeBindings)
                if (binding != null && binding.TileSet != null)
                {
                    DrawBattlefieldTerrainEdgeLayer(map, grid, binding.TileSet,
                        binding.LandformSurfaceId, binding.BaseSurfaceId,
                        binding.ContourStyleId, binding.EdgeStyleId, false);
                    if (!palette.HasExactEdgeBinding(binding.BaseSurfaceId,
                            binding.LandformSurfaceId, binding.ContourStyleId,
                            binding.EdgeStyleId))
                        DrawBattlefieldTerrainEdgeLayer(map, grid, binding.TileSet,
                            binding.BaseSurfaceId, binding.LandformSurfaceId,
                            binding.ContourStyleId, binding.EdgeStyleId, true);
                }

            GUI.color = previous;
            GUI.EndGroup();
            return true;
        }

        private void RefreshTerrainPresentationStatus()
        {
            string reason;
            SetTerrainPresentationError(ValidateActiveTerrainPresentation(out reason)
                ? string.Empty : reason);
        }

        private void SetTerrainPresentationError(string reason)
        {
            _terrainPresentationError = reason ?? string.Empty;
            if (string.IsNullOrEmpty(_terrainPresentationError)
                || string.Equals(_lastLoggedTerrainPresentationError,
                    _terrainPresentationError, StringComparison.Ordinal)) return;
            _lastLoggedTerrainPresentationError = _terrainPresentationError;
            Debug.LogWarning("Battlefield terrain presentation stopped: "
                + _terrainPresentationError, this);
        }

        private void DrawTerrainPresentationFailure(BattleUiLayout layout)
        {
            var panel = layout.TerrainFailurePanel;
            DrawWorldRect(panel, new Color(.24f, .10f, .10f, 1f));
            GUI.Label(Grow(panel, -12f),
                "Terrain presentation unavailable\n" + _terrainPresentationError,
                _terrainFailureStyle);
        }

        private void DrawBattlefieldBaseLayer(BattlefieldMapDefinition map, Rect grid,
            BattlefieldTerrainPalette palette)
        {
            var uniformSurfaceId = map.BaseSurfaceAt(Vector2Int.zero);
            var uniformBase = true;
            for (var cellY = 0; cellY < map.GridHeight && uniformBase; cellY++)
            for (var cellX = 0; cellX < map.GridWidth; cellX++)
                if (!string.Equals(map.BaseSurfaceAt(new Vector2Int(cellX, cellY)),
                        uniformSurfaceId, StringComparison.Ordinal))
                {
                    uniformBase = false;
                    break;
                }

            Texture2D uniformTexture;
            if (uniformBase && palette.TryGetBaseTexture(uniformSurfaceId, out uniformTexture))
            {
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, grid.width, grid.height),
                    uniformTexture,
                    BattlefieldDualGridTerrain.BaseTextureUv(map, null, uniformTexture), true);
                return;
            }

            for (var cellY = 0; cellY < map.GridHeight; cellY++)
            for (var cellX = 0; cellX < map.GridWidth; cellX++)
            {
                var cell = new Vector2Int(cellX, cellY);
                Texture2D texture;
                if (!palette.TryGetBaseTexture(map.BaseSurfaceAt(cell), out texture)) continue;
                var rect = Projection.CellRect(cell);
                rect.position -= grid.position;
                GUI.DrawTextureWithTexCoords(rect, texture,
                    BattlefieldDualGridTerrain.BaseCellUv(map, texture, cellX, cellY), true);
            }
        }

        private void DrawBattlefieldTerrainLayer(BattlefieldMapDefinition map, Rect grid,
            DualGridTileSet tileSet, string surfaceId, string contourStyleId)
        {
            for (var vertexY = 0; vertexY <= map.GridHeight; vertexY++)
            for (var vertexX = 0; vertexX <= map.GridWidth; vertexX++)
            {
                var mask = BattlefieldDualGridTerrain.ResolveLandformMask(
                    map, vertexX, vertexY, surfaceId, contourStyleId);
                Sprite sprite;
                if (mask == DualGridMask.Empty || !tileSet.TryGetSprite(mask, out sprite)) continue;
                var rect = BattlefieldDualGridTerrain.VisualTileRect(
                    Projection, vertexX, vertexY);
                rect.position -= grid.position;
                GUI.DrawTextureWithTexCoords(Grow(rect, TerrainTileSeamOverlap), sprite.texture,
                    BattlefieldDualGridTerrain.SpriteUv(sprite), true);
            }
        }

        private void DrawBattlefieldTerrainEdgeLayer(BattlefieldMapDefinition map, Rect grid,
            DualGridTileSet tileSet, string landformSurfaceId, string baseSurfaceId,
            string contourStyleId, string edgeStyleId, bool complementMask)
        {
            for (var vertexY = 0; vertexY <= map.GridHeight; vertexY++)
            for (var vertexX = 0; vertexX <= map.GridWidth; vertexX++)
            {
                var mask = BattlefieldDualGridTerrain.ResolveEdgeMask(map, vertexX, vertexY,
                    landformSurfaceId, baseSurfaceId, contourStyleId, edgeStyleId);
                if (!DualGridMaskUtility.TryResolveSharedEdgeMask(mask,
                        complementMask, out mask)) continue;
                Sprite sprite;
                if (!tileSet.TryGetSprite(mask, out sprite)) continue;
                var rect = BattlefieldDualGridTerrain.VisualTileRect(
                    Projection, vertexX, vertexY);
                rect.position -= grid.position;
                GUI.DrawTextureWithTexCoords(Grow(rect, TerrainTileSeamOverlap), sprite.texture,
                    BattlefieldDualGridTerrain.SpriteUv(sprite), true);
            }
        }

        private void DrawCore()
        {
            var rect = Projection.CoreRect;
            DrawWorldRect(rect,
                ThemeColor(theme => theme.CoreColor, new Color(.71f, .79f, .45f)));
            DrawWorldLabel(
                new Rect(rect.x, rect.y + rect.height * .08f,
                    rect.width, rect.height * .55f),
                "♣", 17, new Color(.19f, .38f, .16f));
            DrawWorldLabel(
                new Rect(rect.x, rect.center.y + rect.height * .08f,
                    rect.width, 18f),
                "核心", 10, Color.white);
        }

        private void DrawRouteTiles()
        {
            var accent = ThemeColor(theme => theme.AccentColor, new Color(.9f, .38f, .24f));
            foreach (var descriptor in _game.Map.RouteTileDescriptors)
            {
                var rect = Projection.RouteTileRect(descriptor.Cell);
                // When canonical terrain presentation is unavailable, keep simulation and
                // endpoint markers alive but never substitute the legacy route paint.
                if (descriptor.Kind != BattlefieldRouteTileKind.Entry
                    && descriptor.Kind != BattlefieldRouteTileKind.Exit) continue;
                var markerSize = rect.width * .28f;
                var marker = new Rect(rect.center.x - markerSize * .5f, rect.center.y - markerSize * .5f,
                    markerSize, markerSize);
                DrawWorldRect(marker, descriptor.Kind == BattlefieldRouteTileKind.Entry
                    ? new Color(.42f, .82f, .32f)
                    : accent);
            }
        }

        private void DrawPlantingCells(bool texturedTerrain, BattleUiLayout layout)
        {
            // Missing contour or pair assets are a presentation failure. Do not disguise it
            // by reverting to the legacy plantable-cell terrain treatment.
            if (!texturedTerrain) return;
            var expansionActive = _potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot);
            var plantable = ThemeColor(theme => theme.PlantableColor, new Color(.58f, .72f, .36f));
            foreach (var cell in _game.Map.PlantableCells)
            {
                var rect = ExpansionRect(cell, layout);
                if (!expansionActive)
                {
                    DrawWorldOutline(
                        Grow(rect, .5f), 1f, new Color(.2f, .24f, .16f, .28f));
                    continue;
                }
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
                    DrawWorldOutline(Grow(rect, .5f), 1f, border);
                else
                    DrawWorldRect(Grow(rect, .5f), border);
                DrawWorldRect(rect, fill);
                if (!active)
                    DrawWorldLabel(rect, expansionActive && legal ? "+" : "·", 11,
                        expansionActive && legal
                            ? new Color(.88f, 1f, .55f)
                            : new Color(.31f, .43f, .2f, .9f));
            }
        }

        private void DrawInspectedAttackRange(BattleUiLayout layout)
        {
            var plant = _game.PlantById(_inspectedPlantId);
            if (plant == null || plant.PotId < 0) return;
            var pot = _game.PotById(plant.PotId);
            var range = EffectiveAttackRange(plant);
            if (pot == null || range <= .0001f) return;
            var rangeRect = Projection.MapRect(_game.PotPoint(pot), range * 2f, range * 2f);
            rangeRect.position -= layout.Board.position;
            GUI.BeginGroup(layout.Board);
            GUI.DrawTexture(rangeRect, _attackRangeTexture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }

        public static float EffectiveAttackRange(Plant plant)
        {
            return plant == null ? 0f : GameConfig.Plant(plant.Kind).Range * GameConfig.StarRange(plant.Star);
        }

        private void DrawPotsAndPlants(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            foreach (var pot in _game.State.Pots.Where(value => value.Active))
            {
                var hitRect = PotHitRect(pot, layout);
                var rect = PotVisualRect(pot, layout);
                var plant = _game.PlantAtPot(pot.Id);
                var selected = plant != null && plant.Id == _inspectedPlantId;
                var target = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon && plant != null
                    ? new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = hitRect }
                    : new DropTarget { Type = DropTargetType.Pot, Id = pot.Id, Rect = hitRect };
                var returning = plant != null && plant.Id == _returnPulsePlantId
                    && _returnPulse.IsActive(Time.unscaledTime);
                DrawTempSprite(rect, plant == null ? TempSprite.EmptyPot : TempSprite.OccupiedPot);
                if (plant == null)
                {
                    if (MatchesDropTarget(target, currentDropTarget))
                        DrawDropCue(drawContext, hitRect, currentDropCue);
                    if (DrawSharedHitTarget(drawContext, hitRect,
                            RuntimeUiInteractionState.Normal))
                        HandlePotClick(pot.Id);
                    continue;
                }
                if (DrawSharedHitTarget(drawContext, hitRect,
                        RuntimeUiInteractionState.Normal))
                    HandlePlantClick(plant);
                DrawAnimatedPlant(Grow(rect, 1f), plant);
                DrawWorldLabel(
                    new Rect(rect.x - 4f, rect.yMax - 1f, rect.width + 8f, 10f),
                    new string('★', plant.Star), 10, Color.white);
                if (plant.MoveCooldown > 0f)
                    DrawWorldLabel(rect, plant.MoveCooldown.ToString("0.0"),
                        10, Color.white);
                if (selected)
                    RuntimeUiGui.DrawIndicator(drawContext,
                        BattleUiLayout.CueBadge(hitRect), RuntimeUiIndicatorKind.Selected);
                else if (returning)
                    RuntimeUiGui.DrawIndicator(drawContext,
                        BattleUiLayout.CueBadge(hitRect), RuntimeUiIndicatorKind.Warning);
                if (MatchesDropTarget(target, currentDropTarget))
                    DrawDropCue(drawContext, hitRect, currentDropCue);
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

        private void DrawExpansionCandidates(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            foreach (var cell in _game.Map.PlantableCells)
            {
                if (_game.State.Pots.Any(pot => pot.Active && pot.Cell == cell)) continue;
                var legal = _game.CanExpand(cell);
                var rect = ExpansionRect(cell, layout);
                var visualRect = layout.Battlefield.PotVisualRect(cell);
                var target = new DropTarget { Type = DropTargetType.Expansion, Cell = cell, Rect = rect };
                DrawTempSprite(visualRect, legal ? TempSprite.ExpansionPot : TempSprite.LockedPot);
                var cue = MatchesDropTarget(target, currentDropTarget)
                    ? currentDropCue
                    : BattleUiPresentationState.ResolveDropCue(legal, false, false);
                DrawDropCue(drawContext, rect, cue);
                var state = legal
                    ? RuntimeUiInteractionState.Normal
                    : RuntimeUiInteractionState.Disabled;
                if (DrawSharedHitTarget(drawContext, rect, state) && legal)
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
                var size = Projection.LegacyVisualSize(96f);
                var rect = new Rect(point.x - size * .5f, point.y - size * .5f, size, size);
                var frozen = zombie.FreezeUntil > _game.State.Elapsed;
                var slowed = zombie.SlowUntil > _game.State.Elapsed;
                if (frozen) DrawVfxSprite(Grow(rect, 4f), CombatSprite.FrozenAura, new Color(1f, 1f, 1f, .82f));
                var tint = slowed ? new Color(.72f, .9f, 1f) : Color.white;
                if (zombie.HitStunUntil > _game.State.Elapsed)
                    tint = Color.Lerp(tint, Color.white, .72f);
                DrawTempSprite(rect, ZombieSprite(zombie.Kind), tint);
                if (zombie.Burns.Count > 0)
                    DrawVfxSprite(new Rect(rect.xMax - 5f, rect.y - 6f, 11f, 11f), CombatSprite.Burning);
                var healthWidth = Projection.LegacyVisualSize(80f);
                var healthRect = new Rect(point.x - healthWidth * .5f, rect.y - 3f,
                    healthWidth, 2f);
                DrawWorldRect(healthRect, new Color(.22f, .16f, .12f));
                healthRect.width *= Mathf.Clamp01(zombie.Hp / zombie.MaxHp);
                DrawWorldRect(healthRect, new Color(.85f, .22f, .16f));
            }
        }

        private void DrawProjectiles()
        {
            foreach (var projectile in _game.State.Projectiles)
            {
                var point = ToBoard(projectile.Position);
                if (projectile.Kind == PlantKind.Pea)
                {
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

        private void DrawFeedback(BattleUiLayout layout)
        {
            foreach (var feedback in _presentation.Feedback)
            {
                var point = ToBoard(feedback.Point);
                DrawWorldLabel(BattleUiLayout.BattlefieldFeedback(
                    layout.Battlefield.GridRect, point), feedback.Text, 11, feedback.Color);
            }
        }

        public static Rect BattlefieldFeedbackRect(Rect gridRect, Vector2 point)
        {
            return BattleUiLayout.BattlefieldFeedback(gridRect, point);
        }

        private void DrawBoardStatus(
            BattleUiLayout layout, RuntimeUiDrawContext drawContext,
            BattleUiDropCue currentDropCue)
        {
            var state = _game.State;
            var viewState = BattleUiPresentationState.Create(state.Phase, state.Paused);
            var transientVisible = _statusPulse.IsActive(Time.unscaledTime)
                && !string.IsNullOrEmpty(_status);
            var text = transientVisible
                ? _status
                : viewState.BoardStatusText(
                    state.WaveIndex, state.Zombies.Count, state.BetweenTimer);
            var statusState = transientVisible
                ? BattleUiPresentationState.ResolveTransientStatusState(
                    _statusSuccess, currentDropCue)
                : viewState.StatusInteractionState;
            var statusRect = viewState.ShowsWaveAction
                ? layout.BoardStatusWithWaveAction
                : layout.BoardStatus;
            if (transientVisible)
            {
                var statusMotion = RuntimeUiMotion.Evaluate(_statusPulse,
                    Time.unscaledTime, _runtimeUiTheme.Feedback,
                    RuntimeUiMotionPattern.Pop);
                PrepareTransientStatusText(drawContext, statusRect, text, statusState);
                RuntimeUiGui.DrawStatus(drawContext, statusRect, _preparedStatusTextLines,
                    statusState, RuntimeUiTypographyRole.Supplemental,
                    _preparedStatusTextMode, motion: statusMotion);
            }
            else
            {
                RuntimeUiGui.DrawStatus(drawContext, statusRect, text, statusState,
                    RuntimeUiTypographyRole.Supplemental,
                    RuntimeUiStatusTextMode.SingleLine);
            }
            if (transientVisible)
                DrawDropCue(drawContext, statusRect, currentDropCue);
            if (viewState.ShowsWaveAction)
            {
                var wavePress = TrackBattleAction(
                    WaveActionFeedbackTarget, layout.WaveAction);
                var actionState = BattleUiPresentationState.ResolveActionState(
                    false, wavePress.Hovered, wavePress.Pressed);
                RuntimeUiGui.DrawActionVisual(drawContext, layout.WaveAction,
                    viewState.WaveActionLabel, RuntimeUiActionKind.Primary,
                    actionState, RuntimeUiArtSlot.IconControlStartWave,
                    RuntimeUiTypographyRole.Supplemental,
                    motion: BattleActionMotion(WaveActionFeedbackTarget));
                if (wavePress.Activated)
                {
                    BeginBattleActionPress(WaveActionFeedbackTarget);
                    SetStatus(_game.StartWave(out var reason), reason);
                }
            }
            else if (_actionPressTracker.ActiveControlId == WaveActionFeedbackTarget)
            {
                _actionPressTracker.Cancel();
            }
        }

        private void DrawEmbeddedBattleControls(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            RuntimeUiGui.DrawStandardPanel(drawContext, layout.ToolTray);
            var toolCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleToolTray);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.ToolTrayTitle,
                toolCopy.Text,
                toolCopy.Role, toolCopy.Tone, toolCopy.Alignment);
            DrawTools(layout, drawContext);
            RuntimeUiGui.DrawStandardPanel(drawContext, layout.NurseryTray);
            var nurseryCopy = RuntimeUiCopyCatalog.Get(
                RuntimeUiCopyId.BattleNurseryTray);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.NurseryTrayTitle,
                nurseryCopy.Text, nurseryCopy.Role, nurseryCopy.Tone,
                nurseryCopy.Alignment);
            DrawNursery(layout, drawContext, currentDropTarget, currentDropCue);
            DrawSelectedPlant(layout, drawContext);
        }

        private void DrawTools(BattleUiLayout layout, RuntimeUiDrawContext drawContext)
        {
            DrawToolButton(layout.WeaponTool(WeaponKind.Gatling), WeaponKind.Gatling,
                _game.State.Inventory.Gatling, layout, drawContext);
            DrawToolButton(layout.WeaponTool(WeaponKind.Ice), WeaponKind.Ice,
                _game.State.Inventory.Ice, layout, drawContext);
            DrawToolButton(layout.WeaponTool(WeaponKind.Chili), WeaponKind.Chili,
                _game.State.Inventory.Chili, layout, drawContext);
            var draggingPot = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot;
            var potRect = layout.PotTool;
            var available = _game.State.Inventory.Pots > 0;
            var state = BattleUiPresentationState.ResolveSlotState(available,
                _potToolSelected || draggingPot, ContainsPointer(potRect),
                IsPointerPress(potRect));
            var selectionEmphasized = _potToolSelected && _selectionPulseTarget == -1
                && _selectionPulse.IsActive(Time.unscaledTime);
            var potMotion = selectionEmphasized
                ? RuntimeUiMotion.Evaluate(_selectionPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop)
                : RuntimeUiMotionSample.Rest;
            var potVisualRect = potMotion.Transform(potRect);
            RuntimeUiGui.DrawSlot(drawContext, potVisualRect, RuntimeUiSlotKind.Tool, state,
                selectionEmphasized);
            RuntimeUiGui.DrawIcon(drawContext,
                TransformChild(layout.PotToolIcon, potRect, potVisualRect),
                RuntimeUiArtSlot.IconToolPot, state);
            RuntimeUiGui.DrawText(drawContext,
                TransformChild(layout.PotToolLabel, potRect, potVisualRect),
                "花盆\n×" + _game.State.Inventory.Pots,
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);
            RuntimeUiGui.DrawStateIndicator(drawContext, potVisualRect, state);
            if (DrawSharedHitTarget(drawContext, potRect, state) && available)
                TogglePotTool();
        }

        private void DrawToolButton(
            Rect rect, WeaponKind weapon, int count, BattleUiLayout layout,
            RuntimeUiDrawContext drawContext)
        {
            var selected = _selectedWeapon == weapon;
            var dragging = _drag != null && _drag.Active && _drag.Type == DragPayloadType.Weapon && _drag.Weapon == weapon;
            var state = BattleUiPresentationState.ResolveSlotState(count > 0,
                selected || dragging, ContainsPointer(rect), IsPointerPress(rect));
            var selectionEmphasized = selected && _selectionPulseTarget == (int)weapon
                && _selectionPulse.IsActive(Time.unscaledTime);
            var motion = selectionEmphasized
                ? RuntimeUiMotion.Evaluate(_selectionPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop)
                : RuntimeUiMotionSample.Rest;
            var visualRect = motion.Transform(rect);
            RuntimeUiGui.DrawSlot(drawContext, visualRect, RuntimeUiSlotKind.Tool, state,
                selectionEmphasized);
            DrawStatefulTempSprite(
                drawContext, layout.ToolIcon(visualRect), WeaponSprite(weapon), state);
            RuntimeUiGui.DrawText(drawContext, layout.ToolCountLabel(visualRect), "×" + count,
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);
            RuntimeUiGui.DrawStateIndicator(drawContext, visualRect, state);
            if (!DrawSharedHitTarget(drawContext, rect, state) || count <= 0) return;
            ToggleWeaponSelection(weapon);
        }

        private void DrawNursery(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            for (var slot = 0; slot < 5; slot++)
            {
                var rect = layout.NurserySlot(slot);
                var plant = _game.PlantAtNursery(slot);
                var target = new DropTarget { Type = DropTargetType.Nursery, Slot = slot, Rect = rect };
                var cue = MatchesDropTarget(target, currentDropTarget)
                    ? currentDropCue
                    : BattleUiDropCue.None;
                if (plant != null && currentDropTarget.Type == DropTargetType.Plant
                    && currentDropTarget.Id == plant.Id)
                    cue = currentDropCue;
                if (plant == null)
                {
                    var showingPotReward = _nurseryRollDisplayPulse.IsActive(Time.unscaledTime)
                        && _game.LastNurseryPotSlots.Contains(slot);
                    var state = cue != BattleUiDropCue.None
                        ? BattleUiPresentationState.DropInteractionState(cue)
                        : showingPotReward
                            ? RuntimeUiInteractionState.Success
                            : BattleUiPresentationState.ResolveSlotState(true, false,
                                ContainsPointer(rect), IsPointerPress(rect));
                    var rewardMotion = showingPotReward
                        ? RuntimeUiMotion.Evaluate(_nurseryRollDisplayPulse,
                            Time.unscaledTime, _runtimeUiTheme.Feedback,
                            RuntimeUiMotionPattern.Pop)
                        : RuntimeUiMotionSample.Rest;
                    var visualRect = rewardMotion.Transform(rect);
                    RuntimeUiGui.DrawSlot(
                        drawContext, visualRect, RuntimeUiSlotKind.Nursery, state);
                    if (showingPotReward)
                    {
                        RuntimeUiGui.DrawIcon(drawContext,
                            BattleUiLayout.FramelessSlotIcon(visualRect),
                            RuntimeUiArtSlot.IconToolPot, state);
                        RuntimeUiGui.DrawSingleLineText(drawContext,
                            BattleUiLayout.NurserySlotLabel(visualRect),
                            RuntimeUiCopyCatalog.Get(
                                RuntimeUiCopyId.BattleNurseryPotStored).Text,
                            RuntimeUiTypographyRole.Supplemental,
                            RuntimeUiTextTone.State, TextAnchor.MiddleCenter, state);
                    }
                    else
                        RuntimeUiGui.DrawSingleLineText(drawContext, visualRect,
                            RuntimeUiCopyCatalog.Get(
                                RuntimeUiCopyId.BattleNurseryEmpty).Text,
                            RuntimeUiTypographyRole.Supplemental,
                            RuntimeUiTextTone.Secondary, TextAnchor.MiddleCenter, state);
                    RuntimeUiGui.DrawStateIndicator(drawContext, visualRect, state);
                    DrawDropCue(drawContext, rect, cue);
                    if (DrawSharedHitTarget(drawContext, rect, state))
                        SetStatus(false, DestinationDragGuidance(_game.PlantById(_inspectedPlantId), true));
                    continue;
                }
                var selected = plant.Id == _inspectedPlantId;
                var returning = plant.Id == _returnPulsePlantId
                    && _returnPulse.IsActive(Time.unscaledTime);
                var occupiedState = cue != BattleUiDropCue.None
                    ? BattleUiPresentationState.DropInteractionState(cue)
                    : returning
                        ? RuntimeUiInteractionState.Warning
                        : BattleUiPresentationState.ResolveSlotState(true, selected,
                            ContainsPointer(rect), IsPointerPress(rect));
                var returnMotion = returning
                    ? RuntimeUiMotion.Evaluate(_returnPulse, Time.unscaledTime,
                        _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop)
                    : RuntimeUiMotionSample.Rest;
                var occupiedVisualRect = returnMotion.Transform(rect);
                RuntimeUiGui.DrawSlot(
                    drawContext, occupiedVisualRect, RuntimeUiSlotKind.Nursery, occupiedState);
                DrawTempSprite(BattleUiLayout.FramelessSlotIcon(occupiedVisualRect), PlantSprite(plant));
                RuntimeUiGui.DrawText(drawContext,
                    BattleUiLayout.NurserySlotLabel(occupiedVisualRect),
                    new string('★', plant.Star), RuntimeUiTypographyRole.Supplemental,
                    RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, occupiedState);
                RuntimeUiGui.DrawStateIndicator(drawContext, occupiedVisualRect, occupiedState);
                DrawDropCue(drawContext, rect, cue);
                if (DrawSharedHitTarget(drawContext, rect, occupiedState))
                    HandlePlantClick(plant);
            }
            var cost = GameConfig.RefreshCost(_game.State.RefreshCount);
            var refreshPress = TrackBattleAction(
                RefreshActionFeedbackTarget, layout.RefreshAction);
            var refreshState = BattleUiPresentationState.ResolveActionState(
                false, refreshPress.Hovered, refreshPress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, layout.RefreshAction,
                RuntimeUiCopyCatalog.FormatRefreshAction(cost),
                RuntimeUiActionKind.Primary, refreshState,
                RuntimeUiArtSlot.IconControlRefresh,
                motion: BattleActionMotion(RefreshActionFeedbackTarget));
            if (refreshPress.Activated)
            {
                BeginBattleActionPress(RefreshActionFeedbackTarget);
                RefreshNurseryFromUi();
            }
        }

        private void RefreshNurseryFromUi()
        {
            var success = _game.RefreshNursery(out var reason);
            if (success)
            {
                _nurseryRollDisplayPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
                if (_inspectedPlantId >= 0 && _game.PlantById(_inspectedPlantId) == null) _inspectedPlantId = -1;
            }
            SetStatus(success, reason);
        }

        private void DrawSelectedPlant(
            BattleUiLayout layout, RuntimeUiDrawContext drawContext)
        {
            var plant = _game.PlantById(_inspectedPlantId);
            if (plant == null) return;
            const RuntimeUiInteractionState detailState =
                RuntimeUiInteractionState.Selected;
            RuntimeUiGui.DrawDetailCard(drawContext, layout.Detail, detailState);
            var stats = GameConfig.Plant(plant.Kind);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.DetailTitle,
                stats.Name + " · " + plant.Star + " 星",
                RuntimeUiTypographyRole.ControlLabel, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, detailState);
            var effectiveRange = EffectiveAttackRange(plant);
            var rangeText = effectiveRange > .0001f
                ? Mathf.RoundToInt(GameConfig.LegacyDistance(effectiveRange)).ToString()
                : "无攻击范围";
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.DetailBody,
                "伤害 " + Mathf.RoundToInt(stats.Damage * GameConfig.StarDamage(plant.Star))
                + " · 范围 " + rangeText
                + " · 装备 " + GameConfig.WeaponName(plant.Weapon),
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Secondary,
                TextAnchor.MiddleLeft, detailState);
            var closePress = TrackBattleAction(
                DetailCloseFeedbackTarget, layout.DetailCloseAction);
            var closeState = BattleUiPresentationState.ResolveActionState(
                false, closePress.Hovered, closePress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, layout.DetailCloseAction,
                string.Empty, RuntimeUiActionKind.Quiet, closeState,
                RuntimeUiArtSlot.IconControlClose,
                motion: BattleActionMotion(DetailCloseFeedbackTarget));
            if (closePress.Activated)
            {
                BeginBattleActionPress(DetailCloseFeedbackTarget);
                _inspectedPlantId = -1;
            }
        }

        private void DrawDragGhost(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentTarget,
            BattleUiDropCue currentDropCue)
        {
            if (_drag == null || !_drag.Active) return;
            var rect = DragGeometry.PreviewRect(_drag.Current);
            if (currentTarget.Type != DropTargetType.None)
                rect.center = Vector2.Lerp(rect.center, currentTarget.Rect.center, .42f);
            rect = layout.ClampDragPreview(rect);

            if (_drag.Type == DragPayloadType.Plant)
            {
                var plant = _game.PlantById(_drag.PlantId);
                if (plant != null) DrawTempSprite(rect, PlantSprite(plant));
            }
            else if (_drag.Type == DragPayloadType.Weapon)
                DrawTempSprite(rect, WeaponSprite(_drag.Weapon));
            else
                DrawTempSprite(rect, TempSprite.EmptyPot);
            DrawDropCue(drawContext, rect, currentDropCue);

            var mergeStatus = _drag.Type == DragPayloadType.Plant
                ? PlantDragTargetStatus(_drag, currentTarget)
                : default(PlantDropStatus);
            if (!ShouldShowMergeHint(_drag.Type, mergeStatus)) return;

            var labelWidth = drawContext.Styles.Text(
                RuntimeUiTypographyRole.Supplemental,
                TextAnchor.MiddleCenter).CalcSize(new GUIContent(mergeStatus.Reason)).x;
            var hintRect = layout.MergeHint(rect, labelWidth);
            RuntimeUiGui.DrawStandardPanel(
                drawContext, hintRect, RuntimeUiInteractionState.Warning);
            RuntimeUiGui.DrawText(drawContext, BattleUiLayout.CueLabel(hintRect),
                mergeStatus.Reason, RuntimeUiTypographyRole.Supplemental,
                RuntimeUiTextTone.State, TextAnchor.MiddleCenter,
                RuntimeUiInteractionState.Warning);
            RuntimeUiGui.DrawStateIndicator(
                drawContext, hintRect, RuntimeUiInteractionState.Warning);
            DrawDropCue(drawContext, hintRect, BattleUiDropCue.Merge);
        }

        private void DrawOverlay(
            BattleUiLayout layout, RuntimeUiDrawContext drawContext)
        {
            var state = BattleUiPresentationState.Create(
                _game.State.Phase, _game.State.Paused);
            if (!state.ShowsOverlay) return;

            var content = state.ModalContent(_game.State.WaveIndex, _game.MaxWaves);
            if (state.Mode == BattleUiChromeMode.Paused)
                DrawModal(layout, drawContext, content,
                    () => _game.TogglePause(), RestartRun);
            else
                DrawModal(layout, drawContext, content, RestartRun);
        }

        private void DrawModal(
            BattleUiLayout layout,
            RuntimeUiDrawContext drawContext,
            BattleUiModalContent content,
            Action primaryCallback,
            Action secondaryCallback = null)
        {
            var modalRect = content.UsesResultCard ? layout.TerminalModal : layout.Modal;
            RuntimeUiGui.DrawBlockingModal(
                drawContext, layout.Design, modalRect, content.SurfaceState);
            if (content.UsesResultCard)
            {
                RuntimeUiGui.DrawResultCard(
                    drawContext, modalRect, RuntimeUiInteractionState.Normal);
                RuntimeUiGui.DrawResultBanner(
                    drawContext, layout.ModalResultBanner);
                RuntimeUiGui.DrawSingleLineText(drawContext,
                    layout.ModalResultBannerText, content.ResultBannerText,
                    RuntimeUiTypographyRole.SectionTitle, RuntimeUiTextTone.State,
                    TextAnchor.MiddleCenter, content.SurfaceState);
                RuntimeUiGui.DrawOrchardVista(
                    drawContext, layout.ModalOrchardVista);
            }
            RuntimeUiGui.DrawSectionRibbon(
                drawContext, content.UsesResultCard
                    ? layout.ModalTerminalTitle : layout.ModalTitle,
                content.SurfaceState);
            RuntimeUiGui.DrawSingleLineText(drawContext,
                content.UsesResultCard ? layout.ModalTerminalTitle : layout.ModalTitle,
                content.Title,
                RuntimeUiTypographyRole.SectionTitle, RuntimeUiTextTone.State,
                TextAnchor.MiddleCenter, content.SurfaceState);
            var messageRect = content.UsesResultCard
                ? layout.ModalTerminalMessage : layout.ModalMessage;
            if (content.MessageLines.HasSecondLine)
            {
                RuntimeUiGui.DrawControlledTwoLineText(drawContext, messageRect,
                    content.MessageLines, RuntimeUiTypographyRole.Body,
                    RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter,
                    content.SurfaceState);
            }
            else
            {
                RuntimeUiGui.DrawSingleLineText(drawContext, messageRect,
                    content.MessageLines.FirstLine, RuntimeUiTypographyRole.Body,
                    RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter,
                    content.SurfaceState);
            }
            if (content.UsesResultCard)
            {
                RuntimeUiGui.DrawIndicator(drawContext, layout.ModalResultIndicator,
                    content.SurfaceState == RuntimeUiInteractionState.Success
                        ? RuntimeUiIndicatorKind.Success
                        : RuntimeUiIndicatorKind.Error);
            }
            else
            {
                RuntimeUiGui.DrawIndicator(drawContext, layout.ModalPauseIndicator,
                    RuntimeUiIndicatorKind.Warning);
            }
            var actionCount = content.ActionCount;
            var primaryRect = layout.ModalAction(0, actionCount);
            var primaryPress = TrackBattleAction(
                ModalPrimaryFeedbackTarget, primaryRect);
            var primaryState = BattleUiPresentationState.ResolveActionState(
                false, primaryPress.Hovered, primaryPress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, primaryRect,
                content.PrimaryAction, content.PrimaryActionKind,
                primaryState, content.PrimaryActionIcon,
                motion: BattleActionMotion(ModalPrimaryFeedbackTarget));
            if (primaryPress.Activated)
            {
                BeginBattleActionPress(ModalPrimaryFeedbackTarget);
                primaryCallback();
            }

            if (actionCount != 2) return;
            var secondaryRect = layout.ModalAction(1, actionCount);
            var secondaryPress = TrackBattleAction(
                ModalSecondaryFeedbackTarget, secondaryRect);
            var secondaryState = BattleUiPresentationState.ResolveActionState(
                false, secondaryPress.Hovered, secondaryPress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, secondaryRect,
                content.SecondaryAction, content.SecondaryActionKind,
                secondaryState, content.SecondaryActionIcon,
                motion: BattleActionMotion(ModalSecondaryFeedbackTarget));
            if (secondaryPress.Activated)
            {
                BeginBattleActionPress(ModalSecondaryFeedbackTarget);
                secondaryCallback();
            }
        }

        private void RestartRun()
        {
            if (!RestartCurrentSession(out var errorCode))
                SetStatus(false, errorCode);
        }

        private void ResetInteractionState()
        {
            ApplyRestartPresentation(new RestartPresentationState());
            _actionPressTarget = 0;
            _actionPressPulse = default;
            _actionPressTracker.Cancel();
        }

        private RestartPresentationState CaptureRestartPresentation()
        {
            return new RestartPresentationState
            {
                InspectedPlantId = _inspectedPlantId,
                SelectedWeapon = _selectedWeapon,
                PotToolSelected = _potToolSelected,
                Status = _status,
                StatusSuccess = _statusSuccess,
                StatusPulse = _statusPulse,
                Drag = _drag,
                DragControlId = _dragControlId,
                ReturnPulsePlantId = _returnPulsePlantId,
                ReturnPulse = _returnPulse,
                NurseryRollDisplayPulse = _nurseryRollDisplayPulse,
                SelectionPulseTarget = _selectionPulseTarget,
                SelectionPulse = _selectionPulse,
            };
        }

        private void ApplyRestartPresentation(RestartPresentationState presentation)
        {
            _inspectedPlantId = presentation.InspectedPlantId;
            _selectedWeapon = presentation.SelectedWeapon;
            _potToolSelected = presentation.PotToolSelected;
            _status = presentation.Status;
            _statusSuccess = presentation.StatusSuccess;
            _statusPulse = presentation.StatusPulse;
            InvalidatePreparedStatusText();
            _drag = presentation.Drag;
            _dragControlId = presentation.DragControlId;
            _returnPulsePlantId = presentation.ReturnPulsePlantId;
            _returnPulse = presentation.ReturnPulse;
            _nurseryRollDisplayPulse = presentation.NurseryRollDisplayPulse;
            _selectionPulseTarget = presentation.SelectionPulseTarget;
            _selectionPulse = presentation.SelectionPulse;
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
            presentation.StatusSuccess = true;
            presentation.StatusPulse = default;
            presentation.Drag = null;
            presentation.DragControlId = 0;
            presentation.ReturnPulsePlantId = -1;
            presentation.ReturnPulse = default;
            presentation.NurseryRollDisplayPulse = default;
            presentation.SelectionPulseTarget = 0;
            presentation.SelectionPulse = default;
        }

        private static Texture2D CreateAttackRangeTexture()
        {
            var size = AttackRangeTextureSize;
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

        private void DrawStatefulTempSprite(RuntimeUiDrawContext drawContext,
            Rect rect, TempSprite sprite, RuntimeUiInteractionState state)
        {
            var opacity = state == RuntimeUiInteractionState.Disabled
                ? drawContext.Opacity(state)
                : 1f;
            DrawTempSprite(rect, sprite, new Color(1f, 1f, 1f, opacity));
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
            var idlePhase = _game.State.Elapsed * 2.2f + plant.Id * .73f;
            var idlePulse = Mathf.Sin(idlePhase);
            rect.y -= idlePulse * .65f;
            rect = ScaleAroundCenter(rect, 1f + idlePulse * .012f, 1f - idlePulse * .008f);
            var angle = Mathf.Sin(idlePhase * .67f) * 1.25f;
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
            DrawTempSprite(rect, PlantSprite(plant));
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

        private static TempSprite PlantSprite(Plant plant)
        {
            return plant != null && plant.Weapon != WeaponKind.None
                ? WeaponSprite(plant.Weapon)
                : PlantSprite(plant == null ? PlantKind.Pea : plant.Kind);
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
            _status = text ?? string.Empty;
            _statusSuccess = success;
            InvalidatePreparedStatusText();
            _statusPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
        }

        private void PrepareTransientStatusText(RuntimeUiDrawContext drawContext,
            Rect statusRect, string text, RuntimeUiInteractionState state)
        {
            if (string.Equals(_preparedStatusSource, text, StringComparison.Ordinal)
                && Mathf.Abs(_preparedStatusWidth - statusRect.width) <= .001f)
                return;

            _preparedStatusTextMode = RuntimeUiGui.ResolveStatusTextMode(
                drawContext, statusRect, text, state,
                RuntimeUiTypographyRole.Supplemental);
            var textLayout = RuntimeUiGui.ResolveStatusTextLayout(
                drawContext, statusRect, state,
                RuntimeUiTypographyRole.Supplemental, _preparedStatusTextMode);
            _preparedStatusTextLines = RuntimeUiGui.ResolveStatusTextLines(textLayout, text);
            _preparedStatusSource = text;
            _preparedStatusWidth = statusRect.width;
        }

        private void InvalidatePreparedStatusText()
        {
            _preparedStatusSource = string.Empty;
            _preparedStatusWidth = -1f;
            _preparedStatusTextMode = RuntimeUiStatusTextMode.SingleLine;
            _preparedStatusTextLines = default;
        }

        private Vector2 ToBoard(Vector2 point)
        {
            return Projection.MapToScreen(point);
        }

        private void DrawWorldLabel(Rect rect, string text, int fontSize, Color color)
        {
            _worldLabelStyle.fontSize = fontSize;
            _worldLabelStyle.normal.textColor = color;
            GUI.Label(rect, text, _worldLabelStyle);
        }

        private static void DrawWorldRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawWorldOutline(Rect rect, float thickness, Color color)
        {
            thickness = Mathf.Max(1f, Mathf.Min(thickness, Mathf.Min(rect.width, rect.height) * .5f));
            DrawWorldRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawWorldRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawWorldRect(new Rect(rect.x, rect.y + thickness,
                thickness, rect.height - thickness * 2f), color);
            DrawWorldRect(new Rect(rect.xMax - thickness, rect.y + thickness,
                thickness, rect.height - thickness * 2f), color);
        }

        private Color ThemeColor(Func<LevelPresentationThemeDefinition, string> select,
            Color fallback)
        {
            var theme = _game == null ? null : _game.Theme;
            if (theme == null || select == null) return fallback;
            return ColorUtility.TryParseHtmlString(select(theme), out var color) ? color : fallback;
        }

    }
}
