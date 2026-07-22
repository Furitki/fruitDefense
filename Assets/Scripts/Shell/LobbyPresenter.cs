using System;
using FruitDefense.App;
using UnityEngine;

namespace FruitDefense.Shell
{
    [DisallowMultipleComponent]
    public sealed class LobbyPresenter : MonoBehaviour
    {
        public const string Orchard01LevelId = "orchard-01";
        public const string Orchard02LevelId = "orchard-02";
        public const string Orchard03LevelId = "orchard-03";
        public const string DefaultLevelId = Orchard01LevelId;
        public const string MissingContext = "shell-context-missing";
        public const string MissingNavigator = "shell-navigator-missing";
        public const string MissingContentVersion = "bundled-content-version-missing";
        public const string MissingSelectedLevel = "lobby-selected-level-missing";
        public const string LevelSelectionUnavailable = "lobby-level-selection-unavailable";
        public const string LevelSelectionMismatch = "lobby-level-selection-mismatch";

        private IShellFlowContext _context;
        private string _visibleSelectedLevelId = DefaultLevelId;
        private ShellStyleSet _styles;
        private Font _font;
        private float _styleScale = -1f;

        public ShellFlowError LastError { get; private set; }
        public string LastSessionId { get; private set; } = string.Empty;
        public int LastSeed { get; private set; }
        public string LastContentVersion { get; private set; } = string.Empty;
        public string SelectedLevelId => _visibleSelectedLevelId;

        public void Initialize(IShellFlowContext context)
        {
            _context = context;
            _visibleSelectedLevelId = context is ILevelSelectionFlowContext selection
                ? selection.SelectedLevelId ?? string.Empty
                : DefaultLevelId;
            LastError = ShellFlowError.None;
        }

        public bool TrySelectLevel(string levelId)
        {
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;

            if (!(_context is ILevelSelectionFlowContext selection))
            {
                if (!string.Equals(levelId, DefaultLevelId, StringComparison.Ordinal))
                    return Fail(new ShellFlowError(LevelSelectionUnavailable, levelId));
                _visibleSelectedLevelId = DefaultLevelId;
                LastError = ShellFlowError.None;
                return true;
            }

            if (!selection.TrySelectLevel(levelId, out var error))
                return Fail(error);
            if (!string.Equals(selection.SelectedLevelId, levelId, StringComparison.Ordinal))
                return Fail(new ShellFlowError(LevelSelectionMismatch,
                    levelId + ":" + (selection.SelectedLevelId ?? string.Empty)));

            _visibleSelectedLevelId = levelId;
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryStart()
        {
            if (_context == null)
                return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null)
                return Fail(new ShellFlowError(MissingNavigator));
            if (_context.Navigator.TransitionState != AppTransitionState.Idle)
                return false;

            var selectedLevelId = _visibleSelectedLevelId;
            if (string.IsNullOrWhiteSpace(selectedLevelId))
                return Fail(new ShellFlowError(MissingSelectedLevel));
            if (_context is ILevelSelectionFlowContext selection
                && !IsPlayable(selection, selectedLevelId))
                return Fail(new ShellFlowError(MissingSelectedLevel, selectedLevelId));

            var contentVersion = _context.BundledContentVersion;
            if (string.IsNullOrWhiteSpace(contentVersion))
                return Fail(new ShellFlowError(MissingContentVersion));

            var sessionId = Guid.NewGuid().ToString("N");
            var seed = CreateNonzeroSeed();
            if (!_context.TryStartDefaultBattle(
                    selectedLevelId,
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
            switch (PortraitShellLayout.HitTest(layout, guiPoint, transitioning))
            {
                case ShellHitTarget.LevelOrchard01: return TrySelectLevel(Orchard01LevelId);
                case ShellHitTarget.LevelOrchard02: return TrySelectLevel(Orchard02LevelId);
                case ShellHitTarget.LevelOrchard03: return TrySelectLevel(Orchard03LevelId);
                case ShellHitTarget.Start: return TryStart();
                default: return false;
            }
        }

        private void OnGUI()
        {
            var layout = PortraitShellLayout.CreateLobby(
                Screen.width, Screen.height, RuntimeSafeAreaResolver.ResolveCurrent());
            EnsureStyles(layout.Frame.Scale);

            ShellGui.DrawPanel(layout.Frame.SafeArea, _styles.Panel);
            GUI.Label(layout.Title, "果园防线", _styles.Title);

            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            DrawLevelCard(layout.Orchard01Card, Orchard01LevelId,
                "第一关 · U形教学", "路线宽松｜熟悉种植与合成", transitioning);
            DrawLevelCard(layout.Orchard02Card, Orchard02LevelId,
                "第二关 · S形覆盖", "连续转弯｜兼顾快速与护甲敌人", transitioning);
            DrawLevelCard(layout.Orchard03Card, Orchard03LevelId,
                "第三关 · 核心走廊", "短线压迫｜守住首领冲击", transitioning);

            var previousEnabled = GUI.enabled;
            GUI.enabled = !transitioning && !string.IsNullOrWhiteSpace(_visibleSelectedLevelId);
            if (GUI.Button(layout.StartButton,
                    transitioning ? "正在进入…" : "开始战斗 · " + _visibleSelectedLevelId,
                    _styles.PrimaryButton))
                TryStart();
            GUI.enabled = previousEnabled;

            var status = LastError.IsEmpty ? string.Empty : "暂时无法继续：" + LastError.Code;
            GUI.Label(layout.Status, status, _styles.Status);
        }

        private void DrawLevelCard(Rect rect, string levelId, string title, string focus,
            bool transitioning)
        {
            var selected = string.Equals(_visibleSelectedLevelId, levelId, StringComparison.Ordinal);
            var available = !(_context is ILevelSelectionFlowContext selection)
                || IsPlayable(selection, levelId);
            var previousEnabled = GUI.enabled;
            GUI.enabled = !transitioning && available;
            var prefix = selected ? "✓ 已选择  " : string.Empty;
            if (GUI.Button(rect, prefix + title + "\n" + focus,
                    selected ? _styles.PrimaryButton : _styles.SecondaryButton))
                TrySelectLevel(levelId);
            GUI.enabled = previousEnabled;
        }

        private static bool IsPlayable(ILevelSelectionFlowContext selection, string levelId)
        {
            if (selection?.PlayableLevels == null || string.IsNullOrEmpty(levelId)) return false;
            for (var i = 0; i < selection.PlayableLevels.Count; i++)
            {
                var level = selection.PlayableLevels[i];
                if (level != null && string.Equals(level.LevelId, levelId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void EnsureStyles(float scale)
        {
            if (_styles != null && Mathf.Approximately(_styleScale, scale)) return;
            if (_font == null) _font = Resources.Load<Font>("Fonts/NotoSansSC-UI");
            _styles = ShellStyleSet.Create(GUI.skin, scale, _font);
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
