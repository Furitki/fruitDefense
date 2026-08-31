using System;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.Editor
{
    public static class FirstLevelSquareTerrainSmoke
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";
        private const string TrialRoot = "Assets/LayeredTerrain/Trials/";
        private const string ApprovedGrassPath =
            "Assets/LayeredTerrain/Trials/CellAlignedSquare/GrassSquareBase-v2.png";
        private const string ApprovedSoilPath =
            "Assets/LayeredTerrain/Trials/CellAlignedSquare/SoilSquareBase-v2.png";

        [MenuItem("Fruit Defense/Validation/Run First-Level Square Terrain Smoke")]
        public static void Run()
        {
            var defaultPalette = LayeredTerrainArtSetup.EnsurePaletteAssets();
            ValidateProductionTextures();
            ValidateFirstLevelPalette(defaultPalette);
            ValidateCatalogIsolation();
            ValidateReleaseScene();
            Debug.Log("FRUIT_DEFENSE_FIRST_LEVEL_SQUARE_TERRAIN_OK");
        }

        private static void ValidateProductionTextures()
        {
            Assert(File.ReadAllBytes(Path.GetFullPath(ApprovedGrassPath)).SequenceEqual(
                    File.ReadAllBytes(Path.GetFullPath(
                        LayeredTerrainArtSetup.FirstLevelSquareGrassPath))),
                "production grass is the byte-identical approved normalized export");
            Assert(File.ReadAllBytes(Path.GetFullPath(ApprovedSoilPath)).SequenceEqual(
                    File.ReadAllBytes(Path.GetFullPath(
                        LayeredTerrainArtSetup.FirstLevelSquareSoilPath))),
                "production soil is the byte-identical approved normalized export");

            foreach (var path in new[]
                     {
                         LayeredTerrainArtSetup.FirstLevelSquareGrassPath,
                         LayeredTerrainArtSetup.FirstLevelSquareSoilPath,
                     })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert(texture != null && texture.width == 64 && texture.height == 64,
                    "production square texture remains 64x64: " + path);
                Assert(importer != null
                    && importer.textureType == TextureImporterType.Sprite
                    && importer.spritePixelsPerUnit == 64f
                    && importer.wrapMode == TextureWrapMode.Repeat
                    && importer.filterMode == FilterMode.Bilinear
                    && !importer.mipmapEnabled
                    && importer.textureCompression == TextureImporterCompression.Uncompressed,
                    "production square texture has deterministic runtime import settings: " + path);
            }
        }

        private static void ValidateFirstLevelPalette(BattlefieldTerrainPalette defaultPalette)
        {
            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                LayeredTerrainArtSetup.FirstLevelSquarePalettePath);
            Assert(palette != null && palette.PaletteId
                    == BundledLevelCatalogIds.TerrainPalettes.Orchard01SquareGrid,
                "first-level production palette has the stable catalog identity");
            Assert(palette.Validate(out var reason),
                "first-level production palette validates: " + reason);
            Assert(palette.SurfaceBindings.Count == defaultPalette.SurfaceBindings.Count
                && palette.LandformBindings.Count == defaultPalette.LandformBindings.Count
                && palette.EdgeBindings.Count == defaultPalette.EdgeBindings.Count,
                "first-level palette retains the complete production binding set");
            Assert(palette.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Grass,
                    out var grass)
                && AssetDatabase.GetAssetPath(grass)
                    == LayeredTerrainArtSetup.FirstLevelSquareGrassPath,
                "first-level palette resolves production grass");
            Assert(palette.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil,
                    out var soil)
                && AssetDatabase.GetAssetPath(soil)
                    == LayeredTerrainArtSetup.FirstLevelSquareSoilPath,
                "first-level palette resolves production soil");
            Assert(AssetDatabase.GetDependencies(
                    LayeredTerrainArtSetup.FirstLevelSquarePalettePath, true)
                    .All(path => !path.StartsWith(TrialRoot, StringComparison.Ordinal)),
                "first-level release palette has no trial dependency");
        }

        private static void ValidateCatalogIsolation()
        {
            var source = BundledLevelCatalogFactory.CreateBundledSource();
            Assert(source.TerrainPaletteIds.SequenceEqual(new[]
                {
                    BundledLevelCatalogIds.TerrainPalettes.Orchard01SquareGrid,
                    BundledLevelCatalogIds.TerrainPalettes.OrchardDefault,
                }), "bundled catalog declares both production palette identities exactly once");
            Assert(source.Themes.Single(theme => theme.ThemeId
                        == BundledLevelCatalogIds.Themes.DayOrchard).TerrainPaletteId
                    == BundledLevelCatalogIds.TerrainPalettes.Orchard01SquareGrid,
                "only the first-level day theme selects the square palette");
            Assert(source.Themes.Where(theme => theme.ThemeId
                        != BundledLevelCatalogIds.Themes.DayOrchard)
                    .All(theme => theme.TerrainPaletteId
                        == BundledLevelCatalogIds.TerrainPalettes.OrchardDefault),
                "later bundled themes retain the default palette");

            var first = source.Maps.Single(map => map.MapId
                == BundledLevelCatalogIds.Maps.Orchard01);
            var grassCount = first.VisualCells.Count(cell => cell.BaseSurfaceId
                    == BattlefieldLayerIds.Surfaces.Grass
                && string.IsNullOrEmpty(cell.LandformSurfaceId)
                && string.IsNullOrEmpty(cell.ContourStyleId)
                && string.IsNullOrEmpty(cell.EdgeStyleId));
            var soilCount = first.VisualCells.Count(cell => cell.BaseSurfaceId
                    == BattlefieldLayerIds.Surfaces.Soil
                && string.IsNullOrEmpty(cell.LandformSurfaceId)
                && string.IsNullOrEmpty(cell.ContourStyleId)
                && string.IsNullOrEmpty(cell.EdgeStyleId));
            Assert(first.GridWidth == 8 && first.GridHeight == 7
                && grassCount == 35 && soilCount == 21,
                "orchard-01 is exactly the approved 35-grass/21-soil base-only grid");

            var layeredVariant = new BattlefieldMapDefinition(
                BattlefieldLayeredMapFactory.CreateSingleRouteMap(first.MapId,
                    first.GridWidth, first.GridHeight, first.MapUnitsPerCell,
                    first.RouteCells, first.CoreCell,
                    first.InitialPotGroupOrder.Select(id => first.InitialPotGroups[id]),
                    BattlefieldPlantableVisualStyle.LayeredSquareGrassOnSoil));
            Assert(first.GameplayFingerprint == layeredVariant.GameplayFingerprint
                && first.RouteCells.SequenceEqual(layeredVariant.RouteCells)
                && first.PlantableCells.SequenceEqual(layeredVariant.PlantableCells),
                "first-level visual selection does not alter gameplay topology or fingerprint");
            Assert(source.Maps.Where(map => map.MapId != first.MapId)
                    .All(map => map.VisualCells.Any(cell =>
                        !string.IsNullOrEmpty(cell.LandformSurfaceId))),
                "later bundled maps retain layered terrain composition");
        }

        private static void ValidateReleaseScene()
        {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            var canRestoreSetup = previousSetup.Any(setup => setup.isLoaded && setup.isActive);
            var battle = default(Scene);
            try
            {
                battle = SceneManager.GetSceneByPath(BattleScenePath);
                if (!battle.IsValid() || !battle.isLoaded)
                    battle = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
                var game = battle.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FruitDefenseGame>(true))
                    .Single();
                var valid = game.ValidateBattlefieldTerrain(out var reason);
                Assert(game.BattlefieldTerrainPalettes.Select(palette => palette.PaletteId)
                        .SequenceEqual(new[]
                        {
                            BundledLevelCatalogIds.TerrainPalettes.OrchardDefault,
                            BundledLevelCatalogIds.TerrainPalettes.Orchard01SquareGrid,
                        }) && valid,
                    "release Battle registers both valid production palettes: " + reason);
                Assert(AssetDatabase.GetDependencies(BattleScenePath, true)
                        .All(path => !path.StartsWith(TrialRoot, StringComparison.Ordinal)),
                    "release Battle scene has no trial dependency");
            }
            finally
            {
                if (canRestoreSetup)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else if (battle.IsValid() && battle.isLoaded)
                    EditorSceneManager.CloseScene(battle, true);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "First-level square terrain smoke failed: " + message);
        }
    }
}
