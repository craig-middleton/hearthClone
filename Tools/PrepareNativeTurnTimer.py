"""Recover original TurnTimer geometry, source clips, particles and exact sound bindings.

No original scripts are imported or executed. Source shader references are stripped
for the separate Ubuntu renderer adapter; their original declarations/values remain
in SourceConfiguration.json and copied .mat saved properties.
"""
from pathlib import Path
import hashlib,json,re,shutil,yaml

P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Box-complete-source/ExportedProject/Assets'
D=P/'Assets/_Project/Art/NativeTurnTimer'
B=S/'AssetBundles/essential_base_global-prefab-0.unity3d'
AUDIO=Path.home()/'Desktop/Hearthstone Export/AudioClip'
ALLOWED={1,4,20,23,33,95,108,111,137,198,199}
SUFFIXES={'.prefab','.mat','.png','.asset','.anim','.controller','.tga','.jpg'}

def blocks(path):
 return {int(i):(int(c),next(iter(yaml.load(body,Loader=yaml.BaseLoader).values())))
  for c,i,body in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',path.read_text(),re.S)}

def guid(path):
 return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]

def index(root):
 result={}
 for meta in root.rglob('*.meta'):
  found=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
  if found:result[found[1]]=meta.with_suffix('')
 return result

source=index(S);existing=index(P/'Assets');paths={};records=[];unresolved=[];stripped_script_refs=[];materials=[]
D.mkdir(exist_ok=True);(D/'Source').mkdir(exist_ok=True)

def import_asset(original):
 key=guid(original)
 if key in paths:return paths[key]
 if original.suffix not in SUFFIXES:raise ValueError('Unexpected original dependency '+str(original))
 reused=key in existing
 target=existing[key] if reused else D/'Source'/original.name
 if not reused and target.exists() and Path(str(target)+'.meta').exists() and guid(target)!=key:
  target=D/'Source'/(original.stem+'_'+key[:8]+original.suffix)
 paths[key]=str(target.relative_to(P))
 raw=original.read_bytes();serialized=None
 if original.suffix in {'.prefab','.mat','.asset','.anim','.controller'}:
  serialized=raw.decode()
  if original.suffix=='.prefab':
   parts=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',serialized,re.S)
   keep={int(i) for _,c,i,_ in parts if int(c) in ALLOWED}
   safe=serialized[:serialized.index('--- !u!')]
   for header,kind,object_id,body in parts:
    if int(object_id) not in keep:continue
    if int(kind)==1:
     body=re.sub(r'(?m)^  - component: \{fileID: (-?\d+)\}\n',lambda m:m[0] if int(m[1]) in keep else '',body)
     body=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',body)
    body=re.sub(r'\{fileID: (-?\d+)\}',lambda m:m[0] if int(m[1]) in keep or int(m[1])==0 else '{fileID: 0}',body)
    safe+=header+body
   serialized=safe
  if original.suffix=='.mat':
   source_mat=next(v for c,v in blocks(original).values() if c==21)
   shader_ref=source_mat['m_Shader'];shader_path=source.get(shader_ref.get('guid'))
   shader_name=''
   if shader_path and shader_path.exists():
    match=re.search(r'Shader\s+"([^"]+)"',shader_path.read_text())
    if match:shader_name=match[1]
   materials.append({'sourcePath':str(original.relative_to(S)),'sourceMaterial':source_mat['m_Name'],
    'asset':paths[key],'shaderName':shader_name,'sourceShaderPath':str(shader_path.relative_to(S)) if shader_path else '',
    'sourceSavedProperties':source_mat['m_SavedProperties']})
   serialized=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',serialized)
  for dep in sorted(set(re.findall(r'guid: (\w+)',serialized))):
   if dep in source and source[dep].suffix in SUFFIXES:import_asset(source[dep])
   elif dep not in existing and dep!='0000000000000000f000000000000000' and dep!='0000000000000000e000000000000000':
    reference={'source':str(original.relative_to(S)),'guid':dep}
    if dep in source and source[dep].suffix=='.cs':stripped_script_refs.append(reference)
    else:unresolved.append(reference)
    serialized=re.sub(r'\{fileID: [^,}]+, guid: '+dep+r', type: \d+\}','{fileID: 0}',serialized)
  if not reused:target.write_text(serialized)
 elif not reused:shutil.copyfile(original,target)
 if not reused:shutil.copyfile(str(original)+'.meta',str(target)+'.meta')
 records.append({'source':str(original.relative_to(S)),'asset':paths[key],
  'sourceSha256':hashlib.sha256(raw).hexdigest(),'reused':reused,
  'treatment':'builtin hierarchy/rendering components only' if original.suffix=='.prefab' else 'shader reference removed; saved properties retained' if original.suffix=='.mat' else 'original animation data; original script reference removed where present' if original.suffix=='.anim' else 'original bytes'})
 return paths[key]

