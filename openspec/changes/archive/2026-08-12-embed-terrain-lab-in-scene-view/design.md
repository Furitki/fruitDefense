## Context

The terrain laboratory currently has two editor surfaces: `LayeredTerrainPainterWindow` draws all semantic brush controls, while `LayeredTerrainPaintSession` captures input and draws hover feedback in a separate Scene view. The split preserved Scene navigation and reused the real Tilemaps, but it turned a simple experiment into a window-management task and made the word “laboratory” refer to the control popup rather than the canvas where work happens.

The existing session already owns the important behavioral boundary: validated target/tool state, Scene callbacks, gesture deduplication, one-gesture Undo, dirty marking, and teardown. The change therefore needs a new host for the controls, not a new terrain model or painting engine.

## Goals / Non-Goals

**Goals:**

- Make the active Scene view the one ordinary terrain-laboratory workspace.
- Keep the full semantic brush workflow visible inside that workspace without opening another editor window.
- Preserve exact target resolution, contour validation, directed-edge validation, Undo grouping, and session teardown.
- Keep existing launch callers working while migrating them away from standalone-window behavior.
- Make panel draw geometry and interaction geometry share the same calculated rectangle.

**Non-Goals:**

- Reimplementing a scene camera, grid renderer, or Tilemap canvas inside a custom `EditorWindow`.
- Changing terrain serialization, the two-material composition contract, generated output ownership, runtime rendering, gameplay, persistence, release scenes, or platform behavior.
- Synchronizing editor-tool ergonomics into the game-design overview.
- Adding player-visible or WebGL UI.

## Decisions

### Use a Scene-view IMGUI side panel owned by one editor service

Add an editor-only laboratory service that owns one `LayeredTerrainPaintSession`, subscribes to `SceneView.duringSceneGui` while the laboratory is open, and draws a bounded side panel with `Handles.BeginGUI`. This keeps the real Scene camera, grid, selection, and Tilemap rendering while putting the controls in the same window.

The panel rectangle is calculated once from the Scene view size and used for both drawing and pointer capture. A narrow collapsed header remains available when the author wants more canvas. Collapsing does not stop painting; closing the laboratory does.

A custom editor window with a home-grown canvas was rejected because it would duplicate Scene camera, picking, zooming, and grid behavior. A Unity `Overlay` subclass was considered, but an IMGUI service matches the existing code and acceptance environment, avoids serialized overlay layout state, and remains portable across the project's Unity 6 editor setup.

### Keep the paint session as the only mutation controller

Move only UI hosting and target discovery out of `LayeredTerrainPainterWindow`. The embedded service calls the existing session setters and `LayeredTerrainTilemap` operations. The session remains responsible for Scene paint capture and Undo, so the new UI cannot create a second active brush or write generated Tilemaps directly.

The session receives a reserved-GUI rectangle from the host. Paint hover and mouse gestures ignore that rectangle, ensuring panel clicks cannot also paint cells. The active brush badge is consolidated into the embedded panel instead of drawing a second floating badge over the Scene.

### Convert the old window API into a compatibility facade

`LayeredTerrainPainterWindow.Open(...)` remains callable for the current Inspector and any acceptance helpers, but it delegates to the embedded laboratory service and never calls `GetWindow`. This avoids breaking callers while removing the extra window from the ordinary flow. The former window class becomes a small obsolete-free facade rather than a second UI implementation.

The menu item stays at `Fruit Defense/地图工具/地貌素材实验室`. Activation focuses the last Scene view, resolves the selected valid target or sole valid candidate, frames/selects the target when appropriate, and repaints all Scene views. Ambiguous scenes show the target picker in the panel and keep painting inactive.

### Give open, collapse, stop-painting, and close distinct meanings

- **Open**: show the panel and resolve a target; it does not force painting to start.
- **Collapse**: retain target, tool, and active session while reducing the UI to a header.
- **Stop painting**: release paint input but keep the panel open for configuration.
- **Close**: stop and dispose the session, release callbacks, and hide the panel.

This keeps destructive lifecycle boundaries explicit and lets authors recover canvas space without losing work state.

### Keep validation editor-only and proportional

Focused editor smoke verifies target resolution, one-host state, open/collapse/close semantics, reserved panel hit testing, and compatibility launch behavior in addition to the existing brush/Undo checks. Aggregate editor smoke remains the required integration gate. Because no runtime code or player-visible UI changes, a new WebGL build or safe-area capture is not required for this iteration; the existing runtime parity contract remains unchanged.

## Risks / Trade-offs

- [The panel can cover useful canvas space on a narrow Scene view] → Clamp its width, support a one-click collapsed header, and keep Scene navigation available outside the calculated panel rectangle.
- [Two `duringSceneGui` subscribers can process one event] → Draw the panel before the paint session uses the event and reserve the exact panel rectangle in the session so control clicks never become paint gestures.
- [Static editor state can survive selection changes unexpectedly] → Re-scan candidates on hierarchy/selection changes only while not actively painting, and require explicit target changes to stop the current session.
- [Domain reload can leave a visually stale panel] → Initialize the service through editor load, recreate state lazily, and unsubscribe through assembly-reload and play-mode hooks.
- [Compatibility facade could hide a lingering standalone window from a prior domain] → Remove all `GetWindow<LayeredTerrainPainterWindow>` calls; existing old instances dispose on reload and cannot be reopened by current entry points.

## Migration Plan

1. Introduce the Scene-embedded laboratory service and reserved-panel support in the paint session.
2. Redirect the menu and Inspector launch path through the compatibility facade.
3. Extend focused smoke coverage for embedded hosting and lifecycle behavior.
4. Run editor compilation, focused smoke, aggregate smoke, and strict OpenSpec validation.

Rollback restores the former window implementation and removes the embedded host. Authored terrain remains compatible because no runtime or serialized authoring model changes.

## Open Questions

None for the first embedded version. If Unity's native Overlay persistence becomes valuable later, it can replace the IMGUI host without changing the session or terrain contract.
