"""Created 2026-11-04: reproduce the pre-C review decisions from retained data.

This checks the B control only. It does not apply C changes or infer causality.
"""
import argparse
import base64
import json
import math
from pathlib import Path
import struct
import tarfile

from analyze import HERE, key, sha

POLICY_INPUTS = ['PROTOCOL.md', 'analyze.py', 'review.py', 'test_selection.py',
                 'test_evidence_checks.py', 'Program.cs.txt', 'FrozenW8.cs.txt',
                 'observer-only.patch', 'expected.json']

def projection(window):
    touch = window['firstOtherTouch']
    return {'seed':window['seed'], 'release':key(window['release']),
            'touch':None if touch is None else (key(touch), touch['agent'], touch['team'], touch['kind']),
            'selected':window['selected'], 'reason':window['reason'],
            'events':[(key(e),e['kind'],e['side'],e['raw']) for e in window['events']]}

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--results', type=Path, required=True)
    parser.add_argument('--analysis-dir', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args(argv)
    report = json.loads((args.analysis_dir / 'analysis.json').read_text())
    windows = json.loads((args.analysis_dir / 'windows.json').read_text())
    if report['status'] != 'PASS' or report['passed'] is not True:
        parser.error('Full digest-inclusive PASS is required')
    with tarfile.open(HERE / 'as-run-v1.tar.xz') as archive:
        original_windows = json.load(archive.extractfile('as-run-v1/windows.json'))
        original_analysis = json.load(archive.extractfile('as-run-v1/analysis.json'))
    assert [projection(w) for w in windows] == [projection(w) for w in original_windows]
    for before, after in zip(original_analysis['seeds'], report['seeds']):
        assert before['seed'] == after['seed']
        for filename in ('digests.bin', 'trajectory.bin', 'ledger.bin'):
            for field in ('equal', 'sha256', 'observedSha256', 'firstDifference'):
                assert before['streams'][filename][field] == after['streams'][filename][field]
    same_frame = []
    selected = [w for w in windows if w['selected']]
    zero = [w for w in selected if w['delayFrames'] == 0]
    assert [(w['seed'],w['release']['frame']) for w in zero] == [
        ('00000000ABCDEF12',24126),('00000000ABCDEF12',24378)]
    rows = [json.loads(line) for line in
            (args.results / 'observed/00000000ABCDEF12/observations.jsonl').read_text().splitlines()]
    for window in zero:
        release, touch = window['release'],window['firstOtherTouch']
        frame_rows = [r for r in rows if r['frame'] == release['frame']]
        launch = next(r for r in frame_rows if r.get('ordinal') == 12)
        distribution = next(r for r in frame_rows if r.get('ordinal') == 22)
        clear = next(r for r in frame_rows if r['kind'] == 'restriction-clear:touch')
        possession = next(r for r in frame_rows if r.get('ordinal') == 4)
        # PassAttemptEvent: 12-byte header, four int fields, then TargetPosition;
        # FinalVelocity begins at offset 40 in its pinned sequential v1 layout.
        velocity = struct.unpack_from('<3f',base64.b64decode(launch['raw']),40)
        delta = [touch['position'][c]-touch['contact'][c] for c in ('x','y')]
        distance = math.hypot(*delta)
        approach = sum(velocity[i]*delta[i] for i in range(2))
        assert distance < 1.0 and approach > 0 and math.hypot(*velocity[:2]) >= 0.5
        assert release['order'] < launch['order'] < distribution['order'] < touch['order'] < clear['order']
        assert clear['restriction'] == -1 and touch['agent'] == 10 and release['agent'] == 11
        assert not window['events']
        same_frame.append({'seed':window['seed'],'frame':release['frame'],
            'releaseOrder':release['order'],'passEventOrder':launch['order'],
            'distributionEventOrder':distribution['order'],'touchOrder':touch['order'],
            'restrictionClearOrder':clear['order'],'releasingKeeper':11,'toucher':10,
            'distanceM':distance,'acceptanceRadiusM':1.0,'passVelocity':velocity,
            'approachDot':approach,'minimumSpeedMps':0.5,
            'finalPossessor':struct.unpack_from('<i',base64.b64decode(possession['raw']),16)[0],
            'shotsInWindow':0,'goalsInWindow':0,'decision':'retain: executor CONTACT precedes approaching first touch in Resolve'})
    coverage = report['observationCounts']
    result = {'ruleVersion':report['ruleVersion'],'releaseDecisionsUnchanged':len(windows),
              'selectedWindowsUnchanged':len(selected),'allRecordedStreamHashesUnchanged':True,
              'abstractLooseTackleOutcomesExcluded':report['excludedAbstractTackleOutcomes'],
              'appliedTouches':report['appliedTouchCount'],'sameFrameCases':same_frame,
              'selectedFirstOtherKinds':{kind:sum(w['firstOtherTouch']['kind']==kind for w in selected)
                  for kind in sorted({w['firstOtherTouch']['kind'] for w in selected})},
              'coverageLimits':{'headerContacts':coverage.get('touch:header-contact',0),
                  'handAttempts':coverage['hand-attempt'],'appliedHandContacts':coverage['touch:hand-contact'],
                  'rejectedHandAttempts':coverage['hand-attempt']-coverage['touch:hand-contact'],
                  'interpretation':'Header and rejected-hand physical producer paths are not exercised by this corpus; unit checks are separate.'}}
    policy = {'ruleVersion':report['ruleVersion'],'date':'2026-11-04',
              'sourceCommit':json.loads((HERE/'freeze.json').read_text())['sourceCommit'],
              'asRunFreezeSha256':sha(HERE/'freeze.json'),
              'captureArchiveSha256':sha(HERE/'capture-traces.tar.xz'),
              'inputs':{name:sha(HERE/name) for name in POLICY_INPUTS},
              'scope':'Pre-C analysis amendment before any C capture; raw instrument and captured games unchanged.'}
    declared = HERE/'policy-freeze.json'
    if declared.exists(): assert json.loads(declared.read_text()) == policy, 'Policy freeze drift'
    args.output_dir.mkdir(parents=True,exist_ok=True)
    (args.output_dir/'review.json').write_text(json.dumps(result,indent=2)+'\n')
    (args.output_dir/'policy-freeze.json').write_text(json.dumps(policy,indent=2)+'\n')
    print(json.dumps(result,indent=2))
    return 0

if __name__ == '__main__': raise SystemExit(main())
