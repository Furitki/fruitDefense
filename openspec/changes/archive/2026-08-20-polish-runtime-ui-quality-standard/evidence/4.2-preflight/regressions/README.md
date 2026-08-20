# Retained acceptance-infrastructure regression

`shell-visual-402x874-full-transition-race-attempt1/` is the first preflight
attempt. The default and alternate Lobby images were valid, but the route left
Lobby before the script could prove the deliberately short Loading frame. The
script failed closed with `Lobby transition frame was not captured before route
change after 1 attempts`; no manifest was emitted and these files are not
canonical product evidence.

The accepted rerun is in
`../canonical/shell-visual-402x874-full/`. It used the existing bounded CDP CPU
throttle parameter at 20, proved that the screenshot remained on route 0, and
did not alter runtime transition timing.

