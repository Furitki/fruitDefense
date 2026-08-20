# Rejected 7.4 plant-detail status capture

This directory preserves the complete first 7.4 editor, WebGL build, supported-size
Shell, and 402x874 cross-route run. It is regression evidence only and is not
canonical final acceptance.

The run used the ordinary-WebGL payload below:

- loader: `e0fd8292100517f441ce1e5ab1eb7a8219aa40919e46f125667090633a9b9388`
- data: `cc0a2b18a0c194bd7aafaff1e0b6b49247162799d54671ca892045612e938a60`
- framework: `628ded04a7af9570e4032627edcf6f973928ea5e8aa51c10f5ed97150760fc77`
- wasm: `868b0585638663a2ad55c2d2ba62dac4caee134ba340e01ebc53c841b25e084a`

Original-resolution review rejected the payload because the Battle plant-detail
transient status was clipped in both 402x874 geometries. The runtime copy is
`正在查看豌豆；拖动可移动或合成`, while the status card visibly stopped after
`正在查看豌豆；拖`.

Primary visual evidence:

- [full plant detail](cross-route/full-402x874-battle/14-plant-detail.png)
- [inset plant detail](cross-route/inset-402x874-44-34-battle/14-plant-detail.png)
- [full manifest](cross-route/full-402x874-battle/acceptance.json)
- [inset manifest](cross-route/inset-402x874-44-34-battle/acceptance.json)

The manifests' automated checks passed, demonstrating why the final manual
text-fit review remains required. No capture in this directory may be presented
as accepted 7.4 evidence. Runtime server stdout/stderr files are intentionally
removed under the task 7.3 cleanup rule.
