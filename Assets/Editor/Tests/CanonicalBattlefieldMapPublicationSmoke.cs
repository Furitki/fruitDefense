using System;
using System.Collections.Generic;
using System.Linq;
using FruitDefense.Content;
using FruitDefense.Core;
using FruitDefense.Tilemaps;
using UnityEditor;
using UnityEngine;

namespace FruitDefense.Editor
{
    public static class CanonicalBattlefieldMapPublicationSmoke
    {
        internal const string FixtureOutputPath =
            "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap/GeneratedPublicationSmoke.asset";
        internal const string DeletedRecoveryOutputPath =
            "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap/DeletedRecoverySmoke.asset";

        public static void Validate()
        {
            DeleteFixtureOutput(FixtureOutputPath);
            try
            {
                var palettes = BattlefieldMapPublicationExporter.LoadReleaseRegisteredPalettes();
                Assert(palettes.Count > 0, "release Battle scene registers a real terrain palette");
                ValidateSchemaRejection(palettes);
                ValidateNegativePublication(palettes);
                ValidateRealPaletteFailures(palettes);
                ValidateManifestRebuildAndAtomicity(palettes);
                Debug.Log("CANONICAL_BATTLEFIELD_MAP_PUBLICATION_SMOKE_OK");
            }
            finally
            {
                DeleteFixtureOutput(FixtureOutputPath);
            }
        }

        public static void ValidateDeletedOutputRecovery()
        {
            DeleteFixtureOutput(DeletedRecoveryOutputPath);
            Assert(AssetDatabase.LoadMainAssetAtPath(DeletedRecoveryOutputPath) == null,
                "deleted-output recovery starts with no generated asset");
            var map = CreatePublishableMap("map.smoke.deleted-recovery");
            var manifest = Manifest(Entry(0, "level.smoke.deleted-recovery", map));
            try
            {
                var result = BattlefieldMapPublicationExporter.Rebuild(manifest,
                    DeletedRecoveryOutputPath,
                    BattlefieldMapPublicationExporter.LoadReleaseRegisteredPalettes());
                Assert(result.Succeeded && result.GeneratedCatalog != null
                    && result.GeneratedCatalog.Entries.Count == 1,
                    "full rebuild recreates equivalent output after deletion: "
                    + Format(result.Diagnostics));
                Debug.Log("CANONICAL_BATTLEFIELD_MAP_DELETED_OUTPUT_RECOVERY_OK");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(manifest);
                UnityEngine.Object.DestroyImmediate(map);
                DeleteFixtureOutput(DeletedRecoveryOutputPath);
            }
        }

        private static void ValidateSchemaRejection(
            IReadOnlyList<BattlefieldTerrainPalette> palettes)
        {
            var staleMap = CreatePublishableMap("map.smoke.schema-stale");
            var staleCatalog = ScriptableObject.CreateInstance<PublishedBattlefieldMapCatalog>();
            try
            {
                SetIntegerProperty(staleMap, "schemaVersion",
                    BattlefieldLayerIds.SchemaVersion - 1);
                ExpectBuildFailure(Manifest(Entry(0, "level.smoke.schema-stale", staleMap)),
                    palettes, "canonical.map.schema-version");

                var bundled = BundledLevelCatalogFactory.CreateBundledSource();
                staleCatalog.Configure(bundled.CatalogId, bundled.ContentVersion,
                    Array.Empty<PublishedBattlefieldMapEntry>());
                SetIntegerProperty(staleCatalog, "schemaVersion",
                    PublishedBattlefieldMapCatalog.CurrentSchemaVersion - 1);
                var rejected = false;
                try
                {
                    BundledLevelCatalogFactory.ComposePublished(bundled, staleCatalog);
                }
                catch (InvalidOperationException exception)
                {
                    rejected = exception.Message.Contains("rebuild it from current-schema");
                }
                Assert(rejected,
                    "stale published catalog schema is explicitly rejected with rebuild guidance");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(staleCatalog);
                UnityEngine.Object.DestroyImmediate(staleMap);
            }
        }

