## 1. Registered Resource Model

- [x] 1.1 Extend terrain brush definitions and import/setup code with a validated complemented TileSet view that guarantees two paint directions per resource.
- [x] 1.2 Register the preserved original square grass/soil laboratory family as a normal brush definition without replacing production Palette authority.
- [x] 1.3 Add stable directional paint-choice enumeration, availability, matching, and direct application helpers.

## 2. Terrain Laboratory Workflow

- [x] 2.1 Replace the resource gallery plus generic A/B selector with one gallery containing two directly paintable direction tiles per registered resource.
- [x] 2.2 Make one tile click configure or reuse the resource, select the exact direction, disable pure-only mode, and enter Scene painting while retaining non-empty-canvas safety.
- [x] 2.3 Keep advanced erase/clear behavior and active-brush feedback coherent after the primary selector removal.
- [x] 2.4 Allow every directional card to switch the active resource on non-empty laboratory canvases while preserving and rebuilding existing logical cells.
- [x] 2.5 Keep the native Overlay displayed and expanded throughout an active laboratory session, including after brush activation and Scene focus changes.
- [x] 2.6 Separate structural authoring readiness from strict content validation so recoverable partial edge data cannot lock target or brush selection.
- [x] 2.7 Compact the directional brush gallery to four cards per row with smaller centered artwork and readable wrapped labels.

## 3. Validation

- [x] 3.1 Update registry and painter smokes for original-resource retention, complemented fallback mapping, stable two-choice expansion, direct activation, direction switching, and duplicate-selector removal.
- [x] 3.2 Run focused registry and terrain painter validation.
- [x] 3.3 Run `FruitDefense.Editor.ProjectSetup.SmokeValidate` to completion after aligning the assertion with the active Runtime64 contract.
- [x] 3.4 Capture and inspect live Unity Scene Overlay evidence showing the unified gallery and preserved original terrain choices.
- [x] 3.5 Add regression coverage for non-empty cross-resource switching, recoverable authoring validation, and automatic Overlay expansion, then rerun focused validation and live evidence.
- [x] 3.6 Cover the four-column compact layout and inspect refreshed live Unity Scene Overlay evidence.
