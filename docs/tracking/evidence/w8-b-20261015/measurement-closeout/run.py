from pathlib import Path
import subprocess,concurrent.futures,os,json,time
root=Path(__file__).resolve().parent
seeds=['0F1E2D3C4B5A6978','00000000D1A6D05E','0000000000000001','00000000ABCDEF12','0000000099887766','000000005A5A5A5A']
def run(job):
 arm,seed=job;p=root/arm;env=dict(os.environ,TD_MEASURE_SEED=seed);start=time.time()
 with (p/(seed+'.log')).open('w') as log:
  r=subprocess.run(['/workspace/scratch/8b408b121b6b/localization-validation-runtime/dotnet',str(p/'runner.dll'),str(p/'TacticalDirector.MatchEngine.Tests.dll'),'--test=TacticalDirector.MatchEngine.MeasurementTests.PairedFrozenCorpus','--workers=0','--result='+str(p/(seed+'.xml'))],stdout=log,stderr=subprocess.STDOUT,env=env)
 result=dict(arm=arm,seed=seed,exit=r.returncode,seconds=time.time()-start);print(json.dumps(result),flush=True);return result
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
 results=list(pool.map(run,[(arm,s) for s in seeds for arm in ['before','after']]))
(root/'runs.json').write_text(json.dumps(results,indent=2))
