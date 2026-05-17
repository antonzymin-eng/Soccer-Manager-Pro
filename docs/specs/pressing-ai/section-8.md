# Pressing AI Specification #13 — Section 8: References and Citations

**Created:** May 17, 2026
**Last Updated:** May 17, 2026
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

## 8.1 Cross-Spec References

All references are grep-verifiable against the cited section files
in `docs/specs/`. Each row identifies what #13 reads from the
cited section.

| Spec | Section(s) | What #13 binds | Typed XC |
|---|---|---|---|
| #1 Ball Physics | §1.2 | Coordinate system: corner origin, X = 0–105 m, Y = 0–68 m | `XC-013-001` |
| #2 Agent Movement | §2.5 (`XC-002-001`) | EntityId no-reuse cross-spec constraint | `XC-013-002` |
| #2 Agent Movement | §3.1 | Hysteresis pattern (dwell-time + dead-zone) — KD-9 | `XC-013-006` |
| #4 First Touch | §3.1 (`q ∈ [0,1]`) | `BAD_TOUCH` quality scalar surface | `XC-013-007` |
| #4 First Touch | §3.5 (`pressureScalar`) | Per-touch local pressure — propagated via #7 §3.10 per Q2 note in §2.3 | `XC-013-008` |
| #5 Pass Mechanics | §2 FR-08 (`PassAttemptEvent` published at `CONTACT`) | `BACKWARD_PASS` event surface; #13 owns the directional threshold | `XC-013-009` |
| #7 Perception System | §3.7 | Per-agent perception snapshot (positions, ball, possession owner) | `XC-013-010` |
| #7 Perception System | §3.9 | Possession state | `XC-013-011` |
| #7 Perception System | §3.10 | `isActive`, attribute lookup, `lastTouch.q` propagation, `perceivedPressure` | `XC-013-012` |
| #8 Decision Tree | §1.4.21 | Stage-1 deferral text anchoring KD-12 | `XC-013-013` |
| #8 Decision Tree | §1.5 | Stage-1 interaction row | `XC-013-014` |
| #8 Decision Tree | §1.7.3 (`XC-008-001`) | EntityId no-reuse | (covered by `XC-013-002`) |
| #8 Decision Tree | §2.2.6 | `TacticalContext` schema — amendment target for OI-001 Option B | `XC-013-015` |
| #8 Decision Tree | §3.1.8, §3.1.8.1, §3.1.8.2 | PRESS utility surface; `PRESS_STAMINA_MINIMUM`; `PRESS_TRIGGER_DISTANCE` | `XC-013-004`, `XC-013-005`, `XC-013-016` |
| #8 Decision Tree | §3.2.7 | PRESS utility scoring | `XC-013-017` |
| #11 Goalkeeper Mechanics | (KD-13 negative invariant only) | GK excluded from press roles | `XC-013-018` |
| #12 Positioning AI | §3.0 | Local phase enum — KD-11 phase gating | `XC-013-019` |
| #12 Positioning AI | §3.6.1 (`SPACING_EPSILON_M2`) | Float-comparison epsilon — KD-9 / KD-14 reuse | `XC-013-003` |
| #12 Positioning AI | §3.7, §4.4.3 | Baseline `formationSlot[]` accessor; `IsSentinel` surface | `XC-013-020` |
| #12 Positioning AI | §7.3 | `PressOverride` displacement-layer reservation slot | `XC-013-021` |
| #16 Deterministic Simulation | §3.2 | Authoritative simulation state definition | `XC-013-022` |
| #16 Deterministic Simulation | §3.2.5 | EntityId-sorted iteration order | `XC-013-023` |
| #16 Deterministic Simulation | §3.4 | Domain-tag registry — `ERR-013-005` allocation target | `XC-013-024` |
| #16 Deterministic Simulation | §5 | Determinism regression scenarios — §5.4 binding | `XC-013-025` |
| #16 Deterministic Simulation | §6.2 | Per-tick digest scope for tactical-AI outputs | `XC-013-026` |
| #17 Event System | §3.10 (schema only at Stage 0) | `PRESS_TRIGGERED` / `PRESS_DISENGAGED` registration target | `XC-013-027` |
| #18 Performance | §3.7 (zero-alloc), §3.7.5 (`[HotPathAllocExempt]`), Appendix F.0 (channel-registry schema) | Hot-path discipline; Stage 1 channel rows | `XC-013-028` |
| #18 Performance | §6 | Per-tick budget framework | `XC-013-029` |
| #19 Testing | §3 | Test taxonomy — §5.1 binding | `XC-013-030` |
| #19 Testing | §4 | FR-traceability framework — §5 binding | `XC-013-031` |
| #20 Code Standards | §4.2 (FR-CS-025) | Single constant catalogue file | `XC-013-032` |

