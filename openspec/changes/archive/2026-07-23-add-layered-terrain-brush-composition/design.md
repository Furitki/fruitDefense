## Context

The completed layered-map migration deliberately separates presentation from gameplay, but its presentation side still stores one exclusive semantic surface per cell. `BattlefieldTerrainPalette` then supplies one opaque palette-wide soil texture and one transparent Dual-Grid TileSet per non-soil surface. This preserves current maps, but it cannot represent a reusable transparent landform independently from its underlay, cannot paint a material as either base or landform, and cannot enable or disable an authored `A on B` edge treatment.

The existing Dual-Grid resolver, half-cell projection, clipping, and sixteen-mask contract are stable and should remain the topology authority. AI-authored raster work should improve only the visible material-contact ribbon; it must not become a runtime mask generator or a gameplay dependency.

## Goals / Non-Goals

**Goals:**

- Represent every visual cell with one required base material, one optional landform material, and an optional ordered-pair edge style.
- Let authors paint a pure base, a landform over the existing base, or an ordered pair in either direction.
- Reuse the same semantic material as an opaque base and a transparent Dual-Grid landform.
- Keep pair-specific second-pass edge art optional and separate from the reusable landform silhouette.
- Preserve projection, safe-area clipping, gameplay affordances, deterministic simulation, snapshots, and release flow.
- Validate topology seams automatically and accept the final look from a real portrait WebGL canvas.

**Non-Goals:**

- Arbitrary numbers of visual overlays, height blending, shaders that synthesize transitions, runtime image generation, or free-form material graphs.
- Inferring placement, route, collision, marker, or combat behavior from visual layers.
- Automatically generating every possible material pair; only explicitly registered ordered pairs are supported.
- Adding mountains, castles, forests, or other large decorations to repeatable terrain tiles in this change.

## Decisions

### Compile one typed visual cell instead of parallel mutable maps

The versioned map source gains a `BattlefieldVisualCellSource` per logical cell with `baseSurfaceId`, optional `landformSurfaceId`, and optional `edgeStyleId`. The compiler validates exact coverage and finite IDs, then exposes immutable `BaseSurfaceAt`, `LandformSurfaceAt`, and `EdgeStyleAt` queries. The compatibility `SurfaceAt` view returns the landform when present and otherwise the base while migrated callers are updated.

This is preferred to three independently sized arrays or ScriptableObjects because dimensions, cell ownership, and validation stay atomic. One optional landform is deliberately bounded; an arbitrary stack would multiply sorting, edge, identity, and authoring ambiguity.

### Treat ordered pair edges as authored presentation assets

A terrain palette contains material bindings and ordered pair-edge bindings. A material binding supplies a stable surface ID, an opaque repeatable base sprite/texture, and a transparent sixteen-mask landform TileSet. An edge binding is keyed by foreground surface, background surface, and stable edge-style ID and supplies a transparent sixteen-mask edge-only TileSet.

`A on B` and `B on A` never alias. Missing or reversed pairs fail validation when explicitly requested. Edge style is optional; an empty style draws only the reusable landform. Runtime code selects masks and draws sprites but never rasterizes, blurs, expands, recolors, or repairs edge pixels.

Flattened pair previews may be retained as acceptance evidence, but runtime source assets remain base, landform, and edge responsibilities so the landform can be reused over another base.

### Resolve base cells directly and landforms through the existing Dual-Grid contract

Base material draws as one opaque cell-aligned tile per logical cell. For each registered landform material, the presenter resolves the existing NW=`1`, NE=`2`, SE=`4`, SW=`8` mask from equality with `landformSurfaceId` and draws one vertex-centered transparent sprite. For each requested ordered edge binding, it resolves the same topology over cells carrying that landform and style, then draws the transparent edge sprite above the landform.

Pair-edge validation requires the authored background ID to match the base material beneath the participating landform cells. A refined connected component may not mix pair background or style identities; authors can split it into separate components or use the generic landform edge. This avoids creating false internal borders between visually continuous cells.

Base squares, landforms, and edge sets share native size, pixels-per-unit, pivot, sampling, and transparent-bleed rules. The base layer is cell-centered; the two Dual-Grid outputs retain the established negative half-cell alignment.

### Provide explicit paint operations over one canonical authoring state

The editor workflow operates on the typed visual-cell source and exposes three operations:

1. **Base** writes the selected material and clears any now-invalid ordered edge while leaving a valid landform intact.
2. **Landform** writes or erases only the selected foreground and optionally selects a registered edge style compatible with the existing base.
3. **Pair** atomically writes the selected background, selected foreground, and optional ordered edge style. Swapping the selectors produces the reverse composition.

Undo records one paint gesture, generated previews refresh only affected cells/vertices, and invalid pair choices are disabled with a visible reason. Pure-base painting clears the landform and edge for the target cell.

### Constrain AI edge refinement to a fixed topology socket

The art workflow starts from approved seamless base materials and exact sixteen-mask alpha topology. A second AI edit receives a tiled contact sheet and may change only the material-contact ribbon. The alpha topology and a protected perimeter socket strip remain invariant so compatible tile borders stay pixel-identical. Output is reviewed as an assembled board, not as sixteen unrelated thumbnails.

AI-authored output is accepted only after checking full fill, single corners, straight edges, concave/convex turns, one-cell islands/holes, masks `5` and `10`, both pair orders, edge enabled/disabled, actual battle scale, and WebGL sampling. Generated art is not accepted from prompt compliance alone.

### Keep presentation identity separate from gameplay identity

Base surfaces, landforms, edge styles, palette resources, and raster hashes remain presentation-only. Gameplay map fingerprinting, snapshots, route sampling, collision, and battle outcomes remain unchanged. A separate presentation signature may be used by validation/evidence but never by deterministic simulation.

## Risks / Trade-offs

- [Pair-specific edges grow quadratically with material count] → Register only approved directed pairs and retain a generic no-pair landform fallback selected explicitly by the author.
- [Per-cell edge toggles can create internal borders] → Validate connected landform components for one background/style identity and clear invalid styles after base edits.
- [AI edits drift mask sockets or alpha] → Protect perimeter sockets, compare compatible RGBA borders, retain the exact alpha guide, and reject failing outputs.
- [Opaque base tiles repeat visibly] → Author low-frequency seamless bases and allow several deterministic visual variants later without changing this two-layer contract.
- [Bilinear sampling produces fringe seams] → Require clamp sampling, consistent native size, transparent color bleed, no mipmaps for the accepted sprite scale, and actual WebGL capture review.
- [Migration changes visual data despite identical gameplay] → Derive soil bases and existing grass/road landforms deterministically, keep edge style empty, and compare gameplay fingerprints plus accepted captures.

## Migration Plan

1. Finish or archive the completed layered-map dependency before this change is archived so its specifications merge first.
2. Add the typed visual cell and compatibility queries, migrate bundled sources to soil base plus existing grass/stone landforms, and prove gameplay identity/outcome parity.
3. Extend terrain palette bindings and renderer composition while retaining a compatibility constructor for current project setup during migration.
4. Add the editor brush state, pair validation, Undo, preview rebuild, and a non-release layered terrain demo.
5. Add one accepted two-material sample with both ordered pair directions and optional second-pass edges.
6. Run focused editor smoke, aggregate project smoke, Unity compilation, ordinary WebGL build, and portrait visual acceptance.

Rollback restores the single-surface compatibility source and existing palette renderer; no gameplay save, economy, platform adapter, or backend migration is required.

## Open Questions

- Whether the new base/landform authoring model should be synchronized into the stable game-design overview remains a user decision after implementation and visual acceptance.
