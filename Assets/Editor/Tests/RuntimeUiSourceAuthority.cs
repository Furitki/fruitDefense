using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FruitDefense.Editor
{
    internal static class RuntimeUiSourceAuthority
    {
        private const int MaximumModuleLineCount = 900;
        private static bool _cSharpStructuralScannerValidated;

        private static readonly string[] RuntimeGuiFileNames =
        {
            "RuntimeUiGui.cs",
            "RuntimeUiGui.ActionsAndMetrics.cs",
            "RuntimeUiGui.Art.cs",
            "RuntimeUiGui.Hub.cs",
            "RuntimeUiGui.TextAndStatus.cs",
        };

        private static readonly string[] FruitDefenseGameFileNames =
        {
            "FruitDefenseGame.cs",
            "FruitDefenseGame.Validation.cs",
            "FruitDefenseGame.Acceptance.cs",
            "FruitDefenseGame.AcceptanceFeedback.cs",
            "FruitDefenseGame.Interaction.cs",
            "FruitDefenseGame.BattlefieldRendering.cs",
            "FruitDefenseGame.ControlsAndOverlays.cs",
            "FruitDefenseGame.SpriteRendering.cs",
        };

        private static readonly string[] VisualValidatorFileNames =
        {
            "RuntimeUiVisualSystemValidator.cs",
            "RuntimeUiVisualSystemValidator.FixedPrimaryAction.cs",
            "RuntimeUiVisualSystemValidator.Manifest.cs",
            "RuntimeUiVisualSystemValidator.PixelGeometry.cs",
            "RuntimeUiVisualSystemValidator.PixelQuality.cs",
            "RuntimeUiVisualSystemValidator.RasterTooling.cs",
            "RuntimeUiVisualSystemValidator.ReleaseBoundary.cs",
            "RuntimeUiVisualSystemValidator.Theme.cs",
        };

        private static readonly string[] AcceptanceModuleLoadOrder =
        {
            "geometry.ps1",
            "hub-matrix.ps1",
            "transport.ps1",
            "evidence-helpers.ps1",
            "image-analysis.ps1",
            "image-presentation-analysis.ps1",
            "settlement-ink-analysis.ps1",
            "settlement-optical-analysis.ps1",
            "self-check.ps1",
            "run-hub.ps1",
            "run-hub-loop.ps1",
            "run-shell.ps1",
            "run-flow.ps1",
            "run-combat.ps1",
            "run-direct.ps1",
            "run-cache.ps1",
        };

        internal static string ReadRuntimeGui()
        {
            return ReadFixedSet(Path.Combine(Application.dataPath, "Scripts/UI"),
                "RuntimeUiGui*.cs", RuntimeGuiFileNames,
                "public static partial class RuntimeUiGui");
        }

        internal static string ReadFruitDefenseGame()
        {
            return ReadFixedSet(Path.Combine(Application.dataPath, "Scripts"),
                "FruitDefenseGame*.cs", FruitDefenseGameFileNames,
                "public sealed partial class FruitDefenseGame");
        }

        internal static string ReadVisualValidator()
        {
            return ReadFixedSet(Path.Combine(Application.dataPath, "Editor/Tools"),
                "RuntimeUiVisualSystemValidator*.cs", VisualValidatorFileNames,
                "public static partial class RuntimeUiVisualSystemValidator");
        }

        internal static string ReadAcceptanceRunner()
        {
            var scriptsDirectory = Path.GetFullPath(Path.Combine(
                Application.dataPath, "../scripts"));
            var entrySource = ReadBoundedSource(Path.Combine(scriptsDirectory,
                "accept-webgl-portrait.ps1"));
            var probeSource = ReadBoundedSource(Path.Combine(scriptsDirectory,
                "webgl-build-profile-probe.ps1"));
            Require(Count(entrySource,
                    "$profileProbeScript = Join-Path $PSScriptRoot 'webgl-build-profile-probe.ps1'") == 1
                && Count(entrySource, ". $profileProbeScript") == 1,
                "acceptance entry point loads the fixed build-profile probe exactly once");

            var moduleListStart = entrySource.IndexOf("$acceptanceModules = @(",
                StringComparison.Ordinal);
            var moduleLoopStart = moduleListStart < 0
                ? -1
                : entrySource.IndexOf(
                    "foreach ($acceptanceModule in $acceptanceModules)",
                    moduleListStart, StringComparison.Ordinal);
            Require(moduleListStart >= 0 && moduleLoopStart > moduleListStart,
                "acceptance entry point has one readable module load graph");
            var moduleListSource = entrySource.Substring(moduleListStart,
                moduleLoopStart - moduleListStart);
            var matches = Regex.Matches(moduleListSource, "'([^']+\\.ps1)'",
                RegexOptions.CultureInvariant);
            Require(matches.Count == AcceptanceModuleLoadOrder.Length,
                "acceptance entry point loads the complete fixed module graph");
            for (var index = 0; index < matches.Count; index++)
                Require(string.Equals(matches[index].Groups[1].Value,
                        AcceptanceModuleLoadOrder[index], StringComparison.Ordinal),
                    "acceptance module load order changed at " + index);
            Require(Count(entrySource,
                    ". (Join-Path $acceptanceModuleRoot $acceptanceModule)") == 1,
                "acceptance entry point imports its module graph exactly once");

            var modulesSource = ReadFixedSet(
                Path.Combine(scriptsDirectory, "webgl-acceptance"), "*.ps1",
                AcceptanceModuleLoadOrder, null);
            var combined = string.Join(Environment.NewLine,
                entrySource, probeSource, modulesSource);
            Require(Count(entrySource, "[switch]$HubVisual") == 1
                && Count(entrySource, "if ($HubVisual) {") == 1
                && Count(entrySource, "Invoke-HubVisualMode") == 1
                && Count(modulesSource,
                    "function Invoke-HubVisualMode") == 1,
                "acceptance runner owns one fixed HubVisual switch and one run-hub implementation");
            var functionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var functionMatches = Regex.Matches(combined,
                @"(?m)^\s*function\s+([A-Za-z0-9_-]+)\s*\{",
                RegexOptions.CultureInvariant);
            for (var index = 0; index < functionMatches.Count; index++)
                Require(functionNames.Add(functionMatches[index].Groups[1].Value),
                    "acceptance function is defined more than once: "
                    + functionMatches[index].Groups[1].Value);
            Require(functionNames.Count > 0,
                "acceptance source authority discovers executable functions");
            return combined;
        }

        private static string ReadFixedSet(string directory, string pattern,
            string[] expectedFileNames, string declaration)
        {
            var expectedPhysicalNames = (string[])expectedFileNames.Clone();
            Array.Sort(expectedPhysicalNames, StringComparer.Ordinal);
            var paths = Directory.GetFiles(directory, pattern,
                SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            Require(paths.Length == expectedPhysicalNames.Length,
                pattern + " source authority has the complete fixed file set");
            for (var index = 0; index < paths.Length; index++)
            {
                Require(string.Equals(Path.GetFileName(paths[index]),
                        expectedPhysicalNames[index],
                        StringComparison.Ordinal),
                    pattern + " source authority includes only the approved file at "
                    + index);
            }

            var sources = new string[expectedFileNames.Length];
            for (var index = 0; index < expectedFileNames.Length; index++)
            {
                sources[index] = ReadBoundedSource(Path.Combine(
                    directory, expectedFileNames[index]));
                if (!string.IsNullOrEmpty(declaration))
                    Require(CountCSharpPartialDeclarations(
                            sources[index], declaration) == 1,
                        expectedFileNames[index] + " declares its partial exactly once");
            }

            return string.Join(Environment.NewLine, sources);
        }

        private static string ReadBoundedSource(string path)
        {
            var lines = File.ReadAllLines(path);
            Require(lines.Length <= MaximumModuleLineCount,
                Path.GetFileName(path) + " exceeds the 900-line module boundary");
            return string.Join(Environment.NewLine, lines);
        }

        internal static int CountCSharpPartialDeclarations(
            string source, string declaration)
        {
            EnsureCSharpStructuralScannerValidated();
            return CountCSharpPartialDeclarationsUnchecked(source, declaration);
        }

        private static int CountCSharpPartialDeclarationsUnchecked(
            string source, string declaration)
        {
            var sanitized = SanitizeCSharpForStructuralChecks(source);
            var pattern = @"^[\t ]*" + Regex.Escape(declaration)
                + @"(?=[\t \r\n:{])";
            return Regex.Matches(sanitized, pattern,
                RegexOptions.Multiline | RegexOptions.CultureInvariant).Count;
        }

        private static void EnsureCSharpStructuralScannerValidated()
        {
            if (_cSharpStructuralScannerValidated) return;

            const string declaration =
                "public sealed partial class LexicalScannerProbe";
            var disguisedDeclarations = string.Join("\n", new[]
            {
                "// " + declaration,
                "/*\n" + declaration + "\n*/",
                "var ordinary = \"" + declaration + "\";",
                "var verbatim = @\"header\n" + declaration + "\nfooter\";",
                "var interpolated = $\"" + declaration
                    + " {Format(\"" + declaration + "\")}\";",
                "var interpolatedVerbatim = $@\"header\n" + declaration
                    + " {Format(@\"" + declaration + "\")}\";",
                "var alternateInterpolatedVerbatim = @$\"" + declaration
                    + " {Format(\"" + declaration + "\")}\";",
                "var quote = '\\'';",
                "var notAnchored = 1; " + declaration,
            });
            Require(Count(SanitizeCSharpForStructuralChecks(
                        disguisedDeclarations), declaration) == 1,
                "comments and literal contents are removed before declaration matching");
            Require(CountCSharpPartialDeclarationsUnchecked(
                    disguisedDeclarations, declaration) == 0,
                "comments, strings, characters, and inline tokens cannot impersonate a partial declaration");

            var oneRealDeclaration = disguisedDeclarations + "\n    "
                + declaration + " : ProbeBase\n    {\n    }\n";
            Require(CountCSharpPartialDeclarationsUnchecked(
                    oneRealDeclaration, declaration) == 1,
                "one anchored partial declaration remains visible after lexical sanitization");
            _cSharpStructuralScannerValidated = true;
        }

        private static string SanitizeCSharpForStructuralChecks(string source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var sanitized = source.ToCharArray();
            var index = 0;
            while (index < source.Length)
            {
                int end;
                if (StartsWith(source, index, "//"))
                    end = SkipLineComment(source, index + 2);
                else if (StartsWith(source, index, "/*"))
                    end = SkipBlockComment(source, index + 2);
                else if (TrySkipLiteral(source, index, out end))
                {
                }
                else
                {
                    index++;
                    continue;
                }

                BlankNonNewlineCharacters(sanitized, index, end);
                index = end;
            }

            return new string(sanitized);
        }

        private static bool TrySkipLiteral(string source, int index, out int end)
        {
            end = index;
            if (index >= source.Length) return false;

            if (StartsWith(source, index, "$@\"")
                || StartsWith(source, index, "@$\""))
            {
                end = SkipInterpolatedString(source, index + 2, true);
                return true;
            }
            if (StartsWith(source, index, "$\""))
            {
                end = SkipInterpolatedString(source, index + 1, false);
                return true;
            }
            if (StartsWith(source, index, "@\""))
            {
                end = SkipVerbatimString(source, index + 1);
                return true;
            }
            if (source[index] == '"')
            {
                end = SkipRegularString(source, index);
                return true;
            }
            if (source[index] == '\'')
            {
                end = SkipCharacter(source, index);
                return true;
            }

            return false;
        }

        private static int SkipRegularString(string source, int quoteIndex)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (source[index] == '\\')
                {
                    index = Math.Min(source.Length, index + 2);
                    continue;
                }
                if (source[index] == '"') return index + 1;
                index++;
            }
            return source.Length;
        }

        private static int SkipVerbatimString(string source, int quoteIndex)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (source[index] != '"')
                {
                    index++;
                    continue;
                }
                if (index + 1 < source.Length && source[index + 1] == '"')
                {
                    index += 2;
                    continue;
                }
                return index + 1;
            }
            return source.Length;
        }

        private static int SkipCharacter(string source, int quoteIndex)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (source[index] == '\\')
                {
                    index = Math.Min(source.Length, index + 2);
                    continue;
                }
                if (source[index] == '\'') return index + 1;
                index++;
            }
            return source.Length;
        }

        private static int SkipInterpolatedString(
            string source, int quoteIndex, bool verbatim)
        {
            var index = quoteIndex + 1;
            while (index < source.Length)
            {
                if (!verbatim && source[index] == '\\')
                {
                    index = Math.Min(source.Length, index + 2);
                    continue;
                }
                if (source[index] == '"')
                {
                    if (verbatim && index + 1 < source.Length
                        && source[index + 1] == '"')
                    {
                        index += 2;
                        continue;
                    }
                    return index + 1;
                }
                if (source[index] == '{')
                {
                    if (index + 1 < source.Length && source[index + 1] == '{')
                    {
                        index += 2;
                        continue;
                    }
                    index = SkipInterpolationHole(source, index + 1);
                    continue;
                }
                if (source[index] == '}' && index + 1 < source.Length
                    && source[index + 1] == '}')
                {
                    index += 2;
                    continue;
                }
                index++;
            }
            return source.Length;
        }

        private static int SkipInterpolationHole(string source, int index)
        {
            var depth = 1;
            while (index < source.Length)
            {
                if (StartsWith(source, index, "//"))
                {
                    index = SkipLineComment(source, index + 2);
                    continue;
                }
                if (StartsWith(source, index, "/*"))
                {
                    index = SkipBlockComment(source, index + 2);
                    continue;
                }
                if (TrySkipLiteral(source, index, out var literalEnd))
                {
                    index = literalEnd;
                    continue;
                }
                if (source[index] == '{')
                {
                    depth++;
                    index++;
                    continue;
                }
                if (source[index] == '}')
                {
                    depth--;
                    index++;
                    if (depth == 0) return index;
                    continue;
                }
                index++;
            }
            return source.Length;
        }

        private static int SkipLineComment(string source, int index)
        {
            while (index < source.Length
                   && source[index] != '\r' && source[index] != '\n')
                index++;
            return index;
        }

        private static int SkipBlockComment(string source, int index)
        {
            while (index < source.Length)
            {
                if (StartsWith(source, index, "*/")) return index + 2;
                index++;
            }
            return source.Length;
        }

        private static bool StartsWith(string source, int index, string token)
        {
            if (index < 0 || index + token.Length > source.Length) return false;
            return string.CompareOrdinal(source, index, token, 0, token.Length) == 0;
        }

        private static void BlankNonNewlineCharacters(
            char[] source, int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (source[index] != '\r' && source[index] != '\n')
                    source[index] = ' ';
            }
        }

        private static int Count(string source, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(token, index,
                       StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }

            return count;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Runtime UI source authority failed: " + message);
        }
    }
}
