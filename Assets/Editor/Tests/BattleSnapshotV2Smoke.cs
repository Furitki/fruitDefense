using System;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class BattleSnapshotV2Smoke
    {
        public static void Run()
        {
            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            ValidateRoundTrip(catalog, BundledLevelCatalogIds.Levels.Orchard01, 7101);
            ValidateRoundTrip(catalog, BundledLevelCatalogIds.Levels.Orchard02, 7102);
            ValidateRoundTrip(catalog, BundledLevelCatalogIds.Levels.Orchard03, 7103);
            ValidateLegacyMigration(catalog);
            ValidateIdentityAndAtomicFailures(catalog);
            Debug.Log("FRUIT_DEFENSE_BATTLE_SNAPSHOT_V2_OK");
        }

        private static void ValidateRoundTrip(CompiledLevelCatalog catalog, string levelId, int seed)
        {
            var resolved = Resolve(catalog, levelId);
            var source = new GameSimulation(resolved, seed);
            source.State.Sun = 1000;
            Assert(source.RefreshNursery(out _), levelId + " fixture refreshes nursery.");
            var runtimePlant = source.State.Plants.OrderBy(value => value.Id).First();
            runtimePlant.Weapon = WeaponKind.Ice;
            runtimePlant.EquipmentId = BattleContentIds.Equipment.Ice;
            source.ApplyStatus(runtimePlant, BattleContentIds.Statuses.IceSlow, runtimePlant.Id);
            Assert(source.StartWave(out _), levelId + " fixture starts its resolved first wave.");
            for (var index = 0; index < 23; index++) source.Step();
            var pendingPassive = runtimePlant.PassiveRuntimes.OrderBy(value => value.PassiveId).First();
            pendingPassive.CooldownTicks = 7;

            var snapshot = source.ExportSnapshotV2(catalog);
            Assert(snapshot.schemaVersion == BattleSnapshotV2Schema.Version,
                levelId + " exports schema V2.");
            Assert(snapshot.catalogId == catalog.CatalogId
                && snapshot.contentCatalogId == catalog.ContentCatalogId
                && snapshot.contentVersion == catalog.ContentVersion,
                levelId + " exports catalog/content identity.");
            Assert(snapshot.levelId == resolved.Identity.LevelId
                && snapshot.mapId == resolved.Identity.MapId
                && snapshot.gameplayMapFingerprint == resolved.Map.GameplayFingerprint
                && snapshot.waveSetId == resolved.Identity.WaveSetId
                && snapshot.ruleSetId == resolved.Identity.RuleSetId
                && snapshot.themeId == resolved.Identity.ThemeId,
                levelId + " exports its complete composite level identity.");
            Assert(snapshot.pots.Length == source.State.Pots.Count
                && snapshot.plants.Length == source.State.Plants.Count
                && snapshot.enemies.Length == source.State.Zombies.Count
                && snapshot.randomState == source.RandomState,
                levelId + " exports complete gameplay state.");
            var entityRuntime = snapshot.combatRuntime.entities
                .Single(value => value.entityId == runtimePlant.Id);
            Assert(snapshot.combatRuntime.present
                && snapshot.combatRuntime.nextCombatEventSequence == source.State.NextCombatEventSequence
                && entityRuntime.passives.Any(value => value.cooldownTicks == 7)
                && entityRuntime.statuses.Any(value => value.definitionId == BattleContentIds.Statuses.IceSlow),
                levelId + " exports entity-owned passive cooldowns and plant statuses.");

            var json = source.ExportSnapshotV2Json(catalog, true);
            Assert(!json.Contains("presentationEvents") && !json.Contains("delivery")
                && !json.Contains("feedback"),
                levelId + " excludes transient presentation delivery from V2.");
            var target = new GameSimulation(resolved, seed + 999);
            target.AdvanceFrame(.01f);
            Assert(target.PendingPresentationEventCount > 0,
                levelId + " target begins with pending presentation feedback.");
            var result = target.RestoreSnapshotV2Json(json, catalog);
            Assert(result.Succeeded, levelId + " V2 JSON restore succeeds: " + result);
            Assert(target.OutcomeStateChecksum() == source.OutcomeStateChecksum()
                && target.RandomState == source.RandomState,
                levelId + " V2 round trip preserves deterministic gameplay state.");
            Assert(target.PendingPresentationEventCount == 0
                && Math.Abs(target.FrameAccumulatorSeconds) < .0000001d,
                levelId + " successful restore resets transient delivery and frame accumulation.");
            for (var step = 0; step < 8; step++)
            {
                source.Step();
                target.Step();
            }
            Assert(target.OutcomeStateChecksum() == source.OutcomeStateChecksum(),
                levelId + " restored passive cooldown continues on the same fixed ticks.");

            var preFingerprintSnapshot = Clone(snapshot);
            preFingerprintSnapshot.gameplayMapFingerprint = string.Empty;
            var compatibilityTarget = new GameSimulation(resolved, seed + 1000);
            Assert(compatibilityTarget.RestoreSnapshotV2(preFingerprintSnapshot, catalog).Succeeded,
                levelId + " restores supported pre-fingerprint V2 snapshots.");
        }

        private static void ValidateLegacyMigration(CompiledLevelCatalog catalog)
        {
            var orchard01 = Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard01);
            var legacySource = new GameSimulation(catalog.BattleContent, 7201,
                BattlefieldMapDefinition.CreateDefault());
            legacySource.State.Sun = 654;
            var legacy = legacySource.ExportSnapshot();
            Assert(!string.IsNullOrEmpty(legacy.mapId)
                && legacy.mapId == BattlefieldMapDefinition.DefaultMapId,
                "Legacy fixture carries an explicit orchard-01 map identity.");

            var target = new GameSimulation(orchard01, 1);
            var rawV1Target = new GameSimulation(orchard01, 0);
            var rawV1Result = rawV1Target.RestoreSnapshot(Clone(legacy), catalog.BattleContent);
            Assert(rawV1Result.Code == BattleSnapshotRestoreCode.UnsupportedSchema
                && rawV1Result.Path == "schemaVersion",
                "Resolved sessions reject generic V1 restore outside the explicit migration path.");
            var result = target.RestoreLegacySnapshotV1(legacy, catalog);
            Assert(result.Succeeded, "Supported bundled V1 snapshot migrates: " + result);
            var current = target.ExportSnapshotV2(catalog);
            Assert(current.schemaVersion == BattleSnapshotV2Schema.Version
                && current.levelId == BundledLevelCatalogIds.Levels.Orchard01
                && current.mapId == BattlefieldMapDefinition.DefaultMapId,
                "Migrated V1 emits orchard-01 schema V2 on the next export.");

            var missingMap = Clone(legacy);
            missingMap.mapId = string.Empty;
            AssertLegacyFailure(new GameSimulation(orchard01, 2), missingMap, catalog,
                BattleSnapshotRestoreCode.IncompatibleMap, "mapId", "missing legacy map identity");

            var ambiguousMap = Clone(legacy);
            ambiguousMap.mapId = BundledLevelCatalogIds.Maps.Orchard02;
            AssertLegacyFailure(new GameSimulation(orchard01, 3), ambiguousMap, catalog,
                BattleSnapshotRestoreCode.IncompatibleMap, "mapId", "ambiguous legacy map identity");

            var stale = Clone(legacy);
            stale.contentVersion = "legacy.unsupported";
            AssertLegacyFailure(new GameSimulation(orchard01, 4), stale, catalog,
                BattleSnapshotRestoreCode.IncompatibleContent, "contentVersion", "stale legacy content");

            AssertLegacyFailure(new GameSimulation(
                    Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard02), 5),
                Clone(legacy), catalog, BattleSnapshotRestoreCode.IncompatibleLevel,
                "levelId", "legacy snapshot cannot follow current Lobby selection");
        }

        private static void ValidateIdentityAndAtomicFailures(CompiledLevelCatalog catalog)
        {
            var resolved = Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard02);
            var source = new GameSimulation(resolved, 7301);
            source.State.Sun = 932;
            source.StartWave(out _);
            for (var index = 0; index < 12; index++) source.Step();
            var baseline = source.ExportSnapshotV2(catalog);

            AssertV2Failure(resolved, catalog, baseline, value => value.levelId = "orchard-missing",
                BattleSnapshotRestoreCode.UnknownLevel, "levelId", "missing level definition");
            AssertV2Failure(resolved, catalog, baseline, value => value.catalogId = "catalog.levels.stale",
                BattleSnapshotRestoreCode.IncompatibleLevelCatalog, "catalogId", "stale level catalog");
            AssertV2Failure(resolved, catalog, baseline, value => value.contentCatalogId = "catalog.content.stale",
                BattleSnapshotRestoreCode.IncompatibleContent, "contentCatalogId", "content catalog mismatch");
            AssertV2Failure(resolved, catalog, baseline, value => value.contentVersion = "99.0.0",
                BattleSnapshotRestoreCode.IncompatibleContent, "contentVersion", "stale content version");
            AssertV2Failure(resolved, catalog, baseline, value => value.mapId = "map.mismatch",
                BattleSnapshotRestoreCode.IncompatibleLevel, "mapId", "map component mismatch");
            AssertV2Failure(resolved, catalog, baseline,
                value => value.gameplayMapFingerprint = "gameplay-map.mismatch",
                BattleSnapshotRestoreCode.IncompatibleMap, "gameplayMapFingerprint",
                "gameplay topology mismatch");
            AssertV2Failure(resolved, catalog, baseline, value => value.waveSetId = "waves.mismatch",
                BattleSnapshotRestoreCode.IncompatibleLevel, "waveSetId", "wave-set component mismatch");
            AssertV2Failure(resolved, catalog, baseline, value => value.ruleSetId = "rules.mismatch",
                BattleSnapshotRestoreCode.IncompatibleLevel, "ruleSetId", "rule-set component mismatch");
            AssertV2Failure(resolved, catalog, baseline, value => value.themeId = "theme.mismatch",
                BattleSnapshotRestoreCode.IncompatibleLevel, "themeId", "theme component mismatch");
            AssertV2Failure(resolved, catalog, baseline, value => value.pots[0].cellX = -99,
                BattleSnapshotRestoreCode.InvalidReference,
                "pots[" + baseline.pots[0].entityId + "].cell", "invalid gameplay reference");
            AssertV2Failure(resolved, catalog, baseline,
                value => value.combatRuntime.nextCombatEventSequence = 0,
                BattleSnapshotRestoreCode.InvalidIdentity,
                "combatRuntime.nextCombatEventSequence", "invalid combat event sequence");
        }

        private static void AssertV2Failure(ResolvedLevelDefinition resolved,
            CompiledLevelCatalog catalog, BattleSnapshotV2 baseline, Action<BattleSnapshotV2> mutate,
            BattleSnapshotRestoreCode code, string path, string label)
        {
            var target = new GameSimulation(resolved, 7401);
            target.State.Sun = 317;
            target.AdvanceFrame(.013f);
            var state = target.State;
            var checksum = target.OutcomeStateChecksum();
            var random = target.RandomState;
            var pending = target.PendingPresentationEventCount;
            var dropped = target.DroppedPresentationEventCount;
            var accumulator = target.FrameAccumulatorSeconds;
            var snapshot = Clone(baseline);
            mutate(snapshot);

            var result = target.RestoreSnapshotV2(snapshot, catalog);
            Assert(result.Code == code && result.Path == path,
                label + " returns " + code + " at " + path + ", got " + result);
            Assert(ReferenceEquals(state, target.State)
                && checksum == target.OutcomeStateChecksum()
                && random == target.RandomState,
                label + " preserves live state and random state atomically.");
            Assert(pending == target.PendingPresentationEventCount
                && dropped == target.DroppedPresentationEventCount
                && Math.Abs(accumulator - target.FrameAccumulatorSeconds) < .0000001d,
                label + " preserves presentation delivery and frame accumulator atomically.");
        }

        private static void AssertLegacyFailure(GameSimulation target, BattleSnapshotV1 snapshot,
            CompiledLevelCatalog catalog, BattleSnapshotRestoreCode code, string path, string label)
        {
            var state = target.State;
            var checksum = target.OutcomeStateChecksum();
            var random = target.RandomState;
            var pending = target.PendingPresentationEventCount;
            var result = target.RestoreLegacySnapshotV1(snapshot, catalog);
            Assert(result.Code == code && result.Path == path,
                label + " returns " + code + " at " + path + ", got " + result);
            Assert(ReferenceEquals(state, target.State) && checksum == target.OutcomeStateChecksum()
                && random == target.RandomState && pending == target.PendingPresentationEventCount,
                label + " fails atomically.");
        }

        private static ResolvedLevelDefinition Resolve(CompiledLevelCatalog catalog, string levelId)
        {
            var result = catalog.Resolve(levelId);
            if (result.Succeeded) return result.Value;
            throw new InvalidOperationException("Unable to resolve smoke level " + levelId + ": " + result.Error);
        }

        private static BattleSnapshotV2 Clone(BattleSnapshotV2 value)
        {
            return JsonUtility.FromJson<BattleSnapshotV2>(JsonUtility.ToJson(value));
        }

        private static BattleSnapshotV1 Clone(BattleSnapshotV1 value)
        {
            return JsonUtility.FromJson<BattleSnapshotV1>(JsonUtility.ToJson(value));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Battle snapshot V2 validation failed: " + message);
        }
    }
}
