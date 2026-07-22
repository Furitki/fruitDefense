using System;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Tilemaps
{
    public enum BattlefieldDualGridLayer
    {
        PlantableGrass,
        MonsterRoute,
    }

    public static class BattlefieldDualGridTerrain
    {
        public static int VisualTileCount(BattlefieldMapDefinition map)
        {
            if (map == null) return 0;
            return (map.GridWidth + 1) * (map.GridHeight + 1);
        }

        public static DualGridMask ResolveMask(BattlefieldMapDefinition map, int vertexX, int vertexY,
            BattlefieldDualGridLayer layer)
        {
            return ResolveMask(map, vertexX, vertexY,
                layer == BattlefieldDualGridLayer.PlantableGrass
                    ? BattlefieldLayerIds.Surfaces.Grass
                    : BattlefieldLayerIds.Surfaces.StoneRoad);
        }

        public static DualGridMask ResolveMask(BattlefieldMapDefinition map, int vertexX, int vertexY,
            string surfaceId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(surfaceId)) throw new ArgumentException(
                "Semantic surface identity is required.", nameof(surfaceId));

            // Battlefield GUI coordinates increase downward. Mapping them into the existing
            // y-up mask resolver keeps NW/NE/SE/SW semantics identical to authored Tilemaps.
            var vertex = new Vector3Int(vertexX, -vertexY, 0);
            return DualGridMaskUtility.Resolve(logicalCell =>
            {
                var battlefieldCell = new Vector2Int(logicalCell.x, -logicalCell.y - 1);
                return string.Equals(map.SurfaceAt(battlefieldCell), surfaceId, StringComparison.Ordinal);
            }, vertex);
        }

        public static bool Validate(BattlefieldTerrainPalette palette, out string reason)
        {
            if (palette == null)
            {
                reason = "Battlefield terrain palette is required.";
                return false;
            }
            if (!palette.Validate(out reason)) return false;
            var reference = palette.ReferenceTileSet;
            Sprite referenceFull;
            if (reference == null || !reference.TryGetSprite(DualGridMask.Full, out referenceFull))
            {
                reason = "Battlefield terrain palette requires a full reference tile.";
                return false;
            }
            foreach (var binding in palette.SurfaceBindings)
            {
                if (!ValidateTileSet(binding.TileSet, binding.SurfaceId, out reason)) return false;
                Sprite full;
                if (!binding.TileSet.TryGetSprite(DualGridMask.Full, out full)
                    || Mathf.Abs(referenceFull.rect.width - full.rect.width) > .001f
                    || Mathf.Abs(referenceFull.rect.height - full.rect.height) > .001f)
                {
                    reason = "Battlefield terrain palette TileSets must share one native tile size.";
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        public static Rect VisualTileRect(BattlefieldProjection projection, int vertexX, int vertexY)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var size = projection.TileSize;
            return new Rect(
                projection.GridRect.xMin + (vertexX - .5f) * size,
                projection.GridRect.yMin + (vertexY - .5f) * size,
                size,
                size);
        }

        public static Rect BaseTextureUv(BattlefieldMapDefinition map, DualGridTileSet tileSet,
            Texture2D baseTexture)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (tileSet == null) throw new ArgumentNullException(nameof(tileSet));
            if (baseTexture == null) throw new ArgumentNullException(nameof(baseTexture));

            Sprite fullSprite;
            if (!tileSet.TryGetSprite(DualGridMask.Full, out fullSprite))
                return new Rect(0f, 0f, 1f, 1f);
            var nativeWidth = Mathf.Max(1f, fullSprite.rect.width);
            var nativeHeight = Mathf.Max(1f, fullSprite.rect.height);
            return new Rect(
                0f,
                0f,
                map.GridWidth * nativeWidth / Mathf.Max(1, baseTexture.width),
                map.GridHeight * nativeHeight / Mathf.Max(1, baseTexture.height));
        }

        public static Rect SpriteUv(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return default(Rect);
            var texture = sprite.texture;
            var rect = sprite.textureRect;
            return new Rect(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height);
        }

        public static bool Validate(DualGridTileSet grassTileSet, DualGridTileSet routeTileSet,
            Texture2D baseTexture, out string reason)
        {
            if (!ValidateTileSet(grassTileSet, "grass", out reason)) return false;
            if (!ValidateTileSet(routeTileSet, "route", out reason)) return false;
            if (baseTexture == null)
            {
                reason = "Battlefield Dual-Grid soil base texture is required.";
                return false;
            }
            if (baseTexture.width <= 0 || baseTexture.height <= 0)
            {
                reason = "Battlefield Dual-Grid soil base texture dimensions are invalid.";
                return false;
            }

            Sprite grassFull;
            Sprite routeFull;
            if (!grassTileSet.TryGetSprite(DualGridMask.Full, out grassFull)
                || !routeTileSet.TryGetSprite(DualGridMask.Full, out routeFull)
                || Mathf.Abs(grassFull.rect.width - routeFull.rect.width) > .001f
                || Mathf.Abs(grassFull.rect.height - routeFull.rect.height) > .001f)
            {
                reason = "Battlefield grass and route TileSets must share one native tile size.";
                return false;
            }

            reason = "ok";
            return true;
        }

        private static bool ValidateTileSet(DualGridTileSet tileSet, string label, out string reason)
        {
            if (tileSet == null)
            {
                reason = "Battlefield Dual-Grid " + label + " TileSet is required.";
                return false;
            }
            if (!tileSet.Validate(out reason)) return false;

            for (var numericMask = 1; numericMask < DualGridMaskUtility.MaskCount; numericMask++)
            {
                Sprite sprite;
                if (tileSet.TryGetSprite((DualGridMask)numericMask, out sprite)
                    && sprite.rect.width > 0f && sprite.rect.height > 0f) continue;
                reason = "Battlefield Dual-Grid " + label + " mask " + numericMask
                    + " does not resolve to a renderable Sprite.";
                return false;
            }

            reason = "ok";
            return true;
        }
    }
}
