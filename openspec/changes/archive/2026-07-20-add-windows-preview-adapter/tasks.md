## 1. Windows Preview Runtime

- [x] 1.1 Add an explicit Windows preview platform identity, adapter, and `UNITY_STANDALONE_WIN` current-host selection without changing mini-game precedence
- [x] 1.2 Extend app-framework validation for successful Windows preview initialization, retained identity, empty launch context, and no Web fallback

## 2. Validation And Build

- [x] 2.1 Run strict OpenSpec validation and the unified Unity P0 release gate with Unity `6000.3.19f1`
- [x] 2.2 Rebuild WebGL to prove the shared baseline still compiles with the extended platform boundary
- [x] 2.3 Rebuild the Windows 64-bit player and confirm a real player launch initializes without platform rejection

## 3. Handoff

- [x] 3.1 Record final artifact details, verify unrelated working-tree changes remain untouched, and confirm release/design documents were not changed
