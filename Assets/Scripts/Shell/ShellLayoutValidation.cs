using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public static class ShellLayoutValidation
    {
        public static void SmokeValidate()
        {
            ValidateReferenceGeometry();
            Debug.Log("FRUIT_DEFENSE_SHELL_LAYOUT_OK");
        }

        public static void ValidateReferenceGeometry()
        {
            var referenceSafeArea = new Rect(0f, 0f,
                PortraitShellLayout.ReferenceWidth, PortraitShellLayout.ReferenceHeight);
            ValidateLobby(PortraitShellLayout.ReferenceWidth,
                PortraitShellLayout.ReferenceHeight, referenceSafeArea, false);
            ValidateSettlement(PortraitShellLayout.ReferenceWidth,
                PortraitShellLayout.ReferenceHeight, referenceSafeArea);
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
                "all reference level cards use the approved 176px height");
            Assert(Equal(referenceA.Title, new Rect(16f, 54f, 370f, 56f))
                && Equal(referenceA.Orchard01Card, new Rect(16f, 130f, 370f, 176f))
                && Equal(referenceA.Orchard02Card, new Rect(16f, 318f, 370f, 176f))
                && Equal(referenceA.Orchard03Card, new Rect(16f, 506f, 370f, 176f))
                && Equal(referenceA.StartButton, new Rect(16f, 702f, 370f, 72f))
                && Equal(referenceA.Status, new Rect(16f, 790f, 370f, 58f)),
                "Lobby reference composition matches the approved quality audit");
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
                "all level cards retain the same scaled 176px geometry");

            ValidateLobbyCardAnatomy(layout.Orchard01Card, layout.Frame.Scale, "orchard-01");
            ValidateLobbyCardAnatomy(layout.Orchard02Card, layout.Frame.Scale, "orchard-02");
            ValidateLobbyCardAnatomy(layout.Orchard03Card, layout.Frame.Scale, "orchard-03");
        }

        private static void ValidateLobbyCardAnatomy(Rect card, float scale, string caseName)
        {
            var anatomy = PortraitShellLayout.CreateLobbyLevelCard(card, scale);
            var expectedFrame = new Vector2(164f * scale, 104f * scale);
            Assert(Contains(card, anatomy.Frame)
                && Contains(anatomy.Frame, anatomy.Thumbnail)
                && Contains(card, anatomy.Title)
                && Contains(card, anatomy.Body)
                && Contains(card, anatomy.SelectedMarker)
                && Contains(card, anatomy.TransientIndicator),
                caseName + " card art, copy and state cues remain inside the original hit rect");
            Assert(Mathf.Approximately(anatomy.Frame.width, expectedFrame.x)
                && Mathf.Approximately(anatomy.Frame.height, expectedFrame.y)
                && Mathf.Approximately(anatomy.Frame.x - card.x, 4f * scale)
                && Mathf.Approximately(anatomy.Frame.y - card.y, 36f * scale)
                && Mathf.Approximately(anatomy.Thumbnail.x - anatomy.Frame.x,
                    6f * scale)
                && Mathf.Approximately(anatomy.Thumbnail.y - anatomy.Frame.y,
                    6f * scale),
                caseName + " card retains the 4,36 inset and 164x104 illustration frame");
            Assert(Mathf.Approximately(anatomy.Title.x - anatomy.Frame.xMax, 8f * scale)
                && Mathf.Approximately(anatomy.Title.x - card.x, 176f * scale)
                && Mathf.Approximately(anatomy.Title.width, 190f * scale)
                && Mathf.Approximately(anatomy.Title.y - card.y, 34f * scale)
                && Mathf.Approximately(anatomy.Title.height, 44f * scale)
                && Mathf.Approximately(anatomy.Body.y - card.y, 90f * scale)
                && Mathf.Approximately(anatomy.Body.height, 44f * scale)
                && !Overlaps(anatomy.Frame, anatomy.Title)
                && !Overlaps(anatomy.Frame, anatomy.Body)
                && !Overlaps(anatomy.Title, anatomy.SelectedMarker)
                && !Overlaps(anatomy.Body, anatomy.TransientIndicator),
                caseName + " card retains the 10px copy gap and cue-safe two-line copy");
            Assert(Mathf.Approximately(anatomy.SelectedMarker.width, 48f * scale)
                && Mathf.Approximately(anatomy.SelectedMarker.height, 48f * scale),
                caseName + " selected source canvas preserves its approved 32-36px optical size");
        }

        private static void ValidateSettlement(float width, float height, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateSettlement(width, height, safeArea);
            if (Mathf.Approximately(width, PortraitShellLayout.ReferenceWidth)
                && Mathf.Approximately(height, PortraitShellLayout.ReferenceHeight)
                && Equal(safeArea, new Rect(0f, 0f, width, height)))
            {
                Assert(Equal(layout.Title, new Rect(16f, 54f, 370f, 56f))
                    && Equal(layout.ResultCard, new Rect(16f, 130f, 370f, 474f))
                    && Equal(layout.ResultBanner, new Rect(58f, 146f, 286f, 72f))
                    && Equal(layout.Outcome, new Rect(98f, 156f, 206f, 52f))
                    && Equal(layout.OrchardVista, new Rect(32f, 234f, 338f, 190f))
                    && Equal(layout.CompletedLevel, new Rect(32f, 436f, 338f, 48f))
                    && Equal(layout.ReachedWave, new Rect(32f, 492f, 338f, 48f))
                    && Equal(layout.RemainingLives, new Rect(32f, 548f, 338f, 48f))
                    && Equal(layout.ResultIndicator, new Rect(308f, 168f, 28f, 28f))
                    && Equal(layout.RetryButton, new Rect(16f, 624f, 370f, 72f))
                    && Equal(layout.ReturnButton, new Rect(16f, 712f, 370f, 64f))
                    && Equal(layout.Status, new Rect(16f, 792f, 370f, 58f)),
                    "Settlement reference composition matches the approved quality audit");
            }
            Assert(Contains(layout.Frame.SafeArea, layout.ResultCard)
                && Contains(layout.ResultCard, layout.Outcome)
                && Contains(layout.ResultCard, layout.CompletedLevel)
                && Contains(layout.ResultCard, layout.ReachedWave)
                && Contains(layout.ResultCard, layout.RemainingLives)
                && Contains(layout.ResultCard, layout.ResultBanner)
                && Contains(layout.ResultCard, layout.OrchardVista)
                && Contains(layout.ResultBanner, layout.ResultIndicator),
                "result values including completed level remain inside result card");
            Assert(!Overlaps(layout.ResultBanner, layout.OrchardVista)
                && Contains(layout.ResultBanner, layout.Outcome)
                && !Overlaps(layout.Outcome, layout.ResultIndicator)
                && !Overlaps(layout.OrchardVista, layout.CompletedLevel)
                && !Overlaps(layout.OrchardVista, layout.ReachedWave)
                && !Overlaps(layout.OrchardVista, layout.RemainingLives),
                "result art hierarchy leaves outcome copy and metrics unobscured");
            Assert(Contains(layout.Frame.SafeArea, layout.RetryButton)
                && Contains(layout.Frame.SafeArea, layout.ReturnButton)
                && !Overlaps(layout.RetryButton, layout.ReturnButton),
                "settlement actions remain usable and non-overlapping");
            Assert(PortraitShellLayout.HitTest(layout, layout.RetryButton.center, false)
                    == ShellHitTarget.Retry
                && PortraitShellLayout.HitTest(layout, layout.ReturnButton.center, false)
                    == ShellHitTarget.Return,
                "Settlement Retry and Return hit exactly their drawn rectangles");
            Assert(PortraitShellLayout.HitTest(layout, layout.RetryButton.center, true)
                    == ShellHitTarget.None
                && PortraitShellLayout.HitTest(layout, layout.ReturnButton.center, true)
                    == ShellHitTarget.None,
                "Settlement hit targets are disabled during transition");
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
