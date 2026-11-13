# Hearthstone visual scene

Open `Assets/Scenes/HearthstoneTable.unity` in Unity 6000.6.9f1. It is enabled as the first build scene. `TestScene.unity` remains the original baseline.

The 20 presentation card copies in `Assets/_Project/Art/HearthstoneTable/Cards` preserve the original costs, stats, target requirements, effect references, schools, and visual shapes. Descriptions and artwork are presentation changes. The original rule assets have not been rewritten.

## Editing through the running Editor

Keep the project open. Stop Play mode before rebuilding. Run from the project directory:

```sh
python3 Tools/editor_api.py "editor status"
python3 Tools/editor_api.py "script run" '{"file":"Tools/BuildHearthstoneTable.cs","dry-run":true}'
```

To regenerate the scene, remove `dry-run`. **The builder regenerates HearthstoneTable from TestScene and overwrites its generated prefabs/materials/card copies. Commit any manual changes first; do not use it as a routine scene refresh.** It refuses a dirty scene and Play mode. Normal incremental changes should use the Editor API to edit existing objects. The helper reads local authentication only at runtime and sends it only to 127.0.0.1.

## Assets and appearance

Only selected PNGs from Craig's supplied `Desktop/Hearthstone Export/Texture2D` are imported, not the whole export. The builder lists every imported filename and its card mapping. Frame sprites retain their source texture; subrectangle `.asset` sprites and a UI background-key shader expose the relevant atlas regions. Some exported textures use alpha as a material channel rather than opacity; the mana importer deliberately ignores that channel and keys the background.

Noto Serif Bold comes from the installed Noto font package, under the included `NotoSerif-LICENSE.txt`. Its generated TMP atlas is static to keep playtests from changing it.

The body frames are Mage-themed. Most card illustrations are thematic provisional assignments: for example, the custom Shieldbearer uses the turtle illustration `HS11-027`, and the custom River Crocodile uses `HS16-018`. They are not claimed as the exact original Hearthstone card art. Hero portraits are the supplied Jaina and Garrosh illustrations. The table is the supplied Nomi's Kitchen texture, flattened for the current UI scene.

## Verification and remaining work

Verified in the running Editor: original card-rule equality across 20 styled copies, non-null artwork, successful compilation, advancing Play mode, mulligan clicks/confirm, concealed opponent hand, drag event resolution/mana spending, AI turns, and zero errors/warnings in the final smoke pass. Craig also reports a successful manual playtest. Full-match, every-card, and all-resolution regression testing remain outstanding.

Next polish: exact artwork mapping, cleaner atlas edges and spell-frame crops, richer corners/lighting, targeting feedback, damage numbers and spell timing. The requested new spells will be a separate gameplay/VFX checkpoint.
