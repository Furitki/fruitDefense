using System;
using System.Collections.Generic;

namespace FruitDefense.Content
{
    public static class BundledBattleContentFactory
    {
        private static readonly string[] AllEquipment =
        {
            BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili,
        };

        private static readonly string[] AllPlants =
        {
            BattleContentIds.Plants.Pea, BattleContentIds.Plants.Watermelon, BattleContentIds.Plants.Banana,
            BattleContentIds.Plants.Durian, BattleContentIds.Plants.Sunflower,
        };

        public static BattleContentCatalogDto Create()
        {
            return new BattleContentCatalogDto
            {
                header = new BattleContentHeaderDto
                {
                    schemaVersion = BattleContentSchema.CurrentSchemaVersion,
                    catalogId = BattleContentSchema.BundledCatalogId,
                    contentVersion = BattleContentSchema.BundledContentVersion,
                    minCodeVersion = BattleContentSchema.MinimumCodeVersion,
                },
                plants = CreatePlants(),
                enemies = CreateEnemies(),
                equipment = CreateEquipment(),
                skills = CreateSkills(),
                projectiles = CreateProjectiles(),
                statuses = CreateStatuses(),
                waves = CreateWaves(),
                starTiers = CreateStarTiers(),
                battleRules = CreateBattleRules(),
            };
        }

        private static PlantDefinitionDto[] CreatePlants()
        {
            return new[]
            {
                Plant(BattleContentIds.Plants.Pea, "\u8c4c\u8c46", "\u7a33\u5b9a\u7684\u5355\u4f53\u8fdc\u7a0b\u8f93\u51fa",
                    12f, 1f, 44f, BattleContentIds.Skills.PeaAttack, BattleContentIds.Projectiles.Pea, AllEquipment),
                Plant(BattleContentIds.Plants.Watermelon, "\u897f\u74dc", "\u4f4e\u9891\u8303\u56f4\u7206\u70b8\u4f24\u5bb3",
                    12f, 2.2f, 44f, BattleContentIds.Skills.WatermelonAttack, BattleContentIds.Projectiles.Watermelon, AllEquipment),
                Plant(BattleContentIds.Plants.Banana, "\u9999\u8549", "\u76f4\u7ebf\u5f80\u8fd4\u7a7f\u900f\u653b\u51fb",
                    6f, 1.6f, 38f, BattleContentIds.Skills.BananaAttack, BattleContentIds.Projectiles.Banana, AllEquipment),
                Plant(BattleContentIds.Plants.Durian, "\u69b4\u83b2", "\u8fd1\u6218\u8303\u56f4\u7838\u51fb",
                    12f, 1.8f, 18f, BattleContentIds.Skills.DurianAttack, string.Empty,
                    new[] { BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili }),
                Plant(BattleContentIds.Plants.Sunflower, "\u5411\u65e5\u8475", "\u5468\u671f\u4ea7\u751f\u9633\u5149",
                    0f, 10f, 0f, BattleContentIds.Skills.SunflowerProduce, string.Empty,
                    new[] { BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili }),
            };
        }

        private static PlantDefinitionDto Plant(string id, string displayName, string description, float damage,
            float interval, float range, string skillId, string projectileId, string[] allowedEquipmentIds)
        {
            return new PlantDefinitionDto
            {
                id = id,
                displayName = displayName,
                description = description,
                damage = damage,
                attackIntervalSeconds = interval,
                rangeLegacyUnits = range,
                skillIds = new[] { skillId },
                projectileId = projectileId,
                allowedEquipmentIds = (string[])allowedEquipmentIds.Clone(),
            };
        }

        private static EnemyDefinitionDto[] CreateEnemies()
        {
            return new[]
            {
                Enemy(BattleContentIds.Enemies.Normal, "\u666e\u901a\u50f5\u5c38", 36f, 4.4f, 1, 1),
                Enemy(BattleContentIds.Enemies.Runner, "\u8def\u969c\u5feb\u5c38", 25f, 6.4f, 1, 1),
                Enemy(BattleContentIds.Enemies.Armored, "\u94c1\u6876\u50f5\u5c38", 80f, 3.4f, 1, 2),
                Enemy(BattleContentIds.Enemies.Boss, "\u56ed\u4e01\u5c38\u738b", 430f, 2.7f, 1, 3),
            };
        }

        private static EnemyDefinitionDto Enemy(string id, string name, float health, float speed, int reward, int threat)
        {
            return new EnemyDefinitionDto
            {
                id = id, displayName = name, health = health, speedLegacyUnits = speed,
                killReward = reward, threat = threat,
            };
        }

