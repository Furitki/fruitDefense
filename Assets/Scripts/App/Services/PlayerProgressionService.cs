using System;
using System.Collections;
using System.Collections.Generic;
using FruitDefense.Content;

namespace FruitDefense.App.Services
{
    public enum PlayerProgressionCommandKind
    {
        ClaimActivity,
        EquipGrowthEquipment,
        UpgradeGrowthEquipment,
        UpgradeCultivation,
    }

    public enum PlayerProgressionCommandStatus
    {
        Success,
        InProgress,
        InvalidRequest,
        ContentNotFound,
        ActivityUnavailable,
        AlreadyClaimed,
        GrantConflict,
        NotOwned,
        IncompatibleSlot,
        AlreadyEquipped,
        InsufficientCost,
        PrerequisiteLocked,
        MaximumRank,
        InvalidProfile,
        PersistenceFailed,
    }

    public sealed class PlayerProgressionCommandResult
    {
        public PlayerProgressionCommandKind Kind { get; }
        public PlayerProgressionCommandStatus Status { get; }
        public string Identity { get; }
        public string RelatedIdentity { get; }
        public long RequiredQuantity { get; }
        public long AvailableQuantity { get; }
        public string Message { get; }
        public ProfileValidationResult Validation { get; }
        public PlayerProgressionProjection Projection { get; }
        public bool Succeeded => Status == PlayerProgressionCommandStatus.Success;

        public PlayerProgressionCommandResult(PlayerProgressionCommandKind kind,
            PlayerProgressionCommandStatus status, string identity,
            PlayerProgressionProjection projection, string relatedIdentity = "",
            long requiredQuantity = 0, long availableQuantity = 0,
            string message = "", ProfileValidationResult validation = default)
        {
            Kind = kind;
            Status = status;
            Identity = identity ?? string.Empty;
            RelatedIdentity = relatedIdentity ?? string.Empty;
            RequiredQuantity = requiredQuantity;
            AvailableQuantity = availableQuantity;
            Message = message ?? string.Empty;
            Validation = validation;
            Projection = projection;
        }
    }

    public sealed class PlayerProgressionService
    {
        private readonly IPlayerProfileStore _store;
        private readonly CompiledOutgameContentCatalog _content;
        private PlayerProfile _committed;
        private bool _commandInProgress;

        public PlayerProgressionService(IPlayerProfileStore store,
            CompiledOutgameContentCatalog content, PlayerProfile initialProfile)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            var validation = PlayerProfileCodec.Validate(initialProfile, content);
            if (!validation.Success)
                throw new ArgumentException(validation.Message,
                    nameof(initialProfile));
            _committed = PlayerProfileCodec.Clone(initialProfile, content);
            Current = PlayerProgressionProjection.Create(_committed, content);
        }

        public bool CommandInProgress => _commandInProgress;
        public PlayerProgressionProjection Current { get; private set; }
        public event Action<PlayerProgressionProjection> ProjectionPublished;

        /// <summary>
        /// Returns the current authoritative aggregate for App-layer integration.
        /// UI presenters continue to consume <see cref="Current"/> and never receive
        /// this mutable persistence DTO.
        /// </summary>
        internal PlayerProfile CreateCommittedProfileSnapshot()
        {
            return PlayerProfileCodec.Clone(_committed, _content);
        }

        public IEnumerator TryClaimActivity(string activityId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return Execute(PlayerProgressionCommandKind.ClaimActivity,
                activityId, string.Empty, completed);
        }

        public IEnumerator TryEquip(string growthEquipmentId, string slotId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return Execute(PlayerProgressionCommandKind.EquipGrowthEquipment,
                growthEquipmentId, slotId, completed);
        }

        public IEnumerator TryUpgradeGrowthEquipment(string growthEquipmentId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return Execute(PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                growthEquipmentId, string.Empty, completed);
        }

