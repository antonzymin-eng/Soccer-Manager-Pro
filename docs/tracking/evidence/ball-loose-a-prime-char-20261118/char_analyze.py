"""2026-11-18: descriptive D1 before/after report for A' (#440). Each folder first passes Stage A's
independent audit (analyze.audit, unchanged); time-to-first-regain bins follow w-440 design
"Reported time to original-carrier regain" and "Denominators, overlap and censoring". Nothing gates."""
import collections, csv, importlib.util, json, math, sys
from pathlib import Path

REPO = next(p for p in Path(__file__).resolve().parents if (p / '.git').exists())
spec = importlib.util.spec_from_file_location('stage_a_analyze', REPO / 'docs/tracking/evidence/ball-loose-stage-a-20261110/analyze.py')
A = importlib.util.module_from_spec(spec); spec.loader.exec_module(A)
BINS = ['f', 'f+1..3', 'f+4..12', 'f+13..24', 'f+25..60', 'complete-without-regain', 'censored-before-regain']
HORIZONS = [3, 24, 60]

def bin_of(rel):
    return 'f' if rel == 0 else 'f+1..3' if rel <= 3 else 'f+4..12' if rel <= 12 else 'f+13..24' if rel <= 24 else 'f+25..60'

def episodes(folder):
    summary, census, rows = A.audit(folder)  # Stage A's full consistency audit, unchanged
    journal = [json.loads(l) for l in (folder / 'chronology.jsonl').read_text().splitlines()]
    raw = [json.loads(l) for l in (folder / 'episodes.jsonl').read_text().splitlines()]
    owner = A.owners(folder)
    starts = [r for r in journal if r['kind'] == 'Tackle' and r['detail'] == 2]
    out = []
    for start, e, row in zip(starts, raw, rows):
        f = start['frame']; horizon = f + 60; carrier = start['other']; tackler = start['actor']
        later = [r for r in journal if A.key(r) > A.key(start) and r['frame'] <= horizon]
        bounds = [r for r in later if r['kind'] == 'Tackle' or r['kind'] in A.CENSORS]
        b = bounds[0] if bounds else None
        censor_frame = b['frame'] if b else (A.FRAMES if horizon > A.FRAMES else None)
        cutoff = A.key(b) if b else (min(horizon, A.FRAMES), float('inf'))
        owned = [r for r in later if A.key(r) < cutoff and r['kind'] == 'Possession']
        first = next((r for r in owned if r['next'] == carrier), None)
        rel = first['frame'] - f if first else None
        reason = row['censorReason']
        bucket = bin_of(rel) if first else ('censored-before-regain' if reason else 'complete-without-regain')
        v = e['frameEndVelocity']; speed = math.hypot(v['x'], v['y'])
        out.append({**row, 'firstRegainRelFrame': rel, 'regainBin': bucket,
                    'entryHolding': owner[f - 1] == carrier,
                    'tacklerAcquired': any(r['next'] == tackler for r in owned),
                    'censorFrameRel': (censor_frame - f) if censor_frame is not None and reason else None,
                    'frameEndBallSpeed': round(speed, 4), 'releaseVelocityAtTackleEventZero': e['initialVelocity']['x'] == 0 and e['initialVelocity']['y'] == 0})
    return summary, out

def cumulative(eps, h):
    n = len(eps)
    k = sum(1 for e in eps if e['firstRegainRelFrame'] is not None and e['firstRegainRelFrame'] <= h)
    u = sum(1 for e in eps if (e['firstRegainRelFrame'] is None or e['firstRegainRelFrame'] > h)
            and e['censorFrameRel'] is not None and e['censorFrameRel'] <= h)
    return {'K': k, 'U': u, 'N': n, 'bounds': [round(k / n, 3), round((k + u) / n, 3)] if n else None}

def report(eps):
    sp = sorted(e['frameEndBallSpeed'] for e in eps)
    return {'N': len(eps),
            'sameFrameCarrierRegain': sum(e['sameFrameCarrierRegain'] for e in eps),
            'entryHoldingAtF+1': sum(e['entryHolding'] for e in eps),
            'regainBins': {b: sum(e['regainBin'] == b for e in eps) for b in BINS},
            'cumulative': {f'f+{h}': cumulative(eps, h) for h in HORIZONS},
            'endpointAtF+60': dict(collections.Counter(e['outcome'] for e in eps)),
            'censorReasons': dict(collections.Counter(e['censorReason'] for e in eps if e['censorReason'])),
            'tacklerAcquiredInWindow': sum(e['tacklerAcquired'] for e in eps),
            'opponentRecovery': sum(e['opponentRecovery'] for e in eps),
            'teammateRecovery': sum(e['teammateRecovery'] for e in eps),
            'frameEndBallSpeedMS': {'zero': sum(s == 0 for s in sp), 'min': sp[0] if sp else None,
                               'median': sp[len(sp) // 2] if sp else None, 'max': sp[-1] if sp else None}}

def arm(work):
    allep, matches = [], []
    for folder in sorted((work / 'results/observed').iterdir()):
        summary, eps = episodes(folder); allep += eps
        matches.append({'seed': summary['seed'], 'ballLoose': len(eps), 'sameFrameRegain': sum(e['sameFrameCarrierRegain'] for e in eps),
                        'shots': summary['shots'], 'goals': summary['goals'], 'fouls': summary['fouls'],
                        'bins': {b: sum(e['regainBin'] == b for e in eps) for b in BINS}})
    return {'pooled': report(allep), 'byCarrierTeam': {t: report([e for e in allep if e['carrierTeam'] == t]) for t in (0, 1)},
            'perSeed': matches}, allep

if __name__ == '__main__':
    base = Path(sys.argv[1]); res = {}
    for name in ['before', 'after']:
        r, eps = arm(base / name); res[name] = r
        res[name]['head'] = (base / name / 'head.txt').read_text().strip()
        with (base / f'episodes-{name}.csv').open('w', newline='') as f:
            w = csv.DictWriter(f, fieldnames=eps[0].keys()); w.writeheader(); w.writerows(eps)
    (base / 'characterization.json').write_text(json.dumps(res, indent=2) + '\n')
    print(json.dumps({k: {'head': v['head'], 'pooled': v['pooled']} for k, v in res.items()}, indent=1))
    for k, v in res.items():
        print(k, [(m['seed'], m['ballLoose'], m['sameFrameRegain'], m['goals'], m['fouls']) for m in v['perSeed']])
