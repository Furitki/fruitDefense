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
        private static void ValidateMicroSilhouetteFamily(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet)
        {
            var slots = new[]
            {
                RuntimeUiArtSlot.IconResourceSunMicro,
                RuntimeUiArtSlot.IconResourceCoreMicro,
                RuntimeUiArtSlot.IconResourceWaveMicro,
            };
            var masks = new List<(RuntimeUiArtSlot Slot, string Path, bool[] Mask)>();
            foreach (var slot in slots)
            {
                if (!artSet.TryGetBinding(slot, out var binding) || binding?.Texture == null)
                    continue;
                var path = RuntimeUiArtSetRegistry.Normalize(
                    AssetDatabase.GetAssetPath(binding.Texture));
                var texture = DecodePng(report, path, "icon.micro.decode");
                try
                {
                    if (texture == null
                        || texture.width != RuntimeUiQualityProfile.MicroIconCanvasSize
                        || texture.height != RuntimeUiQualityProfile.MicroIconCanvasSize)
                        continue;
                    var pixels = texture.GetPixels32();
                    masks.Add((slot, path, pixels.Select(pixel =>
                        pixel.a >= RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh)
                        .ToArray()));
                }
                finally
                {
                    if (texture != null) Object.DestroyImmediate(texture);
                }
            }

            for (var firstIndex = 0; firstIndex < masks.Count; firstIndex++)
            for (var secondIndex = firstIndex + 1; secondIndex < masks.Count; secondIndex++)
            {
                var first = masks[firstIndex];
                var second = masks[secondIndex];
                var intersection = 0;
                var union = 0;
                for (var pixelIndex = 0; pixelIndex < first.Mask.Length; pixelIndex++)
                {
                    if (first.Mask[pixelIndex] && second.Mask[pixelIndex]) intersection++;
                    if (first.Mask[pixelIndex] || second.Mask[pixelIndex]) union++;
                }
                var iou = union == 0 ? 1f : intersection / (float)union;
                if (iou < RuntimeUiQualityProfile.MicroIconSilhouetteIouMaximum)
                    continue;
                report.Error("icon.micro.silhouette-confusion", first.Path,
                    RuntimeUiArtSlots.SemanticId(first.Slot) + " and "
                    + RuntimeUiArtSlots.SemanticId(second.Slot)
                    + " have silhouette IoU " + iou.ToString("0.###") + ".",
                    "Redesign the target-size silhouette rather than relying on color.");
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

        private static void ValidateOpticalInset(RuntimeUiVisualValidationReport report,
            string assetPath, RuntimeUiArtBinding binding, Color32[] pixels,
            int width, int height)
        {
            if (!TrySignificantAlphaBounds(pixels, width, height,
                    RuntimeUiQualityProfile.NineSliceSignificantAlphaHigh,
                    out var bounds))
            {
                report.Error("optical-inset.alpha-empty", assetPath,
                    "Runtime art has no significant alpha for its optical contract.",
                    "Restore visible runtime art and regenerate optical metadata.");
                return;
            }

            var expected = new RuntimeUiPixelInsets(
                bounds.xMin,
                height - bounds.yMax,
                width - bounds.xMax,
                bounds.yMin);
            if (SameInsets(expected, binding.OpticalInset))
                return;
            report.Error("optical-inset.stale", assetPath,
                "Serialized optical inset does not match the final runtime PNG alpha bounds.",
                "Regenerate the ArtSet from the owned exporter.");
        }

        private static bool TrySignificantAlphaBounds(Color32[] pixels,
            int width, int height, byte minimumAlpha, out RectInt bounds)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < minimumAlpha)
                    continue;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
            if (maxX < minX || maxY < minY)
            {
                bounds = default;
                return false;
            }
            bounds = new RectInt(minX, minY,
                maxX - minX + 1, maxY - minY + 1);
            return true;
        }

        private static void ValidateNineSlicePixels(RuntimeUiVisualValidationReport report,
            string assetPath, RuntimeUiArtBinding binding, Color32[] pixels,
            int width, int height)
        {
            var expectedBorder = binding.Slot == RuntimeUiArtSlot.SurfaceGameplayStage
                ? RuntimeUiQualityProfile.GameplayStageNineSliceBorder
                : RuntimeUiQualityProfile.NineSliceBorder;
            if (width != RuntimeUiQualityProfile.NineSliceCanvasSize
                || height != RuntimeUiQualityProfile.NineSliceCanvasSize
                || binding.SliceBorder.Left != expectedBorder
                || binding.SliceBorder.Right != expectedBorder
                || binding.SliceBorder.Top != expectedBorder
                || binding.SliceBorder.Bottom != expectedBorder
                || binding.SafeInset.Left != RuntimeUiQualityProfile.NineSliceSafeInset
                || binding.SafeInset.Right != RuntimeUiQualityProfile.NineSliceSafeInset
                || binding.SafeInset.Top != RuntimeUiQualityProfile.NineSliceSafeInset
                || binding.SafeInset.Bottom != RuntimeUiQualityProfile.NineSliceSafeInset)
            {
                report.Error("nine-slice.quality-profile", assetPath,
                    "Production nine-slice must be 128x128 with its semantic border "
                    + expectedBorder + " and safeInset20.",
                    "Restore the reviewed nine-slice geometry metadata.");
            }

            var allowMatchedTransparency =
                binding.Slot == RuntimeUiArtSlot.SurfaceIllustrationFrame
                || binding.Slot == RuntimeUiArtSlot.SurfaceGameplayStage;
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
                        "The transparent-frame stretch center contains visible protected ornament alpha.",
                        "Keep frame rails and corner motifs inside the fixed semantic border.");
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
            else if (binding.Slot == RuntimeUiArtSlot.IllustrationShellOrchardDepth)
            {
                expectedWidth = 402;
                expectedHeight = 874;
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
                || binding.Slot == RuntimeUiArtSlot.SurfaceScrim
                || binding.Slot == RuntimeUiArtSlot.IllustrationShellOrchardDepth)
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

    }
}
