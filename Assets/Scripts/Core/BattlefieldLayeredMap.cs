using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FruitDefense.Core
{
    public static class BattlefieldLayerIds
    {
        public const int SchemaVersion = 3;
        public const string PrimaryRoute = "route.main";

        public static class Surfaces
        {
            public const string Soil = "surface.soil";
            public const string Grass = "surface.grass";
            public const string StoneRoad = "surface.stone-road";
            public const string Water = "surface.water";
        }

        public static class EdgeStyles
        {
            public const string Refined = "edge.refined";
        }

        public static class ContourStyles
        {
            public const string Square = "contour.square";
            public const string Organic = "contour.organic";
        }

        public static class Capabilities
        {
            public const string Plantable = "capability.plantable";
            public const string EnemyTraversable = "capability.enemy-traversable";
            public const string PlayerTraversable = "capability.player-traversable";
            public const string ItemSpawnCompatible = "capability.item-spawn-compatible";
        }

        public static class Collisions
        {
            public const string BlocksGround = "collision.blocks-ground";
            public const string BlocksProjectile = "collision.blocks-projectile";
            public const string BlocksPlacement = "collision.blocks-placement";
        }
    }

    [Flags]
    public enum BattlefieldCellCapabilities
    {
        None = 0,
        Plantable = 1,
        EnemyTraversable = 2,
        PlayerTraversable = 4,
        ItemSpawnCompatible = 8,
    }

    [Flags]
    public enum BattlefieldCollisionChannels
    {
        None = 0,
        BlocksGround = 1,
        BlocksProjectile = 2,
        BlocksPlacement = 4,
    }

    public enum BattlefieldMarkerKind
    {
        EnemySpawn,
        RouteGoal,
        Core,
        InitialPotCandidate,
        PlayerSpawn,
        ItemSpawn,
        Trigger,
    }

    public sealed class BattlefieldGameplayCellSource
    {
        public IReadOnlyList<string> CapabilityIds { get; private set; }
        public IReadOnlyList<string> CollisionIds { get; private set; }

        public BattlefieldGameplayCellSource(IEnumerable<string> capabilityIds = null,
            IEnumerable<string> collisionIds = null)
        {
            CapabilityIds = Array.AsReadOnly((capabilityIds ?? Enumerable.Empty<string>()).ToArray());
            CollisionIds = Array.AsReadOnly((collisionIds ?? Enumerable.Empty<string>()).ToArray());
        }
    }

    public sealed class BattlefieldVisualCellSource
    {
        public string BaseSurfaceId { get; private set; }
        public string LandformSurfaceId { get; private set; }
        public string ContourStyleId { get; private set; }
        public string EdgeStyleId { get; private set; }

        // Legacy construction keeps the original organic silhouette. Canonical authored maps
        // use the explicit four-identity overload below.
        public BattlefieldVisualCellSource(string baseSurfaceId, string landformSurfaceId = null,
            string edgeStyleId = null)
            : this(baseSurfaceId, landformSurfaceId,
                string.IsNullOrEmpty(landformSurfaceId)
                    ? string.Empty : BattlefieldLayerIds.ContourStyles.Organic,
                edgeStyleId)
        {
        }

        public BattlefieldVisualCellSource(string baseSurfaceId, string landformSurfaceId,
            string contourStyleId, string edgeStyleId)
        {
            BaseSurfaceId = baseSurfaceId ?? string.Empty;
            LandformSurfaceId = landformSurfaceId ?? string.Empty;
            ContourStyleId = contourStyleId ?? string.Empty;
            EdgeStyleId = edgeStyleId ?? string.Empty;
        }

        public string EffectiveSurfaceId
        {
            get { return string.IsNullOrEmpty(LandformSurfaceId) ? BaseSurfaceId : LandformSurfaceId; }
        }
    }

    public readonly struct BattlefieldGameplayCell
    {
        public BattlefieldCellCapabilities Capabilities { get; }
        public BattlefieldCollisionChannels CollisionChannels { get; }

        public BattlefieldGameplayCell(BattlefieldCellCapabilities capabilities,
            BattlefieldCollisionChannels collisionChannels)
        {
            Capabilities = capabilities;
            CollisionChannels = collisionChannels;
        }

        public bool Has(BattlefieldCellCapabilities capability)
        {
            return (Capabilities & capability) == capability;
        }

        public bool Blocks(BattlefieldCollisionChannels channel)
        {
            return (CollisionChannels & channel) == channel;
        }
    }

    public sealed class BattlefieldRouteDefinition
    {
        public string RouteId { get; private set; }
        public IReadOnlyList<Vector2Int> Cells { get; private set; }

        public BattlefieldRouteDefinition(string routeId, IEnumerable<Vector2Int> cells)
        {
            RouteId = routeId ?? string.Empty;
            Cells = Array.AsReadOnly((cells ?? Enumerable.Empty<Vector2Int>()).ToArray());
        }
    }

    public sealed class BattlefieldMarkerGroupDefinition
    {
        public string GroupId { get; private set; }
        public BattlefieldMarkerKind MarkerKind { get; private set; }
        public int SelectionCount { get; private set; }

        public BattlefieldMarkerGroupDefinition(string groupId, BattlefieldMarkerKind markerKind,
            int selectionCount)
        {
            GroupId = groupId ?? string.Empty;
            MarkerKind = markerKind;
            SelectionCount = selectionCount;
        }
    }

    public sealed class BattlefieldMarkerDefinition
    {
        public string MarkerId { get; private set; }
        public BattlefieldMarkerKind Kind { get; private set; }
        public Vector2Int Cell { get; private set; }
        public string RouteId { get; private set; }
        public string GroupId { get; private set; }
        public string ContentId { get; private set; }
        public BattlefieldDirection Facing { get; private set; }

        public BattlefieldMarkerDefinition(string markerId, BattlefieldMarkerKind kind,
            Vector2Int cell, string routeId = null, string groupId = null,
            string contentId = null, BattlefieldDirection facing = BattlefieldDirection.None)
        {
            MarkerId = markerId ?? string.Empty;
            Kind = kind;
            Cell = cell;
            RouteId = routeId ?? string.Empty;
            GroupId = groupId ?? string.Empty;
            ContentId = contentId ?? string.Empty;
            Facing = facing;
        }
    }

    public sealed class BattlefieldLayeredMapSource
    {
        public int SchemaVersion { get; private set; }
        public string MapId { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float MapUnitsPerCell { get; private set; }
        public string PrimaryRouteId { get; private set; }
        public IReadOnlyList<BattlefieldVisualCellSource> VisualCells { get; private set; }
        public IReadOnlyList<string> VisualSurfaceIds { get; private set; }
        public IReadOnlyList<BattlefieldGameplayCellSource> GameplayCells { get; private set; }
        public IReadOnlyList<BattlefieldRouteDefinition> Routes { get; private set; }
        public IReadOnlyList<BattlefieldMarkerGroupDefinition> MarkerGroups { get; private set; }
        public IReadOnlyList<BattlefieldMarkerDefinition> Markers { get; private set; }

        public BattlefieldLayeredMapSource(int schemaVersion, string mapId, int gridWidth,
            int gridHeight, float mapUnitsPerCell, string primaryRouteId,
            IEnumerable<BattlefieldVisualCellSource> visualCells,
            IEnumerable<BattlefieldGameplayCellSource> gameplayCells,
            IEnumerable<BattlefieldRouteDefinition> routes,
            IEnumerable<BattlefieldMarkerGroupDefinition> markerGroups,
            IEnumerable<BattlefieldMarkerDefinition> markers)
        {
            SchemaVersion = schemaVersion;
            MapId = mapId ?? string.Empty;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            MapUnitsPerCell = mapUnitsPerCell;
            PrimaryRouteId = primaryRouteId ?? string.Empty;
            var authoredVisualCells = (visualCells
                ?? Enumerable.Empty<BattlefieldVisualCellSource>()).ToArray();
            VisualCells = Array.AsReadOnly(authoredVisualCells);
            VisualSurfaceIds = Array.AsReadOnly(authoredVisualCells.Select(cell =>
                cell == null ? string.Empty : cell.EffectiveSurfaceId).ToArray());
            GameplayCells = Array.AsReadOnly((gameplayCells
                ?? Enumerable.Empty<BattlefieldGameplayCellSource>()).ToArray());
            Routes = Array.AsReadOnly((routes ?? Enumerable.Empty<BattlefieldRouteDefinition>()).ToArray());
            MarkerGroups = Array.AsReadOnly((markerGroups
                ?? Enumerable.Empty<BattlefieldMarkerGroupDefinition>()).ToArray());
            Markers = Array.AsReadOnly((markers ?? Enumerable.Empty<BattlefieldMarkerDefinition>()).ToArray());
        }

        public BattlefieldLayeredMapSource(int schemaVersion, string mapId, int gridWidth,
            int gridHeight, float mapUnitsPerCell, string primaryRouteId,
            IEnumerable<string> visualSurfaceIds,
            IEnumerable<BattlefieldGameplayCellSource> gameplayCells,
            IEnumerable<BattlefieldRouteDefinition> routes,
            IEnumerable<BattlefieldMarkerGroupDefinition> markerGroups,
            IEnumerable<BattlefieldMarkerDefinition> markers)
            : this(schemaVersion, mapId, gridWidth, gridHeight, mapUnitsPerCell, primaryRouteId,
                ToVisualCells(visualSurfaceIds), gameplayCells, routes, markerGroups, markers)
        {
        }

        private static IEnumerable<BattlefieldVisualCellSource> ToVisualCells(
            IEnumerable<string> visualSurfaceIds)
        {
            return (visualSurfaceIds ?? Enumerable.Empty<string>()).Select(surfaceId =>
                string.Equals(surfaceId, BattlefieldLayerIds.Surfaces.Soil, StringComparison.Ordinal)
                    ? new BattlefieldVisualCellSource(BattlefieldLayerIds.Surfaces.Soil)
                    : new BattlefieldVisualCellSource(BattlefieldLayerIds.Surfaces.Soil, surfaceId));
        }
    }

    public sealed class BattlefieldLayeredMapValidationIssue
    {
        public string Code { get; private set; }
        public string Field { get; private set; }
        public string Message { get; private set; }

        public BattlefieldLayeredMapValidationIssue(string code, string field, string message)
        {
            Code = code ?? string.Empty;
            Field = field ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            return Code + " [" + Field + "] " + Message;
        }
    }

    public sealed class BattlefieldLayeredMapValidationResult
    {
        private readonly List<BattlefieldLayeredMapValidationIssue> _issues =
            new List<BattlefieldLayeredMapValidationIssue>();
        private readonly ReadOnlyCollection<BattlefieldLayeredMapValidationIssue> _readOnlyIssues;

        public BattlefieldLayeredMapValidationResult()
        {
            _readOnlyIssues = _issues.AsReadOnly();
        }

        public bool IsValid { get { return _issues.Count == 0; } }
        public IReadOnlyList<BattlefieldLayeredMapValidationIssue> Issues { get { return _readOnlyIssues; } }

        internal void Add(string code, string field, string message)
        {
            _issues.Add(new BattlefieldLayeredMapValidationIssue(code, field, message));
        }
    }

    public sealed class CompiledBattlefieldMap
    {
        private readonly IReadOnlyDictionary<string, BattlefieldRouteDefinition> _routes;
        private readonly IReadOnlyDictionary<string, BattlefieldMarkerGroupDefinition> _markerGroups;
        private readonly IReadOnlyDictionary<string, BattlefieldMarkerDefinition> _markers;

        public string MapId { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }
        public float MapUnitsPerCell { get; private set; }
        public string PrimaryRouteId { get; private set; }
        public IReadOnlyList<BattlefieldVisualCellSource> VisualCells { get; private set; }
        public IReadOnlyList<string> VisualSurfaceIds { get; private set; }
        public IReadOnlyList<BattlefieldGameplayCell> GameplayCells { get; private set; }
        public IReadOnlyList<BattlefieldRouteDefinition> RoutesInSourceOrder { get; private set; }
        public IReadOnlyDictionary<string, BattlefieldRouteDefinition> Routes { get { return _routes; } }
        public IReadOnlyList<BattlefieldMarkerGroupDefinition> MarkerGroupsInSourceOrder { get; private set; }
        public IReadOnlyDictionary<string, BattlefieldMarkerGroupDefinition> MarkerGroups { get { return _markerGroups; } }
        public IReadOnlyList<BattlefieldMarkerDefinition> MarkersInSourceOrder { get; private set; }
        public IReadOnlyDictionary<string, BattlefieldMarkerDefinition> Markers { get { return _markers; } }
        public string GameplayFingerprint { get; private set; }

        public BattlefieldRouteDefinition PrimaryRoute { get { return _routes[PrimaryRouteId]; } }

        internal CompiledBattlefieldMap(BattlefieldLayeredMapSource source,
            IEnumerable<BattlefieldGameplayCell> gameplayCells,
            IDictionary<string, BattlefieldRouteDefinition> routes,
            IDictionary<string, BattlefieldMarkerGroupDefinition> markerGroups,
            IDictionary<string, BattlefieldMarkerDefinition> markers,
            string gameplayFingerprint)
        {
            MapId = source.MapId;
            GridWidth = source.GridWidth;
            GridHeight = source.GridHeight;
            MapUnitsPerCell = source.MapUnitsPerCell;
            PrimaryRouteId = source.PrimaryRouteId;
            VisualCells = Array.AsReadOnly(source.VisualCells.ToArray());
            VisualSurfaceIds = Array.AsReadOnly(source.VisualSurfaceIds.ToArray());
            GameplayCells = Array.AsReadOnly(gameplayCells.ToArray());
            RoutesInSourceOrder = Array.AsReadOnly(source.Routes.ToArray());
            MarkerGroupsInSourceOrder = Array.AsReadOnly(source.MarkerGroups.ToArray());
            MarkersInSourceOrder = Array.AsReadOnly(source.Markers.ToArray());
            _routes = new ReadOnlyDictionary<string, BattlefieldRouteDefinition>(
                new Dictionary<string, BattlefieldRouteDefinition>(routes, StringComparer.Ordinal));
            _markerGroups = new ReadOnlyDictionary<string, BattlefieldMarkerGroupDefinition>(
                new Dictionary<string, BattlefieldMarkerGroupDefinition>(markerGroups, StringComparer.Ordinal));
            _markers = new ReadOnlyDictionary<string, BattlefieldMarkerDefinition>(
                new Dictionary<string, BattlefieldMarkerDefinition>(markers, StringComparer.Ordinal));
            GameplayFingerprint = gameplayFingerprint;
        }

        public int CellIndex(Vector2Int cell)
        {
            return cell.y * GridWidth + cell.x;
        }

        public string SurfaceAt(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return string.Empty;
            return VisualSurfaceIds[CellIndex(cell)];
        }

        public string BaseSurfaceAt(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return string.Empty;
            var visual = VisualCells[CellIndex(cell)];
            return visual == null ? string.Empty : visual.BaseSurfaceId;
        }

        public string LandformSurfaceAt(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return string.Empty;
            var visual = VisualCells[CellIndex(cell)];
            return visual == null ? string.Empty : visual.LandformSurfaceId;
        }

        public string ContourStyleAt(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return string.Empty;
            var visual = VisualCells[CellIndex(cell)];
            return visual == null ? string.Empty : visual.ContourStyleId;
        }

        public string EdgeStyleAt(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return string.Empty;
            var visual = VisualCells[CellIndex(cell)];
            return visual == null ? string.Empty : visual.EdgeStyleId;
        }

        public BattlefieldGameplayCell GameplayCellAt(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return default(BattlefieldGameplayCell);
            return GameplayCells[CellIndex(cell)];
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
        }
    }

    public static class BattlefieldLayeredMapCompiler
    {
        private static readonly IReadOnlyDictionary<string, BattlefieldCellCapabilities> CapabilityIds =
            new ReadOnlyDictionary<string, BattlefieldCellCapabilities>(
                new Dictionary<string, BattlefieldCellCapabilities>(StringComparer.Ordinal)
                {
                    { BattlefieldLayerIds.Capabilities.Plantable, BattlefieldCellCapabilities.Plantable },
                    { BattlefieldLayerIds.Capabilities.EnemyTraversable, BattlefieldCellCapabilities.EnemyTraversable },
                    { BattlefieldLayerIds.Capabilities.PlayerTraversable, BattlefieldCellCapabilities.PlayerTraversable },
                    { BattlefieldLayerIds.Capabilities.ItemSpawnCompatible, BattlefieldCellCapabilities.ItemSpawnCompatible },
                });

        private static readonly IReadOnlyDictionary<string, BattlefieldCollisionChannels> CollisionIds =
            new ReadOnlyDictionary<string, BattlefieldCollisionChannels>(
                new Dictionary<string, BattlefieldCollisionChannels>(StringComparer.Ordinal)
                {
                    { BattlefieldLayerIds.Collisions.BlocksGround, BattlefieldCollisionChannels.BlocksGround },
                    { BattlefieldLayerIds.Collisions.BlocksProjectile, BattlefieldCollisionChannels.BlocksProjectile },
                    { BattlefieldLayerIds.Collisions.BlocksPlacement, BattlefieldCollisionChannels.BlocksPlacement },
                });

        private static readonly HashSet<string> SurfaceIds = new HashSet<string>(StringComparer.Ordinal)
        {
            BattlefieldLayerIds.Surfaces.Soil,
            BattlefieldLayerIds.Surfaces.Grass,
            BattlefieldLayerIds.Surfaces.StoneRoad,
            BattlefieldLayerIds.Surfaces.Water,
        };

        private static readonly HashSet<string> EdgeStyleIds = new HashSet<string>(StringComparer.Ordinal)
        {
            BattlefieldLayerIds.EdgeStyles.Refined,
        };

        private static readonly HashSet<string> ContourStyleIds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                BattlefieldLayerIds.ContourStyles.Square,
                BattlefieldLayerIds.ContourStyles.Organic,
            };

        public static bool TryCompile(BattlefieldLayeredMapSource source,
            out CompiledBattlefieldMap compiled, out BattlefieldLayeredMapValidationResult validation)
        {
            compiled = null;
            validation = new BattlefieldLayeredMapValidationResult();
            if (source == null)
            {
                validation.Add("map.null", "map", "Layered battlefield source is required.");
                return false;
            }

            ValidateHeader(source, validation);
            var expectedCells = source.GridWidth > 0 && source.GridHeight > 0
                ? source.GridWidth * source.GridHeight : 0;
            if (source.VisualCells.Count != expectedCells)
                validation.Add("map.surface-count", "visualSurfaceIds",
                    "Expected " + expectedCells + " visual cells but found " + source.VisualCells.Count + ".");
            if (source.GameplayCells.Count != expectedCells)
                validation.Add("map.gameplay-cell-count", "gameplayCells",
                    "Expected " + expectedCells + " gameplay cells but found " + source.GameplayCells.Count + ".");

            ValidateVisualCells(source, validation);
            var gameplayCells = CompileGameplayCells(source, validation);
            var routes = IndexRoutes(source, gameplayCells, validation);
            var groups = IndexGroups(source, validation);
            var markers = IndexMarkers(source, gameplayCells, routes, groups, validation);
            ValidateExecutionProfile(source, routes, groups, markers, validation);

            if (!validation.IsValid) return false;
            var fingerprint = ComputeGameplayFingerprint(source, gameplayCells);
            compiled = new CompiledBattlefieldMap(source, gameplayCells, routes, groups, markers, fingerprint);
            return true;
        }

        public static CompiledBattlefieldMap CompileOrThrow(BattlefieldLayeredMapSource source)
        {
            CompiledBattlefieldMap compiled;
            BattlefieldLayeredMapValidationResult validation;
            if (TryCompile(source, out compiled, out validation)) return compiled;
            throw new InvalidOperationException("Layered battlefield map compilation failed:\n"
                + string.Join("\n", validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static void ValidateHeader(BattlefieldLayeredMapSource source,
            BattlefieldLayeredMapValidationResult validation)
        {
            if (source.SchemaVersion != BattlefieldLayerIds.SchemaVersion)
                validation.Add("map.schema-version", "schemaVersion",
                    "Unsupported layered map schema version " + source.SchemaVersion
                    + ". Expected " + BattlefieldLayerIds.SchemaVersion
                    + "; migrate the authoring asset before compilation.");
            if (string.IsNullOrWhiteSpace(source.MapId))
                validation.Add("map.id", "mapId", "Map identity is required.");
            if (source.GridWidth <= 0 || source.GridHeight <= 0)
                validation.Add("map.dimensions", "grid", "Grid dimensions must be positive.");
            if (source.MapUnitsPerCell <= 0f || float.IsNaN(source.MapUnitsPerCell)
                || float.IsInfinity(source.MapUnitsPerCell))
                validation.Add("map.scale", "mapUnitsPerCell", "Map units per cell must be finite and positive.");
            if (string.IsNullOrWhiteSpace(source.PrimaryRouteId))
                validation.Add("route.primary-id", "primaryRouteId", "Primary route identity is required.");
        }

        private static void ValidateVisualCells(BattlefieldLayeredMapSource source,
            BattlefieldLayeredMapValidationResult validation)
        {
            for (var index = 0; index < source.VisualCells.Count; index++)
            {
                var cell = source.VisualCells[index];
                var field = "visualCells[" + index + "]";
                if (cell == null)
                {
                    validation.Add("visual-cell.null", field, "Visual cell is required.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(cell.BaseSurfaceId))
                    validation.Add("surface.base-required", field + ".baseSurfaceId",
                        "Visual cell requires one base surface identity.");
                else if (!SurfaceIds.Contains(cell.BaseSurfaceId))
                    validation.Add("surface.unknown", field + ".baseSurfaceId",
                        "Unknown base surface identity '" + cell.BaseSurfaceId + "'.");

                if (!string.IsNullOrEmpty(cell.LandformSurfaceId)
                    && !SurfaceIds.Contains(cell.LandformSurfaceId))
                    validation.Add("surface.unknown", field + ".landformSurfaceId",
                        "Unknown landform surface identity '" + cell.LandformSurfaceId + "'.");
                if (!string.IsNullOrEmpty(cell.LandformSurfaceId)
                    && string.Equals(cell.BaseSurfaceId, cell.LandformSurfaceId, StringComparison.Ordinal))
                    validation.Add("surface.same-layer", field + ".landformSurfaceId",
                        "Base and landform surfaces must be different.");
                if (string.IsNullOrEmpty(cell.LandformSurfaceId))
                {
                    if (!string.IsNullOrEmpty(cell.ContourStyleId))
                        validation.Add("contour.without-landform", field + ".contourStyleId",
                            "A base-only visual cell must not declare a contour style.");
                }
                else if (string.IsNullOrWhiteSpace(cell.ContourStyleId))
                    validation.Add("contour.required", field + ".contourStyleId",
                        "Landform '" + cell.LandformSurfaceId
                        + "' requires an explicit contour style identity.");
                else if (!ContourStyleIds.Contains(cell.ContourStyleId))
                    validation.Add("contour.unknown-style", field + ".contourStyleId",
                        "Unknown contour style identity '" + cell.ContourStyleId + "'.");
                if (!string.IsNullOrEmpty(cell.EdgeStyleId)
                    && string.IsNullOrEmpty(cell.LandformSurfaceId))
                    validation.Add("edge.without-landform", field + ".edgeStyleId",
                        "An edge style requires a landform surface.");
                else if (!string.IsNullOrEmpty(cell.EdgeStyleId)
                    && !EdgeStyleIds.Contains(cell.EdgeStyleId))
                    validation.Add("edge.unknown-style", field + ".edgeStyleId",
                        "Unknown edge style identity '" + cell.EdgeStyleId + "'.");
            }

            ValidateContourCompatibility(source, validation);
            ValidateEdgeStyleCompatibility(source, validation);
        }

        private static void ValidateContourCompatibility(BattlefieldLayeredMapSource source,
            BattlefieldLayeredMapValidationResult validation)
        {
            if (source.GridWidth <= 0 || source.GridHeight <= 0
                || source.VisualCells.Count != source.GridWidth * source.GridHeight) return;
            var forwardNeighbors = new[]
            {
                Vector2Int.right,
                Vector2Int.down,
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
            };
            for (var index = 0; index < source.VisualCells.Count; index++)
            {
                var cell = source.VisualCells[index];
                if (cell == null || string.IsNullOrEmpty(cell.LandformSurfaceId)
                    || string.IsNullOrEmpty(cell.ContourStyleId)) continue;
                var coordinate = new Vector2Int(index % source.GridWidth, index / source.GridWidth);
                foreach (var offset in forwardNeighbors)
                {
                    var neighbor = coordinate + offset;
                    if (!InBounds(source, neighbor)) continue;
                    var neighborIndex = neighbor.y * source.GridWidth + neighbor.x;
                    var other = source.VisualCells[neighborIndex];
                    if (other == null || string.IsNullOrEmpty(other.LandformSurfaceId)
                        || string.IsNullOrEmpty(other.ContourStyleId)
                        || string.Equals(cell.ContourStyleId, other.ContourStyleId,
                            StringComparison.Ordinal)) continue;
                    validation.Add("contour.shared-vertex-mix",
                        "visualCells[" + neighborIndex + "].contourStyleId",
                        "Landform cells " + coordinate + " ('" + cell.ContourStyleId
                        + "') and " + neighbor + " ('" + other.ContourStyleId
                        + "') share an edge or vertex without a contour transition binding.");
                }
            }
        }

        private static void ValidateEdgeStyleCompatibility(BattlefieldLayeredMapSource source,
            BattlefieldLayeredMapValidationResult validation)
        {
            if (source.GridWidth <= 0 || source.GridHeight <= 0
                || source.VisualCells.Count != source.GridWidth * source.GridHeight) return;
            var forwardNeighbors = new[]
            {
                Vector2Int.right,
                Vector2Int.down,
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
            };
            for (var index = 0; index < source.VisualCells.Count; index++)
            {
                var cell = source.VisualCells[index];
                if (cell == null || string.IsNullOrEmpty(cell.LandformSurfaceId)
                    || string.IsNullOrEmpty(cell.ContourStyleId)) continue;
                var coordinate = new Vector2Int(index % source.GridWidth, index / source.GridWidth);
                foreach (var offset in forwardNeighbors)
                {
                    var neighbor = coordinate + offset;
                    if (!InBounds(source, neighbor)) continue;
                    var neighborIndex = neighbor.y * source.GridWidth + neighbor.x;
                    var other = source.VisualCells[neighborIndex];
                    if (!HasSameExactTerrainRegionIdentity(cell, other)
                        || string.Equals(cell.EdgeStyleId, other.EdgeStyleId,
                            StringComparison.Ordinal)) continue;
                    validation.Add("edge.shared-region-mix",
                        "visualCells[" + neighborIndex + "].edgeStyleId",
                        "Landform cells " + coordinate + " and " + neighbor
                        + " share one exact foreground/background/contour region but use edge styles '"
                        + cell.EdgeStyleId + "' and '" + other.EdgeStyleId
                        + "'. One connected exact region must use one optional edge style.");
                }
            }
        }

        private static bool HasSameExactTerrainRegionIdentity(BattlefieldVisualCellSource first,
            BattlefieldVisualCellSource second)
        {
            return first != null && second != null
                && !string.IsNullOrEmpty(first.LandformSurfaceId)
                && string.Equals(first.LandformSurfaceId, second.LandformSurfaceId,
                    StringComparison.Ordinal)
                && string.Equals(first.BaseSurfaceId, second.BaseSurfaceId,
                    StringComparison.Ordinal)
                && string.Equals(first.ContourStyleId, second.ContourStyleId,
                    StringComparison.Ordinal);
        }

        private static BattlefieldGameplayCell[] CompileGameplayCells(BattlefieldLayeredMapSource source,
            BattlefieldLayeredMapValidationResult validation)
        {
            var cells = new BattlefieldGameplayCell[source.GameplayCells.Count];
            for (var index = 0; index < source.GameplayCells.Count; index++)
            {
                var authored = source.GameplayCells[index];
                if (authored == null)
                {
                    validation.Add("gameplay-cell.null", "gameplayCells[" + index + "]",
                        "Gameplay cell is required.");
                    continue;
                }

                var capabilities = BattlefieldCellCapabilities.None;
                var capabilitySet = new HashSet<string>(StringComparer.Ordinal);
                foreach (var id in authored.CapabilityIds)
                {
                    var normalized = id ?? string.Empty;
                    BattlefieldCellCapabilities resolved;
                    if (!capabilitySet.Add(normalized))
                        validation.Add("capability.duplicate", "gameplayCells[" + index + "].capabilityIds",
                            "Duplicate capability identity '" + normalized + "'.");
                    else if (!CapabilityIds.TryGetValue(normalized, out resolved))
                        validation.Add("capability.unknown", "gameplayCells[" + index + "].capabilityIds",
                            "Unknown capability identity '" + normalized + "'.");
                    else capabilities |= resolved;
                }

                var collisions = BattlefieldCollisionChannels.None;
                var collisionSet = new HashSet<string>(StringComparer.Ordinal);
                foreach (var id in authored.CollisionIds)
                {
                    var normalized = id ?? string.Empty;
                    BattlefieldCollisionChannels resolved;
                    if (!collisionSet.Add(normalized))
                        validation.Add("collision.duplicate", "gameplayCells[" + index + "].collisionIds",
                            "Duplicate collision identity '" + normalized + "'.");
                    else if (!CollisionIds.TryGetValue(normalized, out resolved))
                        validation.Add("collision.unknown", "gameplayCells[" + index + "].collisionIds",
                            "Unknown collision identity '" + normalized + "'.");
                    else collisions |= resolved;
                }
                cells[index] = new BattlefieldGameplayCell(capabilities, collisions);
            }
            return cells;
        }

        private static Dictionary<string, BattlefieldRouteDefinition> IndexRoutes(
            BattlefieldLayeredMapSource source, IReadOnlyList<BattlefieldGameplayCell> gameplayCells,
            BattlefieldLayeredMapValidationResult validation)
        {
            var routes = new Dictionary<string, BattlefieldRouteDefinition>(StringComparer.Ordinal);
            for (var routeIndex = 0; routeIndex < source.Routes.Count; routeIndex++)
            {
                var route = source.Routes[routeIndex];
                var field = "routes[" + routeIndex + "]";
                if (route == null)
                {
                    validation.Add("route.null", field, "Route definition is required.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(route.RouteId))
                    validation.Add("route.id", field + ".routeId", "Route identity is required.");
                else if (routes.ContainsKey(route.RouteId))
                    validation.Add("route.duplicate-id", field + ".routeId",
                        "Duplicate route identity '" + route.RouteId + "'.");
                else routes.Add(route.RouteId, route);

                if (route.Cells.Count < 2)
                    validation.Add("route.length", field + ".cells", "Route requires at least two cells.");
                var seen = new HashSet<Vector2Int>();
                for (var cellIndex = 0; cellIndex < route.Cells.Count; cellIndex++)
                {
                    var cell = route.Cells[cellIndex];
                    if (!InBounds(source, cell))
                        validation.Add("route.out-of-bounds", field + ".cells[" + cellIndex + "]",
                            "Route cell is outside the grid: " + cell + ".");
                    else if (!seen.Add(cell))
                        validation.Add("route.duplicate-cell", field + ".cells[" + cellIndex + "]",
                            "Route cell is duplicated: " + cell + ".");
                    else if (CellAt(source, gameplayCells, cell).Has(BattlefieldCellCapabilities.EnemyTraversable) == false)
                        validation.Add("route.not-traversable", field + ".cells[" + cellIndex + "]",
                            "Route cell lacks enemy-traversable capability: " + cell + ".");

                    if (cellIndex > 0 && !BattlefieldTopology.AreCoordinatesCardinalNeighbors(
                            route.Cells[cellIndex - 1], cell))
                        validation.Add("route.disconnected", field + ".cells[" + cellIndex + "]",
                            "Route cells are not cardinal neighbors: " + route.Cells[cellIndex - 1]
                            + " -> " + cell + ".");
                }
            }
            return routes;
        }

        private static Dictionary<string, BattlefieldMarkerGroupDefinition> IndexGroups(
            BattlefieldLayeredMapSource source, BattlefieldLayeredMapValidationResult validation)
        {
            var groups = new Dictionary<string, BattlefieldMarkerGroupDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < source.MarkerGroups.Count; index++)
            {
                var group = source.MarkerGroups[index];
                var field = "markerGroups[" + index + "]";
                if (group == null)
                {
                    validation.Add("marker-group.null", field, "Marker group is required.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(group.GroupId))
                    validation.Add("marker-group.id", field + ".groupId", "Marker group identity is required.");
                else if (groups.ContainsKey(group.GroupId))
                    validation.Add("marker-group.duplicate-id", field + ".groupId",
                        "Duplicate marker group identity '" + group.GroupId + "'.");
                else groups.Add(group.GroupId, group);
                if (!Enum.IsDefined(typeof(BattlefieldMarkerKind), group.MarkerKind))
                    validation.Add("marker-group.kind", field + ".markerKind", "Marker group kind is invalid.");
                if (group.MarkerKind != BattlefieldMarkerKind.InitialPotCandidate)
                    validation.Add("marker-group.unsupported-kind", field + ".markerKind",
                        "Only initial-pot candidate groups are supported by the current execution profile.");
                if (group.SelectionCount < 0)
                    validation.Add("marker-group.selection", field + ".selectionCount",
                        "Marker group selection count must not be negative.");
            }
            return groups;
        }

        private static Dictionary<string, BattlefieldMarkerDefinition> IndexMarkers(
            BattlefieldLayeredMapSource source, IReadOnlyList<BattlefieldGameplayCell> gameplayCells,
            IReadOnlyDictionary<string, BattlefieldRouteDefinition> routes,
            IReadOnlyDictionary<string, BattlefieldMarkerGroupDefinition> groups,
            BattlefieldLayeredMapValidationResult validation)
        {
            var markers = new Dictionary<string, BattlefieldMarkerDefinition>(StringComparer.Ordinal);
            var cellsByMarkerKind = new Dictionary<Vector2Int, HashSet<BattlefieldMarkerKind>>();
            for (var index = 0; index < source.Markers.Count; index++)
            {
                var marker = source.Markers[index];
                var field = "markers[" + index + "]";
                if (marker == null)
                {
                    validation.Add("marker.null", field, "Marker is required.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(marker.MarkerId))
                    validation.Add("marker.id", field + ".markerId", "Marker identity is required.");
                else if (markers.ContainsKey(marker.MarkerId))
                    validation.Add("marker.duplicate-id", field + ".markerId",
                        "Duplicate marker identity '" + marker.MarkerId + "'.");
                else markers.Add(marker.MarkerId, marker);
                if (!Enum.IsDefined(typeof(BattlefieldMarkerKind), marker.Kind))
                {
                    validation.Add("marker.kind", field + ".kind", "Marker kind is invalid.");
                    continue;
                }
                if (!InBounds(source, marker.Cell))
                {
                    validation.Add("marker.out-of-bounds", field + ".cell",
                        "Marker cell is outside the grid: " + marker.Cell + ".");
                    continue;
                }

                HashSet<BattlefieldMarkerKind> kinds;
                if (!cellsByMarkerKind.TryGetValue(marker.Cell, out kinds))
                {
                    kinds = new HashSet<BattlefieldMarkerKind>();
                    cellsByMarkerKind.Add(marker.Cell, kinds);
                }
                if (!kinds.Add(marker.Kind))
                    validation.Add("marker.duplicate-kind-at-cell", field + ".cell",
                        "Marker kind " + marker.Kind + " already exists at " + marker.Cell + ".");

                switch (marker.Kind)
                {
                    case BattlefieldMarkerKind.EnemySpawn:
                        ValidateRouteMarker(marker, field, routes, true, validation);
                        break;
                    case BattlefieldMarkerKind.RouteGoal:
                        ValidateRouteMarker(marker, field, routes, false, validation);
                        break;
                    case BattlefieldMarkerKind.Core:
                        RequireEmpty(marker.RouteId, "routeId", marker, field, validation);
                        RequireEmpty(marker.GroupId, "groupId", marker, field, validation);
                        break;
                    case BattlefieldMarkerKind.InitialPotCandidate:
                        BattlefieldMarkerGroupDefinition group;
                        if (string.IsNullOrWhiteSpace(marker.GroupId)
                            || !groups.TryGetValue(marker.GroupId, out group)
                            || group.MarkerKind != BattlefieldMarkerKind.InitialPotCandidate)
                            validation.Add("marker.missing-group", field + ".groupId",
                                "Initial-pot marker references an unknown or incompatible group '"
                                + marker.GroupId + "'.");
                        if (!CellAt(source, gameplayCells, marker.Cell)
                            .Has(BattlefieldCellCapabilities.Plantable))
                            validation.Add("marker.non-plantable", field + ".cell",
                                "Initial-pot marker cell is not plantable: " + marker.Cell + ".");
                        break;
                    case BattlefieldMarkerKind.ItemSpawn:
                        if (!CellAt(source, gameplayCells, marker.Cell)
                            .Has(BattlefieldCellCapabilities.ItemSpawnCompatible))
                            validation.Add("marker.item-incompatible", field + ".cell",
                                "Item-spawn marker cell lacks item-spawn capability: " + marker.Cell + ".");
                        if (string.IsNullOrWhiteSpace(marker.ContentId))
                            validation.Add("marker.content-id", field + ".contentId",
                                "Item-spawn marker requires a content identity.");
                        break;
                    case BattlefieldMarkerKind.Trigger:
                        if (string.IsNullOrWhiteSpace(marker.ContentId))
                            validation.Add("marker.content-id", field + ".contentId",
                                "Trigger marker requires a content identity.");
                        break;
                }
            }

            foreach (var pair in cellsByMarkerKind)
                if (pair.Value.Contains(BattlefieldMarkerKind.Core)
                    && pair.Value.Contains(BattlefieldMarkerKind.InitialPotCandidate))
                    validation.Add("marker.incompatible-at-cell", "markers",
                        "Core and initial-pot candidate markers cannot share cell " + pair.Key + ".");
            return markers;
        }

        private static void ValidateRouteMarker(BattlefieldMarkerDefinition marker, string field,
            IReadOnlyDictionary<string, BattlefieldRouteDefinition> routes, bool start,
            BattlefieldLayeredMapValidationResult validation)
        {
            BattlefieldRouteDefinition route;
            if (string.IsNullOrWhiteSpace(marker.RouteId) || !routes.TryGetValue(marker.RouteId, out route))
            {
                validation.Add("marker.missing-route", field + ".routeId",
                    "Marker references an unknown route '" + marker.RouteId + "'.");
                return;
            }
            if (route.Cells.Count == 0) return;
            var expected = start ? route.Cells[0] : route.Cells[route.Cells.Count - 1];
            if (marker.Cell != expected)
                validation.Add(start ? "marker.spawn-endpoint" : "marker.goal-endpoint", field + ".cell",
                    "Marker cell " + marker.Cell + " does not match route endpoint " + expected + ".");
        }

        private static void RequireEmpty(string value, string valueField,
            BattlefieldMarkerDefinition marker, string field,
            BattlefieldLayeredMapValidationResult validation)
        {
            if (!string.IsNullOrEmpty(value))
                validation.Add("marker.unexpected-reference", field + "." + valueField,
                    "Marker '" + marker.MarkerId + "' does not support " + valueField + ".");
        }

        private static void ValidateExecutionProfile(BattlefieldLayeredMapSource source,
            IReadOnlyDictionary<string, BattlefieldRouteDefinition> routes,
            IReadOnlyDictionary<string, BattlefieldMarkerGroupDefinition> groups,
            IReadOnlyDictionary<string, BattlefieldMarkerDefinition> markers,
            BattlefieldLayeredMapValidationResult validation)
        {
            if (routes.Count != 1)
                validation.Add("execution.route-count", "routes",
                    "Current execution profile requires exactly one route; found " + routes.Count + ".");
            BattlefieldRouteDefinition primary;
            if (!routes.TryGetValue(source.PrimaryRouteId, out primary))
            {
                validation.Add("execution.primary-route", "primaryRouteId",
                    "Primary route '" + source.PrimaryRouteId + "' is missing.");
                return;
            }

            var spawns = markers.Values.Where(marker => marker.Kind == BattlefieldMarkerKind.EnemySpawn).ToArray();
            var goals = markers.Values.Where(marker => marker.Kind == BattlefieldMarkerKind.RouteGoal).ToArray();
            var cores = markers.Values.Where(marker => marker.Kind == BattlefieldMarkerKind.Core).ToArray();
            if (spawns.Length != 1)
                validation.Add("execution.spawn-count", "markers", "Exactly one enemy-spawn marker is required.");
            if (goals.Length != 1)
                validation.Add("execution.goal-count", "markers", "Exactly one route-goal marker is required.");
            if (cores.Length != 1)
                validation.Add("execution.core-count", "markers", "Exactly one core marker is required.");

            if (spawns.Length == 1 && !string.Equals(spawns[0].RouteId, source.PrimaryRouteId,
                    StringComparison.Ordinal))
                validation.Add("execution.spawn-route", "markers", "Enemy spawn must reference the primary route.");
            if (goals.Length == 1 && !string.Equals(goals[0].RouteId, source.PrimaryRouteId,
                    StringComparison.Ordinal))
                validation.Add("execution.goal-route", "markers", "Route goal must reference the primary route.");
            if (goals.Length == 1 && cores.Length == 1
                && !BattlefieldTopology.AreCoordinatesCardinalNeighbors(goals[0].Cell, cores[0].Cell))
                validation.Add("execution.goal-core", "markers",
                    "Route goal " + goals[0].Cell + " is not cardinally adjacent to core " + cores[0].Cell + ".");

            var candidates = markers.Values
                .Where(marker => marker.Kind == BattlefieldMarkerKind.InitialPotCandidate)
                .GroupBy(marker => marker.GroupId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var totalSelection = 0;
            foreach (var group in groups.Values)
            {
                int candidateCount;
                candidates.TryGetValue(group.GroupId, out candidateCount);
                if (group.SelectionCount > candidateCount)
                    validation.Add("execution.marker-group-selection", "markerGroups." + group.GroupId,
                        "Selection count " + group.SelectionCount + " exceeds " + candidateCount + " candidates.");
                totalSelection += group.SelectionCount;
            }
            foreach (var groupId in candidates.Keys)
                if (!groups.ContainsKey(groupId))
                    validation.Add("execution.unowned-candidates", "markers",
                        "Initial-pot candidates reference missing group '" + groupId + "'.");
            if (totalSelection <= 0)
                validation.Add("execution.initial-pot-count", "markerGroups",
                    "At least one initial flowerpot selection is required.");
        }

        private static string ComputeGameplayFingerprint(BattlefieldLayeredMapSource source,
            IReadOnlyList<BattlefieldGameplayCell> gameplayCells)
        {
            const ulong offset = 14695981039346656037ul;
            var hash = offset;
            AddHash(ref hash, source.GridWidth);
            AddHash(ref hash, source.GridHeight);
            AddHash(ref hash, source.MapUnitsPerCell);
            foreach (var cell in gameplayCells)
            {
                AddHash(ref hash, (int)cell.Capabilities);
                AddHash(ref hash, (int)cell.CollisionChannels);
            }
            foreach (var route in source.Routes.OrderBy(value => value.RouteId, StringComparer.Ordinal))
            {
                AddHash(ref hash, route.RouteId);
                foreach (var cell in route.Cells)
                {
                    AddHash(ref hash, cell.x);
                    AddHash(ref hash, cell.y);
                }
            }
            foreach (var group in source.MarkerGroups.OrderBy(value => value.GroupId, StringComparer.Ordinal))
            {
                AddHash(ref hash, group.GroupId);
                AddHash(ref hash, (int)group.MarkerKind);
                AddHash(ref hash, group.SelectionCount);
            }
            foreach (var marker in source.Markers.OrderBy(value => value.MarkerId, StringComparer.Ordinal))
            {
                AddHash(ref hash, marker.MarkerId);
                AddHash(ref hash, (int)marker.Kind);
                AddHash(ref hash, marker.Cell.x);
                AddHash(ref hash, marker.Cell.y);
                AddHash(ref hash, marker.RouteId);
                AddHash(ref hash, marker.GroupId);
                AddHash(ref hash, marker.ContentId);
                AddHash(ref hash, (int)marker.Facing);
            }
            return "gameplay-map." + hash.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static void AddHash(ref ulong hash, float value)
        {
            AddHash(ref hash, BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        private static void AddHash(ref ulong hash, int value)
        {
            unchecked
            {
                for (var shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (byte)(value >> shift);
                    hash *= 1099511628211ul;
                }
            }
        }

        private static void AddHash(ref ulong hash, string value)
        {
            unchecked
            {
                var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
                foreach (var item in bytes)
                {
                    hash ^= item;
                    hash *= 1099511628211ul;
                }
                hash ^= 0xff;
                hash *= 1099511628211ul;
            }
        }

        private static BattlefieldGameplayCell CellAt(BattlefieldLayeredMapSource source,
            IReadOnlyList<BattlefieldGameplayCell> cells, Vector2Int cell)
        {
            var index = cell.y * source.GridWidth + cell.x;
            return index >= 0 && index < cells.Count ? cells[index] : default(BattlefieldGameplayCell);
        }

        private static bool InBounds(BattlefieldLayeredMapSource source, Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < source.GridWidth && cell.y >= 0 && cell.y < source.GridHeight;
        }
    }

    public static class BattlefieldLayeredMapFactory
    {
        public static BattlefieldLayeredMapSource CreateSingleRouteMap(string mapId, int width, int height,
            float mapUnitsPerCell, IEnumerable<Vector2Int> orderedRoute, Vector2Int core,
            IEnumerable<InitialPotGroup> initialPotGroups)
        {
            var route = (orderedRoute ?? Enumerable.Empty<Vector2Int>()).ToArray();
            var routeLookup = new HashSet<Vector2Int>(route);
            var groups = (initialPotGroups ?? Enumerable.Empty<InitialPotGroup>()).ToArray();
            var visuals = new BattlefieldVisualCellSource[Math.Max(0, width * height)];
            var gameplay = new BattlefieldGameplayCellSource[visuals.Length];
            for (var index = 0; index < visuals.Length; index++)
            {
                var cell = new Vector2Int(index % width, index / width);
                if (routeLookup.Contains(cell))
                {
                    visuals[index] = new BattlefieldVisualCellSource(
                        BattlefieldLayerIds.Surfaces.Soil);
                    gameplay[index] = new BattlefieldGameplayCellSource(
                        new[] { BattlefieldLayerIds.Capabilities.EnemyTraversable });
                }
                else if (cell == core)
                {
                    visuals[index] = new BattlefieldVisualCellSource(BattlefieldLayerIds.Surfaces.Soil);
                    gameplay[index] = new BattlefieldGameplayCellSource();
                }
                else
                {
                    visuals[index] = new BattlefieldVisualCellSource(
                        BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square, string.Empty);
                    gameplay[index] = new BattlefieldGameplayCellSource(
                        new[] { BattlefieldLayerIds.Capabilities.Plantable });
                }
            }

            var markerGroups = groups.Select(group => new BattlefieldMarkerGroupDefinition(
                group.Name, BattlefieldMarkerKind.InitialPotCandidate, group.InitialCount)).ToArray();
            var markers = new List<BattlefieldMarkerDefinition>();
            if (route.Length > 0)
            {
                markers.Add(new BattlefieldMarkerDefinition("marker.enemy-spawn.main",
                    BattlefieldMarkerKind.EnemySpawn, route[0], BattlefieldLayerIds.PrimaryRoute));
                markers.Add(new BattlefieldMarkerDefinition("marker.route-goal.main",
                    BattlefieldMarkerKind.RouteGoal, route[route.Length - 1], BattlefieldLayerIds.PrimaryRoute));
            }
            markers.Add(new BattlefieldMarkerDefinition("marker.core.main", BattlefieldMarkerKind.Core, core));
            foreach (var group in groups)
            for (var index = 0; index < group.Cells.Count; index++)
                markers.Add(new BattlefieldMarkerDefinition(
                    "marker.initial-pot." + NormalizeId(group.Name) + "." + (index + 1).ToString("00"),
                    BattlefieldMarkerKind.InitialPotCandidate, group.Cells[index], groupId: group.Name));

            return new BattlefieldLayeredMapSource(BattlefieldLayerIds.SchemaVersion,
                mapId, width, height, mapUnitsPerCell, BattlefieldLayerIds.PrimaryRoute,
                visuals, gameplay,
                new[] { new BattlefieldRouteDefinition(BattlefieldLayerIds.PrimaryRoute, route) },
                markerGroups, markers);
        }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unnamed";
            var builder = new StringBuilder(value.Length);
            foreach (var character in value.ToLowerInvariant())
                builder.Append(char.IsLetterOrDigit(character) || character == '-' ? character : '-');
            return builder.ToString().Trim('-');
        }
    }
}
