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
                ValidateOpticalInset(report, assetPath, binding, pixels,
                    texture.width, texture.height);

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
                    if (RuntimeUiArtSlots.IsMicroIcon(binding.Slot))
                        ValidateMicroIconOptics(report, assetPath, binding,
                            pixels, texture.width, texture.height);
                    else if (binding.Slot != RuntimeUiArtSlot.OrnamentScreenCorner)
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
                || slot == RuntimeUiArtSlot.IllustrationLobbyOrchard03
                || slot == RuntimeUiArtSlot.IllustrationShellOrchardDepth;
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

        private static void ValidateSemanticActionContainerManifest(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSlot slot,
            ArtManifestBinding row, string manifestPath)
        {
            string expectedContainer;
            switch (slot)
            {
                case RuntimeUiArtSlot.ActionPrimary:
                    expectedContainer = "436C15";
                    break;
                case RuntimeUiArtSlot.ActionDanger:
                    expectedContainer = "9F302B";
                    break;
                default:
                    return;
            }

            if (!string.Equals(row.container_contract,
                    "semantic-action-container", StringComparison.Ordinal)
                || !string.Equals(row.target_rgb, expectedContainer,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(row.content_reference_rgb, "FFF6E0",
                    StringComparison.OrdinalIgnoreCase)
                || row.content_region_min_contrast + .001f
                    < RuntimeUiQualityProfile.NormalTextContrast)
            {
                report.Error("action-container.manifest-contract", manifestPath,
                    RuntimeUiArtSlots.SemanticId(slot)
                    + " does not record a passing final-pixel content-region contract; contrast="
                    + row.content_region_min_contrast.ToString("0.00") + ":1.",
                    "Record the semantic container target, FFF6E0 content reference, and measured central-region contrast >=4.5:1.");
            }
        }

        private static void ValidateTintableActionGlyph(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSlot slot,
            ArtManifestBinding row,
            string sourcePath, string runtimePath)
        {
            if (Array.IndexOf(TintableActionGlyphSlots, slot) < 0)
                return;

            if (!string.Equals(row.render_contract, "tintable-action-glyph",
                    StringComparison.Ordinal)
                || !string.Equals(row.neutral_rgb, "FFFFFF",
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Error("action-glyph.manifest-contract", runtimePath,
                    RuntimeUiArtSlots.SemanticId(slot)
                    + " does not declare its tintable white-mask render contract.",
                    "Record render_contract=tintable-action-glyph and neutral_rgb=FFFFFF in the owning manifest row.");
            }

            ValidateWhiteAlphaMaskPng(report, runtimePath,
                "action-glyph.runtime-mask");

            var extension = Path.GetExtension(sourcePath);
            if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
            {
                ValidateWhiteAlphaMaskPng(report, sourcePath,
                    "action-glyph.source-mask");
                return;
            }

            if (string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
            {
                ValidateWhiteActionGlyphSvg(report, sourcePath);
                return;
            }

            report.Error("action-glyph.source-format", sourcePath,
                RuntimeUiArtSlots.SemanticId(slot)
                + " must use a white-mask PNG or white-painted SVG source.",
                "Normalize the action glyph source without adding a colored fallback.");
        }

        private static void ValidateWhiteAlphaMaskPng(
            RuntimeUiVisualValidationReport report, string assetPath, string issueCode)
        {
            if (!File.Exists(ToAbsolute(assetPath)))
                return;

            var texture = DecodePng(report, assetPath, issueCode + ".decode");
            if (texture == null)
                return;

            try
            {
                var pixels = texture.GetPixels32();
                var visiblePixels = 0;
                var coloredVisiblePixels = 0;
                var dirtyTransparentPixels = 0;
                for (var index = 0; index < pixels.Length; index++)
                {
                    var pixel = pixels[index];
                    if (pixel.a > 0)
                    {
                        visiblePixels++;
                        if (pixel.r != byte.MaxValue
                            || pixel.g != byte.MaxValue
                            || pixel.b != byte.MaxValue)
                            coloredVisiblePixels++;
                    }
                    else if (pixel.r != 0 || pixel.g != 0 || pixel.b != 0)
                    {
                        dirtyTransparentPixels++;
                    }
                }

                if (visiblePixels == 0 || coloredVisiblePixels > 0
                    || dirtyTransparentPixels > 0)
                {
                    report.Error(issueCode, assetPath,
                        "Tintable action glyph must be a strict white alpha mask; visible="
                        + visiblePixels + ", non-white-visible=" + coloredVisiblePixels
                        + ", dirty-transparent=" + dirtyTransparentPixels + ".",
                        "Export every alpha>0 pixel as RGBA(255,255,255,A) and every alpha=0 pixel as transparent black.");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void ValidateWhiteActionGlyphSvg(
            RuntimeUiVisualValidationReport report, string assetPath)
        {
            var absolute = ToAbsolute(assetPath);
            if (!File.Exists(absolute))
                return;

            string source;
            try
            {
                source = File.ReadAllText(absolute);
            }
            catch (Exception exception)
            {
                report.Error("action-glyph.source-svg.read", assetPath,
                    exception.Message, "Restore the manifest-owned SVG source.");
                return;
            }

            var paintMatches = System.Text.RegularExpressions.Regex.Matches(source,
                "(?:fill|stroke)\\s*=\\s*[\"']([^\"']+)[\"']",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var hasWhitePaint = false;
            var invalidPaint = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in paintMatches)
            {
                var paint = match.Groups[1].Value.Trim();
                if (string.Equals(paint, "none", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(paint, "#fff", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(paint, "#ffffff", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(paint, "white", StringComparison.OrdinalIgnoreCase))
                {
                    hasWhitePaint = true;
                    continue;
                }
                invalidPaint.Add(paint);
            }

            var colorLiterals = System.Text.RegularExpressions.Regex.Matches(source,
                "#[0-9a-f]{3,8}|rgba?\\s*\\([^)]*\\)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in colorLiterals)
            {
                var literal = match.Value.Trim();
                if (!string.Equals(literal, "#fff", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(literal, "#ffffff", StringComparison.OrdinalIgnoreCase))
                    invalidPaint.Add(literal);
            }

            var hasStatefulEffects = source.IndexOf("<linearGradient",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("<radialGradient",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("<filter",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || source.IndexOf("drop-shadow", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!hasWhitePaint || invalidPaint.Count > 0 || hasStatefulEffects)
            {
                report.Error("action-glyph.source-svg-mask", assetPath,
                    "Tintable SVG action glyph must use only white visible paint and no gradients/filters; invalid paint="
                    + (invalidPaint.Count == 0
                        ? "none" : string.Join(",", invalidPaint.ToArray())) + ".",
                    "Set every visible fill/stroke to #FFFFFF and remove baked color, highlight, gradient, and shadow effects.");
            }
        }

        private static void ValidateMicroIconOptics(
            RuntimeUiVisualValidationReport report, string assetPath,
            RuntimeUiArtBinding binding, Color32[] pixels, int width, int height)
        {
            if (width != RuntimeUiQualityProfile.MicroIconCanvasSize
                || height != RuntimeUiQualityProfile.MicroIconCanvasSize
                || binding.SafeInset.Left != RuntimeUiQualityProfile.MicroIconSafeInset
                || binding.SafeInset.Right != RuntimeUiQualityProfile.MicroIconSafeInset
                || binding.SafeInset.Top != RuntimeUiQualityProfile.MicroIconSafeInset
                || binding.SafeInset.Bottom != RuntimeUiQualityProfile.MicroIconSafeInset)
            {
                report.Error("icon.micro.canvas", assetPath,
                    "Micro resource art must use the final 18x18 canvas with a 1px edge.",
                    "Run the target-size micro exporter.");
                return;
            }

            if (!TryAlphaMetrics(pixels, width, height, out var bounds, out var centroid))
            {
                report.Error("icon.micro.empty", assetPath,
                    "Micro icon has no significant alpha mass.",
                    "Restore the reviewed resource master and re-export.");
                return;
            }

            var major = Mathf.Max(bounds.width, bounds.height);
            var significant = pixels.Count(pixel =>
                pixel.a >= RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh);
            if (major < RuntimeUiQualityProfile.MicroIconAlphaDimensionMinimum
                || major > RuntimeUiQualityProfile.MicroIconAlphaDimensionMaximum
                || significant < RuntimeUiQualityProfile.MicroIconSignificantPixelMinimum
                || significant > RuntimeUiQualityProfile.MicroIconSignificantPixelMaximum
                || !HasVisibleSquare(pixels, width, height, 2,
                    RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh))
            {
                report.Error("icon.micro.envelope", assetPath,
                    "Micro icon target envelope is " + bounds.width + "x" + bounds.height
                    + " with " + significant + " significant pixels.",
                    "Keep a two-pixel critical feature and re-export the silhouette at the final 18px target.");
            }

            var center = new Vector2((width - 1f) * .5f, (height - 1f) * .5f);
            var offset = centroid - center;
            if (Mathf.Abs(offset.x) > RuntimeUiQualityProfile.MicroIconOpticalCenterTolerance
                || Mathf.Abs(offset.y)
                    > RuntimeUiQualityProfile.MicroIconOpticalCenterTolerance)
            {
                report.Error("icon.micro.centroid", assetPath,
                    "Micro alpha centroid offset is (" + offset.x.ToString("0.###") + ", "
                    + offset.y.ToString("0.###") + ").",
                    "Correct optical placement in the target-size exporter.");
            }
        }

        private static void ValidateImagegenProvenance(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet,
            RuntimeUiArtBinding binding, ArtManifestBinding row, string manifestPath)
        {
            if (binding.Slot != RuntimeUiArtSlot.ActionCompactControl
                && binding.Slot != RuntimeUiArtSlot.ActionCompactControlActive)
                return;
            var expectedRecord = RuntimeUiArtSetRegistry.SourceDirectory(artSet)
                + "/prompt-record.json";
            if (!string.Equals(row.imagegen_provider, "built-in-imagegen",
                    StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(row.imagegen_output)
                || RuntimeUiArtSetRegistry.Normalize(row.prompt_record) != expectedRecord)
            {
                report.Error("manifest.imagegen.provenance", manifestPath,
                    RuntimeUiArtSlots.SemanticId(binding.Slot)
                    + " lacks its built-in imagegen output and prompt-record ownership.",
                    "Record imagegen_provider, imagegen_output, and the local prompt_record path.");
                return;
            }

            ImagegenPromptRecord record;
            try
            {
                record = JsonUtility.FromJson<ImagegenPromptRecord>(
                    File.ReadAllText(ToAbsolute(expectedRecord)));
            }
            catch (Exception exception)
            {
                report.Error("manifest.imagegen.prompt-record", expectedRecord,
                    exception.Message, "Restore the generated-art prompt record.");
                return;
            }
            var semantic = RuntimeUiArtSlots.SemanticId(binding.Slot);
            var asset = record?.assets?.SingleOrDefault(candidate =>
                candidate != null && candidate.semanticId == semantic);
            if (record == null || record.schema != "fruit-defense.imagegen-prompt-record.v1"
                || record.setId != artSet.SetId || record.provider != "built-in-imagegen"
                || asset == null || asset.generatedOutput != row.imagegen_output
                || asset.selectedOutput != row.imagegen_output
                || string.IsNullOrWhiteSpace(asset.prompt)
                || asset.references == null
                || string.IsNullOrWhiteSpace(asset.alphaContract)
                || !string.Equals(asset.sourceSha256, row.sourceSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Error("manifest.imagegen.prompt-contract", expectedRecord,
                    semantic + " prompt record does not match its manifest output/hash.",
                    "Record the selected built-in imagegen output, prompt, references, alpha contract, and source hash.");
            }
        }

    }
}
