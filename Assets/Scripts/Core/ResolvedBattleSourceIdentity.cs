using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FruitDefense.Content;

namespace FruitDefense.Core
{
    public sealed class ResolvedBattleSourceIdentity : IEquatable<ResolvedBattleSourceIdentity>
    {
        public string LevelCatalogId { get; }
        public string ContentCatalogId { get; }
        public string ContentVersion { get; }
        public string LevelId { get; }
        public string MapId { get; }
        public string WaveSetId { get; }
        public string RuleSetId { get; }
        public string ThemeId { get; }
        public string GameplayMapFingerprint { get; }
        public string DefinitionFingerprint { get; }

        private ResolvedBattleSourceIdentity(CompiledLevelCatalog catalog,
            ResolvedLevelDefinition resolved)
        {
            LevelCatalogId = catalog.CatalogId;
            ContentCatalogId = catalog.ContentCatalogId;
            ContentVersion = catalog.ContentVersion;
            LevelId = resolved.Identity.LevelId;
            MapId = resolved.Identity.MapId;
            WaveSetId = resolved.Identity.WaveSetId;
            RuleSetId = resolved.Identity.RuleSetId;
            ThemeId = resolved.Identity.ThemeId;
            GameplayMapFingerprint = resolved.Map.GameplayFingerprint;
            DefinitionFingerprint = ResolvedBattleSourceFingerprint.Compute(catalog, resolved);
        }

        internal static CatalogResolvedBattleSource Resolve(CompiledLevelCatalog catalog,
            string levelId)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var resolution = catalog.Resolve(levelId);
            if (!resolution.Succeeded)
                throw new ArgumentException("Level cannot be resolved from the supplied catalog: "
                    + resolution.Error, nameof(levelId));
            return new CatalogResolvedBattleSource(resolution.Value,
                new ResolvedBattleSourceIdentity(catalog, resolution.Value));
        }

