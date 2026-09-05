using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FruitDefense.Content
{
    public static class OutgameContentValidator
    {
        private const int MaximumRank = 20;
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);
        private static readonly HashSet<string> GrowthDomains =
            new HashSet<string>(new[]
            {
                OutgameContentIds.GrowthDomains.Equipment,
                OutgameContentIds.GrowthDomains.Cultivation,
            }, StringComparer.Ordinal);

        public static ContentValidationResult Validate(
            OutgameContentCatalogDto catalog)
        {
            var result = new ContentValidationResult();
            if (catalog == null)
            {
                result.Add("outgame.catalog.null", "outgame", string.Empty,
                    string.Empty, "Outgame catalog is required.");
                return result;
            }

            ValidateHeader(catalog.header, result);
            RequireCollection(catalog.items, "items", result);
            RequireCollection(catalog.activities, "activities", result);
            RequireCollection(catalog.growthEquipment, "growthEquipment", result);
            RequireCollection(catalog.cultivationNodes, "cultivationNodes", result);
            RequireCollection(catalog.growthPolicies, "growthPolicies", result);

            var items = Index(catalog.items, value => value.id, "items", result);
            var activities = Index(catalog.activities, value => value.id,
                "activities", result);
            var equipment = Index(catalog.growthEquipment, value => value.id,
                "growthEquipment", result);
            var cultivation = Index(catalog.cultivationNodes, value => value.id,
                "cultivationNodes", result);
            Index(catalog.growthPolicies, value => value.id, "growthPolicies", result);

            ValidateItems(catalog.items, result);
            ValidateEquipment(catalog.growthEquipment, items, result);
            ValidateCultivation(catalog.cultivationNodes, items, cultivation, result);
            ValidateActivities(catalog.activities, items, equipment, result);
            ValidatePolicies(catalog.growthPolicies, equipment, cultivation, result);
            ValidateUniqueReceipts(activities.Values, result);
            ValidateCultivationCycles(cultivation, result);
            return result;
        }

        public static ContentValidationResult ValidateCrossCatalog(
            OutgameContentCatalogDto catalog, LevelCatalogSource levels)
        {
            var result = Validate(catalog);
            if (levels == null)
            {
                result.Add("outgame.level-catalog.null", "outgame", string.Empty,
                    "levels", "Level catalog is required for growth-policy validation.");
                return result;
            }

            var policyIds = new HashSet<string>((catalog == null
                    ? Array.Empty<GrowthPolicyDefinitionDto>()
                    : catalog.growthPolicies ?? Array.Empty<GrowthPolicyDefinitionDto>())
                .Where(value => value != null)
                .Select(value => value.id), StringComparer.Ordinal);
            foreach (var level in levels.Levels)
            {
                if (level == null) continue;
                RequireStableId(level.GrowthPolicyId, "levels", level.LevelId,
                    "growthPolicyId", result);
                if (!policyIds.Contains(level.GrowthPolicyId))
                    result.Add("outgame.reference.missing", "levels", level.LevelId,
                        "growthPolicyId", "Missing growth policy '"
                        + level.GrowthPolicyId + "'.");
            }
            return result;
        }

        public static ContentValidationResult ValidateBundledBaseline(
            OutgameContentCatalogDto catalog, LevelCatalogSource levels)
        {
            var result = ValidateCrossCatalog(catalog, levels);
            if (catalog == null) return result;
            if (catalog.header == null
                || !string.Equals(catalog.header.catalogId,
                    OutgameContentSchema.BundledCatalogId, StringComparison.Ordinal)
                || !string.Equals(catalog.header.contentVersion,
                    OutgameContentSchema.BundledContentVersion, StringComparison.Ordinal))
            {
                result.Add("outgame.bundled.header.mismatch", "header",
                    catalog.header == null ? string.Empty : catalog.header.catalogId,
                    "catalogId", "Bundled outgame identity does not match the current schema.");
            }

            RequireExactIds(catalog.items, value => value.id, "items",
                new[] { OutgameContentIds.Items.MorningDew }, result);
            RequireExactIds(catalog.activities, value => value.id, "activities",
                new[] { OutgameContentIds.Activities.StarterSupplies }, result);
            RequireExactIds(catalog.growthEquipment, value => value.id,
                "growthEquipment",
                new[] { OutgameContentIds.GrowthEquipment.SunleafEmblem }, result);
            RequireExactIds(catalog.cultivationNodes, value => value.id,
                "cultivationNodes",
                new[] { OutgameContentIds.CultivationNodes.VitalRoots }, result);
            RequireExactIds(catalog.growthPolicies, value => value.id,
                "growthPolicies", new[]
                {
                    OutgameContentIds.GrowthPolicies.Orchard01,
                    OutgameContentIds.GrowthPolicies.Orchard02,
                    OutgameContentIds.GrowthPolicies.Orchard03,
                }, result);
            ValidateStarterLoop(catalog, result);
            return result;
        }

        private static void ValidateHeader(OutgameContentHeaderDto header,
            ContentValidationResult result)
        {
            if (header == null)
            {
                result.Add("outgame.header.missing", "header", string.Empty,
                    string.Empty, "Outgame catalog header is required.");
                return;
            }
            if (header.schemaVersion != OutgameContentSchema.CurrentSchemaVersion)
                result.Add("outgame.header.schema.unsupported", "header",
                    header.catalogId, "schemaVersion",
                    "Unsupported outgame catalog schema version.");
            RequireStableId(header.catalogId, "header", header.catalogId,
                "catalogId", result);
            RequireText(header.contentVersion, "header", header.catalogId,
                "contentVersion", result);
            RequireText(header.minCodeVersion, "header", header.catalogId,
                "minCodeVersion", result);
        }

        private static void ValidateItems(ItemDefinitionDto[] values,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                ValidatePresentation(value.presentationId, "items", value.id, result);
                ValidateCopy(value.displayName, value.description, "items", value.id,
                    result);
                if (value.maximumQuantity <= 0)
                    result.Add("outgame.item.quantity.invalid", "items", value.id,
                        "maximumQuantity", "Item maximum quantity must be positive.");
            }
        }

        private static void ValidateActivities(ActivityDefinitionDto[] values,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            IReadOnlyDictionary<string, GrowthEquipmentDefinitionDto> equipment,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                ValidatePresentation(value.presentationId, "activities", value.id,
                    result);
                ValidateCopy(value.displayName, value.description, "activities",
                    value.id, result);
                RequireStableId(value.receiptId, "activities", value.id,
                    "receiptId", result);
                if (value.rewards == null || value.rewards.Length == 0)
                {
                    result.Add("outgame.activity.rewards.empty", "activities",
                        value.id, "rewards", "Activity must contain at least one reward.");
                    continue;
                }
                var grants = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < value.rewards.Length; index++)
                {
                    var reward = value.rewards[index];
                    var field = "rewards[" + index + "]";
                    if (reward == null)
                    {
                        result.Add("outgame.definition.null", "activities", value.id,
                            field, "Reward grant is null.");
                        continue;
                    }
                    if (reward.quantity <= 0)
                        result.Add("outgame.reward.quantity.invalid", "activities",
                            value.id, field + ".quantity",
                            "Reward quantity must be positive.");
                    if (string.Equals(reward.operationId,
                            OutgameContentIds.RewardOperations.Item,
                            StringComparison.Ordinal))
                    {
                        if (!string.IsNullOrEmpty(reward.growthEquipmentId)
                            || reward.initialRank != 0)
                            result.Add("outgame.reward.shape.invalid", "activities",
                                value.id, field,
                                "Item reward cannot contain growth-equipment fields.");
                        ItemDefinitionDto item;
                        if (!items.TryGetValue(reward.itemId ?? string.Empty, out item))
                            MissingReference("activities", value.id,
                                field + ".itemId", reward.itemId, result);
                        else if (reward.quantity > item.maximumQuantity)
                            result.Add("outgame.reward.quantity.exceeds-cap",
                                "activities", value.id, field + ".quantity",
                                "Reward exceeds the item maximum quantity.");
                    }
                    else if (string.Equals(reward.operationId,
                                 OutgameContentIds.RewardOperations.GrowthEquipment,
                                 StringComparison.Ordinal))
                    {
                        if (!string.IsNullOrEmpty(reward.itemId)
                            || reward.quantity != 1)
                            result.Add("outgame.reward.shape.invalid", "activities",
                                value.id, field,
                                "Growth-equipment reward grants exactly one identity.");
                        GrowthEquipmentDefinitionDto definition;
                        if (!equipment.TryGetValue(
                                reward.growthEquipmentId ?? string.Empty,
                                out definition))
                            MissingReference("activities", value.id,
                                field + ".growthEquipmentId",
                                reward.growthEquipmentId, result);
                        else if (definition.ranks == null
                            || !definition.ranks.Any(rank => rank != null
                                && rank.rank == reward.initialRank))
                            result.Add("outgame.reward.rank.invalid", "activities",
                                value.id, field + ".initialRank",
                                "Initial equipment rank is not defined.");
                    }
                    else
                    {
                        result.Add("outgame.reward.operation.unsupported", "activities",
                            value.id, field + ".operationId",
                            "Unsupported reward operation '" + reward.operationId + "'.");
                    }
                    var grantKey = reward.operationId + "\n" + reward.itemId
                        + "\n" + reward.growthEquipmentId;
                    if (!grants.Add(grantKey))
                        result.Add("outgame.reward.duplicate", "activities", value.id,
                            field, "Activity repeats the same reward grant.");
                }
            }
        }

        private static void ValidateEquipment(
            GrowthEquipmentDefinitionDto[] values,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                ValidatePresentation(value.presentationId, "growthEquipment",
                    value.id, result);
                ValidateCopy(value.displayName, value.description,
                    "growthEquipment", value.id, result);
                RequireStableId(value.slotId, "growthEquipment", value.id,
                    "slotId", result);
                if (value.ranks == null || value.ranks.Length == 0)
                {
                    result.Add("outgame.equipment.ranks.empty", "growthEquipment",
                        value.id, "ranks", "Equipment must define rank zero.");
                    continue;
                }
                ValidateRanks(value.ranks, items,
                    OutgameContentIds.GrowthDomains.Equipment,
                    "growthEquipment", value.id, startsAtZero: true, result: result);
            }
        }

        private static void ValidateCultivation(
            CultivationNodeDefinitionDto[] values,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            IReadOnlyDictionary<string, CultivationNodeDefinitionDto> nodes,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                ValidatePresentation(value.presentationId, "cultivationNodes",
                    value.id, result);
                ValidateCopy(value.displayName, value.description,
                    "cultivationNodes", value.id, result);
                var prerequisites = value.prerequisites
                    ?? Array.Empty<CultivationPrerequisiteDto>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < prerequisites.Length; index++)
                {
                    var prerequisite = prerequisites[index];
                    var field = "prerequisites[" + index + "]";
                    if (prerequisite == null)
                    {
                        result.Add("outgame.definition.null", "cultivationNodes",
                            value.id, field, "Cultivation prerequisite is null.");
                        continue;
                    }
                    CultivationNodeDefinitionDto target;
                    if (!nodes.TryGetValue(prerequisite.nodeId ?? string.Empty,
                            out target))
                        MissingReference("cultivationNodes", value.id,
                            field + ".nodeId", prerequisite.nodeId, result);
                    else
                    {
                        var maximum = target.ranks == null ? 0
                            : target.ranks.Where(rank => rank != null)
                                .Select(rank => rank.rank).DefaultIfEmpty(0).Max();
                        if (prerequisite.requiredRank <= 0
                            || prerequisite.requiredRank > maximum)
                            result.Add("outgame.cultivation.prerequisite-rank.invalid",
                                "cultivationNodes", value.id,
                                field + ".requiredRank",
                                "Prerequisite rank is outside the target node.");
                    }
                    if (!seen.Add(prerequisite.nodeId ?? string.Empty))
                        result.Add("outgame.cultivation.prerequisite.duplicate",
                            "cultivationNodes", value.id, field,
                            "Cultivation prerequisite is duplicated.");
                }
                if (value.ranks == null || value.ranks.Length == 0)
                {
                    result.Add("outgame.cultivation.ranks.empty", "cultivationNodes",
                        value.id, "ranks", "Cultivation must define rank one.");
                    continue;
                }
                ValidateRanks(value.ranks, items,
                    OutgameContentIds.GrowthDomains.Cultivation,
                    "cultivationNodes", value.id, startsAtZero: false, result: result);
            }
        }

        private static void ValidateRanks(GrowthEquipmentRankDefinitionDto[] ranks,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            string domain, string category, string itemId, bool startsAtZero,
            ContentValidationResult result)
        {
            var projected = ranks == null
                ? Array.Empty<RankProjection>()
                : ranks.Select(value => value == null ? null
                    : new RankProjection(value.rank, value.costs,
                        value.contributions)).ToArray();
            ValidateRankProjections(projected, items, domain, category, itemId,
                startsAtZero, result);
        }

        private static void ValidateRanks(CultivationRankDefinitionDto[] ranks,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            string domain, string category, string itemId, bool startsAtZero,
            ContentValidationResult result)
        {
            var projected = ranks == null
                ? Array.Empty<RankProjection>()
                : ranks.Select(value => value == null ? null
                    : new RankProjection(value.rank, value.costs,
                        value.contributions)).ToArray();
            ValidateRankProjections(projected, items, domain, category, itemId,
                startsAtZero, result);
        }

        private static void ValidateRankProjections(RankProjection[] ranks,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            string domain, string category, string itemId, bool startsAtZero,
            ContentValidationResult result)
        {
            var expected = startsAtZero ? 0 : 1;
            var ordered = ranks.Where(value => value != null)
                .OrderBy(value => value.Rank).ToArray();
            if (ordered.Length != ranks.Length)
                result.Add("outgame.definition.null", category, itemId, "ranks",
                    "Rank definition is null.");
            for (var index = 0; index < ordered.Length; index++)
            {
                var rank = ordered[index];
                var field = "ranks[" + index + "]";
                if (rank.Rank != expected || rank.Rank > MaximumRank)
                    result.Add("outgame.rank.sequence.invalid", category, itemId,
                        field + ".rank", "Ranks must be consecutive and bounded from "
                        + (startsAtZero ? "zero" : "one") + ".");
                expected++;
                var costs = rank.Costs ?? Array.Empty<GrowthCostDto>();
                if (startsAtZero && rank.Rank == 0 && costs.Length != 0)
                    result.Add("outgame.rank.zero-cost.invalid", category, itemId,
                        field + ".costs", "Owned rank zero cannot have an acquisition cost.");
                if ((!startsAtZero || rank.Rank > 0) && costs.Length == 0)
                    result.Add("outgame.rank.cost.empty", category, itemId,
                        field + ".costs", "Purchasable rank requires a complete cost.");
                ValidateCosts(costs, items, category, itemId, field, result);
                ValidateContributions(rank.Contributions, domain, category,
                    itemId, field, result);
            }
        }

        private static void ValidateCosts(GrowthCostDto[] costs,
            IReadOnlyDictionary<string, ItemDefinitionDto> items,
            string category, string itemId, string ownerField,
            ContentValidationResult result)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < costs.Length; index++)
            {
                var cost = costs[index];
                var field = ownerField + ".costs[" + index + "]";
                if (cost == null)
                {
                    result.Add("outgame.definition.null", category, itemId, field,
                        "Growth cost is null.");
                    continue;
                }
                if (!items.ContainsKey(cost.itemId ?? string.Empty))
                    MissingReference(category, itemId, field + ".itemId",
                        cost.itemId, result);
                if (cost.quantity <= 0)
                    result.Add("outgame.cost.quantity.invalid", category, itemId,
                        field + ".quantity", "Growth cost must be positive.");
                if (!seen.Add(cost.itemId ?? string.Empty))
                    result.Add("outgame.cost.duplicate", category, itemId, field,
                        "Growth cost repeats an item identity.");
            }
        }

        private static void ValidateContributions(
            GrowthContributionDto[] contributions, string expectedDomain,
            string category, string itemId, string ownerField,
            ContentValidationResult result)
        {
            contributions = contributions ?? Array.Empty<GrowthContributionDto>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < contributions.Length; index++)
            {
                var contribution = contributions[index];
                var field = ownerField + ".contributions[" + index + "]";
                if (contribution == null)
                {
                    result.Add("outgame.definition.null", category, itemId, field,
                        "Growth contribution is null.");
                    continue;
                }
                if (!string.Equals(contribution.domainId, expectedDomain,
                        StringComparison.Ordinal))
                    result.Add("outgame.contribution.domain.invalid", category,
                        itemId, field + ".domainId",
                        "Contribution domain does not match its owner.");
                if (!CombatFrameworkCompiler.SupportsAttribute(
                        contribution.attributeId))
                    result.Add("outgame.contribution.attribute.unsupported", category,
                        itemId, field + ".attributeId",
                        "Unsupported growth attribute '"
                        + contribution.attributeId + "'.");
                if (!CombatFrameworkCompiler.SupportsOperation(
                        contribution.operationId))
                    result.Add("outgame.contribution.operation.unsupported", category,
                        itemId, field + ".operationId",
                        "Unsupported growth operation '"
                        + contribution.operationId + "'.");
                if (!IsFinite(contribution.value) || contribution.value <= 0f)
                    result.Add("outgame.contribution.value.invalid", category,
                        itemId, field + ".value",
                        "Growth contribution must be finite and positive.");
                var key = contribution.domainId + "\n"
                    + contribution.attributeId + "\n" + contribution.operationId;
                if (!seen.Add(key))
                    result.Add("outgame.contribution.duplicate", category, itemId,
                        field, "Rank repeats a growth contribution identity.");
            }
        }

        private static void ValidatePolicies(GrowthPolicyDefinitionDto[] values,
            IReadOnlyDictionary<string, GrowthEquipmentDefinitionDto> equipment,
            IReadOnlyDictionary<string, CultivationNodeDefinitionDto> cultivation,
            ContentValidationResult result)
        {
            if (values == null) return;
            foreach (var value in values)
            {
                if (value == null) continue;
                RequireText(value.displayName, "growthPolicies", value.id,
                    "displayName", result);
                var domains = ValidateUniqueIds(value.permittedDomainIds,
                    "growthPolicies", value.id, "permittedDomainIds", result,
                    GrowthDomains);
                var attributes = ValidateUniqueSupportedAttributes(
                    value.permittedAttributeIds, value.id, result);
                var sources = value.permittedSourceIds ?? Array.Empty<string>();
                var seenSources = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < sources.Length; index++)
                {
                    var sourceId = sources[index];
                    var field = "permittedSourceIds[" + index + "]";
                    RequireStableId(sourceId, "growthPolicies", value.id,
                        field, result);
                    if (!seenSources.Add(sourceId ?? string.Empty))
                        result.Add("outgame.policy.source.duplicate",
                            "growthPolicies", value.id, field,
                            "Growth policy repeats a source filter.");
                    GrowthEquipmentDefinitionDto equipmentSource;
                    CultivationNodeDefinitionDto cultivationSource;
                    if (equipment.TryGetValue(sourceId ?? string.Empty,
                            out equipmentSource))
                    {
                        if (!domains.Contains(
                                OutgameContentIds.GrowthDomains.Equipment))
                            result.Add("outgame.policy.source-domain.invalid",
                                "growthPolicies", value.id, field,
                                "Equipment source is not in a permitted domain.");
                    }
                    else if (cultivation.TryGetValue(sourceId ?? string.Empty,
                                 out cultivationSource))
                    {
                        if (!domains.Contains(
                                OutgameContentIds.GrowthDomains.Cultivation))
                            result.Add("outgame.policy.source-domain.invalid",
                                "growthPolicies", value.id, field,
                                "Cultivation source is not in a permitted domain.");
                    }
                    else MissingReference("growthPolicies", value.id, field,
                        sourceId, result);
                }
                var caps = value.caps ?? Array.Empty<GrowthPolicyCapDto>();
                var seenCaps = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < caps.Length; index++)
                {
                    var cap = caps[index];
                    var field = "caps[" + index + "]";
                    if (cap == null)
                    {
                        result.Add("outgame.definition.null", "growthPolicies",
                            value.id, field, "Growth policy cap is null.");
                        continue;
                    }
                    if (!attributes.Contains(cap.attributeId ?? string.Empty))
                        result.Add("outgame.policy.cap.attribute.invalid",
                            "growthPolicies", value.id, field + ".attributeId",
                            "Cap attribute is not permitted by this policy.");
                    if (!IsFinite(cap.minimumValue)
                        || !IsFinite(cap.maximumValue)
                        || cap.minimumValue > cap.maximumValue)
                        result.Add("outgame.policy.cap.range.invalid",
                            "growthPolicies", value.id, field,
                            "Growth cap must be finite with minimum <= maximum.");
                    if (!seenCaps.Add(cap.attributeId ?? string.Empty))
                        result.Add("outgame.policy.cap.duplicate",
                            "growthPolicies", value.id, field,
                            "Growth policy repeats an attribute cap.");
                }
            }
        }

        private static void ValidateUniqueReceipts(
            IEnumerable<ActivityDefinitionDto> activities,
            ContentValidationResult result)
        {
            var receipts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var activity in activities)
            {
                if (activity == null) continue;
                if (!receipts.Add(activity.receiptId ?? string.Empty))
                    result.Add("outgame.activity.receipt.duplicate", "activities",
                        activity.id, "receiptId",
                        "Activity receipt identity must be globally unique.");
            }
        }

        private static void ValidateCultivationCycles(
            IReadOnlyDictionary<string, CultivationNodeDefinitionDto> nodes,
            ContentValidationResult result)
        {
            var states = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in nodes.Keys.OrderBy(value => value,
                         StringComparer.Ordinal))
                VisitCultivation(id, nodes, states, result);
        }

        private static void VisitCultivation(string id,
            IReadOnlyDictionary<string, CultivationNodeDefinitionDto> nodes,
            IDictionary<string, int> states, ContentValidationResult result)
        {
            int state;
            if (states.TryGetValue(id, out state))
            {
                if (state == 1)
                    result.Add("outgame.cultivation.cycle", "cultivationNodes",
                        id, "prerequisites",
                        "Cultivation prerequisite graph contains a cycle.");
                return;
            }
            states[id] = 1;
            var node = nodes[id];
            foreach (var prerequisite in node.prerequisites
                         ?? Array.Empty<CultivationPrerequisiteDto>())
            {
                if (prerequisite != null
                    && nodes.ContainsKey(prerequisite.nodeId ?? string.Empty))
                    VisitCultivation(prerequisite.nodeId, nodes, states, result);
            }
            states[id] = 2;
        }

        private static void ValidateStarterLoop(OutgameContentCatalogDto catalog,
            ContentValidationResult result)
        {
            var activity = (catalog.activities ?? Array.Empty<ActivityDefinitionDto>())
                .FirstOrDefault(value => value != null
                    && string.Equals(value.id,
                        OutgameContentIds.Activities.StarterSupplies,
                        StringComparison.Ordinal));
            var equipment = (catalog.growthEquipment
                    ?? Array.Empty<GrowthEquipmentDefinitionDto>())
                .FirstOrDefault(value => value != null
                    && string.Equals(value.id,
                        OutgameContentIds.GrowthEquipment.SunleafEmblem,
                        StringComparison.Ordinal));
            if (activity == null || equipment == null) return;
            if (!activity.bundledAvailable)
                result.Add("outgame.starter.activity.unavailable", "activities",
                    activity.id, "bundledAvailable",
                    "Starter activity must be available without a clock.");
            var rewards = activity.rewards ?? Array.Empty<RewardGrantDto>();
            var grantsEquipment = rewards.Any(value => value != null
                && value.operationId
                    == OutgameContentIds.RewardOperations.GrowthEquipment
                && value.growthEquipmentId == equipment.id);
            var itemGrants = rewards.Where(value => value != null
                    && value.operationId == OutgameContentIds.RewardOperations.Item)
                .GroupBy(value => value.itemId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key,
                    group => group.Sum(value => value.quantity),
                    StringComparer.Ordinal);
            var affordableEquipmentRank = (equipment.ranks
                    ?? Array.Empty<GrowthEquipmentRankDefinitionDto>())
                .Where(rank => rank != null && rank.rank > 0)
                .OrderBy(rank => rank.rank).FirstOrDefault();
            var affordableCultivationRank = (catalog.cultivationNodes
                    ?? Array.Empty<CultivationNodeDefinitionDto>())
                .Where(node => node != null
                    && (node.prerequisites == null
                        || node.prerequisites.Length == 0))
                .SelectMany(node => node.ranks
                    ?? Array.Empty<CultivationRankDefinitionDto>())
                .Where(rank => rank != null)
                .OrderBy(rank => rank.rank).FirstOrDefault();
            var fundsUpgrade = grantsEquipment
                && (CostsCovered(affordableEquipmentRank == null
                        ? null : affordableEquipmentRank.costs, itemGrants)
                    || CostsCovered(affordableCultivationRank == null
                        ? null : affordableCultivationRank.costs, itemGrants));
            if (!fundsUpgrade)
                result.Add("outgame.starter.loop.unfunded", "activities",
                    activity.id, "rewards",
                    "Starter claim must grant equipment and fund one real upgrade.");
        }

        private static bool CostsCovered(GrowthCostDto[] costs,
            IReadOnlyDictionary<string, int> grants)
        {
            if (costs == null || costs.Length == 0) return false;
            foreach (var cost in costs)
            {
                int quantity;
                if (cost == null || !grants.TryGetValue(cost.itemId, out quantity)
                    || quantity < cost.quantity) return false;
            }
            return true;
        }

        private static Dictionary<string, T> Index<T>(T[] values,
            Func<T, string> getId, string category,
            ContentValidationResult result) where T : class
        {
            var index = new Dictionary<string, T>(StringComparer.Ordinal);
            if (values == null) return index;
            for (var position = 0; position < values.Length; position++)
            {
                var value = values[position];
                if (value == null)
                {
                    result.Add("outgame.definition.null", category, string.Empty,
                        "[" + position + "]", "Definition is null.");
                    continue;
                }
                var id = getId(value) ?? string.Empty;
                RequireStableId(id, category, id, "id", result);
                if (index.ContainsKey(id))
                    result.Add("outgame.identity.duplicate", category, id, "id",
                        "Identity '" + id + "' is duplicated.");
                else index.Add(id, value);
            }
            return index;
        }

        private static HashSet<string> ValidateUniqueIds(string[] values,
            string category, string itemId, string field,
            ContentValidationResult result, IReadOnlyCollection<string> allowed)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            values = values ?? Array.Empty<string>();
            if (values.Length == 0)
                result.Add("outgame.policy.collection.empty", category, itemId,
                    field, "Growth policy collection must not be empty.");
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                RequireStableId(value, category, itemId,
                    field + "[" + index + "]", result);
                if (!allowed.Contains(value))
                    result.Add("outgame.policy.value.unsupported", category,
                        itemId, field + "[" + index + "]",
                        "Unsupported policy value '" + value + "'.");
                if (!set.Add(value ?? string.Empty))
                    result.Add("outgame.policy.value.duplicate", category,
                        itemId, field + "[" + index + "]",
                        "Growth policy value is duplicated.");
            }
            return set;
        }

        private static HashSet<string> ValidateUniqueSupportedAttributes(
            string[] values, string itemId, ContentValidationResult result)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            values = values ?? Array.Empty<string>();
            if (values.Length == 0)
                result.Add("outgame.policy.collection.empty", "growthPolicies",
                    itemId, "permittedAttributeIds",
                    "Growth policy must permit at least one attribute.");
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                if (!CombatFrameworkCompiler.SupportsAttribute(value))
                    result.Add("outgame.policy.attribute.unsupported",
                        "growthPolicies", itemId,
                        "permittedAttributeIds[" + index + "]",
                        "Unsupported growth attribute '" + value + "'.");
                if (!set.Add(value ?? string.Empty))
                    result.Add("outgame.policy.attribute.duplicate",
                        "growthPolicies", itemId,
                        "permittedAttributeIds[" + index + "]",
                        "Growth policy attribute is duplicated.");
            }
            return set;
        }

        private static void RequireExactIds<T>(T[] values,
            Func<T, string> getId, string category, string[] expected,
            ContentValidationResult result) where T : class
        {
            var actual = (values ?? Array.Empty<T>()).Where(value => value != null)
                .Select(getId).OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var orderedExpected = expected.OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(orderedExpected))
                result.Add("outgame.bundled.ids.mismatch", category, string.Empty,
                    "ids", "Bundled " + category + " identities do not match the starter set.");
        }

        private static void RequireCollection<T>(T[] values, string category,
            ContentValidationResult result)
        {
            if (values != null && values.Length > 0) return;
            result.Add("outgame.collection.empty", category, string.Empty,
                string.Empty, "Catalog collection must not be empty.");
        }

        private static void ValidatePresentation(string value, string category,
            string itemId, ContentValidationResult result)
        {
            RequireStableId(value, category, itemId, "presentationId", result);
        }

        private static void ValidateCopy(string displayName, string description,
            string category, string itemId, ContentValidationResult result)
        {
            RequireText(displayName, category, itemId, "displayName", result);
            RequireText(description, category, itemId, "description", result);
        }

        private static void RequireText(string value, string category,
            string itemId, string field, ContentValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(value)) return;
            result.Add("outgame.text.missing", category, itemId, field,
                "Required text is missing.");
        }

        private static void RequireStableId(string value, string category,
            string itemId, string field, ContentValidationResult result)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && StableIdPattern.IsMatch(value)) return;
            result.Add("outgame.identity.invalid", category, itemId, field,
                "Value must be a stable lowercase semantic ID.");
        }

        private static void MissingReference(string category, string itemId,
            string field, string referencedId, ContentValidationResult result)
        {
            result.Add("outgame.reference.missing", category, itemId, field,
                "Missing referenced identity '" + referencedId + "'.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class RankProjection
        {
            public int Rank { get; private set; }
            public GrowthCostDto[] Costs { get; private set; }
            public GrowthContributionDto[] Contributions { get; private set; }

            public RankProjection(int rank, GrowthCostDto[] costs,
                GrowthContributionDto[] contributions)
            {
                Rank = rank;
                Costs = costs;
                Contributions = contributions;
            }
        }
    }
}
