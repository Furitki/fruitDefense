using System;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class OutgameContentHeaderDto
    {
        public int schemaVersion = OutgameContentSchema.CurrentSchemaVersion;
        public string catalogId = OutgameContentSchema.BundledCatalogId;
        public string contentVersion = OutgameContentSchema.BundledContentVersion;
        public string minCodeVersion = OutgameContentSchema.MinimumCodeVersion;
    }

    [Serializable]
    public sealed class ItemDefinitionDto
    {
        public string id = string.Empty;
        public string presentationId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public int maximumQuantity;
    }

    [Serializable]
    public sealed class RewardGrantDto
    {
        public string operationId = string.Empty;
        public string itemId = string.Empty;
        public string growthEquipmentId = string.Empty;
        public int quantity;
        public int initialRank;
    }

    [Serializable]
    public sealed class ActivityDefinitionDto
    {
        public string id = string.Empty;
        public string presentationId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public bool bundledAvailable;
        public string receiptId = string.Empty;
        public RewardGrantDto[] rewards = Array.Empty<RewardGrantDto>();
    }

    [Serializable]
    public sealed class GrowthCostDto
    {
        public string itemId = string.Empty;
        public int quantity;
    }

    [Serializable]
    public sealed class GrowthContributionDto
    {
        public string domainId = string.Empty;
        public string attributeId = string.Empty;
        public string operationId = string.Empty;
        public float value;
    }

    [Serializable]
    public sealed class GrowthEquipmentRankDefinitionDto
    {
        public int rank;
        public GrowthCostDto[] costs = Array.Empty<GrowthCostDto>();
        public GrowthContributionDto[] contributions = Array.Empty<GrowthContributionDto>();
    }

    [Serializable]
    public sealed class GrowthEquipmentDefinitionDto
    {
        public string id = string.Empty;
        public string presentationId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public string slotId = string.Empty;
        public GrowthEquipmentRankDefinitionDto[] ranks =
            Array.Empty<GrowthEquipmentRankDefinitionDto>();
    }

    [Serializable]
    public sealed class CultivationPrerequisiteDto
    {
        public string nodeId = string.Empty;
        public int requiredRank;
    }

    [Serializable]
    public sealed class CultivationRankDefinitionDto
    {
        public int rank;
        public GrowthCostDto[] costs = Array.Empty<GrowthCostDto>();
        public GrowthContributionDto[] contributions = Array.Empty<GrowthContributionDto>();
    }

    [Serializable]
    public sealed class CultivationNodeDefinitionDto
    {
        public string id = string.Empty;
        public string presentationId = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public CultivationPrerequisiteDto[] prerequisites =
            Array.Empty<CultivationPrerequisiteDto>();
        public CultivationRankDefinitionDto[] ranks =
            Array.Empty<CultivationRankDefinitionDto>();
    }

    [Serializable]
    public sealed class GrowthPolicyCapDto
    {
        public string attributeId = string.Empty;
        public float minimumValue;
        public float maximumValue;
    }

    [Serializable]
    public sealed class GrowthPolicyDefinitionDto
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string[] permittedDomainIds = Array.Empty<string>();
        public string[] permittedAttributeIds = Array.Empty<string>();
        public string[] permittedSourceIds = Array.Empty<string>();
        public GrowthPolicyCapDto[] caps = Array.Empty<GrowthPolicyCapDto>();
    }

    [Serializable]
    public sealed class OutgameContentCatalogDto
    {
        public OutgameContentHeaderDto header = new OutgameContentHeaderDto();
        public ItemDefinitionDto[] items = Array.Empty<ItemDefinitionDto>();
        public ActivityDefinitionDto[] activities = Array.Empty<ActivityDefinitionDto>();
        public GrowthEquipmentDefinitionDto[] growthEquipment =
            Array.Empty<GrowthEquipmentDefinitionDto>();
        public CultivationNodeDefinitionDto[] cultivationNodes =
            Array.Empty<CultivationNodeDefinitionDto>();
        public GrowthPolicyDefinitionDto[] growthPolicies =
            Array.Empty<GrowthPolicyDefinitionDto>();
    }
}
