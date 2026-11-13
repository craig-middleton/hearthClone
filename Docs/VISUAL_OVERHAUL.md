# HearthstoneTable — current project guide

Open `Assets/Scenes/HearthstoneTable.unity` in Unity 6000.6.9f1. The original `TestScene.unity` is retained. The styled scene is the enabled build scene. Development and smoke tests use the running Editor through Unity Pipeline.

## Board and live UI

Classic Orgrimmar, Winterveil, and Harvest Tavern share one layout. `RandomBoardTheme` selects a board at every `BeginNewGame`, including Play Again, and avoids an immediate repeat. Portraits, health, mana, cards, deck counts, targeting panels, and the turn button are live Unity UI. The corner props and lighting are baked into each background plate.

The final three plates in `Assets/_Project/Art/Boards` were made with the built-in **image_gen** tool using Craig's screenshot and supplied `ORG_Combined.png` as references, then imported through the Editor. They replace the unpublished raw-atlas prototype that produced seams and broken silhouettes. No characters, cards, deck stacks or counters are baked into these final images. Exact variant prompts are saved in `BOARD_IMAGE_PROMPTS.json`. Source Desktop exports remain untouched.

Other frames, portraits, card art and sound clips come from Craig's supplied assets. Several card-art assignments remain thematic placeholders rather than verified exact canonical art. Noto Serif Bold and its license are included with the generated TMP atlas; the atlas is static to prevent playtests changing it.

## Cards and deck size

The pool contains 26 selectable cards, with at most two copies of each. A shuffled selection supplies each starting deck. Each player's **deck plus hand totals 30 at the start**, including opening draws. Per Craig's requested total, Player Two gets 29 selected cards plus the Coin, rather than a 30-card deck plus a bonus Coin.

On turn one: Player One has 26 in deck and 4 in hand; Player Two has 25 in deck and 5 in hand, including Coin. Mulligan and restart preserve these totals and each card copy has its own `CardInstance`.

New spells:

| Card | Cost | Rule | Presentation |
|---|---:|---|---|
| Flamestrike | 7 | 4 damage to all enemy minions | Fire columns, impact flashes, embers and supplied fire audio |
| Arcane Explosion | 2 | 1 damage to all enemy minions | Expanding violet shockwave, motes and supplied Mage audio |
| Cone of Cold | 4 | 1 damage and Freeze to a minion and its living neighbours | Directional ice fan, shards, frozen frames and supplied Mage audio |

Cone may target either side; the AI uses it offensively. Area spells derive enemy ownership from the caster. The AI retains area damage when the opposing board is empty.

New minions: Ironfur Grizzly (3 mana, 3/3, Taunt), Shielded Guardian (4 mana, 3/5, Taunt), Wolf Rider (3 mana, 3/1, Charge), Stormwind Rider (5 mana, 5/2, Charge). The existing 6-mana 5/5 Charging Rhino now also has Charge. Charge allows the summon-turn attack but still obeys Freeze, Taunt, and the one-attack limit.

## Rules and presentation boundaries

`IAreaDamageEffect.GetAffectedMinions` is a pure snapshot shared by rules and presentation. `PlayerHand.CardPlayed` publishes that snapshot after a successful play, before callers refresh the board, retaining lethal targets for animation. Effects remain in the Effects assembly; Core remains unaware of CardData.

`AreaSpellAnimator` creates a separate camera/render texture per cast, displays school-specific particles and numbers above held lethal views, resolves surviving views after refresh, and releases its temporary objects. It does not delay or change rule resolution. Original single-target spells continue through `SpellAnimationSequencer`.

## Editor tools

Run from the project folder with this same project open:

```sh
python3 Tools/editor_api.py "editor status"
python3 Tools/editor_api.py "script recompile" '{"focus":true}'
python3 Tools/editor_api.py "script recompile_status"
```

The helper uses only 127.0.0.1 and reads the local bearer token from ignored Library/ at request time. It never stores or prints credentials. Play-mode start/stop commands wait for the requested state before returning. Wait for compilation to complete before running installers that need newly added fields.

The base `BuildHearthstoneTable.cs` regenerates the styled scene from TestScene and **overwrites manual scene changes**. Commit first and do not use it as an ordinary refresh. If regeneration is necessary, run the installers afterward in this order:

```sh
python3 Tools/editor_api.py "script run" '{"file":"Tools/InstallMageSpells.cs"}'
python3 Tools/editor_api.py "script run" '{"file":"Tools/InstallKeywordMinions.cs"}'
python3 Tools/editor_api.py "script run" '{"file":"Tools/InstallBoardPlates.cs"}'
```

The installers run outside Play mode. `PreviewMageSpell.cs` is a disposable Play-mode fixture: it replaces the current match's hand/boards to inspect effects and never saves the scene. Stop Play mode afterward to discard it.

## Verification

