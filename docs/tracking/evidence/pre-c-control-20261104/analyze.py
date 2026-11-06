"""Created 2026-11-04: exact-touch selection and paired Pre-C evidence checks."""
from pathlib import Path
from collections import Counter
import argparse, base64, hashlib, json, struct

HERE = Path(__file__).resolve().parent
SEEDS = ['0F1E2D3C4B5A6978','00000000D1A6D05E','0000000000000001','00000000ABCDEF12','0000000099887766','000000005A5A5A5A']
FRAMES = 324000
RULE_VERSION = '1.1'
APPLIED_TOUCH_KINDS = frozenset({
    'touch:pass-contact', 'touch:shot-contact', 'touch:header-contact',
    'touch:first-touch', 'touch:loose-pickup', 'touch:body-applied',
    'touch:hand-contact', 'touch:tackle-won',
})

def applied_touch(row):
    # The historical raw label is preserved. BallLoose only releases control;
    # it does not call the engine's physical-touch bookkeeping or apply contact.
    kind = row['kind']
    if kind == 'touch:tackle-loose': return False
    if kind.startswith('touch:') and kind not in APPLIED_TOUCH_KINDS:
        raise ValueError('Unregistered physical-touch kind: ' + kind)
    return kind in APPLIED_TOUCH_KINDS

def full_time(rows):
    ends = [key(r) for r in rows if r.get('ordinal') == 0x1A
            and base64.b64decode(r['raw'])[12] == 1]
    if len(ends) > 1: raise ValueError('More than one full-time transition')
    return ends[0] if ends else None
def key(row): return row['frame'], row['order']
def sha(path):
    digest=hashlib.sha256()
    with path.open('rb') as f:
        while block:=f.read(1024*1024): digest.update(block)
    return digest.hexdigest()

def windows(rows, frames):
    rows=sorted(rows,key=key)
    ended=full_time(rows)
    live_end=min(frames,ended[0]-1) if ended else frames
    events=[r for r in rows if r['kind'] in ('shot','goal')]
    releases=[]
    for release in [r for r in rows if r['kind']=='release']:
        item={'release':release,'ownTouches':[],'firstOtherTouch':None,'selected':False,'reason':'run-end','events':[]}
        for row in rows:
            if key(row)<=key(release): continue
            if ended and key(row)>=ended: item['reason']='full-time';break
            if row['kind']=='restriction-end:restart': item['reason']='restart';break
            if not applied_touch(row): continue
            if row['agent']==release['agent']: item['ownTouches'].append(row);continue
            item['firstOtherTouch']=row
            delay=row['frame']-release['frame']
            item['delayFrames']=delay
            if row['team']==release['team']: item['reason']='teammate-first'
            elif delay>507: item['reason']='opponent-after-507'
            else:
                item.update(selected=True,reason='opponent-first-within-507',complete=row['frame']+600<=live_end,
                            observedFollowingFrames=min(600,live_end-row['frame']))
                for event in events:
                    if key(event)>key(row) and event['frame']<=row['frame']+600:
                        team=event['scoringTeam'] if event['kind']=='goal' else event['team']
                        item['events'].append(dict(event,side='releasing-team' if team==release['team'] else 'opponent'))
            break
        releases.append(item)
    return releases

