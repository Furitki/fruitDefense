using FruitDefense.Development.GmStress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class GmStressBattleTools
    {
        public const string PlayMenuPath = "Fruit Defense/Playtest/GM 压力测试关";
        public const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        [MenuItem(PlayMenuPath, priority = 20)]
        public static void Play()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请先退出当前 Play Mode，再启动 GM 压力测试关。");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            GmStressBattleLaunchRequest.SetEditorOneShot();
            try
            {
                EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
            catch
            {
                GmStressBattleLaunchRequest.ClearEditorOneShot();
                throw;
            }
        }
    }
}
