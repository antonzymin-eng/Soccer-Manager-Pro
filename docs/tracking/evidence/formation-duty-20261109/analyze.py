"""Created 2026-11-09. Audit/characterize #434 under frozen PROTOCOL.md definitions.

Use only after run.py's full control and matrix stages pass. No acceptance bands.
"""
import argparse, csv, gzip, hashlib, itertools, json, math, struct, importlib.util
from pathlib import Path
roots=[*Path(__file__).resolve().parents,Path.cwd()]
repo=next(p for p in roots if (p/'.git').exists())
spec=importlib.util.spec_from_file_location('trace_store',repo/'tools/dotnet-ci/retained_trace_store.py')
trace_store=importlib.util.module_from_spec(spec);spec.loader.exec_module(trace_store)
def open_trace(path):return trace_store.open_trace(path.parent,path.name)
FRAMES=324000
GEOMETRY=struct.Struct('<ii22fi22f')
TRAJECTORY=struct.Struct('<59fii')
def write_json(path,value):path.write_text(json.dumps(value,indent=2,allow_nan=False)+'\n')
def stream_size(path,width):
 digest=hashlib.sha256();size=0
 with open_trace(path) as f:
  while block:=f.read(1024*1024):digest.update(block);size+=len(block)
 if width:assert size==width*FRAMES,(path,size,width*FRAMES)
 return {'bytes':size,'sha256Uncompressed':digest.hexdigest()}
def geometry(path,summary):
 sums=[[0.0]*5 for _ in range(2)];counts=[0,0];phases=[[0]*4 for _ in range(2)]
 with open_trace(path) as f:
  previous=0
  while block:=f.read(GEOMETRY.size):
   assert len(block)==GEOMETRY.size
   data=GEOMETRY.unpack(block);frame=data[0];assert frame>previous and frame%6==0 and frame<FRAMES
   previous=frame
   for t,start in [(0,1),(1,24)]:
    phase=data[start];assert 0<=phase<4;phases[t][phase]+=1
    vectors=list(zip(data[start+1:start+23:2],data[start+2:start+23:2]))[1:]
    active=[]
    for x,y in vectors:
     if x==float('-inf') and y==float('-inf'):continue
     assert math.isfinite(x) and math.isfinite(y),(path,frame,t,x,y)
     # Existing #12 SlotComposer clamp, not a new football acceptance band.
     assert 0.5<=x<=104.5 and 0.5<=y<=67.5,('outfield slot outside #12 bounds',path,frame,t,x,y)
     active.append((x,y))
    if len(active)<2:continue
    xs,ys=zip(*active);minimum=min(math.dist(a,b) for a,b in itertools.combinations(active,2))
    for i,v in enumerate((min(xs),max(xs),min(ys),max(ys),minimum)):sums[t][i]+=v
    counts[t]+=1
 assert phases[0]+phases[1]==summary['phase']
 return [{k:(sums[t][i]/counts[t] if counts[t] else None) for i,k in enumerate(['meanSlotMinX','meanSlotMaxX','meanSlotMinY','meanSlotMaxY','meanMinimumPairDistance'])}|{'geometrySamples':counts[t]} for t in range(2)]
def audit_trajectory(path):
 with open_trace(path) as f:
  count=0
  while block:=f.read(TRAJECTORY.size*4096):
   assert len(block)%TRAJECTORY.size==0
   for row in TRAJECTORY.iter_unpack(block):
    assert all(map(math.isfinite,row[:59])),'non-finite position or full BallState'
    assert -1<=row[-1]<22,'invalid owner'
    count+=1
 assert count==FRAMES
 return count

