# Rejected transparent screen-background captures

After removing the duplicate draw, the right-side crescent disappeared, but the
single full-screen art exposed an independent source-export defect. In the inset
ready, paused, detail, and terminal frames the top-left decorative ellipse leaves
a black crescent at roughly `x=45..120, y=100..112`; exact screenshot samples are
`(50,105)=#1D1B12`, `(80,105)=#201D14`, while `(200,105)=#F5DDAE` is the intended
continuous edge background.

The runtime PNG confirms the cause: its base pixel is `#F5DDAEFF`, while the
supposedly overlaid top ellipse center is `#FFE79F20` and the bottom-right ellipse
is `#71B84612`. The source exporter replaced the opaque base alpha instead of
source-over compositing the translucent decoration. These captures are rejected
regression evidence and must not be mixed with the accepted 4.6 matrix.
