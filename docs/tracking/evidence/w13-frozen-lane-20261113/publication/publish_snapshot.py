"""Created 2026-11-13. Snapshot completed W13 captures without touching active producers."""
from pathlib import Path
import datetime,hashlib,importlib.util,json,shutil,tarfile
SRC=Path('/workspace/work/w13-frozen-20261113')
REPO=Path('/workspace/work/w13-publish')
OUT=REPO/'docs/tracking/evidence/w13-frozen-lane-20261113'
def sha(path):
 with Path(path).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def copy(src,dst):
 dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
def save_json(path,value):
 path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,indent=2)+'\n')
def archive(path,items):
 path.parent.mkdir(parents=True,exist_ok=True);members={}
 with tarfile.open(path,'w:xz',preset=1) as tar:
  for name,source in sorted(items):
   if source.is_symlink() or not source.is_file():raise RuntimeError('archive regular files only: '+str(source))
   members[name]={'bytes':source.stat().st_size,'sha256':sha(source)};tar.add(source,arcname=name,recursive=False)
 save_json(path.with_name(path.name+'.members.json'),{'archiveSha256':sha(path),'members':members})
def main():
 OUT.mkdir(parents=True,exist_ok=False)
 current=json.loads((SRC/'section10/phase-aligned/measurement-status.json').read_text())
 cutoff={'capturedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'liveStatusAtCutoff':current,
         'scope':'completed captures only; running and future captures excluded','includedCompleted':[]}
 save_json(OUT/'checkpoint.json',cutoff)
 spec=importlib.util.spec_from_file_location('store',REPO/'tools/dotnet-ci/retained_trace_store.py');store=importlib.util.module_from_spec(spec);spec.loader.exec_module(store)
 work=[]
 for name in ['IDENTITY-PROTOCOL.md','IdentityProgram.cs','prepare_identity.py','run_identity.py','identity-prepared.json','identity-comparison.json','identity-status.json','identity-controller.log','historical-expected.json']:
  copy(SRC/name,OUT/'identity'/name)
 for arm in ['historical','w13_0','w13_off']:
  for source in sorted((SRC/'identity-runs'/arm).iterdir()):
   target=OUT/'identity/retained'/arm/source.name;shutil.copytree(source,target);work.append(target)
 base=SRC/'section10'
 for name in ['integration-source.json','freeze_integration.py','source-integration.patch','TracePossessionLag.cs','possession-lag-reproduction.json','OFF-COMPARISON-PROTOCOL.md','run_off_comparison.py','off-comparison-freeze.json','off-comparison-PASS.json']:
  copy(base/name,OUT/'section10'/name)
 for source in sorted((base/'off-comparison').glob('*/*')):
  target=OUT/'section10/off-comparison'/source.relative_to(base/'off-comparison');shutil.copytree(source,target);work.append(target)
 active=base/'phase-aligned'
 for name in ['PHASE-ALIGNED-PROTOCOL.md','prepare_capture.py','extend_runner.py','preflight.py','run_measurement.py','capture-prepared.json','measurement-freeze.json','controller-startup-accounting.json']:
  copy(active/name,OUT/'section10/phase-aligned'/name)
 for source in sorted((active/'source').iterdir()):
  if source.is_file():copy(source,OUT/'section10/phase-aligned/source'/source.name)
 for row in current['completed']:
  if row.get('error') is not None or row['exitCode']!=0:raise RuntimeError('failed completed capture needs explicit handling')
  flavor='armed' if row['armed'] else 'off'
  relative=Path(row['arm'])/('C'+str(row['case']))/row['seed']/(row['mode']+'-'+flavor)
  source=active/'captures'/relative;target=OUT/'section10/phase-aligned/captures'/relative
  ledger=json.loads((source/'files-SHA256.json').read_text())
  shutil.copytree(source,target)
  for name,h in ledger.items():
   if sha(target/name)!=h:raise RuntimeError('completed file changed: '+str(relative/name))
  work.append(target);cutoff['includedCompleted'].append(row)
 save_json(OUT/'checkpoint.json',cutoff)
 for source in sorted((base/'test-results').glob('*.trx')):copy(source,OUT/'section10/tests'/source.name)
 # Retain all failed/superseded instrument attempts and the independent read-only repeat.
 items=[]
 for folder in ['preflight','repro-a','repro-b','pilot-no-543-armed']:
  for source in sorted((base/folder).rglob('*')):
   if source.is_file():items.append((str(source.relative_to(base)),source))
 for name in ['capture-prepared-prototype.json','capture-prepared.json','pilot-no-543-armed.log','preflight-controller.log','reference-build.log','w13-build.log','prepare-capture.log','prepare-capture-prototype.log','trace-build.log']:
  if (base/name).is_file():items.append((name,base/name))
 for folder in ['source','pilot-build']:
  for source in sorted((base/folder).glob('*.cs*')):items.append((str(source.relative_to(base)),source))
 archive(OUT/'attempts/initial-instruments.tar.xz',items)
 items=[]
 for source in sorted((active/'preflight').rglob('*')):
  if source.is_file():items.append((str(source.relative_to(active)),source))
 for name in ['preflight-controller.log','measurement-controller.log','prepare-capture.log','reference-build.log','w13-build.log','waiting-attempt-1-status.json']:
  p=active/name
  if p.is_file():
   # Controller stdout may still be growing; copy a bounded current snapshot first.
   target=OUT/'publication'/(name+'.as-of');copy(p,target);items.append((name,target))
 archive(OUT/'attempts/phase-aligned-preflight.tar.xz',items)
 logs=[]
 for name in ['historical-build-attempt-1.log','historical-build.log','w13_0-build.log','w13_off-build.log','prepare-identity-attempt-1.log','prepare-identity.log']:
  logs.append((name,SRC/name))
 archive(OUT/'attempts/identity-build-logs.tar.xz',logs)
 copy(Path(__file__),OUT/'publication/publish_snapshot.py')
 packed=[]
 for i,path in enumerate(work,1):
  index=store.pack(path,delete_originals=True,preset=1)
  row={'path':str(path.relative_to(OUT)),'streams':len(index['streams']),'rawBytes':sum(x['bytes'] for x in index['streams'].values()),'archiveBytes':(path/store.ARCHIVE).stat().st_size,'archiveSha256':index['archiveSha256'],'verified':True}
  packed.append(row);save_json(OUT/'publication/packed-stores.json',packed)
  print(f'Packed/verified {i}/{len(work)} {row["path"]} {row["archiveBytes"]}',flush=True)
 print('Snapshot complete:',len(cutoff['includedCompleted']),'section 10 captures,',len(work),'verified stores',flush=True)
if __name__=='__main__':main()
