## Context

The app navigator provides Lobby, Battle, and Settlement routes, while the battle-session contracts define immutable launch/result values. The project needs minimal production shell surfaces that are usable at the 402x874 portrait acceptance size without prematurely implementing the meta game.

## Goals / Non-Goals

**Goals:**

- Provide clear Start, Return, and Retry actions.
- Reserve stable locations for future level, growth, and settings systems.
- Display the actual terminal result and keep navigation idempotent.
- Derive shell draw and hit geometry from the same layout values.

**Non-Goals:**

- Level selection logic, growth, economy, rewards, authentication, cloud data, ads, or settings implementation.
- Battle UI changes.

## Decisions

1. Lobby defaults to `orchard-01` and the active bundled content version. Start generates a GUID session and app-provided nonzero seed.
2. Reserved cards are visible, labeled as unavailable, and non-interactive; they are not deceptive placeholder buttons.
3. Settlement displays outcome, reached wave, and remaining lives from the immutable result.
4. Return clears the active result/session before navigating. Retry retains level/content version but generates a new GUID and seed.
5. Missing or mismatched Settlement data routes safely to Lobby with a structured error.
6. Presenters remain thin: they read route data and send navigator commands, with no simulation, save, or platform logic.

## Risks / Trade-offs

- **[Immediate-mode shell becomes another monolith]** -> Isolate shared portrait layout, Lobby presenter, and Settlement presenter in separate types.
- **[Reserved areas appear functional]** -> Render explicit locked/coming-later state and ignore pointer input.
- **[Repeated button events create duplicate transitions]** -> Disable active controls while navigator transition state is not Idle.

## Migration Plan

1. Add presenters and route-data binding without enabling them in the build list.
2. Add deterministic layout and interaction validation.
3. Let the integration change create/configure scenes and activate the full flow.