        private static void ValidateNegativePublication(
            IReadOnlyList<BattlefieldTerrainPalette> palettes)
        {
            var incomplete = CreatePublishableMap("map.smoke.invalid.coverage");
            var serialized = new SerializedObject(incomplete);
            var visualCells = serialized.FindProperty("visualCells");
            visualCells.arraySize--;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ExpectBuildFailure(Manifest(Entry(0, "level.smoke.invalid.coverage", incomplete)),
                palettes, "authoring.visual-coverage");

            var outOfBounds = CreatePublishableMap("map.smoke.invalid.out-of-bounds");
            SetRouteCell(outOfBounds, 1, new Vector2Int(99, 99));
            ExpectBuildFailure(Manifest(Entry(0, "level.smoke.invalid.out-of-bounds", outOfBounds)),
                palettes, "canonical.route.out-of-bounds");

            var disconnected = CreatePublishableMap("map.smoke.invalid.disconnected");
            SetRouteCell(disconnected, 1, new Vector2Int(1, 1));
            ExpectBuildFailure(Manifest(Entry(0, "level.smoke.invalid.disconnected", disconnected)),
                palettes, "canonical.route.disconnected");

            var missingCore = CreatePublishableMap("map.smoke.invalid.missing-core");
            string reason;
            Assert(missingCore.TryRemoveMarker("marker.core.main", out reason), reason);
            ExpectBuildFailure(Manifest(Entry(0, "level.smoke.invalid.missing-core", missingCore)),
                palettes, "canonical.execution.core-count");

            var markerConflict = CreatePublishableMap("map.smoke.invalid.marker-conflict");
            Assert(markerConflict.TrySetGameplay(new Vector2Int(3, 0),
                new[] { BattlefieldLayerIds.Capabilities.Plantable },
                Array.Empty<string>(), out reason), reason);
            string markerId;
            Assert(markerConflict.TryPlaceMarker(BattlefieldMarkerKind.InitialPotCandidate,
                new Vector2Int(3, 0), "pots.smoke", out markerId, out reason), reason);
            ExpectBuildFailure(Manifest(Entry(0, "level.smoke.invalid.marker-conflict",
                markerConflict)), palettes, "canonical.marker.incompatible-at-cell");

            var duplicateA = CreatePublishableMap("map.smoke.duplicate");
            var duplicateB = CreatePublishableMap("map.smoke.duplicate");
            ExpectBuildFailure(Manifest(
                    Entry(0, "level.smoke.duplicate", duplicateA),
                    Entry(1, "level.smoke.duplicate", duplicateB)),
                palettes, "publication.level-id-duplicate", "publication.map-id-duplicate");

            var bundledConflict = CreatePublishableMap(BundledLevelCatalogIds.Maps.Orchard01);
            ExpectBuildFailure(Manifest(Entry(0,
                    BundledLevelCatalogIds.Levels.Orchard01, bundledConflict)),
                palettes, "publication.level-id-duplicate", "publication.map-id-duplicate");

            var invalidTemplate = CreatePublishableMap("map.smoke.invalid.template");
            ExpectBuildFailure(Manifest(new BattlefieldMapPublicationManifestEntry(0,
                    "level.smoke.invalid.template", "level.template.missing", invalidTemplate)),
                palettes, "publication.template-missing");

            var partialEdge = CreatePublishableMap("map.smoke.invalid.partial-edge");
            SetVisualEdgeStyle(partialEdge, 4, BattlefieldLayerIds.EdgeStyles.Refined);
            ExpectBuildFailure(Manifest(Entry(0,
                    "level.smoke.invalid.partial-edge", partialEdge)), palettes,
                "canonical.edge.shared-region-mix");

            foreach (var asset in new[]
            {
                incomplete, outOfBounds, disconnected, missingCore, markerConflict,
                duplicateA, duplicateB, bundledConflict, invalidTemplate, partialEdge,
            })
                UnityEngine.Object.DestroyImmediate(asset);
        }

