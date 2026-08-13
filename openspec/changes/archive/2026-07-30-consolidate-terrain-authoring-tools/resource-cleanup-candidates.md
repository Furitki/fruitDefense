# Terrain Resource Cleanup Candidates

This inventory is non-destructive. No file in this list is deleted by the current change.

## Keep: current acceptance resources

| Resource | Role | Decision |
|---|---|---|
| `Assets/LayeredTerrain/GrassSoil/Base/` | Grass and soil opaque bases | Keep |
| `Assets/LayeredTerrain/GrassSoil/Authoring/` | Logical markers and base Tile assets | Keep |
| `Assets/LayeredTerrain/GrassSoil/Square/LandformGrass/` | Current square grass contour | Keep |
| `Assets/LayeredTerrain/GrassSoil/Square/LandformSoil/` | Current square soil contour | Keep |
| `Assets/LayeredTerrain/GrassSoil/Square/EdgeGrassOnSoilPainted/` | Current shared square edge; direct mask for grass-on-soil and complemented mask for soil-on-grass | Keep |
| `Assets/LayeredTerrain/GrassSoil/Square/LandformStoneRoad/` | Current square route contour used by battlefield palettes | Keep |

## First deletion candidate: superseded exact reverse edge

| Resource | Size snapshot | Why it is a candidate | Remaining blockers |
|---|---:|---|---|
| `Assets/LayeredTerrain/GrassSoil/EdgeSoilOnGrassRefined/` | 66 files, 104,098 bytes | The reverse brush can now reuse the same-contour grass-on-soil edge through complemented masks. New palette and acceptance setup no longer register this family. | Existing serialized references in `CombinedWorkflowTrialTerrainLab.unity` and `ProtectedHybridTrialPalette.asset`, plus the focused compatibility test, must be migrated or retired before deletion. |

## Conditional candidates if the organic contour is retired

These are not duplicates of the current square family. Delete them only after a separate decision to remove organic contour support.

| Resource | Size snapshot | Current dependency |
|---|---:|---|
| `Assets/LayeredTerrain/GrassSoil/LandformGrass/` | generated 32 px organic grass family | Organic contour compatibility and tests |
| `Assets/LayeredTerrain/GrassSoil/LandformSoil/` | generated 32 px organic soil family | Organic contour compatibility and tests |
| `Assets/LayeredTerrain/GrassSoil/EdgeGrassOnSoilRefined/` | 66 files, 106,543 bytes | Shared organic edge for both pair directions |

## Trial/debug candidates

| Resource | Size snapshot | Review note |
|---|---:|---|
| `Assets/LayeredTerrain/Trials/CombinedWorkflowProtected/` | 71 files, 185,795 bytes | Generated isolated trial output; verify the trial workflow is no longer needed before deleting. |
| `Assets/Scenes/CombinedWorkflowTrialTerrainLab.unity` | generated trial scene | Still serializes the legacy exact reverse edge. Regenerate or delete with the trial output. |

## Source and provenance candidates

| Resource | Size snapshot | Review note |
|---|---:|---|
| `Assets/LayeredTerrain/GrassSoil/Square/Sources/` | 22 files, 6,177,478 bytes | Contains accepted source/provenance plus rejected drafts. Archive rejected attempts separately before considering project deletion; the accepted ribbon and provenance are still used by packaging validation. |
| `Assets/LayeredTerrain/GrassSoil/Square/Topology/` | 8 files, 72,602 bytes | Generator/validator references; not a safe deletion candidate while deterministic repackaging remains supported. |
| `Assets/LayeredTerrain/GrassSoil/Sources/` | 2 files, 4,129 bytes | Legacy prompt record; documentation-only, so it can be archived after confirming no audit requirement. |

## Required check before any deletion

1. Search the candidate TileSet GUIDs across `.unity`, `.asset`, tests, generators, and documentation.
2. Regenerate or remove trial artifacts that still serialize a candidate.
3. Run focused terrain smoke, aggregate editor smoke, and a runtime terrain presentation check.
4. Delete the approved folder together with its `.meta` files in one reviewed change.
