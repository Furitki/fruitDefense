using System;
using System.Linq;
using FruitDefense.Content;

namespace FruitDefense.Editor
{
    internal static class GameContentManifestSmoke
    {
        public static void Run(GameContentManifestDto authored, string committedJson,
            BattleContentCatalogDto battleCatalog)
        {
            Expect(authored != null, "Authored game-content manifest is missing.");
            ExpectValid(GameContentManifestValidator.Validate(authored, battleCatalog,
                BundledLevelCatalogIds.Catalog), "Authored game-content manifest");

            var canonical = GameContentManifestJson.SerializeCanonical(authored);
            Expect(canonical == Normalize(committedJson),
                "Committed manifest JSON differs from canonical authored content.");
            var roundTrip = GameContentManifestJson.Deserialize(committedJson);
            ExpectValid(GameContentManifestValidator.Validate(roundTrip, battleCatalog,
                BundledLevelCatalogIds.Catalog), "Round-tripped game-content manifest");
            Expect(GameContentManifestJson.SerializeCanonical(roundTrip) == canonical,
                "Manifest JSON round trip changed canonical content.");

            var invalid = GameContentManifestJson.DeepCopy(authored);
            invalid.battleContentVersion = "missing-version";
            var invalidResult = GameContentManifestValidator.Validate(invalid, battleCatalog,
                BundledLevelCatalogIds.Catalog);
            Expect(!invalidResult.IsValid && invalidResult.Issues.Any(value =>
                    value.code == "manifest.battle-content.mismatch"),
                "Manifest/catalog identity mismatch diagnostic is missing.");
        }

        private static string Normalize(string value)
        {
            var normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.EndsWith("\n", StringComparison.Ordinal)
                ? normalized
                : normalized + "\n";
        }

        private static void ExpectValid(ContentValidationResult validation, string label)
        {
            if (validation.IsValid) return;
            throw new InvalidOperationException(label + " validation failed:\n"
                + string.Join("\n", validation.Issues.Select(value => value.ToString()).ToArray()));
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
