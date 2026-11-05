"""Created 2026-11-04: format a passing replay into a new scratch review bundle.

Historical one-off roadmap mutation is archived in as-run-v1.tar.xz. This command
does not edit repository records, change the frozen capture, or publish anything.
"""
import argparse
import csv
import json
from pathlib import Path

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--analysis-dir', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args(argv)
    report = json.loads((args.analysis_dir / 'analysis.json').read_text())
    windows = json.loads((args.analysis_dir / 'windows.json').read_text())
    if report.get('status') != 'PASS' or report.get('passed') is not True:
        parser.error('A complete PASS with digest chains checked is required')
    target = args.output_dir.resolve()
    if target.exists() and any(target.iterdir()): parser.error('--output-dir must be empty')
    target.mkdir(parents=True, exist_ok=True)
    with (target / 'per-match.csv').open('w', newline='') as output:
        writer = csv.writer(output, lineterminator='\n')
        writer.writerow(['seed','adopted_digest','plain_digest','observed_digest','digest_chain_equal',
                         'ball_stream_equal','ledger_equal','goals','shots','distributions'])
        for item in report['seeds']:
            p,o,s = item['plain'],item['observed'],item['streams']
            writer.writerow([item['seed'],item['adoptedDigest'],p['digest'],o['digest'],s['digests.bin']['equal'],
                             s['trajectory.bin']['equal'],s['ledger.bin']['equal'],o['goals'],o['shots'],o['distributions']])
    with (target / 'selected-windows.csv').open('w', newline='') as output:
        writer = csv.writer(output, lineterminator='\n')
        writer.writerow(['seed','keeper','team','release_frame','release_order','first_opponent_touch_frame',
                         'touch_order','touch_kind','toucher','delay_frames','complete',
                         'own_shots','opponent_shots','own_goals','opponent_goals'])
        for item in windows:
            if not item['selected']: continue
            r,t = item['release'],item['firstOtherTouch']
            counts = [sum(e['kind']==kind and e['side']==side for e in item['events'])
                      for kind in ('shot','goal') for side in ('releasing-team','opponent')]
            writer.writerow([item['seed'],r['agent'],r['team'],r['frame'],r['order'],t['frame'],t['order'],
                             t['kind'],t['agent'],item['delayFrames'],item['complete'],*counts])
    (target / 'summary.md').write_text(
        '# Pre-C replay\n\nStatus: PASS. Rule version ' + report['ruleVersion'] + '.\n\n'
        + str(len(report['seeds'])) + ' paired seeds; all digest, ball and ledger streams match.\n')
    print('Wrote review bundle to', target)
    return 0

if __name__ == '__main__': raise SystemExit(main())
