namespace FruitDefense.Content
{
    public static class OutgameContentSchema
    {
        public const int CurrentSchemaVersion = 1;
        public const string BundledCatalogId = "catalog.outgame.orchard";
        public const string BundledContentVersion = "1.0.0";
        public const string MinimumCodeVersion = "0.1.0";
    }

    public static class OutgameContentIds
    {
        public static class Items
        {
            public const string MorningDew = "item.growth.morning-dew";
        }

        public static class Activities
        {
            public const string StarterSupplies = "activity.orchard.starter-supplies";
        }

        public static class Receipts
        {
            public const string StarterSupplies = "receipt.activity.orchard.starter-supplies";
        }

        public static class GrowthEquipment
        {
            public const string SunleafEmblem = "growth-equipment.sunleaf-emblem";
        }

        public static class GrowthSlots
        {
            public const string Offense = "growth-slot.offense";
        }

        public static class CultivationNodes
        {
            public const string VitalRoots = "cultivation.vital-roots";
        }

        public static class GrowthPolicies
        {
            public const string Orchard01 = "growth-policy.orchard-01";
            public const string Orchard02 = "growth-policy.orchard-02";
            public const string Orchard03 = "growth-policy.orchard-03";
        }

        public static class Presentations
        {
            public const string MorningDew = "presentation.outgame.item.morning-dew";
            public const string StarterSupplies = "presentation.outgame.activity.starter-supplies";
            public const string SunleafEmblem = "presentation.outgame.equipment.sunleaf-emblem";
            public const string VitalRoots = "presentation.outgame.cultivation.vital-roots";
        }

        public static class RewardOperations
        {
            public const string Item = "reward.item";
            public const string GrowthEquipment = "reward.growth-equipment";
        }

        public static class GrowthDomains
        {
            public const string Equipment = "growth-domain.equipment";
            public const string Cultivation = "growth-domain.cultivation";
        }
    }
}
