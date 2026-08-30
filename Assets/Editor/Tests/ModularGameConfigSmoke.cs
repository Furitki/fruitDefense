using System;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Presentation;
using UnityEngine;

namespace FruitDefense.Editor
{
    internal static class ModularGameConfigSmoke
    {
        private const string RapidPeaId = "plant.test.pea-rapid";
        private const string RapidPeaAbilityId = "ability.test.pea-rapid.attack";
        private const string RapidUpgradeProfileId = "upgrade.test.pea-rapid";
        private const string RapidNurseryProfileId = "nursery.test.pea-rapid";

        public static void Run()
        {
            var authored = CreateVariantCatalog();
            var validation = BattleContentValidator.Validate(authored);
            Assert(validation.IsValid, "Variant catalog validation failed: "
                + string.Join(" | ", validation.Issues.Select(value => value.ToString()).ToArray()));
            Assert(BattleContentCompiler.TryCompile(authored, out var content,
                    out var compileValidation),
                "Variant catalog compilation failed: " + string.Join(" | ",
                    compileValidation.Issues.Select(value => value.ToString()).ToArray()));

            ValidateSharedPresentationWithIndependentParameters(content);
            ValidateNurseryProfile(content);
            ValidateMergeIdentityAndMaximumTier(content);
            ValidateReferenceDiagnostics(authored);
            Debug.Log("FRUIT_DEFENSE_MODULAR_GAME_CONFIG_OK");
        }

        private static BattleContentCatalogDto CreateVariantCatalog()
        {
            var authored = BundledBattleContentFactory.Create();
            var pea = authored.plants.Single(value => value.id == BattleContentIds.Plants.Pea);
            var rapidAbility = JsonUtility.FromJson<AbilityDefinitionDto>(JsonUtility.ToJson(
                authored.abilities.Single(value =>
                    value.id == BattleContentIds.Abilities.PeaAttack)));
            rapidAbility.id = RapidPeaAbilityId;
            rapidAbility.activation.cooldownSeconds = .55f;
            authored.abilities = authored.abilities.Concat(new[] { rapidAbility }).ToArray();
            authored.upgradeProfiles = authored.upgradeProfiles.Concat(new[]
            {
                new UpgradeProfileDefinitionDto
                {
                    id = RapidUpgradeProfileId,
                    tiers = new[]
                    {
                        Tier(1, 1f, 1f, 1f),
                        Tier(2, 1.35f, 1.25f, 1.05f),
                        Tier(3, 1.8f, 1.6f, 1.1f),
                    },
                },
            }).ToArray();
            authored.plants = authored.plants.Concat(new[]
            {
                new PlantDefinitionDto
                {
                    id = RapidPeaId,
                    presentationId = pea.presentationId,
                    upgradeProfileId = RapidUpgradeProfileId,
                    displayName = "迅捷豌豆",
                    description = "与豌豆共用外观的独立测试变体",
                    damage = 7f,
                    rangeLegacyUnits = 32f,
                    potVisualHeightOffset = pea.potVisualHeightOffset,
                    abilityIds = new[] { RapidPeaAbilityId },
                    tags = pea.tags.ToArray(),
                    allowedEquipmentIds = pea.allowedEquipmentIds.ToArray(),
                },
            }).ToArray();
            authored.nurseryProfiles = authored.nurseryProfiles.Concat(new[]
            {
                new NurseryProfileDefinitionDto
                {
                    id = RapidNurseryProfileId,
                    entries = new[]
                    {
                        new NurseryEntryDefinitionDto { plantId = RapidPeaId, weight = 1 },
                    },
                    potChance = 0f,
                    firstRefreshGuaranteedTag = "plant.damage",
                    firstRefreshGuaranteedCount = 1,
                    cappedTag = string.Empty,
                    maxCappedTagCount = 0,
                },
            }).ToArray();
            authored.battleRules.nurseryProfileId = RapidNurseryProfileId;
            authored.battleRules.nurserySlotCount = 4;
            authored.battleRules.relocationCooldownSeconds = 3.25f;
            authored.battleRules.refreshBaseCost = 13;
            authored.battleRules.refreshCostStep = 7;
            return authored;
        }

        private static UpgradeTierDefinitionDto Tier(int tier, float damage,
            float speed, float range)
        {
            return new UpgradeTierDefinitionDto
            {
                tier = tier,
                damageMultiplier = damage,
                attackSpeedMultiplier = speed,
                rangeMultiplier = range,
            };
        }

