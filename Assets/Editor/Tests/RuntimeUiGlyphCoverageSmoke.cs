using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiGlyphCoverageSmoke
    {
        private readonly struct FontSpec
        {
            public FontSpec(string path, int weight, string sha256)
            {
                Path = path;
                Weight = weight;
                Sha256 = sha256;
            }

            public string Path { get; }
            public int Weight { get; }
            public string Sha256 { get; }
        }

        private static readonly FontSpec[] FontSpecs =
        {
            new FontSpec(ProjectSetup.ReadingRuntimeUiFontPath, 400,
                "1fd3333be8e3496dbced280b559ea6f708abcfdb4e6f880bffaf67c8f9b9320d"),
            new FontSpec(ProjectSetup.DisplayRuntimeUiFontPath, 400,
                "dad00a57a3d3bb474abe7abf4a33e5c4e08742a900a00f7770ac37d723c1d7f3"),
        };

        public static void Run()
        {
            var glyphs = RuntimeUiChineseGlyphCoverage.RequiredGlyphs;
            var unique = new HashSet<char>();
            for (var index = 0; index < glyphs.Length; index++)
                Assert(unique.Add(glyphs[index]),
                    "authoritative glyph probe contains duplicate '" + glyphs[index] + "'");

            ValidatePlayerVisibleGlyphClosure(glyphs);

            foreach (var spec in FontSpecs)
                ValidateStaticFont(spec);

            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            foreach (RuntimeUiTypographyRole role in Enum.GetValues(
                         typeof(RuntimeUiTypographyRole)))
            {
                var typography = theme.Typography.For(role);
                var expectedPath = RuntimeUiQualityProfile.UsesDisplayFace(role)
                    ? ProjectSetup.DisplayRuntimeUiFontPath
                    : ProjectSetup.ReadingRuntimeUiFontPath;
                Assert(typography.Font != null
                    && AssetDatabase.GetAssetPath(typography.Font) == expectedPath,
                    role + " explicitly binds the approved static role font");
                var context = RuntimeUiDrawContext.Create(theme, 1f);
                var style = context.Styles.SingleLineText(role, TextAnchor.MiddleCenter);
                Assert(ReferenceEquals(style.font, typography.Font)
                    && style.fontStyle == FontStyle.Normal,
                    role + " measures and draws with its role font without synthesized styling");
            }

            ValidateLicenseAndSourceRecords();
            ValidateSourceAuthorities();

            Debug.Log("RUNTIME_UI_GLYPH_COVERAGE_OK glyphs=" + glyphs.Length
                + " unique=" + unique.Count + " fonts=" + FontSpecs.Length);
        }

        private static void ValidatePlayerVisibleGlyphClosure(string glyphs)
        {
            foreach (RuntimeUiCopyId id in Enum.GetValues(typeof(RuntimeUiCopyId)))
            {
                var copy = RuntimeUiCopyCatalog.Get(id);
                ValidateCopyGlyphClosure(glyphs, id + " finite copy", copy.Text);
            }

            var bundledCopy = RuntimeUiChineseGlyphCoverage
                .ReadBundledOutgameVisibleCopy();
            Assert(bundledCopy.Count > 0,
                "bundled outgame content contributes player-visible copy");
            for (var index = 0; index < bundledCopy.Count; index++)
                ValidateCopyGlyphClosure(glyphs,
                    "bundled outgame copy #" + index, bundledCopy[index]);
        }

        private static void ValidateCopyGlyphClosure(string glyphs,
            string owner, string copy)
        {
            for (var index = 0; index < copy.Length; index++)
            {
                var glyph = copy[index];
                if (char.IsControl(glyph)
                    || (glyph >= 32 && glyph <= 126))
                    continue;
                Assert(glyphs.IndexOf(glyph) >= 0,
                    owner + " contains glyph '" + glyph
                    + "' outside the packaged release authority");
            }
        }

        private static void ValidateStaticFont(FontSpec spec)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(spec.Path);
            Assert(font != null, "packaged release role font exists: " + spec.Path);
            Assert(RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph(font, out var missing),
                spec.Path + " is missing authoritative glyph '" + missing + "'");

            var absolutePath = ToAbsolute(spec.Path);
            var bytes = File.ReadAllBytes(absolutePath);
            Assert(!TryFindTable(bytes, "fvar", out _, out _),
                spec.Path + " is a truly static font without an fvar table");
            Assert(TryFindTable(bytes, "OS/2", out var os2Offset, out var os2Length)
                && os2Length >= 6 && ReadUInt16(bytes, os2Offset + 4) == spec.Weight,
                spec.Path + " declares OS/2.usWeightClass=" + spec.Weight);
            Assert(string.Equals(ComputeSha256(bytes), spec.Sha256,
                    StringComparison.Ordinal),
                spec.Path + " matches the deterministic output SHA-256");

            var meta = File.ReadAllText(absolutePath + ".meta");
            Assert(meta.Contains("includeFontData: 1")
                && meta.Contains("fallbackFontReferences: []")
                && meta.Contains("static-weight=" + spec.Weight),
                spec.Path + " packages font data without importer fallbacks");
        }

        private static void ValidateLicenseAndSourceRecords()
        {
            var licensePath = ToAbsolute("Assets/Resources/Fonts/OFL-NotoSansSC.txt");
            var displayLicensePath = ToAbsolute(
                "Assets/Resources/Fonts/OFL-SmileySans.txt");
            var readmePath = ToAbsolute("Assets/Resources/Fonts/README.md");
            Assert(File.Exists(licensePath) && File.Exists(displayLicensePath),
                "both SIL OFL licenses are packaged beside their role fonts");
            Assert(File.Exists(readmePath), "role-font source and hash record exists");
            var record = File.ReadAllText(readmePath);
            Assert(record.Contains("SIL Open Font License 1.1")
                && record.Contains(
                    "a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da")
                && record.Contains(
                    "b447d7e781f08bc95c4c9f23ba71ed2b8ebb639aa7184485c71c4ca5afcd25c4"),
                "font record identifies both licenses and pinned upstream SHA-256 values");
            foreach (var spec in FontSpecs)
                Assert(record.Contains(spec.Path) && record.Contains(spec.Sha256),
                    "font record identifies output and SHA-256: " + spec.Path);
        }

        private static void ValidateSourceAuthorities()
        {
            var projectSetup = File.ReadAllText(ToAbsolute(
                "Assets/Editor/Tools/ProjectSetup.cs"));
            var validator = RuntimeUiSourceAuthority.ReadVisualValidator();
            var themeSource = File.ReadAllText(ToAbsolute(
                "Assets/Scripts/UI/RuntimeUiTheme.cs"));
            var guiSource = File.ReadAllText(ToAbsolute(
                "Assets/Scripts/UI/RuntimeUiGui.cs"));
            var themeAsset = File.ReadAllText(ToAbsolute(
                ProjectSetup.ReleaseRuntimeUiThemePath));
            Assert(Count(projectSetup, "RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph") == 1,
                "ProjectSetup consumes the single glyph authority exactly once");
            Assert(Count(validator, "RuntimeUiChineseGlyphCoverage.TryFindMissingGlyph") == 1,
                "visual validator consumes the single glyph authority exactly once");
            Assert(!projectSetup.Contains("session-control glyph")
                && !projectSetup.Contains("level-card glyph")
                && !validator.Contains("ChineseGlyphProbe"),
                "legacy duplicated glyph probes were removed");
            var legacyField = "packaged" + "Chinese" + "Font";
            Assert(!themeSource.Contains(legacyField)
                && !guiSource.Contains(char.ToUpperInvariant(legacyField[0])
                    + legacyField.Substring(1))
                && !themeAsset.Contains(legacyField + ":"),
                "legacy single-font field and access path are absent");
            Assert(!themeSource.Contains("FontStyle")
                && guiSource.Contains("fontStyle = FontStyle.Normal"),
                "static role weights replace synthesized font-style configuration");
        }

        private static bool TryFindTable(byte[] bytes, string tag,
            out int offset, out int length)
        {
            offset = 0;
            length = 0;
            if (bytes == null || bytes.Length < 12 || tag == null || tag.Length != 4)
                return false;
            var tableCount = ReadUInt16(bytes, 4);
            for (var index = 0; index < tableCount; index++)
            {
                var recordOffset = 12 + index * 16;
                if (recordOffset < 0 || recordOffset + 16 > bytes.Length)
                    return false;
                if (bytes[recordOffset] != tag[0]
                    || bytes[recordOffset + 1] != tag[1]
                    || bytes[recordOffset + 2] != tag[2]
                    || bytes[recordOffset + 3] != tag[3])
                    continue;
                var resolvedOffset = ReadUInt32(bytes, recordOffset + 8);
                var resolvedLength = ReadUInt32(bytes, recordOffset + 12);
                if (resolvedOffset > int.MaxValue || resolvedLength > int.MaxValue
                    || resolvedOffset + resolvedLength > bytes.Length)
                    return false;
                offset = (int)resolvedOffset;
                length = (int)resolvedLength;
                return true;
            }
            return false;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
        }

        private static uint ReadUInt32(byte[] bytes, int offset)
        {
            return ((uint)bytes[offset] << 24)
                | ((uint)bytes[offset + 1] << 16)
                | ((uint)bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
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
                throw new InvalidOperationException(
                    "Runtime UI glyph coverage smoke failed: " + message);
        }
    }
}
