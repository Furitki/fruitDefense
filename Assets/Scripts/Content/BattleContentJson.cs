using System;
using System.Text;
using UnityEngine;

namespace FruitDefense.Content
{
    public static class BattleContentJson
    {
        public static BattleContentCatalogDto Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Catalog JSON is empty.", nameof(json));
            var catalog = JsonUtility.FromJson<BattleContentCatalogDto>(json);
            if (catalog == null) throw new InvalidOperationException("Catalog JSON could not be deserialized.");
            EnsureArrays(catalog);
            return catalog;
        }

        public static BattleContentCatalogDto DeepCopy(BattleContentCatalogDto source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Deserialize(JsonUtility.ToJson(source, false));
        }

        public static string SerializeCanonical(BattleContentCatalogDto source, bool prettyPrint = true)
        {
            var copy = DeepCopy(source);
            Canonicalize(copy);
            return NormalizeLineEndings(JsonUtility.ToJson(copy, prettyPrint)) + "\n";
        }

        public static byte[] SerializeCanonicalUtf8(BattleContentCatalogDto source, bool prettyPrint = true)
        {
            return new UTF8Encoding(false).GetBytes(SerializeCanonical(source, prettyPrint));
        }

        public static void Canonicalize(BattleContentCatalogDto catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            EnsureArrays(catalog);

            Array.Sort(catalog.plants, ComparePlant);
            Array.Sort(catalog.enemies, CompareEnemy);
            Array.Sort(catalog.equipment, CompareEquipment);
            Array.Sort(catalog.abilities, CompareAbility);
            Array.Sort(catalog.projectiles, CompareProjectile);
            Array.Sort(catalog.statuses, CompareStatus);
            Array.Sort(catalog.waves, CompareWave);
            Array.Sort(catalog.starTiers, CompareStarTier);

            foreach (var plant in catalog.plants)
            {
                if (plant == null) continue;
                plant.abilityIds = SortedStrings(plant.abilityIds);
                plant.tags = SortedStrings(plant.tags);
                plant.allowedEquipmentIds = SortedStrings(plant.allowedEquipmentIds);
            }

            foreach (var enemy in catalog.enemies)
            {
                if (enemy == null) continue;
                enemy.abilityIds = SortedStrings(enemy.abilityIds);
                enemy.tags = SortedStrings(enemy.tags);
            }

            foreach (var equipment in catalog.equipment)
            {
                if (equipment == null) continue;
                equipment.compatiblePlantIds = SortedStrings(equipment.compatiblePlantIds);
                equipment.grants = equipment.grants ?? Array.Empty<AbilityGrantDefinitionDto>();
                Array.Sort(equipment.grants, (left, right) => CompareIds(left == null ? null : left.abilityId, right == null ? null : right.abilityId));
                equipment.modifiers = equipment.modifiers ?? Array.Empty<AbilityModifierDefinitionDto>();
                Array.Sort(equipment.modifiers, (left, right) => CompareIds(left == null ? null : left.id, right == null ? null : right.id));
            }

            foreach (var status in catalog.statuses)
            {
                if (status == null) continue;
                status.tags = SortedStrings(status.tags);
                status.modifiers = status.modifiers ?? Array.Empty<StatusModifierDefinitionDto>();
            }

            foreach (var ability in catalog.abilities)
            {
                if (ability == null) continue;
                ability.tags = SortedStrings(ability.tags);
                ability.deliveries = ability.deliveries ?? Array.Empty<AbilityDeliveryDefinitionDto>();
                foreach (var delivery in ability.deliveries)
                    if (delivery != null) delivery.payload = delivery.payload
                        ?? Array.Empty<AbilityPayloadEffectDefinitionDto>();
            }

            foreach (var wave in catalog.waves)
                if (wave != null && wave.enemyIds == null) wave.enemyIds = Array.Empty<string>();

            if (catalog.battleRules != null)
            {
                var rewards = catalog.battleRules.milestoneRewards ?? Array.Empty<MilestoneRewardDefinitionDto>();
                Array.Sort(rewards, (left, right) => NullSafeInt(left == null ? int.MaxValue : left.wave,
                    right == null ? int.MaxValue : right.wave));
                foreach (var reward in rewards)
                    if (reward != null) reward.equipmentIds = SortedStrings(reward.equipmentIds);
                catalog.battleRules.milestoneRewards = rewards;
            }
        }

