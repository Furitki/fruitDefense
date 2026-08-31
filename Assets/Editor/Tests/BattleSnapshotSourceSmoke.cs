using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;

namespace FruitDefense.Editor
{
    internal static class BattleSnapshotSourceSmoke
    {
        public static void Validate(CompiledLevelCatalog catalog)
        {
            var levelId = BundledLevelCatalogIds.Levels.Orchard01;
            var source = BattleSnapshotSmoke.CreateScenario(catalog, levelId, 9101);
            source.Step();
            var snapshot = source.ExportSnapshot().Snapshot;
            ValidateSerializedIdentityRejection(catalog, levelId, snapshot);
            ValidateSuppliedCatalogIdentity(catalog, levelId, snapshot);
            ValidateSameIdentityDefinitionMutation(catalog, levelId, snapshot);
        }

        private static void ValidateSerializedIdentityRejection(CompiledLevelCatalog catalog,
            string levelId, BattleSnapshot snapshot)
        {
            var mutations = new Dictionary<string, Action<BattleSnapshot>>
            {
                { "levelCatalogId", value => value.levelCatalogId += ".other" },
                { "contentCatalogId", value => value.contentCatalogId += ".other" },
                { "contentVersion", value => value.contentVersion += ".other" },
                { "levelId", value => value.levelId = BundledLevelCatalogIds.Levels.Orchard02 },
                { "mapId", value => value.mapId += ".other" },
                { "gameplayMapFingerprint", value => value.gameplayMapFingerprint += "0" },
                { "waveSetId", value => value.waveSetId += ".other" },
                { "ruleSetId", value => value.ruleSetId += ".other" },
                { "themeId", value => value.themeId += ".other" },
                { "resolvedSourceDefinitionFingerprint",
                    value => value.resolvedSourceDefinitionFingerprint += "0" },
            };
            foreach (var mutation in mutations)
            {
                var corrupt = BattleSnapshotSmoke.Clone(snapshot);
                mutation.Value(corrupt);
                AssertAtomicSourceFailure(catalog, levelId, corrupt, catalog,
                    mutation.Key, "serialized " + mutation.Key);
            }
        }

        private static void ValidateSuppliedCatalogIdentity(CompiledLevelCatalog catalog,
            string levelId, BattleSnapshot snapshot)
        {
            var baseSource = BundledLevelCatalogFactory.CreateSource();
            var otherCatalogSource = CloneLevelSource(baseSource,
                catalogId: baseSource.CatalogId + ".other");
            var otherCatalog = CompileLevel(otherCatalogSource, catalog.BattleContent);
            AssertAtomicSourceFailure(catalog, levelId, snapshot, otherCatalog,
                "levelCatalogId", "cross catalog");

            var authored = BundledBattleContentFactory.Create();
            authored.header.contentVersion += ".other";
            var otherContent = CompileContent(authored);
            var otherVersionSource = CloneLevelSource(baseSource,
                contentVersion: authored.header.contentVersion);
            var otherVersion = CompileLevel(otherVersionSource, otherContent);
            AssertAtomicSourceFailure(catalog, levelId, snapshot, otherVersion,
                "contentVersion", "cross content version");
        }

