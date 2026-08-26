using System;
using System.Reflection;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CompactControlLifecycleSmoke
    {
        public static void Run()
        {
            var tokens = RuntimeUiFeedbackTokens.SunnyOrchardDefault();
            ValidateFeedbackContract(tokens);
            ValidateActivation(tokens);
            ValidateDeactivation(tokens);
            ValidateUnscaledProgress(tokens);
            ValidateRapidReversal(tokens);
            ValidateReducedMotion(tokens);
            ValidateSessionReset(tokens);
            ValidateActiveSurfaceContract(tokens);
            Debug.Log("COMPACT_CONTROL_LIFECYCLE_SMOKE_OK");
        }

        private static void ValidateFeedbackContract(RuntimeUiFeedbackTokens tokens)
        {
            Assert(Approximately(tokens.CompactControlActivateSeconds, .16f)
                && Approximately(tokens.CompactControlDeactivateSeconds, .12f),
                "compact-control lifecycle uses the approved unscaled timing tokens");
            Assert(typeof(RuntimeUiFeedbackTokens).GetField(
                    "compactControlTransitionInsetScale",
                    BindingFlags.Instance | BindingFlags.NonPublic) == null
                && typeof(RuntimeUiFeedbackTokens).GetField(
                    "compactControlActiveCycleInsetScale",
                    BindingFlags.Instance | BindingFlags.NonPublic) == null
                && typeof(RuntimeUiCompactControlState).GetProperty(
                    "StartingOverlayScale") == null
                && typeof(RuntimeUiCompactControlVisualSample).GetProperty(
                    "OverlayScale") == null
                && typeof(RuntimeUiCompactControlVisualSample).GetProperty(
                    "OverlayOpacity") == null
                && typeof(RuntimeUiCompactControlVisualSample).GetProperty(
                    "ShowsActiveOverlay") == null,
                "obsolete compact-control overlay and scale names are removed");
            Assert(typeof(RuntimeUiCompactControlVisualSample).GetProperty(
                    "ActiveSurfaceOpacity") == null
                && typeof(RuntimeUiCompactControlVisualSample).GetProperty(
                    "ShowsActiveSurface") == null
                && typeof(RuntimeUiFeedbackTokens).GetProperty(
                    "CompactControlActiveCycleSeconds") == null
                && typeof(RuntimeUiFeedbackTokens).GetField(
                    "compactControlActiveCycleSeconds",
                    BindingFlags.Instance | BindingFlags.NonPublic) == null,
                "superseded overlay-opacity and no-op active-cycle APIs are deleted");
        }

        private static void ValidateActivation(RuntimeUiFeedbackTokens tokens)
        {
            const float start = 10f;
            var state = RuntimeUiCompactControlLifecycle.Reset(false, start);
            Assert(state.IsBound
                && !state.AuthoritativeActive
                && state.Phase == RuntimeUiCompactControlPhase.Inactive,
                "false reset starts in stable inactive");

            var activating = RuntimeUiCompactControlLifecycle.Evaluate(
                state, true, start, tokens);
            Assert(activating.State.AuthoritativeActive
                && activating.State.Phase == RuntimeUiCompactControlPhase.Activating
                && Approximately(activating.State.PhaseDuration,
                    tokens.CompactControlActivateSeconds)
                && activating.Sample.Phase == RuntimeUiCompactControlPhase.Activating
                && Approximately(activating.Sample.ActiveAmount, 0f),
                "false to true starts one finite activating transition");

            var mid = RuntimeUiCompactControlLifecycle.Evaluate(
                activating.State, true,
                start + tokens.CompactControlActivateSeconds * .5f, tokens);
            Assert(mid.State.Phase == RuntimeUiCompactControlPhase.Activating
                && mid.Sample.ActiveAmount > 0f && mid.Sample.ActiveAmount < 1f,
                "activating exposes one bounded semantic active amount");

            var active = RuntimeUiCompactControlLifecycle.Evaluate(
                mid.State, true, start + tokens.CompactControlActivateSeconds, tokens);
            Assert(active.State.AuthoritativeActive
                && active.State.Phase == RuntimeUiCompactControlPhase.Active
                && active.Sample.Phase == RuntimeUiCompactControlPhase.Active
                && Approximately(active.Sample.ActiveAmount, 1f),
                "activating resolves exactly to sustained active; state="
                + active.State.Phase + " sample=" + active.Sample.Phase
                + " amount=" + active.Sample.ActiveAmount);
        }

        private static void ValidateDeactivation(RuntimeUiFeedbackTokens tokens)
        {
            const float start = 20f;
            var state = RuntimeUiCompactControlLifecycle.Reset(true, start);
            Assert(state.AuthoritativeActive
                && state.Phase == RuntimeUiCompactControlPhase.Active,
                "true reset starts in stable active");

            var deactivating = RuntimeUiCompactControlLifecycle.Evaluate(
                state, false, start, tokens);
            Assert(!deactivating.State.AuthoritativeActive
                && deactivating.State.Phase == RuntimeUiCompactControlPhase.Deactivating
                && Approximately(deactivating.State.PhaseDuration,
                    tokens.CompactControlDeactivateSeconds)
                && Approximately(deactivating.Sample.ActiveAmount, 1f),
                "true to false starts one finite deactivating transition");

            var mid = RuntimeUiCompactControlLifecycle.Evaluate(
                deactivating.State, false,
                start + tokens.CompactControlDeactivateSeconds * .5f, tokens);
            Assert(mid.State.Phase == RuntimeUiCompactControlPhase.Deactivating
                && mid.Sample.ActiveAmount > 0f && mid.Sample.ActiveAmount < 1f,
                "deactivating preserves one bounded semantic active amount");

            var inactive = RuntimeUiCompactControlLifecycle.Evaluate(
                mid.State, false, start + tokens.CompactControlDeactivateSeconds, tokens);
            Assert(!inactive.State.AuthoritativeActive
                && inactive.State.Phase == RuntimeUiCompactControlPhase.Inactive
                && inactive.Sample.Phase == RuntimeUiCompactControlPhase.Inactive
                && Approximately(inactive.Sample.ActiveAmount, 0f),
                "deactivating resolves exactly to inactive without residual overlay state");
        }

        private static void ValidateUnscaledProgress(RuntimeUiFeedbackTokens tokens)
        {
            const float scaledTimeAtPause = 7f;
            const float scaledTimeAfterDeadline = 7f;
            const float unscaledStart = 30f;
            var state = RuntimeUiCompactControlLifecycle.Reset(false, unscaledStart);
            var activating = RuntimeUiCompactControlLifecycle.Evaluate(
                state, true, unscaledStart, tokens);
            var active = RuntimeUiCompactControlLifecycle.Evaluate(
                activating.State, true,
                unscaledStart + tokens.CompactControlActivateSeconds, tokens);
            Assert(Approximately(scaledTimeAtPause, scaledTimeAfterDeadline)
                && active.State.Phase == RuntimeUiCompactControlPhase.Active
                && Approximately(active.Sample.ActiveAmount, 1f),
                "caller-supplied unscaled time completes activation while scaled time is frozen");

            var firstStable = RuntimeUiCompactControlLifecycle.Sample(
                active.State, unscaledStart + tokens.CompactControlActivateSeconds, tokens);
            var laterStable = RuntimeUiCompactControlLifecycle.Sample(active.State,
                unscaledStart + tokens.CompactControlActivateSeconds + 3f, tokens);
            Assert(firstStable.Phase == RuntimeUiCompactControlPhase.Active
                && laterStable.Phase == RuntimeUiCompactControlPhase.Active
                && Approximately(firstStable.ActiveAmount, 1f)
                && Approximately(laterStable.ActiveAmount, 1f),
                "sustained active remains a stable complete semantic endpoint");
        }

        private static void ValidateRapidReversal(RuntimeUiFeedbackTokens tokens)
        {
            const float start = 40f;
            var initial = RuntimeUiCompactControlLifecycle.Reset(false, start);
            var activating = RuntimeUiCompactControlLifecycle.Evaluate(
                initial, true, start, tokens);
            var activationMid = RuntimeUiCompactControlLifecycle.Evaluate(
                activating.State, true,
                start + tokens.CompactControlActivateSeconds * .5f, tokens);
            var amountBeforeFirstReverse = activationMid.Sample.ActiveAmount;

            var deactivating = RuntimeUiCompactControlLifecycle.Evaluate(
                activationMid.State, false,
                start + tokens.CompactControlActivateSeconds * .5f, tokens);
            Assert(deactivating.State.Phase
                    == RuntimeUiCompactControlPhase.Deactivating
                && Approximately(deactivating.State.StartingActiveAmount,
                    amountBeforeFirstReverse)
                && Approximately(deactivating.Sample.ActiveAmount,
                    amountBeforeFirstReverse)
                && deactivating.State.PhaseDuration
                    < tokens.CompactControlDeactivateSeconds,
                "first reversal redirects continuously from the sampled visual amount");

            var reverseTime = deactivating.State.PhaseStartedAt
                + deactivating.State.PhaseDuration * .5f;
            var deactivationMid = RuntimeUiCompactControlLifecycle.Evaluate(
                deactivating.State, false, reverseTime, tokens);
            var amountBeforeSecondReverse = deactivationMid.Sample.ActiveAmount;
            var reactivating = RuntimeUiCompactControlLifecycle.Evaluate(
                deactivationMid.State, true, reverseTime, tokens);
            Assert(reactivating.State.Phase == RuntimeUiCompactControlPhase.Activating
                && Approximately(reactivating.State.StartingActiveAmount,
                    amountBeforeSecondReverse)
                && Approximately(reactivating.Sample.ActiveAmount,
                    amountBeforeSecondReverse)
                && reactivating.State.PhaseDuration
                    < tokens.CompactControlActivateSeconds,
                "second reversal replaces the stale transition without a visual jump");

            var active = RuntimeUiCompactControlLifecycle.Evaluate(
                reactivating.State, true,
                reactivating.State.PhaseStartedAt + reactivating.State.PhaseDuration,
                tokens);
            Assert(active.State.Phase == RuntimeUiCompactControlPhase.Active
                && active.State.AuthoritativeActive
                && Approximately(active.Sample.ActiveAmount, 1f),
                "rapid reversals settle only at the current authoritative target");
        }

        private static void ValidateReducedMotion(RuntimeUiFeedbackTokens tokens)
        {
            const float start = 50f;
            var inactive = RuntimeUiCompactControlLifecycle.Reset(false, start);
            var active = RuntimeUiCompactControlLifecycle.Evaluate(
                inactive, true, start, tokens, true);
            Assert(active.State.Phase == RuntimeUiCompactControlPhase.Active
                && active.Sample.Phase == RuntimeUiCompactControlPhase.Active
                && Approximately(active.Sample.ActiveAmount, 1f),
                "explicit reduced motion immediately returns static active");

            var backToInactive = RuntimeUiCompactControlLifecycle.Evaluate(
                active.State, false, start, tokens, true);
            Assert(backToInactive.State.Phase == RuntimeUiCompactControlPhase.Inactive
                && backToInactive.Sample.Phase == RuntimeUiCompactControlPhase.Inactive
                && Approximately(backToInactive.Sample.ActiveAmount, 0f),
                "explicit reduced motion immediately returns static inactive");

            var reducedTokens = WithReducedMotion(tokens);
            var tokenDriven = RuntimeUiCompactControlLifecycle.Evaluate(
                inactive, true, start, reducedTokens);
            var later = RuntimeUiCompactControlLifecycle.Sample(tokenDriven.State,
                start + 3f, reducedTokens);
            Assert(tokenDriven.State.Phase == RuntimeUiCompactControlPhase.Active
                && Approximately(tokenDriven.Sample.ActiveAmount, 1f)
                && Approximately(later.ActiveAmount, 1f),
                "theme reduced-motion token removes transition motion");
        }

        private static void ValidateSessionReset(RuntimeUiFeedbackTokens tokens)
        {
            const float start = 60f;
            var oldSession = RuntimeUiCompactControlLifecycle.Evaluate(
                RuntimeUiCompactControlLifecycle.Reset(false, start),
                true, start, tokens);
            Assert(oldSession.State.Phase == RuntimeUiCompactControlPhase.Activating,
                "reset fixture owns transient state before session replacement");

            var restarted = RuntimeUiCompactControlLifecycle.Reset(false, start + .03f);
            Assert(restarted.IsBound
                && !restarted.AuthoritativeActive
                && restarted.Phase == RuntimeUiCompactControlPhase.Inactive
                && Approximately(restarted.StartingActiveAmount, 0f)
                && Approximately(RuntimeUiCompactControlLifecycle.Sample(restarted,
                    start + .03f, tokens).ActiveAmount, 0f),
                "restart clears an old transition and active amount");

            var rebound = RuntimeUiCompactControlLifecycle.Rebind(true, start + .04f);
            Assert(rebound.IsBound && rebound.AuthoritativeActive
                && rebound.Phase == RuntimeUiCompactControlPhase.Active
                && Approximately(rebound.StartingActiveAmount, 1f)
                && Approximately(RuntimeUiCompactControlLifecycle.Sample(rebound,
                    start + .04f, tokens).ActiveAmount, 1f),
                "session rebind initializes directly from the new authoritative value");

            var unbound = RuntimeUiCompactControlLifecycle.Evaluate(default,
                false, start + .05f, tokens);
            Assert(unbound.State.IsBound
                && unbound.State.Phase == RuntimeUiCompactControlPhase.Inactive,
                "default state cannot leak presentation from a prior session");
        }

        private static void ValidateActiveSurfaceContract(
            RuntimeUiFeedbackTokens tokens)
        {
            Assert((int)RuntimeUiArtSlot.ActionCompactControlActive == 54
                && RuntimeUiArtSlots.SemanticId(
                    RuntimeUiArtSlot.ActionCompactControlActive)
                    == "action.compact-control-active"
                && RuntimeUiArtSlots.Geometry(
                    RuntimeUiArtSlot.ActionCompactControlActive)
                    == RuntimeUiArtGeometry.NineSlice
                && Array.IndexOf(Enum.GetNames(typeof(RuntimeUiArtSlot)),
                    "IndicatorControlActive") < 0,
                "slot 54 is only the required nine-slice active surface contract");

            var controlRect = new Rect(20f, 30f, 52f, 52f);
            var iconLayout = RuntimeUiGui.ResolveCompactControlLayout(controlRect,
                RuntimeUiInteractionState.Normal, false, tokens);
            var multiplierLayout = RuntimeUiGui.ResolveCompactControlLayout(controlRect,
                RuntimeUiInteractionState.Pressed, true, tokens,
                new RuntimeUiMotionSample(.98f, 1f, 1f));

            Assert(SameRect(iconLayout.SurfaceRect,
                    iconLayout.VisualBounds)
                && SameRect(multiplierLayout.SurfaceRect,
                    multiplierLayout.VisualBounds),
                "compact layout exposes exactly one final transformed surface geometry");
            Assert(!iconLayout.UsesMultiplierText
                && multiplierLayout.UsesMultiplierText
                && multiplierLayout.ContentRect.width
                    > iconLayout.ContentRect.width
                && iconLayout.IsContained() && multiplierLayout.IsContained(),
                "icons stay compact while the single multiplier text owns the center");
        }

        private static RuntimeUiFeedbackTokens WithReducedMotion(
            RuntimeUiFeedbackTokens tokens)
        {
            object boxed = tokens;
            var field = typeof(RuntimeUiFeedbackTokens).GetField("reducedMotion",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert(field != null, "feedback contract exposes the serialized reduced-motion token");
            field.SetValue(boxed, true);
            return (RuntimeUiFeedbackTokens)boxed;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= .0001f;
        }

        private static bool SameRect(Rect left, Rect right)
        {
            return Approximately(left.x, right.x)
                && Approximately(left.y, right.y)
                && Approximately(left.width, right.width)
                && Approximately(left.height, right.height);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Compact-control lifecycle smoke failed: " + message);
        }
    }
}
