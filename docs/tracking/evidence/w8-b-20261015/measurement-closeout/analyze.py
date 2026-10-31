from pathlib import Path
import csv,json,statistics,lzma,collections,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parent
expected=list(csv.DictReader((root.parent/'rehandling/per-match.csv').open()))
summary=[];spells=[];issues=[];unique_danger=set();hand_ends=[]
for arm in ['before','after']:
 for old in [r for r in expected if r['arm']==arm]:
  seed=old['seed'];p=root/(arm+'-'+seed+'.xml.xz')
  if not p.exists():continue
  tree=ET.ElementTree(ET.fromstring(lzma.decompress(p.read_bytes())));assert tree.getroot().get('result')=='Passed'
  captured=tree.find('.//test-case/output').text
  lines=captured.splitlines();end=[r.split(',') for r in lines if r.startswith('SEED,end,')]
  assert len(end)==1,(arm,seed,end)
  _,_,_,claims,contacts,digest=end[0];assert digest==old['digest'],(arm,seed,'digest',digest,old['digest'])
  assert contacts==old['contacts'],(arm,seed,'contacts')
  row=dict(arm=arm,seed=seed,claims=int(claims),contacts=int(contacts),sameKeeperHandReturns=int(old['sameKeeperHands']),otherClaims=int(claims)-int(old['sameKeeperHands']),digest=digest)
  summary.append(row)
  danger=[(int(r.split(',')[1]),r.split(',')[2]) for r in lines if r.startswith('DANGER,')]
  teams={int(r.split(',')[1]):int(r.split(',')[2]) for r in lines if r.startswith('0x'+seed+',')}
  if arm=='after':
   for line in lines:
    if line.startswith('0x'+seed+','):
     cells=line.split(',')
     if cells[3]=='hand':
      assert cells[7]=='PassKick'
      hand_ends.append((seed,int(cells[1]),int(cells[5])))
  current=None
  for line in lines:
   if not line.startswith('SPELL,'):continue
   a=line.split(',');frame=int(a[2]);keeper=int(a[3])
   if a[1]=='start':
    if current:issues.append((seed,'overlapping-start',current,a))
    current=dict(seed=seed,keeper=keeper,keeperTeam=teams[keeper],startFrame=frame)
   else:
    if current is None:issues.append((seed,'unmatched-end',a));continue
    assert current['keeper']==keeper
    current.update(endFrame=frame,durationFrames=frame-current['startFrame'],endReason=a[4],touchAgent=int(a[5]),touchTeam=int(a[6]))
    current['endReason']=('teammate-touch' if current['touchTeam']==current['keeperTeam'] else 'opponent-touch') if current['endReason']=='touch' else current['endReason']
    current['followedFrames']=min(600,324000-frame) if current['endReason']=='opponent-touch' else None
    current['shotsWithin10s']=sum(k=='shot' and 0<=f-frame<=600 for f,k in danger) if current['endReason']=='opponent-touch' else None
    current['goalsWithin10s']=sum(k=='goal' and 0<=f-frame<=600 for f,k in danger) if current['endReason']=='opponent-touch' else None
    if current['endReason']=='opponent-touch':
     unique_danger.update((seed,i,k) for i,(f,k) in enumerate(danger) if 0<=f-frame<=600)
    spells.append(current);current=None
  if current:
   current.update(endFrame=324000,durationFrames=324000-current['startFrame'],endReason='end-of-run-censored',touchAgent=-1,touchTeam=-1,followedFrames=None,shotsWithin10s=None,goalsWithin10s=None);spells.append(current)
assert not issues,issues
assert len(summary)==12
assert collections.Counter(hand_ends)==collections.Counter((s["seed"],s["keeper"],s["startFrame"]) for s in spells)
for name,rows in [('claims',summary),('spells',spells)]:
 if rows:
  with (root/(name+'.csv')).open('w') as f:
   w=csv.DictWriter(f,fieldnames=list(rows[0]),lineterminator="\n");w.writeheader();w.writerows(rows)
result=dict(matches=len(summary),claims=summary,spellCount=len(spells),endReasons={reason:sum(s['endReason']==reason for s in spells) for reason in sorted(set(s['endReason'] for s in spells))})
result['uniqueOpponentWindowShots']=sum(k=='shot' for _,_,k in unique_danger)
result['uniqueOpponentWindowGoals']=sum(k=='goal' for _,_,k in unique_danger)
if spells:
 result.update(minFrames=min(s['durationFrames'] for s in spells),medianFrames=statistics.median(s['durationFrames'] for s in spells),maxFrames=max(s['durationFrames'] for s in spells),opponentWindowShots=sum(s['shotsWithin10s'] or 0 for s in spells),opponentWindowGoals=sum(s['goalsWithin10s'] or 0 for s in spells))
(root/'analysis.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
