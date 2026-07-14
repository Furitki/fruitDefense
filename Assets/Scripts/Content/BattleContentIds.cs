namespace FruitDefense.Content
{
    public static class BattleContentSchema
    {
        public const string CurrentSchemaVersion = "1";
        public const string BundledCatalogId = "catalog.bundled.orchard";
        public const string BundledContentVersion = "1.0.0";
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

        public static class Skills
        {
            public const string PeaAttack = "skill.plant.pea.attack";
            public const string WatermelonAttack = "skill.plant.watermelon.attack";
            public const string BananaAttack = "skill.plant.banana.attack";
            public const string DurianAttack = "skill.plant.durian.attack";
            public const string SunflowerProduce = "skill.plant.sunflower.produce";
            public const string GatlingBurst = "skill.equipment.gatling.burst";
            public const string IceOnHit = "skill.equipment.ice.on-hit";
            public const string ChiliOnHit = "skill.equipment.chili.on-hit";
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
            public const string ChiliBurn = "status.chili.burn";
        }

        public static class BattleRules
        {
            public const string Default = "rules.orchard.default";
        }
    }
}
