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
        public const int DefaultNurserySlotCount = 5;
        public const float MergeHintMinimumWidth = 92f;
        public const float MergeHintMaximumWidth = 160f;
        public const float MergeHintHeight = 24f;
        public const float HeaderMetricIconSize = 24f;
        public const float SpacingUnit = 4f;
        public const float ContentInset = SpacingUnit * 2f;

        private const float ToolGap = SpacingUnit * 2f;
        private const float NurseryGap = SpacingUnit * 3f;

        public BattleUiLayout(BattlefieldMapDefinition map,
            int nurserySlotCount = DefaultNurserySlotCount)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (nurserySlotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(nurserySlotCount));
            NurserySlotCount = nurserySlotCount;

            Design = new Rect(0f, 0f, DesignWidth, DesignHeight);
            Header = new Rect(14f, 36f, 374f, 114f);
            PageShell = new Rect(14f, 154f, 374f, 698f);
            BattleStage = new Rect(22f, 168f, 358f, 338f);
            Board = BattleStage;
            PhaseWaveRow = new Rect(24f, 518f, 354f, 52f);
            ContextTray = new Rect(24f, 578f, 354f, 88f);
            NurseryTray = new Rect(24f, 674f, 354f, 92f);
            RefreshAction = new Rect(24f, 774f, 354f, 64f);
            Modal = new Rect(36f, 300f, 330f, 244f);
            TerminalModal = new Rect(28f, 270f, 346f, 320f);

            HeaderTitle = new Rect(40f, 52f, 210f, 38f);
            PauseAction = new Rect(264f, 50f, 48f, 48f);
            SpeedAction = new Rect(318f, 50f, 56f, 48f);
            SunMetric = new Rect(28f, 101f, 112f, 40f);
            LivesMetric = new Rect(145f, 101f, 112f, 40f);
            WaveMetric = new Rect(262f, 101f, 112f, 40f);

            PhaseStatus = PhaseWaveRow;
            PhaseStatusWithWaveAction = new Rect(24f, 518f, 168f, 52f);
            WaveAction = new Rect(204f, 518f, 174f, 52f);

            ContextTrayTitle = new Rect(32f, 582f, 120f, 24f);
            NurseryTrayTitle = new Rect(32f, 678f, 120f, 24f);
            DetailTitle = new Rect(32f, 582f, 290f, 24f);
            DetailBody = new Rect(32f, 614f, 290f, 22f);
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
        }

        public Rect Design { get; }
        public Rect Header { get; }
        public Rect PageShell { get; }
        public Rect BattleStage { get; }
        public Rect Board { get; }
        public Rect PhaseWaveRow { get; }
        public Rect ContextTray { get; }
        public Rect NurseryTray { get; }
        public int NurserySlotCount { get; }
        public Rect RefreshAction { get; }
        public Rect Modal { get; }
        public Rect TerminalModal { get; }

        public Rect HeaderTitle { get; }
        public Rect SunMetric { get; }
        public Rect LivesMetric { get; }
        public Rect WaveMetric { get; }
        public Rect PauseAction { get; }
        public Rect SpeedAction { get; }
        public Rect PhaseStatus { get; }
        public Rect PhaseStatusWithWaveAction { get; }
        public Rect WaveAction { get; }

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
                610f,
                width,
                48f);
        }

        public static Rect ToolRecipeSourceIcon(Rect tool)
        {
            return new Rect(tool.x + 4f, tool.y + 7f, 34f, 34f);
        }

        public static Rect ToolRecipeOperator(Rect tool)
        {
            return new Rect(tool.x + 40f, tool.y + 12f, 12f, 24f);
        }

        public static Rect ToolRecipeTargetIcon(Rect tool)
        {
            return new Rect(tool.xMax - 23f, tool.y + 13f, 20f, 20f);
        }

        public static Rect ToolInventoryBadge(Rect tool)
        {
            return new Rect(tool.x + 2f, tool.y + 24f, 24f, 24f);
        }

        public Rect NurserySlot(int slot)
        {
            var width = (NurseryTray.width - ContentInset * 2f
                - NurseryGap * (NurserySlotCount - 1)) / NurserySlotCount;
            return new Rect(
                NurseryTray.x + ContentInset + slot * (width + NurseryGap),
                706f,
                width,
                52f);
        }

        public static Rect FramelessSlotIcon(Rect slot)
        {
            return Grow(slot, -2f);
        }

        public static Rect NurserySlotLabel(Rect slot)
        {
            return new Rect(slot.x + 2f, slot.y + 6f, slot.width - 4f, 44f);
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

    }
}
