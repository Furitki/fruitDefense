using FruitDefense.App;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class P0ValidationSuite
    {
        [MenuItem("Fruit Defense/Validation/Run P0 Release Gate")]
        public static void Run()
        {
            BattleContentCatalogEditor.ValidateBundledCatalog();
            AppFrameworkValidation.SmokeValidate();
            LocalServicePortsSmoke.Run();
            DeterministicSimulationSmoke.Run();
            ComposableBattleSkillsSmoke.Run();
            BattleSnapshotV1Smoke.Run();
            ProjectSetup.SmokeValidate();
            Debug.Log("FRUIT_DEFENSE_P0_RELEASE_GATE_OK");
        }
    }
}
