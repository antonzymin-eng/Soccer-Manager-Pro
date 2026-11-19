"""2026-11-10: resume the storage-interrupted scheduler without changing the capture.

Only original exit-0 receipts are reused. Unreceipted output is retained separately
and rerun. The instrument, binaries, seeds, horizons and comparisons are unchanged.
"""
import concurrent.futures,datetime,json,os,subprocess
from pathlib import Path
from prepare import HERE,sha
from run import SEEDS,same

def main():
 import argparse
 p=argparse.ArgumentParser();p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--main-control',type=Path,required=True);a=p.parse_args();w=a.work_dir.resolve();pin=json.loads((w/'prepared.json').read_text())
 for name,digest in pin['inputs'].items():assert sha(HERE/name)==digest,('changed capture input',name)
 for section in ['sources','binaries']:
  for name,digest in pin[section].items():assert sha(w/'engine'/name)==digest,('changed prepared input',name)
 for name,digest in pin['runtimeFiles'].items():assert sha(name)==digest,('changed runtime',name)
 original=w/'runs-before-recovery.json'
 if not original.exists():(w/'runs.json').rename(original)
 records=json.loads(original.read_text());assert all(r['exitCode']==0 for r in records)
 seen={(r['seed'],r['mode']) for r in records};jobs=[(s,m) for s in SEEDS for m in ['plain','observed'] if (s,m) not in seen]
 for seed,mode in jobs:
  folder=w/'results'/mode/seed
  if folder.exists():
   retained=w/'unreceipted-at-interruption'/mode/seed;retained.parent.mkdir(parents=True,exist_ok=True);folder.rename(retained)
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str(w/'dotnet-home'))
 def task(seed,mode):
  target=w/'results'/mode/seed;target.mkdir(parents=True)
  cmd=[pin['dotnet'],'--fx-version','8.0.32',str(w/'engine/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'),seed,'0',mode,str(target),'324000']
  record={'seed':seed,'mode':mode,'frames':324000,'command':cmd,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'recovery':True}
  with (target/'runner.log').open('w') as out:code=subprocess.run(cmd,cwd=w/'engine',env=env,stdout=out,stderr=subprocess.STDOUT).returncode
  record.update(exitCode=code,endedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat());return record
 recovery=[];it=iter(jobs)
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
  active={pool.submit(task,*job) for job in [next(it),next(it)]}
  while active:
   done,active=concurrent.futures.wait(active,return_when=concurrent.futures.FIRST_COMPLETED)
   for future in done:
    r=future.result();records.append(r);recovery.append(r);print(json.dumps(r),flush=True)
    (w/'runs.json').write_text(json.dumps(records,indent=2)+'\n');(w/'recovery-runs.json').write_text(json.dumps(recovery,indent=2)+'\n')
    if r['exitCode']:raise RuntimeError('Recovery capture failed; stop')
    job=next(it,None)
    if job:active.add(pool.submit(task,*job))
 checks=[]
 for seed in SEEDS:
  for name in ['digests.bin.gz','trajectory.bin.gz','ledger.bin.gz','geometry.bin.gz','team-possession.bin.gz']:
   checks.append({'seed':seed,'comparison':'disabled/enabled','stream':name,'equal':same(w/'results/plain'/seed/name,w/'results/observed'/seed/name)})
   checks.append({'seed':seed,'comparison':'independent main C0/disabled','stream':name,'equal':same(a.main_control/'base/C0'/seed/'plain'/name,w/'results/plain'/seed/name)})
  plain=json.loads((w/'results/plain'/seed/'summary.json').read_text());observed=json.loads((w/'results/observed'/seed/'summary.json').read_text());assert plain['buildHash']==observed['buildHash'] and len(plain['buildHash'])==64
  census=json.loads((w/'results/observed'/seed/'census.json').read_text());episodes=[json.loads(line) for line in (w/'results/observed'/seed/'episodes.jsonl').read_text().splitlines()]
  assert len(episodes)==census['episodes']==census['engineBallLoose'] and all(e['outcome']!='pending' for e in episodes)
 report={'passed':all(r['equal'] for r in checks),'runs':len(records),'originalReceiptedRuns':len(seen),'recoveryRuns':len(recovery),'unreceiptedOriginalOutputsNotUsed':1,'checks':checks};(w/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
 assert report['passed'],'Observation neutrality failed; do not interpret census'
 (w/'PASS.json').write_text(json.dumps(report,indent=2)+'\n')
if __name__=='__main__':main()
