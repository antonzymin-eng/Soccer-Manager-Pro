# Club Infrastructure & Facilities #53 — Section 8: Cross-References & Back-Propagations

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 8.1 Typed cross-references

| ID | Target | Contract |
|---|---|---|
| XC-053-001 | #42 `youth-academy-intake` §2.2 `AcademyQuality` | #53 returns **#42's** type. `Neutral => default` (all-zero), so #53's baseline projection `(0, 0)` **is** the identity exactly (§3.4). Consumed, never re-declared. |
| XC-053-002 | #42 FR-YA-009 | `AcademyQuality` is caller-supplied and #42 references neither producer. #53 satisfies that contract from the other side; **#42 needs no shape change**. |
| XC-053-003 | #42 FR-YA-011 / `ACADEMY_CEILING_SHIFT_ABS_MAX` (`300‰`) | #42 **fails loud** on an out-of-bounds dial, so #53 clamps against **#42's** bound rather than a bound of its own — producing a value the consumer would reject is a producer bug (§3.4). |
| XC-053-004 | #42 `section-1.md` / `section-4.md` | The mis-attribution: *"#40 facility spend"* as an `AcademyQuality` input. Re-attributed to #53 (ERR-042-001) — a **pointer fix**, not a design change. |
| XC-053-005 | #41 `injuries-medical` §2.2 `MedicalModifier` | #53 returns **#41's** type via its `Identity` factory shape. `OccurrenceRiskMillMult` stays at `PERMILLE_ONE`: #53 supplies a **recovery** term only (§3.4). |
| XC-053-006 | #41 FR-MD-016 | `MedicalModifier.Identity` is an explicit factory and `default(MedicalModifier)` (all-zero, a ×0 recovery multiplier) **fails loud**. This is why an unmodelled club yields `Identity`, never `default` — the difference between honouring the two identity conventions and conflating them (KD-8). |
| XC-053-007 | #29 `training-system` FR-TR-004/005 | **#29 is the sole writer of `TrainingInput`.** #53 therefore supplies a root-assembled *input* to `ComputeTrainingInput` and **never returns a `TrainingInput`** (KD-9 / ERR-029-003). |
| XC-053-008 | #29 FR-TR-006 | `ComputeTrainingInput` is pure and deterministic. #53's term is a pure integer, preserving that property. |
| XC-053-009 | #34 `staff-backroom` §1 / §3 | The double-count rule — staff and facilities reach shared consumers by **separate seams**. The rule is unchanged and still correct; only the producer's identity was wrong (ERR-034-001). #53 never reads staff state (FR-IN-023). |
| XC-053-010 | #40 `club-finances-economy` `ApplyTransaction` | The upgrade debit runs through #40's **existing** transaction path, sequenced by the command layer. **No reference in either direction** (§4.3). |
| XC-053-011 | #40 §7.2 | The deferred Stage-3 matchday-attendance accrual. `StadiumCapacity` (§3.5) is the number it needs and which no spec currently holds (ERR-040-002). |
| XC-053-012 | #40 scope | #40's approved scope is **budgets, wage ledger, revenue, FFP** — a `grep` for `facilit` across `docs/specs/club-finances-economy/` returns nothing. #40 is not late; it was never the owner (§1.1). |
| XC-053-013 | #28 `player-progression-lifecycle` §1 / §7 | The academy structure *"(facilities → intake quality)"* is described without an owner. #53 is named as the producer feeding #42's dial (ERR-028-002); #28's own out-of-scope position is intact. |
| XC-053-014 | #30 `season-competition-loop` §3.3 `RunWorldTickInFixedOrder` | `AdvanceFacilityDay` enters the **pinned** day-advance order (ERR-030-020), before any same-day consumer of a facility-derived input. |
| XC-053-015 | #30 `SeasonSaveCodec` | #53's sub-blob is composed as an **opaque** length-prefixed block; the outer codec never parses it (KD-5). |
| XC-053-016 | #16 §3.4 | **No row.** #53 is draw-free (KD-6): no `DOMAIN_TAG_*`, no `SubsystemOrdinal`, and **no `_RESERVED_` placeholder either** — see §8.4. |
| XC-053-017 | #50 `save-migration-versioning` KD-2 | #53 is **outside** `WORLD_GENERATION_VERSION` because genesis is a uniform baseline and state is stored, not regenerated — a property that holds only while KD-2 does. #53 registers `FACILITY_SAVE_FORMAT_VERSION` in #50's version registry. |
| XC-053-018 | #19 §3.1.4 | Test-ID prefixes; the §5.10 closed-loop scenario registration under `SCENARIO_PATH_CROSS_SPEC_PREFIX`. |

