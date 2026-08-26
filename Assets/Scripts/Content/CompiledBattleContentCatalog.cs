using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FruitDefense.Content
{
    public sealed class CompiledBattleContentCatalog
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<CompiledAbilityDefinition>> _plantAbilityLoadouts;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<CompiledAbilityDefinition>> _enemyAbilityLoadouts;

        public BattleContentHeaderDto Header { get; private set; }
        public BattleRulesDto BattleRules { get; private set; }
        public IReadOnlyDictionary<string, PlantDefinitionDto> Plants { get; private set; }
        public IReadOnlyDictionary<string, EnemyDefinitionDto> Enemies { get; private set; }
        public IReadOnlyDictionary<string, EquipmentDefinitionDto> Equipment { get; private set; }
        public IReadOnlyDictionary<string, AbilityDefinitionDto> Abilities { get; private set; }
        public IReadOnlyDictionary<string, ProjectileDefinitionDto> Projectiles { get; private set; }
        public IReadOnlyDictionary<string, StatusDefinitionDto> Statuses { get; private set; }
        public IReadOnlyDictionary<string, WaveDefinitionDto> Waves { get; private set; }
        public IReadOnlyDictionary<string, StarTierDefinitionDto> StarTiers { get; private set; }
        public IReadOnlyDictionary<string, CompiledAbilityDefinition> RuntimeAbilities { get; private set; }
        public IReadOnlyDictionary<string, CompiledProjectileDefinition> RuntimeProjectiles { get; private set; }
        public IReadOnlyDictionary<string, CompiledStatusDefinition> RuntimeStatuses { get; private set; }

        internal CompiledBattleContentCatalog(BattleContentCatalogDto catalog)
        {
            Header = catalog.header;
            BattleRules = catalog.battleRules;
            Plants = Index(catalog.plants, value => value.id);
            Enemies = Index(catalog.enemies, value => value.id);
            Equipment = Index(catalog.equipment, value => value.id);
            Abilities = Index(catalog.abilities, value => value.id);
            Projectiles = Index(catalog.projectiles, value => value.id);
            Statuses = Index(catalog.statuses, value => value.id);
            Waves = Index(catalog.waves, value => value.id);
            StarTiers = Index(catalog.starTiers, value => value.id);
            RuntimeAbilities = Index(catalog.abilities.Select(BattleAbilityCompiler.Compile).ToArray(), value => value.Id);
            RuntimeProjectiles = Index(catalog.projectiles.Select(BattleAbilityCompiler.Compile).ToArray(), value => value.Id);
            RuntimeStatuses = Index(catalog.statuses.Select(BattleAbilityCompiler.Compile).ToArray(), value => value.Id);
            _plantAbilityLoadouts = BuildPlantAbilityLoadouts();
            _enemyAbilityLoadouts = BuildEnemyAbilityLoadouts();
        }

        public IReadOnlyList<CompiledAbilityDefinition> ResolvePlantAbilities(string plantId, string equipmentId)
        {
            var key = LoadoutKey(plantId, equipmentId);
            IReadOnlyList<CompiledAbilityDefinition> loadout;
            if (_plantAbilityLoadouts.TryGetValue(key, out loadout)) return loadout;
            if (!Plants.ContainsKey(plantId)) throw new KeyNotFoundException("Unknown plant ID '" + plantId + "'.");
            if (!string.IsNullOrEmpty(equipmentId) && !Equipment.ContainsKey(equipmentId))
                throw new KeyNotFoundException("Unknown equipment ID '" + equipmentId + "'.");
            throw new InvalidOperationException("Equipment '" + equipmentId
                + "' is not compatible with plant '" + plantId + "'.");
        }

        public IReadOnlyList<CompiledAbilityDefinition> ResolveEnemyAbilities(string enemyId)
        {
            IReadOnlyList<CompiledAbilityDefinition> loadout;
            if (_enemyAbilityLoadouts.TryGetValue(enemyId, out loadout)) return loadout;
            throw new KeyNotFoundException("Unknown enemy ID '" + enemyId + "'.");
        }

        private IReadOnlyDictionary<string, IReadOnlyList<CompiledAbilityDefinition>> BuildPlantAbilityLoadouts()
        {
            var result = new Dictionary<string, IReadOnlyList<CompiledAbilityDefinition>>(StringComparer.Ordinal);
            foreach (var plant in Plants.Values.OrderBy(value => value.id, StringComparer.Ordinal))
            {
                result.Add(LoadoutKey(plant.id, string.Empty), BuildPlantAbilityLoadout(plant, null));
                foreach (var equipment in Equipment.Values
                             .Where(value => value.compatiblePlantIds.Contains(plant.id))
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                    result.Add(LoadoutKey(plant.id, equipment.id), BuildPlantAbilityLoadout(plant, equipment));
            }
            return new ReadOnlyDictionary<string, IReadOnlyList<CompiledAbilityDefinition>>(result);
        }

        private IReadOnlyList<CompiledAbilityDefinition> BuildPlantAbilityLoadout(PlantDefinitionDto plant,
            EquipmentDefinitionDto equipment)
        {
            var resolved = plant.abilityIds.Select(id => RuntimeAbilities[id].Clone()).ToList();
            var plantTags = new HashSet<string>(plant.tags, StringComparer.Ordinal);
            if (equipment != null)
            {
                foreach (var grant in equipment.grants)
                {
                    if (!string.IsNullOrEmpty(grant.requiredPlantTag)
                        && !plantTags.Contains(grant.requiredPlantTag)) continue;
                    if (resolved.All(ability => ability.Id != grant.abilityId))
                        resolved.Add(RuntimeAbilities[grant.abilityId].Clone());
                }
                foreach (var source in equipment.modifiers.OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    if (!string.IsNullOrEmpty(source.requiredPlantTag)
                        && !plantTags.Contains(source.requiredPlantTag)) continue;
                    var modifier = BattleAbilityCompiler.Compile(source);
                    foreach (var ability in resolved.Where(value => ModifierMatches(modifier, value)))
                        CompiledAbilityModifierApplicator.Apply(ability, modifier);
                }
            }
            return Array.AsReadOnly(resolved
                .OrderBy(value => value.Activation.Priority)
                .ThenBy(value => value.Id, StringComparer.Ordinal).ToArray());
        }

        private IReadOnlyDictionary<string, IReadOnlyList<CompiledAbilityDefinition>> BuildEnemyAbilityLoadouts()
        {
            var result = new Dictionary<string, IReadOnlyList<CompiledAbilityDefinition>>(StringComparer.Ordinal);
            foreach (var enemy in Enemies.Values.OrderBy(value => value.id, StringComparer.Ordinal))
                result.Add(enemy.id, Array.AsReadOnly(enemy.abilityIds.Select(id => RuntimeAbilities[id])
                    .OrderBy(value => value.Activation.Priority)
                    .ThenBy(value => value.Id, StringComparer.Ordinal).ToArray()));
            return new ReadOnlyDictionary<string, IReadOnlyList<CompiledAbilityDefinition>>(result);
        }

        private static bool ModifierMatches(CompiledAbilityModifier modifier, CompiledAbilityDefinition ability)
        {
            if (!string.IsNullOrEmpty(modifier.TargetAbilityId)
                && !string.Equals(modifier.TargetAbilityId, ability.Id, StringComparison.Ordinal)) return false;
            return string.IsNullOrEmpty(modifier.TargetAbilityTag) || ability.Tags.Contains(modifier.TargetAbilityTag);
        }

        private static string LoadoutKey(string plantId, string equipmentId)
        {
            return (plantId ?? string.Empty) + "\n" + (equipmentId ?? string.Empty);
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
