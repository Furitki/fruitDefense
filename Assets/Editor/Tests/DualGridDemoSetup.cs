using System;
using System.IO;
using System.Linq;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

namespace FruitDefense.Editor
{
    public static class DualGridDemoSetup
    {
        public const string DemoRoot = "Assets/DualGridDemo";
        public const string DemoScenePath = "Assets/Scenes/DualGridDemo.unity";
        public const string EvidencePath = "Builds/Evidence/dual-grid-demo.png";
        public const string CartoonGrassTileSetPath =
            "Assets/DualGridDemo/CartoonGrass/CartoonGrassDualGridTileSet.asset";
        public const string CartoonGrassSoilBaseTilePath =
            "Assets/DualGridDemo/CartoonGrass/CartoonGrassSoilBase.asset";

        private const string TileFolder = DemoRoot + "/Tiles";
        private const string TileSetPath = DemoRoot + "/DemoDualGridTileSet.asset";
        private const string LogicalTilePath = TileFolder + "/LogicalPaint.asset";

        public static void CreateOrRefreshDemo()
        {
            EnsureFolder(DemoRoot);
            EnsureFolder(TileFolder);

            var logicalTile = LoadOrCreateDebugTile(LogicalTilePath, DualGridMask.Full,
                new Color(.42f, .64f, .95f, .28f), new Color(.18f, .30f, .54f, .35f));
            var debugTileSet = LoadOrCreateAsset<DualGridTileSet>(TileSetPath);
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var path = TileFolder + "/Mask-" + mask.ToString("00") + ".asset";
                var tile = LoadOrCreateDebugTile(path, (DualGridMask)mask,
                    new Color(.27f, .79f, .49f, 1f), new Color(.06f, .25f, .14f, 1f));
                debugTileSet.SetTile((DualGridMask)mask, tile);
            }
            EditorUtility.SetDirty(debugTileSet);
            AssetDatabase.SaveAssets();

            var selectedTileSetPath = SelectPreferredTileSetPath();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            logicalTile = AssetDatabase.LoadAssetAtPath<DualGridDebugTile>(LogicalTilePath);
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(selectedTileSetPath);
            var soilBaseTile = AssetDatabase.LoadAssetAtPath<TileBase>(CartoonGrassSoilBaseTilePath);
            if (logicalTile == null || tileSet == null)
                throw new InvalidOperationException("Dual-Grid demo assets could not be reloaded after scene reset.");
            CreateCamera();

            var gridObject = new GameObject("Dual-Grid Demo (select to rebuild)", typeof(Grid));
            var grid = gridObject.GetComponent<Grid>();
            grid.cellSize = Vector3.one;

            var logicalObject = new GameObject("Logical Paint - author here", typeof(Tilemap), typeof(TilemapRenderer));
            logicalObject.transform.SetParent(gridObject.transform, false);
            var logical = logicalObject.GetComponent<Tilemap>();
            logicalObject.GetComponent<TilemapRenderer>().enabled = false;

            if (soilBaseTile != null)
            {
                var soilObject = new GameObject("Soil Base - author-owned ground",
                    typeof(Tilemap), typeof(TilemapRenderer));
                soilObject.transform.SetParent(gridObject.transform, false);
                var soilTilemap = soilObject.GetComponent<Tilemap>();
                soilObject.GetComponent<TilemapRenderer>().sortingOrder = -10;
                PopulateSoilBase(soilTilemap, soilBaseTile);
            }

            var outputObject = new GameObject("Generated Visuals - do not edit", typeof(Tilemap), typeof(TilemapRenderer));
            outputObject.transform.SetParent(gridObject.transform, false);
            var output = outputObject.GetComponent<Tilemap>();
            outputObject.GetComponent<TilemapRenderer>().sortingOrder = 0;

            var dualGrid = gridObject.AddComponent<DualGridTilemap>();
            var configuration = new SerializedObject(dualGrid);
            configuration.FindProperty("logicalTilemap").objectReferenceValue = logical;
            configuration.FindProperty("generatedTilemap").objectReferenceValue = output;
            configuration.FindProperty("tileSet").objectReferenceValue = tileSet;
            configuration.FindProperty("automaticRefresh").boolValue = true;
            configuration.ApplyModifiedPropertiesWithoutUndo();
            dualGrid.AlignGeneratedTilemap();
            PopulateMaskGallery(logical, logicalTile);
            PopulateTerrainSample(logical, logicalTile);
            string reason;
            if (!dualGrid.Rebuild(out reason)) throw new InvalidOperationException(reason);

            CreateLabel("Dual-Grid: paint logic, generate transitions", new Vector3(13.5f, 16.35f, 0f),
                .14f, 42, new Color(.90f, .96f, 1f), FontStyle.Bold);
            CreateLabel("16-mask gallery", new Vector3(6.5f, 15.6f, 0f),
                .10f, 34, new Color(.58f, .82f, 1f), FontStyle.Bold);
            CreateLabel("soil base + generated grass edge", new Vector3(22.5f, 14.15f, 0f),
                .10f, 34, new Color(.58f, .82f, 1f), FontStyle.Bold);

