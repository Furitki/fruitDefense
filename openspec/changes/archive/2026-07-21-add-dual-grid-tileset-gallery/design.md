## Context

`DualGridTilemapEditor` currently renders four object fields and a manual Scene-view paint toggle. Authors must remember asset locations and open the object picker to change the component-wide `DualGridTileSet`, even though the project already has several reusable sets. The runtime component resolves only occupied versus empty logical cells and owns one TileSet for the entire generated layer, so the feature must remain an editor-only whole-layer selector.

## Goals / Non-Goals

**Goals:**

- Discover every project `DualGridTileSet` without maintaining a serialized registry.
- Show stable, pixel-preserving visual cards directly below the existing Tile Set field.
- Make a valid card one-click selectable with Undo, immediate validation/rebuild, scene dirtiness, and clear selected feedback.
- Refresh discovery when assets are added, removed, renamed, moved, or regenerated.
- Keep discovery and preview work cached so Inspector repaint remains cheap.
- Cover the authoring workflow from the required aggregate editor smoke entry.

**Non-Goals:**

- Painting different terrain identities into cells of one logical Tilemap.
- Replacing Unity's Tile Palette or changing the manual occupancy paint controls.
- Persisting generated preview PNGs, modifying TileSet assets, or changing runtime mask resolution.
- Changing gameplay, WebGL UI, safe-area behavior, persistence, or release scenes.

## Decisions

### Keep the object field and add a cached gallery below it

The existing serialized `tileSet` object field remains the authoritative fallback for direct references and debugging. A gallery beneath it searches `Assets` with `AssetDatabase.FindAssets("t:DualGridTileSet")`, loads the matching assets, removes null or duplicate entries, and sorts by asset path for deterministic layout.

A static editor-only cache is invalidated by `EditorApplication.projectChanged`, with an explicit Refresh button for immediate recovery. Scanning on every `OnInspectorGUI` repaint is rejected because Inspector layout and repaint events are frequent.

### Draw previews from the TileSet's real corner sprites

Each card draws a standard 2×2 island from the four single-corner masks (`SE`, `SW`, `NE`, `NW`) using the Sprite texture and UV rectangle already referenced by each `Tile`. This exposes material, outline, and transition character while preserving Point-filtered source pixels. If a custom `TileBase` does not expose a Sprite directly, the card falls back to Unity's cached asset preview or mini thumbnail.

The preview is composed in IMGUI and cached through existing asset references; no PNG or runtime asset is written. Drawing only mask `15` is rejected because it hides the transition edge the author needs to compare.

### Treat selection as an explicit whole-layer operation

Clicking a valid card records the component and generated Tilemap with Undo, assigns the serialized TileSet reference, rebuilds immediately when the configuration is valid, marks the scene dirty, and keeps manual paint mode unchanged. Invalid sets remain visible with a disabled/error presentation so discovery problems are diagnosable without allowing a broken assignment.

The gallery labels itself as a whole-layer style selector. Per-cell material painting is rejected for this change because `DualGridMaskUtility.Resolve` currently consumes only `HasTile`; supporting multiple materials would require a new logical data model and mixed-terrain resolution rules.

### Keep testable logic outside card drawing

Discovery, deterministic sorting, preview-source lookup, and selection/rebuild are exposed as editor-internal helpers. `DualGridTilemapSmoke` exercises those helpers without depending on mouse coordinates, while the Inspector uses the same paths for visible cards and hit handling.

## Risks / Trade-offs

- [A large project contains many TileSets] → Cache discovery, use compact responsive rows, and provide a manual refresh button.
- [Asset previews are unavailable immediately or use a custom TileBase] → Draw concrete `Tile.sprite` data first and fall back to Unity preview/thumbnail APIs.
- [An invalid TileSet is discovered] → Show it but disable selection and expose its validation reason in the tooltip/status.
- [Switching a set unexpectedly restyles existing terrain] → Label the control as applying to the whole layer and record a single Undo operation covering assignment and generated output.
- [Asset changes leave stale cards] → Invalidate on project changes and expose explicit refresh.

## Migration Plan

No serialized or runtime migration is required. Existing `DualGridTilemap` components retain their TileSet references and gain the gallery automatically when inspected. Rollback removes the editor gallery/helper code and smoke assertions; component and TileSet assets remain compatible.

## Open Questions

- Per-cell multi-terrain painting remains a separate future capability requiring an explicit terrain identity model.
