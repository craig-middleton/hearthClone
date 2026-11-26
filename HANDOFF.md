# HearthClone — current checkpoint

Updated 26 November 2026. Codex project continuity. Read AGENTS.md, this file,
PROJECT_STATUS.md and relevant PROJECT_HISTORY.md entries before editing. Inspect
Git status first. Run git log -1 to identify this containing checkpoint. Recovery
baseline is bd24473 (the final Codex typography/mulligan commit of22November).

## Current two-card review checkpoint

Craig reported that Beast was larger than Fire, and the Beast plate overlapped the
attack sword and health droplet. Fixed both in High. Recovered complete source
Card_Hand_Ally RaceUberText has the same .85 x .17 text box as the spell school
label. Projected through original transforms, this is(74.79374,14.958747), replacing
the study's manually chosen(96,18). Beast now uses the existing Fire label's recovered
Belwe font/material and shared TMP auto-fitting. Original plate/anchor/art unchanged.
Moved the plate's UI sibling before the original stat gems, which draw in front.
InstallNativeCardStudy.RefreshTribePresentation applies these changes without
recreating the existing prefab children. Full installer applies them too.

Fresh VerifyNativeCardStudy and VerifyNativeCards pass; study explicitly compares
rendered Beast/Fire sizes and verifies plate order below sword/health. Updated
actual Game-view Card-study-review.png inspected. Saved clean Edit, auto-tick off,
Console0 errors/0 warnings. No full match/build/user visual approval is claimed.
Prior Fire anchor fix remains: original RaceUberText(3.4838707,-105.92626), with
baseline spell anchor restored on rebind. Verified baseline3551696; git log -1
identifies this containing checkpoint.

Craig wants a quicker project-wide visual boost rather than prolonged small-detail
iterations. Recommended next milestone is shared card presentation across the
existing deck, then a whole-match composition pass with before/after screens.
He specifically APPROVED Fireball's “Deal 3 damage” description font and requested
it throughout. Audited all47 CardData assets in a disposable live CardView: every
body ALREADY shares FranklinGothic/FranklinGothicInk, base size17.25346, ink and line
spacing.21 nonempty descriptions fit, sizes12.05..17.25346; longest Cone of Cold.
No font substitution was needed. Retain this approved standard and original keyword
markup; longer descriptions auto-fit. Audit was a temporary /tmp script, not a new
runtime feature. Current effort High; Ultra only when complex source rendering or
sequence reconstruction is next. Craig controls switching.

Only Ironfur Grizzly/Fireball opt into the source-presentation study. Other card
faces use the baseline library; card rules unchanged (Fireball deals3 here). No
exact whole-card parity claim. See Docs/Recovery/TWO_CARD_STUDY.md for source limits.

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

Use the approved Fireball description typography throughout (already verified on
all47 assets). Prioritize shared card presentation and full-match visual impact,
with actual Game-view comparisons; Craig wants faster visible progress. Wider
source-presentation rollout still needs original dependencies and must preserve
approved mana/board and mechanics. High is suitable for current font/layout work;
complex shader/sequence reconstruction needs Ultra. Explain and wait at a switch.

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
