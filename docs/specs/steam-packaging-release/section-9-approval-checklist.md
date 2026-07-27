# Steam Packaging & Release Engineering #39 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — PASS-1 fix pass recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Completeness

- [x] §1 scope, dependencies, the five verified facts, KD-1..KD-7, staging.
- [x] §2 FR-PK-001..045, data structures, failure modes F1..F9.
- [x] §3 FM-PK-01..04 with fifteen hand-verifiable worked examples.
- [x] §4 architecture — the leaf-but-for-#50 assembly, the release gate as a process, the Cloud path.
- [x] §5 test plan, incl. the fail-closed property tested **as a property**, the conflict matrix with its
      metadata-refusal lock, observer-neutral achievements, and **§5.7's statement of what cannot be
      tested before an artifact exists**.
- [x] §6 performance — no loop path; budgets in seconds and minutes, deliberately.
- [x] §7 T-phase plan, extensions, risks R-1..R-6.
- [x] §8 cross-references XC-039-001..016 and **no back-propagations at approval**.
- [x] Appendices A (constants), B (the release-gate evidence set), C (the Cloud conflict matrix),
      D (the store/compliance checklist).

## 9.2 Constant discipline

- [x] Every constant carries exactly one tag; Appendix A is the single catalogue.
- [x] No `[EST]` constants.
- [x] **No `[CROSS-PENDING]` constants** — #39 takes no determinism reservation (KD-7), so nothing is
      blocked on an upstream allocation.
- [x] **No `[GT]` constant affects the simulation.** #39's `[GT]` values are four process/latency
      ceilings. None is read by any sim assembly, none reaches a digest, and none is serialized.
      Verifiable in Appendix A.
- [x] `REQUIRED_EVIDENCE` is `[FIXED]`, not `[GT]`: a **tunable required-evidence set is a gate that can
      be turned off**, which is precisely what a fail-closed gate must not be (FR-PK-011).

## 9.3 Determinism discipline

- [x] No RNG stream, no domain tag, no `SubsystemOrdinal`, **no `_RESERVED_` placeholder** (FR-PK-041).
- [x] Nothing is serialized into any sim save; no format version anywhere (FR-PK-042).
- [x] Achievement evaluation is **observer-neutral**, and true **by construction** — #39 adds no event,
      hook or sim field (FR-PK-021/045).
- [x] **No sim assembly reads achievement state** (FR-PK-027) — the one #39 violation that would be a
      determinism defect rather than a packaging bug, asserted by a **reverse**-reference scan.
- [x] Identity: Cloud off + no achievements ⇒ save behaviour byte-for-byte today's local path
      (FR-PK-044).

## 9.4 Gates

| Gate | Status |
|---|---|
| **G1** — section-file PASS-1 adversarial review + fix pass | **CLOSED** — see §9.4.1 |
| **G2** — back-props filed at approval | **N/A — there are none** (§8.2), stated as a positive finding rather than left as an empty table |
| **G3** — lead-developer R-01..R-05 sign-off | **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `[GT]` balance pass | **N/A** — #39 has no behavioural magnitude. The four `[GT]` rows are process ceilings; `PK_BUDGET_SMOKE_PATH_MIN` wants **justification** on overrun rather than tuning (§6.3) |

### 9.4.1 PASS-1 adversarial review — **0H + 4M + 5L**, all resolved

**M-1 — the gate existed only as a runbook, which made its central property untestable.** The supplement
specifies the evidence set and the fail-closed posture but describes the gate as a process — and a process
cannot be unit-tested, so *"a skipped check fails the gate"* would have been verifiable only by attempting
a release. That is the worst possible moment to discover the posture is wrong. Resolved as **FR-PK-020**:
the gate MUST be evaluable as a **pure function of an evidence set**, with the runbook producing that set.
§5.1 exists because of this fix, and T-PK-U-003 locks the property against the **real** skip-shaped CI
artifact.

**M-2 — nothing barred a conflict outcome from destroying an unchosen copy, and nothing said what happens
when sync and a save collide.** KD-1's table says what to *select* in each case and KD-5 says *when* to
sync, but the two concrete ways a second writer loses a career — overwriting the copy the player did not
pick, and syncing over a file being written — were each covered only by implication. Resolved as
**FR-PK-009** and **FR-PK-036 / F7**.

**M-3 — the platform-is-truth rule had no flush obligation behind it.** KD-3 correctly makes the platform
the store of record and the local file a queue, but nothing stated that a pending unlock flushes **exactly
once** and that an already-held unlock is **not re-granted** — which is the entire pair of defects
(double-grant, lost unlock) a queue-plus-platform design exists to avoid. Resolved as **FR-PK-025 / F8**;
locked by T-PK-I-009/010.

