from pathlib import Path
import shutil,subprocess
root=Path('/workspace/scratch/057b1a2b626b'); repo=root/'repo'; base=Path('/workspace/scratch/7f8fcc6d8c8e'); sdk=Path('/workspace/scratch/8b408b121b6b/localization-validation-runtime')
for arm,src,oldout in [('after',repo,base/'validation/build'),('before',base/'corrected-b',base/'validation/before')]:
 out=root/'measure2'/arm;out.mkdir(exist_ok=True)
 for f in oldout.glob('*.dll'):shutil.copy2(f,out/f.name)
 shutil.copy2(base/'validation/build/runner.dll',out/'runner.dll');shutil.copy2(base/'validation/build/runner.runtimeconfig.json',out/'runner.runtimeconfig.json')
 # Compile with the existing dependency closure and exact source compile set.
 for name in ['TacticalDirector.MatchEngine','TacticalDirector.MatchEngine.Tests']:
  rsp=(oldout/(name+'.rsp')).read_text()
  oldsrc=base/('Soccer-Manager-Pro' if arm=='after' else 'corrected-b')
  rsp=rsp.replace(str(oldsrc),str(src)).replace(str(oldout),str(out))
  if name.endswith('.Tests'):
   # Exact frozen common observer, kept byte-for-byte.
   frozen=out/'W8StageADiagnosticTests.cs';shutil.copy2(repo/'docs/tracking/evidence/pre-b-anchor-3974b78-20261013/W8StageADiagnosticTests-520ba31c.cs.txt',frozen)
   rsp=rsp.replace(str(src/'src/match-engine/tests/W8StageADiagnosticTests.cs'),str(frozen))
   rsp+='\n'+str(root/'measure2/MeasurementTests.cs')+'\n'
  elif arm=='after':
   p=src/'src/match-engine/MatchEngine.KeeperHandlingLaw.cs';s=p.read_text()
   s=s.replace('if (_releasedHandKeeper != agentId) _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION;', '''if (_releasedHandKeeper != agentId) { if (_releasedHandKeeper >= 0) Console.WriteLine($"SPELL,end,{_clock.CurrentTick},{_releasedHandKeeper},touch,{agentId},{_teamIds[agentId]}"); _releasedHandKeeper = MatchEngineConstants.NO_POSSESSION; }''')
   s=s.replace('_releasedHandKeeper = agentId;','{ _releasedHandKeeper = agentId; Console.WriteLine($"SPELL,start,{_clock.CurrentTick},{agentId}"); }')
   s=s.replace('private void ClearHandlingLawAtRestart()\n        {','private void ClearHandlingLawAtRestart()\n        {\n            if (_releasedHandKeeper >= 0) Console.WriteLine($"SPELL,end,{_clock.CurrentTick},{_releasedHandKeeper},restart,-1,-1");')
   overlay=out/p.name;overlay.write_text(s);rsp=rsp.replace(str(p),str(overlay))
  response=out/(name+'.rsp');response.write_text(rsp)
  run=subprocess.run([str(sdk/'dotnet'),str(sdk/'sdk/8.0.422/Roslyn/bincore/csc.dll'),'@'+str(response)],capture_output=True,text=True)
  print(arm,name,run.returncode,run.stdout,run.stderr,flush=True)
  if run.returncode:raise SystemExit(run.returncode)
