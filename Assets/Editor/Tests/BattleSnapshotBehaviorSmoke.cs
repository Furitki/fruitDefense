using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Development.GmStress;
using UnityEngine;

namespace FruitDefense.Editor
{
    internal static class BattleSnapshotBehaviorSmoke
    {
        public static void Validate(CompiledLevelCatalog catalog)
        {
            ValidateReadyAndBetweenWaves(catalog);
            ValidateCompleteRuntimeContinuation(catalog);
            ValidateStatusOwnershipAndDerivedRoute(catalog);
            ValidateEscapedEnemyContinuation(catalog);
            ValidatePresentationEventBoundary(catalog);
            ValidateWaveStateFailures(catalog);
            ValidateMutationFreeCandidateFailures(catalog);
            ValidatePendingReferenceCanonicalization(catalog);
            ValidateUnsupportedConstructionPaths(catalog);
        }

        private static void ValidateReadyAndBetweenWaves(CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var ready = new GameSimulation(catalog, levelId, 8101);
            RoundTripAndContinue(ready, catalog, levelId, 5, "Ready");

            var between = new GameSimulation(catalog, levelId, 8102);
            between.State.Phase = GamePhase.BetweenWaves;
            between.State.WaveIndex = 1;
            between.State.WaveTotal = 5;
            between.State.WaveSpawned = 5;
            between.State.BetweenTimer = 1.25f;
            RoundTripAndContinue(between, catalog, levelId, 40, "BetweenWaves");
        }

        private static void ValidateStatusOwnershipAndDerivedRoute(
            CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8201);
            source.Step();
            var enemy = source.State.Zombies.Single();
            enemy.Statuses.Add(new StatusInstance
            {
                DefinitionId = BattleContentIds.Statuses.IceSlow,
                SourceEntityId = source.State.Plants.Single().Id,
                RemainingTicks = 8,
                StackCount = 1,
                Magnitude = .5f,
                Sequence = source.State.NextStatusSequence++,
                TickProgress = 0,
            });
            var export = source.ExportSnapshot();
            BattleSnapshotSmoke.Assert(export.Succeeded,
                "status branch exports successfully");
            var snapshot = export.Snapshot;
            BattleSnapshotSmoke.Assert(typeof(BattleSnapshotEnemy).GetField("statuses") == null
                && typeof(BattleSnapshotEnemy).GetField("routeId") == null
                && snapshot.combatRuntime.entities.Single(value => value.entityId == enemy.Id)
                    .statuses.Single().definitionId == BattleContentIds.Statuses.IceSlow,
                "entity runtime is the sole status owner and enemy route is not serialized");

            var target = new GameSimulation(catalog, levelId, 999);
            var result = target.RestoreSnapshot(BattleSnapshotSmoke.Clone(snapshot), catalog);
            BattleSnapshotSmoke.Assert(result.Succeeded
                && target.State.Zombies.Single().RouteId == target.Map.PrimaryRouteId
                && target.State.Zombies.Single().Statuses.Single().DefinitionId
                    == BattleContentIds.Statuses.IceSlow,
                "restore derives the Standard route and restores status runtime once");
            for (var step = 0; step < 15; step++)
            {
                source.Step();
                target.Step();
            }
            BattleSnapshotSmoke.Assert(source.OutcomeStateChecksum()
                == target.OutcomeStateChecksum(),
                "status and Ability runtime continue deterministically");
        }

        private static void ValidateCompleteRuntimeContinuation(
            CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8151,
                BattleContentIds.Plants.Pea, BattleContentIds.Equipment.Gatling);
            var occupiedPot = source.State.Plants.Single().PotId;
            var sparePots = source.State.Pots.Where(value => value.Active
                    && value.Id != occupiedPot)
                .OrderBy(value => value.Id).Take(2).ToArray();
            BattleSnapshotSmoke.Assert(sparePots.Length == 2,
                "runtime fixture has pots for periodic and recovery owners");
            source.State.Plants.Add(new Plant
            {
                Id = 9003,
                DefinitionId = BattleContentIds.Plants.Sunflower,
                Star = 1,
                PotId = sparePots[0].Id,
                NurseryIndex = -1,
            });
            source.State.Plants.Add(new Plant
            {
                Id = 9004,
                DefinitionId = BattleContentIds.Plants.Durian,
                Star = 1,
                PotId = sparePots[1].Id,
                NurseryIndex = -1,
            });
            source.Step();
            BattleSnapshotSmoke.Assert(source.State.Projectiles.Count > 0,
                "runtime branch has an active projectile");

