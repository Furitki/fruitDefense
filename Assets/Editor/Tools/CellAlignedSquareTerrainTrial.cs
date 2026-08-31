using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        internal const string ApprovedReferencePath =
            Root + "/approved-in-game-grid-reference-v2.png";
        internal const string GrassTexturePath = Root + "/GrassSquareBase-v2.png";
        internal const string SoilTexturePath = Root + "/SoilSquareBase-v2.png";
        internal const string GrassTilePath = Root + "/GrassSquareBase-v2.asset";
        internal const string SoilTilePath = Root + "/SoilSquareBase-v2.asset";
        internal const string PromptPath = Root + "/prompts-v2.md";
        internal const string ProvenancePath = Root + "/art-provenance-v2.json";
        internal const string PalettePath = Root + "/CellAlignedSquareTrialPalette.asset";
        internal const string ScenePath = Root + "/CellAlignedSquareTerrainTrial.unity";
        internal const string EvidencePath =
            "Builds/Evidence/cell-aligned-square-terrain/cell-aligned-square-terrain-v2.png";
        internal const string WebGlBuildPath =
            "Builds/CellAlignedSquareTerrainTrialWebGL";
        internal const string PaletteId = "palette.trial.cell-aligned-square.v2";
        internal const int TextureSize = 64;
        internal const int BoardWidth = 8;
        internal const int BoardHeight = 14;

        private const string ApprovedReferenceSha256 =
            "E1E3F5180FCBA505571E96401B09BAE2F1B3756A5DCD63A8376CF18568C2671D";
        private const string GrassTextureSha256 =
            "CF2EAC649F4F92F86B4999CF9D7447272A1233220D2ABC2395E3458BDBE4F321";
        private const string SoilTextureSha256 =
            "2B3794E23E55C5BFE3643C5AF833A3E473BAB19D08DA09AC10741D7FA54554F4";

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
            if (!ValidateApprovedSources(out reason)) return false;
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
                cameraObject.transform.position = new Vector3(4f, 7f, -10f);
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
            for (var y = 0; y < 5; y++)
            for (var x = 0; x < BoardWidth; x++)
                Require(renderer.PaintBase(new Vector3Int(x, y, 0),
                    LayeredTerrainMaterial.B,
                    out reason), reason);

            for (var y = 7; y < BoardHeight; y++)
            for (var x = 0; x < BoardWidth; x++)
            {
                var isGrass = x < 7 && y > 7 && y < BoardHeight - 1;
                Require(renderer.PaintBase(new Vector3Int(x, y, 0),
                    isGrass ? LayeredTerrainMaterial.A : LayeredTerrainMaterial.B,
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
            if (texture.width != TextureSize || texture.height != TextureSize
                || pixels.Any(pixel => pixel.a != byte.MaxValue))
            {
                reason = "Trial base texture must be an opaque 64x64 cell: " + path;
                return false;
            }
            var interiorMean = MeanLuminance(pixels, texture.width, texture.height, 8, true);
            var frameMean = MeanLuminance(pixels, texture.width, texture.height, 8, false);
            var frameContrast = interiorMean - frameMean;
            if (frameContrast < 6f || frameContrast > 24f)
            {
                reason = "Trial texture does not preserve one restrained inset frame: " + path
                    + " contrast=" + frameContrast.ToString("0.00");
                return false;
            }
            if (InteriorLuminanceStandardDeviation(pixels, texture.width,
                    texture.height, 8) > 5f)
            {
                reason = "Trial texture contains too much interior tonal noise: " + path;
                return false;
            }
            reason = "ok";
            return true;
        }

        private static bool ValidateApprovedSources(out string reason)
        {
            var requiredText = new[] { PromptPath, ProvenancePath };
            var missing = requiredText.FirstOrDefault(path => !File.Exists(Path.GetFullPath(path)));
            if (!string.IsNullOrEmpty(missing))
            {
                reason = "Approved trial record is missing: " + missing;
                return false;
            }
            var expectedHashes = new[]
            {
                new KeyValuePair<string, string>(ApprovedReferencePath,
                    ApprovedReferenceSha256),
                new KeyValuePair<string, string>(GrassTexturePath, GrassTextureSha256),
                new KeyValuePair<string, string>(SoilTexturePath, SoilTextureSha256),
            };
            foreach (var entry in expectedHashes)
            {
                if (!File.Exists(Path.GetFullPath(entry.Key))
                    || !string.Equals(HashFile(entry.Key), entry.Value,
                        StringComparison.Ordinal))
                {
                    reason = "Approved trial pixel source drifted: " + entry.Key;
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        private static float MeanLuminance(Color32[] pixels, int width, int height,
            int inset, bool interior)
        {
            var total = 0f;
            var count = 0;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var isInterior = x >= inset && x < width - inset
                    && y >= inset && y < height - inset;
                if (isInterior != interior) continue;
                total += Luminance(pixels[y * width + x]);
                count++;
            }
            return total / Mathf.Max(1, count);
        }

        private static float InteriorLuminanceStandardDeviation(Color32[] pixels,
            int width, int height, int inset)
        {
            var mean = MeanLuminance(pixels, width, height, inset, true);
            var total = 0d;
            var count = 0;
            for (var y = inset; y < height - inset; y++)
            for (var x = inset; x < width - inset; x++)
            {
                var difference = Luminance(pixels[y * width + x]) - mean;
                total += difference * difference;
                count++;
            }
            return count == 0 ? float.PositiveInfinity
                : (float)Math.Sqrt(total / count);
        }

        private static float Luminance(Color32 pixel)
        {
            return .2126f * pixel.r + .7152f * pixel.g + .0722f * pixel.b;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(Path.GetFullPath(path)))
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
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
