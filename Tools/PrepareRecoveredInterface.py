"""Import selected original interface geometry, paint, fonts and clips without client code."""
import argparse
import hashlib
import json
import re
import shutil
from collections import Counter, deque
from pathlib import Path
from PrepareRecoveredOrgrimmar import GUID, visual_prefab

PROJECT = Path(__file__).resolve().parent.parent
DEST = PROJECT / 'Assets/_Project/RecoveredInterface'
DEAD = '0000000deadbeef15deadf00d0000000'
ROOTS = ['Card_Hand_Ally', 'Card_Hand_Ability', 'EndTurnButton',
         'Target_Arrow_Bullseye', 'Target_Arrow_Link']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('export_assets', type=Path)
    source = parser.parse_args().export_assets.resolve()
    index = {}
    for meta in source.rglob('*.meta'):
        asset = meta.with_suffix('')
        if asset.is_file() and (match := GUID.search(meta.read_text())):
            index[match[1]] = asset
    existing = {}
    for meta in (PROJECT / 'Assets').rglob('*.meta'):
        if DEST in meta.parents:
            continue
        if match := GUID.search(meta.read_text()):
            existing[match[1]] = meta.with_suffix('')
    queue = deque(source / ('AssetBundles/essential_base_global-prefab-0.unity3d/' + name + '.prefab') for name in ROOTS)
    for name in ['Belwe', 'BelweNumbers', 'FranklinGothic']:
        queue.append(source / ('AssetBundles/essential_base_global-content-0.unity3d/' + name + '.ttf'))
    # Actor scripts select class paint at runtime; include source class paint explicitly.
    textures = source / 'AssetBundles/essential_base_global-texture-0.unity3d'
    for name in ['Card_Inhand_Ability_Mage']:
        candidate = textures / (name + '.png')
        if candidate.exists(): queue.append(candidate)
    seen, records, reused, missing = set(), [], set(), set()
    removed = Counter()
    while queue:
        asset = queue.popleft()
        if asset in seen: continue
        seen.add(asset)
        meta = asset.with_name(asset.name + '.meta')
        guid = GUID.search(meta.read_text())[1]
        if guid in existing:
            reused.add(str(existing[guid].relative_to(PROJECT)))
            continue
        if asset.suffix not in {'.prefab', '.asset', '.mat', '.anim', '.png', '.jpg', '.tga', '.ttf'}:
            raise ValueError('Unexpected dependency: ' + str(asset))
        target = DEST / asset.relative_to(source)
        target.parent.mkdir(parents=True, exist_ok=True)
        if asset.suffix in {'.prefab', '.asset', '.mat', '.anim'}:
            text = asset.read_text()
            if asset.suffix == '.prefab':
                text, counts = visual_prefab(text)
                removed.update({int(k): v for k, v in counts.items()})
            if asset.suffix == '.anim':
                # Original callbacks target client SoundDefs/behaviours that are excluded.
                text = re.sub(r'(?ms)^  m_Events:.*\Z', '  m_Events: []\n', text)
            for dependency in set(GUID.findall(text)):
                if dependency in index:
                    if index[dependency].suffix != '.shader': queue.append(index[dependency])
                elif dependency != DEAD and not dependency.startswith('0000000000000000'):
                    missing.add(dependency)
            if asset.suffix == '.mat':
                text = re.sub(r'(?m)^  m_Shader:.*$', '  m_Shader: {fileID: 0}', text)
                text = re.sub(r'\{fileID: [^,}]+, guid: ' + DEAD + r', type: \d+\}', '{fileID: 0}', text)
            target.write_text(text)
        else:
            shutil.copyfile(asset, target)
        target.with_name(target.name + '.meta').write_text(re.sub(r'(?m)^  assetBundleName:.*$', '  assetBundleName:', meta.read_text()))
        records.append({'source': str(asset.relative_to(source)), 'destination': str(target.relative_to(PROJECT)),
                        'source_sha256': hashlib.sha256(asset.read_bytes()).hexdigest()})
    if missing: raise ValueError('Unresolved GUIDs: ' + ', '.join(sorted(missing)))
    # CardColorSwitcher's index-zero neutral assignment was verified against the client.
    neutral = Path.home() / 'Desktop/Hearthstone Export/Texture2D/Card_Inhand_Generic.png'
    neutral_target = DEST / 'SourcePaint/Card_Inhand_Generic.png'
    neutral_target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(neutral, neutral_target)
    records.append({'source': str(neutral), 'destination': str(neutral_target.relative_to(PROJECT)),
                    'source_sha256': hashlib.sha256(neutral.read_bytes()).hexdigest()})
    report = {'roots': ROOTS, 'visual_files': len(records), 'removed_component_classes': dict(removed),
              'client_animation_events_removed': True, 'reused_existing_assets': sorted(reused), 'files': records}
    (PROJECT / 'Docs/Recovery/Interface-import-manifest.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'files': len(records), 'types': dict(Counter(Path(x['source']).suffix for x in records)),
                      'reused': len(reused), 'removed': dict(removed)}))


if __name__ == '__main__': main()
