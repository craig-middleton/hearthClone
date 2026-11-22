# HearthClone — current handoff

Updated 22 November 2026. Single rolling handoff shared by Codex and local Gemini.
Read AGENTS.md, GEMINI.md, PROJECT_STATUS.md and git status before editing. Run
`git log -1` to identify the containing checkpoint. Previous baseline:2b462b5.

## Current checkpoint

Craig REJECTED the visual result of2b462b5 in screenshots22Nov20:47. Do not
present the menu/mulligan as visually finished. This checkpoint fixes specific
faults: opaque centre-disk wood disabled by an unsupported glow submesh, unreadable
menu text, horizontal labels during camera tilt, and a left-shifted mulligan row.
Original selectable silhouette glow now appears on kept mulligan cards and hides
on replacement. Captured actual Game view screen (not only isolated-camera renders)
in Docs/Previews/Native-menu-corrected.png and Native-mulligan-corrected.png.
Menu/mulligan/frontend verifiers pass and final Console has zero errors/warnings.
Unity is stopped in clean Edit, temporary auto-tick off. Visual acceptance pending.

Remaining visible shortcomings include dark/flat source-surface compatibility,
card typography/material richness and hero/banner/confirm crowding. Do not treat
functional tests as visual approval. New menu fit margin1.08 and gold outlined
labels are explicit compatibility presentation choices, not recovered source values.
Mulligan spacing is original but centred for our three-card/no-Coin local player;
the earlier asymmetric source layout reserved a phantom Coin slot in this flow.
Continue using original assets. No new motion was invented. Original camera clip
remains. Complex original shader reconstruction needs Ultra; Craig controls effort.

Craig requires brief progress explanations, direct-main significant commits/pushes
and this handoff overwritten in the same commit. Compatibility rendering is allowed.
Do not claim pixel-identical original shaders or full visual completion.

## Implemented and verified in this checkpoint

- **Menu/intro:** imported120 original dependencies,242 transforms and68 mesh
  renderers from complete TheBox export. Original chest, table, disk and button
  geometry/camera/state poses; safely baked7 transform channels drive original
  4.983s camera intro once per launch. Existing Play Game/Choose Deck actions map
  onto the original first/second slots. Other slots remain decorative. Source
  Startup_Hub hides its loading Logo, reproduced here. Camera isolated on layer30,
  no new listener/client scripts; model/camera hide in deck builder and match.
- **Important menu fix:** source GPU meshes are not runtime-readable. Installer
  bakes exact vertices into serialized Slot.sourcePoints. Runtime projects those
  points, without mesh.vertices reads/per-frame arrays. Earlier tests passed while
  console spam exposed this fault; fixed and retested fresh launch with clean
  Console. Do not regress to direct runtime vertex reads. Ordinary lifecycle plus
  Start fallback is used; no unsafe AnimationClip.SampleAnimation.
- **Mulligan:** original10-unit StartingHandZone slots, original1.1 desktop X/Z
  scale, height/z offsets and source keep/replace positions projected through the
  board camera. Original Mulligan_Banner_Choose wrapper supplies missing
  1.75×1.630027 scale, Y180 rotation, headline/caption anchors and original colors.
  Standalone BannerMulligan alone was too small. New visual-only wrapper retained.
  Hero socket clips occur BEFORE dealing; no invented move-aside animation added.
  Source spacing now centres the actual visible local hand; see checkpoint correction. Existing confirm/deal remains.
- **Source material values:** SourceMaterialProperties reads preserved serialized
  colors/floats/textures/UV values. Material.GetColor/GetFloat with unavailable
  original shaders returned zeros despite source values existing. Fixed hand,
  minion, power and spell installers, then reinstalled. Do not use missing-shader
  getters for source data.
- **Played minions:** original532-vertex oval actor/frame, portrait, native gems,
  shadow and settled taunt. Source states4/5/6 map to HighlightGreenInPlay, now used
  for ready/selected; Yellow is original combo-hover state10. Eligibility mask is
  source geometry. Existing input/stats/lifecycle are retained. rootCanvas handles
  held views. Legacy art suppressed after refresh; portrait/stage cleanup checked.
- **Hand:** original Ally/Ability silhouette textures, original green parameters.
  Explicit owner/panel binding from EffectTester→HandDisplay avoids a failed
  canvas search (EffectTester is a separate root). Mana, space, turn, concealment,
  ghost/mulligan/collection suppression verified; graphics do not intercept input.
- **Spells:** actual Fireball/Frostbolt casts use original missile/impact prefabs,
  particle modules, source .4s easeInQuad paths, flash/light curves, exact sounds,
  original viewport sizing/camera and independent original tails. Queue completion,
  lethal target snapshot and menu/new-game disposal checked. Full58-record source
  manifest dependency traversal repaired (previous rerun produced8records).
  Exact source portraits wired for Fireball/Frostbolt/Arcane Explosion/Flamestrike.
  Only the first TWO have newly installed original spell VFX; others keep prior FX.

## Actual verification and limits

