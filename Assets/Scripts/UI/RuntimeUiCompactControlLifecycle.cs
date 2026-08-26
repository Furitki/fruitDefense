using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public enum RuntimeUiCompactControlPhase
    {
        Inactive = 0,
        Activating = 1,
        Active = 2,
        Deactivating = 3,
    }

    public readonly struct RuntimeUiCompactControlState
    {
        internal RuntimeUiCompactControlState(bool isBound, bool authoritativeActive,
            RuntimeUiCompactControlPhase phase, float phaseStartedAt,
            float phaseDuration, float startingActiveAmount)
        {
            IsBound = isBound;
            AuthoritativeActive = authoritativeActive;
            Phase = phase;
            PhaseStartedAt = phaseStartedAt;
            PhaseDuration = phaseDuration;
            StartingActiveAmount = startingActiveAmount;
        }

        public bool IsBound { get; }
        public bool AuthoritativeActive { get; }
        public RuntimeUiCompactControlPhase Phase { get; }
        public float PhaseStartedAt { get; }
        public float PhaseDuration { get; }
        public float StartingActiveAmount { get; }
    }

    public readonly struct RuntimeUiCompactControlVisualSample
    {
        private readonly bool resolved;
        private readonly RuntimeUiCompactControlPhase phase;
        private readonly float activeAmount;

        public RuntimeUiCompactControlVisualSample(RuntimeUiCompactControlPhase phase,
            float activeAmount)
        {
            if (!Enum.IsDefined(typeof(RuntimeUiCompactControlPhase), phase))
                throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            if (!IsUnit(activeAmount))
                throw new ArgumentOutOfRangeException(nameof(activeAmount));

            resolved = true;
            this.phase = phase;
            this.activeAmount = activeAmount;
        }

        public static RuntimeUiCompactControlVisualSample Inactive =>
            new RuntimeUiCompactControlVisualSample(
                RuntimeUiCompactControlPhase.Inactive, 0f);

        public static RuntimeUiCompactControlVisualSample Active =>
            new RuntimeUiCompactControlVisualSample(
                RuntimeUiCompactControlPhase.Active, 1f);

        public RuntimeUiCompactControlPhase Phase => resolved
            ? phase : RuntimeUiCompactControlPhase.Inactive;
        public float ActiveAmount => resolved ? activeAmount : 0f;

        private static bool IsUnit(float value)
        {
            return IsFinite(value) && value >= 0f && value <= 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct RuntimeUiCompactControlEvaluation
    {
        public RuntimeUiCompactControlEvaluation(RuntimeUiCompactControlState state,
            RuntimeUiCompactControlVisualSample sample)
        {
            State = state;
            Sample = sample;
        }

        public RuntimeUiCompactControlState State { get; }
        public RuntimeUiCompactControlVisualSample Sample { get; }
    }

    public static class RuntimeUiCompactControlLifecycle
    {
        private const float Epsilon = .0001f;

        public static RuntimeUiCompactControlState Reset(bool authoritativeActive,
            float unscaledTime)
        {
            ValidateTime(unscaledTime);
            return Stable(authoritativeActive, unscaledTime);
        }

        public static RuntimeUiCompactControlState Rebind(bool authoritativeActive,
            float unscaledTime)
        {
            return Reset(authoritativeActive, unscaledTime);
        }

        public static RuntimeUiCompactControlEvaluation Evaluate(
            RuntimeUiCompactControlState state, bool authoritativeActive,
            float unscaledTime, RuntimeUiFeedbackTokens tokens,
            bool reduceMotion = false)
        {
            ValidateTime(unscaledTime);
            var staticPresentation = reduceMotion || tokens.ReducedMotion;
            if (!state.IsBound || staticPresentation)
            {
                var stable = Stable(authoritativeActive, unscaledTime);
                return new RuntimeUiCompactControlEvaluation(stable,
                    authoritativeActive
                        ? RuntimeUiCompactControlVisualSample.Active
                        : RuntimeUiCompactControlVisualSample.Inactive);
            }

            ResolveCompletedTransition(ref state, unscaledTime);
            var current = Sample(state, unscaledTime, tokens);
            if (state.AuthoritativeActive != authoritativeActive)
            {
                state = Redirect(current, authoritativeActive,
                    unscaledTime, tokens);
                current = Sample(state, unscaledTime, tokens);
            }

            return new RuntimeUiCompactControlEvaluation(state, current);
        }

        public static RuntimeUiCompactControlVisualSample Sample(
            RuntimeUiCompactControlState state, float unscaledTime,
            RuntimeUiFeedbackTokens tokens, bool reduceMotion = false)
        {
            ValidateTime(unscaledTime);
            if (!state.IsBound)
                return RuntimeUiCompactControlVisualSample.Inactive;
            if (reduceMotion || tokens.ReducedMotion)
            {
                return state.AuthoritativeActive
                    ? RuntimeUiCompactControlVisualSample.Active
                    : RuntimeUiCompactControlVisualSample.Inactive;
            }

            var phase = state.Phase;
            switch (phase)
            {
                case RuntimeUiCompactControlPhase.Inactive:
                    return RuntimeUiCompactControlVisualSample.Inactive;
                case RuntimeUiCompactControlPhase.Active:
                    return RuntimeUiCompactControlVisualSample.Active;
                case RuntimeUiCompactControlPhase.Activating:
                case RuntimeUiCompactControlPhase.Deactivating:
                    var target = phase == RuntimeUiCompactControlPhase.Activating ? 1f : 0f;
                    if (state.PhaseDuration <= 0f
                        || unscaledTime + Epsilon
                            >= state.PhaseStartedAt + state.PhaseDuration)
                    {
                        if (target <= 0f)
                            return RuntimeUiCompactControlVisualSample.Inactive;
                        return RuntimeUiCompactControlVisualSample.Active;
                    }
                    var progress = Mathf.Clamp01((unscaledTime - state.PhaseStartedAt)
                        / state.PhaseDuration);
                    var eased = Smooth(progress);
                    var amount = Mathf.Lerp(state.StartingActiveAmount, target, eased);
                    return new RuntimeUiCompactControlVisualSample(
                        phase, amount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), phase, null);
            }
        }

        private static RuntimeUiCompactControlState Redirect(
            RuntimeUiCompactControlVisualSample current,
            bool authoritativeActive, float unscaledTime,
            RuntimeUiFeedbackTokens tokens)
        {
            var currentAmount = Mathf.Clamp01(current.ActiveAmount);
            if (authoritativeActive && currentAmount >= 1f - Epsilon)
                return Stable(true, unscaledTime);
            if (!authoritativeActive && currentAmount <= Epsilon)
                return Stable(false, unscaledTime);

            var fullDuration = authoritativeActive
                ? tokens.CompactControlActivateSeconds
                : tokens.CompactControlDeactivateSeconds;
            var remaining = authoritativeActive ? 1f - currentAmount : currentAmount;
            return new RuntimeUiCompactControlState(true, authoritativeActive,
                authoritativeActive
                    ? RuntimeUiCompactControlPhase.Activating
                    : RuntimeUiCompactControlPhase.Deactivating,
                unscaledTime, fullDuration * remaining, currentAmount);
        }

        private static void ResolveCompletedTransition(
            ref RuntimeUiCompactControlState state, float unscaledTime)
        {
            if (state.Phase != RuntimeUiCompactControlPhase.Activating
                && state.Phase != RuntimeUiCompactControlPhase.Deactivating)
                return;
            if (state.PhaseDuration > 0f
                && unscaledTime + Epsilon
                    < state.PhaseStartedAt + state.PhaseDuration)
                return;

            var stableStartedAt = state.PhaseStartedAt
                + Mathf.Max(0f, state.PhaseDuration);
            state = Stable(state.AuthoritativeActive, stableStartedAt);
        }

        private static RuntimeUiCompactControlState Stable(bool active,
            float unscaledTime)
        {
            return new RuntimeUiCompactControlState(true, active,
                active ? RuntimeUiCompactControlPhase.Active
                    : RuntimeUiCompactControlPhase.Inactive,
                unscaledTime, 0f, active ? 1f : 0f);
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static void ValidateTime(float unscaledTime)
        {
            if (float.IsNaN(unscaledTime) || float.IsInfinity(unscaledTime))
                throw new ArgumentOutOfRangeException(nameof(unscaledTime), unscaledTime,
                    "Compact-control feedback time must be finite.");
        }
    }
}
