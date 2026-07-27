# Manager Career, Reputation & Job Market #54 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 2.1 Functional requirements

**Ownership & cadence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-001 | #54 MUST own the **whole** tenure lifecycle — the termination **decision**, its **consequences**, and appointment — in one spec. Splitting rule from aftermath is what produced the orphaned MUST §1.4(a) records. | MUST | KD-1 |
| FR-MC-002 | All #54 state MUST advance on the **world tick** or a **season boundary** — never the 10 Hz tactical or 60 Hz physics loops. No #54 type MUST be reachable from `MatchEngine.RunTick`. | MUST | KD-1 |
| FR-MC-003 | #54's assembly MUST reference **nothing**, at every tier. Board confidence, objective outcomes, and club values MUST arrive as **integers supplied by the root**. | MUST | KD-1 |
| FR-MC-004 | #54 MUST NOT write into #45's store, MUST NOT expose an API that does, and MUST NOT mutate any #30, #40, #53 or #27 state. | MUST | KD-4 |
| FR-MC-005 | All #54 state and formulas MUST be **integer**. No float MUST appear at any tier. | MUST | KD-7 |
| FR-MC-006 | #54 MUST be the **sole writer** of the career record and tenure state. | MUST | KD-7 |
| FR-MC-007 | #54 MUST NOT declare a type named `ManagerProfile` or `ManagerMode` — **#26 already ships both**, for in-match tactical adaptation, and they mean something else entirely. | MUST | §1.4(c) |

**Tenure and termination**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-008 | `EvaluateTenure(confidencePermille, objectiveOutcome, worldDay) → Continue \| Terminate` MUST be a **pure rule**: no mutation, no draw, no dependence on call order, and identical inputs MUST always yield the identical verdict. | MUST | KD-1/KD-6 |
| FR-MC-009 | Evaluation MUST **not** mutate #45's state. #54 is a **reader** — the direction `FR-BD-012` protects. | MUST | KD-1 |
| FR-MC-010 | A termination MUST leave the career **continuing and unemployed** — never ended. The manager holds no club until appointed. | MUST | KD-4 |
| FR-MC-011 | A completed tenure MUST be **APPEND-only**: never rewritten, reordered, or removed by a later appointment or termination. | MUST | KD-2/KD-7 |

**Reputation**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-012 | `ReputationOf(manager)` MUST be a **projection computed on read** from the career record. | MUST | KD-2 |
| FR-MC-013 | #54 MUST NOT store a reputation field, in the career block or anywhere else, **at any tier** — including as a cache. A stored scalar beside a stored history is two truths that diverge at the first restore with nothing to detect it (`ERR-030-009`). | MUST | KD-2 |
| FR-MC-014 | The projection MUST be **pure** and MUST depend only on the record — never on the current club, the current confidence, or the world day. | MUST | KD-2 |
| FR-MC-015 | `EndReason` MUST carry an **ORDINAL STABILITY — APPEND-only** contract: the ordinal is serialized in the career block and weights the reputation projection, so a reorder silently re-reads every historical tenure **and** changes every historical reputation. | MUST | KD-7 |

**Appointment (the mirror case)**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-016 | `Appoint(manager, clubId, worldDay)` MUST record the tenure **only**. The paired factory-built `{BoardConfidence, OwnershipProfile}` insertion MUST be performed by the **command layer**, never by #54 (FR-MC-004). | MUST | KD-4 |
| FR-MC-017 | An appointment MUST initialise the club's board confidence to the **factory honeymoon value** — never `default(BoardConfidence)`, and never the predecessor's standing. Inheriting a crisis is defensible as a design but MUST be a **chosen** one. | MUST | KD-4 |
| FR-MC-018 | `Appoint` MUST fail loud if the manager already holds an open tenure — a manager holds at most one club, and a silent second open tenure would make `CurrentTenure` ambiguous and the career record un-reconstructable. | MUST | KD-7 |
| FR-MC-019 | `Terminate` MUST fail loud if the manager holds **no** open tenure. | MUST | KD-7 |

**Vacancies and the job market**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-020 | A vacancy MUST be a property of a **club**, derived from root-supplied club values. #54 MUST NOT model, store, or expose a **rival manager** entity, at any tier described here. | MUST | KD-3 |
| FR-MC-021 | #54 MUST NOT emit any event or view implying a rival manager was appointed or dismissed, and MUST NOT let the player observe a rival's tenure. | MUST | KD-3 |
| FR-MC-022 | `Attractiveness` MUST be a **read-only projection** over root-supplied values. #54 MUST reference none of #40, #53 or #27. | MUST | KD-3 |
| FR-MC-023 | When #22's phase-5 `BackgroundTierSim` lands, the vacancy **source** MUST be replaceable behind an unchanged #54 surface — a producer swap, not a redesign. | SHOULD | KD-3 |

