#!/usr/bin/env python3
"""Verify the durable PR #439 evidence-ref archive."""

from __future__ import annotations

import argparse
import csv
import subprocess
from collections import Counter
from pathlib import Path

ROOT = Path("docs/tracking/evidence/pr439-ref-archive")
MANIFEST = ROOT / "MANIFEST.tsv"
DISPOSITION = ROOT / "ref-disposition.tsv"
RUN_HEADS = ROOT / "run-heads.tsv"
EXPECTED_MANIFEST_ROWS = 54
EXPECTED_RUN_ROWS = 21
EXPECTED_HEADS = {
    "evidence/pr439-discipline-seed-probe": "142b9f98f68e8c5d9b4a8e21e61f44efd931f354",
    "evidence/pr439-header-reachability": "7494300341cae94ed3eae41033d874e23cac0b06",
    "evidence/pr439-m7-order-mutation": "58cf6ec88e33054c069ef8e2914ce395fd2d76f1",
    "evidence/pr439-season-save-logscope-focused": "d6b6a565b064d0471763d7792ddce3eeacaf6f04",
    "evidence/pr439-seasonsave-focused": "6214a6039a5e6ff6ca2c225cf1394827c9d63de9",
    "evidence/pr439-seasonsave-focused-v2": "300aae34349493b4a111659a1ac4aa58d9b72fce",
    "evidence/pr439-w2-diag-base": "8b4bf0204179e0824a37a67ac9818d7674975a98",
    "evidence/pr439-w2-diag-head": "b15366ced301f988af3b72817839b6f463ce1e71",
    "evidence/pr439-w2-diag-w3": "fe99bae58045151c91653dc0c8da90b8679c35c8",
    "evidence/pr439-w3-pre-w6-six-seed": "d8b362d4c84b9d8fb461ffea66187f50a8780033",
    "evidence/pr439-w3-prearm-20260922": "b8b59a41fe56cf6a5ac67e952435cfb8a0707317",
    "evidence/pr439-w3-prewire-six-seed": "cf36526ce6ecd64632133780ed98ca0afe46c987",
    "evidence/pr439-w3-six-seed-20260922": "7347b0452d8143c5b540575b4db1f882302f1988",
    "evidence/pr439-w3-six-seed-rerun-20260923": "3f391fed45795a9c4f22bc601e187dd2cbe0e39a",
    "evidence/pr439-w6-postfix-focused": "495f9b0ed2eef06b6dffb7f3b09e4e9e0eaae0f3",
    "evidence/pr439-w6-prefixtest": "a6cfa40703c74ca42848ae1deaa44ba99f61fe4d"
}
EXPECTED_RUN_IDS = [
    "35813627716",
    "35814050060",
    "35814099718",
    "35814523589",
    "35814675549",
    "35814874360",
    "35814941362",
    "35815215062",
    "35815420707",
    "35815667045",
    "35815761065",
    "35878618102",
    "35878656931",
    "35878699989",
    "35878836477",
    "35879276984",
    "35879288048",
    "35887103114",
    "35887451692",
    "35887487201",
    "35925236129"
]

def read_tsv(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))

def git_blob(repo: Path, path: str) -> str | None:
    p = subprocess.run(["git","-C",str(repo),"rev-parse","--verify",f"HEAD:{path}"],capture_output=True,text=True)
    return p.stdout.strip() if p.returncode == 0 else None

def main(argv: list[str] | None = None) -> int:
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo", default=".")
    args=ap.parse_args(argv)
    repo=Path(args.repo).resolve()
    errors: list[str]=[]
    manifest=read_tsv(repo / MANIFEST)
    dispositions=read_tsv(repo / DISPOSITION)
    runs=read_tsv(repo / RUN_HEADS)

    if len(manifest) != EXPECTED_MANIFEST_ROWS:
        errors.append(f"manifest rows {len(manifest)} != {EXPECTED_MANIFEST_ROWS}")
    if len(dispositions) != len(EXPECTED_HEADS):
        errors.append(f"disposition rows {len(dispositions)} != {len(EXPECTED_HEADS)}")
    if len(runs) != EXPECTED_RUN_ROWS:
        errors.append(f"run rows {len(runs)} != {EXPECTED_RUN_ROWS}")

    paths=set()
    counts=Counter()
    refs=Counter()
    for row in manifest:
        kind=row.get("kind","")
        if kind not in {"current","history"}:
            errors.append(f"unexpected manifest kind {kind!r}")
        counts[kind]+=1
        ref=row.get("source_ref","")
        refs[ref]+=1
        path=row.get("archive_path","")
        if not path or path in paths:
            errors.append(f"duplicate/empty archive path {path!r}")
        paths.add(path)
        if not path.endswith(".txt"):
            errors.append(f"snapshot is not quarantined as .txt: {path}")
        blob=git_blob(repo,path)
        if blob != row.get("blob_sha",""):
            errors.append(f"blob mismatch {path}: {blob} != {row.get('blob_sha','')}")
        if not row.get("source_head") or not row.get("commit_subject") or not row.get("commit_date"):
            errors.append(f"incomplete provenance row for {path}")

    disp_by_ref={r.get("ref",""):r for r in dispositions}
    if set(disp_by_ref) != set(EXPECTED_HEADS):
        errors.append("disposition ref set does not match expected 16 refs")
    for ref,head in EXPECTED_HEADS.items():
        row=disp_by_ref.get(ref)
        if not row:
            continue
        if row.get("current_head") != head:
            errors.append(f"{ref}: current head {row.get('current_head')} != {head}")
        if row.get("disposition") != "deletable" or row.get("delete_now") != "false":
            errors.append(f"{ref}: archive PR must remain deletable/delete_now=false")
        current=sum(1 for m in manifest if m.get("source_ref")==ref and m.get("kind")=="current")
        history=sum(1 for m in manifest if m.get("source_ref")==ref and m.get("kind")=="history")
        if row.get("archived_current_files") != str(current) or row.get("archived_history_files") != str(history):
            errors.append(f"{ref}: disposition counts do not match manifest")

    run_ids=[r.get("run_id","") for r in runs]
    if sorted(run_ids) != EXPECTED_RUN_IDS:
        errors.append("run ID ledger does not match expected 21 Actions runs")
    if len(set(run_ids)) != len(run_ids):
        errors.append("duplicate run IDs")
    manifest_runs={r.get("run_id","") for r in manifest if r.get("run_id","")}
    if not manifest_runs.issubset(set(run_ids)):
        errors.append("manifest cites run IDs absent from run-heads.tsv")
    for row in runs:
        if not row.get("head_branch") or not row.get("head_sha") or not row.get("commit_subject") or not row.get("commit_date"):
            errors.append(f"run {row.get('run_id','')}: incomplete provenance")

    if errors:
        for e in errors:
            print("ERROR:",e)
        return 1
    print(f"PR439 evidence-ref archive: PASS (refs={len(dispositions)} snapshots={len(manifest)} current={counts['current']} history={counts['history']} runs={len(runs)})")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
