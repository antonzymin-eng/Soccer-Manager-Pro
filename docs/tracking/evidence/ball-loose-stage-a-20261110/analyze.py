"""Created 2026-11-10: independently audit #440 windows against source chronology.

Frozen definitions are in w-440-ball-loose-design.md. Metrics are descriptive.
"""
import argparse, collections, csv, hashlib, json, struct, sys
from pathlib import Path
HERE=Path(__file__).resolve().parent
repo=next(p for p in HERE.parents if (p/'.git').exists());sys.path.insert(0,str(repo/'tools/dotnet-ci'))
from retained_trace_store import open_trace
FRAMES=324000
ROW=struct.Struct('<59fii')
CENSORS={'Restart':'restart','OutOfPlay':'out-of-play','Foul':'foul','HalfTime':'half-time','FullTime':'full-time'}
def key(r):return r['frame'],r['sequence']
def classify(owner,carrier,team):
 if owner<0:return 'still-loose'
 if owner==carrier:return 'original-carrier'
 return 'carrier-teammate' if owner//11==team else 'opponent'
def owners(folder):
 values=[]
 with open_trace(folder,'trajectory.bin.gz') as f:
  while block:=f.read(ROW.size*4096):
   assert len(block)%ROW.size==0
   values.extend(row[-1] for row in ROW.iter_unpack(block))
 assert len(values)==FRAMES and all(-1<=v<22 for v in values)
 return values
