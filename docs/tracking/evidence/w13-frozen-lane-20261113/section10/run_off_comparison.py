"""Created 2026-11-13. Frozen unarmed C0 reference comparison; independent of the armed stop."""
import concurrent.futures,datetime,gzip,hashlib,json,os,subprocess,threading
from pathlib import Path
ROOT=Path(__file__).resolve().parent
STREAMS={'digests':32,'aligned':32,'trajectory':244,'team-possession':8,'ledger':None,'geometry':None,'marking':None,'mark-witness':None}
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def sha(path):
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def compare(left,right,width=None):
 size=0;ha=hashlib.sha256();hb=hashlib.sha256();equal=True;first=None
 with gzip.open(left,'rb') as a,gzip.open(right,'rb') as b:
  while True:
   x,y=a.read(1024*1024),b.read(1024*1024)
   if not x and not y:break
   if x!=y and first is None:
    first=size+next((i for i,(p,q) in enumerate(zip(x,y)) if p!=q),min(len(x),len(y)))
   equal &= x==y;ha.update(x);hb.update(y);size+=len(x)
 return {'equal':equal,'referenceBytes':size,'referenceSha256':ha.hexdigest(),'w13Sha256':hb.hexdigest(),
         'firstDifferentByte':first,'firstDifferentTick':None if first is None or width is None else first//width+1}
def main():
 pin=json.loads((ROOT/'capture-prepared.json').read_text())
 if json.loads((ROOT.parent/'identity-status.json').read_text())['state']!='passed':raise RuntimeError('historical/W13 identity prerequisite not passed')
 for name,h in pin['inputs'].items():
  if sha(ROOT/name)!=h:raise RuntimeError('capture input changed: '+name)
 for arm,info in pin['arms'].items():
  for name,h in info['binaries'].items():
   if sha(Path(info['binary']).parent/name)!=h:raise RuntimeError('capture binary changed: '+arm+'/'+name)
 protocol='''# Section 10 unarmed C0 comparison\n\nCreated 2026-11-13. This independent subset continues while the armed preflight is blocked by the declared post-tick possession hard stop. It does not waive that stop or start armed full matches.\n\nReference: reviewed 645924057694fd9aa8bf2ab33e43a5810d3d3feb (src 249ce15b33879717a774b1437a1147d28d7918b4). Candidate: frozen source integration of W13 4171d3d1 and that reference, recorded in integration-source.json. Both schemas are v29; compare literal digests, not structurally masked values. Config binding before first catalogue/engine construction sets match-engine.MarkMovementEnabled=false. Both teams C0 F442, otherwise the exact #543 v1.1 setup. 324000 calls, six fixed seeds, one seed per process, at most two captures concurrently. SDK/CLR/JIT/build/environment pins unchanged.\n\nCompare every per-tick raw digest, aligned digest, trajectory, ordered ledger, team-possession witness, geometry, assignment/mark stream and phase witness. All must match. A mismatch, nonfinite value, invalid attribution, invariant failure, nonzero runner exit, truncated stream, or any mark target while disabled stops this subset. Preserve every attempt. No baseline re-adoption, gameplay tuning or source change. Observed mode independently verifies each snapshot preimage and previous link, with BuildHash retained.\n\nThese are 12 full C0 captures (six reference/candidate pairs), not an armed measurement or Unity certification. Short probes and independent diagnosis are separate. The armed hard stop remains unresolved; no M1-M8 armed acceptance claim is made.\n'''
 (ROOT/'OFF-COMPARISON-PROTOCOL.md').write_text(protocol)
 freeze={str(p.relative_to(ROOT)):sha(p) for p in [ROOT/'run_off_comparison.py',ROOT/'capture-prepared.json',ROOT/'integration-source.json',ROOT/'OFF-COMPARISON-PROTOCOL.md']}
 (ROOT/'off-comparison-freeze.json').write_text(json.dumps({'frozenUtc':now(),'files':freeze},indent=2)+'\n')
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str(ROOT/'dotnet-home'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 status={'state':'running','startedUtc':now(),'completed':[],'running':[],'pairs':[],'failures':[]};lock=threading.Lock()
 def save():
  p=ROOT/'off-comparison-status.json';q=p.with_suffix('.tmp');q.write_text(json.dumps(status,indent=2)+'\n');q.replace(p)
 save()
 def task(seed):
  for arm in ['reference','w13']:
   out=ROOT/'off-comparison'/arm/seed;out.mkdir(parents=True,exist_ok=False)
   command=[pin['dotnet'],'--fx-version',pin['clr'],pin['arms'][arm]['binary'],seed,'0','observed',str(out),'324000','off']
   row={'arm':arm,'seed':seed,'command':command,'startedUtc':now()}
   with lock:status['running'].append(arm+'/'+seed);save()
   with (out/'runner.log').open('w') as log:row['exitCode']=subprocess.run(command,cwd=ROOT/arm,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
   row['finishedUtc']=now();(out/'run.json').write_text(json.dumps(row,indent=2)+'\n')
   error=None
   if row['exitCode']!=0:error='runner exited '+str(row['exitCode'])
   else:
    summary=json.loads((out/'summary.json').read_text())
    if summary['frames']!=324000 or summary['schema']!=29:error='frames/schema mismatch'
    elif sum(summary['possession'])!=summary['live'] or sum(summary['teamPossession'])!=summary['live']:error='denominator integrity'
    elif summary['markingEnabled'] or sum(summary['marking']['targetSamplesByTeam'])!=0:error='unexpected armed/mark target'
    else:
     for stream,width in [('digests',32),('aligned',32),('trajectory',244),('team-possession',8)]:
      with gzip.open(out/(stream+'.bin.gz'),'rb') as f:data=f.read()
      if len(data)!=324000*width:error='truncated '+stream;break
     if data is not None:
      with gzip.open(out/'digests.bin.gz','rb') as f:digests=f.read()
      if digests[-32:].hex()!=summary['digest']:error='terminal digest mismatch'
    (out/'files-SHA256.json').write_text(json.dumps({p.name:sha(p) for p in sorted(out.iterdir()) if p.is_file()},indent=2)+'\n')
   with lock:
    status['running'].remove(arm+'/'+seed);status['completed'].append({**row,'error':error})
    if error:status['failures'].append({'arm':arm,'seed':seed,'error':error})
    save()
   print(now(),arm,seed,'FAIL '+error if error else 'PASS execution',flush=True)
   if error:return error
  results={name:compare(ROOT/'off-comparison/reference'/seed/(name+'.bin.gz'),ROOT/'off-comparison/w13'/seed/(name+'.bin.gz'),width) for name,width in STREAMS.items()}
  pair={'seed':seed,'ticks':324000,'streams':results,'equal':all(r['equal'] for r in results.values())}
  with lock:status['pairs'].append(pair);save()
  print(now(),seed,'PAIR EQUAL' if pair['equal'] else 'PAIR MISMATCH',flush=True)
  return None if pair['equal'] else 'stream mismatch '+seed
 try:
  with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
   it=iter(pin['seeds']);active={pool.submit(task,s) for s in [next(it),next(it)]}
   while active:
    done,active=concurrent.futures.wait(active,return_when=concurrent.futures.FIRST_COMPLETED)
    errors=[f.result() for f in done];bad=next((e for e in errors if e),None)
    if bad:
     for f in active:f.result()
     raise RuntimeError(bad)
    for _ in done:
     seed=next(it,None)
     if seed:active.add(pool.submit(task,seed))
  with lock:status.update(state='passed',finishedUtc=now());save()
  (ROOT/'off-comparison-PASS.json').write_text(json.dumps(status,indent=2)+'\n');return 0
 except Exception as ex:
  with lock:status.update(state='failed',finishedUtc=now(),error=str(ex));save()
  print(str(ex),flush=True);return 1
if __name__=='__main__':raise SystemExit(main())
