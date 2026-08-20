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
    public enum RuntimeUiVisualIssueSeverity
    {
        Error = 0,
        Warning = 1,
    }

    public sealed class RuntimeUiVisualIssue
    {
        public RuntimeUiVisualIssue(RuntimeUiVisualIssueSeverity severity, string code,
            string assetPath, string message, string action)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            Message = message ?? string.Empty;
            Action = action ?? string.Empty;
        }

        public RuntimeUiVisualIssueSeverity Severity { get; }
        public string Code { get; }
        public string AssetPath { get; }
        public string Message { get; }
        public string Action { get; }

        public override string ToString()
        {
            var location = string.IsNullOrEmpty(AssetPath) ? string.Empty : " [" + AssetPath + "]";
            var action = string.IsNullOrEmpty(Action) ? string.Empty : " Fix: " + Action;
            return Severity + " " + Code + location + ": " + Message + action;
        }
    }

    public sealed class RuntimeUiVisualValidationReport
    {
        private readonly List<RuntimeUiVisualIssue> issues = new List<RuntimeUiVisualIssue>();

        public IReadOnlyList<RuntimeUiVisualIssue> Issues => issues;
        public bool IsValid => issues.All(issue => issue.Severity != RuntimeUiVisualIssueSeverity.Error);
        public int ErrorCount => issues.Count(issue => issue.Severity == RuntimeUiVisualIssueSeverity.Error);
        public int WarningCount => issues.Count(issue => issue.Severity == RuntimeUiVisualIssueSeverity.Warning);

        public void Error(string code, string path, string message, string action)
        {
            issues.Add(new RuntimeUiVisualIssue(RuntimeUiVisualIssueSeverity.Error,
                code, path, message, action));
        }

        public void Warning(string code, string path, string message, string action)
        {
            issues.Add(new RuntimeUiVisualIssue(RuntimeUiVisualIssueSeverity.Warning,
                code, path, message, action));
        }

        public void Append(RuntimeUiVisualValidationReport other)
        {
            if (other != null) issues.AddRange(other.issues);
        }

        public string Summary()
        {
            return IsValid
                ? "Valid (" + WarningCount + " warning(s))."
                : ErrorCount + " error(s), " + WarningCount + " warning(s).";
        }
    }

    public static class RuntimeUiVisualSystemValidator
    {
        private const string ManifestSchema = "fruit-defense.runtime-ui-art-manifest.v1";
        private const string ApprovedThemeId = "ui.sunny-orchard";
        private const string PaintedSetId = "sunny-orchard-painted";
        private const string SharedConsumerSetId = "sunny-orchard";
        private const float ColorTolerance = 1.1f / 255f;

        private static readonly string[] ForbiddenReleaseSegments =
        {
            "/Sources/", "/ReferenceBoards/", "/Fixtures/", "/evidence/",
        };

        public static RuntimeUiVisualValidationReport ValidateCandidate(RuntimeUiTheme releaseTheme,
            RuntimeUiArtSet candidate)
        {
            var report = new RuntimeUiVisualValidationReport();
            if (releaseTheme == null)
            {
                report.Error("release.theme.missing", RuntimeUiArtSetRegistry.ReleaseThemePath,
                    "The fixed release RuntimeUiTheme asset is missing.",
                    "Create the release theme at the fixed path before previewing an art set.");
                return report;
            }

            if (candidate == null)
            {
                report.Error("candidate.missing", string.Empty, "No candidate art set is selected.",
                    "Select one production RuntimeUiArtSet.");
                return report;
            }

            var candidatePath = AssetDatabase.GetAssetPath(candidate);
            if (!RuntimeUiArtSetRegistry.IsProductionSet(candidate))
            {
                report.Error("candidate.not-production", candidatePath,
                    "The candidate is not stored under the production art-set root.",
                    "Store candidate sets under " + RuntimeUiArtSetRegistry.ArtSetRoot + ".");
            }

            AppendRuntimeValidation(report, candidate.Validate(), candidatePath, "art-set");
            ValidateRegistryDuplicates(report,
                RuntimeUiArtSetRegistry.DiscoverProductionSets());
            ValidateRegistryIdentity(report, candidate);
            ValidateManifestAndBindings(report, candidate);
            ValidateForbiddenDependencies(report, candidatePath);
            ValidateThemeWithCandidate(report, releaseTheme, candidate);
            return report;
        }

        public static RuntimeUiVisualValidationReport ValidateRelease()
        {
            var report = new RuntimeUiVisualValidationReport();
            var theme = RuntimeUiArtSetRegistry.LoadReleaseTheme();
            if (theme == null)
            {
                report.Error("release.theme.missing", RuntimeUiArtSetRegistry.ReleaseThemePath,
                    "The fixed release RuntimeUiTheme asset is missing.",
                    "Create and bind the release theme at the fixed path.");
                return report;
            }

            var themePath = AssetDatabase.GetAssetPath(theme);
            AppendRuntimeValidation(report, theme.Validate(), themePath, "theme");
            ValidateThemeContract(report, theme, themePath);
            ValidateSingleReleaseTheme(report, theme);
            ValidateForbiddenDependencies(report, themePath);

            var sets = RuntimeUiArtSetRegistry.DiscoverProductionSets();
            ValidateRegistryDuplicates(report, sets);
            foreach (var set in sets)
            {
                AppendRuntimeValidation(report, set.Validate(), AssetDatabase.GetAssetPath(set),
                    "art-set");
                ValidateRegistryIdentity(report, set);
                ValidateManifestAndBindings(report, set);
                ValidateForbiddenDependencies(report, AssetDatabase.GetAssetPath(set));
            }

            if (theme.ActiveArtSet == null)
            {
                report.Error("release.active-set.missing", themePath,
                    "The release theme has no active production art set.",
                    "Activate one fully valid production set in the Visual System window.");
            }
            else if (!RuntimeUiArtSetRegistry.IsProductionSet(theme.ActiveArtSet))
            {
                report.Error("release.active-set.not-production", themePath,
                    "The release theme references an art set outside the production registry.",
                    "Activate a set discovered under " + RuntimeUiArtSetRegistry.ArtSetRoot + ".");
            }

            ValidateReleaseScenes(report, theme);
            return report;
        }

        public static void ValidateReleaseOrThrow()
        {
            var report = ValidateRelease();
            if (!report.IsValid)
                throw new InvalidOperationException(FormatReport(report));
            Debug.Log("Runtime UI visual system validation passed. " + report.Summary());
        }

        public static string FormatReport(RuntimeUiVisualValidationReport report)
        {
            if (report == null) return "No Runtime UI visual validation report.";
            return report.Summary() + Environment.NewLine
                + string.Join(Environment.NewLine, report.Issues.Select(issue => issue.ToString()));
        }

        private static void AppendRuntimeValidation(RuntimeUiVisualValidationReport report,
            RuntimeUiValidationResult runtimeResult, string assetPath, string owner)
        {
            if (runtimeResult == null)
            {
                report.Error("runtime-validation.null", assetPath,
                    "The " + owner + " runtime validator returned no result.",
                    "Repair the runtime validation contract.");
                return;
            }

            foreach (var issue in runtimeResult.Issues)
            {
                report.Error("runtime." + issue.Code, assetPath,
                    issue.Field + ": " + issue.Message,
                    "Correct the serialized " + owner + " contract value.");
            }
        }

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
            ValidateApprovedColor(report, path, "colors.primaryAction", theme.Colors.PrimaryAction,
                new Color32(85, 154, 57, 255));
            ValidateApprovedColor(report, path, "colors.success", theme.Colors.Success,
                new Color32(109, 190, 75, 255));
            ValidateApprovedColor(report, path, "colors.secondaryAction", theme.Colors.SecondaryAction,
                new Color32(143, 191, 116, 255));
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
            ValidateContrast(report, path, "inverseText/primaryAction", theme.Colors.InverseText,
                theme.Colors.PrimaryAction, RuntimeUiQualityProfile.LargeOrBoldTextContrast);
            ValidateContrast(report, path, "inverseText/danger", theme.Colors.InverseText,
                theme.Colors.Danger, RuntimeUiQualityProfile.LargeOrBoldTextContrast);
            ValidateStateCoverage(report, theme.ActiveArtSet, path);
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
                || Enum.GetValues(typeof(RuntimeUiActionKind)).Length != 4)
            {
                report.Error("component.coverage", path,
                    "The finite component/action contract is incomplete.",
                    "Restore all existing component kinds and four action roles.");
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

        private static void ValidateRegistryIdentity(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet)
        {
            var setPath = AssetDatabase.GetAssetPath(artSet);
            if (!Directory.Exists(ToAbsolute(RuntimeUiArtSetRegistry.RuntimeDirectory(artSet))))
            {
                report.Error("art-set.runtime-directory", setPath,
                    "No matching Runtime/<setId> directory exists.",
                    "Export runtime PNGs to " + RuntimeUiArtSetRegistry.RuntimeDirectory(artSet) + ".");
            }
            if (!Directory.Exists(ToAbsolute(RuntimeUiArtSetRegistry.SourceDirectory(artSet))))
            {
                report.Error("art-set.source-directory", setPath,
                    "No matching Sources/<setId> directory exists.",
                    "Store source masters and manifest under " + RuntimeUiArtSetRegistry.SourceDirectory(artSet) + ".");
            }
        }

        private static void ValidateRegistryDuplicates(RuntimeUiVisualValidationReport report,
            IReadOnlyList<RuntimeUiArtSet> sets)
        {
            foreach (var group in sets.GroupBy(set => set.SetId + "\n" + set.Revision,
                         StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                report.Error("registry.identity.duplicate", RuntimeUiArtSetRegistry.ArtSetRoot,
                    "More than one production art set has identity/revision '"
                    + group.First().SetId + "@" + group.First().Revision + "'.",
                    "Keep exactly one production asset for each stable identity/revision.");
            }
        }

        private static void ValidateManifestAndBindings(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet)
        {
            var manifestPath = RuntimeUiArtSetRegistry.ManifestPath(artSet);
            var absoluteManifest = ToAbsolute(manifestPath);
            if (!File.Exists(absoluteManifest))
            {
                report.Error("manifest.missing", manifestPath,
                    "The art set has no source/runtime ownership manifest.",
                    "Generate art_manifest.json in the matching source directory.");
                return;
            }

            ArtManifest manifest;
            try
            {
                manifest = JsonUtility.FromJson<ArtManifest>(File.ReadAllText(absoluteManifest));
            }
            catch (Exception exception)
            {
                report.Error("manifest.parse", manifestPath, exception.Message,
                    "Repair the JSON manifest.");
                return;
            }
            if (manifest == null || manifest.bindings == null)
            {
                report.Error("manifest.contract", manifestPath,
                    "The manifest or bindings array is null.", "Restore the v1 manifest contract.");
                return;
            }

            if (manifest.importContract == null)
            {
                report.Error("manifest.import-contract", manifestPath,
                    "The manifest importContract is missing.",
                    "Restore the v1 standalone Sprite Single import contract.");
                return;
            }

            if (manifest.schema != ManifestSchema || manifest.setId != artSet.SetId
                || manifest.revision != artSet.Revision
                || manifest.slotCount != RuntimeUiArtSlots.RequiredCount
                || manifest.bindings.Length != RuntimeUiArtSlots.RequiredCount
                || !Nearly(manifest.sourceScale,
                    RuntimeUiQualityProfile.ProductionPixelsPerLogicalUnit)
                || manifest.importContract.pixelsPerUnit
                    != RuntimeUiQualityProfile.ProductionImporterPixelsPerUnit)
            {
                report.Error("manifest.identity", manifestPath,
                    "Schema, set identity/revision, or finite "
                    + RuntimeUiArtSlots.RequiredCount
                    + "-slot count does not match the asset.",
                    "Regenerate the manifest from this exact art-set revision.");
            }

            var manifestBySlot = new Dictionary<int, ArtManifestBinding>();
            foreach (var row in manifest.bindings)
            {
                if (row == null || manifestBySlot.ContainsKey(row.slot))
                {
                    report.Error("manifest.slot.duplicate", manifestPath,
                        "The manifest contains a null or duplicate slot row.",
                        "Emit exactly one row for each slot 0-"
                        + (RuntimeUiArtSlots.RequiredCount - 1) + ".");
                    continue;
                }
                manifestBySlot.Add(row.slot, row);
            }

            var ownedRuntimePaths = new HashSet<string>(StringComparer.Ordinal);
            var ownedSourcePaths = new HashSet<string>(StringComparer.Ordinal);
            var referencePpu = -1f;
            foreach (var binding in artSet.Bindings)
            {
                if (binding == null) continue;
                if (!manifestBySlot.TryGetValue((int)binding.Slot, out var row))
                {
                    report.Error("manifest.slot.missing", manifestPath,
                        "No manifest row owns " + RuntimeUiArtSlots.SemanticId(binding.Slot) + ".",
                        "Add the exact binding row.");
                    continue;
                }

                ValidateManifestRow(report, artSet, binding, row, manifest, manifestPath,
                    ownedRuntimePaths, ownedSourcePaths);
                ValidateImportedBinding(report, artSet, binding, row, manifest);
                if (referencePpu < 0f) referencePpu = binding.PixelsPerLogicalUnit;
                else if (!Nearly(binding.PixelsPerLogicalUnit, referencePpu))
                {
                    report.Error("art-set.ppu.inconsistent", AssetDatabase.GetAssetPath(artSet),
                        "Bindings use inconsistent logical source scale.",
                        "Use one pixelsPerLogicalUnit value for the full set.");
                }
            }

            if (ownedRuntimePaths.Count != manifest.uniqueExportCount)
            {
                report.Error("manifest.export-count", manifestPath,
                    "uniqueExportCount does not match referenced runtime exports.",
                    "Regenerate the manifest ownership summary.");
            }
            if (string.Equals(artSet.SetId, PaintedSetId, StringComparison.Ordinal)
                && manifest.uniqueExportCount
                    != RuntimeUiQualityProfile.PaintedUniqueExportCount)
            {
                report.Error("manifest.export-count.painted", manifestPath,
                    "The painted production set must own exactly "
                    + RuntimeUiQualityProfile.PaintedUniqueExportCount
                    + " unique runtime exports for 49 semantic bindings.",
                    "Restore the reviewed continue/start/start-wave sharing contract.");
            }
            ValidateNoUnownedFiles(report, RuntimeUiArtSetRegistry.RuntimeDirectory(artSet), "*.png",
                ownedRuntimePaths, "runtime");
            ValidateNoUnownedFiles(report, RuntimeUiArtSetRegistry.SourceDirectory(artSet), "*.svg",
                ownedSourcePaths, "source");
            ValidateNoUnownedFiles(report, RuntimeUiArtSetRegistry.SourceDirectory(artSet), "*.png",
                ownedSourcePaths, "source");
            ValidateProductionAncillaryFiles(report, artSet);
        }

        private static void ValidateManifestRow(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet, RuntimeUiArtBinding binding, ArtManifestBinding row,
            ArtManifest manifest, string manifestPath, HashSet<string> runtimePaths,
            HashSet<string> sourcePaths)
        {
            var semantic = RuntimeUiArtSlots.SemanticId(binding.Slot);
            var runtime = RuntimeUiArtSetRegistry.Normalize(row.runtime);
            var source = RuntimeUiArtSetRegistry.Normalize(row.source);
            var sharedOwner = string.IsNullOrWhiteSpace(row.shared_from_set)
                ? string.Empty
                : row.shared_from_set.Trim();
            var expectedRuntimeDirectory = string.IsNullOrEmpty(sharedOwner)
                ? RuntimeUiArtSetRegistry.RuntimeDirectory(artSet)
                : RuntimeUiArtSetRegistry.RuntimeArtRoot + "/" + sharedOwner;
            var expectedSourceDirectory = string.IsNullOrEmpty(sharedOwner)
                ? RuntimeUiArtSetRegistry.SourceDirectory(artSet)
                : RuntimeUiArtSetRegistry.SourceArtRoot + "/" + sharedOwner;
            var sharedSlot = (int)binding.Slot >= (int)RuntimeUiArtSlot.OrnamentScreenCorner;
            if (string.Equals(artSet.SetId, PaintedSetId, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(sharedOwner))
            {
                report.Error("manifest.shared-owner.painted", manifestPath,
                    "The painted owner set must keep all 49 bindings local.",
                    "Remove shared_from_set from the painted manifest.");
            }
            else if (string.Equals(artSet.SetId, SharedConsumerSetId,
                         StringComparison.Ordinal)
                     && ((!sharedSlot && !string.IsNullOrEmpty(sharedOwner))
                         || (sharedSlot && !string.Equals(sharedOwner, PaintedSetId,
                             StringComparison.Ordinal))))
            {
                report.Error("manifest.shared-owner.policy", manifestPath,
                    "sunny-orchard may share only slots 40-48 directly from "
                    + PaintedSetId + ".",
                    "Keep slots 0-39 local and declare painted as the direct owner for 40-48.");
            }
            else if (!string.Equals(artSet.SetId, SharedConsumerSetId,
                         StringComparison.Ordinal)
                     && !string.IsNullOrEmpty(sharedOwner))
            {
                report.Error("manifest.shared-owner.unapproved", manifestPath,
                    "This production set is not an approved mixed-set consumer.",
                    "Own the binding locally; do not introduce a sharing chain.");
            }
            if (string.IsNullOrEmpty(sharedOwner))
            {
                runtimePaths.Add(runtime);
                sourcePaths.Add(source);
            }
            if (row.semantic_id != semantic || row.geometry != GeometryName(binding.Geometry)
                || !runtime.StartsWith(expectedRuntimeDirectory + "/", StringComparison.Ordinal)
                || !source.StartsWith(expectedSourceDirectory + "/", StringComparison.Ordinal)
                || !Nearly(row.pixels_per_logical_unit, binding.PixelsPerLogicalUnit)
                || !Nearly(manifest.sourceScale, binding.PixelsPerLogicalUnit))
            {
                report.Error("manifest.binding.contract", manifestPath,
                    "Manifest row " + row.slot + " does not match " + semantic
                    + " identity, geometry, directory, or logical scale.",
                    "Regenerate this binding row from the production asset.");
            }

            if (!string.IsNullOrEmpty(sharedOwner))
                ValidateSharedManifestOwner(report, artSet, binding, row, sharedOwner,
                    manifestPath);

            var uniformSlice = UniformInset(binding.SliceBorder);
            var uniformSafe = UniformInset(binding.SafeInset);
            if (uniformSlice != row.slice_border || uniformSafe != row.safe_inset)
            {
                report.Error("manifest.binding.insets", manifestPath,
                    semantic + " manifest insets differ from the serialized binding.",
                    "Keep manifest and binding slice/safe inset metadata identical.");
            }

            ValidateOwnedFile(report, source, row.sourceSha256, string.Empty, "source");
            ValidateOwnedFile(report, runtime, row.runtimeSha256, row.guid, "runtime");
            var texturePath = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(binding.Texture));
            var spritePath = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(binding.Sprite));
            if (texturePath != runtime || spritePath != runtime)
            {
                report.Error("binding.asset-path", AssetDatabase.GetAssetPath(artSet),
                    semantic + " Texture/Sprite do not both point to the manifest runtime PNG.",
                    "Bind both references to the standalone Sprite Single asset at " + runtime + ".");
            }
        }

        private static void ValidateSharedManifestOwner(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet,
            RuntimeUiArtBinding binding, ArtManifestBinding row, string sharedOwner,
            string manifestPath)
        {
            if (string.Equals(sharedOwner, artSet.SetId, StringComparison.Ordinal))
            {
                report.Error("manifest.shared-owner.self", manifestPath,
                    "A shared binding cannot name its own set as owner.",
                    "Remove shared_from_set or name the production set that owns the files.");
                return;
            }

            var owners = RuntimeUiArtSetRegistry.DiscoverProductionSets()
                .Where(candidate => string.Equals(candidate.SetId, sharedOwner,
                    StringComparison.Ordinal))
                .ToArray();
            if (owners.Length != 1)
            {
                report.Error("manifest.shared-owner.identity", manifestPath,
                    "Shared binding owner " + sharedOwner
                    + " must resolve to exactly one production ArtSet.",
                    "Restore the uniquely identified owning production set.");
                return;
            }

            var owner = owners[0];
            var ownerBinding = owner.Bindings.FirstOrDefault(candidate =>
                candidate != null && candidate.Slot == binding.Slot);
            var runtime = RuntimeUiArtSetRegistry.Normalize(row.runtime);
            var ownerRuntime = ownerBinding == null
                ? string.Empty
                : RuntimeUiArtSetRegistry.Normalize(
                    AssetDatabase.GetAssetPath(ownerBinding.Texture));
            if (ownerBinding == null || ownerRuntime != runtime)
            {
                report.Error("manifest.shared-owner.binding", manifestPath,
                    RuntimeUiArtSlots.SemanticId(binding.Slot)
                    + " does not resolve to the same runtime asset in owner "
                    + sharedOwner + ".",
                    "Bind both sets to the owner's exact semantic runtime asset.");
                return;
            }

            var ownerManifestPath = RuntimeUiArtSetRegistry.ManifestPath(owner);
            ArtManifest ownerManifest;
            try
            {
                ownerManifest = JsonUtility.FromJson<ArtManifest>(
                    File.ReadAllText(ToAbsolute(ownerManifestPath)));
            }
            catch (Exception exception)
            {
                report.Error("manifest.shared-owner.parse", ownerManifestPath,
                    exception.Message, "Restore the owning set manifest.");
                return;
            }
            var ownerRow = ownerManifest?.bindings?.FirstOrDefault(candidate =>
                candidate != null && candidate.slot == row.slot);
            if (ownerRow == null
                || !string.IsNullOrWhiteSpace(ownerRow.shared_from_set)
                || ownerRow.semantic_id != row.semantic_id
                || ownerRow.geometry != row.geometry
                || ownerRow.size != row.size
                || ownerRow.width != row.width
                || ownerRow.height != row.height
                || ownerRow.slice_border != row.slice_border
                || ownerRow.safe_inset != row.safe_inset
                || !Nearly(ownerRow.pixels_per_logical_unit,
                    row.pixels_per_logical_unit)
                || RuntimeUiArtSetRegistry.Normalize(ownerRow.source)
                    != RuntimeUiArtSetRegistry.Normalize(row.source)
                || RuntimeUiArtSetRegistry.Normalize(ownerRow.runtime) != runtime
                || !string.Equals(ownerRow.sourceSha256, row.sourceSha256,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(ownerRow.runtimeSha256, row.runtimeSha256,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(ownerRow.guid, row.guid,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Error("manifest.shared-owner.contract", manifestPath,
                    "Shared binding does not exactly mirror the owning manifest row.",
                    "Copy the owner's semantic paths, hashes and GUID without chaining owners.");
            }
        }

        private static void ValidateOwnedFile(RuntimeUiVisualValidationReport report, string path,
            string expectedHash, string expectedGuid, string kind)
        {
            var absolute = ToAbsolute(path);
            if (!File.Exists(absolute))
            {
                report.Error("manifest." + kind + ".missing", path,
                    "Owned " + kind + " file is missing.", "Restore the manifest-owned file.");
                return;
            }
            if (!string.Equals(Sha256(absolute), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                report.Error("manifest." + kind + ".hash", path,
                    "The file hash differs from the manifest.",
                    "Re-export intentionally and update the manifest in one change.");
            }
            if (!string.IsNullOrEmpty(expectedGuid)
                && !string.Equals(AssetDatabase.AssetPathToGUID(path), expectedGuid,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Error("manifest." + kind + ".guid", path,
                    "The imported GUID differs from the manifest.",
                    "Preserve the production .meta GUID or regenerate the manifest intentionally.");
            }
        }

        private static void ValidateImportedBinding(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet, RuntimeUiArtBinding binding, ArtManifestBinding row,
            ArtManifest manifest)
        {
            var path = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(binding.Texture));
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                report.Error("importer.missing", path, "Runtime PNG has no TextureImporter.",
                    "Import it as a standalone Sprite (2D and UI).");
                return;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var wrong = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || settings.spriteMeshType != SpriteMeshType.FullRect
                || !importer.sRGBTexture || !importer.alphaIsTransparency
                || importer.wrapModeU != TextureWrapMode.Clamp
                || importer.wrapModeV != TextureWrapMode.Clamp
                || importer.wrapModeW != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear
                || importer.mipmapEnabled || importer.isReadable
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.npotScale != TextureImporterNPOTScale.None
                || !Nearly(importer.spritePixelsPerUnit,
                    RuntimeUiQualityProfile.ProductionImporterPixelsPerUnit)
                || manifest.importContract.pixelsPerUnit
                    != RuntimeUiQualityProfile.ProductionImporterPixelsPerUnit
                || !Nearly(binding.PixelsPerLogicalUnit,
                    RuntimeUiQualityProfile.ProductionPixelsPerLogicalUnit);
            var standaloneOverride = importer.GetPlatformTextureSettings("Standalone");
            var webGlOverride = importer.GetPlatformTextureSettings("WebGL");
            wrong |= standaloneOverride.overridden || webGlOverride.overridden;
            if (wrong)
            {
                report.Error("importer.contract", path,
                    "Importer must be Sprite Single/FullRect, sRGB+alpha, Clamp/Bilinear, no mip/read-write/compression, and unified PPU.",
                    "Apply the manifest import contract without platform overrides.");
            }

            var border = importer.spriteBorder;
            var expected = binding.SliceBorder;
            if (!Nearly(border.x, expected.Left) || !Nearly(border.y, expected.Bottom)
                || !Nearly(border.z, expected.Right) || !Nearly(border.w, expected.Top))
            {
                report.Error("importer.border", path,
                    "Sprite border differs from the binding slice metadata.",
                    "Set importer border left/bottom/right/top to the serialized slice border.");
            }
            if (binding.Geometry != RuntimeUiArtGeometry.NineSlice
                && (expected.Left != 0 || expected.Right != 0
                    || expected.Top != 0 || expected.Bottom != 0))
            {
                report.Error("binding.border.non-nine-slice", path,
                    "Stretch/Icon bindings must not serialize a slice border.",
                    "Set the binding and importer border to zero.");
            }

            if (binding.Sprite != null)
            {
                var rect = binding.Sprite.rect;
                if (binding.Sprite.packed || !Nearly(rect.x, 0f) || !Nearly(rect.y, 0f)
                    || !Nearly(rect.width, binding.Texture.width)
                    || !Nearly(rect.height, binding.Texture.height))
                {
                    report.Error("sprite.standalone-fullrect", path,
                        "Every binding must be an unpacked full-texture standalone sprite.",
                        "Disable atlas packing and restore Sprite Single / Full Rect.");
                }
                var expectedWidth = row.width > 0 ? row.width : row.size;
                var expectedHeight = row.height > 0 ? row.height : row.size;
                if (expectedWidth <= 0 || expectedHeight <= 0
                    || expectedWidth != binding.Texture.width
                    || expectedHeight != binding.Texture.height)
                {
                    report.Error("manifest.canvas-size", path,
                        "Manifest canvas dimensions differ from the imported texture.",
                        "Record width/height for fixed-aspect art or size for square art.");
                }
            }

            if (binding.Geometry == RuntimeUiArtGeometry.NineSlice
                && (binding.Texture.width - binding.SliceBorder.Horizontal <= 0
                    || binding.Texture.height - binding.SliceBorder.Vertical <= 0))
            {
                report.Error("nine-slice.center", path,
                    "Nine-slice borders leave no positive center region.",
                    "Reduce the slice border or increase the source canvas.");
            }
            ValidatePixelQuality(report, path, binding, row);
            ValidateIllustrationSourceAspect(report, binding, row);
        }

        internal static bool IsFullyOpaque(Color32[] pixels,
            out int nonOpaquePixelCount, out byte minimumAlpha)
        {
            nonOpaquePixelCount = 0;
            minimumAlpha = byte.MaxValue;
            if (pixels == null || pixels.Length == 0)
            {
                minimumAlpha = 0;
                return false;
            }

            for (var index = 0; index < pixels.Length; index++)
            {
                var alpha = pixels[index].a;
                if (alpha < minimumAlpha) minimumAlpha = alpha;
                if (alpha != byte.MaxValue) nonOpaquePixelCount++;
            }

            return nonOpaquePixelCount == 0;
        }

        private static void ValidatePixelQuality(RuntimeUiVisualValidationReport report,
            string assetPath, RuntimeUiArtBinding binding, ArtManifestBinding row)
        {
            var texture = DecodePng(report, assetPath, "runtime-png.decode");
            if (texture == null) return;
            try
            {
                var pixels = texture.GetPixels32();
                ValidateVisibleMagenta(report, assetPath, pixels, texture.width);

                if (RequiresOpaquePixels(binding.Slot)
                    && !IsFullyOpaque(pixels, out var nonOpaqueCount, out var minimumAlpha))
                {
                    report.Error("runtime-png.alpha.opaque", assetPath,
                        RuntimeUiArtSlots.SemanticId(binding.Slot) + " contains "
                        + nonOpaqueCount + " non-opaque pixel(s); minimum alpha is "
                        + minimumAlpha + ".",
                        "Export the full-canvas background/illustration with alpha 255.");
                }

                if (RequiresTransparentOuterEdge(binding))
                    ValidateTransparentOuterEdge(report, assetPath, pixels,
                        texture.width, texture.height);

                if (binding.Geometry == RuntimeUiArtGeometry.Icon)
                {
                    ValidateTransparentPadding(report, assetPath, binding, pixels,
                        texture.width, texture.height);
                    if (binding.Slot != RuntimeUiArtSlot.OrnamentScreenCorner)
                        ValidateCommonIconOptics(report, assetPath, binding,
                            pixels, texture.width, texture.height);
                }

                if (binding.Geometry == RuntimeUiArtGeometry.NineSlice)
                    ValidateNineSlicePixels(report, assetPath, binding, pixels,
                        texture.width, texture.height);

                ValidateFixedAspectOrnament(report, assetPath, binding, row,
                    texture.width, texture.height);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static Texture2D DecodePng(RuntimeUiVisualValidationReport report,
            string assetPath, string issueCode)
        {
            var absolute = ToAbsolute(assetPath);
            if (!File.Exists(absolute))
            {
                report.Error(issueCode, assetPath, "PNG file is missing.",
                    "Restore the manifest-owned deterministic export.");
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (texture.LoadImage(File.ReadAllBytes(absolute), false))
                return texture;
            Object.DestroyImmediate(texture);
            report.Error(issueCode, assetPath, "PNG could not be decoded.",
                "Re-export a valid deterministic RGBA PNG.");
            return null;
        }

        private static void ValidateVisibleMagenta(RuntimeUiVisualValidationReport report,
            string assetPath, Color32[] pixels, int width)
        {
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.a == 0 || pixel.r != byte.MaxValue || pixel.g != 0
                    || pixel.b != byte.MaxValue)
                    continue;
                report.Error("runtime-png.edge-contamination", assetPath,
                    "Visible exact #FF00FF contamination at (" + (index % width) + ","
                    + (index / width) + ") with alpha " + pixel.a + ".",
                    "Clean the reviewed master and re-export in place without changing the GUID.");
                return;
            }
        }

        private static bool RequiresOpaquePixels(RuntimeUiArtSlot slot)
        {
            return slot == RuntimeUiArtSlot.SurfaceScreenBackground
                || slot == RuntimeUiArtSlot.SurfaceScrim
                || slot == RuntimeUiArtSlot.IllustrationOrchardVista
                || slot == RuntimeUiArtSlot.IllustrationLobbyOrchard01
                || slot == RuntimeUiArtSlot.IllustrationLobbyOrchard02
                || slot == RuntimeUiArtSlot.IllustrationLobbyOrchard03;
        }

        private static bool RequiresTransparentOuterEdge(RuntimeUiArtBinding binding)
        {
            return binding.Geometry == RuntimeUiArtGeometry.Icon
                || binding.Slot == RuntimeUiArtSlot.OrnamentMetricDivider
                || binding.Slot == RuntimeUiArtSlot.OrnamentResultBanner;
        }

        private static void ValidateTransparentOuterEdge(
            RuntimeUiVisualValidationReport report, string assetPath, Color32[] pixels,
            int width, int height)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixels[x].a != RuntimeUiQualityProfile.TransparentEdgeRequiredAlpha
                    || pixels[(height - 1) * width + x].a
                        != RuntimeUiQualityProfile.TransparentEdgeRequiredAlpha)
                {
                    report.Error("runtime-png.outer-edge", assetPath,
                        "Transparent art must have alpha 0 on every outer-edge pixel.",
                        "Restore transparent padding around the reviewed art.");
                    return;
                }
            }
            for (var y = 1; y < height - 1; y++)
            {
                if (pixels[y * width].a != RuntimeUiQualityProfile.TransparentEdgeRequiredAlpha
                    || pixels[y * width + width - 1].a
                        != RuntimeUiQualityProfile.TransparentEdgeRequiredAlpha)
                {
                    report.Error("runtime-png.outer-edge", assetPath,
                        "Transparent art must have alpha 0 on every outer-edge pixel.",
                        "Restore transparent padding around the reviewed art.");
                    return;
                }
            }
        }

        private static void ValidateCommonIconOptics(
            RuntimeUiVisualValidationReport report, string assetPath,
            RuntimeUiArtBinding binding,
            Color32[] pixels, int width, int height)
        {
            if (width != RuntimeUiQualityProfile.CommonIconCanvasSize
                || height != RuntimeUiQualityProfile.CommonIconCanvasSize
                || binding.SafeInset.Left != RuntimeUiQualityProfile.CommonIconSafeInset
                || binding.SafeInset.Right != RuntimeUiQualityProfile.CommonIconSafeInset
                || binding.SafeInset.Top != RuntimeUiQualityProfile.CommonIconSafeInset
                || binding.SafeInset.Bottom != RuntimeUiQualityProfile.CommonIconSafeInset)
            {
                report.Error("icon.canvas.quality-profile", assetPath,
                    "Common icon/state art must use the 96x96 quality-profile canvas.",
                    "Re-export on the reviewed common icon canvas.");
                return;
            }

            if (!TryAlphaMetrics(pixels, width, height, out var bounds,
                    out var centroid))
            {
                report.Error("icon.alpha.empty", assetPath,
                    "Icon has no visible alpha mass.", "Restore the reviewed icon art.");
                return;
            }

            var major = Mathf.Max(bounds.width, bounds.height);
            if (major < RuntimeUiQualityProfile.CommonIconAlphaDimensionMinimum
                || major > RuntimeUiQualityProfile.CommonIconAlphaDimensionMaximum)
            {
                report.Error("icon.optical.family-size", assetPath,
                    "Alpha-bounds major dimension is " + major
                    + "px; the common family range is "
                    + RuntimeUiQualityProfile.CommonIconAlphaDimensionMinimum + "-"
                    + RuntimeUiQualityProfile.CommonIconAlphaDimensionMaximum + "px.",
                    "Correct visual weight in the reviewed master.");
            }

            var canvasCenter = new Vector2((width - 1f) * .5f, (height - 1f) * .5f);
            var offset = centroid - canvasCenter;
            if (Mathf.Abs(offset.x) > RuntimeUiQualityProfile.OpticalCenterToleranceSourcePixels
                || Mathf.Abs(offset.y)
                    > RuntimeUiQualityProfile.OpticalCenterToleranceSourcePixels)
            {
                report.Error("icon.optical.centroid", assetPath,
                    "Alpha-mass centroid offset is (" + offset.x.ToString("0.###") + ", "
                    + offset.y.ToString("0.###") + ")px; maximum is 4px per axis.",
                    "Recenter the reviewed master without changing its semantic direction.");
            }

            if (binding.Slot == RuntimeUiArtSlot.IndicatorDragLegal
                || binding.Slot == RuntimeUiArtSlot.IndicatorDragIllegal)
            {
                var shortDimension = Mathf.Min(bounds.width, bounds.height);
                if (shortDimension
                    < RuntimeUiQualityProfile.DragCueAlphaShortDimensionMinimum)
                {
                    report.Error("icon.optical.smallest-draw", assetPath,
                        "The drag-cue alpha short dimension is " + shortDimension
                        + " source px; minimum is "
                        + RuntimeUiQualityProfile.DragCueAlphaShortDimensionMinimum + ".",
                        "Increase the reviewed alpha silhouette within the 96px canvas.");
                }

                var cueSize = BattleUiLayout.CueBadge(new Rect(0f, 0f,
                    RuntimeUiQualityProfile.MinimumTouchTarget,
                    RuntimeUiQualityProfile.MinimumTouchTarget)).width;
                var logicalShort = shortDimension * cueSize / width;
                var logicalMajor = major * cueSize / width;
                if (logicalShort
                        < RuntimeUiQualityProfile.CommonIconOpticalShortEdgeMinimum
                    || logicalMajor
                        < RuntimeUiQualityProfile.CommonIconOpticalMajorEdgeMinimum)
                {
                    report.Error("icon.optical.smallest-draw", assetPath,
                        "At the authoritative " + cueSize.ToString("0.###")
                        + "-point cue draw, optical bounds are "
                        + logicalShort.ToString("0.###") + "x"
                        + logicalMajor.ToString("0.###")
                        + " logical points; minima are "
                        + RuntimeUiQualityProfile.CommonIconOpticalShortEdgeMinimum
                        + "x"
                        + RuntimeUiQualityProfile.CommonIconOpticalMajorEdgeMinimum + ".",
                        "Increase the reviewed drag-cue silhouette without changing its canvas.");
                }

                var minimumSourceStroke = Mathf.CeilToInt(
                    RuntimeUiQualityProfile.CommonIconStrokeMinimum
                    * width / cueSize);
                if (!HasVisibleSquare(pixels, width, height, minimumSourceStroke,
                        RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh))
                {
                    report.Error("icon.optical.stroke", assetPath,
                        "No " + minimumSourceStroke + "x" + minimumSourceStroke
                        + " source-pixel visible stroke witness exists for the "
                        + RuntimeUiQualityProfile.CommonIconStrokeMinimum
                        + "-point minimum at the authoritative cue draw size.",
                        "Thicken the reviewed drag-cue mark while preserving its semantic shape.");
                }
            }
        }

        private static bool HasVisibleSquare(Color32[] pixels, int width, int height,
            int size, byte minimumAlpha)
        {
            if (size <= 0 || size > width || size > height) return false;
            for (var y = 0; y <= height - size; y++)
            for (var x = 0; x <= width - size; x++)
            {
                var visible = true;
                for (var sampleY = y; sampleY < y + size && visible; sampleY++)
                for (var sampleX = x; sampleX < x + size; sampleX++)
                {
                    if (pixels[sampleY * width + sampleX].a >= minimumAlpha) continue;
                    visible = false;
                    break;
                }
                if (visible) return true;
            }
            return false;
        }

        private static bool TryAlphaMetrics(Color32[] pixels, int width, int height,
            out RectInt bounds, out Vector2 centroid)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;
            double weightedX = 0d;
            double weightedY = 0d;
            double alphaSum = 0d;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var alpha = pixels[y * width + x].a;
                if (alpha == 0) continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
                weightedX += x * (double)alpha;
                weightedY += y * (double)alpha;
                alphaSum += alpha;
            }
            if (maxX < minX || maxY < minY || alphaSum <= 0d)
            {
                bounds = default;
                centroid = default;
                return false;
            }
            bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            centroid = new Vector2((float)(weightedX / alphaSum),
                (float)(weightedY / alphaSum));
            return true;
        }

        private static void ValidateNineSlicePixels(RuntimeUiVisualValidationReport report,
            string assetPath, RuntimeUiArtBinding binding, Color32[] pixels,
            int width, int height)
        {
            if (width != RuntimeUiQualityProfile.NineSliceCanvasSize
                || height != RuntimeUiQualityProfile.NineSliceCanvasSize
                || binding.SliceBorder.Left != RuntimeUiQualityProfile.NineSliceBorder
                || binding.SliceBorder.Right != RuntimeUiQualityProfile.NineSliceBorder
                || binding.SliceBorder.Top != RuntimeUiQualityProfile.NineSliceBorder
                || binding.SliceBorder.Bottom != RuntimeUiQualityProfile.NineSliceBorder
                || binding.SafeInset.Left != RuntimeUiQualityProfile.NineSliceSafeInset
                || binding.SafeInset.Right != RuntimeUiQualityProfile.NineSliceSafeInset
                || binding.SafeInset.Top != RuntimeUiQualityProfile.NineSliceSafeInset
                || binding.SafeInset.Bottom != RuntimeUiQualityProfile.NineSliceSafeInset)
            {
                report.Error("nine-slice.quality-profile", assetPath,
                    "Production nine-slice must be 128x128 with border32 and safeInset20.",
                    "Restore the reviewed nine-slice geometry metadata.");
            }

            var allowMatchedTransparency =
                binding.Slot == RuntimeUiArtSlot.SurfaceIllustrationFrame;
            if (HasInvalidVerticalBoundary(pixels, width, height,
                    binding.SliceBorder.Left - 1, binding.SliceBorder.Left,
                    binding.SliceBorder.Bottom,
                    height - binding.SliceBorder.Top, allowMatchedTransparency)
                || HasInvalidVerticalBoundary(pixels, width, height,
                    width - binding.SliceBorder.Right - 1,
                    width - binding.SliceBorder.Right,
                    binding.SliceBorder.Bottom,
                    height - binding.SliceBorder.Top, allowMatchedTransparency)
                || HasInvalidHorizontalBoundary(pixels, width, height,
                    binding.SliceBorder.Bottom - 1, binding.SliceBorder.Bottom,
                    binding.SliceBorder.Left,
                    width - binding.SliceBorder.Right, allowMatchedTransparency)
                || HasInvalidHorizontalBoundary(pixels, width, height,
                    height - binding.SliceBorder.Top - 1,
                    height - binding.SliceBorder.Top,
                    binding.SliceBorder.Left,
                    width - binding.SliceBorder.Right, allowMatchedTransparency))
            {
                report.Error("nine-slice.boundary-discontinuity", assetPath,
                    "A slice boundary pairs significant alpha (>=48) with transparent alpha (<16).",
                    "Keep protected edge motifs out of stretch partitions; both-transparent frame boundaries are valid.");
            }
            if (allowMatchedTransparency)
            {
                for (var y = binding.SliceBorder.Bottom;
                     y < height - binding.SliceBorder.Top; y++)
                for (var x = binding.SliceBorder.Left;
                     x < width - binding.SliceBorder.Right; x++)
                {
                    if (pixels[y * width + x].a
                        < RuntimeUiQualityProfile.NineSliceSignificantAlphaLow)
                        continue;
                    report.Error("nine-slice.protected-center", assetPath,
                        "The illustration-frame stretch center contains visible protected ornament alpha.",
                        "Keep frame rails and corner leaves inside the fixed 32px border.");
                    return;
                }
            }
        }

        private static bool HasInvalidVerticalBoundary(Color32[] pixels,
            int width, int height, int firstX, int secondX,
            int firstY, int endY,
            bool allowMatchedTransparency)
        {
            if (firstX < 0 || secondX < 0 || firstX >= width || secondX >= width)
                return true;
            if (firstY < 0 || endY > height || firstY >= endY) return true;
            for (var y = firstY; y < endY; y++)
                if (!IsNineSliceBoundaryPairSafe(pixels[y * width + firstX].a,
                        pixels[y * width + secondX].a, allowMatchedTransparency))
                    return true;
            return false;
        }

        private static bool HasInvalidHorizontalBoundary(Color32[] pixels,
            int width, int height, int firstY, int secondY,
            int firstX, int endX,
            bool allowMatchedTransparency)
        {
            if (firstY < 0 || secondY < 0 || firstY >= height || secondY >= height)
                return true;
            if (firstX < 0 || endX > width || firstX >= endX) return true;
            for (var x = firstX; x < endX; x++)
                if (!IsNineSliceBoundaryPairSafe(pixels[firstY * width + x].a,
                        pixels[secondY * width + x].a, allowMatchedTransparency))
                    return true;
            return false;
        }

        internal static bool SignificantAlphaMismatch(byte first, byte second)
        {
            return first >= RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh
                    && second < RuntimeUiQualityProfile.NineSliceSignificantAlphaLow
                || second >= RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh
                    && first < RuntimeUiQualityProfile.NineSliceSignificantAlphaLow;
        }

        internal static bool IsNineSliceBoundaryPairSafe(byte first, byte second,
            bool allowMatchedTransparency)
        {
            if (SignificantAlphaMismatch(first, second)) return false;
            return allowMatchedTransparency
                || first >= RuntimeUiQualityProfile.NineSliceSignificantAlphaLow
                && second >= RuntimeUiQualityProfile.NineSliceSignificantAlphaLow;
        }

        private static void ValidateFixedAspectOrnament(
            RuntimeUiVisualValidationReport report, string assetPath,
            RuntimeUiArtBinding binding, ArtManifestBinding row, int width, int height)
        {
            var expectedWidth = 0;
            var expectedHeight = 0;
            if (binding.Slot == RuntimeUiArtSlot.OrnamentMetricDivider)
            {
                expectedWidth = 24;
                expectedHeight = 96;
            }
            else if (binding.Slot == RuntimeUiArtSlot.OrnamentResultBanner)
            {
                expectedWidth = 256;
                expectedHeight = 72;
            }
            else return;

            if (binding.Geometry != RuntimeUiArtGeometry.Stretch
                || width != expectedWidth || height != expectedHeight
                || row.width != expectedWidth || row.height != expectedHeight
                || row.slice_border != 0)
            {
                report.Error("ornament.fixed-aspect", assetPath,
                    RuntimeUiArtSlots.SemanticId(binding.Slot)
                    + " must keep its reviewed " + expectedWidth + "x" + expectedHeight
                    + " Stretch/border0 contract.",
                    "Re-export the deterministic tight crop without nine-slicing it.");
            }
        }

        private static void ValidateIllustrationSourceAspect(
            RuntimeUiVisualValidationReport report, RuntimeUiArtBinding binding,
            ArtManifestBinding row)
        {
            if (!RequiresOpaquePixels(binding.Slot)
                || binding.Slot == RuntimeUiArtSlot.SurfaceScreenBackground
                || binding.Slot == RuntimeUiArtSlot.SurfaceScrim)
                return;
            var sourcePath = RuntimeUiArtSetRegistry.Normalize(row.source);
            var runtimePath = RuntimeUiArtSetRegistry.Normalize(row.runtime);
            var source = DecodePng(report, sourcePath, "illustration.source.decode");
            var runtime = DecodePng(report, runtimePath, "illustration.runtime.decode");
            try
            {
                if (source == null || runtime == null) return;
                var sourceAspect = source.width / (float)source.height;
                var runtimeAspect = runtime.width / (float)runtime.height;
                var relativeError = Mathf.Abs(runtimeAspect - sourceAspect) / sourceAspect;
                if (relativeError > RuntimeUiQualityProfile.IllustrationAspectTolerance)
                {
                    report.Error("illustration.aspect", runtimePath,
                        "Source/runtime aspect error is "
                        + (relativeError * 100f).ToString("0.###") + "%; maximum is 1%.",
                        "Preserve the reviewed illustration aspect during deterministic export.");
                }
            }
            finally
            {
                if (source != null) Object.DestroyImmediate(source);
                if (runtime != null) Object.DestroyImmediate(runtime);
            }
        }

        private static void ValidateTransparentPadding(RuntimeUiVisualValidationReport report,
            string assetPath, RuntimeUiArtBinding binding, Color32[] pixels,
            int width, int height)
        {
            if (width != height)
            {
                report.Error("icon.canvas.square", assetPath,
                    "Icon canvas is not square.", "Export every icon on one square canvas.");
                return;
            }

            var safe = binding.SafeInset;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var outside = x < safe.Left || x >= width - safe.Right
                    || y < safe.Bottom || y >= height - safe.Top;
                if (!outside || pixels[y * width + x].a == 0) continue;
                report.Error("icon.padding.alpha", assetPath,
                    "Icon pixels extend into the declared transparent safe padding.",
                    "Keep all visible pixels inside the binding safe inset.");
                return;
            }
        }

        private static void ValidateNoUnownedFiles(RuntimeUiVisualValidationReport report,
            string directory, string pattern, HashSet<string> owned, string kind)
        {
            var absolute = ToAbsolute(directory);
            if (!Directory.Exists(absolute)) return;
            foreach (var file in Directory.GetFiles(absolute, pattern, SearchOption.AllDirectories))
            {
                var path = ToAssetPath(file);
                if (owned.Contains(path)) continue;
                report.Error("manifest." + kind + ".unowned", path,
                    "The file is not owned by any manifest binding.",
                    "Remove it from the production directory or add its semantic binding.");
            }
        }

        private static void ValidateProductionAncillaryFiles(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet)
        {
            ValidateAncillaryDirectory(report,
                RuntimeUiArtSetRegistry.SourceDirectory(artSet), true);
            ValidateAncillaryDirectory(report,
                RuntimeUiArtSetRegistry.RuntimeDirectory(artSet), false);
        }

        private static void ValidateAncillaryDirectory(
            RuntimeUiVisualValidationReport report, string directory, bool sourceDirectory)
        {
            var absolute = ToAbsolute(directory);
            if (!Directory.Exists(absolute)) return;
            foreach (var file in Directory.GetFiles(absolute, "*", SearchOption.AllDirectories))
            {
                var assetPath = ToAssetPath(file);
                var relative = RuntimeUiArtSetRegistry.Normalize(
                    Path.GetRelativePath(absolute, file));
                var extension = Path.GetExtension(relative);
                if (string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
                    continue;

                var allowed = string.Equals(relative, "README.md", StringComparison.Ordinal)
                    || sourceDirectory && (
                        string.Equals(relative, "art_manifest.json", StringComparison.Ordinal)
                        || string.Equals(relative, "prompt-record.json", StringComparison.Ordinal)
                        || string.Equals(relative, "icons/alignment-audit.md", StringComparison.Ordinal)
                        || string.Equals(relative, "icons/prompt-record.md", StringComparison.Ordinal)
                        || (relative.StartsWith("export_", StringComparison.Ordinal)
                            && relative.EndsWith(".py", StringComparison.Ordinal)));
                if (allowed) continue;
                report.Error("production.ancillary.unclassified", assetPath,
                    "Production art roots may contain only manifest/export/prompt/readme/alignment metadata besides owned art.",
                    "Move review evidence and generated caches outside the production set root.");
            }
        }

        private static void ValidateSingleReleaseTheme(RuntimeUiVisualValidationReport report,
            RuntimeUiTheme releaseTheme)
        {
            var paths = AssetDatabase.FindAssets("t:RuntimeUiTheme",
                    new[] { RuntimeUiArtSetRegistry.ThemeRoot })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (paths.Length != 1 || paths[0] != RuntimeUiArtSetRegistry.ReleaseThemePath
                || AssetDatabase.GetAssetPath(releaseTheme) != RuntimeUiArtSetRegistry.ReleaseThemePath)
            {
                report.Error("release.theme.unique", RuntimeUiArtSetRegistry.ThemeRoot,
                    "Release must have exactly one RuntimeUiTheme at the fixed path.",
                    "Remove alternate release themes and keep " + RuntimeUiArtSetRegistry.ReleaseThemePath + ".");
            }
        }

        private static void ValidateForbiddenDependencies(RuntimeUiVisualValidationReport report,
            string ownerPath)
        {
            if (string.IsNullOrWhiteSpace(ownerPath)) return;
            foreach (var dependency in AssetDatabase.GetDependencies(ownerPath, true))
            {
                var normalized = "/" + RuntimeUiArtSetRegistry.Normalize(dependency).TrimStart('/');
                if (!ForbiddenReleaseSegments.Any(segment =>
                        normalized.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;
                report.Error("release.forbidden-reference", ownerPath,
                    "Release asset references forbidden authoring/evidence content: " + dependency,
                    "Reference only the Theme, Art/Sets, Art/Runtime and packaged font release roots.");
            }
        }

        private static void ValidateReleaseScenes(RuntimeUiVisualValidationReport report,
            RuntimeUiTheme theme)
        {
            foreach (var scenePath in RuntimeUiArtSetRegistry.ReleaseScenes)
            {
                if (!File.Exists(ToAbsolute(scenePath)))
                {
                    report.Error("release.scene.missing", scenePath,
                        "A fixed release scene is missing.", "Restore the Bootstrap → Lobby → Battle → Settlement flow.");
                    continue;
                }
                ValidateForbiddenDependencies(report, scenePath);
                ValidateSceneReferences(report, scenePath, theme);
            }
        }

        private static void ValidateSceneReferences(RuntimeUiVisualValidationReport report,
            string scenePath, RuntimeUiTheme releaseTheme)
        {
            var existing = SceneManager.GetSceneByPath(scenePath);
            var openedHere = !existing.IsValid() || !existing.isLoaded;
            var scene = openedHere
                ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
                : existing;
            try
            {
                var themeReferences = 0;
                var artSetReferences = 0;
                var coordinatorCount = 0;
                var correctCoordinatorBinding = 0;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    var isCoordinator = behaviour.GetType().FullName == "FruitDefense.App.AppFlowCoordinator";
                    if (isCoordinator) coordinatorCount++;
                    var serialized = new SerializedObject(behaviour);
                    var iterator = serialized.GetIterator();
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var value = iterator.objectReferenceValue;
                        if (value is RuntimeUiTheme referencedTheme)
                        {
                            themeReferences++;
                            if (isCoordinator && iterator.propertyPath == "runtimeUiTheme"
                                && referencedTheme == releaseTheme)
                                correctCoordinatorBinding++;
                        }
                        else if (value is RuntimeUiArtSet) artSetReferences++;
                    }
                }

                var bootstrap = scenePath == RuntimeUiArtSetRegistry.BootstrapScenePath;
                if (bootstrap)
                {
                    if (coordinatorCount != 1 || themeReferences != 1
                        || correctCoordinatorBinding != 1 || artSetReferences != 0)
                    {
                        report.Error("scene.bootstrap.theme-binding", scenePath,
                            "Bootstrap must contain one AppFlowCoordinator with the sole direct release-theme reference and no direct art-set reference.",
                            "Bind runtimeUiTheme once on the unique AppFlowCoordinator.");
                    }
                }
                else if (themeReferences != 0 || artSetReferences != 0)
                {
                    report.Error("scene.release.direct-reference", scenePath,
                        "Only Bootstrap may directly reference the runtime theme; release scenes must not reference theme/art-set assets.",
                        "Remove direct references and accept the injected theme from the flow coordinator.");
                }
            }
            finally
            {
                if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
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

        private static int UniformInset(RuntimeUiPixelInsets inset)
        {
            return inset.Left == inset.Top && inset.Left == inset.Right && inset.Left == inset.Bottom
                ? inset.Left : int.MinValue;
        }

        private static string GeometryName(RuntimeUiArtGeometry geometry)
        {
            switch (geometry)
            {
                case RuntimeUiArtGeometry.Stretch: return "stretch";
                case RuntimeUiArtGeometry.NineSlice: return "nine-slice";
                case RuntimeUiArtGeometry.Icon: return "icon";
                default: return string.Empty;
            }
        }

        private static bool Nearly(float left, float right)
        {
            return Mathf.Abs(left - right) <= .001f;
        }

        private static string Sha256(string absolutePath)
        {
            using (var stream = File.OpenRead(absolutePath))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ToAbsolute(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                RuntimeUiArtSetRegistry.Normalize(assetPath)));
        }

        private static string ToAssetPath(string absolutePath)
        {
            var project = Path.GetFullPath(Directory.GetCurrentDirectory())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(absolutePath);
            return RuntimeUiArtSetRegistry.Normalize(full.Substring(project.Length + 1));
        }

        [Serializable]
        private sealed class ArtManifest
        {
            public string schema;
            public string setId;
            public string revision;
            public float sourceScale;
            public int slotCount;
            public int uniqueExportCount;
            public ArtManifestBinding[] bindings;
            public ImportContract importContract;
        }

        [Serializable]
        private sealed class ArtManifestBinding
        {
            public string stem;
            public string semantic_id;
            public string geometry;
            public int size;
            public int width;
            public int height;
            public string source;
            public string runtime;
            public string sourceSha256;
            public string runtimeSha256;
            public string guid;
            public int slice_border;
            public int safe_inset;
            public float pixels_per_logical_unit;
            public int slot;
            public string shared_from_set;
        }

        [Serializable]
        private sealed class ImportContract
        {
            public int pixelsPerUnit;
        }
    }
}
