## MODIFIED Requirements

### Requirement: Focused drag hint presentation
An active plant drag SHALL show a stable dashed connector from the authoritative source location to the current drag preview or selected legal destination, SHALL show a target-sized semantic frame and existing state icon for the resolved target, and SHALL continue to show a compact floating text hint only for a legal plant merge.

#### Scenario: Plant drag has no resolved target
- **WHEN** a plant crosses the drag activation threshold without overlapping a drop target
- **THEN** a dashed connector runs from the original board or nursery source to the current clamped drag preview and no target frame is shown

#### Scenario: Plant can be planted, moved, returned, or cannot be dropped
- **WHEN** a dragged plant overlaps a non-merge target
- **THEN** a legal empty flowerpot or nursery destination receives the snapped connector endpoint, legal selection frame, and legal icon, while an illegal destination receives the rejection frame and prohibition icon without a legal snap, and no floating text hint is shown

#### Scenario: Plant can be merged
- **WHEN** a dragged plant overlaps a compatible same-kind, same-star plant and the merge is legal
- **THEN** the connector endpoint and merge frame visually snap to the authoritative target rectangle and a compact floating hint identifies the resulting star level

#### Scenario: Plant can be swapped
- **WHEN** a dragged plant overlaps an occupied destination whose legal action is swap
- **THEN** the connector endpoint and swap frame visually snap to the authoritative target rectangle and the swap icon distinguishes it from movement and merge

#### Scenario: Plant overlaps an illegal destination
- **WHEN** a dragged plant overlaps a destination rejected by simulation legality
- **THEN** the connector continues to the current drag preview, the destination shows a danger rejection frame and prohibition icon, and no legal snap or floating text hint is shown

#### Scenario: Drag feedback ends
- **WHEN** the drag is released, cancelled, blocked by pause or terminal state, or cleared by restart
- **THEN** the connector and target frame disappear with the existing drag session and no residual overlay remains

## ADDED Requirements

### Requirement: Plant drag feedback preserves authoritative geometry
Plant drag feedback SHALL use the same source, target, viewport, and safe-area geometry as drawing and hit testing and SHALL NOT alter target discovery, target bounds, legality, or release behavior.

#### Scenario: Legal target feedback is projected
- **WHEN** a board or nursery target is highlighted at any supported portrait safe area
- **THEN** the feedback frame equals the existing authoritative target rectangle and the connector endpoint aligns with that rectangle without creating a second projection

#### Scenario: Feedback is visible near a target
- **WHEN** the drag preview overlaps or leaves a target
- **THEN** target selection remains determined by the existing preview-overlap rule and simulation legality rather than by visual connector or frame proximity
