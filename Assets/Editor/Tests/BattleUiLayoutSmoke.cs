using System;
using System.IO;
using FruitDefense.Core;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleUiLayoutSmoke
    {
        public static void Run()
        {
            var layout = new BattleUiLayout(GameConfig.DefaultBattlefield);
            ValidateReferenceGeometry(layout);
            ValidateNamedTracksAndRhythm(layout);
            ValidateViewportBackgroundContract();
            ValidateProjectedViewportMatrix(layout);
            ValidateInteractionBoundaries(layout);
            ValidateDragFeedbackGeometry(layout);
            ValidateFinitePresentationState();
            ValidatePhaseWaveText(layout);
            ValidateTransientStatusText(layout);
            ValidateSharedChromeSource();
            ValidateSharedControlSource();
            ValidateBattlefieldContainmentSource(layout);
            ValidateSharedDetailAndOverlaySource();
            ValidateLegacyBattleUiRemoval();
            Debug.Log("FRUIT_DEFENSE_BATTLE_UI_LAYOUT_OK");
        }

        public static void RunBattlefieldContainment()
        {
            ValidateBattlefieldContainmentSource(
                new BattleUiLayout(GameConfig.DefaultBattlefield));
            Debug.Log("FRUIT_DEFENSE_BATTLEFIELD_CONTAINMENT_OK");
        }

        private static void ValidateReferenceGeometry(BattleUiLayout layout)
        {
            Assert(Approximately(layout.Design, new Rect(0f, 0f, 402f, 874f)),
                "design geometry changed");
            Assert(Approximately(layout.Header, new Rect(14f, 36f, 374f, 114f))
                && Approximately(layout.PageShell, new Rect(14f, 154f, 374f, 698f))
                && Approximately(layout.BattleStage, new Rect(22f, 168f, 358f, 338f))
                && Approximately(layout.Board, layout.BattleStage),
                "top-level Battle chrome changed");
            Assert(Approximately(layout.PhaseWaveRow, new Rect(24f, 518f, 354f, 52f))
                && Approximately(layout.ContextTray, new Rect(24f, 578f, 354f, 88f))
                && Approximately(layout.NurseryTray, new Rect(24f, 674f, 354f, 92f))
                && Approximately(layout.RefreshAction, new Rect(24f, 774f, 354f, 64f))
                && Mathf.Approximately(layout.PageShell.yMax - layout.RefreshAction.yMax, 14f),
                "phase/context/nursery/refresh closeout geometry changed");
            Assert(Approximately(layout.PauseAction, new Rect(264f, 50f, 48f, 48f))
                && Approximately(layout.SpeedAction, new Rect(318f, 50f, 56f, 48f))
                && Approximately(layout.HeaderTitle, new Rect(40f, 52f, 210f, 38f))
                && Approximately(layout.SunMetric, new Rect(28f, 101f, 112f, 40f))
                && Approximately(layout.LivesMetric, new Rect(145f, 101f, 112f, 40f))
                && Approximately(layout.WaveMetric, new Rect(262f, 101f, 112f, 40f)),
                "header action targets changed");
            Assert(Approximately(layout.PhaseStatus, new Rect(24f, 518f, 354f, 52f))
                && Approximately(layout.PhaseStatusWithWaveAction,
                    new Rect(24f, 518f, 168f, 52f))
                && Approximately(layout.WaveAction, new Rect(204f, 518f, 174f, 52f)),
                "independent phase status or Wave target changed");
            Assert(Approximately(layout.ContextTrayTitle, new Rect(32f, 582f, 120f, 24f))
                && Approximately(layout.NurseryTrayTitle, new Rect(32f, 678f, 120f, 24f))
                && Approximately(layout.Tool(0), new Rect(32f, 610f, 78.5f, 48f))
                && Approximately(layout.Tool(3), new Rect(291.5f, 610f, 78.5f, 48f))
                && Approximately(layout.NurserySlot(0), new Rect(32f, 706f, 58f, 52f))
                && Approximately(layout.NurserySlot(4), new Rect(312f, 706f, 58f, 52f)),
                "tool or nursery cell geometry changed");
            Assert(Approximately(layout.DetailTitle, new Rect(32f, 582f, 290f, 24f))
                && Approximately(layout.DetailBody, new Rect(32f, 614f, 290f, 22f))
                && Approximately(layout.DetailCloseAction, new Rect(330f, 582f, 44f, 44f))
                && layout.DetailBody.yMin - layout.DetailTitle.yMax >= 8f
                && layout.DetailCloseAction.xMin - layout.DetailTitle.xMax >= 8f
                && layout.DetailCloseAction.xMin - layout.DetailBody.xMax >= 8f,
                "detail title/body/close preserve the approved eight-point gaps");
            Assert(Approximately(layout.Modal, new Rect(36f, 300f, 330f, 244f))
                && Approximately(layout.TerminalModal, new Rect(28f, 270f, 346f, 320f))
                && Approximately(layout.ModalTerminalTitle, new Rect(48f, 292f, 306f, 56f))
                && Approximately(layout.ModalResultBanner, new Rect(70f, 352f, 262f, 64f))
                && Approximately(layout.ModalResultBannerText,
                    new Rect(102f, 360f, 198f, 48f))
                && Approximately(layout.ModalOrchardVista, new Rect(56f, 424f, 112f, 63f))
                && Approximately(layout.ModalTerminalMessage, new Rect(180f, 420f, 142f, 64f))
                && Approximately(layout.ModalResultIndicator, new Rect(328f, 438f, 24f, 24f))
                && Approximately(layout.ModalAction(0, 1), new Rect(90f, 510f, 222f, 52f))
                && Approximately(layout.ModalAction(0, 2), new Rect(54f, 466f, 142f, 52f))
                && Approximately(layout.ModalAction(1, 2), new Rect(206f, 466f, 142f, 52f)),
                "modal/result geometry changed");
            Assert(Approximately(layout.Battlefield.BoardRect, layout.Board),
                "layout and battlefield projection do not share the authoritative board rectangle");
            Assert(Approximately(layout.Battlefield.MapViewportRect,
                    layout.Board)
                && Approximately(layout.Battlefield.GridRect,
                    new Rect(24f, 182.125f, 354f, 309.75f))
                && Mathf.Approximately(layout.Battlefield.TileSize, 44.25f),
                "Battlefield grid composition changed inside the map viewport");
            var left = layout.Battlefield.GridRect.xMin
                - layout.Battlefield.MapViewportRect.xMin;
            var right = layout.Battlefield.MapViewportRect.xMax
                - layout.Battlefield.GridRect.xMax;
            var top = layout.Battlefield.GridRect.yMin
                - layout.Battlefield.MapViewportRect.yMin;
            var bottom = layout.Battlefield.MapViewportRect.yMax
                - layout.Battlefield.GridRect.yMax;
            Assert(Mathf.Abs(left - right) <= 1f
                && Mathf.Abs(top - bottom) <= 1f
                && Mathf.Approximately(left, 2f)
                && Mathf.Approximately(top, 14.125f),
                "Battlefield grid has symmetric visual gutters relative to MapViewportRect");
        }

        private static void ValidateNamedTracksAndRhythm(BattleUiLayout layout)
        {
            Assert(Mathf.Approximately(BattleUiLayout.SpacingUnit, 4f)
                && Mathf.Approximately(BattleUiLayout.ContentInset, 8f),
                "Battle named tracks derive from the four-point spacing unit");
            Assert(Mathf.Approximately(layout.Header.xMin, layout.PageShell.xMin)
                && Mathf.Approximately(layout.Header.xMax, layout.PageShell.xMax)
                && Mathf.Approximately(layout.PageShell.yMin - layout.Header.yMax,
                    BattleUiLayout.SpacingUnit),
                "raised Header and PageShell share peer edges with one four-point gap");
            var stageHeightFraction = layout.BattleStage.height
                / BattleUiLayout.DesignHeight;
            Assert(stageHeightFraction >= .38f && stageHeightFraction <= .43f,
                "BattleStage leaves the approved 38-to-43-percent height band");

            var pageChildren = new[]
            {
                layout.BattleStage, layout.PhaseWaveRow, layout.ContextTray,
                layout.NurseryTray, layout.RefreshAction,
            };
            for (var index = 0; index < pageChildren.Length; index++)
            {
                Assert(Contains(layout.PageShell, pageChildren[index]),
                    "PageShell no longer contains Battle child track: " + index);
            }
            Assert(Mathf.Approximately(layout.BattleStage.xMin - layout.PageShell.xMin, 8f)
                && Mathf.Approximately(layout.PageShell.xMax - layout.BattleStage.xMax, 8f)
                && Mathf.Approximately(layout.PhaseWaveRow.yMin - layout.Board.yMax, 12f)
                && Mathf.Approximately(layout.ContextTray.yMin - layout.PhaseWaveRow.yMax, 8f)
                && Mathf.Approximately(layout.NurseryTray.yMin - layout.ContextTray.yMax, 8f)
                && Mathf.Approximately(layout.RefreshAction.yMin - layout.NurseryTray.yMax, 8f),
                "PageShell preserves its inset stage and four-point vertical rhythm");

            Assert(layout.ContextTrayTitle.height == 24f
                && layout.NurseryTrayTitle.height == 24f
                && BattleUiLayout.NurserySlotLabel(layout.NurserySlot(0)).height == 44f
                && layout.DetailTitle.height == 24f
                && layout.DetailBody.height == 22f
                && layout.HeaderTitle.height == 38f
                && layout.SunMetric.height == 40f
                && layout.LivesMetric.height == 40f
                && layout.WaveMetric.height == 40f,
                "Battle text owners expose complete semantic line-height boxes");
            Assert(Mathf.Approximately(layout.ContextTrayTitle.yMin - layout.ContextTray.yMin,
                       BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.Tool(0).yMin - layout.ContextTrayTitle.yMax,
                    BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.ContextTray.yMax - layout.Tool(0).yMax, 8f)
                && Mathf.Approximately(layout.NurseryTrayTitle.yMin - layout.NurseryTray.yMin,
                    BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.NurserySlot(0).yMin
                    - layout.NurseryTrayTitle.yMax, BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.NurseryTray.yMax
                    - layout.NurserySlot(0).yMax, 8f),
                "tool and nursery tracks preserve complete four-side insets");
        }

        private static void ValidateViewportBackgroundContract()
        {
            var full = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 0f, 402f, 874f), 402f, 874f);
            var inset = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 34f, 402f, 796f), 402f, 874f);
            Assert(Approximately(full.DesignViewportRect, new Rect(0f, 0f, 402f, 874f)),
                "full viewport no longer maps the design one-to-one");
            Assert(inset.DesignViewportRect.xMin > 0f
                && inset.DesignViewportRect.yMin >= 43.99f
                && inset.DesignViewportRect.xMax < 402f
                && inset.DesignViewportRect.yMax <= 840.01f,
                "inset viewport no longer letterboxes the design inside the safe area");
        }

        private static void ValidateProjectedViewportMatrix(BattleUiLayout layout)
        {
            foreach (var viewport in RuntimeUiQualityProfile.Viewports)
            {
                ValidateProjectedViewport(layout, viewport, viewport.FullSafeArea, "full");
                ValidateProjectedViewport(layout, viewport, viewport.InsetSafeArea, "inset");
            }
        }

        private static void ValidateProjectedViewport(BattleUiLayout layout,
            RuntimeUiQualityViewportCase viewport, Rect safeArea, string safeAreaKind)
        {
            var projection = BattlefieldProjection.CalculateViewportLayout(
                viewport.Width, viewport.Height, safeArea,
                BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
            var caseName = viewport.Id + "/" + safeAreaKind;
            Assert(Approximately(projection.ProjectDesignRect(layout.Design),
                    projection.DesignViewportRect)
                && Contains(projection.SafeAreaInGuiSpace,
                    projection.DesignViewportRect),
                caseName + " projects the complete design inside the GUI safe area");

            var header = SnapDeviceRect(projection.ProjectDesignRect(layout.Header));
            var pageShell = SnapDeviceRect(
                projection.ProjectDesignRect(layout.PageShell));
            var stage = SnapDeviceRect(
                projection.ProjectDesignRect(layout.BattleStage));
            Assert(Mathf.Approximately(header.xMin, pageShell.xMin)
                && Mathf.Approximately(header.xMax, pageShell.xMax),
                caseName + " preserves peer-frame device edges");

            var expectedGap = Mathf.Round(
                BattleUiLayout.SpacingUnit * projection.Scale);
            Assert(Mathf.Abs(pageShell.yMin - header.yMax - expectedGap) <= 1f,
                caseName + " preserves the four-point top-level gap after snapping");

            var projectedBoard = projection.ProjectDesignRect(layout.Board);
            Assert(Approximately(projectedBoard,
                    projection.ProjectDesignRect(layout.Battlefield.BoardRect))
                && Contains(stage, SnapDeviceRect(projectedBoard))
                && Contains(pageShell, stage)
                && Contains(pageShell, SnapDeviceRect(
                    projection.ProjectDesignRect(layout.RefreshAction)))
                && Contains(SnapDeviceRect(projection.ProjectDesignRect(
                    layout.PhaseWaveRow)), SnapDeviceRect(
                    projection.ProjectDesignRect(layout.WaveAction))),
                caseName + " preserves draw/hit identity and control containment");

            var lineOwners = new[]
            {
                layout.HeaderTitle,
                layout.SunMetric,
                layout.LivesMetric,
                layout.WaveMetric,
                layout.ContextTrayTitle,
                layout.NurseryTrayTitle,
                BattleUiLayout.NurserySlotLabel(layout.NurserySlot(0)),
                layout.DetailTitle,
                layout.DetailBody,
            };
            var logicalLineHeights = new[]
            {
                38f, 40f, 40f, 40f, 24f, 24f, 22f, 24f, 22f,
            };
            for (var index = 0; index < lineOwners.Length; index++)
            {
                var owner = SnapDeviceRect(
                    projection.ProjectDesignRect(lineOwners[index]));
                var requiredHeight = Mathf.Floor(
                    logicalLineHeights[index] * projection.Scale);
                Assert(Contains(SnapDeviceRect(projection.DesignViewportRect), owner)
                    && owner.height >= requiredHeight,
                    caseName + " preserves projected semantic line-height owner " + index);
            }
        }

        private static void ValidateInteractionBoundaries(BattleUiLayout layout)
        {
            Assert(!layout.PauseAction.Overlaps(layout.SpeedAction),
                "pause and speed targets overlap");
            Assert(Contains(layout.Header, layout.PauseAction)
                && Contains(layout.Header, layout.SpeedAction)
                && Contains(layout.Header, layout.SunMetric)
                && Contains(layout.Header, layout.LivesMetric)
                && Contains(layout.Header, layout.WaveMetric)
                && !layout.SunMetric.Overlaps(layout.LivesMetric)
                && !layout.LivesMetric.Overlaps(layout.WaveMetric),
                "header targets leave the header");

            Rect? previous = null;
            for (var index = 0; index < BattleUiLayout.ToolCount; index++)
            {
                var rect = layout.Tool(index);
                Assert(Contains(layout.ContextTray, rect) && Mathf.Min(rect.width, rect.height) >= 44f,
                    "tool target is clipped or undersized: " + index);
                var sourceIcon = BattleUiLayout.ToolRecipeSourceIcon(rect);
                var operatorGlyph = BattleUiLayout.ToolRecipeOperator(rect);
                var targetIcon = BattleUiLayout.ToolRecipeTargetIcon(rect);
                var inventoryBadge = BattleUiLayout.ToolInventoryBadge(rect);
                Assert(Contains(rect, sourceIcon)
                    && Contains(rect, operatorGlyph)
                    && Contains(rect, targetIcon)
                    && Contains(rect, inventoryBadge)
                    && !sourceIcon.Overlaps(operatorGlyph)
                    && !sourceIcon.Overlaps(targetIcon)
                    && !operatorGlyph.Overlaps(targetIcon),
                    "recipe card anatomy clips or overlaps: " + index);
                Assert(!previous.HasValue || !previous.Value.Overlaps(rect),
                    "tool targets overlap: " + index);
                previous = rect;
            }

            previous = null;
            for (var slot = 0; slot < layout.NurserySlotCount; slot++)
            {
                var rect = layout.NurserySlot(slot);
                Assert(Contains(layout.NurseryTray, rect) && Mathf.Min(rect.width, rect.height) >= 44f,
                    "nursery target is clipped or undersized: " + slot);
                Assert(!previous.HasValue || !previous.Value.Overlaps(rect),
                    "nursery targets overlap: " + slot);
                previous = rect;
            }

            Assert(Approximately(layout.BattleStage, layout.Board)
                && Contains(layout.PhaseWaveRow, layout.PhaseStatus)
                && Contains(layout.PhaseWaveRow, layout.PhaseStatusWithWaveAction)
                && Contains(layout.PhaseWaveRow, layout.WaveAction)
                && !layout.PhaseStatusWithWaveAction.Overlaps(layout.WaveAction)
                && Contains(layout.ContextTray, layout.Tool(0))
                && Contains(layout.ContextTray, layout.DetailTitle)
                && Contains(layout.ContextTray, layout.DetailBody)
                && Contains(layout.ContextTray, layout.DetailCloseAction)
                && !layout.ContextTray.Overlaps(layout.NurseryTray)
                && !layout.NurseryTray.Overlaps(layout.RefreshAction),
                "stage and mutually exclusive context anatomy lost authority");
            Assert(!layout.BattleStage.Overlaps(layout.PhaseWaveRow)
                && Mathf.Min(layout.WaveAction.width, layout.WaveAction.height) >= 44f,
                "phase/Wave row crossed battlefield geometry or lost its touch target");

            var firstModal = layout.ModalAction(0, 2);
            var secondModal = layout.ModalAction(1, 2);
            Assert(Contains(layout.Modal, firstModal) && Contains(layout.Modal, secondModal)
                && !firstModal.Overlaps(secondModal),
                "modal action targets are clipped or overlapping");
            var terminalAction = layout.ModalAction(0, 1);
            Assert(Contains(layout.TerminalModal, layout.ModalResultBanner)
                && Contains(layout.ModalResultBanner, layout.ModalResultBannerText)
                && Contains(layout.TerminalModal, layout.ModalOrchardVista)
                && Contains(layout.TerminalModal, layout.ModalTerminalMessage)
                && Contains(layout.TerminalModal, layout.ModalResultIndicator)
                && Contains(layout.TerminalModal, layout.ModalTerminalTitle)
                && Contains(layout.TerminalModal, terminalAction)
                && !layout.ModalTerminalTitle.Overlaps(layout.ModalResultBanner)
                && !layout.ModalTerminalTitle.Overlaps(layout.ModalOrchardVista)
                && !layout.ModalResultBanner.Overlaps(layout.ModalOrchardVista)
                && !layout.ModalOrchardVista.Overlaps(layout.ModalTerminalMessage)
                && !layout.ModalResultIndicator.Overlaps(layout.ModalTerminalMessage)
                && layout.ModalResultIndicator.xMin - layout.ModalTerminalMessage.xMax >= 6f
                && !layout.ModalResultBanner.Overlaps(terminalAction)
                && !layout.ModalOrchardVista.Overlaps(terminalAction)
                && !layout.ModalTerminalMessage.Overlaps(terminalAction),
                "terminal result hierarchy clips or overlaps copy/actions");

            var clamped = layout.ClampDragPreview(new Rect(-30f, 900f, 48f, 48f));
            Assert(Approximately(clamped.center, new Vector2(24f, 850f)),
                "drag preview clamp changed");
            var mergeHint = layout.MergeHint(new Rect(180f, 180f, 48f, 48f), 118f);
            Assert(Approximately(mergeHint, new Rect(125f, 232f, 158f, 24f)),
                "merge hint geometry changed");
            Assert(Approximately(BattleUiLayout.CueBadge(layout.Tool(0)),
                    new Rect(34f, 612f, 28f, 28f))
                && Contains(layout.Tool(0), BattleUiLayout.CueBadge(layout.Tool(0)))
                && Contains(mergeHint, BattleUiLayout.CueLabel(mergeHint)),
                "drop/state cue escaped the authoritative target rectangle");
        }

        private static void ValidateFinitePresentationState()
        {
            var ready = BattleUiPresentationState.Create(GamePhase.Ready, false);
            var active = BattleUiPresentationState.Create(GamePhase.Playing, false);
            var between = BattleUiPresentationState.Create(GamePhase.BetweenWaves, false);
            var paused = BattleUiPresentationState.Create(GamePhase.Playing, true);
            var victory = BattleUiPresentationState.Create(GamePhase.Victory, true);
            var defeat = BattleUiPresentationState.Create(GamePhase.Defeat, false);
            var pausedModal = paused.ModalContent(3, 10);
            var victoryModal = victory.ModalContent(6, 10);
            var defeatModal = defeat.ModalContent(6, 10);

            Assert(ready.Mode == BattleUiChromeMode.Ready && ready.ShowsWaveAction
                && ready.WaveActionLabel == "开始波次" && !ready.BlocksDrag
                && ready.PauseActionIcon == RuntimeUiArtSlot.IconControlPause
                && ready.PhaseStatusInteractionState == RuntimeUiInteractionState.Warning
                && !ready.BlocksBackgroundInput,
                "ready presentation state changed");
            Assert(active.Mode == BattleUiChromeMode.ActiveWave && !active.ShowsWaveAction
                && active.PhaseStatusText(3, 7, 0f) == "第 3 波 · 7 个敌人"
                && active.PhaseStatusInteractionState == RuntimeUiInteractionState.Normal
                && !active.BlocksBackgroundInput,
                "active-wave presentation state changed");
            Assert(between.Mode == BattleUiChromeMode.BetweenWaves && between.ShowsWaveAction
                && between.WaveActionLabel == "立即开始下一波"
                && between.PhaseStatusText(1, 0, 9.5f) == "下一波倒计时 10 秒"
                && between.PhaseStatusInteractionState == RuntimeUiInteractionState.Warning
                && !between.BlocksBackgroundInput,
                "between-wave presentation state changed");
            Assert(paused.Mode == BattleUiChromeMode.Paused && paused.BlocksDrag
                && paused.ModalActionCount == 2
                && paused.PauseActionIcon == RuntimeUiArtSlot.IconControlContinue
                && paused.PhaseMode == BattleUiPhaseMode.ActiveWave
                && paused.PhaseStatusText(3, 7, 0f) == "第 3 波 · 7 个敌人"
                && paused.BlocksBackgroundInput
                && pausedModal.SurfaceState == RuntimeUiInteractionState.Warning
                && !pausedModal.UsesResultCard
                && pausedModal.PrimaryActionKind == RuntimeUiActionKind.Primary
                && pausedModal.PrimaryActionIcon == RuntimeUiArtSlot.IconControlContinue
                && pausedModal.SecondaryActionKind == RuntimeUiActionKind.Danger
                && pausedModal.SecondaryActionIcon == RuntimeUiArtSlot.IconControlRetry,
                "paused presentation state changed");
            Assert(victory.Mode == BattleUiChromeMode.Victory && victory.BlocksDrag
                && victory.ModalActionCount == 1
                && victory.PhaseStatusInteractionState == RuntimeUiInteractionState.Success
                && victory.BlocksBackgroundInput
                && victoryModal.MessageLines.FirstLine == "成功抵御全部"
                && victoryModal.MessageLines.SecondLine == "10 波僵尸"
                && victoryModal.SurfaceState == RuntimeUiInteractionState.Success
                && victoryModal.UsesResultCard
                && victoryModal.ResultBannerText == "胜利"
                && victoryModal.PrimaryActionKind == RuntimeUiActionKind.Primary
                && victoryModal.PrimaryActionIcon == RuntimeUiArtSlot.IconControlRetry,
                "victory presentation or terminal precedence changed");
            Assert(defeat.Mode == BattleUiChromeMode.Defeat && defeat.BlocksDrag
                && defeat.PhaseStatusInteractionState == RuntimeUiInteractionState.Error
                && defeat.BlocksBackgroundInput
                && defeatModal.MessageLines.FirstLine == "坚持到第 6 波"
                && !defeatModal.MessageLines.HasSecondLine
                && defeatModal.SurfaceState == RuntimeUiInteractionState.Error
                && defeatModal.UsesResultCard
                && defeatModal.ResultBannerText == "失败"
                && defeatModal.PrimaryActionKind == RuntimeUiActionKind.Primary
                && defeatModal.PrimaryActionIcon == RuntimeUiArtSlot.IconControlRetry,
                "defeat presentation state changed");

            Assert(BattleUiPresentationState.ResolveActionState(
                    false, false, false) == RuntimeUiInteractionState.Normal
                && BattleUiPresentationState.ResolveActionState(
                    false, true, false) == RuntimeUiInteractionState.HoveredOrFocused
                && BattleUiPresentationState.ResolveActionState(
                    true, true, false) == RuntimeUiInteractionState.Selected
                && BattleUiPresentationState.ResolveActionState(
                    true, true, true) == RuntimeUiInteractionState.Pressed,
                "header/wave interaction-state precedence changed");

            Assert(BattleUiPresentationState.ResolveSlotState(
                    false, true, true, true) == RuntimeUiInteractionState.Disabled
                && BattleUiPresentationState.ResolveSlotState(
                    true, true, true, false) == RuntimeUiInteractionState.Selected,
                "tool/slot disabled or selected precedence changed");
            Assert(BattleUiPresentationState.ResolveDropCue(
                    true, false, false) == BattleUiDropCue.Legal
                && BattleUiPresentationState.ResolveDropCue(
                    false, true, true) == BattleUiDropCue.Illegal
                && BattleUiPresentationState.ResolveDropCue(
                    true, true, false) == BattleUiDropCue.Merge
                && BattleUiPresentationState.ResolveDropCue(
                    true, false, true) == BattleUiDropCue.Swap
                && BattleUiPresentationState.DropIndicatorKind(
                    BattleUiDropCue.Legal) == RuntimeUiIndicatorKind.DragLegal
                && BattleUiPresentationState.DropIndicatorKind(
                    BattleUiDropCue.Illegal) == RuntimeUiIndicatorKind.DragIllegal
                && BattleUiPresentationState.DropIndicatorKind(
                    BattleUiDropCue.Merge) == RuntimeUiIndicatorKind.Merge
                && BattleUiPresentationState.DropIndicatorKind(
                    BattleUiDropCue.Swap) == RuntimeUiIndicatorKind.Swap,
                "finite legal/illegal/merge/swap cues changed");
            Assert(BattleUiPresentationState.SnapsPlantDragFeedback(
                    BattleUiDropCue.Legal)
                && BattleUiPresentationState.SnapsPlantDragFeedback(
                    BattleUiDropCue.Merge)
                && BattleUiPresentationState.SnapsPlantDragFeedback(
                    BattleUiDropCue.Swap)
                && !BattleUiPresentationState.SnapsPlantDragFeedback(
                    BattleUiDropCue.Illegal)
                && !BattleUiPresentationState.SnapsPlantDragFeedback(
                    BattleUiDropCue.None),
                "plant drag feedback snap semantics changed");
            Assert(BattleUiPresentationState.ResolveTransientStatusState(
                    RuntimeUiInteractionState.Normal, BattleUiDropCue.None)
                    == RuntimeUiInteractionState.Normal
                && BattleUiPresentationState.ResolveTransientStatusState(
                    RuntimeUiInteractionState.Error, BattleUiDropCue.None)
                    == RuntimeUiInteractionState.Error
                && BattleUiPresentationState.ResolveTransientStatusState(
                    RuntimeUiInteractionState.Success, BattleUiDropCue.Swap)
                    == RuntimeUiInteractionState.Selected,
                "transient status semantic state changed");
            Assert(BattleUiPresentationState.FormatTransientStatus(true, "水果已放回刷新栏")
                    == "✓ 水果已放回刷新栏"
                && BattleUiPresentationState.FormatTransientStatus(false,
                    "目标植物移动冷却 10.0 秒")
                    == "! 目标植物移动冷却 10.0 秒",
                "transient status uses the finite real success/error prefixes");
        }

        private static void ValidateDragFeedbackGeometry(BattleUiLayout layout)
        {
            var source = layout.NurserySlot(0);
            var freePreview = layout.ClampDragPreview(
                DragGeometry.PreviewRect(new Vector2(190f, 610f)));
            var free = DragGeometry.ResolveConnector(source, freePreview);
            Assert(free.Visible && free.DashCount > 0
                && PointOnBoundary(source, free.Start)
                && PointOnBoundary(freePreview, free.End),
                "free plant drag connector does not join authoritative source and preview bounds");
            for (var index = 0; index < free.DashCount; index++)
            {
                var dash = free.DashRect(index);
                Assert(IsFinite(dash.x) && IsFinite(dash.y)
                    && IsFinite(dash.width) && IsFinite(dash.height)
                    && dash.width > 0f
                    && Mathf.Approximately(
                        dash.height, DragGeometry.ConnectorThickness)
                    && dash.xMin >= free.Start.x
                    && dash.xMax <= free.Start.x + free.Length + .01f,
                    "plant drag connector produced invalid dash geometry at " + index);
            }

            var target = new Rect(248f, 238f, 46f, 46f);
            var snapped = DragGeometry.ResolveConnector(source, target);
            Assert(snapped.Visible
                && PointOnBoundary(source, snapped.Start)
                && PointOnBoundary(target, snapped.End),
                "legal plant drag connector does not terminate on the authoritative target frame");

            var pcViewport = BattlefieldProjection.CalculateViewportLayout(
                1280f, 720f, new Rect(0f, 0f, 1280f, 720f),
                BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
            var projected = DragGeometry.ProjectConnector(
                snapped, pcViewport.GuiMatrix);
            var expectedStart3 = pcViewport.GuiMatrix.MultiplyPoint3x4(snapped.Start);
            var expectedEnd3 = pcViewport.GuiMatrix.MultiplyPoint3x4(snapped.End);
            Assert(projected.Visible
                && Approximately(projected.Start,
                    new Vector2(expectedStart3.x, expectedStart3.y))
                && Approximately(projected.End,
                    new Vector2(expectedEnd3.x, expectedEnd3.y))
                && Mathf.Approximately(projected.Thickness,
                    snapped.Thickness * pcViewport.Scale)
                && Mathf.Abs(projected.AngleDegrees - snapped.AngleDegrees) < .01f,
                "letterboxed PC connector projection reapplied offset or skewed its dashes");
            for (var index = 0; index < projected.DashCount; index++)
            {
                var dash = projected.DashRect(index);
                Assert(IsFinite(dash.x) && IsFinite(dash.y)
                    && IsFinite(dash.width) && IsFinite(dash.height)
                    && dash.width > 0f && dash.height > 0f,
                    "letterboxed PC connector produced invalid projected dash geometry at "
                    + index);
            }
            Assert(!DragGeometry.ResolveConnector(source, source).Visible
                && !DragGeometry.ResolveConnector(default, target).Visible
                && !DragGeometry.ProjectConnector(snapped, Matrix4x4.zero).Visible,
                "overlapping or invalid drag feedback geometry remained visible");
        }

        private static void ValidatePhaseWaveText(BattleUiLayout layout)
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            var actionFont = theme.Typography.For(
                RuntimeUiTypographyRole.ControlLabel).Font;
            var statusFont = theme.Typography.For(
                RuntimeUiTypographyRole.Supplemental).Font;
            Assert(actionFont != null && statusFont != null,
                "release theme has no packaged role font for phase/Wave copy");
            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var actionStyle = context.Styles.SingleLineText(
                RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleCenter);
            Assert(ReferenceEquals(actionStyle.font, actionFont)
                && !actionStyle.wordWrap
                && actionStyle.clipping == TextClipping.Clip
                && statusFont != null,
                "phase/Wave styles must use their packaged role fonts without wrapping");

            var between = BattleUiPresentationState.Create(GamePhase.BetweenWaves, false);
            var actionText = between.WaveActionLabel;
            var statusNineSeconds = between.PhaseStatusText(1, 0, 8.5f);
            var statusTenSeconds = between.PhaseStatusText(1, 0, 9.5f);
            Assert(actionText == "立即开始下一波"
                && statusNineSeconds == "下一波倒计时 9 秒"
                && statusTenSeconds == "下一波倒计时 10 秒",
                "between-wave product copy changed");

            var actionLayout = RuntimeUiGui.ResolveActionContentLayout(
                context, layout.WaveAction, actionText,
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.StartWave),
                RuntimeUiInteractionState.Normal,
                RuntimeUiArtSlot.IconControlStartWave,
                RuntimeUiTypographyRole.ControlLabel);
            Assert(actionLayout.Fits
                && Contains(layout.WaveAction, actionLayout.GroupRect),
                "Wave action does not contain its packaged control-label font and icon");
            AssertStatusCopyFits(context, layout.PhaseStatusWithWaveAction,
                statusNineSeconds, RuntimeUiInteractionState.Warning,
                "nine-second phase status");
            AssertStatusCopyFits(context, layout.PhaseStatusWithWaveAction,
                statusTenSeconds, RuntimeUiInteractionState.Warning,
                "ten-second phase status");

            Assert(Approximately(layout.PhaseStatusWithWaveAction,
                    new Rect(24f, 518f, 168f, 52f))
                && Approximately(layout.WaveAction, new Rect(204f, 518f, 174f, 52f)),
                "between-wave draw/hit rectangles changed while fixing text clipping");
        }

        private static void AssertStatusCopyFits(RuntimeUiDrawContext context,
            Rect owner, string text, RuntimeUiInteractionState state,
            string caseName)
        {
            var mode = RuntimeUiGui.ResolveStatusTextMode(context, owner, text,
                state, RuntimeUiTypographyRole.Supplemental);
            var layout = RuntimeUiGui.ResolveStatusTextLayout(context, owner,
                state, RuntimeUiTypographyRole.Supplemental, mode);
            var lines = RuntimeUiGui.ResolveStatusTextLines(layout, text);
            Assert(ReferenceEquals(layout.Style.font, context.Theme.Typography.For(
                       RuntimeUiTypographyRole.Supplemental).Font)
                && !layout.Style.wordWrap
                && layout.Style.clipping == TextClipping.Clip,
                caseName + " does not use the packaged supplemental role");
            if (mode == RuntimeUiStatusTextMode.SingleLine)
            {
                Assert(!lines.HasSecondLine && lines.FirstLine == text,
                    caseName + " changed its complete one-line copy");
                AssertControlledLineFits(layout.Style, lines.FirstLine,
                    layout.FirstLineRect, 1f, caseName);
                return;
            }

            Assert(mode == RuntimeUiStatusTextMode.CompactTwoLines
                && lines.HasSecondLine
                && lines.FirstLine + lines.SecondLine == text,
                caseName + " is not a complete controlled two-line copy");
            AssertControlledLineFits(layout.Style, lines.FirstLine,
                layout.FirstLineRect, 1f, caseName + " first line");
            AssertControlledLineFits(layout.Style, lines.SecondLine,
                layout.SecondLineRect, 1f, caseName + " second line");
        }

        private static void ValidateTransientStatusText(BattleUiLayout layout)
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var statusRect = layout.PhaseStatusWithWaveAction;
            var plantDetail = "正在查看豌豆；拖动可移动或合成";
            var messages = new[]
            {
                "已取消武器选择",
                "拖动或点击植物安装机枪",
                "拖动或点击植物安装冰块",
                "拖动或点击植物安装辣椒",
                "拖动花盆到绿色候选格，或点击扩建",
                "已取消扩建",
                "拖拽已取消，物品返回原位",
                "已取消拖拽，物品返回原位",
                "拖动场上水果到这里",
                "将选中水果拖到这里",
                "拖动苗圃水果到这里",
                "将向日葵拖到这里",
                plantDetail,
                "正在查看向日葵；拖动到花盆种植",
                "刷新完成：水果 0 株，花盆×5 已入库",
                "获得 5 株水果",
                "目标植物移动冷却 10.0 秒",
                "植物已在这个苗圃槽位中",
                "只能扩建到现有花盆的上下左右",
                "这株植物已经装备武器",
                "武器与该植物不兼容",
                "水果已移动到新槽位",
                "水果已放回刷新栏",
                "植物已交换位置",
                "向日葵升至 4 星",
                "第 15 波来袭",
            };
            var full = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 0f, 402f, 874f), 402f, 874f);
            var inset = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 34f, 402f, 796f), 402f, 874f);
            var statusStates = new[]
            {
                RuntimeUiInteractionState.Success,
                RuntimeUiInteractionState.Warning,
                RuntimeUiInteractionState.Error,
            };
            for (var stateIndex = 0; stateIndex < statusStates.Length; stateIndex++)
            {
                var stateLayout = RuntimeUiGui.ResolveStatusTextLayout(context, statusRect,
                    statusStates[stateIndex], RuntimeUiTypographyRole.Supplemental,
                    RuntimeUiStatusTextMode.CompactTwoLines);
                Assert(stateLayout.HasIndicator
                        && stateLayout.IndicatorRect.width > 0f
                        && stateLayout.IndicatorRect.height > 0f
                        && !stateLayout.FirstLineRect.Overlaps(stateLayout.IndicatorRect)
                        && !stateLayout.SecondLineRect.Overlaps(stateLayout.IndicatorRect),
                    statusStates[stateIndex]
                    + " transient status must retain an independent non-color indicator");
            }

            for (var index = 0; index < messages.Length; index++)
            {
                var message = messages[index];
                var mode = RuntimeUiGui.ResolveStatusTextMode(context, statusRect, message,
                    RuntimeUiInteractionState.Success,
                    RuntimeUiTypographyRole.Supplemental);
                var textLayout = RuntimeUiGui.ResolveStatusTextLayout(context, statusRect,
                    RuntimeUiInteractionState.Success,
                    RuntimeUiTypographyRole.Supplemental, mode);
                var textLines = RuntimeUiGui.ResolveStatusTextLines(textLayout, message);
                Assert(textLayout.Style.font == theme.Typography.For(
                           RuntimeUiTypographyRole.Supplemental).Font
                    && textLayout.Style.clipping == TextClipping.Clip,
                    "transient status must use the release Noto font and controlled clipping: "
                    + message);
                Assert(!textLayout.HasIndicator
                        || (!textLayout.FirstLineRect.Overlaps(textLayout.IndicatorRect)
                            && !textLayout.SecondLineRect.Overlaps(
                                textLayout.IndicatorRect)),
                    "transient status text overlaps its independent non-color indicator: "
                    + message);
                Assert(!textLayout.FirstLineRect.Overlaps(layout.WaveAction)
                        && !textLayout.SecondLineRect.Overlaps(layout.WaveAction)
                        && !textLayout.IndicatorRect.Overlaps(layout.WaveAction),
                    "transient status escaped into the wave action: " + message);

                if (mode == RuntimeUiStatusTextMode.SingleLine)
                {
                    Assert(!textLayout.Style.wordWrap && textLayout.MaximumLineCount == 1,
                        "short transient status must remain explicit single-line/no-wrap: "
                        + message);
                    Assert(!textLines.HasSecondLine && textLines.FirstLine == message,
                        "short transient status must remain one complete line: " + message);
                    AssertSingleLineFits(textLayout.Style, message,
                        textLayout.FirstLineRect.size, full.Scale,
                        "402 full transient status: " + message);
                    AssertSingleLineFits(textLayout.Style, message,
                        textLayout.FirstLineRect.size, inset.Scale,
                        "402 inset transient status: " + message);
                    continue;
                }

                Assert(mode == RuntimeUiStatusTextMode.CompactTwoLines
                        && !textLayout.Style.wordWrap
                        && textLayout.MaximumLineCount == 2
                        && textLines.HasSecondLine
                        && textLines.FirstLine + textLines.SecondLine == message,
                    "long transient status must use two controlled complete no-wrap lines: "
                    + message);
                AssertControlledLineFits(textLayout.Style, textLines.FirstLine,
                    textLayout.FirstLineRect, full.Scale,
                    "402 full transient status first line: " + message);
                AssertControlledLineFits(textLayout.Style, textLines.SecondLine,
                    textLayout.SecondLineRect, full.Scale,
                    "402 full transient status second line: " + message);
                AssertControlledLineFits(textLayout.Style, textLines.FirstLine,
                    textLayout.FirstLineRect, inset.Scale,
                    "402 inset transient status first line: " + message);
                AssertControlledLineFits(textLayout.Style, textLines.SecondLine,
                    textLayout.SecondLineRect, inset.Scale,
                    "402 inset transient status second line: " + message);
            }

            var guidanceMessages = new[]
            {
                "拖动场上水果到这里",
                "将选中水果拖到这里",
                "拖动苗圃水果到这里",
                "将向日葵拖到这里",
            };
            for (var index = 0; index < guidanceMessages.Length; index++)
            {
                var guidance = guidanceMessages[index];
                var mode = RuntimeUiGui.ResolveStatusTextMode(context, statusRect,
                    guidance, RuntimeUiInteractionState.Normal,
                    RuntimeUiTypographyRole.Supplemental);
                var guidanceLayout = RuntimeUiGui.ResolveStatusTextLayout(context, statusRect,
                    RuntimeUiInteractionState.Normal,
                    RuntimeUiTypographyRole.Supplemental, mode);
                Assert(mode == RuntimeUiStatusTextMode.SingleLine
                        && !guidanceLayout.HasIndicator,
                    "destination guidance must remain neutral, indicator-free, and single-line: "
                    + guidance);
                AssertSingleLineFits(guidanceLayout.Style, guidance,
                    guidanceLayout.FirstLineRect.size, full.Scale,
                    "402 full destination guidance: " + guidance);
                AssertSingleLineFits(guidanceLayout.Style, guidance,
                    guidanceLayout.FirstLineRect.size, inset.Scale,
                    "402 inset destination guidance: " + guidance);
            }

            var plantMode = RuntimeUiGui.ResolveStatusTextMode(context, statusRect,
                plantDetail, RuntimeUiInteractionState.Success,
                RuntimeUiTypographyRole.Supplemental);
            var plantLayout = RuntimeUiGui.ResolveStatusTextLayout(context, statusRect,
                RuntimeUiInteractionState.Success,
                RuntimeUiTypographyRole.Supplemental, plantMode);
            var plantLines = RuntimeUiGui.ResolveStatusTextLines(plantLayout, plantDetail);
            Assert(plantDetail == "正在查看豌豆；拖动可移动或合成"
                    && plantMode == RuntimeUiStatusTextMode.CompactTwoLines
                    && plantLines.HasSecondLine
                    && plantLines.FirstLine + plantLines.SecondLine == plantDetail,
                "plant-detail product copy must remain complete and render on two lines");
            Assert(Approximately(statusRect, new Rect(24f, 518f, 168f, 52f))
                    && Approximately(layout.WaveAction, new Rect(204f, 518f, 174f, 52f)),
                "transient status fix changed phase status or Wave draw/hit geometry");
        }

        private static void AssertSingleLineFits(GUIStyle style, string text,
            Vector2 availableLogicalSize, float viewportScale, string caseName)
        {
            var content = new GUIContent(text);
            var measured = style.CalcSize(content);
            var calculatedHeight = style.CalcHeight(content, availableLogicalSize.x);
            var measuredPixels = measured * viewportScale;
            var availablePixels = availableLogicalSize * viewportScale;
            const float pixelRoundingTolerance = .25f;
            Assert(measuredPixels.x <= availablePixels.x + pixelRoundingTolerance
                && calculatedHeight * viewportScale <= availablePixels.y
                    + pixelRoundingTolerance,
                caseName + " must fit one line; measured " + measuredPixels
                + " CalcHeight=" + calculatedHeight * viewportScale
                + " available " + availablePixels);
        }

        private static void AssertControlledLineFits(GUIStyle style, string text,
            Rect lineRect, float viewportScale, string caseName)
        {
            Assert(!style.wordWrap && style.clipping == TextClipping.Clip,
                caseName + " must use explicit no-wrap/controlled clipping");
            AssertSingleLineFits(style, text, lineRect.size, viewportScale, caseName);
        }

        private static void ValidateSharedChromeSource()
        {
            var source = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var header = MethodSlice(source,
                "private void DrawHeader(", "private void DrawBoard(");
            var status = MethodSlice(source,
                "private void DrawPhaseWaveRow(", "private void DrawEmbeddedBattleControls(");
            var embeddedControls = MethodSlice(source,
                "private void DrawEmbeddedBattleControls(", "private void DrawTools(");
            var nursery = MethodSlice(source,
                "private void DrawNursery(", "private void RefreshNurseryFromUi(");

            Assert(header.Contains("RuntimeUiGui.DrawRaisedPanel")
                && header.Contains("RuntimeUiGui.DrawMetric")
                && CountOccurrences(header, "drawSurface: true") == 3
                && !header.Contains("drawSurface: false")
                && !header.Contains("RuntimeUiGui.DrawMetricDivider")
                && header.Contains("TrackBattleAction(")
                && header.Contains("RuntimeUiGui.DrawCompactControlVisual")
                && !header.Contains("RuntimeUiActionKind.Quiet")
                && header.Contains("_game.TogglePause()")
                && header.Contains("_game.SetSpeed(")
                && status.Contains("RuntimeUiGui.DrawStatus")
                && status.Contains("TrackBattleAction(")
                && status.Contains("RuntimeUiGui.DrawActionVisual")
                && status.Contains("PrepareTransientStatusText")
                && status.Contains("RuntimeUiGui.ResolveStatusTextMode")
                && status.Contains("RuntimeUiArtSlot.IconControlStartWave")
                && status.Contains("_game.StartWave(out var reason)")
                && source.Contains("_actionPressTracker.Update")
                && source.Contains("RuntimeUiGui.ResolveStatusTextMode")
                && source.Contains("RuntimeUiGui.ResolveStatusTextLines")
                && source.Contains("_runtimeUiDrawContext = RuntimeUiGui.RequireContext")
                && embeddedControls.Contains(
                    "RuntimeUiGui.DrawStandardPanel(drawContext, layout.NurseryTray)")
                && !nursery.Contains("drawSurface: false"),
                "header/phase-Wave and framed nursery slices are bound to the cached shared visual system");

            var sharedSource = RuntimeUiSourceAuthority.ReadRuntimeGui();
            Assert(sharedSource.Contains("public GUIStyle SingleLineText(")
                && sharedSource.Contains("public GUIStyle CompactTwoLineText(")
                && sharedSource.Contains("wordWrap = false")
                && sharedSource.Contains("RuntimeUiStatusTextMode.CompactTwoLines")
                && sharedSource.Contains("ResolveStatusTextLines(")
                && sharedSource.Contains(
                    "context.Styles.SingleLineText(labelRole, TextAnchor.MiddleCenter)"),
                "shared action text is not explicitly bound to the cached single-line style");

            Assert(source.Contains("\"拖动可移动或合成\"")
                && source.Contains("\"正在查看\" + PlantDisplayName(_game, plant) + \"；\" + verb")
                && source.Contains("BattleUiPresentationState.FormatTransientStatus"),
                "Battle transient product copy or shared status-prefix formatter changed");

            var legacyTokens = new[]
            {
                "DrawPanel(", "DrawRect(", "GUI.Label(", "ColoredButton(",
                "Texture2D.whiteTexture", "GUI.skin", "new Rect(",
            };
            for (var index = 0; index < legacyTokens.Length; index++)
            {
                Assert(!header.Contains(legacyTokens[index])
                    && !status.Contains(legacyTokens[index]),
                    "header/phase-Wave slice retained legacy rendering: " + legacyTokens[index]);
            }
        }

        private static void ValidateSharedControlSource()
        {
            var source = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var runtimeGui = RuntimeUiSourceAuthority.ReadRuntimeGui();
            var controls = MethodSlice(source,
                "private void DrawEmbeddedBattleControls(",
                "private void RefreshNurseryFromUi(");
            var dragGhost = MethodSlice(source,
                "private void DrawDragGhost(", "private void DrawOverlay(");
            var dragConnector = MethodSlice(runtimeGui,
                "public static void DrawDragConnector(",
                "public static void DrawDragTargetFrame(");
            var dragTargetFrame = MethodSlice(runtimeGui,
                "public static void DrawDragTargetFrame(",
                "public static void DrawStateIndicator(");

            Assert(controls.Contains("RuntimeUiGui.DrawStandardPanel")
                && controls.Contains("RuntimeUiGui.DrawSlot")
                && controls.Contains("RuntimeUiGui.DrawActionVisual")
                && controls.Contains("RuntimeUiGui.DrawSingleLineText")
                && controls.Contains("if (_game.PlantById(_inspectedPlantId) == null)")
                && controls.Contains("DrawSelectedPlant(layout, drawContext)")
                && !controls.Contains("RuntimeUiGui.DrawText")
                && controls.Contains("RuntimeUiArtSlot.IconToolPot")
                && controls.Contains("BattleUiLayout.ToolRecipeSourceIcon")
                && controls.Contains("BattleUiLayout.ToolRecipeOperator")
                && controls.Contains("BattleUiLayout.ToolRecipeTargetIcon")
                && controls.Contains("BattleUiLayout.ToolInventoryBadge")
                && !controls.Contains("ToolCountLabel")
                && !controls.Contains("PotToolNameLabel")
                && !controls.Contains("PotToolCountLabel")
                && controls.Contains("RuntimeUiArtSlot.IconControlRefresh")
                && controls.Contains("DrawSharedHitTarget")
                && dragGhost.Contains("RuntimeUiGui.DrawStandardPanel")
                && dragGhost.Contains("RuntimeUiGui.DrawSingleLineText")
                && !dragGhost.Contains("RuntimeUiGui.DrawText")
                && dragGhost.Contains("DrawDropCue")
                && source.Contains("SourceRect = rect")
                && dragGhost.Contains("RuntimeUiGui.DrawDragConnector")
                && dragGhost.Contains("RuntimeUiGui.DrawDragTargetFrame")
                && dragGhost.Contains("_drag.SourceRect")
                && dragGhost.Contains("currentTarget.Rect")
                && dragGhost.Contains(
                    "if (_drag == null || !_drag.Active) return;")
                && dragGhost.Contains(
                    "_drag.Type != DragPayloadType.Plant")
                && dragGhost.Contains("Vector2.Lerp(")
                && runtimeGui.Contains(
                    "public static void DrawDragConnector(")
                && runtimeGui.Contains(
                    "public static void DrawDragTargetFrame(")
                && dragConnector.Contains("DragGeometry.ProjectConnector")
                && dragConnector.Contains("GUI.matrix = Matrix4x4.identity")
                && dragConnector.Contains("RuntimeUiArtSlot.SurfaceScrim")
                && dragTargetFrame.Contains(
                    "RuntimeUiArtSlot.SurfaceIllustrationFrame")
                && !dragTargetFrame.Contains("new Rect(")
                && !dragTargetFrame.Contains("RuntimeUiArtSlot.SurfaceScrim")
                && source.Contains("BattleUiPresentationState.DropIndicatorKind")
                && !source.Contains("DropHighlightColor")
                && !source.Contains("IsCurrentDropTarget")
                && !source.Contains("BattleSurface")
                && !source.Contains("ToolTray"),
                "tool/nursery/drag/status slice is not bound to finite shared components");

            var legacyTokens = new[]
            {
                "DrawPanel(", "DrawRect(", "GUI.Label(", "ColoredButton(",
                "GUIStyle.none", "DrawOutline(", "Texture2D.whiteTexture", "GUI.skin",
            };
            for (var index = 0; index < legacyTokens.Length; index++)
            {
                Assert(!controls.Contains(legacyTokens[index])
                    && !dragGhost.Contains(legacyTokens[index]),
                    "tool/nursery/drag slice retained legacy rendering: "
                    + legacyTokens[index]);
            }
        }

        private static void ValidateSharedDetailAndOverlaySource()
        {
            var source = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var detail = MethodSlice(source,
                "private void DrawSelectedPlant(", "private void DrawDragGhost(");
            var overlay = MethodSlice(source,
                "private void DrawOverlay(", "private void RestartRun(");

            Assert(detail.Contains("RuntimeUiGui.DrawDetailCard")
                && detail.Contains("RuntimeUiGui.DrawSingleLineText")
                && detail.Contains("RuntimeUiGui.DrawCompactControlVisual")
                && detail.Contains("RuntimeUiCompactControlVisualSample.Inactive")
                && detail.Contains("TrackBattleAction(")
                && detail.Contains("RuntimeUiArtSlot.IconControlClose")
                && detail.Contains("_inspectedPlantId = -1"),
                "plant-detail slice is not bound to the shared detail component");
            Assert(overlay.Contains("RuntimeUiGui.DrawBlockingModal")
                && overlay.Contains("RuntimeUiGui.DrawResultCard")
                && overlay.Contains("RuntimeUiGui.DrawSectionRibbon")
                && overlay.Contains("RuntimeUiGui.DrawResultBanner")
                && overlay.Contains("RuntimeUiGui.DrawOrchardVista")
                && overlay.Contains("RuntimeUiGui.DrawIndicator")
                && overlay.Contains("RuntimeUiGui.DrawSingleLineText")
                && overlay.Contains("RuntimeUiGui.DrawControlledTwoLineText")
                && overlay.Contains("RuntimeUiGui.DrawAction")
                && overlay.Contains("_game.TogglePause()")
                && overlay.Contains("RestartRun"),
                "pause/result slice is not bound to the shared modal/result components");
            Assert(source.Contains("BlocksBackgroundInput()")
                && source.Contains("IsModalActionTarget(target)")
                && source.Contains("if (BlocksBackgroundInput()) return false;"),
                "blocking modal does not reject background action and hit-target input");

            var legacyTokens = new[]
            {
                "DrawPanel(", "DrawRect(", "GUI.Label(", "ColoredButton(",
                "GUIStyle.none", "Texture2D.whiteTexture", "GUI.skin", "new Rect(",
            };
            for (var index = 0; index < legacyTokens.Length; index++)
            {
                Assert(!detail.Contains(legacyTokens[index])
                    && !overlay.Contains(legacyTokens[index]),
                    "detail/modal/result slice retained legacy rendering: "
                    + legacyTokens[index]);
            }
        }

        private static void ValidateLegacyBattleUiRemoval()
        {
            var source = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var viewportChrome = MethodSlice(source,
                "private void OnGUI(", "private void HandleDragInput(");
            var boardChrome = MethodSlice(source,
                "private void DrawBoard(", "private bool DrawBattlefieldTerrain(");
            var worldRect = MethodSlice(source,
                "private static void DrawWorldRect(",
                "private static void DrawWorldOutline(");
            var projectionSource = File.ReadAllText(
                "Assets/Scripts/Core/BattlefieldProjection.cs");
            var layoutSource = File.ReadAllText(
                "Assets/Scripts/Presentation/BattleUiLayout.cs");

            const string ScreenBackgroundToken = "RuntimeUiGui.DrawScreenBackground";
            var firstScreenBackground = viewportChrome.IndexOf(
                ScreenBackgroundToken, System.StringComparison.Ordinal);
            Assert(firstScreenBackground >= 0
                && viewportChrome.IndexOf(ScreenBackgroundToken,
                    firstScreenBackground + ScreenBackgroundToken.Length,
                    System.StringComparison.Ordinal) < 0
                && viewportChrome.Contains("var outerMatrix = GUI.matrix")
                && viewportChrome.Contains("finally")
                && viewportChrome.Contains("GUI.matrix = outerMatrix")
                && viewportChrome.Contains("new Rect(0f, 0f, Screen.width, Screen.height)")
                && firstScreenBackground < viewportChrome.IndexOf(
                    "GUI.matrix = viewportLayout.GuiMatrix", System.StringComparison.Ordinal)
                && !viewportChrome.Contains("DrawWorldRect")
                && viewportChrome.Contains("DrawPhaseWaveRow(")
                && viewportChrome.Contains("RuntimeUiGui.DrawSafeArea(")
                && viewportChrome.Contains("layout.PageShell")
                && viewportChrome.IndexOf("DrawHeader(", StringComparison.Ordinal)
                    < viewportChrome.IndexOf("RuntimeUiGui.DrawSafeArea(", StringComparison.Ordinal)
                && viewportChrome.IndexOf("RuntimeUiGui.DrawSafeArea(", StringComparison.Ordinal)
                    < viewportChrome.IndexOf("DrawBoard(", StringComparison.Ordinal)
                && viewportChrome.IndexOf("DrawOverlay(", StringComparison.Ordinal)
                    < viewportChrome.IndexOf("RuntimeUiGui.DrawScreenCorners(", StringComparison.Ordinal)
                && viewportChrome.Contains("RuntimeUiGui.DrawGameplayStage")
                && !boardChrome.Contains("RuntimeUiGui.DrawGameplayStage")
                && !boardChrome.Contains("DrawWorldRect"),
                "screen background must draw exactly once in identity viewport space before Battle chrome");

            var removedWaveGeometryTokens = new[]
            {
                "ControlStripRect", "WaveActionRect", "ValidateControlInset",
                "BoardStatus", "BoardStatusWithWaveAction",
            };
            foreach (var token in removedWaveGeometryTokens)
                Assert(!projectionSource.Contains(token)
                    && !layoutSource.Contains(token)
                    && !source.Contains(token),
                    "Battle retained removed in-stage Wave geometry: " + token);

            var removedSimulationViewTokens = new[]
            {
                "plant.Kind", "plant.Weapon", "zombie.Kind",
                "zombie.SlowUntil", "zombie.FreezeUntil", "zombie.IceHits",
                "zombie.Burns", "projectile.Kind", "LegacyBattleContentIds",
                ".ContentId",
            };
            foreach (var token in removedSimulationViewTokens)
                Assert(!source.Contains(token),
                    "Battle presentation reads a removed simulation mirror: " + token);

            var removedTokens = new[]
            {
                "BuildStyles(", "private GUIStyle Style(", "ColoredButton(",
                "private static void DrawPanel(", "private static void DrawRect(",
                "private static void DrawOutline(", "Resources.Load<Font>",
                "Resources.GetBuiltinResource<Font>", "InstallStandaloneCompatibilityHost",
                "StandaloneCompatibilityResultSink",
            };
            for (var index = 0; index < removedTokens.Length; index++)
                Assert(!source.Contains(removedTokens[index]),
                    "Battle retained legacy UI compatibility: " + removedTokens[index]);

            Assert(source.Contains("BuildWorldRenderingStyles")
                && source.Contains("runtimeUiTheme.Typography.For(")
                && source.Contains("RuntimeUiTypographyRole.Body).Font")
                && source.Contains("private void DrawWorldLabel(")
                && source.Contains("private static void DrawWorldRect(")
                && source.Contains("private static void DrawWorldOutline("),
                "world-only rendering helpers were not explicitly narrowed");
            const string WhiteTextureToken = "Texture2D.whiteTexture";
            var whiteTextureIndex = source.IndexOf(
                WhiteTextureToken, StringComparison.Ordinal);
            Assert(whiteTextureIndex >= 0
                && worldRect.Contains(WhiteTextureToken)
                && source.IndexOf(WhiteTextureToken,
                    whiteTextureIndex + WhiteTextureToken.Length,
                    StringComparison.Ordinal) < 0,
                "white texture usage is not isolated to the world pixel renderer");
        }

        private static string MethodSlice(string source, string startToken, string endToken)
        {
            var start = source.IndexOf(startToken, StringComparison.Ordinal);
            Assert(start >= 0,
                "cannot locate shared Battle chrome method boundary: " + startToken);
            var end = source.IndexOf(endToken, start + startToken.Length,
                StringComparison.Ordinal);
            Assert(end > start,
                "cannot locate shared Battle chrome method boundary: " + endToken);
            return source.Substring(start, end - start);
        }

        private static void ValidateBattlefieldContainmentSource(
            BattleUiLayout layout)
        {
            var source = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var runtimeGui = RuntimeUiSourceAuthority.ReadRuntimeGui();
            var viewportChrome = MethodSlice(source,
                "private void OnGUI(", "private void HandleDragInput(");
            var board = MethodSlice(source,
                "private void DrawBoard(", "private bool DrawBattlefieldTerrain(");
            var dragGhost = MethodSlice(source,
                "private void DrawDragGhost(", "private void DrawOverlay(");
            var drawGhostIndex = viewportChrome.IndexOf(
                "DrawDragGhost(", StringComparison.Ordinal);
            var drawStageIndex = viewportChrome.IndexOf(
                "RuntimeUiGui.DrawGameplayStage(", StringComparison.Ordinal);
            var drawOverlayIndex = viewportChrome.IndexOf(
                "DrawOverlay(", StringComparison.Ordinal);
            var connectorIndex = dragGhost.IndexOf(
                "RuntimeUiGui.DrawDragConnector", StringComparison.Ordinal);
            var clipIndex = dragGhost.IndexOf(
                "BeginAbsoluteDesignClip(layout,", StringComparison.Ordinal);
            var targetFrameIndex = dragGhost.IndexOf(
                "RuntimeUiGui.DrawDragTargetFrame", StringComparison.Ordinal);
            var ghostIndex = dragGhost.IndexOf(
                "DrawTempSprite(rect", StringComparison.Ordinal);

            Assert(board.Contains("BeginBattleStageClip(layout)")
                && board.Contains(
                    "RuntimeUiGui.GameplayStageMaskRect(")
                && board.Contains(
                    "BeginAbsoluteDesignClip(layout, maskRect)")
                && board.Contains("finally")
                && board.Contains("EndAbsoluteDesignClip()")
                && board.Contains("EndBattleStageClip()")
                && board.Contains("DrawBattlefieldHitTargets(layout, drawContext)")
                && !board.Contains("RuntimeUiGui.DrawGameplayStage")
                && board.IndexOf("BeginBattleStageClip(layout)", StringComparison.Ordinal)
                    < board.IndexOf("DrawBattlefieldTerrain(maskRect)", StringComparison.Ordinal)
                && board.IndexOf("DrawBattlefieldFlash(layout)", StringComparison.Ordinal)
                    < board.IndexOf("EndAbsoluteDesignClip()", StringComparison.Ordinal)
                && board.IndexOf("EndAbsoluteDesignClip()", StringComparison.Ordinal)
                    < board.IndexOf("DrawBattlefieldHitTargets(layout, drawContext)", StringComparison.Ordinal)
                && board.IndexOf("DrawBattlefieldHitTargets(layout, drawContext)", StringComparison.Ordinal)
                    < board.LastIndexOf("EndBattleStageClip()", StringComparison.Ordinal)
                && source.Contains("GUI.BeginGroup(clipRect)")
                && source.Contains(
                    "absoluteDesign.position = -clipRect.position")
                && CountOccurrences(source,
                    "BeginBattleStageClip(layout)") == 1
                && CountOccurrences(source,
                    "BeginAbsoluteDesignClip(layout,") == 3
                && runtimeGui.Contains(
                    "private const float GameplayStageMaskInset = 8f;")
                && runtimeGui.Contains(
                    "public static Rect GameplayStageMaskRect(")
                && !runtimeGui.Contains(
                    "var safeInset = binding.SafeInset")
                && runtimeGui.Contains("_ClipRectPixels")
                && runtimeGui.Contains("SystemInfo.graphicsUVStartsAtTop")
                && File.ReadAllText(
                    "Assets/UI/RuntimeUiNineSlice.shader").Contains(
                    "clip(input.vertex.x - _ClipRectPixels.x)"),
                "battlefield visuals must use the component-owned 8pt opening clip while hit targets retain the guarded BattleStage clip");
            Assert(drawGhostIndex >= 0 && drawStageIndex > drawGhostIndex
                && drawOverlayIndex > drawStageIndex
                && CountOccurrences(source,
                    "RuntimeUiGui.DrawGameplayStage(") == 1,
                "gameplay-stage frame must be the final stage occluder before blocking overlays");
            var terrainRenderer = File.ReadAllText(
                "Assets/Scripts/Tilemaps/BattlefieldDualGridTerrain.cs");
            var drawTerrain = MethodSlice(source,
                "private bool DrawBattlefieldTerrain(",
                "private void RefreshTerrainPresentationStatus(");
            Assert(drawTerrain.Contains(
                    "BattlefieldTerrainGuiRenderer.DrawBackdrop(")
                && drawTerrain.IndexOf(
                    "BattlefieldTerrainGuiRenderer.DrawBackdrop(",
                    StringComparison.Ordinal)
                    < drawTerrain.IndexOf(
                        "BattlefieldTerrainGuiRenderer.DrawValidated(",
                        StringComparison.Ordinal)
                && terrainRenderer.Contains(
                    "public static void DrawBackdrop(")
                && terrainRenderer.Contains(
                    "map.GridHeight - (backdropRect.yMax - grid.yMin) / tileSize")
                && terrainRenderer.Contains(
                    "GUI.DrawTextureWithTexCoords(backdropRect, texture, uv, true)"),
                "base terrain backdrop must fill the stage mask before the unchanged square grid");
            var imageAnalysis = File.ReadAllText(
                "scripts/webgl-acceptance/image-analysis.ps1");
            var directAcceptance = File.ReadAllText(
                "scripts/webgl-acceptance/run-direct.ps1");
            Assert(imageAnalysis.Contains(
                    "function Get-BattleStageBackdropFitEvidence")
                && imageAnalysis.Contains(
                    "base terrain fills both vertical aspect-ratio gutters")
                && directAcceptance.Contains(
                    "Get-BattleStageBackdropFitEvidence")
                && directAcceptance.Contains(
                    "battleStageVerticalBackdropFit = 'pass'"),
                "WebGL acceptance must verify top and bottom base-terrain gutter coverage");
            Assert(connectorIndex >= 0 && clipIndex > connectorIndex
                && targetFrameIndex > clipIndex && ghostIndex > clipIndex
                && dragGhost.Contains(
                    "currentTarget.Type == DropTargetType.Pot")
                && dragGhost.Contains(
                    "currentTarget.Type == DropTargetType.Plant")
                && dragGhost.Contains(
                    "currentTarget.Type == DropTargetType.Expansion")
                && dragGhost.Contains(
                    "RuntimeUiGui.GameplayStageMaskRect(")
                && dragGhost.Contains(
                    "currentDropCue), stageMaskRect")
                && dragGhost.Contains(
                    "if (clipsToBattleStage) EndAbsoluteDesignClip()"),
                "board-target frame/cue/ghost must be clipped while the connector remains cross-region");

            var releaseTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeUiTheme>(
                "Assets/UI/Theme/ReleaseRuntimeUiTheme.asset");
            Assert(releaseTheme != null && releaseTheme.ActiveArtSet != null,
                "release gameplay-stage binding is unavailable");
            var context = RuntimeUiDrawContext.Create(releaseTheme, 1f);
            var maskRect = RuntimeUiGui.GameplayStageMaskRect(
                context, layout.BattleStage);
            Assert(Approximately(maskRect,
                    new Rect(30f, 176f, 342f, 322f))
                && Contains(layout.BattleStage, maskRect),
                "gameplay-stage component must resolve the seam-free protected 8pt mask rect");
            var pcViewport = BattlefieldProjection.CalculateViewportLayout(
                1280f, 720f, new Rect(0f, 0f, 1280f, 720f),
                BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
            var pcStage = pcViewport.ProjectDesignRect(layout.BattleStage);
            var pcBoard = pcViewport.ProjectDesignRect(layout.Board);
            var pcContent = pcViewport.ProjectDesignRect(maskRect);
            Assert(pcViewport.Scale > 0f && pcViewport.Scale < 1f
                && IsFinite(pcStage.x) && IsFinite(pcStage.y)
                && IsFinite(pcStage.width) && IsFinite(pcStage.height)
                && pcStage.width > 0f && pcStage.height > 0f
                && Approximately(pcStage, pcBoard)
                && Contains(pcStage, pcContent)
                && Contains(pcViewport.DesignViewportRect, pcStage),
                "1280x720 fractional projection must keep clip, content, frame, and hit geometry identical");
        }

        private static int CountOccurrences(string source, string token)
        {
            var count = 0;
            for (var index = 0;;)
            {
                index = source.IndexOf(token, index, StringComparison.Ordinal);
                if (index < 0) return count;
                count++;
                index += token.Length;
            }
        }

        private static bool Approximately(Rect left, Rect right)
        {
            return Approximately(left.position, right.position)
                && Approximately(left.size, right.size);
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Abs(left.x - right.x) <= .001f
                && Mathf.Abs(left.y - right.y) <= .001f;
        }

        private static bool PointOnBoundary(Rect rect, Vector2 point)
        {
            const float tolerance = .001f;
            var contained = point.x >= rect.xMin - tolerance
                && point.x <= rect.xMax + tolerance
                && point.y >= rect.yMin - tolerance
                && point.y <= rect.yMax + tolerance;
            return contained && (Mathf.Abs(point.x - rect.xMin) <= tolerance
                || Mathf.Abs(point.x - rect.xMax) <= tolerance
                || Mathf.Abs(point.y - rect.yMin) <= tolerance
                || Mathf.Abs(point.y - rect.yMax) <= tolerance);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static Rect SnapDeviceRect(Rect rect)
        {
            return Rect.MinMaxRect(Mathf.Round(rect.xMin), Mathf.Round(rect.yMin),
                Mathf.Round(rect.xMax), Mathf.Round(rect.yMax));
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            var tolerance = RuntimeUiQualityProfile.GeometryTolerance;
            return inner.xMin >= outer.xMin - tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Battle UI layout validation failed: " + message);
        }
    }
}