        public IEnumerator TryUpgradeCultivation(string cultivationNodeId,
            Action<PlayerProgressionCommandResult> completed)
        {
            return Execute(PlayerProgressionCommandKind.UpgradeCultivation,
                cultivationNodeId, string.Empty, completed);
        }

        private IEnumerator Execute(PlayerProgressionCommandKind kind,
            string identity, string secondaryIdentity,
            Action<PlayerProgressionCommandResult> completed)
        {
            var gate = new CompletionGate<PlayerProgressionCommandResult>(completed);
            if (_commandInProgress)
            {
                gate.Complete(Result(kind,
                    PlayerProgressionCommandStatus.InProgress, identity,
                    "Another profile command is already persisting."));
                yield break;
            }
            if (string.IsNullOrWhiteSpace(identity))
            {
                gate.Complete(Result(kind,
                    PlayerProgressionCommandStatus.InvalidRequest, identity,
                    "A stable command identity is required."));
                yield break;
            }

            _commandInProgress = true;
            try
            {
                var candidate = PlayerProfileCodec.Clone(_committed, _content);
                var failure = Apply(kind, candidate, identity, secondaryIdentity);
                if (failure != null)
                {
                    gate.Complete(failure);
                    yield break;
                }

                var validation = PlayerProfileCodec.Validate(candidate, _content);
                if (!validation.Success)
                {
                    gate.Complete(new PlayerProgressionCommandResult(kind,
                        PlayerProgressionCommandStatus.InvalidProfile, identity,
                        Current, message: validation.Message,
                        validation: validation));
                    yield break;
                }

                ProfileSaveResult save = null;
                var routine = _store.Save(candidate, value => save = value);
                Exception persistenceException = null;
                while (true)
                {
                    bool more;
                    object current = null;
                    try
                    {
                        more = routine.MoveNext();
                        if (more) current = routine.Current;
                    }
                    catch (Exception exception)
                    {
                        persistenceException = exception;
                        more = false;
                    }
                    if (!more) break;
                    yield return current;
                }

                if (persistenceException != null)
                {
                    gate.Complete(Result(kind,
                        PlayerProgressionCommandStatus.PersistenceFailed,
                        identity, persistenceException.Message));
                    yield break;
                }
                if (save == null || save.Status != ProfileSaveStatus.Success
                    || save.Profile == null)
                {
                    gate.Complete(Result(kind,
                        PlayerProgressionCommandStatus.PersistenceFailed,
                        identity, save?.Error ?? "Profile store did not complete."));
                    yield break;
                }
                if (save.Profile.revision != _committed.revision + 1)
                {
                    gate.Complete(Result(kind,
                        PlayerProgressionCommandStatus.PersistenceFailed,
                        identity,
                        "Profile store returned an unexpected revision."));
                    yield break;
                }

                var persistedValidation = PlayerProfileCodec.Validate(save.Profile,
                    _content);
                if (!persistedValidation.Success)
                {
                    gate.Complete(new PlayerProgressionCommandResult(kind,
                        PlayerProgressionCommandStatus.PersistenceFailed,
                        identity, Current, message: persistedValidation.Message,
                        validation: persistedValidation));
                    yield break;
                }

                _committed = PlayerProfileCodec.Clone(save.Profile, _content);
                Current = PlayerProgressionProjection.Create(_committed, _content);
                ProjectionPublished?.Invoke(Current);
                gate.Complete(new PlayerProgressionCommandResult(kind,
                    PlayerProgressionCommandStatus.Success, identity, Current));
            }
            finally
            {
                _commandInProgress = false;
            }
        }

