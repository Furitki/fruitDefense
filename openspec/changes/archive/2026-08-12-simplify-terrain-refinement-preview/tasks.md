## 1. Refined-only brush contract

- [x] 1.1 Expose read-only active base, landform, and directed edge preview sources from the layered terrain target
- [x] 1.2 Remove refinement mode state and controls from the laboratory session and make all landform-bearing validation and writes require exact refinement
- [x] 1.3 Update accepted laboratory setup content so ordinary sample pairs no longer demonstrate the removed bare-edge mode

## 2. Representative preset cards

- [x] 2.1 Draw pure preset cards from real base sprites and pair cards from real base, active contour, and directed refined-edge sprites
- [x] 2.2 Keep unavailable pair cards identifiable but disabled with the exact missing-direction or missing-contour reason

## 3. Responsive Scene cell indicator

- [x] 3.1 Add explicit cached hover-cell state and targeted Scene repaint when the resolved cell changes
- [x] 3.2 Enable mouse-move events only while painting and restore previous Scene settings during every teardown path
- [x] 3.3 Clear the cell indicator at panel entry, window exit, target/tool change, stop, close, play-mode, and reload boundaries

## 4. Acceptance

- [x] 4.1 Extend focused editor smoke for refined-only presets, preview sources, missing-direction refusal, hover state, and lifecycle restoration
- [x] 4.2 Capture and inspect Unity editor evidence showing composed preset cards with no edge-mode controls
- [x] 4.3 Run Unity compilation, focused terrain-painter smoke, aggregate project smoke, and strict OpenSpec validation with no new errors
- [x] 4.4 Review the scoped diff, remove temporary acceptance helpers, and record final evidence without modifying runtime gameplay or product-direction documents
