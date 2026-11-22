# HearthClone shared agent instructions

Read `AGENTS.md`, then `HANDOFF.md` before editing. These are the shared project
instructions and current stopping point for switching between Gemini and Codex.
Read relevant `PROJECT_STATUS.md` constraints and `PROJECT_HISTORY.md` entries.
Current user instructions take precedence over older notes.

Use this existing local working copy. Check `git status` and the open Unity project
before changing files. Preserve unrelated edits and recovery copies. The selected
original assets are already in the project. Larger source exports and the installed
client remain at the absolute paths in `HANDOFF.md`.

Use the existing `Tools/editor_api.py` authenticated local Unity API. Its helper
reads ignored credentials itself. Keep credentials private. Launch Unity through
Unity Hub if needed. Read the current scene/Play/compile state before mutations.

Update the single root `HANDOFF.md` in the same commit as every main commit.
Include what passed, what remains unverified, next commands, and external file
locations. Push checkpoints to main as Craig authorized. At a model handover,
finish at a safe boundary and stop active implementation tools so the next agent
can inspect the same working copy. Craig may choose to pause before completion.
