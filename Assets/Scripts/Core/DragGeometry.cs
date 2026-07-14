using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Core
{
    public static class DragGeometry
    {
        public const float PreviewSize = 44f;
        public const float CursorOffset = 24f;

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
    }
}
