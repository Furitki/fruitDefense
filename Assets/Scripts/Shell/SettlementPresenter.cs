using FruitDefense.App;
using UnityEngine;

namespace FruitDefense.Shell
{
    [DisallowMultipleComponent]
    public sealed class SettlementPresenter : MonoBehaviour
    {
        public const string MissingContext = "shell-context-missing";
        public const string MissingNavigator = "shell-navigator-missing";
        public const string MissingResult = "settlement-result-missing";
        public const string InvalidResult = "settlement-result-invalid";

        private IShellFlowContext _context;
        private ShellStyleSet _styles;
        private Font _font;
        private float _styleScale = -1f;
        private bool _recoveryAttempted;

        public SettlementViewData ViewData { get; private set; }
        public bool HasViewData { get; private set; }
        public ShellFlowError LastError { get; private set; }

        public void Initialize(IShellFlowContext context)
        {
            _context = context;
            ViewData = default;
            HasViewData = false;
            LastError = ShellFlowError.None;
            _recoveryAttempted = false;
            BindResultOrRecover();
        }

        public bool TryReturn()
        {
            if (!CanSendCommand()) return false;
            if (!_context.TryReturnToLobby(out var error)) return Fail(error);
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryRetry()
        {
            if (!HasViewData || !CanSendCommand()) return false;
            if (!_context.TryRetryBattle(out var error)) return Fail(error);
            LastError = ShellFlowError.None;
            return true;
        }

        public bool TryActivateAt(Vector2 guiPoint, float viewportWidth, float viewportHeight, Rect safeArea)
        {
            var layout = PortraitShellLayout.CreateSettlement(viewportWidth, viewportHeight, safeArea);
            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            switch (PortraitShellLayout.HitTest(layout, guiPoint, transitioning))
            {
                case ShellHitTarget.Retry: return TryRetry();
                case ShellHitTarget.Return: return TryReturn();
                default: return false;
            }
        }

        private void BindResultOrRecover()
        {
            if (_context == null)
            {
                Fail(new ShellFlowError(MissingContext));
                return;
            }

            if (_context.Navigator == null)
            {
                Recover(new ShellFlowError(MissingNavigator));
                return;
            }

            if (!_context.TryGetSettlementViewData(out var viewData, out var error))
            {
                Recover(error.IsEmpty ? new ShellFlowError(MissingResult) : error);
                return;
            }

            if (viewData.ReachedWave < 0 || viewData.RemainingLives < 0)
            {
                Recover(new ShellFlowError(InvalidResult));
                return;
            }

            ViewData = viewData;
            HasViewData = true;
            LastError = ShellFlowError.None;
        }

        private void Recover(ShellFlowError error)
        {
            if (_recoveryAttempted) return;
            _recoveryAttempted = true;
            Fail(error);
            _context.ReportRecoverableError(LastError);
            if (!_context.TryReturnToLobby(out var navigationError) && !navigationError.IsEmpty)
                LastError = navigationError;
        }

        private bool CanSendCommand()
        {
            if (_context == null) return Fail(new ShellFlowError(MissingContext));
            if (_context.Navigator == null) return Fail(new ShellFlowError(MissingNavigator));
            return _context.Navigator.TransitionState == AppTransitionState.Idle;
        }

        private void OnGUI()
        {
            var layout = PortraitShellLayout.CreateSettlement(Screen.width, Screen.height, Screen.safeArea);
            EnsureStyles(layout.Frame.Scale);

            ShellGui.DrawPanel(layout.Frame.SafeArea, _styles.Panel);
            GUI.Label(layout.Title, "战斗结算", _styles.Title);
            ShellGui.DrawPanel(layout.ResultCard, _styles.Panel);

            if (HasViewData)
            {
                GUI.Label(layout.Outcome, ViewData.Victory ? "胜利" : "失败", _styles.ResultOutcome);
                GUI.Label(layout.ReachedWave, "到达波次  " + ViewData.ReachedWave, _styles.ResultMetric);
                GUI.Label(layout.RemainingLives, "剩余生命  " + ViewData.RemainingLives, _styles.ResultMetric);
            }
            else
            {
                GUI.Label(layout.Outcome, "正在返回大厅", _styles.ResultOutcome);
            }

            var transitioning = _context?.Navigator == null
                || _context.Navigator.TransitionState != AppTransitionState.Idle;
            var previousEnabled = GUI.enabled;
            GUI.enabled = HasViewData && !transitioning;
            if (GUI.Button(layout.RetryButton, "再来一局", _styles.PrimaryButton)) TryRetry();
            GUI.enabled = !transitioning;
            if (GUI.Button(layout.ReturnButton, "返回大厅", _styles.SecondaryButton)) TryReturn();
            GUI.enabled = previousEnabled;

            var status = LastError.IsEmpty ? "" : "已安全处理：" + LastError.Code;
            GUI.Label(layout.Status, status, _styles.Status);
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
    }
}
