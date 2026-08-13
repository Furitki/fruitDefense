using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class ComposableBattleSkillsSmoke
    {
        public static void Run()
        {
            var catalog = Compile(BundledBattleContentFactory.Create());
            ValidateCompiledMechanisms(catalog);
            ValidateStructuredFailures();
            ValidatePlantParity(catalog);
            ValidateRepeatedBasicAttacks();
            ValidateEquipmentAndStatuses(catalog);
            ValidateMilestones(catalog);
            ValidateDataOnlyPlant();
            Debug.Log("FRUIT_DEFENSE_COMPOSABLE_SKILLS_OK");
        }

        private static void ValidateCompiledMechanisms(CompiledBattleContentCatalog catalog)
        {
            Assert(catalog.RuntimeSkills[BattleContentIds.Skills.PeaAttack].Trigger == BattleTriggerKind.CooldownReady,
                "pea trigger compiled");
            Assert(catalog.RuntimeProjectiles[BattleContentIds.Projectiles.Pea].Mode == BattleProjectileMode.Tracking,
                "tracking projectile compiled");
            Assert(catalog.RuntimeProjectiles[BattleContentIds.Projectiles.Watermelon].FlightTicks == 8,
                "0.4 second arc compiled to eight fixed ticks");
            Assert(catalog.RuntimeStatuses[BattleContentIds.Statuses.IceSlow].DurationTicks == 40,
                "two second slow compiled to fixed ticks");

            var gatling = catalog.ResolvePlantSkills(BattleContentIds.Plants.Pea, BattleContentIds.Equipment.Gatling)
                .Single(skill => skill.Id == BattleContentIds.Skills.PeaAttack);
            Assert(gatling.BurstCount == 4 && gatling.BurstIntervalTicks == 4,
                "gatling modifier resolves to four shots at 0.2 seconds");
            var iceProducer = catalog.ResolvePlantSkills(BattleContentIds.Plants.Sunflower, BattleContentIds.Equipment.Ice);
            Assert(iceProducer.Any(skill => skill.Id == BattleContentIds.Skills.IceProducerOpening)
                && iceProducer.All(skill => skill.Id != BattleContentIds.Skills.IceOnHit),
                "ice grants are selected by producer tag");
            var iceProducerPassives = catalog.ResolvePlantPassives(
                BattleContentIds.Plants.Sunflower, BattleContentIds.Equipment.Ice);
            Assert(iceProducerPassives.Any(value => value.Id == BattleContentIds.Passives.IceProducerOpening)
                && iceProducerPassives.All(value => value.Id != BattleContentIds.Passives.IceOnHit),
                "ice passives are selected independently by producer tag");
            var iceDamagePassives = catalog.ResolvePlantPassives(
                BattleContentIds.Plants.Pea, BattleContentIds.Equipment.Ice);
            Assert(iceDamagePassives.Single().Id == BattleContentIds.Passives.IceOnHit,
                "ice on-hit is a first-class passive instead of a polling skill");
            var chiliProducer = catalog.ResolvePlantSkills(BattleContentIds.Plants.Sunflower, BattleContentIds.Equipment.Chili)
                .Single(skill => skill.Id == BattleContentIds.Skills.SunflowerProduce);
            Assert(chiliProducer.ResourceAmount == 2, "chili producer modifier adds one resource");
        }

        private static void ValidateStructuredFailures()
        {
            var unknown = BundledBattleContentFactory.Create();
            unknown.skills[0].effects[0].kindId = "effect.unknown";
            var result = BattleContentValidator.Validate(unknown);
            Assert(result.Issues.Any(issue => issue.code == "mechanism.unknown" && issue.category == "skills"),
                "unknown effect rejected with structured issue");

            var zero = BundledBattleContentFactory.Create();
            zero.equipment.Single(value => value.id == BattleContentIds.Equipment.Gatling)
                .modifiers[0].targetSkillTag = "skill.no-match";
            result = BattleContentValidator.Validate(zero);
            Assert(result.Issues.Any(issue => issue.code == "modifier.match.zero"),
                "zero-match modifier rejected");

            var ambiguous = BundledBattleContentFactory.Create();
            var pea = ambiguous.plants.Single(value => value.id == BattleContentIds.Plants.Pea);
            pea.skillIds = new[] { BattleContentIds.Skills.PeaAttack, BattleContentIds.Skills.WatermelonAttack };
            result = BattleContentValidator.Validate(ambiguous);
            Assert(result.Issues.Any(issue => issue.code == "modifier.match.ambiguous"),
                "ambiguous modifier rejected");
        }

        private static void ValidatePlantParity(CompiledBattleContentCatalog catalog)
        {
            var pea = CreateScenario(catalog, BattleContentIds.Plants.Pea);
            pea.Step();
            Assert(pea.State.Projectiles.Count == 1 && pea.State.Projectiles[0].Mode == BattleProjectileMode.Tracking,
                "pea launches delayed tracking projectile");
            TickUntilProjectilesFinish(pea, 40);
            Assert(pea.State.Zombies[0].Hp < 1000f, "pea tracking damage resolves");

            var watermelon = CreateScenario(catalog, BattleContentIds.Plants.Watermelon);
            watermelon.Step();
            Assert(watermelon.State.Projectiles.Single().Mode == BattleProjectileMode.TimedArc
                && watermelon.State.Projectiles[0].Progress > 0f, "watermelon starts timed arc");
            for (var step = 0; step < 12; step++) watermelon.Step();
            Assert(watermelon.State.Zombies[0].Hp < 1000f, "watermelon area damage resolves");

            var banana = CreateScenario(catalog, BattleContentIds.Plants.Banana);
            banana.Step();
            SetSkillCooldown(banana.State.Plants[0], BattleContentIds.Skills.BananaAttack, 999f);
            TickUntilProjectilesFinish(banana, 90);
            Assert(Mathf.Approximately(banana.State.Zombies[0].Hp, 988f),
                "banana hits each target once outbound and once returning");

            var durian = CreateScenario(catalog, BattleContentIds.Plants.Durian);
            durian.Step();
            Assert(durian.State.Zombies[0].Hp == 988f
                && CountCues(durian, BattleContentIds.Cues.DurianDrop) == 1,
                "durian damage and cue resolve from effects");
        }

        private static void ValidateRepeatedBasicAttacks()
        {
            var resolution = BundledLevelCatalogFactory.CreateCompiled()
                .Resolve(BundledLevelCatalogIds.Levels.Orchard01);
            Assert(resolution.Succeeded, "bundled orchard level resolves for repeated-attack coverage");
            var attackerIds = new[]
            {
                BattleContentIds.Plants.Pea,
                BattleContentIds.Plants.Watermelon,
                BattleContentIds.Plants.Banana,
                BattleContentIds.Plants.Durian,
            };
            var frameDeltas = new[] { .016f, .033f, .011f, .025f };

            foreach (var attackerId in attackerIds)
            {
                var simulation = CreateScenario(resolution.Value, attackerId);
                var plant = simulation.State.Plants.Single();
                var enemy = simulation.State.Zombies.Single();
                var initialHp = enemy.Hp;
                var lastActionStartedAt = plant.ActionStartedAt;
                var actionCount = 0;

                for (var frame = 0; frame < 600; frame++)
                {
                    simulation.Tick(frameDeltas[frame % frameDeltas.Length]);
                    if (plant.ActionStartedAt <= lastActionStartedAt + .0001f) continue;
                    lastActionStartedAt = plant.ActionStartedAt;
                    actionCount++;
                }

                var baseDamage = resolution.Value.BattleContent.Plants[attackerId].damage;
                Assert(actionCount >= 3
                    && initialHp - enemy.Hp >= baseDamage * 3f - .001f,
                    attackerId + " repeatedly attacks and damages a durable in-range enemy under real frame ticks");
                Debug.Log("FRUIT_DEFENSE_REPEATED_ATTACK_OK plant=" + attackerId
                    + " actions=" + actionCount
                    + " damage=" + (initialHp - enemy.Hp).ToString("0.###"));
            }
        }

        private static void ValidateEquipmentAndStatuses(CompiledBattleContentCatalog catalog)
        {
            var ice = CreateScenario(catalog, BattleContentIds.Plants.Pea, BattleContentIds.Equipment.Ice);
            for (var hit = 0; hit < 5; hit++)
            {
                ice.State.Plants[0].SkillRuntimes.Clear();
                ice.State.Plants[0].AttackCooldown = 0f;
                ice.Step();
                TickUntilProjectilesFinish(ice, 40);
            }
            var iceZombie = ice.State.Zombies[0];
            Assert(iceZombie.IceHits == 0 && iceZombie.FreezeUntil > ice.State.Elapsed
                && iceZombie.Statuses.Any(status => status.DefinitionId == BattleContentIds.Statuses.IceFreeze),
                "fifth ice hit clears counter and applies freeze");

            var burn = CreateScenario(catalog, BattleContentIds.Plants.Durian, BattleContentIds.Equipment.Chili);
            for (var hit = 0; hit < 5; hit++)
            {
                burn.State.Plants[0].SkillRuntimes.Clear();
                burn.State.Plants[0].AttackCooldown = 0f;
                burn.Step();
            }
            var burnStatuses = burn.State.Zombies[0].Statuses
                .Where(status => status.DefinitionId == BattleContentIds.Statuses.ChiliBurn).ToArray();
            Assert(burnStatuses.Length == 3 && burnStatuses.Select(status => status.Sequence).SequenceEqual(
                burnStatuses.Select(status => status.Sequence).OrderBy(value => value)),
                "burn retains three newest independent instances deterministically");

            var gatling = CreateScenario(catalog, BattleContentIds.Plants.Pea, BattleContentIds.Equipment.Gatling);
            for (var step = 0; step < 17; step++)
                gatling.Step();
            var muzzleCues = CountCues(gatling, BattleContentIds.Cues.GatlingMuzzle);
            Assert(muzzleCues == 4 && gatling.State.Plants[0].BurstShotsRemaining == 0,
                "gatling emits four shots at fixed intervals");

            var chiliProducer = CreateScenario(catalog, BattleContentIds.Plants.Sunflower, BattleContentIds.Equipment.Chili);
            chiliProducer.State.Sun = 0;
            chiliProducer.State.Plants[0].ProductionProgress = 9.99f;
            chiliProducer.Step();
            Assert(chiliProducer.State.Sun == 2, "producer tag modifier grants two resources");

            var iceProducer = CreateScenario(catalog, BattleContentIds.Plants.Sunflower, BattleContentIds.Equipment.Ice);
            iceProducer.State.Zombies.Clear();
            iceProducer.State.WaveSpawned = 0;
            iceProducer.State.WaveTotal = catalog.Waves["wave.01"].enemyIds.Length;
            iceProducer.State.SpawnCooldown = 0f;
            iceProducer.Step();
            Assert(iceProducer.State.Zombies.Count > 0
                && iceProducer.State.Zombies.All(zombie => zombie.Statuses.Any(status =>
                    status.DefinitionId == BattleContentIds.Statuses.IceSlow)),
                "producer-tag ice grant applies opening slow without plant-ID branching");
        }

        private static void ValidateMilestones(CompiledBattleContentCatalog catalog)
        {
            var simulation = new GameSimulation(catalog, 9191);
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 12;
            simulation.State.WaveSpawned = 0;
            simulation.State.WaveTotal = 0;
            simulation.State.Zombies.Clear();
            simulation.Step();
            Assert(simulation.State.Inventory.Gatling == 1 && simulation.State.Inventory.Ice == 1
                && simulation.State.Inventory.Chili == 1 && simulation.State.Inventory.Pots == 1,
                "wave twelve milestone reads catalog reward data");
        }

        private static void ValidateDataOnlyPlant()
        {
            var authored = BundledBattleContentFactory.Create();
            const string skillId = "skill.test.frost-pea";
            const string plantId = "plant.test-frost-pea";
            authored.skills = authored.skills.Concat(new[]
            {
                new SkillDefinitionDto
                {
                    id = skillId,
                    triggerId = "trigger.cooldown",
                    targetId = "target.front",
                    cooldownSeconds = 1f,
                    damageMultiplier = 1f,
                    actionSeconds = .22f,
                    tags = new[] { "skill.damage", "skill.ranged.projectile" },
                    effects = new[]
                    {
                        new SkillEffectDefinitionDto
                        {
                            kindId = "effect.launch-projectile",
                            projectileId = BattleContentIds.Projectiles.Pea,
                            magnitude = 1f,
                        },
                        new SkillEffectDefinitionDto
                        {
                            kindId = "effect.apply-status",
                            statusId = BattleContentIds.Statuses.IceSlow,
                            magnitude = 1f,
                        },
                    },
                },
            }).ToArray();
            authored.plants = authored.plants.Concat(new[]
            {
                new PlantDefinitionDto
                {
                    id = plantId,
                    displayName = "Data-only frost pea",
                    description = "Smoke-only composition",
                    damage = 12f,
                    attackIntervalSeconds = 1f,
                    rangeLegacyUnits = 44f,
                    skillIds = new[] { skillId },
                    projectileId = BattleContentIds.Projectiles.Pea,
                    tags = new[] { "plant.damage", "plant.projectile", "plant.ranged" },
                },
            }).ToArray();
            var catalog = Compile(authored);
            var simulation = CreateScenario(catalog, plantId);
            simulation.Step();
            Assert(simulation.State.Projectiles.Count == 1
                && simulation.State.Zombies[0].Statuses.Any(status => status.DefinitionId == BattleContentIds.Statuses.IceSlow),
                "data-only plant composes existing trigger, target, projectile and status executors");
        }

        private static GameSimulation CreateScenario(CompiledBattleContentCatalog catalog, string plantId,
            string equipmentId = "")
        {
            return ConfigureScenario(new GameSimulation(catalog, 7777), plantId, equipmentId);
        }

        private static GameSimulation CreateScenario(ResolvedLevelDefinition resolvedLevel, string plantId,
            string equipmentId = "")
        {
            return ConfigureScenario(new GameSimulation(resolvedLevel, 7777), plantId, equipmentId);
        }

        private static GameSimulation ConfigureScenario(GameSimulation simulation, string plantId,
            string equipmentId)
        {
            simulation.State.Plants.Clear();
            simulation.State.Zombies.Clear();
            simulation.State.Projectiles.Clear();
            simulation.DiscardPendingPresentationEvents();
            simulation.State.Phase = GamePhase.Playing;
            simulation.State.WaveIndex = 1;
            simulation.State.WaveTotal = 1;
            simulation.State.WaveSpawned = 1;
            simulation.State.NextId = 10000;
            var pot = simulation.State.Pots[0];
            PlantKind kind;
            if (!LegacyBattleContentIds.TryPlantKindFromId(plantId, out kind)) kind = PlantKind.Pea;
            var weapon = string.IsNullOrEmpty(equipmentId) ? WeaponKind.None : LegacyBattleContentIds.WeaponKindFromId(equipmentId);
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                ContentId = plantId,
                EquipmentId = equipmentId,
                Kind = kind,
                Weapon = weapon,
                Star = 1,
                PotId = pot.Id,
                NurseryIndex = -1,
            });
            simulation.State.Zombies.Add(new Zombie
            {
                Id = 9002,
                ContentId = BattleContentIds.Enemies.Normal,
                Kind = ZombieKind.Normal,
                Hp = 1000f,
                MaxHp = 1000f,
                Speed = 0f,
                PathProgress = NearestPathProgress(simulation, simulation.PotPoint(pot)),
                Reward = 0,
                Threat = 1,
            });
            return simulation;
        }

        private static void SetSkillCooldown(Plant plant, string skillId, float seconds)
        {
            var ticks = BattleSkillTiming.SecondsToTicks(seconds);
            plant.SkillRuntimes.Single(runtime => runtime.SkillId == skillId).CooldownTicks = ticks;
            plant.AttackCooldown = BattleSkillTiming.TicksToSeconds(ticks);
        }

        private static int CountCues(GameSimulation simulation, string cueId)
        {
            var events = new List<BattlePresentationEvent>();
            simulation.DrainPresentationEvents(events);
            return events.Count(value => value.Kind == BattlePresentationEventKind.Cue
                && value.CueId == cueId);
        }

        private static float NearestPathProgress(GameSimulation simulation, Vector2 point)
        {
            var bestProgress = 0f;
            var bestDistance = float.MaxValue;
            var step = GameConfig.MapDistance(.25f);
            for (var progress = 0f; progress <= simulation.Map.Route.TotalLength; progress += step)
            {
                var distance = Vector2.SqrMagnitude(simulation.Map.Route.Sample(progress) - point);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestProgress = progress;
            }
            return bestProgress;
        }

        private static void TickUntilProjectilesFinish(GameSimulation simulation, int maxSteps)
        {
            for (var step = 0; step < maxSteps && simulation.State.Projectiles.Count > 0; step++) simulation.Step();
        }

        private static CompiledBattleContentCatalog Compile(BattleContentCatalogDto authored)
        {
            CompiledBattleContentCatalog compiled;
            ContentValidationResult validation;
            if (BattleContentCompiler.TryCompile(authored, out compiled, out validation)) return compiled;
            throw new InvalidOperationException("Catalog compile failed:\n"
                + string.Join("\n", validation.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Composable battle skill validation failed: " + message);
        }
    }
}
