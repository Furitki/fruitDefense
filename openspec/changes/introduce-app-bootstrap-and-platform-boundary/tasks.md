## 1. Platform Runtime Contracts

- [x] 1.1 Add platform identity, launch context, initialization result, visibility, adapter, and lifecycle receiver contracts
- [x] 1.2 Implement immutable launch-query parsing and available Editor/Web adapters
- [x] 1.3 Implement explicit unavailable Douyin/WeChat adapters and current/explicit platform factory selection without Web fallback

## 2. Application Navigation

- [x] 2.1 Add application route and transition state contracts
- [x] 2.2 Implement the guarded two-phase Lobby/Battle/Settlement navigator with failure and retry behavior

## 3. Composition Root

- [x] 3.1 Add the dormant `AppBootstrap` component with duplicate protection, persistence, adapter ownership, and initialization state
- [x] 3.2 Forward deduplicated Unity visibility changes through the active adapter and dispose subscriptions safely

## 4. Validation

- [x] 4.1 Add deterministic contract validation for URL parsing, Editor/Web success, unavailable mini-game identities, navigation edges, duplicate guards, failure, and retry
- [x] 4.2 Run OpenSpec validation, Unity compilation, the new app-framework validation, and the existing `FruitDefense.Editor.ProjectSetup.SmokeValidate`
- [x] 4.3 Confirm no changes to `FruitDefenseGame`, Main scene, ProjectSettings, EditorBuildSettings, ProjectSetup, or existing WebGL build behavior
