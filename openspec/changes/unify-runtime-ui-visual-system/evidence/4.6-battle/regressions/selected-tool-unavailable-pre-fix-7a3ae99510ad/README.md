# Rejected selected-tool captures

This opaque-background payload fixed both background crescents and otherwise
passed the Battle matrix, but its selected-tool step reused the
`selection-inspection` acceptance state where Gatling inventory was zero. The
real click was correctly rejected by gameplay, and the entire first-tool card
region was pixel-identical before/after. These four runs are retained as failed
regression history and are not accepted task 4.6 evidence.

The replacement matrix uses the URL-guarded `selected-tool` acceptance state,
captures the available tool before input, performs a real click, and rejects an
unchanged selected-state hash.
