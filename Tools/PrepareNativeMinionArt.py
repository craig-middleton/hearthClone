"""Recover original standard played-minion core, shadow and taunt geometry without client code."""
from pathlib import Path
import re,yaml,shutil,json,hashlib
P=Path(__file__).resolve().parent.parent;S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets';D=P/'Assets/_Project/Art/NativeMinions';D.mkdir(exist_ok=True)
index={}
for m in S.rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if g:index[g[1]]=m.with_suffix('')
existing={}
for m in (P/'Assets').rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if g:existing[g[1]]=m.with_suffix('')
queue=[];records=[]
for seed,names in [('Card_Play_Ally',{'Gem_Attack','Mesh','Gem_Health','Highlight_Card_Plane'}),('Card_Play_Ally_Taunt',{'Taunt_Shield_mesh'}),('Card_InPlay_Ally_Shadow',None)]:
 p=next(S.rglob(seed+'.prefab'));raw=p.read_text();parts=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',raw,re.S);objs={int(i):(int(c),next(iter(yaml.load(b,Loader=yaml.BaseLoader).values()))) for h,c,i,b in parts};gos={i:v for i,(c,v) in objs.items() if c==1};ts={i:v for i,(c,v) in objs.items() if c==4};gt={int(v['m_GameObject']['fileID']):i for i,v in ts.items()}
 selected={g for g,v in gos.items() if names is None or v['m_Name'] in names};keepgo=set();keept=set();keepcomp=set()
 for g in selected:
  t=gt[g]
  while t in ts:keept.add(t);keepgo.add(int(ts[t]['m_GameObject']['fileID']));t=int(ts[t]['m_Father']['fileID'])
  keepcomp.update(int(x['component']['fileID']) for x in gos[g]['m_Component'] if objs[int(x['component']['fileID'])][0] in (23,33))
 keep=keepgo|keept|keepcomp;out=raw[:raw.find('--- !u!')]
 for h,c,i,b in parts:
  if int(i) not in keep:continue
  if int(c)==1:
   b=re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keep else '',b);b=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',b)
  elif int(c)==4:b=re.sub(r'(?m)^  - \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keept else '',b);b=b.replace('  m_Children:\n  m_Father:','  m_Children: []\n  m_Father:')
  out+=h+b
 dest=D/(seed+'.prefab');dest.write_text(out);records.append({'source':str(p),'asset':str(dest.relative_to(P)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
 queue.extend(index[g] for g in set(re.findall(r'guid: (\w+)',out)) if g in index)
# Original static silhouette paint for hand/ability highlights, no painted replacement outline.
for seed in ['Card_InHand_Ally_Silhouette','Card_Inhand_Ability_Silhouette']:
 queue.append(next(S.rglob(seed+'.png')))
queue.append(next(S.rglob('HighlightGreenInPlay.mat')))
seen=set()
while queue:
 p=queue.pop();guid=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
 if guid in seen:continue
 seen.add(guid)
 if guid in existing and D not in existing[guid].parents:
  records.append({'source':str(p),'reused':str(existing[guid].relative_to(P))});continue
 if p.suffix not in ('.mat','.asset','.png'):raise ValueError(p)
 dest=D/p.name
 if p.suffix in ('.mat','.asset'):
  text=p.read_text()
  for dep in set(re.findall(r'guid: (\w+)',text)):
   if dep in index and index[dep].suffix!='.shader':queue.append(index[dep])
  if p.suffix=='.mat':text=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',text)
  if p.suffix=='.asset':text=text.replace('m_IsReadable: 0','m_IsReadable: 1')
  dest.write_text(text)
 else:shutil.copyfile(p,dest)
 shutil.copyfile(str(p)+'.meta',str(dest)+'.meta');records.append({'source':str(p),'asset':str(dest.relative_to(P)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(D/'SourceManifest.json').write_text(json.dumps({'method':'Narrow source GameObjects and transform ancestry; original MeshFilter/MeshRenderer only. No client scripts or animation invented. Portrait material is dynamically bound as in source.','files':records},indent=2)+'\n')
print('Recovered standard played-minion frame/portrait/gems, source taunt/shadow and hand silhouette paint;',len(records),'records')