def first_difference(a,b,record_size=None):
    offset=0
    with a.open('rb') as fa,b.open('rb') as fb:
        while True:
            x,y=fa.read(1024*1024),fb.read(1024*1024)
            if x!=y:
                index=next((i for i,(p,q) in enumerate(zip(x,y)) if p!=q),min(len(x),len(y)))
                position=offset+index
                return {'byteOffset':position,'frame':position//record_size+1 if record_size else None}
            if not x: return None
            offset+=len(x)

def stream_check(plain, observed, filename, width, frames, skip_digests=False):
    if filename == 'digests.bin' and skip_digests:
        return {'checked':False,'equal':None,'firstDifference':None,
                'sha256':None,'observedSha256':None,'reason':'explicit --skip-digests'}
    a,b=plain/filename,observed/filename
    for path in (a,b):
        if not path.is_file():
            raise FileNotFoundError(f'{path}: extract capture-traces.tar.xz; --skip-digests is partial validation only')
        if width and path.stat().st_size != frames*width:
            raise ValueError(f'{path}: expected {frames*width} bytes')
    diff=first_difference(a,b,width)
    return {'checked':True,'equal':diff is None,'firstDifference':diff,
            'sha256':sha(a),'observedSha256':sha(b)}

def verify_published_ledger(rows,path,frames):
    """Each publish must survive in the separate canonical ledger, exactly once."""
    published=Counter((r['frame'],r['ordinal'],base64.b64decode(r['raw'])) for r in rows if 'raw' in r)
    canonical=Counter()
    with path.open('rb') as f:
        for frame in range(1,frames+1):
            count,=struct.unpack('<i',f.read(4))
            for _ in range(count):
                ordinal=f.read(1)[0];size,=struct.unpack('<i',f.read(4));raw=f.read(size)
                assert len(raw)==size and raw[0]==ordinal and struct.unpack_from('<I',raw,4)[0]==frame
                canonical[frame,ordinal,raw]+=1
        assert not f.read(1),'unexpected trailing ledger bytes'
    assert published==canonical,'publish capture and canonical ledger disagree'
    return sum(canonical.values())

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--results',type=Path,required=True,help='Extracted results directory')
    parser.add_argument('--output-dir',type=Path,required=True,help='New scratch analysis directory')
    parser.add_argument('--skip-digests',action='store_true',help='Validate terminal/ball/event evidence only; status is PARTIAL')
    args=parser.parse_args(argv)
    if args.output_dir.resolve()==HERE: parser.error('write replay output outside committed evidence')
    args.output_dir.mkdir(parents=True,exist_ok=True)
    results=args.results; report={'seeds':[],'passed':True,'ruleVersion':RULE_VERSION,
                                 'digestChainsChecked':not args.skip_digests}
    expected=json.loads((HERE/'expected.json').read_text())
    all_windows=[]; counts=Counter(); total_goals=0; live_frames=0
    for seed in SEEDS:
        plain,observed=results/'plain'/seed,results/'observed'/seed
        p=json.loads((plain/'summary.json').read_text());o=json.loads((observed/'summary.json').read_text())
        entry={'seed':seed,'plain':p,'observed':o,'adoptedDigest':expected[seed],'streams':{}}
        passed=all(x['frames']==FRAMES and x['tick']==FRAMES and x['seed']==seed
                   and x['digest']==expected[seed] for x in (p,o))
        for filename,width in [('digests.bin',32),('trajectory.bin',68),('ledger.bin',None)]:
            check=stream_check(plain,observed,filename,width,FRAMES,args.skip_digests)
            entry['streams'][filename]=check
            if check['checked']: passed &= check['equal']
        if not args.skip_digests:
            for folder,summary in ((plain,p),(observed,o)):
                with (folder/'digests.bin').open('rb') as f:
                    f.seek(-32,2)
                    assert f.read().hex()==summary['digest'], 'terminal summary/digest bytes disagree'
        rows=[json.loads(line) for line in (observed/'observations.jsonl').read_text().splitlines()]
        entry['publishLedgerRecordsVerified']=verify_published_ledger(rows,observed/'ledger.bin',FRAMES)
        keys=[key(r) for r in rows]
        assert keys==sorted(set(keys)), 'duplicate or unordered observer sequence'
        kind_counts=Counter(r['kind'] for r in rows);counts.update(kind_counts)
        assert kind_counts['goal']==o['goals'] and kind_counts['shot']==o['shots']
        assert kind_counts['release']==o['distributions'], 'release/distribution disagreement'
        for row in rows:
            applied_touch(row)  # Fail closed on a new unregistered producer kind.
            if row['kind']=='shot': assert row['team'] in (0,1) and row['agent']>=0
            if row['kind']=='goal': assert (row['team'] in (0,1) and row['agent']>=0) or (row['team']==-1 and row['agent']==-1)
            if row['kind']=='goal': assert row['scoringTeam'] in (0,1)
        selected=windows(rows,FRAMES)
        for window in selected: window['seed']=seed
        all_windows.extend(selected)
        entry['touchCounts']=dict(kind_counts)
        entry['traceChecksPassed']=passed
        entry['passed']=None if args.skip_digests and passed else passed
        report['seeds'].append(entry);report['passed'] &= passed
        total_goals+=o['goals']
        end=full_time(rows)
        live_frames+=end[0]-1 if end else FRAMES
    selected=[w for w in all_windows if w['selected']]
    event_counts=Counter(); unique={}
    for window in selected:
        for event in window['events']:
            event_counts[event['kind']+':'+event['side']]+=1
            identity=(window['seed'],event['frame'],event['order'],event['ordinal'],event['subsystem'],event['draw'])
            unique[identity]=event
    report.update(observationCounts=dict(counts),releaseSelection=dict(Counter(w['reason'] for w in all_windows)),
                  selectedWindows=len(selected),completeWindows=sum(w['complete'] for w in selected),
                  overlappingWindowPairs=sum(1 for i,a in enumerate(selected) for b in selected[i+1:] if a['seed']==b['seed'] and max(key(a['firstOtherTouch']),key(b['firstOtherTouch']))[0]<=min(a['firstOtherTouch']['frame']+600,b['firstOtherTouch']['frame']+600)),
                  perWindowEventCounts=dict(event_counts),uniqueEvents=dict(Counter(e['kind'] for e in unique.values())),
                  corpusGoals=total_goals,simulatedFrames=6*FRAMES,corpusGoalsPer600Frames=total_goals/(6*FRAMES/600),
                  livePreFreezeFrames=live_frames,corpusGoalsPer600LiveFrames=total_goals/(live_frames/600),
                  appliedTouchCount=sum(n for kind,n in counts.items() if kind in APPLIED_TOUCH_KINDS),
                  excludedAbstractTackleOutcomes=counts['touch:tackle-loose'])
    report['traceChecksPassed']=report['passed']
    report['status']='FAIL' if not report['passed'] else 'PARTIAL' if args.skip_digests else 'PASS'
    if args.skip_digests and report['passed']: report['passed']=None
    (args.output_dir/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
    (args.output_dir/'windows.json').write_text(json.dumps(all_windows,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k!='seeds'},indent=2))
    return 0 if report['traceChecksPassed'] else 1
if __name__=='__main__': raise SystemExit(main())
