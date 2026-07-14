# P1 first-wave gate

Observed on 2026-07-15 against Unity `6000.3.19f1` on branch
`codex/p0-p1-foundation`.

## Decision

**The P1 second wave is closed.** The battle state/presentation boundary is
Green, but the Douyin and WeChat device/export spikes remain Yellow. Per the
Douyin-first plan, no production runtime adapter, remote-content delivery,
profile backend, or code-package updater is authorized yet.

Ordinary WebGL success is only a shared baseline. It must not be reported as a
Douyin or WeChat conversion result, and an unavailable mini-game adapter must
not silently fall back to the Web adapter.

## First-wave status

| Change | Progress | Gate | Result |
|---|---:|---|---|
| `separate-battle-state-and-presentation-events` | 14/14 | Green | Simulation emits bounded, ordered, one-way presentation events; snapshots and checksums exclude delivery state. |
| `spike-douyin-unity6-export-and-device` | 8/14 | Yellow | Unity/WebGL baseline is healthy; TTSDK, authorized conversion, simulator, Android/iOS, remote-content, Wasm, and soak evidence are missing. |
| `spike-wechat-unity6-export-and-device` | 8/15 | Yellow | Unity/WebGL baseline is healthy; reviewed WXSDK conversion, Stable Developer Tools, simulator, devices, remote-content, subpackage/Wasm, and soak evidence are missing. |

Both readiness scripts return zero for a Yellow diagnostic run and exit `2`
with `-RequireGreen`, so CI can block formal integration without treating a
partially prepared developer machine as a script failure.

## Integrated acceptance evidence

- Presentation boundary: `FRUIT_DEFENSE_PRESENTATION_BOUNDARY_OK` in
  `Logs/final-p1-boundary.log`.
- Unified P0 regression: `FRUIT_DEFENSE_P0_RELEASE_GATE_OK` in
  `Logs/final-p1-p0-suite.log`.
- WebGL: `FRUIT_DEFENSE_WEB_BUILD_OK`, build version `2f080450b1d0`, total
  output `8,078,303` bytes in `Logs/final-integrated-webgl-build.log`.
- Full route flow: `Logs/flow-acceptance/20260715-031011/flow-acceptance.json`.
- Existing 13-state acceptance:
  `Logs/visual-acceptance/20260715-031024/acceptance.json`.
- Strict OpenSpec validation passes for all three first-wave changes.

## Minimum unblock sequence

1. Provide non-production Douyin and WeChat AppIDs and interactive developer
   sessions outside Git.
2. Review and install a candidate TTSDK/Douyin IDE pair and a candidate official
   WXSDK/Stable WeChat Developer Tools pair in isolated platform worktrees.
3. Compile, export, convert, and repeat from a clean checkout while recording
   exact versions, hashes, and generated manifests.
4. Complete both simulator matrices and Android/iOS physical-device matrices,
   including 30-minute stability runs.
5. Prove platform content loading/cache/fallback and separately prove
   subpackage/Wasm behavior; neither is runtime code hot replacement.
6. Re-run both readiness scripts with `-RequireGreen`.
7. After Douyin is Green, open the formal Douyin runtime/content/backend changes.
   WeChat remains a follow-up until the Douyin-first release path is stable.

Detailed evidence requirements remain in `docs/platform/douyin-spike-status.md`
and `docs/platform/wechat-spike-status.md`.
