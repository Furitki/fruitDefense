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

        public static PlantVisualArchetype Plant(string definitionId)
        {
            if (definitionId == BattleContentIds.Plants.Pea) return PlantVisualArchetype.Pea;
            if (definitionId == BattleContentIds.Plants.Watermelon) return PlantVisualArchetype.Watermelon;
            if (definitionId == BattleContentIds.Plants.Banana) return PlantVisualArchetype.Banana;
            if (definitionId == BattleContentIds.Plants.Durian) return PlantVisualArchetype.Durian;
            if (definitionId == BattleContentIds.Plants.Sunflower) return PlantVisualArchetype.Sunflower;
            return PlantVisualArchetype.Generic;
        }

        public static EnemyVisualArchetype Enemy(string definitionId)
        {
            if (definitionId == BattleContentIds.Enemies.Normal) return EnemyVisualArchetype.Normal;
            if (definitionId == BattleContentIds.Enemies.Runner) return EnemyVisualArchetype.Runner;
            if (definitionId == BattleContentIds.Enemies.Armored) return EnemyVisualArchetype.Armored;
            if (definitionId == BattleContentIds.Enemies.Boss) return EnemyVisualArchetype.Boss;
            return EnemyVisualArchetype.Generic;
        }

        public static ProjectileVisualArchetype Projectile(string definitionId)
        {
            if (definitionId == BattleContentIds.Projectiles.Pea) return ProjectileVisualArchetype.Pea;
            if (definitionId == BattleContentIds.Projectiles.Watermelon) return ProjectileVisualArchetype.Watermelon;
            if (definitionId == BattleContentIds.Projectiles.Banana) return ProjectileVisualArchetype.Banana;
            return ProjectileVisualArchetype.Generic;
        }

        public static EquipmentVisualArchetype Equipment(string definitionId)
        {
            if (definitionId == BattleContentIds.Equipment.Gatling) return EquipmentVisualArchetype.Gatling;
            if (definitionId == BattleContentIds.Equipment.Ice) return EquipmentVisualArchetype.Ice;
            if (definitionId == BattleContentIds.Equipment.Chili) return EquipmentVisualArchetype.Chili;
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
