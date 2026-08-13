## Context

The terrain laboratory no longer opens a standalone `EditorWindow`; `LayeredTerrainPainterWindow` is a compatibility facade and `LayeredTerrainSceneLaboratory` draws a fixed `GUILayout.BeginArea` during `SceneView.duringSceneGui`. Painting and Undo already live in `LayeredTerrainPaintSession`. The accepted ordinary interaction is two directed composition cards in one row plus `只绘制纯图`. Each target supplies one configured contour; one edge TileSet can describe both pair directions because the reverse direction uses the complemented four-corner mask.

The fixed Scene GUI area still owns custom panel placement, collapse/close controls, input reservation, and lifetime. It looks like an ad-hoc floating window and its title does not make the non-playable resource-validation boundary obvious.

## Goals / Non-Goals

**Goals:**

- Preserve the current two brush cards, preview composition, pure-only checkbox, target selection, painting, and Undo while allowing one edge resource to satisfy both pair directions.
- Host those controls in a Unity-native Scene Overlay that authors can dock, float, collapse, and close through standard editor behavior.
- Identify the surface as terrain-resource acceptance, show the target's configured contour as read-only context, and direct playable-map work to the canonical map editor.
- Keep the existing launch menu and Inspector entry working through a compatibility facade.

**Non-Goals:**

- Adding brush presets, changing the canonical map editor, changing map serialization, exposing a contour switch in the laboratory, generating missing reverse assets, or deleting candidate assets before review.
- Rewriting the accepted IMGUI controls in UI Toolkit.
- Changing runtime terrain rendering, gameplay, builds, or platform support.

## Decisions

### Use an instance `IMGUIOverlay` in the active Scene view

The menu launch creates a native `UnityEditor.Overlays.IMGUIOverlay` instance and adds it to the active Scene view. The overlay calls the existing IMGUI drawing functions, so brush artwork, labels, help boxes, and testable utility methods remain stable. Unity owns docking, floating, collapsing, clipping, and the panel title bar.

An instance overlay is preferred to an always-registered persistent overlay because the laboratory is an explicit diagnostic session: it should not appear automatically in every Scene view or survive as an apparently ordinary authoring tool when no valid laboratory target exists.

### Keep one session service and make overlay lifetime authoritative

`LayeredTerrainSceneLaboratory` continues to own the single `LayeredTerrainPaintSession`, target discovery, selection hooks, and painting teardown. Opening adds one overlay to the focused Scene view; repeated opening focuses/reuses the same session rather than adding duplicates. Hiding, closing, destroying, assembly reloading, play-mode transition, or invalidating the target stops painting and releases Scene input.

The old hand-calculated panel rectangle and duplicate collapse/header controls are removed. Scene painting continues through the existing session subscription. Native overlay input handling replaces the manual reserved-area hit test; focused tests verify clicking the overlay cannot mutate terrain.

### Keep the accepted brush model and share one edge resource across directions

The primary brush array remains `AOnB` and `BOnA`. `只绘制纯图` remains a contextual checkbox that writes the selected brush's opaque foreground endpoint. Edge resolution first uses an exact directed binding when one already exists for compatibility. Otherwise it resolves the opposite ordered binding in the same contour and edge style and complements the 4-bit mask (`mask ^ 15`) before selecting the sprite. A shared fallback therefore requires a renderable mask-00 endpoint: a full reverse source mask complements to mask 00 and must still render the reverse material center. An empty source mask is rejected before complementation so unrelated empty vertices do not become mask 15. This reuses the authored material-side feather correctly without flipping pixels, generating textures, or crossing contour styles.

New acceptance configuration registers only the currently selected canonical edge family. Legacy exact reverse bindings remain readable so existing scenes and palettes do not break, but they are no longer required or newly generated.

### Inventory before deleting resources

No asset is deleted automatically. A change-local inventory separates current active resources, compatibility-only reverse resources, optional organic contour families, and provenance/debug sources. A later explicit cleanup can remove only candidates proven to have no serialized, test, validation, or documentation dependency.

### Add read-only identity and boundary language

The overlay title becomes `地貌资源验收`. Its content shows `当前轮廓：方形` or `当前轮廓：自然` from `ActiveContourStyleId`, and states that it validates terrain resources without producing a playable map. No contour selector is added.

The compatibility class may keep its historical C# name until all external callers migrate, but user-visible copy, test names, and specification language use `Overlay` or `资源验收` rather than `Window`.

## Risks / Trade-offs

- [Native Overlay input reaches `duringSceneGui` differently across editor layouts] → Add a focused editor test and manual Scene check that overlay interaction never paints; retain a narrowly scoped pointer guard only if Unity's native event ownership proves insufficient.
- [Closing or hiding an overlay leaves the static paint session subscribed] → Bind overlay display/destruction callbacks to one idempotent teardown path and test subsequent Scene clicks.
- [An IMGUI Overlay sizes differently when docked] → Give the overlay bounded minimum/default/maximum sizes, keep the existing scroll view, and test brush-card geometry at narrow and normal widths.
- [Compatibility facade keeps conceptual debt] → Keep it code-only and small; remove it in a later migration only after Inspector and acceptance callers no longer reference it.
- [Complementing an empty or full reverse mask paints the wrong tile] → Treat an unresolved empty source mask as no edge before complementing, but render a complemented empty mask through the shared resource's required mask-00 endpoint so the reverse center is preserved.
- [A legacy exact reverse asset changes appearance if silently replaced] → Exact directed bindings win; shared complemented resolution is fallback-only until the candidate resource is explicitly deleted.

## Migration Plan

1. Add the native Scene Overlay host and route the existing launch facade to it.
2. Move existing panel content into the overlay without changing brush state or painting operations.
3. Add shared reverse-mask edge resolution to the laboratory and runtime palette/presenter while preserving exact reverse compatibility.
4. Configure the acceptance surface around the current canonical edge family and record, but do not delete, cleanup candidates.
5. Add read-only contour and resource-acceptance guidance, then update focused tests and README wording.
6. Remove the fixed-area geometry, custom header/collapse state, and reserved panel rectangle after native input ownership is verified.

Rollback restores the fixed Scene GUI host; no scene, map, palette, or runtime data migration is required.

## Open Questions

- None for this scoped change. A broader canonical-map brush redesign remains explicitly outside this work.
