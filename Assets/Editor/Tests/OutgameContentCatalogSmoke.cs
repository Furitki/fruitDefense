using System;
using System.IO;
using System.Linq;
using FruitDefense.Content;

namespace FruitDefense.Editor
{
    internal static class OutgameContentCatalogSmoke
    {
        public static void Run(OutgameContentCatalogDto authored,
            string committedJson, BattleContentCatalogDto battle,
            GameContentManifestDto manifest)
        {
            Expect(authored != null, "Authored outgame catalog is missing.");
            ExpectValid(OutgameContentValidator.ValidateBundledBaseline(authored,
                BundledLevelCatalogFactory.CreateSource()), "Authored outgame catalog");

            var canonical = OutgameContentJson.SerializeCanonical(authored);
            Expect(canonical == Normalize(committedJson),
                "Committed outgame JSON differs from canonical authored content.");
            var roundTrip = OutgameContentJson.Deserialize(committedJson);
            ExpectValid(OutgameContentValidator.ValidateBundledBaseline(roundTrip,
                BundledLevelCatalogFactory.CreateSource()),
                "Round-tripped outgame catalog");
            Expect(OutgameContentJson.SerializeCanonical(roundTrip) == canonical,
                "Outgame JSON round trip changed canonical content.");

            CompiledOutgameContentCatalog compiled;
            ContentValidationResult validation;
            Expect(OutgameContentCompiler.TryCompile(roundTrip,
                    BundledLevelCatalogFactory.CreateSource(), out compiled,
                    out validation), "Outgame catalog did not compile: "
                + Format(validation));
            Expect(compiled.ResolveItem(OutgameContentIds.Items.MorningDew) != null,
                "Compiled item index did not resolve the starter material.");
            Expect(compiled.ResolveActivity(
                    OutgameContentIds.Activities.StarterSupplies) != null,
                "Compiled activity index did not resolve the starter activity.");
            Expect(compiled.ResolveGrowthEquipment(
                    OutgameContentIds.GrowthEquipment.SunleafEmblem) != null,
                "Compiled equipment index did not resolve the starter equipment.");
            Expect(compiled.ResolveCultivationNode(
                    OutgameContentIds.CultivationNodes.VitalRoots) != null,
                "Compiled cultivation index did not resolve the starter node.");
            foreach (var level in BundledLevelCatalogFactory.CreateSource().Levels)
                Expect(compiled.ResolveGrowthPolicy(level.GrowthPolicyId) != null,
                    "Playable level does not resolve its growth policy: "
                    + level.LevelId);
            ValidateInvalidCatalogs(authored);
            ValidateCompiledIsolation(authored);
            ValidateLastValidExportPreservation(battle, authored, manifest);
        }

        private static void ValidateInvalidCatalogs(
            OutgameContentCatalogDto authored)
        {
            var invalid = OutgameContentJson.DeepCopy(authored);
            invalid.items[0].id = "Invalid Item";
            ExpectInvalid(invalid, "outgame.identity.invalid", "invalid ID");

            invalid = OutgameContentFixture.WithMissingCostItem(authored);
            ExpectInvalid(invalid, "outgame.reference.missing",
                "missing cost item reference");

            invalid = OutgameContentFixture.WithUnsupportedOperation(authored);
            ExpectInvalid(invalid, "outgame.contribution.operation.unsupported",
                "unsupported growth operation");

            invalid = OutgameContentJson.DeepCopy(authored);
            invalid.growthEquipment[0].ranks[1].rank = 3;
            ExpectInvalid(invalid, "outgame.rank.sequence.invalid",
                "non-consecutive equipment rank");

            invalid = OutgameContentFixture.WithInvalidCap(authored);
            ExpectInvalid(invalid, "outgame.policy.cap.range.invalid",
                "invalid policy cap");

            invalid = OutgameContentJson.DeepCopy(authored);
            var levels = BundledLevelCatalogFactory.CreateSource();
            var first = levels.Levels[0];
            var invalidLevel = new LevelDefinition(first.LevelId, first.MapId,
                first.WaveSetId, first.RuleSetId, first.ThemeId,
                "growth-policy.missing");
            var invalidLevels = new LevelCatalogSource(levels.CatalogId,
                levels.ContentCatalogId, levels.ContentVersion,
                levels.DefaultLevelId,
                levels.Levels.Select(value => value.LevelId == first.LevelId
                    ? invalidLevel : value), levels.Maps, levels.WaveSets,
                levels.RuleSets, levels.Themes, levels.TerrainPaletteIds);
            var validation = OutgameContentValidator.ValidateCrossCatalog(invalid,
                invalidLevels);
            Expect(!validation.IsValid && validation.Issues.Any(value =>
                    value.code == "outgame.reference.missing"
                    && value.field == "growthPolicyId"
                    && value.itemId == first.LevelId),
                "Missing level growth-policy reference diagnostic is incomplete.");
        }

