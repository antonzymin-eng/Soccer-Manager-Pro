# Match Presentation Depth #48 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 2.1 Functional requirements

**Observation-only (KD-1)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-001 | #48 MUST write nothing into the simulation: no event emitted, no tick advanced, no sim state held or mutated. | MUST | KD-1 |
| FR-MP-002 | #48 MUST read only (i) the public observation surface's **value copies** and (ii) the **live per-tick event tap**. | MUST | KD-1 |
| FR-MP-003 | **No sim assembly MUST reference #48** — asserted by the mechanical `.asmdef` reverse-reference scan #38's T0 already ships (FR-UI-001), extended to #48. | MUST | KD-1 |
| FR-MP-004 | #48's assembly MUST NOT reference **`match-client-core`**, whose `ILiveMatchMutations` and `ManagerCommandQueue` are a genuine mutation surface in the same layer. A presentation surface that gains a mutation channel **stops being presentation**. | MUST | KD-1 |
| FR-MP-005 | #48's assembly MUST NOT reference `TacticalDirector.Localization` (FR-LC-012), `living-world`, `#51`, `SeasonSave`, or any management spec. | MUST | KD-2/KD-4 |
| FR-MP-006 | Playback controls (pause, speed, scrub) MUST belong to the client shell, never to #48. | MUST | KD-1 |

**The shared tap (KD-2(i))**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-007 | #48 MUST consume the **#37-specified** live per-tick ledger tap (FR-AN-002) as a **third consumer**, alongside #37 and #44. It MUST NOT build a second tap. | MUST | KD-2 |
| FR-MP-008 | The tap is **#37-owned**: whichever of #37 / #44 / #48 is implemented first **builds it to #37's contract**, and the others **join**. If #48 lands first, the tap it builds is **#37's surface, not a #48 one**. | MUST | KD-2 |
| FR-MP-009 | #48 MUST NOT re-parse the serialized ledger. `SerializeLedger` is write-only, and FR-AN-021 states the bytes MUST NOT be assumed re-parseable. | MUST | KD-2 |

**Commentary (KD-2)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-010 | Event-driven commentary MUST be **captured live during the tick loop** into a session transcript of `{tick, intent, slots}` records. The replay path MUST **replay the transcript**, never re-derive it. | MUST | KD-2 |
| FR-MP-011 | The transcript MUST hold **native values only** — no rendered string, and no locale identifier. | MUST | KD-2 |
| FR-MP-012 | The FR-LC-004 `ulong` MUST be a **local keyed SplitMix64 mix** over `(tick, intentOrdinal, subjectAgentId)`. #48 MUST register **no** RNG stream, touch **no** cursor, and serialize **nothing**. `world.text` MUST NOT be consumed. | MUST | KD-2 |
| FR-MP-013 | `tick` MUST be part of the selection key. Without it, every occurrence of one intent for one agent would select the same variant for the whole match. | MUST | KD-2 |
| FR-MP-014 | `CommentaryIntent` MUST carry an **ORDINAL STABILITY — APPEND-only** contract: the ordinal is the `LocalOrdinal` half of the `TextTemplateId` the #49 catalogue is keyed on, **and** it is embedded in exported artifacts. | MUST | KD-2 |
| FR-MP-015 | #48 MUST apply the FR-LC-015 **intent-value pre-gate** and MUST carry an **FR-LC-008a coverage assertion** over its full `CommentaryIntent` roster. | MUST | KD-2 |
| FR-MP-016 | #48 MUST NOT emit a baked, human-readable localized string (FR-LC-002), through any surface. | MUST | KD-2 |
| FR-MP-017 | The **exported HTML artifact** MUST **embed rendered commentary text**, baked by the **exporter at the boundary layer** — never by #48. A file cannot re-derive lines it did not carry (FR-MP-010). | MUST | KD-2 |
| FR-MP-018 | #48 MUST supply its `SelectionDraw` per FR-MP-012, **inheriting** #35's `ERR-049-001` dependency. If that fix is refused, #48 MUST take the same `SelectionDraw = 0` fallback and MUST NOT file a duplicate #49 back-prop. | MUST | KD-2 |

