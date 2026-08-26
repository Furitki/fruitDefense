using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class WebBuildArtifactSurfaceSmoke
    {
        private const string BaseIndex =
            "<!doctype html><html><head></head><body><script>"
            + "createUnityInstance(canvas, config, () => {})"
            + ".then((unityInstance) => { ready(); });"
            + "</script></body></html>";

        public static void Run()
        {
            ValidateProfileScopedIndexBootstrap();
            ValidateCompleteProfileSurfaces();
            ValidateForbiddenTokensAcrossReleaseArtifacts();
            ValidateAcceptanceTokenArtifactOwnership();
            ValidateFailClosedArtifactReads();
            Debug.Log("FRUIT_DEFENSE_WEB_BUILD_ARTIFACT_SURFACE_OK");
        }

        private static void ValidateProfileScopedIndexBootstrap()
        {
            var release = WebBuild.InjectAcceptanceUnityInstanceBootstrap(
                BaseIndex, WebBuildProfile.Release);
            var gmStress = WebBuild.InjectAcceptanceUnityInstanceBootstrap(
                BaseIndex, WebBuildProfile.GmStress);
            var acceptance = WebBuild.InjectAcceptanceUnityInstanceBootstrap(
                BaseIndex, WebBuildProfile.Acceptance);

            Assert(string.Equals(release, BaseIndex, StringComparison.Ordinal),
                "release index receives no acceptance bootstrap or empty shell");
            Assert(string.Equals(gmStress, BaseIndex, StringComparison.Ordinal),
                "GM index receives no acceptance bootstrap or empty shell");
            foreach (var token in WebBuild.CreateRequiredAcceptanceTokens(
                         WebBuildArtifactRole.Index))
            {
                Assert(CountOccurrences(acceptance, token) == 1,
                    "acceptance index owns exactly one required bootstrap token: " + token);
                Assert(!release.Contains(token, StringComparison.Ordinal)
                    && !gmStress.Contains(token, StringComparison.Ordinal),
                    "release and GM indexes omit acceptance token: " + token);
            }

            AssertThrows(
                () => WebBuild.InjectAcceptanceUnityInstanceBootstrap(
                    acceptance, WebBuildProfile.Acceptance),
                "acceptance bootstrap injection rejects duplicate exposure");
        }

        private static void ValidateCompleteProfileSurfaces()
        {
            WithTempRoot(root =>
            {
                foreach (var profile in new[]
                         {
                             WebBuildProfile.Release,
                             WebBuildProfile.Acceptance,
                             WebBuildProfile.GmStress,
                         })
                {
                    var output = CreateValidOutput(
                        Path.Combine(root, profile.Identity), profile);
                    WebBuild.ValidateBuiltArtifactSurface(output, profile);
                }

                var gmWithSymbols = CreateValidOutput(
                    Path.Combine(root, "gm-with-symbols"), WebBuildProfile.GmStress);
                WriteArtifact(
                    ArtifactPath(gmWithSymbols, WebBuildProfile.GmStress,
                        WebBuildArtifactRole.DebugSymbols),
                    "gm-debug-symbols");
                WebBuild.ValidateBuiltArtifactSurface(
                    gmWithSymbols, WebBuildProfile.GmStress);

                var releaseWithSymbols = CreateValidOutput(
                    Path.Combine(root, "release-with-symbols"),
                    WebBuildProfile.Release);
                WriteArtifact(
                    ArtifactPath(releaseWithSymbols, WebBuildProfile.Release,
                        WebBuildArtifactRole.DebugSymbols),
                    "release-debug-symbols");
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        releaseWithSymbols, WebBuildProfile.Release),
                    "release pins debug symbols Off and rejects a symbol artifact");
            });
        }

        private static void ValidateForbiddenTokensAcrossReleaseArtifacts()
        {
            var cases = new[]
            {
                new ForbiddenCase(WebBuildArtifactRole.Index,
                    "fruitDefensePendingUnityInstance"),
                new ForbiddenCase(WebBuildArtifactRole.Index,
                    WebBuild.LegacyAcceptanceHostQueryPredicate),
                new ForbiddenCase(WebBuildArtifactRole.Loader,
                    "ConfigureAcceptanceFlow"),
                new ForbiddenCase(WebBuildArtifactRole.Data,
                    "AcceptanceSafeAreaDecorator"),
                new ForbiddenCase(WebBuildArtifactRole.Data,
                    "ShouldEnterAcceptanceBattle"),
                new ForbiddenCase(WebBuildArtifactRole.Data, "safeTop"),
                new ForbiddenCase(WebBuildArtifactRole.Data, "safeBottom"),
                new ForbiddenCase(WebBuildArtifactRole.Framework,
                    "fruitDefenseCombatFeedbackTelemetry"),
                new ForbiddenCase(WebBuildArtifactRole.Wasm,
                    "FruitDefensePublishCombatFeedbackTelemetry"),
            };
            WithTempRoot(root =>
            {
                foreach (var testCase in cases)
                {
                    var output = CreateValidOutput(
                        Path.Combine(root, testCase.Role.ToString()),
                        WebBuildProfile.Release);
                    AppendArtifactToken(output, WebBuildProfile.Release,
                        testCase.Role, testCase.Token);
                    AssertThrows(
                        () => WebBuild.ValidateBuiltArtifactSurface(
                            output, WebBuildProfile.Release),
                        "release rejects forbidden token in exact artifact role "
                        + testCase.Role);
                }

                const string boundaryToken = "ConfigureAcceptanceState";
                var boundaryOutput = CreateValidOutput(
                    Path.Combine(root, "chunk-boundary"), WebBuildProfile.Release);
                RewriteArtifact(boundaryOutput, WebBuildProfile.Release,
                    WebBuildArtifactRole.Loader,
                    ignored => new string('x', 64 * 1024 - boundaryToken.Length / 2)
                        + boundaryToken);
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        boundaryOutput, WebBuildProfile.Release),
                    "release token scan detects a forbidden token across read chunks");

                var gmOutput = CreateValidOutput(
                    Path.Combine(root, "gm-forbidden"), WebBuildProfile.GmStress);
                AppendArtifactToken(gmOutput, WebBuildProfile.GmStress,
                    WebBuildArtifactRole.Framework, "FruitDefenseAcceptanceReady");
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        gmOutput, WebBuildProfile.GmStress),
                    "GM rejects an acceptance bridge token instead of retaining a no-op shell");

                var gmInsetOutput = CreateValidOutput(
                    Path.Combine(root, "gm-inset"), WebBuildProfile.GmStress);
                AppendArtifactToken(gmInsetOutput, WebBuildProfile.GmStress,
                    WebBuildArtifactRole.Data, "AcceptanceSafeAreaDecorator");
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        gmInsetOutput, WebBuildProfile.GmStress),
                    "GM rejects the exact synthetic safe-area type surface");

                var gmLegacyRouteOutput = CreateValidOutput(
                    Path.Combine(root, "gm-legacy-route"), WebBuildProfile.GmStress);
                AppendArtifactToken(gmLegacyRouteOutput, WebBuildProfile.GmStress,
                    WebBuildArtifactRole.Index,
                    WebBuild.LegacyAcceptanceHostQueryPredicate);
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        gmLegacyRouteOutput, WebBuildProfile.GmStress),
                    "GM rejects the legacy direct acceptance query bootstrap");

                var acceptanceLegacyRouteOutput = CreateValidOutput(
                    Path.Combine(root, "acceptance-legacy-route"),
                    WebBuildProfile.Acceptance);
                AppendArtifactToken(
                    acceptanceLegacyRouteOutput, WebBuildProfile.Acceptance,
                    WebBuildArtifactRole.Index,
                    WebBuild.LegacyAcceptanceHostQueryPredicate);
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        acceptanceLegacyRouteOutput, WebBuildProfile.Acceptance),
                    "acceptance rejects the obsolete direct query-routing predicate");

                var gmSymbolOutput = CreateValidOutput(
                    Path.Combine(root, "gm-symbol-scan"), WebBuildProfile.GmStress);
                WriteArtifact(
                    ArtifactPath(gmSymbolOutput, WebBuildProfile.GmStress,
                        WebBuildArtifactRole.DebugSymbols),
                    "AcceptanceSafeAreaDecorator");
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        gmSymbolOutput, WebBuildProfile.GmStress),
                    "allowed GM debug-symbol artifact is still scanned for forbidden surface");
            });
        }

        private static void ValidateAcceptanceTokenArtifactOwnership()
        {
            var cases = new[]
            {
                new RequiredLocationCase(
                    WebBuildArtifactRole.Index,
                    WebBuildArtifactRole.Loader,
                    WebBuild.AcceptanceHostQueryPredicate),
                new RequiredLocationCase(
                    WebBuildArtifactRole.Data,
                    WebBuildArtifactRole.Framework,
                    "ConfigureAcceptanceFlow"),
                new RequiredLocationCase(
                    WebBuildArtifactRole.Framework,
                    WebBuildArtifactRole.Data,
                    "fruitDefenseAcceptanceIdentity"),
                new RequiredLocationCase(
                    WebBuildArtifactRole.Wasm,
                    WebBuildArtifactRole.Loader,
                    "FruitDefenseAcceptanceReady"),
            };
            WithTempRoot(root =>
            {
                foreach (var testCase in cases)
                {
                    var output = CreateValidOutput(
                        Path.Combine(root, testCase.RequiredRole.ToString()),
                        WebBuildProfile.Acceptance);
                    RemoveArtifactToken(output, WebBuildProfile.Acceptance,
                        testCase.RequiredRole, testCase.Token);
                    AppendArtifactToken(output, WebBuildProfile.Acceptance,
                        testCase.WrongRole, testCase.Token);
                    AssertThrows(
                        () => WebBuild.ValidateBuiltArtifactSurface(
                            output, WebBuildProfile.Acceptance),
                        "acceptance required token cannot move from "
                        + testCase.RequiredRole + " to " + testCase.WrongRole);
                }
            });
        }

        private static void ValidateFailClosedArtifactReads()
        {
            WithTempRoot(root =>
            {
                var corrupt = CreateValidOutput(
                    Path.Combine(root, "corrupt-brotli"), WebBuildProfile.Release);
                File.WriteAllBytes(
                    ArtifactPath(corrupt, WebBuildProfile.Release,
                        WebBuildArtifactRole.Data),
                    Encoding.ASCII.GetBytes("not-a-brotli-stream"));
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        corrupt, WebBuildProfile.Release),
                    "corrupt Brotli payload fails closed");

                var missing = CreateValidOutput(
                    Path.Combine(root, "missing-framework"), WebBuildProfile.Release);
                File.Delete(ArtifactPath(missing, WebBuildProfile.Release,
                    WebBuildArtifactRole.Framework));
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        missing, WebBuildProfile.Release),
                    "missing build artifact fails closed");

                var invalidIndex = CreateValidOutput(
                    Path.Combine(root, "invalid-index"), WebBuildProfile.Release);
                File.WriteAllBytes(
                    ArtifactPath(invalidIndex, WebBuildProfile.Release,
                        WebBuildArtifactRole.Index),
                    new byte[] { 0xc3, 0x28 });
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        invalidIndex, WebBuildProfile.Release),
                    "invalid UTF-8 host index fails closed");

                var invalidLoader = CreateValidOutput(
                    Path.Combine(root, "invalid-loader"), WebBuildProfile.Release);
                WriteArtifactBytes(
                    ArtifactPath(invalidLoader, WebBuildProfile.Release,
                        WebBuildArtifactRole.Loader),
                    new byte[] { 0xc3, 0x28 });
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        invalidLoader, WebBuildProfile.Release),
                    "invalid UTF-8 loader fails closed");

                var invalidFramework = CreateValidOutput(
                    Path.Combine(root, "invalid-framework"), WebBuildProfile.Release);
                WriteArtifactBytes(
                    ArtifactPath(invalidFramework, WebBuildProfile.Release,
                        WebBuildArtifactRole.Framework),
                    new byte[] { 0xc3, 0x28 });
                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        invalidFramework, WebBuildProfile.Release),
                    "invalid UTF-8 decompressed framework fails closed");

                AssertThrows(
                    () => WebBuild.ValidateBuiltArtifactSurface(
                        Path.Combine(root, "absent"), WebBuildProfile.Release),
                    "absent build output fails closed");
            });
        }

        private static string CreateValidOutput(
            string output,
            WebBuildProfile profile)
        {
            Directory.CreateDirectory(Path.Combine(output, "Build"));
            var index = WebBuild.InjectBuildProfileIdentity(BaseIndex, profile);
            index = WebBuild.InjectAcceptanceUnityInstanceBootstrap(index, profile);
            File.WriteAllText(
                ArtifactPath(output, profile, WebBuildArtifactRole.Index), index,
                new UTF8Encoding(false));
            foreach (var role in new[]
                     {
                         WebBuildArtifactRole.Loader,
                         WebBuildArtifactRole.Data,
                         WebBuildArtifactRole.Framework,
                         WebBuildArtifactRole.Wasm,
                     })
            {
                var requiredTokens = WebBuild.CreateRequiredAcceptanceTokens(role);
                var content = profile.IncludesAcceptanceRuntime
                              && requiredTokens.Length > 0
                    ? string.Join("|", requiredTokens)
                    : profile.Identity + "-" + role;
                WriteArtifact(ArtifactPath(output, profile, role), content);
            }

            return output;
        }

        private static void AppendArtifactToken(
            string output,
            WebBuildProfile profile,
            WebBuildArtifactRole role,
            string token)
        {
            RewriteArtifact(output, profile, role,
                content => content + "|" + token);
        }

        private static void RemoveArtifactToken(
            string output,
            WebBuildProfile profile,
            WebBuildArtifactRole role,
            string token)
        {
            RewriteArtifact(output, profile, role, content =>
            {
                Assert(content.Contains(token, StringComparison.Ordinal),
                    "smoke fixture owns required token before removal: " + token);
                var tokenIndex = content.IndexOf(token, StringComparison.Ordinal);
                return content.Remove(tokenIndex, token.Length)
                    .Insert(tokenIndex, "removed-token");
            });
        }

        private static void RewriteArtifact(
            string output,
            WebBuildProfile profile,
            WebBuildArtifactRole role,
            Func<string, string> rewrite)
        {
            var path = ArtifactPath(output, profile, role);
            var content = ReadArtifact(path);
            WriteArtifact(path, rewrite(content));
        }

        private static string ReadArtifact(string path)
        {
            using var file = File.OpenRead(path);
            if (!path.EndsWith(".unityweb", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(
                    file, Encoding.UTF8, true, 1024, false);
                return reader.ReadToEnd();
            }

            using var decompressed = new BrotliStream(
                file, CompressionMode.Decompress, false);
            using var decompressedReader = new StreamReader(
                decompressed, Encoding.UTF8, true, 1024, false);
            return decompressedReader.ReadToEnd();
        }

        private static void WriteArtifact(string path, string content)
        {
            WriteArtifactBytes(path, Encoding.UTF8.GetBytes(content));
        }

        private static void WriteArtifactBytes(string path, byte[] bytes)
        {
            if (!path.EndsWith(".unityweb", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllBytes(path, bytes);
                return;
            }

            using var file = new FileStream(
                path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var compressed = new BrotliStream(
                file, System.IO.Compression.CompressionLevel.Optimal, false);
            compressed.Write(bytes, 0, bytes.Length);
        }

        private static string ArtifactPath(
            string output,
            WebBuildProfile profile,
            WebBuildArtifactRole role)
        {
            if (role == WebBuildArtifactRole.Index)
                return Path.Combine(output, "index.html");
            var suffix = role switch
            {
                WebBuildArtifactRole.Loader => ".loader.js",
                WebBuildArtifactRole.Data => profile.IsDevelopmentBuild
                    ? ".data"
                    : ".data.unityweb",
                WebBuildArtifactRole.Framework => profile.IsDevelopmentBuild
                    ? ".framework.js"
                    : ".framework.js.unityweb",
                WebBuildArtifactRole.Wasm => profile.IsDevelopmentBuild
                    ? ".wasm"
                    : ".wasm.unityweb",
                WebBuildArtifactRole.DebugSymbols => profile.IsDevelopmentBuild
                    ? ".symbols.json"
                    : ".symbols.json.unityweb",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
            };
            return Path.Combine(output, "Build", profile.Identity + suffix);
        }

        private static void WithTempRoot(Action<string> action)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "fruit-defense-web-artifact-surface-" + Guid.NewGuid().ToString("N"));
            try
            {
                action(root);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static int CountOccurrences(string source, string token)
        {
            var count = 0;
            var offset = 0;
            while ((offset = source.IndexOf(
                       token, offset, StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += token.Length;
            }

            return count;
        }

        private static void AssertThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(
                "WebGL artifact surface smoke failed: " + message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "WebGL artifact surface smoke failed: " + message);
        }

        private readonly struct ForbiddenCase
        {
            internal ForbiddenCase(WebBuildArtifactRole role, string token)
            {
                Role = role;
                Token = token;
            }

            internal WebBuildArtifactRole Role { get; }
            internal string Token { get; }
        }

        private readonly struct RequiredLocationCase
        {
            internal RequiredLocationCase(
                WebBuildArtifactRole requiredRole,
                WebBuildArtifactRole wrongRole,
                string token)
            {
                RequiredRole = requiredRole;
                WrongRole = wrongRole;
                Token = token;
            }

            internal WebBuildArtifactRole RequiredRole { get; }
            internal WebBuildArtifactRole WrongRole { get; }
            internal string Token { get; }
        }
    }
}
