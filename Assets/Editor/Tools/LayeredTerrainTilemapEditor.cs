using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitDefense.Editor
{
    [CustomEditor(typeof(LayeredTerrainTilemap))]
    public sealed class LayeredTerrainTilemapEditor : UnityEditor.Editor
    {
        private static bool developerConfigurationExpanded;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var renderer = (LayeredTerrainTilemap)target;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("分层地貌资源验收目标", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("资源验收会在当前 Scene Overlay 中展开；正式关卡请使用关卡地图编辑器。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6f);

            string reason;
            var terrainValid = renderer.ValidateConfiguration(out reason);
            EditorGUILayout.HelpBox(terrainValid ? "地图结构配置有效。" : reason,
                terrainValid ? MessageType.Info : MessageType.Error);
            var presentationValid = renderer.ValidateAuthoringPresentation(out reason);
            EditorGUILayout.HelpBox(presentationValid ? "绘制器名称与预览配置有效。" : reason,
                presentationValid ? MessageType.Info : MessageType.Warning);
            if (terrainValid && !renderer.HasExpectedAlignment())
                EditorGUILayout.HelpBox("生成的地貌与边缘输出需要重新进行半格对齐。",
                    MessageType.Warning);

            using (new EditorGUI.DisabledScope(!terrainValid || !presentationValid))
                if (GUILayout.Button("在 Scene 中打开地貌资源验收", GUILayout.Height(34f)))
                    LayeredTerrainPainterWindow.Open(renderer);

            using (new EditorGUI.DisabledScope(!terrainValid))
                if (GUILayout.Button("重建地貌输出"))
                {
                    RegisterOutputUndo(renderer, "重建地貌输出");
                    renderer.Rebuild(out reason);
                    MarkDirty(renderer);
                }

            EditorGUILayout.Space(8f);
            developerConfigurationExpanded = EditorGUILayout.Foldout(
                developerConfigurationExpanded, "开发者配置", true);
            if (developerConfigurationExpanded)
            {
                EditorGUILayout.HelpBox("以下字段是底层逻辑层、生成输出与素材绑定。普通绘制不需要修改。",
                    MessageType.None);
                DrawPropertiesExcluding(serializedObject, "m_Script");
            }
            serializedObject.ApplyModifiedProperties();
        }

        private static void RegisterOutputUndo(LayeredTerrainTilemap renderer, string label)
        {
            Undo.RegisterCompleteObjectUndo(new Object[]
            {
                renderer,
                renderer.BaseOutputTilemap,
                renderer.LandformAOutputTilemap,
                renderer.LandformBOutputTilemap,
                renderer.EdgeAOnBOutputTilemap,
                renderer.EdgeBOnAOutputTilemap,
            }, label);
        }

        private static void MarkDirty(LayeredTerrainTilemap renderer)
        {
            EditorUtility.SetDirty(renderer);
            foreach (var tilemap in new[]
                     {
                         renderer.BaseOutputTilemap, renderer.LandformAOutputTilemap,
                         renderer.LandformBOutputTilemap, renderer.EdgeAOnBOutputTilemap,
                         renderer.EdgeBOnAOutputTilemap,
                     })
                if (tilemap != null) EditorUtility.SetDirty(tilemap);
            if (renderer.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        }
    }
}
