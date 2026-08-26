using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static partial class WebBuild
    {
        public const string ReleaseOutputDirectory = "Builds/WebGL";
        public const string AcceptanceOutputDirectory = "Builds/WebGL-Acceptance";
        public const string GmStressOutputDirectory = "Builds/WebGL-GM-Stress";
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
            BuildInternal(ReleaseOutputDirectory, false);
        }

        public static void BuildAcceptance()
        {
            BuildInternal(WebBuildProfile.Acceptance);
        }

        [MenuItem("Fruit Defense/Playtest/构建 GM 压力测试 WebGL")]
        public static void BuildGmStressDevelopment()
        {
            BuildInternal(GmStressOutputDirectory, true);
        }

        private static void BuildInternal(string outputDirectory, bool gmStressDevelopment)
        {
            var profile = gmStressDevelopment
                ? WebBuildProfile.GmStress
                : WebBuildProfile.Release;
            var expectedOptions = gmStressDevelopment ? BuildOptions.Development : BuildOptions.None;
            if (!string.Equals(profile.OutputDirectory, outputDirectory,
                    StringComparison.Ordinal)
                || profile.AdditionalBuildOptions != expectedOptions)
            {
                throw new InvalidOperationException(
                    $"WebGL build entry does not match profile {profile.Identity}.");
            }

            BuildInternal(profile);
        }

        private static void BuildInternal(WebBuildProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var outputPath = Path.GetFullPath(profile.OutputDirectory);
            RunProfileBuildGuards(
                profile, outputPath,
                () => BuildProfileOutput(profile, outputPath),
                () => PlayerSettings.GetScriptingDefineSymbols(
                    NamedBuildTarget.WebGL));
        }

        internal static void RunProfileBuildGuards(
            WebBuildProfile profile,
            string outputPath,
            Action buildAction,
            Func<string> readPersistentDefines)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (buildAction == null) throw new ArgumentNullException(nameof(buildAction));
            if (readPersistentDefines == null)
                throw new ArgumentNullException(nameof(readPersistentDefines));

            RunWithFailedOutputCleanup(
                outputPath,
                () => RunIfPersistentDefinesClean(
                    profile, buildAction, readPersistentDefines));
        }

        private static void BuildProfileOutput(WebBuildProfile profile, string outputPath)
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

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            // The current public origin is HTTP, so Unity's loader performs Brotli decompression.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.WebGL,
                ManagedStrippingLevel.High);
            if (!profile.AllowsDebugSymbolArtifact)
            {
                PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
                if (PlayerSettings.WebGL.debugSymbolMode != WebGLDebugSymbolMode.Off)
                    throw new InvalidOperationException(
                        $"WebGL profile {profile.Identity} requires debug symbols Off.");
            }

            var options = CreateBuildPlayerOptions(profile, scenes, outputPath);
            BuildReport report = null;
            RunWithPersistentScriptingDefineGuard(
                () => report = BuildPipeline.BuildPlayer(options),
                () => PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL),
                value => PlayerSettings.SetScriptingDefineSymbols(
                    NamedBuildTarget.WebGL, value));
            if (report == null)
                throw new InvalidOperationException("Web build did not return a build report.");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    $"Web build failed: {report.summary.result}, errors={report.summary.totalErrors}");

            ValidateBuiltTemplate(outputPath);
            var indexPath = Path.Combine(outputPath, "index.html");
            var indexHtml = File.ReadAllText(indexPath);
            indexHtml = InjectBuildProfileIdentity(indexHtml, profile);
            var buildDirectory = Path.Combine(outputPath, "Build");
            var payloadPaths = Directory.GetFiles(buildDirectory)
                .Where(path => IsWebGlPayload(path, profile.IsDevelopmentBuild))
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
            indexHtml = InjectAcceptanceUnityInstanceBootstrap(indexHtml, profile);
            if (profile.IsDevelopmentBuild)
            {
                const string headMarker = "<head>";
                if (!indexHtml.Contains(headMarker))
                    throw new InvalidOperationException(
                        "WebGL index does not expose a head marker for GM routing.");
                indexHtml = indexHtml.Replace(headMarker,
                    headMarker
                    + "\n    <meta name=\"fruit-defense-build-mode\" content=\"gm-stress\">"
                    + "\n    <script>(function(){const u=new URL(window.location.href);"
                    + "u.searchParams.set('gmStress','1');history.replaceState(null,'',u);})();</script>");
            }
            File.WriteAllText(indexPath, indexHtml);
            ValidateBuildProfileIdentity(File.ReadAllText(indexPath), profile, indexPath);
            ValidateBuiltArtifactSurface(outputPath, profile);

            var outputSize = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories)
                .Sum(path => new FileInfo(path).Length);
            var payloadCompression = payloadPaths.Any(path =>
                path.EndsWith(".unityweb", StringComparison.OrdinalIgnoreCase))
                ? "BrotliFallback"
                : "DevelopmentUncompressed";
            var payloadSummary = string.Join(
                ", ",
                payloadPaths.Select(path =>
                    $"{Path.GetFileName(path)}:version={payloadVersions[Path.GetFileName(path)]}"
                    + $":size={new FileInfo(path).Length}"));
            Debug.Log(
                $"{BuildSuccessMarker(profile)} "
                + $"path={outputPath} profile={profile.Identity} "
                + $"mode={(profile.IsDevelopmentBuild ? "Development" : "Release")} compression={payloadCompression} "
                + $"stripping=High debugSymbols="
                + $"{(profile.AllowsDebugSymbolArtifact ? "Optional" : "Off")} "
                + $"template={WebGlTemplate} host={TemplateHostId} "
                + $"size={outputSize} payloads=[{payloadSummary}]");
        }

        internal static BuildPlayerOptions CreateBuildPlayerOptions(
            WebBuildProfile profile,
            string[] scenes,
            string outputPath)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (scenes == null || scenes.Length == 0)
                throw new ArgumentException("At least one WebGL build scene is required.",
                    nameof(scenes));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("WebGL build output is required.",
                    nameof(outputPath));

            return new BuildPlayerOptions
            {
                scenes = (string[])scenes.Clone(),
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                // Keep byte-identical source inputs on one stable cache identity. Unity's
                // generated build GUID is otherwise embedded in the Web data package.
                options = BuildOptions.NoUniqueIdentifier
                    | profile.AdditionalBuildOptions,
                extraScriptingDefines = profile.CreateExtraScriptingDefines(),
            };
        }

        internal static void RunIfPersistentDefinesClean(
            WebBuildProfile profile,
            Action buildAction,
            Func<string> readPersistentDefines)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (buildAction == null) throw new ArgumentNullException(nameof(buildAction));
            if (readPersistentDefines == null)
                throw new ArgumentNullException(nameof(readPersistentDefines));

            ValidatePersistentAcceptanceDefineAbsent(
                readPersistentDefines() ?? string.Empty, profile);
            buildAction();
        }

        internal static void ValidatePersistentAcceptanceDefineAbsent(
            string persistentDefines,
            WebBuildProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var symbols = (persistentDefines ?? string.Empty).Split(
                new[] { ';', ',', ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            if (symbols.Any(symbol => string.Equals(
                    symbol,
                    WebBuildProfile.AcceptanceScriptingDefine,
                    StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"WebGL profile {profile.Identity} is blocked because persistent "
                    + $"scripting defines contain "
                    + $"{WebBuildProfile.AcceptanceScriptingDefine}.");
            }
        }

        internal static void RunWithFailedOutputCleanup(
            string outputPath,
            Action buildAction)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("WebGL build output is required.",
                    nameof(outputPath));
            if (buildAction == null) throw new ArgumentNullException(nameof(buildAction));

            var exactOutputPath = Path.GetFullPath(outputPath);
            var pathRoot = Path.GetPathRoot(exactOutputPath);
            if (string.Equals(
                    exactOutputPath.TrimEnd(Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar),
                    pathRoot?.TrimEnd(Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "A filesystem root cannot be used as a WebGL build output.");

            try
            {
                buildAction();
            }
            catch (Exception buildFailure)
            {
                try
                {
                    if (Directory.Exists(exactOutputPath))
                        Directory.Delete(exactOutputPath, true);
                }
                catch (Exception cleanupFailure)
                {
                    throw new InvalidOperationException(
                        $"WebGL build failed and its exact output could not be removed: "
                        + exactOutputPath,
                        new AggregateException(buildFailure, cleanupFailure));
                }

                ExceptionDispatchInfo.Capture(buildFailure).Throw();
                throw;
            }
        }

        internal static void RunWithPersistentScriptingDefineGuard(
            Action buildAction,
            Func<string> readPersistentDefines,
            Action<string> restorePersistentDefines)
        {
            if (buildAction == null) throw new ArgumentNullException(nameof(buildAction));
            if (readPersistentDefines == null)
                throw new ArgumentNullException(nameof(readPersistentDefines));
            if (restorePersistentDefines == null)
                throw new ArgumentNullException(nameof(restorePersistentDefines));

            var definesBefore = readPersistentDefines() ?? string.Empty;
            Exception buildFailure = null;
            try
            {
                buildAction();
            }
            catch (Exception exception)
            {
                buildFailure = exception;
            }

            var definesAfter = readPersistentDefines() ?? string.Empty;
            if (!string.Equals(definesBefore, definesAfter, StringComparison.Ordinal))
            {
                try
                {
                    restorePersistentDefines(definesBefore);
                }
                catch (Exception restoreFailure)
                {
                    throw new InvalidOperationException(
                        "WebGL build mutated persistent scripting defines and restoration failed.",
                        restoreFailure);
                }

                throw new InvalidOperationException(
                    "WebGL build mutated persistent scripting defines; the original value was restored.",
                    buildFailure);
            }

            if (buildFailure != null)
                ExceptionDispatchInfo.Capture(buildFailure).Throw();
        }

        private static string BuildSuccessMarker(WebBuildProfile profile)
        {
            if (ReferenceEquals(profile, WebBuildProfile.Release))
                return "FRUIT_DEFENSE_WEB_BUILD_OK";
            if (ReferenceEquals(profile, WebBuildProfile.Acceptance))
                return "FRUIT_DEFENSE_ACCEPTANCE_WEB_BUILD_OK";
            return "FRUIT_DEFENSE_GM_STRESS_WEB_BUILD_OK";
        }

        private static bool IsWebGlPayload(string path, bool gmStressDevelopment)
        {
            if (path.EndsWith(".loader.js", StringComparison.OrdinalIgnoreCase)) return true;
            return gmStressDevelopment
                ? path.EndsWith(".data", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".framework.js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase)
                : path.EndsWith(".data.unityweb", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".framework.js.unityweb", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".wasm.unityweb", StringComparison.OrdinalIgnoreCase);
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
