# News, Inbox & Man-Management #46 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / leaf DAG / **§1.4's three verification findings** / KD-1..KD-9
      (with KD-9 split into its two distinct scopes) / determinism posture.
- [x] §2 FR-NW-001..036, data structures, failure modes F1..F9, and the two explicit *"not a failure
      mode"* notes.
- [x] §3 FM-NW-01..05 with append + id allocation + payload snapshot, the write-nothing query, the
      monotone read watermark, man-management with its refuse/throw split, the routed drain and the
      **root's post-sum clamp**, plus seventeen hand-verifiable worked examples.
- [x] §4 leaf assembly + DAG with the FR-LW-031 consequence, file layout with its three deliberate
      absences, the root projectors **and their per-producer sites**, the `InboxTextBoundary` adapter and
      the two-enum split, the #30 drain generalization, save composition, neighbour contracts.
- [x] §5 test plan across identity (both scopes) / units / read state / man-management / delivery + save
      / localization / determinism / structural / fail-loud + the T-phase closed-loop scenario.
- [x] §6 loop classification (no tick slot, no hot path), the three cadences, cost profile, `[GT]`
      ceilings.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6.
- [x] §8 XC-046-001..020 + the back-prop table + the not-a-back-prop list.
- [x] Appendices A (constants), B (save layout), C (the `SourceTag` / `ItemKind` / `InboxIntent` rosters
      + the pinned payload schemas).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them) — #46 has **no `[CROSS-PENDING]` constants at all**,
      because it has no reserved determinism value to promote (KD-8), so that region does not appear.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #46 re-declares none of #49's or
      #33's types (T-NW-BOUND-003).
- [x] `[DERIVED]` rows document their formula — the intent counts derive from the enums, never from
      literals (the `POSITION_COUNT` parallel-surface precedent).
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts only
      shape, bounds and direction — never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] #30's `Fixture` is `{ RoundIndex, HomeClubId, AwayClubId, Played }` and the result is recorded **on
      the table, not the fixture** — *"the fixture list is the immutable schedule."* **The fact that makes
      KD-1's persistence forced rather than chosen.** *(`season-competition-loop/section-2.md` §2.2)*
- [x] #30 §3.4 runs `Table.ApplyResult(result)` → `EmitMatchOutcome(result)` → `f.Played := true`, with
      FR-SN-017 pinning #30 as *"the producer only"* — the projector site, and the only instant the
      scoreline exists. *(`season-competition-loop/section-3.md`)*
- [x] #44 records the same wall from the other side: *"#30 retains no per-fixture ledgers … recompute-on-
      load has no input."* The precedent KD-1 follows rather than inventing.
      *(`discipline-suspensions/section-1.md` KD-1)*
- [x] #33 **FR-HS-002**: *"No other assembly writes them."*
- [x] #33 **FR-HS-024**: *"#46 is the only consumer that **writes** #33 morale (man-management)"*, with a
      read-accessor list of *"#31/#35/#45"* that **excludes #46** — the asymmetry FR-NW-006 rests on.
- [x] #33 **FR-HS-025**: *"Morale is a projection OUT of #33 — no two-way coupling."* **The requirement
      that makes "#46 never reads morale" a MUST rather than a simplification.**
- [x] #33 §3.3 + `XC-033-007` refer to *"#46's future man-management seam (deferred)"* without saying what
      it is — the ambiguity ERR-033-004 closes. Read with FR-HS-002 it permits exactly one coherent shape.
      *(`personalities-morale-dynamics/section-3.md`, `section-8.md`)*
- [x] #33 §2.2 `HumanSystemsDayInput` is a **transient input struct**, not serialized state — the fact
      that makes ERR-033-003 carry **no** `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` bump.
- [x] #37 **FR-AN-020** (*"MUST hold no persistent state"*) and **FR-AN-021** (*"MUST consume live during
      the match … MUST NOT assume the serialized ledger bytes can be re-parsed"*) — so #37 **cannot** be
      called after the fact and is not an inbox source. *(`match-analytics-statistics/section-2.md`)*
- [x] #35's KD-6 exposes a **read-only conference query** for exactly this consumption, and asserts the
      `#46 → #35` direction from its own side — so neither spec references the other.
- [x] #49 **§7.3 names `InboxTextBoundary` in advance**, and **§2.2** pins that *"#35/#46 carry disjoint
      slots"* — so #46 fits an existing extension point and needs no #49 structural change.
      *(`localization-accessibility/section-7.md`, `section-2.md`)*
- [x] #49 **FR-LC-012** makes a sim assembly referencing `TacticalDirector.Localization` a **build
      error** — the fact §4.2's adapter placement rests on.
- [x] #49 **FR-LC-020** binds `SelectionDraw` to #22's `world.text` draw — the defect #35's ERR-049-001
      fixes and which #46 **inherits** rather than re-filing.
