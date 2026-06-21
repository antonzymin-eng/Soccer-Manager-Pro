# Living World System Specification #22 — Appendices

**Created:** June 21, 2026
**Last Updated:** June 21, 2026 (v0.3 — PASS-2 fix pass: `ActiveLayers` bitmask added to the Appendix B snapshot order, replacing the NaN sentinel (AR2-M1))
**Version:** 0.3
**Status:** IN REVIEW (June 21, 2026)

---

## Appendix A — Constant catalogue (`LivingWorldConstants.cs`)

All `[GT]` magnitudes below are **illustrative pending the §7 balance pass** — the reviewed contract is
the shape/direction, not the value (precedent #21 G2, #8 draft-level). No `[EST]` tags (FR-LW-029).

| Constant | Value (illustrative) | Tag | Rationale |
|---|---|---|---|
| `MEMORY_BUFFER_DEPTH` | 12 | `[GT]` | episodes retained per significant edge (target 8–16, §3.2) |
| `SALIENCE_INITIAL` | 1.0 | `[GT]` | salience of a fresh episode |
| `SALIENCE_DECAY_RATE` | 0.02 / tick | `[GT]` | per-calendar-day salience decay (§3.2) |
| `SALIENCE_REF_THRESHOLD` | 0.30 | `[GT]` | min salience to be citable in text (§3.3) |
| `LAYER_VOLATILITY_DEFAULT` | 0.30 | `[GT]` | edge-update responsiveness `v` (§3.1) |
| `LAYER_DECAY_RATE` | 0.01 / tick | `[GT]` | relaxation toward baseline `r` (§3.1) |
| `ARC_MAX_LIFETIME_DAYS` | 120 | `[GT]` | per-instance liveness bound (§3.4 / §6.2) |
| `ARC_SPAWN_THRESHOLD_*` | per-arc | `[GT]` | trigger thresholds (one per `ArcKind`, §3.4) |
| `ACTIVE_SET_EXTERNAL_CONTACTS_MAX` | 64 | `[GT]` | bound on per-manager external contacts (§3.5) |
| `SAVE_SIZE_BUDGET` | (platform) | `[GT]` | caps live edges + live episodes + cold summaries (§4.5) |
| `COLD_SUMMARY_RETAINED_EPISODES` | 4 | `[GT]` | top-N salient retained on demotion (schema deferred §7) |
| `CLIQUE_THRESHOLD` | 0.6 | `[CROSS]` | vol-2 §2.1 — consumed, not set here |
| `PULSE_INTRA_RETENTION` | 0.90–1.00 | `[CROSS]` | vol-2 §2.2 — consumed |
| `PULSE_INTER_RETENTION` | 0.40–0.60 | `[CROSS]` | vol-2 §2.2 — consumed |
| `PULSE_INTER_LATENCY_DAYS` | 1–2 | `[CROSS]` | vol-2 §2.2 — consumed |

## Appendix B — Canonical snapshot field order (pinned before first serialisation)

`RelationshipEdge { FromId, ToId, ActiveLayers, PlayerEdge, Affinity, Trust, Memory[] }` →
`MemoryEpisode { EpisodeId, Kind, Salience, WorldTick, ManagerChoiceId }` →
`Arc { Kind, State, Cause, PinnedEpisodes[], SpawnTick, MaxLifetimeTick }` →
`SpawnCause { TriggerId, Inputs[], SnapshotRef, WorldTick }` →
`ColdSummary { EntityId, NetRelationship, RetainedEpisodes[] }`.

Order is load-bearing for the `SNAPSHOT_SCHEMA_VERSION` digest (ERR-022-002); locked before T-store
activation.

## Appendix C — Enum rosters (APPEND-only, ordinal-stable — FR-LW-028)

- `RelationshipLayer { PlayerEdge=0, Affinity=1, Trust=2 }`

**Active-layer matrix by node-type pairing (FR-LW-005 / §2.2.1).** Inactive layers store sentinel `NaN`
and are excluded from updates and the F6 [0,1] invariant.

| Edge pairing | PlayerEdge | Affinity | Trust |
|---|---|---|---|
| player ↔ player | active | — | — |
| manager ↔ journalist/board/staff | — | active | active |
| manager ↔ player | — | active | active |

- `EventKind` — populated from the consumed vol-2 §7 event taxonomy + manager-choice outcomes (roster
  finalised at implementation; APPEND-only).
- `ArcKind { DressingRoomSplit=0, MediaVendetta=1, BoardPatienceCollapse=2, WonderkidVsVeteran=3, … }`
  (APPEND-only; ordinal order is also the non-entity arc evaluation order, FR-LW-017).
- `InteractionIntent` — indexed against the authored corpus (APPEND-only).
