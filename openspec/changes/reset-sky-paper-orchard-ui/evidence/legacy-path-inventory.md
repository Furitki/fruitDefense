# Legacy-path removal inventory

This inventory was captured before the reset implementation. A final audit must make
every production/layout/test/acceptance occurrence either use `PhaseWaveRow` role
language or disappear. Historical OpenSpec evidence outside the current specs may keep
old terms as history only.

## In-stage Wave/control-strip references

- `Assets/Scripts/Core/BattlefieldProjection.cs`
- `Assets/Scripts/Presentation/BattleUiLayout.cs`
- `Assets/Scripts/Presentation/BattleUiPresentationState.cs`
- `Assets/Scripts/FruitDefenseGame.cs`
- `Assets/Scripts/FruitDefenseGame.BattlefieldRendering.cs`
- `Assets/Scripts/FruitDefenseGame.Validation.cs`
- `Assets/Editor/Tests/BattleUiLayoutSmoke.cs`
- `Assets/Editor/Tests/RuntimeUiFeedbackTimingSmoke.cs`
- `Assets/Editor/Tests/RuntimeUiQualitySmoke.cs`
- `Assets/Editor/Tests/RuntimeUiTextInspectionCatalog.cs`
- `Assets/Editor/Tools/ProjectSetup.cs`
- `scripts/accept-webgl-portrait.ps1`
- `scripts/webgl-acceptance/run-direct.ps1`
- Current capabilities: `battle-session-controls`,
  `embedded-battle-control-surface`, `portrait-game-interface`,
  `webgl-visual-acceptance`, and the lifecycle wording in
  `compact-control-lifecycle-feedback`.

## Legacy single-font and synthesized-style references

- `Assets/Scripts/UI/RuntimeUiTheme.cs`
- `Assets/Scripts/UI/RuntimeUiVisualTypes.cs`
- `Assets/Scripts/UI/RuntimeUiGui.cs`
- `Assets/UI/Theme/ReleaseRuntimeUiTheme.asset`
- `Assets/Editor/Tools/ProjectSetup.cs`
- `Assets/Editor/Tools/RuntimeUiVisualSystemValidator.Theme.cs`
- `Assets/Editor/Tools/RuntimeUiChineseGlyphCoverage.cs`
- `Assets/Editor/Tools/CombatFloatingTextSdfGenerator.cs`
- `Assets/Editor/Tests/RuntimeUiGlyphCoverageSmoke.cs`
- `Assets/Editor/Tests/RuntimeUiQualityProfile.cs`
- `Assets/Editor/Tests/RuntimeUiQualitySmoke.cs`
- `Assets/Editor/Tests/BattleUiLayoutSmoke.cs`
- `Assets/Editor/Tests/CompactControlAcceptanceSmoke.cs`
- Runtime/development presenters that construct a UI draw context.
- `Assets/Resources/Fonts/README.md`
- `scripts/rebuild-ui-font.ps1`

Editor-only demonstration fonts that do not consume the release theme are outside the
runtime migration, but final searches must distinguish them explicitly rather than
silently treating them as release fallbacks.
