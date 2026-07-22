using System;
using System.Collections.Generic;
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
    public static class BattlefieldDualGridTerrainSmoke
    {
        private const string BattleScenePath = "Assets/Scenes/Battle.unity";
        private static readonly Rect ReferenceBoardRect = new Rect(0f, 72f, 402f, 500f);

        public static void Validate()
        {
            var grassTileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(
                ProjectSetup.BattlefieldGrassTileSetPath);
            var routeTileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(
                ProjectSetup.BattlefieldRouteTileSetPath);
            var baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                ProjectSetup.BattlefieldTerrainBaseTexturePath);
            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ProjectSetup.BattlefieldTerrainPalettePath);
            var terrainValid = BattlefieldDualGridTerrain.Validate(palette, out var terrainReason);
            Assert(terrainValid,
                "battlefield terrain assets validate: " + terrainReason);
            ValidateImportedArt(grassTileSet, "grass");
            ValidateImportedArt(routeTileSet, "route");
            ValidateBaseTexture(baseTexture);

            var maps = BundledLevelCatalogFactory.CreateSource().Maps;
            Assert(maps.Count == 3, "all three bundled maps participate in terrain validation");
            foreach (var map in maps)
                ValidateMap(map, grassTileSet, routeTileSet, baseTexture);

            ValidateSetupBinding(palette, grassTileSet, routeTileSet, baseTexture);
            ValidateReleaseSceneBinding(palette, grassTileSet, routeTileSet, baseTexture);
            Debug.Log("FRUIT_DEFENSE_BATTLEFIELD_DUAL_GRID_TERRAIN_OK maps=" + maps.Count);
        }

        private static void ValidateImportedArt(DualGridTileSet tileSet, string layerName)
        {
            var textures = new HashSet<Texture2D>();
            for (var numericMask = 1; numericMask < DualGridMaskUtility.MaskCount; numericMask++)
            {
                Assert(tileSet.TryGetSprite((DualGridMask)numericMask, out var sprite),
                    layerName + " mask has a runtime Sprite: " + numericMask);
                Assert(Mathf.Approximately(sprite.rect.width, 32f)
                    && Mathf.Approximately(sprite.rect.height, 32f),
                    layerName + " mask keeps its 32x32 native size: " + numericMask);
                Assert(sprite.texture.filterMode == FilterMode.Point,
                    layerName + " mask keeps point filtering: " + numericMask);
                var uv = BattlefieldDualGridTerrain.SpriteUv(sprite);
                Assert(uv.width > 0f && uv.height > 0f
                    && uv.xMin >= 0f && uv.yMin >= 0f
                    && uv.xMax <= 1f && uv.yMax <= 1f,
                    layerName + " mask UV is normalized: " + numericMask);
                textures.Add(sprite.texture);
            }
            Assert(textures.Count == 15,
                layerName + " keeps one generated runtime texture for each required mask");
        }

        private static void ValidateBaseTexture(Texture2D baseTexture)
        {
            var importer = AssetImporter.GetAtPath(
                ProjectSetup.BattlefieldTerrainBaseTexturePath) as TextureImporter;
            Assert(importer != null && importer.filterMode == FilterMode.Point
                && !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Repeat,
                "battlefield terrain base texture preserves pixel-art sampling");
            Assert(baseTexture.width > 32 && baseTexture.height > 32,
                "battlefield terrain base texture provides enough source area for scaled tiling");
        }

        private static void ValidateMap(BattlefieldMapDefinition map,
            DualGridTileSet grassTileSet, DualGridTileSet routeTileSet, Texture2D baseTexture)
        {
            Assert(map != null, "terrain map is present");
            Assert(map.UsesLayeredMap, "terrain map uses layered semantic surfaces");
            Assert(map.Validate(out var mapReason),
                "terrain map topology validates: " + mapReason);
            Assert(BattlefieldDualGridTerrain.VisualTileCount(map)
                == (map.GridWidth + 1) * (map.GridHeight + 1),
                "terrain visual count covers every vertex: " + map.MapId);

            var projection = new BattlefieldProjection(map, ReferenceBoardRect);
            var grassMasks = new HashSet<DualGridMask>();
            var routeMasks = new HashSet<DualGridMask>();
            var grassFullCount = 0;
            var grassTransitionCount = 0;
            var routeNonEmptyCount = 0;
            var routeTransitionCount = 0;
            for (var vertexY = 0; vertexY <= map.GridHeight; vertexY++)
            for (var vertexX = 0; vertexX <= map.GridWidth; vertexX++)
            {
                var expectedGrass = ExpectedMask(map, vertexX, vertexY,
                    BattlefieldLayerIds.Surfaces.Grass);
                var actualGrass = BattlefieldDualGridTerrain.ResolveMask(map, vertexX, vertexY,
                    BattlefieldLayerIds.Surfaces.Grass);
                Assert(actualGrass == expectedGrass,
                    "grass mask matches plantable corners for " + map.MapId
                    + " at (" + vertexX + "," + vertexY + ")");
                var expectedRoute = ExpectedMask(map, vertexX, vertexY,
                    BattlefieldLayerIds.Surfaces.StoneRoad);
                var actualRoute = BattlefieldDualGridTerrain.ResolveMask(map, vertexX, vertexY,
                    BattlefieldLayerIds.Surfaces.StoneRoad);
                Assert(actualRoute == expectedRoute,
                    "route mask matches monster-route corners for " + map.MapId
                    + " at (" + vertexX + "," + vertexY + ")");
                Assert(((int)actualGrass & (int)actualRoute) == 0,
                    "grass and route do not occupy the same logical corner for " + map.MapId
                    + " at (" + vertexX + "," + vertexY + ")");

                grassMasks.Add(actualGrass);
                routeMasks.Add(actualRoute);
                if (actualGrass == DualGridMask.Full) grassFullCount++;
                else if (actualGrass != DualGridMask.Empty) grassTransitionCount++;
                if (actualRoute != DualGridMask.Empty) routeNonEmptyCount++;
                if (actualRoute != DualGridMask.Empty && actualRoute != DualGridMask.Full)
                    routeTransitionCount++;

                var rect = BattlefieldDualGridTerrain.VisualTileRect(
                    projection, vertexX, vertexY);
                var expectedCenter = new Vector2(
                    projection.GridRect.xMin + vertexX * projection.TileSize,
                    projection.GridRect.yMin + vertexY * projection.TileSize);
                Assert(Mathf.Abs(rect.width - projection.TileSize) < .001f
                    && Mathf.Abs(rect.height - projection.TileSize) < .001f
                    && Vector2.Distance(rect.center, expectedCenter) < .001f,
                    "terrain tile is square and vertex-aligned for " + map.MapId);
                var clipped = Intersection(rect, projection.GridRect);
                Assert(clipped.width >= 0f && clipped.height >= 0f
                    && Contains(projection.GridRect, clipped),
                    "terrain tile clips inside GridRect for " + map.MapId);
            }

            Assert(grassFullCount > 0 && grassTransitionCount > 0 && grassMasks.Count > 1,
                "terrain map exercises full grass and transition masks: " + map.MapId);
            Assert(routeNonEmptyCount > 0 && routeTransitionCount > 0 && routeMasks.Count > 1,
                "terrain map exercises stone-route transition masks: " + map.MapId);
            var first = BattlefieldDualGridTerrain.VisualTileRect(projection, 0, 0);
            var last = BattlefieldDualGridTerrain.VisualTileRect(
                projection, map.GridWidth, map.GridHeight);
            Assert(Mathf.Abs(first.xMin - (projection.GridRect.xMin - projection.TileSize * .5f)) < .001f
                && Mathf.Abs(first.yMin - (projection.GridRect.yMin - projection.TileSize * .5f)) < .001f
                && Mathf.Abs(last.xMax - (projection.GridRect.xMax + projection.TileSize * .5f)) < .001f
                && Mathf.Abs(last.yMax - (projection.GridRect.yMax + projection.TileSize * .5f)) < .001f,
                "terrain layer uses the authored negative half-cell alignment: " + map.MapId);

            var baseUv = BattlefieldDualGridTerrain.BaseTextureUv(
                map, grassTileSet, baseTexture);
            Assert(baseUv.x == 0f && baseUv.y == 0f
                && baseUv.width > 0f && baseUv.height > 0f
                && baseUv.width <= 1f && baseUv.height <= 1f,
                "terrain base texture samples at native pixels per tile: " + map.MapId);

            var feedbackRect = FruitDefenseGame.BattlefieldFeedbackRect(
                projection.GridRect, projection.MapToScreen(map.Core));
            Assert(Contains(projection.GridRect, feedbackRect),
                "core feedback remains inside the projected grid: " + map.MapId);
        }

        private static DualGridMask ExpectedMask(
            BattlefieldMapDefinition map, int vertexX, int vertexY,
            string surfaceId)
        {
            var result = DualGridMask.Empty;
            Func<Vector2Int, bool> occupied = cell => string.Equals(
                map.SurfaceAt(cell), surfaceId, StringComparison.Ordinal);
            if (occupied(new Vector2Int(vertexX - 1, vertexY - 1)))
                result |= DualGridMask.NorthWest;
            if (occupied(new Vector2Int(vertexX, vertexY - 1)))
                result |= DualGridMask.NorthEast;
            if (occupied(new Vector2Int(vertexX, vertexY)))
                result |= DualGridMask.SouthEast;
            if (occupied(new Vector2Int(vertexX - 1, vertexY)))
                result |= DualGridMask.SouthWest;
            return result;
        }

        private static void ValidateSetupBinding(BattlefieldTerrainPalette palette,
            DualGridTileSet grassTileSet,
            DualGridTileSet routeTileSet, Texture2D baseTexture)
        {
            var root = new GameObject("BattlefieldTerrainSetupSmoke");
            try
            {
                var game = root.AddComponent<FruitDefenseGame>();
                ProjectSetup.ConfigureBattlefieldTerrain(game);
                var valid = game.ValidateBattlefieldTerrain(out var reason);
                Assert(game.BattlefieldTerrainPalettes.Count == 1
                    && game.BattlefieldTerrainPalettes[0] == palette
                    && game.BattlefieldGrassTileSet == grassTileSet
                    && game.BattlefieldRouteTileSet == routeTileSet
                    && game.BattlefieldSoilBaseTexture == baseTexture
                    && valid,
                    "project setup reproduces terrain binding: " + reason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ValidateReleaseSceneBinding(
            BattlefieldTerrainPalette palette, DualGridTileSet grassTileSet,
            DualGridTileSet routeTileSet, Texture2D baseTexture)
        {
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            var canRestoreSetup = previousSetup.Any(setup => setup.isLoaded && setup.isActive);
            var battle = default(Scene);
            try
            {
                battle = SceneManager.GetSceneByPath(BattleScenePath);
                if (!battle.IsValid() || !battle.isLoaded)
                    battle = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
                var games = battle.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FruitDefenseGame>(true))
                    .ToArray();
                Assert(games.Length == 1,
                    "release Battle scene contains exactly one FruitDefenseGame");
                var game = games[0];
                var valid = game.ValidateBattlefieldTerrain(out var reason);
                Assert(game.BattlefieldTerrainPalettes.Count == 1
                    && game.BattlefieldTerrainPalettes[0] == palette
                    && game.BattlefieldGrassTileSet == grassTileSet
                    && game.BattlefieldRouteTileSet == routeTileSet
                    && game.BattlefieldSoilBaseTexture == baseTexture
                    && valid,
                    "release Battle scene binds valid terrain assets: " + reason);
            }
            finally
            {
                if (canRestoreSetup)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else if (battle.IsValid() && battle.isLoaded)
                    EditorSceneManager.CloseScene(battle, true);
            }
        }

        private static Rect Intersection(Rect first, Rect second)
        {
            var xMin = Mathf.Max(first.xMin, second.xMin);
            var yMin = Mathf.Max(first.yMin, second.yMin);
            var xMax = Mathf.Min(first.xMax, second.xMax);
            var yMax = Mathf.Min(first.yMax, second.yMax);
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - .001f && inner.yMin >= outer.yMin - .001f
                && inner.xMax <= outer.xMax + .001f && inner.yMax <= outer.yMax + .001f;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Battlefield Dual-Grid smoke failed: " + message);
        }
    }
}
