using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FruitDefense.Content
{
    public sealed class CompiledBattleContentCatalog
    {
        public BattleContentHeaderDto Header { get; private set; }
        public BattleRulesDto BattleRules { get; private set; }
        public IReadOnlyDictionary<string, PlantDefinitionDto> Plants { get; private set; }
        public IReadOnlyDictionary<string, EnemyDefinitionDto> Enemies { get; private set; }
        public IReadOnlyDictionary<string, EquipmentDefinitionDto> Equipment { get; private set; }
        public IReadOnlyDictionary<string, SkillDefinitionDto> Skills { get; private set; }
        public IReadOnlyDictionary<string, ProjectileDefinitionDto> Projectiles { get; private set; }
        public IReadOnlyDictionary<string, StatusDefinitionDto> Statuses { get; private set; }
        public IReadOnlyDictionary<string, WaveDefinitionDto> Waves { get; private set; }
        public IReadOnlyDictionary<string, StarTierDefinitionDto> StarTiers { get; private set; }
        public IReadOnlyDictionary<string, CompiledBattleSkill> RuntimeSkills { get; private set; }
        public IReadOnlyDictionary<string, CompiledProjectileDefinition> RuntimeProjectiles { get; private set; }
        public IReadOnlyDictionary<string, CompiledStatusDefinition> RuntimeStatuses { get; private set; }

        internal CompiledBattleContentCatalog(BattleContentCatalogDto catalog)
        {
            Header = catalog.header;
            BattleRules = catalog.battleRules;
            Plants = Index(catalog.plants, value => value.id);
            Enemies = Index(catalog.enemies, value => value.id);
            Equipment = Index(catalog.equipment, value => value.id);
            Skills = Index(catalog.skills, value => value.id);
            Projectiles = Index(catalog.projectiles, value => value.id);
            Statuses = Index(catalog.statuses, value => value.id);
            Waves = Index(catalog.waves, value => value.id);
            StarTiers = Index(catalog.starTiers, value => value.id);
            RuntimeSkills = Index(catalog.skills.Select(BattleSkillCompiler.Compile).ToArray(), value => value.Id);
            RuntimeProjectiles = Index(catalog.projectiles.Select(BattleSkillCompiler.Compile).ToArray(), value => value.Id);
            RuntimeStatuses = Index(catalog.statuses.Select(BattleSkillCompiler.Compile).ToArray(), value => value.Id);
        }

        public IReadOnlyList<CompiledBattleSkill> ResolvePlantSkills(string plantId, string equipmentId)
        {
            PlantDefinitionDto plant;
            if (!Plants.TryGetValue(plantId, out plant)) throw new KeyNotFoundException("Unknown plant ID '" + plantId + "'.");
            var resolved = plant.skillIds.Select(id => RuntimeSkills[id].Clone()).ToList();
            if (!string.IsNullOrEmpty(equipmentId))
            {
                EquipmentDefinitionDto equipment;
                if (!Equipment.TryGetValue(equipmentId, out equipment))
                    throw new KeyNotFoundException("Unknown equipment ID '" + equipmentId + "'.");
                if (!equipment.compatiblePlantIds.Contains(plantId))
                    throw new InvalidOperationException("Equipment '" + equipmentId
                        + "' is not compatible with plant '" + plantId + "'.");
                var plantTags = new HashSet<string>(plant.tags, StringComparer.Ordinal);
                foreach (var grant in equipment.grants)
                {
                    if (!string.IsNullOrEmpty(grant.requiredPlantTag) && !plantTags.Contains(grant.requiredPlantTag)) continue;
                    if (resolved.All(skill => skill.Id != grant.skillId)) resolved.Add(RuntimeSkills[grant.skillId].Clone());
                }
                foreach (var modifier in equipment.modifiers)
                {
                    if (!string.IsNullOrEmpty(modifier.requiredPlantTag) && !plantTags.Contains(modifier.requiredPlantTag)) continue;
                    foreach (var skill in resolved.Where(value => value.Tags.Contains(modifier.targetSkillTag)).ToArray())
                    {
                        if (modifier.burstCountOverride > 0) skill.BurstCount = modifier.burstCountOverride;
                        if (modifier.burstIntervalSeconds > 0f)
                            skill.BurstIntervalTicks = BattleSkillTiming.SecondsToTicks(modifier.burstIntervalSeconds);
                        if (modifier.resourceAmountDelta != 0) skill.ResourceAmount += modifier.resourceAmountDelta;
                    }
                }
            }
            return resolved.OrderBy(skill => skill.Id, StringComparer.Ordinal).ToArray();
        }

        private static IReadOnlyDictionary<string, T> Index<T>(T[] values, Func<T, string> getId)
        {
            var dictionary = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var value in values) dictionary.Add(getId(value), value);
            return new ReadOnlyDictionary<string, T>(dictionary);
        }
    }

    public static class BattleContentCompiler
    {
        public static bool TryCompile(BattleContentCatalogDto source, out CompiledBattleContentCatalog compiled,
            out ContentValidationResult validation)
        {
            validation = BattleContentValidator.Validate(source);
            if (!validation.IsValid)
            {
                compiled = null;
                return false;
            }

            var copy = BattleContentJson.DeepCopy(source);
            BattleContentJson.Canonicalize(copy);
            compiled = new CompiledBattleContentCatalog(copy);
            return true;
        }
    }
}
