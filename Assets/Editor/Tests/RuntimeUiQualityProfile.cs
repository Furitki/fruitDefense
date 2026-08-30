using System;
using System.Collections.Generic;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Editor
{
    public readonly struct RuntimeUiQualityViewportCase
    {
        public RuntimeUiQualityViewportCase(string id, int width, int height,
            int safeTop, int safeBottom)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A stable viewport-case ID is required.", nameof(id));
            if (width <= 0 || height <= 0 || safeTop < 0 || safeBottom < 0
                || safeTop + safeBottom >= height)
            {
                throw new ArgumentOutOfRangeException(nameof(width),
                    "Viewport dimensions and representative safe-area insets must be valid.");
            }

            Id = id;
            Width = width;
            Height = height;
            SafeTop = safeTop;
            SafeBottom = safeBottom;
        }

        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public int SafeTop { get; }
        public int SafeBottom { get; }
        public Vector2Int Viewport => new Vector2Int(Width, Height);
        public Rect FullSafeArea => new Rect(0f, 0f, Width, Height);
        public Rect InsetSafeArea => new Rect(
            0f, SafeBottom, Width, Height - SafeTop - SafeBottom);
    }

    /// <summary>
    /// Editor-owned numeric authority for deterministic runtime-UI quality gates.
    /// Runtime layout and theme assets remain the authorities for draw geometry and
    /// production values; validation code must consume this profile instead of
    /// copying acceptance matrices or thresholds.
    /// </summary>
    public static class RuntimeUiQualityProfile
    {
        private static readonly RuntimeUiQualityViewportCase[] SupportedViewportCases =
        {
            new RuntimeUiQualityViewportCase("360x800", 360, 800, 32, 24),
            new RuntimeUiQualityViewportCase("375x812", 375, 812, 40, 21),
            new RuntimeUiQualityViewportCase("402x874", 402, 874, 44, 34),
            new RuntimeUiQualityViewportCase("430x932", 430, 932, 50, 36),
        };

        public static IReadOnlyList<RuntimeUiQualityViewportCase> Viewports =>
            SupportedViewportCases;

        public const float GeometryTolerance = .01f;
        public const float BaselineTolerance = 1f;
        public const float RepeatedCenterTolerance = 1f;
        public const float OpticalCenterToleranceLogical = 2f;
        public const float OpticalCenterToleranceSourcePixels = 4f;
        public const float IllustrationAspectTolerance = .01f;
        public const int IllustrationUnusedBarMaximum = 8;
        public const int LobbyThumbnailMinimumWidth = 72;
        public const int LobbyThumbnailMinimumHeight = 46;
        public const int ResultVistaMinimumWidth = 128;
        public const int ResultVistaMinimumHeight = 72;
        public const int ResultVistaCropMaximum = 24;
        public const byte TransparentEdgeRequiredAlpha = 0;
        public const byte NineSliceSignificantAlphaLow = 16;
        public const byte NineSliceSignificantAlphaHigh = 48;
        public const int MinimumNormalTextSize = 15;
        public const int MinimumTouchTarget = 44;
        public const int SpacingGrid = 4;
        public const int MinimumIconTextGap = 4;
        public const int MaximumIconTextGap = 8;
        public const int MinimumContentGap = 8;
        public const int MinimumContentInset = 8;
        public const int MinimumTextToBorderGap = 4;
        public const int EmphasisOutlineCapturePixels = 2;
        public const int SettlementOutcomeInkHeightMinimum = 28;
        public const int SettlementOutcomeInkHeightMaximum = 32;
        public const float SettlementOutcomeOccupancyMinimum = .64f;
        public const float SettlementOutcomeOccupancyMaximum = .72f;
        public const int SettlementOutcomePaddingMinimum = 6;
        public const int SettlementOutcomePaddingImbalanceMaximum = 2;
        public const int OccupiedContentCenterTolerance = 24;
        public const int OccupiedContentBottomGapMaximum = 100;
        public const int OppositeGutterTolerance = 1;
        public const float NormalTextContrast = 4.5f;
        public const float LargeOrBoldTextContrast = 3f;
        public const float DisabledReadableContrast = 3f;
        public const float NonTextContrast = 3f;
        public const int CommonIconCanvasSize = 96;
        public const int CommonIconSafeInset = 12;
        public const int CommonIconAlphaDimensionMinimum = 60;
        public const int CommonIconAlphaDimensionMaximum = 72;
        public const int DragCueAlphaShortDimensionMinimum = 64;
        public const int CommonIconOpticalShortEdgeMinimum = 16;
        public const int CommonIconOpticalMajorEdgeMinimum = 18;
        public const int CommonIconStrokeMinimum = 2;
        public const int MicroIconCanvasSize = 18;
        public const int MicroIconSafeInset = 1;
        public const int MicroIconAlphaDimensionMinimum = 15;
        public const int MicroIconAlphaDimensionMaximum = 16;
        public const int MicroIconSignificantPixelMinimum = 40;
        public const int MicroIconSignificantPixelMaximum = 210;
        public const float MicroIconOpticalCenterTolerance = 1.5f;
        public const float MicroIconSilhouetteIouMaximum = .8f;
        public const int NineSliceCanvasSize = 128;
        public const int NineSliceBorder = 32;
        public const int GameplayStageNineSliceBorder = 20;
        public const int NineSliceSafeInset = 20;
        public const int NineSliceMinimumDestination = 32;
        public const int NineSlicePartitionCoverageCount = 1;
        public const int NineSliceSeamToleranceDevicePixels = 0;
        public const int ProductionPixelsPerLogicalUnit = 2;
        public const int ProductionImporterPixelsPerUnit = 100;
        public const int PaintedUniqueExportCount = 54;

        public static int MinimumFontSize(RuntimeUiTypographyRole role)
        {
            switch (role)
            {
                case RuntimeUiTypographyRole.Display: return 28;
                case RuntimeUiTypographyRole.ScreenTitle: return 32;
                case RuntimeUiTypographyRole.SectionTitle: return 28;
                case RuntimeUiTypographyRole.Body: return 20;
                case RuntimeUiTypographyRole.ControlLabel: return 20;
                case RuntimeUiTypographyRole.Metric: return 24;
                case RuntimeUiTypographyRole.Supplemental: return 16;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static int LineHeight(RuntimeUiTypographyRole role)
        {
            switch (role)
            {
                case RuntimeUiTypographyRole.Display: return 34;
                case RuntimeUiTypographyRole.ScreenTitle: return 38;
                case RuntimeUiTypographyRole.SectionTitle: return 34;
                case RuntimeUiTypographyRole.Body: return 28;
                case RuntimeUiTypographyRole.ControlLabel: return 24;
                case RuntimeUiTypographyRole.Metric: return 28;
                case RuntimeUiTypographyRole.Supplemental: return 22;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static bool UsesDisplayFace(RuntimeUiTypographyRole role)
        {
            switch (role)
            {
                case RuntimeUiTypographyRole.Display:
                case RuntimeUiTypographyRole.ScreenTitle:
                case RuntimeUiTypographyRole.SectionTitle:
                case RuntimeUiTypographyRole.ControlLabel:
                    return true;
                case RuntimeUiTypographyRole.Body:
                case RuntimeUiTypographyRole.Metric:
                case RuntimeUiTypographyRole.Supplemental:
                    return false;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }
    }
}
