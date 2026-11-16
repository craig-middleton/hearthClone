# Original card draw motion — 16 November 2026

The live match now presents ordinary successful deck draws using motion recovered from the installed client. `NativeCardDrawPresentation` evaluates the source path, Euler rotation, scale timing, and easing in source world coordinates, then returns the card to HearthClone's existing UI hand. `NativeCardMotionMath` contains the small mathematical implementation. No original executable game assembly is imported, loaded, or invoked at runtime.

This is source-derived motion applied to the current layered card visuals. It is not a restoration of the original client's complete card actor, lighting, shaders, hand system, or animation state machine.

## Source evidence

The audit read installed `Assembly-CSharp.dll` and `Blizzard.T5.iTween.dll` with Mono.Cecil. Only our own inspection tool executed; client assemblies were read as metadata and IL. Source board transforms came from exported `BoardStandardGame.prefab`, including the complete parent hierarchy. The recovered board camera supplies the projection.

Exact input paths, sizes, and SHA-256 hashes are in [OriginalCardMotion-source-provenance.json](OriginalCardMotion-source-provenance.json). Key identities:

| Input | SHA-256 |
| --- | --- |
| Assembly-CSharp.dll | `2408c5eae3d5ab083b8b290a7278147159e79a3c6e72e1d922ef44028ed16afb` |
| Blizzard.T5.iTween.dll | `04f0721435ac1cd14e68ffebd373db46205818cdbd4408d39ad72ee20021e1b3` |
| BoardStandardGame.prefab | `6686571e356cb8f05474b5dc237678c29d56e5dffc14b13d1e3a53be8a2861d5` |
| BoardCameras.prefab | `2a224fef3e56cb1f4d379f8a889cbe8d4582fbb658e59eba238f9544e77e5923` |

The detailed local audit remains outside the repository at `../../recovery/card-animation-audit/` relative to this repository root. It includes `source-draw-motion-spec.md`, `original-draw-coroutines-il.txt`, `itween-targeted-il.txt`, `board-draw-source-poses.json`, and `board-camera-source-pose.json`. Client DLLs and bulk IL dumps are not included in this documentation.

Useful method-local IL offsets for the hashed inputs:

| Method | Offsets | Evidence |
| --- | --- | --- |
| `Card/<DrawFriendlyCardWithTiming>d__509.MoveNext` | `0353..040e` | Three position nodes, duration1.5, ease33 |
| Same | `0413..053d` | Rotation and scale targets, delays, durations |
| Same | `0636..06fa` | End draw, hand refresh/layout, interaction timing |
| `Card/<DrawUnknownOpponentCard>d__514.MoveNext` | `025b..0451` | Four-node concealed opponent draw and channel easing |
| `ZoneDeck.SetCardToInDeckState` | `0000..005d` | Top-of-deck bone and initial scale.88 |
| `ZoneHand.FocusCard` | `023e..02de`, `0618..076f` | Hand reflow times, SLOW style easing, tween arguments |
| `iTween.GenerateMoveToPathTargets` | `0146..01ea` | Reflected endpoint controls |
| `iTween/CRSpline.Interp` | `0000..010f` | Uniform Catmull–Rom interpolation |
| `iTween.easeInSineOutExpo` | `0000..001d` | Actual branch condition behind enum33 |

The source `draw_card_1.prefab` and related numbered prefabs are audio definitions, not the movement animation. Constant-pose `AllyInHandBirth`/`Death` clips do not contain deck travel. Those names were not used to invent a replacement trajectory.

## Trigger and lifecycle

`PlayerHand.DrawCard` emits `CardDrawn(card, remaining)` only after a card is successfully added to the hand. `EffectTester` forwards that event once mulligan is complete. `HandDisplay.RenderHand` lays out the new views and binds the presentation to their card-instance identities.

The presentation temporarily hides the real hand view and creates noninteractive front/back snapshots on a world-space Canvas rendered by the board camera. Every snapshot vertex follows the 3D pose. Friendly cards switch between front and back according to their facing; concealed opponent cards remain face down. The existing overlay hand still owns interaction and final layout.

Rebuilding the hand during a flight rebinds its destination to the replacement view. Finishing restores the destination view's prior alpha/interactivity and destroys the transient Canvas. Removing a card, resetting a match, disabling the presenter, or destroying it cleans up pending flights. A missing camera does not leave hidden hand cards behind.

Generated cards, the opening deal, mulligan replacements, burned draws, and fatigue do not receive a substitute deck-draw animation. Manually revealing the opponent hand retains its immediate presentation: that distinct revealed-opponent source sequence has not been integrated.

## Implemented timing

All times below are seconds at the original regular draw multiplier1. The presenter uses `Time.unscaledTime`; changing Unity's time scale therefore does not stretch these visual durations. It does not implement the source client's accelerated multi-draw tracker, fast-draw game option, or custom card draw override spells.

Let `D` be the top bone of the visible deck before the draw. `remaining+1` reconstructs that count because the successful-draw event reports the count after removal. The source thickness selection uses strict thresholds of count/26: above.75 Full, above.5 75%, above.25 50%, otherwise25%. The one-card and25% top bones coincide in this source prefab.