**Determinism**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-024 | The minimal tier MUST be **draw-free**: no `RegisterStream`, no domain-tag promotion. | MUST | KD-6 |
| FR-MC-025 | #54's promotion MUST add a **`_RESERVED_0x2E_`** placeholder row (ordinal 96) to #16 §3.4 — **reserved, not a named tag** (the #40 `_RESERVED_0x29_` / #29 `0x21` precedent). | MUST | KD-6 |
| FR-MC-026 | Any S3 job-market draw MUST use **one** subsystem-wide stream with **keyed, position-independent** action ordinals — never one per club or per vacancy (the shared `MaxRngStreams = 64` bound). | MUST | KD-6 |
| FR-MC-027 | A refused appointment, termination or evaluation MUST mutate **nothing**. | MUST | KD-7 |

**Persistence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-MC-028 | `CAREER_SAVE_FORMAT_VERSION` [FIXED] = 1; the career MUST land as an **opaque, independently version-gated** sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump and **not** a `SeasonState` field. | MUST | KD-7 |
| FR-MC-029 | The career block MUST **outlive the season**: #30's season state is replaced at each boundary roll, and a tenure spans them. | MUST | KD-7 |
| FR-MC-030 | Every field MUST round-trip **field-identical**; **serialize, don't regenerate**. There is no RNG cursor to serialize at the minimal tier. | MUST | KD-7 |
| FR-MC-031 | Restore MUST **fail loud** on version mismatch, an out-of-bounds length prefix (overflow-safe bound against `total − offset`), trailing bytes, an undefined `EndReason`, a tenure whose `endWorldDay < startWorldDay`, more than one open tenure, or a `CurrentTenure` index that does not point at an open tenure. | MUST | KD-7 |
| FR-MC-032 | The layout MUST be **APPEND-only**. | MUST | KD-7 |
| FR-MC-033 | #54 MUST NOT require a `SeasonState` field of its own. The one #30 change it needs is `ManagedClubId` becoming an **explicit optional** (ERR-030-021). | MUST | KD-5 |
| FR-MC-034 | The unemployed representation MUST be an **explicit optional**, never a sentinel value in an `int` field — so every read site is **forced by the type** to state its behaviour rather than crashing only for a save the corpus cannot construct. | MUST | KD-5 |

## 2.2 Data structures

```csharp
// APPEND-only, NEVER reordered (FR-MC-015). The ordinal is BOTH serialized in the career block
// AND a weight input to the reputation projection -- so a reorder re-reads every historical
// tenure AND changes every historical reputation, with no version gate to catch either.
public enum EndReason : byte
{
    Open = 0,          // the tenure has not ended; endWorldDay is meaningless
    Sacked,            // terminated by the board rule (FM-MC-01)
    Resigned,          // the manager left
    ContractExpired,
    MutualConsent,
}

// One completed or open tenure. APPEND-only history (FR-MC-011).
public struct Tenure
{
    public int       ClubId;
    public uint      StartWorldDay;
    public uint      EndWorldDay;        // meaningful only when Reason != Open
    public EndReason Reason;
    public int       SeasonsServed;
    public int[]     Finishes;           // final league position per season served, in order
    public int[]     Trophies;           // competition ids won, ascending
}

// The career. Outlives the season (FR-MC-029). NOTE what is ABSENT: no Reputation field,
// at any tier, including as a cache (FR-MC-013) -- its presence here WOULD BE the second truth.
public struct ManagerCareer
{
    public int      ManagerId;
    public Tenure[] Tenures;             // APPEND-only, chronological
    public int      CurrentTenure;       // index into Tenures, or MC_UNEMPLOYED (-1)
}

// The committed-values input the root routes IN (the HumanSystemsDayInput / BoardDayInput posture)
// -- integers only, so #54 names no #45 and no #30 type.
public readonly struct TenureEvaluationInput
{
    public readonly int ConfidencePermille;    // [0,1000], #45's committed value (FR-MC-009: READ only)
    public readonly int ObjectiveOutcome;      // #30's committed grade; MC_OBJECTIVE_NEUTRAL == on track
    public readonly int SeasonsServed;         // from the open tenure
}

// Read-only projection over root-supplied club values (FR-MC-022). #54 references no producer.
public readonly struct VacancyInput
{
    public readonly int ClubId;
    public readonly int LeaguePositionPermille;   // relative standing
    public readonly int FinanceHealthPermille;    // #40, routed
    public readonly int FacilityLevelPermille;    // #53, routed
    public readonly int SquadStrengthPermille;    // #27, routed
}

public enum TenureVerdict : byte { Continue = 0, Terminate }
```

