# Rejected duplicate-background captures

These full and inset captures use the first terminal-preview payload, but Battle
painted `surface.screen-background` twice: once in identity screen space and once
after applying the inset design matrix. The inset run exposes a dark green/black
right-side crescent near `x=382, y=600..780`. The payload is rejected and these
images are regression history, not task 4.6 evidence.

