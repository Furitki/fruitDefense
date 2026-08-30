using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FruitDefense.Presentation;
using FruitDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FruitDefense.Editor
{
    public static partial class RuntimeUiVisualSystemValidator
    {
        private static void ValidateRegistryIdentity(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet)
        {
            var setPath = AssetDatabase.GetAssetPath(artSet);
            if (!Directory.Exists(ToAbsolute(RuntimeUiArtSetRegistry.RuntimeDirectory(artSet))))
            {
                report.Error("art-set.runtime-directory", setPath,
                    "No matching Runtime/<setId> directory exists.",
                    "Export runtime PNGs to " + RuntimeUiArtSetRegistry.RuntimeDirectory(artSet) + ".");
            }
            if (!Directory.Exists(ToAbsolute(RuntimeUiArtSetRegistry.SourceDirectory(artSet))))
            {
                report.Error("art-set.source-directory", setPath,
                    "No matching Sources/<setId> directory exists.",
                    "Store source masters and manifest under " + RuntimeUiArtSetRegistry.SourceDirectory(artSet) + ".");
            }
        }

        private static void ValidateRegistryDuplicates(RuntimeUiVisualValidationReport report,
            IReadOnlyList<RuntimeUiArtSet> sets)
        {
            foreach (var group in sets.GroupBy(set => set.SetId + "\n" + set.Revision,
                         StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                report.Error("registry.identity.duplicate", RuntimeUiArtSetRegistry.ArtSetRoot,
                    "More than one production art set has identity/revision '"
                    + group.First().SetId + "@" + group.First().Revision + "'.",
                    "Keep exactly one production asset for each stable identity/revision.");
            }
        }

        private static void ValidateManifestAndBindings(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet)
        {
            var manifestPath = RuntimeUiArtSetRegistry.ManifestPath(artSet);
            var absoluteManifest = ToAbsolute(manifestPath);
            if (!File.Exists(absoluteManifest))
            {
                report.Error("manifest.missing", manifestPath,
                    "The art set has no source/runtime ownership manifest.",
                    "Generate art_manifest.json in the matching source directory.");
                return;
            }

            ArtManifest manifest;
            try
            {
                manifest = JsonUtility.FromJson<ArtManifest>(File.ReadAllText(absoluteManifest));
            }
            catch (Exception exception)
            {
                report.Error("manifest.parse", manifestPath, exception.Message,
                    "Repair the JSON manifest.");
                return;
            }
            if (manifest == null || manifest.bindings == null)
            {
                report.Error("manifest.contract", manifestPath,
                    "The manifest or bindings array is null.", "Restore the v1 manifest contract.");
                return;
            }

            if (manifest.importContract == null)
            {
                report.Error("manifest.import-contract", manifestPath,
                    "The manifest importContract is missing.",
                    "Restore the v1 standalone Sprite Single import contract.");
                return;
            }

            if (manifest.schema != ManifestSchema || manifest.setId != artSet.SetId
                || manifest.revision != artSet.Revision
                || manifest.slotCount != RuntimeUiArtSlots.RequiredCount
                || manifest.bindings.Length != RuntimeUiArtSlots.RequiredCount
                || !Nearly(manifest.sourceScale,
                    RuntimeUiQualityProfile.ProductionPixelsPerLogicalUnit)
                || manifest.importContract.pixelsPerUnit
                    != RuntimeUiQualityProfile.ProductionImporterPixelsPerUnit)
            {
                report.Error("manifest.identity", manifestPath,
                    "Schema, set identity/revision, or finite "
                    + RuntimeUiArtSlots.RequiredCount
                    + "-slot count does not match the asset.",
                    "Regenerate the manifest from this exact art-set revision.");
            }

            var manifestBySlot = new Dictionary<int, ArtManifestBinding>();
            foreach (var row in manifest.bindings)
            {
                if (row == null || manifestBySlot.ContainsKey(row.slot))
                {
                    report.Error("manifest.slot.duplicate", manifestPath,
                        "The manifest contains a null or duplicate slot row.",
                        "Emit exactly one row for each slot 0-"
                        + (RuntimeUiArtSlots.RequiredCount - 1) + ".");
                    continue;
                }
                manifestBySlot.Add(row.slot, row);
            }

            foreach (var group in manifest.bindings.Where(row => row != null
                         && string.Equals(row.authoring_contract,
                             "imagegen-direct-master", StringComparison.Ordinal)
                         && !string.IsNullOrWhiteSpace(row.generated_asset))
                         .GroupBy(row => row.generated_asset,
                             StringComparer.OrdinalIgnoreCase))
            {
                var edgeContracts = group.Select(row =>
                        (row.material_anatomy ?? string.Empty) + "\n"
                        + (row.render_contract ?? string.Empty) + "\n"
                        + (row.deterministic_transform ?? string.Empty))
                    .Distinct(StringComparer.Ordinal).ToArray();
                if (edgeContracts.Length <= 1) continue;
                report.Error("material.imagegen.master-contract-sharing", manifestPath,
                    "Direct ImageGen master '" + group.Key
                    + "' is shared by incompatible edge/anatomy contracts: "
                    + string.Join(", ", group.Select(row => row.semantic_id)),
                    "Give each incompatible semantic contract its own reviewed master; reuse is permitted only when edge, outline, shadow, alpha, anatomy, and transform are identical.");
            }

            var ownedRuntimePaths = new HashSet<string>(StringComparer.Ordinal);
            var ownedSourcePaths = new HashSet<string>(StringComparer.Ordinal);
            var referencePpu = -1f;
            foreach (var binding in artSet.Bindings)
            {
                if (binding == null) continue;
                if (!manifestBySlot.TryGetValue((int)binding.Slot, out var row))
                {
                    report.Error("manifest.slot.missing", manifestPath,
                        "No manifest row owns " + RuntimeUiArtSlots.SemanticId(binding.Slot) + ".",
                        "Add the exact binding row.");
                    continue;
                }

                ValidateManifestRow(report, artSet, binding, row, manifest, manifestPath,
                    ownedRuntimePaths, ownedSourcePaths);
                ValidateImportedBinding(report, artSet, binding, row, manifest);
                if (RuntimeUiArtSlots.IsMicroIcon(binding.Slot))
                {
                    if (!Nearly(binding.PixelsPerLogicalUnit, 1f))
                    {
                        report.Error("art-set.ppu.micro", AssetDatabase.GetAssetPath(artSet),
                            RuntimeUiArtSlots.SemanticId(binding.Slot)
                            + " must use one source pixel per logical point.",
                            "Re-export the micro tier at its final 18px canvas.");
                    }
                }
                else if (referencePpu < 0f) referencePpu = binding.PixelsPerLogicalUnit;
                else if (!Nearly(binding.PixelsPerLogicalUnit, referencePpu))
                {
                    report.Error("art-set.ppu.inconsistent", AssetDatabase.GetAssetPath(artSet),
                        "Bindings use inconsistent logical source scale.",
                        "Use one pixelsPerLogicalUnit value for the full set.");
                }
            }

            ValidatePanelFamilyMetadata(report, artSet);

            if (ownedRuntimePaths.Count != manifest.uniqueExportCount)
            {
                report.Error("manifest.export-count", manifestPath,
                    "uniqueExportCount does not match referenced runtime exports.",
                    "Regenerate the manifest ownership summary.");
            }
            if (string.Equals(artSet.SetId, PaintedSetId, StringComparison.Ordinal)
                && manifest.uniqueExportCount
                    != RuntimeUiQualityProfile.PaintedUniqueExportCount)
            {
                report.Error("manifest.export-count.painted", manifestPath,
                    "The painted production set must own exactly "
                    + RuntimeUiQualityProfile.PaintedUniqueExportCount
                    + " unique runtime exports for " + RuntimeUiArtSlots.RequiredCount
                    + " semantic bindings.",
                    "Restore the reviewed continue/start/start-wave sharing contract.");
            }
            ValidateNoUnownedFiles(report, RuntimeUiArtSetRegistry.RuntimeDirectory(artSet), "*.png",
                ownedRuntimePaths, "runtime");
            ValidateNoUnownedFiles(report, RuntimeUiArtSetRegistry.SourceDirectory(artSet), "*.svg",
                ownedSourcePaths, "source");
            ValidateNoUnownedFiles(report, RuntimeUiArtSetRegistry.SourceDirectory(artSet), "*.png",
                ownedSourcePaths, "source");
            ValidateProductionAncillaryFiles(report, artSet);
            ValidateMicroSilhouetteFamily(report, artSet);
        }

        internal static RuntimeUiVisualValidationReport ValidatePanelFamilyMetadata(
            RuntimeUiArtSet artSet)
        {
            var report = new RuntimeUiVisualValidationReport();
            ValidatePanelFamilyMetadata(report, artSet);
            return report;
        }

        private static void ValidatePanelFamilyMetadata(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet)
        {
            var artSetPath = artSet == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(artSet);
            if (artSet == null)
            {
                report.Error("panel-family.art-set-missing", artSetPath,
                    "Panel-family metadata cannot be validated without an ArtSet.",
                    "Provide one complete production ArtSet.");
                return;
            }

            RuntimeUiArtBinding reference = null;
            foreach (var slot in PanelFamilySlots)
            {
                if (!artSet.TryGetBinding(slot, out var binding) || binding == null)
                {
                    report.Error("panel-family.binding-missing", artSetPath,
                        RuntimeUiArtSlots.SemanticId(slot)
                        + " is missing from the structural panel family.",
                        "Restore the complete finite panel family.");
                    continue;
                }

                if (binding.Geometry != RuntimeUiArtGeometry.NineSlice)
                {
                    report.Error("panel-family.geometry", artSetPath,
                        RuntimeUiArtSlots.SemanticId(slot)
                        + " is not a protected nine-slice surface.",
                        "Bind the slot to the declared nine-slice panel geometry.");
                }

                if (binding.Texture == null
                    || binding.OpticalInset.Horizontal >= binding.Texture.width
                    || binding.OpticalInset.Vertical >= binding.Texture.height)
                {
                    report.Error("panel-family.optical-envelope", artSetPath,
                        RuntimeUiArtSlots.SemanticId(slot)
                        + " has no positive significant-alpha optical envelope.",
                        "Restore the runtime PNG and its extracted optical inset metadata.");
                }

                if (reference == null)
                {
                    reference = binding;
                    continue;
                }

                if (!SameInsets(reference.SliceBorder, binding.SliceBorder)
                    || !SameInsets(reference.SafeInset, binding.SafeInset))
                {
                    report.Error("panel-family.protected-border", artSetPath,
                        RuntimeUiArtSlots.SemanticId(slot)
                        + " uses protected border/safe-inset metadata that differs from "
                        + RuntimeUiArtSlots.SemanticId(reference.Slot) + ".",
                        "Keep panel-family slicing and protected content rails identical.");
                }
                if (!Nearly(reference.PixelsPerLogicalUnit,
                        binding.PixelsPerLogicalUnit))
                {
                    report.Error("panel-family.logical-scale", artSetPath,
                        RuntimeUiArtSlots.SemanticId(slot)
                        + " uses a different pixels-per-logical-unit scale from "
                        + RuntimeUiArtSlots.SemanticId(reference.Slot) + ".",
                        "Export the full panel family at one declared logical scale.");
                }
            }
        }

        internal static RuntimeUiVisualValidationReport ValidateBattleStructuralHierarchy(
            RuntimeUiArtSet artSet)
        {
            var report = new RuntimeUiVisualValidationReport();
            ValidateBattleStructuralHierarchy(report, artSet);
            return report;
        }

        private static void ValidateBattleStructuralHierarchy(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet)
        {
            var artSetPath = artSet == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(artSet);
            if (artSet == null)
            {
                report.Error("battle-stage.art-set-missing", artSetPath,
                    "Battle structural hierarchy requires one complete ArtSet.",
                    "Provide one complete production ArtSet.");
                return;
            }

            if (!artSet.TryGetBinding(RuntimeUiArtSlot.SurfacePanelStandard,
                    out var standard) || standard == null
                || !artSet.TryGetBinding(RuntimeUiArtSlot.SurfaceGameplayStage,
                    out var stage) || stage == null)
            {
                report.Error("battle-stage.binding-missing", artSetPath,
                    "Battle requires light standard and gameplay-stage bindings.",
                    "Restore both semantic nine-slice bindings.");
                return;
            }

            if (standard.Geometry != RuntimeUiArtGeometry.NineSlice
                || stage.Geometry != RuntimeUiArtGeometry.NineSlice)
            {
                report.Error("battle-stage.geometry", artSetPath,
                    "Battle standard and gameplay-stage surfaces must be nine-slice.",
                    "Restore the declared protected geometry.");
            }
            if (stage.Slot != RuntimeUiArtSlot.SurfaceGameplayStage
                || stage.Texture == null || stage.Sprite == null)
            {
                report.Error("battle-stage.semantic-binding", artSetPath,
                    "The heavy Battle frame is not bound to surface.gameplay-stage.",
                    "Bind the standalone gameplay-stage asset without a fallback.");
            }
            if (stage.SliceBorder.Left != 20 || stage.SliceBorder.Top != 20
                || stage.SliceBorder.Right != 20 || stage.SliceBorder.Bottom != 20
                || stage.SafeInset.Left != 20 || stage.SafeInset.Top != 20
                || stage.SafeInset.Right != 20 || stage.SafeInset.Bottom != 20)
            {
                report.Error("battle-stage.protected-border", artSetPath,
                    "surface.gameplay-stage must keep the approved 20px slice and safe inset.",
                    "Re-export the standalone stage master with its protected corners.");
            }
            if (!Nearly(standard.PixelsPerLogicalUnit, stage.PixelsPerLogicalUnit))
            {
                report.Error("battle-stage.logical-scale", artSetPath,
                    "Battle standard and gameplay-stage surfaces use different logical scales.",
                    "Export both surfaces at the production set's declared scale.");
            }
        }

        internal static RuntimeUiVisualValidationReport ValidateStructuralPeerOutlineRole(
            RuntimeUiArtSet artSet, string route, IReadOnlyList<string> regionNames,
            IReadOnlyList<RuntimeUiArtSlot> slots)
        {
            var report = new RuntimeUiVisualValidationReport();
            ValidateStructuralPeerOutlineRole(report, artSet, route, regionNames, slots);
            return report;
        }

        private static void ValidateStructuralPeerOutlineRole(
            RuntimeUiVisualValidationReport report, RuntimeUiArtSet artSet,
            string route, IReadOnlyList<string> regionNames,
            IReadOnlyList<RuntimeUiArtSlot> slots)
        {
            var artSetPath = artSet == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(artSet);
            if (regionNames == null || slots == null || regionNames.Count < 2
                || regionNames.Count != slots.Count)
            {
                report.Error("panel-peer.contract", artSetPath,
                    (route ?? "Runtime route")
                    + " must declare two or more named structural peers.",
                    "Register every peer region with its actual semantic panel slot.");
                return;
            }

            var expected = slots[0];
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                if (!PanelFamilySlots.Contains(slot))
                {
                    report.Error("panel-peer.non-panel-slot", artSetPath,
                        (route ?? "Runtime route") + "/" + regionNames[index]
                        + " uses non-panel slot " + slot + ".",
                        "Bind structural peers to a declared panel-family slot.");
                    continue;
                }
                if (slot == expected) continue;
                report.Error("panel-peer.outline-role", artSetPath,
                    (route ?? "Runtime route") + " structural peers "
                    + regionNames[0] + " and " + regionNames[index]
                    + " use different final-pixel outline roles: "
                    + RuntimeUiArtSlots.SemanticId(expected) + " vs "
                    + RuntimeUiArtSlots.SemanticId(slot) + ".",
                    "Bind equivalent structural peers to the same semantic panel slot.");
            }
        }

        private static void ValidateManifestRow(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet, RuntimeUiArtBinding binding, ArtManifestBinding row,
            ArtManifest manifest, string manifestPath, HashSet<string> runtimePaths,
            HashSet<string> sourcePaths)
        {
            var semantic = RuntimeUiArtSlots.SemanticId(binding.Slot);
            var runtime = RuntimeUiArtSetRegistry.Normalize(row.runtime);
            var source = RuntimeUiArtSetRegistry.Normalize(row.source);
            var sharedOwner = string.IsNullOrWhiteSpace(row.shared_from_set)
                ? string.Empty
                : row.shared_from_set.Trim();
            var expectedRuntimeDirectory = RuntimeUiArtSetRegistry.RuntimeDirectory(artSet);
            var expectedSourceDirectory = RuntimeUiArtSetRegistry.SourceDirectory(artSet);
            if (!string.IsNullOrEmpty(sharedOwner))
            {
                report.Error("manifest.shared-owner.unapproved", manifestPath,
                    "Production ArtSets may not inherit or share bindings from another set.",
                    "Remove shared_from_set and own the binding locally.");
            }
            runtimePaths.Add(runtime);
            sourcePaths.Add(source);
            ValidateReferenceMaterialManifest(report, binding, row, manifestPath);
            ValidateSemanticActionContainerManifest(report, binding.Slot, row,
                manifestPath);
            if (row.semantic_id != semantic || row.geometry != GeometryName(binding.Geometry)
                || !runtime.StartsWith(expectedRuntimeDirectory + "/", StringComparison.Ordinal)
                || !source.StartsWith(expectedSourceDirectory + "/", StringComparison.Ordinal)
                || !Nearly(row.pixels_per_logical_unit, binding.PixelsPerLogicalUnit))
            {
                report.Error("manifest.binding.contract", manifestPath,
                    "Manifest row " + row.slot + " does not match " + semantic
                    + " identity, geometry, directory, or logical scale.",
                    "Regenerate this binding row from the production asset.");
            }

            var uniformSlice = UniformInset(binding.SliceBorder);
            var uniformSafe = UniformInset(binding.SafeInset);
            if (uniformSlice != row.slice_border || uniformSafe != row.safe_inset
                || !MatchesOpticalInset(binding.OpticalInset, row.optical_inset))
            {
                report.Error("manifest.binding.insets", manifestPath,
                    semantic + " manifest insets differ from the serialized binding.",
                    "Keep manifest and binding slice/safe/optical inset metadata identical.");
            }

            ValidateOwnedFile(report, source, row.sourceSha256, string.Empty, "source");
            ValidateOwnedFile(report, runtime, row.runtimeSha256, row.guid, "runtime");
            ValidateTintableActionGlyph(report, binding.Slot, row, source, runtime);
            var texturePath = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(binding.Texture));
            var spritePath = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(binding.Sprite));
            if (texturePath != runtime || spritePath != runtime)
            {
                report.Error("binding.asset-path", AssetDatabase.GetAssetPath(artSet),
                    semantic + " Texture/Sprite do not both point to the manifest runtime PNG.",
                    "Bind both references to the standalone Sprite Single asset at " + runtime + ".");
            }
        }

        private static void ValidateOwnedFile(RuntimeUiVisualValidationReport report, string path,
            string expectedHash, string expectedGuid, string kind)
        {
            var absolute = ToAbsolute(path);
            if (!File.Exists(absolute))
            {
                report.Error("manifest." + kind + ".missing", path,
                    "Owned " + kind + " file is missing.", "Restore the manifest-owned file.");
                return;
            }
            if (!string.Equals(Sha256(absolute), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                report.Error("manifest." + kind + ".hash", path,
                    "The file hash differs from the manifest.",
                    "Re-export intentionally and update the manifest in one change.");
            }
            if (!string.IsNullOrEmpty(expectedGuid)
                && !string.Equals(AssetDatabase.AssetPathToGUID(path), expectedGuid,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.Error("manifest." + kind + ".guid", path,
                    "The imported GUID differs from the manifest.",
                    "Preserve the production .meta GUID or regenerate the manifest intentionally.");
            }
        }

        private static void ValidateImportedBinding(RuntimeUiVisualValidationReport report,
            RuntimeUiArtSet artSet, RuntimeUiArtBinding binding, ArtManifestBinding row,
            ArtManifest manifest)
        {
            var path = RuntimeUiArtSetRegistry.Normalize(AssetDatabase.GetAssetPath(binding.Texture));
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                report.Error("importer.missing", path, "Runtime PNG has no TextureImporter.",
                    "Import it as a standalone Sprite (2D and UI).");
                return;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var wrong = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || settings.spriteMeshType != SpriteMeshType.FullRect
                || !importer.sRGBTexture || !importer.alphaIsTransparency
                || importer.wrapModeU != TextureWrapMode.Clamp
                || importer.wrapModeV != TextureWrapMode.Clamp
                || importer.wrapModeW != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear
                || importer.mipmapEnabled || importer.isReadable
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.npotScale != TextureImporterNPOTScale.None
                || !Nearly(importer.spritePixelsPerUnit,
                    RuntimeUiQualityProfile.ProductionImporterPixelsPerUnit)
                || manifest.importContract.pixelsPerUnit
                    != RuntimeUiQualityProfile.ProductionImporterPixelsPerUnit
                || !Nearly(binding.PixelsPerLogicalUnit,
                    RuntimeUiArtSlots.IsMicroIcon(binding.Slot)
                        ? 1f : RuntimeUiQualityProfile.ProductionPixelsPerLogicalUnit);
            var standaloneOverride = importer.GetPlatformTextureSettings("Standalone");
            var webGlOverride = importer.GetPlatformTextureSettings("WebGL");
            wrong |= standaloneOverride.overridden || webGlOverride.overridden;
            if (wrong)
            {
                report.Error("importer.contract", path,
                    "Importer must be Sprite Single/FullRect, sRGB+alpha, Clamp/Bilinear, no mip/read-write/compression, and unified PPU.",
                    "Apply the manifest import contract without platform overrides.");
            }

            var border = importer.spriteBorder;
            var expected = binding.SliceBorder;
            if (!Nearly(border.x, expected.Left) || !Nearly(border.y, expected.Bottom)
                || !Nearly(border.z, expected.Right) || !Nearly(border.w, expected.Top))
            {
                report.Error("importer.border", path,
                    "Sprite border differs from the binding slice metadata.",
                    "Set importer border left/bottom/right/top to the serialized slice border.");
            }
            if (binding.Geometry != RuntimeUiArtGeometry.NineSlice
                && (expected.Left != 0 || expected.Right != 0
                    || expected.Top != 0 || expected.Bottom != 0))
            {
                report.Error("binding.border.non-nine-slice", path,
                    "Stretch/Icon bindings must not serialize a slice border.",
                    "Set the binding and importer border to zero.");
            }

            if (binding.Sprite != null)
            {
                var rect = binding.Sprite.rect;
                if (binding.Sprite.packed || !Nearly(rect.x, 0f) || !Nearly(rect.y, 0f)
                    || !Nearly(rect.width, binding.Texture.width)
                    || !Nearly(rect.height, binding.Texture.height))
                {
                    report.Error("sprite.standalone-fullrect", path,
                        "Every binding must be an unpacked full-texture standalone sprite.",
                        "Disable atlas packing and restore Sprite Single / Full Rect.");
                }
                var expectedWidth = row.width > 0 ? row.width : row.size;
                var expectedHeight = row.height > 0 ? row.height : row.size;
                if (expectedWidth <= 0 || expectedHeight <= 0
                    || expectedWidth != binding.Texture.width
                    || expectedHeight != binding.Texture.height)
                {
                    report.Error("manifest.canvas-size", path,
                        "Manifest canvas dimensions differ from the imported texture.",
                        "Record width/height for fixed-aspect art or size for square art.");
                }
            }

            if (binding.Geometry == RuntimeUiArtGeometry.NineSlice
                && (binding.Texture.width - binding.SliceBorder.Horizontal <= 0
                    || binding.Texture.height - binding.SliceBorder.Vertical <= 0))
            {
                report.Error("nine-slice.center", path,
                    "Nine-slice borders leave no positive center region.",
                    "Reduce the slice border or increase the source canvas.");
            }
            ValidatePixelQuality(report, path, binding, row);
            ValidateIllustrationSourceAspect(report, binding, row);
        }

        private static int UniformInset(RuntimeUiPixelInsets inset)
        {
            return inset.Left == inset.Top && inset.Left == inset.Right && inset.Left == inset.Bottom
                ? inset.Left : int.MinValue;
        }

        private static bool MatchesOpticalInset(RuntimeUiPixelInsets inset,
            ArtManifestInsets manifest)
        {
            return manifest != null && inset.Left == manifest.left
                && inset.Top == manifest.top && inset.Right == manifest.right
                && inset.Bottom == manifest.bottom;
        }

        private static bool SameInsets(RuntimeUiPixelInsets left,
            RuntimeUiPixelInsets right)
        {
            return left.Left == right.Left && left.Top == right.Top
                && left.Right == right.Right && left.Bottom == right.Bottom;
        }

        private static string GeometryName(RuntimeUiArtGeometry geometry)
        {
            switch (geometry)
            {
                case RuntimeUiArtGeometry.Stretch: return "stretch";
                case RuntimeUiArtGeometry.NineSlice: return "nine-slice";
                case RuntimeUiArtGeometry.Icon: return "icon";
                default: return string.Empty;
            }
        }

        private static bool Nearly(float left, float right)
        {
            return Mathf.Abs(left - right) <= .001f;
        }

        [Serializable]
        private sealed class ArtManifest
        {
            public string schema;
            public string setId;
            public string revision;
            public float sourceScale;
            public int slotCount;
            public int uniqueExportCount;
            public ArtManifestBinding[] bindings;
            public ImportContract importContract;
        }

        [Serializable]
        private sealed class ArtManifestBinding
        {
            public string stem;
            public string semantic_id;
            public string geometry;
            public int size;
            public int width;
            public int height;
            public string source;
            public string runtime;
            public string sourceSha256;
            public string runtimeSha256;
            public string guid;
            public int slice_border;
            public int safe_inset;
            public ArtManifestInsets optical_inset;
            public float pixels_per_logical_unit;
            public int slot;
            public string shared_from_set;
            public string imagegen_provider;
            public string imagegen_output;
            public string prompt_record;
            public string render_contract;
            public string neutral_rgb;
            public string container_contract;
            public string target_rgb;
            public string content_reference_rgb;
            public float content_region_min_contrast;
            public string authoring_contract;
            public string material_recipe;
            public string material_anatomy;
            public string outer_cream_rgb;
            public string face_rgb;
            public string soil_outline_rgb;
            public string upper_highlight_rgb;
            public string bottom_shadow_rgb;
            public string content_layout_contract;
            public string content_tone;
            public string generated_asset;
            public string generated_asset_sha256;
            public string generated_sheet;
            public string generated_sheet_sha256;
            public int[] generated_crop;
            public string deterministic_transform;
        }

        [Serializable]
        private sealed class ArtManifestInsets
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [Serializable]
        private sealed class ImportContract
        {
            public int pixelsPerUnit;
        }
    }
}
