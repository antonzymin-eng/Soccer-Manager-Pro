# National Teams & International Management #36 — Section 9: Approval Checklist

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.3 — APPROVED: R-01..R-05 sign-off granted; back-props filed atomically)
**Last Updated (prior):** July 27, 2026 (v0.2 — G1 CLOSED; PASS-1 + AR-2 recorded)
**Version:** 0.3
**Status:** APPROVED

---

## 9.1 Content completeness

- [x] §1 scope / out-of-scope table / leaf DAG / **§1.4's three verification findings** / KD-1..KD-8 /
      determinism posture.
- [x] §2 FR-NT-001..034, data structures, failure modes F1..F9, and the *"an absent pin is not a
      failure"* note.
- [x] §3 FM-NT-01..06 with pin-then-derive and its inverse-transform walk, the re-key hook and its
      load-bearing ordering, the calendar derivation and its guard, the capped draw-free ranking, the
      pure-removal filter **with its order-independence proof**, the deferred squad resolution and the
      root composite, plus fifteen hand-verifiable worked examples.
- [x] §4 leaf assembly + DAG with the `ISquadProvider` trap, file layout with its three deliberate
      absences, the two existing #30 seams, the root composite, save composition, neighbour contracts.
- [x] §5 test plan led by **the two locks the spec exists for**, then units / composition / save /
      identity / structural / fail-loud + the T-phase closed-loop scenario.
- [x] §6 loop classification (world tick + seam, no hot path), cost profile, `[GT]` ceilings, memory.
- [x] §7 T0–T3 plan, the Stage-5 gate, deep-tier extensions, the not-planned list, risks R-1..R-6.
- [x] §8 XC-036-001..018 + the single back-prop + the deliberately longer not-a-back-prop list.
- [x] Appendices A (constants), B (save layout), C (the nation catalogue + id-range table).

## 9.2 Constant-tag discipline

- [x] Every constant in Appendix A carries exactly **one** of `[FIXED]` / `[DERIVED]` / `[CROSS]` /
      `[CROSS-PENDING]` / `[GT]`.
- [x] No `[EST]` remains (none was introduced).
- [x] Empty regions omitted (#20 prohibits them).
- [x] `[DERIVED]` rows document their formula — `NT_WEIGHT_TOTAL` derives from the catalogue's weights,
      and **T-NT-U-004 locks it**, because setting it independently makes §3.1's terminating `throw`
      reachable.
- [x] `[CROSS]` rows name their authority and are consumed read-only — #36 re-declares none of #27's
      types and never names `ISquadProvider` (T-NT-BOUND-002/006).
- [x] `[CROSS-PENDING]`: `_RESERVED_0x28_` / `SubsystemOrdinals.NationalTeams = 90` — **already present
      in #16 and already correct**; it stays reserved, possibly permanently (KD-8).
- [x] **`NT_WEIGHT_TOTAL` and the catalogue weights are `[GT]` with a save-visible caveat** (FR-NT-014):
      changing them changes `NationOf` for every existing player in every existing career. Recorded at the
      declaration, not only in §7.5.
- [x] The `[GT]` magnitudes are declared **illustrative pending the T3 balance pass**, and §5 asserts only
      shape, bounds and direction — never magnitude.

## 9.3 Verification of load-bearing claims (checked against source, not asserted)

- [x] `PlayerRecord` is `{ PlayerId, FirstName, LastName, Age, Position, Attributes }` and a
      case-insensitive search for *nationality* / *nation* across `docs/specs/` and `src/` returns **no
      owner and no field**. **The fact the entire spec rests on.** *(`src/player-database/PlayerRecord.cs`)*
- [x] `RosterGenerator` consumes **exactly** `PlayerDatabaseConstants.FIELDS_PER_PLAYER` draws per player
      under an explicit ORDINAL STABILITY contract, and the additive `Generate(…, PlayerPosition[])`
      overload was written to keep the draw budget **byte-identical**.
      *(`src/player-database/RosterGenerator.cs`)*
- [x] Club rosters are **regenerated from the world seed, never saved**, so a change to draw order or
      count *"would silently rewrite every club in every existing save with the whole suite green"* —
      which is why `LeagueBootstrapGoldenVectorTests` pins a golden digest. **The cost KD-1 avoids.**
      *(root `CLAUDE.md` league-bootstrap AR-5 H-1)*
- [x] #31's KD-7 **re-keys** the club-scoped `PlayerId` on transfer; #44 **migrates** bans across it
      (FR-DC-013) and #32 **drops** knowledge at it. **The fact that makes the derivation alone
      insufficient**, and the source of the migrate-vs-drop contrast FR-NT-023 resolves.
- [x] #31 FR-TX-022 is the roster-move hook #44 already uses — so #36 is a **second subscriber**, not a
      new mechanism.
- [x] #30 FR-SN-013 pins a **resolve → filter → configure** null seam, and #44's FR-DC-010 makes it *"a
      value-copy reduction"* applied to **both** clubs — the shape #36's filter copies, needing **no new
      #30 seam**. *(`season-competition-loop/section-2.md`, `discipline-suspensions/section-2.md`)*
- [x] #31 FR-TX-019: *"The transfer window MUST be a #31-owned `TransferWindow` derived deterministically
      from #30's `SeasonCalendar` (read-only) … #31 MUST NOT mutate the calendar."* The precedent #36's
      window stands in exactly. *(`transfers-contracts-negotiation/section-2.md`)*
- [x] #43 FR-CP-001/005/006/007/009: formats, canonically ordered **`int`** entrant sets,
      `FixtureScheduler.Generate(clubIds, seed)` reuse, and **keyed cursor-free** draws.
      *(`competition-structure/section-2.md`)*
- [x] **`FixtureScheduler.Generate(int[] clubIds, ulong seed)` is verifiably id-agnostic** — the signature
      KD-3's disjoint-range answer depends on, and the reason **#43 needs no change**.
      *(`src/season-save/FixtureScheduler.cs`)*
- [x] **`ISquadProvider` is declared in `src/match-engine/`** — the fact FR-NT-004 rests on, and the
      reason `League` (in `season-save`, which already references `match-engine`) may implement it while
      #36 may not.
- [x] `LineupSelector` **fails loud** on an unfillable starter line — the fact that makes the empty-squad
      floor a real risk rather than a theoretical one. *(league-bootstrap KD-6)*
- [x] #16 §3.4 carries *"**Reserved — held for National Teams #36 per roadmap §6 (`SubsystemOrdinals`
      90); MUST NOT be reused.**"* — so **no #16 back-prop at approval**.
      *(`deterministic-sim/section-3.md` §3.4, v1.0.13 A-04 sweep)*
