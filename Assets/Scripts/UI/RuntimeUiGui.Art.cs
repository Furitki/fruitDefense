using System;
using UnityEngine;

namespace FruitDefense.UI
{
    public static partial class RuntimeUiGui
    {
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

        public static void DrawGameplayStage(RuntimeUiDrawContext context, Rect rect,
            RuntimeUiInteractionState state = RuntimeUiInteractionState.Normal)
        {
            DrawStatefulSurface(Require(context), rect,
                RuntimeUiArtSlot.SurfaceGameplayStage, state);
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
