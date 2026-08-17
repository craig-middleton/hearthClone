# HearthstoneClone — Project Status

> **Current state only.** For *why* things are the way they are — session narratives, past bugs, Editor gotchas, asset pipelines, tooling setup — see `PROJECT_HISTORY.md`.

_Last updated: 2026-08-17 (session 28 — audited this file against the source and corrected four wrong rows; removed `Combat.TryAttack`'s `defender` parameter in favour of internal derivation; fixed Hero Power for manual control mode; made log context universal; split this file into STATUS + HISTORY)_

## How to use these files
- **`PROJECT_STATUS.md` (this file)** — paste at the start of a new Claude chat. It is the current truth and is written to stand alone: a fresh session should be able to start the next feature from this file alone.
- **`PROJECT_HISTORY.md`** — read when you need to know *why*: which bug a guard prevents, what a refactor tried and abandoned, how the art/music pipelines were built.
- **If a session claims something isn't documented, check `PROJECT_HISTORY.md` before believing it.** A chat given only STATUS is missing the history, and pasted files can truncate mid-section without the chat noticing.
- **New work is recorded in HISTORY.** In STATUS only the current-state sections are *updated in place* — rows get rewritten, not appended to. STATUS must not grow over time.

## Working Preferences
- A 2-paragraph explanation after each new/updated code block: what it does and why.
- **Chat sessions only: give the full rewritten class/file, not a diff** — Craig pastes whole files and partial merges cause errors. **Does not apply to Claude Code sessions**, which edit on disk: there a whole-file rewrite is pure overhead and an unreadable diff. Make targeted edits.
- **Avoid singletons** — explicit constructor/field/parameter-passed references over globally-reachable static instances (script execution order, `DontDestroyOnLoad`, creeping responsibility). This targets *global static state*, not parameters: passing `Board` explicitly into a method is the preferred style.

## Project Goal
Hearthstone-style card game in Unity, single-player vs AI, C# with a data-driven card/effect system (ScriptableObjects). Ubuntu, Unity + VS Code, Git/GitHub with Git LFS for art/audio. Repo: `github.com/craig-middleton/hearthClone`.

**Unity 6.5 (60000.5.2f1).** TextMeshPro for all UI text, never legacy `UnityEngine.UI.Text`.

## Architecture
- **Folders** under `Assets/_Project/`: Scripts, ScriptableObjects, Prefabs, Art, Scenes. Cards are data-driven — they reference reusable `CardEffect` assets rather than each having bespoke code.
- **Assembly Definitions** enforce one-way flow: `Core` (no deps) ← `Effects` (Core) ← `Cards` (Core, Effects) ← `AI` (Core, Cards, Effects) ← `UI` (Core, Cards, Effects, **AI**, TMP). `Effects` must NOT reference `Cards`. `UI` references `AI` because `EffectTester` constructs `AIController`. Not circular; verified against the actual asmdef reference lists.
- **Keep `Core` generic and unaware of `CardData`.** AI-ness isn't a flag on `Player`.

### Design principles in force
- **Rules live at the chokepoint.** If every path to an action funnels through one method, the rule belongs *inside* it. `Combat.TryAttack` owns Taunt for this reason.
- **A rule is only centralised if its *inputs* are too.** If a caller can defeat a check by passing the wrong argument, it isn't centralised. `TryAttack` derives the defending player from the target rather than accepting it — this is what finally satisfies the test.
- **Guard *before* you instantiate.** A guard at the top of `SetCard`/`SetMinion` fires after the caller already ran `Instantiate`, leaving a blank prefab-default card in the layout with live serialized listeners — worse than the NRE it replaced.
- **Hiding something from the view doesn't remove it from state.** A render filter and a held reference that disagree create unreachable states: the reference still drives logic while invisible. Any view filter needs a matching check where the object is *held*.
- **Reference identity: `Minion` clean, `CardData` not.** `Minion` is a plain class overriding none of `Equals`/`GetHashCode`/`operator==`, and `new Minion(...)` appears exactly once (`PlayerHand.PlayCard`), so a reference identifies one minion for its lifetime — `Contains`/`Remove`/`GetOwnerOf` are safe. **`CardData` is the opposite**: `BuildDeck()` adds the same asset reference `copiesPerCard` times, so two "copies" in hand are one object and `Contains`/`Remove`/`HashSet` cannot tell them apart.
- Generic display components support "two of something" via a second *instance*, not code juggling two lists. `?.Invoke(x)` gives free read-only-mode support.

## Code Written So Far

⚠️ = a live constraint. Don't change it without reading the note.

| File | Location | Purpose |
|---|---|---|
| `CardEffect.cs` | `Scripts/Effects/` | Abstract `ScriptableObject` base; `Execute(GameContext, Target)`. |
| `DealDamageEffect.cs` | `Scripts/Effects/` | Deals `damageAmount` to a `Target`, logs remaining health. |
| `GainManaEffect.cs` | `Scripts/Effects/` | Calls `target.GainMana(manaAmount)`. Powers The Coin. |
| `CardData.cs` | `Scripts/Cards/` | SO: `cardName`, `description` (declared, never read), `artwork`, `manaCost`, `cardType`, `attack`, `health`, `hasTaunt`, `onPlayEffect`, `targetsSelf`. |
| `Minion.cs` | `Scripts/Core/` | Runtime board minion. `HasSummoningSickness` (true from construction), `HasAttackedThisTurn`, `HasTaunt`, `Artwork`, `TakeDamage()`, `ResetForNewTurn()`; optional ctor params for `hasTaunt`/`artwork`. `IsDead` = `CurrentHealth <= 0`. `CanAttack` = `!IsDead && !HasSummoningSickness && !HasAttackedThisTurn`, read by `MinionView`'s tint, `EffectTester`'s selection check, and `AIController`'s attack loop — which iterates a snapshot taken before any attack resolves, so `!IsDead` is what stops it swinging with a corpse. No `CurrentAttack > 0` term: 0-attack minions can legally attack. |
| `Player.cs` | `Scripts/Core/` | `PlayerName`, `Health` (30), `CurrentMana`/`MaxMana`, `BoardMinions`, `FatigueDamage`, `HasUsedHeroPowerThisTurn`, `TakeDamage(int)`. No `IsAI` flag. |
| `Board.cs` | `Scripts/Core/` | Both `Player`s; `GetOpponent()`, `RemoveDeadMinions()`, and two XML-documented lookups. ⚠️ **`GetTauntMinions(Player)` MUST NEVER RETURN NULL** — `Combat.TryAttack` calls `.Count` directly, so a null-returning guard here reintroduces an NRE there. `List.FindAll` already returns an empty list, the correct "no Taunts" answer. ⚠️ **`GetOwnerOf(Minion)` returns null** when the minion is on neither board; callers must handle it. ⚠️ **`GetTauntMinions` has two call sites** — `Combat.TryAttack` (the rule) and `AIController` (its targeting heuristic). The *rule* lives in one place, the *lookup* in two: **changing this method means checking both.** |
| `GameContext.cs` | `Scripts/Core/` | Holds a `Board` reference. Assigned in the ctor, never read — currently write-only. |
| `Target.cs` | `Scripts/Core/` | Points to a `Player` **or** a `Minion`. `TakeDamage()`, `GetCurrentHealth()`, `GainMana(int)`. Built with a null argument it is non-null with *both* fields null — `TryAttack` rejects that. `GainMana` writes `CurrentMana` uncapped. |
| `Combat.cs` | `Scripts/Core/` | Static, stateless. `TryAttack(Minion attacker, Target target, Board board, out string failReason)`. **Sole owner of the Taunt rule**; derives the defender internally (`target.TargetPlayer` for faces, `board.GetOwnerOf(...)` for minions) so no caller can select the wrong side. ⚠️ **Guard order**: null attacker/target → null board → malformed `Target` → derive defender → attacker `IsDead` → target minion `IsDead` → defender null → own-side → Taunt → `CanAttack`. Taunt sits *above* `CanAttack` for message precedence; dead-attacker above both, since `CanAttack` includes `!IsDead` and would otherwise blame "summoning sickness" for a corpse. On success applies damage, and strikes back if the target is a minion (simultaneous). ⚠️ **Accepted gap**: an attacker on neither board passes the own-side check (`GetOwnerOf(attacker)` is null, `defender` isn't). `attacker.IsDead` covers the realistic case; the rest needs a caller fabricating an off-board `Minion`. Deliberately unguarded. |
| `TurnManager.cs` | `Scripts/Core/` | Turn order + mana. Private `StartTurnFor(player)` wraps mana refill, `ResetMinionsForNewTurn(player)` and clearing `HasUsedHeroPowerThisTurn`; called from `StartGame()` and `EndTurn()`, so a minion summoned on one turn stays sick through the opponent's whole turn. **`MaxMana` hard-capped at 10** (`MaxManaCap`). `TurnNumber` counts half-turns, not rounds. |
| `PlayerHand.cs` | `Scripts/Cards/` | Wraps a `Player` with `Deck`/`Hand`. `Shuffle()` (in-place Fisher-Yates), `DrawOpeningHand()`, `AddCardToHand()`, `MulliganCard()`, `DrawCard()` (escalating fatigue on empty deck; burns a draw if hand is full at 10). `PlayCard()` blocks Minion plays when the board is full (7, checked before mana/hand are touched), passes `hasTaunt`/`artwork` to the ctor, warns if a card has an `onPlayEffect` but no target. **Only place `new Minion(...)` appears.** |
| `EffectTester.cs` | `Scripts/UI/` | Bootstrapper: decks/hands, opening hands + Coin, mulligan, manual control mode, click-to-attack, face display, win condition, per-turn draw, minimal Hero Power, random background + music (`boardBackgroundImage`/`boardBackgrounds`, `musicSource`/`musicTracks`/`musicVolume`; one shared `Random.InitState(...)` at the top of `Start()`, before the `cardPool` empty-check). ⚠️ `AfterGameAction()` = `RemoveDeadMinions()` → drop a dead `selectedAttacker` → `RefreshAll()` → `CheckWinCondition()`, **in that order** (the drop must stay *before* the refresh), called by *every* state-changing path. ⚠️ `ResolveAttack(Minion, Target)` clears `selectedAttacker` **only on success** — a rejected attack deliberately keeps the selection for retargeting. Hero Power resolves entirely through `turnManager.CurrentPlayer` / `board.GetOpponent(...)`, so it works for either player under manual control. ⚠️ **Temporary — delete or rename to `GameBootstrapper` once a real gameplay loop exists.** |
| `CardView.cs` | `Scripts/UI/` | One card, two modes sharing a private `WriteCardText()`: `SetCard()` (play) and `SetCardForMulligan()` (toggle-select, dim/highlight); both guard `cardData`. ⚠️ `nameText`, `costText`, `statsText`, `button` are guarded **and log a named warning** with `this` as context; `cardBackground`/`artworkImage` are guarded but **deliberately silent** — unassigned is valid for decorative fields. `artworkImage.enabled = false` hides the slot when a card has no art. |
| `MinionView.cs` | `Scripts/UI/` | One board minion. Clickable; `minionBackground` tints selected/eligible; `nameText` appends `" (Taunt)"`. `SetMinion()` takes optional `clickCallback`, `isSelected`, `showAttackEligibility`; guards `minionData`. ⚠️ `nameText`, `statsText`, `button` **log named warnings**; `minionBackground`/`artworkImage` **deliberately silent**. |
| `HandDisplay.cs` | `Scripts/UI/` | `RenderHand(List<CardData>, Action<CardData>)` — generic, two instances for the two hands. Guards `handPanel` *before* the child-clearing loop, returns early on a null list, guards `cardViewPrefab`, skips null entries, destroys an instantiated object whose `GetComponent<CardView>()` is null rather than leaving a ghost in the layout. Deliberately **no** per-entry null logging — that would spam every refresh. ⚠️ **Structurally identical to `BoardDisplay.RenderBoard` by design: a change to one must be mirrored in the other.** |
| `BoardDisplay.cs` | `Scripts/UI/` | `RenderBoard()` — optional `onMinionClicked`, `selectedAttacker`, `showAttackEligibility`, passed through to `MinionView.SetMinion()`. Generic, two instances. ⚠️ Same guard structure and ordering as `HandDisplay` over `boardPanel`/`minionViewPrefab`, including destroying an object whose `GetComponent<MinionView>()` is null (see parity note), plus **skips `IsDead` minions** as defence-in-depth against a future damage path forgetting to sweep. |
| `FaceView.cs` | `Scripts/UI/` | Health + mana display, doubling as the face attack target. `SetPlayer(Player, Action<Player>)` writes `"{PlayerName}: {Health} HP\nMana: {CurrentMana}/{MaxMana}"` and wires a `Button`; guards `playerData`. ⚠️ `healthText` and `button` **log named warnings**; `avatarImage` **deliberately silent** (decorative). Procedural idle animation in `Update()` — sine "breathing" scale + slow sway, base scale/position captured on the first `SetPlayer()`, four tunable fields. |
| `AIController.cs` | `Scripts/AI/` | `PerformMulligan()` (returns cards at/above a mana threshold, skips nulls) and `TakeTurn()` (card-play loop, then attacks). Sweeps `RemoveDeadMinions()` after the card-play loop and after every swing. Attack loop breaks early if the opponent is dead, skips null and non-`CanAttack` minions, targets the first opponent Taunt if any exist else the face. The AI owns its targeting *heuristic* — a legitimate strategy decision — but the *legality* of its pick is validated centrally by `Combat.TryAttack`. |

## Verification Status
- **Confirmed in play**: board visuals, AI turn logic, opponent board visualisation, shuffled decks + draw, opening hands + Coin, mulligan both sides, Taunt rejection *and* success on the human side, AI-side Taunt enforcement including re-targeting after a kill, lethal-via-spell ending the game with input genuinely locked out, `ConfirmMulliganButton` not lingering, background/music.
- **Compile-verified**: all five assemblies build clean via `dotnet build Core.csproj` (and `Effects`, `Cards`, `AI`, `UI`) against the Unity-generated `.csproj` files, no Unity launch needed. **Catches syntax, not behaviour** — not a substitute for playtesting.
- ⚠️ **`.csproj` files are gitignored and Unity-generated.** Fine for editing existing scripts, but **adding a new script and building without letting Unity regenerate would build clean while silently omitting it** — open Unity after adding files.
- **Not yet verified** — see Next Steps 1: the Taunt/own-side rejection behaviour, Hero Power under manual control mode, and a visual render check.

## Known Issues — real, not fixed
- **Mulligan can hand back the card you just returned.** `MulliganCard()` removes → adds to deck → shuffles → draws, so the returned card is in the deck when the replacement is drawn. Real Hearthstone shuffles replacements back only *after* drawing. Fixing means restructuring the mulligan from per-card to batch — a behaviour change needing its own decision (Next Steps 6).
- **`FaceView` idle-animation base capture can land pre-layout.** Only `OpponentAvatarImage` is exposed — stretch-anchored under a canvas-stretched parent, so its `localPosition` depends on the parent's resolved rect. `AvatarImage` is point-anchored and **not** at risk. **On the normal path this cannot fire at all**: `SetPlayer` is first reached from a user click (confirm mulligan), many frames after the first layout pass. The only trigger is `ShowMulliganUI()`'s unwired-panel fallback, which calls `RefreshAll()` from inside `Start()` — the one path rendering views before the first Canvas layout. `OpponentAvatarImage`'s anchors are arbitrary drag-created values (`AnchorMax.y 0.6135163`), not a clean preset — don't blindly match them.
- **Both avatar `Image`s still have `Raycast Target` checked** (should be unchecked for decorative images), and the two avatars use inconsistent anchor types.
- **Spells can only ever target the enemy face.** `OnCardClicked` reuses a single cached `opponentTarget`; the opponent-side and AI paths build fresh `Target`s but still aim at the face. No way for a human to target a minion with a damage spell — Next Steps 2.
- **No draw-game handling.** `CheckWinCondition()` checks Player One first, so a simultaneous double-KO always awards Player Two the win.
- **`gameOver`/`winner` are never reset** — no restart/new-game path exists.
- **`CardData` reference identity is not clean** (see design principles): mulligan-selecting one copy of a duplicate toggles the shared entry.
- **Fatigue and the 7-minion board cap compile but have never been triggered** in play.
- **Manual control mode's turn-gating fix is written but was never re-tested.**
- Smaller: 0-attack minions can attack; `GameContext.Board` and `CardData.description` declared but never read; `TurnNumber` counts half-turns; `Target.GainMana` is uncapped, so The Coin can exceed both `MaxMana` and 10; `OnFaceClicked` blocks attacking your own face as a UI guard, in addition to Combat's own-side rejection.
- **Cosmetic**: `BoardPanel`/`OpponentBoardPanel` text clipping (minion name cut off at the left edge); board panel vertical spacing needs more separation; `MulliganPanel` shares `HandPanel`'s screen position (fine, hidden after confirm).

## Next Steps (in order)
1. **Playtest — two newly-changed, entirely unverified behaviours first.**
   - **Taunt rejection**: with a Taunt up, attacking a non-Taunt target must be rejected, the message must name the owning player (`"Player Two has a Taunt minion — you must attack it first."`), and **the attacker must stay selected**.
   - **Hero Power under manual control mode**: turn manual control on and use it as Player Two — verify the *correct player* is charged 2 mana, the *correct player* is flagged as having used it, and the *correct player* takes the 1 damage. Then regression-check with manual control **off**: behaviour should be identical to before.
   - **Visual render check**: every card in hand and every minion on both boards shows name, cost, stats. Both prefabs are currently fully wired, so this is a **layout and text-clipping check**, not a guard check — the mis-wired-prefab case it was written for cannot presently occur.
2. **Targeting system — let spells target minions, not just the face.** Build "select a card → select a target". Do this *first* of the three features, since drag-and-drop needs the same interaction layer. Can likely reuse the `selectedAttacker` pattern. `AfterGameAction()` sweeping corpses on every path is what makes this safe to build now.
3. **More spells.** Mostly content on the existing `CardEffect` system, but AoE / heal / buff / draw each need a new `CardEffect` subclass. Scope first: pick the spells, then sort into "new asset only" vs "needs a new effect class".
4. **Drag-and-drop to play cards.** Sequenced *after* targeting so it layers over a working system. Touches all five UI scripts — `IBeginDragHandler`/`IDragHandler`/`IEndDragHandler`, drop zones, a drag ghost, canvas raycasting.
5. Generate art for the remaining 14 cards (only Goblin has real art). Pure content — repeat generate → import → assign.
6. **Batch the mulligan** so a returned card can't be drawn straight back. Behaviour change; decide the shape first.
7. Longer playtest to trigger fatigue and the 7-minion board cap.
8. Check the same minion attacking twice in one turn is rejected.
9. Re-test manual control mode's turn-gating fix.
10. `FaceView` scene issues: `OpponentAvatarImage`'s pre-layout exposure, both avatars' `Raycast Target`, inconsistent anchors.
11. More board backgrounds and/or music tracks (currently 3 backgrounds, 1 track).
12. Smarter AI attack logic (trading/risk awareness) — this is when the centralised Taunt check stops being a no-op on the AI path.
13. Draw-game outcome and a restart/new-game path.
14. **Tier 2 content**: Deathrattle, Charge/Rush, Divine Shield, Windfury, Silence, full Hero classes + real Hero Powers (the current one is a deliberate placeholder), Weapons — each needs its own scoping pass.
15. Record `BoardPanel`'s corrected `Pos Y` and `HandPanel`'s Rect Transform values.
16. Delete `EffectTester` / rename to `GameBootstrapper` once real play replaces the manual test setup.
17. Simple visual effects/animation feedback (flashes, particles for spells/attacks).

## Git Habits
- `git add .` / `git commit -m "short one-line summary"` / `git push`, pushed regularly to `github.com/craig-middleton/hearthClone`.
- `.gitignore` excludes `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
