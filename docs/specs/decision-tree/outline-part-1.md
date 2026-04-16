# Decision Tree Specification #8 — Detailed Outline

**File:** `Decision_Tree_Spec_Outline_v1_1.md`  
**Purpose:** Planning document for the Decision Tree Specification #8. Establishes scope,
architecture, mathematical framework, action model, and open questions before full section
drafting begins. This document defines *what* will be written — not yet the full technical
detail.

**Created:** February 26, 2026, 11:45 PM PST  
**Revised:** February 26, 2026 (v1.1)  
**Version:** 1.1  
**Status:** DRAFT — Awaiting lead developer approval before section drafting begins  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Estimated Effort:** ~35–45 hours  
**Estimated Pages:** ~35–45  
**Author:** Claude (AI) with Anton (Lead Developer)

---

## ⚠ PRE-APPROVAL BLOCKING ISSUE

**BLK-001: Spec numbering conflict — must resolve before Section 2 is drafted**

Shot Mechanics Spec §1.1 refers to the Decision Tree as **Spec #7**.  
Perception System Spec §1.1 refers to the Decision Tree as **Spec #8**.  
PROGRESS.md and FILE_MANIFEST.md list it as **Spec #8**.  

The canonical numbering is **#8** (PROGRESS.md is authoritative). However, Shot
Mechanics §1.1 must be corrected before the Decision Tree spec references Shot Mechanics
as a hard dependency. This is a documentation error, not an architectural one, but must
be formally logged and resolved.

**Action required:** Add to Spec_Error_Log as ERR-010 (low priority, non-blocking on DT
outline approval, but must be closed before Shot Mechanics receives final sign-off and
before Decision Tree §4 interface contracts are written).

---

## UPSTREAM DEPENDENCIES

All three primary input sources are now specified — interface definition is authorised.

| Specification | What It Produces for DT | Interface Status |
|---------------|--------------------------|-----------------|
| Pass Mechanics (#5) | `PassRequest` struct — DT must populate and pass to Pass Mechanics | Caller contract defined in Pass Mechanics §4 |
| Shot Mechanics (#6) | `ShotRequest` struct — DT must populate and pass to Shot Mechanics | Caller contract defined in Shot Mechanics §3.1 |
| Perception System (#7) | `PerceptionSnapshot` struct — DT sole consumer; received each 10Hz heartbeat | Producer contract defined in Perception §4.5; no `IDecisionTreeConsumer` defined yet |
| Agent Movement (#2) | `AgentState`, `PlayerAttributes` — read-only inputs | Approved; DT reads but does not write |
| Ball Physics (#1) | `BallState` — read-only input for ball position/velocity | Approved; DT reads but does not write |
| Collision System (#3) | Spatial hash — DT may query for tactical option evaluation | Approved; query interface defined in Collision §4 |

**Interface authority note:** This specification must define `IPerceptionConsumer` or
equivalent delivery mechanism for `PerceptionSnapshot`. Perception System §4.5.3
explicitly defers this to Decision Tree Specification #8.

---

## DOWNSTREAM DEPENDENCIES (DT produces requests to these systems)

| Specification | What DT Sends | Notes |
|---------------|--------------|-------|
| Pass Mechanics (#5) | `PassRequest` | DT selects pass type, target, intent parameters |
| Shot Mechanics (#6) | `ShotRequest` | DT selects PowerIntent, ContactZone, PlacementTarget, SpinIntent |
| Agent Movement (#2) | Movement commands (struct TBD) | DT instructs agent to move to target position/orientation |
| First Touch (#4) | Implicit — DT decides to receive ball; First Touch is triggered by Collision System | No direct call from DT |
| Event System (#17) | `DecisionMadeEvent` (stub only at Stage 0) | For statistics and replay |

---

## EXECUTIVE SUMMARY

The Decision Tree is the agent brain. It receives a `PerceptionSnapshot` 10 times per
second and must decide, for its agent, what action to take: pass, shoot, dribble, hold,
move, press, or defend.

The central design problem: **actions must emerge from context, not from scripted
behaviour tables.** An agent in a shooting position should shoot; an agent under pressure
with no options should hold; a full-back with space to overlap should recognise and exploit
it. These outcomes must follow from attribute-weighted evaluation of the perceptual state
— not from hand-coded if/else trees.

The core model:

```
AgentAction = f(PerceptionSnapshot, PlayerAttributes, TacticalContext, GamePhase)
```

Where:
- `PerceptionSnapshot` — the agent's current filtered world view (from Perception System)
- `PlayerAttributes` — Decisions, Vision, Composure, Technique, etc.
- `TacticalContext` — current team tactical instructions (formation, pressing mode, etc.)
- `GamePhase` — possession state, field zone, match situation

**Output:** `AgentAction` — a typed action instruction dispatched to the relevant execution
system (Pass Mechanics, Shot Mechanics, Agent Movement).

**Simulation frequency:** Decision Tree runs at the **10Hz tactical heartbeat** — same
as Perception. Each heartbeat, every agent evaluates its snapshot and produces exactly
one action (which may be "continue current action" if mid-execution).

**Heartbeat evaluation order (from Perception System §7.7 KR-5 risk item):**
Perception runs first, Decision Tree runs second. This is a strict ordering constraint
that must be documented as a Key Design Decision in Section 1.

---

## CRITICAL DESIGN CONSTRAINT — NO OMNISCIENCE

The Decision Tree **may only consume information from `PerceptionSnapshot`**. It must not
read world state directly. An agent cannot react to a player it cannot perceive. This is
the fundamental constraint that makes the simulation epistemically grounded.

The only exception: game-state information that is publicly available to all agents
(score, time, possession indicator, match phase). These may be accessed via a
`MatchContext` struct, not via world state reads.

---

## STAGE 0 ACTION SET

At Stage 0, the Decision Tree selects from 7 action types, consistent with the Master
Development Plan §2.4:

| Action | Execution System | When Available |
|--------|-----------------|----------------|
| PASS | Pass Mechanics (#5) | Has ball; teammate visible; passing viable |
| SHOOT | Shot Mechanics (#6) | Has ball; within shooting range; goal visible |
| DRIBBLE | Agent Movement (#2) | Has ball; space ahead; dribbling viable |
| HOLD | Agent Movement (#2) | Has ball; no safe option; maintaining possession |
| MOVE_TO_POSITION | Agent Movement (#2) | No ball; tactical positioning instruction |
| PRESS | Agent Movement (#2) | No ball; opponent has ball within pressing trigger distance |
| INTERCEPT | Agent Movement (#2) | No ball; ball trajectory passes within interception window |

**Stage 0 does NOT include:** Heading (no ball above 0.5m decisions), set pieces,
goalkeeper decisions (Goalkeeper Mechanics #11), or any team instruction modifiers
(Formation System Stage 1+).

---

