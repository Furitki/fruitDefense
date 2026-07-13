## 1. Battlefield Geometry

- [x] 1.1 Derive and render the square orchard destination from the final path point and remove the separate exit marker
- [x] 1.2 Render soil cells and planting targets as strict squares while preserving grid positions and expansion behavior
- [x] 1.3 Add endpoint and square-grid regression tests

## 2. Responsive Shell

- [x] 2.1 Add dynamic-height board sizing and a compact short-landscape two-column layout
- [x] 2.2 Keep portrait and desktop layouts free of horizontal overflow and retain accessible primary controls

## 3. Weapon Dragging

- [x] 3.1 Add typed weapon drag session state and pointer lifecycle handlers to inventory cards
- [x] 3.2 Resolve hovered plants during weapon drags and install only on legal pointer drops
- [x] 3.3 Add weapon ghost, source lift, and valid/invalid target feedback while preserving native drag and click installation
- [x] 3.4 Add mouse-equivalent pointer and touch-style weapon drag component tests

## 4. Plant Drop Visibility

- [x] 4.1 Offset, clamp, and simplify the plant drag ghost so the pointer and target remain visible
- [x] 4.2 Strengthen destination-owned feedback for valid, merge, and invalid plant drops

## 5. Verification

- [x] 5.1 Run the full test suite, typecheck, production build, and OpenSpec validation
- [x] 5.2 Verify map semantics, plant drag, weapon drag, and layout at 1280x720, 390x844, and 844x390 in the running page
