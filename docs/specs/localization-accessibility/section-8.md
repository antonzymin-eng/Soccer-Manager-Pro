# Localization & Accessibility #49 — Section 8: References

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (1H+1M+1L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 Internal source (verified against real files)

- **The one built producer** — `src/living-world/InteractionTextGenerator.cs`
  (`Generate(InteractionIntent, in InteractionSlots) → string`; the `world.text` reserved draw;
  `templates[draw % (ulong)templates.Length]`; `Expand` `.Replace` slot substitution; `EpisodeClause`
  append; the pre-draw validation ordering).
- **The corpus** — `src/living-world/InteractionTextCorpus.cs` (`TemplatesFor(intent)` — `None`=empty row,
  throws on `None`/out-of-roster; `EpisodeClause(EventKind)` — six defined kinds; the APPEND-only
  determinism comment: template order is load-bearing for `draw mod count`).
- **The slot carrier** — `src/living-world/InteractionSlots.cs` (`SubjectName`/`OpponentName`/`HomeGoals`/
  `AwayGoals`/`HasCitedEpisode`/`CitedEpisode`).
- **The determinism draw seam** — `DeterministicRngService.Reserve`/`DrawReserved`/`CloseReservation` (the
  `world.text` sub-stream; `DrawReserved` yields a `ulong`).

## 8.2 Cross-spec

- **Code Standards #20 §3.5.2** — the layer taxonomy: the presentation/content layer reads sim; sim never
  references presentation (the no-reverse-reference rule KD-6 inherits).
- **UI / Client Framework #38** — FR-UI-004 ("the UI MUST compute no game state, analytics, or localized
  text") and §7.3 / KD-5 ("localized strings the UI renders through #49's seam; the UI holds no string
  catalogue — that is #49"). #49 supplies the contract #38 was written against.
- **Living World #22** — §3.3 (surface-text expansion, KD-6), FR-LW-011/012/013/020/028 (the `world.text`
  draw discipline, offline-authored corpus, citation gate, APPEND-only order), slice-3 AR-1 L-3 (the
  no-cursor-on-refusal invariant §3.4 preserves).
- **Living World #22 FR-LW-031** — the phantom-dependency rule (KD-6 / FR-LC-013: reference only built
  producers).
- **Deterministic Simulation #16 §3.4** — the domain-tag / SubsystemOrdinal catalogue #49 allocates nothing
  in (KD-7 / FR-LC-017).

## 8.3 Precedent (this project's pattern)

- **Match Analytics #37 / UI Framework #38** — the forward-design + no-determinism-identifier posture (a
  read-only/display surface approved before its T-phase code; no `_RESERVED_` placeholder).
- **`match-viewer` observer-neutrality** — the digest-lock precedent T-LC-DET-001 mirrors (observing/
  rendering perturbs no digest).
- **#19 `ScenarioIndex` / #21 `TeamTacticFileLoader`** — the "content authored in code at Stage 0; the
  on-disk encoding is a later parser swap" precedent the base-locale catalogue follows.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial references (internal source verified; cross-spec #20/#38/#22/#16; project precedent). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (1H+1M+1L; H-1 generic-core / per-producer boundary-adapter split, M-1 FR-LC-008a construction-time roster-coverage invariant, L-1 `{score}` derived) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
