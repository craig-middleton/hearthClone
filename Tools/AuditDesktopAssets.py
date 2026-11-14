"""Read-only recursive inventory, full SHA-256 and format inspection of Craig's exports."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
from collections import Counter,defaultdict
import csv,gzip,hashlib,io,json,os,time,wave,struct
from PIL import Image
ROOT=Path('/home/craig-middleton/Desktop')
NAMES=['Hearthstone Card Database','Hearthstone Export','Hearthstone Extraction']
OUT=Path(__file__).resolve().parent.parent/'Docs/AssetAudit'
CATEGORIES={'board':['org_','playspace','tabletop','boardframe','groundcorner','_table'], 'card_frame':['card_inhand','card_inplay'], 'hero':['hero','jaina','garrosh'], 'mana_stats':['gem_','mana','armor_d'], 'deck_menu':['deck','thebox','collection','menu'], 'effects':['genfx','impact','slash','fireball','frost','arcane','target','arrow'], 'fonts_models':['.ttf','.otf','.fbx','.obj','.gltf','.glb','.mesh']}
files=sorted(p for name in NAMES for p in (ROOT/name).rglob('*') if p.is_file())
OUT.mkdir(exist_ok=True,parents=True)
start=time.monotonic()
def inspect(p):
 r={'root':p.relative_to(ROOT).parts[0],'path':str(p.relative_to(ROOT)),'extension':p.suffix.lower() or '(none)','bytes':p.stat().st_size,'sha256':'','kind':'','details':{},'error':''}
 try:
  h=hashlib.sha256();data=None;first=b''
  if p.suffix.lower()=='.png':
   data=p.read_bytes();h.update(data);first=data[:64]
  else:
   with p.open('rb') as f:
    first=f.read(64);h.update(first)
    for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
  r['sha256']=h.hexdigest()
  if first.startswith(b'\x89PNG'):
   with Image.open(io.BytesIO(data) if data is not None else p) as im:
    r['kind']='image';r['details']={'width':im.width,'height':im.height,'mode':im.mode};im.verify()
  elif first.startswith(b'RIFF') and first[8:12]==b'WAVE':
   r['kind']='audio'
   with wave.open(str(p)) as w:r['details']={'channels':w.getnchannels(),'rate':w.getframerate(),'frames':w.getnframes(),'seconds':round(w.getnframes()/w.getframerate(),3),'sample_bytes':w.getsampwidth()}
  elif p.suffix.lower() in ('.json',):
   value=json.loads(p.read_text(encoding='utf-8-sig'));r['kind']='data';r['details']={'format':'json','records':len(value),'keys':list(value[0]) if isinstance(value,list) and value and isinstance(value[0],dict) else list(value)[:30] if isinstance(value,dict) else []}
  elif p.suffix.lower()=='.csv':
   with p.open(encoding='utf-8-sig',newline='') as f:
    rows=csv.reader(f);header=next(rows,[]);count=sum(1 for _ in rows)
   r['kind']='data';r['details']={'format':'csv','rows':count,'columns':header}
  elif p.suffix.lower() in ('.txt','.ps1','.m3u8') or not p.suffix:
   text=p.read_text(encoding='utf-8-sig',errors='replace');r['kind']='script' if p.suffix.lower()=='.ps1' else 'text';r['details']={'lines':len(text.replace('\\r\\n','\n').splitlines()),'characters':len(text),'opening':text[:180]}
  else:r['kind']='binary';r['details']={'magic':first[:16].hex()}
 except Exception as e:r['error']=type(e).__name__+': '+str(e)
 return r
counts=Counter();sizes=Counter();folders=Counter();kinds=Counter();dimensions=Counter();hashes={};duplicates=[];errors=[];categories=defaultdict(list);audio=[];datafiles=[];totalbytes=0
with gzip.open(OUT/'inventory.csv.gz','wt',encoding='utf-8',newline='') as f:
 writer=csv.DictWriter(f,fieldnames=['root','path','extension','bytes','sha256','kind','details','error']);writer.writeheader()
 with ThreadPoolExecutor(max_workers=4) as workers:
  for i,r in enumerate(workers.map(inspect,files),1):
   counts[r['extension']]+=1;sizes[r['extension']]+=r['bytes'];folders[r['root']]+=1;kinds[r['kind']]+=1;totalbytes+=r['bytes']
   if r['sha256'] in hashes:duplicates.append({'path':r['path'],'same_as':hashes[r['sha256']],'bytes':r['bytes']})
   elif r['sha256']:hashes[r['sha256']]=r['path']
   if r['error']:errors.append({'path':r['path'],'error':r['error']})
   if r['kind']=='image':dimensions[f"{r['details']['width']}x{r['details']['height']}"]+=1
   if r['kind']=='audio':audio.append({'path':r['path'],**r['details']})
   if r['kind'] in ('data','script','text'):datafiles.append({'path':r['path'],**r['details']})
   for label,terms in CATEGORIES.items():
    if any(t in r['path'].lower() for t in terms):categories[label].append(r['path'])
   r['details']=json.dumps(r['details'],ensure_ascii=False);writer.writerow(r)
   if i%5000==0:print(json.dumps({'scanned':i,'of':len(files),'GiB':round(totalbytes/2**30,2),'seconds':round(time.monotonic()-start)}),flush=True)
summary={'roots':dict(folders),'files':len(files),'bytes':totalbytes,'extensions':dict(counts),'bytes_by_extension':dict(sizes),'kinds':dict(kinds),'image_dimensions':dict(dimensions.most_common()),'unique_hashes':len(hashes),'duplicate_files':len(duplicates),'duplicate_bytes':sum(x['bytes'] for x in duplicates),'errors':errors,'elapsed_seconds':round(time.monotonic()-start)}
for name,value in [('summary',summary),('categories',categories),('duplicates',duplicates),('audio',audio),('data_files',datafiles)]:
 with gzip.open(OUT/(name+'.json.gz'),'wt',encoding='utf-8') as f:json.dump(value,f,ensure_ascii=False)
(OUT/'summary.json').write_text(json.dumps(summary,indent=2))
print(json.dumps(summary,indent=2),flush=True)
