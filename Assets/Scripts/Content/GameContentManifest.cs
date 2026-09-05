using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FruitDefense.Content
{
    [Serializable]
    public sealed class GameContentManifestDto
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public string manifestId = "manifest.fruit-defense.bundled";
        public string battleCatalogResourcePath = "Content/battle-content-bundled.v3";
        public string battleCatalogId = BattleContentSchema.BundledCatalogId;
        public string battleContentVersion = BattleContentSchema.BundledContentVersion;
        public string outgameCatalogResourcePath = "Content/outgame-content-bundled.v1";
        public string outgameCatalogId = OutgameContentSchema.BundledCatalogId;
        public string outgameContentVersion = OutgameContentSchema.BundledContentVersion;
        public string outgameContentFingerprint = string.Empty;
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
                throw new InvalidOperationException(
                    "Game-content manifest JSON could not be deserialized.");
            return value;
        }

        public static GameContentManifestDto DeepCopy(GameContentManifestDto source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Deserialize(JsonUtility.ToJson(source, false));
        }

        public static string SerializeCanonical(GameContentManifestDto source,
            bool prettyPrint = true)
        {
            var copy = DeepCopy(source);
            return JsonUtility.ToJson(copy, prettyPrint)
                .Replace("\r\n", "\n").Replace('\r', '\n') + "\n";
        }

        public static byte[] SerializeCanonicalUtf8(GameContentManifestDto source,
            bool prettyPrint = true)
        {
            return new UTF8Encoding(false).GetBytes(
                SerializeCanonical(source, prettyPrint));
        }
    }

    public static class GameContentManifestValidator
    {
        private static readonly Regex StableIdPattern = new Regex(
            "^[a-z][a-z0-9]*(?:[._-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);
        private static readonly Regex ResourcePathPattern = new Regex(
            "^[A-Za-z0-9][A-Za-z0-9_./-]*$", RegexOptions.CultureInvariant);
        private static readonly Regex Sha256Pattern = new Regex(
            "^[0-9a-f]{64}$", RegexOptions.CultureInvariant);

        public static ContentValidationResult Validate(GameContentManifestDto manifest,
            BattleContentCatalogDto battleCatalog = null,
            OutgameContentCatalogDto outgameCatalog = null,
            string expectedLevelCatalogId = null)
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
            RequireId(manifest.battleCatalogId, "battleCatalogId", result,
                manifest.manifestId);
            RequireId(manifest.outgameCatalogId, "outgameCatalogId", result,
                manifest.manifestId);
            RequireId(manifest.levelCatalogId, "levelCatalogId", result,
                manifest.manifestId);
            RequireId(manifest.presentationCatalogId, "presentationCatalogId", result,
                manifest.manifestId);
            RequireId(manifest.defaultNurseryProfileId, "defaultNurseryProfileId", result,
                manifest.manifestId);
            RequireVersion(manifest.battleContentVersion, "battleContentVersion",
                result, manifest.manifestId);
            RequireVersion(manifest.outgameContentVersion, "outgameContentVersion",
                result, manifest.manifestId);
            RequireResourcePath(manifest.battleCatalogResourcePath,
                "battleCatalogResourcePath", result, manifest.manifestId);
            RequireResourcePath(manifest.outgameCatalogResourcePath,
                "outgameCatalogResourcePath", result, manifest.manifestId);
            if (!Sha256Pattern.IsMatch(manifest.outgameContentFingerprint ?? string.Empty))
                result.Add("manifest.fingerprint.invalid", "manifest", manifest.manifestId,
                    "outgameContentFingerprint",
                    "Outgame content fingerprint must be a lowercase SHA-256 value.");

            if (battleCatalog != null)
            {
                if (battleCatalog.header == null
                    || !string.Equals(manifest.battleCatalogId,
                        battleCatalog.header.catalogId, StringComparison.Ordinal)
                    || !string.Equals(manifest.battleContentVersion,
                        battleCatalog.header.contentVersion, StringComparison.Ordinal))
                    result.Add("manifest.battle-content.mismatch", "manifest",
                        manifest.manifestId, "battleCatalogId",
                        "Manifest and battle catalog identities do not match.");
                if (battleCatalog.nurseryProfiles == null
                    || Array.Find(battleCatalog.nurseryProfiles, value => value != null
                        && string.Equals(value.id, manifest.defaultNurseryProfileId,
                            StringComparison.Ordinal)) == null)
                    result.Add("manifest.nursery-profile.missing", "manifest",
                        manifest.manifestId, "defaultNurseryProfileId",
                        "Default nursery profile is not present in battle content.");
            }

            if (outgameCatalog != null)
            {
                if (outgameCatalog.header == null
                    || !string.Equals(manifest.outgameCatalogId,
                        outgameCatalog.header.catalogId, StringComparison.Ordinal)
                    || !string.Equals(manifest.outgameContentVersion,
                        outgameCatalog.header.contentVersion, StringComparison.Ordinal))
                    result.Add("manifest.outgame-content.mismatch", "manifest",
                        manifest.manifestId, "outgameCatalogId",
                        "Manifest and outgame catalog identities do not match.");
                var fingerprint = OutgameContentJson.ComputeFingerprint(outgameCatalog);
                if (!string.Equals(manifest.outgameContentFingerprint, fingerprint,
                        StringComparison.Ordinal))
                    result.Add("manifest.outgame-fingerprint.mismatch", "manifest",
                        manifest.manifestId, "outgameContentFingerprint",
                        "Manifest outgame fingerprint '"
                        + manifest.outgameContentFingerprint
                        + "' does not match canonical content '" + fingerprint + "'.");
            }

            if (!string.IsNullOrEmpty(expectedLevelCatalogId)
                && !string.Equals(manifest.levelCatalogId, expectedLevelCatalogId,
                    StringComparison.Ordinal))
                result.Add("manifest.level-catalog.mismatch", "manifest",
                    manifest.manifestId, "levelCatalogId",
                    "Manifest level catalog identity does not match the bundled catalog.");
            return result;
        }

        private static void RequireVersion(string value, string field,
            ContentValidationResult result, string itemId)
        {
            if (!string.IsNullOrWhiteSpace(value)) return;
            result.Add("manifest.version.invalid", "manifest", itemId, field,
                "Content version is required.");
        }

        private static void RequireResourcePath(string value, string field,
            ContentValidationResult result, string itemId)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && ResourcePathPattern.IsMatch(value)
                && !value.StartsWith("/", StringComparison.Ordinal)
                && !value.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return;
            result.Add("manifest.resource-path.invalid", "manifest", itemId, field,
                "Catalog resource path must be Resources-relative and omit the extension.");
        }

        private static void RequireId(string value, string field,
            ContentValidationResult result, string itemId)
        {
            if (!string.IsNullOrWhiteSpace(value) && StableIdPattern.IsMatch(value)) return;
            result.Add("manifest.identity.invalid", "manifest", itemId, field,
                "Value must be a stable lowercase semantic ID.");
        }
    }

    public sealed class CompiledGameContentBundle
    {
        public GameContentManifestDto Manifest { get; private set; }
        public CompiledBattleContentCatalog Battle { get; private set; }
        public CompiledOutgameContentCatalog Outgame { get; private set; }

        internal CompiledGameContentBundle(GameContentManifestDto manifest,
            CompiledBattleContentCatalog battle,
            CompiledOutgameContentCatalog outgame)
        {
            Manifest = GameContentManifestJson.DeepCopy(manifest);
            Battle = battle;
            Outgame = outgame;
        }
    }

    public static class BundledGameContentLoader
    {
        public const string ManifestResourcePath = "Content/game-content-manifest.v2";

        public static bool TryLoadBundle(out CompiledGameContentBundle bundle,
            out ContentValidationResult validation)
        {
            bundle = null;
            validation = new ContentValidationResult();
            var manifestText = Resources.Load<TextAsset>(ManifestResourcePath);
            if (manifestText == null)
            {
                validation.Add("manifest.resource.missing", "manifest", string.Empty,
                    ManifestResourcePath,
                    "Bundled game-content manifest resource is missing.");
                return false;
            }

            GameContentManifestDto manifest;
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

            validation.Append(GameContentManifestValidator.Validate(manifest));
            if (!validation.IsValid) return false;

            BattleContentCatalogDto battleCatalog;
            if (!TryLoadBattle(manifest, validation, out battleCatalog)) return false;
            OutgameContentCatalogDto outgameCatalog;
            if (!TryLoadOutgame(manifest, validation, out outgameCatalog)) return false;

            var levels = BundledLevelCatalogFactory.CreateSource();
            validation.Append(GameContentManifestValidator.Validate(manifest,
                battleCatalog, outgameCatalog, BundledLevelCatalogIds.Catalog));
            validation.Append(BattleContentValidator.ValidateBundledBaseline(battleCatalog));
            validation.Append(OutgameContentValidator.ValidateBundledBaseline(
                outgameCatalog, levels));
            if (!validation.IsValid) return false;

            ContentValidationResult contentValidation;
            CompiledBattleContentCatalog battle;
            if (!BattleContentCompiler.TryCompile(battleCatalog, out battle,
                    out contentValidation))
            {
                validation.Append(contentValidation);
                return false;
            }
            CompiledOutgameContentCatalog outgame;
            if (!OutgameContentCompiler.TryCompile(outgameCatalog, levels,
                    out outgame, out contentValidation))
            {
                validation.Append(contentValidation);
                return false;
            }
            bundle = new CompiledGameContentBundle(manifest, battle, outgame);
            return true;
        }

        private static bool TryLoadBattle(GameContentManifestDto manifest,
            ContentValidationResult validation,
            out BattleContentCatalogDto catalog)
        {
            catalog = null;
            var text = Resources.Load<TextAsset>(manifest.battleCatalogResourcePath);
            if (text == null)
            {
                validation.Add("battle-content.resource.missing", "manifest",
                    manifest.manifestId, "battleCatalogResourcePath",
                    "Referenced bundled battle catalog is missing.");
                return false;
            }
            try
            {
                catalog = BattleContentJson.Deserialize(text.text);
                return true;
            }
            catch (Exception exception)
            {
                validation.Add("battle-content.deserialize.failed", "manifest",
                    manifest.manifestId, "battleCatalogResourcePath", exception.Message);
                return false;
            }
        }

        private static bool TryLoadOutgame(GameContentManifestDto manifest,
            ContentValidationResult validation,
            out OutgameContentCatalogDto catalog)
        {
            catalog = null;
            var text = Resources.Load<TextAsset>(manifest.outgameCatalogResourcePath);
            if (text == null)
            {
                validation.Add("outgame-content.resource.missing", "manifest",
                    manifest.manifestId, "outgameCatalogResourcePath",
                    "Referenced bundled outgame catalog is missing.");
                return false;
            }
            try
            {
                catalog = OutgameContentJson.Deserialize(text.text);
                return true;
            }
            catch (Exception exception)
            {
                validation.Add("outgame-content.deserialize.failed", "manifest",
                    manifest.manifestId, "outgameCatalogResourcePath", exception.Message);
                return false;
            }
        }
    }
}
