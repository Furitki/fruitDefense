# 5.3 desktop WebGL host acceptance

Status: **pass**.

This is the latest identity after the Foundation 5.4–5.8 runtime/layout work.
The final build log is [`final-webbuild.log`](final-webbuild.log) and the matrix
was regenerated from that exact `Builds/WebGL` output.

The stable [`scripts/accept-webgl-host.ps1`](../../../../../scripts/accept-webgl-host.ps1)
starts the ordinary WebGL build on a random port and uses a disposable desktop
Chrome profile for each matrix entry. The complete machine result is
[`matrix/webgl-host-acceptance.json`](matrix/webgl-host-acceptance.json).

## One payload, three host sizes

All three browsers loaded the same versioned ordinary WebGL payload:

| Role | Version | SHA-256 |
| --- | --- | --- |
| loader | `cfaa2d82d6d0` | `cfaa2d82d6d07c12674952310a75b305ecbb1bc55f3c302f8e29c114c5c5dc76` |
| data | `7fa3ef3d7c43` | `7fa3ef3d7c43fa535fef4ea935c85866fbbffbe36198cd54256302d97431be56` |
| framework | `0c5ecd20fc1c` | `0c5ecd20fc1c192495e6c368f0642cb5cb2937296cafae6130224ea9262081e6` |
| wasm | `b0689f427953` | `b0689f4279535d61f913e108ef6b61b8bd071a066856df94ef74c20ee0113c4e` |

## DOM and input results

| Viewport | Scale | DOM canvas rect | Document | Lobby input proof |
| --- | ---: | --- | --- | --- |
| `1280×720` | `0.8237986270` | `(474.421875,0,331.15625,720)` | `1280×720`, scroll `0,0` | logical `(201,406)` → client `(640,334.4622)` → `orchard-02` |
| `1440×900` | `1.0297482838` | `(513.015625,0,413.953125,900)` | `1440×900`, scroll `0,0` | logical `(201,406)` → client `(719.9922,418.0778)` → `orchard-02` |
| `1024×640` | `0.7322654462` | `(364.8125,0,294.359375,640)` | `1024×640`, scroll `0,0` | logical `(201,406)` → client `(511.9922,297.2998)` → `orchard-02` |

For every case:

- the canvas is fully inside the viewport and centered;
- horizontal and vertical scale are the same;
- the backing size remains `402×874`;
- document scroll offsets are zero and document dimensions do not exceed the
  viewport by more than `0.5` CSS px;
- all four expected versioned payload resources are present in browser resource
  timing;
- canvas-relative client/logical coordinate round-trip error is exactly `0`;
- the real click remains on Lobby route `0` and changes the selected level from
  `orchard-01` to `orchard-02`.

## Original-resolution review

- [`1280×720`](matrix/1280x720-lobby-orchard-02.png)
- [`1440×900`](matrix/1440x900-lobby-orchard-02.png)
- [`1024×640`](matrix/1024x640-lobby-orchard-02.png)

All three original screenshots were inspected: the whole portrait surface,
including top and bottom ornaments, is visible; the letterbox is centered; no
Unity footer or scroll-dependent region remains. The three complete card titles
(`第一关・U形教学`, `第二关・S形覆盖`, and `第三关・核心走廊`) are visible without
right-edge truncation at every desktop host size. This is a host acceptance,
not an aesthetic score.

The script cleans its owned Chrome processes, profiles, random-port server, and
server logs. It does not stop or reconfigure port 4173.
