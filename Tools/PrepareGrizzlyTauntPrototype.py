"""Record the exact original taunt birth parameters and unchanged source sound."""
from pathlib import Path
import hashlib,json,re,shutil,struct,yaml
P=Path(__file__).resolve().parent.parent
S=P.parent.parent/'recovery/Interface-native-source/ExportedProject/Assets'
D=P/'Assets/_Project/Art/GrizzlyPrototype';D.mkdir(exist_ok=True)
prefab=next(S.rglob('Card_Play_Ally_Taunt.prefab'))
components=[next(iter(yaml.load(b,Loader=yaml.BaseLoader).values())) for c,i,b in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',prefab.read_text(),re.S) if c=='114']
fsm=next(o['fsm'] for o in components if 'fsm' in o)
states={s['name']:s['actionData'] for s in fsm['states']}
# This export stores FsmFloat/Vector3 values in byteData, followed by useVariable.
# Positions are original actionData.paramDataPos, not list indices.
idle=bytes.fromhex(states['Idle State']['byteData']);birth=bytes.fromhex(states['State 6']['byteData'])
read=lambda b,o:struct.unpack_from('<f',b,o)[0]
values={'idleDelay':read(idle,44),'initialScale':[read(birth,i) for i in (13,18,23)],'settledScale':list(struct.unpack_from('<3f',birth,154)),'duration':read(birth,167),'delay':read(birth,172),'easeType':30,'upVolume':read(birth,110),'upPitch':read(birth,115),'upSpatialBlend':read(birth,120)}
assert abs(values['idleDelay']-.6)<1e-6 and abs(values['duration']-.4)<1e-6
assert all(abs(x-.1)<1e-6 for x in values['initialScale'])
assert all(abs(x-.9775735736)<1e-6 for x in values['settledScale'])
assert abs(values['upVolume']-.9)<1e-6 and values['upPitch']==1 and values['upSpatialBlend']==0
clip=next(S.rglob('AllyInPlaySpawn.anim'))
shutil.copyfile(clip,D/clip.name)
shutil.copyfile(str(clip)+'.meta',str(D/clip.name)+'.meta')
sound=Path('/home/craig-middleton/Desktop/Hearthstone Export/AudioClip/taunt_shield_up.wav')
shutil.copyfile(sound,D/sound.name)
(D/'Source.json').write_text(json.dumps({'sourcePrefab':str(prefab),'sourceSha256':hashlib.sha256(prefab.read_bytes()).hexdigest(),'state':'Idle State→State 6','values':values,'sound':{'source':str(sound),'sha256':hashlib.sha256(sound.read_bytes()).hexdigest(),'soundDefGuid':'1fc272e143ba92949b28a80b752836f0'},'spawnClip':{'source':str(clip),'sha256':hashlib.sha256(clip.read_bytes()).hexdigest(),'context':'Card_Play_Ally_SummonIn BIRTH AnimationPlaythrough Actor: AllyInPlaySpawn, crossfade0, playmode4','maxCurveTime':.48333338},'springIL':'Blizzard.T5.iTween.dll iTween.spring, inspected as metadata only','limits':'Original summon position/quaternion channels, shield visibility/birth scale and exact up SFX are ported. Taunt glow/particles/root fade/death/VO omitted. Shared Bear artwork requires additional exact minion-name gate. Initial birth time is view first observation because existing game exposes no source spell-entry clock.'},indent=2)+'\n')
print('Prepared original Grizzly taunt scale/sound source evidence:',values)