def audit(folder):
 journal=[json.loads(line) for line in (folder/'chronology.jsonl').read_text().splitlines()]
 episodes=[json.loads(line) for line in (folder/'episodes.jsonl').read_text().splitlines()]
 census=json.loads((folder/'census.json').read_text());summary=json.loads((folder/'summary.json').read_text())
 owner=owners(folder);assert summary['frames']==FRAMES
 assert [r['sequence'] for r in journal]==list(range(1,len(journal)+1))
 assert [key(r) for r in journal]==sorted(key(r) for r in journal)
 assert all(1<=r['frame']<=FRAMES for r in journal)
 changes=[r for r in journal if r['kind']=='Possession'];assert len(changes)==census['ownershipChanges']
 current=changes[0]['previous'] if changes else owner[0];offset=0
 for frame,actual in enumerate(owner,1):
  while offset<len(changes) and changes[offset]['frame']==frame:
   r=changes[offset];assert r['previous']==current and r['next']!=current
   current=r['next'];offset+=1
  assert current==actual,('chronology/frame-end mismatch',frame,current,actual)
 assert offset==len(changes)
 starts=[r for r in journal if r['kind']=='Tackle' and r['detail']==2] # #14 append-only BallLoose ordinal.
 assert len(starts)==len(episodes)==census['episodes']==census['engineBallLoose']
 result=[]
 for start,e in zip(starts,episodes):
  assert (e['frame'],e['tackler'],e['carrier'],e['sequence'])==(start['frame'],start['actor'],start['other'],start['sequence'])
  frame=start['frame'];horizon=frame+60;carrier=start['other'];team=carrier//11;assert e['carrierTeam']==team
  later=[r for r in journal if key(r)>key(start) and r['frame']<=horizon]
  boundaries=[r for r in later if r['kind']=='Tackle' or r['kind'] in CENSORS]
  boundary=boundaries[0] if boundaries else None
  reason=('new-tackle' if boundary['kind']=='Tackle' else CENSORS[boundary['kind']]) if boundary else 'capture-end' if horizon>FRAMES else None
  cutoff=key(boundary) if boundary else (min(horizon,FRAMES),float('inf'))
  prefix=[r for r in later if key(r)<cutoff]
  owned=[r for r in prefix if r['kind']=='Possession'];paths=[r for r in prefix if r['kind'] in ['FirstTouch','LoosePickup']]
  assert e['ownership']==owned and e['paths']==paths
  regains=[r for r in owned if r['next']==carrier and r['frame']==frame]
  opponent=any(r['next']>=0 and r['next']//11!=team for r in owned)
  teammate=any(r['next']>=0 and r['next']!=carrier and r['next']//11==team for r in owned)
  outcome='censored' if reason else classify(owner[horizon-1],carrier,team)
  assert (e['sameFrameCarrierRegain'],e['opponentRecovery'],e['teammateRecovery'])==(bool(regains),opponent,teammate)
  assert e['frameEndOwner']==owner[frame-1]
  assert e['frameEndClass']==classify(owner[frame-1],carrier,team)
  assert e['outcome']==outcome and e['censorReason']==reason
  assert e['endFrame']==(boundary['frame'] if boundary else min(horizon,FRAMES))
  cause='none'
  if regains:
   regain=regains[0]
   next_change=next((key(r) for r in owned if key(r)>key(regain)),cutoff)
   previous_change=max((key(r) for r in owned if key(r)<key(regain)),default=key(start))
   following=[r for r in paths if r['kind']=='LoosePickup' and r['frame']==frame and r['actor']==carrier and key(regain)<key(r)<next_change]
   preceding=[r for r in paths if r['kind']=='FirstTouch' and r['frame']==frame and r['actor']==carrier and previous_change<key(r)<key(regain)]
   cause='passive-pickup' if following else 'first-touch' if preceding else 'ownership-seam-only'
  v=e['initialVelocity'];resting=v['x']==0 and v['y']==0
  result.append({'seed':summary['seed'],'carrierTeam':team,'frame':frame,'tackler':e['tackler'],'carrier':carrier,'sequence':e['sequence'],
                 'sameFrameCarrierRegain':bool(regains),'frameEndClass':e['frameEndClass'],'outcome':outcome,'censorReason':reason,
                 'opponentRecovery':opponent,'teammateRecovery':teammate,'firstSameFrameRegainPath':cause,'initialPlanarVelocityExactlyZero':resting})
 return summary,census,result

def main():
 p=argparse.ArgumentParser();p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);a=p.parse_args();w=a.work_dir;out=a.output_dir;out.mkdir(parents=True,exist_ok=False)
 assert json.loads((w/'PASS.json').read_text())['passed']
 all_rows=[];matches=[]
 for folder in sorted((w/'results/observed').iterdir()):
  summary,census,rows=audit(folder);all_rows+=rows
  for team in range(2):
   selected=[r for r in rows if r['carrierTeam']==team]
   matches.append({'seed':summary['seed'],'team':team,'frames':summary['frames'],'liveFrames':summary['live'],
                   'episodes':len(selected),'sameFrameCarrierRegains':sum(r['sameFrameCarrierRegain'] for r in selected),
                   'dispossessions':sum(r['opponentRecovery'] for r in selected),'teammateRecoveries':sum(r['teammateRecovery'] for r in selected),
                   'globalHolderChangesBothTeams':census['ownershipChanges'],'shots':summary['shots'][team],'goals':summary['goals'][team],'fouls':summary['fouls'][team],
                   **{'frameEnd_'+k:sum(r['frameEndClass']==k for r in selected) for k in ['original-carrier','carrier-teammate','opponent','still-loose']},
                   **{'horizon_'+k:sum(r['outcome']==k for r in selected) for k in ['original-carrier','carrier-teammate','opponent','still-loose','censored']}})
 for name,rows in [('episodes.csv',all_rows),('per-match-team.csv',matches)]:
  with (out/name).open('w',newline='') as f:
   fields=rows[0].keys() if rows else ['seed','carrierTeam','frame','tackler','carrier','sequence','sameFrameCarrierRegain','frameEndClass','outcome','censorReason','opponentRecovery','teammateRecovery','firstSameFrameRegainPath','initialPlanarVelocityExactlyZero']
   writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)
 report={'passed':True,'matches':6,'episodes':len(all_rows),'sameFrameCarrierRegains':sum(r['sameFrameCarrierRegain'] for r in all_rows),
         'endFrame':dict(collections.Counter(r['frameEndClass'] for r in all_rows)),
         'horizon':dict(collections.Counter(r['outcome'] for r in all_rows)),
         'censorReasons':dict(collections.Counter(r['censorReason'] for r in all_rows if r['censorReason'])),
         'dispossessions':sum(r['opponentRecovery'] for r in all_rows),'teammateRecoveries':sum(r['teammateRecovery'] for r in all_rows),
         'firstSameFrameRegainPaths':dict(collections.Counter(r['firstSameFrameRegainPath'] for r in all_rows if r['sameFrameCarrierRegain']))}
 (out/'audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
