using System;
using System.Collections;
using System.IO;
using System.Text;
using FruitDefense.App.Services;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class LocalServicePortsSmoke
    {
        public static void Run()
        {
            var temporaryRoot = Path.Combine(Path.GetTempPath(), "fruit-defense-profile-smoke-" + Guid.NewGuid().ToString("N"));
            try
            {
                ValidateFileStore(temporaryRoot);
                ValidateWebStore();
                ValidateSubstitutionAndBundledConfig();
                Debug.Log("FRUIT_DEFENSE_LOCAL_SERVICES_OK");
            }
            finally
            {
                if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
            }
        }

        private static void ValidateFileStore(string root)
        {
            var backend = new EditorFileProfileBackend(root);
            IPlayerProfileStore store = new LocalPlayerProfileStore(backend);

            var callbackCount = 0;
            ProfileLoadResult initial = null;
            Drain(store.Load(result =>
            {
                callbackCount++;
                initial = result;
            }));
            Assert(callbackCount == 1, "default load callback fires once");
            Assert(initial != null && initial.Status == ProfileLoadStatus.DefaultCreated && initial.HasProfile,
                "missing file creates a valid default profile");
            Assert(initial.Profile.lastSelectedLevelId == "orchard-01" && File.Exists(backend.PrimaryPath),
                "default profile persists to the primary path");

            initial.Profile.soundVolume = .6f;
            ProfileSaveResult firstSave = null;
            callbackCount = 0;
            Drain(store.Save(initial.Profile, result =>
            {
                callbackCount++;
                firstSave = result;
            }));
            Assert(callbackCount == 1 && firstSave.Status == ProfileSaveStatus.Success && firstSave.Profile.revision == 1,
                "first save increments revision and completes once");

            firstSave.Profile.musicVolume = .4f;
            ProfileSaveResult secondSave = null;
            Drain(store.Save(firstSave.Profile, result => secondSave = result));
            Assert(secondSave.Status == ProfileSaveStatus.Success && secondSave.Profile.revision == 2
                && File.Exists(backend.BackupPath), "second save retains the previous valid primary as backup");

            File.WriteAllText(backend.PrimaryPath, "{broken-json", new UTF8Encoding(false));
            ProfileLoadResult recovered = null;
            Drain(store.Load(result => recovered = result));
            Assert(recovered.Status == ProfileLoadStatus.RecoveredFromBackup && recovered.Profile.revision == 1,
                "corrupt primary is quarantined and the previous valid profile is recovered");
            Assert(Directory.GetFiles(root, "*.corrupt-*").Length == 1,
                "corrupt primary is retained for diagnostics");

            var unsupported = PlayerProfileCodec.Clone(recovered.Profile);
            unsupported.schemaVersion = 99;
            File.WriteAllText(backend.PrimaryPath, JsonUtility.ToJson(unsupported), new UTF8Encoding(false));
            ProfileLoadResult unsupportedResult = null;
            Drain(store.Load(result => unsupportedResult = result));
            Assert(unsupportedResult.Status == ProfileLoadStatus.UnsupportedSchema && !unsupportedResult.HasProfile,
                "unsupported schema is not interpreted as V1 or overwritten");
        }

        private static void ValidateWebStore()
        {
            var backend = new WebPlayerPrefsProfileBackend("fruit-defense.profile.smoke." + Guid.NewGuid().ToString("N"));
            try
            {
                IPlayerProfileStore store = new LocalPlayerProfileStore(backend);
                var callbackCount = 0;
                ProfileLoadResult loaded = null;
                Drain(store.Load(result =>
                {
                    callbackCount++;
                    loaded = result;
                }));
                Assert(callbackCount == 1 && loaded.Status == ProfileLoadStatus.DefaultCreated,
                    "Web profile default completes once");

                ProfileSaveResult saved = null;
                Drain(store.Save(loaded.Profile, result => saved = result));
                var raw = backend.ReadPrimary();
                Assert(saved.Status == ProfileSaveStatus.Success && raw.Found,
                    "Web profile promotes staging to primary");
                Assert(Encoding.UTF8.GetByteCount(raw.Json) < WebPlayerPrefsProfileBackend.MaximumProfileBytes,
                    "Web profile remains within the small-profile budget");
                Assert(raw.Json.IndexOf("BattleSnapshot", StringComparison.OrdinalIgnoreCase) < 0
                    && raw.Json.IndexOf("GameState", StringComparison.OrdinalIgnoreCase) < 0,
                    "profile payload excludes battle snapshot and active battle state");
            }
            finally
            {
                backend.ClearForTesting();
            }
        }

        private static void ValidateSubstitutionAndBundledConfig()
        {
            IPlayerProfileStore substitute = new FakeProfileStore(PlayerProfileEnvelopeV1.CreateDefault());
            ProfileLoadResult substituted = null;
            Drain(substitute.Load(result => substituted = result));
            Assert(substituted.Status == ProfileLoadStatus.Success && substituted.HasProfile,
                "callers can substitute another profile store through the same port");

            IRemoteConfigService service = new BundledRemoteConfigService();
            var callbackCount = 0;
            RemoteConfigLoadResult config = null;
            Drain(service.Load(result =>
            {
                callbackCount++;
                config = result;
            }));
            Assert(callbackCount == 1 && config.Status == RemoteConfigLoadStatus.Success,
                "bundled config completes once without network access");
            Assert(config.Config.contentChannel == "bundled" && config.Config.bundledContentVersion == "1.0.0"
                && !config.Config.cloudProfileEnabled && !config.Config.remoteContentEnabled,
                "P0 bundled config exposes offline defaults");
        }

        private static void Drain(IEnumerator operation)
        {
            while (operation.MoveNext()) { }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Local service smoke failed: " + message);
        }

        private sealed class FakeProfileStore : IPlayerProfileStore
        {
            private readonly PlayerProfileEnvelopeV1 _profile;

            public FakeProfileStore(PlayerProfileEnvelopeV1 profile)
            {
                _profile = profile;
            }

            public IEnumerator Load(Action<ProfileLoadResult> completed)
            {
                yield return null;
                completed?.Invoke(new ProfileLoadResult(ProfileLoadStatus.Success, PlayerProfileCodec.Clone(_profile)));
            }

            public IEnumerator Save(PlayerProfileEnvelopeV1 profile, Action<ProfileSaveResult> completed)
            {
                yield return null;
                completed?.Invoke(new ProfileSaveResult(ProfileSaveStatus.Success, PlayerProfileCodec.Clone(profile)));
            }
        }
    }
}
