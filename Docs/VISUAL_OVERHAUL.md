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

Next polish: replace provisional card art as exact mappings are identified, improve individual card-frame crops and targeting/attack animation, and balance the expanded pool.


## Portrait refinement review

The current portrait pass is visual-only and awaits Craig's review. See `Previews/Portrait-Review.png` and the three `Portraits-*.png` captures. The existing Desktop paintings are retained: Garrosh is the classic painting; Jaina is the supplied Death Knight skin. No new hero or mechanics were introduced.

The upper socket uses a ~172×179 canvas-unit pointed mask, and the lower ~180×188, measured against the actual plates. Health badges use the existing painted texture with a spatial teardrop stencil rather than global colour removal. Digits remain unclipped. Targeting and health-value bindings were checked.

After a full base-scene regeneration, run `Tools/RefineHeroPortraits.cs` with `script run` after the other installers. Incremental board-installer reruns preserve an existing reviewed outline.

Visual references: Craig's Screenshot From 2026-11-13 13-25-59.png and [Blizzard's official classic gameplay screenshot/article](https://hearthstone.blizzard.com/en-us/news/11347305). Online imagery is reference only; the game artwork used in this portrait pass comes from the supplied Desktop assets.
