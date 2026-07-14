using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace FruitDefense.Core
{
    public sealed class InitialPotGroup
    {
        public string Name { get; private set; }
        public int InitialCount { get; private set; }
        public IReadOnlyList<Vector2Int> Cells { get; private set; }

        public InitialPotGroup(string name, int initialCount, IEnumerable<Vector2Int> cells)
        {
            Name = name;
            InitialCount = initialCount;
            Cells = Array.AsReadOnly(cells.ToArray());
        }
    }

    public sealed class BattlefieldMapDefinition
    {
        public const float LegacyRouteLength = 228f;

        private readonly HashSet<Vector2Int> _plantableLookup;

        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float LegacyToMapScale { get; private set; }
        public IReadOnlyList<Vector2Int> PlantableCells { get; private set; }
        public IReadOnlyList<Vector2> RouteNodes { get; private set; }
        public Vector2 Entry { get; private set; }
        public Vector2 Exit { get; private set; }
        public Vector2 Core { get; private set; }
        public IReadOnlyDictionary<string, InitialPotGroup> InitialPotGroups { get; private set; }
        public IReadOnlyList<string> InitialPotGroupOrder { get; private set; }
        public Rect MapBounds { get; private set; }
        public BattlefieldRouteMetrics Route { get; private set; }
        public BattlefieldTopology Topology { get; private set; }

        public BattlefieldMapDefinition(
            int gridWidth,
            int gridHeight,
            float legacyToMapScale,
            IEnumerable<Vector2Int> plantableCells,
            IEnumerable<Vector2> routeNodes,
            Vector2 core,
            IEnumerable<InitialPotGroup> initialPotGroups)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            LegacyToMapScale = legacyToMapScale;
            var cells = plantableCells.ToArray();
            var nodes = routeNodes.ToArray();
            var groups = initialPotGroups.ToArray();
            PlantableCells = Array.AsReadOnly(cells);
            RouteNodes = Array.AsReadOnly(nodes);
            Entry = nodes.Length > 0 ? nodes[0] : Vector2.zero;
            Exit = nodes.Length > 0 ? nodes[nodes.Length - 1] : Vector2.zero;
            Core = core;
            _plantableLookup = new HashSet<Vector2Int>(cells);
            InitialPotGroups = new ReadOnlyDictionary<string, InitialPotGroup>(
                groups.ToDictionary(group => group.Name, StringComparer.Ordinal));
            InitialPotGroupOrder = Array.AsReadOnly(groups.Select(group => group.Name).ToArray());
            Route = new BattlefieldRouteMetrics(RouteNodes);
            Topology = new BattlefieldTopology(this);
            MapBounds = CalculateBounds(cells, nodes, core);
        }

        public static BattlefieldMapDefinition CreateDefault()
        {
            var cells = new List<Vector2Int>(48);
            for (var row = 0; row < 6; row++)
                for (var column = 0; column < 8; column++)
                    cells.Add(new Vector2Int(column, row));

            var north = cells.Where(cell => cell.y == 0).ToArray();
            var east = cells.Where(cell => cell.x == 7 && cell.y > 0 && cell.y < 5).ToArray();
            var south = cells.Where(cell => cell.y == 5).ToArray();
            var route = new[]
            {
                new Vector2(-.5f, -1f),
                new Vector2(7.5f, -1f),
                new Vector2(7.5f, 6f),
                new Vector2(-.5f, 6f),
            };
            const float routeLength = 23f;
            return new BattlefieldMapDefinition(
                8,
                6,
                routeLength / LegacyRouteLength,
                cells,
                route,
                new Vector2(3.5f, 2.5f),
                new[]
                {
                    new InitialPotGroup("north-roadside", 3, north),
                    new InitialPotGroup("east-roadside", 2, east),
                    new InitialPotGroup("south-roadside", 3, south),
                });
        }

        public bool IsPlantable(Vector2Int cell)
        {
            return _plantableLookup.Contains(cell);
        }

        public Vector2 CellToMap(Vector2Int cell)
        {
            return new Vector2(cell.x, cell.y);
        }

        public float FromLegacyDistance(float legacyDistance)
        {
            return legacyDistance * LegacyToMapScale;
        }

        public float ToLegacyDistance(float mapDistance)
        {
            return LegacyToMapScale <= 0f ? 0f : mapDistance / LegacyToMapScale;
        }

        public bool Validate(out string reason)
        {
            return Topology.Validate(out reason);
        }

        private static Rect CalculateBounds(IEnumerable<Vector2Int> cells, IEnumerable<Vector2> nodes, Vector2 core)
        {
            var points = nodes.Concat(cells.Select(cell => new Vector2(cell.x, cell.y))).Concat(new[] { core }).ToArray();
            if (points.Length == 0) return new Rect();
            var minX = points.Min(point => point.x);
            var maxX = points.Max(point => point.x);
            var minY = points.Min(point => point.y);
            var maxY = points.Max(point => point.y);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }

    public sealed class BattlefieldTopology
    {
        private static readonly Vector2Int[] CardinalOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
        };

        private readonly BattlefieldMapDefinition _map;

        public BattlefieldTopology(BattlefieldMapDefinition map)
        {
            _map = map;
        }

        public IEnumerable<Vector2Int> CardinalNeighbors(Vector2Int cell)
        {
            foreach (var offset in CardinalOffsets)
            {
                var neighbor = cell + offset;
                if (_map.IsPlantable(neighbor)) yield return neighbor;
            }
        }

        public bool AreCardinalNeighbors(Vector2Int first, Vector2Int second)
        {
            return _map.IsPlantable(first)
                && _map.IsPlantable(second)
                && Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        public bool Validate(out string reason)
        {
            if (_map.GridWidth <= 0 || _map.GridHeight <= 0)
            {
                reason = "grid dimensions must be positive";
                return false;
            }
            if (_map.PlantableCells.Count != _map.PlantableCells.Distinct().Count())
            {
                reason = "plantable cells must be unique";
                return false;
            }
            foreach (var cell in _map.PlantableCells)
            {
                if (cell.x < 0 || cell.x >= _map.GridWidth || cell.y < 0 || cell.y >= _map.GridHeight)
                {
                    reason = "plantable cell is outside grid bounds: " + cell;
                    return false;
                }
            }
            if (_map.RouteNodes.Count < 2)
            {
                reason = "route requires at least two nodes";
                return false;
            }
            for (var index = 1; index < _map.RouteNodes.Count; index++)
            {
                if (Vector2.Distance(_map.RouteNodes[index - 1], _map.RouteNodes[index]) <= .0001f)
                {
                    reason = "route contains a zero-length segment at index " + (index - 1);
                    return false;
                }
            }
            if (_map.Entry != _map.RouteNodes[0] || _map.Exit != _map.RouteNodes[_map.RouteNodes.Count - 1])
            {
                reason = "entry and exit must match the route endpoints";
                return false;
            }
            var groupedCells = new HashSet<Vector2Int>();
            var initialCount = 0;
            foreach (var name in _map.InitialPotGroupOrder)
            {
                InitialPotGroup group;
                if (!_map.InitialPotGroups.TryGetValue(name, out group) || string.IsNullOrEmpty(group.Name))
                {
                    reason = "initial-pot semantic group is missing: " + name;
                    return false;
                }
                if (group.InitialCount < 0 || group.InitialCount > group.Cells.Count)
                {
                    reason = "initial-pot group count is invalid: " + group.Name;
                    return false;
                }
                foreach (var cell in group.Cells)
                {
                    if (!_map.IsPlantable(cell))
                    {
                        reason = "initial-pot group contains a non-plantable cell: " + group.Name + " " + cell;
                        return false;
                    }
                    if (!groupedCells.Add(cell))
                    {
                        reason = "initial-pot groups contain a duplicate cell: " + cell;
                        return false;
                    }
                }
                initialCount += group.InitialCount;
            }
            if (initialCount <= 0)
            {
                reason = "initial-pot groups must place at least one flowerpot";
                return false;
            }
            foreach (var cell in _map.PlantableCells)
            {
                foreach (var neighbor in CardinalNeighbors(cell))
                {
                    if (!AreCardinalNeighbors(cell, neighbor))
                    {
                        reason = "cardinal neighbor relation is inconsistent: " + cell + " -> " + neighbor;
                        return false;
                    }
                }
            }
            reason = "ok";
            return true;
        }
    }

    public sealed class BattlefieldRouteMetrics
    {
        private readonly IReadOnlyList<Vector2> _nodes;
        private readonly float[] _cumulativeLengths;

        public float TotalLength { get; private set; }
        public IReadOnlyList<float> CumulativeLengths { get; private set; }

        public BattlefieldRouteMetrics(IReadOnlyList<Vector2> nodes)
        {
            _nodes = nodes;
            _cumulativeLengths = new float[nodes.Count];
            for (var index = 1; index < nodes.Count; index++)
                _cumulativeLengths[index] = _cumulativeLengths[index - 1]
                    + Vector2.Distance(nodes[index - 1], nodes[index]);
            TotalLength = _cumulativeLengths.Length == 0 ? 0f : _cumulativeLengths[_cumulativeLengths.Length - 1];
            CumulativeLengths = Array.AsReadOnly(_cumulativeLengths);
        }

        public Vector2 Sample(float progress)
        {
            if (_nodes.Count == 0) return Vector2.zero;
            if (_nodes.Count == 1 || progress <= 0f) return _nodes[0];
            if (progress >= TotalLength) return _nodes[_nodes.Count - 1];
            var value = Mathf.Clamp(progress, 0f, TotalLength);
            for (var index = 1; index < _nodes.Count; index++)
            {
                if (value > _cumulativeLengths[index]) continue;
                var fromLength = _cumulativeLengths[index - 1];
                var segmentLength = _cumulativeLengths[index] - fromLength;
                var ratio = segmentLength <= .0001f ? 0f : (value - fromLength) / segmentLength;
                return Vector2.Lerp(_nodes[index - 1], _nodes[index], ratio);
            }
            return _nodes[_nodes.Count - 1];
        }
    }
}
