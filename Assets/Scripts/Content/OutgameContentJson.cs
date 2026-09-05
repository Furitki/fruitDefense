using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace FruitDefense.Content
{
    public static class OutgameContentJson
    {
        public static OutgameContentCatalogDto Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Outgame catalog JSON is empty.", nameof(json));
            var catalog = JsonUtility.FromJson<OutgameContentCatalogDto>(json);
            if (catalog == null)
                throw new InvalidOperationException(
                    "Outgame catalog JSON could not be deserialized.");
            EnsureArrays(catalog);
            return catalog;
        }

        public static OutgameContentCatalogDto DeepCopy(
            OutgameContentCatalogDto source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Deserialize(JsonUtility.ToJson(source, false));
        }

        public static string SerializeCanonical(OutgameContentCatalogDto source,
            bool prettyPrint = true)
        {
            var copy = DeepCopy(source);
            Canonicalize(copy);
            return JsonUtility.ToJson(copy, prettyPrint)
                .Replace("\r\n", "\n").Replace('\r', '\n') + "\n";
        }

        public static byte[] SerializeCanonicalUtf8(
            OutgameContentCatalogDto source, bool prettyPrint = true)
        {
            return new UTF8Encoding(false).GetBytes(
                SerializeCanonical(source, prettyPrint));
        }

        public static string ComputeFingerprint(OutgameContentCatalogDto source)
        {
            var bytes = SerializeCanonicalUtf8(source);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var index = 0; index < hash.Length; index++)
                    builder.Append(hash[index].ToString("x2"));
                return builder.ToString();
            }
        }

        public static void Canonicalize(OutgameContentCatalogDto catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            EnsureArrays(catalog);
            Array.Sort(catalog.items, (left, right) => CompareIds(
                left == null ? null : left.id, right == null ? null : right.id));
            Array.Sort(catalog.activities, (left, right) => CompareIds(
                left == null ? null : left.id, right == null ? null : right.id));
            Array.Sort(catalog.growthEquipment, (left, right) => CompareIds(
                left == null ? null : left.id, right == null ? null : right.id));
            Array.Sort(catalog.cultivationNodes, (left, right) => CompareIds(
                left == null ? null : left.id, right == null ? null : right.id));
            Array.Sort(catalog.growthPolicies, (left, right) => CompareIds(
                left == null ? null : left.id, right == null ? null : right.id));

            foreach (var activity in catalog.activities)
            {
                if (activity == null) continue;
                Array.Sort(activity.rewards, CompareReward);
            }

            foreach (var equipment in catalog.growthEquipment)
            {
                if (equipment == null) continue;
                Array.Sort(equipment.ranks, (left, right) => CompareRank(
                    left == null ? int.MaxValue : left.rank,
                    right == null ? int.MaxValue : right.rank));
                foreach (var rank in equipment.ranks)
                {
                    if (rank == null) continue;
                    CanonicalizeCosts(rank.costs);
                    CanonicalizeContributions(rank.contributions);
                }
            }

            foreach (var node in catalog.cultivationNodes)
            {
                if (node == null) continue;
                Array.Sort(node.prerequisites, (left, right) => CompareIds(
                    left == null ? null : left.nodeId,
                    right == null ? null : right.nodeId));
                Array.Sort(node.ranks, (left, right) => CompareRank(
                    left == null ? int.MaxValue : left.rank,
                    right == null ? int.MaxValue : right.rank));
                foreach (var rank in node.ranks)
                {
                    if (rank == null) continue;
                    CanonicalizeCosts(rank.costs);
                    CanonicalizeContributions(rank.contributions);
                }
            }

            foreach (var policy in catalog.growthPolicies)
            {
                if (policy == null) continue;
                policy.permittedDomainIds = SortedStrings(
                    policy.permittedDomainIds);
                policy.permittedAttributeIds = SortedStrings(
                    policy.permittedAttributeIds);
                policy.permittedSourceIds = SortedStrings(
                    policy.permittedSourceIds);
                Array.Sort(policy.caps, (left, right) => CompareIds(
                    left == null ? null : left.attributeId,
                    right == null ? null : right.attributeId));
            }
        }

        private static void EnsureArrays(OutgameContentCatalogDto catalog)
        {
            if (catalog.header == null) catalog.header = new OutgameContentHeaderDto();
            if (catalog.items == null) catalog.items = Array.Empty<ItemDefinitionDto>();
            if (catalog.activities == null)
                catalog.activities = Array.Empty<ActivityDefinitionDto>();
            if (catalog.growthEquipment == null)
                catalog.growthEquipment = Array.Empty<GrowthEquipmentDefinitionDto>();
            if (catalog.cultivationNodes == null)
                catalog.cultivationNodes = Array.Empty<CultivationNodeDefinitionDto>();
            if (catalog.growthPolicies == null)
                catalog.growthPolicies = Array.Empty<GrowthPolicyDefinitionDto>();
            foreach (var activity in catalog.activities)
                if (activity != null && activity.rewards == null)
                    activity.rewards = Array.Empty<RewardGrantDto>();
            foreach (var equipment in catalog.growthEquipment)
            {
                if (equipment == null) continue;
                if (equipment.ranks == null)
                    equipment.ranks = Array.Empty<GrowthEquipmentRankDefinitionDto>();
                foreach (var rank in equipment.ranks)
                {
                    if (rank == null) continue;
                    if (rank.costs == null) rank.costs = Array.Empty<GrowthCostDto>();
                    if (rank.contributions == null)
                        rank.contributions = Array.Empty<GrowthContributionDto>();
                }
            }
            foreach (var node in catalog.cultivationNodes)
            {
                if (node == null) continue;
                if (node.prerequisites == null)
                    node.prerequisites = Array.Empty<CultivationPrerequisiteDto>();
                if (node.ranks == null)
                    node.ranks = Array.Empty<CultivationRankDefinitionDto>();
                foreach (var rank in node.ranks)
                {
                    if (rank == null) continue;
                    if (rank.costs == null) rank.costs = Array.Empty<GrowthCostDto>();
                    if (rank.contributions == null)
                        rank.contributions = Array.Empty<GrowthContributionDto>();
                }
            }
            foreach (var policy in catalog.growthPolicies)
            {
                if (policy == null) continue;
                if (policy.permittedDomainIds == null)
                    policy.permittedDomainIds = Array.Empty<string>();
                if (policy.permittedAttributeIds == null)
                    policy.permittedAttributeIds = Array.Empty<string>();
                if (policy.permittedSourceIds == null)
                    policy.permittedSourceIds = Array.Empty<string>();
                if (policy.caps == null)
                    policy.caps = Array.Empty<GrowthPolicyCapDto>();
            }
        }

        private static void CanonicalizeCosts(GrowthCostDto[] costs)
        {
            Array.Sort(costs, (left, right) => CompareIds(
                left == null ? null : left.itemId,
                right == null ? null : right.itemId));
        }

        private static void CanonicalizeContributions(
            GrowthContributionDto[] contributions)
        {
            Array.Sort(contributions, (left, right) =>
            {
                var domain = CompareIds(left == null ? null : left.domainId,
                    right == null ? null : right.domainId);
                if (domain != 0) return domain;
                var attribute = CompareIds(
                    left == null ? null : left.attributeId,
                    right == null ? null : right.attributeId);
                if (attribute != 0) return attribute;
                var operation = CompareIds(
                    left == null ? null : left.operationId,
                    right == null ? null : right.operationId);
                if (operation != 0) return operation;
                var leftValue = left == null ? float.MaxValue : left.value;
                var rightValue = right == null ? float.MaxValue : right.value;
                return leftValue.CompareTo(rightValue);
            });
        }

        private static int CompareReward(RewardGrantDto left,
            RewardGrantDto right)
        {
            var operation = CompareIds(left == null ? null : left.operationId,
                right == null ? null : right.operationId);
            if (operation != 0) return operation;
            var leftId = left == null ? null
                : string.IsNullOrEmpty(left.itemId)
                    ? left.growthEquipmentId : left.itemId;
            var rightId = right == null ? null
                : string.IsNullOrEmpty(right.itemId)
                    ? right.growthEquipmentId : right.itemId;
            return CompareIds(leftId, rightId);
        }

        private static string[] SortedStrings(string[] source)
        {
            var copy = source == null ? Array.Empty<string>()
                : (string[])source.Clone();
            Array.Sort(copy, StringComparer.Ordinal);
            return copy;
        }

        private static int CompareIds(string left, string right)
        {
            return StringComparer.Ordinal.Compare(left ?? string.Empty,
                right ?? string.Empty);
        }

        private static int CompareRank(int left, int right)
        {
            return left.CompareTo(right);
        }
    }
}
