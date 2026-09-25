# PR #439 Evidence-Ref Archive

> **Created:** September 23, 2026
> **Purpose:** Preserve every branch-exclusive changed-file blob state and cited workflow-run head needed to make the PR #439 evidence refs disposable without losing the evidence record.
> **Status:** **ALL 16 EVIDENCE REFS AND THE SEPARATE SUPERSEDED #446 ARCHIVE BRANCH REMOVED; ZERO-REF GATES PASSED.**

## Archived scope

This archive records:

- all **16** former `evidence/pr439-*` refs and their exact observed remote heads in `ref-heads.tsv`;
- all **39** branch-exclusive changed-file blob states across those refs in `MANIFEST.tsv`, each copied byte-for-byte under `history/**.txt`;
- all **15** GitHub Actions run ids cited on PR #439 in `run-heads.tsv`, including exact run branch, run head SHA, event, conclusion and workflow name;
- an explicit `ref-disposition.tsv` in which every ref is classified `deletable` and all 16 `delete_now` values are now `true` together.

The archive includes intermediate states, not only branch tips. In particular it preserves the pre-W3 instrument/parser/workflow lineage `6f2ed037…` → `ccb7bf67…` → `cf36526c…`, the W6 pre-fix discriminator source, all three W2 diagnostic arms, the M7 mutation proof, and the full #441 header-reachability evidence lineage.

The result-bearing six-seed W3 tables are preserved separately in `../pr439-w3/`.

## Later historical evidence closeout

`superseded-runs.tsv` is a separate, eight-row ledger for runs in the superseded
PR #446 attempt that appear neither in the 15-run PR #439 citation ledger nor elsewhere
in `main` at the time of this closeout. It records each outcome, evidence head,
production base, workflow, artifact disposition, and reason for supersession. It is
**not** an extension of `run-heads.tsv`; the PR #439 citation equality remains exactly
the same 15 run IDs.

Run `35814941362` was a successful, alternate pre-W3 characterization against the
same production base `876a3343319050187c2a5505b18cb32fc3d0f89d` as the
authoritative run `35815761065`. Its instrument and parser are materially different:
test blobs `380f9ee34b7ff629b2c57172842437b31e51a25e` versus
`5f2c30f4eda2f05ace6e72a8e61f9432946fd0be`, parser blobs
`e81987356b4a9483d7d6e47f14e17384ca3c179c` versus
`f8b5fa4b4c8f5b8d3644e5a52eee31573ef4d779`, respectively. The alternate run's
entire 175,296-byte artifact ZIP is retained under `historical-artifacts/`; its
SHA-256 `981600d85ddcb67cd0d313ed698e4f1a6db5c70cce1e753b80a84600a7497571`
matches GitHub's live artifact digest. All seven payload entries match the ZIP's own
`SHA256SUMS`. GitHub artifact `10731182039` expires `2026-12-22T03:35:34Z`.
This archive permits later forensic comparison without treating the alternate output
as the governing pre-W3 table.

`archived-job-logs.tsv` identifies six complete decoded Actions job logs retained
losslessly as `.txt.xz`: five W2 three-arm diagnostic jobs and the M7 order-mutation
job. These are API-delivered decoded logs, with both compressed and decoded SHA-256
and decoded line counts recorded. All five W2 runs uploaded no artifacts. Their
quantitative interpretation remains in `w3-agent-ball-fanout-design.md` §6.3;
the logs preserve the challenge/summary lines behind it. The M7 log preserves the
target failure (`Expected: 2`, `But was:  1`) and the explicit mutation-proof
success line. Use `xz -dc job-logs/<name>.txt.xz` to inspect a full log.

`historical-workflows.tsv` records two exact Git-blob copies under
`historical-workflows/`: the failed first foul/card harness at `7a45f57f…` and
the W2 #442 six-seed evidence workflow at `2b3bcb29…`. The result-bearing foul/card
harness is already preserved separately at `../foul-card-six-seed/harness-50d6229.yml`.

### Retention decisions

- The foul/card successful package on `main` retains exact compressed TRX,
  report, TSV/JSON, parser and successful harness, plus hashes of the original
  raw streams. Its original `instrument-output.txt` and `measurement.txt` are
  optional forensic detail and are not newly duplicated here.