- [x] #33 FR-HS-008 pins the unadvanced cursor sentinel at `uint.MaxValue`, **not** `0` — adopted
      verbatim for `WindowCursor`.
- [x] **`ERR-030-016` is free** — the filed rows reach `ERR-030-014`, and `-015` is claimed by #46 in this
      same wave. *(`docs/tracking/spec-error-log.md`; `news-inbox-man-management-design.md`)*
- [x] `FR-NT-*` is **unclaimed** — verified by enumerating every `FR-[A-Z]{2,3}-` prefix in `docs/specs/`.

## 9.4 Gates

| Gate | Owner | Status |
|---|---|---|
| **G1** — section-file PASS-1 adversarial review + a fix pass, to convergence. | drafter | ✅ **CLOSED** — see §9.4.1 |
| **G2** — file **ERR-030-016** atomically with the status flip. | drafter | ✅ **CLOSED** — filed and RESOLVED July 27, 2026, atomically with the flip (`spec-error-log.md` v1.47) |
| **G3** — lead-developer R-01..R-05 sign-off. | lead developer | ✅ **CLOSED** — R-01..R-05 granted by the lead developer, July 27, 2026 |
| **G4** — `SPEC_INDEX.md` registry row + Registry-Changes entry, added at promotion. | drafter | ✅ **CLOSED** — row + Registry-Changes entry landed July 27, 2026 |

