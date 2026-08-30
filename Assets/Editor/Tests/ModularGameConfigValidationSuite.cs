using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class ModularGameConfigValidationSuite
    {
        [MenuItem("Fruit Defense/Validation/Run Modular Game Config Gate")]
        public static void Run()
        {
            BattleContentCatalogEditor.ValidateBundledCatalog();
            ModularGameConfigSmoke.Run();
            ComposableBattleAbilitiesSmoke.Run();
            MultiLevelSimulationSmoke.Run();
            PlantInteractionPresentationSmoke.Run();
            Debug.Log("FRUIT_DEFENSE_MODULAR_GAME_CONFIG_GATE_OK");
        }
    }
}
