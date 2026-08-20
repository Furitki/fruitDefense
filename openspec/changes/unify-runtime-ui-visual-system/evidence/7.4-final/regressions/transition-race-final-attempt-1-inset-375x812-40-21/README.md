# Rejected transition capture attempt

This partial 375x812 inset ShellVisual run belongs to the final post-fix payload.
It captured the default and alternate Lobby frames, but the intentionally short
Lobby Loading state routed to Battle before the third screenshot could be
committed. No manifest was emitted.

This is a bounded acceptance-timing failure, not accepted evidence and not a
product visual result. The partial run is retained for audit history; its server
stdout/stderr files were removed under the task 7.3 cleanup rule. One serial
retry is permitted without changing runtime timing or the acceptance script.
