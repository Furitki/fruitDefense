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
            Array.Sort(catalog.skills, CompareSkill);
            Array.Sort(catalog.projectiles, CompareProjectile);
            Array.Sort(catalog.statuses, CompareStatus);
            Array.Sort(catalog.waves, CompareWave);
            Array.Sort(catalog.starTiers, CompareStarTier);

            foreach (var plant in catalog.plants)
            {
                if (plant == null) continue;
                plant.skillIds = SortedStrings(plant.skillIds);
                plant.tags = SortedStrings(plant.tags);
                plant.allowedEquipmentIds = SortedStrings(plant.allowedEquipmentIds);
            }

            foreach (var equipment in catalog.equipment)
            {
                if (equipment == null) continue;
                equipment.skillIds = SortedStrings(equipment.skillIds);
                equipment.statusIds = SortedStrings(equipment.statusIds);
                equipment.compatiblePlantIds = SortedStrings(equipment.compatiblePlantIds);
                equipment.grants = equipment.grants ?? Array.Empty<EquipmentSkillGrantDto>();
                Array.Sort(equipment.grants, (left, right) => CompareIds(left == null ? null : left.skillId, right == null ? null : right.skillId));
                equipment.modifiers = equipment.modifiers ?? Array.Empty<SkillModifierDefinitionDto>();
                Array.Sort(equipment.modifiers, (left, right) => CompareIds(left == null ? null : left.id, right == null ? null : right.id));
            }

            foreach (var skill in catalog.skills)
            {
                if (skill == null) continue;
                skill.tags = SortedStrings(skill.tags);
                skill.effects = skill.effects ?? Array.Empty<SkillEffectDefinitionDto>();
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
            if (catalog.skills == null) catalog.skills = Array.Empty<SkillDefinitionDto>();
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
            }
            foreach (var equipment in catalog.equipment)
            {
                if (equipment == null) continue;
                if (equipment.grants == null) equipment.grants = Array.Empty<EquipmentSkillGrantDto>();
                if (equipment.modifiers == null) equipment.modifiers = Array.Empty<SkillModifierDefinitionDto>();
            }
            foreach (var skill in catalog.skills)
            {
                if (skill == null) continue;
                if (skill.tags == null) skill.tags = Array.Empty<string>();
                if (skill.effects == null) skill.effects = Array.Empty<SkillEffectDefinitionDto>();
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
        private static int CompareSkill(SkillDefinitionDto left, SkillDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareProjectile(ProjectileDefinitionDto left, ProjectileDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareStatus(StatusDefinitionDto left, StatusDefinitionDto right) { return CompareIds(left == null ? null : left.id, right == null ? null : right.id); }
        private static int CompareWave(WaveDefinitionDto left, WaveDefinitionDto right) { return NullSafeInt(left == null ? int.MaxValue : left.index, right == null ? int.MaxValue : right.index); }
        private static int CompareStarTier(StarTierDefinitionDto left, StarTierDefinitionDto right) { return NullSafeInt(left == null ? int.MaxValue : left.star, right == null ? int.MaxValue : right.star); }
        private static int CompareIds(string left, string right) { return StringComparer.Ordinal.Compare(left ?? string.Empty, right ?? string.Empty); }
        private static int NullSafeInt(int left, int right) { return left.CompareTo(right); }
        private static string NormalizeLineEndings(string value) { return value.Replace("\r\n", "\n").Replace('\r', '\n'); }
    }
}
