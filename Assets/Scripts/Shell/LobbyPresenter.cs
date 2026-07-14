using System;
using FruitDefense.App;
using UnityEngine;

namespace FruitDefense.Shell
{
    [DisallowMultipleComponent]
    public sealed class LobbyPresenter : MonoBehaviour
    {
        public const string DefaultLevelId = "orchard-01";
        public const string MissingContext = "shell-context-missing";
        public const string MissingNavigator = "shell-navigator-missing";
        public const string MissingContentVersion = "bundled-content-version-missing";

        private IShellFlowContext _context;
        private ShellStyleSet _styles;
        private float _styleScale = -1f;

        public ShellFlowError LastError { get; private set; }
        public string LastSessionId { get; private set; } = string.Empty;
        public int LastSeed { get; private set; }
        public string LastContentVersion { get; private set; } = string.Empty;

        public void Initialize(IShellFlowContext context)
        {
            _context = context;
            LastError = ShellFlowError.None;
        }

        public bool TryStart()
        {
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;

            var contentVersion = _context.BundledContentVersion;
            if (string.IsNullOrWhiteSpace(contentVersion))
                return Fail(new ShellFlowError(MissingContentVersion));

            var sessionId = Guid.NewGuid().ToString("N");
            var seed = CreateNonzeroSeed();
            if (!_context.TryStartDefaultBattle(
                    DefaultLevelId,
                    sessionId,
                    seed,
                    contentVersion,
                    out var error))
                return Fail(error);

            LastSessionId = sessionId;
            LastSeed = seed;
            LastContentVersion = contentVersion;
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryActivateAt(Vector2 guiPoint, float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateLobby(viewportWidth, viewportHeight, safeArea);
            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            return PortraitShellLayout.HitTest(layout, guiPoint, transitioning) == ShellHitTarget.Start
                && TryStart();
        }

        private void OnGUI()
        {
            var layout = PortraitShellLayout.CreateLobby(Screen.width, Screen.height, Screen.safeArea);
            EnsureStyles(layout.Frame.Scale);

            ShellGui.DrawPanel(layout.Frame.SafeArea, _styles.Panel);
            GUI.Label(layout.Title, "果园防线", _styles.Title);

            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            var previousEnabled = GUI.enabled;
            GUI.enabled = !transitioning;
            if (GUI.Button(layout.StartButton, transitioning ? "正在进入…" : "开始战斗", _styles.PrimaryButton))
                TryStart();
            GUI.enabled = previousEnabled;

            ShellGui.DrawReservedCard(layout.LevelCard, "关卡选择 · 未开放", "P0 默认进入 orchard-01", _styles);
            ShellGui.DrawReservedCard(layout.GrowthCard, "成长 · 未开放", "成长系统将在后续迭代接入", _styles);
            ShellGui.DrawReservedCard(layout.SettingsCard, "设置 · 未开放", "设置入口已预留", _styles);

            var status = LastError.IsEmpty ? "" : "暂时无法开始：" + LastError.Code;
            GUI.Label(layout.Status, status, _styles.Status);
        }

        private void EnsureStyles(float scale)
        {
            if (_styles != null && Mathf.Approximately(_styleScale, scale)) return;
            _styles = ShellStyleSet.Create(GUI.skin, scale);
            _styleScale = scale;
        }

        private bool Fail(ShellFlowError error)
        {
            LastError = error.IsEmpty ? new ShellFlowError("shell-command-rejected") : error;
            return false;
        }

        internal static int CreateNonzeroSeed()
        {
            var bytes = Guid.NewGuid().ToByteArray();
            var seed = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            return seed == 0 ? 1 : seed;
        }
    }
}
