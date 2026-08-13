using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class SquareTerrainArtValidator
    {
        private const string ValidatorVersion = "square-contour-top-down-transition-v8";
        internal const string ProvenanceSchema =
            "fruit-defense.square-terrain-continuous-ribbon-provenance.v7";
        internal const string ApprovedStyleReferenceSha256 =
            "be554e857249aa294b5152059962561448e545c2da4764eddd10105866e119e4";
        internal const string MachineTopologySha256 =
            "646e32b5929bb39e13ce3ffd2583a4990410302de5e878aa01ccb682dd194c15";
        internal const string Attempt01PromptSha256 =
            "a0918e2027b95e8c86023eaf17291e38c1adb8f15421d6a62ae1e0a2899ceb87";
        internal const string Attempt02PromptSha256 =
            "926530abc99f05966031ef4514e28d81cc49e819af39bd5bd3a5354dc3f6668f";
        internal const string Attempt01OutputSha256 =
            "498af109b2c092be51d68a59e577d8ec49831ab3286923dc295934aba193fdf5";
        internal const string Attempt02RawSha256 =
            "228856466f4357ad3a3722831987219525e141f385fdfaf5a5a16a2480a73963";
        internal const string CandidateSha256 =
            "83158a973fcb18e56f456214b21b545e58536c7a34cc2adc3eb624bdfc4c2f88";
        internal const string ContinuousRibbonSha256 =
            "9594f3aef2c04de548246dd5ef8f5290e51e84a0d7db45bf8bdfbe7bceeba07a";
        internal const string SoilBaseSha256 =
            "54de95aea887fb5e853d069f5d9f5186f8fa13a2ef985c3f1fb0c483b3c24019";
        internal const string GrassBaseSha256 =
            "e730d59a009b5037658c2960b0079ce5efd04197158a88d15cbbed23f134fc34";
        internal const string ContinuousRibbonPromptSha256 =
            "7246177094f3c62bcf8ac24f7b5f78f064a11dbee03379c3413c4a6ed5847dd2";
        internal const string RejectedSemanticBoardSha256 =
            "05b86f450d28f969738f2dc6246e9413ac9b152c8bcf390c170544074728387e";
        internal const string PreviousProvenanceSha256 =
            "77c2e68b217cd6a2850b4ea12aa4b040875f85c0f0f3bd54811e6fe2eb3f5836";

        [Serializable]
        internal sealed class ProvenanceImage
        {
            public string path;
            public string sha256;
            public int width;
            public int height;
            public string colorMode;
        }

        [Serializable]
        internal sealed class ImagegenToolArguments
        {
            public string[] referenced_image_paths;
        }

        [Serializable]
        internal sealed class ImagegenAttempt
        {
            public int attempt;
            public string toolName;
            public string promptPath;
            public string promptSha256;
            public ImagegenToolArguments toolArguments;
            public string[] retainedReferencedImages;
            public string toolOutputPath;
            public ProvenanceImage retainedOutput;
            public string decision;
            public bool paintSourceAccepted;
            public bool atlasTopologyAcceptedDirectly;
            public string reason;
        }

        [Serializable]
        internal sealed class ImagegenNormalization
        {
            public int sourceAttempt;
            public string operation;
        }

        [Serializable]
        internal sealed class ImagegenReview
        {
            public bool styleAcceptedAsPaintSource;
            public bool atlasTopologyAcceptedDirectly;
            public bool firstAttemptCheckerboardRejected;
            public string notes;
        }

        [Serializable]
        internal sealed class ImagegenPackaging
        {
            public int tileSize;
            public int atlasColumns;
            public int atlasRows;
            public string maskOrder;
            public int transitionBandPixels;
            public int paintOutsideDepthPixels;
            public int protectedSocketPixels;
            public string chromaKey;
            public int chromaTolerance;
            public bool guideOwnsTopology;
            public bool diagonalMasksRemainDisconnected;
            public bool scriptedReplacementArtwork;
            public bool directImagegenAtlasAllowed;
            public bool exactCandidateColorCopiesOnly;
            public string paintSampling;
        }

        [Serializable]
        internal sealed class ImagegenProvenance
        {
            public string schema;
            public string date;
            public ProvenanceImage styleReference;
            public ProvenanceImage machineTopology;
            public ImagegenAttempt[] attempts;
            public ProvenanceImage normalizedCandidate;
            public ImagegenNormalization normalization;
            public ImagegenReview review;
            public ImagegenPackaging packaging;
        }

        [Serializable]
        internal sealed class ContinuousGeneration
        {
            public string toolName;
            public string promptPath;
            public string promptSha256;
            public ImagegenToolArguments toolArguments;
            public string[] retainedReferencedImages;
            public string toolOutputPath;
            public ProvenanceImage retainedOutput;
            public string decision;
            public bool paintMaterialAccepted;
            public bool atlasTopologyAcceptedDirectly;
            public string reason;
        }

        [Serializable]
        internal sealed class ContinuousReview
        {
            public bool continuousRibbonAcceptedAsPaintMaterial;
            public bool continuousRibbonAcceptedAsLipProfile;
            public bool runtimeRibbonRgbAccepted;
            public bool atlasTopologyAcceptedDirectly;
            public bool rejectedSemanticSamplingBoardRetained;
            public string notes;
        }

        [Serializable]
        internal sealed class ContinuousPackaging
        {
            public int tileSize;
            public int transitionBandPixels;
            public int grassBlendInsidePixels;
            public int grassFeatherBasePixels;
            public int grassFeatherVariationPixels;
            public int grassFeatherOutsideMaxPixels;
            public int paintOutsideDepthPixels;
            public int protectedSocketPixels;
            public bool guideOwnsTopology;
            public bool diagonalMasksRemainDisconnected;
            public bool removeDetachedPaint;
            public bool exactBaseGrassRgbOnly;
            public bool deterministicAlphaModulationOnly;
            public int grassFeatherAlphaNear;
            public int grassFeatherAlphaFar;
            public bool importsDirectionalSoilOrShadow;
            public bool mipmapsRequired;
            public bool interpolationAllowed;
            public bool perPixelRandomSampling;
            public bool semanticColorListSampling;
            public bool tileableGrassSurface;
            public string tangentPhaseMode;
            public bool grassLipDetectedPerSourceColumn;
            public int grassLipOffsetMinPixels;
            public int grassLipOffsetMaxPixels;
            public int grassDripMaxDepthPixels;
            public int maximumTransparentLipGapPixels;
            public string grassDripEvents;
            public string grassSurfaceMode;
            public string lipProfileMode;
            public int lipEndpointZeroPixels;
            public int lipEventsPerTile;
            public string allowedIntegerTransforms;
            public string paintSampling;
        }

        [Serializable]
        internal sealed class ContinuousProvenance
        {
            public string schema;
            public string date;
            public ProvenanceImage styleReference;
            public ProvenanceImage rejectedVisualReference;
            public ProvenanceImage machineTopology;
            public ProvenanceImage baseSoilMaterial;
            public ProvenanceImage baseGrassMaterial;
            public ContinuousGeneration generation;
            public string previousAttemptsProvenancePath;
            public string previousAttemptsProvenanceSha256;
            public ContinuousReview review;
            public ContinuousPackaging packaging;
        }

        private struct PngInfo
        {
            public int Width;
            public int Height;
            public string ColorMode;
        }

        [Serializable]
        internal sealed class ValidationReport
        {
            public string result;
            public string validatorVersion;
            public int tileSize;
            public int atlasSize;
            public int cornerRadius;
            public int protectedSocketPixels;
            public int transitionBandPixels;
            public int horizontalCompatiblePairs;
            public int verticalCompatiblePairs;
            public int maximumRgbaDifference;
            public int mask05Components;
            public int mask10Components;
            public int paintedEdgeMask05Components;
            public int paintedEdgeMask10Components;
            public bool diagonalSoilConnectionsSeamless;
            public bool isolatedCellRoundedSquare;
            public bool stripsTurnsHolesValid;
            public bool grassLandformValid;
            public bool soilLandformValid;
            public bool stoneRoadLandformValid;
            public bool paintedEdgeValid;
            public bool paintedRgbComesFromContinuousSource;
            public bool edgeRgbComesFromGrassBase;
            public bool grassLandformUsesBaseTexture;
            public bool baseSoilMatchesEdgeTanPalette;
            public int baseSoilMaximumEdgeMeanDelta;
            public int minimumBoundaryCoveragePermille;
            public int minimumStraightInsideDepthPixels;
            public int minimumStraightOutsideDepthPixels;
            public int[] boundaryCoveragePermilleByMask;
            public int[] medianInsideDepthByMask;
            public int[] medianOutsideDepthByMask;
            public int[] grassPixelCountByMask;
            public int[] soilPixelCountByMask;
            public int[] darkContactPixelCountByMask;
            public int[] semiTransparentPixelCountByMask;
            public int[] softOuterShadowPixelCountByMask;
            public int[] medianSoilBandPixelsByMask;
            public int minimumStraightSoilBandPixels;
            public int minimumSemiTransparentPixels;
            public int minimumSoftOuterShadowPixels;
            public int grassLandformColorCount;
            public int outsideOpaquePixelCount;
            public int maximumOutsideDepthPixels;
            public int editorScaleOutsideFeatherPixels;
            public int portraitScaleOutsideFeatherPixels;
            public bool runtimeMipmapsEnabled;
            public bool diagonalGrassComponentsSeparated;
            public bool provenanceValid;
            public string topologyGuideSha256;
            public string candidateSha256;
            public string continuousRibbonSha256;
            public string baseSoilSha256;
            public string approvedStyleReferenceSha256;
            public string rawImagegenSha256;
            public string rejectedAttemptSha256;
            public string attempt01PromptSha256;
            public string attempt02PromptSha256;
            public string grassLandformSha256;
            public string soilLandformSha256;
            public string stoneRoadLandformSha256;
            public string paintedEdgeSha256;
        }

        internal sealed class EdgeQualityMetrics
        {
            public readonly int[] BoundaryCoveragePermille = new int[16];
            public readonly int[] MedianInsideDepth = new int[16];
            public readonly int[] MedianOutsideDepth = new int[16];
            public readonly int[] GrassPixelCount = new int[16];
            public readonly int[] SoilPixelCount = new int[16];
            public readonly int[] DarkContactPixelCount = new int[16];
            public readonly int[] SemiTransparentPixelCount = new int[16];
            public readonly int[] SoftOuterShadowPixelCount = new int[16];
            public readonly int[] MedianSoilBandPixels = new int[16];
            public int MinimumBoundaryCoveragePermille = 1000;
            public int MinimumStraightInsideDepthPixels = int.MaxValue;
            public int MinimumStraightOutsideDepthPixels = int.MaxValue;
            public int MinimumStraightSoilBandPixels = int.MaxValue;
            public int MinimumSemiTransparentPixels = int.MaxValue;
            public int MinimumSoftOuterShadowPixels = int.MaxValue;
            public int OutsideOpaquePixelCount;
            public int MaximumOutsideDepthPixels;
        }

        public static void ValidateGeneratedAssets()
        {
            var report = ValidateGeneratedAssetsInternal(true);
            SquareTerrainEvidence.WriteValidationReport(report);
            Debug.Log("Square terrain topology, native assets and retained painted source passed: "
                + SquareTerrainArtProfile.ValidationEvidencePath);
        }

        public static void ValidateTopologyOnly()
        {
            var topology = SquareTerrainArtGenerator.BuildAllTopologyMasks();
            ValidateTopologyPixels(topology);
            Debug.Log("Square sixteen-mask topology passed isolated-cell, socket and diagonal checks.");
        }

        internal static ValidationReport ValidateGeneratedAssetsInternal(bool requirePaintedEdge)
        {
            SquareTerrainArtProfile.ValidateContract();
            var provenance = ValidateProvenanceContract();
            var topology = SquareTerrainArtGenerator.BuildAllTopologyMasks();
            ValidateTopologyPixels(topology);
            ValidateTopologyGuide(topology);

            var grass = ReadFamily(SquareTerrainArtProfile.GrassLandformFolder);
            var soil = ReadFamily(SquareTerrainArtProfile.SoilLandformFolder);
            var stone = ReadFamily(SquareTerrainArtProfile.StoneRoadLandformFolder);
            ValidateLandformFamily(grass, topology, "square grass");
            ValidateLandformFamily(soil, topology, "square soil");
            ValidateLandformFamily(stone, topology, "square stone-road");
            ValidateTileSet(SquareTerrainArtProfile.GrassLandformTileSetPath,
                SquareTerrainArtProfile.GrassLandformFolder, "square grass");
            ValidateTileSet(SquareTerrainArtProfile.SoilLandformTileSetPath,
                SquareTerrainArtProfile.SoilLandformFolder, "square soil");
            ValidateTileSet(SquareTerrainArtProfile.StoneRoadLandformTileSetPath,
                SquareTerrainArtProfile.StoneRoadLandformFolder, "square stone-road");

            var grassBase = LoadPng(SquareTerrainArtProfile.GrassBaseSourcePath);
            int grassLandformColorCount;
            try
            {
                grassLandformColorCount = ValidateLandformRgbComesFromBase(
                    grass, grassBase.GetPixels32(), "square grass");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(grassBase);
            }

            Color32[][] edge = null;
            EdgeQualityMetrics edgeQuality = null;
            var edgeRgbComesFromGrassBase = false;
            var edgeExists = File.Exists(SquareTerrainArtGenerator.AbsolutePath(
                SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath));
            if (requirePaintedEdge || edgeExists)
            {
                RequireRetainedCandidate();
                edge = ReadFamily(SquareTerrainArtProfile.GrassOnSoilEdgeFolder);
                grassBase = LoadPng(SquareTerrainArtProfile.GrassBaseSourcePath);
                try
                {
                    ValidateEdgeRgbComesFromGrassBase(edge, grassBase.GetPixels32());
                    edgeRgbComesFromGrassBase = true;
                    edgeQuality = ValidatePackagedEdgePixels(edge, topology);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(grassBase);
                }
                ValidateTileSet(SquareTerrainArtProfile.GrassOnSoilEdgeTileSetPath,
                    SquareTerrainArtProfile.GrassOnSoilEdgeFolder, "square grass-on-soil edge");
            }

            var report = new ValidationReport
            {
                result = "pass",
                validatorVersion = ValidatorVersion,
                tileSize = SquareTerrainArtProfile.TileSize,
                atlasSize = SquareTerrainArtProfile.AtlasSize,
                cornerRadius = SquareTerrainArtProfile.CornerRadius,
                protectedSocketPixels = SquareTerrainArtProfile.ProtectedSocketPixels,
                transitionBandPixels = SquareTerrainArtProfile.TransitionBandPixels,
                horizontalCompatiblePairs = 64,
                verticalCompatiblePairs = 64,
                maximumRgbaDifference = 0,
                mask05Components = CountComponents(topology[5]),
                mask10Components = CountComponents(topology[10]),
                paintedEdgeMask05Components = edge == null ? 0 : CountComponents(edge[5]),
                paintedEdgeMask10Components = edge == null ? 0 : CountComponents(edge[10]),
                diagonalSoilConnectionsSeamless = false,
                diagonalGrassComponentsSeparated = edge != null
                    && CountComponents(edge[5]) == 2 && CountComponents(edge[10]) == 2,
                isolatedCellRoundedSquare = ValidateIsolatedCell(topology),
                stripsTurnsHolesValid = ValidateAssembledPatterns(topology),
                grassLandformValid = true,
                soilLandformValid = true,
                stoneRoadLandformValid = true,
                paintedEdgeValid = edge != null,
                paintedRgbComesFromContinuousSource = false,
                edgeRgbComesFromGrassBase = edgeRgbComesFromGrassBase,
                grassLandformUsesBaseTexture = grassLandformColorCount > 1,
                baseSoilMatchesEdgeTanPalette = false,
                baseSoilMaximumEdgeMeanDelta = 0,
                minimumBoundaryCoveragePermille = edgeQuality == null ? 0
                    : edgeQuality.MinimumBoundaryCoveragePermille,
                minimumStraightInsideDepthPixels = edgeQuality == null ? 0
                    : edgeQuality.MinimumStraightInsideDepthPixels,
                minimumStraightOutsideDepthPixels = edgeQuality == null ? 0
                    : edgeQuality.MinimumStraightOutsideDepthPixels,
                boundaryCoveragePermilleByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.BoundaryCoveragePermille,
                medianInsideDepthByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.MedianInsideDepth,
                medianOutsideDepthByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.MedianOutsideDepth,
                grassPixelCountByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.GrassPixelCount,
                soilPixelCountByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.SoilPixelCount,
                darkContactPixelCountByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.DarkContactPixelCount,
                semiTransparentPixelCountByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.SemiTransparentPixelCount,
                softOuterShadowPixelCountByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.SoftOuterShadowPixelCount,
                medianSoilBandPixelsByMask = edgeQuality == null ? new int[0]
                    : edgeQuality.MedianSoilBandPixels,
                minimumStraightSoilBandPixels = edgeQuality == null ? 0
                    : edgeQuality.MinimumStraightSoilBandPixels,
                minimumSemiTransparentPixels = edgeQuality == null ? 0
                    : edgeQuality.MinimumSemiTransparentPixels,
                minimumSoftOuterShadowPixels = edgeQuality == null ? 0
                    : edgeQuality.MinimumSoftOuterShadowPixels,
                grassLandformColorCount = grassLandformColorCount,
                outsideOpaquePixelCount = edgeQuality == null ? 0
                    : edgeQuality.OutsideOpaquePixelCount,
                maximumOutsideDepthPixels = edgeQuality == null ? 0
                    : edgeQuality.MaximumOutsideDepthPixels,
                editorScaleOutsideFeatherPixels = edgeQuality == null ? 0
                    : Mathf.CeilToInt(edgeQuality.MaximumOutsideDepthPixels * 72f
                        / SquareTerrainArtProfile.TileSize),
                portraitScaleOutsideFeatherPixels = edgeQuality == null ? 0
                    : Mathf.CeilToInt(edgeQuality.MaximumOutsideDepthPixels * 46f
                        / SquareTerrainArtProfile.TileSize),
                runtimeMipmapsEnabled = true,
                provenanceValid = provenance != null,
                topologyGuideSha256 = HashFile(SquareTerrainArtProfile.TopologyGuidePath),
                candidateSha256 = File.Exists(SquareTerrainArtGenerator.AbsolutePath(
                    SquareTerrainArtProfile.CandidatePath))
                    ? HashFile(SquareTerrainArtProfile.CandidatePath) : string.Empty,
                continuousRibbonSha256 = HashFile(
                    SquareTerrainArtProfile.ContinuousRibbonPath),
                baseSoilSha256 = HashFile(SquareTerrainArtProfile.SoilBaseSourcePath),
                approvedStyleReferenceSha256 = HashFile(
                    SquareTerrainArtProfile.ApprovedStyleReferencePath),
                rawImagegenSha256 = HashFile(SquareTerrainArtProfile.RawImagegenDraftPath),
                rejectedAttemptSha256 = HashFile(
                    SquareTerrainArtProfile.RejectedImagegenAttemptPath),
                attempt01PromptSha256 = HashFile(SquareTerrainArtProfile.Attempt01PromptPath),
                attempt02PromptSha256 = HashFile(SquareTerrainArtProfile.Attempt02PromptPath),
                grassLandformSha256 = HashFamily(SquareTerrainArtProfile.GrassLandformFolder),
                soilLandformSha256 = HashFamily(SquareTerrainArtProfile.SoilLandformFolder),
                stoneRoadLandformSha256 = HashFamily(
                    SquareTerrainArtProfile.StoneRoadLandformFolder),
                paintedEdgeSha256 = edge == null ? string.Empty
                    : HashFamily(SquareTerrainArtProfile.GrassOnSoilEdgeFolder),
            };
            return report;
        }

        internal static EdgeQualityMetrics ValidatePackagedEdgePixels(Color32[][] edge,
            Color32[][] topology)
        {
            ValidateFamilyShape(edge, "square grass-on-soil edge");
            RequireAllTransparent(edge[0], "Painted edge mask 00 must be transparent.");
            RequireAllTransparent(edge[15], "Painted edge mask 15 must be transparent.");
            ValidateCompatibleSockets(edge, "square grass-on-soil edge");
            var quality = new EdgeQualityMetrics();
            for (var mask = 1; mask < 15; mask++)
            {
                var opaque = CountOpaque(edge[mask]);
                if (opaque < 64)
                    throw new InvalidOperationException("Top-down edge mask " + mask
                        + " does not retain a readable grass feather.");
                ValidateBoundaryQuality(edge[mask], topology[mask], mask, quality);
            }
            ValidateOppositeCornerGrassSeparation(edge[5], topology[5], 5);
            ValidateOppositeCornerGrassSeparation(edge[10], topology[10], 10);
            var editorPixels = Mathf.CeilToInt(quality.MaximumOutsideDepthPixels * 72f
                / SquareTerrainArtProfile.TileSize);
            var portraitPixels = Mathf.CeilToInt(quality.MaximumOutsideDepthPixels * 46f
                / SquareTerrainArtProfile.TileSize);
            if (editorPixels > 5 || portraitPixels > 3)
                throw new InvalidOperationException("Top-down grass feather reaches "
                    + quality.MaximumOutsideDepthPixels + " native pixels and expands to "
                    + editorPixels + "/" + portraitPixels
                    + " pixels at 72/46 px display scales; maximum is 5/3.");
            return quality;
        }

        private static int ValidateLandformRgbComesFromBase(Color32[][] family,
            Color32[] basePixels, string label)
        {
            if (basePixels == null || basePixels.Length == 0)
                throw new InvalidOperationException(label + " base texture is missing.");
            var authoredColors = new HashSet<uint>();
            for (var index = 0; index < basePixels.Length; index++)
                authoredColors.Add(PackRgb(basePixels[index]));
            var usedColors = new HashSet<uint>();
            for (var mask = 0; mask < family.Length; mask++)
            for (var index = 0; index < family[mask].Length; index++)
            {
                var color = family[mask][index];
                if (color.a == 0) continue;
                var rgb = PackRgb(color);
                if (!authoredColors.Contains(rgb))
                    throw new InvalidOperationException(label + " mask " + mask
                        + " contains RGB outside its registered base texture.");
                usedColors.Add(rgb);
            }
            if (usedColors.Count < 8)
                throw new InvalidOperationException(label
                    + " collapsed to a flat fill instead of retaining its seamless texture: "
                    + usedColors.Count + " colors.");
            return usedColors.Count;
        }

        internal static void ValidateEdgeRgbComesFromGrassBase(Color32[][] edge,
            Color32[] grassPixels)
        {
            if (grassPixels == null || grassPixels.Length == 0)
                throw new InvalidOperationException("Registered base-grass pixels are missing.");
            var authoredColors = new HashSet<uint>();
            for (var index = 0; index < grassPixels.Length; index++)
                authoredColors.Add(PackRgb(grassPixels[index]));
            var allowedAlpha = BuildAllowedPaintAlphaSet();
            for (var mask = 0; mask < edge.Length; mask++)
            for (var index = 0; index < edge[mask].Length; index++)
                if (edge[mask][index].a != 0
                    && (!authoredColors.Contains(PackRgb(edge[mask][index]))
                        || !allowedAlpha.Contains(edge[mask][index].a)))
                    throw new InvalidOperationException("Top-down edge mask " + mask
                        + " contains RGB outside Grass.png or alpha outside the deterministic "
                        + "grass feather.");
        }

        private static HashSet<byte> BuildAllowedPaintAlphaSet()
        {
            var allowed = new HashSet<byte> { 255 };
            for (var extent = SquareTerrainArtProfile.GrassFeatherBasePixels;
                 extent <= SquareTerrainArtProfile.GrassFeatherOutsideMaxPixels; extent++)
                AddAlphaRamp(allowed, SquareTerrainArtProfile.GrassFeatherAlphaNear,
                    SquareTerrainArtProfile.GrassFeatherAlphaFar, extent);
            return allowed;
        }

        private static void AddAlphaRamp(HashSet<byte> output, int near, int far,
            int layerCount)
        {
            for (var layer = 0; layer < layerCount; layer++)
                output.Add((byte)Mathf.RoundToInt(Mathf.Lerp(near, far,
                    layer / (float)(layerCount - 1))));
        }

        private static int ValidateBaseSoilMatchesEdgeTanPalette(Color32[] soilBasePixels,
            Color32[] ribbonPixels, int ribbonWidth, int ribbonHeight)
        {
            if (soilBasePixels == null || soilBasePixels.Length == 0)
                throw new InvalidOperationException("Base-soil palette source is empty.");
            if (ribbonPixels == null || ribbonPixels.Length != ribbonWidth * ribbonHeight)
                throw new InvalidOperationException("Continuous-ribbon palette source is invalid.");

            long baseR = 0;
            long baseG = 0;
            long baseB = 0;
            for (var index = 0; index < soilBasePixels.Length; index++)
            {
                baseR += soilBasePixels[index].r;
                baseG += soilBasePixels[index].g;
                baseB += soilBasePixels[index].b;
            }

            long edgeR = 0;
            long edgeG = 0;
            long edgeB = 0;
            var edgeCount = 0;
            for (var rowFromTop = 240; rowFromTop <= 480; rowFromTop++)
            {
                var y = ribbonHeight - 1 - rowFromTop;
                for (var x = 173; x <= 941; x++)
                {
                    var color = ribbonPixels[x + y * ribbonWidth];
                    edgeR += color.r;
                    edgeG += color.g;
                    edgeB += color.b;
                    edgeCount++;
                }
            }

            var baseMeanR = (int)Math.Round(baseR / (double)soilBasePixels.Length);
            var baseMeanG = (int)Math.Round(baseG / (double)soilBasePixels.Length);
            var baseMeanB = (int)Math.Round(baseB / (double)soilBasePixels.Length);
            var edgeMeanR = (int)Math.Round(edgeR / (double)edgeCount);
            var edgeMeanG = (int)Math.Round(edgeG / (double)edgeCount);
            var edgeMeanB = (int)Math.Round(edgeB / (double)edgeCount);
            var maximumDelta = Math.Max(Math.Abs(baseMeanR - edgeMeanR),
                Math.Max(Math.Abs(baseMeanG - edgeMeanG),
                    Math.Abs(baseMeanB - edgeMeanB)));
            if (maximumDelta > 3 || baseMeanR < 185 || baseMeanG < 145 || baseMeanB < 90)
                throw new InvalidOperationException("Base soil must use the accepted edge's warm "
                    + "light-tan palette. Base RGB " + baseMeanR + "/" + baseMeanG + "/"
                    + baseMeanB + ", edge-soil RGB " + edgeMeanR + "/" + edgeMeanG + "/"
                    + edgeMeanB + ".");
            return maximumDelta;
        }

        private static void ValidateBoundaryQuality(Color32[] paint, Color32[] topology,
            int mask, EdgeQualityMetrics quality)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var boundaryPixels = 0;
            var coveredBoundaryPixels = 0;
            var insideDepths = new List<int>();
            var outsideDepths = new List<int>();
            var grassPixels = 0;
            var soilPixels = 0;
            var darkContactPixels = 0;
            var semiTransparentPixels = 0;
            var outsideOpaquePixels = 0;

            for (var index = 0; index < paint.Length; index++)
            {
                var color = paint[index];
                if (color.a == 0) continue;
                if (color.a < 255) semiTransparentPixels++;
                if (IsSoilLayer(color))
                    soilPixels++;
                else if (IsDarkContactLayer(color))
                    darkContactPixels++;
                else if (IsGrassLayer(color))
                    grassPixels++;
                if (topology[index].a == 0 && color.a == 255) outsideOpaquePixels++;
            }

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var index = x + y * size;
                if (topology[index].a == 0) continue;
                var outward = new List<Vector2Int>(4);
                AddOutwardDirection(topology, x, y, -1, 0, outward);
                AddOutwardDirection(topology, x, y, 1, 0, outward);
                AddOutwardDirection(topology, x, y, 0, -1, outward);
                AddOutwardDirection(topology, x, y, 0, 1, outward);
                if (outward.Count == 0) continue;
                boundaryPixels++;
                if (paint[index].a != 0) coveredBoundaryPixels++;
                for (var directionIndex = 0; directionIndex < outward.Count; directionIndex++)
                {
                    var direction = outward[directionIndex];
                    insideDepths.Add(CountPaintDepth(paint, topology, x, y,
                        -direction.x, -direction.y, true));
                    outsideDepths.Add(CountPaintDepth(paint, topology,
                        x + direction.x, y + direction.y,
                        direction.x, direction.y, false));
                }
            }

            if (boundaryPixels == 0 || insideDepths.Count == 0 || outsideDepths.Count == 0)
                throw new InvalidOperationException("Painted edge mask " + mask
                    + " has no measurable guide boundary.");
            var coverage = coveredBoundaryPixels * 1000 / boundaryPixels;
            var medianInside = Median(insideDepths);
            var medianOutside = Median(outsideDepths);
            var maximumOutside = MeasureMaximumOutsideDistance(paint, topology);
            quality.BoundaryCoveragePermille[mask] = coverage;
            quality.MedianInsideDepth[mask] = medianInside;
            quality.MedianOutsideDepth[mask] = medianOutside;
            quality.MedianSoilBandPixels[mask] = 0;
            quality.GrassPixelCount[mask] = grassPixels;
            quality.SoilPixelCount[mask] = soilPixels;
            quality.DarkContactPixelCount[mask] = darkContactPixels;
            quality.SemiTransparentPixelCount[mask] = semiTransparentPixels;
            quality.SoftOuterShadowPixelCount[mask] = 0;
            quality.MinimumBoundaryCoveragePermille = Math.Min(
                quality.MinimumBoundaryCoveragePermille, coverage);
            quality.MinimumSemiTransparentPixels = Math.Min(
                quality.MinimumSemiTransparentPixels, semiTransparentPixels);
            quality.MinimumSoftOuterShadowPixels = 0;
            quality.OutsideOpaquePixelCount += outsideOpaquePixels;
            quality.MaximumOutsideDepthPixels = Math.Max(
                quality.MaximumOutsideDepthPixels, maximumOutside);

            if (coverage < 1000)
                throw new InvalidOperationException("Top-down edge mask " + mask
                    + " contacts only " + coverage
                    + "/1000 of its guide boundary; complete coverage is required.");
            var layerMinimum = Math.Max(64, boundaryPixels / 2);
            if (grassPixels < layerMinimum)
                throw new InvalidOperationException("Top-down edge mask " + mask
                    + " does not retain enough grass-derived transition pixels: "
                    + grassPixels + ", requires " + layerMinimum + ".");
            if (soilPixels != 0 || darkContactPixels != 0 || outsideOpaquePixels != 0)
                throw new InvalidOperationException("Top-down edge mask " + mask
                    + " reintroduced soil-wall, dark-contact, or opaque outside pixels: "
                    + soilPixels + "/" + darkContactPixels + "/" + outsideOpaquePixels + ".");
            if (semiTransparentPixels < layerMinimum)
                throw new InvalidOperationException("Top-down edge mask " + mask
                    + " has no sufficient alpha feather: " + semiTransparentPixels
                    + ", requires " + layerMinimum + ".");

            if (mask == 3 || mask == 6 || mask == 9 || mask == 12)
            {
                quality.MinimumStraightInsideDepthPixels = Math.Min(
                    quality.MinimumStraightInsideDepthPixels, medianInside);
                quality.MinimumStraightOutsideDepthPixels = Math.Min(
                    quality.MinimumStraightOutsideDepthPixels, medianOutside);
                quality.MinimumStraightSoilBandPixels = 0;
                if (medianInside != SquareTerrainArtProfile.GrassBlendInsidePixels
                    || medianOutside < SquareTerrainArtProfile.GrassFeatherBasePixels
                    || maximumOutside > SquareTerrainArtProfile.GrassFeatherOutsideMaxPixels)
                    throw new InvalidOperationException("Straight top-down edge mask " + mask
                        + " must retain the narrow inside/outside feather; actual "
                        + medianInside + "/" + medianOutside + "/" + maximumOutside + ".");
            }
        }

        private static void AddOutwardDirection(Color32[] topology, int x, int y,
            int dx, int dy, List<Vector2Int> output)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var nextX = x + dx;
            var nextY = y + dy;
            if (nextX < 0 || nextX >= size || nextY < 0 || nextY >= size) return;
            if (topology[nextX + nextY * size].a == 0)
                output.Add(new Vector2Int(dx, dy));
        }

        private static int CountPaintDepth(Color32[] paint, Color32[] topology,
            int startX, int startY, int dx, int dy, bool expectTopology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var depth = 0;
            var step = 0;
            while (step <= SquareTerrainArtProfile.TransitionBandPixels)
            {
                var x = startX + dx * step;
                var y = startY + dy * step;
                if (x < 0 || x >= size || y < 0 || y >= size) break;
                var index = x + y * size;
                if ((topology[index].a != 0) != expectTopology || paint[index].a == 0) break;
                depth++;
                step++;
            }
            return depth;
        }

        private static int MeasureMaximumOutsideDistance(Color32[] paint,
            Color32[] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var distance = new int[topology.Length];
            var queue = new Queue<int>(topology.Length);
            for (var index = 0; index < distance.Length; index++)
                distance[index] = int.MaxValue;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var index = x + y * size;
                if (topology[index].a != 0) continue;
                if (!HasTopologyNeighbor(topology, x, y)) continue;
                distance[index] = 0;
                queue.Enqueue(index);
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % size;
                var y = index / size;
                PropagateOutsideDistance(index - 1, x > 0, index,
                    topology, distance, queue);
                PropagateOutsideDistance(index + 1, x + 1 < size, index,
                    topology, distance, queue);
                PropagateOutsideDistance(index - size, y > 0, index,
                    topology, distance, queue);
                PropagateOutsideDistance(index + size, y + 1 < size, index,
                    topology, distance, queue);
            }
            var maximum = 0;
            for (var index = 0; index < paint.Length; index++)
                if (topology[index].a == 0 && paint[index].a != 0
                    && distance[index] != int.MaxValue)
                    maximum = Math.Max(maximum, distance[index] + 1);
            return maximum;
        }

        private static bool HasTopologyNeighbor(Color32[] topology, int x, int y)
        {
            var size = SquareTerrainArtProfile.TileSize;
            return x > 0 && topology[x - 1 + y * size].a != 0
                || x + 1 < size && topology[x + 1 + y * size].a != 0
                || y > 0 && topology[x + (y - 1) * size].a != 0
                || y + 1 < size && topology[x + (y + 1) * size].a != 0;
        }

        private static void PropagateOutsideDistance(int index, bool valid, int from,
            Color32[] topology, int[] distance, Queue<int> queue)
        {
            if (!valid || topology[index].a != 0
                || distance[from] + 1 >= distance[index]) return;
            distance[index] = distance[from] + 1;
            queue.Enqueue(index);
        }

        private static int Median(List<int> values)
        {
            values.Sort();
            return values[values.Count / 2];
        }

        private static int CountLongestSoilRun(Color32[] paint, Color32[] topology,
            int startX, int startY, int dx, int dy)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var longest = 0;
            var current = 0;
            for (var step = 0; step <= SquareTerrainArtProfile.TransitionBandPixels; step++)
            {
                var x = startX + dx * step;
                var y = startY + dy * step;
                if (x < 0 || x >= size || y < 0 || y >= size) break;
                var index = x + y * size;
                if (topology[index].a != 0 || paint[index].a == 0) break;
                if (IsSoilLayer(paint[index]))
                {
                    current++;
                    longest = Math.Max(longest, current);
                }
                else current = 0;
            }
            return longest;
        }

        private static uint PackColor(Color32 color)
        {
            return (uint)(color.r | color.g << 8 | color.b << 16 | color.a << 24);
        }

        private static uint PackRgb(Color32 color)
        {
            return (uint)(color.r | color.g << 8 | color.b << 16);
        }

        private static bool IsGrassLayer(Color32 color)
        {
            return color.g >= color.r + 15 && color.g > 140 && color.b < 110;
        }

        private static bool IsSoilLayer(Color32 color)
        {
            return color.r >= color.g + 15 && color.r > 140 && color.b < 125;
        }

        private static bool IsDarkContactLayer(Color32 color)
        {
            return color.r > 60 && color.r < 135 && color.g < 150 && color.b < 95
                && color.g <= color.r + 40
                && !IsGrassLayer(color) && !IsSoilLayer(color);
        }

        internal static void ValidateTopologyPixels(Color32[][] topology)
        {
            ValidateFamilyShape(topology, "square topology");
            for (var mask = 0; mask < SquareTerrainArtProfile.MaskCount; mask++)
            {
                var expected = SquareTerrainArtGenerator.BuildTopologyMask(mask);
                RequireSameAlpha(expected, topology[mask], "Square topology mask " + mask);
            }
            if (CountOpaque(topology[0]) != 0)
                throw new InvalidOperationException("Square topology mask 00 must be empty.");
            if (CountOpaque(topology[15]) != topology[15].Length)
                throw new InvalidOperationException("Square topology mask 15 must be full.");
            if (CountComponents(topology[5]) != 2 || CountComponents(topology[10]) != 2)
                throw new InvalidOperationException(
                    "Square topology diagonal masks 05 and 10 must contain two components.");
            RequireCenterTransparent(topology[5], "05");
            RequireCenterTransparent(topology[10], "10");
            ValidateCompatibleAlphaSockets(topology);
            if (!ValidateIsolatedCell(topology))
                throw new InvalidOperationException(
                    "Four one-corner masks do not assemble a contained rounded square.");
            if (!ValidateAssembledPatterns(topology))
                throw new InvalidOperationException(
                    "Square topology failed strip, turn, hole, or diagonal assembly checks.");
        }

        private static void ValidateTopologyGuide(Color32[][] topology)
        {
            var path = SquareTerrainArtGenerator.AbsolutePath(
                SquareTerrainArtProfile.TopologyGuidePath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Square topology guide is missing.", path);
            var atlas = LoadPng(SquareTerrainArtProfile.TopologyGuidePath);
            try
            {
                if (atlas.width != SquareTerrainArtProfile.AtlasSize
                    || atlas.height != SquareTerrainArtProfile.AtlasSize)
                    throw new InvalidOperationException("Square topology guide must be 1024x1024.");
                for (var mask = 0; mask < SquareTerrainArtProfile.MaskCount; mask++)
                {
                    var tile = CropAtlasTile(atlas, mask);
                    RequireSameAlpha(topology[mask], tile,
                        "Square topology guide mask " + mask);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(atlas);
            }
        }

        private static void ValidateLandformFamily(Color32[][] family, Color32[][] topology,
            string label)
        {
            ValidateFamilyShape(family, label);
            for (var mask = 0; mask < SquareTerrainArtProfile.MaskCount; mask++)
                RequireSameAlpha(topology[mask], family[mask], label + " mask " + mask);
            ValidateCompatibleSockets(family, label);
        }

        private static void ValidateTileSet(string tileSetPath, string folder, string label)
        {
            var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(tileSetPath);
            var reason = "TileSet asset is missing.";
            if (tileSet == null || !tileSet.Validate(out reason))
                throw new InvalidOperationException(label + " TileSet is invalid: " + reason);
            for (var mask = 0; mask < SquareTerrainArtProfile.MaskCount; mask++)
            {
                var path = SquareTerrainArtProfile.MaskTexturePath(folder, mask);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite
                    || importer.spritePixelsPerUnit != SquareTerrainArtProfile.TileSize
                    || !importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear
                    || importer.maxTextureSize < SquareTerrainArtProfile.TileSize)
                    throw new InvalidOperationException(label + " mask " + mask
                        + " does not retain the native 256 px scale-aware sprite import contract.");
                Sprite sprite;
                if (!tileSet.TryGetSprite((DualGridMask)mask, out sprite) || sprite == null
                    || sprite.texture.width != SquareTerrainArtProfile.TileSize
                    || sprite.texture.height != SquareTerrainArtProfile.TileSize)
                    throw new InvalidOperationException(label + " mask " + mask
                        + " is not wired to a native-size sprite.");
            }
        }

        private static bool ValidateIsolatedCell(Color32[][] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var assembled = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                int mask;
                int sampleX;
                int sampleY;
                if (x < size / 2 && y >= size / 2)
                {
                    mask = 4;
                    sampleX = x + size / 2;
                    sampleY = y - size / 2;
                }
                else if (x >= size / 2 && y >= size / 2)
                {
                    mask = 8;
                    sampleX = x - size / 2;
                    sampleY = y - size / 2;
                }
                else if (x >= size / 2)
                {
                    mask = 1;
                    sampleX = x - size / 2;
                    sampleY = y + size / 2;
                }
                else
                {
                    mask = 2;
                    sampleX = x + size / 2;
                    sampleY = y + size / 2;
                }
                assembled[x + y * size] = topology[mask][sampleX + sampleY * size];
            }
            if (CountComponents(assembled) != 1) return false;
            var center = size / 2;
            if (assembled[center + center * size].a == 0) return false;
            if (assembled[0].a != 0 || assembled[size - 1].a != 0
                || assembled[(size - 1) * size].a != 0
                || assembled[size * size - 1].a != 0) return false;
            if (assembled[center].a == 0 || assembled[center * size].a == 0
                || assembled[size - 1 + center * size].a == 0
                || assembled[center + (size - 1) * size].a == 0) return false;
            return true;
        }

        private static bool ValidateAssembledPatterns(Color32[][] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var strip = new bool[4, 1];
            for (var x = 0; x < 4; x++) strip[x, 0] = true;
            var stripPixels = AssembleLogicalCells(strip, topology);
            if (CountComponents(stripPixels, strip.GetLength(0) * size) != 1) return false;
            for (var seam = 1; seam < 4; seam++)
                if (stripPixels[seam * size + (size / 2) * (4 * size)].a == 0) return false;

            var turn = new bool[3, 3];
            turn[0, 0] = true;
            turn[1, 0] = true;
            turn[2, 0] = true;
            turn[2, 1] = true;
            turn[2, 2] = true;
            var turnPixels = AssembleLogicalCells(turn, topology);
            if (CountComponents(turnPixels, turn.GetLength(0) * size) != 1) return false;

            var ring = new bool[3, 3];
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
                ring[x, y] = x != 1 || y != 1;
            var ringPixels = AssembleLogicalCells(ring, topology);
            if (CountComponents(ringPixels, ring.GetLength(0) * size) != 1) return false;
            var ringWidth = ring.GetLength(0) * size;
            if (ringPixels[(size + size / 2) + (size + size / 2) * ringWidth].a != 0)
                return false;

            var diagonal = new bool[2, 2];
            diagonal[0, 0] = true;
            diagonal[1, 1] = true;
            var diagonalPixels = AssembleLogicalCells(diagonal, topology);
            if (CountComponents(diagonalPixels, diagonal.GetLength(0) * size) != 2) return false;
            var diagonalWidth = diagonal.GetLength(0) * size;
            if (diagonalPixels[size + size * diagonalWidth].a != 0) return false;
            return true;
        }

        private static Color32[] AssembleLogicalCells(bool[,] cells, Color32[][] topology)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var width = cells.GetLength(0) * size;
            var height = cells.GetLength(1) * size;
            var result = new Color32[width * height];
            for (var globalY = 0; globalY < height; globalY++)
            for (var globalX = 0; globalX < width; globalX++)
            {
                var cellX = globalX / size;
                var cellY = globalY / size;
                var localX = globalX % size;
                var localY = globalY % size;
                var vertexX = localX < size / 2 ? cellX : cellX + 1;
                var vertexY = localY < size / 2 ? cellY : cellY + 1;
                var sampleX = localX < size / 2 ? localX + size / 2 : localX - size / 2;
                var sampleY = localY < size / 2 ? localY + size / 2 : localY - size / 2;
                var mask = 0;
                if (Occupied(cells, vertexX - 1, vertexY)) mask |= 1;
                if (Occupied(cells, vertexX, vertexY)) mask |= 2;
                if (Occupied(cells, vertexX, vertexY - 1)) mask |= 4;
                if (Occupied(cells, vertexX - 1, vertexY - 1)) mask |= 8;
                result[globalX + globalY * width] =
                    topology[mask][sampleX + sampleY * size];
            }
            return result;
        }

        private static bool Occupied(bool[,] cells, int x, int y)
        {
            return x >= 0 && x < cells.GetLength(0) && y >= 0 && y < cells.GetLength(1)
                && cells[x, y];
        }

        private static void ValidateOppositeCornerGrassSeparation(Color32[] edge,
            Color32[] topology, int mask)
        {
            if (CountComponents(topology) != 2)
                throw new InvalidOperationException("Square topology mask " + mask
                    + " must retain two disconnected grass components.");
            if (CountComponents(edge) != 2)
                throw new InvalidOperationException("Top-down edge mask " + mask
                    + " must keep diagonal grass feathers disconnected.");
            var size = SquareTerrainArtProfile.TileSize;
            var half = size / 2;
            for (var y = half - 2; y <= half + 1; y++)
            for (var x = half - 2; x <= half + 1; x++)
                if (edge[x + y * size].a != 0)
                    throw new InvalidOperationException("Top-down edge mask " + mask
                        + " bridges diagonal grass through its center.");
        }

        private static void ValidateCompatibleAlphaSockets(Color32[][] family)
        {
            var alphaOnly = new Color32[family.Length][];
            for (var mask = 0; mask < family.Length; mask++)
            {
                alphaOnly[mask] = new Color32[family[mask].Length];
                for (var index = 0; index < family[mask].Length; index++)
                    alphaOnly[mask][index].a = family[mask][index].a;
            }
            ValidateCompatibleSockets(alphaOnly, "square topology alpha");
        }

        private static void ValidateCompatibleSockets(Color32[][] family, string label)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var socket = SquareTerrainArtProfile.ProtectedSocketPixels;
            var horizontalPairs = 0;
            var verticalPairs = 0;
            for (var first = 0; first < 16; first++)
            for (var second = 0; second < 16; second++)
            {
                if (((first & 2) != 0) == ((second & 1) != 0)
                    && ((first & 4) != 0) == ((second & 8) != 0))
                {
                    horizontalPairs++;
                    for (var y = 0; y < size; y++)
                    for (var depth = 0; depth < socket; depth++)
                        RequireSameColor(family[first][size - 1 - depth + y * size],
                            family[second][depth + y * size], label, first, second);
                }
                if (((first & 1) != 0) == ((second & 8) != 0)
                    && ((first & 2) != 0) == ((second & 4) != 0))
                {
                    verticalPairs++;
                    for (var x = 0; x < size; x++)
                    for (var depth = 0; depth < socket; depth++)
                        RequireSameColor(family[first][x + (size - 1 - depth) * size],
                            family[second][x + depth * size], label, first, second);
                }
            }
            if (horizontalPairs != 64 || verticalPairs != 64)
                throw new InvalidOperationException(label + " compatible-pair count drifted.");
        }

        private static void RequireSameColor(Color32 first, Color32 second, string label,
            int firstMask, int secondMask)
        {
            if (first.r == second.r && first.g == second.g && first.b == second.b
                && first.a == second.a) return;
            throw new InvalidOperationException(label + " socket mismatch between masks "
                + firstMask + " and " + secondMask + ".");
        }

        private static void RequireCenterTransparent(Color32[] pixels, string label)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var half = size / 2;
            for (var y = half - 2; y <= half + 1; y++)
            for (var x = half - 2; x <= half + 1; x++)
                if (pixels[x + y * size].a != 0)
                    throw new InvalidOperationException("Diagonal mask " + label
                        + " must remain transparent through the center.");
        }

        private static int CountComponents(Color32[] pixels)
        {
            return CountComponents(pixels, SquareTerrainArtProfile.TileSize);
        }

        private static int CountComponents(Color32[] pixels, int width)
        {
            if (width <= 0 || pixels.Length % width != 0)
                throw new ArgumentException("Component buffer dimensions are invalid.");
            var height = pixels.Length / width;
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            var count = 0;
            for (var start = 0; start < pixels.Length; start++)
            {
                if (visited[start] || pixels[start].a == 0) continue;
                count++;
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    var x = index % width;
                    var y = index / width;
                    Visit(index - 1, x > 0, pixels, visited, queue);
                    Visit(index + 1, x + 1 < width, pixels, visited, queue);
                    Visit(index - width, y > 0, pixels, visited, queue);
                    Visit(index + width, y + 1 < height, pixels, visited, queue);
                }
            }
            return count;
        }

        private static void Visit(int index, bool valid, Color32[] pixels, bool[] visited,
            Queue<int> queue)
        {
            if (!valid || visited[index] || pixels[index].a == 0) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static Color32[][] ReadFamily(string folder)
        {
            var result = new Color32[SquareTerrainArtProfile.MaskCount][];
            for (var mask = 0; mask < result.Length; mask++)
                result[mask] = SquareTerrainArtGenerator.ReadMaskTexture(folder, mask);
            return result;
        }

        private static void ValidateFamilyShape(Color32[][] family, string label)
        {
            var expected = SquareTerrainArtProfile.TileSize * SquareTerrainArtProfile.TileSize;
            if (family == null || family.Length != SquareTerrainArtProfile.MaskCount)
                throw new InvalidOperationException(label + " must contain sixteen masks.");
            for (var mask = 0; mask < family.Length; mask++)
                if (family[mask] == null || family[mask].Length != expected)
                    throw new InvalidOperationException(label + " mask " + mask
                        + " is not a native 256 px tile.");
        }

        private static void RequireSameAlpha(Color32[] expected, Color32[] actual, string label)
        {
            for (var index = 0; index < expected.Length; index++)
                if ((expected[index].a == 0) != (actual[index].a == 0))
                    throw new InvalidOperationException(label
                        + " does not match the deterministic square topology at pixel " + index + ".");
        }

        private static void RequireAllTransparent(Color32[] pixels, string reason)
        {
            if (CountOpaque(pixels) != 0) throw new InvalidOperationException(reason);
        }

        private static int CountOpaque(Color32[] pixels)
        {
            var count = 0;
            for (var index = 0; index < pixels.Length; index++)
                if (pixels[index].a != 0) count++;
            return count;
        }

        private static void RequireRetainedCandidate()
        {
            ValidateProvenanceContract();
        }

        internal static ContinuousProvenance ValidateProvenanceContract()
        {
            var path = SquareTerrainArtGenerator.AbsolutePath(
                SquareTerrainArtProfile.ContinuousRibbonProvenancePath);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Painted square edge requires retained continuous-ribbon provenance.", path);
            return ValidateProvenanceJson(File.ReadAllText(path));
        }

        internal static ContinuousProvenance ValidateProvenanceJson(string json)
        {
            ContinuousProvenance provenance;
            try
            {
                provenance = JsonUtility.FromJson<ContinuousProvenance>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Square imagegen provenance is not valid JSON.",
                    exception);
            }
            if (provenance == null || !string.Equals(provenance.schema, ProvenanceSchema,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Square imagegen provenance schema must be '"
                    + ProvenanceSchema + "'.");
            if (!string.Equals(provenance.date, "2026-07-26", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Continuous-ribbon provenance date is missing or changed.");

            ValidateProvenanceImage(provenance.styleReference,
                SquareTerrainArtProfile.ApprovedStyleReferencePath,
                ApprovedStyleReferenceSha256, 685, 352, "RGB", "approved style reference");
            ValidateProvenanceImage(provenance.machineTopology,
                SquareTerrainArtProfile.TopologyGuidePath,
                MachineTopologySha256, 1024, 1024, "RGBA", "machine topology guide");
            ValidateProvenanceImage(provenance.baseSoilMaterial,
                SquareTerrainArtProfile.SoilBaseSourcePath,
                SoilBaseSha256, 32, 32, "RGBA", "authoritative base-soil material");
            ValidateProvenanceImage(provenance.baseGrassMaterial,
                SquareTerrainArtProfile.GrassBaseSourcePath,
                GrassBaseSha256, 32, 32, "RGBA", "authoritative base-grass material");
            ValidateProvenanceImage(provenance.rejectedVisualReference,
                SquareTerrainArtProfile.SemanticSamplingRejectedPath,
                RejectedSemanticBoardSha256, 936, 648, "RGBA",
                "rejected semantic-sampling board");
            ValidateContinuousGeneration(provenance.generation);
            RequireString(provenance.previousAttemptsProvenancePath,
                SquareTerrainArtProfile.CandidateProvenancePath,
                "previous attempts provenance path");
            RequireString(provenance.previousAttemptsProvenanceSha256,
                PreviousProvenanceSha256, "previous attempts provenance hash");
            ValidateFileHash(SquareTerrainArtProfile.CandidateProvenancePath,
                PreviousProvenanceSha256, "previous v2 imagegen provenance");
            if (provenance.review == null
                || !provenance.review.continuousRibbonAcceptedAsPaintMaterial
                || !provenance.review.continuousRibbonAcceptedAsLipProfile
                || provenance.review.runtimeRibbonRgbAccepted
                || provenance.review.atlasTopologyAcceptedDirectly
                || !provenance.review.rejectedSemanticSamplingBoardRetained
                || string.IsNullOrWhiteSpace(provenance.review.notes))
                throw new InvalidOperationException(
                    "V7 review must retain the generated ribbon as lip-profile provenance, "
                    + "reject its runtime RGB and direct topology, and use base-material color.");
            ValidateContinuousPackaging(provenance.packaging);
            return provenance;
        }

        private static void ValidateContinuousGeneration(ContinuousGeneration generation)
        {
            if (generation == null
                || !string.Equals(generation.toolName, "image_gen__imagegen",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("V3 imagegen tool identity is missing.");
            RequireString(generation.promptPath,
                SquareTerrainArtProfile.ContinuousRibbonPromptPath, "v3 prompt path");
            RequireString(generation.promptSha256,
                ContinuousRibbonPromptSha256, "v3 prompt hash");
            ValidateFileHash(SquareTerrainArtProfile.ContinuousRibbonPromptPath,
                ContinuousRibbonPromptSha256, "v3 full prompt");
            if (generation.toolArguments == null)
                throw new InvalidOperationException("V3 imagegen arguments are missing.");
            RequireArray(generation.toolArguments.referenced_image_paths, new[]
            {
                "C:/Users/18163/AppData/Local/Temp/codex-clipboard-3ed40819-8aab-4462-be2b-1288e8999535.png",
                "E:/project/unity/furitDefense/Builds/Evidence/terrain-contours/square-contour-board.png",
            }, "v3 referenced_image_paths");
            RequireArray(generation.retainedReferencedImages, new[]
            {
                SquareTerrainArtProfile.ApprovedStyleReferencePath,
                SquareTerrainArtProfile.SemanticSamplingRejectedPath,
            }, "v3 retained references");
            RequireString(generation.toolOutputPath,
                "C:/Users/18163/.codex/generated_images/019f8df4-c04f-7911-866f-d09f5b56cd35/exec-88149249-ad1c-4276-a34b-93d9c8c14073.png",
                "v3 original tool output path");
            ValidateProvenanceImage(generation.retainedOutput,
                SquareTerrainArtProfile.ContinuousRibbonPath, ContinuousRibbonSha256,
                2172, 724, "RGB", "continuous ribbon paint source");
            if (!string.Equals(generation.decision,
                    "accepted-as-continuous-paint-material-only", StringComparison.Ordinal)
                || !generation.paintMaterialAccepted
                || generation.atlasTopologyAcceptedDirectly
                || string.IsNullOrWhiteSpace(generation.reason))
                throw new InvalidOperationException(
                    "V3 output must be accepted only as continuous paint material.");
        }

        private static void ValidateContinuousPackaging(ContinuousPackaging packaging)
        {
            if (packaging == null
                || packaging.tileSize != SquareTerrainArtProfile.TileSize
                || packaging.transitionBandPixels != SquareTerrainArtProfile.TransitionBandPixels
                || packaging.grassBlendInsidePixels
                    != SquareTerrainArtProfile.GrassBlendInsidePixels
                || packaging.grassFeatherBasePixels
                    != SquareTerrainArtProfile.GrassFeatherBasePixels
                || packaging.grassFeatherVariationPixels
                    != SquareTerrainArtProfile.GrassFeatherVariationPixels
                || packaging.grassFeatherOutsideMaxPixels
                    != SquareTerrainArtProfile.GrassFeatherOutsideMaxPixels
                || packaging.paintOutsideDepthPixels
                    != SquareTerrainArtProfile.PaintOutsideDepthPixels
                || packaging.protectedSocketPixels
                    != SquareTerrainArtProfile.ProtectedSocketPixels
                || !packaging.guideOwnsTopology
                || !packaging.diagonalMasksRemainDisconnected
                || !packaging.removeDetachedPaint
                || !packaging.exactBaseGrassRgbOnly
                || !packaging.deterministicAlphaModulationOnly
                || packaging.grassFeatherAlphaNear
                    != SquareTerrainArtProfile.GrassFeatherAlphaNear
                || packaging.grassFeatherAlphaFar
                    != SquareTerrainArtProfile.GrassFeatherAlphaFar
                || packaging.importsDirectionalSoilOrShadow
                || !packaging.mipmapsRequired
                || packaging.interpolationAllowed
                || packaging.perPixelRandomSampling
                || packaging.semanticColorListSampling
                || !packaging.tileableGrassSurface
                || !string.Equals(packaging.tangentPhaseMode,
                    "endpoint-locked-ping-pong", StringComparison.Ordinal)
                || !packaging.grassLipDetectedPerSourceColumn
                || packaging.grassLipOffsetMinPixels != 0
                || packaging.grassLipOffsetMaxPixels != 8
                || packaging.grassDripMaxDepthPixels
                    != SquareTerrainArtProfile.GrassFeatherVariationPixels
                || packaging.maximumTransparentLipGapPixels != 0
                || !string.Equals(packaging.grassDripEvents,
                    "source-derived-per-column-smoothed-with-12px-socket-taper",
                    StringComparison.Ordinal)
                || !string.Equals(packaging.grassSurfaceMode,
                    "exact-tiled-base-grass-texture", StringComparison.Ordinal)
                || !string.Equals(packaging.lipProfileMode,
                    "continuous-source-derived-irregular-lip",
                    StringComparison.Ordinal)
                || packaging.lipEndpointZeroPixels != 12
                || packaging.lipEventsPerTile != 0
                || !string.Equals(packaging.allowedIntegerTransforms,
                    "identity-rotate90-rotate180-rotate270-mirror", StringComparison.Ordinal)
                || !string.Equals(packaging.paintSampling,
                    "connected-boundary-source-lip-profile-plus-base-grass-feather-v7",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Continuous-ribbon packaging contract is missing or changed.");
        }

        private static void ValidateAttempt01(ImagegenAttempt attempt)
        {
            ValidateAttemptCommon(attempt, 1, SquareTerrainArtProfile.Attempt01PromptPath,
                Attempt01PromptSha256,
                new[]
                {
                    "E:/project/unity/furitDefense/Assets/LayeredTerrain/GrassSoil/Square/Topology/SquareContourTopologyGuide.png",
                    "C:/Users/18163/AppData/Local/Temp/codex-clipboard-3ed40819-8aab-4462-be2b-1288e8999535.png",
                },
                new[]
                {
                    SquareTerrainArtProfile.TopologyGuidePath,
                    SquareTerrainArtProfile.ApprovedStyleReferencePath,
                },
                "C:/Users/18163/.codex/generated_images/019f8df4-c04f-7911-866f-d09f5b56cd35/exec-a6606a11-dcac-479e-b42c-70e85b1a01ec.png");
            ValidateProvenanceImage(attempt.retainedOutput,
                SquareTerrainArtProfile.RejectedImagegenAttemptPath, Attempt01OutputSha256,
                1254, 1254, "RGB", "rejected imagegen attempt 1");
            if (!string.Equals(attempt.decision, "rejected", StringComparison.Ordinal)
                || attempt.paintSourceAccepted || attempt.atlasTopologyAcceptedDirectly
                || string.IsNullOrWhiteSpace(attempt.reason)
                || attempt.reason.IndexOf("checkerboard", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "Imagegen attempt 1 must remain rejected for baked checkerboard and topology drift.");
        }

        private static void ValidateAttempt02(ImagegenAttempt attempt)
        {
            ValidateAttemptCommon(attempt, 2, SquareTerrainArtProfile.Attempt02PromptPath,
                Attempt02PromptSha256,
                new[]
                {
                    "E:/project/unity/furitDefense/Assets/LayeredTerrain/GrassSoil/Square/Topology/SquareContourTopologyGuide.png",
                    "C:/Users/18163/AppData/Local/Temp/codex-clipboard-3ed40819-8aab-4462-be2b-1288e8999535.png",
                    "C:/Users/18163/.codex/generated_images/019f8df4-c04f-7911-866f-d09f5b56cd35/exec-a6606a11-dcac-479e-b42c-70e85b1a01ec.png",
                },
                new[]
                {
                    SquareTerrainArtProfile.TopologyGuidePath,
                    SquareTerrainArtProfile.ApprovedStyleReferencePath,
                    SquareTerrainArtProfile.RejectedImagegenAttemptPath,
                },
                "C:/Users/18163/.codex/generated_images/019f8df4-c04f-7911-866f-d09f5b56cd35/exec-22eedc7c-5544-48c8-8d77-9c77292ea2a9.png");
            ValidateProvenanceImage(attempt.retainedOutput,
                SquareTerrainArtProfile.RawImagegenDraftPath, Attempt02RawSha256,
                1254, 1254, "RGB", "imagegen attempt 2 raw paint source");
            if (!string.Equals(attempt.decision, "accepted-as-paint-source-only",
                    StringComparison.Ordinal)
                || !attempt.paintSourceAccepted || attempt.atlasTopologyAcceptedDirectly
                || string.IsNullOrWhiteSpace(attempt.reason))
                throw new InvalidOperationException(
                    "Imagegen attempt 2 must be accepted only as paint source, never as direct topology.");
        }

        private static void ValidateAttemptCommon(ImagegenAttempt attempt, int number,
            string promptPath, string promptSha256, string[] originalReferences,
            string[] retainedReferences, string toolOutputPath)
        {
            if (attempt == null || attempt.attempt != number
                || !string.Equals(attempt.toolName, "image_gen__imagegen",
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Imagegen attempt " + number
                    + " tool identity is missing or changed.");
            RequireString(attempt.promptPath, promptPath, "attempt " + number + " prompt path");
            RequireString(attempt.promptSha256, promptSha256,
                "attempt " + number + " prompt hash");
            ValidateFileHash(promptPath, promptSha256, "attempt " + number + " full prompt");
            if (string.IsNullOrWhiteSpace(File.ReadAllText(
                    SquareTerrainArtGenerator.AbsolutePath(promptPath))))
                throw new InvalidOperationException("Imagegen attempt " + number
                    + " full prompt is empty.");
            if (attempt.toolArguments == null)
                throw new InvalidOperationException("Imagegen attempt " + number
                    + " tool arguments are missing.");
            RequireArray(attempt.toolArguments.referenced_image_paths, originalReferences,
                "attempt " + number + " referenced_image_paths");
            RequireArray(attempt.retainedReferencedImages, retainedReferences,
                "attempt " + number + " retained references");
            RequireString(attempt.toolOutputPath, toolOutputPath,
                "attempt " + number + " original tool output path");
        }

        private static void ValidatePackaging(ImagegenPackaging packaging)
        {
            if (packaging == null
                || packaging.tileSize != SquareTerrainArtProfile.TileSize
                || packaging.atlasColumns != SquareTerrainArtProfile.AtlasColumns
                || packaging.atlasRows != SquareTerrainArtProfile.AtlasRows
                || !string.Equals(packaging.maskOrder, "row-major-00-through-15",
                    StringComparison.Ordinal)
                || packaging.transitionBandPixels != SquareTerrainArtProfile.TransitionBandPixels
                || packaging.paintOutsideDepthPixels
                    != SquareTerrainArtProfile.PaintOutsideDepthPixels
                || packaging.protectedSocketPixels
                    != SquareTerrainArtProfile.ProtectedSocketPixels
                || !string.Equals(packaging.chromaKey, "#ff00ff", StringComparison.Ordinal)
                || packaging.chromaTolerance != SquareTerrainArtProfile.ChromaTolerance
                || !packaging.guideOwnsTopology
                || !packaging.diagonalMasksRemainDisconnected
                || packaging.scriptedReplacementArtwork
                || packaging.directImagegenAtlasAllowed
                || !packaging.exactCandidateColorCopiesOnly
                || !string.Equals(packaging.paintSampling,
                    "full-atlas-signed-distance-orientation-semantic-remap",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Square imagegen provenance packaging contract is missing or changed.");
        }

        private static void ValidateProvenanceImage(ProvenanceImage image, string expectedPath,
            string expectedSha256, int expectedWidth, int expectedHeight,
            string expectedColorMode, string label)
        {
            if (image == null) throw new InvalidOperationException(label + " record is missing.");
            RequireString(image.path, expectedPath, label + " path");
            RequireString(image.sha256, expectedSha256, label + " hash");
            if (image.width != expectedWidth || image.height != expectedHeight
                || !string.Equals(image.colorMode, expectedColorMode, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " recorded dimensions or color mode changed.");
            ValidateFileHash(expectedPath, expectedSha256, label);
            var png = ReadPngInfo(expectedPath);
            if (png.Width != expectedWidth || png.Height != expectedHeight
                || !string.Equals(png.ColorMode, expectedColorMode, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " file dimensions or PNG color mode changed.");
        }

        private static void ValidateFileHash(string path, string expectedSha256, string label)
        {
            var absolute = SquareTerrainArtGenerator.AbsolutePath(path);
            if (!File.Exists(absolute))
                throw new FileNotFoundException(label + " is not retained in the project.", absolute);
            var actual = HashFile(path);
            if (!string.Equals(actual, expectedSha256, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " SHA256 changed: " + actual + ".");
        }

        private static PngInfo ReadPngInfo(string path)
        {
            var bytes = File.ReadAllBytes(SquareTerrainArtGenerator.AbsolutePath(path));
            if (bytes.Length < 26 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78
                || bytes[3] != 71 || bytes[4] != 13 || bytes[5] != 10
                || bytes[6] != 26 || bytes[7] != 10
                || bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82)
                throw new InvalidOperationException("Provenance image is not a PNG: " + path);
            var colorMode = bytes[25] == 2 ? "RGB" : bytes[25] == 6 ? "RGBA" : string.Empty;
            if (string.IsNullOrEmpty(colorMode))
                throw new InvalidOperationException("Unsupported provenance PNG color type at " + path + ".");
            return new PngInfo
            {
                Width = ReadBigEndianInt32(bytes, 16),
                Height = ReadBigEndianInt32(bytes, 20),
                ColorMode = colorMode,
            };
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static void RequireString(string actual, string expected, string label)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " must be '" + expected + "'.");
        }

        private static void RequireArray(string[] actual, string[] expected, string label)
        {
            if (actual == null || actual.Length != expected.Length)
                throw new InvalidOperationException(label + " length changed.");
            for (var index = 0; index < expected.Length; index++)
                if (!string.Equals(actual[index], expected[index], StringComparison.Ordinal))
                    throw new InvalidOperationException(label + " entry " + index + " changed.");
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (texture.LoadImage(File.ReadAllBytes(SquareTerrainArtGenerator.AbsolutePath(path)), false))
                return texture;
            UnityEngine.Object.DestroyImmediate(texture);
            throw new InvalidOperationException("PNG could not be decoded: " + path);
        }

        private static Color32[] CropAtlasTile(Texture2D atlas, int mask)
        {
            var size = SquareTerrainArtProfile.TileSize;
            var rowFromTop = mask / SquareTerrainArtProfile.AtlasColumns;
            var column = mask % SquareTerrainArtProfile.AtlasColumns;
            var atlasPixels = atlas.GetPixels32();
            var tile = new Color32[size * size];
            var baseX = column * size;
            var baseY = atlas.height - (rowFromTop + 1) * size;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tile[x + y * size] = atlasPixels[baseX + x + (baseY + y) * atlas.width];
            return tile;
        }

        private static string HashFamily(string folder)
        {
            using (var sha = SHA256.Create())
            {
                for (var mask = 0; mask < SquareTerrainArtProfile.MaskCount; mask++)
                {
                    var bytes = File.ReadAllBytes(SquareTerrainArtGenerator.AbsolutePath(
                        SquareTerrainArtProfile.MaskTexturePath(folder, mask)));
                    sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
                }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return ToHex(sha.Hash);
            }
        }

        private static string HashFile(string path)
        {
            var absolute = SquareTerrainArtGenerator.AbsolutePath(path);
            var extension = Path.GetExtension(path);
            if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
                return HashCanonicalText(File.ReadAllText(absolute));

            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(File.ReadAllBytes(absolute)));
        }

        internal static string HashCanonicalText(string text)
        {
            var canonical = text.Replace("\r\n", "\n").Replace('\r', '\n');
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (var index = 0; index < bytes.Length; index++)
                builder.Append(bytes[index].ToString("x2"));
            return builder.ToString();
        }
    }
}
