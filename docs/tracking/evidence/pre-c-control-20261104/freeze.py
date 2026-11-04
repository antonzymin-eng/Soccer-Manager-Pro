"""Created 2026-11-04: freeze inputs before result-bearing captures."""
from pathlib import Path
import datetime, hashlib, json, os, platform, re, subprocess
HERE=Path(__file__).resolve().parent
REPO=Path('/workspace/Soccer-Manager-Pro')
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
text=(REPO/'docs/tracking/w8-b-implementation-results.md').read_text()
expected=dict(re.findall(r'\| `([0-9A-F]{16})` \| `([0-9a-f]{64})` \|',text))
assert len(expected)==6
(HERE/'expected.json').write_text(json.dumps(expected,indent=2)+'\n')
patch=[]
for name in ['src/match-engine/MatchEngine.cs','src/match-engine/MatchEngine.KeeperHandlingLaw.cs','src/match-engine/MatchEngine.PreCObservation.cs','src/collision-system/CollisionSystem.cs','src/event-system/EventBus.cs']:
    base=HERE/'plain'/name
    result=subprocess.run(['diff','-u','--label','a/'+name,'--label','b/'+name,str(base if base.exists() else Path('/dev/null')),str(HERE/'observed'/name)],capture_output=True,text=True)
    assert result.returncode==1
    patch.append(result.stdout)
(HERE/'observer-only.patch').write_text(''.join(patch))
files={}
for name in ['Program.cs','prepare.py','PROTOCOL.md','analyze.py','test_selection.py','expected.json','observer-only.patch','source.tar','freeze.py','run.py']:
    files[name]=sha(HERE/name)
sources={};binaries={}
for arm in ['plain','observed']:
    root=HERE/arm
    sources[arm]={str(p.relative_to(root)):sha(p) for p in root.rglob('*') if p.is_file() and p.suffix in ('.cs','.csproj','.json','.targets') and not set(('obj','bin')).intersection(p.relative_to(root).parts)}
    binaries[arm]={str(p.relative_to(root)):sha(p) for p in (root/'capture/bin/Debug/net8.0').iterdir() if p.is_file()}
runtime=HERE.parents[1]/'.cloud-tools/dotnet'
# Actual installation path is explicit and recorded, never inferred from HOME.
runtime=Path('/workspace/.cloud-tools/dotnet')
framework=runtime/'shared/Microsoft.NETCore.App/8.0.32'
pin={
 'frozenAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'sourceCommit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=REPO,text=True).strip(),
 'sourceTree':subprocess.check_output(['git','rev-parse','HEAD:src'],cwd=REPO,text=True).strip(),
 'sourceWorkingTree':subprocess.check_output(['git','status','--porcelain=v1'],cwd=REPO,text=True),
 'os':Path('/etc/os-release').read_text(),'machine':platform.machine(),'kernel':platform.release(),
 'sdk':'8.0.426','clr':'8.0.32','configuration':'Debug / Optimize=false / DEVELOPMENT_BUILD',
 'frozenW8GitBlob':subprocess.check_output(['git','hash-object',str(HERE/'observed/capture/FrozenW8.cs')],text=True).strip(),
 'dotnetSha256':sha(runtime/'dotnet'),
 'frameworkSha256':{p.name:sha(p) for p in framework.iterdir() if p.is_file()},
 'selectedEnvironment':{name:os.environ.get(name) for name in ['DOTNET_ROOT','DOTNET_PROCESSOR_COUNT','DOTNET_TieredCompilation','DOTNET_TieredPGO','DOTNET_ReadyToRun','DOTNET_EnableHWIntrinsic','COMPlus_TieredCompilation','COMPlus_TieredPGO','COMPlus_EnableHWIntrinsic']},
 'cpuModel':next(line.split(':',1)[1].strip() for line in Path('/proc/cpuinfo').read_text().splitlines() if line.startswith('model name')),
 'cpuQuota':Path('/sys/fs/cgroup/cpu.max').read_text().strip(),
 'configuration':{'constructor':'new MatchEngine(seed); EnableGkHeading()','frames':324000,'squads':'constructor defaults','gameplayConfig':'unbound GameplayConfig.Empty; shipped fallback values','gkPolicy':'SlowDown both teams, asserted','jit':'runtime defaults, selected override variables recorded above'},
 'inputs':files,'sources':sources,'binaries':binaries}
assert not pin['sourceWorkingTree']
assert pin['frozenW8GitBlob']=='520ba31c71e06ed408fd5c282b5fd5611cf74879'
(HERE/'freeze.json').write_text(json.dumps(pin,indent=2)+'\n')
print('Frozen',pin['frozenAtUtc'],'manifest SHA-256',sha(HERE/'freeze.json'))
