using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class MultiLevelSimulationSmoke
    {
        [MenuItem("Fruit Defense/Validate Multi-Level Simulation")]
        public static void Run()
        {
            var catalog = BundledLevelCatalogFactory.CreateCompiled();
            var teaching = Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard01);
            var coverage = Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard02);
            var pressure = Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard03);

            ValidateDistinctLevelComposition(teaching, coverage, pressure);
            ValidateResolvedRulesAndWaves(teaching);
            ValidateResolvedRulesAndWaves(coverage);
            ValidateResolvedRulesAndWaves(pressure);
            ValidateSameSeedReplay(coverage);
            ValidateLaunchIdentityIsPinned(catalog, coverage);
            ValidateThemeDoesNotAffectSimulation(catalog, teaching);
            ValidateUnknownLevelHasNoFallback(catalog);

            Debug.Log("FRUIT_DEFENSE_MULTI_LEVEL_SIMULATION_OK");
        }

        private static ResolvedLevelDefinition Resolve(CompiledLevelCatalog catalog, string levelId)
        {
            var result = catalog.Resolve(levelId);
            Assert(result.Succeeded && result.Value != null,
                "Expected bundled level to resolve: " + levelId);
            return result.Value;
        }

        private static void ValidateDistinctLevelComposition(params ResolvedLevelDefinition[] levels)
        {
            Assert(levels.Length == 3, "Smoke requires all three bundled levels.");
            Assert(levels.Select(value => value.Identity.MapId).Distinct(StringComparer.Ordinal).Count() == 3,
                "Bundled simulations must use three distinct maps.");
            Assert(levels.Select(value => value.Identity.WaveSetId).Distinct(StringComparer.Ordinal).Count() == 3,
                "Bundled simulations must use three distinct wave sets.");
            Assert(levels.Select(value => value.Identity.RuleSetId).Distinct(StringComparer.Ordinal).Count() == 3,
                "Bundled simulations must use three distinct rule sets.");
            Assert(levels.Select(RouteSignature).Distinct(StringComparer.Ordinal).Count() == 3,
                "Bundled simulations must expose three distinct ordered routes.");

            foreach (var level in levels)
            {
                var simulation = new GameSimulation(level, 1701);
                Assert(ReferenceEquals(simulation.ActiveLevel, level)
                    && ReferenceEquals(simulation.Identity, level.Identity),
                    level.Identity.LevelId + " did not retain its launch-time resolved identity.");
                Assert(ReferenceEquals(simulation.Map, level.Map)
                    && simulation.MapId == level.Identity.MapId,
                    level.Identity.LevelId + " did not inject the resolved P0 map.");
                Assert(simulation.State.Sun == level.RuleSet.InitialSun
                    && simulation.State.Lives == level.RuleSet.InitialLives
                    && simulation.State.Pots.Count == level.RuleSet.InitialPotCount,
                    level.Identity.LevelId + " did not initialize from its active rule set.");
                Assert(simulation.OrderedWaves.Select(value => value.id)
                        .SequenceEqual(level.WaveSet.WaveIds, StringComparer.Ordinal),
                    level.Identity.LevelId + " did not retain its resolved wave order.");
            }
        }

        private static void ValidateResolvedRulesAndWaves(ResolvedLevelDefinition level)
        {
            var simulation = new GameSimulation(level, 2202);
            Assert(simulation.MaxWaves == level.RuleSet.MaxWaves
                && simulation.BetweenWaveSeconds == level.RuleSet.BetweenWaveSeconds
                && simulation.NurserySlotCount == level.RuleSet.NurserySlotCount
                && simulation.NurseryPotChance == level.RuleSet.NurseryPotChance,
                level.Identity.LevelId + " did not expose its HUD-readable active rules.");

            var startingSun = 1000;
            simulation.State.Sun = startingSun;
            Assert(simulation.RefreshNursery(out _),
                level.Identity.LevelId + " could not refresh its resolved nursery.");
            Assert(simulation.State.Sun == startingSun - level.RuleSet.RefreshBaseCost,
                level.Identity.LevelId + " did not use the resolved refresh cost.");
            Assert(simulation.State.Plants.All(value => value.NurseryIndex < level.RuleSet.NurserySlotCount)
                && simulation.LastNurseryPotSlots.All(value => value < level.RuleSet.NurserySlotCount),
                level.Identity.LevelId + " exceeded the resolved nursery slot count.");

            Assert(simulation.StartWave(out _), level.Identity.LevelId + " could not start wave one.");
            Assert(simulation.State.WaveTotal == level.OrderedWaves[0].enemyIds.Length,
                level.Identity.LevelId + " did not start the first resolved wave-set entry.");
            simulation.State.WaveSpawned = simulation.State.WaveTotal;
            simulation.State.Zombies.Clear();
            simulation.Step();
            Assert(simulation.State.Phase == GamePhase.BetweenWaves
                && Mathf.Approximately(simulation.State.BetweenTimer, level.RuleSet.BetweenWaveSeconds),
                level.Identity.LevelId + " did not use the resolved between-wave duration.");

            if (level.RuleSet.MilestoneRewards.Count == 0) return;
            var milestone = level.RuleSet.MilestoneRewards[0];
            var milestoneSimulation = new GameSimulation(level, 2203);
            milestoneSimulation.State.WaveIndex = milestone.Wave - 1;
            var potsBefore = milestoneSimulation.State.Inventory.Pots;
            Assert(milestoneSimulation.StartWave(out _),
                level.Identity.LevelId + " could not start its milestone wave.");
            milestoneSimulation.State.WaveSpawned = milestoneSimulation.State.WaveTotal;
            milestoneSimulation.State.Zombies.Clear();
            milestoneSimulation.Step();
            Assert(milestoneSimulation.State.Inventory.Pots == potsBefore + milestone.PotCount,
                level.Identity.LevelId + " did not grant the resolved milestone pot reward.");
            foreach (var equipmentId in milestone.EquipmentIds)
            {
                var kind = LegacyBattleContentIds.WeaponKindFromId(equipmentId);
                Assert(milestoneSimulation.State.Inventory.Get(kind) > 0,
                    level.Identity.LevelId + " did not grant resolved milestone equipment "
                    + equipmentId + ".");
            }
        }

        private static void ValidateSameSeedReplay(ResolvedLevelDefinition level)
        {
            const int seed = 3303;
            var first = new GameSimulation(level, seed);
            var second = new GameSimulation(level, seed);
            PrepareDeterministicRun(first);
            PrepareDeterministicRun(second);

            var steps = 0;
            while (steps++ < 50000
                && first.State.Phase != GamePhase.Victory
                && first.State.Phase != GamePhase.Defeat)
            {
                Assert(first.Step() == second.Step(), "Replay step availability diverged.");
            }

            Assert(first.State.Phase == second.State.Phase
                && (first.State.Phase == GamePhase.Victory || first.State.Phase == GamePhase.Defeat),
                "Same resolved level and seed did not reach the same terminal result.");
            Assert(first.RandomState == second.RandomState
                && first.OutcomeStateChecksum() == second.OutcomeStateChecksum(),
                "Same resolved level, seed, and inputs did not replay deterministically.");
        }

        private static void PrepareDeterministicRun(GameSimulation simulation)
        {
            simulation.State.Sun = 1000;
            Assert(simulation.RefreshNursery(out _), "Deterministic nursery input failed.");
            Assert(simulation.StartWave(out _), "Deterministic wave input failed.");
        }

        private static void ValidateLaunchIdentityIsPinned(CompiledLevelCatalog catalog,
            ResolvedLevelDefinition launched)
        {
            var simulation = new GameSimulation(launched, 4404);
            var identity = simulation.Identity;
            var identityText = identity.ToString();
            var waves = simulation.OrderedWaves.Select(value => value.id).ToArray();
            Resolve(catalog, BundledLevelCatalogIds.Levels.Orchard03);
            simulation.State.Sun = 1000;
            simulation.RefreshNursery(out _);
            simulation.StartWave(out _);
            for (var index = 0; index < 120; index++) simulation.Step();

            Assert(ReferenceEquals(identity, simulation.Identity)
                && identityText == simulation.Identity.ToString()
                && waves.SequenceEqual(simulation.OrderedWaves.Select(value => value.id),
                    StringComparer.Ordinal),
                "Active simulation identity or wave composition changed after launch.");
        }

        private static void ValidateThemeDoesNotAffectSimulation(CompiledLevelCatalog catalog,
            ResolvedLevelDefinition teaching)
        {
            var source = BundledLevelCatalogFactory.CreateSource();
            var alternateTheme = new LevelPresentationThemeDefinition(
                "theme.smoke.alternate", "Alternate Smoke Theme", "#112233", "#223344",
                "#334455", "#445566", "#556677", "#667788", "#778899", "#8899AA",
                teaching.Theme.TerrainPaletteId);
            var alternateLevel = new LevelDefinition("orchard-theme-smoke", teaching.Identity.MapId,
                teaching.Identity.WaveSetId, teaching.Identity.RuleSetId, alternateTheme.ThemeId);
            var themeSource = new LevelCatalogSource("catalog.levels.theme-smoke",
                source.ContentCatalogId, source.ContentVersion, teaching.Identity.LevelId,
                new[] { teaching.Level, alternateLevel }, source.Maps, source.WaveSets,
                source.RuleSets, source.Themes.Concat(new[] { alternateTheme }), source.TerrainPaletteIds);
            CompiledLevelCatalog themeCatalog;
            LevelCatalogValidationResult validation;
            Assert(LevelCatalogCompiler.TryCompile(themeSource, catalog.BattleContent,
                    out themeCatalog, out validation),
                "Theme-only smoke catalog failed to compile: "
                + string.Join(" | ", validation.Issues.Select(value => value.ToString()).ToArray()));

            var first = new GameSimulation(Resolve(themeCatalog, teaching.Identity.LevelId), 5505);
            var second = new GameSimulation(Resolve(themeCatalog, alternateLevel.LevelId), 5505);
            PrepareDeterministicRun(first);
            PrepareDeterministicRun(second);
            for (var index = 0; index < 500; index++)
            {
                first.Step();
                second.Step();
            }
            Assert(first.Theme.ThemeId != second.Theme.ThemeId
                && first.OutcomeStateChecksum() == second.OutcomeStateChecksum(),
                "Theme-only differences changed deterministic gameplay state.");
        }

        private static void ValidateUnknownLevelHasNoFallback(CompiledLevelCatalog catalog)
        {
            var result = catalog.Resolve("orchard-missing");
            Assert(!result.Succeeded && result.Value == null && result.Error != null
                && result.Error.Code == LevelResolutionErrorCode.UnknownLevel,
                "Unknown LevelId did not produce a structured resolution failure.");

            var threw = false;
            try
            {
                new GameSimulation((ResolvedLevelDefinition)null, 6606);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }
            Assert(threw, "A failed level resolution was allowed to construct the default simulation.");
        }

        private static string RouteSignature(ResolvedLevelDefinition level)
        {
            return string.Join("|", level.Map.RouteCells
                .Select(value => value.x + "," + value.y).ToArray());
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