**Animation (KD-3)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-019 | Animation and render state MUST be **derived** from the observation surface's position history plus #48's own state machine. #48 MUST NOT require a new match-engine field. | MUST | KD-3 |
| FR-MP-020 | If a future fidelity genuinely needs a sim-side fact, it MUST be an **additive read-only property on match-engine**, in the `BallView` / `AgentView` class — **never** a presentation-side push and **never** a new serialized field. | MUST | KD-3 |
| FR-MP-021 | Any such addition MUST (a) state **why the value cannot be derived** from the observation history, and (b) pass the existing observer-neutrality digest lock **unchanged**. | MUST | KD-3 |
| FR-MP-022 | #48's contract — trigger mapping and animation **state** — MUST be authorable and testable **without Unity host access**. Only the renderer is host-gated. | MUST | KD-3 |

**Audio cue selection (KD-4)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-023 | #48 MUST map an observed event to a `CueId` plus parameters, off the **same** live tap, and MUST stop there. Playback, mixer, buses and the cue **catalogue** are #51's. | MUST | KD-4 |
| FR-MP-024 | #48 MUST declare `ICueSink` **itself** and emit into it. #48 MUST NOT call a playback API directly, so #51's arrival is a **sink-implementation change** rather than a rewrite. | MUST | KD-4 |
| FR-MP-025 | **#51 MUST NOT implement `ICueSink` and MUST NOT reference #48.** The **client shell** supplies the adapter. A Wave-8 spec must not become a Wave-7 dependency. | MUST | KD-4 |
| FR-MP-026 | `ICueSink` MUST have a **no-op default**, so a headless run is valid forever. | MUST | KD-4 |
| FR-MP-027 | `CueId` MUST carry the same **APPEND-only ordinal stability** as the text intents — #51's catalogue will be keyed on it. | MUST | KD-4 |

**Composition and the thread boundary (KD-5)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-028 | #48 MUST expose **immutable value-type** view models through #38's `IViewModelSource<T>` contract (`where T : struct`). | MUST | KD-5 |
| FR-MP-029 | `CommentaryFeedView` MUST be a **bounded window** — a fixed-capacity struct carrying the last `COMMENTARY_WINDOW_LINES` entries **by value** — never a handle or alias to the transcript. The full transcript stays inside #48. | MUST | KD-5 |
| FR-MP-030 | The window MUST be produced by **snapshot-copy at the thread boundary**. `CommentaryRecorder.OnTick` runs on the streamer's **tick thread**; #38 renders on the UI thread, and MUST NOT read the live transcript. | MUST | KD-5 |
| FR-MP-031 | #48 MUST own no navigation, layout or input. | MUST | KD-5 |

**Determinism and state (KD-6 / KD-7)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MP-032 | #48 MUST hold **no persistent game state**, MUST bump **no format version**, and MUST add **no save sub-blob**. | MUST | KD-6 |
| FR-MP-033 | #48 MUST be **draw-free at every tier**: no stream, no domain tag, no `SubsystemOrdinal`, and **no `_RESERVED_` placeholder** — #16 is untouched, and #48 has **nothing to promote later**. | MUST | KD-6 |
| FR-MP-034 | **Observer neutrality MUST be unconditional**: a match run with commentary, animation and cue mapping **all enabled** MUST produce a digest chain **byte-identical** to an unobserved same-seed run. | MUST | KD-7 |

## 2.2 Data structures

```csharp
// APPEND-only, NEVER reordered (FR-MP-014): the ordinal is the LocalOrdinal half of the #49
// TextTemplateId AND is embedded in exported HTML artifacts, so a reorder re-points every
// catalogue row AND mis-labels every existing export -- neither with a version gate.
public enum CommentaryIntent : int { None = 0, /* goal, card, save, chance, … */ }

// APPEND-only for the weaker but real reason that #51's catalogue will be keyed on it (FR-MP-027).
public enum CueId : int { None = 0, /* crowd, whistle, impact, … */ }

// One captured line. NATIVE VALUES ONLY -- no rendered string, no locale (FR-MP-011).
public readonly struct CommentaryLine
{
    public readonly int              Tick;
    public readonly CommentaryIntent Intent;
    public readonly CommentarySlots  Slots;
}

// #48's own slots, DISJOINT from #22's / #35's / #46's (FR-LC-014). Native values.
public readonly struct CommentarySlots
{
    public readonly int SubjectAgentId;      // MP_NO_SUBJECT (-1) when absent; NOT 0 (a valid agent id)
    public readonly int SecondaryAgentId;    // MP_NO_SUBJECT when absent
    public readonly int HomeScore, AwayScore, Minute;
}

// The view model #38 renders. A BOUNDED WINDOW BY VALUE, not a handle (FR-MP-029) --
// IViewModelSource<T> is `where T : struct`, and a growing list behind a struct is either a
// per-frame allocation or a live alias (the SquadPositionCounts / MatchReplay defect class).
public readonly struct CommentaryFeedView
{
    public readonly int Count;                                   // <= COMMENTARY_WINDOW_LINES
    // fixed-capacity inline storage of the last Count lines, snapshot-copied at the boundary
}

// Derived from the observation surface's position history -- NO new engine field (FR-MP-019).
public readonly struct AnimationFrameView
{
    public readonly int Tick;
    // per-agent derived pose/gait state, by value; fixed capacity over SQUAD_SIZE
}

// #48 DECLARES this; the client SHELL implements it against #51 (FR-MP-025).
public interface ICueSink { void Emit(CueId cue, in CueParams p); }   // default impl: no-op
```

