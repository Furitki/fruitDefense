using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattlefieldLayeredMapSmoke
    {
        private const string FixturePath =
            "Assets/Editor/Tests/Fixtures/battlefield-layered-pre-migration.json";
        private const int FixtureSchemaVersion = 2;
        private const int FixtureOutcomeProjectionVersion = 4;

        [Serializable]
        private sealed class BaselineFile
        {
            public int schemaVersion;
            public int outcomeProjectionVersion;
            public MapBaseline[] maps = Array.Empty<MapBaseline>();
        }

        [Serializable]
        private sealed class MapBaseline
        {
            public string levelId = string.Empty;
            public string mapId = string.Empty;
            public int gridWidth;
            public int gridHeight;
            public float mapUnitsPerCell;
            public int plantableCount;
            public string cellRoles = string.Empty;
            public string routeCells = string.Empty;
            public string routeDescriptors = string.Empty;
            public float routeLength;
            public string entryCell = string.Empty;
            public string exitCell = string.Empty;
            public string coreCell = string.Empty;
            public string initialPotGroups = string.Empty;
            public string grassMasks = string.Empty;
            public string routeMasks = string.Empty;
            public int outcomeStepCount;
            public int outcomeLives;
            public int outcomeWaveSpawned;
            public int outcomeZombieCount;
            public string outcomePhase = string.Empty;
            public string outcomeChecksum = string.Empty;
        }

        public static void Validate()
        {
            ValidateCompilerDiagnostics();
            ValidateFingerprintBoundaries();
            ValidateMigrationFixtures();
            Debug.Log("FRUIT_DEFENSE_LAYERED_MAP_OK fixtures=3");
        }

        private static void ValidateCompilerDiagnostics()
        {
            var source = CreateValidSource();
            ExpectValid(source, "valid layered source");
            ExpectIssue(Copy(source, schemaVersion: BattlefieldLayerIds.SchemaVersion - 1),
                "map.schema-version", "pre-contour map schema is explicitly rejected");
            ExpectIssue(Copy(source, surfaces: source.VisualSurfaceIds.Take(3)),
                "map.surface-count", "surface-size mismatch");

            var unknownSurfaces = source.VisualSurfaceIds.ToArray();
            unknownSurfaces[0] = "surface.unknown";
            ExpectIssue(Copy(source, surfaces: unknownSurfaces), "surface.unknown", "unknown surface");

            var missingBase = source.VisualCells.ToArray();
            missingBase[0] = new BattlefieldVisualCellSource(string.Empty,
                BattlefieldLayerIds.Surfaces.Grass);
            ExpectIssue(Copy(source, visualCells: missingBase), "surface.base-required",
                "missing visual base");

            var unsupportedStack = source.VisualCells.ToArray();
            unsupportedStack[0] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Grass, BattlefieldLayerIds.Surfaces.Grass);
            ExpectIssue(Copy(source, visualCells: unsupportedStack), "surface.same-layer",
                "same base and landform stack");

            var edgeWithoutLandform = source.VisualCells.ToArray();
            edgeWithoutLandform[0] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Soil, edgeStyleId: BattlefieldLayerIds.EdgeStyles.Refined);
            ExpectIssue(Copy(source, visualCells: edgeWithoutLandform), "edge.without-landform",
                "edge without landform");

            var unknownEdge = source.VisualCells.ToArray();
            unknownEdge[0] = new BattlefieldVisualCellSource(BattlefieldLayerIds.Surfaces.Soil,
                BattlefieldLayerIds.Surfaces.Grass, "edge.unknown");
            ExpectIssue(Copy(source, visualCells: unknownEdge), "edge.unknown-style",
                "unknown edge style");

            var partialExactEdge = source.VisualCells.ToArray();
            const int grassCellIndex = 4;
            partialExactEdge[grassCellIndex] = new BattlefieldVisualCellSource(
                partialExactEdge[grassCellIndex].BaseSurfaceId,
                partialExactEdge[grassCellIndex].LandformSurfaceId,
                partialExactEdge[grassCellIndex].ContourStyleId,
                BattlefieldLayerIds.EdgeStyles.Refined);
            ExpectIssue(Copy(source, visualCells: partialExactEdge),
                "edge.shared-region-mix",
                "partial edge style inside one shared-vertex exact region");

            var missingContour = source.VisualCells.ToArray();
            missingContour[6] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Soil, BattlefieldLayerIds.Surfaces.Grass,
                string.Empty, string.Empty);
            ExpectIssue(Copy(source, visualCells: missingContour), "contour.required",
                "landform without contour");

            var contourWithoutLandform = source.VisualCells.ToArray();
            contourWithoutLandform[6] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Soil, string.Empty,
                BattlefieldLayerIds.ContourStyles.Square, string.Empty);
            ExpectIssue(Copy(source, visualCells: contourWithoutLandform),
                "contour.without-landform", "base-only contour");

            var unknownContour = source.VisualCells.ToArray();
            unknownContour[6] = new BattlefieldVisualCellSource(
                BattlefieldLayerIds.Surfaces.Soil, BattlefieldLayerIds.Surfaces.Grass,
                "contour.unknown", string.Empty);
            ExpectIssue(Copy(source, visualCells: unknownContour), "contour.unknown-style",
                "unknown contour");

            var mixedContour = source.VisualCells.ToArray();
            mixedContour[6] = new BattlefieldVisualCellSource(
                mixedContour[6].BaseSurfaceId, mixedContour[6].LandformSurfaceId,
                BattlefieldLayerIds.ContourStyles.Organic, mixedContour[6].EdgeStyleId);
            ExpectIssue(Copy(source, visualCells: mixedContour), "contour.shared-vertex-mix",
                "shared edge or vertex contour mixture");

            var unknownCapabilities = source.GameplayCells.ToArray();
            unknownCapabilities[6] = new BattlefieldGameplayCellSource(new[] { "capability.unknown" });
            ExpectIssue(Copy(source, gameplay: unknownCapabilities), "capability.unknown", "unknown capability");

            var unknownCollisions = source.GameplayCells.ToArray();
            unknownCollisions[6] = new BattlefieldGameplayCellSource(
                unknownCollisions[6].CapabilityIds, new[] { "collision.unknown" });
            ExpectIssue(Copy(source, gameplay: unknownCollisions), "collision.unknown", "unknown collision");

            ExpectIssue(Copy(source, routes: source.Routes.Concat(new[] { source.Routes[0] })),
                "route.duplicate-id", "duplicate route identity");
            ExpectIssue(Copy(source, routes: new[]
            {
                new BattlefieldRouteDefinition(BattlefieldLayerIds.PrimaryRoute,
                    new[] { new Vector2Int(0, 0), new Vector2Int(2, 0) }),
            }), "route.disconnected", "disconnected route");

            var missingReferenceMarkers = source.Markers.Select(marker =>
                marker.Kind == BattlefieldMarkerKind.EnemySpawn
                    ? new BattlefieldMarkerDefinition(marker.MarkerId, marker.Kind, marker.Cell, "route.missing")
                    : marker).ToArray();
            ExpectIssue(Copy(source, markers: missingReferenceMarkers),
                "marker.missing-route", "missing route reference");

            var endpointMarkers = source.Markers.Select(marker =>
                marker.Kind == BattlefieldMarkerKind.EnemySpawn
                    ? new BattlefieldMarkerDefinition(marker.MarkerId, marker.Kind,
                        new Vector2Int(1, 0), marker.RouteId)
                    : marker).ToArray();
            ExpectIssue(Copy(source, markers: endpointMarkers),
                "marker.spawn-endpoint", "invalid spawn endpoint");

            ExpectIssue(Copy(source, markers: source.Markers.Concat(new[]
            {
                new BattlefieldMarkerDefinition("marker.invalid.outside", BattlefieldMarkerKind.Trigger,
                    new Vector2Int(99, 99), contentId: "trigger.invalid"),
            })), "marker.out-of-bounds", "out-of-bounds marker");

            ExpectIssue(Copy(source, markers: source.Markers.Concat(new[]
            {
                new BattlefieldMarkerDefinition("marker.invalid.core-pot",
                    BattlefieldMarkerKind.InitialPotCandidate, new Vector2Int(3, 2),
                    groupId: "pot-group"),
            })), "marker.incompatible-at-cell", "incompatible marker combination");

            ExpectIssue(Copy(source, routes: source.Routes.Concat(new[]
            {
                new BattlefieldRouteDefinition("route.alternate", source.Routes[0].Cells),
            })), "execution.standard.route-count", "unsupported multiple routes");
        }

        private static void ValidateFingerprintBoundaries()
        {
            var source = CreateValidSource();
            var baseline = Compile(source);
            var presentationOnly = source.VisualSurfaceIds.Select(id =>
                id == BattlefieldLayerIds.Surfaces.Grass
                    ? BattlefieldLayerIds.Surfaces.Soil : id).ToArray();
            var visualVariant = Compile(Copy(source, surfaces: presentationOnly));
            Assert(baseline.GameplayFingerprint == visualVariant.GameplayFingerprint,
                "presentation-only surface change altered gameplay fingerprint");

            var layeredVariant = source.VisualCells.Select((cell, index) => index == 6
                ? new BattlefieldVisualCellSource(BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.Water,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined)
                : cell).ToArray();
            var layeredCompiled = Compile(Copy(source, visualCells: layeredVariant));
            Assert(baseline.GameplayFingerprint == layeredCompiled.GameplayFingerprint,
                "base, landform or edge presentation change altered gameplay fingerprint");
            Assert(layeredCompiled.BaseSurfaceAt(new Vector2Int(2, 1))
                    == BattlefieldLayerIds.Surfaces.Soil
                && layeredCompiled.LandformSurfaceAt(new Vector2Int(2, 1))
                    == BattlefieldLayerIds.Surfaces.Water
                && layeredCompiled.ContourStyleAt(new Vector2Int(2, 1))
                    == BattlefieldLayerIds.ContourStyles.Square
                && layeredCompiled.EdgeStyleAt(new Vector2Int(2, 1))
                    == BattlefieldLayerIds.EdgeStyles.Refined,
                "compiled layered visual queries preserve ordered pair and edge style");

            var contourVariant = source.VisualCells.Select(cell =>
                cell == null || string.IsNullOrEmpty(cell.LandformSurfaceId) ? cell
                    : new BattlefieldVisualCellSource(cell.BaseSurfaceId,
                        cell.LandformSurfaceId, BattlefieldLayerIds.ContourStyles.Organic,
                        cell.EdgeStyleId)).ToArray();
            var contourCompiled = Compile(Copy(source, visualCells: contourVariant));
            Assert(baseline.GameplayFingerprint == contourCompiled.GameplayFingerprint,
                "presentation-only contour change altered gameplay fingerprint");

            var capabilityCells = source.GameplayCells.ToArray();
            capabilityCells[6] = new BattlefieldGameplayCellSource(new[]
            {
                BattlefieldLayerIds.Capabilities.Plantable,
                BattlefieldLayerIds.Capabilities.PlayerTraversable,
            });
            Assert(baseline.GameplayFingerprint != Compile(Copy(source,
                    gameplay: capabilityCells)).GameplayFingerprint,
                "capability change did not alter gameplay fingerprint");

            var collisionCells = source.GameplayCells.ToArray();
            collisionCells[6] = new BattlefieldGameplayCellSource(
                collisionCells[6].CapabilityIds,
                new[] { BattlefieldLayerIds.Collisions.BlocksProjectile });
            Assert(baseline.GameplayFingerprint != Compile(Copy(source,
                    gameplay: collisionCells)).GameplayFingerprint,
                "collision change did not alter gameplay fingerprint");

            var alternateRoute = BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                "smoke.route-variant", 4, 3, 1f,
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 1),
                    new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                }, new Vector2Int(3, 2),
                new[] { new InitialPotGroup("pot-group", 1, new[] { new Vector2Int(1, 2) }) },
                BattlefieldPlantableVisualStyle.LayeredSquareGrassOnSoil);
            Assert(baseline.GameplayFingerprint != Compile(alternateRoute).GameplayFingerprint,
                "route change did not alter gameplay fingerprint");

            var markerVariant = BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                source.MapId, source.GridWidth, source.GridHeight, source.MapUnitsPerCell,
                source.Routes[0].Cells, new Vector2Int(3, 2),
                new[] { new InitialPotGroup("pot-group-variant", 1, new[] { new Vector2Int(1, 1) }) },
                BattlefieldPlantableVisualStyle.LayeredSquareGrassOnSoil);
            Assert(baseline.GameplayFingerprint != Compile(markerVariant).GameplayFingerprint,
                "gameplay marker change did not alter gameplay fingerprint");

            var first = new GameSimulation(BundledLevelCatalogFactory.CreateCompiled().BattleContent,
                8080, new BattlefieldMapDefinition(source));
            var second = new GameSimulation(BundledLevelCatalogFactory.CreateCompiled().BattleContent,
                8080, new BattlefieldMapDefinition(Copy(source, surfaces: presentationOnly)));
            Assert(first.StartWave(out var firstReason), firstReason);
            Assert(second.StartWave(out var secondReason), secondReason);
            for (var step = 0; step < 240; step++)
            {
                first.Step();
                second.Step();
            }
            Assert(first.OutcomeStateChecksum() == second.OutcomeStateChecksum(),
                "presentation-only surface change altered deterministic outcome");
        }

        private static void ValidateMigrationFixtures()
        {
            Assert(File.Exists(FixturePath), "pre-migration fixture is missing");
            var fixture = JsonUtility.FromJson<BaselineFile>(File.ReadAllText(FixturePath));
            // Fixture v2 intentionally accepts the private v4 sole-owner outcome projection.
            // The transition removed duplicate enemy statuses and the compatibility sentinel;
            // map structure and simulation counters remain independently compared below.
            Assert(fixture != null
                && fixture.schemaVersion == FixtureSchemaVersion
                && fixture.outcomeProjectionVersion == FixtureOutcomeProjectionVersion
                && fixture.maps.Length == 3,
                "pre-migration fixture header is invalid");
            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            var mismatches = new List<string>();
            foreach (var expected in fixture.maps)
            {
                var actual = Capture(catalog, expected.levelId, expected.outcomeStepCount);
                var firstLevelSquare = string.Equals(expected.levelId,
                    BundledLevelCatalogIds.Levels.Orchard01, StringComparison.Ordinal);
                var comparableActual = JsonUtility.FromJson<MapBaseline>(
                    JsonUtility.ToJson(actual));
                if (firstLevelSquare)
                    comparableActual.grassMasks = expected.grassMasks;
                if (JsonUtility.ToJson(expected) != JsonUtility.ToJson(comparableActual))
                    mismatches.Add(expected.levelId + " checksum old="
                        + expected.outcomeChecksum + " new=" + actual.outcomeChecksum
                        + "\nExpected: " + JsonUtility.ToJson(expected)
                        + "\nActual: " + JsonUtility.ToJson(actual));
                var map = catalog.Resolve(expected.levelId).Value.Map;
                Assert(map.UsesLayeredMap && map.PrimaryRouteId == BattlefieldLayerIds.PrimaryRoute
                    && !string.IsNullOrWhiteSpace(map.GameplayFingerprint),
                    "bundled map is not fully layered: " + expected.levelId);
                Assert(map.VisualCells.All(cell => cell != null
                        && string.IsNullOrEmpty(cell.EdgeStyleId)
                        && (firstLevelSquare
                            ? (cell.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                                || cell.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Grass)
                                && string.IsNullOrEmpty(cell.LandformSurfaceId)
                                && string.IsNullOrEmpty(cell.ContourStyleId)
                            : cell.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Soil
                                && (string.IsNullOrEmpty(cell.LandformSurfaceId)
                                    ? string.IsNullOrEmpty(cell.ContourStyleId)
                                    : cell.ContourStyleId
                                        == BattlefieldLayerIds.ContourStyles.Square))),
                    "bundled map visual representation is not the approved per-level form: "
                    + expected.levelId);
            }
            Assert(mismatches.Count == 0,
                "layered migration differs from pre-migration fixture:\n"
                + string.Join("\n", mismatches.ToArray()));
        }

        private static MapBaseline Capture(CompiledLevelCatalog catalog, string levelId, int stepCount)
        {
            var resolution = catalog.Resolve(levelId);
            Assert(resolution.Succeeded, resolution.Error == null ? "level resolution failed" : resolution.Error.ToString());
            var resolved = resolution.Value;
            var map = resolved.Map;
            var simulation = new GameSimulation(resolved, 20260722);
            Assert(simulation.StartWave(out var reason), reason);
            for (var step = 0; step < stepCount; step++) simulation.Step();
            return new MapBaseline
            {
                levelId = levelId,
                mapId = map.MapId,
                gridWidth = map.GridWidth,
                gridHeight = map.GridHeight,
                mapUnitsPerCell = map.MapUnitsPerCell,
                plantableCount = map.PlantableCells.Count,
                cellRoles = LegacyRoleSignature(map),
                routeCells = string.Join(";", map.RouteCells.Select(Cell).ToArray()),
                routeDescriptors = string.Join(";", map.RouteTileDescriptors.Select(value =>
                    Cell(value.Cell) + ":" + (int)value.Kind + ":" + (int)value.Connections).ToArray()),
                routeLength = map.Route.TotalLength,
                entryCell = Cell(map.EntryCell),
                exitCell = Cell(map.ExitCell),
                coreCell = Cell(map.CoreCell),
                initialPotGroups = string.Join(";", map.InitialPotGroupOrder.Select(groupId =>
                {
                    var group = map.InitialPotGroups[groupId];
                    return groupId + ":" + group.InitialCount + ":"
                        + string.Join(",", group.Cells.Select(Cell).ToArray());
                }).ToArray()),
                grassMasks = Masks(map, BattlefieldLayerIds.Surfaces.Grass),
                routeMasks = Masks(map, BattlefieldLayerIds.Surfaces.StoneRoad),
                outcomeStepCount = stepCount,
                outcomeLives = simulation.State.Lives,
                outcomeWaveSpawned = simulation.State.WaveSpawned,
                outcomeZombieCount = simulation.State.Zombies.Count,
                outcomePhase = simulation.State.Phase.ToString(),
                outcomeChecksum = simulation.OutcomeStateChecksum(),
            };
        }

        private static BattlefieldLayeredMapSource CreateValidSource()
        {
            return BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                "smoke.layered-map", 4, 3, 1f,
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(3, 0), new Vector2Int(3, 1),
                }, new Vector2Int(3, 2),
                new[] { new InitialPotGroup("pot-group", 1, new[] { new Vector2Int(1, 1) }) },
                BattlefieldPlantableVisualStyle.LayeredSquareGrassOnSoil);
        }

        private static BattlefieldLayeredMapSource Copy(BattlefieldLayeredMapSource source,
            int? schemaVersion = null,
            IEnumerable<string> surfaces = null,
            IEnumerable<BattlefieldVisualCellSource> visualCells = null,
            IEnumerable<BattlefieldGameplayCellSource> gameplay = null,
            IEnumerable<BattlefieldRouteDefinition> routes = null,
            IEnumerable<BattlefieldMarkerGroupDefinition> groups = null,
            IEnumerable<BattlefieldMarkerDefinition> markers = null)
        {
            if (visualCells != null || surfaces == null)
                return new BattlefieldLayeredMapSource(schemaVersion ?? source.SchemaVersion,
                    source.MapId,
                    source.GridWidth, source.GridHeight, source.MapUnitsPerCell, source.PrimaryRouteId,
                    visualCells ?? source.VisualCells,
                    gameplay ?? source.GameplayCells, routes ?? source.Routes,
                    groups ?? source.MarkerGroups, markers ?? source.Markers,
                    source.ExecutionProfile);
            return new BattlefieldLayeredMapSource(schemaVersion ?? source.SchemaVersion,
                source.MapId,
                source.GridWidth, source.GridHeight, source.MapUnitsPerCell, source.PrimaryRouteId,
                surfaces ?? source.VisualSurfaceIds, gameplay ?? source.GameplayCells,
                routes ?? source.Routes, groups ?? source.MarkerGroups, markers ?? source.Markers,
                source.ExecutionProfile);
        }

        private static CompiledBattlefieldMap Compile(BattlefieldLayeredMapSource source)
        {
            ExpectValid(source, "fingerprint source");
            return BattlefieldLayeredMapCompiler.CompileOrThrow(source);
        }

        private static void ExpectValid(BattlefieldLayeredMapSource source, string label)
        {
            Assert(BattlefieldLayeredMapCompiler.TryCompile(source, out var compiled, out var validation)
                && compiled != null && validation.IsValid,
                label + " failed: " + string.Join(" | ", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static void ExpectIssue(BattlefieldLayeredMapSource source, string code, string label)
        {
            Assert(!BattlefieldLayeredMapCompiler.TryCompile(source, out var compiled, out var validation)
                && compiled == null && validation.Issues.Any(issue => issue.Code == code),
                label + " did not report " + code + ": "
                + string.Join(" | ", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static string Masks(BattlefieldMapDefinition map, string surfaceId)
        {
            var values = new string[(map.GridWidth + 1) * (map.GridHeight + 1)];
            var index = 0;
            for (var y = 0; y <= map.GridHeight; y++)
            for (var x = 0; x <= map.GridWidth; x++)
                values[index++] = ((int)BattlefieldDualGridTerrain.ResolveLandformMask(
                    map, x, y, surfaceId,
                    BattlefieldLayerIds.ContourStyles.Square)).ToString();
            return string.Join(",", values);
        }

        private static string LegacyRoleSignature(BattlefieldMapDefinition map)
        {
            var roles = new int[map.GridWidth * map.GridHeight];
            for (var index = 0; index < roles.Length; index++)
            {
                var cell = new Vector2Int(index % map.GridWidth, index / map.GridWidth);
                roles[index] = map.IsPlantable(cell) ? 0 : 2;
            }
            foreach (var cell in map.RouteCells) roles[cell.y * map.GridWidth + cell.x] = 1;
            roles[map.EnemySpawnCell.y * map.GridWidth + map.EnemySpawnCell.x] = 3;
            roles[map.RouteGoalCell.y * map.GridWidth + map.RouteGoalCell.x] = 4;
            roles[map.CoreCell.y * map.GridWidth + map.CoreCell.x] = 5;
            return string.Join(",", roles.Select(value => value.ToString()).ToArray());
        }

        private static string Cell(Vector2Int cell)
        {
            return cell.x + "," + cell.y;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Layered battlefield smoke failed: " + message);
        }
    }
}
