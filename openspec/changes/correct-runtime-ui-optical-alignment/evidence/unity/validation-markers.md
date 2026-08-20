# Unity validation markers

All commands used Unity `6000.3.19f1`, batch mode, `-nographics`, and exited
with return code `0`.

| Execute method | Terminal marker |
| --- | --- |
| `FruitDefense.Editor.RuntimeUiQualitySmoke.Run` | `RUNTIME_UI_QUALITY_OK cases=59 viewports=4` |
| `FruitDefense.Editor.RuntimeUiVisualSystemSmoke.Run` | `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK` |
| `FruitDefense.Editor.ProjectSetup.SmokeValidate` | `FRUIT_DEFENSE_SMOKE_OK` |
| `FruitDefense.Editor.WebBuild.Build` | `FRUIT_DEFENSE_WEB_BUILD_OK path=.../Builds/WebGL compression=BrotliFallback stripping=High template=PROJECT:FruitDefensePortraitContain host=fruit-defense-portrait-contain-v1 size=9676034 payloads=[WebGL.data.unityweb:version=78d62a3c45c5:size=5583352, WebGL.framework.js.unityweb:version=7b327fa58679:size=69007, WebGL.loader.js:version=bdd789111db2:size=117893, WebGL.wasm.unityweb:version=8d135a1947bd:size=3895659]` |

No C# compiler error, unhandled exception, failed assertion, or validator failure
was present in the three Editor validation runs. Web delivery hashes, response
headers, cold/warm transfer measurements, and real-canvas checks are retained
in the JSON manifests under `../webgl/`.
