using FruitDefense.Shell;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class HubPagesRuntimeSmoke
    {
        public static void Run()
        {
            ShellFlowValidation.Validate(
                ProjectSetup.RequireReleaseRuntimeUiTheme());
            Debug.Log("FRUIT_DEFENSE_HUB_PAGES_RUNTIME_OK");
        }
    }
}
