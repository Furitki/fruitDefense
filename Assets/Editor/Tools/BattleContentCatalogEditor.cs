using System;
using System.IO;
using System.Linq;
using System.Text;
using FruitDefense.Content;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleContentCatalogEditor
    {
        public const string AuthoringAssetPath = "Assets/Content/BundledBattleContent.asset";
        public const string ManifestAuthoringAssetPath = "Assets/Content/GameContentManifest.asset";
        public const string BundledJsonPath = "Assets/Resources/Content/battle-content-bundled.v3.json";
        public const string ManifestJsonPath = "Assets/Resources/Content/game-content-manifest.v1.json";

        [MenuItem("Fruit Defense/Content/Export Game Content Bundle")]
        public static void ExportGameContentBundle()
        {
            var asset = RequireAuthoringAsset();
            var manifestAsset = RequireManifestAuthoringAsset();
            var copy = BattleContentJson.DeepCopy(asset.Catalog);
            var manifest = GameContentManifestJson.DeepCopy(manifestAsset.Manifest);
            var validation = BattleContentValidator.ValidateBundledBaseline(copy);
            ThrowIfInvalid(validation, "Bundled catalog export rejected");
            ThrowIfInvalid(GameContentManifestValidator.Validate(manifest, copy,
                BundledLevelCatalogIds.Catalog), "Game-content manifest export rejected");

            var battleBytes = BattleContentJson.SerializeCanonicalUtf8(copy);
            var manifestBytes = GameContentManifestJson.SerializeCanonicalUtf8(manifest);
            var battleAbsolutePath = AbsoluteProjectPath(BundledJsonPath);
            var manifestAbsolutePath = AbsoluteProjectPath(ManifestJsonPath);
            Directory.CreateDirectory(Path.GetDirectoryName(battleAbsolutePath));
            File.WriteAllBytes(battleAbsolutePath, battleBytes);
            File.WriteAllBytes(manifestAbsolutePath, manifestBytes);
            AssetDatabase.ImportAsset(BundledJsonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(ManifestJsonPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Exported game-content bundle: " + ManifestJsonPath + " + "
                + BundledJsonPath + " (" + (manifestBytes.Length + battleBytes.Length) + " bytes)");
        }

        [MenuItem("Fruit Defense/Content/Validate Game Content Bundle")]
        public static void ValidateBundledCatalog()
        {
            var asset = RequireAuthoringAsset();
            var manifestAsset = RequireManifestAuthoringAsset();
            var battleAbsolutePath = AbsoluteProjectPath(BundledJsonPath);
            var manifestAbsolutePath = AbsoluteProjectPath(ManifestJsonPath);
            if (!File.Exists(battleAbsolutePath))
                throw new FileNotFoundException("Bundled catalog JSON is missing.", battleAbsolutePath);
            if (!File.Exists(manifestAbsolutePath))
                throw new FileNotFoundException("Game-content manifest JSON is missing.", manifestAbsolutePath);
            var json = File.ReadAllText(battleAbsolutePath, Encoding.UTF8);
            var manifestJson = File.ReadAllText(manifestAbsolutePath, Encoding.UTF8);
            BattleContentCatalogSmoke.Run(asset.Catalog, json);
            GameContentManifestSmoke.Run(manifestAsset.Manifest, manifestJson, asset.Catalog);
            Debug.Log("Battle content validation passed: " + asset.Catalog.header.catalogId + "@" + asset.Catalog.header.contentVersion);
        }

        public static void ExportAndValidateGameContentBundle()
        {
            ExportGameContentBundle();
            ValidateBundledCatalog();
        }

        private static BattleContentCatalogAsset RequireAuthoringAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BattleContentCatalogAsset>(AuthoringAssetPath);
            if (asset == null) throw new InvalidOperationException(
                "Missing battle-content authoring asset at " + AuthoringAssetPath + ".");
            return asset;
        }

        private static GameContentManifestAsset RequireManifestAuthoringAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameContentManifestAsset>(
                ManifestAuthoringAssetPath);
            if (asset == null) throw new InvalidOperationException(
                "Missing game-content manifest authoring asset at " + ManifestAuthoringAssetPath + ".");
            return asset;
        }

        private static void ThrowIfInvalid(ContentValidationResult validation, string prefix)
        {
            if (validation.IsValid) return;
            throw new InvalidOperationException(prefix + ":\n" + string.Join("\n",
                validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static string AbsoluteProjectPath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
