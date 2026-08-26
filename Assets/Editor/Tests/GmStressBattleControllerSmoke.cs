using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Development.GmStress;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmStressBattleControllerSmoke
    {
        public static void Validate()
        {
            ValidateSelectorCatalogs();
            ValidatePerLaneFifoDrain();
            ValidateAllLaneFanOut();
            ValidateBoundedCapacityAndStablePrefix();
            ValidateEquivalentFramePartitions();
            ValidateFreePlantPlacementAndReplacement();
            ValidateDragOnlyPlantDeploymentInteraction();
            ValidatePerCellCombatDistanceParity();
            ValidateBundledPlantAbilityExecution();
            ValidateNoFailureLifecycle();
            ValidateInvalidCommandsAreAtomic();
            Debug.Log("FRUIT_DEFENSE_GM_STRESS_CONTROLLER_OK");
        }

        private static void ValidateSelectorCatalogs()
        {
            Assert(GmStressBattleIds.BatchCounts.SequenceEqual(new[] { 1, 10, 50 }),
                "GM batch selector catalog is exactly 1/10/50");
            Assert(GmStressBattleIds.EnemyDefinitionIds.Count == 4
                && GmStressBattleIds.EnemyDefinitionIds.Distinct(
                    StringComparer.Ordinal).Count() == 4,
                "GM exposes exactly four distinct bundled enemy selectors");
            Assert(GmStressBattleIds.PlantDefinitionIds.Count == 5
                && GmStressBattleIds.PlantDefinitionIds.Distinct(
                    StringComparer.Ordinal).Count() == 5,
                "GM exposes exactly five distinct bundled plant selectors");
            var content = GmStressBattleFactory.CreateContent();
            Assert(GmStressBattleIds.EnemyDefinitionIds.All(content.Enemies.ContainsKey)
                && GmStressBattleIds.PlantDefinitionIds.All(content.Plants.ContainsKey),
                "every GM selector resolves released battle content");
        }

        private static void ValidatePerLaneFifoDrain()
        {
            using (var controller = GmStressBattleFactory.Create(20001))
            {
                var normal = BattleContentIds.Enemies.Normal;
                var runner = BattleContentIds.Enemies.Runner;
                Assert(controller.EnqueueLane(3, normal, 1, out var firstAccepted,
                        out var firstReason) && firstAccepted == 1,
                    "first lane command is accepted: " + firstReason);
                Assert(controller.EnqueueLane(3, runner, 10, out var secondAccepted,
                        out var secondReason) && secondAccepted == 10,
                    "second lane command is accepted: " + secondReason);
                Assert(controller.PendingCount == 11
                    && controller.PendingInLane(3) == 11
                    && controller.PeekPendingEnemy(3) == normal,
                    "lane queue preserves FIFO command order before draining");

                Assert(controller.RunFixedSteps(1) == 1
                    && controller.ActiveCount == 1
                    && controller.PendingInLane(3) == 10
                    && controller.PeekPendingEnemy(3) == runner,
                    "one fixed step drains one FIFO head from the addressed lane");
                var first = controller.Simulation.State.Zombies.Single();
                Assert(first.DefinitionId == normal
                    && first.RouteId == controller.LaneIds[3],
                    "drained enemy owns the selected definition and true lane route");

                controller.RunFixedSteps(1);
                var ordered = controller.Simulation.State.Zombies
                    .OrderBy(enemy => enemy.Id).ToArray();
                Assert(ordered.Length == 2 && ordered[1].DefinitionId == runner
                    && ordered[1].RouteId == controller.LaneIds[3],
                    "the next fixed step drains the next queued enemy in order");
            }
        }

        private static void ValidateAllLaneFanOut()
        {
            using (var controller = GmStressBattleFactory.Create(20002))
            {
                var armored = BattleContentIds.Enemies.Armored;
                Assert(controller.EnqueueAll(armored, 10, out var accepted,
                        out var reason) && accepted == 80,
                    "all-lanes batch is accepted: " + reason);
                Assert(controller.PendingCount == 80
                    && Enumerable.Range(0, 8).All(lane =>
                        controller.PendingInLane(lane) == 10
                        && controller.PeekPendingEnemy(lane) == armored),
                    "all-lanes command fans out ten enemies to every lane");
                controller.RunFixedSteps(1);
                Assert(controller.ActiveCount == 8 && controller.PendingCount == 72,
                    "one fixed step drains exactly one enemy from every non-empty lane");
                var routeOrder = controller.Simulation.State.Zombies
                    .OrderBy(enemy => enemy.Id).Select(enemy => enemy.RouteId).ToArray();
                Assert(routeOrder.SequenceEqual(controller.LaneIds),
                    "all-lanes draining uses stable lane-zero-through-seven order");
            }
        }

        private static void ValidateBoundedCapacityAndStablePrefix()
        {
            using (var laneClamp = GmStressBattleFactory.Create(20003))
            {
                FillTo495(laneClamp);
                var beforeLane = laneClamp.PendingInLane(0);
                Assert(laneClamp.EnqueueLane(0, BattleContentIds.Enemies.Runner, 10,
                        out var accepted, out var reason) && accepted == 5,
                    "partially available lane command accepts its stable prefix: "
                    + reason);
                Assert(laneClamp.PendingCount == laneClamp.Capacity
                    && laneClamp.ActiveCount + laneClamp.PendingCount
                        == GmStressBattleIds.ActiveAndPendingCapacity
                    && laneClamp.PendingInLane(0) == beforeLane + 5,
                    "lane command clamps to the remaining five capacity slots");
                Assert(!laneClamp.EnqueueLane(1, BattleContentIds.Enemies.Normal, 1,
                        out var fullAccepted, out _) && fullAccepted == 0
                    && laneClamp.PendingCount == laneClamp.Capacity,
                    "a full controller rejects further input without exceeding 500");
            }

            using (var allClamp = GmStressBattleFactory.Create(20004))
            {
                Assert(allClamp.EnqueueAll(BattleContentIds.Enemies.Boss, 50,
                        out var firstAccepted, out _) && firstAccepted == 400,
                    "initial 400-enemy all-lanes batch is accepted");
                Assert(allClamp.RemainingCapacity == 100,
                    "all-lanes prefix fixture leaves exactly 100 slots");
                Assert(allClamp.EnqueueAll(BattleContentIds.Enemies.Normal, 50,
                        out var prefixAccepted, out var reason)
                    && prefixAccepted == 100,
                    "partially available all-lanes command accepts a stable prefix: "
                    + reason);
                Assert(allClamp.PendingCount == allClamp.Capacity
                    && allClamp.PendingInLane(0) == 100
                    && allClamp.PendingInLane(1) == 100
                    && Enumerable.Range(2, 6).All(lane =>
                        allClamp.PendingInLane(lane) == 50),
                    "all-lanes capacity prefix fills lane zero then lane one deterministically");
            }
        }

        private static void ValidateEquivalentFramePartitions()
        {
            using (var fine = GmStressBattleFactory.Create(20005))
            using (var coarse = GmStressBattleFactory.Create(20005))
            {
                ApplyDeterministicCommands(fine);
                ApplyDeterministicCommands(coarse);
                for (var frame = 0; frame < 100; frame++) fine.AdvanceFrame(.01f);
                var coarseSteps = 0;
                for (var frame = 0; frame < 20; frame++)
                    coarseSteps += coarse.AdvanceFrame(.05f);
                Assert(coarseSteps == 20,
                    "coarse controller frames consume twenty fixed steps");
                Assert(fine.Checksum == coarse.Checksum
                    && fine.ActiveCount == coarse.ActiveCount
                    && fine.PendingCount == coarse.PendingCount
                    && fine.EscapedCount == coarse.EscapedCount,
                    "equivalent frame partitions preserve queues, counters, and checksum");
            }
        }

        private static void ValidateFreePlantPlacementAndReplacement()
        {
            using (var controller = GmStressBattleFactory.Create(20006))
            {
                var simulation = controller.Simulation;
                var economy = CaptureEconomy(simulation);
                var cells = simulation.State.Pots.OrderBy(pot => pot.Cell.y)
                    .ThenBy(pot => pot.Cell.x).Select(pot => pot.Cell).ToArray();
                Assert(cells.Length == 16, "GM controller owns sixteen production pots");
                for (var index = 0;
                     index < GmStressBattleIds.PlantDefinitionIds.Count; index++)
                {
                    var plantId = GmStressBattleIds.PlantDefinitionIds[index];
                    Assert(controller.PlaceOrReplacePlant(cells[index], plantId,
                            out var reason),
                        "bundled plant selector places for free: " + reason);
                    var pot = simulation.State.Pots.Single(value =>
                        value.Cell == cells[index]);
                    var plant = simulation.PlantAtPot(pot.Id);
                    Assert(plant != null && plant.DefinitionId == plantId
                        && plant.Star == 1 && plant.NurseryIndex == -1,
                        "placed GM plant is one-star and bound directly to its pot");
                }
                Assert(CaptureEconomy(simulation) == economy,
                    "placing all five plant types changes no economy or inventory value");

                var replacementCell = cells[0];
                var countBefore = simulation.State.Plants.Count;
                var replacementId = GmStressBattleIds.PlantDefinitionIds[4];
                Assert(controller.PlaceOrReplacePlant(replacementCell, replacementId,
                        out var replacementReason),
                    "occupied pot replacement succeeds: " + replacementReason);
                var replacementPot = simulation.State.Pots.Single(value =>
                    value.Cell == replacementCell);
                var replacement = simulation.PlantAtPot(replacementPot.Id);
                Assert(simulation.State.Plants.Count == countBefore
                    && replacement != null
                    && replacement.DefinitionId == replacementId
                    && replacement.Star == 1,
                    "replacement removes the old plant and leaves one selected one-star plant");
                Assert(CaptureEconomy(simulation) == economy,
                    "plant replacement changes no economy or inventory value");
            }
        }

        private static void ValidatePerCellCombatDistanceParity()
        {
            var standard = GameConfig.DefaultBattlefield;
            var gm = GmStressBattleFactory.CreateMap();
            Assert(Mathf.Approximately(standard.MapUnitsPerCell, gm.MapUnitsPerCell),
                "GM uses the normal battle's canonical map units per cell");
            Assert(Mathf.Approximately(standard.LegacyToMapScale,
                    gm.LegacyToMapScale),
                "GM uses the explicit normal-battle combat-distance calibration");
            var standardCellDistance = standard.FromLegacyDistance(44f)
                / standard.MapUnitsPerCell;
            var gmCellDistance = gm.FromLegacyDistance(44f) / gm.MapUnitsPerCell;
            Assert(Mathf.Approximately(standardCellDistance, gmCellDistance),
                "legacy combat distance is calibrated per cell instead of total route length");
        }

        private static void ValidateDragOnlyPlantDeploymentInteraction()
        {
            using (var controller = GmStressBattleFactory.Create(20009))
            {
                var simulation = controller.Simulation;
                var layout = new GmStressBattleLayout(simulation.Map);
                var pots = simulation.State.Pots.Where(value => value.Active)
                    .OrderBy(value => value.Cell.y)
                    .ThenBy(value => value.Cell.x)
                    .ThenBy(value => value.Id).ToArray();
                var potRects = pots.Select(value =>
                    layout.Battlefield.PotHitRect(value.Cell)).ToArray();
                var source = layout.PlantChoice(0).center;
                var drag = new GmStressPlantDragInteractor();

                drag.Begin(0, source);
                var tap = drag.Release(source, potRects);
                Assert(tap.Kind == GmStressPlantDragReleaseKind.Selected
                    && simulation.State.Plants.Count == 0,
                    "a plant-card tap selects only and never deploys");

                drag.Begin(0, source);
                var thresholdPoint = source + Vector2.right
                    * DragGeometry.ActivationDistance;
                var thresholdRelease = drag.Release(thresholdPoint, potRects);
                Assert(thresholdRelease.Kind == GmStressPlantDragReleaseKind.Selected
                    && simulation.State.Plants.Count == 0,
                    "movement at the normal threshold remains a click instead of a deploy");

                drag.Begin(0, source);
                var miss = drag.Release(new Vector2(390f, 850f), potRects);
                Assert(miss.Kind == GmStressPlantDragReleaseKind.Cancelled
                    && simulation.State.Plants.Count == 0,
                    "a drag released away from every pot is atomic and cancelled");

                drag.Begin(0, source);
                var targetCursor = potRects[0].center + Vector2.one
                    * DragGeometry.CursorOffset;
                var deploy = drag.Release(targetCursor, potRects);
                Assert(deploy.Kind == GmStressPlantDragReleaseKind.Deploy
                    && deploy.PotIndex == 0,
                    "the normal preview-overlap rule resolves the intended GM pot");
                var plantId = GmStressBattleIds.PlantDefinitionIds[deploy.PlantIndex];
                Assert(controller.PlaceOrReplacePlant(
                        pots[deploy.PotIndex].Cell, plantId, out var reason)
                    && simulation.State.Plants.Count == 1,
                    "only a successful drag release dispatches deployment: " + reason);
            }
        }

        private static void ValidateBundledPlantAbilityExecution()
        {
            var damagePlants = new[]
            {
                BattleContentIds.Plants.Pea,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Plants.Banana,
                BattleContentIds.Plants.Durian,
            };
            for (var index = 0; index < damagePlants.Length; index++)
            using (var controller = GmStressBattleFactory.Create(20100 + index))
            {
                var simulation = controller.Simulation;
                Assert(controller.PlaceOrReplacePlant(new Vector2Int(0, 5),
                        damagePlants[index], out var placeReason),
                    "GM damage plant deploys before combat: " + placeReason);
                var enemy = simulation.SpawnEnemy(BattleContentIds.Enemies.Boss,
                    controller.LaneIds[0]);
                enemy.Speed = 0f;
                enemy.PathProgress = simulation.Map.RouteLength(enemy.RouteId) - .1f;
                var initialHp = enemy.Hp;
                controller.RunFixedSteps(160);
                Assert(enemy.Hp < initialHp,
                    "GM plant executes its shared damage ability: " + damagePlants[index]);
                var plant = simulation.State.Plants.Single();
                Assert(plant.AbilityRuntimes.Count
                        == simulation.Content.ResolvePlantAbilities(
                            plant.DefinitionId, plant.EquipmentId).Count,
                    "GM plant owns the compiled normal-battle ability runtime: "
                    + damagePlants[index]);
            }

            using (var controller = GmStressBattleFactory.Create(20110))
            {
                var simulation = controller.Simulation;
                Assert(controller.PlaceOrReplacePlant(new Vector2Int(0, 5),
                        BattleContentIds.Plants.Sunflower, out var placeReason),
                    "GM producer deploys before combat: " + placeReason);
                var initialSun = simulation.State.Sun;
                controller.RunFixedSteps(210);
                Assert(simulation.State.Sun > initialSun,
                    "GM producer executes its shared periodic ability");
            }
        }

        private static void ValidateNoFailureLifecycle()
        {
            using (var controller = GmStressBattleFactory.Create(20007))
            {
                var simulation = controller.Simulation;
                simulation.State.Plants.Clear();
                var lives = simulation.State.Lives;
                Assert(simulation.State.Phase == GamePhase.Playing,
                    "GM simulation starts in a playable phase");
                Assert(!simulation.StartWave(out var waveReason)
                    && waveReason.IndexOf("GM", StringComparison.OrdinalIgnoreCase) >= 0,
                    "GM simulation explicitly rejects automatic waves");

                var routeId = controller.LaneIds[6];
                var escaped = simulation.SpawnEnemy(
                    BattleContentIds.Enemies.Normal, routeId);
                escaped.PathProgress = simulation.Map.RouteLength(routeId);
                controller.RunFixedSteps(1);
                Assert(controller.ActiveCount == 0 && controller.EscapedCount == 1
                    && simulation.State.Lives == lives
                    && simulation.State.Phase == GamePhase.Playing,
                    "route completion removes and counts the enemy without life loss or defeat");

                controller.RunFixedSteps(120);
                Assert(controller.ActiveCount == 0 && controller.PendingCount == 0
                    && simulation.State.Phase == GamePhase.Playing,
                    "an empty GM battlefield remains active and accepts future commands");
                Assert(controller.EnqueueLane(0, BattleContentIds.Enemies.Normal, 1,
                        out var accepted, out _) && accepted == 1,
                    "GM session still accepts commands after becoming empty");
            }
        }

        private static void ValidateInvalidCommandsAreAtomic()
        {
            using (var controller = GmStressBattleFactory.Create(20008))
            {
                var checksum = controller.Checksum;
                Assert(!controller.EnqueueLane(0, "enemy.gm.missing", 1,
                        out var missingAccepted, out _) && missingAccepted == 0
                    && !controller.EnqueueLane(0,
                        BattleContentIds.Enemies.Normal, 2,
                        out var batchAccepted, out _) && batchAccepted == 0
                    && controller.Checksum == checksum,
                    "unknown enemy and unsupported batch commands leave state unchanged");
                var plantCount = controller.Simulation.State.Plants.Count;
                Assert(!controller.PlaceOrReplacePlant(new Vector2Int(0, 4),
                        BattleContentIds.Plants.Pea, out _)
                    && !controller.PlaceOrReplacePlant(new Vector2Int(0, 5),
                        "plant.gm.missing", out _)
                    && controller.Simulation.State.Plants.Count == plantCount,
                    "invalid GM plant commands are rejected atomically");
            }
        }

        private static void FillTo495(GmStressBattleController controller)
        {
            Assert(controller.EnqueueAll(BattleContentIds.Enemies.Boss, 50,
                    out var allAccepted, out _) && allAccepted == 400,
                "capacity fixture enqueues 400 enemies");
            Assert(controller.EnqueueLane(7, BattleContentIds.Enemies.Armored, 50,
                    out var fiftyAccepted, out _) && fiftyAccepted == 50,
                "capacity fixture enqueues the next fifty enemies");
            for (var index = 0; index < 4; index++)
                Assert(controller.EnqueueLane(7, BattleContentIds.Enemies.Runner, 10,
                        out var tenAccepted, out _) && tenAccepted == 10,
                    "capacity fixture enqueues ten-enemy batch " + index);
            for (var index = 0; index < 5; index++)
                Assert(controller.EnqueueLane(7, BattleContentIds.Enemies.Normal, 1,
                        out var oneAccepted, out _) && oneAccepted == 1,
                    "capacity fixture enqueues one-enemy batch " + index);
            Assert(controller.PendingCount == 495 && controller.ActiveCount == 0,
                "capacity fixture reaches 495 active-plus-pending enemies");
        }

        private static void ApplyDeterministicCommands(
            GmStressBattleController controller)
        {
            Assert(controller.EnqueueLane(0, BattleContentIds.Enemies.Normal, 10,
                    out var firstAccepted, out _) && firstAccepted == 10,
                "deterministic lane-zero command succeeds");
            Assert(controller.EnqueueLane(0, BattleContentIds.Enemies.Runner, 10,
                    out var secondAccepted, out _) && secondAccepted == 10,
                "deterministic second FIFO command succeeds");
            Assert(controller.EnqueueLane(5, BattleContentIds.Enemies.Armored, 10,
                    out var thirdAccepted, out _) && thirdAccepted == 10,
                "deterministic lane-five command succeeds");
            Assert(controller.EnqueueAll(BattleContentIds.Enemies.Boss, 1,
                    out var allAccepted, out _) && allAccepted == 8,
                "deterministic all-lanes command succeeds");
            Assert(controller.PlaceOrReplacePlant(new Vector2Int(2, 5),
                BattleContentIds.Plants.Pea, out _),
                "deterministic plant command succeeds");
        }

        private static string CaptureEconomy(GameSimulation simulation)
        {
            var equipment = string.Join("|", simulation.State.Inventory.Equipment
                .Select(value => value.Key + "=" + value.Value));
            return simulation.State.Sun + ";" + simulation.State.RefreshCount + ";"
                + simulation.State.Inventory.Pots + ";" + equipment;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) Fail(message);
        }

        private static void Fail(string message)
        {
            throw new InvalidOperationException(
                "GM stress controller validation failed: " + message);
        }
    }
}