        private static void ValidateRealPaletteFailures(
            IReadOnlyList<BattlefieldTerrainPalette> palettes)
        {
            var actual = palettes.Single(palette => palette.PaletteId
                == BundledLevelCatalogIds.TerrainPalettes.Orchard01SquareGrid);
            var missingWaterBasePalette = UnityEngine.Object.Instantiate(actual);
            missingWaterBasePalette.ConfigureLayered(actual.PaletteId,
                actual.BaseBindings.Where(binding => binding != null
                    && binding.SurfaceId != BattlefieldLayerIds.Surfaces.Water),
                actual.LandformBindings,
                actual.EdgeBindings.Where(binding => binding != null
                    && binding.LandformSurfaceId != BattlefieldLayerIds.Surfaces.Water
                    && binding.BaseSurfaceId != BattlefieldLayerIds.Surfaces.Water));
            var palettesWithoutWaterBase = palettes.Select(palette => palette.PaletteId
                    == actual.PaletteId ? missingWaterBasePalette : palette)
                .ToArray();
            var missingBase = CreatePublishableMap("map.smoke.palette.base");
            string reason;
            Assert(missingBase.TrySetVisual(new Vector2Int(3, 2),
                BattlefieldLayerIds.Surfaces.Water, string.Empty, string.Empty,
                out reason), reason);
            var baseFailure = ExpectBuildFailure(Manifest(Entry(0,
                "level.smoke.palette.base", missingBase)), palettesWithoutWaterBase,
                "publication.palette-base-missing");
            Assert(baseFailure.Diagnostics.Any(issue =>
                    issue.Code == "publication.palette-base-missing"
                    && issue.HasCell && issue.Cell == new Vector2Int(3, 2)
                    && issue.MapId == missingBase.MapId
                    && issue.BaseSurfaceId == BattlefieldLayerIds.Surfaces.Water),
                "missing base diagnostic carries map, coordinate, and surface");

            var missingLandform = CreatePublishableMap("map.smoke.palette.landform");
            Assert(missingLandform.TrySetVisual(new Vector2Int(3, 2),
                BattlefieldLayerIds.Surfaces.Soil, BattlefieldLayerIds.Surfaces.Water,
                string.Empty, out reason), reason);
            var landformFailure = ExpectBuildFailure(Manifest(Entry(0,
                "level.smoke.palette.landform", missingLandform)), palettes,
                "publication.palette-landform-missing");
            Assert(landformFailure.Diagnostics.Any(issue =>
                    issue.Code == "publication.palette-landform-missing"
                    && issue.HasCell && issue.Cell == new Vector2Int(3, 2)
                    && issue.LandformSurfaceId == BattlefieldLayerIds.Surfaces.Water),
                "missing landform diagnostic carries coordinate and material");

            Assert(actual.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined, out var forwardEdge),
                "real palette supplies the forward refined edge used by the reverse-only fixture");
            Assert(actual.TryGetLandformTileSet(BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.ContourStyles.Square, out var squareGrass),
                "real palette supplies the square landform used by the reverse-only fixture");
            var reverseLandforms = actual.LandformBindings.ToList();
            if (!actual.TryGetLandformTileSet(BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square, out _))
                reverseLandforms.Add(new BattlefieldTerrainLandformBinding(
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square, squareGrass));
            var reverseOnly = ScriptableObject.CreateInstance<BattlefieldTerrainPalette>();
            reverseOnly.ConfigureLayered(actual.PaletteId, actual.BaseBindings,
                reverseLandforms,
                new[]
                {
                    new BattlefieldTerrainEdgeBinding(BattlefieldLayerIds.Surfaces.Soil,
                        BattlefieldLayerIds.Surfaces.Grass,
                        BattlefieldLayerIds.ContourStyles.Square,
                        BattlefieldLayerIds.EdgeStyles.Refined, forwardEdge),
                });
            var directed = CreatePublishableMap("map.smoke.palette.directed-edge");
            Assert(directed.TrySetVisual(new Vector2Int(3, 2),
                BattlefieldLayerIds.Surfaces.Soil, BattlefieldLayerIds.Surfaces.Grass,
                BattlefieldLayerIds.EdgeStyles.Refined, out reason), reason);
            Assert(reverseOnly.TryGetEdgeTileSet(BattlefieldLayerIds.Surfaces.Grass,
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.ContourStyles.Square,
                    BattlefieldLayerIds.EdgeStyles.Refined, out var sharedEdge,
                    out var complementMask)
                && sharedEdge == forwardEdge && complementMask,
                "reverse-only fixture resolves the same TileSet with a complemented mask");
            var directedManifest = Manifest(Entry(0,
                "level.smoke.palette.directed-edge", directed));
            try
            {
                var built = BattlefieldMapPublicationExporter.TryBuildCatalog(
                    directedManifest, new[] { reverseOnly }, out var generated,
                    out var diagnostics);
                Assert(built && generated != null
                    && diagnostics.All(issue => !issue.IsBlocking),
                    "publication accepts a same-contour edge resource from the opposite material direction: "
                    + Format(diagnostics));
                if (generated != null) UnityEngine.Object.DestroyImmediate(generated);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(directedManifest);
            }

