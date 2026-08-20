# Obsolete Shell style removal

Task 5.2 deletes the final legacy Shell visual implementation after Lobby and Settlement migrated to the shared runtime UI system.

Deleted assets:

- `Assets/Scripts/Shell/ShellStyles.cs`
- `Assets/Scripts/Shell/ShellStyles.cs.meta`
- removed Unity GUID: `254185fe115e44f28089ada026ef8256`

No compatibility wrapper, fallback class, or replacement alias was added.

## Static no-reference evidence

Post-deletion repository searches report:

- `ShellStyleSet|ShellGui` under `Assets`: zero occurrences;
- removed GUID under `Assets`: zero occurrences;
- `Resources.Load<Font>`, `Fonts/NotoSansSC-UI`, `GUI.skin`, `Texture2D.whiteTexture`, skin-based `new GUIStyle(...)`, and `GUI.Box` under runtime `Assets/Scripts/Shell`: zero occurrences;
- `GUI.Label`, `GUI.Box`, and visual `GUI.DrawTexture` under runtime Shell: zero occurrences.

Lobby retains one deliberate direct `GUI.Button(rect, GUIContent.none, _drawContext.Styles.HitTarget)` call. It is the transparent hit layer paired with the shared themed card draw, contains no label/background/default skin, and preserves the authoritative draw/hit rectangle.

## Unity validation

Unity `6000.3.19f1` ran `FruitDefense.Editor.P0ValidationSuite.Run` after both deleted files were absent. The log contains:

- `FRUIT_DEFENSE_SHELL_OK`
- `FRUIT_DEFENSE_SMOKE_OK`
- `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`
- `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`
- process return code `0`

Raw log: [`unity-p0-after-shellstyles-removal.log`](unity-p0-after-shellstyles-removal.log).

The removal changes no presenter, layout, route behavior, scene, theme, art set, Battle code, or WebGL evidence.