        private static void ValidateSameIdentityDefinitionMutation(
            CompiledLevelCatalog catalog, string levelId, BattleSnapshot snapshot)
        {
            var source = BundledLevelCatalogFactory.CreateSource();

            var selectionLevelId = BundledLevelCatalogIds.Levels.Orchard02;
            var selectionSource = BattleSnapshotSmoke.CreateScenario(catalog,
                selectionLevelId, 9151);
            selectionSource.Step();
            var selectionSnapshot = selectionSource.ExportSnapshot().Snapshot;
            var waveSets = source.WaveSets.Select(value =>
            {
                if (value.WaveSetId != BundledLevelCatalogIds.WaveSets.Coverage) return value;
                var ids = value.WaveIds.ToArray();
                ids[1] = "wave.03";
                return new LevelWaveSetDefinition(value.WaveSetId, ids);
            }).ToArray();
            var reorderedWaves = CompileLevel(CloneLevelSource(source, waveSets: waveSets),
                catalog.BattleContent);
            BattleSnapshotSmoke.Assert(ReferenceEquals(catalog.BattleContent,
                    reorderedWaves.BattleContent),
                "ordered-wave mutation reuses the identical compiled content catalog");
            AssertDefinitionMutation(catalog, selectionLevelId, selectionSnapshot,
                reorderedWaves, "same-content ordered-wave selection/order");

            var rules = source.RuleSets.Select(value =>
            {
                if (value.RuleSetId != BundledLevelCatalogIds.RuleSets.Baseline) return value;
                var dto = value.CreateBattleRules();
                dto.initialSun += 1;
                return new LevelRuleSetDefinition(dto);
            }).ToArray();
            AssertDefinitionMutation(catalog, levelId, snapshot,
                CompileLevel(CloneLevelSource(source, ruleSets: rules), catalog.BattleContent),
                "same-ID rule payload");

            var themes = source.Themes.Select(value =>
                value.ThemeId != BundledLevelCatalogIds.Themes.DayOrchard ? value
                    : new LevelPresentationThemeDefinition(value.ThemeId, value.DisplayName,
                        value.BackgroundColor, value.GroundColor, value.RouteColor,
                        value.RouteEdgeColor, value.PlantableColor, value.BlockedColor,
                        value.CoreColor, "#11AA22", value.TerrainPaletteId)).ToArray();
            AssertDefinitionMutation(catalog, levelId, snapshot,
                CompileLevel(CloneLevelSource(source, themes: themes), catalog.BattleContent),
                "same-ID theme payload");

            var waveAuthored = BundledBattleContentFactory.Create();
            waveAuthored.waves.Single(value => value.id == "wave.01").completionReward += 1;
            var waveContent = CompileContent(waveAuthored);
            AssertDefinitionMutation(catalog, levelId, snapshot,
                CompileLevel(source, waveContent), "same-ID ordered-wave payload");

            var contentAuthored = BundledBattleContentFactory.Create();
            contentAuthored.plants.Single(value => value.id == BattleContentIds.Plants.Pea)
                .damage += 1f;
            var changedContent = CompileContent(contentAuthored);
            AssertDefinitionMutation(catalog, levelId, snapshot,
                CompileLevel(source, changedContent), "same-ID compiled-content payload");

            var baseMap = BattlefieldMapDefinition.CreateDefault();
            var baseMapSource = BattlefieldLayeredMapFactory.CreateSingleRouteMap(
                baseMap.MapId, baseMap.GridWidth, baseMap.GridHeight,
                baseMap.MapUnitsPerCell, baseMap.RouteCells, baseMap.CoreCell,
                baseMap.InitialPotGroupOrder.Select(id => baseMap.InitialPotGroups[id]),
                BattlefieldPlantableVisualStyle.BaseOnlyGrass);
            var gameplay = baseMapSource.GameplayCells.ToArray();
            var changedCell = Array.FindIndex(gameplay, value => value.CapabilityIds.Contains(
                BattlefieldLayerIds.Capabilities.Plantable));
            BattleSnapshotSmoke.Assert(changedCell >= 0,
                "same-ID map fixture has a plantable gameplay cell");
            gameplay[changedCell] = new BattlefieldGameplayCellSource(
                gameplay[changedCell].CapabilityIds.Concat(new[]
                {
                    BattlefieldLayerIds.Capabilities.ItemSpawnCompatible,
                }), gameplay[changedCell].CollisionIds);
            var changedMapSource = new BattlefieldLayeredMapSource(
                baseMapSource.SchemaVersion, baseMapSource.MapId,
                baseMapSource.GridWidth, baseMapSource.GridHeight,
                baseMapSource.MapUnitsPerCell, baseMapSource.PrimaryRouteId,
                baseMapSource.VisualCells, gameplay, baseMapSource.Routes,
                baseMapSource.MarkerGroups, baseMapSource.Markers,
                baseMapSource.ExecutionProfile);
            var changedMap = new BattlefieldMapDefinition(changedMapSource);
            var maps = source.Maps.Select(value => value.MapId == changedMap.MapId
                ? changedMap : value).ToArray();
            AssertMapMutation(catalog, levelId, snapshot,
                CompileLevel(CloneLevelSource(source, maps: maps), catalog.BattleContent));
        }

