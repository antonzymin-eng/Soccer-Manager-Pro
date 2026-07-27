# Save Migration & Versioning #50 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass recorded)
**Version:** 0.2
**Status:** IN REVIEW

---

## 9.1 Completeness

- [x] §1 scope, dependencies, the five verified facts, KD-1..KD-7, staging.
- [x] §2 FR-MG-001..038, data structures, failure modes F1..F8.
- [x] §3 FM-MG-01..05 with fifteen hand-verifiable worked examples.
- [x] §4 architecture — the leaf assembly, the delegate inversion, the load-path composition.
- [x] §5 test plan, incl. the classification matrix, the **two different** determinism properties, the
      generation lock, and the closed-loop scenario.
- [x] §6 performance — a load-path component with no loop; ceilings, not measurements.
- [x] §7 T-phase plan, extensions, risks R-1..R-6.
- [x] §8 cross-references XC-050-001..016 and back-props **ERR-030-019** / **ERR-027-003**.
- [x] Appendices A (constants), B (the `SaveOriginStamp` frame layout), C (the version-surface
      inventory), D (the classification matrix).

## 9.2 Constant discipline

- [x] Every constant carries exactly one tag; Appendix A is the single catalogue.
- [x] No `[EST]` constants.
- [x] **No `[CROSS-PENDING]` constants** — #50 takes no determinism reservation (KD-7), so nothing is
      blocked on an upstream allocation.
- [x] **No `[GT]` constant affects the simulation.** #50's only behavioural `[GT]` is the supported floor,
      which governs which *files* open — never how a match, season or world behaves. The four budget rows
      are ceilings. Verifiable in Appendix A.
- [x] `WORLD_GENERATION_VERSION` is `[FIXED]`, not `[GT]`: it is an identity, and making it tunable would
      let it be changed without the deliberate bump KD-2 exists to force.

## 9.3 Determinism discipline

- [x] No RNG stream, no domain tag, no `SubsystemOrdinal`, **no `_RESERVED_` placeholder** (FR-MG-036).
- [x] **The two transform classes are specified separately** (KD-7): format transforms are byte-pure;
      generation migrations are deterministic-by-seed and run against a `DeterministicRngService`. §5.2
      tests them as two different properties.
- [x] Migration runs **outside the tick loop**, before any subsystem is constructed (FR-MG-038).
- [x] Identity: an empty chain + a current save ⇒ zero transforms, byte-identical to pre-#50; every
      non-current save refused exactly as today (FR-MG-037).

## 9.4 Gates

| Gate | Status |
|---|---|
| **G1** — section-file PASS-1 adversarial review + fix pass | **CLOSED** — see §9.4.1 |
| **G2** — back-props filed at approval (`ERR-030-019`, `ERR-027-003`) | **OPEN** — lands atomically with the flip to `APPROVED` |
| **G3** — lead-developer R-01..R-05 sign-off | **OPEN** — a human authority, **not self-grantable** |
| **G4** — `[GT]` balance pass | **N/A in the usual sense** — #50 has no behavioural magnitude to balance. The **supported floor** is a product decision (R-5), not a balance one, and is chosen knowing it is measured in retained generator code |

### 9.4.1 PASS-1 adversarial review — **0H + 4M + 6L**, all resolved

**M-1 — the classification table was per-save, but a save carries many versions.** §1's five classes
described one verdict while the file has a frame version **and** one per sub-blob, and nothing said how
they combine. Two natural readings both fail: "any non-current ⇒ Migratable" would run a chain over a
`Corrupt` blob, and "the frame decides" would ignore a `TooNew` sub-blob entirely. Resolved as
**FR-MG-008**, a most-severe fold with `Corrupt` dominating `TooNew` — because a damaged file reported as
merely futuristic invites the player to wait for a patch that will never help. Worked example §3.7(e);
locked by T-MG-U-007.

**M-2 — nothing barred a step from reaching into a neighbouring blob.** KD-3's isolation was stated as an
intent and enforced nowhere, and a step written by a spec author with the whole file in hand is exactly
where "while I'm here, I'll fix the other blob too" happens. Resolved as **FR-MG-021 / F8**, expressed so
the violation is **not representable** — the runner hands each step only its own bytes — rather than
merely forbidden. Locked by T-MG-BOUND-003.

**M-3 — the completeness check existed only as a risk sentence.** The supplement's R-2 names the failure
(a spec bumps a version, forgets to register a step, and every old save becomes `Unsupported` **with no
diagnosis**) and proposes a build-time check, but no requirement carried it, so it would have been
implemented as a warning or not at all. Resolved as **FR-MG-022**, a **build** failure; locked by
T-MG-BOUND-004 with non-vacuity proven by removing a real registration.

