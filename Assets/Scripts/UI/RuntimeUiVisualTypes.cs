using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FruitDefense.UI
{
    public enum RuntimeUiArtSlot
    {
        SurfaceScreenBackground = 0,
        SurfaceSafeArea = 1,
        SurfacePanelStandard = 2,
        SurfacePanelRaised = 3,
        SurfaceCardSelectable = 4,
        SurfaceMetric = 5,
        SurfaceStatus = 6,
        SurfaceDetail = 7,
        SurfaceModal = 8,
        SurfaceResult = 9,
        SurfaceScrim = 10,
        ActionPrimary = 11,
        ActionSecondary = 12,
        ActionQuiet = 13,
        ActionDanger = 14,
        SlotTool = 15,
        SlotNursery = 16,
        MarkerSelected = 17,
        IndicatorDisabled = 18,
        IndicatorLoading = 19,
        IndicatorSuccess = 20,
        IndicatorWarning = 21,
        IndicatorError = 22,
        IndicatorDragLegal = 23,
        IndicatorDragIllegal = 24,
        IndicatorMerge = 25,
        IndicatorSwap = 26,
        IconResourceSun = 27,
        IconResourceCore = 28,
        IconResourceWave = 29,
        IconControlPause = 30,
        IconControlContinue = 31,
        IconControlSpeed = 32,
        IconControlStartWave = 33,
        IconControlRetry = 34,
        IconControlReturn = 35,
        IconControlClose = 36,
        IconToolPot = 37,
        IconControlStart = 38,
        IconControlRefresh = 39,
        OrnamentScreenCorner = 40,
        SurfaceSectionRibbon = 41,
        SurfaceIllustrationFrame = 42,
        OrnamentMetricDivider = 43,
        OrnamentResultBanner = 44,
        IllustrationOrchardVista = 45,
        IllustrationLobbyOrchard01 = 46,
        IllustrationLobbyOrchard02 = 47,
        IllustrationLobbyOrchard03 = 48,
    }

    public enum RuntimeUiArtGeometry
    {
        Stretch = 0,
        NineSlice = 1,
        Icon = 2,
    }

    public enum RuntimeUiComponentKind
    {
        Screen = 0,
        SafeArea = 1,
        StandardPanel = 2,
        RaisedPanel = 3,
        SelectableCard = 4,
        PrimaryButton = 5,
        SecondaryButton = 6,
        QuietButton = 7,
        DangerButton = 8,
        Metric = 9,
        Status = 10,
        DetailCard = 11,
        BlockingModal = 12,
        ResultCard = 13,
        ToolSlot = 14,
        NurserySlot = 15,
    }

    public enum RuntimeUiInteractionState
    {
        Normal = 0,
        HoveredOrFocused = 1,
        Pressed = 2,
        Disabled = 3,
        Selected = 4,
        Loading = 5,
        Success = 6,
        Warning = 7,
        Error = 8,
    }

    public enum RuntimeUiTypographyRole
    {
        Display = 0,
        ScreenTitle = 1,
        SectionTitle = 2,
        Body = 3,
        ControlLabel = 4,
        Metric = 5,
        Supplemental = 6,
    }

    public static class RuntimeUiArtSlots
    {
        private static readonly RuntimeUiArtSlot[] RequiredSlotArray =
        {
            RuntimeUiArtSlot.SurfaceScreenBackground,
            RuntimeUiArtSlot.SurfaceSafeArea,
            RuntimeUiArtSlot.SurfacePanelStandard,
            RuntimeUiArtSlot.SurfacePanelRaised,
            RuntimeUiArtSlot.SurfaceCardSelectable,
            RuntimeUiArtSlot.SurfaceMetric,
            RuntimeUiArtSlot.SurfaceStatus,
            RuntimeUiArtSlot.SurfaceDetail,
            RuntimeUiArtSlot.SurfaceModal,
            RuntimeUiArtSlot.SurfaceResult,
            RuntimeUiArtSlot.SurfaceScrim,
            RuntimeUiArtSlot.ActionPrimary,
            RuntimeUiArtSlot.ActionSecondary,
            RuntimeUiArtSlot.ActionQuiet,
            RuntimeUiArtSlot.ActionDanger,
            RuntimeUiArtSlot.SlotTool,
            RuntimeUiArtSlot.SlotNursery,
            RuntimeUiArtSlot.MarkerSelected,
            RuntimeUiArtSlot.IndicatorDisabled,
            RuntimeUiArtSlot.IndicatorLoading,
            RuntimeUiArtSlot.IndicatorSuccess,
            RuntimeUiArtSlot.IndicatorWarning,
            RuntimeUiArtSlot.IndicatorError,
            RuntimeUiArtSlot.IndicatorDragLegal,
            RuntimeUiArtSlot.IndicatorDragIllegal,
            RuntimeUiArtSlot.IndicatorMerge,
            RuntimeUiArtSlot.IndicatorSwap,
            RuntimeUiArtSlot.IconResourceSun,
            RuntimeUiArtSlot.IconResourceCore,
            RuntimeUiArtSlot.IconResourceWave,
            RuntimeUiArtSlot.IconControlPause,
            RuntimeUiArtSlot.IconControlContinue,
            RuntimeUiArtSlot.IconControlSpeed,
            RuntimeUiArtSlot.IconControlStartWave,
            RuntimeUiArtSlot.IconControlRetry,
            RuntimeUiArtSlot.IconControlReturn,
            RuntimeUiArtSlot.IconControlClose,
            RuntimeUiArtSlot.IconToolPot,
            RuntimeUiArtSlot.IconControlStart,
            RuntimeUiArtSlot.IconControlRefresh,
            RuntimeUiArtSlot.OrnamentScreenCorner,
            RuntimeUiArtSlot.SurfaceSectionRibbon,
            RuntimeUiArtSlot.SurfaceIllustrationFrame,
            RuntimeUiArtSlot.OrnamentMetricDivider,
            RuntimeUiArtSlot.OrnamentResultBanner,
            RuntimeUiArtSlot.IllustrationOrchardVista,
            RuntimeUiArtSlot.IllustrationLobbyOrchard01,
            RuntimeUiArtSlot.IllustrationLobbyOrchard02,
            RuntimeUiArtSlot.IllustrationLobbyOrchard03,
        };

        private static readonly ReadOnlyCollection<RuntimeUiArtSlot> ReadOnlyRequiredSlots =
            Array.AsReadOnly(RequiredSlotArray);

        public static IReadOnlyList<RuntimeUiArtSlot> Required => ReadOnlyRequiredSlots;
        public static int RequiredCount => RequiredSlotArray.Length;

        public static bool IsRequired(RuntimeUiArtSlot slot)
        {
            var value = (int)slot;
            return value >= (int)RuntimeUiArtSlot.SurfaceScreenBackground
                && value <= (int)RuntimeUiArtSlot.IllustrationLobbyOrchard03;
        }

        public static int RequiredIndex(RuntimeUiArtSlot slot)
        {
            return IsRequired(slot) ? (int)slot : -1;
        }

        public static RuntimeUiArtGeometry Geometry(RuntimeUiArtSlot slot)
        {
            if (!IsRequired(slot))
                throw new ArgumentOutOfRangeException(nameof(slot), slot,
                    "The UI art slot is not part of the finite runtime contract.");

            switch (slot)
            {
                case RuntimeUiArtSlot.SurfaceScreenBackground:
                case RuntimeUiArtSlot.SurfaceScrim:
                case RuntimeUiArtSlot.OrnamentMetricDivider:
                case RuntimeUiArtSlot.OrnamentResultBanner:
                case RuntimeUiArtSlot.IllustrationOrchardVista:
                case RuntimeUiArtSlot.IllustrationLobbyOrchard01:
                case RuntimeUiArtSlot.IllustrationLobbyOrchard02:
                case RuntimeUiArtSlot.IllustrationLobbyOrchard03:
                    return RuntimeUiArtGeometry.Stretch;
                case RuntimeUiArtSlot.SurfaceSafeArea:
                case RuntimeUiArtSlot.SurfacePanelStandard:
                case RuntimeUiArtSlot.SurfacePanelRaised:
                case RuntimeUiArtSlot.SurfaceCardSelectable:
                case RuntimeUiArtSlot.SurfaceMetric:
                case RuntimeUiArtSlot.SurfaceStatus:
                case RuntimeUiArtSlot.SurfaceDetail:
                case RuntimeUiArtSlot.SurfaceModal:
                case RuntimeUiArtSlot.SurfaceResult:
                case RuntimeUiArtSlot.ActionPrimary:
                case RuntimeUiArtSlot.ActionSecondary:
                case RuntimeUiArtSlot.ActionQuiet:
                case RuntimeUiArtSlot.ActionDanger:
                case RuntimeUiArtSlot.SlotTool:
                case RuntimeUiArtSlot.SlotNursery:
                case RuntimeUiArtSlot.SurfaceSectionRibbon:
                case RuntimeUiArtSlot.SurfaceIllustrationFrame:
                    return RuntimeUiArtGeometry.NineSlice;
                case RuntimeUiArtSlot.MarkerSelected:
                case RuntimeUiArtSlot.IndicatorDisabled:
                case RuntimeUiArtSlot.IndicatorLoading:
                case RuntimeUiArtSlot.IndicatorSuccess:
                case RuntimeUiArtSlot.IndicatorWarning:
                case RuntimeUiArtSlot.IndicatorError:
                case RuntimeUiArtSlot.IndicatorDragLegal:
                case RuntimeUiArtSlot.IndicatorDragIllegal:
                case RuntimeUiArtSlot.IndicatorMerge:
                case RuntimeUiArtSlot.IndicatorSwap:
                case RuntimeUiArtSlot.IconResourceSun:
                case RuntimeUiArtSlot.IconResourceCore:
                case RuntimeUiArtSlot.IconResourceWave:
                case RuntimeUiArtSlot.IconControlPause:
                case RuntimeUiArtSlot.IconControlContinue:
                case RuntimeUiArtSlot.IconControlSpeed:
                case RuntimeUiArtSlot.IconControlStartWave:
                case RuntimeUiArtSlot.IconControlRetry:
                case RuntimeUiArtSlot.IconControlReturn:
                case RuntimeUiArtSlot.IconControlClose:
                case RuntimeUiArtSlot.IconToolPot:
                case RuntimeUiArtSlot.IconControlStart:
                case RuntimeUiArtSlot.IconControlRefresh:
                case RuntimeUiArtSlot.OrnamentScreenCorner:
                    return RuntimeUiArtGeometry.Icon;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot,
                        "The UI art slot is not part of the finite runtime contract.");
            }
        }

        public static string SemanticId(RuntimeUiArtSlot slot)
        {
            switch (slot)
            {
                case RuntimeUiArtSlot.SurfaceScreenBackground: return "surface.screen-background";
                case RuntimeUiArtSlot.SurfaceSafeArea: return "surface.safe-area";
                case RuntimeUiArtSlot.SurfacePanelStandard: return "surface.panel-standard";
                case RuntimeUiArtSlot.SurfacePanelRaised: return "surface.panel-raised";
                case RuntimeUiArtSlot.SurfaceCardSelectable: return "surface.card-selectable";
                case RuntimeUiArtSlot.SurfaceMetric: return "surface.metric";
                case RuntimeUiArtSlot.SurfaceStatus: return "surface.status";
                case RuntimeUiArtSlot.SurfaceDetail: return "surface.detail";
                case RuntimeUiArtSlot.SurfaceModal: return "surface.modal";
                case RuntimeUiArtSlot.SurfaceResult: return "surface.result";
                case RuntimeUiArtSlot.SurfaceScrim: return "surface.scrim";
                case RuntimeUiArtSlot.ActionPrimary: return "action.primary";
                case RuntimeUiArtSlot.ActionSecondary: return "action.secondary";
                case RuntimeUiArtSlot.ActionQuiet: return "action.quiet";
                case RuntimeUiArtSlot.ActionDanger: return "action.danger";
                case RuntimeUiArtSlot.SlotTool: return "slot.tool";
                case RuntimeUiArtSlot.SlotNursery: return "slot.nursery";
                case RuntimeUiArtSlot.MarkerSelected: return "marker.selected";
                case RuntimeUiArtSlot.IndicatorDisabled: return "indicator.disabled";
                case RuntimeUiArtSlot.IndicatorLoading: return "indicator.loading";
                case RuntimeUiArtSlot.IndicatorSuccess: return "indicator.success";
                case RuntimeUiArtSlot.IndicatorWarning: return "indicator.warning";
                case RuntimeUiArtSlot.IndicatorError: return "indicator.error";
                case RuntimeUiArtSlot.IndicatorDragLegal: return "indicator.drag-legal";
                case RuntimeUiArtSlot.IndicatorDragIllegal: return "indicator.drag-illegal";
                case RuntimeUiArtSlot.IndicatorMerge: return "indicator.merge";
                case RuntimeUiArtSlot.IndicatorSwap: return "indicator.swap";
                case RuntimeUiArtSlot.IconResourceSun: return "icon.resource-sun";
                case RuntimeUiArtSlot.IconResourceCore: return "icon.resource-core";
                case RuntimeUiArtSlot.IconResourceWave: return "icon.resource-wave";
                case RuntimeUiArtSlot.IconControlPause: return "icon.control-pause";
                case RuntimeUiArtSlot.IconControlContinue: return "icon.control-continue";
                case RuntimeUiArtSlot.IconControlSpeed: return "icon.control-speed";
                case RuntimeUiArtSlot.IconControlStartWave: return "icon.control-start-wave";
                case RuntimeUiArtSlot.IconControlRetry: return "icon.control-retry";
                case RuntimeUiArtSlot.IconControlReturn: return "icon.control-return";
                case RuntimeUiArtSlot.IconControlClose: return "icon.control-close";
                case RuntimeUiArtSlot.IconToolPot: return "icon.tool-pot";
                case RuntimeUiArtSlot.IconControlStart: return "icon.control-start";
                case RuntimeUiArtSlot.IconControlRefresh: return "icon.control-refresh";
                case RuntimeUiArtSlot.OrnamentScreenCorner: return "ornament.screen-corner";
                case RuntimeUiArtSlot.SurfaceSectionRibbon: return "surface.section-ribbon";
                case RuntimeUiArtSlot.SurfaceIllustrationFrame: return "surface.illustration-frame";
                case RuntimeUiArtSlot.OrnamentMetricDivider: return "ornament.metric-divider";
                case RuntimeUiArtSlot.OrnamentResultBanner: return "ornament.result-banner";
                case RuntimeUiArtSlot.IllustrationOrchardVista: return "illustration.orchard-vista";
                case RuntimeUiArtSlot.IllustrationLobbyOrchard01: return "illustration.lobby-orchard-01";
                case RuntimeUiArtSlot.IllustrationLobbyOrchard02: return "illustration.lobby-orchard-02";
                case RuntimeUiArtSlot.IllustrationLobbyOrchard03: return "illustration.lobby-orchard-03";
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot,
                        "The UI art slot is not part of the finite runtime contract.");
            }
        }
    }

    [Serializable]
    public struct RuntimeUiPixelInsets
    {
        [SerializeField, Min(0)] private int left;
        [SerializeField, Min(0)] private int top;
        [SerializeField, Min(0)] private int right;
        [SerializeField, Min(0)] private int bottom;

        public RuntimeUiPixelInsets(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public int Left => left;
        public int Top => top;
        public int Right => right;
        public int Bottom => bottom;
        public long Horizontal => (long)left + right;
        public long Vertical => (long)top + bottom;
        public bool IsZero => left == 0 && top == 0 && right == 0 && bottom == 0;
        public bool HasNegativeValue => left < 0 || top < 0 || right < 0 || bottom < 0;
    }

    [Serializable]
    public struct RuntimeUiTypographyStyle
    {
        [SerializeField, Min(1)] private int fontSize;
        [SerializeField, Min(1)] private int lineHeight;
        [SerializeField] private FontStyle fontStyle;

        public RuntimeUiTypographyStyle(int fontSize, int lineHeight, FontStyle fontStyle)
        {
            this.fontSize = fontSize;
            this.lineHeight = lineHeight;
            this.fontStyle = fontStyle;
        }

        public int FontSize => fontSize;
        public int LineHeight => lineHeight;
        public FontStyle FontStyle => fontStyle;

        internal void AppendValidation(RuntimeUiValidationResult result, string field)
        {
            if (fontSize <= 0)
                result.Add("theme.typography.font-size", field + ".fontSize",
                    "Font size must be positive.");
            if (lineHeight < fontSize)
                result.Add("theme.typography.line-height", field + ".lineHeight",
                    "Line height must be at least the font size.");
            if (!Enum.IsDefined(typeof(FontStyle), fontStyle))
                result.Add("theme.typography.font-style", field + ".fontStyle",
                    "Font style is outside Unity's supported values.");
        }
    }

    [Serializable]
    public struct RuntimeUiTypographyTokens
    {
        [SerializeField] private RuntimeUiTypographyStyle display;
        [SerializeField] private RuntimeUiTypographyStyle screenTitle;
        [SerializeField] private RuntimeUiTypographyStyle sectionTitle;
        [SerializeField] private RuntimeUiTypographyStyle body;
        [SerializeField] private RuntimeUiTypographyStyle controlLabel;
        [SerializeField] private RuntimeUiTypographyStyle metric;
        [SerializeField] private RuntimeUiTypographyStyle supplemental;

        public RuntimeUiTypographyStyle Display => display;
        public RuntimeUiTypographyStyle ScreenTitle => screenTitle;
        public RuntimeUiTypographyStyle SectionTitle => sectionTitle;
        public RuntimeUiTypographyStyle Body => body;
        public RuntimeUiTypographyStyle ControlLabel => controlLabel;
        public RuntimeUiTypographyStyle Metric => metric;
        public RuntimeUiTypographyStyle Supplemental => supplemental;

        public RuntimeUiTypographyStyle For(RuntimeUiTypographyRole role)
        {
            switch (role)
            {
                case RuntimeUiTypographyRole.Display: return display;
                case RuntimeUiTypographyRole.ScreenTitle: return screenTitle;
                case RuntimeUiTypographyRole.SectionTitle: return sectionTitle;
                case RuntimeUiTypographyRole.Body: return body;
                case RuntimeUiTypographyRole.ControlLabel: return controlLabel;
                case RuntimeUiTypographyRole.Metric: return metric;
                case RuntimeUiTypographyRole.Supplemental: return supplemental;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, null);
            }
        }

        public static RuntimeUiTypographyTokens SunnyOrchardDefault()
        {
            return new RuntimeUiTypographyTokens
            {
                display = new RuntimeUiTypographyStyle(40, 46, FontStyle.Bold),
                screenTitle = new RuntimeUiTypographyStyle(32, 38, FontStyle.Bold),
                sectionTitle = new RuntimeUiTypographyStyle(28, 34, FontStyle.Bold),
                body = new RuntimeUiTypographyStyle(20, 28, FontStyle.Normal),
                controlLabel = new RuntimeUiTypographyStyle(20, 24, FontStyle.Bold),
                metric = new RuntimeUiTypographyStyle(24, 28, FontStyle.Bold),
                supplemental = new RuntimeUiTypographyStyle(16, 22, FontStyle.Normal),
            };
        }

        internal void AppendValidation(RuntimeUiValidationResult result, string field)
        {
            display.AppendValidation(result, field + ".display");
            screenTitle.AppendValidation(result, field + ".screenTitle");
            sectionTitle.AppendValidation(result, field + ".sectionTitle");
            body.AppendValidation(result, field + ".body");
            controlLabel.AppendValidation(result, field + ".controlLabel");
            metric.AppendValidation(result, field + ".metric");
            supplemental.AppendValidation(result, field + ".supplemental");
        }
    }

    [Serializable]
    public struct RuntimeUiSemanticColors
    {
        [SerializeField] private Color edgeBackground;
        [SerializeField] private Color baseSurface;
        [SerializeField] private Color raisedSurface;
        [SerializeField] private Color primaryAction;
        [SerializeField] private Color secondaryAction;
        [SerializeField] private Color selectionAccent;
        [SerializeField] private Color success;
        [SerializeField] private Color warning;
        [SerializeField] private Color danger;
        [SerializeField] private Color disabled;
        [SerializeField] private Color scrim;
        [SerializeField] private Color primaryText;
        [SerializeField] private Color secondaryText;
        [SerializeField] private Color inverseText;

        public Color EdgeBackground => edgeBackground;
        public Color BaseSurface => baseSurface;
        public Color RaisedSurface => raisedSurface;
        public Color PrimaryAction => primaryAction;
        public Color SecondaryAction => secondaryAction;
        public Color SelectionAccent => selectionAccent;
        public Color Success => success;
        public Color Warning => warning;
        public Color Danger => danger;
        public Color Disabled => disabled;
        public Color Scrim => scrim;
        public Color PrimaryText => primaryText;
        public Color SecondaryText => secondaryText;
        public Color InverseText => inverseText;

        public static RuntimeUiSemanticColors SunnyOrchardDefault()
        {
            return new RuntimeUiSemanticColors
            {
                edgeBackground = new Color32(245, 221, 174, 255),
                baseSurface = new Color32(255, 246, 224, 255),
                raisedSurface = new Color32(255, 231, 163, 255),
                primaryAction = new Color32(85, 154, 57, 255),
                secondaryAction = new Color32(143, 191, 116, 255),
                selectionAccent = new Color32(255, 210, 77, 255),
                success = new Color32(109, 190, 75, 255),
                warning = new Color32(255, 185, 66, 255),
                danger = new Color32(211, 78, 69, 255),
                disabled = new Color32(143, 191, 116, 255),
                scrim = new Color32(61, 42, 32, 255),
                primaryText = new Color32(139, 94, 60, 255),
                secondaryText = new Color32(111, 90, 69, 255),
                inverseText = new Color32(255, 246, 224, 255),
            };
        }

        internal void AppendValidation(RuntimeUiValidationResult result, string field)
        {
            RuntimeUiNumbers.ValidateColor(result, field + ".edgeBackground", edgeBackground);
            RuntimeUiNumbers.ValidateColor(result, field + ".baseSurface", baseSurface);
            RuntimeUiNumbers.ValidateColor(result, field + ".raisedSurface", raisedSurface);
            RuntimeUiNumbers.ValidateColor(result, field + ".primaryAction", primaryAction);
            RuntimeUiNumbers.ValidateColor(result, field + ".secondaryAction", secondaryAction);
            RuntimeUiNumbers.ValidateColor(result, field + ".selectionAccent", selectionAccent);
            RuntimeUiNumbers.ValidateColor(result, field + ".success", success);
            RuntimeUiNumbers.ValidateColor(result, field + ".warning", warning);
            RuntimeUiNumbers.ValidateColor(result, field + ".danger", danger);
            RuntimeUiNumbers.ValidateColor(result, field + ".disabled", disabled);
            RuntimeUiNumbers.ValidateColor(result, field + ".scrim", scrim);
            RuntimeUiNumbers.ValidateColor(result, field + ".primaryText", primaryText);
            RuntimeUiNumbers.ValidateColor(result, field + ".secondaryText", secondaryText);
            RuntimeUiNumbers.ValidateColor(result, field + ".inverseText", inverseText);
        }
    }

    [Serializable]
    public struct RuntimeUiMetrics
    {
        [SerializeField, Min(4)] private int spacingXs;
        [SerializeField, Min(4)] private int spacingSm;
        [SerializeField, Min(4)] private int spacingMd;
        [SerializeField, Min(4)] private int spacingLg;
        [SerializeField, Min(4)] private int spacingXl;
        [SerializeField, Min(4)] private int spacingXxl;
        [SerializeField, Min(1)] private int touchTargetMinimum;
        [SerializeField, Min(0)] private int surfaceInset;
        [SerializeField, Min(0)] private int componentGap;
        [SerializeField, Min(1)] private int outlineThin;
        [SerializeField, Min(1)] private int outlineStrong;
        [SerializeField, Min(0)] private int cornerSmall;
        [SerializeField, Min(0)] private int cornerMedium;
        [SerializeField, Min(0)] private int cornerLarge;
        [SerializeField, Min(0)] private int pressedOffset;
        [SerializeField, Min(0)] private int shallowShadowOffset;

        public int SpacingXs => spacingXs;
        public int SpacingSm => spacingSm;
        public int SpacingMd => spacingMd;
        public int SpacingLg => spacingLg;
        public int SpacingXl => spacingXl;
        public int SpacingXxl => spacingXxl;
        public int TouchTargetMinimum => touchTargetMinimum;
        public int SurfaceInset => surfaceInset;
        public int ComponentGap => componentGap;
        public int OutlineThin => outlineThin;
        public int OutlineStrong => outlineStrong;
        public int CornerSmall => cornerSmall;
        public int CornerMedium => cornerMedium;
        public int CornerLarge => cornerLarge;
        public int PressedOffset => pressedOffset;
        public int ShallowShadowOffset => shallowShadowOffset;

        public static RuntimeUiMetrics SunnyOrchardDefault()
        {
            return new RuntimeUiMetrics
            {
                spacingXs = 4,
                spacingSm = 8,
                spacingMd = 12,
                spacingLg = 16,
                spacingXl = 24,
                spacingXxl = 32,
                touchTargetMinimum = 44,
                surfaceInset = 16,
                componentGap = 8,
                outlineThin = 2,
                outlineStrong = 3,
                cornerSmall = 8,
                cornerMedium = 12,
                cornerLarge = 14,
                pressedOffset = 2,
                shallowShadowOffset = 2,
            };
        }

        internal void AppendValidation(RuntimeUiValidationResult result, string field)
        {
            ValidateSpacing(result, field + ".spacingXs", spacingXs, 0);
            ValidateSpacing(result, field + ".spacingSm", spacingSm, spacingXs);
            ValidateSpacing(result, field + ".spacingMd", spacingMd, spacingSm);
            ValidateSpacing(result, field + ".spacingLg", spacingLg, spacingMd);
            ValidateSpacing(result, field + ".spacingXl", spacingXl, spacingLg);
            ValidateSpacing(result, field + ".spacingXxl", spacingXxl, spacingXl);
            ValidateFourPointValue(result, field + ".touchTargetMinimum", touchTargetMinimum, false);
            ValidateFourPointValue(result, field + ".surfaceInset", surfaceInset, true);
            ValidateFourPointValue(result, field + ".componentGap", componentGap, true);

            if (outlineThin <= 0 || outlineStrong < outlineThin)
                result.Add("theme.metrics.outline", field + ".outline",
                    "Outline weights must be positive and strong must not be thinner than thin.");
            if (cornerSmall < 0 || cornerMedium < cornerSmall || cornerLarge < cornerMedium)
                result.Add("theme.metrics.corner", field + ".corner",
                    "Corner families must be non-negative and ordered small, medium, large.");
            if (pressedOffset < 0 || shallowShadowOffset < 0)
                result.Add("theme.metrics.offset", field + ".offset",
                    "Pressed and shallow-shadow offsets cannot be negative.");
        }

        private static void ValidateSpacing(RuntimeUiValidationResult result, string field,
            int value, int previous)
        {
            ValidateFourPointValue(result, field, value, false);
            if (value < previous)
                result.Add("theme.metrics.spacing-order", field,
                    "Spacing tokens must remain in ascending order.");
        }

        private static void ValidateFourPointValue(RuntimeUiValidationResult result, string field,
            int value, bool allowZero)
        {
            if ((allowZero ? value < 0 : value <= 0) || value % 4 != 0)
                result.Add("theme.metrics.four-point", field,
                    "The metric must be a non-negative multiple of the four-point rhythm.");
        }
    }

    [Serializable]
    public struct RuntimeUiFeedbackTokens
    {
        [SerializeField, Range(0f, 1f)] private float normalOpacity;
        [SerializeField, Range(0f, 1f)] private float focusedOpacity;
        [SerializeField, Range(0f, 1f)] private float pressedOpacity;
        [SerializeField, Range(0f, 1f)] private float selectedOpacity;
        [SerializeField, Range(0f, 1f)] private float disabledOpacity;
        [SerializeField, Range(0f, 1f)] private float loadingOpacity;
        [SerializeField, Range(0f, 1f)] private float scrimOpacity;
        [SerializeField, Min(0f)] private float unscaledFocusSeconds;
        [SerializeField, Min(0f)] private float unscaledPressSeconds;
        [SerializeField, Min(0f)] private float unscaledSelectionSeconds;
        [SerializeField, Min(0f)] private float unscaledTransitionSeconds;
        [SerializeField, Min(0f)] private float unscaledStatusSeconds;
        [SerializeField, Range(.8f, 1f)] private float pressScale;
        [SerializeField, Range(1f, 1.5f)] private float popScale;
        [SerializeField, Range(1f, 1.5f)] private float strongPopScale;
        [SerializeField, Range(0f, 64f)] private float revealOffset;
        [SerializeField, Min(0f)] private float unscaledRevealSeconds;
        [SerializeField, Min(0f)] private float unscaledStaggerSeconds;
        [SerializeField, Range(0f, 44f)] private float dragCancelDistance;
        [SerializeField] private bool reducedMotion;

        public float NormalOpacity => normalOpacity;
        public float FocusedOpacity => focusedOpacity;
        public float PressedOpacity => pressedOpacity;
        public float SelectedOpacity => selectedOpacity;
        public float DisabledOpacity => disabledOpacity;
        public float LoadingOpacity => loadingOpacity;
        public float ScrimOpacity => scrimOpacity;
        public float UnscaledFocusSeconds => unscaledFocusSeconds;
        public float UnscaledPressSeconds => unscaledPressSeconds;
        public float UnscaledSelectionSeconds => unscaledSelectionSeconds;
        public float UnscaledTransitionSeconds => unscaledTransitionSeconds;
        public float UnscaledStatusSeconds => unscaledStatusSeconds;
        public float PressScale => pressScale;
        public float PopScale => popScale;
        public float StrongPopScale => strongPopScale;
        public float RevealOffset => revealOffset;
        public float UnscaledRevealSeconds => unscaledRevealSeconds;
        public float UnscaledStaggerSeconds => unscaledStaggerSeconds;
        public float DragCancelDistance => dragCancelDistance;
        public bool ReducedMotion => reducedMotion;

        public static RuntimeUiFeedbackTokens SunnyOrchardDefault()
        {
            return new RuntimeUiFeedbackTokens
            {
                normalOpacity = 1f,
                focusedOpacity = 1f,
                pressedOpacity = .94f,
                selectedOpacity = 1f,
                disabledOpacity = .58f,
                loadingOpacity = .72f,
                scrimOpacity = .68f,
                unscaledFocusSeconds = .08f,
                unscaledPressSeconds = .08f,
                unscaledSelectionSeconds = .12f,
                unscaledTransitionSeconds = .18f,
                unscaledStatusSeconds = 2.6f,
                pressScale = .96f,
                popScale = 1.12f,
                strongPopScale = 1.16f,
                revealOffset = 12f,
                unscaledRevealSeconds = .34f,
                unscaledStaggerSeconds = .055f,
                dragCancelDistance = 10f,
                reducedMotion = false,
            };
        }

        internal void AppendValidation(RuntimeUiValidationResult result, string field)
        {
            RuntimeUiNumbers.ValidateOpacity(result, field + ".normalOpacity", normalOpacity);
            RuntimeUiNumbers.ValidateOpacity(result, field + ".focusedOpacity", focusedOpacity);
            RuntimeUiNumbers.ValidateOpacity(result, field + ".pressedOpacity", pressedOpacity);
            RuntimeUiNumbers.ValidateOpacity(result, field + ".selectedOpacity", selectedOpacity);
            RuntimeUiNumbers.ValidateOpacity(result, field + ".disabledOpacity", disabledOpacity);
            RuntimeUiNumbers.ValidateOpacity(result, field + ".loadingOpacity", loadingOpacity);
            RuntimeUiNumbers.ValidateOpacity(result, field + ".scrimOpacity", scrimOpacity);
            RuntimeUiNumbers.ValidateDuration(result, field + ".unscaledFocusSeconds", unscaledFocusSeconds);
            RuntimeUiNumbers.ValidateDuration(result, field + ".unscaledPressSeconds", unscaledPressSeconds);
            RuntimeUiNumbers.ValidateDuration(result, field + ".unscaledSelectionSeconds", unscaledSelectionSeconds);
            RuntimeUiNumbers.ValidateDuration(result, field + ".unscaledTransitionSeconds", unscaledTransitionSeconds);
            RuntimeUiNumbers.ValidateDuration(result, field + ".unscaledStatusSeconds", unscaledStatusSeconds);
            ValidateScale(result, field + ".pressScale", pressScale, .8f, 1f);
            ValidateScale(result, field + ".popScale", popScale, 1f, 1.5f);
            ValidateScale(result, field + ".strongPopScale", strongPopScale,
                popScale, 1.5f);
            ValidateRange(result, field + ".revealOffset", revealOffset, 0f, 64f);
            RuntimeUiNumbers.ValidateDuration(result,
                field + ".unscaledRevealSeconds", unscaledRevealSeconds);
            RuntimeUiNumbers.ValidateDuration(result,
                field + ".unscaledStaggerSeconds", unscaledStaggerSeconds);
            ValidateRange(result, field + ".dragCancelDistance",
                dragCancelDistance, 0f, 44f);
        }

        private static void ValidateScale(RuntimeUiValidationResult result,
            string field, float value, float minimum, float maximum)
        {
            ValidateRange(result, field, value, minimum, maximum);
        }

        private static void ValidateRange(RuntimeUiValidationResult result,
            string field, float value, float minimum, float maximum)
        {
            if (!RuntimeUiNumbers.IsFinite(value) || value < minimum || value > maximum)
            {
                result.Add("theme.feedback.range", field,
                    "Motion value must be finite and inside its authored range.");
            }
        }
    }

    public readonly struct RuntimeUiFeedbackPulse
    {
        private readonly float startedAt;
        private readonly float deadline;

        private RuntimeUiFeedbackPulse(float startedAt, float deadline)
        {
            this.startedAt = startedAt;
            this.deadline = deadline;
        }

        public bool IsScheduled => deadline > startedAt;
        public float StartedAt => startedAt;
        public float Deadline => deadline;
        public float Duration => IsScheduled ? deadline - startedAt : 0f;

        public static RuntimeUiFeedbackPulse Begin(float unscaledTime, float duration)
        {
            if (!RuntimeUiNumbers.IsFinite(unscaledTime))
                throw new ArgumentOutOfRangeException(nameof(unscaledTime), unscaledTime,
                    "Feedback time must be finite.");
            if (!RuntimeUiNumbers.IsFinite(duration) || duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), duration,
                    "Feedback duration must be finite and non-negative.");
            if (duration == 0f)
                return default;

            var resolvedDeadline = unscaledTime + duration;
            if (!RuntimeUiNumbers.IsFinite(resolvedDeadline))
                throw new ArgumentOutOfRangeException(nameof(duration), duration,
                    "Feedback deadline must remain finite.");
            return new RuntimeUiFeedbackPulse(unscaledTime, resolvedDeadline);
        }

        public bool IsActive(float unscaledTime)
        {
            return IsScheduled
                && RuntimeUiNumbers.IsFinite(unscaledTime)
                && unscaledTime >= startedAt
                && unscaledTime < deadline;
        }

        public float Progress(float unscaledTime)
        {
            if (!IsScheduled || !RuntimeUiNumbers.IsFinite(unscaledTime)) return 1f;
            if (unscaledTime <= startedAt) return 0f;
            if (unscaledTime >= deadline) return 1f;
            return Mathf.Clamp01((unscaledTime - startedAt) / Duration);
        }
    }

    public sealed class RuntimeUiValidationIssue
    {
        public RuntimeUiValidationIssue(string code, string field, string message)
        {
            Code = code ?? string.Empty;
            Field = field ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string Code { get; }
        public string Field { get; }
        public string Message { get; }

        public override string ToString()
        {
            return Code + " [" + Field + "]: " + Message;
        }
    }

    public sealed class RuntimeUiValidationResult
    {
        private readonly List<RuntimeUiValidationIssue> issues =
            new List<RuntimeUiValidationIssue>();
        private readonly ReadOnlyCollection<RuntimeUiValidationIssue> readOnlyIssues;

        public RuntimeUiValidationResult()
        {
            readOnlyIssues = issues.AsReadOnly();
        }

        public bool IsValid => issues.Count == 0;
        public IReadOnlyList<RuntimeUiValidationIssue> Issues => readOnlyIssues;

        internal void Add(string code, string field, string message)
        {
            issues.Add(new RuntimeUiValidationIssue(code, field, message));
        }

        internal void Append(RuntimeUiValidationResult other, string fieldPrefix)
        {
            if (other == null) return;
            for (var index = 0; index < other.Issues.Count; index++)
            {
                var issue = other.Issues[index];
                var field = string.IsNullOrEmpty(fieldPrefix)
                    ? issue.Field
                    : string.IsNullOrEmpty(issue.Field)
                        ? fieldPrefix
                        : fieldPrefix + "." + issue.Field;
                Add(issue.Code, field, issue.Message);
            }
        }

        public string FirstIssueOr(string validValue)
        {
            return IsValid ? validValue : issues[0].ToString();
        }
    }

    internal static class RuntimeUiIdentity
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);
        private static readonly Regex StableRevisionPattern = new Regex(
            "^[a-z0-9]+(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);

        internal static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && StableIdPattern.IsMatch(value);
        }

        internal static bool IsValidRevision(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && StableRevisionPattern.IsMatch(value);
        }
    }

    internal static class RuntimeUiNumbers
    {
        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static void ValidateColor(RuntimeUiValidationResult result, string field, Color value)
        {
            if (!IsFinite(value.r) || !IsFinite(value.g) || !IsFinite(value.b) || !IsFinite(value.a)
                || value.r < 0f || value.r > 1f || value.g < 0f || value.g > 1f
                || value.b < 0f || value.b > 1f || value.a < 0f || value.a > 1f)
            {
                result.Add("theme.color.range", field,
                    "Semantic color channels must be finite values in the 0..1 range.");
            }
        }

        internal static void ValidateOpacity(RuntimeUiValidationResult result, string field, float value)
        {
            if (!IsFinite(value) || value < 0f || value > 1f)
                result.Add("theme.opacity.range", field,
                    "Opacity must be a finite value in the 0..1 range.");
        }

        internal static void ValidateDuration(RuntimeUiValidationResult result, string field, float value)
        {
            if (!IsFinite(value) || value < 0f)
                result.Add("theme.motion.duration", field,
                    "Unscaled feedback duration must be a finite non-negative value.");
        }
    }
}
