## 1. Session Contracts

- [x] 1.1 Add immutable BattleLaunchRequest, BattleResult, BattleOutcome, initialization result, result sink, and IBattleSessionHost contracts
- [x] 1.2 Validate required session, level, seed, and content-version fields and reject repeated initialization

## 2. Battle Host Lifecycle

- [x] 2.1 Move simulation construction and current-request reset into the Battle host
- [x] 2.2 Remove battle-owned runtime bootstrap and persistent lifetime when the app Bootstrap integration is ready
- [x] 2.3 Submit terminal victory/defeat at most once and preserve pause-menu local restart semantics
- [x] 2.4 Dispose visibility, navigation, simulation, presenter, and transient callbacks on scene destruction

## 3. Platform Lifecycle

- [x] 3.1 Pause an active battle and reset the fixed-step accumulator on Background
- [x] 3.2 Keep the battle paused on Foreground until explicit player resume

## 4. Compatibility Validation

- [x] 4.1 Preserve FruitDefenseGame presentation and ConfigureAcceptanceState behavior
- [x] 4.2 Add host validation for single initialization/result, cleanup, local restart, and background handling
- [x] 4.3 Run OpenSpec validation, Unity compilation, host smoke, and the current project smoke
