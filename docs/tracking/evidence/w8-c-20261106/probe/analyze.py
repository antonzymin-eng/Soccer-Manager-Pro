from pathlib import Path
import csv,json,struct,hashlib,re
root=Path(__file__).resolve().parents[1]
seeds=['0F1E2D3C4B5A6978','00000000D1A6D05E','0000000000000001','00000000ABCDEF12','0000000099887766','000000005A5A5A5A']
expected=dict(re.findall(r'\| `([A-F0-9]{16})` \| `([a-f0-9]{64})`', (root.parents[1]/'docs/tracking/w8-b-implementation-results.md').read_text()))
summaryB=list(csv.DictReader((root/'measurement-B/summary.csv').open()))
assert len(summaryB)==6, 'B capture incomplete'
assert all(expected[r['seed']]==r['snapshotDigest'] for r in summaryB),'B fingerprints do not reproduce; do not interpret C'
summaryC=list(csv.DictReader((root/'measurement-C/summary.csv').open()))
assert len(summaryC)==6,'C capture incomplete'
out=root/'analysis';out.mkdir(exist_ok=True)
result={'B_adopted_fingerprints_reproduced':6,'traceRecordBytes':77,'comparison':[],'arms':{}}
for seed in seeds:
 a=root/f'measurement-B/{seed}.trace';b=root/f'measurement-C/{seed}.trace'
 assert a.stat().st_size==b.stat().st_size==324000*77
 first=None;offset=0
 with a.open('rb') as fa,b.open('rb') as fb:
  while True:
   ba=fa.read(77*4096);bb=fb.read(77*4096)
   if not ba:break
   if ba!=bb:
    idx=next(i for i,(x,y) in enumerate(zip(ba,bb)) if x!=y)
    pos=offset+idx;frame=pos//77+1;field=pos%77
    first={'frame':frame,'byteWithinRecord':field,'field':'events' if field>=45 else 'frame' if field<4 else 'holder' if field<8 else 'ballState' if field==8 else 'ballKinematics'};break
   offset+=len(ba)
 result['comparison'].append({'seed':seed,'frames':324000,'firstDivergence':first,'B_traceSHA256':hashlib.sha256(a.read_bytes()).hexdigest(),'C_traceSHA256':hashlib.sha256(b.read_bytes()).hexdigest()})
