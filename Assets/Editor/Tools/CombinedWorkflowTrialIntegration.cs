using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using FruitDefense.Trials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class CombinedWorkflowTrialIntegration
    {
        private const string EvidenceRoot =
            "Builds/Evidence/combined-workflow-trial-20260730-155939-derived-composite";
        private const string CandidateRuntimeRoot =
            EvidenceRoot + "/candidates/ProtectedHybrid/Runtime32";
        private const string CandidateManifestPath =
            EvidenceRoot + "/candidates/ProtectedHybrid/manifest.json";
        private const string AssetRoot =
            "Assets/LayeredTerrain/Trials/CombinedWorkflowProtected";
        private const string RuntimeAssetRoot = AssetRoot + "/Runtime32";
        private const string TileSetPath = AssetRoot + "/ProtectedHybridTrialTileSet.asset";
        private const string TrialPalettePath = AssetRoot + "/ProtectedHybridTrialPalette.asset";
        private const string TrialSourceManifestPath = AssetRoot + "/TrialSourceManifest.json";
        private const string TrialPureSoilTexturePath = RuntimeAssetRoot + "/Mask-00.png";
        private const string TrialPureGrassTexturePath = RuntimeAssetRoot + "/Mask-15.png";
        private const string SourcePalettePath =
            "Assets/Battlefield/Terrain/OrchardDefaultTerrainPalette.asset";
        private const string ReleaseBattleScenePath = "Assets/Scenes/Battle.unity";
        public const string TrialBattleScenePath =
            "Assets/Scenes/CombinedWorkflowTrialBattle.unity";
        public const string TrialTerrainLabScenePath =
            "Assets/Scenes/CombinedWorkflowTrialTerrainLab.unity";
        private const string UnityEvidencePath = EvidenceRoot + "/evidence/unity-integration.json";
        private const int RuntimeTileSize = 32;

        [MenuItem("Fruit Defense/Validation/Open Combined Workflow Trial Battle")]
        public static void PrepareAndOpen()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            CopyCandidateIntoAssets(projectRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var trialTileSet = BuildTrialTileSet();
            var trialPalette = BuildTrialPalette(trialTileSet);
            BuildTrialBattleScene(trialPalette);
            BuildTrialTerrainLabScene(trialTileSet);
            WriteIntegrationEvidence(projectRoot);

            Debug.Log("FRUIT_DEFENSE_COMBINED_WORKFLOW_TRIAL_READY scene="
                + TrialBattleScenePath + " palette=" + TrialPalettePath
                + " tileset=" + TileSetPath
                + " candidate=ProtectedHybrid knownSeamSafe=false");

            if (!Application.isBatchMode)
                EditorApplication.delayCall += EnterTrialPlayMode;
        }

        [MenuItem("Fruit Defense/Validation/Open Combined Workflow Trial Terrain Lab")]
        public static void PrepareAndOpenTerrainLab()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            CopyCandidateIntoAssets(projectRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var trialTileSet = BuildTrialTileSet();
            BuildTrialPalette(trialTileSet);
            BuildTrialTerrainLabScene(trialTileSet);
            WriteIntegrationEvidence(projectRoot);

            Debug.Log("FRUIT_DEFENSE_COMBINED_WORKFLOW_TRIAL_LAB_READY scene="
                + TrialTerrainLabScenePath + " contour="
                + BattlefieldLayerIds.ContourStyles.Square
                + " preset=A-on-B candidate=ProtectedHybrid knownSeamSafe=false");

            if (!Application.isBatchMode)
                EditorApplication.delayCall += OpenPreparedTerrainLab;
        }

        private static void CopyCandidateIntoAssets(string projectRoot)
        {
            EnsureFolder(RuntimeAssetRoot);
            var sourceRoot = ToAbsolutePath(projectRoot, CandidateRuntimeRoot);
            if (!Directory.Exists(sourceRoot))
                throw new DirectoryNotFoundException(
                    "Combined workflow candidate Runtime32 folder is missing: " + sourceRoot);

            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var fileName = "Mask-" + mask.ToString("00") + ".png";
                var source = Path.Combine(sourceRoot, fileName);
                var destination = ToAbsolutePath(projectRoot, RuntimeAssetRoot + "/" + fileName);
                if (!File.Exists(source))
                    throw new FileNotFoundException(
                        "Combined workflow candidate mask is missing.", source);
                File.Copy(source, destination, true);
            }

            var sourceManifest = ToAbsolutePath(projectRoot, CandidateManifestPath);
            if (!File.Exists(sourceManifest))
                throw new FileNotFoundException(
                    "Combined workflow candidate manifest is missing.", sourceManifest);
            File.Copy(sourceManifest,
                ToAbsolutePath(projectRoot, TrialSourceManifestPath), true);
        }

        private static DualGridTileSet BuildTrialTileSet()
        {
            EnsureFolder(AssetRoot);
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(TileSetPath);
            if (tileSet == null)
            {
                tileSet = ScriptableObject.CreateInstance<DualGridTileSet>();
                AssetDatabase.CreateAsset(tileSet, TileSetPath);
            }

            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var texturePath = RuntimeAssetRoot + "/Mask-" + mask.ToString("00") + ".png";
                ConfigureTexture(texturePath);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                if (sprite == null)
                    throw new InvalidOperationException(
                        "Combined workflow Sprite import failed: " + texturePath);

                var tilePath = AssetRoot + "/Mask-" + mask.ToString("00") + ".asset";
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
                tileSet.SetTile((DualGridMask)mask, tile);
            }

            EditorUtility.SetDirty(tileSet);
            AssetDatabase.SaveAssets();
            if (!tileSet.Validate(out var reason))
                throw new InvalidOperationException(
                    "Combined workflow trial TileSet is invalid: " + reason);
            return tileSet;
        }

        private static BattlefieldTerrainPalette BuildTrialPalette(DualGridTileSet trialTileSet)
        {
            var source = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(SourcePalettePath);
            if (source == null)
                throw new InvalidOperationException(
                    "Source battlefield terrain palette is missing: " + SourcePalettePath);
            if (!source.Validate(out var sourceReason))
                throw new InvalidOperationException(
                    "Source battlefield terrain palette is invalid: " + sourceReason);

            var trial = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(TrialPalettePath);
            if (trial == null)
            {
                trial = UnityEngine.Object.Instantiate(source);
                trial.name = "ProtectedHybridTrialPalette";
                AssetDatabase.CreateAsset(trial, TrialPalettePath);
            }

            var replaced = 0;
            var pureGrassTexture = RequireAsset<Texture2D>(TrialPureGrassTexturePath);
            var pureSoilTexture = RequireAsset<Texture2D>(TrialPureSoilTexturePath);
            var surfaces = source.SurfaceBindings.Select(binding =>
            {
                if (binding == null) return null;
                var texture = string.Equals(binding.SurfaceId,
                        BattlefieldLayerIds.Surfaces.Grass, StringComparison.Ordinal)
                    ? pureGrassTexture
                    : string.Equals(binding.SurfaceId,
                        BattlefieldLayerIds.Surfaces.Soil, StringComparison.Ordinal)
                        ? pureSoilTexture : binding.BaseTexture;
                return new BattlefieldTerrainSurfaceBinding(binding.SurfaceId, texture);
            }).ToArray();
            var edges = new List<BattlefieldTerrainEdgeBinding>();
            foreach (var edge in source.EdgeBindings)
            {
                if (edge == null) continue;
                var isTrialTarget = string.Equals(edge.LandformSurfaceId,
                        BattlefieldLayerIds.Surfaces.Grass, StringComparison.Ordinal)
                    && string.Equals(edge.BaseSurfaceId,
                        BattlefieldLayerIds.Surfaces.Soil, StringComparison.Ordinal)
                    && string.Equals(edge.ContourStyleId,
                        BattlefieldLayerIds.ContourStyles.Square, StringComparison.Ordinal)
                    && string.Equals(edge.EdgeStyleId,
                        BattlefieldLayerIds.EdgeStyles.Refined, StringComparison.Ordinal);
                edges.Add(new BattlefieldTerrainEdgeBinding(edge.LandformSurfaceId,
                    edge.BaseSurfaceId, edge.ContourStyleId, edge.EdgeStyleId,
                    isTrialTarget ? trialTileSet : edge.TileSet));
                if (isTrialTarget) replaced++;
            }
            if (replaced != 1)
                throw new InvalidOperationException(
                    "Expected exactly one square grass-on-soil refined edge binding, found "
                    + replaced + ".");

            trial.ConfigureLayered(source.PaletteId, surfaces,
                source.LandformBindings, edges);
            EditorUtility.SetDirty(trial);
            AssetDatabase.SaveAssets();
            if (!trial.Validate(out var trialReason))
                throw new InvalidOperationException(
                    "Combined workflow trial palette is invalid: " + trialReason);
            return trial;
        }

        private static void BuildTrialBattleScene(BattlefieldTerrainPalette trialPalette)
        {
            var releaseScene = EditorSceneManager.OpenScene(
                ReleaseBattleScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(releaseScene, TrialBattleScenePath, true))
                throw new InvalidOperationException(
                    "Failed to copy the release Battle scene to the isolated trial scene.");

            var trialScene = EditorSceneManager.OpenScene(
                TrialBattleScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<FruitDefenseGame>();
            if (game == null)
                throw new InvalidOperationException(
                    "Trial Battle scene does not contain FruitDefenseGame.");
            game.ConfigureBattlefieldTerrain(new[] { trialPalette });
            EditorUtility.SetDirty(game);

            var trialBootstrap = game.GetComponent<CombinedWorkflowTrialBootstrap>();
            if (trialBootstrap == null)
                trialBootstrap = game.gameObject.AddComponent<CombinedWorkflowTrialBootstrap>();
            trialBootstrap.Configure(game, ProjectSetup.RequireReleaseRuntimeUiTheme());
            EditorUtility.SetDirty(trialBootstrap);

            var marker = GameObject.Find("TRIAL-ProtectedHybrid-FullComposite-KnownSeams");
            if (marker == null)
                marker = new GameObject("TRIAL-ProtectedHybrid-FullComposite-KnownSeams");
            marker.transform.SetParent(game.transform, false);

            EditorSceneManager.MarkSceneDirty(trialScene);
            if (!EditorSceneManager.SaveScene(trialScene))
                throw new InvalidOperationException("Failed to save the isolated trial Battle scene.");
            Selection.activeGameObject = game.gameObject;
            EditorGUIUtility.PingObject(game);
        }

        private static void BuildTrialTerrainLabScene(DualGridTileSet trialTileSet)
        {
            var activeScene = SceneManager.GetActiveScene();
            Scene sourceScene;
            if (activeScene.IsValid() && activeScene.isDirty
                && string.IsNullOrEmpty(activeScene.path))
            {
                sourceScene = EditorSceneManager.OpenScene(
                    LayeredTerrainArtSetup.AcceptanceScenePath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(sourceScene);
                if (!EditorSceneManager.CloseScene(activeScene, true))
                    throw new InvalidOperationException(
                        "Failed to close the task-owned unsaved trial scene.");
            }
            else
            {
                sourceScene = EditorSceneManager.OpenScene(
                    LayeredTerrainArtSetup.AcceptanceScenePath, OpenSceneMode.Single);
            }
            if (!EditorSceneManager.SaveScene(sourceScene, TrialTerrainLabScenePath, true))
                throw new InvalidOperationException(
                    "Failed to copy the layered terrain laboratory to the isolated trial scene.");

            var trialScene = EditorSceneManager.OpenScene(
                TrialTerrainLabScenePath, OpenSceneMode.Single);
            var renderer = UnityEngine.Object.FindFirstObjectByType<LayeredTerrainTilemap>();
            if (renderer == null)
                throw new InvalidOperationException(
                    "Trial terrain laboratory does not contain LayeredTerrainTilemap.");
            trialTileSet = RequireAsset<DualGridTileSet>(TileSetPath);

            ClearTerrainCanvas(renderer);
            renderer.gameObject.name =
                "TRIAL-ProtectedHybrid-Square-A-on-B-Known-Seams";
            var pureGrassTile = RequireTrialPureEndpoint(trialTileSet,
                DualGridMask.Full, "grass");
            var pureSoilTile = RequireTrialPureEndpoint(trialTileSet,
                DualGridMask.Empty, "soil");
            renderer.ConfigureBaseVisuals(pureGrassTile, pureSoilTile);
            renderer.ConfigureContourBindings(new[]
                {
                    new LayeredTerrainContourBinding(BattlefieldLayerIds.ContourStyles.Organic,
                        RequireAsset<DualGridTileSet>(
                            LayeredTerrainArtSetup.GrassLandformTileSetPath),
                        RequireAsset<DualGridTileSet>(
                            LayeredTerrainArtSetup.SoilLandformTileSetPath),
                        RequireAsset<DualGridTileSet>(
                            LayeredTerrainArtSetup.GrassOnSoilEdgeTileSetPath), null),
                    new LayeredTerrainContourBinding(BattlefieldLayerIds.ContourStyles.Square,
                        RequireAsset<DualGridTileSet>(
                            SquareTerrainArtProfile.GrassLandformTileSetPath),
                        RequireAsset<DualGridTileSet>(
                            SquareTerrainArtProfile.SoilLandformTileSetPath),
                        trialTileSet, null),
                }, BattlefieldLayerIds.ContourStyles.Square);
            renderer.ConfigureAuthoringPresentation("Grass [Protected Trial]",
                pureGrassTile.sprite,
                renderer.MaterialSwatch(LayeredTerrainMaterial.A), "Soil",
                pureSoilTile.sprite,
                renderer.MaterialSwatch(LayeredTerrainMaterial.B));
            string bindingReason;
            Require(renderer.TrySetContourStyle(
                BattlefieldLayerIds.ContourStyles.Organic, out bindingReason), bindingReason);
            Require(renderer.TrySetContourStyle(
                BattlefieldLayerIds.ContourStyles.Square, out bindingReason), bindingReason);
            Require(renderer.CanPaintPair(LayeredTerrainMaterial.A,
                LayeredTerrainMaterial.B, true, out bindingReason), bindingReason);
            Require(renderer.TryGetBasePreviewSprite(LayeredTerrainMaterial.A,
                    out var configuredPureGrass)
                && configuredPureGrass == pureGrassTile.sprite,
                "ProtectedHybrid pure grass must resolve from its own Mask-15 endpoint.");
            Require(renderer.TryGetBasePreviewSprite(LayeredTerrainMaterial.B,
                    out var configuredPureSoil)
                && configuredPureSoil == pureSoilTile.sprite,
                "ProtectedHybrid pure soil must resolve from its own Mask-00 endpoint.");
            PaintTrialTerrainLabBoard(renderer);

            var marker = GameObject.Find("TRIAL-DO-NOT-PUBLISH-KNOWN-SEAMS");
            if (marker == null)
                marker = new GameObject("TRIAL-DO-NOT-PUBLISH-KNOWN-SEAMS");
            marker.transform.SetParent(renderer.transform, false);

            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(trialScene);
            if (!EditorSceneManager.SaveScene(trialScene))
                throw new InvalidOperationException(
                    "Failed to save the isolated trial terrain laboratory scene.");
            Selection.activeGameObject = renderer.gameObject;
            EditorGUIUtility.PingObject(renderer);
        }

        private static void ClearTerrainCanvas(LayeredTerrainTilemap renderer)
        {
            foreach (var tilemap in new[]
                     {
                         renderer.BaseLogicalTilemap, renderer.LandformLogicalTilemap,
                         renderer.EdgeLogicalTilemap, renderer.BaseOutputTilemap,
                         renderer.LandformAOutputTilemap, renderer.LandformBOutputTilemap,
                         renderer.EdgeAOnBOutputTilemap, renderer.EdgeBOnAOutputTilemap,
                     })
            {
                if (tilemap != null) tilemap.ClearAllTiles();
            }
        }

        private static void PaintTrialTerrainLabBoard(LayeredTerrainTilemap renderer)
        {
            string reason;
            for (var y = 0; y < 14; y++)
            for (var x = 0; x < 8; x++)
                Require(renderer.PaintBase(new Vector3Int(x, y, 0),
                    LayeredTerrainMaterial.B, out reason), reason);

            var trialCells = new[]
            {
                new Vector3Int(1, 1, 0),
                new Vector3Int(1, 3, 0), new Vector3Int(2, 3, 0),
                new Vector3Int(3, 3, 0),
                new Vector3Int(5, 1, 0), new Vector3Int(5, 2, 0),
                new Vector3Int(6, 1, 0),
                new Vector3Int(1, 6, 0), new Vector3Int(2, 7, 0),
                new Vector3Int(4, 5, 0), new Vector3Int(5, 5, 0),
                new Vector3Int(6, 5, 0), new Vector3Int(4, 6, 0),
                new Vector3Int(6, 6, 0), new Vector3Int(4, 7, 0),
                new Vector3Int(5, 7, 0), new Vector3Int(6, 7, 0),
                new Vector3Int(1, 10, 0), new Vector3Int(2, 10, 0),
                new Vector3Int(3, 10, 0), new Vector3Int(3, 11, 0),
                new Vector3Int(4, 11, 0), new Vector3Int(4, 12, 0),
                new Vector3Int(5, 12, 0), new Vector3Int(6, 12, 0),
            };
            foreach (var cell in trialCells)
                Require(renderer.PaintPair(cell, LayeredTerrainMaterial.A,
                    LayeredTerrainMaterial.B, true, out reason), reason);
            Require(renderer.Rebuild(out reason), reason);
        }

        [MenuItem("Fruit Defense/Validation/Open Prepared Combined Workflow Trial Terrain Lab")]
        public static void OpenPreparedTerrainLab()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenPreparedTerrainLab;
                return;
            }
            EditorApplication.isPlaying = false;
            EditorSceneManager.OpenScene(TrialTerrainLabScenePath, OpenSceneMode.Single);
            var renderer = UnityEngine.Object.FindFirstObjectByType<LayeredTerrainTilemap>();
            if (renderer == null)
                throw new InvalidOperationException(
                    "Prepared trial terrain laboratory is missing its renderer.");
            Selection.activeGameObject = renderer.gameObject;
            LayeredTerrainPainterWindow.Open(renderer);
            SceneView.RepaintAll();
            Debug.Log("FRUIT_DEFENSE_COMBINED_WORKFLOW_TRIAL_LAB_PANEL_RESTORED");
        }

        [InitializeOnLoadMethod]
        private static void SchedulePreparedTerrainLabPanelRestore()
        {
            EditorApplication.delayCall += RestorePreparedTerrainLabPanel;
        }

        private static void RestorePreparedTerrainLabPanel()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RestorePreparedTerrainLabPanel;
                return;
            }
            if (!string.Equals(SceneManager.GetActiveScene().path, TrialTerrainLabScenePath,
                    StringComparison.Ordinal)) return;
            var renderer = UnityEngine.Object.FindFirstObjectByType<LayeredTerrainTilemap>();
            if (renderer == null) return;
            Selection.activeGameObject = renderer.gameObject;
            LayeredTerrainPainterWindow.Open(renderer);
            SceneView.RepaintAll();
            Debug.Log("FRUIT_DEFENSE_COMBINED_WORKFLOW_TRIAL_LAB_PANEL_RESTORED");
        }

        private static void EnterTrialPlayMode()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnterTrialPlayMode;
                return;
            }
            EditorSceneManager.OpenScene(TrialBattleScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<FruitDefenseGame>();
            if (game != null) Selection.activeGameObject = game.gameObject;
            EditorApplication.isPlaying = true;
        }

        private static void ConfigureTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Texture importer is unavailable: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = RuntimeTileSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = RuntimeTileSize;
            if (AssetDatabase.WriteImportSettingsIfDirty(path))
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Tile RequireTrialPureEndpoint(DualGridTileSet tileSet,
            DualGridMask mask, string label)
        {
            var tile = tileSet == null ? null : tileSet.GetTile(mask) as Tile;
            if (tile == null || tile.sprite == null || tile.sprite.texture == null)
                throw new InvalidOperationException("ProtectedHybrid " + label
                    + " pure endpoint is missing at mask " + (int)mask + ".");
            return tile;
        }

        private static void WriteIntegrationEvidence(string projectRoot)
        {
            var path = ToAbsolutePath(projectRoot, UnityEvidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sourceManifest = ToAbsolutePath(projectRoot, CandidateManifestPath);
            var json = "{\n"
                + "  \"status\": \"ready-for-manual-unity-review\",\n"
                + "  \"candidate\": \"ProtectedHybrid\",\n"
                + "  \"integrationMode\": \"isolated trial scene; full-composite candidate substituted for square grass-on-soil refined edge\",\n"
                + "  \"seamSafetyClaimed\": false,\n"
                + "  \"sourceManifestSha256\": \"" + Sha256(sourceManifest) + "\",\n"
                + "  \"tileSet\": \"" + TileSetPath + "\",\n"
                + "  \"palette\": \"" + TrialPalettePath + "\",\n"
                + "  \"scene\": \"" + TrialBattleScenePath + "\",\n"
                + "  \"terrainLabScene\": \"" + TrialTerrainLabScenePath + "\",\n"
                + "  \"releaseSceneModified\": false,\n"
                + "  \"sourcePaletteModified\": false,\n"
                + "  \"humanVisualReview\": \"pending\"\n"
                + "}\n";
            File.WriteAllText(path, json);
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
                return string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static string ToAbsolutePath(string projectRoot, string projectRelativePath)
        {
            return Path.Combine(projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException("Required trial lab asset is missing: " + path);
            return asset;
        }

        private static void Require(bool condition, string reason)
        {
            if (!condition)
                throw new InvalidOperationException(reason ?? "Trial terrain lab operation failed.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            if (separator <= 0)
                throw new ArgumentException("Invalid asset folder: " + path);
            var parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
