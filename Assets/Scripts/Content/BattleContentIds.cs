namespace FruitDefense.Content
{
    public static class BattleContentSchema
    {
        public const string CurrentSchemaVersion = "2";
        public const string BundledCatalogId = "catalog.bundled.orchard";
        public const string BundledContentVersion = "2.0.0";
        public const string MinimumCodeVersion = "0.1.0";
    }

    public static class BattleContentIds
    {
        public static class Plants
        {
            public const string Pea = "plant.pea";
            public const string Watermelon = "plant.watermelon";
            public const string Banana = "plant.banana";
            public const string Durian = "plant.durian";
            public const string Sunflower = "plant.sunflower";
        }

        public static class Enemies
        {
            public const string Normal = "enemy.normal";
            public const string Runner = "enemy.runner";
            public const string Armored = "enemy.armored";
            public const string Boss = "enemy.boss";
        }

        public static class Equipment
        {
            public const string Gatling = "equipment.gatling";
            public const string Ice = "equipment.ice";
            public const string Chili = "equipment.chili";
        }

        public static class Abilities
        {
            public const string PeaAttack = "ability.plant.pea.attack";
            public const string WatermelonAttack = "ability.plant.watermelon.attack";
            public const string BananaAttack = "ability.plant.banana.attack";
            public const string DurianAttack = "ability.plant.durian.attack";
            public const string SunflowerProduce = "ability.plant.sunflower.produce";
            public const string IceOnHit = "ability.equipment.ice.on-hit";
            public const string IceProducerOpening = "ability.equipment.ice.producer-opening";
            public const string ChiliOnHit = "ability.equipment.chili.on-hit";
        }

        public static class Projectiles
        {
            public const string Pea = "projectile.pea";
            public const string Watermelon = "projectile.watermelon";
            public const string Banana = "projectile.banana";
        }

        public static class Statuses
        {
            public const string IceSlow = "status.ice.slow";
            public const string IceFreeze = "status.ice.freeze";
            public const string IceCount = "status.ice.hit-count";
            public const string ChiliBurn = "status.chili.burn";
        }

        public static class Resources { public const string Sun = "resource.sun"; }
        public static class BattleStates
        {
            public const string Ready = "battle.ready";
            public const string WaveStarted = "wave.started";
            public const string CoreDamaged = "core.damaged";
            public const string WaveCompleted = "wave.completed";
            public const string MilestoneReward = "reward.milestone";
            public const string PotExpanded = "pot.expanded";
            public const string PlantMoved = "plant.moved";
            public const string PlantMerged = "plant.merged";
            public const string EquipmentInstalled = "equipment.installed";
        }

        public static class BattleRules
        {
            public const string Default = "rules.orchard.default";
        }
    }
}
