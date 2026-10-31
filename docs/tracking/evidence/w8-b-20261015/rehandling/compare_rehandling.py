#!/usr/bin/env python3
"""Created 2026-10-16. Reproduce isolated W8 re-handling tables from completed NUnit logs."""
from collections import Counter
from pathlib import Path
import csv
import json
import lzma
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
SEEDS = ('0F1E2D3C4B5A6978', '00000000D1A6D05E', '0000000000000001',
         '00000000ABCDEF12', '0000000099887766', '000000005A5A5A5A')


def read(path):
    return lzma.decompress(path.read_bytes()).decode() if path.suffix == '.xz' else path.read_text()


def records(log):
    rows = []
    for line in log.splitlines():
        parts = line.strip().split(',')
        if len(parts) < 3 or parts[0] not in ('bSummary', 'passOutcome', 'contact', 'handlingSummary', 'handlingAttempt', 'handlingRestart'):
            continue
        fields = dict(part.split('=', 1) for part in parts[2:] if '=' in part)
        rows.append({'record': parts[0], 'seed': parts[1], **fields})
    return rows


def write_csv(name, rows, fields):
    with (ROOT / name).open('w', newline='') as file:
        writer = csv.DictWriter(file, fieldnames=fields, lineterminator='\n')
        writer.writeheader()
        writer.writerows(rows)


def main():
    result = {}
    seed_rows, outcome_rows, attempt_rows, restart_rows = [], [], [], []
    historical = {row['seed']: row for row in csv.DictReader((ROOT.parent / 'corrected-arm/bSummary.csv').open())}
    for arm in ('before', 'after'):
        test = ET.fromstring(read(ROOT / (arm + '-six.xml.xz')))
        assert test.get('failed') == '0' and test.get('passed') == '1', (arm, test.attrib)
        data = records(read(ROOT / (arm + '-six.log.xz')))
        summaries = {row['seed']: row for row in data if row['record'] == 'bSummary' and row['seed'] in SEEDS}
        assert set(summaries) == set(SEEDS), (arm, summaries)
        total = Counter()
        outcomes = Counter()
        handling = {row['seed']: row for row in data if row['record'] == 'handlingSummary' and row['seed'] in SEEDS}
        if arm == 'after':
            assert set(handling) == set(SEEDS)
        for seed in SEEDS:
            summary = summaries[seed]
            passes = [row for row in data if row['record'] == 'passOutcome' and row['seed'] == seed]
            contacts = [row for row in data if row['record'] == 'contact' and row['seed'] == seed]
            assert len(passes) == len(contacts) == int(summary['completed'])
            counts = Counter(row['reason'] for row in passes)
            outcomes.update(counts)
            for key, value in summary.items():
                if key not in ('record', 'seed', 'digest', 'maxOutOfPlayRun', 'maxStationaryRun'):
                    total[key] += int(value)
            law = handling.get(seed)
            row = {'arm': arm, 'seed': seed, 'contacts': summary['completed'],
                   'receiver': counts['receiver-controlled'], 'sameKeeperHands': counts['same-keeper-hand-reclaim'],
                   'sameKeeperFeet': counts['same-keeper-feet-control'],
                   'other': len(passes) - counts['receiver-controlled'] - counts['same-keeper-hand-reclaim'] - counts['same-keeper-feet-control'],
                   'restrictedAttempts': law['attempts'] if law else 'not-instrumented',
                   'sanctions': law['sanctions'] if law else 'not-implemented',
                   'ordinaryFouls': summary['fouls'], 'yellows': summary['yellows'], 'reds': summary['reds'],
                   'secondYellows': summary['secondYellows'], 'digest': summary['digest']}
            seed_rows.append(row)
        for key, count in sorted(outcomes.items()):
            outcome_rows.append({'arm': arm, 'outcome': key, 'count': count})
        for row in data:
            if row['seed'] not in SEEDS:
                continue
            if row['record'] == 'handlingAttempt':
                attempt_rows.append({key: value for key, value in row.items() if key != 'record'})
            if row['record'] == 'handlingRestart':
                restart_rows.append({key: value for key, value in row.items() if key != 'record'})
        result[arm] = {'totals': dict(total), 'outcomes': dict(outcomes),
                       'digests': {seed: summaries[seed]['digest'] for seed in SEEDS},
                       'durationSeconds': test.get('duration'), 'start': test.get('start-time'), 'end': test.get('end-time')}
        if arm == 'before':
            keys = set(next(iter(historical.values()))) - {'seed'}
            result['controlMatchesArchivedCorrectedB'] = all(
                all(summaries[seed][key] == historical[seed][key] for key in keys) for seed in SEEDS)
    result['after']['restrictedAttempts'] = len(attempt_rows)
    result['after']['sanctions'] = len(restart_rows)
    buckets = Counter({'goal-area-line': 0, 'penalty-area': 0, 'outside-penalty-area': 0})
    for row in restart_rows:
        offending_team = 1 - int(row['team'])
        depth = abs(float(row['x']) - (0.0 if offending_team == 0 else 105.0))
        lateral = abs(float(row['y']) - 34.0)
        area = 'outside-penalty-area'
        if depth <= 16.5 and lateral <= 16.5 + 7.32 / 2:
            area = 'goal-area-line' if abs(depth - 5.5) < 0.0001 and lateral <= 5.5 + 7.32 / 2 else 'penalty-area'
        row['takenArea'] = area
        buckets[area] += 1
    result['after']['restartLocations'] = dict(buckets)
    result['after']['shotsWithin10s'] = sum(int(row['shots10s']) for row in restart_rows)
    result['after']['goalsWithin10s'] = sum(int(row['goals10s']) for row in restart_rows)
    result['after']['sanctionsPerMatch'] = len(restart_rows) / len(SEEDS)
    result['after']['restartFollowupPopulation'] = len(restart_rows)
    law_rows = [row for row in seed_rows if row['arm'] == 'after']
    assert sum(int(row['restrictedAttempts']) for row in law_rows) == len(attempt_rows)
    assert sum(int(row['sanctions']) for row in law_rows) == len(restart_rows)
    result['fingerprintsChanged'] = sum(result['before']['digests'][seed] != result['after']['digests'][seed] for seed in SEEDS)
    (ROOT / 'comparison.json').write_text(json.dumps(result, indent=2) + '\n')
    write_csv('per-match.csv', seed_rows, list(seed_rows[0]))
    write_csv('distribution-outcomes.csv', outcome_rows, ['arm', 'outcome', 'count'])
    write_csv('restricted-attempts.csv', attempt_rows, ['seed', 'frame', 'keeper', 'x', 'y', 'z'])
    write_csv('handling-restarts.csv', restart_rows, ['seed', 'frame', 'kind', 'team', 'x', 'y', 'shots10s', 'goals10s', 'followedFrames', 'takenArea'])
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
