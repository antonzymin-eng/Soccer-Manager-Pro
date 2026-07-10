# Dismarking & Marker-Awareness AI Specification #23 — Section 7: Future Extensions and Deferrals

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 Attribute-scaled dismark quality (Stage 2+)

Scaling the offset by the marked agent's own off-the-ball attributes (a skilled mover loses a
marker better) is deliberately left to the balance pass that pins the `[GT]` scalars — adding an
attribute term now would double the §3.3 tuning surface before the base shape is validated.

## 7.2 Marker counter-behaviour (Stage 2+, #14-owned)

The defender's reaction to being shaken (re-tighten, switch, pass on the mark) belongs to Defensive
AI #14. No hook is written here (FR-DM-018). When #14 takes it up, the marker perceives the
attacker's movement through its own `FilteredView` — the same KD-1 invariant from the other side.

## 7.3 Open-space direction selection

§3.3's directly-away direction upgrades to a side-preferring variant (biasing toward the larger
perceived gap) once the base behaviour is balance-validated. Deterministic tiebreak rules would be
defined then.

## 7.4 Per-agent dial

`DismarkIntensity` is team-level here. A per-agent override slot on `PlayerInstructions` (the
`InstrBias` pattern) is a natural #21 extension once the team dial proves out.

## 7.5 Blind-side runs

True blind-side movement (exploiting the marker's FoV, not just distance) requires modeling the
*opponent's* facing from perceived data — richer than `PerceivedAgent` carries today. Explicitly
deferred rather than approximated from ground truth (which would violate KD-1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial deferrals. |
#endregion
