# Manager Career, Reputation & Job Market #54 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / leaf DAG / **§1.4's verification findings** / KD-1..KD-8 /
      determinism posture.
- [x] §2 FR-MC-001..034, data structures, failure modes F1..F9, and the *"being unemployed is not a
      failure mode"* note.
- [x] §3 FM-MC-01..05 with the two-band rule and its guard ordering, the freezing termination, the
      appointment and the command-layer join it must **not** perform, the read-only reputation
      projection, attractiveness and the deferred S3 draw, the §3.6 division convention, and fifteen
      hand-verifiable worked examples.
- [x] §4 leaf assembly with the #45-reference trap and the **foreseen** third CS0104 instance, file
      layout, the #30 seam, the command-layer join, the explicit-optional unemployed representation, save
      composition, neighbour contracts.
- [x] §5 test plan led by **the two locks the spec exists for**, then rule / lifecycle / projection units,
      the appointment join, save/restore, determinism + identity, structural, fail-loud, and the T-phase
      closed-loop scenario.
- [x] §6 loop classification (world tick + boundary, no hot path), cost profile **costing the
      recomputation explicitly**, `[GT]` ceilings, memory.
- [x] §7 T0–T3 plan, deep-tier extensions, the not-planned list, risks R-1..R-6.
- [x] §8 XC-054-001..016 + the two back-props + the not-a-back-prop list.
- [x] Appendices A (constants), B (save layout), C (the `EndReason` roster + reputation-term table).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them).
- [x] `[DERIVED]` rows document their formula and are never set independently.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #54 re-declares none of #45's,
      #30's or #26's types (T-MC-BOUND-003/004).
- [x] `[CROSS-PENDING]`: `_RESERVED_0x2E_` / `SubsystemOrdinals.ManagerCareer = 96` — **the placeholder is
      added at approval; the named tag waits for the S3 draw site** (FR-MC-025).
