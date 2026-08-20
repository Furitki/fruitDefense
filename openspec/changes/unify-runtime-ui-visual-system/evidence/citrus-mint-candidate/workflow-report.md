# Citrus Mint replacement-workflow proof

## Scope and final state

This run exercised the real production `citrus-mint@1` ArtSet through the
stable `Fruit Defense/UI/Visual System` workflow. It did not use a cloned test
fixture as the activation target and did not touch WebGL, Chrome, the manual
server on port 4173, candidate art/exporters, scenes, or runtime code.

- Unity: `6000.3.19f1`
- Release theme: `ui.sunny-orchard@1`, GUID
  `932a9783b6b3eee45970ab3ef84a4c39`
- Starting/final active set: `sunny-orchard@1`, ArtSet GUID
  `12cc0c638d174040bb0384d7bf17ea92`
- Replacement candidate: `citrus-mint@1`, ArtSet GUID
  `8c547f9fccb3673ef6115dcb820ebc1e`
- Final theme SHA-256 (identical to the pre-workflow byte snapshot):
  `375990DC5E2C670AAE5B34212C27D9C83982C53C8CEEC88DEAE27E62AB18C911`
- Citrus Mint remains unapproved and unreferenced by the release theme and all
  four release scenes. OpenSpec task 3.4 remains unchecked.

## Workflow assertions

The registry discovered the two production sets in deterministic semantic
identity order: `citrus-mint@1, sunny-orchard@1`. Candidate validation returned
`0 errors / 0 warnings`.

The isolated preview used a non-persistent theme clone and candidate draw
context. The actual EditorWindow preview path was render-ready for all 40
semantic slots, all 16 finite component kinds across all 9 interaction states,
and representative Lobby, Battle, and Settlement chrome. Preview left the
serialized release-theme bytes and all four scene bytes unchanged.

Activation changed only `RuntimeUiTheme.activeArtSet`, resolved every one of
the 40 required slots, and created exactly one Undo group named
`Activate Runtime UI Art Set` (`undoGroup=1` in the direct run; group numbering
is intentionally editor-session-local). One Undo restored Sunny Orchard, one
Redo restored Citrus Mint, and the final Undo restored Sunny Orchard. The final
theme bytes, GUID, theme ID, and revision exactly matched the starting state.
The existing invalid-candidate matrix also remained zero-mutation and created
no Undo group.

The stabilized smoke has no Citrus Mint ID or filename branch: it applies the
same preview, activation, Undo/Redo, source/layout/scene immutability, complete
slot-resolution, and final release-dependency checks to every non-active
production ArtSet discovered by the registry.

## Protected hashes

The smoke captured these values before candidate preview/activation and
asserted the same bytes after preview, activation, Undo, Redo, and final restore.

| Protected owner | SHA-256 |
| --- | --- |
| Aggregate `Assets/Scripts/**/*.cs` snapshot | `CD666D08D6FD00E1F5BA4D826CDDFBD04F08579434F37F566FB8339D518DC4DE` |
| Aggregate authoritative-layout snapshot | `CF01AC5183EF3B07C1C4848E95D0F366D1BE904F0F77458C7ABFC62688AF65B7` |
| `PortraitShellLayout.cs` | `4E396C6EF55DE72D14667019290A437490506751E52BFF9C491E6832D2A88C75` |
| `BattleUiLayout.cs` | `41E8BD586065A1DF531875BCFD3073E74DEF5C63E8241D5E4A3CEDBEA98375A7` |
| `Bootstrap.unity` | `27AD84F0D624DA6C1BE7152AD801990E6AE832E0A92019E6D585D35421E8ABD1` |
| `Lobby.unity` | `B4FA8E3B1656D1440A47D38FFA6B2E0CAD512E40DEE673D35EC39E505FDA2A6C` |
| `Battle.unity` | `C6CF5D7246B4FE21EB205FF0D7D740B3B5FE2C1D482D5721F13D75C311621C4E` |
| `Settlement.unity` | `FB6A7204EE71A9C38551920A88287F394EF45899974F599D8C89C6B6BC6569BC` |

## Gates

- [`workflow-validation.log`](workflow-validation.log): direct registry,
  isolated preview, real activation, Undo/Redo, invalid rejection, in-place
  reimport, and exact final restore; Unity return code `0` and
  `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`.
- [`workflow-release-validator.log`](workflow-release-validator.log):
  `Valid (0 warning(s))`; Unity return code `0`.
- [`workflow-p0.log`](workflow-p0.log): the aggregate gate repeated the real
  candidate workflow and ended with `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`; Unity
  return code `0`.
- `openspec validate unify-runtime-ui-visual-system --strict`: pass.

Post-run checks found zero Citrus Mint GUID references in the release theme or
four scenes and zero `GeneratedInvalid*` files. The manual server remained the
same listening process throughout the run.