The source upward offset is `(-.3,3.6,0)`. Friendly reveal bone `F=(-4.3,8,-5.303798)` and opponent reveal bone `O=(-3.598883,2.072181,-2.346226)` include their source parents. Using the deck root without its top-bone/parent transforms gives a different path.

| Channel | Friendly draw | Concealed opponent draw |
| --- | --- | --- |
| Position | `[D,D+offset,F]`,1.5s, source ease33 | `[D,D+offset,O,current hand target]`,1.75s, in/out quartic |
| Initial Euler | `(270,270,0)` | `(270,270,0)` |
| Rotation | To`(0,0,357)`,1.35s after.15s, out-exponential | To retargeted hand Euler,.7s after.8s, in/out cubic |
| Scale | `.88→1`,.75s after.15s, out-exponential | `.88→hand scale`,.7s after.8s, in/out quintic |
| Final friendly hand movement | Position/rotation/scale: .5s, in-exponential, beginning at1.5s | Included in the four-node path |
| Completion |2s|1.75s|

The friendly phase at1.5..2s reproduces the source SLOW hand transition's duration and easing, with the endpoint adapted to the live UI fan. Rotation and scale channels explicitly reach their targets at completion. The hand view remains hidden and noninteractive throughout its visible draw; the friendly presentation participates in the current action-busy guard until completion.

## Original mathematical behavior

The source curve names alone are insufficient. Easing enum33 is called `easeInSineOutExpo`, but its installed implementation compares `value > start/2`. The path samples it with start0 and end1, so for positive normalized time it is the ordinary out-exponential function. There is no added sine/exponential blend.

```
outExpo(t) = 1 - 2^(-10*t)
inExpo(t)  = 2^(10*(t-1))
friendlyPathEase(0) = 0
friendlyPathEase(t>0) = outExpo(t)
```

These formulas retain the actual endpoint residuals: outExpo(1)=1023/1024 and inExpo(0)=1/1024. Source ordinary transform channels apply their exact target at completion; source path evaluation has no corresponding final position snap. Friendly reflow consequently begins at the spline sampled with1023/1024, preserving that small difference.

The path is a uniform Catmull–Rom spline. For nodes`P0..Pn`, controls are padded with`2P0-P1` and`2Pn-P(n-1)`. Eased parameter is divided equally among the node-to-node segments; there is no arc-length resampling or Bezier substitution. Each segment evaluates:

```
.5 * ((-a+3b-3c+d)*t^3 + (2a-5b+4c-d)*t^2 + (-a+c)*t + 2b)
```

Rotation interpolates the shortest endpoint on each Euler axis, then calls`Quaternion.Euler`. It does not use quaternion Slerp. The opponent's symmetric in/out cubic, quartic, and quintic curves are provided by`InOutPower`.

## Retargeting and limits

The front snapshots use HearthClone's current native layered card UI: source meshes/UVs have already been flattened into separate composited layers. During flight those layers form a card plane; they are not the full original depth-separated actor or shader stack. Original source world motion and the recovered camera provide perspective and the flip. Existing180×270 UI fitting, the source card center correction, and the XY-to-XZ plane rotation adapt the visuals to that motion. Original3D material reflections, lighting responses, bevel depth, particles, and arbitrary client animation events are not restored by this component.

The final hand point, scale, and orientation are calculated from the current live CardView and board-camera projection. This keeps the motion aligned with the existing UI at different screen sizes and fan positions. It deliberately does not replace the hand fan with original `ZoneHand` geometry/spacing, reproduce the source anisotropic hand scale, or claim identical final source actor poses. The source reveals and deck bones currently use the audited desktop prefab pose; alternate screen-adapter poses and other boards' bespoke draw bones are not selected dynamically.

Multiple successful draws can fly concurrently; original global friendly-draw serialization, blocker-spell waits, and the source stand-in interaction milestone at.3s into hand reflow are not ported. The current AI also still resolves its turn synchronously. It may play a newly drawn opponent card before that hand is rebound, in which case no persistent view exists for the concealed draw. AI turn sequencing has not been slowed or redesigned for this presentation pass.

## Installation and validation scope

`Tools/InstallOriginalCardMotion.cs` attaches one presenter to each hand display in`Assets/Scenes/HearthstoneTable.unity` and binds the recovered board camera. Run through the existing editor API in edit mode after reviewing/saving the active scene; the installer refuses a dirty scene. It saves the playable scene and does not import the inspected client DLLs.

The math file has passed an independent compiler check against the installed UnityEngine assembly. Integration and visual verification are performed in the active Unity editor; this document does not claim a standalone build, a complete match simulation, or pixel-identical rendering. Check a normal post-mulligan friendly draw, a concealed opponent card retained in hand, hand refresh during flight, screen-size changes, removal/reset cleanup, and restored drag/target controls after completion. Continue to run existing card, interface, and30-card starting-deck checks alongside the presentation checks.
