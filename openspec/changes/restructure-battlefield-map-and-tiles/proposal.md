## Why

The battlefield route, planting cells, flowerpot positions, and visual sizes are currently tied together through hard-coded normalized coordinates and unrelated scale constants. Doubling each flowerpot's width and height would overlap the existing grid, so the battlefield needs a stable map and projection framework before it can grow safely.

## What Changes

- Introduce an explicit battlefield definition for the planting grid, route nodes, entry, exit, core, and semantic initial-pot regions.
- Separate map topology and route sampling from the portrait-screen projection used for drawing and hit testing.
- Replace duplicated stored flowerpot points and presentation magic numbers with positions derived from canonical cells and one shared battlefield projection.
- Enlarge the portrait gameplay region and make the battlefield nearly full width.
- Increase each on-board flowerpot's rendered and interactive width and height to 200% of the current values, producing four times the area without overlap.
- Preserve the current 8-by-6 set of 48 planting cells, initial-pot count, combat ranges, wave balance, and expansion rules while migrating them to the new framework.
- Extend editor smoke and real WebGL visual acceptance to validate route continuity, cell uniqueness, enlarged targets, and board containment.

## Capabilities

### New Capabilities

- `battlefield-map-layout`: Canonical battlefield topology, route sampling, screen projection, enlarged portrait board, and doubled flowerpot/tile presentation.

### Modified Capabilities

None. The repository has no promoted baseline specifications for this behavior.

## Impact

- Core map data and sampling in `Assets/Scripts/Core/GameConfig.cs`, `GameModel.cs`, and `GameSimulation.cs`.
- Battlefield drawing, hit testing, drag targets, entities, and effects in `Assets/Scripts/FruitDefenseGame.cs`.
- Geometry coverage in `Assets/Editor/ProjectSetup.cs` and WebGL canvas acceptance coordinates/evidence.
- No save-data migration is required because the current runtime has no persisted map state. Combat balance and wave content are not redesigned.
