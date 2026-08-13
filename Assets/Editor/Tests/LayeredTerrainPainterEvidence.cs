using System;
using System.IO;
using System.Linq;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FruitDefense.Editor
{
    public static class LayeredTerrainPainterEvidence
    {
        internal const string EvidencePath =
            "Builds/Evidence/terrain-material-laboratory/unity-terrain-material-laboratory.png";

        public static void Capture()
        {
            var previousActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(LayeredTerrainArtSetup.AcceptanceScenePath,
                OpenSceneMode.Additive);
            var target = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<LayeredTerrainTilemap>(true))
                .Single();
            Selection.activeGameObject = target.gameObject;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                throw new InvalidOperationException("Terrain-material laboratory needs an active Scene view.");
            var wasMaximized = sceneView.maximized;
            sceneView.maximized = true;
            sceneView.FrameSelected();
            sceneView.Repaint();
            EditorApplication.delayCall += () =>
            {
                var liveTarget = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<LayeredTerrainTilemap>(true))
                    .Single();
                liveTarget.EdgeLogicalTilemap.ClearAllTiles();
                if (!liveTarget.Rebuild(out var rebuildReason))
                    throw new InvalidOperationException(
                        "Terrain-material laboratory capture copy could not normalize legacy edges: "
                        + rebuildReason);
                if (!liveTarget.ValidateConfiguration(out var reason))
                    throw new InvalidOperationException(
                        "Terrain-material laboratory target is invalid after maximizing Scene: "
                        + reason);
                LayeredTerrainPainterWindow.Open(liveTarget);
                LayeredTerrainPainterWindow.PrepareAcceptanceView();
                SceneView.RepaintAll();
                EditorApplication.delayCall += () =>
                {
                    CaptureRect(sceneView.position, EvidencePath);
                    LayeredTerrainSceneLaboratory.Close();
                    sceneView.maximized = wasMaximized;
                    if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                        SceneManager.SetActiveScene(previousActiveScene);
                    if (scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                    Debug.Log("CODEX_LAYERED_TERRAIN_LAB_EVIDENCE_OK: " + EvidencePath);
                };
            };
        }

        private static void CaptureRect(Rect rect, string path)
        {
            var width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            var height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
            var pixels = InternalEditorUtility.ReadScreenPixel(
                new Vector2(rect.x, rect.y), width, height);
            if (pixels == null || pixels.Length != width * height)
                throw new InvalidOperationException("Terrain-material laboratory screen capture returned no pixels.");
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                image.SetPixels(pixels);
                image.Apply(false, false);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
