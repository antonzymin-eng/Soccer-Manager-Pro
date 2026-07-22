# Match Analytics & Statistics (#37) — Design Supplement

> **Created:** July 22, 2026 · **Last Updated:** July 22, 2026 (v0.2 — AR-1 fix + AR-2 convergence)
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` registry row).
> Same governance class as `season-competition-loop-design.md` / `match-engine-design.md`.
> **Candidate spec:** #37 · **Wave:** 1 · **Tier:** Stage 1 · **FR prefix:** `FR-AN`
> **Determinism:** read-only / presentation — **no RNG stream, no domain tag, no `SubsystemOrdinal`**
> (the `match-viewer` / analytics class).
> **Source plan:** `docs/tracking/spec-plans/spec-37-match-analytics-statistics.md` v0.2

---

## 0. One-paragraph intent

Match statistics derived **read-only** from what a real match already produces — the digest-bearing
Tier A event ledger (Event System #17) plus the observational world-state surface (`MatchEngine`'s
`BallView`/`AgentView`, the `match-viewer` precedent). #37 adds **no engine event and no producer**;
it mutates nothing; it stores nothing new and bumps no format version. It is the read-only prerequisite
the post-match report UI (#38) and news/inbox (#46) render against.

---

## 1. The load-bearing question, settled first (KD-1 grounding)

The single risk the plan flags (§9): *"discovering a target stat the ledger cannot supply turns a cheap
read-only spec into a match-engine change."* This supplement settles it **before** any FR is written,
by inventorying what a real match actually emits, verified against source.

### 1.1 What the match engine publishes into the digest-bearing ledger TODAY

`MatchEngine` has exactly **9 `EventBus.Publish` sites** (grep-verified in `MatchEngine.cs`), producing
**8 distinct Tier A record types**. Their payloads (verified in `src/event-system/*.cs`). Note `GoalAwardedEvent.BallPosition` is documented
as *"the ball at the moment it crossed the goal line"* — the **crossing point**, not the shot origin
(it drives a goal-location map, not a shot-distance xG; see KD-2):

| Ordinal | Type | Payload fields | Carries geometry? | Carries team? |
|---|---|---|---|---|
| `0x04` | `PossessionChangedEvent` | `PreviousHolder`, `NewHolder`, `Reason` | no | **agentId only** |
| `0x05` | `FoulCommittedEvent` | `Offender`, `Victim`, `Location(V3)`, `FoulKind` | **yes** | agentId only |
| `0x06` | `CardIssuedEvent` | `Recipient`, `CardKind`, `FoulOrdinal` | no | agentId only |
| `0x07` | `GoalAwardedEvent` | `Scorer`, `Assister`, `ScoringTeam`, `BallPosition(V3)` | **yes (crossing pt)** | **yes** |
| `0x08` | `SubstitutionEvent` | (roster swap) | no | (slot) |
| `0x18` | `OffsideCalledEvent` | `OffendingAgentId`, `Team`, `Location(V3)` | **yes** | **yes** |
| `0x19` | `RestartAwardedEvent` | `RestartKind`, `AwardedTeam`, `Location(V3)` | **yes** | **yes** |
| `0x1A` | `MatchPhaseChangedEvent` | `newPhase`, `homeScore`, `awayScore` | no | (score) |

### 1.2 What the ledger does NOT carry (verified: registered ≠ produced)

The registry (`EventRegistry.cs`) reserves Tier A ordinals for pass (`0x0C`/`0x0D`), and the GK plugin
registers save/claim/distribution (`0x14`/`0x15`/`0x16`) — **but `MatchEngine` publishes none of them**
(grep for `Publish.*Pass|Publish.*Shot|Publish.*Tackle|Publish.*Save` in `MatchEngine.cs` → zero hits).
There is **no shot event, no pass-completed event, no tackle event, and no interception event** produced
by a real match. Goals appear only as `GoalAwardedEvent`; shots that miss/are saved leave no ledger trace.
The GK/Heading orchestrators (opt-in Phase 1, July 22) do not commit their events into the per-tick digest.

**Therefore, definitively:** shots/on-target, pass-completion %, tackles, saves, and any
**shot-geometry xG** are **not derivable from today's ledger.** They are the plan's KD-1 producer-gated set.

### 1.3 The second read source (positional data the ledger never carries)

Possession events carry a holder *agentId*, never a position; no event streams ball/agent positions.
So **territorial %, heatmaps, and average-position/shape maps cannot come from the ledger at all** —
they come from the **observational world-state sample** (`MatchEngine.BallView` / `AgentView(i)` /
`AgentTeamId(i)` / `PossessingAgentId` / `CurrentTick`), the exact read `match-viewer`'s
`MatchReplayRecorder` already performs. This is read-only and deterministic; it is not a new surface.

### 1.4 There is no post-match ledger reader

`EventBus.SerializeLedger` is **write-only** — grep for `DeserializeLedger|ReadLedger|ParseLedger`
returns nothing. The serialized ledger bytes in a match snapshot **cannot be re-parsed today.** So #37
cannot be a "load the save and recompute" reader; it must observe **live during the match** (KD-3).

---

## 2. Key design decisions

### KD-1 — Scope is exactly the derivable set; the rest is an explicit match-engine follow-up, not #37

**In scope (Stage-1 minimal, all from §1.1 + §1.3, no new producer):**
- **Possession share** — tick-weighted from `PossessionChangedEvent` deltas × the agent→team map (KD-6).
- **Goals + goal-location map** — `GoalAwardedEvent` (scorer/assister/team/`BallPosition`).
- **Set-piece & discipline tallies** — corners / throw-ins / goal-kicks (`RestartAwardedEvent.RestartKind`),
  fouls (`FoulCommittedEvent`, with location), cards (`CardIssuedEvent`), offsides (`OffsideCalledEvent`),
  substitutions — counts per team, plus the location-carrying ones as point maps.
- **Territorial % + heatmaps** — from the §1.3 observational positional sample, **not** the ledger.

**Deferred, producer-gated (NOT #37's surface):** shots/on-target, pass-completion %, tackles, saves,
and shot-geometry xG. Each needs a **new Tier A producer in the match engine** (a `ShotAttemptedEvent`
with geometry, a `PassCompletedEvent`, a `TackleEvent`) — a match-engine change with its own review.
Building a #37 consumer for events that have no producer would be the **FR-LW-031 phantom-consumer**
violation the project forbids (the same discipline that kept #30 producer-only and GK/Heading opt-in).
The spec **names each deferred stat and the exact producer it waits on**, so the follow-up is a clean
"add producer → #37 aggregation extends over it," not a redesign.

### KD-2 — xG is authored as a model SHAPE now, but is FULLY producer-gated (no valid Stage-1 live input)

The xG location model is a **pinned deterministic function** of shot geometry (distance-to-goal-centre
and the angle the goal subtends from the shot point — the standard two-term logistic shape), with
`[GT]` coefficients in a `MatchAnalyticsConstants` catalogue, **illustrative pending a Stage-2 balance
pass** (the #21/#8 precedent — the contract is the model shape, not the tuned numbers).

**Its input is the shot ORIGIN, which the ledger does not carry anywhere.** The one geometry the ledger
does have — `GoalAwardedEvent.BallPosition` — is the **goal-line crossing point** (verified: its own doc
says *"the ball at the moment it crossed the goal line"*), which is nearly constant across goals (all
cross within the goal mouth) and carries no shot-distance information. It **cannot** substitute for a
shot origin. So xG is **fully producer-gated**: the model function is authored and unit-locked at T0
(pure, testable against hand-derived geometries), but has **no valid live input at Stage 1** and produces
no live xG until the deferred `ShotAttemptedEvent` producer (KD-1) — which must carry the shot-origin
position — exists. When it lands, the model function is consumed unchanged. This is stated so the model's
presence is not mistaken for a working Stage-1 xG. The goal `BallPosition` data is still used, but as a
**goal-location map** (a legitimate presentation stat), never as xG.

### KD-3 — Live observational aggregation (there is no post-match reader), one core, two read taps

#37 consumes **live during the match**, not post-hoc (§1.4 — no ledger reader exists). One aggregation
core is fed two deterministic read taps:
1. the **event ledger**, via a read-only per-tick observational tap (see KD-7) — event-count/location stats;
2. the **observational world-state sample** (`BallView`/`AgentView`, `match-viewer` cadence) — positional
   heatmap/territorial stats.

Because the ledger records carry **no tick field**, the aggregator timestamps by observing the tap
**per tick** and reading `MatchEngine.CurrentTick` — this is what makes possession-share tick-weighting
(and any duration stat) well-defined without a producer change.

Both are the same observational class `match-viewer` already established. A **post-match snapshot reader
is out of scope and blocked upstream** — it would require a #17 ledger deserializer that does not exist;
if career mode later wants persisted reports, that reader is #38's/#30's concern, not #37's.

### KD-4 — The #38 view-model contract: immutable per-match value structs, one-directional

#38 renders against immutable per-match view models — `MatchStatline` (per team: possession %, goals,
fouls, cards, offsides, corners, throw-ins, subs) + `AdvancedStatline` (territorial %, goal-quality/xG
sum, point maps) — plain value types, no engine references leaking through. **Presentation-clean and
one-directional:** the sim never references #37; #37 references only the #17 event structs and the
`MatchEngine` observation surface (exactly `match-viewer`'s reference set). No sim assembly may reference
`TacticalDirector.MatchAnalytics`.

### KD-5 — Determinism by construction: no RNG, no tag, observer-neutral

The derivation is a **pure function** of the deterministic ledger + the deterministic world-state sample.
No draw, no domain tag, no `SubsystemOrdinal`. Two contracts, both `match-viewer`-precedented:
**two-run determinism** (same match ⇒ byte-identical stats) and **observer-neutrality** (computing
analytics does not perturb the match digest — the `MatchViewerTests` digest-lock). #37 must introduce no
dependency on wall-clock or observation order.

### KD-6 — Agent→team resolution via the observational map, snapshotted at boot

Several ledger records key on *agentId* only (`FoulCommittedEvent.Offender`,
`PossessionChangedEvent.NewHolder`), not team. The aggregation core resolves team through
`MatchEngine.AgentTeamId(i)` — the observational map — captured once at boot (a match's roster→team
binding is fixed; a substitution swaps the bench roster into a slot whose team is unchanged, so the
slot→team map is stable). Records that already carry a team byte (`GoalAwardedEvent.ScoringTeam`,
`RestartAwardedEvent.AwardedTeam`, `OffsideCalledEvent.Team`) use it directly; the agentId-only records
route through the map.

### KD-7 — The read-only ledger tap is an observation surface, not a producer (no boundary violation)

To feed the core the per-tick published records, #37 needs to *see* them. Two options were weighed:
(a) subscribe as an ordinary Tier A/B consumer at boot; (b) a read-only observational per-tick ledger
view on `MatchEngine`, mirroring `BallView`. **(b) is chosen** — it keeps analytics fully in the
`match-viewer` observational class (a read-only accessor, added the way `BallView`/`AgentView` were),
avoids coupling #37 into the engine's digest-bearing subscribe path, and honours the plan's "no new
**engine event or producer**" bar precisely: an observational read accessor is neither an event nor a
producer. The accessor exposes the current tick's drained Tier A records as read-only copies; it cannot
mutate the ledger or the digest. This is the one small `MatchEngine` addition #37 lands, and it is the
same *kind* of addition `match-viewer` already made (`MatchEngine` v1.24's read-only observation props).

---

## 3. Primary surfaces (proposed)

- New assembly **`TacticalDirector.MatchAnalytics`** (`src/match-analytics/`), references `event-system`
  (the #17 event structs) + `match-engine` (the observation surface) — the `match-viewer` reference set;
  referenced by **no** sim assembly (KD-4).
- **`MatchAnalyticsAggregator`** — the KD-3 core: fed the per-tick ledger tap + the world-state sample,
  accumulates counts/locations/possession-ticks/positional bins into the view models.
- **`XgLocationModel`** — the KD-2 pinned deterministic function; coefficients in `MatchAnalyticsConstants`.
- **`MatchStatline` / `AdvancedStatline`** — the KD-4 immutable per-match view models #38 renders.
- Referenced existing seams: the `EventBus` ledger / Event System #17; the `MatchReplayRecorder`
  observational-read precedent in `src/match-viewer/`; the `MatchEngine` observation surface (KD-6/KD-7).

---

## 4. Reserved identifiers

- **Candidate number #37** — matches the roadmap / `spec-plans/spec-37-…` reservation. `SPEC_INDEX.md`
  registry row is added **at promotion to section files** (the #30 / #21–#27 precedent), not now.
- **FR prefix `FR-AN`** — verify unclaimed by grep over `docs/specs/**/*.md` before promotion (current
  prefixes: AT, BU, CS, DA, DM, DS, EVT, GK, HE, LW, PA, PO, PR, RO, SN, SQ, TI, TP, TS — `AN` free).
- **No determinism identifiers** — #37 registers no domain tag / ordinal / RNG stream (KD-5). This is a
  positive design property, **not** a deferred allocation: unlike #30's `0x22`, there is nothing to
  reserve in #16 §3.4, and no `_RESERVED_` placeholder is warranted.

---

## 5. Implementation plan (post-approval, forward design)

**Promotion pipeline (this supplement → APPROVED):** author the 11-file section set at `IN REVIEW`
(§1 scope/boundary/KD-1..7, §2 FR-AN-*/data structures/failure modes, §3 algorithms — possession
tick-weighting, the xG function, positional binning, agent→team resolution, §4 architecture/assembly/
observation-tap signature, §5 test plan, §6 perf, §7 forward/deferred-producer table, §8 refs, §9
checklist, appendices) → PASS-1 adversarial review → AR-2 convergence → R-01..R-05 sign-off → APPROVED.

**T-phase code sequence (post-APPROVED, the #21–#30 pre-T0 precedent):**
- **T0** — value types (`MatchStatline`/`AdvancedStatline`), `MatchAnalyticsConstants`, `XgLocationModel`
  (pure), + unit locks. No engine wiring; behaviour-neutral (the engine is untouched).
- **T1** — the read-only per-tick ledger tap on `MatchEngine` (KD-7, the `BallView`-class accessor) +
  `MatchAnalyticsAggregator` consuming it and the world-state sample; observer-neutrality digest-lock
  (the `MatchViewerTests` precedent) + two-run determinism.
- **T2** — the deferred-producer follow-up (a match-engine change, **its own review**): add
  `ShotAttemptedEvent` (carrying the shot-origin geometry) + pass/tackle producers as scoped, then
  extend the aggregator over them and **activate xG over real shots** (the T0 model function, unchanged).
  Explicitly out of #37's own scope; #37 approval names it, doesn't own it.

**Deliberately NOT built:** any new engine event/producer (KD-1 — that is the T2 follow-up's review);
a post-match ledger reader (§1.4 — a #17 addition); persisted reports (#38/#30); the balance pass pinning
the xG `[GT]` coefficients (Stage-2, #21 §9.2 precedent).

## Version History
| Version | Date | Change |
|---|---|---|
| v0.1 | July 22, 2026 | Initial supplement. KD-1 settled against verified source: the engine publishes 8 Tier A record types and no shot/pass/tackle/save events; no ledger reader exists. Scope = the derivable set + observational positional sample; shots/passes/tackles/xG-over-shots deferred to a producer-gated match-engine follow-up. KD-3 live two-tap aggregation, KD-4 view-model contract, KD-5 no-RNG determinism, KD-6 agent→team map, KD-7 read-only ledger tap = observation surface not producer. |
| v0.2 | July 22, 2026 | **AR-1 (1M, fixed):** KD-2's "goals-only corpus feeds xG" was geometrically unsound — `GoalAwardedEvent.BallPosition` is the goal-line **crossing point** (verified against its own doc + the `MatchEngine` publish site), not the shot origin an xG-by-location model needs; rewrote KD-2 as **fully producer-gated** (model shape authored + unit-locked at T0, no valid Stage-1 live input; goal position → goal-location map, never xG), and aligned the §1.1 table note + T2 text. **AR-2 (0H+0M; L-only ⇒ CONVERGENCE):** re-read all KDs against source — added the KD-3 clarification that ledger records carry no tick field so the aggregator timestamps via per-tick observation of `MatchEngine.CurrentTick` (load-bearing for possession-share); re-verified the 9-publish-sites/8-types count, the no-reader claim, the agent→team map stability under subs, and the `FR-AN`/no-domain-tag identifiers. |
