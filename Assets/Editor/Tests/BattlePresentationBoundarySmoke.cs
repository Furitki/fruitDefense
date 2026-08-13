using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Presentation;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattlePresentationBoundarySmoke
    {
        public static void Run()
        {
            ValidateOrderedSingleConsumptionAndCapacity();
            var catalog = Compile();
            ValidateStableCueAndViewBuffer(catalog);
            ValidatePersistenceExclusion(catalog);
            ValidateConsumerIndependence(catalog);
            ValidateAuthoritativeStateShape();
            Debug.Log("FRUIT_DEFENSE_PRESENTATION_BOUNDARY_OK");
        }

        private static void ValidateOrderedSingleConsumptionAndCapacity()
        {
            var stream = new BattlePresentationEventStream(2);
            stream.EmitFeedback(1, "first", Vector2.zero, Color.white, 1f);
            var second = stream.EmitCue(2, BattleContentIds.Cues.PeaImpact,
                BattleContentIds.Visuals.Pea, 10, 20, Vector2.one,
                true, CombatEffectKind.PeaImpact, .3f);
            var third = stream.EmitFeedback(3, "third", Vector2.right, Color.yellow, 1.8f);
            Assert(stream.PendingCount == 2 && stream.DroppedCount == 1,
                "bounded stream drops the oldest transient event");

            var drained = new List<BattlePresentationEvent>();
            Assert(stream.DrainTo(drained) == 2 && drained.Count == 2
                && drained[0] == second && drained[1] == third
                && drained[0].Sequence < drained[1].Sequence,
                "retained events drain once in sequence order");
            Assert(stream.DrainTo(new List<BattlePresentationEvent>()) == 0,
                "second drain observes no already-consumed event");

            stream.EmitFeedback(4, "discard", Vector2.zero, Color.white, 1f);
            stream.DiscardPending();
            var afterDiscard = stream.EmitFeedback(5, "after", Vector2.zero, Color.white, 1f);
            drained.Clear();
            stream.DrainTo(drained);
            Assert(drained.Count == 1 && drained[0] == afterDiscard && afterDiscard.Sequence == 5,
                "discard removes pending events without rewinding delivery order");

            stream.Reset();
            var afterReset = stream.EmitFeedback(0, "reset", Vector2.zero, Color.white, 1f);
            Assert(afterReset.Sequence == 1 && stream.DroppedCount == 0,
                "stream reset starts a new local delivery sequence");
        }

        private static void ValidateStableCueAndViewBuffer(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateDurianScenario(catalog, 7001);
            simulation.Step();
            var checksum = simulation.OutcomeStateChecksum();
            var random = simulation.RandomState;
            var buffer = new BattlePresentationBuffer();
            var consumed = buffer.Consume(simulation);
            Assert(consumed >= 2 && buffer.CombatEffects.Count == 1,
                "view buffer consumes cue and damage feedback");
            var effect = buffer.CombatEffects[0];
            Assert(effect.CueId == BattleContentIds.Cues.DurianDrop
                && effect.VisualId == BattleContentIds.Visuals.Durian
                && effect.Kind == CombatEffectKind.DurianDrop
                && Mathf.Approximately(effect.Duration, .7f),
                "cue retains stable asset IDs and legacy visual mapping");
            Assert(simulation.OutcomeStateChecksum() == checksum && simulation.RandomState == random,
                "presentation consumption cannot change battle state or random state");

            buffer.Advance(2f);
            Assert(buffer.CombatEffects.Count == 0 && buffer.Feedback.Count == 0,
                "view-local lifetime expires without a simulation step");
        }

        private static void ValidatePersistenceExclusion(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateDurianScenario(catalog, 7002);
            simulation.Step();
            Assert(simulation.PendingPresentationEventCount > 0, "snapshot fixture has pending events");
            var checksum = simulation.OutcomeStateChecksum();
            var json = simulation.ExportSnapshotJson();
            Assert(!json.Contains(BattleContentIds.Cues.DurianDrop)
                && !json.Contains(BattleContentIds.Visuals.Durian)
                && json.IndexOf("presentation", StringComparison.OrdinalIgnoreCase) < 0
                && json.IndexOf("delivery", StringComparison.OrdinalIgnoreCase) < 0,
                "snapshot JSON excludes presentation payload and delivery cursor");

            simulation.DiscardPendingPresentationEvents();
            Assert(simulation.OutcomeStateChecksum() == checksum,
                "discarding pending events does not change outcome checksum");

            simulation.State.Plants[0].SkillRuntimes.Clear();
            simulation.State.Plants[0].AttackCooldown = 0f;
            simulation.Step();
            var snapshot = simulation.ExportSnapshot();
            Assert(simulation.PendingPresentationEventCount > 0, "restore fixture replenishes transient events");
            var result = simulation.RestoreSnapshot(snapshot, catalog);
            Assert(result.Succeeded && simulation.PendingPresentationEventCount == 0,
                "successful restore clears transient delivery state");
        }

        private static void ValidateConsumerIndependence(CompiledBattleContentCatalog catalog)
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
            var names = typeof(GameState).GetFields()
                .Select(field => field.Name.ToLowerInvariant()).ToArray();
            Assert(names.All(name => !name.Contains("cue") && !name.Contains("effect")
                && !name.Contains("feedback") && !name.Contains("presentation")),
                "authoritative GameState exposes no presentation delivery field");
        }

        private static GameSimulation CreateDurianScenario(CompiledBattleContentCatalog catalog, int seed)
        {
            var simulation = new GameSimulation(catalog, seed);
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = 1;
            simulation.State.WaveSpawned = 1;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                ContentId = BattleContentIds.Plants.Durian,
                Kind = PlantKind.Durian,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
            });
            simulation.State.Zombies.Add(new Zombie
            {
                Id = 9002,
                ContentId = BattleContentIds.Enemies.Normal,
                Kind = ZombieKind.Normal,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = 0f,
                Reward = 0,
                Threat = 1,
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
            });
            return simulation;
        }

        private static float NearestPathProgress(GameSimulation simulation, Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f; progress <= simulation.Map.Route.TotalLength; progress += step)
            {
                var distance = Vector2.SqrMagnitude(simulation.Map.Route.Sample(progress) - point);
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
            if (!BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(), out catalog, out validation))
                throw new InvalidOperationException(string.Join("\n", validation.Issues.Select(issue => issue.ToString()).ToArray()));
            return catalog;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Presentation boundary smoke failed: " + message);
        }
    }
}
