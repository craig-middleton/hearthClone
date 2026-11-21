# HearthClone — current handoff

Updated 21 November 2026. **Overwrite this single file in every main commit**, in
the same commit as the work; see `AGENTS.md`. Start with `git log -1 --oneline`
and `git status --short`. This handoff describes the corner-integration checkpoint
containing it, following workflow commit163894f and mulligan checkpoint9e12621.

## Outcome and next step

The original Orgrimmar corner interactions are installed and verified in
`Assets/Scenes/HearthstoneTable.unity`: chimney, flag, gate, rock spire, zeppelin,
thorns and the complete catapult sequence. **Ready for Craig's visual/play test.**
Unity was left in clean Edit mode on that saved scene; temporary auto-tick is off.
Press Play, choose Play Game, confirm mulligan and try the board corners.

Craig last confirmed Ultra. Source integration is complete; High is appropriate
for the next routine visual-feedback/fix pass. Craig controls the setting: explain
and wait for his switch when needed. Preserve working mechanics; no new gameplay
systems requested. Give brief progress explanations and commit significant work
directly to main, updating this handoff in the same commit. Check remote state
rather than assuming this containing commit has already been pushed.

## What this checkpoint includes

- Six source FSMs restored alongside the existing chimney. Original clips animate
  existing board meshes/bones; no duplicate geometry or background overlays.
- Eight source particle systems and eleven original colliders merged by source
  path, preserving transforms, modules, curves and numeric layers. Native hierarchy
  remains intact. Original material paint/tints/blends use the authorized Ubuntu
  compatibility shader; original compiled shaders are not claimed identical.
- Eighteen exact SoundDef action bindings,48 original WAVs and original weights,
  volume/pitch ranges. Mappings recovered from original UnityFS references rather
  than filename guesses. Moving prop audio adapts its near radius to the elevated
  camera each frame, retaining3D position and source pitch.
- Catapult pickup, depth18 dragging, out-of-bounds cancellation, basket release,
  loading, firing, six6.25-degree bounce winding steps and seventh-click lock.
- Source clips, disabled action flags, gate thresholds, waits and completion
  semantics retained. See `Docs/Recovery/Corners-behavior.md` for provenance/limits.

## Verification actually performed

All passed through the live Unity Editor API:

- `VerifyNativeCorners` Main: actual projected collider hits/UI clearance, changed
  animation transforms, gate wiggle/close/open+dust, spire poke/rock+pebbles,
  flag click/idle, thorns poke/reaction+wood chips, audio and menu/restart.
  Observed larger-reaction counts: gate7, spire5, thorns9 (source range5–9).
- Its Zeppelin entry: idle/poke/major reaction, audio, completion and reset;
  observed threshold11 (source range6–11).
- Its Catapult entry: complete pickup/cancel/load/fire/wind/lock and menu/reset,
  using a disposable synthetic Mouse through actual Dynamic input updates.
- Regressions: `VerifyNativeChimney`, `VerifyNativeMulligan`, `VerifyNativeManaRack`,
  `VerifyNativeCardLayout`, `VerifyStartingDecks`, `VerifyNativeMatchControls`,
  `VerifyFrontEnd`. Both decks still total30 including opening hand/Coin;60 card
  instances distinct. End Turn flip/return, targeting and deck builder passed.
- Final console: zero errors and warnings. Native captures inspected. No standalone
  build, full automated match or speaker listening test claimed. Audio checks verify
  real clip playback and listener radius. Craig's subjective visual review remains.

The Catapult audio-radius fix was made after the other corner tests and verified
by the full Catapult check; controls/front-end were subsequently checked. No gameplay
rules changed. Test fixtures and synthetic input settings/devices were discarded.

## Important existing approvals

Native mana tray was approved in5d07762. Its recess belongs to the original
`STW_Frame`/`FRAME.png`; extra UI tray/flat crystals remain hidden. Crystals render
directly in the board. Do not restore RawImage/capture-camera overlays.
Chimney/hand centring5f42986 and original mulligan9e12621 remain intact. The
chimney's original ember material was null: its documented texture fallback is
still a limitation, distinct from the eight fully mapped new corner emitters.

## Working tree / portability

All intended corner code, selected assets, scene bindings, baked logic and evidence
belong to this checkpoint. The pre-existing unrelated modification
`Assets/_Project/RecoveredDecks/Deck_75_mesh.mat` was excluded and remains local.
Do not discard or casually commit it. Inspect git status for anything newer.

A GitHub clone needs **Git LFS content** and matching Unity to play; selected runtime
assets are included. A GitHub link does not grant an agent local Editor access.
Further extraction/recovery requires the external source folders/evidence below.
Do not assume another model has those files or can run tools.

Project: `/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone`
Editor: `/home/craig-middleton/Unity/Hub/Editor/6000.6.9f1/Editor/Unity` (Unity6.6).
Desktop: `/home/craig-middleton/Desktop/Hearthstone Card Database`,
`/home/craig-middleton/Desktop/Hearthstone Export`,
`/home/craig-middleton/Desktop/Hearthstone Extraction`.
Original client:
`/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone`.
Recovery: `/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/`,
including `ORG-native-source`, `Interface-native-source`,
`corner-source-normalized.json`, `corner-semantics-notes.md` and CIL captures.
Read client metadata only; do not execute/import original assemblies.

## Tools to resume or reproduce

Read `PROJECT_STATUS.md` for architecture/constraints and `PROJECT_HISTORY.md` for
previous decisions. Key implementation: `NativeCornerInteractions.cs`,
`NativeCornerLibrary.cs`, `Art/NativeCorners/CornerLogic.json` and baked
`NativeCorners.asset`. `SourceManifest.json` and
`Docs/Recovery/Corners-audio-source.json` preserve exact dependency provenance.

`Tools/PrepareNativeCornerEffects.py`, `AuditNativeCornerFsms.py`,
`PrepareNativeCornerAudio.py`, then `PrepareNativeCornerLogic.py` regenerate
selected data using external recovery/client sources. The installer reads committed
`CornerLogic.json` and selected assets, so reinstalling does not need those sources.
Run `InstallNativeCorners.cs` only in clean Edit mode. It preserves source geometry,
remaps native component references and records prefab GameObject-layer overrides.

Editor API (project open, automation package imported):

```sh
python3 Tools/editor_api.py get /api/editor_status
python3 Tools/editor_api.py 'script recompile_status'
python3 Tools/editor_api.py 'console get' '{"level":"error","tail":20}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeCorners.cs"}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeCorners.cs","entry":"Zeppelin"}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeCorners.cs","entry":"Catapult"}'
```

Checks require Play mode/auto-tick; run them sequentially. Script arguments use
`args`. Allow reload/Start to finish. API credentials stay in ignored `Library/`;
never print or commit them. Sandbox permission may need renewal for loopback/GitHub.
Stop temporary auto-tick and discard Play fixtures before handing back control.
