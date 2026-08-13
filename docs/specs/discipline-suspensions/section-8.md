# Discipline & Suspensions #44 — Section 8: References & Cross-References

**Created:** July 24, 2026
**Last Updated:** August 13, 2026 (v0.2 — L6, adversarial review over the C1/C2 landing: §8.3's
ERR-030-009 entry annotated LIVE since T2, so the "null seam until #44 T2 wires it" clause reads as
the historical approval-time text it is rather than the current state)
**Last Updated (prior):** July 24, 2026 (v0.1 — initial)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Cross-spec cross-references (XC-044-*)

| ID | Direction | Target | Contract |
|---|---|---|---|
| XC-044-001 | #44 → engine events (via the tap) | `CardIssuedEvent` 0x06 `{ int Recipient (agent id); byte CardKind 0/1/2; ushort FoulOrdinal }`; `SubstitutionEvent` 0x08 `{ int Outgoing, Incoming; byte Team, SubstitutionReason }`; both Tier A with tick headers (verified `CardIssuedEvent.cs`/`SubstitutionEvent.cs`) | the fold's inputs; the **single-event kind-2** emission contract (`ApplyCardAndCheckSentOff`, `MatchEngine.cs`) is the KD-5 de-dup rule; `Recipient` is an agent id — occupancy attribution required (KD-2). |
| XC-044-002 | #44 → #37 (pattern reuse) | the read-only per-tick ledger tap (FR-AN-002; lossless; unknown ordinals ignored, FR-AN-019/F5) | the approved observational read #44 consumes; one tap feeds both when built; neither references the other. |
| XC-044-003 | #44 → #30 (via composition root) | FR-SN-013's managed-fixture flow (resolve → configure); FR-SN-013a card-free quick-sim; `SeasonSaveCodec`; `RollToNextSeason` | the **ERR-030-009** resolve→*filter*→configure null seam (the one approval-time back-prop); serving reported per played fixture on both paths; the boundary rule (yellows reset, bans carry). |
| XC-044-004 | #44 → #27 | `PlayerId`/`Squad` (read-only) | `FilterAvailable` returns a reduced **value copy**; #27 state never written (FR-DC-001/009). |
| XC-044-005 | #44 ↔ #31/#28 (indirect) | the FR-TX-022 roster-move hook / FR-TX-028 #28 lifecycle coordination | delivers the KD-6 hygiene: **migrate** on re-key (bans follow the player — the recorded contrast with #32's drop rule), drop on retirement; T-phase wiring. |
| XC-044-006 | #44 → #43 (deferred) | `CompetitionId` on #43 fixtures/results (FR-CP-020) | the partition key FR-DC-012 pre-shapes (`0` at minimal); activation is deep; no #43 assembly reference (an `int` key). |
| XC-044-007 | #44 → #38 / #46 (deferred, producer side) | availability/suspension view models (read-only value copies) | #38 renders, #46 aggregates; no interface built (FR-LW-031). |

## 8.2 Determinism references

**#44 registers no RNG stream, no domain tag, and no `SubsystemOrdinals` entry** — the #37/#38/#49
read-only class (roadmap §6 lists #44 read-only). This is a positive property, not a deferred
allocation: no #16 §3.4 row exists or is needed. The accumulation is a pure fold over
already-deterministic Tier A events in the bus's canonical publish order; any future quick-sim
card synthesis draws on **#30's** `0x22` stream (FR-DC-019), never a #44 stream.

## 8.3 Back-prop references

- **ERR-030-009 (proposed, at #44 approval)** — #30 FR-SN-013's managed-fixture flow gains the
  pre-declared **availability-filter null seam**: "the resolved squad MAY be filtered through the
  #44 availability view (a value-copy reduction) between `ISquadProvider.ResolveByClubId` and
  `ConfigureSquads`" — a null seam until #44 T2 wires it (the ERR-030-002/004/006/007
  pre-declaration pattern, flow-side rather than tick-order-side). Doc-only. **LIVE since T2 (C1/C2,
  August 13, 2026)** — recorded here as the historical text of the back-prop AT APPROVAL; the seam
  is no longer null (see §7.3).
- **ERR-030-008 (soft-reserved by #43)** — noted to keep the numbering straight: #43 holds 008;
  #44 takes **009**.
- **No #16/#37/#43/#27/#17 change at approval.**

## 8.4 Master-plan & literature anchors

- Master development plan §4.1 (season-level discipline) — the staging source. Threshold/ban
  magnitudes are game-design `[GT]` tuning (real-competition rules — e.g. 5-booking bans — inform
  the balance pass, recorded there, not here). No external academic citation is load-bearing (the
  #40/#41/#32/#43 posture).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §8 (XC-044-001..007, the no-determinism-row positive property, back-prop references, master-plan anchor), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-08-13 | — | **L6** (adversarial review over the C1/C2 landing): §8.3's ERR-030-009 entry annotated LIVE since T2 — the "null seam until #44 T2 wires it" clause is the approval-time back-prop text, frozen as a historical record, not a claim about the current state. |
#endregion
