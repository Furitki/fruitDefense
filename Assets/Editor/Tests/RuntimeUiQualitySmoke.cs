using System;
using System.Collections.Generic;
using System.IO;
using FruitDefense.App;
using FruitDefense.Core;
using FruitDefense.Presentation;
using FruitDefense.Shell;
using FruitDefense.UI;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class RuntimeUiQualitySmoke
    {
        private readonly struct TextGeometry
        {
            public TextGeometry(RuntimeUiDrawContext context, Rect componentRect,
                Rect firstLineRect, Rect secondLineRect, Rect iconRect,
                Rect groupRect, GUIStyle style, int maximumLineCount,
                bool hasIcon, RuntimeUiStatusTextLayout statusLayout = default,
                bool hasStatusLayout = false)
            {
                Context = context;
                ComponentRect = componentRect;
                FirstLineRect = firstLineRect;
                SecondLineRect = secondLineRect;
                IconRect = iconRect;
                GroupRect = groupRect;
                Style = style;
                MaximumLineCount = maximumLineCount;
                HasIcon = hasIcon;
                StatusLayout = statusLayout;
                HasStatusLayout = hasStatusLayout;
            }

            public RuntimeUiDrawContext Context { get; }
            public Rect ComponentRect { get; }
            public Rect FirstLineRect { get; }
            public Rect SecondLineRect { get; }
            public Rect IconRect { get; }
            public Rect GroupRect { get; }
            public GUIStyle Style { get; }
            public int MaximumLineCount { get; }
            public bool HasIcon { get; }
            public RuntimeUiStatusTextLayout StatusLayout { get; }
            public bool HasStatusLayout { get; }
        }

        private sealed class LayoutBundle
        {
            public LayoutBundle(RuntimeUiTheme theme,
                RuntimeUiQualityViewportCase viewport, Rect safeArea)
            {
                Viewport = viewport;
                SafeArea = safeArea;
                Bootstrap = AppFlowCoordinator.CreateBootstrapPresentationLayout(
                    viewport.Width, viewport.Height, safeArea, true);
                Lobby = PortraitShellLayout.CreateLobby(
                    viewport.Width, viewport.Height, safeArea);
                Settlement = PortraitShellLayout.CreateSettlement(
                    viewport.Width, viewport.Height, safeArea);
                Battle = new BattleUiLayout(GameConfig.DefaultBattlefield);
                BootstrapContext = RuntimeUiDrawContext.Create(theme, Bootstrap.Scale);
                LobbyContext = RuntimeUiDrawContext.Create(theme, Lobby.Frame.Scale);
                SettlementContext = RuntimeUiDrawContext.Create(
                    theme, Settlement.Frame.Scale);
                BattleContext = RuntimeUiDrawContext.Create(theme, 1f);
            }

            public RuntimeUiQualityViewportCase Viewport { get; }
            public Rect SafeArea { get; }
            public AppFlowCoordinator.BootstrapPresentationLayout Bootstrap { get; }
            public LobbyShellLayout Lobby { get; }
            public SettlementShellLayout Settlement { get; }
            public BattleUiLayout Battle { get; }
            public RuntimeUiDrawContext BootstrapContext { get; }
            public RuntimeUiDrawContext LobbyContext { get; }
            public RuntimeUiDrawContext SettlementContext { get; }
            public RuntimeUiDrawContext BattleContext { get; }
        }

        public static void Run()
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            ValidateProfile(theme);
            ValidateCatalogCoverage();
            ValidateTextAndGeometryMatrix(theme);
            ValidateEffectiveContrast(theme);
            ValidateSourceAuthorities();
            Debug.Log("RUNTIME_UI_QUALITY_OK cases="
                + RuntimeUiTextInspectionCatalog.Cases.Count
                + " viewports=" + RuntimeUiQualityProfile.Viewports.Count);
        }

        private static void ValidateProfile(RuntimeUiTheme theme)
        {
            Assert(theme != null && theme.Validate().IsValid,
                "release theme is valid before quality inspection");
            Assert(theme.PackagedChineseFont != null
                && AssetDatabase.GetAssetPath(theme.PackagedChineseFont)
                    == "Assets/Resources/Fonts/NotoSansSC-UI.ttf",
                "quality inspection uses the packaged release Noto Sans SC font");
            Assert(RuntimeUiQualityProfile.Viewports.Count
                    == BattlefieldProjection.RequiredPortraitViewports.Count,
                "quality profile and runtime projection cover the same finite viewports");
            for (var index = 0; index < RuntimeUiQualityProfile.Viewports.Count; index++)
            {
                var profileCase = RuntimeUiQualityProfile.Viewports[index];
                Assert(profileCase.Viewport
                        == BattlefieldProjection.RequiredPortraitViewports[index],
                    "quality viewport order matches runtime projection: " + profileCase.Id);
            }

            foreach (RuntimeUiTypographyRole role in Enum.GetValues(
                         typeof(RuntimeUiTypographyRole)))
            {
                var typography = theme.Typography.For(role);
                Assert(typography.FontSize == RuntimeUiQualityProfile.MinimumFontSize(role)
                    && typography.FontSize >= RuntimeUiQualityProfile.MinimumNormalTextSize
                    && typography.LineHeight == RuntimeUiQualityProfile.LineHeight(role)
                    && typography.FontStyle == RuntimeUiQualityProfile.FontStyle(role),
                    role + " matches the finite typography role profile");
            }

            Assert(theme.Metrics.TouchTargetMinimum
                    >= RuntimeUiQualityProfile.MinimumTouchTarget,
                "theme touch target meets the 44-point profile");
            var spacings = new[]
            {
                theme.Metrics.SpacingXs,
                theme.Metrics.SpacingSm,
                theme.Metrics.SpacingMd,
                theme.Metrics.SpacingLg,
                theme.Metrics.SpacingXl,
                theme.Metrics.SpacingXxl,
                theme.Metrics.SurfaceInset,
                theme.Metrics.ComponentGap,
            };
            for (var index = 0; index < spacings.Length; index++)
            {
                Assert(spacings[index] >= 0
                    && spacings[index] % RuntimeUiQualityProfile.SpacingGrid == 0,
                    "theme spacing value follows the four-point grid: " + spacings[index]);
            }
            Assert(theme.Metrics.ComponentGap
                    >= RuntimeUiQualityProfile.MinimumContentGap,
                "theme component gap meets the eight-point quality floor");
            Assert(theme.Metrics.SurfaceInset
                    >= RuntimeUiQualityProfile.MinimumContentInset,
                "theme surface inset meets the eight-point quality floor");
        }

        private static void ValidateCatalogCoverage()
        {
            var enumValues = (RuntimeUiCopyId[])Enum.GetValues(typeof(RuntimeUiCopyId));
            Assert(enumValues.Length == RuntimeUiCopyCatalog.Count,
                "copy catalog count matches its explicit finite enum");
            for (var index = 0; index < enumValues.Length; index++)
            {
                Assert((int)enumValues[index] == index,
                    "copy IDs remain contiguous and explicitly stable at " + index);
                var copy = RuntimeUiCopyCatalog.Get(enumValues[index]);
                Assert(copy.Id == enumValues[index]
                    && !string.IsNullOrWhiteSpace(copy.Text)
                    && copy.MaximumLineCount >= 1
                    && copy.MaximumLineCount <= 2,
                    enumValues[index] + " has finite copy and line policy");
            }

            var inspected = new HashSet<RuntimeUiCopyId>();
            var caseIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var inspection in RuntimeUiTextInspectionCatalog.Cases)
            {
                Assert(caseIds.Add(inspection.Id),
                    "text inspection IDs are unique: " + inspection.Id);
                inspected.Add(inspection.CopyId);
            }

            foreach (var copyId in enumValues)
            {
                Assert(inspected.Contains(copyId),
                    "every rendered stable product copy has an inspection case: " + copyId);
            }
            var localizedStart = RuntimeUiCopyCatalog.FormatLobbyStart("orchard-03");
            Assert(localizedStart.IndexOf("第三关", StringComparison.Ordinal) >= 0
                && localizedStart.IndexOf("orchard-", StringComparison.Ordinal) < 0,
                "Lobby CTA presents the selected level name instead of an internal ID");
            Assert(RuntimeUiCopyCatalog.LevelDisplayName("orchard-01") == "第一关"
                && RuntimeUiCopyCatalog.LevelDisplayName("orchard-02") == "第二关"
                && RuntimeUiCopyCatalog.LevelDisplayName("orchard-03") == "第三关",
                "finite release level IDs map to localized player-facing names");
        }

        private static void ValidateTextAndGeometryMatrix(RuntimeUiTheme theme)
        {
            foreach (var viewport in RuntimeUiQualityProfile.Viewports)
            {
                ValidateLayoutCase(theme, viewport, viewport.FullSafeArea, "full");
                ValidateLayoutCase(theme, viewport, viewport.InsetSafeArea, "inset");
            }
        }

        private static void ValidateLayoutCase(RuntimeUiTheme theme,
            RuntimeUiQualityViewportCase viewport, Rect safeArea, string safeAreaKind)
        {
            var bundle = new LayoutBundle(theme, viewport, safeArea);
            var suffix = viewport.Id + "/" + safeAreaKind;
            foreach (var inspection in RuntimeUiTextInspectionCatalog.Cases)
                ValidateTextCase(bundle, inspection, suffix);
            ValidateRouteGeometry(bundle, suffix);
            ValidateRepeatedBaselines(bundle, suffix);
        }

        private static void ValidateTextCase(LayoutBundle bundle,
            RuntimeUiTextInspectionCase inspection, string suffix)
        {
            var copy = inspection.Copy;
            var geometry = ResolveTextGeometry(bundle, inspection);
            var caseName = inspection.Id + "@" + suffix;
            Assert(geometry.Style != null
                && ReferenceEquals(geometry.Style.font,
                    geometry.Context.Theme.PackagedChineseFont),
                caseName + " uses the packaged Noto style");
            Assert(geometry.Style.fontSize == Mathf.Max(1, Mathf.RoundToInt(
                       geometry.Context.Theme.Typography.For(copy.Role).FontSize
                       * geometry.Context.Scale)),
                caseName + " uses its semantic typography role");
            Assert(geometry.Style.alignment == copy.Alignment,
                caseName + " uses its catalog alignment");
            Assert(Contains(geometry.ComponentRect, geometry.FirstLineRect)
                && (!geometry.HasIcon
                    || Contains(geometry.ComponentRect, geometry.IconRect)),
                caseName + " text/icon remain inside the owning component");

            var expectedLineCount = copy.LinePolicy
                == RuntimeUiCopyLinePolicy.ControlledTwoLines ? 2 : 1;
            Assert(geometry.MaximumLineCount == expectedLineCount
                && !geometry.Style.wordWrap
                && geometry.Style.clipping == TextClipping.Clip,
                caseName + " has an explicit no-wrap finite line policy");

            if (expectedLineCount == 1)
            {
                AssertSingleLineFits(geometry.Style, copy.Text,
                    geometry.FirstLineRect, caseName);
            }
            else
            {
                Assert(geometry.HasStatusLayout,
                    caseName + " controlled copy uses shared status anatomy");
                var lines = RuntimeUiGui.ResolveStatusTextLines(
                    geometry.StatusLayout, copy.Text);
                Assert(lines.HasSecondLine
                    && lines.FirstLine + lines.SecondLine == copy.Text,
                    caseName + " resolves to exactly two complete controlled lines");
                AssertSingleLineFits(geometry.Style, lines.FirstLine,
                    geometry.FirstLineRect, caseName + "/line-1");
                AssertSingleLineFits(geometry.Style, lines.SecondLine,
                    geometry.SecondLineRect, caseName + "/line-2");
            }

            var expectedLineHeight = Mathf.Round(
                geometry.Context.Theme.Typography.For(copy.Role).LineHeight
                * geometry.Context.Scale);
            Assert(geometry.FirstLineRect.height + RuntimeUiQualityProfile.GeometryTolerance
                    >= Mathf.Min(expectedLineHeight,
                        geometry.Style.CalcSize(new GUIContent(copy.Text)).y),
                caseName + " provides a valid semantic line box");

            if (expectedLineCount == 1 && IsMiddleAnchor(copy.Alignment))
            {
                var textOwner = RuntimeUiGui.ResolveTextContentRect(
                    geometry.Context, geometry.ComponentRect, inspection.State);
                Assert(Mathf.Abs(geometry.FirstLineRect.center.y
                        - textOwner.center.y)
                        <= RuntimeUiQualityProfile.GeometryTolerance,
                    caseName + " centers its finite single-line box inside the owner");
            }

            if (geometry.HasIcon && geometry.FirstLineRect.width > 0f)
            {
                var gap = geometry.FirstLineRect.xMin - geometry.IconRect.xMax;
                Assert(gap + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumIconTextGap
                        * geometry.Context.Scale
                    && gap <= RuntimeUiQualityProfile.MaximumIconTextGap
                        * geometry.Context.Scale
                        + RuntimeUiQualityProfile.GeometryTolerance,
                    caseName + " icon/copy gap meets the quality profile");
            }

            if (IsVisualGroupTarget(inspection.Target))
            {
                Assert(Mathf.Abs(geometry.GroupRect.center.x
                        - geometry.ComponentRect.center.x)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                            * geometry.Context.Scale
                            + RuntimeUiQualityProfile.GeometryTolerance
                    && Mathf.Abs(geometry.GroupRect.center.y
                        - geometry.ComponentRect.center.y)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                            * geometry.Context.Scale
                            + RuntimeUiQualityProfile.GeometryTolerance,
                    caseName + " centers icon and label as one visual group");
            }

            if (IsActionTarget(inspection.Target))
            {
                var borderGap = RuntimeUiQualityProfile.MinimumTextToBorderGap
                    * geometry.Context.Scale;
                Assert(geometry.IconRect.xMin >= geometry.ComponentRect.xMin + borderGap
                    && geometry.IconRect.yMin >= geometry.ComponentRect.yMin + borderGap
                    && geometry.IconRect.xMax <= geometry.ComponentRect.xMax - borderGap
                    && geometry.IconRect.yMax <= geometry.ComponentRect.yMax - borderGap
                    && geometry.FirstLineRect.xMin
                        >= geometry.ComponentRect.xMin + borderGap
                    && geometry.FirstLineRect.xMax
                        <= geometry.ComponentRect.xMax - borderGap,
                    caseName + " visible icon and glyph boxes clear the action stroke");
            }
        }

        private static TextGeometry ResolveTextGeometry(LayoutBundle bundle,
            RuntimeUiTextInspectionCase inspection)
        {
            var copy = inspection.Copy;
            var component = ResolveComponentRect(bundle, inspection);
            var context = ResolveContext(bundle, inspection.Target);

            if (inspection.Target == RuntimeUiTextInspectionTarget.BattleModalMessage)
            {
                var inline = RuntimeUiGui.ResolveInlineContentLayout(
                    context, component, RuntimeUiArtSlot.IndicatorWarning,
                    copy.Text, copy.Role, inspection.State);
                return new TextGeometry(context, component, inline.LabelRect,
                    default, inline.IconVisualRect, inline.GroupRect,
                    context.Styles.SingleLineText(copy.Role, TextAnchor.MiddleCenter),
                    1, true);
            }

            if (IsActionTarget(inspection.Target))
            {
                var action = RuntimeUiGui.ResolveActionContentLayout(
                    context, component, copy.Text, inspection.ActionKind,
                    inspection.State, inspection.IconSlot, copy.Role);
                return new TextGeometry(context, component, action.LabelRect,
                    default, action.IconVisualRect, action.GroupRect,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, action.HasIcon);
            }

            if (IsStatusTarget(inspection.Target))
            {
                var mode = RuntimeUiCopyCatalog.StatusTextMode(copy);
                var status = RuntimeUiGui.ResolveStatusTextLayout(context,
                    component, inspection.State, copy.Role, mode);
                return new TextGeometry(context, component, status.FirstLineRect,
                    status.SecondLineRect, status.IndicatorRect, default,
                    status.Style, status.MaximumLineCount, status.HasIndicator,
                    status, true);
            }

            if (IsMetricTarget(inspection.Target))
            {
                var compactIconSize = IsBattleHeaderMetricTarget(inspection.Target)
                    ? BattleUiLayout.HeaderMetricIconSize : 24f;
                var metric = RuntimeUiGui.ResolveCompactInlineMetricContentLayout(
                    context, component, MetricIcon(inspection.Target), copy.Text,
                    MetricValue(inspection.Target), inspection.State,
                    compactIconSize);
                return new TextGeometry(context, component, metric.LabelRect, default,
                    metric.IconVisualRect, metric.GroupRect,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, true);
            }

            if (copy.LinePolicy == RuntimeUiCopyLinePolicy.ControlledTwoLines)
            {
                var twoLine = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                    context, component, copy.Role, copy.Alignment, inspection.State);
                return new TextGeometry(context, component, twoLine.FirstLineRect,
                    twoLine.SecondLineRect, default, default, twoLine.Style, 2,
                    false, twoLine, true);
            }

            var textRect = RuntimeUiGui.ResolveSingleLineTextRect(
                context, component, copy.Role, copy.Alignment, inspection.State);
            return new TextGeometry(context, component, textRect, default,
                default, default,
                context.Styles.SingleLineText(copy.Role, copy.Alignment),
                1, false);
        }

        private static RuntimeUiDrawContext ResolveContext(LayoutBundle bundle,
            RuntimeUiTextInspectionTarget target)
        {
            if (target <= RuntimeUiTextInspectionTarget.BootstrapRecoverableStatus)
                return bundle.BootstrapContext;
            if (target <= RuntimeUiTextInspectionTarget.LobbyStatus)
                return bundle.LobbyContext;
            if (target <= RuntimeUiTextInspectionTarget.BattleModalTerminalAction)
                return bundle.BattleContext;
            return bundle.SettlementContext;
        }

        private static Rect ResolveComponentRect(LayoutBundle bundle,
            RuntimeUiTextInspectionCase inspection)
        {
            var target = inspection.Target;
            switch (target)
            {
                case RuntimeUiTextInspectionTarget.BootstrapTitle:
                    return bundle.Bootstrap.Title;
                case RuntimeUiTextInspectionTarget.BootstrapStatus:
                    return bundle.Bootstrap.Status;
                case RuntimeUiTextInspectionTarget.BootstrapRetry:
                    return bundle.Bootstrap.RetryAction;
                case RuntimeUiTextInspectionTarget.BootstrapRecoverableStatus:
                    return bundle.Bootstrap.RecoverableStatus;
                case RuntimeUiTextInspectionTarget.LobbyTitle:
                    return bundle.Lobby.Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard01Title:
                    return PortraitShellLayout.CreateLobbyLevelCard(
                        bundle.Lobby.Orchard01Card, bundle.Lobby.Frame.Scale).Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard01Body:
                    return PortraitShellLayout.CreateLobbyLevelCard(
                        bundle.Lobby.Orchard01Card, bundle.Lobby.Frame.Scale).Body;
                case RuntimeUiTextInspectionTarget.LobbyOrchard02Title:
                    return PortraitShellLayout.CreateLobbyLevelCard(
                        bundle.Lobby.Orchard02Card, bundle.Lobby.Frame.Scale).Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard02Body:
                    return PortraitShellLayout.CreateLobbyLevelCard(
                        bundle.Lobby.Orchard02Card, bundle.Lobby.Frame.Scale).Body;
                case RuntimeUiTextInspectionTarget.LobbyOrchard03Title:
                    return PortraitShellLayout.CreateLobbyLevelCard(
                        bundle.Lobby.Orchard03Card, bundle.Lobby.Frame.Scale).Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard03Body:
                    return PortraitShellLayout.CreateLobbyLevelCard(
                        bundle.Lobby.Orchard03Card, bundle.Lobby.Frame.Scale).Body;
                case RuntimeUiTextInspectionTarget.LobbyStart:
                    return bundle.Lobby.StartButton;
                case RuntimeUiTextInspectionTarget.LobbyStatus:
                    return bundle.Lobby.Status;
                case RuntimeUiTextInspectionTarget.BattleHeaderTitle:
                    return bundle.Battle.HeaderTitle;
                case RuntimeUiTextInspectionTarget.BattleSunMetric:
                    return bundle.Battle.SunMetric;
                case RuntimeUiTextInspectionTarget.BattleCoreMetric:
                    return bundle.Battle.LivesMetric;
                case RuntimeUiTextInspectionTarget.BattleWaveMetric:
                    return bundle.Battle.WaveMetric;
                case RuntimeUiTextInspectionTarget.BattleBoardStatus:
                    return bundle.Battle.BoardStatusWithWaveAction;
                case RuntimeUiTextInspectionTarget.BattleBoardStatusFull:
                    return bundle.Battle.BoardStatus;
                case RuntimeUiTextInspectionTarget.BattleWaveAction:
                    return bundle.Battle.WaveAction;
                case RuntimeUiTextInspectionTarget.BattleToolTrayTitle:
                    return bundle.Battle.ToolTrayTitle;
                case RuntimeUiTextInspectionTarget.BattleNurseryTrayTitle:
                    return bundle.Battle.NurseryTrayTitle;
                case RuntimeUiTextInspectionTarget.BattleNurserySlot:
                    return inspection.CopyId == RuntimeUiCopyId.BattleNurseryPotStored
                        ? BattleUiLayout.NurserySlotLabel(bundle.Battle.NurserySlot(0))
                        : bundle.Battle.NurserySlot(0);
                case RuntimeUiTextInspectionTarget.BattleRefreshAction:
                    return bundle.Battle.RefreshAction;
                case RuntimeUiTextInspectionTarget.BattleModalTitle:
                    return inspection.CopyId == RuntimeUiCopyId.BattleVictoryTitle
                        || inspection.CopyId == RuntimeUiCopyId.BattleDefeatTitle
                        ? bundle.Battle.ModalTerminalTitle
                        : bundle.Battle.ModalTitle;
                case RuntimeUiTextInspectionTarget.BattleModalMessage:
                    return bundle.Battle.ModalPauseHint;
                case RuntimeUiTextInspectionTarget.BattleModalResultBanner:
                    return bundle.Battle.ModalResultBannerText;
                case RuntimeUiTextInspectionTarget.BattleModalTerminalMessage:
                    return bundle.Battle.ModalTerminalMessage;
                case RuntimeUiTextInspectionTarget.BattleModalPrimaryAction:
                    return bundle.Battle.ModalAction(0, 2);
                case RuntimeUiTextInspectionTarget.BattleModalSecondaryAction:
                    return bundle.Battle.ModalAction(1, 2);
                case RuntimeUiTextInspectionTarget.BattleModalTerminalAction:
                    return bundle.Battle.ModalAction(0, 1);
                case RuntimeUiTextInspectionTarget.SettlementTitle:
                    return bundle.Settlement.Title;
                case RuntimeUiTextInspectionTarget.SettlementOutcome:
                    return bundle.Settlement.Outcome;
                case RuntimeUiTextInspectionTarget.SettlementCompletedLevel:
                    return bundle.Settlement.CompletedLevel;
                case RuntimeUiTextInspectionTarget.SettlementReachedWave:
                    return bundle.Settlement.ReachedWave;
                case RuntimeUiTextInspectionTarget.SettlementRemainingLives:
                    return bundle.Settlement.RemainingLives;
                case RuntimeUiTextInspectionTarget.SettlementRetry:
                    return bundle.Settlement.RetryButton;
                case RuntimeUiTextInspectionTarget.SettlementReturn:
                    return bundle.Settlement.ReturnButton;
                case RuntimeUiTextInspectionTarget.SettlementStatus:
                    return bundle.Settlement.Status;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        private static bool IsActionTarget(RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.BootstrapRetry
                || target == RuntimeUiTextInspectionTarget.LobbyStart
                || target == RuntimeUiTextInspectionTarget.BattleWaveAction
                || target == RuntimeUiTextInspectionTarget.BattleRefreshAction
                || target == RuntimeUiTextInspectionTarget.BattleModalPrimaryAction
                || target == RuntimeUiTextInspectionTarget.BattleModalSecondaryAction
                || target == RuntimeUiTextInspectionTarget.BattleModalTerminalAction
                || target == RuntimeUiTextInspectionTarget.SettlementRetry
                || target == RuntimeUiTextInspectionTarget.SettlementReturn;
        }

        private static bool IsVisualGroupTarget(RuntimeUiTextInspectionTarget target)
        {
            return IsActionTarget(target)
                || target == RuntimeUiTextInspectionTarget.BattleModalMessage;
        }

        private static bool IsMiddleAnchor(TextAnchor anchor)
        {
            return anchor == TextAnchor.MiddleLeft
                || anchor == TextAnchor.MiddleCenter
                || anchor == TextAnchor.MiddleRight;
        }

        private static bool IsStatusTarget(RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.BootstrapStatus
                || target == RuntimeUiTextInspectionTarget.BootstrapRecoverableStatus
                || target == RuntimeUiTextInspectionTarget.LobbyStatus
                || target == RuntimeUiTextInspectionTarget.BattleBoardStatus
                || target == RuntimeUiTextInspectionTarget.BattleBoardStatusFull
                || target == RuntimeUiTextInspectionTarget.SettlementStatus;
        }

        private static bool IsMetricTarget(RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.BattleSunMetric
                || target == RuntimeUiTextInspectionTarget.BattleCoreMetric
                || target == RuntimeUiTextInspectionTarget.BattleWaveMetric
                || IsSettlementMetricTarget(target);
        }

        private static bool IsSettlementMetricTarget(
            RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.SettlementCompletedLevel
                || target == RuntimeUiTextInspectionTarget.SettlementReachedWave
                || target == RuntimeUiTextInspectionTarget.SettlementRemainingLives;
        }

        private static bool IsBattleHeaderMetricTarget(
            RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.BattleSunMetric
                || target == RuntimeUiTextInspectionTarget.BattleCoreMetric
                || target == RuntimeUiTextInspectionTarget.BattleWaveMetric;
        }

        private static RuntimeUiArtSlot MetricIcon(
            RuntimeUiTextInspectionTarget target)
        {
            switch (target)
            {
                case RuntimeUiTextInspectionTarget.BattleSunMetric:
                    return RuntimeUiArtSlot.IconResourceSun;
                case RuntimeUiTextInspectionTarget.BattleCoreMetric:
                    return RuntimeUiArtSlot.IconResourceCore;
                case RuntimeUiTextInspectionTarget.BattleWaveMetric:
                    return RuntimeUiArtSlot.IconResourceWave;
                case RuntimeUiTextInspectionTarget.SettlementCompletedLevel:
                    return RuntimeUiArtSlot.IconResourceSun;
                case RuntimeUiTextInspectionTarget.SettlementReachedWave:
                    return RuntimeUiArtSlot.IconResourceWave;
                case RuntimeUiTextInspectionTarget.SettlementRemainingLives:
                    return RuntimeUiArtSlot.IconResourceCore;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        private static string MetricValue(RuntimeUiTextInspectionTarget target)
        {
            switch (target)
            {
                case RuntimeUiTextInspectionTarget.BattleSunMetric: return "999";
                case RuntimeUiTextInspectionTarget.BattleCoreMetric: return "99";
                case RuntimeUiTextInspectionTarget.BattleWaveMetric: return "15";
                case RuntimeUiTextInspectionTarget.SettlementCompletedLevel:
                    return RuntimeUiCopyCatalog.LevelDisplayName("orchard-03");
                case RuntimeUiTextInspectionTarget.SettlementReachedWave: return "15";
                case RuntimeUiTextInspectionTarget.SettlementRemainingLives: return "3";
                default: throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        private static void ValidateRouteGeometry(LayoutBundle bundle, string suffix)
        {
            var viewportRect = new Rect(0f, 0f,
                bundle.Viewport.Width, bundle.Viewport.Height);
            Assert(Contains(viewportRect, bundle.Bootstrap.SafeArea)
                && Contains(bundle.Bootstrap.SafeArea, bundle.Bootstrap.Modal)
                && Contains(bundle.Lobby.Frame.SafeArea, bundle.Lobby.Frame.Content)
                && Contains(bundle.Settlement.Frame.SafeArea,
                    bundle.Settlement.Frame.Content),
                suffix + " route chrome remains inside the resolved safe area");

            ValidateTouch(bundle.Bootstrap.RetryAction,
                bundle.BootstrapContext.Scale, suffix + "/bootstrap.retry");
            ValidateTouch(bundle.Lobby.StartButton,
                bundle.LobbyContext.Scale, suffix + "/lobby.start");
            ValidateTouch(bundle.Settlement.RetryButton,
                bundle.SettlementContext.Scale, suffix + "/settlement.retry");
            ValidateTouch(bundle.Settlement.ReturnButton,
                bundle.SettlementContext.Scale, suffix + "/settlement.return");
            ValidateTouch(bundle.Battle.WaveAction, 1f,
                suffix + "/battle.wave-action");
            ValidateTouch(bundle.Battle.RefreshAction, 1f,
                suffix + "/battle.refresh");

            var lobbyCards = new[]
            {
                bundle.Lobby.Orchard01Card,
                bundle.Lobby.Orchard02Card,
                bundle.Lobby.Orchard03Card,
            };
            var lobbyThumbnails = new[]
            {
                RuntimeUiArtSlot.IllustrationLobbyOrchard01,
                RuntimeUiArtSlot.IllustrationLobbyOrchard02,
                RuntimeUiArtSlot.IllustrationLobbyOrchard03,
            };
            for (var index = 0; index < lobbyCards.Length; index++)
            {
                var card = PortraitShellLayout.CreateLobbyLevelCard(
                    lobbyCards[index], bundle.Lobby.Frame.Scale);
                Assert(Contains(lobbyCards[index], card.Frame)
                    && Contains(lobbyCards[index], card.Title)
                    && Contains(lobbyCards[index], card.Body)
                    && Contains(lobbyCards[index], card.SelectedMarker)
                    && Contains(lobbyCards[index], card.TransientIndicator),
                    suffix + "/lobby.card-" + index + " anatomy is contained");
                Assert(card.Title.xMin - card.Frame.xMax
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumContentGap
                        * bundle.LobbyContext.Scale,
                    suffix + "/lobby.card-" + index + " illustration/copy gap");
                var visualGroupCenter = (card.Frame.xMin
                    + Mathf.Max(card.Title.xMax, card.Body.xMax)) * .5f;
                Assert(Mathf.Abs(visualGroupCenter - lobbyCards[index].center.x)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                            * bundle.LobbyContext.Scale
                            + RuntimeUiQualityProfile.GeometryTolerance,
                    suffix + "/lobby.card-" + index
                    + " illustration/copy group is optically centered");
                Assert(!Overlaps(card.Title, card.SelectedMarker)
                    && !Overlaps(card.Body, card.TransientIndicator),
                    suffix + "/lobby.card-" + index + " cues do not cover copy");
                ValidateTouch(lobbyCards[index], bundle.LobbyContext.Scale,
                    suffix + "/lobby.card-" + index);
                ValidateIllustrationOccupancy(card.Thumbnail, bundle.LobbyContext,
                    lobbyThumbnails[index], suffix + "/lobby.card-" + index
                    + ".thumbnail", true);
            }

            Assert(PortraitShellLayout.HitTest(bundle.Lobby,
                    bundle.Lobby.Orchard01Card.center, false)
                    == ShellHitTarget.LevelOrchard01
                && PortraitShellLayout.HitTest(bundle.Lobby,
                    bundle.Lobby.Orchard02Card.center, false)
                    == ShellHitTarget.LevelOrchard02
                && PortraitShellLayout.HitTest(bundle.Lobby,
                    bundle.Lobby.Orchard03Card.center, false)
                    == ShellHitTarget.LevelOrchard03
                && PortraitShellLayout.HitTest(bundle.Lobby,
                    bundle.Lobby.StartButton.center, false) == ShellHitTarget.Start,
                suffix + " Lobby draw and hit rects share one layout authority");
            Assert(PortraitShellLayout.HitTest(bundle.Settlement,
                    bundle.Settlement.RetryButton.center, false) == ShellHitTarget.Retry
                && PortraitShellLayout.HitTest(bundle.Settlement,
                    bundle.Settlement.ReturnButton.center, false) == ShellHitTarget.Return,
                suffix + " Settlement draw and hit rects share one layout authority");

            var minimumNineSliceDestinations = new[]
            {
                bundle.Bootstrap.Modal,
                bundle.Lobby.Orchard01Card,
                bundle.Lobby.Orchard02Card,
                bundle.Lobby.Orchard03Card,
                bundle.Lobby.StartButton,
                bundle.Settlement.ResultCard,
                bundle.Settlement.RetryButton,
                bundle.Settlement.ReturnButton,
                bundle.Battle.Header,
                bundle.Battle.ToolTray,
                bundle.Battle.NurseryTray,
                bundle.Battle.Detail,
                bundle.Battle.Modal,
                bundle.Battle.TerminalModal,
            };
            var minimumNineSliceScales = new[]
            {
                bundle.BootstrapContext.Scale,
                bundle.LobbyContext.Scale,
                bundle.LobbyContext.Scale,
                bundle.LobbyContext.Scale,
                bundle.LobbyContext.Scale,
                bundle.SettlementContext.Scale,
                bundle.SettlementContext.Scale,
                bundle.SettlementContext.Scale,
                1f, 1f, 1f, 1f, 1f, 1f,
            };
            for (var index = 0; index < minimumNineSliceDestinations.Length; index++)
            {
                Assert(Mathf.Min(minimumNineSliceDestinations[index].width,
                           minimumNineSliceDestinations[index].height)
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.NineSliceMinimumDestination
                            * minimumNineSliceScales[index],
                    suffix + "/nine-slice-destination-" + index
                    + " meets the finite 32-point destination floor");
            }
            var lobbyOccupiedCenter = (bundle.Lobby.Title.yMin
                + bundle.Lobby.StartButton.yMax) * .5f;
            var settlementOccupiedCenter = (bundle.Settlement.Title.yMin
                + bundle.Settlement.ReturnButton.yMax) * .5f;
            Assert(Mathf.Abs(lobbyOccupiedCenter
                        - bundle.Lobby.Frame.SafeArea.center.y)
                    <= RuntimeUiQualityProfile.OccupiedContentCenterTolerance
                        * Mathf.Max(1f, bundle.Lobby.Frame.Scale)
                        + RuntimeUiQualityProfile.GeometryTolerance
                && bundle.Lobby.Frame.SafeArea.yMax
                    - bundle.Lobby.StartButton.yMax
                    <= RuntimeUiQualityProfile.OccupiedContentBottomGapMaximum
                        * Mathf.Max(1f, bundle.Lobby.Frame.Scale)
                        + RuntimeUiQualityProfile.GeometryTolerance,
                suffix + " Lobby occupied content is vertically balanced with <=100 lower gap");
            Assert(Mathf.Abs(settlementOccupiedCenter
                        - bundle.Settlement.Frame.SafeArea.center.y)
                    <= RuntimeUiQualityProfile.OccupiedContentCenterTolerance
                        * Mathf.Max(1f, bundle.Settlement.Frame.Scale)
                        + RuntimeUiQualityProfile.GeometryTolerance
                && bundle.Settlement.Frame.SafeArea.yMax
                    - bundle.Settlement.ReturnButton.yMax
                    <= RuntimeUiQualityProfile.OccupiedContentBottomGapMaximum
                        * Mathf.Max(1f, bundle.Settlement.Frame.Scale)
                        + RuntimeUiQualityProfile.GeometryTolerance,
                suffix + " Settlement occupied content is vertically balanced with <=100 lower gap");

            Assert(bundle.Battle.ToolTrayTitle.yMin
                    >= bundle.Battle.ToolTray.yMin
                    + RuntimeUiQualityProfile.MinimumTextToBorderGap
                && bundle.Battle.Tool(0).yMin
                    - bundle.Battle.ToolTrayTitle.yMax
                    >= RuntimeUiQualityProfile.MinimumTextToBorderGap,
                suffix + " Battle tool title clears the panel top edge");
            Assert(bundle.Battle.NurseryTrayTitle.yMin
                    >= bundle.Battle.NurseryTray.yMin
                    + RuntimeUiQualityProfile.MinimumTextToBorderGap
                && bundle.Battle.NurserySlot(0).yMin
                    - bundle.Battle.NurseryTrayTitle.yMax
                    >= RuntimeUiQualityProfile.MinimumTextToBorderGap,
                suffix + " Battle nursery title clears the panel top edge");
            Assert(!Overlaps(bundle.Battle.ModalResultBanner,
                    bundle.Battle.ModalTerminalMessage)
                && !Overlaps(bundle.Battle.ModalResultBanner,
                    bundle.Battle.ModalResultIndicator)
                && !Overlaps(bundle.Battle.ModalTerminalMessage,
                    bundle.Battle.ModalResultIndicator)
                && bundle.Battle.ModalResultIndicator.xMin
                    - bundle.Battle.ModalTerminalMessage.xMax >= 6f,
                suffix + " Battle terminal banner, copy and indicator do not overlap");
            Assert(Contains(bundle.Battle.ModalResultBanner,
                    bundle.Battle.ModalResultBannerText),
                suffix + " Battle terminal outcome copy remains inside its semantic banner");
            var pauseHint = RuntimeUiGui.ResolveInlineContentLayout(
                bundle.BattleContext, bundle.Battle.ModalPauseHint,
                RuntimeUiArtSlot.IndicatorWarning,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattlePausedMessage).Text,
                RuntimeUiTypographyRole.Body, RuntimeUiInteractionState.Warning);
            Assert(Contains(bundle.Battle.ModalPauseHint, pauseHint.IconVisualRect)
                && Contains(bundle.Battle.ModalPauseHint, pauseHint.LabelRect)
                && Mathf.Abs(pauseHint.GroupRect.center.x
                    - bundle.Battle.ModalPauseHint.center.x)
                    <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                        * bundle.BattleContext.Scale
                && Mathf.Abs(pauseHint.GroupRect.center.y
                    - bundle.Battle.ModalPauseHint.center.y)
                    <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                        * bundle.BattleContext.Scale,
                suffix + " Battle pause centers warning and copy as one optical group");
            var victoryContent = BattleUiPresentationState.Create(
                GamePhase.Victory, false).ModalContent(15, 15);
            var terminalText = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                bundle.BattleContext, bundle.Battle.ModalTerminalMessage,
                RuntimeUiTypographyRole.Body, TextAnchor.MiddleCenter,
                RuntimeUiInteractionState.Success);
            Assert(victoryContent.MessageLines.HasSecondLine
                && victoryContent.MessageLines.FirstLine + " "
                    + victoryContent.MessageLines.SecondLine
                    == RuntimeUiCopyCatalog.FormatVictoryMessage(15)
                && Contains(bundle.Battle.ModalTerminalMessage,
                    terminalText.FirstLineRect)
                && Contains(bundle.Battle.ModalTerminalMessage,
                    terminalText.SecondLineRect),
                suffix + " Battle terminal message uses two complete controlled lines");
            AssertSingleLineFits(terminalText.Style,
                victoryContent.MessageLines.FirstLine, terminalText.FirstLineRect,
                suffix + "/battle.terminal-message.line-1");
            AssertSingleLineFits(terminalText.Style,
                victoryContent.MessageLines.SecondLine, terminalText.SecondLineRect,
                suffix + "/battle.terminal-message.line-2");
            Assert(bundle.Battle.Battlefield.ValidateControlInset(out var battleReason),
                suffix + " Battle chrome preserves battlefield interaction geometry: "
                + battleReason);
            var battlefield = bundle.Battle.Battlefield;
            var leftGutter = battlefield.GridRect.xMin
                - battlefield.MapViewportRect.xMin;
            var rightGutter = battlefield.MapViewportRect.xMax
                - battlefield.GridRect.xMax;
            var topGutter = battlefield.GridRect.yMin
                - battlefield.MapViewportRect.yMin;
            var bottomGutter = battlefield.MapViewportRect.yMax
                - battlefield.GridRect.yMax;
            Assert(Mathf.Abs(leftGutter - rightGutter)
                    <= RuntimeUiQualityProfile.OppositeGutterTolerance
                && Mathf.Abs(topGutter - bottomGutter)
                    <= RuntimeUiQualityProfile.OppositeGutterTolerance,
                suffix + " Battle grid visual gutters are symmetric inside MapViewportRect");

            ValidateIllustrationOccupancy(bundle.Settlement.OrchardVista,
                bundle.SettlementContext,
                RuntimeUiArtSlot.IllustrationOrchardVista,
                suffix + "/settlement.vista", false);
            ValidateBattleHeaderMetrics(bundle, suffix);
        }

        private static void ValidateBattleHeaderMetrics(LayoutBundle bundle, string suffix)
        {
            var rects = new[]
            {
                bundle.Battle.SunMetric,
                bundle.Battle.LivesMetric,
                bundle.Battle.WaveMetric,
            };
            var labels = new[]
            {
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleSun).Text,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleCore).Text,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.BattleWave).Text,
            };
            var values = new[] { "999", "99", "15" };
            var icons = new[]
            {
                RuntimeUiArtSlot.IconResourceSun,
                RuntimeUiArtSlot.IconResourceCore,
                RuntimeUiArtSlot.IconResourceWave,
            };
            var style = bundle.BattleContext.Styles.SingleLineText(
                RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
            var firstCenter = 0f;
            for (var index = 0; index < rects.Length; index++)
            {
                var layout = RuntimeUiGui.ResolveCompactInlineMetricContentLayout(
                    bundle.BattleContext, rects[index], icons[index],
                    labels[index], values[index],
                    compactIconSize: BattleUiLayout.HeaderMetricIconSize);
                Assert(Contains(rects[index], layout.IconRect)
                    && Contains(rects[index], layout.LabelRect)
                    && Contains(rects[index], layout.ValueRect)
                    && layout.IconRect.width + RuntimeUiQualityProfile.GeometryTolerance
                        >= BattleUiLayout.HeaderMetricIconSize
                    && layout.LabelRect.xMin - layout.IconVisualRect.xMax
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumIconTextGap
                    && layout.LabelRect.xMin - layout.IconVisualRect.xMax
                        <= RuntimeUiQualityProfile.MaximumIconTextGap
                            + RuntimeUiQualityProfile.GeometryTolerance
                    && layout.ValueRect.xMin - layout.LabelRect.xMax
                        >= RuntimeUiQualityProfile.MinimumIconTextGap
                    && layout.ValueRect.xMin - layout.LabelRect.xMax
                        <= RuntimeUiQualityProfile.MaximumIconTextGap
                    && Mathf.Abs(layout.GroupRect.center.x - rects[index].center.x)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                    && Mathf.Abs(layout.GroupRect.center.y - rects[index].center.y)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                    && layout.IconVisualRect.yMin - rects[index].yMin
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumContentInset
                    && rects[index].yMax - layout.IconVisualRect.yMax
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumContentInset,
                    suffix + "/battle.metric-" + index
                    + " has finite compact icon/label/value anatomy");
                AssertSingleLineFits(style, labels[index], layout.LabelRect,
                    suffix + "/battle.metric-" + index + ".label");
                AssertSingleLineFits(style, values[index], layout.ValueRect,
                    suffix + "/battle.metric-" + index + ".value");
                if (index == 0) firstCenter = layout.IconVisualRect.center.y;
                else Assert(Mathf.Abs(layout.IconVisualRect.center.y - firstCenter)
                        <= RuntimeUiQualityProfile.RepeatedCenterTolerance,
                    suffix + " Battle compact metric icon centers align");
            }

            var speed = RuntimeUiGui.ResolveActionContentLayout(
                bundle.BattleContext, bundle.Battle.SpeedAction, "2×",
                RuntimeUiActionKind.Quiet, RuntimeUiInteractionState.Selected,
                RuntimeUiArtSlot.IconControlSpeed,
                RuntimeUiTypographyRole.Supplemental);
            Assert(Mathf.Abs(speed.GroupRect.center.x
                    - bundle.Battle.SpeedAction.center.x)
                    <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                && Mathf.Abs(speed.GroupRect.center.y
                    - bundle.Battle.SpeedAction.center.y)
                    <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                && speed.LabelRect.xMin - speed.IconVisualRect.xMax
                    >= RuntimeUiQualityProfile.MinimumIconTextGap
                && speed.LabelRect.xMin - speed.IconVisualRect.xMax
                    <= RuntimeUiQualityProfile.MaximumIconTextGap,
                suffix + " Battle speed icon and value use the shared centered action group");
        }

        private static void ValidateRepeatedBaselines(LayoutBundle bundle, string suffix)
        {
            var cards = new[]
            {
                PortraitShellLayout.CreateLobbyLevelCard(
                    bundle.Lobby.Orchard01Card, bundle.Lobby.Frame.Scale),
                PortraitShellLayout.CreateLobbyLevelCard(
                    bundle.Lobby.Orchard02Card, bundle.Lobby.Frame.Scale),
                PortraitShellLayout.CreateLobbyLevelCard(
                    bundle.Lobby.Orchard03Card, bundle.Lobby.Frame.Scale),
            };
            var titleBaseline = LocalLineBoxBaseline(
                cards[0].Title, bundle.Lobby.Orchard01Card,
                bundle.LobbyContext.Styles.SingleLineText(
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft));
            var bodyBaseline = LocalLineBoxBaseline(
                cards[0].Body, bundle.Lobby.Orchard01Card,
                bundle.LobbyContext.Styles.SingleLineText(
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft));
            for (var index = 1; index < cards.Length; index++)
            {
                var owner = index == 1
                    ? bundle.Lobby.Orchard02Card : bundle.Lobby.Orchard03Card;
                Assert(Mathf.Abs(LocalLineBoxBaseline(cards[index].Title, owner,
                            bundle.LobbyContext.Styles.SingleLineText(
                                RuntimeUiTypographyRole.ControlLabel,
                                TextAnchor.MiddleLeft)) - titleBaseline)
                        <= RuntimeUiQualityProfile.BaselineTolerance,
                    suffix + " Lobby title baselines repeat within tolerance");
                Assert(Mathf.Abs(LocalLineBoxBaseline(cards[index].Body, owner,
                            bundle.LobbyContext.Styles.SingleLineText(
                                RuntimeUiTypographyRole.Supplemental,
                                TextAnchor.MiddleLeft)) - bodyBaseline)
                        <= RuntimeUiQualityProfile.BaselineTolerance,
                    suffix + " Lobby body baselines repeat within tolerance");
            }

            var settlementRows = new[]
            {
                bundle.Settlement.CompletedLevel,
                bundle.Settlement.ReachedWave,
                bundle.Settlement.RemainingLives,
            };
            var firstOffset = settlementRows[0].center.y
                - bundle.Settlement.ResultCard.yMin;
            var firstStep = settlementRows[1].center.y - settlementRows[0].center.y;
            Assert(firstOffset > 0f
                && Mathf.Abs((settlementRows[2].center.y - settlementRows[1].center.y)
                    - firstStep) <= RuntimeUiQualityProfile.BaselineTolerance,
                suffix + " Settlement metric rows use a repeated baseline step");

            var settlementLabels = new[]
            {
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.SettlementCompletedLevel).Text,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.SettlementReachedWave).Text,
                RuntimeUiCopyCatalog.Get(RuntimeUiCopyId.SettlementRemainingLives).Text,
            };
            var settlementValues = new[]
            {
                RuntimeUiCopyCatalog.LevelDisplayName("orchard-03"), "15", "3",
            };
            var settlementIcons = new[]
            {
                RuntimeUiArtSlot.IconResourceSun,
                RuntimeUiArtSlot.IconResourceWave,
                RuntimeUiArtSlot.IconResourceCore,
            };
            var firstIconOffset = 0f;
            var firstLabelBaseline = 0f;
            for (var index = 0; index < settlementRows.Length; index++)
            {
                var metric = RuntimeUiGui.ResolveCompactInlineMetricContentLayout(
                    bundle.SettlementContext, settlementRows[index],
                    settlementIcons[index], settlementLabels[index],
                    settlementValues[index]);
                Assert(Mathf.Abs(metric.GroupRect.center.x
                        - settlementRows[index].center.x)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                            * bundle.SettlementContext.Scale
                    && metric.IconVisualRect.xMin - settlementRows[index].xMin
                        >= RuntimeUiQualityProfile.MinimumContentInset
                            * bundle.SettlementContext.Scale
                    && metric.IconVisualRect.yMin - settlementRows[index].yMin
                        >= RuntimeUiQualityProfile.MinimumContentInset
                            * bundle.SettlementContext.Scale,
                    suffix + "/settlement.metric-" + index
                    + " centers its visual group with an eight-point icon inset");
                var iconOffset = metric.IconVisualRect.center.y
                    - settlementRows[index].yMin;
                var labelBaseline = LocalLineBoxBaseline(metric.LabelRect,
                    settlementRows[index], bundle.SettlementContext.Styles.SingleLineText(
                        RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft));
                if (index == 0)
                {
                    firstIconOffset = iconOffset;
                    firstLabelBaseline = labelBaseline;
                }
                else
                {
                    Assert(Mathf.Abs(iconOffset - firstIconOffset)
                            <= RuntimeUiQualityProfile.RepeatedCenterTolerance
                                * bundle.SettlementContext.Scale
                        && Mathf.Abs(labelBaseline - firstLabelBaseline)
                            <= RuntimeUiQualityProfile.BaselineTolerance
                                * bundle.SettlementContext.Scale,
                        suffix + " Settlement repeated metric icon centers/baselines align");
                }
            }
        }

        private static float LocalLineBoxBaseline(Rect textRect, Rect owner, GUIStyle style)
        {
            return textRect.center.y - owner.yMin + style.lineHeight * .5f;
        }

        private static void ValidateIllustrationOccupancy(Rect destination,
            RuntimeUiDrawContext context, RuntimeUiArtSlot slot, string caseName,
            bool lobbyThumbnail)
        {
            Assert(context.ArtSet.TryGetBinding(slot, out var binding)
                && binding.Texture != null,
                caseName + " has a production illustration binding");
            var sourceAspect = binding.Texture.width / (float)binding.Texture.height;
            var fittedWidth = Mathf.Min(destination.width,
                destination.height * sourceAspect);
            var fittedHeight = Mathf.Min(destination.height,
                destination.width / sourceAspect);
            var minimumWidth = (lobbyThumbnail
                    ? RuntimeUiQualityProfile.LobbyThumbnailMinimumWidth
                    : RuntimeUiQualityProfile.ResultVistaMinimumWidth)
                * context.Scale;
            var minimumHeight = (lobbyThumbnail
                    ? RuntimeUiQualityProfile.LobbyThumbnailMinimumHeight
                    : RuntimeUiQualityProfile.ResultVistaMinimumHeight)
                * context.Scale;
            Assert(destination.width + RuntimeUiQualityProfile.GeometryTolerance
                    >= minimumWidth
                && destination.height + RuntimeUiQualityProfile.GeometryTolerance
                    >= minimumHeight
                && (destination.width - fittedWidth) * .5f
                    <= RuntimeUiQualityProfile.IllustrationUnusedBarMaximum
                        * context.Scale + RuntimeUiQualityProfile.GeometryTolerance
                && (destination.height - fittedHeight) * .5f
                    <= RuntimeUiQualityProfile.IllustrationUnusedBarMaximum
                        * context.Scale + RuntimeUiQualityProfile.GeometryTolerance,
                caseName + " uses its destination without a decorative dead axis");
        }

        private static void ValidateEffectiveContrast(RuntimeUiTheme theme)
        {
            var normal = Contrast(theme.Colors.PrimaryText, theme.Colors.BaseSurface);
            Assert(normal + .001f >= RuntimeUiQualityProfile.NormalTextContrast,
                "normal primary text meets the small-text contrast floor");
            var secondary = Contrast(theme.Colors.SecondaryText,
                theme.Colors.BaseSurface);
            Assert(secondary + .001f >= RuntimeUiQualityProfile.NormalTextContrast,
                "secondary card copy remains subordinate without falling below 4.5:1");
            var primary = Contrast(theme.Colors.InverseText,
                theme.Colors.PrimaryAction);
            Assert(primary + .001f >= RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                "primary action inverse text meets the bold-text contrast floor");

            var pressedText = Composite(theme.Colors.InverseText,
                theme.Colors.PrimaryAction, theme.Feedback.PressedOpacity);
            Assert(Contrast(pressedText, theme.Colors.PrimaryAction) + .001f
                    >= RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                "pressed action copy preserves effective contrast");
            var contrastContext = RuntimeUiDrawContext.Create(theme, 1f);
            Assert(Mathf.Approximately(RuntimeUiGui.ResolveTextOpacity(contrastContext,
                        RuntimeUiInteractionState.Normal), theme.Feedback.NormalOpacity)
                && Mathf.Approximately(RuntimeUiGui.ResolveTextOpacity(contrastContext,
                        RuntimeUiInteractionState.Pressed), theme.Feedback.PressedOpacity)
                && Mathf.Approximately(RuntimeUiGui.ResolveTextOpacity(contrastContext,
                        RuntimeUiInteractionState.Selected), theme.Feedback.SelectedOpacity),
                "normal/pressed/selected text feedback remains unchanged");
            var loadingText = Composite(theme.Colors.PrimaryText,
                theme.Colors.BaseSurface, RuntimeUiGui.ResolveTextOpacity(
                    contrastContext,
                    RuntimeUiInteractionState.Loading));
            Assert(Contrast(loadingText, theme.Colors.BaseSurface) + .001f
                    >= RuntimeUiQualityProfile.DisabledReadableContrast,
                "loading readable copy preserves effective contrast");
            var disabledSurface = Composite(theme.Colors.Disabled,
                theme.Colors.BaseSurface, theme.Feedback.DisabledOpacity);
            var disabledText = Composite(theme.Colors.PrimaryText,
                disabledSurface, RuntimeUiGui.ResolveTextOpacity(
                    contrastContext,
                    RuntimeUiInteractionState.Disabled));
            Assert(RuntimeUiGui.ResolveActionTextTone(RuntimeUiActionKind.Primary,
                    RuntimeUiInteractionState.Disabled) == RuntimeUiTextTone.Primary
                && Contrast(disabledText, disabledSurface) + .001f
                    >= RuntimeUiQualityProfile.DisabledReadableContrast,
                "disabled readable copy preserves effective contrast");

            var requiredCueSlots = new[]
            {
                RuntimeUiArtSlot.MarkerSelected,
                RuntimeUiArtSlot.IndicatorDisabled,
                RuntimeUiArtSlot.IndicatorLoading,
                RuntimeUiArtSlot.IndicatorSuccess,
                RuntimeUiArtSlot.IndicatorWarning,
                RuntimeUiArtSlot.IndicatorError,
            };
            var sprites = new HashSet<Sprite>();
            for (var index = 0; index < requiredCueSlots.Length; index++)
            {
                Assert(theme.ActiveArtSet.TryGetBinding(requiredCueSlots[index],
                        out var binding)
                    && binding.Sprite != null && sprites.Add(binding.Sprite),
                    RuntimeUiArtSlots.SemanticId(requiredCueSlots[index])
                    + " is a distinct non-color cue");
            }

            var stateCopies = new[]
            {
                RuntimeUiInteractionState.Normal,
                RuntimeUiInteractionState.Selected,
                RuntimeUiInteractionState.Pressed,
                RuntimeUiInteractionState.Loading,
                RuntimeUiInteractionState.Disabled,
                RuntimeUiInteractionState.Success,
                RuntimeUiInteractionState.Warning,
                RuntimeUiInteractionState.Error,
            };
            for (var index = 0; index < stateCopies.Length; index++)
            {
                if (stateCopies[index] == RuntimeUiInteractionState.Normal
                    || stateCopies[index] == RuntimeUiInteractionState.Pressed)
                    continue;
                Assert(RuntimeUiGui.TryResolveStateIndicatorRect(
                        RuntimeUiDrawContext.Create(theme, 1f),
                        new Rect(0f, 0f, 88f, 52f), out var indicator)
                    && indicator.width > 0f && indicator.height > 0f,
                    stateCopies[index] + " has finite non-color indicator geometry");
            }
        }

        private static void ValidateSourceAuthorities()
        {
            var runtimeGui = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/UI/RuntimeUiGui.cs"));
            Assert(runtimeGui.Contains("ResolveActionContentLayout(")
                && runtimeGui.Contains("ResolveMetricContentLayout(")
                && runtimeGui.Contains("TryResolveStateIndicatorRect(")
                && runtimeGui.Contains("IconVisualRect")
                && runtimeGui.Contains("GroupRect")
                && !runtimeGui.Contains("centerIconAndLabel"),
                "shared draw code exposes the same component anatomy used by quality tests");

            var lobby = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Shell/LobbyPresenter.cs"));
            var settlement = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Shell/SettlementPresenter.cs"));
            var bootstrap = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/App/AppFlowCoordinator.cs"));
            var battle = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/FruitDefenseGame.cs"));
            Assert(lobby.Contains("RuntimeUiCopyCatalog")
                && settlement.Contains("RuntimeUiCopyCatalog")
                && bootstrap.Contains("RuntimeUiCopyCatalog")
                && battle.Contains("RuntimeUiCopyCatalog"),
                "all four routes consume the finite product-copy authority");
            Assert(runtimeGui.Contains("public static void DrawBlockingModal(")
                && runtimeGui.Contains("public static void DrawResultCard(")
                && !MethodBodyContains(runtimeGui,
                    "public static void DrawBlockingModal(", "DrawStateIndicator(")
                && !MethodBodyContains(runtimeGui,
                    "public static void DrawResultCard(", "DrawStateIndicator("),
                "modal/result surfaces do not auto-own a duplicate state badge");
            Assert(CountOccurrences(settlement,
                       "RuntimeUiGui.DrawIndicator(_drawContext, indicatorRect") == 1
                && CountOccurrences(settlement,
                       "layout.ResultIndicator, layout.ResultCard, resultCardRect") == 1
                && CountOccurrences(battle,
                       "RuntimeUiGui.DrawIndicator(drawContext, layout.ModalResultIndicator") == 1,
                "Settlement and Battle each own one explicit terminal/result badge in their visual parent");
            Assert(bootstrap.Contains(
                    "layout.Screen, layout.Modal, RuntimeUiInteractionState.Normal")
                && bootstrap.Contains("RuntimeUiGui.DrawStatus(_runtimeUiDrawContext"),
                "Bootstrap keeps the modal neutral and assigns the sole state cue to status");
            Assert(battle.Contains("compactInline: true")
                && battle.Contains("BattleUiLayout.HeaderMetricIconSize")
                && battle.Contains("DrawControlledTwoLineText")
                && battle.Contains("layout.ModalResultBannerText")
                && battle.Contains("content.ResultBannerText")
                && battle.Contains("RuntimeUiArtSlot.IconControlSpeed")
                && !battle.Contains("layout.SpeedActionIcon")
                && !battle.Contains("layout.SpeedActionValue"),
                "Battle consumes compact metric and controlled terminal-copy anatomy");
            Assert(!settlement.Contains("DrawMetricDivider")
                && !settlement.Contains("FirstMetricDivider")
                && !settlement.Contains("SecondMetricDivider"),
                "Settlement full-width metric rows replace obsolete empty dividers");
            Assert(lobby.Contains("FormatLobbyStart(_visibleSelectedLevelId)")
                && settlement.Contains("LevelDisplayName(ViewData.LevelId)")
                && lobby.Contains("drawStateIndicator: false")
                && lobby.Contains("RuntimeUiIndicatorKind.Selected")
                && !lobby.Contains("\"开始战斗 · \" + _visibleSelectedLevelId")
                && !settlement.Contains("\"完成关卡 \" + ViewData.LevelId"),
                "Lobby and Settlement never expose internal level IDs as copy");
            Assert(!runtimeGui.Contains("GUI.skin")
                && !runtimeGui.Contains("Texture2D.whiteTexture")
                && !runtimeGui.Contains("Resources.Load"),
                "shared quality path has no default-skin or resource fallback");
        }

        private static bool MethodBodyContains(string source, string signature,
            string token)
        {
            var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
            if (signatureIndex < 0) return false;
            var bodyStart = source.IndexOf('{', signatureIndex);
            if (bodyStart < 0) return false;
            var depth = 0;
            for (var index = bodyStart; index < source.Length; index++)
            {
                if (source[index] == '{') depth++;
                else if (source[index] == '}' && --depth == 0)
                    return source.IndexOf(token, bodyStart,
                        index - bodyStart, StringComparison.Ordinal) >= 0;
            }
            return false;
        }

        private static int CountOccurrences(string source, string token)
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

        private static void ValidateTouch(Rect rect, float scale, string caseName)
        {
            Assert(Mathf.Min(rect.width, rect.height)
                    + RuntimeUiQualityProfile.GeometryTolerance
                    >= RuntimeUiQualityProfile.MinimumTouchTarget * scale,
                caseName + " meets the scaled 44-point touch target");
        }

        private static void AssertSingleLineFits(GUIStyle style, string text,
            Rect rect, string caseName)
        {
            var content = new GUIContent(text);
            var measured = style.CalcSize(content);
            var calculatedHeight = style.CalcHeight(content, rect.width);
            Assert(measured.x <= rect.width + RuntimeUiQualityProfile.GeometryTolerance
                && measured.y <= rect.height + RuntimeUiQualityProfile.GeometryTolerance
                && calculatedHeight
                    <= rect.height + RuntimeUiQualityProfile.GeometryTolerance,
                caseName + " fits without clipping; measured=" + measured
                + " CalcHeight=" + calculatedHeight + " rect=" + rect);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            var tolerance = RuntimeUiQualityProfile.GeometryTolerance;
            return inner.xMin >= outer.xMin - tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        private static bool Overlaps(Rect first, Rect second)
        {
            var tolerance = RuntimeUiQualityProfile.GeometryTolerance;
            return Mathf.Min(first.xMax, second.xMax)
                    - Mathf.Max(first.xMin, second.xMin) > tolerance
                && Mathf.Min(first.yMax, second.yMax)
                    - Mathf.Max(first.yMin, second.yMin) > tolerance;
        }

        private static Color Composite(Color foreground, Color background, float alpha)
        {
            alpha = Mathf.Clamp01(alpha * foreground.a);
            return new Color(
                foreground.r * alpha + background.r * (1f - alpha),
                foreground.g * alpha + background.g * (1f - alpha),
                foreground.b * alpha + background.b * (1f - alpha), 1f);
        }

        private static float Contrast(Color first, Color second)
        {
            var bright = Mathf.Max(Luminance(first), Luminance(second));
            var dark = Mathf.Min(Luminance(first), Luminance(second));
            return (bright + .05f) / (dark + .05f);
        }

        private static float Luminance(Color color)
        {
            return .2126f * Linear(color.r)
                + .7152f * Linear(color.g)
                + .0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= .03928f ? value / 12.92f
                : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Runtime UI quality validation failed: " + message);
        }
    }
}
