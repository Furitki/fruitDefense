using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FruitDefense.Content
{
    public sealed class CompiledOutgameContentCatalog
    {
        public OutgameContentHeaderDto Header { get; private set; }
        public string Fingerprint { get; private set; }
        public IReadOnlyDictionary<string, ItemDefinitionDto> Items { get; private set; }
        public IReadOnlyDictionary<string, ActivityDefinitionDto> Activities { get; private set; }
        public IReadOnlyDictionary<string, GrowthEquipmentDefinitionDto> GrowthEquipment
        {
            get;
            private set;
        }
        public IReadOnlyDictionary<string, CultivationNodeDefinitionDto> CultivationNodes
        {
            get;
            private set;
        }
        public IReadOnlyDictionary<string, GrowthPolicyDefinitionDto> GrowthPolicies
        {
            get;
            private set;
        }

        internal CompiledOutgameContentCatalog(OutgameContentCatalogDto canonical)
        {
            Header = canonical.header;
            Fingerprint = OutgameContentJson.ComputeFingerprint(canonical);
            Items = Index(canonical.items, value => value.id);
            Activities = Index(canonical.activities, value => value.id);
            GrowthEquipment = Index(canonical.growthEquipment, value => value.id);
            CultivationNodes = Index(canonical.cultivationNodes, value => value.id);
            GrowthPolicies = Index(canonical.growthPolicies, value => value.id);
        }

        public ItemDefinitionDto ResolveItem(string id)
        {
            return Resolve(Items, id, "item");
        }

        public ActivityDefinitionDto ResolveActivity(string id)
        {
            return Resolve(Activities, id, "activity");
        }

        public GrowthEquipmentDefinitionDto ResolveGrowthEquipment(string id)
        {
            return Resolve(GrowthEquipment, id, "growth equipment");
        }

        public CultivationNodeDefinitionDto ResolveCultivationNode(string id)
        {
            return Resolve(CultivationNodes, id, "cultivation node");
        }

        public GrowthPolicyDefinitionDto ResolveGrowthPolicy(string id)
        {
            return Resolve(GrowthPolicies, id, "growth policy");
        }

        public GrowthEquipmentRankDefinitionDto ResolveGrowthEquipmentRank(
            string equipmentId, int rank)
        {
            var definition = ResolveGrowthEquipment(equipmentId);
            var resolved = definition.ranks.FirstOrDefault(value => value != null
                && value.rank == rank);
            if (resolved == null)
                throw new KeyNotFoundException("Growth equipment '" + equipmentId
                    + "' does not define rank " + rank + ".");
            return resolved;
        }

        public CultivationRankDefinitionDto ResolveCultivationRank(
            string nodeId, int rank)
        {
            var definition = ResolveCultivationNode(nodeId);
            var resolved = definition.ranks.FirstOrDefault(value => value != null
                && value.rank == rank);
            if (resolved == null)
                throw new KeyNotFoundException("Cultivation node '" + nodeId
                    + "' does not define rank " + rank + ".");
            return resolved;
        }

        private static T Resolve<T>(IReadOnlyDictionary<string, T> values,
            string id, string category)
        {
            T value;
            if (values.TryGetValue(id ?? string.Empty, out value)) return value;
            throw new KeyNotFoundException("Unknown " + category + " ID '" + id + "'.");
        }

        private static IReadOnlyDictionary<string, T> Index<T>(T[] values,
            Func<T, string> getId)
        {
            var dictionary = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var value in values) dictionary.Add(getId(value), value);
            return new ReadOnlyDictionary<string, T>(dictionary);
        }
    }

    public static class OutgameContentCompiler
    {
        public static bool TryCompile(OutgameContentCatalogDto source,
            out CompiledOutgameContentCatalog compiled,
            out ContentValidationResult validation)
        {
            validation = OutgameContentValidator.Validate(source);
            return CompileValidated(source, validation, out compiled);
        }

        public static bool TryCompile(OutgameContentCatalogDto source,
            LevelCatalogSource levels, out CompiledOutgameContentCatalog compiled,
            out ContentValidationResult validation)
        {
            validation = OutgameContentValidator.ValidateCrossCatalog(source, levels);
            return CompileValidated(source, validation, out compiled);
        }

        private static bool CompileValidated(OutgameContentCatalogDto source,
            ContentValidationResult validation,
            out CompiledOutgameContentCatalog compiled)
        {
            if (!validation.IsValid)
            {
                compiled = null;
                return false;
            }
            var copy = OutgameContentJson.DeepCopy(source);
            OutgameContentJson.Canonicalize(copy);
            compiled = new CompiledOutgameContentCatalog(copy);
            return true;
        }
    }
}
