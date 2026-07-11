# Tactical Presets & AI-Manager Selection Specification #26 — Section 8: References

**Created:** July 8, 2026
**Last Updated:** July 10, 2026, later same day (v0.3)
**Version:** 0.3
**Status:** APPROVED

---

## 8.1 Project-internal (authoritative)

| Ref | Content |
|---|---|
| `docs/tracking/game-model-ai-manager-design.md` v0.4 | source supplement; KD-1 boot/mid-match correction (AR-1), KD-2 producer-verification correction (AR-2) |
| Tactical Instructions #21 (whole spec) | hard dependency: `TeamTactic`/`PlayerTactic`, FR-TI-027 stride commit, FR-TI-031 identity, G2 pinned values |
| `src/match-engine/TeamTacticConfig(Applier).cs`, `PlayerTacticConfig(Applier).cs` | the boot seam FM-TP-01 projects into (pre-kickoff scope per their doc comments) |
| `src/match-engine/MatchEngine.cs` (`SetTeamTactic`/`SetPlayerTactic`) | the mid-match seam |
| Living World #22 KD-4 / `WorldClock` | the cadence-separation precedent KD-2 mirrors |
| Deterministic Sim #16 | ordinal stability, snapshot obligations, no-domain-tag posture |

## 8.2 Domain literature

| Ref | Status |
|---|---|
| Bradley, P. S. & Noakes, T. D. (2013) — *Match running performance fluctuations in elite soccer: indicative of fatigue, pacing or situational influences?* — **Journal of Sports Sciences 31(15):1627–1638, DOI [10.1080/02640414.2013.796062](https://doi.org/10.1080/02640414.2013.796062), PubMed PMID 23808376** — examines score-line (score differential ≥ 3), match importance, and substitution effects on high-intensity running (motivates the score/time adaptation form) | **VERIFIED** July 10, 2026 (later same day) — title/authors/journal/volume/pages/DOI corroborated across PubMed (PMID 23808376) and independent bibliographic indexes; direct publisher/Crossref DOI resolution remains blocked by the authoring environment's network policy, so verification is index-level (same evidence class as the accepted Wilson row). Closes the last open §8 row across specs #23–#26 |
| Wilson, J. — *Inverting the Pyramid: The History of Football Tactics* (Orion, hardcover, September 2, 2008, ISBN 978-0-7528-8995-5) — named-system tactical archetypes behind the preset catalogue | **VERIFIED** July 10, 2026 (publisher/retail listings; first edition, Orion Books) |

No formula constant is literature-derived; all magnitudes `[GT]`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial references. |
| 0.2 | 2026-07-10 | — | Wilson row VERIFIED (ISBN 978-0-7528-8995-5); Bradley row remains `[CITATION-PENDING]` with the July 10 verification attempt recorded (environment-blocked, not fabricated). |
| 0.3 | 2026-07-10 | — | Bradley row VERIFIED (Bradley & Noakes 2013, J Sports Sci 31(15):1627–1638, DOI 10.1080/02640414.2013.796062, PMID 23808376; index-level corroboration — publisher/Crossref direct resolution still environment-blocked). §8.2 fully closed. |
#endregion