        private PlayerProgressionCommandResult Apply(
            PlayerProgressionCommandKind kind, PlayerProfile candidate,
            string identity, string secondaryIdentity)
        {
            switch (kind)
            {
                case PlayerProgressionCommandKind.ClaimActivity:
                    return ApplyClaim(candidate, identity);
                case PlayerProgressionCommandKind.EquipGrowthEquipment:
                    return ApplyEquip(candidate, identity, secondaryIdentity);
                case PlayerProgressionCommandKind.UpgradeGrowthEquipment:
                    return ApplyEquipmentUpgrade(candidate, identity);
                case PlayerProgressionCommandKind.UpgradeCultivation:
                    return ApplyCultivationUpgrade(candidate, identity);
                default:
                    return Result(kind,
                        PlayerProgressionCommandStatus.InvalidRequest, identity,
                        "Unsupported profile command.");
            }
        }

        private PlayerProgressionCommandResult ApplyClaim(PlayerProfile candidate,
            string activityId)
        {
            if (!_content.Activities.TryGetValue(activityId, out var activity))
                return Result(PlayerProgressionCommandKind.ClaimActivity,
                    PlayerProgressionCommandStatus.ContentNotFound, activityId,
                    "Activity is absent from compiled content.");
            if (!activity.bundledAvailable)
                return Result(PlayerProgressionCommandKind.ClaimActivity,
                    PlayerProgressionCommandStatus.ActivityUnavailable, activityId,
                    "Activity is not available.");
            if (HasReceipt(candidate, activity.receiptId))
                return Result(PlayerProgressionCommandKind.ClaimActivity,
                    PlayerProgressionCommandStatus.AlreadyClaimed, activityId,
                    "Activity reward was already claimed.", activity.receiptId);

            var rewards = activity.rewards ?? Array.Empty<RewardGrantDto>();
            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                if (string.Equals(reward.operationId,
                        OutgameContentIds.RewardOperations.Item,
                        StringComparison.Ordinal))
                {
                    var failure = AddItemGrant(candidate, activityId, reward);
                    if (failure != null) return failure;
                }
                else if (string.Equals(reward.operationId,
                             OutgameContentIds.RewardOperations.GrowthEquipment,
                             StringComparison.Ordinal))
                {
                    if (FindEquipment(candidate, reward.growthEquipmentId) >= 0)
                    {
                        return Result(
                            PlayerProgressionCommandKind.ClaimActivity,
                            PlayerProgressionCommandStatus.GrantConflict,
                            activityId,
                            "Reward equipment is already owned.",
                            reward.growthEquipmentId);
                    }
                    AppendEquipment(candidate, new PlayerGrowthEquipment
                    {
                        growthEquipmentId = reward.growthEquipmentId,
                        rank = reward.initialRank,
                    });
                }
                else
                {
                    return Result(PlayerProgressionCommandKind.ClaimActivity,
                        PlayerProgressionCommandStatus.InvalidProfile, activityId,
                        "Compiled activity contains an unsupported reward operation.",
                        reward.operationId);
                }
            }

