# Settlement acceptance evidence (task 5.3)

Status: accepted on the final post-partition-fix WebGL payload.

## Fixed release payload

- Loader: `23dda1fa00d8`
- Data: `761196808a41`
- Framework: `74a8df0275f8`
- Wasm: `577ad40e2527`
- Runtime UI: `ui.sunny-orchard@1 / sunny-orchard@1`
- Active ArtSet GUID: `12cc0c638d174040bb0384d7bf17ea92`

The two runs below reused the exact `Builds/WebGL` payload re-signed by task 3.5.
No rebuild or runtime mutation occurred during task 5.3.

## Real WebGL route evidence

| Case | Outcome | Selected level | Safe area | Evidence |
|---|---|---|---|---|
| Reference | Victory | `orchard-02` | 402x874, full | [`flow-acceptance.json`](victory-402x874-full/flow-acceptance.json), [`Settlement`](victory-402x874-full/03-settlement-victory.png), [`returned Lobby`](victory-402x874-full/04-returned-lobby.png), [`retry Battle`](victory-402x874-full/05-retry-battle.png) |
| Narrow portrait | Defeat | `orchard-03` | 360x800, top 32 / bottom 24 | [`flow-acceptance.json`](defeat-360x800-inset/flow-acceptance.json), [`Settlement`](defeat-360x800-inset/03-settlement-defeat.png), [`returned Lobby`](defeat-360x800-inset/04-returned-lobby.png), [`retry Battle`](defeat-360x800-inset/05-retry-battle.png) |

Both manifests report `accepted=true`, zero failed checks, the same release UI
and payload identities, and the following route invariants:

- Battle and Settlement retain the same completed session.
- Return clears the completed session/result before routing to Lobby and keeps
  the selected level (`orchard-02` / `orchard-03`).
- Starting again after Return creates a different session.
- Retry creates a fresh session ID and fresh nonzero seed while preserving the
  completed level and content identity.
- Retry and Return are each issued once; transition-disabled behavior and exact
  hit rectangles remain covered by `ShellLayoutValidation` in the P0 gate.

The stable reference centers remain Retry `(201, 449)` and Return `(201, 528)`.
For the 360x800 inset projection they map to `(180, 414.2151)` and
`(180, 481.4645)` without changing the underlying hit rectangles.

## Visual review

- Victory and defeat use distinct wording plus success/error indicator sprites;
  the outcome is not communicated by color alone.
- Titles, metrics, Retry, and Return remain inside the result card/safe area with
  no overlap or clipping in either capture.
- The 360x800 `orchard-03` completed-level metric remains on one line.
- The selected level is visibly preserved in each returned-Lobby capture.
- Both retry captures visibly return to a fresh Battle route.
- Shell/App chrome has no `ShellStyleSet`, `ShellGui`, runtime font Resources,
  `GUI.skin`, or `Texture2D.whiteTexture` path.

The final result-card partition was probed across all four internal boundaries.
Every five-pixel neighborhood is continuous `#FFF0C1`:

| Case | Left | Right | Top | Bottom |
|---|---|---|---|---|
| 402x874 victory | x30--34 around x32 | x368--372 around x370 | y135--139 around y137 | y363--367 around y365 |
| 360x800 defeat | x34--38 around x36 (including former gap x35) | x322--326 around x324 | y147--151 around y149 | y341--345 around y343 |

Task 3.5 separately re-ran the full 8+2 Bootstrap/Lobby matrix and outer-edge
probes on this same payload after the renderer fix.

## Editor recovery and geometry evidence

`Logs/ui-nine-slice-complete-partition-p0.log` completed with
`FRUIT_DEFENSE_SHELL_OK`, `RUNTIME_UI_NINE_SLICE_PARTITION_OK`, and
`FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.

The aggregate Shell validation covers victory, defeat, Retry, Return, selected
level preservation, and both missing-result and result-level-mismatch recovery.
Recovery rejects fabricated view data, reports a structured recoverable error,
and requests Lobby exactly once. There is deliberately no WebGL-only hook that
injects an impossible missing Settlement result into the release flow.
