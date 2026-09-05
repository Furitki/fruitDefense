using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FruitDefense.Content;

namespace FruitDefense.App.Services
{
    public readonly struct PlayerItemBalanceView
    {
        public PlayerItemBalanceView(string itemId, long quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public string ItemId { get; }
        public long Quantity { get; }
    }

    public readonly struct PlayerGrowthEquipmentView
    {
        public PlayerGrowthEquipmentView(string growthEquipmentId, int rank)
        {
            GrowthEquipmentId = growthEquipmentId;
            Rank = rank;
        }

        public string GrowthEquipmentId { get; }
        public int Rank { get; }
    }

    public readonly struct PlayerGrowthLoadoutView
    {
        public PlayerGrowthLoadoutView(string slotId, string growthEquipmentId)
        {
            SlotId = slotId;
            GrowthEquipmentId = growthEquipmentId;
        }

        public string SlotId { get; }
        public string GrowthEquipmentId { get; }
    }

    public readonly struct PlayerCultivationRankView
    {
        public PlayerCultivationRankView(string cultivationNodeId, int rank)
        {
            CultivationNodeId = cultivationNodeId;
            Rank = rank;
        }

        public string CultivationNodeId { get; }
        public int Rank { get; }
    }

    public sealed class PlayerProgressionProjection
    {
        private readonly HashSet<string> _receipts;
        private readonly Dictionary<string, long> _itemQuantities;
        private readonly Dictionary<string, int> _equipmentRanks;
        private readonly Dictionary<string, string> _loadout;
        private readonly Dictionary<string, int> _cultivationRanks;

        private PlayerProgressionProjection(PlayerProfile profile)
        {
            ProfileId = profile.profileId;
            Revision = profile.revision;
            CreatedAtUtc = profile.createdAtUtc;
            UpdatedAtUtc = profile.updatedAtUtc;
            Locale = profile.locale;
            MusicVolume = profile.musicVolume;
            SoundVolume = profile.soundVolume;
            VibrationEnabled = profile.vibrationEnabled;
            LastSelectedLevelId = profile.lastSelectedLevelId;
            ShowBattleTips = profile.showBattleTips;
            ConfirmBeforeBattleRestart = profile.confirmBeforeBattleRestart;

            var balances = new PlayerItemBalanceView[profile.itemBalances.Length];
            _itemQuantities = new Dictionary<string, long>(
                profile.itemBalances.Length, StringComparer.Ordinal);
            for (var index = 0; index < balances.Length; index++)
            {
                var source = profile.itemBalances[index];
                balances[index] = new PlayerItemBalanceView(source.itemId,
                    source.quantity);
                _itemQuantities.Add(source.itemId, source.quantity);
            }
            ItemBalances = Array.AsReadOnly(balances);

            var receiptIds = new string[profile.activityReceipts.Length];
            _receipts = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < receiptIds.Length; index++)
            {
                receiptIds[index] = profile.activityReceipts[index].receiptId;
                _receipts.Add(receiptIds[index]);
            }
            ActivityReceiptIds = Array.AsReadOnly(receiptIds);

            var equipment = new PlayerGrowthEquipmentView[
                profile.ownedGrowthEquipment.Length];
            _equipmentRanks = new Dictionary<string, int>(equipment.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < equipment.Length; index++)
            {
                var source = profile.ownedGrowthEquipment[index];
                equipment[index] = new PlayerGrowthEquipmentView(
                    source.growthEquipmentId, source.rank);
                _equipmentRanks.Add(source.growthEquipmentId, source.rank);
            }
            OwnedGrowthEquipment = Array.AsReadOnly(equipment);

            var loadout = new PlayerGrowthLoadoutView[profile.growthLoadout.Length];
            _loadout = new Dictionary<string, string>(loadout.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < loadout.Length; index++)
            {
                var source = profile.growthLoadout[index];
                loadout[index] = new PlayerGrowthLoadoutView(source.slotId,
                    source.growthEquipmentId);
                _loadout.Add(source.slotId, source.growthEquipmentId);
            }
            GrowthLoadout = Array.AsReadOnly(loadout);

            var cultivation = new PlayerCultivationRankView[
                profile.cultivationRanks.Length];
            _cultivationRanks = new Dictionary<string, int>(cultivation.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < cultivation.Length; index++)
            {
                var source = profile.cultivationRanks[index];
                cultivation[index] = new PlayerCultivationRankView(
                    source.cultivationNodeId, source.rank);
                _cultivationRanks.Add(source.cultivationNodeId, source.rank);
            }
            CultivationRanks = Array.AsReadOnly(cultivation);
        }

        public string ProfileId { get; }
        public long Revision { get; }
        public string CreatedAtUtc { get; }
        public string UpdatedAtUtc { get; }
        public string Locale { get; }
        public float MusicVolume { get; }
        public float SoundVolume { get; }
        public bool VibrationEnabled { get; }
        public string LastSelectedLevelId { get; }
        public bool ShowBattleTips { get; }
        public bool ConfirmBeforeBattleRestart { get; }
        public ReadOnlyCollection<PlayerItemBalanceView> ItemBalances { get; }
        public ReadOnlyCollection<string> ActivityReceiptIds { get; }
        public ReadOnlyCollection<PlayerGrowthEquipmentView> OwnedGrowthEquipment { get; }
        public ReadOnlyCollection<PlayerGrowthLoadoutView> GrowthLoadout { get; }
        public ReadOnlyCollection<PlayerCultivationRankView> CultivationRanks { get; }

        public long ItemQuantity(string itemId)
        {
            return itemId != null && _itemQuantities.TryGetValue(itemId, out var value)
                ? value
                : 0;
        }

        public bool HasReceipt(string receiptId)
        {
            return receiptId != null && _receipts.Contains(receiptId);
        }

        public bool TryGetGrowthEquipmentRank(string growthEquipmentId,
            out int rank)
        {
            return _equipmentRanks.TryGetValue(growthEquipmentId ?? string.Empty,
                out rank);
        }

        public bool TryGetEquipped(string slotId, out string growthEquipmentId)
        {
            return _loadout.TryGetValue(slotId ?? string.Empty,
                out growthEquipmentId);
        }

        public int CultivationRank(string cultivationNodeId)
        {
            return cultivationNodeId != null
                && _cultivationRanks.TryGetValue(cultivationNodeId, out var rank)
                    ? rank
                    : 0;
        }

        public static PlayerProgressionProjection Create(PlayerProfile profile,
            CompiledOutgameContentCatalog content)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (content == null) throw new ArgumentNullException(nameof(content));
            var clone = PlayerProfileCodec.Clone(profile, content);
            return new PlayerProgressionProjection(clone);
        }
    }
}
