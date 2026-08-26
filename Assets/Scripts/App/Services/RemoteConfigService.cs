using System;
using System.Collections;
using FruitDefense.Content;
using UnityEngine;

namespace FruitDefense.App.Services
{
    [Serializable]
    public sealed class RuntimeConfigV1
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string configVersion = "config.bundled.v1";
        public string contentChannel = "bundled";
        public string bundledContentVersion = BattleContentSchema.BundledContentVersion;
        public bool cloudProfileEnabled;
        public bool remoteContentEnabled;
        public bool codeUpdateEnabled;

        public RuntimeConfigV1 Clone()
        {
            return JsonUtility.FromJson<RuntimeConfigV1>(JsonUtility.ToJson(this));
        }
    }

    public enum RemoteConfigLoadStatus
    {
        Success,
        InvalidBundledConfig,
    }

    public sealed class RemoteConfigLoadResult
    {
        public RemoteConfigLoadStatus Status { get; }
        public RuntimeConfigV1 Config { get; }
        public string Error { get; }

        public RemoteConfigLoadResult(RemoteConfigLoadStatus status, RuntimeConfigV1 config, string error = "")
        {
            Status = status;
            Config = config;
            Error = error ?? string.Empty;
        }
    }

    public interface IRemoteConfigService
    {
        IEnumerator Load(Action<RemoteConfigLoadResult> completed);
    }

    public sealed class BundledRemoteConfigService : IRemoteConfigService
    {
        private readonly RuntimeConfigV1 _config;

        public BundledRemoteConfigService(RuntimeConfigV1 config = null)
        {
            _config = config?.Clone() ?? new RuntimeConfigV1();
        }

        public IEnumerator Load(Action<RemoteConfigLoadResult> completed)
        {
            var gate = new CompletionGate<RemoteConfigLoadResult>(completed);
            yield return null;
            if (_config.schemaVersion != RuntimeConfigV1.CurrentSchemaVersion
                || string.IsNullOrWhiteSpace(_config.configVersion)
                || string.IsNullOrWhiteSpace(_config.contentChannel)
                || string.IsNullOrWhiteSpace(_config.bundledContentVersion))
            {
                gate.Complete(new RemoteConfigLoadResult(
                    RemoteConfigLoadStatus.InvalidBundledConfig,
                    null,
                    "Bundled runtime configuration is invalid."));
                yield break;
            }
            gate.Complete(new RemoteConfigLoadResult(RemoteConfigLoadStatus.Success, _config.Clone()));
        }
    }
}
