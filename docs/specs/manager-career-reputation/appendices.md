# Manager Career, Reputation & Job Market #54 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #54 has no `[EST]` constants, so that region does not appear. `[GT]` values are
**illustrative pending the T3 balance pass** — §5 asserts only shape, direction and symmetry, never
magnitude, so the balance pass cannot invalidate a passing suite.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `CAREER_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The career block's own version gate (KD-7). Independent of `SEASON_SAVE_FORMAT_VERSION` **and** of #30's `SEASON_STATE_FORMAT_VERSION`, which ERR-030-021 bumps — three versions, none implying the others (§4.6). |
| `MC_UNEMPLOYED` | `-1` | `[FIXED]` | The `CurrentTenure` value meaning *"the manager holds no club"* (FR-MC-010). **This is the definition of unemployment** — not *"the last tenure happens to be closed"*, which is the incoherent second representation F6 rejects. |
| `MC_OBJECTIVE_NEUTRAL` | `500` | `[FIXED]` | The per-mille objective outcome meaning *"on track"*. Fixed because §3.1's band comparison is stated against it; moving it would silently re-shape the termination rule. |
| `MC_OBJECTIVE_MIN` / `MC_OBJECTIVE_MAX` | `0` / `1000` | `[FIXED]` | The contract range for the routed objective outcome (F3). |
| `MC_REP_TERM_ABS_MAX` | `200` | `[FIXED]` | The absolute bound on every per-term reputation constant, enforced at the seam. With `MC_MAX_TENURES` it is what keeps the §3.4 accumulator three orders of magnitude inside `int` before the final clamp. **Fixed, not tunable** — raising it is an arithmetic-safety change, not a balance change. |
| `MC_MAX_TENURES` | `64` | `[FIXED]` | The career-history bound (§6.4). Bounds the block, the projection's cost, and the overflow argument together. |
| `MC_MAX_SEASONS_PER_TENURE` | `50` | `[FIXED]` | Bounds each tenure's `Finishes` / `Trophies` arrays; enforced at the write seam and on decode (F5). |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `MC_END_REASON_COUNT` | `Enum.GetValues(typeof(EndReason)).Length` | `[DERIVED]` | The length of Appendix C's weight table. Derived from the enum, never a literal — two surfaces carrying private copies of a member count is the `POSITION_COUNT` parallel-surface defect, and here a mismatch would index the weight table out of range or silently drop a row. |
| `MC_ATTR_W_TOTAL` | `MC_ATTR_W_POSITION + MC_ATTR_W_FINANCE + MC_ATTR_W_FACILITY + MC_ATTR_W_SQUAD` | `[DERIVED]` | §3.5's normaliser. **Never set beside the weights**: a hand-set total scales every attractiveness reading by a constant factor, which reads as a plausible balance change and is a bug. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `BoardConfidence`, `OwnershipProfile`, `JobSecurityBand` | #45 §2.2 | **Never named by #54.** Confidence arrives as a bare `int`; the factory-pair insertion is the **command layer's** call (FR-MC-016). |
| `BD_CONFIDENCE_NEUTRAL_PERMILLE` | #45 Appendix A | The honeymoon value an appointment initialises to — **#45's factory**, never `default` (FR-MC-017 / XC-054-004). |
| `SeasonState`, `RoundResolutionMode` | #30 | **Never named.** ERR-030-021's optional `ManagedClubId` is a #30-side change; `RoundResolutionMode` is the existing capability KD-4 relies on. |
| `ManagerProfile`, `ManagerMode` | **#26** | **Never named and never shadowed** (FR-MC-007). Listed here so the exclusion is deliberate rather than accidental — this is the **foreseen** third CS0104 instance. |
| `_RESERVED_0x2E_`, `SubsystemOrdinals.ManagerCareer` (96) | #16 §3.4 | `[CROSS-PENDING]` — the **placeholder** lands at approval (FR-MC-025); the **named tag** waits for the S3 draw site. |

### A.4 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `MC_GRACE_PERIOD_DAYS` | `90` | Days after appointment during which no termination fires (§3.1). **Not a nicety:** without it, appointment to a club whose confidence is already low — the realistic case — terminates the manager on his first evaluation. It is one of **two** independent guards on that case; FR-MC-017's honeymoon value is the other. |
| `MC_TERMINATE_CONFIDENCE_FLOOR` | `200` | Below this, the board has lost patience regardless of the objective. Deliberately aligned with #45's `Critical` band edge, so the two specs describe the same standing. |
| `MC_AT_RISK_CONFIDENCE_MAX` | `400` | The upper edge of the band in which a failed objective tips the decision. |
| `MC_REPUTATION_BASE` | `300` | A new manager's starting reputation, per-mille. |
| `MC_REP_PER_SEASON` | `10` | Per season served, per tenure. |
| `MC_REP_PER_TROPHY` | `60` | Per trophy won. |
| `MC_REP_FINISH_SCALE` | `40` | Scales `FinishTerm` — positive for a finish above expectation, negative below. |
| `MC_ATTR_W_POSITION` / `_FINANCE` / `_FACILITY` / `_SQUAD` | `40` / `20` / `15` / `25` | §3.5 attractiveness weights. Their sum is `MC_ATTR_W_TOTAL` (`[DERIVED]`, A.2). |
| `MC_BUDGET_EVALUATE_US` | `2` | §6.3 ceiling for one `EvaluateTenure`. A **ceiling, not a measurement** — no certified number exists for #54. |
| `MC_BUDGET_REPUTATION_US` | `50` | §6.3 ceiling for one full-career `ReputationOf`. Same caveat — **deliberately an order of magnitude above the expected cost**, so it trips on a real regression (an unbounded or non-linear projection) rather than on ordinary recomputation. It is the number a cache proposal will cite. |
| `MC_BUDGET_ATTRACTIVENESS_US` | `2` | §6.3 ceiling for one vacancy projection. Same caveat. |

**Where a reputation *value* is not.** No `Reputation` constant, field or default appears anywhere in this
catalogue — the `[GT]` rows above are **inputs to a projection**, not a stored quantity (FR-MC-013).
Reputation costs **zero bytes** (§6.4), and §5.2 asserts the field's absence structurally.

## Appendix B — Career save block layout (KD-7)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** — the
outer codec sees a length-prefixed byte block and never parses it (FR-MC-028).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `CAREER_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below it is interpreted (F9). |
| 2 | `ManagerId` | `i32` | |
| 3 | `CurrentTenure` | `i32` | `MC_UNEMPLOYED` (`-1`) or a valid index. **Coherence-gated on decode** (F6): a value pointing at a tenure whose `Reason != Open` throws. |
| 4 | `TenureCount` | `i32` | Length prefix — read through the overflow-safe bound compared against `total − offset`, never `offset + need` (F9). Bounded by `MC_MAX_TENURES`. |
| 5 | per tenure × `TenureCount` | — | `ClubId` (`i32`); `StartWorldDay` (`u32`); `EndWorldDay` (`u32`); `Reason` (`u8`); `SeasonsServed` (`i32`); `FinishCount` (`i32`) + that many `i32`; `TrophyCount` (`i32`) + that many `i32`. |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F9). |

