"""Import a selected original visual dependency closure without executing client code."""
from pathlib import Path
import re,json,hashlib,shutil
P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets'
D=P/'Assets/_Project/Art/NativeHeroEffects';D.mkdir(exist_ok=True)
index={}
for m in S.rglob('*.meta'):
 match=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if match:index[match[1]]=m.with_suffix('')
existing={}
for m in (P/'Assets').rglob('*.meta'):
 match=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if match:existing[match[1]]=m.with_suffix('')
base=S/'AssetBundles/essential_base_global-prefab-0.unity3d'
seeds=['Card_Play_HeroPower','Card_Play_Hero_EndGame_Lose_Impact','ReuseFX_Missile_Fire_Fireball_Medium','ReuseFX_Impact_Fire_Fireball_Medium']
queue=[base/(n+'.prefab') for n in seeds];seen=set();records=[];missing=set()
allowed={1,4,20,23,33,54,65,95,108,111,135,137,198,199}
while queue:
 f=queue.pop()
 if f in seen:continue
 seen.add(f);guid=re.search(r'^guid: (\w+)',Path(str(f)+'.meta').read_text(),re.M)[1]
 if guid in existing and D not in existing[guid].parents:
  records.append({'source':str(f.relative_to(S)),'reused':str(existing[guid].relative_to(P))});continue
 if f.suffix not in ('.prefab','.mat','.png','.asset','.anim','.controller','.wav'):
  missing.add(str(f.relative_to(S)));continue
 dest=D/f.name
 if f.suffix in ('.prefab','.mat','.asset','.anim','.controller'):
  text=f.read_text()
  if f.suffix=='.prefab':
   blocks=re.findall(r'(--- !u!(\d+) &(-?\d+)\n)(.*?)(?=--- !u!|\Z)',text,re.S)
   keep={int(i) for h,c,i,b in blocks if int(c) in allowed}
   out=text[:text.find('--- !u!')]
   for h,c,i,b in blocks:
    if int(i) not in keep:continue
    if int(c)==1:
     b=re.sub(r'(?m)^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if int(m[1]) in keep else '',b)
     b=re.sub(r'(?m)^  m_TagString:.*$','  m_TagString: Untagged',b)
    b=re.sub(r'\{fileID: (-?\d+)\}',lambda m:m[0] if int(m[1]) in keep or int(m[1])==0 else '{fileID: 0}',b)
    out+=h+b
   text=out
  if f.suffix=='.mat':text=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',text)
  for dep in set(re.findall(r'guid: (\w+)',text)):
   if dep in index and index[dep].suffix not in ('.shader','.cs'):queue.append(index[dep])
   elif dep=='0000000deadbeef15deadf00d0000000':missing.add(str(f.relative_to(S))+': unresolved exported reference')
  text=re.sub(r'\{fileID: [^,}]+, guid: 0000000deadbeef15deadf00d0000000, type: \d+\}','{fileID: 0}',text)
  dest.write_text(text)
 else:shutil.copyfile(f,dest)
 shutil.copyfile(str(f)+'.meta',str(dest)+'.meta')
 records.append({'source':str(f.relative_to(S)),'asset':str(dest.relative_to(P)),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
(D/'SourceManifest.json').write_text(json.dumps({'seeds':seeds,'files':records,'unresolved':sorted(missing),'note':'Visual templates only: source client scripts removed, source controllers/clips retained for audit before playback; materials require disclosed compatibility mapping.'},indent=2)+'\n')
print('Selected dependency closure:',len(records),'assets;',len(missing),'unresolved references require audit')
