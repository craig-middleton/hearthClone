# HearthstoneClone — Project Status

> **Current state only.** For *why* — session narratives, past bugs, Editor gotchas, asset pipelines, tooling setup — see `PROJECT_HISTORY.md`.

_Last updated: 2026-08-18 (session 29 — playtest confirmed Next Steps 1a–1d; fixed `MinionView` nameText/statsText overflow and undersized card)_

## How to use these files
- **`PROJECT_STATUS.md` (this file)** — give it to a new Claude chat at the start. Current truth, stands alone: enough to start the next feature.
- **Upload the file rather than pasting it.** Uploading avoids truncation entirely; pasting is what silently drops the middle of a long file, and the chat does not notice it happened.
- **`PROJECT_HISTORY.md`** — read for *why*: which bug a guard prevents, what a refactor abandoned, how the pipelines work.
- **If a session claims something isn't documented, check HISTORY before believing it.** A chat given only STATUS lacks the history, and pastes can truncate silently.
- **New work goes to HISTORY.** In STATUS only current-state sections are *updated in place*. STATUS must not grow.

## Working Preferences
- A 2-paragraph explanation after each new/updated code block: what it does and why.
- **Chat sessions only: full rewritten class/file, not a diff** — Craig pastes whole files and partial merges cause errors. **Not for Claude Code sessions**, which edit on disk: there a whole-file rewrite is overhead and an unreadable diff. Make targeted edits.
- **Avoid singletons** — explicit constructor/field/parameter-passed references over global static instances. Targets *global static state*, not parameters: passing `Board` into a method is the preferred style.

## Project Goal
Hearthstone-style card game in Unity, single-player vs AI, C# with data-driven cards/effects (ScriptableObjects). Ubuntu, Unity + VS Code, Git/GitHub with LFS for art/audio. Repo: `github.com/craig-middleton/hearthClone`.

**Unity 6.5 (60000.5.2f1).** TextMeshPro for all UI text, never legacy `UnityEngine.UI.Text`.

## Architecture
- Folders under `Assets/_Project/`: Scripts, ScriptableObjects, Prefabs, Art, Scenes. Cards are data-driven — they reference reusable `CardEffect` assets.
- **Assembly Definitions** enforce one-way flow: `Core` (no deps) ← `Effects` (Core) ← `Cards` (Core, Effects) ← `AI` (Core, Cards, Effects) ← `UI` (Core, Cards, Effects, **AI**, TMP). `Effects` must NOT reference `Cards`; `UI` references `AI` because `EffectTester` constructs `AIController`.
- **Keep `Core` generic and unaware of `CardData`.** AI-ness isn't a flag on `Player`.

### Design principles in force
Named only; reasoning and worked examples in HISTORY under Lessons Learned.
- **Rules live at the chokepoint** · **a rule is only centralised if its *inputs* are too** (`TryAttack` derives the defender rather than accepting it) · **guard *before* you instantiate** · **hiding something from the view doesn't remove it from state** · **reference identity: `Minion` is safe, `CardData` is not** · **"two of something" is a second *instance***, not code juggling two lists.

## ⚠️ Live Constraints
Read before changing the named code. Kept above the table deliberately — a truncated paste loses the *end* of a long table, and these are the parts that must not go missing.