## 8.2 Back-propagations

### At approval — land **atomically** with the status flip

| ID | Target | Change | Kind |
|---|---|---|---|
| **ERR-034-001** | `staff-backroom/section-1.md`, `section-3.md` | Re-attribute *"#40 facilities"* to **#53**. #40 owns the *funding*; #53 owns the *level*. The double-count rule itself is unchanged and still correct — only the producer's identity is wrong. (`ERR-034-*` unfiled and unproposed — verified.) | Doc-only pointer fix |
| **ERR-042-001** | `youth-academy-intake/section-1.md`, `section-4.md` | Re-attribute *"#40 facility spend"* to **#53**'s `YouthFacilities` projection. `AcademyQuality`'s shape, its `Neutral` identity, and the root-assembly pattern are all unchanged. (`ERR-042-*` unfiled — verified.) | Doc-only pointer fix |
| **ERR-028-002** | `player-progression-lifecycle/section-1.md`, `section-7.md` | Name **#53** as the facility producer behind the academy structure feeding #42's dial, keeping #28's own out-of-scope position intact. (`ERR-028-001` is filed; `-002` is next free — verified.) | Doc-only pointer fix |
| **ERR-040-002** | `club-finances-economy/section-1.md`, `section-7.md` | Record that **#53 owns facility state** and that #40's role is funding via the existing transaction path (KD-1), closing the gap where four specs point at #40 for a model its own scope excludes. Name #53's `Stadium` capacity as the input for §7.2's deferred matchday-attendance accrual. **No #40 code, constraint, ledger, or requirement change.** (`ERR-040-001` is filed; `-002` is next free — verified.) | Doc-only scope clarification |
| **ERR-029-003** | `training-system/section-2.md`, `section-3.md` | Record the #53 facility term as a **second root-assembled input** to `ComputeTrainingInput`, alongside #34's `CoachingModifier` — because FR-TR-005 makes #29 the sole writer of `TrainingInput` and a #53-returned `TrainingInput` would be the second path it forbids (KD-9). One additional input parameter at #29's Stage-3 tier; **no #29 logic change and no #28 type change**. (`ERR-029-001` filed + RESOLVED; `-002` soft-reserved by #34; `-003` next free — all verified.) | ◑ Spec-text-first: text at approval, the parameter at #29's Stage-3 tier |
| **ERR-030-020** | `season-competition-loop/section-3.md` §3.3 + `section-2.md` FR-SN-034 | Insert `AdvanceFacilityDay` into the **pinned** day-advance tick order, before any same-day consumer of a facility-derived input, renumbering the steps below it. **Filed at approval, not deferred**, because #30's tick order is a pinned sequence cited **by number** in several approved specs: #41's `AdvanceMedicalDay` and #45's board seam (`ERR-030-008`, which renumbered `AdvanceDay` 8 → 9) both landed as filed insertions. *A step whose position is decided later is a step whose ordering was never reviewed.* (Proposed `ERR-030-*` ids across the pre-promotion supplements reach `-019`; `-020` is #53's — verified.) | Doc-only re-pin |

**Why five doc-only pointer fixes and no design change anywhere.** Every consumer was built correctly —
value input, explicit neutral identity, assembled by the root, no assembly reference — so #53 fits the
seams that already exist. Four of the six back-props above change **who the producer is**, not what the
seam looks like. A spec that arrives to find its downstream seams already written should not invent
changes to prove it landed; the one exception, ERR-029-003, is a real (if small) surface addition and is
marked as such.

### Deferred — land at the named tier, **not** at approval

