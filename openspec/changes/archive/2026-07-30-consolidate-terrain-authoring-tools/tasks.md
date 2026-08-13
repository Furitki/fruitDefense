## 1. Native Scene Overlay host

- [x] 1.1 Add one instance native Scene `IMGUIOverlay` that reuses the existing terrain paint session and ordinary panel content
- [x] 1.2 Route menu, Inspector, close/hide, play-mode, assembly-reload, and repeated-open lifecycles through one idempotent Overlay teardown path
- [x] 1.3 Remove the hand-positioned Scene GUI panel geometry, custom collapse/header state, and obsolete reserved-area plumbing after native input ownership is covered

## 2. Resource-acceptance presentation

- [x] 2.1 Rename user-facing laboratory copy to terrain-resource acceptance, show the configured contour read-only, and route playable-map work to the canonical editor
- [x] 2.2 Preserve the two directed composition cards, real previews, contextual `只绘制纯图` checkbox, advanced erasure, and Undo behavior while one edge TileSet serves both directions through complemented reverse masks
- [x] 2.3 Keep exact reverse bindings as compatibility overrides, switch active acceptance configuration to the current canonical edge family, and record non-destructive resource cleanup candidates
- [x] 2.4 Update concise README and active workflow wording so `Window`, map authoring, resource acceptance, and shared edge reuse are not conflated
- [x] 2.5 Preserve the B-on-A full interior by rendering the shared mask-00 endpoint after complementation, while still rejecting an empty source mask before complementation
- [x] 2.6 Make the shared same-contour edge and mask-00 endpoint contract mandatory for every future registered directed pair brush

## 3. Validation and evidence

- [x] 3.1 Extend focused editor smoke coverage for native Overlay identity/activation, two-card stability, pure-only behavior, shared reverse masks, exact-override compatibility, configured-contour labeling, lifecycle teardown, and no duplicate session
- [x] 3.2 Compile and run focused terrain acceptance plus `FruitDefense.Editor.ProjectSetup.SmokeValidate`, retaining the successful log
- [ ] 3.3 Inspect the native Overlay in a real Scene view and retain valid visual evidence
- [x] 3.4 Run strict validation for this change and all OpenSpec changes, record evidence and remaining limitations, and verify runtime terrain parity because edge resolution is no longer editor-only
