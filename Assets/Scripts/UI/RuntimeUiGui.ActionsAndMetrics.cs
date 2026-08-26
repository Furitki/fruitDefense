using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public static partial class RuntimeUiGui
    {
        public static bool DrawAction(RuntimeUiDrawContext context, Rect rect, string label,
            RuntimeUiActionSpec spec, RuntimeUiInteractionState state,
            RuntimeUiArtSlot? iconSlot = null,
            RuntimeUiTypographyRole labelRole = RuntimeUiTypographyRole.ControlLabel,
            bool emphasized = false,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            DrawActionVisual(context, rect, label, spec, state, iconSlot,
                labelRole, emphasized, motion);

            var enabled = GUI.enabled;
            GUI.enabled = enabled && state != RuntimeUiInteractionState.Disabled
                && state != RuntimeUiInteractionState.Loading;
            try
            {
                return GUI.Button(rect, GUIContent.none, context.Styles.HitTarget);
            }
            finally
            {
                GUI.enabled = enabled;
            }
        }

        public static void DrawActionVisual(RuntimeUiDrawContext context, Rect rect,
            string label, RuntimeUiActionSpec spec, RuntimeUiInteractionState state,
            RuntimeUiArtSlot? iconSlot = null,
            RuntimeUiTypographyRole labelRole = RuntimeUiTypographyRole.ControlLabel,
            bool emphasized = false,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            RequireStandardActionContent(spec, label, iconSlot);
            var heldMotion = state == RuntimeUiInteractionState.Pressed
                ? RuntimeUiMotion.HeldPress(context.Theme.Feedback)
                : RuntimeUiMotionSample.Rest;
            var visualMotion = RuntimeUiMotionSample.Combine(motion, heldMotion);
            var visualRect = visualMotion.Transform(rect);
            var style = context.Theme.ResolveActionStyle(spec, state, false);
            var visualState = ResolveActionDrawState(spec.Role, state, emphasized);
            var previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                previousColor.a * visualMotion.Alpha);
            try
            {
                DrawSlotArt(context, visualRect, style.ContainerSlot,
                    RuntimeUiInteractionState.Normal);
                DrawActionInteractionCue(context, visualRect, state,
                    style.OutlineColor);

                var contentLayout = ResolveActionContentLayout(context, visualRect, label,
                    spec, state, iconSlot, labelRole, emphasized);
                RequireContentFit(contentLayout.Fits, "action", label);
                if (iconSlot.HasValue && contentLayout.HasIcon)
                {
                    RequireActionGlyphSlot(iconSlot.Value);
                    DrawSlotArt(context, contentLayout.IconRect,
                        iconSlot.Value, RuntimeUiInteractionState.Normal,
                        tintOverride: style.ContentColor);
                }

                if (contentLayout.HasLabel)
                {
                    DrawTextCore(context, contentLayout.LabelRect, label, labelRole,
                        RuntimeUiTextTone.Primary, TextAnchor.MiddleCenter, visualState, true,
                        context.Styles.SingleLineText(labelRole, TextAnchor.MiddleCenter),
                        style.ContentColor);
                }

                DrawStateIndicator(context, visualRect, state);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static void DrawCompactControlVisual(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiActionSpec spec,
            RuntimeUiInteractionState interactionState,
            RuntimeUiCompactControlVisualSample lifecycleSample,
            RuntimeUiArtSlot? iconSlot = null, string multiplierText = null,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            RequireCompactInteractionState(interactionState);
            RequireCompactActionSpec(spec);
            var hasIcon = iconSlot.HasValue;
            var hasMultiplierText = !string.IsNullOrEmpty(multiplierText);
            if (hasIcon == hasMultiplierText)
            {
                throw new ArgumentException(
                    "Compact controls require exactly one icon or multiplier text.");
            }
            if (hasIcon)
                RequireActionGlyphSlot(iconSlot.Value);
            if (hasIcon != (spec.ContentForm == RuntimeUiActionContentForm.IconOnly)
                || hasMultiplierText
                    != (spec.ContentForm == RuntimeUiActionContentForm.CompactMultiplier))
            {
                throw new ArgumentException(
                    "Compact-control content must match its explicit action form.");
            }
            var layout = ResolveCompactControlLayout(rect, interactionState,
                hasMultiplierText, context.Theme.Feedback, motion);
            var modeActive = lifecycleSample.ActiveAmount >= .5f;
            var style = context.Theme.ResolveActionStyle(
                spec, interactionState, modeActive);
            var previousColor = ApplyMotionAlpha(motion);
            try
            {
                DrawSlotArt(context, layout.SurfaceRect,
                    style.ContainerSlot, RuntimeUiInteractionState.Normal);
                DrawActionInteractionCue(context, layout.SurfaceRect,
                    interactionState, style.OutlineColor);

                if (hasIcon)
                {
                    DrawSlotArt(context, layout.ContentRect, iconSlot.Value,
                        RuntimeUiInteractionState.Normal,
                        tintOverride: style.ContentColor);
                }
                else
                {
                    DrawTextCore(context, layout.ContentRect, multiplierText,
                        RuntimeUiTypographyRole.Metric, RuntimeUiTextTone.Primary,
                        TextAnchor.MiddleCenter, interactionState, true,
                        context.Styles.SingleLineText(RuntimeUiTypographyRole.Metric,
                            TextAnchor.MiddleCenter), style.ContentColor);
                }

                DrawStateIndicator(context, layout.SurfaceRect, interactionState);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static RuntimeUiCompactControlLayout ResolveCompactControlLayout(
            Rect rect, RuntimeUiInteractionState interactionState,
            bool usesMultiplierText,
            RuntimeUiFeedbackTokens feedback, RuntimeUiMotionSample motion = default)
        {
            RequireCompactInteractionState(interactionState);
            if (rect.width <= 0f || rect.height <= 0f
                || !IsFinite(rect.x) || !IsFinite(rect.y)
                || !IsFinite(rect.width) || !IsFinite(rect.height))
            {
                throw new ArgumentOutOfRangeException(nameof(rect), rect,
                    "Compact-control geometry must be finite and positive.");
            }

            var heldMotion = interactionState == RuntimeUiInteractionState.Pressed
                ? RuntimeUiMotion.HeldPress(feedback)
                : RuntimeUiMotionSample.Rest;
            var visualMotion = RuntimeUiMotionSample.Combine(motion, heldMotion);
            var baseRect = ContainedTransform(rect, visualMotion);
            var shortest = Mathf.Min(baseRect.width, baseRect.height);
            var contentSize = shortest * (usesMultiplierText ? .78f : .56f);
            var contentRect = new Rect(0f, 0f, contentSize, contentSize);
            contentRect.center = baseRect.center;

            var result = new RuntimeUiCompactControlLayout(rect, baseRect,
                contentRect, usesMultiplierText);
            if (!result.IsContained())
                throw new InvalidOperationException(
                    "Compact-control visual geometry escaped its authoritative rectangle.");
            return result;
        }

        public static RuntimeUiActionContentLayout ResolveActionContentLayout(
            RuntimeUiDrawContext context, Rect rect, string label,
            RuntimeUiActionSpec spec, RuntimeUiInteractionState state,
            RuntimeUiArtSlot? iconSlot,
            RuntimeUiTypographyRole labelRole = RuntimeUiTypographyRole.ControlLabel,
            bool emphasized = false)
        {
            context = Require(context);
            RequireStandardActionContent(spec, label, iconSlot);
            var visualState = ResolveActionDrawState(spec.Role, state, emphasized);
            var contentRect = context.ContentRect(
                Inset(rect, context.Scaled(context.Theme.Metrics.SpacingSm)),
                visualState);
            var hasLabel = !string.IsNullOrEmpty(label);
            var labelSize = hasLabel
                ? MeasureSingleLine(context, labelRole, label,
                    TextAnchor.MiddleCenter, ActionMeasurementContent)
                : Vector2.zero;
            var labelHeight = Mathf.Min(contentRect.height, labelSize.y);
            if (!iconSlot.HasValue)
            {
                var labelOnlyWidth = Mathf.Min(contentRect.width, labelSize.x);
                var labelRect = hasLabel ? new Rect(
                    contentRect.center.x - labelOnlyWidth * .5f,
                    contentRect.center.y - labelHeight * .5f,
                    labelOnlyWidth, labelHeight) : default;
                var fits = !hasLabel
                    || labelSize.x <= contentRect.width + PixelRoundingTolerance
                    && labelSize.y <= contentRect.height + PixelRoundingTolerance;
                return new RuntimeUiActionContentLayout(contentRect, default,
                    default, labelRect, labelRect, false, hasLabel, fits);
            }

            RequireActionGlyphSlot(iconSlot.Value);
            var desiredIconSize = Mathf.Min(contentRect.height,
                context.Scaled(context.Theme.Metrics.TouchTargetMinimum));
            if (!hasLabel)
            {
                var centeredIcon = CenterSquare(contentRect, desiredIconSize);
                var iconOnlyVisual = ResolveOpticalVisualRect(
                    context, iconSlot.Value, centeredIcon);
                var fits = centeredIcon.width + PixelRoundingTolerance
                    >= desiredIconSize;
                return new RuntimeUiActionContentLayout(contentRect, centeredIcon,
                    iconOnlyVisual, default, iconOnlyVisual, true, false, fits);
            }

            var gap = context.Scaled(context.Theme.Metrics.SpacingXs);
            var desiredProbeIcon = CenterSquare(contentRect, desiredIconSize);
            var desiredProbeVisual = ResolveOpticalVisualRect(
                context, iconSlot.Value, desiredProbeIcon);
            var visualRatio = desiredIconSize <= 0f ? 1f
                : desiredProbeVisual.width / desiredIconSize;
            visualRatio = Mathf.Max(.001f, visualRatio);
            var minimumIconSize = Mathf.Min(desiredIconSize,
                context.Scaled(context.Theme.Typography.For(labelRole).FontSize));
            var minimumVisualWidth = minimumIconSize * visualRatio;
            var labelWidth = Mathf.Min(labelSize.x, Mathf.Max(0f,
                contentRect.width - gap - minimumVisualWidth));
            var iconSize = Mathf.Min(desiredIconSize, Mathf.Max(0f,
                contentRect.width - gap - labelWidth) / visualRatio);
            var probeIcon = CenterSquare(contentRect, iconSize);
            var probeVisual = ResolveOpticalVisualRect(
                context, iconSlot.Value, probeIcon);
            var groupWidth = probeVisual.width + gap + labelWidth;
            var groupX = contentRect.center.x - groupWidth * .5f;
            var iconRect = new Rect(
                groupX - (probeVisual.xMin - probeIcon.xMin),
                contentRect.center.y - iconSize * .5f
                    - (probeVisual.center.y - probeIcon.center.y),
                iconSize, iconSize);
            var iconVisual = ResolveOpticalVisualRect(context, iconSlot.Value, iconRect);
            var labelRectWithIcon = new Rect(iconVisual.xMax + gap,
                contentRect.center.y - labelHeight * .5f,
                labelWidth, labelHeight);
            var fitsIconLabel = labelWidth + PixelRoundingTolerance >= labelSize.x
                && labelHeight + PixelRoundingTolerance >= labelSize.y
                && iconSize + PixelRoundingTolerance >= desiredIconSize;
            return new RuntimeUiActionContentLayout(contentRect, iconRect,
                iconVisual, labelRectWithIcon,
                Union(iconVisual, labelRectWithIcon), true, true, fitsIconLabel);
        }

        public static RuntimeUiInlineContentLayout ResolveInlineContentLayout(
            RuntimeUiDrawContext context, Rect rect, RuntimeUiArtSlot iconSlot,
            string label, RuntimeUiTypographyRole labelRole,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal,
            float iconSizeLogical = 24f)
        {
            context = Require(context);
            RequireIconSlot(iconSlot);
            var contentRect = context.ContentRect(rect, state);
            var labelSize = MeasureSingleLine(context, labelRole, label,
                TextAnchor.MiddleCenter, ActionMeasurementContent);
            var labelWidth = Mathf.Min(contentRect.width, labelSize.x);
            var labelHeight = Mathf.Min(contentRect.height, labelSize.y);
            var gap = context.Scaled(context.Theme.Metrics.SpacingSm);
            var iconSize = Mathf.Min(contentRect.height,
                context.Scaled(iconSizeLogical));
            var probeIcon = CenterSquare(contentRect, iconSize);
            var probeVisual = ResolveOpticalVisualRect(context, iconSlot, probeIcon);
            var maximumLabelWidth = Mathf.Max(0f,
                contentRect.width - probeVisual.width - gap);
            labelWidth = Mathf.Min(labelWidth, maximumLabelWidth);
            var groupWidth = probeVisual.width + gap + labelWidth;
            var groupX = contentRect.center.x - groupWidth * .5f;
            var iconRect = new Rect(
                groupX - (probeVisual.xMin - probeIcon.xMin),
                contentRect.center.y - iconSize * .5f
                    - (probeVisual.center.y - probeIcon.center.y),
                iconSize, iconSize);
            var iconVisual = ResolveOpticalVisualRect(context, iconSlot, iconRect);
            var labelRect = new Rect(iconVisual.xMax + gap,
                contentRect.center.y - labelHeight * .5f,
                labelWidth, labelHeight);
            var fits = labelWidth + PixelRoundingTolerance >= labelSize.x
                && labelHeight + PixelRoundingTolerance >= labelSize.y
                && iconSize + PixelRoundingTolerance
                    >= context.Scaled(iconSizeLogical);
            return new RuntimeUiInlineContentLayout(contentRect, iconRect,
                iconVisual, labelRect, Union(iconVisual, labelRect), fits);
        }

        public static void DrawInlineIconLabel(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiArtSlot iconSlot, string label, RuntimeUiTypographyRole labelRole,
            RuntimeUiTextTone tone,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal,
            float iconSizeLogical = 24f)
        {
            context = Require(context);
            var layout = ResolveInlineContentLayout(context, rect, iconSlot,
                label, labelRole, state, iconSizeLogical);
            RequireContentFit(layout.Fits, "inline icon-label", label);
            DrawSlotArt(context, layout.IconRect, iconSlot,
                RuntimeUiInteractionState.Normal);
            DrawTextCore(context, layout.LabelRect, label, labelRole, tone,
                TextAnchor.MiddleCenter, state, true);
        }

        public static void DrawMetric(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiArtSlot resourceIcon, string label, string value,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal,
            bool compactInline = false, float compactIconSize = 24f,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            RequireMetricIcon(resourceIcon);
            rect = motion.Transform(rect);
            var previousColor = ApplyMotionAlpha(motion);
            try
            {
                if (compactInline)
                {
                    var compactLayout = ResolveCompactInlineMetricContentLayout(
                        context, rect, resourceIcon, label, value,
                        state, compactIconSize);
                    RequireContentFit(compactLayout.Fits, "compact metric",
                        label + " " + value);
                    DrawSlotArt(context, compactLayout.IconRect, resourceIcon, state);
                    DrawTextCore(context, compactLayout.LabelRect, label,
                        RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Secondary,
                        TextAnchor.MiddleLeft, state, true);
                    DrawTextCore(context, compactLayout.ValueRect, value,
                        RuntimeUiTypographyRole.Supplemental, RuntimeUiTextTone.Primary,
                        TextAnchor.MiddleLeft, state, true);
                    DrawStateIndicator(context, rect, state);
                    return;
                }

                var metricLayout = ResolveMetricContentLayout(
                    context, rect, resourceIcon, label, value, state);
                RequireContentFit(metricLayout.Fits, "metric",
                    label + " " + value);
                DrawSlotArt(context, metricLayout.IconRect, resourceIcon, state);
                DrawTextCore(context, metricLayout.ValueRect, value,
                    RuntimeUiTypographyRole.Metric, RuntimeUiTextTone.Primary,
                    TextAnchor.MiddleLeft, state, true);
                DrawTextCore(context, metricLayout.LabelRect,
                    label, RuntimeUiTypographyRole.Supplemental,
                    RuntimeUiTextTone.Secondary, TextAnchor.MiddleLeft, state, true);
                DrawStateIndicator(context, rect, state);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static RuntimeUiMetricContentLayout ResolveCompactInlineMetricContentLayout(
            RuntimeUiDrawContext context, Rect rect, RuntimeUiArtSlot resourceIcon,
            string label, string value,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal,
            float compactIconSize = 24f)
        {
            context = Require(context);
            RequireMetricIcon(resourceIcon);
            var content = context.ContentRect(rect, state);
            var iconSize = Mathf.Min(content.height,
                context.Scaled(Mathf.Max(0f, compactIconSize)));
            var probeIcon = CenterSquare(content, iconSize);
            var probeVisual = ResolveOpticalVisualRect(context, resourceIcon, probeIcon);
            var iconGap = context.Scaled(context.Theme.Metrics.SpacingXs);
            var labelSize = MeasureSingleLine(context,
                RuntimeUiTypographyRole.Supplemental, label,
                TextAnchor.MiddleLeft, MetricMeasurementContent);
            var valueSize = MeasureSingleLine(context,
                RuntimeUiTypographyRole.Supplemental, value,
                TextAnchor.MiddleLeft, MetricMeasurementContent);
            var valueGap = context.Scaled(context.Theme.Metrics.SpacingXs);
            var availableTextWidth = Mathf.Max(0f,
                content.width - probeVisual.width - iconGap - valueGap);
            var valueWidth = Mathf.Min(valueSize.x, availableTextWidth);
            var labelWidth = Mathf.Min(labelSize.x,
                Mathf.Max(0f, availableTextWidth - valueWidth));
            var groupWidth = probeVisual.width + iconGap
                + labelWidth + valueGap + valueWidth;
            var groupX = content.center.x - groupWidth * .5f;
            var iconRect = new Rect(
                groupX - (probeVisual.xMin - probeIcon.xMin),
                content.center.y - iconSize * .5f
                    - (probeVisual.center.y - probeIcon.center.y),
                iconSize, iconSize);
            var iconVisual = ResolveOpticalVisualRect(context, resourceIcon, iconRect);
            var lineHeight = Mathf.Min(content.height,
                Mathf.Max(labelSize.y, valueSize.y));
            var labelRect = new Rect(iconVisual.xMax + iconGap,
                content.center.y - lineHeight * .5f, labelWidth, lineHeight);
            var valueRect = new Rect(labelRect.xMax + valueGap,
                labelRect.y, valueWidth, lineHeight);
            var fits = labelWidth + PixelRoundingTolerance >= labelSize.x
                && valueWidth + PixelRoundingTolerance >= valueSize.x
                && lineHeight + PixelRoundingTolerance
                    >= Mathf.Max(labelSize.y, valueSize.y)
                && iconSize + PixelRoundingTolerance
                    >= context.Scaled(Mathf.Max(0f, compactIconSize));
            return new RuntimeUiMetricContentLayout(content, iconRect,
                iconVisual, valueRect, labelRect,
                Union(iconVisual, Union(labelRect, valueRect)), fits);
        }

        public static RuntimeUiMetricContentLayout ResolveMetricContentLayout(
            RuntimeUiDrawContext context, Rect rect, RuntimeUiArtSlot resourceIcon,
            string label, string value,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            RequireMetricIcon(resourceIcon);
            var padding = context.Scaled(context.Theme.Metrics.SpacingXs);
            var content = context.ContentRect(Inset(rect, padding), state);
            var iconSize = Mathf.Min(content.height,
                context.Scaled(context.Theme.Metrics.TouchTargetMinimum));
            var probeIcon = CenterSquare(content, iconSize);
            var probeVisual = ResolveOpticalVisualRect(context, resourceIcon, probeIcon);
            var gap = context.Scaled(context.Theme.Metrics.SpacingXs);
            var valueSize = MeasureSingleLine(context,
                RuntimeUiTypographyRole.Metric, value,
                TextAnchor.MiddleLeft, MetricMeasurementContent);
            var labelSize = MeasureSingleLine(context,
                RuntimeUiTypographyRole.Supplemental, label,
                TextAnchor.MiddleLeft, MetricMeasurementContent);
            var textWidth = Mathf.Min(Mathf.Max(valueSize.x, labelSize.x),
                Mathf.Max(0f, content.width - probeVisual.width - gap));
            var textHeight = Mathf.Min(content.height, valueSize.y + labelSize.y);
            var groupWidth = probeVisual.width + gap + textWidth;
            var groupX = content.center.x - groupWidth * .5f;
            var iconRect = new Rect(
                groupX - (probeVisual.xMin - probeIcon.xMin),
                content.center.y - iconSize * .5f
                    - (probeVisual.center.y - probeIcon.center.y),
                iconSize, iconSize);
            var iconVisual = ResolveOpticalVisualRect(context, resourceIcon, iconRect);
            var textY = content.center.y - textHeight * .5f;
            var valueHeight = Mathf.Min(valueSize.y, textHeight);
            var labelHeight = Mathf.Max(0f, textHeight - valueHeight);
            var valueRect = new Rect(iconVisual.xMax + gap,
                textY, textWidth, valueHeight);
            var labelRect = new Rect(valueRect.x, valueRect.yMax,
                textWidth, labelHeight);
            var fits = textWidth + PixelRoundingTolerance
                    >= Mathf.Max(valueSize.x, labelSize.x)
                && valueHeight + PixelRoundingTolerance >= valueSize.y
                && labelHeight + PixelRoundingTolerance >= labelSize.y
                && iconSize + PixelRoundingTolerance
                    >= context.Scaled(context.Theme.Metrics.TouchTargetMinimum);
            return new RuntimeUiMetricContentLayout(content, iconRect,
                iconVisual, valueRect, labelRect,
                Union(iconVisual, Union(valueRect, labelRect)), fits);
        }

        private static RuntimeUiDrawContext Require(RuntimeUiDrawContext context)
        {
            return context ?? throw new ArgumentNullException(nameof(context));
        }

        private static void RequireContentFit(bool fits, string semantic, string content)
        {
            if (fits) return;
            throw new InvalidOperationException(
                "Runtime UI " + semantic + " owner cannot contain its measured content: "
                + (content ?? string.Empty));
        }

        public static RuntimeUiActionInteractionCueLayout
            ResolveActionInteractionCueLayout(RuntimeUiDrawContext context,
                Rect rect, RuntimeUiInteractionState state)
        {
            context = Require(context);
            if (state != RuntimeUiInteractionState.HoveredOrFocused)
            {
                return new RuntimeUiActionInteractionCueLayout(
                    rect, default, default, default, default, false);
            }
            if (rect.width <= 0f || rect.height <= 0f
                || !IsFinite(rect.x) || !IsFinite(rect.y)
                || !IsFinite(rect.width) || !IsFinite(rect.height))
            {
                throw new ArgumentOutOfRangeException(nameof(rect), rect,
                    "Action interaction-cue geometry must be finite and positive.");
            }

            var thickness = Mathf.Max(1f,
                context.Scaled(context.Theme.Metrics.OutlineThin));
            var inset = Mathf.Max(thickness,
                context.Scaled(context.Theme.Metrics.SpacingXs));
            var inner = Inset(rect, inset);
            if (inner.width <= thickness * 2f || inner.height <= thickness * 2f)
            {
                return new RuntimeUiActionInteractionCueLayout(
                    rect, default, default, default, default, false);
            }

            var cornerGap = Mathf.Min(
                context.Scaled(context.Theme.Metrics.CornerSmall),
                Mathf.Min(inner.width, inner.height) * .25f);
            var horizontalLength = inner.width - cornerGap * 2f;
            var verticalLength = inner.height - cornerGap * 2f;
            if (horizontalLength <= 0f || verticalLength <= 0f)
            {
                return new RuntimeUiActionInteractionCueLayout(
                    rect, default, default, default, default, false);
            }

            var top = new Rect(inner.xMin + cornerGap, inner.yMin,
                horizontalLength, thickness);
            var right = new Rect(inner.xMax - thickness,
                inner.yMin + cornerGap, thickness, verticalLength);
            var bottom = new Rect(inner.xMin + cornerGap,
                inner.yMax - thickness, horizontalLength, thickness);
            var left = new Rect(inner.xMin, inner.yMin + cornerGap,
                thickness, verticalLength);
            var result = new RuntimeUiActionInteractionCueLayout(
                rect, top, right, bottom, left, true);
            if (!result.IsContained())
            {
                throw new InvalidOperationException(
                    "Action interaction cue escaped its authoritative rectangle.");
            }
            return result;
        }

        private static void DrawActionInteractionCue(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiInteractionState state, Color outlineColor)
        {
            var layout = ResolveActionInteractionCueLayout(context, rect, state);
            if (!layout.Visible)
                return;

            var previousColor = GUI.color;
            try
            {
                outlineColor.a *= previousColor.a;
                GUI.color = outlineColor;
                // This built-in pixel is only a primitive for the four contained
                // focus segments; it never substitutes for an ArtSet surface.
                var pixel = Texture2D.whiteTexture;
                GUI.DrawTexture(layout.Top, pixel);
                GUI.DrawTexture(layout.Right, pixel);
                GUI.DrawTexture(layout.Bottom, pixel);
                GUI.DrawTexture(layout.Left, pixel);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private static void RequireStandardActionContent(RuntimeUiActionSpec spec,
            string label, RuntimeUiArtSlot? iconSlot)
        {
            if (spec.Behavior != RuntimeUiActionBehavior.Instantaneous
                || spec.ContentForm == RuntimeUiActionContentForm.CompactMultiplier)
            {
                throw new ArgumentException(
                    "Standard action surfaces accept only instantaneous text, icon-label, or icon-only specs.",
                    nameof(spec));
            }

            var hasLabel = !string.IsNullOrEmpty(label);
            var hasIcon = iconSlot.HasValue;
            var matches = spec.ContentForm == RuntimeUiActionContentForm.Text
                    && !hasIcon
                || spec.ContentForm == RuntimeUiActionContentForm.IconLabel
                    && hasLabel && hasIcon
                || spec.ContentForm == RuntimeUiActionContentForm.IconOnly
                    && !hasLabel && hasIcon;
            if (!matches)
            {
                throw new ArgumentException(
                    "Action label/icon content must match its explicit content form.",
                    nameof(spec));
            }
        }

        private static void RequireCompactActionSpec(RuntimeUiActionSpec spec)
        {
            if (spec.Role != RuntimeUiActionKind.Quiet
                || (spec.ContentForm != RuntimeUiActionContentForm.IconOnly
                    && spec.ContentForm
                        != RuntimeUiActionContentForm.CompactMultiplier))
            {
                throw new ArgumentException(
                    "Compact controls must be explicit Quiet icon-only or multiplier actions.",
                    nameof(spec));
            }
        }

        private static void RequireCompactInteractionState(
            RuntimeUiInteractionState state)
        {
            switch (state)
            {
                case RuntimeUiInteractionState.Normal:
                case RuntimeUiInteractionState.HoveredOrFocused:
                case RuntimeUiInteractionState.Pressed:
                case RuntimeUiInteractionState.Disabled:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state,
                        "Compact controls accept interaction state independently from mode lifecycle.");
            }
        }

        private static Rect ContainedTransform(Rect bounds,
            RuntimeUiMotionSample motion)
        {
            var transformed = motion.Transform(bounds);
            var width = Mathf.Min(bounds.width, transformed.width);
            var height = Mathf.Min(bounds.height, transformed.height);
            var x = Mathf.Clamp(transformed.x, bounds.xMin, bounds.xMax - width);
            var y = Mathf.Clamp(transformed.y, bounds.yMin, bounds.yMax - height);
            return new Rect(x, y, width, height);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static RuntimeUiInteractionState ResolveActionVisualState(
            RuntimeUiActionKind kind, RuntimeUiInteractionState state)
        {
            if (state != RuntimeUiInteractionState.Loading)
                return state;
            return kind == RuntimeUiActionKind.Primary || kind == RuntimeUiActionKind.Danger
                ? RuntimeUiInteractionState.Normal
                : state;
        }

        private static RuntimeUiInteractionState ResolveActionDrawState(
            RuntimeUiActionKind kind, RuntimeUiInteractionState state, bool emphasized)
        {
            var resolved = ResolveActionVisualState(kind, state);
            if (!emphasized || state == RuntimeUiInteractionState.Disabled
                || state == RuntimeUiInteractionState.Loading)
            {
                return resolved;
            }

            return RuntimeUiInteractionState.Pressed;
        }

    }
}
