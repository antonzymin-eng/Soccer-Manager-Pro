# Remote branch cleanup disposition

**Created:** October 6, 2026  
**Purpose:** Record deletion candidates, preserve cited provenance, and track retained work branches.  
**Version:** 0.2  
**Status:** PROPOSED — owner authorization pending; no archive tags created and no candidate branches deleted.

## Audited snapshot

Repository: `antonzymin-eng/Soccer-Manager-Pro`. Authoritative remote main:
`4103e6204e58542d4c3d5fca676f68b35240cc79`. The snapshot contains 22 branches.
The remote tag inventory was empty when checked. This record proposes eight deletions
(six without a new tag, two only after verified archive tags), with fourteen retained
branches. A branch opened to review this record is outside this snapshot.

The five merged candidates were checked through authoritative GitHub comparisons
against that main OID: each has zero commits ahead and its tip equals the merge-base.
This satisfies the remote/API alternative in the
[branch cleanup rule](../agent-guides/project-reference.md). No local ancestry claim
is made from a shallow checkout.

| Branch | Audited remote head | Disposition / next review |
|---|---|---|
| `BLOCKED-codex/a3.4-reapproval` | `ca344beb585366255f15faacf8ffbdf5ad474738` | Keep: 5 branch-only commits; owner to assign A3.4 author/reviewer at the next A3.4 reapproval review. |
| `BLOCKED-ui/p5b-shell-foundation` | `509a9f5dc43878d134d4356146bc313ebe8062b5` | Delete after authorization and fresh ancestry/head check; merged PR #470. |
| `claude/fervent-ride-gmknin` | `f7f44b513f5b496ef0d9b2853c2920e49aeb6660` | Archive to annotated tag `archive/fervent-ride-gmknin`, verify remote tag, then delete after authorization; cited provenance, no PR. |
| `codex/spec20-unity-verification-drift` | `97966ddc5c8208a9f4dbd503a054cbf2ffb2cdbc` | Keep: 9 branch-only commits; owner to assign #20 reviewer at the next A3.4/KD-4 modernization review. |
| `codex/trace-variable-in-positioning-ai-system` | `7b39161dbe101805e019bcba1e789b360430c431` | Keep: open draft PR #434; PR author/reviewer resolves its disposition when #434 closes. |
| `docs/p5b-lifecycle-identity-plan` | `705b5ff91eec5b98f490e2e53063a773fc647929` | Delete after authorization and fresh ancestry/head check; merged PR #479. |
| `evidence/a1c-green-arm` | `d689f2bfd2823f72fa646c0a3b92a2541fce3657` | Keep: A1c enforcement record explicitly prohibits deletion; project owner must resolve that policy before reconsideration. |
| `evidence/a1c-red-arm` | `d497a4d4c7248acc2d7c935cd7df7480f7956334` | Keep: A1c enforcement record explicitly prohibits deletion; project owner must resolve that policy before reconsideration. |
| `evidence/pr416-close-chance-retirement` | `adcf21bf36c07273f082053d3778693f4075bf2a` | Keep: PR416 ref-disposition ledger marks policy-retained/delete_now=false; project owner must resolve that policy before reconsideration. |
| `evidence/pr416-narrow-rolling-candidate` | `bb501a2128f9efbef5e98bffadb0d98214493e78` | Keep: PR416 ref-disposition ledger marks policy-retained/delete_now=false; project owner must resolve that policy before reconsideration. |
| `evidence/pr416-state-only-preforce-candidate` | `efa2f8946a9a6a8852946b97a8e4c7d55013b0bf` | Keep: PR416 ref-disposition ledger marks policy-retained/delete_now=false; project owner must resolve that policy before reconsideration. |
| `evidence/w8-stage-a-reclaim-split-20260926` | `73ca38bf8b6981a826946984284191d0253a6b84` | Keep: 3 branch-only commits; review reclaim-split harness and evidence before W8 archive cleanup. |
| `localization/l1-core-contracts` | `1b70f0dec10933509c381e3eda77352bd67d01a7` | Delete after authorization and fresh PR-ref check; superseded by #468 from the restart branch; closed unmerged PR #397 retains this exact head. |
| `main` | `4103e6204e58542d4c3d5fca676f68b35240cc79` | Keep: authoritative integration branch. |
| `measure/w8-clock-correction-six-seed-20260926` | `3aea8a4bf84ac2186742c296a2e572db0cfe8f2e` | Keep: 2 branch-only commits; review six-seed clock-correction measurement before W8 archive cleanup. |
| `measure/w8-possession-helper-equivalence` | `ba2c8e5b6022403f008ab3dcaf85ed32f0a435ce` | Keep: 3 branch-only commits; review possession-helper equivalence harness before W8 archive cleanup. |
| `measure/w8-stage-a-baseline` | `b6967b49eabfd836a5a330b90f4f1deaeddfa202` | Keep: 1 branch-only commit; review baseline workflow/evidence before W8 archive cleanup. |
| `measure/w8-stage-a-validation` | `ebc9de047bc141ddb0231bc6843a7aa6e481d348` | Keep: 1 branch-only commit; review validation workflow/evidence before W8 archive cleanup. |
| `ux/s0-gate-i-contract-completion` | `c6c1a105ed277e84ab20c08576f85657544b3726` | Delete after authorization and fresh ancestry/head check; merged PR #478. |
| `ux/s0-gate-i-implementation-handoff` | `15865e0f0e80702a5f12717fc45add2e5c97fe5f` | Delete after authorization and fresh ancestry/head check; merged PR #476. |
| `ux/s0-gate-i-owner-decisions` | `80f6d07aaa7933c8a501d9192bfe10ded078f5d4` | Delete after authorization and fresh ancestry/head check; merged PR #477. |
| `wiring/w12-evidence-repair` | `7dd81a9c842298927ac929d35e8caac12860a365` | Archive to annotated tag `archive/w12-evidence-repair`, verify remote tag, then delete after authorization; cited provenance, no PR. |

