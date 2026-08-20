using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.UI;
using UnityEditor;

namespace FruitDefense.Editor
{
    public static class RuntimeUiArtSetRegistry
    {
        public const string ArtSetRoot = "Assets/UI/Art/Sets";
        public const string RuntimeArtRoot = "Assets/UI/Art/Runtime";
        public const string SourceArtRoot = "Assets/UI/Art/Sources";
        public const string ThemeRoot = "Assets/UI/Theme";
        public const string ReleaseThemePath =
            "Assets/UI/Theme/ReleaseRuntimeUiTheme.asset";
        public const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        private static readonly string[] ReleaseScenePaths =
        {
            BootstrapScenePath,
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/Battle.unity",
            "Assets/Scenes/Settlement.unity",
        };

        public static IReadOnlyList<string> ReleaseScenes => ReleaseScenePaths;

        public static RuntimeUiTheme LoadReleaseTheme()
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeUiTheme>(ReleaseThemePath);
        }

        public static IReadOnlyList<RuntimeUiArtSet> DiscoverProductionSets()
        {
            return AssetDatabase.FindAssets("t:RuntimeUiArtSet", new[] { ArtSetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RuntimeUiArtSet>)
                .Where(candidate => candidate != null)
                .OrderBy(candidate => candidate.SetId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Revision, StringComparer.Ordinal)
                .ThenBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal)
                .ToArray();
        }

        public static string RuntimeDirectory(RuntimeUiArtSet artSet)
        {
            if (artSet == null) throw new ArgumentNullException(nameof(artSet));
            return RuntimeArtRoot + "/" + artSet.SetId;
        }

        public static string SourceDirectory(RuntimeUiArtSet artSet)
        {
            if (artSet == null) throw new ArgumentNullException(nameof(artSet));
            return SourceArtRoot + "/" + artSet.SetId;
        }

        public static string ManifestPath(RuntimeUiArtSet artSet)
        {
            return SourceDirectory(artSet) + "/art_manifest.json";
        }

        public static bool IsProductionSet(RuntimeUiArtSet artSet)
        {
            if (artSet == null) return false;
            var path = Normalize(AssetDatabase.GetAssetPath(artSet));
            return path.StartsWith(ArtSetRoot + "/", StringComparison.Ordinal);
        }

        public static bool IsRuntimeAssetForSet(string assetPath, RuntimeUiArtSet artSet)
        {
            if (artSet == null || string.IsNullOrEmpty(assetPath)) return false;
            var path = Normalize(assetPath);
            return path.StartsWith(RuntimeDirectory(artSet) + "/", StringComparison.Ordinal);
        }

        public static string Normalize(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath)
                ? string.Empty
                : assetPath.Replace('\\', '/');
        }
    }
}
