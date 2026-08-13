# dual-grid-art-evidence Specification

## Purpose
TBD - created by archiving change consolidate-dual-grid-stress-atlas. Update Purpose after archive.
## Requirements
### Requirement: Single compact native-tile stress atlas
The Dual-Grid art pipeline SHALL emit exactly one fully opaque 1024×1024 stress PNG per finalized candidate, SHALL derive each scenario from a 17×17 logical vertex field that places 16×16 Runtime tiles, and SHALL place the four 512×512 scenarios into fixed quadrants without scaling the native 32×32 Runtime tiles or adding labels, gutters, overlays, filtering, or other pixel modification.

#### Scenario: Finalize a candidate
- **WHEN** a candidate with sixteen valid Runtime32 masks is finalized
- **THEN** the pipeline writes one stress atlas whose top-left, top-right, bottom-left, and bottom-right quadrants contain proportional 16×16 forms of `pureLandform`, `landformWithCentralBaseHole`, `baseWithCentralLandformIsland`, and `diagonalMixed` respectively
- **AND** the atlas preserves all sixteen mask values across the four scenarios
- **AND** the pipeline does not emit separate per-scenario stress PNGs

### Requirement: Decodable stress manifest
The candidate manifest SHALL identify the stress atlas path, dimensions, panel size, tile-grid size, logical-vertex size, native Runtime tile size, fixed layout version, and every scenario's identifier, top-left pixel rectangle, formula, decoded RGBA hash, and opacity result.

#### Scenario: Read stress evidence without filename inference
- **WHEN** a validator or reviewer reads a finalized candidate manifest
- **THEN** it can locate and decode all four scenarios from the one atlas using only the declared stress-atlas metadata

### Requirement: Independent whole-image and panel reconstruction
Validation SHALL rebuild every stress scenario from the finalized Runtime32 masks, assemble the expected atlas, compare the complete decoded RGBA buffer, and independently crop and compare every declared panel.

#### Scenario: A quadrant is altered or moved
- **WHEN** any scenario pixels, rectangle, identifier, order, or atlas pixels no longer match the deterministic contract
- **THEN** validation fails and reports the atlas or affected scenario rather than accepting file existence or dimensions alone

### Requirement: Exhaustive adjacency remains separate evidence
The pipeline SHALL continue to enumerate all 64 legal horizontal and 64 legal vertical mask pairs for Review256 and Runtime32, SHALL record actual pair and pixel mismatch counts, and SHALL NOT claim seam safety solely because the single stress atlas validates.

#### Scenario: Stress atlas passes while a legal socket mismatches
- **WHEN** the stress atlas reconstructs exactly but an enumerated legal adjacency has a pixel mismatch
- **THEN** the manifest retains the nonzero mismatch and `seamSafetyClaimed=false` for human disposition