**Types #48 consumes but does not declare:**

| Type | Owner | #48's use |
|---|---|---|
| `BallView`, `AgentView`, `PossessingAgentId`, `HomeScore` / `AwayScore`, `MatchEnded` | match-engine | **value copies**, read-only (FR-MP-002) |
| The per-tick event tap's record type | **#37** (FR-AN-002) | joined as a third consumer, **never redefined** (FR-MP-007/008) |
| `TextTemplateId`, `LocalizedTextRequest`, `ILocalizer` | #49 | used **only inside `MatchTextBoundary`**, which is not a #48 assembly |
| `LiveMatchFrame` | match-viewer | frame types, as a sibling in the same layer |
| `ILiveMatchMutations`, `ManagerCommandQueue` | match-client-core | **never referenced** (FR-MP-004) — listed so the exclusion is deliberate |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | `CommentaryIntent.None` or an undefined ordinal reaching the render path. | **Fail loud** at the **pre-selection** gate (FR-MP-015), before any selection work — so a refused item consumes nothing. |
| **F2** | A `CueId.None` or undefined ordinal reaching `ICueSink.Emit`. | **Fail loud** at the mapper, not at the sink — the sink may be a no-op, so a guard living only there would be silently absent in a headless run. |
| **F3** | Malformed observation or tap input — a non-finite position, an out-of-range agent id. | **Fail loud** at the boundary. #48 must not silently render a nonsense frame, and it must not sanitise sim data, which would hide a sim defect behind presentation. |
| **F4** | An attempt to render from the **live transcript** on the UI thread rather than from a snapshot-copied window. | **Barred by construction** — `CommentaryFeedView` carries values, not a handle (FR-MP-029) — and asserted behaviourally (§5.5), because the *boundary* code can still get it wrong. |
| **F5** | An exported artifact containing **no** commentary for a match that produced lines. | **A defect in the exporter**, caught by §5.4's export-equivalence lock. It is silent at runtime — the file simply has none — which is why it is a test rather than a guard. |
| **F6** | A second live tap constructed by #48. | **Barred by FR-MP-007** and asserted structurally. A parallel tap would double-read one ledger with two lifetimes and two sets of ordering assumptions. |
| **F7** | Any #48 write into the simulation, or a `match-client-core` reference. | **Barred structurally** (FR-MP-001/004) and asserted by the reverse-reference scan plus an explicit reference-absence test. |

**Deliberately not a failure mode: an empty transcript.** A match in which no commentary-worthy event
occurred yields an empty feed, and every #48 surface must behave correctly in it — the window is empty,
the export carries no lines, and nothing throws. Stated because "no lines" looks like a failure and is the
minimal tier's normal state.

**Deliberately not a failure mode: a no-op `ICueSink`.** It is the **default** (FR-MP-026), and a headless
or pre-#51 run is expected to use it forever.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-MP-001..034, data structures, F1..F5) from supplement v0.6. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **F2** with the guard placed at the **mapper**, not the sink — the sink's default is a no-op, so a `CueId` validity check living only there would be **silently absent in a headless run**, which is the default configuration. **M:** added **F3** — nothing said what #48 does with malformed observation input, and the two wrong answers are both natural: render a nonsense frame, or *sanitise* it and thereby hide a sim defect behind presentation. **M:** added **FR-MP-013** making `tick`'s presence in the selection key a requirement rather than a parenthetical; without it every occurrence of one intent for one agent picks the same variant for the whole match, which is the most visible possible regression in a commentary system. **L:** added F6/F7 (the second-tap and mutation-channel bars, as failure modes rather than only as FRs), the two *"not a failure mode"* notes, and wrote out `CommentaryLine`, `CommentarySlots`, `CommentaryFeedView`, `AnimationFrameView` and `ICueSink`, each annotated with the constraint that shapes it. |
#endregion
