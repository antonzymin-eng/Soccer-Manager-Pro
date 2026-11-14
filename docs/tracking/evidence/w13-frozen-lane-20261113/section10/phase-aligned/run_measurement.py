"""Created 2026-11-13. Frozen, fail-closed W13 section 10 captures (two processes)."""
import concurrent.futures,csv,datetime,gzip,hashlib,importlib.util,json,os,struct,subprocess,threading,time
from pathlib import Path
ROOT=Path(__file__).resolve().parent
EXPERIMENT=ROOT.parent.parent
CORE={'digests':32,'trajectory':244,'team-possession':8,'geometry':None,'ledger':None}
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def sha(path):
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def same(a,b):
 with gzip.open(a,'rb') as x,gzip.open(b,'rb') as y:
  while True:
   p,q=x.read(1024*1024),y.read(1024*1024)
   if p!=q:return False
   if not p:return True
def audit(path,observed):
 d=json.loads((path/'summary.json').read_text());assert d['frames']==324000 and d['schema']==29
 assert len(d['buildHash'])==64 and sum(d['possession'])==sum(d['teamPossession'])==d['live']
 streams={}
 for name,width in CORE.items():
  h=hashlib.sha256();size=0
  with gzip.open(path/(name+'.bin.gz'),'rb') as f:
   while block:=f.read(1024*1024):size+=len(block);h.update(block)
  if width:assert size==324000*width,(name,size)
  streams[name]={'bytes':size,'sha256Uncompressed':h.hexdigest()}
 with gzip.open(path/'digests.bin.gz','rb') as f:raw=f.read()
 assert raw[-32:].hex()==d['digest']
 if not observed:return {'summary':d,'streams':streams}
 layouts={x['ordinal']:x for x in json.loads((path/'event-layouts.json').read_text())}
 counts={k:[0,0] for k in ['shots','goals','fouls','offsides']}
 with gzip.open(path/'ledger.bin.gz','rb') as f:data=f.read()
 at=0;event_count=0
 for frame in range(1,324001):
  n=struct.unpack_from('<i',data,at)[0];at+=4;assert n>=0
  for _ in range(n):
   ordinal=data[at];size=struct.unpack_from('<i',data,at+1)[0];at+=5
   assert size>=0 and at+size<=len(data);payload=data[at:at+size];at+=size;event_count+=1
   layout=layouts.get(ordinal)
   if not layout:continue
   fields=layout['fields'];name=layout['name'].rsplit('.',1)[-1]
   if name=='ShotExecutedEvent':key='shots';team=struct.unpack_from('<i',payload,fields['TeamId'])[0]
   elif name=='GoalAwardedEvent':key='goals';team=payload[fields['ScoringTeam']]
   elif name=='FoulCommittedEvent':key='fouls';team=struct.unpack_from('<i',payload,fields['Offender'])[0]//11
   elif name=='OffsideCalledEvent':key='offsides';team=payload[fields['Team']]
   else:continue
   assert team in (0,1),(name,team);counts[key][team]+=1
 assert at==len(data)
 for key in ['shots','goals','fouls']:assert counts[key]==d[key],(key,counts[key],d[key])
 assert counts['offsides']==d['marking']['offsidesByTeam']
 with gzip.open(path/'marking.bin.gz','rb') as f:marks=f.read()
 assert len(marks)==sum(d['phase'][:4])*246
 with gzip.open(path/'mark-witness.bin.gz','rb') as f:witness=f.read()
 assert len(witness)==sum(d['phase'][:4])*312
 with gzip.open(path/'retained-lag.bin.gz','rb') as f:lag=f.read()
 assert len(lag)==324000*16
 possession=[0,0];inactive=[0,0];max_run=[0,0];current=[0]*22
 for i,(frame,targets,owned,dead) in enumerate(struct.iter_unpack('<iIII',lag)):
  assert frame==i+1 and not (targets>>22) and not (owned & ~targets) and not (dead & ~targets)
  if i<d['live']:
   for team in (0,1):
    mask=((1<<11)-1)<<(team*11);possession[team]+=(owned&mask).bit_count();inactive[team]+=(dead&mask).bit_count()
   for agent in range(22):
    current[agent]=current[agent]+1 if owned & (1<<agent) else 0
    max_run[agent//11]=max(max_run[agent//11],current[agent])
 assert possession==d['marking']['retainedPossessionLagMarkerFramesByTeam']
 assert inactive==d['marking']['retainedInactiveLagMarkerFramesByTeam']
 return {'summary':d,'streams':streams,'events':event_count,'ledgerCounts':counts,'maxConsecutivePossessionLagFramesByTeam':max_run}
def main():
 pin=json.loads((ROOT/'capture-prepared.json').read_text())
 env=os.environ.copy();env.update(DOTNET_PROCESSOR_COUNT='4',DOTNET_CLI_HOME=str(ROOT/'dotnet-home'),XDG_DATA_HOME='/workspace/work/xdg',NUGET_PACKAGES='/workspace/work/nuget',DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 status={'state':'waiting_for_off_comparison','startedUtc':now(),'requiredW13Captures':66,'additionalPlainControls':12,
         'freshReferenceCaptures':60,'plannedFullCaptures':138,'completed':[],'running':[],'checks':[],'failures':[]}
 lock=threading.Lock()
 def save():
  p=ROOT/'measurement-status.json';q=p.with_suffix('.tmp');q.write_text(json.dumps(status,indent=2)+'\n');q.replace(p)
 save()
 old_status=ROOT.parent/'off-comparison-status.json'
 while True:
  previous=json.loads(old_status.read_text())
  if previous['state']=='passed':break
  if previous['state']=='failed':raise RuntimeError('preceding unarmed comparison failed')
  time.sleep(10)
 # Freeze this controller and the source-phase interpretation before executing corrected probes or full captures.
 files=[ROOT/'run_measurement.py',ROOT/'preflight.py',ROOT/'PHASE-ALIGNED-PROTOCOL.md',ROOT/'capture-prepared.json',ROOT/'integration-source.json']
 (ROOT/'measurement-freeze.json').write_text(json.dumps({'frozenUtc':now(),'files':{str(p.relative_to(ROOT)):sha(p) for p in files},'inputs':pin['inputs']},indent=2)+'\n')
 def verify():
  for name,digest in pin['inputs'].items():assert sha(ROOT/name)==digest,('changed capture input',name)
  for arm,info in pin['arms'].items():
   for name,digest in info['binaries'].items():assert sha(Path(info['binary']).parent/name)==digest,('changed binary',arm,name)
  for name,digest in json.loads((ROOT/'measurement-freeze.json').read_text())['files'].items():assert sha(ROOT/name)==digest,('changed controller/protocol',name)
 verify()
 status['state']='corrected_preflight';save()
 with (ROOT/'preflight-controller.log').open('w') as log:
  code=subprocess.run(['python3',str(ROOT/'preflight.py')],cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
 if code:
  status.update(state='failed_preflight',finishedUtc=now());save();return 1
 status['state']='full_observer_controls';save()
 def task(job):
  arm,c,seed,mode,armed=job;flavor='armed' if armed else 'off'
  target=ROOT/'captures'/arm/('C'+str(c))/seed/(mode+'-'+flavor);target.mkdir(parents=True,exist_ok=False)
  label='/'.join([arm,'C'+str(c),seed,mode+'-'+flavor])
  command=[pin['dotnet'],'--fx-version',pin['clr'],pin['arms'][arm]['binary'],seed,str(c),mode,str(target),'324000',flavor]
  row={'label':label,'arm':arm,'case':c,'seed':seed,'mode':mode,'armed':armed,'command':command,'startedUtc':now()}
  with lock:status['running'].append(label);save()
  with (target/'runner.log').open('w') as log:row['exitCode']=subprocess.run(command,cwd=ROOT/arm,env=env,stdout=log,stderr=subprocess.STDOUT).returncode
  row['finishedUtc']=now();(target/'run.json').write_text(json.dumps(row,indent=2)+'\n')
  error=None
  if row['exitCode']:error='capture exited '+str(row['exitCode'])
  else:
   try:
    result=audit(target,mode=='observed');(target/'audit.json').write_text(json.dumps(result,indent=2)+'\n')
    if mode=='observed' and armed:assert sum(result['summary']['marking']['targetSamplesByTeam'])>0,'vacuous armed capture'
    (target/'files-SHA256.json').write_text(json.dumps({p.name:sha(p) for p in sorted(target.iterdir()) if p.is_file()},indent=2)+'\n')
   except Exception as ex:error='integrity audit: '+repr(ex)
  with lock:
   status['running'].remove(label);status['completed'].append({**row,'error':error})
   if error:status['failures'].append({'label':label,'error':error})
   save()
  print(now(),label,'FAIL '+error if error else 'PASS capture/audit',flush=True)
  return error
 def batch(jobs):
  with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
   it=iter(jobs);pending={pool.submit(task,j) for j in [next(it,None),next(it,None)] if j}
   while pending:
    done,pending=concurrent.futures.wait(pending,return_when=concurrent.futures.FIRST_COMPLETED)
    bad=next((e for e in [f.result() for f in done] if e),None)
    if bad:
     for f in pending:f.result()
     raise RuntimeError(bad)
    for _ in done:
     job=next(it,None)
     if job:pending.add(pool.submit(task,job))
 def record_check(kind,seed,stream,a,b):
  equal=same(a,b);status['checks'].append({'kind':kind,'seed':seed,'stream':stream,'equal':equal});save()
  if not equal:raise RuntimeError(kind+' mismatch '+seed+' '+stream)
 try:
  # An armed pair starts first; no matrix starts until all six seeds' observer controls pass.
  for seed in pin['seeds']:
   jobs=[('w13',0,seed,'plain',True),('w13',0,seed,'observed',True),('w13',0,seed,'plain',False),('w13',0,seed,'observed',False),('reference',0,seed,'observed',False)]
   batch(jobs)
   for armed in [False,True]:
    flavor='armed' if armed else 'off';base=ROOT/'captures/w13/C0'/seed
    for stream in CORE:record_check('observation-neutrality-'+flavor,seed,stream,base/('plain-'+flavor)/(stream+'.bin.gz'),base/('observed-'+flavor)/(stream+'.bin.gz'))
   a=ROOT/'captures/reference/C0'/seed/'observed-off';b=ROOT/'captures/w13/C0'/seed/'observed-off'
   for stream in [*CORE,'aligned','marking','mark-witness','retained-lag']:record_check('switch-off-identity',seed,stream,a/(stream+'.bin.gz'),b/(stream+'.bin.gz'))
   on=ROOT/'captures/w13/C0'/seed/'observed-armed'
   assert not same(on/'digests.bin.gz',b/'digests.bin.gz'),'armed positive control is identical'
  status['state']='paired_matrix';save();verify()
  batch([(arm,c,seed,'observed',armed) for seed in pin['seeds'] for c in range(1,10) for arm,armed in [('reference',False),('w13',True)]])
  rows=[]
  for row in status['completed']:
   if row['mode']!='observed' or (row['arm']=='w13' and not row['armed'] and row['case']!=0):continue
   flavor='armed' if row['armed'] else 'off';path=ROOT/'captures'/row['arm']/('C'+str(row['case']))/row['seed']/('observed-'+flavor)
   d=json.loads((path/'summary.json').read_text())
   for team in (0,1):rows.append({'arm':row['arm'],'armed':row['armed'],'case':row['case'],'seed':row['seed'],'team':team,'goals':d['goals'][team],'shots':d['shots'][team],'fouls':d['fouls'][team],'offsides':d['marking']['offsidesByTeam'][team],'targetSamples':d['marking']['targetSamplesByTeam'][team],'freshMarkerDecisions':d['marking']['freshMarkerDecisionsByTeam'][team],'nonCarrierMarkerPress':d['marking']['nonCarrierMarkerPressByTeam'][team],'retainedPossessionLagMarkerFrames':d['marking']['retainedPossessionLagMarkerFramesByTeam'][team]})
  with (ROOT/'per-match-team.csv').open('w',newline='') as f:
   writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
  status.update(state='captures_complete_analysis_pending',finishedUtc=now());save()
  print('All raw captures/audits complete. M1-M8 analysis and owner review remain; no enablement approval.',flush=True);return 0
 except Exception as ex:
  status.update(state='failed_capture_or_comparison',finishedUtc=now(),error=str(ex));save();print(str(ex),flush=True);return 1
if __name__=='__main__':raise SystemExit(main())
