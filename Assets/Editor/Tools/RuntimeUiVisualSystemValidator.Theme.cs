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
                new Color32(255, 249, 238, 255));
            ValidateApprovedColor(report, path, "colors.edgeBackground", theme.Colors.EdgeBackground,
                new Color32(115, 201, 244, 255));
            ValidateApprovedColor(report, path, "colors.raisedSurface", theme.Colors.RaisedSurface,
                new Color32(255, 241, 210, 255));
            ValidateApprovedColor(report, path, "colors.selectionAccent", theme.Colors.SelectionAccent,
                new Color32(255, 197, 66, 255));
            ValidateApprovedColor(report, path, "colors.success", theme.Colors.Success,
                new Color32(84, 169, 40, 255));
            ValidateApprovedColor(report, path, "colors.disabled", theme.Colors.Disabled,
                new Color32(167, 185, 155, 255));
            ValidateApprovedColor(report, path, "colors.primaryText", theme.Colors.PrimaryText,
                new Color32(86, 52, 31, 255));
            ValidateApprovedColor(report, path, "colors.warning", theme.Colors.Warning,
                new Color32(230, 154, 25, 255));
            ValidateApprovedColor(report, path, "colors.danger", theme.Colors.Danger,
                new Color32(200, 77, 63, 255));
            ValidateApprovedColor(report, path, "colors.scrim", theme.Colors.Scrim,
                new Color32(59, 36, 22, 255));
            ValidateApprovedColor(report, path, "colors.secondaryText", theme.Colors.SecondaryText,
                new Color32(111, 88, 70, 255));
            ValidateApprovedColor(report, path, "colors.inverseText", theme.Colors.InverseText,
                new Color32(255, 249, 238, 255));
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
            ValidateFontLicenseRecords(report, path);
            ValidateTypographyFonts(report, theme, path);
            ValidateNoLegacyFontField(report, path);

            ValidateApprovedActionColors(report, path, "actionStyles.primary",
                theme.ActionStyles.Primary, new Color32(160, 199, 61, 255),
                new Color32(86, 52, 31, 255));
            ValidateApprovedActionColors(report, path, "actionStyles.secondary",
                theme.ActionStyles.Secondary, new Color32(160, 199, 61, 255),
                new Color32(86, 52, 31, 255));
            ValidateApprovedActionColors(report, path, "actionStyles.quiet",
                theme.ActionStyles.Quiet, new Color32(255, 249, 238, 255),
                new Color32(86, 52, 31, 255));
            ValidateApprovedActionColors(report, path, "actionStyles.danger",
                theme.ActionStyles.Danger, new Color32(168, 56, 49, 255),
                new Color32(255, 249, 238, 255));
            ValidateApprovedActionColors(report, path, "actionStyles.modeActive",
                theme.ActionStyles.ModeActive, new Color32(255, 197, 66, 255),
                new Color32(59, 36, 22, 255));
            ValidateApprovedActionColors(report, path, "actionStyles.disabled",
                theme.ActionStyles.Disabled, new Color32(223, 227, 216, 255),
                new Color32(86, 52, 31, 255));

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

            if (style.ContainerColor.a < .999f || style.ContentColor.a < .999f)
            {
                report.Error("theme.action-style.opacity", path,
                    "Resolved action container and content must be opaque for "
                    + spec.Role + "/" + state + "/modeActive=" + modeActive + ".",
                    "Resolve complete opaque tokens instead of global alpha attenuation.");
            }
            ValidateContrast(report, path,
                "action-content/" + spec.Role + "/" + state
                + "/modeActive=" + modeActive,
                style.ContentColor, style.ContainerColor,
                RuntimeUiQualityProfile.NormalTextContrast);
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

        private static void ValidateApprovedActionColors(
            RuntimeUiVisualValidationReport report, string path, string token,
            RuntimeUiActionColorPair actual, Color expectedContainer, Color expectedContent)
        {
            ValidateApprovedColor(report, path, token + ".container",
                actual.Container, expectedContainer);
            ValidateApprovedColor(report, path, token + ".content",
                actual.Content, expectedContent);
        }

        private static void ValidateTypographyMinimum(RuntimeUiVisualValidationReport report,
            string path, RuntimeUiTypographyStyle style, RuntimeUiTypographyRole typographyRole,
            string role)
        {
            var minimum = RuntimeUiQualityProfile.MinimumFontSize(typographyRole);
            if (style.FontSize != minimum
                || style.LineHeight != RuntimeUiQualityProfile.LineHeight(typographyRole))
            {
                report.Error("theme.typography.minimum", path,
                    role + " must match the approved size/line-height profile ("
                    + minimum + "/" + RuntimeUiQualityProfile.LineHeight(typographyRole)
                    + ").",
                    "Restore the serialized typography role values.");
            }
        }

        private static void ValidateTypographyFonts(
            RuntimeUiVisualValidationReport report, RuntimeUiTheme theme, string themePath)
        {
            RuntimeUiDrawContext context = null;
            try
            {
                context = RuntimeUiDrawContext.Create(theme, 1f);
            }
            catch (Exception exception)
            {
                report.Error("theme.font.style-cache", themePath,
                    "Role-font style cache could not be created: " + exception.Message,
                    "Bind every typography role to its packaged static font.");
            }

            foreach (RuntimeUiTypographyRole role in Enum.GetValues(
                         typeof(RuntimeUiTypographyRole)))
            {
                var typography = theme.Typography.For(role);
                var expectedPath = RuntimeUiQualityProfile.UsesDisplayFace(role)
                    ? ProjectSetup.DisplayRuntimeUiFontPath
                    : ProjectSetup.ReadingRuntimeUiFontPath;
                ValidateFont(report, typography.Font, themePath, role, expectedPath);
                if (context == null || typography.Font == null) continue;

                var renderStyle = context.Styles.SingleLineText(
                    role, TextAnchor.MiddleCenter);
                var measurementStyle = context.Styles.SingleLineText(
                    role, TextAnchor.MiddleCenter);
                if (!ReferenceEquals(renderStyle, measurementStyle)
                    || !ReferenceEquals(renderStyle.font, typography.Font))
                {
                    report.Error("theme.font.measure-render-mismatch", themePath,
                        role + " does not use the same role font/style for measurement and drawing.",
                        "Resolve both operations from the shared role GUIStyle cache.");
                }
                if (renderStyle.fontStyle != FontStyle.Normal)
                {
                    report.Error("theme.font.synthesized-style", themePath,
                        role + " requests synthesized GUIStyle font styling.",
                        "Use the authored static font weight and FontStyle.Normal.");
                }
            }
        }

        private static void ValidateFont(RuntimeUiVisualValidationReport report, Font font,
            string themePath, RuntimeUiTypographyRole role, string expectedPath)
        {
            if (font == null)
            {
                report.Error("theme.font.null", themePath,
                    role + " has no packaged static font reference.",
                    "Assign the approved role font directly on the typography style.");
                return;
            }
            var path = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(font));
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                report.Error("theme.font.not-packaged", themePath,
                    "The Chinese font is not a project-packaged asset.",
                    "Assign a font asset stored under Assets.");
                return;
            }

            if (!string.Equals(path, expectedPath, StringComparison.Ordinal))
            {
                report.Error("theme.font.role-binding", themePath,
                    role + " resolves to '" + path + "' instead of '" + expectedPath + "'.",
                    "Bind the explicit approved static face for this typography role.");
            }

            var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
            if (importer == null)
            {
                report.Error("theme.font.importer", path,
                    "The packaged role font does not use Unity's TrueType font importer.",
                    "Import the approved static TTF as a project font asset.");
            }
            else
            {
                var absoluteFontPath = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", path));
                var metaPath = absoluteFontPath + ".meta";
                var importerRecord = File.Exists(metaPath)
                    ? File.ReadAllText(metaPath)
                    : string.Empty;
                if (!importerRecord.Contains("includeFontData: 1"))
                {
                    report.Error("theme.font.host-data", path,
                        "The role font is not configured to include its data in the player.",
                        "Enable packaged font data for deterministic WebGL rendering.");
                }
                if (!importerRecord.Contains("fallbackFontReferences: []"))
                {
                    report.Error("theme.font.fallback", path,
                        "The role font importer contains fallback font references.",
                        "Remove fallback references and cover the finite release glyph authority.");
                }
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

        private static void ValidateFontLicenseRecords(
            RuntimeUiVisualValidationReport report, string themePath)
        {
            const string licensePath = "Assets/Resources/Fonts/OFL-NotoSansSC.txt";
            const string displayLicensePath =
                "Assets/Resources/Fonts/OFL-SmileySans.txt";
            const string readmePath = "Assets/Resources/Fonts/README.md";
            var absoluteLicense = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", licensePath));
            var absoluteReadme = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", readmePath));
            var absoluteDisplayLicense = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", displayLicensePath));
            if (!File.Exists(absoluteLicense) || !File.Exists(absoluteDisplayLicense)
                || !File.Exists(absoluteReadme))
            {
                report.Error("theme.font.license-missing", themePath,
                    "The packaged role fonts lack their OFL license or source record.",
                    "Restore the project font license and deterministic source manifest.");
                return;
            }

            var record = File.ReadAllText(absoluteReadme);
            if (!record.Contains("SIL Open Font License 1.1")
                || !record.Contains(ProjectSetup.ReadingRuntimeUiFontPath)
                || !record.Contains(ProjectSetup.DisplayRuntimeUiFontPath)
                || !record.Contains(
                    "a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da")
                || !record.Contains(
                    "b447d7e781f08bc95c4c9f23ba71ed2b8ebb639aa7184485c71c4ca5afcd25c4"))
            {
                report.Error("theme.font.license-record", readmePath,
                    "The role-font record does not identify both outputs, the OFL license, and pinned source hash.",
                    "Record the deterministic font source, hash, weights, outputs, and license.");
            }
        }

        private static void ValidateNoLegacyFontField(
            RuntimeUiVisualValidationReport report, string themePath)
        {
            if (string.IsNullOrEmpty(themePath) || !themePath.EndsWith(".asset",
                    StringComparison.OrdinalIgnoreCase))
                return;
            var absolutePath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", themePath));
            if (!File.Exists(absolutePath)) return;
            var source = File.ReadAllText(absolutePath);
            var legacyField = "packaged" + "Chinese" + "Font";
            if (source.Contains(legacyField + ":"))
            {
                report.Error("theme.font.legacy-field", themePath,
                    "The release theme still serializes the removed single-font field.",
                    "Delete the legacy field and bind all seven typography roles explicitly.");
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
