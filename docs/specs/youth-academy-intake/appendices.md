# Youth Academy & Intake #42 — Appendices

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants**
(§20 prohibits empty regions) — #42 has no `[DERIVED]` and no `[EST]` constants, so neither region
appears. `[GT]` values are **illustrative pending the balance pass** (§A.3) — the spec's contract is
their *shape and identity behaviour*, not their magnitude (the #21 G2 / #22 / #34 precedent).

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `ACADEMY_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The sub-blob's own version gate (KD-6). Independent of `SEASON_SAVE_FORMAT_VERSION` — bumping one never implies the other. |
| `DRAW_PURPOSE_INTAKE` | `0` | `[FIXED]` | The §3.2 purpose ordinal. **APPEND-only** — reordering changes every anchor and breaks replay parity. |
| `DRAW_PURPOSE_RADIX` | `16` | `[FIXED]` | The §3.2 fixed radix. **Fixed, never "the current purpose count"** — a growing radix breaks cross-version replay parity the moment a purpose is appended (#41's own AR-2 finding, adopted here). |
| `ACADEMY_CLUB_STRIDE` | `65536` | `[FIXED]` | The §3.2 club stride; bounds the club-id space the anchor keeps injective. |
| `YOUTH_INTAKE_STREAM_SITE_ID` | `"youth.intake"` | `[FIXED]` | The `RegisterStream` site id (FR-YA-018). |
| `YOUTH_INTAKE_STREAM_VERSION` | `1` | `[FIXED]` | Bumping it re-keys the stream and changes every future cohort — a deliberate, digest-visible act. |

### A.2 Cross (consumed read-only; never re-declared)

| Constant | Authority | Notes |
|---|---|---|
| `PA_MIN` (4000), `ABILITY_MAX` (10000), `REGEN_PA_HEADROOM` (1000) | #28 `PlayerProgressionConstants` | The §3.3.1 clamp bounds. |
| `REGEN_AGE_MIN` (16), `REGEN_AGE_MAX` (20) | #28 | The minimal intake band (FR-YA-008). |
| `PROGRESSION_REGEN_FIELDS` | #28 | The per-prospect draw budget (FR-YA-002). |
| `DAYS_PER_YEAR` (365) | #28 | The `BirthWorldDay` formula + the default intake period. |
| `CLUB_SQUAD_SIZE` (25) | #27 `PlayerDatabaseConstants` | The promotion bound (FR-YA-025). |
| `DOMAIN_TAG_YOUTH_ACADEMY` (`0x2B`), `SubsystemOrdinals.YouthAcademy` (93) | #16 §3.4 | `[CROSS-PENDING]` until the T2 promotion (§8.2). |

### A.3 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `ACADEMY_INTAKE_PERIOD_DAYS` | `= DAYS_PER_YEAR` (365) | The KD-4 trigger period. |
| `ACADEMY_AGE_MIN` / `ACADEMY_AGE_MAX` | `= REGEN_AGE_MIN` / `REGEN_AGE_MAX` (16 / 20) | At these values `ReanchorAge` is a no-op (FR-YA-008). |
| `ACADEMY_INTAKE_COHORT_SIZE` | `6` | Prospects per intake at neutral quality. |
| `ACADEMY_COHORT_SIZE_MIN` / `_MAX` | `1` / `12` | The §3.3 / F2 bound on the composed `ACADEMY_INTAKE_COHORT_SIZE + CohortSizeDelta`; the *dial* itself is bounded by FR-YA-011. |
| `ACADEMY_CEILING_SHIFT_ABS_MAX` | `300` (‰) | The FR-YA-011 bound on `CeilingShiftPerMille`; ±30% of PA at the extremes. |
| `ACADEMY_COHORT_CAPACITY` | `24` | Academy-roster capacity; a full roster refuses further intake (F5-class). |

**Tagging note (why there is no `[DERIVED]` region).** `ACADEMY_INTAKE_PERIOD_DAYS` and
`ACADEMY_AGE_MIN/MAX` each *default* to a #28 value, which invites a `[DERIVED]` tag — but a
`[DERIVED]` constant is one a designer must **never** set independently, and all three are exactly the
dials §7.2 deepens. They are therefore `[GT]` whose **default happens to equal** a `[CROSS]` value.
Tagging them `[DERIVED]` would have been a double tag (`[DERIVED]` + "`[GT]`-overridable"), which Spec
#20's one-tag-per-constant rule forbids.

**Consequence, stated plainly:** the minimal identity (FR-YA-008 / T-YA-ID-004) holds **at the default
values**. A config that moves `ACADEMY_AGE_*` off the #28 band deliberately leaves the identity tier —
that is the intended deep-tier behaviour, not a violation, and §5's identity tests run at defaults.

**Balance-pass note.** The `[GT]` magnitudes above are pinned only for shape: cohort size positive and
bounded, the ceiling shift symmetric about zero with the identity **exactly** at zero, and capacity above
cohort size. §5 asserts identity and direction, never magnitude. The numerical balance pass lands at T3
with real career data — the #21 G2 / #22 / #26 §9.2 precedent.

## Appendix B — Save sub-blob layout (KD-6)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** —
the outer codec sees a length-prefixed byte block and never parses it (FR-YA-028).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `ACADEMY_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below is interpreted (F3). |
| 2 | `ClubId` | `i32` | |
| 3 | `HasIntaken` | `u8` | The FR-YA-015 genesis sentinel; any value other than 0/1 fails loud. |
| 4 | `LastIntakeWorldDay` | `u32` | The KD-4 latch. |
| 5 | `NextYouthPlayerId` | `i32` | The FR-YA-027 monotonic high-water. |
| 6 | `LastAppliedQuality.CeilingShiftPerMille` | `i32` | Provenance only — **never re-applied on load** (re-applying would compound the shift on every restore). |
| 7 | `LastAppliedQuality.CohortSizeDelta` | `i32` | Provenance only. |
| 8 | `CohortCount` | `i32` | Length prefix — read through the overflow-safe `Require(offset, need, total)` bound compared against `total − offset` (F3; the `MatchSaveCodec` hardening). |
| 9 | per prospect × `CohortCount` | — | `PlayerRecord` (#27 canonical order) then `PlayerLifecycle` (#28 canonical order) then `IntakeWorldDay` (`u32`), `ContractState` (`i32`). |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F3). |

**Deliberately absent: any `RngStreamState` / cursor** (FR-YA-020). KD-7's per-intake anchor makes the
next cohort a pure function of `(worldSeed, clubId, intakeWorldDay)`, every part of which is already in
the blob or the world. A future maintainer tempted to add a cursor here should first read §3.2 and
T-YA-DET-002.

**APPEND-only.** New fields go at the end with a `ACADEMY_SAVE_FORMAT_VERSION` bump; inserting in the
middle shifts every subsequent offset.

## Appendix C — Worked cohort example (the identity)

The §3.5 arithmetic examples are exact and hand-verifiable. A full *cohort* example is deliberately **not
tabulated**: the attribute values are the output of #28's SipHash-keyed draw sequence and are not
hand-computable, so any table here would be fabricated. The cohort is instead pinned relationally —
identity against a direct `RegenGenerator` call (T-YA-ID-001), two-run equality (T-YA-DET-001), and
position-independence (T-YA-DET-002) — which are mechanically checkable without knowing a single drawn
number. See §5's preamble for the full rationale.

At T2 the closed-loop scenario (§5.8) produces a real cohort on the pinned host; if a golden cohort is
wanted for regression purposes it is captured **then**, from a real run, and recorded as evidence — never
authored here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial appendices (A.1–A.4 constant catalogue with the fixed-radix and APPEND-only notes, B save-block layout with the deliberately-absent-cursor note, C the no-fabricated-cohort rationale). Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | PASS-1 fixes (M+M): (a) `ACADEMY_INTAKE_PERIOD_DAYS` / `ACADEMY_AGE_MIN`/`_MAX` were double-tagged `[DERIVED]` + "`[GT]`-overridable", violating Spec #20's one-tag rule — retagged `[GT]` (they are dials §7.2 deepens; a `[DERIVED]` constant is one a designer must never set) with an explicit note that the minimal identity holds *at the defaults*. A.3 is now empty rather than mis-tagged. (b) A.4's cohort-size row cited FR-YA-008 (the age band); corrected to the §3.3/F2 composed bound, with the dial bound attributed to FR-YA-011. |
#endregion
