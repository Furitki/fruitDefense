using System;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public sealed class TempArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/TempArt/")) return;
            var seamless = assetPath.IndexOf("-seamless-", StringComparison.OrdinalIgnoreCase) >= 0;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !seamless;
            importer.mipmapEnabled = seamless;
            importer.wrapMode = seamless ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = seamless ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.anisoLevel = seamless ? 2 : 1;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }

        [MenuItem("Fruit Defense/Art/Reimport Seamless Temp Textures")]
        public static void ReimportSeamlessTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D",
                new[] { "Assets/Resources/TempArt" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("-seamless-", StringComparison.OrdinalIgnoreCase) < 0) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("FRUIT_DEFENSE_SEAMLESS_TEXTURE_IMPORT_OK");
        }
    }
}
