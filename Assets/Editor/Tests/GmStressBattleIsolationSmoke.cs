using System;
using System.IO;
using System.Linq;
using FruitDefense.App.Services;
using FruitDefense.Content;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmStressBattleIsolationSmoke
    {
        private const string NormalWebBuildSource = "Assets/Editor/Tools/WebBuild.cs";
        private static readonly string[] SerializedResourceExtensions =
        {
            ".asset", ".json", ".prefab", ".unity", ".txt", ".bytes",
        };

        public static void Validate(string gmLevelId, string gmMapId,
            string developmentWebBuildDirectory)
        {
            RequireStableGmIdentity(gmLevelId, nameof(gmLevelId));
            RequireStableGmIdentity(gmMapId, nameof(gmMapId));
            if (string.IsNullOrWhiteSpace(developmentWebBuildDirectory))
                Fail("GM Development WebGL output directory is required.");

            ValidateReleasedCatalogs(gmLevelId, gmMapId);
            ValidatePublicationManifests(gmLevelId, gmMapId);
            ValidateProductionResources(gmLevelId, gmMapId);
            ValidateDevelopmentCompilationBoundary();
            ValidateProfileSelectionBoundary(gmLevelId);
            ValidateNormalWebBuildBoundary(gmLevelId, gmMapId,
                developmentWebBuildDirectory);
            Debug.Log("FRUIT_DEFENSE_GM_STRESS_ISOLATION_OK");
        }

        private static void ValidateReleasedCatalogs(string gmLevelId, string gmMapId)
        {
            var bundled = BundledLevelCatalogFactory.CreateBundledSource();
            Assert(bundled.Levels.All(level => level != null
                    && !string.Equals(level.LevelId, gmLevelId, StringComparison.Ordinal)),
                "GM level is absent from the bundled level source");
            Assert(bundled.Maps.All(map => map != null
                    && !string.Equals(map.MapId, gmMapId, StringComparison.Ordinal)),
                "GM map is absent from the bundled map source");

            var production = BundledLevelCatalogFactory.CreateCompiled();
            Assert(production.PlayableLevels.All(level => level != null
                    && !string.Equals(level.LevelId, gmLevelId, StringComparison.Ordinal)),
                "GM level is absent from production PlayableLevels");
            var resolution = production.Resolve(gmLevelId);
            Assert(!resolution.Succeeded && resolution.Value == null,
                "production level resolution rejects the GM identity");

            var published = PublishedBattlefieldMapCatalog.LoadGenerated();
            if (published == null) return;
            Assert(published.Entries.All(entry => entry != null
                    && !string.Equals(entry.LevelId, gmLevelId, StringComparison.Ordinal)
                    && (entry.Map == null
                        || !string.Equals(entry.Map.MapId, gmMapId,
                            StringComparison.Ordinal))),
                "GM level/map is absent from the generated publication catalog");
        }

        private static void ValidatePublicationManifests(string gmLevelId, string gmMapId)
        {
            var manifestGuids = AssetDatabase.FindAssets(
                "t:BattlefieldMapPublicationManifest", new[] { "Assets" });
            foreach (var guid in manifestGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var manifest = AssetDatabase.LoadAssetAtPath<BattlefieldMapPublicationManifest>(
                    path);
                Assert(manifest != null, "publication manifest loads: " + path);
                Assert(manifest.Entries.All(entry => entry != null
                        && !string.Equals(entry.LevelId, gmLevelId,
                            StringComparison.Ordinal)
                        && (entry.Map == null
                            || !string.Equals(entry.Map.MapId, gmMapId,
                                StringComparison.Ordinal))),
                    "GM identity is absent from publication manifest " + path);
            }
        }

        private static void ValidateProductionResources(string gmLevelId, string gmMapId)
        {
            var guids = AssetDatabase.FindAssets(string.Empty,
                new[] { "Assets/Resources" });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;

                var normalized = assetPath.Replace('\\', '/');
                Assert(!IsGmOnlyPath(normalized),
                    "GM-only asset path is absent from production Resources: "
                    + normalized);
                var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (mainAsset != null)
                {
                    Assert(!ContainsExactIdentity(mainAsset.name, gmLevelId, gmMapId),
                        "GM identity is absent from Resources object name: " + normalized);
                }

                var extension = Path.GetExtension(assetPath);
                if (!SerializedResourceExtensions.Contains(extension,
                        StringComparer.OrdinalIgnoreCase))
                    continue;
                var absolutePath = Path.GetFullPath(assetPath);
                if (!File.Exists(absolutePath)) continue;
                var serialized = File.ReadAllText(absolutePath);
                Assert(!ContainsExactIdentity(serialized, gmLevelId, gmMapId),
                    "GM identity is absent from serialized Resources content: "
                    + normalized);
            }
        }

        private static void ValidateProfileSelectionBoundary(string gmLevelId)
        {
            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            var profile = PlayerProfile.CreateDefault();
            Assert(!string.Equals(profile.lastSelectedLevelId, gmLevelId,
                    StringComparison.Ordinal)
                && catalog.Resolve(profile.lastSelectedLevelId).Succeeded,
                "default player profile selects a released level");

            profile.lastSelectedLevelId = gmLevelId;
            Assert(!catalog.Resolve(profile.lastSelectedLevelId).Succeeded,
                "a persisted GM identity cannot resolve through the player profile catalog");
            profile.lastSelectedLevelId = catalog.DefaultLevelId;
            Assert(catalog.Resolve(profile.lastSelectedLevelId).Succeeded,
                "the release catalog default remains the valid profile recovery target");
        }

        private static void ValidateNormalWebBuildBoundary(string gmLevelId,
            string gmMapId, string developmentWebBuildDirectory)
        {
            Assert(string.Equals(WebBuild.ReleaseOutputDirectory,
                    "Builds/WebGL", StringComparison.Ordinal),
                "normal WebBuild output remains Builds/WebGL");
            Assert(string.Equals(developmentWebBuildDirectory,
                    WebBuild.GmStressOutputDirectory, StringComparison.Ordinal),
                "GM acceptance uses the canonical Development WebGL output");

            var normalOutput = Path.GetFullPath(WebBuild.ReleaseOutputDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var developmentOutput = Path.GetFullPath(developmentWebBuildDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Assert(!string.Equals(normalOutput, developmentOutput,
                    StringComparison.OrdinalIgnoreCase),
                "GM Development WebGL output is distinct from normal Builds/WebGL");

            Assert(File.Exists(NormalWebBuildSource),
                "normal WebBuild source exists");
            var source = File.ReadAllText(NormalWebBuildSource);
            Assert(source.Contains(
                    "BuildInternal(ReleaseOutputDirectory, false);",
                    StringComparison.Ordinal)
                && source.Contains(
                    "BuildInternal(GmStressOutputDirectory, true);",
                    StringComparison.Ordinal)
                && source.Contains(
                    "gmStressDevelopment ? BuildOptions.Development : BuildOptions.None",
                    StringComparison.Ordinal),
                "release and GM WebGL entry methods select explicit non-development/development modes");
        }

        private static void ValidateDevelopmentCompilationBoundary()
        {
            const string root = "Assets/Scripts/Development/GmStress";
            var files = Directory.GetFiles(Path.GetFullPath(root), "*.cs",
                SearchOption.AllDirectories);
            Assert(files.Length > 0,
                "GM runtime implementation exists in the development-only module");
            foreach (var file in files)
            {
                var source = File.ReadAllText(file).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                Assert(source.StartsWith(
                        "#if UNITY_EDITOR || DEVELOPMENT_BUILD",
                        StringComparison.Ordinal),
                    "GM runtime source is excluded from non-development players: "
                    + file.Replace('\\', '/'));
            }
        }

        private static bool IsGmOnlyPath(string normalizedAssetPath)
        {
            return normalizedAssetPath.IndexOf("/GmStress/",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedAssetPath.IndexOf("/GMStress/",
                       StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedAssetPath.IndexOf("gm-stress",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsExactIdentity(string value, string gmLevelId,
            string gmMapId)
        {
            return !string.IsNullOrEmpty(value)
                && (value.IndexOf(gmLevelId, StringComparison.Ordinal) >= 0
                    || value.IndexOf(gmMapId, StringComparison.Ordinal) >= 0);
        }

        private static void RequireStableGmIdentity(string identity, string field)
        {
            if (string.IsNullOrWhiteSpace(identity)
                || identity.IndexOf("gm", StringComparison.OrdinalIgnoreCase) < 0)
                Fail(field + " must be a non-empty explicit GM identity: " + identity);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) Fail(message);
        }

        private static void Fail(string message)
        {
            throw new InvalidOperationException(
                "GM stress isolation validation failed: " + message);
        }
    }
}
