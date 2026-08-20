# Runtime UI editor smoke fixtures

`RuntimeUiVisualSystemSmoke` clones the currently approved complete production art set in memory, then applies one explicit serialized mutation for each fixture case:

- `complete`: unchanged 49-slot clone;
- `incomplete`: empty binding array;
- `missing`: one required binding removed;
- `duplicate`: one slot changed to duplicate another required slot;
- `invalid-path`: the complete clone is temporarily saved below this directory so production-path validation can reject it.

Generated `.asset` files are deleted in the smoke test's `finally` block. These fixtures are editor-only, are never stored in `Resources`, and must never be referenced by `ReleaseRuntimeUiTheme.asset` or a release scene.
