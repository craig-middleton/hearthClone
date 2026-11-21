"""Translate the six remaining original ORG FSMs to the narrow compatibility adapter."""
from pathlib import Path
import json,re,yaml
P=Path(__file__).resolve().parent.parent
R=P.parent.parent/'recovery'
source=json.loads((R/'corner-source-normalized.json').read_text())
audio=json.loads((P/'Docs/Recovery/Corners-audio-source.json').read_text())['actions']
paths=source['objectPaths']
prefab=R/'ORG-native-source/ExportedProject/Assets/AssetBundles/initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d/ORG.prefab'
component_paths={}
for c,i,b in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',prefab.read_text(),re.S):
    if int(c) not in (65,135):continue
    o=next(iter(yaml.load(b,Loader=yaml.BaseLoader).values()))
    component_paths[i]=paths[o['m_GameObject']['fileID']]

def parameter(name,v):
    p={'name':name}
    if isinstance(v,dict):
        if 'ownerOption' in v:
            return {'name':name,'path':'$owner'} if v['ownerOption']=='0' else parameter(name,v['gameObject'])
        if v.get('useVariable')=='1':
            p.update(variable=v['name'],none=not v['name']);return p
        if 'value' in v:return parameter(name,v['value'])
        if 'fileID' in v:
            p['path']=paths.get(v['fileID'],component_paths.get(v['fileID'],''));return p
        if all(k in v for k in ('x','y','z')):
            p['vector']={k:float(v[k]) for k in ('x','y','z')};return p
        return None  # Original arrays are handled separately (sound lists/weights).
    if isinstance(v,bool):p['number']=int(v)
    elif isinstance(v,(int,float)):p['number']=v
    else:
        p['text']=v
        try:p['number']=float(v)
        except ValueError:pass
    return p

out={'machines':[]}
for m in source['machines']:
    if m['path'].endswith('/Hut'):continue
    machine={k:m[k] for k in ('path','start')};machine['states']=[]
    for s in m['states']:
        state={'name':s['name'],'transitions':[{'eventName':t['fsmEvent']['name'],'toState':t['toState']} for t in s['transitions']],'actions':[]}
        for index,a in enumerate(s['actions']):
            if not a['enabled']:continue
            action={'type':a['type'],'parameters':[],'sourceIndex':index,'soundKey':m['path']+'/'+s['name']+'/'+str(index)}
            for v in a['params']:
                if not v['name']:continue
                if v['name']=='targetProperty':
                    prop=v['value'];assert prop['PropertyName']=='enabled' and prop['TargetTypeName'] in ('UnityEngine.SphereCollider','UnityEngine.BoxCollider')
                    action['parameters'] += [parameter('collider',prop['TargetObject']),parameter('enabled',prop['BoolParameter'])]
                else:
                    p=parameter(v['name'],v['value'])
                    if p is not None:action['parameters'].append(p)
            # Preserve exact sound weights from unnamed serialized array elements.
            for j,v in enumerate(a['params']):
                if v['name']=='m_Weights':
                    action['weights']=[float(q['value']['value']) for q in a['params'][j+1:j+1+v['value']['count']]]
            if a['type'].startswith('Audio'):
                sound=next(v for v in audio if v['path']==m['path'] and v['state']==s['name'] and v['actionIndex']==index)
                action['soundPaths']=sound['clips'];action['weights']=sound['weights']
            state['actions'].append(action)
        machine['states'].append(state)
    out['machines'].append(machine)
target=P/'Assets/_Project/Art/NativeCorners/CornerLogic.json'
target.write_text(json.dumps(out,indent=2)+'\n')
print('Translated',len(out['machines']),'original FSMs, excluding disabled actions; data:',target)
