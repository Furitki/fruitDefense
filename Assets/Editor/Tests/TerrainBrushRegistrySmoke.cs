using System;
using System.IO;
using System.Linq;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class TerrainBrushRegistrySmoke
    {
        public static void Validate()
        {
            var palette = LayeredTerrainArtSetup.RequirePaletteAssets();
            var definitions = TerrainBrushRegistry.FindAll();
            Assert(TerrainBrushRegistry.Validate(out var registryReason), registryReason);
            Assert(definitions.Count == 3,
                "expected two imported resources plus the preserved original resource");
            Assert(definitions.Select(value => value.BrushId).SequenceEqual(new[]
                {
                    "terrain-brush.grass-on-soil",
                    LayeredTerrainArtSetup.OriginalBrushId,
                    "terrain-brush.stone-on-water",
                }), "terrain brush registry has stable id order");
            Assert(CanonicalBattlefieldMapEditorWindow.RegisteredBrushes()
                    .SequenceEqual(definitions),
                "canonical map editor enumerates the shared terrain brush registry");
            Assert(LayeredTerrainSceneLaboratory.RegisteredBrushes()
                    .SequenceEqual(definitions),
                "terrain laboratory enumerates the shared terrain brush registry");
            var choices = TerrainBrushRegistry.FindPaintChoices();
            Assert(choices.Count == definitions.Count * 2
                && choices.Select(value => value.ChoiceId).SequenceEqual(
                    new[]
                    {
                        LayeredTerrainArtSetup.OriginalBrushId + ".forward",
                        LayeredTerrainArtSetup.OriginalBrushId + ".reverse",
                        "terrain-brush.grass-on-soil.forward",
                        "terrain-brush.grass-on-soil.reverse",
                        "terrain-brush.stone-on-water.forward",
                        "terrain-brush.stone-on-water.reverse",
                    }), "each registered resource expands to two stable paint choices with the preserved original first");
            Assert(LayeredTerrainSceneLaboratory.RegisteredPaintChoices()
                    .Select(value => value.ChoiceId)
                    .SequenceEqual(choices.Select(value => value.ChoiceId)),
                "terrain laboratory uses the shared directional choice registry");

            foreach (var definition in definitions)
            {
                Assert(definition.Validate(out var reason), reason);
                Assert(TerrainBrushRegistry.IsLaboratoryAvailable(definition, palette,
                    out reason), reason);
                Assert(TerrainBrushRegistry.IsDirectionAvailable(definition, palette,
                        false, out reason)
                    && TerrainBrushRegistry.IsDirectionAvailable(definition, palette,
                        true, out reason),
                    definition.BrushId + " provides both laboratory directions");
                ValidateComplementedView(definition);
                if (definition.PublishEndpointsToPalette)
                {
                    Assert(TerrainBrushRegistry.IsAvailable(definition, palette, out reason),
                        reason);
                    Assert(CanonicalBattlefieldMapEditorWindow.IsRegisteredBrushAvailable(
                        definition, palette),
                        definition.BrushId + " is selectable in map authoring");
                    ValidateFamily(definition);
                    ValidatePalette(definition, palette);
                }
                else
                {
                    Assert(!TerrainBrushRegistry.IsAvailable(definition, palette, out _)
                        && !CanonicalBattlefieldMapEditorWindow.IsRegisteredBrushAvailable(
                            definition, palette),
                        "the original laboratory visual does not replace production Palette authority");
                    ValidateOriginalFamily(definition);
                }
            }
            var stoneWater = definitions.Single(value => value.BrushId
                == "terrain-brush.stone-on-water");
            Assert(TerrainBrushRegistry.TryResolveLaboratoryLandforms(stoneWater, palette,
                    out _, out var waterLandform, out var fallbackReason)
                && waterLandform == stoneWater.ReverseLandformTileSet,
                "stone-water reverse uses its registered complemented view: " + fallbackReason);

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            foreach (var package in new[] { "GrassSoil", "StoneWater" })
            {
                var packageRoot = Path.Combine(projectRoot, "Assets", "LayeredTerrain",
                    "CompositeBrushes", package);
                Assert(TerrainBrushImportSetup.ResolveCandidateRoot(packageRoot) == packageRoot,
                    package + " resolves as a reusable one-click import package");
            }
            Debug.Log("FRUIT_DEFENSE_TERRAIN_BRUSH_REGISTRY_OK count=" + definitions.Count);
        }

        private static void ValidateComplementedView(TerrainBrushDefinition definition)
        {
            Assert(definition.ReverseLandformTileSet != null,
                definition.BrushId + " owns a complemented TileSet view");
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
                Assert(definition.ReverseLandformTileSet.GetTile((DualGridMask)mask)
                        == definition.CompositeTileSet.GetTile(
                            DualGridMaskUtility.Complement((DualGridMask)mask)),
                    definition.BrushId + " complemented mask " + mask + " is exact");
        }

        private static void ValidateOriginalFamily(TerrainBrushDefinition definition)
        {
            Assert(definition.BrushId == LayeredTerrainArtSetup.OriginalBrushId
                && definition.SourceManifest.text.Contains(
                    "\"schema\": \"fruit-defense.authored-terrain-source.v1\"")
                && definition.RuntimeTileSize == SquareTerrainArtProfile.TileSize
                && definition.ForegroundBaseTile
                    == RequireAsset<UnityEngine.Tilemaps.Tile>(
                        LayeredTerrainArtSetup.GrassBaseTilePath)
                && definition.BackgroundBaseTile
                    == RequireAsset<UnityEngine.Tilemaps.Tile>(
                        LayeredTerrainArtSetup.SoilBaseTilePath)
                && definition.CompositeTileSet == RequireAsset<DualGridTileSet>(
                    SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath),
                "original square resource retains the initial laboratory artwork");
        }

        private static void ValidateFamily(TerrainBrushDefinition definition)
        {
            var definitionPath = AssetDatabase.GetAssetPath(definition);
            var root = Path.GetDirectoryName(definitionPath).Replace('\\', '/');
            var descriptor = RequireAsset<TextAsset>(root + "/BrushImport.json").text;
            Assert(descriptor.Contains("\"schema\": \"fruit-defense.terrain-brush-import.v2\"")
                && descriptor.Contains("\"brushId\": \"" + definition.BrushId + "\"")
                && descriptor.Contains("\"profileId\": \""
                    + definition.SourceProfileId + "\"")
                && descriptor.Contains("\"runtimeTileSize\": 64")
                && descriptor.Contains("\"runtimeMaskDirectory\": \"Runtime64\""),
                definition.BrushId + " retains matching import metadata");
            var manifest = definition.SourceManifest.text;
            Assert(manifest.Contains("\"schema\": \"fruit-defense.dual-grid-art-pipeline.v3\"")
                && manifest.Contains("\"profileId\": \""
                    + definition.SourceProfileId + "\"")
                && manifest.Contains("\"runtimeMaskCount\": 16")
                && manifest.Contains("\"runtimeTileSize\": 64")
                && manifest.Contains("\"runtimeSamplingMethod\": \"lanczos\"")
                && manifest.Contains("\"visualInspectionPerformed\": false"),
                definition.BrushId + " retains explicit pipeline provenance");

            Assert(definition.RuntimeTileSize == 64,
                definition.BrushId + " registers the clear 64px runtime contract");

            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var path = root + "/Runtime64/Mask-" + mask.ToString("00") + ".png";
                var texture = RequireAsset<Texture2D>(path);
                Assert(texture.width == 64 && texture.height == 64,
                    definition.BrushId + " mask " + mask + " is 64x64");
                var sprite = RequireAsset<Sprite>(path);
                Assert(Mathf.Approximately(sprite.pixelsPerUnit, 64f)
                    && sprite.pivot == new Vector2(32f, 32f),
                    definition.BrushId + " mask " + mask + " has a centered cell socket");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var endpoint = mask == definition.ForegroundMask
                    || mask == definition.BackgroundMask;
                Assert(importer != null
                    && importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single
                    && importer.wrapMode == (endpoint
                        ? TextureWrapMode.Repeat : TextureWrapMode.Clamp)
                    && importer.filterMode == FilterMode.Bilinear
                    && importer.textureCompression == TextureImporterCompression.Uncompressed
                    && !importer.mipmapEnabled,
                    definition.BrushId + " mask " + mask + " import settings are deterministic");
            }
        }

        private static void ValidatePalette(TerrainBrushDefinition definition,
            BattlefieldTerrainPalette palette)
        {
            Assert(palette.TryGetBaseTexture(definition.BaseSurfaceId, out var background)
                    && background == definition.BackgroundTexture,
                definition.BrushId + " background endpoint is palette registered");
            Assert(palette.TryGetBaseTexture(definition.LandformSurfaceId, out var foreground)
                    && foreground == definition.ForegroundTexture,
                definition.BrushId + " foreground endpoint is palette registered");
            Assert(palette.TryGetEdgeTileSet(definition.BaseSurfaceId,
                    definition.LandformSurfaceId, definition.ContourStyleId,
                    definition.EdgeStyleId, out var reverse, out var complement)
                && reverse == definition.CompositeTileSet && complement,
                definition.BrushId + " reverse edge resolves through complemented masks");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required asset missing: " + path);
            return asset;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
