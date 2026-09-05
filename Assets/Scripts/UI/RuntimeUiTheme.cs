using UnityEngine;

namespace FruitDefense.UI
{
    [CreateAssetMenu(fileName = "RuntimeUiTheme",
        menuName = "Fruit Defense/UI/Runtime UI Theme")]
    public sealed class RuntimeUiTheme : ScriptableObject
    {
        [SerializeField] private string themeId = "ui.sunny-orchard";
        [SerializeField] private string revision = "3";
        [SerializeField] private RuntimeUiSemanticColors colors =
            RuntimeUiSemanticColors.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiActionStyleTokens actionStyles =
            RuntimeUiActionStyleTokens.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiTypographyTokens typography =
            RuntimeUiTypographyTokens.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiMetrics metrics =
            RuntimeUiMetrics.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiFeedbackTokens feedback =
            RuntimeUiFeedbackTokens.SunnyOrchardDefault();
        [SerializeField] private RuntimeUiArtSet activeArtSet;

        public string ThemeId => themeId;
        public string Revision => revision;
        public RuntimeUiSemanticColors Colors => colors;
        public RuntimeUiActionStyleTokens ActionStyles => actionStyles;
        public RuntimeUiTypographyTokens Typography => typography;
        public RuntimeUiMetrics Metrics => metrics;
        public RuntimeUiFeedbackTokens Feedback => feedback;
        public RuntimeUiArtSet ActiveArtSet => activeArtSet;

        public RuntimeUiValidationResult Validate()
        {
            var result = new RuntimeUiValidationResult();
            if (!RuntimeUiIdentity.IsValid(themeId))
            {
                result.Add("theme.identity", "themeId",
                    "Theme ID must be a stable lowercase semantic identifier.");
            }

            if (!RuntimeUiIdentity.IsValidRevision(revision))
            {
                result.Add("theme.revision", "revision",
                    "Revision must be a non-empty stable lowercase token.");
            }

            colors.AppendValidation(result, "colors");
            actionStyles.AppendValidation(result, "actionStyles");
            typography.AppendValidation(result, "typography");
            metrics.AppendValidation(result, "metrics");
            feedback.AppendValidation(result, "feedback");

            if (activeArtSet == null)
            {
                result.Add("theme.art-set.null", "activeArtSet",
                    "The release theme must reference exactly one active runtime UI art set.");
            }
            else
            {
                result.Append(activeArtSet.Validate(), "activeArtSet");
            }

            return result;
        }

        public RuntimeUiResolvedActionStyle ResolveActionStyle(
            RuntimeUiActionKind role, RuntimeUiActionContentForm contentForm,
            RuntimeUiActionBehavior behavior, RuntimeUiInteractionState interactionState,
            bool modeActive)
        {
            return ResolveActionStyle(new RuntimeUiActionSpec(role, contentForm, behavior),
                interactionState, modeActive);
        }

        public RuntimeUiResolvedActionStyle ResolveActionStyle(RuntimeUiActionSpec spec,
            RuntimeUiInteractionState interactionState, bool modeActive)
        {
            if (!System.Enum.IsDefined(typeof(RuntimeUiInteractionState), interactionState))
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(interactionState), interactionState, null);
            }
            if (modeActive && spec.Behavior != RuntimeUiActionBehavior.PersistentMode)
            {
                throw new System.ArgumentException(
                    "Only persistent mode actions can resolve a mode-active style.",
                    nameof(modeActive));
            }

            var disabledState = interactionState == RuntimeUiInteractionState.Disabled;
            var visualRole = disabledState
                ? RuntimeUiActionVisualRole.Disabled
                : modeActive
                    ? RuntimeUiActionVisualRole.ModeActive
                    : ResolveVisualRole(spec.Role);
            var pair = actionStyles.For(visualRole);
            return new RuntimeUiResolvedActionStyle(spec, interactionState, visualRole,
                ResolveContainerSlot(spec, modeActive, disabledState), pair,
                modeActive);
        }

        private static RuntimeUiActionVisualRole ResolveVisualRole(RuntimeUiActionKind role)
        {
            switch (role)
            {
                case RuntimeUiActionKind.Primary: return RuntimeUiActionVisualRole.Primary;
                case RuntimeUiActionKind.Secondary: return RuntimeUiActionVisualRole.Secondary;
                case RuntimeUiActionKind.Quiet: return RuntimeUiActionVisualRole.Quiet;
                case RuntimeUiActionKind.Danger: return RuntimeUiActionVisualRole.Danger;
                default: throw new System.ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        private static RuntimeUiArtSlot ResolveContainerSlot(RuntimeUiActionSpec spec,
            bool modeActive, bool disabled)
        {
            // Disabled is a complete low-emphasis pairing, not a faded role surface.
            // ActionQuiet is the finite light container owned by every production ArtSet,
            // including compact controls while they are unavailable.
            if (disabled)
                return RuntimeUiArtSlot.ActionQuiet;

            if (spec.ContentForm == RuntimeUiActionContentForm.CompactMultiplier
                || (spec.Role == RuntimeUiActionKind.Quiet
                    && spec.ContentForm == RuntimeUiActionContentForm.IconOnly))
            {
                return modeActive
                    ? RuntimeUiArtSlot.ActionCompactControlActive
                    : RuntimeUiArtSlot.ActionCompactControl;
            }

            switch (spec.Role)
            {
                case RuntimeUiActionKind.Primary: return RuntimeUiArtSlot.ActionPrimary;
                case RuntimeUiActionKind.Secondary: return RuntimeUiArtSlot.ActionSecondary;
                case RuntimeUiActionKind.Quiet: return RuntimeUiArtSlot.ActionQuiet;
                case RuntimeUiActionKind.Danger: return RuntimeUiArtSlot.ActionDanger;
                default: throw new System.ArgumentOutOfRangeException(
                    nameof(spec), spec.Role, null);
            }
        }

        public bool TryValidate(out string reason)
        {
            var validation = Validate();
            reason = validation.FirstIssueOr("ok");
            return validation.IsValid;
        }
    }
}
