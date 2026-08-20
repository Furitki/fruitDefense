# Superseded ShellVisual transition race

The first 360×800 inset ShellVisual run initialized and captured the Lobby, but
the short transition completed before the single bounded screenshot attempt.
The script rejected the run because the post-capture route was no longer Lobby.

This is retained as acceptance-timing history, not canonical evidence. The same
unchanged WebGL payload is retried serially without changing runtime transition
timing or adding a capture hook.
