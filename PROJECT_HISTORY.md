# HearthstoneClone — Project History

> **Why things are the way they are.** For what the code currently does, see `PROJECT_STATUS.md` — that is the file to paste into a new chat. This one is for when you need the reasoning behind a guard, a refactor, or a setup step.

## Session log

Sessions are recorded here as they were documented at the time. Sessions 15–22 and 24–25 were not individually written up.

- **Session 9** — Board visuals working: `MinionView`, `BoardDisplay`, `BoardPanel`; Goblin renders on the board after being played.
- **Session 10** — AI turn logic.
- **Session 11** — Opponent board visualisation.
- **Session 12** — Shuffled decks, draw, opponent hand display.
- **Session 13** — Asymmetric opening hands + The Coin + multi-card AI turn.
- **Session 14** — Full mulligan system, both sides.
- **Session 23 — first Claude Code session, two real bugs found and fixed.** A late win-check that could let a turn transition happen after the AI had already won mid-turn; and a silent no-op when a card effect had no target passed in (`PlayerHand`'s effect-execution line skipped the effect with no log — now logs a warning). Both fixes reviewed and confirmed sound in chat.
- **Session 26 — four real bugs found via Claude Code review, all fixed.**
  1. `OnCardClicked()`/`OnOpponentCardClicked()` never called `CheckWinCondition()` on a successful play — a lethal spell let the game continue instead of ending.
  2. `SetRandomBoardBackground()`/`SetRandomMusic()` were called *after* the `cardPool` empty-check `return`, producing a silent black screen with no audio instead of a visible board with a warning. Both moved to the top of `Start()`.
  3. `endTurnButton`'s click listener was registered *inside* `OnConfirmMulliganClicked()`, and `ConfirmMulliganButton` stayed active and clickable for the entire match — clicking it twice registered the listener twice, silently doubling every subsequent turn. Fixed by registering once in `Start()`, adding a `mulliganComplete` guard, and deactivating the button after use.
  4. `AIController.RemoveDeadMinions()` ran only once, after the entire attack loop — so if the AI's first attacker killed a Taunt minion it stayed in `BoardMinions` for the rest of the loop and subsequent attackers "attacked" the corpse. Fixed by moving the sweep inside the loop, plus an early `break` when the opponent's health hits 0.
  Also corrected in session 26: the assembly dependency graph had been documented as `AI`/`UI` being parallel leaves. In reality `UI` references `AI` directly (`EffectTester` constructs and calls `AIController`), confirmed via both the asmdef reference list and the `using HearthstoneClone.AI;` in `EffectTester.cs`.
- **Session 27** — playtest of session 26's fixes, the Taunt refactor, the guard backfill, and three rounds of review. Full account below.
- **Session 28** — full audit of `PROJECT_STATUS.md` against the source, four code fixes arising from it, and the split into STATUS + HISTORY. Full account below.
- **Session 30** — `MinionView`/`CardView` size parity fix (Live Constraint 14), and the spell-cast animation's travel-effect + target-reaction vertical slice, confirmed via playtest. Full account below.
- **Held-minion hold/layout investigation (2026-08-31)** — the held-view sibling-index conflict: three sequential bugs (leftmost-slot position snap, draw-order-behind-siblings, then a survivor layout-reflow regression) across two fix attempts before the final two-channel fix (pinned sibling index for position, a per-view `overrideSorting` `Canvas` for draw order) landed and was playtest-confirmed. Full account below; final state recorded in STATUS as Live Constraint 23.

---

## Session 27 — the Taunt refactor and guard backfill

### 1. Playtest of session 26's fixes — all four passed
1. **Lethal via spell correctly ends the game.** Console showed `Player One played Arcane Bolt` → `Dealt 3 damage to target. Remaining health: -2` → `*** Player One wins! ***`, all in the same timestamp. Player Two's face displayed `-2 HP`. Additionally confirmed **all input is genuinely locked out post-win** — no buttons respond, so the `gameOver` guards work, not just the log line.
2. **AI correctly re-evaluates targets after killing a Taunt minion** rather than continuing to attack the corpse. This closed the long-standing gap where AI-side Taunt enforcement had never been confirmed in play.
3. **`ConfirmMulliganButton` disappears after one press** and does not linger as an invisible-but-clickable element.
4. **Background/music continued working normally** throughout, with no console errors.

### 2. The Taunt refactor
The rule was previously duplicated across three call sites (`EffectTester.OnMinionClicked`, `EffectTester.OnFaceClicked`, `AIController.TakeTurn`), while `Combat.TryAttack` — the one chokepoint every attack passes through — had no idea Taunt existed. It was moved to live solely inside `Combat.TryAttack`.

An **intermediate version passed a `List<Minion> defenderTaunts` parameter instead**, chosen to keep `Core.Combat` decoupled from `Board`. That was abandoned during review: a `null` list would have silently disabled the Taunt rule entirely — the exact silent-failure mode the refactor existed to eliminate, just relocated. Passing `Board` + the defending `Player` and letting `TryAttack` call `GetTauntMinions` itself closed the hole, and as a bonus restored the owner-named failure message. `Board` lives in `Core` alongside `Combat`, so there was never an assembly obstacle — the original objection was a misreading of the no-singletons preference as "avoid parameters".

*(Session 28 went further and removed the `defender` parameter too — see below.)*

### 3. Null/`IsDead` guard backfill
Argument null guards added to `MinionView.SetMinion`, `CardView.SetCard`, `CardView.SetCardForMulligan`, `FaceView.SetPlayer`, `BoardDisplay.RenderBoard`, `HandDisplay.RenderHand`; plus serialized-field and `GetComponent`-result guards in the two display scripts. `Combat.TryAttack` gained null, malformed-`Target`, and `IsDead` checks. `Minion.CanAttack` gained `!IsDead`.

### Claude Code review of items 2 and 3 — 12 findings
Twelve labelled findings (A1–A5, B1–B3, C1, C2, D, E). Nine needed code changes; **three (B2, C2, E) needed no fix** — two were "correct as-is, worth recording" and one had dissolved on its own.

- **A1 (regression introduced by the refactor itself):** moving the Taunt check into `ResolveAttack` meant a *rejected* attack now cleared `selectedAttacker`, which the old code preserved — so misclicking into a Taunt cost you your selection. Fixed by moving `selectedAttacker = null` inside the success branch only. A real behaviour change the chat workflow made silently and didn't catch; the review did.
- **A2 (the load-bearing one):** only the two *attack* paths called `RemoveDeadMinions()` before refreshing — card plays, hero power, and the AI's card-play loop did not. Unreachable at the time (spells were face-only), but it breaks the moment minion-targeting spells land, which is the very next planned feature. Fixed at the root with `EffectTester.AfterGameAction()` (sweep + refresh + win check) called by every state-changing path, plus `RemoveDeadMinions()` after `AIController`'s card-play loop, plus an `IsDead` skip in `BoardDisplay.RenderBoard` as defence-in-depth. Without this, a spell-killed minion would have rendered as a live `2 / -1` card and stayed clickable.
- **A3:** `FaceView.SetPlayer` was missed by the backfill entirely — it wasn't on the Next Steps list, which named only four view scripts. Fixed.
- **A4:** Taunt was being checked *after* `CanAttack`, reversing the old precedence (an exhausted attacker clicking past a Taunt got "already attacked this turn" instead of the Taunt message), and the message had lost the owner's name. Both restored.
- **A5:** a `Target` with both fields null passed every guard, no-opped on damage, and still set `HasAttackedThisTurn = true` — burning the attack and returning `true`. Now explicitly rejected.
- **B1:** the `List<Minion>` parameter design — resolved by switching to `Board`/`Player`, see above.
- **B2 (no fix needed):** the centralised Taunt check can never fire on the AI path, since the AI's heuristic passes by construction in both branches. Means AI games give the check zero test coverage. Fine — insurance for a future smarter AI.
- **B3 / C1:** guards that early-returned *after* `Instantiate` left blank ghost cards parented in the layout with live prefab-serialized listeners (worse than the NRE they replaced). `ShowMulliganUI` now skips nulls and validates the component *before* wiring. Also resolved the asymmetry where `SetCardForMulligan`'s guard was reachable but `SetCard`'s and `SetMinion`'s were not.
- **C2 (dissolved on its own):** `OnFaceClicked` no longer builds a Taunt list at all.
- **D:** `Minion.CanAttack` now includes `!IsDead`, so the eligibility tint and selection check can't treat a corpse as attackable.
- **E (no fix needed, worth recording):** `List<Minion>.Contains()` reference equality confirmed safe — see the identity lesson below.

### Follow-up pass (same session) — the finding-D remainder
`BoardDisplay` had got the full serialized-field treatment while `HandDisplay` had not, and neither `MinionView` nor `CardView` guarded the text fields they write into. Closed in a second pass:
- **`HandDisplay.RenderHand` brought to parity with `BoardDisplay.RenderBoard`** — guards `handPanel` *before* the child-clearing loop, guards `cardViewPrefab`, destroys an instantiated object whose `GetComponent<CardView>()` comes back null.
- **`MinionView`** — `nameText`/`statsText` were the last unguarded dereferences in the file.
- **`CardView`** — `nameText`/`costText`/`statsText` in `WriteCardText()`, plus `button` in both setters.
- **`AfterGameAction()` now drops a dead `selectedAttacker`.** Direct consequence of the A1 fix: keeping the selection on a *rejected* attack is correct, but combined with `BoardDisplay` skipping `IsDead` minions it meant a selected minion that died would stay selected while being invisible — unclickable, therefore undeselectable, and silently routed into every subsequent attack attempt as a permanent rejection loop.
- **`Combat`** — dropped the `defenderTaunts != null` half of the Taunt condition, dead the moment B1 moved the lookup inside `Combat`.

### Cleanup pass (same session)
- **The serialized-field guards were made to log instead of skipping silently.** The previous pass's guards silently skipped, reproducing exactly the "guard *before* you instantiate" failure mode: a mis-wired prefab rendered a blank card with no console signal at all, strictly worse than the NRE it replaced. Each guard now logs once per bad call, naming the component and the field, with `this` as the log context. Scoped to the *functional* fields; decorative ones (`artworkImage`, `cardBackground`, `minionBackground`) stay silent because being unassigned is valid for them.
- **`Board.GetTauntMinions()` gained an XML doc comment pinning the never-null contract**, since `Combat.TryAttack` calls `.Count` on the result directly.

### Taunt — confirmed working via direct playtesting
- `TestCard_Shieldbearer` (1/5) had its `Has Taunt` checkbox enabled — no new asset needed.
- Both boards correctly render `"Shieldbearer (Taunt)"`.
- **Rejection confirmed**: attacking a non-Taunt enemy minion while a Taunt was in play was correctly blocked, console showed `"Player Two has a Taunt minion — you must attack it first."`, and the attacker stayed selected.
- **Success confirmed**: attacking the Taunt minion directly resolved — `"Shieldbearer attacked."`, health dropped 1/3 → 1/2 from a 1-attack hit.
- **AI-side enforcement confirmed** in a session 27 playtest.

---

## Session 28 — audit, four fixes, and the doc split

A full line-by-line audit of `PROJECT_STATUS.md` against the source, run in both directions (documented→code *and* code→documented, the latter never having been done before).

**Four rows were wrong:**
1. "`GetTauntMinions` is now called from exactly one place" — false. Two call sites: `Combat.TryAttack` and `AIController`'s targeting heuristic. The file contradicted itself, since another row described the AI call.
2. `FaceView` was silently exempt from the logging convention — its `healthText` and `button` guards were bare `if != null` with no `else`, so a mis-wired face gave no console signal at all. It had now been missed **twice**: once by the A3 backfill (which named only four view scripts) and once by the logging cleanup pass.
3. **A `TesCard_Watchman` asset-name typo that never existed.** The file is `TestCard_Watchman.asset`, internal `m_Name: TestCard_Watchman`, and `git log --all --diff-filter=A` shows the string `TesCard` has never appeared anywhere in the repository. The fabricated finding had survived several passes and generated a standing Next Steps item.
4. "10 findings, all fixed in-session" — wrong twice. There were 12 labels, and three (B2, C2, E) needed no code change.

**Four code fixes followed:**
- **`Combat.TryAttack`'s `defender` parameter removed entirely.** The audit found that the "centralised" Taunt rule still took the defending player on trust and never cross-checked that the target actually belonged to them — so a caller could still defeat it by passing the wrong side, exactly the failure the "a rule is only centralised if its inputs are" lesson had been written about. New `Board.GetOwnerOf(Minion)` returns the owning player by reference match (or null), and `TryAttack` derives the defender itself. Two new rejections were added — defender-null and own-side — both currently unreachable through the UI and AI paths, same status as the centralised Taunt check on the AI path. They exist for the targeting and drag-and-drop work.
- **Hero Power fixed for manual control mode.** It was gated to Player One only while card plays, minion attacks and face attacks all honoured the mode. The fix prompt named four things to change; the code actually had **six** hardcoded `playerOne` references — the two extra were the early-return guards on `HasUsedHeroPowerThisTurn` and `CurrentMana`, which would have let Player Two fire the power off Player One's mana pool and cooldown. All six now resolve through the acting player.
- **`FaceView` brought into the logging convention** — `healthText` and `button` now log named warnings with `this` as context; `avatarImage` stays silent as decorative.
- **Log context (`this`) made universal** across all five view scripts and `EffectTester`, so clicking any console warning selects the offending object in the Hierarchy. (`EffectTester`'s fourth `LogWarning`, the combat rejection message, deliberately has no context — it's gameplay feedback, not a wiring diagnostic.)

**A later pass in the same session** audited all 16 `CardData` assets field-by-field and found two more things the docs had never recorded. First, **the asset YAML is not uniform**: `hasTaunt` is physically absent from 14 of 16 files and `targetsSelf` from 4, because Unity only wrote a field once it was explicitly set or the asset re-serialized. That is now Live Constraint 12 — it means grepping the assets for a field name misses most cards, and changing a field's initializer in `CardData.cs` would silently flip every asset that omits it. Second, **`TestCard_Wisp`'s mana cost was documented as 1 and has been 2 in the asset since `d1cd398`** — a fifth wrong row, inherited from the original `PROJECT_STATUS.md`, and the second fabricated card-asset detail after the `TesCard_Watchman` typo. Both were found by reading the assets rather than the docs, which is the point.

**Deliberately not fixed:** the mulligan redraw (needs the mulligan restructured from per-card to batch — a behaviour change needing its own decision), and the off-board-attacker gap in `Combat` (documented as an accepted gap instead).

---

## Session 30 — card size parity, and the spell-cast animation vertical slice

### 1. `MinionView`/`CardView` size parity (Live Constraint 14)
Craig had manually resized `MinionView`'s root and `ArtworkImage` to 160x220 to match `CardView`, so board minions and hand cards render at the same size. After that change, minions *with* artwork (Goblin) still appeared to render larger on the board than minions without (Wisp, Warhorse, Murloc) — same prefab, same `LayoutElement`.

Investigated in the order the task specified: (a) `BoardPanel`'s `HorizontalLayoutGroup` sizes every child from the same fixed `LayoutElement` (160x220 for every instance), so it can't explain a per-card difference on its own; (b) `ArtworkImage` was point-anchored (`AnchorMin == AnchorMax == 0.5,0.5`) with a `SizeDelta` hand-matched to 160x220 — an *absolute* size, not one that tracks the parent; (c) confirmed algebraically that every `MinionView` root resolves to the identical size regardless of artwork, since `LayoutElement.minWidth`/`minHeight` are unset (0) uniformly, so a width shortfall shrinks every card by the same ratio.

The real mechanism: when the board has enough minions to exceed the panel's width, the layout group shrinks every root below 160x220 — but `ArtworkImage`, a grandchild rather than a direct child, is invisible to the layout group and stays fixed at 160x220, overflowing past the shrunk card. Invisible on cards with no artwork (`Image.enabled = false`), visible on Goblin. **The apparent size difference was an illusion** — root sizes were identical the whole time. Fixed by switching `ArtworkImage` to stretch anchors (`0,0`→`1,1`, `SizeDelta 0,0`) so it always exactly fills whatever size the root actually resolves to, compressed or not.

Couldn't verify live in Play mode — Craig's Unity Editor had the project locked, so a second batch-mode instance couldn't open it to instantiate and inspect at runtime. The fix is deterministic anchor math, not something that needed runtime confirmation, but this is worth flagging: a future session hitting the same lock should either ask Craig to close/unfocus the Editor for a batch-mode check, or accept the same static-analysis approach and say so explicitly rather than claim an unverified runtime check.

Also confirmed **`MinionView` has never had a `costText` field** — board minions have never shown mana cost, not a regression from the resize. Asked Craig explicitly rather than adding the field speculatively; answer was leave it as-is (matches real Hearthstone, which doesn't show cost on played minions either).

### 2. Spell-cast animation — planning, then the vertical slice

**Planning pass.** Read `CardEffect.Execute()`, `DealDamageEffect`, `GainManaEffect`, and how `EffectTester.OnCardClicked`/`PlayerHand.PlayCard` resolve a spell play, before proposing anything. Key finding: `PlayerHand.PlayCard` mutates mana/hand/effect all synchronously in one call, and `HandDisplay.RenderHand` (called from `AfterGameAction()` right after) destroys and rebuilds every `CardView` from the post-mutation `Hand` list — same frame. There is currently no persisting visual object to animate mid-sequence; that's the central architectural obstacle for a four-beat animation, not a missing tween library.

Recommended (Option A, accepted): keep `CardEffect.Execute()` synchronous and keep `Effects.asmdef` completely animation-ignorant — state mutates instantly as it does today, and the animation sequence is a purely visual overlay built entirely in `UI.asmdef`. The alternative (Option B — split `PlayCard` into validate/commit phases so damage lands on visual impact, matching real Hearthstone's timing exactly) was flagged as a possible phase-2, not needed to make the sequence look right for a first pass, and it would touch `Cards.asmdef`, which Option A leaves untouched entirely.

Tooling survey: no coroutines, no `ParticleSystem`, no Animator usage anywhere in the project — `FaceView`'s idle sway (`Update()` + sine math) is the only existing "animation," and is the precedent this feature follows. No DOTween/LeanTween in `Packages/manifest.json`. Recommended plain coroutines + `AnimationCurve`, consistent with that precedent, and explicitly recommended against Animator (state-machine overhead for four short procedural beats) and against Timeline (installed but wrong tool — it's for hand-authored sequences, not a system needing to resolve an arbitrary runtime source/target pair).

Build order recommended: travel + impact reaction first (needs zero architecture change — `FaceView` persists across refresh, so both source and target can be handled without touching the destroy/rebuild pattern), then lift/zoom (first piece that actually needs the destroy-timing problem solved), then discard-off last (same fix, plus a new discard-pile anchor that doesn't exist yet). Craig confirmed this plan and asked for the vertical slice built and reported back before anything past it.

**The order-of-operations question, and why it mattered.** Before building, Craig asked to confirm explicitly whether the sequencer captures the `CardView`'s position *before* `PlayCard` runs. Checking `CardView.SetCard`/`HandDisplay.RenderHand` showed the click callback only ever carried `CardData` — `Action<CardData>` — with no way to reach the clicked `CardView` at all. That had to change: `CardView.SetCard`, `HandDisplay.RenderHand`, and both `EffectTester` click handlers now thread `Action<CardData, CardView>` instead, invoked as `onClicked?.Invoke(card, this)` at the moment of the actual click, while the `CardView` is still guaranteed alive.

Inside `OnCardClicked`, the position is captured as a plain `Vector3` — `cardView.transform.position` — *before* `PlayCard()`/`AfterGameAction()` run, specifically because `RefreshHandDisplay()` (called synchronously from `AfterGameAction()` right after) destroys that exact `CardView` in the same frame, before any coroutine would get a second frame to read a live `Transform` off it. A live `Transform` reference carried into the coroutine would not have survived; the `Vector3` snapshot does, because the projectile the sequencer spawns is its own GameObject, entirely decoupled from the `CardView`'s fate. This is now Live Constraint 15.

**What got built.** `SpellAnimationSequencer.cs` — new file, spawns a plain runtime `Image` (no new prefab or particle asset needed) and lerps it from the captured source position to the (live, persistent) target `Transform` over an `AnimationCurve`, re-sampling the target's position every frame since `FaceView`'s idle sway is running concurrently. On arrival it destroys the projectile and, for damage effects, calls a new `FaceView.PlayDamageReaction()`.

`FaceView.PlayDamageReaction()` is deliberately color-flash only, not shake. `FaceView.Update()` already drives `avatarRect.localPosition`/`localScale` every frame for the idle breathe/sway; a reaction that touched position or scale from outside `Update()` would just get overwritten the next frame instead of composing with it. Color is untouched by the existing `Update()`, so it's the safe surface to animate from outside. Shake is a legitimate later addition, but needs `Update()` itself taught to combine a shake offset with the sway base — not something to bolt on from the sequencer without touching `FaceView`'s existing animation loop.

Heal/buff glow (the fourth reaction type in the original ask) isn't wired at all — there's no `CardEffect` subtype to key off yet (`GainManaEffect` is self-targeted mana gain, not a heal). Left as a comment at the call site rather than stubbed.

`EffectTester.ResolveViewTransform(Target)` was written generically against `Target.TargetPlayer`/`TargetMinion` even though the `Minion` branch is unreachable today (spells can only target a face) — per the plan, so the animation's targeting doesn't need rework once minion-targeting for spells lands; it just needs `BoardDisplay` to start tracking a `Minion → MinionView` mapping, which doesn't exist yet.

**Compile verification hit Live Constraint 13 directly.** After creating `SpellAnimationSequencer.cs`, `dotnet build UI.csproj` failed with `CS0246: SpellAnimationSequencer could not be found` — not a code error, but the Unity-generated `.csproj` not yet regenerated to include the new file, because Craig's Editor window wasn't focused and hadn't reimported it. Waited ~100s with no pickup; asked Craig to focus the Editor. Once focused, the `.meta` file appeared within moments and all five assemblies built clean, confirmed both via `dotnet build` and Unity's own `Editor.log` showing no `error CS` lines.

**Confirmed via playtest**: Craig wired the `SpellAnimationSequencer` component (the one manual step — same pattern as every other view reference on `EffectTester`, dragged into the Inspector, no code-side wiring) and played Fireball/Arcane Bolt against the opponent's face. Travel effect and impact flash both fired correctly.

---

## Held-minion hold/layout investigation (2026-08-31) — three bugs, two fix attempts, then the final two-channel fix

Found via the Arcane-lethal spell-burst playtest (Next Steps 18, step 3, spell VFX plan) — the same session that wired `SpellBurstFactory.CreateArcaneBurst` into `SpellAnimationSequencer`. This is the full investigative record behind Live Constraint 23 in STATUS, which now states only the final architecture.

### Bug 1 — held view snapped to the board's leftmost slot

A burst rendered far to the left of its target minion's actual on-screen position. Traced to `BoardDisplay.RenderBoard`: its destroy loop skips a held child (Constraint 22), so the held child keeps its *old* sibling index, but the rebuild loop `Instantiate`s fresh views for every surviving minion as *new* children appended after it — so a minion that died mid-row ends up at sibling index 0 once its still-held view is the only survivor of the old set. `BoardPanel`/`OpponentBoardPanel` both carry a `HorizontalLayoutGroup` (and `MinionView`'s prefab a `LayoutElement`), so sibling index drives horizontal position directly — the layout group snaps the held view to the leftmost slot regardless of where it actually died. `SpellAnimationSequencer`'s position tracking (`lastKnownTargetPosition`, Constraint 16) was never the bug — it re-samples `targetView.position` live and faithfully reports wherever the view *actually* is; the view itself was silently misplaced upstream.

**Not Arcane-specific** — same shared `BoardDisplay`/`MinionView` code path Fire's lethal case also goes through. Re-verified specifically for this fix: Fire's lethal case against a non-leftmost minion showed the identical leftward-snap bug, confirming it went unnoticed in the original Fire playtest (Next Steps 18 step 2) only because that test's target happened to already be in the leftmost slot (sibling index 0 is a no-op for this bug) and/or because Fire's large, chaotic burst visually masked a modest offset that Arcane's tight, deliberate swirl made obvious.

**Fix attempt 1**: `MinionView.BeginHold`/`EndHold` toggled `LayoutElement.ignoreLayout` (`true` on the first hold, `false` back once the hold count returns to zero, including via the TTL-expiry path) — pulling the held view out of the `HorizontalLayoutGroup`'s control entirely, freezing it exactly where it visually sat at the moment it was held, fully decoupled from whatever reflow happened to its rebuilt siblings.

**Rejected alternative, considered at the time**: restoring the held view's original sibling index (`SetSiblingIndex`) instead of excluding it via `ignoreLayout`. Rejected because a `HorizontalLayoutGroup` recalculates *every* child's position on any change to the group's children, not just the moved one, so the held view's pixel position still wasn't guaranteed stable if the surviving siblings' count/spacing changed elsewhere in the row during the hold window; layout exclusion was also considered the semantically correct behavior at the time — a dying minion should die where it stood, not snap to a recalculated layout slot mid-animation. (This reasoning was later superseded — see Bug 3 below, where staying a *counted* participant turned out to be necessary after all, just combined with a pinned rather than free-floating sibling index.)

**Applies wherever else the hold mechanism reaches a layout-group-controlled panel**: `CardView`/`HandDisplay` are the next intended callers of the identical `BeginHold`/`EndHold` pattern (Constraint 15, Next Steps 17, for lift/zoom and discard-off) — confirmed (this session, `TestScene.unity`) that `HandPanel`/`OpponentHandPanel` **both also carry a `HorizontalLayoutGroup`**, so this was not a hypothetical for future work.

### Bug 2 — held view drew behind its rebuilt siblings

Same session, after the `ignoreLayout` fix landed: position was then confirmed correct (Fire and Arcane both re-tested against a mid-row minion), but the held view started rendering *behind* its surviving siblings instead of in front of/alongside them.

**Root cause**: `ignoreLayout` only excludes the held view from the `HorizontalLayoutGroup`'s **position** math — it does nothing to sibling index, and sibling index is what independently controls **draw order** in this project's setup (`MinionView`'s prefab carried no `Canvas`/`overrideSorting` of its own at this point, so it drew purely by sibling position within `GameCanvas`, a single `Screen Space - Overlay` canvas). `RenderBoard`'s destroy loop left the held view at its *old, low* sibling index (skipped, not re-added), while the rebuild loop's `Instantiate`d survivors all landed at *new, higher* indices (`Instantiate` appends as last child) — so the held view silently drew underneath them.

**Fix attempt 2**: `RenderBoard` collected held children during the destroy pass and called `SetAsLastSibling()` on each *after* the rebuild loop finished, every call — not just once at `BeginHold` — since the rebuild loop re-appended survivors with fresh sibling indices on every refresh a hold spanned, so a one-time reorder wouldn't have survived the next refresh. Position (`ignoreLayout`) and draw order (`SetAsLastSibling`) were treated as independent fixes for independent mechanisms that happened to share the same root symptom (a stale sibling index) — fixing one did not fix the other, and both were needed at the time.

Both fixes were playtest-confirmed correct **in isolation** — re-tested against a mid-row exactly-lethal hit for both Fire and Arcane: burst rendered at the correct position, and rendered on top of the surviving siblings, not behind them.

### Bug 3 — surviving minions reflowed during the held sequence

Combining the two fixes above exposed a third bug: `ignoreLayout = true` removed the held view from the `HorizontalLayoutGroup`'s participant count entirely, not just from having its own position overridden — so with one fewer child counted, the *surviving* minions visibly reflowed/shifted into the space that would otherwise still include the held view's slot, instead of staying put during the held sequence.

Investigation showed this wasn't fixable by adjusting `ignoreLayout` further — the group had to keep counting the held view for the survivors' positions to stay put at all. But that created a direct conflict with the standing draw-order fix: `SetAsLastSibling` and "stay a counted participant at the correct *relative* slot" both wanted to control the same single sibling-index value for two unrelated jobs (layout order vs. draw order), and only one could win it at a time.

**Root cause, in hindsight, across all three bugs**: sibling index was being used as the sole channel for two independent concerns (board position and render order) that only coincided by accident in this project's flat `HorizontalLayoutGroup` + single `Screen Space - Overlay` canvas setup — every fix attempt that left this shared-channel assumption in place just moved the collision to a different symptom.

### Final fix — two independent channels, one per concern

- **Position**: `MinionView.BeginHold`/`EndHold` no longer touch `LayoutElement.ignoreLayout` at all — the held view stays a normal, counted `HorizontalLayoutGroup` participant, same as any live minion. `BoardDisplay.RenderBoard` captures each held child's sibling index *before* its destroy/instantiate pass touches the hierarchy, then — after instantiating fresh survivor views (which `Instantiate` always appends as new last children) — rebuilds the full sibling order as a merge: survivors that belonged before the held view's pinned index keep their relative order first, the held view is reinserted at exactly that pinned index, then the remaining survivors follow. This works because `MinionView`'s `LayoutElement` already carries a fixed, content-independent `PreferredWidth`/`PreferredHeight` (160×220) rather than deriving size from live text/artwork, and `BoardPanel`/`OpponentBoardPanel`'s `HorizontalLayoutGroup` has `ChildForceExpand` off with `ChildControlWidth/Height` on — so with a stable participant count, stable per-child size, and the held view restored to its original relative position, the row's total preferred width and every slot's computed position are byte-identical to the pre-death arrangement; survivors don't move at all.
- **Draw order**: sibling index no longer has any draw-order responsibility. `MinionView` now lazily adds (or reuses) a `Canvas` component on itself and toggles `overrideSorting`/`sortingOrder` in `BeginHold`/`EndHold` (mirroring the existing lazy-`GetComponent` pattern the old `LayoutElement` reference used to follow) — this renders the held view on top of its siblings via Unity's canvas-sorting system, completely independent of where it sits in the sibling list. `RenderBoard` no longer calls `SetAsLastSibling()` anywhere.

**Playtest-confirmed**: 3-minion board, exactly-lethal spell on the middle minion — both survivors' `RectTransform.anchoredPosition` logged identical across every `RenderBoard` call spanning the hold (temporary debug logging, added for verification and stripped once confirmed), held minion stayed frozen in its correct middle slot, burst rendered on top, then it disappeared cleanly with no post-death reflow glitch.

**Applies to the future `CardView`/`HandDisplay` hold** (Constraint 15, Next Steps 17): `HandPanel`/`OpponentHandPanel` also carry a `HorizontalLayoutGroup`, so that implementation should use this same two-channel pattern (counted participant + pinned sibling index for position, a local `overrideSorting` `Canvas` for draw order) from the start, not `ignoreLayout`/`SetAsLastSibling`.

Next Steps 18 step 3 (Arcane) is now fully complete — position, draw order, and layout stability are all playtest-confirmed with no open caveats.

---

## Card Artwork pipeline — DONE
Full pipeline built and verified end-to-end:
- **Art source**: Leonardo AI (free tier); JPG downloads are fine, no transparency needed since art fills the card's background area. Aspect guidance: portrait, close to 3:4, e.g. 768×1024.
- **Import**: drag JPG into `Art` → select → Inspector → **Texture Type** → `Sprite (2D and UI)` → Apply.
- **Assignment**: drag the sprite onto the `CardData` asset's pre-existing `Artwork` field (present since the first `CardData.cs` but unused until then).
- **Code**: `CardView` and `MinionView` each gained an `Image artworkImage` field; the shared text-writing helper sets `artworkImage.sprite` when art is present, or `artworkImage.enabled = false` to cleanly hide the slot — so cards without art render as text-only with no broken sprite.
- **`Minion.cs`** gained an `Artwork` field and an optional constructor parameter, since board minions are runtime objects separate from `CardData`. `PlayerHand.PlayCard()` threads `card.artwork` through, mirroring `card.hasTaunt`.
- **Editor setup** (both prefabs): added an `Image` child (`ArtworkImage`) as the *first* child so it renders behind the name/stats text.
- **Bug hit and fixed**: on `MinionView` the `Artwork Image` field was left unassigned (`None`) after adding the child — the same "empty field fails silently" pattern hit several times before. Board minions showed a blank white box (Unity's default unconfigured `Image` colour) until the field was wired.
- **Confirmed via screenshot**: Goblin displays correctly on both hand card and board minion, both sides; cards without art show as plain text with no glitch.

## Board Background & Music pipeline — DONE
Same "list + random pick" shape as `cardPool`:
- **Background art**: same tools as card art but **landscape** (e.g. 1536×1024, 1216×832), prompted for a framed "game board" feel — bordered edges, empty foreground for game pieces, no text. 3 generated. Style: cartoon/painted fantasy, warm saturated colours, whimsical — deliberately genre-level rather than referencing any specific existing game's visual identity.
- **Music**: AI generators — Mubert (ambient specialist), AIVA (structured orchestral/game soundtrack), Sonauto (unrestricted free tier) all viable free options. Prompt: whimsical fantasy tavern instrumental, no vocals, seamless loop, upbeat but not distracting.
- **Scene setup**: `BoardBackground` — a full-screen `Image` child under `GameCanvas`, positioned **first** so it renders behind everything, Rect Transform stretched to fill (`Alt+Shift` + stretch-both preset), **Raycast Target unchecked** proactively. An `Audio Source` added directly onto the `EffectTester` GameObject. **`Loop` is set by the script** (`musicSource.loop = true`), so only `Play On Awake` needs manually unchecking — playback is script-controlled.
- **Bug hit and fixed**: the background wasn't varying between Play-mode restarts. Root cause was Unity's default `Random` seeding, which can produce the same first result on rapid consecutive restarts in the Editor — a known quirk, not a logic bug. Fixed with an explicit `Random.InitState(System.DateTime.Now.Millisecond + System.Environment.TickCount)` at the top of `Start()`, shared by both picks. A debug log showing the picked index/sprite name was added so this is directly verifiable in the console rather than by eyeballing.

## Test Assets
- Original 5: `TestCard_Wisp` (2, 1/1 — **documented as cost 1 until session 28; the asset has said 2 since it was created in `d1cd398`**), `TestCard_Goblin` (2, 2/2), `TestCard_RiverCroc` "River Crocodile" (3, 2/3), `TestCard_Fireball` (4, Spell → `Effect_Deal3Damage`), `TestCard_Boulderfist` (5, 4/4).
- `TestCard_Coin` "The Coin" (0, Spell, `targetsSelf = true` → `Effect_GainMana1`) — kept OUT of `Card Pool`, granted only via `EffectTester`'s dedicated `Coin Card` field.
- `Effect_Deal3Damage` (damage = 3), `Effect_GainMana1` (manaAmount = 1).
- **10 more added in an earlier session**, bringing `Card Pool` to 15 entries × `Copies Per Card = 2` = a real 30-card deck — matching Hearthstone's max-2-copies rule rather than just raising the copy count on the original 5:
  `TestCard_Murloc` (1, 1/1) · `TestCard_Watchman` (2, 3/1) · `TestCard_Shieldbearer` (3, 1/5, **the only Taunt card**) · `TestCard_Warhorse` (3, 3/3) · `TestCard_ArcaneBolt` (3, Spell, `targetsSelf = false` → `Effect_Deal3Damage`) · `TestCard_Bear` (4, 3/6) · `TestCard_MagePupil` (4, Spell, `targetsSelf = true` → `Effect_GainMana1`) · `TestCard_ChargingRhino` (6, 5/5) · `TestCard_StoneGuardian` (6, 4/8) · `TestCard_AncientColossus` (7, 7/7).
- No code changes were needed — `BuildDeck()`/`Copies Per Card` already handled any pool size. Playtested extensively across sessions 26–27 with no pool-related errors.
- Only Goblin has generated art so far.
- ✅ **This list was diffed against the actual `.asset` files in session 28** — every name, mana cost, attack, health, card type, effect asset and `targetsSelf` value above now matches the assets. Two entries were wrong and are fixed: the `TesCard_Watchman` typo (which never existed) and Wisp's mana cost. The other 14 were correct. Re-check after any bulk card edit rather than trusting this line indefinitely.

## Tooling Setup
- Unity Hub + Unity LTS installed via AppImage.
- VS Code with C# Dev Kit + Unity extension.
- .NET SDK installed (for VS Code C# IntelliSense/debugging, separate from Unity's own runtime; install via Microsoft's apt feed, not Ubuntu's default repo).
- Git + GitHub connected via Unity Hub's built-in integration, authenticated via `gh auth login`.
- Git LFS enabled for binary assets (art, audio).

## Tooling workflow — Claude Desktop's Code tab
Craig uses Claude Desktop's **Code** tab (Local environment, pointed at the `hearthClone` project folder) alongside the chat-based workflow. Claude Code reads/edits files directly on disk — no copy-pasting. Both workflows continue to be used; the STATUS file stays the single source of truth regardless of which one makes a change.

**Discipline that keeps this safe**: whoever is driving should read the *actual current* file before editing, never assume memory of its contents is accurate. This has caught real problems (a missing `avatarImage` field, background/music code not actually saved) and is the main safeguard against the two tools drifting out of sync.

**Division of labour (session 27)**: the chat workflow wrote the refactor, then Claude Code reviewed it with a prompt describing exactly what changed and what to check. That caught a behaviour regression the chat workflow had introduced and missed (A1). Worth repeating — write in one tool, review in the other, with the review prompt scoped to the actual diff rather than another full-codebase sweep.

**Compile verification (session 27)**: Claude Code can compile-verify without opening Unity — `dotnet build Core.csproj`, and the same for `Effects`, `Cards`, `AI`, `UI`, works against the Unity-generated `.csproj` files and resolves the engine/TMPro references correctly. This does *not* replace playtesting.

**Truncation (session 27)**: pasting a long status file into a fresh chat can silently truncate its middle section, and the chat may not realise content is missing. One session answered "that card isn't in your pool" about `TestCard_MagePupil`, which *is* documented — it just fell inside the truncated range. This is part of why the docs are now split.

---

## Lessons Learned / Gotchas

### Assembly definitions (.asmdef)
- Circular references are rejected — keep dependency direction one-way.
- An asmdef's real identity is its **Name** field (Inspector), not its filename.
- `CS0246` can mean either a missing asmdef reference OR a missing `using`.
- Cross-assembly type usage needs BOTH the `using` directive AND an explicit **Assembly Definition Reference**.

### Unity Editor / UI basics
- Script filename must exactly match its class name. Scene changes aren't saved until `Ctrl+S`.
- For stretch-anchored panels, reposition via Rect Transform `Pos Y`/`Left`/`Right`/`Height`, not free dragging.
- A circular gizmo appearing unexpectedly in Scene view is likely just the `Global Light 2D` gizmo — editor-only.
- Editing a runtime `(Clone)` GameObject in Play mode is temporary — use Prefab Edit Mode for permanent changes.
- **An empty/unassigned Inspector field for a listener-style hookup fails silently.** This is the single most repeated bug class in this project.
- Editing a TMP label's `Text Input` box requires clicking in, changing text, then clicking away to commit.
- `CS0102` after a manual paste-in usually means a duplicated field/header block — this is why full-file pastes are standard in chat sessions.
- A script field needing a component on the *same* GameObject: drag that GameObject onto its own field, Unity finds the matching component.
- A scene GameObject can accidentally become a prefab instance (showing `Prefab`/`Overrides`/`Select`/`Open` at the top of its Inspector) if it's ever dragged into the Project window. For a one-off bootstrapper never meant to be reused (like `EffectTester`), fix via right-click → **Prefab → Unpack Completely**, then delete the orphaned prefab asset.
- `UnityEngine.Random` can produce the same first result across rapid consecutive Play-mode restarts — a seeding quirk, not a logic bug. Add an explicit `Random.InitState(...)` before the pick.
- A full-screen background `Image` should have `Raycast Target` unchecked **proactively** when created, not fixed reactively after discovering it blocks clicks. Treat as the default habit for any purely-decorative `Image`.

### Code structure / design
- **Why `Minion` reference identity is safe — the full chain.** `List<Minion>.Contains()` resolves to reference equality (`Minion` is a plain class with no `Equals`/`GetHashCode`/`operator==` override), and that is safe here because minions are never copied: `new Minion(...)` appears exactly once in the codebase (`PlayerHand`), `Board.GetTauntMinions` returns a `FindAll` (new list, same element references), `BoardMinions` is only ever `Add`ed to and `RemoveAll`ed from, and the reference survives intact all the way through `RenderBoard` → `SetMinion` → the click closure → `new Target(minion)`. `Board.GetOwnerOf` (session 28) relies on the same property. **`CardData` is the opposite** — `BuildDeck` adds the same asset reference multiple times, so `Hand.Contains(card)`, `Hand.Remove(card)` and the `HashSet<CardData> mulliganSelections` cannot tell duplicates apart.
- `ScriptableObject`-based cards support "multiple copies in a deck" via repeated list references — no asset duplication. **But** see the identity caveat above.
- Use in-place Fisher-Yates for shuffling a `List<T>`.
- A boolean flag on the data itself (`CardData.targetsSelf`) is the right way to encode "who does this affect."
- A temporary/this-turn-only stat bonus needs no explicit expiry if it modifies a field some other system already resets unconditionally each cycle.
- When a UI component needs two distinct interaction modes sharing most setup, pull the shared parts into a private helper and give each mode its own public entry point, rather than a mode-flag parameter on one method.
- A "confirm/complete" boolean guard flag checked at the top of interactive handlers is a simple way to gate a whole phase without a full state machine — appropriate at small scale, worth revisiting as a real state machine if more phases get added.
- When adding a toggleable alternate mode (like `manualControlMode`) to code with implicit assumptions baked in (like "only Player One's hand is ever clickable, so no one checks whose turn it is"), audit those assumptions explicitly — new modes are a good forcing function for surfacing hidden coupling. *(Session 28 found Hero Power was still carrying exactly this bug, a session after the mode was added.)*
- **Deck design**: Hearthstone's real rule is many unique cards each capped at ~2 copies, not few unique cards with many copies. Preserve that shape (add `CardData` assets, don't raise `copiesPerCard`). Because `BuildDeck()` scales to any pool size without code changes, growing the pool is content work, not engineering.
- **Moving a check changes more than the check.** Relocating the Taunt guard from `OnMinionClicked`/`OnFaceClicked` (which `return`ed early) into `ResolveAttack` (which unconditionally cleared `selectedAttacker`) silently changed rejection behaviour. The logic was identical; the surrounding control flow was not. When lifting a guard into another method, check what the *new* location does after the guard.
- **Write in one tool, review in the other.** The chat workflow wrote the session 27 refactor and introduced a regression it didn't notice; Claude Code caught it, along with a missed file and four smaller issues. Scoping the review prompt to *what actually changed* made the findings much more targeted. Keep the "report findings before fixing anything" instruction — it makes the fix order a deliberate decision rather than an automatic one.
- **Documentation written about code the writer cannot read decays silently — audit it against the source periodically, in both directions.** The session 28 audit found four wrong rows, three of them written by a chat session with no filesystem access, plus a fabricated asset-name typo that survived several passes and generated a standing to-do item. It also found six hardcoded references in Hero Power where the fix prompt — written from the documentation — had named four. **A fix list derived from documentation is incomplete by default.** Every earlier pass had only checked documented→code; nobody had ever checked code→documented, which is where the unrecorded behaviour was hiding.

### Git / GitHub / environment
- GitHub requires `gh auth login` or a PAT (repo scope only) for HTTPS git auth.
- `.gitignore` should exclude `Library/`, `Temp/`, `.sln`/`.slnx`, `.csproj`.
- Unity's crash-recovery prompt (`Assets/_Recovery/`) is safe to accept; delete the folder after.
- `grep -rn "TryAttack" --include=*.cs .` from the project root is the fastest way to find every call site of a method before/after changing its signature — used repeatedly in sessions 27 and 28. VS Code's `Ctrl+Shift+F` does the same with clickable results.
