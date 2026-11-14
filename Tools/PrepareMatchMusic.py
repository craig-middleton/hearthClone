"""Convert the selected source WAVs to compact Vorbis assets using system libsndfile."""
import ctypes as c,ctypes.util,json,hashlib
from pathlib import Path
base=Path(__file__).resolve().parent.parent
lib=c.CDLL(ctypes.util.find_library('sndfile'))
class Info(c.Structure):_fields_=[('frames',c.c_longlong),('samplerate',c.c_int),('channels',c.c_int),('format',c.c_int),('sections',c.c_int),('seekable',c.c_int)]
lib.sf_open.argtypes=[c.c_char_p,c.c_int,c.POINTER(Info)];lib.sf_open.restype=c.c_void_p
for name in ['sf_readf_float','sf_writef_float']:
 fn=getattr(lib,name);fn.argtypes=[c.c_void_p,c.POINTER(c.c_float),c.c_longlong];fn.restype=c.c_longlong
lib.sf_close.argtypes=[c.c_void_p];lib.sf_close.restype=c.c_int
lib.sf_command.argtypes=[c.c_void_p,c.c_int,c.c_void_p,c.c_int]
records=json.loads((base/'Docs/MUSIC_SELECTION.json').read_text());out=base/'Assets/_Project/Audio/MatchMusic';out.mkdir(parents=True,exist_ok=True)
for r in records:
 src=Path(r['source']);dest=out/(r['name']+'.ogg');info=Info();reader=lib.sf_open(str(src).encode(),0x10,c.byref(info));assert reader
 target=Info(0,info.samplerate,info.channels,0x200000|0x0060,0,0);writer=lib.sf_open(str(dest).encode(),0x20,c.byref(target));assert writer
 quality=c.c_double(.65);lib.sf_command(writer,0x1300,c.byref(quality),c.sizeof(quality));buf=(c.c_float*(8192*info.channels))()
 try:
  while True:
   count=lib.sf_readf_float(reader,buf,8192)
   if count==0:break
   assert lib.sf_writef_float(writer,buf,count)==count
 finally:lib.sf_close(reader);lib.sf_close(writer)
 r['asset']=str(dest.relative_to(base));r['source_sha256']=hashlib.sha256(src.read_bytes()).hexdigest();r['asset_bytes']=dest.stat().st_size
 print(dest.name,dest.stat().st_size,flush=True)
(base/'Docs/MUSIC_SELECTION.json').write_text(json.dumps(records,indent=2)+'\n')
