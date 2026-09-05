using System;
using FruitDefense.UI;

namespace FruitDefense.Editor
{
    public static partial class RuntimeUiVisualSystemValidator
    {
        private static bool ValidateFixedPrimaryActionManifest(
            RuntimeUiVisualValidationReport report, RuntimeUiArtBinding binding,
            ArtManifestBinding row, string manifestPath)
        {
            if (binding.Slot != RuntimeUiArtSlot.ActionPrimary) return false;

            const string fixedMaster =
                "openspec/changes/restore-reference-home-activity-pages/"
                + "evidence/fixed-master/action-primary-original-square.png";
            const string fixedHash =
                "967A2AE0DFECC4196D99CF4DA236774B11C1D716EF6AB48EC18C2E665D6C7801";
            const string historicalCommit =
                "d423af201917d6a66a1328f55533d2119203db28";
            const string historicalPath =
                "Assets/UI/Art/Sources/sunny-orchard-painted/surfaces/"
                + "action-primary.png";
            const string transform =
                "byte-for-byte-master-copy|alpha-safe-resize|low-alpha-cleanup";
            const string anatomy =
                "outer-cream-rim|rounded-square-lime-face|soil-outline|"
                + "upper-highlight|short-bottom-shadow";
            if (row.authoring_contract != "user-approved-fixed-raster-master"
                || row.material_anatomy != anatomy
                || row.fixed_master != fixedMaster
                || row.fixed_master_sha256 != fixedHash
                || row.historical_commit != historicalCommit
                || row.historical_path != historicalPath
                || row.deterministic_transform != transform
                || row.content_tone != "primary"
                || !string.IsNullOrWhiteSpace(row.imagegen_provider)
                || !string.IsNullOrWhiteSpace(row.imagegen_output)
                || !string.IsNullOrWhiteSpace(row.generated_asset)
                || !string.IsNullOrWhiteSpace(row.generated_asset_sha256))
            {
                report.Error("material.fixed-primary.manifest", manifestPath,
                    "action.primary does not retain the user-selected original "
                    + "square raster master and fixed provenance.",
                    "Restore the hash-locked historical PNG without ImageGen, "
                    + "procedural drawing, recoloring, or capsule composition.");
            }
            else
                ValidateOwnedFile(report, row.fixed_master,
                    row.fixed_master_sha256, string.Empty, "fixed-master");
            return true;
        }
    }
}
