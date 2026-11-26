"""Selected original dependencies for the two-card material/tribe study."""
from pathlib import Path
import re,json,yaml,hashlib,shutil
P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets'
D=P/'Assets/_Project/Art/CardStudy/Source';D.mkdir(parents=True,exist_ok=True)
def index(root):
 result={}
 for m in root.rglob('*.meta'):
  g=re.search(r'^guid: (\w+)',m.read_text(),re.M)
  if g:result[g[1]]=m.with_suffix('')
 return result
source=index(S);existing=index(P/'Assets');queue=[];records=[];seen=set()
def objects(path):
 return {int(i):(int(c),next(iter(yaml.load(b,Loader=yaml.BaseLoader).values()))) for c,i,b in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',path.read_text(),re.S)}
def renderer_paths(path):
 o=objects(path);ts={i:v for i,(c,v) in o.items() if c==4};gos={i:v for i,(c,v) in o.items() if c==1};out={}
 root=next(i for i,t in ts.items() if t['m_Father']['fileID']=='0')
 def walk(i,path):
  t=ts[i];go=gos[int(t['m_GameObject']['fileID'])]
  for c in go['m_Component']:
   kind,v=o[int(c['component']['fileID'])]
   if kind in (23,33):out.setdefault(path,{})[kind]=v
  for child in t.get('m_Children') or []:
   n=int(child['fileID']);name=gos[int(ts[n]['m_GameObject']['fileID'])]['m_Name'];walk(n,(path+'/' if path else '')+name)
 walk(root,'');return out
lib=next(iter(yaml.load((P/'Assets/_Project/Art/NativeCards/NativeCards.asset').read_text().split('--- !u!114 &11400000\n')[-1],Loader=yaml.BaseLoader).values()))
for family,seed in [('minion','Card_Hand_Ally'),('spell','Card_Hand_Ability')]:
 paths=renderer_paths(next(S.rglob(seed+'.prefab')))
 recipes=lib[family]
 keys=[r['source'].rsplit(' [',1)[0] for r in recipes if r['role']!='2']
 if family=='minion':keys.append('RootObject/RacePlate_mesh')
 for key in set(keys):
  for c,v in paths[key].items():
   for guid in re.findall(r"'guid': '(\w+)'",str(v)):
    if guid in source:queue.append(source[guid])
queue.append(next(S.rglob('Card_InHand_Ability_Description_School_mesh.asset')))
while queue:
 p=queue.pop();g=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
 if g in seen:continue
 seen.add(g)
 if p.suffix not in ('.asset','.mat','.png'):continue
 raw=p.read_bytes();text=None
 if p.suffix in ('.asset','.mat'):
  text=p.read_text()
  for dep in set(re.findall(r'guid: (\w+)',text)):
   if dep in source and source[dep].suffix in ('.asset','.mat','.png'):queue.append(source[dep])
 if g in existing:dest=existing[g]
 else:
  dest=D/p.name
  if p.suffix=='.mat':text=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',text)
  if text is None:shutil.copyfile(p,dest)
  else:dest.write_text(text)
  shutil.copyfile(str(p)+'.meta',str(dest)+'.meta')
 records.append({'source':str(p.relative_to(S)),'asset':str(dest.relative_to(P)),'sha256':hashlib.sha256(raw).hexdigest(),'reused':g in existing})
(D.parent/'SourceManifest.json').write_text(json.dumps({'purpose':'Two-card source geometry/material study; original source params and dependencies, source shaders stripped only where unavailable; no source game code.','files':records},indent=2)+'\n')
print('Resolved',len(records),'selected mesh/material/paint dependencies')