prefab=B/'TurnTimer.prefab';objects=blocks(prefab)
gos={str(i):v for i,(c,v) in objects.items() if c==1}
transforms={str(i):v for i,(c,v) in objects.items() if c==4}
go_to_transform={v['m_GameObject']['fileID']:i for i,v in transforms.items()}
def path(go):
 names=[];tid=go_to_transform[go]
 while tid in transforms:
  t=transforms[tid];names.append(gos[t['m_GameObject']['fileID']]['m_Name']);tid=t['m_Father']['fileID']
 return '/'.join(reversed(names[:-1]))

sounds=[]
def copy_sound(sounddef,original,component,name=None):
 key=sounddef.get('m_AudioClip')
 if not key:return None
 filename=key.split(':')[0]
 audio=AUDIO/filename
 if not audio.exists():raise FileNotFoundError('Original audio not found: '+str(audio))
 target=D/filename;shutil.copyfile(audio,target)
 record={'name':name or Path(filename).stem,'sourceObject':path(sounddef['m_GameObject']['fileID']) if original==prefab else '',
  'sourceSoundDef':str(original.relative_to(S)),'sourceComponent':str(component),
  'audioKey':key,'asset':str(target.relative_to(P)),
  'sha256':hashlib.sha256(audio.read_bytes()).hexdigest(),'soundDefSettings':sounddef}
 if not any(s['name']==record['name'] for s in sounds):sounds.append(record)
 return record

for component,(kind,value) in objects.items():
 if kind==114 and value.get('m_AudioClip'):copy_sound(value,prefab,component)
# Exact SoundDef references from original TurnTimer FSM AudioPlaythrough/AudioPlayClip actions.
EXTERNAL_SOUNDS={
 'rollout':'73cbe473817deeb44bbffef6e683ae5c',
 'rollout_secondary':'3dd0cb2d5492b9f4e9e667a5189bab50',
 'expiry_explosion':'96de56233687da14ba85cd62e00f3869',
 'expiry_explosion_secondary':'0ab9303caf48d1548944c391f36c830e',
}
for name,key in EXTERNAL_SOUNDS.items():
 original=source[key]
 candidates=[(i,v) for i,(kind,v) in blocks(original).items() if kind==114 and v.get('m_AudioClip')]
 if len(candidates)!=1:raise ValueError('Ambiguous original SoundDef '+str(original))
 component,sounddef=candidates[0];copy_sound(sounddef,original,component,name)

config=next(v for kind,v in objects.values() if kind==114 and 'm_RopeCapSeconds' in v)
source_prefab=import_asset(prefab)
config_out={
 'sourcePrefab':source_prefab,
 'ropeCapSeconds':float(config['m_RopeCapSeconds']),
 'fuseParameter':config['m_FuseMatValName'],
 'fuseStart':float(config['m_FuseMatValStart']),
 'fuseFinish':float(config['m_FuseMatValFinish']),
 'sparksPath':path(config['m_SparksObject']['fileID']),
 'startBonePath':path(transforms[config['m_SparksStartBone']['fileID']]['m_GameObject']['fileID']),
 'finishBonePath':path(transforms[config['m_SparksFinishBone']['fileID']]['m_GameObject']['fileID']),
 'wickPath':path(config['m_FuseWickObject']['fileID']),
 'shadowPath':path(config['m_FuseShadowObject']['fileID']),
 'sounds':sounds,'materials':materials,
 'sourceTimerSettings':config,
 'sourceFSM':[v['fsm'] for kind,v in objects.values() if kind==114 and 'fsm' in v],
 'unresolvedReferences':unresolved,'strippedOriginalScriptReferences':stripped_script_refs,
 'rolloutFuseKeys':[{k:float(v) for k,v in key.items() if k in ('time','value','inSlope','outSlope','inWeight','outWeight')} for key in blocks(B/'TurnTimer_Wick_mat.anim')[7400000][1]['m_FloatCurves'][0]['curve']['m_Curve']],
 'notes':['No original game scripts imported/executed. Builtin models, bones, particle systems and animation data retained.',
  'Original shader references removed; separate compatibility renderer must use the recorded original shader family and saved properties.',
  'TurnTimer m_DebugTimeout30/m_DebugTimeoutStart20 are debug fields, not gameplay turn durations.',
  'Source timer gameplay mapping, skinning/animations and material burn still require runtime integration and verification.']}
(D/'SourceConfiguration.json').write_text(json.dumps(config_out,indent=2)+'\n')
(D/'SourceManifest.json').write_text(json.dumps({'purpose':'Recovered original TurnTimer assets/audio; compatibility rendering remains separate.','files':records,'sounds':[{k:v for k,v in s.items() if k!='soundDefSettings'} for s in sounds],'unresolvedReferences':unresolved,'strippedOriginalScriptReferences':stripped_script_refs},indent=2)+'\n')
print(json.dumps({'sourcePrefab':source_prefab,'files':len(records),'sounds':[{k:s[k] for k in ['name','asset']} for s in sounds],'unresolved':unresolved},indent=2))
