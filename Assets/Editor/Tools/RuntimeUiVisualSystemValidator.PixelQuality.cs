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
                    if (IsHubNavigationIcon(binding.Slot))
                    {
                        ValidateTransparentGeneratedAlphaContract(report,
                            assetPath, pixels, "hub-icon.alpha-contract");
                        ValidateHubNavigationIconReadability(report, assetPath,
                            pixels, texture.width, texture.height);
                    }
                    if (RuntimeUiArtSlots.IsMicroIcon(binding.Slot))
                        ValidateMicroIconOptics(report, assetPath, binding,
                            pixels, texture.width, texture.height);
                    else if (binding.Slot != RuntimeUiArtSlot.OrnamentScreenCorner)
                        ValidateCommonIconOptics(report, assetPath, binding,
                            pixels, texture.width, texture.height);
                }

                if (binding.Geometry == RuntimeUiArtGeometry.NineSlice)
                {
                    ValidateNineSlicePixels(report, assetPath, binding, pixels,
                        texture.width, texture.height);
                    ValidateReferenceMaterialPixels(report, assetPath, binding, row,
                        pixels, texture.width, texture.height);
                }

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
                || binding.Slot == RuntimeUiArtSlot.OrnamentResultBanner
                || binding.Slot
                    == RuntimeUiArtSlot.IllustrationHubActivityReward;
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
                    expectedContainer = "A0C73D";
                    break;
                case RuntimeUiArtSlot.ActionSecondary:
                    expectedContainer = "88AF35";
                    break;
                case RuntimeUiArtSlot.ActionDanger:
                    expectedContainer = "C81409";
                    break;
                default:
                    return;
            }
            var expectedContent = slot == RuntimeUiArtSlot.ActionDanger
                ? "F9EFDA"
                : slot == RuntimeUiArtSlot.ActionPrimary
                    ? "0C0804"
                    : "4B2A13";

            if (!string.Equals(row.container_contract,
                    "semantic-action-container", StringComparison.Ordinal)
                || !string.Equals(row.target_rgb, expectedContainer,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(row.content_reference_rgb, expectedContent,
                    StringComparison.OrdinalIgnoreCase)
                || row.content_region_min_contrast + .001f
                    < RuntimeUiQualityProfile.NormalTextContrast)
            {
                report.Error("action-container.manifest-contract", manifestPath,
                    RuntimeUiArtSlots.SemanticId(slot)
                    + " does not record a passing final-pixel content-region contract; contrast="
                    + row.content_region_min_contrast.ToString("0.00") + ":1.",
                    "Keep the reference-authoritative container unchanged; recalibrate the separate text/icon content token first, then record the measured central-region contrast >=4.5:1.");
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

        private const string ReferenceMaterialAnatomy =
            "outer-cream-rim|face|soil-outline|upper-highlight|short-bottom-shadow";
        private const string LineFreeCarrierMaterialAnatomy =
            "rounded-paper-face|soft-tonal-edge|upper-highlight|short-bottom-shadow|no-linear-rail";
        private const byte DirectMaterialAlphaCleanupThreshold = 48;

        private static void ValidateReferenceMaterialManifest(
            RuntimeUiVisualValidationReport report, RuntimeUiArtBinding binding,
            ArtManifestBinding row, string manifestPath)
        {
            if (binding.Geometry != RuntimeUiArtGeometry.NineSlice) return;
            var semantic = RuntimeUiArtSlots.SemanticId(binding.Slot);
            if (ValidateFixedPrimaryActionManifest(
                    report, binding, row, manifestPath)) return;
            if (IsImagegenMaterialSlot(binding.Slot))
            {
                var usesGeometryMask =
                    binding.Slot == RuntimeUiArtSlot.ActionSecondary;
                var lineFreeCarrier = binding.Slot == RuntimeUiArtSlot.SurfaceMetric
                    || binding.Slot == RuntimeUiArtSlot.SlotNursery;
                var usesBackgroundCleanup = lineFreeCarrier
                    || binding.Slot == RuntimeUiArtSlot.SurfaceCardSelectable;
                var expectedAnatomy = lineFreeCarrier
                    ? LineFreeCarrierMaterialAnatomy
                    : ReferenceMaterialAnatomy;
                var expectedTransform =
                    "content-crop|transparent-padding|alpha-safe-resize"
                    + (usesGeometryMask ? "|approved-geometry-alpha-mask" : string.Empty)
                    + (usesBackgroundCleanup
                        ? "|connected-neutral-background-cleanup"
                        : string.Empty);
                var expectedRenderContract = binding.Slot
                    == RuntimeUiArtSlot.SurfaceMetric
                    ? "line-free-rounded-paper-metric"
                    : binding.Slot == RuntimeUiArtSlot.SlotNursery
                        ? "line-free-rounded-paper-slot"
                        : string.Empty;
                if (!string.Equals(row.authoring_contract,
                        "imagegen-direct-master", StringComparison.Ordinal)
                    || !string.Equals(row.material_anatomy,
                        expectedAnatomy, StringComparison.Ordinal)
                    || lineFreeCarrier && !string.Equals(row.render_contract,
                        expectedRenderContract, StringComparison.Ordinal)
                    || !string.Equals(row.imagegen_provider,
                        "built-in-imagegen", StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(row.imagegen_output)
                    || string.IsNullOrWhiteSpace(row.prompt_record)
                    || string.IsNullOrWhiteSpace(row.generated_asset)
                    || string.IsNullOrWhiteSpace(row.generated_asset_sha256)
                    || row.deterministic_transform != expectedTransform
                    || !string.IsNullOrWhiteSpace(row.generated_sheet)
                    || !string.IsNullOrWhiteSpace(row.generated_sheet_sha256)
                    || row.generated_crop != null
                    || !string.IsNullOrWhiteSpace(row.material_recipe)
                    || !string.IsNullOrWhiteSpace(row.outer_cream_rgb)
                    || !string.IsNullOrWhiteSpace(row.face_rgb)
                    || !string.IsNullOrWhiteSpace(row.soil_outline_rgb)
                    || !string.IsNullOrWhiteSpace(row.upper_highlight_rgb)
                    || !string.IsNullOrWhiteSpace(row.bottom_shadow_rgb))
                {
                    report.Error("material.imagegen-direct.manifest", manifestPath,
                        semantic + " does not own the reviewed individual-master ImageGen contract.",
                        "Record one generated asset/output/hash and the permitted direct transform; remove sheet and procedural fields.");
                }
                else
                    ValidateOwnedFile(report, row.generated_asset,
                        row.generated_asset_sha256, string.Empty, "generated-asset");
                if (binding.Slot == RuntimeUiArtSlot.ActionSecondary
                    && row.content_tone != "primary"
                    || binding.Slot == RuntimeUiArtSlot.ActionDanger
                    && row.content_tone != "inverse")
                {
                    report.Error("material.action.content-tone", manifestPath,
                        semantic + " does not declare its semantic content tone.",
                        "Keep the reference-authoritative action raster unchanged and restore its approved separate content token: primary soil brown for light-green actions, inverse warm white for danger.");
                }
                return;
            }
            var colors = new[]
            {
                row.outer_cream_rgb, row.face_rgb, row.soil_outline_rgb,
                row.upper_highlight_rgb, row.bottom_shadow_rgb,
            };
            if (!string.Equals(row.authoring_contract,
                    "deterministic-reference-material-kit", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(row.material_recipe)
                || !string.Equals(row.material_anatomy, ReferenceMaterialAnatomy,
                    StringComparison.Ordinal)
                || colors.Any(color => !IsRgbHex(color))
                || colors.Distinct(StringComparer.OrdinalIgnoreCase).Count() != colors.Length)
            {
                report.Error("material.anatomy.manifest", manifestPath,
                    semantic + " does not own five distinct reference-material layers.",
                    "Regenerate the text-free material kit with cream rim, face, soil outline, upper highlight, and short bottom shadow records.");
            }
            if (!string.IsNullOrWhiteSpace(row.imagegen_provider)
                || !string.IsNullOrWhiteSpace(row.imagegen_output))
            {
                report.Error("material.v1-generation-path", manifestPath,
                    semantic + " still records the rejected v1 generated-surface path.",
                    "Remove the superseded imagegen surface binding and keep the deterministic reference kit only.");
            }

            var expectedRecipe = ExpectedReferenceMaterialRecipe(binding.Slot);
            if (!string.IsNullOrEmpty(expectedRecipe)
                && !string.Equals(row.material_recipe, expectedRecipe,
                    StringComparison.Ordinal))
            {
                report.Error("material.recipe.semantic", manifestPath,
                    semantic + " uses recipe '" + row.material_recipe
                    + "' instead of '" + expectedRecipe + "'.",
                    "Restore the reference-faithful semantic material recipe.");
            }
            if (binding.Slot == RuntimeUiArtSlot.SlotTool
                && row.content_layout_contract
                    != "main-icon|multiply|target-glyph|corner-inventory-badge")
            {
                report.Error("material.recipe-card.content-layout", manifestPath,
                    "slot.tool does not reserve the approved recipe-card content sequence.",
                    "Record the main-icon, multiply, target-glyph, and corner inventory-badge contract.");
            }
            if ((binding.Slot == RuntimeUiArtSlot.ActionPrimary
                    || binding.Slot == RuntimeUiArtSlot.ActionSecondary)
                && row.content_tone != "primary"
                || binding.Slot == RuntimeUiArtSlot.ActionDanger
                && row.content_tone != "inverse")
            {
                report.Error("material.action.content-tone", manifestPath,
                    semantic + " does not declare its semantic content tone.",
                    "Keep the reference-authoritative action raster unchanged and restore its approved separate content token: primary soil brown for light-green actions, inverse warm white for danger.");
            }
        }

        private static void ValidateReferenceMaterialPixels(
            RuntimeUiVisualValidationReport report, string assetPath,
            RuntimeUiArtBinding binding, ArtManifestBinding row, Color32[] pixels,
            int width, int height)
        {
            if (binding.Geometry != RuntimeUiArtGeometry.NineSlice) return;
            if (IsImagegenMaterialSlot(binding.Slot)
                || binding.Slot == RuntimeUiArtSlot.ActionPrimary)
            {
                var hiddenRgbPixels = pixels.Count(pixel => pixel.a == 0
                    && (pixel.r != 0 || pixel.g != 0 || pixel.b != 0));
                var lowAlphaPixels = pixels.Count(pixel => pixel.a > 0
                    && pixel.a < DirectMaterialAlphaCleanupThreshold);
                if (hiddenRgbPixels > 0 || lowAlphaPixels > 0)
                {
                    report.Error("material.imagegen.alpha-hygiene", assetPath,
                        RuntimeUiArtSlots.SemanticId(binding.Slot) + " contains "
                        + hiddenRgbPixels + " hidden RGB pixel(s) and "
                        + lowAlphaPixels + " low-alpha ringing pixel(s).",
                        "Clear RGB at alpha zero and clear low-alpha ringing after every source/runtime resize for every transparent ImageGen nine-slice material.");
                }
            }

            var lineFreeCarrier = binding.Slot == RuntimeUiArtSlot.SurfaceMetric
                || binding.Slot == RuntimeUiArtSlot.SlotNursery;
            if (lineFreeCarrier)
            {
                var darkPartialFringePixels = pixels.Count(pixel =>
                {
                    if (pixel.a == 0 || pixel.a == 255) return false;
                    var minimum = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
                    var maximum = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
                    return maximum < 160
                        || maximum - minimum <= 10 && minimum < 225;
                });
                if (darkPartialFringePixels > 0)
                {
                    report.Error("material.line-free-carrier.alpha-fringe", assetPath,
                        RuntimeUiArtSlots.SemanticId(binding.Slot) + " contains "
                        + darkPartialFringePixels
                        + " dark/neutral partial-alpha fringe pixel(s).",
                        "Derive alpha from the selected carrier's own pixels, clear low-alpha ringing after each resize, and never reuse another component's geometry mask.");
                }
            }

            if (binding.Slot == RuntimeUiArtSlot.SurfaceMetric
                && HasContinuousMetricPerimeterRail(pixels, width, height))
            {
                report.Error("material.metric.linear-rail.pixels", assetPath,
                    "The compact metric carrier contains a continuous dark perimeter rail.",
                    "Use the dedicated line-free surface.metric master; do not reuse a bordered panel master or suppress the metric surface.");
            }

            if (binding.Slot == RuntimeUiArtSlot.SlotNursery)
            {
                var railPixels = pixels.Count(pixel => pixel.a >= 96
                    && pixel.r >= 190 && pixel.g >= 120 && pixel.b <= 135
                    && pixel.r - pixel.g >= 20 && pixel.g - pixel.b >= 40);
                if (railPixels > 4)
                {
                    report.Error("material.nursery.linear-rail.pixels", assetPath,
                        "The rail-free nursery slot still contains " + railPixels
                        + " orange solid/dashed rail pixel(s).",
                        "Replace only slot.nursery with the reviewed line-free rounded paper master; do not hide its surface or change the nine-slice renderer.");
                }
            }
            var witnesses = new[]
            {
                new KeyValuePair<string, string>("outer cream rim", row.outer_cream_rgb),
                new KeyValuePair<string, string>("face", row.face_rgb),
                new KeyValuePair<string, string>("soil outline", row.soil_outline_rgb),
                new KeyValuePair<string, string>("upper highlight", row.upper_highlight_rgb),
                new KeyValuePair<string, string>("short bottom shadow", row.bottom_shadow_rgb),
            };
            foreach (var witness in witnesses)
            {
                if (!TryParseRgb(witness.Value, out var expected)) continue;
                var minimumAlpha = witness.Key == "short bottom shadow" ? 24 : 96;
                var count = pixels.Count(pixel => pixel.a >= minimumAlpha
                    && Mathf.Abs(pixel.r - expected.r) <= 12
                    && Mathf.Abs(pixel.g - expected.g) <= 12
                    && Mathf.Abs(pixel.b - expected.b) <= 12);
                if (count >= 4) continue;
                report.Error("material.anatomy.pixel-witness", assetPath,
                    RuntimeUiArtSlots.SemanticId(binding.Slot) + " has only " + count
                    + " visible pixel witness(es) for " + witness.Key + ".",
                    "Rebuild the layer as independently visible runtime ink rather than a flat gradient.");
            }
            var stage = binding.Slot == RuntimeUiArtSlot.SurfaceGameplayStage;
            ValidateLayerWitness(report, assetPath, "outer cream rim",
                row.outer_cream_rgb, pixels, width, height, 64, stage ? 9 : 5, 96);
            ValidateLayerWitness(report, assetPath, "soil outline",
                row.soil_outline_rgb, pixels, width, height, 64, stage ? 12 : 8, 96);
            ValidateLayerWitness(report, assetPath, "upper highlight",
                row.upper_highlight_rgb, pixels, width, height, 64, stage ? 14 : 11, 96);
            ValidateLayerWitness(report, assetPath, "face",
                row.face_rgb, pixels, width, height, 64, stage ? 113 : 20, 96);
            ValidateLayerWitness(report, assetPath, "short bottom shadow",
                row.bottom_shadow_rgb, pixels, width, height, 64, 122, 24);
        }

        private static bool HasContinuousMetricPerimeterRail(
            Color32[] pixels, int width, int height)
        {
            if (pixels == null || pixels.Length != width * height
                || width < 8 || height < 8)
                return false;
            var minimumX = width / 4;
            var maximumX = width - minimumX;
            var minimumY = height / 4;
            var maximumY = height - minimumY;
            var searchDepth = Mathf.Max(1, Mathf.Min(width, height) / 4);
            for (var offset = 0; offset < searchDepth; offset++)
            {
                if (HasDarkHorizontalRail(pixels, width, offset,
                        minimumX, maximumX)
                    || HasDarkHorizontalRail(pixels, width,
                        height - 1 - offset, minimumX, maximumX)
                    || HasDarkVerticalRail(pixels, width, offset,
                        minimumY, maximumY)
                    || HasDarkVerticalRail(pixels, width,
                        width - 1 - offset, minimumY, maximumY))
                    return true;
            }
            return false;
        }

        private static bool HasDarkHorizontalRail(Color32[] pixels, int width,
            int y, int minimumX, int maximumX)
        {
            var dark = 0;
            for (var x = minimumX; x < maximumX; x++)
                if (IsDarkCarrierRailPixel(pixels[y * width + x])) dark++;
            return dark * 4 >= (maximumX - minimumX) * 3;
        }

        private static bool HasDarkVerticalRail(Color32[] pixels, int width,
            int x, int minimumY, int maximumY)
        {
            var dark = 0;
            for (var y = minimumY; y < maximumY; y++)
                if (IsDarkCarrierRailPixel(pixels[y * width + x])) dark++;
            return dark * 4 >= (maximumY - minimumY) * 3;
        }

        private static bool IsDarkCarrierRailPixel(Color32 pixel)
        {
            return pixel.a >= 96 && pixel.r + pixel.g + pixel.b < 225 * 3;
        }

        private static void ValidateLayerWitness(
            RuntimeUiVisualValidationReport report, string assetPath, string layer,
            string expectedRgb, Color32[] pixels, int width, int height,
            int x, int yFromTop, byte minimumAlpha)
        {
            if (!TryParseRgb(expectedRgb, out var expected) || width <= x
                || height <= yFromTop)
                return;
            var pixel = pixels[(height - 1 - yFromTop) * width + x];
            var tolerance = layer == "short bottom shadow" ? 30 : 12;
            if (pixel.a >= minimumAlpha
                && Mathf.Abs(pixel.r - expected.r) <= tolerance
                && Mathf.Abs(pixel.g - expected.g) <= tolerance
                && Mathf.Abs(pixel.b - expected.b) <= tolerance)
                return;
            report.Error("material.anatomy.layer-position", assetPath,
                layer + " is not independently visible at its protected witness; actual=#"
                + pixel.r.ToString("X2") + pixel.g.ToString("X2")
                + pixel.b.ToString("X2") + "/" + pixel.a + ".",
                "Restore the fixed five-layer material order in the deterministic master.");
        }

        private static string ExpectedReferenceMaterialRecipe(RuntimeUiArtSlot slot)
        {
            switch (slot)
            {
                case RuntimeUiArtSlot.SurfaceStatus: return "sunlight-phase-status";
                default: return string.Empty;
            }
        }

        private static bool IsImagegenMaterialSlot(RuntimeUiArtSlot slot)
        {
            return slot == RuntimeUiArtSlot.ActionSecondary
                || slot == RuntimeUiArtSlot.ActionQuiet
                || slot == RuntimeUiArtSlot.ActionDanger
                || slot == RuntimeUiArtSlot.ActionCompactControl
                || slot == RuntimeUiArtSlot.ActionCompactControlActive
                || slot == RuntimeUiArtSlot.SurfaceSafeArea
                || slot == RuntimeUiArtSlot.SurfacePanelStandard
                || slot == RuntimeUiArtSlot.SurfacePanelRaised
                || slot == RuntimeUiArtSlot.SurfaceMetric
                || slot == RuntimeUiArtSlot.SurfaceCardSelectable
                || slot == RuntimeUiArtSlot.SlotTool
                || slot == RuntimeUiArtSlot.SlotNursery
                || slot == RuntimeUiArtSlot.SurfaceGameplayStage;
        }

        private static bool IsRgbHex(string value)
        {
            return TryParseRgb(value, out _);
        }

        private static bool TryParseRgb(string value, out Color32 color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(value) || value.Length != 6) return false;
            try
            {
                color = new Color32(Convert.ToByte(value.Substring(0, 2), 16),
                    Convert.ToByte(value.Substring(2, 2), 16),
                    Convert.ToByte(value.Substring(4, 2), 16), byte.MaxValue);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

    }
}
