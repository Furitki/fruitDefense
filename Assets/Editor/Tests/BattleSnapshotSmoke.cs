using System;
using System.IO;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleSnapshotSmoke
    {
        private const string Marker = "FRUIT_DEFENSE_BATTLE_SNAPSHOT_OK";

        public static void Run()
        {
            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            ValidateRoundTrip(catalog, BundledLevelCatalogIds.Levels.Orchard01);
            ValidateRoundTrip(catalog, BundledLevelCatalogIds.Levels.Orchard02);
            ValidateRoundTrip(catalog, BundledLevelCatalogIds.Levels.Orchard03);
            ValidateRawFixtures(catalog);
            BattleSnapshotBehaviorSmoke.Validate(catalog);
            BattleSnapshotSourceSmoke.Validate(catalog);
            Debug.Log(Marker);
        }

        internal static void ValidateRoundTrip(CompiledLevelCatalog catalog, string levelId)
        {
            var source = CreateScenario(catalog, levelId, 7101);
            source.Step();
            var runtime = source.State.Plants.Single().AbilityRuntimes
                .Single(value => value.AbilityId == BattleContentIds.Abilities.DurianAttack);
            if (runtime.Phase != AbilityRuntimePhase.Windup)
            {
                runtime.Phase = AbilityRuntimePhase.Windup;
                runtime.WindupTicksRemaining = 8;
                runtime.BurstShotsRemaining = 1;
                runtime.CooldownTicks = 36;
                runtime.PendingSourceEntityId = source.State.Plants.Single().Id;
                runtime.PendingTargetEntityId = source.State.Zombies.Single().Id;
            }

            var export = source.ExportSnapshot();
            Assert(export.Succeeded, levelId + " export succeeds: " + export);
            var snapshot = export.Snapshot;
            var entity = snapshot.combatRuntime.entities
                .Single(value => value.entityId == source.State.Plants.Single().Id);
            var saved = entity.abilities.Single(value =>
                value.definitionId == BattleContentIds.Abilities.DurianAttack);
            Assert(snapshot.schemaId == BattleSnapshotSchema.Id
                && snapshot.schemaVersion == BattleSnapshotSchema.Version
                && snapshot.levelId == levelId
                && snapshot.escapedEnemyCount == source.EscapedEnemyCount
                && saved.phase == (int)AbilityRuntimePhase.Windup
                && saved.windupTicksRemaining == 8
                && saved.cooldownTicks > 0,
                levelId + " exports current identity and Ability runtime");

            var json = BattleSnapshotJson.Serialize(snapshot, true);
            Assert(json.IndexOf("presentation", StringComparison.OrdinalIgnoreCase) < 0
                && json.IndexOf("routeId", StringComparison.OrdinalIgnoreCase) < 0
                && json.IndexOf("\"present\"", StringComparison.Ordinal) < 0,
                levelId + " JSON excludes presentation, enemy route, and compatibility sentinel");
            var read = BattleSnapshotJson.Deserialize(json, out var deserialized);
            Assert(read.Succeeded, levelId + " current JSON presence gate succeeds: " + read);

            var target = new GameSimulation(catalog, levelId, 9999,
                source.LaunchGrowthSnapshot);
            var result = target.RestoreSnapshot(deserialized, catalog);
            Assert(result.Succeeded, levelId + " restore succeeds: " + result);
            Assert(target.PendingPresentationEventCount == 0
                && target.OutcomeStateChecksum() == source.OutcomeStateChecksum(),
                levelId + " restore preserves deterministic state and clears presentation");
            for (var index = 0; index < 20; index++)
            {
                source.Step();
                target.Step();
            }
            Assert(target.OutcomeStateChecksum() == source.OutcomeStateChecksum(),
                levelId + " continuation remains deterministic");
        }

        private static void ValidateRawFixtures(CompiledLevelCatalog catalog)
        {
            var root = Path.Combine(Application.dataPath,
                "Editor", "Tests", "Fixtures", "BattleSnapshot");
            var target = new GameSimulation(catalog,
                BundledLevelCatalogIds.Levels.Orchard01, 7201,
                BattleGrowthTestFixture.ResolveBundled(catalog,
                    BundledLevelCatalogIds.Levels.Orchard01));
            target.DiscardPendingPresentationEvents();
            var stream = BattleSnapshotBehaviorSmoke.PresentationStream(target);
            var initialIssued = stream.LastIssuedSequence;
            stream.EmitBattleStateChanged(1, "raw-first", Vector2.zero);
            stream.EmitBattleStateChanged(2, "raw-second", Vector2.one);
            target.AdvanceFrame(.01f);
            var activeState = target.State;
            var checksum = target.OutcomeStateChecksum();
            var randomState = target.RandomState;
            var accumulator = target.FrameAccumulatorSeconds;
            var pending = target.PendingPresentationEventCount;
            var dropped = target.DroppedPresentationEventCount;
            foreach (var path in Directory.GetFiles(root, "*.json")
                .OrderBy(value => value, StringComparer.Ordinal))
            {
                var result = BattleSnapshotJson.Deserialize(File.ReadAllText(path), out var snapshot);
                var name = Path.GetFileName(path);
                var legacy = name.StartsWith("legacy-", StringComparison.Ordinal);
                Assert(!result.Succeeded && snapshot == null
                    && result.Code == (legacy
                        ? BattleSnapshotRestoreCode.UnsupportedSchema
                        : BattleSnapshotRestoreCode.MissingRequiredField),
                    name + " is rejected by the raw structural gate: " + result);
                Assert(ReferenceEquals(target.State, activeState)
                    && target.OutcomeStateChecksum() == checksum
                    && target.RandomState == randomState
                    && Math.Abs(target.FrameAccumulatorSeconds - accumulator) < .0000001
                    && target.PendingPresentationEventCount == pending
                    && target.DroppedPresentationEventCount == dropped,
                    name + " raw rejection cannot mutate an active target");
            }
            var drained = new System.Collections.Generic.List<BattlePresentationEvent>();
            target.DrainPresentationEvents(drained);
            Assert(drained.Count == 2
                    && drained[0].SemanticId == "raw-first"
                    && drained[0].Sequence == initialIssued + 1
                    && drained[1].SemanticId == "raw-second"
                    && drained[1].Sequence == initialIssued + 2,
                "raw rejection preserves pending event content and order");
            var next = stream.EmitBattleStateChanged(3, "raw-next", Vector2.zero);
            Assert(next.Sequence == initialIssued + 3,
                "raw rejection preserves the next presentation sequence");
        }

        internal static GameSimulation CreateScenario(CompiledLevelCatalog catalog,
            string levelId, int seed, string plantId = BattleContentIds.Plants.Durian,
            string equipmentId = "", BattleGrowthSnapshot growthSnapshot = null)
        {
            var simulation = new GameSimulation(catalog, levelId, seed,
                growthSnapshot ?? BattleGrowthTestFixture.ResolveBundled(catalog,
                    levelId));
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = simulation.OrderedWaves[0].enemyIds.Length;
            simulation.State.WaveSpawned = simulation.State.WaveTotal;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots.Where(value => value.Active)
                .OrderBy(value => Vector2.Distance(simulation.PotPoint(value),
                    simulation.Map.Route.Sample(NearestPathProgress(
                        simulation, simulation.PotPoint(value)))))
                .First();
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                DefinitionId = plantId,
                EquipmentId = equipmentId,
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
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
                Reward = 0,
                Threat = 1,
            });
            return simulation;
        }

        internal static float NearestPathProgress(GameSimulation simulation, Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f; progress <= simulation.Map.Route.TotalLength;
                progress += step)
            {
                var distance = Vector2.SqrMagnitude(simulation.Map.Route.Sample(progress) - point);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestProgress = progress;
            }
            return bestProgress;
        }

        internal static BattleSnapshot Clone(BattleSnapshot value)
        {
            return JsonUtility.FromJson<BattleSnapshot>(BattleSnapshotJson.Serialize(value));
        }

        internal static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Battle snapshot validation failed: " + message);
        }
    }
}
