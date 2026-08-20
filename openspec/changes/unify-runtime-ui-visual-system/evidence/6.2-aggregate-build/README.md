# 6.2 aggregate validation and WebGL build

Unity `6000.3.19f1 (7689f4515d75)` ran the final merged source in batch mode.

## Strict and aggregate gates

- `openspec validate unify-runtime-ui-visual-system --strict` passed before Unity.
- The single aggregate Unity invocation was
  `FruitDefense.Editor.P0ValidationSuite.Run`; its log is
  `Logs/runtime-ui-6.2-p0-final.log`.
- Unity compiled the editor and runtime assemblies without a C# error before the
  entry point ran. The same invocation emitted the deterministic, battle-session,
  Shell, feedback-timing, Chinese-glyph, binding-cache, UI-performance,
  visual-system, ProjectSetup aggregate, and P0 markers:

| Contract | Marker |
|---|---|
| Deterministic simulation | `Fruit Defense deterministic simulation validation passed.` |
| Battle session | `Fruit Defense battle session host validation passed.` |
| Shell | `FRUIT_DEFENSE_SHELL_OK` |
| Feedback timing | `RUNTIME_UI_FEEDBACK_TIMING_OK` |
| Chinese glyphs | `RUNTIME_UI_GLYPH_COVERAGE_OK glyphs=372 unique=372` |
| Binding cache | `RUNTIME_UI_BINDING_CACHE_CONTRACT_OK slots=40` |
| UI performance | `RUNTIME_UI_PERFORMANCE_SMOKE_OK slots=40 lookup-passes=25000` |
| Visual system | `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK` |
| ProjectSetup aggregate | `FRUIT_DEFENSE_SMOKE_OK` |
| Release gate | `FRUIT_DEFENSE_P0_RELEASE_GATE_OK` |

The Unity process ended with `return code 0`.

## Ordinary WebGL build

`FruitDefense.Editor.WebBuild.Build` ran independently after P0. The durable log
path is `Logs/runtime-ui-6.2-webbuild-final.log`; it contains both
`Build Successful` and `FRUIT_DEFENSE_WEB_BUILD_OK`, followed by Unity
`return code 0`. The generated `Builds/WebGL` output is 8,922,008 bytes.

| Payload | Bytes | Full SHA-256 |
|---|---:|---|
| `WebGL.data.unityweb` | 4,833,465 | `cc0a2b18a0c194bd7aafaff1e0b6b49247162799d54671ca892045612e938a60` |
| `WebGL.framework.js.unityweb` | 69,023 | `628ded04a7af9570e4032627edcf6f973928ea5e8aa51c10f5ed97150760fc77` |
| `WebGL.loader.js` | 117,893 | `e0fd8292100517f441ce1e5ab1eb7a8219aa40919e46f125667090633a9b9388` |
| `WebGL.wasm.unityweb` | 3,881,824 | `868b0585638663a2ad55c2d2ba62dac4caee134ba340e01ebc53c841b25e084a` |
| `index.html` | n/a | `fa2b11983ba390afda467dfe188ae97b95a61f28116b8460023ecd84ac535f5c` |

The version prefixes written into `index.html` are respectively
`cc0a2b18a0c1`, `628ded04a7af`, `e0fd82921005`, and `868b05856386`.

## Side-effect boundary

Content digests were recorded immediately before P0 and after WebBuild. The
following groups were byte-identical before and after:

| Protected group | Files | Aggregate SHA-256 |
|---|---:|---|
| Runtime-UI fixtures | 2 | `f16f9746b439387a9deeea1855314e41b08eb6d1afa99c87ba74c215eb1a4905` |
| Bootstrap/Lobby/Battle/Settlement scenes plus metas | 8 | `69fe32f2ef6447ea3fc39df14fc34db6665732f582b5029cca810575137fe7c8` |
| Release theme directory | 4 | `1502cc76d395518d0bc18e2fbaf05eba968761c23663ff4802db2c6490b91de7` |
| Art-set definition directory | 6 | `4a087c323415484454970bcbb87be52592ea8282e11b442aee52676da0973b91` |

`Assets/Editor/Tests/Fixtures/RuntimeUi/GeneratedInvalid` was absent before and
after the gate. The release theme remains `ui.sunny-orchard@1`, with the active
production set `sunny-orchard@1`. `ProjectSettings/TimeManager.asset` has no
content diff from `HEAD` despite its pre-existing working-tree stat entry.
Existing non-UI canonical-map smoke fixture serialization noise was neither
claimed as UI evidence nor reverted.
