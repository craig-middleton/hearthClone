# Windows asset recovery; Unity development stays on Ubuntu

**Superseded15November2026:** the game is now installed locally on Ubuntu through Proton. OriginalORG models have been recovered directly here. Continue with [Recovery/README.md](Recovery/README.md); no Windows switch is needed. The older workflow below is retained for context.

## Agreed outcome

Craig requires the complete game board to be assembled from individual Unity objects: playing surface, borders, corner scenery, hero trays/sockets, deck recesses and props. A complete-board image with added overlays does not satisfy this. Texture images mapped onto appropriate individual geometry are expected.

The previous playable presentation is retained as a baseline, not accepted as the completed board. The first NativeBoardStudy was rejected visually. The subsequent deck-pocket overlay preview was also rejected and discarded without saving it. Do not repeat either approach or replace the playable scene with the rejected study.

**Windows is only for inspecting/exporting original Hearthstone assets. Do not install Unity on Windows. The existing Unity project remains on Ubuntu.**

## What is available

Craig confirmed the original Hearthstone game files still exist on another Windows PC. Their present path must be verified there. The retained audio extraction script records the earlier location as:

`C:\Program Files (x86)\Hearthstone\Data\Win`

The earlier tool was recorded as `C:\Users\craig\Desktop\assetStudio\AssetStudio.CLI.exe`. That executable's identity/version must be checked before reusing flags. The retained command selects AudioClip assets; it documents the audio export only. No retained script proves which settings were used for the graphics export.

On Ubuntu, the three named Desktop folders have been independently rechecked by contents/signatures: 144,235 files comprising 71,795 PNG, 72,172 WAV, 262 text/data/script/playlist files and six cursors. No identifiable original Unity bundles or model geometry were found; see AssetAudit/container_recheck.json. This does not establish what was originally downloaded or exclude files elsewhere.

The card database preserves original board prefab identifiers in `Hearthstone Card Database/All tables (raw)/BOARD.txt`:

| Board | ID | Prefab reference |
|---|---:|---|
| Orgrimmar | 2 | `ORG.prefab:f1880bb09582d6a429a79214401dfb5c` |
| Stranglethorn | 4 | `STR.prefab:0c5d1122cfb42df4fbf1f01b253437a9` |
| Frozen Throne | 54 | `ICC.prefab:448123f63df359d4186bdb3a9e638d39` |

These are lookup clues, not verified filenames of the current client bundles. The source client's Unity version was recorded as 6000.3.11f1 in the earlier script; verify the actual files rather than forcing that version blindly.

## First Windows task: prove one board export

1. Locate the existing original game data and the extraction tool, if retained. Preserve the originals and their directory structure. Inspect a copied/staging input where practical.
2. Inventory the relevant assets before bulk exporting: Mesh, GameObject, Transform, MeshRenderer/SkinnedMeshRenderer, Material, Texture2D, Animator and AnimationClip. Retain dependencies/shared bundles. A board's geometry and referenced textures may be in different bundles.
3. Look for the Orgrimmar prefab identifier and corresponding objects/meshes. Do not infer the correct graphics bundle from the retained audio bundle list.
4. Inspect the board hierarchy and mesh previews. Establish that meshes are actual scenery/frame geometry with plausible dimensions and UVs. A flat mesh containing a complete-board picture does not meet the requirement.
5. Export one board's assembled model/hierarchy, textures, supported material data and relevant animations to a NEW folder outside the game installation. Keep logs, source bundle paths, object names/IDs, and export settings. Do not export the whole client into the Git repository.
6. Transfer that test export and its provenance to Ubuntu. If model export loses necessary references, retain the original contributing bundles and shared dependencies for investigation.

No game modification, game launch automation, account access, networking implementation or Unity installation on Windows is needed for this task. If a new extraction tool is required, inspect the official project and platform/runtime requirements before installing or running anything.

## Verified tooling and limits

[AssetStudioMod's changelog](https://github.com/aelurum/AssetStudioMod/blob/AssetStudioMod/CHANGELOG.md) documents Unity 6 support. Its [README](https://github.com/aelurum/AssetStudioMod/blob/AssetStudioMod/README.md) distinguishes raw Mesh OBJ export from model FBX export through Scene Hierarchy. That distinction matters: individual OBJ files do not retain the assembled board hierarchy.

The [CLI documentation](https://github.com/aelurum/AssetStudioMod/blob/AssetStudioMod/AssetStudioCLI/ReadMe.md) documents inventory mode (`-m info --load-all`), model export (`-m splitObjects`) and Animator export (`-m animator`). These examples are for AssetStudioModCLI, not necessarily the old AssetStudio.CLI.exe in Craig's extraction script. Verify the executable/version/help first. The model mode's supported filter is by name; do not invent container-filter behavior for that mode.

FBX export can recover transforms, meshes, UVs and some material/texture assignments. It does not automatically recreate arbitrary Unity shaders, scripts, particle systems or runtime behavior; see the project's [ModelConverter implementation](https://github.com/aelurum/AssetStudioMod/blob/AssetStudioMod/AssetStudioUtility/ModelConverter.cs). A successful export is not yet proof that the board looks right. Shader export support is limited for newer Unity versions.

[AssetRipper](https://github.com/AssetRipper/AssetRipper) is a possible fallback for a Unity-format recovery if required; its documentation notes that recovery quality varies. Do not promise a complete original project from either tool.

## Ubuntu acceptance check

Import the recovered test assets into an isolated study scene. Keep HearthstoneTable and its existing mechanics intact until the replacement is visibly better.

- Confirm geometry, transforms, UVs and dependencies survived the transfer.
- Assemble one faithful corner and its adjacent board edge, then inspect it beside the gameplay reference.
- Check perspective, scale, silhouettes, depth/occlusion and material appearance. Avoid global colour removal that damages interior paint.
- Reconstruct the remainder only after that test succeeds. The complete target contains no baked whole-board background plate.
- Keep 30-card inclusive starting totals, current cards/rules, menu/deck selection, targeting and random music behavior.
- Provide brief progress updates, be explicit about unresolved visual gaps, and commit/push significant work directly to main.

If the original board meshes are absent or cannot be exported correctly, report the actual evidence and extraction limitations before another approximation. Manual modeling remains possible, but the rejected procedural reconstruction demonstrated that it cannot be presented as a faithful finished board.
