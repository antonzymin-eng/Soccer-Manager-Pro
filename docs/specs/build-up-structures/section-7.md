# Scripted Build-Up Structures Specification #24 — Section 7: Future Extensions and Deferrals

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 Additional structures

The catalogue ships three named structures. Additions (e.g. a false-nine drop, a box midfield) are
pure `[GT]` data rows behind the APPEND-only enum — no code change beyond the ordinal test.

## 7.2 Pressure-adaptive build-up

Switching structure under opponent press intensity requires opponent-derived signals; when taken
up, it must route through #7 `FilteredView` per KD-5 — likely composing with #23's
`MarkingPressure` (a team-aggregate "we are being pressed" scalar). Deferred; no hook written.

## 7.3 Out-of-possession structures

Deliberately excluded (§1.1) — mid-block/low-block shapes belong to #14's line/compactness space.
If ever unified, that is a #12/#14 boundary renegotiation, not an extension here.

## 7.4 Pass-pattern scripting

"Play out through the pivot" as an *action* preference is a #8 concern (a `PlayerInstructions`
bias, the #21 InstrBias pattern) — explicitly not smuggled in via positioning (KD-1). No hook.

## 7.5 Per-zone tempo coupling

Coupling `Tempo` to the committed zone (slower in own third) is a #21/#8 balance-pass idea once
this spec's zone state exists as a routing field. Deferred until a consumer is specified.

## 7.6 Slot-specific overlay rows

The Appendix A tables are lane-keyed, so paired half-space slots always move together (PASS-1 M-3
note). Per-`SlotIndex` rows (e.g. only one pivot drops) are a data-model extension deferred until
the lane-keyed catalogue is balance-validated.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial deferrals. |
| 0.2 | 2026-07-08 | — | PASS-1: §7.6 slot-specific-rows deferral added (M-3). |
#endregion
