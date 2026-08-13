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
            ValidateDeterministicTopologyRepeat();
            ValidateProvenanceGuards();
            SquareTerrainArtGenerator.GenerateAvailableSquareAssets();
            var report = SquareTerrainArtValidator.ValidateGeneratedAssetsInternal(true);
            SquareTerrainEvidence.RenderReviewEvidence();
            var absolute = SquareTerrainArtGenerator.AbsolutePath(EvidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute,
                "Square terrain art smoke passed.\n"
                + "tileSize=" + report.tileSize + "\n"
                + "atlasSize=" + report.atlasSize + "\n"
                + "horizontalCompatiblePairs=" + report.horizontalCompatiblePairs + "\n"
                + "verticalCompatiblePairs=" + report.verticalCompatiblePairs + "\n"
                + "mask05Components=" + report.mask05Components + "\n"
                + "mask10Components=" + report.mask10Components + "\n"
                + "paintedEdgeMask05Components="
                    + report.paintedEdgeMask05Components + "\n"
                + "paintedEdgeMask10Components="
                    + report.paintedEdgeMask10Components + "\n"
                + "diagonalGrassComponentsSeparated="
                    + report.diagonalGrassComponentsSeparated + "\n"
                + "isolatedCellRoundedSquare=" + report.isolatedCellRoundedSquare + "\n"
                + "stripsTurnsHolesValid=" + report.stripsTurnsHolesValid + "\n"
                + "provenanceValid=" + report.provenanceValid + "\n"
                + "edgeRgbComesFromGrassBase="
                    + report.edgeRgbComesFromGrassBase + "\n"
                + "grassLandformUsesBaseTexture="
                    + report.grassLandformUsesBaseTexture + "\n"
                + "grassLandformColorCount=" + report.grassLandformColorCount + "\n"
                + "outsideOpaquePixelCount=" + report.outsideOpaquePixelCount + "\n"
                + "maximumOutsideDepthPixels=" + report.maximumOutsideDepthPixels + "\n"
                + "editorScaleOutsideFeatherPixels="
                    + report.editorScaleOutsideFeatherPixels + "\n"
                + "portraitScaleOutsideFeatherPixels="
                    + report.portraitScaleOutsideFeatherPixels + "\n"
                + "runtimeMipmapsEnabled=" + report.runtimeMipmapsEnabled + "\n"
                + "minimumBoundaryCoveragePermille="
                    + report.minimumBoundaryCoveragePermille + "\n"
                + "minimumStraightInsideDepthPixels="
                    + report.minimumStraightInsideDepthPixels + "\n"
                + "minimumStraightOutsideDepthPixels="
                    + report.minimumStraightOutsideDepthPixels + "\n"
                + "boundaryCoveragePermilleByMask="
                    + string.Join(",", report.boundaryCoveragePermilleByMask) + "\n"
                + "medianInsideDepthByMask="
                    + string.Join(",", report.medianInsideDepthByMask) + "\n"
                + "medianOutsideDepthByMask="
                    + string.Join(",", report.medianOutsideDepthByMask) + "\n"
                + "grassPixelCountByMask="
                    + string.Join(",", report.grassPixelCountByMask) + "\n"
                + "soilPixelCountByMask="
                    + string.Join(",", report.soilPixelCountByMask) + "\n"
                + "darkContactPixelCountByMask="
                    + string.Join(",", report.darkContactPixelCountByMask) + "\n"
                + "semiTransparentPixelCountByMask="
                    + string.Join(",", report.semiTransparentPixelCountByMask) + "\n"
                + "minimumSemiTransparentPixels="
                    + report.minimumSemiTransparentPixels + "\n"
                + "soilBaseSha256=" + report.baseSoilSha256 + "\n"
                + "approvedStyleReferenceSha256="
                    + report.approvedStyleReferenceSha256 + "\n"
                + "rawImagegenSha256=" + report.rawImagegenSha256 + "\n"
                + "rejectedAttemptSha256=" + report.rejectedAttemptSha256 + "\n"
                + "attempt01PromptSha256=" + report.attempt01PromptSha256 + "\n"
                + "attempt02PromptSha256=" + report.attempt02PromptSha256 + "\n"
                + "continuousRibbonSha256=" + report.continuousRibbonSha256 + "\n"
                + "grassLandformSha256=" + report.grassLandformSha256 + "\n"
                + "soilLandformSha256=" + report.soilLandformSha256 + "\n"
                + "stoneRoadLandformSha256=" + report.stoneRoadLandformSha256 + "\n"
                + "paintedEdgeSha256=" + report.paintedEdgeSha256 + "\n");
            Debug.Log("Square terrain art smoke passed: " + EvidencePath);
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
