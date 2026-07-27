# Steam Packaging & Release Engineering #39 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #39 has no `[EST]` constants and — because it takes **no determinism
reservation** (KD-7) — **no `[CROSS-PENDING]` constants either**, so neither region appears.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `REQUIRED_EVIDENCE` | the six `EvidenceKind` rows of Appendix B | `[FIXED]` | The gate's required set. **`[FIXED]`, emphatically not `[GT]`:** a **tunable required-evidence set is a gate that can be turned off**, which is exactly what a fail-closed gate must not be (FR-PK-011). Adding a row is a spec change with a review; removing one should be too. |
| `ACHIEVEMENT_NONE` | `0` | `[FIXED]` | `AchievementId.None` — a **refused** value, never a default. The zero value is defined as invalid rather than merely unset. |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `REQUIRED_EVIDENCE_COUNT` | `REQUIRED_EVIDENCE.Length` | `[DERIVED]` | Derived, **never a hand-maintained literal** — the `POSITION_COUNT` precedent. A lagging literal would let the fold check five of six rows and report `Pass`, which is the one arithmetic error in this spec that produces a **false green gate**. |
| `INPUT_SIDE_EVIDENCE_COUNT` | `REQUIRED_EVIDENCE.Count(IsInputSide)` | `[DERIVED]` | The commit-check scope (FR-PK-014). Derived so the input-side/artifact-side split cannot drift out of sync with the set — the drift that would either un-run the smoke path or leave a stale-commit row admissible. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `SaveClass`, `Classify`, `CompareForConflict` | **#50** | **The only source of version judgement** (FR-PK-001). #39 re-declares nothing and compares nothing. |
| The pinned platform tuple | `certification-platform.md` v1.4 | **Consumed, never re-pinned** (FR-PK-017). |
| The certified KAT record and the FR-PO-052 baseline | #16 / #18 | **Evidence inputs** (FR-PK-019). #39 defines no determinism proof. |
| The save file, as bytes | #30 + each sub-blob owner | **Transported, never parsed** (FR-PK-033). |
| `TextTemplateId`, `LocalizedTextRequest` | #49 | Used **only** inside the boundary adapter, which is not a #39 assembly (FR-LC-012). |
| The Steam SDK | platform | Bound **by the shell**. #39 declares `ICloudSyncPolicy` and holds no SDK type — which is what keeps #39 buildable and testable **without the platform**. |

### A.4 GT

| Constant | Value | Notes |
|---|---|---|
| `PK_BUDGET_GATE_EVAL_MS` | `10` | §6.3 ceiling for one `EvaluateGate` fold. |
| `PK_BUDGET_CONFLICT_RESOLVE_S` | `30` | §6.3 ceiling for fetch + classify + decide. **In seconds**: it contains a network round trip. |
| `PK_BUDGET_SYNC_S` | `60` | §6.3 ceiling for one whole-file upload. Dominated by save size and network, neither of which is #39's. |
| `PK_BUDGET_SMOKE_PATH_MIN` | `10` | §6.3 ceiling for the packaged-build smoke path. **In minutes**, and it is a budget on a *process* — an overrun is a signal to **justify the added steps, not to raise the number** (R-1a). |

**All four are ceilings, not measurements.** No certified number exists for #39 and none is invented here:
a certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #39 has no implementation — **or artifact** — to measure.

**No `[GT]` constant in this catalogue affects the simulation** (§9.2). All four are process and latency
ceilings: none is read by a sim assembly, none reaches a digest, and none is serialized. **#39 declares no
behavioural `[GT]` at all**, and therefore carries **no balance pass** — in a wave where most siblings do,
that absence is a classification rather than an omission.

## Appendix B — The release-gate evidence set

Six rows. **Each must be present and affirmative; absence is failure** (FR-PK-011).

