# Tactical Presets & AI-Manager Selection Specification #26 — Section 7: Future Extensions and Deferrals

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 Opponent-aware adaptation (explicit deferral; supplement open question 3)

Reading an opponent's *observable pattern* (not their private structs — KD-5) requires an
opponent-modeling canon that does not exist. Building a consumer now is the FR-LW-031
phantom-interface class. Deferred to a numbered follow-up when that canon lands, exactly as Living
World #22 defers `BackgroundTierSim`.

## 7.2 Event-triggered decision points (deferral; KD-2)

Goal/red-card/substitution triggers join §3.2's gate as OR-terms when their producers exist.
Goal-scoring detection is the likely first (ball-in-goal geometry #1 already has). The gate's
shape anticipates the OR-extension; no subscription is written until then.

## 7.3 On-disk preset format (deferral; KD-6)

The Stage-1 `[GT]` disk loader (FR-CS-019 lineage) swaps the in-code catalogue for parsed data;
`TacticPresetLibrary` consumers are unchanged. Same contract as `TeamTacticFileLoader`.

## 7.4 Preset visibility / scouting UI (supplement open question 4)

Whether a human sees an AI opponent's preset is a Stage-1+ UI-layer question; nothing in the
simulation layer exposes it (names never serialize, FR-TP-001).

## 7.5 Squad-aware selection

Selecting presets from squad attributes (pace ⇒ counter-attack) needs the vol-2 human-systems
canon; deferred with no hook.

## 7.6 Richer manager personality

`ManagerProfile` is deliberately 3 parameters. A personality model (risk appetite curves,
touchline behaviour) belongs to the Living-World/human-systems layer; this spec's profile is the
minimal deterministic surface adaptation needs.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial deferrals (four carried from the supplement, two added). |
#endregion
