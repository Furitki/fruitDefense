# Runtime UI release dependency and texture footprint

The authoritative full WebGL BuildReport is the first 7.2 build log,
`Logs/runtime-ui-7.2-webgl-build.log` (SHA-256
`6D4ABBCFBEA81F68A6B2CFED4CDDB267B25D39F9FD8752A700DA5CCAB83EA15D`).
The final build was script-only after the action-state priority regression fix;
there were no asset or scene dependency changes.

| Set measured during task 7.2 | Standalone PNG paths | Distinct content hashes | On-disk PNG bytes | RGBA32 estimate | Release BuildReport |
|---|---:|---:|---:|---:|---|
| `sunny-orchard` (active A) | 38 | 37 | 147,116 | 2,023,424 B (1.93 MiB) | 38 texture rows, 1,933.5 rounded KB total; ArtSet present once |
| `orchard-woodcraft` (then-inactive B; historical pre-rejection measurement) | 38 | 38 | 165,660 | 2,023,424 B (1.93 MiB) | 0 texture rows; ArtSet absent |

At measurement time both sets contained `1 x 256x256`, `15 x 128x128`, `21 x 96x96`, and
`1 x 32x32` standalone Sprite Single PNGs. A has 37 distinct content hashes
because `surface-panel-standard.png` and `surface-safe-area.png` are
intentionally identical source exports; they remain two stable semantic paths
and bindings.

The same BuildReport records the packaged Chinese font exactly once at
`251.9 KB`. It also records its owned `OFL-NotoSansSC.txt` (`4.4 KB`) and Fonts
README (`1.0 KB`). The BuildReport contained no candidate-B runtime texture or
B ArtSet dependency. The user later rejected B and task 7.3 removed its
production assets, so the current repository inventory is A-only while this row
remains an explicitly historical footprint measurement. No atlas,
Resources-based UI art lookup, or second drawing path was introduced.

Final 7.2 WebGL payload:

| File | Bytes | SHA-256 |
|---|---:|---|
| `WebGL.loader.js` | 117,893 | `E0FD8292100517F441CE1E5AB1EB7A8219AA40919E46F125667090633A9B9388` |
| `WebGL.data.unityweb` | 4,833,465 | `CC0A2B18A0C194BD7AAFAFF1E0B6B49247162799D54671CA892045612E938A60` |
| `WebGL.framework.js.unityweb` | 69,023 | `628DED04A7AF9570E4032627EDCF6F973928EA5E8AA51C10F5ED97150760FC77` |
| `WebGL.wasm.unityweb` | 3,881,824 | `868B0585638663A2AD55C2D2BA62DAC4CAEE134BA340E01EBC53C841B25E084A` |