            var registryMissing = CreatePublishableMap("map.smoke.palette.registry");
            var registryFailure = ExpectBuildFailure(Manifest(Entry(0,
                "level.smoke.palette.registry", registryMissing)),
                Array.Empty<BattlefieldTerrainPalette>(), "publication.palette-not-registered");
            Assert(registryFailure.Diagnostics.Any(issue =>
                    issue.Code == "publication.palette-not-registered"
                    && issue.MapId == registryMissing.MapId
                    && issue.TemplateLevelId == BundledLevelCatalogIds.Levels.Orchard01),
                "registry omission identifies map and template");

            foreach (var asset in new[]
            {
                missingBase, missingLandform, directed, registryMissing,
            })
                UnityEngine.Object.DestroyImmediate(asset);
            UnityEngine.Object.DestroyImmediate(missingWaterBasePalette);
            UnityEngine.Object.DestroyImmediate(reverseOnly);
        }

        private static void ValidateManifestRebuildAndAtomicity(
            IReadOnlyList<BattlefieldTerrainPalette> palettes)
        {
            DeleteFixtureOutput(FixtureOutputPath);
            var mapA = CreatePublishableMap("map.smoke.publish-a");
            var mapB = CreatePublishableMap("map.smoke.publish-b");
            var manifest = Manifest(Entry(20, "level.smoke.publish-a", mapA));
            try
            {
                var first = Rebuild(manifest, FixtureOutputPath, palettes);
                Assert(first.Entries.Count == 1
                    && first.Entries[0].LevelId == "level.smoke.publish-a",
                    "first full rebuild contains exactly manifest A");
                var firstContent = SerializeCatalog(first);
                var firstMapA = CanonicalBattlefieldMapAuthoringSmoke.SerializeSource(
                    first.Entries[0].Map.ToSource());

                var idempotent = Rebuild(manifest, FixtureOutputPath, palettes);
                Assert(SerializeCatalog(idempotent) == firstContent,
                    "identical manifest rebuild is content-idempotent");

                manifest.Configure(new[]
                {
                    Entry(20, "level.smoke.publish-a", mapA),
                    Entry(10, "level.smoke.publish-b", mapB),
                });
                var both = Rebuild(manifest, FixtureOutputPath, palettes);
                Assert(both.Entries.Select(entry => entry.LevelId).SequenceEqual(new[]
                    {
                        "level.smoke.publish-b", "level.smoke.publish-a",
                    }), "full rebuild sorts by manifest order then stable level ID");
                Assert(CanonicalBattlefieldMapAuthoringSmoke.SerializeSource(
                        both.Entries.Single(entry => entry.LevelId
                            == "level.smoke.publish-a").Map.ToSource()) == firstMapA,
                    "adding B preserves unrelated published A content");

                manifest.Configure(new[] { Entry(10, "level.smoke.publish-b", mapB) });
                var cancelled = Rebuild(manifest, FixtureOutputPath, palettes);
                Assert(cancelled.Entries.Count == 1
                    && cancelled.Entries[0].LevelId == "level.smoke.publish-b",
                    "removing/cancelling A removes it from the full rebuilt output");

                manifest.Configure(new[] { Entry(20, "level.smoke.publish-a", mapA) });
                var beforeDraftChange = Rebuild(manifest, FixtureOutputPath, palettes);
                var publishedBeforeDraftChange = SerializeCatalog(beforeDraftChange);
                string reason;
                Assert(mapA.TrySetVisual(new Vector2Int(3, 2),
                    BattlefieldLayerIds.Surfaces.Soil,
                    BattlefieldLayerIds.Surfaces.StoneRoad, string.Empty, out reason), reason);
                var stillPublished = AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                    FixtureOutputPath);
                Assert(SerializeCatalog(stillPublished) == publishedBeforeDraftChange,
                    "draft edits remain isolated from the generated catalog until rebuild");
                var afterDraftRebuild = Rebuild(manifest, FixtureOutputPath, palettes);
                Assert(SerializeCatalog(afterDraftRebuild) != publishedBeforeDraftChange,
                    "later successful rebuild publishes the changed draft snapshot");

                var lastValid = SerializeCatalog(afterDraftRebuild);
                manifest.Configure(new[]
                {
                    Entry(0, "level.smoke.atomic-duplicate", mapA),
                    Entry(1, "level.smoke.atomic-duplicate", mapB),
                });
                var failed = BattlefieldMapPublicationExporter.Rebuild(manifest,
                    FixtureOutputPath, palettes);
                Assert(!failed.Succeeded && failed.Diagnostics.Any(issue => issue.IsBlocking),
                    "invalid replacement rebuild fails before publication");
                var afterFailure = AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                    FixtureOutputPath);
                Assert(SerializeCatalog(afterFailure) == lastValid,
                    "failed rebuild leaves the last valid generated output unchanged");

