## Why

The current battlefield places the orchard core in the middle even though zombies leave through a separate road exit, and its rounded planting tiles do not match the requested square-grid presentation. Weapon dragging also relies on desktop-only native drag events, while the existing drag preview obscures the destination and the layout becomes unusably tall on short landscape screens.

## What Changes

- Render planting soil cells as strict squares and move the orchard core to a square destination at the road endpoint, removing the separate exit marker.
- Add height-aware responsive layouts for desktop, portrait mobile, and short landscape viewports.
- Give weapons the same pointer/touch drag lifecycle as plants while preserving native drag and click-to-install fallbacks.
- Offset and simplify drag previews so they do not cover the pointer, and make the hovered destination itself the primary placement indicator.
- Add automated interaction and layout coverage plus browser acceptance checks at 1280x720, 390x844, and 844x390.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `orchard-grid-layout`: Requires square planting cells and a square orchard destination aligned with the end of the zombie path.
- `web-game-shell`: Requires a usable height-aware layout in short landscape as well as desktop and portrait viewports.
- `weapon-modifiers`: Requires weapon drag installation to work with mouse, touch, and pen pointer input with live target feedback.
- `plant-manipulation`: Requires the drag preview to remain offset from the pointer and keep the destination visible.

## Impact

- Battlefield configuration, rendering, soil/core styles, and path presentation.
- App-level drag session state, weapon inventory controls, battlefield drop routing, and equipment presentation.
- Responsive shell, battlefield, and equipment CSS.
- Component and state tests for pointer dragging, target resolution, square layout invariants, and endpoint semantics.
