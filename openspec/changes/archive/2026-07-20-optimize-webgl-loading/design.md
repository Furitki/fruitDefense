## Context

The Unity WebGL output is 9.65 MB compressed, dominated by `WebGL.data.gz` and `WebGL.wasm.gz`. The Node static server currently applies `Cache-Control: no-cache` to every response, the HTML versions only the loader, and public transfer measurements are much slower than server-local reads. The deploy flow already builds, runs portrait visual acceptance locally, uploads the output, and repeats acceptance against the public service.

## Goals / Non-Goals

**Goals:**

- Make repeat visits reuse the large Unity payload without serving stale builds after deployment.
- Reduce first-load transfer size with Brotli and production managed-code stripping.
- Make cache and compression correctness a deploy-blocking acceptance condition.
- Preserve the existing portrait player flow and four-state canvas acceptance.

**Non-Goals:**

- Changing server bandwidth, provisioning CDN infrastructure, or introducing a domain/TLS migration.
- Changing gameplay, persistence, UI geometry, or runtime controls.
- Replacing the small Node service with a different production server in this change.

## Decisions

### Content-derived version query on every Unity payload URL

After a successful build, the editor build script will hash the compressed Unity payload and loader files, derive a short build version, and append the same `?v=<version>` query to loader, data, framework, and WASM URLs in `index.html`. Query versioning keeps Unity's conventional filenames while giving every deployment a distinct browser cache key. Timestamp-only versioning was rejected because identical builds would produce needless cache misses, and renaming files was rejected because it would require deeper Unity template/loader changes.

### Split cache policy by entry point and versioned assets

`index.html` and unversioned resources will remain `no-cache`, while requests under `/Build/` with a non-empty version query will receive `public, max-age=31536000, immutable`. The HTML therefore discovers each new version immediately, and large versioned payloads remain reusable. Applying immutable caching to stable filenames without versioning was rejected because deployments would leave clients on stale code.

### Brotli with decompression fallback on the current HTTP origin

Unity will produce Brotli-compressed `.unityweb` payloads and include its JavaScript decompression fallback. The static server will deliver those opaque containers as `application/octet-stream` without `Content-Encoding`; the Unity loader will decompress them before execution. Native browser Brotli was initially preferred, but public acceptance proved that the current bare-HTTP origin cannot reliably load `Content-Encoding: br` even though localhost succeeds. When the deployment moves to HTTPS/CDN, the fallback can be disabled and native Brotli headers can be enabled for lower client CPU cost.

### High managed stripping with runtime smoke and canvas acceptance

The WebGL build will use Unity's high managed stripping level to remove unused IL2CPP code. Risk from reflection or over-stripping is controlled by the existing editor smoke plus a real WebGL startup and four-state canvas interaction test on both local and deployed builds.

### Delivery metadata is validated from generated HTML

Acceptance will extract the actual loader/data/framework/WASM URLs from `index.html`, require a shared version token, and inspect their headers. This avoids hardcoded `.gz` assumptions and makes compression/cache regressions fail before or after deployment. Asset sizes and delivery metadata will be recorded in the acceptance manifest for comparison.

## Risks / Trade-offs

- [JavaScript Brotli fallback adds startup CPU work] -> Keep the fallback only while the origin is bare HTTP; verify successful startup on the mobile-sized real canvas and switch to native Brotli after HTTPS/CDN becomes available.
- [High stripping removes reflection-only code] -> Keep Unity smoke and interactive WebGL acceptance mandatory; add link preservation only if a concrete failure appears.
- [A stale HTML document points at an old immutable payload] -> Keep HTML `no-cache`; deployed output replacement remains atomic enough for the current single-process service because old assets are not expected to be requested after the new HTML is fetched.
- [Query-version policy could cache an arbitrary caller-supplied version] -> Only generated HTML advertises versioned URLs; version tokens are content-derived and acceptance checks consistency.
- [First visit is still constrained by public bandwidth] -> Brotli and stripping reduce bytes, but CDN/bandwidth work remains a later infrastructure change.

## Migration Plan

1. Build locally with Brotli fallback, high stripping, and content-versioned URLs.
2. Run smoke plus local delivery/header and four-state canvas acceptance.
3. Deploy through the existing script, restart the service, and run the same public acceptance.
4. Confirm HTML is non-immutable and all advertised `.unityweb` payloads are opaque Brotli fallback containers plus immutable.
5. Roll back by redeploying the previous gzip output and server implementation if startup or compatibility validation fails.

## Open Questions

None for the current direct-server deployment. CDN cache rules and purchased bandwidth will be handled when infrastructure changes become available.
