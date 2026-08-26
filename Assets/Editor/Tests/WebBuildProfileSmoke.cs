using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class WebBuildProfileSmoke
    {
        public static void Run()
        {
            ValidateWebBuildSourceAuthority();
            ValidateImmutableProfiles();
            ValidateSharedReleaseInputs();
            ValidateHostProfileIdentity();
            ValidateStrictAcceptanceHostPredicate();
            ValidatePersistentDefineGuard();
            ValidatePersistentDefinePollutionGate();
            ValidateFailedOutputCleanup();
            WebBuildArtifactSurfaceSmoke.Run();
            Debug.Log("FRUIT_DEFENSE_WEB_BUILD_PROFILE_OK");
        }

        private static void ValidateWebBuildSourceAuthority()
        {
            const string declaration = "public static partial class WebBuild";
            var expectedNames = new[]
            {
                "WebBuild.ArtifactSurface.cs",
                "WebBuild.cs",
            };
            var sourceDirectory = "Assets/Editor/Tools";
            var actualPaths = Directory.GetFiles(
                    sourceDirectory, "WebBuild*.cs", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(
                    Path.GetFileName(path), "WebBuildProfile.cs",
                    StringComparison.Ordinal))
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .ToArray();
            Assert(actualPaths.Select(Path.GetFileName).SequenceEqual(expectedNames),
                "WebBuild source authority is exactly the fixed two-file set");

            foreach (var path in actualPaths)
            {
                Assert(File.Exists(path), "WebBuild source module exists: " + path);
                var source = File.ReadAllText(path);
                Assert(File.ReadAllLines(path).Length <= 900,
                    Path.GetFileName(path) + " stays at or below 900 lines");
                Assert(RuntimeUiSourceAuthority.CountCSharpPartialDeclarations(
                        source, declaration) == 1,
                    Path.GetFileName(path)
                    + " contains exactly one WebBuild partial declaration");
            }
        }

        private static void ValidateImmutableProfiles()
        {
            var release = WebBuildProfile.Release;
            var acceptance = WebBuildProfile.Acceptance;
            var gmStress = WebBuildProfile.GmStress;

            Assert(string.Equals(release.Identity, "release", StringComparison.Ordinal)
                && string.Equals(release.OutputDirectory, "Builds/WebGL",
                    StringComparison.Ordinal)
                && release.IsPublishable
                && !release.IsDevelopmentBuild
                && !release.IncludesAcceptanceRuntime
                && !release.AllowsDebugSymbolArtifact,
                "release profile owns the only publishable non-development output");
            Assert(string.Equals(acceptance.Identity, "acceptance",
                    StringComparison.Ordinal)
                && string.Equals(acceptance.OutputDirectory,
                    "Builds/WebGL-Acceptance", StringComparison.Ordinal)
                && !acceptance.IsPublishable
                && !acceptance.IsDevelopmentBuild
                && acceptance.IncludesAcceptanceRuntime
                && !acceptance.AllowsDebugSymbolArtifact,
                "acceptance profile owns its fixed non-publishable output");
            Assert(string.Equals(gmStress.Identity, "gm-stress",
                    StringComparison.Ordinal)
                && string.Equals(gmStress.OutputDirectory,
                    "Builds/WebGL-GM-Stress", StringComparison.Ordinal)
                && !gmStress.IsPublishable
                && gmStress.IsDevelopmentBuild
                && !gmStress.IncludesAcceptanceRuntime
                && gmStress.AllowsDebugSymbolArtifact,
                "existing GM stress profile remains development-only and non-publishable");
            Assert(new[] { release, acceptance, gmStress }.Count(profile =>
                    profile.IsPublishable) == 1,
                "exactly one WebGL profile is publishable");

            Assert(release.CreateExtraScriptingDefines().Length == 0,
                "release profile has no extra scripting define");
            var acceptanceDefines = acceptance.CreateExtraScriptingDefines();
            Assert(acceptanceDefines.SequenceEqual(new[]
                {
                    WebBuildProfile.AcceptanceScriptingDefine,
                }),
                "acceptance profile has exactly the acceptance scripting define");
            acceptanceDefines[0] = "MUTATED_BY_CALLER";
            Assert(string.Equals(
                    acceptance.CreateExtraScriptingDefines().Single(),
                    WebBuildProfile.AcceptanceScriptingDefine,
                    StringComparison.Ordinal),
                "profile scripting defines are returned as defensive copies");
        }

        private static void ValidateSharedReleaseInputs()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            Assert(scenes.Length > 0, "release scene list is available");

            var persistentDefinesBefore = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.WebGL);
            var releaseOptions = WebBuild.CreateBuildPlayerOptions(
                WebBuildProfile.Release,
                scenes,
                Path.GetFullPath(WebBuild.ReleaseOutputDirectory));
            var acceptanceOptions = WebBuild.CreateBuildPlayerOptions(
                WebBuildProfile.Acceptance,
                scenes,
                Path.GetFullPath(WebBuild.AcceptanceOutputDirectory));
            var persistentDefinesAfter = PlayerSettings.GetScriptingDefineSymbols(
                NamedBuildTarget.WebGL);

            Assert(releaseOptions.scenes.SequenceEqual(acceptanceOptions.scenes)
                && releaseOptions.scenes.SequenceEqual(scenes),
                "release and acceptance profiles use the same enabled release scenes");
            Assert(releaseOptions.target == BuildTarget.WebGL
                && acceptanceOptions.target == BuildTarget.WebGL,
                "release and acceptance profiles target WebGL");
            Assert(releaseOptions.options == BuildOptions.NoUniqueIdentifier
                && acceptanceOptions.options == BuildOptions.NoUniqueIdentifier,
                "release and acceptance profiles use identical non-development build options");
            Assert(releaseOptions.extraScriptingDefines.Length == 0
                && acceptanceOptions.extraScriptingDefines.SequenceEqual(new[]
                {
                    WebBuildProfile.AcceptanceScriptingDefine,
                }),
                "the acceptance define is the only compile-input difference");
            Assert(!string.Equals(releaseOptions.locationPathName,
                    acceptanceOptions.locationPathName,
                    StringComparison.OrdinalIgnoreCase),
                "release and acceptance outputs are distinct");
            Assert(string.Equals(persistentDefinesBefore, persistentDefinesAfter,
                    StringComparison.Ordinal),
                "constructing either profile leaves persistent scripting defines unchanged");
        }

        private static void ValidateHostProfileIdentity()
        {
            const string template = "<!doctype html><html><head></head><body></body></html>";
            var releaseIndex = WebBuild.InjectBuildProfileIdentity(
                template, WebBuildProfile.Release);
            var acceptanceIndex = WebBuild.InjectBuildProfileIdentity(
                template, WebBuildProfile.Acceptance);

            WebBuild.ValidateBuildProfileIdentity(
                releaseIndex, WebBuildProfile.Release, "release smoke index");
            WebBuild.ValidateBuildProfileIdentity(
                acceptanceIndex, WebBuildProfile.Acceptance, "acceptance smoke index");
            Assert(releaseIndex.Contains(
                    "<meta name=\"fruit-defense-build-profile\" content=\"release\">",
                    StringComparison.Ordinal),
                "release host declares the exact release profile identity");
            Assert(acceptanceIndex.Contains(
                    "<meta name=\"fruit-defense-build-profile\" content=\"acceptance\">",
                    StringComparison.Ordinal),
                "acceptance host declares the exact acceptance profile identity");

            AssertThrows(
                () => WebBuild.ValidateBuildProfileIdentity(
                    releaseIndex, WebBuildProfile.Acceptance, "wrong-profile smoke index"),
                "profile validation rejects the wrong expected identity");
            AssertThrows(
                () => WebBuild.InjectBuildProfileIdentity(
                    releaseIndex, WebBuildProfile.Release),
                "profile injection rejects an existing marker");

            var equivalentVariants = new[]
            {
                "<META NAME=\"FRUIT-DEFENSE-BUILD-PROFILE\" CONTENT=\"RELEASE\">",
                "<meta name='fruit-defense-build-profile' content='release'>",
                "<meta content=\"release\" name=\"fruit-defense-build-profile\">",
            };
            foreach (var variant in equivalentVariants)
            {
                var variantIndex = template.Replace("<head>", "<head>" + variant);
                AssertThrows(
                    () => WebBuild.ValidateBuildProfileIdentity(
                        variantIndex, WebBuildProfile.Release, "variant smoke index"),
                    "profile validation rejects non-canonical equivalent meta: " + variant);
                AssertThrows(
                    () => WebBuild.InjectBuildProfileIdentity(
                        variantIndex, WebBuildProfile.Release),
                    "profile injection rejects equivalent existing meta: " + variant);
                AssertThrows(
                    () => WebBuild.ValidateBuildProfileIdentity(
                        releaseIndex.Replace("</head>", variant + "</head>"),
                        WebBuildProfile.Release, "duplicate variant smoke index"),
                    "profile validation rejects canonical plus equivalent duplicate: "
                    + variant);
            }
        }

        private static void ValidatePersistentDefineGuard()
        {
            var fakePersistentDefines = "USER_DEFINED_SYMBOL";
            var expectedBuildFailure = new InvalidOperationException(
                "synthetic build failure");
            InvalidOperationException mutationFailure = null;
            try
            {
                WebBuild.RunWithPersistentScriptingDefineGuard(
                    () =>
                    {
                        fakePersistentDefines = "MUTATED_SYMBOL";
                        throw expectedBuildFailure;
                    },
                    () => fakePersistentDefines,
                    value => fakePersistentDefines = value);
            }
            catch (InvalidOperationException exception)
            {
                mutationFailure = exception;
            }

            Assert(mutationFailure != null
                && ReferenceEquals(mutationFailure.InnerException, expectedBuildFailure)
                && string.Equals(fakePersistentDefines, "USER_DEFINED_SYMBOL",
                    StringComparison.Ordinal),
                "failed build restores unexpected persistent define mutation and reports it");

            Exception observedBuildFailure = null;
            try
            {
                WebBuild.RunWithPersistentScriptingDefineGuard(
                    () => throw expectedBuildFailure,
                    () => fakePersistentDefines,
                    value => fakePersistentDefines = value);
            }
            catch (Exception exception)
            {
                observedBuildFailure = exception;
            }

            Assert(ReferenceEquals(observedBuildFailure, expectedBuildFailure)
                && string.Equals(fakePersistentDefines, "USER_DEFINED_SYMBOL",
                    StringComparison.Ordinal),
                "failed build with unchanged defines preserves the original failure");
        }

        private static void ValidateStrictAcceptanceHostPredicate()
        {
            Assert(string.Equals(
                    WebBuild.AcceptanceHostQueryPredicate,
                    "new URLSearchParams(window.location.search)"
                    + ".get('acceptance') === '1'",
                    StringComparison.Ordinal),
                "host bootstrap requires the exact acceptance value 1");
            Assert(!WebBuild.AcceptanceHostQueryPredicate.Contains(
                    ".has('acceptance')", StringComparison.Ordinal),
                "host bootstrap does not activate from key presence alone");
        }

        private static void ValidatePersistentDefinePollutionGate()
        {
            foreach (var profile in new[]
                     {
                         WebBuildProfile.Release,
                         WebBuildProfile.Acceptance,
                         WebBuildProfile.GmStress,
                     })
            {
                var buildCalled = false;
                AssertThrows(
                    () => WebBuild.RunIfPersistentDefinesClean(
                        profile,
                        () => buildCalled = true,
                        () => "USER_SYMBOL;FRUIT_DEFENSE_ACCEPTANCE;SECOND_SYMBOL"),
                    profile.Identity
                    + " rejects a persistent acceptance define before invoking the build");
                Assert(!buildCalled,
                    profile.Identity
                    + " does not invoke BuildPipeline when persistent defines are polluted");
            }

            var exactMatchBuildCalled = false;
            WebBuild.RunIfPersistentDefinesClean(
                WebBuildProfile.Release,
                () => exactMatchBuildCalled = true,
                () => "MY_FRUIT_DEFENSE_ACCEPTANCE;FRUIT_DEFENSE_ACCEPTANCE_EXTRA");
            Assert(exactMatchBuildCalled,
                "persistent define pollution uses exact delimiter-separated matching");
        }

        private static void ValidateFailedOutputCleanup()
        {
            var testRoot = Path.Combine(
                Path.GetTempPath(),
                "fruit-defense-web-build-profile-smoke-"
                + Guid.NewGuid().ToString("N"));
            var failedOutput = Path.Combine(testRoot, "failed-profile");
            var stalePreflightOutput = Path.Combine(testRoot, "stale-preflight-profile");
            var unrelatedOutput = Path.Combine(testRoot, "unrelated-profile");
            try
            {
                Directory.CreateDirectory(unrelatedOutput);
                File.WriteAllText(
                    Path.Combine(unrelatedOutput, "keep.txt"), "preserve");
                AssertThrows(
                    () => WebBuild.RunWithFailedOutputCleanup(
                        failedOutput,
                        () =>
                        {
                            Directory.CreateDirectory(failedOutput);
                            File.WriteAllText(
                                Path.Combine(failedOutput, "unverified.txt"),
                                "unverified");
                            throw new InvalidOperationException(
                                "synthetic post-build validation failure");
                        }),
                    "failed post-processing removes the exact unverified profile output");

                Assert(!Directory.Exists(failedOutput),
                    "failed profile output is absent after cleanup");
                Assert(File.Exists(Path.Combine(unrelatedOutput, "keep.txt")),
                    "failed profile cleanup preserves sibling profile and user directories");

                Directory.CreateDirectory(stalePreflightOutput);
                File.WriteAllText(
                    Path.Combine(stalePreflightOutput, "stale.txt"), "stale");
                var preflightBuildCalled = false;
                AssertThrows(
                    () => WebBuild.RunProfileBuildGuards(
                        WebBuildProfile.Release,
                        stalePreflightOutput,
                        () => preflightBuildCalled = true,
                        () => "FRUIT_DEFENSE_ACCEPTANCE"),
                    "persistent-define preflight failure removes stale exact output");
                Assert(!preflightBuildCalled
                    && !Directory.Exists(stalePreflightOutput),
                    "preflight pollution never builds and cannot leave stale release output");
                Assert(File.Exists(Path.Combine(unrelatedOutput, "keep.txt")),
                    "preflight cleanup preserves sibling profile and user directories");
            }
            finally
            {
                if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
            }
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
                "WebGL build profile smoke failed: " + message);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "WebGL build profile smoke failed: " + message);
        }
    }
}
