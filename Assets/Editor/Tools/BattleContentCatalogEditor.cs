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
        public const string BundledJsonPath = "Assets/Resources/Content/battle-content-bundled.v1.json";

        [MenuItem("Fruit Defense/Content/Rebuild Bundled Catalog")]
        public static void CreateOrRefreshBundledCatalog()
        {
            EnsureAssetDirectory(AuthoringAssetPath);
            var asset = AssetDatabase.LoadAssetAtPath<BattleContentCatalogAsset>(AuthoringAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BattleContentCatalogAsset>();
                AssetDatabase.CreateAsset(asset, AuthoringAssetPath);
            }
            asset.Catalog = BundledBattleContentFactory.Create();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            ExportBundledCatalog();
            Debug.Log("Rebuilt battle content authoring asset and JSON.");
        }

        [MenuItem("Fruit Defense/Content/Export Bundled Catalog")]
        public static void ExportBundledCatalog()
        {
            var asset = RequireAuthoringAsset();
            var copy = BattleContentJson.DeepCopy(asset.Catalog);
            var validation = BattleContentValidator.ValidateBundledBaseline(copy);
            ThrowIfInvalid(validation, "Bundled catalog export rejected");

            var bytes = BattleContentJson.SerializeCanonicalUtf8(copy);
            var absolutePath = AbsoluteProjectPath(BundledJsonPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, bytes);
            AssetDatabase.ImportAsset(BundledJsonPath, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Exported battle content JSON: " + BundledJsonPath + " (" + bytes.Length + " bytes)");
        }

        public static void ValidateBundledCatalog()
        {
            var asset = RequireAuthoringAsset();
            var absolutePath = AbsoluteProjectPath(BundledJsonPath);
            if (!File.Exists(absolutePath)) throw new FileNotFoundException("Bundled catalog JSON is missing.", absolutePath);
            var json = File.ReadAllText(absolutePath, Encoding.UTF8);
            BattleContentCatalogSmoke.Run(asset.Catalog, json);
            Debug.Log("Battle content validation passed: " + asset.Catalog.header.catalogId + "@" + asset.Catalog.header.contentVersion);
        }

        public static void ExportAndValidateBundledCatalog()
        {
            ExportBundledCatalog();
            ValidateBundledCatalog();
        }

        private static BattleContentCatalogAsset RequireAuthoringAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BattleContentCatalogAsset>(AuthoringAssetPath);
            if (asset == null) throw new InvalidOperationException("Missing authoring asset at " + AuthoringAssetPath
                + ". Run Rebuild Bundled Catalog first.");
            return asset;
        }

        private static void ThrowIfInvalid(ContentValidationResult validation, string prefix)
        {
            if (validation.IsValid) return;
            throw new InvalidOperationException(prefix + ":\n" + string.Join("\n",
                validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            var directory = Path.GetDirectoryName(AbsoluteProjectPath(assetPath));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string AbsoluteProjectPath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
