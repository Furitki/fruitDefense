using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FruitDefense.Editor
{
    public static partial class RuntimeUiVisualSystemValidator
    {
        private static void ValidateNoUnownedFiles(RuntimeUiVisualValidationReport report,
            string directory, string pattern, HashSet<string> owned, string kind)
        {
            var absolute = ToAbsolute(directory);
            if (!Directory.Exists(absolute)) return;
            foreach (var file in Directory.GetFiles(absolute, pattern, SearchOption.AllDirectories))
            {
                var path = ToAssetPath(file);
                if (owned.Contains(path)) continue;
                report.Error("manifest." + kind + ".unowned", path,
                    "The file is not owned by any manifest binding.",
                    "Remove it from the production directory or add its semantic binding.");
            }
        }

        private static void ValidateProductionAncillaryFiles(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet)
        {
            ValidateAncillaryDirectory(report,
                RuntimeUiArtSetRegistry.SourceDirectory(artSet), true);
            ValidateAncillaryDirectory(report,
                RuntimeUiArtSetRegistry.RuntimeDirectory(artSet), false);
        }

        private static void ValidateAncillaryDirectory(
            RuntimeUiVisualValidationReport report, string directory, bool sourceDirectory)
        {
            var absolute = ToAbsolute(directory);
            if (!Directory.Exists(absolute)) return;
            foreach (var file in Directory.GetFiles(absolute, "*", SearchOption.AllDirectories))
            {
                var assetPath = ToAssetPath(file);
                var relative = RuntimeUiArtSetRegistry.Normalize(
                    Path.GetRelativePath(absolute, file));
                var extension = Path.GetExtension(relative);
                if (string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
                    continue;

                var allowed = string.Equals(relative, "README.md", StringComparison.Ordinal)
                    || sourceDirectory && (
                        string.Equals(relative, "art_manifest.json", StringComparison.Ordinal)
                        || string.Equals(relative, "prompt-record.json", StringComparison.Ordinal)
                        || string.Equals(relative, "icons/alignment-audit.md", StringComparison.Ordinal)
                        || string.Equals(relative, "icons/prompt-record.md", StringComparison.Ordinal)
                        || (relative.StartsWith("export_", StringComparison.Ordinal)
                            && relative.EndsWith(".py", StringComparison.Ordinal)));
                if (allowed) continue;
                report.Error("production.ancillary.unclassified", assetPath,
                    "Production art roots may contain only manifest/export/prompt/readme/alignment metadata besides owned art.",
                    "Move review evidence and generated caches outside the production set root.");
            }
        }

        private static void ValidateSingleReleaseTheme(RuntimeUiVisualValidationReport report,
            RuntimeUiTheme releaseTheme)
        {
            var paths = AssetDatabase.FindAssets("t:RuntimeUiTheme",
                    new[] { RuntimeUiArtSetRegistry.ThemeRoot })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (paths.Length != 1 || paths[0] != RuntimeUiArtSetRegistry.ReleaseThemePath
                || AssetDatabase.GetAssetPath(releaseTheme) != RuntimeUiArtSetRegistry.ReleaseThemePath)
            {
                report.Error("release.theme.unique", RuntimeUiArtSetRegistry.ThemeRoot,
                    "Release must have exactly one RuntimeUiTheme at the fixed path.",
                    "Remove alternate release themes and keep " + RuntimeUiArtSetRegistry.ReleaseThemePath + ".");
            }
        }

        private static void ValidateForbiddenDependencies(RuntimeUiVisualValidationReport report,
            string ownerPath)
        {
            if (string.IsNullOrWhiteSpace(ownerPath)) return;
            foreach (var dependency in AssetDatabase.GetDependencies(ownerPath, true))
            {
                var normalized = "/" + RuntimeUiArtSetRegistry.Normalize(dependency).TrimStart('/');
                if (!ForbiddenReleaseSegments.Any(segment =>
                        normalized.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;
                report.Error("release.forbidden-reference", ownerPath,
                    "Release asset references forbidden authoring/evidence content: " + dependency,
                    "Reference only the Theme, Art/Sets, Art/Runtime and packaged font release roots.");
            }
        }

        private static void ValidateReleaseScenes(RuntimeUiVisualValidationReport report,
            RuntimeUiTheme theme)
        {
            foreach (var scenePath in RuntimeUiArtSetRegistry.ReleaseScenes)
            {
                if (!File.Exists(ToAbsolute(scenePath)))
                {
                    report.Error("release.scene.missing", scenePath,
                        "A fixed release scene is missing.", "Restore the Bootstrap → Lobby → Battle → Settlement flow.");
                    continue;
                }
                ValidateForbiddenDependencies(report, scenePath);
                ValidateSceneReferences(report, scenePath, theme);
            }
        }

        private static void ValidateSceneReferences(RuntimeUiVisualValidationReport report,
            string scenePath, RuntimeUiTheme releaseTheme)
        {
            var existing = SceneManager.GetSceneByPath(scenePath);
            var openedHere = !existing.IsValid() || !existing.isLoaded;
            var scene = openedHere
                ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
                : existing;
            try
            {
                var themeReferences = 0;
                var artSetReferences = 0;
                var coordinatorCount = 0;
                var correctCoordinatorBinding = 0;
                foreach (var root in scene.GetRootGameObjects())
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    var isCoordinator = behaviour.GetType().FullName == "FruitDefense.App.AppFlowCoordinator";
                    if (isCoordinator) coordinatorCount++;
                    var serialized = new SerializedObject(behaviour);
                    var iterator = serialized.GetIterator();
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var value = iterator.objectReferenceValue;
                        if (value is RuntimeUiTheme referencedTheme)
                        {
                            themeReferences++;
                            if (isCoordinator && iterator.propertyPath == "runtimeUiTheme"
                                && referencedTheme == releaseTheme)
                                correctCoordinatorBinding++;
                        }
                        else if (value is RuntimeUiArtSet) artSetReferences++;
                    }
                }

                var bootstrap = scenePath == RuntimeUiArtSetRegistry.BootstrapScenePath;
                if (bootstrap)
                {
                    if (coordinatorCount != 1 || themeReferences != 1
                        || correctCoordinatorBinding != 1 || artSetReferences != 0)
                    {
                        report.Error("scene.bootstrap.theme-binding", scenePath,
                            "Bootstrap must contain one AppFlowCoordinator with the sole direct release-theme reference and no direct art-set reference.",
                            "Bind runtimeUiTheme once on the unique AppFlowCoordinator.");
                    }
                }
                else if (themeReferences != 0 || artSetReferences != 0)
                {
                    report.Error("scene.release.direct-reference", scenePath,
                        "Only Bootstrap may directly reference the runtime theme; release scenes must not reference theme/art-set assets.",
                        "Remove direct references and accept the injected theme from the flow coordinator.");
                }
            }
            finally
            {
                if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string Sha256(string absolutePath)
        {
            using (var stream = File.OpenRead(absolutePath))
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ToAbsolute(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                RuntimeUiArtSetRegistry.Normalize(assetPath)));
        }

        private static string ToAssetPath(string absolutePath)
        {
            var project = Path.GetFullPath(Directory.GetCurrentDirectory())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(absolutePath);
            return RuntimeUiArtSetRegistry.Normalize(full.Substring(project.Length + 1));
        }
    }
}
