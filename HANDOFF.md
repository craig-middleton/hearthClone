# HearthClone — current handoff

Updated 22 November 2026 for immediate local handover to Gemini. This is the single
rolling handoff shared by Codex and Gemini. Overwrite it in every main commit.
Read AGENTS.md, GEMINI.md, git log -1 and git status before editing. The containing
commit is a WIP handover checkpoint following verified hero-death 98d3481 and
hero-power 37c4ca4. Run git log -1 to identify this commit without a self-hash cycle.

## User objective and working preferences

Continue the existing Unity card game using Craig's supplied original assets.
Current requested work is original hero powers, shattering hero deaths, real spell
animations/sounds, better hand/played-minion graphics, and now an original-asset
intro/menu plus a corrected mulligan. Preserve the existing working mechanics,
30-card deck totals and approved board/mana/corner work.

Craig last chose Ultra. Recommend Carbon for the next source-integration pass in
Gemini. Neon High is suitable for later routine fixes/visual tuning. Craig controls
model/effort settings. Explain meaningful progress briefly. Stop promptly if asked.
Significant work goes directly to main and GitHub. Include this updated handoff in
the same commit. Use original models, textures, fonts, sounds and animation data.
Compatibility rendering is authorized. Record rendering gaps honestly and keep
added motion grounded in recovered clips/curves/source behavior.

## Immediate state at handover

All implementation agents were stopped at safe file boundaries for this handover.
Unity is open normally through Hub on Assets/Scenes/HearthstoneTable.unity, clean
Edit mode, with temporary auto-tick disabled. Fresh recompile completed successfully.
Fresh console reports zero errors/warnings. Current code compiles, but the WIP
features below still require Play tests and visual review. No new feature work was
performed after the handover request.

This commit includes the previously local pending minion/hand/spell code and selected
assets, rather than leaving another agent dependent on an untracked draft. Their
status is explicit below. Large source archives stay local at the listed paths.
The pre-existing unrelated RecoveredDecks/Deck_75_mesh.mat change and recovery
metadata Assets/_Recovery.meta remain excluded. The superseded local
Docs/Previews/Native-hero-powers-first.png is an early grey-shadow proof, not an
approved reference. Inspect git status for newer work before staging anything.

## Finished and verified

- Corner2e92890, hero powers37c4ca4 and hero death98d3481 are pushed checkpoints.
- Mana recess5d07762 is approved. It is original STW_Frame/FRAME.png with direct
  native crystal models. Extra UI tray/capture overlay stays hidden.
- Both power actors use original frame/portrait/class-back geometry, native mana
  gems and original used/restore curves. Direct AnimationClip.SampleAnimation on
  mixed exported bindings caused a native editor crash. Use the safe baked-channel
  path already implemented. Source white-neutral shadow uses multiply rendering.
- Hero death uses source crack quad/paint,17 fragment tracks, original particles,
  light curves, source effect clocks and exact crack/explode WAVs. Detached health,
  armor and power disappear at the explosion. Result text waits. Opponent, player,
  simultaneous-draw, menu and Play Again verifiers passed in the previous run.
  Original-hero-explosion.png was inspected. Distortion/erosion and global scene
  shake remain compatibility gaps. Effect-local slow time is an integration choice.
- VerifyNativeHeroPowers passed again during this session. Its fixture now waits
  for opening draw completion before setting the used flag, preventing a test race.
- Selection masks derive from actual source mesh triangles and recovered 256px
  projection/pass layout. CPU fill/blur/composite kernels are compatibility
  reconstructions. Extra blur offset1.5 is a reconstruction choice. Original hand
  gloss/reflection rendering remains unfinished.

## Installed WIP — first tests for Gemini

Tools/InstallNativeMinions.cs and Tools/InstallNativeHandOutlines.cs both completed
successfully during this session. They changed TableMinion.prefab and TableCard.prefab.
They have NOT received their live Play-mode/visual verification yet.

