#!/usr/bin/env python3
"""Created 2026-10-15. Compare frozen W8 reports and preserve per-seed/per-keeper outcomes."""
import csv
import io
import json
import lzma
import statistics
import sys
from collections import Counter
from pathlib import Path

SENTINEL = '=== W8 Stage A frozen six-seed baseline ==='
SEEDS = ['0x0F1E2D3C4B5A6978','0x00000000D1A6D05E','0x0000000000000001','0x00000000ABCDEF12','0x0000000099887766','0x000000005A5A5A5A']

def report(path):
    path=Path(path)
    if not path.exists(): path=Path(str(path)+".xz")
    source=lzma.open(path,'rt').read() if path.suffix=='.xz' else path.read_text()
    start=source.index(SENTINEL)
    lines=[]; ends=0
    for line in source[start:].splitlines():
        line=line.strip(); lines.append(line)
        if line.startswith('seedEnd,'):
            ends+=1
            if ends==6:break
    assert ends==6,(path,ends)
    data={'lines':lines,'counts':{},'summary':{},'discipline':{},'ends':{},'exposure':{},'episodes':[]}
    header=next(line for line in lines if line.startswith('seed,keeper,'))
    fields=next(csv.reader([header]))
    for line in lines:
        cells=next(csv.reader([line])) if line else []
        if not cells:continue
        kind=cells[0]
        if kind=='count':data['counts'][tuple(cells[1:3])]=int(cells[3])
        elif kind in ('summary','discipline','seedEnd','exposure'):
            values=dict(cell.split('=',1) for cell in cells[2:])
            if kind=='seedEnd':data['ends'][cells[1]]=values
            elif kind=='exposure':data['exposure'][(cells[1],values['keeper'])]=values
            else:data[kind][cells[1]]=values
        elif kind in SEEDS:data['episodes'].append(dict(zip(fields,cells)))
    assert list(data['ends'])==SEEDS
    return data

def comparable(d):return [line for line in d['lines'] if not line.startswith('seedEnd,')]

def keeper_metrics(d,arm):
    for seed in SEEDS:
        for keeper in ('0','11'):
            rows=[e for e in d['episodes'] if e['seed']==seed and e['keeper']==keeper]
            hands=[e for e in rows if e['kind']=='hand']
            durations=[int(e['durationFrames']) for e in hands]
            ends=Counter(e['endReason'] for e in hands)
            yield {'arm':arm,'seed':seed,'keeper':keeper,'eligibleFrames':d['exposure'][(seed,keeper)]['eligibleFrames'],
                'hands':len(hands),'feet':sum(e['kind']=='feet' for e in rows),
                'holdMin':min(durations,default=0),'holdMax':max(durations,default=0),
                'holdMean':statistics.mean(durations) if durations else 0,
                'holdMedian':statistics.median(durations) if durations else 0,
                'handEndReasons':json.dumps(dict(sorted(ends.items())),sort_keys=True)}

def aggregate(d):
    counts=Counter()
    for (_,key),value in d['counts'].items():counts[key]+=value
    discipline={key:sum(int(d['discipline'][s].get(key,0)) for s in SEEDS) for key in ['fouls','yellows','straightReds','secondYellowDismissals']}
    hands=[row for row in d['episodes'] if row['kind']=='hand']
    durations=[int(row['durationFrames']) for row in hands]
    return {'claims':sum(int(d['summary'][s]['claims']) for s in SEEDS),'counts':dict(sorted(counts.items())),'discipline':discipline,
        'handHoldFrames':{'count':len(durations),'min':min(durations,default=0),'max':max(durations,default=0),
            'mean':statistics.mean(durations) if durations else 0,'median':statistics.median(durations) if durations else 0},
        'handEndReasons':dict(Counter(row['endReason'] for row in hands)),
        'handLastDecisionLabels':dict(Counter(row['lastDecision'] for row in hands)),
        'feetLastDecisionLabels':dict(Counter(row['lastDecision'] for row in d['episodes'] if row['kind']=='feet')),
        'directOwnPassPrecededHandClaims':sum(row['lastTouchAgent']==row['keeper'] and row['lastTouchKind']=='PassKick' for row in hands),
        'terminalDigests':{s:d['ends'][s]['digest'] for s in SEEDS}}

def main():
    work=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parent
    if (work/'corrected-arm').is_dir():work=work/'corrected-arm'
    def locate(name):
        for parent in [work,work.parent]:
            candidate=parent/name
            if candidate.exists() or Path(str(candidate)+'.xz').exists():return candidate
        raise FileNotFoundError(name)
    root=next((parent for parent in Path(__file__).resolve().parents if (parent/'docs/tracking/evidence/pre-b-anchor-3974b78-20261013').is_dir()),None)
    archive=root/'docs/tracking/evidence/pre-b-anchor-3974b78-20261013/anchor-w8-output.txt.xz' if root else locate('anchor-frozen-output.txt')
    a=report(archive);host=report(locate('host-anchor-output.txt'));bridge=report(locate('prewire-output.txt'));b=report(work/'b-final-common-output.txt')
    assert a['lines']==host['lines'],'host does not reproduce frozen anchor byte-for-byte'
    assert comparable(a)==comparable(bridge),'pre-wire bridge changes observed outcome stream'
    assert {seed:{k:v for k,v in a['ends'][seed].items() if k!='digest'} for seed in SEEDS}=={seed:{k:v for k,v in bridge['ends'][seed].items() if k!='digest'} for seed in SEEDS},'pre-wire bridge changes non-digest terminal fields'
    result={'hostAnchorExact':True,'prewireObservedReportExactExcludingDigests':True,'arms':{k:aggregate(v) for k,v in [('anchor',a),('bridge',bridge),('B',b)]}}
    (work/'comparison.json').write_text(json.dumps(result,indent=2)+'\n')
    rows=list(keeper_metrics(a,'anchor'))+list(keeper_metrics(bridge,'bridge'))+list(keeper_metrics(b,'B'))
    with (work/'per-keeper.csv').open('w') as f:
        writer=csv.DictWriter(f,fieldnames=list(rows[0]),lineterminator="\n");writer.writeheader();writer.writerows(rows)
    for arm,data in [('anchor',a),('bridge',bridge),('B',b)]:
        with (work/(arm+'-episodes.csv')).open('w') as f:
            writer=csv.DictWriter(f,fieldnames=list(data['episodes'][0]),lineterminator="\n");writer.writeheader();writer.writerows(data['episodes'])
    print(json.dumps({k:{'claims':v['claims'],'discipline':v['discipline']} for k,v in result['arms'].items()},indent=2))

if __name__=='__main__':main()
