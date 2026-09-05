using System;
using System.Collections.Generic;
using FruitDefense.Content;

namespace FruitDefense.App.Services
{
    public static partial class PlayerProfileCodec
    {
        public static ProfileValidationResult Validate(PlayerProfile profile,
            CompiledOutgameContentCatalog content)
        {
            var structural = Validate(profile);
            if (!structural.Success) return structural;
            if (content == null)
            {
                return Fail(ProfileValidationCode.MissingContent, "content",
                    string.Empty,
                    "Compiled outgame content is required for profile validation.");
            }

            for (var index = 0; index < profile.itemBalances.Length; index++)
            {
                var entry = profile.itemBalances[index];
                if (!content.Items.TryGetValue(entry.itemId, out var definition))
                {
                    return Fail(ProfileValidationCode.UnknownItem,
                        "itemBalances[" + index + "].itemId", entry.itemId,
                        "Profile item identity is absent from compiled outgame content.");
                }
                if (entry.quantity > definition.maximumQuantity)
                {
                    return Fail(ProfileValidationCode.InvalidItemQuantity,
                        "itemBalances[" + index + "].quantity", entry.itemId,
                        "Profile item quantity exceeds its content-defined maximum.");
                }
            }

            for (var index = 0; index < profile.activityReceipts.Length; index++)
            {
                var receipt = profile.activityReceipts[index];
                if (!TryResolveReceipt(content, receipt.receiptId, out _))
                {
                    return Fail(ProfileValidationCode.UnknownActivityReceipt,
                        "activityReceipts[" + index + "].receiptId",
                        receipt.receiptId,
                        "Profile receipt is absent from compiled outgame content.");
                }
            }

            for (var index = 0; index < profile.ownedGrowthEquipment.Length; index++)
            {
                var owned = profile.ownedGrowthEquipment[index];
                if (!content.GrowthEquipment.TryGetValue(
                        owned.growthEquipmentId, out var definition))
                {
                    return Fail(ProfileValidationCode.UnknownGrowthEquipment,
                        "ownedGrowthEquipment[" + index + "].growthEquipmentId",
                        owned.growthEquipmentId,
                        "Owned growth equipment is absent from compiled content.");
                }
                if (!ContainsRank(definition.ranks, owned.rank))
                {
                    return Fail(ProfileValidationCode.InvalidGrowthEquipmentRank,
                        "ownedGrowthEquipment[" + index + "].rank",
                        owned.growthEquipmentId,
                        "Owned growth-equipment rank is not content-defined.");
                }
            }

            for (var index = 0; index < profile.growthLoadout.Length; index++)
            {
                var entry = profile.growthLoadout[index];
                if (!content.GrowthEquipment.TryGetValue(
                        entry.growthEquipmentId, out var definition))
                {
                    return Fail(ProfileValidationCode.UnknownGrowthEquipment,
                        "growthLoadout[" + index + "].growthEquipmentId",
                        entry.growthEquipmentId,
                        "Equipped growth equipment is absent from compiled content.");
                }
                if (!string.Equals(entry.slotId, definition.slotId,
                        StringComparison.Ordinal))
                {
                    return Fail(ProfileValidationCode.InvalidGrowthEquipmentSlot,
                        "growthLoadout[" + index + "].slotId", entry.slotId,
                        "Growth equipment is assigned to an incompatible slot.");
                }
            }

            var cultivationRanks = new Dictionary<string, int>(
                profile.cultivationRanks.Length, StringComparer.Ordinal);
            for (var index = 0; index < profile.cultivationRanks.Length; index++)
            {
                var entry = profile.cultivationRanks[index];
                cultivationRanks.Add(entry.cultivationNodeId, entry.rank);
                if (!content.CultivationNodes.TryGetValue(
                        entry.cultivationNodeId, out var definition))
                {
                    return Fail(ProfileValidationCode.UnknownCultivationNode,
                        "cultivationRanks[" + index + "].cultivationNodeId",
                        entry.cultivationNodeId,
                        "Cultivation node is absent from compiled content.");
                }
                if (!ContainsRank(definition.ranks, entry.rank))
                {
                    return Fail(ProfileValidationCode.InvalidCultivationRank,
                        "cultivationRanks[" + index + "].rank",
                        entry.cultivationNodeId,
                        "Cultivation rank is not content-defined.");
                }
            }

            for (var index = 0; index < profile.cultivationRanks.Length; index++)
            {
                var entry = profile.cultivationRanks[index];
                var definition = content.CultivationNodes[entry.cultivationNodeId];
                var prerequisites = definition.prerequisites
                    ?? Array.Empty<CultivationPrerequisiteDto>();
                for (var prerequisiteIndex = 0;
                     prerequisiteIndex < prerequisites.Length;
                     prerequisiteIndex++)
                {
                    var prerequisite = prerequisites[prerequisiteIndex];
                    cultivationRanks.TryGetValue(prerequisite.nodeId,
                        out var actualRank);
                    if (actualRank >= prerequisite.requiredRank) continue;
                    return Fail(ProfileValidationCode.InvalidCultivationPrerequisite,
                        "cultivationRanks[" + index + "]",
                        entry.cultivationNodeId,
                        "Stored cultivation rank does not satisfy prerequisite '"
                        + prerequisite.nodeId + "' rank "
                        + prerequisite.requiredRank + ".");
                }
            }

            return structural;
        }

        public static ProfileValidationResult TryDeserialize(string json,
            CompiledOutgameContentCatalog content, out PlayerProfile profile)
        {
            var structural = TryDeserialize(json, out profile);
            if (!structural.Success) return structural;
            var complete = Validate(profile, content);
            if (!complete.Success) profile = null;
            return complete;
        }

        public static string Serialize(PlayerProfile profile,
            CompiledOutgameContentCatalog content)
        {
            var validation = Validate(profile, content);
            if (!validation.Success)
                throw new InvalidOperationException(validation.Message);
            return Serialize(profile);
        }

        public static PlayerProfile Clone(PlayerProfile profile,
            CompiledOutgameContentCatalog content)
        {
            var json = Serialize(profile, content);
            var validation = TryDeserialize(json, content, out var clone);
            if (!validation.Success)
                throw new InvalidOperationException(validation.Message);
            return clone;
        }

        internal static bool TryResolveReceipt(
            CompiledOutgameContentCatalog content, string receiptId,
            out ActivityDefinitionDto activity)
        {
            foreach (var pair in content.Activities)
            {
                if (!string.Equals(pair.Value.receiptId, receiptId,
                        StringComparison.Ordinal))
                    continue;
                activity = pair.Value;
                return true;
            }
            activity = null;
            return false;
        }

        private static bool ContainsRank(
            GrowthEquipmentRankDefinitionDto[] ranks, int rank)
        {
            if (ranks == null) return false;
            for (var index = 0; index < ranks.Length; index++)
                if (ranks[index] != null && ranks[index].rank == rank) return true;
            return false;
        }

        private static bool ContainsRank(CultivationRankDefinitionDto[] ranks,
            int rank)
        {
            if (ranks == null) return false;
            for (var index = 0; index < ranks.Length; index++)
                if (ranks[index] != null && ranks[index].rank == rank) return true;
            return false;
        }
    }
}
