"""Record active original Fireball shader families from the complete source export.
This does not execute the original client or mistake stale material properties for
properties declared by the material's actual original shader.
"""
from pathlib import Path
import re, json, hashlib, yaml
P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Box-complete-source/ExportedProject/Assets'
D=P/'Assets/_Project/Art/FireballPrototype'
D.mkdir(exist_ok=True)
index={}
for meta in S.rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
    if match:index[match[1]]=meta.with_suffix('')
names=['GenFX_FireballCore','GenFX_FireballGlow01','GenFX_FireballImpactGlow01',
       'GenFX_FireballBlastMark','GenFX_FireParticle01','GenFX_FireParticle02',
       'GenFX_FireParticle03','GenFX_CoreBlack2','GenFX_CoreBlack3',
       'GenFX_FireBallSmoke02','GenFX_FireBallSparks01']
rows=[]
for name in names:
    source=next(S.rglob(name+'.mat'))
    raw=source.read_text()
    guid=re.search(r'^guid: (\w+)',Path(str(source)+'.meta').read_text(),re.M)[1]
    shader=index[re.search(r'm_Shader: \{[^}]*guid: (\w+)',raw)[1]]
    shader_text=shader.read_text()
    shader_name=re.search(r'Shader "([^"]+)"',shader_text)[1]
    props=sorted(set(re.findall(r'(_\w+)\s*\(',shader_text.split('//DummyShaderTextExporter')[0])))
    saved=yaml.load(raw[raw.index('Material:'):],Loader=yaml.BaseLoader)['Material']['m_SavedProperties']
    floats=[dict(name=k,value=float(v)) for k,v in saved.get('m_Floats',{}).items() if k in props]
    colors=[dict(name=k,value={a:float(b) for a,b in v.items()}) for k,v in saved.get('m_Colors',{}).items() if k in props]
    textures=[]
    for key,tex in saved.get('m_TexEnvs',{}).items():
        if key not in props:continue
        original=index.get(tex['m_Texture'].get('guid'))
        textures.append(dict(name=key,textureName=original.stem if original else '',
            sha256=hashlib.sha256(original.read_bytes()).hexdigest() if original else '',
            builtinId=int(tex['m_Texture']['fileID']) if original is None else 0,
            scale={a:float(b) for a,b in tex['m_Scale'].items()},
            offset={a:float(b) for a,b in tex['m_Offset'].items()}))
    rows.append(dict(floats=floats,colors=colors,textures=textures,name=name,guid=guid,source=str(source.relative_to(S)),
                     sourceShader=shader_name,shaderSource=str(shader.relative_to(S)),
                     activeProperties=props,sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                     shaderSha256=hashlib.sha256(shader.read_bytes()).hexdigest()))
(D/'SourceMaterialFamilies.json').write_text(json.dumps(dict(materials=rows,
    method='Actual source shader references resolved from complete 30-bundle export; material values read from preserved saved serialization by installer.',
    limits='Shader exports contain DummyShaderTextExporter, not original kernels. Compatibility fragment equations, blend/alpha reconstruction and two-layer alpha product remain approximations; source textures/parameters/rates/transforms/particle modules/timing/sounds retained. No invented electrical animation: ElectricityBrightness/GlowBrightness on CoreBlack are stale and not declared by actual Unlit_Cutout shader.'),indent=2)+'\n')
print('Resolved',len(rows),'Fireball source material/shader families')