1. **`Board.GetTauntMinions(Player)` MUST NEVER RETURN NULL.** `Combat.TryAttack` calls `.Count` on it directly, so a null-returning guard here reintroduces an NRE there. `List.FindAll` already returns an empty list — the correct "no Taunts" answer.
2. **`Board.GetTauntMinions` has two call sites**: `Combat.TryAttack` (the rule) and `AIController` (its targeting heuristic). The *rule* lives in one place, the *lookup* in two — **changing it means checking both.**
3. **`Board.GetOwnerOf(Minion)` returns null** when the minion is on neither board. Callers must handle null.
4. **`Combat.TryAttack` guard order**: null attacker/target → null board → malformed `Target` → derive defender → attacker `IsDead` → target minion `IsDead` → defender null → own-side → Taunt → `CanAttack`. Taunt sits *above* `CanAttack` for message precedence; dead-attacker above both, since `CanAttack` includes `!IsDead` and would otherwise blame "summoning sickness" for a corpse.
5. **Accepted gap in `Combat.TryAttack`**: an attacker on neither board passes the own-side check (`GetOwnerOf(attacker)` is null, `defender` isn't). `attacker.IsDead` covers the realistic case; the rest needs a caller fabricating an off-board `Minion`. Deliberately unguarded.
6. **`EffectTester.AfterGameAction()` order**: `RemoveDeadMinions()` → drop a dead `selectedAttacker` → `RefreshAll()` → `CheckWinCondition()`. The drop must stay *before* the refresh. Called by *every* state-changing path.
7. **`EffectTester.ResolveAttack` clears `selectedAttacker` only on success** — a rejected attack deliberately keeps the selection for retargeting.
8. **`HandDisplay.RenderHand` and `BoardDisplay.RenderBoard` are structurally identical by design** — same guards in the same order. A change to one must be mirrored in the other.
9. **Which view guards log**: `CardView` (`nameText`/`costText`/`statsText`/`button`), `MinionView` (`nameText`/`statsText`/`button`) and `FaceView` (`healthText`/`button`) log a named warning with `this` as context. `artworkImage`, `cardBackground`, `minionBackground` and `avatarImage` are guarded but **deliberately silent** — unassigned is a valid configuration for decorative fields.
10. **`BoardDisplay` skips `IsDead` minions** as defence-in-depth against a future damage path forgetting to sweep.
11. **`EffectTester` is temporary** — delete or rename to `GameBootstrapper` once a real gameplay loop exists.
12. **Older `CardData` assets physically omit later-added fields.**
    - **State**: `hasTaunt` is absent from **14 of the 16** assets (present only on `TestCard_Shieldbearer` as `1` and `TestCard_Goblin` as `0`); `targetsSelf` is absent from **4** (`Wisp`, `RiverCroc`, `Fireball`, `Boulderfist`). `artwork` and `description` are present on all 16.
    - **Mechanism**: adding a field to `CardData.cs` does **not** rewrite existing assets. An asset's YAML is written only when Unity saves *that asset* — an Inspector edit, a scripted write, or a forced re-serialize — and until then it keeps whatever field set it had when last written. Missing fields fall back to the C# initializer at load. Git shows it cleanly: `targetsSelf` landed in `48e9d98`, and the ten-card batch created two commits later (`8963670`) has it; `hasTaunt` landed in `b2c217f`, five commits *after* that batch, so every asset lacks it except the two re-saved since. The clincher is `de18de0`, where assigning Goblin's artwork wrote **both** `artwork:` and `hasTaunt: 0` into the file in one diff — nobody touched Goblin's Taunt checkbox; the whole asset was simply re-serialized.
    - ✅ **New assets are fine.** A card asset created *after* a field exists gets that field written at creation — which is exactly why the ten-card batch has `targetsSelf`. The hazard is confined to assets that already exist when the field is added.
    - **Hazards**: grepping the assets for a field name misses most cards (`grep hasTaunt` returns 2 of 16); and changing a field's initializer in `CardData.cs` silently flips every asset that omits the field, while leaving the explicitly-written ones alone.
    - **Remedy**: force re-serialization — `AssetDatabase.ForceReserializeAssets` on the `CardData` assets, or touching each one in the Inspector — which writes the omitted fields physically into every file. ⚠️ **Do this BEFORE changing any field's initializer, never after.** Afterwards the silent flip has already happened, and re-serializing only pins the wrong values in permanently.
13. **`.csproj` files are gitignored and Unity-generated.** Fine for editing existing scripts, but adding a new script and building without letting Unity regenerate would build clean while silently omitting it — open Unity after adding files.

## Code Written So Far
Constraint notes live above; these rows say what each file does.

| File | Location | Purpose |
|---|---|---|
| `CardEffect.cs` | `Scripts/Effects/` | Abstract SO base; `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Deals `damageAmount`; logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` | Calls `target.GainMana(manaAmount)`. |
| `CardData.cs` | `Scripts/Cards/` | SO: `cardName`, `description` (never read), `artwork`, `manaCost`, `cardType`, `attack`, `health`, `hasTaunt`, `onPlayEffect`, `targetsSelf`. |
| `Minion.cs` | `Scripts/Core/` | Board minion: `HasSummoningSickness` (true from construction), `HasAttackedThisTurn`, `HasTaunt`, `Artwork`, `ResetForNewTurn()`. `IsDead` = `CurrentHealth <= 0`. `CanAttack` = `!IsDead && !HasSummoningSickness && !HasAttackedThisTurn` — read by `MinionView`'s tint, `EffectTester`'s selection, and `AIController`'s attack loop over a pre-attack snapshot, where `!IsDead` stops it swinging with a corpse. No `CurrentAttack > 0` term. |
| `Player.cs` | `Scripts/Core/` | `PlayerName`, `Health` (30), `CurrentMana`/`MaxMana`, `BoardMinions`, `FatigueDamage`, `HasUsedHeroPowerThisTurn`, `TakeDamage(int)`. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Both `Player`s; `GetOpponent()`, `RemoveDeadMinions()`, `GetTauntMinions()`, `GetOwnerOf()` — the last two XML-documented. Constraints 1–3. |
| `GameContext.cs` | `Scripts/Core/` | Holds a `Board` reference. Assigned in the ctor, never read — write-only. |
| `Target.cs` | `Scripts/Core/` | A `Player` **or** a `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)` (uncapped). Built with a null argument it is non-null with *both* fields null — `TryAttack` rejects that. |
| `Combat.cs` | `Scripts/Core/` | Static, stateless. `TryAttack(Minion attacker, Target target, Board board, out string failReason)`. **Sole owner of the Taunt rule**; derives the defender internally (`target.TargetPlayer` for faces, `board.GetOwnerOf(...)` for minions) so no caller can select the wrong side. On success applies damage, and strikes back if the target is a minion (simultaneous). Constraints 4–5. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana. Private `StartTurnFor(player)` refills mana, resets minions, clears `HasUsedHeroPowerThisTurn`; called from `StartGame()` and `EndTurn()`, so a minion stays sick through the opponent's whole turn. **`MaxMana` capped at 10.** `TurnNumber` = half-turns. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Player` with `Deck`/`Hand`. `Shuffle()` (Fisher-Yates), `DrawOpeningHand()`, `AddCardToHand()`, `MulliganCard()`, `DrawCard()` (escalating fatigue when empty; burns a draw at hand size 10). `PlayCard()` blocks Minion plays at board size 7, before mana/hand are touched. **Only `new Minion(...)` site.** |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper: decks/hands, opening hands + Coin, mulligan, manual control mode, click-to-attack, face display, win condition, per-turn draw, minimal Hero Power, random background + music (`boardBackgroundImage`/`boardBackgrounds`, `musicSource`/`musicTracks`/`musicVolume`; one `Random.InitState(...)` atop `Start()`, before the `cardPool` empty-check). Hero Power resolves through `turnManager.CurrentPlayer` / `board.GetOpponent(...)`, working for either player under manual control. Constraints 6, 7, 11. |
| `CardView.cs` | `Scripts/UI/` | One card; `SetCard()` (play) and `SetCardForMulligan()` (toggle-select) share a private `WriteCardText()`; both guard `cardData`. `artworkImage.enabled = false` hides the slot when a card has no art. Constraint 9. |
| `MinionView.cs` | `Scripts/UI/` | One board minion. Clickable; `minionBackground` tints selected/eligible; `nameText` appends `" (Taunt)"`. `SetMinion()` takes optional `clickCallback`, `isSelected`, `showAttackEligibility`; guards `minionData`. Constraint 9. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, two instances. Guards `handPanel` *before* the child-clearing loop, returns early on a null list, guards `cardViewPrefab`, skips null entries, destroys an object whose `GetComponent<CardView>()` is null. **No** per-entry null logging — it would spam every refresh. Constraint 8. |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard()` — optional `onMinionClicked`, `selectedAttacker`, `showAttackEligibility`, passed to `MinionView.SetMinion()`. Generic, two instances, over `boardPanel`/`minionViewPrefab`. Constraints 8, 10. |
| `FaceView.cs` | `Scripts/UI/` | Health + mana display, doubling as the face attack target. `SetPlayer(Player, Action<Player>)` writes `"{PlayerName}: {Health} HP\nMana: {CurrentMana}/{MaxMana}"` and wires a `Button`; guards `playerData`. Idle animation in `Update()`: sine scale pulse + sway, base captured on first `SetPlayer()`, four tunable fields. Constraint 9. |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan()` (returns cards at/above a mana threshold) and `TakeTurn()` (card-play loop, then attacks). Sweeps `RemoveDeadMinions()` after the card-play loop and after every swing. Attack loop breaks early if the opponent is dead, skips null and non-`CanAttack` minions, targets the first opponent Taunt else the face. The AI owns its *heuristic*; the *legality* of its pick is validated by `Combat.TryAttack`. |

## Card Pool
15 `CardData` assets in `EffectTester.cardPool` × `copiesPerCard = 2` = a 30-card deck: 12 Minions and 3 Spells. The Coin sits outside the pool.

| Minion | Cost | A/H | Note |
|---|---|---|---|
| Murloc | 1 | 1/1 | |
| Goblin | 2 | 2/2 | only card with artwork |
| Watchman | 2 | 3/1 | |
| Wisp | 2 | 1/1 | |
| River Crocodile | 3 | 2/3 | |
| Shieldbearer | 3 | 1/5 | **only Taunt** (`hasTaunt: 1`) |
| Warhorse | 3 | 3/3 | |
| Bear | 4 | 3/6 | |
| Boulderfist | 5 | 4/4 | |
| Charging Rhino | 6 | 5/5 | |
| Stone Guardian | 6 | 4/8 | |
| Ancient Colossus | 7 | 7/7 | |

Spells:

| Spell | Cost | `onPlayEffect` | `targetsSelf` |
|---|---|---|---|
| Fireball | 4 | `Effect_Deal3Damage` | false |
| Arcane Bolt | 3 | `Effect_Deal3Damage` | false |
| Mage Pupil | 4 | `Effect_GainMana1` | true |
| The Coin | 0 | `Effect_GainMana1` | true — **not in the pool**, granted via `EffectTester.coinCard` |

`Effect_Deal3Damage` is a `DealDamageEffect` (damage 3); `Effect_GainMana1` a `GainManaEffect` (mana 1).

## Verification Status
- **Confirmed in play, still valid**: rendering both sides, decks/shuffle/draw, opening hands + Coin, mulligan both sides, lethal-via-spell ending the game with input genuinely locked out, `ConfirmMulliganButton` not lingering, background/music. Per-session detail in HISTORY.
- ✅ **Confirmed via playtest (Next Steps 1a–1d)**: Hero Power under manual control mode (correct player charged/flagged/damaged, regression-checked with manual control off), Taunt rejection (message, attacker stays selected), the wider attack path (Taunt resolves, AI-side Taunt enforcement and re-targeting after a kill, AI attack phase generally), and the visual render check (name/cost/stats on every card and minion). The own-side rejection remains unreachable from the UI and stays unverified.
- **Never verified at all**: the own-side rejection (unreachable from the UI today — see Next Steps 1c note).
- **Compile-verified**: all five assemblies build clean via `dotnet build Core.csproj` (and `Effects`, `Cards`, `AI`, `UI`), no Unity launch needed. **Catches syntax, not behaviour.**

## Known Issues — real, not fixed
- **Mulligan can hand back the card you just returned.** `MulliganCard()` removes → adds to deck → shuffles → draws, so the returned card is in the deck when the replacement is drawn. Real Hearthstone shuffles replacements back only *after* drawing. Fixing means restructuring the mulligan from per-card to batch — a behaviour change needing its own decision (Next Steps 6).
- **`FaceView` idle-animation base capture can land pre-layout.** Only `OpponentAvatarImage` is exposed (stretch-anchored under a canvas-stretched parent); `AvatarImage` is point-anchored and safe. **Cannot fire on the normal path** — the only trigger is `ShowMulliganUI()`'s unwired-panel fallback, which calls `RefreshAll()` from inside `Start()`. Full analysis and the exact anchor values in HISTORY.
- **Both avatar `Image`s still have `Raycast Target` checked** (should be unchecked for decorative images), and the two avatars use inconsistent anchor types.
- **Spells can only ever target the enemy face.** `OnCardClicked` reuses a single cached `opponentTarget`; the opponent-side and AI paths build fresh `Target`s but still aim at the face. No way for a human to target a minion with a damage spell — Next Steps 2.
- **No draw-game handling.** `CheckWinCondition()` checks Player One first, so a simultaneous double-KO always awards Player Two the win.
- **`gameOver`/`winner` are never reset** — no restart/new-game path exists.
- **`CardData` reference identity is not clean** (full chain in HISTORY): mulligan-selecting one copy of a duplicate toggles the shared entry.
- **Fatigue and the 7-minion board cap compile but have never been triggered** in play.
- **Manual control mode's turn-gating fix is written but was never re-tested.**
- Smaller: 0-attack minions can attack; `GameContext.Board` and `CardData.description` declared but never read; `TurnNumber` counts half-turns; `Target.GainMana` is uncapped, so The Coin can exceed both `MaxMana` and 10; `OnFaceClicked` blocks attacking your own face as a UI guard, in addition to Combat's own-side rejection.
- **Cosmetic**: `BoardPanel`/`OpponentBoardPanel` text clipping (minion name cut off at the left edge); board panel vertical spacing needs more separation; `MulliganPanel` shares `HandPanel`'s screen position (fine, hidden after confirm).

## Next Steps (in order)
1. ✅ **Playtest — confirmed, all four passes.**
   - **1a — Hero Power under manual control mode**: confirmed. As Player Two, the *correct player* is charged 2 mana, flagged `HasUsedHeroPowerThisTurn`, and takes the 1 damage; regression-checked identical with manual control off.
   - **1b — Taunt rejection**: confirmed. With a Taunt up, attacking a non-Taunt target is rejected, the message names the owner, and the attacker stays selected.
   - **1c — The wider attack path**: confirmed. Attacking the Taunt minion itself resolves; AI-side Taunt enforcement and re-targeting after killing a Taunt work; AI attack phase generally works. The own-side rejection **still cannot be reached from the UI** — own-minion clicks route to selection and own-face clicks are blocked in `OnFaceClicked` — so it remains unverified until targeting or drag-and-drop can reach it.
   - **1d — Visual render check**: confirmed. Every card in hand and every minion on both boards shows name, cost and stats. `MinionView`'s `nameText`/`statsText` originally overflowed the card (box wider than the card, no word wrap, and the card's `RectTransform` didn't match the `LayoutElement` size the layout group actually rendered at) — fixed session 29: word wrap + TMP auto-sizing enabled, text boxes narrowed to fit, and the card resized to 170x190 (`RectTransform` and `LayoutElement` now match, as large as fits the 200px-tall board panel).
2. **Targeting system — let spells target minions, not just the face.** Build "select a card → select a target". First of the three features, since drag-and-drop needs the same interaction layer; can likely reuse the `selectedAttacker` pattern.
3. **More spells.** Content on the existing `CardEffect` system, but AoE / heal / buff / draw each need a new `CardEffect` subclass. Scope first: pick the spells, then sort into "new asset only" vs "needs a new effect class".
4. **Drag-and-drop to play cards.** After targeting, so it layers over a working system. Touches all five UI scripts — `IBeginDragHandler`/`IDragHandler`/`IEndDragHandler`, drop zones, a drag ghost, canvas raycasting.
5. Generate art for the remaining 14 cards (only Goblin has art). Pure content.
6. **Batch the mulligan** so a returned card can't be drawn straight back. Behaviour change; decide the shape first.
7. Longer playtest to trigger fatigue and the 7-minion board cap.
8. Check the same minion attacking twice in one turn is rejected.
9. Re-test manual control mode's turn-gating fix.
10. `FaceView` scene issues: `OpponentAvatarImage`'s pre-layout exposure, both avatars' `Raycast Target`, inconsistent anchors.
11. More board backgrounds and/or music tracks (currently 3 backgrounds, 1 track).
12. Smarter AI attack logic — this is when the centralised Taunt check stops being a no-op on the AI path.
13. Draw-game outcome and a restart/new-game path.
14. **Tier 2 content**: Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence, full Hero classes + real Hero Powers (the current one is a placeholder), Weapons — each needs its own scoping pass. ⚠️ **Sequencing — see Live Constraint 12.** Deathrattle/Charge/Divine Shield/Windfury/Silence is five new bools on `CardData`, and all 16 existing assets will physically omit all five, falling back to the C# initializer. Add them with `false` initializers, force re-serialize every `CardData` asset so the fields are written into the files, and only then start setting per-card flags. Cards created after the fields exist are unaffected.
15. Record `BoardPanel`'s corrected `Pos Y` and `HandPanel`'s Rect Transform values.
16. Delete `EffectTester` / rename to `GameBootstrapper`.
17. Simple visual effects/animation feedback (flashes, particles).

## Git Habits
- `git add .` / `git commit -m "short summary"` / `git push`, pushed regularly to `github.com/craig-middleton/hearthClone`.
- `.gitignore` excludes `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
