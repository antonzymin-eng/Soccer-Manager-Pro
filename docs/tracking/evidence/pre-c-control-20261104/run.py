"""Created 2026-11-04: run a portable prepared B control, one seed per process."""
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
import datetime
import json
from pathlib import Path
import subprocess

from prepare import HERE, capture_environment, sha, validate_runtime

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--work-dir', type=Path, required=True)
    parser.add_argument('--jobs', type=int, default=2)
    parser.add_argument('--seed', help='Optional single seed from the frozen corpus')
    parser.add_argument('--frames', type=int, default=324000, help='Non-324000 runs are smoke checks only')
    parser.add_argument('--packages', type=Path)
    args = parser.parse_args(argv)
    if args.jobs < 1 or args.frames < 1: parser.error('--jobs and --frames must be positive')
    work = args.work_dir.resolve()
    pin = json.loads((work / 'prepared.json').read_text())
    freeze = json.loads((HERE / 'freeze.json').read_text())
    if not pin['built']: parser.error('Run prepare.py --build first')
    if sha(HERE / 'freeze.json') != pin['asRunFreezeSha256']: raise ValueError('Freeze manifest changed')
    if sha(work / 'source.tar') != pin['sourceArchiveSha256']: raise ValueError('Source archive changed')
    for arm in ('plain', 'observed'):
        for section in ('sources', 'binaries'):
            for name, expected in pin[section][arm].items():
                if sha(work / arm / name) != expected: raise ValueError('Prepared input changed: ' + name)
    env = capture_environment(work, args.packages)
    validate_runtime(pin['dotnet'], work, env, freeze)
    expected = json.loads((HERE / 'expected.json').read_text())
    if args.seed and args.seed not in expected: parser.error('--seed must be a 16-digit seed in expected.json')
    seeds = [args.seed] if args.seed else list(expected)
    category = 'results' if args.frames == 324000 and not args.seed else 'smoke'
    result_root = work / category
    if result_root.exists(): parser.error(str(result_root) + ' already exists; use a new prepared workspace')
    result_root.mkdir()
    records = []
    def run(arm, seed):
        target = result_root / arm / seed
        target.mkdir(parents=True)
        command = [pin['dotnet'], '--fx-version', pin['clr'],
                   str(work / arm / 'capture/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll'),
                   seed, arm, str(target), str(args.frames)]
        started = datetime.datetime.now(datetime.timezone.utc).isoformat()
        with (target / 'runner.log').open('w') as output:
            result = subprocess.run(command, cwd=work / arm, env=env, stdout=output, stderr=subprocess.STDOUT)
        record = {'arm':arm, 'seed':seed, 'command':command, 'startedUtc':started,
                  'endedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(), 'exitCode':result.returncode,
                  'fullPopulation':category == 'results', 'frames':args.frames}
        if result.returncode == 0 and args.frames == 324000:
            summary = json.loads((target / 'summary.json').read_text())
            if summary['digest'] != expected[seed]: record['baselineMismatch'] = True
        print(json.dumps(record), flush=True)
        return record
    with ThreadPoolExecutor(max_workers=args.jobs) as pool:
        futures = [pool.submit(run, arm, seed) for seed in seeds for arm in ('plain', 'observed')]
        for task in as_completed(futures):
            records.append(task.result())
            (work / (category + '-runs.json')).write_text(json.dumps(records, indent=2) + '\n')
    if any(r['exitCode'] or r.get('baselineMismatch') for r in records): return 1
    print('Full control captured.' if category == 'results' else 'Smoke only; this is not a six-seed control.')
    return 0

if __name__ == '__main__': raise SystemExit(main())
