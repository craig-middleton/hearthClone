"""Recover native ORG SoundDef bindings directly from original UnityFS type trees.

The narrow visual export replaced missing SoundDefs with deadbeef GUIDs. This
reader follows their original fileID/pathID references instead. It reads data,
never executes source game assemblies. Requires Linux liblz4 and Python stdlib.
"""
from pathlib import Path
import struct,io,ctypes,re,json
lib=ctypes.CDLL('liblz4.so.1');lib.LZ4_decompress_safe.argtypes=[ctypes.c_void_p,ctypes.c_void_p,ctypes.c_int,ctypes.c_int]
def unpack_bundle(p):
 b=io.BytesIO(p.read_bytes())
 def z():
  r=bytearray()
  while (a:=b.read(1))!=b'\0':r+=a
  return r.decode()
 def u(fmt):return struct.unpack('>'+fmt,b.read(struct.calcsize(fmt)))[0]
 def dec(a,n,kind):
  if not kind:return a
  out=ctypes.create_string_buffer(n);s=lib.LZ4_decompress_safe(a,out,len(a),n);assert s==n,(s,n,kind);return out.raw
 assert z()=='UnityFS';version=u('I');z();z();size=u('Q');csize=u('I');usize=u('I');flags=u('I')
 if version>=7:b.seek((b.tell()+15)&~15)
 start=b.tell()
 if flags&128:b.seek(size-csize)
 info=io.BytesIO(dec(b.read(csize),usize,flags&63));info.read(16)
 def iu(fmt):return struct.unpack('>'+fmt,info.read(struct.calcsize(fmt)))[0]
 blocks=[(iu('I'),iu('I'),iu('H')) for _ in range(iu('I'))]
 def iz():
  r=bytearray()
  while (a:=info.read(1))!=b'\0':r+=a
  return r.decode()
 nodes=[(iu('Q'),iu('Q'),iu('I'),iz()) for _ in range(iu('I'))]
 if flags&128:b.seek(start)
 if flags&512:b.seek((b.tell()+15)&~15)
 data=b''.join(dec(b.read(cs),us,f&63) for us,cs,f in blocks)
 return [(name,data[o:o+s]) for o,s,fl,name in nodes]
class Ser:
 def __init__(self,data):
  self.data=data;self.b=io.BytesIO(data);self.types=[]
  self.b.seek(8);self.ver=self.u('I','>');self.b.seek(20);meta=self.u('I','>');size=self.u('Q','>');self.offset=self.u('Q','>');self.b.read(8)
  self.z();self.platform=self.u('i');self.tree=self.u('?');n=self.u('i')
  for _ in range(n):
   cid=self.u('i');strip=self.u('?');index=self.u('h')
   if cid==114:self.b.read(16)
   self.b.read(16);nodes=[]
   if self.tree:
    nn=self.u('i');ns=self.u('i')
    raw=[struct.unpack('<HBBIIiiiQ',self.b.read(32)) for i in range(nn)];strings=self.b.read(ns)
    def string(x):
     if x&0x80000000:return '#'+str(x&0x7fffffff)
     return strings[x:strings.index(b'\0',x)].decode()
    nodes=[dict(level=x[1],type=string(x[3]),name=string(x[4]),size=x[5],flags=x[7]) for x in raw]
   deps=[self.u('i') for i in range(self.u('i'))]
   self.types.append(dict(cid=cid,nodes=nodes))
  self.objects={}
  for i in range(self.u('i')):
   self.align();pid=self.u('q');offset=self.u('Q');size=self.u('I');tid=self.u('i');self.objects[pid]=dict(offset=offset+self.offset,size=size,tid=tid)
  self.scripts=[]
  for i in range(self.u('i')):
   self.u('i');self.align();self.u('q')
  self.externals=[]
  for i in range(self.u('i')):
   self.z();self.b.read(16);self.u('i');self.externals.append(self.z())
 def u(self,f,e='<'):return struct.unpack(e+f,self.b.read(struct.calcsize(f)))[0]
 def z(self):
  r=bytearray()
  while (a:=self.b.read(1)) and a!=b'\0':r+=a
  return r.decode()
 def align(self):self.b.seek((self.b.tell()+3)&~3)
def read_object(s,pid):
 obj=s.objects[pid];nodes=s.types[obj['tid']]['nodes'];b=io.BytesIO(s.data[obj['offset']:obj['offset']+obj['size']])
 def align():b.seek((b.tell()+3)&~3)
 def build(i):
  x=nodes[i].copy();x['children']=[];j=i+1
  while j<len(nodes) and nodes[j]['level']>x['level']:
   n,j=build(j);x['children'].append(n)
  return x,j
 def read(n):
  ty=n['type'];cs=n['children']
  if ty=='#840':
   length=struct.unpack('<i',b.read(4))[0];v=b.read(length).decode();align();return v
  if ty=='#49':
   length=struct.unpack('<i',b.read(4))[0];assert 0<=length<1000000,(length,b.tell(),n);v=[read(cs[1]) for i in range(length)]
  elif cs:v={x['name']:read(x) for x in cs}
  else:
   raw=b.read(n['size']);assert len(raw)==n['size'],(n,b.tell(),obj)
   if ty=='#161':v=struct.unpack('<f',raw)[0]
   elif n['size']==1:v=raw[0]
   elif n['size'] in (2,4,8):v=int.from_bytes(raw,'little',signed=ty in ('#222','#814'))
   else:v=raw.hex()
  if n['flags']&16384:align()
  return v
 v=read(build(0)[0]);assert b.tell()==obj['size'],(b.tell(),obj['size']);return v


