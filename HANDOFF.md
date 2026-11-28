# HearthClone — fresh-chat handoff

Prepared 28 November 2026. Burst resource-lifetime fixes and focused review completed.
Read this and AGENTS.md first. Use git log -1 to identify the containing checkpoint;
this handoff is included in the same main commit as the code and verifier.
Previous game/code baseline: 7b6abfb (rope and production-code audit fixes), followed
by documentation-only 79cdc0e. Push verification is performed after the commit;
this document does not claim its own future push has already succeeded.

## Incoming-chat instructions

1. Use the existing project folder below; do not create another clone or checkout.
   Read AGENTS.md, this file, PROJECT_STATUS.md, Docs/Recovery/Code-audit.md, then
   relevant PROJECT_HISTORY.md sections. Inspect git status before any edits.
2. Start with a short status and next-step recommendation. Craig resumed on
   28 November and authorised the focused review and two resource-lifetime fixes.
   Those are complete. Next complex source reconstruction awaits an Ultra switch;
   pause for Craig to switch and say continue before starting that dependent work.
3. Recommend High for the initial review/playtest and routine fixes. Recommend
   Ultra before complex source shader/animation reconstruction. At useful switches,
   explain why, stop dependent work and wait for Craig. This standing preference is
   in AGENTS.md and must persist across chats/compactions; agents cannot switch it.
4. Use original Hearthstone assets, sounds, animation curves and transforms. Custom
   compatibility rendering is authorized, with limits disclosed. No invented artwork,
   substitute motion or full-board background overlays. Preserve approved board,
   mana recess, deck pockets and card typography. Do not silently rebalance content.
5. Give brief progress updates explaining what/why. Significant work goes directly
   to main/GitHub; overwrite this ONE HANDOFF.md in the SAME commit every time.
   Stage only intended files. Do not discard the three unrelated local files below.

## Next actions / effort switch

- High was recommended for this resumed review and the routine lifetime fixes.
  The agent cannot inspect or change Craig's actual effort setting.
- Baseline rope/deadline/card-cancellation/native-snapshot focused checks passed;
  the two resource leaks are now fixed and verified. No full manual match claimed.
- Recommend Ultra next for original attack sequence recovery: trace the source
  transforms, curves, timing, sounds and effect dependencies, then replace the
  existing generic attack presentation with an honestly scoped source adapter.
  Stop for Craig to switch and say continue before that investigation/implementation.
- Prioritize broad visible improvements. Power/area-spell presentation and shared
  card rendering also remain. Return to High for routine review/layout fixes;
  remind Craig and pause at useful effort-switch boundaries.
- Do not rerun historical broad card/board installers: they can overwrite approved
  bindings. Use the targeted installer/check documented for each subsystem.

## Current local access and verification limits

On 28 November the authenticated Unity API was used for actual runtime checks in
HearthClone, Unity 6000.6.9f1. Network permission was granted for localhost/GitHub
in this chat; another chat may need its own permission. Final state: Play stopped,
auto-tick off, active HearthstoneTable loaded/not dirty, Console 0 errors/0 warnings.
No scene or source-art changes. Trust repository evidence over presumed chat memory.

## Completed changes

- Resumed resource cleanup: SpellBurstMaterialOwner owns each fallback burst's
  runtime Material and destroys it with the particle object (natural stop or
  interrupted cleanup). Renderer uses sharedMaterial to avoid hidden material copies.
- UIParticleBurstRenderer detaches only its own camera/image bindings, releases GPU
  storage and destroys its RenderTexture on resize and component teardown. External
  replacement bindings are preserved. No new artwork, motion, timing or rebalance.
- VerifySpellBurstResources reproduced the pre-fix leaks and now passes all five
  builders, natural/forced cleanup, point reuse, region resize/release, point restore,
  component-only destruction with surviving camera/image, and external binding checks.

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

This resume: baseline VerifyTurnTimer, VerifyCardDragInput, VerifyNativeTurnTimer
and VerifyNativeCombatSnapshots passed. Fresh production compilation completed
without errors after the lifetime changes. Post-fix VerifySpellBurstResources,
VerifyNativeSpells (real Fireball/Frostbolt) and VerifyTurnTimer passed. These were
the checks rerun this resume; earlier broad audit checks below remain prior evidence.
A fresh source=screen mid-burn rope capture was inspected in this chat's work folder;
it is temporary external evidence, not a tracked deliverable or saved fixture.
No full manual match, standalone build, performance profile or physical audio review.