for arm in ['B','C']:
 windows=[];members=[];canonicalReleases=[];allgoals=0;allshots=0;releasesN=0;touchN=0;unique={'ownShots':set(),'opponentShots':set(),'ownGoals':set(),'opponentGoals':set()}
 for seed in seeds:
  raw=list(csv.DictReader((root/f'measurement-{arm}/{seed}-raw.csv').open()))
  for r in raw:
   for k in ['frame','order','agent','team','scoringTeam','restrictionKeeper']:r[k]=int(r[k])
  touchkinds={'body','first-touch','hand','pass','shot-touch','heading-or-gk','acquire'}
  touches=[];lastphysical=None;effectConsumed=False
  for r in raw:
   if r['kind'] not in touchkinds:continue
   same=lastphysical is not None and (r['frame'],r['agent'])==(lastphysical['frame'],lastphysical['agent'])
   duplicate=same and not effectConsumed and ((r['kind']=='acquire' and lastphysical['kind'] in {'hand','first-touch'}) or (r['kind']=='heading-or-gk' and lastphysical['kind']=='hand'))
   if duplicate:
    # A contact is consumed once: a parry response followed by loose pickup is two contacts.
    effectConsumed=True
    continue
   touches.append(r);lastphysical=r;effectConsumed=False
  touchN+=len(touches)
  # The frozen capture marker follows an unbraced keeper-state if and therefore emits
  # for every keeper kick. Join to unchanged frozen-W8 hand-episode end identities;
  # this restores the preregistered canonical hand-release population without rerunning
  # or changing either capture. Preserve the rejected superset rows for audit.
  frozen=list(csv.reader((root/f'measurement-{arm}/{seed}-frozen-w8.txt').open()))
  handEnds={(int(r[1]),int(r[5])) for r in frozen if len(r)>7 and r[0].startswith('0x') and r[3]=='hand' and r[7] in {'PassKick','ShotKick','GkHeadingKick','KickRelease'}}
  releaseMarkers=[r for r in raw if r['kind']=='release']
  releases=[r for r in releaseMarkers if (r['agent'],r['frame']) in handEnds]
  assert len(releases)==len(handEnds),(arm,seed,'missing/duplicate canonical hand-release marker')
  assert all(r['restrictionKeeper']==r['agent'] for r in releases)
  excluded=[r for r in releaseMarkers if (r['agent'],r['frame']) not in handEnds]
  with (out/f'{arm}-{seed}-excluded-release-markers.csv').open('w') as f:
   writer=csv.DictWriter(f,fieldnames=list(raw[0]),lineterminator="\n");writer.writeheader();writer.writerows(excluded)
  releasesN+=len(releases)
  canonicalReleases.extend({"seed":seed,**r} for r in releases)
  canonical=list(csv.DictReader((root/f'measurement-{arm}/{seed}-events.csv').open()))
  allgoals+=sum(e['kind']=='goal' for e in canonical);allshots+=sum(e['kind']=='shot' for e in canonical)
  for e in canonical:
   for k in ['frame','ledgerIndex','agent','creditedTeam','scoringTeam']:e[k]=int(e[k])
   mark=[r for r in raw if r['kind']==e['kind'] and r['frame']==e['frame'] and r['agent']==e['agent']]
   assert len(mark)==1,(arm,seed,e,'canonical source join not unique')
   e['order']=mark[0]['order'];e['eventId']=f"{seed}:{e['frame']}:{e['ledgerIndex']}:{e['kind']}"
   if e['kind']=='shot':e['x'],e['y'],e['z']=[mark[0][v] for v in ['x','y','z']]
   else:assert e['scoringTeam']==mark[0]['scoringTeam']
  for release in releases:
   key=lambda r:(r['frame'],r['order'])
   restart=next((r for r in raw if r['kind']=='restart' and key(r)>key(release)),None)
   stop=min(key(restart) if restart else (324000,-1),(324000,-1))
   other=next((t for t in touches if key(release)<key(t)<stop and t['agent']!=release['agent']),None)
   if other is None or other['team']==release['team'] or other['frame']-release['frame']>507:continue
   wid=f"{arm}:{seed}:{release['frame']}:{release['order']}:{release['agent']}"
   end=other['frame']+600
   row={'windowId':wid,'seed':seed,'keeper':release['agent'],'keeperTeam':release['team'],'releaseFrame':release['frame'],'releaseOrder':release['order'],'clearingFrame':other['frame'],'clearingOrder':other['order'],'clearingAgent':other['agent'],'clearingKind':other['kind'],'endFrame':min(323999,end),'complete':end<324000,'overlap':False,'ownShots':0,'opponentShots':0,'ownGoals':0,'opponentGoals':0}
   for e in canonical:
    if (e['frame'],e['order']) <= key(other) or e['frame']>end or e['frame']>=324000:continue
    team=e['creditedTeam'] if e['kind']=='shot' else e['scoringTeam']
    group=('own' if team==release['team'] else 'opponent')+('Shots' if e['kind']=='shot' else 'Goals')
    row[group]+=1;unique[group].add(e['eventId']);members.append({'windowId':wid,**e,'relativeTeam':'keeper' if team==release['team'] else 'opponent'})
   windows.append(row)
 for i,w in enumerate(windows):
  w['overlap']=any(i!=j and w['seed']==v['seed'] and max(w['clearingFrame'],v['clearingFrame'])<=min(w['endFrame'],v['endFrame']) for j,v in enumerate(windows))
 stats={'handReleases':releasesN,'physicalTouchRowsAfterDuplicateEffects':touchN,'selectedWindows':len(windows),'completeWindows':sum(w['complete'] for w in windows),'censoredWindows':sum(not w['complete'] for w in windows),'overlappingWindows':sum(w['overlap'] for w in windows),'playedFrames':6*324000,'livePreFreezeFrames':6*323999,'corpusGoalsPer600LiveFrames':allgoals*600/(6*323999),'corpusGoals':allgoals,'corpusShots':allshots,'corpusGoalsPer600Frames':allgoals*600/(6*324000),'uniqueSelectedEvents':{k:len(v) for k,v in unique.items()},'uniqueShots':len(unique['ownShots']|unique['opponentShots']),'uniqueGoals':len(unique['ownGoals']|unique['opponentGoals']),'windowMembershipTotals':{k:sum(w[k] for w in windows) for k in unique},'perWindow':{k:sum(w[k] for w in windows)/len(windows) if windows else None for k in unique}}
 result['arms'][arm]=stats
 for label,rows in [('canonical-releases',canonicalReleases),('windows',windows),('memberships',members)]:
  if rows:
   with (out/f'{arm}-{label}.csv').open('w') as f:
    writer=csv.DictWriter(f,fieldnames=list(rows[0]),lineterminator="\n");writer.writeheader();writer.writerows(rows)
result['ordinaryGameplayAndBallStreamsEqual']=all(x['firstDivergence'] is None for x in result['comparison'])
result['snapshotDigests']={'B':summaryB,'C':summaryC}
(out/'summary.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='snapshotDigests'},indent=2))
