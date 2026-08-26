using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FruitDefense.Editor
{
    public static partial class RuntimeUiVisualSystemValidator
    {
        private static void ValidateThemeWithCandidate(RuntimeUiVisualValidationReport report,
            RuntimeUiTheme theme, RuntimeUiArtSet candidate)
        {
            RuntimeUiTheme clone = null;
            try
            {
                clone = Object.Instantiate(theme);
                clone.hideFlags = HideFlags.HideAndDontSave;
                var serialized = new SerializedObject(clone);
                serialized.FindProperty("activeArtSet").objectReferenceValue = candidate;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AppendRuntimeValidation(report, clone.Validate(), AssetDatabase.GetAssetPath(theme),
                    "candidate theme clone");
                ValidateThemeContract(report, clone, AssetDatabase.GetAssetPath(theme));
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone);
            }
        }

        private static void ValidateThemeContract(RuntimeUiVisualValidationReport report,
            RuntimeUiTheme theme, string path)
        {
            if (!string.Equals(theme.ThemeId, ApprovedThemeId, StringComparison.Ordinal))
            {
                report.Error("theme.direction.identity", path,
                    "The approved A direction requires theme ID '" + ApprovedThemeId + "'.",
                    "Use the approved stable theme identity.");
            }

            ValidateApprovedColor(report, path, "colors.baseSurface", theme.Colors.BaseSurface,
                new Color32(255, 246, 224, 255));
            ValidateApprovedColor(report, path, "colors.edgeBackground", theme.Colors.EdgeBackground,
                new Color32(245, 221, 174, 255));
            ValidateApprovedColor(report, path, "colors.raisedSurface", theme.Colors.RaisedSurface,
                new Color32(255, 231, 163, 255));
            ValidateApprovedColor(report, path, "colors.selectionAccent", theme.Colors.SelectionAccent,
                new Color32(255, 210, 77, 255));
            ValidateApprovedColor(report, path, "colors.success", theme.Colors.Success,
                new Color32(109, 190, 75, 255));
            ValidateApprovedColor(report, path, "colors.disabled", theme.Colors.Disabled,
                new Color32(143, 191, 116, 255));
            ValidateApprovedColor(report, path, "colors.primaryText", theme.Colors.PrimaryText,
                new Color32(139, 94, 60, 255));
            ValidateApprovedColor(report, path, "colors.warning", theme.Colors.Warning,
                new Color32(255, 185, 66, 255));
            ValidateApprovedColor(report, path, "colors.danger", theme.Colors.Danger,
                new Color32(211, 78, 69, 255));
            ValidateApprovedColor(report, path, "colors.scrim", theme.Colors.Scrim,
                new Color32(61, 42, 32, 255));
            ValidateApprovedColor(report, path, "colors.secondaryText", theme.Colors.SecondaryText,
                new Color32(111, 90, 69, 255));
            ValidateApprovedColor(report, path, "colors.inverseText", theme.Colors.InverseText,
                new Color32(255, 246, 224, 255));
            if (Mathf.Abs(theme.Colors.Scrim.a - 1f) > ColorTolerance)
            {
                report.Error("theme.scrim.alpha-authority", path,
                    "The semantic scrim color must be opaque; Feedback.ScrimOpacity is the sole alpha authority.",
                    "Set colors.scrim alpha to 1.");
            }

            ValidateTypographyMinimum(report, path, theme.Typography.Display,
                RuntimeUiTypographyRole.Display, "display");
            ValidateTypographyMinimum(report, path, theme.Typography.ScreenTitle,
                RuntimeUiTypographyRole.ScreenTitle, "screenTitle");
            ValidateTypographyMinimum(report, path, theme.Typography.SectionTitle,
                RuntimeUiTypographyRole.SectionTitle, "sectionTitle");
            ValidateTypographyMinimum(report, path, theme.Typography.Body,
                RuntimeUiTypographyRole.Body, "body");
            ValidateTypographyMinimum(report, path, theme.Typography.ControlLabel,
                RuntimeUiTypographyRole.ControlLabel, "controlLabel");
            ValidateTypographyMinimum(report, path, theme.Typography.Metric,
                RuntimeUiTypographyRole.Metric, "metric");
            ValidateTypographyMinimum(report, path, theme.Typography.Supplemental,
                RuntimeUiTypographyRole.Supplemental, "supplemental");
            ValidateFont(report, theme.PackagedChineseFont, path);

            ValidateContrast(report, path, "primaryText/baseSurface", theme.Colors.PrimaryText,
                theme.Colors.BaseSurface, RuntimeUiQualityProfile.NormalTextContrast);
            ValidateContrast(report, path, "primaryText/raisedSurface", theme.Colors.PrimaryText,
                theme.Colors.RaisedSurface, RuntimeUiQualityProfile.NormalTextContrast);
            ValidateContrast(report, path, "inverseText/danger", theme.Colors.InverseText,
                theme.Colors.Danger, RuntimeUiQualityProfile.LargeOrBoldTextContrast);
            ValidateEmphasisTextContract(report, theme, path);
            ValidateActionStyleContract(report, theme, path);
            ValidateStateCoverage(report, theme.ActiveArtSet, path);
        }

        private static void ValidateEmphasisTextContract(
            RuntimeUiVisualValidationReport report, RuntimeUiTheme theme, string path)
        {
            if (!RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.Display)
                || !RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.ScreenTitle)
                || !RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.SectionTitle)
                || RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.Body)
                || RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.ControlLabel)
                || RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.Metric)
                || RuntimeUiGui.IsApprovedEmphasisRole(RuntimeUiTypographyRole.Supplemental))
            {
                report.Error("theme.emphasis.roles", path,
                    "True outline is not limited to the three approved emphasis roles.",
                    "Keep outline support on display, screen-title, and section-title only.");
                return;
            }

            try
            {
                var context = RuntimeUiDrawContext.Create(theme, 1f);
                var layout = RuntimeUiGui.ResolveEmphasisTextLayout(context,
                    new Rect(0f, 0f, 206f, 40f),
                    RuntimeUiTypographyRole.SectionTitle,
                    RuntimeUiTextTone.State, TextAnchor.MiddleCenter,
                    RuntimeUiInteractionState.Success);
                if (layout.OutlinePixels
                    != RuntimeUiQualityProfile.EmphasisOutlineCapturePixels)
                {
                    report.Error("theme.emphasis.outline-width", path,
                        "Reference emphasis outline resolved to " + layout.OutlinePixels
                        + " capture pixels instead of "
                        + RuntimeUiQualityProfile.EmphasisOutlineCapturePixels + ".",
                        "Restore the shared reference-raster outline width.");
                }
                ValidateContrast(report, path, "emphasis-outline/fill",
                    layout.OutlineColor, layout.FillColor,
                    RuntimeUiQualityProfile.NonTextContrast);
                if (!Mathf.Approximately(layout.FillColor.a, 1f)
                    || !Mathf.Approximately(layout.OutlineColor.a, 1f))
                {
                    report.Error("theme.emphasis.opaque-composition", path,
                        "Emphasis fill and outline are not both opaque.",
                        "Gate emphasis visibility before drawing and keep every repeated "
                        + "outline pass opaque; do not fade individual passes.");
                }
            }
            catch (Exception exception)
            {
                report.Error("theme.emphasis.resolve", path,
                    "The shared emphasis path failed to resolve: " + exception.Message,
                    "Restore the finite shared emphasis typography contract.");
            }
        }

        private static void ValidateActionStyleContract(
            RuntimeUiVisualValidationReport report, RuntimeUiTheme theme, string path)
        {
            var interactionStates = new[]
            {
                RuntimeUiInteractionState.Normal,
                RuntimeUiInteractionState.HoveredOrFocused,
                RuntimeUiInteractionState.Pressed,
                RuntimeUiInteractionState.Disabled,
            };
            var roles = new[]
            {
                RuntimeUiActionKind.Primary,
                RuntimeUiActionKind.Secondary,
                RuntimeUiActionKind.Quiet,
                RuntimeUiActionKind.Danger,
            };

            foreach (var role in roles)
            {
                var spec = new RuntimeUiActionSpec(role,
                    RuntimeUiActionContentForm.IconLabel,
                    RuntimeUiActionBehavior.Instantaneous);
                foreach (var state in interactionStates)
                {
                    ValidateResolvedActionStyle(report, theme, path, spec,
                        state, false,
                        state == RuntimeUiInteractionState.Disabled
                            ? RuntimeUiArtSlot.ActionQuiet
                            : ActionContainerSlot(role));
                }
            }

            foreach (var form in new[]
                     {
                         RuntimeUiActionContentForm.IconOnly,
                         RuntimeUiActionContentForm.CompactMultiplier,
                     })
            {
                var spec = new RuntimeUiActionSpec(RuntimeUiActionKind.Quiet,
                    form, RuntimeUiActionBehavior.PersistentMode);
                foreach (var state in interactionStates)
                {
                    ValidateResolvedActionStyle(report, theme, path, spec,
                        state, false, RuntimeUiArtSlot.ActionCompactControl);
                    ValidateResolvedActionStyle(report, theme, path, spec,
                        state, true, RuntimeUiArtSlot.ActionCompactControlActive);
                }
            }
        }

        private static void ValidateResolvedActionStyle(
            RuntimeUiVisualValidationReport report, RuntimeUiTheme theme, string path,
            RuntimeUiActionSpec spec, RuntimeUiInteractionState state,
            bool modeActive, RuntimeUiArtSlot expectedContainerSlot)
        {
            RuntimeUiResolvedActionStyle style;
            try
            {
                style = theme.ResolveActionStyle(spec, state, modeActive);
            }
            catch (Exception exception)
            {
                report.Error("theme.action-style.resolve", path,
                    spec.Role + "/" + spec.ContentForm + "/" + spec.Behavior
                    + "/" + state + "/modeActive=" + modeActive
                    + " failed to resolve: " + exception.Message,
                    "Restore the finite action-style resolver contract.");
                return;
            }

            var expectedVisualRole = state == RuntimeUiInteractionState.Disabled
                ? RuntimeUiActionVisualRole.Disabled
                : modeActive
                    ? RuntimeUiActionVisualRole.ModeActive
                    : (RuntimeUiActionVisualRole)spec.Role;
            if (style.Spec.Role != spec.Role
                || style.Spec.ContentForm != spec.ContentForm
                || style.Spec.Behavior != spec.Behavior
                || style.InteractionState != state
                || style.VisualRole != expectedVisualRole
                || style.ContainerSlot != expectedContainerSlot
                || style.ModeActive != modeActive
                || style.Disabled != (state == RuntimeUiInteractionState.Disabled))
            {
                report.Error("theme.action-style.mapping", path,
                    "Resolved action style changed one of its explicit role/form/behavior/state inputs for "
                    + spec.Role + "/" + spec.ContentForm + "/" + spec.Behavior
                    + "/" + state + "/modeActive=" + modeActive + ".",
                    "Return one complete pairing without inferring semantics from an icon or control name.");
            }

            if (style.ContainerColor.a < .999f || style.ContentColor.a < .999f
                || style.OutlineColor.a < .999f)
            {
                report.Error("theme.action-style.opacity", path,
                    "Resolved action container, content, and outline must be opaque for "
                    + spec.Role + "/" + state + "/modeActive=" + modeActive + ".",
                    "Resolve complete opaque tokens instead of global alpha attenuation.");
            }
            ValidateContrast(report, path,
                "action-content/" + spec.Role + "/" + state
                + "/modeActive=" + modeActive,
                style.ContentColor, style.ContainerColor,
                RuntimeUiQualityProfile.NormalTextContrast);
            ValidateContrast(report, path,
                "action-outline/" + spec.Role + "/" + state
                + "/modeActive=" + modeActive,
                style.OutlineColor, style.ContainerColor,
                RuntimeUiQualityProfile.NonTextContrast);
        }

        private static RuntimeUiArtSlot ActionContainerSlot(RuntimeUiActionKind role)
        {
            switch (role)
            {
                case RuntimeUiActionKind.Primary: return RuntimeUiArtSlot.ActionPrimary;
                case RuntimeUiActionKind.Secondary: return RuntimeUiArtSlot.ActionSecondary;
                case RuntimeUiActionKind.Quiet: return RuntimeUiArtSlot.ActionQuiet;
                case RuntimeUiActionKind.Danger: return RuntimeUiArtSlot.ActionDanger;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        private static void ValidateApprovedColor(RuntimeUiVisualValidationReport report,
            string path, string token, Color actual, Color expected)
        {
            if (Mathf.Abs(actual.r - expected.r) <= ColorTolerance
                && Mathf.Abs(actual.g - expected.g) <= ColorTolerance
                && Mathf.Abs(actual.b - expected.b) <= ColorTolerance)
                return;
            report.Error("theme.direction.palette", path,
                token + " does not match the approved A / Sunny Orchard palette.",
                "Restore the approved semantic color token; do not compensate in drawing code.");
        }

        private static void ValidateTypographyMinimum(RuntimeUiVisualValidationReport report,
            string path, RuntimeUiTypographyStyle style, RuntimeUiTypographyRole typographyRole,
            string role)
        {
            var minimum = RuntimeUiQualityProfile.MinimumFontSize(typographyRole);
            if (style.FontSize != minimum
                || style.LineHeight != RuntimeUiQualityProfile.LineHeight(typographyRole)
                || style.FontStyle != RuntimeUiQualityProfile.FontStyle(typographyRole))
            {
                report.Error("theme.typography.minimum", path,
                    role + " must match the approved size/line-height/style profile ("
                    + minimum + "/" + RuntimeUiQualityProfile.LineHeight(typographyRole)
                    + "/" + RuntimeUiQualityProfile.FontStyle(typographyRole) + ").",
                    "Restore the serialized typography role values.");
            }
        }

        private static void ValidateFont(RuntimeUiVisualValidationReport report, Font font,
            string themePath)
        {
            if (font == null) return;
            var path = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(font));
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                report.Error("theme.font.not-packaged", themePath,
                    "The Chinese font is not a project-packaged asset.",
                    "Assign a font asset stored under Assets.");
                return;
            }

            if (!RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph(font,
                    out var missingGlyph))
            {
                report.Error("theme.font.glyph-missing", path,
                    "The packaged font lacks required player-visible glyph '"
                    + missingGlyph + "'.",
                    "Use a packaged font covering the authoritative release UI glyph probe.");
            }
        }

        private static void ValidateContrast(RuntimeUiVisualValidationReport report, string path,
            string pair, Color foreground, Color background, float minimum)
        {
            var ratio = Contrast(foreground, background);
            if (ratio + .001f >= minimum) return;
            report.Error("theme.contrast", path,
                pair + " contrast is " + ratio.ToString("0.00") + ":1; approved minimum is "
                + minimum.ToString("0.0") + ":1.",
                "Correct the owning semantic color tokens.");
        }

        private static void ValidateStateCoverage(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet, string path)
        {
            if (Enum.GetValues(typeof(RuntimeUiInteractionState)).Length != 9)
            {
                report.Error("state.count", path,
                    "The visual contract must expose exactly nine component interaction states.",
                    "Restore Normal, HoveredOrFocused, Pressed, Disabled, Selected, Loading, Success, Warning, Error.");
            }
            if (Enum.GetValues(typeof(RuntimeUiComponentKind)).Length != 16
                || Enum.GetValues(typeof(RuntimeUiActionKind)).Length != 4
                || Enum.GetValues(typeof(RuntimeUiActionContentForm)).Length != 4
                || Enum.GetValues(typeof(RuntimeUiActionBehavior)).Length != 2
                || Enum.GetValues(typeof(RuntimeUiActionVisualRole)).Length != 6)
            {
                report.Error("component.coverage", path,
                    "The finite component/action role-form-behavior contract is incomplete.",
                    "Restore the approved component kinds, four action roles, four content forms, two behaviors, and six resolved visual roles.");
            }
            if (artSet == null) return;

            var nonColorCues = new[]
            {
                RuntimeUiArtSlot.MarkerSelected,
                RuntimeUiArtSlot.IndicatorDisabled,
                RuntimeUiArtSlot.IndicatorLoading,
                RuntimeUiArtSlot.IndicatorSuccess,
                RuntimeUiArtSlot.IndicatorWarning,
                RuntimeUiArtSlot.IndicatorError,
            };
            foreach (var slot in nonColorCues)
            {
                if (!artSet.TryGetBinding(slot, out var binding) || binding.Sprite == null)
                {
                    report.Error("state.non-color-cue", AssetDatabase.GetAssetPath(artSet),
                        "State '" + RuntimeUiArtSlots.SemanticId(slot) + "' lacks its non-color indicator.",
                        "Bind a dedicated indicator sprite for this state.");
                }
            }
            var distinctCueSprites = nonColorCues
                .Select(slot => artSet.TryGetBinding(slot, out var binding) ? binding.Sprite : null)
                .Where(sprite => sprite != null).Distinct().Count();
            if (distinctCueSprites != nonColorCues.Length)
            {
                report.Error("state.non-color-cue.duplicate", AssetDatabase.GetAssetPath(artSet),
                    "Selected/disabled/loading/success/warning/error must have distinct non-color cue art.",
                    "Use a visually and semantically distinct sprite for each state cue.");
            }
        }

        private static float Contrast(Color first, Color second)
        {
            var bright = Mathf.Max(Luminance(first), Luminance(second));
            var dark = Mathf.Min(Luminance(first), Luminance(second));
            return (bright + .05f) / (dark + .05f);
        }

        private static float Luminance(Color color)
        {
            return .2126f * Linear(color.r) + .7152f * Linear(color.g) + .0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= .03928f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        }
    }
}
