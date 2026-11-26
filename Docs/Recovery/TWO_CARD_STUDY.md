# Ironfur Grizzly and Fireball source study

26 November 2026. Review checkpoint, not a claim of pixel-identical original rendering.

## Scope and binding

Only CardData references for Ironfur Grizzly and the existing Fireball variants opt
into Art/CardStudy/Study.asset. NativeCardFace retains its baseline library and
selects the study library per bind. Other cards restore baseline materials; inactive
extra geometry remains hidden. Ghosts, collection/mulligan and concealment keep the
existing input/rules. Minion prototype also requires exact name AND sprite because
Bear shares Grizzly's artwork. No card rules changed: this project's Fireball still
costs4 and deals3, so its wording differs from the original game's6-damage card.

CardStudy copies selected source meshes/materials/textures, reusing existing GUIDs.
SourceManifest.json records origin and hashes. Original source code is never run.
Source geometry is projected into UI layers as before; this is not a new full3D
card renderer or a whole-card bitmap replacement. Original UV2, material inputs,
source normals and source glyph proportions are retained in study data. Actual
frame/portrait/description rendering is primarily unlit, as the recovered shader
families specify. No invented PBR/specular overlay was added.

## Card materials and typography

The complete Box export resolves original shader families missing in the narrow
Interface export. Frames/banners are original unlit texture shaders. Portrait and
description materials add a second atlas on UV2. Gems use the source diffuse
alpha-mask cloud scroller; stored legacy reflection/shininess fields absent from
the active shader declarations are not evidence for extra specular effects.

PrototypeCardSurface preserves paint alpha as a GEM EFFECT MASK, not card opacity.
The secondary multiply and gem kernels are compatibility reconstructions: compiled
original shader bodies are unavailable. Texture/tint/ST/scale/speed values come
from original materials. Scroller timebase is inferred. Source normals are retained
but the compatibility fragment path does not reproduce full diffuse lighting.

A GPU test caught linear color-space conversion of the source .5 mod2x tint:
white became .428. The shader restores that authored numeric factor before the
multiply; white now stays1. This is a rendering compatibility correction, not a
manual brightness boost to the artwork. The test runs in the active color space.

Grizzly has its original RacePlate_mesh and Beast label; Fireball uses the original
130vertex/two-submesh Description_School mesh. Titles use recovered Belwe and
source NameUberText rectangles1.9×.3(minion)/.28(spell), source outlineRGB/size3 and
resolution300×2. TMP metric conversion, supersampling and outline rasterisation
are compatibility choices. No global name textures/fonts were changed. Beast font
size uses UI conversion, not a recovered UberText pixel-size claim.

## Grizzly summon and Taunt

The ordinary source AllyInPlaySpawn clip supplies seven position/quaternion
channels over.48333338s. AnimationUtility bakes them in Edit; runtime evaluates
curves directly. NEVER SampleAnimation on mixed exported actor bindings (known
native crash). Source Taunt Idle State waits.6s, then original.4s iTween Spring
scales.1→.9775736 with exact taunt_shield_up WAV, volume.9/pitch1/spatialBlend0.
Source.json records raw state offsets, original clip/sound hashes and source IL.

State belongs to the board director and actual Minion instance, so refreshing views
does not replay the sequence/sound. It resets on menu and ignores same-art Bear.
Direct mid-birth SetMinion rebinding also restores the previous resting pose/shield
before handing the view back to baseline presentation. Initial timing is adapted
from first view observation, not the unrecovered original spell-entry clock.

Unported: Taunt root fade, gleam, particles, death sequence and VO. Existing played
minion frame/portrait/gem renderer is otherwise unchanged, including its material
limitations. No invented summon pop, camera shake or new animation was authored.

## Fireball effect

Only Fireball's missile/impact prefab references in NativeSpells.asset change.
Frostbolt is compared before/after by the installer and retains its old entry.
Source transforms, meshes, particles, clocks, path timing, viewport camera and audio
remain. The material adapter uses ACTIVE shader declarations to distinguish
cutout/depth-writing cores, layered alpha-noise particles, additive glow, smoke and
multiply impact material. It ignores stale electrical-effect fields on unlit cores.

Different exports remap material GUIDs. The fallback resolves exact material name
and validates active values, texture SHA256 and UV transforms against the complete
export before accepting it. ResolvedMaterialBindings.json records accepted mappings.
Generic compatibility/prototype materials are rejected as original source inputs.

GPU kernels, noise product, tint convention and final bloom/compositor remain
compatibility approximations. The existing spell-light compositor and shared root
particle capture are not the original AlphaClipBloom renderer. Original camera
shake/distortion/bloom internals remain unported. This pass does not promise exact
original VFX or new original effects for other spells.

## Verification and review

Fresh checks on25–26November passed: VerifyNativeCardStudy, VerifyGrizzlyTauntPrototype,
VerifyNativeSpells, VerifyNativeCards, VerifyNativeCardLayout, VerifyNativeMulligan,
VerifyNativeMenu, VerifyFrontEnd, VerifyStartingDecks, VerifyNativeMatchControls,
VerifyNativeManaRack. Console0 errors/0 warnings; clean compilation.

The study test covers exact opt-in/fallback, prototype→ordinary→prototype rebind,
masking/input, concealment, ghost cloning and GPU neutral-white result. Grizzly test
also checks same-art isolation, summon landing, source spring overshoot/settled
scale, one sound, refresh/menu and mid-birth rebind. It temporarily slows only the
disposable fixture to.25 timeScale so background Editor frames can observe the
short overshoot; original production timings remain unchanged. Wall timeout30s
allows slow Editor pumps. The finally block restores timeScale.

Spell test covers actual Fireball hero damage and Frostbolt lethal target, paint,
audio playback, isolated cameras/input, queue release, tails and reset. Cleanup
wall timeout is20s to allow background Editor stalls; original effect lifetime is
unchanged. Capture fixture waits for actual impact phase and source time, rather
than assuming a wall-clock delay lands on impact. It pauses time for capture:
always run PreviewNativeSpell.Resume or stop Play afterward.

Inspected actual Game-view captures: Docs/Previews/Card-study-review.png and
Docs/Previews/Fireball-study-impact.png. PreviewCardStudy uses real hand views,
enlarged and repositioned for review; it disables the fan only in that disposable
Play session. Stop Play before other verifiers or regular play. Final Editor is
stopped in clean Edit with auto-tick off. No standalone build, full manual match,
physical speaker listening or user visual approval is claimed.

## Reproduce

Run preparers from Tools in the existing local source environment; compile/import
new types, then use the authenticated editor_api.py helper in clean Edit:

- InstallNativeCardStudy.cs: selected alternate card library/profile/prefab binding.
- InstallGrizzlyTauntPrototype.cs: source channels/library, optional minion adapter
  and board director. Saves scene/prefab.
- InstallFireballPrototype.cs: only Fireball phase references and source materials.

Do not run broad historical card/board installers over these prefabs. The profile
can be disabled by clearing NativeCardFace.study and hiding its extra decoration/
tribe objects; existing baseline library and paints remain available.
