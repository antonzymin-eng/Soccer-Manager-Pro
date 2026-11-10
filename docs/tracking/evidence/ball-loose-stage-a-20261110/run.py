"""Created 2026-11-10: six-seed same-binary #440 observer neutrality and census."""
import argparse, concurrent.futures, datetime, gzip, importlib.util, json, os, subprocess
from pathlib import Path
from prepare import HERE, sha
import sys
repo=next(p for p in HERE.parents if (p/'.git').exists())
sys.path.insert(0,str(repo/'tools/dotnet-ci'))
from retained_trace_store import open_trace
SEEDS=list(json.loads((HERE.parent/'pre-c-control-20261104/expected.json').read_text()))
def same(a,b):
 with open_trace(a.parent,a.name) as x,open_trace(b.parent,b.name) as y:
  while True:
   p,q=x.read(1024*1024),y.read(1024*1024)
   if p!=q:return False
   if not p:return True
def main():
 p=argparse.ArgumentParser();p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--main-control',type=Path,required=True);a=p.parse_args();w=a.work_dir.resolve();pin=json.loads((w/'prepared.json').read_text())
 for name,digest in pin['inputs'].items():assert sha(HERE/name)==digest,('changed capture input',name)
 for section in ['sources','binaries']:
  for name,digest in pin[section].items():assert sha(w/'engine'/name)==digest,('changed prepared input',name)
 for name,digest in pin['runtimeFiles'].items():assert sha(name)==digest,('changed runtime',name)
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str(w/'dotnet-home'))
 root=w/'results';root.mkdir(exist_ok=False);records=[]
 def task(seed,mode):
  target=root/mode/seed;target.mkdir(parents=True)
  cmd=[pin['dotnet'],'--fx-version','8.0.32',str(w/'engine/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'),seed,'0',mode,str(target),'324000']
  record={'seed':seed,'mode':mode,'frames':324000,'command':cmd,'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()}
  with (target/'runner.log').open('w') as out:code=subprocess.run(cmd,cwd=w/'engine',env=env,stdout=out,stderr=subprocess.STDOUT).returncode
  record.update(exitCode=code,endedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat());return record
 jobs=iter((seed,mode) for seed in SEEDS for mode in ['plain','observed'])
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
  active={pool.submit(task,*next(jobs)) for _ in range(2)}
  while active:
   done,active=concurrent.futures.wait(active,return_when=concurrent.futures.FIRST_COMPLETED)
   for future in done:
    record=future.result();records.append(record);print(json.dumps(record),flush=True);(w/'runs.json').write_text(json.dumps(records,indent=2)+'\n')
    if record['exitCode']:raise RuntimeError('Capture failed; stop and inspect '+str(record))
    nxt=next(jobs,None)
    if nxt:active.add(pool.submit(task,*nxt))
 checks=[]
 for seed in SEEDS:
  for name in ['digests.bin.gz','trajectory.bin.gz','ledger.bin.gz','geometry.bin.gz','team-possession.bin.gz']:
   checks.append({'seed':seed,'comparison':'disabled/enabled','stream':name,'equal':same(root/'plain'/seed/name,root/'observed'/seed/name)})
  for name in ['digests.bin.gz','trajectory.bin.gz','ledger.bin.gz','geometry.bin.gz','team-possession.bin.gz']:
   checks.append({'seed':seed,'comparison':'independent main C0/disabled','stream':name,'equal':same(a.main_control/'base/C0'/seed/'plain'/name,root/'plain'/seed/name)})
  plain=json.loads((root/'plain'/seed/'summary.json').read_text());observed=json.loads((root/'observed'/seed/'summary.json').read_text())
  assert plain['buildHash']==observed['buildHash'] and len(plain['buildHash'])==64
  census=json.loads((root/'observed'/seed/'census.json').read_text());episodes=[json.loads(line) for line in (root/'observed'/seed/'episodes.jsonl').read_text().splitlines()]
  assert len(episodes)==census['episodes']==census['engineBallLoose']
  assert all(e['outcome']!='pending' for e in episodes)
 report={'passed':all(r['equal'] for r in checks),'runs':len(records),'checks':checks};(w/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
 if not report['passed']:raise ValueError('Observation neutrality failed; do not interpret census')
 (w/'PASS.json').write_text(json.dumps(report,indent=2)+'\n')
if __name__=='__main__':main()
