"""Created 2026-11-13. Freeze and build unmodified historical / W13.0 / W13 trees."""
import hashlib, importlib.util, json, os, platform, shutil, subprocess, tarfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
REPO = Path('/workspace/Soccer-Manager-Pro')
DOTNET = Path('/workspace/work/dotnet/dotnet')
COMMITS = {
    'historical': '1d3cebf3b8d874715158067d9deb5fa48b309a30',
    'w13_0': 'f64729d2f2a20a2c6c96f4ce26df80844c9571d4',
    'w13_off': '4171d3d1e3f07a661e085ffa6f51770384af92c8',
}
def sha(path):
    with Path(path).open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()
def git(*args): return subprocess.check_output(['git','-C',str(REPO),*args],text=True).strip()

def main():
    spec=importlib.util.spec_from_file_location('historical_prepare', REPO/'docs/tracking/evidence/pre-c-control-20261104/prepare.py')
    old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
    freeze=json.loads((old.HERE/'freeze.json').read_text())
    env=old.capture_environment(ROOT,Path('/workspace/work/nuget'))
    env['PATH']=str(DOTNET.parent)+os.pathsep+env['PATH']
    env['XDG_DATA_HOME']='/workspace/work/xdg'
    cli=Path(env['DOTNET_CLI_HOME'])/'.dotnet';cli.mkdir(parents=True,exist_ok=True)
    for name in ['8.0.426.dotnetFirstUseSentinel','8.0.426.aspNetCertificateSentinel','8.0.426.toolpath.sentinel']:(cli/name).touch()
    (ROOT/'global.json').write_text(json.dumps({'sdk':{'version':'8.0.426','rollForward':'disable'}})+'\n')
    runtime=old.validate_runtime(str(DOTNET), ROOT,env,freeze)
    if Path('/etc/os-release').read_text()!=freeze['os']:raise RuntimeError('OS differs from frozen lane')
    if platform.release()!=freeze['kernel']:raise RuntimeError('kernel differs from frozen lane')
    protocol='''# W13 frozen-lane identity protocol\n\nCreated 2026-11-13, frozen before result-bearing runs.\n\nReproduce all six historical v27 terminal fingerprints using the unmodified historical source first. Stop the acceptance sequence on any mismatch. Then compare W13 switched OFF against the post-W13.0 branch tip: six fixed seeds, 324000 ticks each, all per-tick digests, terminal digests and 6000-tick checkpoints. No config, gameplay change, tuning, observer callback, or RNG draw. Exact raw equality, with no schema alignment needed (both W13 arms v28). One fresh process per seed, at most two simultaneous captures. SDK 8.0.426 / CLR 8.0.32, Debian 13 x64, Debug, optimization off, DEVELOPMENT_BUILD, default JIT settings, DOTNET_PROCESSOR_COUNT=4. This is the frozen Linux measurement lane, not Unity determinism certification. Every attempt and exit is retained. Historical controls and new W13 evidence remain separate; no prior fingerprint is adopted for W13. Any mismatch is a hard stop, never a rebaseline.\n\nThe W13 baseline is f64729d2 (post-W13.0, including fd73de77); W13 candidate is 4171d3d1 (same runtime source as 4f4e9094). Main 39de4662 is not substituted for the explicit plan baseline. #543 and §10 require a separate source integration and observer-neutrality checks.\n'''
    (ROOT/'IDENTITY-PROTOCOL.md').write_text(protocol)
    expected=json.loads((old.HERE/'expected.json').read_text())
    (ROOT/'historical-expected.json').write_text(json.dumps(expected,indent=2)+'\n')
    pin={'createdUtc':__import__('datetime').datetime.now(__import__('datetime').timezone.utc).isoformat(),
        'dotnet':str(DOTNET),'sdk':'8.0.426','clr':'8.0.32','configuration':'Debug','environment':{
        k:env.get(k) for k in freeze['selectedEnvironment']},'os':Path('/etc/os-release').read_text(),
        'kernel':platform.release(),'machine':platform.machine(),'runtimeFiles':runtime,
        'dotnetSha256':sha(DOTNET),'maxConcurrent':2,'frames':324000,'seeds':list(expected),
        'protocolSha256':sha(ROOT/'IDENTITY-PROTOCOL.md'),'runnerSha256':sha(ROOT/'IdentityProgram.cs'),
        'controllerSha256':sha(ROOT/'run_identity.py'),'preparerSha256':sha(__file__),'arms':{}}
    for name,commit in COMMITS.items():
        root=ROOT/name;root.mkdir(exist_ok=True);archive=ROOT/(name+'-source.tar')
        with archive.open('wb') as f:subprocess.run(['git','-C',str(REPO),'-c','filter.lfs.process=','-c','filter.lfs.smudge=','-c','filter.lfs.required=false','archive',commit,*old.SOURCE_PATHS],stdout=f,check=True)
        if name=='historical' and sha(archive)!=freeze['inputs']['source.tar']:raise RuntimeError('historical archive mismatch')
        with tarfile.open(archive) as f:f.extractall(root,filter='data')
        subprocess.run(['python3','tools/dotnet-ci/generate_projects.py'],cwd=root,env=env,check=True)
        cap=root/'identity';cap.mkdir(exist_ok=True);shutil.copyfile(ROOT/'IdentityProgram.cs',cap/'Program.cs')
        source=root/'src/match-engine/tests/match-engine-tests.gen.csproj'
        refs=[(source.parent/e.attrib['Include']).resolve() for e in ET.parse(source).iter('ProjectReference')]
        project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
        for k,v in {'OutputType':'Exe','TargetFramework':'net8.0','LangVersion':'9.0','AssemblyName':'TacticalDirector.MatchEngine.Tests','GenerateAssemblyInfo':'false','Deterministic':'true','Optimize':'false','DefineConstants':'DEVELOPMENT_BUILD','RuntimeFrameworkVersion':'8.0.32','RollForward':'Disable'}.items():ET.SubElement(props,k).text=v
        group=ET.SubElement(project,'ItemGroup')
        for ref in refs:ET.SubElement(group,'ProjectReference',Include=str(ref))
        ET.SubElement(group,'PackageReference',Include='NUnit',Version='3.14.0');ET.ElementTree(project).write(cap/'Identity.csproj',encoding='unicode')
        with (ROOT/(name+'-build.log')).open('w') as out:
            subprocess.run([str(DOTNET),'build',str(cap/'Identity.csproj'),'-c','Debug','--disable-build-servers','-m:1','-p:NuGetAudit=false','-v','minimal'],cwd=ROOT,env=env,stdout=out,stderr=subprocess.STDOUT,check=True)
        binary=cap/'bin/Debug/net8.0'
        pin['arms'][name]={'commit':commit,'srcTree':git('rev-parse',commit+':src'),'archiveSha256':sha(archive),
            'binary':str(binary/'TacticalDirector.MatchEngine.Tests.dll'),
            'binaries':{p.name:sha(p) for p in sorted(binary.iterdir()) if p.is_file()},
            'sources':{str(p.relative_to(root)):sha(p) for p in sorted((root/'src').rglob('*.cs'))}}
        print('Built',name,commit,flush=True)
    (ROOT/'identity-prepared.json').write_text(json.dumps(pin,indent=2)+'\n')
    print('Frozen identity inputs ready.',flush=True)
if __name__=='__main__':main()
