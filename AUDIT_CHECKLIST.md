# HearthstoneClone — Pre-Graphics-Overhaul Audit Checklist

> **Investigation only.** This audit changed no code. It was taken at commit `844293d` (2026-10-01).
> **What was read:** all 34 scripts, `Assets/Scenes/TestScene.unity` (components and serialized values), all 4 prefabs, all 20 card assets and 6 effect assets, `PROJECT_STATUS.md`, `PROJECT_HISTORY.md`, `NETWORKING_PREP.md`, `ProjectSettings/EditorSettings.asset`, and the previous audit's report (session "Board overhaul UI tidy and code audit", 2026-09-22).
> **Goal:** before the graphics overhaul starts, every item below is either ticked (fixed and verified) or explicitly decided (write the decision next to it).

## How to use this file

- Every item has one checkbox. Tick it only when the fix is in **and** its test has passed, or when a decision has been written next to it.
- Each item belongs to exactly one category:
  - **A** bug or latent bug
  - **B** rule or behaviour inconsistency
  - **C** code quality
  - **D** documentation inaccuracy
  - **E** verification gap
  - **F** networking architecture
  - **G** unbuilt feature or design decision
- **Severity:**
  - **High:** a wrong game outcome or a crash in normal play.
  - **Medium:** visibly wrong behaviour in normal play, or a latent rule hole that planned work will hit.
  - **Low:** cosmetic, an edge case, or latent with nothing planned that would trigger it.
- **Size:**
  - **S:** under about an hour, one or two files.
  - **M:** several files, or needs a short design.
  - **L:** multi-session.
- **Risk** means regression risk: how much else could break.
- "Evidence" notes say whether a finding is reproduced or read from the code or YAML. Nothing here was run in the Editor during the audit.
- Fix batches are in §11 and the full regression playtest script is in §12. Batch gates refer to the regression sections by number (R1, R2, …).

## Contents

1. Summary
2. Previous audit reconciliation
3. A — Bugs and latent bugs
4. B — Rule and behaviour inconsistencies
5. C — Code quality
6. D — Documentation inaccuracies
7. E — Verification gaps
8. F — Networking architecture
9. G — Unbuilt features and design decisions
10. Cross-cutting maps (human vs AI decisions, Constraint 26, lifecycle)
11. Fix batches
12. Full manual regression playtest script

---

## 1. Summary

| Category | Count | Highest severity |
|---|---|---|
| A — Bugs / latent bugs | 12 | Medium |
| B — Rule / behaviour inconsistencies | 6 | Medium |
| C — Code quality | 18 | Low |
| D — Documentation inaccuracies | 16 | Low |
| E — Verification gaps | 14 | Medium |
| F — Networking architecture | 16 | 6 items safe to do now |
| G — Unbuilt features / design decisions | 13 | 2 block the overhaul |
| **Total** | **95** | |

**No High-severity finding.** No crash or wrong game outcome turned up on the normal play path. The most serious items are:

1. **A-02:** Dropping a targeted spell on your own mana crystal row targets your own face. The crystals are raycastable children of the player's `FaceView`.
2. **A-01:** A spell that doesn't kill its minion target never flashes it. This is the open Known Issue and Next Steps 20.
3. **B-01:** Whether a target satisfies `TargetRequirement` is decided in three separate places (the drop classifier, the AI selector, and some effects). `PlayerHand.PlayCard` never checks it. This is the project's most common bug pattern, still open in one more place.
4. **F-07:** Turn ownership and game phase aren't checked at the chokepoint. This was carried over from the previous audit and is safe to do now.
5. **A-03:** Overlapping spell VFX corrupt the single shared particle renderer. Carried over from the previous audit.
6. **G-01 / G-02:** Two decisions that block the overhaul: the canvas render mode, and whether AI turns are paced and animated.
7. **C-15:** Force-reserialize the assets before the overhaul adds any fields (Constraint 12).
8. **E-10:** A standalone player build has never been made. `Shader.Find` and the RenderTexture path are unproven outside the Editor.

**Proposed batch order** (details in §11):
1. Docs
2. Comments and dead code
3. Asset and prefab hygiene, including A-02
4. Small UI fixes
5. VFX resource lifetime
6. Small rules fixes
7. Safe-now networking seams
8. Target validity at the chokepoint
9. Surviving-minion flash
10. Freeze-timing decision
11. Particle overlap, after G-01

Verification-only E items are run as a baseline before batch 1 and again in the final regression.

---

## 2. Previous audit reconciliation (2026-09-22)

| Previous item | Status now | Where |
|---|---|---|
| 1a `faceDown`/`fanInteractive` predicates, `manualControlMode` read in many places | Open | F-08 |
| 1b Don't split `HandFanLayout` | Decided (keep). No action | — |
| 1b `interactionEnabled` is public but overwritten on every render | Open | C-05 |
| 1b `pressEventCamera` should be `enterEventCamera` | Open | C-16 |
| 1c Don't restructure `FaceView` yet | Deferred | G-13 |
| 1d Unused `CardView.Card`, `BurstSpawnPoint`, `DrawOpeningHand` default | Open | C-02 |
| 1d `[Header("Face UI")]` on a private field | Open | C-04 |
| 1d `FaceView.startingDeckSize = 30` placeholder | Open | C-06 |
| 1d `SetCard(CardInstance cardData)` naming | Open | C-07 |
| 1d Stale comments | Partly fixed (`CardData.cs` header fixed); rest open | C-01 |
| 1d `RenderHand(null)` leaves `HandFanLayout.views` stale | Open | A-07 |
| 1d Mulligan cards can be drag-ghosted | Open | A-05 |
| 2a `GainManaEffect` trusts its `Target` | **Fixed** `dd6e467` | — |
| 2a Turn ownership not enforced at the chokepoint | Open | F-07 |
| 2b Two damage classifications | **Fixed** `dd6e467` | — |
| 2b `OnEndTurnClicked` sequence duplicated | Open | C-08 |
| 2b `FlashRoutine` / `CheckWinCondition` duplication | Open | C-09 |
| 2b "Who may act" rule rebuilt in 4 places | Open | F-08 |
| 2b `Minion` carries a `Sprite`; `PlayerHand` uses `UnityEngine.Random` | Open | F-15, F-04 |
| 2b Player One's turn-1 draw | **Fixed** `2048486` | — |
| 2c-1 AI spends spells on dead minions | **Fixed** `dfd75e1` | — |
| 2c-2 `None` card loses its effect for a human | **Fixed** `dfd75e1` | — |
| 2c-3 Overlapping VFX corrupt the shared renderer | Open | A-03 |
| 2c-4 Frostbolt flash/hold | **Fixed** `dd6e467` + `844293d` (lethal flash) | — |
| 2c-5 Screen Space – Overlay assumptions | Needs a decision | G-01 |
| 2c-6 `EnsureRenderTexture` throws a null reference when fields are unassigned | Open | A-06 |
| Area 3 networking inventory | Saved as `NETWORKING_PREP.md` | F-01…F-16 |
| Area 4 STATUS accuracy | **Fixed** `fc828c8`. New drift since then is in D | D-01…D-16 |

Accepted earlier and not re-raised here:
- Constraint 5 (an off-board attacker passes the own-side check).
- Constraint 31 (a `manualControlMode` toggle takes effect one refresh late).
- HISTORY restart step 7 (in-flight animations survive a restart for about 1s).

---

## 3. A — Bugs and latent bugs

- [ ] **A-01 · Medium · M · Risk: Medium — A spell that doesn't kill its minion target never flashes it.**
  - **Source:** STATUS Known Issues ("NEXT FIX"), Next Steps 20.
  - **Where:** `SpellAnimationSequencer.TravelAndReactRoutine`, `CardDragResolver.TriggerSpellAnimation` / `ResolveViewTransform`, `BoardDisplay.RenderBoard` / `GetViewTransform`.
  - **Problem:** `ResolveCardDrag` captures the target `MinionView` Transform before `PlayCard`. In the same frame, `AfterGameAction → RefreshAll → RenderBoard` destroys every non-held view. At impact `targetView == null`, so `PlayDamageReaction` is skipped. Faces never get destroyed and lethal hits are held, so both are unaffected.
  - **Fix:**
    1. Pass the target `Minion`, plus a `Func<Minion, Transform>` lookup into the owning `BoardDisplay`, alongside (or instead of) the Transform.
    2. During travel and at impact, re-resolve the minion's current view through the lookup whenever `targetView` is null.
    3. Flash the rebuilt view.
    4. ⚠️ This is the lethal-flash bug class again. The flash coroutine lives on a view that any `RefreshAll` inside the next 0.25s destroys, for example playing a second card quickly.
       - Simplest option: accept the truncation.
       - Robust option: `BoardDisplay` keeps a short-lived "pending reaction" keyed by `Minion` and replays the rest of it on the rebuilt view.
  - **Test:** R7.4–R7.6:
    - Arcane Bolt on Shieldbearer (1/5). It survives and flashes once.
    - Frostbolt on a survivor flashes, then shows the frozen tint.
    - Verdant Growth and Rebirth's Touch don't flash.
    - Lethal hits behave as before.
    - Cast a non-lethal spell, then immediately play a minion. There's no exception and no duplicate view.