- The older W2 six-seed run `35389986678` has 13 live artifacts expiring
  `2026-12-17T20:10:23Z`. `../w2-six-seed/README.md` explicitly accepts that
  artifact retention boundary; this closeout does not change its disposition.
- The alternate pre-W3 ZIP above is retained because it captures a distinct,
  successful evidence revision that the published authoritative table supersedes.

## Verification

`tools/dotnet-ci/check_pr439_evidence_refs.py` owns committed archive integrity and the post-delete live-ref certification path. The historical pre-delete proof is preserved by the committed manifests and Git history. `.github/workflows/pr439-evidence-ref-gate.yml`:

1. fetches full history and any live `evidence/pr439-*` refs;
2. verifies every archived `.txt` blob equals the recorded original Git blob and hashes the added historical artifact, workflows and six full decoded job logs;
3. recognizes the exact 16-ref pre-delete topology at recorded heads and the authorized zero-ref post-delete topology; its automatic CI invocation now requires zero refs, and any partial topology fails;
4. in pre-delete mode, re-derives every branch-exclusive changed-file blob state and requires the live 39-state set to match the manifest exactly; in post-delete mode, checks the committed 39-state archive without relying on disappearing branch objects;
5. verifies that the 15-run ledger exactly covers the workflow-run ids cited on PR #439 and cross-checks each cited run against the GitHub Actions API. The separate eight-run historical ledger does not enter that equality check.

The workflow requires `delete_now=true` for all 16 rows and defaults to
explicit `post-delete` on automatic and manual runs. Its green result certifies
that no `evidence/pr439-*` ref remains. The CLI still exposes the historical
`pre-delete` selector, but after the 2026-09-25 deletion it cannot pass against
the live remote; historical transaction inputs are audited from the committed
archive/manifests and Git history. Automatic CI rejects ref reappearance.

The one-shot `pr439-evidence-atomic-delete.yml` action ran on PR #451's landing
to `main` (merge `250b15fec3b98b5d9fece6e9db1e0d787bd623fd`). It ran
the complete authorized pre-delete gate, compared all 16 remote heads,
completed an atomic dry run, and submitted one `git push --atomic` with an exact
expected-head lease for every deletion. Run `36078635104` passed the immediate
zero-ref check and dispatched a separate explicit post-delete gate rerun
`36078666318`, which also passed. This one-shot workflow is now retired from
the maintained tree. PR #452 (merge `df63ff91a989b9e5272de6936a45de659ca178b7`)
made the automatic gate require zero refs. Its separate cleanup run
`36080270280` first passed the full post-delete verifier, then removed only
the superseded `archive/pr439-evidence-20260923` branch at recorded head
`d8f61356ba5b45debe04371343a5e2715d493ede`. The same merge's automatic
evidence gate run `36080270275` passed. The completed cleanup workflow has
also been retired from the maintained tree.

Final post-merge `main` CI run `36080899633` on merge `2e02b32bc1b2350f57d44dc683b91502bb794e94` completed successfully on 2026-09-25, including the functional gate.

## Residual evidence-branch dispositions

- `evidence/foul-card-six-seed-20260922` may be deleted only while it still points at recorded head `7db673cacf7fb24d1db0b8340cbc3ef5e3821daf`. Its branch-only workflow history is already preserved; do not rewrite the hash-covered `foul-card-six-seed/` payload to record ref deletion.
- `evidence/w2-442-six-seed-measure-20260923` may be deleted only while it still points at recorded head `2b3bcb290c65fb50fd999033055e39635bc325f8`. Its branch-only workflow is archived and its preregistration commit remains in `main` history.
- Deleting either branch ref does not delete its retained GitHub Actions runs. These two refs are outside the fixed 16-row PR439 `ref-disposition.tsv` contract and must not be added to that ledger.


## Deletion boundary

PR #447 did **not** delete evidence branches or authorize deletion. PR #451
preserved the remaining historical evidence, authorized all 16 rows together,
and landed the guarded one-shot action. Both the one-shot job and the separate
explicit post-delete run passed, and the live remote now has zero
`evidence/pr439-*` refs. The superseded `archive/pr439-evidence-20260923`
branch was excluded from that atomic operation and removed independently by
run `36080270280` after the explicit post-delete certification.
