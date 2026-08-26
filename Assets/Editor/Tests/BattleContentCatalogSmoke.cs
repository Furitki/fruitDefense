using System;
using System.Collections.Generic;
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
            ValidateCanonicalStableDefinitions(compiled);
            ValidateDeepCopy(roundTrip, compiled);
            ValidateInvalidDiagnostics(roundTrip);
        }

        private static void ValidateCompiledCounts(CompiledBattleContentCatalog compiled)
        {
            Expect(compiled.Plants.Count == 5, "Compiled plant count mismatch.");
            Expect(compiled.Enemies.Count == 4, "Compiled enemy count mismatch.");
            Expect(compiled.Equipment.Count == 3, "Compiled equipment count mismatch.");
            Expect(compiled.Abilities.Count == 8, "Compiled Ability count mismatch.");
            Expect(compiled.Projectiles.Count == 3, "Compiled projectile count mismatch.");
            Expect(compiled.Statuses.Count == 4, "Compiled status count mismatch.");
            Expect(compiled.Waves.Count == 15, "Compiled wave count mismatch.");
            Expect(compiled.StarTiers.Count == 4, "Compiled star-tier count mismatch.");
        }

        private static void ValidateCanonicalStableDefinitions(CompiledBattleContentCatalog compiled)
        {
            ExpectKeys(compiled.Plants.Keys, new[]
            {
                BattleContentIds.Plants.Pea, BattleContentIds.Plants.Watermelon,
                BattleContentIds.Plants.Banana, BattleContentIds.Plants.Durian,
                BattleContentIds.Plants.Sunflower,
            }, "plant");
            ExpectKeys(compiled.Enemies.Keys, new[]
            {
                BattleContentIds.Enemies.Normal, BattleContentIds.Enemies.Runner,
                BattleContentIds.Enemies.Armored, BattleContentIds.Enemies.Boss,
            }, "enemy");
            ExpectKeys(compiled.Equipment.Keys, new[]
            {
                BattleContentIds.Equipment.Gatling, BattleContentIds.Equipment.Ice,
                BattleContentIds.Equipment.Chili,
            }, "equipment");
            ExpectKeys(compiled.Abilities.Keys, new[]
            {
                BattleContentIds.Abilities.PeaAttack, BattleContentIds.Abilities.WatermelonAttack,
                BattleContentIds.Abilities.BananaAttack, BattleContentIds.Abilities.DurianAttack,
                BattleContentIds.Abilities.SunflowerProduce, BattleContentIds.Abilities.IceOnHit,
                BattleContentIds.Abilities.IceProducerOpening, BattleContentIds.Abilities.ChiliOnHit,
            }, "Ability");
            ExpectKeys(compiled.Projectiles.Keys, new[]
            {
                BattleContentIds.Projectiles.Pea, BattleContentIds.Projectiles.Watermelon,
                BattleContentIds.Projectiles.Banana,
            }, "projectile");
            ExpectKeys(compiled.Statuses.Keys, new[]
            {
                BattleContentIds.Statuses.IceSlow, BattleContentIds.Statuses.IceFreeze,
                BattleContentIds.Statuses.IceCount, BattleContentIds.Statuses.ChiliBurn,
            }, "status");
            ExpectKeys(compiled.StarTiers.Keys,
                new[] { "star.1", "star.2", "star.3", "star.4" }, "star tier");

            var expectedWaveIds = Enumerable.Range(1, 15)
                .Select(index => "wave." + index.ToString("00")).ToArray();
            ExpectKeys(compiled.Waves.Keys, expectedWaveIds, "wave");
            for (var index = 1; index <= expectedWaveIds.Length; index++)
            {
                var id = expectedWaveIds[index - 1];
                var definition = compiled.Waves[id];
                Expect(definition.index == index, id + " index mismatch.");
                Expect(definition.enemyIds.Length > 0, id + " has no enemy sequence.");
                Expect(definition.enemyIds.All(compiled.Enemies.ContainsKey),
                    id + " contains an unknown stable enemy ID.");
            }

            var rules = compiled.BattleRules;
            Expect(rules.initialSun == 10 && rules.initialLives == 10, "Initial battle rules mismatch.");
            Expect(rules.maxWaves == 15, "Max waves rule mismatch.");
            Expect(rules.initialPotCount == 8, "Initial pot count mismatch.");
            Approximately(rules.betweenWaveSeconds, 15f, "between-wave seconds");
            Expect(rules.refreshBaseCost == 10, "Refresh base cost mismatch.");
            Expect(rules.refreshCostStep == 5, "Refresh step mismatch.");
            Expect(rules.milestoneRewards.Length == 4, "Milestone count mismatch.");
        }

        private static void ExpectKeys(IEnumerable<string> actual, IEnumerable<string> expected, string category)
        {
            var actualIds = actual.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var expectedIds = expected.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Expect(actualIds.SequenceEqual(expectedIds), "Canonical " + category + " IDs mismatch: expected ["
                + string.Join(",", expectedIds) + "], got [" + string.Join(",", actualIds) + "].");
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
            invalid.plants[0].abilityIds = new[] { "ability.missing" };
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
