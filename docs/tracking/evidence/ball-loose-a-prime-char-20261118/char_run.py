"""2026-11-18: D1 six-seed before/after characterization for A' (#440, ERR-014-011).
Reuses the Stage A observer (src/match-engine/tests/BallLooseEpisodeObserver.cs) and runner
(Program.cs.txt) unchanged, built for two heads on the local Windows .NET 8 lane. Observed mode only;
observer neutrality was established by Stage A and is not re-certified here."""
import argparse, concurrent.futures, datetime, json, os, shutil, subprocess, sys, tarfile
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = next(p for p in Path(__file__).resolve().parents if (p / '.git').exists())
EVID = REPO / 'docs/tracking/evidence'
STAGE_A = EVID / 'ball-loose-stage-a-20261110'
SOURCE_PATHS = ['src', 'tools/dotnet-ci', 'Directory.Build.targets', 'Packages', 'ProjectSettings']
SEEDS = list(json.loads((EVID / 'pre-c-control-20261104/expected.json').read_text()))
DOTNET = os.environ.get('DOTNET', 'dotnet')
FX = '8.0.31'
FRAMES = 324000

def env_for(w):
    e = os.environ.copy()
    e.update(DOTNET_CLI_HOME=str(w / 'dotnet-home'), DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1',
             DOTNET_PROCESSOR_COUNT='4')
    return e

def prepare(head, w):
    w.mkdir(parents=True, exist_ok=False); root = w / 'engine'; root.mkdir()
    archive = w / 'source.tar'
    with archive.open('wb') as f:
        subprocess.run(['git', '-C', str(REPO), '-c', 'filter.lfs.process=', '-c', 'filter.lfs.smudge=',
                        '-c', 'filter.lfs.required=false', 'archive', head, *SOURCE_PATHS], stdout=f, check=True)
    with tarfile.open(archive) as f: f.extractall(root, filter='data')
    archive.unlink()
    env = env_for(w)
    subprocess.run([sys.executable, 'tools/dotnet-ci/generate_projects.py'], cwd=root, env=env, check=True)
    cap = root / 'capture'; cap.mkdir(); shutil.copyfile(STAGE_A / 'Program.cs.txt', cap / 'Program.cs')
    source = root / 'src/match-engine/tests/match-engine-tests.gen.csproj'
    refs = [(source.parent / e.attrib['Include']).resolve() for e in ET.parse(source).iter('ProjectReference')]
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk'); props = ET.SubElement(project, 'PropertyGroup')
    for k, v in {'OutputType': 'Exe', 'TargetFramework': 'net8.0', 'LangVersion': '9.0',
                 'AssemblyName': 'TacticalDirector.MatchEngine.Tests', 'GenerateAssemblyInfo': 'false',
                 'Deterministic': 'true', 'Optimize': 'false', 'DefineConstants': 'DEVELOPMENT_BUILD',
                 'RuntimeFrameworkVersion': FX, 'RollForward': 'Disable'}.items():
        ET.SubElement(props, k).text = v
    g = ET.SubElement(project, 'ItemGroup')
    for r in refs: ET.SubElement(g, 'ProjectReference', Include=str(r))
    ET.SubElement(g, 'Compile', Include=str(root / 'src/match-engine/tests/BallLooseEpisodeObserver.cs'))
    ET.SubElement(g, 'PackageReference', Include='NUnit', Version='3.14.0')
    ET.ElementTree(project).write(cap / 'Capture.csproj', encoding='unicode')
    with (w / 'build.log').open('w') as out:
        subprocess.run([DOTNET, 'build', str(cap / 'Capture.csproj'), '-c', 'Debug', '--disable-build-servers', '-m:1',
                        '-p:NuGetAudit=false', '-v', 'minimal'], env=env, cwd=w, stdout=out, stderr=subprocess.STDOUT, check=True)
    (w / 'head.txt').write_text(head + '\n')

def run(w, seeds, workers):
    env = env_for(w); root = w / 'results'; root.mkdir(exist_ok=True); records = []
    dll = w / 'engine/capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'
    def task(seed):
        t = root / 'observed' / seed; t.mkdir(parents=True)
        cmd = [DOTNET, '--fx-version', FX, str(dll), seed, '0', 'observed', str(t), str(FRAMES)]
        rec = {'seed': seed, 'command': cmd, 'startedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
        with (t / 'runner.log').open('w') as out:
            rec['exitCode'] = subprocess.run(cmd, cwd=w / 'engine', env=env, stdout=out, stderr=subprocess.STDOUT).returncode
        rec['endedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat(); return rec
    with concurrent.futures.ThreadPoolExecutor(max_workers=workers) as pool:
        for rec in pool.map(task, seeds):
            records.append(rec); print(json.dumps({k: rec[k] for k in ['seed', 'exitCode', 'startedUtc', 'endedUtc']}), flush=True)
            (w / 'runs.json').write_text(json.dumps(records, indent=2) + '\n')
    bad = [r for r in records if r['exitCode']]
    if bad: raise RuntimeError(f'failed runs: {bad}')

if __name__ == '__main__':
    p = argparse.ArgumentParser(); p.add_argument('--head', required=True); p.add_argument('--work-dir', type=Path, required=True)
    p.add_argument('--workers', type=int, default=3); p.add_argument('--seeds', nargs='*'); a = p.parse_args()
    if not (a.work_dir / 'head.txt').exists(): prepare(a.head, a.work_dir)
    run(a.work_dir, a.seeds or SEEDS, a.workers)
