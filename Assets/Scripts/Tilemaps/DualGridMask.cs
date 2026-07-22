using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Tilemaps
{
    [Flags]
    public enum DualGridMask : byte
    {
        Empty = 0,
        NorthWest = 1,
        NorthEast = 2,
        SouthEast = 4,
        SouthWest = 8,
        Full = NorthWest | NorthEast | SouthEast | SouthWest,
    }

    public static class DualGridMaskUtility
    {
        public const int MaskCount = 16;

        public static DualGridMask Resolve(Tilemap logicalTilemap, Vector3Int vertex)
        {
            if (logicalTilemap == null) throw new ArgumentNullException(nameof(logicalTilemap));
            return Resolve(logicalTilemap.HasTile, vertex);
        }

        public static DualGridMask Resolve(Func<Vector3Int, bool> isOccupied, Vector3Int vertex)
        {
            if (isOccupied == null) throw new ArgumentNullException(nameof(isOccupied));

            var mask = DualGridMask.Empty;
            if (isOccupied(LogicalCell(vertex, DualGridMask.NorthWest))) mask |= DualGridMask.NorthWest;
            if (isOccupied(LogicalCell(vertex, DualGridMask.NorthEast))) mask |= DualGridMask.NorthEast;
            if (isOccupied(LogicalCell(vertex, DualGridMask.SouthEast))) mask |= DualGridMask.SouthEast;
            if (isOccupied(LogicalCell(vertex, DualGridMask.SouthWest))) mask |= DualGridMask.SouthWest;
            return mask;
        }

        public static Vector3Int LogicalCell(Vector3Int vertex, DualGridMask corner)
        {
            switch (corner)
            {
                case DualGridMask.NorthWest:
                    return vertex + Vector3Int.left;
                case DualGridMask.NorthEast:
                    return vertex;
                case DualGridMask.SouthEast:
                    return vertex + Vector3Int.down;
                case DualGridMask.SouthWest:
                    return vertex + Vector3Int.left + Vector3Int.down;
                default:
                    throw new ArgumentOutOfRangeException(nameof(corner), corner,
                        "Expected one Dual-Grid corner bit.");
            }
        }

        public static void GetAffectedVertices(Vector3Int logicalCell, Vector3Int[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < 4) throw new ArgumentException("Four vertex slots are required.", nameof(buffer));

            buffer[0] = logicalCell;
            buffer[1] = logicalCell + Vector3Int.right;
            buffer[2] = logicalCell + Vector3Int.up;
            buffer[3] = logicalCell + Vector3Int.right + Vector3Int.up;
        }

        public static bool TryGetOccupiedBounds(Tilemap logicalTilemap, out BoundsInt occupiedBounds)
        {
            occupiedBounds = default(BoundsInt);
            if (logicalTilemap == null) return false;

            var found = false;
            var min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            var max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            foreach (var position in logicalTilemap.cellBounds.allPositionsWithin)
            {
                if (!logicalTilemap.HasTile(position)) continue;
                found = true;
                min = Vector3Int.Min(min, position);
                max = Vector3Int.Max(max, position);
            }

            if (!found) return false;
            occupiedBounds = new BoundsInt(min, max - min + Vector3Int.one);
            return true;
        }

        public static BoundsInt ToVertexBounds(BoundsInt occupiedBounds)
        {
            return new BoundsInt(
                occupiedBounds.position,
                new Vector3Int(occupiedBounds.size.x + 1, occupiedBounds.size.y + 1,
                    Mathf.Max(1, occupiedBounds.size.z)));
        }
    }
}
