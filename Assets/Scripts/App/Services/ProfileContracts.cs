using System;
using System.Collections;
using UnityEngine;

namespace FruitDefense.App.Services
{
    [Serializable]
    public sealed class PlayerProfileEnvelopeV1
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string profileId;
        public long revision;
        public string createdAtUtc;
        public string updatedAtUtc;
        public string locale = "zh-CN";
        public float musicVolume = 1f;
        public float soundVolume = 1f;
        public bool vibrationEnabled = true;
        public string lastSelectedLevelId = "orchard-01";
        public bool showBattleTips = true;
        public bool confirmBeforeBattleRestart = true;

        public static PlayerProfileEnvelopeV1 CreateDefault()
        {
            var now = DateTimeOffset.UtcNow.ToString("o");
            return new PlayerProfileEnvelopeV1
            {
                profileId = Guid.NewGuid().ToString("N"),
                createdAtUtc = now,
                updatedAtUtc = now,
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
    }

    public readonly struct ProfileValidationResult
    {
        public bool Success => Code == ProfileValidationCode.Success;
        public ProfileValidationCode Code { get; }
        public string Message { get; }

        public ProfileValidationResult(ProfileValidationCode code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }
    }

    public static class PlayerProfileCodec
    {
        public static ProfileValidationResult Validate(PlayerProfileEnvelopeV1 profile)
        {
            if (profile == null)
                return Fail(ProfileValidationCode.InvalidJson, "Profile is null.");
            if (profile.schemaVersion != PlayerProfileEnvelopeV1.CurrentSchemaVersion)
                return Fail(ProfileValidationCode.UnsupportedSchema, $"Unsupported profile schema {profile.schemaVersion}.");
            if (!Guid.TryParse(profile.profileId, out _))
                return Fail(ProfileValidationCode.MissingProfileId, "Profile ID must be a GUID.");
            if (profile.revision < 0)
                return Fail(ProfileValidationCode.InvalidRevision, "Profile revision cannot be negative.");
            if (!DateTimeOffset.TryParse(profile.createdAtUtc, out _)
                || !DateTimeOffset.TryParse(profile.updatedAtUtc, out _))
                return Fail(ProfileValidationCode.InvalidTimestamp, "Profile timestamps must use an ISO-8601 value.");
            if (string.IsNullOrWhiteSpace(profile.locale))
                return Fail(ProfileValidationCode.InvalidLocale, "Profile locale is required.");
            if (!IsUnitFloat(profile.musicVolume) || !IsUnitFloat(profile.soundVolume))
                return Fail(ProfileValidationCode.InvalidVolume, "Profile volume values must be finite and between zero and one.");
            if (string.IsNullOrWhiteSpace(profile.lastSelectedLevelId))
                return Fail(ProfileValidationCode.MissingLevelId, "Last selected level is required.");
            return new ProfileValidationResult(ProfileValidationCode.Success, string.Empty);
        }

        public static ProfileValidationResult TryDeserialize(string json, out PlayerProfileEnvelopeV1 profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(json))
                return Fail(ProfileValidationCode.EmptyPayload, "Profile JSON is empty.");
            try
            {
                profile = JsonUtility.FromJson<PlayerProfileEnvelopeV1>(json);
            }
            catch (Exception exception)
            {
                return Fail(ProfileValidationCode.InvalidJson, exception.Message);
            }
            return Validate(profile);
        }

        public static string Serialize(PlayerProfileEnvelopeV1 profile)
        {
            var validation = Validate(profile);
            if (!validation.Success)
                throw new InvalidOperationException(validation.Message);
            return JsonUtility.ToJson(profile, true);
        }

        public static PlayerProfileEnvelopeV1 Clone(PlayerProfileEnvelopeV1 profile)
        {
            var json = Serialize(profile);
            var validation = TryDeserialize(json, out var clone);
            if (!validation.Success)
                throw new InvalidOperationException(validation.Message);
            return clone;
        }

        private static bool IsUnitFloat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }

        private static ProfileValidationResult Fail(ProfileValidationCode code, string message)
        {
            return new ProfileValidationResult(code, message);
        }
    }

    public enum ProfileLoadStatus
    {
        Success,
        DefaultCreated,
        RecoveredFromBackup,
        UnsupportedSchema,
        StorageError,
    }

    public sealed class ProfileLoadResult
    {
        public ProfileLoadStatus Status { get; }
        public PlayerProfileEnvelopeV1 Profile { get; }
        public string Error { get; }
        public bool HasProfile => Profile != null;

        public ProfileLoadResult(ProfileLoadStatus status, PlayerProfileEnvelopeV1 profile, string error = "")
        {
            Status = status;
            Profile = profile;
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
        public PlayerProfileEnvelopeV1 Profile { get; }
        public string Error { get; }

        public ProfileSaveResult(ProfileSaveStatus status, PlayerProfileEnvelopeV1 profile, string error = "")
        {
            Status = status;
            Profile = profile;
            Error = error ?? string.Empty;
        }
    }

    public interface IPlayerProfileStore
    {
        IEnumerator Load(Action<ProfileLoadResult> completed);
        IEnumerator Save(PlayerProfileEnvelopeV1 profile, Action<ProfileSaveResult> completed);
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
