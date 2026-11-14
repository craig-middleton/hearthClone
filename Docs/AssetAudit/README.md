# Complete Desktop asset audit — 14 November 2026

Scanned the three requested folders recursively, without executing their contents or modifying source files. Every regular file was fully read for SHA-256. PNG chunks passed structural verification; WAV headers/durations were parsed; JSON/CSV was parsed and counted; text/scripts were decoded and indexed; cursor files were fingerprinted as binary.

- Files: **144,235**; bytes: **43,437,205,881** (43.44 GB).
- Hearthstone Export: **143,986** files.
- Hearthstone Card Database: **244** files.
- Hearthstone Extraction: **5** files (four PowerShell scripts and an audio-bundle list).
- Images: **71,795 PNG**. Audio: **72,172 WAV**, totaling 71.6 hours.
- **1,792 duplicate files**, 196.3 MB, detected by identical complete SHA-256, not names. Nothing was deleted.
- **Zero read/format-validation errors**.

## Contents and limits

The export includes Texture2D, Sprite, AudioClip, Cursors and startup Texture2D/TextAsset directories. The complete inventory contains no model (.fbx/.obj/.gltf/.glb/.mesh), material, shader, font or Unity asset-bundle files. Original 3D board geometry therefore cannot be recovered directly from this particular export. Native board/frame/pocket textures are present, including ORG_GroundCorners and its variants, ORG_Combined, table surfaces and deck/menu atlases.

The database includes Cards.json (36,390 card records), collectible/all-card CSVs, card tags/tag names, deck lists and 235 raw tables. These describe cards/decks/localization; they do not supply executable server-side effect implementations or a complete verified mapping from every artwork filename to a card. The Extraction scripts parse AssetStudio database dumps, build card/deck exports and export listed audio bundles. They were inspected as text and never run.

The seven playlists identify 292 distinct music files, including 101 in-game board tracks. Ten tracks were randomly selected from those in-game playlists for this implementation; provenance is in ../MUSIC_SELECTION.json. Music copies were encoded as Vorbis for streaming; Desktop WAVs are unchanged.

This is a complete file-level catalogue and structural inspection. It is not a claim that every image has been visually identified or every audio clip listened to. Keyword categories are search aids, not verified semantic labels.

## Searchable outputs

- inventory.csv.gz: every path, size, full hash, type, detailed metadata and error field.
- audio.json.gz: every WAV's channels, sample rate, duration and frames.
- data_files.json.gz: text/script/data structure summaries.
- duplicates.json.gz: every duplicate and its original matching path.
- categories.json.gz: filename-based board, card-frame, hero, mana, deck/menu and effect candidates.
- summary.json: totals, extensions, image sizes and validation results.

From the repo: `python3 Tools/FindDesktopAssets.py org_ --kind image`; add `--limit 100` for more results. To repeat the full snapshot: `python3 Tools/AuditDesktopAssets.py`.
