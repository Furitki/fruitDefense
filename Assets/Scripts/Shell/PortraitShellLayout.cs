using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public enum ShellHitTarget
    {
        None,
        Start,
        Retry,
        Return,
    }

    public readonly struct PortraitShellFrame
    {
        public PortraitShellFrame(Rect safeArea, Rect content, Rect header, float scale)
        {
            SafeArea = safeArea;
            Content = content;
            Header = header;
            Scale = scale;
        }

        public Rect SafeArea { get; }
        public Rect Content { get; }
        public Rect Header { get; }
        public float Scale { get; }
    }

    public readonly struct LobbyShellLayout
    {
        public LobbyShellLayout(
            PortraitShellFrame frame,
            Rect title,
            Rect startButton,
            Rect levelCard,
            Rect growthCard,
            Rect settingsCard,
            Rect status)
        {
            Frame = frame;
            Title = title;
            StartButton = startButton;
            LevelCard = levelCard;
            GrowthCard = growthCard;
            SettingsCard = settingsCard;
            Status = status;
        }

        public PortraitShellFrame Frame { get; }
        public Rect Title { get; }
        public Rect StartButton { get; }
        public Rect LevelCard { get; }
        public Rect GrowthCard { get; }
        public Rect SettingsCard { get; }
        public Rect Status { get; }
    }

    public readonly struct SettlementShellLayout
    {
        public SettlementShellLayout(
            PortraitShellFrame frame,
            Rect title,
            Rect resultCard,
            Rect outcome,
            Rect reachedWave,
            Rect remainingLives,
            Rect retryButton,
            Rect returnButton,
            Rect status)
        {
            Frame = frame;
            Title = title;
            ResultCard = resultCard;
            Outcome = outcome;
            ReachedWave = reachedWave;
            RemainingLives = remainingLives;
            RetryButton = retryButton;
            ReturnButton = returnButton;
            Status = status;
        }

        public PortraitShellFrame Frame { get; }
        public Rect Title { get; }
        public Rect ResultCard { get; }
        public Rect Outcome { get; }
        public Rect ReachedWave { get; }
        public Rect RemainingLives { get; }
        public Rect RetryButton { get; }
        public Rect ReturnButton { get; }
        public Rect Status { get; }
    }

    public static class PortraitShellLayout
    {
        public const float ReferenceWidth = 402f;
        public const float ReferenceHeight = 874f;

        public static LobbyShellLayout CreateLobby(float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var frame = CreateFrame(viewportWidth, viewportHeight, safeArea);
            var x = frame.Content.x;
            var width = frame.Content.width;
            var y = frame.Content.y;
            var scale = frame.Scale;

            return new LobbyShellLayout(
                frame,
                RectAt(x, y + 17f * scale, width, 48f * scale),
                RectAt(x, y + 102f * scale, width, 64f * scale),
                RectAt(x, y + 194f * scale, width, 82f * scale),
                RectAt(x, y + 294f * scale, width, 82f * scale),
                RectAt(x, y + 394f * scale, width, 82f * scale),
                RectAt(x, y + 504f * scale, width, 58f * scale));
        }

        public static SettlementShellLayout CreateSettlement(float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var frame = CreateFrame(viewportWidth, viewportHeight, safeArea);
            var x = frame.Content.x;
            var width = frame.Content.width;
            var y = frame.Content.y;
            var scale = frame.Scale;

            var resultCard = RectAt(x, y + 103f * scale, width, 260f * scale);
            return new SettlementShellLayout(
                frame,
                RectAt(x, y + 17f * scale, width, 48f * scale),
                resultCard,
                RectAt(x + 18f * scale, resultCard.y + 22f * scale, width - 36f * scale, 58f * scale),
                RectAt(x + 18f * scale, resultCard.y + 105f * scale, width - 36f * scale, 42f * scale),
                RectAt(x + 18f * scale, resultCard.y + 166f * scale, width - 36f * scale, 42f * scale),
                RectAt(x, y + 399f * scale, width, 64f * scale),
                RectAt(x, y + 481f * scale, width, 58f * scale),
                RectAt(x, y + 557f * scale, width, 58f * scale));
        }

        public static ShellHitTarget HitTest(LobbyShellLayout layout, Vector2 guiPoint, bool isTransitioning)
        {
            if (!isTransitioning && layout.StartButton.Contains(guiPoint)) return ShellHitTarget.Start;
            return ShellHitTarget.None;
        }

        public static ShellHitTarget HitTest(SettlementShellLayout layout, Vector2 guiPoint, bool isTransitioning)
        {
            if (isTransitioning) return ShellHitTarget.None;
            if (layout.RetryButton.Contains(guiPoint)) return ShellHitTarget.Retry;
            if (layout.ReturnButton.Contains(guiPoint)) return ShellHitTarget.Return;
            return ShellHitTarget.None;
        }

        public static Rect ToGuiSafeArea(float viewportHeight, Rect screenSafeArea)
        {
            return new Rect(
                screenSafeArea.x,
                viewportHeight - screenSafeArea.yMax,
                screenSafeArea.width,
                screenSafeArea.height);
        }

        private static PortraitShellFrame CreateFrame(float viewportWidth, float viewportHeight, Rect safeArea)
        {
            if (viewportWidth <= 0f || viewportHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(viewportWidth), "Viewport dimensions must be positive.");

            var viewport = new Rect(0f, 0f, viewportWidth, viewportHeight);
            var screenSafeArea = Intersect(viewport, safeArea);
            if (screenSafeArea.width <= 0f || screenSafeArea.height <= 0f)
                screenSafeArea = viewport;

            var guiSafeArea = ToGuiSafeArea(viewportHeight, screenSafeArea);
            var scale = Mathf.Min(guiSafeArea.width / ReferenceWidth, guiSafeArea.height / ReferenceHeight);
            scale = Mathf.Max(.001f, scale);
            var horizontalMargin = 16f * scale;
            var contentWidth = Mathf.Min(370f * scale, guiSafeArea.width - horizontalMargin * 2f);
            var contentX = guiSafeArea.x + (guiSafeArea.width - contentWidth) * .5f;
            var contentY = guiSafeArea.y + 18f * scale;
            var contentHeight = Mathf.Max(0f, guiSafeArea.yMax - contentY - 18f * scale);
            var content = new Rect(contentX, contentY, contentWidth, contentHeight);
            var header = RectAt(contentX, contentY, contentWidth, 74f * scale);
            return new PortraitShellFrame(guiSafeArea, content, header, scale);
        }

        private static Rect RectAt(float x, float y, float width, float height)
        {
            return new Rect(x, y, Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        private static Rect Intersect(Rect first, Rect second)
        {
            var xMin = Mathf.Max(first.xMin, second.xMin);
            var yMin = Mathf.Max(first.yMin, second.yMin);
            var xMax = Mathf.Min(first.xMax, second.xMax);
            var yMax = Mathf.Min(first.yMax, second.yMax);
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }
    }
}
