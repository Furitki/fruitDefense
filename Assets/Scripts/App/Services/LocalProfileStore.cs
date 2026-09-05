using System;
using System.Collections;
using System.IO;
using System.Text;
using FruitDefense.Content;
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
        bool TryReset(out string error);
        void QuarantinePrimary();
    }

    public sealed class EditorFileProfileBackend : IProfileStorageBackend
    {
        private readonly string _directory;
        public string PrimaryPath { get; }
        public string BackupPath { get; }

        public EditorFileProfileBackend(string directory,
            string fileName = "player-profile.json")
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

        public bool TryReset(out string error)
        {
            error = string.Empty;
            try
            {
                DeleteIfPresent(PrimaryPath);
                DeleteIfPresent(BackupPath);
                DeleteIfPresent(PrimaryPath + ".staging");
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
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

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public sealed class WebPlayerPrefsProfileBackend : IProfileStorageBackend
    {
        public const int MaximumProfileBytes = 64 * 1024;

        private readonly string _primaryKey;
        private readonly string _backupKey;
        private readonly string _stagingKey;
        private readonly string _corruptKey;

        public WebPlayerPrefsProfileBackend(
            string keyPrefix = "fruit-defense.player-profile")
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
            TryReset(out _);
        }

        public bool TryReset(out string error)
        {
            error = string.Empty;
            try
            {
                PlayerPrefs.DeleteKey(_primaryKey);
                PlayerPrefs.DeleteKey(_backupKey);
                PlayerPrefs.DeleteKey(_stagingKey);
                PlayerPrefs.DeleteKey(_corruptKey);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
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
        private readonly CompiledOutgameContentCatalog _content;

        public LocalPlayerProfileStore(IProfileStorageBackend backend,
            CompiledOutgameContentCatalog content)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _content = content ?? throw new ArgumentNullException(nameof(content));
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
                var validation = PlayerProfileCodec.TryDeserialize(primary.Json,
                    _content, out var profile);
                if (validation.Success)
                {
                    gate.Complete(new ProfileLoadResult(ProfileLoadStatus.Success, profile));
                    yield break;
                }
                if (validation.Code == ProfileValidationCode.UnsupportedSchema)
                {
                    gate.Complete(new ProfileLoadResult(
                        ProfileLoadStatus.UnsupportedSchema, null,
                        validation.Message, validation));
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
                var validation = PlayerProfileCodec.TryDeserialize(backup.Json,
                    _content, out var profile);
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
                    gate.Complete(new ProfileLoadResult(
                        ProfileLoadStatus.UnsupportedSchema, null,
                        validation.Message, validation));
                    yield break;
                }
            }

            var created = PlayerProfile.CreateDefault();
            var json = PlayerProfileCodec.Serialize(created, _content);
            if (!_backend.TryWriteAtomically(json, out var error))
            {
                gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError, created, error));
                yield break;
            }
            gate.Complete(new ProfileLoadResult(ProfileLoadStatus.DefaultCreated, created));
        }

        public IEnumerator Save(PlayerProfile profile,
            Action<ProfileSaveResult> completed)
        {
            var gate = new CompletionGate<ProfileSaveResult>(completed);
            yield return null;

            var validation = PlayerProfileCodec.Validate(profile, _content);
            if (!validation.Success)
            {
                gate.Complete(new ProfileSaveResult(ProfileSaveStatus.InvalidProfile,
                    null, validation.Message, validation));
                yield break;
            }

            var persisted = PlayerProfileCodec.Clone(profile, _content);
            persisted.revision++;
            persisted.updatedAtUtc = DateTimeOffset.UtcNow.ToString("o");
            var json = PlayerProfileCodec.Serialize(persisted, _content);
            if (!_backend.TryWriteAtomically(json, out var error))
            {
                gate.Complete(new ProfileSaveResult(ProfileSaveStatus.StorageError, null, error));
                yield break;
            }
            gate.Complete(new ProfileSaveResult(ProfileSaveStatus.Success, persisted));
        }

        public IEnumerator Reset(Action<ProfileLoadResult> completed)
        {
            var gate = new CompletionGate<ProfileLoadResult>(completed);
            yield return null;

            if (!_backend.TryReset(out var resetError))
            {
                gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError,
                    null, resetError));
                yield break;
            }

            var created = PlayerProfile.CreateDefault();
            var json = PlayerProfileCodec.Serialize(created, _content);
            if (!_backend.TryWriteAtomically(json, out var writeError))
            {
                gate.Complete(new ProfileLoadResult(ProfileLoadStatus.StorageError,
                    null, writeError));
                yield break;
            }
            gate.Complete(new ProfileLoadResult(ProfileLoadStatus.ResetCreated,
                created));
        }
    }

    public static class LocalProfileStoreFactory
    {
        public static IPlayerProfileStore CreateDefault(
            CompiledOutgameContentCatalog content)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new LocalPlayerProfileStore(new WebPlayerPrefsProfileBackend(),
                content);
#else
            return new LocalPlayerProfileStore(
                new EditorFileProfileBackend(Application.persistentDataPath), content);
#endif
        }
    }
}
