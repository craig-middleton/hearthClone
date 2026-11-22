"""Recover the exact Fireball/Frostbolt visual phases and SoundDef clip bindings."""
from pathlib import Path
import re,yaml,json,hashlib,shutil
P=Path(__file__).resolve().parent.parent;R=P.parent.parent/'recovery';S=R/'Interface-native-source/ExportedProject/Assets';D=P/'Assets/_Project/Art/NativeSpells';D.mkdir(exist_ok=True)
B=S/'AssetBundles/essential_base_global-prefab-0.unity3d'
def blocks(p):return {int(i):(int(c),next(iter(yaml.load(b,Loader=yaml.BaseLoader).values()))) for c,i,b in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',p.read_text(),re.S)}
def index(folder):
 out={}
 for m in folder.rglob('*.meta'):
  match=re.search(r'^guid: (\w+)',m.read_text(),re.M)
  if match:out[match[1]]=m.with_suffix('')
 return out
idx=index(S);existing=index(P/'Assets');allowed={1,4,20,23,33,95,108,111,137,198,199};records=[];unresolved=[];seen=set();paths={}
def import_asset(f):
 guid=re.search(r'^guid: (\w+)',Path(str(f)+'.meta').read_text(),re.M)[1]
 if guid in paths:return paths[guid]
 if f.suffix not in ('.prefab','.mat','.png','.asset','.anim','.controller'):raise ValueError(f)
 reused=guid in existing
 target=existing[guid] if reused else D/f.name;paths[guid]=str(target.relative_to(P));seen.add(f)
 if f.suffix in ('.prefab','.mat','.asset','.anim','.controller'):
  text=f.read_text()
  if f.suffix=='.prefab':
   parts=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',text,re.S);keep={int(i) for h,c,i,b in parts if int(c) in allowed};out=text[:text.find('--- !u!')]
   for h,c,i,b in parts:
    if int(i) not in keep:continue
    if int(c)==1:
     b=re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keep else '',b);b=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',b)
    b=re.sub(r'\{fileID: (-?\d+)\}',lambda m:m[0] if int(m[1]) in keep or int(m[1])==0 else '{fileID: 0}',b);out+=h+b
   text=out
  if f.suffix=='.mat':text=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',text)
  for dep in set(re.findall(r'guid: (\w+)',text)):
   if dep in idx and idx[dep].suffix not in ('.shader','.cs'):import_asset(idx[dep])
   elif dep=='0000000deadbeef15deadf00d0000000':unresolved.append(str(f.relative_to(S)))
  text=re.sub(r'\{fileID: [^,}]+, guid: 0000000deadbeef15deadf00d0000000, type: \d+\}','{fileID: 0}',text)
  if not reused:target.write_text(text)
 elif not reused:shutil.copyfile(f,target)
 if not reused:shutil.copyfile(str(f)+'.meta',str(target)+'.meta')
 records.append({'source':str(f.relative_to(S)),'asset':str(target.relative_to(P)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'reused':reused});return paths[guid]
def sound_clip(ref,source):
 f=idx[ref['guid']] if ref.get('guid') in idx else source;bs=blocks(f);sd=bs[int(ref['fileID'])][1];key=sd['m_AudioClip'];assert key and sd['m_RandomPitchMin']==sd['m_RandomPitchMax']=='1';assert sd['m_RandomVolumeMin']==sd['m_RandomVolumeMax']=='1';assert not sd['m_RandomClips']
 sourceclip=Path.home()/'Desktop/Hearthstone Export/AudioClip'/key.split(':')[0];target=D/sourceclip.name;shutil.copyfile(sourceclip,target)
 records.append({'sounddefSource':str(f.relative_to(S)),'sounddefComponent':ref['fileID'],'audioKey':key,'asset':str(target.relative_to(P)),'sha256':hashlib.sha256(sourceclip.read_bytes()).hexdigest()});return str(target.relative_to(P))
def phase(name,kind):
 f=B/(name+'.prefab');bs=blocks(f);gos={str(i):v for i,(c,v) in bs.items() if c==1};transforms={str(i):v for i,(c,v) in bs.items() if c==4};goTransforms={v['m_GameObject']['fileID']:i for i,v in transforms.items()}
 def path(go):
  names=[];tid=goTransforms[go]
  while tid in transforms:
   t=transforms[tid];names.append(gos[t['m_GameObject']['fileID']]['m_Name']);tid=t['m_Father']['fileID']
  return '/'.join(reversed(names[:-1]))
 def vector(v):return [float(v[k]) for k in 'xyz']
 def mul(a,b):
  x,y,z,w=a;X,Y,Z,W=b
  return [w*X+x*W+y*Z-z*Y,w*Y-x*Z+y*W+z*X,w*Z+x*Y-y*X+z*W,w*W-x*X-y*Y-z*Z]
 def rotate(q,v):return mul(mul(q,v+[0]),[-q[0],-q[1],-q[2],q[3]])[:3]
 def pose(tid):
  t=transforms[tid];parent=t['m_Father']['fileID']
  if parent not in transforms:return [0,0,0],[0,0,0,1],[1,1,1]
  pos,rot,scale=pose(parent);offset=rotate(rot,[float(t['m_LocalPosition'][k])*scale[i] for i,k in enumerate('xyz')]);q=[float(t['m_LocalRotation'][k]) for k in 'xyzw']
  return [pos[i]+offset[i] for i in range(3)],mul(rot,q),[scale[i]*float(t['m_LocalScale'][k]) for i,k in enumerate('xyz')]
 rotators=[];audio=None;capture=None
 for i,(c,v) in bs.items():
  if c!=114:continue
  if 'RotateSpeedX' in v:
   assert all(v[k]=='0' for k in ('RandomStartX','RandomStartY','RandomStartZ'))
   rotators.append({'path':path(v['m_GameObject']['fileID']),'speed':{k.lower():float(v['RotateSpeed'+k]) for k in 'XYZ'}})
  if v.get('m_AudioClip') and gos[v['m_GameObject']['fileID']]['m_Name']=='SFX_Action':audio=sound_clip({'fileID':str(i)},f)
  if 'm_ObjectToRender' in v:capture=v
  if 'fsm' in v and kind=='impact' and audio is None:
   for state in v['fsm']['states']:
    if state['name']!='Death 1':continue
    for obj in state['actionData']['fsmObjectParams']:
     if obj['value'].get('guid') in idx:audio=sound_clip(obj['value'],f)
 alphaKeys=[];lightKeys=[]
 if kind=='impact':
  for c,v in bs.values():
   if c==114 and 'fsm' in v:
    for state in v['fsm']['states']:
     if state['name']=='Death 3':
      curves=state['actionData']['animationCurveParams']
      def keys(curve):return [{k:float(v) for k,v in key.items() if k in ('time','value','inSlope','outSlope')} for key in curve['curve']['m_Curve']]
      alphaKeys=keys(curves[0]);lightKeys=keys(curves[1])
 assert audio and capture,(name,audio,capture)
 anchor,rotation,_=pose(goTransforms[capture['m_GameObject']['fileID']]);offset=vector(capture['m_PositionOffset']);cameraOffset=vector(capture['m_CameraOffset']);cameraPosition=[anchor[i]+offset[i]+cameraOffset[i] for i in range(3)]
 cameraRotation=mul(rotation,[2**-.5,0,0,2**-.5])
 cameraData={'cameraPosition':dict(zip('xyz',cameraPosition)),'cameraRotation':dict(zip('xyzw',cameraRotation)),'planeOffset':{'x':anchor[0]+offset[0],'y':anchor[2]+offset[2]},'flashRiseDuration':.05 if kind=='impact' and 'Fireball' in name else 0,'flashRiseScale':4 if kind=='impact' and 'Fireball' in name else .5}

 return {**cameraData,'name':name,'sourcePrefab':import_asset(f),'soundPath':audio,'rotators':rotators,'width':float(capture['m_Width']),'height':float(capture['m_Height']),'resolution':int(capture['m_Resolution']),'cameraNear':float(capture['m_NearClip']),'cameraFar':float(capture['m_FarClip']),'captureSettings':capture,'flashAlphaKeys':alphaKeys,'lightKeys':lightKeys}
entries=[]
for card,name,missile,impact in [('CS2_029','Fireball','ReuseFX_Missile_Fire_Fireball_Medium','ReuseFX_Impact_Fire_Fireball_Medium'),('CS2_024','Frostbolt','ReuseFX_Ice_Missile','ReuseFX_Ice_Impact')]:
 src=json.loads((R/(card+'-effect-source.json')).read_text());info=src['scripts'][0]['m_MissileInfo'];assert info['m_PathEaseType']==0 and abs(info['m_PathDurationMin']-.4)<1e-5
 entries.append({'cardId':card,'cardName':name,'pathDuration':info['m_PathDurationMin'],'easeType':0,'missile':phase(missile,'missile'),'impact':phase(impact,'impact')})
# Exact art keys come from recovered CardDef metadata, not filename guesses.
portraits=[]
portrait_folder=D/'Portraits';portrait_folder.mkdir(exist_ok=True)
for card,name in [('CS2_029','Fireball'),('CS2_024','Frostbolt'),('CS2_025','Arcane Explosion'),('CS2_032','Flamestrike')]:
 carddef=json.loads((R/(card+'-carddef-source.json')).read_text())
 def find_key(value):
  if isinstance(value,dict):
   if 'm_PortraitTexturePath' in value:return value['m_PortraitTexturePath']
   for child in value.values():
    found=find_key(child)
    if found:return found
  elif isinstance(value,list):
   for child in value:
    found=find_key(child)
    if found:return found
 key=find_key(carddef);assert key,card
 filename=Path(key.split(':')[0]).with_suffix('.png').name
 original=Path.home()/'Desktop/Hearthstone Export/Texture2D'/filename
 target=portrait_folder/filename
 shutil.copyfile(original,target)
 record={'cardId':card,'cardName':name,'portraitKey':key,'asset':str(target.relative_to(P)),'sha256':hashlib.sha256(original.read_bytes()).hexdigest()}
 portraits.append(record);records.append(record)
(D/'SourceConfiguration.json').write_text(json.dumps({'entries':entries,'portraits':portraits,'sourceRecords':records,'unresolved':unresolved},indent=2)+'\n')
(P/'Docs/Recovery/Spells-source.json').write_text(json.dumps({'cards':[json.loads((R/(c+'-effect-source.json')).read_text()) for c in ['CS2_029','CS2_024']],'files':records,'note':'Original prefab/SoundDef references verified. Original RotateOverTime Self axes and iTween easeInQuad0/easeOutQuad1 inspected from metadata. Source camera pose, longest-axis texture resolution, viewport and Fireball .05s linear flash rise to4 followed by .75s easeOutQuad to7 retained. Original shader bloom/blur/distortion/shake are compatibility gaps. Actor-root trail particles share the compatible capture instead of the original separate world rendering.'},indent=2)+'\n')
print('Recovered',len(entries),'spell sequences;',len(records),'source assets/audio records')
