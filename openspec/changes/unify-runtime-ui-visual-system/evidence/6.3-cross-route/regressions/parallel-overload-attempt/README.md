# Superseded parallel acceptance attempt

The first full-safe-area batch started five independent WebGL players, local
servers, and Chrome CDP profiles at once. `ShellError` completed, while the four
heavier modes failed before state capture with CDP WebSocket cancellation or a
warm-reload document timeout. This is acceptance-host resource contention, not a
runtime visual result.

The four failure modes are recorded here; their empty partial-run directories
and unreferenced server stdout/stderr were removed during task 7.3 cleanup. The
one ShellError image and manifest captured under load remain as diagnostic
evidence. Canonical task 6.3 reruns every affected mode serially against the
unchanged payload.
