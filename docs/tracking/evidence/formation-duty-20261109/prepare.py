"""Created 2026-11-09. Build committed unmodified base/head plus a read-only runner."""
import argparse, hashlib, importlib.util, json, os, shutil, subprocess, tarfile
from pathlib import Path
import xml.etree.ElementTree as ET
HERE=Path(__file__).resolve().parent
BASE='d4b026aa951432d342217314b8c4d49b53a19e7d'
def sha(p): return hashlib.file_digest(Path(p).open('rb'),'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--repo',type=Path,required=True);p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--head',required=True);p.add_argument('--dotnet',required=True);p.add_argument('--packages',type=Path,required=True);a=p.parse_args()
 work=a.work_dir.resolve();work.mkdir(parents=True,exist_ok=False);repo=a.repo.resolve()
 module=importlib.util.spec_from_file_location('historical_prepare',HERE.parent/'pre-c-control-20261104/prepare.py');old=importlib.util.module_from_spec(module);module.loader.exec_module(old)
 frozen=json.loads((old.HERE/'freeze.json').read_text());env=old.capture_environment(work,a.packages);old.validate_runtime(a.dotnet,work,env,frozen)
 (work/'global.json').write_text(json.dumps({'sdk':{'version':'8.0.426','rollForward':'disable'}}))
 pin={'base':BASE,'head':a.head,'dotnet':a.dotnet,'clr':'8.0.32','sdk':'8.0.426','inputs':{p.name:sha(p) for p in HERE.iterdir() if p.is_file()},'arms':{}}
 for arm,commit in [('base',BASE),('head',a.head)]:
  root=work/arm;root.mkdir();archive=work/(arm+'.tar')
  with archive.open('wb') as f:subprocess.run(['git','-C',str(repo),'-c','filter.lfs.process=','-c','filter.lfs.smudge=','-c','filter.lfs.required=false','archive',commit,*old.SOURCE_PATHS],stdout=f,check=True)
  with tarfile.open(archive) as f:f.extractall(root,filter='data')
  subprocess.run(['python3','tools/dotnet-ci/generate_projects.py'],cwd=root,env=env,check=True)
  cap=root/'capture';cap.mkdir();shutil.copyfile(HERE/'Program.cs.txt',cap/'Program.cs')
  source=root/'src/match-engine/tests/match-engine-tests.gen.csproj';refs=[(source.parent/e.attrib['Include']).resolve() for e in ET.parse(source).iter('ProjectReference')]
  project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
  for key,value in {'OutputType':'Exe','TargetFramework':'net8.0','LangVersion':'9.0','AssemblyName':'TacticalDirector.MatchEngine.Tests','GenerateAssemblyInfo':'false','Deterministic':'true','Optimize':'false','DefineConstants':'DEVELOPMENT_BUILD','RuntimeFrameworkVersion':'8.0.32','RollForward':'Disable'}.items():ET.SubElement(props,key).text=value
  group=ET.SubElement(project,'ItemGroup')
  for ref in refs:ET.SubElement(group,'ProjectReference',Include=str(ref))
  ET.SubElement(group,'PackageReference',Include='NUnit',Version='3.14.0');ET.ElementTree(project).write(cap/'Capture.csproj',encoding='unicode')
  with (work/(arm+'-build.log')).open('w') as out:subprocess.run([a.dotnet,'build',str(cap/'Capture.csproj'),'-c','Debug','--disable-build-servers','-m:1','-p:NuGetAudit=false','-v','minimal'],env=env,cwd=work,stdout=out,stderr=subprocess.STDOUT,check=True)
  subprocess.run([a.dotnet,'--fx-version','8.0.32',str(cap/'bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'),'--self-test'],cwd=root,env=env,check=True)
  pin['arms'][arm]={'commit':commit,'archiveSha256':sha(archive),'srcTree':subprocess.check_output(['git','-C',str(repo),'rev-parse',commit+':src'],text=True).strip(),'binaries':{str(p.relative_to(root)):sha(p) for p in sorted((cap/'bin/Debug/net8.0').iterdir()) if p.is_file()},'sources':{str(p.relative_to(root)):sha(p) for p in sorted((root/'src').rglob('*.cs'))}}
 (work/'prepared.json').write_text(json.dumps(pin,indent=2)+'\n');print('Prepared frozen inputs:',work)
if __name__=='__main__':main()
