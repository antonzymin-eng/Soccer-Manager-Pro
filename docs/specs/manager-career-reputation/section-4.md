# Manager Career, Reputation & Job Market #54 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.ManagerCareer`** at `src/manager-career/`, referencing **nothing** — at
**every** tier.

```
root ──▶ {#54, #30, #45, #40, #53, #27}

  #54 ──▶ { }        #45 ──▶ { }        (both leaves)

  root: TenureEvaluationInput  ← {#45 confidence, #30 objective outcome}  → #54
  root: VacancyInput           ← {#40, #53, #27}                          → #54
  root: on appoint             → #45.Insert(factory pair) THEN #54.Appoint(...)
```

**#54 is a leaf, and the fact that it does not reference #45 matters more than usual.** The natural
implementation of *"read board confidence to decide a sacking"* is a direct reference — and that would put
a Wave-6 spec **inside #45's one-directional guarantee**, the property `FR-BD-012` exists to preserve.
Routing confidence in as an integer keeps #45's posture exactly as approved while giving #54 the input it
needs.

The wave's established inversion applies unchanged: #48's cue sink, #50's registry, #51's mapping, #53's
projections, #46's projectors — every one of them a leaf fed by the root.

**CS0104 pre-check — and this one is not hypothetical.** #26 (Tactical Presets) **already ships
`ManagerProfile` and `ManagerMode.Human`** in `src/match-engine`, for **in-match tactical adaptation**. It
is a different "manager" entirely: #26's is a per-team AI personality that adjusts mentality during a
match; #54's is the human's career. Reusing either name would be the `TacticTranslation` /
`PlayerAttributes` CS0104 class this project has hit twice — **the third instance, foreseen** rather than
discovered at compile time.

#54 therefore introduces `ManagerCareer`, `Tenure`, `TenureState`, `EndReason`, `TenureVerdict`,
`TenureEvaluationInput`, `VacancyInput`, `CareerStore`, `CareerSaveCodec`, `ReputationProjection` — each
checked against every name that could be in scope with it (FR-MC-007).

## 4.2 File layout

```
src/manager-career/
├── ManagerCareerConstants.cs    # the Appendix A catalogue — no magic numbers in formula code
├── EndReason.cs                 # APPEND-only; serialized AND a reputation weight index (FR-MC-015)
├── Tenure.cs                    # one open or closed tenure
├── ManagerCareer.cs             # the APPEND-only history + CurrentTenure (NO reputation field)
├── TenureEvaluationInput.cs     # committed integers routed in — names no #45/#30 type
├── VacancyInput.cs              # committed club values routed in — names no #40/#53/#27 type
├── TenureRule.cs                # FM-MC-01 — the pure evaluation rule
├── CareerStore.cs               # FM-MC-02/03 — Appoint / Terminate; the SINGLE writer
├── ReputationProjection.cs      # FM-MC-04 — computed on read, stored nowhere
├── VacancyProjection.cs         # FM-MC-05 — attractiveness over routed values
├── CareerSaveCodec.cs           # KD-7 sub-blob, version gate first
└── tests/
```

**`ReputationProjection.cs` is a separate file with no state, deliberately.** Co-locating it with
`ManagerCareer.cs` is the shortest path to someone adding a `_cachedReputation` field beside the record it
projects over. Keeping the projection stateless and elsewhere makes the cache an obvious intrusion rather
than a local optimisation (FR-MC-013).

**No job-market draw file exists at the minimal tier.** The S3 interest draw is specified (§3.5) and not
built; a stream with no draw site is the phantom surface FR-LW-031 forbids.

**The command-layer appointment join is absent from this tree**, and must be: it references **both** #54
and #45 (§4.4).

## 4.3 The #30 seam (the caller side)

#30 owns the call; #54 owns the rule and its consequences.

