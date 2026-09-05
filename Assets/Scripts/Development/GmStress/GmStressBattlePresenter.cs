#if UNITY_EDITOR || DEVELOPMENT_BUILD
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

namespace FruitDefense.Development.GmStress
{
    [DisallowMultipleComponent]
    public sealed class GmStressBattlePresenter : MonoBehaviour, IBattleSessionHost
    {
        public const string SnapshotUnsupported = "gm-stress-snapshot-unsupported";
        public const string ResultUnsupported = "gm-stress-result-unsupported";
        public const string LevelMismatch = "gm-stress-level-mismatch";
        public const string TerrainPaletteRequired = "gm-stress-terrain-palette-required";
        public const string TerrainPaletteInvalid = "gm-stress-terrain-palette-invalid";
        public const string CombatVfxAtlasInvalid = "gm-stress-combat-vfx-atlas-invalid";

        private enum AtlasSprite
        {
            Pea,
            Watermelon,
            Banana,
            Durian,
            Sunflower,
            NormalEnemy,
            RunnerEnemy,
            ArmoredEnemy,
            BossEnemy,
            Gatling,
            Ice,
            Chili,
            EmptyPot,
            OccupiedPot,
        }

        private readonly BattlePresentationBuffer _presentation =
            new BattlePresentationBuffer();
        private readonly GmStressPlantDragInteractor _plantDrag =
            new GmStressPlantDragInteractor();
        private GmStressBattleController _controller;
        private BattleLaunchRequest _currentRequest;
        private IAppNavigator _navigator;
        private RuntimeUiTheme _runtimeUiTheme;
        private BattlefieldTerrainPalette _terrainPalette;
        private RuntimeUiDrawContext _drawContext;
        private GmStressBattleLayout _layout;
        private CombatFloatingTextSdfOverlay _floatingTextOverlay;
        private Texture2D _tempArtAtlas;
        private Texture2D _combatVfxAtlas;
        private GUIStyle _worldLabel;
        private bool _initialized;
        private bool _disposed;
        private bool _hasEnteredBattleRoute;
        private int _selectedEnemyIndex;
        private int _selectedBatchIndex;
        private int _selectedPlantIndex;
        private int _plantDragControlId;
        private RuntimeUiCompactControlState _pauseCompactControlState;
        private RuntimeUiCompactControlState _speedCompactControlState;
        private string _status = "选择怪物和批量，点击顶部任一路出怪";
        private RuntimeUiInteractionState _statusState = RuntimeUiInteractionState.Normal;

        public BattleSessionStatus Status
        {
            get
            {
                var simulation = _controller == null ? null : _controller.Simulation;
                return !_initialized || simulation == null
                    ? BattleSessionStatus.Uninitialized
                    : new BattleSessionStatus(true, simulation.State.Phase,
                        simulation.State.WaveIndex, simulation.State.Lives,
                        simulation.State.Paused, false);
            }
        }

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            _tempArtAtlas = Resources.Load<Texture2D>("TempArt/fruit-defense-temp-atlas");
            if (_tempArtAtlas != null)
            {
                _tempArtAtlas.filterMode = FilterMode.Bilinear;
                _tempArtAtlas.wrapMode = TextureWrapMode.Clamp;
            }
        }

