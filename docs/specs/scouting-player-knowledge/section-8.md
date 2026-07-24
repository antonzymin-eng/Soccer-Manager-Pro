# Scouting & Player Knowledge #32 — Section 8: References & Cross-References

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Cross-spec cross-references (XC-032-*)

| ID | Direction | Target | Contract |
|---|---|---|---|
| XC-032-001 | #32 → #27 | `PlayerRecord { PlayerId, FirstName, LastName, Age, Position, Attributes }`; `PlayerAttributes` 31 `int [1,20]` (FR-SQ-002) + `WeakFootRating [1,5]` (FR-SQ-003); `AttrIdx` ordinal map (FR-SQ-006); `CLUB_SQUAD_SIZE = 25`; `PlayerId = clubId·CLUB_SQUAD_SIZE + localIndex` | the truth the view masks — **read-only, never written** (FR-SC-001); the own-squad check derives from the id formula (§3.1). |
| XC-032-002 | #32 → #34 (deep) | `ToScoutQuality(in StaffRecord scout) → int` (ChiefScout role slot, #34 §3.1; XC-034-008 reciprocal) | #32 consumes the quality projection read-only and defines its baseline `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000` (closing #34's "a baseline #32 will define"; value-compatible with #34's neutral `FacetPermille` row — no #34 edit). Speed-only consumption (KD-4). |
| XC-032-003 | #32 → #30 (via composition root) | `RunWorldTickInFixedOrder` slot (new step 7, ERR-030-007); `SeasonSaveCodec`/`SEASON_SAVE_FORMAT_VERSION` (compose); `ISquadProvider` (truth resolution) | #30 invokes #32 + owns the save root (KD-6/KD-7); #32 never references #30. |
| XC-032-004 | #32 → #16 | `_RESERVED_0x24_` / `SubsystemOrdinals.Scouting = 86` (RESERVED); world-tick `DeterministicRngService` keyed draws (deep only, `scouting.accuracy`, `entityId = playerId`) | draw-free minimal (KD-3); promotes at the deep first accuracy draw (spec-text-first). |
| XC-032-005 | #32 ↔ #31 (indirect) | `SubmitBid` (FR-TX-025); the truth-based counterparty valuation (FR-TX-001); the counterparty-generic seam (FR-TX-010/011) | #32 informs the manager's decision; the action stays #31's; fog never touches the counterparty's valuation. The FR-TX-010 reuse expectation activates at the far-deep AI-scouting tier — recorded here so it stays honest. #31 built no #32 interface. |
| XC-032-006 | #32 ↔ #28 (indirect) | #28 growth/decline mutates the truth the live-form window derives from (FR-SC-010); #28 retirement/regen triggers the FR-SC-019 entry drop (via the season-boundary lifecycle coordination #31 FR-TX-028 names) | no #28 reference; PA/potential estimates are a §7 extension reading #28 read-only. |
| XC-032-007 | #32 → #38 / #46 (deferred, producer side) | `KnownPlayer`/report view surface (the FR-UI-002 immutable-projection shape); band-up report events | #32 publishes the view; #38 renders, #46 aggregates; no interface built (FR-LW-031). #38 MUST NOT reach around the view to truth for external players (FR-UI-004). |
| XC-032-008 | #32 → #49 / #22 (non-reference) | structured reports only — no display text (the #49 localize-after-generate boundary) | prose is presentation-side; the plan's floated #22 `InteractionTextGenerator` consumption is **rejected** (assembly coupling + a `world.text` draw for a presentation artifact). |

## 8.2 Determinism references

- `_RESERVED_0x24_` / `0x24` / [FIXED] — the #16 §3.4 placeholder row
  (`deterministic-sim/section-3.md:268`), held for #32, `SubsystemOrdinals.Scouting = 86`, whose
  rationale already anticipates promotion at #32's first draw. **Stays RESERVED at #32 approval**
  (draw-free minimal, KD-3) — the #40 `_RESERVED_0x29_` (ERR-040-001) / #31 `_RESERVED_0x23_` / #34
  `_RESERVED_0x26_` precedent. Promotes to `DOMAIN_TAG_SCOUTING = 0x24` at #32 T3's first accuracy
  draw (siteId `scouting.accuracy`).
- The keyed-draw mechanism is the #41 §3.1.1 fixed-radix action-ordinal bijection (`DeriveScoutOrdinal`,
  §3.3) — same append-parity discipline, **different key** (deliberately no `worldDay`, FR-SC-011).

## 8.3 Back-prop references

- **ERR-030-007 (proposed, at #32 approval)** — #30 §3.3 `RunWorldTickInFixedOrder` gains the
  scouting tick-order null-seam slot as new step 7, after staff (#34) and before the world-day tick
  (`AdvanceDay` → step 8); FR-SN-034's enumeration updated (the ERR-030-002 #41 / ERR-030-004 #31 /
  ERR-030-006 #34 precedent — a new insertion, since FR-SN-034 enumerates #28/#29/#33/#41/#31/#34
  only). Doc-only; the seam is empty until #32 T2/T3.
- **ERR-016 (deferred, at #32 T3)** — `DOMAIN_TAG_SCOUTING = 0x24` promotion at the first accuracy
  draw.
- **No #34/#31/#27/#38 back-prop** — #34's open baseline is closed by a #32-owned constant; the
  others' #32-facing contracts are already recorded from their side (FR-ST-021, FR-TX-010/011,
  FR-UI-002/004).

## 8.4 Master-plan & literature anchors

- Master development plan §5 (recruitment/scouting) — the staging source; the roadmap §5 invariant
  ("a per-manager view over true attributes, never a mutation") is this spec's governing contract.
  No external academic citation is load-bearing for the deterministic masking model (a game-design
  tuning surface, not an empirical model — the #40/#41/#34 posture). Any deep-tier width/cadence
  calibration references are recorded at the balance pass, not here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §8 (XC-032-001..008, determinism reference, back-prop references, master-plan anchor), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
