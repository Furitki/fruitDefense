using System;
using System.Collections.Generic;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    public enum LayeredTerrainPainterTool
    {
        AOnB,
        BOnA,
        LandformA,
        LandformB,
        EraseLandform,
        ClearCell,
    }

    public static class LayeredTerrainPainterToolUtility
    {
        public static bool ContainsLandform(LayeredTerrainPainterTool tool)
        {
            return tool == LayeredTerrainPainterTool.AOnB
                || tool == LayeredTerrainPainterTool.BOnA
                || tool == LayeredTerrainPainterTool.LandformA
                || tool == LayeredTerrainPainterTool.LandformB;
        }

        public static bool IsPair(LayeredTerrainPainterTool tool)
        {
            return tool == LayeredTerrainPainterTool.AOnB
                || tool == LayeredTerrainPainterTool.BOnA;
        }

        public static LayeredTerrainMaterial LandformMaterial(LayeredTerrainPainterTool tool)
        {
            return tool == LayeredTerrainPainterTool.BOnA
                || tool == LayeredTerrainPainterTool.LandformB
                ? LayeredTerrainMaterial.B : LayeredTerrainMaterial.A;
        }

        public static LayeredTerrainMaterial ExpectedBaseMaterial(LayeredTerrainPainterTool tool)
        {
            return LandformMaterial(tool) == LayeredTerrainMaterial.A
                ? LayeredTerrainMaterial.B : LayeredTerrainMaterial.A;
        }

        public static string Label(LayeredTerrainTilemap target,
            LayeredTerrainPainterTool tool)
        {
            if (target == null) return "未选择笔刷";
            var a = target.MaterialDisplayName(LayeredTerrainMaterial.A);
            var b = target.MaterialDisplayName(LayeredTerrainMaterial.B);
            string label;
            switch (tool)
            {
                case LayeredTerrainPainterTool.AOnB:
                    label = a + "覆" + b;
                    break;
                case LayeredTerrainPainterTool.BOnA:
                    label = b + "覆" + a;
                    break;
                case LayeredTerrainPainterTool.LandformA:
                    label = a + "地貌";
                    break;
                case LayeredTerrainPainterTool.LandformB:
                    label = b + "地貌";
                    break;
                case LayeredTerrainPainterTool.EraseLandform:
                    return "擦除地貌（保留底图）";
                default:
                    return "清空格子（全部移除）";
            }
            return label;
        }
    }

    public sealed class LayeredTerrainPaintSession : IDisposable
    {
        private readonly HashSet<Vector3Int> gestureCells = new HashSet<Vector3Int>();
        private LayeredTerrainTilemap target;
        private LayeredTerrainPainterTool tool = LayeredTerrainPainterTool.AOnB;
        private bool pureBaseOnly;
        private bool active;
        private bool subscribed;
        private bool gestureActive;
        private int gestureUndoGroup = -1;
        private int gestureMutationCount;
        private bool hasHoveredCell;
        private Vector3Int hoveredCell;
        private Vector3 hoveredCenter;
        private SceneView hoveredSceneView;
        private readonly Dictionary<int, SceneViewMouseMoveSetting> mouseMoveSettings =
            new Dictionary<int, SceneViewMouseMoveSetting>();

        private sealed class SceneViewMouseMoveSetting
        {
            public SceneView SceneView;
            public bool PreviousMouseMoveValue;
            public bool PreviousMouseEnterLeaveValue;
        }

        public event Action Changed;

        public LayeredTerrainTilemap Target { get { return target; } }
        public LayeredTerrainPainterTool Tool { get { return tool; } }
        public bool PureBaseOnly { get { return pureBaseOnly; } }
        public bool IsActive { get { return active; } }
        public bool IsGestureActive { get { return gestureActive; } }
        internal bool HasHoveredCell { get { return hasHoveredCell; } }
        internal Vector3Int HoveredCell { get { return hoveredCell; } }
        internal int TrackedMouseMoveSceneCount { get { return mouseMoveSettings.Count; } }
        public int LastCompletedGestureMutationCount { get; private set; }
        public string ActiveToolLabel
        {
            get
            {
                var label = LayeredTerrainPainterToolUtility.Label(target, tool);
                return pureBaseOnly && LayeredTerrainPainterToolUtility.ContainsLandform(tool)
                    ? label + "（只绘制纯图）" : label;
            }
        }

        public void SetTarget(LayeredTerrainTilemap value)
        {
            if (ReferenceEquals(target, value)) return;
            Stop();
            target = value;
            NotifyChanged();
        }

        public void SetTool(LayeredTerrainPainterTool value)
        {
            if (tool == value) return;
            EndGesture();
            ClearHoveredCellAndRepaint();
            tool = value;
            if (!LayeredTerrainPainterToolUtility.ContainsLandform(tool))
                pureBaseOnly = false;
            NotifyChanged();
            SceneView.RepaintAll();
        }

        public void SetPureBaseOnly(bool value)
        {
            if (pureBaseOnly == value) return;
            EndGesture();
            ClearHoveredCellAndRepaint();
            pureBaseOnly = value;
            NotifyChanged();
            SceneView.RepaintAll();
        }

        public bool ValidateActiveTool(out string reason)
        {
            if (target == null)
            {
                reason = "请先选择要编辑的地图。";
                return false;
            }
            if (!target.ValidateAuthoringConfiguration(out reason)) return false;
            if (!target.ValidateAuthoringPresentation(out reason)) return false;
            if (pureBaseOnly && LayeredTerrainPainterToolUtility.ContainsLandform(tool))
                return target.CanPaintPair(
                    LayeredTerrainPainterToolUtility.LandformMaterial(tool),
                    LayeredTerrainPainterToolUtility.ExpectedBaseMaterial(tool),
                    false, out reason);
            if (!LayeredTerrainPainterToolUtility.ContainsLandform(tool))
            {
                reason = "ok";
                return true;
            }
            return target.CanPaintPair(
                LayeredTerrainPainterToolUtility.LandformMaterial(tool),
                LayeredTerrainPainterToolUtility.ExpectedBaseMaterial(tool),
                true, out reason);
        }

        public bool CanUseTool(LayeredTerrainPainterTool value, out string reason)
        {
            return CanUseTool(value, pureBaseOnly, out reason);
        }

        public bool CanUseTool(LayeredTerrainPainterTool value, bool usePureBaseOnly,
            out string reason)
        {
            if (target == null)
            {
                reason = "Select a terrain laboratory target first.";
                return false;
            }
            if (usePureBaseOnly && LayeredTerrainPainterToolUtility.ContainsLandform(value))
                return target.CanPaintPair(
                    LayeredTerrainPainterToolUtility.LandformMaterial(value),
                    LayeredTerrainPainterToolUtility.ExpectedBaseMaterial(value),
                    false, out reason);
            if (!LayeredTerrainPainterToolUtility.ContainsLandform(value))
            {
                reason = "ok";
                return true;
            }
            return target.CanPaintPair(
                LayeredTerrainPainterToolUtility.LandformMaterial(value),
                LayeredTerrainPainterToolUtility.ExpectedBaseMaterial(value),
                true, out reason);
        }

        public bool Start(out string reason)
        {
            if (!ValidateActiveTool(out reason)) return false;
            if (active) return true;
            active = true;
            Subscribe();
            NotifyChanged();
            SceneView.RepaintAll();
            return true;
        }

        public void Stop()
        {
            EndGesture();
            ClearHoveredCellAndRepaint();
            if (!active && !subscribed) return;
            active = false;
            Unsubscribe();
            NotifyChanged();
            SceneView.RepaintAll();
        }

        public bool BeginGesture(out string reason)
        {
            if (!active)
            {
                reason = "请先开始绘制。";
                return false;
            }
            if (gestureActive)
            {
                reason = "ok";
                return true;
            }
            if (!ValidateActiveTool(out reason)) return false;
            Undo.IncrementCurrentGroup();
            gestureUndoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("地貌绘制：" + ActiveToolLabel);
            Undo.RegisterCompleteObjectUndo(AuthoringObjects(), "地貌绘制：" + ActiveToolLabel);
            gestureCells.Clear();
            gestureMutationCount = 0;
            gestureActive = true;
            reason = "ok";
            return true;
        }

        public bool ApplyCell(Vector3Int cell, out string reason)
        {
            if (!gestureActive)
            {
                reason = "绘制手势尚未开始。";
                return false;
            }
            if (!gestureCells.Add(cell))
            {
                reason = "duplicate";
                return true;
            }

            bool changed;
            if (pureBaseOnly && LayeredTerrainPainterToolUtility.ContainsLandform(tool))
            {
                changed = target.PaintBase(cell,
                    LayeredTerrainPainterToolUtility.LandformMaterial(tool), out reason);
                if (!changed) return false;
                gestureMutationCount++;
                MarkDirty(target);
                NotifyChanged();
                return true;
            }
            switch (tool)
            {
                case LayeredTerrainPainterTool.AOnB:
                    changed = target.PaintPair(cell, LayeredTerrainMaterial.A,
                        LayeredTerrainMaterial.B, true, out reason);
                    break;
                case LayeredTerrainPainterTool.BOnA:
                    changed = target.PaintPair(cell, LayeredTerrainMaterial.B,
                        LayeredTerrainMaterial.A, true, out reason);
                    break;
                case LayeredTerrainPainterTool.LandformA:
                    changed = target.PaintLandform(cell, LayeredTerrainMaterial.A,
                        true, out reason);
                    break;
                case LayeredTerrainPainterTool.LandformB:
                    changed = target.PaintLandform(cell, LayeredTerrainMaterial.B,
                        true, out reason);
                    break;
                case LayeredTerrainPainterTool.EraseLandform:
                    changed = target.EraseLandform(cell, out reason);
                    break;
                default:
                    changed = target.EraseCell(cell, out reason);
                    break;
            }

            if (!changed) return false;
            gestureMutationCount++;
            MarkDirty(target);
            NotifyChanged();
            return true;
        }

        public void EndGesture()
        {
            if (!gestureActive) return;
            LastCompletedGestureMutationCount = gestureMutationCount;
            if (gestureUndoGroup >= 0) Undo.CollapseUndoOperations(gestureUndoGroup);
            gestureCells.Clear();
            gestureMutationCount = 0;
            gestureUndoGroup = -1;
            gestureActive = false;
            NotifyChanged();
        }

        public void Dispose()
        {
            Stop();
        }

        private void Subscribe()
        {
            if (subscribed) return;
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            SceneView.duringSceneGui -= OnSceneGui;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            RestoreMouseMoveSettings();
            subscribed = false;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) Stop();
        }

        private void OnSceneGui(SceneView sceneView)
        {
            if (!active) return;
            EnsurePointerEvents(sceneView);
            if (target == null || target.BaseLogicalTilemap == null)
            {
                Stop();
                return;
            }

            var current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                Stop();
                current.Use();
                return;
            }

            if (current.type == EventType.MouseLeaveWindow)
            {
                ClearHoveredCellAndRepaint(sceneView);
                return;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (current.type == EventType.MouseUp && current.button == 0)
            {
                EndGesture();
                current.Use();
                return;
            }

            Vector3Int cell;
            Vector3 center;
            var hasHoveredCell = TryResolveHoveredCell(current.mousePosition, out cell, out center);
            if (hasHoveredCell) SetHoveredCell(sceneView, cell, center);
            else ClearHoveredCellAndRepaint(sceneView);
            if (current.type == EventType.Repaint && this.hasHoveredCell
                && hoveredSceneView == sceneView)
            {
                var cellSize = target.BaseLogicalTilemap.layoutGrid.cellSize;
                Handles.color = tool == LayeredTerrainPainterTool.EraseLandform
                        || tool == LayeredTerrainPainterTool.ClearCell
                    ? new Color(1f, .35f, .25f, .95f)
                    : new Color(.25f, .9f, 1f, .95f);
                Handles.DrawWireCube(hoveredCenter, new Vector3(cellSize.x, cellSize.y, 0f));
                Handles.Label(hoveredCenter + Vector3.up * cellSize.y * .58f, ActiveToolLabel);
            }
            if (current.alt || current.button != 0 || !hasHoveredCell
                || (current.type != EventType.MouseDown && current.type != EventType.MouseDrag))
                return;

            string reason;
            if (current.type == EventType.MouseDown && !BeginGesture(out reason))
            {
                Debug.LogWarning("Terrain painter could not start: " + reason);
                current.Use();
                return;
            }
            if (!gestureActive && !BeginGesture(out reason)) return;
            if (!ApplyCell(cell, out reason) && reason != "duplicate")
                Debug.LogWarning("Terrain painter skipped cell " + cell + ": " + reason);
            current.Use();
            sceneView.Repaint();
        }

        internal bool SetHoveredCell(Vector3Int cell, Vector3 center)
        {
            if (hasHoveredCell && hoveredCell == cell && hoveredCenter == center) return false;
            hasHoveredCell = true;
            hoveredCell = cell;
            hoveredCenter = center;
            return true;
        }

        internal bool ClearHoveredCell()
        {
            if (!hasHoveredCell && hoveredSceneView == null) return false;
            hasHoveredCell = false;
            hoveredCell = default(Vector3Int);
            hoveredCenter = default(Vector3);
            hoveredSceneView = null;
            return true;
        }

        private void SetHoveredCell(SceneView sceneView, Vector3Int cell, Vector3 center)
        {
            var previousScene = hoveredSceneView;
            var changedScene = hoveredSceneView != sceneView;
            hoveredSceneView = sceneView;
            if (SetHoveredCell(cell, center) || changedScene)
            {
                if (previousScene != null && previousScene != sceneView) previousScene.Repaint();
                sceneView.Repaint();
            }
        }

        private void ClearHoveredCellAndRepaint(SceneView sceneView = null)
        {
            if (sceneView != null && hoveredSceneView != null && hoveredSceneView != sceneView) return;
            var previousScene = hoveredSceneView;
            if (!ClearHoveredCell()) return;
            if (previousScene != null) previousScene.Repaint();
            else if (sceneView != null) sceneView.Repaint();
        }

        internal void EnsurePointerEvents(SceneView sceneView)
        {
            if (!active || sceneView == null) return;
            var instanceId = sceneView.GetInstanceID();
            if (!mouseMoveSettings.ContainsKey(instanceId))
                mouseMoveSettings.Add(instanceId, new SceneViewMouseMoveSetting
                {
                    SceneView = sceneView,
                    PreviousMouseMoveValue = sceneView.wantsMouseMove,
                    PreviousMouseEnterLeaveValue = sceneView.wantsMouseEnterLeaveWindow,
                });
            sceneView.wantsMouseMove = true;
            sceneView.wantsMouseEnterLeaveWindow = true;
        }

        private void RestoreMouseMoveSettings()
        {
            foreach (var setting in mouseMoveSettings.Values)
                if (setting.SceneView != null)
                {
                    setting.SceneView.wantsMouseMove = setting.PreviousMouseMoveValue;
                    setting.SceneView.wantsMouseEnterLeaveWindow =
                        setting.PreviousMouseEnterLeaveValue;
                }
            mouseMoveSettings.Clear();
        }

        private bool TryResolveHoveredCell(Vector2 mousePosition, out Vector3Int cell,
            out Vector3 center)
        {
            var logical = target.BaseLogicalTilemap;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(logical.transform.forward, logical.transform.position);
            float distance;
            if (!plane.Raycast(ray, out distance))
            {
                cell = default(Vector3Int);
                center = default(Vector3);
                return false;
            }
            cell = logical.WorldToCell(ray.GetPoint(distance));
            center = logical.GetCellCenterWorld(cell);
            return true;
        }

        private UnityEngine.Object[] AuthoringObjects()
        {
            return new UnityEngine.Object[]
            {
                target,
                target.BaseLogicalTilemap,
                target.LandformLogicalTilemap,
                target.EdgeLogicalTilemap,
                target.BaseOutputTilemap,
                target.LandformAOutputTilemap,
                target.LandformBOutputTilemap,
                target.EdgeAOnBOutputTilemap,
                target.EdgeBOnAOutputTilemap,
            };
        }

        private static void MarkDirty(LayeredTerrainTilemap renderer)
        {
            EditorUtility.SetDirty(renderer);
            foreach (var tilemap in new Tilemap[]
                     {
                         renderer.BaseLogicalTilemap, renderer.LandformLogicalTilemap,
                         renderer.EdgeLogicalTilemap, renderer.BaseOutputTilemap,
                         renderer.LandformAOutputTilemap, renderer.LandformBOutputTilemap,
                         renderer.EdgeAOnBOutputTilemap, renderer.EdgeBOnAOutputTilemap,
                     })
                if (tilemap != null) EditorUtility.SetDirty(tilemap);
            if (renderer.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        }

        private void NotifyChanged()
        {
            if (Changed != null) Changed();
        }
    }
}
