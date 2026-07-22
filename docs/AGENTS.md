# Documentation agent instructions

<!-- design-kb:start -->
## Design knowledge base

- Treat Markdown selected by docs/design-kb.config.json as the only authored knowledge source.
- Never edit the generated HTML declared by that config.
- Preserve front matter fields id, parent, order, and status, plus the config projectId.
- Rebuild with node tools/design-kb/build.mjs --root <repo-root> when publishing or refreshing the embedded static snapshot.
- Before handoff, run node tools/design-kb/validate.mjs --root <repo-root>.
- Keep edits within the user-requested documentation scope and preserve unrelated dirty worktree changes.
<!-- design-kb:end -->
