# Path to Playable — Audio Parallel Workstream Amendment

> **Created:** September 6, 2026
> **Version:** 0.1
> **Status:** ROADMAP AMENDMENT — pending owner acceptance with the audio G0 plan
> **Amends:** `docs/tracking/path-to-playable-roadmap.md`
> **Scope:** implementation sequencing only. This document changes no APPROVED specification, no `SPEC_INDEX.md` row, and no PM-1/PM-2/PM-3 exit criterion.

---

## 1. Reason for the amendment

The Path-to-Playable roadmap deliberately deferred **#48 Match Presentation Depth** and **#51 Audio & Sound Design** past PM-3 because neither is required to prove a playable season. That remains correct for the **critical path**.

The owner has subsequently directed that audio development proceed **in parallel** with the main development stream. The original deferral did not distinguish "not on the playable-season critical path" from "must not be worked on concurrently." This amendment makes that distinction explicit.

The amendment is intentionally narrow: it authorizes safe #51 work that cannot block Track S, while preserving all prerequisite boundaries for the audible integration stages.

---

## 2. Revised sequencing rule

### 2.1 Authorized in parallel

After acceptance of `docs/planning/audio-implementation-plan.md` G0, the following may proceed concurrently with the primary roadmap:

- #51 **T0** — the pure, silent `TacticalDirector.Audio` contract/catalogue/ducking assembly;
- #51 **T1** — the host-free playback contract, mixer arithmetic, settings fragment, and display-side variation contract;
- the audio-production **pipeline substrate** — source/runtime layout, provenance/rights metadata, stable naming, provisional-state rules, and validators;
- the deliberately bounded **G3 prototype asset set**, but not library-scale production.

These items are additive, host-free where specified, and do not advance or alter the season-simulation critical path.

### 2.2 Still blocked on #48

#51 **T2** shell integration and the G3 audible vertical slice require the real #48 T0 surfaces — `CueId`, #48 `CueParams`, and `ICueSink` — to exist first.

This amendment **does not authorize audio work to invent, duplicate, or silently implement #48 inside #51 or the shell.** #48 remains a separate governed implementation landing. Until it exists, audio T2 is BLOCKED.

### 2.3 Still blocked on #49 for caption rendering

#51 can own `CaptionId` and enforce caption decisions at T0 without #49. Actual localized caption rendering waits until #49's production assembly exists and can add the approved inbound `#49 → #51` reference.

This amendment does not bring #49 forward inside the audio workstream.

---

## 3. Critical-path protection

Audio remains **non-critical to PM-2** and must not delay Track S or other owner-designated critical-path work.

If shared capacity becomes constrained, precedence is:

1. active PM-2/PM-3 critical-path blockers;
2. architecture/governance work required to keep those landings safe;
3. parallel audio work.

An audio failure cannot turn a PM-2-ready build into a non-playable build. Until the owner separately changes a playable milestone, silence remains a valid state under #51 FR-AU-038.

---

## 4. Production scaling boundary

This amendment does **not** authorize bulk sound-library production immediately.

Large-scale UI/SFX/crowd/music production remains gated by audio **G3**, which requires:

- the real #48 shell path;
- build-time mapping completeness;
- host binding;
- a small audible vertical slice;
- observer-neutrality proof;
- host profiling sufficient to freeze the Early Access middleware/import posture.

Before G3, only the explicitly bounded prototype set in the audio implementation plan may be authored.

---

## 5. Effect on the original roadmap

Read the original statement that #48/#51 are "deliberately deferred past PM-3" as follows after this amendment:

- they remain **deferred from the PM-2/PM-3 critical path and exit criteria**;
- #51 T0/T1 and its pipeline substrate may nevertheless advance as a **parallel, non-blocking workstream**;
- #51 T2 remains prerequisite-gated on #48 T0;
- no PM milestone is redefined by this amendment.

All other Path-to-Playable sequencing and constraints remain unchanged.

---

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-09-06 | Narrow owner-directed sequencing amendment: authorizes #51 T0/T1 + pipeline work in parallel without adding audio to the PM-2 critical path; keeps T2 blocked on #48 and caption rendering blocked on #49; keeps bulk production behind G3. |
