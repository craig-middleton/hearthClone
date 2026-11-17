"""Import only original classic mulligan geometry/paint; exclude client behaviours/variants."""
from pathlib import Path
import re,shutil,json,hashlib,yaml
P=Path(__file__).resolve().parent.parent;S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets';D=P/'Assets/_Project/Art/NativeMulligan';D.mkdir(exist_ok=True)
index={}
for m in S.rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if g:index[g[1]]=m.with_suffix('')
existing={}
for m in (P/'Assets').rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if g:existing[g[1]]=m.with_suffix('')
records=[];queue=[]
for name in ['BannerMulligan','Mulligan_Button_Keep','Mulligan_Button_Replace','Mulligan_X_Mark','MulliganManager']:
 suffix='essential_base_global-mesh-0.unity3d' if name=='BannerMulligan' else 'essential_base_global-prefab-0.unity3d'
 f=S/'AssetBundles'/suffix/(name+'.prefab');raw=f.read_text();blocks=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',raw,re.S)
 objs={int(i):(int(c),next(iter(yaml.safe_load(b).values()))) for _,c,i,b in blocks if int(c) in (1,4,23,33)};gos={i:v for i,(c,v) in objs.items() if c==1};ts={i:v for i,(c,v) in objs.items() if c==4};gt={v['m_GameObject']['fileID']:i for i,v in ts.items()}
 rootid=next(i for i,g in gos.items() if g['m_Name']==('ConfirmButton' if name=='MulliganManager' else name));rootT=gt[rootid]
 def below(t):
  while t in ts:
   if t==rootT:return True
   t=ts[t]['m_Father']['fileID']
  return False
 validnames={'BannerMulligan','BannerMulliganGlow_mesh','Quad_Btn_Keep_mesh','Quad_Btn_Replace_mesh','Quad_mesh','Button_mesh','Glow','Glow_Mouseover'}
 selected={i for i,g in gos.items() if g['m_Name'] in validnames and below(gt[i])};keptT={rootT}
 for g in selected:
  t=gt[g]
  while t in ts:
   keptT.add(t)
   if t==rootT:break
   t=ts[t]['m_Father']['fileID']
 keptGO={ts[t]['m_GameObject']['fileID'] for t in keptT};keptComp=set(keptT)
 for g in selected:
  keptComp.update(c['component']['fileID'] for c in gos[g]['m_Component'] if c['component']['fileID'] in objs and objs[c['component']['fileID']][0] in (23,33))
 out=raw[:raw.find('--- !u!')]
 for head,c,i,b in blocks:
  if int(i) not in keptGO|keptComp:continue
  if c=='1':
   b=re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keptComp else '',b);b=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',b)
  if c=='4':
   b=re.sub(r'(?m)^  - \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keptT else '',b)
   if int(i)==rootT:b=re.sub(r'^  m_Father:.*$','  m_Father: {fileID: 0}',b,flags=re.M)
  out+=head+b
 out=out.replace('  m_Children:\n  m_Father:','  m_Children: []\n  m_Father:')
 out=re.sub(r'\{fileID: [^,}]+, guid: 0000000deadbeef15deadf00d0000000, type: \d+\}','{fileID: 0}',out)
 dest=D/(('OriginalConfirm' if name=='MulliganManager' else name)+'.prefab');dest.write_text(out)
 records.append({'source':str(f.relative_to(S)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'destination':str(dest.relative_to(P))})
 queue += [index[g] for g in set(re.findall(r'guid: (\w+)',out)) if g in index]
seen=set()
while queue:
 f=queue.pop()
 if f in seen:continue
 seen.add(f);g=re.search(r'^guid: (\w+)',Path(str(f)+'.meta').read_text(),re.M)[1]
 if g in existing:continue
 if f.suffix not in ('.mat','.asset','.png'):raise ValueError(f)
 dest=D/f.name
 if f.suffix in ('.mat','.asset'):
  text=f.read_text()
  for guid in set(re.findall(r'guid: (\w+)',text)):
   if guid in index and index[guid].suffix!='.shader':queue.append(index[guid])
  if f.suffix=='.mat':text=re.sub(r'^  m_Shader:.*$','  m_Shader: {fileID: 0}',text,flags=re.M)
  dest.write_text(text)
 else:shutil.copyfile(f,dest)
 shutil.copyfile(str(f)+'.meta',str(dest)+'.meta');records.append({'source':str(f.relative_to(S)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'destination':str(dest.relative_to(P))})
(P/'Docs/Recovery/Mulligan-source.json').write_text(json.dumps({'files':records},indent=2)+'\n')
print('Imported',len(records),'selected source mulligan parts/dependencies')
