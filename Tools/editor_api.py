"""Call the open project Editor through Unity Pipeline's authenticated loopback API.

Reads credentials from ignored Library/ at request time; never prints or stores tokens.
Usage: python3 Tools/editor_api.py "editor status"
       python3 Tools/editor_api.py "script run" '{"file":"Tools/BuildHearthstoneTable.cs","dry-run":true}'
"""
import json,urllib.request,urllib.error,pathlib,sys,time
PROJECT=pathlib.Path(__file__).resolve().parent.parent
def request(endpoint,body=None,timeout=65):
 d=json.loads((PROJECT/'Library/Pipeline/.unity-pipeline-port').read_text())
 if pathlib.Path(d['projectPath']).resolve()!=PROJECT:
  raise RuntimeError('Pipeline descriptor belongs to a different project')
 r=urllib.request.Request('http://127.0.0.1:%s%s'%(d['port'],endpoint),data=json.dumps(body).encode() if body is not None else None,headers={'Authorization':'Bearer '+d['evalToken'],'Content-Type':'application/json'})
 try:
  with urllib.request.urlopen(r,timeout=timeout) as x: return json.load(x)
 except urllib.error.HTTPError as e: return json.load(e)
def command(name,**parameters):
 result=request('/api/exec',{'command':name,'parameters':parameters})
 expected={'editor.playmode start':'playing','editor.playmode stop':'stopped'}.get(name)
 if expected and result.get('success'):
  deadline=time.monotonic()+30
  while time.monotonic()<deadline:
   try:
    status=request('/api/editor_status',timeout=3)
    if status.get('playMode')==expected and not status.get('compiling') and not status.get('domainReloadInProgress'):return result
   except (OSError,ValueError):pass
   time.sleep(.2)
  return {'success':False,'error':'Editor did not reach '+expected+' within 30 seconds'}
 return result
if __name__=='__main__':
 if len(sys.argv)<2:
  print('Usage: python3 Tools/editor_api.py COMMAND [PARAMETERS_JSON]');raise SystemExit(2)
 if sys.argv[1]=='eval-file': result=command('script eval',file=str(pathlib.Path(sys.argv[2]).resolve()))
 elif sys.argv[1]=='get': result=request(sys.argv[2])
 else: result=command(sys.argv[1],**json.loads(sys.argv[2] if len(sys.argv)>2 else '{}'))
 print(json.dumps(result,indent=2))
 if result.get("success") is False: raise SystemExit(1)