Files include NativeMinionPresentation.cs, NativeHandCardPresentation.cs,
NativeMinions assets/source manifest, NativeHandOutline.shader, CardView/MinionView
read-only presentation state and shared NativeHighlightBaker/SelectionHighlight.

The minion uses original 532-vertex oval actor, original stat gems/shadow/taunt and
source-geometry eligibility outline. Taunt uses source settled scale.9775736. Its
source spring birth motion is still unported. Draft fixes already saved include
rootCanvas lookup for held minions, suppressing legacy art after SetMinion,
null-art rebinding and stage disposal. Hand outlines use exact original static
Ally/Ability silhouettes and source green parameters. Eligibility checks turn,
canDrag, mana, board space and correct hand ancestry. Concealed, ghost, mulligan and
collection views should suppress it.

Run in Play with auto-tick, sequentially:

python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeMinions.cs"}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeHandOutlines.cs"}'

Inspect a disposable visual fixture through Tools/PreviewNativeCards.cs. Do not
save its gameplay objects. Fix actual failures before claiming this WIP complete.

## Spell WIP — compiled, installer still needs retry

NativeSpellLibrary.cs, NativeSpellPresentation.cs, NativeSpellSurface.shader,
NativeSpells assets/configuration, PrepareNativeSpells.py, InstallNativeSpells.cs,
VerifyNativeSpells.cs and the BattleEffects queue seam are included. UI.asmdef adds
Universal.Runtime for isolated capture-camera setup. Updated runtime compiled at
handover. The original spell presenter is not installed into the live scene yet.

Last installation failed on FireParticleBall null material. Source inspection
proved it is the intentionally null second particle-trail slot with TrailModule
disabled. Installer now preserves that exact case, rejects active/primary missing
materials and cleans temporary actors in finally. Retry it in clean Edit mode.
The failed temporary scene was reopened/discarded earlier. Current scene is clean.

Source corrections saved and still unverified in Play:
- Original Fireball/Frostbolt missile duration.4sec, ease0 = easeInQuad.
- Source capture camera90deg X, original near/far/plane offsets and resolution on
  longest texture axis.
- Fireball flash first .05sec linear expansion.5 to4, then .75sec easeOutQuad to7.
  Original alpha/light keys and finish/tail timing are retained.
- Exact cast/impact SoundDefs and source prefab/PS data are selected.
- Moving-target fallback retains last live target location.

Next commands:
python3 Tools/editor_api.py 'script run' '{"file":"Tools/InstallNativeSpells.cs"}'
Then in Play/autotick:
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeSpells.cs"}'

Inspect actual Fireball/Frostbolt captures. Original bloom/blur/distortion/shake
internals remain approximated. Root Sparks/Smoke share the capture instead of the
original separate world trails. Preparation currently short-circuits existing
GUIDs, so rerunning can produce an incomplete 8-record manifest. Assets remain
present. Preserve/re-walk existing dependency records before treating a regenerated
manifest as exhaustive. See Docs/Recovery/Spells-source.json and external spell CIL.

Exact source portrait PNGs already exist in Desktop/Hearthstone Export/Texture2D:
WoW_CHI_006_D.png (Frostbolt), W15_A087_D.png (Fireball), W10_A176_D.png (Arcane
Explosion), WOW_SPE_006(b)_D.png (Flamestrike). Card artwork binding still needs review.

## Intro/menu and mulligan — recovered source, implementation pending

Current frontend still uses the earlier cropped box image and side buttons. Current
mulligan has original banner/keep/replace/X/confirm assets but oversized layout
obscures hero portraits. Craig explicitly wants both improved.

New original source recovered outside the repo at:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/Box-complete-source
Use ExportedProject/Assets/AssetBundles/essential_base_box_global-prefab-0.unity3d/TheBox.prefab.
It contains 242 transforms, 68 renderers, actual box/lid/center disk/buttons/table,
source cameras, TitleLogo and open/close/camera clips. Selected game-project closure
has not been prepared or installed yet. Use complete 30-bundle export, not the first
incomplete Box-native-source export.

