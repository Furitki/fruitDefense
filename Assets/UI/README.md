# Runtime UI asset ownership

`Assets/UI` is the only production ownership root for the shared runtime UI visual system. It is deliberately outside every `Resources` folder; release dependencies must be explicit references from the release theme and its single active art-set definition.

## Stable locations

| Location | Ownership |
| --- | --- |
| `Art/Sources/<set-id>/` | Editable, lossless masters and source manifests. |
| `Art/Sources/ReferenceBoards/Approved/` | Approved visual-review references only; never production slices. |
| `Art/Runtime/<set-id>/` | Optimized production exports that may fill semantic art slots. |
| `Art/Sets/` | `RuntimeUiArtSet` definition assets. |
| `Theme/` | The one authoritative release `RuntimeUiTheme` asset. |

The approved release direction and active art-set ID is `sunny-orchard-painted`, derived from A「阳光果园」 and the approved painted component proof. Its copied reference board is [here](Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png). The copy and the painted review gallery are review inputs, not texture atlases or production exports.

The production hierarchy also retains the complete non-active `sunny-orchard` set to exercise isolated preview and atomic replacement. The rejected `citrus-mint` treatment has been removed from `Art/Sources`, `Art/Runtime`, and `Art/Sets`; historical review evidence outside `Assets` is not an activatable art set and must never be referenced by the release theme or a release scene.

## Source to runtime rule

Every production export has exactly one owned master under `Art/Sources/<set-id>/`, keeps the same semantic basename, and is recorded in that set's source/runtime manifest. Re-exporting in place preserves the runtime path and `.meta` GUID; visual changes increment the art-set content revision rather than changing presenter, scene, or layout bindings.

Raw generation output, experiments, review boards, captures, and editor-test fixtures are not production assets. Release themes and scenes may reference only complete set definitions in `Art/Sets/`, whose concrete textures/sprites all live under `Art/Runtime/<set-id>/`.

## Semantic filenames

Runtime filenames describe reusable semantic slots, never screens or routes:

- `surface-<role>.png`, for example `surface-panel-standard.png` or `surface-card-selectable.png`;
- `action-<role>.png`, for example `action-primary.png` or `action-danger.png`;
- `slot-<role>.png`, for example `slot-tool.png` or `slot-nursery.png`;
- `marker-<role>.png`, with selection represented by `marker-selected.png`;
- `indicator-<state>.png`, for example `indicator-loading.png` or `indicator-error.png`;
- `icon-<role>.png`, for example `icon-resource-sun.png` or `icon-control-pause.png`.
- `ornament-<role>.png` and `illustration-<role>.png` for explicitly contracted decorative and content-owned composition layers.

Names use lowercase ASCII kebab-case. Do not use `lobby-*`, `battle-*`, `settlement-*`, candidate labels, dates, or revision suffixes in asset filenames. The finite slot contract owned by `RuntimeUiArtSet` decides which names are required; a new role is a contract change, not an ad-hoc filename.
