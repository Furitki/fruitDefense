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
    public sealed partial class FruitDefenseGame
    {
        private void DrawHeader(BattleUiLayout layout, RuntimeUiDrawContext drawContext)
        {
            var viewState = BattleUiPresentationState.Create(
                _game.State.Phase, _game.State.Paused);
            RefreshHeaderMetricMotion();
            RuntimeUiGui.DrawStandardPanel(drawContext, layout.Header);
            var titleCopy = RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleTitle);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.HeaderTitle,
                titleCopy.Text,
                titleCopy.Role, titleCopy.Tone, titleCopy.Alignment);
            RuntimeUiGui.DrawMetric(drawContext, layout.SunMetric,
                RuntimeUiArtSlot.IconResourceSunMicro,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleSun).Text,
                _game.State.Sun.ToString(), compactInline: true,
                compactIconSize: BattleUiLayout.HeaderMetricIconSize,
                motion: RuntimeUiMotion.Evaluate(_sunPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop));
            RuntimeUiGui.DrawMetric(drawContext, layout.LivesMetric,
                RuntimeUiArtSlot.IconResourceCoreMicro,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleCore).Text,
                _game.State.Lives.ToString(), compactInline: true,
                compactIconSize: BattleUiLayout.HeaderMetricIconSize,
                motion: RuntimeUiMotion.Evaluate(_livesPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.StrongPop));
            RuntimeUiGui.DrawMetric(drawContext, layout.WaveMetric,
                RuntimeUiArtSlot.IconResourceWaveMicro,
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
                false, pausePress.Hovered, pausePress.Pressed);
            var pauseLifecycle = RuntimeUiCompactControlLifecycle.Evaluate(
                _pauseCompactControlState, viewState.IsPaused, Time.unscaledTime,
                _runtimeUiTheme.Feedback);
            _pauseCompactControlState = pauseLifecycle.State;
            RuntimeUiGui.DrawCompactControlVisual(drawContext, layout.PauseAction,
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.PauseContinue),
                pauseState, pauseLifecycle.Sample, viewState.PauseActionIcon,
                motion: BattleActionMotion(PauseActionFeedbackTarget));
            if (pausePress.Activated)
            {
                BeginBattleActionPress(PauseActionFeedbackTarget);
                _game.TogglePause();
            }

            var speedPress = TrackBattleAction(
                SpeedActionFeedbackTarget, layout.SpeedAction);
            var speedState = BattleUiPresentationState.ResolveActionState(
                false, speedPress.Hovered, speedPress.Pressed);
            var speedLifecycle = RuntimeUiCompactControlLifecycle.Evaluate(
                _speedCompactControlState, _game.State.Speed != 1,
                Time.unscaledTime, _runtimeUiTheme.Feedback);
            _speedCompactControlState = speedLifecycle.State;
            RuntimeUiGui.DrawCompactControlVisual(drawContext, layout.SpeedAction,
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.Speed),
                speedState, speedLifecycle.Sample,
                multiplierText: _game.State.Speed + "×",
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
            RuntimeUiGui.DrawGameplayStage(drawContext, layout.BattleStage);
            var worldMatrix = GUI.matrix;
            var worldOffset = _presentation.BattlefieldOffset;
            var texturedTerrain = false;
            try
            {
                GUI.matrix = worldMatrix * Matrix4x4.Translate(
                    new Vector3(worldOffset.x, worldOffset.y, 0f));
                texturedTerrain = DrawBattlefieldTerrain();
                DrawRouteTiles();
                DrawCore();
                DrawPlantingCells(texturedTerrain, layout);
                DrawInspectedAttackRange(layout);
            }
            finally
            {
                GUI.matrix = worldMatrix;
            }
            if (!texturedTerrain)
                DrawTerrainPresentationFailure(layout);
            if (_potToolSelected || (_drag != null && _drag.Active && _drag.Type == DragPayloadType.Pot))
                DrawExpansionCandidates(
                    layout, drawContext, currentDropTarget, currentDropCue);
            DrawPotsAndPlants(layout, drawContext, currentDropTarget, currentDropCue);
            DrawProjectiles();
            DrawZombies();
            DrawCombatEffects();
            DrawBattlefieldFlash(layout);
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
            BattlefieldTerrainGuiRenderer.DrawValidated(map, Projection, palette);
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
            var range = EffectiveAttackRange(_game, plant);
            if (pot == null || range <= .0001f) return;
            var rangeRect = Projection.MapRect(_game.PotPoint(pot), range * 2f, range * 2f);
            rangeRect.position -= layout.Board.position;
            GUI.BeginGroup(layout.Board);
            GUI.DrawTexture(rangeRect, _attackRangeTexture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }

        public static float EffectiveAttackRange(GameSimulation simulation, Plant plant)
        {
            if (simulation == null || plant == null || simulation.Content == null)
                return 0f;
            PlantDefinitionDto definition;
            StarTierDefinitionDto starTier;
            if (!simulation.Content.Plants.TryGetValue(
                    plant.DefinitionId ?? string.Empty, out definition)
                || !simulation.Content.StarTiers.TryGetValue(
                    "star." + Mathf.Clamp(plant.Star, 1, 4), out starTier))
                return 0f;
            var baseRange = simulation.Map.FromLegacyDistance(
                definition.rangeLegacyUnits) * starTier.rangeMultiplier;
            return simulation.GetEffectiveAttribute(
                plant, CombatAttributeKind.Range, baseRange);
        }

        private static float EffectivePlantDamage(GameSimulation simulation,
            Plant plant, PlantDefinitionDto definition)
        {
            if (simulation == null || plant == null || definition == null)
                return 0f;
            StarTierDefinitionDto starTier;
            if (!simulation.Content.StarTiers.TryGetValue(
                    "star." + Mathf.Clamp(plant.Star, 1, 4), out starTier))
                return 0f;
            return simulation.GetEffectiveAttribute(plant,
                CombatAttributeKind.Damage,
                definition.damage * starTier.damageMultiplier);
        }

        private static string PlantDisplayName(
            GameSimulation simulation, Plant plant)
        {
            if (simulation == null || simulation.Content == null || plant == null)
                return "未知水果";
            PlantDefinitionDto definition;
            return simulation.Content.Plants.TryGetValue(
                    plant.DefinitionId ?? string.Empty, out definition)
                && !string.IsNullOrEmpty(definition.displayName)
                ? definition.displayName
                : "未知水果";
        }

        private string EquipmentDisplayName(string equipmentId)
        {
            if (string.IsNullOrEmpty(equipmentId)) return "无";
            EquipmentDefinitionDto definition;
            return _game != null && _game.Content != null
                && _game.Content.Equipment.TryGetValue(equipmentId, out definition)
                && !string.IsNullOrEmpty(definition.displayName)
                ? definition.displayName
                : "未知装备";
        }

        private void DrawPotsAndPlants(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            foreach (var pot in _game.State.Pots.Where(value => value.Active))
            {
                var hitRect = PotHitRect(pot, layout);
                var rect = OffsetBattlefieldVisual(PotVisualRect(pot, layout));
                var plant = _game.PlantAtPot(pot.Id);
                var selected = plant != null && plant.Id == _inspectedPlantId;
                var target = _drag != null && _drag.Active
                    && _drag.Type == DragPayloadType.Equipment && plant != null
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
            SetGuidanceStatus(DestinationDragGuidance(_game, inspected, false));
        }

        private void HandlePlantClick(Plant plant)
        {
            if (!string.IsNullOrEmpty(_selectedEquipmentId))
            {
                var success = _game.InstallEquipment(
                    plant.Id, _selectedEquipmentId, out var reason);
                _selectedEquipmentId = string.Empty;
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
            _selectedEquipmentId = string.Empty;
            _potToolSelected = false;
            var verb = plant.PotId >= 0 ? "拖动可移动或合成" : "拖动到花盆种植";
            SetStatus(true, "正在查看" + PlantDisplayName(_game, plant) + "；" + verb);
        }

        private static int InspectionPlantId(Plant plant)
        {
            return plant == null ? -1 : plant.Id;
        }

        private static string DestinationDragGuidance(
            GameSimulation simulation, Plant inspected, bool nursery)
        {
            if (nursery) return inspected == null
                ? "拖动场上水果到这里"
                : "将选中水果拖到这里";
            return inspected == null
                ? "拖动苗圃水果到这里"
                : "将" + PlantDisplayName(simulation, inspected) + "拖到这里";
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
                var visualRect = OffsetBattlefieldVisual(
                    layout.Battlefield.PotVisualRect(cell));
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
            var interpolation = _game.PresentationInterpolationFraction;
            foreach (var zombie in _game.State.Zombies)
            {
                var pathProgress = _renderSamples.EnemyPathProgress(
                    zombie.Id, zombie.PathProgress, interpolation);
                var reaction = _presentation.ReactionFor(zombie.Id);
                var point = ToBoard(_game.Map.SampleRoute(zombie.RouteId, pathProgress))
                    + reaction.Offset;
                var size = Projection.LegacyVisualSize(96f);
                var rect = new Rect(point.x - size * .5f, point.y - size * .5f, size, size);
                rect = ScaleAroundCenter(rect, reaction.Scale.x, reaction.Scale.y);
                var frozen = _game.HasStatus(
                    zombie.Id, BattleContentIds.Statuses.IceFreeze);
                var slowed = _game.HasStatus(
                    zombie.Id, BattleContentIds.Statuses.IceSlow);
                if (frozen) DrawVfxSprite(Grow(rect, 4f), CombatSprite.FrozenAura, new Color(1f, 1f, 1f, .82f));
                var tint = slowed ? new Color(.72f, .9f, 1f) : Color.white;
                tint = Color.Lerp(tint, new Color(1f, .9f, .55f), reaction.Flash);
                DrawTempSprite(rect, ZombieSprite(zombie.DefinitionId), tint);
                if (_game.HasStatus(
                        zombie.Id, BattleContentIds.Statuses.ChiliBurn))
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
            var interpolation = _game.PresentationInterpolationFraction;
            foreach (var projectile in _game.State.Projectiles)
            {
                var position = _renderSamples.ProjectilePosition(
                    projectile.Id, projectile.Position, interpolation);
                var point = ToBoard(position);
                var archetype = BattlePresentationVisualCatalog.Projectile(
                    projectile.ProjectileId);
                if (archetype == ProjectileVisualArchetype.Pea
                    || archetype == ProjectileVisualArchetype.Generic)
                {
                    DrawVfxSprite(CenteredRect(point, 26f), CombatSprite.PeaProjectile);
                }
                else if (archetype == ProjectileVisualArchetype.Watermelon)
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
                    case PresentationVfxKind.PeaImpact:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(20f, 39f, progress)), CombatSprite.PeaImpact, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.WatermelonBlast:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(48f, 102f, progress)), CombatSprite.WatermelonBlast, new Color(1f, 1f, 1f, fade));
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(35f, 125f, progress)), CombatSprite.ShockwaveRing, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.BananaHit:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(18f, 43f, progress)),
                            CombatSprite.HitSpark, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.DurianImpact:
                        DrawVfxSprite(CenteredRect(point + Vector2.up * Projection.LegacyVisualSize(13f),
                                Mathf.Lerp(52f, 128f, progress)),
                            CombatSprite.DurianShockwave, new Color(1f, 1f, 1f, fade));
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(40f, 138f, progress)),
                            CombatSprite.ShockwaveRing, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.SunBurst:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(38f, 78f, Mathf.Sin(progress * Mathf.PI))), CombatSprite.SunBurst,
                            new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.GatlingMuzzle:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(34f, 20f, progress)), CombatSprite.GatlingMuzzle, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.IceImpact:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(30f, 58f, progress)), CombatSprite.IceImpact, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.FreezeProc:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(42f, 78f, progress)),
                            CombatSprite.FrozenAura, new Color(1f, 1f, 1f, fade));
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(25f, 70f, progress)),
                            CombatSprite.ShockwaveRing, new Color(.7f, .9f, 1f, fade));
                        break;
                    case PresentationVfxKind.ChiliImpact:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(34f, 62f, progress)), CombatSprite.ChiliImpact, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.BurnTick:
                        DrawVfxSprite(CenteredRect(point + Vector2.up * 5f,
                                Mathf.Lerp(18f, 30f, progress)),
                            CombatSprite.Burning, new Color(1f, 1f, 1f, fade));
                        break;
                    case PresentationVfxKind.Defeat:
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(36f, 82f, progress)),
                            CombatSprite.HitSpark, new Color(1f, .88f, .45f, fade));
                        DrawVfxSprite(CenteredRect(point, Mathf.Lerp(24f, 94f, progress)),
                            CombatSprite.ShockwaveRing, new Color(1f, .82f, .35f, fade));
                        break;
                    default:
                        break;
                }
            }
        }

        private void DrawBattlefieldFlash(BattleUiLayout layout)
        {
            var flash = _presentation.BattlefieldFlash;
            if (flash <= .001f) return;
            DrawWorldRect(layout.Battlefield.GridRect,
                new Color(1f, .94f, .72f, flash * .22f));
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
                    _statusState, currentDropCue)
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
                    viewState.WaveActionLabel,
                    BattleUiPresentationState.ResolveActionSpec(
                        BattleUiActionSemantic.StartWave),
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

    }
}
