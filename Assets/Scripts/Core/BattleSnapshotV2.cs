using System;

namespace FruitDefense.Core
{
    public static class BattleSnapshotV2Schema
    {
        public const int Version = 2;
    }

    [Serializable]
    public sealed class BattleSnapshotV2
    {
        public int schemaVersion = BattleSnapshotV2Schema.Version;
        public string catalogId = string.Empty;
        public string contentCatalogId = string.Empty;
        public string contentVersion = string.Empty;
        public string levelId = string.Empty;
        public string mapId = string.Empty;
        public string gameplayMapFingerprint = string.Empty;
        public string waveSetId = string.Empty;
        public string ruleSetId = string.Empty;
        public string themeId = string.Empty;
        public int logicStep;
        public uint randomState;
        public int randomSeed;
        public int phase;
        public bool paused;
        public int speed = 1;
        public float elapsed;
        public int sun;
        public int lives;
        public int refreshCount;
        public int waveIndex;
        public int waveSpawned;
        public int waveTotal;
        public float spawnCooldown;
        public float betweenTimer;
        public int nextEntityId = 1;
        public int nextStatusSequence = 1;
        public int availablePots;
        public BattleSnapshotEquipmentV1[] equipment = Array.Empty<BattleSnapshotEquipmentV1>();
        public BattleSnapshotPotV1[] pots = Array.Empty<BattleSnapshotPotV1>();
        public BattleSnapshotPlantV1[] plants = Array.Empty<BattleSnapshotPlantV1>();
        public BattleSnapshotEnemyV1[] enemies = Array.Empty<BattleSnapshotEnemyV1>();
        public BattleSnapshotProjectileV1[] projectiles = Array.Empty<BattleSnapshotProjectileV1>();
        public BattleSnapshotCombatRuntimeV2 combatRuntime = new BattleSnapshotCombatRuntimeV2();
    }

    [Serializable]
    public sealed class BattleSnapshotCombatRuntimeV2
    {
        public bool present;
        public long nextCombatEventSequence = 1;
        public BattleSnapshotEntityRuntimeV2[] entities = Array.Empty<BattleSnapshotEntityRuntimeV2>();
    }

    [Serializable]
    public sealed class BattleSnapshotEntityRuntimeV2
    {
        public int entityId;
        public BattleSnapshotPassiveRuntimeV2[] passives = Array.Empty<BattleSnapshotPassiveRuntimeV2>();
        public BattleSnapshotStatusV1[] statuses = Array.Empty<BattleSnapshotStatusV1>();
    }

    [Serializable]
    public sealed class BattleSnapshotPassiveRuntimeV2
    {
        public string definitionId = string.Empty;
        public int cooldownTicks;
        public long lastRootEventSequence;
    }
}