            ExcludeDemoFromBuildSettings();
            EditorSceneManager.SaveScene(scene, DemoScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = gridObject;
            Debug.Log("Dual-Grid demo created at " + DemoScenePath);
        }

        public static void RenderDemoEvidence()
        {
            CreateOrRefreshDemo();
            RenderCurrentDemoEvidence(EvidencePath);
        }

        public static void RenderCurrentDemoEvidence(string evidencePath)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException(
                    "Dual-Grid evidence rendering requires a graphics device; run batch mode without -nographics.");
            var cameraObject = GameObject.Find("Demo Camera");
            if (cameraObject == null) throw new InvalidOperationException("Dual-Grid demo camera was not created.");
            var camera = cameraObject.GetComponent<Camera>();
            const int width = 1600;
            const int height = 900;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                var outputPath = Path.Combine(projectRoot,
                    evidencePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                Debug.Log("Dual-Grid evidence rendered to " + outputPath);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        public static void OpenManualPaintTest()
        {
            if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    DemoScenePath.Replace('/', Path.DirectorySeparatorChar))))
                CreateOrRefreshDemo();
            EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
            EditorApplication.delayCall += PrepareManualPaintWorkspace;
        }

        private static void PrepareManualPaintWorkspace()
        {
            var dualGrid = UnityEngine.Object.FindFirstObjectByType<DualGridTilemap>();
            if (dualGrid == null)
            {
                Debug.LogError("Dual-Grid manual paint test could not find the demo component.");
                return;
            }

            Selection.activeGameObject = dualGrid.gameObject;
            DualGridTilemapEditor.EnableManualPainting();
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Focus();
            }
            Debug.Log("FRUIT_DEFENSE_DUAL_GRID_MANUAL_PAINT_READY: left-drag paints, Shift+left-drag erases.");
        }

        private static void PopulateMaskGallery(Tilemap logical, TileBase logicalTile)
        {
            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var column = mask % 4;
                var row = mask / 4;
                var origin = new Vector3Int(column * 4, row * 4, 0);
                var vertex = origin + new Vector3Int(1, 1, 0);
                SetCorner(logical, logicalTile, vertex, (DualGridMask)mask, DualGridMask.NorthWest);
                SetCorner(logical, logicalTile, vertex, (DualGridMask)mask, DualGridMask.NorthEast);
                SetCorner(logical, logicalTile, vertex, (DualGridMask)mask, DualGridMask.SouthEast);
                SetCorner(logical, logicalTile, vertex, (DualGridMask)mask, DualGridMask.SouthWest);
                CreateLabel(mask.ToString("00") + " / " + Convert.ToString(mask, 2).PadLeft(4, '0'),
                    new Vector3(vertex.x, origin.y - .62f, 0f), .065f, 28,
                    new Color(.72f, .82f, .92f), FontStyle.Normal);
            }
        }

        private static string SelectPreferredTileSetPath()
        {
            var candidates = new[]
            {
                CartoonGrassTileSetPath,
                TileSetPath,
            };
            foreach (var path in candidates)
                if (AssetDatabase.LoadAssetAtPath<DualGridTileSet>(path) != null) return path;
            return TileSetPath;
        }

        private static void PopulateTerrainSample(Tilemap logical, TileBase logicalTile)
        {
            var rows = new[]
            {
                "..##......",
                ".####.....",
                ".#####.##.",
                "..###.###.",
                "..##..####",
                ".####.####.",
                ".##.#.###..",
                ".####..##..",
                "..##.......",
            };
            var origin = new Vector3Int(18, 2, 0);
            for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < rows[y].Length; x++)
                if (rows[y][x] == '#') logical.SetTile(origin + new Vector3Int(x, y, 0), logicalTile);
        }

        private static void PopulateSoilBase(Tilemap soil, TileBase soilTile)
        {
            var bounds = new BoundsInt(17, 1, 0, 14, 12, 1);
            foreach (var position in bounds.allPositionsWithin) soil.SetTile(position, soilTile);
        }

        private static void SetCorner(Tilemap logical, TileBase logicalTile, Vector3Int vertex,
            DualGridMask completeMask, DualGridMask corner)
        {
            if ((completeMask & corner) != 0)
                logical.SetTile(DualGridMaskUtility.LogicalCell(vertex, corner), logicalTile);
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Demo Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(13.5f, 7.6f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .055f, .085f, 1f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            return camera;
        }

        private static void CreateLabel(string text, Vector3 position, float characterSize,
            int fontSize, Color color, FontStyle style)
        {
            var labelObject = new GameObject("Label - " + text, typeof(TextMesh));
            labelObject.transform.position = position;
            var label = labelObject.GetComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static DualGridDebugTile LoadOrCreateDebugTile(string path, DualGridMask mask,
            Color fill, Color edge)
        {
            var tile = LoadOrCreateAsset<DualGridDebugTile>(path);
            tile.Configure(mask, fill, edge);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ExcludeDemoFromBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => !string.Equals(scene.path, DemoScenePath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (scenes.Length != EditorBuildSettings.scenes.Length) EditorBuildSettings.scenes = scenes;
        }
    }
}
