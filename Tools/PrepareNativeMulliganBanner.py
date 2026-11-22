"""Keep the actual default mulligan banner wrapper, without client scripts/particles.

The standalone BannerMulligan mesh does not carry the wrapper's authored scale,
rotation or text anchors. Dependencies are already part of NativeMulligan.
"""
from pathlib import Path
import hashlib,json,re,yaml

P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets/AssetBundles/essential_base_global-prefab-0.unity3d/Mulligan_Banner_Choose.prefab'
D=P/'Assets/_Project/Art/NativeMulligan/OriginalChooseBanner.prefab'
raw=S.read_text()
blocks=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',raw,re.S)
objects={int(i):(int(c),next(iter(yaml.safe_load(body).values()))) for _,c,i,body in blocks if int(c) in (1,4,23,33)}
gos={i:v for i,(c,v) in objects.items() if c==1}
transforms={i:v for i,(c,v) in objects.items() if c==4}
go_transform={v['m_GameObject']['fileID']:i for i,v in transforms.items()}
root=next(i for i,g in gos.items() if g['m_Name']=='Mulligan_Banner_Choose')
root_t=go_transform[root]
banner=next(i for i,g in gos.items() if g['m_Name']=='BannerMulligan' and transforms[go_transform[i]]['m_Father']['fileID']==root_t)
selected={root,banner}
selected.update(i for i,g in gos.items() if g['m_Name'] in ('HeadlineText','CaptionText') and transforms[go_transform[i]]['m_Father']['fileID']==go_transform[banner])
kept_transforms={go_transform[g] for g in selected}
kept_components=set(kept_transforms)
for g in selected:
    for component in gos[g]['m_Component']:
        key=component['component']['fileID']
        if key in objects and objects[key][0] in (23,33):kept_components.add(key)
out=raw[:raw.find('--- !u!')]
for head,kind,key,body in blocks:
    if int(key) not in selected|kept_components:continue
    if kind=='1':
        body=re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in kept_components else '',body)
        body=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',body)
    elif kind=='4':
        body=re.sub(r'(?m)^  - \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in kept_transforms else '',body)
    out+=head+body
out=out.replace('  m_Children:\n  m_Father:','  m_Children: []\n  m_Father:')
# Do not silently import the entire client when an expected selected dependency is missing.
known={m.group(1) for path in (P/'Assets').rglob('*.meta') if (m:=re.search(r'^guid: (\w+)',path.read_text(errors='replace'),re.M))}
missing=set(re.findall(r'guid: (\w+)',out))-known
if missing:raise RuntimeError('Missing previously imported mulligan dependencies: '+str(missing))
D.write_text(out)
manifest=P/'Docs/Recovery/Mulligan-layout-source.json'
data=json.loads(manifest.read_text())
record={'path':str(S.relative_to(S.parents[2])),'sha256':hashlib.sha256(S.read_bytes()).hexdigest(),'destination':str(D.relative_to(P))}
data['source_files']=[r for r in data['source_files'] if r['path']!=record['path']]+[record]
data['banner_wrapper']={'root_scale':1.75,'mesh_child_scale':1.630027,'mesh_child_rotation_y_degrees':180,'headline':'BannerMulligan/HeadlineText','caption':'BannerMulligan/CaptionText','headline_rgba':[.9019608,.9019608,.9019608,1],'caption_rgba':[.99215686,.81960785,.16078432,1],'outline_rgba':[0,0,0,1],'note':'Preserve wrapper scale/rotation and use its own headline/caption anchors, not the standalone mesh or separate MulliganDetail bone.'}
manifest.write_text(json.dumps(data,indent=2)+'\n')
print('Prepared original default banner wrapper; only four transforms and the default mesh renderer retained.')
