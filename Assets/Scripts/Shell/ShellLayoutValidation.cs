using System;
using UnityEngine;

namespace FruitDefense.Shell
{
    public static class ShellLayoutValidation
    {
        public static void ValidateReferenceGeometry()
        {
            var safeArea = new Rect(0f, 0f, PortraitShellLayout.ReferenceWidth, PortraitShellLayout.ReferenceHeight);
            var lobbyA = PortraitShellLayout.CreateLobby(402f, 874f, safeArea);
            var lobbyB = PortraitShellLayout.CreateLobby(402f, 874f, safeArea);
            var settlement = PortraitShellLayout.CreateSettlement(402f, 874f, safeArea);

            Assert(Equal(lobbyA.StartButton, lobbyB.StartButton), "reference layout is deterministic");
            Assert(Equal(lobbyA.Frame.SafeArea, safeArea), "full reference safe area maps to GUI coordinates");
            Assert(Contains(lobbyA.Frame.SafeArea, lobbyA.Title), "lobby title remains inside safe area");
            Assert(Contains(lobbyA.Frame.SafeArea, lobbyA.StartButton), "lobby Start remains inside safe area");
            Assert(Contains(lobbyA.Frame.SafeArea, lobbyA.LevelCard)
                && Contains(lobbyA.Frame.SafeArea, lobbyA.GrowthCard)
                && Contains(lobbyA.Frame.SafeArea, lobbyA.SettingsCard),
                "reserved cards remain inside safe area");
            Assert(!lobbyA.StartButton.Overlaps(lobbyA.LevelCard), "Start does not overlap reserved cards");

            Assert(Contains(settlement.Frame.SafeArea, settlement.ResultCard), "result card remains inside safe area");
            Assert(Contains(settlement.ResultCard, settlement.Outcome)
                && Contains(settlement.ResultCard, settlement.ReachedWave)
                && Contains(settlement.ResultCard, settlement.RemainingLives),
                "result values remain inside result card");
            Assert(Contains(settlement.Frame.SafeArea, settlement.RetryButton)
                && Contains(settlement.Frame.SafeArea, settlement.ReturnButton),
                "settlement actions remain inside safe area");
            Assert(!settlement.RetryButton.Overlaps(settlement.ReturnButton),
                "settlement actions do not overlap");

            var insetSafeArea = new Rect(0f, 20f, 402f, 834f);
            var insetLobby = PortraitShellLayout.CreateLobby(402f, 874f, insetSafeArea);
            Assert(Mathf.Approximately(insetLobby.Frame.SafeArea.y, 20f)
                && Mathf.Approximately(insetLobby.Frame.SafeArea.height, 834f),
                "screen safe-area origin converts to top-origin GUI coordinates");
            Assert(Contains(insetLobby.Frame.SafeArea, insetLobby.SettingsCard),
                "required Lobby content respects an inset safe area");
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = .01f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMax <= outer.yMax + tolerance;
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