                ValidateCatalogReloadAndBundledRegression(afterDraftRebuild);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(manifest);
                UnityEngine.Object.DestroyImmediate(mapA);
                UnityEngine.Object.DestroyImmediate(mapB);
                DeleteFixtureOutput(FixtureOutputPath);
            }
        }

        private static void ValidateCatalogReloadAndBundledRegression(
            PublishedBattlefieldMapCatalog generated)
        {
            AssetDatabase.ImportAsset(FixtureOutputPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<PublishedBattlefieldMapCatalog>(
                FixtureOutputPath);
            Assert(reloaded != null && SerializeCatalog(reloaded) == SerializeCatalog(generated),
                "generated editor fixture survives save/import/reload");

            var bundled = BundledLevelCatalogFactory.CreateBundledSource();
            var composed = BundledLevelCatalogFactory.ComposePublished(bundled, reloaded);
            Assert(composed.Levels.Take(3).Select(level => level.LevelId)
                    .SequenceEqual(bundled.Levels.Select(level => level.LevelId))
                && composed.Maps.Take(3).Select(map => map.MapId)
                    .SequenceEqual(bundled.Maps.Select(map => map.MapId)),
                "published composition keeps all three bundled levels/maps in stable order");
            for (var index = 0; index < 3; index++)
                Assert(composed.Maps[index].GameplayFingerprint
                        == bundled.Maps[index].GameplayFingerprint,
                    "published composition preserves bundled map fingerprint at " + index);

            Assert(BattleContentCompiler.TryCompile(BundledBattleContentFactory.Create(),
                out var content, out var contentValidation),
                "bundled battle content compiles: " + Format(contentValidation));
            Assert(LevelCatalogCompiler.TryCompile(composed, content,
                out var compiled, out var levelValidation),
                "normal level catalog compiles generated content: " + Format(levelValidation));
            var published = reloaded.Entries.Single();
            var resolved = compiled.Resolve(published.LevelId);
            Assert(resolved.Succeeded
                && resolved.Value.Identity.LevelId == published.LevelId
                && resolved.Value.Identity.MapId == published.Map.MapId,
                "explicitly composed catalog resolves expected authored levelId/mapId");

            var production = BundledLevelCatalogFactory.CreateCompiled();
            Assert(production.PlayableLevels.Select(level => level.LevelId)
                    .SequenceEqual(bundled.Levels.Select(level => level.LevelId))
                && !production.Resolve(published.LevelId).Succeeded,
                "editor publication fixture does not enter the production playable catalog");

            var noGenerated = BundledLevelCatalogFactory.ComposePublished(bundled, null);
            Assert(noGenerated.Levels.Count == 3 && noGenerated.Maps.Count == 3,
                "absent generated resource leaves exactly three bundled levels/maps");
        }

        private static BattlefieldMapPublicationResult ExpectBuildFailure(
            BattlefieldMapPublicationManifest manifest,
            IEnumerable<BattlefieldTerrainPalette> palettes, params string[] expectedCodes)
        {
            try
            {
                var result = BattlefieldMapPublicationExporter.TryBuildCatalog(manifest,
                    palettes, out var generated, out var diagnostics);
                if (generated != null) UnityEngine.Object.DestroyImmediate(generated);
                var wrapped = new BattlefieldMapPublicationResult(result, generated, diagnostics);
                Assert(!result && expectedCodes.All(code => diagnostics.Any(issue =>
                        issue.Code == code && issue.IsBlocking)),
                    "expected publication failures " + string.Join(",", expectedCodes)
                    + "; actual=" + Format(diagnostics));
                return wrapped;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(manifest);
            }
        }

        private static PublishedBattlefieldMapCatalog Rebuild(
            BattlefieldMapPublicationManifest manifest, string path,
            IEnumerable<BattlefieldTerrainPalette> palettes)
        {
            var result = BattlefieldMapPublicationExporter.Rebuild(manifest, path, palettes);
            Assert(result.Succeeded && result.GeneratedCatalog != null,
                "publication rebuild succeeds: " + Format(result.Diagnostics));
            return result.GeneratedCatalog;
        }

        private static BattlefieldMapAuthoringAsset CreatePublishableMap(string mapId)
        {
            var map = CanonicalBattlefieldMapAuthoringSmoke.CreateValidMap(mapId);
            string reason;
            Assert(map.TrySetMarkerGroup("pots.smoke", 8, out reason), reason);
            foreach (var cell in new[]
            {
                new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2),
                new Vector2Int(3, 2),
            })
            {
                Assert(map.TrySetGameplay(cell,
                    new[] { BattlefieldLayerIds.Capabilities.Plantable },
                    Array.Empty<string>(), out reason), reason);
                string markerId;
                Assert(map.TryPlaceMarker(BattlefieldMarkerKind.InitialPotCandidate,
                    cell, "pots.smoke", out markerId, out reason), reason);
            }
            Assert(map.ApplyRecommendedPresentation(out reason), reason);
            CanonicalBattlefieldMapAuthoringSmoke.Compile(map);
            return map;
        }

        private static BattlefieldMapPublicationManifest Manifest(
            params BattlefieldMapPublicationManifestEntry[] entries)
        {
            var manifest = ScriptableObject.CreateInstance<BattlefieldMapPublicationManifest>();
            manifest.Configure(entries);
            return manifest;
        }

        private static BattlefieldMapPublicationManifestEntry Entry(int order,
            string levelId, BattlefieldMapAuthoringAsset map)
        {
            return new BattlefieldMapPublicationManifestEntry(order, levelId,
                BundledLevelCatalogIds.Levels.Orchard01, map);
        }

        private static void SetRouteCell(BattlefieldMapAuthoringAsset map, int index,
            Vector2Int cell)
        {
            var serialized = new SerializedObject(map);
            var cells = serialized.FindProperty("primaryRoute").FindPropertyRelative("cells");
            Assert(cells != null && index >= 0 && index < cells.arraySize,
                "serialized route fixture index exists");
            cells.GetArrayElementAtIndex(index).vector2IntValue = cell;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVisualEdgeStyle(BattlefieldMapAuthoringAsset map, int index,
            string edgeStyleId)
        {
            var serialized = new SerializedObject(map);
            var cells = serialized.FindProperty("visualCells");
            Assert(cells != null && index >= 0 && index < cells.arraySize,
                "serialized visual fixture index exists");
            cells.GetArrayElementAtIndex(index).FindPropertyRelative("edgeStyleId")
                .stringValue = edgeStyleId ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIntegerProperty(UnityEngine.Object target,
            string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            Assert(property != null, "serialized integer fixture property exists: "
                + propertyName);
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DeleteFixtureOutput(string path)
        {
            const string fixtureRoot =
                "Assets/Editor/Tests/Fixtures/CanonicalBattlefieldMap/";
            Assert(!string.IsNullOrWhiteSpace(path)
                    && path.StartsWith(fixtureRoot, StringComparison.Ordinal)
                    && path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase),
                "publication smoke may only delete its explicit editor fixture outputs");
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Assert(AssetDatabase.LoadMainAssetAtPath(path) == null,
                "publication smoke fixture output was not removed: " + path);
        }

        private static string SerializeCatalog(PublishedBattlefieldMapCatalog catalog)
        {
            if (catalog == null) return "<null>";
            return catalog.SchemaVersion + "|" + catalog.SourceCatalogId + "|"
                + catalog.ContentVersion + "\n" + string.Join("\n", catalog.Entries.Select(entry =>
                    entry == null ? "<null>" : entry.Order + "|" + entry.LevelId + "|"
                        + entry.TemplateLevelId + "|"
                        + (entry.Map == null ? "<null>"
                            : CanonicalBattlefieldMapAuthoringSmoke.SerializeSource(
                                entry.Map.ToSource()))));
        }

        private static string Format(IEnumerable<BattlefieldMapPublicationDiagnostic> values)
        {
            return string.Join(" | ", (values
                ?? Enumerable.Empty<BattlefieldMapPublicationDiagnostic>())
                .Select(value => value.ToString()).ToArray());
        }

        private static string Format(ContentValidationResult value)
        {
            return value == null ? "<null>" : string.Join(" | ",
                value.Issues.Select(issue => issue.ToString()).ToArray());
        }

        private static string Format(LevelCatalogValidationResult value)
        {
            return value == null ? "<null>" : string.Join(" | ",
                value.Issues.Select(issue => issue.ToString()).ToArray());
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(
                "Canonical battlefield publication smoke failed: " + message);
        }
    }
}
