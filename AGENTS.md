# HearthClone agent instructions

Read `HANDOFF.md` first, then `PROJECT_STATUS.md` and relevant entries in
`PROJECT_HISTORY.md`. Inspect `git status` before editing; preserve existing work.
These instructions apply throughout this repository. Current user instructions
take precedence over older project notes.

## Rolling handoff required for every main commit

- Before **every commit to `main`**, overwrite the single root `HANDOFF.md` and
  include it in the **same commit**. Do not create dated/numbered handoff copies
  or a second commit solely to record the first commit's hash.
- Describe the checkpoint: completed work, actual verification and its limits,
  remaining work, known issues, and concrete next steps/files.
- Distinguish committed work from local uncommitted files and external assets or
  recovery evidence. A GitHub-only agent does not automatically have those files
  or access to Craig's Unity Editor. Never claim untested work is complete.
- Use `git log -1` to identify the containing checkpoint; the document cannot
  embed its own final commit hash. Record an older verified baseline when useful.
  Do not claim a push succeeded until verified.
- Keep `HANDOFF.md` current. Durable architecture belongs in `PROJECT_STATUS.md`;
  decision history belongs in `PROJECT_HISTORY.md`.

## Project preferences

- Work continues in Codex. Do not recreate alternate-model handover configuration unless Craig asks.

- Significant work goes directly to `main` and GitHub, as authorized by Craig.
  Stage intended files only; never discard unrelated edits or commit credentials.
- Use recovered original Hearthstone assets, sounds, animation curves, transforms
  and effects. Compatibility rendering is authorized for Ubuntu; disclose limits.
  Do not invent replacement motion or reintroduce full-board image overlays.
- Preserve approved native mana recess, working mechanics and source geometry.
- Give brief progress explanations. Pause promptly when Craig asks to stop.
- Craig controls High/Ultra. At a useful switch point, explain the recommendation
  and wait for him to change it; an agent cannot change that setting itself.