Passed this session: VerifyNativeMinions, VerifyNativeHandOutlines,
VerifyNativeHeroPowers, VerifyNativeSpells, VerifyNativeMulligan, VerifyNativeMenu,
VerifyFrontEnd, VerifyStartingDecks, VerifyNativeMatchControls, VerifyNativeManaRack.
Menu verified again after source-point fix, including actual intro advancement,
slot raycasts, Choose Deck/back, match start and return. FrontEnd real collection
clicks/save/reload/exact30 composition passed on a fresh start. Deck counts include
opening hands (P1:26+4 at turn1, P2:25+5 including Coin). Disposable fixtures only.

Inspected captures in Docs/Previews: Native-menu-verified.png,
Native-mulligan-source-layout.png, Native-cards-verified.png,
Original-Fireball-impact.png, Original-Frostbolt-missile.png. No standalone build,
full-match manual playtest or physical-speaker listening claim. Audio checks prove
bindings and AudioSource playback. Hero-death all3 branches passed prior98d3481;
not rerun here. Corner checkpoint2e92890 and approved mana5d07762 preserved.

## Remaining visual work / next actions

1. Craig rejected previous menu/mulligan visuals. Review new actual-screen captures with him; prioritize lighting, card typography and portrait/modal composition. No visual approval yet.
2. Hand/minion material richness is unfinished: original gloss/reflection/bloom,
   animated source highlight motes, source taunt birth spring are not installed.
   No invented shimmer or tweens. Existing name meshes/typography need review.
3. Spell surface shader remains generic compatibility: original electrical/noise,
   scrolling-flame, AlphaClipBloom compositor, bloom/distortion/shake are incomplete.
   Source root Sparks/Smoke share capture instead of original separate world trail.
   Preserve originals, investigate their shader semantics before more changes.
4. Menu uses compatibility unlit/multiply surfaces, with unsupported scrolling/glow
   placeholders hidden. Original lid open/close clips are retained but not played;
   only original camera intro is wired. Original menu sounds not added in this pass.
5. Mulligan original hero intro/deal motion remains unported; confirm position is
   existing adapted UI. Banner font sizing is TMP compatibility conversion.
6. Existing hero-death crack distortion/erosion/global shake remain incomplete.

## Files / tools for next agent

Runtime: Assets/_Project/Scripts/UI/{NativeMenuPresentation,
NativeMenuSourceConfiguration,NativeMulliganPresentation,NativeMinionPresentation,
NativeHandCardPresentation,NativeSpellPresentation,NativeSpellLibrary}.cs.
Authoring helper: Assets/_Project/Editor/SourceMaterialProperties.cs.
Installers/preparers/verifiers: Tools/*NativeMenu*, *NativeMulligan*,
*NativeMinions*, *NativeHandOutlines*, *NativeSpells*. PreviewNativeSpell has
Fireball/Frostbolt entries that PAUSE Time.timeScale for capture; always run its
Resume entry or stop Play afterward. Do not save gameplay fixtures.
Source records: NativeMenu/SourceManifest.json + SourceConfiguration.json,
Docs/Recovery/Mulligan-layout-source.json, Docs/Recovery/Spells-source.json.

Use Tools/editor_api.py authenticated loopback API; it reads ignored credentials
privately. Never print/commit credentials. Do not launch Unity from a restricted
tool shell (previous preferences/GI-cache failure); use Hub.

python3 Tools/editor_api.py get /api/editor_status
python3 Tools/editor_api.py 'script recompile_status'
python3 Tools/editor_api.py 'editor.playmode start'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":true,"interval-ms":16,"persist":false}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyNativeMenu.cs"}'
python3 Tools/editor_api.py 'capture game_view' '{"output-path":"Docs/Previews/review.png","source":"camera","camera":"TheBoxCamera","width":1469,"height":740}'
python3 Tools/editor_api.py 'editor.playmode stop'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":false,"persist":false}'

Menu camera is Untagged; named TheBoxCamera. Default Camera.main capture shows the
board, not the native menu. Gameplay captures use Main Camera. Run installers in
clean Edit, recompile new runtime types first. Run menu verifier before pointer
frontend tests on fresh launch; controls wait for the5-second source intro.

## Local environment, external sources and unrelated files

Project: /home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone
Remote: https://github.com/craig-middleton/hearthClone.git, branch main, Git LFS.
Unity: /home/craig-middleton/Unity/Hub/Editor/6000.6.9f1/Editor/Unity
Recovery: /home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery
Complete menu: recovery/Box-complete-source/ExportedProject/Assets (use this, not
incomplete Box-native-source). Interface-native-source and ORG-native-source also
remain external. Source CIL/evidence includes box-recovery-handoff.md,
box-objects-summary.json, mulligan-original-flow-il.txt, spell-super-path-il.txt,
spell-render-to-texture-camera-il.txt and card-highlight-render-il.txt.
Desktop: /home/craig-middleton/Desktop/{Hearthstone Card Database,Hearthstone Export,
Hearthstone Extraction}. Original installed client:
/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone
Inspect assemblies as metadata only, never execute original client scripts.

Unrelated pre-existing local changes remain excluded:
Assets/_Project/RecoveredDecks/Deck_75_mesh.mat, Assets/_Recovery.meta,
Docs/Previews/Native-hero-powers-first.png (rejected early shadow proof).
Selected runtime assets are committed, large recovery archives stay local. A fresh
GitHub clone needs LFS; it does not supply external archives or Editor access.
