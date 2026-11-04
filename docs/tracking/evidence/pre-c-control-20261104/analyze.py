"""Created 2026-11-04: exact-touch selection and paired Pre-C evidence checks."""
from pathlib import Path
from collections import Counter
import base64, hashlib, json, re, struct, sys

HERE = Path(__file__).resolve().parent
SEEDS = ['0F1E2D3C4B5A6978','00000000D1A6D05E','0000000000000001','00000000ABCDEF12','0000000099887766','000000005A5A5A5A']
FRAMES = 324000
def key(row): return row['frame'], row['order']
def sha(path):
    digest=hashlib.sha256()
    with path.open('rb') as f:
        while block:=f.read(1024*1024): digest.update(block)
    return digest.hexdigest()

def windows(rows, frames):
    rows=sorted(rows,key=key)
    events=[r for r in rows if r['kind'] in ('shot','goal')]
    releases=[]
    for release in [r for r in rows if r['kind']=='release']:
        item={'release':release,'ownTouches':[],'firstOtherTouch':None,'selected':False,'reason':'run-end','events':[]}
        for row in rows:
            if key(row)<=key(release): continue
            if row['kind']=='restriction-end:restart': item['reason']='restart';break
            if not row['kind'].startswith('touch:'): continue
            if row['agent']==release['agent']: item['ownTouches'].append(row);continue
            item['firstOtherTouch']=row
            delay=row['frame']-release['frame']
            item['delayFrames']=delay
            if row['team']==release['team']: item['reason']='teammate-first'
            elif delay>507: item['reason']='opponent-after-507'
            else:
                item.update(selected=True,reason='opponent-first-within-507',complete=row['frame']+600<=frames,
                            observedFollowingFrames=min(600,frames-row['frame']))
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

def main():
    results=HERE/'results'; report={'seeds':[],'passed':True}
    expected=json.loads((HERE/'expected.json').read_text())
    all_windows=[]; counts=Counter(); total_goals=0
    for seed in SEEDS:
        plain,observed=results/'plain'/seed,results/'observed'/seed
        p=json.loads((plain/'summary.json').read_text());o=json.loads((observed/'summary.json').read_text())
        entry={'seed':seed,'plain':p,'observed':o,'adoptedDigest':expected[seed],'streams':{}}
        passed=all(x['frames']==FRAMES and x['digest']==expected[seed] for x in (p,o))
        for filename,width in [('digests.bin',32),('trajectory.bin',68),('ledger.bin',None)]:
            diff=first_difference(plain/filename,observed/filename,width)
            entry['streams'][filename]={'equal':diff is None,'firstDifference':diff,'sha256':sha(plain/filename),'observedSha256':sha(observed/filename)}
            passed &= diff is None
        assert (plain/'digests.bin').stat().st_size==FRAMES*32
        assert (observed/'trajectory.bin').stat().st_size==FRAMES*68
        rows=[json.loads(line) for line in (observed/'observations.jsonl').read_text().splitlines()]
        entry['publishLedgerRecordsVerified']=verify_published_ledger(rows,observed/'ledger.bin',FRAMES)
        keys=[key(r) for r in rows]
        assert keys==sorted(set(keys)), 'duplicate or unordered observer sequence'
        kind_counts=Counter(r['kind'] for r in rows);counts.update(kind_counts)
        assert kind_counts['goal']==o['goals'] and kind_counts['shot']==o['shots']
        assert kind_counts['release']==o['distributions'], 'release/distribution disagreement'
        for row in rows:
            if row['kind']=='shot': assert row['team'] in (0,1) and row['agent']>=0
            if row['kind']=='goal': assert (row['team'] in (0,1) and row['agent']>=0) or (row['team']==-1 and row['agent']==-1)
            if row['kind']=='goal': assert row['scoringTeam'] in (0,1)
        selected=windows(rows,FRAMES)
        for window in selected: window['seed']=seed
        all_windows.extend(selected)
        entry['touchCounts']=dict(kind_counts);entry['passed']=passed
        report['seeds'].append(entry);report['passed'] &= passed
        total_goals+=o['goals']
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
                  corpusGoals=total_goals,simulatedFrames=6*FRAMES,corpusGoalsPer600Frames=total_goals/(6*FRAMES/600))
    (HERE/'analysis.json').write_text(json.dumps(report,indent=2)+'\n')
    (HERE/'windows.json').write_text(json.dumps(all_windows,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k!='seeds'},indent=2))
    if not report['passed']: raise SystemExit('Pre-C control FAILED; C interpretation blocked')
if __name__=='__main__': main()