```
# inside #30, at the tenure slot (ERR-030-021)
var verdict = career.EvaluateTenure(
    new TenureEvaluationInput(
        confidencePermille: board.ConfidenceOf(managedClubId),   # #45, read-only, routed as an int
        objectiveOutcome:   season.ObjectiveOutcome(),           # #30's own committed grade
        seasonsServed:      career.CurrentSeasonsServed()),
    worldDay);

if (verdict == TenureVerdict.Terminate)
    career.Terminate(EndReason.Sacked, worldDay);                # #54 owns the consequence too
```

**#30 gains a seam, not a mechanic** (KD-1) — the same relationship it has to #40's `SettleFinances` and
#41's `AdvanceMedicalDay`. The rule, the verdict and the aftermath all stay in #54, which is the whole
point: splitting them is what produced the orphaned MUST.

**Provenance is enforced at #30's call seam**, not inside #54. #54 cannot verify that
`confidencePermille` really came from #45's store for the managed club — only that it is in range. Same
division of responsibility as #33's committed inputs, and what keeps #54 free of a #45 reference.

**The slot's position is filed at approval, not deferred**, because #30's tick/boundary order is a pinned
sequence cited **by number** in several approved specs — the `ERR-030-008` / `ERR-030-020` precedent. A
step whose position is decided later is a step whose ordering was never reviewed.

## 4.4 The command-layer appointment join (KD-4)

The one sequence that spans #54 and #45, and which **neither** may perform alone:

```
# root-side — the root already references both
OnAppointCommand(clubId, worldDay):
    if (!board.HasEntry(clubId))
        board.Insert(clubId,
                     BoardConfidence.Create(),        # the FACTORY honeymoon value — never default()
                     OwnershipProfile.Identity);      # #45 FR-BD-005a: a GUARDED, FACTORY-BUILT PAIR
    career.Appoint(clubId, worldDay);                 # #54 records the tenure ONLY
```

**Why #54 must not do the insertion.** It would be a **write into #45's store**, which breaks two things
at once: #54's leaf position (FR-MC-003) and #45's one-directional guarantee. The two-spec join belongs to
the layer that already sees both — the same shape KD-1 uses for the evaluation and #53 uses for a
purchase.

**Why the factory value and not `default`.** #45's `FR-BD-005a` requires the pair factory-built and
guarded **at insertion**, because `default(BoardConfidence)` is *field-in-range yet semantically severe*:
confidence `0` is the `Critical` band — *"dismissal imminent"* — with a `LastAdvancedWorldDay = 0` that
no-ops the day-0 guard. A default-constructed appointment hands the manager a **new job in crisis on day
one**, and #45's guard throws — a crash on an ordinary career action.

**Why not the predecessor's standing.** Confidence is the board's view of the **current** manager, and the
new one has no record at that club yet. Inheriting a crisis is a defensible *design*; it must be a
**chosen** one, not the accident of reusing whatever was in the store.

**ERR-045-002 asks #45 to confirm the mid-career case.** If #45's store is populated for every club at
world genesis, the `HasEntry` branch is a no-op and the back-prop records that instead — but the
assumption is load-bearing for this seam, so it is asked rather than assumed.

## 4.5 The unemployed representation (KD-5)

`SeasonState.ManagedClubId` is validated against the club set, so **an unemployed manager is currently
unrepresentable** (§1.4(b)). ERR-030-021 makes it an **explicit optional**:

```
# #30-side, after ERR-030-021
public int? ManagedClubId;        // 'none' == the human manages nobody
```

**An explicit optional rather than a sentinel, and the reason is about failure surfaces.** `ManagedClubId`
is read at many sites that legitimately assume a club — fixture routing, the engine-vs-model decision, the
table view. A `-1` sentinel makes **every one of them a latent crash that only fires for an unemployed
save**, a state the entire test corpus currently cannot construct. An optional makes the **compiler**
enumerate the work.

**What it unlocks is a capability #30 already has.** With no managed club, every fixture resolves through
`RoundResolutionMode`'s model rather than the engine — that path exists and is tested. What did not exist
is a season state that can *express* the configuration.

