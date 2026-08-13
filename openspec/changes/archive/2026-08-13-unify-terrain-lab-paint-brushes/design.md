## Context

The Scene Overlay currently renders `TerrainBrushDefinition` assets as a resource gallery. Clicking a resource rewrites the laboratory target's generic A/B configuration, after which the author must make a second selection from a separate two-card A-on-B / B-on-A grid before painting. The original square grass/soil family shown by the initial laboratory target is direct target configuration rather than a registered resource, so it has no card and is replaced visually when another definition is applied.

The laboratory target intentionally stores only generic A/B markers and exercises one material family at a time. Switching resources therefore keeps the authored logical shapes and reprojects the complete canvas through the newly selected family; it does not create mixed-family runtime data. This change keeps that diagnostic boundary and removes the duplicated selection surface around it.

## Goals / Non-Goals

**Goals:**

- Make one visible gallery the complete ordinary paint selector.
- Expand every registered resource into two reciprocal, directly paintable direction tiles.
- Keep the gallery compact with four tiles per row and smaller centered artwork.
- Configure the resource, select the direction, and enter painting from the same click.
- Register and retain the original square grass/soil laboratory artwork through the same definition/registry path.
- Guarantee a paintable reverse direction even when the palette has no reusable reverse landform, using a definition-owned complemented view of the same full-composite sixteen-mask resource.
- Preserve stable ordering, square preview geometry, Undo, Overlay input teardown, and existing runtime output.

**Non-Goals:**

- Mixing different registered material families on one generic A/B laboratory canvas.
- Changing canonical map serialization, gameplay, persistence, release scenes, or player-facing WebGL UI.
- Generating new terrain pixels or modifying the original organic textures and TileSets.
- Moving erase and clear operations into the ordinary brush gallery.

## Decisions

### One directional choice model drives both drawing and hit testing

The registry will expose a small editor-only directional choice value containing a definition and a reverse flag. Each valid definition yields forward and reverse choices in a stable order, with the preserved original family first and remaining resources ordered by `(brushId, direction)`. The Overlay will calculate one grid from these choices and use the same rectangles for card drawing and button hit testing.

Selecting a choice will stop the previous gesture, apply the definition only when the target is not already configured for it, set A-on-B or B-on-A, disable contextual pure-only mode, and start painting. The old registered-resource cards, ordinary A/B card grid, and primary pure-only checkbox will not be drawn.

Resource application is allowed on a non-empty laboratory canvas. Existing generic A/B marker cells remain in place and are rebuilt with the selected resource, so every visible card is genuinely switchable without a hidden clear prerequisite.

Alternative considered: keep the resource cards and automatically focus the second selector. This retains two concepts and does not meet the one-click selection requirement.

### Four-column compact gallery

The Overlay lays out directional choices in four equal-width columns. Cards keep a fixed compact height, use the same rectangles for drawing and hit testing, and center a smaller square artwork preview above the wrapped direction label. Additional resources add rows inside the existing scroll view rather than widening the Overlay.

### Registration owns a reversible fallback view

`TerrainBrushDefinition` will reference a companion `DualGridTileSet` whose mask `m` points at the primary composite resource's tile `Complement(m)`. The importer creates or updates this companion without copying or changing pixels. Normal reusable palette landforms remain preferred. When the reverse surface has no palette landform (currently water), the laboratory uses the companion as the reverse landform layer while the existing complemented edge resolution remains authoritative.

This makes both directions paintable from one registered resource without inventing an unrelated water art family or silently borrowing another contour. Validation checks the companion against the primary TileSet and requires a renderable mask-00 endpoint.

Alternative considered: add a global water landform binding to the production palette. A pair-specific full-composite resource is not a reusable water material and would leak invalid combinations into canonical map authoring.

### Original square grass/soil becomes a normal registered definition

Editor setup will create/update a laboratory-only definition under the existing registered brush root that points to the original 32-pixel grass/soil endpoints, square landform TileSets, and refined square edge used by the initial target. A small source record identifies those assets as preserved project-authored originals. The definition participates in the same laboratory registry while its duplicate semantic palette key remains isolated from the newer production composite definition; existing organic Palette compatibility remains untouched.

The source PNGs, TileSets, `.meta` files, and GUIDs remain unchanged. This is laboratory registration and migration metadata, not an art conversion or a replacement of production Palette authority.

### Active Overlay remains expanded

The native Scene Overlay is kept displayed and expanded while the laboratory session is open. Programmatic brush activation and Scene focus changes reassert panel form, and a collapsed-state callback restores the panel on the next editor tick. Explicit laboratory teardown still removes the Overlay and releases the paint session.

### Authoring validation remains recoverable

The painter uses structural authoring validation for target discovery, brush activation, mutations, and preview rebuilding. Strict configuration validation continues to report inconsistent authored content such as a partially refined connected edge region, but that report no longer prevents the author from selecting a brush and repairing or reprojecting the canvas. This avoids a circular failure where the tool required already-correct content before it could edit that content.

## Risks / Trade-offs

- [Legacy or manually edited cells fail strict whole-canvas validation] -> Keep strict diagnostics, but gate editing on structural authoring readiness so the laboratory can repair the data.

- [Switching resources changes the visual meaning of existing generic A/B cells] → Keep cell coordinates and logical A/B data unchanged, rebuild the whole laboratory preview through the selected resource, and state this one-family-at-a-time boundary in the Overlay.
- [Unity restores a persisted collapsed Overlay state after attachment] → Listen for `collapsedChanged` and reassert expanded panel state after brush activation and Scene focus changes.

- [A complemented full-composite fallback is drawn in both landform and edge outputs] → Use it only when the palette lacks the reverse reusable landform; both references resolve to the same opaque pixels, and focused tests compare the mask mapping and rendered availability.
- [Old scenes have no stored active brush id] → Match target contour, endpoints, landforms, edge, and author-facing names against registered definitions when the Overlay opens or the target changes.
- [Adding the original definition changes registry counts and order] → Assert stable ids and two choices per definition in focused registry and painter smokes.
- [Editor changes accidentally enter the player] → Keep choice/registry orchestration under `Assets/Editor` and run aggregate editor smoke, including player compilation; ordinary WebGL and safe-area behavior remain unchanged because no player UI geometry changes.
- [Setup mutates generated assets unexpectedly] → Create/update only the definition, companion TileSet, and source record; reuse existing art assets and preserve their GUIDs.
