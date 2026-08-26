using System;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmMultiRouteDeterminismSmoke
    {
        private const int LaneCount = 8;
        private const int TravelRowCount = 5;
        private const int PlantRowStart = 5;
        private const int PlantRowCount = 2;

        public static void Validate(Func<BattlefieldMapDefinition> createMap)
        {
            if (createMap == null) throw new ArgumentNullException(nameof(createMap));
            var content = CreateBattleContent();
            ValidateTopology(createMap, content);
            ValidateSimultaneousLanePositions(createMap, content);
            ValidateUnknownRouteRejection(createMap, content);
            ValidateRouteIdentityChecksum(createMap, content);
            ValidateEquivalentFramePartitions(createMap, content);
            ValidateStandardSingleRouteParity();
            Debug.Log("FRUIT_DEFENSE_GM_MULTI_ROUTE_DETERMINISM_OK");
        }

        private static void ValidateTopology(Func<BattlefieldMapDefinition> createMap,
            CompiledBattleContentCatalog content)
        {
            var map = RequireMap(createMap());
            Assert(map.ExecutionProfile == BattlefieldExecutionProfile.GmMultiRoute,
                "factory map uses the GM multi-route execution profile");
            Assert(map.GridWidth == LaneCount && map.GridHeight == 7,
                "factory map is the required 8-by-7 grid");
            Assert(string.IsNullOrEmpty(map.PrimaryRouteId),
                "GM map does not expose a fake primary route");
            Assert(map.RouteIds.Count == LaneCount
                && map.RouteIds.Distinct(StringComparer.Ordinal).Count() == LaneCount,
                "GM map exposes eight stable distinct route IDs");
            Assert(map.Markers.Count(marker => marker.Kind
                    == BattlefieldMarkerKind.EnemySpawn) == LaneCount
                && map.Markers.Count(marker => marker.Kind
                    == BattlefieldMarkerKind.RouteGoal) == LaneCount
                && map.Markers.All(marker => marker.Kind != BattlefieldMarkerKind.Core),
                "GM map exposes eight spawn/goal pairs and no damageable core");

            for (var lane = 0; lane < LaneCount; lane++)
            {
                var routeId = map.RouteIds[lane];
                var route = map.Routes.Single(value => string.Equals(value.RouteId,
                    routeId, StringComparison.Ordinal));
                Assert(route.Cells.Count == TravelRowCount,
                    "lane " + lane + " contains five travel cells");
                for (var row = 0; row < TravelRowCount; row++)
                    Assert(route.Cells[row] == new Vector2Int(lane, row),
                        "lane " + lane + " stays in its own column at row " + row);
                Assert(map.SpawnCellForRoute(routeId) == new Vector2Int(lane, 0)
                    && map.GoalCellForRoute(routeId)
                        == new Vector2Int(lane, TravelRowCount - 1),
                    "lane " + lane + " resolves its paired spawn and goal markers");
                Assert(map.SampleRoute(routeId, 0f)
                        == map.CellToMap(new Vector2Int(lane, 0))
                    && map.SampleRoute(routeId, map.RouteLength(routeId))
                        == map.CellToMap(new Vector2Int(lane, TravelRowCount - 1)),
                    "lane " + lane + " endpoint sampling matches canonical cells");
            }

            var expectedPots = Enumerable.Range(0, LaneCount)
                .SelectMany(column => Enumerable.Range(PlantRowStart, PlantRowCount)
                    .Select(row => new Vector2Int(column, row)))
                .OrderBy(cell => cell.y).ThenBy(cell => cell.x).ToArray();
            Assert(map.PlantableCells.OrderBy(cell => cell.y).ThenBy(cell => cell.x)
                    .SequenceEqual(expectedPots),
                "only the complete bottom two rows are plantable");
            var simulation = NewGmSimulation(content, 19001, map);
            Assert(simulation.State.Pots.Select(pot => pot.Cell)
                    .OrderBy(cell => cell.y).ThenBy(cell => cell.x)
                    .SequenceEqual(expectedPots),
                "GM simulation creates exactly sixteen independently addressed pots");
        }

        private static void ValidateSimultaneousLanePositions(
            Func<BattlefieldMapDefinition> createMap,
            CompiledBattleContentCatalog content)
        {
            var simulation = NewGmSimulation(content, 19002, RequireMap(createMap()));
            var firstRoute = simulation.Map.RouteIds[0];
            var lastRoute = simulation.Map.RouteIds[LaneCount - 1];
            var first = simulation.SpawnEnemy(BattleContentIds.Enemies.Normal, firstRoute);
            var last = simulation.SpawnEnemy(BattleContentIds.Enemies.Normal, lastRoute);
            first.PathProgress = simulation.Map.RouteLength(firstRoute) * .5f;
            last.PathProgress = simulation.Map.RouteLength(lastRoute) * .5f;
            var firstPoint = simulation.ZombiePoint(first);
            var lastPoint = simulation.ZombiePoint(last);
            Assert(first.RouteId == firstRoute && last.RouteId == lastRoute,
                "live enemies retain their assigned route IDs");
            Assert(Mathf.Abs(firstPoint.x - lastPoint.x)
                    >= simulation.Map.MapUnitsPerCell * (LaneCount - 1) - .0001f
                && Mathf.Abs(firstPoint.y - lastPoint.y) <= .0001f,
                "equal progress on the outside lanes resolves distinct canonical positions");
        }

        private static void ValidateUnknownRouteRejection(
            Func<BattlefieldMapDefinition> createMap,
            CompiledBattleContentCatalog content)
        {
            var simulation = NewGmSimulation(content, 19003, RequireMap(createMap()));
            var countBefore = simulation.State.Zombies.Count;
            try
            {
                simulation.SpawnEnemy(BattleContentIds.Enemies.Normal,
                    "route.gm.missing");
                Fail("unknown route ID unexpectedly created a live enemy");
            }
            catch (ArgumentException exception)
            {
                Assert(exception.Message.IndexOf("route.gm.missing",
                        StringComparison.Ordinal) >= 0,
                    "unknown route rejection identifies the invalid route ID");
            }
            Assert(simulation.State.Zombies.Count == countBefore,
                "unknown route rejection occurs before the enemy becomes live");
        }

        private static void ValidateRouteIdentityChecksum(
            Func<BattlefieldMapDefinition> createMap,
            CompiledBattleContentCatalog content)
        {
            var first = NewGmSimulation(content, 19004, RequireMap(createMap()));
            var second = NewGmSimulation(content, 19004, RequireMap(createMap()));
            first.SpawnEnemy(BattleContentIds.Enemies.Normal, first.Map.RouteIds[0]);
            second.SpawnEnemy(BattleContentIds.Enemies.Normal, second.Map.RouteIds[1]);
            Assert(first.OutcomeStateChecksum() != second.OutcomeStateChecksum(),
                "enemy route identity participates in the deterministic gameplay checksum");
        }

        private static void ValidateEquivalentFramePartitions(
            Func<BattlefieldMapDefinition> createMap,
            CompiledBattleContentCatalog content)
        {
            var fine = NewGmSimulation(content, 19005, RequireMap(createMap()));
            var coarse = NewGmSimulation(content, 19005, RequireMap(createMap()));
            for (var lane = 0; lane < LaneCount; lane += 2)
            {
                fine.SpawnEnemy(BattleContentIds.Enemies.Runner, fine.Map.RouteIds[lane]);
                coarse.SpawnEnemy(BattleContentIds.Enemies.Runner,
                    coarse.Map.RouteIds[lane]);
            }
            for (var frame = 0; frame < 100; frame++) fine.AdvanceFrame(.01f);
            var coarseSteps = 0;
            for (var frame = 0; frame < 20; frame++)
                coarseSteps += coarse.AdvanceFrame(.05f);
            Assert(coarseSteps == 20,
                "coarse GM render frames consume twenty fixed logical steps");
            Assert(fine.OutcomeStateChecksum() == coarse.OutcomeStateChecksum(),
                "equivalent GM render-frame partitions produce the same state checksum");
        }

        private static void ValidateStandardSingleRouteParity()
        {
            var standard = new GameSimulation(19006);
            Assert(standard.Mode == BattleSimulationMode.Standard
                && standard.Map.ExecutionProfile
                    == BattlefieldExecutionProfile.StandardRelease
                && standard.Map.RouteIds.Count == 1
                && standard.Map.PrimaryRouteId == standard.Map.RouteIds[0],
                "standard simulation retains exactly one explicit primary route");
            Assert(standard.StartWave(out var reason),
                "standard first wave starts: " + reason);
            standard.Step();
            Assert(standard.State.Zombies.Count > 0
                && standard.State.Zombies.All(zombie => string.Equals(zombie.RouteId,
                    standard.Map.PrimaryRouteId, StringComparison.Ordinal)),
                "standard wave spawning assigns the validated primary route explicitly");
            var enemy = standard.State.Zombies[0];
            Assert(Vector2.Distance(standard.ZombiePoint(enemy),
                    standard.Map.Route.Sample(enemy.PathProgress)) <= .0001f,
                "standard route-aware lookup preserves the former single-route position");
        }

        private static CompiledBattleContentCatalog CreateBattleContent()
        {
            Assert(BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                    out var content, out var validation),
                "bundled battle content compiles for GM deterministic validation: "
                + Format(validation));
            return content;
        }

        private static GameSimulation NewGmSimulation(
            CompiledBattleContentCatalog content, int seed,
            BattlefieldMapDefinition map)
        {
            return new GameSimulation(content, seed, map,
                BattleSimulationMode.GmStress);
        }

        private static BattlefieldMapDefinition RequireMap(BattlefieldMapDefinition map)
        {
            if (map == null) Fail("production GM battlefield factory returned null");
            return map;
        }

        private static string Format(ContentValidationResult validation)
        {
            return validation == null ? "<missing validation>"
                : string.Join(" | ", validation.Issues.Select(issue => issue.ToString()));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) Fail(message);
        }

        private static void Fail(string message)
        {
            throw new InvalidOperationException(
                "GM multi-route deterministic validation failed: " + message);
        }
    }
}
