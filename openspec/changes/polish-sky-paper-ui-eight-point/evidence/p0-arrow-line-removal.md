# P0 annotated-line removal evidence

> **Rejected overcorrection.** The user confirmed that this candidate removed
> the black seam but also removed required authored UI frames. The captures and
> hashes below are retained only as negative evidence and are superseded by the
> authored-frame restoration evidence.

## Result

The user's annotated runtime screenshot supersedes the earlier keep-visible
conclusion in `pc-nine-slice-adaptation.md`. The three rejected Battle surface
families are now omitted at their owning calls:

- Header Sun/Core/Wave metrics retain icon, copy, value, and pulse but do not
  draw `surface.metric`.
- `NurseryTray` retains its title, five owners, Refresh action, layout, and hit
  geometry but does not draw `surface.panel-standard`.
- Empty and occupied nursery cells retain content, state indicator ownership,
  drag geometry, and contained selection motion but do not draw
  `slot.nursery`, including its authored outer rim and dashed inner rail.

The reviewed rasters, ArtSet bindings, source/runtime hashes, colors, and
importer metadata were not modified or replaced for this correction.

## Implementation identities

| Artifact | SHA-256 |
| --- | --- |
| `Assets/Scripts/FruitDefenseGame.BattlefieldRendering.cs` | `E6AF13A44B479A503F95A1B2BF019BDC7A3D520626F2CBEC7FE739BD0CB0B326` |
| `Assets/Scripts/FruitDefenseGame.ControlsAndOverlays.cs` | `F713F99616DC450D43076BBA68A93F064B91E06CA9605241F352A706F80C5EAA` |
| `Assets/Scripts/UI/RuntimeUiGui.Art.cs` | `03E931A377EB03DBC28E83C384E2348B39AE1AA089C5D9219D4EEAE070D9B23D` |

## Validation

- Strict OpenSpec validation: pass.
- `CompactControlAcceptanceSmoke.Run`: pass,
  `COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK`.
- `BattleUiLayoutSmoke.Run`: pass,
  `FRUIT_DEFENSE_BATTLE_UI_LAYOUT_OK`.
- Windows x64 incremental player build: pass,
  `Build Finished, Result: Success`; compiled
  `Assembly-CSharp.dll` SHA-256
  `4B2DB717642948D7693A5D41743C9FD81115ABA884BE03E36D73A55551E23292`.
- Ordinary WebGL release build: pass,
  `FRUIT_DEFENSE_WEB_BUILD_OK`; `index.html` SHA-256
  `0AC71D51B93BDD247E0C947BBF739D044D71AF5EFE37FB90422E12D2063AEF8D`.
  The build logged a Unity temporary websockify `EADDRINUSE` warning because an
  existing proxy already owned port `35020`; it still completed and emitted the
  success marker.

## Real canvas evidence

| State | Capture | SHA-256 |
| --- | --- | --- |
| New ordinary WebGL release, Battle ready at 402x874 | `p0-line-removal-webgl-ready.png` | `E9275CF78983C409AB8A92422944B7AA7C3A969CF7681DE16719D153572E5F90` |
| Same payload immediately after empty-nursery click | `p0-line-removal-webgl-nursery-pulse.png` | `ABC3C391F91475855166072A6EAAECE01252F62F9C5EEB357946272A6F833E7C` |

The new ready capture contains no individual Header metric capsules, nursery
section perimeter, or nursery-cell rim/dashed rail. The click capture does not
restore a selection marker or four-edge outline.

The rebuilt Windows D3D player was launched successfully, but the available
Windows Graphics Capture client rejected that Unity window twice with
`SetIsBorderRequired: 0x80004002`. Therefore this increment records the PC
build as successful but does not mislabel the WebGL screenshot as PC visual
evidence. The shared Battle call sites are identical across both builds; final
PC visual approval remains part of the open multi-resolution Gate A.
