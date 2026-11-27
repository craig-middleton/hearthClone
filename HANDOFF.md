# HearthClone — safe stopping checkpoint

Read AGENTS.md, this file and PROJECT_STATUS.md before editing. Use git log -1 for
this containing checkpoint. Baseline before this work:2243ae4. Craig has only2%
weekly credits and requested a safe stop. No further feature work until resumed.
Current effort Ultra; recommend High for playtest/layout review after resuming,
Ultra before complex source shader/sequence reconstruction. AGENTS.md now records
this as a standing preference across all sessions/compactions. Craig changes it.

## Completed changes

- Original turn rope installed: source10-joint skin, spiral/rollout curves, particles,
  texture/mask inputs and exact WAVs.75-second configurable local turn limit; rope
  appears in final20seconds. Source client uses server TIMEOUT; special opening,
  AFK and card-modified limits are not implemented. Debug30 is not used.
- Source .87s birth, correct controller speeds2/1/2, linear spark/mask movement over
  remaining time after birth. Real unscaled deadline, source fuse volume.4 and fades.
  Original standard settings disable ticks (tick assets retained). Friendly timeout
  uses safely baked ENDTURN_YOUR_TIMER_DONE pose/atlas/visibility; no double flip.
  Export stop marker1s differs from Unity's effective.85s clip length; adapter uses
  actual imported length. Opponent timeout doesn't use friendly button animation.
- Timeout cancels pending powers/combat/held-card ghost, locks new input, waits for
  already accepted VFX, then advances exactly once. Menu/mulligan/game-over/restart
  and synchronous AI return have explicit timer resets. No original client code runs.
- Code audit corrections: zero-attack eligibility; owner-end Freeze semantics;
  AI stops after lethal and sweeps between cards; win/draw before HUD refresh and
  before further AI power; invalid/off-board/dual targets rejected before mutation.
- Drag requires accepted left pointer; foreign pointer/button cannot resolve it.
  CardView alone owns fan lock. Escape/right-click cancel through EffectTester.
- Native combat snapshots own separate original model stages and portrait materials.
  Disabling/deleting a ghost no longer destroys the live model. Actual attacker hides
  while its snapshot moves, survives refresh, and restores; lethal spell snapshots
  keep their model/material to impact. Existing attack trajectory is unchanged.

## Verification and stopping state

Fresh Unity compilation passed after all production edits. New focused verifiers:
VerifyCoreRules(11groups), VerifyGameManagerLifecycle, VerifyCardDragInput,
VerifyTurnTimer, VerifyNativeTurnTimer, VerifyNativeCombatSnapshots. Actual results
and final scene/Console state are recorded in Docs/Recovery/Code-audit.md.
Earlier in this session, native spells/cards/starting decks/match controls passed.
Frontend verifier now waits for original menu intro instead of clicking disabled
buttons; recheck results in audit document. No full manual match or standalone build.

Source evidence: Docs/Recovery/Turn-timer-source-audit.md. Assets under
Assets/_Project/Art/NativeTurnTimer with safe source prefab/configuration/manifest.
Installer destroys its disposable actor BEFORE saving scene; never save preview
fixtures. PreviewNativeTurnTimer uses injected remaining time only in Play.
Tools/editor_api.py privately reads ignored localhost credentials; never print them.
Wait for script recompile_status COMPLETED before dynamic verification: running
immediately after triggering compilation can execute stale types or lose connection.

## Remaining work — do not call this exact parity

Runtime89 C# files,20 custom shaders and6 Editor helpers inspected across this audit;
historical tools reviewed selectively, not all78 scripts. See Code-audit.md.
Two confirmed lower-priority resource ownership issues deferred at requested stop:
SpellBurstFactory.cs344 creates an unowned per-burst Material; UIParticleBurstRenderer
allocates a RenderTexture without component teardown release. Fix on resume.

Original shader bodies are unavailable. Rope UV2 mask kernel (horizontal doubling
inferred from endpoints/gradient) and unlit response are compatibility. Original
shadow mesh/transform retained but shadow alignment isn't certified; original camera
shake/compositor/ducking kernels unported. Source smoke/audio/button motion present.

Wider exact-game gaps remain: generic attack/power/area-spell fallback motion and
bursts, hero idle/flash; only Fireball/Frostbolt original VFX installed; partial
lighting/bloom/distortion; other Taunt sequences; original intro/deal/menu motions;
chosen minion placement; prototype/custom card balance. Fireball remains3damage,
Frostbolt1/minionfreeze; don't silently rebalance approved existing content. User
still wants these closer to the original; prioritize broad visible gains next.

Prior approved work preserved: native board/mana recess, deck pockets, cards/source
fonts. Beast/Fire labels matched, stat gems above tribe plate. All47 card assets
already share approved FranklinGothic body typography; original rich-text retained.

## Workspace/source access

Project /home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone
GitHub https://github.com/craig-middleton/hearthClone.git main with Git LFS.
Unity6000.6.9f1. Open through Hub; never launch from a restricted tool shell.
Never AnimationClip.SampleAnimation exported mixed bindings (known native crash).
Original assets external: if-x20/recovery/{Box-complete-source,Interface-native-source,
ORG-native-source}, Desktop/{Hearthstone Export,Hearthstone Extraction,Hearthstone
Card Database}; installed client in Steam compatdata2505554610. Fresh Git clone
alone doesn't include exports/Editor access. Managed client inspected as metadata
only, never executed. Network permission session may need renewal for localhost.

Unrelated pre-existing files preserved/excluded: RecoveredDecks/Deck_75_mesh.mat,
Assets/_Recovery.meta, Docs/Previews/Native-hero-powers-first.png.
