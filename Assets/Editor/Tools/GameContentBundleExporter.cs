using System;
using System.IO;
using FruitDefense.Content;

namespace FruitDefense.Editor
{
    internal sealed class GameContentBundleBytes
    {
        public byte[] Battle { get; private set; }
        public byte[] Outgame { get; private set; }
        public byte[] Manifest { get; private set; }

        internal GameContentBundleBytes(byte[] battle, byte[] outgame,
            byte[] manifest)
        {
            Battle = battle;
            Outgame = outgame;
            Manifest = manifest;
        }
    }

    internal static class GameContentBundleExporter
    {
        public static bool TryBuild(BattleContentCatalogDto battle,
            OutgameContentCatalogDto outgame, GameContentManifestDto manifest,
            out GameContentBundleBytes bytes,
            out ContentValidationResult validation)
        {
            bytes = null;
            validation = new ContentValidationResult();
            validation.Append(BattleContentValidator.ValidateBundledBaseline(battle));
            var levels = BundledLevelCatalogFactory.CreateSource();
            validation.Append(OutgameContentValidator.ValidateBundledBaseline(
                outgame, levels));
            validation.Append(GameContentManifestValidator.Validate(manifest,
                battle, outgame, BundledLevelCatalogIds.Catalog));
            if (!validation.IsValid) return false;

            CompiledBattleContentCatalog compiledBattle;
            ContentValidationResult compileValidation;
            if (!BattleContentCompiler.TryCompile(battle, out compiledBattle,
                    out compileValidation))
                validation.Append(compileValidation);
            CompiledOutgameContentCatalog compiledOutgame;
            if (!OutgameContentCompiler.TryCompile(outgame, levels,
                    out compiledOutgame, out compileValidation))
                validation.Append(compileValidation);
            if (!validation.IsValid) return false;

            bytes = new GameContentBundleBytes(
                BattleContentJson.SerializeCanonicalUtf8(battle),
                OutgameContentJson.SerializeCanonicalUtf8(outgame),
                GameContentManifestJson.SerializeCanonicalUtf8(manifest));
            return true;
        }

        public static bool TryWrite(BattleContentCatalogDto battle,
            OutgameContentCatalogDto outgame, GameContentManifestDto manifest,
            string battlePath, string outgamePath, string manifestPath,
            out ContentValidationResult validation)
        {
            GameContentBundleBytes bytes;
            if (!TryBuild(battle, outgame, manifest, out bytes, out validation))
                return false;
            RequirePath(battlePath, nameof(battlePath));
            RequirePath(outgamePath, nameof(outgamePath));
            RequirePath(manifestPath, nameof(manifestPath));
            Directory.CreateDirectory(Path.GetDirectoryName(battlePath));
            Directory.CreateDirectory(Path.GetDirectoryName(outgamePath));
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            File.WriteAllBytes(battlePath, bytes.Battle);
            File.WriteAllBytes(outgamePath, bytes.Outgame);
            File.WriteAllBytes(manifestPath, bytes.Manifest);
            return true;
        }

        private static void RequirePath(string value, string parameter)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && !string.IsNullOrEmpty(Path.GetDirectoryName(value))) return;
            throw new ArgumentException("Export path must include a directory.",
                parameter);
        }
    }
}
