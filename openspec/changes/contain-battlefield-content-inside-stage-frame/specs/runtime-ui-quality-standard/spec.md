## ADDED Requirements

### Requirement: Battlefield stage pixel containment
Battlefield-owned terrain, cells, entities, health bars, projectiles, combat effects, flashes, attack-range feedback, board target frames, board target cues, and board-target drag ghosts SHALL be rendered through one stage-viewport containment contract using the existing authoritative Battle draw/hit projection. The gameplay-stage frame SHALL remain above those pixels in the final composition, battlefield pixels SHALL meet its visible inner opening without an artificial empty band, and containment SHALL NOT move or shrink gameplay hit rectangles.

#### Scenario: Battlefield content reaches a stage edge
- **WHEN** any battlefield-owned visual reaches or exceeds an edge because of its sprite extent, plant height, rotation, reaction offset/scale, combat effect radius, battlefield shake, or drag preview extent
- **THEN** no pixel escapes the `BattleStage` viewport and the protected gameplay-stage rail remains visible above that content without changing the entity or target's logical position

#### Scenario: Battlefield content meets the frame opening
- **WHEN** terrain or edge feedback is composited beneath the gameplay-stage frame
- **THEN** the visible battlefield reaches the frame's inner opening on every side without using the ArtSet interaction `safeInset` as an additional presentation gap, and any aspect-ratio gutter outside the square-tile `GridRect` is filled by the same base terrain presentation without stretching the grid

#### Scenario: Board target feedback is drawn during a plant drag
- **WHEN** the resolved plant drag target is a board pot, plant, or expansion cell
- **THEN** the target frame, target cue, and plant ghost obey the same stage containment and rail-occlusion contract while the existing authoritative target rectangle and release legality remain unchanged

#### Scenario: Plant drag crosses between board and nursery
- **WHEN** an active plant drag connects a board source and a nursery destination or a nursery source and a board destination
- **THEN** the connector remains a finite design-space overlay while battlefield-owned ghost and target pixels cannot overwrite or escape the gameplay-stage frame

#### Scenario: Fractional viewport scaling is active
- **WHEN** Battle is rendered at a supported inset portrait geometry or the 1280×720 letterboxed PC matrix
- **THEN** the stage clip, content, frame, and hit projection use one finite transform without device-pixel gaps, offset reapplication, or unprotected rail pixels
