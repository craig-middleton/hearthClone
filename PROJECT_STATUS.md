# HearthstoneClone - Project Status

## Working Preferences
- Craig wants a 2-paragraph explanation after each new/updated code block, describing what it does and why.
- For code changes, always provide the full rewritten class/file, not just a diff/snippet — Craig pastes a complete file each time rather than manually merging partial additions.
- For hearthClone architecture, avoid singletons where possible — prefer explicit constructor/field-passed references (as the project already does throughout) over globally-reachable static instances, to keep dependencies visible and avoid Unity singleton pitfalls (script execution order, DontDestroyOnLoad issues, creeping responsibility).

_Last updated: 2026-08-15 (session 26 — mulligan panel repositioned, hero avatars with idle animation added, then a full Claude Code codebase review surfaced 4 real bugs which were fixed and reviewed together in this chat)_

## Tooling note: Claude Code now in the workflow
Craig has Claude Desktop installed on Ubuntu and has started using its **Code** tab (Local environment, pointed at the `hearthClone` project folder) as an additional way to work on the codebase, alongside this chat-based workflow. Claude Code reads/edits files directly on disk — no more copy-pasting needed when working that way. Both workflows are expected to continue being used; this file should stay the single source of truth regardless of which one makes a given change, so any session (chat-based or Claude Code) should update it. **Discipline that keeps this safe**: whoever is driving (this chat or Claude Code) should paste/read the *actual current* file before editing it, never assume memory of a file's contents is accurate — this already caught real problems (missing `avatarImage` field, background/music code not actually saved) and is the main safeguard against the two tools silently drifting out of sync with each other.

## How to use this file
Paste the contents of this file at the start of any new Claude chat to get instant context on the project. Update it at the end of each working session (ask Claude to update it, or do it yourself) so it never goes stale.