def clean(x):
    if isinstance(x,dict):
        return clean(x['#49']) if list(x)==['#49'] else {k:clean(v) for k,v in x.items()}
    if isinstance(x,list): return [clean(i) for i in x]
    return x

def main():
    import argparse, hashlib, shutil
    repo=Path(__file__).resolve().parent.parent
    default=Path('/home/craig-middleton/.local/share/Steam/steamapps/compatdata/2505554610/pfx/drive_c/Program Files (x86)/Hearthstone/Data/Win')
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-bundles',type=Path,default=default)
    parser.add_argument('--audio',type=Path,default=Path('/home/craig-middleton/Desktop/Hearthstone Export/AudioClip'))
    args=parser.parse_args()
    bundle_names=['initial_base_qualityhigh_global-b6b43e63-prefab-0.unity3d','initial_base_global-9401fe9e-prefab-1.unity3d','initial_base_global-9401fe9e-prefab-2.unity3d']
    files={};bundle_hashes=[]
    for name in bundle_names:
        path=args.game_bundles/name
        bundle_hashes.append({'name':name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
        for cab,data in unpack_bundle(path):
            if cab.startswith('CAB-') and not cab.endswith('.resS'):files[cab]=Ser(data)
    root_cab=next(cab for cab,s in files.items() if any(n['name']=='fsm' for t in s.types for n in t['nodes']))
    source=files[root_cab];actions=[]
    for pid,obj in source.objects.items():
        if not any(n['name']=='fsm' for n in source.types[obj['tid']]['nodes']):continue
        machine=clean(read_object(source,pid));owner=read_object(source,machine['#374']['m_PathID'])['#427']
        if owner=='Hut':continue # Existing chimney already owns these clips.
        for state in machine['fsm']['states']:
            a=state['actionData'];starts=a['actionStartIndex'];n=len(a['paramName'])
            for j,action_name in enumerate(a['actionNames']):
                if 'AudioPlay' not in action_name:continue
                pointers=[];weights=[]
                for k in range(starts[j],starts[j+1] if j+1<len(starts) else n):
                    name=a['paramName'][k];pos=a['paramDataPos'][k]
                    if name=='m_Sounds':
                        count=a['arrayParamSizes'][pos]
                        pointers=[a['unityObjectParams'][a['paramDataPos'][ki]] for ki in range(k+1,k+1+count)]
                    elif name=='m_Sound':pointers=[a['fsmObjectParams'][pos]['value']]
                    elif name=='m_Weights':
                        count=a['arrayParamSizes'][pos]
                        weights=[a['fsmFloatParams'][a['paramDataPos'][ki]]['value'] for ki in range(k+1,k+1+count)]
                if not weights:weights=[1]*len(pointers)
                refs=[];clips=[]
                for pointer in pointers:
                    cab=source.externals[pointer['m_FileID']-1].split('/')[-1] if pointer['m_FileID'] else root_cab
                    sound_file=files[cab];sd=clean(read_object(sound_file,pointer['m_PathID']))
                    assert all(sd[k]==1 for k in ('m_RandomPitchMin','m_RandomPitchMax','m_RandomVolumeMin','m_RandomVolumeMax'))
                    assert not sd['m_RandomClips'], 'Random subclip SoundDef needs explicit support'
                    filename=sd['m_AudioClip'].split(':')[0]
                    assert filename.endswith('.wav') and '/' not in filename
                    original=args.audio/filename;dest=repo/'Assets/_Project/Art/NativeCorners/Audio'/filename
                    dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(original,dest)
                    clips.append(str(dest.relative_to(repo)))
                    refs.append({'cab':cab,'pathID':pointer['m_PathID'],'sourceFileID':pointer['m_FileID'],'audioKey':sd['m_AudioClip'],'sha256':hashlib.sha256(original.read_bytes()).hexdigest()})
                actions.append({'machine':owner,'path':'ORG/Clickables/'+owner,'state':state['name'],'actionIndex':j,'clips':clips,'weights':weights,'soundDefs':refs})
    result={'method':'Original UnityFS serialized type trees, original PPtr fileID/pathID to SoundDef.m_AudioClip; not inferred by filenames. WAVs are user desktop exports. SoundDef internal random pitch/volume ranges all exactly1, random subclips empty. Source FSM applies action-level volume/pitch.', 'bundles':bundle_hashes,'actions':actions}
    out=repo/'Docs/Recovery/Corners-audio-source.json';out.write_text(json.dumps(result,indent=2)+'\n')
    print('Recovered',len(actions),'source actions and',len({c for a in actions for c in a['clips']}),'original WAVs to NativeCorners/Audio')

if __name__=='__main__': main()
