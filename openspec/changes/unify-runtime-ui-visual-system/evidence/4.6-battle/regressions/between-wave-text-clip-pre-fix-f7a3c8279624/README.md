# Superseded Battle acceptance: between-wave text clipping

These four real WebGL runs use payload `f7a3c8279624 / 3fe0d65d9975 / 74a8df0275f8 / ddd2f1f46254` and are retained only as failed regression evidence.

Manual review rejected every `03-between-wave.png`: the status copy `下一波倒计时 9 秒` and primary action `立即开始下一波` wrap inside their single-line content rectangles and are visibly clipped. The automated manifests report their state and pixel checks as accepted, but that result does not override the failed visual geometry review.

Do not cite these captures as canonical task 4.6 evidence. A fresh payload and four-run matrix are required after the shared text/presentation fix.
