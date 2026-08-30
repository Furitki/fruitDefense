## ADDED Requirements

### Requirement: Battlefield protected-rail WebGL gate
Dedicated ordinary-WebGL visual acceptance SHALL verify battlefield stage containment from final composited canvas pixels rather than inferring it only from logical rectangles or ArtSet metadata.

#### Scenario: Edge-heavy Battle states are captured
- **WHEN** ready/active and board-target drag states exercise edge cells, representative transient motion, and the largest required battlefield visuals at 402×874 full/inset and 1280×720 letterboxed PC projection
- **THEN** captured evidence shows no battlefield-owned pixels outside the stage, no contamination of the visible gameplay-stage rail, base-terrain coverage in the top and bottom aspect-ratio gutters, no empty seam between battlefield pixels and the frame opening, and a visible correctly projected connector for the explicit cross-region drag exception

#### Scenario: Protected rail is contaminated
- **WHEN** final-pixel comparison detects entity, effect, target-frame, cue, or drag-ghost pixels in the protected gameplay-stage rail or outside the stage viewport beyond the approved connector mask
- **THEN** visual acceptance fails even if draw/hit rectangle validation, ArtSet validation, and the ordinary WebGL build otherwise pass
