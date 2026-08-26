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
            ValidateFinitePresentationState();
            ValidateBetweenWaveSingleLineText(layout);
            ValidateTransientStatusText(layout);
            ValidateSharedChromeSource();
            ValidateSharedControlSource();
            ValidateSharedDetailAndOverlaySource();
            ValidateLegacyBattleUiRemoval();
            Debug.Log("FRUIT_DEFENSE_BATTLE_UI_LAYOUT_OK");
        }

        private static void ValidateReferenceGeometry(BattleUiLayout layout)
        {
            Assert(Approximately(layout.Design, new Rect(0f, 0f, 402f, 874f)),
                "design geometry changed");
            Assert(Approximately(layout.Header, new Rect(0f, 8f, 402f, 96f))
                && Approximately(layout.BattleStage, new Rect(0f, 108f, 402f, 486f))
                && Approximately(layout.Board, layout.BattleStage),
                "top-level Battle chrome changed");
            Assert(Approximately(layout.ContextTray, new Rect(8f, 602f, 386f, 78f))
                && Approximately(layout.NurseryTray, new Rect(8f, 688f, 386f, 88f))
                && Approximately(layout.RefreshAction, new Rect(8f, 784f, 386f, 52f))
                && Mathf.Approximately(layout.Design.yMax - layout.RefreshAction.yMax, 38f),
                "context/nursery/refresh closeout geometry changed");
            Assert(Approximately(layout.PauseAction, new Rect(274f, 12f, 52f, 52f))
                && Approximately(layout.SpeedAction, new Rect(334f, 12f, 52f, 52f))
                && Approximately(layout.HeaderTitle, new Rect(16f, 26f, 246f, 24f))
                && Approximately(layout.SunMetric, new Rect(16f, 68f, 118f, 32f))
                && Approximately(layout.LivesMetric, new Rect(142f, 68f, 118f, 32f))
                && Approximately(layout.WaveMetric, new Rect(268f, 68f, 118f, 32f))
                && Approximately(layout.FirstMetricDivider, new Rect(134f, 68f, 8f, 32f))
                && Approximately(layout.SecondMetricDivider, new Rect(260f, 68f, 8f, 32f)),
                "header action targets changed");
            Assert(Approximately(layout.BoardStatus, new Rect(8f, 544f, 386f, 48f))
                && Approximately(layout.BoardStatusWithWaveAction,
                    new Rect(16f, 544f, 190f, 48f))
                && Approximately(layout.WaveAction, new Rect(210f, 548f, 184f, 44f)),
                "board status or wave target changed");
            Assert(Approximately(layout.ContextTrayTitle, new Rect(16f, 606f, 180f, 22f))
                && Approximately(layout.NurseryTrayTitle, new Rect(16f, 692f, 180f, 22f))
                && Approximately(layout.Tool(0), new Rect(16f, 632f, 89.5f, 44f))
                && Approximately(layout.Tool(3), new Rect(296.5f, 632f, 89.5f, 44f))
                && Approximately(layout.NurserySlot(0), new Rect(16f, 718f, 70.8f, 54f))
                && Approximately(layout.NurserySlot(4), new Rect(315.2f, 718f, 70.8f, 54f)),
                "tool or nursery cell geometry changed");
            Assert(Approximately(layout.DetailTitle, new Rect(16f, 606f, 322f, 24f))
                && Approximately(layout.DetailBody, new Rect(16f, 638f, 322f, 22f))
                && Approximately(layout.DetailCloseAction, new Rect(346f, 606f, 44f, 44f))
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
                    new Rect(0f, 108f, 402f, 424f))
                && Approximately(layout.Battlefield.GridRect,
                    new Rect(8f, 151.125f, 386f, 337.75f)),
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
                && Mathf.Approximately(left, 8f)
                && Mathf.Approximately(top, 43.125f),
                "Battlefield grid has symmetric visual gutters relative to MapViewportRect");
        }

        private static void ValidateNamedTracksAndRhythm(BattleUiLayout layout)
        {
            Assert(Mathf.Approximately(BattleUiLayout.SpacingUnit, 4f)
                && Mathf.Approximately(BattleUiLayout.ContentInset, 8f)
                && Mathf.Approximately(BattleUiLayout.SectionGap, 8f),
                "Battle named tracks derive from the four-point spacing unit");
            Assert(Mathf.Approximately(layout.Header.xMin, layout.BattleStage.xMin)
                && Mathf.Approximately(layout.Header.xMax, layout.BattleStage.xMax)
                && Mathf.Approximately(layout.BattleStage.yMin - layout.Header.yMax,
                    BattleUiLayout.SpacingUnit),
                "Header and BattleStage share one full-width track with one four-point gap");

            var sections = new[]
            {
                layout.ContextTray, layout.NurseryTray, layout.RefreshAction,
            };
            for (var index = 0; index < sections.Length; index++)
            {
                Assert(Mathf.Approximately(sections[index].xMin,
                        layout.BattleStage.xMin + BattleUiLayout.ContentInset)
                    && Mathf.Approximately(sections[index].xMax,
                        layout.BattleStage.xMax - BattleUiLayout.ContentInset),
                    "Battle control section uses the shared inset track: " + index);
            }
            Assert(Mathf.Approximately(layout.ContextTray.yMin - layout.Board.yMax,
                       BattleUiLayout.SectionGap)
                && Mathf.Approximately(layout.NurseryTray.yMin - layout.ContextTray.yMax,
                    BattleUiLayout.SectionGap)
                && Mathf.Approximately(layout.RefreshAction.yMin - layout.NurseryTray.yMax,
                    BattleUiLayout.SectionGap),
                "Battle sections preserve the named eight-point rhythm");

            Assert(layout.ContextTrayTitle.height == 22f
                && layout.NurseryTrayTitle.height == 22f
                && BattleUiLayout.NurserySlotLabel(layout.NurserySlot(0)).height == 22f
                && layout.DetailTitle.height == 24f
                && layout.DetailBody.height == 22f
                && layout.HeaderTitle.height == 24f
                && layout.SunMetric.height == 32f
                && layout.LivesMetric.height == 32f
                && layout.WaveMetric.height == 32f,
                "Battle text owners expose complete semantic line-height boxes");
            Assert(Mathf.Approximately(layout.ContextTrayTitle.yMin - layout.ContextTray.yMin,
                       BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.Tool(0).yMin - layout.ContextTrayTitle.yMax,
                    BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.ContextTray.yMax - layout.Tool(0).yMax,
                    BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.NurseryTrayTitle.yMin - layout.NurseryTray.yMin,
                    BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.NurserySlot(0).yMin
                    - layout.NurseryTrayTitle.yMax, BattleUiLayout.SpacingUnit)
                && Mathf.Approximately(layout.NurseryTray.yMax
                    - layout.NurserySlot(0).yMax, BattleUiLayout.SpacingUnit),
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
            var surface = SnapDeviceRect(
                projection.ProjectDesignRect(layout.BattleStage));
            Assert(Mathf.Approximately(header.xMin, surface.xMin)
                && Mathf.Approximately(header.xMax, surface.xMax),
                caseName + " preserves peer-frame device edges");

            var expectedGap = Mathf.Round(
                BattleUiLayout.SpacingUnit * projection.Scale);
            Assert(Mathf.Abs(surface.yMin - header.yMax - expectedGap) <= 1f,
                caseName + " preserves the four-point top-level gap after snapping");

            var projectedBoard = projection.ProjectDesignRect(layout.Board);
            Assert(Approximately(projectedBoard,
                    projection.ProjectDesignRect(layout.Battlefield.BoardRect))
                && Contains(surface, SnapDeviceRect(projectedBoard))
                && Contains(SnapDeviceRect(projectedBoard), SnapDeviceRect(
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
                24f, 22f, 22f, 22f, 22f, 22f, 22f, 24f, 22f,
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
                && Contains(layout.Header, layout.FirstMetricDivider)
                && Contains(layout.Header, layout.SecondMetricDivider),
                "header targets leave the header");

            Rect? previous = null;
            for (var index = 0; index < BattleUiLayout.ToolCount; index++)
            {
                var rect = layout.Tool(index);
                Assert(Contains(layout.ContextTray, rect) && Mathf.Min(rect.width, rect.height) >= 44f,
                    "tool target is clipped or undersized: " + index);
                Assert(!previous.HasValue || !previous.Value.Overlaps(rect),
                    "tool targets overlap: " + index);
                previous = rect;
            }

            previous = null;
            for (var slot = 0; slot < BattleUiLayout.NurserySlotCount; slot++)
            {
                var rect = layout.NurserySlot(slot);
                Assert(Contains(layout.NurseryTray, rect) && Mathf.Min(rect.width, rect.height) >= 44f,
                    "nursery target is clipped or undersized: " + slot);
                Assert(!previous.HasValue || !previous.Value.Overlaps(rect),
                    "nursery targets overlap: " + slot);
                previous = rect;
            }

            Assert(Approximately(layout.BattleStage, layout.Board)
                && Contains(layout.ContextTray, layout.Tool(0))
                && Contains(layout.ContextTray, layout.DetailTitle)
                && Contains(layout.ContextTray, layout.DetailBody)
                && Contains(layout.ContextTray, layout.DetailCloseAction)
                && !layout.ContextTray.Overlaps(layout.NurseryTray)
                && !layout.NurseryTray.Overlaps(layout.RefreshAction),
                "stage and mutually exclusive context anatomy lost authority");
            Assert(layout.Battlefield.ValidateControlInset(out var controlReason),
                "wave target crossed battlefield interaction geometry: " + controlReason);

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
                    new Rect(18f, 634f, 28f, 28f))
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
                && ready.StatusInteractionState == RuntimeUiInteractionState.Normal,
                "ready presentation state changed");
            Assert(active.Mode == BattleUiChromeMode.ActiveWave && !active.ShowsWaveAction
                && active.BoardStatusText(3, 7, 0f) == "第 3 波 · 7 个敌人",
                "active-wave presentation state changed");
            Assert(between.Mode == BattleUiChromeMode.BetweenWaves && between.ShowsWaveAction
                && between.WaveActionLabel == "立即开始下一波"
                && between.BoardStatusText(1, 0, 9.5f) == "下一波倒计时 10 秒"
                && between.StatusInteractionState == RuntimeUiInteractionState.Warning,
                "between-wave presentation state changed");
            Assert(paused.Mode == BattleUiChromeMode.Paused && paused.BlocksDrag
                && paused.ModalActionCount == 2
                && paused.PauseActionIcon == RuntimeUiArtSlot.IconControlContinue
                && paused.BoardMode == BattleUiBoardMode.ActiveWave
                && paused.BoardStatusText(3, 7, 0f) == "第 3 波 · 7 个敌人"
                && pausedModal.SurfaceState == RuntimeUiInteractionState.Warning
                && !pausedModal.UsesResultCard
                && pausedModal.PrimaryActionKind == RuntimeUiActionKind.Primary
                && pausedModal.PrimaryActionIcon == RuntimeUiArtSlot.IconControlContinue
                && pausedModal.SecondaryActionKind == RuntimeUiActionKind.Danger
                && pausedModal.SecondaryActionIcon == RuntimeUiArtSlot.IconControlRetry,
                "paused presentation state changed");
            Assert(victory.Mode == BattleUiChromeMode.Victory && victory.BlocksDrag
                && victory.ModalActionCount == 1
                && victory.StatusInteractionState == RuntimeUiInteractionState.Success
                && victoryModal.MessageLines.FirstLine == "成功抵御全部"
                && victoryModal.MessageLines.SecondLine == "10 波僵尸"
                && victoryModal.SurfaceState == RuntimeUiInteractionState.Success
                && victoryModal.UsesResultCard
                && victoryModal.ResultBannerText == "胜利"
                && victoryModal.PrimaryActionKind == RuntimeUiActionKind.Primary
                && victoryModal.PrimaryActionIcon == RuntimeUiArtSlot.IconControlRetry,
                "victory presentation or terminal precedence changed");
            Assert(defeat.Mode == BattleUiChromeMode.Defeat && defeat.BlocksDrag
                && defeat.StatusInteractionState == RuntimeUiInteractionState.Error
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

        private static void ValidateBetweenWaveSingleLineText(BattleUiLayout layout)
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            Assert(theme.PackagedChineseFont != null,
                "release theme has no packaged Chinese font");
            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var actionStyle = context.Styles.SingleLineText(
                RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleCenter);
            var statusLayout = RuntimeUiGui.ResolveStatusTextLayout(context,
                layout.BoardStatusWithWaveAction, RuntimeUiInteractionState.Warning,
                RuntimeUiTypographyRole.Supplemental,
                RuntimeUiStatusTextMode.SingleLine);
            var statusStyle = statusLayout.Style;
            Assert(ReferenceEquals(actionStyle.font, theme.PackagedChineseFont)
                && ReferenceEquals(statusStyle.font, theme.PackagedChineseFont)
                && !actionStyle.wordWrap && !statusStyle.wordWrap
                && actionStyle.clipping == TextClipping.Clip
                && statusStyle.clipping == TextClipping.Clip,
                "between-wave single-line styles must use the release Noto font without wrapping");

            var between = BattleUiPresentationState.Create(GamePhase.BetweenWaves, false);
            var actionText = between.WaveActionLabel;
            var statusNineSeconds = between.BoardStatusText(1, 0, 8.5f);
            var statusTenSeconds = between.BoardStatusText(1, 0, 9.5f);
            Assert(actionText == "立即开始下一波"
                && statusNineSeconds == "下一波倒计时 9 秒"
                && statusTenSeconds == "下一波倒计时 10 秒",
                "between-wave product copy changed");

            var actionContentHeight = layout.WaveAction.height
                - theme.Metrics.SpacingSm * 2f;
            var actionIconWidth = Mathf.Min(actionContentHeight,
                theme.Metrics.TouchTargetMinimum);
            var actionContentSize = new Vector2(
                layout.WaveAction.width - theme.Metrics.SpacingSm * 2f
                    - actionIconWidth - theme.Metrics.SpacingXs,
                actionContentHeight);
            var statusContentSize = statusLayout.FirstLineRect.size;

            var full = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 0f, 402f, 874f), 402f, 874f);
            var inset = BattlefieldProjection.CalculateViewportLayout(
                402f, 874f, new Rect(0f, 34f, 402f, 796f), 402f, 874f);
            AssertSingleLineFits(actionStyle, actionText, actionContentSize,
                full.Scale, "402 full wave action");
            AssertSingleLineFits(statusStyle, statusNineSeconds, statusContentSize,
                full.Scale, "402 full nine-second status");
            AssertSingleLineFits(statusStyle, statusTenSeconds, statusContentSize,
                full.Scale, "402 full ten-second status");
            AssertSingleLineFits(actionStyle, actionText, actionContentSize,
                inset.Scale, "402 inset wave action");
            AssertSingleLineFits(statusStyle, statusNineSeconds, statusContentSize,
                inset.Scale, "402 inset nine-second status");
            AssertSingleLineFits(statusStyle, statusTenSeconds, statusContentSize,
                inset.Scale, "402 inset ten-second status");

            Assert(Approximately(layout.BoardStatusWithWaveAction,
                    new Rect(16f, 544f, 190f, 48f))
                && Approximately(layout.WaveAction, new Rect(210f, 548f, 184f, 44f)),
                "between-wave draw/hit rectangles changed while fixing text clipping");
        }

        private static void ValidateTransientStatusText(BattleUiLayout layout)
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var statusRect = layout.BoardStatusWithWaveAction;
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
                Assert(textLayout.Style.font == theme.PackagedChineseFont
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
            Assert(Approximately(statusRect, new Rect(16f, 544f, 190f, 48f))
                    && Approximately(layout.WaveAction, new Rect(210f, 548f, 184f, 44f)),
                "transient status fix changed board status or wave-action draw/hit geometry");
        }

        private static void AssertSingleLineFits(GUIStyle style, string text,
            Vector2 availableLogicalSize, float viewportScale, string caseName)
        {
            var content = new GUIContent(text);
            var measured = style.CalcSize(content);
            var calculatedHeight = style.CalcHeight(content, availableLogicalSize.x);
            var measuredPixels = measured * viewportScale;
            var availablePixels = availableLogicalSize * viewportScale;
            Assert(measuredPixels.x <= availablePixels.x + .001f
                && calculatedHeight * viewportScale <= availablePixels.y + .001f,
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
                "private void DrawBoardStatus(", "private void DrawEmbeddedBattleControls(");

            Assert(header.Contains("RuntimeUiGui.DrawStandardPanel")
                && header.Contains("RuntimeUiGui.DrawMetric")
                && header.Contains("RuntimeUiGui.DrawMetricDivider")
                && header.Contains("TrackBattleAction(")
                && header.Contains("RuntimeUiGui.DrawCompactControlVisual")
                && !header.Contains("RuntimeUiActionKind.Quiet")
                && header.Contains("_game.TogglePause()")
                && header.Contains("_game.SetSpeed(")
                && status.Contains("RuntimeUiGui.DrawStatus")
                && status.Contains("TrackBattleAction(")
                && status.Contains("RuntimeUiGui.DrawActionVisual")
                && status.Contains("PrepareTransientStatusText")
                && status.Contains("RuntimeUiStatusTextMode.SingleLine")
                && status.Contains("RuntimeUiArtSlot.IconControlStartWave")
                && status.Contains("_game.StartWave(out var reason)")
                && source.Contains("_actionPressTracker.Update")
                && source.Contains("RuntimeUiGui.ResolveStatusTextMode")
                && source.Contains("RuntimeUiGui.ResolveStatusTextLines")
                && source.Contains("_runtimeUiDrawContext = RuntimeUiGui.RequireContext"),
                "header/status slice is not bound to the cached shared visual system");

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
                    "header/status slice retained legacy rendering: " + legacyTokens[index]);
            }
        }

        private static void ValidateSharedControlSource()
        {
            var source = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var controls = MethodSlice(source,
                "private void DrawEmbeddedBattleControls(",
                "private void RefreshNurseryFromUi(");
            var dragGhost = MethodSlice(source,
                "private void DrawDragGhost(", "private void DrawOverlay(");

            Assert(controls.Contains("RuntimeUiGui.DrawStandardPanel")
                && controls.Contains("RuntimeUiGui.DrawSlot")
                && controls.Contains("RuntimeUiGui.DrawActionVisual")
                && controls.Contains("RuntimeUiGui.DrawSingleLineText")
                && controls.Contains("if (_game.PlantById(_inspectedPlantId) == null)")
                && controls.Contains("DrawSelectedPlant(layout, drawContext)")
                && !controls.Contains("RuntimeUiGui.DrawText")
                && controls.Contains("RuntimeUiArtSlot.IconToolPot")
                && controls.Contains("RuntimeUiArtSlot.IconControlRefresh")
                && controls.Contains("DrawSharedHitTarget")
                && dragGhost.Contains("RuntimeUiGui.DrawStandardPanel")
                && dragGhost.Contains("RuntimeUiGui.DrawSingleLineText")
                && !dragGhost.Contains("RuntimeUiGui.DrawText")
                && dragGhost.Contains("DrawDropCue")
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
                && boardChrome.Contains("RuntimeUiGui.DrawGameplayStage")
                && !boardChrome.Contains("DrawWorldRect"),
                "screen background must draw exactly once in identity viewport space before Battle chrome");

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
                && source.Contains("runtimeUiTheme.PackagedChineseFont")
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
