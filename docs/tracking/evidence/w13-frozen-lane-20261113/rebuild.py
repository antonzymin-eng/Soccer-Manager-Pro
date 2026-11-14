"""Created 2026-11-13. Portable preparation wrapper, distinct from retained as-run tools."""
import argparse,hashlib,json,os,platform,shutil,subprocess,tarfile,xml.etree.ElementTree as ET
from pathlib import Path
HERE=Path(__file__).resolve().parent
def sha(path):
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--scope',choices=['identity','section10'],required=True);p.add_argument('--repo',type=Path,required=True);p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--dotnet',type=Path,required=True);p.add_argument('--packages',type=Path);p.add_argument('--cli-home',type=Path);p.add_argument('--xdg-data-home',type=Path);p.add_argument('--check-runtime',action='store_true');a=p.parse_args()
 repo=a.repo.resolve();work=a.work_dir.resolve();dotnet=a.dotnet.resolve()
 if work.exists() and any(work.iterdir()):raise ValueError('work directory must be empty')
 work.mkdir(parents=True,exist_ok=True)
 frozen=json.loads((HERE/'identity/identity-prepared.json').read_text())
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str((a.cli_home or work/'dotnet-home').resolve()),XDG_DATA_HOME=str((a.xdg_data_home or work/'xdg').resolve()),DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_NOLOGO='1')
 if a.packages:env['NUGET_PACKAGES']=str(a.packages.resolve())
 (work/'global.json').write_text(json.dumps({'sdk':{'version':'8.0.426','rollForward':'disable'}})+'\n')
 if platform.system()!='Linux' or platform.machine()!='x86_64':raise ValueError('frozen lane is Linux x64')
 if Path('/etc/os-release').read_text()!=frozen['os'] or platform.release()!=frozen['kernel']:raise ValueError('OS/kernel differs from frozen lane')
 if sha(dotnet)!=frozen['dotnetSha256']:raise ValueError('dotnet host differs from frozen lane')
 if subprocess.check_output([str(dotnet),'--version'],cwd=work,env=env,text=True).strip()!='8.0.426':raise ValueError('SDK differs')
 for name,value in frozen['environment'].items():
  if name not in ['DOTNET_ROOT','DOTNET_PROCESSOR_COUNT'] and env.get(name)!=value:raise ValueError('JIT environment differs: '+name)
 framework=dotnet.parent/'shared/Microsoft.NETCore.App/8.0.32'
 for path,h in frozen['runtimeFiles'].items():
  if sha(framework/Path(path).name)!=h:raise ValueError('CLR file differs: '+path)
 if a.check_runtime:print('Frozen host, OS/kernel, SDK, CLR and JIT settings match.');return
 pin=json.loads((HERE/('identity/identity-prepared.json' if a.scope=='identity' else 'section10/phase-aligned/capture-prepared.json')).read_text())
 receipts={}
 for arm,info in pin['arms'].items():
  root=work/arm;root.mkdir();archive=work/(arm+'-source.tar')
  command=['git','-C',str(repo),'-c','filter.lfs.process=','-c','filter.lfs.smudge=','-c','filter.lfs.required=false','archive',info['commit'],'src','tools/dotnet-ci','Directory.Build.targets','Packages','ProjectSettings']
  with archive.open('wb') as f:subprocess.run(command,stdout=f,check=True)
  if sha(archive)!=info['archiveSha256']:raise ValueError('source archive mismatch: '+arm)
  with tarfile.open(archive) as f:f.extractall(root,filter='data')
  subprocess.run(['python3','tools/dotnet-ci/generate_projects.py'],cwd=root,env=env,check=True)
  cap=root/'capture';cap.mkdir()
  if a.scope=='identity':shutil.copyfile(HERE/'identity/IdentityProgram.cs',cap/'Program.cs')
  else:
   for source in (HERE/'section10/phase-aligned/source').glob('*.cs'):shutil.copyfile(source,cap/source.name)
  reference=root/'src/match-engine/tests/match-engine-tests.gen.csproj'
  refs=[(reference.parent/e.attrib['Include']).resolve() for e in ET.parse(reference).iter('ProjectReference')]
  project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
  for k,v in {'OutputType':'Exe','TargetFramework':'net8.0','LangVersion':'9.0','AssemblyName':'TacticalDirector.MatchEngine.Tests','GenerateAssemblyInfo':'false','Deterministic':'true','Optimize':'false','DefineConstants':'DEVELOPMENT_BUILD','RuntimeFrameworkVersion':'8.0.32','RollForward':'Disable'}.items():ET.SubElement(props,k).text=v
  group=ET.SubElement(project,'ItemGroup')
  for ref in refs:ET.SubElement(group,'ProjectReference',Include=str(ref))
  ET.SubElement(group,'PackageReference',Include='NUnit',Version='3.14.0');ET.ElementTree(project).write(cap/'Capture.csproj',encoding='unicode')
  with (work/(arm+'-build.log')).open('w') as out:subprocess.run([str(dotnet),'build',str(cap/'Capture.csproj'),'-c','Debug','--disable-build-servers','-m:1','-p:NuGetAudit=false','-v','minimal'],cwd=work,env=env,stdout=out,stderr=subprocess.STDOUT,check=True)
  binary=cap/'bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'
  receipts[arm]={'commit':info['commit'],'archiveSha256':sha(archive),'binary':str(binary),'binaries':{x.name:sha(x) for x in sorted(binary.parent.iterdir()) if x.is_file()}}
 (work/'rebuild-prepared.json').write_text(json.dumps({'scope':a.scope,'sdk':'8.0.426','clr':'8.0.32','arms':receipts},indent=2)+'\n')
 print('Prepared committed inputs:',work,'; preserve these new binary identities. Run one seed per process, at most two captures concurrently.')
if __name__=='__main__':main()
