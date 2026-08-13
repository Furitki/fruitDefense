using System;
using System.Collections.Generic;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FruitDefense.Editor
{
    [CustomEditor(typeof(DualGridTileSet))]
    public sealed class DualGridTileSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var tiles = serializedObject.FindProperty("maskTiles");
            EditorGUILayout.LabelField("4-bit mask tiles", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Bit order: NW=1, NE=2, SE=4, SW=8. Mask 0 may be empty for transparent layers.",
                MessageType.Info);

            for (var mask = 0; mask < DualGridMaskUtility.MaskCount; mask++)
            {
                var property = tiles.GetArrayElementAtIndex(mask);
                EditorGUILayout.PropertyField(property, new GUIContent(MaskLabel(mask)));
            }
            serializedObject.ApplyModifiedProperties();

            string reason;
            var set = (DualGridTileSet)target;
            if (!set.Validate(out reason)) EditorGUILayout.HelpBox(reason, MessageType.Error);
            else EditorGUILayout.HelpBox("Tile set is valid.", MessageType.Info);
        }

        private static string MaskLabel(int mask)
        {
            var names = string.Empty;
            if ((mask & (int)DualGridMask.NorthWest) != 0) names += " NW";
            if ((mask & (int)DualGridMask.NorthEast) != 0) names += " NE";
            if ((mask & (int)DualGridMask.SouthEast) != 0) names += " SE";
            if ((mask & (int)DualGridMask.SouthWest) != 0) names += " SW";
            if (names.Length == 0) names = " Empty (optional)";
            return mask.ToString("00") + " / " + Convert.ToString(mask, 2).PadLeft(4, '0') + " ·" + names;
        }
    }

    internal static class DualGridTileSetGalleryUtility
    {
        private static readonly List<DualGridTileSet> CachedTileSets = new List<DualGridTileSet>();
        private static bool cacheDirty = true;

        static DualGridTileSetGalleryUtility()
        {
            EditorApplication.projectChanged += Invalidate;
        }

        internal static IReadOnlyList<DualGridTileSet> GetTileSets()
        {
            EnsureCache();
            return CachedTileSets;
        }

        internal static void Refresh()
        {
            cacheDirty = true;
            EnsureCache();
        }

        internal static void Invalidate()
        {
            cacheDirty = true;
        }

        internal static DualGridMask GetPreviewMask(int quadrant)
        {
            switch (quadrant)
            {
                case 0: return DualGridMask.SouthEast;
                case 1: return DualGridMask.SouthWest;
                case 2: return DualGridMask.NorthEast;
                case 3: return DualGridMask.NorthWest;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quadrant), quadrant,
                        "A TileSet preview has four quadrants.");
            }
        }

        internal static TileBase GetPreviewTile(DualGridTileSet tileSet, int quadrant)
        {
            return tileSet == null ? null : tileSet.GetTile(GetPreviewMask(quadrant));
        }

        internal static bool TryGetPreviewSprite(TileBase tileBase, out Sprite sprite)
        {
            var tile = tileBase as Tile;
            sprite = tile == null ? null : tile.sprite;
            return sprite != null && sprite.texture != null;
        }

        internal static Texture2D GetFallbackPreview(TileBase tileBase)
        {
            if (tileBase == null) return null;
            return AssetPreview.GetAssetPreview(tileBase) ?? AssetPreview.GetMiniThumbnail(tileBase);
        }

        internal static bool AssignAndRebuild(DualGridTilemap renderer, DualGridTileSet tileSet,
            bool recordUndo, out string reason)
        {
            if (renderer == null)
            {
                reason = "Dual-Grid component is missing.";
                return false;
            }
            if (tileSet == null)
            {
                reason = "TileSet is missing.";
                return false;
            }
            if (!tileSet.Validate(out var tileSetReason))
            {
                reason = "TileSet is invalid: " + tileSetReason;
                return false;
            }

            var undoGroup = -1;
            if (recordUndo)
            {
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Select Dual-Grid TileSet");
                Undo.RecordObject(renderer, "Select Dual-Grid TileSet");
                if (renderer.GeneratedTilemap != null)
                    Undo.RegisterCompleteObjectUndo(renderer.GeneratedTilemap,
                        "Rebuild Dual-Grid output");
            }

            renderer.Configure(renderer.LogicalTilemap, renderer.GeneratedTilemap, tileSet,
                renderer.AutomaticRefresh);
            EditorUtility.SetDirty(renderer);

            if (renderer.ValidateConfiguration(out var configurationReason))
            {
                if (!renderer.Rebuild(out var rebuildReason))
                {
                    reason = "TileSet selected, but rebuild failed: " + rebuildReason;
                    MarkSceneDirty(renderer);
                    if (recordUndo) Undo.CollapseUndoOperations(undoGroup);
                    return false;
                }
                if (renderer.GeneratedTilemap != null)
                    EditorUtility.SetDirty(renderer.GeneratedTilemap);
                reason = "ok";
            }
            else
            {
                reason = "TileSet selected; rebuild skipped: " + configurationReason;
            }

            MarkSceneDirty(renderer);
            if (recordUndo) Undo.CollapseUndoOperations(undoGroup);
            return true;
        }

        private static void EnsureCache()
        {
            if (!cacheDirty) return;

            CachedTileSets.Clear();
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:DualGridTileSet", new[] { "Assets" });
            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }

            foreach (var path in paths)
            {
                var tileSet = AssetDatabase.LoadAssetAtPath<DualGridTileSet>(path);
                if (tileSet != null) CachedTileSets.Add(tileSet);
            }
            cacheDirty = false;
        }

        private static void MarkSceneDirty(DualGridTilemap renderer)
        {
            if (renderer.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        }
    }

    [CustomEditor(typeof(DualGridTilemap))]
    public sealed class DualGridTilemapEditor : UnityEditor.Editor
    {
        private const string DemoPaintTilePath = "Assets/DualGridDemo/Tiles/LogicalPaint.asset";
        private const float GalleryCardWidth = 94f;
        private const float GalleryCardHeight = 124f;
        private const float GalleryCardGap = 4f;
        private static bool manualPaintEnabled;
        private static Vector3Int lastPaintedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        private string galleryMessage;
        private MessageType galleryMessageType = MessageType.Info;

        internal static bool ManualPaintingEnabled
        {
            get { return manualPaintEnabled; }
        }

        public static void EnableManualPainting()
        {
            manualPaintEnabled = true;
            lastPaintedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            SceneView.RepaintAll();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("logicalTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedTilemap"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSet"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("automaticRefresh"));
            serializedObject.ApplyModifiedProperties();

            var renderer = (DualGridTilemap)target;
            DrawTileSetGallery(renderer);

            string reason;
            var valid = renderer.ValidateConfiguration(out reason);
            EditorGUILayout.HelpBox(valid ? "Configuration is valid." : reason,
                valid ? MessageType.Info : MessageType.Error);
            if (valid && !renderer.HasExpectedAlignment())
                EditorGUILayout.HelpBox("Generated Tilemap is not aligned to logical-grid vertices.",
                    MessageType.Warning);

            using (new EditorGUI.DisabledScope(!valid))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Align output"))
                {
                    Undo.RecordObject(renderer.GeneratedTilemap.transform, "Align Dual-Grid output");
                    renderer.AlignGeneratedTilemap();
                    MarkSceneDirty(renderer);
                }
                if (GUILayout.Button("Rebuild now"))
                {
                    Undo.RegisterCompleteObjectUndo(renderer.GeneratedTilemap, "Rebuild Dual-Grid output");
                    renderer.Rebuild(out reason);
                    MarkSceneDirty(renderer);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            var nextPaintMode = GUILayout.Toggle(manualPaintEnabled,
                manualPaintEnabled ? "Stop manual terrain painting" : "Start manual terrain painting",
                "Button");
            if (nextPaintMode != manualPaintEnabled)
            {
                manualPaintEnabled = nextPaintMode;
                lastPaintedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
                SceneView.RepaintAll();
            }
            if (manualPaintEnabled)
                EditorGUILayout.HelpBox(
                    "Scene view: drag left mouse to paint grass; hold Shift and drag left mouse to erase.",
                    MessageType.Info);
        }

        private void DrawTileSetGallery(DualGridTilemap renderer)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("TileSet brush", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Refresh", "Refresh discovered TileSets"),
                    GUILayout.Width(58f)))
            {
                DualGridTileSetGalleryUtility.Refresh();
                galleryMessage = "TileSet list refreshed.";
                galleryMessageType = MessageType.Info;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Choose a card to restyle and rebuild this entire generated layer. "
                + "Manual painting still edits occupied cells.", MessageType.None);

            var tileSets = DualGridTileSetGalleryUtility.GetTileSets();
            if (tileSets.Count == 0)
            {
                EditorGUILayout.HelpBox("No DualGridTileSet assets were found under Assets.",
                    MessageType.Warning);
                return;
            }

            var availableWidth = Mathf.Max(GalleryCardWidth,
                EditorGUIUtility.currentViewWidth - 36f);
            var columns = Mathf.Max(1,
                Mathf.FloorToInt((availableWidth + GalleryCardGap)
                    / (GalleryCardWidth + GalleryCardGap)));

            for (var start = 0; start < tileSets.Count; start += columns)
            {
                EditorGUILayout.BeginHorizontal();
                var end = Mathf.Min(start + columns, tileSets.Count);
                for (var index = start; index < end; index++)
                {
                    if (index > start) GUILayout.Space(GalleryCardGap);
                    DrawTileSetCard(renderer, tileSets[index]);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(GalleryCardGap);
            }

            if (!string.IsNullOrEmpty(galleryMessage))
                EditorGUILayout.HelpBox(galleryMessage, galleryMessageType);
        }

        private void DrawTileSetCard(DualGridTilemap renderer, DualGridTileSet tileSet)
        {
            var valid = tileSet.Validate(out var validationReason);
            var selected = renderer.TileSet == tileSet;
            var assetPath = AssetDatabase.GetAssetPath(tileSet);
            var tooltip = valid ? assetPath : assetPath + "\n" + validationReason;
            var cardRect = GUILayoutUtility.GetRect(GalleryCardWidth, GalleryCardHeight,
                GUILayout.Width(GalleryCardWidth), GUILayout.Height(GalleryCardHeight));

            var outlineColor = !valid
                ? new Color(.72f, .25f, .2f, 1f)
                : selected
                    ? new Color(.2f, .62f, 1f, 1f)
                    : EditorGUIUtility.isProSkin
                        ? new Color(.31f, .31f, .31f, 1f)
                        : new Color(.62f, .62f, .62f, 1f);
            var backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(.15f, .15f, .15f, 1f)
                : new Color(.86f, .86f, .86f, 1f);
            EditorGUI.DrawRect(cardRect, outlineColor);
            EditorGUI.DrawRect(Inset(cardRect, selected ? 2f : 1f), backgroundColor);

            var clicked = GUI.Button(cardRect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            var previewRect = new Rect(cardRect.x + 7f, cardRect.y + 7f,
                cardRect.width - 14f, cardRect.width - 14f);
            EditorGUI.DrawRect(previewRect, new Color(.08f, .08f, .08f, 1f));
            DrawIslandPreview(previewRect, tileSet);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
            };
            var labelRect = new Rect(cardRect.x + 4f, previewRect.yMax + 4f,
                cardRect.width - 8f, 18f);
            GUI.Label(labelRect, TileSetDisplayName(tileSet), labelStyle);

            var stateRect = new Rect(cardRect.x + 4f, labelRect.yMax,
                cardRect.width - 8f, 14f);
            if (!valid)
            {
                var invalidStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    normal = { textColor = new Color(1f, .42f, .35f, 1f) },
                };
                GUI.Label(stateRect, "INVALID", invalidStyle);
            }
            else if (selected)
            {
                var activeStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    normal = { textColor = new Color(.35f, .75f, 1f, 1f) },
                };
                GUI.Label(stateRect, "ACTIVE", activeStyle);
            }

            if (!clicked || selected) return;
            if (!valid)
            {
                galleryMessage = tileSet.name + " cannot be selected: " + validationReason;
                galleryMessageType = MessageType.Error;
                return;
            }

            if (DualGridTileSetGalleryUtility.AssignAndRebuild(renderer, tileSet, true,
                    out var assignmentReason))
            {
                galleryMessage = assignmentReason == "ok"
                    ? "Applied " + tileSet.name + " and rebuilt the generated layer."
                    : assignmentReason;
                galleryMessageType = assignmentReason == "ok"
                    ? MessageType.Info
                    : MessageType.Warning;
            }
            else
            {
                galleryMessage = assignmentReason;
                galleryMessageType = MessageType.Error;
            }
            serializedObject.Update();
            SceneView.RepaintAll();
            Repaint();
        }

        private void DrawIslandPreview(Rect rect, DualGridTileSet tileSet)
        {
            var halfWidth = rect.width * .5f;
            var halfHeight = rect.height * .5f;
            for (var quadrant = 0; quadrant < 4; quadrant++)
            {
                var x = rect.x + (quadrant % 2) * halfWidth;
                var y = rect.y + (quadrant / 2) * halfHeight;
                var quadrantRect = new Rect(x, y, halfWidth, halfHeight);
                var tile = DualGridTileSetGalleryUtility.GetPreviewTile(tileSet, quadrant);
                if (!DrawTilePreview(quadrantRect, tile)
                    && tile != null
                    && AssetPreview.IsLoadingAssetPreview(tile.GetInstanceID()))
                    Repaint();
            }
        }

        private static bool DrawTilePreview(Rect rect, TileBase tileBase)
        {
            if (DualGridTileSetGalleryUtility.TryGetPreviewSprite(tileBase, out var sprite))
            {
                var texture = sprite.texture;
                var spriteRect = sprite.textureRect;
                var uv = new Rect(spriteRect.x / texture.width, spriteRect.y / texture.height,
                    spriteRect.width / texture.width, spriteRect.height / texture.height);
                GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
                return true;
            }

            var fallback = DualGridTileSetGalleryUtility.GetFallbackPreview(tileBase);
            if (fallback == null) return false;
            GUI.DrawTexture(rect, fallback, ScaleMode.ScaleToFit, true);
            return true;
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f),
                Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static string TileSetDisplayName(DualGridTileSet tileSet)
        {
            const string suffix = "DualGridTileSet";
            var displayName = tileSet == null ? string.Empty : tileSet.name;
            return displayName.EndsWith(suffix, StringComparison.Ordinal)
                ? displayName.Substring(0, displayName.Length - suffix.Length)
                : displayName;
        }

        private void OnSceneGUI()
        {
            if (!manualPaintEnabled) return;
            var renderer = (DualGridTilemap)target;
            var logical = renderer.LogicalTilemap;
            if (logical == null) return;

            var currentEvent = Event.current;
            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            var plane = new Plane(logical.transform.forward, logical.transform.position);
            if (!plane.Raycast(ray, out var distance)) return;
            var worldPosition = ray.GetPoint(distance);
            var cell = logical.WorldToCell(worldPosition);
            var center = logical.GetCellCenterWorld(cell);
            var cellSize = logical.layoutGrid.cellSize;
            Handles.color = currentEvent.shift
                ? new Color(1f, .35f, .25f, .95f)
                : new Color(.35f, 1f, .45f, .95f);
            Handles.DrawWireCube(center, new Vector3(cellSize.x, cellSize.y, 0f));
            Handles.Label(center + Vector3.up * cellSize.y * .58f,
                currentEvent.shift ? "erase" : "paint");

            if (currentEvent.alt || currentEvent.button != 0
                || (currentEvent.type != EventType.MouseDown
                    && currentEvent.type != EventType.MouseDrag))
            {
                if (currentEvent.type == EventType.MouseUp) lastPaintedCell = InvalidCell();
                return;
            }
            if (cell == lastPaintedCell)
            {
                currentEvent.Use();
                return;
            }

            var paintTile = AssetDatabase.LoadAssetAtPath<TileBase>(DemoPaintTilePath);
            if (!currentEvent.shift && paintTile == null)
            {
                Debug.LogError("Dual-Grid manual paint tile is missing: " + DemoPaintTilePath);
                manualPaintEnabled = false;
                return;
            }

            Undo.RegisterCompleteObjectUndo(logical,
                currentEvent.shift ? "Erase Dual-Grid logical tile" : "Paint Dual-Grid logical tile");
            if (renderer.GeneratedTilemap != null)
                Undo.RegisterCompleteObjectUndo(renderer.GeneratedTilemap, "Refresh Dual-Grid output");
            if (!renderer.SetLogicalTile(cell, currentEvent.shift ? null : paintTile, out var reason))
            {
                Debug.LogError("Dual-Grid manual paint failed: " + reason);
                return;
            }

            lastPaintedCell = cell;
            EditorUtility.SetDirty(logical);
            if (renderer.GeneratedTilemap != null) EditorUtility.SetDirty(renderer.GeneratedTilemap);
            MarkSceneDirty(renderer);
            currentEvent.Use();
            SceneView.RepaintAll();
        }

        private static Vector3Int InvalidCell()
        {
            return new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }

        private static void MarkSceneDirty(DualGridTilemap renderer)
        {
            EditorUtility.SetDirty(renderer);
            if (renderer.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        }
    }
}