- [ ] **A-02 · Medium · S · Risk: Low — Dropping a spell on your own mana crystal row targets your own face.**
  - **Source:** fresh.
  - **Where:** `ManaCrystal.prefab` (Image `m_RaycastTarget: 1`), `FaceView.BuildManaCrystalRow`, `CardDragResolver.ResolveCardDrag` (`GetComponentInParent<FaceView>()`).
  - **Problem:**
    - `ManaCrystalRow` is a child of `PlayerFaceDisplay`, the `FaceView` root. Every crystal instance is a raycastable `Image`, so a drop anywhere over the 500-wide crystal row resolves to `hitFaceView = player's FaceView`.
    - Fireball, Arcane Bolt or Frostbolt released there damages your own hero. Rebirth's Touch released there heals it.
    - Clicking a crystal also bubbles to the face `Button`.
    - Every other decorative face child (health gem, deck pile, avatar) already has Raycast Target off. The crystals are the only exception.
  - **Evidence:** read from the scene and prefab YAML; not reproduced in play.
  - **Fix:** Untick Raycast Target on the `ManaCrystal` prefab's `Image`. As a belt-and-braces measure, also set `crystalImage.raycastTarget = false` in `BuildManaCrystalRow`.
  - **Test:** R6.9:
    - Drag Fireball onto the crystals: you get the invalid-drop log and no damage.
    - Rebirth's Touch onto the crystals is invalid.
    - Dropping on the portrait still works.
    - The End Turn button is unaffected.

- [ ] **A-03 · Low–Medium · M · Risk: Medium — Overlapping spell VFX corrupt the single shared particle renderer.**
  - **Source:** previous audit 2c-3.
  - **Where:** `UIParticleBurstRenderer.ShowAt` / `ShowAtRegion`, `SpellAnimationSequencer.PlaySchoolBurst` / `BoardSweepRoutine`.
  - **Problem:**
    - `ShowAt` moves the one `RawImage`, so a burst still fading jumps to the next burst's position.
    - A point spell cast during Blizzard's 1.2s sweep runs `RestorePointBurstFraming` and collapses the sweep into point framing.
    - This becomes much more likely once AI plays animate (G-02) or play gets faster.
  - **Fix (pick after G-01):**
    - (a) A small pool of renderers (camera, RT and RawImage), each leased per effect until its particles finish.
    - (b) One full-canvas capture camera and RT. Particles are positioned in world space from their screen position, so there's no per-burst reframing at all. Recommended if G-01 keeps Overlay.
    - (c) If G-01 moves to a Camera-space canvas, drop the RT workaround entirely.
  - **Test:** R7.9. Blizzard, then Frostbolt within 1s: the sweep finishes intact and the Frost burst is correct. Two point spells back to back both render in place.

- [ ] **A-04 · Low · S · Risk: Low — The AI keeps playing cards after the opponent is dead.**
  - **Source:** fresh (the "game ending mid-action" lens).
  - **Where:** `AIController.TakeTurn` card loop.
  - **Problem:** `AttackPhase` stops once `opponent.Health <= 0`, but the card loop doesn't. After a lethal spell to the face, the AI goes on spending cards, including more face damage. The win check only runs after `TakeTurn` returns.
  - **Fix:** Add `if (opponent.Health <= 0) break;` at the top of each card-loop pass (both loops), and skip `AttackPhase` when the opponent is already dead.
  - **Test:** R13.8, using a harness: Player One at 3 HP, and the AI holding Arcane Bolt and Frostbolt with 5 mana. The AI casts one spell, the game ends, and the second card stays in its hand (check the log).

- [ ] **A-05 · Low · S · Risk: Low — Mulligan cards can be drag-ghosted.**
  - **Source:** previous audit 1d.
  - **Where:** `CardView.SetCardForMulligan`, `CardView.OnBeginDrag`.
  - **Problem:** `canDrag` is null on mulligan cards, and null means allowed. A ghost spawns and follows the cursor, and nothing resolves on release.
  - **Fix:** In `OnBeginDrag`, return when `onDragEnded == null`. Or set `canDrag = () => false` in `SetCardForMulligan`.
  - **Test:** R2.6. Dragging a mulligan card shows no ghost. The click toggle still works, and hand drags still work.

- [ ] **A-06 · Low · S · Risk: Low — `UIParticleBurstRenderer` throws a null reference instead of its promised warning when fields are unassigned.**
  - **Source:** previous audit 2c-6.
  - **Where:** `UIParticleBurstRenderer.ShowAt` → `RestorePointBurstFraming` → `EnsureRenderTexture`.
  - **Problem:** `Awake` warns and returns, but the next `ShowAt` still calls `EnsureRenderTexture`, which dereferences `burstCamera` and `displayImage`.
  - **Fix:** Early-return from `ShowAt`, `ShowAtRegion` and `EnsureRenderTexture` when `burstCamera` or `displayImage` is null.
  - **Test:** Code review. Optionally, unassign `displayImage` in a throwaway scene copy, cast a spell, and confirm a single warning and no exception.

- [ ] **A-07 · Low · S · Risk: Low — `HandFanLayout` keeps the previous game's destroyed views after a restart.**
  - **Source:** previous audit 1d (Constraint 27 class).
  - **Where:** `HandDisplay.RenderHand` (the `hand == null` early return).
  - **Problem:** `RenderHand(null)` returns before `ApplyLayout`, so `views` still lists destroyed `CardView`s until the mulligan is confirmed. The null checks make it harmless today; it's a trap for future code.
  - **Fix:** When `hand == null`, still call `fanLayout.ApplyLayout(new List<CardView>(), …)` after the destroy loop.
  - **Test:** R16.2. Restart and hover the empty hand area during the mulligan: no errors. After confirming, the fan behaves normally.

- [ ] **A-08 · Low (latent) · S · Risk: Low — The mana crystal row can't show temporary mana above `MaxMana`.**
  - **Source:** fresh.
  - **Where:** `FaceView.BuildManaCrystalRow`.
  - **Problem:**
    - It loops `MaxMana` times and lights `CurrentMana` of them, so a Coin-boosted `CurrentMana > MaxMana` shows as all crystals lit, with the extra crystal missing.
    - It isn't reachable today. Only Player One has a row, Player One never gets The Coin, and Mage Pupil costs more than it gives.
    - It becomes live once the opponent's row is shown (F-13) or Player One ever gains mana.
  - **Fix:** Loop to `Mathf.Max(MaxMana, CurrentMana)`, styling crystals beyond `MaxMana` as temporary.
  - **Test:** Harness: give Player One The Coin on turn 2. The row shows 3 crystals, with 1 marked temporary.

- [ ] **A-09 · Low (latent) · S · Risk: Low — `CardView`'s `ArtworkImage` is point-anchored, which violates Constraint 14.**
  - **Source:** fresh.
  - **Where:** `CardView.prefab` → `ArtworkImage`: anchors (0.5, 0.5), `SizeDelta` 160×218.
  - **Problem:** Constraint 14 says any child that fills `MinionView` or `CardView` must use stretch anchors. `MinionView` complies; `CardView` doesn't. Any layout that shrinks a card (the mulligan panel's `HorizontalLayoutGroup`, or any graphics-overhaul layout) will overflow the art. Only Goblin has art today.
  - **Fix:** Stretch anchors (0,0)–(1,1) with `SizeDelta` (0,0), as on `MinionView`.
  - **Test:** R2 and R4 with Goblin. Its art fits in the mulligan row, the fanned hand, and the drag ghost.

- [ ] **A-10 · Low · S · Risk: Low — The health gem can show negative numbers.**
  - **Source:** fresh (the "health at 0 or below" lens).
  - **Where:** `FaceView.SetPlayer` (`player.Health.ToString()`).
  - **Problem:** Overkill and fatigue past 0 show "-3" on the gem at game over. A-04 makes this worse.
  - **Fix:** Display `Mathf.Max(0, player.Health)`. The model value stays as it is, because `CheckWinCondition` reads it.
  - **Test:** R15.1. A lethal hit for more than the remaining health shows "0".

- [ ] **A-11 · Low now, Medium at first build · S · Risk: Low — VFX resource lifetime problems.**
  - **Source:** fresh.
  - **Where:** `SpellBurstFactory.ApplyUnlitMaterial`, `UIParticleBurstRenderer.EnsureRenderTexture`.
  - **Problem:**
    1. **Shader lookup:** `Shader.Find("Universal Render Pipeline/Particles/Unlit")` only works in a build if that shader is referenced by an asset or listed under Always Included Shaders. Otherwise the code falls back to `Sprites/Default`, which looks different, or logs an error.
    2. **Material leak:** `new Material(shader)` runs on every burst and is never destroyed. Destroying the particle GameObject doesn't free it.
    3. **RenderTexture leak:** the RenderTexture is never released, because there's no `OnDestroy`.
  - **Fix:**
    - Add a serialized particle `Material` asset (URP Particles/Unlit) on `UIParticleBurstRenderer` or `SpellAnimationSequencer`, passed into the factory and assigned as `sharedMaterial`. Per-burst colour already comes from `colorOverLifetime`.
    - Add `OnDestroy` to release the RT.
  - **Test:**
    - R7.7–R7.8: all five effects (4 bursts and the sweep) look identical before and after.
    - Memory profiler: the material count stays flat after 20 casts.
    - E-10: the build looks the same.

- [ ] **A-12 · Low · S–M · Risk: Low — Known cosmetic layout issues.**
  - **Source:** STATUS Known Issues ("Cosmetic").
  - **Where:** `BoardPanel` / `OpponentBoardPanel` (scene), `MinionView.prefab` `NameText`.
  - **Problem:**
    - The minion name is clipped at the left edge.
    - Board panels need more vertical separation.
    - `MulliganPanel` shares `HandPanel`'s screen position. That's fine because the mulligan panel is hidden after confirm.
  - **Fix / decision:** Fold these into the graphics overhaul's layout pass. Write that decision here, or fix the `NameText` width or overflow mode now (S).
  - **Test:** R1.4 and R5.

