# Douyin Unity compatibility spike status

Observed on 2026-07-15 against Unity 6000.3.19f1 and baseline revision `4034f7e`.

## Decision

**Overall gate: Yellow.** The project is healthy as a standard Unity WebGL build, but the production Douyin adapter is not authorized yet.

Green desktop evidence:

- Unity 6000.3.19f1 and its WebGL module are installed.
- The existing Unity rule smoke passes with `FRUIT_DEFENSE_SMOKE_OK`.
- The existing 13-state WebGL portrait acceptance passes.
- The baseline WebGL output exists and the pre-conversion output is approximately 7.7 MB.
- The readiness report omits credential values and records only presence flags.

Yellow release blockers:

- No pinned TTSDK or compatible Addressables/TTAssetBundle provider is installed.
- Douyin developer tools and an authorized AppID/session are unavailable on this machine.
- No converted project, simulator record, UpdateManager record, or remote-content fallback record exists.
- Android and iOS cold/warm start, lifecycle, battle, Wasm collection, and 30-minute stability runs are not available.

## Follow-up environment audit

The 2026-07-15 read-only audit found Windows 10 build 19045, PowerShell 5.1, Unity WebGL support, and Node `v24.14.0`. It found no TTSDK/StarkSDK root, TTSDK package, Addressables package, Douyin IDE uninstall record, `tmg` command, `tt-wasmsplit-ci` command, AppID flag, developer-session flag, upload-credential flag, Android device evidence, or iOS device evidence. Node remains an observed desktop version, not a converter pin. The isolated follow-up worktree does not contain the ignored `Builds/WebGL` artifact, so its refreshed readiness report correctly marks that local artifact row Yellow; this does not invalidate the earlier 7.7 MB baseline result, but a fresh build is still required before conversion.

None of tasks 3.1 through 3.6 can be completed from this machine state:

| Task | Reusable preparation now present | Evidence still required |
|---|---|---|
| 3.1 | Exact pin fields and hash slots in `douyin-toolchain-pin-template.json` | Reviewed TTSDK, IDE and converter; compile, WebGL export, conversion and clean retry on Unity 6000.3.19f1 |
| 3.2 | Simulator checklist in `douyin-evidence-manifest-template.json` | Authorized AppID/session, converted project and simulator logs for every row |
| 3.3 | Android matrix and non-secret device metadata fields | Physical Android preview, full matrix, memory/crash evidence and 30-minute run |
| 3.4 | iOS matrix and non-secret device metadata fields | Physical iOS preview, full matrix, memory/crash evidence and 30-minute run |
| 3.5 | Provider/fallback/cache/unload checklist | Addressables plus TTSDK provider, approved HTTPS cache domain, supported-host and fallback runs |
| 3.6 | Named flow coverage checklist | AppID-backed Wasm preparation, Android then iOS function collection, split artifact and post-split preview |

## Minimum unblock checklist

1. Authorize a non-production Douyin Mini Game AppID and interactive developer login; keep credentials outside Git.
2. Review and install one official TTSDK package and Douyin IDE `4.5.2` candidate in the isolated platform worktree. Do not call either version pinned yet.
3. Copy `douyin-toolchain-pin-template.json` to a non-template pin file and fill every applicable version only after it is directly observed.
4. Compile, export WebGL, convert, then repeat from a clean checkout; record SHA-256 values for logs and generated manifests.
5. Copy `douyin-evidence-manifest-template.json` to `docs/platform/douyin-evidence/manifest.json` and complete simulator, Android, iOS, remote-content, and Wasm rows. Each `artifacts` item must contain a manifest-relative `path` and matching `sha256`.
6. Re-run `scripts/check-douyin-readiness.ps1`; keep the gate Yellow until all blocking rows have evidence.

## Next action

Retain the Yellow gate. An authorized operator must provide platform access, then run tasks 3.1 through 3.6 in the OpenSpec change. Start `add-douyin-runtime-adapter` only after all blocking rows in `douyin-compatibility-report.json` are Green.
