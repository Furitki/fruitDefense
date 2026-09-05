using System;
using FruitDefense.App;
using FruitDefense.App.Services;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.UI;
using UnityEngine;

namespace FruitDefense.Trials
{
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public sealed class CombinedWorkflowTrialBootstrap : MonoBehaviour, IBattleResultSink
    {
        [SerializeField] private FruitDefenseGame game;
        [SerializeField] private RuntimeUiTheme runtimeUiTheme;

        public void Configure(FruitDefenseGame value, RuntimeUiTheme theme)
        {
            game = value;
            runtimeUiTheme = theme;
        }

        private void Awake()
        {
            if (game == null) game = FindFirstObjectByType<FruitDefenseGame>();
            if (game == null)
                throw new InvalidOperationException(
                    "Combined workflow trial scene requires FruitDefenseGame.");
            if (runtimeUiTheme == null)
                throw new InvalidOperationException(
                    "Combined workflow trial scene requires an explicit runtime UI theme.");
            var themeValidation = runtimeUiTheme.Validate();
            if (!themeValidation.IsValid)
                throw new InvalidOperationException(
                    "Combined workflow trial runtime UI theme is invalid: "
                    + themeValidation.Issues[0]);
            if (game.Status.IsInitialized) return;

            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            var resolution = catalog.Resolve(BundledLevelCatalogIds.Levels.Orchard01);
            if (!resolution.Succeeded || resolution.Value == null)
                throw new InvalidOperationException(
                    "Combined workflow trial could not resolve orchard-01 from the bundled catalog.");

            var navigator = new AppNavigator();
            if (!navigator.TryBeginTransition(AppRoute.Battle, out var navigationError)
                || !navigator.TryCompleteTransition(out navigationError))
                throw new InvalidOperationException(
                    "Combined workflow trial could not enter the Battle route: "
                    + navigationError);

            var level = resolution.Value;
            if (!BundledGameContentLoader.TryLoadBundle(out var bundle,
                    out var contentValidation))
                throw new InvalidOperationException(
                    "Combined workflow trial could not load outgame content: "
                    + (contentValidation.Issues.Count == 0
                        ? "unknown"
                        : contentValidation.Issues[0].ToString()));
            var growth = BattleGrowthResolver.Resolve(bundle.Outgame, level,
                PlayerProgressionProjection.Create(PlayerProfile.CreateDefault(),
                    bundle.Outgame));
            if (!growth.Succeeded)
                throw new InvalidOperationException(
                    "Combined workflow trial could not resolve launch growth: "
                    + growth.Code + " at " + growth.Path);
            var request = new BattleLaunchRequest(
                "combined-workflow-trial-" + Guid.NewGuid().ToString("N"),
                level.Identity.LevelId,
                0,
                level.BattleContent.Header.contentVersion,
                BattleSessionMode.Standard, growth.Snapshot);
            var result = game.Initialize(request, navigator, this, runtimeUiTheme,
                catalog, bundle.Outgame);
            if (!result.Success)
                throw new InvalidOperationException(
                    "Combined workflow trial Battle initialization failed: "
                    + result.ErrorCode);

            Debug.Log("FRUIT_DEFENSE_COMBINED_WORKFLOW_TRIAL_RUNTIME_OK level="
                + level.Identity.LevelId + " theme=" + level.Theme.ThemeId
                + " palette=" + level.Theme.TerrainPaletteId
                + " candidate=ProtectedHybrid knownSeamSafe=false");
        }

        public bool TrySubmitResult(BattleResult result, out string errorCode)
        {
            errorCode = string.Empty;
            return true;
        }
    }
}
