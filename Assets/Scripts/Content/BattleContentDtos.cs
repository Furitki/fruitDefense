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
        public string[] skillIds = Array.Empty<string>();
        public string[] passiveIds = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();
        public string projectileId = string.Empty;
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
        public string[] skillIds = Array.Empty<string>();
        public string[] passiveIds = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();
    }

    [Serializable]
    public sealed class EquipmentDefinitionDto
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string[] skillIds = Array.Empty<string>();
        public string[] statusIds = Array.Empty<string>();
        public string[] compatiblePlantIds = Array.Empty<string>();
        public EquipmentSkillGrantDto[] grants = Array.Empty<EquipmentSkillGrantDto>();
        public SkillModifierDefinitionDto[] modifiers = Array.Empty<SkillModifierDefinitionDto>();
        public string[] passiveIds = Array.Empty<string>();
        public EquipmentPassiveGrantDto[] passiveGrants = Array.Empty<EquipmentPassiveGrantDto>();
    }

    [Serializable]
    public sealed class EquipmentSkillGrantDto
    {
        public string skillId = string.Empty;
        public string requiredPlantTag = string.Empty;
    }

    [Serializable]
    public sealed class EquipmentPassiveGrantDto
    {
        public string passiveId = string.Empty;
        public string requiredPlantTag = string.Empty;
    }

    [Serializable]
    public sealed class SkillModifierDefinitionDto
    {
        public string id = string.Empty;
        public string requiredPlantTag = string.Empty;
        public string targetSkillTag = string.Empty;
        public bool allowMultipleMatches;
        public int burstCountOverride;
        public float burstIntervalSeconds;
        public int resourceAmountDelta;
    }

    [Serializable]
    public sealed class SkillDefinitionDto
    {
        public string id = string.Empty;
        public string triggerId = string.Empty;
        public string targetId = string.Empty;
        public string projectileId = string.Empty;
        public string statusId = string.Empty;
        public float cooldownSeconds;
        public float damageMultiplier = 1f;
        public int resourceAmount;
        public int burstCount = 1;
        public float burstIntervalSeconds;
        public string[] tags = Array.Empty<string>();
        public SkillEffectDefinitionDto[] effects = Array.Empty<SkillEffectDefinitionDto>();
        public string visualId = string.Empty;
        public string cueId = string.Empty;
        public float actionSeconds;
    }

    [Serializable]
    public sealed class SkillEffectDefinitionDto
    {
        public string kindId = string.Empty;
        public string projectileId = string.Empty;
        public string statusId = string.Empty;
        public float magnitude = 1f;
        public float radiusLegacyUnits;
        public int resourceAmount;
        public string cueId = string.Empty;
    }

    [Serializable]
    public sealed class PassiveDefinitionDto
    {
        public string id = string.Empty;
        public string triggerId = string.Empty;
        public string ownerRoleId = string.Empty;
        public string targetId = string.Empty;
        public int priority;
        public float cooldownSeconds;
        public string[] tags = Array.Empty<string>();
        public SkillEffectDefinitionDto[] effects = Array.Empty<SkillEffectDefinitionDto>();
    }

    [Serializable]
    public sealed class ProjectileDefinitionDto
    {
        public string id = string.Empty;
        public string travelMode = string.Empty;
        public float speedLegacyUnits;
        public float flightSeconds;
        public float blastRadiusLegacyUnits;
        public float rangeMultiplier = 1f;
        public float hitRadiusLegacyUnits;
        public int maxHitsPerTarget = 1;
        public string visualId = string.Empty;
        public string impactCueId = string.Empty;
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
        public string cueId = string.Empty;
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
        public SkillDefinitionDto[] skills = Array.Empty<SkillDefinitionDto>();
        public PassiveDefinitionDto[] passives = Array.Empty<PassiveDefinitionDto>();
        public ProjectileDefinitionDto[] projectiles = Array.Empty<ProjectileDefinitionDto>();
        public StatusDefinitionDto[] statuses = Array.Empty<StatusDefinitionDto>();
        public WaveDefinitionDto[] waves = Array.Empty<WaveDefinitionDto>();
        public StarTierDefinitionDto[] starTiers = Array.Empty<StarTierDefinitionDto>();
        public BattleRulesDto battleRules = new BattleRulesDto();
    }
}