            AppendReceipt(candidate, new PlayerActivityReceipt
            {
                receiptId = activity.receiptId,
            });
            return null;
        }

        private PlayerProgressionCommandResult AddItemGrant(
            PlayerProfile candidate, string activityId, RewardGrantDto reward)
        {
            if (!_content.Items.TryGetValue(reward.itemId, out var item))
            {
                return Result(PlayerProgressionCommandKind.ClaimActivity,
                    PlayerProgressionCommandStatus.InvalidProfile, activityId,
                    "Compiled reward references an unknown item.", reward.itemId);
            }
            var itemIndex = FindItem(candidate, reward.itemId);
            var current = itemIndex < 0 ? 0 : candidate.itemBalances[itemIndex].quantity;
            long granted;
            try
            {
                granted = checked(current + reward.quantity);
            }
            catch (OverflowException)
            {
                return Result(PlayerProgressionCommandKind.ClaimActivity,
                    PlayerProgressionCommandStatus.GrantConflict, activityId,
                    "Reward quantity overflowed the profile balance.", reward.itemId);
            }
            if (granted > item.maximumQuantity)
            {
                return Result(PlayerProgressionCommandKind.ClaimActivity,
                    PlayerProgressionCommandStatus.GrantConflict, activityId,
                    "Reward exceeds the item maximum quantity.", reward.itemId,
                    item.maximumQuantity, current);
            }
            if (itemIndex < 0)
            {
                AppendItem(candidate, new PlayerItemBalance
                {
                    itemId = reward.itemId,
                    quantity = granted,
                });
            }
            else
            {
                candidate.itemBalances[itemIndex].quantity = granted;
            }
            return null;
        }

        private PlayerProgressionCommandResult ApplyEquip(PlayerProfile candidate,
            string growthEquipmentId, string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                return Result(PlayerProgressionCommandKind.EquipGrowthEquipment,
                    PlayerProgressionCommandStatus.InvalidRequest,
                    growthEquipmentId, "A growth slot identity is required.");
            if (!_content.GrowthEquipment.TryGetValue(growthEquipmentId,
                    out var definition))
            {
                return Result(PlayerProgressionCommandKind.EquipGrowthEquipment,
                    PlayerProgressionCommandStatus.ContentNotFound,
                    growthEquipmentId,
                    "Growth equipment is absent from compiled content.");
            }
            if (FindEquipment(candidate, growthEquipmentId) < 0)
                return Result(PlayerProgressionCommandKind.EquipGrowthEquipment,
                    PlayerProgressionCommandStatus.NotOwned, growthEquipmentId,
                    "Growth equipment is not owned.");
            if (!string.Equals(definition.slotId, slotId,
                    StringComparison.Ordinal))
            {
                return Result(PlayerProgressionCommandKind.EquipGrowthEquipment,
                    PlayerProgressionCommandStatus.IncompatibleSlot,
                    growthEquipmentId,
                    "Growth equipment is incompatible with the requested slot.",
                    slotId);
            }

            var entries = new List<PlayerGrowthLoadoutEntry>(
                candidate.growthLoadout.Length + 1);
            var alreadyEquipped = false;
            for (var index = 0; index < candidate.growthLoadout.Length; index++)
            {
                var entry = candidate.growthLoadout[index];
                if (string.Equals(entry.slotId, slotId, StringComparison.Ordinal))
                {
                    alreadyEquipped = string.Equals(entry.growthEquipmentId,
                        growthEquipmentId, StringComparison.Ordinal);
                    continue;
                }
                entries.Add(entry);
            }
            if (alreadyEquipped)
                return Result(PlayerProgressionCommandKind.EquipGrowthEquipment,
                    PlayerProgressionCommandStatus.AlreadyEquipped,
                    growthEquipmentId,
                    "Growth equipment is already equipped in this slot.", slotId);
            entries.Add(new PlayerGrowthLoadoutEntry
            {
                slotId = slotId,
                growthEquipmentId = growthEquipmentId,
            });
            candidate.growthLoadout = entries.ToArray();
            return null;
        }

        private PlayerProgressionCommandResult ApplyEquipmentUpgrade(
            PlayerProfile candidate, string growthEquipmentId)
        {
            if (!_content.GrowthEquipment.TryGetValue(growthEquipmentId,
                    out var definition))
            {
                return Result(PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                    PlayerProgressionCommandStatus.ContentNotFound,
                    growthEquipmentId,
                    "Growth equipment is absent from compiled content.");
            }
            var ownedIndex = FindEquipment(candidate, growthEquipmentId);
            if (ownedIndex < 0)
                return Result(PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                    PlayerProgressionCommandStatus.NotOwned, growthEquipmentId,
                    "Growth equipment is not owned.");
            var nextRank = candidate.ownedGrowthEquipment[ownedIndex].rank + 1;
            var rank = FindEquipmentRank(definition.ranks, nextRank);
            if (rank == null)
                return Result(PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                    PlayerProgressionCommandStatus.MaximumRank,
                    growthEquipmentId,
                    "Growth equipment is already at maximum rank.");
            var costFailure = ValidateCosts(
                PlayerProgressionCommandKind.UpgradeGrowthEquipment,
                growthEquipmentId, candidate, rank.costs);
            if (costFailure != null) return costFailure;
            Debit(candidate, rank.costs);
            candidate.ownedGrowthEquipment[ownedIndex].rank = nextRank;
            return null;
        }

        private PlayerProgressionCommandResult ApplyCultivationUpgrade(
            PlayerProfile candidate, string nodeId)
        {
            if (!_content.CultivationNodes.TryGetValue(nodeId, out var definition))
                return Result(PlayerProgressionCommandKind.UpgradeCultivation,
                    PlayerProgressionCommandStatus.ContentNotFound, nodeId,
                    "Cultivation node is absent from compiled content.");

            var prerequisites = definition.prerequisites
                ?? Array.Empty<CultivationPrerequisiteDto>();
            for (var index = 0; index < prerequisites.Length; index++)
            {
                var prerequisite = prerequisites[index];
                var actual = GetCultivationRank(candidate, prerequisite.nodeId);
                if (actual >= prerequisite.requiredRank) continue;
                return Result(PlayerProgressionCommandKind.UpgradeCultivation,
                    PlayerProgressionCommandStatus.PrerequisiteLocked, nodeId,
                    "Cultivation prerequisite is not satisfied.",
                    prerequisite.nodeId, prerequisite.requiredRank, actual);
            }

            var ownedIndex = FindCultivation(candidate, nodeId);
            var currentRank = ownedIndex < 0 ? 0
                : candidate.cultivationRanks[ownedIndex].rank;
            var nextRank = currentRank + 1;
            var rank = FindCultivationRank(definition.ranks, nextRank);
            if (rank == null)
                return Result(PlayerProgressionCommandKind.UpgradeCultivation,
                    PlayerProgressionCommandStatus.MaximumRank, nodeId,
                    "Cultivation node is already at maximum rank.");
            var costFailure = ValidateCosts(
                PlayerProgressionCommandKind.UpgradeCultivation,
                nodeId, candidate, rank.costs);
            if (costFailure != null) return costFailure;
            Debit(candidate, rank.costs);
            if (ownedIndex < 0)
            {
                AppendCultivation(candidate, new PlayerCultivationRank
                {
                    cultivationNodeId = nodeId,
                    rank = nextRank,
                });
            }
            else
            {
                candidate.cultivationRanks[ownedIndex].rank = nextRank;
            }
            return null;
        }

        private PlayerProgressionCommandResult ValidateCosts(
            PlayerProgressionCommandKind kind, string identity,
            PlayerProfile candidate, GrowthCostDto[] costs)
        {
            var values = costs ?? Array.Empty<GrowthCostDto>();
            for (var index = 0; index < values.Length; index++)
            {
                var cost = values[index];
                var itemIndex = FindItem(candidate, cost.itemId);
                var available = itemIndex < 0 ? 0
                    : candidate.itemBalances[itemIndex].quantity;
                if (available >= cost.quantity) continue;
                return Result(kind,
                    PlayerProgressionCommandStatus.InsufficientCost, identity,
                    "The complete item cost is not available.", cost.itemId,
                    cost.quantity, available);
            }
            return null;
        }

        private static void Debit(PlayerProfile candidate, GrowthCostDto[] costs)
        {
            var values = costs ?? Array.Empty<GrowthCostDto>();
            for (var index = 0; index < values.Length; index++)
            {
                var cost = values[index];
                var itemIndex = FindItem(candidate, cost.itemId);
                candidate.itemBalances[itemIndex].quantity -= cost.quantity;
            }
        }

        private PlayerProgressionCommandResult Result(
            PlayerProgressionCommandKind kind,
            PlayerProgressionCommandStatus status, string identity,
            string message, string relatedIdentity = "",
            long requiredQuantity = 0, long availableQuantity = 0)
        {
            return new PlayerProgressionCommandResult(kind, status, identity,
                Current, relatedIdentity, requiredQuantity, availableQuantity,
                message);
        }

        private static bool HasReceipt(PlayerProfile profile, string receiptId)
        {
            for (var index = 0; index < profile.activityReceipts.Length; index++)
            {
                if (string.Equals(profile.activityReceipts[index].receiptId,
                        receiptId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static int FindItem(PlayerProfile profile, string itemId)
        {
            for (var index = 0; index < profile.itemBalances.Length; index++)
                if (string.Equals(profile.itemBalances[index].itemId, itemId,
                        StringComparison.Ordinal)) return index;
            return -1;
        }

        private static int FindEquipment(PlayerProfile profile,
            string growthEquipmentId)
        {
            for (var index = 0;
                 index < profile.ownedGrowthEquipment.Length; index++)
            {
                if (string.Equals(
                        profile.ownedGrowthEquipment[index].growthEquipmentId,
                        growthEquipmentId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static int FindCultivation(PlayerProfile profile, string nodeId)
        {
            for (var index = 0; index < profile.cultivationRanks.Length; index++)
                if (string.Equals(
                        profile.cultivationRanks[index].cultivationNodeId,
                        nodeId, StringComparison.Ordinal)) return index;
            return -1;
        }

        private static int GetCultivationRank(PlayerProfile profile,
            string nodeId)
        {
            var index = FindCultivation(profile, nodeId);
            return index < 0 ? 0 : profile.cultivationRanks[index].rank;
        }

        private static GrowthEquipmentRankDefinitionDto FindEquipmentRank(
            GrowthEquipmentRankDefinitionDto[] ranks, int rank)
        {
            if (ranks == null) return null;
            for (var index = 0; index < ranks.Length; index++)
                if (ranks[index] != null && ranks[index].rank == rank)
                    return ranks[index];
            return null;
        }

        private static CultivationRankDefinitionDto FindCultivationRank(
            CultivationRankDefinitionDto[] ranks, int rank)
        {
            if (ranks == null) return null;
            for (var index = 0; index < ranks.Length; index++)
                if (ranks[index] != null && ranks[index].rank == rank)
                    return ranks[index];
            return null;
        }

        private static void AppendItem(PlayerProfile profile,
            PlayerItemBalance item)
        {
            var entries = new PlayerItemBalance[profile.itemBalances.Length + 1];
            Array.Copy(profile.itemBalances, entries, profile.itemBalances.Length);
            entries[entries.Length - 1] = item;
            profile.itemBalances = entries;
        }

        private static void AppendReceipt(PlayerProfile profile,
            PlayerActivityReceipt receipt)
        {
            var entries = new PlayerActivityReceipt[
                profile.activityReceipts.Length + 1];
            Array.Copy(profile.activityReceipts, entries,
                profile.activityReceipts.Length);
            entries[entries.Length - 1] = receipt;
            profile.activityReceipts = entries;
        }

        private static void AppendEquipment(PlayerProfile profile,
            PlayerGrowthEquipment equipment)
        {
            var entries = new PlayerGrowthEquipment[
                profile.ownedGrowthEquipment.Length + 1];
            Array.Copy(profile.ownedGrowthEquipment, entries,
                profile.ownedGrowthEquipment.Length);
            entries[entries.Length - 1] = equipment;
            profile.ownedGrowthEquipment = entries;
        }

        private static void AppendCultivation(PlayerProfile profile,
            PlayerCultivationRank cultivation)
        {
            var entries = new PlayerCultivationRank[
                profile.cultivationRanks.Length + 1];
            Array.Copy(profile.cultivationRanks, entries,
                profile.cultivationRanks.Length);
            entries[entries.Length - 1] = cultivation;
            profile.cultivationRanks = entries;
        }
    }
}
