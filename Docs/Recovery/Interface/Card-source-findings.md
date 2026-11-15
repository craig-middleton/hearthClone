# Original card presentation findings — 15 November 2026

Read-only source investigation. No game/Unity project files were changed. Full source decode is /tmp/card-source-hierarchy.json; curated layout, actor bindings, materials and font hashes are in card-layout-source.json.

## Exact sources

- Full minion actor: **Card_Hand_Ally**, original GUID `d00eb0f79080e0749993fe4619e9143d`, CAB-69c88b234dfa427e02d1fcee9337ae34 pathID -2189759849338270441, essential_base_global-prefab-0.unity3d. 126 transforms, 50 mesh renderers/filters, 6 legacy Animation components, 65 game MonoBehaviours.
- Full spell actor: **Card_Hand_Ability**, GUID `3c3f5189f0d0b3745a1c1ca21d41efe0`, same CAB/bundle, pathID -2690728072414629242.
- Geometry-only minion: existing exported `essential_base_global-mesh-0.unity3d/GenFX_Card_InHand_Ally_v3.prefab` (export GUID30bd28f750103e84c83ab81806438e79), nine transforms/eight meshes. Its mostly-empty dummy materials require rebinding.
- Geometry-only spell: existing `.../Card_InHand_Ability.prefab` (export GUIDe13d35c883505d943bb37e0fec35f9fb), nine transforms/eight meshes. Full actor supplies stats/text/real placement.
- `essential_base_global-content-0.unity3d` supplies genuine embedded TTF fonts. See hashes and IDs in JSON. Belwe Bd BT54,324B; FranklinGothicURWConMed154,600B; BelweNumbers4,212B. Exact bytes are staged /tmp/native-*.ttf. Description uses FranklinGothic; names and numbers in actor use Belwe_Outline (same underlying font file as Belwe), with UberText outlines.

Default-actor card core material dependencies use only essential_base_global-material-0.unity3d, essential_base_global-texture-0.unity3d and essential_base_global-texture-2.unity3d, already in original13. Full actor/font export additionally needs essential_base_global-prefab-0 and global-content-0. BelweNumbers scriptable wrapper sits in global-prefab-1.

## Material slots and geometry

Minion RootObject/Mesh: Card_InHand_Ally_Base_mesh, material0=front GenFX_Card_Inhand_Warlock, material1=back GenFX_Card_Back_Default_D. PortraitFrame_mesh: material0=portrait Card_InHand_Ally-Card_InPlay_AllyPortrait; material1=frame GenFX_Card_Inhand_Warlock. These are default editor/class assignments, not an assertion every card uses Warlock. Class-specific frame/portrait textures are assigned dynamically by the original Actor; for a Mage spell substitute source Card_Inhand_Ability_Mage texture into the source frame material.

Minion NameBanner_mesh uses GenFX_Card_InHand_BannerAtlas; Description_mesh uses Card_InHand_BannerAtlasMod2x. Use their original UVs and transforms; do not slice/reassemble a rectangular atlas into a frame. Spell front mesh has front/back at0/1, but spell portrait renderer has **frame at0 and portrait at1**, the reverse of the minion. Actor bindings in JSON identify exact renderer paths.

Both full prefabs use XZ card surfaces with Y as depth. Minion actor’s root source placement is (-15.90749,1.5,-9.9); reset only its root position for a proof, retain child transforms. Name, description and stat anchors have native nontrivial rotations/scales. Name text is rendered onto a separate curved-text mesh via UberText (disabled static renderer is expected until dynamically supplied a text render texture). Preserve mesh UVs. Original scripts must be stripped and their presentation reconstructed with our runtime/TMP code.

**Exact neutral frame resolved:** original CardColorSwitcher (source root GUID `b30c8322821f1524d9e08a59e78e2c85`) defines minionCardTextures[0] as `Card_Inhand_Generic.tif:8ec60e9cd2501ba4aa19980bade1163a`. This source texture is in **essential_base_global-texture-3.unity3d**, pathID4604725608518819955. Desktop `Hearthstone Export/Texture2D/Card_Inhand_Generic.png` also exists; its hash is in the JSON. Use this original generic frame rather than Rogue grey. Mage spell is spellCardTextures[5], source texture GUID `6cf6332645dc19c41b038c238d905f7b`. The previous default-material closure used Warlock; the exact neutral substitution additionally requires texture-3 or the verified Desktop file.

## First normal-card proof configuration

Retain **RootObject/Mesh** and normal CostObject, AttackObject and HealthObject for minion. Retain **RootObject/NonQuestObjects/Mesh** and its CostObject for spell. Enable one appropriate neutral/school description variant; source Actor references identify both. Retain NameBanner and PortraitFrame, map class material and selected artwork into their correct slots. Legacy animation should be disabled for the static proof until clips/states are deliberately selected.

Disable CardStateMgr (contains highlight cameras), GhostCard, MissingCard and all special branches outside normal Mesh/stat roots. These are dynamic game-managed placeholders, not ordinary visible card geometry. Disable EvolutionVFX/BGRewardVFX, RuneBanner, faction/multiclass/special ribbons, speed wing, trinket/quest branches. For an ordinary nonlegendary card disable the legendary dragon, rarity gem/frame when no rarity is represented, and special dual-tribe plates. Enable single RacePlate only if we supply real tribe text. Several source default flags assume Actor performs game-specific toggles; blindly preserving active flags does not produce a normal card.

Fonts, names, descriptions, digits and card class/art bindings are therefore as important as mesh recovery. A native mesh proof with dummy textures and missing curved text would not yet demonstrate final card fidelity.