def main():
 p=argparse.ArgumentParser();p.add_argument('--work-dir',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);a=p.parse_args();w=a.work_dir.resolve();out=a.output_dir;out.mkdir(parents=True,exist_ok=False)
 for stage in ['probe','control','matrix']:
  data=json.loads((w/stage/'PASS.json').read_text());assert data['passed']
 seeds=sorted(p.name for p in (w/'control/head/C0').iterdir())
 assert len(seeds)==6
 rows=[];audits=[];cache={}
 def load(arm,c,seed):
  measured=c if arm=='head' or c>=5 and c<=8 else 0
  stage='control' if measured==0 else 'matrix'
  path=w/stage/arm/('C'+str(measured))/seed/'observed'
  if path not in cache:
   d=json.loads((path/'summary.json').read_text());assert d['frames']==FRAMES and d['seed']==seed and d['case']==measured
   assert d['schema']==(28 if arm=='base' else 29) and len(d['buildHash'])==64
   assert sum(d['possession'])==d['live']==sum(d['teamPossession'])
   streams={name:stream_size(path/name,width) for name,width in [('digests.bin.gz',32),('aligned.bin.gz',32),('trajectory.bin.gz',244),('team-possession.bin.gz',8),('ledger.bin.gz',None),('geometry.bin.gz',None)]}
   assert audit_trajectory(path/'trajectory.bin.gz')==FRAMES
   g=geometry(path/'geometry.bin.gz',d);cache[path]=(d,g)
   audits.append({'path':str(path.relative_to(w)),'schema':d['schema'],'buildHash':d['buildHash'],'terminalDigest':d['digest'],'streams':streams,'geometry':g})
  return measured,path,*cache[path]
 for c in range(10):
  for seed in seeds:
   for arm in ['base','head']:
    measured,path,d,g=load(arm,c,seed)
    for t in range(2):
     row={'case':'C'+str(c),'seed':seed,'arm':arm,'team':t,'measuredCase':'C'+str(measured),'reusedC0':arm=='base' and c in [1,2,3,4,9],'reference':str(path.relative_to(w)),
          'scheduledFrames':FRAMES,'liveFrames':d['live'],'shots':d['shots'][t],'goals':d['goals'][t],'fouls':d['fouls'][t],
          'controlledFrames':d['possession'][t],'looseFrames':d['possession'][2],'teamPossessionFrames':d['teamPossession'][t],
          'unclaimedTeamFrames':d['teamPossession'][2],**g[t]}
     row.update({name:d['phase'][t*4+i] for i,name in enumerate(['InPossSamples','OutOfPossSamples','TransToAtkSamples','TransToDefSamples'])});rows.append(row)
 with (out/'per-match-team.csv').open('w',newline='') as f:
  writer=csv.DictWriter(f,fieldnames=rows[0].keys());writer.writeheader();writer.writerows(rows)
 comparisons=[]
 metrics=['shots','goals','fouls','controlledFrames','teamPossessionFrames','InPossSamples','OutOfPossSamples','TransToAtkSamples','TransToDefSamples','meanSlotMinX','meanSlotMaxX','meanSlotMinY','meanSlotMaxY','meanMinimumPairDistance']
 for c in range(10):
  for seed in seeds:
   for t in range(2):
    pair={r['arm']:r for r in rows if r['case']=='C'+str(c) and r['seed']==seed and r['team']==t}
    comparisons.append({'case':'C'+str(c),'seed':seed,'team':t,'baseMeasuredCase':pair['base']['measuredCase'],'reusedC0':pair['base']['reusedC0'],**{k+'Delta':pair['head'][k]-pair['base'][k] for k in metrics}})
 with (out/'paired-deltas.csv').open('w',newline='') as f:
  writer=csv.DictWriter(f,fieldnames=comparisons[0].keys());writer.writeheader();writer.writerows(comparisons)
 aggregate=[]
 for c in range(10):
  arms={}
  for arm in ['base','head']:
   selected=[r for r in rows if r['case']=='C'+str(c) and r['arm']==arm]
   arms[arm]={k:sum(r[k] for r in selected) for k in ['shots','goals','fouls']}
  aggregate.append({'case':'C'+str(c),'matchesPerArm':6,'baseReference':'measured C0 behavior reused' if c in [1,2,3,4,9] else 'own case',**arms})
 write_json(out/'audit.json',{'passed':True,'uniqueObservedFullMatches':len(audits),'framesPerMatch':FRAMES,'audits':audits})
 write_json(out/'aggregate.json',aggregate)
 print('Audited',len(audits),'unique observed full matches; report rows',len(rows),'paired team rows',len(comparisons))
if __name__=='__main__':main()
