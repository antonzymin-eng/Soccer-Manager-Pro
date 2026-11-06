"""Created 2026-11-04: rebuild the pinned B capture from a checkout into scratch.

The original as-run preparation scripts are retained in as-run-v1.tar.xz.
This portable entry point never changes the source checkout or historical freeze.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import tarfile
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
SOURCE_PATHS = ['src', 'tools/dotnet-ci', 'Directory.Build.targets', 'Packages', 'ProjectSettings']

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b''): digest.update(chunk)
    return digest.hexdigest()

def repository_root(start):
    for path in (start, *start.parents):
        if (path / '.git').exists(): return path
    raise ValueError('Pass --repo pointing to the source Git checkout')

def capture_environment(work, packages=None):
    env = os.environ.copy()
    env.update(DOTNET_CLI_HOME=str(work / 'dotnet-home'), DOTNET_NOLOGO='1',
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_PROCESSOR_COUNT='4')
    if packages: env['NUGET_PACKAGES'] = str(packages.resolve())
    elif not env.get('NUGET_PACKAGES'): env['NUGET_PACKAGES'] = str(work / 'nuget-packages')
    return env

def validate_runtime(dotnet, work, env, freeze):
    if platform.system() != 'Linux' or platform.machine() != 'x86_64':
        raise ValueError('This declared lane is Linux x64')
    if sha(Path(dotnet)) != freeze['dotnetSha256']:
        raise ValueError('dotnet host executable differs from the frozen lane')
    version = subprocess.check_output([dotnet, '--version'], cwd=work, env=env, text=True).strip()
    if version != freeze['sdk']: raise ValueError(f"Expected SDK {freeze['sdk']}; found {version}")
    runtimes = subprocess.check_output([dotnet, '--list-runtimes'], env=env, text=True)
    framework = None
    for line in runtimes.splitlines():
        if line.startswith('Microsoft.NETCore.App ' + freeze['clr'] + ' '):
            framework = Path(line.split('[', 1)[1].rstrip(']')) / freeze['clr']
    if framework is None: raise ValueError('Install Microsoft.NETCore.App ' + freeze['clr'])
    for name, expected in freeze['frameworkSha256'].items():
        if sha(framework / name) != expected:
            raise ValueError('CLR identity differs from the frozen lane: ' + name)
    for name, expected in freeze['selectedEnvironment'].items():
        if name not in {'DOTNET_ROOT', 'DOTNET_PROCESSOR_COUNT'} and env.get(name) != expected:
            raise ValueError('JIT environment differs from freeze: ' + name)
    return {str(framework / name): digest for name, digest in freeze['frameworkSha256'].items()}

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--work-dir', type=Path, required=True)
    parser.add_argument('--repo', type=Path)
    parser.add_argument('--dotnet', default='dotnet')
    parser.add_argument('--packages', type=Path, help='Optional existing NuGet package cache')
    parser.add_argument('--build', action='store_true')
    parser.add_argument('--offline', action='store_true', help='Restore from package cache only')
    args = parser.parse_args(argv)
    repo = args.repo.resolve() if args.repo else repository_root(HERE)
    work = args.work_dir.resolve()
    if work.exists() and any(work.iterdir()): parser.error('--work-dir must be empty')
    work.mkdir(parents=True, exist_ok=True)
    freeze = json.loads((HERE / 'freeze.json').read_text())
    archive_path = work / 'source.tar'
    with archive_path.open('wb') as output:
        # A source archive must contain the committed pointer bytes, exactly as
        # in the original capture, regardless of the caller's LFS installation.
        subprocess.run(['git', '-C', str(repo), '-c', 'filter.lfs.process=',
                        '-c', 'filter.lfs.smudge=', '-c', 'filter.lfs.required=false',
                        'archive', freeze['sourceCommit'], *SOURCE_PATHS], stdout=output, check=True)
    if sha(archive_path) != freeze['inputs']['source.tar']:
        raise ValueError('Pinned source archive SHA-256 mismatch')
    for name, original in [('Program.cs.txt', 'Program.cs'), ('observer-only.patch', 'observer-only.patch')]:
        if sha(HERE / name) != freeze['inputs'][original]:
            raise ValueError('Frozen instrument changed: ' + name)
    frozen = (HERE / 'FrozenW8.cs.txt').read_bytes()
    blob = hashlib.sha1(b'blob ' + str(len(frozen)).encode() + b'\0' + frozen).hexdigest()
    if blob != freeze['frozenW8GitBlob']: raise ValueError('Frozen W8 observer blob mismatch')
    (work / 'global.json').write_text(json.dumps({'sdk': {'version': freeze['sdk'], 'rollForward': 'disable'}}) + '\n')
    for arm in ('plain', 'observed'):
        root = work / arm
        root.mkdir()
        with tarfile.open(archive_path) as archive: archive.extractall(root, filter='data')
        if arm == 'observed':
            subprocess.run(['patch', '--batch', '--forward', '-p1', '-i', str(HERE / 'observer-only.patch')], cwd=root, check=True)
        subprocess.run(['python3', str(root / 'tools/dotnet-ci/generate_projects.py')], cwd=root, check=True)
        capture = root / 'capture'
        capture.mkdir()
        shutil.copyfile(HERE / 'Program.cs.txt', capture / 'Program.cs')
        shutil.copyfile(HERE / 'FrozenW8.cs.txt', capture / 'FrozenW8.cs')
        source_project = root / 'src/match-engine/tests/match-engine-tests.gen.csproj'
        references = [(source_project.parent / element.attrib['Include']).resolve()
                      for element in ET.parse(source_project).iter('ProjectReference')]
        project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
        properties = ET.SubElement(project, 'PropertyGroup')
        for key, value in {'OutputType':'Exe', 'TargetFramework':'net8.0', 'LangVersion':'9.0',
                           'AssemblyName':'TacticalDirector.MatchEngine.Tests', 'GenerateAssemblyInfo':'false',
                           'Deterministic':'true', 'Optimize':'false', 'DefineConstants':'DEVELOPMENT_BUILD',
                           'RuntimeFrameworkVersion':freeze['clr'], 'RollForward':'Disable'}.items():
            ET.SubElement(properties, key).text = value
        group = ET.SubElement(project, 'ItemGroup')
        for path in references: ET.SubElement(group, 'ProjectReference', Include=str(path))
        ET.SubElement(group, 'PackageReference', Include='NUnit', Version='3.14.0')
        ET.ElementTree(project).write(capture / 'Capture.csproj', encoding='unicode')
    prepared = {'sourceCommit': freeze['sourceCommit'], 'sourceTree': freeze['sourceTree'],
                'sourceArchiveSha256': sha(archive_path), 'asRunFreezeSha256': sha(HERE / 'freeze.json'),
                'sdk':freeze['sdk'], 'clr':freeze['clr'], 'ruleVersion':'1.1', 'built':False,
                'sources':{}, 'binaries':{}, 'runtimeFiles':{}}
    if args.build:
        executable = shutil.which(args.dotnet)
        if executable is None: raise ValueError('dotnet not found; pass --dotnet')
        dotnet = str(Path(executable).resolve())
        env = capture_environment(work, args.packages)
        prepared['runtimeFiles'] = validate_runtime(dotnet, work, env, freeze)
        prepared['dotnet'] = dotnet
        for arm in ('plain', 'observed'):
            command = [dotnet, 'build', str(work / arm / 'capture/Capture.csproj'), '-c', 'Debug',
                       '--disable-build-servers', '-m:1', '-p:NuGetAudit=false', '-v', 'minimal']
            if args.offline:
                feed = work / 'empty-feed'; feed.mkdir(exist_ok=True)
                command += ['--source', str(feed)]
            with (work / (arm + '-build.log')).open('w') as output:
                subprocess.run(command, cwd=work, env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
            binary_root = work / arm / 'capture/bin/Debug/net8.0'
            prepared['binaries'][arm] = {str(p.relative_to(work / arm)): sha(p)
                                        for p in sorted(binary_root.iterdir()) if p.is_file()}
        prepared['built'] = True
    for arm in ('plain', 'observed'):
        prepared['sources'][arm] = {str(p.relative_to(work / arm)):sha(p)
            for p in sorted((work / arm).rglob('*')) if p.is_file()
            and p.suffix in {'.cs', '.csproj', '.json', '.targets', '.asmdef'}
            and not {'bin', 'obj'}.intersection(p.relative_to(work / arm).parts)}
    (work / 'prepared.json').write_text(json.dumps(prepared, indent=2) + '\n')
    print('Prepared pinned source/instrument in', work, '(built=' + str(prepared['built']) + ')')
    return 0

if __name__ == '__main__': raise SystemExit(main())
