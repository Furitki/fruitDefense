using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FruitDefense.Core
{
    public readonly struct BattlefieldViewportLayout
    {
        public Vector2 ViewportSize { get; }
        public Rect SafeArea { get; }
        public Rect SafeAreaInGuiSpace { get; }
        public Rect DesignViewportRect { get; }
        public float Scale { get; }
        public Vector2 Offset { get; }
        public Matrix4x4 GuiMatrix { get; }

        public BattlefieldViewportLayout(
            Vector2 viewportSize,
            Rect safeArea,
            Rect safeAreaInGuiSpace,
            Rect designViewportRect,
            float scale,
            Vector2 offset)
        {
            ViewportSize = viewportSize;
            SafeArea = safeArea;
            SafeAreaInGuiSpace = safeAreaInGuiSpace;
            DesignViewportRect = designViewportRect;
            Scale = scale;
            Offset = offset;
            GuiMatrix = Matrix4x4.TRS(new Vector3(offset.x, offset.y, 0f), Quaternion.identity,
                new Vector3(scale, scale, 1f));
        }

        public Rect ProjectDesignRect(Rect designRect)
        {
            return new Rect(
                Offset.x + designRect.x * Scale,
                Offset.y + designRect.y * Scale,
                designRect.width * Scale,
                designRect.height * Scale);
        }
    }

    public sealed class BattlefieldProjection
    {
        private const float RectangleOverlapEpsilon = 0.001f;
        public const float PotVisualRatio = 0.88f;
        public const float CoreVisualRatio = 0.84f;
        public const float ReferenceLegacyPotSize = 62f;
        public const float PreviousReferenceBoardWidth = 386f;
        public const float PreviousReferenceBoardScaleDivisor = 1050f;
        public const float ReferencePotSize = ReferenceLegacyPotSize * PreviousReferenceBoardWidth
            / PreviousReferenceBoardScaleDivisor * 2f;

        private const float BoardPadding = 2f;

        private readonly BattlefieldMapDefinition _map;
        private readonly Vector2 _mapOrigin;

        public Rect BoardRect { get; private set; }
        public Rect MapViewportRect { get; private set; }
        public Rect ContentRect { get; private set; }
        public Rect GridRect { get; private set; }
        public Rect CoreRect { get; private set; }
        public float MapScale { get; private set; }
        public float PotSize { get; private set; }
        public float CellSize { get; private set; }
        public float TileSize { get; private set; }
        public IReadOnlyList<Vector2> RoutePoints { get; private set; }

        public static IReadOnlyList<Vector2Int> RequiredPortraitViewports { get; } = new[]
        {
            new Vector2Int(360, 800),
            new Vector2Int(375, 812),
            new Vector2Int(402, 874),
            new Vector2Int(430, 932),
        };

        public static BattlefieldViewportLayout CalculateViewportLayout(
            float viewportWidth,
            float viewportHeight,
            Rect safeArea,
            float designWidth,
            float designHeight)
        {
            viewportWidth = Mathf.Max(0f, viewportWidth);
            viewportHeight = Mathf.Max(0f, viewportHeight);
            var viewport = new Rect(0f, 0f, viewportWidth, viewportHeight);
            var safeXMin = Mathf.Clamp(safeArea.xMin, viewport.xMin, viewport.xMax);
            var safeYMin = Mathf.Clamp(safeArea.yMin, viewport.yMin, viewport.yMax);
            var safeXMax = Mathf.Clamp(safeArea.xMax, viewport.xMin, viewport.xMax);
            var safeYMax = Mathf.Clamp(safeArea.yMax, viewport.yMin, viewport.yMax);
            var resolvedSafeArea = Rect.MinMaxRect(safeXMin, safeYMin, safeXMax, safeYMax);
            if (resolvedSafeArea.width <= 0f || resolvedSafeArea.height <= 0f)
                resolvedSafeArea = viewport;

            designWidth = Mathf.Max(.0001f, designWidth);
            designHeight = Mathf.Max(.0001f, designHeight);
            var scale = Mathf.Max(0f, Mathf.Min(
                resolvedSafeArea.width / designWidth,
                resolvedSafeArea.height / designHeight));
            var offsetX = resolvedSafeArea.x + (resolvedSafeArea.width - designWidth * scale) * .5f;
            var safeTop = viewportHeight - resolvedSafeArea.yMax;
            var offsetY = safeTop + (resolvedSafeArea.height - designHeight * scale) * .5f;
            var guiSafeArea = new Rect(
                resolvedSafeArea.x,
                safeTop,
                resolvedSafeArea.width,
                resolvedSafeArea.height);
            var designViewport = new Rect(offsetX, offsetY, designWidth * scale, designHeight * scale);
            return new BattlefieldViewportLayout(
                new Vector2(viewportWidth, viewportHeight),
                resolvedSafeArea,
                guiSafeArea,
                designViewport,
                scale,
                new Vector2(offsetX, offsetY));
        }

        public BattlefieldProjection(BattlefieldMapDefinition map, Rect boardRect)
        {
            _map = map;
            BoardRect = boardRect;
            MapViewportRect = boardRect;
            ContentRect = new Rect(
                MapViewportRect.x + BoardPadding,
                MapViewportRect.y + BoardPadding,
                Mathf.Max(0f, MapViewportRect.width - BoardPadding * 2f),
                Mathf.Max(0f, MapViewportRect.height - BoardPadding * 2f));
            var gridWidth = Mathf.Max(1, map.GridWidth);
            var gridHeight = Mathf.Max(1, map.GridHeight);
            TileSize = Mathf.Max(0f, Mathf.Min(ContentRect.width / gridWidth, ContentRect.height / gridHeight));
            var projectedWidth = TileSize * gridWidth;
            var projectedHeight = TileSize * gridHeight;
            GridRect = new Rect(
                ContentRect.center.x - projectedWidth * .5f,
                ContentRect.center.y - projectedHeight * .5f,
                projectedWidth,
                projectedHeight);
            MapScale = map.MapUnitsPerCell <= .0001f ? TileSize : TileSize / map.MapUnitsPerCell;
            _mapOrigin = new Vector2(
                GridRect.xMin + TileSize * .5f,
                GridRect.yMin + TileSize * .5f);
            CellSize = TileSize;
            PotSize = TileSize * PotVisualRatio;
            CoreRect = TileLocalVisualRect(map.CoreCell, CoreVisualRatio);
            RoutePoints = map.RouteNodes.Select(MapToScreen).ToArray();
        }

        public Vector2 MapToScreen(Vector2 point)
        {
            return _mapOrigin + point * MapScale;
        }

        public Rect TileRect(Vector2Int cell)
        {
            return new Rect(
                GridRect.xMin + cell.x * TileSize,
                GridRect.yMin + cell.y * TileSize,
                TileSize,
                TileSize);
        }

        public Rect RouteTileRect(Vector2Int cell)
        {
            return TileRect(cell);
        }

        public Rect PotHitRect(Vector2Int cell)
        {
            return TileRect(cell);
        }

        public Rect PotVisualRect(Vector2Int cell)
        {
            return TileLocalVisualRect(cell, PotVisualRatio);
        }

        // Compatibility aliases for callers outside the presentation migration.
        public Rect CellRect(Vector2Int cell)
        {
            return TileRect(cell);
        }

        public Rect PotRect(Vector2Int cell)
        {
            return PotHitRect(cell);
        }

        public Rect MapRect(Vector2 center, float mapWidth, float mapHeight)
        {
            return CenteredRect(MapToScreen(center), MapDistanceToScreen(mapWidth), MapDistanceToScreen(mapHeight));
        }

        public Rect LegacyVisualRect(Vector2 center, float legacyWidth, float legacyHeight)
        {
            return CenteredRect(MapToScreen(center), LegacyVisualSize(legacyWidth), LegacyVisualSize(legacyHeight));
        }

        public Rect EntityRect(Vector2 center, float legacySize)
        {
            var size = LegacyVisualSize(legacySize);
            return CenteredRect(MapToScreen(center), size, size);
        }

        public float MapDistanceToScreen(float mapDistance)
        {
            return mapDistance * MapScale;
        }

        public float LegacyVisualSize(float legacyDesignSize)
        {
            return legacyDesignSize * BoardRect.width / PreviousReferenceBoardScaleDivisor;
        }

        public bool ValidatePlantingGeometry(out string reason)
        {
            if (_map.PlantableCells.Count == 0)
            {
                reason = "expected projected planting cells";
                return false;
            }
            var rects = _map.PlantableCells.Select(PotHitRect).ToArray();
            for (var index = 0; index < rects.Length; index++)
            {
                var rect = rects[index];
                if (rect.xMin < GridRect.xMin - .01f || rect.yMin < GridRect.yMin - .01f
                    || rect.xMax > GridRect.xMax + .01f || rect.yMax > GridRect.yMax + .01f)
                {
                    reason = "projected cell is clipped by battlefield: " + _map.PlantableCells[index];
                    return false;
                }
                if (Mathf.Abs(rect.width - rect.height) > .01f)
                {
                    reason = "projected tile is not square: " + rect;
                    return false;
                }
                var visual = PotVisualRect(_map.PlantableCells[index]);
                if (!Contains(rect, visual)
                    || Mathf.Abs(visual.width / rect.width - PotVisualRatio) > .001f
                    || PotHitRect(_map.PlantableCells[index]) != TileRect(_map.PlantableCells[index]))
                {
                    reason = "flowerpot visual and hit bounds are inconsistent";
                    return false;
                }
                for (var other = index + 1; other < rects.Length; other++)
                {
                    if (!HasPositiveAreaOverlap(rect, rects[other])) continue;
                    reason = "projected flowerpot targets overlap: " + _map.PlantableCells[index]
                        + " and " + _map.PlantableCells[other];
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        private static Rect CenteredRect(Vector2 center, float width, float height)
        {
            return new Rect(center.x - width * .5f, center.y - height * .5f, width, height);
        }

        private Rect TileLocalVisualRect(Vector2Int cell, float ratio)
        {
            var tile = TileRect(cell);
            var size = tile.width * Mathf.Clamp01(ratio);
            return CenteredRect(tile.center, size, size);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - .01f && inner.yMin >= outer.yMin - .01f
                && inner.xMax <= outer.xMax + .01f && inner.yMax <= outer.yMax + .01f;
        }

        private static bool HasPositiveAreaOverlap(Rect first, Rect second)
        {
            var overlapWidth = Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin);
            var overlapHeight = Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin);
            return overlapWidth > RectangleOverlapEpsilon && overlapHeight > RectangleOverlapEpsilon;
        }
    }
}
