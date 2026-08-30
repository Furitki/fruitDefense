## Context

Plant drag input, target discovery, target legality, drawing, and release already converge in the immediate-mode `FruitDefenseGame` presenter. `FindDropTargetAt` chooses authoritative pot or nursery rectangles with `DragGeometry.BestOverlapIndex`, while `GameSimulation` owns legal plant, move, return, merge, and swap decisions. The current ghost is partially interpolated toward every overlapped target and only small target/ghost badges communicate the resolved state.

The working runtime uses one `BattlefieldProjection` for board drawing and hit testing, one shared runtime UI theme/ArtSet, and ordinary WebGL as the portrait acceptance baseline. The new feedback must remain transient presentation geometry, allocate nothing per frame, preserve the finite ArtSet contract, and disappear with the existing drag lifecycle.

## Goals / Non-Goals

**Goals:**

- Keep a stable visible relationship between a dragged plant and its original board or nursery location.
- Make the exact authoritative drop owner legible through a full state frame and existing semantic icon.
- Snap only the feedback endpoint/frame for legal destinations while keeping the drag ghost under the pointer.
- Reuse existing target discovery, legality, viewport scaling, safe-area projection, semantic colors, and drag icons.
- Validate geometry/state behavior deterministically and from a real ordinary-WebGL portrait canvas.

**Non-Goals:**

- Mechanical magnetism, expanded hit rectangles, nearest-target selection, or release assistance.
- Changes to simulation legality, cooldown, merge, swap, inventory, persistence, balance, or scene flow.
- Connector feedback for equipment installation or flowerpot expansion in this first version.
- New ArtSet slots, generated raster assets, looping motion, haptics, or mini-game platform acceptance.

## Decisions

### Capture a separate authoritative source rectangle

`DragSession` will retain the pointer-down position for activation-threshold evaluation and separately capture the source rectangle returned by the existing `PlantSourceRect`. Board sources therefore remain projection-owned and nursery sources remain layout-owned. Capturing at pointer down produces a stable origin even if later presentation effects offset or pulse the source.

Alternative considered: reuse `DragSession.Start`. Rejected because it is the exact pointer-down coordinate rather than the visual source center and can make the connector originate from an arbitrary part of the plant.

### Keep target selection and snapping presentation-only

`FindDropTargetAt` and the simulation legality calls remain the sole target/legality authorities. When the current target is legal, connector geometry ends at that target rectangle and the full frame is drawn on the same rectangle. With no target or an illegal target, the connector ends at the clamped drag-preview rectangle and no legal snap occurs. Illegal targets may retain a danger rejection frame and prohibition icon but never use the legal snap endpoint.

Alternative considered: add a magnetic distance around targets. Rejected because it changes drop behavior and would introduce a second target-selection rule.

### Replace partial ghost interpolation with one clear feedback model

The existing `.42f` interpolation toward any overlapped target will be removed. The drag ghost stays aligned to the pointer-offset preview; the connector endpoint and target frame communicate the selected destination. This prevents illegal targets from looking partly accepted and eliminates competing pointer, ghost, and destination positions.

### Use allocation-free shared dynamic geometry

`DragGeometry` will resolve edge-trimmed connector endpoints and a finite dashed layout without allocating a list each repaint. It will also project that resolved geometry through the current design-to-device matrix before any rotation is applied. `RuntimeUiGui` will temporarily switch to the identity GUI matrix, rotate around the already projected device-space origin, and draw the projected dash rectangles. The presenter supplies rectangles and explicit state only; it will not hard-code color or create a private texture/rendering path.

The connector remains dynamic because its length and angle are runtime geometry. The target frame is not synthesized from four stretched rectangles: it reuses the approved transparent-center `surface.illustration-frame` nine-slice production binding, tinted by the resolved drag state, while the existing `indicator.drag-legal`, `indicator.drag-illegal`, `indicator.merge`, and `indicator.swap` assets remain the distinct non-color semantic cue.

### Project rotated connector geometry into device space

The previous first version called `GUIUtility.RotateAroundPivot` while the portrait design scale and letterbox translation were still installed in `GUI.matrix`. At the 402×874 reference viewport the outer transform is identity, masking the problem. A PC window commonly has a fractional scale plus a large horizontal or vertical offset; composing the pivot rotation with that outer matrix lets IMGUI interpret the pivot and dash rectangles in different transformed spaces, producing visible displacement and skew.

The connector will therefore capture the current axis-aligned GUI matrix, project its start, end, dash length, gap, and thickness into device coordinates, set `GUI.matrix` to identity, and rotate only the projected rectangles around the projected start. It restores the original matrix in `finally`. This leaves all other Battle UI in design space and prevents the portrait letterbox offset from participating in the rotation.

### Derive visibility from the existing drag lifecycle

Feedback appears only for an active plant drag. Release, Escape, pause/terminal cancellation, restart, or any existing drag reset removes it because no separate persistent feedback session is introduced. Reduced-motion requires no special branch because the first version is static and non-looping.

### Draw as the final drag-feedback overlay

The connector is drawn first, then the authoritative target frame/icon, then the drag ghost/icon and merge hint. The geometry stays in design-space coordinates under the existing safe-area GUI matrix. This keeps the connector behind the important endpoints while avoiding a second viewport transform.

## Risks / Trade-offs

- [Long nursery-to-board connectors can cross control surfaces] → Keep the line dashed and semantically tinted, trim it to source/destination bounds, and verify legibility without obscuring copy on the supported portrait sizes.
- [Rotated IMGUI dashes can alias or shift under host scaling] → Project once through the axis-aligned design matrix, rotate under the identity matrix, test a 1280×720 letterboxed PC viewport, and retain ordinary-WebGL full/inset captures.
- [A full frame can compete with the gameplay-stage rail] → Keep it transient, target-sized, and visually lighter than the unique permanent stage frame.
- [Current uncommitted UI work touches the same presenter files] → Patch only the drag slices, preserve surrounding changes, and rerun the aggregate smoke instead of restoring files wholesale.
