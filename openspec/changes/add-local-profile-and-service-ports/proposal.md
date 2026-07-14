## Why

Lobby, settings, remote configuration, and future cloud save need explicit persistence/service boundaries before platform code arrives. P0 should provide a small versioned profile and bundled-config implementation without coupling gameplay to files, PlayerPrefs, SDKs, or a generic backend facade.

## What Changes

- Define `PlayerProfileEnvelopeV1` for schema identity, settings, last selected level, and non-economic shell preferences.
- Define coroutine/callback-based `IPlayerProfileStore` and `IRemoteConfigService` ports.
- Add an atomic local JSON profile store for Editor and Web-compatible persistent storage, with corruption quarantine and bundled-default fallback.
- Add a bundled remote-config implementation with version and content-channel fields used by P0 composition.
- Keep battle snapshots in their separate protocol; P0 does not automatically save or restore a running battle.
- Exclude accounts, cloud synchronization, economy, rewards, platform SDKs, and production remote config.

## Capabilities

### New Capabilities

- `local-profile-service-ports`: Defines versioned profile data, local persistence behavior, bundled configuration, and future backend substitution boundaries.

### Modified Capabilities

None.

## Impact

- Adds application-level persistence and configuration contracts plus local/bundled adapters.
- App Bootstrap will compose these services during final P0 integration.
- P1 may replace implementations without changing callers or the profile envelope.
