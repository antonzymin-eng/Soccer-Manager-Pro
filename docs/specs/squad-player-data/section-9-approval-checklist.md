# Squad / Player Data Layer Specification #27 — Section 9: Approval Checklist

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

Checklist entries are verified against actual files; nothing below is checked without a
programmatically verifiable anchor (CLAUDE.md "Never fabricate verification values"). This spec
**documents an already-built, already-wired layer** (code precedes the numbered spec), so the
implementation gates are checked and the review/sign-off gates remain open by construction.

## 9.1 Content gates

- [x] Every constant in Appendix A carries exactly one source tag, verified against
      `src/player-database/PlayerDatabaseConstants.cs` (`[FIXED]`: `ATTRIBUTE_MIN/MAX`,
      `WEAK_FOOT_MIN/MAX`, `CLUB_SQUAD_SIZE`; `[DERIVED]`: `ATTRIBUTE_COUNT`,
      `IDENTITY_DRAWS_PER_PLAYER`, `FIELDS_PER_PLAYER`; `[GT]`: means/spreads/ages/position-bias;
      `[CROSS]`: `DOMAIN_TAG_PLAYER_DATABASE`, `SubsystemOrdinals.PlayerDatabase`)
- [x] Every §3 formula/draw sequence has ranges and a worked example (one generated `PlayerRecord`)
- [x] No `[EST]` tags present
- [x] KD-2 scale isolation: `WeakFootRating` [1,5] excluded from the [1,20] array & clamp helpers
- [x] KD-3 identity separation: `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex` (club ≠ match teamId)

## 9.2 Implementation status (unusual — code precedes the spec)

- [x] FR set complete and stable: FR-SQ-001..026 (grep-verified in §2/§5)
- [x] Layer LANDED + tested — `src/player-database/*.cs` + `tests/`; full dotnet gate green
- [x] T-phase wiring LANDED (§7.1): T1/T2 (`PlayerAttributeProjection` + `ConfigureSquads`, closes
      `ERR-007`), T3 (`_rosterClubId` at `SNAPSHOT_SCHEMA_VERSION` 16), Phase-2 (`ISquadProvider`
      re-projection), `LineupSelector` — each with its governing sibling design doc cited in §7
- [x] Two-run determinism proven (same seed ⇒ byte-identical `Squad`, FR-SQ-016)

## 9.3 Review gates — OPEN

- [x] **PASS-1 adversarial review of section files — RUN July 22, 2026 (0H+1M+2L); v0.2 fix pass
      applied.** M-1: the `Squad` constructor's F3 guards (null/empty/oversize) + the §2.2.4
      snapshot-copy invariant had no direct test (both callers pre-guard) — added `SquadTests.cs`
      + corrected the §5 F3 labels. L: FR-SQ-018 default-name (`FirstName "Player"` / `LastName =
      playerId`, was "Player N"); §9.4 prefix list gained the missing `PR`.
- [ ] **AR-2 convergence sweep — pending** (re-review after the v0.2 fixes; the cycle closes on an
      L-only or clean round per the #21–#26 convention).
- [ ] PASS-2 (only if a sweep finds a High)
- [ ] **Formal `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` / `SubsystemOrdinals.PlayerDatabase = 81`
      cross-cite confirmation in Deterministic Sim #16 §3.4 — code allocation verified
      (`DeterministicSimConstants.cs`, `SubsystemOrdinals.cs`); the #16 spec-text row + back-prop
      ERR (ERR-022-001 precedent) NOT yet filed.**
- [ ] **Lead-developer R-01..R-05 sign-off — PENDING.**

## 9.4 Consistency gates

- [x] FR prefix `FR-SQ-` verified unclaimed by grep over `docs/specs/**/*.md` (existing prefixes:
      AT, BU, CS, DA, DM, DS, EVT, GK, HE, LW, PA, PO, PR, RO, TI, TP, TS)
- [x] Candidate number #27 matches the design-supplement reservation
- [x] `SPEC_INDEX.md` row added (`IN REVIEW`)

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: OPEN — NOT signed.** The spec is `IN REVIEW`; sign-off is pending PASS-1.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — all sections (§1–§9 + appendices) present per the 9-section template | `outline.md`, `section-1..8`, `section-9-approval-checklist.md`, `appendices.md` | ☐ |
| R-02 | **Technical accuracy** — canonical record / `AttrIdx` map / draw sequence / position-bias / `WeakFoot` jitter / `SquadFileLoader` grammar internally consistent; 26 FRs; constants one tag each, no `[EST]` | §2/§3/Appendix A; verified against `src/player-database/*.cs` | ☐ |
| R-03 | **Cross-spec consistency** — `DOMAIN_TAG`/`SubsystemOrdinals` cross-cite confirmed in #16 §3.4; `ERR-007` closure recorded; T-phase wiring docs cited (§7/§8) | §8.1 / §7.1 / #16 §3.4 | ☐ |
| R-04 | **Stage-binding correctness** — KD-6 club-setup-time (not per-tick / not zero-alloc); KD-7 boot-constant snapshot exclusion (T3 is a header id, not per-player values); KD-8 text import is not a wire format | §4 / §6 / §7 | ☐ |
| R-05 | **Approval granted** — PASS-1 resolved; `SPEC_INDEX.md` row flipped `IN REVIEW → APPROVED` | pending | ☐ |

## 9.6 Decision

**IN REVIEW — sign-off NOT granted.** The layer is fully built, wired, and tested (§9.2), but the
section-file PASS-1 adversarial review + v0.2 fix pass have not run, the #16 §3.4 cross-cite
confirmation is not filed, and lead-developer R-01..R-05 sign-off is pending (§9.3/§9.5). No
approval is claimed.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial checklist. Implementation gates checked (code precedes the spec); PASS-1, #16 cross-cite confirmation, and R-01..R-05 sign-off OPEN. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | PASS-1 run (0H+1M+2L); §9.3 updated — PASS-1 checked, AR-2 sweep pending. §9.4 prefix list gained `PR`. |
#endregion
