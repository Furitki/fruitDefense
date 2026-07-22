## 1. Battle Surface Geometry

- [x] 1.1 Define the enlarged map region, shared battle surface, embedded tool tray, embedded nursery tray, refresh action, and contextual detail rectangles
- [x] 1.2 Repoint tool, nursery, refresh, click, drag-source, and drop-target geometry to the embedded rectangle helpers
- [x] 1.3 Extend portrait layout validation for containment, ordering, non-overlap, enlarged projection, and 44-point controls

## 2. Embedded Presentation

- [x] 2.1 Replace the standalone build panel with tool and nursery controls drawn inside the battle surface
- [x] 2.2 Remove the default guidance block and standalone operation-status panel from the draw sequence
- [x] 2.3 Render selected-plant information only as a compact contextual card inside the battle surface

## 3. Runtime and Visual Acceptance

- [x] 3.1 Run Unity 6000.3.19f1 compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`
- [x] 3.2 Rebuild the WebGL player and update acceptance coordinates or state hooks for the embedded layout
- [x] 3.3 Capture and review the live 402-by-874 WebGL initial and interaction states for enlarged map, embedded trays, touch alignment, and removed bottom copy
- [x] 3.4 Run strict OpenSpec validation and review the scoped diff

## 4. Frameless Entity Icons

- [x] 4.1 Remove persistent opaque backplates from map flowerpots/fruits, occupied nursery fruits, nursery pot rewards, and the flowerpot tool
- [x] 4.2 Enlarge map and tray flowerpot/fruit art to nearly fill each logical cell while preserving existing hit and drag bounds
- [x] 4.3 Preserve selection, target, return, and drag feedback with transient outlines and add geometry assertions for the new icon sizing

## 5. Follow-up Validation

- [x] 5.1 Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`
- [x] 5.2 Rebuild WebGL and review the 402-by-874 evidence through the project external-headless acceptance flow
- [x] 5.3 Run strict OpenSpec validation and scoped diff checks

## 6. Configured Plant Height

- [x] 6.1 Add a finite non-negative flowerpot visual-height offset to each plant content definition with exact zero/unit semantics
- [x] 6.2 Configure subtle independent offsets for pea, watermelon, banana, durian, and sunflower in bundled content
- [x] 6.3 Apply the resolved content value generically to map plant art without plant-kind-specific height code or nursery/drag-ghost movement
- [x] 6.4 Rebuild and validate the authored battle-content asset and canonical bundled JSON

## 7. Plant Height Validation

- [x] 7.1 Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`
- [x] 7.2 Rebuild WebGL and review map plant-over-pot placement through the external-headless 402-by-874 evidence flow
- [x] 7.3 Run strict OpenSpec validation and scoped diff checks
