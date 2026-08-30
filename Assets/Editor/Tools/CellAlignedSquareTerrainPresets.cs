using System;
using System.Collections.Generic;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEngine;

namespace FruitDefense.Editor
{
    internal readonly struct CellAlignedSquareTerrainPreset
    {
        public string DisplayName { get; }
        public string SurfaceId { get; }

        public CellAlignedSquareTerrainPreset(string displayName, string surfaceId)
        {
            DisplayName = displayName ?? string.Empty;
            SurfaceId = surfaceId ?? string.Empty;
        }
    }

    internal static class CellAlignedSquareTerrainPresets
    {
        private static readonly IReadOnlyList<CellAlignedSquareTerrainPreset> Presets =
            Array.AsReadOnly(new[]
            {
                new CellAlignedSquareTerrainPreset("纯草地方块",
                    BattlefieldLayerIds.Surfaces.Grass),
                new CellAlignedSquareTerrainPreset("纯泥地方块",
                    BattlefieldLayerIds.Surfaces.Soil),
            });

        public static IReadOnlyList<CellAlignedSquareTerrainPreset> All
        {
            get { return Presets; }
        }

        public static bool IsAvailable(CellAlignedSquareTerrainPreset preset,
            BattlefieldTerrainPalette palette)
        {
            Texture2D texture;
            return palette != null && palette.TryGetBaseTexture(preset.SurfaceId, out texture)
                && texture != null;
        }

        public static bool TryResolve(string surfaceId, out string baseSurfaceId,
            out string landformSurfaceId, out string contourStyleId, out string edgeStyleId,
            out string reason)
        {
            foreach (var preset in Presets)
            {
                if (!string.Equals(preset.SurfaceId, surfaceId, StringComparison.Ordinal))
                    continue;
                baseSurfaceId = preset.SurfaceId;
                landformSurfaceId = string.Empty;
                contourStyleId = string.Empty;
                edgeStyleId = string.Empty;
                reason = "ok";
                return true;
            }

            baseSurfaceId = string.Empty;
            landformSurfaceId = string.Empty;
            contourStyleId = string.Empty;
            edgeStyleId = string.Empty;
            reason = "Unsupported pure-square surface: " + (surfaceId ?? string.Empty);
            return false;
        }

        public static bool TryApply(BattlefieldMapAuthoringAsset map,
            IEnumerable<Vector2Int> cells, string surfaceId, out string reason)
        {
            if (map == null)
            {
                reason = "Battlefield map authoring asset is required.";
                return false;
            }

            string baseSurfaceId;
            string landformSurfaceId;
            string contourStyleId;
            string edgeStyleId;
            if (!TryResolve(surfaceId, out baseSurfaceId, out landformSurfaceId,
                    out contourStyleId, out edgeStyleId, out reason)) return false;
            return map.TrySetVisualCells(cells, baseSurfaceId, landformSurfaceId,
                contourStyleId, edgeStyleId, out reason);
        }
    }
}
