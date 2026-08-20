using System;
using System.Reflection;
using FruitDefense.Shell;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiInteractionPolishSmoke
    {
        public static void Run()
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            ValidateMotionTokens(theme.Feedback);
            ValidateMotionSamples(theme.Feedback);
            ValidatePressLifecycle(theme.Feedback);
            ValidateAuthoritativeHitGeometry(theme.Feedback);
            ValidateAllocationShape();
            Debug.Log("RUNTIME_UI_INTERACTION_POLISH_OK");
        }

        private static void ValidateMotionTokens(RuntimeUiFeedbackTokens tokens)
        {
            Assert(tokens.PressScale >= .8f && tokens.PressScale < 1f,
                "press scale remains restrained and visible");
            Assert(tokens.PopScale > 1f && tokens.PopScale <= 1.3f,
                "routine pop remains restrained");
            Assert(tokens.StrongPopScale >= tokens.PopScale
                && tokens.StrongPopScale <= 1.3f,
                "strong pop is ordered and restrained");
            Assert(tokens.RevealOffset > 0f && tokens.RevealOffset <= 24f,
                "route travel remains bounded");
            Assert(tokens.UnscaledRevealSeconds >= .1f
                && tokens.UnscaledRevealSeconds <= .5f
                && tokens.UnscaledStaggerSeconds >= 0f
                && tokens.UnscaledStaggerSeconds <= .1f,
                "route timing remains short and capped");
            Assert(tokens.DragCancelDistance >= 4f
                && tokens.DragCancelDistance <= 20f,
                "drag suppression threshold is usable at the reference viewport");
        }

        private static void ValidateMotionSamples(RuntimeUiFeedbackTokens tokens)
        {
            const float start = 10f;
            var press = RuntimeUiFeedbackPulse.Begin(start, tokens.UnscaledPressSeconds);
            var heldPress = RuntimeUiMotion.HeldPress(tokens);
            Assert(Mathf.Approximately(heldPress.Scale, tokens.PressScale)
                && Mathf.Approximately(heldPress.Alpha, 1f),
                "held press resolves directly to the theme press scale");
            Assert(Mathf.Approximately(RuntimeUiMotion.Evaluate(press, start,
                    tokens, RuntimeUiMotionPattern.Press).Scale, tokens.PressScale),
                "release rebound starts from the held press scale");
            var pressMid = RuntimeUiMotion.Evaluate(press,
                start + tokens.UnscaledPressSeconds * .25f,
                tokens, RuntimeUiMotionPattern.Press);
            Assert(pressMid.Scale < 1f && Mathf.Approximately(pressMid.Alpha, 1f),
                "press sample visibly compresses without fading");
            Assert(RuntimeUiMotion.Evaluate(press, press.Deadline,
                    tokens, RuntimeUiMotionPattern.Press).IsResting,
                "press resolves to the exact resting sample");

            var popDuration = tokens.UnscaledSelectionSeconds
                + tokens.UnscaledTransitionSeconds;
            var pop = RuntimeUiFeedbackPulse.Begin(start, popDuration);
            var popMid = RuntimeUiMotion.Evaluate(pop, start + popDuration * .35f,
                tokens, RuntimeUiMotionPattern.Pop);
            Assert(popMid.Scale > 1f && Mathf.Approximately(popMid.Alpha, 1f),
                "pop sample reaches a visible overshoot");

            var reveal = RuntimeUiMotion.BeginReveal(start, tokens, 3);
            var revealStart = RuntimeUiMotion.Evaluate(reveal, start,
                tokens, RuntimeUiMotionPattern.Stagger, 0);
            var delayedStart = RuntimeUiMotion.Evaluate(reveal, start,
                tokens, RuntimeUiMotionPattern.Stagger, 3);
            var revealMid = RuntimeUiMotion.Evaluate(reveal,
                start + tokens.UnscaledRevealSeconds * .5f,
                tokens, RuntimeUiMotionPattern.Stagger, 0);
            Assert(revealStart.Alpha <= .001f
                && Mathf.Approximately(revealStart.OffsetY, tokens.RevealOffset)
                && delayedStart.Alpha <= .001f
                && revealMid.Alpha > 0f && revealMid.Alpha < 1f
                && revealMid.OffsetY > 0f,
                "stagger has a hidden delayed start and an eased midpoint");
            Assert(RuntimeUiMotion.Evaluate(reveal, reveal.Deadline,
                    tokens, RuntimeUiMotionPattern.Stagger, 3).IsResting,
                "stagger resolves to exact resting geometry");
            Assert(RuntimeUiMotion.Evaluate(reveal, start,
                    tokens, RuntimeUiMotionPattern.StrongPop,
                    reduceMotion: true).IsResting,
                "reduced motion resolves immediately without travel or overshoot");

            var replaced = RuntimeUiFeedbackPulse.Begin(start + .04f, popDuration);
            Assert(Mathf.Approximately(replaced.StartedAt, start + .04f)
                && RuntimeUiMotion.Evaluate(replaced, start + .04f,
                    tokens, RuntimeUiMotionPattern.Pop).IsResting,
                "owner replacement restarts feedback from a deterministic sample");
        }

        private static void ValidatePressLifecycle(RuntimeUiFeedbackTokens tokens)
        {
            var tracker = new RuntimeUiPressTracker();
            var rect = new Rect(10f, 20f, 120f, 48f);
            const int control = 7101;
            var leaveEvent = new Event { type = EventType.MouseLeaveWindow };
            Assert(RuntimeUiPointerSample.FromEvent(leaveEvent).Phase
                    == RuntimeUiPointerPhase.Cancel,
                "leaving the pointer window maps to explicit cancellation");
            var down = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Down, rect.center),
                tokens.DragCancelDistance);
            var held = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.None, rect.center),
                tokens.DragCancelDistance);
            var up = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Up, rect.center),
                tokens.DragCancelDistance);
            Assert(down.Pressed && held.Pressed && up.Activated && !tracker.HasOwner,
                "valid down-hold-up activates once and releases ownership");

            tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Down, rect.center),
                tokens.DragCancelDistance);
            var moved = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Move,
                    rect.center + Vector2.right * (tokens.DragCancelDistance + 1f)),
                tokens.DragCancelDistance);
            var draggedUp = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Up, rect.center),
                tokens.DragCancelDistance);
            Assert(moved.Cancelled && !moved.Pressed
                && !draggedUp.Activated && !tracker.HasOwner,
                "movement beyond the threshold suppresses activation");

            var nearEdge = new Vector2(rect.xMin + 1f, rect.center.y);
            tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Down, nearEdge),
                tokens.DragCancelDistance);
            var leftRect = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Move,
                    nearEdge + Vector2.left * 2f), tokens.DragCancelDistance);
            var reenteredUp = tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Up, rect.center),
                tokens.DragCancelDistance);
            Assert(leftRect.Cancelled && !tracker.HasOwner
                && !reenteredUp.Activated,
                "leaving the hit rect cancels ownership even below drag threshold");

            tracker.Update(control, rect, true,
                new RuntimeUiPointerSample(RuntimeUiPointerPhase.Down, rect.center),
                tokens.DragCancelDistance);
            var disabled = tracker.Update(control, rect, false, default,
                tokens.DragCancelDistance);
            Assert(disabled.Cancelled && !tracker.HasOwner,
                "disabling an owned target cancels the press");
        }

        private static void ValidateAuthoritativeHitGeometry(RuntimeUiFeedbackTokens tokens)
        {
            var safeArea = new Rect(0f, 0f, 402f, 874f);
            var layout = PortraitShellLayout.CreateLobby(402f, 874f, safeArea);
            var original = layout.StartButton;
            var pulse = RuntimeUiFeedbackPulse.Begin(0f,
                tokens.UnscaledSelectionSeconds + tokens.UnscaledTransitionSeconds);
            var visual = RuntimeUiMotion.Evaluate(pulse, pulse.Deadline * .35f,
                tokens, RuntimeUiMotionPattern.Pop).Transform(original);
            var visualOnlyPoint = new Vector2(original.xMin - 1f, original.center.y);
            Assert(!Approximately(visual, original)
                && PortraitShellLayout.HitTest(layout, original.center, false)
                    == ShellHitTarget.Start
                && visual.Contains(visualOnlyPoint)
                && !original.Contains(visualOnlyPoint)
                && PortraitShellLayout.HitTest(layout, visualOnlyPoint, false)
                    == ShellHitTarget.None,
                "motion-only overflow remains outside the authoritative hit layout");
        }

        private static void ValidateAllocationShape()
        {
            AssertValueOnly(typeof(RuntimeUiMotionSample));
            AssertValueOnly(typeof(RuntimeUiFeedbackPulse));
            AssertValueOnly(typeof(RuntimeUiPointerSample));
            AssertValueOnly(typeof(RuntimeUiPressResult));
            AssertValueOnly(typeof(RuntimeUiPressTracker));
        }

        private static void AssertValueOnly(Type type)
        {
            Assert(type.IsValueType, type.Name + " is a value type");
            var fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var index = 0; index < fields.Length; index++)
            {
                Assert(fields[index].FieldType.IsValueType,
                    type.Name + " owns no allocating reference field: " + fields[index].Name);
            }
        }

        private static bool Approximately(Rect left, Rect right)
        {
            return Vector2.Distance(left.position, right.position) <= .001f
                && Vector2.Distance(left.size, right.size) <= .001f;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Runtime UI interaction polish validation failed: " + message);
        }
    }
}
