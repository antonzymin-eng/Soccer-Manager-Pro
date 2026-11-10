"""Created 2026-11-09. Run #434 probes, full observation proof and approved matrix."""
import argparse, concurrent.futures, datetime, gzip, hashlib, json, os, subprocess
from pathlib import Path
from prepare import HERE, sha
SEEDS=list(json.loads((HERE.parent/'pre-c-control-20261104/expected.json').read_text()))
def same(a,b):
 with gzip.open(a,'rb') as x,gzip.open(b,'rb') as y:
  while True:
   p,q=x.read(1024*1024),y.read(1024*1024)
   if p!=q: return False
   if not p:return True
def main():
 p=argparse.ArgumentParser();p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--stage',choices=['probe','control','matrix'],required=True);a=p.parse_args();w=a.work_dir.resolve();pin=json.loads((w/'prepared.json').read_text())
 for name,digest in pin['inputs'].items():
  if sha(HERE/name)!=digest:raise ValueError('Frozen measurement input changed: '+name)
 for arm,data in pin['arms'].items():
  for section in ['sources','binaries']:
   for name,digest in data[section].items():
    if sha(w/arm/name)!=digest:raise ValueError('Prepared input changed: '+name)
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str(w/'dotnet-home'))
 root=w/a.stage;root.mkdir(exist_ok=False);records=[]
 def task(arm,c,seed,mode,frames):
  target=root/arm/('C'+str(c))/seed/mode;target.mkdir(parents=True)
  command=[pin['dotnet'],'--fx-version','8.0.32',str(w/arm/'capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'),seed,str(c),mode,str(target),str(frames)]
  record={'arm':arm,'case':c,'seed':seed,'mode':mode,'frames':frames,'command':command,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
  with (target/'runner.log').open('w') as f:code=subprocess.run(command,cwd=w/arm,env=env,stdout=f,stderr=subprocess.STDOUT).returncode
  record.update(exitCode=code,endedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat());return record
 if a.stage=='probe':jobs=[('base',c,s,'reuse',1000) for s in SEEDS for c in [0,1,2,3,4,9]]
 elif a.stage=='control':
  if not (w/'probe/PASS.json').exists():raise ValueError('Probe eligibility must pass first')
  jobs=[(arm,0,s,mode,324000) for s in SEEDS for arm in ['base','head'] for mode in ['plain','observed']]
 else:
  if not (w/'control/PASS.json').exists():raise ValueError('Full control must pass first')
  jobs=[(arm,c,s,'observed',324000) for s in SEEDS for arm,cases in [('base',[5,6,7,8]),('head',list(range(1,10)))] for c in cases]
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
  # Keep at most two in flight and fail before scheduling further work after a bad result.
  it=iter(jobs);active={pool.submit(task,*j) for j in [next(it),next(it)]}
  while active:
   done,active=concurrent.futures.wait(active,return_when=concurrent.futures.FIRST_COMPLETED)
   for future in done:
    record=future.result();records.append(record);print(json.dumps(record),flush=True);(root/'runs.json').write_text(json.dumps(records,indent=2)+'\n')
    if record['exitCode']:raise RuntimeError('Capture failed; stop and inspect '+str(record))
    nxt=next(it,None)
    if nxt:active.add(pool.submit(task,*nxt))
 checks=[]
 if a.stage=='probe':
  for s in SEEDS:
   for c in [1,2,3,4,9]:
    for name in ['aligned.bin.gz','trajectory.bin.gz','ledger.bin.gz','team-possession.bin.gz']:
     ok=same(root/'base/C0'/s/'reuse'/name,root/'base'/('C'+str(c))/s/'reuse'/name);checks.append({'seed':s,'case':c,'stream':name,'equal':ok})
 elif a.stage=='control':
  for s in SEEDS:
   for arm in ['base','head']:
    for name in ['digests.bin.gz','trajectory.bin.gz','ledger.bin.gz','team-possession.bin.gz','geometry.bin.gz']:
     checks.append({'seed':s,'arm':arm,'stream':name,'equal':same(root/arm/'C0'/s/'plain'/name,root/arm/'C0'/s/'observed'/name)})
   for name in ['aligned.bin.gz','trajectory.bin.gz','ledger.bin.gz','team-possession.bin.gz','geometry.bin.gz']:
    checks.append({'seed':s,'pair':'base/head','stream':name,'equal':same(root/'base/C0'/s/'observed'/name,root/'head/C0'/s/'observed'/name)})
 report={'stage':a.stage,'runs':len(records),'checks':checks,'passed':all(x['equal'] for x in checks)};(root/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
 if not report['passed']:raise ValueError('Paired comparison failed; no acceptance')
 (root/'PASS.json').write_text(json.dumps(report,indent=2)+'\n')
if __name__=='__main__':main()
