using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class LayeredTerrainArtSetup
    {
        internal const string Root = "Assets/LayeredTerrain/GrassSoil";
        internal const string GrassBasePath = Root + "/Base/Grass.png";
        internal const string SoilBasePath = Root + "/Base/Soil.png";
        internal const string GrassLandformTileSetPath = Root + "/LandformGrass/GrassLandformTileSet.asset";
        internal const string SoilLandformTileSetPath = Root + "/LandformSoil/SoilLandformTileSet.asset";
        internal const string OrganicStoneRoadTileSetPath =
            "Assets/DualGridTerrain/StoneFloor/Generated/StoneFloorDualGridTileSet.asset";
        internal const string GrassOnSoilEdgeTileSetPath =
            Root + "/EdgeGrassOnSoilRefined/GrassOnSoilRefinedTileSet.asset";
        internal const string FirstLevelSquareRoot =
            "Assets/Battlefield/Terrain/Orchard01SquareGrid";
        internal const string FirstLevelSquareGrassPath =
            FirstLevelSquareRoot + "/GrassSquareBase.png";
        internal const string FirstLevelSquareSoilPath =
            FirstLevelSquareRoot + "/SoilSquareBase.png";
        internal const string FirstLevelSquarePalettePath =
            FirstLevelSquareRoot + "/Orchard01SquareTerrainPalette.asset";
        internal const string SoilOnGrassEdgeTileSetPath =
            Root + "/EdgeSoilOnGrassRefined/SoilOnGrassRefinedTileSet.asset";
        internal const string AcceptanceScenePath = "Assets/Scenes/LayeredTerrainDemo.unity";
        internal const string AcceptanceBuildPath = "Builds/LayeredTerrainWebGL";
        internal const string AcceptanceEvidencePath =
            "Builds/Evidence/layered-terrain/unity-layered-terrain-demo.png";
        internal const string OriginalBrushId = "terrain-brush.original-square-grass-on-soil";
        internal const string OriginalBrushRoot =
            TerrainBrushImportSetup.AssetRoot + "/OriginalGrassSoil";
        internal const string OriginalBrushDefinitionPath =
            OriginalBrushRoot + "/OriginalGrassSoilBrush.asset";
        internal const string OriginalBrushSourceRecordPath =
            OriginalBrushRoot + "/SourceManifest.json";

        private const int NativeTileSize = 32;
        private const int FirstLevelSquareTileSize = 64;
        private const string AuthoringFolder = Root + "/Authoring";
        internal const string GrassBaseTilePath = AuthoringFolder + "/GrassBase.asset";
        internal const string SoilBaseTilePath = AuthoringFolder + "/SoilBase.asset";
        private const string MarkerAPath = AuthoringFolder + "/MarkerA.asset";
        private const string MarkerBPath = AuthoringFolder + "/MarkerB.asset";
        private const string EdgeMarkerPath = AuthoringFolder + "/EdgeEnabled.asset";

        [MenuItem("Fruit Defense/地图工具/地貌素材实验室/导入素材并创建诊断场景")]
        public static void ImportArtAndCreateDemo()
        {
            EnsurePaletteAssets();
            CreateAcceptanceScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Terrain-material laboratory art, palette and diagnostic scene are ready; this is not playable-map readiness.");
        }

        internal static BattlefieldTerrainPalette EnsurePaletteAssets()
        {
            AssetDatabase.Refresh();
            SquareTerrainArtGenerator.GenerateAvailableSquareAssets();
            ConfigureTexture(GrassBasePath, true, NativeTileSize);
            ConfigureTexture(SoilBasePath, true, NativeTileSize);
            ConfigureTexture(FirstLevelSquareGrassPath, true, FirstLevelSquareTileSize);
            ConfigureTexture(FirstLevelSquareSoilPath, true, FirstLevelSquareTileSize);
            BuildBaseTile(GrassBasePath, GrassBaseTilePath);
            BuildBaseTile(SoilBasePath, SoilBaseTilePath);
            LoadOrCreateTile(MarkerAPath);
            LoadOrCreateTile(MarkerBPath);
            LoadOrCreateTile(EdgeMarkerPath);

            BuildTileSet(Root + "/LandformGrass", "GrassLandformTileSet");
            BuildTileSet(Root + "/LandformSoil", "SoilLandformTileSet");
            BuildTileSet(Root + "/EdgeGrassOnSoilRefined", "GrassOnSoilRefinedTileSet");
            TerrainBrushImportSetup.EnsureComplementedViewsForRegisteredBrushes();
            EnsureOriginalBrushDefinition();
            return RefreshPaletteFromRegisteredBrushes();
        }

        /// <summary>
        /// Loads the authored terrain package without repairing it. Release validation must use
        /// this path so a missing or stale authored asset fails the gate instead of being silently
        /// regenerated during a build.
        /// </summary>
        internal static BattlefieldTerrainPalette RequirePaletteAssets()
        {
            var palette = RequireAsset<BattlefieldTerrainPalette>(
                ProjectSetup.BattlefieldTerrainPalettePath);
            var firstLevelPalette = RequireAsset<BattlefieldTerrainPalette>(
                FirstLevelSquarePalettePath);

            RequireAsset<Texture2D>(GrassBasePath);
            RequireAsset<Texture2D>(SoilBasePath);
            RequireAsset<Texture2D>(FirstLevelSquareGrassPath);
            RequireAsset<Texture2D>(FirstLevelSquareSoilPath);
            RequireAsset<Tile>(GrassBaseTilePath);
            RequireAsset<Tile>(SoilBaseTilePath);
            RequireAsset<Tile>(MarkerAPath);
            RequireAsset<Tile>(MarkerBPath);
            RequireAsset<Tile>(EdgeMarkerPath);
            RequireAsset<TerrainBrushDefinition>(OriginalBrushDefinitionPath);
            RequireAsset<TextAsset>(OriginalBrushSourceRecordPath);

            foreach (var path in new[]
                     {
                         GrassLandformTileSetPath,
                         SoilLandformTileSetPath,
                         GrassOnSoilEdgeTileSetPath,
                         OrganicStoneRoadTileSetPath,
                         ProjectSetup.BattlefieldGrassTileSetPath,
                         SquareTerrainArtProfile.SoilLandformTileSetPath,
                         SquareTerrainArtProfile.StoneRoadLandformTileSetPath,
                         SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath,
                     })
                RequireValidTileSet(path);

            if (!palette.Validate(out var paletteReason))
                throw new InvalidOperationException(
                    "Authored layered terrain palette is invalid: " + paletteReason);
            if (!firstLevelPalette.Validate(out var firstLevelReason))
                throw new InvalidOperationException(
                    "Authored first-level square terrain palette is invalid: "
                    + firstLevelReason);
            if (!TerrainBrushRegistry.Validate(out var registryReason))
                throw new InvalidOperationException(
                    "Authored terrain brush registry is invalid: " + registryReason);
            return palette;
        }

        internal static BattlefieldTerrainPalette RefreshPaletteFromRegisteredBrushes()
        {
            var grassLandform = RequireAsset<DualGridTileSet>(GrassLandformTileSetPath);
            var soilLandform = RequireAsset<DualGridTileSet>(SoilLandformTileSetPath);
            var grassOnSoil = RequireAsset<DualGridTileSet>(GrassOnSoilEdgeTileSetPath);
            var squareGrass = RequireAsset<DualGridTileSet>(
                ProjectSetup.BattlefieldGrassTileSetPath);
            var squareSoil = RequireAsset<DualGridTileSet>(
                SquareTerrainArtProfile.SoilLandformTileSetPath);
            var squareStoneRoad = RequireAsset<DualGridTileSet>(
                SquareTerrainArtProfile.StoneRoadLandformTileSetPath);
            var routeTileSet = RequireAsset<DualGridTileSet>(OrganicStoneRoadTileSetPath);
            var definitions = TerrainBrushRegistry.FindAll();
            var bases = RegisteredBaseBindings(definitions);
            var edges = RegisteredEdgeBindings(definitions, grassOnSoil);

            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ProjectSetup.BattlefieldTerrainPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
                AssetDatabase.CreateAsset(palette, ProjectSetup.BattlefieldTerrainPalettePath);
            }
            palette.ConfigureLayered(BundledLevelCatalogIds.TerrainPalettes.OrchardDefault,
                bases,
                new[]
                {
                    new BattlefieldTerrainLandformBinding(BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.ContourStyles.Organic, soilLandform),
                    new BattlefieldTerrainLandformBinding(BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Organic, grassLandform),
                    new BattlefieldTerrainLandformBinding(BattlefieldLayerIds.Surfaces.StoneRoad,
                        BattlefieldLayerIds.ContourStyles.Organic, routeTileSet),
                    new BattlefieldTerrainLandformBinding(BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.ContourStyles.Square, squareSoil),
                    new BattlefieldTerrainLandformBinding(BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square, squareGrass),
                    new BattlefieldTerrainLandformBinding(BattlefieldLayerIds.Surfaces.StoneRoad,
                        BattlefieldLayerIds.ContourStyles.Square, squareStoneRoad),
                },
                edges);
            string reason;
            if (!palette.Validate(out reason))
                throw new InvalidOperationException("Layered terrain palette is invalid: " + reason);
            EditorUtility.SetDirty(palette);
            EnsureFirstLevelSquarePalette(palette);
            AssetDatabase.SaveAssets();
            return palette;
        }

        internal static BattlefieldTerrainPalette EnsureFirstLevelSquarePalette(
            BattlefieldTerrainPalette defaultPalette)
        {
            if (defaultPalette == null)
                throw new ArgumentNullException(nameof(defaultPalette));
            var grass = RequireAsset<Texture2D>(FirstLevelSquareGrassPath);
            var soil = RequireAsset<Texture2D>(FirstLevelSquareSoilPath);
            if (grass.width != FirstLevelSquareTileSize || grass.height != FirstLevelSquareTileSize
                || soil.width != FirstLevelSquareTileSize || soil.height != FirstLevelSquareTileSize)
                throw new InvalidOperationException(
                    "First-level square terrain textures must remain normalized 64x64 exports.");

            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                FirstLevelSquarePalettePath);
            if (palette == null)
            {
                EnsureFolder(FirstLevelSquareRoot);
                palette = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
                AssetDatabase.CreateAsset(palette, FirstLevelSquarePalettePath);
            }

            var bases = defaultPalette.SurfaceBindings.Select(binding =>
                new BattlefieldTerrainSurfaceBinding(binding.SurfaceId,
                    string.Equals(binding.SurfaceId, BattlefieldLayerIds.Surfaces.Grass,
                        StringComparison.Ordinal) ? grass
                    : string.Equals(binding.SurfaceId, BattlefieldLayerIds.Surfaces.Soil,
                        StringComparison.Ordinal) ? soil
                    : binding.BaseTexture)).ToArray();
            var landforms = defaultPalette.LandformBindings.Select(binding =>
                new BattlefieldTerrainLandformBinding(binding.SurfaceId,
                    binding.ContourStyleId, binding.TileSet)).ToArray();
            var edges = defaultPalette.EdgeBindings.Select(binding =>
                new BattlefieldTerrainEdgeBinding(binding.LandformSurfaceId,
                    binding.BaseSurfaceId, binding.ContourStyleId,
                    binding.EdgeStyleId, binding.TileSet)).ToArray();
            palette.ConfigureLayered(BundledLevelCatalogIds.TerrainPalettes.Orchard01SquareGrid,
                bases, landforms, edges);
            if (!palette.Validate(out var reason))
                throw new InvalidOperationException(
                    "First-level square terrain palette is invalid: " + reason);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static BattlefieldTerrainSurfaceBinding[] RegisteredBaseBindings(
            IReadOnlyList<TerrainBrushDefinition> definitions)
        {
            var textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (!definition.Validate(out var reason))
                    throw new InvalidOperationException("Registered terrain brush is invalid: "
                        + AssetDatabase.GetAssetPath(definition) + ": " + reason);
                if (!definition.PublishEndpointsToPalette) continue;
                RegisterEndpoint(textures, definition.LandformSurfaceId,
                    definition.ForegroundTexture, definition.BrushId);
                RegisterEndpoint(textures, definition.BaseSurfaceId,
                    definition.BackgroundTexture, definition.BrushId);
            }
            if (!textures.ContainsKey(BattlefieldLayerIds.Surfaces.Soil))
                textures.Add(BattlefieldLayerIds.Surfaces.Soil,
                    RequireAsset<Texture2D>(SoilBasePath));
            if (!textures.ContainsKey(BattlefieldLayerIds.Surfaces.Grass))
                textures.Add(BattlefieldLayerIds.Surfaces.Grass,
                    RequireAsset<Texture2D>(GrassBasePath));
            if (!textures.ContainsKey(BattlefieldLayerIds.Surfaces.StoneRoad))
                textures.Add(BattlefieldLayerIds.Surfaces.StoneRoad,
                    RequireAsset<Texture2D>(SquareTerrainArtProfile.StoneRoadSourcePath));
            return textures.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new BattlefieldTerrainSurfaceBinding(pair.Key, pair.Value))
                .ToArray();
        }

        private static BattlefieldTerrainEdgeBinding[] RegisteredEdgeBindings(
            IReadOnlyList<TerrainBrushDefinition> definitions,
            DualGridTileSet organicGrassOnSoil)
        {
            var bindings = new Dictionary<string, BattlefieldTerrainEdgeBinding>(
                StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (!definition.PublishEndpointsToPalette) continue;
                var key = EdgeKey(definition.LandformSurfaceId, definition.BaseSurfaceId,
                    definition.ContourStyleId, definition.EdgeStyleId);
                if (bindings.ContainsKey(key))
                    throw new InvalidOperationException("Duplicate registered terrain brush edge: "
                        + key);
                bindings.Add(key, new BattlefieldTerrainEdgeBinding(
                    definition.LandformSurfaceId, definition.BaseSurfaceId,
                    definition.ContourStyleId, definition.EdgeStyleId,
                    definition.CompositeTileSet));
            }
            var organicKey = EdgeKey(BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.ContourStyles.Organic,
                BattlefieldLayerIds.EdgeStyles.Refined);
            if (!bindings.ContainsKey(organicKey))
                bindings.Add(organicKey, new BattlefieldTerrainEdgeBinding(
                    BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Organic,
                    BattlefieldLayerIds.EdgeStyles.Refined, organicGrassOnSoil));
            return bindings.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Value).ToArray();
        }

        private static void EnsureOriginalBrushDefinition()
        {
            EnsureFolder(OriginalBrushRoot);
            var definition = AssetDatabase.LoadAssetAtPath<TerrainBrushDefinition>(
                OriginalBrushDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<TerrainBrushDefinition>();
                AssetDatabase.CreateAsset(definition, OriginalBrushDefinitionPath);
            }
            var edge = RequireAsset<DualGridTileSet>(
                SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath);
            var reverse = TerrainBrushImportSetup.BuildComplementedTileSet(
                OriginalBrushRoot + "/OriginalGrassSoilReverseLandformTileSet.asset", edge);
            var grassBase = RequireAsset<Tile>(GrassBaseTilePath);
            var soilBase = RequireAsset<Tile>(SoilBaseTilePath);
            definition.name = "OriginalGrassSoilBrush";
            definition.Configure(OriginalBrushId, "原版草地 + 泥土",
                "original-square-grass-soil", "草地", "泥土",
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.ContourStyles.Square,
                BattlefieldLayerIds.EdgeStyles.Refined, 15, 0,
                SquareTerrainArtProfile.TileSize,
                edge, reverse, grassBase, soilBase,
                RequireAsset<Texture2D>(GrassBasePath),
                RequireAsset<Texture2D>(SoilBasePath),
                RequireAsset<TextAsset>(OriginalBrushSourceRecordPath), false);
            if (!definition.Validate(out var reason))
                throw new InvalidOperationException(
                    "Original terrain brush registration is invalid: " + reason);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            TerrainBrushRegistry.Invalidate();
        }

        private static void RegisterEndpoint(IDictionary<string, Texture2D> textures,
            string surfaceId, Texture2D texture, string brushId)
        {
            if (textures.TryGetValue(surfaceId, out var existing))
            {
                if (existing != texture)
                    throw new InvalidOperationException("Registered brushes assign different "
                        + "endpoint textures to surface '" + surfaceId + "': " + brushId);
                return;
            }
            textures.Add(surfaceId, texture);
        }

        private static string EdgeKey(string landform, string background,
            string contour, string edge)
        {
            return landform + "|" + background + "|" + contour + "|" + edge;
        }

        public static void CreateAcceptanceScene()
        {
            EnsurePaletteAssets();
            EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("LayeredTerrainAcceptance");
            root.AddComponent<LayeredTerrainAcceptancePresenter>();

            var gridObject = new GameObject("LayeredTerrainGrid");
            gridObject.transform.SetParent(root.transform, false);
            var grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var baseLogical = CreateTilemap(gridObject.transform, "Source-Base", false, 0);
            var landformLogical = CreateTilemap(gridObject.transform, "Source-Landform", false, 0);
            var edgeLogical = CreateTilemap(gridObject.transform, "Source-Edge", false, 0);
            var baseOutput = CreateTilemap(gridObject.transform, "Output-Base", true, 0);
            var landformAOutput = CreateTilemap(gridObject.transform, "Output-Grass", true, 10);
            var landformBOutput = CreateTilemap(gridObject.transform, "Output-Soil", true, 11);
            var edgeAOnBOutput = CreateTilemap(gridObject.transform, "Output-GrassOnSoil-Refined", true, 20);
            var edgeBOnAOutput = CreateTilemap(gridObject.transform, "Output-SoilOnGrass-Refined", true, 21);

            var grassBaseTile = BuildBaseTile(GrassBasePath, GrassBaseTilePath);
            var soilBaseTile = BuildBaseTile(SoilBasePath, SoilBaseTilePath);
            var markerA = LoadOrCreateTile(MarkerAPath);
            var markerB = LoadOrCreateTile(MarkerBPath);
            var edgeMarker = LoadOrCreateTile(EdgeMarkerPath);
            var renderer = root.AddComponent<LayeredTerrainTilemap>();
            var squareGrass = RequireAsset<DualGridTileSet>(
                SquareTerrainArtProfile.GrassLandformTileSetPath);
            var squareSoil = RequireAsset<DualGridTileSet>(
                SquareTerrainArtProfile.SoilLandformTileSetPath);
            var squareEdge = RequireAsset<DualGridTileSet>(
                SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath);
            renderer.Configure(baseLogical, landformLogical, edgeLogical,
                baseOutput, landformAOutput, landformBOutput,
                edgeAOnBOutput, edgeBOnAOutput,
                markerA, markerB, edgeMarker, grassBaseTile, soilBaseTile,
                squareGrass, squareSoil, squareEdge, null);
            renderer.ConfigureContourBindings(new[]
                {
                    new LayeredTerrainContourBinding(BattlefieldLayerIds.ContourStyles.Square,
                        squareGrass, squareSoil, squareEdge, null),
                }, BattlefieldLayerIds.ContourStyles.Square);
            renderer.ConfigureAuthoringPresentation("草地", grassBaseTile.sprite,
                new Color(.31f, .76f, .24f, 1f), "泥土", soilBaseTile.sprite,
                new Color(.61f, .38f, .2f, 1f));
            PaintAcceptanceBoard(renderer);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(3.5f, 6.5f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .09f, .1f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 50f;

            EditorSceneManager.SaveScene(scene, AcceptanceScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Layered terrain acceptance scene created without changing release build scenes: "
                + AcceptanceScenePath);
        }

        public static void RenderAcceptanceEvidence()
        {
            CreateAcceptanceScene();
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Acceptance camera is unavailable.");
            var target = new RenderTexture(402, 874, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                var image = new Texture2D(402, 874, TextureFormat.RGBA32, false);
                try
                {
                    image.ReadPixels(new Rect(0, 0, 402, 874), 0, 0);
                    image.Apply(false, false);
                    var directory = Path.GetDirectoryName(AcceptanceEvidencePath);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                    File.WriteAllBytes(AcceptanceEvidencePath, image.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(target);
            }
            Debug.Log("Layered terrain evidence rendered to " + AcceptanceEvidencePath);
        }

        public static void BuildAcceptanceWebGl()
        {
            CreateAcceptanceScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { AcceptanceScenePath },
                locationPathName = AcceptanceBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Layered terrain WebGL build failed: "
                    + report.summary.result);
            var indexPath = Path.Combine(AcceptanceBuildPath, "index.html");
            var index = File.ReadAllText(indexPath);
            const string headEnd = "</head>";
            const string portraitStyle = "<style>html,body{margin:0;width:100%;height:100%;"
                + "overflow:hidden;background:#091718}#unity-container{width:100vw!important;"
                + "height:100vh!important}#unity-canvas{display:block;width:100vw!important;"
                + "height:100vh!important}</style>";
            if (!index.Contains(headEnd))
                throw new InvalidOperationException("Acceptance WebGL index is missing </head>.");
            File.WriteAllText(indexPath, index.Replace(headEnd, portraitStyle + headEnd));
            Debug.Log("Layered terrain WebGL acceptance build completed: " + AcceptanceBuildPath);
        }

        private static void PaintAcceptanceBoard(LayeredTerrainTilemap renderer)
        {
            string reason;
            for (var y = 0; y < 14; y++)
            for (var x = 0; x < 8; x++)
                Require(renderer.PaintBase(new Vector3Int(x, y, 0),
                    x < 4 ? LayeredTerrainMaterial.B : LayeredTerrainMaterial.A, out reason), reason);

            var grassRefined = new[]
            {
                new Vector3Int(0, 9), new Vector3Int(1, 9), new Vector3Int(1, 10),
                new Vector3Int(2, 10), new Vector3Int(2, 11), new Vector3Int(3, 11),
                new Vector3Int(0, 12), new Vector3Int(1, 12), new Vector3Int(2, 12),
                new Vector3Int(3, 13),
            };
            var soilRefined = new[]
            {
                new Vector3Int(7, 9), new Vector3Int(6, 9), new Vector3Int(6, 10),
                new Vector3Int(5, 10), new Vector3Int(5, 11), new Vector3Int(4, 11),
                new Vector3Int(5, 12), new Vector3Int(6, 12), new Vector3Int(7, 12),
                new Vector3Int(4, 13),
            };
            foreach (var cell in grassRefined)
                Require(renderer.PaintPair(cell, LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
            foreach (var cell in soilRefined)
                Require(renderer.PaintPair(cell, LayeredTerrainMaterial.B,
                    LayeredTerrainMaterial.A, true, out reason), reason);

            for (var x = 0; x < 3; x++)
                Require(renderer.PaintPair(new Vector3Int(x, 6 + x % 2, 0),
                    LayeredTerrainMaterial.A, LayeredTerrainMaterial.B, true, out reason), reason);
            for (var x = 5; x < 8; x++)
                Require(renderer.PaintPair(new Vector3Int(x, 6 + x % 2, 0),
                    LayeredTerrainMaterial.B, LayeredTerrainMaterial.A, true, out reason), reason);

            Require(renderer.PaintPair(new Vector3Int(1, 4, 0), LayeredTerrainMaterial.A,
                LayeredTerrainMaterial.B, true, out reason), reason);
            Require(renderer.PaintPair(new Vector3Int(2, 5, 0), LayeredTerrainMaterial.A,
                LayeredTerrainMaterial.B, true, out reason), reason);
            Require(renderer.PaintPair(new Vector3Int(5, 4, 0), LayeredTerrainMaterial.B,
                LayeredTerrainMaterial.A, true, out reason), reason);
            Require(renderer.PaintPair(new Vector3Int(6, 5, 0), LayeredTerrainMaterial.B,
                LayeredTerrainMaterial.A, true, out reason), reason);
            Require(renderer.Rebuild(out reason), reason);
        }

        private static Tilemap CreateTilemap(Transform parent, string name, bool render, int order)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var tilemap = gameObject.AddComponent<Tilemap>();
            if (render)
            {
                var tilemapRenderer = gameObject.AddComponent<TilemapRenderer>();
                tilemapRenderer.sortingOrder = order;
            }
            return tilemap;
        }

        private static DualGridTileSet BuildTileSet(string folder, string name)
        {
            var tileSetPath = folder + "/" + name + ".asset";
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(tileSetPath);
            if (tileSet == null)
            {
                tileSet = ScriptableObject.CreateInstance<DualGridTileSet>();
                AssetDatabase.CreateAsset(tileSet, tileSetPath);
            }
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var texturePath = folder + "/Mask-" + mask.ToString("00") + ".png";
                ConfigureTexture(texturePath, false, NativeTileSize);
                var tilePath = folder + "/Mask-" + mask.ToString("00") + ".asset";
                tileSet.SetTile((DualGridMask)mask, BuildTile(texturePath, tilePath));
            }
            EditorUtility.SetDirty(tileSet);
            return tileSet;
        }

        private static Tile BuildBaseTile(string texturePath, string tilePath)
        {
            ConfigureTexture(texturePath, true, NativeTileSize);
            return BuildTile(texturePath, tilePath);
        }

        private static Tile BuildTile(string texturePath, string tilePath)
        {
            var sprite = RequireAsset<Sprite>(texturePath);
            var tile = LoadOrCreateTile(tilePath);
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockAll;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static Tile LoadOrCreateTile(string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static void ConfigureTexture(string path, bool repeat, int nativeTileSize)
        {
            if (!File.Exists(Path.GetFullPath(path)))
                throw new FileNotFoundException("Layered terrain source image is missing.", path);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = nativeTileSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = nativeTileSize;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            throw new InvalidOperationException(typeof(T).Name + " asset is unavailable: " + path);
        }

        private static void RequireValidTileSet(string path)
        {
            var tileSet = RequireAsset<DualGridTileSet>(path);
            if (!tileSet.Validate(out var reason))
                throw new InvalidOperationException("DualGridTileSet is invalid: "
                    + path + ": " + reason);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            if (separator <= 0) throw new ArgumentException("Invalid asset folder: " + path);
            var parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException(reason);
        }
    }
}
