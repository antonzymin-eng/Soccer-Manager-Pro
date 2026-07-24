# Scouting & Player Knowledge #32 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.3 — cross-set AR; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

## 3.1 Band resolution (FR-SC-002/007/009)

The band a view read resolves at — the only place the `fogEnabled` dial and the own-squad rule act
**on the view path** (the dial's other two sites are the command/tick gates — FR-SC-002/020/022):

```
ResolveBand(playerId, managedClubId, overlay) -> int:
    if not fogEnabled:                      return KNOWLEDGE_BAND_MAX      # minimal identity (FR-SC-007)
    if playerId / CLUB_SQUAD_SIZE == managedClubId:
                                            return KNOWLEDGE_BAND_MAX      # own-squad omniscience (FR-SC-009)
    if overlay.TryGet(playerId, out e):     return e.KnowledgeBand         # scouted external player
    return 0                                                               # unscouted external player
```

Integer division on the club-scoped `PlayerId = clubId·CLUB_SQUAD_SIZE + localIndex` (#27 FR-SQ) makes
the own-squad check derivable with no extra state. `EstimateFor` itself never sees the dial — one code
path (KD-8).

## 3.2 Estimate derivation (FR-SC-003/004/005/010/012)

For attribute `a` (by `AttrIdx` ordinal) of a truth record `t` at band `b`:

```
EstimateAttribute(t, playerId, b, attrIdx) -> AttributeEstimate:
    w := KNOWLEDGE_BAND_HALFWIDTH[b]                       # [GT] table, strictly decreasing to 0 (FR-SC-005)
    truth := t.Attributes[attrIdx]                         # read-only (FR-SC-001)
    if w == 0: return { Min = truth, Max = truth }         # exact — NO RNG call (FR-SC-004/012)
    draw   := KeyedDraw(scouting.accuracy, playerId, DeriveScoutOrdinal(b, attrIdx, Center))   # §3.3, deep
    offset := (int)(draw mod (2w + 1)) − w                 # uniform in [−w, +w]
    center := truth + offset
    return { Min = max(ATTRIBUTE_MIN, center − w), Max = min(ATTRIBUTE_MAX, center + w) }
```

**Containment proof (FR-SC-003):** `|center − truth| = |offset| ≤ w` ⇒ `truth ∈ [center − w,
center + w]`; `truth ∈ [1, 20]` always; therefore `truth` is in the intersection — the returned
`[Min, Max]` — and the intersection is non-empty. The invariant is directly testable without exposing
truth to the test's assertion surface.

**Worked example.** `truth = 14`, band `b = 1` with illustrative `HALFWIDTH = {6, 4, 2, 1, 0}` ⇒
`w = 4`; suppose the keyed draw yields `offset = −2` ⇒ `center = 12` ⇒ estimate `[8, 16]` (contains
14 ✓). Band advances to 3 (`w = 1`), keyed draw for the new band yields `offset = +1` ⇒ `center = 15`
⇒ `[14, 16]` (contains 14 ✓, strictly narrower). At `b = 4 = BAND_MAX` (`w = 0`): `[14, 14]` — the
exact-value identity, no draw. All integer; two runs identical.

**Freshness (FR-SC-010):** `truth` is the **current** record — the window tracks live form (KD-1's
pinned live-form semantic). The offset for a given `(playerId, band, attrIdx)` never changes; only a
band advance (new `b` in the key) re-centres.

## 3.3 Keyed-ordinal derivation (FR-SC-011 — the #41 §3.1.1 fixed-radix mechanism)

```
DeriveScoutOrdinal(band, attrIdx, purpose) -> u64:
    assert 0 <= band    < SCOUT_BAND_RADIX                 # bound guards (fail loud, F4-class)
    assert 0 <= attrIdx < SCOUT_ATTR_RADIX
    assert 0 <= purpose < SCOUT_PURPOSE_RADIX
    return ((u64)band * SCOUT_ATTR_RADIX + (u64)attrIdx) * SCOUT_PURPOSE_RADIX + (u64)purpose
```

A pure bijection — **not** an incrementing counter. Two calls with the same `(playerId, band,
attrIdx, purpose)` always resolve the same draw regardless of call order, day, or save/restore
(`entityId = playerId` on the `scouting.accuracy` stream). The radices are **fixed constants**
(`SCOUT_ATTR_RADIX = 32 > ATTRIBUTE_COUNT = 31`; `SCOUT_PURPOSE_RADIX` fixed with APPEND-only purpose
ordinals; `SCOUT_BAND_RADIX` fixed above any tuned band count) — using a growing count as a radix
would shift all prior ordinals on append, the exact cross-version replay-parity hazard #41's
`DRAW_PURPOSE_RADIX` exists to prevent. **`worldDay` is deliberately absent from the key** (FR-SC-011):
an estimate must be stable across days until its band advances.

The stream itself is registered only at the deep T-phase's first draw (`DOMAIN_TAG_SCOUTING = 0x24` /
`SubsystemOrdinals.Scouting = 86` promote then — FR-SC-013); the minimal tier never reaches this
function (every read short-circuits at `w == 0`).

## 3.4 Assignment lifecycle (deep — FR-SC-020/021/022/023)

```
AssignScout(playerId):                                     # manager command (FR-SC-020)
    require fogEnabled                                     # F2 — with fog off there is nothing to scout
    require ResolvableInPool(playerId)                     # F1
    require playerId / CLUB_SQUAD_SIZE != managedClubId    # F2 — own-squad has nothing to scout
    require no active assignment                           # F2 — MAX_ACTIVE_ASSIGNMENTS = 1 (ChiefScout)
    require BandOf(playerId) < KNOWLEDGE_BAND_MAX          # F2 — fully-scouted has nothing to learn
    active := (playerId, DaysIntoBand = 0)

CancelAssignment():                                        # manager command (FR-SC-021)
    require active assignment exists                       # F2
    active := none                                         # in-band progress discarded; completed bands kept

AdvanceScoutingDay(quality):                               # #30-invoked at slot 7 (FR-SC-022)
    if not fogEnabled: return                              # fog off => no-op; a loaded assignment stays INERT
    if active is none: return
    require quality > 0                                    # F5 (FR-SC-023)
    active.DaysIntoBand += 1
    if active.DaysIntoBand >= DaysPerBand(quality):
        b := BandOf(active.PlayerId) + 1                   # band-up
        overlay[active.PlayerId] := (b, worldDay)          # stores band + report stamp ONLY (FR-SC-006)
        if b == KNOWLEDGE_BAND_MAX: active := none         # assignment complete
        else:                       active.DaysIntoBand := 0

DaysPerBand(quality) := max(1, DAYS_PER_BAND_BASE * SCOUT_QUALITY_NEUTRAL_PERMILLE / quality)
```

`quality` is #34's `ToScoutQuality(chiefScout)` (per-mille; neutral house scout ⇒ `1000` ⇒ base
cadence). **Worked example:** `DAYS_PER_BAND_BASE = 14` (illustrative), a scout of quality `1250` ⇒
`max(1, 14 · 1000 / 1250) = max(1, 11) = 11` days per band (integer floor); a neutral scout ⇒ 14; a
poor scout of `700` ⇒ 20. Speed only — the width table never sees `quality` (KD-4).