**M-4 — §5 did not say which of its own tests could be run.** R-5 warns that a gate never exercised is a
document, but the test plan listed everything uniformly, so a reader would reasonably assume the whole
suite was live. In fact the two artifact-side rows cannot exist until the first player build does.
Resolved as **§5.7**, which classifies every suite by when it becomes exercisable and names the two
`T-PK-PROC-*` rows explicitly.

**L-1** — `EvidenceRecord` needed `Executed` as a field **separate** from `Passed`; a single "did it pass"
boolean is satisfied by a skip and would reproduce, in the data model, the exact CI defect §1.4(b)
identifies. **L-2** — §3.1's fold had to state that `REQUIRED_EVIDENCE` is the iterated collection:
walking the record list instead returns `Pass` for an empty set. **L-3** — the commit check had to be
scoped to **input-side** rows only, or the smoke path (which binds by construction) becomes un-runnable;
T-PK-U-006 locks the scoping. **L-4** — the four `[GT]` budget ceilings declared in §6.3 were **absent
from the Appendix A catalogue** (the #45 PASS-1 M-2 defect, now seen for the **tenth** time in this wave);
added. **L-5** — §8.4 needed the row recording that #39 asks for **no CI change**, since the obvious "fix"
for the skip-open finding is to make CI fail without a licence — which would make every contributor
without secrets see red for no benefit. The defect is not that CI skips; it is that a ship decision would
read CI's answer.

**AR-2 sweep — 0H + 0M + 2L → CONVERGENCE** (an L-only round closes the cycle, per project convention).
The sweep re-walked every claim about *what is measured on what*, since the supplement's own AR-2 and AR-3
were both instances of that error class and it is demonstrably the highest-yield thing to re-check here.
**L-6:** §6's cost table listed achievement predicate evaluation without attributing it to the **shell**,
which would have read as a #39 per-event cost and quietly contradicted FR-PK-022. **L-7:** §8.5 did not
say why platform requirements carry no citation; pinning externally-versioned store clauses into an
approved spec would guarantee it is wrong at some future date while looking authoritative.

## 9.5 Verification anchors

Every claim below is checkable against a named file; none is a summary of another summary.

| Claim | Anchor |
|---|---|
| There is no build pipeline at all | `.github/workflows/ci.yml` + a tree-wide `BuildPlayer` search (§1.4(a)) |
| CI is skip-open | `unity-tests`' `if:` condition + `unity-license-check`'s notice (XC-039-002/003) |
| The platform pin is certified | `certification-platform.md` v1.4 (XC-039-004) |
| The KAT and perf baseline exist | `cert-runs/determinism-cert-2026-07-19.md`; `kickoff-multi-second.cert.md` (XC-039-005/006) |
| The Linux gate is non-certifying | `cert-run-runbook.md`, in its own words (XC-039-008) |
| The save is one atomic file with opaque sub-blobs | `SeasonSaveCodec` (XC-039-009) |
| #50 already names #39 as its consumer | #50's `CompareForConflict`, direction `#50 → #39` (XC-039-012) |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **none — and §8.2 records that as a positive property** (`spec-error-log.md` v1.47). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #39 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** G1 is closed and **G2 is N/A — #39 files no back-props at all**. **G3 remains
open** and cannot be closed by the author: lead-developer R-01..R-05 sign-off is a human authority, not
self-grantable, per the promotion pipeline. The spec does **not** claim `APPROVED`.

**One question should be settled at review, because it is a policy decision rather than an engineering
one: the supported posture toward waiving the gate.** Every rule in this spec costs something at the exact
moment it is least welcome — the gate blocks a ship, the conflict policy refuses a sync, the smoke path
adds ten minutes (R-6). The spec deliberately provides **no waiver mechanism**, because a documented
waiver becomes the normal path. If the project wants one, it should be designed at review with an
explicit owner and an explicit record, rather than improvised on a release night — which is the only other
way it will ever exist.

**A second, smaller note for the reviewer:** #39 is the **last spec in the authoring wave** and the one
whose subject — shipping — does not yet exist. §5.7 is deliberately explicit that two of its test classes
cannot run until the first player build, and that gap is a property of the project's stage, not a defect
in the spec. **The right time to re-read §5.7 is when the first artifact is built**, and the checklist
records that so the re-read is scheduled rather than remembered.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial checklist. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 recorded: **0H + 4M + 5L**, all fixed in the v0.2 files; AR-2 sweep **0H + 0M + 2L → CONVERGENCE**. The four M were: the gate existing only as a runbook, making its central property untestable until a release was attempted (now a pure fold, FR-PK-020); no bar on a conflict outcome destroying an unchosen copy and no rule for sync colliding with a save (the two concrete ways a second writer loses a career); the platform-is-truth rule having no flush-exactly-once obligation behind it; and §5 not stating which of its own tests could actually be run before an artifact exists. §9.6 raises the waiver question explicitly, on the ground that an improvised waiver on a release night is the only other way one will ever exist. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **none — and §8.2 records that as a positive property** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.47). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
