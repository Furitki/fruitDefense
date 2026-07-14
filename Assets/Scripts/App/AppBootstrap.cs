using System;
using System.Collections;
using FruitDefense.Platform;
using UnityEngine;

namespace FruitDefense.App
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        public const string AdapterCreationFailed = "platform-adapter-creation-failed";
        public const string AdapterInitializationThrew = "platform-initialization-threw";
        public const string AdapterCompletionMissing = "platform-completion-missing";

        public static AppBootstrap Instance { get; private set; }

        public IAppNavigator Navigator { get; private set; }
        public IPlatformAdapter PlatformAdapter { get; private set; }
        public PlatformInitResult InitializationResult { get; private set; }
        public PlatformVisibility CurrentVisibility { get; private set; } = PlatformVisibility.Foreground;
        public bool IsInitialized { get; private set; }
        public bool IsReady => IsInitialized && InitializationResult.Success;

        public event Action<PlatformVisibility> VisibilityChanged;
        public event Action<PlatformInitResult> InitializationCompleted;

        private void Awake()
        {
            if (ShouldRejectDuplicate(Instance, this))
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Navigator = new AppNavigator();

            try
            {
                PlatformAdapter = PlatformAdapterFactory.CreateCurrent();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FinishInitialization(PlatformInitResult.Failed(AdapterCreationFailed));
                return;
            }

            PlatformAdapter.VisibilityChanged += OnPlatformVisibilityChanged;
            StartCoroutine(InitializePlatform());
        }

        private IEnumerator InitializePlatform()
        {
            var completionCount = 0;
            var completionResult = PlatformInitResult.Failed(AdapterCompletionMissing);
            IEnumerator initialization;

            try
            {
                initialization = PlatformAdapter.Initialize(result =>
                {
                    completionCount++;
                    if (completionCount == 1) completionResult = result;
                });
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FinishInitialization(PlatformInitResult.Failed(AdapterInitializationThrew));
                yield break;
            }

            if (initialization == null)
            {
                FinishInitialization(PlatformInitResult.Failed(AdapterCompletionMissing));
                yield break;
            }

            while (true)
            {
                bool moved;
                object current = null;
                try
                {
                    moved = initialization.MoveNext();
                    if (moved) current = initialization.Current;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    FinishInitialization(PlatformInitResult.Failed(AdapterInitializationThrew));
                    yield break;
                }

                if (!moved) break;
                yield return current;
            }

            if (completionCount != 1)
            {
                Debug.LogError($"Platform adapter completion callback count was {completionCount}; expected exactly one.");
                FinishInitialization(PlatformInitResult.Failed(AdapterCompletionMissing));
                yield break;
            }

            FinishInitialization(completionResult);
        }

        private void OnApplicationPause(bool paused)
        {
            ForwardVisibility(paused ? PlatformVisibility.Background : PlatformVisibility.Foreground);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            ForwardVisibility(hasFocus ? PlatformVisibility.Foreground : PlatformVisibility.Background);
        }

        private void ForwardVisibility(PlatformVisibility visibility)
        {
            if (PlatformAdapter == null) return;
            PlatformAdapterFactory.TryForwardVisibility(PlatformAdapter, visibility);
        }

        private void OnPlatformVisibilityChanged(PlatformVisibility visibility)
        {
            if (CurrentVisibility == visibility) return;
            CurrentVisibility = visibility;
            VisibilityChanged?.Invoke(visibility);
        }

        private void FinishInitialization(PlatformInitResult result)
        {
            if (IsInitialized) return;
            InitializationResult = result;
            IsInitialized = true;
            InitializationCompleted?.Invoke(result);
        }

        private void OnDestroy()
        {
            if (PlatformAdapter != null)
            {
                PlatformAdapter.VisibilityChanged -= OnPlatformVisibilityChanged;
                PlatformAdapter.Dispose();
                PlatformAdapter = null;
            }

            if (Instance == this) Instance = null;
        }

        internal static bool ShouldRejectDuplicate(AppBootstrap existing, AppBootstrap candidate)
        {
            return existing != null && existing != candidate;
        }
    }
}
