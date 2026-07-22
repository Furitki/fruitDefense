using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.Core
{
    public static class P0GameplayParityBaseline
    {
        public const string MapId = BattlefieldMapDefinition.DefaultMapId;
        public const float RouteLength = BattlefieldMapDefinition.DefaultRouteLength;
        public const float LegacyNormalEnemySpeed = 4.4f;
        public const float NormalEnemyTraversalSeconds = BattlefieldMapDefinition.LegacyRouteLength / LegacyNormalEnemySpeed;
        public const int WaveCount = 15;
        public const int InitialPotCount = 8;
        public const string WaveContentSignature =
            "5,0,0,0|6,2,0,0|7,3,0,0|8,3,1,0|8,4,2,0|9,5,2,0|9,6,3,0|"
            + "10,6,4,0|10,7,5,0|11,7,5,1|11,8,6,0|12,8,7,0|12,9,8,0|13,10,9,1|14,11,10,2";
        public const string CombatNumericSignature =
            "plants:pea=12/1/44,watermelon=12/2.2/44,banana=6/1.6/38,durian=12/1.8/18,sunflower=0/10/0;"
            + "enemies:normal=36/4.4/1/1,runner=25/6.4/1/1,armored=80/3.4/1/2,boss=430/2.7/1/3;"
            + "stars:damage=1/1.5/3/5,speed=1/1.05/1.1/1.2,range=1/1.05/1.1/1.15;"
            + "waves=15,between=15,initial-pots=8";
    }

    public struct PlantStats
    {
        public string Name;
        public string Emoji;
        public float Damage;
        public float Interval;
        public float Range;
        public string Description;

        public PlantStats(string name, string emoji, float damage, float interval, float range, string description)
        {
            Name = name;
            Emoji = emoji;
            Damage = damage;
            Interval = interval;
            Range = range;
            Description = description;
        }
    }

    public struct ZombieStats
    {
        public string Name;
        public string Emoji;
        public float Hp;
        public float Speed;
        public int Reward;
        public int Threat;

        public ZombieStats(string name, string emoji, float hp, float speed, int reward, int threat)
        {
            Name = name;
            Emoji = emoji;
            Hp = hp;
            Speed = speed;
            Reward = reward;
            Threat = threat;
        }
    }

    public sealed class WaveDefinition
    {
        public int Index;
        public float HpMultiplier;
        public float SpeedMultiplier;
        public float SpawnInterval;
        public int Reward;
        public readonly List<ZombieKind> Sequence = new List<ZombieKind>();
    }

    public static class GameConfig
    {
        public const int MaxWaves = 15;
        public const int InitialPotCount = 8;
        public const float BetweenWaveSeconds = 15f;
        public static readonly BattlefieldMapDefinition DefaultBattlefield = BattlefieldMapDefinition.CreateDefault();
        public static IReadOnlyList<Vector2Int> PlantingCells { get { return DefaultBattlefield.PlantableCells; } }
        public static IReadOnlyList<Vector2> PathPoints { get { return DefaultBattlefield.RouteNodes; } }
        public static float PathLength { get { return DefaultBattlefield.Route.TotalLength; } }

        private static readonly int[,] WaveCounts =
        {
            { 5, 0, 0, 0 }, { 6, 2, 0, 0 }, { 7, 3, 0, 0 },
            { 8, 3, 1, 0 }, { 8, 4, 2, 0 }, { 9, 5, 2, 0 },
            { 9, 6, 3, 0 }, { 10, 6, 4, 0 }, { 10, 7, 5, 0 },
            { 11, 7, 5, 1 }, { 11, 8, 6, 0 }, { 12, 8, 7, 0 },
            { 12, 9, 8, 0 }, { 13, 10, 9, 1 }, { 14, 11, 10, 2 },
        };

        public static PlantStats Plant(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Pea: return new PlantStats("豌豆", "●", 12f, 1f, MapDistance(44f), "稳定的单体远程输出");
                case PlantKind.Watermelon: return new PlantStats("西瓜", "◆", 12f, 2.2f, MapDistance(44f), "低频范围爆炸伤害");
                case PlantKind.Banana: return new PlantStats("香蕉", "◒", 6f, 1.6f, MapDistance(38f), "直线往返穿透攻击");
                case PlantKind.Durian: return new PlantStats("榴莲", "✹", 12f, 1.8f, MapDistance(18f), "近战范围砸击");
                default: return new PlantStats("向日葵", "✿", 0f, 10f, 0f, "周期生产阳光");
            }
        }

        public static ZombieStats Zombie(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Runner: return new ZombieStats("路障快尸", "▶", 25f, MapDistance(6.4f), 1, 1);
                case ZombieKind.Armored: return new ZombieStats("铁桶僵尸", "■", 80f, MapDistance(3.4f), 1, 2);
                case ZombieKind.Boss: return new ZombieStats("园丁尸王", "♛", 430f, MapDistance(2.7f), 1, 3);
                default: return new ZombieStats("普通僵尸", "◆", 36f, MapDistance(4.4f), 1, 1);
            }
        }

        public static string WeaponName(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.Gatling: return "机枪";
                case WeaponKind.Ice: return "冰块";
                case WeaponKind.Chili: return "辣椒";
                default: return "无";
            }
        }

        public static string WeaponEmoji(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.Gatling: return "▰";
                case WeaponKind.Ice: return "❄";
                case WeaponKind.Chili: return "▲";
                default: return "";
            }
        }

        public static float StarDamage(int star)
        {
            switch (star) { case 2: return 1.5f; case 3: return 3f; case 4: return 5f; default: return 1f; }
        }

        public static float StarSpeed(int star)
        {
            switch (star) { case 2: return 1.05f; case 3: return 1.1f; case 4: return 1.2f; default: return 1f; }
        }

        public static float StarRange(int star)
        {
            switch (star) { case 2: return 1.05f; case 3: return 1.1f; case 4: return 1.15f; default: return 1f; }
        }

        public static int RefreshCost(int refreshCount) { return 10 + refreshCount * 5; }
        public static float WaveHpMultiplier(int wave) { return Mathf.Pow(2f, Mathf.FloorToInt((Mathf.Max(1, wave) - 1) / 2f)); }
        public static float WaveSpeedMultiplier(int wave) { return Mathf.Min(2f, Mathf.Pow(1.5f, Mathf.FloorToInt((Mathf.Max(1, wave) - 1) / 2f))); }
        public static int WaveCountMultiplier(int wave) { return (int)Mathf.Pow(3, Mathf.FloorToInt((Mathf.Max(1, wave) - 1) / 5f)); }

        public static WaveDefinition GetWave(int index)
        {
            index = Mathf.Clamp(index, 1, MaxWaves);
            var wave = new WaveDefinition
            {
                Index = index,
                HpMultiplier = WaveHpMultiplier(index),
                SpeedMultiplier = WaveSpeedMultiplier(index),
                SpawnInterval = Mathf.Max(.38f, 1.05f - (index - 1) * .045f),
                Reward = 5 + Mathf.CeilToInt(index / 3f),
            };
            var multiplier = WaveCountMultiplier(index);
            var remaining = new[]
            {
                WaveCounts[index - 1, 0] * multiplier,
                WaveCounts[index - 1, 1] * multiplier,
                WaveCounts[index - 1, 2] * multiplier,
                WaveCounts[index - 1, 3] * multiplier,
            };
            while (remaining[0] + remaining[1] + remaining[2] + remaining[3] > 0)
            {
                for (var kind = 0; kind < remaining.Length; kind++)
                {
                    if (remaining[kind] <= 0) continue;
                    wave.Sequence.Add((ZombieKind)kind);
                    remaining[kind]--;
                }
            }
            return wave;
        }

        public static float MapDistance(float legacyDistance) { return DefaultBattlefield.FromLegacyDistance(legacyDistance); }
        public static float LegacyDistance(float mapDistance) { return DefaultBattlefield.ToLegacyDistance(mapDistance); }

        public static Vector2 GridPoint(int column, int row)
        {
            return DefaultBattlefield.CellToMap(new Vector2Int(column, row));
        }

        public static Vector2 SamplePath(float progress)
        {
            return DefaultBattlefield.Route.Sample(progress);
        }
    }
}
