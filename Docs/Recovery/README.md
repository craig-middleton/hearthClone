# Original Orgrimmar board recovery — 15 November 2026

The new local Hearthstone installation supplied the original board geometry. This supersedes the plan to move extraction work to Windows. The complete-board image/overlay approach is rejected; the proof scene is built from separate original mesh objects.

## Current result

Open `Assets/Scenes/RecoveredOrgrimmarProof.unity` in Unity 6000.6.9f1. It is intentionally excluded from build scenes and is not connected to the playable match yet.

- Original 121-object hierarchy and local transforms retained.
- 35 distinct native meshes,35 MeshFilters and2 SkinnedMeshRenderers, with original UVs and bone references.
- 10 rebound materials,9 native textures,14 legacy animation clips.
- Native camera position, rotation and 34.870445° verticalFOV recovered from BoardCameras.prefab.
- No Canvas or complete-board background plate. The wood/cloth table is a textured native mesh, as are the field, frame, ground, hero trays and corner props.
- Game MonoBehaviours, audio, particles, colliders and original light components were removed from this visual-only copy. No client scripts/assemblies were imported or executed in HearthClone. Original legacy clips are retained; game-specific interactions are not recreated.

`../Previews/Recovered-ORG-Unlit.png` and `../Previews/Recovered-ORG-Lit.png` show the first material pass and the subsequent depth-shadow pass. Craig judged the geometry proof the best result so far, while stating that it is still short of the real game's appearance. This is a recovery/lighting checkpoint, not completed visual acceptance or game integration.

## Locations

Installed game data:

```text
/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone/Data/Win
```

The directory contains5,063 packed Unity bundles. The asset manifest reports Unity 6000.3.11f1. Original files were read only; all 13 selected source-bundle hashes were checked unchanged after recovery.

Original prefab: `initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d`, original GUID`f1880bb09582d6a429a79214401dfb5c`. Its visual dependencies resolve to 13 bundles totaling62,863,405bytes. `ORG-source-bundles.json` records exact filenames, sizes and hashes; `ORG-object-references.json` records source mesh, material and animation references. FX/audio dependencies were deliberately excluded from this first visual closure.

AssetRipper's larger shared-bundle export is staged outside the game project:

```text
/home/craig-middleton/Documents/Codex/2026-11-12/if-x20/recovery/ORG-native-source
```

The selected 69 visual assets are copied into `Assets/_Project/RecoveredOrgrimmar`. The surrounding shared-bundle export, game bundles and extraction-tool binary are not committed to the game repository.

## Rendering and remaining limits

Compiled original shaders did not recover as usable shader source. `CreateRecoveredBoardProof.cs` binds opaque native RGB textures and genuine netting alpha. This preserves original UVs; atlas backdrop colour keying is not used.

The project uses UniversalRP through its quality settings, with Renderer2D as default index 0. `RefineRecoveredBoardLighting.cs` adds a dedicated 3D UniversalRenderer at index 1 and selects it only on the proof camera. The existing default 2D renderer remains intact. Soft-shadow support is enabled in the shared pipeline asset. No gameplay scene or build-scene configuration changes are made.

`RecoveredBoardPaint.shader` preserves native painted colour and applies a reconstructed, warm depth-shadow treatment. The light uses the recovered light transform, source shadow tint and original shadow-strength value as starting points. This is an approximation of material behaviour, not a claim that the original shaders, highlights, ambient treatment or visual identity have been recovered. Lighting, framing and materials still need comparison against the reference. Live heroes, cards, deck stacks, turn controls and effects still require integration with the new board.

## Reproduce and validate

AssetRipper 2.0.0 Linux x64 was obtained from its [official release](https://github.com/AssetRipper/AssetRipper/releases/tag/2.0.0). The downloaded archive matched published SHA256`6f5d4352d36795b944243eb3ec790ec37dbca6d6756e52605bc8616e042d384e`. The tool lives outside the repo under the workspace's `tooling/AssetRipper-2.0.0` directory.

For the same installed client: load the 13 files in `ORG-source-bundles.json` into AssetRipper with default source version 6000.3.11f1, script content disabled, and bundled assets grouped by bundle name. Export to a new external Unity project. Do not export directly into HearthClone or load/run recovered game scripts.

Run `Tools/PrepareRecoveredOrgrimmar.py EXPORTED_PROJECT_ASSETS_PATH` to copy the sanitized visual dependency closure. It removes nonvisual component documents, verifies referenced visual files, excludes game code/shaders, and records `ORG-import-manifest.json`.

Outside Play mode, run `Tools/CreateRecoveredBoardProof.cs`, followed by `Tools/RefineRecoveredBoardLighting.cs`, through `Tools/editor_api.py`. The first tool creates only the isolated proof scene; the second sets its 3D renderer and material/shadow pass. Do not run these while another scene has unsaved work. `Tools/VerifyRecoveredBoard.cs` checks original hierarchy size, all mesh/UV/index data, skins/bones, 14 clips, active material/texture references, native camera and renderer separation.

`ORG-recovery-validation.json` records the checkpoint evidence. Existing `VerifyFrontEnd.cs` real-input checks and `VerifyStartingDecks.cs` start/restart checks pass with the added renderer. The playable scene and build configuration match the previous commit. No standalone build or completed full-match/native-board integration is claimed.
