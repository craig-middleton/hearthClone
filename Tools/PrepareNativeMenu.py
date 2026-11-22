"""Select original TheBox geometry, paint, clips and source state metadata.

Client behaviours, colliders, source audio listeners and unsafe Animation drivers
are excluded. No original client code is executed. Original transforms/meshes/UVs
and animation assets remain unchanged. Source shaders require compatibility code.
"""
from pathlib import Path
import re, json, shutil, hashlib
P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Box-complete-source/ExportedProject/Assets'
D=P/'Assets/_Project/Art/NativeMenu';(D/'Source').mkdir(parents=True,exist_ok=True)
index={}
for m in S.rglob('*.meta'):
 q=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if q:index[q[1]]=m.with_suffix('')
existing={}
for m in (P/'Assets').rglob('*.meta'):
 q=re.search(r'^guid: (\w+)',m.read_text(),re.M)
 if q and D not in m.parents:existing[q[1]]=m.with_suffix('')
base=S/'AssetBundles/essential_base_box_global-prefab-0.unity3d'
clips=['StartOfGame_BoxCameraPerspective','StartOfGame_BoxOpen','StartOfGame_BoxClose','TheBox_FakeShadowFadeIn','TheBox_FakeShadowFadeOut','TheBox_Dark','TheBoxFrame_Dark']
seeds=[base/'TheBox.prefab']+[next(S.rglob(n+'.anim')) for n in clips]
queue=list(seeds);seen=set();records=[];missing=set();materials=[]
# Visual scene graph only. Omit source animation controllers/drivers/particles;
# the authored camera clip is baked through AnimationUtility, evaluated safely.
allowed={1,4,20,23,33,108,137}
while queue:
 f=queue.pop()
 if f in seen:continue
 seen.add(f);guid=re.search(r'^guid: (\w+)',Path(str(f)+'.meta').read_text(),re.M)[1]
 if f.suffix not in ('.prefab','.mat','.asset','.png','.anim','.wav'):
  missing.add(str(f.relative_to(S)));continue
 original=f.read_bytes();text=original.decode() if f.suffix in ('.prefab','.mat','.asset','.anim') else None
 if text is not None:
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
  if f.suffix=='.mat':
   shader=re.search(r'm_Shader: \{fileID: [^,]+, guid: (\w+)',text)
   shaderText=index[shader[1]].read_text() if shader and shader[1] in index else ''
   name=re.search(r'Shader "([^"]+)"',shaderText)
   materials.append({'guid':guid,'name':f.stem,'sourceShader':name[1] if name else 'unresolved'})
   text=re.sub(r'(?m)^  m_Shader:.*$','  m_Shader: {fileID: 0}',text)
  text=re.sub(r'\{fileID: [^,}]+, guid: 0000000deadbeef15deadf00d0000000, type: \d+\}','{fileID: 0}',text)
  for dep in set(re.findall(r'guid: (\w+)',text)):
   if dep in index and index[dep].suffix not in ('.shader','.cs','.controller'):queue.append(index[dep])
   elif dep not in index and dep not in existing and dep!='0000000000000000e000000000000000':missing.add(str(f.relative_to(S))+': '+dep)
 if guid in existing:
  target=existing[guid];records.append({'source':str(f.relative_to(S)),'reused':str(target.relative_to(P)),'guid':guid,'sha256':hashlib.sha256(original).hexdigest()});continue
 target=D/'Source'/ (f.name if f in seeds else guid[:8]+'_'+f.name)
 if text is not None:target.write_text(text)
 else:shutil.copyfile(f,target)
 shutil.copyfile(str(f)+'.meta',str(target)+'.meta')
 records.append({'source':str(f.relative_to(S)),'asset':str(target.relative_to(P)),'guid':guid,'sha256':hashlib.sha256(original).hexdigest()})
(D/'SourceManifest.json').write_text(json.dumps({'seeds':[str(f.relative_to(S)) for f in seeds],'files':records,'unresolved':sorted(missing),'note':'Source meshes/UVs/transforms/paint and complete camera clip. Client code and Animation drivers removed; shader exports are dummy source and mapped explicitly by installer. Original source camera intro only; lid open/close retained for later original transition work.'},indent=2)+'\n')
x=json.loads((P.parent.parent/'recovery/box-objects-summary.json').read_text());root=next(a for a in x['scripts'] if a.get('path')=='TheBox' and 'm_StateInfoList' in a)
(D/'SourceConfiguration.json').write_text(json.dumps({'states':root['m_StateInfoList'],'materials':materials,'sourceEnabledButtonColor':root['m_EnabledColor'],'sourceDisabledButtonColor':root['m_DisabledColor'],'cameraIntroSeconds':4.983333,'note':'All positions, rotations, source material values and camera motion come from TheBox original export. Compatibility surface response and UI-to-world hitbox projection are custom integration.'},indent=2)+'\n')
print('Prepared',len(records),'source assets;',len(missing),'unresolved references; original client scripts excluded')
