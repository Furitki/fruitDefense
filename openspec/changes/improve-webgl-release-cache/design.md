## Context

The WebGL build currently hashes all four Unity payloads into one shared 12-character version. The static server treats any `/Build/` request with a non-empty `v` query as immutable, while its ETag is derived from file size and modification time. Unity data caching is enabled, but the generated loader revalidates the data payload; replacing the deployed files can therefore change its validator even when the bytes and URL remain identical. Existing acceptance checks advertised URLs and headers before one visual run, but does not measure a second launch using the same browser profile.

The current public origin is bare HTTP. Unity therefore emits Brotli fallback containers without `Content-Encoding` and decompresses them in the client. Moving the origin to HTTPS or a CDN needs infrastructure authority and is not part of this repository-only change.

## Goals / Non-Goals

**Goals:**

- Preserve the cache key of every unchanged Unity payload across publications.
- Make HTTP validators stable for identical bytes and reject arbitrary version queries as immutable cache keys.
- Prove cold and warm behavior in the same fresh browser profile before local upload and again after deployment.
- Keep release manifests explicit about every advertised payload version.

**Non-Goals:**

- Migrate the public service to HTTPS, a CDN, a service worker, or a new hosting provider.
- Split the Unity data file, introduce Addressables, or change game content loading.
- Change gameplay, persistence, portrait layout, or mini-game platform adapters.

## Decisions

### Version each payload from its own SHA-256

`WebBuild` will calculate SHA-256 independently for loader, data, framework, and WebAssembly output, use the first 12 lowercase hexadecimal characters as that file's `v` query, and log the complete per-file map. Unchanged files therefore retain their URL even when a different payload changes. A shared release token is removed instead of retained as a compatibility field.

Renaming Unity output files was rejected because query versions already integrate with the generated loader and static server without a custom WebGL template. A single release version was rejected because it invalidates unrelated payloads.

### Derive strong ETags from the same file bytes

The static server will lazily hash served files and emit the full SHA-256 hex digest as a strong ETag. A `/Build/` response is immutable only when its `v` value exactly equals the first 12 characters of that digest; missing or incorrect tokens remain revalidatable. The service process is restarted for every deployment, so a process-local path-to-hash cache is sufficient and avoids hashing large files on every request.

Size/mtime validators are removed because deployment extraction changes modification times independently of content. Trusting any non-empty version query was rejected because it allows callers to create immutable aliases that do not identify the bytes.

### Measure a cold launch and an immediate warm reload

Acceptance will start Chrome with a fresh profile, wait for the first Unity instance, and record Resource Timing entries for every advertised payload. It will then reload the same URL in the same profile, wait for a new document and Unity instance, and record the second set of entries. Every warm payload must transfer at most a small header allowance rather than its body, and total warm payload transfer must stay below a fixed 64 KiB ceiling. The manifest records per-asset transfer sizes and cold/warm startup durations.

The reload is performed before visual interactions so the existing route and screenshot acceptance proceeds from the warm, fully initialized instance. A separate browser or `curl` run was rejected because neither would prove reuse of the same browser cache.

### Replace singular version fields in pipeline evidence

Local and online pipeline manifests will move to schema version 2 and store an `assetVersions` map. The local pipeline extracts this map from the generated entry page rather than parsing one log token. The online pipeline requires schema version 2 and copies the map into publication evidence. Obsolete singular `contentVersion` fields are removed rather than supported in parallel.

## Risks / Trade-offs

- [Resource Timing can report small validation/header transfers for a cached response] → Permit at most 16 KiB per payload and 64 KiB total while still rejecting body-sized downloads.
- [A browser can expose an unexpected or missing timing entry] → Fail acceptance with the asset role and URL instead of silently treating it as cached.
- [Hashing a 14 MB payload adds server startup/request CPU] → Cache the digest for the lifetime of the deployment process; only the first request per file pays the cost.
- [Manifest schema 2 invalidates old ignored build evidence] → Require a new local build before the next online publication; do not add a schema compatibility path.
- [Bare-HTTP JavaScript Brotli decompression remains costly on cold launch] → Keep it explicit in evidence and defer native Brotli to an authorized HTTPS/CDN change.

## Migration Plan

1. Update build version injection and pipeline evidence parsing.
2. Update the static server validator/cache policy and deployment header checks.
3. Add cold/warm acceptance and manifest evidence.
4. Run syntax/self-checks, Unity release validation, a fresh WebGL build, and local cold/warm acceptance.
5. On the next authorized online publication, the pipeline writes schema-2 evidence and validates the deployed warm reload. Rollback is a source rollback followed by rebuilding; old schema-1 manifests are intentionally not reusable.

## Open Questions

None for the repository implementation. HTTPS/CDN ownership and migration timing remain a separate infrastructure decision.
