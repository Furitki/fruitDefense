# Retained acceptance-infrastructure regressions

These captures are **not canonical** and must not be presented as product
failures or acceptance passes.

- `old-shell-control-geometry/full-360x800-attempt-01/01-lobby-default.png`:
  the final Lobby rendered correctly, but the acceptance probe still sampled
  the former Start rectangle. The stable script control points were aligned to
  the final layout/hit authority and `-SelfCheck` passed before canonical runs.
- `transition-timing/full-360x800-cpu8-attempt-01/`: default and alternate
  states rendered correctly, but the short transition was missed after one
  bounded CPU-throttle 8 attempt. The existing CPU-throttle 20 path captured
  all eight canonical transitions on attempt 1 without changing runtime timing.

Capture-server stdout/stderr are disposable and intentionally removed.

