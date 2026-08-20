# Superseded error-copy clipping regression evidence

This directory preserves the rejected ordinary-WebGL captures from payload
`data 1147491fa6d2 / wasm a11e5d08b673`. The shared semantic-state contrast fix
was present, but the raw blocking-error detail wrapped inside the fixed 45 px
status slot. Only `__missing-ui-` remained visible, so neither the user-facing
failure reason nor the application error code could be read.

The full and inset captures passed automated frame/no-route gates but failed the
manual copy and clipping review. This payload is superseded and is not release
evidence.