## 3.5 Reports & ranking (FR-SC-006/026)

A **report** is the overlay entry itself — `(PlayerId, KnowledgeBand, LastReportWorldDay)` — plus the
estimates re-derived on read (§3.2): nothing else is stored (KD-1). Presentation (#38) renders the
derived `KnownPlayer`; #46 may later aggregate band-up events (deferred, FR-LW-031).

```
RankByEstimate(position, candidates: KnownPlayer[]) -> ordered PlayerIds:   # deep, read-only (FR-SC-026)
    score(p) := sum over position-relevant attrIdx of (p.Est[attrIdx].Min + p.Est[attrIdx].Max)   # 2×midpoint, integer
    order by score desc, then PlayerId asc                                  # total order, deterministic
```

Pure, draw-free, allocation-bounded by the caller-supplied candidate list; no stored shortlist. The
position-relevance attribute sets are a `[GT]` table (Appendix A) whose magnitudes are
balance-pass-illustrative. The manager's subsequent action is #31's `SubmitBid`, unchanged.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §3 (band resolution, estimate derivation + containment proof + worked example, fixed-radix keyed ordinal, assignment lifecycle + DaysPerBand worked example, reports/ranking), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1 (M-1): §3.4 `AssignScout` gains the `fogEnabled` gate; `AdvanceScoutingDay` is a no-op with fog off (a loaded assignment stays inert — FR-SC-022). |
| 0.3 | 2026-07-24 | — | Cross-set AR pass 2 (L residual of M-2): §3.1's "the only place the dial acts" scoped to **the view path** (the dial's other two sites are the FR-SC-020/022 command/tick gates). |
#endregion
