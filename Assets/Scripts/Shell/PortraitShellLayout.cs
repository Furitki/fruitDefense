using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public enum ShellHitTarget
    {
        None,
        Start,
        LevelOrchard01,
        LevelOrchard02,
        LevelOrchard03,
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
            Rect orchard01Card,
            Rect orchard02Card,
            Rect orchard03Card,
            Rect startButton,
            Rect status)
        {
            Frame = frame;
            Title = title;
            Orchard01Card = orchard01Card;
            Orchard02Card = orchard02Card;
            Orchard03Card = orchard03Card;
            StartButton = startButton;
            Status = status;
        }

        public PortraitShellFrame Frame { get; }
        public Rect Title { get; }
        public Rect Orchard01Card { get; }
        public Rect Orchard02Card { get; }
        public Rect Orchard03Card { get; }
        public Rect StartButton { get; }
        public Rect Status { get; }

        public Rect LevelCardFor(string levelId)
        {
            switch (levelId)
            {
                case LobbyPresenter.Orchard01LevelId: return Orchard01Card;
                case LobbyPresenter.Orchard02LevelId: return Orchard02Card;
                case LobbyPresenter.Orchard03LevelId: return Orchard03Card;
                default: return default;
            }
        }
    }

    public readonly struct LobbyLevelCardLayout
    {
        public LobbyLevelCardLayout(Rect frame, Rect thumbnail, Rect title, Rect body,
            Rect selectedMarker, Rect transientIndicator)
        {
            Frame = frame;
            Thumbnail = thumbnail;
            Title = title;
            Body = body;
            SelectedMarker = selectedMarker;
            TransientIndicator = transientIndicator;
        }

        public Rect Frame { get; }
        public Rect Thumbnail { get; }
        public Rect Title { get; }
        public Rect Body { get; }
        public Rect SelectedMarker { get; }
        public Rect TransientIndicator { get; }
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
        public const float ReferenceLevelCardHeight = 176f;

        public static LobbyLevelCardLayout CreateLobbyLevelCard(Rect card, float scale)
        {
            if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
                throw new ArgumentOutOfRangeException(nameof(scale),
                    "Lobby level-card scale must be finite and positive.");
            var frame = new Rect(card.x + 4f * scale,
                card.y + 36f * scale,
                Mathf.Min(card.width - 8f * scale, 164f * scale),
                Mathf.Min(card.height - 72f * scale, 104f * scale));
            var thumbnailInset = Mathf.Min(6f * scale,
                Mathf.Min(frame.width, frame.height) * .25f);
            var thumbnail = Inset(frame, thumbnailInset);
            // The selected source uses a 96px canvas at 2x source scale with a
            // ~68-72px alpha box. A 48 logical canvas therefore renders the
            // approved 32-36px optical medallion without stretching its art.
            var markerSize = Mathf.Min(48f * scale,
                Mathf.Min(frame.width, frame.height));
            var marker = new Rect(frame.xMax - markerSize, frame.y,
                markerSize, markerSize);
            var transientSize = Mathf.Min(28f * scale,
                Mathf.Min(frame.width, frame.height));
            var transient = new Rect(frame.xMax - transientSize,
                frame.yMax - transientSize, transientSize, transientSize);
            var textX = Mathf.Min(card.xMax, card.x + 176f * scale);
            var textWidth = Mathf.Max(0f,
                Mathf.Min(190f * scale, card.xMax - textX - 4f * scale));
            return new LobbyLevelCardLayout(frame, thumbnail,
                new Rect(textX, card.y + 34f * scale,
                    textWidth, 44f * scale),
                new Rect(textX, card.y + 90f * scale,
                    textWidth, 44f * scale),
                marker, transient);
        }

        public static LobbyShellLayout CreateLobby(float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var frame = CreateFrame(viewportWidth, viewportHeight, safeArea);
            var x = frame.Content.x;
            var width = frame.Content.width;
            var y = frame.Content.y;
            var scale = frame.Scale;

            return new LobbyShellLayout(
                frame,
                RectAt(x, y + 24f * scale, width, 64f * scale),
                RectAt(x, y + 100f * scale, width, ReferenceLevelCardHeight * scale),
                RectAt(x, y + 286f * scale, width, ReferenceLevelCardHeight * scale),
                RectAt(x, y + 472f * scale, width, ReferenceLevelCardHeight * scale),
                RectAt(x, y + 690f * scale, width, 76f * scale),
                RectAt(x, y + 772f * scale, width, 60f * scale));
        }

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

        public static ShellHitTarget HitTest(LobbyShellLayout layout, Vector2 guiPoint, bool isTransitioning)
        {
            if (isTransitioning) return ShellHitTarget.None;
            if (layout.Orchard01Card.Contains(guiPoint)) return ShellHitTarget.LevelOrchard01;
            if (layout.Orchard02Card.Contains(guiPoint)) return ShellHitTarget.LevelOrchard02;
            if (layout.Orchard03Card.Contains(guiPoint)) return ShellHitTarget.LevelOrchard03;
            if (layout.StartButton.Contains(guiPoint)) return ShellHitTarget.Start;
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

        private static Rect Inset(Rect rect, float inset)
        {
            var value = Mathf.Min(Mathf.Max(0f, inset),
                Mathf.Min(Mathf.Max(0f, rect.width), Mathf.Max(0f, rect.height)) * .5f);
            return new Rect(rect.x + value, rect.y + value,
                Mathf.Max(0f, rect.width - value * 2f),
                Mathf.Max(0f, rect.height - value * 2f));
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