**Not gating (deferred by design, recorded so they are not mistaken for omissions):** the outer
`SEASON_SAVE_FORMAT_VERSION` bump (T2); #43 instance registration, the root composite, and the #29/#41
minute routing (all T3, and all gated on the **Stage-5 global sim** rather than on #36); the
`_RESERVED_0x28_` promotion (only if a #36-owned draw ever appears); and the T3 `[GT]` balance pass.

**#36 carries no prerequisite gate.** Unlike #35, it cites **no step number** in #30's currently-malformed
tick order — its filter attaches to a seam identified by name, not by position — so #35's G0 does not
gate it.

### 9.4.1 PASS-1 adversarial review record (G1)

**PASS-1: 0H + 4M + 6L, all resolved in the v0.2 fix pass.** The M findings cluster in one place: the
`NationPin` mechanism, which the supplement introduced late (at its own AR-2) and which three subsequent
AR rounds were still catching up with.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| M-1 | M | **The re-key hook's ordering was stated but not enforced.** `OnPlayerReKeyed` must resolve the **pre**-transfer nation, and a hook invoked after the old id is unresolvable would pin the *post*-transfer derivation — **silently recording the wrong nationality via the very mechanism meant to prevent it**. Worse, a test asserting *"a pin exists after a transfer"* passes against that bug. | New **F4** + **FR-NT-011**, with §3.2 marking the ordering load-bearing; **T-NT-U-008** and the §5.1 transfer lock both assert the **value**, not the presence. |
| M-2 | M | **Nothing said a pin equal to its derivation must still be written.** Skipping it is the obvious optimisation and it is wrong: the coincidence does not survive the *next* transfer, so the "redundant" pin is exactly the one the following re-key needs. | New **FR-NT-012**; §3.8(g) and **T-NT-U-009** lock it, with the reason attached. |
| M-3 | M | **Nothing bounded the pin table's writers.** KD-1's whole cost argument is that the table is bounded by **transfer volume, not pool size** — but a stray write for an untransferred player was undetectable, and enough of them make the table exactly the per-player stored field KD-1 declines to add. | New **F3** + **FR-NT-010**'s write restriction; **T-NT-U-011** asserts it. |
| M-4 | M | **The per-club cap's application point was unspecified.** Capping *after* selecting the best 23 yields a **different squad** and — worse — one that depends on the trim order. Capping inside the greedy walk lets the next-best eligible player take the place, which is both the intended behaviour and order-free. | §3.4 pins it inside the walk; **T-NT-U-013** asserts against the trimmed alternative. |
| L-1 | L | **KD-8 lived as a clause inside KD-3**, though the draw-free property spans KD-3 *and* KD-5 and is the first thing a determinism reviewer looks for. | Promoted to a key decision of its own (§1.5), with **FR-NT-030** recording that a future promotion happens at a first draw site, on the record. |
| L-2 | L | **The `PlayerId` tie-break was stated without its reason.** Mean attributes tie **constantly** in a generated league — every roster is drawn from one distribution — so without it the selection depends on enumeration order, which survives every same-process test and breaks across a restore. | §3.4 states it; **T-NT-U-014** permutes the input pool. |
| L-3 | L | §3.1's terminating `throw` looked like a runtime branch. It is an internal-invariant abort, unreachable while `NT_WEIGHT_TOTAL` derives from the weights — which is a `[DERIVED]` contract nothing asserted. | Annotated as an abort; **T-NT-U-004** locks the derivation so a maintainer cannot set the total independently. |
| L-4 | L | The **filter's pure-removal property** was an observation, not a requirement — while the entire order-independence argument (and ERR-030-016's contract note) depends on it. | New **FR-NT-017**; **T-NT-U-021** asserts the subset property directly. |
| L-5 | L | The **`NationOf` cache** question was unaddressed. It is the obvious optimisation and it would go stale at exactly the re-key event the pin exists to handle — **silently**. | §6.2 records the deliberate absence and why; §7.4 lists it as not-planned. |
| L-6 | L | `NationId`, `WindowCursor` and `IntlMinutes` were described in prose only, and §5 had no `NT_WEIGHT_TOTAL` or catalogue-ordinal lock. | Written out in §2.2; **T-NT-U-007** added for the catalogue's save-correctness ordinal contract. |

**AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle, per the
project convention). **L-1:** §7.1 placed the **re-key hook** at T3 with the rest of the deep tier, but
transfers happen from the moment #31 is live and every transfer without the hook writes a silently wrong
nationality into a career — pulled forward to **T2**, with the sequencing note attached. **L-2:** §6.4 did
not state that the **pin table is the one #36 collection that grows with career length**, nor that
FR-NT-013's drop-on-retire is the only thing bounding it — a partial implementation that stops iterating
the player but leaves the row is the realistic failure, and T-NT-U-012 asserts both halves. **L-3:** §8.5
did not say that the **nation catalogue is not a citation surface** — its weights are `[GT]` tuning
values, and tabulating real-world demographics would give them a false authority the distribution test
deliberately does not assert against.

## 9.5 Sign-off

