# Competition Structure #43 — Section 8: References & Cross-References

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 8.1 Cross-spec cross-references (XC-043-*)

| ID | Direction | Target | Contract |
|---|---|---|---|
| XC-043-001 | #43 → #30 (via composition root) | `FixtureScheduler.Generate(clubIds, seed)` (FR-SN-001, pure); `LeagueTable`/`Empty` + the FR-SN-007 total order; the resolution paths (FR-SN-013/013a); `SeasonViewModel` value-copy reads (FR-SN-033) | the instance-ready machinery #43 reuses per instance; instance 0 is a **binding** — #43 holds no #30 object (FR-CP-002); #30 §7's generalization row pre-declares this composition. |
| XC-043-002 | #43 → #30 (the boundary roll) | FR-SN-031's insertion point **(a')** (pre-declared; before #40's (b')); FR-SN-029 restartability; FR-SN-032 sole-writer | the promotion/relegation transform site; the code-side (a') execution hook + deep fixture-day driver are the soft-reserved **ERR-030-008** T-phase coordinations (FR-CP-017). |
| XC-043-003 | #43 ↔ #40 (indirect) | `SettleFinances` at (b'), "positioned AFTER the (a') #43 insertion … the budget depends on the post-promotion division" (#40 §1) | the (a')→(b') ordering, recorded from both sides; #43 owns no money (FR-CP-021); per-competition prize money is a #40 deep extension. |
| XC-043-004 | #43 → #27 | the club-id universe (stable `ClubId`s) | entrant identity, read-only; the transform never re-keys a club (FR-CP-016). |
| XC-043-005 | #43 → #16 | `_RESERVED_0x2C_` / `SubsystemOrdinals.Competition = 94` (created by ERR-043-001, RESERVED); the `competition.draws` keyed stream (`entityId = competitionId`) at the deep first draw | draw-free minimal (KD-2); fixed-radix ordinals (§3.2, the #41 §3.1.1 mechanism); promotes spec-text-first at T3. |
| XC-043-006 | #43 → #44 (deferred, producer side) | `CompetitionId` on #43 fixtures/results (FR-CP-020) | the per-competition suspension-scoping surface; no #44 interface built (FR-LW-031). |
| XC-043-007 | #43 → #36 / #38 (deferred, producer side) | the competition/calendar model + bracket view models (FR-CP-022) | #36 overlays later; #38 renders; no interface built (FR-LW-031). |

## 8.2 Determinism references

- `_RESERVED_0x2C_` / `0x2C` / [FIXED] — **created at #43's approval by the ERR-043-001 A-04
  placeholder sweep** (with `_RESERVED_0x2B_` #42 and `_RESERVED_0x2D_` #45 — the #16 §3.4
  catalogue ended at `0x2A`, verified). Stays RESERVED at approval (draw-free minimal); promotes
  to `DOMAIN_TAG_COMPETITION = 0x2C` at #43 T3's first knockout draw (siteId `competition.draws`).
- The keyed-draw mechanism is the #41 §3.1.1 / #32 §3.3 fixed-radix action-ordinal bijection;
  #30's FR-SN-013a quick-sim keyed draws are the in-family precedent for competition-scoped keys.

## 8.3 Back-prop references

- **ERR-043-001 (proposed, at #43 approval)** — #16 §3.4 gains the three A-04 placeholder rows
  `_RESERVED_0x2B_` (Youth Academy #42, ordinal 93) / `_RESERVED_0x2C_` (Competition Structure
  #43, ordinal 94) / `_RESERVED_0x2D_` (Board & Ownership #45, ordinal 95), completing the roadmap
  §6 contiguous block `0x20`–`0x2D` (the v1.0.13 "every allocation gap must have an explicit
  placeholder" precedent). Pure namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump.
- **ERR-030-008 (soft-reserved, at the #43 T-phase — NOT filed at approval)** — the #30 code-side
  coordinations: the (a') execution hook (T2) + the deep multi-competition fixture-day driver
  (T3). Reserved by name here so the number is not reused (the ERR-030-005 soft-reserve
  precedent).
- **ERR-016 pattern (deferred, at #43 T3)** — the `DOMAIN_TAG_COMPETITION = 0x2C` promotion at the
  first draw.
- **No #30/#40/#27 spec-text change at approval** — FR-SN-031's (a') and #40's (b') ordering
  pre-exist; #43 is the first management spec whose #30 spec-text seams were all reserved ahead.

## 8.4 Master-plan & literature anchors

- Master development plan §4.1/§5 (competition structure; cups/promotion) — the staging source. No
  external academic citation is load-bearing for the deterministic draw/transform machinery (a
  game-design surface — the #40/#41/#34/#32 posture); real-competition format references (seeding
  pots, two-legged ties) are recorded at the Stage-5 extensions (§7.2), not here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §8 (XC-043-001..007, determinism reference, back-prop references incl. the ERR-030-008 soft-reserve, master-plan anchor), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
