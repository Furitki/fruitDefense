using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public enum ShellHitTarget
    {
        None,
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

    public readonly struct SettlementShellLayout
    {
        public SettlementShellLayout(
            PortraitShellFrame frame,
            Rect title,
            Rect resultCard,
            Rect outcome,
            Rect completedLevel,
            Rect reachedWave,
            Rect remainingLives,
            Rect resultBanner,
            Rect orchardVista,
            Rect resultIndicator,
            Rect retryButton,
            Rect returnButton,
            Rect status)
        {
            Frame = frame;
            Title = title;
            ResultCard = resultCard;
            Outcome = outcome;
            CompletedLevel = completedLevel;
            ReachedWave = reachedWave;
            RemainingLives = remainingLives;
            ResultBanner = resultBanner;
            OrchardVista = orchardVista;
            ResultIndicator = resultIndicator;
            RetryButton = retryButton;
            ReturnButton = returnButton;
            Status = status;
        }

        public PortraitShellFrame Frame { get; }
        public Rect Title { get; }
        public Rect ResultCard { get; }
        public Rect Outcome { get; }
        public Rect CompletedLevel { get; }
        public Rect ReachedWave { get; }
        public Rect RemainingLives { get; }
        public Rect ResultBanner { get; }
        public Rect OrchardVista { get; }
        public Rect ResultIndicator { get; }
        public Rect RetryButton { get; }
        public Rect ReturnButton { get; }
        public Rect Status { get; }
    }

    public static class PortraitShellLayout
    {
        public const float ReferenceWidth = 402f;
        public const float ReferenceHeight = 874f;
        public static SettlementShellLayout CreateSettlement(float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var frame = CreateFrame(viewportWidth, viewportHeight, safeArea);
            var x = frame.Content.x;
            var width = frame.Content.width;
            var y = frame.Content.y;
            var scale = frame.Scale;

            var resultCard = RectAt(x, y + 102f * scale, width, 502f * scale);
            var metricX = x + 16f * scale;
            var metricWidth = 338f * scale;
            // ResultBanner is the intended significant-alpha envelope, not the
            // ornament's transparent 256x72 canvas. RuntimeUiGui expands the
            // complete raster around this 330x48 visible target.
            var resultBanner = RectAt(x + 20f * scale, y + 132f * scale,
                330f * scale, 48f * scale);
            return new SettlementShellLayout(
                frame,
                RectAt(x, y + 24f * scale, width, 64f * scale),
                resultCard,
                RectAt(x + 82f * scale, y + 136f * scale,
                    206f * scale, 40f * scale),
                RectAt(metricX, y + 432f * scale, metricWidth, 48f * scale),
                RectAt(metricX, y + 488f * scale, metricWidth, 48f * scale),
                RectAt(metricX, y + 544f * scale, metricWidth, 48f * scale),
                resultBanner,
                RectAt(x + 16f * scale, y + 204f * scale,
                    338f * scale, 216f * scale),
                RectAt(x + 292f * scale, y + 142f * scale,
                    28f * scale, 28f * scale),
                RectAt(x, y + 620f * scale, width, 72f * scale),
                RectAt(x, y + 704f * scale, width, 64f * scale),
                RectAt(x, y + 780f * scale, width, 58f * scale));
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

        public static PortraitShellFrame CreateFrame(float viewportWidth, float viewportHeight, Rect safeArea)
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
            var designHeight = ReferenceHeight * scale;
            var verticalLetterbox = Mathf.Max(0f,
                (guiSafeArea.height - designHeight) * .5f);
            var contentY = guiSafeArea.y + verticalLetterbox + 18f * scale;
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
