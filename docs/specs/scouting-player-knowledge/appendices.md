# Scouting & Player Knowledge #32 — Appendices

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — section-file AR PASS-2; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `SCOUTING_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the scouting sub-blob's own version gate (KD-6). |
| `SCOUT_QUALITY_NEUTRAL_PERMILLE` | `[FIXED]` | 1000 | the neutral scout-quality baseline (`×1.0`) — **closes #34 §3.1's "a baseline #32 will define"**; value-compatible with #34's neutral `FacetPermille` row (FR-SC-024). The identity is load-bearing (behaviour-neutrality), not a tunable. |
| `KNOWLEDGE_BAND_MAX` | `[GT]` | 4 (illustrative) | the maximal knowledge band; the terminal band's half-width is `0` by FR-SC-004. Balance-pass-pinned; changing it after ship re-keys derived estimates for in-flight careers (a tuning-migration concern recorded here). |
| `KNOWLEDGE_BAND_HALFWIDTH[]` | `[GT]` | {6, 4, 2, 1, 0} (illustrative) | per-band error half-width in attribute points; **shape pinned** (strictly decreasing, terminal 0 — FR-SC-005), magnitudes balance-pass-pinned (#21 G2). |
| `DAYS_PER_BAND_BASE` | `[GT]` | 14 (illustrative) | base days per band-up at neutral scout quality (§3.4); balance-pass-pinned. |
| `MAX_ACTIVE_ASSIGNMENTS` | `[FIXED]` | 1 | 1:1 with the single #34 ChiefScout role slot at Stage 3; a deep #34 staff pool widens this (append-only semantics — a multi-lane extension, §7.2). |
| `SCOUT_ATTR_RADIX` | `[FIXED]` | 32 | fixed ordinal radix `> ATTRIBUTE_COUNT = 31` (§3.3); never derived from the live count (append-parity). |
| `SCOUT_PURPOSE_RADIX` | `[FIXED]` | 16 | fixed ordinal radix for `ScoutDrawPurpose` (APPEND-only members; `Center = 0`). |
| `SCOUT_BAND_RADIX` | `[FIXED]` | 16 | fixed ordinal bound above any tuned `KNOWLEDGE_BAND_MAX`; guards the top digit (§3.3). |
| `POSITION_RELEVANT_ATTRS[position]` | `[GT]` | illustrative | *(deep)* the per-position attribute sets `RankByEstimate` sums (§3.5); balance-pass-pinned. |
| `ATTRIBUTE_MIN` / `ATTRIBUTE_MAX` / `ATTRIBUTE_COUNT` / `AttrIdx` | `[CROSS]` | #27 = 1 / 20 / 31 / ordinal map | the truth schema the view masks (FR-SQ-002/004/006). |
| `CLUB_SQUAD_SIZE` | `[CROSS]` | #27 = 25 | the own-squad check divisor (`PlayerId / CLUB_SQUAD_SIZE`, §3.1). |
| `_RESERVED_0x24_` / `SubsystemOrdinals.Scouting = 86` | `[CROSS]` | #16 | reserved for #32; stays RESERVED at approval (draw-free minimal, KD-3); promotes at the deep first accuracy draw. |

**Tag note:** the `[GT]` magnitudes are **illustrative pending a Stage-3 balance pass** — the
reviewed contract is the shapes (strictly-decreasing widths with terminal 0, floor-clamped cadence,
monotone speed-in-quality), not the numbers (the #21 G2 / #40 / #41 / #34 precedent).
`SCOUT_QUALITY_NEUTRAL_PERMILLE` and the radices are `[FIXED]` because identity and key-parity are
load-bearing contracts, not tunables.

## Appendix B — Scouting sub-blob layout (KD-6)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block (mirrors
the `SeasonSaveCodec` / `MedicalSaveCodec` / `StaffSaveCodec` posture; every length-prefixed read
preceded by an overflow-safe `Require(offset, need, total)` bound against `total − offset`):

| Field | Type | Notes |
|---|---|---|
| version | u32 | `SCOUTING_SAVE_FORMAT_VERSION`; **gate first** (F3) |
| managedClubId | i32 | the manager whose overlay this block holds (managed-manager scope, FR-SC-025; the `StaffSaveCodec` precedent) — enables the codec-level own-squad assignment check below |
| entryCount | u32 | `Require`-bounded count of overlay entries (0 at minimal — fog off stores nothing) |
| per entry: PlayerId | i32 | **strictly ascending across entries** (canonical order, FR-SC-017; F4 on violation); **not own-squad** (`id / CLUB_SQUAD_SIZE != managedClubId`, F4 — the hygiene rule drops an entry on a buy, so an own-squad entry is incoherent state) |
| per entry: KnowledgeBand | i32 | `[1, KNOWLEDGE_BAND_MAX]` at rest (band-0 players are simply unstored; F4 on out-of-range) |
| per entry: LastReportWorldDay | u32 | the report stamp (KD-1) |
| hasActiveAssignment | u8 | 0/1 (deep; 0 at minimal) |
| if 1: assignment PlayerId | i32 | codec checks **not own-squad** (`id / CLUB_SQUAD_SIZE != managedClubId`, F4); pool **resolvability** is a post-load composition-root validation (the codec has no player pool) |
| if 1: assignment DaysIntoBand | u32 | the mid-band cursor (T-SC-DET-001) |
| (trailing-byte guard) | — | `if (o != len) throw` (F3) |

**No `RngCursor`/`actionOrdinal` field** — draws are keyed and stateless (FR-SC-014). No estimate,
range, or per-attribute field appears anywhere in the block (FR-SC-006 — derived on read). Deep
extensions (multi-lane assignments, coverage overlays) **append** behind the version bump; the
layout above is never reordered.

## Appendix C — Worked estimate example (end to end)

External player `PlayerId = 183` (club 7, local 8), truth `Finishing = 14`. Managed club is 3
(`183 / 25 = 7 ≠ 3` — not own-squad). Illustrative tables: `HALFWIDTH = {6, 4, 2, 1, 0}`,
`DAYS_PER_BAND_BASE = 14`, scout quality `1250`.

1. **Fog off (minimal):** `ResolveBand → BAND_MAX = 4`, `w = 0` ⇒ `[14, 14]`, no draw. Identity.
2. **Fog on, unscouted:** band 0, `w = 6`; keyed draw for `(183, band 0, Finishing, Center)` gives
   say `offset = +3` ⇒ `center = 17` ⇒ `[max(1, 11), min(20, 23)] = [11, 20]` — contains 14 ✓.
   Repeated views, any day, any save/restore: identical.
3. **`AssignScout(183)`** — 11 days per band (`max(1, 14·1000/1250) = 11`). After 11 ticks of
   `AdvanceScoutingDay`: band 1 (`w = 4`), new keyed draw (band 1 in the key) say `offset = −2` ⇒
   `[8, 16]`; report stamped `(183, band 1, day)`.
4. **Truth grows** (#28): `Finishing 14 → 15` at band 1: same offset (−2) ⇒ `center = 13` ⇒
   `[9, 17]` — the live-form window re-centres on current truth (FR-SC-010), width unchanged.
5. **Assignment completes** at band 4 ⇒ `w = 0` ⇒ `[15, 15]` (current truth) — the exact-value
   identity, no draw, assignment cleared.
6. **Manager buys the player** — the `PlayerId` re-keys into club 3; the overlay entry is dropped
   (FR-SC-019) and the own-squad rule resolves `BAND_MAX` thereafter.

All integer; two runs identical; the #27 record was never written.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial appendices (constant catalogue, sub-blob layout, end-to-end worked example), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M-3): Appendix B gains the `managedClubId` field (the `StaffSaveCodec` precedent) so the own-squad assignment check is codec-performable; pool resolvability split out as a post-load composition-root validation (the v0.1 note assigned the codec a check it could not perform). |
| 0.3 | 2026-07-24 | — | Section-file AR PASS-2 (L): overlay entries gain the codec-level not-own-squad coherence gate (F4 — the hygiene rule drops an entry on a buy, so an own-squad entry is incoherent state). |
#endregion
