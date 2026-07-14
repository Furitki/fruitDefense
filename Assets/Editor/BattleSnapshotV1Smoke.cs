using System;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleSnapshotV1Smoke
    {
        [MenuItem("Fruit Defense/Validate Battle Snapshot V1")]
        public static void Run()
        {
            var catalog = Compile();
            ValidatePhaseRoundTrips(catalog);
            ValidatePresentationExclusionAndDeepCopy(catalog);
            ValidateProjectileContinuation(catalog);
            ValidateStatusContinuation(catalog);
            ValidateBananaContinuation(catalog);
            ValidateBurstContinuation(catalog);
            ValidateSafeFailures(catalog);
            Debug.Log("FRUIT_DEFENSE_BATTLE_SNAPSHOT_V1_OK");
        }

        private static void ValidatePhaseRoundTrips(CompiledBattleContentCatalog catalog)
        {
            var ready = new GameSimulation(catalog, 10101);
            AssertRoundTrip(ready, catalog, "Ready");

            var playing = CreateScenario(catalog, BattleContentIds.Plants.Pea);
            playing.Step();
            Assert(playing.State.Projectiles.Count > 0, "Playing fixture contains a projectile");
            AssertRoundTrip(playing, catalog, "Playing");

            var between = new GameSimulation(catalog, 30303);
            between.State.Phase = GamePhase.BetweenWaves;
            between.State.WaveIndex = 1;
            between.State.WaveSpawned = catalog.Waves["wave.01"].enemyIds.Length;
            between.State.WaveTotal = between.State.WaveSpawned;
            between.State.BetweenTimer = 7.35f;
            between.State.Elapsed = 12.5f;
            between.State.LogicTick = 250;
            between.AdvanceFrame(.02f);
            var json = between.ExportSnapshotJson(true);
            var beforeRandom = between.RandomState;
            var result = between.RestoreSnapshotJson(json, catalog);
            Assert(result.Succeeded, "BetweenWaves restore succeeds: " + result);
            Assert(between.State.Phase == GamePhase.BetweenWaves, "BetweenWaves phase survives");
            Assert(between.State.LogicTick == 250 && between.RandomState == beforeRandom,
                "logical step and random state survive");
            Assert(Math.Abs(between.FrameAccumulatorSeconds) < .0000001d,
                "successful restore resets frame accumulator");
            AssertRoundTrip(between, catalog, "BetweenWaves");
        }

        private static void ValidatePresentationExclusionAndDeepCopy(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Durian);
            simulation.Step();
            Assert(simulation.PendingPresentationEventCount > 0,
                "fixture contains pending presentation events");
            simulation.State.Plants[0].Facing = Vector2.up;
            simulation.State.Plants[0].ActionStartedAt = 1f;
            simulation.State.Plants[0].ActionUntil = 2f;
            var snapshot = simulation.ExportSnapshot();
            var json = JsonUtility.ToJson(snapshot);
            Assert(!json.Contains(BattleContentIds.Cues.DurianDrop)
                && !json.Contains(BattleContentIds.Visuals.Durian)
                && !json.Contains("presentationEvents") && !json.Contains("combatEffects")
                && !json.Contains("feedback") && !json.Contains("delivery") && !json.Contains("facing")
                && !json.Contains("actionStartedAt") && !json.Contains("actionUntil"),
                "presentation, selection-adjacent action view, cue, and VFX state are excluded");

            var restoreTarget = new GameSimulation(catalog, 1);
            Assert(restoreTarget.PendingPresentationEventCount > 0, "new simulation starts with feedback pending");
            var presentationFreeRestore = restoreTarget.RestoreSnapshot(snapshot, catalog);
            Assert(presentationFreeRestore.Succeeded && restoreTarget.PendingPresentationEventCount == 0,
                "successful restore starts without pending transient presentation");

            var originalSun = simulation.State.Sun;
            var originalPotId = simulation.State.Plants[0].PotId;
            snapshot.sun += 1000;
            snapshot.plants[0].potEntityId = -1;
            Assert(simulation.State.Sun == originalSun && simulation.State.Plants[0].PotId == originalPotId,
                "exported DTO is a deep copy");

            var refreshed = new GameSimulation(catalog, 40404);
            string reason;
            Assert(refreshed.RefreshNursery(out reason), "Ready fixture refreshes nursery");
            var refreshedRestore = new GameSimulation(catalog, 1).RestoreSnapshotJson(refreshed.ExportSnapshotJson(), catalog);
            Assert(refreshedRestore.Succeeded, "legacy plant definitions are canonicalized on export");
        }

        private static void ValidateProjectileContinuation(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Pea);
            simulation.Step();
            Assert(simulation.State.Projectiles.Any(projectile => projectile.Mode == BattleProjectileMode.Tracking),
                "tracking projectile pending at branch point");
            AssertContinuation(simulation, catalog, 45, "tracking projectile");
        }

        private static void ValidateStatusContinuation(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Durian, BattleContentIds.Equipment.Ice);
            var enemy = simulation.State.Zombies[0];
            enemy.Statuses.Add(new StatusInstance
            {
                DefinitionId = BattleContentIds.Statuses.ChiliBurn,
                SourceEntityId = simulation.State.Plants[0].Id,
                RemainingTicks = 37,
                StackCount = 1,
                Magnitude = 2.4f,
                Sequence = 1,
            });
            enemy.Statuses.Add(new StatusInstance
            {
                DefinitionId = BattleContentIds.Statuses.IceSlow,
                SourceEntityId = simulation.State.Plants[0].Id,
                RemainingTicks = 24,
                StackCount = 1,
                Magnitude = .55f,
                Sequence = 2,
            });
            enemy.Statuses.Add(new StatusInstance
            {
                DefinitionId = BattleContentIds.Statuses.IceCount,
                SourceEntityId = simulation.State.Plants[0].Id,
                RemainingTicks = 1999980,
                StackCount = 4,
                Magnitude = 1f,
                Sequence = 3,
            });
            simulation.State.NextStatusSequence = 4;
            AssertContinuation(simulation, catalog, 30, "burn, slow, and fourth ice hit");
        }

        private static void ValidateBananaContinuation(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Banana);
            simulation.Step();
            simulation.State.Plants[0].AttackCooldown = 999f;
            for (var index = 0; index < 60 && simulation.State.Projectiles.Count > 0
                && !simulation.State.Projectiles[0].Returning; index++) simulation.Step();
            Assert(simulation.State.Projectiles.Count == 1 && simulation.State.Projectiles[0].Returning,
                "banana is returning at branch point");
            Assert(simulation.State.Projectiles[0].HitIds.Count > 0,
                "banana hit history is present at branch point");
            AssertContinuation(simulation, catalog, 70, "banana return and hit history");
        }

        private static void ValidateBurstContinuation(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Pea, BattleContentIds.Equipment.Gatling);
            simulation.Step();
            var runtime = simulation.State.Plants[0].SkillRuntimes
                .Single(value => value.SkillId == BattleContentIds.Skills.PeaAttack);
            Assert(runtime.BurstShotsRemaining == 3 && runtime.BurstIntervalTicks > 0,
                "machine-gun burst is pending at branch point");
            AssertContinuation(simulation, catalog, 40, "machine-gun four-shot burst");
        }

        private static void ValidateSafeFailures(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog, BattleContentIds.Plants.Pea);
            simulation.Step();
            var json = simulation.ExportSnapshotJson();
            var checksum = simulation.OutcomeStateChecksum();
            var state = simulation.State;
            var random = simulation.RandomState;

            AssertFailure(simulation, JsonUtility.FromJson<BattleSnapshotV1>(json), null,
                BattleSnapshotRestoreCode.ContentUnavailable, state, checksum, random, "unavailable content");

            var unsupported = JsonUtility.FromJson<BattleSnapshotV1>(json);
            unsupported.schemaVersion = 2;
            AssertFailure(simulation, unsupported, catalog, BattleSnapshotRestoreCode.UnsupportedSchema,
                state, checksum, random, "unsupported schema");

            var incompatible = JsonUtility.FromJson<BattleSnapshotV1>(json);
            incompatible.contentVersion = "99.0.0";
            AssertFailure(simulation, incompatible, catalog, BattleSnapshotRestoreCode.IncompatibleContent,
                state, checksum, random, "content mismatch");

            var wrongMap = JsonUtility.FromJson<BattleSnapshotV1>(json);
            wrongMap.mapId = "another-map";
            AssertFailure(simulation, wrongMap, catalog, BattleSnapshotRestoreCode.IncompatibleMap,
                state, checksum, random, "map mismatch");

            var duplicate = JsonUtility.FromJson<BattleSnapshotV1>(json);
            duplicate.plants[0].entityId = duplicate.pots[0].entityId;
            AssertFailure(simulation, duplicate, catalog, BattleSnapshotRestoreCode.InvalidIdentity,
                state, checksum, random, "duplicate entity ID");

            var definition = JsonUtility.FromJson<BattleSnapshotV1>(json);
            definition.plants[0].definitionId = "plant.missing";
            AssertFailure(simulation, definition, catalog, BattleSnapshotRestoreCode.UnknownDefinition,
                state, checksum, random, "missing definition");

            var reference = JsonUtility.FromJson<BattleSnapshotV1>(json);
            reference.plants[0].potEntityId = 987654;
            AssertFailure(simulation, reference, catalog, BattleSnapshotRestoreCode.InvalidReference,
                state, checksum, random, "invalid reference");

            var numeric = JsonUtility.FromJson<BattleSnapshotV1>(json);
            numeric.enemies[0].hp = float.NaN;
            AssertFailure(simulation, numeric, catalog, BattleSnapshotRestoreCode.InvalidNumericValue,
                state, checksum, random, "non-finite number");

            var nextId = JsonUtility.FromJson<BattleSnapshotV1>(json);
            nextId.nextEntityId = nextId.enemies[0].entityId;
            AssertFailure(simulation, nextId, catalog, BattleSnapshotRestoreCode.InvalidIdentity,
                state, checksum, random, "invalid next entity ID");

            var invalidJsonResult = simulation.RestoreSnapshotJson("{not-json", catalog);
            Assert(invalidJsonResult.Code == BattleSnapshotRestoreCode.InvalidPayload,
                "invalid JSON returns structured payload failure");
            Assert(ReferenceEquals(state, simulation.State) && checksum == simulation.OutcomeStateChecksum()
                && random == simulation.RandomState, "invalid JSON leaves live simulation unchanged");
        }

        private static void AssertRoundTrip(GameSimulation source, CompiledBattleContentCatalog catalog, string label)
        {
            var checksum = source.OutcomeStateChecksum();
            var random = source.RandomState;
            var json = source.ExportSnapshotJson(true);
            var restored = new GameSimulation(catalog, 999999);
            restored.AdvanceFrame(.01f);
            var result = restored.RestoreSnapshotJson(json, catalog);
            Assert(result.Succeeded, label + " JSON restore succeeds: " + result);
            Assert(checksum == restored.OutcomeStateChecksum(), label + " checksum survives JSON round trip");
            Assert(random == restored.RandomState, label + " random state survives JSON round trip");
            Assert(Math.Abs(restored.FrameAccumulatorSeconds) < .0000001d,
                label + " restore clears frame accumulator");
        }

        private static void AssertContinuation(GameSimulation uninterrupted, CompiledBattleContentCatalog catalog,
            int steps, string label)
        {
            var json = uninterrupted.ExportSnapshotJson();
            var restored = new GameSimulation(catalog, 42);
            var result = restored.RestoreSnapshotJson(json, catalog);
            Assert(result.Succeeded, label + " branch restore succeeds: " + result);
            for (var index = 0; index < steps; index++)
            {
                uninterrupted.Step();
                restored.Step();
            }
            Assert(uninterrupted.OutcomeStateChecksum() == restored.OutcomeStateChecksum(),
                label + " continuation checksum matches after " + steps + " fixed steps");
        }

        private static void AssertFailure(GameSimulation simulation, BattleSnapshotV1 snapshot,
            CompiledBattleContentCatalog availableContent, BattleSnapshotRestoreCode expected,
            GameState originalState, string originalChecksum, uint originalRandom, string label)
        {
            var result = simulation.RestoreSnapshot(snapshot, availableContent);
            Assert(result.Code == expected, label + " returns " + expected + ", got " + result);
            Assert(ReferenceEquals(originalState, simulation.State), label + " preserves active GameState identity");
            Assert(originalChecksum == simulation.OutcomeStateChecksum(), label + " preserves outcome state");
            Assert(originalRandom == simulation.RandomState, label + " preserves random state");
        }

        private static GameSimulation CreateScenario(CompiledBattleContentCatalog catalog, string plantId,
            string equipmentId = "")
        {
            var simulation = new GameSimulation(catalog, 7777);
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = 1;
            simulation.State.WaveSpawned = 1;
            simulation.State.SpawnCooldown = 0f;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            PlantKind kind;
            if (!LegacyBattleContentIds.TryPlantKindFromId(plantId, out kind)) kind = PlantKind.Pea;
            var weapon = WeaponKind.None;
            if (equipmentId == BattleContentIds.Equipment.Gatling) weapon = WeaponKind.Gatling;
            else if (equipmentId == BattleContentIds.Equipment.Ice) weapon = WeaponKind.Ice;
            else if (equipmentId == BattleContentIds.Equipment.Chili) weapon = WeaponKind.Chili;
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                ContentId = plantId,
                EquipmentId = equipmentId,
                Kind = kind,
                Weapon = weapon,
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
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
                Reward = 0,
                Threat = 1,
            });
            return simulation;
        }

        private static float NearestPathProgress(GameSimulation simulation, Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f; progress < simulation.Map.Route.TotalLength; progress += step)
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
            CompiledBattleContentCatalog compiled;
            ContentValidationResult validation;
            if (BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(), out compiled, out validation))
                return compiled;
            throw new InvalidOperationException("Catalog compile failed:\n"
                + string.Join("\n", validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Battle snapshot V1 validation failed: " + message);
        }
    }
}
