# Season & Competition Loop Specification #30 — Appendices

**Created:** July 22, 2026
**Last Updated:** August 11, 2026 (v1.1 — ERR-028-019 back-prop: Appendix B.1 gains the anchor-vs-clock rule (§2.3's new F10) as a paragraph adjacent to the existing cursor-vs-clock rule — a DIFFERENT invariant (#28's `BirthWorldDay` anchor, checked ahead-only) sharing the same two-boundary, one-shared-owner mechanism)
**Last Updated (prior):** August 9, 2026 (v1.0 — ERR-028-014: Appendix B.1's cursor-vs-clock paragraph corrected from "all three ... cursors" / "exempt in every case" to the fourth (#28) cursor kind and the #28-only exception to the sentinel exemption, with the reason carried in full — the sweep-stopped-at-a-grep-boundary class, corrected here in the same pass as `section-2.md`'s F8 row)
**Last Updated (prior):** August 8, 2026, later still (v0.9 — ERR-030-030: Appendix A's frame-version row 4 → 5 and Appendix B's frame gains the mandatory #28 progression sub-blob, for #28 T1)
**Last Updated (prior):** August 8, 2026, later same day (v0.8 — balance-pass AR pass 13 M4: Appendix A's frame-version row corrected 2 → 4 and the three #30-owned appearance constants catalogued)
**Last Updated (prior):** August 8, 2026 (v0.7 — balance-pass AR pass 11 M2: the cross-blob cursor rule stated in full in Appendix B)
**Last Updated (prior):** August 8, 2026 (v0.6 — **ERR-030-028**: new **B.1** pins the appearance sub-blob's byte layout field by field — it was specified in NO spec, existing only in `AppearanceSaveCodec.cs`'s own comment, while F3 makes the first written layout the format permanently (the ERR-029-004 class, on the block created one landing after that ERR was filed); + the four sibling MUSTs and the deliberate no-`[GT]`-gating-on-decode decision)
**Last Updated (prior):** August 7, 2026 (v0.5 — the #29/#41 balance pass D2 (ERR-041-010(b)): Appendix B's outer-frame description gains the three mandatory career sub-blobs — the #29 training and #41 medical blocks (frame v2→3, landed at their T1 and previously unrecorded here) and the new #30 appearance block (frame v3→4), between the season block and the optional match block)
**Last Updated (prior):** July 27, 2026 (v0.4 — back-props ERR-030-017 (#47 conditional authored sub-blob) + ERR-030-019 (#50 `SaveOriginStamp` in the outer frame) landed atomically with the ten-spec approval wave; Appendix B's outer-frame description amended)
**Last Updated (prior):** July 25, 2026 (v0.3 — ERR-030-010 Appendix C venue correction, found at #30 T0)
**Version:** 1.1
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## Appendix A — Constant catalogue (`SeasonLoopConstants`)

All values proposed; magnitudes are illustrative pending a Stage-2 balance pass (the #21 §9.2
precedent — the spec's contract is the shapes/directions, the `[GT]` numbers are tunable).

| Constant | Tag | Value | Meaning |
|---|---|---|---|
| `SEASON_SAVE_FORMAT_VERSION` | `[FIXED]` | 5 | outer season-frame version (owned by `SeasonSaveConstants`) — 1 → 2 at #30 T1 (the season block), 2 → 3 at #29/#41 T1 (the training + medical blocks), 3 → 4 at the balance pass D2 (the appearance block; Appendix B), 4 → 5 at #28 T1 (ERR-030-030): the mandatory `PROG` career-state sub-blob. *(Row corrected at AR pass 13 M4 — it read 2 while Appendix B in this same file described the v4 frame; corrected again August 8, 2026 — it read 4 while #28 T1 shipped the v5 frame the same day.)* |
| `SEASON_STATE_FORMAT_VERSION` | `[FIXED]` | 1 | the season sub-blob's own version (new) |
| `APPEARANCE_SAVE_MAGIC` | `[FIXED]` | `"APPR"` | the appearance sub-blob's self-identifying leading tag (Appendix B.1; the ERR-029-005/ERR-041-009 rule — a format version is not a format identifier) |
| `APPEARANCE_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the appearance sub-blob's own version (Appendix B.1) |
| `APPEARANCE_BITMASK_MAX_WINDOW_DAYS` | `[FIXED]` | 31 | the structural ceiling of the u32 appearance day-bitmask — `AppearanceWindow` fail-louds a configured window outside `[1, 31]` at the reading site, and #41's `APPEARANCE_WINDOW_DAYS` `[GT]` is bounded by it (its catalogue lock hard-codes the 31 because #41 sits below `season-save` and cannot read this constant). *(Catalogued at AR pass 13 M4 — load-bearing since D2, previously in no spec: ERR-030-028's class on a constant.)* |
| `WIN_POINTS` | `[GT]` | 3 | points for a win |
| `DRAW_POINTS` | `[GT]` | 1 | points for a draw |
| `LOSS_POINTS` | `[GT]` | 0 | points for a loss |
| `DEFAULT_OBJECTIVE_POSITION` | `[GT]` | (per-club) | the board's "finish at or above" target |
| `DOMAIN_TAG_SEASON_LOOP` | `[CROSS]` | `0x22` | mirror of `DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP` (#16 §3.4; allocated at approval) |
| `SUBSYSTEM_ORDINAL_SEASON_LOOP` | `[CROSS]` | 84 | mirror of `SubsystemOrdinals.SeasonLoop` (#16; allocated at approval) |

`WIN/DRAW/LOSS_POINTS` are the association-football 3/1/0 convention (§8.2); a `[GT]` catalogue value,
not a physical constant, so a rules variant (e.g. 2/1/0) is a config change.

## Appendix B — Season-state sub-blob byte layout (KD-1 / §3.6)

The season block, in order (all via `CanonicalSerializer`; every length prefix via an overflow-safe
`ReadCount`, `0 ≤ n ≤ remaining`):

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | version | u32 | `SEASON_STATE_FORMAT_VERSION`; gate first (F3) |
| 2 | seed | u64 | the season seed |
| 3 | seasonNumber | i32 | multi-season counter |
| 3a | managedClubId | i32 | the human manager's club (KD-9 / FR-SN-013b) |
| 4 | clubCount | count | `ReadCount` |
| 5 | clubIds[] | i32 × clubCount | the roster world |
| 6 | fixtureCount | count | `N·(N−1)` |
| 7 | fixtures[] | (round i32, home i32, away i32, played u8) × fixtureCount | the serialized schedule (KD-5) |
| 8 | calendar | (nextRoundIndex i32, roundCount count, roundToDay[] u32) | the cursor (KD-4); day values are `uint` to match `WorldStore.CurrentWorldTick` |
| 9 | tableRowCount | count | = clubCount |
| 10 | tableRows[] | (clubId, P, W, D, L, GF, GA, GD, Pts) i32 × 9 × rows | ClubId order |
| 11 | board | (targetPosition i32, jobSecurityPerMille i32) | the objective + security |

> **Row 11 pinned at #30 T1 (ERR-030-011).** The v0.1 row left the representation open as
> `jobSecurity f32/u8` — neither of which the implementation uses. #30 T0 resolved `BoardState` to an
> integer per-mille in `[0, JobSecurityScale]`, following the integer-arithmetic convention every later
> management spec standardized on (#41's AR-1 moved that spec's whole model float → integer per-mille;
> #40 uses integer currency; #33 uses per-mille scalars), and recorded the row as a back-prop
> candidate. T1 is where it became a real byte layout, so the row is now pinned to `i32`. Integers also
> make the sub-blob round-trip exact with no NaN gate.


The outer `SeasonSaveCodec` frame nesting this block, **as amended by the July 27, 2026 approval wave,
the #29/#41 landings (T1 frame v3; the balance pass frame v4), and #28 T1 (ERR-030-030, frame v5)**:

`SEASON_SAVE_FORMAT_VERSION (u32) → SaveOriginStamp{ WorldGenerationVersion i32, BuildId i32 } →
matchPresent flag (u8) → hasAuthoredDb flag (u8) → [len u32]world → [len u32]season →
[len u32]training → [len u32]medical → [len u32]appearance → [len u32]progression →
([len u32]match iff matchPresent) → ([len u32]authoredDb iff hasAuthoredDb)`

Trailing bytes after the declared content ⇒ throw (F3).

**The four mandatory career sub-blobs.** The #29 training block (`TRAINING_SAVE_FORMAT_VERSION`,
FR-TR-018, frame v2→3), the #41 medical block (`MEDICAL_SAVE_FORMAT_VERSION`, FR-MD-017, same bump),
the #30 appearance block (`APPEARANCE_SAVE_FORMAT_VERSION`, ERR-041-010(b), frame v3→4) and the #28
progression block (`PROGRESSION_SAVE_FORMAT_VERSION`, FR-PG-016/017, frame v4→5, ERR-030-030) sit
between the season block and the optional match block, in that order, all four **mandatory** — career
state has no absent case, only an empty one (a zero-club block), so no presence flags are added and a
later wiring change needs no further frame bump. Each is typed at the `Encode` seam
(`TrainingBlock` / `MedicalBlock` / `AppearanceBlock` / `ProgressionBlock`) and self-identified by a
leading magic (ERR-029-005 / ERR-041-009 / ERR-028-004: a format version distinguishes generations of
one format, never one format from another). The appearance block is #30's own domain — the per-player
fielded-XI record that supplies #41's FR-MD-010 `MatchLoad`, which neither sibling block may carry
(each is forbidden to describe the other's domain).

**The progression block is different in kind from its three siblings: it carries the ROSTER itself,
not an overlay on one.** Training/medical/appearance each hold state keyed against a roster that
still comes from elsewhere (the world-seed bootstrap); #28's block holds the complete evolving
`PlayerRecord` — identity **and** the `[1,20]` attributes, per #28 KD-4 — plus its `PlayerLifecycle`
overlay, because from this frame version a career's `[1,20]` attributes are no longer a pure function
of the world seed (they grow). **This retires roadmap A3's property that a career could be reopened
from the world seed alone**: from v5, a club's roster comes from the save file's progression block,
never from re-running the bootstrap over `WorldGenerationVersion`/`Seed`. Bootstrap generation is
still a pure function of the world seed — that is what makes a *new* game reproducible, and it is
exactly what `ProgressionEngine.SeedFrom` consumes at new-game — it is simply no longer how an
*existing* career gets its rosters back. See #28 §3.5 for the progression block's own byte layout
(magic-led `PROG`, mirroring this appendix's B.1 discipline for the appearance block); it is not
duplicated here, the same split as the training/medical blocks, each pinned in its own spec. *(Note
the `SaveOriginStamp` / `hasAuthoredDb` elements above remain future amendments landing at #50/#47 T1;
the frame in code today is
`version → matchPresent → world → season → training → medical → appearance → progression → [match]`.)*

**B.1 The appearance sub-blob's byte layout (ERR-030-028, balance-pass AR pass 5).** Pinned here
because **F3 refuses every cross-version migration, so the first written layout IS the format
permanently** — the exact reasoning ERR-029-004 / ERR-041-008 recorded when the sibling #29/#41
blocks got their layouts pinned, which this block shipped without (the same defect class, one landing
later, on the block created by the landing that fixed it in the siblings):

```
EncodeAppearance(clubs) -> bytes            # canonicalized; DecodeAppearance is the exact inverse
    WriteU32(APPEARANCE_SAVE_MAGIC)          # "APPR" — BEFORE the version (ERR-029-005: a version
                                             # gates generations of ONE format, never one format
                                             # from another; every sub-blob sits at version 1)
    WriteU32(APPEARANCE_SAVE_FORMAT_VERSION) # 1
    WriteU32(clubCount)
    per club, ascending ClubId:              # canonical order — the block is a MAP, order is not state
        WriteI32(clubId)                     # club identity is WRITTEN, never implied by list order
                                             # (the ERR-041-008 rule: order-carried identity is an
                                             # implicit agreement with a sibling blob KD-2 forbids)
        WriteU32(playerCount)
        per player, ascending PlayerId:
            WriteI32(playerId)
            WriteU32(recentBits)             # the day-bitmask (bit k = fielded on anchor − k)
            WriteU32(bitsAsOfWorldDay)       # the anchor day bit 0 refers to
```

MUSTs, mirroring #29 §4.4 / #41 §4.4: the magic is checked before the version and a block without it
is refused; keys are strictly ascending on decode (every career lookup is a binary search over them);
trailing bytes after the declared content throw (F3); the coherence gates run on encode as well as
decode, so the codec can never write a block its own decode refuses. **Deliberately NO `[GT]` gating
on decode:** `recentBits` is structurally valid at any value — bits outside the configured window are
dead weight the read masks off — and gating it against `AppearanceWindowDays` would turn a window
retune into data loss (the ERR-029-004 rule). The cross-blob cursor-vs-clock rule is stated here in full (AR pass 11 M2 — this paragraph previously
covered one cursor kind, one direction, one boundary; corrected again at **ERR-028-014** for a fourth
cursor kind and a fourth, non-uniform exemption): **all four persisted per-player cursors must sit
inside the coherent band relative to the world clock, checked at Save, at Load AND at `SeasonLoop`
composition** (§2.3 F8). The #29/#41 `LastAdvancedWorldDay` cursors, and #28's progression
`LastAdvancedWorldDay` (ERR-028-007 — the fourth, added at #28 T1/T2a), are all checked in BOTH
directions — AHEAD of the clock means the sibling specs' F6 idempotency silently skips the day step
until the clock catches up; LAGGING by two or more is WORSE, because their F7 gap refusal then fires on
every later advance and, the career day-steps running before the clock increment (§3.3.2), the gap can
never close: the career wedges permanently while the file saves and reloads cleanly (**#28's lag case
compounds this** — `ProgressionEngine.AdvanceDay` REPLAYS a gap rather than banking one day, so a
mispaired restore would bank N days of growth from a single day's inputs, invisibly). The appearance
anchor is checked AHEAD-only — a lazily-shifted bitmask has no gap contract (shifting is the read's
job).

**The sentinel (never-advanced) is exempt for #29/#41 only — it is NOT exempt for #28, and is not a
legal #28 store state at all (ERR-028-014).** The reason is the load-bearing part, not the exception
itself: #29's and #41's fresh states carry no clock-anchored quantity — a freshly created training or
medical record means exactly the same thing ("never advanced") on world day 0 as on world day 40,000 —
so their sentinel cursor is coherent at any clock and the band check waves it through unconditionally.
#28's fresh state is the only one of the four that DOES carry a clock-anchored quantity: a player's age
is derived from `BirthWorldDay` (§3.1.1), so a never-advanced #28 state would mean something different
at every clock value it was paired against — the premise the siblings' exemption rests on is false for
#28, and inheriting the exemption verbatim would have left the gate with a hole shaped exactly like the
state every new game starts in. Accordingly `ProgressionEngine.SeedFrom` anchors the cursor at the seed
day it is handed (never at the sentinel), and `FromBlocks` refuses a lifecycle carrying the sentinel
outright — so #28's cursor is checked against the coherent band unconditionally, with no exempted value.

All four boundaries evaluate ONE predicate set —
`PlayerCareerStates`' per-cursor owners — so the save root's gate and the composition gate cannot drift
(the parallel-surface rule). `SeasonSaveManager` owns the file-boundary halves as the only layer holding
the world blob and the career blocks together.

**The sibling anchor-vs-clock rule (ERR-028-019 back-prop, §2.3's new F10) — a DIFFERENT invariant,
sharing the mechanism above but not the predicate.** #28's `BirthWorldDay` is an ANCHOR, not one of the
four cursors above — a player's age is derived from it (#28 §3.1.1), and an anchor arbitrarily far in
the PAST is the ORDINARY case for a generated player (#28's `BirthWorldDay` is signed and typically
negative at new-game, ERR-028-006), so this rule is checked AHEAD-only, never for lag, unlike the
bidirectional cursor rule above. An anchor ahead of the world clock is corrupt state: #28's own
`GrowthProjection.AdvanceDayForPlayer` fails loud on the resulting negative `ageDays` (its M2(a) guard),
but only once a day step actually reaches that player — this rule is the boundary that refuses the
pairing BEFORE a day step can. `ProgressionSaveCodec`'s own value gates (#28 §3.5) cannot enforce this
half at all: the codec has no world clock to bound the anchor's upper end against, only the format's own
`uint.MaxValue` ceiling, which rules out anchors that cannot correspond to ANY reachable world day, not
anchors ahead of THIS particular clock. `PlayerCareerStates.RequireBirthWorldDayWithinClock` is the
shared owner, called from the SAME two boundaries as the cursor rule above — `SeasonLoop`'s per-player
composition walk and `SeasonSaveManager`'s block-level walk (Save AND Load) — alongside the cursor check
in both, so the two rules cannot drift from each other by one boundary gaining a call the other lacks.

**`SaveOriginStamp` (ERR-030-019, at #50's approval)** sits in the **frame**, immediately after the
version and **before any length-prefixed blob**. The placement is load-bearing rather than aesthetic:
#50's classifier reads version fields **without parsing any sub-blob**, and a stamp inside the season
block would force it to parse into one in order to classify — defeating the property that makes
classification both cheap and safe. `WorldGenerationVersion` is the migration input; `BuildId` is
**diagnostic only** and MUST NOT be a migration input, since migrating off a build number would make two
builds sharing a format falsely incompatible. **This carries a `SEASON_SAVE_FORMAT_VERSION` bump at
#50's T1** — pre-bump saves are rejected fail-loud, with no migration, which is the same posture the
codec already takes.

**The authored-database sub-blob (ERR-030-017, at #47's approval)** is written **only when
`hasAuthoredDb`**. A generated game writes **no block at all — not an empty one**, which is what keeps a
generated save byte-identical to pre-#47 rather than merely similar. The flag and the block's presence
MUST agree in both directions, and a mismatch fails loud: a generated save carrying a stale authored
block would load the wrong rosters as silently as an authored save missing its block would regenerate
them. The frame does not parse the block (FR-ED-011); `AUTHORED_DB_SAVE_FORMAT_VERSION` is #47's.

**Both additions are conditional or fixed-width, and neither touches the world, season or match blobs.**

## Appendix C — Worked 4-club round-robin schedule

`clubIds = [10, 11, 12, 13]`, identity permutation, circle method (M = 4, index 0 pinned):

| Round | Fixture 1 | Fixture 2 |
|---|---|---|
| 0 | 10 v 13 | 11 v 12 |
| 1 | 12 v 10 | 11 v 13 |
| 2 | 10 v 11 | 12 v 13 |
| 3 | 13 v 10 | 12 v 11 |
| 4 | 10 v 12 | 13 v 11 |
| 5 | 11 v 10 | 13 v 12 |

> **Corrected at #30 T0 (ERR-030-010).** Rounds 1 and 4 previously read `10 v 12 / 13 v 11` and
> `12 v 10 / 11 v 13` — the venues were inverted because this table (and the identical §3.7 one) was
> hand-derived without applying §3.1's round-parity venue rule. The **pairings were always right**;
> only the home/away side of the odd first-leg round (and its second-leg mirror) changed, so the set
> of 12 ordered pairs below is unchanged. Measured at the Stage-2 target size of 20 clubs, the
> unparried form gives the pinned club **all 19** first-leg fixtures at home; with parity every club
> lands in 8–10 of an ideal 9–10.

- 12 fixtures = `N·(N−1) = 4·3` (FR-SN-002).
- Every ordered pair appears once: `{10v13, 10v12, 10v11, 13v10, 12v10, 11v10, 11v12, 13v11, 12v13,
  12v11, 11v13, 13v12}` — all 12 ordered pairs of distinct clubs.
- Each club appears exactly once per round (FR-SN-003): round 0 = {10,13,11,12}, etc.
- Rounds 0–2 are the first leg, 3–5 the second leg with venues reversed (round offset `M−1 = 3`).

## Appendix D — Table tie-break worked example

Two clubs tied through the first three keys:

| Club | P | W | D | L | GF | GA | GD | Pts |
|---|---|---|---|---|---|---|---|---|
| 10 | 3 | 2 | 0 | 1 | 5 | 3 | +2 | 6 |
| 11 | 3 | 2 | 0 | 1 | 5 | 3 | +2 | 6 |

Points equal (6=6) → GD equal (+2=+2) → GF equal (5=5) → **ClubId ascending**: 10 orders above 11.
The final key is `ClubId`, and clubIds are unique (F2 keeps each club to one row), so the comparator
is a **total order** — no two rows ever compare equal (FR-SN-007).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial appendices: constant catalogue, season-state byte layout, worked 4-club schedule, tie-break worked example. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
| 0.3 | 2026-07-25 | — | **ERR-030-010** (found at #30 T0 implementation): Appendix C rounds 1 and 4 venue-corrected — the table was hand-derived without §3.1's round-parity venue rule. Pairings unchanged, so the 12-ordered-pair completeness bullet is unaffected; justification (20-club venue distribution) recorded inline. |
| 0.4 | 2026-07-27 | — | **ERR-030-019** (#50) + **ERR-030-017** (#47), landed atomically with the ten-spec approval wave. Appendix B's outer-frame description gains the `SaveOriginStamp` (`WorldGenerationVersion` + `BuildId`) immediately after the version field and **before any length-prefixed blob** — the placement is load-bearing, since #50's classifier must read the generation version without parsing a sub-blob, and `BuildId` is recorded as **diagnostic only** so it can never become a migration input; and the **conditional** authored-database sub-blob, written only when `hasAuthoredDb`, with the flag/blob agreement required in both directions and failing loud. The world, season and match blobs are byte-untouched by both. |
| 0.5 | 2026-08-07 | — | **Balance pass D2 (ERR-041-010(b))**: Appendix B's frame gains the mandatory #29 training / #41 medical blocks (v2→3 — a T1 change this appendix had missed) and the #30 appearance block (v3→4): the per-player fielded-XI record supplying FR-MD-010's `MatchLoad`, #30-owned because neither sibling block may describe the other's domain. All three mandatory (career state has an empty case, never an absent one), typed at the Encode seam, magic-led per ERR-029-005/ERR-041-009. |
| 0.6 | 2026-08-08 | — | **ERR-030-028** (balance-pass AR pass 5, M1): new **B.1** — the appearance sub-blob's byte layout pinned field by field (magic → version → clubCount → {clubId, playerCount} → {playerId, recentBits, bitsAsOfWorldDay}), the four MUSTs its siblings carry, and the deliberate no-`[GT]`-gating-on-decode decision. The layout had shipped into every v4 save while specified in no spec — F3 makes the first written layout the format permanently, the exact ERR-029-004 reasoning, missed on the very next block. |
| 0.7 | 2026-08-08 | — | **Balance-pass AR pass 11 (M2)**: the cursor-vs-clock paragraph stated in FULL — the prior single sentence covered the appearance anchor's ahead direction at the save root only, while the enforced rule spans three cursor kinds, two directions and three boundaries (§2.3's new F8; one shared predicate set). |
| 0.8 | 2026-08-08 | — | **Balance-pass AR pass 13 (M4)**: Appendix A still said `SEASON_SAVE_FORMAT_VERSION = 2` — the identical wrong value pass 5 M6 fixed in the manifest, left in the OWNING catalogue, contradicting Appendix B in the same file — and carried no rows for `APPEARANCE_SAVE_MAGIC` / `APPEARANCE_SAVE_FORMAT_VERSION` / `APPEARANCE_BITMASK_MAX_WINDOW_DAYS`, the last load-bearing (the `AppearanceWindow` runtime guard reads it; #41's lock hard-codes its value) and in NO spec anywhere — ERR-030-028's class on a constant, one appendix over from where that ERR landed. |
| 0.9 | 2026-08-08 | — | **ERR-030-030** (found at #28 T2a implementation): Appendix A's `SEASON_SAVE_FORMAT_VERSION` row 4 → 5 for the mandatory #28 `PROG` sub-blob. Appendix B's outer-frame nesting string gains `[len u32]progression` between `appearance` and the optional `match`; "three mandatory career sub-blobs" → four; new paragraph explaining the #28 block carries the ROSTER itself (KD-4) rather than an overlay, so from v5 a career's rosters come from the save file, not from re-running the bootstrap on the world seed — retiring roadmap A3's from-seed-alone reopening property. Byte layout not duplicated here; see #28 §3.5. |
| 1.0 | 2026-08-09 | — | **ERR-028-014** (found at #28 implementation, August 8–9, 2026): Appendix B.1's cross-blob cursor-vs-clock paragraph still said "all three persisted per-player cursors" and "the sentinel ... is exempt in every case" — both false the day #28's progression cursor became the fourth (ERR-028-007) and #28's own sentinel exemption was retired as the defect it was. Corrected to name all four cursor kinds, state #28's worse-case lag consequence (`AdvanceDay` replays a gap rather than banking one day), and carry the full reason #28 alone has no sentinel exemption: #29/#41's fresh state carries no clock-anchored quantity (so "never advanced" is coherent at any clock), while #28's fresh state derives age from `BirthWorldDay` (so it is not) — `SeedFrom` anchors the cursor at the seed day and `FromBlocks` refuses a carried sentinel accordingly. Landed in the same pass as `section-2.md`'s F8 row, the section that paragraph exists to describe. |
| 1.1 | 2026-08-11 | — | **ERR-028-019 back-prop** (docs close-out for #28's AR passes 5-8, four production landings — `39c385a`, `cf5abf0`, `8556ddd`, `b798ce2` — with no `docs/specs/` edit at all): Appendix B.1 gains a new paragraph for `PlayerCareerStates.RequireBirthWorldDayWithinClock` (AR pass 6 M2(b)), the anchor-vs-clock rule §2.3's new F10 states — a DIFFERENT invariant from the cursor-vs-clock paragraph immediately above (an ANCHOR, checked ahead-only, never for lag, since an anchor arbitrarily in the past is ordinary for #28), sharing its two-boundary (`SeasonLoop` composition, `SeasonSaveManager` Save/Load) one-shared-owner mechanism. This rule had NO normative text anywhere in `docs/specs/` before this pass despite being enforced in `src/season-save/` since `cf5abf0` (August 11, 2026). No code changed by this back-prop. |
#endregion
