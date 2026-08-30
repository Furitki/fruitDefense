using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public static class CellAlignedSquareTerrainTrial
    {
        internal const string Root =
            "Assets/LayeredTerrain/Trials/CellAlignedSquare";
        internal const string GrassTexturePath = Root + "/GrassSquareBase-v1.png";
        internal const string SoilTexturePath = Root + "/SoilSquareBase-v1.png";
        internal const string GrassTilePath = Root + "/GrassSquareBase-v1.asset";
        internal const string SoilTilePath = Root + "/SoilSquareBase-v1.asset";
        internal const string PalettePath = Root + "/CellAlignedSquareTrialPalette.asset";
        internal const string ScenePath = Root + "/CellAlignedSquareTerrainTrial.unity";
        internal const string EvidencePath =
            "Builds/Evidence/cell-aligned-square-terrain/cell-aligned-square-terrain-v1.png";
        internal const string WebGlBuildPath =
            "Builds/CellAlignedSquareTerrainTrialWebGL";
        internal const string PaletteId = "palette.trial.cell-aligned-square.v1";
        internal const int TextureSize = 64;
        internal const int BoardWidth = 8;
        internal const int BoardHeight = 14;

        private const string MarkerAPath =
            "Assets/LayeredTerrain/GrassSoil/Authoring/MarkerA.asset";
        private const string MarkerBPath =
            "Assets/LayeredTerrain/GrassSoil/Authoring/MarkerB.asset";
        private const string EdgeMarkerPath =
            "Assets/LayeredTerrain/GrassSoil/Authoring/EdgeEnabled.asset";

        [MenuItem("Fruit Defense/地图工具/纯方块兼容试验/打开试验场景")]
        public static void OpenTrialScene()
        {
            GenerateArtifacts();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Single();
            Selection.activeGameObject = camera.gameObject;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log("FRUIT_DEFENSE_CELL_ALIGNED_SQUARE_TRIAL_OPENED: " + ScenePath);
        }

        [MenuItem("Fruit Defense/地图工具/纯方块兼容试验/生成试验场景")]
        public static void GenerateArtifacts()
        {
            EnsureFolder(Root);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(GrassTexturePath);
            ConfigureTexture(SoilTexturePath);
            var grassTile = BuildBaseTile(GrassTexturePath, GrassTilePath);
            var soilTile = BuildBaseTile(SoilTexturePath, SoilTilePath);
            CreateTrialPalette();
            if (grassTile == null || soilTile == null)
                throw new InvalidOperationException("Trial base tile generation failed.");
            CreateTrialScene();
            string reason;
            if (!Validate(out reason))
                throw new InvalidOperationException("Cell-aligned square trial is invalid: " + reason);
            Debug.Log("FRUIT_DEFENSE_CELL_ALIGNED_SQUARE_TRIAL_READY: " + ScenePath);
        }

        [MenuItem("Fruit Defense/地图工具/纯方块兼容试验/渲染对比图")]
        public static void RenderEvidence()
        {
            GenerateArtifacts();
            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var camera = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Single();
                var target = new RenderTexture(402, 874, 24, RenderTextureFormat.ARGB32);
                var previousTarget = camera.targetTexture;
                var previousRenderTarget = RenderTexture.active;
                try
                {
                    camera.targetTexture = target;
                    RenderTexture.active = target;
                    camera.Render();
                    var image = new Texture2D(402, 874, TextureFormat.RGBA32, false);
                    try
                    {
                        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
                        image.Apply(false, false);
                        var directory = Path.GetDirectoryName(EvidencePath);
                        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                        File.WriteAllBytes(EvidencePath, image.EncodeToPNG());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                }
                finally
                {
                    camera.targetTexture = previousTarget;
                    RenderTexture.active = previousRenderTarget;
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log("FRUIT_DEFENSE_CELL_ALIGNED_SQUARE_TRIAL_EVIDENCE_OK: "
                + EvidencePath);
        }

        [MenuItem("Fruit Defense/地图工具/纯方块兼容试验/构建独立 WebGL")]
        public static void BuildTrialWebGl()
        {
            GenerateArtifacts();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WebGlBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Cell-aligned square trial WebGL build failed: "
                    + report.summary.result);
            Debug.Log("FRUIT_DEFENSE_CELL_ALIGNED_SQUARE_TRIAL_WEBGL_OK: "
                + WebGlBuildPath);
        }

        internal static bool Validate(out string reason)
        {
            if (!ValidateTexture(GrassTexturePath, out reason)
                || !ValidateTexture(SoilTexturePath, out reason)) return false;

            var release = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ProjectSetup.BattlefieldTerrainPalettePath);
            var trial = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(PalettePath);
            if (release == null || trial == null || ReferenceEquals(release, trial))
            {
                reason = "Trial and release terrain palettes must exist as distinct assets.";
                return false;
            }
            if (!string.Equals(trial.PaletteId, PaletteId, StringComparison.Ordinal))
            {
                reason = "Trial palette identity is not isolated.";
                return false;
            }
            Texture2D grass;
            Texture2D soil;
            if (!trial.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Grass, out grass)
                || !trial.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil, out soil)
                || AssetDatabase.GetAssetPath(grass) != GrassTexturePath
                || AssetDatabase.GetAssetPath(soil) != SoilTexturePath)
            {
                reason = "Trial palette does not bind the generated square textures.";
                return false;
            }
            Texture2D releaseGrass;
            Texture2D releaseSoil;
            if (!release.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Grass,
                    out releaseGrass)
                || !release.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil,
                    out releaseSoil)
                || AssetDatabase.GetAssetPath(releaseGrass) == GrassTexturePath
                || AssetDatabase.GetAssetPath(releaseSoil) == SoilTexturePath)
            {
                reason = "Release palette was contaminated by trial textures.";
                return false;
            }
            if (!trial.Validate(out reason)) return false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                reason = "Trial scene asset is missing.";
                return false;
            }
            if (EditorBuildSettings.scenes.Any(value => string.Equals(value.path, ScenePath,
                    StringComparison.Ordinal)))
            {
                reason = "Trial scene must not be registered in release build settings.";
                return false;
            }
            reason = "ok";
            return true;
        }

        private static void CreateTrialPalette()
        {
            var releasePath = ProjectSetup.BattlefieldTerrainPalettePath;
            var releaseAbsolute = Path.GetFullPath(releasePath);
            var releaseBefore = File.ReadAllBytes(releaseAbsolute);
            var release = RequireAsset<BattlefieldTerrainPalette>(releasePath);
            var grass = RequireAsset<Texture2D>(GrassTexturePath);
            var soil = RequireAsset<Texture2D>(SoilTexturePath);
            var trial = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(PalettePath);
            if (trial == null)
            {
                trial = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
                AssetDatabase.CreateAsset(trial, PalettePath);
            }
            var bases = release.SurfaceBindings.Select(binding =>
                new BattlefieldTerrainSurfaceBinding(binding.SurfaceId,
                    string.Equals(binding.SurfaceId, BattlefieldLayerIds.Surfaces.Grass,
                        StringComparison.Ordinal) ? grass
                    : string.Equals(binding.SurfaceId, BattlefieldLayerIds.Surfaces.Soil,
                        StringComparison.Ordinal) ? soil : binding.BaseTexture));
            var landforms = release.LandformBindings.Select(binding =>
                new BattlefieldTerrainLandformBinding(binding.SurfaceId,
                    binding.ContourStyleId, binding.TileSet));
            var edges = release.EdgeBindings.Select(binding =>
                new BattlefieldTerrainEdgeBinding(binding.LandformSurfaceId,
                    binding.BaseSurfaceId, binding.ContourStyleId, binding.EdgeStyleId,
                    binding.TileSet));
            trial.ConfigureLayered(PaletteId, bases, landforms, edges);
            EditorUtility.SetDirty(trial);
            AssetDatabase.SaveAssetIfDirty(trial);
            if (!releaseBefore.SequenceEqual(File.ReadAllBytes(releaseAbsolute)))
                throw new InvalidOperationException(
                    "Trial generation changed the release terrain palette on disk.");
        }

        private static void CreateTrialScene()
        {
            SceneSetup[] previousSetup = null;
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    throw new OperationCanceledException(
                        "Cell-aligned square trial generation was cancelled before changing scenes.");
                previousSetup = EditorSceneManager.GetSceneManagerSetup();
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            try
            {
                var grassTile = RequireAsset<Tile>(GrassTilePath);
                var soilTile = RequireAsset<Tile>(SoilTilePath);
                var root = new GameObject("CellAlignedSquareTerrainTrial");
                var gridObject = new GameObject("TerrainGrid");
                gridObject.transform.SetParent(root.transform, false);
                var grid = gridObject.AddComponent<Grid>();
                grid.cellSize = Vector3.one;

                var baseLogical = CreateTilemap(gridObject.transform, "Source-Base", false, 0);
                var landformLogical = CreateTilemap(gridObject.transform,
                    "Source-Landform", false, 0);
                var edgeLogical = CreateTilemap(gridObject.transform, "Source-Edge", false, 0);
                var baseOutput = CreateTilemap(gridObject.transform, "Output-Base", true, 0);
                var landformAOutput = CreateTilemap(gridObject.transform,
                    "Output-Grass-DualGrid", true, 10);
                var landformBOutput = CreateTilemap(gridObject.transform,
                    "Output-Soil-DualGrid", true, 11);
                var edgeAOnBOutput = CreateTilemap(gridObject.transform,
                    "Output-GrassOnSoil-Edge", true, 20);
                var edgeBOnAOutput = CreateTilemap(gridObject.transform,
                    "Output-SoilOnGrass-Edge", true, 21);

                var squareGrass = RequireAsset<DualGridTileSet>(
                    SquareTerrainArtProfile.GrassLandformTileSetPath);
                var squareSoil = RequireAsset<DualGridTileSet>(
                    SquareTerrainArtProfile.SoilLandformTileSetPath);
                var squareEdge = RequireAsset<DualGridTileSet>(
                    SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath);
                var renderer = root.AddComponent<LayeredTerrainTilemap>();
                renderer.Configure(baseLogical, landformLogical, edgeLogical,
                    baseOutput, landformAOutput, landformBOutput,
                    edgeAOnBOutput, edgeBOnAOutput,
                    RequireAsset<TileBase>(MarkerAPath), RequireAsset<TileBase>(MarkerBPath),
                    RequireAsset<TileBase>(EdgeMarkerPath), grassTile, soilTile,
                    squareGrass, squareSoil, squareEdge, null, false);
                renderer.ConfigureContourBindings(new[]
                {
                    new LayeredTerrainContourBinding(
                        BattlefieldLayerIds.ContourStyles.Square,
                        squareGrass, squareSoil, squareEdge, null),
                }, BattlefieldLayerIds.ContourStyles.Square);
                renderer.ConfigureAuthoringPresentation("草地方块", grassTile.sprite,
                    new Color(.63f, .82f, .31f, 1f), "泥地方块", soilTile.sprite,
                    new Color(.82f, .61f, .31f, 1f));
                PaintBoard(renderer);

                var cameraObject = new GameObject("Main Camera");
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(3.5f, 6.5f, -10f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 9.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.74f, .9f, .98f, 1f);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 50f;

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Failed to save trial scene: " + ScenePath);
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        private static void PaintBoard(LayeredTerrainTilemap renderer)
        {
            string reason;
            for (var y = 0; y < BoardHeight; y++)
            for (var x = 0; x < BoardWidth; x++)
            {
                var material = y >= 9
                    ? LayeredTerrainMaterial.A : LayeredTerrainMaterial.B;
                Require(renderer.PaintBase(new Vector3Int(x, y, 0), material,
                    out reason), reason);
            }

            var dualGridIsland = new[]
            {
                new Vector3Int(2, 1, 0), new Vector3Int(3, 1, 0),
                new Vector3Int(4, 1, 0), new Vector3Int(5, 1, 0),
                new Vector3Int(1, 2, 0), new Vector3Int(2, 2, 0),
                new Vector3Int(3, 2, 0), new Vector3Int(4, 2, 0),
                new Vector3Int(5, 2, 0), new Vector3Int(6, 2, 0),
                new Vector3Int(2, 3, 0), new Vector3Int(3, 3, 0),
                new Vector3Int(4, 3, 0), new Vector3Int(5, 3, 0),
            };
            foreach (var cell in dualGridIsland)
                Require(renderer.PaintPair(cell, LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
            Require(renderer.Rebuild(out reason), reason);
        }

        private static bool ValidateTexture(string path, out string reason)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (importer == null || texture == null || sprite == null)
            {
                reason = "Trial texture, Sprite, or importer is missing: " + path;
                return false;
            }
            if (importer.wrapMode != TextureWrapMode.Repeat
                || importer.textureType != TextureImporterType.Sprite
                || !importer.isReadable || importer.mipmapEnabled)
            {
                reason = "Trial texture import settings are invalid: " + path;
                return false;
            }
            if (!Mathf.Approximately(sprite.bounds.size.x, 1f)
                || !Mathf.Approximately(sprite.bounds.size.y, 1f))
            {
                reason = "Trial Sprite must occupy exactly one terrain cell: " + path;
                return false;
            }
            var pixels = texture.GetPixels32();
            if (pixels.Any(pixel => pixel.a != byte.MaxValue))
            {
                reason = "Trial base texture is not fully opaque: " + path;
                return false;
            }
            var horizontal = BorderDifference(pixels, texture.width, texture.height, true);
            var vertical = BorderDifference(pixels, texture.width, texture.height, false);
            if (horizontal > 8f || vertical > 8f)
            {
                reason = "Trial texture opposite-edge difference is too high: " + path
                    + " horizontal=" + horizontal.ToString("0.00")
                    + " vertical=" + vertical.ToString("0.00");
                return false;
            }
            if (MaximumChannelStandardDeviation(pixels) > 2f)
            {
                reason = "Trial texture contains too much within-cell tonal variation: " + path;
                return false;
            }
            reason = "ok";
            return true;
        }

        private static float MaximumChannelStandardDeviation(Color32[] pixels)
        {
            if (pixels == null || pixels.Length == 0) return float.PositiveInfinity;
            var meanR = pixels.Average(pixel => (double)pixel.r);
            var meanG = pixels.Average(pixel => (double)pixel.g);
            var meanB = pixels.Average(pixel => (double)pixel.b);
            var varianceR = pixels.Average(pixel => Math.Pow(pixel.r - meanR, 2d));
            var varianceG = pixels.Average(pixel => Math.Pow(pixel.g - meanG, 2d));
            var varianceB = pixels.Average(pixel => Math.Pow(pixel.b - meanB, 2d));
            return (float)Math.Sqrt(Math.Max(varianceR,
                Math.Max(varianceG, varianceB)));
        }

        private static float BorderDifference(Color32[] pixels, int width, int height,
            bool horizontal)
        {
            var samples = horizontal ? height : width;
            var total = 0f;
            for (var index = 0; index < samples; index++)
            {
                var first = horizontal ? pixels[index * width] : pixels[index];
                var second = horizontal ? pixels[index * width + width - 1]
                    : pixels[(height - 1) * width + index];
                total += (Mathf.Abs(first.r - second.r) + Mathf.Abs(first.g - second.g)
                    + Mathf.Abs(first.b - second.b)) / 3f;
            }
            return total / Mathf.Max(1, samples);
        }

        private static void ConfigureTexture(string path)
        {
            if (!File.Exists(Path.GetFullPath(path)))
                throw new FileNotFoundException("Trial source image is missing.", path);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Texture importer is missing: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.GetSourceTextureWidthAndHeight(out var sourceWidth, out var sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0 || sourceWidth != sourceHeight)
                throw new InvalidOperationException(
                    "Trial source texture must be a non-empty square: " + path);
            importer.spritePixelsPerUnit = sourceWidth;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = TextureSize;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Tile BuildBaseTile(string texturePath, string tilePath)
        {
            var sprite = RequireAsset<Sprite>(texturePath);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.transform = Matrix4x4.identity;
            tile.flags = TileFlags.LockAll;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssetIfDirty(tile);
            return tile;
        }

        private static Tilemap CreateTilemap(Transform parent, string name, bool render,
            int order)
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

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            throw new InvalidOperationException(typeof(T).Name + " asset is unavailable: " + path);
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
