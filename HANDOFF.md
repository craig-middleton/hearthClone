# HearthClone — current checkpoint

Updated 26 November 2026. Codex project continuity. Read AGENTS.md, this file,
PROJECT_STATUS.md and relevant PROJECT_HISTORY.md entries before editing. Inspect
Git status first. Run git log -1 to identify this containing checkpoint. Recovery
baseline is bd24473 (the final Codex typography/mulligan commit of22November).

## Current two-card review checkpoint

Craig reviewed the first pair and reported that Fireball's “Fire” label sat above
its school plate. He switched to High for the targeted correction and reiterated
original game assets only, wired properly. Fixed the label binding using the
original Card_Hand_Ability/Description_mesh/RaceUberText anchor. Its projected
position is (3.4838707,-105.92626), replacing the earlier adapted Y=-95.14285 only
for Fireball study entries. The source school mesh, textures and recovered font
are unchanged. Other spells restore their baseline anchor on the same view.

Fresh focused checks pass: VerifyNativeCardStudy (source anchor, visible font-glyph
ink in the plate band, Fireball→Frostbolt→Fireball rebinding), VerifyNativeCards and
VerifyNativeCardLayout. Actual Game-view Card-study-review.png was updated and
inspected: “Fire” now sits inside its gold plate. Final Console0 errors/0 warnings,
clean compile, saved scene clean in Edit, auto-tick off. No full match or build was
run for this anchor fix. No exact whole-card parity or Craig's approval is claimed.

The opt-in Ironfur Grizzly/Fireball prototype remains the scope. Other card faces
use the baseline library; Frostbolt retains its baseline spell entry. Card rules
are unchanged (Fireball still deals3 here). Review this pair before wider rollout.
Details/source limits: Docs/Recovery/TWO_CARD_STUDY.md. The preceding verified study
checkpoint is051296c; use git log -1 for this containing commit.

Implemented:
- CardStudy profiles retain original UV2/secondary paint and source gem mask inputs;
  restored original Beast plate/label and Fireball school description mesh.
- Prototype-only Belwe titles use source name rectangles/outline; TMP conversion,
  supersampling and raster outline are compatibility. Global titles unchanged.
- Linear-space GPU test found .5 mod2x tint made white.428; shader now preserves
  numeric neutral factor, GPU white1. Source frame shaders are primarily unlit,
  not PBR. Extra specular/reflection based on stale material fields is not justified.
- Grizzly uses original.48333338s summon position/quaternion channels and original
  .6wait/.4Spring Taunt.1→.9775736 plus exact up sound. Board-owned per-Minion clock
  survives view refresh; name+art gate avoids Bear. Mid-birth rebind restores pose.
- Fireball-only adapted prefabs use recovered active shader families: cutout cores,
  alpha-noise particles, additive glow, smoke and multiply mark. Source PS, motion,
  camera, clocks and sounds retained. Cross-export GUID fallback validates source
  values/texture hashes/UVs; see FireballPrototype/ResolvedMaterialBindings.json.

Important limits: UI geometry projection is retained, not full original3D card
lighting. Shader kernels/scroller timebase/multiply and final compositor remain
compatibility approximations. Taunt fade/gleam/particles/death/VO, original bloom/
distortion/camera shake and separate world trails remain unported. First observation
starts Grizzly birth clock. Do not present this prototype as finished visual parity.

## Editor and verification

Unity6000.6.9f1 on saved HearthstoneTable, stopped clean Edit, auto-tick off. Disposable
fixtures discarded. Fresh compile and final Console0 errors/0 warnings.

Passed25–26November: VerifyNativeCardStudy, VerifyGrizzlyTauntPrototype,
VerifyNativeSpells, VerifyNativeCards, VerifyNativeCardLayout, VerifyNativeMulligan,
VerifyNativeMenu, VerifyFrontEnd, VerifyStartingDecks, VerifyNativeMatchControls,
VerifyNativeManaRack. Study checks opt-in/fallback/rebind/clip/ghost/concealment and
GPU neutral-white. Grizzly test slows only its fixture to.25timeScale to observe
brief overshoot on background Editor frames, restoring it in finally. Runtime
source curves/timings unchanged. Wall timeouts accommodate background stalls.

Actual Game-view captures inspected: Card-study-review.png (real hand views enlarged
for review) and Fireball-study-impact.png. No standalone build, full manual match,
physical speaker listening or visual approval claimed. Earlier recovery58371da
removed all uncommitted external-model experiments/setup and restored bd24473;
this new prototype is separately sourced implementation, not salvaged code.

## Next action / effort

Review the corrected school-label preview with Craig. Continue specific feedback
on ONLY these two cards before applying a study profile to more cards. High is suitable for review and small
layout refinements; source shader/sequence reconstruction remains Ultra work. Craig
controls effort; explain and wait at a switch point. Keep brief progress updates.

## Prototype files / reinstall

New source/runtime: NativeCardStudy, GrizzlyTauntPrototype/Director/Library,
PrototypeCardSurface, FireballPrototype shader/DTO, PrototypeCardMaterialBaker and
TypographyBaker. Minimal optional bindings in NativeCardFace/Layer/Library.

Tools preparers: PrepareCardStudySource.py, PrepareGrizzlyTauntPrototype.py,
PrepareFireballPrototype.py. Clean Edit installers: InstallNativeCardStudy.cs,
InstallGrizzlyTauntPrototype.cs, InstallFireballPrototype.cs. Import/recompile new
runtime/Editor types first. NativeCardStudy installer opens proof scene additively
and closes it without saving. Do not rerun historical broad card installers that
replace outline/decorative children.

PreviewCardStudy.cs enlarges real hand views and DISABLES FAN only in its disposable
Play session: stop Play before normal testing. PreviewNativeSpell.Fireball waits
for real impact/source time and PAUSES Time.timeScale for capture: call Resume or
stop Play afterward. Never save the fixtures. Use source=screen for actual Game view.

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
