using System;
using UnityEditor;

namespace FruitDefense.Editor
{
    internal sealed class WebBuildProfile
    {
        internal const string AcceptanceScriptingDefine = "FRUIT_DEFENSE_ACCEPTANCE";

        internal static WebBuildProfile Release { get; } = new WebBuildProfile(
            "release",
            WebBuild.ReleaseOutputDirectory,
            true,
            false,
            false,
            false,
            Array.Empty<string>());

        internal static WebBuildProfile Acceptance { get; } = new WebBuildProfile(
            "acceptance",
            WebBuild.AcceptanceOutputDirectory,
            false,
            false,
            true,
            false,
            new[] { AcceptanceScriptingDefine });

        internal static WebBuildProfile GmStress { get; } = new WebBuildProfile(
            "gm-stress",
            WebBuild.GmStressOutputDirectory,
            false,
            true,
            false,
            true,
            Array.Empty<string>());

        private readonly string[] _extraScriptingDefines;

        private WebBuildProfile(
            string identity,
            string outputDirectory,
            bool isPublishable,
            bool isDevelopmentBuild,
            bool includesAcceptanceRuntime,
            bool allowsDebugSymbolArtifact,
            string[] extraScriptingDefines)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("WebGL build profile identity is required.",
                    nameof(identity));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("WebGL build profile output is required.",
                    nameof(outputDirectory));

            Identity = identity;
            OutputDirectory = outputDirectory;
            IsPublishable = isPublishable;
            IsDevelopmentBuild = isDevelopmentBuild;
            _extraScriptingDefines = extraScriptingDefines == null
                ? Array.Empty<string>()
                : (string[])extraScriptingDefines.Clone();
            var hasAcceptanceDefine = Array.IndexOf(
                _extraScriptingDefines, AcceptanceScriptingDefine) >= 0;
            if (hasAcceptanceDefine != includesAcceptanceRuntime)
                throw new ArgumentException(
                    "Acceptance runtime ownership must match the exact scripting define.",
                    nameof(includesAcceptanceRuntime));
            IncludesAcceptanceRuntime = includesAcceptanceRuntime;
            AllowsDebugSymbolArtifact = allowsDebugSymbolArtifact;
        }

        internal string Identity { get; }
        internal string OutputDirectory { get; }
        internal bool IsPublishable { get; }
        internal bool IsDevelopmentBuild { get; }
        internal bool IncludesAcceptanceRuntime { get; }
        internal bool AllowsDebugSymbolArtifact { get; }

        internal BuildOptions AdditionalBuildOptions => IsDevelopmentBuild
            ? BuildOptions.Development
            : BuildOptions.None;

        internal string[] CreateExtraScriptingDefines()
        {
            return (string[])_extraScriptingDefines.Clone();
        }
    }
}
