using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.Editor
{
    public enum CanonicalBattlefieldMapWorkspace
    {
        Gameplay,
        RouteAndMarkers,
        Presentation,
        Validation,
    }

    public enum CanonicalBattlefieldMapTool
    {
        SingleCell,
        Rectangle,
        FloodFill,
        Eyedropper,
    }

    public readonly struct CanonicalBattlefieldCanvasLayout
    {
        public const float BaseCellSize = 44f;

        public Rect CanvasRect { get; }
        public int GridWidth { get; }
        public int GridHeight { get; }
        public float CellSize { get; }

        public CanonicalBattlefieldCanvasLayout(Rect canvasRect, int gridWidth,
            int gridHeight, float zoom)
        {
            CanvasRect = canvasRect;
            GridWidth = Math.Max(0, gridWidth);
            GridHeight = Math.Max(0, gridHeight);
            CellSize = BaseCellSize * Mathf.Clamp(zoom, .4f, 2.5f);
        }

        public Vector2 ContentSize
        {
            get { return new Vector2(GridWidth * CellSize, GridHeight * CellSize); }
        }

        public Rect CellRect(Vector2Int cell)
        {
            return new Rect(CanvasRect.x + cell.x * CellSize,
                CanvasRect.y + cell.y * CellSize, CellSize, CellSize);
        }

        public bool TryHit(Vector2 pointer, out Vector2Int cell)
        {
            cell = new Vector2Int(
                Mathf.FloorToInt((pointer.x - CanvasRect.x) / CellSize),
                Mathf.FloorToInt((pointer.y - CanvasRect.y) / CellSize));
            return cell.x >= 0 && cell.x < GridWidth
                && cell.y >= 0 && cell.y < GridHeight;
        }
    }

    public static class CanonicalBattlefieldMapEditorOperations
    {
        public static bool TryResolveRectangle(BattlefieldMapAuthoringAsset asset,
            Vector2Int first, Vector2Int second, out IReadOnlyList<Vector2Int> cells,
            out string reason)
        {
            cells = Array.Empty<Vector2Int>();
            if (asset == null)
            {
                reason = "A map authoring asset is required.";
                return false;
            }
            if (!asset.InBounds(first) || !asset.InBounds(second))
            {
                reason = "Rectangle endpoints must be inside the bounded canvas.";
                return false;
            }
            var values = new List<Vector2Int>();
            for (var y = Math.Min(first.y, second.y); y <= Math.Max(first.y, second.y); y++)
            for (var x = Math.Min(first.x, second.x); x <= Math.Max(first.x, second.x); x++)
                values.Add(new Vector2Int(x, y));
            cells = values.AsReadOnly();
            reason = "ok";
            return true;
        }

        public static bool TryResolveVisualFlood(BattlefieldMapAuthoringAsset asset,
            Vector2Int start, out IReadOnlyList<Vector2Int> cells, out string reason)
        {
            cells = Array.Empty<Vector2Int>();
            BattlefieldVisualCellAuthoringRecord sample;
            if (asset == null || !asset.TryGetVisual(start, out sample))
            {
                reason = "Flood start must resolve to a valid visual cell.";
                return false;
            }
            var resolved = ResolveFlood(asset, start, cell =>
            {
                BattlefieldVisualCellAuthoringRecord value;
                return asset.TryGetVisual(cell, out value) && value != null
                    && string.Equals(value.BaseSurfaceId, sample.BaseSurfaceId,
                        StringComparison.Ordinal)
                    && string.Equals(value.LandformSurfaceId, sample.LandformSurfaceId,
                        StringComparison.Ordinal)
                    && string.Equals(value.ContourStyleId, sample.ContourStyleId,
                        StringComparison.Ordinal)
                    && string.Equals(value.EdgeStyleId, sample.EdgeStyleId,
                        StringComparison.Ordinal);
            });
            cells = resolved.AsReadOnly();
            reason = "ok";
            return true;
        }

        public static bool TryResolveGameplayFlood(BattlefieldMapAuthoringAsset asset,
            Vector2Int start, out IReadOnlyList<Vector2Int> cells, out string reason)
        {
            cells = Array.Empty<Vector2Int>();
            BattlefieldGameplayCellAuthoringRecord sample;
            if (asset == null || !asset.TryGetGameplay(start, out sample))
            {
                reason = "Flood start must resolve to a valid gameplay cell.";
                return false;
            }
            var resolved = ResolveFlood(asset, start, cell =>
            {
                BattlefieldGameplayCellAuthoringRecord value;
                return asset.TryGetGameplay(cell, out value) && value != null
                    && value.CapabilityIds.SequenceEqual(sample.CapabilityIds)
                    && value.CollisionIds.SequenceEqual(sample.CollisionIds);
            });
            cells = resolved.AsReadOnly();
            reason = "ok";
            return true;
        }

        public static bool TryApplyVisual(BattlefieldMapAuthoringAsset asset,
            IEnumerable<Vector2Int> cells, string baseSurfaceId,
            string landformSurfaceId, string edgeStyleId, out string reason)
        {
            return TryApplyVisual(asset, cells, baseSurfaceId, landformSurfaceId,
                string.IsNullOrEmpty(landformSurfaceId)
                    ? string.Empty : FruitDefense.Core.BattlefieldLayerIds.ContourStyles.Square,
                edgeStyleId, out reason);
        }

        public static bool TryApplyVisual(BattlefieldMapAuthoringAsset asset,
            IEnumerable<Vector2Int> cells, string baseSurfaceId,
            string landformSurfaceId, string contourStyleId, string edgeStyleId,
            out string reason)
        {
            if (asset == null)
            {
                reason = "A map authoring asset is required.";
                return false;
            }
            return asset.TrySetVisualCells(cells, baseSurfaceId,
                landformSurfaceId, contourStyleId, edgeStyleId, out reason);
        }

        public static bool TryApplyGameplay(BattlefieldMapAuthoringAsset asset,
            IEnumerable<Vector2Int> cells, IEnumerable<string> capabilityIds,
            IEnumerable<string> collisionIds, out string reason)
        {
            if (asset == null)
            {
                reason = "A map authoring asset is required.";
                return false;
            }
            return asset.TrySetGameplayCells(cells, capabilityIds, collisionIds,
                out reason);
        }

        private static List<Vector2Int> ResolveFlood(BattlefieldMapAuthoringAsset asset,
            Vector2Int start, Func<Vector2Int, bool> matches)
        {
            var resolved = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            seen.Add(start);
            var offsets = new[]
            {
                Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down,
            };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!matches(current)) continue;
                resolved.Add(current);
                foreach (var offset in offsets)
                {
                    var candidate = current + offset;
                    if (asset.InBounds(candidate) && seen.Add(candidate))
                        queue.Enqueue(candidate);
                }
            }
            return resolved.OrderBy(cell => cell.y).ThenBy(cell => cell.x).ToList();
        }
    }
}
