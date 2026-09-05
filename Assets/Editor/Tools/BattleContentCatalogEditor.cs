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
        public const string OutgameAuthoringAssetPath =
            "Assets/Content/BundledOutgameContent.asset";
        public const string ManifestAuthoringAssetPath = "Assets/Content/GameContentManifest.asset";
        public const string BundledJsonPath = "Assets/Resources/Content/battle-content-bundled.v3.json";
        public const string OutgameJsonPath =
            "Assets/Resources/Content/outgame-content-bundled.v1.json";
        public const string ManifestJsonPath =
            "Assets/Resources/Content/game-content-manifest.v2.json";

        [MenuItem("Fruit Defense/Content/Export Game Content Bundle")]
        public static void ExportGameContentBundle()
        {
            var asset = RequireAuthoringAsset();
            var outgameAsset = RequireOutgameAuthoringAsset();
            var manifestAsset = RequireManifestAuthoringAsset();
            var copy = BattleContentJson.DeepCopy(asset.Catalog);
            var outgame = OutgameContentJson.DeepCopy(outgameAsset.Catalog);
            var manifest = GameContentManifestJson.DeepCopy(manifestAsset.Manifest);
            var battleAbsolutePath = AbsoluteProjectPath(BundledJsonPath);
            var outgameAbsolutePath = AbsoluteProjectPath(OutgameJsonPath);
            var manifestAbsolutePath = AbsoluteProjectPath(ManifestJsonPath);
            ContentValidationResult validation;
            if (!GameContentBundleExporter.TryWrite(copy, outgame, manifest,
                    battleAbsolutePath, outgameAbsolutePath, manifestAbsolutePath,
                    out validation))
                ThrowIfInvalid(validation, "Game-content bundle export rejected");
            AssetDatabase.ImportAsset(BundledJsonPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(OutgameJsonPath,
                ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(ManifestJsonPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Exported game-content bundle: " + ManifestJsonPath + " + "
                + BundledJsonPath + " + " + OutgameJsonPath + ".");
        }

        [MenuItem("Fruit Defense/Content/Validate Game Content Bundle")]
        public static void ValidateBundledCatalog()
        {
            var asset = RequireAuthoringAsset();
            var outgameAsset = RequireOutgameAuthoringAsset();
            var manifestAsset = RequireManifestAuthoringAsset();
            var battleAbsolutePath = AbsoluteProjectPath(BundledJsonPath);
            var outgameAbsolutePath = AbsoluteProjectPath(OutgameJsonPath);
            var manifestAbsolutePath = AbsoluteProjectPath(ManifestJsonPath);
            if (!File.Exists(battleAbsolutePath))
                throw new FileNotFoundException("Bundled catalog JSON is missing.", battleAbsolutePath);
            if (!File.Exists(outgameAbsolutePath))
                throw new FileNotFoundException("Bundled outgame catalog JSON is missing.",
                    outgameAbsolutePath);
            if (!File.Exists(manifestAbsolutePath))
                throw new FileNotFoundException("Game-content manifest JSON is missing.", manifestAbsolutePath);
            var json = File.ReadAllText(battleAbsolutePath, Encoding.UTF8);
            var outgameJson = File.ReadAllText(outgameAbsolutePath, Encoding.UTF8);
            var manifestJson = File.ReadAllText(manifestAbsolutePath, Encoding.UTF8);
            BattleContentCatalogSmoke.Run(asset.Catalog, json);
            OutgameContentCatalogSmoke.Run(outgameAsset.Catalog, outgameJson,
                asset.Catalog, manifestAsset.Manifest);
            GameContentManifestSmoke.Run(manifestAsset.Manifest, manifestJson,
                asset.Catalog, outgameAsset.Catalog);
            Debug.Log("Game content validation passed: " + asset.Catalog.header.catalogId
                + "@" + asset.Catalog.header.contentVersion + " + "
                + outgameAsset.Catalog.header.catalogId + "@"
                + outgameAsset.Catalog.header.contentVersion);
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

        private static OutgameContentCatalogAsset RequireOutgameAuthoringAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<OutgameContentCatalogAsset>(
                OutgameAuthoringAssetPath);
            if (asset == null) throw new InvalidOperationException(
                "Missing outgame-content authoring asset at "
                + OutgameAuthoringAssetPath + ".");
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
