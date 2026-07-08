# Tactical Presets & AI-Manager Selection Specification #26 — Section 8: References

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

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
| Bradley, P. et al. — score-line effects on match running/tactical behaviour (motivates the score/time adaptation form) | `[CITATION-PENDING]` — specific paper + DOI to pin before `APPROVED` |
| Wilson, J. — *Inverting the Pyramid* (Orion, 2008) — named-system tactical archetypes behind the preset catalogue | `[CITATION-PENDING]` edition/ISBN |

No formula constant is literature-derived; all magnitudes `[GT]`.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial references. |
#endregion
