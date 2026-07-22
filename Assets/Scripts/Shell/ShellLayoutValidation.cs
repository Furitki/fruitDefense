using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public static class ShellLayoutValidation
    {
        private readonly struct ViewportCase
        {
            public ViewportCase(float width, float height, float safeBottom, float safeTop)
            {
                Width = width;
                Height = height;
                Full = new Rect(0f, 0f, width, height);
                Inset = new Rect(0f, safeBottom, width, height - safeBottom - safeTop);
            }

            public float Width { get; }
            public float Height { get; }
            public Rect Full { get; }
            public Rect Inset { get; }
        }

        public static void SmokeValidate()
        {
            ValidateReferenceGeometry();
            Debug.Log("FRUIT_DEFENSE_SHELL_LAYOUT_OK");
        }

        public static void ValidateReferenceGeometry()
        {
            var cases = new[]
            {
                new ViewportCase(360f, 800f, 24f, 32f),
                new ViewportCase(375f, 812f, 21f, 40f),
                new ViewportCase(402f, 874f, 34f, 44f),
                new ViewportCase(430f, 932f, 36f, 50f),
            };

            foreach (var viewportCase in cases)
            {
                ValidateLobby(viewportCase.Width, viewportCase.Height, viewportCase.Full, false);
                ValidateLobby(viewportCase.Width, viewportCase.Height, viewportCase.Inset, true);
                ValidateSettlement(viewportCase.Width, viewportCase.Height, viewportCase.Full);
                ValidateSettlement(viewportCase.Width, viewportCase.Height, viewportCase.Inset);
            }

            var referenceSafeArea = new Rect(0f, 0f,
                PortraitShellLayout.ReferenceWidth, PortraitShellLayout.ReferenceHeight);
            var referenceA = PortraitShellLayout.CreateLobby(402f, 874f, referenceSafeArea);
            var referenceB = PortraitShellLayout.CreateLobby(402f, 874f, referenceSafeArea);
            Assert(Equal(referenceA.StartButton, referenceB.StartButton),
                "reference layout is deterministic");
            Assert(Mathf.Approximately(referenceA.Orchard01Card.height,
                    PortraitShellLayout.ReferenceLevelCardHeight)
                && Mathf.Approximately(referenceA.Orchard02Card.height,
                    PortraitShellLayout.ReferenceLevelCardHeight)
                && Mathf.Approximately(referenceA.Orchard03Card.height,
                    PortraitShellLayout.ReferenceLevelCardHeight),
                "all reference level cards are exactly 82 pixels high");
        }

        private static void ValidateLobby(float width, float height, Rect safeArea, bool inset)
        {
            var layout = PortraitShellLayout.CreateLobby(width, height, safeArea);
            var expectedGuiSafeArea = PortraitShellLayout.ToGuiSafeArea(height, safeArea);
            Assert(Equal(layout.Frame.SafeArea, expectedGuiSafeArea),
                "screen safe area maps to top-origin GUI coordinates");
            Assert(Contains(layout.Frame.SafeArea, layout.Title)
                && Contains(layout.Frame.SafeArea, layout.Orchard01Card)
                && Contains(layout.Frame.SafeArea, layout.Orchard02Card)
                && Contains(layout.Frame.SafeArea, layout.Orchard03Card)
                && Contains(layout.Frame.SafeArea, layout.StartButton)
                && Contains(layout.Frame.SafeArea, layout.Status),
                "all Lobby content remains inside " + width + "x" + height
                + (inset ? " inset" : " full") + " safe area");

            Assert(!Overlaps(layout.Orchard01Card, layout.Orchard02Card)
                && !Overlaps(layout.Orchard01Card, layout.Orchard03Card)
                && !Overlaps(layout.Orchard02Card, layout.Orchard03Card)
                && !Overlaps(layout.StartButton, layout.Orchard01Card)
                && !Overlaps(layout.StartButton, layout.Orchard02Card)
                && !Overlaps(layout.StartButton, layout.Orchard03Card),
                "three cards and Start never overlap");

            Assert(Equal(layout.LevelCardFor(LobbyPresenter.Orchard01LevelId), layout.Orchard01Card)
                && Equal(layout.LevelCardFor(LobbyPresenter.Orchard02LevelId), layout.Orchard02Card)
                && Equal(layout.LevelCardFor(LobbyPresenter.Orchard03LevelId), layout.Orchard03Card),
                "draw and hit-test use the same three card rectangles");
            Assert(PortraitShellLayout.HitTest(layout, layout.Orchard01Card.center, false)
                    == ShellHitTarget.LevelOrchard01
                && PortraitShellLayout.HitTest(layout, layout.Orchard02Card.center, false)
                    == ShellHitTarget.LevelOrchard02
                && PortraitShellLayout.HitTest(layout, layout.Orchard03Card.center, false)
                    == ShellHitTarget.LevelOrchard03
                && PortraitShellLayout.HitTest(layout, layout.StartButton.center, false)
                    == ShellHitTarget.Start,
                "each card and Start hit only its drawn rectangle");
            Assert(PortraitShellLayout.HitTest(layout, layout.Orchard01Card.center, true)
                    == ShellHitTarget.None
                && PortraitShellLayout.HitTest(layout, layout.StartButton.center, true)
                    == ShellHitTarget.None,
                "Lobby hit targets are disabled during transition");

            var expectedCardHeight = PortraitShellLayout.ReferenceLevelCardHeight * layout.Frame.Scale;
            Assert(Mathf.Approximately(layout.Orchard01Card.height, expectedCardHeight)
                && Mathf.Approximately(layout.Orchard02Card.height, expectedCardHeight)
                && Mathf.Approximately(layout.Orchard03Card.height, expectedCardHeight),
                "all level cards retain the same scaled 82-pixel geometry");
        }

        private static void ValidateSettlement(float width, float height, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateSettlement(width, height, safeArea);
            Assert(Contains(layout.Frame.SafeArea, layout.ResultCard)
                && Contains(layout.ResultCard, layout.Outcome)
                && Contains(layout.ResultCard, layout.CompletedLevel)
                && Contains(layout.ResultCard, layout.ReachedWave)
                && Contains(layout.ResultCard, layout.RemainingLives),
                "result values including completed level remain inside result card");
            Assert(Contains(layout.Frame.SafeArea, layout.RetryButton)
                && Contains(layout.Frame.SafeArea, layout.ReturnButton)
                && !Overlaps(layout.RetryButton, layout.ReturnButton),
                "settlement actions remain usable and non-overlapping");
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = .01f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        private static bool Overlaps(Rect first, Rect second)
        {
            const float tolerance = .01f;
            return Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin) > tolerance
                && Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin) > tolerance;
        }

        private static bool Equal(Rect first, Rect second)
        {
            return Mathf.Approximately(first.x, second.x)
                && Mathf.Approximately(first.y, second.y)
                && Mathf.Approximately(first.width, second.width)
                && Mathf.Approximately(first.height, second.height);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Shell layout validation failed: " + message);
        }
    }
}
