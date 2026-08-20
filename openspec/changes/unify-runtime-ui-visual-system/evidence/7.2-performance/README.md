# 7.2 Runtime UI performance, dependency, glyph, and clarity evidence

## Result

The release visual path remains one `RuntimeUiTheme` + one active production
ArtSet. `RuntimeUiDrawContext` now resolves the finite 40-slot semantic contract
once at context construction and serves required bindings in O(1) thereafter.
The active A set is present in the WebGL BuildReport. At measurement time the
inactive B set had zero release dependencies; the user subsequently rejected
it and task 7.3 removed its production assets, leaving the current repository
inventory A-only. The packaged font covers the single authoritative 372-glyph
player-visible probe. Three required WebGL clarity cases pass.

- [Binding-cache benchmark](binding-cache-benchmark.md)
- [Build dependency and texture report](build-dependency-report.md)
- [Warm-frame profile](warm-profile.json)

## Warm-frame profile

The profile uses a `402x874` EditorWindow proxy that draws the same shared
`RuntimeUiGui` chrome for 20 warm repaints followed by 45 sampled repaints per
scenario. Battle world simulation/rendering and browser overhead are excluded.

| Scenario | GC bytes median / p95 | Main-thread median / p95 |
|---|---:|---:|
| Lobby idle | 3,330 / 4,290 | 0.679 / 0.987 ms |
| Battle ready | 2,754 / 2,754 | 1.075 / 1.348 ms |
| Battle dense | 2,820 / 2,820 | 1.092 / 1.329 ms |
| Battle pause | 3,108 / 3,108 | 1.245 / 1.492 ms |
| Settlement | 2,988 / 2,988 | 0.744 / 0.820 ms |

These are reproducible local observations, not pass/fail budgets. In batchmode,
the `GUI.Repaint` ProfilerRecorder and `UnityStats` draw/batch/SetPass counters
returned only zeros. The evidence records those fields as `-1` and labels them
`unavailable-in-batch-editor-window`; zero is not claimed as the render cost.
The ordinary release WebGL player is non-development and exposes no Unity
Profiler stream, so no unsupported WebGL profiler numbers are invented.

## Glyph authority

`RuntimeUiChineseGlyphCoverage.RequiredGlyphs` is the only glyph-probe list.
ProjectSetup and RuntimeUiVisualSystemValidator each consume it directly; the
old representative lists were removed. Its 372 unique characters cover the
player-visible Bootstrap, Lobby, Battle, and Settlement copy, dynamic content
names, the supported level/error ID alphabet, punctuation, and UI symbols.

The direct and aggregate smoke result is:

```text
RUNTIME_UI_GLYPH_COVERAGE_OK glyphs=372 unique=372 font=Assets/Resources/Fonts/NotoSansSC-UI.ttf
```

The authority is explicit and does not scan source comments.

## WebGL clarity

All three manifests identify `ui.sunny-orchard@1 / sunny-orchard@1`, use the
same final payload listed in the dependency report, and report accepted default,
alternate-selection, and Loading frames. Every action contrast sample is
`3.2155599848:1`; black and invalid fractions are zero.

| Case | Result | Evidence |
|---|---|---|
| 360x800 inset 32/24 | pass | [manifest](clarity/inset-360x800-32-24/shell-visual-evidence.json), [Loading PNG](clarity/inset-360x800-32-24/03-lobby-transition.png) |
| 402x874 full | pass | [manifest](clarity/full-402x874/shell-visual-evidence.json), [selected PNG](clarity/full-402x874/02-lobby-alternate-selection.png) |
| 402x874 inset 44/34 | pass | [manifest](clarity/inset-402x874-44-34/shell-visual-evidence.json), [default PNG](clarity/inset-402x874-44-34/01-lobby-default.png) |

Original-resolution manual review confirms readable Chinese strokes, square
icons within their safe inset, visible non-color selection/loading cues, and
continuous rounded nine-slice borders with no internal or outer gaps.

The 360 inset run also exposed a real feedback-priority regression: during the
short transition emphasis pulse, `emphasized` changed Loading Primary to
Pressed, producing measured WebGL colors `#5F9F43` / `#F6F1D7` and only
`2.8317:1`. `ResolveActionDrawState` now preserves approved Loading and Disabled
priority, while other emphasized states still map to Pressed. Pulse duration,
tokens, commands, layout, hit rectangles, and palette are unchanged. Direct
regression and final WebGL evidence both report `3.2156:1`.

## Gates

- release Runtime UI validator: `Valid (0 warning(s))`
- binding cache, glyph coverage, Loading contrast, and visual system direct smokes: pass
- aggregate P0: `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`
- final WebGL build: `FRUIT_DEFENSE_WEB_BUILD_OK`
- `openspec validate unify-runtime-ui-visual-system --strict`: pass
