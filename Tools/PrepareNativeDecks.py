"""Copy only the five original deck meshes and paint textures; no client behaviours."""
import json,re,shutil,hashlib
from pathlib import Path
P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets'
rows=json.loads((P.parent.parent/'recovery/card-animation-audit/deck-renderers.json').read_text())
idx={re.search(r'^guid: (\w+)',p.read_text(),re.M)[1]:p.with_suffix('') for p in S.rglob('*.meta') if re.search(r'^guid: (\w+)',p.read_text(),re.M)}
existing={re.search(r'^guid: (\w+)',p.read_text(),re.M)[1]:p.with_suffix('') for p in (P/'Assets').rglob('*.meta') if re.search(r'^guid: (\w+)',p.read_text(),re.M)}
manifest=[]
def copy(p):
 guid=re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
 if guid in existing:return str(existing[guid].relative_to(P))
 dest=P/'Assets/_Project/RecoveredDecks'/p.name;dest.parent.mkdir(parents=True,exist_ok=True)
 shutil.copyfile(p,dest);shutil.copyfile(str(p)+'.meta',str(dest)+'.meta');existing[guid]=dest
 manifest.append(dict(source=str(p.relative_to(S)),destination=str(dest.relative_to(P)),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 return str(dest.relative_to(P))
out=[]
for r in rows:
 if '/Deck_' not in r['mesh'] or not r['mesh'].endswith('_mesh.asset'):continue
 mat=(S/r['materials'][0]).read_text();guid=re.search(r'm_Texture: \{fileID: \d+, guid: (\w+)',mat)[1]
 r['mesh']=copy(S/r['mesh']);r['texture']=copy(idx[guid]);out.append(r)
(P/'Docs/Recovery/NativeDeck-source-poses.json').write_text(json.dumps({'rows':out,'files':manifest},indent=2)+'\n')
print('Imported',len(manifest),'source files;',len(out),'source deck poses')