The seven deferred work branches (five W8, A3.4, and #20) remain unresolved.
The project owner is the disposition decision-maker and must assign a named reviewer
before the respective review trigger above; no assignment or acceptance is claimed
here. Review each branch for salvage, integration, or archive retention, then update
this ledger before proposing deletion. Keeping them is a deferral, not completion.

A1c retention is owned by
[a1c-enforcement-evidence.md](a1c-enforcement-evidence.md).
PR416 retention is owned by
[ref-disposition.tsv](evidence/pr416-ref-archive/ref-disposition.tsv).

## Provenance archive mapping

| Historical branch | Proposed annotated tag | Required peeled commit |
|---|---|---|
| `claude/fervent-ride-gmknin` | `archive/fervent-ride-gmknin` | `f7f44b513f5b496ef0d9b2853c2920e49aeb6660` |
| `wiring/w12-evidence-repair` | `archive/w12-evidence-repair` | `7dd81a9c842298927ac929d35e8caac12860a365` |

The W12 tip retains ancestor `add310c94adbe0b4f452dfa5ec7108df1a63eab8`.
An authoritative API comparison from that ancestor to the tip returned zero commits
behind, four ahead, and the ancestor itself as merge-base.

The W12 census and checker retain their historical `salvaged_from` branch identifier
verbatim. Do not change
[w12-gate-firing-census.json](evidence/w12/w12-gate-firing-census.json)
or `EXPECTED_SALVAGED_FROM` in
[check_w12_evidence.py](../../tools/dotnet-ci/check_w12_evidence.py).
This ledger and the [W12 manifest](evidence/w12/README.md) explain the intended
branch-to-tag transition. The stale comparison was deliberately not cherry-picked;
retaining provenance does not approve that comparison or restore obsolete bundle files.

Use the following annotation messages verbatim (with normal tagger metadata).

### archive/fervent-ride-gmknin annotation

```text
Archive historical branch claude/fervent-ride-gmknin before branch deletion.
Preserve f7f44b513f5b496ef0d9b2853c2920e49aeb6660 and its history as cited provenance.
Content disposition: corrected H v0.2 incorporates the relevant work; this tag
preserves history and does not reinstate the stale approval claim.

Files citing this commit:
docs/design/s0-high-fidelity/README.md
docs/design/s0-high-fidelity/verify.cjs
docs/design/s0-high-fidelity/evidence/v0.2/walkthrough.json
docs/design/ux-validation-protocol.md
docs/design/ux-s0-pm1-journey.md
docs/tracking/file-manifest.md
docs/tracking/open-issues.md
docs/tracking/CHANGELOG.md

Disposition record: docs/tracking/branch-cleanup-disposition.md
```

### archive/w12-evidence-repair annotation

```text
Archive historical branch wiring/w12-evidence-repair before branch deletion.
Preserve 7dd81a9c842298927ac929d35e8caac12860a365 and ancestor
add310c94adbe0b4f452dfa5ec7108df1a63eab8 as cited provenance.
PR #432 salvaged the census and reconciled the evidence. The stale comparison
was deliberately not cherry-picked; this tag preserves history, not approval.

Files citing these commits or the historical branch:
docs/tracking/evidence/w12/w12-gate-firing-census.json
tools/dotnet-ci/check_w12_evidence.py
docs/tracking/w12-preregistration-reconciliation.md
docs/tracking/evidence/w12/README.md
docs/tracking/w6-controlled-ball-closeout.md
docs/tracking/CHANGELOG.md
docs/tracking/file-manifest.md

Disposition record: docs/tracking/branch-cleanup-disposition.md
```

## Execution gates

1. Obtain the project owner's explicit authorization for the two annotated tags and
   the eight listed candidate deletions. Record the authorization link/date here.
   Land this disposition record and the W12 manifest note before deleting branches.
2. Refresh remote heads and main. Stop for any candidate whose head has changed.
   Repeat authoritative remote/API ancestry comparisons for the five merged branches.
   If using local Git instead, fetch full history and run
   `tools/dotnet-ci/check_branch_ancestry.py` against fresh remote-tracking main;
   a shallow clone or an indeterminate result is not accepted.
3. Recheck that `refs/pull/397/head` on the remote equals
   `1b70f0dec10933509c381e3eda77352bd67d01a7` before localization deletion.
   Confirm PR #397 remains the retained reference for the superseded branch.
4. Create annotated tags at the exact two commit OIDs above using the recorded
   annotation messages, and push those two tag refs explicitly. Do not replace a
   pre-existing tag: inspect and reconcile any collision before continuing.
5. Run `git ls-remote --tags origin` after pushing. For each tag require both its
   published tag-object OID and its `refs/tags/<name>^{}` peeled commit entry.
   The peeled commit must equal the required commit above. Fetch/inspect the tag
   object to confirm it is annotated and has the recorded message. Record the
   observed tag-object OIDs and peeled OIDs here. A locally existing tag alone
   is insufficient; an annotated tag's object OID differs from its commit OID.
6. Delete only the eight table candidates, each conditioned on its expected remote
   head. For Git use the explicit lease form below once per candidate; never use
   wildcard deletion. An equivalent API operation must reject a moved head.
7. Query the remote again: all eight candidate branch refs must be absent; the
   fourteen retained snapshot refs must still exist; both archive tags must still
   peel to the required commits, and PR #397 must retain its recorded head.
   Record actual results and any concurrently moved retained heads. Update the
   status, W12 manifest note, and CHANGELOG with the completed transition.

```bash
git push --force-with-lease=refs/heads/<branch>:<audited-head> origin :refs/heads/<branch>
```

Deletion of the two provenance branches is blocked independently until their
respective tag is verified remotely. Deletion of the six other candidates does not
depend on the archive tags but still requires authorization and its own fresh checks.

## Execution record

| Event | State / evidence |
|---|---|
| Owner authorization | Pending |
| Disposition record landed | Pending |
| Archive tag publication and remote verification | Not performed |
| Candidate branch deletions | Not performed |
| Post-delete remote verification | Not performed |
| Deferred-work reviewer assignments | Pending project-owner decision |

## Version history

| Version | Date | Change |
|---|---|---|
| 0.1 | October 6, 2026 | Initial audited dispositions, annotated archive messages, remote verification and authorization gates; execution pending. |
| 0.2 | October 6, 2026 | PR #480 review correction: W12 annotation adds the W6 pre-registration closeout provenance citation and the CHANGELOG/file-manifest references; execution remains pending. |