## 8.2 CLAUDE.md Invariants Bound

| Invariant | Where bound in #13 |
|---|---|
| Corner-origin coordinate system | §1.7, §3.1.3 |
| Fatigue convention `0 = rested, 1 = fatigued` | §1.7, §3.7, FR-PR-008 |
| 10 Hz tactical / 60 Hz physics tick split | §1.7, §4.1, FR-PR-001 |
| Parameter-based physics (no type enums) | §1.7 — #13 outputs structs with `EntityId?` + `Vector2`, not enum-tagged actions |
| Constant-tag policy | KD-14, §6.1, FR-PR-041 |
| Interface Design Principle | KD-5 / KD-6 / KD-12, §7 — declared bindings only against unspecified #14 / #15 |
| Stage 0 uses `float` (Fixed64 deferred to Stage 5+) | §4.6, §7.9 |
| State-snapshot determinism | §4.6 |
| EntityId no-reuse | §1.7 (via `XC-013-002`) |
| Spec Renumbering Cascades hazard | §8.3 — cross-refs verified by grep |

## 8.3 Typed Cross-Reference IDs

`XC-013-NNN` IDs allocated at section-file draft (this draft) —
sequential allocation as cited above. The full table appears in
§8.1; this subsection records the allocation policy.

- Numbering is sequential per the first body-text citation order.
- Reciprocal slots into other specs are NOT pre-allocated (those
  ride on `ERR-013-NNN` patches and are assigned by the
  consuming spec at amendment time).
- The `XC-013-NNN` block is reserved through `XC-013-099`;
  Stage 1+ extensions may extend it.

## 8.4 Spec Error Log Entries Filed

| ERR | Subject | Status |
|---|---|---|
| `ERR-013-001` | Back-prop into #8 §3.1.8.2 (or §2.2.6) — read of #13's `PressAssignment`; mechanism deferred (OI-001 / KD-3) | Open — filed at section-file draft (May 17, 2026); gates §9 sign-off |
| `ERR-013-002` | `PRESS_TRIGGERED` channel registration in #17 §3.10 | Open (Stage 1) — non-blocking for Stage 0 spec text |
| `ERR-013-003` | `PRESS_DISENGAGED` channel registration in #17 §3.10 | Open (Stage 1) — non-blocking for Stage 0 spec text |
| `ERR-013-004` | Stale "Fatigue System #13" reference at `decision-tree/section-3-1.md` L753; current #13 is Pressing AI | Open — verified present at section-file draft; one-token patch |
| `ERR-013-005` | `DOMAIN_TAG_PRESSING_AI = 0x19` allocation in #16 §3.4 (inherits ERR-012-001 Phase B/C block) | Open — gates `[CROSS-PENDING] → [CROSS]` promotion in §6.1 |

## 8.5 Citation Verification

Per CLAUDE.md "Things That Have Gone Wrong Before" — Stale Spec
Numbers — every cross-spec reference in this document was
grep-checked at draft time against the cited spec's current
section files. Verification commands (to be re-run before
§9 sign-off):

```
grep -rn "TacticalContext"     docs/specs/decision-tree/
grep -rn "PRESS_STAMINA_MINIMUM\|PRESS_TRIGGER_DISTANCE" docs/specs/decision-tree/
grep -rn "DOMAIN_TAG_"         docs/specs/deterministic-sim/
grep -rn "FR-CS-025"           docs/specs/code-standards/
grep -rn "PassAttemptEvent"    docs/specs/pass-mechanics/
grep -rn "pressureScalar\|q "  docs/specs/first-touch/
grep -rn "Phase\|formationSlot" docs/specs/positioning-ai/
grep -rn "Fatigue System"      docs/specs/decision-tree/   # ERR-013-004 confirmation
```

Section-file draft greps performed May 17, 2026:

- `PassAttemptEvent` — verified at `pass-mechanics/section-1.md`
  L274 and `pass-mechanics/section-2.md` L330 (FR-08 publish-at-
  `CONTACT`). No dedicated "directional" or "backward" event;
  #13 dots the kick-velocity payload locally.
- `pressureScalar` — computed in
  `first-touch/section-3-1-to-3-5.md` §3.5 (lines 594+); fed into
  the `q` computation at §3.1 line 71. #7 propagation route
  verified for the per-tick perception field; Q2 default
  (perception-propagated) preserved per outline KD-7.
- `Fatigue System #13` — present at
  `decision-tree/section-3-1.md` L753 → `ERR-013-004` filed.

## 8.6 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. `XC-013-001`..`XC-013-032` allocated. `ERR-013-001`..`ERR-013-005` filed in `spec-error-log.md`. |
