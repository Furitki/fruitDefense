using System;
using System.Linq;
using FruitDefense.App.Services;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Shell
{
    internal readonly struct HubActivityRewardPresentation
    {
        public string Equipment { get; }
        public string Item { get; }

        public HubActivityRewardPresentation(string equipment, string item)
        {
            Equipment = equipment ?? string.Empty;
            Item = item ?? string.Empty;
        }
    }

    internal static class ActivityHubPageModel
    {
        public static ActivityDefinitionDto SelectPrimaryActivity(
            CompiledOutgameContentCatalog catalog)
        {
            return catalog?.Activities
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => value.Value)
                .FirstOrDefault();
        }

        public static HubActivityState ResolveState(
            ActivityDefinitionDto definition,
            PlayerProgressionProjection progression, bool commandBusy,
            PlayerProgressionCommandResult lastResult)
        {
            if (definition == null || !definition.bundledAvailable)
                return HubActivityState.Locked;
            if (progression == null)
                return HubActivityState.InsufficientContext;
            if (progression.HasReceipt(definition.receiptId))
                return HubActivityState.Claimed;
            if (commandBusy) return HubActivityState.Claiming;
            if (Matches(lastResult,
                    PlayerProgressionCommandKind.ClaimActivity, definition.id))
            {
                if (lastResult.Status == PlayerProgressionCommandStatus.Success
                    || lastResult.Status
                    == PlayerProgressionCommandStatus.AlreadyClaimed)
                    return HubActivityState.Claimed;
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.ActivityUnavailable)
                    return HubActivityState.Locked;
                if (!lastResult.Succeeded) return HubActivityState.Error;
            }
            return HubActivityState.Claimable;
        }

        public static HubActivityRewardPresentation ResolveRewards(
            ActivityDefinitionDto activity,
            CompiledOutgameContentCatalog catalog)
        {
            var equipmentReward = "成长装备";
            var itemReward = "成长素材";
            var rewards = activity?.rewards ?? Array.Empty<RewardGrantDto>();
            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                if (string.Equals(reward.operationId,
                        OutgameContentIds.RewardOperations.GrowthEquipment,
                        StringComparison.Ordinal)
                    && catalog != null
                    && catalog.GrowthEquipment.TryGetValue(
                        reward.growthEquipmentId, out var equipment))
                {
                    equipmentReward = equipment.displayName + " × "
                        + reward.quantity;
                }
                else if (string.Equals(reward.operationId,
                             OutgameContentIds.RewardOperations.Item,
                             StringComparison.Ordinal)
                         && catalog != null
                         && catalog.Items.TryGetValue(reward.itemId,
                             out var item))
                {
                    itemReward = item.displayName + " × " + reward.quantity;
                }
            }
            return new HubActivityRewardPresentation(
                equipmentReward, itemReward);
        }

        public static RuntimeUiCopyId StatusCopy(HubActivityState state)
        {
            switch (state)
            {
                case HubActivityState.Claiming:
                    return RuntimeUiCopyId.HubActivityClaiming;
                case HubActivityState.Claimed:
                    return RuntimeUiCopyId.HubActivityClaimed;
                case HubActivityState.Locked:
                    return RuntimeUiCopyId.HubActivityLocked;
                case HubActivityState.Error:
                    return RuntimeUiCopyId.HubActivityError;
                case HubActivityState.InsufficientContext:
                    return RuntimeUiCopyId.HubActivityUnavailableBody;
                default:
                    return RuntimeUiCopyId.HubActivityClaimable;
            }
        }

        public static RuntimeUiCopyId ActionCopy(HubActivityState state)
        {
            switch (state)
            {
                case HubActivityState.Claiming:
                    return RuntimeUiCopyId.HubActivityClaiming;
                case HubActivityState.Claimed:
                    return RuntimeUiCopyId.HubActivityClaimed;
                case HubActivityState.Locked:
                    return RuntimeUiCopyId.HubActivityLocked;
                case HubActivityState.InsufficientContext:
                    return RuntimeUiCopyId.HubActivityLocked;
                default:
                    return RuntimeUiCopyId.HubActivityClaim;
            }
        }

        public static RuntimeUiInteractionState VisualState(
            HubActivityState state)
        {
            switch (state)
            {
                case HubActivityState.Claiming:
                    return RuntimeUiInteractionState.Loading;
                case HubActivityState.Claimed:
                    return RuntimeUiInteractionState.Success;
                case HubActivityState.Locked:
                case HubActivityState.InsufficientContext:
                    return RuntimeUiInteractionState.Disabled;
                case HubActivityState.Error:
                    return RuntimeUiInteractionState.Error;
                default:
                    return RuntimeUiInteractionState.Normal;
            }
        }

        private static bool Matches(PlayerProgressionCommandResult result,
            PlayerProgressionCommandKind kind, string identity)
        {
            return result != null && result.Kind == kind
                && string.Equals(result.Identity, identity,
                    StringComparison.Ordinal);
        }
    }

    internal static class GrowthHubPageModel
    {
        public static HubGrowthState ResolveEquipmentState(
            GrowthEquipmentDefinitionDto definition,
            PlayerProgressionProjection progression, bool commandBusy,
            PlayerProgressionCommandResult lastResult)
        {
            var eligibility = ResolveEquipmentEligibility(definition,
                progression, commandBusy);
            if (eligibility == HubGrowthState.Locked
                || eligibility == HubGrowthState.Loading)
                return eligibility;
            if (MatchesEquipmentResult(lastResult, definition.id))
            {
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.PersistenceFailed
                    || lastResult.Status
                    == PlayerProgressionCommandStatus.InvalidProfile)
                    return HubGrowthState.Error;
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.InsufficientCost)
                    return HubGrowthState.Insufficient;
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.MaximumRank)
                    return HubGrowthState.Maximum;
                if (lastResult.Succeeded) return HubGrowthState.Success;
            }
            return eligibility;
        }

        public static HubGrowthState ResolveEquipmentEligibility(
            GrowthEquipmentDefinitionDto definition,
            PlayerProgressionProjection progression, bool commandBusy)
        {
            if (definition == null || progression == null
                || !progression.TryGetGrowthEquipmentRank(definition.id,
                    out var rank))
                return HubGrowthState.Locked;
            if (commandBusy) return HubGrowthState.Loading;
            if (!progression.TryGetEquipped(definition.slotId,
                    out var equipped)
                || !string.Equals(equipped, definition.id,
                    StringComparison.Ordinal))
                return HubGrowthState.Owned;
            var nextRank = FindEquipmentRank(definition, rank + 1);
            if (nextRank == null) return HubGrowthState.Maximum;
            return CostsAffordable(nextRank.costs, progression)
                ? HubGrowthState.Upgradeable
                : HubGrowthState.Insufficient;
        }

        public static HubGrowthState ResolveCultivationState(
            CultivationNodeDefinitionDto definition,
            PlayerProgressionProjection progression, bool commandBusy,
            PlayerProgressionCommandResult lastResult)
        {
            var eligibility = ResolveCultivationEligibility(definition,
                progression, commandBusy);
            if (eligibility == HubGrowthState.Locked
                || eligibility == HubGrowthState.Loading)
                return eligibility;
            if (Matches(lastResult,
                    PlayerProgressionCommandKind.UpgradeCultivation,
                    definition.id))
            {
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.PersistenceFailed
                    || lastResult.Status
                    == PlayerProgressionCommandStatus.InvalidProfile)
                    return HubGrowthState.Error;
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.PrerequisiteLocked)
                    return HubGrowthState.Locked;
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.InsufficientCost)
                    return HubGrowthState.Insufficient;
                if (lastResult.Status
                    == PlayerProgressionCommandStatus.MaximumRank)
                    return HubGrowthState.Maximum;
                if (lastResult.Succeeded) return HubGrowthState.Success;
            }
            return eligibility;
        }

        public static HubGrowthState ResolveCultivationEligibility(
            CultivationNodeDefinitionDto definition,
            PlayerProgressionProjection progression, bool commandBusy)
        {
            if (definition == null || progression == null)
                return HubGrowthState.Locked;
            var prerequisites = definition.prerequisites
                ?? Array.Empty<CultivationPrerequisiteDto>();
            for (var index = 0; index < prerequisites.Length; index++)
            {
                var prerequisite = prerequisites[index];
                if (progression.CultivationRank(prerequisite.nodeId)
                    < prerequisite.requiredRank)
                    return HubGrowthState.Locked;
            }
            if (commandBusy) return HubGrowthState.Loading;
            var nextRank = FindCultivationRank(definition,
                progression.CultivationRank(definition.id) + 1);
            if (nextRank == null) return HubGrowthState.Maximum;
            return CostsAffordable(nextRank.costs, progression)
                ? HubGrowthState.Upgradeable
                : HubGrowthState.Insufficient;
        }

        public static HubGrowthPrimaryAction ResolvePrimaryAction(
            GrowthPageId page, HubGrowthState state, bool equipped)
        {
            if (state == HubGrowthState.Loading
                || state == HubGrowthState.Locked
                || state == HubGrowthState.Insufficient
                || state == HubGrowthState.Maximum)
                return HubGrowthPrimaryAction.None;
            if (page == GrowthPageId.Cultivation)
                return HubGrowthPrimaryAction.UpgradeCultivation;
            return equipped
                ? HubGrowthPrimaryAction.UpgradeEquipment
                : HubGrowthPrimaryAction.Equip;
        }

        public static GrowthEquipmentRankDefinitionDto FindEquipmentRank(
            GrowthEquipmentDefinitionDto definition, int rank)
        {
            return (definition?.ranks
                    ?? Array.Empty<GrowthEquipmentRankDefinitionDto>())
                .FirstOrDefault(value => value != null && value.rank == rank);
        }

        public static CultivationRankDefinitionDto FindCultivationRank(
            CultivationNodeDefinitionDto definition, int rank)
        {
            return (definition?.ranks
                    ?? Array.Empty<CultivationRankDefinitionDto>())
                .FirstOrDefault(value => value != null && value.rank == rank);
        }

        public static int MaximumRank(GrowthEquipmentDefinitionDto definition)
        {
            var ranks = definition?.ranks
                ?? Array.Empty<GrowthEquipmentRankDefinitionDto>();
            return ranks.Length == 0 ? 0 : ranks.Max(value => value.rank);
        }

        public static int MaximumRank(CultivationNodeDefinitionDto definition)
        {
            var ranks = definition?.ranks
                ?? Array.Empty<CultivationRankDefinitionDto>();
            return ranks.Length == 0 ? 0 : ranks.Max(value => value.rank);
        }

        public static string FormatCost(GrowthCostDto[] costs,
            CompiledOutgameContentCatalog catalog,
            PlayerProgressionProjection progression)
        {
            var value = (costs ?? Array.Empty<GrowthCostDto>())
                .FirstOrDefault();
            if (value == null) return "无需材料";
            var itemName = catalog != null
                && catalog.Items.TryGetValue(value.itemId, out var item)
                    ? item.displayName : "成长素材";
            var owned = progression?.ItemQuantity(value.itemId) ?? 0;
            return RuntimeUiCopyCatalog.FormatHubCost(itemName,
                value.quantity, owned);
        }

        public static string FormatPrerequisite(
            CultivationNodeDefinitionDto definition,
            CompiledOutgameContentCatalog catalog,
            PlayerProgressionProjection progression)
        {
            var prerequisites = definition?.prerequisites
                ?? Array.Empty<CultivationPrerequisiteDto>();
            for (var index = 0; index < prerequisites.Length; index++)
            {
                var prerequisite = prerequisites[index];
                if (progression != null
                    && progression.CultivationRank(prerequisite.nodeId)
                    >= prerequisite.requiredRank)
                    continue;
                var label = catalog != null
                    && catalog.CultivationNodes.TryGetValue(
                        prerequisite.nodeId, out var node)
                        ? node.displayName
                        : prerequisite.nodeId;
                return "解锁条件：" + label + " 等级 "
                    + prerequisite.requiredRank;
            }
            return RuntimeUiCopyCatalog.Get(
                RuntimeUiCopyId.HubCultivationLocked).Text;
        }

        public static string FormatContribution(
            GrowthContributionDto[] contributions)
        {
            var value = (contributions
                ?? Array.Empty<GrowthContributionDto>()).FirstOrDefault();
            if (value == null) return "当前等级没有成长加成";
            var label = string.Equals(value.attributeId,
                "attribute.damage", StringComparison.Ordinal)
                ? "守卫伤害" : "阳光收益";
            return RuntimeUiCopyCatalog.FormatHubPercentEffect(label,
                value.value);
        }

        public static RuntimeUiCopyId StatusCopy(HubGrowthState state,
            bool cultivation)
        {
            switch (state)
            {
                case HubGrowthState.Owned:
                    return RuntimeUiCopyId.HubGrowthOwned;
                case HubGrowthState.Equipped:
                    return RuntimeUiCopyId.HubGrowthEquipped;
                case HubGrowthState.Upgradeable:
                    return cultivation ? RuntimeUiCopyId.HubCultivationReady
                        : RuntimeUiCopyId.HubGrowthEquipped;
                case HubGrowthState.Insufficient:
                    return RuntimeUiCopyId.HubGrowthInsufficient;
                case HubGrowthState.Locked:
                    return cultivation ? RuntimeUiCopyId.HubCultivationLocked
                        : RuntimeUiCopyId.HubGrowthLocked;
                case HubGrowthState.Maximum:
                    return cultivation ? RuntimeUiCopyId.HubCultivationMaximum
                        : RuntimeUiCopyId.HubGrowthMaximum;
                case HubGrowthState.Loading:
                    return RuntimeUiCopyId.HubGrowthLoading;
                case HubGrowthState.Error:
                    return RuntimeUiCopyId.HubGrowthError;
                default:
                    return cultivation ? RuntimeUiCopyId.HubCultivationReady
                        : RuntimeUiCopyId.HubGrowthEquipped;
            }
        }

        public static RuntimeUiCopyId ActionCopy(
            HubGrowthPrimaryAction action, HubGrowthState state,
            bool cultivation)
        {
            if (state == HubGrowthState.Loading)
                return RuntimeUiCopyId.HubGrowthLoading;
            if (state == HubGrowthState.Insufficient)
                return RuntimeUiCopyId.HubGrowthInsufficient;
            if (state == HubGrowthState.Maximum)
                return cultivation ? RuntimeUiCopyId.HubCultivationMaximum
                    : RuntimeUiCopyId.HubGrowthMaximum;
            if (state == HubGrowthState.Locked)
                return cultivation
                    ? RuntimeUiCopyId.HubCultivationLockedAction
                    : RuntimeUiCopyId.HubGrowthLocked;
            switch (action)
            {
                case HubGrowthPrimaryAction.Equip:
                    return RuntimeUiCopyId.HubGrowthEquip;
                case HubGrowthPrimaryAction.UpgradeCultivation:
                    return RuntimeUiCopyId.HubCultivationUpgrade;
                default:
                    return RuntimeUiCopyId.HubGrowthUpgrade;
            }
        }

        public static RuntimeUiInteractionState VisualState(
            HubGrowthState state)
        {
            switch (state)
            {
                case HubGrowthState.Loading:
                    return RuntimeUiInteractionState.Loading;
                case HubGrowthState.Locked:
                    return RuntimeUiInteractionState.Disabled;
                case HubGrowthState.Insufficient:
                    return RuntimeUiInteractionState.Warning;
                case HubGrowthState.Maximum:
                case HubGrowthState.Success:
                    return RuntimeUiInteractionState.Success;
                case HubGrowthState.Error:
                    return RuntimeUiInteractionState.Error;
                default:
                    return RuntimeUiInteractionState.Normal;
            }
        }

        private static bool CostsAffordable(GrowthCostDto[] costs,
            PlayerProgressionProjection progression)
        {
            var values = costs ?? Array.Empty<GrowthCostDto>();
            for (var index = 0; index < values.Length; index++)
                if (progression.ItemQuantity(values[index].itemId)
                    < values[index].quantity) return false;
            return true;
        }

        private static bool Matches(PlayerProgressionCommandResult result,
            PlayerProgressionCommandKind kind, string identity)
        {
            return result != null && result.Kind == kind
                && string.Equals(result.Identity, identity,
                    StringComparison.Ordinal);
        }

        private static bool MatchesEquipmentResult(
            PlayerProgressionCommandResult result, string identity)
        {
            return result != null
                && (result.Kind
                        == PlayerProgressionCommandKind.EquipGrowthEquipment
                    || result.Kind
                        == PlayerProgressionCommandKind.UpgradeGrowthEquipment)
                && string.Equals(result.Identity, identity,
                    StringComparison.Ordinal);
        }
    }

    internal static class HomeHubPageModel
    {
        public static RuntimeUiInteractionState ResolvePreviewState(
            BattleGrowthResolution preview, bool transitioning)
        {
            if (transitioning) return RuntimeUiInteractionState.Loading;
            if (!preview.Succeeded) return RuntimeUiInteractionState.Error;
            if (preview.Snapshot.SourceRecords.Any(value => value.Disposition
                    == BattleGrowthSourceDisposition.Suppressed))
                return RuntimeUiInteractionState.Warning;
            return preview.Snapshot.SourceRecords.Count > 0
                ? RuntimeUiInteractionState.Success
                : RuntimeUiInteractionState.Disabled;
        }

        public static string FormatPreview(BattleGrowthResolution preview,
            CompiledOutgameContentCatalog catalog)
        {
            if (!preview.Succeeded || catalog == null)
                return RuntimeUiCopyCatalog.Get(
                    RuntimeUiCopyId.HubGrowthPreviewError).Text;
            var snapshot = preview.Snapshot;
            var policy = catalog.ResolveGrowthPolicy(snapshot.PolicyId);
            if (snapshot.SourceRecords.Count == 0)
                return policy.displayName + "\n"
                    + RuntimeUiCopyCatalog.Get(
                        RuntimeUiCopyId.HubGrowthPreviewEmpty).Text;
            var applied = snapshot.SourceRecords.Count(value =>
                value.Disposition == BattleGrowthSourceDisposition.Applied);
            var suppressed = snapshot.SourceRecords.Count - applied;
            var aggregate = snapshot.AggregateModifiers.FirstOrDefault();
            var effect = aggregate == null ? string.Empty
                : " · " + RuntimeUiCopyCatalog.FormatHubPercentEffect(
                    string.Equals(aggregate.AttributeId, "attribute.damage",
                        StringComparison.Ordinal) ? "伤害" : "收益",
                    aggregate.Additive);
            return policy.displayName + "\n生效 " + applied + " 项 · 受限 "
                + suppressed + " 项" + effect;
        }

    }
}
