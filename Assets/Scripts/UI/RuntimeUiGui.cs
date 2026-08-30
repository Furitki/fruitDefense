using System;
using UnityEngine;

namespace FruitDefense.UI
{
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

    public readonly struct RuntimeUiEmphasisTextLayout
    {
        internal RuntimeUiEmphasisTextLayout(Rect textRect, Rect outlinedRect,
            GUIStyle style, int outlinePixels, Color fillColor, Color outlineColor)
        {
            TextRect = textRect;
            OutlinedRect = outlinedRect;
            Style = style;
            OutlinePixels = outlinePixels;
            FillColor = fillColor;
            OutlineColor = outlineColor;
        }

        public Rect TextRect { get; }
        public Rect OutlinedRect { get; }
        public GUIStyle Style { get; }
        public int OutlinePixels { get; }
        public Color FillColor { get; }
        public Color OutlineColor { get; }
    }

    public readonly struct RuntimeUiActionContentLayout
    {
        internal RuntimeUiActionContentLayout(Rect contentRect, Rect iconRect,
            Rect iconVisualRect, Rect labelRect, Rect groupRect,
            bool hasIcon, bool hasLabel, bool fits)
        {
            ContentRect = contentRect;
            IconRect = iconRect;
            IconVisualRect = iconVisualRect;
            LabelRect = labelRect;
            GroupRect = groupRect;
            HasIcon = hasIcon;
            HasLabel = hasLabel;
            Fits = fits;
        }

        public Rect ContentRect { get; }
        public Rect IconRect { get; }
        public Rect IconVisualRect { get; }
        public Rect LabelRect { get; }
        public Rect GroupRect { get; }
        public bool HasIcon { get; }
        public bool HasLabel { get; }
        public bool Fits { get; }
    }

    public readonly struct RuntimeUiCompactControlLayout
    {
        internal RuntimeUiCompactControlLayout(Rect controlRect, Rect surfaceRect,
            Rect contentRect, bool usesMultiplierText)
        {
            ControlRect = controlRect;
            SurfaceRect = surfaceRect;
            ContentRect = contentRect;
            UsesMultiplierText = usesMultiplierText;
        }

        public Rect ControlRect { get; }
        public Rect SurfaceRect { get; }
        public Rect ContentRect { get; }
        public bool UsesMultiplierText { get; }
        public Rect VisualBounds => SurfaceRect;

        public bool IsContained()
        {
            return Contains(ControlRect, SurfaceRect)
                && Contains(ControlRect, ContentRect);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }
    }

    public readonly struct RuntimeUiInlineContentLayout
    {
        internal RuntimeUiInlineContentLayout(Rect contentRect, Rect iconRect,
            Rect iconVisualRect, Rect labelRect, Rect groupRect, bool fits)
        {
            ContentRect = contentRect;
            IconRect = iconRect;
            IconVisualRect = iconVisualRect;
            LabelRect = labelRect;
            GroupRect = groupRect;
            Fits = fits;
        }

        public Rect ContentRect { get; }
        public Rect IconRect { get; }
        public Rect IconVisualRect { get; }
        public Rect LabelRect { get; }
        public Rect GroupRect { get; }
        public bool Fits { get; }
    }

    public readonly struct RuntimeUiMetricContentLayout
    {
        internal RuntimeUiMetricContentLayout(Rect contentRect, Rect iconRect,
            Rect iconVisualRect, Rect valueRect, Rect labelRect, Rect groupRect,
            bool fits)
        {
            ContentRect = contentRect;
            IconRect = iconRect;
            IconVisualRect = iconVisualRect;
            ValueRect = valueRect;
            LabelRect = labelRect;
            GroupRect = groupRect;
            Fits = fits;
        }

        public Rect ContentRect { get; }
        public Rect IconRect { get; }
        public Rect IconVisualRect { get; }
        public Rect ValueRect { get; }
        public Rect LabelRect { get; }
        public Rect GroupRect { get; }
        public bool Fits { get; }
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
                scaledLineHeights[roleIndex] =
                    Mathf.Max(1f, Mathf.Round(token.LineHeight * scale));
                for (var anchorIndex = 0; anchorIndex < TextAnchorCount; anchorIndex++)
                {
                    textStyles[roleIndex, anchorIndex] = CreateTextStyle(
                        token, scale, (TextAnchor)anchorIndex);
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
                fixedHeight = scaledLineHeights[roleIndex],
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

        private static GUIStyle CreateTextStyle(RuntimeUiTypographyStyle token,
            float scale, TextAnchor alignment)
        {
            var style = new GUIStyle
            {
                font = token.Font,
                fontSize = Mathf.Max(1, Mathf.RoundToInt(token.FontSize * scale)),
                fontStyle = FontStyle.Normal,
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

    public static partial class RuntimeUiGui
    {
        private const float PixelRoundingTolerance = .51f;
        public const float EmphasisOutlineWidthLogical = 2f;
        private static readonly GUIContent StatusMeasurementContent = new GUIContent();
        private static readonly GUIContent ActionMeasurementContent = new GUIContent();
        private static readonly GUIContent MetricMeasurementContent = new GUIContent();

        public static RuntimeUiDrawContext RequireContext(RuntimeUiDrawContext current,
            RuntimeUiTheme theme, float scale)
        {
            return RuntimeUiDrawContext.Require(current, theme, scale);
        }
    }
}
