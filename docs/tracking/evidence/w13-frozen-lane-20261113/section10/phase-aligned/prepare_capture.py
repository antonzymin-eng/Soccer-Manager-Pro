"""Created 2026-11-13. Freeze the exact reference and combined runtime source and section 10 instruments."""
import hashlib,importlib.util,json,os,shutil,subprocess,tarfile,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parent
REPO=Path('/workspace/Soccer-Manager-Pro');DOTNET=Path('/workspace/work/dotnet/dotnet')
def sha(path):
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 integration=json.loads((ROOT/'integration-source.json').read_text())
 subprocess.run(['python3',str(ROOT/'extend_runner.py')],check=True)
 spec=importlib.util.spec_from_file_location('old',REPO/'docs/tracking/evidence/pre-c-control-20261104/prepare.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
 env=old.capture_environment(ROOT,Path('/workspace/work/nuget'));env.update(XDG_DATA_HOME='/workspace/work/xdg',PATH=str(DOTNET.parent)+os.pathsep+env['PATH'])
 cli=Path(env['DOTNET_CLI_HOME'])/'.dotnet';cli.mkdir(parents=True,exist_ok=True)
 for name in ['8.0.426.dotnetFirstUseSentinel','8.0.426.aspNetCertificateSentinel','8.0.426.toolpath.sentinel']:(cli/name).touch()
 freeze=json.loads((old.HERE/'freeze.json').read_text());old.validate_runtime(str(DOTNET),ROOT,env,freeze)
 (ROOT/'global.json').write_text(json.dumps({'sdk':{'version':'8.0.426','rollForward':'disable'}})+'\n')
 pin={'dotnet':str(DOTNET),'sdk':'8.0.426','clr':'8.0.32','configuration':'Debug','frames':324000,'maxConcurrent':2,
      'seeds':list(json.loads((old.HERE/'expected.json').read_text())), 'reference':integration['reference'],
      'measurementCommit':integration['measurementCommit'],'inputs':{str(p.relative_to(ROOT)):sha(p) for p in [ROOT/'extend_runner.py',ROOT/'prepare_capture.py',*sorted((ROOT/'source').glob('*.cs'))]},'arms':{}}
 for name,commit in [('reference',integration['reference']),('w13',integration['measurementCommit'])]:
  root=ROOT/name;root.mkdir(exist_ok=True);archive=ROOT/(name+'-source.tar')
  with archive.open('wb') as f:subprocess.run(['git','-C',str(REPO),'-c','filter.lfs.process=','-c','filter.lfs.smudge=','-c','filter.lfs.required=false','archive',commit,*old.SOURCE_PATHS],stdout=f,check=True)
  with tarfile.open(archive) as f:f.extractall(root,filter='data')
  subprocess.run(['python3','tools/dotnet-ci/generate_projects.py'],cwd=root,env=env,check=True)
  cap=root/'capture';cap.mkdir(exist_ok=True)
  for p in (ROOT/'source').glob('*.cs'):shutil.copyfile(p,cap/p.name)
  source=root/'src/match-engine/tests/match-engine-tests.gen.csproj';refs=[(source.parent/e.attrib['Include']).resolve() for e in ET.parse(source).iter('ProjectReference')]
  project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
  for k,v in {'OutputType':'Exe','TargetFramework':'net8.0','LangVersion':'9.0','AssemblyName':'TacticalDirector.MatchEngine.Tests','GenerateAssemblyInfo':'false','Deterministic':'true','Optimize':'false','DefineConstants':'DEVELOPMENT_BUILD','RuntimeFrameworkVersion':'8.0.32','RollForward':'Disable'}.items():ET.SubElement(props,k).text=v
  group=ET.SubElement(project,'ItemGroup')
  for ref in refs:ET.SubElement(group,'ProjectReference',Include=str(ref))
  ET.SubElement(group,'PackageReference',Include='NUnit',Version='3.14.0');ET.ElementTree(project).write(cap/'Capture.csproj',encoding='unicode')
  with (ROOT/(name+'-build.log')).open('w') as out:subprocess.run([str(DOTNET),'build',str(cap/'Capture.csproj'),'-c','Debug','--disable-build-servers','-m:1','-p:NuGetAudit=false','-v','minimal'],cwd=ROOT,env=env,stdout=out,stderr=subprocess.STDOUT,check=True)
  binary=cap/'bin/Debug/net8.0'
  pin['arms'][name]={'commit':commit,'srcTree':subprocess.check_output(['git','-C',str(REPO),'rev-parse',commit+':src'],text=True).strip(),'archiveSha256':sha(archive),
       'binary':str(binary/'TacticalDirector.MatchEngine.Tests.dll'),'binaries':{p.name:sha(p) for p in sorted(binary.iterdir()) if p.is_file()},
       'sources':{str(p.relative_to(root)):sha(p) for p in sorted((root/'src').rglob('*.cs'))}}
  print('Built capture arm',name,commit,flush=True)
 (ROOT/'capture-prepared.json').write_text(json.dumps(pin,indent=2)+'\n');print('Section 10 capture binaries frozen.',flush=True)
if __name__=='__main__':main()
