using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class WebBuild
    {
        private const string OutputDirectory = "Builds/WebGL";

        public static void Build()
        {
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
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Web build failed: {report.summary.result}, errors={report.summary.totalErrors}");

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

            var buildVersion = CreateContentVersion(payloadPaths);
            foreach (var payloadPath in payloadPaths)
            {
                var fileName = Path.GetFileName(payloadPath);
                var source = $"/{fileName}\"";
                var versioned = $"/{fileName}?v={buildVersion}\"";
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
                + "\n          if (new URLSearchParams(window.location.search).has('acceptance')) "
                + "window.fruitDefenseUnityInstance = unityInstance;");
            File.WriteAllText(indexPath, indexHtml);

            var outputSize = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
                .Sum(path => new FileInfo(path).Length);
            var payloadSummary = string.Join(
                ", ",
                payloadPaths.Select(path => $"{Path.GetFileName(path)}={new FileInfo(path).Length}"));
            Debug.Log(
                $"FRUIT_DEFENSE_WEB_BUILD_OK path={outputPath} compression=BrotliFallback "
                + $"stripping=High version={buildVersion} size={outputSize} payloads=[{payloadSummary}]");
        }

        private static string CreateContentVersion(string[] payloadPaths)
        {
            var fingerprint = new StringBuilder();
            foreach (var path in payloadPaths)
            {
                using var stream = File.OpenRead(path);
                using var fileHash = SHA256.Create();
                fingerprint.Append(Path.GetFileName(path));
                fingerprint.Append(':');
                fingerprint.Append(BitConverter.ToString(fileHash.ComputeHash(stream)).Replace("-", string.Empty));
                fingerprint.Append(';');
            }

            using var buildHash = SHA256.Create();
            var digest = buildHash.ComputeHash(Encoding.UTF8.GetBytes(fingerprint.ToString()));
            return BitConverter.ToString(digest)
                .Replace("-", string.Empty)
                .Substring(0, 12)
                .ToLowerInvariant();
        }
    }
}
