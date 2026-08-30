# Eight-point UI polish evidence

## New candidate Gate A

- Review comparison and automated evidence:
  [gate-a-8-point/README.md](gate-a-8-point/README.md).
- Current candidate identity:
  `ui.sunny-orchard@6 / sunny-orchard-painted@5`.
- Full and representative inset ordinary-WebGL visual acceptance passed.
- User scoring is still required before calling the candidate `8/10` or
  synchronizing the stable visual document.

## Approved 7/10 baseline

- Runtime identity: `ui.sunny-orchard@5 / sunny-orchard-painted@4`.
- User review: procedural buttons `3/10`; first ImageGen visual `8.5/10`;
  production solid-background extraction `7.5/10`; composed page approximately
  `7/10` and explicitly approved.
- Non-blocking direction carried into this change: the current green is darker
  and less fresh than the supplied reference; page, card, and container material
  integration should move toward the 8-point target.
- Approved ready frame:
  [01-ready-approved-7.png](before-approved-7/01-ready-approved-7.png), SHA-256
  `62E3CB27F823965F384773A7E5089459957F6C0660F3CDC0C3B4CA99AB811BD2`.
- Approved side-by-side:
  [comparison-reference-approved-7.png](before-approved-7/comparison-reference-approved-7.png),
  SHA-256 `5C7523E938CF6BE452A053908503EA6D91BADE7ECB5F1860A3B78A5F37DA9EDA`.
- Approved generated action sheet SHA-256:
  `7F06E07A56F943B9FD2178F45678AD5C6211C5A7A389B86155B04413F218FC56`.
- Baseline content-region contrast: primary `5.533:1`, secondary `9.1791:1`,
  danger `5.8392:1` against warm-white content.

The copied frames are evidence only. Production assets do not reference this
change directory or the supplied full-page reference.

## Selected production inventory before replacement

All selected runtime destinations use stable standalone FullRect Sprite paths.
Action slots are fixed crops from the approved sheet; the eight structural
slots still use the procedural reference-material kit and are the deletion
targets for this increment.

| Semantic slot | Runtime GUID | Source SHA-256 | Runtime SHA-256 | Before authoring contract |
| --- | --- | --- | --- | --- |
| `surface.safe-area` | `82f763a9fcef976d97dbd5f1934a9c9d` | `314BBFE60930F61613DB6CEA7C0F1E6E5DFA5A3376F7782C697A79D222DB9DFF` | `CB7D200675A1B972A470A50E14D2A3B23B8C76FF3962D43529386C0DF7AFC5C3` | `deterministic-reference-material-kit / warm-paper-page` |
| `surface.panel-standard` | `4b7cae1cf3370e8ef6224428d92c471e` | `A6998F1C22F417725F1AE27E68F7272DE09CD0C21531AB73879057F1D2CB8747` | `14D3275E5A3724D2EC79360C731888C93BFB2D08BA12D19A152231B22E4C92C2` | `deterministic-reference-material-kit / warm-paper-panel` |
| `surface.panel-raised` | `ca6d054466a7f7e47e9d08f61f3c1fc8` | `801DF4AAAB877ACC8FF395E49F3565A6AE9FC2D932F5CF363C0022D970A07D08` | `1ED8E6FE5D701C046FC1A8BCE984E876E8D885B46EEA88D1C89AAC64B1D5C60B` | `deterministic-reference-material-kit / raised-warm-paper-panel` |
| `surface.card-selectable` | `bb8b22a7c34fae3ac4757a58c64b776f` | `2B3464053CD8CA954B4E587FDE085E044E2962D2B91D45549A58A8A25C118466` | `544970B37A74685C9507542B40A0D75FBCAF3751D489D31C45F6C190C98631AD` | `deterministic-reference-material-kit / sunlit-selectable-paper` |
| `surface.metric` | `115def71075d5a3d3e3ab7727835dae6` | `26BE0D00C213DE22AEEEE0CC3A73FBF58923B069D6CFCDA1820749593C44E730` | `AD53D559C84A64F11BE5661A564D26B9639D2B5C5A9881094F17ADBC6BCB297B` | `deterministic-reference-material-kit / raised-metric-capsule` |
| `action.primary` | `f3a4968a2afcfb59d13ed53ec5977f60` | `AA16B6F1B095BFE8F67098E35B555B3F401E89F6F188B7B75FDDB5716878A90B` | `7E7A7556E2AB682496F745E1B0AB3924BA47C0CD11CD85A697F5B5A97480CBE4` | `imagegen-sheet-crop` |
| `action.secondary` | `248c291ce629562b508210a448a9834a` | `FAA9F954F1E65970AEA91AB44D3539F41F69C9600CA8376254BD6493C9B9B918` | `A56FC4ACA3155EAC01494CFB86377EE316B92050925A45EBF8BAEECD3B5773B5` | `imagegen-sheet-crop` |
| `action.quiet` | `5f69d95b0cb4c6769fdf02de793731da` | `9AFA8A240E250B14388117DAF3E823B6C442C582E03A9803C307D18342F83212` | `C0B12CA0C9720CFF0D0A69111B156857A35CC21C7192BD2AD72FD8B0B0FE34B8` | `imagegen-sheet-crop` |
| `action.danger` | `1fc23fa236f1a4ed0dce0fe174f6a1a3` | `A7053032247A0C2EEBA4F7ABF398415A3890CF234883C9A3081E4CBF6E7A081A` | `6FE71EBDAC5F46CD4ADDE02BE1EC2D2CEAED9B43D9802A5858F24F5E03997C60` | `imagegen-sheet-crop` |
| `slot.tool` | `d622e8316c6b878ee9b1c57cfcb737e2` | `8D8FA4113C009949D40555267C79E5DF27BC6D63E589629BD4D5E6480C52CBFA` | `028F05D5F39A4FA37E778861248A4FEA005BC9546D3F1CF930FF703B4AE7D8FD` | `deterministic-reference-material-kit / recipe-tool-card` |
| `slot.nursery` | `e1dc825dd016d7387ea066f85be08ab3` | `9B49E33FF53B2C0FE6465A397F87563BDDDFD261A2B945BA5B503FE50CD420EC` | `D789DF8DE08BBB89036169490293AE5A702EEF977C708943BD39D5D007EB537A` | `deterministic-reference-material-kit / dashed-nursery-slot` |
| `action.compact-control` | `7dfb0b5af19df10465572bee07dd6df4` | `228B367C80E54D8F3154ED0E503A00540CD1021FB85F8763DEE3EB5F9A388436` | `0CA4B39B376CAC38B01E31DC17DCC6AD86C68061E98AF24768C2EBE829864C39` | `imagegen-sheet-crop` |
| `action.compact-control-active` | `de20db00bda29fa42c8ed9ae4424dcd2` | `260FE6D4EEAB835233AE4DB96D478F7C52026BB5A36E50786D73DEAA65E20277` | `EFB77D0CFA4216580DBFB04233156F39F1B61AA329D0BE9EFECD690C769BBF0B` | `imagegen-sheet-crop` |
| `surface.gameplay-stage` | `22874c05e340198be7942216980453c2` | `9E1CB8B2DE38602FD5E873C8FB224BFE1C6BCB70685A9D2EBF9039D348D732B3` | `D81ECC0672A5A93B91B684960DA6BC72632B38E0C541C0F7CFB95132667C8E3B` | `deterministic-reference-material-kit / cream-rimmed-soil-stage-frame` |

The first thirteen slots use 32px slice borders and 20px safe insets at the
256px source scale; `surface.gameplay-stage` uses a 20px slice/safe inset and a
transparent center. These geometry contracts must remain unchanged.
