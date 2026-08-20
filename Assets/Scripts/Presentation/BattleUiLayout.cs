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
        public const float HeaderMetricIconSize = 13f;

        private const float ToolGap = 5f;
        private const float NurseryGap = 5f;

        public BattleUiLayout(BattlefieldMapDefinition map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            Design = new Rect(0f, 0f, DesignWidth, DesignHeight);
            Header = new Rect(8f, 8f, 386f, 60f);
            BattleSurface = new Rect(0f, 72f, 402f, 798f);
            Board = new Rect(0f, 72f, 402f, 500f);
            ToolTray = new Rect(8f, 580f, 386f, 68f);
            NurseryTray = new Rect(8f, 656f, 386f, 80f);
            RefreshAction = new Rect(8f, 744f, 386f, 44f);
            Detail = new Rect(8f, 796f, 386f, 70f);
            Modal = new Rect(36f, 300f, 330f, 244f);
            TerminalModal = new Rect(28f, 270f, 346f, 320f);

            HeaderTitle = new Rect(16f, 12f, 96f, 20f);
            SunMetric = new Rect(16f, 36f, 82f, 26f);
            LivesMetric = new Rect(106f, 36f, 76f, 26f);
            WaveMetric = new Rect(190f, 36f, 72f, 26f);
            FirstMetricDivider = new Rect(98f, 41f, 8f, 16f);
            SecondMetricDivider = new Rect(182f, 41f, 8f, 16f);
            PauseAction = new Rect(274f, 16f, 52f, 44f);
            SpeedAction = new Rect(334f, 16f, 52f, 44f);

            ToolTrayTitle = new Rect(ToolTray.x + 8f, ToolTray.y + 4f, 180f, 16f);
            NurseryTrayTitle = new Rect(NurseryTray.x + 8f, NurseryTray.y + 4f, 180f, 16f);
            DetailTitle = new Rect(Detail.x + 8f, Detail.y + 4f, Detail.width - 64f, 22f);
            DetailBody = new Rect(Detail.x + 8f, Detail.y + 34f, Detail.width - 64f, 28f);
            DetailCloseAction = new Rect(Detail.xMax - 48f, Detail.y + 4f, 44f, 44f);

            ModalTitle = new Rect(52f, 326f, 298f, 52f);
            ModalMessage = new Rect(92f, 390f, 250f, 52f);
            ModalPauseIndicator = new Rect(60f, 404f, 24f, 24f);
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
                BoardStatus.width - WaveAction.width - 16f,
                BoardStatus.height);
        }

        public Rect Design { get; }
        public Rect Header { get; }
        public Rect BattleSurface { get; }
        public Rect Board { get; }
        public Rect ToolTray { get; }
        public Rect NurseryTray { get; }
        public Rect RefreshAction { get; }
        public Rect Detail { get; }
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

        public Rect ToolTrayTitle { get; }
        public Rect NurseryTrayTitle { get; }
        public Rect DetailTitle { get; }
        public Rect DetailBody { get; }
        public Rect DetailCloseAction { get; }
        public Rect ModalTitle { get; }
        public Rect ModalMessage { get; }
        public Rect ModalPauseIndicator { get; }
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

        public Rect WeaponTool(WeaponKind weapon)
        {
            var index = weapon == WeaponKind.Gatling ? 0 : weapon == WeaponKind.Ice ? 1 : 2;
            return Tool(index);
        }

        public Rect PotTool => Tool(3);

        public Rect Tool(int index)
        {
            var width = (ToolTray.width - 16f - ToolGap * 3f) / ToolCount;
            return new Rect(
                ToolTray.x + 8f + index * (width + ToolGap),
                ToolTray.y + 24f,
                width,
                44f);
        }

        public Rect ToolIcon(Rect tool)
        {
            return new Rect(tool.x + 6f, tool.y + 5f, 36f, 36f);
        }

        public Rect ToolCountLabel(Rect tool)
        {
            return new Rect(tool.x + 43f, tool.y + 3f, 41f, 44f);
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
                return new Rect(rect.x + 47f, rect.y + 3f, rect.width - 49f, 44f);
            }
        }

        public Rect NurserySlot(int slot)
        {
            var width = (NurseryTray.width - 16f - NurseryGap * 4f) / NurserySlotCount;
            return new Rect(
                NurseryTray.x + 8f + slot * (width + NurseryGap),
                NurseryTray.y + 24f,
                width,
                54f);
        }

        public static Rect FramelessSlotIcon(Rect slot)
        {
            return Grow(slot, -2f);
        }

        public static Rect NurserySlotLabel(Rect slot)
        {
            return new Rect(slot.x + 2f, slot.yMax - 18f, slot.width - 4f, 16f);
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
            const float gap = 2f;
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
            var width = Mathf.Clamp(labelWidth + 20f,
                MergeHintMinimumWidth, MergeHintMaximumWidth);
            var x = Mathf.Clamp(dragPreview.center.x - width * .5f,
                8f, DesignWidth - 8f - width);
            return new Rect(x, dragPreview.yMax + 4f, width, MergeHintHeight);
        }

        public static Rect BattlefieldFeedback(Rect gridRect, Vector2 point)
        {
            var width = Mathf.Min(90f, Mathf.Max(0f, gridRect.width));
            var height = Mathf.Min(18f, Mathf.Max(0f, gridRect.height));
            var x = Mathf.Clamp(point.x - width * .5f, gridRect.xMin, gridRect.xMax - width);
            var y = Mathf.Clamp(point.y - 28f, gridRect.yMin, gridRect.yMax - height);
            return new Rect(x, y, width, height);
        }

        private static Rect Grow(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount,
                rect.width + amount * 2f, rect.height + amount * 2f);
        }
    }
}
