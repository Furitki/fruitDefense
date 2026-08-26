using System;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class BattleContentHeaderDto
    {
        public string schemaVersion = BattleContentSchema.CurrentSchemaVersion;
        public string catalogId = string.Empty;
        public string contentVersion = string.Empty;
        public string minCodeVersion = string.Empty;
    }

    [Serializable]
    public sealed class PlantDefinitionDto
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public float damage;
        public float attackIntervalSeconds;
        public float rangeLegacyUnits;
        public float potVisualHeightOffset;
        public string[] abilityIds = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();
        public string[] allowedEquipmentIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class EnemyDefinitionDto
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public float health;
        public float speedLegacyUnits;
        public int killReward;
        public int threat;
        public string[] abilityIds = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();
    }

    [Serializable]
    public sealed class EquipmentDefinitionDto
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string[] compatiblePlantIds = Array.Empty<string>();
        public AbilityGrantDefinitionDto[] grants = Array.Empty<AbilityGrantDefinitionDto>();
        public AbilityModifierDefinitionDto[] modifiers = Array.Empty<AbilityModifierDefinitionDto>();
    }

    [Serializable]
    public sealed class AbilityGrantDefinitionDto
    {
        public string abilityId = string.Empty;
        public string requiredPlantTag = string.Empty;
    }

    [Serializable]
    public sealed class AbilityModifierDefinitionDto
    {
        public string id = string.Empty;
        public string requiredPlantTag = string.Empty;
        public string targetAbilityId = string.Empty;
        public string targetAbilityTag = string.Empty;
        public bool allowMultipleMatches;
        public string attributeId = string.Empty;
        public string operationId = string.Empty;
        public float value;
    }

    [Serializable]
    public sealed class AbilityDefinitionDto
    {
        public string id = string.Empty;
        public AbilityActivationDefinitionDto activation = new AbilityActivationDefinitionDto();
        public AbilityTimelineDefinitionDto timeline = new AbilityTimelineDefinitionDto();
        public float damageMultiplier = 1f;
        public int burstCount = 1;
        public float burstIntervalSeconds;
        public string[] tags = Array.Empty<string>();
        public AbilityDeliveryDefinitionDto[] deliveries = Array.Empty<AbilityDeliveryDefinitionDto>();
    }

    [Serializable]
    public sealed class AbilityActivationDefinitionDto
    {
        public string kindId = string.Empty;
        public string eventId = string.Empty;
        public string ownerRoleId = "owner.any";
        public int priority;
        public float cooldownSeconds;
        public float periodSeconds;
    }

    [Serializable]
    public sealed class AbilityTimelineDefinitionDto
    {
        public float windupSeconds;
        public float recoverySeconds;
    }

    [Serializable]
    public sealed class AbilityDeliveryDefinitionDto
    {
        public string targetId = string.Empty;
        public string modeId = "delivery.instant";
        public string projectileId = string.Empty;
        public float radiusLegacyUnits;
        public AbilityPayloadEffectDefinitionDto[] payload = Array.Empty<AbilityPayloadEffectDefinitionDto>();
    }

    [Serializable]
    public sealed class AbilityPayloadEffectDefinitionDto
    {
        public string kindId = string.Empty;
        public string statusId = string.Empty;
        public float magnitude = 1f;
        public int resourceAmount;
    }

    [Serializable]
    public sealed class ProjectileDefinitionDto
    {
        public string id = string.Empty;
        public string travelMode = string.Empty;
        public float speedLegacyUnits;
        public float flightSeconds;
        public float rangeMultiplier = 1f;
        public float hitRadiusLegacyUnits;
        public int maxHitsPerTarget = 1;
    }

    [Serializable]
    public sealed class StatusDefinitionDto
    {
        public string id = string.Empty;
        public string stackingMode = string.Empty;
        public float durationSeconds;
        public float tickIntervalSeconds;
        public float magnitude;
        public int maxStacks = 1;
        public int hitsToProc;
        public string kindId = string.Empty;
        public string procStatusId = string.Empty;
        public string polarityId = "polarity.neutral";
        public string[] tags = Array.Empty<string>();
        public bool blocksMovement;
        public StatusModifierDefinitionDto[] modifiers = Array.Empty<StatusModifierDefinitionDto>();
        public string periodicEffectId = "periodic.none";
    }

    [Serializable]
    public sealed class StatusModifierDefinitionDto
    {
        public string attributeId = string.Empty;
        public string operationId = string.Empty;
        public float value;
        public bool scaleWithMagnitude;
    }

    [Serializable]
    public sealed class WaveDefinitionDto
    {
        public string id = string.Empty;
        public int index;
        public float healthMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float spawnIntervalSeconds;
        public int completionReward;
        public string[] enemyIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class StarTierDefinitionDto
    {
        public string id = string.Empty;
        public int star;
        public float damageMultiplier = 1f;
        public float attackSpeedMultiplier = 1f;
        public float rangeMultiplier = 1f;
    }

    [Serializable]
    public sealed class MilestoneRewardDefinitionDto
    {
        public int wave;
        public string[] equipmentIds = Array.Empty<string>();
        public int potCount;
    }

    [Serializable]
    public sealed class BattleRulesDto
    {
        public string id = BattleContentIds.BattleRules.Default;
        public int initialSun;
        public int initialLives;
        public int maxWaves;
        public int initialPotCount;
        public float betweenWaveSeconds;
        public int nurserySlotCount;
        public float nurseryPotChance;
        public int refreshBaseCost;
        public int refreshCostStep;
        public MilestoneRewardDefinitionDto[] milestoneRewards = Array.Empty<MilestoneRewardDefinitionDto>();
    }

    [Serializable]
    public sealed class BattleContentCatalogDto
    {
        public BattleContentHeaderDto header = new BattleContentHeaderDto();
        public PlantDefinitionDto[] plants = Array.Empty<PlantDefinitionDto>();
        public EnemyDefinitionDto[] enemies = Array.Empty<EnemyDefinitionDto>();
        public EquipmentDefinitionDto[] equipment = Array.Empty<EquipmentDefinitionDto>();
        public AbilityDefinitionDto[] abilities = Array.Empty<AbilityDefinitionDto>();
        public ProjectileDefinitionDto[] projectiles = Array.Empty<ProjectileDefinitionDto>();
        public StatusDefinitionDto[] statuses = Array.Empty<StatusDefinitionDto>();
        public WaveDefinitionDto[] waves = Array.Empty<WaveDefinitionDto>();
        public StarTierDefinitionDto[] starTiers = Array.Empty<StarTierDefinitionDto>();
        public BattleRulesDto battleRules = new BattleRulesDto();
    }
}
