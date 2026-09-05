using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FruitDefense.App.Services
{
    [Serializable]
    public sealed class PlayerItemBalance
    {
        public string itemId;
        public long quantity;
    }

    [Serializable]
    public sealed class PlayerActivityReceipt
    {
        public string receiptId;
    }

    [Serializable]
    public sealed class PlayerGrowthEquipment
    {
        public string growthEquipmentId;
        public int rank;
    }

    [Serializable]
    public sealed class PlayerGrowthLoadoutEntry
    {
        public string slotId;
        public string growthEquipmentId;
    }

    [Serializable]
    public sealed class PlayerCultivationRank
    {
        public string cultivationNodeId;
        public int rank;
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public const string CurrentSchemaId = "fruit-defense.player-profile";
        public const int CurrentSchemaVersion = 2;

        public string schemaId;
        public int schemaVersion;
        public string profileId;
        public long revision;
        public string createdAtUtc;
        public string updatedAtUtc;
        public string locale;
        public float musicVolume;
        public float soundVolume;
        public bool vibrationEnabled;
        public string lastSelectedLevelId;
        public bool showBattleTips;
        public bool confirmBeforeBattleRestart;
        public PlayerItemBalance[] itemBalances;
        public PlayerActivityReceipt[] activityReceipts;
        public PlayerGrowthEquipment[] ownedGrowthEquipment;
        public PlayerGrowthLoadoutEntry[] growthLoadout;
        public PlayerCultivationRank[] cultivationRanks;

        public static PlayerProfile CreateDefault()
        {
            var now = DateTimeOffset.UtcNow.ToString("o");
            return new PlayerProfile
            {
                schemaId = CurrentSchemaId,
                schemaVersion = CurrentSchemaVersion,
                profileId = Guid.NewGuid().ToString("N"),
                revision = 0,
                createdAtUtc = now,
                updatedAtUtc = now,
                locale = "zh-CN",
                musicVolume = 1f,
                soundVolume = 1f,
                vibrationEnabled = true,
                lastSelectedLevelId = "orchard-01",
                showBattleTips = true,
                confirmBeforeBattleRestart = true,
                itemBalances = Array.Empty<PlayerItemBalance>(),
                activityReceipts = Array.Empty<PlayerActivityReceipt>(),
                ownedGrowthEquipment = Array.Empty<PlayerGrowthEquipment>(),
                growthLoadout = Array.Empty<PlayerGrowthLoadoutEntry>(),
                cultivationRanks = Array.Empty<PlayerCultivationRank>(),
            };
        }
    }

    public enum ProfileValidationCode
    {
        Success,
        EmptyPayload,
        InvalidJson,
        UnsupportedSchema,
        MissingProfileId,
        InvalidRevision,
        InvalidTimestamp,
        InvalidLocale,
        InvalidVolume,
        MissingLevelId,
        MissingCollection,
        CollectionLimitExceeded,
        InvalidStableId,
        DuplicateItemBalance,
        InvalidItemQuantity,
        DuplicateActivityReceipt,
        DuplicateGrowthEquipment,
        InvalidGrowthEquipmentRank,
        DuplicateLoadoutSlot,
        DuplicateLoadoutEquipment,
        UnownedLoadoutEquipment,
        DuplicateCultivationNode,
        InvalidCultivationRank,
        MissingContent,
        UnknownItem,
        UnknownActivityReceipt,
        UnknownGrowthEquipment,
        InvalidGrowthEquipmentSlot,
        UnknownCultivationNode,
        InvalidCultivationPrerequisite,
        UnknownLevel,
    }

    public readonly struct ProfileValidationResult
    {
        public bool Success => Code == ProfileValidationCode.Success;
        public ProfileValidationCode Code { get; }
        public string Path { get; }
        public string Identity { get; }
        public string Message { get; }

        public ProfileValidationResult(ProfileValidationCode code, string path,
            string identity, string message)
        {
            Code = code;
            Path = path ?? string.Empty;
            Identity = identity ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }

    public static partial class PlayerProfileCodec
    {
        private const int MaximumCollectionEntries = 512;

        [Serializable]
        private sealed class ProfileHeader
        {
            public string schemaId;
            public int schemaVersion;
        }

        public static ProfileValidationResult Validate(PlayerProfile profile)
        {
            if (profile == null)
                return Fail(ProfileValidationCode.InvalidJson, string.Empty,
                    string.Empty, "Profile is null.");
            if (!string.Equals(profile.schemaId, PlayerProfile.CurrentSchemaId,
                    StringComparison.Ordinal)
                || profile.schemaVersion != PlayerProfile.CurrentSchemaVersion)
            {
                return Fail(ProfileValidationCode.UnsupportedSchema, "schemaId",
                    profile.schemaId,
                    "Stored profile schema is unsupported and requires explicit reset.");
            }
            if (!Guid.TryParse(profile.profileId, out _))
                return Fail(ProfileValidationCode.MissingProfileId, "profileId",
                    profile.profileId, "Profile ID must be a GUID.");
            if (profile.revision < 0 || profile.revision == long.MaxValue)
                return Fail(ProfileValidationCode.InvalidRevision, "revision",
                    profile.revision.ToString(),
                    "Profile revision must be non-negative and incrementable.");
            if (!DateTimeOffset.TryParse(profile.createdAtUtc, out var created)
                || !DateTimeOffset.TryParse(profile.updatedAtUtc, out var updated)
                || updated < created)
            {
                return Fail(ProfileValidationCode.InvalidTimestamp, "updatedAtUtc",
                    profile.updatedAtUtc,
                    "Profile timestamps must be ordered ISO-8601 values.");
            }
            if (string.IsNullOrWhiteSpace(profile.locale))
                return Fail(ProfileValidationCode.InvalidLocale, "locale",
                    profile.locale, "Profile locale is required.");
            if (!IsUnitFloat(profile.musicVolume) || !IsUnitFloat(profile.soundVolume))
                return Fail(ProfileValidationCode.InvalidVolume, "volume", string.Empty,
                    "Profile volume values must be finite and between zero and one.");
            if (string.IsNullOrWhiteSpace(profile.lastSelectedLevelId))
                return Fail(ProfileValidationCode.MissingLevelId, "lastSelectedLevelId",
                    profile.lastSelectedLevelId, "Last selected level is required.");

            var result = ValidateCollectionsPresent(profile);
            if (!result.Success) return result;
            result = ValidateItemBalances(profile.itemBalances);
            if (!result.Success) return result;
            result = ValidateReceipts(profile.activityReceipts);
            if (!result.Success) return result;
            result = ValidateEquipment(profile.ownedGrowthEquipment);
            if (!result.Success) return result;
            result = ValidateLoadout(profile.growthLoadout,
                profile.ownedGrowthEquipment);
            if (!result.Success) return result;
            return ValidateCultivation(profile.cultivationRanks);
        }

        public static ProfileValidationResult TryDeserialize(string json,
            out PlayerProfile profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(json))
                return Fail(ProfileValidationCode.EmptyPayload, string.Empty,
                    string.Empty, "Profile JSON is empty.");

            var trimmed = json.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '{'
                || trimmed[trimmed.Length - 1] != '}')
            {
                return Fail(ProfileValidationCode.InvalidJson, string.Empty,
                    string.Empty, "Profile JSON must contain one object.");
            }

            try
            {
                var header = JsonUtility.FromJson<ProfileHeader>(json);
                if (header == null
                    || !string.Equals(header.schemaId, PlayerProfile.CurrentSchemaId,
                        StringComparison.Ordinal)
                    || header.schemaVersion != PlayerProfile.CurrentSchemaVersion)
                {
                    return Fail(ProfileValidationCode.UnsupportedSchema, "schemaId",
                        header?.schemaId,
                        "Stored profile schema is unsupported and requires explicit reset.");
                }

                profile = JsonUtility.FromJson<PlayerProfile>(json);
            }
            catch (Exception exception)
            {
                profile = null;
                return Fail(ProfileValidationCode.InvalidJson, string.Empty,
                    string.Empty, exception.Message);
            }

            var validation = Validate(profile);
            if (!validation.Success)
            {
                profile = null;
                return validation;
            }

            Normalize(profile);
            return validation;
        }

        public static string Serialize(PlayerProfile profile)
        {
            var validation = Validate(profile);
            if (!validation.Success)
                throw new InvalidOperationException(validation.Message);
            var normalized = Copy(profile);
            Normalize(normalized);
            return JsonUtility.ToJson(normalized, true);
        }

        public static PlayerProfile Clone(PlayerProfile profile)
        {
            var json = Serialize(profile);
            var validation = TryDeserialize(json, out var clone);
            if (!validation.Success)
                throw new InvalidOperationException(validation.Message);
            return clone;
        }

        internal static void Normalize(PlayerProfile profile)
        {
            Array.Sort(profile.itemBalances, CompareItemBalance);
            Array.Sort(profile.activityReceipts, CompareReceipt);
            Array.Sort(profile.ownedGrowthEquipment, CompareEquipment);
            Array.Sort(profile.growthLoadout, CompareLoadout);
            Array.Sort(profile.cultivationRanks, CompareCultivation);
        }

        private static PlayerProfile Copy(PlayerProfile source)
        {
            var clone = new PlayerProfile
            {
                schemaId = source.schemaId,
                schemaVersion = source.schemaVersion,
                profileId = source.profileId,
                revision = source.revision,
                createdAtUtc = source.createdAtUtc,
                updatedAtUtc = source.updatedAtUtc,
                locale = source.locale,
                musicVolume = source.musicVolume,
                soundVolume = source.soundVolume,
                vibrationEnabled = source.vibrationEnabled,
                lastSelectedLevelId = source.lastSelectedLevelId,
                showBattleTips = source.showBattleTips,
                confirmBeforeBattleRestart = source.confirmBeforeBattleRestart,
                itemBalances = new PlayerItemBalance[source.itemBalances.Length],
                activityReceipts = new PlayerActivityReceipt[source.activityReceipts.Length],
                ownedGrowthEquipment = new PlayerGrowthEquipment[
                    source.ownedGrowthEquipment.Length],
                growthLoadout = new PlayerGrowthLoadoutEntry[source.growthLoadout.Length],
                cultivationRanks = new PlayerCultivationRank[
                    source.cultivationRanks.Length],
            };

            for (var index = 0; index < clone.itemBalances.Length; index++)
            {
                var entry = source.itemBalances[index];
                clone.itemBalances[index] = new PlayerItemBalance
                {
                    itemId = entry.itemId,
                    quantity = entry.quantity,
                };
            }
            for (var index = 0; index < clone.activityReceipts.Length; index++)
            {
                clone.activityReceipts[index] = new PlayerActivityReceipt
                {
                    receiptId = source.activityReceipts[index].receiptId,
                };
            }
            for (var index = 0; index < clone.ownedGrowthEquipment.Length; index++)
            {
                var entry = source.ownedGrowthEquipment[index];
                clone.ownedGrowthEquipment[index] = new PlayerGrowthEquipment
                {
                    growthEquipmentId = entry.growthEquipmentId,
                    rank = entry.rank,
                };
            }
            for (var index = 0; index < clone.growthLoadout.Length; index++)
            {
                var entry = source.growthLoadout[index];
                clone.growthLoadout[index] = new PlayerGrowthLoadoutEntry
                {
                    slotId = entry.slotId,
                    growthEquipmentId = entry.growthEquipmentId,
                };
            }
            for (var index = 0; index < clone.cultivationRanks.Length; index++)
            {
                var entry = source.cultivationRanks[index];
                clone.cultivationRanks[index] = new PlayerCultivationRank
                {
                    cultivationNodeId = entry.cultivationNodeId,
                    rank = entry.rank,
                };
            }
            return clone;
        }

        private static ProfileValidationResult ValidateCollectionsPresent(
            PlayerProfile profile)
        {
            if (profile.itemBalances == null) return MissingCollection("itemBalances");
            if (profile.activityReceipts == null) return MissingCollection("activityReceipts");
            if (profile.ownedGrowthEquipment == null)
                return MissingCollection("ownedGrowthEquipment");
            if (profile.growthLoadout == null) return MissingCollection("growthLoadout");
            if (profile.cultivationRanks == null) return MissingCollection("cultivationRanks");
            if (profile.itemBalances.Length > MaximumCollectionEntries
                || profile.activityReceipts.Length > MaximumCollectionEntries
                || profile.ownedGrowthEquipment.Length > MaximumCollectionEntries
                || profile.growthLoadout.Length > MaximumCollectionEntries
                || profile.cultivationRanks.Length > MaximumCollectionEntries)
            {
                return Fail(ProfileValidationCode.CollectionLimitExceeded,
                    "collections", string.Empty,
                    "A profile collection exceeds the finite entry limit.");
            }
            return Success();
        }

        private static ProfileValidationResult ValidateItemBalances(
            PlayerItemBalance[] entries)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var path = "itemBalances[" + index + "]";
                if (entry == null || !IsStableId(entry.itemId))
                    return InvalidStableId(path + ".itemId", entry?.itemId);
                if (!seen.Add(entry.itemId))
                    return Fail(ProfileValidationCode.DuplicateItemBalance, path,
                        entry.itemId, "Item balances must have unique identities.");
                if (entry.quantity < 0)
                    return Fail(ProfileValidationCode.InvalidItemQuantity,
                        path + ".quantity", entry.itemId,
                        "Item quantity cannot be negative.");
            }
            return Success();
        }

        private static ProfileValidationResult ValidateReceipts(
            PlayerActivityReceipt[] entries)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var path = "activityReceipts[" + index + "]";
                if (entry == null || !IsStableId(entry.receiptId))
                    return InvalidStableId(path + ".receiptId", entry?.receiptId);
                if (!seen.Add(entry.receiptId))
                    return Fail(ProfileValidationCode.DuplicateActivityReceipt, path,
                        entry.receiptId,
                        "Activity receipts must have unique identities.");
            }
            return Success();
        }

        private static ProfileValidationResult ValidateEquipment(
            PlayerGrowthEquipment[] entries)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var path = "ownedGrowthEquipment[" + index + "]";
                if (entry == null || !IsStableId(entry.growthEquipmentId))
                    return InvalidStableId(path + ".growthEquipmentId",
                        entry?.growthEquipmentId);
                if (!seen.Add(entry.growthEquipmentId))
                    return Fail(ProfileValidationCode.DuplicateGrowthEquipment, path,
                        entry.growthEquipmentId,
                        "Owned growth equipment must have unique identities.");
                if (entry.rank < 0)
                    return Fail(ProfileValidationCode.InvalidGrowthEquipmentRank,
                        path + ".rank", entry.growthEquipmentId,
                        "Growth-equipment rank cannot be negative.");
            }
            return Success();
        }

        private static ProfileValidationResult ValidateLoadout(
            PlayerGrowthLoadoutEntry[] entries, PlayerGrowthEquipment[] owned)
        {
            var ownedIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < owned.Length; index++)
                ownedIds.Add(owned[index].growthEquipmentId);

            var slots = new HashSet<string>(StringComparer.Ordinal);
            var equipped = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var path = "growthLoadout[" + index + "]";
                if (entry == null || !IsStableId(entry.slotId))
                    return InvalidStableId(path + ".slotId", entry?.slotId);
                if (!IsStableId(entry.growthEquipmentId))
                    return InvalidStableId(path + ".growthEquipmentId",
                        entry.growthEquipmentId);
                if (!slots.Add(entry.slotId))
                    return Fail(ProfileValidationCode.DuplicateLoadoutSlot, path,
                        entry.slotId, "Loadout slots must have unique identities.");
                if (!equipped.Add(entry.growthEquipmentId))
                    return Fail(ProfileValidationCode.DuplicateLoadoutEquipment, path,
                        entry.growthEquipmentId,
                        "One growth-equipment identity cannot occupy multiple slots.");
                if (!ownedIds.Contains(entry.growthEquipmentId))
                    return Fail(ProfileValidationCode.UnownedLoadoutEquipment, path,
                        entry.growthEquipmentId,
                        "Loadout equipment must be owned by the profile.");
            }
            return Success();
        }

        private static ProfileValidationResult ValidateCultivation(
            PlayerCultivationRank[] entries)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var path = "cultivationRanks[" + index + "]";
                if (entry == null || !IsStableId(entry.cultivationNodeId))
                    return InvalidStableId(path + ".cultivationNodeId",
                        entry?.cultivationNodeId);
                if (!seen.Add(entry.cultivationNodeId))
                    return Fail(ProfileValidationCode.DuplicateCultivationNode, path,
                        entry.cultivationNodeId,
                        "Cultivation nodes must have unique identities.");
                if (entry.rank <= 0)
                    return Fail(ProfileValidationCode.InvalidCultivationRank,
                        path + ".rank", entry.cultivationNodeId,
                        "Stored cultivation ranks must be positive; omit rank zero.");
            }
            return Success();
        }

        private static bool IsStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || character == '.' || character == '-')
                    continue;
                return false;
            }
            return true;
        }

        private static bool IsUnitFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value)
                && value >= 0f && value <= 1f;
        }

        private static int CompareItemBalance(PlayerItemBalance left,
            PlayerItemBalance right) => string.CompareOrdinal(left.itemId, right.itemId);
        private static int CompareReceipt(PlayerActivityReceipt left,
            PlayerActivityReceipt right) => string.CompareOrdinal(left.receiptId, right.receiptId);
        private static int CompareEquipment(PlayerGrowthEquipment left,
            PlayerGrowthEquipment right) => string.CompareOrdinal(left.growthEquipmentId,
            right.growthEquipmentId);
        private static int CompareLoadout(PlayerGrowthLoadoutEntry left,
            PlayerGrowthLoadoutEntry right) => string.CompareOrdinal(left.slotId, right.slotId);
        private static int CompareCultivation(PlayerCultivationRank left,
            PlayerCultivationRank right) => string.CompareOrdinal(left.cultivationNodeId,
            right.cultivationNodeId);

        private static ProfileValidationResult MissingCollection(string path)
        {
            return Fail(ProfileValidationCode.MissingCollection, path, string.Empty,
                "Profile collection is required and must use an explicit empty array.");
        }

        private static ProfileValidationResult InvalidStableId(string path,
            string identity)
        {
            return Fail(ProfileValidationCode.InvalidStableId, path, identity,
                "Progression identity must use lowercase stable-ID characters.");
        }

        private static ProfileValidationResult Success()
        {
            return new ProfileValidationResult(ProfileValidationCode.Success,
                string.Empty, string.Empty, string.Empty);
        }

        private static ProfileValidationResult Fail(ProfileValidationCode code,
            string path, string identity, string message)
        {
            return new ProfileValidationResult(code, path, identity, message);
        }
    }

    public enum ProfileLoadStatus
    {
        Success,
        DefaultCreated,
        RecoveredFromBackup,
        ResetCreated,
        UnsupportedSchema,
        StorageError,
    }

    public sealed class ProfileLoadResult
    {
        public ProfileLoadStatus Status { get; }
        public PlayerProfile Profile { get; }
        public ProfileValidationResult Validation { get; }
        public string Error { get; }
        public bool HasProfile => Profile != null;

        public ProfileLoadResult(ProfileLoadStatus status, PlayerProfile profile,
            string error = "", ProfileValidationResult validation = default)
        {
            Status = status;
            Profile = profile;
            Validation = validation;
            Error = error ?? string.Empty;
        }
    }

    public enum ProfileSaveStatus
    {
        Success,
        InvalidProfile,
        StorageError,
    }

    public sealed class ProfileSaveResult
    {
        public ProfileSaveStatus Status { get; }
        public PlayerProfile Profile { get; }
        public ProfileValidationResult Validation { get; }
        public string Error { get; }

        public ProfileSaveResult(ProfileSaveStatus status, PlayerProfile profile,
            string error = "", ProfileValidationResult validation = default)
        {
            Status = status;
            Profile = profile;
            Validation = validation;
            Error = error ?? string.Empty;
        }
    }

    public interface IPlayerProfileStore
    {
        IEnumerator Load(Action<ProfileLoadResult> completed);
        IEnumerator Save(PlayerProfile profile, Action<ProfileSaveResult> completed);
        IEnumerator Reset(Action<ProfileLoadResult> completed);
    }

    internal sealed class CompletionGate<T>
    {
        private readonly Action<T> _completed;
        private bool _didComplete;

        public CompletionGate(Action<T> completed)
        {
            _completed = completed;
        }

        public void Complete(T value)
        {
            if (_didComplete) return;
            _didComplete = true;
            _completed?.Invoke(value);
        }
    }
}
