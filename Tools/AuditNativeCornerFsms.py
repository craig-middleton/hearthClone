"""Read original corner state data, preserving binary hex arrays (YAML1.1 must not parse them as octal)."""
from pathlib import Path
import yaml,re,json,struct
P=Path(__file__).resolve().parent.parent;source=P.parent.parent/'recovery/ORG-native-source/ExportedProject/Assets/AssetBundles/initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d/ORG.prefab'
blocks=re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',source.read_text(),re.S)
objs={int(i):(int(c),next(iter(yaml.load(b,Loader=yaml.BaseLoader).values()))) for c,i,b in blocks if int(c) in (1,4,114)}
gos={i:v for i,(c,v) in objs.items() if c==1};ts={i:v for i,(c,v) in objs.items() if c==4};gt={int(v['m_GameObject']['fileID']):i for i,v in ts.items()}
def path(g):
 a=[];t=gt.get(int(g),0)
 while t in ts:a.append(gos[int(ts[t]['m_GameObject']['fileID'])]['m_Name']);t=int(ts[t]['m_Father']['fileID'])
 return '/'.join(a[::-1])
def ints(value,count):
 h=value.zfill(count*8);return [int.from_bytes(bytes.fromhex(h[i:i+8]),'little') for i in range(0,len(h),8)]
fields={15:'fsmFloatParams',16:'fsmIntParams',17:'fsmBoolParams',18:'fsmStringParams',19:'fsmGameObjectParams',20:'fsmOwnerDefaultParams',22:'animationCurveParams',24:'fsmObjectParams',25:'fsmColorParams',28:'fsmVector3Params',30:'fsmRectParams',31:'fsmEventTargetParams',35:'fsmQuaternionParams',36:'fsmPropertyParams',37:'fsmVector2Params',38:'fsmTemplateControlParams',39:'fsmVarParams',41:'fsmArrayParams',42:'fsmEnumParams'}
result=[]
for _,(cl,f) in objs.items():
 if cl!=114 or 'fsm' not in f:continue
 owner=path(f['m_GameObject']['fileID']);states=[]
 for s in f['fsm']['states']:
  a=s['actionData'];n=len(a['paramName']);types=ints(a['paramDataType'],n);positions=ints(a['paramDataPos'],n);starts=ints(a['actionStartIndex'],len(a['actionNames']));blob=bytes.fromhex(a['byteData'].zfill((len(a['byteData'])+1)//2*2));enabled=bytes.fromhex(a.get('actionEnabled','').zfill(len(a['actionNames'])*2));actions=[]
  for j,name in enumerate(a['actionNames']):
   params=[]
   for k in range(starts[j],starts[j+1] if j+1<len(starts) else n):
    ty,pos=types[k],positions[k];value=None
    if ty in fields:value=a[fields[ty]][pos]
    elif ty in (0,6,7):value=int.from_bytes(blob[pos:pos+4],'little',signed=True)
    elif ty==1:value=bool(blob[pos])
    elif ty==2:value=struct.unpack('<f',blob[pos:pos+4])[0]
    elif ty in (3,23):value=a['stringParams'][pos]
    elif ty==5:value=a['unityObjectParams'][pos]
    elif ty==12:value={'count':ints(a['arrayParamSizes'],len(a['arrayParamTypes']))[pos],'elementType':a['arrayParamTypes'][pos]}
    else:value={'unknownType':ty,'position':pos}
    params.append({'name':a['paramName'][k],'type':ty,'value':value})
   actions.append({'type':name.split('.')[-1],'enabled':bool(enabled[j]),'params':params})
  states.append({'name':s['name'],'actions':actions,'transitions':s['transitions']})
 result.append({'path':owner,'start':f['fsm']['startState'],'states':states,'variables':f['fsm'].get('variables',{})})
out=P.parent.parent/'recovery/corner-source-normalized.json';out.write_text(json.dumps({'objectPaths':{str(g):path(g) for g in gos},'machines':result},indent=2)+'\n')
print('Decoded',len(result),'original machines to',out)
for m in result:
 if m['path'].endswith(('TowerGate','Catapult')):
  print(m['path'])
  for s in m['states']:
   print(s['name'],[(a['type'],[(p['name'],p['value']) for p in a['params'] if p['name'] in ['animName','animationName','m_AnimName','playMode','blendTime','m_CrossFadeSec','property','GameObject','gameObject','vector','space','mouseDownGateOpen','mouseDownGateClosed']]) for a in s['actions']])
