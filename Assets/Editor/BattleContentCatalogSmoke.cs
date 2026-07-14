using System;
using System.Linq;
using System.Text;
using FruitDefense.Content;
using FruitDefense.Core;

namespace FruitDefense.Editor
{
    internal static class BattleContentCatalogSmoke
    {
        public static void Run(BattleContentCatalogDto authored, string committedJson)
        {
            Expect(authored != null, "Authored catalog is missing.");
            var authoredValidation = BattleContentValidator.ValidateBundledBaseline(authored);
            ExpectValid(authoredValidation, "Authored catalog");

            var canonical = BattleContentJson.SerializeCanonical(authored);
            Expect(canonical == Normalize(committedJson), "Committed JSON differs from canonical authored content.");
            var second = BattleContentJson.SerializeCanonical(authored);
            Expect(ByteEqual(Encoding.UTF8.GetBytes(canonical), Encoding.UTF8.GetBytes(second)),
                "Repeated canonical export was not byte-identical.");

            var roundTrip = BattleContentJson.Deserialize(committedJson);
            ExpectValid(BattleContentValidator.ValidateBundledBaseline(roundTrip), "Round-tripped JSON catalog");
            Expect(BattleContentJson.SerializeCanonical(roundTrip) == canonical, "JSON round trip changed canonical content.");

            CompiledBattleContentCatalog compiled;
            ContentValidationResult compileValidation;
            Expect(BattleContentCompiler.TryCompile(roundTrip, out compiled, out compileValidation),
                "Valid bundled catalog failed compilation.");
            ExpectValid(compileValidation, "Compiled catalog");
            ValidateCompiledCounts(compiled);
            ValidateLegacyParity(compiled);
            ValidateDeepCopy(roundTrip, compiled);
            ValidateInvalidDiagnostics(roundTrip);
        }

        private static void ValidateCompiledCounts(CompiledBattleContentCatalog compiled)
        {
            Expect(compiled.Plants.Count == 5, "Compiled plant count mismatch.");
            Expect(compiled.Enemies.Count == 4, "Compiled enemy count mismatch.");
            Expect(compiled.Equipment.Count == 3, "Compiled equipment count mismatch.");
            Expect(compiled.Skills.Count == 8, "Compiled skill count mismatch.");
            Expect(compiled.Projectiles.Count == 3, "Compiled projectile count mismatch.");
            Expect(compiled.Statuses.Count == 5, "Compiled status count mismatch.");
            Expect(compiled.Waves.Count == 15, "Compiled wave count mismatch.");
            Expect(compiled.StarTiers.Count == 4, "Compiled star-tier count mismatch.");
        }

        private static void ValidateLegacyParity(CompiledBattleContentCatalog compiled)
        {
            foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
            {
                var id = LegacyBattleContentIds.Plant(kind);
                PlantDefinitionDto definition;
                Expect(compiled.Plants.TryGetValue(id, out definition), "Missing mapped plant " + id + ".");
                var legacy = GameConfig.Plant(kind);
                Approximately(definition.damage, legacy.Damage, id + " damage");
                Approximately(definition.attackIntervalSeconds, legacy.Interval, id + " interval");
                Approximately(definition.rangeLegacyUnits, GameConfig.LegacyDistance(legacy.Range), id + " range");
            }

            foreach (ZombieKind kind in Enum.GetValues(typeof(ZombieKind)))
            {
                var id = LegacyBattleContentIds.Enemy(kind);
                EnemyDefinitionDto definition;
                Expect(compiled.Enemies.TryGetValue(id, out definition), "Missing mapped enemy " + id + ".");
                var legacy = GameConfig.Zombie(kind);
                Approximately(definition.health, legacy.Hp, id + " health");
                Approximately(definition.speedLegacyUnits, GameConfig.LegacyDistance(legacy.Speed), id + " speed");
                Expect(definition.killReward == legacy.Reward, id + " reward mismatch.");
                Expect(definition.threat == legacy.Threat, id + " threat mismatch.");
            }

            foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
            {
                string id;
                var mapped = LegacyBattleContentIds.TryEquipment(kind, out id);
                if (kind == WeaponKind.None)
                {
                    Expect(!mapped && string.IsNullOrEmpty(id), "WeaponKind.None must not map to content.");
                    continue;
                }
                Expect(mapped && compiled.Equipment.ContainsKey(id), "Missing mapped equipment for " + kind + ".");
            }

            for (var star = 1; star <= 4; star++)
            {
                var tier = compiled.StarTiers["star." + star];
                Approximately(tier.damageMultiplier, GameConfig.StarDamage(star), "star damage " + star);
                Approximately(tier.attackSpeedMultiplier, GameConfig.StarSpeed(star), "star speed " + star);
                Approximately(tier.rangeMultiplier, GameConfig.StarRange(star), "star range " + star);
            }

            for (var index = 1; index <= GameConfig.MaxWaves; index++)
            {
                var id = "wave." + index.ToString("00");
                var definition = compiled.Waves[id];
                var legacy = GameConfig.GetWave(index);
                Expect(definition.index == legacy.Index, id + " index mismatch.");
                Approximately(definition.healthMultiplier, legacy.HpMultiplier, id + " health multiplier");
                Approximately(definition.speedMultiplier, legacy.SpeedMultiplier, id + " speed multiplier");
                Approximately(definition.spawnIntervalSeconds, legacy.SpawnInterval, id + " spawn interval");
                Expect(definition.completionReward == legacy.Reward, id + " completion reward mismatch.");
                Expect(definition.enemyIds.Length == legacy.Sequence.Count, id + " sequence count mismatch.");
                for (var entry = 0; entry < definition.enemyIds.Length; entry++)
                    Expect(definition.enemyIds[entry] == LegacyBattleContentIds.Enemy(legacy.Sequence[entry]),
                        id + " sequence mismatch at " + entry + ".");
            }

            var rules = compiled.BattleRules;
            Expect(rules.initialSun == 10 && rules.initialLives == 10, "Initial battle rules mismatch.");
            Expect(rules.maxWaves == GameConfig.MaxWaves, "Max waves rule mismatch.");
            Expect(rules.initialPotCount == GameConfig.InitialPotCount, "Initial pot count mismatch.");
            Approximately(rules.betweenWaveSeconds, GameConfig.BetweenWaveSeconds, "between-wave seconds");
            Expect(rules.refreshBaseCost == GameConfig.RefreshCost(0), "Refresh base cost mismatch.");
            Expect(rules.refreshCostStep == GameConfig.RefreshCost(1) - GameConfig.RefreshCost(0), "Refresh step mismatch.");
            Expect(rules.milestoneRewards.Length == 4, "Milestone count mismatch.");
        }

