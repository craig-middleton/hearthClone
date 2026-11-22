# HearthClone — current handoff

Updated 22 November 2026. Overwrite this one file in the same commit as every main
commit. Read AGENTS.md, git log -1 and git status. This checkpoint follows 37c4ca4
hero powers and approved corner 2e92890.

## Current request and status

Craig confirmed Ultra and resumed. Current scope includes original hero-power
presentation, hero shattering, original spell VFX/sounds, hand/minion rendering,
and now a faithful intro/menu and corrected mulligan. Use supplied original assets
and animation data, with authorized compatibility rendering. Explain progress
briefly. Update this handoff with every main commit.

This checkpoint saves the installed hero-death sequence and source-derived power
readiness silhouette. Hero death passed opponent/player/simultaneous checks during
the previous run. The explosion capture has now been inspected. Fresh 22 Nov Editor
compile/console status is clean, and VerifyNativeHeroPowers passed after its fixture
was corrected to wait for the opening draw before testing the used state.

Hero death uses source crack geometry/paint,17-piece motion, original particles,
light curves and exact crack/explosion audio. Detached health/armor and powers hide
at explosion. Result UI waits, and Play Again/menu reset correctly. Local source
slow-time curves preserve effect pacing. Original distortion/erosion and global
camera/minion shake remain compatibility gaps. See Hero-death-source.json.

Source readiness masks are reconstructed from actual source mesh triangles through
recovered 256px projection/blur passes. The unavailable blur/composite shader kernels
are compatibility reconstructions. No artist-drawn replacement shape or motion was
added. NativeHighlightBaker and SourceSelectionHighlight are shared with pending
minion work. Current Power mask is nonblank and original flip/restore/input checks
pass. The previous grey square shadow issue remains fixed.

Remaining spell/card/screen work is local and unfinished. Active source recovery
continues on Ultra. Switch to High only at a tested visual-adjustment checkpoint.
Unity is kept in clean Edit mode between installer passes. Launch it through Hub.

## Verified hero-power checkpoint

- Both buttons now render original Card_Play_HeroPower 3D actors: original three
  submeshes (frame/portrait/class back), mana gem, shadow and ready highlight.
  Current original Fireblast/Armor Up artwork is mapped onto source portrait UVs.
  UI buttons/input remain, old flat images hidden. World geometry tracks input
  anchors; cost uses native gem position and original Belwe font.
- Original Used (.5333sec) and Restore (.4333sec) transform/enabled curves are baked
  through AnimationUtility and evaluated directly. Direct SampleAnimation on the
  mixed exported power clip triggered a Unity assertion/crash; DO NOT restore it.
  Manual evaluation retains source keys/tangents, composes quaternion channels
  before normalizing, and avoids the unsafe native clip binding.
- Source white-neutral grayscale shadow uses a compatibility multiply shader;
  ordinary alpha or vertex-RGB multiplication produced visible rectangles. Original
  source color/paint are retained. Compiled source specular/highlight shader math
  is not fully recovered, so bit-identical lighting/shimmer is not claimed.
- Source closure89 assets includes future death/Fireball visual templates, imported
  without client scripts. These inactive templates do not mean those features are
  installed. NativeHeroEffects/SourceManifest.json records provenance/gaps; two
  unused power animation slots were unresolved, while used/restore are recovered.
- Tests passed 21 Nov: VerifyNativeHeroPowers (both meshes/paint/curves, input and
  alignment, ready, used/restore, menu/new-game); VerifyNativeMatchControls;
  VerifyPowersAndBattle (actual targeting,2-mana/once-per-turn, lethal, AI Armor Up,
  cleanup). Console zero errors/warnings after safe preview. Native-hero-powers.png
  inspected. 22 Nov fresh Editor status/compilation/console checked, no rerun claimed.

## Unfinished LOCAL work, excluded from this commit

A GitHub-only clone does not have these uncommitted files. Preserve the working
copy; git status identifies any newer work. Pending parts must be checked before running their installers:

1. Hero death is now committed and installed. Files include NativeHeroDeathLibrary,
   NativeHeroDeathPresentation, NativeHeroCrackPart, two shaders, derived source
   prefab/materials/library, InstallNativeHeroDeath and VerifyNativeHeroDeath.
   Actual source paths/curves/clocks/audio are in Docs/Recovery/Hero-death-source.json.
   PreviewNativeHeroDeath creates a disposable lethal fixture for captures.
2. Minions/cards: Art/NativeMinions/ source closure + SourceManifest,
   PrepareNativeMinionArt.py, draft NativeMinionPresentation.cs and
   InstallNativeMinions.cs. DO NOT RUN installer until reviewed. Known drafts need
   rootCanvas lookup, persistent hiding of old artwork (SetMinion re-enables it),
   and original eligible highlight before removing existing feedback.532-vertex
   original oval actor, real gems/taunt/shadow recovered. Hand original meshes
   currently flatten lighting/depth/shadows. Highlight _MainTex is a black runtime
   placeholder; original HighlightRender generates silhouettes with multiple
   render/blur passes. Do not substitute a generic painted glow or invented motion.
3. Spells: NativeSpellLibrary/Presentation/Surface shader, preparation/configuration,
   installer/verifier and BattleEffects queue seam now exist locally, unverified.
   Last install failed on FireParticleBall second material slot, subsequently found
   to be an intentionally null unused trail slot. Resolve through source semantics.
   UI.asmdef adds Universal.Runtime for the capture camera. Exact mappings remain outside
   repo under recovery/CS2_024,025,029,032-carddef-source.json and matching
   *-effect-source.json; spell-super-path-il.txt/spell-super-fields.txt. Frostbolt,
   Arcane Explosion, Fireball, Flamestrike aliases resolve to original reuseFX.
   Fireball/Frostbolt source missile .4sec; ease0 = easeInQuad (NOT linear).
   Integrate through existing BattleEffects queue, preserving lethal holds and
   input; original sound references/timeline still need completion.

Also excluded: pre-existing changed RecoveredDecks/Deck_75_mesh.mat and Unity
recovery backup metadata Assets/_Recovery.meta. Do not discard either casually.

## Resume and tools

Read PROJECT_STATUS/HISTORY and the source manifests. Finish one original sequence
at a time, install only in clean Edit mode and run meaningful live checks. Main
checkpoint actor code: NativeHeroPowerPresentation.cs, HeroPowerPresentation.cs,
InstallNativeHeroPowers.cs, PrepareNativeHeroEffects.py, VerifyNativeHeroPowers.cs.
Run source hero power tests in Play with auto-tick enabled. Avoid concurrent scene
mutations from multiple tools/agents. Scripts are not client-code execution.

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