Reusable checks: `VerifyMageSpells.cs`, `VerifyBoardsAndCharge.cs`, and `VerifyStartingDecks.cs`, executed with `script run`. They cover damage/ownership, adjacency, lethal snapshots, failed casts, AI targets, Charge/Taunt/Freeze, immediate AI Charge attacks, all three board choices, single active board, no immediate repeats, starting counts, unique card instances, copy limits, and restart.

The deck check must run in a fresh Play-mode match. It also restarts the game. Other rule checks construct separate model fixtures. Native-resolution Editor captures are in `Docs/Previews`. These are Editor smoke checks, not a claim of complete match/balance/platform regression or a standalone build.

The current agreed batch is complete and should stop for Craig's playtest. Several thematic art assignments still need verified canonical mappings; no additional balance/content work is included in this batch.


## Portrait refinement

The portrait pass is visual-only. Craig subsequently requested autonomous iteration without per-part review stops. See `Previews/Portrait-Review.png` and the three `Portraits-*.png` captures. The existing Desktop paintings are retained: Garrosh is the classic painting; Jaina is the supplied Death Knight skin. No new hero or mechanics were introduced.

The upper socket uses a ~172×179 canvas-unit pointed mask, and the lower ~180×188, measured against the actual plates. Health badges use the existing painted texture with a spatial teardrop stencil rather than global colour removal. Digits remain unclipped. Targeting and health-value bindings were checked.

After a full base-scene regeneration, run `Tools/RefineHeroPortraits.cs` with `script run` after the other installers. Incremental board-installer reruns preserve an existing reviewed outline.

