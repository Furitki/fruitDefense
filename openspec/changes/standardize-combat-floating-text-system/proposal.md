## Why

The current combat floating text is functionally bounded but visually under-specified: every record is rendered through one 11-pixel label treatment, warm colors collide with the green-and-brown battlefield, linear movement lacks a readable contact beat, and 2x speed halves real reading time. Dense or future mowing-style combat would also create avoidable record and string churn even when visible feedback is capped.

## What Changes

- Establish one durable floating-text language for typography, semantic color, outline, entry/hold/exit rhythm, rebound strength, density fallback, and 1x/2x readability.
- Separate event/merge timing from local readable animation timing so pause still freezes feedback while 2x cannot compress text below its real-time reading floor.
- Replace per-frame text construction and recurring floating-record allocation with cached display content and reusable bounded records.
- Limit simultaneously readable ordinary damage text independently from higher-priority resource, control, heavy-impact, and defeat feedback; merge and evict by semantic priority before drawing.
- Validate the final raster against both grass and route surfaces at 402-by-874 in ordinary and dense 1x/2x WebGL battles.
- Keep the packaged Noto Sans SC font path. Do not add a project-authored bitmap glyph atlas in this change: the visible-count budget makes cached font rendering the simpler first baseline, while a future atlas renderer is authorized by either verified performance pressure or a final-raster quality failure such as discontinuous outline scaling or unreliable release glyph coverage.
- Keep gameplay, combat outcomes, snapshots, checksums, RNG, hit targets, and platform-adapter authorization unchanged.

## Capabilities

### New Capabilities

- `combat-floating-text-language`: Defines the semantic typography, color, motion, density, clock, allocation, and final-raster acceptance contract for combat floating text.

### Modified Capabilities

None.

## Impact

- Presentation contracts and routing under `Assets/Scripts/Presentation/`.
- Floating-text drawing and style setup in `Assets/Scripts/FruitDefenseGame.cs`.
- Aggregate Editor smoke coverage and real portrait WebGL evidence.
- Stable combat-feedback principles in `docs/design/game-design-overview.md`.
- No new package, font, sprite-atlas, gameplay, snapshot, or platform dependency in this baseline change; later renderer migration remains governed by the explicit quality/performance gate.
