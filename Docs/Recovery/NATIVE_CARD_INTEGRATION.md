# Native card UI integration — 16 November 2026

The recovered cards are now live through Assets/_Project/Prefabs/HearthstoneTable/TableCard.prefab. CardData, game rules, starting totals and build settings are unchanged. The existing hand layout, hover events, collection buttons, mulligan callbacks and drag/drop system still own interaction.

## Rendering

Tools/InstallNativeCards.cs reads the selected native minion/spell actors in RecoveredInterfaceProof. It transforms each native mesh through its entire actor hierarchy, selects upward-facing triangles via geometric cross-product Y>0, preserves original UVs and texture scale/offset, and sorts source part/triangle depth for UI compositing. Both templates share the source rectangle X[-1.14,1.03], Z[-1.47,1.552], fitted into the original180x270 CardView. Geometry, frame, portrait, description, banner and gems remain separate maskable layers. Depth is projected into the existing overlay UI; this is not the original client's full3D lighting/reflection renderer.

NativeCardPaint.shader ignores source paint alpha because the gem shader stores scrolling/reflection masks there. It preserves UI tint/alpha, stencil and rectangular clipping. No colour keying or whole-card background bitmap is used. Original Card_Inhand_Generic neutral paint, Mage spell paint and source UV silhouettes replace the earlier sliced/reassembled card layout.

NativeCardLibrary stores source-derived vertex/UV/index recipes. Portrait texture and crop follow each CardData sprite. Card names are baked from native Belwe onto original curved-name meshes by editor-only NativeCardTextBaker. Franklin body text and Belwe numbers are placed at source-derived anchors. Names are prepared for47 existing CardData assets; the active game pool remains26. Unknown new names fall back to flat Belwe text until the installer is rerun. There is no per-card runtime camera, RenderTexture or owned material/texture lifecycle. Ghosts clone serialized visuals and keep shared immutable assets.

NativeCardFace binds at the end of SetCard/SetCardForMulligan and mirrors selection tint. Old CardView text values are retained, but their Graphics are hidden for native fronts. The root remains a transparent raycast target. All native Graphics are non-raycastable and maskable; no nested Canvas is introduced. Face-down cards hide all front layers and retain the existing classic back presentation. The original TestScene/non-styled prefabs keep their fallback behavior.

## Rebuild

With Unity stopped and the active scene clean, run Tools/InstallNativeCards.cs through Tools/editor_api.py. It reads the isolated proof, saves the shared card prefab/library/name textures, and opens HearthstoneTable. It does not save or alter the source proof/game rules. All17 native mesh layers must have serialized CanvasRenderer components. Wait for source/script recompile/domain reload before running authoring or Play commands.

## Validation

- VerifyNativeCards: visible values/curved names, artwork/type rebinding, complete concealment, selection tint, real mulligan clicks and per-copy identity, first-hit raycasts, hover, upright nonblocking ghost cloning/cleanup, collection clicks, maskability, manual reveal/reconceal.
- VerifyFrontEnd: real collection clicks, limits, exact saved30-card composition and a fresh match.
- VerifyStartingDecks:60 distinct instances,30 inclusive cards each side at turn one and restart.
- VerifyTableInterface and VerifyPowersAndBattle: native UI integration preserves hero targets, mana, power costs/once-per-turn behavior, lethal spell/attack cleanup and AI Armor Up.
- PreviewMageSpell: actual Flamestrike drag spends7 mana and resolves affected enemy minions.
- VerifyNativeMatchControls: original targeting and animated End Turn still pass.

Inspected previews: Native-Cards-First-Live.png, Native-Cards-Hover.png and Native-Cards-Collection.png. No standalone build or complete-match simulation is claimed.

## Remaining visual work

Core integration is complete. Pause for Craig to switch to High before routine visual tuning. Hover clearance near the screen bottom, title/body spacing, material richness/reflections and a fully school-less spell-description variant remain candidates for refinement against the reference. In-play minion frames, board lighting and other existing effects are outside this card-face integration pass. No pixel-identical appearance is claimed.


## Layout refinement and next animation step

Titles now use more of their original curved-name mesh height. Spell body text reserves a distinct school-label strip. Hover lift is230 for the player and340 for the mirrored manual opponent hand; VerifyNativeCardLayout checks first/middle/last full card rectangles in ten-card hands against screen bounds and verifies separate text boxes. NativeCard/NativeMatchControls checks still pass. A new-game ready-pose reset prevents the End Turn mesh remaining frozen midway through a previous native flip.

Craig explicitly requires original game animations for the added card motions. The reconstructed gem-shimmer experiment was removed. No authored draw/pickup/play tweens were added. Next, on Ultra, inspect recovered AllyInHandBirth/BirthFast/ScaleDown/FlipOver and related source clips, choose appropriate original motion and retarget needed channels while preserving source curves/timing. Existing projected UI does not automatically support every original3D/material channel; do not promise a clip works until tested.
