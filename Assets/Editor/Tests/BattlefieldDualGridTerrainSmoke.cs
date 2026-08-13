using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

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
            ValidateDirectedEdges(palette);
            ValidateTileSetDimensionAndSocketContracts();
            ValidateStablePaletteOrder(palette);
            ValidateRuntimeTerrainFailurePolicy(palette);

            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            var expectedMapIds = new HashSet<string>(catalog.Maps.Keys,
                StringComparer.Ordinal);
            var bundledMapIds = new HashSet<string>(
                BundledLevelCatalogFactory.CreateBundledSource().Maps
                    .Select(map => map.MapId), StringComparer.Ordinal);
            Assert(expectedMapIds.SetEquals(bundledMapIds)
                && !catalog.Resolve(CanonicalBattlefieldMapAcceptance.LevelId).Succeeded,
                "production terrain catalog contains only bundled release maps");

            var validatedMapIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapId in expectedMapIds.OrderBy(value => value,
                         StringComparer.Ordinal))
            {
                var map = catalog.Maps[mapId];
                Assert(validatedMapIds.Add(map.MapId),
                    "each current catalog map participates in terrain validation once: "
                    + map.MapId);
                Assert(BattlefieldDualGridTerrain.Validate(map, palette, out terrainReason),
                    "map requires only exact contour palette bindings: " + terrainReason);
                ValidateMap(map, grassTileSet, routeTileSet, baseTexture);
            }
            Assert(validatedMapIds.SetEquals(expectedMapIds),
                "every unique current catalog map participates in terrain validation");

            var acceptanceAsset = AssetDatabase.LoadAssetAtPath<BattlefieldMapAuthoringAsset>(
                CanonicalBattlefieldMapAcceptance.MapAssetPath);
            CompiledBattlefieldMap acceptanceCompiled = null;
            BattlefieldLayeredMapValidationResult acceptanceValidation = null;
            Assert(acceptanceAsset != null
                && BattlefieldLayeredMapCompiler.TryCompile(acceptanceAsset.ToSource(),
                    out acceptanceCompiled, out acceptanceValidation),
                "isolated acceptance map compiles: "
                + (acceptanceValidation == null ? string.Empty : string.Join(" | ",
                    acceptanceValidation.Issues.Select(issue => issue.ToString()))));
            var acceptanceMap = new BattlefieldMapDefinition(acceptanceCompiled);
            Assert(acceptanceMap.MapId == CanonicalBattlefieldMapAcceptance.MapId,
                "isolated acceptance terrain keeps its authored map identity");
            Assert(BattlefieldDualGridTerrain.Validate(
                    acceptanceMap, palette, out terrainReason),
                "isolated acceptance map requires exact contour palette bindings: "
                + terrainReason);
            ValidateMap(acceptanceMap, grassTileSet, routeTileSet, baseTexture);

            ValidateSetupBinding(palette, grassTileSet, routeTileSet, baseTexture);
            ValidateReleaseSceneBinding(palette, grassTileSet, routeTileSet, baseTexture);
            Debug.Log("FRUIT_DEFENSE_BATTLEFIELD_DUAL_GRID_TERRAIN_OK productionMaps="
                + validatedMapIds.Count + " acceptanceFixtures=1");
        }

        private static void ValidateImportedArt(DualGridTileSet tileSet, string layerName)
        {
            var textures = new HashSet<Texture2D>();
            var nativeSize = Vector2.zero;
            for (var numericMask = 1; numericMask < DualGridMaskUtility.MaskCount; numericMask++)
            {
                Assert(tileSet.TryGetSprite((DualGridMask)numericMask, out var sprite),
                    layerName + " mask has a runtime Sprite: " + numericMask);
                if (nativeSize == Vector2.zero)
                    nativeSize = new Vector2(sprite.rect.width, sprite.rect.height);
                Assert(sprite.rect.width >= 32f && sprite.rect.height >= 32f
                    && Mathf.Approximately(sprite.rect.width, nativeSize.x)
                    && Mathf.Approximately(sprite.rect.height, nativeSize.y),
                    layerName + " masks keep one internally consistent native size: "
                    + numericMask);
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
            Assert(importer != null && importer.filterMode == FilterMode.Bilinear
                && !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Repeat,
                "battlefield terrain base texture preserves painterly sampling");
            Assert(baseTexture.width == 64 && baseTexture.height == 64,
                "battlefield terrain base texture matches the active Runtime64 tile contract");
        }

        private static void ValidateDirectedEdges(BattlefieldTerrainPalette palette)
        {
            Assert(palette.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined,
                    out var grassOnSoil, out var forwardComplemented)
                && !forwardComplemented,
                "square grass-on-soil refined edge is explicitly bound");
            Assert(palette.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined,
                    out var soilOnGrass, out var reverseComplemented)
                && ReferenceEquals(grassOnSoil, soilOnGrass)
                && reverseComplemented
                && grassOnSoil.TryGetSprite(DualGridMask.Empty, out _)
                && !palette.HasExactEdgeBinding(BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined),
                "one square edge binding resolves both directions and supplies the reverse center endpoint");
            Assert(!palette.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.StoneRoad,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined,
                    out _),
                "missing contour-specific material pairs do not cross-apply another resource");
            ValidateImportedArt(grassOnSoil, "square grass-on-soil edge");
        }

        private static void ValidateTileSetDimensionAndSocketContracts()
        {
            var owned = new List<UnityEngine.Object>();
            try
            {
                var organic = CreateMemoryTileSet("organic", 32, 32, 32f, owned);
                var square = CreateMemoryTileSet("square", 256, 256, 256f, owned);
                Assert(organic.HasCompatibleNormalizedSockets(square, out var reason), reason);

                var badSockets = CreateMemoryTileSet("bad-sockets", 64, 64, 32f, owned);
                Assert(!organic.HasCompatibleNormalizedSockets(badSockets, out reason)
                    && reason.Contains("normalized"),
                    "exact landform/edge pairs reject incompatible normalized sockets");

                var inconsistent = CreateMemoryTileSet("inconsistent", 32, 32, 32f, owned);
                inconsistent.SetTile((DualGridMask)2,
                    CreateMemoryTile("inconsistent-mask", 16, 32, 32f, owned));
                Assert(!inconsistent.Validate(out reason) && reason.Contains("native dimensions"),
                    "one TileSet rejects a non-full mask with inconsistent native dimensions");
            }
            finally
            {
                foreach (var value in owned.Where(value => value != null).Reverse())
                    UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private static void ValidateStablePaletteOrder(BattlefieldTerrainPalette palette)
        {
            var expectedLandforms = palette.LandformBindings.Select(binding =>
                binding.SurfaceId + "|" + binding.ContourStyleId).ToArray();
            var expectedEdges = palette.EdgeBindings.Select(binding =>
                binding.LandformSurfaceId + "|" + binding.BaseSurfaceId + "|"
                + binding.ContourStyleId + "|" + binding.EdgeStyleId).ToArray();
            Assert(expectedLandforms.SequenceEqual(expectedLandforms.OrderBy(value => value,
                    StringComparer.Ordinal))
                && expectedEdges.SequenceEqual(expectedEdges.OrderBy(value => value,
                    StringComparer.Ordinal)),
                "runtime palette traversal has explicit stable semantic-key order");

            var reversed = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
            try
            {
                reversed.ConfigureLayered(palette.PaletteId, palette.BaseBindings.Reverse(),
                    palette.LandformBindings.Reverse(), palette.EdgeBindings.Reverse());
                Assert(reversed.LandformBindings.Select(binding =>
                            binding.SurfaceId + "|" + binding.ContourStyleId)
                        .SequenceEqual(expectedLandforms)
                    && reversed.EdgeBindings.Select(binding =>
                            binding.LandformSurfaceId + "|" + binding.BaseSurfaceId + "|"
                            + binding.ContourStyleId + "|" + binding.EdgeStyleId)
                        .SequenceEqual(expectedEdges),
                    "serialized registration order cannot change landform or edge draw order");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reversed);
            }
        }

        private static void ValidateRuntimeTerrainFailurePolicy(
            BattlefieldTerrainPalette completePalette)
        {
            var missingSquareGrass = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
            var missingSquareEdge = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
            var edgeMap = CanonicalBattlefieldMapAuthoringSmoke.CreateValidMap(
                "map.smoke.runtime-edge-failure");
            var hostObject = new GameObject("TerrainPresentationFailureSmoke");
            var host = hostObject.AddComponent<FruitDefenseGame>();
            try
            {
                missingSquareGrass.ConfigureLayered(completePalette.PaletteId,
                    completePalette.BaseBindings,
                    completePalette.LandformBindings.Where(binding =>
                        !(binding.SurfaceId == BattlefieldLayerIds.Surfaces.Grass
                            && binding.ContourStyleId
                                == BattlefieldLayerIds.ContourStyles.Square)),
                    completePalette.EdgeBindings.Where(binding =>
                        !(binding.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass
                            && binding.ContourStyleId
                                == BattlefieldLayerIds.ContourStyles.Square)));
                Assert(missingSquareGrass.Validate(out var reason), reason);

                missingSquareEdge.ConfigureLayered(completePalette.PaletteId,
                    completePalette.BaseBindings, completePalette.LandformBindings,
                    completePalette.EdgeBindings.Where(binding =>
                        !(binding.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Grass
                            && binding.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                            && binding.ContourStyleId
                                == BattlefieldLayerIds.ContourStyles.Square
                            && binding.EdgeStyleId
                                == BattlefieldLayerIds.EdgeStyles.Refined)));
                Assert(missingSquareEdge.Validate(out reason), reason);
                Assert(edgeMap.TrySetVisual(new Vector2Int(0, 1),
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
                var edgeDefinition = new BattlefieldMapDefinition(
                    CanonicalBattlefieldMapAuthoringSmoke.Compile(edgeMap));
                Assert(!BattlefieldDualGridTerrain.Validate(edgeDefinition,
                        missingSquareEdge, out reason)
                    && reason.Contains(BattlefieldLayerIds.Surfaces.Grass)
                    && reason.Contains(BattlefieldLayerIds.Surfaces.Soil)
                    && reason.Contains(BattlefieldLayerIds.ContourStyles.Square)
                    && reason.Contains(BattlefieldLayerIds.EdgeStyles.Refined),
                    "runtime rejects a material pair when neither ordered binding exists");

                var resolved = BundledLevelCatalogFactory.CreateCompiled()
                    .Resolve(BundledLevelCatalogIds.Levels.Orchard01);
                Assert(resolved.Succeeded && resolved.Value != null,
                    "runtime terrain failure fixture resolves the bundled level");
                var navigator = new AppNavigator();
                Assert(navigator.TryBeginTransition(AppRoute.Battle, out reason)
                    && navigator.TryCompleteTransition(out reason), reason);
                var request = new BattleLaunchRequest("terrain-failure-smoke",
                    resolved.Value.Identity.LevelId, 7319,
                    resolved.Value.BattleContent.Header.contentVersion);
                host.ConfigureBattlefieldTerrain(new[] { missingSquareGrass });
                var initialization = host.Initialize(request, navigator,
                    new AcceptingResultSink(), resolved.Value);
                Assert(initialization.Success && host.IsInitialized && host.Simulation != null,
                    "missing terrain presentation does not destroy non-terrain gameplay initialization");
                Assert(!host.IsTerrainPresentationAvailable
                    && host.TerrainPresentationError.Contains(
                        BattlefieldLayerIds.Surfaces.Grass)
                    && host.TerrainPresentationError.Contains(
                        BattlefieldLayerIds.ContourStyles.Square),
                    "runtime stops terrain presentation with an explicit missing exact contour error");

                host.ConfigureBattlefieldTerrain(new[] { completePalette });
                Assert(host.IsTerrainPresentationAvailable
                    && host.ValidateActiveTerrainPresentation(out reason), reason);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
                UnityEngine.Object.DestroyImmediate(edgeMap);
                UnityEngine.Object.DestroyImmediate(missingSquareGrass);
                UnityEngine.Object.DestroyImmediate(missingSquareEdge);
            }
        }

        private static DualGridTileSet CreateMemoryTileSet(string label, int width, int height,
            float pixelsPerUnit, ICollection<UnityEngine.Object> owned)
        {
            var set = ScriptableObject.CreateInstance<DualGridTileSet>();
            set.name = label;
            owned.Add(set);
            var tile = CreateMemoryTile(label + "-tile", width, height, pixelsPerUnit, owned);
            for (var mask = 1; mask < DualGridMaskUtility.MaskCount; mask++)
                set.SetTile((DualGridMask)mask, tile);
            return set;
        }

        private static Tile CreateMemoryTile(string label, int width, int height,
            float pixelsPerUnit, ICollection<UnityEngine.Object> owned)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = label + "-texture",
            };
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(.5f, .5f), pixelsPerUnit);
            sprite.name = label + "-sprite";
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = label;
            tile.sprite = sprite;
            owned.Add(texture);
            owned.Add(sprite);
            owned.Add(tile);
            return tile;
        }

        private sealed class AcceptingResultSink : IBattleResultSink
        {
            public bool TrySubmitResult(BattleResult result, out string errorCode)
            {
                errorCode = string.Empty;
                return true;
            }
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
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square);
                var actualGrass = BattlefieldDualGridTerrain.ResolveLandformMask(map,
                    vertexX, vertexY, BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square);
                Assert(actualGrass == expectedGrass,
                    "grass mask matches plantable corners for " + map.MapId
                    + " at (" + vertexX + "," + vertexY + ")");
                var expectedRoute = ExpectedMask(map, vertexX, vertexY,
                    BattlefieldLayerIds.Surfaces.StoneRoad,
                    BattlefieldLayerIds.ContourStyles.Square);
                var actualRoute = BattlefieldDualGridTerrain.ResolveLandformMask(map,
                    vertexX, vertexY, BattlefieldLayerIds.Surfaces.StoneRoad,
                    BattlefieldLayerIds.ContourStyles.Square);
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
                && Mathf.Approximately(baseUv.width, map.GridWidth)
                && Mathf.Approximately(baseUv.height, map.GridHeight),
                "terrain base texture repeats continuously at one native tile per cell: " + map.MapId);
            var grassCellUv = BattlefieldDualGridTerrain.BaseCellUv(map, grassTileSet,
                baseTexture, 2, 3);
            var routeCellUv = BattlefieldDualGridTerrain.BaseCellUv(map, routeTileSet,
                baseTexture, 2, 3);
            Assert(grassCellUv == routeCellUv && Mathf.Approximately(grassCellUv.width, 1f)
                && Mathf.Approximately(grassCellUv.height, 1f),
                "base UV is independent from organic/square landform native sizes");

            var feedbackRect = FruitDefenseGame.BattlefieldFeedbackRect(
                projection.GridRect, projection.MapToScreen(map.Core));
            Assert(Contains(projection.GridRect, feedbackRect),
                "core feedback remains inside the projected grid: " + map.MapId);
        }

        private static DualGridMask ExpectedMask(
            BattlefieldMapDefinition map, int vertexX, int vertexY,
            string surfaceId, string contourStyleId)
        {
            var result = DualGridMask.Empty;
            Func<Vector2Int, bool> occupied = cell => string.Equals(
                    map.LandformSurfaceAt(cell), surfaceId, StringComparison.Ordinal)
                && string.Equals(map.ContourStyleAt(cell), contourStyleId,
                    StringComparison.Ordinal);
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
