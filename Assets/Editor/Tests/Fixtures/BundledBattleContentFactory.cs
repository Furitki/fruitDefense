using System;
using System.Collections.Generic;
using System.Linq;

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
                abilities = CreateAbilities(),
                projectiles = CreateProjectiles(),
                statuses = CreateStatuses(),
                waves = CreateWaves(),
                upgradeProfiles = CreateUpgradeProfiles(),
                nurseryProfiles = CreateNurseryProfiles(),
                battleRules = CreateBattleRules(),
            };
        }

        private static PlantDefinitionDto[] CreatePlants()
        {
            return new[]
            {
                Plant(BattleContentIds.Plants.Pea, "\u8c4c\u8c46", "\u7a33\u5b9a\u7684\u5355\u4f53\u8fdc\u7a0b\u8f93\u51fa",
                    12f, 44f, 6f, BattleContentIds.Abilities.PeaAttack, AllEquipment),
                Plant(BattleContentIds.Plants.Watermelon, "\u897f\u74dc", "\u4f4e\u9891\u8303\u56f4\u7206\u70b8\u4f24\u5bb3",
                    12f, 44f, 6f, BattleContentIds.Abilities.WatermelonAttack, AllEquipment),
                Plant(BattleContentIds.Plants.Banana, "\u9999\u8549", "\u76f4\u7ebf\u5f80\u8fd4\u7a7f\u900f\u653b\u51fb",
                    6f, 38f, 7f, BattleContentIds.Abilities.BananaAttack, AllEquipment),
                Plant(BattleContentIds.Plants.Durian, "\u69b4\u83b2", "\u8fd1\u6218\u8303\u56f4\u7838\u51fb",
                    12f, 18f, 5f, BattleContentIds.Abilities.DurianAttack,
                    new[] { BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili }),
                Plant(BattleContentIds.Plants.Sunflower, "\u5411\u65e5\u8475", "\u5468\u671f\u4ea7\u751f\u9633\u5149",
                    0f, 0f, 5f, BattleContentIds.Abilities.SunflowerProduce,
                    new[] { BattleContentIds.Equipment.Ice, BattleContentIds.Equipment.Chili }),
            };
        }

        private static PlantDefinitionDto Plant(string id, string displayName, string description, float damage,
            float range, float potVisualHeightOffset, string abilityId,
            string[] allowedEquipmentIds)
        {
            return new PlantDefinitionDto
            {
                id = id,
                presentationId = PlantPresentation(id),
                upgradeProfileId = BattleContentIds.UpgradeProfiles.Baseline,
                displayName = displayName,
                description = description,
                damage = damage,
                rangeLegacyUnits = range,
                potVisualHeightOffset = potVisualHeightOffset,
                abilityIds = new[] { abilityId },
                tags = id == BattleContentIds.Plants.Sunflower
                    ? new[] { "plant.producer" }
                    : id == BattleContentIds.Plants.Durian
                        ? new[] { "plant.damage", "plant.melee", "plant.area" }
                        : new[] { "plant.damage", "plant.ranged", "plant.projectile" },
                allowedEquipmentIds = (string[])allowedEquipmentIds.Clone(),
            };
        }

        private static string PlantPresentation(string id)
        {
            if (id == BattleContentIds.Plants.Pea) return BattleContentIds.Presentation.PlantPea;
            if (id == BattleContentIds.Plants.Watermelon) return BattleContentIds.Presentation.PlantWatermelon;
            if (id == BattleContentIds.Plants.Banana) return BattleContentIds.Presentation.PlantBanana;
            if (id == BattleContentIds.Plants.Durian) return BattleContentIds.Presentation.PlantDurian;
            if (id == BattleContentIds.Plants.Sunflower) return BattleContentIds.Presentation.PlantSunflower;
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown bundled plant ID.");
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
                id = id, presentationId = EnemyPresentation(id), displayName = name,
                health = health, speedLegacyUnits = speed,
                killReward = reward, threat = threat,
                tags = id == BattleContentIds.Enemies.Runner
                    ? new[] { "enemy", "enemy.fast" }
                    : id == BattleContentIds.Enemies.Armored
                        ? new[] { "enemy", "enemy.armored" }
                        : id == BattleContentIds.Enemies.Boss
                            ? new[] { "enemy", "enemy.boss" }
                            : new[] { "enemy", "enemy.normal" },
            };
        }

        private static string EnemyPresentation(string id)
        {
            if (id == BattleContentIds.Enemies.Normal) return BattleContentIds.Presentation.EnemyNormal;
            if (id == BattleContentIds.Enemies.Runner) return BattleContentIds.Presentation.EnemyRunner;
            if (id == BattleContentIds.Enemies.Armored) return BattleContentIds.Presentation.EnemyArmored;
            if (id == BattleContentIds.Enemies.Boss) return BattleContentIds.Presentation.EnemyBoss;
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown bundled enemy ID.");
        }

        private static EquipmentDefinitionDto[] CreateEquipment()
        {
            return new[]
            {
                new EquipmentDefinitionDto
                {
                    id = BattleContentIds.Equipment.Gatling,
                    presentationId = BattleContentIds.Presentation.EquipmentGatling,
                    displayName = "\u673a\u67aa",
                    modifiers = new[]
                    {
                        new AbilityModifierDefinitionDto
                        {
                            id = "modifier.gatling.burst",
                            requiredPlantTag = "plant.ranged",
                            targetAbilityTag = "ability.ranged.projectile",
                            attributeId = "ability-attribute.burst-count",
                            operationId = "ability-modifier.override",
                            value = 4f,
                        },
                        new AbilityModifierDefinitionDto
                        {
                            id = "modifier.gatling.burst-interval",
                            requiredPlantTag = "plant.ranged",
                            targetAbilityTag = "ability.ranged.projectile",
                            attributeId = "ability-attribute.burst-interval",
                            operationId = "ability-modifier.override",
                            value = .2f,
                        },
                    },
                    compatiblePlantIds = new[]
                    {
                        BattleContentIds.Plants.Pea, BattleContentIds.Plants.Watermelon, BattleContentIds.Plants.Banana,
                    },
                },
                new EquipmentDefinitionDto
                {
                    id = BattleContentIds.Equipment.Ice,
                    presentationId = BattleContentIds.Presentation.EquipmentIce,
                    displayName = "\u51b0\u5757",
                    grants = new[]
                    {
                        new AbilityGrantDefinitionDto { abilityId = BattleContentIds.Abilities.IceOnHit, requiredPlantTag = "plant.damage" },
                        new AbilityGrantDefinitionDto { abilityId = BattleContentIds.Abilities.IceProducerOpening, requiredPlantTag = "plant.producer" },
                    },
                    compatiblePlantIds = (string[])AllPlants.Clone(),
                },
                new EquipmentDefinitionDto
                {
                    id = BattleContentIds.Equipment.Chili,
                    presentationId = BattleContentIds.Presentation.EquipmentChili,
                    displayName = "\u8fa3\u6912",
                    grants = new[]
                    {
                        new AbilityGrantDefinitionDto { abilityId = BattleContentIds.Abilities.ChiliOnHit, requiredPlantTag = "plant.damage" },
                    },
                    modifiers = new[]
                    {
                        new AbilityModifierDefinitionDto
                        {
                            id = "modifier.chili.producer-resource",
                            requiredPlantTag = "plant.producer",
                            targetAbilityTag = "ability.producer",
                            attributeId = "ability-attribute.resource-amount",
                            operationId = "ability-modifier.add",
                            value = 1f,
                        },
                    },
                    compatiblePlantIds = (string[])AllPlants.Clone(),
                },
            };
        }

        private static AbilityDefinitionDto[] CreateAbilities()
        {
            return new[]
            {
                ProjectileAbility(BattleContentIds.Abilities.PeaAttack,
                    BattleContentIds.Projectiles.Pea, 1f, 0f),
                ProjectileAbility(BattleContentIds.Abilities.WatermelonAttack,
                    BattleContentIds.Projectiles.Watermelon, 2.2f, 7f),
                ProjectileAbility(BattleContentIds.Abilities.BananaAttack,
                    BattleContentIds.Projectiles.Banana, 1.6f, 0f),
                new AbilityDefinitionDto
                {
                    id = BattleContentIds.Abilities.DurianAttack,
                    activation = CooldownActivation(1.8f),
                    timeline = new AbilityTimelineDefinitionDto
                    {
                        windupSeconds = .4f,
                        recoverySeconds = .3f,
                    },
                    damageMultiplier = 1f,
                    tags = new[] { "ability.damage", "ability.area", "ability.melee" },
                    deliveries = new[]
                    {
                        InstantDelivery("target.area", 18f, Payload("effect.damage")),
                    },
                },
                new AbilityDefinitionDto
                {
                    id = BattleContentIds.Abilities.SunflowerProduce,
                    activation = new AbilityActivationDefinitionDto
                    {
                        kindId = "activation.periodic",
                        ownerRoleId = "owner.any",
                        periodSeconds = 10f,
                    },
                    timeline = new AbilityTimelineDefinitionDto { recoverySeconds = .55f },
                    damageMultiplier = 0f,
                    tags = new[] { "ability.producer" },
                    deliveries = new[]
                    {
                        InstantDelivery("target.self", 0f,
                            Payload("effect.grant-resource", resource: 1)),
                    },
                },
                new AbilityDefinitionDto
                {
                    id = BattleContentIds.Abilities.IceOnHit,
                    activation = CombatEventActivation("event.after-damage-dealt", "owner.event-source"),
                    damageMultiplier = 0f,
                    tags = new[] { "ability.equipment", "ability.on-hit" },
                    deliveries = new[]
                    {
                        InstantDelivery("target.event-target", 0f,
                            Payload("effect.apply-status", BattleContentIds.Statuses.IceSlow),
                            Payload("effect.apply-status", BattleContentIds.Statuses.IceCount)),
                    },
                },
                new AbilityDefinitionDto
                {
                    id = BattleContentIds.Abilities.IceProducerOpening,
                    activation = CombatEventActivation("event.wave-first-spawned", "owner.any"),
                    timeline = new AbilityTimelineDefinitionDto { recoverySeconds = .55f },
                    damageMultiplier = 0f,
                    tags = new[] { "ability.equipment", "ability.producer" },
                    deliveries = new[]
                    {
                        InstantDelivery("target.all-enemies", 0f,
                            Payload("effect.apply-status", BattleContentIds.Statuses.IceSlow)),
                    },
                },
                new AbilityDefinitionDto
                {
                    id = BattleContentIds.Abilities.ChiliOnHit,
                    activation = CombatEventActivation("event.after-damage-dealt", "owner.event-source"),
                    damageMultiplier = 0f,
                    tags = new[] { "ability.equipment", "ability.on-hit" },
                    deliveries = new[]
                    {
                        InstantDelivery("target.event-target", 0f,
                            Payload("effect.apply-status", BattleContentIds.Statuses.ChiliBurn, .2f)),
                    },
                },
            };
        }

        private static AbilityDefinitionDto ProjectileAbility(string id, string projectileId,
            float cooldown, float radius)
        {
            return new AbilityDefinitionDto
            {
                id = id,
                activation = CooldownActivation(cooldown),
                damageMultiplier = 1f,
                tags = new[] { "ability.ranged.projectile", "ability.damage" },
                deliveries = new[]
                {
                    new AbilityDeliveryDefinitionDto
                    {
                        targetId = projectileId == BattleContentIds.Projectiles.Banana
                            ? "target.line" : "target.front",
                        modeId = "delivery.projectile",
                        projectileId = projectileId,
                        radiusLegacyUnits = radius,
                        payload = new[] { Payload("effect.damage") },
                    },
                },
            };
        }

        private static AbilityActivationDefinitionDto CooldownActivation(float seconds)
        {
            return new AbilityActivationDefinitionDto
            {
                kindId = "activation.cooldown",
                ownerRoleId = "owner.any",
                cooldownSeconds = seconds,
            };
        }

        private static AbilityActivationDefinitionDto CombatEventActivation(string eventId,
            string ownerRoleId)
        {
            return new AbilityActivationDefinitionDto
            {
                kindId = "activation.combat-event",
                eventId = eventId,
                ownerRoleId = ownerRoleId,
            };
        }

        private static AbilityDeliveryDefinitionDto InstantDelivery(string targetId, float radius,
            params AbilityPayloadEffectDefinitionDto[] payload)
        {
            return new AbilityDeliveryDefinitionDto
            {
                targetId = targetId,
                modeId = "delivery.instant",
                radiusLegacyUnits = radius,
                payload = payload,
            };
        }

        private static AbilityPayloadEffectDefinitionDto Payload(string kindId, string statusId = "",
            float magnitude = 1f, int resource = 0)
        {
            return new AbilityPayloadEffectDefinitionDto
            {
                kindId = kindId,
                statusId = statusId,
                magnitude = magnitude,
                resourceAmount = resource,
            };
        }

        private static ProjectileDefinitionDto[] CreateProjectiles()
        {
            return new[]
            {
                new ProjectileDefinitionDto
                {
                    id = BattleContentIds.Projectiles.Pea,
                    presentationId = BattleContentIds.Presentation.ProjectilePea,
                    travelMode = "travel.tracking",
                    speedLegacyUnits = 65f,
                    hitRadiusLegacyUnits = 2.25f,
                },
                new ProjectileDefinitionDto
                {
                    id = BattleContentIds.Projectiles.Watermelon,
                    presentationId = BattleContentIds.Presentation.ProjectileWatermelon,
                    travelMode = "travel.timed-arc",
                    flightSeconds = .4f,
                    hitRadiusLegacyUnits = 2.25f,
                },
                new ProjectileDefinitionDto
                {
                    id = BattleContentIds.Projectiles.Banana,
                    presentationId = BattleContentIds.Presentation.ProjectileBanana,
                    travelMode = "travel.linear-return",
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
                    kindId = "status-kind.slow",
                    stackingMode = "stacking.refresh",
                    durationSeconds = 2f,
                    magnitude = .55f,
                    polarityId = "polarity.debuff",
                    tags = new[] { "status.control", "status.ice", "status.slow" },
                    modifiers = new[]
                    {
                        new StatusModifierDefinitionDto
                        {
                            attributeId = "attribute.move-speed",
                            operationId = "modifier.multiplicative",
                            value = .55f,
                        },
                    },
                },
                new StatusDefinitionDto
                {
                    id = BattleContentIds.Statuses.IceFreeze,
                    kindId = "status-kind.freeze",
                    stackingMode = "stacking.refresh",
                    durationSeconds = 1f,
                    magnitude = 1f,
                    polarityId = "polarity.debuff",
                    tags = new[] { "status.control", "status.freeze", "status.ice" },
                    blocksMovement = true,
                },
                new StatusDefinitionDto
                {
                    id = BattleContentIds.Statuses.IceCount,
                    kindId = "status-kind.hit-count",
                    stackingMode = "stacking.proc-after-hits",
                    durationSeconds = 99999f,
                    magnitude = 1f,
                    maxStacks = 5,
                    hitsToProc = 5,
                    procStatusId = BattleContentIds.Statuses.IceFreeze,
                    polarityId = "polarity.neutral",
                    tags = new[] { "status.counter", "status.ice" },
                },
                new StatusDefinitionDto
                {
                    id = BattleContentIds.Statuses.ChiliBurn,
                    kindId = "status-kind.burn",
                    stackingMode = "stacking.independent",
                    durationSeconds = 3f,
                    tickIntervalSeconds = .05f,
                    magnitude = .2f,
                    maxStacks = 3,
                    polarityId = "polarity.debuff",
                    tags = new[] { "status.burn", "status.damage-over-time" },
                    periodicEffectId = "periodic.damage",
                },
            };
        }

        private static UpgradeProfileDefinitionDto[] CreateUpgradeProfiles()
        {
            return new[]
            {
                new UpgradeProfileDefinitionDto
                {
                    id = BattleContentIds.UpgradeProfiles.Baseline,
                    tiers = new[]
                    {
                        Tier(1, 1f, 1f, 1f),
                        Tier(2, 1.5f, 1.05f, 1.05f),
                        Tier(3, 3f, 1.1f, 1.1f),
                        Tier(4, 5f, 1.2f, 1.15f),
                    },
                },
            };
        }

        private static UpgradeTierDefinitionDto Tier(int tier, float damage, float speed, float range)
        {
            return new UpgradeTierDefinitionDto
            {
                tier = tier,
                damageMultiplier = damage,
                attackSpeedMultiplier = speed,
                rangeMultiplier = range,
            };
        }

        private static NurseryProfileDefinitionDto[] CreateNurseryProfiles()
        {
            return new[]
            {
                new NurseryProfileDefinitionDto
                {
                    id = BattleContentIds.NurseryProfiles.Baseline,
                    entries = AllPlants.Select(id => new NurseryEntryDefinitionDto
                    {
                        plantId = id,
                        weight = 1,
                    }).ToArray(),
                    potChance = .1f,
                    firstRefreshGuaranteedTag = "plant.damage",
                    firstRefreshGuaranteedCount = 2,
                    cappedTag = "plant.producer",
                    maxCappedTagCount = 2,
                },
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
                nurseryProfileId = BattleContentIds.NurseryProfiles.Baseline,
                relocationCooldownSeconds = 2f,
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
