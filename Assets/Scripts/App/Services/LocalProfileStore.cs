using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace FruitDefense.App.Services
{
    public readonly struct ProfileStorageReadResult
    {
        public bool Found { get; }
        public string Json { get; }
        public string Error { get; }

        public ProfileStorageReadResult(bool found, string json, string error = "")
        {
            Found = found;
            Json = json;
            Error = error ?? string.Empty;
        }
    }

    public interface IProfileStorageBackend
    {
        ProfileStorageReadResult ReadPrimary();
        ProfileStorageReadResult ReadBackup();
        bool TryWriteAtomically(string json, out string error);
        void QuarantinePrimary();
    }

    public sealed class EditorFileProfileBackend : IProfileStorageBackend
    {
        private readonly string _directory;
        public string PrimaryPath { get; }
        public string BackupPath { get; }

        public EditorFileProfileBackend(string directory, string fileName = "profile-v1.json")
        {
            _directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
            PrimaryPath = Path.Combine(_directory, fileName);
            BackupPath = PrimaryPath + ".backup";
        }

        public ProfileStorageReadResult ReadPrimary() => Read(PrimaryPath);
        public ProfileStorageReadResult ReadBackup() => Read(BackupPath);

        public bool TryWriteAtomically(string json, out string error)
        {
            error = string.Empty;
            var temporaryPath = PrimaryPath + ".staging";
            try
            {
                Directory.CreateDirectory(_directory);
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                var bytes = new UTF8Encoding(false).GetBytes(json ?? string.Empty);
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(PrimaryPath))
                    File.Replace(temporaryPath, PrimaryPath, BackupPath, true);
                else
                    File.Move(temporaryPath, PrimaryPath);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch { /* Best-effort cleanup of our staging file. */ }
                return false;
            }
        }

        public void QuarantinePrimary()
        {
            if (!File.Exists(PrimaryPath)) return;
            Directory.CreateDirectory(_directory);
            var quarantine = PrimaryPath + ".corrupt-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            File.Move(PrimaryPath, quarantine);
        }

        private static ProfileStorageReadResult Read(string path)
        {
            if (!File.Exists(path)) return new ProfileStorageReadResult(false, string.Empty);
            try
            {
                return new ProfileStorageReadResult(true, File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception exception)
            {
                return new ProfileStorageReadResult(false, string.Empty, exception.Message);
            }
        }
    }

    public sealed class WebPlayerPrefsProfileBackend : IProfileStorageBackend
    {
        public const int MaximumProfileBytes = 64 * 1024;

        private readonly string _primaryKey;
        private readonly string _backupKey;
        private readonly string _stagingKey;
        private readonly string _corruptKey;

        public WebPlayerPrefsProfileBackend(string keyPrefix = "fruit-defense.profile.v1")
        {
            if (string.IsNullOrWhiteSpace(keyPrefix)) throw new ArgumentException("Key prefix is required.", nameof(keyPrefix));
            _primaryKey = keyPrefix + ".primary";
            _backupKey = keyPrefix + ".backup";
            _stagingKey = keyPrefix + ".staging";
            _corruptKey = keyPrefix + ".corrupt";
        }

        public ProfileStorageReadResult ReadPrimary() => Read(_primaryKey);
        public ProfileStorageReadResult ReadBackup() => Read(_backupKey);

        public bool TryWriteAtomically(string json, out string error)
        {
            error = string.Empty;
            var byteCount = Encoding.UTF8.GetByteCount(json ?? string.Empty);
            if (byteCount > MaximumProfileBytes)
            {
                error = $"Profile payload is {byteCount} bytes; maximum is {MaximumProfileBytes}.";
                return false;
            }

            try
            {
                PlayerPrefs.SetString(_stagingKey, json ?? string.Empty);
                PlayerPrefs.Save();
                if (PlayerPrefs.HasKey(_primaryKey))
                    PlayerPrefs.SetString(_backupKey, PlayerPrefs.GetString(_primaryKey));
                PlayerPrefs.SetString(_primaryKey, PlayerPrefs.GetString(_stagingKey));
                PlayerPrefs.DeleteKey(_stagingKey);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public void QuarantinePrimary()
        {
            if (!PlayerPrefs.HasKey(_primaryKey)) return;
            PlayerPrefs.SetString(_corruptKey, PlayerPrefs.GetString(_primaryKey));
            PlayerPrefs.DeleteKey(_primaryKey);
            PlayerPrefs.Save();
        }

        public void ClearForTesting()
        {
            PlayerPrefs.DeleteKey(_primaryKey);
            PlayerPrefs.DeleteKey(_backupKey);
            PlayerPrefs.DeleteKey(_stagingKey);
            PlayerPrefs.DeleteKey(_corruptKey);
            PlayerPrefs.Save();
        }

        private static ProfileStorageReadResult Read(string key)
        {
            try
            {
                return PlayerPrefs.HasKey(key)
                    ? new ProfileStorageReadResult(true, PlayerPrefs.GetString(key))
                    : new ProfileStorageReadResult(false, string.Empty);
            }
            catch (Exception exception)
            {
                return new ProfileStorageReadResult(false, string.Empty, exception.Message);
            }
        }
    }

    public sealed class LocalPlayerProfileStore : IPlayerProfileStore
    {
        private readonly IProfileStorageBackend _backend;

        public LocalPlayerProfileStore(IProfileStorageBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public IEnumerator Load(Action<ProfileLoadResult> completed)
        {
            var gate = new CompletionGate<ProfileLoadResult>(completed);
            yield return null;

            var primary = _backend.ReadPrimary();
            if (!string.IsNullOrWhiteSpace(primary.Error))
            {
                gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError, null, primary.Error));
                yield break;
            }

            if (primary.Found)
            {
                var validation = PlayerProfileCodec.TryDeserialize(primary.Json, out var profile);
                if (validation.Success)
                {
                    gate.Complete(new ProfileLoadResult(ProfileLoadStatus.Success, profile));
                    yield break;
                }
                if (validation.Code == ProfileValidationCode.UnsupportedSchema)
                {
                    gate.Complete(new ProfileLoadResult(ProfileLoadStatus.UnsupportedSchema, null, validation.Message));
                    yield break;
                }
                try { _backend.QuarantinePrimary(); }
                catch (Exception exception)
                {
                    gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError, null, exception.Message));
                    yield break;
                }
            }

            var backup = _backend.ReadBackup();
            if (!string.IsNullOrWhiteSpace(backup.Error))
            {
                gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError, null, backup.Error));
                yield break;
            }
            if (backup.Found)
            {
                var validation = PlayerProfileCodec.TryDeserialize(backup.Json, out var profile);
                if (validation.Success)
                {
                    if (!_backend.TryWriteAtomically(backup.Json, out var recoveryError))
                    {
                        gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError, profile, recoveryError));
                        yield break;
                    }
                    gate.Complete(new ProfileLoadResult(ProfileLoadStatus.RecoveredFromBackup, profile));
                    yield break;
                }
                if (validation.Code == ProfileValidationCode.UnsupportedSchema)
                {
                    gate.Complete(new ProfileLoadResult(ProfileLoadStatus.UnsupportedSchema, null, validation.Message));
                    yield break;
                }
            }

            var created = PlayerProfileEnvelopeV1.CreateDefault();
            var json = PlayerProfileCodec.Serialize(created);
            if (!_backend.TryWriteAtomically(json, out var error))
            {
                gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError, created, error));
                yield break;
            }
            gate.Complete(new ProfileLoadResult(ProfileLoadStatus.DefaultCreated, created));
        }

        public IEnumerator Save(PlayerProfileEnvelopeV1 profile, Action<ProfileSaveResult> completed)
        {
            var gate = new CompletionGate<ProfileSaveResult>(completed);
            yield return null;

            var validation = PlayerProfileCodec.Validate(profile);
            if (!validation.Success)
            {
                gate.Complete(new ProfileSaveResult(ProfileSaveStatus.InvalidProfile, null, validation.Message));
                yield break;
            }

            var persisted = PlayerProfileCodec.Clone(profile);
            persisted.revision++;
            persisted.updatedAtUtc = DateTimeOffset.UtcNow.ToString("o");
            var json = PlayerProfileCodec.Serialize(persisted);
            if (!_backend.TryWriteAtomically(json, out var error))
            {
                gate.Complete(new ProfileSaveResult(ProfileSaveStatus.StorageError, null, error));
                yield break;
            }
            gate.Complete(new ProfileSaveResult(ProfileSaveStatus.Success, persisted));
        }
    }

    public static class LocalProfileStoreFactory
    {
        public static IPlayerProfileStore CreateDefault()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new LocalPlayerProfileStore(new WebPlayerPrefsProfileBackend());
#else
            return new LocalPlayerProfileStore(new EditorFileProfileBackend(Application.persistentDataPath));
#endif
        }
    }
}
