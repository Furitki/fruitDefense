using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class GameContentManifestDto
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string manifestId = "manifest.fruit-defense.bundled";
        public string battleCatalogResourcePath = "Content/battle-content-bundled.v3";
        public string battleCatalogId = BattleContentSchema.BundledCatalogId;
        public string battleContentVersion = BattleContentSchema.BundledContentVersion;
        public string levelCatalogId = BundledLevelCatalogIds.Catalog;
        public string presentationCatalogId = "presentation.orchard.bundled";
        public string defaultNurseryProfileId = BattleContentIds.NurseryProfiles.Baseline;
    }

    public static class GameContentManifestJson
    {
        public static GameContentManifestDto Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Game-content manifest JSON is empty.", nameof(json));
            var value = JsonUtility.FromJson<GameContentManifestDto>(json);
            if (value == null)
                throw new InvalidOperationException("Game-content manifest JSON could not be deserialized.");
            return value;
        }

        public static GameContentManifestDto DeepCopy(GameContentManifestDto source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Deserialize(JsonUtility.ToJson(source, false));
        }

        public static string SerializeCanonical(GameContentManifestDto source, bool prettyPrint = true)
        {
            var copy = DeepCopy(source);
            return JsonUtility.ToJson(copy, prettyPrint)
                .Replace("\r\n", "\n").Replace('\r', '\n') + "\n";
        }

        public static byte[] SerializeCanonicalUtf8(GameContentManifestDto source,
            bool prettyPrint = true)
        {
            return new UTF8Encoding(false).GetBytes(SerializeCanonical(source, prettyPrint));
        }
    }

    public static class GameContentManifestValidator
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$", RegexOptions.CultureInvariant);
        private static readonly Regex ResourcePathPattern = new Regex(
            "^[A-Za-z0-9][A-Za-z0-9_./-]*$", RegexOptions.CultureInvariant);

        public static ContentValidationResult Validate(GameContentManifestDto manifest,
            BattleContentCatalogDto battleCatalog = null, string expectedLevelCatalogId = null)
        {
            var result = new ContentValidationResult();
            if (manifest == null)
            {
                result.Add("manifest.null", "manifest", string.Empty, string.Empty,
                    "Game-content manifest is required.");
                return result;
            }

            if (manifest.schemaVersion != GameContentManifestDto.CurrentSchemaVersion)
                result.Add("manifest.schema.unsupported", "manifest", manifest.manifestId,
                    "schemaVersion", "Unsupported game-content manifest schema version.");
            RequireId(manifest.manifestId, "manifestId", result, manifest.manifestId);
            RequireId(manifest.battleCatalogId, "battleCatalogId", result, manifest.manifestId);
            RequireId(manifest.levelCatalogId, "levelCatalogId", result, manifest.manifestId);
            RequireId(manifest.presentationCatalogId, "presentationCatalogId", result,
                manifest.manifestId);
            RequireId(manifest.defaultNurseryProfileId, "defaultNurseryProfileId", result,
                manifest.manifestId);
            if (string.IsNullOrWhiteSpace(manifest.battleContentVersion))
                result.Add("manifest.version.invalid", "manifest", manifest.manifestId,
                    "battleContentVersion", "Battle content version is required.");
            if (string.IsNullOrWhiteSpace(manifest.battleCatalogResourcePath)
                || !ResourcePathPattern.IsMatch(manifest.battleCatalogResourcePath)
                || manifest.battleCatalogResourcePath.StartsWith("/", StringComparison.Ordinal)
                || manifest.battleCatalogResourcePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                result.Add("manifest.resource-path.invalid", "manifest", manifest.manifestId,
                    "battleCatalogResourcePath",
                    "Battle catalog resource path must be a Resources-relative path without extension.");

            if (battleCatalog != null)
            {
                if (battleCatalog.header == null
                    || !string.Equals(manifest.battleCatalogId, battleCatalog.header.catalogId,
                        StringComparison.Ordinal)
                    || !string.Equals(manifest.battleContentVersion,
                        battleCatalog.header.contentVersion, StringComparison.Ordinal))
                    result.Add("manifest.battle-content.mismatch", "manifest", manifest.manifestId,
                        "battleCatalogId", "Manifest and battle catalog identities do not match.");
                if (battleCatalog.nurseryProfiles == null
                    || Array.Find(battleCatalog.nurseryProfiles, value => value != null
                        && string.Equals(value.id, manifest.defaultNurseryProfileId,
                            StringComparison.Ordinal)) == null)
                    result.Add("manifest.nursery-profile.missing", "manifest", manifest.manifestId,
                        "defaultNurseryProfileId", "Default nursery profile is not present in battle content.");
            }

            if (!string.IsNullOrEmpty(expectedLevelCatalogId)
                && !string.Equals(manifest.levelCatalogId, expectedLevelCatalogId,
                    StringComparison.Ordinal))
                result.Add("manifest.level-catalog.mismatch", "manifest", manifest.manifestId,
                    "levelCatalogId", "Manifest level catalog identity does not match the bundled catalog.");
            return result;
        }

        private static void RequireId(string value, string field,
            ContentValidationResult result, string itemId)
        {
            if (!string.IsNullOrWhiteSpace(value) && StableIdPattern.IsMatch(value)) return;
            result.Add("manifest.identity.invalid", "manifest", itemId, field,
                "Value must be a stable lowercase semantic ID.");
        }
    }

    public static class BundledGameContentLoader
    {
        public const string ManifestResourcePath = "Content/game-content-manifest.v1";

        public static bool TryLoad(out GameContentManifestDto manifest,
            out CompiledBattleContentCatalog compiled, out ContentValidationResult validation)
        {
            manifest = null;
            compiled = null;
            validation = new ContentValidationResult();
            var manifestText = Resources.Load<TextAsset>(ManifestResourcePath);
            if (manifestText == null)
            {
                validation.Add("manifest.resource.missing", "manifest", string.Empty,
                    ManifestResourcePath, "Bundled game-content manifest resource is missing.");
                return false;
            }

            try
            {
                manifest = GameContentManifestJson.Deserialize(manifestText.text);
            }
            catch (Exception exception)
            {
                validation.Add("manifest.deserialize.failed", "manifest", string.Empty,
                    ManifestResourcePath, exception.Message);
                return false;
            }

            var manifestOnly = GameContentManifestValidator.Validate(manifest);
            validation.Append(manifestOnly);
            if (!validation.IsValid) return false;

            var battleText = Resources.Load<TextAsset>(manifest.battleCatalogResourcePath);
            if (battleText == null)
            {
                validation.Add("battle-content.resource.missing", "manifest", manifest.manifestId,
                    "battleCatalogResourcePath", "Referenced bundled battle catalog is missing.");
                return false;
            }

            BattleContentCatalogDto catalog;
            try
            {
                catalog = BattleContentJson.Deserialize(battleText.text);
            }
            catch (Exception exception)
            {
                validation.Add("battle-content.deserialize.failed", "manifest", manifest.manifestId,
                    "battleCatalogResourcePath", exception.Message);
                return false;
            }

            validation.Append(GameContentManifestValidator.Validate(manifest, catalog,
                BundledLevelCatalogIds.Catalog));
            validation.Append(BattleContentValidator.ValidateBundledBaseline(catalog));
            if (!validation.IsValid) return false;
            ContentValidationResult contentValidation;
            if (!BattleContentCompiler.TryCompile(catalog, out compiled, out contentValidation))
            {
                validation.Append(contentValidation);
                return false;
            }
            return true;
        }
    }
}