| Role | Criterion | Signed |
|---|---|---|
| R-01 | Scope and out-of-scope boundaries are unambiguous; no model #36 does not own is duplicated, and the #43 / #44 / #30 boundaries are explicit rather than implied. | ⏳ pending |
| R-02 | Every formula has units, ranges, and at least one worked example; no fabricated verification values — **and no test pins a `Derive` output**, which would be a fabricated hash. | ⏳ pending |
| R-03 | Determinism posture is complete: the draw-free claim at every tier, the pin-then-derive contract, the catalogue's ordinal-stability rule, and the absence of any cursor or cache are each justified rather than asserted. | ⏳ pending |
| R-04 | Persistence is version-gated, opaque, fail-loud, canonically ordered and APPEND-only; **the pin table's presence in the blob is argued**, not merely listed. | ⏳ pending |
| R-05 | Cross-spec back-props are enumerated with owners and timing, the proposed ERR id is verified free, and the **#27 no-change claim** — the one that makes this spec cheap — is asserted by test rather than by assurance. | ⏳ pending |

## 9.6 Decision

**APPROVED — July 27, 2026.** Lead-developer **R-01..R-05 sign-off granted**, and the back-props filed and RESOLVED **atomically with the flip** per this spec's own promotion pipeline step 6: **ERR-030-016** (`spec-error-log.md` v1.47). All 11 section files carry `Status: APPROVED`; the `SPEC_INDEX.md` row records the date.

**What approval does and does not mean here.** It approves the **forward design** — the #21–#30 pre-T0 precedent — not an implementation: #36 has **no `src/` assembly**, and its §7 T-phase plan is the sequence for building one. Items listed as *not gating* above remain open by design and are named at their tiers.

**The prior decision text is retained below, because the reasoning it records is what the sign-off was granted against.**

**(prior, recorded at `IN REVIEW`)** — G1 closed (PASS-1 0H+4M+6L → AR-2 0H+0M+3L convergence, §9.4.1). G2–G4 remain open:
the back-prop lands atomically with the status flip, sign-off is a human authority, and the registry row
is added at promotion.

**What verification did to this spec, restated at the decision point.** #36's plan led with the Stage-5
global-sim dependency. Checking against source found something more immediate and entirely unmentioned:
**the game has no concept of a player's nationality**, and the obvious fix — a drawn `PlayerRecord` field
— would cost a `FIELDS_PER_PLAYER` bump, a golden-vector rebaseline, and **a silent rewrite of every
existing save's rosters**, because club rosters are regenerated from the world seed rather than saved.

KD-1's pin-then-derive answer makes #36 free on that axis: **#27 is untouched at every level.** The
Stage-5 gate then turns out to be the *easier* problem — the machinery for tournaments already exists in
#43, and what is missing is only opponents to field.

**Two things this spec should be judged on.** First, it files **one doc-only back-prop** while introducing
both a new concept and a new class of entity — the measure of how much was already waiting upstream.
Second, its single most important test is **an assertion that another spec's golden vector is unchanged**,
placed inside #36's own suite so that whoever proposes *"just add the field"* fails #36's tests as well as
#27's. That is where the cost of the rejected alternative needed to become visible.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §9 (completeness, tag discipline, the §9.3 source-verified claims table, four gates plus the explicit note that #36 carries **no** prerequisite gate unlike #35, R-01..R-05). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | G1 CLOSED: §9.4.1 records the section-file PASS-1 (0H+4M+6L, all resolved — clustered on the `NationPin` mechanism the supplement introduced late) and the AR-2 convergence sweep (0H+0M+3L, headed by pulling the re-key hook forward to T2). §9.1 completeness updated for KD-8 and FR-NT-010/011/012/017/030; §9.2 gained the `NT_WEIGHT_TOTAL` derivation lock and the save-visible `[GT]` caveat; §9.3 gained the `ISquadProvider`-declaration-site row, the `FixtureScheduler` signature row, the `ERR-030-016`-is-free check and the `FR-NT` prefix check. G2–G4 remain open. |
| 0.3 | 2026-07-27 | — | **`IN REVIEW → APPROVED`.** Lead-developer R-01..R-05 sign-off granted. Back-props **ERR-030-016** filed and RESOLVED atomically with the flip (`spec-error-log.md` v1.47). Gates G2–G5 closed; §9.6 decision updated. All 11 section files flip to `Status: APPROVED`. |
#endregion
