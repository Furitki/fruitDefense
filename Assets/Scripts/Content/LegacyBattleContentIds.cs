using System;
using FruitDefense.Core;

namespace FruitDefense.Content
{
    public static class LegacyBattleContentIds
    {
        public static string Plant(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Pea: return BattleContentIds.Plants.Pea;
                case PlantKind.Watermelon: return BattleContentIds.Plants.Watermelon;
                case PlantKind.Banana: return BattleContentIds.Plants.Banana;
                case PlantKind.Durian: return BattleContentIds.Plants.Durian;
                case PlantKind.Sunflower: return BattleContentIds.Plants.Sunflower;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported legacy plant kind.");
            }
        }

        public static string Enemy(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Normal: return BattleContentIds.Enemies.Normal;
                case ZombieKind.Runner: return BattleContentIds.Enemies.Runner;
                case ZombieKind.Armored: return BattleContentIds.Enemies.Armored;
                case ZombieKind.Boss: return BattleContentIds.Enemies.Boss;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported legacy enemy kind.");
            }
        }

        public static bool TryEquipment(WeaponKind kind, out string id)
        {
            switch (kind)
            {
                case WeaponKind.Gatling: id = BattleContentIds.Equipment.Gatling; return true;
                case WeaponKind.Ice: id = BattleContentIds.Equipment.Ice; return true;
                case WeaponKind.Chili: id = BattleContentIds.Equipment.Chili; return true;
                default: id = string.Empty; return false;
            }
        }

        public static string Equipment(WeaponKind kind)
        {
            string id;
            if (TryEquipment(kind, out id)) return id;
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported legacy equipment kind.");
        }
    }
}
