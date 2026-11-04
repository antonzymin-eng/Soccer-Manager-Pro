"""Created 2026-11-04: retain compact repository evidence and full local artifacts."""
from pathlib import Path
import csv, hashlib, json, lzma, shutil, tarfile
ROOT=Path(__file__).resolve().parent
# This managed executor has no /codex mount; retain the export bundle as scratch.
OUTPUT=ROOT/'deliverables'
REPO=Path('/workspace/Soccer-Manager-Pro')
TARGET=REPO/'docs/tracking/evidence/pre-c-control-20261104'
report=json.loads((ROOT/'analysis.json').read_text())
assert report['passed']
TARGET.mkdir(parents=True,exist_ok=True);OUTPUT.mkdir(parents=True,exist_ok=True)
for name in ['PROTOCOL.md','freeze.json','expected.json','observer-only.patch','Program.cs','prepare.py','freeze.py','run.py','analyze.py','test_selection.py','verify_freeze.py','analysis.json','windows.json','runs.json','selection-tests.log','classifier-tests.log','plain-build-final-v2.log','observed-build-final-v2.log','plain-build-accepted.log','observed-build-accepted.log','freeze-verification.log','analysis.log']:
    dest=TARGET/(name+'.txt' if name.endswith('.cs') else name)
    shutil.copyfile(ROOT/name,dest)
for name in ['test_evidence_checks.py','evidence-check-tests.log']:
    shutil.copyfile(ROOT/name,TARGET/name)
shutil.copyfile(ROOT/'observed/capture/FrozenW8.cs',TARGET/'FrozenW8.cs.txt')
with (TARGET/'per-match.csv').open('w') as f:
    w=csv.writer(f,lineterminator="\n");w.writerow(['seed','adopted_digest','plain_digest','observed_digest','digest_chain_equal','ball_stream_equal','ledger_equal','goals','shots','distributions'])
    for item in report['seeds']:
        p,o=item['plain'],item['observed'];s=item['streams']
        w.writerow([item['seed'],item['adoptedDigest'],p['digest'],o['digest'],s['digests.bin']['equal'],s['trajectory.bin']['equal'],s['ledger.bin']['equal'],o['goals'],o['shots'],o['distributions']])
with (TARGET/'selected-windows.csv').open('w') as f:
    w=csv.writer(f,lineterminator="\n");w.writerow(['seed','keeper','team','release_frame','release_order','first_opponent_touch_frame','touch_order','touch_kind','toucher','delay_frames','complete','own_shots','opponent_shots','own_goals','opponent_goals'])
    for item in json.loads((ROOT/'windows.json').read_text()):
        if not item['selected']:continue
        r,t=item['release'],item['firstOtherTouch']
        counts=[sum(e['kind']==kind and e['side']==side for e in item['events']) for kind in ('shot','goal') for side in ('releasing-team','opponent')]
        w.writerow([item['seed'],r['agent'],r['team'],r['frame'],r['order'],t['frame'],t['order'],t['kind'],t['agent'],item['delayFrames'],item['complete'],*counts])

def pack(destination,paths,root,dedup=False):
    seen={}
    with lzma.open(destination,'wb',preset=3) as compressed,tarfile.open(fileobj=compressed,mode='w|') as archive:
        for path in sorted(paths):
            name=str(path.relative_to(root));info=archive.gettarinfo(str(path),arcname=name)
            if dedup:
                digest=hashlib.sha256(path.read_bytes()).hexdigest()
                if digest in seen:
                    info.type=tarfile.LNKTYPE;info.linkname=seen[digest];info.size=0;archive.addfile(info);continue
                seen[digest]=name
            with path.open('rb') as f:archive.addfile(info,f)

# Both arms' original logs and the observed raw, ordered touch/event records and
# frozen census are retained. Per-tick digest bytes are in the full local artifact;
# their complete stream SHA-256 hashes and equality results are in analysis.json.
compact=[]
for path in (ROOT/'results').rglob('*'):
    if path.is_file() and path.name!='digests.bin':
        compact.append(path)
pack(TARGET/'capture-traces.tar.xz',compact,ROOT,dedup=True)
pack(OUTPUT/'pre-c-control-full-captures.tar.xz',[p for p in (ROOT/'results').rglob('*') if p.is_file()],ROOT,dedup=True)
with lzma.open(OUTPUT/'pre-c-control-source.tar.xz','wb',preset=3) as f:f.write((ROOT/'source.tar').read_bytes())
pack(OUTPUT/'pre-c-control-binaries.tar.xz',[p for arm in ('plain','observed') for p in (ROOT/arm/'capture/bin/Debug/net8.0').iterdir() if p.is_file()],ROOT,dedup=True)
attempts=[p for p in (ROOT/'superseded-v1').rglob('*') if p.is_file() and p.name not in ('digests.bin','trajectory.bin','ledger.bin','observations.jsonl','engine.log','frozen-w8.txt')]
attempts += [ROOT/name for name in ['prepare.log','plain-build.log','plain-build-diagnostic.log','observed-build.log','plain-build-final.log'] if (ROOT/name).exists()]
pack(TARGET/'preflight-attempts.tar.xz',attempts,ROOT)
print('Packaged evidence at',TARGET,'and full source/binaries/captures at',OUTPUT)