## Card Artwork — DONE this session
Full pipeline built and verified working end-to-end:
- **Art source**: Leonardo AI (free tier), JPG downloads work fine (no transparency needed since art fills the card's background area). Aspect ratio guidance used: portrait, close to 3:4, e.g. 768×1024 or similar.
- **Import**: drag JPG into `Art` folder → select it → Inspector → **Texture Type** → `Sprite (2D and UI)` → Apply.
- **Assignment**: drag the imported sprite onto the relevant `CardData` asset's pre-existing `Artwork` field (this field existed since the very first `CardData.cs` but was unused until now).
- **Code (both `CardView.cs` and `MinionView.cs`)**: each gained a new `Image artworkImage` field; the existing shared text-writing helper (`WriteCardText()` in `CardView`, inline in `MinionView.SetMinion()`) now also sets `artworkImage.sprite` when `card.artwork`/`minion.Artwork` is non-null, or sets `artworkImage.enabled = false` to cleanly hide the image slot when there's no art — so cards/minions without generated art still render correctly as text-only, no broken/blank sprites.
- **`Minion.cs`**: gained an `Artwork` field (`Sprite`) and a new optional constructor parameter, since board minions are runtime objects separate from `CardData` and needed their own copy of the reference. `PlayerHand.PlayCard()` passes `card.artwork` through when constructing a `Minion`, mirroring how `card.hasTaunt` was already threaded through the same constructor call.
- **Editor setup (done for both `CardView` and `MinionView` prefabs)**: added a new `Image` child (`ArtworkImage`) positioned as the *first* child (so it renders behind the name/stats text, not covering it), wired to the new script field.
- **Bug hit and fixed this session**: on `MinionView`, the `Artwork Image` field was left unassigned (`None`) after adding the child — this is the same "empty field fails silently" pattern hit several times before (Button/Image references need explicit dragging, the code can't infer it). Board minions showed a blank white box (Unity's default unconfigured `Image` color) instead of either the real art or a clean hidden fallback, until the field was actually wired.
- **Confirmed via screenshot**: Goblin (the only card with generated art so far) displays correctly on both the hand card and the board minion, on both players' sides; cards/minions without art (Wisp, Shieldbearer, etc.) correctly show as plain text with no visual glitch.
- **Only one card has real art so far (Goblin)** — the rest of the 15-card pool still needs art generated the same way if Craig wants full coverage. No further code/Editor work needed for additional cards — just repeat the generate → import → assign steps per card.

## Board Background & Music — DONE this session
Full pipeline built and verified working end-to-end, following the same "list + random pick" shape as `cardPool`:
- **Background art source**: same tools as card art (Leonardo AI etc.), but **landscape/wide orientation** this time (e.g. 1536×1024 or 1216×832), prompted for a framed "game board" feel (bordered edges, empty foreground for game pieces, no text) — 3 backgrounds generated and imported. Style prompts used: cartoon/painted fantasy, warm saturated colors, whimsical (deliberately generic/genre-level rather than referencing any specific existing game's exact visual identity).
- **Music source**: AI music generators — Mubert (ambient/background specialist), AIVA (structured orchestral/game-soundtrack), Sonauto (unrestricted free tier) all viable free options. Prompt used: whimsical fantasy tavern instrumental, no vocals, seamless loop, upbeat but not distracting.
- **Scene setup**: `BoardBackground` — a new full-screen `Image` child under `GameCanvas`, positioned as the **first** child (renders behind everything), Rect Transform stretched to fill (`Alt+Shift` + stretch-both anchor preset), **Raycast Target unchecked** proactively (learned from the earlier `BoardPanel` click-blocking bug — a background should never intercept clicks). An `Audio Source` component added directly onto the `EffectTester` GameObject, with `Play On Awake` and `Loop`... **`Loop` is set by the script itself** (`musicSource.loop = true` in `SetRandomMusic()`), so only `Play On Awake` needs manually unchecking in the Inspector (playback is script-controlled, not automatic).
- **Code (`EffectTester.cs`)**: `boardBackgroundImage`/`boardBackgrounds` and `musicSource`/`musicTracks`/`musicVolume` fields, `SetRandomBoardBackground()` and `SetRandomMusic()` methods, both called at the very top of `Start()`.
- **Bug hit and fixed this session**: the background wasn't varying between Play-mode restarts — root cause was Unity's default `Random` seeding, which can produce the same first result on rapid consecutive Play-mode restarts within the Editor (a known Unity quirk, not a real logic bug). Fixed with an explicit `Random.InitState(System.DateTime.Now.Millisecond + System.Environment.TickCount)` call at the top of `Start()`, shared by both the background and music picks. A debug log was added to `SetRandomBoardBackground()` showing the picked index/sprite name, to make this kind of issue directly verifiable in the console rather than relying on eyeballing a visual that might look similar between two different picks.
- **Confirmed via screenshot**: full game scene rendering with a proper bordered fantasy-arena-style background behind all UI, cards/boards/panels all correctly layered on top and still fully clickable (Raycast Target fix held).

## Project Goal
A Hearthstone-style card game built in Unity, single-player vs AI, using C# with a data-driven card/effect system (ScriptableObjects). Developed on Ubuntu using Unity + VS Code, version controlled via Git/GitHub (with Git LFS enabled for art/audio).

**Unity version: 6.5 (60000.5.2f1).** Use TextMeshPro (TMP) for all UI text, not legacy `UnityEngine.UI.Text`.

## Tooling Setup (Done)
- Unity Hub + Unity LTS installed via AppImage
- VS Code with C# Dev Kit + Unity extension
- .NET SDK installed (for VS Code C# debugging, separate from Unity's own runtime)
- Git + GitHub connected via Unity Hub's built-in integration, authenticated via `gh auth login`
- Git LFS enabled for binary assets (art, audio)
- Repo: `github.com/craig-middleton/hearthClone`

## Architecture Decisions
- **Folder structure** under `Assets/_Project/`: Scripts, ScriptableObjects, Prefabs, Art, Scenes
- **Card/effect model**: data-driven via ScriptableObjects — cards reference reusable `CardEffect` assets instead of each card having bespoke code.
- **Assembly Definitions** (`.asmdef`) enforce one-way dependency flow: `Core` (no deps) ← `Effects` (deps: Core) ← `Cards` (deps: Core, Effects) ← `AI` (deps: Core, Cards, Effects) ← `UI` (deps: Core, Cards, Effects, **AI**). `Effects` must NOT reference `Cards`. **Corrected this session** — this was previously (mis)documented as `AI`/`UI` being parallel leaves; in reality `UI` also references `AI` directly (`EffectTester` constructs and calls `AIController`), confirmed via both the asmdef reference list and the `using HearthstoneClone.AI;` in `EffectTester.cs`. Not circular, compiles fine — just was inaccurately described here previously.
- **No singletons** — every class is constructed explicitly and handed the exact references it needs.

## Code Written So Far

| File | Location | Purpose |
|---|---|---|
| `CardEffect.cs` | `Scripts/Effects/` | Abstract `ScriptableObject` base; `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Deals damage to a Target, logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` | Calls `target.GainMana(manaAmount)`. Powers The Coin. |
| `CardData.cs` | `Scripts/Cards/` | ScriptableObject: `cardName`, `description`, `artwork` (Sprite — now actively used, see Card Artwork section), `manaCost`, `cardType`, `attack`, `health`, `hasTaunt`, `onPlayEffect`, `targetsSelf`. |
| `Minion.cs` | `Scripts/Core/` | Runtime board minion. `HasSummoningSickness` (true from construction), `HasAttackedThisTurn`, `CanAttack` (computed), `ResetForNewTurn()`, `HasTaunt` (optional constructor param, `false` default), `Artwork` (Sprite, optional constructor param, `null` default). All new fields use optional constructor parameters so existing call sites keep compiling unchanged as each was added. |
| `Player.cs` | `Scripts/Core/` | Health, mana (current/max), board minions list, `FatigueDamage`, `HasUsedHeroPowerThisTurn`. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Holds both `Player`s; `GetOpponent()`, `RemoveDeadMinions()`, `GetTauntMinions(Player player)` (returns that player's Taunt minions — no separate "alive" filter needed since dead minions are already stripped by `RemoveDeadMinions()`). Used identically by both the human attack path (`EffectTester`) and the AI (`AIController`) to enforce Taunt consistently on both sides. |
| `GameContext.cs` | `Scripts/Core/` | Holds a real `Board` reference. |
| `Target.cs` | `Scripts/Core/` | Points to a `Player` or `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)`. |
| `Combat.cs` | `Scripts/Core/` | Static class, `TryAttack(Minion attacker, Target target, out string failReason)` — validates `attacker.CanAttack`, applies damage to the target, strikes back at the attacker if the target is a minion (simultaneous damage). Static/stateless — a pure function, not a singleton. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana progression. A private `StartTurnFor(player)` wraps mana refill, `ResetMinionsForNewTurn(player)` (clears summoning sickness/attacked flags), and `player.HasUsedHeroPowerThisTurn = false` — called from both `StartGame()` and `EndTurn()`. Guarantees a minion summoned on one turn stays sick through the opponent's whole turn, clearing only on the controller's own next turn — matching real Hearthstone timing with no separate counter needed. |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper. Builds decks/hands, asymmetric opening hands + Coin, full mulligan flow, manual control mode toggle, click-to-attack combat wiring, `FaceView` health/mana display, win condition, per-turn draw, Taunt enforcement, minimal Hero Power, random board background + music (`boardBackgroundImage`/`boardBackgrounds`, `musicSource`/`musicTracks`/`musicVolume`, both picked via a shared `Random.InitState(...)` seed at the top of `Start()`). **Four real bugs found via a full Claude Code codebase review this session, all fixed and reviewed together in this chat**: (1) `OnCardClicked()`/`OnOpponentCardClicked()` never called `CheckWinCondition()` on a successful play — a lethal spell let the game continue instead of ending; both now call it, matching every other mutating handler. (2) `SetRandomBoardBackground()`/`SetRandomMusic()` were called *after* the `cardPool` empty-check `return`, producing a silent black screen with no audio instead of a visible board with a warning if the pool was ever misconfigured; both calls moved to the very top of `Start()`. (3) `endTurnButton`'s click listener was registered *inside* `OnConfirmMulliganClicked()`, and `ConfirmMulliganButton` remained active and clickable for the entire match (a direct Canvas child, not inside `mulliganPanel`) — clicking it twice registered the listener twice, silently doubling every subsequent turn. Fixed by registering the listener exactly once in `Start()` (safe since `OnEndTurnClicked()` already gates on `mulliganComplete`), adding a matching guard to `OnConfirmMulliganClicked()`, and explicitly deactivating the button's GameObject after use. **Not permanent** — delete/rename once real gameplay loop exists. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Core.Player` with `Deck`/`Hand`. `Shuffle()`, `DrawOpeningHand()`, `AddCardToHand()`, `MulliganCard()`. `DrawCard()` deals escalating fatigue damage when the deck is empty; burns a newly-drawn card if hand is full (10). `PlayCard()` blocks Minion plays if the board is full (7, checked before mana/hand are touched), and passes `card.hasTaunt`/`card.artwork` through to the `Minion` constructor on summon. **Bug fixed via Claude Code**: the effect-execution line silently skipped a card's effect with no log if a target wasn't provided — now logs a warning instead. Taunt and artwork fully verified working; fatigue/board cap not yet stress-tested (need a long enough game to trigger either). |
| `CardView.cs` | `Scripts/UI/` | Displays one card. Two setup modes sharing a private `WriteCardText()` helper — `SetCard()` (play mode) and `SetCardForMulligan()` (toggle-select mode with dim/highlight visual). **New this session (artwork)**: a new `artworkImage` field; `WriteCardText()` now also sets `artworkImage.sprite = card.artwork` when present, or `artworkImage.enabled = false` to cleanly hide it when a card has no art — confirmed working for both display modes since both share the same helper. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, reused for both hands. |
| `MinionView.cs` | `Scripts/UI/` | Displays one board minion. Clickable (`Button`), `minionBackground` (Image) for selected/attack-eligibility tinting, `nameText` appends `" (Taunt)"` for Taunt minions. `SetMinion()` takes optional `clickCallback`, `isSelected`, `showAttackEligibility` (all defaulted so old call sites still compile). **New this session (artwork)**: a new `artworkImage` field, same pattern as `CardView` — shows `minion.Artwork` if present, hides itself if not. **Bug hit and fixed this session**: the `Artwork Image` field was left unassigned in the prefab Inspector after adding the child, causing a blank white box instead of either art or a clean hidden fallback — fixed by dragging the `ArtworkImage` child onto the field. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard()` — takes optional `onMinionClicked`, `selectedAttacker`, `showAttackEligibility`, passed straight through to each `MinionView.SetMinion()` call. Generic, reused for both boards via two separate instances. |
| `FaceView.cs` | `Scripts/UI/` | The visible player health + mana display. `SetPlayer(Player, Action<Player> clickCallback)` writes `"{PlayerName}: {Health} HP\nMana: {CurrentMana}/{MaxMana}"` and wires a `Button` click to the callback — doubles as the attack target for face damage. **New this session**: `avatarImage` field for a static hero portrait (assigned once, no runtime lookup needed since it doesn't change mid-game); a procedural idle animation in `Update()` — sine-wave "breathing" scale pulse + slow horizontal sway, base scale/position captured on first `SetPlayer()` call, four tunable fields. Confirmed working: both hero portraits (Warrior for Player One, Spellcaster for Player Two) rendering next to their health/mana displays. **Flagged by Claude Code, not yet addressed**: base position/scale capture happens inside `Start()`, potentially before the first Canvas layout pass — if it lands pre-layout, the avatar could end up permanently offset with no error. Also, the two avatar `Image`s are set up inconsistently in the scene (one stretch-anchored, one point-anchored), and both currently have `Raycast Target` checked (should be unchecked per the established habit for decorative images). |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan()`, `TakeTurn()` (card-play loop + attack phase). Attack loop checks `board.GetTauntMinions(opponent)` before choosing a target — attacks the first Taunt minion found if any exist, otherwise face. **Bug found and fixed this session (Claude Code review)**: `board.RemoveDeadMinions()` previously ran only once, after the entire attack loop finished — meaning if the AI's first attacker killed a Taunt minion, it remained in `BoardMinions` (`GetTauntMinions()` has no `IsDead` filter, by design, since it assumes corpses are already stripped) for the rest of the loop, so subsequent attackers would target and "attack" the corpse. Fixed by moving `RemoveDeadMinions()` inside the loop, called after each individual attack — matching how the human path (`EffectTester.ResolveAttack()`) already worked. Also added an early `break` if the opponent's `Health <= 0`, so the AI stops attacking once it's already won. |

## Test Assets Created / In Progress
- Original 5: `TestCard_Wisp` (1, 1/1), `TestCard_Goblin` (2, 2/2), `TestCard_RiverCroc` "River Crocodile" (3, 2/3), `TestCard_Fireball` (4, Spell → `Effect_Deal3Damage`), `TestCard_Boulderfist` (5, 4/4).
- `TestCard_Coin` "The Coin" (0, Spell, `targetsSelf = true` → `Effect_GainMana1`) — kept OUT of `Card Pool`, granted only via `EffectTester`'s dedicated `Coin Card` field.
- `Effect_Deal3Damage` (damage = 3), `Effect_GainMana1` (manaAmount = 1).
- **Completed this session** — 10 more unique cards created, bringing `Card Pool` to 15 total entries (confirmed via Inspector screenshot) × `Copies Per Card = 2` = a real 30-card deck, matching Hearthstone's max-2-copies deckbuilding rule rather than just raising the copy count on the original 5:
  - `TestCard_Murloc` (1, 1/1)
  - `TesCard_Watchman` (2, 3/1) — **note the typo in the asset name** ("Tes" not "Test"), purely cosmetic, doesn't affect function, left as-is at Craig's discretion to fix later
  - `TestCard_Shieldbearer` (3, 1/5)
  - `TestCard_Warhorse` (3, 3/3)
  - `TestCard_ArcaneBolt` (3, Spell, `targetsSelf = false` → `Effect_Deal3Damage`)
  - `TestCard_Bear` (4, 3/6)
  - `TestCard_MagePupil` (4, Spell, `targetsSelf = true` → `Effect_GainMana1`)
  - `TestCard_ChargingRhino` (6, 5/5)
  - `TestCard_StoneGuardian` (6, 4/8)
  - `TestCard_AncientColossus` (7, 7/7)
  - No code changes were needed for this — `BuildDeck()`/`Copies Per Card` already handled any pool size. **Not yet playtested** — a fresh Play session drawing from the real 30-card deck hasn't been confirmed error-free yet (see Next Steps).

## Verified Working
- **Board visuals** (session 9), **AI turn logic** (session 10), **opponent board visualization** (session 11), **shuffled decks + draw + opponent hand display** (session 12), **asymmetric opening hands + Coin + multi-card AI turn** (session 13), **full mulligan system, both sides** (session 14) — all previously confirmed.
- **Manual control mode (this session)**: toggle exists and Player Two's hand does become clickable when enabled. **Turn-gating bug found via direct testing** (Craig played Player Two's Coin during Player One's turn) — root cause diagnosed (missing `CurrentPlayer` check) and fix written, but **not yet re-tested after the fix was applied** — confirm next session that turn order is now properly enforced in manual mode.

## Current Blocker / Last Thing Worked On
**Session 26 (this session)**: mulligan panel repositioned (bottom-stretch anchor to match `HandPanel`, `Child Alignment: Middle Center` — cards now correctly bottom-centered, not bunched left); hero avatar portraits added to both `FaceView`s with a procedural idle animation; then Craig ran a full Claude Code codebase review, which surfaced **4 real, confirmed bugs** — all fixed and reviewed together in this chat (see `EffectTester.cs`/`AIController.cs` rows above for full detail):
1. Spell kills didn't end the game (`CheckWinCondition()` missing from card-play handlers) — **fixed, not yet re-playtested**.
2. AI could attack a dead Taunt minion mid-loop (`RemoveDeadMinions()` only ran once, after the whole loop) — **fixed, not yet re-playtested**. This is very likely why AI-vs-Taunt enforcement was never successfully confirmed in earlier sessions.
3. `ConfirmMulliganButton` stayed active/clickable the entire match, and a second click would double-register the End Turn listener, silently doubling every subsequent turn — **fixed** (listener now registered once in `Start()`, button explicitly deactivated after use).
4. Background/music setup ran after an early-return, so a misconfigured card pool produced a silent black screen — **fixed** (moved to the top of `Start()`).

**Real issues identified by the same review but deliberately NOT fixed yet** (tracked here so they aren't lost, not urgent):
- Taunt's rule is duplicated across three call sites (`EffectTester.OnMinionClicked`/`OnFaceClicked`, `AIController`) and `Combat.TryAttack` — the one chokepoint every attack passes through — has no idea Taunt exists at all. Flagged as the structural reason Taunt keeps being able to break when new attack paths get added (e.g. future drag-and-drop). Worth a deliberate refactor before adding any more attack paths, not a quick patch.
- `Combat.TryAttack` has no null checks on `attacker`/`target`, and no `IsDead` check on either side — it trusts every caller completely.
- `FaceView`'s idle-animation base position/scale is captured inside `Start()`, potentially before the first Canvas layout pass — could cause a permanently offset avatar with no error in some circumstances (not observed yet, just a risk).
- Both avatar `Image`s still have `Raycast Target` checked (should be unchecked per established habit); the two avatars are set up with inconsistent anchor types in the scene.
- `MinionView`/`CardView`/`BoardDisplay`/`HandDisplay` (the oldest view scripts) don't guard their core text/prefab fields with null checks the way newer code (artwork, avatar, background fields) consistently does — the "guard new fields" convention was never backfilled into the original views.
- `PROJECT_STATUS.md`'s stated assembly graph (`Core ← Effects ← Cards ← AI/UI`, implying AI and UI are parallel leaves) doesn't match reality — it's actually `Cards ← AI ← UI` (UI references AI). Not circular, compiles fine, just a documentation inaccuracy — corrected in the Architecture Decisions section above.
- Spells can currently only ever target the enemy face (`opponentTarget` is a single cached `Target` reused for every non-self spell) — there's no way for a human to target a minion with a damage spell.
- No draw-game handling — `CheckWinCondition()` checks Player One first, so a simultaneous double-KO always awards the win to Player Two.
- `gameOver`/`winner` are never reset — no restart/new-game path exists yet.
- Several smaller nice-to-haves noted (0-attack minions could theoretically attack per `CanAttack`'s current logic; `GameContext.Board` and `CardData.description` are currently unused/write-only; `TurnNumber` counts half-turns not rounds).

**Older, still-outstanding items:**

**Taunt — confirmed working via direct playtesting:**
- `TestCard_Shieldbearer` (1/5 Minion) had its new `Has Taunt` checkbox enabled in the Inspector — no new card asset needed, just flipped the existing card's flag.
- Both boards correctly render `"Shieldbearer (Taunt)"` for any Taunt minion.
- **Rejection confirmed**: tried attacking a non-Taunt enemy minion while the opponent had a Taunt minion in play — correctly blocked, console showed `"Player Two has a Taunt minion — you must attack it first."`, and the attacker stayed selected.
- **Success confirmed**: attacking the Taunt minion directly afterward correctly resolved — console showed `"Shieldbearer attacked."`, and the Taunt minion's health dropped exactly as expected (1/3 → 1/2 from a 1-attack hit).
- AI-side enforcement (`AIController.cs` checking `GetTauntMinions()` before choosing its own attack target) was built using the identical `Board` helper as the human path, but wasn't separately confirmed via AI-initiated attacks this session — worth watching for next time a game reaches a state where the AI faces an opposing Taunt minion.

**Tier 1 (fatigue/board/hand caps) — pasted in, compiles cleanly, not yet stress-tested:**
- Neither fatigue (needs a fully emptied deck) nor the board cap (needs 7 minions on one side) came up naturally in that session's shorter Taunt-focused playtest. Worth a longer test session at some point to confirm both actually fire correctly when triggered.

**Session 23 — first Claude Code session, two real bugs found and fixed** (see `EffectTester.cs`/`PlayerHand.cs` rows above for full detail): a late win-check that could let a turn transition happen after the AI already won mid-turn, and a silent no-op when a card effect had no target passed in. Both fixes reviewed and confirmed sound in this chat. Neither has been re-playtested since the fix — the win-check bug in particular is hard to trigger deliberately, since it needs the AI's attack phase to land the exact killing blow.

**Cosmetic, still low priority:**
- `BoardPanel`/`OpponentBoardPanel` text-clipping issue (minion name cut off at left edge) unaddressed.
- Board panel vertical spacing could use more separation.
- `MulliganPanel` sits in the same screen position as `HandPanel` (fine functionally, hidden after confirm).

## Lessons Learned / Gotchas (useful to remember)
**Assembly definitions (.asmdef)**
- Circular references are rejected — keep dependency direction one-way.
- An asmdef's real identity is its **Name** field (Inspector), not its filename.
- `CS0246` errors can mean either a missing asmdef reference OR a missing `using` statement.
- Cross-assembly type usage needs BOTH the `using` directive AND an explicit **Assembly Definition Reference**.

**Unity Editor / UI basics**
- Script filename must exactly match its class name. Scene changes aren't saved until `Ctrl+S`.
- For stretch-anchored panels, reposition via Rect Transform `Pos Y`/`Left`/`Right`/`Height`, not free dragging.
- A circular gizmo appearing unexpectedly in Scene view is likely just the `Global Light 2D` gizmo — editor-only.
- Editing a runtime `(Clone)` GameObject in Play mode is temporary — use Prefab Edit Mode for permanent changes.
- An empty/unassigned Inspector field for a listener-style hookup fails **silently**.
- Editing a TMP label's `Text Input` box requires clicking in, changing text, then clicking away to commit.
- `CS0102` after a manual paste-in usually means a duplicated field/header block — this is why full-file pastes are now standard.
- A script field needing a component that lives on the *same* GameObject the script is attached to: drag that same GameObject onto its own field, Unity finds the matching component.
- **New this session**: a scene GameObject can accidentally become a prefab instance (showing `Prefab`/`Overrides`/`Select`/`Open` at the top of its Inspector) if it's ever dragged into the Project window — easy to do by accident. For a one-off bootstrapper object never meant to be reused as a prefab (like `EffectTester`), fix via right-click → **Prefab → Unpack Completely** (keeps current field values, restores it to a plain scene object), then delete the now-orphaned prefab asset from the Project window so it doesn't cause confusion later.
- **New this session**: `UnityEngine.Random` can produce the same first result across rapid consecutive Play-mode restarts in the Editor — a known quirk of its default seeding, not a logic bug. If something that's supposed to be random (a background, a shuffled pick, etc.) seems to repeat suspiciously often during quick testing, add an explicit `Random.InitState(System.DateTime.Now.Millisecond + System.Environment.TickCount)` call before the random pick — `TickCount`'s high resolution reliably varies the seed even when Play is restarted within the same second.
- **New this session**: a full-screen background `Image` should have `Raycast Target` unchecked proactively when created, not just fixed reactively after discovering it blocks clicks — this was already learned the hard way once with `BoardPanel`'s background; worth treating as a default habit for any purely-decorative `Image` from now on rather than a bug to rediscover each time.

**Core architecture principle**
- Keep `Core` types generic and unaware of `CardData`. AI-ness isn't a flag on `Player`.
- When a display/rendering component is already generic, support "two of something" via a second *instance*, not code to juggle two lists.
- A callback using the null-conditional operator (`onClicked?.Invoke(card)`) gives "free" read-only-mode support.
- `ScriptableObject`-based cards support "multiple copies in a deck" via repeated list references — no asset duplication needed.
- Use in-place Fisher-Yates for shuffling a `List<T>`.
- A boolean flag on the data itself (`CardData.targetsSelf`) is the right way to encode "who does this affect."
- A temporary/this-turn-only stat bonus needs no explicit expiry if it modifies a field some other system already resets unconditionally each cycle.
- When a UI component needs two distinct interaction modes sharing most setup, pull shared parts into a private helper and give each mode its own public entry point, rather than a mode-flag parameter on one method.
- A "confirm/complete" boolean guard flag checked at the top of interactive handlers is a simple way to gate a whole phase without a full state machine — appropriate at small scale; worth revisiting as a real state machine if more phases (e.g. combat) get added.
- **New this session**: when adding a toggleable alternate mode (like `manualControlMode`) to code that already has implicit assumptions baked in (like "only Player One's hand is ever clickable, so no one checks whose turn it is"), audit those implicit assumptions explicitly — the toggle didn't introduce a new bug so much as expose a check (turn ownership) that was never needed before because only one hand was ever interactive. New modes are a good forcing function for surfacing this kind of hidden coupling.
- **New this session (deck design)**: Hearthstone's real deckbuilding rule is many unique cards each capped at ~2 copies, not few unique cards with many copies — worth preserving that shape (add more `CardData` assets, not a higher `copiesPerCard`) to keep matches feeling varied, and because `BuildDeck()`/`Copies Per Card` was deliberately built to scale to any pool size without code changes, so growing the card pool is pure content work, not engineering work.
- **New this session (UI Layout Groups)**: any prefab that's a child of a Horizontal/Vertical Layout Group with `Control Child Size` enabled MUST have a `Layout Element` component defining its own `Preferred Width`/`Height` — without one, the layout group has no sensible size to assign it and results can range from squished/clipped to comically oversized, depending on what "no size" resolves to. When one prefab (`CardView`) works fine in a layout group but a sibling prefab (`MinionView`) renders broken under the same layout settings, checking whether both actually have a `Layout Element` component is the first thing to check — don't assume the layout group settings themselves are wrong.
- **New this session (Editor view vs. real UI bugs)**: before concluding a UI element is actually clipped/broken, check whether the Game view *pane itself* in the Editor is just too short (e.g. a Console panel docked directly below eating vertical space) — this can crop the rendered frame and look identical to a real clipping bug. Try maximizing/resizing the Game view tab before assuming the underlying Rect Transform or Layout Group settings need changing.
- **New this session (Canvas nesting — recurring failure mode)**: if a UI GameObject has completely correct script wiring, Rect Transform values, and Inspector field assignments, but still never renders anything at all, check whether it's actually nested *under* the scene's `Canvas` in the Hierarchy before debugging anything else. A UI element that's a sibling of the Canvas (same indentation level) rather than a child of it will not render, full stop, regardless of how correct everything else about it is — and this is easy to cause by accident when dragging objects around in the Hierarchy. This exact issue hit the same two GameObjects (`PlayerFaceDisplay`/`OpponentFaceDisplay`) twice across sessions — worth checking Canvas nesting FIRST for any newly-invisible UI element, before checking scripts, values, or Inspector wiring.
- **New this session (Raycast Target on background images)**: a plain background `Image` component (one that isn't itself meant to be clicked) rarely needs `Raycast Target` checked. If it's left checked and the panel's position/size ever changes to overlap other clickable UI, it can silently intercept clicks meant for elements underneath it — with no error, just unresponsive buttons. Worth unchecking `Raycast Target` on purely-decorative background images as a general habit, not just when a bug like this actually appears.

**Git / GitHub / environment**
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- .NET SDK (install via Microsoft's apt feed, not Ubuntu's default repo) is only needed for VS Code's C# IntelliSense/debugging.

## Next Steps (in order)
1. **Playtest to confirm tonight's 4 bug fixes actually work**: (a) win a game via a lethal spell (not an attack) and confirm it correctly ends immediately; (b) get a Taunt minion on the AI's opposing board with 2+ AI attackers available, confirm the AI correctly re-evaluates after each kill rather than attacking a dead Taunt minion; (c) confirm `ConfirmMulliganButton` is genuinely gone/inert after confirming mulligan; (d) not urgent to specifically test, but keep an eye out that background/music still work normally in ordinary play.
2. Decide whether to tackle the Taunt-refactor (`Combat.TryAttack` owning the rule instead of three duplicated call sites) before or after adding any new attack path (drag-and-drop, smarter AI) — flagged as structurally important by the Claude Code review, not urgent on its own.
3. Consider adding null/`IsDead` guards to `Combat.TryAttack` and backfilling null checks into the older view scripts (`MinionView`, `CardView`, `BoardDisplay`, `HandDisplay`) to match the convention newer code already follows.
4. Generate art for the remaining 14 cards (only Goblin has real art so far).
5. Consider generating more board background variety and/or more music tracks (currently 3 backgrounds, 1 music track).
6. Longer playtest to actually trigger and confirm fatigue and the 7-minion board cap.
7. Quick explicit check: same minion attacking twice in one turn should be rejected (logic looks correct per code review, not separately re-verified in play).
8. Consider smarter AI attack logic (trading/risk awareness).
9. Consider adding a draw-game outcome (currently a simultaneous double-KO always awards Player Two the win) and a restart/new-game path (`gameOver`/`winner` are never reset currently).
10. **Remaining Tier 2 content work**: Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence, full Hero classes + real Hero Powers (current one is a deliberate minimal placeholder), Weapons, and letting spells target minions (not just face) — each needs its own scoping/design pass before building.
11. Record `BoardPanel`'s corrected `Pos Y` value and `HandPanel`'s current Rect Transform values for documentation completeness.
12. Delete `EffectTester`/rename to something like `GameBootstrapper` once real play/board interaction replaces the manual test setup.
13. Consider upgrading click-to-play to real drag-and-drop, if desired — note this would add a third attack/play path that must remember to respect Taunt, reinforcing item #2 above.
14. Consider simple visual effects/animation feedback (flashes, particles for spells/attacks) as a further visual layer.

## Git Habits Being Followed
- Simple commit template: `git add .` / `git commit -m "short one-line summary"` / `git push`
- Push regularly to `github.com/craig-middleton/hearthClone`