        private static EquipmentDefinitionDto[] CreateEquipment()
        {
            return new[]
            {
                new EquipmentDefinitionDto
                {
                    id = BattleContentIds.Equipment.Gatling,
                    displayName = "\u673a\u67aa",
                    skillIds = new[] { BattleContentIds.Skills.GatlingBurst },
                    compatiblePlantIds = new[]
                    {
                        BattleContentIds.Plants.Pea, BattleContentIds.Plants.Watermelon, BattleContentIds.Plants.Banana,
                    },
                },
                new EquipmentDefinitionDto
                {
                    id = BattleContentIds.Equipment.Ice,
                    displayName = "\u51b0\u5757",
                    skillIds = new[] { BattleContentIds.Skills.IceOnHit },
                    statusIds = new[] { BattleContentIds.Statuses.IceSlow, BattleContentIds.Statuses.IceFreeze },
                    compatiblePlantIds = (string[])AllPlants.Clone(),
                },
                new EquipmentDefinitionDto
                {
                    id = BattleContentIds.Equipment.Chili,
                    displayName = "\u8fa3\u6912",
                    skillIds = new[] { BattleContentIds.Skills.ChiliOnHit },
                    statusIds = new[] { BattleContentIds.Statuses.ChiliBurn },
                    compatiblePlantIds = (string[])AllPlants.Clone(),
                },
            };
        }

        private static SkillDefinitionDto[] CreateSkills()
        {
            return new[]
            {
                Skill(BattleContentIds.Skills.PeaAttack, "trigger.cooldown", "target.front", BattleContentIds.Projectiles.Pea, string.Empty, 1f, 1f),
                Skill(BattleContentIds.Skills.WatermelonAttack, "trigger.cooldown", "target.front", BattleContentIds.Projectiles.Watermelon, string.Empty, 2.2f, 1f),
                Skill(BattleContentIds.Skills.BananaAttack, "trigger.cooldown", "target.line", BattleContentIds.Projectiles.Banana, string.Empty, 1.6f, 1f),
                Skill(BattleContentIds.Skills.DurianAttack, "trigger.cooldown", "target.area", string.Empty, string.Empty, 1.8f, 1f),
                new SkillDefinitionDto
                {
                    id = BattleContentIds.Skills.SunflowerProduce,
                    triggerId = "trigger.periodic",
                    targetId = "target.self",
                    cooldownSeconds = 10f,
                    damageMultiplier = 0f,
                    resourceAmount = 1,
                },
                new SkillDefinitionDto
                {
                    id = BattleContentIds.Skills.GatlingBurst,
                    triggerId = "trigger.attack",
                    targetId = "target.event",
                    cooldownSeconds = 0f,
                    damageMultiplier = 1f,
                    burstCount = 4,
                    burstIntervalSeconds = .2f,
                },
                Skill(BattleContentIds.Skills.IceOnHit, "trigger.on-hit", "target.event", string.Empty,
                    BattleContentIds.Statuses.IceSlow, 0f, 0f),
                Skill(BattleContentIds.Skills.ChiliOnHit, "trigger.on-hit", "target.event", string.Empty,
                    BattleContentIds.Statuses.ChiliBurn, 0f, .2f),
            };
        }

        private static SkillDefinitionDto Skill(string id, string triggerId, string targetId, string projectileId,
            string statusId, float cooldown, float damageMultiplier)
        {
            return new SkillDefinitionDto
            {
                id = id,
                triggerId = triggerId,
                targetId = targetId,
                projectileId = projectileId,
                statusId = statusId,
                cooldownSeconds = cooldown,
                damageMultiplier = damageMultiplier,
            };
        }

        private static ProjectileDefinitionDto[] CreateProjectiles()
        {
            return new[]
            {
                new ProjectileDefinitionDto
                {
                    id = BattleContentIds.Projectiles.Pea,
                    travelMode = "travel.homing",
                    speedLegacyUnits = 65f,
                    hitRadiusLegacyUnits = 2.25f,
                },
                new ProjectileDefinitionDto
                {
                    id = BattleContentIds.Projectiles.Watermelon,
                    travelMode = "travel.arc",
                    flightSeconds = .4f,
                    blastRadiusLegacyUnits = 7f,
                    hitRadiusLegacyUnits = 2.25f,
                },
                new ProjectileDefinitionDto
                {
                    id = BattleContentIds.Projectiles.Banana,
                    travelMode = "travel.returning-line",
                    speedLegacyUnits = 48f,
                    rangeMultiplier = 1.5f,
                    hitRadiusLegacyUnits = 2.25f,
                    maxHitsPerTarget = 2,
                },
            };
        }

