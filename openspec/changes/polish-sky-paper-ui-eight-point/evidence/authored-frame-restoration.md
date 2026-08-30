# Authored-frame restoration evidence

> **Partially superseded.** This candidate correctly restored the nursery
> carrier surfaces, but it also restored the former `slot.nursery` solid rim
> and dashed rail. The later line-free nursery-carrier correction keeps the
> surfaces while replacing only that rejected raster anatomy.

## Result

The rejected surface-suppression candidate was removed. Battle now draws the
reviewed UI frames exactly once:

- Header Sun/Core/Wave each draw one `surface.metric` capsule.
- `NurseryTray` draws one `surface.panel-standard`.
- Each empty or occupied nursery cell draws one `slot.nursery`, including the
  intended light rim and orange dashed rail.

The slot renderer no longer exposes a `drawSurface` suppression branch.
Marker-free contained click/selection motion remains, and the shared
single-draw nine-slice shader that removed the confirmed PC black seam is
unchanged.

## Validation

- Strict OpenSpec validation: pass.
- `CompactControlAcceptanceSmoke.Run`: pass,
  `COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK`.
- `BattleUiLayoutSmoke.Run`: pass,
  `FRUIT_DEFENSE_BATTLE_UI_LAYOUT_OK`.
- `RuntimeUiVisualSystemSmoke.Run`: pass,
  `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`.
- Windows x64 player build: pass, `Build Finished, Result: Success`.
- Ordinary WebGL release build: pass, `FRUIT_DEFENSE_WEB_BUILD_OK`.

The full workspace `git diff --check` remains blocked by pre-existing trailing
whitespace in unrelated serialized YAML assets. No such warning belongs to the
files changed by this restoration.

## Implementation identities

| Artifact | SHA-256 |
| --- | --- |
| `Assets/Scripts/FruitDefenseGame.BattlefieldRendering.cs` | `4388C54E5EB3F5515AC2E1CA3C7A0FFF0E117BB813C85F54E7AC127F505D4F8C` |
| `Assets/Scripts/FruitDefenseGame.ControlsAndOverlays.cs` | `D889462312F6FC4711C0F88B0532D18EFB9373B6F534B39EB48B032BAE077D38` |
| `Assets/Scripts/UI/RuntimeUiGui.Art.cs` | `50FA313ABA710E5AA2465C0A21423D6F117116A015989A270383DA8B74394551` |
| `Assets/UI/RuntimeUiNineSlice.shader` | `21E9B3E2AC0DDE56AB6886EA22EAFC7FF4820D1EAF3A25E7F00E0B1D77BBC41F` |
| Windows `Assembly-CSharp.dll` | `3485B04D488A54BA38BA23774BBE6EAA0B8724968EA611EF506E5A7804FAE162` |
| WebGL `index.html` | `6FD6F70C72E71A5582AD0AB38DBC3BA5E95C825564D700B22E849C0C1AB5DCF8` |

## Real canvas evidence

| State | Capture | SHA-256 |
| --- | --- | --- |
| Ordinary WebGL Battle ready | `frame-restoration-webgl-ready.png` | `406E54B6F01497C50F9FEA2A9997E9844F56050EB861C326C1EAB7E81A23D6C0` |
| Immediately after clicking the first empty nursery cell | `frame-restoration-webgl-nursery-click.png` | `5247E5584AA804542FEEC928438B345FD5212CEFA4B8232843B04E6080BEDDA2` |

Both captures show the authored Header metric capsules, nursery-section frame,
and five nursery slot frames. The clicked-state capture keeps the base frame and
does not add a selected marker, primitive four-edge outline, duplicate surface,
or visible black internal seam.

PC-specific visual confirmation remains part of the open multi-resolution Gate
A; this correction does not relabel WebGL evidence as PC evidence. The rebuilt
PC player consumes the same restored Battle calls and the unchanged seam-safe
single-draw renderer that the user already confirmed removed the black seam.