**Types #54 consumes but does not declare:**

| Type | Owner | #54's use |
|---|---|---|
| `BoardConfidence`, `OwnershipProfile`, `JobSecurityBand` | #45 | **Never named.** Confidence arrives as a bare `int`; the factory-pair insertion is the **command layer's** call (FR-MC-016). |
| `SeasonState`, `RoundResolutionMode` | #30 | **Never named.** ERR-030-021's optional `ManagedClubId` is a #30-side change. |
| `ManagerProfile`, `ManagerMode` | **#26** | **Never named and never shadowed** (FR-MC-007) — a different "manager" entirely. |
| `ClubId` | #27 | An `int` in #54's own state; #54 does not reference `PlayerDatabase`. |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | `Appoint` while the manager already holds an **open** tenure. | **Fail loud** (FR-MC-018). Two open tenures make `CurrentTenure` ambiguous and the career un-reconstructable — and it would decode cleanly, so nothing downstream would catch it. |
| **F2** | `Terminate` while the manager holds **no** open tenure. | **Fail loud** (FR-MC-019). |
| **F3** | An out-of-range `ConfidencePermille` or `ObjectiveOutcome` at the evaluation seam. | **Fail loud** — never clamped silently. A corrupt routed value must not silently produce a verdict. |
| **F4** | An **undefined `EndReason`**, or `EndReason.Open` on a tenure that also carries an `EndWorldDay`, at a write seam or on decode. | **Fail loud**. `Open` is a state, not an ending; the pair must be coherent or the record is uninterpretable. |
| **F5** | A tenure with `EndWorldDay < StartWorldDay`, or a `Finishes` array longer than `SeasonsServed`. | **Fail loud** at both write and decode — an incoherent tenure would silently corrupt every reputation projection over it. |
| **F6** | A `CurrentTenure` index that is out of range, or that points at a tenure whose `Reason != Open`. | **Fail loud** on decode. This is the invariant that makes `MC_UNEMPLOYED` meaningful: unemployment is `CurrentTenure == -1`, **not** "the last tenure happens to be closed". |
| **F7** | An attempt to write a reputation value into the career block, or to construct a `ManagerCareer` carrying one. | **Structurally impossible** — the field does not exist (FR-MC-013), and §5.2 asserts its absence over the type. Listed as a failure mode because the *pressure* to add it is real and this is where a reader looks for the answer. |
| **F8** | A #54 call that would write into #45's store, or an `Appoint` that default-constructs a `BoardConfidence`. | **Barred by construction** — #54 references nothing (FR-MC-003) — and asserted behaviourally (§5.6), because the *command layer* can still get it wrong and #45's own insertion guard would then throw on an ordinary career action. |
| **F9** | Bad `CAREER_SAVE_FORMAT_VERSION`, an out-of-bounds length prefix, or trailing bytes on restore. | **Fail loud** — version gate read **first**; the bound compared against `total − offset`, never `offset + need`, which can wrap negative on a crafted near-`int.MaxValue` prefix. |

**Deliberately not a failure mode: being unemployed.** `CurrentTenure == MC_UNEMPLOYED` is the **normal**
post-termination state (FR-MC-010), and every #54 surface must behave sensibly in it — `ReputationOf`
still projects, the career still round-trips, the season still advances. It is listed by its absence
because *"no current club"* looks like an error state and is the whole point of the spec.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-MC-001..034, data structures, F1..F9) from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-MC-015 / F4** — `EndReason`'s ordinal is load-bearing **twice** (serialized *and* a reputation weight), so a reorder re-reads every historical tenure **and** changes every historical reputation, with no version gate for either; the supplement mentioned "ordinal stability on `endReason`" only in its test list. **M:** added **F6** — nothing pinned that unemployment is `CurrentTenure == -1` rather than *"the last tenure happens to be closed"*, leaving two representations of one state, of which only one is checkable. **M:** added **FR-MC-018/019 / F1/F2** — the supplement specified neither guard, and a second open tenure **decodes cleanly** while making the record un-reconstructable. **L:** added **FR-MC-014** (the projection depends only on the record — not on current confidence, which would make reputation move without the record changing), **F5** (incoherent tenure bounds), **F7** as an explicitly-structural non-failure, and the *"being unemployed is not a failure mode"* note; `TenureEvaluationInput`, `VacancyInput` and `EndReason` written out. |
#endregion
