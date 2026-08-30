using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Core
{
    public readonly struct DragConnectorGeometry
    {
        internal DragConnectorGeometry(Vector2 start, Vector2 end, float length,
            float angleDegrees, float dashScale, float thicknessScale)
        {
            Start = start;
            End = end;
            Length = length;
            AngleDegrees = angleDegrees;
            DashLength = DragGeometry.ConnectorDashLength * dashScale;
            DashStride = DragGeometry.ConnectorDashStride * dashScale;
            Thickness = DragGeometry.ConnectorThickness * thicknessScale;
            DashCount = DashStride > 0f
                ? Mathf.Max(1, Mathf.CeilToInt(length / DashStride))
                : 0;
        }

        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Length { get; }
        public float AngleDegrees { get; }
        public float DashLength { get; }
        public float DashStride { get; }
        public float Thickness { get; }
        public int DashCount { get; }
        public bool Visible => DashCount > 0 && Length > 0f && Thickness > 0f;

        public Rect DashRect(int index)
        {
            if (index < 0 || index >= DashCount)
                throw new System.ArgumentOutOfRangeException(nameof(index));
            var offset = index * DashStride;
            var length = Mathf.Min(DashLength,
                Mathf.Max(0f, Length - offset));
            return new Rect(Start.x + offset,
                Start.y - Thickness * .5f,
                length, Thickness);
        }
    }

    public static class DragGeometry
    {
        public const float PreviewSize = 44f;
        public const float CursorOffset = 24f;
        public const float ActivationDistance = 8f;
        public const float ConnectorDashLength = 8f;
        public const float ConnectorDashGap = 4f;
        public const float ConnectorDashStride =
            ConnectorDashLength + ConnectorDashGap;
        public const float ConnectorThickness = 2f;

        public static bool CrossedActivationThreshold(Vector2 start, Vector2 current)
        {
            return Vector2.Distance(start, current) > ActivationDistance;
        }

        public static Rect PreviewRect(Vector2 cursor)
        {
            var center = cursor - new Vector2(CursorOffset, CursorOffset);
            return new Rect(
                center.x - PreviewSize * .5f,
                center.y - PreviewSize * .5f,
                PreviewSize,
                PreviewSize);
        }

        public static float OverlapArea(Rect left, Rect right)
        {
            var width = Mathf.Min(left.xMax, right.xMax) - Mathf.Max(left.xMin, right.xMin);
            var height = Mathf.Min(left.yMax, right.yMax) - Mathf.Max(left.yMin, right.yMin);
            return width > 0f && height > 0f ? width * height : 0f;
        }

        public static int BestOverlapIndex(Rect preview, IReadOnlyList<Rect> targets)
        {
            var bestIndex = -1;
            var bestArea = 0f;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < targets.Count; index++)
            {
                var area = OverlapArea(preview, targets[index]);
                if (area <= 0f) continue;
                var distance = (preview.center - targets[index].center).sqrMagnitude;
                if (area > bestArea || Mathf.Approximately(area, bestArea) && distance < bestDistance)
                {
                    bestIndex = index;
                    bestArea = area;
                    bestDistance = distance;
                }
            }
            return bestIndex;
        }

        public static DragConnectorGeometry ResolveConnector(
            Rect source, Rect destination)
        {
            if (!IsFinitePositive(source) || !IsFinitePositive(destination)
                || OverlapArea(source, destination) > 0f)
                return default;

            var start = EdgePoint(source, destination.center);
            var end = EdgePoint(destination, source.center);
            var delta = end - start;
            var length = delta.magnitude;
            if (!IsFinite(length) || length <= ConnectorThickness)
                return default;

            return new DragConnectorGeometry(start, end, length,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg,
                1f, 1f);
        }

        public static DragConnectorGeometry ProjectConnector(
            DragConnectorGeometry geometry, Matrix4x4 matrix)
        {
            if (!geometry.Visible)
                return default;

            var projectedStart3 = matrix.MultiplyPoint3x4(geometry.Start);
            var projectedEnd3 = matrix.MultiplyPoint3x4(geometry.End);
            var projectedStart = new Vector2(projectedStart3.x, projectedStart3.y);
            var projectedEnd = new Vector2(projectedEnd3.x, projectedEnd3.y);
            var projectedDelta = projectedEnd - projectedStart;
            var projectedLength = projectedDelta.magnitude;

            var direction = (geometry.End - geometry.Start).normalized;
            var normal = new Vector3(-direction.y, direction.x, 0f);
            var projectedNormal3 = matrix.MultiplyVector(normal);
            var projectedNormal = new Vector2(
                projectedNormal3.x, projectedNormal3.y);
            var dashScale = projectedLength / geometry.Length;
            var thicknessScale = projectedNormal.magnitude;
            if (!IsFinite(projectedStart)
                || !IsFinite(projectedEnd)
                || !IsFinite(projectedLength) || projectedLength <= 0f
                || !IsFinite(dashScale) || dashScale <= 0f
                || !IsFinite(thicknessScale) || thicknessScale <= 0f)
                return default;

            return new DragConnectorGeometry(
                projectedStart, projectedEnd, projectedLength,
                Mathf.Atan2(projectedDelta.y, projectedDelta.x) * Mathf.Rad2Deg,
                dashScale, thicknessScale);
        }

        private static Vector2 EdgePoint(Rect rect, Vector2 toward)
        {
            var center = rect.center;
            var direction = toward - center;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return center;

            var scaleX = Mathf.Abs(direction.x) > Mathf.Epsilon
                ? rect.width * .5f / Mathf.Abs(direction.x)
                : float.PositiveInfinity;
            var scaleY = Mathf.Abs(direction.y) > Mathf.Epsilon
                ? rect.height * .5f / Mathf.Abs(direction.y)
                : float.PositiveInfinity;
            return center + direction * Mathf.Min(scaleX, scaleY);
        }

        private static bool IsFinitePositive(Rect rect)
        {
            return IsFinite(rect.x) && IsFinite(rect.y)
                && IsFinite(rect.width) && IsFinite(rect.height)
                && rect.width > 0f && rect.height > 0f;
        }

        private static bool IsFinite(Vector2 point)
        {
            return IsFinite(point.x) && IsFinite(point.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