        private static void ValidateSharedPresentationWithIndependentParameters(
            CompiledBattleContentCatalog content)
        {
            var baseline = content.Plants[BattleContentIds.Plants.Pea];
            var rapid = content.Plants[RapidPeaId];
            Assert(baseline.presentationId == rapid.presentationId,
                "Test variants must share one presentation ID.");
            Assert(BattlePresentationVisualCatalog.Plant(baseline.presentationId)
                    == BattlePresentationVisualCatalog.Plant(rapid.presentationId),
                "Shared presentation ID did not resolve to one visual archetype.");
            var baselineInterval = BattleAbilityTiming.TicksToSeconds(content
                .ResolvePlantAbilities(baseline.id, string.Empty)[0].Activation.CooldownTicks);
            var rapidInterval = BattleAbilityTiming.TicksToSeconds(content
                .ResolvePlantAbilities(rapid.id, string.Empty)[0].Activation.CooldownTicks);
            Assert(!Mathf.Approximately(baseline.damage, rapid.damage)
                && !Mathf.Approximately(baselineInterval, rapidInterval)
                && baseline.upgradeProfileId != rapid.upgradeProfileId,
                "Shared-looking variants did not retain independent gameplay parameters.");
            Assert(content.PlantMaximumTier(BattleContentIds.Plants.Pea) == 4
                && content.PlantMaximumTier(RapidPeaId) == 3,
                "Variant-specific maximum tiers were not resolved from upgrade profiles.");
        }

        private static void ValidateNurseryProfile(CompiledBattleContentCatalog content)
        {
            var simulation = new GameSimulation(content, 7137);
            var replay = new GameSimulation(content, 7137);
            simulation.State.Sun = replay.State.Sun = 1000;
            Assert(simulation.RefreshCost(0) == 13 && simulation.RefreshCost(1) == 20,
                "Refresh cost did not resolve from active battle rules.");
            Assert(simulation.RefreshNursery(out var reason)
                && replay.RefreshNursery(out var replayReason),
                "Variant nursery refresh failed: " + reason);
            Assert(simulation.State.Plants.Count == simulation.NurserySlotCount
                && simulation.State.Plants.All(value => value.DefinitionId == RapidPeaId),
                "Nursery profile did not drive the generated plant pool.");
            Assert(simulation.NurserySlotCount == 4
                && simulation.State.Sun == 987
                && simulation.State.Plants.Select(PlantRoll).SequenceEqual(
                    replay.State.Plants.Select(PlantRoll), StringComparer.Ordinal)
                && simulation.LastNurseryPotSlots.SequenceEqual(replay.LastNurseryPotSlots),
                "Nursery profile/rules did not replay deterministically.");
        }

        private static string PlantRoll(Plant plant)
        {
            return plant.DefinitionId + ":" + plant.NurseryIndex;
        }

        private static void ValidateMergeIdentityAndMaximumTier(
            CompiledBattleContentCatalog content)
        {
            var simulation = new GameSimulation(content, 8138);
            simulation.State.Plants.Clear();
            simulation.State.Pots.Clear();
            simulation.State.Pots.Add(new Pot { Id = 1, Active = true });
            simulation.State.Pots.Add(new Pot { Id = 2, Active = true });
            simulation.State.Pots.Add(new Pot { Id = 3, Active = true });
            simulation.State.Plants.Add(new Plant
            {
                Id = 1, DefinitionId = RapidPeaId, PotId = 1, Star = 1,
            });
            simulation.State.Plants.Add(new Plant
            {
                Id = 2, DefinitionId = BattleContentIds.Plants.Pea, PotId = 2, Star = 1,
            });
            var crossVariant = simulation.GetPlantDropStatus(1, 2);
            Assert(crossVariant.Legal && crossVariant.Action == PlantDropAction.Swap,
                "Same-looking variants were incorrectly treated as merge-identical.");

            simulation.State.Plants[1].DefinitionId = RapidPeaId;
            var sameVariant = simulation.GetPlantDropStatus(1, 2);
            Assert(sameVariant.Legal && sameVariant.Action == PlantDropAction.Merge,
                "Matching variant IDs and tiers did not merge.");
            simulation.State.Plants[0].Star = 3;
            simulation.State.Plants[1].Star = 3;
            var atMaximum = simulation.GetPlantDropStatus(1, 2);
            Assert(atMaximum.Legal && atMaximum.Action == PlantDropAction.Swap,
                "Variant merge exceeded its configured maximum tier.");

            simulation.State.Phase = GamePhase.Playing;
            Assert(simulation.MoveOrMergePlant(1, 3, out var reason)
                && Mathf.Approximately(simulation.State.Plants[0].MoveCooldown, 3.25f),
                "Relocation cooldown did not resolve from active battle rules: " + reason);
        }

        private static void ValidateReferenceDiagnostics(BattleContentCatalogDto source)
        {
            var invalid = BattleContentJson.DeepCopy(source);
            invalid.plants.Single(value => value.id == RapidPeaId).upgradeProfileId =
                "upgrade.missing";
            invalid.battleRules.nurseryProfileId = "nursery.missing";
            var validation = BattleContentValidator.Validate(invalid);
            Assert(!validation.IsValid
                && validation.Issues.Count(value => value.code == "reference.missing") >= 2,
                "Dangling config references did not produce complete diagnostics.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
