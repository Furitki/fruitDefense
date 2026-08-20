# Runtime UI final handoff

The ordinary-WebGL runtime UI visual/theme/art-set acceptance is complete for
`ui.sunny-orchard@1 / sunny-orchard@1` with active ArtSet GUID
`12cc0c638d174040bb0384d7bf17ea92`.

Review entry points:

- [final acceptance README](README.md)
- [machine-readable audit](final-audit.json)
- [aggregate P0 log](editor/p0-aggregate.log)
- [independent WebBuild log](webgl-build/webgl-build.log)
- [supported portrait Shell matrix](shell-visual/)
- [402x874 cross-route matrix](cross-route/)

The exact final payload is:

- loader `851fcdde61eef1484651f2967dacaa331ac377b0b8466d78c7557aaf1e0a507e`
- data `f6ef5e73ae2c98afee676d099f7b3a0eda2dcd946ef76362e456aaf759f24044`
- framework `628ded04a7af9570e4032627edcf6f973928ea5e8aa51c10f5ed97150760fc77`
- wasm `1e2ae62ab967aba634bef0d8aa3e5726eab40cbf20acbc7893fc4a43d55c7015`

`Builds/WebGL` is preserved at those hashes. Candidate B remains pending and
non-active, so task 3.4 remains intentionally unchecked.

That candidate sentence is the acceptance-time snapshot. The user later
rejected B; task 7.3 removed its production assets and retained its review logs
only as historical evidence. The accepted build was already A-only, its hashes
remain unchanged, and task 3.4 remains unchecked.

This handoff is limited to the UI visual system and ordinary WebGL evidence. It
does not authorize claims about gameplay correctness, persistence, Douyin or
WeChat support, or changes to the game-design overview.

## Post-rejection build addendum

After treatment B was removed from production ownership, an independent Unity
`6000.3.19f1` `FruitDefense.Editor.WebBuild.Build` completed with `Build
Successful`, `FRUIT_DEFENSE_WEB_BUILD_OK`, and return code `0`. The
[post-rejection log](webgl-build/post-rejection-webgl-build.log) has SHA-256
`08F83655004D85810159BBD34FF1F5167CFC5C7386CB5915E98D478EB97FEA89` and
contains zero rejected path/GUID occurrences.

The rebuilt loader, data, framework, and wasm SHA-256 values are exactly the
four signed values above; `index.html` also remains
`b9da71958fb5c463f1e146dc9904fcaf034ed47841f1d926dd6b652e22776091`.
The release theme still references only `sunny-orchard@1`, and protected
theme/A/scenes hashes are unchanged. Therefore the existing 85 canonical PNG
captures remain exact evidence for this byte-identical player and do not need
to be recaptured. Task 3.4 remains intentionally unchecked.
