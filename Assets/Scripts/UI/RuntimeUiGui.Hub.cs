using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public readonly struct RuntimeUiHubNavigationItemLayout
    {
        internal RuntimeUiHubNavigationItemLayout(Rect hitRect, Rect surface,
            Rect icon, Rect label, Rect underline)
        {
            HitRect = hitRect;
            Surface = surface;
            Icon = icon;
            Label = label;
            Underline = underline;
        }

        public Rect HitRect { get; }
        public Rect Surface { get; }
        public Rect Icon { get; }
        public Rect Label { get; }
        public Rect Underline { get; }
    }

    public readonly struct RuntimeUiHubGrowthPreviewLayout
    {
        internal RuntimeUiHubGrowthPreviewLayout(Rect surface, Rect ribbon,
            Rect icon, Rect title, Rect body, Rect divider,
            Rect stateIndicator)
        {
            Surface = surface;
            Ribbon = ribbon;
            Icon = icon;
            Title = title;
            Body = body;
            Divider = divider;
            StateIndicator = stateIndicator;
        }

        public Rect Surface { get; }
        public Rect Ribbon { get; }
        public Rect Icon { get; }
        public Rect Title { get; }
        public Rect Body { get; }
        public Rect Divider { get; }
        public Rect StateIndicator { get; }
    }

    public readonly struct RuntimeUiHubRewardTileLayout
    {
        internal RuntimeUiHubRewardTileLayout(Rect surface, Rect icon, Rect label)
        {
            Surface = surface;
            Icon = icon;
            Label = label;
        }

        public Rect Surface { get; }
        public Rect Icon { get; }
        public Rect Label { get; }
    }

    public readonly struct RuntimeUiHubGrowthTabLayout
    {
        internal RuntimeUiHubGrowthTabLayout(Rect hitRect, Rect surface,
            Rect label, Rect underline)
        {
            HitRect = hitRect;
            Surface = surface;
            Label = label;
            Underline = underline;
        }

        public Rect HitRect { get; }
        public Rect Surface { get; }
        public Rect Label { get; }
        public Rect Underline { get; }
    }

    public readonly struct RuntimeUiHubBalanceLayout
    {
        internal RuntimeUiHubBalanceLayout(Rect surface, Rect label, Rect value)
        {
            Surface = surface;
            Label = label;
            Value = value;
        }

        public Rect Surface { get; }
        public Rect Label { get; }
        public Rect Value { get; }
    }

    public static partial class RuntimeUiGui
    {
        public static void DrawHubScreenBackground(RuntimeUiDrawContext context,
            Rect rect)
        {
            context = Require(context);
            var paperTint = Color.Lerp(Color.white,
                context.Theme.Colors.EdgeBackground, .5f);
            DrawSlotArt(context, rect, RuntimeUiArtSlot.SurfaceScreenBackground,
                RuntimeUiInteractionState.Normal, tintOverride: paperTint);
        }

        public static void DrawHubTopBarWithBalance(RuntimeUiDrawContext context,
            Rect frame, Rect titleRect, Rect balanceRect, string title,
            string label, string value, RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(frame, nameof(frame));
            RequireHubContainedRect(frame, titleRect, nameof(titleRect));
            RequireHubContainedRect(frame, balanceRect, nameof(balanceRect));

            DrawSlotArt(context, frame, RuntimeUiArtSlot.SurfacePanelRaised,
                ResolveSurfaceVisualState(state));
            DrawSingleLineText(context, titleRect, title,
                RuntimeUiTypographyRole.ScreenTitle, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);

            var balance = ResolveHubBalanceLayout(context, balanceRect,
                label, value, state);
            DrawSlotArt(context, balance.Surface, RuntimeUiArtSlot.SurfaceMetric,
                ResolveSurfaceVisualState(state));
            DrawSingleLineText(context, balance.Label, label,
                RuntimeUiTypographyRole.Supplemental,
                RuntimeUiTextTone.Secondary, TextAnchor.MiddleLeft, state);
            DrawSingleLineText(context, balance.Value, value,
                RuntimeUiTypographyRole.Metric, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleLeft, state);
            DrawStateIndicator(context, balance.Surface, state);
        }

        public static RuntimeUiHubBalanceLayout ResolveHubBalanceLayout(
            RuntimeUiDrawContext context, Rect rect, string label, string value,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));

            var surface = RuntimeUiMotion.InteractionState(
                state, context.Theme.Feedback).Transform(rect);
            var inset = context.Scaled(context.Theme.Metrics.SpacingSm);
            var indicatorReserve = HasHubStateIndicator(state)
                ? context.Scaled(context.Theme.Metrics.SpacingXl
                    + context.Theme.Metrics.SpacingXs)
                : 0f;
            var content = Rect.MinMaxRect(surface.xMin + inset, surface.yMin,
                surface.xMax - inset - indicatorReserve, surface.yMax);
            var labelSize = MeasureSingleLine(context,
                RuntimeUiTypographyRole.Supplemental, label,
                TextAnchor.MiddleLeft, HubMeasurementContent);
            var valueSize = MeasureSingleLine(context,
                RuntimeUiTypographyRole.Metric, value,
                TextAnchor.MiddleLeft, HubMeasurementContent);
            var gap = context.Scaled(context.Theme.Metrics.SpacingXs);
            var groupWidth = labelSize.x + gap + valueSize.x;
            if (content.width <= 0f || content.height <= 0f
                || groupWidth > content.width + PixelRoundingTolerance
                || labelSize.y > content.height + PixelRoundingTolerance
                || valueSize.y > content.height + PixelRoundingTolerance)
            {
                throw new InvalidOperationException(
                    "Hub resource balance cannot contain its finite label/value anatomy. "
                    + "rect=" + rect + " state=" + state
                    + " label=" + (label ?? string.Empty)
                    + " value=" + (value ?? string.Empty));
            }

            var groupX = content.x + (content.width - groupWidth) * .5f;
            var labelRect = new Rect(groupX, content.y,
                labelSize.x, content.height);
            var valueRect = new Rect(labelRect.xMax + gap, content.y,
                valueSize.x, content.height);
            return new RuntimeUiHubBalanceLayout(surface, labelRect, valueRect);
        }

        public static void DrawHubPageSurface(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStandardPanel(Require(context), rect, state);
        }

        public static void DrawHubLevelCardSurface(RuntimeUiDrawContext context,
            Rect rect, bool selected, RuntimeUiInteractionState state,
            RuntimeUiMotionSample motion)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            var previousColor = GUI.color;
            var alpha = Mathf.Clamp01(motion.Alpha);
            GUI.color = new Color(previousColor.r, previousColor.g,
                previousColor.b, previousColor.a * alpha);
            DrawSlotArt(context, motion.Transform(rect), selected
                    ? RuntimeUiArtSlot.SurfaceCardSelectable
                    : RuntimeUiArtSlot.SurfacePanelRaised,
                ResolveSurfaceVisualState(state));
            GUI.color = previousColor;
        }

        public static void DrawHubNavigationTray(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            DrawSlotArt(context, rect, RuntimeUiArtSlot.SurfaceHubNavigationBase,
                RuntimeUiInteractionState.Normal);
        }

        public static void DrawHubNavigationItem(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiArtSlot iconSlot, string label, bool selected,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubIconSlot(iconSlot);
            var layout = ResolveHubNavigationItemLayout(
                context, rect, selected, state);
            if (selected)
            {
                DrawSlotArt(context, layout.Surface,
                    RuntimeUiArtSlot.SurfaceHubNavigationSelectedTab,
                    RuntimeUiInteractionState.Normal);
            }

            // The reference contract permits only one base silhouette and one
            // selected-tab silhouette. Icons remain single-subject overlays;
            // selection tint never multiplies into their raster.
            DrawIcon(context, layout.Icon, iconSlot, RuntimeUiInteractionState.Normal);
            DrawSingleLineText(context, layout.Label, label,
                RuntimeUiTypographyRole.ControlLabel, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);
            if (selected)
                DrawHubUnderline(context, layout.Underline);
            if (state != RuntimeUiInteractionState.Normal
                && state != RuntimeUiInteractionState.HoveredOrFocused
                && state != RuntimeUiInteractionState.Pressed
                && state != RuntimeUiInteractionState.Selected)
            {
                DrawStateIndicator(context, layout.Surface, state);
            }
        }

        public static RuntimeUiHubNavigationItemLayout ResolveHubNavigationItemLayout(
            RuntimeUiDrawContext context, Rect rect, bool selected,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            if (rect.width <= 0f || rect.height < context.Scaled(44f))
            {
                throw new ArgumentOutOfRangeException(nameof(rect), rect,
                    "Hub navigation item must retain a positive 44-point hit target.");
            }

            var motion = RuntimeUiMotion.InteractionState(state, context.Theme.Feedback);
            var visual = motion.Transform(rect);
            var lift = selected ? context.Scaled(10f) : 0f;
            var horizontalInset = 0f;
            var surface = new Rect(visual.x + horizontalInset, visual.y - lift,
                visual.width - horizontalInset * 2f, visual.height + lift);
            var iconSize = Mathf.Min(context.Scaled(33f), visual.height * .44f);
            var icon = new Rect(visual.center.x - iconSize * .5f,
                visual.y + context.Scaled(5.5f), iconSize, iconSize);
            var label = new Rect(visual.x + context.Scaled(6f),
                visual.y + context.Scaled(39f), visual.width - context.Scaled(12f),
                context.Scaled(29f));
            var underlineWidth = Mathf.Min(context.Scaled(46f),
                visual.width - context.Scaled(24f));
            var underline = new Rect(visual.center.x - underlineWidth * .5f,
                visual.y + context.Scaled(69f), underlineWidth,
                context.Scaled(5f));
            if (icon.yMax > label.yMin || label.yMax > underline.yMin
                || underline.yMax > visual.yMax)
            {
                throw new InvalidOperationException(
                    "Hub navigation item cannot contain icon, label, and underline anatomy. "
                    + "state=" + state + " scale=" + context.Scale
                    + " rect=" + rect + " visual=" + visual
                    + " icon=" + icon + " label=" + label
                    + " underline=" + underline);
            }
            return new RuntimeUiHubNavigationItemLayout(rect, surface, icon,
                label, underline);
        }

        public static void DrawHubGrowthTab(RuntimeUiDrawContext context, Rect rect,
            string label, bool selected, RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            var layout = ResolveHubGrowthTabLayout(context, rect, state);
            var surfaceState = selected
                && state != RuntimeUiInteractionState.Disabled
                && state != RuntimeUiInteractionState.Loading
                    ? RuntimeUiInteractionState.Selected
                    : state;
            DrawSelectableCard(context, layout.Surface, surfaceState,
                emphasized: state == RuntimeUiInteractionState.Pressed,
                drawStateIndicator: false);
            DrawSingleLineText(context, layout.Label, label,
                RuntimeUiTypographyRole.ControlLabel, RuntimeUiTextTone.Primary,
                TextAnchor.MiddleCenter, state);
            if (selected)
                DrawHubUnderline(context, layout.Underline);
            if (state != RuntimeUiInteractionState.Selected)
                DrawStateIndicator(context, layout.Surface, state);
        }

        public static RuntimeUiHubGrowthTabLayout ResolveHubGrowthTabLayout(
            RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            if (rect.width <= 0f || rect.height < context.Scaled(44f))
            {
                throw new ArgumentOutOfRangeException(nameof(rect), rect,
                    "Hub growth tab must retain a positive 44-point hit target.");
            }

            var motion = RuntimeUiMotion.InteractionState(state,
                context.Theme.Feedback);
            var surface = motion.Transform(rect);
            var underlineHeight = context.Scaled(5f);
            var underlineInset = context.Scaled(20f);
            var label = Rect.MinMaxRect(surface.xMin + context.Scaled(8f),
                surface.yMin, surface.xMax - context.Scaled(8f),
                surface.yMax - underlineHeight - context.Scaled(5f));
            var underline = new Rect(surface.x + underlineInset,
                surface.yMax - underlineHeight,
                surface.width - underlineInset * 2f, underlineHeight);
            if (label.width <= 0f || label.height <= 0f
                || label.yMax > underline.yMin
                || underline.width <= 0f || underline.yMax > surface.yMax)
            {
                throw new InvalidOperationException(
                    "Hub growth tab cannot contain label and underline anatomy. "
                    + "state=" + state + " scale=" + context.Scale
                    + " rect=" + rect + " surface=" + surface
                    + " label=" + label + " underline=" + underline);
            }
            return new RuntimeUiHubGrowthTabLayout(rect, surface, label,
                underline);
        }

        public static void DrawHubActivityCard(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            DrawHubSurface(context, rect, RuntimeUiArtSlot.SurfacePanelRaised,
                state);
        }

        public static void DrawHubActivityBanner(RuntimeUiDrawContext context,
            Rect rect, string title, RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            DrawHubSurface(context, rect, RuntimeUiArtSlot.SurfacePanelRaised,
                state);
            DrawSingleLineText(context, rect, title,
                RuntimeUiTypographyRole.ScreenTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, state);
        }

        public static void DrawHubRewardPanel(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiInteractionState state)
        {
            DrawHubSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfacePanelStandard, state);
        }

        public static void DrawHubRewardTile(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiArtSlot iconSlot, string label,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            var layout = ResolveHubRewardTileLayout(context, rect, state);
            DrawSlotArt(context, layout.Surface, RuntimeUiArtSlot.SurfaceMetric,
                ResolveSurfaceVisualState(state));
            DrawIcon(context, layout.Icon, iconSlot, state);
            DrawSingleLineText(context, layout.Label, label,
                RuntimeUiTypographyRole.ControlLabel,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, state);
        }

        public static RuntimeUiHubRewardTileLayout ResolveHubRewardTileLayout(
            RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            var surface = RuntimeUiMotion.InteractionState(
                state, context.Theme.Feedback).Transform(rect);
            var inset = context.Scaled(context.Theme.Metrics.SpacingXs);
            var horizontalInset = context.Scaled(
                context.Theme.Metrics.SpacingSm);
            var labelHeight = context.Scaled(30f);
            var iconSize = Mathf.Min(context.Scaled(40f),
                Mathf.Min(surface.width - horizontalInset * 2f,
                    surface.height - labelHeight - inset * 3f));
            var icon = new Rect(surface.center.x - iconSize * .5f,
                surface.y + inset, iconSize, iconSize);
            var label = Rect.MinMaxRect(
                surface.x + horizontalInset, icon.yMax + inset,
                surface.xMax - horizontalInset, surface.yMax - inset);
            if (iconSize <= 0f || label.width <= 0f
                || !ContainsHubRect(surface, icon)
                || !ContainsHubRect(surface, label))
            {
                throw new InvalidOperationException(
                    "Hub reward tile cannot contain its icon/label anatomy. rect="
                    + rect + " state=" + state);
            }
            return new RuntimeUiHubRewardTileLayout(surface, icon, label);
        }

        public static void DrawHubActivityStatus(RuntimeUiDrawContext context,
            Rect rect, Rect stateIndicator, string text,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            RequireHubContainedRect(rect, stateIndicator,
                nameof(stateIndicator));
            var gap = context.Scaled(context.Theme.Metrics.SpacingSm);
            var rightInset = context.Scaled(context.Theme.Metrics.SpacingMd);
            var label = Rect.MinMaxRect(stateIndicator.xMax + gap, rect.y,
                rect.xMax - rightInset, rect.yMax);
            RequireHubContainedRect(rect, label, nameof(label));
            if (state == RuntimeUiInteractionState.Normal
                || state == RuntimeUiInteractionState.HoveredOrFocused
                || state == RuntimeUiInteractionState.Pressed
                || state == RuntimeUiInteractionState.Selected)
            {
                DrawIcon(context, stateIndicator,
                    RuntimeUiArtSlot.IconHubActivity, state);
            }
            else
            {
                DrawStateIndicator(context, stateIndicator, state);
            }
            DrawSingleLineText(context, label, text,
                RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.State, TextAnchor.MiddleLeft, state);
        }

        public static void DrawHubGrowthEntry(RuntimeUiDrawContext context,
            Rect rect, bool selected, RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            var surfaceState = selected && (state == RuntimeUiInteractionState.Normal
                    || state == RuntimeUiInteractionState.Selected)
                ? RuntimeUiInteractionState.Selected
                : state;
            var motion = RuntimeUiMotion.InteractionState(
                state, context.Theme.Feedback);
            DrawSelectableCard(context, rect, surfaceState,
                state == RuntimeUiInteractionState.Pressed,
                drawStateIndicator: false, motion: motion);
        }

        public static void DrawHubGrowthDetail(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiInteractionState state)
        {
            DrawHubSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfaceDetail, state);
        }

        public static void DrawHubHomeGrowthPreview(
            RuntimeUiDrawContext context, Rect rect, string title, string body,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            var layout = ResolveHubHomeGrowthPreviewLayout(context, rect, state);
            DrawSlotArt(context, layout.Surface,
                RuntimeUiArtSlot.SurfacePanelRaised,
                ResolveSurfaceVisualState(state));
            DrawSectionRibbon(context, layout.Ribbon, state);
            DrawIcon(context, layout.Icon, RuntimeUiArtSlot.IconHubGrowth, state);
            DrawSingleLineText(context, layout.Title, title,
                RuntimeUiTypographyRole.SectionTitle,
                RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, state);
            var lines = ResolveStatusTextLines(
                ResolveControlledTwoLineTextLayout(context, layout.Body,
                    RuntimeUiTypographyRole.Body, TextAnchor.MiddleLeft, state), body);
            DrawControlledTwoLineText(context, layout.Body, lines,
                RuntimeUiTypographyRole.Body, RuntimeUiTextTone.Secondary,
                TextAnchor.MiddleLeft, state);
            DrawSlotArt(context, layout.Divider,
                RuntimeUiArtSlot.SurfaceScrim,
                RuntimeUiInteractionState.Normal);
            DrawStateIndicator(context, layout.StateIndicator, state);
        }

        public static RuntimeUiHubGrowthPreviewLayout ResolveHubHomeGrowthPreviewLayout(
            RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            RequireHubState(state);
            if (rect.width <= 0f || rect.height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(rect), rect, null);
            var inset = context.Scaled(context.Theme.Metrics.SpacingLg);
            var surface = new Rect(rect.x, rect.y + context.Scaled(22f),
                rect.width, rect.height - context.Scaled(23f));
            var ribbonWidth = context.Scaled(160f);
            var ribbonHeight = context.Scaled(44f);
            var ribbon = new Rect(rect.x + context.Scaled(12f), rect.y,
                ribbonWidth, ribbonHeight);
            var title = new Rect(ribbon.x + context.Scaled(
                    context.Theme.Metrics.SpacingMd), ribbon.y,
                ribbon.width - context.Scaled(
                    context.Theme.Metrics.SpacingMd * 2f), ribbon.height);
            var iconSize = context.Scaled(44f);
            var icon = new Rect(rect.x + context.Scaled(24f),
                rect.y + context.Scaled(56f), iconSize, iconSize);
            var indicatorSize = context.Scaled(32f);
            var stateIndicator = new Rect(rect.x + context.Scaled(30f),
                rect.y + context.Scaled(112f), indicatorSize, indicatorSize);
            var body = new Rect(rect.x + context.Scaled(78f),
                rect.y + context.Scaled(48f),
                rect.width - context.Scaled(102f), context.Scaled(104f));
            var divider = new Rect(body.x,
                rect.y + context.Scaled(100f), body.width,
                Mathf.Max(1f, context.Scaled(2f)));
            if (surface.height <= 0f || title.width <= 0f || body.width <= 0f
                || !ContainsHubRect(rect, surface)
                || !ContainsHubRect(rect, ribbon)
                || !ContainsHubRect(rect, icon)
                || !ContainsHubRect(rect, title)
                || !ContainsHubRect(rect, body)
                || !ContainsHubRect(rect, divider)
                || !ContainsHubRect(rect, stateIndicator))
            {
                throw new InvalidOperationException(
                    "Hub growth preview cannot contain its finite ribbon/icon/title/body/state anatomy.");
            }
            return new RuntimeUiHubGrowthPreviewLayout(surface, ribbon, icon,
                title, body, divider, stateIndicator);
        }

        private static void DrawHubUnderline(RuntimeUiDrawContext context, Rect rect)
        {
            DrawSlotArt(context, rect, RuntimeUiArtSlot.SurfaceScrim,
                RuntimeUiInteractionState.Normal, tintOverride:
                    context.Theme.Colors.SelectionAccent);
        }

        private static readonly GUIContent HubMeasurementContent = new GUIContent();

        private static void DrawHubSurface(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiArtSlot surfaceSlot, RuntimeUiInteractionState state)
        {
            RequireHubState(state);
            RequireHubRect(rect, nameof(rect));
            var visual = RuntimeUiMotion.InteractionState(
                state, context.Theme.Feedback).Transform(rect);
            DrawSlotArt(context, visual, surfaceSlot,
                ResolveSurfaceVisualState(state));
        }

        private static bool HasHubStateIndicator(RuntimeUiInteractionState state)
        {
            return state == RuntimeUiInteractionState.Selected
                || state == RuntimeUiInteractionState.Disabled
                || state == RuntimeUiInteractionState.Loading
                || state == RuntimeUiInteractionState.Success
                || state == RuntimeUiInteractionState.Warning
                || state == RuntimeUiInteractionState.Error;
        }

        private static void RequireHubRect(Rect rect, string parameterName)
        {
            if (rect.width <= 0f || rect.height <= 0f
                || !IsFinite(rect.x) || !IsFinite(rect.y)
                || !IsFinite(rect.width) || !IsFinite(rect.height))
            {
                throw new ArgumentOutOfRangeException(parameterName, rect,
                    "Hub component geometry must be finite and positive.");
            }
        }

        private static void RequireHubContainedRect(Rect owner, Rect child,
            string parameterName)
        {
            RequireHubRect(child, parameterName);
            if (child.xMin < owner.xMin || child.yMin < owner.yMin
                || child.xMax > owner.xMax || child.yMax > owner.yMax)
            {
                throw new ArgumentOutOfRangeException(parameterName, child,
                    "Hub component child geometry must remain inside its owner.");
            }
        }

        private static bool ContainsHubRect(Rect owner, Rect child)
        {
            return child.xMin >= owner.xMin && child.yMin >= owner.yMin
                && child.xMax <= owner.xMax && child.yMax <= owner.yMax;
        }

        private static void RequireHubIconSlot(RuntimeUiArtSlot iconSlot)
        {
            if (iconSlot != RuntimeUiArtSlot.IconHubHome
                && iconSlot != RuntimeUiArtSlot.IconHubActivity
                && iconSlot != RuntimeUiArtSlot.IconHubGrowth)
            {
                throw new ArgumentOutOfRangeException(nameof(iconSlot), iconSlot,
                    "Hub navigation accepts only its three finite semantic icon slots.");
            }
        }

        private static void RequireHubState(RuntimeUiInteractionState state)
        {
            if (!Enum.IsDefined(typeof(RuntimeUiInteractionState), state))
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