**M-4 — the T-phase ordering put the generation stamp too late to be useful.** The obvious sequence lands
the stamp with the first generation migration, since it buys nothing until then. But **a stamp added at
T3 can only classify saves written after T3**, so every career from before it is permanently
unclassifiable — and refusing those is the worst possible first impression of a migration system.
Resolved by moving the stamp to **T1**, with the reasoning stated in §7.1: *the stamp is worth writing
long before it is worth reading.*

**L-1** — `SaveClass` had no stated ordinal contract, which invited someone to pin it "for consistency"
with `BlobKind`; §2.2 now records that it is deliberately **not** pinned (it is never serialized) while
`BlobKind` **is** (it is the registry key). **L-2** — `BuildId`'s diagnostic-only status was in the
supplement's prose but not in the data structure, where the temptation to migrate off it actually lives;
the annotation and the false-incompatibility consequence are now at the field. **L-3** — §3.2 did not
verify a step's output version, so a step claiming `4 → 6` would silently skip `5`; the `+1`
post-condition is now in the runner (§3.7(i), T-MG-DET-004). **L-4** — §6 gave no cadence for
classification, which is the only #50 cost multiplied by **file count** rather than by a user action;
§6.1/§6.2 now name the load-screen path and §6.3 flags `MG_BUDGET_CLASSIFY_MS` as the first to measure.
**L-5** — the four `[GT]` budget ceilings declared in §6.3 were **absent from the Appendix A catalogue**
(the #45 PASS-1 M-2 defect, now seen for the **eighth** time in this wave); added. **L-6** — §5 lacked a
statement of the test that must **not** be written; T-MG-I-003 now records that counterfactual identity is
declined by FR-MG-033 and that asserting it would force a transform to fake data.

**AR-2 sweep — 0H + 0M + 2L → CONVERGENCE** (an L-only round closes the cycle, per project convention).
The sweep specifically re-walked every remaining statement about transform purity — the supplement's own
AR-2 fix propagated into **six** stale statements across three of its rounds, so purity claims were the
highest-yield thing to re-check — and found none surviving in the section files. **L-7:** §4.1's reference
list did not justify the `DeterministicSim` dependency, which looks gratuitous for a byte-shuffling
component until one notices FR-MG-035 requires a real `DeterministicRngService`; justified in place.
**L-8:** §8.2 asserted the two back-prop ids were free; the assertion is now recorded as **verified
against the log and against every spec folder**, with the neighbourhood spelled out — three specs in this
same wave proposed ids that had already been filed, so an unverified id in a back-prop table is a known
failure mode here rather than a hypothetical one.

## 9.5 Verification anchors

Every claim below is checkable against a named file; none is a summary of another summary.

| Claim | Anchor |
|---|---|
| Squads are regenerated, not saved | `WorldStore.WorldSeed`'s doc comment (XC-050-005) |
| The frame is version-first with opaque sub-blobs | `SeasonSaveCodec` (XC-050-001) |
| There is no migration machinery anywhere | tree-wide `Migrat` / `Upgrade` search over `src/` (XC-050-004) |
| A representation-changing bump is already queued | #45 `ERR-030-009` (XC-050-008) |
| The materialisation target shape already exists | #47's authored sub-blob (XC-050-009) |
| `ERR-030-019` and `ERR-027-003` are free | `spec-error-log.md` + a sweep of every spec folder (§8.2) |

## 9.6 Decision

**Status: `IN REVIEW`.** G1 is closed. **G2 and G3 remain open**, and G3 cannot be closed by the author:
lead-developer R-01..R-05 sign-off is a human authority, not self-grantable, per the promotion pipeline.
The spec does **not** claim `APPROVED`, and the flip lands the §8.2 back-props atomically with it.

**One reviewer question is worth putting first**, because it is the decision the rest of the spec is
downstream of: **KD-2's cost.** Retaining a frozen generator per supported version means shipping code
that normal play never exercises, and it makes the supported floor a code-size decision rather than a
test-surface one. The alternatives are both recorded and both rejected — eagerly materialising every save
inflates every file forever to insure against a bump that may never happen; forbidding post-ship
generation changes is unenforceable. **If the owner rejects KD-2, #50 becomes a format-only migrator**,
which is a coherent spec that provably cannot detect this project's most likely breaking change — and
that trade should be made explicitly rather than by omission, which is exactly how it was nearly made in
the original plan.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial checklist. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 recorded: **0H + 4M + 6L**, all fixed in the v0.2 files; AR-2 sweep **0H + 0M + 2L → CONVERGENCE**. The four M were the per-blob classification fold (a save carries many versions and nothing said how they combine), step isolation (stated as intent, enforced nowhere), the build-time completeness check (a risk sentence with no requirement behind it), and the T-phase ordering that would have added the generation stamp too late to classify any existing career. §9.6 puts KD-2's cost first as the reviewer question the rest of the spec depends on. |
#endregion
