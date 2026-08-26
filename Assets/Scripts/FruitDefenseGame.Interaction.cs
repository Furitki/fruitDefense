using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.App;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Platform;
using FruitDefense.Presentation;
using FruitDefense.Tilemaps;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense
{
    public sealed partial class FruitDefenseGame
    {
        private void OnGUI()
        {
            if (!_isInitialized || _game == null) return;
            var outerMatrix = GUI.matrix;
            try
            {
                var layout = BattleLayout;
                _runtimeUiDrawContext = RuntimeUiGui.RequireContext(
                    _runtimeUiDrawContext, _runtimeUiTheme, 1f);
                var safeArea = RuntimeSafeAreaResolver.ResolveCurrent();
                var viewportLayout = BattlefieldProjection.CalculateViewportLayout(
                    Screen.width, Screen.height, safeArea,
                    BattleUiLayout.DesignWidth, BattleUiLayout.DesignHeight);
                GUI.matrix = Matrix4x4.identity;
                RuntimeUiGui.DrawScreenBackground(_runtimeUiDrawContext,
                    new Rect(0f, 0f, Screen.width, Screen.height));
                GUI.matrix = viewportLayout.GuiMatrix;
                HandleDragInput(Event.current, layout);
                var currentDropTarget = CurrentDropTarget(layout);
                var currentDropCue = ResolveDropCue(currentDropTarget);
                DrawHeader(layout, _runtimeUiDrawContext);
                DrawBoard(layout, _runtimeUiDrawContext, currentDropTarget, currentDropCue);
                DrawEmbeddedBattleControls(
                    layout, _runtimeUiDrawContext, currentDropTarget, currentDropCue);
                DrawDragGhost(
                    layout, _runtimeUiDrawContext, currentDropTarget, currentDropCue);
                DrawOverlay(layout, _runtimeUiDrawContext);
                if (_floatingTextOverlay != null)
                    _floatingTextOverlay.DrawOnGuiRepaint();
            }
            finally
            {
                GUI.matrix = outerMatrix;
            }
        }

        private void HandleDragInput(Event evt, BattleUiLayout layout)
        {
            if (_game == null) return;
            var controlId = GUIUtility.GetControlID(0x4F524348, FocusType.Passive);
            var viewState = BattleUiPresentationState.Create(
                _game.State.Phase, _game.State.Paused);
            if (viewState.BlocksDrag && _drag != null) CancelDrag("拖拽已取消，物品返回原位");

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && _drag != null)
            {
                CancelDrag("已取消拖拽，物品返回原位");
                evt.Use();
                return;
            }
            if (viewState.BlocksDrag) return;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var source = FindDragSourceAt(evt.mousePosition, layout);
                if (source == null) return;
                source.Start = evt.mousePosition;
                source.Current = evt.mousePosition;
                _drag = source;
                _dragControlId = controlId;
                GUIUtility.hotControl = controlId;
                evt.Use();
                return;
            }

            if (_drag == null) return;
            if (evt.type == EventType.MouseDrag)
            {
                _drag.Current = evt.mousePosition;
                if (!_drag.Active && DragGeometry.CrossedActivationThreshold(
                        _drag.Start, _drag.Current))
                {
                    _drag.Active = true;
                    _selectedEquipmentId = string.Empty;
                    _potToolSelected = false;
                }
                if (_drag.Active) UpdateDragHoverStatus(_drag.Current, layout);
                evt.Use();
                return;
            }

            if (evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp)
            {
                var session = _drag;
                session.Current = evt.mousePosition;
                if (session.Active) CompleteDrag(session, session.Current, layout);
                else PerformSourceClick(session);
                _drag = null;
                if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
                _dragControlId = 0;
                evt.Use();
            }
        }

        private DragSession FindDragSourceAt(Vector2 point, BattleUiLayout layout)
        {
            foreach (var plant in _game.State.Plants)
            {
                var rect = PlantSourceRect(plant, layout);
                if (rect.width > 0f && rect.Contains(point))
                    return new DragSession { Type = DragPayloadType.Plant, PlantId = plant.Id };
            }
            if (_game.State.Inventory.Get(BattleContentIds.Equipment.Gatling) > 0
                && layout.EquipmentTool(BattleContentIds.Equipment.Gatling).Contains(point))
                return new DragSession
                {
                    Type = DragPayloadType.Equipment,
                    EquipmentId = BattleContentIds.Equipment.Gatling,
                };
            if (_game.State.Inventory.Get(BattleContentIds.Equipment.Ice) > 0
                && layout.EquipmentTool(BattleContentIds.Equipment.Ice).Contains(point))
                return new DragSession
                {
                    Type = DragPayloadType.Equipment,
                    EquipmentId = BattleContentIds.Equipment.Ice,
                };
            if (_game.State.Inventory.Get(BattleContentIds.Equipment.Chili) > 0
                && layout.EquipmentTool(BattleContentIds.Equipment.Chili).Contains(point))
                return new DragSession
                {
                    Type = DragPayloadType.Equipment,
                    EquipmentId = BattleContentIds.Equipment.Chili,
                };
            if (_game.State.Inventory.Pots > 0 && layout.PotTool.Contains(point))
                return new DragSession { Type = DragPayloadType.Pot };
            return null;
        }

        private DropTarget FindDropTargetAt(
            DragSession session, Vector2 cursor, BattleUiLayout layout)
        {
            var targets = new List<DropTarget>();
            if (session.Type == DragPayloadType.Plant)
            {
                foreach (var pot in _game.State.Pots.Where(value => value.Active))
                {
                    var rect = layout.Battlefield.PotHitRect(pot.Cell);
                    targets.Add(new DropTarget { Type = DropTargetType.Pot, Id = pot.Id, Rect = rect });
                }
                for (var slot = 0; slot < 5; slot++)
                {
                    var rect = layout.NurserySlot(slot);
                    targets.Add(new DropTarget { Type = DropTargetType.Nursery, Slot = slot, Rect = rect });
                }
            }
            else if (session.Type == DragPayloadType.Equipment)
            {
                foreach (var plant in _game.State.Plants)
                {
                    var rect = PlantSourceRect(plant, layout);
                    if (rect.width > 0f)
                        targets.Add(new DropTarget { Type = DropTargetType.Plant, Id = plant.Id, Rect = rect });
                }
            }
            else
            {
                foreach (var cell in _game.Map.PlantableCells)
                {
                    if (_game.State.Pots.Any(pot => pot.Active && pot.Cell == cell)) continue;
                    var rect = layout.Battlefield.PotHitRect(cell);
                    targets.Add(new DropTarget { Type = DropTargetType.Expansion, Cell = cell, Rect = rect });
                }
            }

            var rects = targets.Select(target => target.Rect).ToList();
            var bestIndex = DragGeometry.BestOverlapIndex(DragGeometry.PreviewRect(cursor), rects);
            return bestIndex >= 0 ? targets[bestIndex] : new DropTarget { Type = DropTargetType.None };
        }

        private void PerformSourceClick(DragSession source)
        {
            if (source.Type == DragPayloadType.Equipment)
            {
                ToggleEquipmentSelection(source.EquipmentId);
                return;
            }
            if (source.Type == DragPayloadType.Pot)
            {
                TogglePotTool();
                return;
            }
            var plant = _game.PlantById(source.PlantId);
            if (plant != null) HandlePlantClick(plant);
        }

        private void CompleteDrag(DragSession session, Vector2 point, BattleUiLayout layout)
        {
            var target = FindDropTargetAt(session, point, layout);
            if (session.Type == DragPayloadType.Plant)
            {
                if (target.Type == DropTargetType.Pot)
                {
                    var status = _game.GetPlantDropStatus(session.PlantId, target.Id);
                    if (!status.Legal) { CancelDrag(status.Reason); return; }
                    var targetPlant = _game.PlantAtPot(target.Id);
                    var selectedAfterDrop = status.Action == PlantDropAction.Merge && targetPlant != null
                        ? targetPlant.Id
                        : session.PlantId;
                    var success = _game.MoveOrMergePlant(session.PlantId, target.Id, out var reason);
                    if (success) _inspectedPlantId = selectedAfterDrop;
                    SetStatus(success, reason);
                    return;
                }
                if (target.Type == DropTargetType.Nursery)
                {
                    var status = _game.GetNurseryDropStatus(session.PlantId, target.Slot);
                    if (!status.Legal) { CancelDrag(status.Reason); return; }
                    var success = _game.MoveToNursery(session.PlantId, target.Slot, out var reason);
                    if (success) _inspectedPlantId = -1;
                    SetStatus(success, reason);
                    return;
                }
                CancelDrag("未命中花盆或刷新栏，水果返回原位");
                return;
            }

            if (session.Type == DragPayloadType.Equipment)
            {
                if (target.Type != DropTargetType.Plant) { CancelDrag("未命中植物，武器返回库存"); return; }
                var status = _game.GetEquipmentInstallStatus(
                    target.Id, session.EquipmentId);
                if (!status.Legal) { CancelDrag(status.Reason); return; }
                var success = _game.InstallEquipment(
                    target.Id, session.EquipmentId, out var reason);
                if (success) _inspectedPlantId = target.Id;
                SetStatus(success, reason);
                return;
            }

            if (target.Type != DropTargetType.Expansion || !_game.CanExpand(target.Cell))
            {
                CancelDrag("未命中绿色扩建格，花盆返回库存");
                return;
            }
            SetStatus(_game.ExpandPot(target.Cell, out var expandReason), expandReason);
        }

        private void CancelDrag(string reason)
        {
            if (_drag != null && _drag.Type == DragPayloadType.Plant)
            {
                _returnPulsePlantId = _drag.PlantId;
                _returnPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledStatusSeconds);
            }
            SetStatus(false, reason);
            _drag = null;
            if (GUIUtility.hotControl == _dragControlId) GUIUtility.hotControl = 0;
            _dragControlId = 0;
        }

        private void UpdateDragHoverStatus(Vector2 point, BattleUiLayout layout)
        {
            var target = FindDropTargetAt(_drag, point, layout);
            var status = DragTargetStatus(_drag, target);
            _status = BattleUiPresentationState.FormatTransientStatus(
                status.Legal, status.Reason);
            _statusState = status.Legal
                ? RuntimeUiInteractionState.Success
                : RuntimeUiInteractionState.Error;
            InvalidatePreparedStatusText();
            _statusPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledFocusSeconds);
        }

        private InteractionStatus DragTargetStatus(DragSession session, DropTarget target)
        {
            if (session == null || target.Type == DropTargetType.None)
                return new InteractionStatus(false, "松开将取消，物品返回原位");
            if (session.Type == DragPayloadType.Plant)
            {
                var status = PlantDragTargetStatus(session, target);
                return new InteractionStatus(status.Legal, status.Reason);
            }
            if (session.Type == DragPayloadType.Equipment)
                return target.Type == DropTargetType.Plant
                    ? _game.GetEquipmentInstallStatus(target.Id,
                        session.EquipmentId)
                    : new InteractionStatus(false, "请拖到一株植物上");
            return new InteractionStatus(target.Type == DropTargetType.Expansion && _game.CanExpand(target.Cell),
                target.Type == DropTargetType.Expansion && _game.CanExpand(target.Cell) ? "松开添加花盆" : "请拖到绿色扩建格");
        }

        private PlantDropStatus PlantDragTargetStatus(DragSession session, DropTarget target)
        {
            if (session == null || session.Type != DragPayloadType.Plant)
                return new PlantDropStatus(false, PlantDropAction.Invalid, "这里不能放置水果");
            return target.Type == DropTargetType.Pot
                ? _game.GetPlantDropStatus(session.PlantId, target.Id)
                : target.Type == DropTargetType.Nursery
                    ? _game.GetNurseryDropStatus(session.PlantId, target.Slot)
                    : new PlantDropStatus(false, PlantDropAction.Invalid, "这里不能放置水果");
        }

        private DropTarget CurrentDropTarget(BattleUiLayout layout)
        {
            return _drag != null && _drag.Active
                ? FindDropTargetAt(_drag, _drag.Current, layout)
                : new DropTarget { Type = DropTargetType.None };
        }

        private BattleUiDropCue ResolveDropCue(DropTarget target)
        {
            if (_drag == null || !_drag.Active) return BattleUiDropCue.None;
            if (_drag.Type == DragPayloadType.Plant)
            {
                var status = PlantDragTargetStatus(_drag, target);
                return BattleUiPresentationState.ResolveDropCue(status.Legal,
                    status.Action == PlantDropAction.Merge,
                    status.Action == PlantDropAction.Swap);
            }

            var interaction = DragTargetStatus(_drag, target);
            return BattleUiPresentationState.ResolveDropCue(
                interaction.Legal, false, false);
        }

        private static bool MatchesDropTarget(DropTarget candidate, DropTarget current)
        {
            if (candidate.Type != current.Type) return false;
            switch (candidate.Type)
            {
                case DropTargetType.Pot:
                case DropTargetType.Plant: return candidate.Id == current.Id;
                case DropTargetType.Nursery: return candidate.Slot == current.Slot;
                case DropTargetType.Expansion: return candidate.Cell == current.Cell;
                default: return false;
            }
        }

        private static void DrawDropCue(RuntimeUiDrawContext drawContext,
            Rect target, BattleUiDropCue cue)
        {
            if (cue == BattleUiDropCue.None) return;
            RuntimeUiGui.DrawIndicator(drawContext, BattleUiLayout.CueBadge(target),
                BattleUiPresentationState.DropIndicatorKind(cue));
        }

        private static bool ShouldShowMergeHint(DragPayloadType payloadType, PlantDropStatus status)
        {
            return payloadType == DragPayloadType.Plant
                && status.Legal
                && status.Action == PlantDropAction.Merge;
        }

        private void ToggleEquipmentSelection(string equipmentId)
        {
            _selectedEquipmentId = string.Equals(
                    _selectedEquipmentId, equipmentId, StringComparison.Ordinal)
                ? string.Empty
                : equipmentId;
            _potToolSelected = false;
            if (string.IsNullOrEmpty(_selectedEquipmentId))
            {
                _selectionPulseTarget = 0;
                _selectionPulse = default;
            }
            else
            {
                _selectionPulseTarget =
                    BattlePresentationVisualCatalog.EquipmentToolIndex(
                        _selectedEquipmentId) + 1;
                _selectionPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledSelectionSeconds);
            }
            SetStatus(true, string.IsNullOrEmpty(_selectedEquipmentId)
                ? "已取消武器选择"
                : "拖动或点击植物安装" + EquipmentDisplayName(
                    _selectedEquipmentId));
        }

        private void TogglePotTool()
        {
            _potToolSelected = !_potToolSelected;
            _selectedEquipmentId = string.Empty;
            _selectionPulseTarget = _potToolSelected ? -1 : 0;
            _selectionPulse = _potToolSelected
                ? RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                    _runtimeUiTheme.Feedback.UnscaledSelectionSeconds)
                : default;
            SetStatus(true, _potToolSelected ? "拖动花盆到绿色候选格，或点击扩建" : "已取消扩建");
        }

        private BattleUiLayout BattleLayout
        {
            get
            {
                if (_battleUiLayout == null)
                    throw new InvalidOperationException("Battle UI layout is unavailable before initialization.");
                return _battleUiLayout;
            }
        }

        private BattlefieldProjection Projection => BattleLayout.Battlefield;

        private static Rect ExpansionRect(Vector2Int cell, BattleUiLayout layout)
        {
            return layout.Battlefield.PotHitRect(cell);
        }

        private static Rect PotHitRect(Pot pot, BattleUiLayout layout)
        {
            return layout.Battlefield.PotHitRect(pot.Cell);
        }

        private static Rect PotVisualRect(Pot pot, BattleUiLayout layout)
        {
            return layout.Battlefield.PotVisualRect(pot.Cell);
        }

        private Rect PlantSourceRect(Plant plant, BattleUiLayout layout)
        {
            if (plant.PotId >= 0)
            {
                var pot = _game.PotById(plant.PotId);
                return pot == null ? new Rect() : PotHitRect(pot, layout);
            }
            return plant.NurseryIndex >= 0
                ? layout.NurserySlot(plant.NurseryIndex)
                : new Rect();
        }
        private static Rect Grow(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static Rect TransformChild(Rect child, Rect sourceParent,
            Rect visualParent)
        {
            if (sourceParent.width <= 0f || sourceParent.height <= 0f) return child;
            var scaleX = visualParent.width / sourceParent.width;
            var scaleY = visualParent.height / sourceParent.height;
            return new Rect(
                visualParent.x + (child.x - sourceParent.x) * scaleX,
                visualParent.y + (child.y - sourceParent.y) * scaleY,
                child.width * scaleX,
                child.height * scaleY);
        }

        private static bool ContainsRect(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }

        private static bool ContainsPointer(Rect rect)
        {
            return Event.current != null && rect.Contains(Event.current.mousePosition);
        }

        private static bool IsPointerPress(Rect rect)
        {
            return ContainsPointer(rect) && Event.current.button == 0
                && (Event.current.rawType == EventType.MouseDown
                    || Event.current.rawType == EventType.MouseDrag);
        }

        private void BeginBattleActionPress(int target)
        {
            _actionPressTarget = target;
            _actionPressPulse = RuntimeUiFeedbackPulse.Begin(Time.unscaledTime,
                _runtimeUiTheme.Feedback.UnscaledPressSeconds);
        }

        private RuntimeUiMotionSample BattleActionMotion(int target)
        {
            if (_actionPressTarget != target)
                return RuntimeUiMotionSample.Rest;
            return RuntimeUiMotion.Evaluate(_actionPressPulse, Time.unscaledTime,
                _runtimeUiTheme.Feedback, RuntimeUiMotionPattern.Press);
        }

        private RuntimeUiPressResult TrackBattleAction(int target, Rect rect,
            bool enabled = true)
        {
            return _actionPressTracker.Update(target, rect, enabled,
                RuntimeUiPointerSample.FromEvent(Event.current),
                _runtimeUiTheme.Feedback.DragCancelDistance);
        }

        private static bool DrawSharedHitTarget(RuntimeUiDrawContext drawContext,
            Rect rect, RuntimeUiInteractionState state)
        {
            var enabled = GUI.enabled;
            GUI.enabled = enabled && state != RuntimeUiInteractionState.Disabled
                && state != RuntimeUiInteractionState.Loading;
            try
            {
                return GUI.Button(rect, GUIContent.none, drawContext.Styles.HitTarget);
            }
            finally
            {
                GUI.enabled = enabled;
            }
        }

    }
}
