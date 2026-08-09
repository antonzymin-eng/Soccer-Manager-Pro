# Player Progression & Lifecycle #28 — Section 9: Approval Checklist

**Created:** July 23, 2026
**Last Updated:** August 9, 2026 (v0.3 — §9.2 implementation status corrected: T0/T1/T2a landed, verified
against `src/`; season-boundary roster mutation deliberately deferred)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/player-progression-lifecycle-design.md` v0.3

---

Checklist entries are verified against real source; nothing is checked without a programmatically
verifiable anchor (CLAUDE.md "Never fabricate verification values"). This is a **forward-design** spec
— implementation gates are open by construction (nothing is built yet); review gates track the pipeline.

## 9.1 Content gates

- [x] Every Appendix A constant carries exactly one source tag (`[FIXED]`/`[DERIVED]`/`[CROSS]`/`[GT]`);
      no `[EST]`.
- [x] Every §3 algorithm has rules + a worked example (Appendix B growth-across-a-save; Appendix C retirement/regen;
      §3.1 pseudocode).
- [x] KD scope stated: lifecycle on the world tick; #29 training / #42 youth structure / #31 valuations
      deferred to their specs (§1.2 / §7.2).
- [x] KD-1 byte-exact integer projection + KD-8 behaviour-neutral identity stated with the save-round-trip
      consequence (§1.5 / §3.1 / Appendix B).
- [x] KD-4 one-way reference direction stated (#30 depends on #28, never the reverse; #28 references
      only #27 + #16) (§1.4 / §4.1).

## 9.2 Implementation status (T0/T1/T2a landed; season boundary deliberately deferred)

- [x] FR set complete + stable: FR-PG-001..024 (grep-verified in §2).
- [x] `TacticalDirector.PlayerProgression` assembly (value types + `GrowthProjection` §4.3 identity +
      `RegenGenerator`) — **LANDED (T0)**. Verified: `src/player-progression/{AbilityModel,
      GrowthProjection, PlayerLifecycle, PlayerProgressionConstants, RegenGenerator, TrainingInput}.cs`
      exist, and `src/player-progression/tests/` compiles and passes under the `dotnet-ci` gate.
- [x] `ProgressionSaveCodec` + season-save composition (a #30 change) — **LANDED (T1, August 8, 2026)**.
      Verified: `src/player-progression/ProgressionSaveCodec.cs` (magic-led per ERR-028-004, since the
      forward design's §3.5 had specified version-first with the RNG domain tag as identifier) and
      `ClubCareerStates.cs` exist; `ProgressionEngine.Snapshot`/`Restore` round-trip through the codec.
- [x] `AdvanceDay` wired at #30's reserved seam — **LANDED (T2a, August 8, 2026)**. Verified:
      `ProgressionEngine.AdvanceDay` is the FR-PG-021 batch entry point; `src/season-save/SeasonLoop.cs`
      calls `_progression.AdvanceDay(day, in growth)` at KD-2 slot 1 (ERR-029-006 closed). Retirement
      FLAGGING (§3.4) is live as part of this draw-free daily step.
- [ ] `RunSeasonBoundary` (retiree removal + 1:1 regen, §3.4) — **NOT STARTED, DELIBERATELY DEFERRED.**
      The roster mutation needs the `player-progression.regen` stream, which this landing does not
      register (no draw site exists yet — new-game `PotentialAbility` is seeded deterministically, not
      drawn). Retirement is FLAGGED by `AdvanceDay` above; the roster is not yet actually shrunk or
      regenerated.
- [ ] Deep CA/PA curve dial + #29 training-input consumption — NOT STARTED (T3).

## 9.3 Review gates

- [x] **PASS-1 adversarial review of the section files — RUN July 23, 2026 (results in §9.3.1); all fixed.**
- [x] **AR-2 → AR-3 convergence sweep — RUN July 23, 2026 (results in §9.3.1); CONVERGENCE.**
- [x] **Lead-developer R-01..R-05 sign-off — GRANTED July 23, 2026 (§9.5).**

### 9.3.1 PASS-1 / AR-2 record

**PASS-1 — July 23, 2026 (0H+2M); all fixed.**
- **M-1 (age-model muddle — the ability-model-muddle class again):** §3.1/§3.1.1/§2.2 carried *two*
  age representations (an `Age0`/`BirthWorldDay` derivation **and** an `AgeAnchorDay` + discrete
  "year rollover" `while`-loop) where the rollover step double-described what the `GrowthCursor`
  already does, and left #27's `PlayerRecord.Age` a stale seed (a footgun — a consumer reading
  `record.Age` would get age-at-new-game). Resolved to ONE model: serialize `BirthWorldDay` as the sole
  authoritative anchor, age is derived (`(worldDay − BirthWorldDay)/DAYS_PER_YEAR`), `record.Age` is
  kept current as a derived cache (the CA-cache pattern), and there is **no** discrete rollover step —
  attribute change is the cursor alone, so nothing can be double-counted (§1.5/§2.2/§3.1/§3.1.1/
  FR-PG-005/App. B).
- **M-2 (determinism identifier contradicted the cited #27 pattern):** FR-PG-020 said "exactly one RNG
  stream (… `entityId = clubId`)" — but a stream keyed `entityId = clubId` is **one per club** (the
  #27 `RosterGenerator.RegisterStream(entityId: clubId)` pattern it cites). Fixed: a
  `player-progression.regen` stream **per club**, registered at the first regen for that club
  (§2/FR-PG-020/§4.3).

**AR-2 → AR-3 — July 23, 2026 (0H+3M cross-fix regressions from the M-1 edits, all fixed → AR-3
clean, CONVERGENCE).** The M-1 fix left §5 T-PG-DET-001 ("… `GrowthCursor` + `AgeAnchorDay`", a save on
"the rollover day") and T-PG-DET-002 ("whole-year rollovers batch … the `while` in §3.1") and §6.2
("one rollover check (a bounded `while` … `⌈gap/DAYS_PER_YEAR⌉`)") still describing the removed field +
loop — a live FR↔test↔perf inconsistency; all realigned to the derived-age model (`BirthWorldDay`, a
single gap-independent division). AR-3 (grep + cross-ref sweep of §1/§2/§3/§5/§6/App. A/B/C): every
`rollover` mention is now a "no rollover" statement; the age model, the per-club stream, and the CA/PA
model are one representation each end to end — no new High/Medium. An L-only/clean round closes the
cycle (the #21–#30 convention).

## 9.4 Consistency gates

- [x] FR prefix `FR-PG-` verified unclaimed by grep over `docs/specs/**` (0 hits before this spec).
- [x] Candidate number #28 matches the roadmap; `_RESERVED_0x20_` / `SubsystemOrdinals` 82 are held for
      #28 in `deterministic-sim/section-3.md` and are promoted at approval (ERR-028-001).
- [x] Cited source APIs verified against real files: `PlayerRecord` (club-scoped `PlayerId`, `Age`,
      `Position`, `Attributes`), `PlayerAttributes` (31 `int[1,20]` + `WeakFootRating [1,5]`,
      `ToArray`/`FromArray`), `RosterGenerator.Generate` (`FIELDS_PER_PLAYER` fixed reservation,
      Reserve/DrawReserved/Close), `PlayerDatabaseConstants` (`CLUB_SQUAD_SIZE`, position-bias table,
      `AgeMin`/`AgeMax`), `WorldStore.AdvanceDay`/`Snapshot`/`Restore`, `SeasonSaveCodec` opaque-sub-blob,
      `CanonicalSerializer`, `DeterministicRngService`.
- [x] `SPEC_INDEX.md` row added at promotion (`IN REVIEW`), flipped `IN REVIEW → APPROVED` at sign-off.
- [x] #16 §3.4 cross-cite is a **promotion** of the reserved `0x20`/82 rows (ERR-028-001), filed at
      approval — the code const + stream registration land at T2 (the first draw site); no
      `DETERMINISM_DIGEST_VERSION` bump (namespace allocation only).

## 9.5 Lead-developer review gates (R-01..R-05)

> **Status: SIGNED — July 23, 2026.** PASS-1 → AR-2 → AR-3 converged (§9.3.1, 0H unresolved). This is a
> forward design (nothing built) — sign-off approves the DESIGN, exactly as #21–#30 were approved before
> their T0 code; the §7 roadmap is the post-APPROVED sequence.

| # | Review gate | Evidence to confirm | Status |
|---|---|---|---|
| R-01 | **Content completeness** — §1–§9 + appendices per the template | all files | ☑ |
| R-02 | **Technical accuracy** — the KD-1 integer-projection / CA-PA / regen / retirement / codec contracts internally consistent; 24 FRs; ONE ability model + ONE age model (no CA-vs-cursor-vs-attributes and no anchor-vs-derivation muddle — both resolved at PASS-1); constants one tag each, no `[EST]`; cited #27/#16/#30 APIs verified | §2/§3/§4/App. A/B/C | ☑ |
| R-03 | **Cross-spec consistency** — the #16 `0x20`/82 promotion (not a new allocation); the no-reverse-reference invariant (#28 references #27 + #16 only); the KD-2 method-parameter seam (no phantom #29 interface); the per-club regen stream (the #27 pattern); #27's struct stays schema-untouched | §4 / §7 | ☑ |
| R-04 | **Stage-binding correctness** — world-tick off-pitch cadence (not the match loops); byte-exact save/restore; retirement-flagged-not-removed-mid-fixture; the `[GT]` magnitudes honestly illustrative | §1 / §3 / §6 | ☑ |
| R-05 | **Approval granted** — PASS-1 + AR-2 + AR-3 resolved; `SPEC_INDEX.md` flipped `IN REVIEW → APPROVED` | | ☑ |

## 9.6 Decision

**APPROVED — July 23, 2026.** The section files are authored from the converged design supplement
(v0.3, AR-1 2M+2L → AR-2 3M → AR-3 clean); the section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3
convergence is resolved (§9.3.1); the #16 §3.4 cross-cite is a **promotion** of the reserved `0x20`/82
rows (ERR-028-001, filed at approval — code const + stream registration at T2, no
`DETERMINISM_DIGEST_VERSION` bump); and lead-developer R-01..R-05 sign-off is granted (§9.5).
`SPEC_INDEX.md` row 28 flips `IN REVIEW → APPROVED`. This approves the **forward design** (the #21–#30
pre-T0 precedent); the §7 plan (T0 value types + `GrowthProjection` §4.3 identity + `RegenGenerator` →
T1 the `PROGRESSION_SAVE_FORMAT_VERSION` block + season-save composition → T2 the `AdvanceDay` /
`RunSeasonBoundary` steps at #30's reserved seams → T3 the deep CA/PA curve dial + #29 training-input
consumption) is the post-APPROVED sequence.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial checklist. Content/consistency gates checked; review + implementation gates OPEN by construction (forward design). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence recorded (§9.3.1); R-01..R-05 signed; §9.6 APPROVED. Status APPROVED. |
| 0.3 | 2026-08-09 | — | §9.2 corrected: this section had stayed "forward design — nothing built yet" since APPROVED, but T0 (July 24, 2026) and T1/T2a (August 8, 2026) have since landed in `src/player-progression/` (verified: `ProgressionEngine`, `ProgressionSaveCodec`, `ClubCareerStates` exist; `AdvanceDay` is called from `src/season-save/SeasonLoop.cs` at KD-2 slot 1, closing ERR-029-006). `RunSeasonBoundary` and T3 remain unchecked — the season-boundary roster mutation (retiree removal + 1:1 regen) is deliberately deferred pending the `player-progression.regen` stream, not asserted as done. No other section touched. |
#endregion
