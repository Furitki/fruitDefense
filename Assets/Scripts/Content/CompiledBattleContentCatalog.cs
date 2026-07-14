using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
