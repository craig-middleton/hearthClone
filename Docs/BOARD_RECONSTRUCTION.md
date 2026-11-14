# Board reconstruction reference and rejected first study

**Latest direction:** the entire board must be assembled from individual assets/geometry. Further overlays on the complete-board plate were explicitly rejected. Original game files are on another Windows PC; Windows is for extraction only, with Unity remaining on Ubuntu. Continue with WINDOWS_BOARD_RECOVERY.md.

14 November 2026. The playable HearthstoneTable scene remains at the previous visual baseline. Craig rejected the first mesh reconstruction as visually worse; it was restored immediately. NativeBoardStudy.unity is an isolated, disabled-from-build experiment, not a finished replacement. RebuildNativeBoard.cs explicitly refuses to run against the playable scene.

## Reference coverage

Source: [Throneful gameplay reference](https://www.youtube.com/watch?v=ydRABrzLX_U). The review used successive browser screenshots, not a continuous video feed or frame-accurate timing analysis. Selected earlier tutorial frames and the section from approximately 11:48 through 17:40 were inspected, with a later frame around 18:33. The whole 40:43 video has **not** been reviewed. Craig subsequently authorized starting implementation and closing the video.

Observed reference details:

| Approximate time | Directly observed |
|---|---|
| 11:55 | Icecrown board: raised-looking side rails, corner scenery overlapping the field, recessed right-edge decks, both hero-power disks with separate cost gems. |
| 12:45 | Necromancer summons framed by green fractured forms and mist; resulting ghouls settle into separate oval minion frames. |
| 12:55–13:05 | Casting rune over the player hero; yellow star-shaped damage markers; segmented red targeting path ending in a target symbol. |
| 14:12 | Wood, sun and ribbon “Your Turn” presentation over the board. |
| 14:21 | Fireball impact over the opposing hero, with bright burst, embers and dark trail. Precise launch/impact timing was not measured. |
| 14:29 | Taunt shields surrounding oval portraits, with sleep markers above newly summoned minions. |
| 14:55 | Jagged ice surrounding each frozen minion while portraits/stats remain readable. |
| 15:00 | Hero-power targeting uses the segmented red path; potential targets receive outlines. |
| 15:05 | A tilted card leaving the opponent's right-side deck pocket, with a shadow across the field. |
| 15:37 | Flamestrike resolution: individual damage markers across the enemy row, with lingering smoke. The full cast sequence needs replay for timing. |
| 17:05–17:20 | Large fire impact, losing hero portrait breaking into pieces with dust, then a framed victory presentation over a blurred/desaturated board. |

Visual depth, overlapping scenery and recesses can be observed. The screenshots alone do not prove Blizzard's original mesh topology, rendering pipeline, shaders or animation timings.

## Verified native source textures

All are unchanged copies from `/home/craig-middleton/Desktop/Hearthstone Export/Texture2D/`. Provenance hashes are recorded in `NATIVE_BOARD_SOURCES.json`.

- `ORG_Combined.png`: rock/zeppelin, Horde fortress, cannon and catapult painted islands. The four quadrants have different solid backdrops; alpha does not isolate ready-to-use props.
- `ORG_GroundCorners.png`: ground, stone rails, deck-pocket regions and central-surface regions. It is a UV atlas, not a complete assembled board.
- `STW_Table.png`: wood and blue cloth table artwork; its bottom unused area must be excluded.
- `Board_HeroTray.png`: the exact red upper and purple lower tray artwork, including their trim and hero openings.
- `HeroFrame_Silver.png`: pointed portrait border with silver outer stone and gold inner trim.
- `VarianLL_Playspace.png`: a native warm stone playing surface with center seam and shaped edges. This is a candidate, not a verified exact Orgrimmar material assignment.

The complete Desktop audit found no exported original model geometry. That prevents direct mesh recovery from this export; it does **not** prevent constructing custom geometry and using the supplied textures.

## Why the first study failed

The first two captures are `Previews/Native-Board-First.png` and `Previews/Native-Board-Second.png`. They are retained as rejected experiments, not finished visual results.

- Coarse hand-defined contours produced blocky boundaries and badly fitted scenery.
- Reprojecting the ground atlas produced discontinuities; the second playing-surface mapping was visibly stretched and soft.
- Global backdrop colour rejection punched holes through painted interior details. Do not use this shader treatment as a production cutout solution.
- Rectangular foundations, rails and pocket housings exposed the construction rather than integrating it.
- The meshes have some relief depth, but do not reproduce the original prop volumes or their interaction/animation. Capturing those meshes through a RenderTexture is not itself proof of authentic reconstruction.

The native hero-tray identification is useful, but the complete board was not an improvement. Craig's rejection is the acceptance result. `Previews/Board-Restored.png` shows the previous playable presentation after rollback.

Next work should reconstruct and visually validate **one** accurately traced piece at a time in the isolated study, preserving interior paint and using appropriate geometry/UVs. Do not replace the playable theme until the result is visibly better. No new mechanics/cards are part of this work.

Validation after restoration: the playable scene has no NativeBoardView and retains its three original themes. Fresh turn-one totals are P1 26 deck + 4 hand and P2 25 deck + 5 hand; restart, distinct 60 instances and copy-limit checks pass. This is not a full visual acceptance or full-match regression.
