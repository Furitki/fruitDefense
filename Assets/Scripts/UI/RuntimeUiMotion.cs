using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public enum RuntimeUiMotionPattern
    {
        Press = 0,
        Pop = 1,
        StrongPop = 2,
        FadeSlide = 3,
        Stagger = 4,
    }

    public readonly struct RuntimeUiMotionSample
    {
        private readonly bool resolved;
        private readonly float scale;
        private readonly float alpha;
        private readonly float offsetY;

        public RuntimeUiMotionSample(float scale, float alpha, float offsetY)
        {
            if (!RuntimeUiNumbers.IsFinite(scale) || scale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(scale));
            if (!RuntimeUiNumbers.IsFinite(alpha) || alpha < 0f || alpha > 1f)
                throw new ArgumentOutOfRangeException(nameof(alpha));
            if (!RuntimeUiNumbers.IsFinite(offsetY))
                throw new ArgumentOutOfRangeException(nameof(offsetY));

            resolved = true;
            this.scale = scale;
            this.alpha = alpha;
            this.offsetY = offsetY;
        }

        public static RuntimeUiMotionSample Rest => new RuntimeUiMotionSample(1f, 1f, 0f);

        public float Scale => resolved ? scale : 1f;
        public float Alpha => resolved ? alpha : 1f;
        public float OffsetY => resolved ? offsetY : 0f;
        public bool IsResting => Mathf.Approximately(Scale, 1f)
            && Mathf.Approximately(Alpha, 1f)
            && Mathf.Approximately(OffsetY, 0f);

        public Rect Transform(Rect rect)
        {
            var center = rect.center + new Vector2(0f, OffsetY);
            rect.size *= Scale;
            rect.center = center;
            return rect;
        }

        public static RuntimeUiMotionSample Combine(RuntimeUiMotionSample first,
            RuntimeUiMotionSample second)
        {
            return new RuntimeUiMotionSample(
                first.Scale * second.Scale,
                first.Alpha * second.Alpha,
                first.OffsetY + second.OffsetY);
        }
    }

    public static class RuntimeUiMotion
    {
        public static RuntimeUiMotionSample Evaluate(RuntimeUiFeedbackPulse pulse,
            float unscaledTime, RuntimeUiFeedbackTokens tokens,
            RuntimeUiMotionPattern pattern, int staggerIndex = 0,
            bool reduceMotion = false)
        {
            if (reduceMotion || tokens.ReducedMotion || !pulse.IsScheduled
                || unscaledTime >= pulse.Deadline)
                return RuntimeUiMotionSample.Rest;

            switch (pattern)
            {
                case RuntimeUiMotionPattern.Press:
                    return Press(pulse.Progress(unscaledTime), tokens.PressScale);
                case RuntimeUiMotionPattern.Pop:
                    return Pop(pulse.Progress(unscaledTime), tokens.PopScale);
                case RuntimeUiMotionPattern.StrongPop:
                    return Pop(pulse.Progress(unscaledTime), tokens.StrongPopScale);
                case RuntimeUiMotionPattern.FadeSlide:
                    return Reveal(pulse.Progress(unscaledTime), tokens.RevealOffset);
                case RuntimeUiMotionPattern.Stagger:
                    return Stagger(pulse, unscaledTime, tokens, staggerIndex);
                default:
                    throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null);
            }
        }

        public static RuntimeUiFeedbackPulse BeginReveal(float unscaledTime,
            RuntimeUiFeedbackTokens tokens, int lastStaggerIndex)
        {
            var index = Mathf.Max(0, lastStaggerIndex);
            var duration = tokens.UnscaledRevealSeconds
                + tokens.UnscaledStaggerSeconds * index;
            return RuntimeUiFeedbackPulse.Begin(unscaledTime, duration);
        }

        public static RuntimeUiMotionSample HeldPress(RuntimeUiFeedbackTokens tokens)
        {
            return tokens.ReducedMotion
                ? RuntimeUiMotionSample.Rest
                : new RuntimeUiMotionSample(tokens.PressScale, 1f, 0f);
        }

        private static RuntimeUiMotionSample Press(float progress, float pressedScale)
        {
            return new RuntimeUiMotionSample(
                Mathf.Lerp(pressedScale, 1f, EaseOutCubic(progress)), 1f, 0f);
        }

        private static RuntimeUiMotionSample Pop(float progress, float peakScale)
        {
            const float risePortion = .42f;
            float scale;
            if (progress < risePortion)
            {
                var rise = EaseOutCubic(progress / risePortion);
                scale = Mathf.Lerp(1f, peakScale, rise);
            }
            else
            {
                var settle = Smooth((progress - risePortion) / (1f - risePortion));
                scale = Mathf.Lerp(peakScale, 1f, settle);
            }
            return new RuntimeUiMotionSample(scale, 1f, 0f);
        }

        private static RuntimeUiMotionSample Reveal(float progress, float offset)
        {
            var eased = EaseOutCubic(progress);
            return new RuntimeUiMotionSample(
                Mathf.Lerp(.985f, 1f, eased),
                Smooth(progress),
                Mathf.Lerp(offset, 0f, eased));
        }

        private static RuntimeUiMotionSample Stagger(RuntimeUiFeedbackPulse pulse,
            float unscaledTime, RuntimeUiFeedbackTokens tokens, int staggerIndex)
        {
            var start = pulse.StartedAt
                + Mathf.Max(0, staggerIndex) * tokens.UnscaledStaggerSeconds;
            var duration = tokens.UnscaledRevealSeconds;
            if (unscaledTime <= start)
                return new RuntimeUiMotionSample(.985f, 0f, tokens.RevealOffset);
            if (duration <= 0f || unscaledTime >= start + duration)
                return RuntimeUiMotionSample.Rest;
            return Reveal(Mathf.Clamp01((unscaledTime - start) / duration),
                tokens.RevealOffset);
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float EaseOutCubic(float value)
        {
            value = 1f - Mathf.Clamp01(value);
            return 1f - value * value * value;
        }
    }
}
