using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace FruitDefense.Core
{
    public enum BattlefieldDirection
    {
        None,
        North,
        East,
        South,
        West,
    }

    [Flags]
    public enum BattlefieldRouteConnections
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8,
    }

    public enum BattlefieldRouteTileKind
    {
        Entry,
        Exit,
        Horizontal,
        Vertical,
        CornerNorthEast,
        CornerNorthWest,
        CornerSouthEast,
        CornerSouthWest,
    }

    public readonly struct BattlefieldRouteTileDescriptor
    {
        public Vector2Int Cell { get; }
        public BattlefieldRouteTileKind Kind { get; }
        public BattlefieldDirection PreviousConnection { get; }
        public BattlefieldDirection NextConnection { get; }
        public BattlefieldDirection Orientation
        {
            get { return Kind == BattlefieldRouteTileKind.Exit ? PreviousConnection : NextConnection; }
        }
        public BattlefieldRouteConnections Connections { get; }

        public BattlefieldRouteTileDescriptor(Vector2Int cell, BattlefieldRouteTileKind kind,
            BattlefieldDirection previousConnection, BattlefieldDirection nextConnection,
            BattlefieldRouteConnections connections)
        {
            Cell = cell;
            Kind = kind;
            PreviousConnection = previousConnection;
            NextConnection = nextConnection;
            Connections = connections;
        }
    }

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
        public const string DefaultMapId = "orchard-01";
        public const float DefaultRouteLength = 23f;
        public const int DefaultRouteSegmentCount = 19;

        private HashSet<Vector2Int> _plantableLookup;
        private HashSet<Vector2Int> _routeCellLookup;
        private Dictionary<Vector2Int, BattlefieldRouteTileDescriptor> _routeTileLookup;
        private CompiledBattlefieldMap _layeredMap;

        public string MapId { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float MapUnitsPerCell { get; private set; }
        public float LegacyToMapScale { get; private set; }
        public bool UsesLayeredMap { get { return _layeredMap != null; } }
        public CompiledBattlefieldMap LayeredMap { get { return _layeredMap; } }
        public string PrimaryRouteId { get { return _layeredMap == null ? string.Empty : _layeredMap.PrimaryRouteId; } }
        public string GameplayFingerprint { get { return _layeredMap == null ? string.Empty : _layeredMap.GameplayFingerprint; } }
        public IReadOnlyList<string> VisualSurfaceIds { get; private set; }
        public IReadOnlyList<BattlefieldGameplayCell> GameplayCells { get; private set; }
        public IReadOnlyList<BattlefieldRouteDefinition> Routes { get; private set; }
        public IReadOnlyList<BattlefieldMarkerGroupDefinition> MarkerGroups { get; private set; }
        public IReadOnlyList<BattlefieldMarkerDefinition> Markers { get; private set; }
        public Vector2Int EnemySpawnCell { get; private set; }
        public Vector2Int RouteGoalCell { get; private set; }
        public IReadOnlyList<Vector2Int> PlantableCells { get; private set; }
        public IReadOnlyList<Vector2Int> RouteCells { get; private set; }
        public Vector2Int EntryCell { get; private set; }
        public Vector2Int ExitCell { get; private set; }
        public Vector2Int CoreCell { get; private set; }
        public IReadOnlyList<BattlefieldRouteTileDescriptor> RouteTileDescriptors { get; private set; }
        public IReadOnlyDictionary<Vector2Int, BattlefieldRouteTileDescriptor> RouteTileLookup { get; private set; }
        public IReadOnlyList<Vector2> RouteNodes { get; private set; }
        public Vector2 Entry { get; private set; }
        public Vector2 Exit { get; private set; }
        public Vector2 Core { get; private set; }
        public IReadOnlyDictionary<string, InitialPotGroup> InitialPotGroups { get; private set; }
        public IReadOnlyList<string> InitialPotGroupOrder { get; private set; }
        public Rect MapBounds { get; private set; }
        public BattlefieldRouteMetrics Route { get; private set; }
        public BattlefieldTopology Topology { get; private set; }

        public BattlefieldMapDefinition(BattlefieldLayeredMapSource source)
            : this(BattlefieldLayeredMapCompiler.CompileOrThrow(source))
        {
        }

        public BattlefieldMapDefinition(CompiledBattlefieldMap layeredMap)
        {
            _layeredMap = layeredMap ?? throw new ArgumentNullException(nameof(layeredMap));
            MapId = layeredMap.MapId;
            GridWidth = layeredMap.GridWidth;
            GridHeight = layeredMap.GridHeight;
            MapUnitsPerCell = layeredMap.MapUnitsPerCell;
            VisualSurfaceIds = layeredMap.VisualSurfaceIds;
            GameplayCells = layeredMap.GameplayCells;
            Routes = layeredMap.RoutesInSourceOrder;
            MarkerGroups = layeredMap.MarkerGroupsInSourceOrder;
            Markers = layeredMap.MarkersInSourceOrder;

            var orderedRouteCells = layeredMap.PrimaryRoute.Cells.ToArray();
            RouteCells = Array.AsReadOnly(orderedRouteCells);
            _routeCellLookup = new HashSet<Vector2Int>(orderedRouteCells);
            EnemySpawnCell = layeredMap.MarkersInSourceOrder
                .Single(marker => marker.Kind == BattlefieldMarkerKind.EnemySpawn).Cell;
            RouteGoalCell = layeredMap.MarkersInSourceOrder
                .Single(marker => marker.Kind == BattlefieldMarkerKind.RouteGoal).Cell;
            EntryCell = EnemySpawnCell;
            ExitCell = RouteGoalCell;
            CoreCell = layeredMap.MarkersInSourceOrder.Single(marker => marker.Kind == BattlefieldMarkerKind.Core).Cell;

            var plantableCells = Enumerable.Range(0, layeredMap.GameplayCells.Count)
                .Where(index => layeredMap.GameplayCells[index].Has(BattlefieldCellCapabilities.Plantable))
                .Select(index => new Vector2Int(index % GridWidth, index / GridWidth))
                .ToArray();
            PlantableCells = Array.AsReadOnly(plantableCells);
            _plantableLookup = new HashSet<Vector2Int>(plantableCells);

            Entry = CellToMap(EntryCell);
            Exit = CellToMap(ExitCell);
            Core = CellToMap(CoreCell);
            var routeNodes = orderedRouteCells.Select(CellToMap).ToArray();
            RouteNodes = Array.AsReadOnly(routeNodes);
            Route = new BattlefieldRouteMetrics(RouteNodes);
            LegacyToMapScale = Route.TotalLength / LegacyRouteLength;

            SetInitialPotGroupsFromMarkers(layeredMap);
            Topology = new BattlefieldTopology(this);
            var descriptors = BuildRouteTileDescriptors();
            RouteTileDescriptors = Array.AsReadOnly(descriptors);
            _routeTileLookup = new Dictionary<Vector2Int, BattlefieldRouteTileDescriptor>();
            foreach (var descriptor in descriptors)
                if (!_routeTileLookup.ContainsKey(descriptor.Cell)) _routeTileLookup.Add(descriptor.Cell, descriptor);
            RouteTileLookup = new ReadOnlyDictionary<Vector2Int, BattlefieldRouteTileDescriptor>(_routeTileLookup);
            MapBounds = CalculateGridBounds(GridWidth, GridHeight, MapUnitsPerCell);
        }

        // Compatibility constructor retained for existing custom-map and validation callers.
        public BattlefieldMapDefinition(
            int gridWidth,
            int gridHeight,
            float legacyToMapScale,
            IEnumerable<Vector2Int> plantableCells,
            IEnumerable<Vector2> routeNodes,
            Vector2 core,
            IEnumerable<InitialPotGroup> initialPotGroups)
        {
            _layeredMap = null;
            MapId = string.Empty;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            MapUnitsPerCell = 1f;
            LegacyToMapScale = legacyToMapScale;
            var cells = plantableCells == null ? Array.Empty<Vector2Int>() : plantableCells.ToArray();
            var nodes = routeNodes == null ? Array.Empty<Vector2>() : routeNodes.ToArray();
            PlantableCells = Array.AsReadOnly(cells);
            _plantableLookup = new HashSet<Vector2Int>(cells);
            RouteCells = Array.AsReadOnly(Array.Empty<Vector2Int>());
            _routeCellLookup = new HashSet<Vector2Int>();
            EntryCell = nodes.Length == 0 ? Vector2Int.zero : Vector2Int.RoundToInt(nodes[0]);
            ExitCell = nodes.Length == 0 ? Vector2Int.zero : Vector2Int.RoundToInt(nodes[nodes.Length - 1]);
            EnemySpawnCell = EntryCell;
            RouteGoalCell = ExitCell;
            CoreCell = Vector2Int.RoundToInt(core);
            RouteNodes = Array.AsReadOnly(nodes);
            Entry = nodes.Length > 0 ? nodes[0] : Vector2.zero;
            Exit = nodes.Length > 0 ? nodes[nodes.Length - 1] : Vector2.zero;
            Core = core;
            SetInitialPotGroups(initialPotGroups);
            SetLegacyLayerViews();
            Route = new BattlefieldRouteMetrics(RouteNodes);
            Topology = new BattlefieldTopology(this);
            RouteTileDescriptors = Array.AsReadOnly(Array.Empty<BattlefieldRouteTileDescriptor>());
            _routeTileLookup = new Dictionary<Vector2Int, BattlefieldRouteTileDescriptor>();
            RouteTileLookup = new ReadOnlyDictionary<Vector2Int, BattlefieldRouteTileDescriptor>(_routeTileLookup);
            MapBounds = CalculateBounds(cells, nodes, core);
        }

        public static BattlefieldMapDefinition CreateDefault()
        {
            const int width = 8;
            const int height = 7;
            var route = new List<Vector2Int>(20);
            for (var column = 0; column < width; column++) route.Add(new Vector2Int(column, 0));
            for (var row = 1; row < height; row++) route.Add(new Vector2Int(width - 1, row));
            for (var column = width - 2; column >= 1; column--) route.Add(new Vector2Int(column, height - 1));
            var initialGroups = new[]
            {
                new InitialPotGroup("north-roadside", 3, new[]
                {
                    new Vector2Int(1, 1), new Vector2Int(4, 1), new Vector2Int(6, 1),
                }),
                new InitialPotGroup("east-roadside", 2, new[]
                {
                    new Vector2Int(6, 2), new Vector2Int(6, 4),
                }),
                new InitialPotGroup("south-roadside", 3, new[]
                {
                    new Vector2Int(1, 5), new Vector2Int(4, 5), new Vector2Int(6, 5),
                }),
            };
            return new BattlefieldMapDefinition(BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                DefaultMapId, width, height, DefaultRouteLength / DefaultRouteSegmentCount,
                route, new Vector2Int(0, 6), initialGroups));
        }

        public bool IsPlantable(Vector2Int cell)
        {
            return _plantableLookup.Contains(cell);
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
        }

        public bool IsRoute(Vector2Int cell)
        {
            return _routeCellLookup.Contains(cell);
        }

        public bool TryGetRouteTile(Vector2Int cell, out BattlefieldRouteTileDescriptor descriptor)
        {
            return _routeTileLookup.TryGetValue(cell, out descriptor);
        }

        public string SurfaceAt(Vector2Int cell)
        {
            return _layeredMap == null ? string.Empty : _layeredMap.SurfaceAt(cell);
        }

        public BattlefieldGameplayCell GameplayCellAt(Vector2Int cell)
        {
            return _layeredMap == null ? default(BattlefieldGameplayCell) : _layeredMap.GameplayCellAt(cell);
        }

        public bool HasCapability(Vector2Int cell, BattlefieldCellCapabilities capability)
        {
            return GameplayCellAt(cell).Has(capability);
        }

        public bool Blocks(Vector2Int cell, BattlefieldCollisionChannels channel)
        {
            return GameplayCellAt(cell).Blocks(channel);
        }

        public Vector2 CellToMap(Vector2Int cell)
        {
            return new Vector2(cell.x * MapUnitsPerCell, cell.y * MapUnitsPerCell);
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

        private BattlefieldRouteTileDescriptor[] BuildRouteTileDescriptors()
        {
            var descriptors = new List<BattlefieldRouteTileDescriptor>(RouteCells.Count);
            for (var index = 0; index < RouteCells.Count; index++)
            {
                BattlefieldRouteTileDescriptor descriptor;
                string reason;
                if (Topology.TryDescribeRouteCell(index, out descriptor, out reason)) descriptors.Add(descriptor);
            }
            return descriptors.ToArray();
        }

        private void SetInitialPotGroups(IEnumerable<InitialPotGroup> initialPotGroups)
        {
            var groups = initialPotGroups == null ? Array.Empty<InitialPotGroup>() : initialPotGroups.ToArray();
            var lookup = new Dictionary<string, InitialPotGroup>(StringComparer.Ordinal);
            foreach (var group in groups)
            {
                if (group == null || lookup.ContainsKey(group.Name ?? string.Empty)) continue;
                lookup[group.Name ?? string.Empty] = group;
            }
            InitialPotGroups = new ReadOnlyDictionary<string, InitialPotGroup>(lookup);
            InitialPotGroupOrder = Array.AsReadOnly(groups
                .Where(group => group != null)
                .Select(group => group.Name ?? string.Empty)
                .ToArray());
        }

        private void SetInitialPotGroupsFromMarkers(CompiledBattlefieldMap layeredMap)
        {
            var groups = new List<InitialPotGroup>();
            foreach (var group in layeredMap.MarkerGroupsInSourceOrder)
            {
                if (group.MarkerKind != BattlefieldMarkerKind.InitialPotCandidate) continue;
                var cells = layeredMap.MarkersInSourceOrder
                    .Where(marker => marker.Kind == BattlefieldMarkerKind.InitialPotCandidate
                        && string.Equals(marker.GroupId, group.GroupId, StringComparison.Ordinal))
                    .Select(marker => marker.Cell)
                    .ToArray();
                groups.Add(new InitialPotGroup(group.GroupId, group.SelectionCount, cells));
            }
            SetInitialPotGroups(groups);
        }

        private void SetLegacyLayerViews()
        {
            VisualSurfaceIds = Array.AsReadOnly(Array.Empty<string>());
            GameplayCells = Array.AsReadOnly(Array.Empty<BattlefieldGameplayCell>());
            Routes = Array.AsReadOnly(Array.Empty<BattlefieldRouteDefinition>());
            MarkerGroups = Array.AsReadOnly(Array.Empty<BattlefieldMarkerGroupDefinition>());
            Markers = Array.AsReadOnly(Array.Empty<BattlefieldMarkerDefinition>());
        }

        private static Rect CalculateGridBounds(int width, int height, float mapUnitsPerCell)
        {
            if (width <= 0 || height <= 0) return new Rect();
            return Rect.MinMaxRect(0f, 0f,
                Math.Max(0, width - 1) * mapUnitsPerCell,
                Math.Max(0, height - 1) * mapUnitsPerCell);
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
                && AreCoordinatesCardinalNeighbors(first, second);
        }

        public static bool AreCoordinatesCardinalNeighbors(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        public bool TryDescribeRouteCell(int routeIndex,
            out BattlefieldRouteTileDescriptor descriptor, out string reason)
        {
            descriptor = default(BattlefieldRouteTileDescriptor);
            if (routeIndex < 0 || routeIndex >= _map.RouteCells.Count)
            {
                reason = "route index is outside the ordered route: " + routeIndex;
                return false;
            }

            var cell = _map.RouteCells[routeIndex];
            BattlefieldDirection previous = BattlefieldDirection.None;
            BattlefieldDirection next = BattlefieldDirection.None;
            if (routeIndex > 0 && !TryDirection(cell, _map.RouteCells[routeIndex - 1], out previous))
            {
                reason = "route index " + routeIndex + " has an invalid previous connection at cell " + cell;
                return false;
            }
            if (routeIndex + 1 < _map.RouteCells.Count
                && !TryDirection(cell, _map.RouteCells[routeIndex + 1], out next))
            {
                reason = "route index " + routeIndex + " has an invalid next connection at cell " + cell;
                return false;
            }

            BattlefieldRouteTileKind kind;
            if (routeIndex == 0)
            {
                if (next == BattlefieldDirection.None)
                {
                    reason = "entry route index 0 requires one cardinal connection at cell " + cell;
                    return false;
                }
                kind = BattlefieldRouteTileKind.Entry;
            }
            else if (routeIndex == _map.RouteCells.Count - 1)
            {
                if (previous == BattlefieldDirection.None)
                {
                    reason = "exit route index " + routeIndex + " requires one cardinal connection at cell " + cell;
                    return false;
                }
                kind = BattlefieldRouteTileKind.Exit;
            }
            else if (previous == next || previous == BattlefieldDirection.None || next == BattlefieldDirection.None)
            {
                reason = "route index " + routeIndex + " has an impossible connection pair at cell " + cell;
                return false;
            }
            else
            {
                var pair = ToConnections(previous) | ToConnections(next);
                switch (pair)
                {
                    case BattlefieldRouteConnections.East | BattlefieldRouteConnections.West:
                        kind = BattlefieldRouteTileKind.Horizontal;
                        break;
                    case BattlefieldRouteConnections.North | BattlefieldRouteConnections.South:
                        kind = BattlefieldRouteTileKind.Vertical;
                        break;
                    case BattlefieldRouteConnections.North | BattlefieldRouteConnections.East:
                        kind = BattlefieldRouteTileKind.CornerNorthEast;
                        break;
                    case BattlefieldRouteConnections.North | BattlefieldRouteConnections.West:
                        kind = BattlefieldRouteTileKind.CornerNorthWest;
                        break;
                    case BattlefieldRouteConnections.South | BattlefieldRouteConnections.East:
                        kind = BattlefieldRouteTileKind.CornerSouthEast;
                        break;
                    case BattlefieldRouteConnections.South | BattlefieldRouteConnections.West:
                        kind = BattlefieldRouteTileKind.CornerSouthWest;
                        break;
                    default:
                        reason = "route index " + routeIndex + " has an inconsistent connection pair at cell " + cell;
                        return false;
                }
            }

            descriptor = new BattlefieldRouteTileDescriptor(cell, kind, previous, next,
                ToConnections(previous) | ToConnections(next));
            reason = "ok";
            return true;
        }

        public bool Validate(out string reason)
        {
            if (_map.UsesLayeredMap) return ValidateLayered(out reason);
            return ValidateLegacy(out reason);
        }

        private bool ValidateLayered(out string reason)
        {
            var expectedCells = _map.GridWidth * _map.GridHeight;
            if (_map.VisualSurfaceIds.Count != expectedCells || _map.GameplayCells.Count != expectedCells)
            {
                reason = "compiled layered map coverage is incomplete";
                return false;
            }
            if (_map.RouteCells.Count < 2 || _map.EnemySpawnCell != _map.RouteCells[0]
                || _map.RouteGoalCell != _map.RouteCells[_map.RouteCells.Count - 1])
            {
                reason = "compiled route markers do not match the primary route endpoints";
                return false;
            }
            for (var index = 0; index < _map.RouteCells.Count; index++)
            {
                var cell = _map.RouteCells[index];
                if (!_map.HasCapability(cell, BattlefieldCellCapabilities.EnemyTraversable))
                {
                    reason = "compiled route cell lacks enemy traversal capability: " + cell;
                    return false;
                }
                if (index > 0 && !AreCoordinatesCardinalNeighbors(_map.RouteCells[index - 1], cell))
                {
                    reason = "compiled route is disconnected at index " + index;
                    return false;
                }
                BattlefieldRouteTileDescriptor descriptor;
                if (!TryDescribeRouteCell(index, out descriptor, out reason)) return false;
            }
            if (!AreCoordinatesCardinalNeighbors(_map.RouteGoalCell, _map.CoreCell))
            {
                reason = "route goal is not cardinally adjacent to the core marker";
                return false;
            }
            return ValidateInitialPotGroups(out reason);
        }

        private bool ValidateLegacy(out string reason)
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
                if (!_map.IsInBounds(cell))
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
            return ValidateInitialPotGroups(out reason);
        }

        private bool ValidateInitialPotGroups(out string reason)
        {
            var groupedCells = new HashSet<Vector2Int>();
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            var initialCount = 0;
            foreach (var name in _map.InitialPotGroupOrder)
            {
                if (!seenNames.Add(name))
                {
                    reason = "initial-pot semantic group name is duplicated: " + name;
                    return false;
                }
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
                for (var cellIndex = 0; cellIndex < group.Cells.Count; cellIndex++)
                {
                    var cell = group.Cells[cellIndex];
                    if (!_map.IsPlantable(cell))
                    {
                        reason = "initial-pot group " + group.Name + " cell index " + cellIndex
                            + " contains a non-plantable cell: " + cell;
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
            reason = "ok";
            return true;
        }

        private static bool TryDirection(Vector2Int from, Vector2Int to, out BattlefieldDirection direction)
        {
            var delta = to - from;
            // Grid rows follow rendered screen space: cell.y increases downward.
            if (delta == Vector2Int.up) direction = BattlefieldDirection.South;
            else if (delta == Vector2Int.right) direction = BattlefieldDirection.East;
            else if (delta == Vector2Int.down) direction = BattlefieldDirection.North;
            else if (delta == Vector2Int.left) direction = BattlefieldDirection.West;
            else
            {
                direction = BattlefieldDirection.None;
                return false;
            }
            return true;
        }

        private static BattlefieldRouteConnections ToConnections(BattlefieldDirection direction)
        {
            switch (direction)
            {
                case BattlefieldDirection.North: return BattlefieldRouteConnections.North;
                case BattlefieldDirection.East: return BattlefieldRouteConnections.East;
                case BattlefieldDirection.South: return BattlefieldRouteConnections.South;
                case BattlefieldDirection.West: return BattlefieldRouteConnections.West;
                default: return BattlefieldRouteConnections.None;
            }
        }
    }

    public sealed class BattlefieldRouteMetrics
    {
        private readonly IReadOnlyList<Vector2> _nodes;
        private readonly float[] _cumulativeLengths;

        public IReadOnlyList<Vector2> Nodes { get { return _nodes; } }
        public int SegmentCount { get { return Math.Max(0, _nodes.Count - 1); } }
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
                if (value == _cumulativeLengths[index]) return _nodes[index];
                var fromLength = _cumulativeLengths[index - 1];
                var segmentLength = _cumulativeLengths[index] - fromLength;
                var ratio = segmentLength <= .0001f ? 0f : (value - fromLength) / segmentLength;
                return Vector2.Lerp(_nodes[index - 1], _nodes[index], ratio);
            }
            return _nodes[_nodes.Count - 1];
        }
    }
}