- **#41's recovery-modifier binding**, when #41's Stage-3 tier consumes a non-identity `MedicalModifier`.
- **#40's matchday accrual** consuming `StadiumCapacity`, at #40's T3 — including #40 calibrating its
  attendance model against `STADIUM_BASE_CAPACITY`, which is #40's calibration to do and not an
  assumption #53 may make on its behalf (§3.5).
- **The outer `SEASON_SAVE_FORMAT_VERSION` bump**, at **T2** when the sub-blob is first composed in.
- **A `ScoutingInfrastructure` roster member**, if and when #32 declares a dial for it (KD-2) — an
  APPEND-only addition, deliberately not declared in advance.
- **Reputation / player-attraction projections** for #54 and #31 — deep tier.

### Explicitly **not** back-props (recorded so their absence is not read as an omission)

- **#16 — nothing to file, not even a placeholder.** See §8.4; this is the one that most looks like an
  omission and is not.
- **#42 / #41 — no shape change.** `AcademyQuality` and `MedicalModifier` are consumed exactly as
  declared. Their `Neutral`/`Identity` factories, their bounds, and their root-assembly patterns are all
  unchanged; ERR-042-001 touches prose only.
- **#30's loop logic** — only the tick-order *slot* is added (ERR-030-020). #53 changes no existing step,
  no boundary roll, and no season state. Unlike #45, it bumps **no** `SEASON_STATE_FORMAT_VERSION`.
- **#34 — no seam change.** Both specs project independent terms into the same dials; the root combines
  (§4.5). ERR-034-001 corrects a name, not a mechanism.
- **#27 — nothing to change.** `ClubId` is consumed read-only and #27's schema is untouched.

## 8.3 References

#53 introduces **no external citation**. Its content is a state model composed from this project's own
approved specs; there is no published result it rests on, and inventing a citation to decorate the
section would be the fabrication the project's rules forbid. The §8.1 typed cross-references are the
authorities, and every one of them names a file and a section that can be checked.

## 8.4 Why #16 gets no row at all — not even a `_RESERVED_` placeholder

Worth stating explicitly, because the sibling specs' pattern makes its absence look like a missed step.

#29, #40, #31, #33, #32, #34 and #45 each carry a **`_RESERVED_`** row in #16 §3.4: a domain-tag slot
held but not promoted, because each of them *has* a deep tier with a real draw site that will eventually
need one. #16 §3.4's **A-04 rule** requires every *allocation gap* to carry an explicit placeholder — and
that is a rule about gaps in the **allocated sequence**, not about specs.

#53 is not in that sequence at all. It declares **no** stochastic surface at any tier described in this
spec (KD-6 / FR-IN-030), so there is no slot to reserve and no gap to mark. Filing a `_RESERVED_0x2E_`
row *for #53* would do two harmful things:

1. It would **consume the roadmap §6 slack** (`0x2E`–`0x2F` / 96–97) that is deliberately held back for a
   currently-read-only spec that later discovers it needs a draw — spending it on a spec that has
   committed to never needing one.
2. It would assert that #53 is *expected* to become stochastic, which FR-IN-031 explicitly makes a
   **future promotion decision on the record** rather than a foregone conclusion.

If that promotion ever happens, it takes `0x2E` / 96 **at that point**, with its own back-prop, at its
first real draw site — the `world.arcs` / `_RESERVED_0x21_` precedent applied in the one direction those
precedents do not themselves cover.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §8 (XC-053-001..018, the six approval-time back-props, the deferred set, the explicit not-a-back-prop list, and the no-external-citation rationale) from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the proposed **ERR-029-001** was **already filed and RESOLVED** (July 23, at #29's own approval) and **`-002` is soft-reserved by #34** — verified against `spec-error-log.md` v1.34 and `staff-backroom-design.md`; renumbered to **ERR-029-003**. Filing against a used id is the collision class the project's own numbering discipline exists to prevent, and it would have landed silently. **L:** added **§8.4** explaining why #16 gets *no* row at all — the sibling `_RESERVED_` pattern makes its absence look like a missed A-04 step, when filing one would both consume the roadmap's held-back slack and assert an expectation FR-IN-031 deliberately leaves open. **L:** the "not a back-prop" list gained #42/#41/#34 rows and the explicit contrast with #45's `SEASON_STATE_FORMAT_VERSION` bump. |
#endregion