            var pea = source.State.Plants.Single(value => value.Id == 9001)
                .AbilityRuntimes.Single(value =>
                    value.AbilityId == BattleContentIds.Abilities.PeaAttack);
            BattleSnapshotSmoke.Assert(pea.BurstShotsRemaining > 0
                    && pea.BurstIntervalTicks > 0,
                "runtime branch has a pending Gatling burst");
            source.State.NextCombatEventSequence = Math.Max(
                source.State.NextCombatEventSequence, 10L);
            pea.PendingEventMagnitude = 3.25f;
            pea.PendingRootEventSequence = 8L;
            pea.LastRootEventSequence = 7L;

            var sunflower = source.State.Plants.Single(value => value.Id == 9003)
                .AbilityRuntimes.Single(value =>
                    value.AbilityId == BattleContentIds.Abilities.SunflowerProduce);
            sunflower.PeriodicProgressTicks = 17;

            var durian = source.State.Plants.Single(value => value.Id == 9004)
                .AbilityRuntimes.Single(value =>
                    value.AbilityId == BattleContentIds.Abilities.DurianAttack);
            durian.Phase = AbilityRuntimePhase.Recovery;
            durian.WindupTicksRemaining = 0;
            durian.RecoveryTicksRemaining = 5;
            durian.BurstShotsRemaining = 0;
            durian.BurstIntervalTicks = 0;
            durian.PendingSourceEntityId = 0;
            durian.PendingTargetEntityId = 0;
            durian.PendingEventMagnitude = 0f;
            durian.PendingRootEventSequence = 0L;

            var export = source.ExportSnapshot();
            BattleSnapshotSmoke.Assert(export.Succeeded,
                "complete runtime branch exports successfully");
            var snapshot = export.Snapshot;
            var peaSaved = Runtime(snapshot, 9001, BattleContentIds.Abilities.PeaAttack);
            var sunflowerSaved = Runtime(snapshot, 9003,
                BattleContentIds.Abilities.SunflowerProduce);
            var durianSaved = Runtime(snapshot, 9004,
                BattleContentIds.Abilities.DurianAttack);
            BattleSnapshotSmoke.Assert(snapshot.projectiles.Length > 0
                    && peaSaved.cooldownTicks > 0
                    && peaSaved.burstShotsRemaining > 0
                    && peaSaved.burstIntervalTicks > 0
                    && peaSaved.pendingSourceEntityId == 9001
                    && peaSaved.pendingTargetEntityId == 9002
                    && Math.Abs(peaSaved.pendingEventMagnitude - 3.25f) < .0001f
                    && peaSaved.pendingRootEventSequence == 8L
                    && peaSaved.lastRootEventSequence == 7L
                    && sunflowerSaved.periodicProgressTicks == 17
                    && durianSaved.phase == (int)AbilityRuntimePhase.Recovery
                    && durianSaved.recoveryTicksRemaining == 5,
                "snapshot owns projectile and every active Ability runtime counter");