        internal static ResolvedBattleSourceIdentity Create(CompiledLevelCatalog catalog,
            ResolvedLevelDefinition resolved)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (resolved == null) throw new ArgumentNullException(nameof(resolved));
            return new ResolvedBattleSourceIdentity(catalog, resolved);
        }

        public bool Equals(ResolvedBattleSourceIdentity other)
        {
            return other != null
                && Same(LevelCatalogId, other.LevelCatalogId)
                && Same(ContentCatalogId, other.ContentCatalogId)
                && Same(ContentVersion, other.ContentVersion)
                && Same(LevelId, other.LevelId)
                && Same(MapId, other.MapId)
                && Same(WaveSetId, other.WaveSetId)
                && Same(RuleSetId, other.RuleSetId)
                && Same(ThemeId, other.ThemeId)
                && Same(GameplayMapFingerprint, other.GameplayMapFingerprint)
                && Same(DefinitionFingerprint, other.DefinitionFingerprint);
        }

        public override bool Equals(object value)
        {
            return Equals(value as ResolvedBattleSourceIdentity);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + OrdinalHash(LevelCatalogId);
                hash = hash * 31 + OrdinalHash(ContentCatalogId);
                hash = hash * 31 + OrdinalHash(ContentVersion);
                hash = hash * 31 + OrdinalHash(LevelId);
                hash = hash * 31 + OrdinalHash(MapId);
                hash = hash * 31 + OrdinalHash(WaveSetId);
                hash = hash * 31 + OrdinalHash(RuleSetId);
                hash = hash * 31 + OrdinalHash(ThemeId);
                hash = hash * 31 + OrdinalHash(GameplayMapFingerprint);
                hash = hash * 31 + OrdinalHash(DefinitionFingerprint);
                return hash;
            }
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        private static int OrdinalHash(string value)
        {
            return StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
        }
    }

    internal sealed class CatalogResolvedBattleSource
    {
        public ResolvedLevelDefinition ResolvedLevel { get; }
        public ResolvedBattleSourceIdentity Identity { get; }

        public CatalogResolvedBattleSource(ResolvedLevelDefinition resolvedLevel,
            ResolvedBattleSourceIdentity identity)
        {
            ResolvedLevel = resolvedLevel ?? throw new ArgumentNullException(nameof(resolvedLevel));
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }

    internal static class ResolvedBattleSourceFingerprint
    {
        public static string Compute(CompiledLevelCatalog catalog,
            ResolvedLevelDefinition resolved)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (resolved == null) throw new ArgumentNullException(nameof(resolved));

            var projection = new FingerprintProjection();
            projection.Add("levelCatalogId", catalog.CatalogId);
            projection.Add("contentCatalogId", catalog.ContentCatalogId);
            projection.Add("contentVersion", catalog.ContentVersion);
            projection.Add("levelId", resolved.Identity.LevelId);
            projection.Add("mapId", resolved.Identity.MapId);
            projection.Add("waveSetId", resolved.Identity.WaveSetId);
            projection.Add("ruleSetId", resolved.Identity.RuleSetId);
            projection.Add("themeId", resolved.Identity.ThemeId);
            projection.Add("gameplayMapFingerprint", resolved.Map.GameplayFingerprint);
            projection.Add("compiledContent", BattleContentJson.SerializeCanonical(
                CreateCatalogProjection(catalog.BattleContent), false));

            projection.Add("waveSet.id", resolved.WaveSet.WaveSetId);
            projection.Add("waveSet.count", resolved.OrderedWaves.Count);
            for (var index = 0; index < resolved.OrderedWaves.Count; index++)
            {
                var wave = resolved.OrderedWaves[index];
                var prefix = "wave[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                projection.Add(prefix + ".id", wave.id);
                projection.Add(prefix + ".index", wave.index);
                projection.Add(prefix + ".healthMultiplier", wave.healthMultiplier);
                projection.Add(prefix + ".speedMultiplier", wave.speedMultiplier);
                projection.Add(prefix + ".spawnIntervalSeconds", wave.spawnIntervalSeconds);
                projection.Add(prefix + ".completionReward", wave.completionReward);
                projection.Add(prefix + ".enemyCount", wave.enemyIds.Length);
                for (var enemyIndex = 0; enemyIndex < wave.enemyIds.Length; enemyIndex++)
                    projection.Add(prefix + ".enemy[" + enemyIndex.ToString(
                        CultureInfo.InvariantCulture) + "]", wave.enemyIds[enemyIndex]);
            }

            var rules = resolved.RuleSet;
            projection.Add("rules.id", rules.RuleSetId);
            projection.Add("rules.initialSun", rules.InitialSun);
            projection.Add("rules.initialLives", rules.InitialLives);
            projection.Add("rules.maxWaves", rules.MaxWaves);
            projection.Add("rules.initialPotCount", rules.InitialPotCount);
            projection.Add("rules.betweenWaveSeconds", rules.BetweenWaveSeconds);
            projection.Add("rules.nurserySlotCount", rules.NurserySlotCount);
            projection.Add("rules.nurseryProfileId", rules.NurseryProfileId);
            projection.Add("rules.relocationCooldownSeconds", rules.RelocationCooldownSeconds);
            projection.Add("rules.refreshBaseCost", rules.RefreshBaseCost);
            projection.Add("rules.refreshCostStep", rules.RefreshCostStep);
            projection.Add("rules.milestoneCount", rules.MilestoneRewards.Count);
            for (var index = 0; index < rules.MilestoneRewards.Count; index++)
            {
                var reward = rules.MilestoneRewards[index];
                var prefix = "rules.milestone[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                projection.Add(prefix + ".wave", reward.Wave);
                projection.Add(prefix + ".potCount", reward.PotCount);
                projection.Add(prefix + ".equipmentCount", reward.EquipmentIds.Count);
                for (var equipmentIndex = 0; equipmentIndex < reward.EquipmentIds.Count;
                    equipmentIndex++)
                    projection.Add(prefix + ".equipment[" + equipmentIndex.ToString(
                        CultureInfo.InvariantCulture) + "]", reward.EquipmentIds[equipmentIndex]);
            }

            var theme = resolved.Theme;
            projection.Add("theme.id", theme.ThemeId);
            projection.Add("theme.displayName", theme.DisplayName);
            projection.Add("theme.backgroundColor", theme.BackgroundColor);
            projection.Add("theme.groundColor", theme.GroundColor);
            projection.Add("theme.routeColor", theme.RouteColor);
            projection.Add("theme.routeEdgeColor", theme.RouteEdgeColor);
            projection.Add("theme.plantableColor", theme.PlantableColor);
            projection.Add("theme.blockedColor", theme.BlockedColor);
            projection.Add("theme.coreColor", theme.CoreColor);
            projection.Add("theme.accentColor", theme.AccentColor);
            projection.Add("theme.terrainPaletteId", theme.TerrainPaletteId);

            return projection.Hash();
        }

        private static BattleContentCatalogDto CreateCatalogProjection(
            CompiledBattleContentCatalog content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            return new BattleContentCatalogDto
            {
                header = content.Header,
                battleRules = content.BattleRules,
                plants = content.Plants.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                enemies = content.Enemies.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                equipment = content.Equipment.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                abilities = content.Abilities.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                projectiles = content.Projectiles.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                statuses = content.Statuses.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                waves = content.Waves.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                upgradeProfiles = content.UpgradeProfiles.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
                nurseryProfiles = content.NurseryProfiles.Values.OrderBy(value => value.id,
                    StringComparer.Ordinal).ToArray(),
            };
        }

        private sealed class FingerprintProjection
        {
            private readonly StringBuilder _builder = new StringBuilder(32768);

            public void Add(string key, string value)
            {
                AppendToken(key);
                AppendToken(value);
            }

            public void Add(string key, int value)
            {
                Add(key, value.ToString(CultureInfo.InvariantCulture));
            }

            public void Add(string key, float value)
            {
                Add(key, value.ToString("R", CultureInfo.InvariantCulture));
            }

            public string Hash()
            {
                using (var hash = SHA256.Create())
                {
                    var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(_builder.ToString()));
                    var result = new StringBuilder(bytes.Length * 2);
                    foreach (var value in bytes)
                        result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                    return result.ToString();
                }
            }

            private void AppendToken(string value)
            {
                value = value ?? string.Empty;
                _builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(value).Append('|');
            }
        }
    }
}
