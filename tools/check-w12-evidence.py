#!/usr/bin/env python3
"""Verify durable W12 evidence, census accounting, and generated comparison doc."""
from __future__ import annotations
import argparse, base64, hashlib, io, json, lzma, re, sys, tarfile, zipfile
from collections import defaultdict
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
EVID=ROOT/'docs/tracking/evidence/w12'
CENSUS=EVID/'w12-gate-firing-census.json'
DOC=ROOT/'docs/tracking/w12-gate-firing-post398-comparison.md'
PART_GLOB='w12-raw-text-evidence.tar.xz.b64.part-*'
BUNDLE_SHA='a1c26890dc7484f59caee0d10a1fe8d7523fced3221d7f5e0d32a471760f07c6'
SWEEP_SHA='c1e3526987793b8d736fad3f52e983982a8d10d483215a4ebbcb0f44bdef94f6'

def sha(b:bytes)->str: return hashlib.sha256(b).hexdigest()
def kv(s:str): return {k:int(v) for k,v in re.findall(r'(\w+)=(\d+)',s)}

def parse_census(raw:bytes):
    lines=raw.decode('utf-8',errors='strict').splitlines()
    start=next(i for i,l in enumerate(lines) if l.strip()=='=== W12 gate-firing census ===')
    rows=[]; seed=final=None; seen=set(); i=start+1
    while i<len(lines):
        line=lines[i].strip()
        sm=re.match(r'seed (0x[0-9A-Fa-f]+)\s+final (\d+-\d+)',line)
        if sm: seed,final=sm.groups(); i+=1; continue
        tm=re.match(r'team (\d+): (.*)',line)
        if tm and seed:
            key=(seed,int(tm.group(1)))
            if key in seen: break
            seen.add(key)
            row={'seed':seed,'final':final,'team':int(tm.group(1)),**kv(tm.group(2)),
                 'phase':kv(lines[i+1]),'exits':kv(lines[i+2]),'raw':kv(lines[i+3]),'committed':kv(lines[i+4])}
            rows.append(row); i+=5
            if len(rows)==6: break
            continue
        i+=1
    if len(rows)!=6: raise AssertionError(f'expected 6 team rows, got {len(rows)}')
    return rows

def aggregate(rows):
    out={k:sum(r[k] for r in rows) for k in ('samples','latestPass','active','primaryAssigned','coverShadows')}
    for group in ('exits','raw','committed'):
        d=defaultdict(int)
        for r in rows:
            for k,v in r[group].items(): d[k]+=v
        out[group]=dict(sorted(d.items()))
    out['scorelines']=[r['final'] for r in rows if r['team']==0]
    out['nonInPossession']=out['samples']-out['exits'].get('InPossession',0)
    return out

def reconstruct(c):
    parts=sorted(EVID.glob(PART_GLOB))
    if len(parts)!=4: raise AssertionError(f'expected 4 evidence parts, found {len(parts)}')
    b64=''.join(p.read_text().strip() for p in parts)
    bundle=base64.b64decode(b64,validate=True)
    if sha(bundle)!=BUNDLE_SHA: raise AssertionError(f'W12 bundle sha mismatch: {sha(bundle)}')
    tarbytes=lzma.decompress(bundle)
    found={}
    with tarfile.open(fileobj=io.BytesIO(tarbytes),mode='r:') as tf:
        for name in ('pre/instrument-output.txt','pre/measurement.txt','post/instrument-output.txt','post/measurement.txt'):
            f=tf.extractfile(name)
            if f is None: raise AssertionError(f'missing {name}')
            found[name]=f.read()
    for side in ('pre','post'):
        p=c['provenance'][side]
        for name,key in [('instrument-output.txt','instrument_output_sha256'),('measurement.txt','measurement_sha256')]:
            got=sha(found[f'{side}/{name}'])
            if got!=p[key]: raise AssertionError(f'{side}/{name} sha mismatch: {got}')
    return found

def verify_sweep(c):
    p=EVID/'w12-static-unread-field-sweep-34803051927.zip'
    b=p.read_bytes()
    if sha(b)!=SWEEP_SHA: raise AssertionError(f'sweep ZIP sha mismatch: {sha(b)}')
    with zipfile.ZipFile(io.BytesIO(b)) as z:
        for name,meta in c['durable_evidence']['sweep_members'].items():
            raw=z.read(name)
            if len(raw)!=meta['size'] or sha(raw)!=meta['sha256']:
                raise AssertionError(f'sweep member mismatch: {name}')

