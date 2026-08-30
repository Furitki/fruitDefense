using System;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.UI
{
    public static partial class RuntimeUiGui
    {
        private const float GameplayStageMaskInset = 8f;
        private const string NineSliceShaderName =
            "Hidden/FruitDefense/RuntimeUiNineSlice";
        private static readonly int NineSliceTintId = Shader.PropertyToID("_Tint");
        private static readonly int NineSliceTargetBorderId =
            Shader.PropertyToID("_TargetBorder");
        private static readonly int NineSliceTargetSizeId =
            Shader.PropertyToID("_TargetSize");
        private static readonly int NineSliceSourceXId = Shader.PropertyToID("_SourceX");
        private static readonly int NineSliceSourceXRightId =
            Shader.PropertyToID("_SourceXRight");
        private static readonly int NineSliceSourceYId = Shader.PropertyToID("_SourceY");
        private static readonly int NineSliceSourceYTopId =
            Shader.PropertyToID("_SourceYTop");
        private static readonly int NineSliceClipRectPixelsId =
            Shader.PropertyToID("_ClipRectPixels");
        private static Material nineSliceMaterial;

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
            DrawOpticalEnvelopeStretchSlotArt(Require(context), rect,
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

        public static void DrawMetricSurface(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStatefulSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfaceMetric, state);
        }

        public static void DrawGameplayStage(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStatefulSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfaceGameplayStage, state);
        }

        public static Rect GameplayStageMaskRect(
            RuntimeUiDrawContext context, Rect rect)
        {
            context = Require(context);
            var inset = context.Scaled(GameplayStageMaskInset);
            if (inset * 2f >= rect.width || inset * 2f >= rect.height)
                throw new InvalidOperationException(
                    "Gameplay-stage opening must leave a positive content rect.");
            return Rect.MinMaxRect(
                rect.xMin + inset, rect.yMin + inset,
                rect.xMax - inset, rect.yMax - inset);
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

        public static Rect DrawSlot(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiSlotKind kind, RuntimeUiInteractionState state,
            bool emphasized = false, RuntimeUiMotionSample motion = default)
        {
            var slot = kind == RuntimeUiSlotKind.Tool
                ? RuntimeUiArtSlot.SlotTool
                : kind == RuntimeUiSlotKind.Nursery
                    ? RuntimeUiArtSlot.SlotNursery
                    : throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            context = Require(context);
            var visualMotion = RuntimeUiMotionSample.Combine(motion,
                RuntimeUiMotion.InteractionState(state, context.Theme.Feedback));
            var visualRect = ContainedTransform(rect, visualMotion);
            DrawSlotArt(context, visualRect, slot,
                emphasized && state != RuntimeUiInteractionState.Disabled
                    ? RuntimeUiInteractionState.Pressed
                    : ResolveSlotSurfaceVisualState(state));
            if (state != RuntimeUiInteractionState.Selected)
                DrawStateIndicator(context, visualRect, state);
            return visualRect;
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

        public static void DrawDragConnector(RuntimeUiDrawContext context,
            DragConnectorGeometry geometry, RuntimeUiInteractionState state)
        {
            if (!geometry.Visible)
                return;
            context = Require(context);
            var matrix = GUI.matrix;
            var projected = DragGeometry.ProjectConnector(geometry, matrix);
            if (!projected.Visible)
                return;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                GUIUtility.RotateAroundPivot(
                    projected.AngleDegrees, projected.Start);
                var tint = context.Tint(state);
                for (var index = 0; index < projected.DashCount; index++)
                {
                    DrawSlotArt(context, projected.DashRect(index),
                        RuntimeUiArtSlot.SurfaceScrim,
                        RuntimeUiInteractionState.Normal, .78f, tint);
                }
            }
            finally
            {
                GUI.matrix = matrix;
            }
        }

        public static void DrawDragTargetFrame(RuntimeUiDrawContext context,
            Rect target, RuntimeUiInteractionState state,
            Rect? designClip = null)
        {
            context = Require(context);
            if (target.width <= 0f || target.height <= 0f
                || !IsFinite(target.x) || !IsFinite(target.y)
                || !IsFinite(target.width) || !IsFinite(target.height))
                return;
            DrawSlotArt(context, target,
                RuntimeUiArtSlot.SurfaceIllustrationFrame, state,
                designClip: designClip);
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

        private static void DrawStatefulSurface(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiArtSlot slot, RuntimeUiInteractionState state)
        {
            DrawSlotArt(context, rect, slot, ResolveSurfaceVisualState(state));
            DrawStateIndicator(context, rect, state);
        }

        private static void DrawSlotArt(RuntimeUiDrawContext context, Rect destination,
            RuntimeUiArtSlot slot, RuntimeUiInteractionState state,
            float opacityMultiplier = 1f, Color? tintOverride = null,
            bool mirrorX = false, bool mirrorY = false,
            Rect? designClip = null)
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
                            binding.SliceBorder, binding.PixelsPerLogicalUnit,
                            context.Scale, designClip);
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

        private static void DrawOpticalEnvelopeStretchSlotArt(RuntimeUiDrawContext context,
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
            RuntimeUiPixelInsets border, float pixelsPerLogicalUnit, float scale,
            Rect? designClip)
        {
            if (destination.width <= 0f || destination.height <= 0f
                || sourcePixels.width <= 0f || sourcePixels.height <= 0f)
                return;
            if (Event.current != null && Event.current.type != EventType.Repaint)
                return;

            int targetWidthPixels;
            int targetHeightPixels;
            var aligned = GUIUtility.AlignRectToDevice(destination,
                out targetWidthPixels, out targetHeightPixels);
            if (targetWidthPixels <= 0 || targetHeightPixels <= 0
                || aligned.width <= 0f || aligned.height <= 0f)
                return;

            var screenMin = GUIUtility.GUIToScreenPoint(aligned.min);
            var screenMax = GUIUtility.GUIToScreenPoint(aligned.max);
            var screenRect = Rect.MinMaxRect(
                Mathf.Min(screenMin.x, screenMax.x),
                Mathf.Min(screenMin.y, screenMax.y),
                Mathf.Max(screenMin.x, screenMax.x),
                Mathf.Max(screenMin.y, screenMax.y));
            if (screenRect.width <= 0f || screenRect.height <= 0f)
                return;

            var clipScreenRect = screenRect;
            if (designClip.HasValue)
            {
                var clipMin = GUIUtility.GUIToScreenPoint(
                    designClip.Value.min);
                var clipMax = GUIUtility.GUIToScreenPoint(
                    designClip.Value.max);
                clipScreenRect = Intersect(
                    screenRect, Rect.MinMaxRect(
                        Mathf.Min(clipMin.x, clipMax.x),
                        Mathf.Min(clipMin.y, clipMax.y),
                        Mathf.Max(clipMin.x, clipMax.x),
                        Mathf.Max(clipMin.y, clipMax.y)));
                if (clipScreenRect.width <= 0f
                    || clipScreenRect.height <= 0f)
                    return;
            }

            var deviceScaleX = targetWidthPixels / aligned.width;
            var deviceScaleY = targetHeightPixels / aligned.height;
            var targetBorder = ResolveNineSliceTargetBorderPixels(border,
                pixelsPerLogicalUnit, scale, deviceScaleX, deviceScaleY,
                targetWidthPixels, targetHeightPixels);
            Vector4 sourceXRight;
            var sourceX = ResolveNineSliceSourceAxis(sourcePixels.xMin,
                sourcePixels.xMax, border.Left, border.Right, texture.width,
                out sourceXRight);
            Vector4 sourceYTop;
            var sourceY = ResolveNineSliceSourceAxis(sourcePixels.yMin,
                sourcePixels.yMax, border.Bottom, border.Top, texture.height,
                out sourceYTop);

            var material = RequireNineSliceMaterial();
            material.SetColor(NineSliceTintId, GUI.color);
            material.SetVector(NineSliceTargetBorderId, targetBorder);
            material.SetVector(NineSliceTargetSizeId,
                new Vector4(targetWidthPixels, targetHeightPixels, 0f, 0f));
            material.SetVector(NineSliceSourceXId, sourceX);
            material.SetVector(NineSliceSourceXRightId, sourceXRight);
            material.SetVector(NineSliceSourceYId, sourceY);
            material.SetVector(NineSliceSourceYTopId, sourceYTop);
            var fragmentYMin = SystemInfo.graphicsUVStartsAtTop
                ? clipScreenRect.yMin
                : Screen.height - clipScreenRect.yMax;
            var fragmentYMax = SystemInfo.graphicsUVStartsAtTop
                ? clipScreenRect.yMax
                : Screen.height - clipScreenRect.yMin;
            material.SetVector(NineSliceClipRectPixelsId,
                new Vector4(clipScreenRect.xMin, fragmentYMin,
                    clipScreenRect.xMax, fragmentYMax));

            // One aligned quad owns the entire nine-slice. The shader remaps its UVs
            // through shared source/target boundaries, so D3D/WebGL never rasterize
            // nine independently rounded patches or sample across slice partitions.
            var previousMatrix = GUI.matrix;
            try
            {
                // Graphics.DrawTexture inherits IMGUI's current projection even though
                // its public rectangle is screen-space. The rectangle above already
                // includes the complete GUIClip/matrix conversion, so draw it once
                // under identity instead of applying the PC viewport transform twice.
                GUI.matrix = Matrix4x4.identity;
                Graphics.DrawTexture(screenRect, texture, new Rect(0f, 0f, 1f, 1f),
                    0, 0, 0, 0, Color.white, material);
            }
            finally
            {
                GUI.matrix = previousMatrix;
            }
        }

        private static Rect Intersect(Rect left, Rect right)
        {
            return Rect.MinMaxRect(
                Mathf.Max(left.xMin, right.xMin),
                Mathf.Max(left.yMin, right.yMin),
                Mathf.Min(left.xMax, right.xMax),
                Mathf.Min(left.yMax, right.yMax));
        }

        private static Vector4 ResolveNineSliceTargetBorderPixels(
            RuntimeUiPixelInsets border, float pixelsPerLogicalUnit,
            float logicalScale, float deviceScaleX, float deviceScaleY,
            int targetWidthPixels, int targetHeightPixels)
        {
            var sourceScale = Mathf.Max(.0001f, pixelsPerLogicalUnit);
            var left = Mathf.Max(0, Mathf.RoundToInt(
                border.Left / sourceScale * logicalScale * deviceScaleX));
            var right = Mathf.Max(0, Mathf.RoundToInt(
                border.Right / sourceScale * logicalScale * deviceScaleX));
            var top = Mathf.Max(0, Mathf.RoundToInt(
                border.Top / sourceScale * logicalScale * deviceScaleY));
            var bottom = Mathf.Max(0, Mathf.RoundToInt(
                border.Bottom / sourceScale * logicalScale * deviceScaleY));
            FitPixelPair(ref left, ref right, Mathf.Max(0, targetWidthPixels));
            FitPixelPair(ref bottom, ref top, Mathf.Max(0, targetHeightPixels));
            return new Vector4(left, bottom, right, top);
        }

        private static Vector4 ResolveNineSliceSourceAxis(float sourceMin,
            float sourceMax, int leadingBorder, int trailingBorder,
            float textureSize, out Vector4 trailing)
        {
            var inverseTextureSize = 1f / Mathf.Max(1f, textureSize);
            var firstBoundary = sourceMin + leadingBorder;
            var secondBoundary = sourceMax - trailingBorder;
            var outerMin = (sourceMin + .5f) * inverseTextureSize;
            var leadingEnd = (firstBoundary - .5f) * inverseTextureSize;
            var centerStart = (firstBoundary + .5f) * inverseTextureSize;
            var centerEnd = (secondBoundary - .5f) * inverseTextureSize;
            var trailingStart = (secondBoundary + .5f) * inverseTextureSize;
            var outerMax = (sourceMax - .5f) * inverseTextureSize;
            trailing = new Vector4(trailingStart, outerMax, 0f, 0f);
            return new Vector4(outerMin, leadingEnd, centerStart, centerEnd);
        }

        private static void FitPixelPair(ref int first, ref int second, int available)
        {
            var total = (long)first + second;
            if (total <= available || total <= 0L)
                return;
            first = Mathf.Clamp(Mathf.RoundToInt(available * (first / (float)total)),
                0, available);
            second = available - first;
        }

        private static Material RequireNineSliceMaterial()
        {
            if (nineSliceMaterial != null)
                return nineSliceMaterial;
            var shader = Shader.Find(NineSliceShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required runtime UI nine-slice shader is unavailable: "
                    + NineSliceShaderName);
            }

            nineSliceMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "Fruit Defense Runtime UI Nine-Slice Material",
            };
            return nineSliceMaterial;
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

            var scaleX = opticalDestination.width / opticalWidth;
            var scaleY = opticalDestination.height / opticalHeight;
            var drawWidth = source.width * scaleX;
            var drawHeight = source.height * scaleY;
            var opticalCenterX = (optical.Left + opticalWidth * .5f) * scaleX;
            var opticalCenterY = (optical.Top + opticalHeight * .5f) * scaleY;
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

        private static RuntimeUiInteractionState ResolveSlotSurfaceVisualState(
            RuntimeUiInteractionState state)
        {
            switch (state)
            {
                case RuntimeUiInteractionState.HoveredOrFocused:
                case RuntimeUiInteractionState.Selected:
                    return RuntimeUiInteractionState.Normal;
                default:
                    return ResolveSurfaceVisualState(state);
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

        private static void RequireActionGlyphSlot(RuntimeUiArtSlot slot)
        {
            switch (slot)
            {
                case RuntimeUiArtSlot.IconControlPause:
                case RuntimeUiArtSlot.IconControlContinue:
                case RuntimeUiArtSlot.IconControlSpeed:
                case RuntimeUiArtSlot.IconControlStartWave:
                case RuntimeUiArtSlot.IconControlRetry:
                case RuntimeUiArtSlot.IconControlReturn:
                case RuntimeUiArtSlot.IconControlClose:
                case RuntimeUiArtSlot.IconControlStart:
                case RuntimeUiArtSlot.IconControlRefresh:
                    RequireIconSlot(slot);
                    return;
                default:
                    throw new ArgumentException("Slot '" + slot
                        + "' is not a tintable action glyph.", nameof(slot));
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