- [x] **#16 §3.4 has no `_RESERVED_` row for #46**, and records that *"#37–#39 are read-only /
      presentation / infra and take no tag"* — the class #46 belongs to. So there is nothing to file
      **and nothing to promote later**. *(`deterministic-sim/section-3.md` §3.4)*
- [x] **`ERR-030-014` is already FILED** — it is ERR-030-014 itself, the match-engine playability defect
      found while running #30's T2 calibration pilot on July 26, the same day this spec's supplement was
      written. The supplement proposed it for the drain generalization; reassigned to **`-024`**
      (§9.4.1 M-1). **`ERR-030-015` is genuinely free.** *(`docs/tracking/spec-error-log.md`)*
- [x] `ERR-033-003` / `-004` are free; `ERR-033-001` is deliberately **retired unused** in favour of
      `-003`, jointly with #35 (§8.2).
- [x] `FR-NW-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in `docs/specs/`.

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-033-003** (jointly with #35), **ERR-033-004**, **ERR-030-024**, **ERR-030-015** atomically with the status flip. | drafter | ✅ **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.46) |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ✅ **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ✅ **CLOSED** — row + Registry-Changes entry landed July 27, 2026 |
| **G5** — **coordination with #35**: ERR-033-003 must supersede #35's ERR-033-001 in whichever spec flips first, so the two never disagree about the field's name or arity. | drafter | ✅ **CLOSED** — roadmap v0.7 + registry row landed July 27, 2026 |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the outer
`SEASON_SAVE_FORMAT_VERSION` bump (T2); every projector after the #30 match one (each lands with its
producer); man-management itself (T3); #37 view-model capture (a root extension, if ever); and the T3
`[GT]` balance pass (§A.4).

**#46 does not carry #35's G0.** It cites **no step number of its own** (KD-7), so #30's malformed
tick order — the prerequisite that gates #35 — does not gate #46. Note the claim is precisely that and
no more: #46's world-tick projectors run *inside* that order at their producers' steps, so its **emission
ordering** is inherited from whatever the repair settles on (§7.4 R-4).

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 5M + 6L, all resolved in the v0.2 fix pass.** The M findings cluster where a
routing-and-log spec is most likely to be wrong: the boundary between what is *stored* and what is
*derived*, and the pair of rules that look inconsistent and are not.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **`ERR-030-014` is already filed.** The supplement proposed it for the step-3 drain generalization; it is ERR-030-014 itself — the match-engine playability defect found while running #30's T2 calibration pilot **on the same day the supplement was written**. Nothing cross-checks a proposed id against the log, so this would have landed silently and produced two unrelated changes under one id. | Verified against `spec-error-log.md`, reassigned to **`ERR-030-024`**; `-015` confirmed genuinely free. Recorded in §9.3 so the verification is re-runnable. `outline.md` / `section-8.md` v0.2. |
| M-2 | M | **The `Payload` schema was an unversioned convention inside a versioned blob** — the supplement's own R-2 — with **nothing making it checkable**. A length mismatch would decode silently into a wrongly-shaped item. | New **FR-NW-011 / F2**: the `(SourceTag, ItemKind) → arity` mapping is a first-class contract in its own file (`PayloadSchema.cs`), checked at **both** `Append` and decode. Locked by T-NW-U-003 / T-NW-FAIL-002. The residual risk — a change to what an existing *slot means* — is retained explicitly as §7.4 R-2, since arity checking does not cover it. |
| M-3 | M | ***"A query writes nothing"* was an implementation note, not a requirement** — while the **entire KD-7 no-tick-slot argument rests on it**. If a query pruned dead keys or aged items, a save taken after merely opening the inbox would differ from one taken before, and #46 would owe #30 a step. | Promoted to **FR-NW-020**, and asserted directly as **T-NW-U-009** (byte-identity across any number of queries), flagged in §5 as *the KD-7 lock*. |
| M-4 | M | **The item-vs-delta departure asymmetry was prose only.** Items about a departed player are **retained** (a historical record) while pending deltas are **dropped** (a pending effect) — a pair that reads as an inconsistency and would be "fixed" in one direction by a later consistency pass. | New **FR-NW-016** stated alongside FR-NW-028 with an explicit cross-reference, and **T-NW-I-004** asserts both halves in **one** test over one scenario, so unifying them fails there rather than in play. |
| M-5 | M | **`Append` did not state that it snapshots the caller's payload array.** Retaining a live handle would let post-`Append` mutation rewrite a stored item — the defect class this project has hit three times (`SpawnArc`'s pin array, `TacticPreset.Players`, `MatchReplay`'s frame list). | `Copy(payload)` pinned in §3.1 with the precedent named; locked by **T-NW-U-005**. |
| L-1 | L | KD-9's identity claim did not distinguish **no projector wired** (a T0/T1 property) from **projectors wired, man-management off** (the shipped minimal tier). Only the second describes what ships. | Split into two scopes in §1.5 and into two tests (T-NW-ID-001/002). |
| L-2 | L | The two ordinal-stability contracts (`ItemKind`, `InboxIntent`) were described together, inviting a single test. They fail in **two different ways** — wrong schema vs wrong template — so one test proves nothing about the other. | Separate requirements (FR-NW-010 / FR-NW-029) and separate locks (T-NW-LOC-002 / T-NW-LOC-003), each naming its own failure. |
| L-3 | L | `InboxCursors`' per-source array had a growth rule in prose but no lock, so a reordering append would silently re-base an existing source's ids and break the total order's tie-freedom. | **T-NW-I-010** asserts cursor stability across a `SourceTag` append. |
| L-4 | L | `SourceTag`, `InboxCursors` and `InboxSlots` were described in prose only. | Written out in §2.2. |
| L-5 | L | The **query budget** was expressed in µs alongside the loop-step budgets, implying a constraint a screen-open operation should not carry. | §6.3 states it in **ms**, with the reason, and names the **drain** as the one to measure first. |
| L-6 | L | §4.7's standing review item was scoped to #46 — but #46 references nothing, so the reference graph already proves most of it. The one place a foreign write can hide is a **root-side projector** holding both sides. | Re-scoped to the projectors; **T-NW-BOUND-004** asserts producer-state immutability across every projection. |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §7.1 did not name the predicted T3 failure — wiring `TryTakePendingDelta`
without the root's post-sum clamp, which no #46-local test would catch; now stated, with T-NW-I-002 named
as the lock. **L-2:** §6.4 asserted a bounded footprint without naming **which four rules** keep it
bounded (retention window, item cap, log-bounded exception set, never-write-zero), any one of whose
removal makes the APPEND-only blob grow across a career. **L-3:** §8.4 did not state the **#16 asymmetry
with #35** — #46 has no reserved value to promote at all, so a future stochastic news generator needs a
fresh allocation rather than a promotion, which is the opposite of #35's position and reads as an
omission otherwise.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; no model #46 does not own is duplicated, and the #35 / #33 / #37 boundaries are explicit rather than implied. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values. | ⏳ pending |
| R-03 | Determinism posture is complete: draw-free at **every** tier, the total order's tie-freedom, the lazy-retention soundness argument, and the write-nothing query are each justified rather than asserted. | ⏳ pending |
| R-04 | Persistence is version-gated, opaque, fail-loud, APPEND-only, bounded, and bumps no format version it does not own; the payload-schema contract and its residual risk are stated. | ⏳ pending |
| R-05 | Cross-spec back-props are enumerated with owners and timing, **every proposed ERR id is verified free against the log**, and the joint #35 filing is recorded on both sides. | ⏳ pending |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-033-003** (jointly with #35), **ERR-033-004**, **ERR-030-024**, **ERR-030-015** (`spec-error-log.md` v1.46). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #46 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** — G1 closed (PASS-1 0H+5M+6L → AR-2 0H+0M+3L convergence, §9.4.1). G2–G5 remain open:
back-props land atomically with the status flip, sign-off is a human authority, the registry row is added
at promotion, and G5 is a sequencing obligation shared with #35.

**What verification did to this spec, restated at the decision point.** #46 arrived with a plan whose KD-4
assumed the inbox was *"largely a derived view over already-persisted events."* Checking #30's own text
showed the opposite for the most common item type there is: the **scoreline is destroyed** the moment the
league table absorbs it, so *"you drew 1–1 away to Everton on matchday 12"* is **not recomputable from a
save**. #44 had already hit the same wall from the other side. KD-1 inverts the plan's premise, and
§5's T-NW-I-005 asserts the consequence directly — a stored item still correct after the table has moved on.

**The second finding is the one that spans specs.** #35 established the morale-routing mechanism a week
earlier with a producer-**specific** field. #46 is producer number two, so the name does not survive — and
a struct that grows a field per producer means a #33 back-prop for every future system that can nudge
morale. ERR-033-003 replaces it with one producer-agnostic field summed and clamped at the root. That is
the same shape as the defect #35 itself found in #49's FR-LC-020: **a contract written correctly for one
producer, breaking the moment a second arrives.** Two instances in one wave is a pattern worth naming, and
it is why both specs record it on both sides.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, the five gates incl. the #35-coordination G5 and the explicit note that #46 does **not** carry #35's G0, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+5M+6L, all resolved) and the AR-2 convergence sweep (0H+0M+3L). §9.1 completeness updated for FR-NW-011/016/020; §9.2 records that #46 has **no** `[CROSS-PENDING]` region at all; §9.3 gained the verified **`ERR-030-014`-is-taken** row (the PASS-1 M-1 correction), the FR-HS-025 row that makes FR-NW-006 a MUST, the #37 rows, and the `FR-NW` prefix check. G2–G5 remain open. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-033-003** (jointly with #35), **ERR-033-004**, **ERR-030-024**, **ERR-030-015** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.46). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
