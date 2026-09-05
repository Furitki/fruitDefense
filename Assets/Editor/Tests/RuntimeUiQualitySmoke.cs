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
                bool hasIcon, bool fits = true,
                RuntimeUiStatusTextLayout statusLayout = default,
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
                Fits = fits;
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
            public bool Fits { get; }
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
                Hub = PortraitHubLayout.Create(
                    viewport.Width, viewport.Height, safeArea);
                Settlement = PortraitShellLayout.CreateSettlement(
                    viewport.Width, viewport.Height, safeArea);
                Battle = new BattleUiLayout(GameConfig.DefaultBattlefield);
                BattleViewport = BattlefieldProjection.CalculateViewportLayout(
                    viewport.Width, viewport.Height, safeArea,
                    BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
                BootstrapContext = RuntimeUiDrawContext.Create(theme, Bootstrap.Scale);
                HubContext = RuntimeUiDrawContext.Create(theme, Hub.Frame.Scale);
                SettlementContext = RuntimeUiDrawContext.Create(
                    theme, Settlement.Frame.Scale);
                BattleContext = RuntimeUiDrawContext.Create(theme, 1f);
                ProjectedBattleContext = RuntimeUiDrawContext.Create(
                    theme, BattleViewport.Scale);
            }

            public RuntimeUiQualityViewportCase Viewport { get; }
            public Rect SafeArea { get; }
            public AppFlowCoordinator.BootstrapPresentationLayout Bootstrap { get; }
            public PortraitHubResolvedLayout Hub { get; }
            public SettlementShellLayout Settlement { get; }
            public BattleUiLayout Battle { get; }
            public BattlefieldViewportLayout BattleViewport { get; }
            public RuntimeUiDrawContext BootstrapContext { get; }
            public RuntimeUiDrawContext HubContext { get; }
            public RuntimeUiDrawContext SettlementContext { get; }
            public RuntimeUiDrawContext BattleContext { get; }
            public RuntimeUiDrawContext ProjectedBattleContext { get; }
        }

        public static void Run()
        {
            var theme = ProjectSetup.RequireReleaseRuntimeUiTheme();
            ValidateProfile(theme);
            ValidateCatalogCoverage();
            ValidateResolverFailureSignals(theme);
            ValidateTextAndGeometryMatrix(theme);
            ValidateSettlementOutcomeEmphasis(theme);
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
            Assert(theme.ThemeId == "ui.sunny-orchard" && theme.Revision == "12",
                "quality inspection uses the sky-paper release theme revision");
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
                var expectedPath = RuntimeUiQualityProfile.UsesDisplayFace(role)
                    ? ProjectSetup.DisplayRuntimeUiFontPath
                    : ProjectSetup.ReadingRuntimeUiFontPath;
                Assert(typography.FontSize == RuntimeUiQualityProfile.MinimumFontSize(role)
                    && typography.FontSize >= RuntimeUiQualityProfile.MinimumNormalTextSize
                    && typography.LineHeight == RuntimeUiQualityProfile.LineHeight(role)
                    && typography.Font != null
                    && AssetDatabase.GetAssetPath(typography.Font) == expectedPath,
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
                if (inspection.CoversCatalogCopy)
                    inspected.Add(inspection.CopyId);
            }

            foreach (var copyId in enumValues)
            {
                Assert(inspected.Contains(copyId),
                    "every rendered stable product copy has an inspection case: " + copyId);
            }
            var requiredHubActionCases = new[]
            {
                "hub.activity.claim",
                "hub.activity.claiming",
                "hub.activity.claimed",
                "hub.activity.locked",
                "hub.growth.equip",
                "hub.growth.upgrade",
                "hub.growth.maximum",
                "hub.growth.insufficient",
                "hub.growth.loading",
                "hub.growth.locked-action",
                "hub.cultivation.upgrade",
                "hub.cultivation.maximum",
                "hub.cultivation.locked-action",
                "hub.cultivation.insufficient-action",
                "hub.cultivation.loading-action",
            };
            for (var index = 0; index < requiredHubActionCases.Length; index++)
            {
                Assert(caseIds.Contains(requiredHubActionCases[index]),
                    "every finite Hub action state owns a CalcSize fit case: "
                    + requiredHubActionCases[index]);
            }
            var localizedStart = RuntimeUiCopyCatalog.Get(
                RuntimeUiCopyId.LobbyStart).Text;
            Assert(localizedStart == "开始战斗",
                "Lobby CTA preserves the concise authoritative reference copy");
            Assert(RuntimeUiCopyCatalog.LevelDisplayName("orchard-01") == "第一关"
                && RuntimeUiCopyCatalog.LevelDisplayName("orchard-02") == "第二关"
                && RuntimeUiCopyCatalog.LevelDisplayName("orchard-03") == "第三关",
                "finite release level IDs map to localized player-facing names");

            var requiredBattleBoundaries = new[]
            {
                "battle.tool-count.max",
                "battle.pot-count.max",
                "battle.nursery-stars.max",
                "battle.refresh-cost.max",
                "battle.status.success-prefix.max",
                "battle.status.error-prefix.max",
                "battle.merge-hint.max",
            };
            for (var index = 0; index < requiredBattleBoundaries.Length; index++)
            {
                Assert(caseIds.Contains(requiredBattleBoundaries[index]),
                    "dynamic Battle boundary is registered: "
                    + requiredBattleBoundaries[index]);
            }

            var refreshSemanticVerified = false;
            foreach (var inspection in RuntimeUiTextInspectionCatalog.Cases)
            {
                if (inspection.Target
                        != RuntimeUiTextInspectionTarget.BattleRefreshAction)
                    continue;
                var spec = inspection.ActionSpec;
                Assert(inspection.ActionSemantic
                        == BattleUiActionSemantic.NurseryRefresh
                    && spec.Role == RuntimeUiActionKind.Secondary
                    && spec.ContentForm == RuntimeUiActionContentForm.IconLabel
                    && spec.Behavior == RuntimeUiActionBehavior.Instantaneous,
                    inspection.Id
                    + " consumes the real Secondary nursery-refresh semantic spec");
                refreshSemanticVerified = true;
            }
            Assert(refreshSemanticVerified,
                "refresh inspection is present and semantic rather than hand-authored");
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
            var typography = geometry.Context.Theme.Typography.For(copy.Role);
            Assert(geometry.Style != null
                && ReferenceEquals(geometry.Style.font, typography.Font)
                && geometry.Style.fontStyle == FontStyle.Normal,
                caseName + " measures and draws with its packaged role font");
            Assert(geometry.Style.fontSize == Mathf.Max(1, Mathf.RoundToInt(
                       typography.FontSize
                       * geometry.Context.Scale)),
                caseName + " uses its semantic typography role");
            Assert(geometry.Style.alignment == copy.Alignment,
                caseName + " uses its catalog alignment");
            Assert(geometry.Fits
                && ContainsTextPixelRounded(
                    geometry.ComponentRect, geometry.FirstLineRect)
                && (!geometry.HasIcon
                    || ContainsTextPixelRounded(
                        geometry.ComponentRect, geometry.IconRect)),
                caseName + " resolver reports fit and text/icon remain inside the owner; owner="
                + geometry.ComponentRect + " line=" + geometry.FirstLineRect
                + " icon=" + geometry.IconRect + " fits=" + geometry.Fits);

            var expectedLineCount = IsAdaptiveBattleStatusTarget(inspection.Target)
                ? geometry.MaximumLineCount
                : IsControlledNurseryStored(inspection) ? 2
                : IsControlledNurseryStars(inspection) ? 2
                : copy.LinePolicy == RuntimeUiCopyLinePolicy.ControlledTwoLines ? 2 : 1;
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
                    >= expectedLineHeight,
                caseName + " provides the complete semantic line height without compression");

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
                Assert((!geometry.HasIcon
                        || geometry.IconRect.xMin
                            >= geometry.ComponentRect.xMin + borderGap
                        && geometry.IconRect.yMin
                            >= geometry.ComponentRect.yMin + borderGap
                        && geometry.IconRect.xMax
                            <= geometry.ComponentRect.xMax - borderGap
                        && geometry.IconRect.yMax
                            <= geometry.ComponentRect.yMax - borderGap)
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
                    1, true, inline.Fits);
            }

            if (inspection.Target == RuntimeUiTextInspectionTarget.HubResourceBalance)
            {
                var balance = RuntimeUiGui.ResolveHubBalanceLayout(
                    context, component, copy.Text,
                    string.IsNullOrEmpty(inspection.MetricValue)
                        ? "999" : inspection.MetricValue,
                    inspection.State);
                var balanceText = RuntimeUiGui.ResolveSingleLineTextRect(
                    context, balance.Label, copy.Role, copy.Alignment,
                    inspection.State);
                return new TextGeometry(context, balance.Label, balanceText,
                    default, default, default,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, false);
            }

            if (IsHubNavigationTarget(inspection.Target))
            {
                var navigation = RuntimeUiGui.ResolveHubNavigationItemLayout(
                    context, component,
                    inspection.Target
                        == RuntimeUiTextInspectionTarget.HubPrimaryHome,
                    inspection.State);
                var hubNavigationTextRect = RuntimeUiGui.ResolveSingleLineTextRect(
                    context, navigation.Label, copy.Role, copy.Alignment,
                    inspection.State);
                return new TextGeometry(context, navigation.Label,
                    hubNavigationTextRect, default,
                    default, default,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, false);
            }

            if (IsHubGrowthTabTarget(inspection.Target))
            {
                var tab = RuntimeUiGui.ResolveHubGrowthTabLayout(
                    context, component, inspection.State);
                var tabTextRect = RuntimeUiGui.ResolveSingleLineTextRect(
                    context, tab.Label, copy.Role, copy.Alignment,
                    inspection.State);
                return new TextGeometry(context, tab.Label, tabTextRect,
                    default, default, default,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, false);
            }

            if (inspection.Target
                    == RuntimeUiTextInspectionTarget.HubHomeGrowthPreviewTitle
                || inspection.Target
                    == RuntimeUiTextInspectionTarget.HubHomeGrowthPreviewBody)
            {
                var preview = RuntimeUiGui.ResolveHubHomeGrowthPreviewLayout(
                    context, component, inspection.State);
                if (inspection.Target
                    == RuntimeUiTextInspectionTarget.HubHomeGrowthPreviewTitle)
                {
                    var previewTitle = RuntimeUiGui.ResolveSingleLineTextRect(
                        context, preview.Title, copy.Role, copy.Alignment,
                        inspection.State);
                    return new TextGeometry(context, preview.Title, previewTitle,
                        default, default, default,
                        context.Styles.SingleLineText(copy.Role, copy.Alignment),
                        1, false);
                }

                if (copy.LinePolicy
                    == RuntimeUiCopyLinePolicy.ControlledTwoLines)
                {
                    var previewBody =
                        RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                            context, preview.Body, copy.Role, copy.Alignment,
                            inspection.State);
                    return new TextGeometry(context, preview.Body,
                        previewBody.FirstLineRect, previewBody.SecondLineRect,
                        default, default, previewBody.Style, 2, false,
                        statusLayout: previewBody, hasStatusLayout: true);
                }

                var previewBodyRect = RuntimeUiGui.ResolveSingleLineTextRect(
                    context, preview.Body, copy.Role, copy.Alignment,
                    inspection.State);
                return new TextGeometry(context, preview.Body, previewBodyRect,
                    default, default, default,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, false);
            }

            if (IsControlledNurseryStored(inspection)
                || IsControlledNurseryStars(inspection))
            {
                var twoLine = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                    context, component, copy.Role, copy.Alignment, inspection.State);
                return new TextGeometry(context, component, twoLine.FirstLineRect,
                    twoLine.SecondLineRect, default, default, twoLine.Style, 2,
                    false, statusLayout: twoLine, hasStatusLayout: true);
            }

            if (IsActionTarget(inspection.Target))
            {
                var action = RuntimeUiGui.ResolveActionContentLayout(
                    context, component, copy.Text, inspection.ActionSpec,
                    inspection.State, inspection.IconSlot, copy.Role);
                return new TextGeometry(context, component, action.LabelRect,
                    default, action.IconVisualRect, action.GroupRect,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, action.HasIcon, action.Fits);
            }

            if (IsStatusTarget(inspection.Target))
            {
                var mode = IsAdaptiveBattleStatusTarget(inspection.Target)
                    ? RuntimeUiGui.ResolveStatusTextMode(context, component,
                        copy.Text, inspection.State, copy.Role)
                    : RuntimeUiCopyCatalog.StatusTextMode(copy);
                var status = RuntimeUiGui.ResolveStatusTextLayout(context,
                    component, inspection.State, copy.Role, mode);
                return new TextGeometry(context, component, status.FirstLineRect,
                    status.SecondLineRect, status.IndicatorRect, default,
                    status.Style, status.MaximumLineCount, status.HasIndicator,
                    statusLayout: status, hasStatusLayout: true);
            }

            if (IsMetricTarget(inspection.Target))
            {
                var compactIconSize = IsBattleHeaderMetricTarget(inspection.Target)
                    ? BattleUiLayout.HeaderMetricIconSize : 24f;
                var metric = RuntimeUiGui.ResolveCompactInlineMetricContentLayout(
                    context, component, MetricIcon(inspection.Target), copy.Text,
                    MetricValue(inspection), inspection.State,
                    compactIconSize,
                    reserveSurfaceInset: IsBattleHeaderMetricTarget(inspection.Target));
                return new TextGeometry(context, component, metric.LabelRect, default,
                    metric.IconVisualRect, metric.GroupRect,
                    context.Styles.SingleLineText(copy.Role, copy.Alignment),
                    1, true, metric.Fits);
            }

            if (copy.LinePolicy == RuntimeUiCopyLinePolicy.ControlledTwoLines)
            {
                var twoLine = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                    context, component, copy.Role, copy.Alignment, inspection.State);
                return new TextGeometry(context, component, twoLine.FirstLineRect,
                    twoLine.SecondLineRect, default, default, twoLine.Style, 2,
                    false, statusLayout: twoLine, hasStatusLayout: true);
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
                return bundle.HubContext;
            if (target <= RuntimeUiTextInspectionTarget.HubGrowthAction)
                return bundle.HubContext;
            if (target <= RuntimeUiTextInspectionTarget.BattleModalTerminalAction)
                return bundle.ProjectedBattleContext;
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
                    return bundle.Hub.TopBar;
                case RuntimeUiTextInspectionTarget.LobbyOrchard01Title:
                    return PortraitHubLayout.CreateHomeLevelCard(
                        bundle.Hub.HomePage.Orchard01Card,
                        bundle.HubContext.Scale).Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard01Body:
                    return PortraitHubLayout.CreateHomeLevelCard(
                        bundle.Hub.HomePage.Orchard01Card,
                        bundle.HubContext.Scale).Body;
                case RuntimeUiTextInspectionTarget.LobbyOrchard02Title:
                    return PortraitHubLayout.CreateHomeLevelCard(
                        bundle.Hub.HomePage.Orchard02Card,
                        bundle.HubContext.Scale).Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard02Body:
                    return PortraitHubLayout.CreateHomeLevelCard(
                        bundle.Hub.HomePage.Orchard02Card,
                        bundle.HubContext.Scale).Body;
                case RuntimeUiTextInspectionTarget.LobbyOrchard03Title:
                    return PortraitHubLayout.CreateHomeLevelCard(
                        bundle.Hub.HomePage.Orchard03Card,
                        bundle.HubContext.Scale).Title;
                case RuntimeUiTextInspectionTarget.LobbyOrchard03Body:
                    return PortraitHubLayout.CreateHomeLevelCard(
                        bundle.Hub.HomePage.Orchard03Card,
                        bundle.HubContext.Scale).Body;
                case RuntimeUiTextInspectionTarget.LobbyStart:
                    return bundle.Hub.HomePage.StartButton;
                case RuntimeUiTextInspectionTarget.LobbyStatus:
                    return bundle.Hub.HomePage.GrowthPreview;
                case RuntimeUiTextInspectionTarget.HubTopBarTitle:
                    return bundle.Hub.TopBarContent.Title;
                case RuntimeUiTextInspectionTarget.HubPrimaryHome:
                    return bundle.Hub.PrimaryNavigation.Home;
                case RuntimeUiTextInspectionTarget.HubPrimaryActivity:
                    return bundle.Hub.PrimaryNavigation.Activity;
                case RuntimeUiTextInspectionTarget.HubPrimaryGrowth:
                    return bundle.Hub.PrimaryNavigation.Growth;
                case RuntimeUiTextInspectionTarget.HubGrowthEquipmentTab:
                    return bundle.Hub.GrowthPage.Navigation.Equipment;
                case RuntimeUiTextInspectionTarget.HubGrowthCultivationTab:
                    return bundle.Hub.GrowthPage.Navigation.Cultivation;
                case RuntimeUiTextInspectionTarget.HubHomeGrowthPreviewTitle:
                case RuntimeUiTextInspectionTarget.HubHomeGrowthPreviewBody:
                    return bundle.Hub.HomePage.GrowthPreview;
                case RuntimeUiTextInspectionTarget.HubUnavailableTitle:
                    return bundle.Hub.ActivityPage.Title;
                case RuntimeUiTextInspectionTarget.HubUnavailableBody:
                    return inspection.CopyId
                        == RuntimeUiCopyId.HubActivityUnavailableBody
                            ? bundle.Hub.ActivityPage.Description
                            : bundle.Hub.GrowthPage.Description;
                case RuntimeUiTextInspectionTarget.HubResourceBalance:
                    return bundle.Hub.TopBarContent.ResourceBalance;
                case RuntimeUiTextInspectionTarget.HubActivityRewardTitle:
                    return bundle.Hub.ActivityPage.RewardTitle;
                case RuntimeUiTextInspectionTarget.HubActivityStatus:
                    return bundle.Hub.ActivityPage.Status;
                case RuntimeUiTextInspectionTarget.HubActivityAction:
                    return bundle.Hub.ActivityPage.PrimaryAction;
                case RuntimeUiTextInspectionTarget.HubGrowthEntryStatus:
                    return bundle.Hub.GrowthPage.EntryStatus;
                case RuntimeUiTextInspectionTarget.HubGrowthRank:
                    return bundle.Hub.GrowthPage.Rank;
                case RuntimeUiTextInspectionTarget.HubGrowthEffect:
                    return bundle.Hub.GrowthPage.Effect;
                case RuntimeUiTextInspectionTarget.HubGrowthCost:
                    return bundle.Hub.GrowthPage.Cost;
                case RuntimeUiTextInspectionTarget.HubGrowthStatus:
                    return bundle.Hub.GrowthPage.Status;
                case RuntimeUiTextInspectionTarget.HubGrowthAction:
                    return inspection.Id.StartsWith("hub.cultivation.",
                               StringComparison.Ordinal)
                        ? bundle.Hub.GrowthPage.CultivationPrimaryAction
                        : bundle.Hub.GrowthPage.EquipmentPrimaryAction;
                case RuntimeUiTextInspectionTarget.BattleHeaderTitle:
                    return ProjectBattleRect(bundle, bundle.Battle.HeaderTitle);
                case RuntimeUiTextInspectionTarget.BattleSunMetric:
                    return ProjectBattleRect(bundle, bundle.Battle.SunMetric);
                case RuntimeUiTextInspectionTarget.BattleCoreMetric:
                    return ProjectBattleRect(bundle, bundle.Battle.LivesMetric);
                case RuntimeUiTextInspectionTarget.BattleWaveMetric:
                    return ProjectBattleRect(bundle, bundle.Battle.WaveMetric);
                case RuntimeUiTextInspectionTarget.BattlePhaseStatus:
                    return ProjectBattleRect(bundle,
                        bundle.Battle.PhaseStatusWithWaveAction);
                case RuntimeUiTextInspectionTarget.BattlePhaseStatusFull:
                    return ProjectBattleRect(bundle, bundle.Battle.PhaseStatus);
                case RuntimeUiTextInspectionTarget.BattleWaveAction:
                    return ProjectBattleRect(bundle, bundle.Battle.WaveAction);
                case RuntimeUiTextInspectionTarget.BattleContextTrayTitle:
                    return ProjectBattleRect(bundle, bundle.Battle.ContextTrayTitle);
                case RuntimeUiTextInspectionTarget.BattleNurseryTrayTitle:
                    return ProjectBattleRect(bundle, bundle.Battle.NurseryTrayTitle);
                case RuntimeUiTextInspectionTarget.BattleNurserySlot:
                    return ProjectBattleRect(bundle,
                        inspection.CopyId == RuntimeUiCopyId.BattleNurseryPotStored
                        ? BattleUiLayout.NurserySlotLabel(bundle.Battle.NurserySlot(0))
                        : bundle.Battle.NurserySlot(0));
                case RuntimeUiTextInspectionTarget.BattleToolCount:
                    return ProjectBattleRect(bundle,
                        BattleUiLayout.ToolInventoryBadge(bundle.Battle.Tool(0)));
                case RuntimeUiTextInspectionTarget.BattlePotCount:
                    return ProjectBattleRect(bundle,
                        BattleUiLayout.ToolInventoryBadge(bundle.Battle.PotTool));
                case RuntimeUiTextInspectionTarget.BattleNurseryStars:
                    return ProjectBattleRect(bundle,
                        BattleUiLayout.NurserySlotLabel(bundle.Battle.NurserySlot(0)));
                case RuntimeUiTextInspectionTarget.BattleRefreshAction:
                    return ProjectBattleRect(bundle, bundle.Battle.RefreshAction);
                case RuntimeUiTextInspectionTarget.BattleDetailTitle:
                    return ProjectBattleRect(bundle, bundle.Battle.DetailTitle);
                case RuntimeUiTextInspectionTarget.BattleDetailBody:
                    return ProjectBattleRect(bundle, bundle.Battle.DetailBody);
                case RuntimeUiTextInspectionTarget.BattleMergeHint:
                {
                    var mergeStyle = bundle.ProjectedBattleContext.Styles.SingleLineText(
                        inspection.Copy.Role, inspection.Copy.Alignment);
                    var measured = mergeStyle.CalcSize(
                        new GUIContent(inspection.Copy.Text)).x;
                    var logicalWidth = measured
                        / Mathf.Max(.0001f, bundle.BattleViewport.Scale);
                    var mergeHint = bundle.Battle.MergeHint(
                        new Rect(180f, 180f, 48f, 48f), logicalWidth);
                    return ProjectBattleRect(bundle,
                        BattleUiLayout.CueLabel(mergeHint));
                }
                case RuntimeUiTextInspectionTarget.BattleModalTitle:
                    return ProjectBattleRect(bundle,
                        inspection.CopyId == RuntimeUiCopyId.BattleVictoryTitle
                        || inspection.CopyId == RuntimeUiCopyId.BattleDefeatTitle
                        ? bundle.Battle.ModalTerminalTitle
                        : bundle.Battle.ModalTitle);
                case RuntimeUiTextInspectionTarget.BattleModalMessage:
                    return ProjectBattleRect(bundle, bundle.Battle.ModalPauseHint);
                case RuntimeUiTextInspectionTarget.BattleModalResultBanner:
                    return ProjectBattleRect(bundle,
                        bundle.Battle.ModalResultBannerText);
                case RuntimeUiTextInspectionTarget.BattleModalTerminalMessage:
                    return ProjectBattleRect(bundle,
                        bundle.Battle.ModalTerminalMessage);
                case RuntimeUiTextInspectionTarget.BattleModalPrimaryAction:
                    return ProjectBattleRect(bundle, bundle.Battle.ModalAction(0, 2));
                case RuntimeUiTextInspectionTarget.BattleModalSecondaryAction:
                    return ProjectBattleRect(bundle, bundle.Battle.ModalAction(1, 2));
                case RuntimeUiTextInspectionTarget.BattleModalTerminalAction:
                    return ProjectBattleRect(bundle, bundle.Battle.ModalAction(0, 1));
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

        private static void ValidateResolverFailureSignals(RuntimeUiTheme theme)
        {
            var context = RuntimeUiDrawContext.Create(theme, 1f);
            var action = RuntimeUiGui.ResolveActionContentLayout(context,
                new Rect(0f, 0f, 72f, 44f),
                RuntimeUiCopyCatalog.FormatRefreshAction(999),
                BattleUiPresentationState.ResolveActionSpec(
                    BattleUiActionSemantic.NurseryRefresh),
                RuntimeUiInteractionState.Normal,
                RuntimeUiArtSlot.IconControlRefresh);
            Assert(!action.Fits,
                "action resolver exposes label/icon clamping as a failed fit");

            var metric = RuntimeUiGui.ResolveCompactInlineMetricContentLayout(
                context, new Rect(0f, 0f, 64f, 22f),
                RuntimeUiArtSlot.IconResourceSunMicro, "阳光", "999",
                compactIconSize: BattleUiLayout.HeaderMetricIconSize);
            Assert(!metric.Fits,
                "metric resolver exposes value clamping as a failed fit");

            var inline = RuntimeUiGui.ResolveInlineContentLayout(context,
                new Rect(0f, 0f, 48f, 24f), RuntimeUiArtSlot.IndicatorWarning,
                "按空格或选择操作", RuntimeUiTypographyRole.Body,
                RuntimeUiInteractionState.Warning);
            Assert(!inline.Fits,
                "inline resolver exposes copy clamping as a failed fit");

            var requiredHeight = theme.Typography.For(
                RuntimeUiTypographyRole.Supplemental).LineHeight;
            AssertThrows<InvalidOperationException>(() =>
                RuntimeUiGui.ResolveSingleLineTextRect(context,
                    new Rect(0f, 0f, 160f, requiredHeight - 1f),
                    RuntimeUiTypographyRole.Supplemental,
                    TextAnchor.MiddleLeft),
                "single-line resolver rejects an owner below semantic line-height");

            var twoLine = RuntimeUiGui.ResolveStatusTextLayout(context,
                new Rect(0f, 0f, 48f, 48f), RuntimeUiInteractionState.Error,
                RuntimeUiTypographyRole.Supplemental,
                RuntimeUiStatusTextMode.CompactTwoLines);
            var split = RuntimeUiGui.ResolveStatusTextLines(twoLine,
                new string('宽', 40));
            Assert(!split.HasSecondLine,
                "controlled two-line resolver exposes an unsplittable boundary sample");

            var previewLayout = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                context, new Rect(0f, 0f, 320f, 60f),
                RuntimeUiTypographyRole.Body, TextAnchor.MiddleLeft);
            const string previewFirst = "教学果园成长规则";
            const string previewSecond = "生效 0 项 · 受限 0 项";
            var previewLines = RuntimeUiGui.ResolveStatusTextLines(previewLayout,
                previewFirst + "\n" + previewSecond);
            Assert(previewLines.HasSecondLine
                && string.Equals(previewLines.FirstLine, previewFirst,
                    StringComparison.Ordinal)
                && string.Equals(previewLines.SecondLine, previewSecond,
                    StringComparison.Ordinal)
                && previewLines.FirstLine.IndexOf('\n') < 0
                && previewLines.SecondLine.IndexOf('\n') < 0,
                "controlled two-line resolver consumes one explicit authoring break without leaking it into a single-line owner");
        }

        private static Rect ProjectBattleRect(LayoutBundle bundle, Rect rect)
        {
            var projected = bundle.BattleViewport.ProjectDesignRect(rect);
            return Rect.MinMaxRect(
                Mathf.Floor(projected.xMin), Mathf.Floor(projected.yMin),
                Mathf.Ceil(projected.xMax), Mathf.Ceil(projected.yMax));
        }

        private static bool IsActionTarget(RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.BootstrapRetry
                || target == RuntimeUiTextInspectionTarget.LobbyStart
                || target == RuntimeUiTextInspectionTarget.HubActivityAction
                || target == RuntimeUiTextInspectionTarget.HubGrowthAction
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
                || target == RuntimeUiTextInspectionTarget.BattlePhaseStatus
                || target == RuntimeUiTextInspectionTarget.BattlePhaseStatusFull
                || target == RuntimeUiTextInspectionTarget.SettlementStatus;
        }

        private static bool IsHubNavigationTarget(
            RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.HubPrimaryHome
                || target == RuntimeUiTextInspectionTarget.HubPrimaryActivity
                || target == RuntimeUiTextInspectionTarget.HubPrimaryGrowth;
        }

        private static bool IsHubGrowthTabTarget(
            RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.HubGrowthEquipmentTab
                || target
                    == RuntimeUiTextInspectionTarget.HubGrowthCultivationTab;
        }

        private static bool IsAdaptiveBattleStatusTarget(
            RuntimeUiTextInspectionTarget target)
        {
            return target == RuntimeUiTextInspectionTarget.BattlePhaseStatus
                || target == RuntimeUiTextInspectionTarget.BattlePhaseStatusFull;
        }

        private static bool IsControlledNurseryStored(
            RuntimeUiTextInspectionCase inspection)
        {
            return inspection.Target == RuntimeUiTextInspectionTarget.BattleNurserySlot
                && inspection.CopyId == RuntimeUiCopyId.BattleNurseryPotStored;
        }

        private static bool IsControlledNurseryStars(
            RuntimeUiTextInspectionCase inspection)
        {
            return inspection.Target == RuntimeUiTextInspectionTarget.BattleNurseryStars;
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
                    return RuntimeUiArtSlot.IconResourceSunMicro;
                case RuntimeUiTextInspectionTarget.BattleCoreMetric:
                    return RuntimeUiArtSlot.IconResourceCoreMicro;
                case RuntimeUiTextInspectionTarget.BattleWaveMetric:
                    return RuntimeUiArtSlot.IconResourceWaveMicro;
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

        private static string MetricValue(RuntimeUiTextInspectionCase inspection)
        {
            if (!string.IsNullOrEmpty(inspection.MetricValue))
                return inspection.MetricValue;
            var target = inspection.Target;
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
                && Contains(bundle.Hub.Frame.SafeArea, bundle.Hub.Frame.Content)
                && Contains(bundle.Hub.Frame.SafeArea, bundle.Hub.TopBar)
                && Contains(bundle.Hub.TopBar, bundle.Hub.TopBarContent.Title)
                && Contains(bundle.Hub.TopBar,
                    bundle.Hub.TopBarContent.ResourceBalance)
                && Contains(bundle.Hub.Frame.SafeArea, bundle.Hub.PageSurface)
                && Contains(bundle.Hub.Frame.SafeArea, bundle.Hub.NavigationTray)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.HomePage.Orchard01Card)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.HomePage.Orchard02Card)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.HomePage.Orchard03Card)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.HomePage.GrowthPreview)
                && Contains(bundle.Hub.HomePage.GrowthPreview,
                    bundle.Hub.HomePage.StartButton)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.ActivityPage.Card)
                && Contains(bundle.Hub.ActivityPage.Card,
                    bundle.Hub.ActivityPage.RewardPanel)
                && Contains(bundle.Hub.ActivityPage.Card,
                    bundle.Hub.ActivityPage.Status)
                && Contains(bundle.Hub.ActivityPage.Card,
                    bundle.Hub.ActivityPage.PrimaryAction)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.GrowthPage.EntryCard)
                && Contains(bundle.Hub.PageSurface,
                    bundle.Hub.GrowthPage.DetailPanel)
                && Contains(bundle.Hub.GrowthPage.DetailPanel,
                    bundle.Hub.GrowthPage.Status)
                && Contains(bundle.Hub.GrowthPage.DetailPanel,
                    bundle.Hub.GrowthPage.EquipmentPrimaryAction)
                && Contains(bundle.Hub.GrowthPage.DetailPanel,
                    bundle.Hub.GrowthPage.CultivationPrimaryAction)
                && Contains(bundle.Settlement.Frame.SafeArea,
                    bundle.Settlement.Frame.Content),
                suffix + " route chrome remains inside the resolved safe area");

            ValidateHubAuthoritativeReferenceHitGeometry(bundle, suffix);
            ValidateHubVisualChildContainment(bundle, suffix);
            ValidateHubNavigationAnatomy(bundle, suffix);

            ValidateProjectedBattleGeometry(bundle, suffix);

            ValidateTouch(bundle.Bootstrap.RetryAction,
                bundle.BootstrapContext.Scale, suffix + "/bootstrap.retry");
            ValidateTouch(bundle.Hub.PrimaryNavigation.Home,
                bundle.HubContext.Scale, suffix + "/hub.nav.home");
            ValidateTouch(bundle.Hub.PrimaryNavigation.Activity,
                bundle.HubContext.Scale, suffix + "/hub.nav.activity");
            ValidateTouch(bundle.Hub.PrimaryNavigation.Growth,
                bundle.HubContext.Scale, suffix + "/hub.nav.growth");
            ValidateTouch(bundle.Hub.GrowthPage.Navigation.Equipment,
                bundle.HubContext.Scale, suffix + "/hub.growth.equipment");
            ValidateTouch(bundle.Hub.GrowthPage.Navigation.Cultivation,
                bundle.HubContext.Scale, suffix + "/hub.growth.cultivation");
            ValidateTouch(bundle.Hub.HomePage.StartButton,
                bundle.HubContext.Scale, suffix + "/hub.home.start");
            ValidateTouch(bundle.Hub.ActivityPage.PrimaryAction,
                bundle.HubContext.Scale, suffix + "/hub.activity.claim");
            ValidateTouch(bundle.Hub.GrowthPage.EntryCard,
                bundle.HubContext.Scale, suffix + "/hub.growth.entry");
            ValidateTouch(bundle.Hub.GrowthPage.EquipmentPrimaryAction,
                bundle.HubContext.Scale,
                suffix + "/hub.growth.equipment.primary-action");
            ValidateTouch(bundle.Hub.GrowthPage.CultivationPrimaryAction,
                bundle.HubContext.Scale,
                suffix + "/hub.growth.cultivation.primary-action");
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
                bundle.Hub.HomePage.Orchard01Card,
                bundle.Hub.HomePage.Orchard02Card,
                bundle.Hub.HomePage.Orchard03Card,
            };
            var lobbyThumbnails = new[]
            {
                RuntimeUiArtSlot.IllustrationLobbyOrchard01,
                RuntimeUiArtSlot.IllustrationLobbyOrchard02,
                RuntimeUiArtSlot.IllustrationLobbyOrchard03,
            };
            for (var index = 0; index < lobbyCards.Length; index++)
            {
                var card = PortraitHubLayout.CreateHomeLevelCard(
                    lobbyCards[index], bundle.HubContext.Scale);
                Assert(Contains(lobbyCards[index], card.Frame)
                    && Contains(lobbyCards[index], card.Title)
                    && Contains(lobbyCards[index], card.Body)
                    && Contains(lobbyCards[index], card.SelectedMarker)
                    && Contains(lobbyCards[index], card.TransientIndicator),
                    suffix + "/lobby.card-" + index + " anatomy is contained");
                Assert(card.Title.xMin - card.Frame.xMax
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumContentGap
                        * bundle.HubContext.Scale,
                    suffix + "/lobby.card-" + index + " illustration/copy gap");
                var visualGroupCenter = (card.Frame.xMin
                    + Mathf.Max(card.Title.xMax, card.Body.xMax)) * .5f;
                Assert(Mathf.Abs(visualGroupCenter - lobbyCards[index].center.x)
                        <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                            * bundle.HubContext.Scale
                            + RuntimeUiQualityProfile.GeometryTolerance,
                    suffix + "/lobby.card-" + index
                    + " illustration/copy group is optically centered");
                var thumbnailCenterDelta = card.Thumbnail.center
                    - card.Frame.center;
                Assert(Mathf.Abs(card.Thumbnail.width
                            - card.Frame.width)
                        <= RuntimeUiQualityProfile.GeometryTolerance
                    && Mathf.Abs(card.Thumbnail.height
                            - card.Frame.height)
                        <= RuntimeUiQualityProfile.GeometryTolerance
                    && Mathf.Abs(thumbnailCenterDelta.x)
                        <= RuntimeUiQualityProfile.GeometryTolerance
                    && Mathf.Abs(thumbnailCenterDelta.y)
                        <= RuntimeUiQualityProfile.GeometryTolerance,
                    suffix + "/lobby.card-" + index
                    + " fills the complete illustration frame; thumbnail="
                    + card.Thumbnail + " frame=" + card.Frame
                    + " frameScale=" + bundle.Hub.Frame.Scale
                    + " contextScale=" + bundle.HubContext.Scale
                    + " centerDelta=" + thumbnailCenterDelta);
                Assert(!Overlaps(card.Title, card.SelectedMarker)
                    && !Overlaps(card.Body, card.TransientIndicator),
                    suffix + "/lobby.card-" + index + " cues do not cover copy");
                ValidateTouch(lobbyCards[index], bundle.HubContext.Scale,
                    suffix + "/lobby.card-" + index);
                ValidateIllustrationOccupancy(card.Thumbnail, bundle.HubContext,
                    lobbyThumbnails[index], suffix + "/lobby.card-" + index
                    + ".thumbnail", true);
            }

            var equipmentOnlyActionPoint = new Vector2(
                bundle.Hub.GrowthPage.EquipmentPrimaryAction.xMin
                    + bundle.Hub.Frame.Scale,
                bundle.Hub.GrowthPage.EquipmentPrimaryAction.yMin
                    + bundle.Hub.Frame.Scale);
            var cultivationOnlyActionPoint = new Vector2(
                bundle.Hub.GrowthPage.CultivationPrimaryAction.center.x,
                bundle.Hub.GrowthPage.CultivationPrimaryAction.yMax
                    - bundle.Hub.Frame.Scale);
            Assert(PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.PrimaryNavigation.Home.center,
                        HubPageId.Activity, false) == HubHitTarget.Home
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.PrimaryNavigation.Activity.center,
                        HubPageId.Home, false) == HubHitTarget.Activity
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.PrimaryNavigation.Growth.center,
                        HubPageId.Home, false) == HubHitTarget.Growth
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.HomePage.Orchard01Card.center,
                        HubPageId.Home, false) == HubHitTarget.LevelOrchard01
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.HomePage.Orchard02Card.center,
                        HubPageId.Home, false) == HubHitTarget.LevelOrchard02
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.HomePage.Orchard03Card.center,
                        HubPageId.Home, false) == HubHitTarget.LevelOrchard03
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.HomePage.StartButton.center,
                        HubPageId.Home, false) == HubHitTarget.Start
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.GrowthPage.Navigation.Equipment.center,
                        HubPageId.Growth, false) == HubHitTarget.Equipment
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.GrowthPage.Navigation.Cultivation.center,
                        HubPageId.Growth, false) == HubHitTarget.Cultivation
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.ActivityPage.Card.center,
                        HubPageId.Activity, false) == HubHitTarget.None
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.ActivityPage.PrimaryAction.center,
                        HubPageId.Activity, false) == HubHitTarget.ActivityClaim
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.GrowthPage.EntryCard.center,
                        HubPageId.Growth, false, GrowthPageId.Equipment)
                    == HubHitTarget.EquipmentEntry
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.GrowthPage.EntryCard.center,
                        HubPageId.Growth, false, GrowthPageId.Cultivation)
                    == HubHitTarget.CultivationEntry
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.GrowthPage.EquipmentPrimaryAction.center,
                        HubPageId.Growth, false, GrowthPageId.Equipment)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.GrowthPage.CultivationPrimaryAction.center,
                        HubPageId.Growth, false, GrowthPageId.Cultivation)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(bundle.Hub,
                        equipmentOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Equipment)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(bundle.Hub,
                        equipmentOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Cultivation) == HubHitTarget.None
                && PortraitHubLayout.HitTest(bundle.Hub,
                        cultivationOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Cultivation)
                    == HubHitTarget.GrowthPrimaryAction
                && PortraitHubLayout.HitTest(bundle.Hub,
                        cultivationOnlyActionPoint, HubPageId.Growth, false,
                        GrowthPageId.Equipment) == HubHitTarget.None
                && PortraitHubLayout.HitTest(bundle.Hub,
                        bundle.Hub.HomePage.StartButton.center,
                        HubPageId.Home, true) == HubHitTarget.None,
                suffix + " Hub draw/hit parity covers navigation, Home, Activity, Growth and transition states");
            Assert(PortraitShellLayout.HitTest(bundle.Settlement,
                    bundle.Settlement.RetryButton.center, false) == ShellHitTarget.Retry
                && PortraitShellLayout.HitTest(bundle.Settlement,
                    bundle.Settlement.ReturnButton.center, false) == ShellHitTarget.Return,
                suffix + " Settlement draw and hit rects share one layout authority");

            var minimumNineSliceDestinations = new[]
            {
                bundle.Bootstrap.Modal,
                bundle.Hub.TopBar,
                bundle.Hub.TopBarContent.ResourceBalance,
                bundle.Hub.PrimaryNavigation.Home,
                bundle.Hub.PrimaryNavigation.Activity,
                bundle.Hub.PrimaryNavigation.Growth,
                bundle.Hub.ActivityPage.Card,
                bundle.Hub.ActivityPage.RewardPanel,
                bundle.Hub.ActivityPage.PrimaryAction,
                bundle.Hub.GrowthPage.EntryCard,
                bundle.Hub.GrowthPage.DetailPanel,
                bundle.Hub.GrowthPage.EquipmentPrimaryAction,
                bundle.Hub.GrowthPage.CultivationPrimaryAction,
                bundle.Hub.HomePage.Orchard01Card,
                bundle.Hub.HomePage.Orchard02Card,
                bundle.Hub.HomePage.Orchard03Card,
                bundle.Hub.HomePage.StartButton,
                bundle.Settlement.ResultCard,
                bundle.Settlement.RetryButton,
                bundle.Settlement.ReturnButton,
                bundle.Battle.Header,
                bundle.Battle.PageShell,
                bundle.Battle.BattleStage,
                bundle.Battle.ContextTray,
                bundle.Battle.NurseryTray,
                bundle.Battle.Modal,
                bundle.Battle.TerminalModal,
            };
            var minimumNineSliceScales = new[]
            {
                bundle.BootstrapContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.HubContext.Scale,
                bundle.SettlementContext.Scale,
                bundle.SettlementContext.Scale,
                bundle.SettlementContext.Scale,
                1f, 1f, 1f, 1f, 1f, 1f, 1f,
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
            var lobbyOccupiedCenter = (bundle.Hub.TopBar.yMin
                + bundle.Hub.NavigationTray.yMax) * .5f;
            var settlementOccupiedCenter = (bundle.Settlement.Title.yMin
                + bundle.Settlement.ReturnButton.yMax) * .5f;
            Assert(Mathf.Abs(lobbyOccupiedCenter
                        - bundle.Hub.Frame.SafeArea.center.y)
                    <= RuntimeUiQualityProfile.OccupiedContentCenterTolerance
                        * Mathf.Max(1f, bundle.Hub.Frame.Scale)
                        + RuntimeUiQualityProfile.GeometryTolerance
                && bundle.Hub.PageSurface.yMax
                    - bundle.Hub.HomePage.GrowthPreview.yMax
                    <= RuntimeUiQualityProfile.OccupiedContentBottomGapMaximum
                        * Mathf.Max(1f, bundle.Hub.Frame.Scale)
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

            Assert(bundle.Battle.ContextTrayTitle.yMin
                    >= bundle.Battle.ContextTray.yMin
                    + RuntimeUiQualityProfile.MinimumTextToBorderGap
                && bundle.Battle.Tool(0).yMin
                    - bundle.Battle.ContextTrayTitle.yMax
                    >= RuntimeUiQualityProfile.MinimumTextToBorderGap,
                suffix + " Battle context title clears the panel top edge");
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
            var settlementBannerDraw = RuntimeUiGui.ResolveOpticalEnvelopeDrawRect(
                bundle.SettlementContext, RuntimeUiArtSlot.OrnamentResultBanner,
                bundle.Settlement.ResultBanner);
            var settlementBannerVisual = RuntimeUiGui.ResolveOpticalVisualRect(
                bundle.SettlementContext, RuntimeUiArtSlot.OrnamentResultBanner,
                settlementBannerDraw);
            var settlementOutcomeText = RuntimeUiGui.ResolveSingleLineTextRect(
                bundle.SettlementContext, bundle.Settlement.Outcome,
                RuntimeUiTypographyRole.Display, TextAnchor.MiddleCenter,
                RuntimeUiInteractionState.Success);
            Assert(Contains(bundle.Settlement.ResultCard, settlementBannerDraw)
                && Contains(bundle.Settlement.ResultBanner, settlementBannerVisual)
                && Contains(settlementBannerVisual, bundle.Settlement.ResultBanner)
                && Contains(settlementBannerVisual, settlementOutcomeText),
                suffix + " Settlement outcome typography is contained by the banner's actual optical pixels");
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
            var battlefield = bundle.Battle.Battlefield;
            Assert(Approximately(battlefield.MapViewportRect,
                    battlefield.BoardRect),
                suffix + " Battle map viewport is exactly the gameplay-stage board");
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

        private static void ValidateHubNavigationAnatomy(
            LayoutBundle bundle, string suffix)
        {
            Assert(RuntimeUiQualityProfile.HubNavigationChromeSilhouetteCount == 2
                && RuntimeUiQualityProfile.HubNavigationIconSubjectCount == 1
                && bundle.HubContext.ArtSet.TryGetBinding(
                    RuntimeUiArtSlot.SurfaceHubNavigationBase, out var baseBinding)
                && baseBinding.Geometry == RuntimeUiArtGeometry.Stretch
                && baseBinding.Sprite != null
                && bundle.HubContext.ArtSet.TryGetBinding(
                    RuntimeUiArtSlot.SurfaceHubNavigationSelectedTab,
                    out var selectedTabBinding)
                && selectedTabBinding.Geometry == RuntimeUiArtGeometry.Stretch
                && selectedTabBinding.Sprite != null
                && !ReferenceEquals(baseBinding.Sprite, selectedTabBinding.Sprite),
                suffix + "/hub.nav-chrome owns exactly one base and one selected-tab silhouette");
            var rects = new[]
            {
                bundle.Hub.PrimaryNavigation.Home,
                bundle.Hub.PrimaryNavigation.Activity,
                bundle.Hub.PrimaryNavigation.Growth,
            };
            var slots = new[]
            {
                RuntimeUiArtSlot.IconHubHome,
                RuntimeUiArtSlot.IconHubActivity,
                RuntimeUiArtSlot.IconHubGrowth,
            };
            var sprites = new HashSet<Sprite>();
            for (var index = 0; index < rects.Length; index++)
            {
                var anatomy = RuntimeUiGui.ResolveHubNavigationItemLayout(
                    bundle.HubContext, rects[index], index == 0,
                    RuntimeUiInteractionState.Normal);
                Assert(Approximately(anatomy.HitRect, rects[index])
                    && Contains(rects[index], anatomy.Icon)
                    && Contains(rects[index], anatomy.Label)
                    && Contains(rects[index], anatomy.Underline)
                    && Contains(bundle.Hub.Frame.SafeArea, anatomy.Surface)
                    && anatomy.Icon.width >= RuntimeUiQualityProfile
                        .HubNavigationIconReviewSizeMinimum
                        * bundle.HubContext.Scale
                    && anatomy.Icon.width <= RuntimeUiQualityProfile
                        .HubNavigationIconReviewSizeMaximum
                        * bundle.HubContext.Scale + .01f
                    && anatomy.Label.height
                        >= bundle.HubContext.ScaledLineHeight(
                            RuntimeUiTypographyRole.ControlLabel)
                    && anatomy.Underline.height >= 4f * bundle.HubContext.Scale,
                    suffix + "/hub.nav-" + index
                    + " keeps one unchanged hit rect with icon, label, lifted surface and underline anatomy");
                Assert(bundle.HubContext.ArtSet.TryGetBinding(slots[index],
                        out var binding)
                    && binding.Geometry == RuntimeUiArtGeometry.Icon
                    && binding.Sprite != null && sprites.Add(binding.Sprite),
                    suffix + "/hub.nav-" + index
                    + " binds one distinct formal semantic icon");

                foreach (RuntimeUiInteractionState state in Enum.GetValues(
                             typeof(RuntimeUiInteractionState)))
                {
                    var stateAnatomy = RuntimeUiGui.ResolveHubNavigationItemLayout(
                        bundle.HubContext, rects[index], index == 0, state);
                    var labelText = RuntimeUiGui.ResolveSingleLineTextRect(
                        bundle.HubContext, stateAnatomy.Label,
                        RuntimeUiTypographyRole.ControlLabel,
                        TextAnchor.MiddleCenter, state);
                    Assert(Approximately(stateAnatomy.HitRect, rects[index])
                        && Contains(rects[index], stateAnatomy.Icon)
                        && Contains(rects[index], stateAnatomy.Label)
                        && Contains(rects[index], stateAnatomy.Underline)
                        && ContainsTextPixelRounded(stateAnatomy.Label, labelText),
                        suffix + "/hub.nav-" + index + "/" + state
                        + " keeps draw anatomy inside the unchanged hit owner");
                }
            }

            var growthTabs = new[]
            {
                bundle.Hub.GrowthPage.Navigation.Equipment,
                bundle.Hub.GrowthPage.Navigation.Cultivation,
            };
            for (var index = 0; index < growthTabs.Length; index++)
            {
                foreach (RuntimeUiInteractionState state in Enum.GetValues(
                             typeof(RuntimeUiInteractionState)))
                {
                    var tab = RuntimeUiGui.ResolveHubGrowthTabLayout(
                        bundle.HubContext, growthTabs[index], state);
                    var tabText = RuntimeUiGui.ResolveSingleLineTextRect(
                        bundle.HubContext, tab.Label,
                        RuntimeUiTypographyRole.ControlLabel,
                        TextAnchor.MiddleCenter, state);
                    Assert(Approximately(tab.HitRect, growthTabs[index])
                        && Contains(growthTabs[index], tab.Surface)
                        && Contains(growthTabs[index], tab.Label)
                        && Contains(growthTabs[index], tab.Underline)
                        && ContainsTextPixelRounded(tab.Label, tabText)
                        && tab.Underline.height >= 4f * bundle.HubContext.Scale,
                        suffix + "/hub.growth-tab-" + index + "/" + state
                        + " keeps label and selection anatomy inside the unchanged 44-point hit owner");
                }
            }

            foreach (var state in new[]
                     {
                         RuntimeUiInteractionState.Disabled,
                         RuntimeUiInteractionState.Loading,
                         RuntimeUiInteractionState.Error,
                     })
            {
                var preview = RuntimeUiGui.ResolveHubHomeGrowthPreviewLayout(
                    bundle.HubContext, bundle.Hub.HomePage.GrowthPreview, state);
                var activityTitle = RuntimeUiGui.ResolveSingleLineTextRect(
                    bundle.HubContext, bundle.Hub.ActivityPage.Title,
                    RuntimeUiTypographyRole.SectionTitle,
                    TextAnchor.MiddleLeft, state);
                var activityBody = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                    bundle.HubContext, bundle.Hub.ActivityPage.Description,
                    RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft, state);
                var growthTitle = RuntimeUiGui.ResolveSingleLineTextRect(
                    bundle.HubContext, bundle.Hub.GrowthPage.DetailTitle,
                    RuntimeUiTypographyRole.SectionTitle,
                    TextAnchor.MiddleLeft, state);
                var growthBody = RuntimeUiGui.ResolveControlledTwoLineTextLayout(
                    bundle.HubContext, bundle.Hub.GrowthPage.Description,
                    RuntimeUiTypographyRole.Body,
                    TextAnchor.MiddleLeft, state);
                Assert(Contains(bundle.Hub.HomePage.GrowthPreview, preview.Title)
                    && Contains(bundle.Hub.HomePage.GrowthPreview, preview.Body)
                    && ContainsTextPixelRounded(bundle.Hub.ActivityPage.Title,
                        activityTitle)
                    && ContainsTextPixelRounded(bundle.Hub.ActivityPage.Description,
                        activityBody.FirstLineRect)
                    && ContainsTextPixelRounded(bundle.Hub.ActivityPage.Description,
                        activityBody.SecondLineRect)
                    && ContainsTextPixelRounded(bundle.Hub.GrowthPage.DetailTitle,
                        growthTitle)
                    && ContainsTextPixelRounded(bundle.Hub.GrowthPage.Description,
                        growthBody.FirstLineRect)
                    && ContainsTextPixelRounded(bundle.Hub.GrowthPage.Description,
                        growthBody.SecondLineRect),
                    suffix + "/hub.finite-panels/" + state
                    + " contains title/body anatomy for every runtime-used state");
            }

            if (Mathf.Approximately(bundle.Viewport.Width, 402f)
                && Mathf.Approximately(bundle.Viewport.Height, 874f)
                && Mathf.Approximately(bundle.Hub.Frame.Scale, 1f))
            {
                var reference = RuntimeUiGui.ResolveHubNavigationItemLayout(
                    bundle.HubContext, bundle.Hub.PrimaryNavigation.Home,
                    true, RuntimeUiInteractionState.Normal);
                Assert(Mathf.Abs(reference.Icon.center.y - 816f) <= 3f
                    && Mathf.Abs(reference.Label.center.y - 849f) <= 3f
                    && Mathf.Abs(reference.Underline.y - 863f) <= 3f
                    && reference.Underline.height >= 4f
                    && reference.Underline.height <= 6f,
                    suffix
                    + " 402x874 bottom navigation matches icon/Chinese-label/selection anchor tolerances");
            }
        }

        private static void ValidateHubAuthoritativeReferenceHitGeometry(
            LayoutBundle bundle, string suffix)
        {
            var referenceViewport = new Rect(0f, 0f, 402f, 874f);
            if (!Mathf.Approximately(bundle.Viewport.Width,
                    referenceViewport.width)
                || !Mathf.Approximately(bundle.Viewport.Height,
                    referenceViewport.height)
                || !ReferenceRectEquals(bundle.SafeArea, referenceViewport))
                return;

            var actual = new[]
            {
                bundle.Hub.PrimaryNavigation.Home,
                bundle.Hub.PrimaryNavigation.Activity,
                bundle.Hub.PrimaryNavigation.Growth,
                bundle.Hub.HomePage.Orchard01Card,
                bundle.Hub.HomePage.Orchard02Card,
                bundle.Hub.HomePage.Orchard03Card,
                bundle.Hub.HomePage.StartButton,
                bundle.Hub.ActivityPage.PrimaryAction,
            };
            var expected = new[]
            {
                new Rect(16f, 794f, 118f, 80f),
                new Rect(142f, 794f, 118f, 80f),
                new Rect(268f, 794f, 118f, 80f),
                new Rect(28f, 122f, 350f, 132f),
                new Rect(27f, 267f, 351f, 124f),
                new Rect(27f, 404f, 351f, 124f),
                new Rect(57f, 700f, 289f, 56f),
                new Rect(66f, 641f, 270f, 57f),
            };
            var names = new[]
            {
                "nav.home",
                "nav.activity",
                "nav.growth",
                "home.orchard-01",
                "home.orchard-02",
                "home.orchard-03",
                "home.start",
                "activity.claim",
            };
            for (var index = 0; index < actual.Length; index++)
            {
                Assert(ReferenceRectEquals(actual[index], expected[index]),
                    suffix + "/hub.reference-hit/" + names[index]
                    + " remains frozen at " + expected[index]
                    + "; actual=" + actual[index]);
            }

            var navigationTargets = new[]
            {
                HubHitTarget.Home,
                HubHitTarget.Activity,
                HubHitTarget.Growth,
            };
            for (var index = 0; index < navigationTargets.Length; index++)
            {
                var anatomy = RuntimeUiGui.ResolveHubNavigationItemLayout(
                    bundle.HubContext, actual[index], index == 0,
                    RuntimeUiInteractionState.Normal);
                Assert(ReferenceRectEquals(anatomy.HitRect, expected[index])
                    && PortraitHubLayout.HitTest(bundle.Hub,
                        actual[index].center, HubPageId.Home, false)
                        == navigationTargets[index],
                    suffix + "/hub.reference-hit/" + names[index]
                    + " keeps draw/hit ownership on the frozen rectangle");
            }

            var homeTargets = new[]
            {
                HubHitTarget.LevelOrchard01,
                HubHitTarget.LevelOrchard02,
                HubHitTarget.LevelOrchard03,
                HubHitTarget.Start,
            };
            for (var index = 0; index < homeTargets.Length; index++)
            {
                var actualIndex = index + 3;
                Assert(PortraitHubLayout.HitTest(bundle.Hub,
                           actual[actualIndex].center, HubPageId.Home, false)
                       == homeTargets[index],
                    suffix + "/hub.reference-hit/" + names[actualIndex]
                    + " resolves from the frozen Home owner");
            }

            Assert(PortraitHubLayout.HitTest(bundle.Hub,
                       bundle.Hub.ActivityPage.PrimaryAction.center,
                       HubPageId.Activity, false)
                   == HubHitTarget.ActivityClaim,
                suffix
                + "/hub.reference-hit/activity.claim resolves from the frozen Activity owner");
        }

        private static void ValidateHubVisualChildContainment(
            LayoutBundle bundle, string suffix)
        {
            // Add richer Home/Activity visual-only anatomy to these lists as it
            // lands. Children are intentionally containment-checked, not frozen;
            // only the interactive owners above carry reference coordinates.
            var preview = RuntimeUiGui.ResolveHubHomeGrowthPreviewLayout(
                bundle.HubContext, bundle.Hub.HomePage.GrowthPreview,
                RuntimeUiInteractionState.Normal);
            AssertVisualChildrenContained(bundle.Hub.HomePage.GrowthPreview,
                suffix + "/hub.home.preview-visuals",
                preview.Surface, preview.Ribbon, preview.Icon, preview.Title,
                preview.Body, preview.Divider, preview.StateIndicator);

            AssertVisualChildrenContained(bundle.Hub.ActivityPage.Card,
                suffix + "/hub.activity.visuals",
                bundle.Hub.ActivityPage.Title,
                bundle.Hub.ActivityPage.Description,
                bundle.Hub.ActivityPage.RewardPanel,
                bundle.Hub.ActivityPage.Status,
                bundle.Hub.ActivityPage.Illustration,
                bundle.Hub.ActivityPage.StateIndicator);
            AssertVisualChildrenContained(bundle.Hub.ActivityPage.RewardPanel,
                suffix + "/hub.activity.reward-visuals",
                bundle.Hub.ActivityPage.RewardTitle,
                bundle.Hub.ActivityPage.RewardEquipment,
                bundle.Hub.ActivityPage.RewardItem);
        }

        private static void AssertVisualChildrenContained(Rect owner,
            string caseName, params Rect[] children)
        {
            Assert(owner.width > 0f && owner.height > 0f && children != null
                && children.Length > 0,
                caseName + " has one finite visual owner and child list");
            for (var index = 0; index < children.Length; index++)
            {
                var child = children[index];
                Assert(child.width > 0f && child.height > 0f
                    && Contains(owner, child),
                    caseName + "/child-" + index
                    + " remains contained without becoming hit geometry; owner="
                    + owner + " child=" + child);
            }
        }

        private static bool IsBattleTarget(RuntimeUiTextInspectionTarget target)
        {
            return target >= RuntimeUiTextInspectionTarget.BattleHeaderTitle
                && target <= RuntimeUiTextInspectionTarget.BattleModalTerminalAction;
        }

        private static void ValidateProjectedBattleGeometry(
            LayoutBundle bundle, string suffix)
        {
            var projection = bundle.BattleViewport;
            var rawProjectedDesign = projection.ProjectDesignRect(
                bundle.Battle.Design);
            var projectedDesign = ProjectBattleRect(bundle, bundle.Battle.Design);
            Assert(Approximately(rawProjectedDesign, projection.DesignViewportRect)
                && Contains(projection.SafeAreaInGuiSpace, rawProjectedDesign)
                && Mathf.Abs(bundle.ProjectedBattleContext.Scale - projection.Scale)
                    <= .00051f,
                suffix + " Battle uses the calculated full/inset viewport projection");

            var header = SnapDeviceRect(ProjectBattleRect(bundle, bundle.Battle.Header));
            var pageShell = SnapDeviceRect(ProjectBattleRect(
                bundle, bundle.Battle.PageShell));
            var stage = SnapDeviceRect(ProjectBattleRect(
                bundle, bundle.Battle.BattleStage));
            Assert(Mathf.Approximately(header.xMin, pageShell.xMin)
                && Mathf.Approximately(header.xMax, pageShell.xMax)
                && Contains(pageShell, stage),
                suffix + " Battle header and PageShell share peer edges around the inset stage");

            var controlStack = new[]
            {
                bundle.Battle.PhaseWaveRow,
                bundle.Battle.ContextTray,
                bundle.Battle.NurseryTray,
                bundle.Battle.RefreshAction,
            };
            for (var index = 0; index < controlStack.Length; index++)
            {
                var projected = SnapDeviceRect(
                    ProjectBattleRect(bundle, controlStack[index]));
                Assert(Contains(pageShell, projected),
                    suffix + " Battle projected control track remains inside PageShell: "
                    + index);
            }

            Assert(Approximately(ProjectBattleRect(bundle,
                    bundle.Battle.Battlefield.BoardRect),
                    ProjectBattleRect(bundle, bundle.Battle.Board))
                && Approximately(ProjectBattleRect(bundle, bundle.Battle.Board),
                    ProjectBattleRect(bundle, bundle.Battle.BattleStage))
                && Contains(ProjectBattleRect(bundle, bundle.Battle.PhaseWaveRow),
                    ProjectBattleRect(bundle, bundle.Battle.WaveAction)),
                suffix + " Battle draw and hit geometry use the same projection");

            var stageHeightFraction = bundle.Battle.BattleStage.height
                / BattleUiLayout.DesignHeight;
            Assert(stageHeightFraction >= .38f && stageHeightFraction <= .43f
                && !Overlaps(bundle.Battle.BattleStage,
                    bundle.Battle.PhaseWaveRow)
                && !Overlaps(bundle.Battle.PhaseWaveRow,
                    bundle.Battle.ContextTray),
                suffix + " Battle stage and independent phase row preserve the six-track contract");

            var expectedTopGap = Mathf.Round(BattleUiLayout.SpacingUnit
                * projection.Scale);
            var actualTopGap = pageShell.yMin - header.yMax;
            Assert(actualTopGap >= Mathf.Max(0f, expectedTopGap - 2f)
                    && actualTopGap <= expectedTopGap + 1f,
                suffix + " Battle snapped top-level gap preserves the four-point rhythm");

            foreach (var inspection in RuntimeUiTextInspectionCatalog.Cases)
            {
                if (!IsBattleTarget(inspection.Target)) continue;
                var owner = SnapDeviceRect(ResolveComponentRect(bundle, inspection));
                Assert(Contains(SnapDeviceRect(projectedDesign), owner),
                    inspection.Id + "@" + suffix
                    + " projected owner remains inside the complete design inset");
            }

            ValidateTouch(ProjectBattleRect(bundle, bundle.Battle.WaveAction),
                projection.Scale, suffix + "/battle.wave-action.projected");
            ValidateTouch(ProjectBattleRect(bundle, bundle.Battle.RefreshAction),
                projection.Scale, suffix + "/battle.refresh.projected");
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
                RuntimeUiArtSlot.IconResourceSunMicro,
                RuntimeUiArtSlot.IconResourceCoreMicro,
                RuntimeUiArtSlot.IconResourceWaveMicro,
            };
            var style = bundle.BattleContext.Styles.SingleLineText(
                RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft);
            var firstCenter = 0f;
            for (var index = 0; index < rects.Length; index++)
            {
                var layout = RuntimeUiGui.ResolveCompactInlineMetricContentLayout(
                    bundle.BattleContext, rects[index], icons[index],
                    labels[index], values[index],
                    compactIconSize: BattleUiLayout.HeaderMetricIconSize,
                    reserveSurfaceInset: true);
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
                        + RuntimeUiQualityProfile.GeometryTolerance
                        >= RuntimeUiQualityProfile.MinimumIconTextGap
                    && layout.ValueRect.xMin - layout.LabelRect.xMax
                        <= RuntimeUiQualityProfile.MaximumIconTextGap
                            + RuntimeUiQualityProfile.GeometryTolerance
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
                    + " has finite compact icon/label/value anatomy; owner=" + rects[index]
                    + " icon=" + layout.IconRect + " visual=" + layout.IconVisualRect
                    + " label=" + layout.LabelRect + " value=" + layout.ValueRect
                    + " group=" + layout.GroupRect);
                AssertSingleLineFits(style, labels[index], layout.LabelRect,
                    suffix + "/battle.metric-" + index + ".label");
                AssertSingleLineFits(style, values[index], layout.ValueRect,
                    suffix + "/battle.metric-" + index + ".value");
                if (index == 0) firstCenter = layout.IconVisualRect.center.y;
                else Assert(Mathf.Abs(layout.IconVisualRect.center.y - firstCenter)
                        <= RuntimeUiQualityProfile.RepeatedCenterTolerance,
                    suffix + " Battle compact metric icon centers align");
            }

            var speed = RuntimeUiGui.ResolveCompactControlLayout(
                bundle.Battle.SpeedAction, RuntimeUiInteractionState.Normal,
                true, bundle.BattleContext.Theme.Feedback);
            Assert(speed.UsesMultiplierText
                && Contains(bundle.Battle.SpeedAction, speed.SurfaceRect)
                && Contains(bundle.Battle.SpeedAction, speed.ContentRect)
                && Mathf.Abs(speed.ContentRect.center.x
                    - bundle.Battle.SpeedAction.center.x)
                    <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical
                && Mathf.Abs(speed.ContentRect.center.y
                    - bundle.Battle.SpeedAction.center.y)
                    <= RuntimeUiQualityProfile.OpticalCenterToleranceLogical,
                suffix + " Battle speed multiplier owns one centered compact-control content rect");
            AssertSingleLineFits(bundle.BattleContext.Styles.SingleLineText(
                    RuntimeUiTypographyRole.Metric, TextAnchor.MiddleCenter),
                "2×", speed.ContentRect,
                suffix + "/battle.speed-multiplier");
        }

        private static void ValidateRepeatedBaselines(LayoutBundle bundle, string suffix)
        {
            var cards = new[]
            {
                PortraitHubLayout.CreateHomeLevelCard(
                    bundle.Hub.HomePage.Orchard01Card,
                    bundle.HubContext.Scale),
                PortraitHubLayout.CreateHomeLevelCard(
                    bundle.Hub.HomePage.Orchard02Card,
                    bundle.HubContext.Scale),
                PortraitHubLayout.CreateHomeLevelCard(
                    bundle.Hub.HomePage.Orchard03Card,
                    bundle.HubContext.Scale),
            };
            var titleBaseline = LocalLineBoxBaseline(
                cards[0].Title, bundle.Hub.HomePage.Orchard01Card,
                bundle.HubContext.Styles.SingleLineText(
                    RuntimeUiTypographyRole.ControlLabel, TextAnchor.MiddleLeft));
            var bodyBaseline = LocalLineBoxBaseline(
                cards[0].Body, bundle.Hub.HomePage.Orchard01Card,
                bundle.HubContext.Styles.SingleLineText(
                    RuntimeUiTypographyRole.Supplemental, TextAnchor.MiddleLeft));
            for (var index = 1; index < cards.Length; index++)
            {
                var owner = index == 1
                    ? bundle.Hub.HomePage.Orchard02Card
                    : bundle.Hub.HomePage.Orchard03Card;
                Assert(Mathf.Abs(LocalLineBoxBaseline(cards[index].Title, owner,
                            bundle.HubContext.Styles.SingleLineText(
                                RuntimeUiTypographyRole.ControlLabel,
                                TextAnchor.MiddleLeft)) - titleBaseline)
                        <= RuntimeUiQualityProfile.BaselineTolerance,
                    suffix + " Lobby title baselines repeat within tolerance");
                Assert(Mathf.Abs(LocalLineBoxBaseline(cards[index].Body, owner,
                            bundle.HubContext.Styles.SingleLineText(
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
            var fillsDestination = true;
            if (lobbyThumbnail)
            {
                fillsDestination = (destination.width - fittedWidth) * .5f
                        <= RuntimeUiQualityProfile.IllustrationUnusedBarMaximum
                            * context.Scale + RuntimeUiQualityProfile.GeometryTolerance
                    && (destination.height - fittedHeight) * .5f
                        <= RuntimeUiQualityProfile.IllustrationUnusedBarMaximum
                            * context.Scale + RuntimeUiQualityProfile.GeometryTolerance;
            }
            else
            {
                var sourceWidth = binding.Texture.width;
                var sourceHeight = binding.Texture.height;
                var coverScale = Mathf.Max(destination.width / sourceWidth,
                    destination.height / sourceHeight);
                var cropX = (sourceWidth * coverScale - destination.width) * .5f;
                var cropY = (sourceHeight * coverScale - destination.height) * .5f;
                fillsDestination = cropX <= RuntimeUiQualityProfile.ResultVistaCropMaximum
                            * context.Scale + RuntimeUiQualityProfile.GeometryTolerance
                    && cropY <= RuntimeUiQualityProfile.ResultVistaCropMaximum
                            * context.Scale + RuntimeUiQualityProfile.GeometryTolerance;
            }
            Assert(destination.width + RuntimeUiQualityProfile.GeometryTolerance
                    >= minimumWidth
                && destination.height + RuntimeUiQualityProfile.GeometryTolerance
                    >= minimumHeight
                && fillsDestination,
                caseName + " fills its destination without bars or excessive crop");
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
            Assert(theme.Colors.Outline.r > theme.Colors.Outline.g
                    && theme.Colors.Outline.g > theme.Colors.Outline.b
                    && theme.Colors.Outline.r >= 70f / 255f
                    && Contrast(theme.Colors.Outline, theme.Colors.InverseText) + .001f
                        >= RuntimeUiQualityProfile.NormalTextContrast,
                "the shared outline token is visibly warm soil-brown rather than pure or near black");
            var primary = theme.ResolveActionStyle(RuntimeUiActionKind.Primary,
                RuntimeUiActionContentForm.IconLabel,
                RuntimeUiActionBehavior.Instantaneous,
                RuntimeUiInteractionState.Normal, false);
            Assert(Contrast(primary.ContentColor, primary.ContainerColor) + .001f
                    >= RuntimeUiQualityProfile.NormalTextContrast,
                "primary action label and glyph share the 4.5:1 content pairing");

            var pressed = theme.ResolveActionStyle(RuntimeUiActionKind.Primary,
                RuntimeUiActionContentForm.IconLabel,
                RuntimeUiActionBehavior.Instantaneous,
                RuntimeUiInteractionState.Pressed, false);
            Assert(Contrast(pressed.ContentColor, pressed.ContainerColor) + .001f
                    >= RuntimeUiQualityProfile.NormalTextContrast,
                "pressed action resolves a complete contrast-safe pairing");
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
            var disabled = theme.ResolveActionStyle(RuntimeUiActionKind.Primary,
                RuntimeUiActionContentForm.IconLabel,
                RuntimeUiActionBehavior.Instantaneous,
                RuntimeUiInteractionState.Disabled, false);
            Assert(disabled.Disabled
                && Contrast(disabled.ContentColor, disabled.ContainerColor) + .001f
                    >= RuntimeUiQualityProfile.LargeOrBoldTextContrast,
                "disabled action owns the approved muted 3:1 pairing and an independent non-color cue");

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
            var runtimeGui = RuntimeUiSourceAuthority.ReadRuntimeGui();
            Assert(runtimeGui.Contains("ResolveActionContentLayout(")
                && runtimeGui.Contains("ResolveMetricContentLayout(")
                && runtimeGui.Contains("ResolveOpticalEnvelopeDrawRect(")
                && runtimeGui.Contains("TryResolveStateIndicatorRect(")
                && runtimeGui.Contains("IconVisualRect")
                && runtimeGui.Contains("GroupRect")
                && !runtimeGui.Contains("centerIconAndLabel"),
                "shared draw code exposes the same component anatomy used by quality tests");

            var lobby = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Shell/LobbyHubPresenter.cs"));
            var settlement = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Shell/SettlementPresenter.cs"));
            var bootstrap = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/App/AppFlowCoordinator.cs"));
            var battle = RuntimeUiSourceAuthority.ReadFruitDefenseGame();
            var acceptance = RuntimeUiSourceAuthority.ReadAcceptanceRunner();
            var hubPageDispatch = lobby.IndexOf(
                "switch (_hubRouter.CurrentPage)",
                StringComparison.Ordinal);
            var growthPageDraw = hubPageDispatch < 0 ? -1 : lobby.IndexOf(
                "DrawGrowthPage(layout.GrowthPage", hubPageDispatch,
                StringComparison.Ordinal);
            var bottomNavigationDraw = growthPageDraw < 0 ? -1
                : lobby.IndexOf("RuntimeUiGui.DrawHubNavigationTray",
                    growthPageDraw, StringComparison.Ordinal);
            Assert(lobby.Contains("RuntimeUiCopyCatalog")
                && settlement.Contains("RuntimeUiCopyCatalog")
                && bootstrap.Contains("RuntimeUiCopyCatalog")
                && battle.Contains("RuntimeUiCopyCatalog"),
                "all four routes consume the finite product-copy authority");
            Assert(hubPageDispatch >= 0
                && growthPageDraw > hubPageDispatch
                && bottomNavigationDraw > growthPageDraw,
                "Lobby draws the shared bottom navigation after every finite Hub page, including cultivation locked");
            Assert(runtimeGui.Contains("public static void DrawBlockingModal(")
                && runtimeGui.Contains("public static void DrawResultCard(")
                && !MethodBodyContains(runtimeGui,
                    "public static void DrawBlockingModal(", "DrawStateIndicator(")
                && !MethodBodyContains(runtimeGui,
                    "public static void DrawResultCard(", "DrawStateIndicator("),
                "modal/result surfaces do not auto-own a duplicate state badge");
            Assert(MethodBodyContains(runtimeGui,
                    "public static void DrawResultBanner(",
                    "DrawOpticalEnvelopeStretchSlotArt(")
                && runtimeGui.Contains("public static void DrawEmphasisText(")
                && runtimeGui.Contains("public static RuntimeUiEmphasisTextLayout ResolveEmphasisTextLayout(")
                && MethodBodyContains(runtimeGui,
                    "public static void DrawEmphasisText(",
                    "RequireOpaqueEmphasisComposition(")
                && !runtimeGui.Contains("ComposeEmphasisAlpha(")
                && CountOccurrences(settlement,
                    "RuntimeUiGui.DrawEmphasisText(_drawContext, outcomeRect") == 1
                && settlement.Contains("SettlementOutcomeRevealPhase.Hidden")
                && settlement.Contains(
                    "#if FRUIT_DEFENSE_ACCEPTANCE && UNITY_WEBGL && !UNITY_EDITOR")
                && settlement.Contains(
                    "FruitDefensePublishSettlementOutcomeReveal((int)phase)")
                && !settlement.Contains(
                    "previousOutcomeColor.a * resultMotion.Alpha")
                && typeof(SettlementPresenter).GetMethod(
                    "FruitDefensePublishSettlementOutcomeReveal",
                    System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.NonPublic) == null
                && MethodBodyContains(runtimeGui,
                    "public static void DrawMetric(", "DrawMetricSurface")
                && runtimeGui.Contains("bool drawSurface = false")
                && !settlement.Contains("drawSurface: true"),
                "text-bearing ornaments use optical pixels, the approved outcome gates one opaque shared outline, and only explicit metric owners draw capsules");
            Assert(acceptance.Contains(
                        "function Test-SettlementOutcomeFillSupportPixel")
                && acceptance.Contains("[MidpointRounding]::ToEven")
                && acceptance.Contains("maximumConnectedThicknessCapturePixels")
                && acceptance.Contains("linkedToInnerRingPixels")
                && acceptance.Contains("touchesSampleBoundary")
                && acceptance.Contains("connectedOutlinePixelsCovered")
                && !acceptance.Contains("outlineNeighborRadius"),
                "WebGL acceptance measures complete connected outline rings at the runtime-rounded exact thickness");
            Assert(acceptance.Contains("[switch]$HubVisual")
                && acceptance.Contains("function Invoke-HubVisualMode")
                && acceptance.Contains("01-hub-home")
                && acceptance.Contains("02-hub-activity")
                && acceptance.Contains("03-hub-growth-equipment")
                && acceptance.Contains("04-hub-growth-cultivation")
                && acceptance.Contains(
                    "Invoke-CanvasClick -X $controls.hubNavActivity.x")
                && acceptance.Contains(
                    "Invoke-CanvasClick -X $controls.hubNavGrowth.x")
                && acceptance.Contains(
                    "Invoke-CanvasClick -X $controls.hubGrowthCultivation.x")
                && acceptance.Contains("FRUIT_DEFENSE_HUB_VISUAL_OK"),
                "WebGL HubVisual acceptance uses live clicks to capture the four finite Hub views");
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
                && battle.Contains("drawSurface: true")
                && !battle.Contains("drawSurface: false")
                && battle.Contains("RuntimeUiGui.DrawRaisedPanel")
                && battle.Contains("RuntimeUiGui.DrawSafeArea")
                && battle.Contains("layout.PageShell")
                && !battle.Contains("RuntimeUiGui.DrawMetricDivider")
                && battle.Contains("RuntimeUiArtSlot.IconResourceSunMicro")
                && battle.Contains("RuntimeUiArtSlot.IconResourceCoreMicro")
                && battle.Contains("RuntimeUiArtSlot.IconResourceWaveMicro")
                && battle.Contains("DrawControlledTwoLineText")
                && battle.Contains("layout.ModalResultBannerText")
                && battle.Contains("content.ResultBannerText")
                && MethodBodyContains(battle, "private void DrawHeader(",
                    "DrawCompactControlVisual(drawContext, layout.SpeedAction")
                && MethodBodyContains(battle, "private void DrawHeader(",
                    "multiplierText: _game.State.Speed + \"×\"")
                && !MethodBodyContains(battle, "private void DrawHeader(",
                    "RuntimeUiArtSlot.IconControlSpeed")
                && !battle.Contains("layout.SpeedActionIcon")
                && !battle.Contains("layout.SpeedActionValue"),
                "Battle consumes compact metrics, centered speed multiplier, and controlled terminal-copy anatomy");
            Assert(lobby.Contains("RuntimeUiGui.DrawHubScreenBackground")
                && !lobby.Contains("RuntimeUiGui.DrawSafeArea")
                && !lobby.Contains("RuntimeUiGui.DrawShellOrchardDepth")
                && MethodBodyContains(runtimeGui,
                    "public static void DrawHubScreenBackground(",
                    "context.Theme.Colors.EdgeBackground")
                && MethodBodyContains(runtimeGui,
                    "public static void DrawHubScreenBackground(",
                    "RuntimeUiArtSlot.SurfaceScreenBackground")
                && settlement.Contains("RuntimeUiGui.DrawShellOrchardDepth")
                && !battle.Contains("RuntimeUiGui.DrawShellOrchardDepth"),
                "Hub owns a semantic blue paper edge while Settlement retains orchard depth and Battle preserves board clarity");
            Assert(!settlement.Contains("DrawMetricDivider")
                && !settlement.Contains("FirstMetricDivider")
                && !settlement.Contains("SecondMetricDivider"),
                "Settlement full-width metric rows replace obsolete empty dividers");
            Assert(lobby.Contains("startCopy.Text")
                && !lobby.Contains("FormatLobbyStart(")
                && settlement.Contains("LevelDisplayName(ViewData.LevelId)")
                && lobby.Contains("RuntimeUiGui.DrawHubLevelCardSurface")
                && lobby.Contains("RuntimeUiIndicatorKind.Selected")
                && !lobby.Contains("\"开始战斗 · \" + _visibleSelectedLevelId")
                && !settlement.Contains("\"完成关卡 \" + ViewData.LevelId"),
                "Lobby and Settlement never expose internal level IDs as copy");
            Assert(!runtimeGui.Contains("GUI.skin")
                && !runtimeGui.Contains("Texture2D.whiteTexture")
                && !runtimeGui.Contains("DrawActionInteractionCue")
                && runtimeGui.Contains("RuntimeUiMotion.InteractionState(")
                && !runtimeGui.Contains("Resources.Load"),
                "shared quality path uses marker-free contained interaction motion and no primitive, default-skin, or resource fallback");
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

        private static void ValidateSettlementOutcomeEmphasis(RuntimeUiTheme theme)
        {
            var layout = PortraitShellLayout.CreateSettlement(402f, 874f,
                new Rect(0f, 0f, 402f, 874f));
            var context = RuntimeUiDrawContext.Create(theme, layout.Frame.Scale);
            var bannerDraw = RuntimeUiGui.ResolveOpticalEnvelopeDrawRect(context,
                RuntimeUiArtSlot.OrnamentResultBanner, layout.ResultBanner);
            var bannerVisual = RuntimeUiGui.ResolveOpticalVisualRect(context,
                RuntimeUiArtSlot.OrnamentResultBanner, bannerDraw);
            var outcomeIds = new[]
            {
                RuntimeUiCopyId.SettlementVictory,
                RuntimeUiCopyId.SettlementDefeat,
            };

            for (var index = 0; index < outcomeIds.Length; index++)
            {
                var copy = RuntimeUiCopyCatalog.Get(outcomeIds[index]);
                var emphasis = RuntimeUiGui.ResolveEmphasisTextLayout(context,
                    layout.Outcome, copy.Role, copy.Tone, copy.Alignment,
                    index == 0
                        ? RuntimeUiInteractionState.Success
                        : RuntimeUiInteractionState.Error);
                Assert(copy.Role == RuntimeUiTypographyRole.Display
                    && emphasis.Style.fontSize
                        == RuntimeUiQualityProfile.MinimumFontSize(copy.Role)
                    && ReferenceEquals(emphasis.Style.font,
                        theme.Typography.For(copy.Role).Font)
                    && emphasis.Style.fontStyle == FontStyle.Normal
                    && emphasis.OutlinePixels
                        == RuntimeUiQualityProfile.EmphasisOutlineCapturePixels
                    && Contrast(emphasis.OutlineColor, emphasis.FillColor) + .001f
                        >= RuntimeUiQualityProfile.NonTextContrast,
                    copy.Id + " keeps the approved static display face and visible two-pixel contrasting outline");
                Assert(Contains(bannerVisual, emphasis.OutlinedRect)
                    && Mathf.Abs(emphasis.TextRect.center.x
                        - bannerVisual.center.x)
                        <= RuntimeUiQualityProfile.GeometryTolerance
                    && Mathf.Abs(emphasis.TextRect.center.y
                        - bannerVisual.center.y)
                        <= RuntimeUiQualityProfile.GeometryTolerance,
                    copy.Id + " outlined line owner stays centered and contained in the optical banner");
            }

            const float checkpointStart = 10f;
            var revealPulse = RuntimeUiMotion.BeginReveal(checkpointStart,
                theme.Feedback, 5);
            var visibleAt = SettlementPresenter.ResolveOutcomeVisibleAt(
                checkpointStart, theme.Feedback);
            var emphasisPulse = RuntimeUiFeedbackPulse.Begin(visibleAt,
                theme.Feedback.UnscaledPopSeconds);
            var hiddenAt = visibleAt
                - theme.Feedback.UnscaledTransitionSeconds - .001f;
            var settledHiddenAt = visibleAt - .001f;
            var appearingAt = visibleAt
                + theme.Feedback.UnscaledPopSeconds * .5f;
            var stableAt = visibleAt + theme.Feedback.UnscaledPopSeconds;
            var hiddenPhase = SettlementPresenter.ResolveOutcomeRevealPhase(
                revealPulse, emphasisPulse, hiddenAt, theme.Feedback);
            var settledHiddenPhase = SettlementPresenter.ResolveOutcomeRevealPhase(
                revealPulse, emphasisPulse, settledHiddenAt, theme.Feedback);
            var appearingPhase = SettlementPresenter.ResolveOutcomeRevealPhase(
                revealPulse, emphasisPulse, appearingAt, theme.Feedback);
            var stablePhase = SettlementPresenter.ResolveOutcomeRevealPhase(
                revealPulse, emphasisPulse, stableAt, theme.Feedback);
            var appearingMotion = RuntimeUiMotion.Evaluate(
                emphasisPulse, appearingAt, theme.Feedback,
                RuntimeUiMotionPattern.StrongPop);
            Assert(hiddenPhase == SettlementOutcomeRevealPhase.Hidden,
                "Settlement outcome draw plan is absent before the result card settles");
            Assert(settledHiddenPhase == SettlementOutcomeRevealPhase.SettledHidden,
                "Settlement holds one stable hidden dwell after the result card settles");
            Assert(appearingPhase == SettlementOutcomeRevealPhase.Appearing
                && Mathf.Approximately(appearingMotion.Alpha, 1f)
                && appearingMotion.Scale < 1f,
                "Settlement outcome appears as one opaque fill-plus-outline composition while StrongPop changes geometry only");
            Assert(stablePhase == SettlementOutcomeRevealPhase.Stable,
                "Settlement outcome reaches a deterministic stable phase after its opaque geometry pulse");

            AssertThrows<ArgumentOutOfRangeException>(() =>
                    RuntimeUiGui.ResolveEmphasisTextLayout(context, layout.Outcome,
                        RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Primary,
                        TextAnchor.MiddleCenter),
                "supporting body copy cannot enter the heavy outline path");
        }

        private static bool ContainsTextPixelRounded(Rect outer, Rect inner)
        {
            const float pixelRoundingTolerance = .51f;
            return inner.xMin >= outer.xMin - pixelRoundingTolerance
                && inner.yMin >= outer.yMin - pixelRoundingTolerance
                && inner.xMax <= outer.xMax + pixelRoundingTolerance
                && inner.yMax <= outer.yMax + pixelRoundingTolerance;
        }

        private static Rect SnapDeviceRect(Rect rect)
        {
            return Rect.MinMaxRect(Mathf.Round(rect.xMin), Mathf.Round(rect.yMin),
                Mathf.Round(rect.xMax), Mathf.Round(rect.yMax));
        }

        private static bool Approximately(Rect left, Rect right)
        {
            var tolerance = RuntimeUiQualityProfile.GeometryTolerance;
            return Mathf.Abs(left.xMin - right.xMin) <= tolerance
                && Mathf.Abs(left.yMin - right.yMin) <= tolerance
                && Mathf.Abs(left.xMax - right.xMax) <= tolerance
                && Mathf.Abs(left.yMax - right.yMax) <= tolerance;
        }

        private static bool ReferenceRectEquals(Rect left, Rect right)
        {
            const float referenceTolerance = .001f;
            return Mathf.Abs(left.xMin - right.xMin) <= referenceTolerance
                && Mathf.Abs(left.yMin - right.yMin) <= referenceTolerance
                && Mathf.Abs(left.xMax - right.xMax) <= referenceTolerance
                && Mathf.Abs(left.yMax - right.yMax) <= referenceTolerance;
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

        private static void AssertThrows<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(
                "Runtime UI quality validation failed: " + message);
        }
    }
}
