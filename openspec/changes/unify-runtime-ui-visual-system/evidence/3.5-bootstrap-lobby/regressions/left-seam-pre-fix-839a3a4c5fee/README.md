# Superseded left-inner nine-slice seam

The payload with data hash `839a3a4c5fee` was rejected after a one-device-pixel
left-inner partition leak was identified in the 360×800 inset Lobby selected
card. At `y=250`, `x=31..34` and `x=37..38` were `#FFCB44`, `x=36` was the
intended edge transition, but `x=35` incorrectly exposed the underlying
`#FFF6E0` base.

This is regression history, not release evidence. The complete partition fix was
rebuilt as payload data `761196808a41`; its accepted image and exact all-side
samples are recorded in the parent [README](../../README.md) and
[seam-probes.json](../../seam-probes.json).