Prior checkpoint: fresh Unity compilation passed after its production edits. Focused verifiers:
VerifyCoreRules(11groups), VerifyGameManagerLifecycle, VerifyCardDragInput,
VerifyTurnTimer, VerifyNativeTurnTimer, VerifyNativeCombatSnapshots. Actual results
and final scene/Console state are recorded in Docs/Recovery/Code-audit.md.
Earlier in this session, native spells/cards/starting decks/match controls passed.
VerifyFrontEnd also passed after waiting for the original menu intro. Final saved
HearthstoneTable was clean Edit, auto-tick off, Console0errors/0warnings. The inspected
mid-burn Game-view is Docs/Previews/Native-turn-rope.png. No full manual match,
standalone build or user visual approval of this checkpoint is claimed.

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
The two resource ownership issues deferred at the previous stop are now fixed.
The new SpellBurstMaterialOwner adds one small production C# component; this resume
is not a repeat of the full 89-file audit or a certification that no other leaks exist.

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

Project root:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/outputs/HearthClone
Workspace root:
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20
GitHub: https://github.com/craig-middleton/hearthClone.git — main, Git LFS.
Scene: Assets/Scenes/HearthstoneTable.unity. Unity6000.6.9f1.
Editor binary: /home/craig-middleton/Unity/Hub/Editor/6000.6.9f1/Editor/Unity
Open through Unity Hub; never launch from a restricted tool shell (past preferences/
GI-cache failures). Never AnimationClip.SampleAnimation exported mixed bindings
(known native crash). Safely bake/evaluate specific original data channels.

Original source roots (external to Git):
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/Box-complete-source/ExportedProject/Assets
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/Interface-native-source/ExportedProject/Assets
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/ORG-native-source
/home/craig-middleton/Desktop/Hearthstone Export
/home/craig-middleton/Desktop/Hearthstone Extraction
/home/craig-middleton/Desktop/Hearthstone Card Database
Installed original client:
/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone

Selected playable assets/source manifests are tracked. A GitHub clone alone does
not supply external exports, this Editor or laptop access. Temporary /tmp audit
scripts/IL are not required handoff artifacts and may disappear; durable findings
are in Docs/Recovery. Original managed client is inspected as metadata only, never
executed/imported as gameplay logic. No alternate-model setup is needed.

## Editor commands / credentials

Run from the project root. Tools/editor_api.py validates the project and privately
reads the ignored Library/Pipeline descriptor/token. Never print, copy into a prompt
or commit credentials. A new environment may need network permission for localhost
and GitHub. Do not treat missing permissions as a Unity/code defect.

python3 Tools/editor_api.py get /api/editor_status
python3 Tools/editor_api.py 'script recompile'
python3 Tools/editor_api.py 'script recompile_status'
# Wait until status COMPLETED and compilationFailed=false before further commands.
python3 Tools/editor_api.py 'editor.playmode start'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":true,"interval-ms":0,"persist":false}'
python3 Tools/editor_api.py 'script run' '{"file":"Tools/VerifyTurnTimer.cs"}'
python3 Tools/editor_api.py 'capture game_view' '{"output-path":"Docs/Previews/review.png","source":"screen"}'
python3 Tools/editor_api.py 'editor.playmode stop'
python3 Tools/editor_api.py 'editor set_auto_tick' '{"enabled":false,"persist":false}'

Run each lifecycle verifier in its documented state; fixtures can leave a match/menu
changed. VerifyFrontEnd starts at the menu; its original intro initially disables
buttons. Preview fixtures are disposable: stop Play before normal tests/play and
never save them. Always inspect actual source=screen captures for visual claims.
No new tests are required merely for this documentation handover.

## Unrelated local files — preserve/exclude

Assets/_Project/RecoveredDecks/Deck_75_mesh.mat (tracked edit)
Assets/_Recovery.meta (untracked)
Docs/Previews/Native-hero-powers-first.png (untracked rejected early proof)

These remain the only pre-existing unrelated local changes and are excluded from
this resource-fix checkpoint. Prior game/audit work is in 7b6abfb. Incoming agents
must not discard or accidentally commit these files, or claim the whole tree clean.