Tenures are written in **chronological order** — which is also their APPEND order, so the block is a
function of state rather than of iteration (FR-MC-011). An empty career is a version header, a manager id,
`MC_UNEMPLOYED`, and **one zero count**.

**Decode validates, it does not trust** (FR-MC-031): `Reason` must be a defined `EndReason` and must not
be `Open` on a tenure carrying an `EndWorldDay` (F4); `EndWorldDay >= StartWorldDay` (F5); `FinishCount <=
SeasonsServed` and both within `MC_MAX_SEASONS_PER_TENURE` (F5); **at most one** tenure may be `Open`
(F1's decode half); and `CurrentTenure` must be `-1` or point at that open tenure (F6).

**That last pair is what makes the unemployed state unambiguous.** Without both gates, a career could
decode with two open tenures, or with `CurrentTenure` pointing at a closed one — each a **valid-looking,
incoherent** state that nothing downstream would catch.

**Deliberately absent — three things, each for its own reason:**

1. **Any reputation field** (FR-MC-013). **This is the point of temptation**: reputation is a single
   number everyone wants to read, cheap to cache and expensive to reconcile — and a stored scalar beside
   this stored history is exactly the two-truths shape `ERR-030-009` documents, which *"diverge at the
   first restore, with nothing to detect it"*. §5.2 asserts the absence **structurally**, because a prose
   rule does not survive a contributor who notices the recomputation.
2. **Any RNG cursor.** The minimal tier is draw-free (FR-MC-024), and the S3 job-market draw is specified
   as **keyed and position-independent** (FR-MC-026), so no cursor is ever persisted at any tier.
3. **Any copy of #45's confidence, #30's objective, or a club's values.** All three arrive as **routed
   integers** at the moment they are needed; mirroring any of them would create a second truth that only
   diverges *after* a restore.

**APPEND-only** (FR-MC-032). New fields go at the **end** behind a version bump. Appending an `EndReason`
member is **not** a layout change and needs no bump — but **reordering one is a silent catastrophe with no
gate to catch it**, and it fails **two** ways at once: every stored tenure re-reads under the wrong reason,
**and** every historical reputation changes, because the ordinal indexes Appendix C's weight table
(FR-MC-015).

## Appendix C — `EndReason` and the reputation terms

### C.1 The roster and its weights

| `EndReason` | Ordinal | `EndReasonTerm` (`[GT]`) | Meaning |
|---|---|---|---|
| `Open` | `0` | `0` | The tenure has not ended. `EndWorldDay` is meaningless and F4 rejects the pair if one is set. |
| `Sacked` | `1` | `−40` | Terminated by the board rule (FM-MC-01). **The only negative row.** |
| `Resigned` | `2` | `0` | The manager left of his own accord. |
| `ContractExpired` | `3` | `+5` | Saw out the term. |
| `MutualConsent` | `4` | `−10` | Left by agreement — softer than a sacking, not neutral. |

**`Open` carries a term of `0` and is included deliberately.** An open tenure still contributes its
seasons, trophies and finishes to the projection (§3.4 / T-MC-U-017), so the table must have a row for it
— and giving it a non-zero value would make reputation **jump on termination**, which reads as a bug and
is one.

**The ordinal is load-bearing twice** (FR-MC-015): it is **serialized** in the career block (Appendix B
row 5) **and** it **indexes this table**. A reorder therefore re-reads every stored tenure under the wrong
reason *and* changes every historical reputation, **with no version gate to catch either**. Both bands of
that failure are why the contract is APPEND-only and why T-MC-U-022 asserts each ordinal against its
pinned value rather than merely asserting the members exist.

This is the same class as #46's `ItemKind`, #35's `MediaIntent`, and #36's `NationId` — the fourth
instance in this wave of *an enum ordinal that is simultaneously a serialized value and a table key*.

### C.2 What the reputation projection sums

| Term | Source | Sign |
|---|---|---|
| `MC_REPUTATION_BASE` | a constant starting point | + |
| `SeasonsServed × MC_REP_PER_SEASON` | per tenure, **open tenures included** | + |
| `Trophies.Length × MC_REP_PER_TROPHY` | per tenure | + |
| `FinishTerm(f)` for each recorded finish | scaled by `MC_REP_FINISH_SCALE` | ± |
| `EndReasonTerm(Reason)` | C.1 | ± |
| *(clamp to `[0, 1000]`)* | **once, at the end** | — |

**The clamp is at the end, not per term** (T-MC-U-020): clamping each term would let a bad early spell
**saturate** the projection at zero, making a later recovery invisible — which is the opposite of what a
career record is for.

**Every term reads only the record** (FR-MC-014) — never the current club, the current confidence, or the
world day. Making reputation respond to *current* standing would let it move while the record stayed
still, which is the second-truth problem KD-2 forbids, reached from the other direction.

**Not tabulated: what a reputation value *means* to a consumer.** Whether `600` opens a job at a top club
is the job market's question (T3) and, later, #31's if reputation ever influences negotiation — each a
**value input**, never a #54 rule. Tabulating thresholds here would put a consumer's policy inside the
producer, which is the shape §1.2's out-of-scope table exists to prevent.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed incl. the `MC_UNEMPLOYED` definition and the overflow-bounding constants, A.2 Derived, A.3 Cross with the #26 exclusion listed deliberately, A.4 GT; B the career block with its coherence gates and three deliberately-absent items; C.1 the `EndReason` roster + weights and C.2 the projection's terms). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the three `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline (the #45 PASS-1 M-2 defect, now seen for the fifth time in this wave) — added to A.4, with the reputation budget's deliberately-high setting explained. **L:** A.1 gained `MC_MAX_TENURES` / `MC_MAX_SEASONS_PER_TENURE` (the bounds §6.4's career-length claim rests on) and the reason `MC_OBJECTIVE_NEUTRAL` is `[FIXED]`; A.2 added `MC_ATTR_W_TOTAL` with the reason a hand-set total is a silent scale factor; B gained the decode-validates paragraph, the two-gate argument for unambiguous unemployment, and the reputation-cache *point of temptation*; C.1 gained the `Open`-row rationale and the fourth-instance note on serialized-ordinal-as-table-key; C.2 added, with the clamp position and the read-only-the-record constraint. |
#endregion
