"""Search the complete audited inventory without re-enumerating the Desktop exports."""
import argparse,csv,gzip,json
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('terms',nargs='*');p.add_argument('--kind');p.add_argument('--limit',type=int,default=40);a=p.parse_args();count=0
with gzip.open(Path(__file__).resolve().parent.parent/'Docs/AssetAudit/inventory.csv.gz','rt',encoding='utf-8') as f:
 for r in csv.DictReader(f):
  if a.kind and r['kind']!=a.kind:continue
  if not all(t.lower() in r['path'].lower() for t in a.terms):continue
  count+=1
  if count<=a.limit:print(json.dumps({k:r[k] for k in ('path','kind','bytes','details')},ensure_ascii=False))
print('Total matches:',count)
