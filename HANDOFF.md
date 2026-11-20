# HearthClone — current handoff

Updated 20 November 2026. **Overwrite this single file in every main commit**, in
the same commit as the work. See `AGENTS.md`. Start with `git log -1 --oneline`
and `git status --short`. This commit establishes the handoff workflow. Last
verified game checkpoint: **9e12621**, native mulligan presentation.

## Current objective

Craig confirmed **Ultra** and authorized resuming the remaining original
Orgrimmar corner interactions. Explain progress briefly. Commit/push significant
work directly to `main`; no new gameplay systems requested. Craig controls effort
switches: stop at a useful checkpoint and wait if recommending High/Ultra.

Use original game geometry, paint, sounds, particle modules, transforms and
animation curves. Ubuntu compatibility rendering is permitted; do not claim it
identical to original compiled shaders. No full-board image overlays or invented
replacement motion. Preserve existing working mechanics and approved mana recess.

## Previously finished, tested and pushed

- **5d07762:** approved native mana recess fix. Tray already belongs to original
  `STW_Frame`/`FRAME.png`; extra UI tray/flat crystals hidden. Crystal meshes render
  directly in the board with source state curves and compatibility shader. Do not
  reintroduce RawImage/capture-camera crystal overlays.
- **5f42986:** original chimney smoke/click states and blacksmith sounds; near
  radius adapted for elevated camera. Player hand centred under portrait. Source
  ember material was null; its documented texture fallback is not an exact mapping.
- **9e12621:** original mulligan banner, blue confirm, keep/replace/X geometry and
  paint; larger cards, per-copy selection, full-viewport shade. Actual replacement,
  menu/restart,30-card and deck-builder checks passed at that checkpoint.

## Unfinished local work — not included in this documentation commit

A GitHub-only clone **will not contain this uncommitted draft**. Preserve it on
this laptop or explicitly transfer it before another agent continues elsewhere:

- `Assets/_Project/Scripts/UI/NativeCornerLibrary.cs`,
  `NativeCornerInteractions.cs` (+ metadata): source-FSM adapter draft.
- `Assets/_Project/Art/NativeCorners/`:8 original particle systems,11 colliders,
  source material/texture manifest and original WAVs; no duplicate board geometry.
- `Tools/AuditNativeCornerFsms.py` modified; new `PrepareNativeCornerLogic.py`,
  `PrepareNativeCornerEffects.py`, `PrepareNativeCornerAudio.py`,
  `InstallNativeCorners.cs`.
- `Docs/Recovery/Corners-audio-source.json`: exact UnityFS SoundDef-to-WAV
  mappings, hashes and weights, recovered from original references.
- Pre-existing unrelated modified `Assets/_Project/RecoveredDecks/Deck_75_mesh.mat`
  must be left alone unless specifically investigated.

Flag, gate, rock spire, zeppelin, thorns and catapult are **not installed,
playtested or committed yet**. A recompile was requested before the previous stop;
its current result has not been checked. No fresh Unity validation is claimed by
this documentation-workflow commit.

## Exact resume point

1. Check draft compilation and console in Unity. Read source notes below.
2. Fix unfinished installer audio contract. It expects nonexistent
   `NativeCorners/Audio-mapping.json` with short clip names/full machine keys.
   Actual producer writes `Docs/Recovery/Corners-audio-source.json`: `path` is full
   machine path, `machine` short name, `clips` full asset paths, `weights` explicit
   (including single-sound actions). Do not guess mappings by filename.
3. Review action completion, component-reference remapping, actual screen clicks,
   UI interception, native layers/trigger queries and clean menu/new-game reset.
4. Install only in clean Edit mode in `Assets/Scenes/HearthstoneTable.unity`.
   Copy colliders/particles onto existing source transforms, preserving original
   geometry/bones/camera and existing chimney; do not duplicate the hierarchy.
5. Verify real screen hits, changed animation bones, source particles/audio,
   repeated-click branches, catapult pickup/cancel/load/fire/seven-step winding,
   then menu/reset cleanup. Inspect captures; fix before claiming completion.
6. Run relevant regressions: native chimney/mulligan/mana/controls/card layout and
   starting deck totals. Discard Play-mode fixtures. Update handoff, commit intended
   files to main and push. Do not accidentally stage unrelated material changes.

## Recovered source semantics

- `PlayAnimation` blocks until legacy AnimationState disables (or ClampForever
  time exceeds length), then emits finish event and finishes the action.
- `AnimationPlaythroughAction` starts motion and **finishes immediately**. Source
  Wait actions govern state timing; do not replace that with clip-length waits.
- Mouse gates persist per action across re-entry. Integer max is exclusive:
  gate/spire/thorns5–9, zeppelin6–11, catapult winding7.
- Disabled source actions stay disabled: obsolete catapult X/Y FloatCompare .5,
  several spire stop actions and thorns State2 Wait.
- Winch RotateAdd(6.25,0,0), .25sec, Self space, ease24=**easeOutBounce**;
  incremental rotation deltas. SetPosition Space0=world,1=local. Mouse uses pixels;
  ScreenToWorldPoint depth18. ScreenPick empty mask uses default raycast layers;
  source trigger queries appear enabled. Test actual catapult raycast results.
- Eight emitters have exact recovered materials/textures; source RGB blends5/10
  (SrcAlpha/OneMinusSrcAlpha). Shader compatibility still needs integration.
- Sound mappings: Zepp State2 four pilot voices + ZeppelinReact_03; State3 three
  ZeppelinPoke. Thorns State2 ThornbushReact_01; State3 three ThornbushPoke.

## Local environment / dependencies not transferred by GitHub

Project: `/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone`

Editor: `/home/craig-middleton/Unity/Hub/Editor/6000.6.9f1/Editor/Unity` (Unity6.6).

Desktop folders: `/home/craig-middleton/Desktop/Hearthstone Card Database`,
`/home/craig-middleton/Desktop/Hearthstone Export`,
`/home/craig-middleton/Desktop/Hearthstone Extraction`.

Original installed client:
`/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone`.
Bundles `Data/Win`; assemblies `Hearthstone_Data/Managed`. Inspect metadata only;
do not execute original client assemblies.

External evidence directory:
`/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/`.
Important: `ORG-native-source/`, `Interface-native-source/`,
`corner-source-normalized.json`, `corner-runtime-data.json`,
`corner-semantics-notes.md`, `corner-*-il.txt`, and
`card-animation-audit/InspectManagedMetadata.exe`/Mono.Cecil.

## Unity control

Use `Tools/editor_api.py` from project root. It reads credentials itself from
ignored `Library/`; never print/copy them. Unity needs this project open and its
automation package imported. Sandbox network permission may need renewal for
loopback/GitHub. Last 20 Nov status attempt was socket-permission blocked, not proof
of a Unity error. Check status before mutation:

```sh
python3 Tools/editor_api.py get /api/editor_status
python3 Tools/editor_api.py 'script recompile_status'
python3 Tools/editor_api.py 'console get' '{"level":"error","tail":30}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeMulligan.cs"}'
```

Inspect each verification fixture; most require Play mode. Script parameters use
`args`, not `arguments`. Allow reload/Start to finish. Installers require clean
Edit mode. Use `capture game_view`/`output-path` for visuals. Stop temporary auto
tick at handoff and never save disposable Play-mode test fixtures into the scene.
