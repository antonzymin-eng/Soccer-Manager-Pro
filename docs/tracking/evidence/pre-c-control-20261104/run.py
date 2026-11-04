"""Created 2026-11-04: one isolated seed per process; refuse changed frozen inputs."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import datetime, hashlib, json, os, subprocess, time
HERE=Path(__file__).resolve().parent
pin=json.loads((HERE/'freeze.json').read_text())
for name,expected in pin['inputs'].items():
    assert hashlib.sha256((HERE/name).read_bytes()).hexdigest()==expected,name
for arm in ('plain','observed'):
    for name,expected in pin['binaries'][arm].items():
        assert hashlib.sha256((HERE/arm/name).read_bytes()).hexdigest()==expected,name
assert not (HERE/'runs.json').exists(),'refuse overwriting capture evidence'
seeds=list(json.loads((HERE/'expected.json').read_text()))
def run(arm,seed):
    target=HERE/'results'/arm/seed
    assert not target.exists()
    target.mkdir(parents=True)
    cmd=['/workspace/.cloud-tools/dotnet/dotnet',str(HERE/arm/'capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'),seed,arm,str(target)]
    start=datetime.datetime.now(datetime.timezone.utc).isoformat()
    with (target/'runner.log').open('w') as output:
        result=subprocess.run(cmd,cwd=HERE/arm,stdout=output,stderr=subprocess.STDOUT)
    record={'arm':arm,'seed':seed,'command':cmd,'startedUtc':start,'endedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':result.returncode}
    print(json.dumps(record),flush=True)
    return record
runs=[]
with ThreadPoolExecutor(max_workers=2) as pool:
    futures=[pool.submit(run,arm,seed) for seed in seeds for arm in ('plain','observed')]
    for task in as_completed(futures):
        runs.append(task.result())
        (HERE/'runs.json').write_text(json.dumps(runs,indent=2)+'\n')
if any(r['exitCode'] for r in runs): raise SystemExit('capture failure; retain evidence')