---

## 4. B — Rule and behaviour inconsistencies

- [ ] **B-01 · Medium (latent) · M · Risk: Medium — `TargetRequirement` validity is decided in three places and enforced at none.**
  - **Source:** fresh (the human-vs-AI and Constraint 26 lenses).
  - **Where:**
    - `CardDragResolver.ResolveCardDrag` decides whether a human drop is valid.
    - `AIController.SelectEffectTarget` decides which valid target the AI picks. Its comment admits "any new TargetRequirement case needs to be added to both places".
    - `GrowthEffect` and `HealEffect` re-check their own halves.
    - `PlayerHand.PlayCard` doesn't check the requirement at all.
  - **Problem:**
    - This is the project's most frequent bug pattern (target defaults, then damage classification), still open in one more place.
    - Today every effect that has a restriction re-checks it, so nothing is exploitable.
    - But a future `AnyMinion` damage spell, or a `Friendly` buff, would accept any `Target` from the AI with no check anywhere.
    - When a restriction is enforced inside `Execute`, a violation still spends the card and the mana.
  - **Fix:**
    1. Add `CardTargeting.IsValidTarget(CardData, Player caster, Target, Board)` next to `DefaultTarget` in `CardData.cs` (Constraint 13: no new file).
    2. `PlayCard` rejects an invalid target before spending anything. That makes it authoritative, alongside the existing dead-target guard.
    3. `ResolveCardDrag` builds a candidate target and asks `IsValidTarget`, instead of its own per-requirement branches.
    4. The AI filters its candidates through `IsValidTarget`.
    5. Effect-level checks stay as defence in depth (Constraint 26).
  - **Test:** R6, the full targeting matrix (every spell × every zone), plus R13 AI turns. Expected results are unchanged except where §12 marks them.

- [ ] **B-02 · Low (latent) · S · Risk: Low — Minion `onPlayEffect` targeting diverges between the human and AI paths.**
  - **Source:** previous HISTORY "open, noted for later" (1), extended.
  - **Where:** `CardDragResolver.ResolveCardDrag` (minion branch → `DefaultTarget`), `AIController.TakeTurn` / `SelectEffectTarget`.
  - **Problem:** For a minion whose effect needs a chosen target (`Any` / `AnyMinion` / `Friendly`):
    - A human drop gets null. The minion is summoned and the effect is skipped with a warning.
    - The AI calls `SelectEffectTarget` and does pick a target.
    - With `AnyMinion` and no friendly minion, the AI skips the whole card.
    - No such card exists today, but Tier 2 battlecries will create them.
  - **Fix:**
    - Until targeted battlecries are designed (G-07), make the combination impossible to author: a `CardData.OnValidate` warning for `cardType == Minion && RequiresChosenTarget`.
    - Make the AI use `DefaultTarget` for minion cards, matching the human path.
  - **Test:** Create a scratch minion asset with `Any` and confirm the warning. Delete the asset after.

- [ ] **B-03 · Low · S · Risk: Low — Self-target spells fire a projectile at the caster's own portrait.**
  - **Source:** fresh.
  - **Where:** `CardDragResolver.TriggerSpellAnimation`.
  - **Problem:**
    - For `None` / `Self` cards, the target is `Target(caster)`, so `ResolveViewTransform` returns your own face and a white square flies there. There's no flash and no burst, because school is `None`. This happens with The Coin and Mage Pupil today.
    - `TriggerSpellAnimation` also doesn't check `cardType`, so any future minion with an `onPlayEffect` will fire a projectile on summon.
  - **Fix:**
    - Skip travel when `!CardTargeting.RequiresChosenTarget(card.Data)` and the shape is `SingleTarget`. Optionally play the school burst at the caster's face instead.
    - Skip `CardType.Minion` until battlecry visuals are designed.
  - **Test:** R6.7–R6.8. The Coin (Player Two, manual) and Mage Pupil play with no projectile. Blizzard's sweep is unchanged.

- [ ] **B-04 · Low · M · Risk: Medium — Freeze timing differs from Hearthstone. Needs a decision.**
  - **Source:** fresh (the freeze-timing lens).
  - **Where:** `Minion.Freeze` / `ResetForNewTurn`, `TurnManager.StartTurnFor`.
  - **Problem:** Thawing happens at the start of the controller's next-but-one turn (Constraint 24's consume-then-clear). That gives two differences from Hearthstone:
    1. A minion you freeze on your own turn, before it attacks (Frostbolt on your own minion), stays frozen through your entire next turn. In Hearthstone it thaws at the end of the current turn.
    2. An enemy minion frozen on your turn keeps its frozen tint through your following turn. In Hearthstone it thaws at the end of its controller's turn.
  - **Fix / decision:**
    - Option A: keep the current rule. Write it down as house rule.
    - Option B: thaw at the end of the controller's turn. `TurnManager.EndTurn` handles the ending player's minions before switching: a frozen minion thaws unless it was frozen during this turn after it had already attacked.
    - Either way, re-run Constraint 24's four-turn trace.
  - **Test:** R9 (four-turn trace for enemy freeze, Blizzard, and self-freeze).

- [ ] **B-05 · Low (latent) · S · Risk: Low — 0-attack minions can attack.**
  - **Source:** STATUS Known Issues ("Smaller").
  - **Where:** `Minion.CanAttack`, `Combat.TryAttack`.
  - **Problem:** A 0-attack minion can swing, take retaliation damage and deal nothing. No current card has 0 attack. Tier 2 and buff or debuff cards would make it reachable.
  - **Fix:** `CanAttack` also requires `CurrentAttack > 0`. The AI already filters on `CanAttack`, so both paths change together. Add a fail reason to `TryAttack`'s message chain.
  - **Test:** Harness: set a minion's attack to 0. It can't be selected, and the AI doesn't attack with it.

- [ ] **B-06 · Low · S · Risk: Low — Clicking an exhausted own minion silently keeps the previous selection.**
  - **Source:** fresh.
  - **Where:** `CombatInputController.OnMinionClicked`.
  - **Problem:** With minion X selected, clicking your own minion Y that can't attack (sick, attacked or frozen) does nothing visible. X stays selected and there's no message.
  - **Fix:** Deselect X and log Y's reason, reusing `TryAttack`'s fail-reason wording. Or keep the behaviour and log the reason only. Decide which.
  - **Test:** R8.2.

---

## 5. C — Code quality

- [ ] **C-01 · Low · S · Risk: none — Stale comments.**
  - **Source:** previous audit 1d, refreshed.
  - **Where:**
    - `CardDragResolver.cs:12-19`: "Constructed once in EffectTester.Start()", and the "Constraint 15 decision" is resolved.
    - `CombatInputController.cs:8`: says "Start()". It's built per game in `BeginNewGame`.
    - `CardView.cs:208`: "drop resolution lives in EffectTester". It's in `CardDragResolver`.
    - `HandDisplay.cs:71-74` and `FaceView.cs:19-22`: say OpponentHandPanel has no `HandFanLayout`. It has one.
    - `HandFanLayout.cs:8`: says "for HandPanel". It's on both hand panels.
    - `SpellAnimationSequencer.cs:37-39` and `:118-119`: "removed from the model … in PlayerHand.PlayCard" / "RemoveDeadMinions ran … in PlayerHand.PlayCard". The minion is only `IsDead` at that point; the sweep runs later, in `GameManager.AfterGameAction`.
    - `SpellAnimationSequencer.cs:105`: "no CardEffect subtype for heal/buff". `HealEffect` and `GrowthEffect` exist.
    - `SpellAnimationSequencer.cs:132-135`: build-step history.
  - **Fix:** Rewrite each to describe the current state.
  - **Test:** Compile. No behaviour change.

- [ ] **C-02 · Low · S · Risk: Low — Unused members.**
  - **Source:** previous audit 1d.
  - **Where:** `CardView.Card` (zero readers, left over from the removed `HashSet<CardView>` workaround), `UIParticleBurstRenderer.BurstSpawnPoint`, and the default `count = 5` on `PlayerHand.DrawOpeningHand` (callers pass 3 and 4).
  - **Fix:** Delete the first two. Make `count` required.
  - **Test:** Compile, then R2 and R3.

- [ ] **C-03 · Low · S · Risk: Low — Unreachable code.**
  - **Source:** STATUS Known Issues ("Smaller"), the `AICombatEvaluator` row.
  - **Where:** `AIController.SelectEffectTarget` final `else`; the `AICombatEvaluator.IsUnfavorableTrade` safety-net branch in `SelectAttackTarget`.
  - **Fix:**
    - Replace the final `else` with a `Debug.LogWarning` for an unknown requirement, returning null.
    - **Decide** whether to keep the safety net as documented insurance for AI step 5 (G-09) or delete it.
  - **Test:** Compile, then R13.

- [ ] **C-04 · Low · S · Risk: none — `[Header("Face UI")]` is attached to a private field, so the header never shows.**
  - **Source:** previous audit 1d.
  - **Where:** `EffectTester.cs:26-27`.
  - **Fix:** Move the attribute down onto `faceView`.
  - **Test:** The Inspector shows the "Face UI" header.

- [ ] **C-05 · Low · S · Risk: Low — `HandFanLayout.interactionEnabled` is a public, tooltipped Inspector field that `ApplyLayout` overwrites on every render.**
  - **Source:** previous audit 1b.
  - **Fix:** Make it a private field (not serialized). The scene keeps a harmless stale `interactionEnabled: 1` key until it's re-saved.
  - **Test:** R4 and R14. Hover works on the player's hand, the opponent's hand is static with manual mode off, and interactive with it on.