            var json = BattleSnapshotJson.Serialize(snapshot);
            var read = BattleSnapshotJson.Deserialize(json, out var decoded);
            BattleSnapshotSmoke.Assert(read.Succeeded,
                "complete runtime JSON passes the structural gate");
            var target = new GameSimulation(catalog, levelId, 8152);
            var result = target.RestoreSnapshot(decoded, catalog);
            BattleSnapshotSmoke.Assert(result.Succeeded
                    && target.OutcomeStateChecksum() == source.OutcomeStateChecksum(),
                "complete runtime branch restores exactly: " + result);
            for (var step = 0; step < 45; step++)
            {
                source.Step();
                target.Step();
            }
            BattleSnapshotSmoke.Assert(target.OutcomeStateChecksum()
                    == source.OutcomeStateChecksum(),
                "projectile, periodic, recovery, burst, and root context continue deterministically");
        }

        private static void ValidateEscapedEnemyContinuation(CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8301);
            source.State.Plants.Clear();
            var enemy = source.State.Zombies.Single();
            enemy.Speed = source.Map.RouteLength(source.Map.PrimaryRouteId);
            enemy.PathProgress = source.Map.RouteLength(source.Map.PrimaryRouteId) - .001f;
            var lives = source.State.Lives;
            source.Step();
            BattleSnapshotSmoke.Assert(source.State.Zombies.Count == 0
                && source.EscapedEnemyCount == 1 && source.State.Lives == lives - 1,
                "enemy crosses the resolved goal before export");

            var export = source.ExportSnapshot();
            BattleSnapshotSmoke.Assert(export.Succeeded
                && export.Snapshot.escapedEnemyCount == 1,
                "escaped enemy count is exported");
            var target = new GameSimulation(catalog, levelId, 8302);
            var result = target.RestoreSnapshot(export.Snapshot, catalog);
            BattleSnapshotSmoke.Assert(result.Succeeded
                && target.EscapedEnemyCount == 1
                && target.OutcomeStateChecksum() == source.OutcomeStateChecksum(),
                "escaped count and outcome checksum round-trip");
            for (var step = 0; step < 30; step++)
            {
                source.Step();
                target.Step();
            }
            BattleSnapshotSmoke.Assert(target.OutcomeStateChecksum()
                == source.OutcomeStateChecksum(),
                "post-escape continuation remains deterministic");

            using (var gm = GmStressBattleFactory.Create(8303))
            {
                var gmSimulation = gm.Simulation;
                var routeId = gmSimulation.Map.RouteIds[0];
                var gmLives = gmSimulation.State.Lives;
                gmSimulation.State.Zombies.Add(new Zombie
                {
                    Id = gmSimulation.State.NextId++,
                    DefinitionId = BattleContentIds.Enemies.Normal,
                    RouteId = routeId,
                    Hp = 1f,
                    MaxHp = 1f,
                    Speed = gmSimulation.Map.RouteLength(routeId),
                    PathProgress = gmSimulation.Map.RouteLength(routeId) - .001f,
                    Reward = 0,
                    Threat = 1,
                });
                gmSimulation.Step();
                BattleSnapshotSmoke.Assert(gmSimulation.State.Zombies.Count == 0
                    && gmSimulation.EscapedEnemyCount == 1
                    && gmSimulation.State.Lives == gmLives,
                    "GM escape increments exactly once without applying Standard life loss");
            }
        }

        private static void ValidatePresentationEventBoundary(CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = new GameSimulation(catalog, levelId, 8401);
            var export = source.ExportSnapshot();
            BattleSnapshotSmoke.Assert(export.Succeeded, "event boundary source exports");

            var successTarget = new GameSimulation(catalog, levelId, 8402);
            var successStream = PresentationStream(successTarget);
            successStream.EmitBattleStateChanged(1, "pre-restore", Vector2.zero);
            var result = successTarget.RestoreSnapshot(export.Snapshot, catalog);
            BattleSnapshotSmoke.Assert(result.Succeeded
                && successTarget.PendingPresentationEventCount == 0
                && successTarget.DroppedPresentationEventCount == 0
                && successStream.LastIssuedSequence == 0,
                "successful restore resets pending, sequence, and drop history without an event");

            var failureTarget = new GameSimulation(catalog, levelId, 8403);
            var failureStream = PresentationStream(failureTarget);
            failureTarget.DiscardPendingPresentationEvents();
            var initialIssued = failureStream.LastIssuedSequence;
            var emitted = BattlePresentationEventStream.DefaultCapacity + 3;
            for (var index = 0; index < emitted; index++)
                failureStream.EmitBattleStateChanged(index, "pending-" + index, Vector2.zero);
            failureTarget.AdvanceFrame(.01f);
            var checksum = failureTarget.OutcomeStateChecksum();
            var activeState = failureTarget.State;
            var randomState = failureTarget.RandomState;
            var indexedEntity = failureTarget.State.Plants.Cast<CombatEntityState>()
                .Concat(failureTarget.State.Zombies).FirstOrDefault();
            var accumulator = failureTarget.FrameAccumulatorSeconds;
            var pending = failureTarget.PendingPresentationEventCount;
            var dropped = failureTarget.DroppedPresentationEventCount;
            var lastIssued = failureStream.LastIssuedSequence;
            var corrupt = BattleSnapshotSmoke.Clone(export.Snapshot);
            corrupt.schemaVersion++;
            result = failureTarget.RestoreSnapshot(corrupt, catalog);
            BattleSnapshotSmoke.Assert(result.Code == BattleSnapshotRestoreCode.UnsupportedSchema
                && ReferenceEquals(failureTarget.State, activeState)
                && (indexedEntity == null
                    || ReferenceEquals(failureTarget.EntityById(indexedEntity.Id), indexedEntity))
                && failureTarget.OutcomeStateChecksum() == checksum
                && failureTarget.RandomState == randomState
                && Math.Abs(failureTarget.FrameAccumulatorSeconds - accumulator) < .0000001
                && failureTarget.PendingPresentationEventCount == pending
                && failureTarget.DroppedPresentationEventCount == dropped
                && failureStream.LastIssuedSequence == lastIssued,
                "failed restore preserves state, accumulator, and event stream counters");

            var drained = new List<BattlePresentationEvent>();
            failureTarget.DrainPresentationEvents(drained);
            var firstOriginalIndex = emitted - BattlePresentationEventStream.DefaultCapacity;
            BattleSnapshotSmoke.Assert(drained.Count == BattlePresentationEventStream.DefaultCapacity
                && drained.First().SemanticId == "pending-" + firstOriginalIndex
                && drained.First().Sequence == initialIssued + firstOriginalIndex + 1
                && drained.Last().SemanticId == "pending-" + (emitted - 1)
                && drained.Last().Sequence == initialIssued + emitted,
                "failed restore preserves pending event content and order");
            var next = failureStream.EmitBattleStateChanged(0, "after-failure", Vector2.zero);
            BattleSnapshotSmoke.Assert(next.Sequence == initialIssued + emitted + 1,
                "failed restore preserves the next presentation sequence");
        }

        private static void ValidateUnsupportedConstructionPaths(CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var resolved = catalog.Resolve(levelId).Value;
            var valid = new GameSimulation(catalog, levelId, 8500).ExportSnapshot().Snapshot;
            var simulations = new List<GameSimulation>
            {
                new GameSimulation(8501),
                new GameSimulation(catalog.BattleContent, 8502),
                new GameSimulation(catalog.BattleContent, 8503, resolved.Map),
                new GameSimulation(resolved, 8504),
            };
            foreach (var simulation in simulations)
                AssertUnsupportedSimulation(simulation, valid, catalog);
            using (var gm = GmStressBattleFactory.Create(8505))
                AssertUnsupportedSimulation(gm.Simulation, valid, catalog);
        }

        private static void ValidateMutationFreeCandidateFailures(
            CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8451,
                BattleContentIds.Plants.Pea);
            source.Step();
            var valid = source.ExportSnapshot().Snapshot;
            BattleSnapshotSmoke.Assert(valid.projectiles.Length > 0,
                "candidate failure fixture has a projectile identity");
            var abilityPath = "combatRuntime.entities[9001].abilities["
                + BattleContentIds.Abilities.PeaAttack + "]";
            var cases = new[]
            {
                new CandidateFailureCase("numeric", BattleSnapshotRestoreCode.InvalidNumericValue,
                    null, value => value.speed = 0),
                new CandidateFailureCase("identity", BattleSnapshotRestoreCode.InvalidIdentity,
                    null, value => value.nextEntityId = 1),
                new CandidateFailureCase("definition", BattleSnapshotRestoreCode.UnknownDefinition,
                    null, value => value.plants[0].definitionId = "plant.missing"),
                new CandidateFailureCase("reference", BattleSnapshotRestoreCode.InvalidReference,
                    null, value => value.plants[0].potEntityId = 999999),
                new CandidateFailureCase("missing pending source",
                    BattleSnapshotRestoreCode.InvalidReference,
                    abilityPath + ".pendingSourceEntityId",
                    value => Runtime(value, 9001, BattleContentIds.Abilities.PeaAttack)
                        .pendingSourceEntityId = 7777),
                new CandidateFailureCase("pot pending target",
                    BattleSnapshotRestoreCode.InvalidReference,
                    abilityPath + ".pendingTargetEntityId",
                    value => Runtime(value, 9001, BattleContentIds.Abilities.PeaAttack)
                        .pendingTargetEntityId = value.pots[0].entityId),
                new CandidateFailureCase("projectile pending target",
                    BattleSnapshotRestoreCode.InvalidReference,
                    abilityPath + ".pendingTargetEntityId",
                    value => Runtime(value, 9001, BattleContentIds.Abilities.PeaAttack)
                        .pendingTargetEntityId = value.projectiles[0].entityId),
            };
            foreach (var failure in cases)
            {
                var corrupt = BattleSnapshotSmoke.Clone(valid);
                failure.Mutate(corrupt);
                var target = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8452);
                AssertMutationFreeRestoreFailure(target, corrupt, catalog,
                    failure.Code, failure.Path, failure.Label);
            }
        }

        private static void ValidateWaveStateFailures(CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8441);
            source.Step();
            var valid = source.ExportSnapshot().Snapshot;
            var cases = new[]
            {
                new CandidateFailureCase("Playing wave zero",
                    BattleSnapshotRestoreCode.InvalidNumericValue, "waveIndex",
                    value => value.waveIndex = 0),
                new CandidateFailureCase("resolved wave size mismatch",
                    BattleSnapshotRestoreCode.InvalidNumericValue, "waveTotal",
                    value => value.waveTotal++),
                new CandidateFailureCase("spawn progress beyond resolved wave",
                    BattleSnapshotRestoreCode.InvalidNumericValue, "waveSpawned",
                    value => value.waveSpawned++),
                new CandidateFailureCase("incomplete BetweenWaves",
                    BattleSnapshotRestoreCode.InvalidNumericValue, "waveSpawned",
                    value =>
                    {
                        value.phase = (int)GamePhase.BetweenWaves;
                        value.waveSpawned--;
                    }),
                new CandidateFailureCase("early Victory",
                    BattleSnapshotRestoreCode.InvalidNumericValue, "waveIndex",
                    value => value.phase = (int)GamePhase.Victory),
            };
            foreach (var failure in cases)
            {
                var corrupt = BattleSnapshotSmoke.Clone(valid);
                failure.Mutate(corrupt);
                AssertMutationFreeRestoreFailure(
                    BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8442), corrupt, catalog,
                    failure.Code, failure.Path, failure.Label);
            }
        }

        private static void ValidatePendingReferenceCanonicalization(
            CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 8461,
                BattleContentIds.Plants.Pea);
            source.Step();
            var runtime = source.State.Plants.Single().AbilityRuntimes.Single(value =>
                value.AbilityId == BattleContentIds.Abilities.PeaAttack);
            runtime.PendingSourceEntityId = source.State.Pots[0].Id;
            runtime.PendingTargetEntityId = source.State.Projectiles.Single().Id;
            var staleChecksum = source.OutcomeStateChecksum();
            var export = source.ExportSnapshot();
            BattleSnapshotSmoke.Assert(export.Succeeded,
                "pending-reference canonicalization fixture exports");
            var saved = Runtime(export.Snapshot, source.State.Plants.Single().Id,
                BattleContentIds.Abilities.PeaAttack);
            BattleSnapshotSmoke.Assert(saved.pendingSourceEntityId == 0
                    && saved.pendingTargetEntityId == 0,
                "export canonicalizes non-combat pending references to zero");
            runtime.PendingSourceEntityId = 0;
            runtime.PendingTargetEntityId = 0;
            BattleSnapshotSmoke.Assert(source.OutcomeStateChecksum() == staleChecksum,
                "checksum uses the same pending-reference canonicalization as export");
        }

        internal static BattleSnapshotRestoreResult AssertMutationFreeRestoreFailure(
            GameSimulation target, BattleSnapshot snapshot,
            CompiledLevelCatalog suppliedCatalog, BattleSnapshotRestoreCode expectedCode,
            string expectedPath, string label)
        {
            target.DiscardPendingPresentationEvents();
            var stream = PresentationStream(target);
            var initialIssued = stream.LastIssuedSequence;
            stream.EmitBattleStateChanged(11, label + "-first", Vector2.zero);
            stream.EmitBattleStateChanged(12, label + "-second", Vector2.one);
            target.AdvanceFrame(.01f);
            var activeState = target.State;
            var indexedEntity = target.State.Plants.Cast<CombatEntityState>()
                .Concat(target.State.Zombies).FirstOrDefault();
            var checksum = target.OutcomeStateChecksum();
            var randomState = target.RandomState;
            var accumulator = target.FrameAccumulatorSeconds;
            var pending = target.PendingPresentationEventCount;
            var dropped = target.DroppedPresentationEventCount;
            var lastIssued = stream.LastIssuedSequence;

            var result = target.RestoreSnapshot(snapshot, suppliedCatalog);
            BattleSnapshotSmoke.Assert(result.Code == expectedCode
                    && (expectedPath == null || result.Path == expectedPath)
                    && ReferenceEquals(target.State, activeState)
                    && (indexedEntity == null
                        || ReferenceEquals(target.EntityById(indexedEntity.Id), indexedEntity))
                    && target.OutcomeStateChecksum() == checksum
                    && target.RandomState == randomState
                    && Math.Abs(target.FrameAccumulatorSeconds - accumulator) < .0000001
                    && target.PendingPresentationEventCount == pending
                    && target.DroppedPresentationEventCount == dropped
                    && stream.LastIssuedSequence == lastIssued,
                label + " rejects without mutating state, indexes, random, accumulator, or events: "
                + result);

            var drained = new List<BattlePresentationEvent>();
            target.DrainPresentationEvents(drained);
            BattleSnapshotSmoke.Assert(drained.Count == 2
                    && drained[0].SemanticId == label + "-first"
                    && drained[0].Sequence == initialIssued + 1
                    && drained[1].SemanticId == label + "-second"
                    && drained[1].Sequence == initialIssued + 2,
                label + " preserves pending event content and order");
            var next = stream.EmitBattleStateChanged(13, label + "-next", Vector2.zero);
            BattleSnapshotSmoke.Assert(next.Sequence == lastIssued + 1,
                label + " preserves the next presentation sequence");
            return result;
        }

        private static void AssertUnsupportedSimulation(GameSimulation simulation,
            BattleSnapshot valid, CompiledLevelCatalog catalog)
        {
            var export = simulation.ExportSnapshot();
            BattleSnapshotSmoke.Assert(!export.Succeeded
                    && export.Code == BattleSnapshotExportCode.UnsupportedSessionSource,
                "unsupported construction path rejects export explicitly");
            AssertMutationFreeRestoreFailure(simulation, valid, catalog,
                BattleSnapshotRestoreCode.UnsupportedSessionSource, "session.source",
                "unsupported construction path");
        }

        private static void RoundTripAndContinue(GameSimulation source,
            CompiledLevelCatalog catalog, string levelId, int steps, string phase)
        {
            var export = source.ExportSnapshot();
            BattleSnapshotSmoke.Assert(export.Succeeded, phase + " export succeeds");
            var json = BattleSnapshotJson.Serialize(export.Snapshot);
            var read = BattleSnapshotJson.Deserialize(json, out var decoded);
            BattleSnapshotSmoke.Assert(read.Succeeded,
                phase + " JSON passes the structural gate: " + read);
            var target = new GameSimulation(catalog, levelId, source.State.RandomSeed + 1);
            var result = target.RestoreSnapshot(decoded, catalog);
            BattleSnapshotSmoke.Assert(result.Succeeded
                && target.State.Phase == source.State.Phase
                && target.OutcomeStateChecksum() == source.OutcomeStateChecksum(),
                phase + " round-trip preserves deterministic state");
            for (var step = 0; step < steps; step++)
            {
                source.Step();
                target.Step();
            }
            BattleSnapshotSmoke.Assert(target.OutcomeStateChecksum()
                == source.OutcomeStateChecksum(), phase + " continuation matches");
        }

        private static BattleSnapshotAbilityRuntime Runtime(BattleSnapshot snapshot,
            int entityId, string abilityId)
        {
            return snapshot.combatRuntime.entities.Single(value => value.entityId == entityId)
                .abilities.Single(value => value.definitionId == abilityId);
        }

        internal static BattlePresentationEventStream PresentationStream(GameSimulation simulation)
        {
            var field = typeof(GameSimulation).GetField("_presentationEvents",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var stream = field == null ? null
                : field.GetValue(simulation) as BattlePresentationEventStream;
            if (stream == null) throw new InvalidOperationException(
                "Battle presentation stream is unavailable for snapshot validation.");
            return stream;
        }

        private sealed class CandidateFailureCase
        {
            public string Label { get; }
            public BattleSnapshotRestoreCode Code { get; }
            public string Path { get; }
            public Action<BattleSnapshot> Mutate { get; }

            public CandidateFailureCase(string label, BattleSnapshotRestoreCode code,
                string path, Action<BattleSnapshot> mutate)
            {
                Label = label;
                Code = code;
                Path = path;
                Mutate = mutate;
            }
        }
    }
}