Visual references: Craig's Screenshot From 2026-11-13 13-25-59.png and [Blizzard's official classic gameplay screenshot/article](https://hearthstone.blizzard.com/en-us/news/11347305). Online imagery is reference only; the game artwork used in this portrait pass comes from the supplied Desktop assets.


## Interface refinement (visual-only)

`Tools/RefineTableInterface.cs` runs after `RefineHeroPortraits.cs` outside Play mode. It updates the current styled scene/prefabs without rebuilding TestScene. It is safe to rerun; source exports remain read-only. Run it last after a full regeneration, because the older base and keyword installers restore their original visual defaults.

- `AtlasContour` cuts opaque exported sprites with normalized mesh outlines. Card frames, name scrolls and attack/health/mana gems retain their painted highlights instead of losing pixels to a global colour key. CardView switches its contour for minion, spell and concealed back. Optional fields leave the original scene compatible.
- Existing neutral creatures use a grey stone frame adapted from the supplied Rogue atlas; the description parchment covers the class emblem. Names are white with a dark outline, descriptions use a clean interior parchment crop, and spell paintings use their own wider shaped window. Costs/stats and drag input retain their original bindings.
- Art sprites crop export borders without changing source pixels. Supplied animation/material alpha is ignored for the full paintings. The bear uses its matching complete HS12-223_S illustration; the horse now retains its full background. Views respect the cropped sprite aspect ratio. Assignments remain thematic where no exact canonical mapping is known.
- The supplied bronze Hero_Power_Frame replaces the Coin_Mana placeholder. A foreground crop of the supplied Mage_power2_D art depicts one fireball; it does not give the upgraded power's damage. The cost gem sits above the disk. `HeroPowerPresentation` now reads each button's owner through the existing refresh. An available power has a light halo; using it reveals the supplied spiral back. The subsequently requested targeting/opponent-power behavior is described below.
- The native ManaTray is sliced from the correct edge of its atlas and rotated into a narrow horizontal well. Full and spent crystals use the same isolated hexagon. `Atlas Paint` supplies brighter blue lighting or a dim desaturated finish. The legacy view has material fallbacks.
- Minions are slightly smaller with layout scaling enabled, leaving more space between rows. Native Taunt/frozen rims exclude unrelated UV scraps. `Atlas Glow` composites the supplied monochrome halos as soft emitted light instead of opaque green paint. Damaged hero health numbers turn red.

Verification in the running Editor: source and both new shaders compile cleanly; all three themes were inspected; `VerifyTableInterface.cs` checks hero/health targeting, opponent-hand concealment, actual power pointer clicks and once-per-turn behavior, its used-state display, and lit/spent mana counts. `PreviewMageSpell.cs` casts Cone of Cold through the real drag/raycast path with correct mana spending. Existing model/keyword/board checks and fresh starting-deck/restart checks pass. Console errors/warnings are zero. No standalone build or full-match regression is claimed.

`PreviewTableInterface.cs` is a disposable Play-mode composition for viewing 4 minions per side, 6 existing cards, damaged portraits and 7/10 mana. It must never be saved as a match. `VerifyTableInterface.cs` runs after it and spends the real power. Stop Play mode to discard both fixtures. Native captures: `Previews/Interface-{Orgrimmar,Winterveil,HarvestTavern}.png`, `Interface-Power-Spent.png` and `Interface-Card-Detail.png`.


## Main menu and deck selection

Craig explicitly added this scope after the visual pass. `Tools/InstallFrontEnd.cs` authors the main menu, collection pages, deck list and board Menu button in the running Editor. It uses only supplied Desktop textures: TheBox_Top, CollectionManagerFrame_Texture, Lettuce_Collections_Page, and the already-imported native tabletop. New art generation/downloads are not used. Run it after the visual installers when regenerating the scene.

Play Game starts a new match. Choose Deck allows choosing exactly 30 cards from the existing pool, with at most two copies each. Click a collection card to add a copy and a deck row to remove one. Save Deck becomes available at 30. Back discards unsaved changes. The choice is saved locally by asset name and validated against the pool/copy limit when loaded and when starting a game. Invalid or absent saved choices fall back to the existing random deck. Coin remains outside the selectable pool, and the opponent's total remains 29 selected plus Coin. No new cards were added.

`VerifyFrontEnd.cs` checks real library/deck-row pointer clicks, incomplete/full deck save behavior, third-copy/31st-card rejection, exact chosen composition in a live game, and a fresh match after returning to Menu. It restores the prior PlayerPrefs key. MainMenu.png and DeckBuilder.png show the native screens. HandPlacement.png confirms the resting hands moved below/above their respective hero portraits; hover lifting remains available.


## Both hero powers and native action effects

Craig added aiming arrows, the missing opponent power and actual hit/spell graphics to the agreed batch, then requested a stop for playtesting after completion. No cards were added. Fireblast now chooses any living hero or board minion (friendly targets allowed, Taunt does not restrict spells). Click the power, then its target; right-click or Esc cancels. Mana is spent only on a valid resolved choice. Garrosh's Armor Up is visible above the table, costs 2 and gives 2 armor. The AI uses it with spare mana after its cards/attacks; manual Player Two can use its own button. The supplied Warrior_power2_D illustration is reused for presentation; it does not grant the upgraded power's armor amount.

Armor is a generic Player stat consumed by TakeDamage before health; overflow reaches health, and existing healing leaves armor intact. Core/HeroPower owns cost, once-per-turn and target validation. GameManager supplies turn/mulligan/game-over eligibility. The targeting UI uses an Input System action for cancel, avoiding frame-poll timing problems. TargetingRibbon draws a curved strip sampled from the native red targeting texture, plus the native cursor; it requires a CanvasRenderer and never receives pointer hits.

PlayerHand.CardResolved publishes the successful card and resolved target before view refresh. Human/AI attack controllers publish successful attack snapshots. BattleEffects observes these events and supplies native fire/frost projectiles, arcane runes, physical hit bursts, floating damage and minion lunges. It does not apply rules. Lethal targets and attackers use independent non-interactive visual snapshots; surviving targets are re-resolved after refresh. A short queue prevents other match actions while these animations complete. Returning to Menu or starting a new match cancels pending input, temporary graphics/holds and effect audio. Existing area stages retain their school choreography, with supplied textures replacing their generated particle textures in the styled scene.

All new textures/audio are copied unchanged from Craig's Desktop export into Art/ActionFX. Examples: ArrowTargetA_D, TargetCursor64, Armor_D, Warrior_power2_D, GenFX_Fireball_1Hot (and Blue), GenFX_Fireball_12, GenFX_ArcaneImpactRunes, GenFX_Slash_01/03, the supplied impact star, and RammingSpeed_Attack_Impact_Sound. Alpha is retained on the cursor/shard where it is actual opacity; RGB light textures use additive composition. Existing card statistics/effects are retained.

After full regeneration, run the authoring tools outside Play mode in this order: BuildHearthstoneTable, InstallMageSpells, InstallKeywordMinions, InstallBoardPlates, RefineHeroPortraits, RefineTableInterface, InstallFrontEnd, InstallBattlePresentation. The last two establish the menu's initial visibility and the final power/animation bindings. If rerunning older visual tools incrementally, activate TableSurface while authoring and reapply the front-end/battle installers last.

VerifyPowersAndBattle covers invalid/off-board targets, armor absorption/overflow, aim/cancel spending, face/minion routing, lethal Fireblast, attack/retaliation snapshots, cleanup, the visible opponent power and AI Armor Up. VerifyFrontEnd additionally reloads the saved JSON choice before starting a real game and restores any prior preference. Fresh 30-card starts/restarts, original Mage rules and Charge/Taunt/Freeze/AI checks pass. Live drag checks cover Fireball, Cone of Cold, Flamestrike and an opponent Arcane Explosion. No standalone build/full balance regression is claimed.

Power-Targeting.png, Native-Fireball.png and Native-Attack-Impact.png are captures from actual resolved/UI-driven actions. PreviewBattlePresentation is a disposable Play-mode fixture that deliberately pauses for a capture; stop Play mode to discard it. The normal saved scene starts at MainMenu. Implementation stops for Craig's playtest after the final commit/push.
