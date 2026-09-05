using System;

namespace FruitDefense.Core
{
    public static class BattleSnapshotSchema
    {
        public const string Id = "fruit-defense.battle-snapshot";
        public const int Version = 5;
    }

    [Serializable]
    public sealed class BattleSnapshot
    {
        public string schemaId;
        public int schemaVersion;
        public string levelCatalogId;
        public string contentCatalogId;
        public string contentVersion;
        public string levelId;
        public string mapId;
        public string gameplayMapFingerprint;
        public string waveSetId;
        public string ruleSetId;
        public string themeId;
        public string growthPolicyId;
        public string growthContentCatalogId;
        public string growthContentVersion;
        public string growthContentFingerprint;
        public string growthProfileId;
        public long growthProfileRevision;
        public string growthFingerprint;
        public string resolvedSourceDefinitionFingerprint;
        public int logicStep;
        public uint randomState;
        public int randomSeed;
        public int phase;
        public bool paused;
        public int speed;
        public float elapsed;
        public int sun;
        public int lives;
        public int refreshCount;
        public int waveIndex;
        public int waveSpawned;
        public int waveTotal;
        public float spawnCooldown;
        public float betweenTimer;
        public int escapedEnemyCount;
        public int nextEntityId;
        public int nextStatusSequence;
        public int availablePots;
        public BattleSnapshotEquipment[] equipment;
        public BattleSnapshotPot[] pots;
        public BattleSnapshotPlant[] plants;
        public BattleSnapshotEnemy[] enemies;
        public BattleSnapshotProjectile[] projectiles;
        public BattleSnapshotCombatRuntime combatRuntime;
    }

    [Serializable]
    public sealed class BattleSnapshotEquipment
    {
        public string definitionId;
        public int count;
    }

    [Serializable]
    public sealed class BattleSnapshotPot
    {
        public int entityId;
        public int cellX;
        public int cellY;
        public bool active;
    }

    [Serializable]
    public sealed class BattleSnapshotPlant
    {
        public int entityId;
        public string definitionId;
        public int star;
        public int potEntityId;
        public int nurseryIndex;
        public string equipmentDefinitionId;
        public float moveCooldown;
    }

    [Serializable]
    public sealed class BattleSnapshotEnemy
    {
        public int entityId;
        public string definitionId;
        public float hp;
        public float maxHp;
        public float speed;
        public float pathProgress;
        public int reward;
        public int threat;
    }

    [Serializable]
    public sealed class BattleSnapshotProjectile
    {
        public int entityId;
        public int sourceEntityId;
        public int targetEntityId;
        public string definitionId;
        public string sourceDefinitionId;
        public string sourceEquipmentId;
        public string abilityId;
        public int deliveryIndex;
        public float originX;
        public float originY;
        public float positionX;
        public float positionY;
        public float targetX;
        public float targetY;
        public float directionX;
        public float directionY;
        public float maxDistance;
        public float progress;
        public bool returning;
        public float damageBasis;
        public int ticksRemaining;
        public int flightTicks;
        public int[] hitEntityIds;
    }

    [Serializable]
    public sealed class BattleSnapshotCombatRuntime
    {
        public long nextCombatEventSequence;
        public BattleSnapshotEntityRuntime[] entities;
    }

    [Serializable]
    public sealed class BattleSnapshotEntityRuntime
    {
        public int entityId;
        public BattleSnapshotAbilityRuntime[] abilities;
        public BattleSnapshotStatus[] statuses;
    }

    [Serializable]
    public sealed class BattleSnapshotAbilityRuntime
    {
        public string definitionId;
        public int phase;
        public int cooldownTicks;
        public int periodicProgressTicks;
        public int windupTicksRemaining;
        public int recoveryTicksRemaining;
        public int burstShotsRemaining;
        public int burstIntervalTicks;
        public int pendingSourceEntityId;
        public int pendingTargetEntityId;
        public float pendingEventMagnitude;
        public long pendingRootEventSequence;
        public long lastRootEventSequence;
    }

    [Serializable]
    public sealed class BattleSnapshotStatus
    {
        public string definitionId;
        public int sourceEntityId;
        public int remainingTicks;
        public int stackCount;
        public float magnitude;
        public int sequence;
        public int tickProgress;
    }
}