        private static void ValidateDeepCopy(BattleContentCatalogDto source, CompiledBattleContentCatalog compiled)
        {
            var copy = BattleContentJson.DeepCopy(source);
            CompiledBattleContentCatalog isolated;
            ContentValidationResult validation;
            Expect(BattleContentCompiler.TryCompile(copy, out isolated, out validation), "Deep-copy isolation setup failed.");
            var id = copy.plants[0].id;
            var before = isolated.Plants[id].damage;
            copy.plants[0].damage += 999f;
            Approximately(isolated.Plants[id].damage, before, "compiled source isolation");
            Expect(!ReferenceEquals(compiled.Plants[id], source.plants.First(value => value.id == id)),
                "Compiled definition retained an authoring reference.");
        }

        private static void ValidateInvalidDiagnostics(BattleContentCatalogDto source)
        {
            var invalid = BattleContentJson.DeepCopy(source);
            invalid.plants[1].id = invalid.plants[0].id;
            invalid.plants[0].skillIds = new[] { "skill.missing" };
            invalid.enemies[0].health = -1f;
            var validation = BattleContentValidator.Validate(invalid);
            Expect(!validation.IsValid, "Invalid catalog unexpectedly passed validation.");
            Expect(validation.Issues.Any(issue => issue.code == "definition.id.duplicate"), "Duplicate ID diagnostic missing.");
            Expect(validation.Issues.Any(issue => issue.code == "reference.missing"), "Missing reference diagnostic missing.");
            Expect(validation.Issues.Any(issue => issue.code == "definition.numeric.invalid" && issue.category == "enemies"),
                "Invalid enemy numeric diagnostic missing.");

            CompiledBattleContentCatalog ignored;
            ContentValidationResult compileValidation;
            Expect(!BattleContentCompiler.TryCompile(invalid, out ignored, out compileValidation) && ignored == null,
                "Invalid catalog unexpectedly compiled.");
            Expect(compileValidation.Issues.Count >= 3, "Compiler did not return complete diagnostics.");
        }

        private static void ExpectValid(ContentValidationResult result, string label)
        {
            if (result.IsValid) return;
            throw new InvalidOperationException(label + " validation failed:\n" + string.Join("\n",
                result.Issues.Select(issue => issue.ToString()).ToArray()));
        }

        private static void Approximately(float actual, float expected, string label)
        {
            Expect(Math.Abs(actual - expected) <= .0001f, label + " mismatch: expected " + expected + ", got " + actual + ".");
        }

        private static string Normalize(string value)
        {
            var normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.EndsWith("\n", StringComparison.Ordinal) ? normalized : normalized + "\n";
        }

        private static bool ByteEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
