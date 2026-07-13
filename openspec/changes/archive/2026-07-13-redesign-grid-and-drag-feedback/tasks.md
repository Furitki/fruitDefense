## 1. Grid Model and Planting Regions

- [x] 1.1 Add canonical grid coordinates and shared grid-to-battlefield position helpers for pots and expansion candidates
- [x] 1.2 Replace scattered initial pot coordinates with regular contiguous planting blocks that reserve road, gates, and core space
- [x] 1.3 Render joined soil-cell regions and grid-aligned active/expandable pot states in the battlefield
- [x] 1.4 Update expansion adjacency to use grid row/column Manhattan distance and cover grid invariants with tests

## 2. Drag and Merge Feedback

- [x] 2.1 Introduce a shared transient drag session for nursery and field plants across native and pointer input
- [x] 2.2 Render the lifted source and pointer-following fruit preview with live attack-range preview at the hovered legal destination
- [x] 2.3 Add hovered placement/move/invalid presentation with outcome-specific labels and cancel/return feedback
- [x] 2.4 Add next-star merge preview, invalid merge reasons, and successful placement/merge animations and feedback

## 3. Remove Selling

- [x] 3.1 Remove sell zones and sell actions from the economy dock, plant details, and pointer drop routing
- [x] 3.2 Remove sell commands, events, values, reducer behavior, copy, and obsolete styles
- [x] 3.3 Update tests to prove plants cannot be sold and nursery blocking copy only offers placement or merging

## 4. Combat Range

- [x] 4.1 Double pea, watermelon, banana, and durian base ranges while leaving sunflower range at zero
- [x] 4.2 Add tests that runtime targeting statistics and visual range data use the doubled values

## 5. Verification

- [x] 5.1 Run the full test suite, typecheck, and production build
- [x] 5.2 Verify grid layout, mouse/pointer drag, cancel, placement, merging, no-sale UI, and doubled range in the running page
