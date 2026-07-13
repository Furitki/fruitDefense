## Why

The current battlefield scatters pots at arbitrary coordinates and gives limited feedback while plants are dragged, so placement and merging do not read as deliberate, tactile actions. The board should use coherent orthogonal planting blocks, remove the unwanted selling escape hatch, and make every attacking fruit useful at a larger scale.

## What Changes

- Replace scattered pot placement with regular orthogonal planting cells grouped into large soil regions around the road and orchard core.
- Add a pointer-following plant drag preview, source lift state, hovered-target emphasis, valid/invalid drop feedback, cancel return feedback, and successful placement feedback.
- Add explicit two-of-a-kind merge preview and completion feedback, including next-star messaging and full-star rejection.
- **BREAKING** Remove plant selling from the nursery, plant details, drag targets, commands, and economy rules.
- Double the base attack range of every attacking fruit while keeping sunflower range at zero.
- Keep range visualization, targeting, expansion adjacency, mouse input, and touch/pointer input consistent with the new grid and range values.

## Capabilities

### New Capabilities

- `orchard-grid-layout`: Defines the orthogonal battlefield grid, contiguous planting regions, reserved road/core cells, and grid-aligned pot rendering.

### Modified Capabilities

- `plant-manipulation`: Adds continuous drag and merge feedback and removes plant selling as an allowed action.
- `plant-combat`: Doubles attacking-fruit range and requires the displayed range to remain identical to runtime targeting range.
- `pot-expansion`: Changes placement and adjacency to operate on the same orthogonal grid used by initial pots.
- `nursery-economy`: Removes selling from the list of renewable sun sources and refresh guidance.
- `weapon-modifiers`: Replaces sale-based weapon recovery with merge-source recovery only.

## Impact

- Battlefield rendering and styles, plant drag state, nursery/economy controls, plant detail presentation, pot configuration and expansion logic.
- Game command and economy types lose the plant-selling action and event.
- Combat configuration changes for pea, watermelon, banana, and durian.
- Existing unit and component tests must be updated; new interaction and grid invariants require coverage.
