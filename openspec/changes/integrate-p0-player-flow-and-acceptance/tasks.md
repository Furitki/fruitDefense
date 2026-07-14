## 1. Dependency Integration

- [x] 1.1 Merge and validate the app/platform, content catalog, and deterministic simulation changes
- [ ] 1.2 Merge and validate the battle session, lobby/settlement, snapshot, and local-service changes
- [x] 1.3 Resolve integration only through the frozen P0 public contracts and record any required contract amendment

## 2. Scene and Composition Setup

- [x] 2.1 Update ProjectSetup to generate or configure Bootstrap, Lobby, Battle, and Settlement scenes
- [x] 2.2 Set the enabled build order to Bootstrap, Lobby, Battle, Settlement and assert a single persistent Bootstrap
- [x] 2.3 Wire bundled content, local profile/config services, navigator, battle launch, result, return, and retry
- [x] 2.4 Add safe routing for missing launch data, missing result data, missing scenes, and platform initialization failure

## 3. Acceptance Integration

- [x] 3.1 Extend editor smoke for scene order, duplicate navigation, session cleanup, retry identity, and background pause
- [ ] 3.2 Route `acceptance=1&route=battle` through Bootstrap and preserve all existing named battle states
- [ ] 3.3 Add WebGL full-flow acceptance for Lobby, Battle, Settlement, return, retry, and failure-safe routing
- [x] 3.4 Derive shell draw and hit-test geometry from shared portrait layout helpers

## 4. Release Validation

- [ ] 4.1 Run OpenSpec validation and Unity editor smoke
- [ ] 4.2 Build WebGL with the release scene order and verify versioned delivery metadata
- [ ] 4.3 Run the existing 13-state battle acceptance and the new complete-flow acceptance
- [ ] 4.4 Verify a clean Git diff, document the P0 release baseline, and mark all integration tasks complete
