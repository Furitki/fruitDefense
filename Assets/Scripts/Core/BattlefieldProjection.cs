using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed class BattlefieldProjection
    {
        public const float ReferenceLegacyPotSize = 62f;
        public const float PreviousReferenceBoardWidth = 386f;
        public const float PreviousReferenceBoardScaleDivisor = 1050f;
        public const float ReferencePotSize = ReferenceLegacyPotSize * PreviousReferenceBoardWidth
            / PreviousReferenceBoardScaleDivisor * 2f;

        private const float BoardPadding = 8f;
        private const float ControlStripHeight = 62f;
        private const float ControlHorizontalPadding = 8f;
        private const float ControlBottomPadding = 2f;
        private const float WaveActionWidth = 184f;
        private const float WaveActionHeight = 44f;

        private readonly BattlefieldMapDefinition _map;
        private readonly Vector2 _mapOrigin;

        public Rect BoardRect { get; private set; }
        public Rect MapViewportRect { get; private set; }
        public Rect ContentRect { get; private set; }
        public Rect ControlStripRect { get; private set; }
        public Rect WaveActionRect { get; private set; }
        public float MapScale { get; private set; }
        public float PotSize { get; private set; }
        public float CellSize { get; private set; }
        public IReadOnlyList<Vector2> RoutePoints { get; private set; }

        public BattlefieldProjection(BattlefieldMapDefinition map, Rect boardRect)
        {
            _map = map;
            BoardRect = boardRect;
            MapViewportRect = new Rect(
                boardRect.x,
                boardRect.y,
                boardRect.width,
                Mathf.Max(0f, boardRect.height - ControlStripHeight));
            ContentRect = new Rect(
                MapViewportRect.x + BoardPadding,
                MapViewportRect.y + BoardPadding,
                Mathf.Max(0f, MapViewportRect.width - BoardPadding * 2f),
                Mathf.Max(0f, MapViewportRect.height - BoardPadding * 2f));
            ControlStripRect = new Rect(
                boardRect.x + ControlHorizontalPadding,
                MapViewportRect.yMax + 12f,
                Mathf.Max(0f, boardRect.width - ControlHorizontalPadding * 2f),
                Mathf.Max(0f, boardRect.yMax - ControlBottomPadding - (MapViewportRect.yMax + 12f)));
            WaveActionRect = new Rect(
                ControlStripRect.xMax - WaveActionWidth,
                boardRect.yMax - ControlBottomPadding - WaveActionHeight,
                WaveActionWidth,
                WaveActionHeight);
            var bounds = map.MapBounds;
            var scaleX = bounds.width <= .0001f ? 1f : ContentRect.width / bounds.width;
            var scaleY = bounds.height <= .0001f ? 1f : ContentRect.height / bounds.height;
            MapScale = Mathf.Min(scaleX, scaleY);
            var projectedWidth = bounds.width * MapScale;
            var projectedHeight = bounds.height * MapScale;
            _mapOrigin = new Vector2(
                ContentRect.center.x - projectedWidth * .5f - bounds.xMin * MapScale,
                ContentRect.center.y - projectedHeight * .5f - bounds.yMin * MapScale);
            var horizontalPitch = map.GridWidth <= 1 ? float.MaxValue : MapScale;
            var verticalPitch = map.GridHeight <= 1 ? float.MaxValue : MapScale;
            PotSize = Mathf.Min(ReferencePotSize, Mathf.Min(horizontalPitch, verticalPitch));
            CellSize = PotSize;
            RoutePoints = map.RouteNodes.Select(MapToScreen).ToArray();
        }

        public Vector2 MapToScreen(Vector2 point)
        {
            return _mapOrigin + point * MapScale;
        }

        public Rect CellRect(Vector2Int cell)
        {
            return CenteredRect(MapToScreen(_map.CellToMap(cell)), CellSize, CellSize);
        }

        public Rect PotRect(Vector2Int cell)
        {
            return CenteredRect(MapToScreen(_map.CellToMap(cell)), PotSize, PotSize);
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
            if (_map.PlantableCells.Count != 48)
            {
                reason = "expected 48 projected planting cells";
                return false;
            }
            var rects = _map.PlantableCells.Select(CellRect).ToArray();
            for (var index = 0; index < rects.Length; index++)
            {
                var rect = rects[index];
                if (rect.xMin < BoardRect.xMin || rect.yMin < BoardRect.yMin
                    || rect.xMax > BoardRect.xMax || rect.yMax > BoardRect.yMax)
                {
                    reason = "projected cell is clipped by battlefield: " + _map.PlantableCells[index];
                    return false;
                }
                if (Mathf.Min(rect.width, rect.height) < 44f)
                {
                    reason = "projected flowerpot target is smaller than 44 logical points: " + rect;
                    return false;
                }
                if (CellRect(_map.PlantableCells[index]) != PotRect(_map.PlantableCells[index]))
                {
                    reason = "cell and flowerpot hit bounds are not shared";
                    return false;
                }
                for (var other = index + 1; other < rects.Length; other++)
                {
                    if (!rect.Overlaps(rects[other])) continue;
                    reason = "projected flowerpot targets overlap: " + _map.PlantableCells[index]
                        + " and " + _map.PlantableCells[other];
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        public bool ValidateControlInset(out string reason)
        {
            if (Mathf.Min(WaveActionRect.width, WaveActionRect.height) < 44f
                || WaveActionRect.xMin < BoardRect.xMin || WaveActionRect.yMin < BoardRect.yMin
                || WaveActionRect.xMax > BoardRect.xMax || WaveActionRect.yMax > BoardRect.yMax)
            {
                reason = "battlefield wave action is outside the board or smaller than 44 logical points";
                return false;
            }

            foreach (var cell in _map.PlantableCells)
            {
                if (!WaveActionRect.Overlaps(CellRect(cell))) continue;
                reason = "battlefield wave action overlaps planting target: " + cell;
                return false;
            }

            if (WaveActionRect.Overlaps(LegacyVisualRect(_map.Core, 172f, 140f)))
            {
                reason = "battlefield wave action overlaps the core";
                return false;
            }

            var routeHalfWidth = MapDistanceToScreen(GameConfig.MapDistance(8f)) * .5f;
            for (var index = 1; index < RoutePoints.Count; index++)
            {
                var from = RoutePoints[index - 1];
                var to = RoutePoints[index];
                var routeBounds = Rect.MinMaxRect(
                    Mathf.Min(from.x, to.x) - routeHalfWidth,
                    Mathf.Min(from.y, to.y) - routeHalfWidth,
                    Mathf.Max(from.x, to.x) + routeHalfWidth,
                    Mathf.Max(from.y, to.y) + routeHalfWidth);
                if (!WaveActionRect.Overlaps(routeBounds)) continue;
                reason = "battlefield wave action overlaps route segment " + (index - 1);
                return false;
            }

            reason = "ok";
            return true;
        }

        private static Rect CenteredRect(Vector2 center, float width, float height)
        {
            return new Rect(center.x - width * .5f, center.y - height * .5f, width, height);
        }
    }
}
