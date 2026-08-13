using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class BattlefieldVisualCellAuthoringRecord
    {
        [SerializeField] private string baseSurfaceId = BattlefieldLayerIds.Surfaces.Soil;
        [SerializeField] private string landformSurfaceId = string.Empty;
        [SerializeField] private string contourStyleId = string.Empty;
        [SerializeField] private string edgeStyleId = string.Empty;

        public string BaseSurfaceId { get { return baseSurfaceId ?? string.Empty; } }
        public string LandformSurfaceId { get { return landformSurfaceId ?? string.Empty; } }
        public string ContourStyleId { get { return contourStyleId ?? string.Empty; } }
        public string EdgeStyleId { get { return edgeStyleId ?? string.Empty; } }

        public BattlefieldVisualCellAuthoringRecord()
        {
        }

        public BattlefieldVisualCellAuthoringRecord(string baseSurfaceId,
            string landformSurfaceId = null, string edgeStyleId = null)
            : this(baseSurfaceId, landformSurfaceId,
                string.IsNullOrEmpty(landformSurfaceId)
                    ? string.Empty : BattlefieldLayerIds.ContourStyles.Square,
                edgeStyleId)
        {
        }

        public BattlefieldVisualCellAuthoringRecord(string baseSurfaceId,
            string landformSurfaceId, string contourStyleId, string edgeStyleId)
        {
            this.baseSurfaceId = baseSurfaceId ?? string.Empty;
            this.landformSurfaceId = landformSurfaceId ?? string.Empty;
            this.contourStyleId = contourStyleId ?? string.Empty;
            this.edgeStyleId = edgeStyleId ?? string.Empty;
        }

        public BattlefieldVisualCellAuthoringRecord Copy()
        {
            return new BattlefieldVisualCellAuthoringRecord(BaseSurfaceId,
                LandformSurfaceId, ContourStyleId, EdgeStyleId);
        }

        public BattlefieldVisualCellAuthoringRecord WithContourStyle(string value)
        {
            return new BattlefieldVisualCellAuthoringRecord(BaseSurfaceId,
                LandformSurfaceId, value, EdgeStyleId);
        }

        public BattlefieldVisualCellAuthoringRecord WithContourAndEdgeStyle(
            string contourStyle, string edgeStyle)
        {
            return new BattlefieldVisualCellAuthoringRecord(BaseSurfaceId,
                LandformSurfaceId, contourStyle, edgeStyle);
        }

        public BattlefieldVisualCellAuthoringRecord WithEdgeStyle(string value)
        {
            return new BattlefieldVisualCellAuthoringRecord(BaseSurfaceId,
                LandformSurfaceId, ContourStyleId, value);
        }

        public BattlefieldVisualCellSource ToSource()
        {
            return new BattlefieldVisualCellSource(BaseSurfaceId,
                LandformSurfaceId, ContourStyleId, EdgeStyleId);
        }
    }

    [Serializable]
    public sealed class BattlefieldGameplayCellAuthoringRecord
    {
        [SerializeField] private string[] capabilityIds = Array.Empty<string>();
        [SerializeField] private string[] collisionIds = Array.Empty<string>();

        public IReadOnlyList<string> CapabilityIds
        {
            get { return capabilityIds ?? Array.Empty<string>(); }
        }

        public IReadOnlyList<string> CollisionIds
        {
            get { return collisionIds ?? Array.Empty<string>(); }
        }

        public BattlefieldGameplayCellAuthoringRecord()
        {
        }

        public BattlefieldGameplayCellAuthoringRecord(IEnumerable<string> capabilityIds,
            IEnumerable<string> collisionIds)
        {
            this.capabilityIds = CopyIds(capabilityIds);
            this.collisionIds = CopyIds(collisionIds);
        }

        public bool HasCapability(string id)
        {
            return CapabilityIds.Contains(id ?? string.Empty, StringComparer.Ordinal);
        }

        public bool HasCollision(string id)
        {
            return CollisionIds.Contains(id ?? string.Empty, StringComparer.Ordinal);
        }

        public BattlefieldGameplayCellAuthoringRecord Copy()
        {
            return new BattlefieldGameplayCellAuthoringRecord(CapabilityIds, CollisionIds);
        }

        public BattlefieldGameplayCellSource ToSource()
        {
            return new BattlefieldGameplayCellSource(CapabilityIds, CollisionIds);
        }

        private static string[] CopyIds(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Select(value => value ?? string.Empty).ToArray();
        }
    }

    [Serializable]
    public sealed class BattlefieldRouteAuthoringRecord
    {
        [SerializeField] private string routeId = BattlefieldLayerIds.PrimaryRoute;
        [SerializeField] private List<Vector2Int> cells = new List<Vector2Int>();

        public string RouteId { get { return routeId ?? string.Empty; } }
        public IReadOnlyList<Vector2Int> Cells
        {
            get { return cells == null ? Array.Empty<Vector2Int>() : cells; }
        }

        public BattlefieldRouteAuthoringRecord()
        {
        }

        public BattlefieldRouteAuthoringRecord(string routeId, IEnumerable<Vector2Int> cells)
        {
            this.routeId = routeId ?? string.Empty;
            this.cells = (cells ?? Enumerable.Empty<Vector2Int>()).ToList();
        }

        internal List<Vector2Int> MutableCells
        {
            get
            {
                if (cells == null) cells = new List<Vector2Int>();
                return cells;
            }
        }

        public BattlefieldRouteAuthoringRecord Copy()
        {
            return new BattlefieldRouteAuthoringRecord(RouteId, Cells);
        }

        public BattlefieldRouteDefinition ToSource()
        {
            return new BattlefieldRouteDefinition(RouteId, Cells);
        }
    }

    [Serializable]
    public sealed class BattlefieldMarkerGroupAuthoringRecord
    {
        [SerializeField] private string groupId = string.Empty;
        [SerializeField] private BattlefieldMarkerKind markerKind =
            BattlefieldMarkerKind.InitialPotCandidate;
        [SerializeField] private int selectionCount;

        public string GroupId { get { return groupId ?? string.Empty; } }
        public BattlefieldMarkerKind MarkerKind { get { return markerKind; } }
        public int SelectionCount { get { return selectionCount; } }

        public BattlefieldMarkerGroupAuthoringRecord()
        {
        }

        public BattlefieldMarkerGroupAuthoringRecord(string groupId,
            BattlefieldMarkerKind markerKind, int selectionCount)
        {
            this.groupId = groupId ?? string.Empty;
            this.markerKind = markerKind;
            this.selectionCount = selectionCount;
        }

        public BattlefieldMarkerGroupAuthoringRecord Copy()
        {
            return new BattlefieldMarkerGroupAuthoringRecord(GroupId, MarkerKind,
                SelectionCount);
        }

        public BattlefieldMarkerGroupDefinition ToSource()
        {
            return new BattlefieldMarkerGroupDefinition(GroupId, MarkerKind,
                SelectionCount);
        }
    }

    [Serializable]
    public sealed class BattlefieldMarkerAuthoringRecord
    {
        [SerializeField] private string markerId = string.Empty;
        [SerializeField] private BattlefieldMarkerKind kind;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private string routeId = string.Empty;
        [SerializeField] private string groupId = string.Empty;
        [SerializeField] private string contentId = string.Empty;
        [SerializeField] private BattlefieldDirection facing;

        public string MarkerId { get { return markerId ?? string.Empty; } }
        public BattlefieldMarkerKind Kind { get { return kind; } }
        public Vector2Int Cell { get { return cell; } }
        public string RouteId { get { return routeId ?? string.Empty; } }
        public string GroupId { get { return groupId ?? string.Empty; } }
        public string ContentId { get { return contentId ?? string.Empty; } }
        public BattlefieldDirection Facing { get { return facing; } }

        public BattlefieldMarkerAuthoringRecord()
        {
        }

        public BattlefieldMarkerAuthoringRecord(string markerId, BattlefieldMarkerKind kind,
            Vector2Int cell, string routeId = null, string groupId = null,
            string contentId = null, BattlefieldDirection facing = BattlefieldDirection.None)
        {
            this.markerId = markerId ?? string.Empty;
            this.kind = kind;
            this.cell = cell;
            this.routeId = routeId ?? string.Empty;
            this.groupId = groupId ?? string.Empty;
            this.contentId = contentId ?? string.Empty;
            this.facing = facing;
        }

        public BattlefieldMarkerAuthoringRecord Copy()
        {
            return new BattlefieldMarkerAuthoringRecord(MarkerId, Kind, Cell, RouteId,
                GroupId, ContentId, Facing);
        }

        public BattlefieldMarkerDefinition ToSource()
        {
            return new BattlefieldMarkerDefinition(MarkerId, Kind, Cell, RouteId,
                GroupId, ContentId, Facing);
        }
    }

    public enum BattlefieldMapAuthoringDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    public sealed class BattlefieldMapAuthoringDiagnostic
    {
        public BattlefieldMapAuthoringDiagnosticSeverity Severity { get; private set; }
        public string Code { get; private set; }
        public string Field { get; private set; }
        public string Message { get; private set; }
        public bool HasCell { get; private set; }
        public Vector2Int Cell { get; private set; }
        public string MarkerId { get; private set; }

        public bool IsBlocking
        {
            get { return Severity == BattlefieldMapAuthoringDiagnosticSeverity.Error; }
        }

        public BattlefieldMapAuthoringDiagnostic(
            BattlefieldMapAuthoringDiagnosticSeverity severity, string code, string field,
            string message, Vector2Int? cell = null, string markerId = null)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Field = field ?? string.Empty;
            Message = message ?? string.Empty;
            HasCell = cell.HasValue;
            Cell = cell.GetValueOrDefault();
            MarkerId = markerId ?? string.Empty;
        }

        public override string ToString()
        {
            return Severity + " " + Code + " [" + Field + "] " + Message;
        }
    }

    public sealed class BattlefieldMapResizeReport
    {
        public IReadOnlyList<Vector2Int> RemovedRouteCells { get; private set; }
        public IReadOnlyList<string> RemovedMarkerIds { get; private set; }

        public BattlefieldMapResizeReport(IEnumerable<Vector2Int> routeCells,
            IEnumerable<string> markerIds)
        {
            RemovedRouteCells = Array.AsReadOnly((routeCells
                ?? Enumerable.Empty<Vector2Int>()).ToArray());
            RemovedMarkerIds = Array.AsReadOnly((markerIds
                ?? Enumerable.Empty<string>()).ToArray());
        }
    }

    public sealed class BattlefieldMapAuthoringAsset : ScriptableObject
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);
        private static readonly HashSet<string> KnownSurfaces = new HashSet<string>(
            new[]
            {
                BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.Surfaces.StoneRoad,
                BattlefieldLayerIds.Surfaces.Water,
            }, StringComparer.Ordinal);
        private static readonly HashSet<string> KnownCapabilities = new HashSet<string>(
            new[]
            {
                BattlefieldLayerIds.Capabilities.Plantable,
                BattlefieldLayerIds.Capabilities.EnemyTraversable,
                BattlefieldLayerIds.Capabilities.PlayerTraversable,
                BattlefieldLayerIds.Capabilities.ItemSpawnCompatible,
            }, StringComparer.Ordinal);
        private static readonly HashSet<string> KnownContourStyles = new HashSet<string>(
            new[]
            {
                BattlefieldLayerIds.ContourStyles.Square,
                BattlefieldLayerIds.ContourStyles.Organic,
            }, StringComparer.Ordinal);
        private static readonly HashSet<string> KnownCollisions = new HashSet<string>(
            new[]
            {
                BattlefieldLayerIds.Collisions.BlocksGround,
                BattlefieldLayerIds.Collisions.BlocksProjectile,
                BattlefieldLayerIds.Collisions.BlocksPlacement,
            }, StringComparer.Ordinal);

        [SerializeField] private int schemaVersion = BattlefieldLayerIds.SchemaVersion;
        [SerializeField] private string mapId = string.Empty;
        [SerializeField] private int gridWidth = 8;
        [SerializeField] private int gridHeight = 7;
        [SerializeField] private float mapUnitsPerCell = 1f;
        [SerializeField] private List<BattlefieldVisualCellAuthoringRecord> visualCells =
            new List<BattlefieldVisualCellAuthoringRecord>();
        [SerializeField] private List<BattlefieldGameplayCellAuthoringRecord> gameplayCells =
            new List<BattlefieldGameplayCellAuthoringRecord>();
        [SerializeField] private BattlefieldRouteAuthoringRecord primaryRoute =
            new BattlefieldRouteAuthoringRecord(BattlefieldLayerIds.PrimaryRoute,
                Array.Empty<Vector2Int>());
        [SerializeField] private List<BattlefieldMarkerGroupAuthoringRecord> markerGroups =
            new List<BattlefieldMarkerGroupAuthoringRecord>();
        [SerializeField] private List<BattlefieldMarkerAuthoringRecord> markers =
            new List<BattlefieldMarkerAuthoringRecord>();

        public int SchemaVersion { get { return schemaVersion; } }
        public string MapId { get { return mapId ?? string.Empty; } }
        public int GridWidth { get { return gridWidth; } }
        public int GridHeight { get { return gridHeight; } }
        public float MapUnitsPerCell { get { return mapUnitsPerCell; } }
        public int ExpectedCellCount
        {
            get { return gridWidth > 0 && gridHeight > 0 ? gridWidth * gridHeight : 0; }
        }
        public IReadOnlyList<BattlefieldVisualCellAuthoringRecord> VisualCells
        {
            get { return visualCells == null
                ? Array.Empty<BattlefieldVisualCellAuthoringRecord>() : visualCells; }
        }
        public IReadOnlyList<BattlefieldGameplayCellAuthoringRecord> GameplayCells
        {
            get { return gameplayCells == null
                ? Array.Empty<BattlefieldGameplayCellAuthoringRecord>() : gameplayCells; }
        }
        public BattlefieldRouteAuthoringRecord PrimaryRoute { get { return primaryRoute; } }
        public IReadOnlyList<BattlefieldMarkerGroupAuthoringRecord> MarkerGroups
        {
            get { return markerGroups == null
                ? Array.Empty<BattlefieldMarkerGroupAuthoringRecord>() : markerGroups; }
        }
        public IReadOnlyList<BattlefieldMarkerAuthoringRecord> Markers
        {
            get { return markers == null
                ? Array.Empty<BattlefieldMarkerAuthoringRecord>() : markers; }
        }

        public static BattlefieldMapAuthoringAsset Create(string mapId, int width,
            int height, float unitsPerCell = 1f)
        {
            var asset = CreateInstance<BattlefieldMapAuthoringAsset>();
            asset.name = string.IsNullOrWhiteSpace(mapId) ? "BattlefieldMap" : mapId;
            string reason;
            if (!asset.Initialize(mapId, width, height, unitsPerCell, out reason))
            {
                DestroyImmediate(asset);
                throw new ArgumentException(reason);
            }
            return asset;
        }

        public bool Initialize(string stableMapId, int width, int height,
            float unitsPerCell, out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableMapId)
                || !StableIdPattern.IsMatch(stableMapId))
            {
                reason = "Map identity must be a stable lowercase semantic ID.";
                return false;
            }
            if (width <= 0 || height <= 0)
            {
                reason = "Grid dimensions must be positive.";
                return false;
            }
            if (unitsPerCell <= 0f || float.IsNaN(unitsPerCell)
                || float.IsInfinity(unitsPerCell))
            {
                reason = "Map units per cell must be finite and positive.";
                return false;
            }

            schemaVersion = BattlefieldLayerIds.SchemaVersion;
            mapId = stableMapId.Trim();
            gridWidth = width;
            gridHeight = height;
            mapUnitsPerCell = unitsPerCell;
            visualCells = Enumerable.Range(0, width * height)
                .Select(_ => DefaultVisual()).ToList();
            gameplayCells = Enumerable.Range(0, width * height)
                .Select(_ => DefaultGameplay()).ToList();
            primaryRoute = new BattlefieldRouteAuthoringRecord(
                BattlefieldLayerIds.PrimaryRoute, Array.Empty<Vector2Int>());
            markerGroups = new List<BattlefieldMarkerGroupAuthoringRecord>();
            markers = new List<BattlefieldMarkerAuthoringRecord>();
            reason = "ok";
            return true;
        }

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
        }

        public int CellIndex(Vector2Int cell)
        {
            return cell.y * gridWidth + cell.x;
        }

        public bool TryGetVisual(Vector2Int cell,
            out BattlefieldVisualCellAuthoringRecord record)
        {
            record = null;
            if (!InBounds(cell) || visualCells == null) return false;
            var index = CellIndex(cell);
            if (index < 0 || index >= visualCells.Count) return false;
            record = visualCells[index];
            return record != null;
        }

        public bool TryGetGameplay(Vector2Int cell,
            out BattlefieldGameplayCellAuthoringRecord record)
        {
            record = null;
            if (!InBounds(cell) || gameplayCells == null) return false;
            var index = CellIndex(cell);
            if (index < 0 || index >= gameplayCells.Count) return false;
            record = gameplayCells[index];
            return record != null;
        }

        public bool TrySetVisual(Vector2Int cell, string baseSurfaceId,
            string landformSurfaceId, string edgeStyleId, out string reason)
        {
            return TrySetVisualCells(new[] { cell }, baseSurfaceId, landformSurfaceId,
                string.IsNullOrEmpty(landformSurfaceId)
                    ? string.Empty : BattlefieldLayerIds.ContourStyles.Square,
                edgeStyleId, out reason);
        }

        public bool TrySetVisual(Vector2Int cell, string baseSurfaceId,
            string landformSurfaceId, string contourStyleId, string edgeStyleId,
            out string reason)
        {
            return TrySetVisualCells(new[] { cell }, baseSurfaceId, landformSurfaceId,
                contourStyleId, edgeStyleId, out reason);
        }

        public bool TrySetVisualCells(IEnumerable<Vector2Int> cells, string baseSurfaceId,
            string landformSurfaceId, string edgeStyleId, out string reason)
        {
            return TrySetVisualCells(cells, baseSurfaceId, landformSurfaceId,
                string.IsNullOrEmpty(landformSurfaceId)
                    ? string.Empty : BattlefieldLayerIds.ContourStyles.Square,
                edgeStyleId, out reason);
        }

        public bool TrySetVisualCells(IEnumerable<Vector2Int> cells, string baseSurfaceId,
            string landformSurfaceId, string contourStyleId, string edgeStyleId,
            out string reason)
        {
            var normalizedBase = baseSurfaceId ?? string.Empty;
            var normalizedLandform = landformSurfaceId ?? string.Empty;
            var normalizedContour = contourStyleId ?? string.Empty;
            var normalizedEdge = edgeStyleId ?? string.Empty;
            if (!KnownSurfaces.Contains(normalizedBase)
                || (!string.IsNullOrEmpty(normalizedLandform)
                    && !KnownSurfaces.Contains(normalizedLandform))
                || string.Equals(normalizedBase, normalizedLandform, StringComparison.Ordinal)
                || (string.IsNullOrEmpty(normalizedLandform)
                    ? !string.IsNullOrEmpty(normalizedContour)
                    : !KnownContourStyles.Contains(normalizedContour))
                || (!string.IsNullOrEmpty(normalizedEdge)
                    && (!string.Equals(normalizedEdge,
                            BattlefieldLayerIds.EdgeStyles.Refined, StringComparison.Ordinal)
                        || string.IsNullOrEmpty(normalizedLandform))))
            {
                reason = "Presentation identifiers do not describe a reviewed base/landform/edge combination.";
                return false;
            }
            Vector2Int[] resolved;
            if (!TryResolveMutationCells(cells, out resolved, out reason)) return false;
            var replacement = new BattlefieldVisualCellAuthoringRecord(normalizedBase,
                normalizedLandform, normalizedContour, normalizedEdge);
            var nextVisual = visualCells.Select(value => value.Copy()).ToList();
            var selected = new HashSet<Vector2Int>(resolved);
            var contourChanged = new HashSet<Vector2Int>();
            IEnumerable<Vector2Int> affectedContour = selected;
            if (!string.IsNullOrEmpty(normalizedLandform))
            {
                affectedContour = ResolveAffectedContourComponent(selected);
                foreach (var componentCell in affectedContour)
                {
                    if (selected.Contains(componentCell)) continue;
                    var index = CellIndex(componentCell);
                    var existing = nextVisual[index];
                    if (!string.Equals(existing.ContourStyleId, normalizedContour,
                            StringComparison.Ordinal))
                        contourChanged.Add(componentCell);
                    nextVisual[index] = existing.WithContourAndEdgeStyle(normalizedContour,
                        string.Equals(existing.ContourStyleId, normalizedContour,
                            StringComparison.Ordinal) ? existing.EdgeStyleId : string.Empty);
                }
            }
            foreach (var cell in resolved)
            {
                var existing = nextVisual[CellIndex(cell)];
                if (!string.Equals(existing.ContourStyleId, normalizedContour,
                        StringComparison.Ordinal))
                    contourChanged.Add(cell);
                nextVisual[CellIndex(cell)] = replacement.Copy();
            }
            if (!string.IsNullOrEmpty(normalizedLandform))
                NormalizeAffectedEdgeRegions(nextVisual, selected, affectedContour,
                    contourChanged, normalizedEdge);
            visualCells = nextVisual;
            reason = "ok";
            return true;
        }

        public bool TrySetGameplay(Vector2Int cell, IEnumerable<string> capabilityIds,
            IEnumerable<string> collisionIds, out string reason)
        {
            return TrySetGameplayCells(new[] { cell }, capabilityIds, collisionIds, out reason);
        }

        public bool TrySetGameplayCells(IEnumerable<Vector2Int> cells,
            IEnumerable<string> capabilityIds, IEnumerable<string> collisionIds,
            out string reason)
        {
            var capabilities = (capabilityIds ?? Enumerable.Empty<string>()).ToArray();
            var collisions = (collisionIds ?? Enumerable.Empty<string>()).ToArray();
            if (capabilities.Any(id => !KnownCapabilities.Contains(id ?? string.Empty))
                || collisions.Any(id => !KnownCollisions.Contains(id ?? string.Empty))
                || capabilities.Distinct(StringComparer.Ordinal).Count() != capabilities.Length
                || collisions.Distinct(StringComparer.Ordinal).Count() != collisions.Length)
            {
                reason = "Gameplay identifiers must be unique reviewed capabilities and collision channels.";
                return false;
            }
            Vector2Int[] resolved;
            if (!TryResolveMutationCells(cells, out resolved, out reason)) return false;
            var replacement = new BattlefieldGameplayCellAuthoringRecord(capabilities,
                collisions);
            foreach (var cell in resolved) gameplayCells[CellIndex(cell)] = replacement.Copy();
            reason = "ok";
            return true;
        }

        public bool TryResize(int width, int height, out BattlefieldMapResizeReport report,
            out string reason)
        {
            report = null;
            if (width <= 0 || height <= 0)
            {
                reason = "Grid dimensions must be positive.";
                return false;
            }
            if (!HasExactCoverage(out reason)) return false;

            var nextVisual = Enumerable.Range(0, width * height)
                .Select(_ => DefaultVisual()).ToList();
            var nextGameplay = Enumerable.Range(0, width * height)
                .Select(_ => DefaultGameplay()).ToList();
            for (var y = 0; y < Math.Min(gridHeight, height); y++)
            for (var x = 0; x < Math.Min(gridWidth, width); x++)
            {
                var oldIndex = y * gridWidth + x;
                var nextIndex = y * width + x;
                nextVisual[nextIndex] = visualCells[oldIndex].Copy();
                nextGameplay[nextIndex] = gameplayCells[oldIndex].Copy();
            }

            Func<Vector2Int, bool> inNext = cell =>
                cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
            var removedRoute = primaryRoute == null
                ? Array.Empty<Vector2Int>()
                : primaryRoute.Cells.Where(cell => !inNext(cell)).ToArray();
            var retainedRoute = primaryRoute == null
                ? Array.Empty<Vector2Int>()
                : primaryRoute.Cells.Where(inNext).ToArray();
            var removedMarkers = markers == null
                ? Array.Empty<string>()
                : markers.Where(marker => marker != null && !inNext(marker.Cell))
                    .Select(marker => marker.MarkerId).ToArray();
            var retainedMarkers = markers == null
                ? new List<BattlefieldMarkerAuthoringRecord>()
                : markers.Where(marker => marker != null && inNext(marker.Cell))
                    .Select(marker => marker.Copy()).ToList();

            gridWidth = width;
            gridHeight = height;
            visualCells = nextVisual;
            gameplayCells = nextGameplay;
            primaryRoute = new BattlefieldRouteAuthoringRecord(
                BattlefieldLayerIds.PrimaryRoute, retainedRoute);
            markers = retainedMarkers;
            report = new BattlefieldMapResizeReport(removedRoute, removedMarkers);
            reason = "ok";
            return true;
        }

        public bool TryAppendRouteCell(Vector2Int cell, out string reason)
        {
            if (!InBounds(cell))
            {
                reason = "Route cell is outside the configured grid: " + cell + ".";
                return false;
            }
            if (!HasExactCoverage(out reason)) return false;
            EnsureCollections();
            var route = primaryRoute.MutableCells;
            if (route.Contains(cell))
            {
                reason = "Route cell is already present: " + cell + ".";
                return false;
            }
            if (route.Count > 0 && !BattlefieldTopology.AreCoordinatesCardinalNeighbors(
                    route[route.Count - 1], cell))
            {
                reason = "Route append must be cardinally adjacent to the tail.";
                return false;
            }
            route.Add(cell);
            reason = "ok";
            return true;
        }

        public bool TryTruncateRoute(int retainedCellCount, out string reason)
        {
            EnsureCollections();
            var route = primaryRoute.MutableCells;
            if (retainedCellCount < 0 || retainedCellCount > route.Count)
            {
                reason = "Retained route-cell count is outside the authored route.";
                return false;
            }
            if (retainedCellCount == route.Count)
            {
                reason = "ok";
                return true;
            }
            route.RemoveRange(retainedCellCount, route.Count - retainedCellCount);
            reason = "ok";
            return true;
        }

        public bool TrySynchronizeRouteEndpoints(out string reason)
        {
            EnsureCollections();
            if (primaryRoute.Cells.Count < 2)
            {
                reason = "A route requires at least two cells before endpoints can be synchronized.";
                return false;
            }
            if (primaryRoute.Cells.Any(cell => !InBounds(cell)))
            {
                reason = "Route contains an out-of-bounds cell.";
                return false;
            }
            var replacements = markers.Where(marker => marker != null
                && marker.Kind != BattlefieldMarkerKind.EnemySpawn
                && marker.Kind != BattlefieldMarkerKind.RouteGoal).Select(marker => marker.Copy()).ToList();
            replacements.Add(new BattlefieldMarkerAuthoringRecord("marker.enemy-spawn.main",
                BattlefieldMarkerKind.EnemySpawn, primaryRoute.Cells[0],
                BattlefieldLayerIds.PrimaryRoute));
            replacements.Add(new BattlefieldMarkerAuthoringRecord("marker.route-goal.main",
                BattlefieldMarkerKind.RouteGoal,
                primaryRoute.Cells[primaryRoute.Cells.Count - 1],
                BattlefieldLayerIds.PrimaryRoute));
            markers = replacements;
            reason = "ok";
            return true;
        }

        public bool TrySetMarkerGroup(string groupId, int selectionCount, out string reason)
        {
            if (string.IsNullOrWhiteSpace(groupId)
                || !StableIdPattern.IsMatch(groupId) || selectionCount < 0)
            {
                reason = "Initial-pot group identity and a non-negative selection count are required.";
                return false;
            }
            EnsureCollections();
            var existing = markerGroups.FindIndex(group => group != null
                && string.Equals(group.GroupId, groupId, StringComparison.Ordinal));
            var replacement = new BattlefieldMarkerGroupAuthoringRecord(groupId,
                BattlefieldMarkerKind.InitialPotCandidate, selectionCount);
            if (existing >= 0) markerGroups[existing] = replacement;
            else markerGroups.Add(replacement);
            reason = "ok";
            return true;
        }

        public bool TryPlaceMarker(BattlefieldMarkerKind kind, Vector2Int cell,
            string groupId, out string markerId, out string reason)
        {
            markerId = string.Empty;
            if (!InBounds(cell))
            {
                reason = "Marker cell is outside the configured grid: " + cell + ".";
                return false;
            }
            if (kind != BattlefieldMarkerKind.Core
                && kind != BattlefieldMarkerKind.InitialPotCandidate)
            {
                reason = "Use the route-endpoint operation for spawn/goal markers; this editor only places core and initial-pot markers.";
                return false;
            }
            if (kind == BattlefieldMarkerKind.InitialPotCandidate
                && (string.IsNullOrWhiteSpace(groupId) || markerGroups == null
                    || markerGroups.All(group => group == null
                        || !string.Equals(group.GroupId, groupId, StringComparison.Ordinal))))
            {
                reason = "Initial-pot marker requires an existing typed group.";
                return false;
            }
            EnsureCollections();
            if (markers.Any(marker => marker != null && marker.Kind == kind && marker.Cell == cell))
            {
                reason = "The same marker kind already exists at " + cell + ".";
                return false;
            }
            if (kind == BattlefieldMarkerKind.Core)
            {
                markers.RemoveAll(marker => marker != null
                    && marker.Kind == BattlefieldMarkerKind.Core);
                markerId = "marker.core.main";
            }
            else
            {
                var generatedMarkerId = "marker.initial-pot." + NormalizeStableSegment(groupId) + ".x"
                    + cell.x + "-y" + cell.y;
                if (markers.Any(marker => marker != null
                    && string.Equals(marker.MarkerId, generatedMarkerId, StringComparison.Ordinal)))
                {
                    reason = "Generated marker identity is already in use: "
                        + generatedMarkerId + ".";
                    markerId = string.Empty;
                    return false;
                }
                markerId = generatedMarkerId;
            }
            markers.Add(new BattlefieldMarkerAuthoringRecord(markerId, kind, cell,
                groupId: kind == BattlefieldMarkerKind.InitialPotCandidate ? groupId : null));
            reason = "ok";
            return true;
        }

        public bool TryRemoveMarker(string markerId, out string reason)
        {
            if (string.IsNullOrWhiteSpace(markerId) || markers == null)
            {
                reason = "Marker identity is required.";
                return false;
            }
            var index = markers.FindIndex(marker => marker != null
                && string.Equals(marker.MarkerId, markerId, StringComparison.Ordinal));
            if (index < 0)
            {
                reason = "Marker does not exist: " + markerId + ".";
                return false;
            }
            markers.RemoveAt(index);
            reason = "ok";
            return true;
        }

        public bool ApplyRecommendedPresentation(out string reason)
        {
            if (!HasExactCoverage(out reason)) return false;
            EnsureCollections();
            var route = new HashSet<Vector2Int>(primaryRoute.Cells);
            for (var y = 0; y < gridHeight; y++)
            for (var x = 0; x < gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                var index = CellIndex(cell);
                var landform = route.Contains(cell)
                    ? BattlefieldLayerIds.Surfaces.StoneRoad
                    : gameplayCells[index].HasCapability(BattlefieldLayerIds.Capabilities.Plantable)
                        ? BattlefieldLayerIds.Surfaces.Grass : string.Empty;
                visualCells[index] = new BattlefieldVisualCellAuthoringRecord(
                    BattlefieldLayerIds.Surfaces.Soil, landform,
                    string.IsNullOrEmpty(landform)
                        ? string.Empty : BattlefieldLayerIds.ContourStyles.Square,
                    string.Empty);
            }
            reason = "ok";
            return true;
        }

        public BattlefieldLayeredMapSource ToSource()
        {
            var routes = primaryRoute == null
                ? Array.Empty<BattlefieldRouteDefinition>()
                : new[] { primaryRoute.ToSource() };
            return new BattlefieldLayeredMapSource(schemaVersion, MapId, gridWidth,
                gridHeight, mapUnitsPerCell, BattlefieldLayerIds.PrimaryRoute,
                (visualCells ?? new List<BattlefieldVisualCellAuthoringRecord>())
                    .Select(cell => cell == null ? null : cell.ToSource()),
                (gameplayCells ?? new List<BattlefieldGameplayCellAuthoringRecord>())
                    .Select(cell => cell == null ? null : cell.ToSource()),
                routes,
                (markerGroups ?? new List<BattlefieldMarkerGroupAuthoringRecord>())
                    .Select(group => group == null ? null : group.ToSource()),
                (markers ?? new List<BattlefieldMarkerAuthoringRecord>())
                    .Select(marker => marker == null ? null : marker.ToSource()));
        }

        public IReadOnlyList<BattlefieldMapAuthoringDiagnostic> CollectDiagnostics()
        {
            var diagnostics = new List<BattlefieldMapAuthoringDiagnostic>();
            var expected = ExpectedCellCount;
            if (visualCells == null || visualCells.Count != expected)
                diagnostics.Add(Error("authoring.visual-coverage", "visualCells",
                    "Expected " + expected + " visual cells but found "
                    + (visualCells == null ? 0 : visualCells.Count) + "."));
            if (gameplayCells == null || gameplayCells.Count != expected)
                diagnostics.Add(Error("authoring.gameplay-coverage", "gameplayCells",
                    "Expected " + expected + " gameplay cells but found "
                    + (gameplayCells == null ? 0 : gameplayCells.Count) + "."));

            CompiledBattlefieldMap ignored;
            BattlefieldLayeredMapValidationResult validation;
            BattlefieldLayeredMapCompiler.TryCompile(ToSource(), out ignored, out validation);
            foreach (var issue in validation.Issues)
            {
                Vector2Int? cell = TryResolveCell(issue.Field);
                diagnostics.Add(new BattlefieldMapAuthoringDiagnostic(
                    BattlefieldMapAuthoringDiagnosticSeverity.Error,
                    "canonical." + issue.Code, issue.Field, issue.Message, cell));
            }

            if (visualCells != null && gameplayCells != null
                && visualCells.Count == expected && gameplayCells.Count == expected)
            {
                for (var index = 0; index < expected; index++)
                {
                    var visual = visualCells[index];
                    var gameplay = gameplayCells[index];
                    if (visual == null || gameplay == null) continue;
                    var effective = string.IsNullOrWhiteSpace(visual.LandformSurfaceId)
                        ? visual.BaseSurfaceId : visual.LandformSurfaceId;
                    if (string.Equals(effective, BattlefieldLayerIds.Surfaces.Grass,
                            StringComparison.Ordinal)
                        && gameplay.HasCollision(BattlefieldLayerIds.Collisions.BlocksPlacement))
                    {
                        diagnostics.Add(new BattlefieldMapAuthoringDiagnostic(
                            BattlefieldMapAuthoringDiagnosticSeverity.Warning,
                            "authoring.presentation-gameplay-mismatch",
                            "visualCells[" + index + "]",
                            "Grass presentation does not make this blocked cell plantable.",
                            new Vector2Int(index % gridWidth, index / gridWidth)));
                    }
                }

                for (var index = 0; index < expected; index++)
                {
                    var visual = visualCells[index];
                    if (visual == null || string.IsNullOrWhiteSpace(
                            visual.LandformSurfaceId)) continue;
                    var cell = new Vector2Int(index % gridWidth, index / gridWidth);
                    var connected = new[]
                    {
                        cell + Vector2Int.left, cell + Vector2Int.right,
                        cell + Vector2Int.up, cell + Vector2Int.down,
                    }.Any(neighbor =>
                    {
                        BattlefieldVisualCellAuthoringRecord other;
                        return TryGetVisual(neighbor, out other) && other != null
                            && string.Equals(other.LandformSurfaceId,
                                visual.LandformSurfaceId, StringComparison.Ordinal);
                    });
                    if (!connected)
                        diagnostics.Add(new BattlefieldMapAuthoringDiagnostic(
                            BattlefieldMapAuthoringDiagnosticSeverity.Warning,
                            "authoring.isolated-landform", "visualCells[" + index + "]",
                            "This landform cell is isolated from the same semantic material.",
                            cell));
                }
            }
            return new ReadOnlyCollection<BattlefieldMapAuthoringDiagnostic>(diagnostics);
        }

        private bool TryResolveMutationCells(IEnumerable<Vector2Int> cells,
            out Vector2Int[] resolved, out string reason)
        {
            resolved = (cells ?? Enumerable.Empty<Vector2Int>()).Distinct().ToArray();
            if (resolved.Length == 0)
            {
                reason = "At least one cell is required.";
                return false;
            }
            var rejected = resolved.FirstOrDefault(cell => !InBounds(cell));
            if (resolved.Any(cell => !InBounds(cell)))
            {
                reason = "Cell is outside the configured grid: " + rejected + ".";
                return false;
            }
            return HasExactCoverage(out reason);
        }

        private IReadOnlyCollection<Vector2Int> ResolveAffectedContourComponent(
            ISet<Vector2Int> selected)
        {
            var resolved = new HashSet<Vector2Int>();
            var queued = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var cell in selected)
            {
                if (queued.Add(cell)) queue.Enqueue(cell);
            }
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                resolved.Add(current);
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;
                    var neighbor = current + new Vector2Int(offsetX, offsetY);
                    if (!InBounds(neighbor) || queued.Contains(neighbor)) continue;
                    var visual = visualCells[CellIndex(neighbor)];
                    if (!selected.Contains(neighbor)
                        && (visual == null || string.IsNullOrEmpty(visual.LandformSurfaceId)))
                        continue;
                    queued.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            return resolved;
        }

        private void NormalizeAffectedEdgeRegions(
            IList<BattlefieldVisualCellAuthoringRecord> records,
            ISet<Vector2Int> selected,
            IEnumerable<Vector2Int> affectedContour,
            ISet<Vector2Int> contourChanged,
            string selectedEdgeStyle)
        {
            var visited = new HashSet<Vector2Int>();
            foreach (var seed in affectedContour)
            {
                if (visited.Contains(seed) || !InBounds(seed)) continue;
                var seedRecord = records[CellIndex(seed)];
                if (seedRecord == null || string.IsNullOrEmpty(seedRecord.LandformSurfaceId))
                    continue;
                var region = ResolveExactTerrainRegion(records, seed);
                foreach (var cell in region) visited.Add(cell);
                var hasSelectedCell = region.Any(selected.Contains);
                var hasContourChange = region.Any(contourChanged.Contains);
                if (!hasSelectedCell && !hasContourChange) continue;
                var edgeStyle = hasSelectedCell ? selectedEdgeStyle : string.Empty;
                foreach (var cell in region)
                {
                    var index = CellIndex(cell);
                    records[index] = records[index].WithEdgeStyle(edgeStyle);
                }
            }
        }

        private IReadOnlyCollection<Vector2Int> ResolveExactTerrainRegion(
            IList<BattlefieldVisualCellAuthoringRecord> records, Vector2Int seed)
        {
            var identity = records[CellIndex(seed)];
            var resolved = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            resolved.Add(seed);
            queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;
                    var neighbor = current + new Vector2Int(offsetX, offsetY);
                    if (!InBounds(neighbor) || resolved.Contains(neighbor)) continue;
                    var candidate = records[CellIndex(neighbor)];
                    if (!HasExactTerrainIdentity(identity, candidate)) continue;
                    resolved.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            return resolved;
        }

        private static bool HasExactTerrainIdentity(
            BattlefieldVisualCellAuthoringRecord first,
            BattlefieldVisualCellAuthoringRecord second)
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

        private bool HasExactCoverage(out string reason)
        {
            if (visualCells == null || gameplayCells == null
                || visualCells.Count != ExpectedCellCount
                || gameplayCells.Count != ExpectedCellCount
                || visualCells.Any(cell => cell == null)
                || gameplayCells.Any(cell => cell == null))
            {
                reason = "Draft coverage is malformed; repair or resize it before editing cells.";
                return false;
            }
            reason = "ok";
            return true;
        }

        private void EnsureCollections()
        {
            if (visualCells == null) visualCells = new List<BattlefieldVisualCellAuthoringRecord>();
            if (gameplayCells == null) gameplayCells = new List<BattlefieldGameplayCellAuthoringRecord>();
            if (primaryRoute == null) primaryRoute = new BattlefieldRouteAuthoringRecord(
                BattlefieldLayerIds.PrimaryRoute, Array.Empty<Vector2Int>());
            if (markerGroups == null) markerGroups = new List<BattlefieldMarkerGroupAuthoringRecord>();
            if (markers == null) markers = new List<BattlefieldMarkerAuthoringRecord>();
        }

        private Vector2Int? TryResolveCell(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return null;
            var start = field.IndexOf('[', StringComparison.Ordinal);
            var end = start < 0 ? -1 : field.IndexOf(']', start + 1);
            int index;
            if (start >= 0 && end > start
                && int.TryParse(field.Substring(start + 1, end - start - 1), out index)
                && gridWidth > 0 && index >= 0 && index < ExpectedCellCount)
                return new Vector2Int(index % gridWidth, index / gridWidth);
            return null;
        }

        private static BattlefieldMapAuthoringDiagnostic Error(string code, string field,
            string message)
        {
            return new BattlefieldMapAuthoringDiagnostic(
                BattlefieldMapAuthoringDiagnosticSeverity.Error, code, field, message);
        }

        private static BattlefieldVisualCellAuthoringRecord DefaultVisual()
        {
            return new BattlefieldVisualCellAuthoringRecord(BattlefieldLayerIds.Surfaces.Soil);
        }

        private static BattlefieldGameplayCellAuthoringRecord DefaultGameplay()
        {
            return new BattlefieldGameplayCellAuthoringRecord(Array.Empty<string>(),
                Array.Empty<string>());
        }

        private static string NormalizeStableSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unnamed";
            return new string(value.Trim().ToLowerInvariant().Select(character =>
                char.IsLetterOrDigit(character) || character == '-' ? character : '-').ToArray())
                .Trim('-');
        }
    }
}
