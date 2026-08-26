using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FruitDefense.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace FruitDefense.Editor
{
    public static class CombatFloatingTextSdfGenerator
    {
        public const string SourceFontPath =
            "Assets/Resources/Fonts/NotoSansSC-UI.ttf";
        public const string OutputDirectory =
            "Assets/Resources/CombatFeedback";
        public const string AtlasAssetPath =
            OutputDirectory + "/CombatFloatingTextAtlas.asset";
        public const string MetadataAssetPath =
            OutputDirectory + "/CombatFloatingTextAtlasMetadata.asset";
        public const int AtlasSize = 512;
        public const int SamplingPointSize = 64;
        public const int AtlasPadding = 8;
        public const int BakedGlyphPadding = 8;
        public const GlyphRenderMode RenderMode = GlyphRenderMode.SDF32;
        public const float FaceThreshold = .43f;
        public const float FaceTransition = .04f;
        public const float OutlineThreshold = .10f;
        public const float OutlineTransition = .05f;
        public const int MinimumBakedFacePixels = 10000;
        public const float MinimumOutlineToFaceRatio = 2f;
        public const int CompositeRegionX = 192;
        public const int CompositeTokenPointSize = 24;
        public const int CompositePackGap = 1;
        public const int RequiredCompositeTokenCount = 114;
        public const int OptionalCompositeTokenCount = 10;

        private const string GenerationShaderName =
            "TextMeshPro/Mobile/Distance Field";

        private sealed class CompositeTokenBuild
        {
            public string Text;
            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;
            public float HorizontalAdvance;
            public int Width;
            public int Height;
            public RectInt PackedRect;
            public Color32[] Pixels;
        }
        private static readonly string[] LegacyProductionPaths =
        {
            OutputDirectory + "/CombatFloatingTextSdf.asset",
            OutputDirectory + "/CombatFloatingTextSdfAtlas.asset",
            OutputDirectory + "/CombatFloatingTextStandard.mat",
            OutputDirectory + "/CombatFloatingTextHeavy.mat",
        };
        private static readonly string[] UnusedEssentialResourcePaths =
        {
            "Assets/TextMesh Pro/Fonts",
            "Assets/TextMesh Pro/Sprites",
            "Assets/TextMesh Pro/Resources/Fonts & Materials",
            "Assets/TextMesh Pro/Resources/Sprite Assets",
            "Assets/TextMesh Pro/Shaders/TMP_SDF Overlay.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Overlay.shader",
            "Assets/TextMesh Pro/Shaders/TMP_Bitmap.shader",
            "Assets/TextMesh Pro/Shaders/TMP_Bitmap-Mobile.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Surface.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile SSD.shader",
            "Assets/TextMesh Pro/Shaders/SDFFunctions.hlsl",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-HDRP UNLIT.shadergraph",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-HDRP LIT.shadergraph",
            "Assets/TextMesh Pro/Shaders/TMP_Sprite.shader",
            "Assets/TextMesh Pro/Shaders/TMPro.cginc",
            "Assets/TextMesh Pro/Shaders/TMPro_Mobile.cginc",
            "Assets/TextMesh Pro/Shaders/TMPro_Surface.cginc",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Masking.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF SSD.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-URP Lit.shadergraph",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-URP Unlit.shadergraph",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Surface-Mobile.shader",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile-2-Pass.shader",
            "Assets/TextMesh Pro/Shaders/TMP_Bitmap-Custom-Atlas.shader",
        };

        [MenuItem("Fruit Defense/Combat Feedback/Rebuild Baked Atlas")]
        public static void Rebuild()
        {
            EnsureTmpEssentialResources();
            EnsureOutputDirectory();
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
                throw new InvalidOperationException(
                    "Packaged combat atlas source font is missing: " + SourceFontPath);

            TMP_FontAsset generated = null;
            Texture2D generatedAtlas = null;
            Material generatedMaterial = null;
            Texture2D baked = null;
            try
            {
                generated = TMP_FontAsset.CreateFontAsset(
                    sourceFont, SamplingPointSize, AtlasPadding, RenderMode,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
                if (generated == null)
                    throw new InvalidOperationException(
                        "TMP failed to create the temporary combat distance field.");
                string missing;
                if (!generated.TryAddCharacters(
                        CombatFloatingTextStyleCatalog.RuntimeGlyphInventory,
                        out missing, false)
                    || !string.IsNullOrEmpty(missing))
                    throw new InvalidOperationException(
                        "Combat atlas source font is missing reviewed glyphs: "
                        + FormatCodePoints(missing));
                if (generated.atlasTextureCount != 1)
                    throw new InvalidOperationException(
                        "Temporary combat distance field exceeded one atlas page.");
                generated.ReadFontAssetDefinition();
                generatedAtlas = generated.atlasTexture;
                generatedMaterial = generated.material;
                if (generatedAtlas == null)
                    throw new InvalidOperationException(
                        "TMP did not create the temporary distance-field texture.");

                var glyphs = BuildGlyphs(generated);
                baked = BakeAtlas(generatedAtlas);
                var compositeTokens = BakeCompositeTokens(baked, glyphs);
                var atlas = UpsertAtlas(baked);
                baked = null;
                var metadata = UpsertMetadata(glyphs, compositeTokens, generated);
                ConfigureTmpSettingsForEditorGeneration();
                DeleteLegacyProductionAssets();
                PruneUnusedEssentialResources();
                EditorUtility.SetDirty(atlas);
                EditorUtility.SetDirty(metadata);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AtlasAssetPath,
                    ImportAssetOptions.ForceSynchronousImport
                    | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(MetadataAssetPath,
                    ImportAssetOptions.ForceSynchronousImport
                    | ImportAssetOptions.ForceUpdate);
                ValidateOrThrow();
                Debug.Log("FRUIT_DEFENSE_COMBAT_SDF_GENERATED");
            }
            finally
            {
                DestroyTransient(baked);
                DestroyTransient(generatedMaterial);
                DestroyTransient(generatedAtlas);
                if (generated != null)
                {
                    generated.atlasTextures = Array.Empty<Texture2D>();
                    generated.material = null;
                }
                DestroyTransient(generated);
            }
        }

        public static void GenerateForBatchMode()
        {
            try { Rebuild(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        public static IReadOnlyList<string> ValidateGeneratedAssets()
        {
            var issues = new List<string>();
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasAssetPath);
            var metadata = AssetDatabase.LoadAssetAtPath<CombatFloatingTextAtlasMetadata>(
                MetadataAssetPath);
            if (source == null) issues.Add("missing-source-font:" + SourceFontPath);
            if (atlas == null) issues.Add("missing-atlas:" + AtlasAssetPath);
            if (metadata == null) issues.Add("missing-metadata:" + MetadataAssetPath);
            for (var index = 0; index < LegacyProductionPaths.Length; index++)
                if (AssetDatabase.LoadMainAssetAtPath(LegacyProductionPaths[index]) != null)
                    issues.Add("legacy-production-asset-present:"
                        + LegacyProductionPaths[index]);
            if (atlas == null || metadata == null) return issues.AsReadOnly();

            if (atlas.width != AtlasSize || atlas.height != AtlasSize)
                issues.Add("atlas-size-not-512");
            if (atlas.format != TextureFormat.RGBA32)
                issues.Add("atlas-not-rgba32:" + atlas.format);
            if (atlas.mipmapCount != 1) issues.Add("atlas-has-mipmaps");
            if (atlas.filterMode != FilterMode.Bilinear)
                issues.Add("atlas-filter-not-bilinear");
            if (atlas.wrapMode != TextureWrapMode.Clamp)
                issues.Add("atlas-wrap-not-clamp");

            var inventory = CombatFloatingTextStyleCatalog.RuntimeGlyphInventory;
            if (!string.Equals(metadata.GlyphInventory, inventory,
                    StringComparison.Ordinal)
                || metadata.GlyphCount != inventory.Length)
                issues.Add("metadata-glyph-inventory-drift");
            for (var index = 0; index < inventory.Length; index++)
            {
                CombatFloatingTextGlyph glyph;
                if (!metadata.TryGetGlyph(inventory[index], out glyph))
                {
                    issues.Add("missing-glyph:U+"
                        + ((int)inventory[index]).ToString("X4"));
                    continue;
                }
                if (glyph.AtlasRect.xMin < 0 || glyph.AtlasRect.yMin < 0
                    || glyph.AtlasRect.xMax > AtlasSize
                    || glyph.AtlasRect.yMax > AtlasSize
                    || glyph.Scale <= 0f || glyph.HorizontalAdvance < 0f)
                    issues.Add("invalid-glyph-metrics:U+"
                        + ((int)inventory[index]).ToString("X4"));
            }

            var compositeRegion = new RectInt(CompositeRegionX, 0,
                AtlasSize - CompositeRegionX, AtlasSize);
            var pixels = atlas.GetPixels32();
            if (metadata.CompositeRegion != compositeRegion)
                issues.Add("composite-region-drift:" + metadata.CompositeRegion);
            if (!Mathf.Approximately(metadata.CompositeBasePointSize,
                    CompositeTokenPointSize))
                issues.Add("composite-base-point-size-drift");
            var expectedTokens = BuildCompositeTokenTexts(true);
            if (metadata.CompositeTokenCount != expectedTokens.Count)
                issues.Add("composite-token-count-drift:"
                    + metadata.CompositeTokenCount + "/" + expectedTokens.Count);
            var occupied = new bool[compositeRegion.width * compositeRegion.height];
            for (var index = 0; index < expectedTokens.Count; index++)
            {
                CombatFloatingTextCompositeToken token;
                var tokenText = expectedTokens[index];
                if (!metadata.TryGetCompositeToken(tokenText, out token))
                {
                    issues.Add("missing-composite-token:" + tokenText);
                    continue;
                }
                if (token.Text != tokenText
                    || !compositeRegion.Contains(token.AtlasRect.min)
                    || token.AtlasRect.xMax > compositeRegion.xMax
                    || token.AtlasRect.yMax > compositeRegion.yMax
                    || token.AtlasRect.width <= 0 || token.AtlasRect.height <= 0
                    || !Mathf.Approximately(token.BaseScale,
                        CompositeTokenPointSize / (float)SamplingPointSize)
                    || token.MaxX <= token.MinX || token.MaxY <= token.MinY
                    || token.HorizontalAdvance <= 0f)
                {
                    issues.Add("invalid-composite-token:" + tokenText);
                    continue;
                }
                var visiblePixels = 0;
                var overlapsExisting = false;
                for (var y = token.AtlasRect.yMin; y < token.AtlasRect.yMax; y++)
                for (var x = token.AtlasRect.xMin; x < token.AtlasRect.xMax; x++)
                {
                    var occupiedIndex = (y - compositeRegion.y) * compositeRegion.width
                        + x - compositeRegion.x;
                    if (occupied[occupiedIndex]) overlapsExisting = true;
                    occupied[occupiedIndex] = true;
                    if (pixels[y * AtlasSize + x].a > 0) visiblePixels++;
                }
                if (overlapsExisting)
                    issues.Add("overlapping-composite-token:" + tokenText);
                if (visiblePixels == 0)
                    issues.Add("empty-composite-token:" + tokenText);
            }

            var transparent = 0;
            var outline = 0;
            var face = 0;
            var outlineColor = (Color32)CombatFloatingTextStyleCatalog.SharedOutlineColor;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.a == 0) transparent++;
                if (pixel.a > 32 && ColorDistance(pixel, outlineColor) <= 48)
                    outline++;
                if (pixel.a > 128 && pixel.r >= 220
                    && pixel.g >= 220 && pixel.b >= 220)
                    face++;
            }
            if (transparent == 0 || outline == 0 || face == 0)
                issues.Add("atlas-lacks-transparent-outline-or-face-pixels");
            if (face < MinimumBakedFacePixels)
                issues.Add("atlas-face-coverage-too-sparse:" + face);
            if (outline < face * MinimumOutlineToFaceRatio)
                issues.Add("atlas-outline-coverage-too-thin:"
                    + outline + "/" + face);
            for (var y = compositeRegion.yMin; y < compositeRegion.yMax; y++)
            for (var x = compositeRegion.xMin; x < compositeRegion.xMax; x++)
            {
                var occupiedIndex = (y - compositeRegion.y) * compositeRegion.width
                    + x - compositeRegion.x;
                if (occupied[occupiedIndex]
                    || pixels[y * AtlasSize + x].a == 0) continue;
                issues.Add("composite-region-gap-not-transparent:" + x + "," + y);
                y = compositeRegion.yMax;
                break;
            }
            return issues.AsReadOnly();
        }

        public static void ValidateOrThrow()
        {
            var issues = ValidateGeneratedAssets();
            if (issues.Count > 0)
                throw new InvalidOperationException(
                    "Generated combat atlas assets are invalid:\n"
                    + string.Join("\n", issues));
        }

        private static Texture2D BakeAtlas(Texture2D source)
        {
            var sourcePixels = source.GetPixels32();
            if (sourcePixels.Length != AtlasSize * AtlasSize)
                throw new InvalidOperationException(
                    "Temporary distance-field atlas size is invalid.");
            var output = new Texture2D(AtlasSize, AtlasSize,
                TextureFormat.RGBA32, false, true)
            {
                name = "CombatFloatingTextAtlas",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
            };
            var outputPixels = new Color32[sourcePixels.Length];
            var outlineColor = CombatFloatingTextStyleCatalog.SharedOutlineColor;
            for (var index = 0; index < sourcePixels.Length; index++)
            {
                var distance = sourcePixels[index].a / 255f;
                var outlineCoverage = SmoothStep(
                    OutlineThreshold - OutlineTransition,
                    OutlineThreshold + OutlineTransition, distance);
                var faceCoverage = SmoothStep(
                    FaceThreshold - FaceTransition,
                    FaceThreshold + FaceTransition, distance);
                if (outlineCoverage <= 0f)
                {
                    outputPixels[index] = new Color32(0, 0, 0, 0);
                    continue;
                }
                var rgb = Color.Lerp(outlineColor, Color.white, faceCoverage);
                rgb.a = outlineCoverage;
                outputPixels[index] = (Color32)rgb;
            }
            output.SetPixels32(outputPixels);
            output.Apply(false, false);
            return output;
        }

        private static CombatFloatingTextGlyph[] BuildGlyphs(TMP_FontAsset font)
        {
            var inventory = CombatFloatingTextStyleCatalog.RuntimeGlyphInventory;
            var result = new CombatFloatingTextGlyph[inventory.Length];
            for (var index = 0; index < inventory.Length; index++)
            {
                TMP_Character character;
                if (!font.characterLookupTable.TryGetValue(inventory[index],
                        out character) || character.glyph == null)
                    throw new InvalidOperationException(
                        "Temporary distance field lacks glyph U+"
                        + ((int)inventory[index]).ToString("X4"));
                var glyph = character.glyph;
                var rect = glyph.glyphRect;
                var x = Mathf.Max(0, rect.x - BakedGlyphPadding);
                var y = Mathf.Max(0, rect.y - BakedGlyphPadding);
                var xMax = Mathf.Min(AtlasSize,
                    rect.x + rect.width + BakedGlyphPadding);
                var yMax = Mathf.Min(AtlasSize,
                    rect.y + rect.height + BakedGlyphPadding);
                var metrics = glyph.metrics;
                result[index] = new CombatFloatingTextGlyph
                {
                    CodePoint = inventory[index],
                    AtlasRect = new RectInt(x, y, xMax - x, yMax - y),
                    Width = metrics.width,
                    Height = metrics.height,
                    HorizontalBearingX = metrics.horizontalBearingX,
                    HorizontalBearingY = metrics.horizontalBearingY,
                    HorizontalAdvance = metrics.horizontalAdvance,
                    Scale = character.scale * glyph.scale,
                    Padding = BakedGlyphPadding,
                };
            }
            return result;
        }

        private static Texture2D UpsertAtlas(Texture2D generated)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasAssetPath);
            if (existing == null)
            {
                generated.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(generated, AtlasAssetPath);
                return generated;
            }
            EditorUtility.CopySerialized(generated, existing);
            existing.name = "CombatFloatingTextAtlas";
            existing.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(existing);
            DestroyTransient(generated);
            return existing;
        }

        private static CombatFloatingTextAtlasMetadata UpsertMetadata(
            CombatFloatingTextGlyph[] glyphs,
            CombatFloatingTextCompositeToken[] compositeTokens,
            TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CombatFloatingTextAtlasMetadata>(
                MetadataAssetPath);
            if (existing == null
                && AssetDatabase.LoadMainAssetAtPath(MetadataAssetPath) != null)
            {
                if (!AssetDatabase.DeleteAsset(MetadataAssetPath))
                    throw new InvalidOperationException(
                        "Unable to replace invalid combat atlas metadata asset.");
            }
            var metadata = existing != null
                ? existing : ScriptableObject.CreateInstance<CombatFloatingTextAtlasMetadata>();
            metadata.name = "CombatFloatingTextAtlasMetadata";
            metadata.Configure(font.faceInfo.pointSize,
                font.faceInfo.ascentLine, font.faceInfo.descentLine,
                CombatFloatingTextStyleCatalog.RuntimeGlyphInventory, glyphs,
                new RectInt(CompositeRegionX, 0,
                    AtlasSize - CompositeRegionX, AtlasSize),
                CompositeTokenPointSize, compositeTokens);
            if (existing == null) AssetDatabase.CreateAsset(metadata, MetadataAssetPath);
            EditorUtility.SetDirty(metadata);
            return metadata;
        }

        private static CombatFloatingTextCompositeToken[] BakeCompositeTokens(
            Texture2D atlas, CombatFloatingTextGlyph[] glyphs)
        {
            var atlasPixels = atlas.GetPixels32();
            EnsureCompositeRegionIsTransparent(atlasPixels);
            var tokenTexts = BuildCompositeTokenTexts(true);
            var builds = BuildCompositeTokenRasters(tokenTexts, atlasPixels, glyphs);
            RectInt packedBounds;
            if (!TryPackCompositeTokens(builds, out packedBounds))
            {
                tokenTexts = BuildCompositeTokenTexts(false);
                builds = BuildCompositeTokenRasters(tokenTexts, atlasPixels, glyphs);
                if (!TryPackCompositeTokens(builds, out packedBounds))
                    throw new InvalidOperationException(
                        "The required 24px combat composite tokens do not fit the "
                        + (AtlasSize - CompositeRegionX) + "x" + AtlasSize
                        + " transparent atlas region.");
            }

            var rasterPixels = 0;
            for (var buildIndex = 0; buildIndex < builds.Count; buildIndex++)
            {
                var build = builds[buildIndex];
                rasterPixels += build.Width * build.Height;
                for (var y = 0; y < build.Height; y++)
                for (var x = 0; x < build.Width; x++)
                    atlasPixels[(build.PackedRect.y + y) * AtlasSize
                        + build.PackedRect.x + x] = build.Pixels[y * build.Width + x];
            }
            atlas.SetPixels32(atlasPixels);
            atlas.Apply(false, false);

            var baseScale = CompositeTokenPointSize / (float)SamplingPointSize;
            var result = builds
                .Select(value => new CombatFloatingTextCompositeToken
                {
                    Text = value.Text,
                    AtlasRect = value.PackedRect,
                    BaseScale = baseScale,
                    MinX = value.MinX,
                    MaxX = value.MaxX,
                    MinY = value.MinY,
                    MaxY = value.MaxY,
                    HorizontalAdvance = value.HorizontalAdvance,
                })
                .ToArray();
            var availablePixels = (AtlasSize - CompositeRegionX) * AtlasSize;
            Debug.Log("FRUIT_DEFENSE_COMBAT_COMPOSITE_CAPACITY tokens="
                + result.Length + " packedBounds=" + packedBounds
                + " rasterPixels=" + rasterPixels
                + " remainingRegionPixels=" + (availablePixels - rasterPixels));
            return result;
        }

        private static List<string> BuildCompositeTokenTexts(bool includeOptional)
        {
            var capacity = RequiredCompositeTokenCount
                + (includeOptional ? OptionalCompositeTokenCount : 0);
            var result = new List<string>(capacity);
            for (var digit = 0; digit <= 9; digit++)
                result.Add("-" + digit.ToString(CultureInfo.InvariantCulture));
            for (var value = 0; value <= 99; value++)
                result.Add("-" + value.ToString("00", CultureInfo.InvariantCulture));
            result.Add("冻结");
            result.Add("击败");
            result.Add("击败×");
            result.Add(" 阳光");
            if (includeOptional)
                for (var digit = 0; digit <= 9; digit++)
                    result.Add("+" + digit.ToString(CultureInfo.InvariantCulture));
            if (result.Count != capacity || result.Distinct().Count() != result.Count)
                throw new InvalidOperationException(
                    "Composite token inventory is not finite and duplicate-free.");
            return result;
        }

        private static List<CompositeTokenBuild> BuildCompositeTokenRasters(
            IReadOnlyList<string> tokenTexts, Color32[] atlasPixels,
            CombatFloatingTextGlyph[] glyphs)
        {
            var result = new List<CompositeTokenBuild>(tokenTexts.Count);
            var baseScale = CompositeTokenPointSize / (float)SamplingPointSize;
            for (var tokenIndex = 0; tokenIndex < tokenTexts.Count; tokenIndex++)
            {
                var text = tokenTexts[tokenIndex];
                var build = MeasureCompositeToken(text, glyphs);
                build.Width = Mathf.Max(1,
                    Mathf.CeilToInt((build.MaxX - build.MinX) * baseScale));
                build.Height = Mathf.Max(1,
                    Mathf.CeilToInt((build.MaxY - build.MinY) * baseScale));
                build.Pixels = RasterCompositeToken(
                    build, atlasPixels, glyphs, baseScale);
                result.Add(build);
            }
            return result;
        }

        private static CompositeTokenBuild MeasureCompositeToken(
            string text, CombatFloatingTextGlyph[] glyphs)
        {
            var result = new CompositeTokenBuild
            {
                Text = text,
                MinX = float.PositiveInfinity,
                MaxX = float.NegativeInfinity,
                MinY = float.PositiveInfinity,
                MaxY = float.NegativeInfinity,
            };
            var cursor = 0f;
            for (var index = 0; index < text.Length; index++)
            {
                var glyph = FindGlyph(glyphs, text[index]);
                var scale = glyph.Scale;
                if (glyph.Width > 0f && glyph.Height > 0f)
                {
                    result.MinX = Mathf.Min(result.MinX,
                        cursor + (glyph.HorizontalBearingX - glyph.Padding) * scale);
                    result.MaxX = Mathf.Max(result.MaxX,
                        cursor + (glyph.HorizontalBearingX + glyph.Width
                            + glyph.Padding) * scale);
                    result.MinY = Mathf.Min(result.MinY,
                        (glyph.HorizontalBearingY - glyph.Height
                            - glyph.Padding) * scale);
                    result.MaxY = Mathf.Max(result.MaxY,
                        (glyph.HorizontalBearingY + glyph.Padding) * scale);
                }
                cursor += glyph.HorizontalAdvance * scale;
            }
            if (float.IsInfinity(result.MinX)
                || result.MaxX <= result.MinX || result.MaxY <= result.MinY)
                throw new InvalidOperationException(
                    "Composite token has no visible glyphs: " + text);
            result.HorizontalAdvance = cursor;
            return result;
        }

        private static Color32[] RasterCompositeToken(CompositeTokenBuild build,
            Color32[] atlasPixels, CombatFloatingTextGlyph[] glyphs,
            float baseScale)
        {
            var result = new Color32[build.Width * build.Height];
            var cursor = 0f;
            for (var characterIndex = 0;
                 characterIndex < build.Text.Length; characterIndex++)
            {
                var glyph = FindGlyph(glyphs, build.Text[characterIndex]);
                var glyphScale = glyph.Scale;
                if (glyph.Width > 0f && glyph.Height > 0f)
                {
                    var left = cursor
                        + (glyph.HorizontalBearingX - glyph.Padding) * glyphScale;
                    var right = cursor
                        + (glyph.HorizontalBearingX + glyph.Width
                            + glyph.Padding) * glyphScale;
                    var bottom = (glyph.HorizontalBearingY - glyph.Height
                        - glyph.Padding) * glyphScale;
                    var top = (glyph.HorizontalBearingY + glyph.Padding) * glyphScale;
                    for (var y = 0; y < build.Height; y++)
                    {
                        var sourceY = build.MinY + (y + .5f) / baseScale;
                        if (sourceY < bottom || sourceY > top) continue;
                        for (var x = 0; x < build.Width; x++)
                        {
                            var sourceX = build.MinX + (x + .5f) / baseScale;
                            if (sourceX < left || sourceX > right) continue;
                            var u = Mathf.InverseLerp(left, right, sourceX);
                            var v = Mathf.InverseLerp(bottom, top, sourceY);
                            var sampled = SampleBilinear(
                                atlasPixels, glyph.AtlasRect, u, v);
                            var pixelIndex = y * build.Width + x;
                            result[pixelIndex] = AlphaComposite(
                                result[pixelIndex], sampled);
                        }
                    }
                }
                cursor += glyph.HorizontalAdvance * glyphScale;
            }
            return result;
        }

        private static bool TryPackCompositeTokens(
            List<CompositeTokenBuild> builds, out RectInt packedBounds)
        {
            var ordered = builds
                .OrderByDescending(value => value.Height)
                .ThenByDescending(value => value.Width)
                .ThenBy(value => value.Text, StringComparer.Ordinal)
                .ToArray();
            var x = CompositeRegionX;
            var y = 0;
            var shelfHeight = 0;
            var usedXMax = CompositeRegionX;
            var usedYMax = 0;
            for (var index = 0; index < ordered.Length; index++)
            {
                var build = ordered[index];
                if (build.Width > AtlasSize - CompositeRegionX
                    || build.Height > AtlasSize)
                {
                    packedBounds = default;
                    return false;
                }
                if (x + build.Width > AtlasSize)
                {
                    x = CompositeRegionX;
                    y += shelfHeight + CompositePackGap;
                    shelfHeight = 0;
                }
                if (y + build.Height > AtlasSize)
                {
                    packedBounds = new RectInt(CompositeRegionX, 0,
                        usedXMax - CompositeRegionX, usedYMax);
                    return false;
                }
                build.PackedRect = new RectInt(x, y, build.Width, build.Height);
                x += build.Width + CompositePackGap;
                shelfHeight = Mathf.Max(shelfHeight, build.Height);
                usedXMax = Mathf.Max(usedXMax, build.PackedRect.xMax);
                usedYMax = Mathf.Max(usedYMax, build.PackedRect.yMax);
            }
            packedBounds = new RectInt(CompositeRegionX, 0,
                usedXMax - CompositeRegionX, usedYMax);
            return true;
        }

        private static void EnsureCompositeRegionIsTransparent(Color32[] pixels)
        {
            for (var y = 0; y < AtlasSize; y++)
            for (var x = CompositeRegionX; x < AtlasSize; x++)
            {
                if (pixels[y * AtlasSize + x].a == 0) continue;
                throw new InvalidOperationException(
                    "Composite token target region is not transparent at "
                    + x + "," + y + ".");
            }
        }

        private static CombatFloatingTextGlyph FindGlyph(
            CombatFloatingTextGlyph[] glyphs, char character)
        {
            for (var index = 0; index < glyphs.Length; index++)
                if (glyphs[index].CodePoint == character) return glyphs[index];
            throw new InvalidOperationException(
                "Composite token glyph is outside the reviewed inventory: U+"
                + ((int)character).ToString("X4"));
        }

        private static Color32 SampleBilinear(Color32[] pixels,
            RectInt rect, float u, float v)
        {
            var sampleX = rect.x + Mathf.Clamp01(u) * rect.width - .5f;
            var sampleY = rect.y + Mathf.Clamp01(v) * rect.height - .5f;
            var x0 = Mathf.Clamp(Mathf.FloorToInt(sampleX), rect.x, rect.xMax - 1);
            var y0 = Mathf.Clamp(Mathf.FloorToInt(sampleY), rect.y, rect.yMax - 1);
            var x1 = Mathf.Min(x0 + 1, rect.xMax - 1);
            var y1 = Mathf.Min(y0 + 1, rect.yMax - 1);
            var tx = Mathf.Clamp01(sampleX - Mathf.Floor(sampleX));
            var ty = Mathf.Clamp01(sampleY - Mathf.Floor(sampleY));
            var bottom = Color.Lerp(
                pixels[y0 * AtlasSize + x0], pixels[y0 * AtlasSize + x1], tx);
            var top = Color.Lerp(
                pixels[y1 * AtlasSize + x0], pixels[y1 * AtlasSize + x1], tx);
            return (Color32)Color.Lerp(bottom, top, ty);
        }

        private static Color32 AlphaComposite(Color32 destination, Color32 source)
        {
            var sourceColor = (Color)source;
            var destinationColor = (Color)destination;
            var alpha = sourceColor.a
                + destinationColor.a * (1f - sourceColor.a);
            if (alpha <= 0f) return new Color32(0, 0, 0, 0);
            var rgb = (sourceColor * sourceColor.a
                + destinationColor * destinationColor.a
                    * (1f - sourceColor.a)) / alpha;
            rgb.a = alpha;
            return (Color32)rgb;
        }

        private static void ConfigureTmpSettingsForEditorGeneration()
        {
            var settings = TMP_Settings.instance;
            if (settings == null) return;
            TMP_Settings.defaultFontAsset = null;
            TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
            TMP_Settings.defaultSpriteAsset = null;
            TMP_Settings.emojiFallbackTextAssets = new List<TMP_Asset>();
            TMP_Settings.enableEmojiSupport = false;
            EditorUtility.SetDirty(settings);
        }

        private static void DeleteLegacyProductionAssets()
        {
            for (var index = 0; index < LegacyProductionPaths.Length; index++)
            {
                var path = LegacyProductionPaths[index];
                if (AssetDatabase.LoadMainAssetAtPath(path) == null) continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException(
                        "Unable to delete legacy combat SDF asset: " + path);
            }
        }

        private static void PruneUnusedEssentialResources()
        {
            for (var index = 0; index < UnusedEssentialResourcePaths.Length; index++)
            {
                var path = UnusedEssentialResourcePaths[index];
                if (AssetDatabase.LoadMainAssetAtPath(path) == null
                    && !AssetDatabase.IsValidFolder(path)) continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException(
                        "Unable to remove unused TMP resource: " + path);
            }
        }

        private static void EnsureOutputDirectory()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(OutputDirectory))
                AssetDatabase.CreateFolder("Assets/Resources", "CombatFeedback");
        }

        private static void EnsureTmpEssentialResources()
        {
            if (TMP_Settings.instance != null
                && Shader.Find(GenerationShaderName) != null) return;
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(TMP_FontAsset).Assembly);
            if (package == null || string.IsNullOrEmpty(package.resolvedPath))
                throw new InvalidOperationException(
                    "Unable to locate the resolved com.unity.ugui package.");
            var essentialPackage = Path.Combine(package.resolvedPath,
                "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(essentialPackage))
                throw new FileNotFoundException(
                    "TMP Essential Resources package is missing.", essentialPackage);
            AssetDatabase.ImportPackage(essentialPackage, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (TMP_Settings.instance == null
                && TMP_Settings.LoadDefaultSettings() == null)
                throw new InvalidOperationException(
                    "TMP Essential Resources did not provide TMP Settings.");
            if (Shader.Find(GenerationShaderName) == null)
                throw new InvalidOperationException(
                    "TMP Essential Resources did not provide the generation shader.");
        }

        private static float SmoothStep(float min, float max, float value)
        {
            var t = Mathf.Clamp01((value - min) / (max - min));
            return t * t * (3f - 2f * t);
        }

        private static int ColorDistance(Color32 left, Color32 right)
        {
            return Mathf.Abs(left.r - right.r)
                + Mathf.Abs(left.g - right.g)
                + Mathf.Abs(left.b - right.b);
        }

        private static string FormatCodePoints(string value)
        {
            if (string.IsNullOrEmpty(value)) return "none";
            return string.Join(",", value.Select(character =>
                "U+" + ((int)character).ToString("X4")));
        }

        private static void DestroyTransient(UnityEngine.Object value)
        {
            if (value == null || AssetDatabase.Contains(value)) return;
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
