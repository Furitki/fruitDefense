using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public enum RuntimeUiActionKind
    {
        Primary = 0,
        Secondary = 1,
        Quiet = 2,
        Danger = 3,
    }

    public enum RuntimeUiSlotKind
    {
        Tool = 0,
        Nursery = 1,
    }

    public enum RuntimeUiIndicatorKind
    {
        Selected = 0,
        Disabled = 1,
        Loading = 2,
        Success = 3,
        Warning = 4,
        Error = 5,
        DragLegal = 6,
        DragIllegal = 7,
        Merge = 8,
        Swap = 9,
    }

    public enum RuntimeUiLobbyThumbnail
    {
        Orchard01 = 0,
        Orchard02 = 1,
        Orchard03 = 2,
    }

    public enum RuntimeUiTextTone
    {
        Primary = 0,
        Secondary = 1,
        Inverse = 2,
        State = 3,
    }

    public enum RuntimeUiStatusTextMode
    {
        Standard = 0,
        SingleLine = 1,
        CompactTwoLines = 2,
    }

    public readonly struct RuntimeUiStatusTextLayout
    {
        internal RuntimeUiStatusTextLayout(Rect firstLineRect, Rect secondLineRect,
            Rect indicatorRect,
            bool hasIndicator, GUIStyle style, int maximumLineCount)
        {
            FirstLineRect = firstLineRect;
            SecondLineRect = secondLineRect;
            IndicatorRect = indicatorRect;
            HasIndicator = hasIndicator;
            Style = style;
            MaximumLineCount = maximumLineCount;
        }

        public Rect FirstLineRect { get; }
        public Rect SecondLineRect { get; }
        public Rect IndicatorRect { get; }
        public bool HasIndicator { get; }
        public GUIStyle Style { get; }
        public int MaximumLineCount { get; }
    }

    public readonly struct RuntimeUiActionContentLayout
    {
        internal RuntimeUiActionContentLayout(Rect contentRect, Rect iconRect,
            Rect iconVisualRect, Rect labelRect, Rect groupRect,
            bool hasIcon, bool hasLabel)
        {
            ContentRect = contentRect;
            IconRect = iconRect;
            IconVisualRect = iconVisualRect;
            LabelRect = labelRect;
            GroupRect = groupRect;
            HasIcon = hasIcon;
            HasLabel = hasLabel;
        }

        public Rect ContentRect { get; }
        public Rect IconRect { get; }
        public Rect IconVisualRect { get; }
        public Rect LabelRect { get; }
        public Rect GroupRect { get; }
        public bool HasIcon { get; }
        public bool HasLabel { get; }
    }

    public readonly struct RuntimeUiInlineContentLayout
    {
        internal RuntimeUiInlineContentLayout(Rect contentRect, Rect iconRect,
            Rect iconVisualRect, Rect labelRect, Rect groupRect)
        {
            ContentRect = contentRect;
            IconRect = iconRect;
            IconVisualRect = iconVisualRect;
            LabelRect = labelRect;
            GroupRect = groupRect;
        }

        public Rect ContentRect { get; }
        public Rect IconRect { get; }
        public Rect IconVisualRect { get; }
        public Rect LabelRect { get; }
        public Rect GroupRect { get; }
    }

    public readonly struct RuntimeUiMetricContentLayout
    {
        internal RuntimeUiMetricContentLayout(Rect contentRect, Rect iconRect,
            Rect iconVisualRect, Rect valueRect, Rect labelRect, Rect groupRect)
        {
            ContentRect = contentRect;
            IconRect = iconRect;
            IconVisualRect = iconVisualRect;
            ValueRect = valueRect;
            LabelRect = labelRect;
            GroupRect = groupRect;
        }

        public Rect ContentRect { get; }
        public Rect IconRect { get; }
        public Rect IconVisualRect { get; }
        public Rect ValueRect { get; }
        public Rect LabelRect { get; }
        public Rect GroupRect { get; }
    }

    public readonly struct RuntimeUiStatusTextLines
    {
        public RuntimeUiStatusTextLines(string firstLine, string secondLine = null)
        {
            FirstLine = firstLine ?? string.Empty;
            SecondLine = secondLine ?? string.Empty;
        }

        public string FirstLine { get; }
        public string SecondLine { get; }
        public bool HasSecondLine => !string.IsNullOrEmpty(SecondLine);
    }

    public readonly struct RuntimeUiGuiCacheKey : IEquatable<RuntimeUiGuiCacheKey>
    {
        public RuntimeUiGuiCacheKey(string themeId, string themeRevision,
            string artSetId, string artSetRevision, int scaleMilli)
        {
            ThemeId = themeId;
            ThemeRevision = themeRevision;
            ArtSetId = artSetId;
            ArtSetRevision = artSetRevision;
            ScaleMilli = scaleMilli;
        }

        public string ThemeId { get; }
        public string ThemeRevision { get; }
        public string ArtSetId { get; }
        public string ArtSetRevision { get; }
        public int ScaleMilli { get; }

        public bool Equals(RuntimeUiGuiCacheKey other)
        {
            return string.Equals(ThemeId, other.ThemeId, StringComparison.Ordinal)
                && string.Equals(ThemeRevision, other.ThemeRevision, StringComparison.Ordinal)
                && string.Equals(ArtSetId, other.ArtSetId, StringComparison.Ordinal)
                && string.Equals(ArtSetRevision, other.ArtSetRevision, StringComparison.Ordinal)
                && ScaleMilli == other.ScaleMilli;
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeUiGuiCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (ThemeId == null ? 0 : ThemeId.GetHashCode());
                hash = hash * 31 + (ThemeRevision == null ? 0 : ThemeRevision.GetHashCode());
                hash = hash * 31 + (ArtSetId == null ? 0 : ArtSetId.GetHashCode());
                hash = hash * 31 + (ArtSetRevision == null ? 0 : ArtSetRevision.GetHashCode());
                return hash * 31 + ScaleMilli;
            }
        }

        public static bool operator ==(RuntimeUiGuiCacheKey left, RuntimeUiGuiCacheKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeUiGuiCacheKey left, RuntimeUiGuiCacheKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ThemeId + "@" + ThemeRevision + "/" + ArtSetId + "@"
                + ArtSetRevision + "/" + ScaleMilli;
        }
    }

    public sealed class RuntimeUiGuiStyleCache
    {
        private const int TypographyRoleCount = 7;
        private const int TextAnchorCount = 9;

        private readonly GUIStyle[,] textStyles =
            new GUIStyle[TypographyRoleCount, TextAnchorCount];
        private readonly GUIStyle[,] singleLineTextStyles =
            new GUIStyle[TypographyRoleCount, TextAnchorCount];
        private readonly GUIStyle[,] compactTwoLineTextStyles =
            new GUIStyle[TypographyRoleCount, TextAnchorCount];
        private readonly float[] scaledFontHeights = new float[TypographyRoleCount];
        private readonly float[] scaledLineHeights = new float[TypographyRoleCount];

        internal RuntimeUiGuiStyleCache(RuntimeUiTheme theme,
            RuntimeUiGuiCacheKey key, float scale)
        {
            Key = key;
            HitTarget = CreateHitTargetStyle();
            for (var roleIndex = 0; roleIndex < TypographyRoleCount; roleIndex++)
            {
                var role = (RuntimeUiTypographyRole)roleIndex;
                var token = theme.Typography.For(role);
                scaledFontHeights[roleIndex] =
                    Mathf.Max(1f, Mathf.Round(token.FontSize * scale));
                scaledLineHeights[roleIndex] =
                    Mathf.Max(1f, Mathf.Round(token.LineHeight * scale));
                for (var anchorIndex = 0; anchorIndex < TextAnchorCount; anchorIndex++)
                {
                    textStyles[roleIndex, anchorIndex] = CreateTextStyle(
                        theme.PackagedChineseFont, token, scale, (TextAnchor)anchorIndex);
                }
            }
        }

        public RuntimeUiGuiCacheKey Key { get; }
        public GUIStyle HitTarget { get; }

        internal GUIStyle Text(RuntimeUiTypographyRole role, TextAnchor alignment)
        {
            var roleIndex = (int)role;
            var anchorIndex = (int)alignment;
            if (roleIndex < 0 || roleIndex >= TypographyRoleCount)
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
            if (anchorIndex < 0 || anchorIndex >= TextAnchorCount)
                throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            return textStyles[roleIndex, anchorIndex];
        }

        public GUIStyle SingleLineText(RuntimeUiTypographyRole role, TextAnchor alignment)
        {
            var roleIndex = (int)role;
            var anchorIndex = (int)alignment;
            if (roleIndex < 0 || roleIndex >= TypographyRoleCount)
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
            if (anchorIndex < 0 || anchorIndex >= TextAnchorCount)
                throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);

            var style = singleLineTextStyles[roleIndex, anchorIndex];
            if (style != null)
                return style;

            style = new GUIStyle(textStyles[roleIndex, anchorIndex])
            {
                clipping = TextClipping.Clip,
                wordWrap = false,
                fixedHeight = scaledFontHeights[roleIndex],
            };
            singleLineTextStyles[roleIndex, anchorIndex] = style;
            return style;
        }

        public GUIStyle CompactTwoLineText(RuntimeUiTypographyRole role,
            TextAnchor alignment)
        {
            var roleIndex = (int)role;
            var anchorIndex = (int)alignment;
            if (roleIndex < 0 || roleIndex >= TypographyRoleCount)
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
            if (anchorIndex < 0 || anchorIndex >= TextAnchorCount)
                throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);

            var style = compactTwoLineTextStyles[roleIndex, anchorIndex];
            if (style != null)
                return style;

            style = new GUIStyle(textStyles[roleIndex, anchorIndex])
            {
                clipping = TextClipping.Clip,
                wordWrap = false,
                fixedHeight = scaledLineHeights[roleIndex],
            };
            compactTwoLineTextStyles[roleIndex, anchorIndex] = style;
            return style;
        }

        private static GUIStyle CreateHitTargetStyle()
        {
            return new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                clipping = TextClipping.Clip,
                richText = false,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
            };
        }

        private static GUIStyle CreateTextStyle(Font font, RuntimeUiTypographyStyle token,
            float scale, TextAnchor alignment)
        {
            var style = new GUIStyle
            {
                font = font,
                fontSize = Mathf.Max(1, Mathf.RoundToInt(token.FontSize * scale)),
                fontStyle = token.FontStyle,
                alignment = alignment,
                contentOffset = new Vector2(0f,
                    Mathf.Round(token.OpticalOffsetY * scale)),
                imagePosition = ImagePosition.TextOnly,
                clipping = TextClipping.Clip,
                richText = false,
                wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
            };
            SetAllTextColors(style, Color.white);
            return style;
        }

        private static void SetAllTextColors(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }
    }

    public sealed class RuntimeUiDrawContext
    {
        private readonly RuntimeUiArtBinding[] bindingCache;

        private RuntimeUiDrawContext(RuntimeUiTheme theme, float scale,
            RuntimeUiGuiCacheKey cacheKey)
        {
            Theme = theme;
            ArtSet = theme.ActiveArtSet;
            Scale = scale;
            CacheKey = cacheKey;
            bindingCache = BuildBindingCache(ArtSet);
            Styles = new RuntimeUiGuiStyleCache(theme, cacheKey, scale);
        }

        public RuntimeUiTheme Theme { get; }
        public RuntimeUiArtSet ArtSet { get; }
        public float Scale { get; }
        public RuntimeUiGuiCacheKey CacheKey { get; }
        public RuntimeUiGuiStyleCache Styles { get; }

        public static RuntimeUiDrawContext Create(RuntimeUiTheme theme, float scale)
        {
            var scaleMilli = ScaleMilli(scale);
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            var validation = theme.Validate();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot build runtime UI drawing context: "
                    + validation.FirstIssueOr("invalid theme"));
            }

            var effectiveScale = scaleMilli / 1000f;
            var artSet = theme.ActiveArtSet;
            var key = new RuntimeUiGuiCacheKey(theme.ThemeId, theme.Revision,
                artSet.SetId, artSet.Revision, scaleMilli);
            return new RuntimeUiDrawContext(theme, effectiveScale, key);
        }

        public static RuntimeUiDrawContext Require(RuntimeUiDrawContext current,
            RuntimeUiTheme theme, float scale)
        {
            if (current != null && current.IsCurrent(theme, scale))
                return current;
            return Create(theme, scale);
        }

        public bool IsCurrent(RuntimeUiTheme theme, float scale)
        {
            if (theme == null || theme.ActiveArtSet == null)
                return false;
            if (!ReferenceEquals(Theme, theme)
                || !ReferenceEquals(ArtSet, theme.ActiveArtSet))
                return false;

            var scaleMilli = TryScaleMilli(scale);
            if (scaleMilli <= 0)
                return false;
            var key = new RuntimeUiGuiCacheKey(theme.ThemeId, theme.Revision,
                theme.ActiveArtSet.SetId, theme.ActiveArtSet.Revision, scaleMilli);
            return CacheKey == key;
        }

        public float Scaled(float logicalValue)
        {
            return logicalValue * Scale;
        }

        public float ScaledLineHeight(RuntimeUiTypographyRole role)
        {
            return Mathf.Round(Theme.Typography.For(role).LineHeight * Scale);
        }

        internal RuntimeUiArtBinding RequiredBinding(RuntimeUiArtSlot slot)
        {
            var requiredIndex = RuntimeUiArtSlots.RequiredIndex(slot);
            if (requiredIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot,
                    "The UI art slot is not part of the finite runtime contract.");
            }

            var binding = bindingCache[requiredIndex];
            if (binding == null)
            {
                throw new InvalidOperationException(
                    "Runtime UI art slot '" + RuntimeUiArtSlots.SemanticId(slot)
                    + "' is unavailable in drawing context '" + CacheKey + "'.");
            }
            return binding;
        }

        private static RuntimeUiArtBinding[] BuildBindingCache(RuntimeUiArtSet artSet)
        {
            var cache = new RuntimeUiArtBinding[RuntimeUiArtSlots.RequiredCount];
            var bindings = artSet.Bindings;
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                if (binding == null)
                    continue;
                var requiredIndex = RuntimeUiArtSlots.RequiredIndex(binding.Slot);
                if (requiredIndex < 0 || cache[requiredIndex] != null)
                {
                    throw new InvalidOperationException(
                        "Cannot build a drawing cache from an invalid runtime UI art set.");
                }
                cache[requiredIndex] = binding;
            }

            for (var index = 0; index < cache.Length; index++)
            {
                if (cache[index] == null)
                {
                    throw new InvalidOperationException(
                        "Cannot build a drawing cache from an incomplete runtime UI art set.");
                }
            }
            return cache;
        }

        internal float Opacity(RuntimeUiInteractionState state)
        {
            switch (state)
            {
                case RuntimeUiInteractionState.Normal: return Theme.Feedback.NormalOpacity;
                case RuntimeUiInteractionState.HoveredOrFocused: return Theme.Feedback.FocusedOpacity;
                case RuntimeUiInteractionState.Pressed: return Theme.Feedback.PressedOpacity;
                case RuntimeUiInteractionState.Disabled: return Theme.Feedback.DisabledOpacity;
                case RuntimeUiInteractionState.Selected: return Theme.Feedback.SelectedOpacity;
                case RuntimeUiInteractionState.Loading: return Theme.Feedback.LoadingOpacity;
                case RuntimeUiInteractionState.Success:
                case RuntimeUiInteractionState.Warning:
                case RuntimeUiInteractionState.Error:
                    return Theme.Feedback.NormalOpacity;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        internal float TextOpacity(RuntimeUiInteractionState state)
        {
            // Loading/disabled opacity belongs to decorative surfaces. Player-readable
            // copy remains fully legible while its independent semantic indicator carries
            // the non-color state cue.
            if (state == RuntimeUiInteractionState.Loading
                || state == RuntimeUiInteractionState.Disabled)
                return Theme.Feedback.NormalOpacity;
            return Opacity(state);
        }

        internal Color Tint(RuntimeUiInteractionState state)
        {
            switch (state)
            {
                case RuntimeUiInteractionState.Normal:
                case RuntimeUiInteractionState.Pressed:
                case RuntimeUiInteractionState.Loading:
                    return Color.white;
                case RuntimeUiInteractionState.HoveredOrFocused:
                case RuntimeUiInteractionState.Selected:
                    return Theme.Colors.SelectionAccent;
                case RuntimeUiInteractionState.Disabled:
                    return Theme.Colors.Disabled;
                case RuntimeUiInteractionState.Success:
                    return Theme.Colors.Success;
                case RuntimeUiInteractionState.Warning:
                    return Theme.Colors.Warning;
                case RuntimeUiInteractionState.Error:
                    return Theme.Colors.Danger;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        internal Color TextColor(RuntimeUiTextTone tone, RuntimeUiInteractionState state)
        {
            switch (tone)
            {
                case RuntimeUiTextTone.Primary: return Theme.Colors.PrimaryText;
                case RuntimeUiTextTone.Secondary: return Theme.Colors.SecondaryText;
                case RuntimeUiTextTone.Inverse: return Theme.Colors.InverseText;
                case RuntimeUiTextTone.State: return Theme.Colors.PrimaryText;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tone), tone, null);
            }
        }

        internal Rect ContentRect(Rect rect, RuntimeUiInteractionState state)
        {
            if (state != RuntimeUiInteractionState.Pressed)
                return rect;
            rect.y += Scaled(Theme.Metrics.PressedOffset);
            return rect;
        }

        private static int ScaleMilli(float scale)
        {
            var value = TryScaleMilli(scale);
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale,
                    "Runtime UI scale must be a finite positive value of at least 0.001.");
            }

            return value;
        }

        private static int TryScaleMilli(float scale)
        {
            if (!RuntimeUiNumbers.IsFinite(scale) || scale < .001f
                || scale > int.MaxValue / 1000f)
                return 0;
            return Mathf.RoundToInt(scale * 1000f);
        }
    }

    public static class RuntimeUiGui
    {
        private static readonly GUIContent StatusMeasurementContent = new GUIContent();
        private static readonly GUIContent ActionMeasurementContent = new GUIContent();
        private static readonly GUIContent MetricMeasurementContent = new GUIContent();

        public static RuntimeUiDrawContext RequireContext(RuntimeUiDrawContext current,
            RuntimeUiTheme theme, float scale)
        {
            return RuntimeUiDrawContext.Require(current, theme, scale);
        }

        public static void DrawScreenBackground(RuntimeUiDrawContext context, Rect rect)
        {
            DrawSlotArt(Require(context), rect,
                RuntimeUiArtSlot.SurfaceScreenBackground, RuntimeUiInteractionState.Normal);
        }

        public static void DrawSafeArea(RuntimeUiDrawContext context, Rect rect)
        {
            DrawSlotArt(Require(context), rect,
                RuntimeUiArtSlot.SurfaceSafeArea, RuntimeUiInteractionState.Normal);
        }

        public static void DrawShellOrchardDepth(RuntimeUiDrawContext context, Rect rect,
            float opacity)
        {
            DrawAspectFillSlotArt(Require(context), rect,
                RuntimeUiArtSlot.IllustrationShellOrchardDepth,
                RuntimeUiInteractionState.Normal, Mathf.Clamp01(opacity));
        }

        public static void DrawScreenCorners(RuntimeUiDrawContext context, Rect safeRect)
        {
            context = Require(context);
            var visualSize = context.Scaled(context.Theme.Metrics.SpacingXl * 2f);
            var width = Mathf.Min(Mathf.Max(0f, visualSize),
                Mathf.Max(0f, safeRect.width * .5f));
            var height = Mathf.Min(Mathf.Max(0f, visualSize),
                Mathf.Max(0f, safeRect.height * .5f));
            if (width <= 0f || height <= 0f)
                return;

            DrawSlotArt(context, new Rect(safeRect.xMin, safeRect.yMin, width, height),
                RuntimeUiArtSlot.OrnamentScreenCorner, RuntimeUiInteractionState.Normal);
            DrawSlotArt(context, new Rect(safeRect.xMax - width, safeRect.yMin, width, height),
                RuntimeUiArtSlot.OrnamentScreenCorner, RuntimeUiInteractionState.Normal,
                1f, null, true, false);
            DrawSlotArt(context, new Rect(safeRect.xMin, safeRect.yMax - height, width, height),
                RuntimeUiArtSlot.OrnamentScreenCorner, RuntimeUiInteractionState.Normal,
                1f, null, false, true);
            DrawSlotArt(context, new Rect(safeRect.xMax - width, safeRect.yMax - height,
                    width, height), RuntimeUiArtSlot.OrnamentScreenCorner,
                RuntimeUiInteractionState.Normal, 1f, null, true, true);
        }

        public static void DrawSectionRibbon(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawSlotArt(Require(context), rect, RuntimeUiArtSlot.SurfaceSectionRibbon,
                ResolveSurfaceVisualState(state));
        }

        public static void DrawIllustrationFrame(RuntimeUiDrawContext context, Rect rect)
        {
            DrawSlotArt(Require(context), rect, RuntimeUiArtSlot.SurfaceIllustrationFrame,
                RuntimeUiInteractionState.Normal);
        }

        public static void DrawMetricDivider(RuntimeUiDrawContext context, Rect rect)
        {
            DrawAspectFitSlotArt(Require(context), rect,
                RuntimeUiArtSlot.OrnamentMetricDivider, RuntimeUiInteractionState.Normal);
        }

        public static void DrawResultBanner(RuntimeUiDrawContext context, Rect rect)
        {
            DrawOpticalEnvelopeFitSlotArt(Require(context), rect,
                RuntimeUiArtSlot.OrnamentResultBanner, RuntimeUiInteractionState.Normal);
        }

        public static void DrawOrchardVista(RuntimeUiDrawContext context, Rect rect)
        {
            DrawAspectFillSlotArt(Require(context), rect,
                RuntimeUiArtSlot.IllustrationOrchardVista,
                RuntimeUiInteractionState.Normal, 1f);
        }

        public static void DrawLobbyThumbnail(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiLobbyThumbnail thumbnail)
        {
            DrawAspectFitSlotArt(Require(context), rect, LobbyThumbnailSlot(thumbnail),
                RuntimeUiInteractionState.Normal);
        }

        public static void DrawStandardPanel(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStatefulSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfacePanelStandard, state);
        }

        public static void DrawRaisedPanel(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStatefulSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfacePanelRaised, state);
        }

        public static void DrawSelectableCard(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state, bool emphasized = false,
            bool drawStateIndicator = true,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            var visualRect = motion.Transform(rect);
            var previousColor = ApplyMotionAlpha(motion);
            try
            {
                DrawSlotArt(context, visualRect, RuntimeUiArtSlot.SurfaceCardSelectable,
                    emphasized
                        && state != RuntimeUiInteractionState.Disabled
                        ? RuntimeUiInteractionState.Pressed
                        : ResolveSurfaceVisualState(state));
                if (drawStateIndicator)
                    DrawStateIndicator(context, visualRect, state);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static void DrawSlot(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiSlotKind kind, RuntimeUiInteractionState state,
            bool emphasized = false)
        {
            var slot = kind == RuntimeUiSlotKind.Tool
                ? RuntimeUiArtSlot.SlotTool
                : kind == RuntimeUiSlotKind.Nursery
                    ? RuntimeUiArtSlot.SlotNursery
                    : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            context = Require(context);
            DrawSlotArt(context, rect, slot,
                emphasized && state != RuntimeUiInteractionState.Disabled
                    ? RuntimeUiInteractionState.Pressed
                    : ResolveSurfaceVisualState(state));
            DrawStateIndicator(context, rect, state);
        }

        public static bool DrawAction(RuntimeUiDrawContext context, Rect rect, string label,
            RuntimeUiActionKind kind, RuntimeUiInteractionState state,
            RuntimeUiArtSlot? iconSlot = null,
            RuntimeUiTypographyRole labelRole = RuntimeUiTypographyRole.ControlLabel,
            bool emphasized = false,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            DrawActionVisual(context, rect, label, kind, state, iconSlot,
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
            string label, RuntimeUiActionKind kind, RuntimeUiInteractionState state,
            RuntimeUiArtSlot? iconSlot = null,
            RuntimeUiTypographyRole labelRole = RuntimeUiTypographyRole.ControlLabel,
            bool emphasized = false,
            RuntimeUiMotionSample motion = default)
        {
            context = Require(context);
            var heldMotion = state == RuntimeUiInteractionState.Pressed
                ? RuntimeUiMotion.HeldPress(context.Theme.Feedback)
                : RuntimeUiMotionSample.Rest;
            var visualMotion = RuntimeUiMotionSample.Combine(motion, heldMotion);
            var visualRect = visualMotion.Transform(rect);
            var artSlot = ActionSlot(kind);
            var visualState = ResolveActionDrawState(kind, state, emphasized);
            var previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                previousColor.a * visualMotion.Alpha);
            try
            {
                DrawSlotArt(context, visualRect, artSlot, visualState);

                var contentLayout = ResolveActionContentLayout(context, visualRect, label,
                    kind, state, iconSlot, labelRole, emphasized);
                if (iconSlot.HasValue && contentLayout.HasIcon)
                {
                    RequireIconSlot(iconSlot.Value);
                    DrawSlotArt(context, contentLayout.IconRect,
                        iconSlot.Value, visualState);
                }

                if (contentLayout.HasLabel)
                {
                    var tone = ResolveActionTextTone(kind, state);
                    DrawTextCore(context, contentLayout.LabelRect, label, labelRole,
                        tone, TextAnchor.MiddleCenter, visualState, true,
                        context.Styles.SingleLineText(labelRole, TextAnchor.MiddleCenter));
                }

                DrawStateIndicator(context, visualRect, state);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        public static RuntimeUiActionContentLayout ResolveActionContentLayout(
            RuntimeUiDrawContext context, Rect rect, string label,
            RuntimeUiActionKind kind, RuntimeUiInteractionState state,
            RuntimeUiArtSlot? iconSlot,
            RuntimeUiTypographyRole labelRole = RuntimeUiTypographyRole.ControlLabel,
            bool emphasized = false)
        {
            context = Require(context);
            var visualState = ResolveActionDrawState(kind, state, emphasized);
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
                return new RuntimeUiActionContentLayout(contentRect, default,
                    default, labelRect, labelRect, false, hasLabel);
            }

            RequireIconSlot(iconSlot.Value);
            var desiredIconSize = Mathf.Min(contentRect.height,
                context.Scaled(context.Theme.Metrics.TouchTargetMinimum));
            if (!hasLabel)
            {
                var centeredIcon = CenterSquare(contentRect, desiredIconSize);
                var iconOnlyVisual = ResolveOpticalVisualRect(
                    context, iconSlot.Value, centeredIcon);
                return new RuntimeUiActionContentLayout(contentRect, centeredIcon,
                    iconOnlyVisual, default, iconOnlyVisual, true, false);
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
            return new RuntimeUiActionContentLayout(contentRect, iconRect,
                iconVisual, labelRectWithIcon,
                Union(iconVisual, labelRectWithIcon), true, true);
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
            return new RuntimeUiInlineContentLayout(contentRect, iconRect,
                iconVisual, labelRect, Union(iconVisual, labelRect));
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
            DrawSlotArt(context, layout.IconRect, iconSlot,
                RuntimeUiInteractionState.Normal);
            DrawTextCore(context, layout.LabelRect, label, labelRole, tone,
                TextAnchor.MiddleCenter, state, true);
        }

        public static RuntimeUiTextTone ResolveActionTextTone(
            RuntimeUiActionKind kind, RuntimeUiInteractionState state)
        {
            switch (kind)
            {
                case RuntimeUiActionKind.Primary:
                case RuntimeUiActionKind.Danger:
                    return state == RuntimeUiInteractionState.Disabled
                        ? RuntimeUiTextTone.Primary : RuntimeUiTextTone.Inverse;
                case RuntimeUiActionKind.Secondary:
                case RuntimeUiActionKind.Quiet:
                    return RuntimeUiTextTone.Primary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
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
            return new RuntimeUiMetricContentLayout(content, iconRect,
                iconVisual, valueRect, labelRect,
                Union(iconVisual, Union(labelRect, valueRect)));
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
            return new RuntimeUiMetricContentLayout(content, iconRect,
                iconVisual, valueRect, labelRect,
                Union(iconVisual, Union(valueRect, labelRect)));
        }

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
                return measured.x <= singleLine.FirstLineRect.width + .001f
                    && measured.y <= singleLine.FirstLineRect.height + .001f
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
                return measured.x <= rect.width + .001f
                    && measured.y <= rect.height + .001f;
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
            GUIStyle explicitStyle = null)
        {
            var color = context.TextColor(tone, state);
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

        public static void DrawIcon(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiArtSlot iconSlot,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            RequireIconSlot(iconSlot);
            DrawSlotArt(Require(context), rect, iconSlot, state);
        }

        public static void DrawIndicator(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiIndicatorKind kind)
        {
            DrawSlotArt(Require(context), rect, IndicatorSlot(kind),
                RuntimeUiInteractionState.Normal);
        }

        public static void DrawStateIndicator(RuntimeUiDrawContext context, Rect componentRect,
            RuntimeUiInteractionState state)
        {
            context = Require(context);
            var slot = StateIndicatorSlot(state);
            if (!slot.HasValue)
                return;

            if (!TryResolveStateIndicatorRect(context, componentRect,
                    out var rect))
                return;
            DrawSlotArt(context, rect, slot.Value, RuntimeUiInteractionState.Normal);
        }

        public static bool TryResolveStateIndicatorRect(RuntimeUiDrawContext context,
            Rect componentRect, out Rect rect)
        {
            context = Require(context);
            var inset = context.Scaled(context.Theme.Metrics.SpacingXs);
            var available = Mathf.Max(0f, componentRect.height - inset * 2f);
            var size = Mathf.Min(available,
                context.Scaled(context.Theme.Metrics.SpacingXl));
            if (size <= 0f || componentRect.width <= 0f)
            {
                rect = default;
                return false;
            }
            rect = new Rect(componentRect.xMax - inset - size,
                componentRect.y + (componentRect.height - size) * .5f,
                size, size);
            return true;
        }

        private static RuntimeUiDrawContext Require(RuntimeUiDrawContext context)
        {
            return context ?? throw new ArgumentNullException(nameof(context));
        }

        private static void DrawStatefulSurface(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiArtSlot slot, RuntimeUiInteractionState state)
        {
            DrawSlotArt(context, rect, slot, ResolveSurfaceVisualState(state));
            DrawStateIndicator(context, rect, state);
        }

        private static void DrawSlotArt(RuntimeUiDrawContext context, Rect destination,
            RuntimeUiArtSlot slot, RuntimeUiInteractionState state,
            float opacityMultiplier = 1f, Color? tintOverride = null,
            bool mirrorX = false, bool mirrorY = false)
        {
            var binding = context.RequiredBinding(slot);
            ResolveSource(binding, out var texture, out var sourcePixels);
            var tint = tintOverride ?? context.Tint(state);
            tint.a *= context.Opacity(state) * opacityMultiplier;

            var previousColor = GUI.color;
            try
            {
                GUI.color = tint;
                switch (binding.Geometry)
                {
                    case RuntimeUiArtGeometry.Stretch:
                    case RuntimeUiArtGeometry.Icon:
                        DrawSourceRect(destination, texture, sourcePixels, mirrorX, mirrorY);
                        break;
                    case RuntimeUiArtGeometry.NineSlice:
                        DrawNineSlice(destination, texture, sourcePixels,
                            binding.SliceBorder, binding.PixelsPerLogicalUnit, context.Scale);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(binding.Geometry),
                            binding.Geometry, null);
                }
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private static void ResolveSource(RuntimeUiArtBinding binding,
            out Texture2D texture, out Rect sourcePixels)
        {
            texture = binding.Texture;
            sourcePixels = binding.Sprite.rect;
        }

        private static void DrawAspectFitSlotArt(RuntimeUiDrawContext context,
            Rect destination, RuntimeUiArtSlot slot, RuntimeUiInteractionState state)
        {
            var binding = context.RequiredBinding(slot);
            var source = binding.Sprite.rect;
            if (source.width <= 0f || source.height <= 0f
                || destination.width <= 0f || destination.height <= 0f)
                return;
            var scale = Mathf.Min(destination.width / source.width,
                destination.height / source.height);
            var width = source.width * scale;
            var height = source.height * scale;
            var fitted = new Rect(destination.x + (destination.width - width) * .5f,
                destination.y + (destination.height - height) * .5f, width, height);
            DrawSlotArt(context, fitted, slot, state);
        }

        private static void DrawOpticalEnvelopeFitSlotArt(RuntimeUiDrawContext context,
            Rect opticalDestination, RuntimeUiArtSlot slot,
            RuntimeUiInteractionState state)
        {
            var drawRect = ResolveOpticalEnvelopeDrawRect(
                context, slot, opticalDestination);
            if (drawRect.width <= 0f || drawRect.height <= 0f)
                return;
            DrawSlotArt(context, drawRect, slot, state);
        }

        private static void DrawAspectFillSlotArt(RuntimeUiDrawContext context,
            Rect destination, RuntimeUiArtSlot slot, RuntimeUiInteractionState state,
            float opacityMultiplier)
        {
            var binding = context.RequiredBinding(slot);
            ResolveSource(binding, out var texture, out var source);
            if (source.width <= 0f || source.height <= 0f
                || destination.width <= 0f || destination.height <= 0f)
                return;

            var sourceRatio = source.width / source.height;
            var destinationRatio = destination.width / destination.height;
            if (sourceRatio > destinationRatio)
            {
                var width = source.height * destinationRatio;
                source.x += (source.width - width) * .5f;
                source.width = width;
            }
            else
            {
                var height = source.width / destinationRatio;
                source.y += (source.height - height) * .5f;
                source.height = height;
            }

            var previousColor = GUI.color;
            try
            {
                var tint = context.Tint(state);
                tint.a *= context.Opacity(state) * opacityMultiplier;
                GUI.color = tint;
                DrawSourceRect(destination, texture, source);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private static void DrawSourceRect(Rect destination, Texture texture, Rect sourcePixels,
            bool mirrorX = false, bool mirrorY = false)
        {
            var x = mirrorX ? sourcePixels.xMax : sourcePixels.xMin;
            var y = mirrorY ? sourcePixels.yMax : sourcePixels.yMin;
            var width = mirrorX ? -sourcePixels.width : sourcePixels.width;
            var height = mirrorY ? -sourcePixels.height : sourcePixels.height;
            var uv = new Rect(x / texture.width, y / texture.height,
                width / texture.width, height / texture.height);
            GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
        }

        private static void DrawNineSlice(Rect destination, Texture texture, Rect sourcePixels,
            RuntimeUiPixelInsets border, float pixelsPerLogicalUnit, float scale)
        {
            var left = border.Left / pixelsPerLogicalUnit * scale;
            var right = border.Right / pixelsPerLogicalUnit * scale;
            var top = border.Top / pixelsPerLogicalUnit * scale;
            var bottom = border.Bottom / pixelsPerLogicalUnit * scale;
            FitPair(ref left, ref right, Mathf.Max(0f, destination.width));
            FitPair(ref top, ref bottom, Mathf.Max(0f, destination.height));

            var dx0 = destination.xMin;
            var dx1 = destination.xMin + left;
            var dx2 = destination.xMax - right;
            var dx3 = destination.xMax;
            var dy0 = destination.yMin;
            var dy1 = destination.yMin + top;
            var dy2 = destination.yMax - bottom;
            var dy3 = destination.yMax;
            SnapNineSliceBoundaries(GUI.matrix,
                ref dx0, ref dx1, ref dx2, ref dx3,
                ref dy0, ref dy1, ref dy2, ref dy3);
            var sx0 = sourcePixels.xMin;
            var sx1 = sourcePixels.xMin + border.Left;
            var sx2 = sourcePixels.xMax - border.Right;
            var sx3 = sourcePixels.xMax;
            var sy0 = sourcePixels.yMax;
            var sy1 = sourcePixels.yMax - border.Top;
            var sy2 = sourcePixels.yMin + border.Bottom;
            var sy3 = sourcePixels.yMin;

            DrawNineSlicePatch(texture, dx0, dy0, dx1, dy1, sx0, sy1, sx1, sy0);
            DrawNineSlicePatch(texture, dx1, dy0, dx2, dy1, sx1, sy1, sx2, sy0);
            DrawNineSlicePatch(texture, dx2, dy0, dx3, dy1, sx2, sy1, sx3, sy0);
            DrawNineSlicePatch(texture, dx0, dy1, dx1, dy2, sx0, sy2, sx1, sy1);
            DrawNineSlicePatch(texture, dx1, dy1, dx2, dy2, sx1, sy2, sx2, sy1);
            DrawNineSlicePatch(texture, dx2, dy1, dx3, dy2, sx2, sy2, sx3, sy1);
            DrawNineSlicePatch(texture, dx0, dy2, dx1, dy3, sx0, sy3, sx1, sy2);
            DrawNineSlicePatch(texture, dx1, dy2, dx2, dy3, sx1, sy3, sx2, sy2);
            DrawNineSlicePatch(texture, dx2, dy2, dx3, dy3, sx2, sy3, sx3, sy2);
        }

        private static void SnapNineSliceBoundaries(Matrix4x4 guiMatrix,
            ref float destinationX0, ref float destinationX1,
            ref float destinationX2, ref float destinationX3,
            ref float destinationY0, ref float destinationY1,
            ref float destinationY2, ref float destinationY3)
        {
            // IMGUI rasterizes each target Rect independently. Snapping only an internal edge
            // still leaves a fractional outer origin, so the first patch can round its origin
            // and width independently and miss the last device-pixel column. Build one complete
            // device-pixel partition instead: outer and internal boundaries are snapped once,
            // mapped back to GUI space, and then shared verbatim by all nine patches.
            if (!IsAxisAlignedGuiMatrix(guiMatrix))
                return;

            destinationX0 = SnapGuiAxis(destinationX0, guiMatrix.m00, guiMatrix.m03);
            destinationX3 = Mathf.Max(destinationX0,
                SnapGuiAxis(destinationX3, guiMatrix.m00, guiMatrix.m03));
            destinationY0 = SnapGuiAxis(destinationY0, guiMatrix.m11, guiMatrix.m13);
            destinationY3 = Mathf.Max(destinationY0,
                SnapGuiAxis(destinationY3, guiMatrix.m11, guiMatrix.m13));

            destinationX1 = Mathf.Clamp(SnapGuiAxis(destinationX1,
                guiMatrix.m00, guiMatrix.m03), destinationX0, destinationX3);
            destinationX2 = Mathf.Clamp(SnapGuiAxis(destinationX2,
                guiMatrix.m00, guiMatrix.m03), destinationX1, destinationX3);
            destinationY1 = Mathf.Clamp(SnapGuiAxis(destinationY1,
                guiMatrix.m11, guiMatrix.m13), destinationY0, destinationY3);
            destinationY2 = Mathf.Clamp(SnapGuiAxis(destinationY2,
                guiMatrix.m11, guiMatrix.m13), destinationY1, destinationY3);
        }

        private static bool IsAxisAlignedGuiMatrix(Matrix4x4 matrix)
        {
            const float epsilon = .00001f;
            return RuntimeUiNumbers.IsFinite(matrix.m00)
                && RuntimeUiNumbers.IsFinite(matrix.m11)
                && RuntimeUiNumbers.IsFinite(matrix.m03)
                && RuntimeUiNumbers.IsFinite(matrix.m13)
                && matrix.m00 > epsilon
                && matrix.m11 > epsilon
                && Mathf.Abs(matrix.m01) <= epsilon
                && Mathf.Abs(matrix.m10) <= epsilon;
        }

        private static float SnapGuiAxis(float guiValue, float matrixScale,
            float matrixOffset)
        {
            var deviceValue = guiValue * matrixScale + matrixOffset;
            return (Mathf.Round(deviceValue) - matrixOffset) / matrixScale;
        }

        private static void DrawNineSlicePatch(Texture texture,
            float destinationXMin, float destinationYMin,
            float destinationXMax, float destinationYMax,
            float sourceXMin, float sourceYMin, float sourceXMax, float sourceYMax)
        {
            var destinationWidth = destinationXMax - destinationXMin;
            var destinationHeight = destinationYMax - destinationYMin;
            var sourceWidth = sourceXMax - sourceXMin;
            var sourceHeight = sourceYMax - sourceYMin;
            if (destinationWidth <= 0f || destinationHeight <= 0f
                || sourceWidth <= 0f || sourceHeight <= 0f)
                return;

            var target = new Rect(destinationXMin, destinationYMin,
                destinationWidth, destinationHeight);
            var uv = new Rect(sourceXMin / texture.width, sourceYMin / texture.height,
                sourceWidth / texture.width, sourceHeight / texture.height);
            GUI.DrawTextureWithTexCoords(target, texture, uv, true);
        }

        private static void FitPair(ref float first, ref float second, float available)
        {
            var total = first + second;
            if (total <= available || total <= 0f)
                return;
            var factor = available / total;
            first *= factor;
            second *= factor;
        }

        private static Rect Inset(Rect rect, float inset)
        {
            return Inset(rect, inset, inset);
        }

        private static Rect Inset(Rect rect, float horizontalInset, float verticalInset)
        {
            var horizontal = Mathf.Min(horizontalInset, Mathf.Max(0f, rect.width * .5f));
            var vertical = Mathf.Min(verticalInset, Mathf.Max(0f, rect.height * .5f));
            return new Rect(rect.x + horizontal, rect.y + vertical,
                Mathf.Max(0f, rect.width - horizontal * 2f),
                Mathf.Max(0f, rect.height - vertical * 2f));
        }

        private static Rect ResolveSingleLineDrawRect(Rect owner, GUIStyle style,
            TextAnchor alignment)
        {
            var requestedHeight = style == null || style.fixedHeight <= 0f
                ? owner.height : style.fixedHeight;
            var height = Mathf.Min(Mathf.Max(0f, owner.height),
                Mathf.Max(0f, requestedHeight));
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

        public static Rect ResolveOpticalVisualRect(RuntimeUiDrawContext context,
            RuntimeUiArtSlot slot, Rect iconRect)
        {
            var binding = context.RequiredBinding(slot);
            var source = binding.Sprite.rect;
            var sourceWidth = Mathf.Max(1f, source.width);
            var sourceHeight = Mathf.Max(1f, source.height);
            var optical = binding.OpticalInset;
            var left = Mathf.Clamp01(optical.Left / sourceWidth);
            var top = Mathf.Clamp01(optical.Top / sourceHeight);
            var right = Mathf.Clamp01(optical.Right / sourceWidth);
            var bottom = Mathf.Clamp01(optical.Bottom / sourceHeight);
            return new Rect(
                iconRect.x + iconRect.width * left,
                iconRect.y + iconRect.height * top,
                Mathf.Max(0f, iconRect.width * (1f - left - right)),
                Mathf.Max(0f, iconRect.height * (1f - top - bottom)));
        }

        public static Rect ResolveOpticalEnvelopeDrawRect(RuntimeUiDrawContext context,
            RuntimeUiArtSlot slot, Rect opticalDestination)
        {
            context = Require(context);
            var binding = context.RequiredBinding(slot);
            var source = binding.Sprite.rect;
            if (source.width <= 0f || source.height <= 0f
                || opticalDestination.width <= 0f
                || opticalDestination.height <= 0f)
                return default;

            var optical = binding.OpticalInset;
            var opticalWidth = source.width - optical.Left - optical.Right;
            var opticalHeight = source.height - optical.Top - optical.Bottom;
            if (opticalWidth <= 0f || opticalHeight <= 0f)
                throw new InvalidOperationException(
                    "Runtime UI optical envelope must have positive dimensions for slot '"
                    + RuntimeUiArtSlots.SemanticId(slot) + "'.");

            var scale = Mathf.Min(opticalDestination.width / opticalWidth,
                opticalDestination.height / opticalHeight);
            var drawWidth = source.width * scale;
            var drawHeight = source.height * scale;
            var opticalCenterX = (optical.Left + opticalWidth * .5f) * scale;
            var opticalCenterY = (optical.Top + opticalHeight * .5f) * scale;
            return new Rect(
                opticalDestination.center.x - opticalCenterX,
                opticalDestination.center.y - opticalCenterY,
                drawWidth, drawHeight);
        }

        private static Rect Union(Rect first, Rect second)
        {
            return Rect.MinMaxRect(
                Mathf.Min(first.xMin, second.xMin),
                Mathf.Min(first.yMin, second.yMin),
                Mathf.Max(first.xMax, second.xMax),
                Mathf.Max(first.yMax, second.yMax));
        }

        private static RuntimeUiArtSlot ActionSlot(RuntimeUiActionKind kind)
        {
            switch (kind)
            {
                case RuntimeUiActionKind.Primary: return RuntimeUiArtSlot.ActionPrimary;
                case RuntimeUiActionKind.Secondary: return RuntimeUiArtSlot.ActionSecondary;
                case RuntimeUiActionKind.Quiet: return RuntimeUiArtSlot.ActionQuiet;
                case RuntimeUiActionKind.Danger: return RuntimeUiArtSlot.ActionDanger;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static RuntimeUiArtSlot LobbyThumbnailSlot(RuntimeUiLobbyThumbnail thumbnail)
        {
            switch (thumbnail)
            {
                case RuntimeUiLobbyThumbnail.Orchard01:
                    return RuntimeUiArtSlot.IllustrationLobbyOrchard01;
                case RuntimeUiLobbyThumbnail.Orchard02:
                    return RuntimeUiArtSlot.IllustrationLobbyOrchard02;
                case RuntimeUiLobbyThumbnail.Orchard03:
                    return RuntimeUiArtSlot.IllustrationLobbyOrchard03;
                default:
                    throw new ArgumentOutOfRangeException(nameof(thumbnail), thumbnail, null);
            }
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

        private static RuntimeUiInteractionState ResolveSurfaceVisualState(
            RuntimeUiInteractionState state)
        {
            switch (state)
            {
                case RuntimeUiInteractionState.Success:
                case RuntimeUiInteractionState.Warning:
                case RuntimeUiInteractionState.Error:
                    return RuntimeUiInteractionState.Normal;
                default:
                    return state;
            }
        }

        private static RuntimeUiArtSlot IndicatorSlot(RuntimeUiIndicatorKind kind)
        {
            switch (kind)
            {
                case RuntimeUiIndicatorKind.Selected: return RuntimeUiArtSlot.MarkerSelected;
                case RuntimeUiIndicatorKind.Disabled: return RuntimeUiArtSlot.IndicatorDisabled;
                case RuntimeUiIndicatorKind.Loading: return RuntimeUiArtSlot.IndicatorLoading;
                case RuntimeUiIndicatorKind.Success: return RuntimeUiArtSlot.IndicatorSuccess;
                case RuntimeUiIndicatorKind.Warning: return RuntimeUiArtSlot.IndicatorWarning;
                case RuntimeUiIndicatorKind.Error: return RuntimeUiArtSlot.IndicatorError;
                case RuntimeUiIndicatorKind.DragLegal: return RuntimeUiArtSlot.IndicatorDragLegal;
                case RuntimeUiIndicatorKind.DragIllegal: return RuntimeUiArtSlot.IndicatorDragIllegal;
                case RuntimeUiIndicatorKind.Merge: return RuntimeUiArtSlot.IndicatorMerge;
                case RuntimeUiIndicatorKind.Swap: return RuntimeUiArtSlot.IndicatorSwap;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static RuntimeUiArtSlot? StateIndicatorSlot(RuntimeUiInteractionState state)
        {
            switch (state)
            {
                case RuntimeUiInteractionState.Selected: return RuntimeUiArtSlot.MarkerSelected;
                case RuntimeUiInteractionState.Disabled: return RuntimeUiArtSlot.IndicatorDisabled;
                case RuntimeUiInteractionState.Loading: return RuntimeUiArtSlot.IndicatorLoading;
                case RuntimeUiInteractionState.Success: return RuntimeUiArtSlot.IndicatorSuccess;
                case RuntimeUiInteractionState.Warning: return RuntimeUiArtSlot.IndicatorWarning;
                case RuntimeUiInteractionState.Error: return RuntimeUiArtSlot.IndicatorError;
                case RuntimeUiInteractionState.Normal:
                case RuntimeUiInteractionState.HoveredOrFocused:
                case RuntimeUiInteractionState.Pressed:
                    return null;
                default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private static void RequireIconSlot(RuntimeUiArtSlot slot)
        {
            if (!RuntimeUiArtSlots.IsRequired(slot)
                || RuntimeUiArtSlots.Geometry(slot) != RuntimeUiArtGeometry.Icon)
            {
                throw new ArgumentException("Slot '" + slot
                    + "' is not a runtime UI icon slot.", nameof(slot));
            }
        }

        private static void RequireMetricIcon(RuntimeUiArtSlot slot)
        {
            if (slot != RuntimeUiArtSlot.IconResourceSun
                && slot != RuntimeUiArtSlot.IconResourceCore
                && slot != RuntimeUiArtSlot.IconResourceWave
                && slot != RuntimeUiArtSlot.IconResourceSunMicro
                && slot != RuntimeUiArtSlot.IconResourceCoreMicro
                && slot != RuntimeUiArtSlot.IconResourceWaveMicro)
            {
                throw new ArgumentException("Metric components require a resource icon slot.",
                    nameof(slot));
            }
        }

        private static Color ApplyMotionAlpha(RuntimeUiMotionSample motion)
        {
            var previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b,
                previous.a * motion.Alpha);
            return previous;
        }
    }
}