Detailed source procedure and next steps:
recovery/box-recovery-handoff.md
recovery/box-objects-summary.json
recovery/mulligan-manager-methods.txt
recovery/mulligan-layout-il.txt
recovery/mulligan-constants-il.txt

Original main camera local(0,31.17171,-.025),90deg X,FOV20.7. Source TheBox authored
opened/closed states and clip transitions are available. Existing Play Game and
Choose Deck actions should map to original button slots. Isolate menu camera and
listeners, preserve deck builder and menu/match visibility.

For mulligan, original MulliganManager uses BoardStandardGame.StartingHandZone.
PC friendly card scale(1.1,.28,1.1), original deal time1.5sec. Recover original hero
move-position clips (GUIDs in external notes) so portraits move clear of the cards.
Keep CardInstance-based selection,30-card counts and non-raycasting art.

## Local environment and access

Project root:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone
Git remote: https://github.com/craig-middleton/hearthClone.git
Branch main. Git LFS 3.8.1 and authenticated GitHub CLI verified. Python yaml/Pillow
and liblz4 source-prep dependencies verified. Gemini account/client/tool access is
user-side setup. No gemini/antigravity/agy executable was found on current PATH,
which does not determine whether a graphical client is installed.

Unity 6.6/6000.6.9f1:
/home/craig-middleton/Unity/Hub/Editor/6000.6.9f1/Editor/Unity
Main scene Assets/Scenes/HearthstoneTable.unity. Launch via Unity Hub if closed.

Desktop source folders:
/home/craig-middleton/Desktop/Hearthstone Card Database
/home/craig-middleton/Desktop/Hearthstone Export
/home/craig-middleton/Desktop/Hearthstone Extraction

Original installed client:
/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone
Bundles Data/Win, metadata Hearthstone_Data/Managed. Inspect metadata without
executing/importing the original client assemblies.

External recovery root:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery
Contains Interface-native-source, ORG-native-source, Box-complete-source,
box-bundles, card-animation-audit/InspectManagedMetadata.exe and Mono.Cecil, exact
CS2_024/025/029/032-carddef/effect JSON, spell-super-path-il.txt,
spell-render-to-texture-camera-il.txt and card-highlight-render-il.txt.
Source tool ../../tooling/AssetRipper-2.0.0/AssetRipper.GUI.Free. A source-export
server may still listen at 127.0.0.1:58741. Check before starting another instance.

All these paths were verified on this laptop. A fresh GitHub clone includes selected
runtime assets via Git LFS, but the external archives require transfer/regeneration
on another computer. Local Gemini can access the existing folders if its file and
terminal permissions permit them.

## Unity API and switching models

Run Tools/editor_api.py from project root. It reads ignored Library/Pipeline
credentials itself. Never print or commit those credentials.

python3 Tools/editor_api.py get /api/editor_status
python3 Tools/editor_api.py 'script recompile' '{"focus":true}'
python3 Tools/editor_api.py 'script recompile_status'
python3 Tools/editor_api.py 'console get' '{"level":"error","tail":20}'
python3 Tools/editor_api.py 'editor.playmode start'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":true,"interval-ms":16,"persist":false}'
python3 Tools/editor_api.py 'capture game_view' '{"output-path":"Docs/Previews/review.png","source":"camera","width":1469,"height":740}'
python3 Tools/editor_api.py 'editor.playmode stop'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":false,"persist":false}'

Use args for script arguments, entry for alternate verifier entry points. Allow
reload/Start completion. Run installers in clean Edit mode and fixtures in Play.
Temporary fixtures must be discarded before saving the scene. Check output and
captures rather than claiming success from a command's existence.

For each switch, have the current agent update this same HANDOFF.md and commit/push
its checkpoint, record any remaining local files, and stop active implementation
before the next agent edits. The next agent reads the files and git status, rather
than assuming access to the other chat. Say continue from HANDOFF.md when returning
to Codex. Keep PROJECT_STATUS current and history in PROJECT_HISTORY.
