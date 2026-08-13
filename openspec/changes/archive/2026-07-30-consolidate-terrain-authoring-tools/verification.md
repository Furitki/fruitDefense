# Verification evidence

## Result

The shared-edge implementation is complete and automated validation passes. One same-contour edge TileSet now supplies both directed brush cards: its authored direction uses the mask directly, while the opposite direction uses the complemented 4-bit mask. Exact reverse bindings still win when legacy content explicitly provides one.

The current acceptance configuration retains the two existing brush cards and the contextual `只绘制纯图` checkbox. No candidate resource was deleted.

## Unity compile and smoke

- Unity `6000.3.19f1` imported and compiled the changed runtime and Editor assemblies in an isolated copy of the project.
- `FruitDefense.Editor.ProjectSetup.SmokeValidate` passed on 2026-07-30. The retained log is `Logs/consolidate-terrain-authoring-tools-smoke.log`.
- Relevant passing markers include `CANONICAL_BATTLEFIELD_MAP_EDITOR_SMOKE_OK`, `CANONICAL_BATTLEFIELD_MAP_PUBLICATION_SMOKE_OK`, `FRUIT_DEFENSE_LAYERED_TERRAIN_TILEMAP_OK`, `FRUIT_DEFENSE_LAYERED_TERRAIN_PAINTER_OK`, `FRUIT_DEFENSE_BATTLEFIELD_DUAL_GRID_TERRAIN_OK productionMaps=3 acceptanceFixtures=1`, and `FRUIT_DEFENSE_SMOKE_OK`.
- The focused tests verify direct masks, complemented reverse masks, the B-on-A full-interior `1111 → 0000` endpoint, rejection of an unoccupied source `0000`, exact-reverse override compatibility, unavailable unrelated pairs, the two-card chooser, contextual pure-only behavior, and native Overlay lifecycle/identity.

## Runtime parity

- `BattlefieldTerrainPalette` resolves exact bindings first and same-contour opposite-direction bindings second, returning whether the caller must complement the mask. A shared reverse fallback is available only when its TileSet has a renderable mask-00 endpoint.
- The Battle presenter renders a missing reverse direction from the canonical TileSet with `mask ^ 15`, skips empty source masks before complementing, retains a complemented-empty result as the reverse center's mask-00 tile, and avoids duplicate fallback output when both exact directions exist.
- Canonical map editor availability and publication validation use the same palette resolution contract, so authoring, publication, acceptance preview, and Battle runtime agree.
- The aggregate smoke passed all three production maps and one acceptance fixture without changing gameplay-map semantics.

## OpenSpec

- `openspec validate consolidate-terrain-authoring-tools --type change --strict --json` passed.
- `openspec validate --all --strict --json` passed all `41` checked specifications/changes; the only output beyond passes was an informational note.

## Resource disposition

- Current resources to keep and non-destructive deletion candidates are recorded in `resource-cleanup-candidates.md`.
- The first duplicate candidate is `Assets/LayeredTerrain/GrassSoil/EdgeSoilOnGrassRefined/`, but serialized trial references and its compatibility test must be migrated or retired before any deletion.
- Organic contour, trial/debug, source/provenance, and topology folders remain separate conditional categories. They were not treated as duplicates of the current square family.

## Future registration contract

- The delta for `layered-terrain-brush-authoring` makes this rule apply to every future registered directed pair brush rather than only the current grass/soil acceptance target.
- Exact directed art remains the first choice. Shared fallback is limited to the same unordered material pair, contour, and edge style, requires a renderable mask-00 endpoint, rejects an empty source mask before complementation, and preserves a full reverse interior through mask 00.
- The still-active `support-multiple-terrain-contour-styles` delta carries the same rule so its later archive cannot restore the superseded reverse-resource requirement.

## Remaining limitation

- Real Scene-view visual inspection is still pending. The active Unity window was minimized and could not be safely activated by automation; the existing laboratory screenshot shows a blank Scene view and is intentionally not accepted as evidence. Functional Overlay coverage passed, but task 3.3 remains open until a valid dock/collapse/float Scene-view capture is made.
- No WebGL package was rebuilt for this focused authoring/runtime-resolution change. This evidence does not authorize Douyin or WeChat support.