| # | Evidence | Source | Side | Absent, skipped, or failed ⇒ |
|---|---|---|---|---|
| 1 | Determinism KAT run record on the pinned tuple | `cert-run-runbook.md` → `cert-runs/` | **input** | **FAIL** |
| 2 | FR-PO-052 perf baseline, **CERTIFIED** (not `PENDING`) | `CertifiedPerfBaseline` + the `.cert.md` record | **input** | **FAIL** |
| 3 | Unity EditMode + PlayMode results, **executed** | `unity-tests` **artifacts** | **input** | **FAIL** — *a skip is not a pass* (§1.4(b)) |
| 4 | Full `dotnet-ci` suite green, quarantine empty | `tools/dotnet-ci/run-gate.sh` | **input** | **FAIL** |
| 5 | **Packaged-build smoke path** (launch → career → save → quit → relaunch → load → advance a day) | the artifact itself | **artifact** | **FAIL** |
| 6 | Compliance subset of the checklist (Appendix D) | KD-4 | **input** | **FAIL** |

**Rows 1–4 and 6 measure the *project at a commit*; only row 5 measures the *packaged binary*.** That
asymmetry is the reason the commit binding (FR-PK-014) is a gate rule rather than hygiene: **the commit is
the only thing tying the input-side evidence to the artifact being shipped**, and an evidence record
naming a different commit is worse than a missing one because it looks like a pass (R-1).

**Row 5 is the only check that can catch a packaging-only failure** — a stripped assembly, a missing
asset, a broken path — that every project-side test passes straight through. It is deliberately small
(§6.3), because the alternative is a second test suite maintained in the worst possible environment.

**Row 3 is the row this whole appendix exists for.** `unity-tests` is gated on a secret and reports
success when that secret is absent, by design and correctly **for CI**. The gate reads its **artifact**
and requires `Executed == true`, which is the same job read with the opposite default — and is the entire
resolution of §1.4(b). **#39 asks for no CI change** (§8.4).

**Two things this set deliberately does not contain:**

1. **A bit-identical rebuild check** (FR-PK-038). Nobody has demonstrated one here, and gating on an
   undemonstrated property is how a gate becomes a formality. Behavioural reproducibility against the
   certified digests is row 1 (KD-6).
2. **Anything sourced from the Linux `tools/dotnet-ci` gate as *certification*.** Row 4 requires that
   suite green as a **necessary** signal; it is explicitly **non-certifying**, and a certified number
   sourced from it *"would be a fabricated certification"* in the runbook's own words (FR-PK-017).

## Appendix C — The Cloud conflict matrix

The authoritative form of KD-1. §5.2 walks it exhaustively.

**Precondition: both copies are local and classified from their bytes** (FR-PK-002). A row cannot be
evaluated from Cloud metadata, and attempting to is **`Refuse`**, not a guess (FR-PK-003 / F4).

| Local | Remote | Outcome | Why |
|---|---|---|---|
| any | `TooNew` | **`NoAction`** | This build cannot reason about a format it does not know. *"Newer wins"* is how the newer copy gets overwritten by a build that could not read it |
| `TooNew` | any | **`NoAction`** | Symmetric — and the symmetry matters: the local copy is not privileged |
| `Corrupt` / `Unsupported` | `Current` / `Migratable` | **`UseRemote`** | The bad copy is **neither selected nor deleted nor repaired** (FR-PK-007/009) |
| `Current` / `Migratable` | `Corrupt` / `Unsupported` | **`UseLocal`** | Symmetric |
| `Corrupt` / `Unsupported` | `Corrupt` / `Unsupported` | **`Refuse`** | Nothing to choose; both files left exactly as found |
| `Current` | `Current`, identical | **`UseLocal`** | No conflict |
| `Current` | `Current`, differing | **`AskPlayer`** | Two divergent careers are **not mergeable** — a save is one causal history, and any automatic pick silently discards the other |
| `Migratable` | `Current` | **compare pre-migration, then as above** | **Migration is not a tiebreaker** (FR-PK-006). Treating a migrated copy as "newer" would let *opening* a career on machine B rewrite machine A's cloud copy |

