using System;
using System.IO;
using System.Linq;
using FruitDefense.UI;

namespace FruitDefense.Editor
{
    public static partial class RuntimeUiVisualSystemValidator
    {
        private static void ValidateProductionRasterTooling(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet)
        {
            var exporterPath = RuntimeUiArtSetRegistry.SourceDirectory(artSet)
                + "/export_" + artSet.SetId.Replace('-', '_') + ".py";
            var absolute = ToAbsolute(exporterPath);
            if (!File.Exists(absolute))
            {
                report.Error("ui-raster.exporter.missing", exporterPath,
                    "The production UI raster exporter is missing.",
                    "Restore the fixed-master normalization exporter.");
                return;
            }

            var source = File.ReadAllText(absolute);
            var forbiddenTokens = new[]
            {
                "ImageDraw",
                ".rounded_rectangle(",
                ".polygon(",
                ".line(",
                ".arc(",
                ".ellipse(",
                ".rectangle(",
            };
            var forbidden = forbiddenTokens.FirstOrDefault(token =>
                source.IndexOf(token, StringComparison.Ordinal) >= 0);
            var authoredFunction = source.Split('\n').Any(line =>
                line.TrimStart().StartsWith("def author_", StringComparison.Ordinal));
            if (forbidden != null || authoredFunction)
            {
                report.Error("ui-raster.script-authoring-forbidden", exporterPath,
                    "Production UI source contains a visible-pixel drawing API or author_* path: "
                    + (forbidden ?? "author_* function") + ".",
                    "Use a fixed owned raster master; scripts may only verify hashes, copy, clean same-output alpha, pad, resize, and encode.");
            }

            var requiredTokens = new[]
            {
                "validate_no_visible_pixel_authoring()",
                "extract_fixed_primary_action_master(project_root, source_root)",
                "PRIMARY_ACTION_FIXED_MASTER_SHA256",
                "extract_hub_navigation_surface_masters(project_root, source_root)",
                "HUB_NAVIGATION_GENERATED_ASSETS",
                "extract_home_reference_illustrations(project_root, source_root)",
                "HOME_REFERENCE_ILLUSTRATION_ASSETS",
            };
            foreach (var required in requiredTokens)
            {
                if (source.IndexOf(required, StringComparison.Ordinal) >= 0) continue;
                report.Error("ui-raster.fixed-master-gate-missing", exporterPath,
                    "Production UI exporter is missing required fixed-master gate: "
                    + required + ".",
                    "Restore the no-drawing gate and every hash-locked fixed-raster integration.");
            }

            ValidateHomeReferenceWindowComposition(report);
        }

        private static void ValidateHomeReferenceWindowComposition(
            RuntimeUiVisualValidationReport report)
        {
            const string presenterPath = "Assets/Scripts/Shell/LobbyHubPresenter.cs";
            var absolute = ToAbsolute(presenterPath);
            if (!File.Exists(absolute))
            {
                report.Error("ui-home.reference-window-presenter-missing",
                    presenterPath, "The Hub presenter source is missing.",
                    "Restore the authoritative Hub presenter.");
                return;
            }

            var source = File.ReadAllText(absolute);
            if (source.IndexOf("cardLayout.Thumbnail, thumbnail);",
                    StringComparison.Ordinal) < 0)
            {
                report.Error("ui-home.reference-window-not-bound",
                    presenterPath,
                    "Home does not draw the reference-derived image window inside its authoritative Thumbnail rectangle.",
                    "Bind the level illustration to cardLayout.Thumbnail.");
            }
            if (source.IndexOf("DrawIllustrationFrame(_drawContext, cardLayout.",
                    StringComparison.Ordinal) >= 0)
            {
                report.Error("ui-home.illustration-frame-overlay-forbidden",
                    presenterPath,
                    "Home still overlays the leaking illustration-frame mask.",
                    "Remove the Home frame overlay and keep one contained reference-derived image window.");
            }
        }
    }
}
