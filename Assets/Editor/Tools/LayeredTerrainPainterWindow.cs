using System.Collections.Generic;
using System.Linq;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    internal sealed class LayeredTerrainResourceAcceptanceOverlay : IMGUIOverlay
    {
        internal const string Title = "地貌素材实验室 · 资源验收";

        private static readonly Vector2 MinimumPanelSize = new Vector2(280f, 220f);
        private static readonly Vector2 PreferredPanelSize = new Vector2(340f, 320f);
        private static readonly Vector2 MaximumPanelSize = new Vector2(420f, 640f);

        public LayeredTerrainResourceAcceptanceOverlay()
        {
            displayName = Title;
            minSize = MinimumPanelSize;
            defaultSize = PreferredPanelSize;
            maxSize = MaximumPanelSize;
            size = PreferredPanelSize;
            collapsed = false;
        }

        public override void OnCreated()
        {
            base.OnCreated();
            displayedChanged += OnDisplayedChanged;
            collapsedChanged += OnCollapsedChanged;
        }

        public override void OnWillBeDestroyed()
        {
            displayedChanged -= OnDisplayedChanged;
            collapsedChanged -= OnCollapsedChanged;
            LayeredTerrainSceneLaboratory.OnOverlayDestroyed(this);
            base.OnWillBeDestroyed();
        }

        public override void OnGUI()
        {
            LayeredTerrainSceneLaboratory.DrawOverlayContents();
        }

        private void OnDisplayedChanged(bool value)
        {
            if (!value) LayeredTerrainSceneLaboratory.OnOverlayHidden(this);
        }

        private void OnCollapsedChanged(bool value)
        {
            if (value) LayeredTerrainSceneLaboratory.OnOverlayCollapsed(this);
        }
    }

    // Kept as the stable launch API for existing Inspector and acceptance callers.
    // The ordinary resource-acceptance workflow now lives in a native Scene Overlay.
    public static class LayeredTerrainPainterWindow
    {
        [MenuItem("Fruit Defense/地图工具/地貌素材实验室")]
        public static void Open()
        {
            Open(null);
        }

        public static void Open(LayeredTerrainTilemap preferredTarget)
        {
            LayeredTerrainSceneLaboratory.Open(preferredTarget);
        }

        public static LayeredTerrainTilemap ResolveInitialTarget(
            LayeredTerrainTilemap selected, IReadOnlyList<LayeredTerrainTilemap> candidates)
        {
            if (selected != null && candidates != null && candidates.Contains(selected)
                && HasValidTerrainConfiguration(selected)) return selected;
            if (candidates == null) return null;
            LayeredTerrainTilemap sole = null;
            var count = 0;
            foreach (var candidate in candidates)
            {
                if (!HasValidTerrainConfiguration(candidate)) continue;
                sole = candidate;
                count++;
                if (count > 1) return null;
            }
            return count == 1 ? sole : null;
        }

        internal static void PrepareAcceptanceView()
        {
            LayeredTerrainSceneLaboratory.PrepareAcceptanceView();
        }

        private static bool HasValidTerrainConfiguration(LayeredTerrainTilemap value)
        {
            string ignored;
            return value != null && value.ValidateAuthoringConfiguration(out ignored);
        }
    }

    internal static class LayeredTerrainSceneLaboratory
    {
        internal const float RegisteredBrushCardHeight = 86f;
        internal const float RegisteredBrushGap = 4f;
        internal const int RegisteredBrushColumnCount = 4;
        internal const string ResourceBoundaryMessage =
            "这里只验收地貌资源与拼接效果，不生成可玩地图；选择其他图块会保留格子并整体切换地貌；正式关卡请使用“关卡地图编辑器”。";

        private static LayeredTerrainPaintSession session;
        private static LayeredTerrainTilemap[] targets = new LayeredTerrainTilemap[0];
        private static SceneView hostSceneView;
        private static LayeredTerrainResourceAcceptanceOverlay overlay;
        private static Vector2 scroll;
        private static bool open;
        private static bool overlayAttached;
        private static bool overlayAttaching;
        private static bool overlayExpansionQueued;
        private static bool advancedExpanded;
        private static bool subscribed;
        private static string activeRegisteredBrushId = string.Empty;
        private static string registeredBrushMessage = string.Empty;

        internal static bool IsOpen { get { return open; } }
        internal static bool IsCollapsed { get { return overlay != null && overlay.collapsed; } }
        internal static bool IsPainting { get { return session != null && session.IsActive; } }
        internal static bool HasNativeOverlay { get { return overlay != null; } }
        internal static int NativeOverlayInstanceCount { get { return overlay == null ? 0 : 1; } }
        internal static LayeredTerrainResourceAcceptanceOverlay ActiveOverlay { get { return overlay; } }
        internal static string ActivePaintChoiceId { get { return activeRegisteredBrushId; } }
        internal static LayeredTerrainTilemap Target
        {
            get { return session == null ? null : session.Target; }
        }
        internal static void Open(LayeredTerrainTilemap preferredTarget)
        {
            EnsureSession();
            open = true;
            Subscribe();
            ResolveHostSceneView();
            EnsureOverlay();
            EnsureOverlayExpanded();
            RefreshTargets(true, preferredTarget);
            SyncActivePaintChoice();
            if (session.Target != null)
            {
                Selection.activeGameObject = session.Target.gameObject;
                EditorGUIUtility.PingObject(session.Target.gameObject);
                if (hostSceneView != null) hostSceneView.FrameSelected();
            }
            if (hostSceneView != null)
            {
                hostSceneView.Focus();
                AttachOverlay();
                RequestOverlayExpanded();
            }
            RepaintAll();
        }

        internal static void Close()
        {
            var existingOverlay = overlay;
            overlay = null;
            if (overlayAttached && existingOverlay != null && !Application.isBatchMode)
                SceneView.RemoveOverlayFromActiveView(existingOverlay);
            overlayAttached = false;
            EditorApplication.update -= CompleteOverlayAttachment;
            overlayAttaching = false;
            EditorApplication.delayCall -= EnsureOverlayExpanded;
            overlayExpansionQueued = false;
            TeardownSession();
        }

        private static void TeardownSession()
        {
            EditorApplication.update -= CompleteOverlayAttachment;
            overlayAttaching = false;
            if (session != null)
            {
                session.Changed -= RepaintAll;
                session.Dispose();
                session = null;
            }
            open = false;
            advancedExpanded = false;
            activeRegisteredBrushId = string.Empty;
            registeredBrushMessage = string.Empty;
            scroll = Vector2.zero;
            targets = new LayeredTerrainTilemap[0];
            hostSceneView = null;
            Unsubscribe();
            RepaintAll();
        }

        internal static void SetCollapsed(bool value)
        {
            if (overlay == null) return;
            if (value && open)
            {
                EnsureOverlayExpanded();
                RepaintAll();
                return;
            }
            overlay.collapsed = value;
            RepaintAll();
        }

        internal static void PrepareAcceptanceView()
        {
            EnsureSession();
            open = true;
            advancedExpanded = true;
            Subscribe();
            ResolveHostSceneView();
            EnsureOverlay();
            EnsureOverlayExpanded();
            SetCollapsed(false);
            AttachOverlay();
            RequestOverlayExpanded();
            session.SetTool(LayeredTerrainPainterTool.AOnB);
            SyncActivePaintChoice();
            var reason = "no valid terrain target is selected";
            if (session.Target == null || !session.Start(out reason))
                throw new System.InvalidOperationException(
                    "Terrain laboratory acceptance could not start painting: " + reason);
            RepaintAll();
        }

        private static void EnsureSession()
        {
            if (session != null) return;
            session = new LayeredTerrainPaintSession();
            session.Changed += RepaintAll;
        }

        private static void EnsureOverlay()
        {
            if (overlay == null) overlay = new LayeredTerrainResourceAcceptanceOverlay();
            overlay.displayed = true;
        }

        private static void AttachOverlay()
        {
            if (overlayAttached || overlay == null || hostSceneView == null
                || Application.isBatchMode) return;
            hostSceneView.Focus();
            overlayAttaching = true;
            EditorApplication.update -= CompleteOverlayAttachment;
            EditorApplication.update += CompleteOverlayAttachment;
            try
            {
                SceneView.AddOverlayToActiveView(overlay);
                overlayAttached = true;
            }
            catch
            {
                EditorApplication.update -= CompleteOverlayAttachment;
                overlayAttaching = false;
                throw;
            }
            RequestOverlayExpanded();
        }

        private static void CompleteOverlayAttachment()
        {
            EditorApplication.update -= CompleteOverlayAttachment;
            if (open && overlay != null) EnsureOverlayExpanded();
            overlayAttaching = false;
        }

        internal static void OnOverlayHidden(LayeredTerrainResourceAcceptanceOverlay value)
        {
            if (overlay != value) return;
            if (overlayAttaching && open)
            {
                EnsureOverlayExpanded();
                RequestOverlayExpanded();
                return;
            }
            TeardownSession();
        }

        internal static void OnOverlayCollapsed(
            LayeredTerrainResourceAcceptanceOverlay value)
        {
            if (overlay != value || !open) return;
            EnsureOverlayExpanded();
            RequestOverlayExpanded();
        }

        internal static void OnOverlayDestroyed(LayeredTerrainResourceAcceptanceOverlay value)
        {
            if (overlay != value) return;
            overlay = null;
            overlayAttached = false;
            TeardownSession();
        }

        private static void Subscribe()
        {
            if (subscribed) return;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            EditorApplication.quitting += Close;
            subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!subscribed) return;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            EditorApplication.quitting -= Close;
            subscribed = false;
        }

        private static void ResolveHostSceneView()
        {
            hostSceneView = SceneView.lastActiveSceneView;
            if (hostSceneView == null && !Application.isBatchMode)
                hostSceneView = EditorWindow.GetWindow<SceneView>();
        }

        private static void OnSelectionChanged()
        {
            if (!open || session == null || session.IsActive) return;
            RefreshTargets(false, SelectedTerrain());
        }

        private static void OnHierarchyChanged()
        {
            if (!open || session == null) return;
            RefreshTargets(false, null);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode && session != null)
                session.Stop();
        }

        internal static void DrawOverlayContents()
        {
            if (!open || session == null)
            {
                EditorGUILayout.HelpBox("请从“Fruit Defense/地图工具/地貌素材实验室”重新开始资源验收。",
                    MessageType.Info);
                return;
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(ResourceBoundaryMessage, MessageType.None);
            DrawTargetPicker();
            var target = session.Target;
            if (target == null)
            {
                EditorGUILayout.HelpBox(targets.Length > 1
                        ? "场景中有多个可测试地貌目标，请先明确选择一个。"
                        : "当前场景中没有可用的分层地貌实验目标。",
                    MessageType.Info);
                if (GUILayout.Button("重新扫描场景")) RefreshTargets(true, null);
                EditorGUILayout.EndScrollView();
                return;
            }

            string reason;
            var terrainValid = target.ValidateAuthoringConfiguration(out reason);
            EditorGUILayout.HelpBox(terrainValid ? "实验目标结构有效。" : reason,
                terrainValid ? MessageType.Info : MessageType.Error);
            if (!terrainValid)
            {
                EditorGUILayout.EndScrollView();
                return;
            }
            var presentationValid = target.ValidateAuthoringPresentation(out reason);
            DrawPaintChoices(target);
            if (!presentationValid)
            {
                EditorGUILayout.HelpBox(reason + " 请在地图对象 Inspector 的“开发者配置”中补齐。",
                    MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("当前轮廓", ContourDisplayName(target.ActiveContourStyleId));

            DrawActiveSummary();
            DrawPaintingToggle();
            DrawAdvancedTools();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPaintChoices(LayeredTerrainTilemap target)
        {
            var choices = RegisteredPaintChoices();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("可绘制地貌图块", EditorStyles.boldLabel);
            if (choices.Count == 0)
            {
                EditorGUILayout.HelpBox("没有已注册的可绘制地貌资源。", MessageType.Warning);
                return;
            }
            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ProjectSetup.BattlefieldTerrainPalettePath);
            var gridHeight = CalculatePreviewGridHeight(choices.Count,
                RegisteredBrushColumnCount, RegisteredBrushCardHeight,
                RegisteredBrushGap);
            var gridRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(gridHeight), GUILayout.ExpandWidth(true));
            var cardBounds = new Rect(gridRect.x, gridRect.y, gridRect.width,
                RegisteredBrushCardHeight);
            var cardRects = CalculateBrushPreviewRects(cardBounds, choices.Count,
                RegisteredBrushColumnCount, RegisteredBrushGap);
            for (var index = 0; index < choices.Count; index++)
                DrawPaintChoiceCard(cardRects[index], choices[index], target, palette);
            if (target.HasAuthoredCells() && GUILayout.Button("清空实验画布"))
            {
                session.Stop();
                TerrainBrushLaboratoryRegistration.TryClear(target,
                    out registeredBrushMessage);
                RepaintAll();
            }
            if (!string.IsNullOrEmpty(registeredBrushMessage))
                EditorGUILayout.HelpBox(registeredBrushMessage,
                    registeredBrushMessage.StartsWith("已", System.StringComparison.Ordinal)
                        ? MessageType.Info : MessageType.Warning);
        }

        internal static IReadOnlyList<TerrainBrushPaintChoice> RegisteredPaintChoices()
        {
            return TerrainBrushRegistry.FindPaintChoices();
        }

        internal static bool TrySelectPaintChoice(TerrainBrushPaintChoice choice,
            LayeredTerrainTilemap target, BattlefieldTerrainPalette palette,
            out string reason)
        {
            EnsureSession();
            if (session.Target != target) session.SetTarget(target);
            session.Stop();
            if (!TerrainBrushRegistry.IsPaintChoiceAvailable(choice, palette,
                    out reason)
                || !TerrainBrushLaboratoryRegistration.TryApply(choice.Definition,
                    target, palette, out reason)) return false;
            session.SetPureBaseOnly(false);
            session.SetTool(choice.Tool);
            if (!session.Start(out reason)) return false;
            activeRegisteredBrushId = choice.ChoiceId;
            EnsureOverlayExpanded();
            RequestOverlayExpanded();
            reason = "已选择“" + choice.DisplayName + "”，可直接在 Scene 中绘制。";
            registeredBrushMessage = reason;
            RepaintAll();
            return true;
        }

        private static void DrawPaintChoiceCard(Rect rect, TerrainBrushPaintChoice choice,
            LayeredTerrainTilemap target, BattlefieldTerrainPalette palette)
        {
            var available = TerrainBrushRegistry.IsPaintChoiceAvailable(choice, palette,
                out var unavailableReason);
            using (new EditorGUI.DisabledScope(!available))
                if (GUI.Button(rect, GUIContent.none, GUI.skin.button))
                    TrySelectPaintChoice(choice, target, palette,
                        out registeredBrushMessage);

            var artworkBounds = new Rect(rect.x + 3f, rect.y + 3f,
                Mathf.Max(1f, rect.width - 6f), Mathf.Max(1f, rect.height - 43f));
            var previewRect = CalculateCenteredSquareRect(artworkBounds);
            DrawPaintChoiceArtwork(previewRect, choice, palette);
            var footerRect = new Rect(rect.x + 3f, rect.yMax - 37f,
                Mathf.Max(1f, rect.width - 6f), 34f);
            EditorGUI.DrawRect(footerRect, new Color(0f, 0f, 0f, .7f));
            var footerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = true,
            };
            footerStyle.normal.textColor = Color.white;
            GUI.Label(footerRect, choice.DisplayName, footerStyle);
            if (!available)
                EditorGUI.DrawRect(previewRect, new Color(.25f, .05f, .03f, .42f));
            if (string.Equals(activeRegisteredBrushId, choice.ChoiceId,
                    System.StringComparison.Ordinal))
                DrawCardBorder(rect, new Color(.2f, .9f, 1f, 1f), 2f);
            GUI.Label(rect, new GUIContent(string.Empty, available
                ? "选择后立即进入 Scene 绘制：" + choice.DisplayName
                : unavailableReason), GUIStyle.none);
        }

        private static void DrawPaintChoiceArtwork(Rect rect,
            TerrainBrushPaintChoice choice, BattlefieldTerrainPalette palette)
        {
            if (choice == null || !TerrainBrushRegistry.TryResolveLaboratoryLandforms(
                    choice.Definition, palette, out var foregroundLandform,
                    out var backgroundLandform, out _))
            {
                EditorGUI.DrawRect(rect, Color.gray);
                return;
            }
            var definition = choice.Definition;
            var selectedLandform = choice.Reverse
                ? backgroundLandform : foregroundLandform;
            var baseTile = (choice.Reverse
                ? definition.ForegroundBaseTile : definition.BackgroundBaseTile) as Tile;
            var baseSprite = baseTile == null ? null : baseTile.sprite;
            var masks = new[]
            {
                DualGridMask.SouthEast, DualGridMask.SouthWest,
                DualGridMask.NorthEast, DualGridMask.NorthWest,
            };
            var halfWidth = rect.width * .5f;
            var halfHeight = rect.height * .5f;
            for (var quadrant = 0; quadrant < masks.Length; quadrant++)
            {
                var quadrantRect = new Rect(rect.x + quadrant % 2 * halfWidth,
                    rect.y + quadrant / 2 * halfHeight, halfWidth, halfHeight);
                DrawSprite(quadrantRect, baseSprite);
                DrawTileSetSprite(quadrantRect, selectedLandform, masks[quadrant]);
                DrawTileSetSprite(quadrantRect, definition.CompositeTileSet,
                    choice.Reverse
                        ? DualGridMaskUtility.Complement(masks[quadrant])
                        : masks[quadrant]);
            }
        }

        internal static IReadOnlyList<TerrainBrushDefinition> RegisteredBrushes()
        {
            return TerrainBrushRegistry.FindAll();
        }

        internal static float CalculatePreviewGridHeight(int itemCount, int columns,
            float cardHeight, float gap)
        {
            if (itemCount <= 0 || columns <= 0 || cardHeight <= 0f) return 0f;
            var rows = Mathf.CeilToInt(itemCount / (float)columns);
            return rows * cardHeight + Mathf.Max(0, rows - 1) * Mathf.Max(0f, gap);
        }

        internal static Rect CalculateCenteredSquareRect(Rect bounds)
        {
            var size = Mathf.Max(1f, Mathf.Min(bounds.width, bounds.height));
            return new Rect(bounds.x + (bounds.width - size) * .5f,
                bounds.y + (bounds.height - size) * .5f, size, size);
        }

        private static void DrawTargetPicker()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("当前实验目标", EditorStyles.boldLabel);
            if (targets.Length == 0)
            {
                EditorGUILayout.LabelField("未找到分层地貌实验目标");
                return;
            }

            var names = new string[targets.Length + 1];
            names[0] = "— 请选择地图 —";
            var currentIndex = 0;
            for (var index = 0; index < targets.Length; index++)
            {
                names[index + 1] = targets[index] == null
                    ? "缺失对象" : targets[index].gameObject.name;
                if (targets[index] == session.Target) currentIndex = index + 1;
            }
            var next = EditorGUILayout.Popup(currentIndex, names);
            if (next != currentIndex) SetTarget(next == 0 ? null : targets[next - 1]);
        }

        internal static Rect[] CalculateBrushPreviewRects(Rect bounds, int itemCount,
            int columns, float gap)
        {
            if (itemCount <= 0 || columns <= 0) return new Rect[0];
            var columnCount = Mathf.Min(itemCount, columns);
            var available = Mathf.Max(1f, bounds.width - gap * (columnCount - 1));
            var width = Mathf.Max(1f, available / columnCount);
            var height = Mathf.Max(1f, bounds.height);
            var rects = new Rect[itemCount];
            for (var index = 0; index < itemCount; index++)
            {
                var column = index % columns;
                var row = index / columns;
                rects[index] = new Rect(bounds.x + column * (width + gap),
                    bounds.y + row * (height + gap), width, height);
            }
            return rects;
        }

        internal static string ContourDisplayName(string contourStyleId)
        {
            if (string.Equals(contourStyleId, BattlefieldLayerIds.ContourStyles.Square,
                    System.StringComparison.Ordinal)) return "方形";
            if (string.Equals(contourStyleId, BattlefieldLayerIds.ContourStyles.Organic,
                    System.StringComparison.Ordinal)) return "自然";
            return string.IsNullOrWhiteSpace(contourStyleId) ? "未配置" : contourStyleId;
        }

        private static void DrawTileSetSprite(Rect rect, DualGridTileSet tileSet,
            DualGridMask mask)
        {
            if (tileSet != null && tileSet.TryGetSprite(mask, out var sprite))
                DrawSprite(rect, sprite);
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            var textureRect = sprite.textureRect;
            var texture = sprite.texture;
            var uv = new Rect(textureRect.x / texture.width, textureRect.y / texture.height,
                textureRect.width / texture.width, textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        }

        private static void DrawCardBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawActiveSummary()
        {
            EditorGUILayout.Space(7f);
            EditorGUILayout.HelpBox("当前笔刷：" + session.ActiveToolLabel,
                MessageType.Info);
        }

        private static void DrawPaintingToggle()
        {
            string reason;
            var canStart = session.ValidateActiveTool(out reason);
            if (session.IsActive)
            {
                var previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, .55f, .35f, 1f);
                if (GUILayout.Button("停止绘制", GUILayout.Height(34f))) session.Stop();
                GUI.backgroundColor = previous;
                EditorGUILayout.HelpBox("左键拖动绘制，Esc 停止，Ctrl+Z 撤销整次拖动。收起面板不会停止笔刷。",
                    MessageType.Info);
                return;
            }
            using (new EditorGUI.DisabledScope(!canStart))
                if (GUILayout.Button("开始绘制", GUILayout.Height(34f)))
                    session.Start(out reason);
            if (!canStart) EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }

        private static void DrawAdvancedTools()
        {
            EditorGUILayout.Space(10f);
            advancedExpanded = EditorGUILayout.Foldout(advancedExpanded,
                "高级分层操作", true);
            if (!advancedExpanded) return;
            EditorGUILayout.HelpBox("擦除地貌会保留底图；清空格子会全部移除。",
                MessageType.None);
            EditorGUILayout.BeginHorizontal();
            DrawAdvancedButton(LayeredTerrainPainterTool.EraseLandform,
                "擦除地貌\n保留底图");
            DrawAdvancedButton(LayeredTerrainPainterTool.ClearCell,
                "清空格子\n全部移除");
            EditorGUILayout.EndHorizontal();
            if (session.Tool == LayeredTerrainPainterTool.LandformA
                || session.Tool == LayeredTerrainPainterTool.LandformB)
                EditorGUILayout.HelpBox("空白格没有底图时不会写入地貌；可勾选“只绘制纯图”先铺底。",
                    MessageType.Info);
        }

        private static void DrawAdvancedButton(LayeredTerrainPainterTool value, string label)
        {
            var previous = GUI.backgroundColor;
            if (session.Tool == value)
                GUI.backgroundColor = new Color(.35f, .82f, 1f, 1f);
            string reason;
            var available = session.CanUseTool(value, out reason);
            using (new EditorGUI.DisabledScope(!available))
                if (GUILayout.Button(label, GUILayout.Height(44f), GUILayout.ExpandWidth(true)))
                    session.SetTool(value);
            GUI.backgroundColor = previous;
        }

        private static void RefreshTargets(bool allowAutomaticSelection,
            LayeredTerrainTilemap preferredTarget)
        {
            EnsureSession();
            targets = Object.FindObjectsByType<LayeredTerrainTilemap>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.scene.IsValid())
                .OrderBy(value => value.gameObject.scene.path)
                .ThenBy(value => value.gameObject.name)
                .ToArray();
            if (preferredTarget != null && targets.Contains(preferredTarget)
                && HasValidTerrainConfiguration(preferredTarget))
            {
                SetTarget(preferredTarget);
                return;
            }
            if (session.Target != null && targets.Contains(session.Target)) return;
            var initial = allowAutomaticSelection
                ? LayeredTerrainPainterWindow.ResolveInitialTarget(SelectedTerrain(), targets)
                : null;
            session.SetTarget(initial);
        }

        private static void SetTarget(LayeredTerrainTilemap value)
        {
            EnsureSession();
            session.SetTarget(value);
            activeRegisteredBrushId = string.Empty;
            registeredBrushMessage = string.Empty;
            if (value != null)
            {
                Selection.activeGameObject = value.gameObject;
                EditorGUIUtility.PingObject(value.gameObject);
            }
            SyncActivePaintChoice();
            RepaintAll();
        }

        private static void SyncActivePaintChoice()
        {
            activeRegisteredBrushId = string.Empty;
            if (session == null || session.Target == null) return;
            var palette = AssetDatabase.LoadAssetAtPath<BattlefieldTerrainPalette>(
                ProjectSetup.BattlefieldTerrainPalettePath);
            var definition = RegisteredBrushes().FirstOrDefault(value =>
                TerrainBrushLaboratoryRegistration.Matches(value, session.Target, palette));
            if (definition == null) return;
            var reverse = session.Tool == LayeredTerrainPainterTool.BOnA;
            activeRegisteredBrushId = definition.BrushId
                + (reverse ? ".reverse" : ".forward");
        }

        private static LayeredTerrainTilemap SelectedTerrain()
        {
            var selected = Selection.activeGameObject;
            return selected == null ? null : selected.GetComponentInParent<LayeredTerrainTilemap>();
        }

        private static bool HasValidTerrainConfiguration(LayeredTerrainTilemap value)
        {
            string ignored;
            return value != null && value.ValidateAuthoringConfiguration(out ignored);
        }

        private static void RepaintAll()
        {
            if (open) RequestOverlayExpanded();
            SceneView.RepaintAll();
        }

        private static void RequestOverlayExpanded()
        {
            if (!open || overlay == null || overlayExpansionQueued) return;
            overlayExpansionQueued = true;
            EditorApplication.delayCall += EnsureOverlayExpanded;
        }

        private static void EnsureOverlayExpanded()
        {
            EditorApplication.delayCall -= EnsureOverlayExpanded;
            overlayExpansionQueued = false;
            if (!open || overlay == null) return;
            if (overlay.containerWindow != null && overlay.isInToolbar) overlay.Undock();
            overlay.displayed = true;
            overlay.collapsed = false;
            if (overlay.size.x < overlay.minSize.x || overlay.size.y < overlay.minSize.y)
                overlay.size = overlay.defaultSize;
        }
    }
}
