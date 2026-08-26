using System;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CombatFrameworkSmoke
    {
        private const string PowerStatus = "status.test.power";

        public static void Run()
        {
            var authored = BundledBattleContentFactory.Create();
            authored.statuses = authored.statuses.Concat(new[]
            {
                new StatusDefinitionDto
                {
                    id = PowerStatus,
                    kindId = "status-kind.modifier",
                    stackingMode = "stacking.additive",
                    durationSeconds = .15f,
                    magnitude = 1f,
                    maxStacks = 3,
                    polarityId = "polarity.buff",
                    tags = new[] { "status.test", "status.buff" },
                    modifiers = new[]
                    {
                        new StatusModifierDefinitionDto
                        {
                            attributeId = "attribute.damage",
                            operationId = "modifier.multiplicative",
                            value = 1.25f,
                        },
                    },
                },
            }).ToArray();
            var catalog = Compile(authored);
            var simulation = new GameSimulation(catalog, 9127);
            var plant = new Plant
            {
                Id = simulation.State.NextId++,
                DefinitionId = BattleContentIds.Plants.Pea,
                Star = 1,
                PotId = simulation.State.Pots.OrderBy(value => value.Id).First().Id,
                NurseryIndex = -1,
            };
            simulation.State.Plants.Add(plant);

            simulation.ApplyStatus(plant, PowerStatus, plant.Id);
            simulation.ApplyStatus(plant, PowerStatus, plant.Id);
            var status = plant.Statuses.Single(value => value.DefinitionId == PowerStatus);
            var baseDamage = catalog.Plants[plant.DefinitionId].damage;
            Assert(status.StackCount == 2
                && Mathf.Approximately(simulation.GetEffectiveAttribute(plant,
                    CombatAttributeKind.Damage, baseDamage), baseDamage * 1.25f * 1.25f),
                "shared status resolver applies deterministic stacked modifiers");
            Assert(simulation.RemoveStatuses(plant.Id, polarity: CombatStatusPolarity.Buff) == 1,
                "status removal filters the shared entity runtime by polarity");
            Assert(plant.AbilityRuntimes.Count == 1
                && plant.AbilityRuntimes[0].AbilityId == BattleContentIds.Abilities.PeaAttack,
                "combat events initialize Ability state from the cached loadout");
            Debug.Log("FRUIT_DEFENSE_COMBAT_FRAMEWORK_OK");
        }

        private static CompiledBattleContentCatalog Compile(BattleContentCatalogDto authored)
        {
            if (BattleContentCompiler.TryCompile(authored, out var compiled, out var validation))
                return compiled;
            throw new InvalidOperationException("Combat framework fixture compile failed:\n"
                + string.Join("\n", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Combat framework validation failed: " + message);
        }
    }
}
