## 1. Payload Identity and Pipeline Evidence

- [x] 1.1 Generate and inject an independent SHA-256-derived version for each Unity WebGL payload.
- [x] 1.2 Replace singular Web content-version evidence with schema-2 per-asset version maps in local and online pipeline manifests.

## 2. Static Delivery Policy

- [x] 2.1 Emit cached strong content-hash ETags and grant immutable caching only when the request version matches the served bytes.
- [x] 2.2 Update deployment header checks to validate every advertised payload version, strong ETag, MIME type, encoding, and cache policy.

## 3. Cold and Warm Acceptance

- [x] 3.1 Record per-asset Resource Timing and Unity startup duration for a cold browser launch.
- [x] 3.2 Reload in the same browser profile, require a new Unity document, and fail when warm payload transfer exceeds the validation-header allowances.
- [x] 3.3 Record per-asset versions, validators, cold/warm timing, and transfer evidence in acceptance manifests.
- [x] 3.4 Document schema-2 per-payload evidence and the cold/warm publication gate in the release workflow owner.

## 4. Verification

- [x] 4.1 Run PowerShell parsing, Node syntax, acceptance self-check, and strict OpenSpec validation.
- [x] 4.2 Run the aggregate Unity P0 release gate and produce a fresh WebGL build.
- [x] 4.3 Run local cold/warm portrait acceptance and confirm schema-2 pipeline evidence is generated from the new build.
