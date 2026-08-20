## Why

The current portrait UI has a coherent visual system and strong static quality gates, but most player feedback is still expressed as discrete state changes with limited motion, cancellation, and gesture semantics. The analyzed reference APK provides concrete, evidence-backed interaction patterns that can make Lobby, Battle, and Settlement feel substantially more responsive without changing gameplay or adopting its heavyweight Lua/C# architecture.

## What Changes

- Add one shared runtime interaction-polish layer for cancellable press, pop/pulse, fade-slide, and stagger motion using unscaled time and a small set of semantic timing/easing tokens.
- Replace per-call pointer-state guesses with one authoritative press lifecycle that distinguishes pressed, released, cancelled, and drag-suppressed activation while preserving existing hit rectangles and commands.
- Apply restrained reference-derived feedback to the Lobby primary action and route reveal, Battle actions/status/resource changes, and Settlement result/reward/action reveal.
- Make motion optional through a reduced-motion policy that preserves state meaning without relying on animation.
- Permit provisional use of reference-derived raster resources only when their source, format, import settings, semantic slot, and replacement status are recorded; protected or unverifiable resources remain outside production runtime assets.
- Extend automated and live WebGL acceptance to verify deterministic motion samples, cancellation, input alignment, stable final layouts, and the absence of continuous repaint/allocation regressions.
- Keep gameplay rules, balance, persistence, content catalogs, route order, and platform-support claims unchanged.

## Capabilities

### New Capabilities

- `runtime-ui-interaction-polish`: Defines the shared motion language, press lifecycle, route-specific feedback, reduced-motion behavior, temporary reference-resource provenance, and runtime performance contract.

### Modified Capabilities

- `runtime-ui-quality-standard`: Adds temporal consistency, cancellation, and motion-safe state communication to the normative UI quality gate.
- `portrait-game-interface`: Requires animated controls to preserve authoritative draw/hit geometry, safe-area containment, and touch behavior throughout their transitions.
- `webgl-visual-acceptance`: Adds repeatable evidence for motion checkpoints, cancellation, and final resting-state stability on the real WebGL canvas.

## Impact

- Runtime UI presentation under `Assets/Scripts/UI`, `Assets/Scripts/Shell`, `Assets/Scripts/Presentation`, and `Assets/Scripts/FruitDefenseGame.cs`.
- Shared theme tokens in `Assets/UI/Theme` and, when suitable source assets become readable, provisional runtime art under the existing semantic ArtSet hierarchy.
- Focused editor validation under `Assets/Editor/Tests` and the existing aggregate smoke/build paths.
- Ordinary WebGL remains the release and acceptance baseline; no UGUI migration, new Tween package, Lua layer, gameplay change, persistence migration, or Douyin/WeChat support claim is introduced.
