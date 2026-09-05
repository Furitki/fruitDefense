using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public static partial class RuntimeUiGui
    {
        public static void DrawStatus(RuntimeUiDrawContext context, Rect rect, string message,
            RuntimeUiInteractionState state,
            RuntimeUiTypographyRole textRole = RuntimeUiTypographyRole.Body,
            bool singleLine = false, bool emphasized = false,
            RuntimeUiMotionSample motion = default)
        {
            rect = motion.Transform(rect);
            var previousColor = ApplyMotionAlpha(motion);
            try
            {
                DrawStatusCore(context, rect, new RuntimeUiStatusTextLines(message), state,
                    textRole, singleLine ? RuntimeUiStatusTextMode.SingleLine
                        : RuntimeUiStatusTextMode.Standard, emphasized);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static void DrawStatus(RuntimeUiDrawContext context, Rect rect, string message,
            RuntimeUiInteractionState state, RuntimeUiTypographyRole textRole,
            RuntimeUiStatusTextMode textMode, bool emphasized = false,
            RuntimeUiMotionSample motion = default)
        {
            rect = motion.Transform(rect);
            var layout = ResolveStatusTextLayout(
                context, rect, state, textRole, textMode, emphasized);
            var previousColor = ApplyMotionAlpha(motion);
            try
            {
                DrawStatusCore(context, rect,
                    ResolveStatusTextLines(layout, message), state, textRole, textMode,
                    emphasized);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static void DrawStatus(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiStatusTextLines lines, RuntimeUiInteractionState state,
            RuntimeUiTypographyRole textRole, RuntimeUiStatusTextMode textMode,
            bool emphasized = false, RuntimeUiMotionSample motion = default)
        {
            rect = motion.Transform(rect);
            var previousColor = ApplyMotionAlpha(motion);
            try
            {
                DrawStatusCore(context, rect, lines, state, textRole, textMode, emphasized);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static RuntimeUiStatusTextMode ResolveStatusTextMode(
            RuntimeUiDrawContext context, Rect rect, string message,
            RuntimeUiInteractionState state, RuntimeUiTypographyRole textRole,
            bool emphasized = false)
        {
            context = Require(context);
            var singleLine = ResolveStatusTextLayout(context, rect, state,
                textRole, RuntimeUiStatusTextMode.SingleLine, emphasized);
            try
            {
                StatusMeasurementContent.text = message ?? string.Empty;
                var measured = singleLine.Style.CalcSize(StatusMeasurementContent);
                return measured.x <= singleLine.FirstLineRect.width
                        + PixelRoundingTolerance
                    && measured.y <= singleLine.FirstLineRect.height
                        + PixelRoundingTolerance
                        ? RuntimeUiStatusTextMode.SingleLine
                        : RuntimeUiStatusTextMode.CompactTwoLines;
            }
            finally
            {
                StatusMeasurementContent.text = string.Empty;
            }
        }

        public static RuntimeUiStatusTextLayout ResolveStatusTextLayout(
            RuntimeUiDrawContext context, Rect rect, RuntimeUiInteractionState state,
            RuntimeUiTypographyRole textRole, RuntimeUiStatusTextMode textMode,
            bool emphasized = false)
        {
            context = Require(context);
            var textModeValue = (int)textMode;
            if (textModeValue < (int)RuntimeUiStatusTextMode.Standard
                || textModeValue > (int)RuntimeUiStatusTextMode.CompactTwoLines)
                throw new ArgumentOutOfRangeException(nameof(textMode), textMode, null);

            var visualState = ResolveStatusVisualState(state, emphasized);
            var compact = textMode == RuntimeUiStatusTextMode.CompactTwoLines;
            var horizontalPadding = context.Scaled(compact
                ? context.Theme.Metrics.SpacingXs
                : context.Theme.Metrics.SpacingSm);
            var verticalPadding = context.Scaled(compact
                ? 0f
                : context.Theme.Metrics.SpacingSm);
            var content = context.ContentRect(
                Inset(rect, horizontalPadding, verticalPadding), visualState);
            var indicatorSlot = StateIndicatorSlot(state);
            var hasIndicator = indicatorSlot.HasValue;
            var indicatorRect = default(Rect);
            var firstLineRect = content;
            var secondLineRect = default(Rect);
            if (compact)
            {
                var firstLineHeight = content.height * .5f;
                firstLineRect = new Rect(content.x, content.y,
                    content.width, firstLineHeight);
                secondLineRect = new Rect(content.x, content.y + firstLineHeight,
                    content.width, Mathf.Max(0f, content.height - firstLineHeight));
                if (hasIndicator)
                {
                    var size = Mathf.Min(firstLineRect.height,
                        context.Scaled(context.Theme.Metrics.SpacingMd));
                    indicatorRect = CenterSquare(
                        new Rect(firstLineRect.x, firstLineRect.y, size,
                            firstLineRect.height), size);
                    firstLineRect.xMin = Mathf.Min(firstLineRect.xMax,
                        indicatorRect.xMax
                        + context.Scaled(context.Theme.Metrics.SpacingXs));
                }
            }
            else if (hasIndicator)
            {
                var size = Mathf.Min(content.height,
                    context.Scaled(context.Theme.Metrics.SpacingXl));
                indicatorRect = CenterSquare(
                    new Rect(content.x, content.y, size, content.height), size);
                firstLineRect.xMin = Mathf.Min(firstLineRect.xMax,
                    indicatorRect.xMax + context.Scaled(context.Theme.Metrics.SpacingXs));
            }

            var style = textMode == RuntimeUiStatusTextMode.SingleLine
                ? context.Styles.SingleLineText(textRole, TextAnchor.MiddleLeft)
                : compact
                    ? context.Styles.CompactTwoLineText(textRole, TextAnchor.MiddleLeft)
                    : context.Styles.Text(textRole, TextAnchor.MiddleLeft);
            return new RuntimeUiStatusTextLayout(firstLineRect, secondLineRect, indicatorRect,
                hasIndicator, style, textMode == RuntimeUiStatusTextMode.SingleLine
                    ? 1
                    : compact ? 2 : 0);
        }

        public static RuntimeUiStatusTextLines ResolveStatusTextLines(
            RuntimeUiStatusTextLayout layout, string message)
        {
            message = message ?? string.Empty;
            if (layout.MaximumLineCount != 2 || message.Length < 2)
                return new RuntimeUiStatusTextLines(message);

            var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
            var explicitBreak = normalized.IndexOf('\n');
            if (explicitBreak >= 0)
            {
                var nextBreak = normalized.IndexOf('\n', explicitBreak + 1);
                var firstLine = normalized.Substring(0, explicitBreak);
                var secondLine = normalized.Substring(explicitBreak + 1);
                if (nextBreak < 0
                    && FitsStatusLine(layout.Style, firstLine,
                        layout.FirstLineRect)
                    && FitsStatusLine(layout.Style, secondLine,
                        layout.SecondLineRect))
                {
                    return new RuntimeUiStatusTextLines(firstLine, secondLine);
                }

                // Explicit separators are authoring hints, not drawable glyphs. If an
                // authored split does not fit, let the finite resolver find another
                // complete two-line split without leaking a newline into a single-line
                // GUIStyle owner.
                message = normalized.Replace('\n', ' ');
            }

            var bestSplit = -1;
            var bestBalance = float.PositiveInfinity;
            for (var split = 1; split < message.Length; split++)
            {
                if (char.IsHighSurrogate(message[split - 1])
                    && char.IsLowSurrogate(message[split]))
                    continue;
                var firstLine = message.Substring(0, split);
                var secondLine = message.Substring(split);
                if (!FitsStatusLine(layout.Style, firstLine, layout.FirstLineRect)
                    || !FitsStatusLine(layout.Style, secondLine, layout.SecondLineRect))
                    continue;
                var firstWidth = MeasureStatusLineWidth(layout.Style, firstLine);
                var secondWidth = MeasureStatusLineWidth(layout.Style, secondLine);
                var balance = Mathf.Abs(
                    firstWidth / Mathf.Max(1f, layout.FirstLineRect.width)
                    - secondWidth / Mathf.Max(1f, layout.SecondLineRect.width));
                if (balance >= bestBalance) continue;
                bestBalance = balance;
                bestSplit = split;
            }

            return bestSplit < 0
                ? new RuntimeUiStatusTextLines(message)
                : new RuntimeUiStatusTextLines(message.Substring(0, bestSplit),
                    message.Substring(bestSplit));
        }

        private static void DrawStatusCore(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiStatusTextLines lines, RuntimeUiInteractionState state,
            RuntimeUiTypographyRole textRole, RuntimeUiStatusTextMode textMode,
            bool emphasized)
        {
            context = Require(context);
            var visualState = ResolveStatusVisualState(state, emphasized);
            DrawSlotArt(context, rect, RuntimeUiArtSlot.SurfaceStatus, visualState);
            var layout = ResolveStatusTextLayout(
                context, rect, state, textRole, textMode, emphasized);
            var indicatorSlot = StateIndicatorSlot(state);
            if (layout.HasIndicator && indicatorSlot.HasValue)
            {
                DrawSlotArt(context, layout.IndicatorRect, indicatorSlot.Value,
                    RuntimeUiInteractionState.Normal);
            }

            DrawTextCore(context, layout.FirstLineRect, lines.FirstLine, textRole,
                RuntimeUiTextTone.State, TextAnchor.MiddleLeft, visualState,
                textMode != RuntimeUiStatusTextMode.Standard,
                layout.Style);
            if (textMode == RuntimeUiStatusTextMode.CompactTwoLines
                && lines.HasSecondLine)
            {
                DrawTextCore(context, layout.SecondLineRect, lines.SecondLine, textRole,
                    RuntimeUiTextTone.State, TextAnchor.MiddleLeft, visualState,
                    true, layout.Style);
            }
        }

        private static bool FitsStatusLine(GUIStyle style, string text, Rect rect)
        {
            try
            {
                StatusMeasurementContent.text = text;
                var measured = style.CalcSize(StatusMeasurementContent);
                return measured.x <= rect.width + PixelRoundingTolerance
                    && measured.y <= rect.height + PixelRoundingTolerance;
            }
            finally
            {
                StatusMeasurementContent.text = string.Empty;
            }
        }

        private static float MeasureStatusLineWidth(GUIStyle style, string text)
        {
            try
            {
                StatusMeasurementContent.text = text;
                return style.CalcSize(StatusMeasurementContent).x;
            }
            finally
            {
                StatusMeasurementContent.text = string.Empty;
            }
        }

        public static void DrawDetailCard(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStatefulSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfaceDetail, state);
        }

        public static void DrawBlockingModal(RuntimeUiDrawContext context, Rect scrimRect,
            Rect modalRect, RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            DrawSlotArt(context, scrimRect, RuntimeUiArtSlot.SurfaceScrim,
                RuntimeUiInteractionState.Normal, context.Theme.Feedback.ScrimOpacity,
                context.Theme.Colors.Scrim);
            DrawSlotArt(context, modalRect, RuntimeUiArtSlot.SurfaceModal,
                ResolveSurfaceVisualState(state));
        }

        public static void DrawResultCard(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            DrawSlotArt(context, rect, RuntimeUiArtSlot.SurfaceResult,
                ResolveSurfaceVisualState(state));
        }

        public static void DrawText(RuntimeUiDrawContext context, Rect rect, string text,
            RuntimeUiTypographyRole role, RuntimeUiTextTone tone, TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            rect = context.ContentRect(rect, state);
            DrawTextCore(context, rect, text, role, tone, alignment, state);
        }

        public static Rect ResolveTextContentRect(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            return Require(context).ContentRect(rect, state);
        }

        public static Rect ResolveSingleLineTextRect(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiTypographyRole role, TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            var content = context.ContentRect(rect, state);
            return ResolveSingleLineDrawRect(content,
                context.Styles.SingleLineText(role, alignment), alignment);
        }

        public static float ResolveTextOpacity(RuntimeUiDrawContext context,
            RuntimeUiInteractionState state)
        {
            return Require(context).TextOpacity(state);
        }

        public static void DrawSingleLineText(RuntimeUiDrawContext context, Rect rect,
            string text, RuntimeUiTypographyRole role, RuntimeUiTextTone tone,
            TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            rect = context.ContentRect(rect, state);
            DrawTextCore(context, rect, text, role, tone, alignment, state, true);
        }

        public static bool IsApprovedEmphasisRole(RuntimeUiTypographyRole role)
        {
            return role == RuntimeUiTypographyRole.Display
                || role == RuntimeUiTypographyRole.ScreenTitle
                || role == RuntimeUiTypographyRole.SectionTitle;
        }

        public static RuntimeUiEmphasisTextLayout ResolveEmphasisTextLayout(
            RuntimeUiDrawContext context, Rect rect, RuntimeUiTypographyRole role,
            RuntimeUiTextTone tone, TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            if (!IsApprovedEmphasisRole(role))
            {
                throw new ArgumentOutOfRangeException(nameof(role), role,
                    "True outline is limited to approved display, screen-title, and section-title roles.");
            }

            var style = context.Styles.SingleLineText(role, alignment);
            var textRect = ResolveSingleLineDrawRect(
                context.ContentRect(rect, state), style, alignment);
            var outlinePixels = Mathf.Max(1,
                Mathf.RoundToInt(context.Scaled(EmphasisOutlineWidthLogical)));
            var outlinedRect = new Rect(
                textRect.x - outlinePixels,
                textRect.y - outlinePixels,
                textRect.width + outlinePixels * 2f,
                textRect.height + outlinePixels * 2f);
            var fillColor = context.TextColor(tone, state);
            fillColor.a *= context.TextOpacity(state);
            var outlineColor = tone == RuntimeUiTextTone.Inverse
                ? context.Theme.Colors.Outline
                : context.Theme.Colors.InverseText;
            outlineColor.a *= context.TextOpacity(state);
            return new RuntimeUiEmphasisTextLayout(textRect, outlinedRect, style,
                outlinePixels, fillColor, outlineColor);
        }

        public static void DrawEmphasisText(RuntimeUiDrawContext context, Rect rect,
            string text, RuntimeUiTypographyRole role, RuntimeUiTextTone tone,
            TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            var layout = ResolveEmphasisTextLayout(
                context, rect, role, tone, alignment, state);
            RequireOpaqueEmphasisComposition(
                GUI.color.a, layout.FillColor, layout.OutlineColor);
            for (var y = -layout.OutlinePixels; y <= layout.OutlinePixels; y++)
            {
                for (var x = -layout.OutlinePixels; x <= layout.OutlinePixels; x++)
                {
                    if (x == 0 && y == 0) continue;
                    DrawTextCore(context,
                         new Rect(layout.TextRect.x + x, layout.TextRect.y + y,
                             layout.TextRect.width, layout.TextRect.height),
                         text, role, tone, alignment, state,
                         false, layout.Style, layout.OutlineColor);
                }
            }
            DrawTextCore(context, layout.TextRect, text, role, tone, alignment,
                state, false, layout.Style, layout.FillColor);
        }

        private static void RequireOpaqueEmphasisComposition(float callerGuiAlpha,
            Color fillColor, Color outlineColor)
        {
            if (!Mathf.Approximately(callerGuiAlpha, 1f)
                || !Mathf.Approximately(fillColor.a, 1f)
                || !Mathf.Approximately(outlineColor.a, 1f))
            {
                throw new InvalidOperationException(
                    "True-outline emphasis is an opaque composition. Gate its visibility "
                    + "before drawing or fade one already-composited layer instead of "
                    + "applying alpha to repeated outline passes.");
            }
        }

        public static RuntimeUiStatusTextLayout ResolveControlledTwoLineTextLayout(
            RuntimeUiDrawContext context, Rect rect, RuntimeUiTypographyRole role,
            TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            var content = context.ContentRect(rect, state);
            var firstLineHeight = content.height * .5f;
            var firstLine = new Rect(content.x, content.y,
                content.width, firstLineHeight);
            var secondLine = new Rect(content.x, content.y + firstLineHeight,
                content.width, Mathf.Max(0f, content.height - firstLineHeight));
            return new RuntimeUiStatusTextLayout(firstLine, secondLine, default,
                false, context.Styles.CompactTwoLineText(role, alignment), 2);
        }

        public static void DrawControlledTwoLineText(RuntimeUiDrawContext context,
            Rect rect, RuntimeUiStatusTextLines lines, RuntimeUiTypographyRole role,
            RuntimeUiTextTone tone, TextAnchor alignment,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            context = Require(context);
            var layout = ResolveControlledTwoLineTextLayout(
                context, rect, role, alignment, state);
            DrawTextCore(context, layout.FirstLineRect, lines.FirstLine, role, tone,
                alignment, state, true, layout.Style);
            DrawTextCore(context, layout.SecondLineRect, lines.SecondLine, role, tone,
                alignment, state, true, layout.Style);
        }

        private static void DrawTextCore(RuntimeUiDrawContext context, Rect rect, string text,
            RuntimeUiTypographyRole role, RuntimeUiTextTone tone, TextAnchor alignment,
            RuntimeUiInteractionState state, bool singleLine = false,
            GUIStyle explicitStyle = null, Color? colorOverride = null)
        {
            var color = colorOverride ?? context.TextColor(tone, state);
            if (!colorOverride.HasValue)
                color.a *= context.TextOpacity(state);
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            try
            {
                GUI.color = color;
                GUI.contentColor = Color.white;
                var style = explicitStyle ?? (singleLine
                    ? context.Styles.SingleLineText(role, alignment)
                    : context.Styles.Text(role, alignment));
                if (singleLine)
                    rect = ResolveSingleLineDrawRect(rect, style, alignment);
                GUI.Label(rect, text ?? string.Empty, style);
            }
            finally
            {
                GUI.contentColor = previousContentColor;
                GUI.color = previousColor;
            }
        }

        private static Rect ResolveSingleLineDrawRect(Rect owner, GUIStyle style,
            TextAnchor alignment)
        {
            var requestedHeight = style == null || style.fixedHeight <= 0f
                ? owner.height : style.fixedHeight;
            var height = Mathf.Max(0f, requestedHeight);
            if (owner.height + PixelRoundingTolerance < height)
            {
                throw new InvalidOperationException(
                    "Single-line owner is shorter than its semantic line height. owner="
                    + owner + " requiredHeight=" + height);
            }
            float y;
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    y = owner.yMin;
                    break;
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    y = owner.center.y - height * .5f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    y = owner.yMax - height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }
            return new Rect(owner.x, y, owner.width, height);
        }

        private static RuntimeUiInteractionState ResolveStatusVisualState(
            RuntimeUiInteractionState state, bool emphasized)
        {
            return emphasized && state != RuntimeUiInteractionState.Disabled
                ? RuntimeUiInteractionState.Pressed
                : ResolveSurfaceVisualState(state);
        }

        private static Rect CenterSquare(Rect rect, float size)
        {
            size = Mathf.Min(size, Mathf.Min(Mathf.Max(0f, rect.width),
                Mathf.Max(0f, rect.height)));
            return new Rect(rect.x + (rect.width - size) * .5f,
                rect.y + (rect.height - size) * .5f, size, size);
        }

        private static Vector2 MeasureSingleLine(RuntimeUiDrawContext context,
            RuntimeUiTypographyRole role, string text, TextAnchor alignment,
            GUIContent measurementContent)
        {
            try
            {
                measurementContent.text = text ?? string.Empty;
                return context.Styles.SingleLineText(role, alignment)
                    .CalcSize(measurementContent);
            }
            finally
            {
                measurementContent.text = string.Empty;
            }
        }

    }
}
