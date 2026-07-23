# Squad / Player Data Layer Specification #27 — Section 9: Approval Checklist

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.3 — AR-2 convergence sweep + #16 §3.4 cross-cite filed + R-01..R-05 signed)
**Version:** 0.3
**Status:** APPROVED

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

## 9.3 Review gates — CLOSED

- [x] **PASS-1 adversarial review of section files — RUN July 22, 2026 (0H+1M+2L); v0.2 fix pass
      applied.** M-1: the `Squad` constructor's F3 guards (null/empty/oversize) + the §2.2.4
      snapshot-copy invariant had no direct test (both callers pre-guard) — added `SquadTests.cs`
      + corrected the §5 F3 labels. L: FR-SQ-018 default-name (`FirstName "Player"` / `LastName =
      playerId`, was "Player N"); §9.4 prefix list gained the missing `PR`.
- [x] **AR-2 convergence sweep — RUN July 22, 2026 (0H+1M; L-only otherwise → CONVERGENCE).** M-1
      (fixed): §7.2 listed the GK #11 / Heading #10 projections as "deferred until those specs are
      engine-wired (KD-P8)", but `PlayerAttributeProjection.cs` v1.2 landed `ToGoalkeeper`/`ToHeading`
      the same day (July 22) with the opt-in #10/#11 engine integration — the deferral was stale;
      §7 gains a §7.3 recording the LANDED status (and completing FR-SQ-001's single-source guarantee
      for the GK/Heading structs). Sweep verified clean against source: `AttrIdx.cs` ordinals ↔
      Appendix B, `PlayerDatabaseConstants.cs` tags/values ↔ Appendix A, the `RosterGenerator` 36-draw
      sequence ↔ §3.2/Appendix D, and the `SquadTests.cs`/`RosterGeneratorTests.cs`/`SquadFileLoaderTests.cs`
      names ↔ §5. Cycle closes per the #21–#26 L-only-round convention.
- [x] PASS-2 not required (no High surfaced in any sweep).
- [x] **`DOMAIN_TAG_PLAYER_DATABASE = 0x1F` / `SubsystemOrdinals.PlayerDatabase = 81` cross-cite
      CONFIRMED in Deterministic Sim #16 §3.4 — spec-text row filed July 22, 2026
      (`deterministic-sim/section-3.md` §3.4, next after `DOMAIN_TAG_LIVING_WORLD = 0x1E`) + back-prop
      ERR-027-001 filed (`spec-error-log.md` v1.32). The living-world `0x1E`/`80` precedent (ERR-022-001)
      was itself a code-only allocation missing from the spec text; it was filed in the same pass so the
      cited precedent is now real.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 22, 2026 (§9.5).**

## 9.4 Consistency gates

- [x] FR prefix `FR-SQ-` verified unclaimed by grep over `docs/specs/**/*.md` (existing prefixes:
      AT, BU, CS, DA, DM, DS, EVT, GK, HE, LW, PA, PO, PR, RO, TI, TP, TS)
- [x] Candidate number #27 matches the design-supplement reservation
- [x] `SPEC_INDEX.md` row added (`IN REVIEW`)

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 22, 2026.** PASS-1 + AR-2 converged (0H, no unresolved M/H); the #16 §3.4
> cross-cite is filed; the layer is built, wired, and dotnet-gate-green.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — all sections (§1–§9 + appendices) present per the 9-section template | `outline.md`, `section-1..8`, `section-9-approval-checklist.md`, `appendices.md` | ☑ |
| R-02 | **Technical accuracy** — canonical record / `AttrIdx` map / draw sequence / position-bias / `WeakFoot` jitter / `SquadFileLoader` grammar internally consistent; 26 FRs; constants one tag each, no `[EST]` | §2/§3/Appendix A; verified against `src/player-database/*.cs` | ☑ |
| R-03 | **Cross-spec consistency** — `DOMAIN_TAG`/`SubsystemOrdinals` cross-cite confirmed in #16 §3.4 (`section-3.md` §3.4 `0x1F`/`81` row + ERR-027-001); `ERR-007` closure recorded; T-phase wiring docs cited (§7/§8) | §8.1 / §7.1 / #16 §3.4 | ☑ |
| R-04 | **Stage-binding correctness** — KD-6 club-setup-time (not per-tick / not zero-alloc); KD-7 boot-constant snapshot exclusion (T3 is a header id, not per-player values); KD-8 text import is not a wire format | §4 / §6 / §7 | ☑ |
| R-05 | **Approval granted** — PASS-1 + AR-2 resolved; `SPEC_INDEX.md` row flipped `IN REVIEW → APPROVED` | ☑ |

## 9.6 Decision

**APPROVED — July 22, 2026.** The layer is fully built, wired, and tested (§9.2); the section-file
PASS-1 (0H+1M+2L) + AR-2 convergence sweep (0H+1M, L-only otherwise) are resolved to convergence
(§9.3); the #16 §3.4 `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` / `SubsystemOrdinals.PlayerDatabase = 81`
cross-cite is filed (ERR-027-001, plus the retroactive ERR-022-001 living-world precedent); and
lead-developer R-01..R-05 sign-off is granted (§9.5). `SPEC_INDEX.md` row 27 flips
`IN REVIEW → APPROVED` (27 APPROVED / 0 IN REVIEW). Post-APPROVED, non-blocking: Stage-1+ on-disk
persistence / transfers / aging / a `PlayerPosition → RoleId` granular mapping (§7.2 deferrals).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial checklist. Implementation gates checked (code precedes the spec); PASS-1, #16 cross-cite confirmation, and R-01..R-05 sign-off OPEN. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | PASS-1 run (0H+1M+2L); §9.3 updated — PASS-1 checked, AR-2 sweep pending. §9.4 prefix list gained `PR`. |
| 0.3 | 2026-07-22 | — | AR-2 convergence sweep (0H+1M: §7.2 GK/Heading deferral stale → §7.3 LANDED; L-only otherwise → CONVERGENCE). #16 §3.4 cross-cite filed (`0x1F`/`81` row + ERR-027-001; retroactive ERR-022-001 living-world precedent). R-01..R-05 signed; §9.6 decision APPROVED. Status APPROVED. |
#endregion
