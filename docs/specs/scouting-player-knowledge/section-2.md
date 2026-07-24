# Scouting & Player Knowledge #32 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.4 — cross-set AR; prior v0.3 — section-file AR PASS-2; prior v0.2 PASS-1, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-SC-001..027)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-SC-001 | #32 MUST NOT write any #27 state, ever. The view MUST be a pure function over a **caller-supplied** `in PlayerRecord`; #32 MUST hold no reference into #27's stores and MUST expose no API taking `ref Squad`/`ref PlayerRecord`. Exercising every #32 path MUST leave the #27 canonical squads byte-identical. | MUST | KD-2 |
| FR-SC-002 | `EstimateFor(in PlayerRecord, int knowledgeBand)` MUST be the **single** view function (one code path): the minimal tier calls it with `band = KNOWLEDGE_BAND_MAX` for every player; the deep tier with the resolved band (§3.1). No **view** consumer branches on `fogEnabled` — the dial acts in exactly **three** places: `ResolveBand` (§3.1), the `AssignScout` gate (FR-SC-020), and the `AdvanceScoutingDay` no-op (FR-SC-022). | MUST | KD-8 |
| FR-SC-003 | Every `AttributeEstimate` MUST satisfy `Min ≤ Max`, both `∈ [ATTRIBUTE_MIN, ATTRIBUTE_MAX]`, and **`truth ∈ [Min, Max]`** (the containment invariant — reports are honest-but-imprecise). | MUST | KD-1 |
| FR-SC-004 | `KNOWLEDGE_BAND_HALFWIDTH[KNOWLEDGE_BAND_MAX]` MUST be `0`, so a `BAND_MAX` read returns `[truth, truth]` **arithmetically** (no special case). | MUST | KD-1 |
| FR-SC-005 | `KNOWLEDGE_BAND_HALFWIDTH[]` MUST be strictly decreasing in band, ending at `0`; the `[GT]` magnitudes are balance-pass-illustrative — the reviewed contract is the shape. | MUST | KD-1 |
| FR-SC-006 | The overlay MUST store per scouted player only `(PlayerId, KnowledgeBand, LastReportWorldDay)`; per-attribute estimates/ranges MUST NOT be serialized (derived on read). | MUST | KD-1 |
| FR-SC-007 | A career that has **never enabled fog** MUST advance **byte-identical** to pre-#32: every read resolves at `BAND_MAX`, the overlay is empty and stays empty, no assignment exists, the #30 tick slot is a no-op, and **zero RNG calls** are made. (A fogged save later loaded into a fog-off config is behaviourally omniscient but carries its preserved state inertly — FR-SC-022; the byte-identity claim applies to the never-fogged career.) | MUST | KD-8 |
| FR-SC-008 | Fog MUST cover only the 31 `[1,20]` attributes of **external** players; identity facts (name/age/position) and `WeakFootRating [1,5]` MUST be exact at any band. | MUST | KD-2 |
| FR-SC-009 | The managed club's own players MUST always resolve at `KNOWLEDGE_BAND_MAX` (own-squad omniscience — a short-circuit ahead of the overlay read, `PlayerId / CLUB_SQUAD_SIZE == managedClubId`); `AssignScout` on an own-squad `PlayerId` MUST fail loud. | MUST | KD-2 |
| FR-SC-010 | The Stage-3 freshness semantic is the **live-form window**: the estimate centre derives from **current** truth (it tracks #28 development); the width — not freshness — is the scouted quantity. Frozen-at-report staleness is a §7 extension, not silently introduced. | MUST | KD-1 |
| FR-SC-011 | *(deep)* Accuracy draws MUST be **position-independent keyed draws** on the `scouting.accuracy` stream (`entityId = playerId`) with a fixed-radix action ordinal over `(band, attrIdx, purpose)` (§3.3) — **NOT keyed on `worldDay`**; no free-running cursor MUST exist or be serialized. | MUST | KD-3 |
| FR-SC-012 | A zero-width estimate (`HALFWIDTH[band] == 0`) MUST make **no RNG call** (short-circuit before the draw), so the minimal tier is provably draw-free. | MUST | KD-3 |
| FR-SC-013 | The minimal tier MUST register **no** RNG stream; `_RESERVED_0x24_` / `SubsystemOrdinals.Scouting = 86` MUST remain RESERVED (not promoted) at approval; promotion to `DOMAIN_TAG_SCOUTING = 0x24` happens at the deep tier's first draw (spec-text-first). | MUST | KD-3 |
| FR-SC-014 | The serialized scouting block MUST contain no `RngCursor`/`actionOrdinal` field (keyed draws — the FR-TX-018 posture). | MUST | KD-3/KD-6 |
| FR-SC-015 | #32 state MUST persist as an opaque, independently version-gated `SCOUTING_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec`; the codec MUST NOT parse it; #32 MUST NOT bump `WORLD_STORE_FORMAT_VERSION`. | MUST | KD-6 |
| FR-SC-016 | The scouting sub-blob codec MUST fail loud (F3) on a `SCOUTING_SAVE_FORMAT_VERSION` mismatch, an out-of-bounds length prefix (overflow-safe `total − offset` bound), or trailing bytes — the `SeasonSaveCodec` posture. | MUST | KD-6/F3 |
| FR-SC-017 | Overlay entries MUST be serialized in **strictly ascending `PlayerId` order** (canonical order — the store is a map whose iteration order must never leak into bytes); decode MUST fail loud (F4) on non-ascending or duplicate ids. | MUST | KD-6 |
| FR-SC-018 | Knowledge MUST be durable career state: entries survive `RollToNextSeason` (no decay at Stage 3); genesis state is the **empty overlay** (nothing to seed); a load MUST reconstruct from the sub-blob and MUST NOT reset a band. | MUST | KD-6 |
| FR-SC-019 | On a roster **re-key** (#31 transfer) or **retirement/regen** (#28), the overlay entry for the affected `PlayerId` MUST be dropped (buy → own-squad rule covers it; sell → knowledge reset, a named Stage-3 simplification) **and an active assignment targeting the affected `PlayerId` MUST be cancelled** (in-band progress discarded — the `CancelAssignment` semantics; the manager-buys-the-scouted-player case would otherwise leave the assignment referencing a dead id and corrupt #32's own state at the next band-up); a view query for a `PlayerId` not resolvable in the pool MUST fail loud (F1) — never silent staleness. | MUST | KD-6 |
| FR-SC-020 | *(deep)* `AssignScout(playerId)` MUST be the sole initiator of scouting progress — an explicit manager command; it MUST fail loud on fog being off (F2 — with `fogEnabled = false` there is nothing to scout), an unknown/unresolvable `PlayerId` (F1), an own-squad id (F2), a busy slot (F2), or a fully-scouted target (`BandOf(playerId) == KNOWLEDGE_BAND_MAX`, F2). The UI MUST drive it through the command seam, never mutate #32 state directly. | MUST | KD-7 |
| FR-SC-021 | *(deep)* `CancelAssignment()` MUST discard in-band `DaysIntoBand` progress and keep completed bands; it MUST fail loud when no assignment is active (F2). | MUST | KD-7 |
| FR-SC-022 | *(deep)* Assignment progress MUST accrue only in `AdvanceScoutingDay` at #30's pre-declared tick-order slot (ERR-030-007 — new step 7, after staff, before `AdvanceDay` → step 8); the slot MUST be a documented null seam at minimal, and `AdvanceScoutingDay` MUST be a no-op while `fogEnabled` is off — a deep save's active assignment loaded into a fog-off config is **inert** (preserved for a fog-on resume, never silently dropped or advanced). | MUST | KD-7 |
| FR-SC-023 | *(deep)* `DaysPerBand = max(1, DAYS_PER_BAND_BASE · SCOUT_QUALITY_NEUTRAL_PERMILLE / quality)` (integer division), with `quality` read from #34's `ToScoutQuality` of the ChiefScout slot-holder; `quality ≤ 0` MUST fail loud (F5). Quality MUST scale assignment **speed only**, never estimate widths. | MUST | KD-4 |
| FR-SC-024 | #32 MUST define `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000` `[FIXED]` (closing #34 §3.1's "a baseline #32 will define"; value-compatible with #34's neutral `FacetPermille` row); #33 judgement MUST reach #32 only through #34's projection. | MUST | KD-4 |
| FR-SC-025 | The overlay/assignment scope is the **managed manager only** at Stage 3; AI clubs do not scout (their omniscient valuation, FR-TX-001, is unchanged); per-AI-manager overlays are far-deep. | MUST | KD-7 |
| FR-SC-026 | *(deep)* `RankByEstimate` MUST be a pure deterministic read-only ranking over caller-supplied `KnownPlayer`s (estimate midpoint; `PlayerId` ascending tiebreak); it MUST make no draw, mutate nothing, and store no shortlist. #32 MUST issue no offers (the manager acts via #31's `SubmitBid`) and MUST build no interface for #38/#46/#42/AI-manager consumers (FR-LW-031). #32 MUST generate no display text (structured reports only — the #49 localize-after-generate boundary). | MUST | KD-5 |
| FR-SC-027 | Every attribute/band/width/estimate/quality field MUST be integer; #32 MUST introduce **no** float. | MUST | KD-1/§1.5 |

## 2.2 Data structures

```csharp
// KD-1/KD-2 — the masked view. Readonly value types; derived on read; never stored; never written back.
public readonly struct AttributeEstimate       // [1,20] integer range; Min <= Max; truth ∈ [Min,Max] (FR-SC-003)
{ public int Min, Max; }                       // default(AttributeEstimate) = {0,0} ∉ [1,20] — the F6 zero-value trap

public readonly struct KnownPlayer             // the manager's view of one player (the #38 FR-UI-002 shape)
{
    public int PlayerId;                       // identity facts are PUBLIC — exact at any band (FR-SC-008):
    public string FirstName, LastName;         //   name/age/position never fogged (carried on the view so the
    public int Age;                            //   UI never needs a truth record for display identity)
    public PlayerPosition Position;
    public int WeakFootRating;                 //   [1,5] — exact at any band (identity-class; fogging it is a §7 extension)
    public int KnowledgeBand;                  // [0, KNOWLEDGE_BAND_MAX]
    /* 31 AttributeEstimate fields in AttrIdx order (indexer over them) */
}

// KD-1 — the overlay: band per scouted player; estimates DERIVED, not stored. Managed-manager scope (FR-SC-025).
// Serialization iterates entries in ascending PlayerId (canonical order, FR-SC-017).
public sealed class ScoutingState
{
    /* map PlayerId -> (int KnowledgeBand, uint LastReportWorldDay);   // only scouted players (band > 0)
       active assignment: (int PlayerId, uint DaysIntoBand) or none;   // deep; MAX_ACTIVE_ASSIGNMENTS = 1
       NO RngCursor (FR-SC-014) */
}

// KD-7 (deep) — the command + tick seams (§3.4). Purpose ordinals for the keyed draws (§3.3), APPEND-only.
public enum ScoutDrawPurpose : byte { Center = 0 /* deep may APPEND; never reorder */ }
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A view query or `AssignScout` for a `PlayerId` not resolvable in the caller-supplied pool (incl. a re-keyed/retired id whose entry was dropped) | **Fail loud** — an unresolvable id is a caller/hygiene bug; silently returning stale knowledge is the trap (FR-SC-019/020). |
| **F2** | `AssignScout` with fog off, on an own-squad id, on a fully-scouted target, or while a slot is busy; `CancelAssignment` with no active assignment | **Fail loud** — command-contract bugs (FR-SC-020/021, the #31 F4/F6 command-gate posture). |
| **F3** | Scouting sub-blob: bad `SCOUTING_SAVE_FORMAT_VERSION` / out-of-bounds length prefix / trailing bytes | **Fail loud** — the `SeasonSaveCodec` posture; no cross-version migration at Stage 0 (FR-SC-016). |
| **F4** | A decoded overlay entry with `KnowledgeBand ∉ [0, KNOWLEDGE_BAND_MAX]`, a non-ascending/duplicate `PlayerId`, or an assignment referencing a player with no resolvable record (the resolvability clause is enforced **post-load** at the composition root — the codec has no pool; Appendix B) | **Fail loud** — corrupt/incoherent overlay state (FR-SC-017, the #22 `RestoreLatched` strict-order precedent). |
| **F5** | *(deep)* `ToScoutQuality` result `≤ 0` reaching the `DaysPerBand` divisor | **Fail loud** — a malformed #34 projection is an upstream-contract bug, never clamped silently (FR-SC-023). |
| **F6** | `default(AttributeEstimate)`/`default(KnownPlayer)` (all-zero — `Min = Max = 0 ∉ [1,20]`) reaching a consuming seam | **Fail loud** — the zero-value-trap discriminator is the out-of-range estimate (the #41 `default(MedicalModifier)` / #34 F4 precedent). `PlayerId = 0` alone is NOT the trap (a real club-0/local-0 id). |

**Zero-value-trap discipline (F6):** `AttributeEstimate` has no all-zero neutral — a real estimate is
always in `[1,20]` — so `default` is invalid by design and caught at the consuming seam.
`KnowledgeBand = 0` **is** a legitimate value (unscouted), so the band is never the discriminator; the
estimate range is.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §2 (FR-SC-001..027, data structures, F1..F6), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Section-file AR PASS-1: **M-1** — FR-SC-020/022 + F2 pin the fog-off command semantics (`AssignScout` refused with fog off; `AdvanceScoutingDay` a no-op; a loaded assignment inert, preserved for a fog-on resume). **M-2** — the fully-scouted target added to FR-SC-020/F2 (was only in §3.4's pseudocode + T-SC-ASN-003). **L-1** — `KnownPlayer` gains `FirstName`/`LastName` (FR-SC-008 declares names public-exact; the UI must not need a truth record for display identity). |
| 0.3 | 2026-07-24 | — | Section-file AR PASS-2 (M, a regression interaction from PASS-1's M-1): FR-SC-007's byte-identity claim scoped to the **never-fogged career** — a fogged save loaded into a fog-off config legitimately carries preserved (inert) state, which the v0.2 wording ("the overlay is empty … no assignment exists") contradicted. |
| 0.4 | 2026-07-24 | — | Cross-set AR (all-3-specs pass): **M-1** — FR-SC-019 gains the active-assignment cancellation on a re-key/retirement of its target (the entry-drop alone left the assignment dangling — the manager-buys-the-scouted-player case corrupted #32's own state at the next band-up). **M-2** — FR-SC-002 scoped: no **view** consumer branches on the dial, which acts in exactly three named places (`ResolveBand`, the `AssignScout` gate, the `AdvanceScoutingDay` no-op) — the unscoped v0.3 claim contradicted the PASS-1 fog-off gates. **L** — the F4 resolvability clause annotated post-load (the codec has no pool). |
#endregion
