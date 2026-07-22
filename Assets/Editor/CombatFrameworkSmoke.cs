using System;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CombatFrameworkSmoke
    {
        private const string PowerStatus = "status.test.power";
        private const string EnemyBuffStatus = "status.test.enemy-buff";
        private const string LoopStatus = "status.test.loop-marker";
        private const string DamagedPassive = "passive.test.after-damage-taken";
        private const string LoopPassive = "passive.test.status-loop";

        [MenuItem("Fruit Defense/Validate Combat Entity Passive Buff Framework")]
        public static void Run()
        {
            var catalog = Compile(CreateAuthoredCatalog());
            ValidateSharedEntityAndBuffRuntime(catalog);
            ValidateEnemyPassiveAndLoopBoundary(catalog);
            Debug.Log("FRUIT_DEFENSE_COMBAT_FRAMEWORK_OK");
        }

        private static void ValidateSharedEntityAndBuffRuntime(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog);
            var plant = simulation.State.Plants.Single();
            var enemy = simulation.State.Zombies.Single();
            Assert(ReferenceEquals(simulation.EntityById(plant.Id), plant)
                && ReferenceEquals(simulation.EntityById(enemy.Id), enemy)
                && simulation.CombatEntities().Select(value => value.Id).SequenceEqual(new[] { plant.Id, enemy.Id }),
                "plants and enemies share stable ordered entity lookup");

            var baseDamage = catalog.Plants[plant.ContentId].damage;
            simulation.ApplyStatus(plant, PowerStatus, plant.Id);
            simulation.ApplyStatus(plant, PowerStatus, plant.Id);
            var power = plant.Statuses.Single(value => value.DefinitionId == PowerStatus);
            Assert(power.StackCount == 2
                && Mathf.Approximately(simulation.GetEffectiveAttribute(plant,
                    CombatAttributeKind.Damage, baseDamage), baseDamage * 1.25f * 1.25f),
                "positive additive buff stacks through the shared attribute resolver");
            Assert(simulation.RemoveStatuses(plant.Id, polarity: CombatStatusPolarity.Buff) == 1
                && plant.Statuses.All(value => value.DefinitionId != PowerStatus),
                "status removal filters by polarity");

            simulation.ApplyStatus(plant, PowerStatus, plant.Id);
            for (var step = 0; step < 3; step++) simulation.Step();
            Assert(plant.Statuses.All(value => value.DefinitionId != PowerStatus),
                "plant-owned buffs expire on deterministic fixed ticks");
        }

        private static void ValidateEnemyPassiveAndLoopBoundary(CompiledBattleContentCatalog catalog)
        {
            var simulation = CreateScenario(catalog);
            var plant = simulation.State.Plants.Single();
            var enemy = simulation.State.Zombies.Single();

            simulation.ApplyStatus(enemy, LoopStatus, plant.Id);
            var marker = enemy.Statuses.Single(value => value.DefinitionId == LoopStatus);
            var loopRuntime = enemy.PassiveRuntimes.Single(value => value.PassiveId == LoopPassive);
            Assert(marker.StackCount == 2 && loopRuntime.LastRootEventSequence > 0,
                "recursive status passive activates once per root event and terminates");

            enemy.Statuses.Clear();
            plant.AttackCooldown = 0f;
            simulation.Step();
            for (var step = 0; step < 40 && simulation.State.Projectiles.Count > 0; step++) simulation.Step();
            var damagedRuntime = enemy.PassiveRuntimes.Single(value => value.PassiveId == DamagedPassive);
            Assert(damagedRuntime.LastRootEventSequence > 0
                && enemy.Statuses.Any(value => value.DefinitionId == EnemyBuffStatus),
                "enemy after-damage passive is dispatched from the same authored passive pipeline");
            Assert(simulation.State.NextCombatEventSequence > damagedRuntime.LastRootEventSequence,
                "root combat event sequences remain monotonic");
        }

        private static BattleContentCatalogDto CreateAuthoredCatalog()
        {
            var authored = BundledBattleContentFactory.Create();
            authored.statuses = authored.statuses.Concat(new[]
            {
                ModifierStatus(PowerStatus, "polarity.buff", .15f, 3,
                    "attribute.damage", "modifier.multiplicative", 1.25f),
                ModifierStatus(EnemyBuffStatus, "polarity.buff", 1f, 1,
                    "attribute.damage-taken", "modifier.multiplicative", .9f),
                ModifierStatus(LoopStatus, "polarity.neutral", 1f, 3,
                    "attribute.move-speed", "modifier.multiplicative", 1f),
            }).ToArray();
            authored.passives = authored.passives.Concat(new[]
            {
                new PassiveDefinitionDto
                {
                    id = DamagedPassive,
                    triggerId = "passive-trigger.after-damage-taken",
                    ownerRoleId = "owner.event-target",
                    targetId = "passive-target.self",
                    priority = 10,
                    tags = new[] { "passive.test", "passive.reactive" },
                    effects = new[] { StatusEffect(EnemyBuffStatus) },
                },
                new PassiveDefinitionDto
                {
                    id = LoopPassive,
                    triggerId = "passive-trigger.status-applied",
                    ownerRoleId = "owner.event-target",
                    targetId = "passive-target.self",
                    priority = 20,
                    tags = new[] { "passive.test", "passive.loop" },
                    effects = new[] { StatusEffect(LoopStatus) },
                },
            }).ToArray();
            var enemy = authored.enemies.Single(value => value.id == BattleContentIds.Enemies.Normal);
            enemy.passiveIds = new[] { DamagedPassive, LoopPassive };
            return authored;
        }

        private static StatusDefinitionDto ModifierStatus(string id, string polarity, float duration,
            int maxStacks, string attribute, string operation, float value)
        {
            return new StatusDefinitionDto
            {
                id = id,
                kindId = "status-kind.modifier",
                stackingMode = "stacking.additive",
                durationSeconds = duration,
                magnitude = 1f,
                maxStacks = maxStacks,
                polarityId = polarity,
                tags = new[] { "status.test", polarity == "polarity.buff" ? "status.buff" : "status.marker" },
                modifiers = new[]
                {
                    new StatusModifierDefinitionDto
                    {
                        attributeId = attribute,
                        operationId = operation,
                        value = value,
                    },
                },
            };
        }

        private static SkillEffectDefinitionDto StatusEffect(string statusId)
        {
            return new SkillEffectDefinitionDto
            {
                kindId = "effect.apply-status",
                statusId = statusId,
                magnitude = 1f,
            };
        }

        private static GameSimulation CreateScenario(CompiledBattleContentCatalog catalog)
        {
            var simulation = new GameSimulation(catalog, 9127);
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
            simulation.State.Plants.Add(new Plant
            {
                Id = 9001,
                ContentId = BattleContentIds.Plants.Pea,
                Kind = PlantKind.Pea,
                Star = 1,
                PotId = pot.Id,
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

        private static CompiledBattleContentCatalog Compile(BattleContentCatalogDto authored)
        {
            CompiledBattleContentCatalog compiled;
            ContentValidationResult validation;
            if (BattleContentCompiler.TryCompile(authored, out compiled, out validation)) return compiled;
            throw new InvalidOperationException("Combat framework fixture compile failed:\n"
                + string.Join("\n", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Combat framework validation failed: " + message);
        }
    }
}
