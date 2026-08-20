# Superseded error-contrast regression evidence

This directory intentionally preserves the failed ordinary-WebGL captures from
payload `data ad29883fcac7 / wasm 6c461f17c1bb`. The payload is superseded and
must not be used as release evidence.

Both the full and inset `402x874` formal invalid-level inputs reached the real
application-owned Bootstrap blocking error without publishing route-ready. The
non-color error icon was visible, but the copy failed visual review:

- modal surface: approximately `#B04339`;
- title: approximately `#765237`, `1.223:1` against the modal;
- status row: approximately `#B04235`;
- state copy: approximately `#B04339`, `1.008:1` against the row.

The captures therefore prove the formal error entrance while also recording why
this payload was rejected. The sibling manifests were generated before manual
visual review; their `accepted: true` means only that automated route/frame gates
passed, not that the human contrast gate passed.
