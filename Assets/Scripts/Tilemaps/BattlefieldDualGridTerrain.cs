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
            return ResolveLandformMask(map, vertexX, vertexY, surfaceId);
        }

        public static DualGridMask ResolveLandformMask(BattlefieldMapDefinition map, int vertexX, int vertexY,
            string surfaceId)
        {
            return ResolveLandformMask(map, vertexX, vertexY, surfaceId,
                BattlefieldLayerIds.ContourStyles.Organic);
        }

        public static DualGridMask ResolveLandformMask(BattlefieldMapDefinition map, int vertexX,
            int vertexY, string surfaceId, string contourStyleId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(surfaceId)) throw new ArgumentException(
                "Semantic surface identity is required.", nameof(surfaceId));
            if (string.IsNullOrWhiteSpace(contourStyleId)) throw new ArgumentException(
                "Contour style identity is required.", nameof(contourStyleId));

            // Battlefield GUI coordinates increase downward. Mapping them into the existing
            // y-up mask resolver keeps NW/NE/SE/SW semantics identical to authored Tilemaps.
            var vertex = new Vector3Int(vertexX, -vertexY, 0);
            return DualGridMaskUtility.Resolve(logicalCell =>
            {
                var battlefieldCell = new Vector2Int(logicalCell.x, -logicalCell.y - 1);
                return string.Equals(map.LandformSurfaceAt(battlefieldCell), surfaceId,
                           StringComparison.Ordinal)
                    && string.Equals(map.ContourStyleAt(battlefieldCell), contourStyleId,
                        StringComparison.Ordinal);
            }, vertex);
        }

        public static DualGridMask ResolveEdgeMask(BattlefieldMapDefinition map, int vertexX, int vertexY,
            string landformSurfaceId, string baseSurfaceId, string edgeStyleId)
        {
            return ResolveEdgeMask(map, vertexX, vertexY, landformSurfaceId, baseSurfaceId,
                BattlefieldLayerIds.ContourStyles.Organic, edgeStyleId);
        }

        public static DualGridMask ResolveEdgeMask(BattlefieldMapDefinition map, int vertexX,
            int vertexY, string landformSurfaceId, string baseSurfaceId,
            string contourStyleId, string edgeStyleId)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrWhiteSpace(landformSurfaceId)) throw new ArgumentException(
                "Landform surface identity is required.", nameof(landformSurfaceId));
            if (string.IsNullOrWhiteSpace(baseSurfaceId)) throw new ArgumentException(
                "Base surface identity is required.", nameof(baseSurfaceId));
            if (string.IsNullOrWhiteSpace(contourStyleId)) throw new ArgumentException(
                "Contour style identity is required.", nameof(contourStyleId));
            if (string.IsNullOrWhiteSpace(edgeStyleId)) throw new ArgumentException(
                "Edge style identity is required.", nameof(edgeStyleId));

            var vertex = new Vector3Int(vertexX, -vertexY, 0);
            return DualGridMaskUtility.Resolve(logicalCell =>
            {
                var battlefieldCell = new Vector2Int(logicalCell.x, -logicalCell.y - 1);
                return string.Equals(map.LandformSurfaceAt(battlefieldCell), landformSurfaceId,
                           StringComparison.Ordinal)
                    && string.Equals(map.BaseSurfaceAt(battlefieldCell), baseSurfaceId,
                        StringComparison.Ordinal)
                    && string.Equals(map.ContourStyleAt(battlefieldCell), contourStyleId,
                        StringComparison.Ordinal)
                    && string.Equals(map.EdgeStyleAt(battlefieldCell), edgeStyleId,
                        StringComparison.Ordinal);
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
            foreach (var binding in palette.SurfaceBindings)
            {
                Texture2D baseTexture;
                if (binding == null || !palette.TryGetBaseTexture(binding.SurfaceId, out baseTexture)
                    || baseTexture.width <= 0 || baseTexture.height <= 0)
                {
                    reason = "Battlefield terrain material requires a renderable base texture.";
                    return false;
                }
            }
            foreach (var binding in palette.LandformBindings)
            {
                if (binding == null || !ValidateTileSet(binding.TileSet,
                        binding.SurfaceId + " " + binding.ContourStyleId, out reason)) return false;
            }
            foreach (var binding in palette.EdgeBindings)
            {
                if (binding == null || !ValidateTileSet(binding.TileSet,
                        binding.LandformSurfaceId + " on " + binding.BaseSurfaceId + " "
                        + binding.ContourStyleId + " edge", out reason))
                    return false;
            }
            reason = "ok";
            return true;
        }

        public static bool Validate(BattlefieldMapDefinition map, BattlefieldTerrainPalette palette,
            out string reason)
        {
            if (map == null)
            {
                reason = "Battlefield map is required for terrain binding validation.";
                return false;
            }
            if (!Validate(palette, out reason)) return false;
            for (var index = 0; index < map.VisualCells.Count; index++)
            {
                var visual = map.VisualCells[index];
                if (visual == null) continue;
                Texture2D baseTexture;
                if (!palette.TryGetBaseTexture(visual.BaseSurfaceId, out baseTexture))
                {
                    reason = "Terrain palette '" + palette.PaletteId + "' has no base binding for '"
                        + visual.BaseSurfaceId + "' at visual cell " + index + ".";
                    return false;
                }
                if (string.IsNullOrEmpty(visual.LandformSurfaceId)) continue;
                DualGridTileSet landform;
                if (!palette.TryGetLandformTileSet(visual.LandformSurfaceId,
                        visual.ContourStyleId, out landform))
                {
                    reason = "Terrain palette '" + palette.PaletteId
                        + "' has no landform binding for surface '" + visual.LandformSurfaceId
                        + "' with contour '" + visual.ContourStyleId + "' at visual cell "
                        + index + ".";
                    return false;
                }
                if (string.IsNullOrEmpty(visual.EdgeStyleId)) continue;
                DualGridTileSet edge;
                bool complementMask;
                if (!palette.TryGetEdgeTileSet(visual.LandformSurfaceId,
                        visual.BaseSurfaceId, visual.ContourStyleId, visual.EdgeStyleId,
                        out edge, out complementMask))
                {
                    reason = "Terrain palette '" + palette.PaletteId
                        + "' has no directed edge binding for foreground '"
                        + visual.LandformSurfaceId + "', background '" + visual.BaseSurfaceId
                        + "', contour '" + visual.ContourStyleId + "', style '"
                        + visual.EdgeStyleId + "' at visual cell " + index + ".";
                    return false;
                }
                if (!landform.HasCompatibleNormalizedSockets(edge, out reason))
                {
                    reason = "Terrain palette '" + palette.PaletteId
                        + "' resolved an incompatible "
                        + (complementMask ? "shared reverse" : "exact")
                        + " edge for foreground '" + visual.LandformSurfaceId
                        + "', background '" + visual.BaseSurfaceId + "', contour '"
                        + visual.ContourStyleId + "'.";
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
            if (baseTexture == null) throw new ArgumentNullException(nameof(baseTexture));

            return new Rect(0f, 0f, map.GridWidth, map.GridHeight);
        }

        public static Rect BaseCellUv(BattlefieldMapDefinition map, DualGridTileSet tileSet,
            Texture2D baseTexture, int cellX, int cellY)
        {
            return BaseCellUv(map, baseTexture, cellX, cellY);
        }

        public static Rect BaseCellUv(BattlefieldMapDefinition map, Texture2D baseTexture,
            int cellX, int cellY)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (baseTexture == null) throw new ArgumentNullException(nameof(baseTexture));

            return new Rect(cellX, map.GridHeight - cellY - 1, 1f, 1f);
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
