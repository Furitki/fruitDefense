## 1. Profile and Configuration Contracts

- [ ] 1.1 Add PlayerProfileEnvelopeV1, validation, structured results, and immutable bundled runtime-config DTOs
- [ ] 1.2 Add coroutine/callback IPlayerProfileStore and IRemoteConfigService ports with single-completion guards

## 2. Local Implementations

- [ ] 2.1 Implement the Editor UTF-8 JSON temp/primary/backup backend with atomic replacement and corrupt-file quarantine
- [ ] 2.2 Implement the Web staging/primary/backup PlayerPrefs backend and explicitly exclude large battle/content payloads
- [ ] 2.3 Implement BundledRemoteConfigService with offline defaults for content version, channel, and feature flags

## 3. Validation and Composition Readiness

- [ ] 3.1 Add new/default, save/load, backup recovery, corrupt primary, unsupported schema, and callback-once tests
- [ ] 3.2 Prove callers can substitute fake/local/future cloud implementations through the same ports
- [ ] 3.3 Prove profile save never serializes BattleSnapshotV1 or active battle state
- [ ] 3.4 Run OpenSpec validation, Unity compile, service smoke, and project smoke
