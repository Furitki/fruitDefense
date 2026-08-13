## Why

Map plants currently animate only during the short basic-attack action window and become completely static as soon as that window ends. This makes healthy plants look frozen between attacks even though their cooldown and targeting logic continue to run.

## What Changes

- Add a subtle, continuous idle motion for planted fruits while the battle presentation is running.
- Layer the existing per-plant attack motion over the idle pose so repeated attacks remain readable.
- Add regression coverage proving that one plant can begin a second basic attack after its cooldown.
- Keep gameplay damage, cooldown, targeting, persistence, and content data unchanged.
- Validate the result through the existing Unity P0 checks and the real WebGL portrait acceptance surface.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `embedded-battle-control-surface`: Idle planted fruits remain visibly alive between combat actions while preserving their existing interaction bounds and transparent atlas presentation.

## Impact

- Runtime presentation: `Assets/Scripts/FruitDefenseGame.cs`.
- Editor regression coverage under `Assets/Editor/Tests/`.
- Ordinary WebGL output and portrait acceptance evidence.
- No new dependency, gameplay API, save-format, content-catalog, or platform-adapter impact.
