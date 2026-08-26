using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FruitDefense.Editor
{
    internal enum WebBuildArtifactRole
    {
        Index,
        Loader,
        Data,
        Framework,
        Wasm,
        DebugSymbols,
    }

    public static partial class WebBuild
    {
        internal const string BuildProfileMetaName = "fruit-defense-build-profile";
        internal const string AcceptanceHostQueryPredicate =
            "new URLSearchParams(window.location.search).get('acceptance') === '1'";
        internal const string LegacyAcceptanceHostQueryPredicate =
            "new URLSearchParams(window.location.search).has('acceptance')";
        private const string UnityReadyMarker = "}).then((unityInstance) => {";
        private static readonly Regex HtmlMetaTagPattern = new Regex(
            @"<meta\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex HtmlQuotedAttributePattern = new Regex(
            @"(?<key>[^\s=/>]+)\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)')",
            RegexOptions.CultureInvariant);
        private static readonly string[] AcceptanceIndexRequiredTokens =
        {
            AcceptanceHostQueryPredicate,
            "fruitDefensePendingUnityInstance",
            "fruitDefenseAcceptanceRouteReady",
            "fruitDefenseUnityInstance",
        };
        private static readonly string[] AcceptanceDataRequiredTokens =
        {
            "ConfigureAcceptanceState",
            "ConfigureAcceptanceFlow",
            "AcceptanceLaunchQuery",
            "AcceptanceSafeAreaDecorator",
            "IAcceptanceBattlePort",
            "AcceptanceCommandResult",
            "AcceptanceTerminalFixture",
            "ShouldEnterAcceptanceBattle",
            "AcceptanceLevelId",
            "IsAcceptanceLaunch",
            "SignalAcceptanceRouteReady",
            "TryConfigureNamedState",
            "TryConfigureTerminalFixture",
            "CombatFeedbackAcceptanceTelemetryJson",
            "ConfigureCombatFeedbackAcceptance",
            "PublishCombatFeedbackAcceptanceTelemetry",
            "safeTop",
            "safeBottom",
        };
        private static readonly string[] AcceptanceFrameworkRequiredTokens =
        {
            "_FruitDefenseAcceptanceReady(",
            "_FruitDefensePublishSettlementOutcomeReveal(",
            "_FruitDefensePublishCombatFeedbackTelemetry(",
            "window.fruitDefenseAcceptanceRouteReady=true",
            "window.fruitDefenseAppRoute=route",
            "window.fruitDefenseAcceptanceIdentity=identity",
            "window.fruitDefenseAcceptanceIdentityHistory=",
            "window.fruitDefensePendingUnityInstance",
            "window.fruitDefenseUnityInstance=",
            "window.fruitDefenseSettlementOutcomeRevealState=",
            "window.fruitDefenseSettlementOutcomeRevealHistory=",
            "window.fruitDefenseCombatFeedbackTelemetry=JSON.parse(json)",
            "window.fruitDefenseCombatFeedbackTelemetryHistory=",
        };
        private static readonly string[] AcceptanceWasmRequiredTokens =
        {
            "FruitDefenseAcceptanceReady",
            "FruitDefensePublishSettlementOutcomeReveal",
            "FruitDefensePublishCombatFeedbackTelemetry",
        };
        private static readonly string[] ForbiddenAcceptanceSurfaceTokens =
        {
            AcceptanceHostQueryPredicate,
            LegacyAcceptanceHostQueryPredicate,
            "fruitDefensePendingUnityInstance",
            "fruitDefenseUnityInstance",
            "fruitDefenseAcceptanceRouteReady",
            "fruitDefenseAppRoute",
            "fruitDefenseAcceptanceIdentity",
            "fruitDefenseAcceptanceIdentityHistory",
            "fruitDefenseSettlementOutcomeRevealState",
            "fruitDefenseSettlementOutcomeRevealHistory",
            "fruitDefenseCombatFeedbackTelemetry",
            "fruitDefenseCombatFeedbackTelemetryHistory",
            "FruitDefenseAcceptanceReady",
            "FruitDefensePublishSettlementOutcomeReveal",
            "FruitDefensePublishCombatFeedbackTelemetry",
            "ConfigureAcceptanceState",
            "ConfigureAcceptanceFlow",
            "AcceptanceLaunchQuery",
            "AcceptanceSafeAreaDecorator",
            "IAcceptanceBattlePort",
            "AcceptanceCommandResult",
            "AcceptanceTerminalFixture",
            "ShouldEnterAcceptanceBattle",
            "AcceptanceLevelId",
            "IsAcceptanceLaunch",
            "SignalAcceptanceRouteReady",
            "TryConfigureNamedState",
            "TryConfigureTerminalFixture",
            "CombatFeedbackAcceptanceTelemetryJson",
            "ConfigureCombatFeedbackAcceptance",
            "PublishCombatFeedbackAcceptanceTelemetry",
        };
        private static readonly string[] ArtifactScanTokens =
            ForbiddenAcceptanceSurfaceTokens
                .Concat(AcceptanceFrameworkRequiredTokens)
                .Concat(new[] { "safeTop", "safeBottom" })
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        internal static string InjectBuildProfileIdentity(
            string indexHtml,
            WebBuildProfile profile)
        {
            if (indexHtml == null) throw new ArgumentNullException(nameof(indexHtml));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            const string headMarker = "<head>";
            var headIndex = indexHtml.IndexOf(headMarker, StringComparison.Ordinal);
            if (headIndex < 0
                || indexHtml.LastIndexOf(headMarker, StringComparison.Ordinal) != headIndex)
            {
                throw new InvalidOperationException(
                    "WebGL index must expose exactly one head marker for build-profile identity.");
            }

            if (FindBuildProfileMetaTags(indexHtml).Count != 0)
                throw new InvalidOperationException(
                    "WebGL index already contains a build-profile identity.");

            return indexHtml.Insert(
                headIndex + headMarker.Length,
                "\n    " + BuildProfileMetaTag(profile));
        }

        internal static string InjectAcceptanceUnityInstanceBootstrap(
            string indexHtml,
            WebBuildProfile profile)
        {
            if (indexHtml == null) throw new ArgumentNullException(nameof(indexHtml));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.IncludesAcceptanceRuntime) return indexHtml;

            if (CountOccurrences(indexHtml, UnityReadyMarker) != 1)
                throw new InvalidOperationException(
                    "Acceptance WebGL index must expose exactly one Unity ready callback.");
            foreach (var token in AcceptanceIndexRequiredTokens)
            {
                if (indexHtml.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Acceptance WebGL index already exposes bootstrap token: " + token);
            }

            return indexHtml.Replace(
                UnityReadyMarker,
                UnityReadyMarker
                + "\n          if (" + AcceptanceHostQueryPredicate + ") {"
                + " window.fruitDefensePendingUnityInstance = unityInstance;"
                + " if (window.fruitDefenseAcceptanceRouteReady)"
                + " window.fruitDefenseUnityInstance = unityInstance; }");
        }

        internal static void ValidateBuiltArtifactSurface(
            string outputPath,
            WebBuildProfile profile)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("WebGL build output is required.",
                    nameof(outputPath));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var artifacts = ResolveBuiltArtifacts(outputPath, profile);
            var indexText = ReadUtf8Artifact(artifacts[WebBuildArtifactRole.Index]);
            ValidateBuildProfileIdentity(
                indexText, profile, artifacts[WebBuildArtifactRole.Index]);
            foreach (var pair in artifacts)
            {
                if (pair.Key == WebBuildArtifactRole.Loader
                    || pair.Key == WebBuildArtifactRole.Framework
                    || pair.Key == WebBuildArtifactRole.DebugSymbols)
                    ValidateStrictUtf8Artifact(pair.Value);
            }

            var matches = artifacts.ToDictionary(
                pair => pair.Key,
                pair => ScanArtifactTokens(pair.Value, ArtifactScanTokens));
            if (profile.IncludesAcceptanceRuntime)
            {
                foreach (var token in AcceptanceIndexRequiredTokens)
                {
                    if (CountOccurrences(indexText, token) != 1)
                        throw new InvalidOperationException(
                            $"Acceptance WebGL index token is missing or ambiguous: {token}");
                }

                RequireArtifactTokens(matches, WebBuildArtifactRole.Data,
                    AcceptanceDataRequiredTokens);
                RequireArtifactTokens(matches, WebBuildArtifactRole.Framework,
                    AcceptanceFrameworkRequiredTokens);
                RequireArtifactTokens(matches, WebBuildArtifactRole.Wasm,
                    AcceptanceWasmRequiredTokens);
                RejectArtifactToken(matches, LegacyAcceptanceHostQueryPredicate,
                    profile.Identity);
                return;
            }

            var forbiddenTokens = profile.IsPublishable
                ? ArtifactScanTokens
                : ForbiddenAcceptanceSurfaceTokens;
            foreach (var pair in matches.OrderBy(item => item.Key))
            {
                foreach (var token in forbiddenTokens)
                {
                    if (!pair.Value.Contains(token)) continue;
                    throw new InvalidOperationException(
                        $"WebGL profile {profile.Identity} exposes forbidden acceptance "
                        + $"token in {pair.Key}: {token}");
                }
            }
        }

        internal static string[] CreateRequiredAcceptanceTokens(
            WebBuildArtifactRole role)
        {
            switch (role)
            {
                case WebBuildArtifactRole.Index:
                    return (string[])AcceptanceIndexRequiredTokens.Clone();
                case WebBuildArtifactRole.Loader:
                    return Array.Empty<string>();
                case WebBuildArtifactRole.Data:
                    return (string[])AcceptanceDataRequiredTokens.Clone();
                case WebBuildArtifactRole.Framework:
                    return (string[])AcceptanceFrameworkRequiredTokens.Clone();
                case WebBuildArtifactRole.Wasm:
                    return (string[])AcceptanceWasmRequiredTokens.Clone();
                case WebBuildArtifactRole.DebugSymbols:
                    return Array.Empty<string>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        private static Dictionary<WebBuildArtifactRole, string> ResolveBuiltArtifacts(
            string outputPath,
            WebBuildProfile profile)
        {
            try
            {
                var exactOutputPath = Path.GetFullPath(outputPath);
                if (!Directory.Exists(exactOutputPath))
                    throw new InvalidOperationException(
                        "WebGL build output is missing: " + exactOutputPath);

                var indexPath = Path.Combine(exactOutputPath, "index.html");
                if (!File.Exists(indexPath))
                    throw new InvalidOperationException(
                        "WebGL build index is missing: " + indexPath);

                var buildDirectory = Path.Combine(exactOutputPath, "Build");
                if (!Directory.Exists(buildDirectory))
                    throw new InvalidOperationException(
                        "WebGL build payload directory is missing: " + buildDirectory);
                var buildFiles = Directory.GetFiles(
                    buildDirectory, "*", SearchOption.TopDirectoryOnly);

                var compressed = !profile.IsDevelopmentBuild;
                var artifacts = new Dictionary<WebBuildArtifactRole, string>
                {
                    [WebBuildArtifactRole.Index] = indexPath,
                    [WebBuildArtifactRole.Loader] = ResolveSingleBuildArtifact(
                        buildFiles, ".loader.js", WebBuildArtifactRole.Loader),
                    [WebBuildArtifactRole.Data] = ResolveSingleBuildArtifact(
                        buildFiles, compressed ? ".data.unityweb" : ".data",
                        WebBuildArtifactRole.Data),
                    [WebBuildArtifactRole.Framework] = ResolveSingleBuildArtifact(
                        buildFiles,
                        compressed ? ".framework.js.unityweb" : ".framework.js",
                        WebBuildArtifactRole.Framework),
                    [WebBuildArtifactRole.Wasm] = ResolveSingleBuildArtifact(
                        buildFiles, compressed ? ".wasm.unityweb" : ".wasm",
                        WebBuildArtifactRole.Wasm),
                };
                var requiredBuildPaths = new HashSet<string>(
                    artifacts.Where(pair => pair.Key != WebBuildArtifactRole.Index)
                        .Select(pair => pair.Value),
                    StringComparer.OrdinalIgnoreCase);
                if (requiredBuildPaths.Count != 4)
                    throw new InvalidOperationException(
                        "WebGL build artifact roles do not resolve to four distinct files.");

                var additionalFiles = buildFiles.Where(path =>
                        !requiredBuildPaths.Contains(path))
                    .ToArray();
                if (additionalFiles.Length > 0)
                {
                    if (!profile.AllowsDebugSymbolArtifact
                        || additionalFiles.Length != 1
                        || !IsDebugSymbolArtifact(additionalFiles[0]))
                    {
                        throw new InvalidOperationException(
                            $"WebGL profile {profile.Identity} contains unexpected "
                            + "additional build artifacts: "
                            + string.Join(", ", additionalFiles.Select(Path.GetFileName)));
                    }

                    artifacts[WebBuildArtifactRole.DebugSymbols] = additionalFiles[0];
                }
                return artifacts;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"WebGL profile {profile.Identity} artifacts could not be enumerated.",
                    exception);
            }
        }

        private static string ResolveSingleBuildArtifact(
            IEnumerable<string> buildFiles,
            string suffix,
            WebBuildArtifactRole role)
        {
            var matches = buildFiles.Where(path =>
                    path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    $"WebGL artifact scan expected one {role} file ending in {suffix}, "
                    + $"found {matches.Length}.");
            return matches[0];
        }

        private static bool IsDebugSymbolArtifact(string path)
        {
            return path.EndsWith(".symbols.json", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(
                    ".symbols.json.unityweb", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadUtf8Artifact(string path)
        {
            try
            {
                return new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "WebGL text artifact could not be read as strict UTF-8: " + path,
                    exception);
            }
        }

        private static void ValidateStrictUtf8Artifact(string path)
        {
            try
            {
                using var file = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (path.EndsWith(".unityweb", StringComparison.OrdinalIgnoreCase))
                {
                    using var decompressed = new BrotliStream(
                        file, CompressionMode.Decompress, false);
                    using var buffer = new MemoryStream();
                    decompressed.CopyTo(buffer);
                    if (buffer.Length == 0)
                        throw new InvalidOperationException(
                            "WebGL text artifact is empty after decompression: " + path);
                    new UTF8Encoding(false, true).GetString(buffer.ToArray());
                    return;
                }

                var bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0)
                    throw new InvalidOperationException(
                        "WebGL text artifact is empty: " + path);
                new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "WebGL text artifact could not be decoded as strict UTF-8: " + path,
                    exception);
            }
        }

        private static HashSet<string> ScanArtifactTokens(
            string path,
            IReadOnlyList<string> tokens)
        {
            try
            {
                using var file = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (path.EndsWith(".unityweb", StringComparison.OrdinalIgnoreCase))
                {
                    using var decompressed = new BrotliStream(
                        file, CompressionMode.Decompress, false);
                    return ScanStreamTokens(decompressed, tokens, path);
                }

                return ScanStreamTokens(file, tokens, path);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "WebGL artifact could not be read and scanned: " + path,
                    exception);
            }
        }

        private static HashSet<string> ScanStreamTokens(
            Stream stream,
            IReadOnlyList<string> tokens,
            string path)
        {
            var tokenBytes = tokens.ToDictionary(
                token => token,
                token => Encoding.ASCII.GetBytes(token),
                StringComparer.Ordinal);
            var longestToken = tokenBytes.Values.Max(bytes => bytes.Length);
            var found = new HashSet<string>(StringComparer.Ordinal);
            var readBuffer = new byte[64 * 1024];
            var tail = Array.Empty<byte>();
            long totalBytes = 0;
            int bytesRead;
            while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                totalBytes += bytesRead;
                var window = new byte[tail.Length + bytesRead];
                Buffer.BlockCopy(tail, 0, window, 0, tail.Length);
                Buffer.BlockCopy(readBuffer, 0, window, tail.Length, bytesRead);
                foreach (var pair in tokenBytes)
                {
                    if (!found.Contains(pair.Key)
                        && IndexOfBytes(window, pair.Value) >= 0)
                        found.Add(pair.Key);
                }

                var tailLength = Math.Min(longestToken - 1, window.Length);
                tail = new byte[tailLength];
                Buffer.BlockCopy(
                    window, window.Length - tailLength, tail, 0, tailLength);
            }

            if (totalBytes == 0)
                throw new InvalidOperationException(
                    "WebGL artifact is empty after decompression: " + path);
            return found;
        }

        private static int IndexOfBytes(byte[] source, byte[] value)
        {
            if (value.Length == 0) return 0;
            var lastStart = source.Length - value.Length;
            for (var index = 0; index <= lastStart; index++)
            {
                var matched = true;
                for (var offset = 0; offset < value.Length; offset++)
                {
                    if (source[index + offset] == value[offset]) continue;
                    matched = false;
                    break;
                }

                if (matched) return index;
            }

            return -1;
        }

        private static void RequireArtifactTokens(
            IReadOnlyDictionary<WebBuildArtifactRole, HashSet<string>> matches,
            WebBuildArtifactRole role,
            IEnumerable<string> requiredTokens)
        {
            foreach (var token in requiredTokens)
            {
                if (matches[role].Contains(token)) continue;
                throw new InvalidOperationException(
                    $"Acceptance WebGL {role} artifact is missing required token: {token}");
            }
        }

        private static void RejectArtifactToken(
            IReadOnlyDictionary<WebBuildArtifactRole, HashSet<string>> matches,
            string token,
            string profileIdentity)
        {
            foreach (var pair in matches.OrderBy(item => item.Key))
            {
                if (!pair.Value.Contains(token)) continue;
                throw new InvalidOperationException(
                    $"WebGL profile {profileIdentity} exposes obsolete direct routing "
                    + $"token in {pair.Key}: {token}");
            }
        }

        internal static void ValidateBuildProfileIdentity(
            string indexHtml,
            WebBuildProfile profile,
            string sourceName)
        {
            if (indexHtml == null) throw new ArgumentNullException(nameof(indexHtml));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var expectedTag = BuildProfileMetaTag(profile);
            var profileTags = FindBuildProfileMetaTags(indexHtml);
            if (profileTags.Count != 1
                || !string.Equals(profileTags[0], expectedTag, StringComparison.Ordinal)
                || CountOccurrences(indexHtml, expectedTag) != 1)
            {
                throw new InvalidOperationException(
                    $"WebGL build profile identity is missing or ambiguous in "
                    + $"{sourceName ?? "generated index"}: expected {profile.Identity}.");
            }
        }

        private static string BuildProfileMetaTag(WebBuildProfile profile)
        {
            return $"<meta name=\"{BuildProfileMetaName}\" content=\"{profile.Identity}\">";
        }

        private static List<string> FindBuildProfileMetaTags(string indexHtml)
        {
            var result = new List<string>();
            foreach (Match metaMatch in HtmlMetaTagPattern.Matches(indexHtml))
            {
                var ownsProfileName = false;
                foreach (Match attributeMatch in HtmlQuotedAttributePattern.Matches(
                             metaMatch.Value))
                {
                    if (!string.Equals(
                            attributeMatch.Groups["key"].Value,
                            "name",
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    var value = attributeMatch.Groups["double"].Success
                        ? attributeMatch.Groups["double"].Value
                        : attributeMatch.Groups["single"].Value;
                    if (string.Equals(value, BuildProfileMetaName,
                            StringComparison.OrdinalIgnoreCase))
                        ownsProfileName = true;
                }

                if (ownsProfileName) result.Add(metaMatch.Value);
            }

            return result;
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var startIndex = 0;
            while (startIndex < source.Length)
            {
                var matchIndex = source.IndexOf(value, startIndex, StringComparison.Ordinal);
                if (matchIndex < 0) break;
                count++;
                startIndex = matchIndex + value.Length;
            }
            return count;
        }
    }
}
