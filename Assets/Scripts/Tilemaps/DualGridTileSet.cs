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
            for (var mask = 1; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                if (maskTiles[mask] != null) continue;
                reason = "Dual-Grid tile set is missing required mask " + mask + " ("
                    + Convert.ToString(mask, 2).PadLeft(4, '0') + ").";
                return false;
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
    }
}
