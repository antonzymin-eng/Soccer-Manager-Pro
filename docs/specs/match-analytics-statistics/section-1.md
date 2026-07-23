# Match Analytics & Statistics #37 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/match-analytics-statistics-design.md` v0.2

---

## 1.1 Introduction

Match Analytics (#37) derives **read-only** statistics from what a real match already produces. It
sits in the presentation layer alongside `match-viewer`: it observes the match, it does not shape it.
The single hard rule that gives the spec its shape is settled in §1.4 before any requirement: a stat
is **in scope only if it is derivable from what the match engine emits today** — the digest-bearing
Tier A event ledger and the observational world-state surface. A stat that would need a new engine
event or producer is **out of scope** and named as a match-engine follow-up.

## 1.2 Cadence and layer

#37 observes at the **world-tick / render cadence**, not inside the 60 Hz physics hot path and not
inside the 10 Hz AI stride. It performs no per-physics-tick work in the engine; it pulls observational
reads at the same rate `match-viewer` samples. It is **not** zero-allocation-critical game-loop code —
it is presentation tooling (Code Standards #20 layer taxonomy: presentation, not sim).

## 1.3 Scope

**In scope (Stage-1, derivable today — see §1.4 and Appendix B):**
- Possession share (per team).
- Goals + a goal-location map.
- Fouls (+ location), cards, offsides (+ location).
- Set-piece tallies: corners, throw-ins, goal-kicks.
- Substitutions.
- Territorial % and positional heatmaps (from the observational positional sample).

**Out of scope — producer-gated, deferred to a named match-engine follow-up (§7.2, Appendix D):**
- Shots / shots-on-target, pass-completion %, tackles, saves.
- Shot-geometry **xG over shots** (the xG model *shape* is authored — KD-2 — but has no valid live
  input at Stage 1).

**Out of scope — other specs:**
- The UI that renders the report (#38).
- News/inbox consumption (#46).
- Any persisted report (a #38/#30 concern — #37 stores nothing).
- Discipline aggregation for suspensions (#44 — a sibling read-only ledger derivation, not a #37
  consumer).

## 1.4 The scope-defining reality (verified against source)

The match engine (`MatchEngine.cs`) has exactly **9 `EventBus.Publish` sites** producing **8 distinct
Tier A record types**: `PossessionChangedEvent` (0x04), `FoulCommittedEvent` (0x05), `CardIssuedEvent`
(0x06), `GoalAwardedEvent` (0x07), `SubstitutionEvent` (0x08), `OffsideCalledEvent` (0x18),
`RestartAwardedEvent` (0x19), `MatchPhaseChangedEvent` (0x1A). It publishes **no** shot, pass, tackle,
or save/claim event — those ordinals are *registered* in `EventRegistry` but have **no producer**. And
there is **no ledger reader** (`EventBus.SerializeLedger` is write-only). These three facts
(enumerated payloads in §2.2 / Appendix B) are the load-bearing constraints:
1. the derivable event set is exactly those 8 record types (KD-1);
2. positional stats (territorial %, heatmaps) cannot come from the ledger — no event carries a
   streamed position — so they come from the observational world-state sample (§1.4 / KD-3);
3. consumption is **live during the match**, not a post-match re-read (KD-3).

## 1.5 Dependencies

| Direction | Spec / surface | Nature |
|---|---|---|
| Upstream (needs) | Event System #17 — the `EventBus` Tier A records | read-only (the 8 types in §1.4) |
| Upstream (needs) | `MatchEngine` observation surface (`BallView`/`AgentView`/`AgentTeamId`/`PossessingAgentId`/`CurrentTick`) + the new read-only ledger tap (KD-7) | read-only |
| Peer precedent | `src/match-viewer/` (`MatchReplayRecorder`) | observational-read + digest-lock pattern reused |
| Downstream (consumers) | #38 UI post-match report; #46 news/inbox | render the #37 view models (KD-4) |

No dependency on #27/#30 for the derivation itself (a match's stats are a function of that match).

## 1.6 Key decisions

**KD-1 — Scope is exactly the derivable set; the rest is a named match-engine follow-up, not #37.**
In scope = the §1.3 in-scope list (all from the 8 record types + the positional sample). The
producer-gated set (shots/on-target, pass %, tackles, saves, shot-geometry xG) requires a **new Tier A
producer in the match engine** — a match-engine change with its own review. Building a #37 consumer for
an event that has no producer is the **FR-LW-031 phantom-consumer** violation the project forbids (the
discipline that kept #30 producer-only and GK/Heading opt-in). The spec **names each deferred stat and
the producer it waits on** (Appendix D), so the follow-up is "add producer → extend aggregation," not a
redesign.

**KD-2 — xG is a model shape now, fully producer-gated.** The xG location model is a pinned
deterministic function of shot geometry (distance-to-goal + subtended angle — the standard two-term
logistic shape, §3.3), `[GT]` coefficients in `MatchAnalyticsConstants`, illustrative pending a Stage-2
balance pass (the #21/#8 precedent). **Its input is the shot origin, which the ledger carries nowhere.**
The one goal geometry it has — `GoalAwardedEvent.BallPosition` — is the goal-line **crossing point**
(its own doc: *"the ball at the moment it crossed the goal line"*), not the shot origin, and cannot
substitute. So xG is authored + unit-locked at T0 but has **no valid live input at Stage 1**; it
activates unchanged when the deferred `ShotAttemptedEvent` producer (carrying the shot origin) lands.
The goal position is used as a **goal-location map**, never as xG.

**KD-3 — Live observational aggregation, one core, two read taps.** #37 consumes live during the match
(no post-match ledger reader exists). One `MatchAnalyticsAggregator` core is fed: (a) the event ledger,
via the read-only per-tick tap (KD-7) — event-count/location stats; (b) the observational world-state
sample (`BallView`/`AgentView`) — positional stats. Both are the `match-viewer` observational class.
Ledger records carry no tick field, so the aggregator timestamps by observing per tick and reading
`MatchEngine.CurrentTick` (load-bearing for possession-share weighting).

**KD-4 — The #38 view-model contract, one-directional.** #38 renders against immutable per-match value
structs (`MatchStatline` per team + `AdvancedStatline`); no engine type leaks through. **Presentation-
clean:** the sim never references #37; #37 references only the #17 event structs + the `MatchEngine`
observation surface (the `match-viewer` reference set). No sim assembly may reference
`TacticalDirector.MatchAnalytics`.

**KD-5 — Determinism by construction.** The derivation is a pure function of the deterministic ledger +
deterministic world-state sample: **no RNG, no domain tag, no `SubsystemOrdinal`**. Two contracts,
both `match-viewer`-precedented: two-run determinism (same match ⇒ byte-identical stats) and
observer-neutrality (computing analytics does not perturb the match digest). No dependency on
wall-clock or observation order.

**KD-6 — Agent→team resolution via the observational map.** Records keyed on *agentId* only
(`FoulCommittedEvent.Offender`, `PossessionChangedEvent.NewHolder`) resolve team through
`MatchEngine.AgentTeamId(i)`, captured once at boot (a slot's team is fixed; a substitution swaps the
bench roster into a slot whose team is unchanged, so slot→team is stable). Records carrying a team byte
(`GoalAwardedEvent.ScoringTeam`, `RestartAwardedEvent.AwardedTeam`, `OffsideCalledEvent.Team`) use it
directly.

**KD-7 — The read-only per-tick ledger tap is an observation surface, not a producer.** To feed the
core the per-tick published records, #37 exposes a read-only per-tick ledger view on `MatchEngine`,
mirroring `BallView` (which `match-viewer` established at `MatchEngine` v1.24). It is a read-only
accessor — it cannot mutate the ledger or the digest — so it honours the "no new engine **event or
producer**" bar (an observation accessor is neither). Lifetime-decoupled pull cadence (the `match-viewer`
model) is preferred over a boot-time Tier A subscription so #37's lifetime is not wired into the engine
boot sequence.

## 1.7 Boundary matrix

| Concern | Owner | #37's relationship |
|---|---|---|
| Producing match events (goals, fouls, …) | `MatchEngine` (#17 records) | **reads** them (never produces) |
| The digest-bearing ledger + its serialization | Event System #17 | **reads** current-tick records via the KD-7 tap |
| World-state (ball/agent positions) | `MatchEngine` | **samples** observationally (`match-viewer` reads) |
| Shot/pass/tackle events | **does not exist** (deferred producer) | **named + deferred** (Appendix D), never phantom-consumed |
| Rendering the report | #38 UI | **provides** the view models (KD-4) |
| Persisting a report | #38 / #30 | **out of scope** (#37 stores nothing) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial section from the converged supplement. Scope/deps/KD-1..7/boundary matrix, grounded in the verified 8-record ledger inventory. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
