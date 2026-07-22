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

        public static PlantKind PlantKindFromId(string id)
        {
            PlantKind kind;
            if (TryPlantKindFromId(id, out kind)) return kind;
            throw new ArgumentOutOfRangeException(nameof(id), id, "No legacy plant enum adapter exists.");
        }

        public static bool TryPlantKindFromId(string id, out PlantKind kind)
        {
            if (id == BattleContentIds.Plants.Pea) { kind = PlantKind.Pea; return true; }
            if (id == BattleContentIds.Plants.Watermelon) { kind = PlantKind.Watermelon; return true; }
            if (id == BattleContentIds.Plants.Banana) { kind = PlantKind.Banana; return true; }
            if (id == BattleContentIds.Plants.Durian) { kind = PlantKind.Durian; return true; }
            if (id == BattleContentIds.Plants.Sunflower) { kind = PlantKind.Sunflower; return true; }
            kind = default(PlantKind);
            return false;
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

        public static ZombieKind EnemyKindFromId(string id)
        {
            ZombieKind kind;
            if (TryEnemyKindFromId(id, out kind)) return kind;
            throw new ArgumentOutOfRangeException(nameof(id), id, "No legacy enemy enum adapter exists.");
        }

        public static bool TryEnemyKindFromId(string id, out ZombieKind kind)
        {
            if (id == BattleContentIds.Enemies.Normal) { kind = ZombieKind.Normal; return true; }
            if (id == BattleContentIds.Enemies.Runner) { kind = ZombieKind.Runner; return true; }
            if (id == BattleContentIds.Enemies.Armored) { kind = ZombieKind.Armored; return true; }
            if (id == BattleContentIds.Enemies.Boss) { kind = ZombieKind.Boss; return true; }
            kind = default(ZombieKind);
            return false;
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


        public static WeaponKind WeaponKindFromId(string id)
        {
            if (id == BattleContentIds.Equipment.Gatling) return WeaponKind.Gatling;
            if (id == BattleContentIds.Equipment.Ice) return WeaponKind.Ice;
            if (id == BattleContentIds.Equipment.Chili) return WeaponKind.Chili;
            if (string.IsNullOrEmpty(id)) return WeaponKind.None;
            throw new ArgumentOutOfRangeException(nameof(id), id, "No legacy equipment enum adapter exists.");
        }
    }
}
