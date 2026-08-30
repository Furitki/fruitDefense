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
        private void DrawEmbeddedBattleControls(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            if (_game.PlantById(_inspectedPlantId) == null)
            {
                RuntimeUiGui.DrawStandardPanel(drawContext, layout.ContextTray);
                var toolCopy = RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.BattleContextTray);
                RuntimeUiGui.DrawSingleLineText(drawContext,
                    layout.ContextTrayTitle, toolCopy.Text,
                    toolCopy.Role, toolCopy.Tone, toolCopy.Alignment);
                DrawTools(layout, drawContext);
            }
            else
            {
                DrawSelectedPlant(layout, drawContext);
            }
            RuntimeUiGui.DrawStandardPanel(drawContext, layout.NurseryTray);
            var nurseryCopy = RuntimeUiCopyCatalog.Get(
                RuntimeUiCopyId.BattleNurseryTray);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.NurseryTrayTitle,
                nurseryCopy.Text, nurseryCopy.Role, nurseryCopy.Tone,
                nurseryCopy.Alignment);
            DrawNursery(layout, drawContext, currentDropTarget, currentDropCue);
        }

        private void DrawTools(BattleUiLayout layout, RuntimeUiDrawContext drawContext)
        {
            DrawEquipmentToolButton(
                layout.EquipmentTool(BattleContentIds.Equipment.Gatling),
                BattleContentIds.Equipment.Gatling,
                _game.State.Inventory.Get(BattleContentIds.Equipment.Gatling),
                layout, drawContext);
            DrawEquipmentToolButton(
                layout.EquipmentTool(BattleContentIds.Equipment.Ice),
                BattleContentIds.Equipment.Ice,
                _game.State.Inventory.Get(BattleContentIds.Equipment.Ice),
                layout, drawContext);
            DrawEquipmentToolButton(
                layout.EquipmentTool(BattleContentIds.Equipment.Chili),
                BattleContentIds.Equipment.Chili,
                _game.State.Inventory.Get(BattleContentIds.Equipment.Chili),
                layout, drawContext);
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
            var potVisualRect = RuntimeUiGui.DrawSlot(drawContext, potRect,
                RuntimeUiSlotKind.Tool, state, selectionEmphasized,
                motion: potMotion);
            RuntimeUiGui.DrawIcon(drawContext,
                TransformChild(BattleUiLayout.ToolRecipeSourceIcon(potRect),
                    potRect, potVisualRect),
                RuntimeUiArtSlot.IconToolPot, state);
            RuntimeUiGui.DrawSingleLineText(drawContext,
                TransformChild(BattleUiLayout.ToolRecipeOperator(potRect),
                    potRect, potVisualRect),
                "×",
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);
            RuntimeUiGui.DrawIndicator(drawContext,
                TransformChild(BattleUiLayout.ToolRecipeTargetIcon(potRect),
                    potRect, potVisualRect), RuntimeUiIndicatorKind.DragLegal);
            if (_game.State.Inventory.Pots > 0)
            {
                RuntimeUiGui.DrawSingleLineText(drawContext,
                    TransformChild(BattleUiLayout.ToolInventoryBadge(potRect),
                        potRect, potVisualRect),
                    _game.State.Inventory.Pots.ToString(),
                    RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                    TextAnchor.MiddleCenter, state);
            }
            if (DrawSharedHitTarget(drawContext, potRect, state) && available)
                TogglePotTool();
        }

        private void DrawEquipmentToolButton(
            Rect rect, string equipmentId, int count, BattleUiLayout layout,
            RuntimeUiDrawContext drawContext)
        {
            var selected = string.Equals(_selectedEquipmentId,
                equipmentId, StringComparison.Ordinal);
            var dragging = _drag != null && _drag.Active
                && _drag.Type == DragPayloadType.Equipment
                && string.Equals(_drag.EquipmentId, equipmentId,
                    StringComparison.Ordinal);
            var state = BattleUiPresentationState.ResolveSlotState(count > 0,
                selected || dragging, ContainsPointer(rect), IsPointerPress(rect));
            var selectionEmphasized = selected && _selectionPulseTarget
                    == BattlePresentationVisualCatalog.EquipmentToolIndex(
                        equipmentId) + 1
                && _selectionPulse.IsActive(Time.unscaledTime);
            var motion = selectionEmphasized
                ? RuntimeUiMotion.Evaluate(_selectionPulse, Time.unscaledTime,
                    _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop)
                : RuntimeUiMotionSample.Rest;
            var visualRect = RuntimeUiGui.DrawSlot(drawContext, rect,
                RuntimeUiSlotKind.Tool, state, selectionEmphasized,
                motion: motion);
            DrawStatefulTempSprite(
                drawContext, BattleUiLayout.ToolRecipeSourceIcon(visualRect),
                EquipmentSprite(equipmentId), state);
            RuntimeUiGui.DrawSingleLineText(drawContext,
                BattleUiLayout.ToolRecipeOperator(visualRect), "×",
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);
            RuntimeUiGui.DrawIcon(drawContext,
                BattleUiLayout.ToolRecipeTargetIcon(visualRect),
                RuntimeUiArtSlot.IconToolPot, state);
            if (count > 0)
            {
                RuntimeUiGui.DrawSingleLineText(drawContext,
                    BattleUiLayout.ToolInventoryBadge(visualRect), count.ToString(),
                    RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                    TextAnchor.MiddleCenter, state);
            }
            if (!DrawSharedHitTarget(drawContext, rect, state) || count <= 0) return;
            ToggleEquipmentSelection(equipmentId);
        }

        private void DrawNursery(BattleUiLayout layout,
            RuntimeUiDrawContext drawContext, DropTarget currentDropTarget,
            BattleUiDropCue currentDropCue)
        {
            for (var slot = 0; slot < layout.NurserySlotCount; slot++)
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
                    var emptySelectionMotion = NurserySelectionMotion(slot);
                    var visualRect = RuntimeUiGui.DrawSlot(drawContext, rect,
                        RuntimeUiSlotKind.Nursery, state, motion:
                        RuntimeUiMotionSample.Combine(
                            rewardMotion, emptySelectionMotion));
                    if (showingPotReward)
                    {
                        RuntimeUiGui.DrawIcon(drawContext,
                            BattleUiLayout.FramelessSlotIcon(visualRect),
                            RuntimeUiArtSlot.IconToolPot, state);
                        var storedOwner = BattleUiLayout.NurserySlotLabel(visualRect);
                        var storedCopy = RuntimeUiCopyCatalog.Get(
                            RuntimeUiCopyId.BattleNurseryPotStored).Text;
                        var storedLayout = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                            drawContext, storedOwner, RuntimeUiTypographyRole.Supplemental,
                            TextAnchor.MiddleCenter, state);
                        RuntimeUiGui.DrawControlledTwoLineText(drawContext, storedOwner,
                            RuntimeUiGui.ResolveStatusTextLines(storedLayout, storedCopy),
                            RuntimeUiTypographyRole.Supplemental,
                            RuntimeUiTextTone.State, TextAnchor.MiddleCenter, state);
                    }
                    else
                        RuntimeUiGui.DrawSingleLineText(drawContext, visualRect,
                            RuntimeUiCopyCatalog.Get(
                                RuntimeUiCopyId.BattleNurseryEmpty).Text,
                            RuntimeUiTypographyRole.Supplemental,
                            RuntimeUiTextTone.Secondary, TextAnchor.MiddleCenter, state);
                    DrawDropCue(drawContext, rect, cue);
                    if (DrawSharedHitTarget(drawContext, rect, state))
                    {
                        BeginNurserySelectionPulse(slot);
                        SetGuidanceStatus(DestinationDragGuidance(
                            _game, _game.PlantById(_inspectedPlantId), true));
                    }
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
                var occupiedSelectionMotion = NurserySelectionMotion(slot);
                var occupiedVisualRect = RuntimeUiGui.DrawSlot(drawContext, rect,
                    RuntimeUiSlotKind.Nursery, occupiedState, motion:
                    RuntimeUiMotionSample.Combine(
                        returnMotion, occupiedSelectionMotion));
                DrawTempSprite(BattleUiLayout.FramelessSlotIcon(occupiedVisualRect), PlantSprite(plant));
                var starOwner = BattleUiLayout.NurserySlotLabel(occupiedVisualRect);
                var starCopy = new string('★', plant.Star);
                if (plant.Star >= 4)
                {
                    var starLayout = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                        drawContext, starOwner, RuntimeUiTypographyRole.Supplemental,
                        TextAnchor.MiddleCenter, occupiedState);
                    RuntimeUiGui.DrawControlledTwoLineText(drawContext, starOwner,
                        RuntimeUiGui.ResolveStatusTextLines(starLayout, starCopy),
                        RuntimeUiTypographyRole.Supplemental,
                        RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, occupiedState);
                }
                else
                {
                    RuntimeUiGui.DrawSingleLineText(drawContext, starOwner, starCopy,
                        RuntimeUiTypographyRole.Supplemental,
                        RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, occupiedState);
                }
                DrawDropCue(drawContext, rect, cue);
                if (DrawSharedHitTarget(drawContext, rect, occupiedState))
                {
                    BeginNurserySelectionPulse(slot);
                    HandlePlantClick(plant);
                }
            }
            var cost = _game.RefreshCost(_game.State.RefreshCount);
            var refreshPress = TrackBattleAction(
                RefreshActionFeedbackTarget, layout.RefreshAction);
            var refreshState = BattleUiPresentationState.ResolveActionState(
                false, refreshPress.Hovered, refreshPress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, layout.RefreshAction,
                RuntimeUiCopyCatalog.FormatRefreshAction(cost),
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.NurseryRefresh), refreshState,
                RuntimeUiArtSlot.IconControlRefresh,
                motion: BattleActionMotion(RefreshActionFeedbackTarget));
            if (refreshPress.Activated)
            {
                BeginBattleActionPress(RefreshActionFeedbackTarget);
                RefreshNurseryFromUi();
            }
        }

        private RuntimeUiMotionSample NurserySelectionMotion(int slot)
        {
            if (_selectionPulseTarget != NurserySelectionFeedbackTarget(slot))
                return RuntimeUiMotionSample.Rest;
            return RuntimeUiMotion.Evaluate(_selectionPulse, Time.unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Pop);
        }

        private void BeginNurserySelectionPulse(int slot)
        {
            _selectionPulseTarget = NurserySelectionFeedbackTarget(slot);
            _selectionPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledSelectionSeconds);
        }

        private int NurserySelectionFeedbackTarget(int slot)
        {
            if (slot < 0 || slot >= _game.NurserySlotCount)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return 100 + slot;
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
            RuntimeUiGui.DrawDetailCard(drawContext, layout.ContextTray, detailState);
            PlantDefinitionDto stats;
            _game.Content.Plants.TryGetValue(
                plant.DefinitionId ?? string.Empty, out stats);
            var displayName = stats == null
                ? "未知水果"
                : stats.displayName;
            var maximumTier = _game.Content.PlantMaximumTier(plant.DefinitionId);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.DetailTitle,
                displayName + " · " + plant.Star + "/" + maximumTier + " 星",
                RuntimeUiTypographyRole.ControlLabel, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, detailState);
            var effectiveRange = EffectiveAttackRange(_game, plant);
            var rangeText = effectiveRange > .0001f
                ? Mathf.RoundToInt(_game.Map.ToLegacyDistance(effectiveRange)).ToString()
                : "无攻击范围";
            var damage = EffectivePlantDamage(_game, plant, stats);
            var attackInterval = _game.EffectivePlantAttackIntervalSeconds(plant);
            RuntimeUiGui.DrawSingleLineText(drawContext, layout.DetailBody,
                "伤害 " + Mathf.RoundToInt(damage)
                + " · 范围 " + rangeText
                + (attackInterval > .0001f
                    ? " · 间隔 " + attackInterval.ToString("0.##") + "s"
                    : string.Empty),
                RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Secondary,
                TextAnchor.MiddleLeft, detailState);
            var closePress = TrackBattleAction(
                DetailCloseFeedbackTarget, layout.DetailCloseAction);
            var closeState = BattleUiPresentationState.ResolveActionState(
                false, closePress.Hovered, closePress.Pressed);
            RuntimeUiGui.DrawCompactControlVisual(drawContext,
                layout.DetailCloseAction,
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.Close), closeState,
                RuntimeUiCompactControlVisualSample.Inactive,
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
            if (_drag.Type != DragPayloadType.Plant
                && currentTarget.Type != DropTargetType.None)
                rect.center = Vector2.Lerp(rect.center, currentTarget.Rect.center, .42f);
            rect = layout.ClampDragPreview(rect);

            var plantDropStatus = _drag.Type == DragPayloadType.Plant
                ? PlantDragTargetStatus(_drag, currentTarget)
                : default(PlantDropStatus);
            if (_drag.Type == DragPayloadType.Plant)
            {
                var snapsToTarget = plantDropStatus.Legal
                    && BattleUiPresentationState.SnapsPlantDragFeedback(
                        currentDropCue);
                var connectorDestination = snapsToTarget
                    ? currentTarget.Rect
                    : rect;
                var connectorState = currentTarget.Type == DropTargetType.None
                    ? RuntimeUiInteractionState.Selected
                    : BattleUiPresentationState.DropInteractionState(
                        currentDropCue);
                RuntimeUiGui.DrawDragConnector(drawContext,
                    DragGeometry.ResolveConnector(
                        _drag.SourceRect, connectorDestination),
                    connectorState);
            }

            var clipsToBattleStage = currentTarget.Type == DropTargetType.Pot
                || currentTarget.Type == DropTargetType.Plant
                || currentTarget.Type == DropTargetType.Expansion;
            var stageMaskRect = clipsToBattleStage
                ? RuntimeUiGui.GameplayStageMaskRect(
                    drawContext, layout.BattleStage)
                : default;
            if (clipsToBattleStage)
                BeginAbsoluteDesignClip(layout, stageMaskRect);
            try
            {
                if (_drag.Type == DragPayloadType.Plant
                    && currentTarget.Type != DropTargetType.None)
                {
                    RuntimeUiGui.DrawDragTargetFrame(drawContext,
                        currentTarget.Rect,
                        BattleUiPresentationState.DropInteractionState(
                            currentDropCue), stageMaskRect);
                    DrawDropCue(drawContext,
                        currentTarget.Rect, currentDropCue);
                }

                if (_drag.Type == DragPayloadType.Plant)
                {
                    var plant = _game.PlantById(_drag.PlantId);
                    if (plant != null) DrawTempSprite(rect, PlantSprite(plant));
                }
                else if (_drag.Type == DragPayloadType.Equipment)
                    DrawTempSprite(rect, EquipmentSprite(_drag.EquipmentId));
                else
                    DrawTempSprite(rect, TempSprite.EmptyPot);
                DrawDropCue(drawContext, rect, currentDropCue);
            }
            finally
            {
                if (clipsToBattleStage) EndAbsoluteDesignClip();
            }

            if (!ShouldShowMergeHint(_drag.Type, plantDropStatus)) return;

            var labelWidth = drawContext.Styles.Text(
                RuntimeUiTypographyRole.Supplemental,
                TextAnchor.MiddleCenter).CalcSize(
                    new GUIContent(plantDropStatus.Reason)).x;
            var hintRect = layout.MergeHint(rect, labelWidth);
            RuntimeUiGui.DrawStandardPanel(
                drawContext, hintRect, RuntimeUiInteractionState.Warning);
            RuntimeUiGui.DrawSingleLineText(drawContext,
                BattleUiLayout.CueLabel(hintRect),
                plantDropStatus.Reason, RuntimeUiTypographyRole.Supplemental,
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
            if (!content.UsesResultCard)
            {
                RuntimeUiGui.DrawInlineIconLabel(drawContext, layout.ModalPauseHint,
                    RuntimeUiArtSlot.IndicatorWarning,
                    content.MessageLines.FirstLine, RuntimeUiTypographyRole.Body,
                    RuntimeUiTextTone.Primary, content.SurfaceState);
            }
            else
            {
                if (content.MessageLines.HasSecondLine)
                {
                    RuntimeUiGui.DrawControlledTwoLineText(drawContext,
                        layout.ModalTerminalMessage, content.MessageLines,
                        RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Primary,
                        TextAnchor.MiddleCenter, content.SurfaceState);
                }
                else
                {
                    RuntimeUiGui.DrawSingleLineText(drawContext,
                        layout.ModalTerminalMessage,
                        content.MessageLines.FirstLine, RuntimeUiTypographyRole.Body,
                        RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter,
                        content.SurfaceState);
                }
            }
            if (content.UsesResultCard)
            {
                RuntimeUiGui.DrawIndicator(drawContext, layout.ModalResultIndicator,
                    content.SurfaceState == RuntimeUiInteractionState.Success
                        ? RuntimeUiIndicatorKind.Success
                        : RuntimeUiIndicatorKind.Error);
            }
            var actionCount = content.ActionCount;
            var primaryRect = layout.ModalAction(0, actionCount);
            var primaryPress = TrackBattleAction(
                ModalPrimaryFeedbackTarget, primaryRect);
            var primaryState = BattleUiPresentationState.ResolveActionState(
                false, primaryPress.Hovered, primaryPress.Pressed);
            RuntimeUiGui.DrawActionVisual(drawContext, primaryRect,
                content.PrimaryAction,
                new RuntimeUiActionSpec(content.PrimaryActionKind,
                    RuntimeUiActionContentForm.IconLabel,
                    RuntimeUiActionBehavior.Instantaneous),
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
                content.SecondaryAction,
                new RuntimeUiActionSpec(content.SecondaryActionKind,
                    RuntimeUiActionContentForm.IconLabel,
                    RuntimeUiActionBehavior.Instantaneous),
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
            RebindCompactControlPresentation();
        }

        private void RebindCompactControlPresentation()
        {
            if (_game == null)
            {
                _pauseCompactControlState = default;
                _speedCompactControlState = default;
                return;
            }

            var unscaledTime = Time.unscaledTime;
            _pauseCompactControlState = RuntimeUiCompactControlLifecycle.Rebind(
                _game.State.Paused, unscaledTime);
            _speedCompactControlState = RuntimeUiCompactControlLifecycle.Rebind(
                _game.State.Speed != 1, unscaledTime);
        }

        private RestartPresentationState CaptureRestartPresentation()
        {
            return new RestartPresentationState
            {
                InspectedPlantId = _inspectedPlantId,
                SelectedEquipmentId = _selectedEquipmentId,
                PotToolSelected = _potToolSelected,
                Status = _status,
                StatusState = _statusState,
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
            _selectedEquipmentId = presentation.SelectedEquipmentId;
            _potToolSelected = presentation.PotToolSelected;
            _status = presentation.Status;
            _statusState = presentation.StatusState;
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
            presentation.SelectedEquipmentId = string.Empty;
            presentation.PotToolSelected = false;
            presentation.Status = DefaultStatus;
            presentation.StatusState = RuntimeUiInteractionState.Normal;
            presentation.StatusPulse = default;
            presentation.Drag = null;
            presentation.DragControlId = 0;
            presentation.ReturnPulsePlantId = -1;
            presentation.ReturnPulse = default;
            presentation.NurseryRollDisplayPulse = default;
            presentation.SelectionPulseTarget = 0;
            presentation.SelectionPulse = default;
        }

    }
}
