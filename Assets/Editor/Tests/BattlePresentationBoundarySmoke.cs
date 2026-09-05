using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Presentation;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattlePresentationBoundarySmoke
    {
        public static void Run()
        {
            ValidateOrderedSingleConsumptionAndCapacity();
            ValidateSemanticViewBuffer();
            var catalog = Compile();
            ValidatePersistenceExclusion(catalog);
            ValidateConsumerIndependence(catalog);
            ValidateAuthoritativeStateShape();
            CombatImpactFeedbackSmoke.Run();
            Debug.Log("FRUIT_DEFENSE_PRESENTATION_BOUNDARY_OK");
        }

        private static void ValidateOrderedSingleConsumptionAndCapacity()
        {
            var stream = new BattlePresentationEventStream(2);
            stream.EmitAbilityStarted(1, BattleContentIds.Abilities.PeaAttack,
                10, 20, Vector2.zero, Vector2.right);
            var second = stream.EmitProjectileLaunched(2,
                BattleContentIds.Abilities.PeaAttack, BattleContentIds.Projectiles.Pea,
                10, 20, Vector2.one, Vector2.right);
            var third = stream.EmitDamageResolved(3,
                BattleContentIds.Abilities.PeaAttack, BattleContentIds.Projectiles.Pea,
                BattleContentIds.Plants.Pea, BattleContentIds.Enemies.Normal,
                10, 20, Vector2.right, Vector2.right, 10f, false);
            Assert(stream.PendingCount == 2 && stream.DroppedCount == 1,
                "bounded stream drops the oldest transient event");

            var drained = new List<BattlePresentationEvent>();
            Assert(stream.DrainTo(drained) == 2 && drained.Count == 2
                && drained[0] == second && drained[1] == third
                && drained[0].Sequence < drained[1].Sequence,
                "retained semantic events drain once in sequence order");
            Assert(stream.DrainTo(new List<BattlePresentationEvent>()) == 0,
                "second drain observes no already-consumed event");

            stream.EmitBattleStateChanged(4,
                BattleContentIds.BattleStates.MilestoneReward, Vector2.zero);
            stream.DiscardPending();
            var afterDiscard = stream.EmitResourceGranted(5,
                BattleContentIds.Abilities.SunflowerProduce,
                BattleContentIds.Resources.Sun, 30, 0, Vector2.zero, 1f);
            drained.Clear();
            stream.DrainTo(drained);
            Assert(drained.Count == 1 && drained[0] == afterDiscard
                && afterDiscard.Sequence == 5,
                "discard removes pending events without rewinding delivery order");

            stream.Reset();
            var afterReset = stream.EmitAbilityStarted(0,
                BattleContentIds.Abilities.PeaAttack, 1, 0,
                Vector2.zero, Vector2.zero);
            Assert(afterReset.Sequence == 1 && stream.DroppedCount == 0,
                "stream reset starts a new local delivery sequence");
        }

        private static void ValidateSemanticViewBuffer()
        {
            var stream = new BattlePresentationEventStream();
            stream.EmitAbilityReleased(8, BattleContentIds.Abilities.DurianAttack,
                1, 2, Vector2.zero, Vector2.right);
            stream.EmitDamageResolved(8, BattleContentIds.Abilities.DurianAttack,
                string.Empty, BattleContentIds.Plants.Durian,
                BattleContentIds.Enemies.Normal, 1, 2, Vector2.zero,
                Vector2.right, 25f, false);
            var events = new List<BattlePresentationEvent>();
            stream.DrainTo(events);
            var buffer = new BattlePresentationBuffer();
            buffer.Consume(events);
            Assert(buffer.CombatEffects.Count == 1
                && buffer.CombatEffects[0].Kind == PresentationVfxKind.DurianImpact,
                "view resolves heavy durian policy from its gameplay identity");
            Assert(buffer.Feedback.Count == 1
                && Mathf.Approximately(buffer.Feedback[0].Magnitude, 25f)
                && buffer.Reactions.Any(value => value.EntityId == 2),
                "view creates local floating text and target reaction from damage");

            buffer.Advance(2f, false, 1);
            Assert(buffer.CombatEffects.Count == 0 && buffer.Feedback.Count == 0
                && buffer.Reactions.Count == 0,
                "view-local lifetime expires without a simulation step");
        }

        private static void ValidatePersistenceExclusion(
            CompiledBattleContentCatalog catalog)
        {
            var levelCatalog = BundledLevelCatalogFactory.CreateCompiled();
            var simulation = CreateDurianScenario(catalog, 7002, levelCatalog);
            AdvanceUntilPresentation(simulation, 30);
            Assert(simulation.PendingPresentationEventCount > 0,
                "snapshot fixture has pending semantic events");
            var checksum = simulation.OutcomeStateChecksum();
            var export = simulation.ExportSnapshot();
            Assert(export.Succeeded, "catalog-resolved Standard session exports a snapshot");
            var json = BattleSnapshotJson.Serialize(export.Snapshot);
            Assert(json.IndexOf("presentation", StringComparison.OrdinalIgnoreCase) < 0
                && json.IndexOf("feedback", StringComparison.OrdinalIgnoreCase) < 0
                && json.IndexOf("reaction", StringComparison.OrdinalIgnoreCase) < 0
                && json.IndexOf("\"profileId\":",
                    StringComparison.OrdinalIgnoreCase) < 0,
                "snapshot JSON excludes presentation event, profile, and reaction state");

            simulation.DiscardPendingPresentationEvents();
            Assert(simulation.OutcomeStateChecksum() == checksum,
                "discarding pending events does not change outcome checksum");

            var snapshot = export.Snapshot;
            AdvanceUntilPresentation(simulation, 50);
            var result = simulation.RestoreSnapshot(snapshot, levelCatalog);
            Assert(result.Succeeded && simulation.PendingPresentationEventCount == 0,
                "successful restore clears transient delivery state");
        }

        private static void ValidateConsumerIndependence(
            CompiledBattleContentCatalog catalog)
        {
            var consumed = CreateDurianScenario(catalog, 7003);
            var headless = CreateDurianScenario(catalog, 7003);
            var scratch = new List<BattlePresentationEvent>();
            for (var step = 0; step < 80; step++)
            {
                consumed.Step();
                scratch.Clear();
                consumed.DrainPresentationEvents(scratch);
                headless.Step();
            }
            Assert(consumed.OutcomeStateChecksum() == headless.OutcomeStateChecksum()
                && consumed.RandomState == headless.RandomState,
                "drained and headless simulations remain deterministic peers");
        }

        private static void ValidateAuthoritativeStateShape()
        {
            var gameStateNames = typeof(GameState).GetFields()
                .Select(field => field.Name.ToLowerInvariant()).ToArray();
            Assert(gameStateNames.All(name => !name.Contains("cue")
                && !name.Contains("feedback") && !name.Contains("presentation")),
                "authoritative GameState exposes no presentation delivery field");

            var plantNames = typeof(Plant).GetFields()
                .Select(field => field.Name.ToLowerInvariant()).ToArray();
            Assert(plantNames.All(name => name != "facing"
                && !name.Contains("actionstarted") && !name.Contains("actionuntil")),
                "authoritative plants expose no presentation action mirror");

            var projectileNames = typeof(ProjectileFlash).GetFields()
                .Select(field => field.Name.ToLowerInvariant()).ToArray();
            Assert(projectileNames.All(name => !name.Contains("visual")
                && !name.Contains("cue")),
                "authoritative projectiles expose no visual or cue identity");
        }

        private static void AdvanceUntilPresentation(GameSimulation simulation,
            int maximumSteps)
        {
            for (var step = 0; step < maximumSteps
                 && simulation.PendingPresentationEventCount == 0; step++)
                simulation.Step();
        }

        private static GameSimulation CreateDurianScenario(
            CompiledBattleContentCatalog catalog, int seed,
            CompiledLevelCatalog levelCatalog = null)
        {
            var simulation = levelCatalog == null
                ? new GameSimulation(catalog, seed)
                : new GameSimulation(levelCatalog,
                    BundledLevelCatalogIds.Levels.Orchard01, seed,
                    BattleGrowthTestFixture.ResolveBundled(levelCatalog,
                        BundledLevelCatalogIds.Levels.Orchard01));
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = simulation.OrderedWaves[0].enemyIds.Length;
            simulation.State.WaveSpawned = simulation.State.WaveTotal;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                DefinitionId = BattleContentIds.Plants.Durian,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
            });
            simulation.State.Zombies.Add(new Zombie
            {
                Id = 9002,
                DefinitionId = BattleContentIds.Enemies.Normal,
                RouteId = simulation.Map.PrimaryRouteId,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = 0f,
                Reward = 0,
                Threat = 1,
                PathProgress = NearestPathProgress(simulation,
                    simulation.PotPoint(pot)),
            });
            return simulation;
        }

        private static float NearestPathProgress(GameSimulation simulation,
            Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f;
                 progress <= simulation.Map.Route.TotalLength; progress += step)
            {
                var distance = Vector2.SqrMagnitude(
                    simulation.Map.Route.Sample(progress) - point);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestProgress = progress;
            }
            return bestProgress;
        }

        private static CompiledBattleContentCatalog Compile()
        {
            CompiledBattleContentCatalog catalog;
            ContentValidationResult validation;
            if (!BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                    out catalog, out validation))
                throw new InvalidOperationException(string.Join("\n",
                    validation.Issues.Select(issue => issue.ToString()).ToArray()));
            return catalog;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Presentation boundary smoke failed: " + message);
        }
    }
}
