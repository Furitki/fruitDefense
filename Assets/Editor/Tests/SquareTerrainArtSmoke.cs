using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class SquareTerrainArtSmoke
    {
        public const string EvidencePath =
            "Builds/Evidence/terrain-contours/square-contour-smoke.log";

        public static void Run()
        {
            ValidateCanonicalTextHashing();
            ValidateDeterministicTopologyRepeat();
            ValidateProvenanceGuards();
            SquareTerrainArtValidator.ValidateGeneratedAssetsInternal(true);
            Debug.Log("Square terrain art smoke passed against the committed authored assets.");
        }

        private static void ValidateProvenanceGuards()
        {
            var path = SquareTerrainArtGenerator.AbsolutePath(
                SquareTerrainArtProfile.ContinuousRibbonProvenancePath);
            var json = File.ReadAllText(path);
            SquareTerrainArtValidator.ValidateProvenanceJson(json);
            ExpectProvenanceRejected(json.Replace(
                SquareTerrainArtValidator.ProvenanceSchema,
                SquareTerrainArtValidator.ProvenanceSchema + ".tampered"), "schema");
            ExpectProvenanceRejected(json.Replace(
                SquareTerrainArtValidator.ApprovedStyleReferenceSha256,
                new string('0', 64)), "reference hash");
            ExpectProvenanceRejected(json.Replace(
                "\"width\": 685", "\"width\": 684"), "reference dimensions");
            ExpectProvenanceRejected(json.Replace(
                SquareTerrainArtProfile.ContinuousRibbonPath,
                SquareTerrainArtProfile.ContinuousRibbonPath + ".missing"), "ribbon path");
            ExpectProvenanceRejected(json.Replace(
                SquareTerrainArtValidator.ContinuousRibbonSha256,
                new string('1', 64)), "ribbon hash");
            var ribbonDimensions = JsonUtility.FromJson<
                SquareTerrainArtValidator.ContinuousProvenance>(json);
            ribbonDimensions.generation.retainedOutput.width = 2171;
            ExpectProvenanceRejected(JsonUtility.ToJson(ribbonDimensions),
                "ribbon dimensions");
            ExpectProvenanceRejected(json.Replace(
                "\"toolName\": \"image_gen__imagegen\"",
                "\"toolName\": \"unknown\""), "tool identity");
            ExpectProvenanceRejected(json.Replace(
                "\"continuousRibbonAcceptedAsPaintMaterial\": true",
                "\"continuousRibbonAcceptedAsPaintMaterial\": false"), "paint acceptance");
            ExpectProvenanceRejected(json.Replace(
                "\"atlasTopologyAcceptedDirectly\": false",
                "\"atlasTopologyAcceptedDirectly\": true"), "direct topology rejection");
            ExpectProvenanceRejected(json.Replace(
                "\"transitionBandPixels\": 16", "\"transitionBandPixels\": 15"),
                "packaging contract");
            ExpectProvenanceRejected(json.Replace(
                "\"exactBaseGrassRgbOnly\": true",
                "\"exactBaseGrassRgbOnly\": false"),
                "exact base-grass RGB contract");
            ExpectProvenanceRejected(json.Replace(
                "\"deterministicAlphaModulationOnly\": true",
                "\"deterministicAlphaModulationOnly\": false"),
                "deterministic alpha-modulation contract");
            ExpectProvenanceRejected(json.Replace(
                SquareTerrainArtProfile.SoilBaseSourcePath,
                SquareTerrainArtProfile.SoilBaseSourcePath + ".missing"),
                "base-soil material path");
            ExpectProvenanceRejected(json.Replace(
                "connected-boundary-source-lip-profile-plus-base-grass-feather-v7",
                "per-tile-random-remap"), "continuous paint-sampling contract");
            ExpectProvenanceRejected(json.Replace(
                "\"grassFeatherOutsideMaxPixels\": 7",
                "\"grassFeatherOutsideMaxPixels\": 8"),
                "narrow top-down feather contract");
        }

        private static void ValidateCanonicalTextHashing()
        {
            var lfHash = SquareTerrainArtValidator.HashCanonicalText("line one\nline two\n");
            var crlfHash = SquareTerrainArtValidator.HashCanonicalText("line one\r\nline two\r\n");
            var crHash = SquareTerrainArtValidator.HashCanonicalText("line one\rline two\r");
            if (!string.Equals(lfHash, crlfHash, StringComparison.Ordinal)
                || !string.Equals(lfHash, crHash, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Square provenance text hashing depends on checkout line endings.");
        }

        private static void ExpectProvenanceRejected(string json, string label)
        {
            try
            {
                SquareTerrainArtValidator.ValidateProvenanceJson(json);
            }
            catch (Exception)
            {
                return;
            }
            throw new InvalidOperationException("Square provenance smoke did not reject tampered "
                + label + ".");
        }

        private static void ValidateDeterministicTopologyRepeat()
        {
            var first = SquareTerrainArtGenerator.BuildAllTopologyMasks();
            var second = SquareTerrainArtGenerator.BuildAllTopologyMasks();
            if (first.Length != second.Length)
                throw new InvalidOperationException("Square topology repeat changed mask count.");
            for (var mask = 0; mask < first.Length; mask++)
            {
                if (first[mask].Length != second[mask].Length)
                    throw new InvalidOperationException("Square topology repeat changed tile size.");
                for (var index = 0; index < first[mask].Length; index++)
                {
                    var a = first[mask][index];
                    var b = second[mask][index];
                    if (a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a)
                        throw new InvalidOperationException("Square topology is not deterministic at mask "
                            + mask + ", pixel " + index + ".");
                }
            }
        }
    }
}
