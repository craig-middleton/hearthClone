"""Extract original chimney particles/collider and transform ancestry, excluding client code."""
from pathlib import Path
import re,shutil,json,hashlib,yaml
P=Path(__file__).resolve().parent.parent;S=P.parent.parent/'recovery/ORG-native-source/ExportedProject/Assets';D=P/'Assets/_Project/Art/Chimney';D.mkdir(exist_ok=True)
p=S/'AssetBundles/initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d/ORG.prefab';raw=p.read_text();parts=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',raw,re.S)
objects={int(i):(int(c),yaml.safe_load(b)) for _,c,i,b in parts if int(c) in (1,4)}
gos={i:b['GameObject'] for i,(c,b) in objects.items() if c==1};ts={i:b['Transform'] for i,(c,b) in objects.items() if c==4}
targets={1900271049623953,1472185465350827,1699617135816954,1518489005915545};keepgo=set(targets);keept=set()
for tid,t in ts.items():
 if t['m_GameObject']['fileID'] not in targets:continue
 while tid in ts:
  keept.add(tid);keepgo.add(ts[tid]['m_GameObject']['fileID']);tid=ts[tid]['m_Father']['fileID']
keepcomp=set(keept)
for g in targets:keepcomp.update(x['component']['fileID'] for x in gos[g]['m_Component'])
kept=keepgo|keepcomp;out=raw[:raw.find('--- !u!')]
for head,c,i,b in parts:
 if int(i) not in kept:continue
 if int(c) not in (1,4,135,198,199):continue
 if int(c)==1:
  b=re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keepcomp else '',b);b=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',b)
 if int(c)==4:b=re.sub(r'(?m)^  - \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keept else '',b)
 b=b.replace('m_Layer: 16','m_Layer: 0');b=re.sub(r'\{fileID: [^,}]+, guid: 0000000deadbeef15deadf00d0000000, type: \d+\}','{fileID: 0}',b)
 out+=head+b
# Leaf transform lists must remain valid Unity YAML.
out=out.replace('  m_Children:\n  m_Father:','  m_Children: []\n  m_Father:')
(D/'ChimneyEffects.prefab').write_text(out)
idx={}
for m in S.rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if g:idx[g[1]]=m.with_suffix('')
existing={}
for m in (P/'Assets').rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if g:existing[g[1]]=m.with_suffix('')
queue=[idx[g] for g in set(re.findall(r'guid: (\w+)',out)) if g in idx];records=[]
# Explicit source-game ember artwork for the renderer whose original material is null.
queue.append(S/'AssetBundles/essential_base_global-texture-2.unity3d/GenFX_Embers01.png')
seen=set()
while queue:
 f=queue.pop()
 if f in seen:continue
 seen.add(f);g=re.search(r'^guid: (\w+)',Path(str(f)+'.meta').read_text(),re.M)[1]
 if g in existing:continue
 if f.suffix not in ('.mat','.png','.asset'):raise ValueError(f)
 dest=D/f.name
 if f.suffix in ('.mat','.asset'):
  text=f.read_text()
  for dep in set(re.findall(r'guid: (\w+)',text)):
   if dep in idx and idx[dep].suffix!='.shader':queue.append(idx[dep])
  if f.suffix=='.mat':text=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',text)
  dest.write_text(text)
 else:shutil.copyfile(f,dest)
 shutil.copyfile(str(f)+'.meta',str(dest)+'.meta');records.append({'source':str(f.relative_to(S)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
for i in range(1,4):
 f=Path.home()/f'Desktop/Hearthstone Export/AudioClip/Orgrimmar_Clickable_Blacksmith_0{i}.wav';shutil.copyfile(f,D/f.name);records.append({'source':str(f),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
(P/'Docs/Recovery/Chimney-source.json').write_text(json.dumps({'prefab_source':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'kept_game_objects':len(keepgo),'particle_systems':3,'original_ember_material':None,'ember_compatibility_texture':'GenFX_Embers01.png','original_click_cooldown':.2,'files':records},indent=2)+'\n')
print('Extracted',len(keepgo),'transforms, 3 source emitters/collider,',len(records),'dependencies/sounds')
