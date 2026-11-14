"""Created 2026-11-13. Stop before full capture on a structural, state, or eligibility failure."""
import concurrent.futures,datetime,gzip,hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def main():
 pin=json.loads((ROOT/'capture-prepared.json').read_text());out=ROOT/'preflight';out.mkdir(exist_ok=False)
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str(ROOT/'dotnet-home'),XDG_DATA_HOME='/workspace/work/xdg',NUGET_PACKAGES='/workspace/work/nuget',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1')
 receipt={'startedUtc':now(),'state':'running','runs':[]};(out/'status.json').write_text(json.dumps(receipt,indent=2)+'\n')
 def run(arm,kind,armed=False,c=0,mode='observed'):
  label=f'{kind}-{arm}-C{c}-{mode}-'+('armed' if armed else 'off');target=out/label;target.mkdir()
  if kind=='structural':args=['--self-test']
  elif kind=='state':args=['--state-proof','0','observed',str(target),'0','armed' if armed else 'off']
  else:args=['0000000000000001',str(c),mode,str(target),'1000','armed' if armed else 'off']
  cmd=[pin['dotnet'],'--fx-version',pin['clr'],pin['arms'][arm]['binary'],*args]
  row={'label':label,'arm':arm,'kind':kind,'armed':armed,'case':c,'mode':mode,'command':cmd,'startedUtc':now()}
  with (target/'runner.log').open('w') as log:row['exitCode']=subprocess.run(cmd,cwd=ROOT/arm,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
  row['finishedUtc']=now();(target/'run.json').write_text(json.dumps(row,indent=2)+'\n');print(label,'exit',row['exitCode'],flush=True);return row
 def batch(jobs):
  with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
   it=iter(jobs);active={pool.submit(run,*j) for j in [next(it,None),next(it,None)] if j}
   while active:
    done,active=concurrent.futures.wait(active,return_when=concurrent.futures.FIRST_COMPLETED)
    bad=False
    for f in done:
     row=f.result();receipt['runs'].append(row);bad|=row['exitCode']!=0
    (out/'status.json').write_text(json.dumps(receipt,indent=2)+'\n')
    if bad:
     for f in active:receipt['runs'].append(f.result())
     raise RuntimeError('preflight failed; inspect retained runner logs; no full captures authorized by this gate')
    for _ in done:
     job=next(it,None)
     if job:active.add(pool.submit(run,*job))
 try:
  batch([('reference','structural'),('w13','structural')])
  batch([('reference','state',False),('w13','state',True)])
  # Fixed before capture: complete C0-C9 smoke matrix on all three source/activation arms.
  batch([(arm,'pilot',armed,c) for c in range(10) for arm,armed in [('reference',False),('w13',False),('w13',True)]])
  batch([('w13','pilot',armed,0,'plain') for armed in [False,True]])
  checks=[]
  for armed in [False,True]:
   a=out/('pilot-w13-C0-observed-'+('armed' if armed else 'off'));b=out/('pilot-w13-C0-plain-'+('armed' if armed else 'off'))
   for stream in ['digests','trajectory','ledger','team-possession','geometry']:
    same=gzip.decompress((a/(stream+'.bin.gz')).read_bytes())==gzip.decompress((b/(stream+'.bin.gz')).read_bytes())
    checks.append({'kind':'short_observation_neutrality','armed':armed,'stream':stream,'equal':same});assert same,checks[-1]
  a=out/'pilot-reference-C0-observed-off';b=out/'pilot-w13-C0-observed-off'
  for stream in ['digests','trajectory','ledger','team-possession','geometry','aligned']:
   same=gzip.decompress((a/(stream+'.bin.gz')).read_bytes())==gzip.decompress((b/(stream+'.bin.gz')).read_bytes())
   checks.append({'kind':'short_switch_off_identity','stream':stream,'equal':same});assert same,checks[-1]
  for p in out.glob('pilot-w13-C*-observed-armed/summary.json'):
   result=json.loads(p.read_text());assert sum(result['marking']['targetSamplesByTeam'])>0,('vacuous armed pilot',str(p))
  receipt.update(state='passed',finishedUtc=now(),checks=checks)
  (out/'PASS.json').write_text(json.dumps(receipt,indent=2)+'\n');print('Preflight passed. Short probes are not full-match evidence.',flush=True);return 0
 except Exception as ex:
  receipt.update(state='failed',finishedUtc=now(),error=str(ex));print(str(ex),flush=True);return 1
 finally:(out/'status.json').write_text(json.dumps(receipt,indent=2)+'\n')
if __name__=='__main__':raise SystemExit(main())
