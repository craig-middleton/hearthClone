# Original targeting arrow investigation

Read-only investigation, 15 November 2026. No Unity scene/project edits.

## Confirmed original assemblies

`essential_base_global-prefab-0.unity3d`, CAB `CAB-69c88b234dfa427e02d1fcee9337ae34` contains:

| Source asset GUID/address | Root | GameObject path ID |
|---|---|---|
| `7afe007e5f455b04b9407307d8df1983` | Target_Arrow_Bullseye | -8531056074620168224 |
| `eb929158148ae954881c5684d27a1aa2` | Target_Arrow_Link | -2832711485509629749 |
| `fcbd8bbbf8c5f4c0589fa9c1927bd018` | TargetReticleManager | -1428210971844022075 |
| `91b47a1196e64e946a974becc0fb29f1` | TargetDamageIndicator | -4278571874702059566 |

Bullseye has head and ring children. Link is a reusable 3D dash template. Their renderers are disabled initially; original gameplay code positions/enables them. Source root transforms have offscreen template coordinates and must not be mistaken for gameplay positions. Bullseye head child rotates 180 degrees around Y and offsets local z by -0.34883952.

TargetReticleManager identifies Assembly-CSharp / TargetReticleManager. The decoded MonoBehaviour has only base fields; no curve/spacing/timing settings are serialized here. No original targeting-arrow animation clip was found in inspected essential prefab/content/mesh bundles. The matching Animation component on Bullseye has an empty clip list. Original dynamic behavior is therefore not recovered by copying visual assets.

## Recovered visual parts already exported

All core parts already exist under `recovery/ORG-native-source/ExportedProject/Assets/AssetBundles`, so no re-export is needed for mesh/material/texture inspection.

Meshes in `essential_base_global-mesh-0.unity3d` (CAB `CAB-66e7137de3b79380e05eb711ae004556`):

| Name | Source path ID | Vertices / indices | Source extents |
|---|---|---|---|
| TargetArrow_ArrowMesh | -1362996082707343006 | 88 / 144 | width 1.01018, z length .73377, y thickness .15223 |
| TargetArrow_DashMesh | 8494104671876798818 | 52 / 96 | width .56246, z length .99102, y thickness .14870 |
| TargetArrow_TargetMesh | 7244398220399860066 | 408 / 1200 | diameter 1.12606, y thickness .08437 |

Head tip points along native -Z. Dash long axis is Z. Meshes have nontrivial thickness/beveled surfaces, original UVs and normals, not flat generic triangles/quads. Ring has actual hole/circular topology.

Runtime material bindings in `essential_base_global-material-0.unity3d` (CAB `CAB-3563ba8b89db13ca6eb196aa111591f1`):

- Head and dash: `TargetingArrow-ArrowDashes`, source path ID 8166759976557517448, texture `ArrowTail_D`. Exported shader is missing and requires compatible reconstruction. `_LightingBlend=0`; source material color is white.
- Ring: `TargetingArrow-ArrowTarget`, source path ID -6068106331289448155, texture `ArrowTargetA_D`. Exported shader is missing; source color white.
- The generic model prefab `Targeting_Arrow_Parts.prefab` uses placeholder `Targeting_Arrow_Parts-lambert1` with no texture. It is useful for geometry only; use the runtime bindings above.

Textures in `essential_base_global-texture-2.unity3d` (CAB `CAB-572cbaceb2612afe464bd92ebbf0736a`):

- `ArrowTail_D`, path ID -7588750597135121321, export GUID 6228fc5cda1edb742961c7d5025795a1, 512x512 RGBA, all alpha255. Upper atlas is head; bottom is rectangular dash.
- `ArrowTargetA_D`, path ID -7582382417671539740, export GUID a850500083067244c8128698499cd31e, 512x512 RGBA, all alpha255. Ring center and exterior are opaque gray atlas background.

Meshes provide atlas clipping. Do not color-key gray or expect alpha masks. Head UV range u .33523..98910, v .42717..97870; dash u .11976..86511, v .03653..43033; ring u/v approximately .0867..9133.

## Consequences for HearthClone

Current TargetingRibbon stretches one native sprite across a continuous Bezier strip and arbitrary triangle, losing source atlas partitioning, the separated solid dashes, 3D thickness, head proportions and hollow bullseye. Native part geometry and its UVs solve these defects. The current cancellation/selection/game-rule API should be retained while replacing its presentation. Actual pointer-to-board curve, link spacing and movement must be reconstructed and visually compared; they are not present as a recovered animation clip.

Inspection records: `/tmp/arrow-source-prefab-objects.json`, `/tmp/arrow-mesh-analysis.json`, `/tmp/essential-prefab-container.json`.
