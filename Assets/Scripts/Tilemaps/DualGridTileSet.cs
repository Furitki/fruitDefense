using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Tilemaps
{
    [CreateAssetMenu(fileName = "DualGridTileSet", menuName = "Fruit Defense/Dual-Grid Tile Set")]
    public sealed class DualGridTileSet : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Mask 0 may be empty for transparent overlay layers. Masks 1-15 are required.")]
        private TileBase[] maskTiles = new TileBase[DualGridMaskUtility.MaskCount];

        public int Count
        {
            get { return maskTiles == null ? 0 : maskTiles.Length; }
        }

        public TileBase GetTile(DualGridMask mask)
        {
            var index = (int)mask;
            if (index < 0 || index >= DualGridMaskUtility.MaskCount)
                throw new ArgumentOutOfRangeException(nameof(mask), mask, "Dual-Grid masks range from 0 to 15.");
            EnsureSlotCount();
            return maskTiles[index];
        }

        public void SetTile(DualGridMask mask, TileBase tile)
        {
            var index = (int)mask;
            if (index < 0 || index >= DualGridMaskUtility.MaskCount)
                throw new ArgumentOutOfRangeException(nameof(mask), mask, "Dual-Grid masks range from 0 to 15.");
            EnsureSlotCount();
            maskTiles[index] = tile;
        }

        public bool TryGetSprite(DualGridMask mask, out Sprite sprite)
        {
            sprite = null;
            var tile = GetTile(mask) as Tile;
            if (tile == null || tile.sprite == null || tile.sprite.texture == null) return false;
            sprite = tile.sprite;
            return true;
        }

        public bool Validate(out string reason)
        {
            EnsureSlotCount();
            var nativeSize = Vector2.zero;
            var normalizedSize = Vector2.zero;
            var normalizedPivot = Vector2.zero;
            for (var mask = 1; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                Sprite sprite;
                if (maskTiles[mask] == null
                    || !TryGetSprite((DualGridMask)mask, out sprite))
                {
                    reason = "Dual-Grid tile set is missing a renderable Sprite for mask "
                        + mask + " (" + Convert.ToString(mask, 2).PadLeft(4, '0') + ").";
                    return false;
                }
                if (sprite.rect.width <= 0f || sprite.rect.height <= 0f
                    || sprite.pixelsPerUnit <= 0f)
                {
                    reason = "Dual-Grid mask " + mask
                        + " has invalid native dimensions or pixels-per-unit.";
                    return false;
                }
                var candidateNativeSize = new Vector2(sprite.rect.width, sprite.rect.height);
                var candidateNormalizedSize = candidateNativeSize / sprite.pixelsPerUnit;
                var candidateNormalizedPivot = new Vector2(
                    sprite.pivot.x / sprite.rect.width,
                    sprite.pivot.y / sprite.rect.height);
                if (nativeSize == Vector2.zero)
                {
                    nativeSize = candidateNativeSize;
                    normalizedSize = candidateNormalizedSize;
                    normalizedPivot = candidateNormalizedPivot;
                    continue;
                }
                if (!Approximately(nativeSize, candidateNativeSize))
                {
                    reason = "Dual-Grid mask " + mask + " uses native dimensions "
                        + candidateNativeSize + " but this TileSet requires " + nativeSize
                        + " for every mask.";
                    return false;
                }
                if (!Approximately(normalizedSize, candidateNormalizedSize)
                    || !Approximately(normalizedPivot, candidateNormalizedPivot))
                {
                    reason = "Dual-Grid mask " + mask
                        + " does not share the TileSet's normalized size and pivot socket frame.";
                    return false;
                }
            }

            reason = "ok";
            return true;
        }

        public bool HasCompatibleNormalizedSockets(DualGridTileSet other, out string reason)
        {
            if (other == null)
            {
                reason = "A second Dual-Grid TileSet is required for normalized socket validation.";
                return false;
            }
            if (!Validate(out reason) || !other.Validate(out reason)) return false;
            for (var mask = 1; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                Sprite first;
                Sprite second;
                if (!TryGetSprite((DualGridMask)mask, out first)
                    || !other.TryGetSprite((DualGridMask)mask, out second))
                {
                    reason = "Dual-Grid normalized socket validation cannot resolve mask "
                        + mask + ".";
                    return false;
                }
                if (!Approximately(NormalizedSize(first), NormalizedSize(second))
                    || !Approximately(NormalizedPivot(first), NormalizedPivot(second)))
                {
                    reason = "Dual-Grid mask " + mask
                        + " has an incompatible normalized size or pivot socket frame.";
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        private void OnValidate()
        {
            EnsureSlotCount();
        }

        private void EnsureSlotCount()
        {
            if (maskTiles != null && maskTiles.Length == DualGridMaskUtility.MaskCount) return;
            var resized = new TileBase[DualGridMaskUtility.MaskCount];
            if (maskTiles != null)
                Array.Copy(maskTiles, resized, Math.Min(maskTiles.Length, resized.Length));
            maskTiles = resized;
        }

        private static Vector2 NormalizedSize(Sprite sprite)
        {
            return new Vector2(sprite.rect.width / sprite.pixelsPerUnit,
                sprite.rect.height / sprite.pixelsPerUnit);
        }

        private static Vector2 NormalizedPivot(Sprite sprite)
        {
            return new Vector2(sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);
        }

        private static bool Approximately(Vector2 first, Vector2 second)
        {
            return Mathf.Abs(first.x - second.x) <= .0001f
                && Mathf.Abs(first.y - second.y) <= .0001f;
        }
    }
}
