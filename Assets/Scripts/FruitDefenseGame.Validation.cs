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
                Id = 8101, DefinitionId = BattleContentIds.Plants.Pea,
                Star = 1, PotId = firstPot.Id, NurseryIndex = -1,
            };
            var second = new Plant
            {
                Id = 8102, DefinitionId = BattleContentIds.Plants.Pea,
                Star = 1, PotId = secondPot.Id, NurseryIndex = -1,
            };
            var nursery = new Plant
            {
                Id = 8103, DefinitionId = BattleContentIds.Plants.Watermelon,
                Star = 1, PotId = -1, NurseryIndex = 0,
            };
            var support = new Plant
            {
                Id = 8104, DefinitionId = BattleContentIds.Plants.Sunflower,
                Star = 1, PotId = firstPot.Id, NurseryIndex = -1,
            };
            simulation.State.Plants.Add(first);
            simulation.State.Plants.Add(second);
            simulation.State.Plants.Add(nursery);

            var inspected = InspectionPlantId(first);
            var firstPosition = first.PotId;
            var secondPosition = second.PotId;
            var plantCount = simulation.State.Plants.Count;
            var guidance = DestinationDragGuidance(simulation, first, false);
            inspected = InspectionPlantId(second);
            if (inspected != second.Id || first.PotId != firstPosition || second.PotId != secondPosition
                || first.Star != 1 || second.Star != 1 || simulation.State.Plants.Count != plantCount
                || string.IsNullOrEmpty(guidance))
            {
                reason = "passive plant and destination clicks changed the formation";
                return false;
            }

            inspected = InspectionPlantId(nursery);
            if (inspected != nursery.Id || nursery.PotId >= 0
                || EffectiveAttackRange(simulation, support) > .0001f)
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
                || ShouldShowMergeHint(DragPayloadType.Equipment, mergeHint)
                || !ShouldShowMergeHint(DragPayloadType.Plant, mergeHint))
            {
                reason = "floating drag hint is not limited to legal plant merges";
                return false;
            }

            var layout = new BattleUiLayout(simulation.Map, simulation.NurserySlotCount);
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
            var range = EffectiveAttackRange(simulation, first);
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

            ContentValidationResult validation;
            CompiledGameContentBundle bundle;
            if (!BundledGameContentLoader.TryLoadBundle(out bundle, out validation))
            {
                reason = "bundled game content could not be loaded: " + string.Join(" | ",
                    validation.Issues.Select(value => value.ToString()).ToArray());
                return false;
            }
            var content = bundle.Battle;

            foreach (var definitionId in BattlePresentationVisualCatalog.BundledPlantDefinitionIds)
            {
                var plant = new Plant { DefinitionId = definitionId };
                if (ResolvePlantSprite(content, plant) == ResolvePlantSprite(content, definitionId)) continue;
                reason = "unequipped plant does not resolve its base resource: "
                    + definitionId;
                return false;
            }

            var evolutionSprites = new HashSet<TempSprite>();
            foreach (var equipmentId in BattlePresentationVisualCatalog.BundledEquipmentDefinitionIds)
            {
                var plant = new Plant
                {
                    DefinitionId = BattleContentIds.Plants.Pea,
                    EquipmentId = equipmentId,
                };
                var resolved = ResolvePlantSprite(content, plant);
                if (resolved != ResolveEquipmentSprite(content, equipmentId)
                    || !evolutionSprites.Add(resolved))
                {
                    reason = "equipment evolution resource mapping is missing or duplicated: "
                        + equipmentId;
                    return false;
                }
            }

            var customPlant = new Plant
            {
                DefinitionId = BattleContentIds.Plants.Watermelon,
                EquipmentId = "equipment.custom",
            };
            if (ResolvePlantSprite(content, customPlant) != TempSprite.Watermelon
                || ResolvePlantSprite(content, "plant.custom") != TempSprite.Pea
                || ResolveEnemySprite(content, "enemy.custom") != TempSprite.Zombie
                || BattlePresentationVisualCatalog.Projectile("projectile.custom")
                    != ProjectileVisualArchetype.Generic)
            {
                reason = "generic stable-ID visual policy is not explicit";
                return false;
            }

            reason = "ok";
            return true;
        }

        public static bool ValidateCombatFeedbackCatalog(
            CombatFeedbackCatalog catalog, CompiledBattleContentCatalog content,
            out string reason)
        {
            if (catalog == null)
            {
                reason = "presentation catalog is unavailable";
                return false;
            }
            if (content == null)
            {
                reason = "compiled battle content is unavailable";
                return false;
            }

            var issues = catalog.ValidateCoverage(content);
            if (issues.Count > 0)
            {
                reason = issues[0];
                return false;
            }

            reason = "ok";
            return true;
        }

        public static bool ValidatePortraitLayout(out string reason)
        {
            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            var design = layout.Design;
            if (!ContainsRect(design, layout.Header)
                || !ContainsRect(design, layout.PageShell)
                || layout.Header.Overlaps(layout.PageShell)
                || !Mathf.Approximately(layout.Header.xMin, layout.PageShell.xMin)
                || !Mathf.Approximately(layout.Header.xMax, layout.PageShell.xMax)
                || !Mathf.Approximately(layout.PageShell.yMin - layout.Header.yMax, 4f))
            {
                reason = "raised Header and PageShell peer-frame authority failed";
                return false;
            }
            var pageRegions = new[]
            {
                layout.Header, layout.BattleStage, layout.PhaseWaveRow, layout.ContextTray,
                layout.NurseryTray, layout.RefreshAction,
            };
            foreach (var region in pageRegions)
            {
                if (!ContainsRect(design, region))
                {
                    reason = "region outside design bounds: " + region;
                    return false;
                }
            }

            for (var index = 1; index < pageRegions.Length; index++)
            {
                if (pageRegions[index - 1].yMax > pageRegions[index].yMin)
                {
                    reason = "battle page regions overlap vertically";
                    return false;
                }
            }

            if (layout.BattleStage != layout.Board
                || !ContainsRect(layout.PageShell, layout.BattleStage)
                || !ContainsRect(layout.PageShell, layout.PhaseWaveRow)
                || !ContainsRect(layout.PageShell, layout.ContextTray)
                || !ContainsRect(layout.PageShell, layout.NurseryTray)
                || !ContainsRect(layout.PageShell, layout.RefreshAction)
                || !Mathf.Approximately(layout.BattleStage.xMin - layout.PageShell.xMin, 8f)
                || !Mathf.Approximately(layout.PageShell.xMax - layout.BattleStage.xMax, 8f)
                || !ContainsRect(layout.ContextTray, layout.DetailTitle)
                || !ContainsRect(layout.ContextTray, layout.DetailBody)
                || !ContainsRect(layout.ContextTray, layout.DetailCloseAction))
            {
                reason = "gameplay stage or mutually exclusive context anatomy is invalid";
                return false;
            }

            var stageHeightFraction = layout.BattleStage.height
                / BattleUiLayout.DesignHeight;
            if (stageHeightFraction < .38f || stageHeightFraction > .43f)
            {
                reason = "battlefield is outside the 38-to-43-percent height band";
                return false;
            }

            var projection = layout.Battlefield;
            if (!projection.ValidatePlantingGeometry(out reason)) return false;
            if (projection.MapViewportRect != projection.BoardRect)
            {
                reason = "battlefield map viewport is not the complete gameplay stage";
                return false;
            }
            if (!Mathf.Approximately(projection.TileSize, 44.25f))
            {
                reason = "battlefield grid no longer preserves the audited 44.25-point tile";
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
            if (!ContainsRect(nurseryCell, nurseryIcon)
                || nurseryIcon.width / nurseryCell.width < .9f
                || nurseryIcon.height / nurseryCell.height < .9f)
            {
                reason = "frameless tray icon does not nearly fill its logical cell";
                return false;
            }
            for (var toolIndex = 0; toolIndex < BattleUiLayout.ToolCount; toolIndex++)
            {
                var tool = layout.Tool(toolIndex);
                var sourceIcon = BattleUiLayout.ToolRecipeSourceIcon(tool);
                var operatorGlyph = BattleUiLayout.ToolRecipeOperator(tool);
                var targetIcon = BattleUiLayout.ToolRecipeTargetIcon(tool);
                var inventoryBadge = BattleUiLayout.ToolInventoryBadge(tool);
                if (!ContainsRect(tool, sourceIcon)
                    || !ContainsRect(tool, operatorGlyph)
                    || !ContainsRect(tool, targetIcon)
                    || !ContainsRect(tool, inventoryBadge)
                    || sourceIcon.Overlaps(operatorGlyph)
                    || sourceIcon.Overlaps(targetIcon)
                    || operatorGlyph.Overlaps(targetIcon))
                {
                    reason = "recipe-card source/operator/target anatomy failed: " + toolIndex;
                    return false;
                }
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
                layout.EquipmentTool(BattleContentIds.Equipment.Gatling),
                layout.EquipmentTool(BattleContentIds.Equipment.Ice),
                layout.EquipmentTool(BattleContentIds.Equipment.Chili), layout.PotTool,
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
            if (!ContainsRect(layout.PhaseWaveRow, layout.PhaseStatus)
                || !ContainsRect(layout.PhaseWaveRow,
                    layout.PhaseStatusWithWaveAction)
                || !ContainsRect(layout.PhaseWaveRow, layout.WaveAction)
                || layout.PhaseStatusWithWaveAction.Overlaps(layout.WaveAction)
                || Mathf.Min(layout.WaveAction.width, layout.WaveAction.height) < 44f
                || layout.PhaseWaveRow.Overlaps(layout.BattleStage))
            {
                reason = "independent phase/Wave row geometry failed";
                return false;
            }

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
                reason = "phase-specific independent-row Wave action contract failed";
                return false;
            }
            if (!pausedUi.BlocksBackgroundInput
                || !victoryUi.BlocksBackgroundInput
                || !defeatUi.BlocksBackgroundInput
                || readyUi.BlocksBackgroundInput
                || playingUi.BlocksBackgroundInput
                || betweenUi.BlocksBackgroundInput)
            {
                reason = "blocking modal background-input contract failed";
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
            restartSimulation.State.Zombies.Add(new Zombie
            {
                Id = 991,
                Hp = 1f,
                MaxHp = 1f,
                RouteId = restartSimulation.Map.PrimaryRouteId,
            });
            restartSimulation.State.Projectiles.Add(new ProjectileFlash { Id = 992 });
            restartSimulation.RefreshNursery(out _);
            var presentation = new RestartPresentationState
            {
                InspectedPlantId = 88,
                SelectedEquipmentId = BattleContentIds.Equipment.Ice,
                PotToolSelected = true,
                Status = "stale",
                StatusState = RuntimeUiInteractionState.Error,
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

    }
}
