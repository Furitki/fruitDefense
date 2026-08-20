# Runtime UI binding-cache benchmark

Unity: `6000.3.19f1`, Windows Editor batchmode. Both runs resolve the release
`sunny-orchard@1` ArtSet and perform 25,000 complete 40-slot passes. Timings are
observations from one paired local run, not release thresholds.

| Sample | Operations | Elapsed | ns/op |
|---|---:|---:|---:|
| Context construction before cache | 64 | 0.4133 ms | 6,457.81 |
| Linear `RuntimeUiArtSet.GetRequiredBinding` before | 1,000,000 | 80.4732 ms | 80.47 |
| Context construction after cache | 64 | 0.4895 ms | 7,648.44 |
| Same-run linear control after | 1,000,000 | 79.6304 ms | 79.63 |
| Cached `RuntimeUiDrawContext.RequiredBinding` after | 1,000,000 | 24.5633 ms | 24.56 |

The hot lookup changed from a 40-entry linear scan to one
`RuntimeUiArtSlots.RequiredIndex` lookup plus array access: approximately
`3.28x` faster in this paired run. The one-time context construction cost rose
by about `1.19 us` while constructing and validating the exact 40-entry cache.
The draw-context reuse key is unchanged.

The contract smoke proves complete, duplicate, and missing bindings; ArtSet
identity and revision invalidation; exact 40-entry cache length; and reference
identity for every cached binding. The final aggregate run reported:

```text
RUNTIME_UI_BINDING_CACHE_CONTRACT_OK slots=40 complete=pass duplicate=rejected missing=rejected identity=pass revision=pass
RUNTIME_UI_PERFORMANCE_SMOKE_OK slots=40 lookup-passes=25000
```

`GC.GetAllocatedBytesForCurrentThread()` returned zero even for the deliberate
context-construction sample in this Unity batch environment, so that direct
counter is not treated as allocation proof. The independent warm-frame
ProfilerRecorder evidence in `warm-profile.json` owns the allocation samples.

Raw log hashes:

- before: `5A5417CB3E0932E29EB267719261F14F3D99D6E686E4F679C9CC1CC720CC089A`
- after: `A6359EE330155EFD6D4D4D97A1C8BC842E7C69B34492EBB7B7039FB65BB38BC7`