**No row merges. No row deletes. No row auto-selects a refused copy.** Those three properties are
asserted over the **whole** matrix (T-PK-U-009/010/011) rather than row by row, because a merge path added
later would satisfy every individual row while breaking the invariant.

**And one rule that sits outside the matrix because it applies after it:** a migrated save is uploaded
**only on the player's next explicit save**, never on load (FR-PK-008). **A read must never become a
write** — otherwise opening a career on the new build silently rewrites the cloud copy into a format the
older build then refuses.

**Expect refusals in the wild** (R-4). Two machines on different builds is the **normal** Cloud case, and
#50's `WORLD_GENERATION_VERSION` makes generation drift refusable too. A clean, non-destructive refusal is
a **success path** — the outcome this matrix is designed to produce, not an error to engineer away.

## Appendix D — The store / compliance checklist

**Two gate classes, and only one can block** (KD-4 / FR-PK-029).

### D.1 Compliance — **blocks the release**

| Item | Note |
|---|---|
| Age-rating declaration | Satisfied against the platform's **live** requirements at release time (§8.5) |
| Third-party licence + attribution manifest | Every bundled dependency accounted for |
| EULA and privacy text | Present **and localized through #49** — not baked strings |
| Cloud configuration matches KD-5's file set | The synced set is exactly the save file; **no partial or extra paths** (FR-PK-033) |
| Crash / exception reporting path | Present and verified on the packaged artifact |
| **The Appendix B evidence set** | The gate itself is a compliance item |

### D.2 Marketing — **does not block**

Capsule art · trailer · screenshots · store copy · tags.

**The split exists because a checklist without one becomes either theatre or a hostage.** With everything
soft, nothing blocks and the checklist is decoration; with everything hard, a missing screenshot blocks a
shipped-quality build — and the second failure teaches a team to bypass the checklist, which produces the
first.

**#39 specifies the checklist and its gate classes; it does not specify the assets** (FR-PK-032) — the
same boundary #48 drew for animation content and #51 for audio content.

**No platform clause is reproduced here, deliberately** (§8.5). Store requirements and rating regimes are
external, versioned by someone else, and change without notice; pinning specific clauses into an approved
spec would guarantee it is wrong at some future date **while looking authoritative**. The rows above name
**what must be true**, and the live platform documentation supplies **what that currently means**.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed with the argument that `REQUIRED_EVIDENCE` is `[FIXED]` because a tunable required set is a gate that can be switched off; A.2 Derived; A.3 Cross; A.4 GT; B the six-row evidence set with its input-side/artifact-side split and two deliberate exclusions; C the conflict matrix with its whole-matrix invariants; D the two-class checklist). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the four `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline — **the #45 PASS-1 M-2 defect, now seen for the tenth time in this wave**, which at this point is a process finding about the order sections get authored in rather than ten independent slips; added to A.4 with the note that #39 declares **no behavioural `[GT]` at all** and therefore carries no balance pass. **M:** added **A.2 `REQUIRED_EVIDENCE_COUNT` / `INPUT_SIDE_EVIDENCE_COUNT`** — both were implicit, and a hand-maintained count is the one arithmetic error in this spec that produces a **false green gate** (a fold checking five of six rows and reporting `Pass`), while a drifting input-side split would either un-run the smoke path or admit stale-commit evidence. **L:** A.1 recorded that `AchievementId.None` is a **refused** value rather than a default; B gained the explicit input/artifact side column, the statement that row 3 is what the appendix exists for, and the two deliberate exclusions; C gained the precondition line, the symmetry note (the local copy is **not** privileged), the whole-matrix invariants, and the record that refusals are expected in the wild as a **success path**; D gained the argument for the split — that an all-hard checklist teaches a team to bypass it, which produces the all-soft one — and the note that no platform clause is reproduced. |
#endregion