        private static void ValidateCompiledIsolation(
            OutgameContentCatalogDto authored)
        {
            var source = OutgameContentJson.DeepCopy(authored);
            CompiledOutgameContentCatalog compiled;
            ContentValidationResult validation;
            Expect(OutgameContentCompiler.TryCompile(source, out compiled,
                    out validation), "Isolation catalog did not compile: "
                + Format(validation));
            source.items[0].displayName = "mutated after compilation";
            Expect(compiled.ResolveItem(OutgameContentIds.Items.MorningDew)
                    .displayName != source.items[0].displayName,
                "Compiled outgame catalog retained mutable authoring references.");
        }

        private static void ValidateLastValidExportPreservation(
            BattleContentCatalogDto battle, OutgameContentCatalogDto outgame,
            GameContentManifestDto manifest)
        {
            var root = Path.Combine(Path.GetTempPath(),
                "FruitDefense.OutgameContentSmoke." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var battlePath = Path.Combine(root, "battle.json");
                var outgamePath = Path.Combine(root, "outgame.json");
                var manifestPath = Path.Combine(root, "manifest.json");
                var sentinel = new byte[] { 11, 23, 37, 41 };
                File.WriteAllBytes(battlePath, sentinel);
                File.WriteAllBytes(outgamePath, sentinel);
                File.WriteAllBytes(manifestPath, sentinel);
                var invalid = OutgameContentJson.DeepCopy(outgame);
                invalid.growthPolicies[0].caps[0].maximumValue = float.NaN;
                ContentValidationResult validation;
                Expect(!GameContentBundleExporter.TryWrite(battle, invalid,
                        manifest, battlePath, outgamePath, manifestPath,
                        out validation),
                    "Invalid outgame catalog unexpectedly produced an export.");
                Expect(File.ReadAllBytes(battlePath).SequenceEqual(sentinel)
                    && File.ReadAllBytes(outgamePath).SequenceEqual(sentinel)
                    && File.ReadAllBytes(manifestPath).SequenceEqual(sentinel),
                    "Invalid export did not preserve the last valid bundle bytes.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void ExpectInvalid(OutgameContentCatalogDto catalog,
            string issueCode, string label)
        {
            CompiledOutgameContentCatalog compiled;
            ContentValidationResult validation;
            Expect(!OutgameContentCompiler.TryCompile(catalog, out compiled,
                    out validation), label + " unexpectedly compiled.");
            Expect(compiled == null && validation.Issues.Any(value =>
                    value.code == issueCode),
                label + " did not report " + issueCode + ".");
        }

        private static string Normalize(string value)
        {
            var normalized = (value ?? string.Empty)
                .Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.EndsWith("\n", StringComparison.Ordinal)
                ? normalized : normalized + "\n";
        }

        private static void ExpectValid(ContentValidationResult validation,
            string label)
        {
            if (validation.IsValid) return;
            throw new InvalidOperationException(label + " validation failed:\n"
                + Format(validation));
        }

        private static string Format(ContentValidationResult validation)
        {
            return validation == null ? "<null>" : string.Join("\n",
                validation.Issues.Select(value => value.ToString()).ToArray());
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
