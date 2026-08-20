using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiGlyphCoverageSmoke
    {
        private const string FontPath = "Assets/Resources/Fonts/NotoSansSC-UI.ttf";

        public static void Run()
        {
            var glyphs = RuntimeUiChineseGlyphCoverage.RequiredGlyphs;
            var unique = new HashSet<char>();
            for (var index = 0; index < glyphs.Length; index++)
                Assert(unique.Add(glyphs[index]),
                    "authoritative glyph probe contains duplicate '" + glyphs[index] + "'");

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert(font != null, "packaged release font exists");
            Assert(RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph(font, out var missing),
                "packaged release font is missing authoritative glyph '" + missing + "'");

            var projectSetup = File.ReadAllText(ToAbsolute(
                "Assets/Editor/Tools/ProjectSetup.cs"));
            var validator = File.ReadAllText(ToAbsolute(
                "Assets/Editor/Tools/RuntimeUiVisualSystemValidator.cs"));
            Assert(Count(projectSetup, "RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph") == 1,
                "ProjectSetup consumes the single glyph authority exactly once");
            Assert(Count(validator, "RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph") == 1,
                "visual validator consumes the single glyph authority exactly once");
            Assert(!projectSetup.Contains("session-control glyph")
                && !projectSetup.Contains("level-card glyph")
                && !validator.Contains("ChineseGlyphProbe"),
                "legacy duplicated glyph probes were removed");

            Debug.Log("RUNTIME_UI_GLYPH_COVERAGE_OK glyphs=" + glyphs.Length
                + " unique=" + unique.Count + " font=" + FontPath);
        }

        private static int Count(string source, string value)
        {
            var count = 0;
            var offset = 0;
            while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += value.Length;
            }
            return count;
        }

        private static string ToAbsolute(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Runtime UI glyph coverage smoke failed: " + message);
        }
    }
}
