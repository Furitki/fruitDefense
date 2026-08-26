using System;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Presentation
{
    /// <summary>
    /// Immutable logical-space geometry for Battle application chrome. The cached instance owns
    /// the one BattlefieldProjection consumed by both rendering and interaction code.
    /// </summary>
    public sealed class BattleUiLayout
    {
        public const float DesignWidth = 402f;
        public const float DesignHeight = 874f;
        public const int ToolCount = 4;
        public const int NurserySlotCount = 5;
        public const float MergeHintMinimumWidth = 92f;
        public const float MergeHintMaximumWidth = 160f;
        public const float MergeHintHeight = 24f;
        public const float HeaderMetricIconSize = 18f;
        public const float SpacingUnit = 4f;
        public const float ContentInset = SpacingUnit * 2f;
        public const float SectionGap = SpacingUnit * 2f;

        private const float ToolGap = SpacingUnit;
        private const float NurseryGap = SpacingUnit;

        public BattleUiLayout(BattlefieldMapDefinition map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            Design = new Rect(0f, 0f, DesignWidth, DesignHeight);
            Header = FullWidthTrack(8f, 96f);
            BattleStage = FullWidthTrack(108f, 486f);
            Board = BattleStage;
            ContextTray = InsetTrack(602f, 78f);
            NurseryTray = InsetTrack(688f, 88f);
            RefreshAction = InsetTrack(784f, 52f);
            Modal = new Rect(36f, 300f, 330f, 244f);
            TerminalModal = new Rect(28f, 270f, 346f, 320f);

            HeaderTitle = new Rect(16f, 26f, 246f, 24f);
            SunMetric = new Rect(16f, 68f, 118f, 32f);
            LivesMetric = new Rect(142f, 68f, 118f, 32f);
            WaveMetric = new Rect(268f, 68f, 118f, 32f);
            FirstMetricDivider = new Rect(134f, 68f, 8f, 32f);
            SecondMetricDivider = new Rect(260f, 68f, 8f, 32f);
            PauseAction = new Rect(274f, 12f, 52f, 52f);
            SpeedAction = new Rect(334f, 12f, 52f, 52f);

            ContextTrayTitle = new Rect(ContextTray.x + ContentInset,
                ContextTray.y + SpacingUnit, 180f, 22f);
            NurseryTrayTitle = new Rect(NurseryTray.x + ContentInset,
                NurseryTray.y + SpacingUnit, 180f, 22f);
            DetailTitle = new Rect(ContextTray.x + ContentInset,
                ContextTray.y + SpacingUnit, ContextTray.width - 64f, 24f);
            DetailBody = new Rect(ContextTray.x + ContentInset,
                ContextTray.y + 36f, ContextTray.width - 64f, 22f);
            DetailCloseAction = new Rect(ContextTray.xMax - 48f,
                ContextTray.y + 4f, 44f, 44f);

            ModalTitle = new Rect(52f, 326f, 298f, 52f);
            ModalPauseHint = new Rect(60f, 390f, 282f, 52f);
            ModalTerminalTitle = new Rect(48f, 292f, 306f, 56f);
            ModalResultBanner = new Rect(70f, 352f, 262f, 64f);
            ModalResultBannerText = new Rect(102f, 360f, 198f, 48f);
            ModalOrchardVista = new Rect(56f, 424f, 112f, 63f);
            ModalTerminalMessage = new Rect(180f, 420f, 142f, 64f);
            ModalResultIndicator = new Rect(328f, 438f, 24f, 24f);
            TerrainFailurePanel = new Rect(Board.x + 6f, Board.y + 6f,
                Board.width - 12f, Board.height - 12f);

            Battlefield = new BattlefieldProjection(map, Board);
            BoardStatus = Battlefield.ControlStripRect;
            WaveAction = Battlefield.WaveActionRect;
            BoardStatusWithWaveAction = new Rect(
                BoardStatus.x + 8f,
                BoardStatus.y,
                BoardStatus.width - WaveAction.width - 12f,
                BoardStatus.height);
        }

        public Rect Design { get; }
        public Rect Header { get; }
        public Rect BattleStage { get; }
        public Rect Board { get; }
        public Rect ContextTray { get; }
        public Rect NurseryTray { get; }
        public Rect RefreshAction { get; }
        public Rect Modal { get; }
        public Rect TerminalModal { get; }

        public Rect HeaderTitle { get; }
        public Rect SunMetric { get; }
        public Rect LivesMetric { get; }
        public Rect WaveMetric { get; }
        public Rect FirstMetricDivider { get; }
        public Rect SecondMetricDivider { get; }
        public Rect PauseAction { get; }
        public Rect SpeedAction { get; }

        public Rect ContextTrayTitle { get; }
        public Rect NurseryTrayTitle { get; }
        public Rect DetailTitle { get; }
        public Rect DetailBody { get; }
        public Rect DetailCloseAction { get; }
        public Rect ModalTitle { get; }
        public Rect ModalPauseHint { get; }
        public Rect ModalTerminalTitle { get; }
        public Rect ModalResultBanner { get; }
        public Rect ModalResultBannerText { get; }
        public Rect ModalOrchardVista { get; }
        public Rect ModalTerminalMessage { get; }
        public Rect ModalResultIndicator { get; }
        public Rect TerrainFailurePanel { get; }

        public BattlefieldProjection Battlefield { get; }
        public Rect BoardStatus { get; }
        public Rect BoardStatusWithWaveAction { get; }
        public Rect WaveAction { get; }

        public Rect EquipmentTool(string equipmentId)
        {
            var index = BattlePresentationVisualCatalog.EquipmentToolIndex(
                equipmentId);
            if (index < 0)
                throw new ArgumentException(
                    "Unsupported bundled equipment tool ID.", nameof(equipmentId));
            return Tool(index);
        }

        public Rect PotTool => Tool(3);

        public Rect Tool(int index)
        {
            var width = (ContextTray.width - ContentInset * 2f - ToolGap * 3f)
                / ToolCount;
            return new Rect(
                ContextTray.x + ContentInset + index * (width + ToolGap),
                ContextTrayTitle.yMax + SpacingUnit,
                width,
                44f);
        }

        public Rect ToolIcon(Rect tool)
        {
            return new Rect(tool.x + 6f, tool.y + 5f, 36f, 36f);
        }

        public Rect ToolCountLabel(Rect tool)
        {
            return new Rect(tool.x + 43f, tool.y, tool.width - 47f, tool.height);
        }

        public Rect PotToolIcon
        {
            get
            {
                var rect = PotTool;
                var size = rect.height - 2f;
                return new Rect(rect.x + 1f, rect.y + 1f, size, size);
            }
        }

        public Rect PotToolLabel
        {
            get
            {
                var rect = PotTool;
                return new Rect(rect.x + 47f, rect.y,
                    rect.width - 49f, rect.height);
            }
        }

        public Rect PotToolNameLabel
        {
            get
            {
                var rect = PotToolLabel;
                return new Rect(rect.x, rect.y, rect.width, 22f);
            }
        }

        public Rect PotToolCountLabel
        {
            get
            {
                var rect = PotToolLabel;
                return new Rect(rect.x, rect.y + 22f, rect.width, 22f);
            }
        }

        public Rect NurserySlot(int slot)
        {
            var width = (NurseryTray.width - ContentInset * 2f
                - NurseryGap * 4f) / NurserySlotCount;
            return new Rect(
                NurseryTray.x + ContentInset + slot * (width + NurseryGap),
                NurseryTrayTitle.yMax + SpacingUnit,
                width,
                54f);
        }

        public static Rect FramelessSlotIcon(Rect slot)
        {
            return Grow(slot, -2f);
        }

        public static Rect NurserySlotLabel(Rect slot)
        {
            return new Rect(slot.x + 2f, slot.yMax - 24f, slot.width - 4f, 22f);
        }

        public static Rect CueBadge(Rect target)
        {
            const float inset = 2f;
            var available = Mathf.Max(0f,
                Mathf.Min(target.width, target.height) - inset * 2f);
            var size = Mathf.Min(28f, available);
            return new Rect(target.x + inset, target.y + inset, size, size);
        }

        public static Rect CueLabel(Rect target)
        {
            const float gap = SpacingUnit;
            var badge = CueBadge(target);
            var x = Mathf.Min(target.xMax, badge.xMax + gap);
            return new Rect(x, target.y,
                Mathf.Max(0f, target.xMax - gap - x), target.height);
        }

        public Rect ModalAction(int index, int actionCount)
        {
            if (actionCount <= 1) return new Rect(90f, 510f, 222f, 52f);
            return new Rect(index == 0 ? 54f : 206f, 466f, 142f, 52f);
        }

        public Rect ClampDragPreview(Rect preview)
        {
            preview.center = new Vector2(
                Mathf.Clamp(preview.center.x, 24f, DesignWidth - 24f),
                Mathf.Clamp(preview.center.y, 24f, DesignHeight - 24f));
            return preview;
        }

        public Rect MergeHint(Rect dragPreview, float labelWidth)
        {
            var width = Mathf.Clamp(labelWidth + 40f,
                MergeHintMinimumWidth, MergeHintMaximumWidth);
            var x = Mathf.Clamp(dragPreview.center.x - width * .5f,
                8f, DesignWidth - 8f - width);
            return new Rect(x, dragPreview.yMax + 4f, width, MergeHintHeight);
        }

        public static Rect BattlefieldFeedback(Rect gridRect, Vector2 point)
        {
            return BattlefieldFeedback(gridRect, point, 112f, 30f, false);
        }

        public static Rect BattlefieldFeedback(Rect gridRect, Vector2 point,
            float requestedWidth, float requestedHeight, bool belowAnchor)
        {
            var width = Mathf.Min(Mathf.Max(0f, requestedWidth),
                Mathf.Max(0f, gridRect.width));
            var height = Mathf.Min(Mathf.Max(0f, requestedHeight),
                Mathf.Max(0f, gridRect.height));
            var x = Mathf.Clamp(point.x - width * .5f, gridRect.xMin, gridRect.xMax - width);
            var desiredY = belowAnchor ? point.y + 8f : point.y - height - 8f;
            var y = Mathf.Clamp(desiredY, gridRect.yMin, gridRect.yMax - height);
            return new Rect(x, y, width, height);
        }

        private static Rect Grow(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount,
                rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static Rect FullWidthTrack(float y, float height)
        {
            return new Rect(0f, y, DesignWidth, height);
        }

        private static Rect InsetTrack(float y, float height)
        {
            return new Rect(ContentInset, y,
                DesignWidth - ContentInset * 2f, height);
        }
    }
}