        private static void EnsureArrays(BattleContentCatalogDto catalog)
        {
            if (catalog.header == null) catalog.header = new BattleContentHeaderDto();
            if (catalog.plants == null) catalog.plants = Array.Empty<PlantDefinitionDto>();
            if (catalog.enemies == null) catalog.enemies = Array.Empty<EnemyDefinitionDto>();
            if (catalog.equipment == null) catalog.equipment = Array.Empty<EquipmentDefinitionDto>();
            if (catalog.abilities == null) catalog.abilities = Array.Empty<AbilityDefinitionDto>();
            if (catalog.projectiles == null) catalog.projectiles = Array.Empty<ProjectileDefinitionDto>();
            if (catalog.statuses == null) catalog.statuses = Array.Empty<StatusDefinitionDto>();
            if (catalog.waves == null) catalog.waves = Array.Empty<WaveDefinitionDto>();
            if (catalog.starTiers == null) catalog.starTiers = Array.Empty<StarTierDefinitionDto>();
            if (catalog.battleRules == null) catalog.battleRules = new BattleRulesDto();
            if (catalog.battleRules.milestoneRewards == null)
                catalog.battleRules.milestoneRewards = Array.Empty<MilestoneRewardDefinitionDto>();
            foreach (var plant in catalog.plants)
            {
                if (plant == null) continue;
                if (plant.tags == null) plant.tags = Array.Empty<string>();
                if (plant.abilityIds == null) plant.abilityIds = Array.Empty<string>();
            }
            foreach (var enemy in catalog.enemies)
            {
                if (enemy == null) continue;
                if (enemy.abilityIds == null) enemy.abilityIds = Array.Empty<string>();
                if (enemy.tags == null) enemy.tags = Array.Empty<string>();
            }
            foreach (var equipment in catalog.equipment)
            {
                if (equipment == null) continue;
                if (equipment.grants == null) equipment.grants = Array.Empty<AbilityGrantDefinitionDto>();
                if (equipment.modifiers == null) equipment.modifiers = Array.Empty<AbilityModifierDefinitionDto>();
            }
            foreach (var status in catalog.statuses)
            {
                if (status == null) continue;
                if (status.tags == null) status.tags = Array.Empty<string>();
                if (status.modifiers == null) status.modifiers = Array.Empty<StatusModifierDefinitionDto>();
                if (string.IsNullOrEmpty(status.polarityId)) status.polarityId = "polarity.neutral";
                if (string.IsNullOrEmpty(status.periodicEffectId)) status.periodicEffectId = "periodic.none";
            }
            foreach (var ability in catalog.abilities)
            {
                if (ability == null) continue;
                if (ability.activation == null) ability.activation = new AbilityActivationDefinitionDto();
                if (ability.timeline == null) ability.timeline = new AbilityTimelineDefinitionDto();
                if (ability.tags == null) ability.tags = Array.Empty<string>();
                if (ability.deliveries == null) ability.deliveries = Array.Empty<AbilityDeliveryDefinitionDto>();
                foreach (var delivery in ability.deliveries)
                    if (delivery != null && delivery.payload == null)
                        delivery.payload = Array.Empty<AbilityPayloadEffectDefinitionDto>();
            }
        }

        private static string[] SortedStrings(string[] source)
        {
            var copy = source == null ? Array.Empty<string>() : (string[])source.Clone();
            Array.Sort(copy, StringComparer.Ordinal);
            return copy;
        }

        private static int ComparePlant(PlantDefinitionDto left, PlantDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareEnemy(EnemyDefinitionDto left, EnemyDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareEquipment(EquipmentDefinitionDto left, EquipmentDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareAbility(AbilityDefinitionDto left, AbilityDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareProjectile(ProjectileDefinitionDto left, ProjectileDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareStatus(StatusDefinitionDto left, StatusDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareWave(WaveDefinitionDto left, WaveDefinitionDto right) { return NullSafeInt(left == null ? int.MaxValue : left.index, right == null ? int.MaxValue : right.index); }
        private static int CompareStarTier(StarTierDefinitionDto left, StarTierDefinitionDto right) { return NullSafeInt(left == null ? int.MaxValue : left.star, right == null ? int.MaxValue : right.star); }
        private static int CompareIds(string left, string right) { return StringComparer.Ordinal.Compare(left ?? string.Empty, right ?? string.Empty); }
        private static int NullSafeInt(int left, int right) { return left.CompareTo(right); }
        private static string NormalizeLineEndings(string value) { return value.Replace("\r\n", "\n").Replace('\r', '\n'); }
    }
}
