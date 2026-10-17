#!/usr/bin/env python3
"""Created 2026-10-15. Validate supplemental per-keeper census against frozen pooled outcomes."""
import csv
import json
import lzma
import statistics
from collections import Counter
from pathlib import Path
import compare_w8


def read(path):
    path=Path(path)
    if not path.exists():path=Path(str(path)+'.xz')
    return lzma.open(path,'rt').read() if path.suffix=='.xz' else path.read_text()


def parse(path):
    text=read(path);start=text.index('=== W8 supplemental per-keeper pass outcomes ===')
    rows=[];ends={}
    for line in text[start:].splitlines():
        line=line.strip()
        if not line.startswith(('keeperPass,','keeperPassEnd,')):continue
        cells=next(csv.reader([line]));row={'seed':cells[1]}
        row.update(cell.split('=',1) for cell in cells[2:])
        if cells[0]=='keeperPass':
            if row['reason']=='censored-at-end':row['reason']='censored-at-fulltime'
            rows.append(row)
        else:
            ends[row['seed']]=row['digest']
            if len(ends)==6:break
    assert set(ends)==set(compare_w8.SEEDS)
    assert len({(r['seed'],r['keeper'],r['contactFrame']) for r in rows})==len(rows)
    return rows,ends


def write_csv(path,rows):
    with path.open('w',newline='') as file:
        writer=csv.DictWriter(file,fieldnames=list(rows[0]),lineterminator="\n");writer.writeheader();writer.writerows(rows)


def main():
    work=Path(__file__).resolve().parent
    corrected=work/'final-arm'
    if not corrected.is_dir():corrected=work/'corrected-arm'
    records=[];validation={};raw=[]
    for arm,path,common in [
        ('A',work/'a-per-keeper-output.txt',work/'prewire-output.txt'),
        ('B',work/'b-per-keeper-output.txt',corrected/'b-final-common-output.txt')]:
        rows,ends=parse(path);frozen=compare_w8.report(common)
        assert ends=={s:frozen['ends'][s]['digest'] for s in compare_w8.SEEDS},(arm,'terminal digest agreement')
        for seed in compare_w8.SEEDS:
            for kind in ['hand','feet']:
                actual=Counter(r['reason'] for r in rows if r['seed']==seed and r['kind']==kind)
                prefix='pass-outcome-'+kind+'-'
                expected={key[len(prefix):]:v for (s,key),v in frozen['counts'].items()
                    if s==seed and key.startswith(prefix) and not key.endswith(('claim-same-keeper','claim-opponent-keeper'))}
                assert dict(actual)==expected,(arm,seed,kind,dict(actual),expected)
                for target in ['receiver','zone']:
                    n=sum(r['seed']==seed and r['kind']==kind and (int(r['receiver'])<0)==(target=='zone') for r in rows)
                    assert n==frozen['counts'].get((seed,'pass-contact-'+kind+'-'+target),0)
            for reclaim in ['same','opponent']:
                n=sum(r['seed']==seed and r['kind']=='hand' and r['reclaim']==reclaim for r in rows)
                assert n==frozen['counts'].get((seed,'pass-outcome-hand-interrupted-by-claim-'+reclaim+'-keeper'),0)
            for keeper in ['0','11']:
                for kind in ['hand','feet']:
                    selected=[r for r in rows if r['seed']==seed and r['keeper']==keeper and r['kind']==kind]
                    metrics=next(r for r in compare_w8.keeper_metrics(frozen,arm) if r['seed']==seed and r['keeper']==keeper)
                    episodes=[r for r in frozen['episodes'] if r['seed']==seed and r['keeper']==keeper and r['kind']==kind]
                    durations=[int(r['durationFrames']) for r in episodes]
                    records.append({**metrics,'kind':kind,'episodeCount':len(episodes),
                        'episodeMinFrames':min(durations,default=0),'episodeMaxFrames':max(durations,default=0),
                        'episodeMeanFrames':statistics.mean(durations) if durations else 0,
                        'episodeMedianFrames':statistics.median(durations) if durations else 0,
                        'episodeEndReasons':json.dumps(dict(sorted(Counter(r['endReason'] for r in episodes).items())),sort_keys=True),
                        'lastDecisionLabels':json.dumps(dict(sorted(Counter(r['lastDecision'] for r in episodes).items())),sort_keys=True),
                        'passContacts':len(selected),
                        'receiverContacts':sum(int(r['receiver'])>=0 for r in selected),
                        'zoneContacts':sum(int(r['receiver'])<0 for r in selected),
                        'passOutcomes':json.dumps(dict(sorted(Counter(r['reason'] for r in selected).items())),sort_keys=True),
                        'sameKeeperHandReclaims':sum(r['reclaim']=='same' for r in selected),
                        'opponentKeeperHandReclaims':sum(r['reclaim']=='opponent' for r in selected)})
        validation[arm]={'terminalDigestsAgree':6,'perSeedHandAndFeetOutcomeCountersAgree':12,'passContacts':len(rows)}
        raw.extend({'arm':arm,**r} for r in rows)
    write_csv(work/'keeper-outcome-comparison.csv',records)
    write_csv(work/'keeper-pass-outcomes.csv',raw)
    (work/'keeper-outcome-validation.json').write_text(json.dumps(validation,indent=2)+'\n')
    print(json.dumps(validation,indent=2))


if __name__=='__main__':main()