**It is a `SEASON_STATE_FORMAT_VERSION` bump, and #45's ERR-030-009 already queues one on the same block.**
The recommendation is to **land both in one bump** if the tiers align: one version step, one #50 registry
row, **one refusal boundary for existing saves** instead of two. §7.4 R-2 carries the risk if they cannot.

## 4.6 Save composition (KD-7)

#54's career block is composed into #30's `SeasonSaveCodec` alongside #40's, #45's, #44's and the rest, as
a length-prefixed **opaque** block: the outer codec never parses it, so `CAREER_SAVE_FORMAT_VERSION` and
`SEASON_SAVE_FORMAT_VERSION` move independently (FR-MC-028). Layout in Appendix B.

**The career outlives the season, which is the one structural difference from every neighbour.** #30's
season state is **replaced** at each boundary roll; a tenure **spans** them. That is why the career is its
own block rather than a `SeasonState` field (FR-MC-029/033) — and it is what makes it meaningful across
the multi-season careers Stage 5 assumes.

**Three versions are in play at #54's landing, and only two are #54's business:**
`CAREER_SAVE_FORMAT_VERSION` (#54's own), the outer `SEASON_SAVE_FORMAT_VERSION` (bumped at T2 when the
block is composed in), and #30's `SEASON_STATE_FORMAT_VERSION` (bumped by ERR-030-021's optional
`ManagedClubId` — **#30-owned**, and the one non-additive consequence of #54's approval).

**Migration posture: none — pre-bump saves are rejected fail-loud.** The living-world slice-2 precedent;
cross-version migration is **#50's** subject, and recording the position here means #50 inherits it.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#45** | **No reference in either direction.** Confidence arrives as a routed integer; #45's `FR-BD-012` posture is preserved *exactly* — it exposes no sacking API and fires no terminating event. **Only the sentence naming its counterparty changes** (ERR-045-002). The factory-pair insertion on appointment is the **command layer's** call, never #54's. |
| **#30** | Invokes `EvaluateTenure` at the pinned tenure slot and routes the objective outcome. Gains an **explicit optional** `ManagedClubId` (ERR-030-021). #54 references #30 never. |
| **#40 / #53 / #27** | Vacancy attractiveness reads **root-supplied values**. **No spec changes and no references** — the value-input pattern #42/#29/#53 already use. |
| **#22** | Phase-5 `BackgroundTierSim` is the **deep-tier producer** of rival-manager outcomes. #54 does not build it, does not model rival managers, and its vacancy source is **replaceable behind an unchanged surface** when phase-5 lands. |
| **#26** | **A different "manager" entirely.** #54 avoids `ManagerProfile` / `ManagerMode` rather than amending #26 (FR-MC-007). |
| **#33** | A manager is **not** a player record. Manager personality is an S5 extension, and would arrive as routed values. |
| **#16** | #54's promotion adds `_RESERVED_0x2E_` / ordinal 96 as a **placeholder** — reserved, **not** a named tag (FR-MC-025). |
| **#50** | Registers `CAREER_SAVE_FORMAT_VERSION`; inherits the stated no-migration posture (§4.6). |
| **#38** | Reads value copies of tenure, career, reputation and vacancies. |

**Standing review item:** #54 performs **no** write to #45, #30, #40, #53 or #27 state. #54 references
nothing, so its own graph proves it — but the **command-layer join** (§4.4) holds both sides, and a join
that default-constructed a `BoardConfidence` or inserted the predecessor's standing would be invisible to
#54's tests. §5.6 asserts the appointment path behaviourally for exactly that reason.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (leaf assembly with the #45-reference trap named, the **foreseen** third CS0104 instance against #26's `ManagerProfile`, file layout with `ReputationProjection` deliberately separated so a cache reads as an intrusion, the #30 seam, the command-layer appointment join, the explicit-optional unemployed representation and its combined-bump recommendation, save composition with the outlives-the-season difference, neighbour contracts). The standing review item is scoped to the **join** rather than to #54 — #54 references nothing, so that is the only place a foreign write can hide. Status IN REVIEW. |
#endregion
