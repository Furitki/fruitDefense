using System;
using System.IO;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public static class DualGridPixelTerrainWizardSmoke
    {
        [Serializable]
        private sealed class TransitionEvidence
        {
            public string bakerVersion;
            public int outlinePixels;
            public int textureGuidancePixels;
            public bool solidOutlineActive;
            public bool sourceGuidanceAvailable;
            public int textureGuidedChangedPixels;
            public int oppositeCornerComponentCount05;
            public int oppositeCornerComponentCount10;
            public int invalidTopologyMasks;
            public string result;
        }

        public static void Validate()
        {
            var grass = AssetDatabase.LoadAssetAtPath<Texture2D>(
                DualGridPixelTileSetGenerator.GrassSourcePath);
            var soil = AssetDatabase.LoadAssetAtPath<Texture2D>(
                DualGridPixelTileSetGenerator.SoilSourcePath);
            Assert(grass != null && soil != null, "wizard smoke sources exist");

            ValidateProfileModes(grass, soil);
            ValidateImagegenContract();
            ValidateEvidenceOwnership(grass, soil);
            ValidateTransitionEvidence();
            ValidateSelectedMapApplication();
            Debug.Log("FRUIT_DEFENSE_DUAL_GRID_PIXEL_WIZARD_SMOKE_OK");
        }

        private static void ValidateTransitionEvidence()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var validatedSamples = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:DualGridPixelTerrainProfile"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(path);
                if (profile == null || profile.TerrainId != "PixelGrass"
                    && profile.TerrainId != "StoneFloor")
                    continue;

                Assert(profile.OutlinePixels == 0 && profile.TextureGuidancePixels > 0,
                    profile.TerrainId + " sample uses a guided connection-safe edge");
                var evidencePath = Path.GetFullPath(Path.Combine(projectRoot,
                    DualGridPixelTileSetGenerator.GetValidationEvidencePath(profile)
                        .Replace('/', Path.DirectorySeparatorChar)));
                Assert(File.Exists(evidencePath),
                    profile.TerrainId + " transition evidence exists");
                var evidence = JsonUtility.FromJson<TransitionEvidence>(
                    File.ReadAllText(evidencePath));
                Assert(evidence != null && evidence.result == "pass"
                    && evidence.bakerVersion.Contains("texture-guided")
                    && evidence.outlinePixels == 0
                    && !evidence.solidOutlineActive
                    && evidence.textureGuidancePixels == profile.TextureGuidancePixels
                    && evidence.sourceGuidanceAvailable
                    && evidence.textureGuidedChangedPixels > 0
                    && evidence.oppositeCornerComponentCount05 == 2
                    && evidence.oppositeCornerComponentCount10 == 2
                    && evidence.invalidTopologyMasks == 0,
                    profile.TerrainId + " evidence proves guided seam-safe output");
                validatedSamples++;
            }
            Assert(validatedSamples == 2,
                "PixelGrass and StoneFloor transition evidence is validated");
        }

        private static void ValidateProfileModes(Texture2D grass, Texture2D soil)
        {
            var single = ScriptableObject.CreateInstance<DualGridPixelTerrainProfile>();
            var dual = ScriptableObject.CreateInstance<DualGridPixelTerrainProfile>();
            try
            {
                single.Configure(DualGridPixelSourceOrigin.Manual,
                    DualGridPixelSourceLayout.Single, grass, null, "WizardSingleSmoke",
                    "Assets/DualGridDemo/WizardSingleSmoke/Generated", 32, 0, 2,
                    new Color32(65, 42, 27, 255), 17, 2);
                Assert(single.Validate(out var singleReason),
                    "single-source profile validates: " + singleReason);
                Assert(single.SoilTexture == grass && single.AuthoredSoilTexture == null,
                    "single-source profile reuses grass as effective soil");
                Assert(single.SourceOrigin == DualGridPixelSourceOrigin.Manual,
                    "single-source profile records manual provenance");
                Assert(single.OutlinePixels == 0 && single.TextureGuidancePixels == 2,
                    "single-source profile accepts connection-safe guided edges");

                dual.Configure(DualGridPixelSourceOrigin.Manual,
                    DualGridPixelSourceLayout.GrassAndSoil, grass, soil,
                    "WizardDualSmoke", "Assets/DualGridDemo/WizardDualSmoke/Generated",
                    32, 1, 2, new Color32(65, 42, 27, 255), 19, 0);
                Assert(dual.Validate(out var dualReason),
                    "two-source profile validates: " + dualReason);
                Assert(dual.SoilTexture == soil,
                    "two-source profile retains independent soil source");
                Assert(dual.OutlinePixels == 1 && dual.TextureGuidancePixels == 0,
                    "two-source profile retains explicit outline and unguided options");
                Assert(DualGridPixelTerrainWizard.ValidateWizardSettings(
                        "Assets/DualGridTerrain/WizardSmoke", "WizardSmoke", 32,
                        0, 2, 2, out var wizardReason),
                    "wizard accepts zero outline and bounded guidance: " + wizardReason);
                Assert(!DualGridPixelTerrainWizard.ValidateWizardSettings(
                        "Assets/DualGridTerrain/WizardSmoke", "WizardSmoke", 32,
                        0, 2, 5, out _),
                    "wizard rejects out-of-range texture guidance");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(single);
                UnityEngine.Object.DestroyImmediate(dual);
            }
        }

        private static void ValidateImagegenContract()
        {
            const string root = "Assets/DualGridTerrain/WizardRequestSmoke";
            var single = DualGridPixelTerrainWizard.CreateImagegenRequest(root,
                "WizardRequestSmoke", DualGridPixelSourceLayout.Single, 32,
                "bright readable moss pixels", string.Empty);
            Assert(single.schemaVersion == 1 && single.requiredSkill == "imagegen",
                "imagegen request names its schema and required skill");
            Assert(!single.scriptDrawingAllowed,
                "imagegen request forbids scripted source drawing");
            Assert(single.sources != null && single.sources.Length == 1,
                "single-source imagegen request has one target");
            Assert(single.sources[0].targetAssetPath.EndsWith(
                    "/Sources/WizardRequestSmoke-Source.png", StringComparison.Ordinal),
                "single-source imagegen target is deterministic");

            var dual = DualGridPixelTerrainWizard.CreateImagegenRequest(root,
                "WizardRequestSmoke", DualGridPixelSourceLayout.GrassAndSoil, 32,
                "bright readable grass pixels", "warm compact soil pixels");
            Assert(dual.sources != null && dual.sources.Length == 2,
                "two-source imagegen request has grass and soil targets");
            Assert(dual.sources[0].role == "grass" && dual.sources[1].role == "soil",
                "two-source imagegen roles are explicit");
            var serialized = JsonUtility.ToJson(dual);
            Assert(serialized.Contains("\"requiredSkill\":\"imagegen\"")
                && serialized.Contains("\"scriptDrawingAllowed\":false"),
                "serialized imagegen request preserves safety constraints");
            var instruction = DualGridPixelTerrainWizard.BuildCodexImagegenInstruction(
                DualGridPixelTerrainWizard.GetImagegenRequestAssetPath(
                    root, "WizardRequestSmoke"));
            Assert(instruction.Contains("imagegen") && instruction.Contains("严禁"),
                "Codex handoff repeats the imagegen-only constraint");
        }

        private static void ValidateEvidenceOwnership(Texture2D grass, Texture2D soil)
        {
            var first = ScriptableObject.CreateInstance<DualGridPixelTerrainProfile>();
            var second = ScriptableObject.CreateInstance<DualGridPixelTerrainProfile>();
            try
            {
                first.Configure(DualGridPixelSourceOrigin.Manual,
                    DualGridPixelSourceLayout.GrassAndSoil, grass, soil, "ForestFloor",
                    "Assets/DualGridTerrain/ForestFloor/Generated", 32, 1, 2,
                    new Color32(65, 42, 27, 255), 1);
                second.Configure(DualGridPixelSourceOrigin.Manual,
                    DualGridPixelSourceLayout.GrassAndSoil, grass, soil, "MoonDust",
                    "Assets/DualGridTerrain/MoonDust/Generated", 32, 1, 2,
                    new Color32(65, 42, 27, 255), 2);
                Assert(DualGridPixelTileSetGenerator.GetValidationEvidencePath(first)
                        != DualGridPixelTileSetGenerator.GetValidationEvidencePath(second),
                    "terrain reports have independent paths");
                Assert(DualGridPixelTileSetGenerator.GetAtlasEvidencePath(first)
                        != DualGridPixelTileSetGenerator.GetAtlasEvidencePath(second),
                    "terrain atlases have independent paths");
                Assert(DualGridPixelTileSetGenerator.ToEvidenceStem("PixelGrass")
                        == "pixel-grass",
                    "PixelGrass keeps its legacy evidence stem");
                Assert(DualGridPixelTileSetGenerator.GetAtlasEvidencePath(
                        AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(
                            DualGridPixelTileSetGenerator.DefaultProfilePath))
                        == DualGridPixelTileSetGenerator.AtlasEvidencePath,
                    "PixelGrass keeps its legacy atlas path");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        private static void ValidateSelectedMapApplication()
        {
            var profile = AssetDatabase.LoadAssetAtPath<DualGridPixelTerrainProfile>(
                DualGridPixelTileSetGenerator.DefaultProfilePath);
            Assert(profile != null, "PixelGrass profile exists for apply smoke");
            var previewScene = EditorSceneManager.NewPreviewScene();
            Tile logicalTile = null;
            try
            {
                var root = new GameObject("Pixel Wizard Apply Smoke", typeof(Grid));
                var logicalObject = new GameObject("Logical", typeof(Tilemap),
                    typeof(TilemapRenderer));
                var generatedObject = new GameObject("Generated", typeof(Tilemap),
                    typeof(TilemapRenderer));
                logicalObject.transform.SetParent(root.transform, false);
                generatedObject.transform.SetParent(root.transform, false);
                SceneManager.MoveGameObjectToScene(root, previewScene);

                var logical = logicalObject.GetComponent<Tilemap>();
                var generated = generatedObject.GetComponent<Tilemap>();
                var map = root.AddComponent<DualGridTilemap>();
                map.Configure(logical, generated, null, true);
                logicalTile = ScriptableObject.CreateInstance<Tile>();
                logicalTile.hideFlags = HideFlags.HideAndDontSave;
                var occupiedCell = new Vector3Int(2, 3, 0);
                logical.SetTile(occupiedCell, logicalTile);

                Assert(DualGridPixelTerrainWizard.ApplyToMap(profile, map, out var reason),
                    "wizard applies selected map: " + reason);
                Assert(map.TileSet == AssetDatabase.LoadAssetAtPath<DualGridTileSet>(
                        DualGridPixelTileSetGenerator.GetTileSetAssetPath(profile)),
                    "selected map receives generated TileSet");
                Assert(logical.GetTile(occupiedCell) == logicalTile,
                    "selected-map application preserves logical authoring cells");
                Assert(generated.GetUsedTilesCount() > 0 && map.HasExpectedAlignment(),
                    "selected-map application aligns and rebuilds generated output");
            }
            finally
            {
                if (logicalTile != null) UnityEngine.Object.DestroyImmediate(logicalTile);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Dual-Grid pixel wizard smoke failed: " + message);
        }
    }
}
