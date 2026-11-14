"""Created 2026-11-13. Verify materialized W13 checkpoint payloads and exact identity claims."""
import hashlib,importlib.util,json
from pathlib import Path
ROOT=Path(__file__).resolve().parent
REPO=next(p for p in ROOT.parents if (p/'.git').exists())
def main():
 spec=importlib.util.spec_from_file_location('store',REPO/'tools/dotnet-ci/retained_trace_store.py');store=importlib.util.module_from_spec(spec);spec.loader.exec_module(store)
 for line in (ROOT/'SHA256SUMS').read_text().splitlines():
  expected,name=line.split('  ',1)
  with (ROOT/name).open('rb') as f:actual=hashlib.file_digest(f,'sha256').hexdigest()
  assert actual==expected,('materialize Git LFS and retry, or corrupt file',name)
 stores=json.loads((ROOT/'publication/packed-stores.json').read_text())
 for row in stores:
  index=store.verify_store(ROOT/row['path']);assert index['archiveSha256']==row['archiveSha256']
 expected=json.loads((ROOT/'identity/historical-expected.json').read_text())
 def equal(a,b,name):
  with store.open_trace(a,name) as x,store.open_trace(b,name) as y:
   while True:
    p,q=x.read(1024*1024),y.read(1024*1024)
    assert p==q,(a,b,name)
    if not p:return
 for seed,digest in expected.items():
  historical=ROOT/'identity/retained/historical'/seed
  assert json.loads((historical/'summary.json').read_text())['terminal']==digest
  equal(ROOT/'identity/retained/w13_0'/seed,ROOT/'identity/retained/w13_off'/seed,'digests.bin.gz')
  a=ROOT/'section10/off-comparison/reference'/seed;b=ROOT/'section10/off-comparison/w13'/seed
  for name in ['digests','aligned','trajectory','ledger','team-possession','geometry','marking','mark-witness']:equal(a,b,name+'.bin.gz')
 snapshot=json.loads((ROOT/'checkpoint.json').read_text())
 for row in snapshot['includedCompleted']:
  flavor='armed' if row['armed'] else 'off'
  folder=ROOT/'section10/phase-aligned/captures'/row['arm']/('C'+str(row['case']))/row['seed']/(row['mode']+'-'+flavor)
  summary=json.loads((folder/'summary.json').read_text());assert summary['frames']==324000 and summary['schema']==29
  assert sum(summary['possession'])==sum(summary['teamPossession'])==summary['live']
  if row['mode']=='observed':
   audit=json.loads((folder/'audit.json').read_text())
   for key in ['shots','goals','fouls']:assert audit['ledgerCounts'][key]==summary[key]
   assert summary['marking']['nonCarrierMarkerPressByTeam']==[0,0]
 print('PASS:',len(stores),'decoded trace stores; six historical fingerprints; six W13 identity pairs; six integrated off pairs;',len(snapshot['includedCompleted']),'completed §10 captures.')
if __name__=='__main__':main()
