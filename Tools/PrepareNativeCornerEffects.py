"""Extract original Orgrimmar corner particles/colliders without client scripts or geometry.

The template has the source hierarchy so its components can be merged by relative path
into the already imported board, without a second copy of the animated meshes.
"""
from pathlib import Path
import hashlib
import json
import re
import shutil
import yaml

PROJECT = Path(__file__).resolve().parent.parent
RECOVERY = PROJECT.parent.parent / 'recovery'
NARROW = RECOVERY / 'ORG-native-source/ExportedProject/Assets'
SOURCE = RECOVERY / 'Interface-native-source/ExportedProject/Assets'
DESTINATION = PROJECT / 'Assets/_Project/Art/NativeCorners'
PREFAB = SOURCE / 'AssetBundles/initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d/ORG.prefab'
DEAD_GUID = '0000000deadbeef15deadf00d0000000'
ALLOWED = {1, 4, 65, 135, 136, 198, 199}


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def asset_index(folder):
    result = {}
    for meta in sorted(folder.rglob('*.meta')):
        match = re.search(r'^guid: (\w+)', meta.read_text(), re.M)
        if match:
            result[match[1]] = meta.with_suffix('')
    return result


def main():
    DESTINATION.mkdir(parents=True, exist_ok=True)
    raw = PREFAB.read_text()
    narrow_prefab = NARROW / PREFAB.relative_to(SOURCE)
    def comparable(text):
        return {int(identifier): (int(kind), re.sub(r'guid: \w+', 'guid: GUID', body))
                for kind, identifier, body in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)', text, re.S)
                if int(kind) in ALLOWED}
    assert comparable(raw) == comparable(narrow_prefab.read_text()), 'Source component IDs or values differ between extractions'
    blocks = re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)', raw, re.S)
    objects = {
        int(identifier): (int(kind), next(iter(yaml.load(body, Loader=yaml.BaseLoader).values())))
        for _, kind, identifier, body in blocks
    }
    game_objects = {key: data for key, (kind, data) in objects.items() if kind == 1}
    transforms = {key: data for key, (kind, data) in objects.items() if kind == 4}
    game_object_transforms = {int(data['m_GameObject']['fileID']): key for key, data in transforms.items()}

    def source_path(game_object):
        segments = []
        transform = game_object_transforms[game_object]
        while transform in transforms:
            data = transforms[transform]
            segments.append(game_objects[int(data['m_GameObject']['fileID'])]['m_Name'])
            transform = int(data['m_Father']['fileID'])
        return '/'.join(reversed(segments))

    # Do not duplicate the installed chimney. The five generic ORG/FX dust emitters
    # have null source materials and are not referenced by these seven source FSMs.
    def relevant(path):
        return not (path.startswith('ORG/FX/') or path.startswith('ORG/Clickables/Hut/')
                    or path == 'ORG/ORG_Hut/FX_SmokeStack')

    selected_components = set()
    for identifier, (kind, data) in objects.items():
        if kind in ALLOWED - {1, 4}:
            if relevant(source_path(int(data['m_GameObject']['fileID']))):
                selected_components.add(identifier)
    kept_game_objects = set()
    kept_transforms = set()
    for identifier in selected_components:
        game_object = int(objects[identifier][1]['m_GameObject']['fileID'])
        transform = game_object_transforms[game_object]
        while transform in transforms:
            kept_transforms.add(transform)
            data = transforms[transform]
            kept_game_objects.add(int(data['m_GameObject']['fileID']))
            transform = int(data['m_Father']['fileID'])
    kept = selected_components | kept_game_objects | kept_transforms
    output = raw[:raw.find('--- !u!')]
    for header, kind_text, identifier_text, body in blocks:
        kind, identifier = int(kind_text), int(identifier_text)
        if identifier not in kept:
            continue
        if kind == 1:
            body = re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',
                          lambda match: match[0] if int(match[1]) in kept else '', body)
            body = re.sub(r'(?m)^  m_TagString:.*$', '  m_TagString: Untagged', body)
            # Preserve original layer number; MousePick directly tests its collider.
        elif kind == 4:
            body = re.sub(r'(?m)^  - \{fileID: (\d+)\}\n',
                          lambda match: match[0] if int(match[1]) in kept_transforms else '', body)
            body = body.replace('  m_Children:\n  m_Father:', '  m_Children: []\n  m_Father:')
        body = re.sub(r'\{fileID: (-?\d+)\}',
                      lambda match: match[0] if int(match[1]) in kept or int(match[1]) == 0 else '{fileID: 0}', body)
        body = re.sub(r'\{fileID: [^,}]+, guid: ' + DEAD_GUID + r', type: \d+\}', '{fileID: 0}', body)
        output += header + body
    (DESTINATION / 'CornerEffects.prefab').write_text(output)

    # The wider extraction resolves dependencies absent from the narrow board export,
    # e.g. the original Splinter1 texture used by FX_Splinter1.
    source_index = asset_index(SOURCE)
    existing = asset_index(PROJECT / 'Assets')
    queue = [source_index[guid] for guid in set(re.findall(r'guid: (\w+)', output)) if guid in source_index]
    records = []
    seen = set()
    while queue:
        source = queue.pop()
        guid = re.search(r'^guid: (\w+)', Path(str(source) + '.meta').read_text(), re.M)[1]
        if guid in seen:
            continue
        seen.add(guid)
        if guid in existing and DESTINATION not in existing[guid].parents:
            records.append({'guid': guid, 'source': str(source), 'sha256': digest(source),
                            'reused_asset': str(existing[guid].relative_to(PROJECT))})
            continue
        if source.suffix not in ('.mat', '.png', '.asset'):
            raise ValueError(f'Unexpected source dependency: {source}')
        destination = DESTINATION / source.name
        if source.suffix in ('.mat', '.asset'):
            text = source.read_text()
            for dependency in set(re.findall(r'guid: (\w+)', text)):
                if dependency in source_index and source_index[dependency].suffix != '.shader':
                    queue.append(source_index[dependency])
            if source.suffix == '.mat':
                # Installer assigns the disclosed URP compatibility shader after
                # copying all original tint/texture/blending properties.
                text = re.sub(r'(?m)^  m_Shader:.*$', '  m_Shader: {fileID: 0}', text)
            text = re.sub(r'\{fileID: [^,}]+, guid: ' + DEAD_GUID + r', type: \d+\}', '{fileID: 0}', text)
            destination.write_text(text)
        else:
            shutil.copyfile(source, destination)
        shutil.copyfile(str(source) + '.meta', str(destination) + '.meta')
        records.append({'guid': guid, 'source': str(source), 'sha256': digest(source),
                        'asset': str(destination.relative_to(PROJECT))})

    components = []
    for identifier in sorted(selected_components):
        kind, data = objects[identifier]
        game_object = int(data['m_GameObject']['fileID'])
        item = {'file_id': identifier, 'class_id': kind, 'path': source_path(game_object),
                'game_object_id': game_object, 'layer': int(game_objects[game_object]['m_Layer'])}
        if kind == 199:
            item['original_materials'] = data.get('m_Materials', [])
        components.append(item)
    manifest = {
        'source_prefab': str(PREFAB), 'source_sha256': digest(PREFAB),
        'narrow_export_verified': str(narrow_prefab),
        'equivalence': 'All GO/Transform/collider/ParticleSystem/renderer component IDs and values match the narrow ORG export after AssetRipper GUID normalization.',
        'template': 'Assets/_Project/Art/NativeCorners/CornerEffects.prefab',
        'merge': 'Match source paths relative to ORG to the existing native board. Copy colliders, ParticleSystem and ParticleSystemRenderer components; do not duplicate geometry or ancestry transforms.',
        'excluded': ['Existing chimney branch and smoke stack', 'ORG/FX/FX_Dust1..5: unreferenced by corner FSMs, source renderer material unresolved'],
        'compatibility': 'Original material data and textures retained; original compiled shaders are unavailable in the Ubuntu URP renderer. Assign Source Board Particles compatibility shader and source blending values.',
        'transforms': len(kept_transforms), 'particle_systems': sum(item['class_id'] == 198 for item in components),
        'colliders': sum(item['class_id'] in (65, 135, 136) for item in components),
        'components': components, 'dependencies': records,
    }
    (DESTINATION / 'SourceManifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(f"Extracted {manifest['transforms']} transforms, {manifest['particle_systems']} original emitters, {manifest['colliders']} original colliders and {len(records)} dependencies")


if __name__ == '__main__':
    main()
