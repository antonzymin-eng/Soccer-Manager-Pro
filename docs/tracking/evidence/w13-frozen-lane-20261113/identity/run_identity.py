"""Created 2026-11-13. Two-process frozen control then W13 identity, fail closed."""
import concurrent.futures, datetime, gzip, hashlib, json, os, subprocess, threading
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def stamp():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def main():
    pin=json.loads((ROOT/'identity-prepared.json').read_text());expected=json.loads((ROOT/'historical-expected.json').read_text())
    for name,key in [('run_identity.py','controllerSha256'),('IdentityProgram.cs','runnerSha256'),('IDENTITY-PROTOCOL.md','protocolSha256')]:
        if hashlib.sha256((ROOT/name).read_bytes()).hexdigest()!=pin[key]:raise RuntimeError('frozen input changed: '+name)
    env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(ROOT/'dotnet-home'),DOTNET_PROCESSOR_COUNT='4',NUGET_PACKAGES='/workspace/work/nuget',XDG_DATA_HOME='/workspace/work/xdg',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1')
    status={'startedUtc':stamp(),'state':'running','stage':'historical','completed':[],'running':[],'failures':[]};lock=threading.Lock()
    def save():
        p=ROOT/'identity-status.json';q=p.with_suffix('.tmp');q.write_text(json.dumps(status,indent=2)+'\n');q.replace(p)
    save()
    def run(job):
        arm,seed=job;out=ROOT/'identity-runs'/arm/seed;out.mkdir(parents=True,exist_ok=False)
        cmd=[pin['dotnet'],'--fx-version',pin['clr'],pin['arms'][arm]['binary'],seed,str(pin['frames']),str(out)]
        receipt={'arm':arm,'seed':seed,'command':cmd,'startedUtc':stamp()}
        with lock:status['running'].append(arm+'/'+seed);save()
        with (out/'runner.log').open('w') as log:receipt['exitCode']=subprocess.run(cmd,cwd=ROOT/arm,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
        receipt['finishedUtc']=stamp();(out/'run.json').write_text(json.dumps(receipt,indent=2)+'\n')
        summary=json.loads((out/'summary.json').read_text()) if (out/'summary.json').is_file() else None
        error=None
        if receipt['exitCode']!=0:error='runner exited '+str(receipt['exitCode'])
        elif summary['frames']!=324000 or summary['tick']!=324000:error='incomplete capture'
        elif arm=='historical' and summary['terminal']!=expected[seed]:error='historical fingerprint mismatch'
        else:
            raw=gzip.decompress((out/'digests.bin.gz').read_bytes())
            if len(raw)!=324000*32 or hashlib.sha256(raw).hexdigest()!=summary['allTicks']:error='digest stream integrity'
        with lock:
            if not error and arm in ['w13_0','w13_off']:
                other='w13_off' if arm=='w13_0' else 'w13_0'
                other_dir=ROOT/'identity-runs'/other/seed
                # Only completed/closed streams may be compared; reject immediately when a pair is available.
                if any(r['arm']==other and r['seed']==seed and r['error'] is None for r in status['completed']):
                    a=gzip.decompress((out/'digests.bin.gz').read_bytes())
                    b=gzip.decompress((other_dir/'digests.bin.gz').read_bytes())
                    if a!=b:
                        first=next(i//32+1 for i,(x,y) in enumerate(zip(a,b)) if x!=y)
                        error='switch-off mismatch at tick '+str(first)
            status['running'].remove(arm+'/'+seed);status['completed'].append({'arm':arm,'seed':seed,'exitCode':receipt['exitCode'],'error':error})
            if error:status['failures'].append({'arm':arm,'seed':seed,'error':error})
            save()
        print(stamp(),arm,seed,'FAIL '+error if error else 'PASS',flush=True)
        return error
    def batch(jobs):
        with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
            # Submit only two at once so a hard stop cannot start further jobs.
            it=iter(jobs);pending={pool.submit(run,j) for j in [next(it,None),next(it,None)] if j}
            while pending:
                done,pending=concurrent.futures.wait(pending,return_when=concurrent.futures.FIRST_COMPLETED)
                failed=any(f.result() for f in done)
                if failed:
                    for f in pending:f.result()
                    return False
                for _ in done:
                    j=next(it,None)
                    if j:pending.add(pool.submit(run,j))
        return True
    try:
        if not batch([('historical',s) for s in pin['seeds']]):raise RuntimeError('historical reproduction failed; acceptance not started')
        with lock:status['stage']='w13_identity';save()
        if not batch([(a,s) for s in pin['seeds'] for a in ['w13_0','w13_off']]):raise RuntimeError('identity execution failed')
        comparisons=[]
        for s in pin['seeds']:
            a=ROOT/'identity-runs/w13_0'/s;b=ROOT/'identity-runs/w13_off'/s
            ar=gzip.decompress((a/'digests.bin.gz').read_bytes());br=gzip.decompress((b/'digests.bin.gz').read_bytes())
            same=ar==br;first=None if same else next(i//32+1 for i,(x,y) in enumerate(zip(ar,br)) if x!=y)
            row={'seed':s,'ticksCompared':324000,'allTicksEqual':same,'firstDivergentTick':first,'checkpointsEqual':(a/'checkpoints.txt').read_bytes()==(b/'checkpoints.txt').read_bytes()}
            comparisons.append(row)
            if not same:raise RuntimeError('switch-off mismatch '+s+' at tick '+str(first))
        (ROOT/'identity-comparison.json').write_text(json.dumps({'status':'PASS','pairs':comparisons},indent=2)+'\n')
        with lock:status.update(state='passed',finishedUtc=stamp());save()
        return 0
    except Exception as ex:
        with lock:status.update(state='failed',finishedUtc=stamp(),error=str(ex));save()
        print(str(ex),flush=True);return 1
if __name__=='__main__':raise SystemExit(main())