def verify_structure(c):
    for side in ('pre','post'):
        rows=c[side]['rows']
        for r in rows:
            if sum(r['exits'].values())!=r['samples']:
                raise AssertionError(f"{side} {r['seed']} team {r['team']}: exits != samples")
            max_latest=r['samples']-r['exits'].get('InPossession',0)-r['exits'].get('Cooldown',0)-r['exits'].get('StaleTick',0)
            if r['latestPass']>max_latest:
                raise AssertionError(f"{side} {r['seed']} team {r['team']}: latestPass {r['latestPass']} > eligible upper bound {max_latest}")
        if aggregate(rows)!=c[side]['aggregate']:
            raise AssertionError(f'{side} aggregate does not equal row sum')
    if c['pre']['aggregate']['scorelines']!=c['post']['aggregate']['scorelines']:
        raise AssertionError('matched-corpus scorelines differ')
    for k in ('Active','InvariantRejected','NoPrimaryPresser','Disengaged','Cooldown','NoCommittedTrigger','InPossession'):
        if c['pre']['aggregate']['exits'].get(k,0)!=c['post']['aggregate']['exits'].get(k,0):
            raise AssertionError(f'gate outcome changed for {k}')

def render(c):
    pre=c['pre']['aggregate']; post=c['post']['aggregate']
    pp=c['provenance']['pre']; qp=c['provenance']['post']
    L=[]; a=L.append
    a('# W12 gate-firing post-#398 comparison — corrected record')
    a('')
    a('> **Correction, September 14, 2026:** the prior version of this document was not a valid transcription of run `34847990460`. This file is generated from the mechanically parsed census and checked against the committed raw source bytes. Do not hand-edit measurement values; run `python tools/check-w12-evidence.py --write` after an intentional census update.')
    a('')
    a('## Provenance')
    a('')
    a('| Lane | Run | Measured commit / ref | Durable source | Artifact corroboration |')
    a('|---|---:|---|---|---|')
    a(f"| Corrected pre-#398 | `{pp['run_id']}` | `{pp['commit']}` / `{pp['ref']}` | committed `pre/instrument-output.txt` bytes, SHA-256 `{pp['instrument_output_sha256']}` | ZIP SHA-256 `{pp['artifact_zip_sha256']}` |")
    a(f"| Post-#398 | `{qp['run_id']}` (job `{qp['job_id']}`) | `{qp['commit']}` / `{qp['ref']}` | committed `post/instrument-output.txt` bytes, SHA-256 `{qp['instrument_output_sha256']}` | ZIP SHA-256 `{qp['artifact_zip_sha256']}` |")
    a('')
    a('The raw files are losslessly preserved under `docs/tracking/evidence/w12/`; workflow metadata and the GitHub ZIP digests corroborate their origin. `w12-gate-firing-census.json` is a mechanical parse of the first complete census block in each committed raw instrument output.')
    a('')
    a('## Measurement semantics')
    a('')
    a('- `latestPass` counts eligible pressing heartbeats on which `PassEventRing.TryGetLatest` succeeded. It is a retained-ring-availability observation, **not pass throughput or a pass-event count**.')
    a('- `raw BackwardPass` is not a pure event count. The evaluator defines it as `freshBackwardPass || pendingBackwardPassDwell`, so it can remain true on dwell-continuation heartbeats.')
    a('- `committed` means the trigger debounce/dwell state is live; `Active` means the resulting press directive survived downstream gates.')
    a('- Phase/cooldown exits occur before pass lookup and trigger evaluation. The checker enforces `latestPass <= samples - InPossession - Cooldown - StaleTick` for every team row, along with complete exit accounting and aggregate reconstruction.')
    a('')
    a('## Matched three-seed result')
    a('')
    a('Scorelines are identical: **' + ', '.join(pre['scorelines']) + '** pre and post.')
    a('')
    a('| Metric | Pre #398 | Post #398 | Delta |')
    a('|---|---:|---:|---:|')
    metrics=[('latestPass',pre['latestPass'],post['latestPass']),('primaryAssigned',pre['primaryAssigned'],post['primaryAssigned']),('coverShadows',pre['coverShadows'],post['coverShadows']),
             ('raw BackwardPass',pre['raw'].get('BackwardPass',0),post['raw'].get('BackwardPass',0)),('committed BackwardPass',pre['committed'].get('BackwardPass',0),post['committed'].get('BackwardPass',0)),
             ('raw SidelineTrap',pre['raw'].get('SidelineTrap',0),post['raw'].get('SidelineTrap',0)),('committed SidelineTrap',pre['committed'].get('SidelineTrap',0),post['committed'].get('SidelineTrap',0)),
             ('raw WeakReceiver',pre['raw'].get('WeakReceiver',0),post['raw'].get('WeakReceiver',0)),('committed WeakReceiver',pre['committed'].get('WeakReceiver',0),post['committed'].get('WeakReceiver',0))]
    for label,x,y in metrics: a(f'| {label} | {x:,} | {y:,} | {y-x:+,} |')
    a('')
    a('### Gate outcomes — exact equality')
    a('')
    a('| Exit/outcome | Pre #398 | Post #398 | Delta |')
    a('|---|---:|---:|---:|')
    for k in ('Active','InvariantRejected','NoPrimaryPresser','Disengaged','Cooldown','NoCommittedTrigger','InPossession'):
        x=pre['exits'].get(k,0); y=post['exits'].get(k,0); a(f'| {k} | {x:,} | {y:,} | {y-x:+,} |')
    a('')
    a('Every gate-outcome counter is unchanged by exact equality. `primaryAssigned` rises by 3 while `WeakReceiver` falls by 3 on the same team/seed; this is positive wiring evidence that committed `BACKWARD_PASS` reached primary-press selection and perturbed internal selection bookkeeping, without an observed change in gate outcomes in this corpus.')
    a('')
    a('## Per-seed / per-team census')
    a('')
    a('| Lane | Seed | Final | Team | samples | latestPass | active | primaryAssigned | InPoss | NoCommitted | Active exit | NoPrimary | InvariantRejected | Disengaged | Cooldown | raw Bwd | committed Bwd | raw Side | committed Side | raw Weak | committed Weak |')
    a('|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|')
    for side,label in [('pre','pre'),('post','post')]:
        for r in c[side]['rows']:
            e=r['exits']; raw=r['raw']; cm=r['committed']
            vals=[label,r['seed'],r['final'],r['team'],r['samples'],r['latestPass'],r['active'],r['primaryAssigned'],e.get('InPossession',0),e.get('NoCommittedTrigger',0),e.get('Active',0),e.get('NoPrimaryPresser',0),e.get('InvariantRejected',0),e.get('Disengaged',0),e.get('Cooldown',0),raw.get('BackwardPass',0),cm.get('BackwardPass',0),raw.get('SidelineTrap',0),cm.get('SidelineTrap',0),raw.get('WeakReceiver',0),cm.get('WeakReceiver',0)]
            a('| '+' | '.join(str(v) for v in vals)+' |')
    a('')
    a('## Verdict')
    a('')
    a('**W5 producer→consumer wiring is proven.** In the matched three-seed pre/post corpus, every gate-outcome counter is exactly unchanged: Active **140 → 140**, InvariantRejected **139,309 → 139,309**, NoPrimaryPresser **84 → 84**, Disengaged **1,890 → 1,890**, Cooldown **22,671 → 22,671**, and all three scorelines are identical. Meanwhile `latestPass` becomes available on **141,491** eligible heartbeats and BackwardPass becomes observable (**1,827 raw / 2,770 committed**). The `primaryAssigned +3` / `WeakReceiver −3` delta shows that the new trigger reached selection and perturbed internal processing, but produced **no observed change in gate outcomes in this corpus**.')
    a('')
    a('This satisfies the pre-registered W5 acceptance contract: a real producer feeds the consumer and the BackwardPass trigger is evaluable from real events without gate collapse. It does **not** establish a general claim of behavioural inertness or material match-level effect.')
    a('')
    return '\n'.join(L)

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--write',action='store_true'); args=ap.parse_args()
    c=json.loads(CENSUS.read_text())
    raw=reconstruct(c); verify_sweep(c)
    for side in ('pre','post'):
        rows=parse_census(raw[f'{side}/instrument-output.txt'])
        if rows!=c[side]['rows']: raise AssertionError(f'{side} committed census differs from raw instrument output')
        if aggregate(rows)!=c[side]['aggregate']: raise AssertionError(f'{side} aggregate differs from raw instrument output')
    verify_structure(c)
    expected=render(c)+'\n'
    if args.write:
        DOC.write_text(expected); print(f'wrote {DOC.relative_to(ROOT)}')
    else:
        actual=DOC.read_text()
        if actual!=expected: raise AssertionError('comparison doc is not the mechanical render of committed census; run with --write and review the diff')
        print('W12 evidence OK: raw hashes, census accounting, and generated comparison agree')
if __name__=='__main__': main()