- [ ] **C-06 · Low · S · Risk: Low — `FaceView.startingDeckSize = 30` is a placeholder that's always overwritten.**
  - **Source:** previous audit 1d.
  - **Fix:** Initialise it to 0, and treat 0 as "no ratio" in `UpdateDeckPile`, which should show all layers while remaining > 0.
  - **Test:** R12.1. The deck pile behaves as before.

- [ ] **C-07 · Low · S · Risk: none — `CardView` naming is out of date.**
  - **Source:** previous audit 1d.
  - **Where:** `CardView.SetCard(CardInstance cardData, …)` / `SetCardForMulligan(CardInstance cardData, …)`, plus the log texts "called with a null CardData".
  - **Fix:** Rename to `cardInstance` and fix the log text.
  - **Test:** Compile.

- [ ] **C-08 · Low · S · Risk: Low–Medium — The turn-start sequence exists in three copies.**
  - **Source:** previous audit 2b.
  - **Where:** `GameManager.OnEndTurnClicked` (end turn → draw → log → win check, twice), `GameManager.BeginFirstTurn` (draw → log → win check), and the commented revert line in `EffectTester.OnMulliganComplete`.
  - **Fix:** Extract `private void StartNextTurn()` (EndTurn, then the first-turn steps) and `private void BeginTurnForCurrentPlayer()` (draw, log, win check). Keep `BeginFirstTurn`'s revert note accurate.
  - **Test:** R3, R12 and R13. The turn and draw counts match §12's expected numbers, and the AI's turn still runs.

- [ ] **C-09 · Low · S · Risk: Low — Duplicated flash and game-over code.**
  - **Source:** previous audit 2b.
  - **Where:** `MinionView.FlashRoutine` and `FaceView.FlashRoutine` are near-identical. `GameManager.CheckWinCondition` repeats the game-over UI block in its draw and winner branches.
  - **Fix:**
    - Extract a static `FlashUtil.Flash(Graphic, Color baseColor, Color flash, float duration)` coroutine.
    - Extract `ShowGameOver(string text)`.
  - **Test:** R7.3 (face flash), R7.5 (lethal minion flash), R15 (win and lose text).

- [ ] **C-10 · Low · S · Risk: Low — `SpellBurstFactory` duplicates its gradients.**
  - **Source:** fresh.
  - **Where:** `CreateFrostBurst` and `CreateFrostSweep` share an identical gradient, and every builder rebuilds gradients and size curves inline.
  - **Fix:** Add a private `MakeGradient(...)` helper and a shared Frost palette constant.
  - **Test:** R7.7–R7.8. Visually identical.

- [ ] **C-11 · Low · S · Risk: Low — The `MinionView.IsHeld` getter has side effects.**
  - **Source:** fresh.
  - **Where:** `MinionView.IsHeld` logs, zeroes `holdCount` and disables `overrideSorting` when the TTL lapses.
  - **Problem:** A property that changes state on read surprises debuggers and any future reader. Inspecting it in the debugger can expire a hold.
  - **Fix:** Make `IsHeld` a pure check. Move the expiry into an explicit `ExpireHoldIfStale()` that `BoardDisplay.RenderBoard` calls.
  - **Test:** R7.5 (lethal hold), R16.4 (restart during an animation).

- [ ] **C-12 · Low · S · Risk: none — Effect logs are inaccurate.**
  - **Source:** fresh.
  - **Where:** `HealEffect` logs "Healed X for 6" even when it's clamped. `DealDamageEffect` and `FrostDamageEffect` don't name the target.
  - **Fix:** Log the actual amount healed and the target's name.
  - **Test:** R6.6. Rebirth's Touch on a lightly damaged minion logs the real amount.

- [ ] **C-13 · Low · S · Risk: Low — The `FaceView` prefab carries dead content.**
  - **Source:** fresh.
  - **Where:** `FaceView.prefab` still has a `HealthText` child GameObject and a serialized `healthText:` key for a field that no longer exists. Both scene instances strip the child through `m_RemovedGameObjects`.
  - **Fix:** In the Editor, delete `HealthText` from the prefab and save it. Then confirm both instances' removed-object overrides go away cleanly.
  - **Test:** R1.3. Both health gems and portraits are unchanged, with no "New Text" placeholder.

- [ ] **C-14 · Low · S · Risk: Low — The `CardView` prefab bakes `cardBackground` colour alpha at 0.392, and code compensates.**
  - **Source:** fresh (documented in the `CardView` row).
  - **Fix:** Set the prefab colour to opaque white so the asset and the code agree. The code keeps forcing `normalColor` regardless.
  - **Test:** R4 and R14. Face-up and face-down cards and mulligan cards are all fully opaque.

- [ ] **C-15 · Low · S · Risk: Low if done first — Force-reserialize assets so their YAML is uniform (Constraint 12).**
  - **Source:** Constraint 12, Next Steps 14.
  - **Where:**
    - 17 of 20 `CardData` assets omit at least one field. `targetRequirement` is missing on all 12 minions, `visualShape` on 17, `spellSchool` on 14, `hasTaunt` on 14.
    - `MinionView.prefab` omits `frozenColor`, `reactionDuration` and `damageFlashColor`.
    - `FaceView.prefab` omits most fields.
  - **Fix:**
    1. Run `AssetDatabase.ForceReserializeAssets` on `ScriptableObjects/` and `Prefabs/`, through a throwaway Editor menu script or by touching each asset in the Inspector.
    2. Review the diff: it should be **added keys only, at their current C# defaults**.
    3. Commit it as its own YAML-only change.
    4. ⚠️ Do this **before** any initializer change, and before the overhaul adds fields.
  - **Test:** The diff review above, then R1 and R6 smoke.

- [ ] **C-16 · Low · S · Risk: Low — `HandFanLayout.StillWithinExpandedRect` uses `eventData.pressEventCamera`.**
  - **Source:** previous audit 1b.
  - **Problem:** It should use `enterEventCamera`. It only works today because a Screen Space – Overlay canvas passes null for both.
  - **Fix:** Switch to `enterEventCamera`. That's safe now whatever G-01 decides.
  - **Test:** R4.3 (exit hysteresis).

- [ ] **C-17 · Low · S · Risk: Low — `TurnManager.TurnNumber` counts half-turns.**
  - **Source:** STATUS Known Issues ("Smaller").
  - **Fix:** Rename it to `HalfTurnNumber`, or add `FullTurnNumber => (TurnNumber + 1) / 2` for the logs and any future turn indicator (G-03).
  - **Test:** Compile. The log turn numbers read as intended.

- [ ] **C-18 · Low · S · Risk: none — Add a defensive re-check after the new hold wait.**
  - **Source:** fresh (review of `844293d`).
  - **Where:** `SpellAnimationSequencer.TravelAndReactRoutine`, after `yield return new WaitForSeconds(heldMinionView.reactionDuration)`.
  - **Problem:** If anything ever destroyed the held view during the wait, `Destroy(heldMinionView.gameObject)` would throw. It's not reachable today: the hold TTL and the coroutine both run on scaled time with the same `maximumDeltaTime` cap, and nothing else destroys a held view.
  - **Fix:** Re-check `if (heldMinionView != null)` after the yield.
  - **Test:** R7.5. Lethal behaviour is unchanged.

---

## 6. D — Documentation inaccuracies

Each item gets a STATUS or HISTORY wording fix only. Test: re-read the edited passage against the named code, per the doc-drift discipline.

- [ ] **D-01 · Low · S** — Constraint 6 says `EffectTester.AfterGameAction()`. It's `GameManager.AfterGameAction()`.
- [ ] **D-02 · Low · S** — Constraint 7 says `EffectTester.ResolveAttack`. It's `CombatInputController.ResolveAttack`.
- [ ] **D-03 · Low · S** — Known Issues says "Fatigue and the 7-minion board cap … never triggered", and Next Steps 7 matches. But the phase 4 playtest confirmed "fatigue triggers correctly". Narrow both to the board cap, plus fatigue for Player Two (E-08).
- [ ] **D-04 · Low · S** — HISTORY's "AI plan step 3 of 5 and step 4 of 5" says step 4 got a "later confirmation" in Verification Status. STATUS says step 4 is **not** playtest-confirmed. Fix the HISTORY wording.
- [ ] **D-05 · Low · S** — The Project Goal section says "Unity 6.5 (60000.5.2f1)". The Editor is `6000.5.2f1`.
- [ ] **D-06 · Low · S** — The `EffectTester` row credits `Start()` with building the decks, hands and controllers. That's `BeginNewGame()`. `Start()` wires the four button trampolines and calls `BeginNewGame()`.
- [ ] **D-07 · Low · S** — Constraint 22 says `BeginHold` is safe only once the minion "has already been removed from the model (`Minion.IsDead` true…)". At `BeginHold` time the minion is `IsDead` but still in `BoardMinions`; `RemoveDeadMinions` runs right after, in `AfterGameAction`. No duplicate view appears because `RenderBoard` skips `IsDead` minions (Constraint 10) and the sweep follows. Reword.
- [ ] **D-08 · Low · S** — Constraint 25 lists the point bursts as "Fire/Arcane/Frost". Nature uses `ShowAt` too.
- [ ] **D-09 · Low · S** — **Undocumented project setting:** `EditorSettings.asset` has `m_EnterPlayModeOptions: 1`, so Reload Domain is disabled. Static fields and runtime-mutated ScriptableObjects persist between Play sessions in the Editor. There's no current static state, but this is a trap for the overhaul (for example a static cache or pooled-VFX registry). Add a Live Constraint.
- [ ] **D-10 · Low · S** — The phase 3 note says the 10-crystal cap "match[es] `Target.GainMana`'s existing clamp". The row's cap comes from `TurnManager.RefillMana`; the `GainMana` clamp itself has never been playtested (Known Issues). Clarify, and link E-07.
- [ ] **D-11 · Low · S** — A resolved Known Issue ("Spells can only ever target the enemy face") says targeting is "resolved in `EffectTester.ResolveCardDrag`". It's `CardDragResolver.ResolveCardDrag`.
- [ ] **D-12 · Low · S** — HISTORY restart step 7 says "a killed minion's view … can visibly linger over the new game's mulligan screen". A minion kill can't end the game, so a held minion view can't overlap a restart; only the face flash and projectile can. Clarify, consistent with the 2026-10-01 Verification Status note.
- [ ] **D-13 · Low · S** — Verification Status says "mulligan both sides". Player Two's mulligan is AI-only; there's no human Player Two mulligan, even in manual mode (F-14). Clarify.
- [ ] **D-14 · Low · S** — HISTORY's `FreezeAllEffect` entry says it derives the opponent via `GetOpponent(target.TargetPlayer)`. The code now uses `caster`. Add a dated note rather than rewriting history.
- [ ] **D-15 · Low · S** — Next Steps 5 says "the remaining 14 cards (only Goblin has art)". 19 of the 20 card assets lack art (18 pool cards plus The Coin).
- [ ] **D-16 · Low · S** — Next Steps 15 ("Record `BoardPanel`'s corrected Pos Y and `HandPanel`'s Rect Transform values") is still open. Record the values from the scene, or drop the step if the overhaul will redo the layout.

