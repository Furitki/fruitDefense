# Home 参考图分布、拆分与固定来源

## 输入锁定

| 角色 | 路径 | 像素尺寸 | SHA-256 |
| --- | --- | ---: | --- |
| P0 参考图 | `docs/ui/mockups/outgame-hub-concepts/home.png` | 852×1846 | `873F6EFBF1B060A8992DB4D7AFAC7EB0E73FF3C8223BE5CA79988620309C9AEE` |
| 本轮不可变改前 | `evidence/home-reference-restoration/before/01-hub-home-user-rejected.png` | 402×874 | `E9AE2E9D053BC45821802672C7B4EB86364D029CB9764179EFBA74EBF43E63F8` |

参考图按 402×874 逻辑画布分析；原始生成栅格的横向倍率约为 2.1194，纵向倍率约为 2.1121。运行时仍以 `PortraitHubLayout` 为唯一几何权威，不以原图像素坐标建立点击区。

## 402×874 页面分布

| 组件 | 逻辑 Rect `(x, y, w, h)` | 页面占高 | 视觉职责 |
| --- | ---: | ---: | --- |
| 顶部栏 | `(7, 15, 388, 80)` | 9.2% | 页面名与已提交余额 |
| 主纸张 | `(11, 103, 386, 690)` | 78.9% | Home 内容宿主 |
| 关卡 1 | `(28, 122, 350, 132)` | 15.1% | 唯一选中卡；左图右文 |
| 关卡 2 | `(27, 267, 351, 124)` | 14.2% | 普通可选卡 |
| 关卡 3 | `(27, 404, 351, 124)` | 14.2% | 普通可选卡 |
| 战前成长 | `(24, 555, 354, 221)` | 25.3% | 真实成长投影与 Start |
| Start | `(57, 700, 289, 56)` | 6.4% | 页面唯一 Primary |
| 底部导航 | `(0, 794, 402, 80)` | 9.2% | 连续底图、单个选中纸签、三组 icon+label |

纵向节奏为：顶部栏到主纸张约 8px；关卡卡片间约 13–14px；第三关到成长区约 27px；导航与画布底边齐平。三张关卡图窗约占卡片宽度 39%，正文占约 55%，其余为间距和状态标记。

## 已授权的完整组件拆分

拆分只使用 `ffmpeg crop` 复制参考图中的完整、无文字图片窗；没有调色、描边、补像素、画遮罩或合成阴影。裁剪格式为源图像素 `(x, y, w, h)`。

| 语义槽 | 参考裁剪 | 固定组件路径 | SHA-256 | 生产变换 |
| --- | ---: | --- | --- | --- |
| `illustration.lobby-orchard-01` | `(84, 280, 282, 230)` | `reference-splits/level-01-window.png` | `6615DFF9585E1A5458B15DB15AAD7114210288BF5B1B08FF6664B531B3E488FC` | 完整组件裁剪 → alpha-safe resize 272×216 → 136×108 |
| `illustration.lobby-orchard-02` | `(79, 582, 282, 214)` | `reference-splits/level-02-window.png` | `4AD8897F90080D8C96ACF9AE672487A3E8F9F1D0F346556BBB7E95FCBC25911C` | 完整组件裁剪 → alpha-safe resize 272×216 → 136×108 |
| `illustration.lobby-orchard-03` | `(79, 870, 282, 218)` | `reference-splits/level-03-window.png` | `D6C594E908FB969F60650039D36FF7D3966FE48F2A18A2BC5F83407608189DA4` | 完整组件裁剪 → alpha-safe resize 272×216 → 136×108 |

三个图片窗已经自带参考图的圆角构图。Home 直接把它们绘制在 `cardLayout.Thumbnail` 内，不再覆盖 `surface.illustration-frame`。因此不存在“方形插画铺满 + 透明外角框盖住”的泄漏链路。

## 参考派生的独立固定组件

下列组件先从参考局部裁剪中单独指定目标，再由内置 ImageGen 只做单组件提取或内容移除；脚本只校验哈希、保留同返图 alpha、裁剪完整组件、透明 padding、alpha-safe resize、清理低 alpha fringe、测量和编码。

| 语义槽 | 固定输出 | 输出 SHA-256 | 禁止残留 |
| --- | --- | --- | --- |
| `surface.card-selectable` | `imagegen/surface-card-selectable-reference-derived.png` | `25BD5ADE71CBF98F38B16B48222011C5CE1FC5CE74AD8BA88390CD96D94C3784` | 插画、文字、星级、勾选缎带、黑边 |
| `surface.hub-navigation-base` | `imagegen/surface-hub-navigation-base-reference-derived.png` | `FC9112C52E5B9B3C27439B89330BCE6E32FB8808D0C2B0F423B219AD561AE9BA` | 选中纸签、图标、中文、果叶角饰 |
| `surface.hub-navigation-selected-tab` | `imagegen/surface-hub-navigation-selected-tab-reference-derived.png` | `CC24CFCE88CA9C5BCDA5D73C14638A049183378193198A500328987498AFF3D5` | 图标、中文、下划线、外部装饰 |
| `icon.hub-home` | `imagegen/icon-hub-home-reference-derived.png` | `8A8A7D7245DD6CD96355EAC96437EC4E08DCE6214AB11E9CEF39AC46361990F6` | 页面背景、中文、叶片、第二主体 |
| `icon.hub-activity` | `imagegen/icon-hub-activity-reference-derived-v3.png` | `352483D4CAC6ECD5ED3A3D7027B6D890D0F43BE55854E2FCC5F0D3B945671A73` | 页面背景、中文、奖励装饰、微型日期格、轮廓式碎线；同返图中性底仅用于 exterior/star alpha |
| `icon.hub-growth` | `imagegen/icon-hub-growth-reference-derived-v2.png` | `D0C8D105469C268A02E29C8ED09DF13B34E35AF43DF8699797DF841D9B5CEE60` | 页面背景、中文、土堆、果实、额外叶片、叶脉 |

## 本轮溢出根因与关闭方式

改前把 168×108 方形外接图缩放到 136×102 的完整 `Frame`，随后覆盖一个外角透明、中心开孔的厚九宫格框。插画仍占满框的外接矩形，所以会从框的透明四角和左下边露出；这不是 Rect 越界，而是错误的可见像素分层。

关闭方式只有一条生产路径：参考图片窗 → 136×108 runtime export → `cardLayout.Thumbnail`。Home 不再绘制共享插画框，不增加 mask、Shader、Canvas、primitive 或 fallback。

## 本轮真实画布与三栏证据

| 证据 | 路径 | 像素尺寸 | SHA-256 |
| --- | --- | ---: | --- |
| WebGL 改后 | `webgl-402x874/01-hub-home.png` | 402×874 | `2C32B2442E8E5A4BD0DD66C16C8FD1BA413F38317DEE5135854CC304037A0153` |
| 参考图 / 改前 / 改后 | `home-reference-before-after.png` | 1206×934 | `D9307420E74567CCD1D1221D303DEBB3C4F5DAFBD4B966C9682216A7E2459E7E` |

技术门禁结果：release visual validator `Valid (0 warning(s))`；Hub component smoke `RUNTIME_UI_HUB_COMPONENTS_OK`；Hub pages runtime smoke `FRUIT_DEFENSE_HUB_PAGES_RUNTIME_OK`；ordinary WebGL build `FRUIT_DEFENSE_ACCEPTANCE_WEB_BUILD_OK`；真实画布采集 `FRUIT_DEFENSE_HUB_VISUAL_OK`。三栏图只用于评审，缩放、标题和拼版像素不进入任何生产 ArtSet 或运行时依赖。
