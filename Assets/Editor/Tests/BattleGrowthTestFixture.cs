using System;
using FruitDefense.App.Services;
using FruitDefense.Battle;
using FruitDefense.Content;
using FruitDefense.Core;

namespace FruitDefense.Editor
{
    internal static class BattleGrowthTestFixture
    {
        public static BattleGrowthSnapshot ResolveBundled(
            CompiledLevelCatalog levelCatalog, string levelId,
            Action<PlayerProfile> configureProfile = null)
        {
            if (levelCatalog == null) throw new ArgumentNullException(nameof(levelCatalog));
            if (!BundledGameContentLoader.TryLoadBundle(out var bundle,
                    out var contentValidation))
                throw new InvalidOperationException("Bundled outgame content is invalid: "
                    + (contentValidation.Issues.Count == 0
                        ? "unknown"
                        : contentValidation.Issues[0].ToString()));
            return Resolve(levelCatalog, bundle.Outgame, levelId, configureProfile);
        }

        public static BattleGrowthSnapshot Resolve(
            CompiledLevelCatalog levelCatalog,
            CompiledOutgameContentCatalog outgameCatalog,
            string levelId, Action<PlayerProfile> configureProfile = null)
        {
            if (levelCatalog == null) throw new ArgumentNullException(nameof(levelCatalog));
            if (outgameCatalog == null) throw new ArgumentNullException(nameof(outgameCatalog));
            if (!levelCatalog.TryResolve(levelId, out var level, out var levelError))
                throw new InvalidOperationException("Fixture level cannot be resolved: "
                    + levelError);
            var profile = PlayerProfile.CreateDefault();
            profile.profileId = "44444444-4444-4444-4444-444444444444";
            profile.revision = 7;
            profile.lastSelectedLevelId = levelId;
            configureProfile?.Invoke(profile);
            var projection = PlayerProgressionProjection.Create(profile, outgameCatalog);
            var result = BattleGrowthResolver.Resolve(outgameCatalog, level, projection);
            if (!result.Succeeded)
                throw new InvalidOperationException("Battle growth fixture failed: "
                    + result.Code + " at " + result.Path + ": " + result.Message);
            return result.Snapshot;
        }

        public static BattleLaunchRequest Launch(CompiledLevelCatalog levelCatalog,
            string sessionId, string levelId, int seed, string contentVersion)
        {
            return new BattleLaunchRequest(sessionId, levelId, seed, contentVersion,
                BattleSessionMode.Standard, ResolveBundled(levelCatalog, levelId));
        }
    }
}
