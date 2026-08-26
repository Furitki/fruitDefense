using System;
using System.Collections.Generic;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        private static IReadOnlyList<CompiledAbilityDefinition> ResolveCandidateAbilities(
            CombatEntityState entity, CompiledBattleContentCatalog content)
        {
            var plant = entity as Plant;
            return plant != null
                ? content.ResolvePlantAbilities(plant.DefinitionId, plant.EquipmentId)
                : content.ResolveEnemyAbilities(entity.DefinitionId);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool FiniteNonNegative(float value)
        {
            return Finite(value) && value >= 0f;
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        private static int CanonicalLiveEntityReference(int entityId,
            HashSet<int> liveEntityIds)
        {
            return entityId > 0 && liveEntityIds.Contains(entityId) ? entityId : 0;
        }

        private static BattleSnapshotRestoreResult ValidateCurrentWaveState(
            BattleSnapshot snapshot, ResolvedLevelDefinition resolved)
        {
            var phase = (GamePhase)snapshot.phase;
            if (phase == GamePhase.Ready)
            {
                if (snapshot.waveIndex != 0)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveIndex", "Ready state must precede the first resolved wave.");
                if (snapshot.waveTotal != 0)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveTotal", "Ready state cannot carry a resolved wave size.");
                if (snapshot.waveSpawned != 0)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveSpawned", "Ready state cannot carry spawned enemies.");
                return BattleSnapshotRestoreResult.Ok();
            }

            if (snapshot.waveIndex < 1 || snapshot.waveIndex > resolved.OrderedWaves.Count)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    "waveIndex", "Active or terminal state must identify a resolved wave.");
            var resolvedTotal = resolved.OrderedWaves[snapshot.waveIndex - 1].enemyIds.Length;
            if (snapshot.waveTotal != resolvedTotal)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    "waveTotal", "Wave size must equal the selected resolved wave definition.");
            if (snapshot.waveSpawned < 0 || snapshot.waveSpawned > resolvedTotal)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                    "waveSpawned", "Spawn progress exceeds the selected resolved wave definition.");

            if (phase == GamePhase.BetweenWaves)
            {
                if (snapshot.waveIndex >= resolved.OrderedWaves.Count)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveIndex", "The final resolved wave cannot enter BetweenWaves.");
                if (snapshot.waveSpawned != resolvedTotal)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveSpawned", "BetweenWaves requires a completely spawned wave.");
            }
            else if (phase == GamePhase.Victory)
            {
                if (snapshot.waveIndex != resolved.OrderedWaves.Count)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveIndex", "Victory requires the final resolved wave.");
                if (snapshot.waveSpawned != resolvedTotal)
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidNumericValue,
                        "waveSpawned", "Victory requires a completely spawned final wave.");
            }
            return BattleSnapshotRestoreResult.Ok();
        }

        private readonly struct EntityIdentity
        {
            public int Id { get; }
            public string Path { get; }

            public EntityIdentity(int id, string path)
            {
                Id = id;
                Path = path;
            }
        }
    }
}