        private static void AssertMapMutation(CompiledLevelCatalog targetCatalog,
            string levelId, BattleSnapshot snapshot, CompiledLevelCatalog supplied)
        {
            var targetIdentity = new GameSimulation(targetCatalog, levelId, 9251)
                .ResolvedSourceIdentity;
            var suppliedIdentity = new GameSimulation(supplied, levelId, 9252)
                .ResolvedSourceIdentity;
            BattleSnapshotSmoke.Assert(targetIdentity.MapId == suppliedIdentity.MapId
                    && targetIdentity.GameplayMapFingerprint
                        != suppliedIdentity.GameplayMapFingerprint
                    && targetIdentity.DefinitionFingerprint
                        != suppliedIdentity.DefinitionFingerprint,
                "same map ID gameplay-cell mutation changes map and source fingerprints");
            AssertAtomicSourceFailure(targetCatalog, levelId, snapshot, supplied,
                "gameplayMapFingerprint", "same-ID gameplay map payload");
        }

        private static void AssertDefinitionMutation(CompiledLevelCatalog targetCatalog,
            string levelId, BattleSnapshot snapshot, CompiledLevelCatalog supplied,
            string label)
        {
            var targetIdentity = new GameSimulation(targetCatalog, levelId, 9201)
                .ResolvedSourceIdentity;
            var suppliedIdentity = new GameSimulation(supplied, levelId, 9202)
                .ResolvedSourceIdentity;
            BattleSnapshotSmoke.Assert(targetIdentity.LevelCatalogId
                    == suppliedIdentity.LevelCatalogId
                && targetIdentity.ContentCatalogId == suppliedIdentity.ContentCatalogId
                && targetIdentity.ContentVersion == suppliedIdentity.ContentVersion
                && targetIdentity.LevelId == suppliedIdentity.LevelId
                && targetIdentity.MapId == suppliedIdentity.MapId
                && targetIdentity.WaveSetId == suppliedIdentity.WaveSetId
                && targetIdentity.RuleSetId == suppliedIdentity.RuleSetId
                && targetIdentity.ThemeId == suppliedIdentity.ThemeId
                && targetIdentity.DefinitionFingerprint
                    != suppliedIdentity.DefinitionFingerprint,
                label + " changes only the resolved definition fingerprint");
            AssertAtomicSourceFailure(targetCatalog, levelId, snapshot, supplied,
                "resolvedSourceDefinitionFingerprint", label);
        }

        private static void AssertAtomicSourceFailure(CompiledLevelCatalog targetCatalog,
            string levelId, BattleSnapshot snapshot, CompiledLevelCatalog suppliedCatalog,
            string path, string label)
        {
            var target = BattleSnapshotSmoke.CreateScenario(targetCatalog, levelId, 9301);
            BattleSnapshotBehaviorSmoke.AssertMutationFreeRestoreFailure(target,
                snapshot, suppliedCatalog, BattleSnapshotRestoreCode.IncompatibleSource,
                path, label);
        }

        private static LevelCatalogSource CloneLevelSource(LevelCatalogSource source,
            string catalogId = null, string contentVersion = null,
            IEnumerable<BattlefieldMapDefinition> maps = null,
            IEnumerable<LevelWaveSetDefinition> waveSets = null,
            IEnumerable<LevelRuleSetDefinition> ruleSets = null,
            IEnumerable<LevelPresentationThemeDefinition> themes = null)
        {
            return new LevelCatalogSource(catalogId ?? source.CatalogId,
                source.ContentCatalogId, contentVersion ?? source.ContentVersion,
                source.DefaultLevelId, source.Levels, maps ?? source.Maps,
                waveSets ?? source.WaveSets,
                ruleSets ?? source.RuleSets, themes ?? source.Themes,
                source.TerrainPaletteIds);
        }

        private static CompiledBattleContentCatalog CompileContent(
            BattleContentCatalogDto authored)
        {
            if (BattleContentCompiler.TryCompile(authored, out var compiled,
                out var validation)) return compiled;
            throw new InvalidOperationException("Mutated battle content is invalid:\n"
                + string.Join("\n", validation.Issues.Select(value => value.ToString())));
        }

        private static CompiledLevelCatalog CompileLevel(LevelCatalogSource source,
            CompiledBattleContentCatalog content)
        {
            if (LevelCatalogCompiler.TryCompile(source, content, out var compiled,
                out var validation)) return compiled;
            throw new InvalidOperationException("Mutated level catalog is invalid:\n"
                + string.Join("\n", validation.Issues.Select(value => value.ToString())));
        }
    }
}