---

## 7. E — Verification gaps (each needs a playtest)

- [ ] **E-01 · Medium · S — The 7-minion board cap has never been triggered.**
  - **Source:** Known Issues, Next Steps 7.
  - **Test (R5.5):**
    - Fill your board to 7. An 8th minion is rejected with the "board is full" log, and the card stays in hand with mana unspent.
    - Fill the AI's board to 7 using manual mode, then let it take a turn holding a minion: no errors.
    - Check the 7-minion row layout and art sizing (Constraint 14).

- [ ] **E-02 · Medium · S — Hand-size burn has never been triggered.**
  - **Source:** fresh.
  - **Test (R12.4):** Reach 10 cards in hand, then end turn. The drawn card is burned (log), the hand stays at 10 and the deck count goes down by 1. The 10-card fan fits within `maxTotalWidth`.

- [ ] **E-03 · Medium · S — AI lethal-detection override has never been playtest-confirmed.**
  - **Source:** Next Steps 12 step 4.
  - **Test (R13.5):** Stage it in manual mode: AI minions with total attack at least Player One's health, and no Taunt. Turn manual mode off before End Turn. Every attacker goes face; none trades.

- [ ] **E-04 · Medium · S — Manual control mode's turn gating has never been re-tested.**
  - **Source:** Known Issues, Next Steps 9.
  - **Test (R14.3–R14.5):**
    - On Player Two's turn, Player One can't drag, attack or use the Hero Power, and the reverse holds on Player One's turn.
    - Hero Power charges the acting player.

- [ ] **E-05 · Low · S — The same minion attacking twice in a turn.**
  - **Source:** Next Steps 8.
  - **Test (R8.5):** After an attack, the minion can't be re-selected. Optionally, a harness calls `Combat.TryAttack` a second time and gets "has already attacked this turn".

- [ ] **E-06 · Low · S — Own-side attack rejection can't be reached from the UI.**
  - **Source:** Next Steps 1.
  - **Test:** A harness calls `TryAttack(ownMinion, Target(ownOtherMinion))` and `TryAttack(ownMinion, Target(ownFace))`, and both are rejected with "Cannot attack your own side". Or explicitly accept it as code-verified only, and record that decision.

- [ ] **E-07 · Low · S — The 10-mana cap with The Coin, and the new "gained no mana" log, have never been playtested.**
  - **Source:** Known Issues (the `GainMana` clamp), `dd6e467`.
  - **Test (R11.4):** In manual mode, have Player Two keep The Coin until 10/10, then play it. Mana stays at 10/10 and the log says "gained no mana — already at the 10-mana cap".

- [ ] **E-08 · Low · S — Player Two (AI) fatigue and death by fatigue have never been tested.**
  - **Source:** fresh. Player One's fatigue is confirmed.
  - **Test (R12.3):** A long game, or a harness that empties Player Two's deck. Escalating fatigue, then the AI dies at the start of its turn: "Player One wins!" shows and the AI's turn is skipped.

- [ ] **E-09 · Low · S — No lethal Fireball on a minion since `dd6e467` / `844293d`.**
  - **Source:** 2026-10-01 note (it shares `DealDamageEffect` with Arcane Bolt).
  - **Test (R7.5):** Hold, flash, Fire burst, then death.

- [ ] **E-10 · Medium · S–M — A standalone player build has never been made.**
  - **Source:** fresh.
  - **Test:** Build for Linux and play R1–R7. Check that the bursts and sweep render with the right shader (A-11), that music plays, and that the input module works.

- [ ] **E-11 · Medium · S — Aspect ratios and resolutions other than the design setup are untested.**
  - **Source:** fresh. The `CanvasScaler` is 1920×1080 matching width, and Craig's desktop is 5120×1440.
  - **Test (R17):** Game view at 16:9, 16:10, 21:9 and 32:9. Hands, boards, faces, crystals and buttons stay on-screen and clickable, and the bursts land on their targets.

- [ ] **E-12 · Low · S — Freeze edge cases.**
  - **Source:** fresh.
  - **Test (R9.3–R9.5):**
    - Frostbolt your own minion.
    - Re-freeze an already frozen minion.
    - Blizzard an empty board: no errors, mana spent.
    - Freeze a minion that has summoning sickness.

- [ ] **E-13 · Low · S — Overlapping holds, and summoning during a hold.**
  - **Source:** fresh.
  - **Test (R7.10):**
    - Two lethal spells in quick succession on two different minions. Both hold and flash, the layout is stable, and there are no TTL warnings.
    - A lethal spell on one of your own minions while your board is full, then immediately play a minion. The 8th child squeezes the layout for under 1s with no errors.

- [ ] **E-14 · Low · S — AI turns with no useful moves have never been tested deliberately.**
  - **Source:** fresh (the "AI with no legal moves" lens).
  - **Test (R13.6–R13.7):**
    - The AI has an empty hand and no minions.
    - Only Verdant Growth in hand with no friendly minions: the card is skipped.
    - Only unaffordable cards.
    - Blizzard into an empty board. Today it's played and wasted; see G-09.

---

## 8. F — Networking architecture

The working assumption, from `NETWORKING_PREP.md`, is one authoritative host. "Safe now" means the item can be done without designing the network layer and without changing single-player behaviour.

