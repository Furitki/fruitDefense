using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace FruitDefense.Editor
{
    public static class CombatFeedbackSdfAtlasSmoke
    {
        public static void Run()
        {
            ValidateGeneratorContractAndDeterminism();
            ValidateStaticFiniteCoverage();
            ValidateNoRuntimeTmpAssetsOrMaterials();
            Debug.Log("FRUIT_DEFENSE_COMBAT_FEEDBACK_SDF_ATLAS_OK");
        }

        private static void ValidateGeneratorContractAndDeterminism()
        {
            Assert(CombatFloatingTextSdfGenerator.AtlasSize == 512
                && CombatFloatingTextSdfGenerator.SamplingPointSize == 64
                && CombatFloatingTextSdfGenerator.AtlasPadding == 8
                && CombatFloatingTextSdfGenerator.BakedGlyphPadding == 8
                && CombatFloatingTextSdfGenerator.CompositeRegionX == 192
                && CombatFloatingTextSdfGenerator.CompositeTokenPointSize == 24
                && CombatFloatingTextSdfGenerator.CompositePackGap == 1
                && Mathf.Approximately(
                    CombatFloatingTextSdfGenerator.FaceThreshold, .43f)
                && Mathf.Approximately(
                    CombatFloatingTextSdfGenerator.FaceTransition, .04f)
                && Mathf.Approximately(
                    CombatFloatingTextSdfGenerator.OutlineThreshold, .10f)
                && Mathf.Approximately(
                    CombatFloatingTextSdfGenerator.OutlineTransition, .05f)
                && CombatFloatingTextSdfGenerator.RenderMode == GlyphRenderMode.SDF32,
                "generator owns the 8-pixel baked field, solid face, thick outline, 24px composite shelf, and final 512 atlas settings");
            CombatFloatingTextSdfGenerator.Rebuild();
            Assert(CombatFloatingTextSdfGenerator.ValidateGeneratedAssets().Count == 0,
                "the first deterministic baked-atlas rebuild validates");
            var first = Fingerprint();
            CombatFloatingTextSdfGenerator.Rebuild();
            var issues = CombatFloatingTextSdfGenerator.ValidateGeneratedAssets();
            Assert(issues.Count == 0,
                "the second deterministic rebuild validates: "
                + string.Join(", ", issues));
            Assert(string.Equals(first, Fingerprint(), StringComparison.Ordinal),
                "two unchanged generator runs preserve atlas pixels, glyph metrics, and GUIDs");
        }

        private static void ValidateStaticFiniteCoverage()
        {
            var inventory = CombatFloatingTextStyleCatalog.RuntimeGlyphInventory;
            Assert(!string.IsNullOrEmpty(inventory)
                && inventory.Distinct().Count() == inventory.Length
                && string.Equals(inventory, "-+0123456789 阳光冻结击败×",
                    StringComparison.Ordinal),
                "combat copy has one reviewed finite duplicate-free inventory");
            var source = AssetDatabase.LoadAssetAtPath<Font>(
                CombatFloatingTextSdfGenerator.SourceFontPath);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
                CombatFloatingTextSdfGenerator.AtlasAssetPath);
            var metadata = AssetDatabase.LoadAssetAtPath<CombatFloatingTextAtlasMetadata>(
                CombatFloatingTextSdfGenerator.MetadataAssetPath);
            Assert(source != null && atlas != null && metadata != null,
                "source font, one RGBA atlas, and static metadata exist");
            Assert(inventory.All(source.HasCharacter)
                && metadata.GlyphInventory == inventory
                && metadata.GlyphCount == inventory.Length,
                "source and metadata cover exactly the reviewed inventory");
            foreach (var character in inventory)
            {
                CombatFloatingTextGlyph glyph;
                Assert(metadata.TryGetGlyph(character, out glyph)
                    && glyph.AtlasRect.xMin >= 0 && glyph.AtlasRect.yMin >= 0
                    && glyph.AtlasRect.xMax <= 512 && glyph.AtlasRect.yMax <= 512
                    && glyph.Scale > 0f
                    && Mathf.Approximately(glyph.Padding, 8f),
                    "static glyph metrics and UV bounds are valid for U+"
                    + ((int)character).ToString("X4"));
            }
            ValidateCompositeTokens(atlas, metadata);
            var missing = RuntimeUiChineseGlyphCoverage.RequiredGlyphs
                .First(value => inventory.IndexOf(value) < 0);
            CombatFloatingTextGlyph ignored;
            Assert(!metadata.TryGetGlyph(missing, out ignored),
                "out-of-inventory glyph remains absent without runtime growth");
            Assert(atlas.width == 512 && atlas.height == 512
                && atlas.format == TextureFormat.RGBA32
                && atlas.mipmapCount == 1
                && atlas.filterMode == FilterMode.Bilinear
                && atlas.wrapMode == TextureWrapMode.Clamp,
                "the committed production atlas is one static RGBA32 page");
            var pixels = atlas.GetPixels32();
            var transparent = pixels.Count(pixel => pixel.a == 0);
            var face = pixels.Count(pixel => pixel.a > 128
                && pixel.r > 220 && pixel.g > 220 && pixel.b > 220);
            var outline = pixels.Count(pixel => pixel.a > 32
                && pixel.r < 180 && pixel.g < 180 && pixel.b < 180);
            Assert(transparent > 0
                && face >= CombatFloatingTextSdfGenerator.MinimumBakedFacePixels
                && outline >= face
                    * CombatFloatingTextSdfGenerator.MinimumOutlineToFaceRatio,
                "atlas bakes a solid white tintable face and a quantitatively thick continuous dark outline");
        }

        private static void ValidateCompositeTokens(Texture2D atlas,
            CombatFloatingTextAtlasMetadata metadata)
        {
            var expected = BuildExpectedCompositeTokens();
            var region = new RectInt(192, 0, 320, 512);
            Assert(metadata.CompositeRegion == region
                && Mathf.Approximately(metadata.CompositeBasePointSize, 24f)
                && metadata.CompositeTokenCount == expected.Count
                && expected.Count == 124,
                "metadata owns all 114 required and 10 capacity-admitted optional tokens in the reviewed right-side region");
            var occupied = new bool[region.width * region.height];
            var pixels = atlas.GetPixels32();
            foreach (var text in expected)
            {
                CombatFloatingTextCompositeToken token;
                Assert(metadata.TryGetCompositeToken(text, out token)
                    && token.Text == text
                    && Mathf.Approximately(token.BaseScale, 24f / 64f)
                    && token.MaxX > token.MinX && token.MaxY > token.MinY
                    && token.HorizontalAdvance > 0f
                    && token.AtlasRect.xMin >= region.xMin
                    && token.AtlasRect.yMin >= region.yMin
                    && token.AtlasRect.xMax <= region.xMax
                    && token.AtlasRect.yMax <= region.yMax,
                    "composite token metrics are finite and packed for " + text);
                var visible = 0;
                var overlap = false;
                for (var y = token.AtlasRect.yMin; y < token.AtlasRect.yMax; y++)
                for (var x = token.AtlasRect.xMin; x < token.AtlasRect.xMax; x++)
                {
                    var index = (y - region.y) * region.width + x - region.x;
                    if (occupied[index]) overlap = true;
                    occupied[index] = true;
                    if (pixels[y * atlas.width + x].a > 0) visible++;
                }
                Assert(!overlap && visible > 0,
                    "composite token shelf rectangles do not overlap and contain raster pixels for "
                    + text);
            }
            for (var y = region.yMin; y < region.yMax; y++)
            for (var x = region.xMin; x < region.xMax; x++)
            {
                var index = (y - region.y) * region.width + x - region.x;
                Assert(occupied[index] || pixels[y * atlas.width + x].a == 0,
                    "unused composite shelf pixels remain transparent at " + x + "," + y);
            }
            var generatorSource = System.IO.File.ReadAllText(
                System.IO.Path.Combine(Application.dataPath,
                    "Editor/Tools/CombatFloatingTextSdfGenerator.cs"));
            Assert(generatorSource.Contains(
                    "EnsureCompositeRegionIsTransparent(atlasPixels);",
                    StringComparison.Ordinal)
                && generatorSource.IndexOf(
                    "EnsureCompositeRegionIsTransparent(atlasPixels);",
                    StringComparison.Ordinal)
                    < generatorSource.IndexOf("atlas.SetPixels32(atlasPixels);",
                        StringComparison.Ordinal),
                "generator hard-checks the complete target region before any token raster is written");
        }

        private static List<string> BuildExpectedCompositeTokens()
        {
            var result = new List<string>(124);
            for (var digit = 0; digit <= 9; digit++)
                result.Add("-" + digit.ToString(CultureInfo.InvariantCulture));
            for (var value = 0; value <= 99; value++)
                result.Add("-" + value.ToString("00", CultureInfo.InvariantCulture));
            result.Add("冻结");
            result.Add("击败");
            result.Add("击败×");
            result.Add(" 阳光");
            for (var digit = 0; digit <= 9; digit++)
                result.Add("+" + digit.ToString(CultureInfo.InvariantCulture));
            return result;
        }

        private static void ValidateNoRuntimeTmpAssetsOrMaterials()
        {
            var materials = AssetDatabase.FindAssets("t:Material",
                new[] { CombatFloatingTextSdfGenerator.OutputDirectory });
            var tmpFonts = AssetDatabase.FindAssets("t:TMP_FontAsset",
                new[] { CombatFloatingTextSdfGenerator.OutputDirectory });
            Assert(materials.Length == 0 && tmpFonts.Length == 0,
                "production combat feedback owns no TMP font or material assets");
            foreach (var path in new[]
            {
                CombatFloatingTextSdfGenerator.OutputDirectory + "/CombatFloatingTextSdf.asset",
                CombatFloatingTextSdfGenerator.OutputDirectory + "/CombatFloatingTextSdfAtlas.asset",
                CombatFloatingTextSdfGenerator.OutputDirectory + "/CombatFloatingTextStandard.mat",
                CombatFloatingTextSdfGenerator.OutputDirectory + "/CombatFloatingTextHeavy.mat",
            })
                Assert(AssetDatabase.LoadMainAssetAtPath(path) == null,
                    "legacy production asset is deleted: " + path);
        }

        private static string Fingerprint()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
                CombatFloatingTextSdfGenerator.AtlasAssetPath);
            var metadata = AssetDatabase.LoadAssetAtPath<CombatFloatingTextAtlasMetadata>(
                CombatFloatingTextSdfGenerator.MetadataAssetPath);
            Assert(atlas != null && metadata != null,
                "generated assets exist before fingerprinting");
            var builder = new StringBuilder();
            builder.Append(AssetDatabase.AssetPathToGUID(
                CombatFloatingTextSdfGenerator.AtlasAssetPath)).Append('|');
            builder.Append(AssetDatabase.AssetPathToGUID(
                CombatFloatingTextSdfGenerator.MetadataAssetPath)).Append('|');
            foreach (var character in metadata.GlyphInventory)
            {
                CombatFloatingTextGlyph glyph;
                Assert(metadata.TryGetGlyph(character, out glyph),
                    "fingerprint glyph exists");
                builder.Append(glyph.CodePoint).Append(':')
                    .Append(glyph.AtlasRect).Append(':')
                    .Append(glyph.Width).Append(':')
                    .Append(glyph.Height).Append(':')
                    .Append(glyph.HorizontalAdvance).Append(';');
            }
            foreach (var tokenText in BuildExpectedCompositeTokens())
            {
                CombatFloatingTextCompositeToken token;
                Assert(metadata.TryGetCompositeToken(tokenText, out token),
                    "fingerprint composite token exists: " + tokenText);
                builder.Append(token.Text).Append(':')
                    .Append(token.AtlasRect).Append(':')
                    .Append(token.BaseScale).Append(':')
                    .Append(token.MinX).Append(':').Append(token.MaxX).Append(':')
                    .Append(token.MinY).Append(':').Append(token.MaxY).Append(':')
                    .Append(token.HorizontalAdvance).Append(';');
            }
            using (var sha = SHA256.Create())
                builder.Append(string.Concat(sha.ComputeHash(
                        atlas.GetRawTextureData<byte>().ToArray())
                    .Select(value => value.ToString("x2"))));
            return builder.ToString();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Combat feedback SDF atlas smoke failed: " + message);
        }
    }
}
