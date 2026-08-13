using System;

namespace FruitDefense.Editor
{
    /// <summary>
    /// Immutable packaging contract for the first native-size square contour family.
    /// Organic assets intentionally keep their existing 32 px import settings.
    /// </summary>
    public sealed class SquareTerrainArtProfile
    {
        public const int MaskCount = 16;
        public const int AtlasColumns = 4;
        public const int AtlasRows = 4;
        public const int TileSize = 256;
        public const int AtlasSize = TileSize * AtlasColumns;
        public const int CornerRadius = 48;
        public const int ProtectedSocketPixels = 2;
        public const int TransitionBandPixels = 16;
        public const int GrassBlendInsidePixels = 8;
        public const int GrassFeatherBasePixels = 4;
        public const int GrassFeatherVariationPixels = 3;
        public const int GrassFeatherOutsideMaxPixels =
            GrassFeatherBasePixels + GrassFeatherVariationPixels;
        public const int GrassDripSocketTaperPixels = 12;
        public const int GrassFeatherAlphaNear = 224;
        public const int GrassFeatherAlphaFar = 24;
        public const int PaintOutsideDepthPixels = GrassFeatherOutsideMaxPixels;
        public const int ChromaTolerance = 24;

        public const string Root = "Assets/LayeredTerrain/GrassSoil/Square";
        public const string TopologyFolder = Root + "/Topology";
        public const string SourcesFolder = Root + "/Sources";
        public const string GrassLandformFolder = Root + "/LandformGrass";
        public const string SoilLandformFolder = Root + "/LandformSoil";
        public const string StoneRoadLandformFolder = Root + "/LandformStoneRoad";
        public const string GrassOnSoilEdgeFolder = Root + "/EdgeGrassOnSoilPainted";

        public const string TopologyGuidePath =
            TopologyFolder + "/SquareContourTopologyGuide.png";
        public const string ImagegenReferencePath =
            TopologyFolder + "/SquareContourImagegenReference.png";
        public const string CandidatePath =
            SourcesFolder + "/GrassOnSoilSquareCandidate.png";
        public const string CandidateProvenancePath =
            SourcesFolder + "/GrassOnSoilSquareCandidate.provenance.json";
        public const string ApprovedStyleReferencePath =
            SourcesFolder + "/ApprovedStyleReference.png";
        public const string RawImagegenDraftPath =
            SourcesFolder + "/GrassOnSoilSquareImagegenDraft.png";
        public const string RejectedImagegenAttemptPath =
            SourcesFolder + "/GrassOnSoilSquareImagegenAttempt01Rejected.png";
        public const string Attempt01PromptPath =
            SourcesFolder + "/GrassOnSoilSquareAttempt01.prompt.txt";
        public const string Attempt02PromptPath =
            SourcesFolder + "/GrassOnSoilSquareAttempt02.prompt.txt";
        public const string ContinuousRibbonPath =
            SourcesFolder + "/GrassOnSoilContinuousRibbon-v1.png";
        public const string ContinuousRibbonPromptPath =
            SourcesFolder + "/GrassOnSoilContinuousRibbon-v1.prompt.txt";
        public const string ContinuousRibbonProvenancePath =
            SourcesFolder + "/GrassOnSoilContinuousRibbon-v1.provenance.json";
        public const string SemanticSamplingRejectedPath =
            SourcesFolder + "/GrassOnSoilSquareSemanticSamplingRejected.png";

        public const string GrassBaseSourcePath =
            "Assets/LayeredTerrain/GrassSoil/Base/Grass.png";
        public const string SoilBaseSourcePath =
            "Assets/LayeredTerrain/GrassSoil/Base/Soil.png";
        public const string StoneRoadSourcePath =
            "Assets/DualGridTerrain/StoneFloor/Generated/Mask-15.png";

        public const string GrassLandformTileSetPath =
            GrassLandformFolder + "/GrassSquareLandformTileSet.asset";
        public const string SoilLandformTileSetPath =
            SoilLandformFolder + "/SoilSquareLandformTileSet.asset";
        public const string StoneRoadLandformTileSetPath =
            StoneRoadLandformFolder + "/StoneRoadSquareLandformTileSet.asset";
        public const string GrassOnSoilEdgeTileSetPath =
            GrassOnSoilEdgeFolder + "/GrassOnSoilSquarePaintedTileSet.asset";

        public const string EvidenceFolder = "Builds/Evidence/terrain-contours";
        public const string ValidationEvidencePath =
            EvidenceFolder + "/square-contour-validation.json";
        public const string SquareBoardEvidencePath =
            EvidenceFolder + "/square-contour-board.png";
        public const string OrganicBoardEvidencePath =
            EvidenceFolder + "/organic-contour-board.png";
        public const string CoexistenceBoardEvidencePath =
            EvidenceFolder + "/square-organic-coexistence-board.png";
        public const string BattleScaleBoardEvidencePath =
            EvidenceFolder + "/square-battle-scale-board.png";

        public const string OrganicGrassFolder =
            "Assets/LayeredTerrain/GrassSoil/LandformGrass";

        public static string MaskTexturePath(string folder, int mask)
        {
            ValidateMask(mask);
            return folder + "/Mask-" + mask.ToString("00") + ".png";
        }

        public static string MaskTilePath(string folder, int mask)
        {
            ValidateMask(mask);
            return folder + "/Mask-" + mask.ToString("00") + ".asset";
        }

        public static void ValidateContract()
        {
            if (TileSize < 128)
                throw new InvalidOperationException("Square contour tiles must be at least 128 px.");
            if (AtlasColumns * AtlasRows != MaskCount || AtlasSize != 1024)
                throw new InvalidOperationException("Square contour atlas contract must remain 4x4 at 1024 px.");
            if (CornerRadius <= 0 || CornerRadius >= TileSize / 2)
                throw new InvalidOperationException("Square contour corner radius is outside the tile quadrant.");
            if (ProtectedSocketPixels <= 0 || ProtectedSocketPixels > CornerRadius)
                throw new InvalidOperationException("Square contour socket width is invalid.");
            if (TransitionBandPixels < GrassFeatherOutsideMaxPixels
                || TransitionBandPixels >= CornerRadius)
                throw new InvalidOperationException(
                    "Square transition band must remain narrow and tile-bounded.");
            if (PaintOutsideDepthPixels < ProtectedSocketPixels
                || PaintOutsideDepthPixels > TransitionBandPixels)
                throw new InvalidOperationException(
                    "Square painted edge outside depth must be visible and band-bounded.");
            if (GrassBlendInsidePixels < ProtectedSocketPixels
                || GrassBlendInsidePixels >= TransitionBandPixels)
                throw new InvalidOperationException(
                    "Square inside blend must cover the contact without becoming a broad rim.");
            if (GrassFeatherBasePixels <= 0 || GrassFeatherVariationPixels < 0
                || GrassFeatherOutsideMaxPixels > TransitionBandPixels)
                throw new InvalidOperationException(
                    "Square outside feather dimensions are invalid.");
            if (GrassFeatherAlphaNear >= 255 || GrassFeatherAlphaFar <= 0
                || GrassFeatherAlphaNear <= GrassFeatherAlphaFar)
                throw new InvalidOperationException(
                    "Square grass feather alpha must fade without an opaque edge or hard cutoff.");
        }

        private static void ValidateMask(int mask)
        {
            if (mask < 0 || mask >= MaskCount)
                throw new ArgumentOutOfRangeException("mask", mask, "Dual-Grid mask must be 0..15.");
        }
    }
}
