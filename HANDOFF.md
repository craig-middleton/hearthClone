# HearthClone — current checkpoint

Updated 23 November 2026. Codex project continuity. Read AGENTS.md, this file,
PROJECT_STATUS.md and relevant PROJECT_HISTORY.md entries before editing. Inspect
Git status first. Run git log -1 to identify this containing checkpoint. Recovery
baseline is bd24473 (the final Codex typography/mulligan commit of22November).

## Recovery completed

Craig reported that the external-model session was a disaster and requested its
code changes and project setup removed. No new commits had been made after
bd24473. Restored all20 modified code/shader/tool files together to that baseline.
Gameplay code, shaders, scene, prefab and saved presentation assets now match
bd24473. No experimental pieces were retained.

Deleted project additions: NativeMinionSummonLibrary.cs, NativeInPlayMinion.shader,
native-card AudioManifest, copied NativeCards/Audio and NativeMinions/Audio folders,
and their metadata. Original Desktop source audio remains untouched. Removed the
alternate-model project instructions and setup references. Do not recreate them
unless Craig explicitly asks. Keep this single rolling HANDOFF for Codex continuity.

The uncommitted recovery is not visible as a gameplay diff in this commit because
those failed experiments had never been committed. Git now records their verified
removal and the setup/documentation cleanup. No destructive branch/history reset.

## Current Editor / verification

Unity6000.6.9f1 is open normally via Hub on Assets/Scenes/HearthstoneTable.unity.
Stopped in clean Edit, temporary auto-tick off; gameplay fixtures discarded. Fresh
C#/shader compile passes. Final Console zero errors/warnings. No ongoing edit jobs.

Fresh recovery checks passed all11 suites:
- VerifyNativeMenu: original intro, menu model/camera/slots/raycast/lifecycle.
- VerifyFrontEnd: actual deck clicks, copy/size limits, save/reload, exact composition.
- VerifyNativeMulligan: source marks/layout, per-copy replacement and reset.
- VerifyNativeCards: values/art/text/type rebinding, input, hover/ghost/concealment.
- VerifyNativeCardLayout: both ten-card hover bounds and body/school separation.
- VerifyNativeMinions: original models, readiness, gems/taunt/input/held lifecycle.
- VerifyNativeHandOutlines: mana/turn/space eligibility and non-hand suppression.
- VerifyNativeSpells: real Fireball/Frostbolt casts, sound/render/capture/tails/cleanup.
- VerifyStartingDecks:30total per player including opening hands/Coin, distinct copies.
- VerifyNativeMatchControls: targeting/cancel, actual End Turn and return from effects.
- VerifyNativeManaRack: approved native recess/models, spend/refill and restart.

Inspected actual Game-view capture Docs/Previews/Recovery-verified-2026-11-23.png.
No standalone build, full manual match, physical speaker listening or new visual
approval claimed. This is recovery to the previous implementation, not new polish.

## Preserved baseline behaviour and important constraints

- Original native board geometry; no full-board background overlay. Approved mana
  recess is source STW_Frame/FRAME with resource meshes directly in the board.
- Native TheBox menu: original242transform/68renderer hierarchy, camera/state poses,
  safely evaluated original4.983s camera intro, Play/Choose Deck source slots. Menu
  layer30 camera Untagged, named TheBoxCamera. Main Camera remains board camera.
  NativeMenuPresentation.Slot.sourcePoints are baked in Edit because original GPU
  meshes are not readable in Play. Never regress to runtime mesh.vertices reads.
  Unsupported glow submeshes are transparent; keep shared opaque centre disk enabled.
- Mulligan: source StartingHandZone spacing/card scale/badge/banner wrapper, centred
  to actual three-card local hand without a phantom Coin slot. Static compatibility
  compositionOffset(0,75), Confirm(0,-155),170x85, shade.4. Original source card
  geometry and selection remain. Original hero intro/deal sequences not installed.
- SourceMaterialProperties reads preserved serialized material values: unsupported
  original shader getters can return zero. Compatibility rendering is authorized.
- Played minions: original532vertex oval actor, portrait/frame/gems/shadow/settled
  taunt and source-shaped green readiness. Source states4/5/6 use green, not yellow.
- Hand: original Ally/Ability silhouette textures and saved green parameters.
  Explicit owner/panel binding. Concealed/ghost/collection/mulligan hand glows off;
  kept mulligan cards use independent original selectable silhouette copies.
