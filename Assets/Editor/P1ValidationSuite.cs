using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class P1ValidationSuite
    {
        [MenuItem("Fruit Defense/Validate P1 Level Catalog Gate")]
        public static void Run()
        {
            ProjectSetup.SmokeValidate();
            Debug.Log("FRUIT_DEFENSE_P1_LEVEL_CATALOG_GATE_OK");
        }
    }
}
