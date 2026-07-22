using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FruitDefense.Platform
{
    public enum PlatformId
    {
        Editor,
        WindowsPreview,
        Web,
        DouyinMiniGame,
        WeChatMiniGame,
    }

    public enum PlatformVisibility
    {
        Foreground,
        Background,
    }

    public sealed class PlatformLaunchContext
    {
        private readonly ReadOnlyDictionary<string, string> _query;

        public PlatformLaunchContext(
            PlatformId platform,
            string launchUrl,
            IDictionary<string, string> query = null)
        {
            Platform = platform;
            LaunchUrl = launchUrl ?? string.Empty;
            _query = new ReadOnlyDictionary<string, string>(
                query == null
                    ? new Dictionary<string, string>(StringComparer.Ordinal)
                    : new Dictionary<string, string>(query, StringComparer.Ordinal));
        }

        public PlatformId Platform { get; }
        public string LaunchUrl { get; }
        public IReadOnlyDictionary<string, string> Query => _query;

        public bool TryGetQuery(string key, out string value)
        {
            if (key == null)
            {
                value = null;
                return false;
            }

            return _query.TryGetValue(key, out value);
        }

        public static PlatformLaunchContext Empty(PlatformId platform)
        {
            return new PlatformLaunchContext(platform, string.Empty);
        }

        public static PlatformLaunchContext FromUrl(PlatformId platform, string launchUrl)
        {
            return new PlatformLaunchContext(platform, launchUrl, ParseQuery(launchUrl));
        }

        internal static IDictionary<string, string> ParseQuery(string launchUrl)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(launchUrl)) return values;

            var questionMark = launchUrl.IndexOf('?');
            if (questionMark < 0 || questionMark == launchUrl.Length - 1) return values;

            var queryEnd = launchUrl.IndexOf('#', questionMark + 1);
            var query = queryEnd < 0
                ? launchUrl.Substring(questionMark + 1)
                : launchUrl.Substring(questionMark + 1, queryEnd - questionMark - 1);
            foreach (var pair in query.Split('&'))
            {
                if (string.IsNullOrEmpty(pair)) continue;
                var separator = pair.IndexOf('=');
                var rawKey = separator < 0 ? pair : pair.Substring(0, separator);
                var rawValue = separator < 0 ? string.Empty : pair.Substring(separator + 1);
                var key = DecodeQueryPart(rawKey);
                if (string.IsNullOrEmpty(key)) continue;
                values[key] = DecodeQueryPart(rawValue);
            }

            return values;
        }

        private static string DecodeQueryPart(string value)
        {
            var formValue = (value ?? string.Empty).Replace('+', ' ');
            try
            {
                return Uri.UnescapeDataString(formValue);
            }
            catch (UriFormatException)
            {
                return formValue;
            }
        }
    }

    public readonly struct PlatformInitResult
    {
        public PlatformInitResult(bool success, bool degraded, string errorCode)
        {
            Success = success;
            Degraded = success && degraded;
            ErrorCode = success ? string.Empty : NormalizeError(errorCode);
        }

        public bool Success { get; }
        public bool Degraded { get; }
        public string ErrorCode { get; }

        public static PlatformInitResult Succeeded(bool degraded = false)
        {
            return new PlatformInitResult(true, degraded, string.Empty);
        }

        public static PlatformInitResult Failed(string errorCode)
        {
            return new PlatformInitResult(false, false, errorCode);
        }

        private static string NormalizeError(string errorCode)
        {
            return string.IsNullOrWhiteSpace(errorCode) ? "platform-initialization-failed" : errorCode;
        }
    }

    public interface IPlatformAdapter : IDisposable
    {
        PlatformId Id { get; }
        PlatformLaunchContext LaunchContext { get; }
        event Action<PlatformVisibility> VisibilityChanged;
        IEnumerator Initialize(Action<PlatformInitResult> completed);
    }

    public interface IPlatformVisibilityReceiver
    {
        void ReceiveVisibility(PlatformVisibility visibility);
    }

    public abstract class PlatformAdapterBase : IPlatformAdapter, IPlatformVisibilityReceiver
    {
        private PlatformVisibility _visibility = PlatformVisibility.Foreground;

        protected PlatformAdapterBase(PlatformId id, PlatformLaunchContext launchContext)
        {
            Id = id;
            LaunchContext = launchContext ?? PlatformLaunchContext.Empty(id);
        }

        public PlatformId Id { get; }
        public PlatformLaunchContext LaunchContext { get; }
        public event Action<PlatformVisibility> VisibilityChanged;

        public abstract IEnumerator Initialize(Action<PlatformInitResult> completed);

        public void ReceiveVisibility(PlatformVisibility visibility)
        {
            if (_visibility == visibility) return;
            _visibility = visibility;
            VisibilityChanged?.Invoke(visibility);
        }

        public virtual void Dispose()
        {
            VisibilityChanged = null;
        }

        protected static void RequireCompletion(Action<PlatformInitResult> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
        }
    }

    public sealed class EditorPlatformAdapter : PlatformAdapterBase
    {
        public EditorPlatformAdapter()
            : base(PlatformId.Editor, PlatformLaunchContext.Empty(PlatformId.Editor))
        {
        }

        public override IEnumerator Initialize(Action<PlatformInitResult> completed)
        {
            RequireCompletion(completed);
            completed(PlatformInitResult.Succeeded());
            yield break;
        }
    }

    public sealed class WindowsPreviewPlatformAdapter : PlatformAdapterBase
    {
        public WindowsPreviewPlatformAdapter()
            : base(PlatformId.WindowsPreview, PlatformLaunchContext.Empty(PlatformId.WindowsPreview))
        {
        }

        public override IEnumerator Initialize(Action<PlatformInitResult> completed)
        {
            RequireCompletion(completed);
            completed(PlatformInitResult.Succeeded());
            yield break;
        }
    }

    public sealed class WebPlatformAdapter : PlatformAdapterBase
    {
        public WebPlatformAdapter(string launchUrl)
            : base(PlatformId.Web, PlatformLaunchContext.FromUrl(PlatformId.Web, launchUrl))
        {
        }

        public override IEnumerator Initialize(Action<PlatformInitResult> completed)
        {
            RequireCompletion(completed);
            completed(PlatformInitResult.Succeeded());
            yield break;
        }
    }

    public sealed class UnavailablePlatformAdapter : PlatformAdapterBase
    {
        public const string AdapterNotInstalled = "adapter-not-installed";

        public UnavailablePlatformAdapter(PlatformId id, string errorCode = AdapterNotInstalled)
            : base(id, PlatformLaunchContext.Empty(id))
        {
            if (id != PlatformId.DouyinMiniGame && id != PlatformId.WeChatMiniGame)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Only mini-game adapter slots can be unavailable.");
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? AdapterNotInstalled : errorCode;
        }

        public string ErrorCode { get; }

        public override IEnumerator Initialize(Action<PlatformInitResult> completed)
        {
            RequireCompletion(completed);
            completed(PlatformInitResult.Failed(ErrorCode));
            yield break;
        }
    }

    public static class PlatformAdapterFactory
    {
        public static IPlatformAdapter CreateCurrent()
        {
#if FRUIT_DEFENSE_DOUYIN
            return Create(PlatformId.DouyinMiniGame);
#elif FRUIT_DEFENSE_WECHAT
            return Create(PlatformId.WeChatMiniGame);
#elif UNITY_EDITOR
            return Create(PlatformId.Editor);
#elif UNITY_WEBGL
            return Create(PlatformId.Web, UnityEngine.Application.absoluteURL);
#elif UNITY_STANDALONE_WIN
            return Create(PlatformId.WindowsPreview);
#else
            throw new PlatformNotSupportedException(
                "FruitDefense currently supports Editor, WebGL, and Windows preview hosts only. Mini-game builds require an explicit adapter symbol.");
#endif
        }

        public static IPlatformAdapter Create(PlatformId platform, string launchUrl = null)
        {
            switch (platform)
            {
                case PlatformId.Editor:
                    return new EditorPlatformAdapter();
                case PlatformId.WindowsPreview:
                    return new WindowsPreviewPlatformAdapter();
                case PlatformId.Web:
                    return new WebPlatformAdapter(launchUrl);
                case PlatformId.DouyinMiniGame:
                case PlatformId.WeChatMiniGame:
                    return new UnavailablePlatformAdapter(platform);
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }

        public static bool TryForwardVisibility(IPlatformAdapter adapter, PlatformVisibility visibility)
        {
            if (!(adapter is IPlatformVisibilityReceiver receiver)) return false;
            receiver.ReceiveVisibility(visibility);
            return true;
        }
    }
}
