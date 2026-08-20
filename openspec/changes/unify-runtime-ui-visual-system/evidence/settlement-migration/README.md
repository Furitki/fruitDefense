# Settlement shared visual-system migration

Task 5.1 migrates Settlement presentation only. It does not change route legality, command order, recovery behavior, `SettlementShellLayout`, any draw/hit rectangle, release theme/art-set assets, scenes, Battle, or WebGL evidence ownership.

## Shared component mapping

| Settlement presentation | Shared contract |
| --- | --- |
| Full viewport and safe area | `DrawScreenBackground` + `DrawSafeArea` |
| “战斗结算” | `ScreenTitle` typography |
| Result container | `DrawResultCard` |
| Victory / defeat / returning | `Success` / `Error` / `Loading`, including distinct non-color indicators |
| Completed level | `DrawMetric` + `icon.resource-sun` |
| Reached wave | `DrawMetric` + `icon.resource-wave` |
| Remaining lives | `DrawMetric` + `icon.resource-core` |
| Retry | Primary action + retry icon |
| Return | Quiet action + return icon |
| Transition-disabled actions | `Loading` state with loading indicator and non-interactive hit target |
| Recoverable status | Warning status surface + warning indicator |

The existing 32/42 px metric rectangles are intentionally unchanged. They use `DrawMetric` in compact single-line value form so adopting the shared component does not create alternate geometry or make the old layout fit a new two-line template.

Retry is still evaluated before Return in `OnGUI`; both shared actions draw and hit-test the exact `SettlementShellLayout` rectangles. Existing `TryRetry`, `TryReturn`, duplicate-command rejection, selected-level preservation, missing-result recovery, and error reporting are unchanged.

Settlement no longer contains `Resources.Load`, `GUI.skin`, `ShellStyleSet`, `ShellGui`, direct `GUI.Label`, or direct styled `GUI.Button` presentation. The remaining obsolete Shell style implementation is deliberately left for task 5.2.

## Validation

Unity `6000.3.19f1` ran `FruitDefense.Editor.P0ValidationSuite.Run` in batch mode. The final log contains:

- `FRUIT_DEFENSE_SHELL_OK`
- `FRUIT_DEFENSE_SMOKE_OK`
- `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`
- `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`
- process return code `0`

Raw log: [`unity-p0.log`](unity-p0.log).

Settlement-specific validation additionally confirms:

- victory, defeat, and missing-result recovery map to success, error, and loading;
- loading/disabled/hover/pressed action state precedence;
- success/error, loading/disabled, and warning/error use distinct indicator Sprites;
- Retry/Return hit their unchanged drawn rectangles and reject hits while transitioning;
- the draw context is bound to the injected release theme/art set and its transparent hit style has no fallback font.

Strict OpenSpec validation is run after this evidence is written. Real WebGL victory/defeat/retry/return captures remain owned by task 5.3.

## Visible risk retained for 5.3

The compact metric rows are mechanically valid and retain their old rectangles, but the semantic icons reduce horizontal text space compared with the old text-only labels. The 5.3 real WebGL review should specifically check long completed-level IDs and Chinese metric readability at 360×800 full/inset portrait sizes before visual acceptance.
