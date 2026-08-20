using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class WebBuild
    {
        private const string OutputDirectory = "Builds/WebGL";
        private const string WebGlTemplate = "PROJECT:FruitDefensePortraitContain";
        private const string TemplateDirectory =
            "Assets/WebGLTemplates/FruitDefensePortraitContain";
        private const string TemplateHostId = "fruit-defense-portrait-contain-v1";
        private static readonly string[] ByteStableTemplateFiles =
        {
            "TemplateData/fruit-defense-host.css",
            "TemplateData/fruit-defense-host.js",
        };

        public static void Build()
        {
            ValidateTemplateSource();
            PlayerSettings.WebGL.template = WebGlTemplate;
            if (PlayerSettings.WebGL.template != WebGlTemplate)
                throw new InvalidOperationException(
                    $"WebGL template selection failed: {PlayerSettings.WebGL.template}");

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured for the Web build.");

            var outputPath = Path.GetFullPath(OutputDirectory);
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            // The current public origin is HTTP, so Unity's loader performs Brotli decompression.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.WebGL,
                ManagedStrippingLevel.High);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                // Keep byte-identical source inputs on one stable cache identity. Unity's
                // generated build GUID is otherwise embedded in the Web data package.
                options = BuildOptions.NoUniqueIdentifier,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Web build failed: {report.summary.result}, errors={report.summary.totalErrors}");

            ValidateBuiltTemplate(outputPath);
            var indexPath = Path.Combine(outputPath, "index.html");
            var indexHtml = File.ReadAllText(indexPath);
            var buildDirectory = Path.Combine(outputPath, "Build");
            var payloadPaths = Directory.GetFiles(buildDirectory)
                .Where(path =>
                    path.EndsWith(".loader.js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".data.unityweb", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".framework.js.unityweb", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".wasm.unityweb", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .ToArray();

            if (payloadPaths.Length != 4)
                throw new InvalidOperationException(
                    $"Expected four versioned WebGL payloads, found {payloadPaths.Length}.");

            var payloadVersions = payloadPaths.ToDictionary(
                path => Path.GetFileName(path),
                CreateContentVersion,
                StringComparer.Ordinal);
            foreach (var payloadPath in payloadPaths)
            {
                var fileName = Path.GetFileName(payloadPath);
                var source = $"/{fileName}\"";
                var versioned = $"/{fileName}?v={payloadVersions[fileName]}\"";
                if (!indexHtml.Contains(source))
                    throw new InvalidOperationException(
                        $"WebGL index does not reference generated payload: {fileName}");
                indexHtml = indexHtml.Replace(source, versioned);
            }
            const string unityReadyMarker = "}).then((unityInstance) => {";
            if (!indexHtml.Contains(unityReadyMarker))
                throw new InvalidOperationException("WebGL index does not expose the Unity ready callback.");
            indexHtml = indexHtml.Replace(
                unityReadyMarker,
                unityReadyMarker
                + "\n          if (new URLSearchParams(window.location.search).has('acceptance')) {"
                + " window.fruitDefensePendingUnityInstance = unityInstance;"
                + " if (window.fruitDefenseAcceptanceRouteReady) window.fruitDefenseUnityInstance = unityInstance; }");
            File.WriteAllText(indexPath, indexHtml);

            var outputSize = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
                .Sum(path => new FileInfo(path).Length);
            var payloadSummary = string.Join(
                ", ",
                payloadPaths.Select(path =>
                    $"{Path.GetFileName(path)}:version={payloadVersions[Path.GetFileName(path)]}"
                    + $":size={new FileInfo(path).Length}"));
            Debug.Log(
                $"FRUIT_DEFENSE_WEB_BUILD_OK path={outputPath} compression=BrotliFallback "
                + $"stripping=High template={WebGlTemplate} host={TemplateHostId} "
                + $"size={outputSize} payloads=[{payloadSummary}]");
        }

        internal static void ValidateTemplateSource()
        {
            var templatePath = Path.GetFullPath(TemplateDirectory);
            var indexPath = Path.Combine(templatePath, "index.html");
            if (!File.Exists(indexPath))
                throw new InvalidOperationException(
                    $"Project-owned WebGL template entry is missing: {indexPath}");

            var index = File.ReadAllText(indexPath);
            RequireSourceToken(index, $"data-fruit-defense-host=\"portrait-contain-v1\"", indexPath);
            RequireSourceToken(index, "TemplateData/fruit-defense-host.css", indexPath);
            RequireSourceToken(index, "TemplateData/fruit-defense-host.js", indexPath);
            RequireSourceToken(index, "matchWebGLToCanvasSize: !host.usesFixedRenderTarget", indexPath);
            RequireSourceToken(index, "}).then((unityInstance) => {", indexPath);

            foreach (var relativePath in ByteStableTemplateFiles)
            {
                var sourcePath = Path.Combine(
                    templatePath,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(sourcePath))
                    throw new InvalidOperationException(
                        $"Project-owned WebGL template source is missing: {sourcePath}");
                if (new FileInfo(sourcePath).Length == 0)
                    throw new InvalidOperationException(
                        $"Project-owned WebGL template source is empty: {sourcePath}");
            }

            var cssPath = Path.Combine(templatePath, "TemplateData", "fruit-defense-host.css");
            var css = File.ReadAllText(cssPath);
            RequireSourceToken(css, "overflow: hidden", cssPath);
            RequireSourceToken(css, "place-items: center", cssPath);
            RequireSourceToken(css, "#unity-canvas", cssPath);

            var scriptPath = Path.Combine(templatePath, "TemplateData", "fruit-defense-host.js");
            var script = File.ReadAllText(scriptPath);
            RequireSourceToken(script, $"const HOST_ID = \"{TemplateHostId}\"", scriptPath);
            RequireSourceToken(script, "const LOGICAL_WIDTH = 402", scriptPath);
            RequireSourceToken(script, "const LOGICAL_HEIGHT = 874", scriptPath);
            RequireSourceToken(
                script,
                "Math.min(viewport.width / LOGICAL_WIDTH, viewport.height / LOGICAL_HEIGHT)",
                scriptPath);
            RequireSourceToken(script, "getBoundingClientRect()", scriptPath);
            RequireSourceToken(script, "armDevicePixelRatioListener", scriptPath);
        }

        private static void ValidateBuiltTemplate(string outputPath)
        {
            var sourceRoot = Path.GetFullPath(TemplateDirectory);
            foreach (var relativePath in ByteStableTemplateFiles)
            {
                var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                var sourcePath = Path.Combine(sourceRoot, normalizedPath);
                var builtPath = Path.Combine(outputPath, normalizedPath);
                if (!File.Exists(builtPath))
                    throw new InvalidOperationException(
                        $"Built WebGL host file is missing: {builtPath}");
                if (!File.ReadAllBytes(sourcePath).SequenceEqual(File.ReadAllBytes(builtPath)))
                    throw new InvalidOperationException(
                        $"Built WebGL host file differs from its project-owned source: {relativePath}");
            }

            var indexPath = Path.Combine(outputPath, "index.html");
            var builtIndex = File.ReadAllText(indexPath);
            RequireSourceToken(
                builtIndex,
                "data-fruit-defense-host=\"portrait-contain-v1\"",
                indexPath);
            RequireSourceToken(builtIndex, "TemplateData/fruit-defense-host.css", indexPath);
            RequireSourceToken(builtIndex, "TemplateData/fruit-defense-host.js", indexPath);
        }

        private static void RequireSourceToken(string source, string token, string path)
        {
            if (!source.Contains(token, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"WebGL host contract token is missing from {path}: {token}");
        }

        private static string CreateContentVersion(string payloadPath)
        {
            using var stream = File.OpenRead(payloadPath);
            using var fileHash = SHA256.Create();
            return BitConverter.ToString(fileHash.ComputeHash(stream))
                .Replace("-", string.Empty)
                .Substring(0, 12)
                .ToLowerInvariant();
        }
    }
}