- Cards: source curved-name geometry/UVs with recovered Belwe title raster, consistent
  .82-unit ink height and.14outline (custom raster-fit choices, not recovered metrics).
  Spell body boxes use upper parchment (+6width/+8height/up4) without moving lower
  edge/school. Targeted Tools/RefreshNativeCardNames.cs avoids full card reinstall
  replacing installed outline children. NativeCardFace.Bind must mark material AND
  vertices dirty so CanvasRenderer updates changed name/art textures.
- Fireball/Frostbolt: original prefabs/particle modules/sounds/source.4s easeInQuad,
  viewport camera/timing/flash/light curves and independent tails.58-record manifest.
  Exact portraits also wired for Arcane Explosion/Flamestrike; only the first TWO
  have newly installed original VFX. Remaining spells keep previous presentation.
- Powers/death/corners/draw remain earlier original-source integrations. NEVER use
  AnimationClip.SampleAnimation on mixed exported actor bindings: native crash.
  Evaluate safely baked source channels. Do not launch Unity from a restricted shell;
  use Hub (previous preferences/GI-cache failure). Never execute original client code.

## Remaining work / next action

Craig rejected earlier menu/mulligan visuals; subsequent corrections remain without
his final visual approval. Mechanics work but this is not an exact visual replica.
Priorities: source material/lighting fidelity, one representative minion/spell card
through hand/hover/play, card typography, hero/banner/modal composition, source VFX
shader behaviour and remaining original sequences. Inspect actual Game-view output;
functional tests do not establish visual correctness.

Compatibility gaps: original gloss/reflections, animated highlight motes, taunt birth
spring, spell electrical/noise/scrolling flame/AlphaClipBloom, exact bloom/distortion/
shake. Spell root Sparks/Smoke share capture instead of separate original world
trails. Menu compatibility unlit/multiply hides unsupported scrolling/glow; original
lid clips retained but not played and menu sounds not wired. Hero-death erosion/
distortion/global shake remain incomplete. No invented tweens/shimmer.

Craig selected High for recovery; sufficient. He controls effort. If later source
rendering reconstruction needs Ultra, explain and stop at a safe checkpoint until
he switches. Give brief progress explanations. Significant work goes directly to
main with this file overwritten in the SAME commit, then push and verify.

## Tools and files

Project root:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone
Remote: https://github.com/craig-middleton/hearthClone.git, main, Git LFS.
Unity: /home/craig-middleton/Unity/Hub/Editor/6000.6.9f1/Editor/Unity

Tools/editor_api.py uses authenticated local Unity API and privately reads ignored
credentials. Never print/commit them. Run installers in clean Edit. Recompile new
runtime types before dynamic installer scripts. Temporary previews are disposable.
PreviewNativeSpell pauses Time.timeScale; run its Resume entry or stop Play after.

python3 Tools/editor_api.py get /api/editor_status
python3 Tools/editor_api.py 'script recompile_status'
python3 Tools/editor_api.py 'editor.playmode start'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":true,"interval-ms":16,"persist":false}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeMenu.cs"}'
python3 Tools/editor_api.py 'capture game_view' '{"output-path":"Docs/Previews/review.png","source":"screen"}'
python3 Tools/editor_api.py 'editor.playmode stop'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":false,"persist":false}'

For isolated menu captures source=camera,camera=TheBoxCamera; default Camera.main
captures board instead. On fresh launch wait for original intro before pointer
frontend tests. Prefer actual source=screen capture for visual review.

## External source assets and unrelated files

Recovery root:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery
Contains Interface-native-source, ORG-native-source, Box-complete-source and source
CIL/evidence files. Use complete menu export, not Box-native-source. Provenance in
NativeMenu/SourceManifest+SourceConfiguration, Docs/Recovery/Mulligan-layout-source,
Docs/Recovery/Spells-source.json. Source inspection is metadata only.

Desktop source folders:
/home/craig-middleton/Desktop/Hearthstone Card Database
/home/craig-middleton/Desktop/Hearthstone Export
/home/craig-middleton/Desktop/Hearthstone Extraction
Original installed client:
/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone

Unrelated pre-existing local files preserved byte-for-byte and excluded from commit:
Assets/_Project/RecoveredDecks/Deck_75_mesh.mat (tracked edit), Assets/_Recovery.meta,
Docs/Previews/Native-hero-powers-first.png (early rejected proof). Do not discard them.
Selected runtime assets are tracked; archives stay external. Fresh clones need LFS
and do not automatically provide external exports or access to Craig's Editor.
