## Context

The runtime UI is implemented entirely in `FruitDefenseGame.OnGUI` with fixed rectangles. The first portrait pass changed the design resolution to the iPhone 17 physical resolution and mapped the old 475-by-842 landscape side panel into a centered portrait column. A live deployed WebGL capture showed two independent failures: the transformed side panel wastes horizontal space and preserves the wrong hierarchy, while `Font.CreateDynamicFontFromOSFont` yields no usable Chinese font in WebGL, leaving every label blank. The normal browser automation connection also fails in this environment, and waiting for a headless screenshot process to exit hangs because the Unity player renders continuously.

## Goals / Non-Goals

**Goals:**

- Deliver a portrait-first information hierarchy at the iPhone 17 logical reference of 402 by 874 points.
- Keep the battlefield large, the build loop immediately usable, and the primary battle action persistently visible.
- Render all Chinese copy from a bundled, redistributable font in WebGL.
- Preserve the existing simulation and drag/drop semantics.
- Make live WebGL visual evidence a repeatable pre-publish gate.

**Non-Goals:**

- Replacing IMGUI with uGUI/UI Toolkit or rewriting gameplay simulation.
- Redesigning combat balance, wave content, art style, or save data.
- Supporting landscape as an equal presentation mode in this change.
- Building a general-purpose pixel-diff service.

## Decisions

### Use logical points and explicit portrait regions

Layout calculations will use a 402-by-874 logical reference and derive a content rectangle from `Screen.safeArea`. The top-level regions will be a compact HUD, a full-width battlefield, a build tray, and a persistent battle-action row. Rectangle helpers will derive controls from these regions; the legacy `SideLayoutRect` transform will be removed.

Using the device's physical 1206-by-2622 pixels as authoring coordinates was rejected because it hides mobile typography and touch-size errors behind a large numeric scale. Retaining the legacy side-panel transform was rejected because it structurally reserves space for desktop information density.

### Keep IMGUI for the scoped rebuild

The change will refactor the current drawing functions into portrait-specific region helpers while retaining IMGUI and the existing input model. This keeps gameplay risk small and makes the presentation change reviewable. A uGUI or UI Toolkit migration could improve long-term maintainability but would combine an architecture migration with an urgent player-facing repair.

### Make secondary information contextual

Equipment, nursery results, refresh, and wave actions remain in the main flow. Selected-plant details open as a dismissible or collapsible bottom sheet layered above the build tray, and transient guidance remains a toast/status strip. This removes the permanently empty details block without hiding the build loop.

### Bundle a licensed CJK font

A redistributable Chinese-capable font asset will be stored under `Assets/Resources/Fonts` with its license, imported by Unity, and loaded through `Resources.Load<Font>`. Runtime operating-system font discovery will be removed. A font subset is preferred if it covers all registered player-facing characters; the complete font is acceptable for the first correct build if subsetting would delay validation, with build-size impact recorded.

### Capture through Chrome DevTools Protocol

The acceptance tool will launch a dedicated headless Chrome profile with a debugging port, wait for the Unity canvas and configured state delay, and call `Page.captureScreenshot` directly. It will terminate only the browser process it launched. This avoids both the unavailable in-app browser runtime and the incorrect assumption that an animated WebGL page becomes idle.

State transitions will be driven through browser input events at stable portrait control coordinates or a development-only runtime hook excluded from release behavior. The tool will record URL, viewport, state, and screenshot paths, and will return non-zero for missing text/load/canvas/capture checks.

## Risks / Trade-offs

- [Bundled CJK fonts can materially increase WebGL download size] → Prefer a licensed subset covering the project's registered visible copy and report the final compressed delta.
- [IMGUI remains harder to maintain than retained-mode UI] → Centralize portrait regions and control rectangles so draw and hit-test paths share the same geometry.
- [Safe-area height differs between browser and iOS] → Test both full browser viewport and synthetic inset cases in layout validation.
- [Coordinate-driven browser interactions can drift] → Derive acceptance coordinates from stable named regions and fail clearly when expected state text is absent.
- [Headless WebGL rendering varies by GPU availability] → Enable a known software WebGL backend for capture and distinguish rendering initialization failures from visual assertion failures.

## Migration Plan

1. Add and verify the packaged font before changing layout so missing copy is independently resolved.
2. Introduce logical portrait region helpers and replace the legacy side transform.
3. Move secondary detail and guidance surfaces into contextual overlays while preserving interaction methods.
4. Add portrait geometry and existing simulation smoke checks.
5. Build WebGL, run live visual acceptance for every required state, and review the evidence.
6. Publish only after smoke, build, HTTP artifact checks, and visual acceptance pass.

Rollback consists of restoring the previous runtime presentation and WebGL build; no gameplay or persisted-data migration is required.

## Open Questions

- Select the final licensed CJK font and determine whether the first release uses a complete font or a project-specific subset after measuring compressed size.
- Decide whether acceptance state setup uses input-only automation or a development-only deterministic setup hook after testing coordinate stability.
