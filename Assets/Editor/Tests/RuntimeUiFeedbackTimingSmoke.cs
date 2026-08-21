using System;
using System.IO;
using System.Reflection;
using FruitDefense.App;
using FruitDefense.Core;
using FruitDefense.Presentation;
using FruitDefense.Shell;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiFeedbackTimingSmoke
    {
        public static void Run()
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            ValidateRestrainedThemeDurations(theme.Feedback);
            ValidateExplicitUnscaledPulse(theme.Feedback);
            ValidateInteractionStatePriority();
            ValidateHitGeometry();
            ValidateRuntimeConsumptionSource();
            Debug.Log("RUNTIME_UI_FEEDBACK_TIMING_OK");
        }

        private static void ValidateRestrainedThemeDurations(RuntimeUiFeedbackTokens feedback)
        {
            Assert(feedback.UnscaledFocusSeconds > 0f
                && feedback.UnscaledPressSeconds > 0f
                && feedback.UnscaledSelectionSeconds > 0f
                && feedback.UnscaledTransitionSeconds > 0f
                && feedback.UnscaledStatusSeconds > 0f
                && feedback.UnscaledPopSeconds > 0f
                && feedback.UnscaledRevealSeconds > 0f
                && feedback.UnscaledStaggerSeconds > 0f,
                "all authored unscaled feedback durations are positive");
            Assert(feedback.UnscaledFocusSeconds <= .5f
                && feedback.UnscaledPressSeconds <= .1f
                && feedback.UnscaledPopSeconds <= .14f
                && feedback.UnscaledSelectionSeconds <= .5f
                && feedback.UnscaledTransitionSeconds <= .5f
                && feedback.UnscaledRevealSeconds <= .5f
                && feedback.UnscaledStaggerSeconds <= .1f
                && feedback.UnscaledStatusSeconds <= 5f,
                "feedback durations remain restrained");
        }

        private static void ValidateExplicitUnscaledPulse(RuntimeUiFeedbackTokens feedback)
        {
            var pulseType = typeof(RuntimeUiFeedbackPulse);
            Assert(pulseType.IsValueType,
                "feedback pulse is a value type");
            var fields = pulseType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var index = 0; index < fields.Length; index++)
            {
                Assert(fields[index].FieldType.IsValueType,
                    "feedback pulse owns no allocating reference field: " + fields[index].Name);
            }

            const float timeScale = 0f;
            const float scaledTimeAtStart = 0f;
            const float scaledTimeBeforeDeadline = 0f;
            const float start = 37.25f;
            var pulse = RuntimeUiFeedbackPulse.Begin(
                start, feedback.UnscaledPressSeconds);
            Assert(timeScale == 0f
                && scaledTimeAtStart == scaledTimeBeforeDeadline
                && pulse.IsScheduled
                && Mathf.Approximately(pulse.StartedAt, start)
                && pulse.IsActive(start)
                && pulse.IsActive(pulse.Deadline - .0001f)
                && !pulse.IsActive(start - .0001f)
                && !pulse.IsActive(pulse.Deadline),
                "pulse advances only from explicit unscaled time at timeScale zero");
            Assert(!RuntimeUiFeedbackPulse.Begin(start, 0f).IsScheduled,
                "zero duration creates no feedback pulse");

            AssertThrows(() => RuntimeUiFeedbackPulse.Begin(float.NaN, .1f),
                "non-finite start time is rejected");
            AssertThrows(() => RuntimeUiFeedbackPulse.Begin(0f, -1f),
                "negative duration is rejected");
            AssertThrows(() => RuntimeUiFeedbackPulse.Begin(float.MaxValue, float.MaxValue),
                "overflowing deadline is rejected");
        }

        private static void ValidateInteractionStatePriority()
        {
            Assert(ResolvePresenterState(typeof(LobbyPresenter), "ResolveCardState",
                    true, false, true, true, true) == RuntimeUiInteractionState.Loading
                && ResolvePresenterState(typeof(LobbyPresenter), "ResolveCardState",
                    false, false, true, true, true) == RuntimeUiInteractionState.Disabled
                && ResolvePresenterState(typeof(LobbyPresenter), "ResolveCardState",
                    false, true, true, true, true) == RuntimeUiInteractionState.Pressed
                && ResolvePresenterState(typeof(LobbyPresenter), "ResolveCardState",
                    false, true, true, false, false) == RuntimeUiInteractionState.Selected,
                "Lobby keeps loading/disabled above transient press and persistent selection");
            Assert(ResolvePresenterState(typeof(SettlementPresenter), "ResolveActionState",
                    true, false, true, true) == RuntimeUiInteractionState.Loading
                && ResolvePresenterState(typeof(SettlementPresenter), "ResolveActionState",
                    false, false, true, true) == RuntimeUiInteractionState.Disabled
                && ResolvePresenterState(typeof(SettlementPresenter), "ResolveActionState",
                    false, true, true, true) == RuntimeUiInteractionState.Pressed,
                "Settlement keeps loading/disabled above transient pointer feedback");
            Assert(BattleUiPresentationState.ResolveSlotState(
                    false, true, true, true) == RuntimeUiInteractionState.Disabled
                && BattleUiPresentationState.ResolveSlotState(
                    true, true, true, false) == RuntimeUiInteractionState.Selected,
                "Battle keeps disabled above transient and selected feedback");
        }

        private static RuntimeUiInteractionState ResolvePresenterState(
            Type presenterType, string methodName, params object[] arguments)
        {
            var method = presenterType.GetMethod(methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert(method != null,
                presenterType.Name + " is missing feedback state resolver " + methodName);
            return (RuntimeUiInteractionState)method.Invoke(null, arguments);
        }

        private static void ValidateHitGeometry()
        {
            var safeArea = new Rect(0f, 0f, 402f, 874f);
            var lobby = PortraitShellLayout.CreateLobby(402f, 874f, safeArea);
            var settlement = PortraitShellLayout.CreateSettlement(402f, 874f, safeArea);
            Assert(PortraitShellLayout.HitTest(lobby, lobby.StartButton.center, false)
                    == ShellHitTarget.Start
                && PortraitShellLayout.HitTest(lobby, lobby.StartButton.center, true)
                    == ShellHitTarget.None,
                "Lobby timing does not create a second hit rectangle");
            Assert(PortraitShellLayout.HitTest(
                    settlement, settlement.RetryButton.center, false) == ShellHitTarget.Retry
                && PortraitShellLayout.HitTest(
                    settlement, settlement.ReturnButton.center, false) == ShellHitTarget.Return
                && PortraitShellLayout.HitTest(
                    settlement, settlement.RetryButton.center, true) == ShellHitTarget.None,
                "Settlement timing does not create a second hit rectangle");

            var battle = new BattleUiLayout(GameConfig.DefaultBattlefield);
            Assert(Approximately(battle.WaveAction, new Rect(210f, 526f, 184f, 44f))
                && Approximately(battle.RefreshAction, new Rect(8f, 744f, 386f, 44f))
                && Approximately(battle.PauseAction, new Rect(274f, 20f, 52f, 52f)),
                "Battle feedback keeps the authoritative action geometry");
        }

        private static void ValidateRuntimeConsumptionSource()
        {
            var visualTypes = ReadSource("Scripts/UI/RuntimeUiVisualTypes.cs");
            var pulseSource = Slice(visualTypes,
                "public readonly struct RuntimeUiFeedbackPulse",
                "public sealed class RuntimeUiValidationIssue");
            Assert(!pulseSource.Contains("Time.")
                && pulseSource.Contains("float unscaledTime")
                && pulseSource.Contains("unscaledTime < deadline"),
                "feedback pulse consumes only caller-supplied unscaled time");

            var sharedGui = ReadSource("Scripts/UI/RuntimeUiGui.cs");
            var action = Slice(sharedGui,
                "public static bool DrawAction(", "public static void DrawMetric(");
            Assert(action.Contains("bool emphasized = false")
                && action.Contains("DrawStateIndicator(context, visualRect, state)")
                && action.Contains("state != RuntimeUiInteractionState.Disabled")
                && action.Contains("state != RuntimeUiInteractionState.Loading")
                && action.Contains("GUI.Button(rect, GUIContent.none")
                && action.Contains("RuntimeUiMotion.HeldPress(context.Theme.Feedback)")
                && action.Contains("visualMotion.Transform(rect)"),
                "action emphasis preserves semantic state, held press scale, disabled/loading priority, and hit rect");

            var appFlow = ReadSource("Scripts/App/AppFlowCoordinator.cs");
            AssertConsumes(appFlow, "AppFlow", "UnscaledFocusSeconds",
                "UnscaledPressSeconds", "UnscaledTransitionSeconds", "UnscaledStatusSeconds");
            var bootstrapGui = Slice(appFlow,
                "private void OnGUI()", "private void RefreshBootstrapFeedback(");
            Assert(bootstrapGui.IndexOf("_retryPressPulse = RuntimeUiFeedbackPulse.Begin",
                    StringComparison.Ordinal)
                    < bootstrapGui.IndexOf("_bootstrap.TryRetryInitialization()",
                        StringComparison.Ordinal)
                && !bootstrapGui.Contains("yield return"),
                "Bootstrap retry command remains in the click frame");

            var lobby = ReadSource("Scripts/Shell/LobbyPresenter.cs");
            AssertConsumes(lobby, "Lobby", "UnscaledFocusSeconds", "UnscaledPressSeconds",
                "UnscaledSelectionSeconds", "UnscaledTransitionSeconds",
                "UnscaledStatusSeconds");
            var lobbyGui = Slice(lobby, "private void OnGUI()", "private void DrawLevelCard(");
            Assert(lobbyGui.IndexOf("BeginPress(StartFeedbackTarget",
                    StringComparison.Ordinal)
                    < lobbyGui.IndexOf("if (TryStart())", StringComparison.Ordinal)
                && !lobbyGui.Contains("yield return"),
                "Lobby Start command remains in the click frame");

            var settlement = ReadSource("Scripts/Shell/SettlementPresenter.cs");
            AssertConsumes(settlement, "Settlement", "UnscaledFocusSeconds",
                "UnscaledPressSeconds", "UnscaledTransitionSeconds",
                "UnscaledStatusSeconds");
            var settlementGui = Slice(settlement,
                "private void OnGUI()", "private void BeginFocus(");
            Assert(settlementGui.IndexOf("BeginPress(RetryFeedbackTarget",
                    StringComparison.Ordinal)
                    < settlementGui.IndexOf("if (TryRetry())", StringComparison.Ordinal)
                && settlementGui.IndexOf("BeginPress(ReturnFeedbackTarget",
                    StringComparison.Ordinal)
                    < settlementGui.IndexOf("if (TryReturn())", StringComparison.Ordinal)
                && !settlementGui.Contains("yield return"),
                "Settlement commands remain in their click frame");

            var battle = ReadSource("Scripts/FruitDefenseGame.cs");
            AssertConsumes(battle, "Battle", "UnscaledFocusSeconds",
                "UnscaledSelectionSeconds", "UnscaledStatusSeconds");
            Assert(!battle.Contains("_statusUntil")
                && !battle.Contains("_returnPulseUntil")
                && !battle.Contains("_nurseryRollDisplayUntil")
                && !battle.Contains("Time.unscaledTime + 2.6f")
                && !battle.Contains("Time.unscaledTime + .4f")
                && !battle.Contains("Time.unscaledTime + .55f")
                && !battle.Contains("Time.unscaledTime + 1.8f"),
                "Battle owns no duplicate UI feedback deadlines");
        }

        private static void AssertConsumes(string source, string owner, params string[] tokens)
        {
            for (var index = 0; index < tokens.Length; index++)
                Assert(source.Contains(tokens[index]),
                    owner + " does not consume theme feedback token " + tokens[index]);
        }

        private static string ReadSource(string assetRelativePath)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, assetRelativePath));
        }

        private static string Slice(string source, string startToken, string endToken)
        {
            var start = source.IndexOf(startToken, StringComparison.Ordinal);
            Assert(start >= 0, "cannot locate source boundary " + startToken);
            var end = source.IndexOf(endToken, start + startToken.Length,
                StringComparison.Ordinal);
            Assert(end > start, "cannot locate source boundary " + endToken);
            return source.Substring(start, end - start);
        }

        private static bool Approximately(Rect left, Rect right)
        {
            return Vector2.Distance(left.position, right.position) <= .001f
                && Vector2.Distance(left.size, right.size) <= .001f;
        }

        private static void AssertThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            throw new InvalidOperationException(
                "Runtime UI feedback timing validation failed: " + message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    "Runtime UI feedback timing validation failed: " + message);
            }
        }
    }
}
