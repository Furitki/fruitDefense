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

## Next action

Retain the Yellow gate. An authorized operator must provide platform access, then run tasks 3.1 through 3.6 in the OpenSpec change. Start `add-douyin-runtime-adapter` only after all blocking rows in `douyin-compatibility-report.json` are Green.
