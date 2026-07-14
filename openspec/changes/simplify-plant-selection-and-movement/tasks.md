## 1. Interaction-State Separation

- [x] 1.1 Confirm `restructure-battlefield-map-and-tiles` is applied and exposes the shared battlefield position/range projection required by this change
- [x] 1.2 Add regression coverage for drag placement, movement, return-to-nursery, merge, invalid-drop return, and explicit weapon-tool behavior before changing clicks
- [x] 1.3 Replace pending plant selection with an inspection-only plant identity that is separate from explicit tool modes and drag sessions
- [x] 1.4 Make below-threshold plant pointer release inspect the clicked field or nursery plant and open its information surface

## 2. Remove Click Relocation

- [x] 2.1 Remove inspected-plant movement/placement from empty flowerpot clicks and show non-destructive drag guidance instead
- [x] 2.2 Make occupied plant clicks switch inspection rather than moving or merging the previously inspected plant
- [x] 2.3 Remove inspected-plant return behavior from nursery-slot clicks while preserving nursery drag targets
- [x] 2.4 Verify every placement, move, return, and merge simulation command is reachable through drag-and-drop and not through passive inspection clicks

## 3. Range and Information Presentation

- [x] 3.1 Render the inspected on-board attack range beneath entities from effective plant range and the shared battlefield projection
- [x] 3.2 Show information without a range overlay for nursery plants and zero-range support plants
- [x] 3.3 Update inspection after a successful drag and clear it after close, refresh removal, return to nursery, plant removal, or restart
- [x] 3.4 Update status/help copy so players are told to drag for placement, movement, return, and merging

## 4. Validation and WebGL Evidence

- [x] 4.1 Extend editor/runtime validation for inspection-only clicks, no-op destination clicks, projected in-range/out-of-range points, and retained drag actions
- [x] 4.2 Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`
- [x] 4.3 Build WebGL and capture a sequence showing plant click information/range, no movement after a destination click, and movement only after a drag