        public BattleSessionInitializationResult InitializeGm(
            BattleLaunchRequest request,
            IAppNavigator navigator,
            IBattleResultSink resultSink,
            RuntimeUiTheme runtimeUiTheme,
            BattlefieldTerrainPalette terrainPalette)
        {
            if (_initialized)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.AlreadyInitialized);
            if (request == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.InvalidRequest);
            if (!request.TryValidate(out var requestError))
                return BattleSessionInitializationResult.Failed(requestError);
            if (request.Mode != BattleSessionMode.GmStress)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.SessionModeMismatch);
            if (!string.Equals(request.LevelId, GmStressBattleIds.LevelId,
                    StringComparison.Ordinal))
                return BattleSessionInitializationResult.Failed(LevelMismatch);
            if (navigator == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.NavigatorRequired);
            if (resultSink == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.ResultSinkRequired);
            if (runtimeUiTheme == null)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.RuntimeUiThemeRequired);
            if (terrainPalette == null)
                return BattleSessionInitializationResult.Failed(TerrainPaletteRequired);
            _combatVfxAtlas = Resources.Load<Texture2D>(
                BattleCombatGuiRenderer.AtlasResourcePath);
            if (!BattleCombatGuiRenderer.ValidateAtlas(
                    _combatVfxAtlas, out var combatAtlasError))
                return BattleSessionInitializationResult.Failed(
                    CombatVfxAtlasInvalid + ":" + combatAtlasError);
            _combatVfxAtlas.filterMode = FilterMode.Bilinear;
            _combatVfxAtlas.wrapMode = TextureWrapMode.Clamp;
            var themeValidation = runtimeUiTheme.Validate();
            if (!themeValidation.IsValid)
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.RuntimeUiThemeInvalid + ":"
                    + themeValidation.Issues[0].Code);

            try
            {
                _controller = GmStressBattleFactory.Create(request.Seed);
                if (!GmStressBattleFactory.ValidateTerrainPalette(
                        _controller.Simulation.Map, terrainPalette, out var terrainError))
                {
                    _controller.Dispose();
                    _controller = null;
                    return BattleSessionInitializationResult.Failed(
                        TerrainPaletteInvalid + ":" + terrainError);
                }
                if (!string.Equals(request.ContentVersion,
                        _controller.Simulation.Content.Header.contentVersion,
                        StringComparison.Ordinal))
                {
                    _controller.Dispose();
                    _controller = null;
                    return BattleSessionInitializationResult.Failed(
                        BattleSessionInitializationResult.ContentVersionMismatch);
                }
                _layout = new GmStressBattleLayout(_controller.Simulation.Map);
                if (!CombatFloatingTextSdfOverlay.TryCreate(transform,
                        out _floatingTextOverlay, out var overlayError))
                    throw new InvalidOperationException(overlayError);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _controller?.Dispose();
                _controller = null;
                return BattleSessionInitializationResult.Failed(
                    BattleSessionInitializationResult.SimulationConstructionFailed);
            }

            _currentRequest = request;
            _navigator = navigator;
            _runtimeUiTheme = runtimeUiTheme;
            _terrainPalette = terrainPalette;
            _worldLabel = new GUIStyle
            {
                font = runtimeUiTheme.Typography.For(
                    RuntimeUiTypographyRole.Body).Font,
                fontStyle = FontStyle.Normal,
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = Color.white },
            };
            _presentation.Clear();
            _presentation.Consume(_controller.Simulation);
            _hasEnteredBattleRoute = navigator.CurrentRoute == AppRoute.Battle;
            _navigator.RouteChanged += HandleRouteChanged;
            _initialized = true;
            _disposed = false;
            return BattleSessionInitializationResult.Succeeded();
        }

        BattleSessionInitializationResult IBattleSessionHost.Initialize(
            BattleLaunchRequest request,
            IAppNavigator navigator, IBattleResultSink resultSink,
            RuntimeUiTheme runtimeUiTheme, CompiledLevelCatalog levelCatalog,
            CompiledOutgameContentCatalog outgameCatalog)
        {
            return BattleSessionInitializationResult.Failed(
                BattleSessionInitializationResult.SessionModeMismatch);
        }

        private void Update()
        {
            if (!_initialized || _controller == null) return;
            if (Input.GetKeyDown(KeyCode.Space)) _controller.TogglePause();
            if (Input.GetKeyDown(KeyCode.Alpha1)) _controller.SetSpeed(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _controller.SetSpeed(2);

            _controller.AdvanceFrame(Time.unscaledDeltaTime);
            _presentation.Advance(Time.unscaledDeltaTime,
                _controller.Simulation.State.Paused,
                _controller.Simulation.State.Speed);
            _presentation.Consume(_controller.Simulation);
            _presentation.RoutePendingAudio(SilentCombatAudioRouter.Instance);
            ResolveFloatingTextFollowAnchors();
            SyncFloatingTextOverlay();
        }

        public void HandlePlatformVisibility(PlatformVisibility visibility)
        {
            if (!_initialized || _controller == null
                || visibility != PlatformVisibility.Background) return;
            _controller.Simulation.State.Paused = true;
            _controller.Simulation.ResetFrameAccumulator();
        }

        public bool RestartCurrentSession(out string errorCode)
        {
            if (!_initialized || _currentRequest == null)
            {
                errorCode = FruitDefenseGame.SessionNotInitialized;
                return false;
            }
            _controller.Dispose();
            _controller = GmStressBattleFactory.Create(_currentRequest.Seed);
            _layout = new GmStressBattleLayout(_controller.Simulation.Map);
            _presentation.Clear();
            _presentation.Consume(_controller.Simulation);
            _selectedEnemyIndex = 0;
            _selectedBatchIndex = 0;
            _selectedPlantIndex = 0;
            CancelPlantDrag();
            _pauseCompactControlState = default;
            _speedCompactControlState = default;
            _status = "压力关已重置";
            _statusState = RuntimeUiInteractionState.Success;
            errorCode = string.Empty;
            return true;
        }

        public BattleSnapshotExportResult ExportCurrentSessionSnapshot()
        {
            return BattleSnapshotExportResult.Unsupported(SnapshotUnsupported);
        }

        public BattleSnapshotRestoreResult RestoreCurrentSessionSnapshot(
            BattleSnapshot snapshot, CompiledLevelCatalog levelCatalog)
        {
            return new BattleSnapshotRestoreResult(
                BattleSnapshotRestoreCode.UnsupportedSessionSource,
                "session.source", SnapshotUnsupported);
        }

        public bool TrySubmitTerminalResult()
        {
            return false;
        }

        public void DisposeSession()
        {
            if (_disposed) return;
            _disposed = true;
            _initialized = false;
            if (_navigator != null) _navigator.RouteChanged -= HandleRouteChanged;
            _navigator = null;
            _controller?.Dispose();
            _controller = null;
            _presentation.Clear();
            if (_floatingTextOverlay != null)
            {
                _floatingTextOverlay.Dispose();
                _floatingTextOverlay = null;
            }
            _runtimeUiTheme = null;
            _terrainPalette = null;
            _drawContext = null;
            _currentRequest = null;
            _pauseCompactControlState = default;
            _speedCompactControlState = default;
            CancelPlantDrag();
        }

        private void OnDestroy()
        {
            DisposeSession();
            _tempArtAtlas = null;
            _combatVfxAtlas = null;
            _worldLabel = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) HandlePlatformVisibility(PlatformVisibility.Background);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) HandlePlatformVisibility(PlatformVisibility.Background);
        }

        private void HandleRouteChanged(AppRoute route)
        {
            if (route == AppRoute.Battle)
            {
                _hasEnteredBattleRoute = true;
                return;
            }
            if (_hasEnteredBattleRoute) Destroy(gameObject);
        }

        private void ResolveFloatingTextFollowAnchors()
        {
            foreach (var feedback in _presentation.Feedback)
            {
                if (feedback == null || !feedback.IsFollowingTarget) continue;
                if (feedback.Kind == BattlePresentationEventKind.EntityDefeated)
                {
                    feedback.Point = feedback.EventPoint;
                    feedback.DetachFromTarget();
                    continue;
                }
                var zombie = _controller.Simulation.ZombieById(
                    feedback.TargetEntityId);
                if (zombie != null && zombie.Hp > 0f)
                {
                    feedback.UpdateFollowPoint(
                        _controller.Simulation.ZombiePoint(zombie));
                    continue;
                }
                var plant = _controller.Simulation.PlantById(
                    feedback.TargetEntityId);
                if (plant != null && plant.PotId >= 0)
                {
                    feedback.UpdateFollowPoint(
                        _controller.Simulation.PotPoint(
                            _controller.Simulation.PotById(plant.PotId)));
                    continue;
                }
                feedback.DetachFromTarget();
            }
        }

        private void SyncFloatingTextOverlay()
        {
            if (_floatingTextOverlay == null || _layout == null) return;
            var viewport = BattlefieldProjection.CalculateViewportLayout(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent(),
                GmStressBattleLayout.DesignWidth, GmStressBattleLayout.DesignHeight);
            _floatingTextOverlay.Sync(_presentation.Feedback,
                _presentation.FloatingTextStyles, _layout.Battlefield,
                viewport, _layout.BoardPanel, _presentation.BattlefieldOffset);
        }

        private void OnGUI()
        {
            if (!_initialized || _controller == null || _layout == null) return;
            var outerMatrix = GUI.matrix;
            try
            {
                _drawContext = RuntimeUiGui.RequireContext(
                    _drawContext, _runtimeUiTheme, 1f);
                var viewport = BattlefieldProjection.CalculateViewportLayout(
                    Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent(),
                    GmStressBattleLayout.DesignWidth, GmStressBattleLayout.DesignHeight);
                GUI.matrix = Matrix4x4.identity;
                RuntimeUiGui.DrawScreenBackground(_drawContext,
                    new Rect(0f, 0f, Screen.width, Screen.height));
                GUI.matrix = viewport.GuiMatrix;
                HandlePlantDragInput(Event.current);
                DrawHeader();
                DrawBoard();
                DrawControls();
                RuntimeUiGui.DrawStatus(_drawContext, _layout.Status,
                    _status, _statusState, RuntimeUiTypographyRole.Supplemental,
                    RuntimeUiStatusTextMode.Standard);
                DrawPlantDragGhost();
                _floatingTextOverlay?.DrawOnGuiRepaint();
            }
            finally
            {
                GUI.matrix = outerMatrix;
            }
        }

        private void DrawHeader()
        {
            RuntimeUiGui.DrawStandardPanel(_drawContext, _layout.Header);
            RuntimeUiGui.DrawSingleLineText(_drawContext, _layout.HeaderTitle,
                "GM 八路压力测试", RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleLeft);
            RuntimeUiGui.DrawMetric(_drawContext, _layout.ActiveMetric,
                RuntimeUiArtSlot.IconResourceWaveMicro, "活动", _controller.ActiveCount.ToString(),
                compactInline: true, compactIconSize: 18f);
            RuntimeUiGui.DrawMetric(_drawContext, _layout.PendingMetric,
                RuntimeUiArtSlot.IconResourceWaveMicro, "队列", _controller.PendingCount.ToString(),
                compactInline: true, compactIconSize: 18f);
            RuntimeUiGui.DrawMetric(_drawContext, _layout.EscapedMetric,
                RuntimeUiArtSlot.IconResourceCoreMicro, "逃逸", _controller.EscapedCount.ToString(),
                compactInline: true, compactIconSize: 18f);

            var pauseState = ButtonState(_layout.PauseAction);
            var pauseLifecycle = RuntimeUiCompactControlLifecycle.Evaluate(
                _pauseCompactControlState, _controller.IsPaused, Time.unscaledTime,
                _runtimeUiTheme.Feedback);
            _pauseCompactControlState = pauseLifecycle.State;
            RuntimeUiGui.DrawCompactControlVisual(_drawContext, _layout.PauseAction,
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.PauseContinue),
                pauseState, pauseLifecycle.Sample,
                _controller.IsPaused
                    ? RuntimeUiArtSlot.IconControlContinue
                    : RuntimeUiArtSlot.IconControlPause);
            if (GUI.Button(_layout.PauseAction, GUIContent.none,
                    _drawContext.Styles.HitTarget))
                _controller.TogglePause();

            var speedState = ButtonState(_layout.SpeedAction);
            var speedLifecycle = RuntimeUiCompactControlLifecycle.Evaluate(
                _speedCompactControlState, _controller.Speed != 1, Time.unscaledTime,
                _runtimeUiTheme.Feedback);
            _speedCompactControlState = speedLifecycle.State;
            RuntimeUiGui.DrawCompactControlVisual(_drawContext, _layout.SpeedAction,
                BattleUiPresentationState.ResolveActionSpec(BattleUiActionSemantic.Speed),
                speedState, speedLifecycle.Sample,
                multiplierText: _controller.Simulation.State.Speed + "×");
            if (GUI.Button(_layout.SpeedAction, GUIContent.none,
                    _drawContext.Styles.HitTarget))
                _controller.SetSpeed(_controller.Speed == 1 ? 2 : 1);
        }

        private void DrawBoard()
        {
            RuntimeUiGui.DrawStandardPanel(_drawContext, _layout.BoardPanel);
            var offset = _presentation.BattlefieldOffset;
            var grid = _layout.Battlefield.GridRect;
            var worldMatrix = GUI.matrix;
            try
            {
                GUI.matrix = worldMatrix * Matrix4x4.Translate(
                    new Vector3(offset.x, offset.y, 0f));
                BattlefieldTerrainGuiRenderer.DrawValidated(
                    _controller.Simulation.Map, _layout.Battlefield, _terrainPalette);
            }
            finally
            {
                GUI.matrix = worldMatrix;
            }
            for (var lane = 0; lane < GmStressBattleIds.GridWidth; lane++)
            for (var row = 0; row < GmStressBattleIds.GridHeight; row++)
            {
                var cell = new Vector2Int(lane, row);
                var hit = _layout.Battlefield.TileRect(cell);
                var visual = Offset(hit, offset);
                DrawOutline(Inset(visual, .5f), 1f,
                    new Color(.15f, .27f, .12f, .28f));
                if (row == 0)
                {
                    DrawOutline(Inset(visual, 2f), 2f,
                        new Color(1f, .79f, .24f, .92f));
                    DrawWorldLabel(visual, (lane + 1).ToString());
                    if (GUI.Button(hit, GUIContent.none, _drawContext.Styles.HitTarget))
                        SpawnLane(lane);
                }
            }

            DrawPots(offset);
            DrawProjectiles(offset);
            DrawZombies(offset);
            DrawCombatEffects(offset);
            if (_presentation.BattlefieldFlash > 0f)
                DrawRect(Offset(grid, offset),
                    new Color(1f, .92f, .55f, _presentation.BattlefieldFlash));
        }

        private void DrawPots(Vector2 offset)
        {
            var dragTarget = CurrentPlantDragTarget();
            foreach (var pot in _controller.Simulation.State.Pots
                .OrderBy(value => value.Id))
            {
                if (!pot.Active) continue;
                var hit = _layout.Battlefield.PotHitRect(pot.Cell);
                var visual = Offset(_layout.Battlefield.PotVisualRect(pot.Cell), offset);
                var plant = _controller.Simulation.PlantAtPot(pot.Id);
                DrawAtlas(visual, plant == null ? AtlasSprite.EmptyPot : AtlasSprite.OccupiedPot,
                    Color.white);
                if (plant != null)
                {
                    var reaction = _presentation.ReactionFor(plant.Id);
                    var plantRect = ScaleAroundCenter(visual, .9f, .9f);
                    plantRect = ApplyPlantVisualHeight(plantRect,
                        PlantVisualHeightOffset(plant));
                    BattleCombatGuiRenderer.DrawPlant(_tempArtAtlas, plantRect,
                        (int)PlantSprite(plant.DefinitionId),
                        _controller.Simulation.State.Elapsed, plant.Id, reaction);
                }
                if (dragTarget != null && dragTarget.Id == pot.Id)
                    RuntimeUiGui.DrawIndicator(_drawContext,
                        BattleUiLayout.CueBadge(hit), RuntimeUiIndicatorKind.DragLegal);
            }
        }

        private void DrawZombies(Vector2 offset)
        {
            foreach (var zombie in _controller.Simulation.State.Zombies
                .OrderBy(value => value.Id))
            {
                var reaction = _presentation.ReactionFor(zombie.Id);
                var center = _layout.Battlefield.MapToScreen(
                        _controller.Simulation.ZombiePoint(zombie))
                    + offset + reaction.Offset;
                var size = Mathf.Min(42f, _layout.Battlefield.TileSize * .82f);
                var rect = new Rect(center.x - size * .5f, center.y - size * .5f, size, size);
                rect = ScaleAroundCenter(rect, reaction.Scale.x, reaction.Scale.y);
                var frozen = _controller.Simulation.HasStatus(
                    zombie.Id, BattleContentIds.Statuses.IceFreeze);
                var slowed = _controller.Simulation.HasStatus(
                    zombie.Id, BattleContentIds.Statuses.IceSlow);
                if (frozen) BattleCombatGuiRenderer.DrawFrozenAura(
                    _combatVfxAtlas, rect, new Color(1f, 1f, 1f, .82f));
                var baseTint = slowed ? new Color(.72f, .9f, 1f) : Color.white;
                DrawAtlas(rect, EnemySprite(zombie.DefinitionId),
                    Color.Lerp(baseTint, new Color(1f, .9f, .55f), reaction.Flash));
                if (_controller.Simulation.HasStatus(
                        zombie.Id, BattleContentIds.Statuses.ChiliBurn))
                    BattleCombatGuiRenderer.DrawBurningStatus(
                        _combatVfxAtlas, rect);
                var hp = new Rect(center.x - size * .42f, rect.y - 2f, size * .84f, 2f);
                DrawRect(hp, new Color(.19f, .13f, .1f, 1f));
                hp.width *= Mathf.Clamp01(zombie.Hp / zombie.MaxHp);
                DrawRect(hp, new Color(.86f, .22f, .16f, 1f));
            }
        }

        private void DrawProjectiles(Vector2 offset)
        {
            var visualScale = _layout.Battlefield.LegacyVisualSize(1f);
            foreach (var projectile in _controller.Simulation.State.Projectiles)
            {
                var point = _layout.Battlefield.MapToScreen(projectile.Position) + offset;
                BattleCombatGuiRenderer.DrawProjectile(_combatVfxAtlas, point,
                    visualScale, projectile,
                    _controller.Simulation.Content.Projectiles[
                        projectile.ProjectileId].presentationId,
                    _controller.Simulation.State.Elapsed);
            }
        }

        private void DrawCombatEffects(Vector2 offset)
        {
            var visualScale = _layout.Battlefield.LegacyVisualSize(1f);
            foreach (var effect in _presentation.CombatEffects)
            {
                var point = _layout.Battlefield.MapToScreen(effect.Position) + offset;
                BattleCombatGuiRenderer.DrawCombatEffect(_combatVfxAtlas, point,
                    visualScale, effect);
            }
        }

        private void DrawControls()
        {
            RuntimeUiGui.DrawStandardPanel(_drawContext, _layout.EnemyPanel);
            RuntimeUiGui.DrawSingleLineText(_drawContext,
                new Rect(20f, 528f, 350f, 22f), "怪物类型",
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Secondary,
                TextAnchor.MiddleLeft);
            for (var index = 0; index < GmStressBattleIds.EnemyDefinitionIds.Count; index++)
                DrawSelector(_layout.EnemyChoice(index), EnemyName(index),
                    index == _selectedEnemyIndex, () => _selectedEnemyIndex = index,
                    AtlasSpriteForEnemySelector(index));

            RuntimeUiGui.DrawStandardPanel(_drawContext, _layout.BatchPanel);
            for (var index = 0; index < GmStressBattleIds.BatchCounts.Count; index++)
            {
                var captured = index;
                DrawSelector(_layout.BatchChoice(index),
                    "×" + GmStressBattleIds.BatchCounts[index],
                    index == _selectedBatchIndex,
                    () => _selectedBatchIndex = captured, null);
            }
            if (RuntimeUiGui.DrawAction(_drawContext, _layout.AllLanesAction,
                    "全路生成",
                    new RuntimeUiActionSpec(RuntimeUiActionKind.Primary,
                        RuntimeUiActionContentForm.Text,
                        RuntimeUiActionBehavior.Instantaneous),
                    ButtonState(_layout.AllLanesAction)))
                SpawnAllLanes();

            RuntimeUiGui.DrawStandardPanel(_drawContext, _layout.PlantPanel);
            RuntimeUiGui.DrawSingleLineText(_drawContext,
                new Rect(20f, 696f, 350f, 22f), "免费植物 · 拖到花盆放置/替换",
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Secondary,
                TextAnchor.MiddleLeft);
            for (var index = 0; index < GmStressBattleIds.PlantDefinitionIds.Count; index++)
            {
                var captured = index;
                DrawSelector(_layout.PlantChoice(index), PlantName(index),
                    index == _selectedPlantIndex
                    || _plantDrag.HasSource && index == _plantDrag.PlantIndex,
                    () => _selectedPlantIndex = captured,
                    PlantSprite(GmStressBattleIds.PlantDefinitionIds[index]), false);
            }
        }

        private void DrawSelector(Rect rect, string label, bool selected,
            Action activate, AtlasSprite? sprite, bool acceptsClick = true)
        {
            var state = selected
                ? RuntimeUiInteractionState.Selected : ButtonState(rect);
            var visualRect = RuntimeUiGui.DrawSlot(_drawContext, rect, RuntimeUiSlotKind.Tool,
                state, selected);
            if (sprite.HasValue)
            {
                var stacked = visualRect.height > 50f && visualRect.width < 80f;
                var iconRect = stacked
                    ? new Rect(visualRect.x + (visualRect.width - 34f) * .5f,
                        visualRect.y + 3f, 34f, 34f)
                    : new Rect(visualRect.x + 4f, visualRect.y + 4f,
                        visualRect.height - 8f, visualRect.height - 8f);
                DrawAtlas(iconRect, sprite.Value, Color.white);
            }
            var stackedLabel = sprite.HasValue
                && visualRect.height > 50f && visualRect.width < 80f;
            RuntimeUiGui.DrawSingleLineText(_drawContext,
                sprite.HasValue
                    ? stackedLabel
                        ? new Rect(visualRect.x + 3f, visualRect.yMax - 24f,
                            visualRect.width - 6f, 22f)
                        : new Rect(visualRect.x + visualRect.height, visualRect.y,
                            visualRect.width - visualRect.height - 3f,
                            visualRect.height)
                    : visualRect,
                label, RuntimeUiTypographyRole.Supplemental,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, state);
            if (acceptsClick
                && GUI.Button(rect, GUIContent.none, _drawContext.Styles.HitTarget))
                activate();
        }

        private void SpawnLane(int lane)
        {
            var enemyId = GmStressBattleIds.EnemyDefinitionIds[_selectedEnemyIndex];
            var batch = GmStressBattleIds.BatchCounts[_selectedBatchIndex];
            var success = _controller.EnqueueLane(lane, enemyId, batch,
                out _, out var reason);
            SetStatus(success, reason);
        }

        private void SpawnAllLanes()
        {
            var enemyId = GmStressBattleIds.EnemyDefinitionIds[_selectedEnemyIndex];
            var batch = GmStressBattleIds.BatchCounts[_selectedBatchIndex];
            var success = _controller.EnqueueAll(enemyId, batch,
                out _, out var reason);
            SetStatus(success, reason);
        }

        private void HandlePlantDragInput(Event evt)
        {
            if (evt == null) return;
            if (_controller.IsPaused)
            {
                if (_plantDrag.HasSource)
                {
                    CancelPlantDrag();
                    SetStatus(false, "拖拽已取消，植物卡保持可用");
                }
                return;
            }
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape
                && _plantDrag.HasSource)
            {
                CancelPlantDrag();
                SetStatus(false, "已取消拖拽，植物卡保持可用");
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var sourceIndex = PlantSourceIndexAt(evt.mousePosition);
                if (sourceIndex < 0) return;
                _plantDrag.Begin(sourceIndex, evt.mousePosition);
                _plantDragControlId = GUIUtility.GetControlID(
                    0x474D504C, FocusType.Passive);
                GUIUtility.hotControl = _plantDragControlId;
                evt.Use();
                return;
            }
            if (!_plantDrag.HasSource) return;

            if (evt.type == EventType.MouseDrag)
            {
                _plantDrag.Move(evt.mousePosition);
                if (_plantDrag.IsActive) UpdatePlantDragStatus();
                evt.Use();
                return;
            }
            if (evt.type != EventType.MouseUp && evt.rawType != EventType.MouseUp) return;

            var pots = ActivePots();
            var release = _plantDrag.Release(evt.mousePosition, PotHitRects(pots));
            ReleasePlantDragControl();
            _selectedPlantIndex = release.PlantIndex;
            if (release.Kind == GmStressPlantDragReleaseKind.Selected)
            {
                SetStatus(true, "已选择" + PlantName(release.PlantIndex)
                    + "，请拖到花盆上阵");
            }
            else if (release.Kind == GmStressPlantDragReleaseKind.Deploy)
            {
                var plantId = GmStressBattleIds.PlantDefinitionIds[release.PlantIndex];
                SetStatus(_controller.PlaceOrReplacePlant(
                    pots[release.PotIndex].Cell, plantId, out var reason), reason);
            }
            else SetStatus(false, "未命中花盆，植物卡保持可用");
            evt.Use();
        }

        private int PlantSourceIndexAt(Vector2 point)
        {
            for (var index = 0;
                 index < GmStressBattleIds.PlantDefinitionIds.Count; index++)
                if (_layout.PlantChoice(index).Contains(point)) return index;
            return -1;
        }

        private Pot[] ActivePots()
        {
            return _controller.Simulation.State.Pots.Where(value => value.Active)
                .OrderBy(value => value.Cell.y)
                .ThenBy(value => value.Cell.x)
                .ThenBy(value => value.Id).ToArray();
        }

        private Rect[] PotHitRects(IReadOnlyList<Pot> pots)
        {
            var rects = new Rect[pots.Count];
            for (var index = 0; index < pots.Count; index++)
                rects[index] = _layout.Battlefield.PotHitRect(pots[index].Cell);
            return rects;
        }

        private Pot CurrentPlantDragTarget()
        {
            if (!_plantDrag.IsActive) return null;
            var pots = ActivePots();
            var index = _plantDrag.CurrentPotIndex(PotHitRects(pots));
            return index < 0 ? null : pots[index];
        }

        private void UpdatePlantDragStatus()
        {
            var pot = CurrentPlantDragTarget();
            if (pot == null)
            {
                SetStatus(false, "请拖到一个花盆上");
                return;
            }
            SetStatus(true, _controller.Simulation.PlantAtPot(pot.Id) == null
                ? "松开放置一星植物"
                : "松开免费替换当前植物");
        }

        private void DrawPlantDragGhost()
        {
            if (!_plantDrag.IsActive) return;
            var rect = _layout.ClampDragPreview(
                DragGeometry.PreviewRect(_plantDrag.Current));
            DrawAtlas(rect, PlantSprite(
                GmStressBattleIds.PlantDefinitionIds[_plantDrag.PlantIndex]),
                Color.white);
        }

        private void CancelPlantDrag()
        {
            _plantDrag.Cancel();
            ReleasePlantDragControl();
        }

        private void ReleasePlantDragControl()
        {
            if (_plantDragControlId != 0
                && GUIUtility.hotControl == _plantDragControlId)
                GUIUtility.hotControl = 0;
            _plantDragControlId = 0;
        }

        private void SetStatus(bool success, string value)
        {
            _status = value ?? string.Empty;
            _statusState = success
                ? RuntimeUiInteractionState.Success
                : RuntimeUiInteractionState.Error;
        }

        private static RuntimeUiInteractionState ButtonState(Rect rect)
        {
            var evt = Event.current;
            if (evt == null || !rect.Contains(evt.mousePosition))
                return RuntimeUiInteractionState.Normal;
            return evt.button == 0 && (evt.rawType == EventType.MouseDown
                    || evt.rawType == EventType.MouseDrag)
                ? RuntimeUiInteractionState.Pressed
                : RuntimeUiInteractionState.HoveredOrFocused;
        }

        private void DrawAtlas(Rect rect, AtlasSprite sprite, Color tint)
        {
            BattleCombatGuiRenderer.DrawAtlasSprite(
                _tempArtAtlas, rect, (int)sprite, tint);
        }

        private void DrawWorldLabel(Rect rect, string text)
        {
            if (_worldLabel == null) return;
            var previous = GUI.contentColor;
            GUI.contentColor = Color.white;
            GUI.Label(rect, text ?? string.Empty, _worldLabel);
            GUI.contentColor = previous;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawOutline(Rect rect, float width, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return Rect.MinMaxRect(rect.xMin + amount, rect.yMin + amount,
                rect.xMax - amount, rect.yMax - amount);
        }

        private static Rect Offset(Rect rect, Vector2 offset)
        {
            rect.position += offset;
            return rect;
        }

        private static Rect ScaleAroundCenter(Rect rect, float scaleX, float scaleY)
        {
            var center = rect.center;
            rect.width *= scaleX;
            rect.height *= scaleY;
            rect.center = center;
            return rect;
        }

        private float PlantVisualHeightOffset(Plant plant)
        {
            if (plant == null || _controller == null
                || _controller.Simulation.Content == null) return 0f;
            PlantDefinitionDto definition;
            return _controller.Simulation.Content.Plants.TryGetValue(
                    plant.DefinitionId ?? string.Empty, out definition)
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

        private AtlasSprite PlantSprite(string definitionId)
        {
            PlantDefinitionDto definition;
            var presentationId = _controller != null
                && _controller.Simulation.Content.Plants.TryGetValue(
                    definitionId ?? string.Empty, out definition)
                    ? definition.presentationId : string.Empty;
            switch (BattlePresentationVisualCatalog.Plant(presentationId))
            {
                case PlantVisualArchetype.Watermelon: return AtlasSprite.Watermelon;
                case PlantVisualArchetype.Banana: return AtlasSprite.Banana;
                case PlantVisualArchetype.Durian: return AtlasSprite.Durian;
                case PlantVisualArchetype.Sunflower: return AtlasSprite.Sunflower;
                default: return AtlasSprite.Pea;
            }
        }

        private AtlasSprite EnemySprite(string definitionId)
        {
            EnemyDefinitionDto definition;
            var presentationId = _controller != null
                && _controller.Simulation.Content.Enemies.TryGetValue(
                    definitionId ?? string.Empty, out definition)
                    ? definition.presentationId : string.Empty;
            switch (BattlePresentationVisualCatalog.Enemy(presentationId))
            {
                case EnemyVisualArchetype.Runner: return AtlasSprite.RunnerEnemy;
                case EnemyVisualArchetype.Armored: return AtlasSprite.ArmoredEnemy;
                case EnemyVisualArchetype.Boss: return AtlasSprite.BossEnemy;
                default: return AtlasSprite.NormalEnemy;
            }
        }

        private AtlasSprite AtlasSpriteForEnemySelector(int index)
        {
            return EnemySprite(GmStressBattleIds.EnemyDefinitionIds[index]);
        }

        private static string EnemyName(int index)
        {
            switch (index)
            {
                case 1: return "疾速";
                case 2: return "装甲";
                case 3: return "首领";
                default: return "普通";
            }
        }

        private static string PlantName(int index)
        {
            switch (index)
            {
                case 1: return "西瓜";
                case 2: return "香蕉";
                case 3: return "榴莲";
                case 4: return "向日葵";
                default: return "豌豆";
            }
        }
    }
}
#endif
