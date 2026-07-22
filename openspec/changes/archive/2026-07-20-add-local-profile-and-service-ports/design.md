## Context

App Bootstrap and the shell need small profile/configuration services, but gameplay must not depend on storage or a generic backend singleton. Unity WebGL also cannot rely on desktop file APIs behaving like a native filesystem, so storage requires a replaceable backend under one profile port.

## Goals / Non-Goals

**Goals:**

- Provide a small versioned profile envelope and bundled runtime configuration.
- Keep callers independent from file, PlayerPrefs, platform SDK, and future cloud implementations.
- Recover safely from missing/corrupt local data and make writes as atomic as each backend allows.
- Use Unity coroutine/callback semantics compatible with WebGL.

**Non-Goals:**

- Account identity, cloud sync, conflict resolution, economy, rewards, or battle autosave.
- Production remote config or network access.

## Decisions

1. `PlayerProfileEnvelopeV1` contains schema version, profile ID, revision, timestamps, locale, audio/vibration settings, last selected level, and shell preferences only.
2. `IPlayerProfileStore.Load/Save` and `IRemoteConfigService.Load` return `IEnumerator` and complete through callbacks with structured result codes.
3. `EditorFileProfileBackend` writes UTF-8 JSON to a temporary file, flushes, then atomically replaces the primary while retaining one backup. Corrupt primary data is quarantined and the backup/default is tried.
4. `WebPlayerPrefsProfileBackend` stores small JSON in staging, primary, and backup keys and calls `PlayerPrefs.Save`; it never stores battle snapshots or large content.
5. `BundledRemoteConfigService` returns an immutable config containing schema version, config version, content channel, bundled content version, and service feature flags.
6. No broad `IGameBackend` is introduced. P1 supplies account/cloud/config implementations for the same narrow ports.

## Risks / Trade-offs

- **[PlayerPrefs is not a database]** -> Keep the profile small and exclude battle/content payloads; P1 replaces it with platform/cloud storage.
- **[Crash between staged WebGL writes]** -> Read primary, then backup, then defaults; staging never becomes authoritative until validated.
- **[Future fields break old JSON]** -> Keep V1 additive defaults and reject only unsupported schema versions.
- **[Callbacks fire more than once]** -> Centralize completion guards and test success/failure paths.

## Migration Plan

1. Add DTOs, result codes, ports, and backend-independent validation.
2. Add Editor file and Web PlayerPrefs backends plus bundled config.
3. Compose services in final P0 Bootstrap integration.
4. Replace implementations in P1 without changing callers or the V1 envelope.
