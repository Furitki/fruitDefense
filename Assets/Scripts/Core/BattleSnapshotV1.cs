using System;

namespace FruitDefense.Core
{
    public static class BattleSnapshotSchema
    {
        public const int Version = 1;
        public const string DefaultMapId = "orchard-01";
    }

    [Serializable]
    public sealed class BattleSnapshotV1
    {
        public int schemaVersion = BattleSnapshotSchema.Version;
        public string catalogId = string.Empty;
        public string contentVersion = string.Empty;
        public string mapId = BattleSnapshotSchema.DefaultMapId;
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
    }

    [Serializable]
    public sealed class BattleSnapshotEquipmentV1
    {
        public string definitionId = string.Empty;
        public int count;
    }

    [Serializable]
    public sealed class BattleSnapshotPotV1
    {
        public int entityId;
        public int cellX;
        public int cellY;
        public bool active;
    }

    [Serializable]
    public sealed class BattleSnapshotPlantV1
    {
        public int entityId;
        public string definitionId = string.Empty;
        public int legacyKind;
        public int star;
        public int potEntityId = -1;
        public int nurseryIndex = -1;
        public string equipmentDefinitionId = string.Empty;
        public int legacyWeapon;
        public float attackCooldown;
        public float productionProgress;
        public float moveCooldown;
        public int burstShotsRemaining;
        public float burstShotCooldown;
        public BattleSnapshotSkillRuntimeV1[] skills = Array.Empty<BattleSnapshotSkillRuntimeV1>();
    }

    [Serializable]
    public sealed class BattleSnapshotSkillRuntimeV1
    {
        public string definitionId = string.Empty;
        public int cooldownTicks;
        public int periodicProgressTicks;
        public int burstShotsRemaining;
        public int burstIntervalTicks;
    }

    [Serializable]
    public sealed class BattleSnapshotEnemyV1
    {
        public int entityId;
        public string definitionId = string.Empty;
        public float hp;
        public float maxHp;
        public float speed;
        public float pathProgress;
        public int reward;
        public int threat;
        public BattleSnapshotStatusV1[] statuses = Array.Empty<BattleSnapshotStatusV1>();
    }

    [Serializable]
    public sealed class BattleSnapshotStatusV1
    {
        public string definitionId = string.Empty;
        public int sourceEntityId;
        public int remainingTicks;
        public int stackCount;
        public float magnitude;
        public int sequence;
        public int tickProgress;
    }

    [Serializable]
    public sealed class BattleSnapshotProjectileV1
    {
        public int entityId;
        public int plantEntityId;
        public int targetEntityId = -1;
        public string definitionId = string.Empty;
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
        public float damage;
        public int ticksRemaining;
        public int flightTicks;
        public int[] hitEntityIds = Array.Empty<int>();
    }

    public enum BattleSnapshotRestoreCode
    {
        Success,
        InvalidPayload,
        UnsupportedSchema,
        ContentUnavailable,
        IncompatibleContent,
        IncompatibleMap,
        UnknownDefinition,
        InvalidReference,
        InvalidNumericValue,
        InvalidIdentity,
    }

    public readonly struct BattleSnapshotRestoreResult
    {
        public BattleSnapshotRestoreCode Code { get; }
        public string Path { get; }
        public string Message { get; }
        public bool Succeeded { get { return Code == BattleSnapshotRestoreCode.Success; } }

        public BattleSnapshotRestoreResult(BattleSnapshotRestoreCode code, string path, string message)
        {
            Code = code;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static BattleSnapshotRestoreResult Ok()
        {
            return new BattleSnapshotRestoreResult(BattleSnapshotRestoreCode.Success, string.Empty, string.Empty);
        }

        public override string ToString()
        {
            return Succeeded ? "Success" : Code + " at " + Path + ": " + Message;
        }
    }
}
