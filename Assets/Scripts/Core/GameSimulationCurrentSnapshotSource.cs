using System;
using System.Linq;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public sealed partial class GameSimulation
    {
        private BattleSnapshotRestoreResult ValidateCurrentSnapshotSource(
            BattleSnapshot snapshot, CompiledLevelCatalog availableCatalog,
            out ResolvedLevelDefinition resolved)
        {
            resolved = null;
            if (Mode != BattleSimulationMode.Standard || ResolvedSourceIdentity == null
                || ActiveLevel == null || Identity == null
                || LaunchGrowthSnapshot == null)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnsupportedSessionSource,
                    "session.source",
                    "Current battle snapshots require a catalog-resolved Standard session.");
            if (snapshot == null)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.InvalidPayload,
                    "$", "Snapshot is null.");
            if (!StringEquals(snapshot.schemaId, BattleSnapshotSchema.Id)
                || snapshot.schemaVersion != BattleSnapshotSchema.Version)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.UnsupportedSchema,
                    "schemaId", "Only the current battle snapshot schema is supported.");
            if (availableCatalog == null)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.SourceCatalogUnavailable,
                    "levelCatalogId", "The source level catalog is unavailable.");

            var targetResult = CompareSnapshotSource(snapshot, ResolvedSourceIdentity, "target");
            if (!targetResult.Succeeded) return targetResult;

            var resolution = availableCatalog.Resolve(snapshot.levelId);
            if (!resolution.Succeeded)
                return CurrentSnapshotFailure(BattleSnapshotRestoreCode.IncompatibleSource,
                    "levelId", "The snapshot level cannot be resolved from the supplied catalog.");
            resolved = resolution.Value;
            var supplied = ResolvedBattleSourceIdentity.Create(availableCatalog,
                resolved, LaunchGrowthSnapshot);

            var result = CompareSnapshotSource(snapshot, supplied, "suppliedCatalog");
            if (!result.Succeeded) return result;
            result = CompareResolvedSources(supplied, ResolvedSourceIdentity, "target");
            if (!result.Succeeded) return result;
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult CompareSnapshotSource(
            BattleSnapshot snapshot, ResolvedBattleSourceIdentity source, string owner)
        {
            var fields = new[]
            {
                new SourceField("levelCatalogId", snapshot.levelCatalogId, source.LevelCatalogId),
                new SourceField("contentCatalogId", snapshot.contentCatalogId, source.ContentCatalogId),
                new SourceField("contentVersion", snapshot.contentVersion, source.ContentVersion),
                new SourceField("levelId", snapshot.levelId, source.LevelId),
                new SourceField("mapId", snapshot.mapId, source.MapId),
                new SourceField("gameplayMapFingerprint", snapshot.gameplayMapFingerprint,
                    source.GameplayMapFingerprint),
                new SourceField("waveSetId", snapshot.waveSetId, source.WaveSetId),
                new SourceField("ruleSetId", snapshot.ruleSetId, source.RuleSetId),
                new SourceField("themeId", snapshot.themeId, source.ThemeId),
                new SourceField("growthPolicyId", snapshot.growthPolicyId,
                    source.GrowthPolicyId),
                new SourceField("growthContentCatalogId",
                    snapshot.growthContentCatalogId, source.GrowthContentCatalogId),
                new SourceField("growthContentVersion", snapshot.growthContentVersion,
                    source.GrowthContentVersion),
                new SourceField("growthContentFingerprint",
                    snapshot.growthContentFingerprint,
                    source.GrowthContentFingerprint),
                new SourceField("growthProfileId", snapshot.growthProfileId,
                    source.GrowthProfileId),
                new SourceField("growthProfileRevision",
                    snapshot.growthProfileRevision.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    source.GrowthProfileRevision.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)),
                new SourceField("growthFingerprint", snapshot.growthFingerprint,
                    source.GrowthFingerprint),
                new SourceField("resolvedSourceDefinitionFingerprint",
                    snapshot.resolvedSourceDefinitionFingerprint, source.DefinitionFingerprint),
            };
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field.Actual)
                    || !StringEquals(field.Actual, field.Expected))
                    return CurrentSnapshotFailure(BattleSnapshotRestoreCode.IncompatibleSource,
                        field.Path, "Snapshot source does not match the " + owner + " source.");
            }
            return BattleSnapshotRestoreResult.Ok();
        }

        private static BattleSnapshotRestoreResult CompareResolvedSources(
            ResolvedBattleSourceIdentity supplied, ResolvedBattleSourceIdentity target,
            string owner)
        {
            if (supplied.Equals(target)) return BattleSnapshotRestoreResult.Ok();
            var fields = new[]
            {
                new SourceField("levelCatalogId", supplied.LevelCatalogId, target.LevelCatalogId),
                new SourceField("contentCatalogId", supplied.ContentCatalogId, target.ContentCatalogId),
                new SourceField("contentVersion", supplied.ContentVersion, target.ContentVersion),
                new SourceField("levelId", supplied.LevelId, target.LevelId),
                new SourceField("mapId", supplied.MapId, target.MapId),
                new SourceField("gameplayMapFingerprint", supplied.GameplayMapFingerprint,
                    target.GameplayMapFingerprint),
                new SourceField("waveSetId", supplied.WaveSetId, target.WaveSetId),
                new SourceField("ruleSetId", supplied.RuleSetId, target.RuleSetId),
                new SourceField("themeId", supplied.ThemeId, target.ThemeId),
                new SourceField("growthPolicyId", supplied.GrowthPolicyId,
                    target.GrowthPolicyId),
                new SourceField("growthContentCatalogId",
                    supplied.GrowthContentCatalogId, target.GrowthContentCatalogId),
                new SourceField("growthContentVersion", supplied.GrowthContentVersion,
                    target.GrowthContentVersion),
                new SourceField("growthContentFingerprint",
                    supplied.GrowthContentFingerprint,
                    target.GrowthContentFingerprint),
                new SourceField("growthProfileId", supplied.GrowthProfileId,
                    target.GrowthProfileId),
                new SourceField("growthProfileRevision",
                    supplied.GrowthProfileRevision.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                    target.GrowthProfileRevision.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)),
                new SourceField("growthFingerprint", supplied.GrowthFingerprint,
                    target.GrowthFingerprint),
                new SourceField("resolvedSourceDefinitionFingerprint", supplied.DefinitionFingerprint,
                    target.DefinitionFingerprint),
            };
            var mismatch = fields.First(field => !StringEquals(field.Actual, field.Expected));
            return CurrentSnapshotFailure(BattleSnapshotRestoreCode.IncompatibleSource,
                mismatch.Path, "Supplied source does not match the " + owner + " source.");
        }

        private readonly struct SourceField
        {
            public string Path { get; }
            public string Actual { get; }
            public string Expected { get; }

            public SourceField(string path, string actual, string expected)
            {
                Path = path;
                Actual = actual;
                Expected = expected;
            }
        }
    }
}