| ID | Item | Safe now? | Size | Notes |
|---|---|---|---|---|
| F-01 | Stable integer IDs on `CardInstance` and `Minion` | **Yes** | S | Additive: assigned at construction from a per-game counter on `PlayerHand` / `Board`. No behaviour change. |
| F-02 | ID-based `Target` wire form | No | M | Depends on F-01 and the protocol design. |
| F-03 | `CardData` card-ID registry | After C-15 | S–M | An explicit `cardId` field is a Constraint 12 hazard, so re-serialize first. Defer until networking. |
| F-04 | Seedable shuffle RNG (inject `System.Random` into `PlayerHand`) | **Yes** | S | Also makes deterministic test harnesses possible. Background and music picks stay on `UnityEngine.Random`. |
| F-05 | Hidden information (clients get only the opponent's hand count) | No | L | Needs a state-snapshot design. |
| F-06 | Game rules out of the UI assembly (turn flow, Hero Power, win check, the `RemoveDeadMinions` sweep from `AfterGameAction`) | Partly | M | Pure refactor into a Core/Rules class with `GameManager` as a thin adapter. Do it after batches 1–7; it carries regression risk. |
| F-07 | **Turn ownership and game phase at the chokepoint**: `PlayCard`, `TryAttack`, Hero Power and EndTurn reject an actor that isn't `CurrentPlayer`, any action after `GameOver`, and any action before the mulligan completes | **Yes** | S–M | Previous audit 2a. Core can't see `GameOver` or the mulligan state, so add a rules-level guard (an `ActionGate` in Core fed by `GameManager`), or pass `TurnManager` into `PlayCard` / `TryAttack`. Also covers A-04 at the chokepoint. |
| F-08 | One "who may act" predicate replacing the 4 rebuilt actor checks and the view's `showAttackEligibility` duplicates | **Yes** | S | `GameManager.CanAct(Player)` reads `manualControlMode` once. Becomes "local seat" later. |
| F-09 | Event-driven spell animation (`CardPlayed{caster, card, target}` raised at the play chokepoint, so the sequencer subscribes instead of the input path calling it) | Partly | M | Lets AI plays animate (G-02) and remote plays later. Needs view positions captured before the refresh, so keep the "sample before refresh" rule. |
| F-10 | Lost refresh mid-drag: queue a refresh while `DragInProgress` and run it on drag end, and stop `dragInProgress` sticking if the dragged view is destroyed | **Yes** | S | No current trigger. A remote event would trigger it. |
| F-11 | All draws and the first-turn start on the host after **both** mulligans | No | M | Protocol-dependent. |
| F-12 | **Give The Coin after the mulligan** | **Yes** | S | Move `AddCardToHand(coinCard)` after `PerformMulligan()`. The AI's threshold never returns it, so the outcome is identical; only the order changes. |
| F-13 | Show the opponent's mana and deck count | Partly | M | Pairs with G-13 (`FaceView` split) and A-08. Best done in the overhaul. |
| F-14 | Both seats mulligan at the same time, including a human Player Two | No | M | Today the AI mulligans Player Two even in manual mode. |
| F-15 | View or engine types in the model: `Minion.Artwork` (`Sprite` in Core), and `UnityEngine.Debug` / `Random` in Core and Cards | Partly | M | Low priority. Hold an art key or ID instead of a `Sprite` once F-03 exists. |
| F-16 | Host-only game creation, rematch consent, AI host-only or disabled in PvP | No | M | Protocol-dependent. |

- [ ] F-01
- [ ] F-02
- [ ] F-03
- [ ] F-04
- [ ] F-05
- [ ] F-06
- [ ] F-07
- [ ] F-08
- [ ] F-09
- [ ] F-10
- [ ] F-11
- [ ] F-12
- [ ] F-13
- [ ] F-14
- [ ] F-15
- [ ] F-16

**Test for the safe-now items:** R14 (manual mode, both seats) and R13 (AI turns). F-07 adds a harness: call `PlayCard` for the non-current player, and an attack after `GameOver`. Both are rejected with a log, and nothing is spent.

---

## 9. G — Unbuilt features and design decisions

Write the decision next to each item, then tick it.

- [ ] **G-01 · Blocks the overhaul — Canvas render mode.** Keep Screen Space – Overlay, or move to Screen Space – Camera? This decides:
  - whether `UIParticleBurstRenderer`'s RenderTexture workaround survives, and which A-03 fix to use;
  - the null-camera screen-to-local conversions in `BoardDisplay.GetPanelScreenBounds`, `SpellAnimationSequencer.SpawnProjectile` / `PlaySchoolBurst` and `UIParticleBurstRenderer.ShowAt` / `ShowAtRegion` (`CardView.MoveGhostTo` already handles both modes, and C-16 fixes `HandFanLayout`);
  - how particles and 3D-ish effects compose with the cards.
- [ ] **G-02 · Blocks the overhaul — AI turn pacing and animation.**
  - The problem: `AIController.TakeTurn` runs synchronously inside one click, so AI spells, attacks and kills have no travel, flash or burst. Its minions just appear and vanish on the next refresh.
  - The decision: a coroutine-driven AI turn that emits per-action events (F-09), with a small delay between actions. This also sets the End Turn button's state during the AI's turn.
- [ ] **G-03 — Board and card affordances for the overhaul.** Spells currently have no rules text (`CardData.description` is never displayed). Other gaps:
  - no card-type or school indicator;
  - Taunt shown only as a text suffix and Frozen only as a tint;
  - no damaged-health colouring;
  - no "playable" glow;
  - no used or unaffordable state on the Hero Power button;
  - no turn indicator;
  - no feedback for a burned card or fatigue damage.
- [ ] **G-04 — Card pool balance and naming.**
  - Mage Pupil is a 4-mana **spell** that gains 1 mana: a strictly negative play, with a minion-like name.
  - Fireball (4 mana) is strictly worse than Arcane Bolt (3 mana), since both use `Effect_Deal3Damage`.
  - Wisp is a 2-mana 1/1.
  - Most descriptions are empty.
  - Decide intended values, or label these as test cards.
- [ ] **G-05 — Draw effect.** Next Steps 3. It needs a new `CardEffect`. `None` targeting already works on both paths.
- [ ] **G-06 — Card art.** Next Steps 5. 19 assets need art; see D-15.
- [ ] **G-07 — Tier 2 content, including the targeted-battlecry design.** Next Steps 14. Gated on C-15 and B-02.
- [ ] **G-08 — Spell-cast animation remainder.** Next Steps 17: card lift/zoom, discard-off, and a discard-pile anchor.
- [ ] **G-09 — AI step 5: board-state-aware spell use.** Next Steps 12. Covers value checks: don't cast Blizzard into an empty board, don't play The Coin with nothing to spend it on, don't heal a full-health target, and don't play Mage Pupil at all.
- [ ] **G-10 — Hero Power.** It's a placeholder (2 mana, 1 face damage, no targeting), and the AI never uses it. Real hero classes are Next Steps 14.
- [ ] **G-11 — Backgrounds and music.** Next Steps 11, plus: re-roll the background and track on Play Again? Currently they're only picked in `Start()`.
- [ ] **G-12 — Concede or restart at any time.** Play Again only appears at game over, so restarting during the mulligan, mid-turn or mid-animation is unreachable. If a concede or restart button is wanted, it needs teardown: stop the sequencer's coroutines, clear held views, cancel drags, and reset `UIParticleBurstRenderer` framing.
- [ ] **G-13 — `FaceView` split.** Extract `ManaCrystalRowView` and `DeckPileView` (previous audit 1c) when the overhaul animates crystals or the deck, or when the opponent's rows are shown (F-13).

---

## 10. Cross-cutting maps

### 10.1 Decisions the human and AI paths make separately

| Decision | Human path | AI path | Shared chokepoint? | Status |
|---|---|---|---|---|
| Default target for `None` / `Self` | `CardTargeting.DefaultTarget` | `CardTargeting.DefaultTarget` | Yes | Fixed `dfd75e1` |
| Is this effect damage? | `is IDamageEffect` | `is IDamageEffect` | Yes | Fixed `dd6e467` |
| Is this target valid for the requirement? | Drop classifier branches | `SelectEffectTarget` branches | **No** | **B-01** |
| Minion `onPlayEffect` target | `DefaultTarget` (null if a chosen target is required) | `SelectEffectTarget` | No | **B-02** |
| Dead target | `PlayCard` guard | `FirstLivingMinion` + `PlayCard` guard | Yes | Fixed `dfd75e1` |
| Mana and board-full checks | `PlayCard` | Pre-check + `PlayCard` | Yes | OK |
| Attack legality | `Combat.TryAttack` | `Combat.TryAttack` | Yes | OK |
| Whose turn / who may act | UI predicates ×4 + view eligibility | Invoked only by `GameManager` | **No** | **F-07, F-08** |
| Stop acting once the game is over | UI `isGameOver` gates | Attack loop only | No | **A-04, F-07** |
| Start-of-turn sequence | `OnEndTurnClicked` | Second copy in the AI branch | n/a | **C-08** |
| Spell animation | Input path only | Never | n/a | **F-09, G-02** |
| Mulligan | Human UI | Threshold (also for a human Player Two in manual mode) | n/a | **F-14** |
| Hero Power | Human only | Never | n/a | **G-10** |

### 10.2 Hard restrictions vs. where they're enforced (Constraint 26)

| Restriction | Enforced in game logic? | UI-only part |
|---|---|---|
| Never heal the opponent (face and minion) | ✅ `HealEffect.Execute` | Friendly drop gate |
| Growth only on a minion | ✅ `GrowthEffect.Execute` | AnyMinion drop gate |
| Mana goes to the caster | ✅ `GainManaEffect.Execute` | — |
| Freeze-all hits the opponent | ✅ `FreezeAllEffect` (derived from the caster) | — |
| Target must satisfy `TargetRequirement` in general | ❌ | Drop classifier (B-01) |
| Only the current player acts (play, attack, Hero Power, end turn) | ❌ | UI predicates (F-07) |
| No actions after game over or before the mulligan | ❌ | UI gates (F-07) |
| Mulligan happens exactly once | ❌ (`MulliganController` flag) | F-14 |
| Hand ≤ 10, board ≤ 7, mana, dead target | ✅ `PlayerHand` | — |
| Taunt, own side, sickness, frozen, already attacked | ✅ `Combat.TryAttack` | — |

### 10.3 Lifecycle and teardown

| Risk | Status |
|---|---|
| A view destroyed while something it hosts is still running (the lethal-flash class) | Lethal flash fixed (`844293d`). C-18 adds a defensive re-check. **A-01's fix reintroduces the risk.** Read its ⚠️ note before implementing. |
| Drag ghost outlives its card | Handled (`CardView.OnDestroy`) |
| `dragInProgress` stuck if the dragged card is destroyed | No current trigger. F-10 |
| Listeners stacking on restart | Handled (trampolines in `Start`, playtested over 4 cycles) |
| Persistent displays not reset on restart (Constraint 27) | Board, faces and hands handled. `HandFanLayout.views` is A-07. In-flight VFX, held views and renderer framing persist for about 1s (accepted). Background and music aren't re-rolled (G-11). |
| Statics persisting across Play sessions | None today. D-09 documents that domain reload is disabled. |
| Per-burst `Material` and the RenderTexture never freed | A-11 |

---

## 11. Fix batches (A–E plus the safe-now F items)

Batches are ordered from lowest to highest regression risk and grouped by file or area to keep churn down. Each batch follows the standing workflow:
- one component at a time;
- `dotnet build` of each touched assembly;
- let the Editor recompile, and check `Logs/Editor.log` for `error CS`;
- playtest the gate;
- commit;
- update STATUS and HISTORY when the batch is playtest-confirmed.

**Batch E0 — Baseline verification (no code).** E-01, E-02, E-03, E-04, E-05, E-07, E-08, E-09, E-11, E-12, E-13, E-14.
- **Gate:** run R5.5, R7.5, R7.10, R8.5, R9, R11.4, R12.3–R12.4, R13.5–R13.7, R14.3–R14.5 and R17.
- Record the results and file anything new as an A or B item.
- E-06 needs a harness, so do it in batch 6. E-10 waits for batch 4.

**Batch 1 — Documentation.** D-01 to D-16.
- **Risk:** none (docs only).
- **Gate:** re-read each edited passage against the named method. One commit.

**Batch 2 — Comments, dead code, naming.** C-01, C-02, C-03 (the `else` now; the safety-net decision recorded), C-04, C-06, C-07, C-12, C-17, C-18.
- **Files:** `CardDragResolver`, `CombatInputController`, `CardView`, `HandDisplay`, `FaceView`, `HandFanLayout` (comment only), `SpellAnimationSequencer`, `UIParticleBurstRenderer`, `PlayerHand`, `AIController`, `EffectTester`, `HealEffect`, `DealDamageEffect`, `FrostDamageEffect`, `TurnManager`.
- **Risk:** very low.
- **Gate:** smoke run of R1, R2, R3, R6.1–R6.3, R7.5, R12.1, R13.1 and R15.1. The console is clean.

**Batch 3 — Asset and prefab hygiene (Editor).** First C-15 (reserialize, its own commit), then A-02, A-09, C-13 and C-14.
- **Files:** `ScriptableObjects/**`, `Prefabs/**`.
- **Risk:** low, as long as C-15 goes first and its diff is added keys only.
- **Gate:** R1.3, R2 (with Goblin), R4, R6.9 (crystal-row drop is now invalid), R11.1 and R14.1–R14.2.

**Batch 4 — Small UI-side fixes.** A-05, A-06, A-07, A-10, C-05, C-11, C-16.
- **Files:** `CardView`, `UIParticleBurstRenderer`, `HandDisplay`, `FaceView`, `HandFanLayout`, `MinionView`, `BoardDisplay`.
- **Risk:** low.
- **Gate:** R2.6, R4, R7.5, R14.1–R14.2, R15.1, and R16 (two restart cycles).

**Batch 5 — VFX resource lifetime.** A-11, C-10, then **E-10** (the first standalone build).
- **Files:** `SpellBurstFactory`, `UIParticleBurstRenderer`, scene (Material reference).
- **Risk:** low.
- **Gate:** R7.7–R7.8 side by side with a pre-change capture; material count flat after 20 casts; E-10 build passes R1–R7.

**Batch 6 — Small rules and logic fixes.** A-04, B-05, B-06, C-08, C-09, plus the E-06 harness.
- **Files:** `AIController`, `Minion`, `Combat`, `CombatInputController`, `GameManager`, `MinionView`, `FaceView`.
- **Risk:** low to medium, because C-08 touches turn flow.
- **Gate:** R3, R8, R10, R12, R13 (including the A-04 harness), R15 and R16 (one cycle).

**Batch 7 — Safe-now networking seams.** F-12, F-08, F-10, F-07, F-01, F-04, in that order.
- **Files:** `EffectTester`, `GameManager`, `CombatInputController`, `CardDragResolver`, `PlayerHand`, `Combat`, `Board`, `CardInstance`, `Minion`.
- **Risk:** low to medium. F-07 adds new rejections on the shared path.
- **Gate:**
  - R2 and R3: The Coin is still in Player Two's hand on its first turn, with counts 6 / 33.
  - R13, R14 (all of it, both seats) and R15 (every input is inert after game over).
  - The F-07 harness rejects an off-turn `PlayCard` and an attack after game over, with nothing spent.

**Batch 8 — Target validity at the chokepoint.** B-01, B-02, B-03.
- **Files:** `CardData.cs` (`CardTargeting`), `PlayerHand`, `CardDragResolver`, `AIController`.
- **Risk:** medium. This touches every play.
- **Gate:** all of R6 (the full matrix), R7.1–R7.3, R13 and R14.6. Any matrix cell that changes must be one §12 marks as intended (The Coin and Mage Pupil lose their projectile).

**Batch 9 — Surviving-minion flash.** A-01.
- **Files:** `SpellAnimationSequencer`, `CardDragResolver`, `BoardDisplay` (plus `EffectTester` wiring if a lookup delegate is passed).
- **Risk:** medium. It shares code with the lethal hold.
- **Gate:** all of R7 (especially R7.4–R7.6 and R7.10), R8.1, and R16.4.

**Batch 10 — Freeze timing.** B-04, only once the decision is written.
- **Files:** `Minion`, `TurnManager`.
- **Risk:** medium.
- **Gate:** all of R9 (four-turn traces), R8.6, and R13 (the AI with frozen minions).

**Batch 11 — Particle overlap.** A-03, after G-01 is decided. It could be folded into the overhaul's first VFX step.
- **Files:** `UIParticleBurstRenderer`, `SpellAnimationSequencer`, scene.
- **Risk:** medium.
- **Gate:** all of R7, especially R7.9, plus E-10 re-run.

**Deferred or decision-only:**
- A-08: with F-13 / G-13.
- A-12: in the overhaul, unless the quick `NameText` fix is chosen.
- All of G: decide before the overhaul. G-01 and G-02 first.
- F-02, F-03, F-05, F-06, F-09, F-11, F-13 to F-16: the networking phase. F-06 and F-09 can start right after batch 9 if wanted.

**Final gate:** the full §12 regression script, end to end, with a clean console and no exceptions in `Logs/Editor.log`.

---

## 12. Full manual regression playtest script

**Setup:**
- Unity Editor, `TestScene`, Manual Control Mode **off** unless a step says otherwise.
- Clear the Console. Note where `Logs/Editor.log` ends.
- After every section: zero errors and zero warnings in the Console, unless a step expects a warning.
- ⚑ marks steps that need a temporary harness. Keep harnesses between markers and revert them before committing.

### R1 — Boot and presentation
- [ ] R1.1 Enter Play: a random board background and the music track play (logs name both).
- [ ] R1.2 The mulligan panel shows Player One's 3 cards face up. End Turn and Hero Power are hidden. Both hand panels are empty, and both boards are empty.
- [ ] R1.3 Both portraits show health gems reading 30, with no "New Text" placeholder and no leftover "Player: X HP" text. Portraits sway and breathe.
- [ ] R1.4 Board panels, faces, the deck pile (player only) and the crystal row (player only, empty until the mulligan is confirmed) are positioned as before. Note minion name clipping (A-12).

### R2 — Mulligan
- [ ] R2.1 Click a card: it greys out. Click again: it returns to normal.
- [ ] R2.2 Select 2 cards (try both copies of a duplicate if one is dealt) and confirm. Both are replaced, the log says "mulliganed 2 card(s)", and neither replaced copy comes back.
- [ ] R2.3 Restart (via a game over, R16) and confirm with nothing selected: the hand is unchanged.
- [ ] R2.4 Restart and mulligan all 3 cards: 3 new cards.
- [ ] R2.5 Confirm is clickable only once. The panel and button hide after confirming.
- [ ] R2.6 Drag a mulligan card: **no ghost** after A-05. Before A-05, a ghost appears and nothing happens.
- [ ] R2.7 The log shows Player Two's AI mulligan ran before the panel appeared. Player Two holds The Coin (visible with manual mode on).
- [ ] R2.8 With Goblin in the mulligan, its art fits the card (A-09).

### R3 — First turn and opening counts
- [ ] R3.1 After confirming: Player One has 4 cards and the deck reads 34 (all 5 pile layers). Mana is 1/1 (one lit crystal). End Turn and Hero Power are visible.
- [ ] R3.2 End Turn: Player Two (AI) gets 6 cards including The Coin, deck 33, on its first turn (log). The AI acts.
- [ ] R3.3 Back on Player One: 5 cards (if nothing was played), deck 33, mana 2/2.

### R4 — Fanned hand
- [ ] R4.1 Cards fan in an arc. Hover lifts, straightens and scales a card, and its neighbours spread.
- [ ] R4.2 Sweep quickly across the hand: no flicker, and draw order settles back after about 0.1s.
- [ ] R4.3 Hover a card's edge and hold still while the neighbours move: no oscillation (exit hysteresis).
- [ ] R4.4 Drag a card: the ghost is upright and unscaled, and the hovered card drops its lift. Other cards don't pop up as the cursor crosses them.
- [ ] R4.5 Drop on an invalid zone (empty space or the hand panel): "invalid zone" log, and the card stays in hand.
- [ ] R4.6 (E-02 setup) A 10-card hand fits within the fan width.

### R5 — Playing minions
- [ ] R5.1 Drag an affordable minion onto your board: it's summoned, mana is spent and crystals empty left to right. It can't attack this turn.
- [ ] R5.2 Drop a minion on the enemy board: rejected (invalid-zone log), nothing spent.
- [ ] R5.3 Drag an unaffordable minion onto your board: "not enough mana" log, nothing changes.
- [ ] R5.4 Shieldbearer shows "(Taunt)".
- [ ] R5.5 (E-01) Fill your board to 7. An 8th minion is rejected with "board is full", and the card and mana are kept. The 7-wide row's art stays inside each card. Repeat for the AI with manual-mode staging.

### R6 — Spell targeting matrix
Zones: enemy minion (EM), own minion (OM), enemy face (EF), own face (OF), empty board (EB), off-zone (X, the hand or empty space), own crystal row (CR).

- [ ] R6.1 **Fireball / Arcane Bolt** (Any): EM, OM, EF and OF all take 3 damage. EB and X are invalid. CR is invalid after A-02 (before A-02 it hits OF).
- [ ] R6.2 **Frostbolt** (Any): 1 damage plus a freeze on EM or OM. EF / OF take 1 damage and nothing freezes. EB and X are invalid.
- [ ] R6.3 **Verdant Growth** (AnyMinion): +3/+3 on EM or OM. EF, OF, EB and X are invalid. The buff persists across turns.
- [ ] R6.4 **Rebirth's Touch** (Friendly): an OM is healed (clamped at max) and OF is healed (clamped at 30). EM and EF are invalid.
- [ ] R6.5 **Blizzard** (Self): a drop on any recognised zone (EB, EM, OM, EF or OF) freezes every enemy minion. Your own board is untouched.
- [ ] R6.6 Rebirth's Touch on a minion missing 2 health logs the real amount after C-12.
- [ ] R6.7 **Mage Pupil** (Self spell): any recognised zone gives 4 mana for +1. No projectile after B-03 (before B-03, a white square flies to your own face).
- [ ] R6.8 **The Coin** (manual, Player Two): +1 mana this turn only. Next turn mana refills normally. No projectile after B-03.
- [ ] R6.9 Every spell dropped on the crystal row (CR): invalid after A-02.
- [ ] R6.10 Dropping on a dying minion during its hold: rejected ("already dead"), nothing spent.

### R7 — Spell visuals
- [ ] R7.1 Every targeted cast: a white projectile travels from the card's position to the target.
- [ ] R7.2 Face targets keep swaying during travel.
- [ ] R7.3 A damage spell on a face: the portrait flashes red once, followed by the school burst (Fire / Arcane / Frost).
- [ ] R7.4 A non-lethal damage spell on a minion: **flashes once after A-01** (before A-01, no flash). The burst plays.
- [ ] R7.5 A lethal damage spell on a minion (Fireball, Arcane Bolt, Frostbolt): the minion stays until impact, flashes red once, the burst plays, then it disappears. Survivors don't shift. No TTL warning.
- [ ] R7.6 A non-lethal spell, then immediately play another card: no exception and no duplicate minion view.
- [ ] R7.7 Bursts look distinct: Fire is an upward ember cone, Arcane a purple swirl, Frost blue diamond shards, Nature drifting green.
- [ ] R7.8 Blizzard: a blue sweep crosses the enemy board left to right in about 1.2s, and the frozen tint appears. A Frostbolt afterwards (wait 3s) shows a normal point burst (Constraint 25).
- [ ] R7.9 (A-03) Blizzard, then Frostbolt within 1s: the sweep stays intact after A-03. Before A-03 it collapses. Two point spells back to back both render in place.
- [ ] R7.10 (E-13) Two quick lethal spells on two different minions: both hold and flash, the layout is stable, no warnings. A lethal kill on a full board followed by an immediate summon: a brief squeeze, no errors.

### R8 — Combat
- [ ] R8.1 Click a ready minion: it's highlighted. Click it again: deselected.
- [ ] R8.2 With X selected, click an exhausted own minion Y. Note the behaviour (B-06 decision).
- [ ] R8.3 Attack an enemy minion: both take damage, and the dead are removed. A mutual kill removes both.
- [ ] R8.4 Attack the enemy face: its health drops. Clicking your own face does nothing.
- [ ] R8.5 (E-05) An attacked minion can't be re-selected this turn. Next turn it can attack again.
- [ ] R8.6 A summoning-sick or frozen minion can't be selected. A frozen minion's tint shows.
- [ ] R8.7 Enemy Taunt present: attacking a non-Taunt minion or the face is rejected with the Taunt message and the selection is kept. Attacking the Taunt works.
- [ ] R8.8 A failed attack keeps the selection, and End Turn clears it.

### R9 — Freeze timing (four-turn traces; Constraint 24)
- [ ] R9.1 Frostbolt an enemy minion on your turn N. On the AI's turn N+1 it doesn't attack. Write down when it thaws and when its tint clears (B-04 decision).
- [ ] R9.2 Blizzard with 3+ enemy minions: all frozen and none attack on the AI's next turn. All thaw together, matching R9.1.
- [ ] R9.3 (E-12) Frostbolt your own ready minion before attacking. Write down how long it stays frozen (B-04).
- [ ] R9.4 Re-freeze an already frozen minion: the duration follows the rule; no errors.
- [ ] R9.5 Blizzard an empty enemy board: no errors, mana spent, the sweep plays.

### R10 — Hero Power
- [ ] R10.1 With 2+ mana: costs 2 and deals 1 to the enemy face (log).
- [ ] R10.2 A second use in the same turn does nothing. With under 2 mana it does nothing.
- [ ] R10.3 Next turn it's usable again.
- [ ] R10.4 Manual mode, Player Two's turn: charges and damages relative to Player Two (E-04).

### R11 — Mana
- [ ] R11.1 Crystals: the count equals `MaxMana` and the lit count equals `CurrentMana`. Spending empties crystals left to right.
- [ ] R11.2 Each of your turns: `MaxMana` +1 and a full refill.
- [ ] R11.3 Long game: the cap is 10 crystals, never 11.
- [ ] R11.4 (E-07) Manual mode, Player Two at 10/10 plays The Coin: stays 10/10, and the log says "gained no mana — already at the 10-mana cap".

### R12 — Deck, fatigue, card burn
- [ ] R12.1 The deck count matches the "Deck remaining" log after every draw. Pile layers step down at the ratio thresholds.
- [ ] R12.2 Player One at 0 cards: the pile shows no layers, the count reads 0, and fatigue escalates 1, 2, 3…, hitting health at the start of each turn.
- [ ] R12.3 (E-08 ⚑ or a long game) Player Two (AI) fatigue escalates. Death by fatigue at the start of its turn shows "Player One wins!" and the AI doesn't act.
- [ ] R12.4 (E-02) At 10 cards, the next draw is burned (log). The hand stays at 10 and the deck goes down by 1.

### R13 — AI turns
- [ ] R13.1 The AI plays affordable cards and its minions appear. The log shows the turn start and end.
- [ ] R13.2 With a damage spell and a killable enemy minion, the AI kills the minion instead of going face (log).
- [ ] R13.3 Enemy Taunt: the AI attacks the Taunt first.
- [ ] R13.4 No Taunt: the AI takes favourable trades (it kills and survives), otherwise goes face.
- [ ] R13.5 (E-03) Lethal on board with no Taunt: every attacker goes face.
- [ ] R13.6 (E-14) An AI with an empty hand and no minions ends its turn cleanly.
- [ ] R13.7 (E-14) An AI with only Verdant Growth and no friendly minion skips the card. Blizzard into an empty board: note it (G-09).
- [ ] R13.8 (A-04 ⚑) A lethal spell to the face mid-turn: the AI stops; any remaining cards stay in its hand.
- [ ] R13.9 AI spells and attacks currently have no animation (G-02). Note it; it's not a failure.

### R14 — Manual control mode
- [ ] R14.1 Mode off: the opponent's hand is face-down card backs in a static upward fan with no hover.
- [ ] R14.2 Turn it on, then do any refresh: the cards turn face up and the fan is interactive (lift, spread, mirrored for the top edge). Turn it off: back to card backs.
- [ ] R14.3 (E-04) On Player Two's turn: drag-play from Player Two's hand works with an upright ghost. Player Two's minions can attack. Hero Power and End Turn work for Player Two.
- [ ] R14.4 (E-04) On Player Two's turn, Player One's cards can't be dragged and Player One's minions can't be selected.
- [ ] R14.5 (E-04) On Player One's turn, Player Two's cards can't be dragged and Player Two's minions can't be selected.
- [ ] R14.6 Blizzard cast by Player Two sweeps **Player One's** board.

### R15 — Win, lose, draw, lockout
- [ ] R15.1 Kill the enemy face: "Player One wins!" and Play Again appear. The gem reads 0, not negative, after A-10.
- [ ] R15.2 Lose (the AI kills you): "Player Two wins!"
- [ ] R15.3 After game over: no drag ghost on hand cards, minions can't be selected, and Hero Power and End Turn do nothing.
- [ ] R15.4 ⚑ (optional) Forced double KO through `AfterGameAction`: "Draw!" Not reachable in normal play.

### R16 — Restart
- [ ] R16.1 Play Again: the mulligan shows with an empty board, empty hands, health gems at 30, and no game-over text.
- [ ] R16.2 During that mulligan, hover where the hand was: no errors (A-07). Mulligan cards aren't squashed.
- [ ] R16.3 Four full win → Play Again cycles: no duplicated per-action log lines (no listener stacking).
- [ ] R16.4 A lethal spell to the face, then click Play Again mid-travel: the new game starts cleanly. A stray projectile or flash may linger for under 1s (accepted).

### R17 — Resolution and aspect (E-11)
- [ ] R17.1 Game view at 16:9, 16:10, 21:9 and 32:9: hands, boards, faces, crystals, deck pile and buttons are visible and clickable.
- [ ] R17.2 At each aspect, a lethal spell's projectile and burst land on the target.

### R18 — Build smoke test (E-10)
- [ ] R18.1 Linux player build: R1–R3, R6.1, R7.3, R7.5, R7.7, R7.8 and R15.1 behave as in the Editor. The bursts use the correct (unlit particle) look.

### R19 — Logs
- [ ] R19.1 Console: zero errors, and only expected warnings (for example "invalid zone" is a log, not a warning).
- [ ] R19.2 `Logs/Editor.log` from the noted position: no `Exception`, no `error CS`, no `expired via TTL`.
