using UnityEngine;

namespace FruitDefense.UI
{
    public enum RuntimeUiPointerPhase
    {
        None = 0,
        Down = 1,
        Move = 2,
        Up = 3,
        Cancel = 4,
    }

    public readonly struct RuntimeUiPointerSample
    {
        public RuntimeUiPointerSample(RuntimeUiPointerPhase phase,
            Vector2 position, int button = 0)
        {
            Phase = phase;
            Position = position;
            Button = button;
        }

        public RuntimeUiPointerPhase Phase { get; }
        public Vector2 Position { get; }
        public int Button { get; }
        public bool IsPrimary => Button == 0;

        public static RuntimeUiPointerSample FromEvent(Event current)
        {
            if (current == null) return default;
            switch (current.rawType)
            {
                case EventType.MouseDown:
                    return new RuntimeUiPointerSample(
                        RuntimeUiPointerPhase.Down, current.mousePosition, current.button);
                case EventType.MouseDrag:
                case EventType.MouseMove:
                    return new RuntimeUiPointerSample(
                        RuntimeUiPointerPhase.Move, current.mousePosition, current.button);
                case EventType.MouseUp:
                    return new RuntimeUiPointerSample(
                        RuntimeUiPointerPhase.Up, current.mousePosition, current.button);
                case EventType.MouseLeaveWindow:
                case EventType.Ignore:
                    return new RuntimeUiPointerSample(
                        RuntimeUiPointerPhase.Cancel, current.mousePosition, current.button);
                default:
                    return new RuntimeUiPointerSample(
                        RuntimeUiPointerPhase.None, current.mousePosition, current.button);
            }
        }
    }

    public readonly struct RuntimeUiPressResult
    {
        public RuntimeUiPressResult(bool hovered, bool pressed,
            bool activated, bool cancelled)
        {
            Hovered = hovered;
            Pressed = pressed;
            Activated = activated;
            Cancelled = cancelled;
        }

        public bool Hovered { get; }
        public bool Pressed { get; }
        public bool Activated { get; }
        public bool Cancelled { get; }
    }

    public struct RuntimeUiPressTracker
    {
        private int activeControlId;
        private Vector2 pressOrigin;

        public int ActiveControlId => activeControlId;
        public bool HasOwner => activeControlId != 0;

        public RuntimeUiPressResult Update(int controlId, Rect hitRect, bool enabled,
            RuntimeUiPointerSample pointer, float dragCancelDistance)
        {
            var hovered = hitRect.Contains(pointer.Position);
            if (controlId == 0 || !RuntimeUiNumbers.IsFinite(dragCancelDistance)
                || dragCancelDistance < 0f)
                return new RuntimeUiPressResult(hovered, false, false, false);

            if (!enabled)
            {
                var cancelled = activeControlId == controlId;
                if (cancelled) Cancel();
                return new RuntimeUiPressResult(hovered, false, false, cancelled);
            }

            if (pointer.Phase == RuntimeUiPointerPhase.Cancel)
            {
                var cancelled = activeControlId == controlId;
                if (cancelled) Cancel();
                return new RuntimeUiPressResult(hovered, false, false, cancelled);
            }

            if (pointer.IsPrimary && pointer.Phase == RuntimeUiPointerPhase.Down
                && activeControlId == 0 && hovered)
            {
                activeControlId = controlId;
                pressOrigin = pointer.Position;
                return new RuntimeUiPressResult(true, true, false, false);
            }

            if (activeControlId != controlId)
                return new RuntimeUiPressResult(hovered, false, false, false);

            if (pointer.IsPrimary && pointer.Phase == RuntimeUiPointerPhase.Move)
            {
                if (!hovered
                    || Vector2.Distance(pressOrigin, pointer.Position) > dragCancelDistance)
                {
                    Cancel();
                    return new RuntimeUiPressResult(hovered, false, false, true);
                }
                return new RuntimeUiPressResult(true, true, false, false);
            }

            if (pointer.IsPrimary && pointer.Phase == RuntimeUiPointerPhase.Up)
            {
                var activated = hovered;
                var cancelled = !activated;
                Cancel();
                return new RuntimeUiPressResult(hovered, false, activated, cancelled);
            }

            return new RuntimeUiPressResult(hovered,
                hovered, false, false);
        }

        public void Cancel()
        {
            activeControlId = 0;
            pressOrigin = default;
        }
    }
}
