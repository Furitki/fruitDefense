## 1. Local Build Pipeline

- [x] 1.1 Add shared PowerShell helpers for Unity/version/module preflight, named locking, batch execution, Git metadata, sizes, hashes, and JSON evidence
- [x] 1.2 Add the `Web`/`PC`/`All` local build entry with one P0 gate, sequential target builds, stable markers, and a local manifest

## 2. Online Publication Pipeline

- [x] 2.1 Add a default plan-only online entry with explicit `-Execute`, expected-branch, clean-tree, and SSH-key gates
- [x] 2.2 Bind publication to a matching local Web manifest and delegate authorized transport/acceptance to `deploy.ps1 -SkipBuild`
- [x] 2.3 Record successful online publication evidence without changing mini-game readiness or publishing during this implementation

## 3. Operator Documentation

- [x] 3.1 Add pipeline commands, artifacts, safety behavior, and CI reuse guidance to a dedicated operator document and route README to it

## 4. Validation

- [x] 4.1 Parse-check the PowerShell entry points, run strict OpenSpec validation, and verify the online plan mode performs no publish action
- [x] 4.2 Execute the local `All` pipeline to prove P0, Web, PC, logs, hashes, and manifest generation end to end
- [x] 4.3 Verify final task status, tracked scope, preserved unrelated changes, and unchanged release/design documents