        private static StatusDefinitionDto[] CreateStatuses()
        {
            return new[]
            {
                new StatusDefinitionDto
                {
                    id = BattleContentIds.Statuses.IceSlow,
                    stackingMode = "stacking.refresh",
                    durationSeconds = 2f,
                    magnitude = .55f,
                },
                new StatusDefinitionDto
                {
                    id = BattleContentIds.Statuses.IceFreeze,
                    stackingMode = "stacking.proc-after-hits",
                    durationSeconds = 1f,
                    magnitude = 1f,
                    hitsToProc = 5,
                },
                new StatusDefinitionDto
                {
                    id = BattleContentIds.Statuses.ChiliBurn,
                    stackingMode = "stacking.independent",
                    durationSeconds = 3f,
                    magnitude = .2f,
                    maxStacks = 3,
                },
            };
        }

        private static StarTierDefinitionDto[] CreateStarTiers()
        {
            return new[]
            {
                Star(1, 1f, 1f, 1f),
                Star(2, 1.5f, 1.05f, 1.05f),
                Star(3, 3f, 1.1f, 1.1f),
                Star(4, 5f, 1.2f, 1.15f),
            };
        }

        private static StarTierDefinitionDto Star(int star, float damage, float speed, float range)
        {
            return new StarTierDefinitionDto
            {
                id = "star." + star,
                star = star,
                damageMultiplier = damage,
                attackSpeedMultiplier = speed,
                rangeMultiplier = range,
            };
        }

        private static BattleRulesDto CreateBattleRules()
        {
            return new BattleRulesDto
            {
                id = BattleContentIds.BattleRules.Default,
                initialSun = 10,
                initialLives = 10,
                maxWaves = 15,
                initialPotCount = 8,
                betweenWaveSeconds = 15f,
                nurserySlotCount = 5,
                nurseryPotChance = .1f,
                refreshBaseCost = 10,
                refreshCostStep = 5,
                milestoneRewards = new[]
                {
                    Milestone(3, BattleContentIds.Equipment.Gatling),
                    Milestone(6, BattleContentIds.Equipment.Ice),
                    Milestone(9, BattleContentIds.Equipment.Chili),
                    Milestone(12, BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili),
                },
            };
        }

        private static MilestoneRewardDefinitionDto Milestone(int wave, params string[] equipmentIds)
        {
            return new MilestoneRewardDefinitionDto { wave = wave, equipmentIds = equipmentIds, potCount = 1 };
        }

        private static WaveDefinitionDto[] CreateWaves()
        {
            var counts = new[,]
            {
                { 5, 0, 0, 0 }, { 6, 2, 0, 0 }, { 7, 3, 0, 0 },
                { 8, 3, 1, 0 }, { 8, 4, 2, 0 }, { 9, 5, 2, 0 },
                { 9, 6, 3, 0 }, { 10, 6, 4, 0 }, { 10, 7, 5, 0 },
                { 11, 7, 5, 1 }, { 11, 8, 6, 0 }, { 12, 8, 7, 0 },
                { 12, 9, 8, 0 }, { 13, 10, 9, 1 }, { 14, 11, 10, 2 },
            };
            var health = new[] { 1f, 1f, 2f, 2f, 4f, 4f, 8f, 8f, 16f, 16f, 32f, 32f, 64f, 64f, 128f };
            var speed = new[] { 1f, 1f, 1.5f, 1.5f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f, 2f };
            var spawn = new[] { 1.05f, 1.005f, .96f, .915f, .87f, .825f, .78f, .735f, .69f, .645f, .6f, .555f, .51f, .465f, .42f };
            var rewards = new[] { 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 10 };
            var waves = new WaveDefinitionDto[15];
            for (var row = 0; row < waves.Length; row++)
            {
                var multiplier = row < 5 ? 1 : row < 10 ? 3 : 9;
                waves[row] = new WaveDefinitionDto
                {
                    id = "wave." + (row + 1).ToString("00"),
                    index = row + 1,
                    healthMultiplier = health[row],
                    speedMultiplier = speed[row],
                    spawnIntervalSeconds = spawn[row],
                    completionReward = rewards[row],
                    enemyIds = ExpandWave(counts, row, multiplier),
                };
            }
            return waves;
        }

        private static string[] ExpandWave(int[,] counts, int row, int multiplier)
        {
            var enemyOrder = new[]
            {
                BattleContentIds.Enemies.Normal, BattleContentIds.Enemies.Runner,
                BattleContentIds.Enemies.Armored, BattleContentIds.Enemies.Boss,
            };
            var remaining = new int[enemyOrder.Length];
            var total = 0;
            for (var index = 0; index < remaining.Length; index++)
            {
                remaining[index] = counts[row, index] * multiplier;
                total += remaining[index];
            }
            var sequence = new List<string>(total);
            while (sequence.Count < total)
            {
                for (var index = 0; index < remaining.Length; index++)
                {
                    if (remaining[index] <= 0) continue;
                    sequence.Add(enemyOrder[index]);
                    remaining[index]--;
                }
            }
            return sequence.ToArray();
        }
    }
}
