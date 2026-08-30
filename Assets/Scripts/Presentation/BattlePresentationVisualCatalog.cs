using System;
using System.Collections.Generic;
using FruitDefense.Content;

namespace FruitDefense.Presentation
{
    public enum PlantVisualArchetype
    {
        Generic,
        Pea,
        Watermelon,
        Banana,
        Durian,
        Sunflower,
    }

    public enum EnemyVisualArchetype
    {
        Generic,
        Normal,
        Runner,
        Armored,
        Boss,
    }

    public enum ProjectileVisualArchetype
    {
        Generic,
        Pea,
        Watermelon,
        Banana,
    }

    public enum EquipmentVisualArchetype
    {
        Generic,
        Gatling,
        Ice,
        Chili,
    }

    /// <summary>
    /// Presentation-owned stable-ID mapping. Unknown content deliberately resolves
    /// to an explicit generic archetype; it never writes a legacy enum back to state.
    /// </summary>
    public static class BattlePresentationVisualCatalog
    {
        public static readonly IReadOnlyList<string> BundledPlantDefinitionIds =
            Array.AsReadOnly(new[]
            {
                BattleContentIds.Plants.Pea,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Plants.Banana,
                BattleContentIds.Plants.Durian,
                BattleContentIds.Plants.Sunflower,
            });

        public static readonly IReadOnlyList<string> BundledEquipmentDefinitionIds =
            Array.AsReadOnly(new[]
            {
                BattleContentIds.Equipment.Gatling,
                BattleContentIds.Equipment.Ice,
                BattleContentIds.Equipment.Chili,
            });

        public static PlantVisualArchetype Plant(string presentationId)
        {
            if (presentationId == BattleContentIds.Presentation.PlantPea) return PlantVisualArchetype.Pea;
            if (presentationId == BattleContentIds.Presentation.PlantWatermelon) return PlantVisualArchetype.Watermelon;
            if (presentationId == BattleContentIds.Presentation.PlantBanana) return PlantVisualArchetype.Banana;
            if (presentationId == BattleContentIds.Presentation.PlantDurian) return PlantVisualArchetype.Durian;
            if (presentationId == BattleContentIds.Presentation.PlantSunflower) return PlantVisualArchetype.Sunflower;
            return PlantVisualArchetype.Generic;
        }

        public static EnemyVisualArchetype Enemy(string presentationId)
        {
            if (presentationId == BattleContentIds.Presentation.EnemyNormal) return EnemyVisualArchetype.Normal;
            if (presentationId == BattleContentIds.Presentation.EnemyRunner) return EnemyVisualArchetype.Runner;
            if (presentationId == BattleContentIds.Presentation.EnemyArmored) return EnemyVisualArchetype.Armored;
            if (presentationId == BattleContentIds.Presentation.EnemyBoss) return EnemyVisualArchetype.Boss;
            return EnemyVisualArchetype.Generic;
        }

        public static ProjectileVisualArchetype Projectile(string presentationId)
        {
            if (presentationId == BattleContentIds.Presentation.ProjectilePea) return ProjectileVisualArchetype.Pea;
            if (presentationId == BattleContentIds.Presentation.ProjectileWatermelon) return ProjectileVisualArchetype.Watermelon;
            if (presentationId == BattleContentIds.Presentation.ProjectileBanana) return ProjectileVisualArchetype.Banana;
            return ProjectileVisualArchetype.Generic;
        }

        public static EquipmentVisualArchetype Equipment(string presentationId)
        {
            if (presentationId == BattleContentIds.Presentation.EquipmentGatling) return EquipmentVisualArchetype.Gatling;
            if (presentationId == BattleContentIds.Presentation.EquipmentIce) return EquipmentVisualArchetype.Ice;
            if (presentationId == BattleContentIds.Presentation.EquipmentChili) return EquipmentVisualArchetype.Chili;
            return EquipmentVisualArchetype.Generic;
        }

        public static int EquipmentToolIndex(string definitionId)
        {
            if (definitionId == BattleContentIds.Equipment.Gatling) return 0;
            if (definitionId == BattleContentIds.Equipment.Ice) return 1;
            if (definitionId == BattleContentIds.Equipment.Chili) return 2;
            return -1;
        }
    }
}