- [x] **No reputation constant is a stored value** — the `[GT]` reputation terms are *inputs to a
      projection*, and Appendix A records that the projection has no field (FR-MC-013).
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts only
      shape, direction and symmetry — never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] #45 `FR-BD-012`: *"#45 MUST NOT expose a sacking API … It supplies confidence; **#30 decides**."*
      Repeated in §1.5 KD-3, `outline.md`, and `appendices.md` (*"#30 owns what a band means for the
      sacking decision"*). **Four sites, one wrong counterparty.**
      *(`docs/specs/board-ownership-dynamics/`)*
- [x] A search for *sack* / *dismiss* / *unemploy* across `docs/specs/season-competition-loop/` returns
      **nothing**. #30 owns `BoardState`, the objective, and the derived `JobSecurityBand` — **and no rule
      that ends a tenure.** The hole this spec exists to fill.
- [x] `SeasonState`'s constructor throws with *"ManagedClubId {id} is not in the season's club set."* —
      **an unemployed manager is structurally unrepresentable**, verified in code rather than inferred.
      *(`src/season-save/SeasonState.cs`)*
- [x] #30 Appendix B row 3a lists `managedClubId i32` as **mandatory**, and its omission from the §3.6
      pseudocode was filed as **`ERR-030-011`** precisely because a season cannot be reconstructed without
      it — so the field is load-bearing, not incidental.
- [x] #45 **`FR-BD-005a`** requires `{BoardConfidence, OwnershipProfile}` to be inserted as a
      **factory-built pair guarded at insertion**, because `default(BoardConfidence)` is field-**in-range**
      (`0` is a legal per-mille and a legal world day) yet means the **`Critical` band** with a
      `LastAdvancedWorldDay = 0` that **no-ops the day-0 guard**. The trap §4.4's appointment join avoids.
- [x] **`ERR-030-009`** turned #30's independent `JobSecurity` scalar into a **derived band** because two
      truths for one quantity *"diverge at the first restore with nothing to detect it"* — **the lesson
      KD-2 applies pre-emptively**, and the **queued bump on the same block** §8.2 recommends combining
      with. *(`docs/tracking/spec-error-log.md`)*
- [x] #30's **`RoundResolutionMode`** already lets a season advance without the engine — so KD-4's
      unemployed season needs **no new capability**, only a representable state.
- [x] #22's `WorldLoop` **phase-5 `BackgroundTierSim`** is a documented null seam, unbuilt because it
      *"summarises club-AI / transfer / **sacking** outcomes that do not exist yet"* (FR-LW-031) — the
      reason KD-3 generates vacancies without rival managers. *(`docs/specs/living-world/`)*
- [x] **#26 ships `ManagerProfile` and `ManagerMode.Human`** in `src/match-engine` for in-match tactical
      adaptation — a different "manager". The **foreseen** third CS0104 instance after `TacticTranslation`
      and `PlayerAttributes`. *(`src/match-engine/`, `docs/specs/tactical-presets/`)*
- [x] The roadmap §6 determinism block is **full** at `0x20`–`0x2D` / 82–95, with `0x2E`–`0x2F` / 96–97
      reserved *"so that if a candidate currently classified read-only/presentation/infra later discovers
      it needs a draw, it extends from `0x2E`/96 onward."* #54 is the plausible first claimant, and KD-6
      declines to claim it yet. *(`docs/tracking/management-layer-spec-roadmap.md` §6)*
- [x] `RegisterStream` appends into a bounded, never-shrinking table; `MaxRngStreams = 64`, no unregister
      — the ceiling FR-MC-026's single-stream rule respects. *(#42 §7.4 R-1)*
- [x] **`ERR-045-001` is filed; `-002` is next free.** **Proposed `ERR-030-*` ids across the
      pre-promotion supplements reach `-020` (#53); `-021` is #54's** — and the *filed* rows reach `-014`,
      so there is no collision with the log. *(`docs/tracking/spec-error-log.md`, the sibling supplements)*
- [x] `FR-MC-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in `docs/specs/`.

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-045-002** and **ERR-030-021** atomically with the status flip. | drafter | ✅ **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.47) |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ✅ **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ✅ **CLOSED** — row + Registry-Changes entry landed July 27, 2026 |
| **G5** — `management-layer-spec-roadmap.md` row + §3 scope sketch + §7 wave placement for **#54**, and `spec-plans/spec-54-manager-career-reputation.md` — the v0.2 gap-fill / v0.4 Amendment-01 precedent for adding a **new** candidate number. | drafter | ✅ **CLOSED** — roadmap v0.7 + registry row landed July 27, 2026 |
| **G6** — **sequencing decision with #45's `ERR-030-009`**: whether ERR-030-021's `SEASON_STATE_FORMAT_VERSION` bump lands **combined** with the queued `JobSecurity` bump on the same block. | lead developer | ✅ **DECIDED — combine them**, recorded July 27, 2026 with the flip. Both bumps hit the **same** season block and both are `no-migration` refusals, so landing them separately would make existing saves unloadable **twice** for one block. The decision is recorded in FR-SN-013b and in `spec-error-log.md` v1.47; the bump itself remains ◑ at #54 T2 |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the outer
`SEASON_SAVE_FORMAT_VERSION` bump (T2); the `_RESERVED_0x2E_` **promotion** to a named tag (only at the S3
draw site — the placeholder itself lands at approval); the job market, rival managers, manager
personality and international appointments (all T3/S5); and the T3 `[GT]` balance pass.

**G5 and G6 are both specific to #54.** G5 exists because #54 is a **new** candidate number proposed by
its own supplement rather than one the roadmap already carried — the governance rows other specs inherited
must land with it. G6 exists because #54 is the **second** spec to queue a bump on the same #30 block, and
nobody else is positioned to make that call.

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 5M + 6L, all resolved in the v0.2 fix pass.** The M findings cluster on the two things the
supplement specified in prose but never made checkable: the **coherence of the career record** (three of
five), and the **ordinal contract** its reputation projection silently depends on.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **`EndReason`'s ordinal is load-bearing twice and nothing said so.** It is **serialized** in the career block *and* **indexes the reputation weight table** — so a reorder re-reads every historical tenure **and** changes every historical reputation, with **no version gate to catch either**. The supplement mentioned *"ordinal stability on `endReason`"* only in its test list, where a requirement it implies does not exist. | New **FR-MC-015** + **F4**; §3.7(n) is the worked example; **T-MC-U-022** asserts it. The same class as #46's `ItemKind` and #35's `MediaIntent` contracts. |
| M-2 | M | **Unemployment had two representations and only one was checkable.** Nothing pinned that it is `CurrentTenure == -1` rather than *"the last tenure happens to be closed"* — so a decoded career whose `CurrentTenure` points at a closed tenure was a **valid-looking, incoherent state**. | New **F6** + a decode coherence gate (FR-MC-031); §3.7(l) and **T-MC-U-013** lock it. |
| M-3 | M | **Neither `Appoint` nor `Terminate` had a precondition guard.** A second `Appoint` while a tenure is open **decodes cleanly** and merely makes `CurrentTenure` meaningless, so nothing downstream would catch it; a `Terminate` while unemployed would corrupt an already-closed tenure. | New **FR-MC-018/019** + **F1/F2**; §3.7(j)/(k) and **T-MC-U-011/012**. |
| M-4 | M | **The termination rule had no grace period**, so appointing a manager to a club whose confidence is already low — **the realistic case**, since clubs sack managers when things are going badly — would terminate him on the **first evaluation after appointment**. FR-MC-017's honeymoon value addresses this from #45's side only. | §3.1 gains `MC_GRACE_PERIOD_DAYS` **as the first guard after the range checks**, with the ordering called out; **T-MC-U-003** locks the ordering and **T-MC-I-005** the composed pair, so neither guard is load-bearing alone. |
| M-5 | M | **The reputation projection's independence was under-specified.** It said "computed from the record", which does not exclude *also* reading current confidence — and making reputation respond to how things are going is the second-truth problem **arrived at from the other direction**, since reputation would then move without the record changing. | New **FR-MC-014**; **T-MC-U-001** varies confidence, club and world day across their ranges and asserts bit-identity. |
| L-1 | L | **KD-8 lived inside KD-6**, where the identity claim's *limit* — the minimal tier makes a sacking **survivable**, not **recoverable** — was reachable only via the determinism decision. Overselling a minimal tier is how a tier gets called finished, and the supplement's own AR-3 had caught the same sentence once already. | Promoted to a key decision of its own, stated in §1.5 and restated in §7.1. |
| L-2 | L | **Open tenures' contribution to reputation was unstated**, leaving a plausible implementation in which reputation **jumps on termination** — which reads as a bug and is one. | §3.4 states it; §3.7(m) and **T-MC-U-017** assert it. |
| L-3 | L | **No coherence gate on tenure bounds** (`EndWorldDay < StartWorldDay`, `Finishes` longer than `SeasonsServed`), either of which silently corrupts every reputation projection over that tenure. | New **F5**; **T-MC-U-014** asserts at write **and** decode. |
| L-4 | L | The **clamp's position** in the projection was unspecified. Clamping per term rather than once at the end would let a bad early spell **saturate** the projection at zero, making later recovery invisible. | §3.4 pins it at the end; **T-MC-U-020**. |
| L-5 | L | **§3.6's division convention was missing**, though the projection and attractiveness both divide — and `Math.Floor` / `Math.Round` would each break sign symmetry across the negative `EndReasonTerm` rows. | §3.6 added, with **T-MC-U-019** locking the symmetry directly. |
| L-6 | L | `TenureEvaluationInput`, `VacancyInput` and `EndReason` were described in prose only; §5 had no structural assertion that **no rival-manager surface exists** — the KD-3 boundary a T3 implementer would breach first. | Written out in §2.2; **T-MC-BOUND-006** added. |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §6.2 did not **cost** the reputation recomputation, though performance is
the argument a cache will be introduced under; it now states the figure and marks the recomputation as
*the design, not a tolerated inefficiency*, with the `[GT]` ceiling deliberately set an order of magnitude
high so it trips on a real regression rather than on ordinary recomputation. **L-2:** §4.2 did not say why
`ReputationProjection` is its own stateless file — co-locating it with the record is the shortest path to
a `_cachedReputation` field. **L-3:** §7.1 did not name that **two** #30-side format bumps land at T2
(the block's own composition, and ERR-030-021's representation change), which is the fact G6's sequencing
decision turns on.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; the #45 / #30 reconciliation is explicit, and **#45's approved posture is preserved rather than amended**. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values. | ⏳ pending |
| R-03 | Determinism posture is complete: the draw-free minimal tier, the reserved-not-promoted placeholder, and the **derived** reputation's inability to desynchronise are each justified rather than asserted. | ⏳ pending |
| R-04 | Persistence is version-gated, opaque, fail-loud, APPEND-only and coherence-checked; the block's **outliving the season** and the **absence** of a reputation field are both argued. | ⏳ pending |
| R-05 | Cross-spec back-props are enumerated with owners and timing, every proposed ERR id is verified free, and **the one non-additive consequence** (ERR-030-021's representation change) is called out rather than buried. | ⏳ pending |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-045-002**, **ERR-030-021** (`spec-error-log.md` v1.47). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #54 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** — G1 closed (PASS-1 0H+5M+6L → AR-2 0H+0M+3L convergence, §9.4.1). G2–G6 remain open:
back-props land atomically with the status flip, sign-off is a human authority, the registry and roadmap
rows are added at promotion, and G6 is a scheduling call only the lead developer can make.

**Why this spec exists, restated at the decision point.** The project has a fully-specified path to *"you
are about to be sacked"* and **no specified behaviour for being sacked**. #45 says four times — including
in a MUST — that #30 decides it; #30 contains no such rule. And underneath that, a manager without a club
**cannot be represented at all**: `SeasonState`'s constructor throws. #45's confidence model is currently
a countdown to nothing.

**Two things this spec should be judged on.** First, that it leaves **#45 unchanged**: the spec that was
*right* is not edited to accommodate the spec that was *missing* — ERR-045-002 changes a name and asks a
question. Second, that its most important test is the one the **current codec cannot even construct** —
a save round-tripping in the unemployed state — which makes it simultaneously #54's acceptance test and
the proof that ERR-030-021 landed.

**The one non-additive consequence, restated.** ERR-030-021 changes `ManagedClubId`'s representation,
carrying a `SEASON_STATE_FORMAT_VERSION` bump that makes pre-bump saves **unloadable with no migration
path**. #45's `ERR-030-009` already queues a bump on the same block, so **G6's answer determines whether
players face one refusal boundary or two.** That was surfaced before sign-off, not after.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, six gates incl. the #54-specific G5 governance rows and G6 sequencing decision, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+5M+6L, all resolved — clustered on career-record coherence and the `EndReason` ordinal contract) and the AR-2 convergence sweep (0H+0M+3L). §9.1 completeness updated for KD-8 and FR-MC-014/015/018/019; §9.2 gained the no-stored-reputation-constant line; §9.3 gained the `ERR-045-001`-is-filed / `ERR-030-021`-is-free checks, the `FR-MC` prefix check, and the #26 CS0104 row. G2–G6 remain open. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-045-002**, **ERR-030-021** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.47). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
