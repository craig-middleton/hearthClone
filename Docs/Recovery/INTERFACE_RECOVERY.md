# Original card, targeting and End Turn sources — 15 November 2026

The source investigation and first native-mesh proof are complete. **Targeting and animated End Turn are now connected to the live game; native cards are now integrated through the TableCard UI prefab.** HearthstoneTable remains the playable native Orgrimmar checkpoint from175a69a. Craig requested stopping at effort-setting changes: the next visual refinement/connection pass should begin after he switches to High and says continue. Reassess Ultra if shader/recovery problems require deeper work.

Open Assets/Scenes/RecoveredInterfaceProof.unity. The left two objects use full original minion/spell actors with existing Wolf Rider/Fireball data and artwork. The right shows the original arrow meshes and End Turn button. Play the proof to cycle original button clips. The proof is excluded from builds and contains no game bootstrapper. Docs/Previews/Recovered-Interface-Proof.png records the first refined render.

## Exact recovered components

- Card_Hand_Ally and Card_Hand_Ability from essential_base_global-prefab-0.unity3d: original separate frame, portrait, description, curved name, gem and stat-anchor geometry. Normal branches are deliberately selected; source Actor-managed ghosts, missing-card placeholders, rarity dragons, quests, ribbons and cameras are excluded from the visible proof. Source transforms below the root remain intact.
- Original CardColorSwitcher table confirms neutral minion texture index0 is Card_Inhand_Generic (GUID8ec60e9cd2501ba4aa19980bade1163a, installed texture3; matching Desktop export supplies the imported PNG). Mage spell texture index5 is Card_Inhand_Ability_Mage. The previous grey Rogue approximation is unnecessary. Minion portrait/frame slots are0/1; spell slots are1/0. See Interface/Card-layout-source.json.
- Genuine embedded Belwe, BelweNumbers and FranklinGothic TTFs from essential_base_global-content-0.unity3d. The proof creates static ASCII TMP font assets from Belwe and Franklin. Names are baked from Belwe into transparent text-only PNGs on original curved-name meshes. Frame/art geometry is not flattened into a complete card image. Descriptions use Franklin.
- Target_Arrow_Bullseye and Target_Arrow_Link supply original arrowhead, separate dash and hollow ring meshes. Original UVs exclude opaque grey atlas surroundings through topology. Head/dashes use ArrowTail_D; ring uses ArrowTargetA_D. The seven-dash proof curve is reconstructed behavior; original reticle movement/spacing code was not imported or recovered as a usable animation.
- Full EndTurnButton has18 transforms and19 ENDTURN clips. The bare Button_EndTurn mesh prefab found earlier lacked its controller/animations. Source clips provide real 3D flip, press/release, hover and atlas states. The proof reconciles the historical Highlight_Plane animation binding with GreenHighlightPlane in its instance. Original native labels use Belwe.

## Source and import

Interface-source-bundles.json records17 selected installed bundles and SHA256s. Full staged AssetRipper export is outside the repo at ../../recovery/Interface-native-source/ExportedProject relative to repo root. It extends the previous13 with essential_base_global-prefab-0, prefab-1, content-0 and texture-1. No source installation/Desktop files were modified. Original compiled shader source is still unavailable.

Tools/PrepareRecoveredInterface.py imports184 selected visual assets under Assets/_Project/RecoveredInterface: five prefabs,50 meshes,50 materials,45 PNGs,31 legacy clips and three TTFs. It strips143 source MonoBehaviours, cameras, particles, colliders and audio components, clears original AnimationEvents/SoundDef callbacks, and resolves visual GUIDs. Raw source snapshots/hashes are in Interface-import-manifest.json. No original C#/assemblies are included. The entire larger export is not imported.

Tools/CreateRecoveredCardProof.cs creates only the isolated proof, builds persisted native-font TMP assets, binds source textures to proof material copies, selects normal card parts and reconstructs typography at native anchors. Load assets after NewScene because Unity can unload otherwise-unreferenced ScriptableObjects during scene replacement. Release temporary text-capture cameras before releasing their render targets.

Tools/AddRecoveredControlsProof.cs appends source targeting geometry and End Turn. NativeInterfaceProofMotion is our small proof-only animation driver; it has no gameplay role. RecoveredInterfaceGlow.shader is a reconstructed additive highlight study using native mask/flare textures and parameters. Original client shader appearance, scrolling gem reflections, exact text metrics, hovering/fanning/dragging and target movement remain to be refined/connected. Source button sounds are excluded. No pixel-identical appearance is claimed.

## Validation and continuation

VerifyRecoveredInterfaceProof ran through the live Editor API in Play mode: five sanitized assemblies contain no client scripts/cameras/audio; all31 clips have empty client-event lists; active card mesh materials/textures resolve; source curved-name meshes and Franklin text are present; native flip exceeds140 degrees, press lowers more than0.1 units, green atlas/highlight state appears, both turn phases are reached; nine original targeting mesh instances are present. Current console is clear. This is visual-proof validation, not live input integration.

The working game scene, card data, decks, rules, random music and build-scene list remain unchanged. Next: refine source-material behavior and proportions, connect the recovered card presentation to existing CardView/fan/drag events, replace the actual TargetingRibbon presentation while retaining its targeting/cancellation rules, and bind the native End Turn assembly/clips to existing turn/input state. Preserve30-card inclusive starts and all existing regression checks.


## Live controls checkpoint

Tools/InstallNativeMatchControls.cs is idempotent and saves the playable scene. Source arrow meshes are projected into UI Graphics with original UVs and top-facing triangles; all parts are non-raycastable. End Turn is actual world geometry, aligned to its existing transparent Button; NativeTurnButtonPresentation maps accepted turn changes and pointer events to source clips. AI remains synchronous, with the flip returning after its existing visual queue. The green state is conservative: affordable target-dependent cards can keep the button yellow even when no target exists. No turn/input rules change. Four live regression tools pass; card rendering integration remains pending.


## Live cards update — 16 November

NativeCardFace/NativeCardLayer now connect original card shape/UV data and source typography to hands, mulligan, collection and drag ghosts. See NATIVE_CARD_INTEGRATION.md for exact design, assets, tests and remaining limits. This supersedes the earlier pending-card notes above; the old isolated proof remains a source comparison scene.
