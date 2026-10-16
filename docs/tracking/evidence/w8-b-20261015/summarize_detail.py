#!/usr/bin/env python3
"""Created 2026-10-15. Validate B census joins and export receiver/zone and landing slices."""
import csv
import json
import lzma
import math
import re
import sys
from collections import Counter
from pathlib import Path
import compare_w8


def source(path):
    path=Path(path)
    if not path.exists():path=Path(str(path)+'.xz')
    return lzma.open(path,'rt').read() if path.suffix=='.xz' else path.read_text()


def detail(path):
    text=source(path)
    text=text[text.index('=== W8 B frozen corpus and landing evidence ==='):]
    rows={k:[] for k in ('commit','contact','landing','passOutcome','terminal','pending','bSummary')}
    seen=set()
    for line in text.splitlines():
        line=line.strip()
        prefix=line.split(',',1)[0]
        if prefix not in rows:continue
        if line in seen:continue
        seen.add(line)
        cells=next(csv.reader([line]));row={'seed':cells[1]}
        row.update(cell.split('=',1) for cell in cells[2:]);rows[prefix].append(row)
    assert len(rows['bSummary'])==8
    return rows


def key(row):return row['seed'],row['keeper'],row.get('contactFrame',row.get('frame'))


def write_csv(path,rows):
    if not rows:return
    columns=list(dict.fromkeys(k for row in rows for k in row))
    with path.open('w',newline='') as file:
        writer=csv.DictWriter(file,fieldnames=columns,lineterminator="\n");writer.writeheader();writer.writerows(rows)


def main():
    work=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parent
    if (work/'corrected-arm').is_dir():work=work/'corrected-arm'
    rows=detail(work/'b-final-detail-v3-output.txt')
    common=compare_w8.report(work/'b-final-common-output.txt')
    contacts={key(row):row for row in rows['contact']}
    landings={key(row):row for row in rows['landing']}
    outcomes={key(row):row for row in rows['passOutcome']}
    assert len(contacts)==len(rows['contact'])
    assert contacts.keys()==landings.keys()==outcomes.keys(),'every CONTACT needs one landing/censor and one outcome/censor'
    for identity,contact in contacts.items():
        assert contact['receiver']==outcomes[identity]['receiver']==landings[identity]['receiver']
        target='zone' if int(contact['receiver'])<0 else 'receiver'
        assert target==outcomes[identity]['targetClass']==landings[identity]['targetClass']
        requested=math.hypot(float(contact['targetX'])-float(contact['sourceX']),float(contact['targetY'])-float(contact['sourceY']))
        assert math.isclose(requested,float(landings[identity]['requestedDistance']),rel_tol=1e-5,abs_tol=1e-4)
    census_summaries={row['seed']:row for row in rows['bSummary']}
    frozen=[]
    for seed in compare_w8.SEEDS:
        token=seed[2:];summary=census_summaries[token];frozen.append(summary)
        assert summary['digest']==common['ends'][seed]['digest'],'independent common/supplemental terminal fingerprints must agree'
        assert summary['completed']==summary['events']==summary['releases']
        assert int(summary['commits'])==int(summary['completed'])+int(summary['cancelled'])+int(summary['pending'])
        assert sum(t['seed']==token for t in rows['terminal'])==int(summary['completed'])+int(summary['cancelled'])+int(summary['rejected'])
        assert int(summary['completed'])==sum(c['seed']==token for c in rows['contact'])
        assert summary['drops']==summary['dtRaces']=='0'
        assert int(summary['maxOutOfPlayRun'])<600
        for common_name,detail_name in [('fouls','fouls'),('yellows','yellows'),('straightReds','reds'),('secondYellowDismissals','secondYellows')]:
            assert common['discipline'][seed][common_name]==summary[detail_name]
    foul=source(work/'b-final-foul-output.txt')
    blocks=re.findall(r'seed (0x[0-9A-F]{16}):\s*(.*?)(?=seed 0x[0-9A-F]{16}:|SOURCE-COMPLETE|AGGREGATE|$)',foul,re.S)
    checked=set()
    for seed,block in blocks:
        if seed in checked:continue
        match=re.search(r'totalFouls=(\d+) yellowCards=(\d+) straightReds=(\d+) secondYellowDismissals=(\d+)',block)
        assert match,(seed,'missing foul report')
        assert list(map(int,match.groups()))==[int(common['discipline'][seed][k]) for k in ['fouls','yellows','straightReds','secondYellowDismissals']]
        checked.add(seed)
    assert checked==set(compare_w8.SEEDS)
    common_hand_contacts={(row['seed'][2:],row['keeper'],row['endFrame']) for row in common['episodes'] if row['kind']=='hand' and row['endReason']=='PassKick'}
    dedicated_contacts={(row['seed'],row['keeper'],row['frame']) for row in rows['contact'] if len(row['seed'])==16}
    assert dedicated_contacts<=common_hand_contacts,'every frozen dedicated CONTACT must join a common hand episode end'
    summary={'independentCommonDigestsAgree':6,'sourceCompleteFoulReportsAgree':6,
        'handEpisodeContactJoins':len(dedicated_contacts),
        'handPassContactsWithoutDedicatedDistribution':len(common_hand_contacts-dedicated_contacts),
        'frozenTotals':{},'outcomes':{},'LongKickLandings':[]}
    for name in ['commits','pending','completed','cancelled','rejected','events','releases','w5Receiver','zones','missing','drops','dtRaces','fouls','yellows','reds','secondYellows']:
        summary['frozenTotals'][name]=sum(int(row[name]) for row in frozen)
    for target in ['receiver','zone']:
        selected=[row for row in rows['passOutcome'] if len(row['seed'])==16 and row['targetClass']==target]
        summary['outcomes'][target]=dict(Counter(row['reason'] for row in selected))
    for row in rows['landing']:
        if row['policy']=='LongKick':summary['LongKickLandings'].append(row)
    (work/'detail-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
    for name,data in rows.items():write_csv(work/(name+'.csv'),data)
    print(json.dumps(summary,indent=2))

if __name__=='__main__':main()
