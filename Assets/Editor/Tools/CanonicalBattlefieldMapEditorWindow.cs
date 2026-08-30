using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitDefense.Editor
{
    [FilePath("UserSettings/CanonicalBattlefieldMapEditorState.asset",
        FilePathAttribute.Location.ProjectFolder)]
    public sealed class CanonicalBattlefieldMapEditorState : ScriptableSingleton<
        CanonicalBattlefieldMapEditorState>
    {
        [SerializeField] internal string mapGuid = string.Empty;
        [SerializeField] internal string manifestGuid = string.Empty;
        [SerializeField] internal CanonicalBattlefieldMapWorkspace workspace;
        [SerializeField] internal CanonicalBattlefieldMapTool tool;
        [SerializeField] internal float zoom = 1f;
        [SerializeField] internal Vector2 scroll;
        [SerializeField] internal Vector2Int selectedCell = new Vector2Int(-1, -1);

        internal void Persist()
        {
            Save(true);
        }
    }

    public enum CanonicalBattlefieldRouteTool
    {
        AppendRoute,
        PlaceCore,
        PlaceInitialPot,
    }

    public static class CanonicalBattlefieldMapPlaytest
    {
        public static bool TryPrepare(BattlefieldMapPublicationManifest manifest,
            string levelId, out string reason)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(levelId))
            {
                reason = "A manifest and published level identity are required.";
                return false;
            }
            var result = BattlefieldMapPublicationExporter.Rebuild(manifest);
            if (!result.Succeeded)
            {
                reason = result.Diagnostics.Count == 0
                    ? "Publication failed." : result.Diagnostics[0].ToString();
                return false;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PublishedBattlefieldMapCatalog reloaded;
            if (!BattlefieldMapPublicationExporter.TryReloadGeneratedResource(
                    out reloaded, out reason)) return false;

            CompiledLevelCatalog catalog;
            LevelCatalogValidationResult levelValidation;
            ContentValidationResult contentValidation;
            if (!BundledLevelCatalogFactory.TryCompile(out catalog,
                    out levelValidation, out contentValidation))
            {
                reason = levelValidation != null && levelValidation.Issues.Count > 0
                    ? levelValidation.Issues[0].ToString()
                    : contentValidation != null && contentValidation.Issues.Count > 0
                        ? contentValidation.Issues[0].ToString()
                        : "Generated catalog did not compile after reload.";
                return false;
            }
            ResolvedLevelDefinition resolved;
            LevelResolutionError error;
            if (!catalog.TryResolve(levelId, out resolved, out error))
            {
                reason = error == null ? "Published level did not resolve after reload."
                    : error.ToString();
                return false;
            }
            PublishedBattlefieldPlaytestRequest.Set(levelId);
            reason = "ok";
            return true;
        }

        public static bool TryLaunch(BattlefieldMapPublicationManifest manifest,
            string levelId, out string reason)
        {
            if (!TryPrepare(manifest, levelId, out reason)) return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                PublishedBattlefieldPlaytestRequest.Clear();
                reason = "Playtest was cancelled while saving open scenes.";
                return false;
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");
            EditorApplication.EnterPlaymode();
            reason = "ok";
            return true;
        }
    }

    public sealed class CanonicalBattlefieldMapEditorWindow : EditorWindow
    {
        private static readonly string[] SurfaceIds =
        {
            BattlefieldLayerIds.Surfaces.Soil,
            BattlefieldLayerIds.Surfaces.Grass,
            BattlefieldLayerIds.Surfaces.StoneRoad,
            BattlefieldLayerIds.Surfaces.Water,
        };

        private BattlefieldMapAuthoringAsset map;
        private BattlefieldMapPublicationManifest manifest;
        private Vector2Int hoveredCell = new Vector2Int(-1, -1);
        private Vector2Int rectangleStart = new Vector2Int(-1, -1);
        private bool gestureActive;
        private int undoGroup = -1;
        private readonly HashSet<Vector2Int> gestureCells = new HashSet<Vector2Int>();
        private string lastOperation = "请选择或创建地图资产。";
        private string createMapId = "map.authored-01";
        private int createWidth = 8;
        private int createHeight = 7;
        private int resizeWidth = 8;
        private int resizeHeight = 7;
        private BattlefieldCellCapabilities gameplayCapabilities;
        private BattlefieldCollisionChannels gameplayCollisions;
        private CanonicalBattlefieldRouteTool routeTool;
        private string markerGroupId = "group.initial-pots";
        private int markerSelectionCount = 1;
        private string baseSurfaceId = BattlefieldLayerIds.Surfaces.Soil;
        private string landformSurfaceId = string.Empty;
        private string contourStyleId = BattlefieldLayerIds.ContourStyles.Square;
        private string edgeStyleId = string.Empty;
        private IReadOnlyList<BattlefieldMapAuthoringDiagnostic> authoringDiagnostics =
            Array.Empty<BattlefieldMapAuthoringDiagnostic>();
        private IReadOnlyList<BattlefieldMapPublicationDiagnostic> publicationDiagnostics =
            Array.Empty<BattlefieldMapPublicationDiagnostic>();
        private BattlefieldTerrainPalette previewPalette;
        private bool publishReady;
        private string publishLevelId = string.Empty;
        private int manifestOrder;
        private string manifestLevelId = "level.authored-01";
        private string manifestTemplateLevelId = BundledLevelCatalogIds.Levels.Orchard01;
        private bool acceptanceCompositeView;

        internal bool IsPublishReady { get { return publishReady; } }
        internal IReadOnlyList<BattlefieldMapPublicationDiagnostic> PublicationDiagnostics
        {
            get { return publicationDiagnostics; }
        }

        [MenuItem("Fruit Defense/地图工具/关卡地图编辑器", priority = 10)]
        public static void Open()
        {
            var window = GetWindow<CanonicalBattlefieldMapEditorWindow>();
            window.titleContent = new GUIContent("关卡地图编辑器");
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }

        public static void Open(BattlefieldMapAuthoringAsset target)
        {
            Open();
            var window = GetWindow<CanonicalBattlefieldMapEditorWindow>();
            window.SetMap(target);
        }

        internal void PrepareAcceptanceView(BattlefieldMapAuthoringAsset target,
            BattlefieldMapPublicationManifest selectedManifest)
        {
            manifest = selectedManifest;
            CanonicalBattlefieldMapEditorState.instance.manifestGuid = GuidFor(manifest);
            SetMap(target);
            var state = CanonicalBattlefieldMapEditorState.instance;
            state.workspace = CanonicalBattlefieldMapWorkspace.RouteAndMarkers;
            state.tool = CanonicalBattlefieldMapTool.SingleCell;
            state.zoom = 1.15f;
            state.scroll = Vector2.zero;
            state.selectedCell = target == null ? new Vector2Int(-1, -1)
                : new Vector2Int(Mathf.Max(0, target.GridWidth / 2),
                    Mathf.Max(0, target.GridHeight / 2));
            hoveredCell = state.selectedCell;
            acceptanceCompositeView = true;
            state.Persist();
            LoadManifestBinding();
            RefreshDiagnostics(true);
            Repaint();
        }

        internal bool PlaytestPublishedLevel(out string reason)
        {
            if (!publishReady || manifest == null || string.IsNullOrWhiteSpace(publishLevelId))
            {
                reason = "Current map is not publish-ready in its manifest entry.";
                return false;
            }
            return CanonicalBattlefieldMapPlaytest.TryLaunch(manifest,
                publishLevelId, out reason);
        }

        [OnOpenAsset]
        public static bool OpenMapAsset(int instanceId, int line)
        {
            var target = AssetDatabase.LoadAssetAtPath<BattlefieldMapAuthoringAsset>(
                AssetDatabase.GetAssetPath(instanceId));
            if (target == null) return false;
            Open(target);
            return true;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("关卡地图编辑器");
            Undo.undoRedoPerformed += HandleUndoRedo;
            map = LoadGuid<BattlefieldMapAuthoringAsset>(
                CanonicalBattlefieldMapEditorState.instance.mapGuid);
            manifest = LoadGuid<BattlefieldMapPublicationManifest>(
                CanonicalBattlefieldMapEditorState.instance.manifestGuid);
            RefreshDiagnostics(false);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            CanonicalBattlefieldMapEditorState.instance.Persist();
        }

        private void OnGUI()
        {
            DrawHeader();
            if (map == null)
            {
                DrawEmptyState();
                return;
            }

            DrawStatusStrip();
            EditorGUILayout.BeginHorizontal();
            DrawToolbox();
            DrawCanvas();
            EditorGUILayout.EndHorizontal();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("关卡地图编辑器", EditorStyles.boldLabel,
                GUILayout.Width(120f));
            var nextMap = EditorGUILayout.ObjectField(map,
                typeof(BattlefieldMapAuthoringAsset), false)
                as BattlefieldMapAuthoringAsset;
            if (nextMap != map) SetMap(nextMap);
            var nextManifest = EditorGUILayout.ObjectField(manifest,
                typeof(BattlefieldMapPublicationManifest), false,
                GUILayout.Width(230f)) as BattlefieldMapPublicationManifest;
            if (nextManifest != manifest)
            {
                manifest = nextManifest;
                CanonicalBattlefieldMapEditorState.instance.manifestGuid = GuidFor(manifest);
                CanonicalBattlefieldMapEditorState.instance.Persist();
                LoadManifestBinding();
                RefreshDiagnostics(true);
            }
            if (GUILayout.Button("新建地图", EditorStyles.toolbarButton,
                    GUILayout.Width(72f))) CreateMapAsset();
            if (GUILayout.Button("新建清单", EditorStyles.toolbarButton,
                    GUILayout.Width(72f))) CreateManifestAsset();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.Space(20f);
            EditorGUILayout.HelpBox(
                "正式关卡从一个有固定宽高的地图资产开始。新资产会自动铺满土壤表现格和空玩法格，画布外永远不能落笔。",
                MessageType.Info);
            createMapId = EditorGUILayout.TextField("地图 ID", createMapId);
            createWidth = EditorGUILayout.IntField("宽度", createWidth);
            createHeight = EditorGUILayout.IntField("高度", createHeight);
            GUI.enabled = createWidth > 0 && createHeight > 0
                && !string.IsNullOrWhiteSpace(createMapId);
            if (GUILayout.Button("创建有界地图资产", GUILayout.Height(34f))) CreateMapAsset();
            GUI.enabled = true;
        }

        private void DrawStatusStrip()
        {
            var state = CanonicalBattlefieldMapEditorState.instance;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Map: " + map.MapId, GUILayout.Width(210f));
            EditorGUILayout.LabelField("尺寸: " + map.GridWidth + " × " + map.GridHeight,
                GUILayout.Width(100f));
            EditorGUILayout.LabelField("悬停: " + CoordinateLabel(hoveredCell),
                GUILayout.Width(110f));
            EditorGUILayout.LabelField("工具: " + state.tool, GUILayout.Width(130f));
            var blocking = authoringDiagnostics.Count(value => value.IsBlocking)
                + publicationDiagnostics.Count(value => value.IsBlocking);
            EditorGUILayout.LabelField("诊断: " + blocking + " 错误 / "
                + (authoringDiagnostics.Count + publicationDiagnostics.Count) + " 总计",
                GUILayout.Width(165f));
            EditorGUILayout.LabelField(EditorUtility.IsDirty(map) ? "未保存" : "已保存",
                GUILayout.Width(58f));
            var previous = GUI.color;
            GUI.color = publishReady ? new Color(.55f, 1f, .55f) : new Color(1f, .6f, .55f);
            EditorGUILayout.LabelField(publishReady ? "可发布" : "草稿/阻塞",
                EditorStyles.boldLabel);
            GUI.color = previous;
            if (acceptanceCompositeView)
                EditorGUILayout.LabelField("验收综合叠加", EditorStyles.miniBoldLabel,
                    GUILayout.Width(84f));
            EditorGUILayout.EndHorizontal();

            var nextWorkspace = (CanonicalBattlefieldMapWorkspace)GUILayout.Toolbar(
                (int)state.workspace, new[] { "玩法格", "路线与点位", "地貌表现", "校验发布" });
            if (nextWorkspace != state.workspace)
            {
                state.workspace = nextWorkspace;
                acceptanceCompositeView = false;
                state.Persist();
            }
        }

        private void DrawToolbox()
        {
            var state = CanonicalBattlefieldMapEditorState.instance;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(292f),
                GUILayout.ExpandHeight(true));
            switch (state.workspace)
            {
                case CanonicalBattlefieldMapWorkspace.Gameplay:
                    DrawGameplayTools();
                    break;
                case CanonicalBattlefieldMapWorkspace.RouteAndMarkers:
                    DrawRouteTools();
                    break;
                case CanonicalBattlefieldMapWorkspace.Presentation:
                    DrawPresentationTools();
                    break;
                case CanonicalBattlefieldMapWorkspace.Validation:
                    DrawValidationTools();
                    break;
            }
            GUILayout.FlexibleSpace();
            state.zoom = EditorGUILayout.Slider("缩放", state.zoom, .4f, 2.5f);
            EditorGUILayout.EndVertical();
        }

        private void DrawGameplayTools()
        {
            EditorGUILayout.LabelField("玩法格", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("玩法能力与碰撞是权威规则；绘制地貌不会改动这里。",
                MessageType.Info);
            gameplayCapabilities = (BattlefieldCellCapabilities)EditorGUILayout.EnumFlagsField(
                "能力", gameplayCapabilities);
            gameplayCollisions = (BattlefieldCollisionChannels)EditorGUILayout.EnumFlagsField(
                "碰撞", gameplayCollisions);
            DrawAreaToolSelector();
        }

        private void DrawRouteTools()
        {
            EditorGUILayout.LabelField("路线与点位", EditorStyles.boldLabel);
            routeTool = (CanonicalBattlefieldRouteTool)GUILayout.Toolbar((int)routeTool,
                new[] { "追加路线", "放置核心", "初始花盆" });
            EditorGUILayout.HelpBox(
                "路线只能逐格四向追加。敌人出生点和路线终点由“同步端点”显式写入；核心必须紧邻终点。",
                MessageType.Info);
            if (routeTool == CanonicalBattlefieldRouteTool.PlaceInitialPot)
            {
                markerGroupId = EditorGUILayout.TextField("花盆组 ID", markerGroupId);
                markerSelectionCount = EditorGUILayout.IntField("选取数量", markerSelectionCount);
                if (GUILayout.Button("创建/更新花盆组"))
                    Mutate("更新初始花盆组", () => map.TrySetMarkerGroup(markerGroupId,
                        markerSelectionCount, out lastOperation));
            }
            if (GUILayout.Button("同步出生点与路线终点"))
                Mutate("同步路线端点", () => map.TrySynchronizeRouteEndpoints(
                    out lastOperation));
            var selectedRouteIndex = map.PrimaryRoute == null ? -1
                : map.PrimaryRoute.Cells.ToList().IndexOf(
                    CanonicalBattlefieldMapEditorState.instance.selectedCell);
            GUI.enabled = selectedRouteIndex >= 0;
            if (GUILayout.Button("从选中格之后截断路线"))
                Mutate("截断路线", () => map.TryTruncateRoute(selectedRouteIndex + 1,
                    out lastOperation));
            GUI.enabled = true;
            if (map.PrimaryRoute != null)
                EditorGUILayout.LabelField("路线格数", map.PrimaryRoute.Cells.Count.ToString());
        }

        private void DrawPresentationTools()
        {
            EditorGUILayout.LabelField("语义地貌", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "只选择语义材质，不编辑 Dual-Grid Mask。Mask 与精修边缘由 Battle 同一规则推导。",
                MessageType.Info);
            DrawPureSquarePresets();
            DrawRegisteredBrushPresets();
            baseSurfaceId = SurfacePopup("底层", baseSurfaceId, false);
            landformSurfaceId = SurfacePopup("地貌", landformSurfaceId, true);
            if (string.IsNullOrEmpty(landformSurfaceId))
            {
                contourStyleId = string.Empty;
                edgeStyleId = string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(contourStyleId))
                    contourStyleId = BattlefieldLayerIds.ContourStyles.Square;
                contourStyleId = ContourPopup(contourStyleId, landformSurfaceId,
                    baseSurfaceId, edgeStyleId);
                DualGridTileSet resolvedEdge;
                var edgeAvailable = previewPalette != null
                    && previewPalette.TryGetEdgeTileSet(landformSurfaceId, baseSurfaceId,
                        contourStyleId, BattlefieldLayerIds.EdgeStyles.Refined, out resolvedEdge);
                var edgeSelected = !string.IsNullOrEmpty(edgeStyleId);
                GUI.enabled = edgeAvailable || edgeSelected;
                edgeSelected = EditorGUILayout.Toggle("精修有向边缘", edgeSelected);
                GUI.enabled = true;
                edgeStyleId = edgeSelected ? BattlefieldLayerIds.EdgeStyles.Refined : string.Empty;
                if (!edgeAvailable)
                    EditorGUILayout.HelpBox("当前材质组合和轮廓没有可兼容的精修边缘素材；可使用基础轮廓，不会借用其他轮廓素材。",
                        MessageType.Info);
            }
            DrawAreaToolSelector();
            if (GUILayout.Button("按玩法生成推荐表现"))
            {
                string recommendationReason;
                if (!CanApplyRecommendedPresentation(out recommendationReason))
                {
                    lastOperation = recommendationReason;
                }
                else if (EditorUtility.DisplayDialog("应用推荐表现",
                        "路线将显示石路、可种植格显示草地，其余显示土壤。玩法格和点位不会改变。",
                        "应用", "取消"))
                    Mutate("应用推荐表现", () => map.ApplyRecommendedPresentation(
                        out lastOperation));
            }
        }

        private void DrawPureSquarePresets()
        {
            EditorGUILayout.LabelField("纯方块笔刷", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var preset in CellAlignedSquareTerrainPresets.All)
            {
                var previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled
                    && CellAlignedSquareTerrainPresets.IsAvailable(preset, previewPalette);
                if (GUILayout.Button(preset.DisplayName))
                {
                    string reason;
                    if (!CellAlignedSquareTerrainPresets.TryResolve(preset.SurfaceId,
                            out baseSurfaceId, out landformSurfaceId, out contourStyleId,
                            out edgeStyleId, out reason))
                        lastOperation = reason;
                    else lastOperation = preset.DisplayName
                        + "已选中；绘制时会清空地貌、轮廓与精修边缘。";
                }
                GUI.enabled = previousEnabled;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "纯方块直接使用不透明底层纹理；不会创建或选择 Dual-Grid Mask。",
                MessageType.None);
        }

        private void DrawRegisteredBrushPresets()
        {
            var definitions = RegisteredBrushes();
            if (definitions.Count == 0) return;
            EditorGUILayout.LabelField("已注册笔刷", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var definition in definitions) DrawRegisteredBrushButton(definition);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRegisteredBrushButton(TerrainBrushDefinition definition)
        {
            var previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && IsRegisteredBrushAvailable(definition,
                previewPalette);
            if (GUILayout.Button(definition.DisplayName))
            {
                landformSurfaceId = definition.LandformSurfaceId;
                baseSurfaceId = definition.BaseSurfaceId;
                contourStyleId = definition.ContourStyleId;
                edgeStyleId = definition.EdgeStyleId;
                lastOperation = definition.DisplayName + " 笔刷已选中。";
            }
            GUI.enabled = previousEnabled;
        }

        internal static IReadOnlyList<TerrainBrushDefinition> RegisteredBrushes()
        {
            return TerrainBrushRegistry.FindAll();
        }

        internal static bool IsRegisteredBrushAvailable(TerrainBrushDefinition definition,
            BattlefieldTerrainPalette palette)
        {
            return TerrainBrushRegistry.IsAvailable(definition, palette, out _);
        }

        private void DrawValidationTools()
        {
            DrawManifestBinding();
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("刷新完整诊断", GUILayout.Height(30f)))
                RefreshDiagnostics(true);
            resizeWidth = EditorGUILayout.IntField("新宽度", resizeWidth);
            resizeHeight = EditorGUILayout.IntField("新高度", resizeHeight);
            if (GUILayout.Button("调整地图尺寸"))
            {
                if (EditorUtility.DisplayDialog("调整地图尺寸",
                        "缩小会移除越界的路线格和点位；操作可通过 Undo 撤回。",
                        "调整", "取消"))
                {
                    BattlefieldMapResizeReport report = null;
                    Mutate("调整地图尺寸", () => map.TryResize(resizeWidth,
                        resizeHeight, out report, out lastOperation));
                }
            }
            EditorGUILayout.Space(8f);
            DrawDiagnostics(authoringDiagnostics, "资产/编译诊断");
            DrawDiagnostics(publicationDiagnostics, "发布诊断");
        }

        private void DrawAreaToolSelector()
        {
            var state = CanonicalBattlefieldMapEditorState.instance;
            state.tool = (CanonicalBattlefieldMapTool)GUILayout.Toolbar((int)state.tool,
                new[] { "单格", "矩形", "填充", "吸管" });
        }

        private void DrawCanvas()
        {
            var state = CanonicalBattlefieldMapEditorState.instance;
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            state.scroll = EditorGUILayout.BeginScrollView(state.scroll,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var cellSize = CanonicalBattlefieldCanvasLayout.BaseCellSize
                * Mathf.Clamp(state.zoom, .4f, 2.5f);
            var canvas = GUILayoutUtility.GetRect(map.GridWidth * cellSize,
                map.GridHeight * cellSize, GUILayout.ExpandWidth(false),
                GUILayout.ExpandHeight(false));
            var layout = new CanonicalBattlefieldCanvasLayout(canvas,
                map.GridWidth, map.GridHeight, state.zoom);
            DrawMap(layout);
            HandleCanvasEvent(layout);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawMap(CanonicalBattlefieldCanvasLayout layout)
        {
            var palette = ResolvePalette();
            BattlefieldMapDefinition compiledMap = null;
            CompiledBattlefieldMap compiled;
            BattlefieldLayeredMapValidationResult ignored;
            if (BattlefieldLayeredMapCompiler.TryCompile(map.ToSource(), out compiled,
                    out ignored)) compiledMap = new BattlefieldMapDefinition(compiled);

            for (var y = 0; y < map.GridHeight; y++)
            for (var x = 0; x < map.GridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                var rect = layout.CellRect(cell);
                BattlefieldVisualCellAuthoringRecord visual;
                Texture2D texture;
                if (map.TryGetVisual(cell, out visual) && palette != null
                    && palette.TryGetBaseTexture(visual.BaseSurfaceId, out texture))
                {
                    if (compiledMap != null)
                        GUI.DrawTextureWithTexCoords(rect, texture,
                            BattlefieldDualGridTerrain.BaseCellUv(compiledMap,
                                palette.ReferenceTileSet, texture, x, y), true);
                    else GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
                }
                else EditorGUI.DrawRect(rect, new Color(.38f, .12f, .12f));
                if (visual != null && !HasCompletePaletteBinding(palette, visual))
                {
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), Color.red);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 4f, rect.width, 4f), Color.red);
                }
            }

            if (palette != null && compiledMap != null)
                DrawDualGridPresentation(layout, compiledMap, palette);

            for (var y = 0; y < map.GridHeight; y++)
            for (var x = 0; x < map.GridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                var rect = layout.CellRect(cell);
                DrawWorkspaceOverlay(rect, cell);
                Handles.BeginGUI();
                Handles.color = new Color(1f, 1f, 1f, .28f);
                Handles.DrawAAPolyLine(1f, new Vector3(rect.xMin, rect.yMin),
                    new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax),
                    new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin));
                Handles.EndGUI();
                GUI.Label(new Rect(rect.x + 2f, rect.y + 1f, rect.width - 4f, 16f),
                    x + "," + y, EditorStyles.miniLabel);
            }

            if (map.InBounds(CanonicalBattlefieldMapEditorState.instance.selectedCell))
                DrawOutline(layout.CellRect(
                    CanonicalBattlefieldMapEditorState.instance.selectedCell), Color.cyan, 3f);
            if (map.InBounds(hoveredCell)) DrawOutline(layout.CellRect(hoveredCell), Color.white, 2f);
            if (map.InBounds(rectangleStart) && map.InBounds(hoveredCell))
            {
                var first = layout.CellRect(rectangleStart);
                var last = layout.CellRect(hoveredCell);
                DrawOutline(Rect.MinMaxRect(Mathf.Min(first.xMin, last.xMin),
                    Mathf.Min(first.yMin, last.yMin), Mathf.Max(first.xMax, last.xMax),
                    Mathf.Max(first.yMax, last.yMax)), Color.yellow, 2f);
            }
        }

        private void DrawDualGridPresentation(CanonicalBattlefieldCanvasLayout layout,
            BattlefieldMapDefinition definition, BattlefieldTerrainPalette palette)
        {
            foreach (var binding in palette.LandformBindings)
            {
                if (binding == null || binding.TileSet == null) continue;
                for (var vertexY = 0; vertexY <= map.GridHeight; vertexY++)
                for (var vertexX = 0; vertexX <= map.GridWidth; vertexX++)
                {
                    var mask = BattlefieldDualGridTerrain.ResolveLandformMask(definition,
                        vertexX, vertexY, binding.SurfaceId, binding.ContourStyleId);
                    DrawMaskSprite(layout, binding.TileSet, mask, vertexX, vertexY);
                }
            }
            foreach (var binding in palette.EdgeBindings)
            {
                if (binding == null || binding.TileSet == null) continue;
                for (var vertexY = 0; vertexY <= map.GridHeight; vertexY++)
                for (var vertexX = 0; vertexX <= map.GridWidth; vertexX++)
                {
                    var mask = BattlefieldDualGridTerrain.ResolveEdgeMask(definition,
                        vertexX, vertexY, binding.LandformSurfaceId,
                        binding.BaseSurfaceId, binding.ContourStyleId,
                        binding.EdgeStyleId);
                    DrawMaskSprite(layout, binding.TileSet, mask, vertexX, vertexY);
                }
            }
        }

        private static void DrawMaskSprite(CanonicalBattlefieldCanvasLayout layout,
            DualGridTileSet tileSet, DualGridMask mask, int vertexX, int vertexY)
        {
            if (mask == DualGridMask.Empty) return;
            Sprite sprite;
            if (!tileSet.TryGetSprite(mask, out sprite) || sprite == null
                || sprite.texture == null) return;
            var rect = new Rect(layout.CanvasRect.x + (vertexX - .5f) * layout.CellSize,
                layout.CanvasRect.y + (vertexY - .5f) * layout.CellSize,
                layout.CellSize, layout.CellSize);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture,
                BattlefieldDualGridTerrain.SpriteUv(sprite), true);
        }

        private void DrawWorkspaceOverlay(Rect rect, Vector2Int cell)
        {
            var workspace = CanonicalBattlefieldMapEditorState.instance.workspace;
            if (workspace == CanonicalBattlefieldMapWorkspace.Gameplay
                || acceptanceCompositeView)
            {
                BattlefieldGameplayCellAuthoringRecord gameplay;
                if (!map.TryGetGameplay(cell, out gameplay)) return;
                if (gameplay.HasCapability(BattlefieldLayerIds.Capabilities.Plantable))
                    EditorGUI.DrawRect(rect, new Color(.1f, .8f, .2f, .18f));
                if (gameplay.HasCapability(BattlefieldLayerIds.Capabilities.EnemyTraversable))
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 5f, rect.width, 5f),
                        new Color(1f, .55f, .1f, .9f));
                if (gameplay.CollisionIds.Count > 0)
                    GUI.Label(new Rect(rect.xMax - 16f, rect.y + 1f, 14f, 14f), "■");
            }
            if (workspace == CanonicalBattlefieldMapWorkspace.RouteAndMarkers
                || acceptanceCompositeView)
            {
                var routeIndex = map.PrimaryRoute == null ? -1
                    : map.PrimaryRoute.Cells.ToList().IndexOf(cell);
                if (routeIndex >= 0)
                {
                    EditorGUI.DrawRect(rect, new Color(1f, .55f, .05f, .34f));
                    GUI.Label(rect, (routeIndex + 1) + RouteDirectionGlyph(routeIndex),
                        new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });
                }
                foreach (var marker in map.Markers.Where(value => value != null
                             && value.Cell == cell))
                    GUI.Label(new Rect(rect.x + 2f, rect.yMax - 18f, rect.width - 4f, 16f),
                        MarkerLabel(marker.Kind), EditorStyles.miniBoldLabel);
            }
            if (workspace == CanonicalBattlefieldMapWorkspace.Validation
                || acceptanceCompositeView)
            {
                if (authoringDiagnostics.Any(value => value.HasCell && value.Cell == cell
                    && value.IsBlocking)
                    || publicationDiagnostics.Any(value => value.HasCell && value.Cell == cell
                        && value.IsBlocking))
                    EditorGUI.DrawRect(rect, new Color(1f, 0f, 0f, .35f));
            }
        }

        private void HandleCanvasEvent(CanonicalBattlefieldCanvasLayout layout)
        {
            var current = Event.current;
            Vector2Int hit;
            var inside = layout.TryHit(current.mousePosition, out hit);
            if (inside && hit != hoveredCell)
            {
                hoveredCell = hit;
                Repaint();
            }
            else if (!inside && hoveredCell.x >= 0)
            {
                hoveredCell = new Vector2Int(-1, -1);
                Repaint();
            }
            if (current.button != 0) return;
            var state = CanonicalBattlefieldMapEditorState.instance;
            if (current.type == EventType.MouseDown && inside)
            {
                state.selectedCell = hit;
                state.Persist();
                if (state.workspace == CanonicalBattlefieldMapWorkspace.Validation) return;
                if (state.tool == CanonicalBattlefieldMapTool.Rectangle
                    && state.workspace != CanonicalBattlefieldMapWorkspace.RouteAndMarkers)
                {
                    rectangleStart = hit;
                    current.Use();
                    return;
                }
                BeginGesture("编辑关卡地图");
                ApplyToolAt(hit);
                if (state.tool != CanonicalBattlefieldMapTool.SingleCell)
                    EndGesture();
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && inside && gestureActive
                     && state.tool == CanonicalBattlefieldMapTool.SingleCell)
            {
                ApplyToolAt(hit);
                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                if (gestureActive) EndGesture();
                if (map.InBounds(rectangleStart) && inside)
                {
                    IReadOnlyList<Vector2Int> cells;
                    string reason;
                    if (CanonicalBattlefieldMapEditorOperations.TryResolveRectangle(map,
                            rectangleStart, hit, out cells, out reason))
                    {
                        BeginGesture("矩形编辑关卡地图");
                        ApplyToCells(cells);
                        EndGesture();
                    }
                    else lastOperation = reason;
                    rectangleStart = new Vector2Int(-1, -1);
                    current.Use();
                }
            }
        }

        private void ApplyToolAt(Vector2Int cell)
        {
            if (!gestureCells.Add(cell)) return;
            var state = CanonicalBattlefieldMapEditorState.instance;
            if (state.workspace == CanonicalBattlefieldMapWorkspace.RouteAndMarkers)
            {
                bool changed;
                string markerId;
                switch (routeTool)
                {
                    case CanonicalBattlefieldRouteTool.AppendRoute:
                        changed = map.TryAppendRouteCell(cell, out lastOperation);
                        break;
                    case CanonicalBattlefieldRouteTool.PlaceCore:
                        changed = map.TryPlaceMarker(BattlefieldMarkerKind.Core, cell,
                            null, out markerId, out lastOperation);
                        break;
                    default:
                        changed = map.TryPlaceMarker(
                            BattlefieldMarkerKind.InitialPotCandidate, cell,
                            markerGroupId, out markerId, out lastOperation);
                        break;
                }
                if (changed) MarkChanged();
                return;
            }
            if (state.tool == CanonicalBattlefieldMapTool.Eyedropper)
            {
                Sample(cell);
                return;
            }
            IReadOnlyList<Vector2Int> cells = new[] { cell };
            if (state.tool == CanonicalBattlefieldMapTool.FloodFill)
            {
                string reason;
                if (state.workspace == CanonicalBattlefieldMapWorkspace.Presentation)
                    CanonicalBattlefieldMapEditorOperations.TryResolveVisualFlood(map,
                        cell, out cells, out reason);
                else CanonicalBattlefieldMapEditorOperations.TryResolveGameplayFlood(map,
                    cell, out cells, out reason);
            }
            ApplyToCells(cells);
        }

        private void ApplyToCells(IEnumerable<Vector2Int> cells)
        {
            bool changed;
            if (CanonicalBattlefieldMapEditorState.instance.workspace
                == CanonicalBattlefieldMapWorkspace.Presentation)
            {
                if (!CanApplyPresentation(out lastOperation)) return;
                changed = CanonicalBattlefieldMapEditorOperations.TryApplyVisual(map,
                    cells, baseSurfaceId, landformSurfaceId, contourStyleId, edgeStyleId,
                    out lastOperation);
            }
            else changed = CanonicalBattlefieldMapEditorOperations.TryApplyGameplay(map,
                cells, CapabilityIds(gameplayCapabilities), CollisionIds(gameplayCollisions),
                out lastOperation);
            if (changed) MarkChanged();
        }

        private void Sample(Vector2Int cell)
        {
            if (CanonicalBattlefieldMapEditorState.instance.workspace
                == CanonicalBattlefieldMapWorkspace.Presentation)
            {
                BattlefieldVisualCellAuthoringRecord visual;
                if (!map.TryGetVisual(cell, out visual)) return;
                baseSurfaceId = visual.BaseSurfaceId;
                landformSurfaceId = visual.LandformSurfaceId;
                contourStyleId = visual.ContourStyleId;
                edgeStyleId = visual.EdgeStyleId;
            }
            else
            {
                BattlefieldGameplayCellAuthoringRecord gameplay;
                if (!map.TryGetGameplay(cell, out gameplay)) return;
                gameplayCapabilities = Capabilities(gameplay.CapabilityIds);
                gameplayCollisions = Collisions(gameplay.CollisionIds);
            }
            lastOperation = "已从 " + cell + " 吸取设置。";
        }

        private void BeginGesture(string label)
        {
            if (gestureActive) return;
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(label);
            Undo.RecordObject(map, label);
            gestureCells.Clear();
            gestureActive = true;
        }

        private void EndGesture()
        {
            if (!gestureActive) return;
            Undo.CollapseUndoOperations(undoGroup);
            gestureActive = false;
            undoGroup = -1;
            gestureCells.Clear();
            RefreshDiagnostics(false);
        }

        private void Mutate(string label, Func<bool> mutation)
        {
            if (map == null) return;
            BeginGesture(label);
            if (mutation()) MarkChanged();
            EndGesture();
        }

        private void MarkChanged()
        {
            EditorUtility.SetDirty(map);
            lastOperation = "操作完成。";
            publishReady = false;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(lastOperation, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("保存草稿", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                EditorUtility.SetDirty(map);
                AssetDatabase.SaveAssets();
                lastOperation = "草稿已保存；有错误的草稿不会被发布。";
            }
            GUI.enabled = publishReady && manifest != null;
            if (GUILayout.Button("全量重建发布", EditorStyles.toolbarButton,
                    GUILayout.Width(100f))) Publish();
            if (GUILayout.Button("正式 Playtest", EditorStyles.toolbarButton,
                    GUILayout.Width(96f))) Playtest();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshDiagnostics(bool includePublication)
        {
            authoringDiagnostics = map == null ? Array.Empty<BattlefieldMapAuthoringDiagnostic>()
                : map.CollectDiagnostics();
            publicationDiagnostics = Array.Empty<BattlefieldMapPublicationDiagnostic>();
            publishReady = false;
            publishLevelId = string.Empty;
            previewPalette = ResolvePaletteFromAssets();
            if (!includePublication || map == null || manifest == null)
            {
                Repaint();
                return;
            }
            PublishedBattlefieldMapCatalog candidate;
            IReadOnlyList<BattlefieldMapPublicationDiagnostic> diagnostics;
            BattlefieldMapPublicationExporter.TryBuildCatalog(manifest,
                BattlefieldMapPublicationExporter.LoadReleaseRegisteredPalettes(),
                out candidate, out diagnostics);
            publicationDiagnostics = diagnostics;
            if (candidate != null) DestroyImmediate(candidate);
            var entry = manifest.FindByMap(map);
            publishLevelId = entry == null ? string.Empty : entry.LevelId;
            publishReady = entry != null && !authoringDiagnostics.Any(value => value.IsBlocking)
                && !publicationDiagnostics.Any(value => value.IsBlocking);
            Repaint();
        }

        private void Publish()
        {
            var result = BattlefieldMapPublicationExporter.Rebuild(manifest);
            publicationDiagnostics = result.Diagnostics;
            publishReady = result.Succeeded;
            lastOperation = result.Succeeded
                ? "发布清单已全量重建，并重新导入生成资源。"
                : result.Diagnostics.Count == 0 ? "发布失败。" : result.Diagnostics[0].ToString();
        }

        private void Playtest()
        {
            string reason;
            if (CanonicalBattlefieldMapPlaytest.TryLaunch(manifest, publishLevelId,
                    out reason)) return;
            lastOperation = reason;
            PublishedBattlefieldPlaytestRequest.Clear();
        }

        private BattlefieldTerrainPalette ResolvePalette()
        {
            return previewPalette;
        }

        private BattlefieldTerrainPalette ResolvePaletteFromAssets()
        {
            if (map == null || manifest == null) return null;
            BattlefieldMapPublicationManifestEntry entry;
            LevelDefinition template;
            LevelPresentationThemeDefinition theme;
            BattlefieldTerrainPalette palette;
            string reason;
            if (BattlefieldMapPublicationExporter.TryResolvePublicationContext(map,
                    manifest, out entry, out template, out theme, out palette, out reason))
                return palette;
            return null;
        }

        private void SetMap(BattlefieldMapAuthoringAsset value)
        {
            map = value;
            CanonicalBattlefieldMapEditorState.instance.mapGuid = GuidFor(map);
            CanonicalBattlefieldMapEditorState.instance.selectedCell = new Vector2Int(-1, -1);
            CanonicalBattlefieldMapEditorState.instance.Persist();
            if (map != null)
            {
                resizeWidth = map.GridWidth;
                resizeHeight = map.GridHeight;
            }
            LoadManifestBinding();
            RefreshDiagnostics(false);
            Repaint();
        }

        private void CreateMapAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("创建关卡地图资产",
                "BattlefieldMap", "asset", "选择地图资产保存位置");
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var created = BattlefieldMapAuthoringAsset.Create(createMapId,
                    Math.Max(1, createWidth), Math.Max(1, createHeight));
                created.name = System.IO.Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(created, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = created;
                SetMap(created);
            }
            catch (Exception exception)
            {
                lastOperation = exception.Message;
            }
        }

        private void CreateManifestAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("创建地图发布清单",
                "BattlefieldMapPublicationManifest", "asset", "选择发布清单保存位置");
            if (string.IsNullOrWhiteSpace(path)) return;
            var created = CreateInstance<BattlefieldMapPublicationManifest>();
            created.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            manifest = created;
            Selection.activeObject = created;
            CanonicalBattlefieldMapEditorState.instance.manifestGuid = GuidFor(manifest);
            CanonicalBattlefieldMapEditorState.instance.Persist();
        }

        private void HandleUndoRedo()
        {
            RefreshDiagnostics(false);
            Repaint();
        }

        private static T LoadGuid<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(guid)) return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static string GuidFor(UnityEngine.Object asset)
        {
            if (asset == null) return string.Empty;
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

        private static string CoordinateLabel(Vector2Int cell)
        {
            return cell.x < 0 || cell.y < 0 ? "—" : cell.x + "," + cell.y;
        }

        private static string MarkerLabel(BattlefieldMarkerKind kind)
        {
            switch (kind)
            {
                case BattlefieldMarkerKind.EnemySpawn: return "出生";
                case BattlefieldMarkerKind.RouteGoal: return "终点";
                case BattlefieldMarkerKind.Core: return "核心";
                case BattlefieldMarkerKind.InitialPotCandidate: return "花盆";
                default: return kind.ToString();
            }
        }

        private string RouteDirectionGlyph(int routeIndex)
        {
            if (map.PrimaryRoute == null || routeIndex < 0
                || routeIndex >= map.PrimaryRoute.Cells.Count - 1) return " · 终";
            var delta = map.PrimaryRoute.Cells[routeIndex + 1]
                - map.PrimaryRoute.Cells[routeIndex];
            if (delta == Vector2Int.right) return " →";
            if (delta == Vector2Int.left) return " ←";
            if (delta == Vector2Int.up) return " ↑";
            if (delta == Vector2Int.down) return " ↓";
            return " !";
        }

        private bool CanApplyPresentation(out string reason)
        {
            if (previewPalette == null)
            {
                reason = "当前发布模板没有解析到 Battle 注册的真实 Palette，不能把缺失绑定当作成功表现。";
                return false;
            }
            Texture2D baseTexture;
            if (!previewPalette.TryGetBaseTexture(baseSurfaceId, out baseTexture))
            {
                reason = "Palette 缺少底层材质绑定：" + baseSurfaceId;
                return false;
            }
            if (!string.IsNullOrEmpty(landformSurfaceId))
            {
                DualGridTileSet landform;
                if (!previewPalette.TryGetLandformTileSet(landformSurfaceId,
                        contourStyleId, out landform))
                {
                    reason = "Palette 缺少精确的地貌+轮廓绑定：" + landformSurfaceId
                        + " / " + contourStyleId;
                    return false;
                }
            }
            if (!string.IsNullOrEmpty(edgeStyleId))
            {
                DualGridTileSet edge;
                if (!previewPalette.TryGetEdgeTileSet(landformSurfaceId,
                        baseSurfaceId, contourStyleId, edgeStyleId, out edge))
                {
                    reason = "Palette 缺少当前 landform/base 同轮廓任一方向的边缘素材。";
                    return false;
                }
            }
            reason = "ok";
            return true;
        }

        private bool CanApplyRecommendedPresentation(out string reason)
        {
            if (previewPalette == null)
            {
                reason = "当前发布模板没有解析到 Battle 注册的真实 Palette。";
                return false;
            }
            Texture2D soil;
            DualGridTileSet grass;
            DualGridTileSet road;
            if (!previewPalette.TryGetBaseTexture(BattlefieldLayerIds.Surfaces.Soil,
                    out soil)
                || !previewPalette.TryGetLandformTileSet(BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square, out grass)
                || !previewPalette.TryGetLandformTileSet(BattlefieldLayerIds.Surfaces.StoneRoad,
                    BattlefieldLayerIds.ContourStyles.Square, out road))
            {
                reason = "真实 Palette 未完整绑定推荐表现所需的土壤、草地和石路。";
                return false;
            }
            reason = "ok";
            return true;
        }

        private static bool HasCompletePaletteBinding(BattlefieldTerrainPalette palette,
            BattlefieldVisualCellAuthoringRecord visual)
        {
            if (palette == null || visual == null) return false;
            Texture2D texture;
            if (!palette.TryGetBaseTexture(visual.BaseSurfaceId, out texture)) return false;
            if (!string.IsNullOrEmpty(visual.LandformSurfaceId))
            {
                DualGridTileSet landform;
                if (!palette.TryGetLandformTileSet(visual.LandformSurfaceId,
                        visual.ContourStyleId, out landform)) return false;
            }
            if (!string.IsNullOrEmpty(visual.EdgeStyleId))
            {
                DualGridTileSet edge;
                if (!palette.TryGetEdgeTileSet(visual.LandformSurfaceId,
                        visual.BaseSurfaceId, visual.ContourStyleId,
                        visual.EdgeStyleId, out edge)) return false;
            }
            return true;
        }

        private void DrawManifestBinding()
        {
            EditorGUILayout.LabelField("发布清单项（唯一发布权威）", EditorStyles.boldLabel);
            if (manifest == null)
            {
                EditorGUILayout.HelpBox("请先在顶栏选择或新建发布清单。", MessageType.Warning);
                return;
            }
            manifestOrder = EditorGUILayout.IntField("顺序", manifestOrder);
            manifestLevelId = EditorGUILayout.TextField("Level ID", manifestLevelId);
            var templates = BundledLevelCatalogFactory.CreateBundledSource().Levels.ToArray();
            var current = Array.FindIndex(templates, level => string.Equals(level.LevelId,
                manifestTemplateLevelId, StringComparison.Ordinal));
            current = EditorGUILayout.Popup("模板关卡", Math.Max(0, current),
                templates.Select(level => level.LevelId).ToArray());
            if (templates.Length > 0) manifestTemplateLevelId = templates[current].LevelId;
            if (GUILayout.Button("写入/更新当前地图的发布项"))
            {
                Undo.RecordObject(manifest, "更新地图发布清单");
                var values = manifest.Entries.Where(entry => entry != null
                    && entry.Map != map).ToList();
                values.Add(new BattlefieldMapPublicationManifestEntry(manifestOrder,
                    manifestLevelId, manifestTemplateLevelId, map));
                manifest.Configure(values);
                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
                RefreshDiagnostics(true);
            }
        }

        private void LoadManifestBinding()
        {
            var entry = manifest == null || map == null ? null : manifest.FindByMap(map);
            if (entry == null) return;
            manifestOrder = entry.Order;
            manifestLevelId = entry.LevelId;
            manifestTemplateLevelId = entry.TemplateLevelId;
        }

        private static string SurfacePopup(string label, string current, bool allowNone)
        {
            var options = allowNone ? new[] { "（无）" }.Concat(SurfaceIds).ToArray()
                : SurfaceIds;
            var offset = allowNone ? 1 : 0;
            var currentIndex = string.IsNullOrEmpty(current) && allowNone ? 0
                : Array.IndexOf(SurfaceIds, current) + offset;
            var selected = EditorGUILayout.Popup(label, Math.Max(0, currentIndex), options);
            return allowNone && selected == 0 ? string.Empty : SurfaceIds[selected - offset];
        }

        private string ContourPopup(string current, string surfaceId,
            string baseSurface, string selectedEdgeStyle)
        {
            var available = AvailableContourStyles(previewPalette, surfaceId,
                baseSurface, selectedEdgeStyle);
            if (available.Length == 0)
            {
                EditorGUILayout.HelpBox(string.IsNullOrEmpty(selectedEdgeStyle)
                        ? "当前材质没有可用轮廓素材。"
                        : "已选有向边缘没有精确的地貌/底层/轮廓/样式组合；请关闭边缘或更换材质组合。",
                    MessageType.Warning);
                return current;
            }
            var labels = available.Select(value => string.Equals(value,
                    BattlefieldLayerIds.ContourStyles.Square, StringComparison.Ordinal)
                ? "方形" : "自然").ToArray();
            var selected = Array.IndexOf(available, current);
            if (selected < 0) selected = 0;
            return available[EditorGUILayout.Popup("轮廓", selected, labels)];
        }

        internal static string[] AvailableContourStyles(BattlefieldTerrainPalette palette,
            string landformSurface, string baseSurface, string selectedEdgeStyle)
        {
            if (palette == null) return Array.Empty<string>();
            var registered = palette.ContourStylesFor(landformSurface)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (string.IsNullOrEmpty(selectedEdgeStyle)) return registered;
            return registered.Where(contour => palette.TryGetEdgeTileSet(
                    landformSurface, baseSurface, contour, selectedEdgeStyle, out _))
                .ToArray();
        }

        private static IEnumerable<string> CapabilityIds(BattlefieldCellCapabilities value)
        {
            if ((value & BattlefieldCellCapabilities.Plantable) != 0)
                yield return BattlefieldLayerIds.Capabilities.Plantable;
            if ((value & BattlefieldCellCapabilities.EnemyTraversable) != 0)
                yield return BattlefieldLayerIds.Capabilities.EnemyTraversable;
            if ((value & BattlefieldCellCapabilities.PlayerTraversable) != 0)
                yield return BattlefieldLayerIds.Capabilities.PlayerTraversable;
            if ((value & BattlefieldCellCapabilities.ItemSpawnCompatible) != 0)
                yield return BattlefieldLayerIds.Capabilities.ItemSpawnCompatible;
        }

        private static IEnumerable<string> CollisionIds(BattlefieldCollisionChannels value)
        {
            if ((value & BattlefieldCollisionChannels.BlocksGround) != 0)
                yield return BattlefieldLayerIds.Collisions.BlocksGround;
            if ((value & BattlefieldCollisionChannels.BlocksProjectile) != 0)
                yield return BattlefieldLayerIds.Collisions.BlocksProjectile;
            if ((value & BattlefieldCollisionChannels.BlocksPlacement) != 0)
                yield return BattlefieldLayerIds.Collisions.BlocksPlacement;
        }

        private static BattlefieldCellCapabilities Capabilities(IEnumerable<string> ids)
        {
            var values = new HashSet<string>(ids ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            var result = BattlefieldCellCapabilities.None;
            if (values.Contains(BattlefieldLayerIds.Capabilities.Plantable))
                result |= BattlefieldCellCapabilities.Plantable;
            if (values.Contains(BattlefieldLayerIds.Capabilities.EnemyTraversable))
                result |= BattlefieldCellCapabilities.EnemyTraversable;
            if (values.Contains(BattlefieldLayerIds.Capabilities.PlayerTraversable))
                result |= BattlefieldCellCapabilities.PlayerTraversable;
            if (values.Contains(BattlefieldLayerIds.Capabilities.ItemSpawnCompatible))
                result |= BattlefieldCellCapabilities.ItemSpawnCompatible;
            return result;
        }

        private static BattlefieldCollisionChannels Collisions(IEnumerable<string> ids)
        {
            var values = new HashSet<string>(ids ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            var result = BattlefieldCollisionChannels.None;
            if (values.Contains(BattlefieldLayerIds.Collisions.BlocksGround))
                result |= BattlefieldCollisionChannels.BlocksGround;
            if (values.Contains(BattlefieldLayerIds.Collisions.BlocksProjectile))
                result |= BattlefieldCollisionChannels.BlocksProjectile;
            if (values.Contains(BattlefieldLayerIds.Collisions.BlocksPlacement))
                result |= BattlefieldCollisionChannels.BlocksPlacement;
            return result;
        }

        private static void DrawOutline(Rect rect, Color color, float width)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }

        private static void DrawDiagnostics<T>(IReadOnlyList<T> diagnostics, string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (diagnostics == null || diagnostics.Count == 0)
            {
                EditorGUILayout.LabelField("无");
                return;
            }
            foreach (var diagnostic in diagnostics.Take(8))
                EditorGUILayout.HelpBox(diagnostic.ToString(), MessageType.Error);
            if (diagnostics.Count > 8)
                EditorGUILayout.LabelField("另有 " + (diagnostics.Count - 8) + " 条…");
        }
    }
}
