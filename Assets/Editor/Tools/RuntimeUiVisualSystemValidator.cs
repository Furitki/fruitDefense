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

    public static partial class RuntimeUiVisualSystemValidator
    {
        private const string ManifestSchema = "fruit-defense.runtime-ui-art-manifest.v2";
        private const string ApprovedThemeId = "ui.sunny-orchard";
        private const string PaintedSetId = "sunny-orchard-painted";
        private const string SharedConsumerSetId = "sunny-orchard";
        private const float ColorTolerance = 1.1f / 255f;

        private static readonly RuntimeUiArtSlot[] TintableActionGlyphSlots =
        {
            RuntimeUiArtSlot.IconControlPause,
            RuntimeUiArtSlot.IconControlContinue,
            RuntimeUiArtSlot.IconControlSpeed,
            RuntimeUiArtSlot.IconControlStartWave,
            RuntimeUiArtSlot.IconControlRetry,
            RuntimeUiArtSlot.IconControlReturn,
            RuntimeUiArtSlot.IconControlClose,
            RuntimeUiArtSlot.IconControlStart,
            RuntimeUiArtSlot.IconControlRefresh,
        };

        private static readonly RuntimeUiArtSlot[] PanelFamilySlots =
        {
            RuntimeUiArtSlot.SurfacePanelStandard,
            RuntimeUiArtSlot.SurfacePanelRaised,
            RuntimeUiArtSlot.SurfaceDetail,
            RuntimeUiArtSlot.SurfaceModal,
            RuntimeUiArtSlot.SurfaceResult,
        };

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
            ValidateBattleStructuralHierarchy(report, candidate);
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
                ValidateBattleStructuralHierarchy(report, set);
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
    }
}
