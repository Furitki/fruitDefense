using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Presentation
{
    public enum CombatFloatingTextRole
    {
        None = 0,
        NormalDamage = 1,
        HeavyDamage = 2,
        PeriodicDamage = 3,
        Resource = 4,
        Control = 5,
        Defeat = 6,
    }

    public readonly struct CombatFloatingTextStyle
    {
        public CombatFloatingTextStyle(CombatFloatingTextRole role, int fontSize,
            Color fillColor,
            float duration, float riseDistance, float startScale, float peakScale,
            bool countsAsOrdinary)
        {
            Role = role;
            FontSize = fontSize;
            FillColor = fillColor;
            Duration = duration;
            RiseDistance = riseDistance;
            StartScale = startScale;
            PeakScale = peakScale;
            CountsAsOrdinary = countsAsOrdinary;
        }

        public CombatFloatingTextRole Role { get; }
        public int FontSize { get; }
        public Color FillColor { get; }
        public float Duration { get; }
        public float RiseDistance { get; }
        public float StartScale { get; }
        public float PeakScale { get; }
        public bool CountsAsOrdinary { get; }
    }

    public readonly struct CombatFloatingTextMotionSample
    {
        public CombatFloatingTextMotionSample(float scale, float offsetY, float opacity)
        {
            Scale = scale;
            OffsetY = offsetY;
            Opacity = opacity;
        }

        public float Scale { get; }
        public float OffsetY { get; }
        public float Opacity { get; }
    }

    public sealed class CombatFloatingTextStyleCatalog
    {
        public const int TotalCapacity = 12;
        public const int OrdinaryCapacity = 8;
        public const int VisualLaneCount = 3;
        public const int CollisionCandidateCount = 9;
        public const int SameProfileTickCapacity = 3;
        public const float FollowSeconds = .12f;
        public const float TerminalLaneDistance = 26f;
        public const float AtlasFrameTimeGateMilliseconds = .5f;
        public const int AtlasAllocationGateBytesPerSecond = 1024;
        public const string RuntimeGlyphInventory = "-+0123456789 阳光冻结击败×";

        public static readonly Color SharedOutlineColor =
            new Color32(58, 35, 26, 255);

        private const float EntryEnd = .12f;
        private const float ReboundEnd = .38f;
        private const float FadeStart = .65f;
        private readonly CombatFloatingTextStyle[] _styles;

        private CombatFloatingTextStyleCatalog(CombatFloatingTextStyle[] styles)
        {
            _styles = styles ?? throw new ArgumentNullException(nameof(styles));
        }

        public int Count { get { return _styles.Length - 1; } }

        public CombatFloatingTextStyle Resolve(CombatFloatingTextRole role)
        {
            var index = (int)role;
            if (role == CombatFloatingTextRole.None
                || index < 0 || index >= _styles.Length
                || _styles[index].Role != role)
                throw new InvalidOperationException(
                    "Unknown combat floating-text role: " + role);
            return _styles[index];
        }

        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            foreach (CombatFloatingTextRole role in Enum.GetValues(
                         typeof(CombatFloatingTextRole)))
            {
                if (role == CombatFloatingTextRole.None) continue;
                var index = (int)role;
                if (index <= 0 || index >= _styles.Length
                    || _styles[index].Role != role)
                {
                    issues.Add("missing-role:" + role);
                    continue;
                }
                var style = _styles[index];
                var minimum = role == CombatFloatingTextRole.PeriodicDamage ? 14 : 16;
                if (style.FontSize < minimum) issues.Add("font-too-small:" + role);
                if (style.Duration <= 0f || style.RiseDistance <= 0f)
                    issues.Add("invalid-motion:" + role);
                if (style.StartScale <= 0f || style.StartScale > 1f
                    || style.PeakScale < 1f || style.PeakScale > 1.3f)
                    issues.Add("invalid-rebound:" + role);
                if (ContrastRatio(style.FillColor, SharedOutlineColor) < 3f)
                    issues.Add("low-fill-outline-contrast:" + role);
            }
            return issues.AsReadOnly();
        }

        public static CombatFloatingTextStyleCatalog CreateBundled()
        {
            var count = Enum.GetValues(typeof(CombatFloatingTextRole)).Length;
            var styles = new CombatFloatingTextStyle[count];
            Add(styles, new CombatFloatingTextStyle(
                CombatFloatingTextRole.NormalDamage, 16,
                Rgb(255, 245, 218),
                .62f, 20f, .78f, 1.08f, true));
            Add(styles, new CombatFloatingTextStyle(
                CombatFloatingTextRole.HeavyDamage, 20,
                Rgb(255, 116, 72),
                .82f, 27f, .62f, 1.22f, false));
            Add(styles, new CombatFloatingTextStyle(
                CombatFloatingTextRole.PeriodicDamage, 15,
                Rgb(255, 184, 68),
                .50f, 15f, .90f, 1.02f, true));
            Add(styles, new CombatFloatingTextStyle(
                CombatFloatingTextRole.Resource, 17,
                Rgb(255, 221, 77),
                .86f, 25f, .72f, 1.14f, false));
            Add(styles, new CombatFloatingTextStyle(
                CombatFloatingTextRole.Control, 17,
                Rgb(185, 232, 255),
                .78f, 20f, .72f, 1.14f, false));
            Add(styles, new CombatFloatingTextStyle(
                CombatFloatingTextRole.Defeat, 19,
                Rgb(255, 242, 166),
                .92f, 30f, .62f, 1.24f, false));
            var catalog = new CombatFloatingTextStyleCatalog(styles);
            var issues = catalog.Validate();
            if (issues.Count > 0)
                throw new InvalidOperationException(
                    "Bundled combat floating-text styles are invalid: "
                    + string.Join("\n", issues));
            return catalog;
        }

        public static CombatFloatingTextMotionSample Sample(
            CombatFloatingTextStyle style, float lifetimeProgress,
            float detachedProgress)
        {
            var value = Mathf.Clamp01(lifetimeProgress);
            float scale;
            if (value < EntryEnd)
            {
                var phase = Smooth(value / EntryEnd);
                scale = Mathf.Lerp(style.StartScale, style.PeakScale, phase);
            }
            else if (value < ReboundEnd)
            {
                var phase = Smooth((value - EntryEnd) / (ReboundEnd - EntryEnd));
                scale = Mathf.Lerp(style.PeakScale, 1f, phase);
            }
            else
            {
                scale = 1f;
            }
            var opacity = value < EntryEnd
                ? Smooth(value / EntryEnd)
                : value <= FadeStart
                    ? 1f
                    : 1f - Smooth((value - FadeStart) / (1f - FadeStart));
            return new CombatFloatingTextMotionSample(
                scale, -style.RiseDistance * RiseProgress(detachedProgress),
                Mathf.Clamp01(opacity));
        }

        public static Vector2 VisualLaneOffset(int lane, bool towardBattlefieldInterior)
        {
            var verticalDirection = towardBattlefieldInterior ? 1f : -1f;
            switch (Mathf.Clamp(lane, 0, VisualLaneCount - 1))
            {
                case 0: return Vector2.zero;
                case 2: return new Vector2(8f, 28f * verticalDirection);
                default: return new Vector2(-8f, 14f * verticalDirection);
            }
        }

        public static Vector2 CollisionCandidateLaneOffset(int originalLane,
            int candidateIndex, bool towardBattlefieldInterior)
        {
            var lane = Mathf.Clamp(originalLane, 0, VisualLaneCount - 1);
            var candidate = Mathf.Clamp(candidateIndex, 0,
                CollisionCandidateCount - 1);
            var original = VisualLaneOffset(lane, towardBattlefieldInterior);
            var mirrorX = Mathf.Approximately(original.x, 0f)
                ? -8f : -original.x;
            var otherX = Mathf.Approximately(original.x, 0f) ? 8f : 0f;
            var nextLane = lane < VisualLaneCount - 1 ? lane + 1 : lane - 1;
            var remainingLane = lane == 0 ? 2 : 0;
            var verticalDirection = towardBattlefieldInterior ? 1f : -1f;
            var nextY = nextLane * 14f * verticalDirection;
            var remainingY = remainingLane * 14f * verticalDirection;
            switch (candidate)
            {
                case 0: return original;
                case 1: return new Vector2(mirrorX, original.y);
                case 2: return new Vector2(original.x, nextY);
                case 3: return new Vector2(mirrorX, nextY);
                case 4: return new Vector2(otherX, nextY);
                case 5: return new Vector2(original.x, remainingY);
                case 6: return new Vector2(mirrorX, remainingY);
                case 7: return new Vector2(otherX, remainingY);
                default: return new Vector2(otherX, original.y);
            }
        }

        public static Vector2 SemanticLaneOffset(CombatFloatingTextRole role,
            bool towardBattlefieldInterior)
        {
            if (role != CombatFloatingTextRole.Defeat) return Vector2.zero;
            return new Vector2(0f, TerminalLaneDistance
                * (towardBattlefieldInterior ? 1f : -1f));
        }

        public static float ContrastRatio(Color first, Color second)
        {
            var a = RelativeLuminance(first);
            var b = RelativeLuminance(second);
            var lighter = Mathf.Max(a, b);
            var darker = Mathf.Min(a, b);
            return (lighter + .05f) / (darker + .05f);
        }

        private static void Add(CombatFloatingTextStyle[] styles,
            CombatFloatingTextStyle style)
        {
            styles[(int)style.Role] = style;
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float RiseProgress(float progress)
        {
            var value = Mathf.Clamp01(progress);
            if (value < EntryEnd)
                return Smooth(value / EntryEnd) * .08f;
            if (value < ReboundEnd)
            {
                var phase = Smooth((value - EntryEnd) / (ReboundEnd - EntryEnd));
                return Mathf.Lerp(.08f, .18f, phase);
            }
            return Mathf.Lerp(.18f, 1f,
                Smooth((value - ReboundEnd) / (1f - ReboundEnd)));
        }

        private static Color Rgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, 255);
        }

        private static float RelativeLuminance(Color color)
        {
            return .2126f * Linear(color.r)
                + .7152f * Linear(color.g)
                + .0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= .03928f
                ? value / 12.92f
                : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
        }
    }
}
