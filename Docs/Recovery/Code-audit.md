# Production-code audit and rope checkpoint

Original audit stopped at Craig's request because weekly credits were nearly exhausted.
On 28 November Craig resumed the focused review and deferred resource fixes below.
This remains a verified checkpoint, not a claim of exact Hearthstone parity.

## Scope

Read all89 production C# files:64UI,9Core,4Cards,10Effects,2AI. Reviewed all20 custom
shader sources and6 Editor helpers. Historical recovery/install/verification tools
were inspected selectively; all78 historical C# tools were not audited. Reading
code does not certify every asset parameter or exact original GPU/audio behavior.

## Corrected faults

| Area | Correction | Focused check |
|---|---|---|
| Attack rules | Zero attack, detached attackers, malformed/off-board targets and finished-match combat rejected | VerifyCoreRules |
| Freeze | Thaw at appropriate owner turn end; spent/summoning/Charge/re-freeze cases | VerifyCoreRules |
| Spell boundaries | Validate target/context before spending mana, removing cards or emitting events; None/Self defaults preserved | VerifyCoreRules |
| AI/result | Stop cards/powers after lethal; sweep each resolved card; commit win/draw before HUD refresh | VerifyCoreRules, VerifyGameManagerLifecycle |
| Drag | Left pointer ownership; foreign events inert; one fan owner; Escape/right cancel, ghost cleanup and late-release safety | VerifyCardDragInput |
| Native combat | Independent snapshot stages/materials, moving original model, live attacker hide/restore through refresh, lethal hold to impact | VerifyNativeCombatSnapshots |
| Turn deadline |75seconds configurable local policy; final20second rope; cancel/lock input at expiry, accepted VFX completes, exactly-once advancement and resets | VerifyTurnTimer |
| Rope/button | Original rig/curves/particles/audio/mask inputs; friendly-only timeout clip, no double flip; normal upright return | VerifyNativeTurnTimer |
| Frontend fixture | Wait for original intro to enable menu controls before simulated clicks | VerifyFrontEnd |

Freeze evidence: [Blizzard patch note](https://hearthstone.blizzard.com/en-us/news/11868609).
The supported model has one attack plus Charge; this is not a new Windfury/freeze-
hero implementation. Source client metadata confirms tags but not server thaw logic.

## Actual verification

After final production compilation, Unity passed VerifyCoreRules(all11 groups),
VerifyGameManagerLifecycle, VerifyCardDragInput(including temporary keyboard/mouse),
VerifyNativeCombatSnapshots(real attack and lethal original Frostbolt), VerifyTurnTimer
(real Fireball busy expiry), VerifyNativeTurnTimer and VerifyFrontEnd.

Earlier in the same session, native spells/cards/starting decks/match controls passed.
No claim that those earlier checks were all rerun after every audit change. New core
boundary tests exercise legal casts/attacks as well as rejection; live snapshot/timer
checks exercise both native spells after the audit edits. Fresh compile clean.
Final saved HearthstoneTable is stopped in clean Edit, auto-tick off, Console0errors/
0warnings, no disposable source importer actor. No full manual match, standalone
build, physical speaker listening, performance profile or exact-parity approval.

Inspected actual Game-view Native-turn-rope.png shows mid-burn original rope/sparks
aligned across the board. Preview uses a disposable injected clock, not production
75-second waits. Stop Play afterward. No fixture saved. Source/model mask/particle
binding tests supplement this image; they do not prove original shader equivalence.

## Deferred resource issues resolved on 28 November

- SpellBurstFactory now attaches SpellBurstMaterialOwner to each fallback burst.
  It owns only the newly created runtime Material and destroys it with the particle
  object, including ParticleSystem natural-stop destruction and forced interruption.
  Assigning sharedMaterial avoids implicit renderer-owned copies. Existing fallback
  presentation is unchanged; current native spell routes normally bypass it.
- UIParticleBurstRenderer now detaches only bindings that still reference its own
  texture, calls Release, destroys the object and clears its field on resize and
  OnDestroy. Camera/image may survive teardown; external bindings remain untouched.
- VerifySpellBurstResources first reproduced the leaks (all five burst builders,
  natural stop, GPU allocation retained during resize, teardown and rebound renderer).
  It passes after the fixes: materials die on forced and natural cleanup, unchanged
  point framing reuses a texture, region/point resize releases old allocations,
  component-only teardown clears owned bindings, and external replacement survives.
  Fixtures clean themselves even on failure; no scene or source art was saved.

Baseline checks this resume: VerifyTurnTimer, VerifyCardDragInput,
VerifyNativeTurnTimer, VerifyNativeCombatSnapshots all passed before code changes.
Fresh production compilation completed without errors after edits; post-fix checks
VerifySpellBurstResources, VerifyNativeSpells (real Fireball/Frostbolt) and
VerifyTurnTimer passed. Other historical checks above were not rerun this resume.
A fresh source=screen mid-burn rope image was inspected (temporary chat work folder,
not a tracked asset). Final HearthstoneTable is loaded/not dirty, Play stopped,
auto-tick off, Console 0 errors/0 warnings. No full manual match, build, audio listening
or performance profile claimed. One small owner component was added to the previously
89-file production audit; this is not a new exhaustive code audit.

## Remaining exact-game gaps

Existing synthetic attack motion, generic power/projectile/burst/area-spell paths,
hero idle/flash and damage-number timing still need original-source replacement.
Only Fireball/Frostbolt original spell VFX are installed; compiled shader/noise/glow,
bloom/distortion/compositor/shake and full lighting remain partial. Grizzly-only
summon/Taunt study does not cover all cards or remaining shield/death/VO sequences.
UI-projected actors/text raster/blur are compatibility. Original hero intro/opening
deal and menu/mulligan motion/composition remain incomplete. Chosen minion placement
is unimplemented. Existing custom/prototype card balance remains, including Fireball3
and Frostbolt1/minion-only freeze. User's30inclusive/Coin deck rule remains unchanged.

Rope uses standard local75seconds, not the original server's opening/AFK/card-modified
policy. Its source10-joint rig/transforms/UVs/particles/WAVs are preserved; missing
mask kernel's doubled horizontalUV2 is an explicit compatibility inference. Shadow
alignment, original lighting, camera shake and mixer ducking aren't certified.
Details: Turn-timer-source-audit.md and NativeTurnTimer source configuration/manifest.

Standing workflow: recommend High for playtest and routine layout; Ultra for complex
source reconstruction. Pause at a switch and wait for Craig. High was recommended for this review/fix pass;
actual UI effort cannot be inspected by the agent. Recommend Ultra next for original
attack sequence recovery and pause for Craig to switch and say continue. Rolling HANDOFF is in this commit.
