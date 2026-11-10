"""Created 2026-11-10: prepare a committed #440 observer without modifying its source."""
import argparse, hashlib, importlib.util, json, os, shutil, subprocess, tarfile
from pathlib import Path
import xml.etree.ElementTree as ET
HERE=Path(__file__).resolve().parent
BASE='d4b026aa951432d342217314b8c4d49b53a19e7d'
def sha(p):return hashlib.file_digest(Path(p).open('rb'),'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--repo',type=Path,required=True);p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--head',required=True);p.add_argument('--dotnet',required=True);p.add_argument('--packages',type=Path,required=True);a=p.parse_args()
 w=a.work_dir.resolve();w.mkdir(parents=True,exist_ok=False);root=w/'engine';root.mkdir();repo=a.repo.resolve()
 spec=importlib.util.spec_from_file_location('old_prepare',HERE.parent/'pre-c-control-20261104/prepare.py');old=importlib.util.module_from_spec(spec);spec.loader.exec_module(old)
 frozen=json.loads((old.HERE/'freeze.json').read_text());env=old.capture_environment(w,a.packages);runtime=old.validate_runtime(a.dotnet,w,env,frozen)
 (w/'global.json').write_text(json.dumps({'sdk':{'version':'8.0.426','rollForward':'disable'}}))
 archive=w/'source.tar'
 with archive.open('wb') as f:subprocess.run(['git','-C',str(repo),'-c','filter.lfs.process=','-c','filter.lfs.smudge=','-c','filter.lfs.required=false','archive',a.head,*old.SOURCE_PATHS],stdout=f,check=True)
 with tarfile.open(archive) as f:f.extractall(root,filter='data')
 subprocess.run(['python3','tools/dotnet-ci/generate_projects.py'],cwd=root,env=env,check=True)
 cap=root/'capture';cap.mkdir();shutil.copyfile(HERE/'Program.cs.txt',cap/'Program.cs')
 source=root/'src/match-engine/tests/match-engine-tests.gen.csproj';refs=[(source.parent/e.attrib['Include']).resolve() for e in ET.parse(source).iter('ProjectReference')]
 project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
 for key,value in {'OutputType':'Exe','TargetFramework':'net8.0','LangVersion':'9.0','AssemblyName':'TacticalDirector.MatchEngine.Tests','GenerateAssemblyInfo':'false','Deterministic':'true','Optimize':'false','DefineConstants':'DEVELOPMENT_BUILD','RuntimeFrameworkVersion':'8.0.32','RollForward':'Disable'}.items():ET.SubElement(props,key).text=value
 group=ET.SubElement(project,'ItemGroup')
 for ref in refs:ET.SubElement(group,'ProjectReference',Include=str(ref))
 ET.SubElement(group,'Compile',Include=str(root/'src/match-engine/tests/BallLooseEpisodeObserver.cs'))
 ET.SubElement(group,'PackageReference',Include='NUnit',Version='3.14.0');ET.ElementTree(project).write(cap/'Capture.csproj',encoding='unicode')
 with (w/'build.log').open('w') as out:subprocess.run([a.dotnet,'build',str(cap/'Capture.csproj'),'-c','Debug','--disable-build-servers','-m:1','-p:NuGetAudit=false','-v','minimal'],env=env,cwd=w,stdout=out,stderr=subprocess.STDOUT,check=True)
 pin={'base':BASE,'head':a.head,'sourceArchiveSha256':sha(archive),'dotnet':a.dotnet,'clr':'8.0.32','sdk':'8.0.426',
      'runtimeFiles':runtime,'inputs':{p.name:sha(p) for p in HERE.iterdir() if p.is_file()},
      'definitionSha256':sha(repo/'docs/tracking/w-440-ball-loose-design.md'),
      'binaries':{str(p.relative_to(root)):sha(p) for p in sorted((cap/'bin/Debug/net8.0').iterdir()) if p.is_file()},
      'sources':{str(p.relative_to(root)):sha(p) for p in sorted((root/'src').rglob('*.cs'))}}
 (w/'prepared.json').write_text(json.dumps(pin,indent=2)+'\n');print('Prepared observer source and identical plain/observed binaries:',w)
if __name__=='__main__':main()
