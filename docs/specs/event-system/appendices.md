# Event System Specification #17 — Appendices

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 0.1 (initial section-file draft from `outline-detailed.md` v1.1)
**Status:** DRAFT

> Appendix layout follows `outline-detailed.md` v1.1 §"APPENDICES"
> (A Registry → B Byte Encoding → C Migration Recipes → D Glossary →
> E Failure-Mode Decision Table) and supersedes the v0.0 stub's
> generic CLAUDE.md template headings (Derivations / Numerical
> Verification / Sensitivity Analysis), which are not applicable to
> a non-physics spec.

---

## Appendix A — Event Type Registry

The canonical list of every event type ever published. Schema and
rules per §2.4.2. Producer-spec ownership and tier are normative.

**Append rules:**

- Downstream specs append rows at the IN REVIEW commit that
  introduces the event.
- `Ordinal` is byte-wide and monotonic. `0x00` is reserved; no
  reuse after deprecation (KD-9; FR-EVT-004).
- `First published in` records `<spec> <version>` for audit trail.
- `Deprecated` defaults to `N`. Once flipped to `Y`, the row is
  retained indefinitely (KD-9).

**Column-semantics note — `Producer phase`:**

- For **Tier A and Tier B** rows, `Producer phase` is **normative**
  and feeds the §3.6.1 phase-WriteSet check + the FM-017-002 sort
  key (component 1, `producingPhaseIndex`). A change requires a
  registry-row update and a coordinated #16 §3.6.1 back-prop per
  §3.7.1 (M6 of the section-files PASS 1 critique).
- For **Tier C** rows, `Producer phase` is **informational**
  (typical producing phase only). Tier C publish has no phase
  restriction per §3.2.1; the column is used for telemetry
  attribution in §6.5.1 trace channels and for documentation. A
  change of a Tier C row's `Producer phase` does NOT require
  back-prop into #16, because Tier C events do not appear in the
  digest, the ledger, or any phase WriteSet.

### A.1 Active registry (Spec #17 v1.0 seed; 11 rows)

| Ordinal | Type | Tier | Producer phase | Owning spec | Current version | Payload field list (canonical order, post-12-byte header) | maxPerTick (Tier C only) | First published in | Deprecated |
|---------|------|------|----------------|-------------|-----------------|-----------------------------------------------------------|---------------------------|---------------------|-----------|
| `0x01` | `ShotExecutedEvent` | A | Resolve | #6 (payload owner; not redefined here) | 1 | per Shot Mechanics #6 §2.4 | n/a | #17 v1.0 (registry seed) | N |
| `0x02` | `BallContactEvent` | A | Physics | #1 / #3 | 1 | `entityIdA: EntityId; entityIdB: EntityId; contactPoint: Vector3; relativeVelocityAtContact: Vector3; contactKind: byte` (per #3 §2 payload schema, not redefined here) | n/a | #17 v1.0 | N |
| `0x03` | `BallCrossedLineEvent` | A | Physics | #1 | 1 | `lineKind: byte; crossingPoint: Vector3; ballVelocityAtCross: Vector3` | n/a | #17 v1.0 | N |
| `0x04` | `PossessionChangedEvent` | A | Resolve | #17 (default owner) | 1 | `previousHolder: EntityId; newHolder: EntityId; reason: byte` | n/a | #17 v1.0 | N |
| `0x05` | `FoulCommittedEvent` | A | Resolve | #17 (default owner) | 1 | `offender: EntityId; victim: EntityId; location: Vector3; foulKind: byte` | n/a | #17 v1.0 | N |
| `0x06` | `CardIssuedEvent` | A | Resolve | #17 (default owner) | 1 | `recipient: EntityId; cardKind: byte; foulOrdinal: byte` | n/a | #17 v1.0 | N |
| `0x07` | `GoalAwardedEvent` | A | Resolve | #17 (default owner) | 1 | `scorer: EntityId; assister: EntityId; scoringTeam: byte; ballPosition: Vector3` | n/a | #17 v1.0 | N |
| `0x08` | `SubstitutionEvent` | A | Resolve | #17 (default owner) | 1 | `outgoing: EntityId; incoming: EntityId; team: byte; substitutionReason: byte` | n/a | #17 v1.0 | N |
| `0x09` | `TickHeartbeatEvent` | C | `AI_NoOp` (typical; Tier C — informational per §A.1 note) | #17 (default owner) | 1 | (header only) | 60 / tick (rate-limited to once per tick) | #17 v1.0 | N |
| `0x0A` | `VfxImpactCue` | C | Resolve | #17 (default owner) | 1 | `impactPoint: Vector3; impactKind: byte; intensity: byte` | 64 / tick | #17 v1.0 | N |
| `0x0B` | `UiNotificationCue` | C | Resolve | #17 (default owner) | 1 | `notificationKind: byte; subjectEntity: EntityId` | 32 / tick | #17 v1.0 | N |

### A.2 Reserved ordinals

| Ordinal | Status | Reason |
|---------|--------|--------|
| `0x00` | Reserved sentinel | "Invalid / unset"; MUST NOT be allocated. |

### A.3 Future-spec append slots (forward reference only)

| Expected ordinal range | Spec | Events (provisional names) |
|------------------------|------|----------------------------|
| `0x0C` … `0x0F` | #10 Heading Mechanics | `HeaderExecutedEvent` (A) |
| `0x10` … `0x13` | #11 Goalkeeper Mechanics | `SaveAttemptedEvent` (A), `BallParriedEvent` (A), `BallCaughtEvent` (A) |
| `0x14` … `0x17` | #13 Pressing AI | `PressTriggeredEvent` (A) |
| `0x18` … `0x1B` | #14 Defensive AI | `MarkAssignedEvent` (A) |
| `0x1C` … `0x1F` | #15 Attacking AI | `RunCalledEvent` (A) |

These rows are **forward references only** and are NOT part of the
Spec #17 v1.0 normative registry. Owning specs allocate the actual
ordinals at their IN REVIEW commit; collisions are prevented by
this single-table registry.

### A.4 Deprecated rows

(Empty at Spec #17 v1.0.) Future deprecations preserve the
`Ordinal` and `Type` columns with `Deprecated = Y` and a
`Deprecated in` column added when the first row deprecates.

## Appendix B — Canonical Byte Encoding Worked Examples

Each example serializes the `EventLedgerRecord` per §3.4.2
(`PhaseScopeFields[Events] = SerializeCanonical(DOMAIN_TAG_EVENT_LEDGER ‖
EventLedgerRecord)`). Bytes shown in hex; multi-byte integers
little-endian per #16 §3.2.4.1 `TBD-NORMATIVE`.

In every example below, `DT` is the symbolic `DOMAIN_TAG_EVENT_LEDGER`
byte (numeric value `TBD-NORMATIVE` per ERR-017-001).

### B.1 Empty `Events` phase

```
phaseScopeFields:
  DT
  count    = 00 00 00 00     // u32, no records
records: (none)
```

Total preimage = `DT 00 00 00 00` (5 bytes).

### B.2 Single-event ledger (`ShotExecutedEvent` only)

Scenario: tick `T = 0x00000123` (291); subsystem #6 shot resolver
publishes one Tier A `ShotExecutedEvent` from `Resolve`;
`intraPhaseDrawIndex = 0`. The 12-byte header for this event is:

| Field | Bytes |
|-------|-------|
| `eventTypeOrdinal` (`0x01`) | `01` |
| `payloadVersion` (`1`) | `01` |
| `_reserved` (canonical zero) | `00 00` |
| `tick` (`0x00000123`) | `23 01 00 00` |
| `subsystemOrdinal` (`0x0006`) | `06 00` |
| `intraPhaseDrawIndex` (`0x0000`) | `00 00` |
| **Header total** | `01 01 00 00 23 01 00 00 06 00 00 00` (12 bytes) |

The payload bytes follow per Shot Mechanics #6 §2.4 canonical
field order; payload contents are out of scope for #17 and are
denoted `<shotPayload>` below.

```
phaseScopeFields:
  DT
  count = 01 00 00 00     // u32, one record
record[0]:
  header  = 01 01 00 00 23 01 00 00 06 00 00 00
  payload = <shotPayload>
```

### B.3 Two-event mixed-producer ledger (sort-key demonstration)

Scenario, same tick `T = 0x00000123`:

- Producer A: `Physics` phase, subsystem `0x0003` (collision),
  publishes one `BallContactEvent` (`0x02`); `intraPhaseDrawIndex = 0`.
- Producer B: `Resolve` phase, subsystem `0x0011` (rules engine),
  publishes one `PossessionChangedEvent` (`0x04`);
  `intraPhaseDrawIndex = 0`.

Sort-key tuple `(producingPhaseIndex, subsystemOrdinal, entityId,
eventTypeOrdinal, intraPhaseDrawIndex)`:

| Record | producingPhaseIndex | subsystemOrdinal | entityId | eventTypeOrdinal | intraPhaseDrawIndex |
|--------|---------------------|------------------|----------|------------------|---------------------|
| BallContactEvent | Physics (per #16 §3.1.2 index) | `0x0003` | (least entity) | `0x02` | `0x0000` |
| PossessionChangedEvent | Resolve (per #16 §3.1.2 index — higher than Physics) | `0x0011` | (entity) | `0x04` | `0x0000` |

Physics-phase index is lower than Resolve-phase index per
#16 §3.1.2 `TBD-NORMATIVE`. The sort therefore emits
`BallContactEvent` first, `PossessionChangedEvent` second:

```
phaseScopeFields:
  DT
  count = 02 00 00 00
record[0] (BallContactEvent):
  header  = 02 01 00 00 23 01 00 00 03 00 00 00
  payload = <ballContactPayload>
record[1] (PossessionChangedEvent):
  header  = 04 01 00 00 23 01 00 00 11 00 00 00
  payload = <possessionChangedPayload>
```

This example demonstrates §3.2.4 sort-key total order (FM-017-002)
and §3.4.2 canonical layout (FM-017-001).

## Appendix C — Versioning Migration Recipes

Each recipe walks a KD-9 / §3.7 evolution path through Appendix A
plus the in-flight code change.

### Recipe 1 — Adding a payload field (allowed)

**Example:** `GoalAwardedEvent` (`0x07`) adds a `tackleAssistCount: byte`
field.

1. Append the new field at the **end** of the payload (canonical
   declaration order). Do NOT reorder existing fields.
2. Bump `payloadVersion` from `1` to `2`.
3. Update Appendix A `Current version` to `2`; append a new
   "Version-history sub-row" indicating the v1 → v2 transition
   and which spec/PR introduced the field. The v1 schema row is
   retained for replay-corpus compatibility.
4. Update the producing code to write the new field.
5. Update consumers per the `payloadVersion` dispatch (§3.7.2): a
   subscriber that reads v2-only fields branches on
   `evt.payloadVersion >= 2`.
6. Add a §5.3 P3 property-test case covering both v1-load and
   v2-load.

### Recipe 2 — Changing a field's width (forbidden in place)

**Example:** `intensity: byte` in `VfxImpactCue` (`0x0A`) is found
to need a wider range and would naturally widen to `ushort`. This
is forbidden in place (FR-EVT-058).

1. Allocate a new `eventTypeOrdinal` (next free slot, e.g.,
   `0x20`).
2. Define `VfxImpactCueV2` (or similar; renaming policy is
   producer-spec-local) with the wider field. New row in
   Appendix A; `Current version = 1`; tier preserved; producer
   phase preserved.
3. Mark `0x0A` as `Deprecated = Y` in Appendix A; add a
   `Deprecated in` column entry pointing at the IN REVIEW commit
   that landed the new ordinal.
4. Producers stop publishing `0x0A` in new code (FR-EVT-060).
5. Existing replay corpora continue to deserialise `0x0A` v1
   correctly because the row is retained.

### Recipe 3 — Deprecating an event type (no replacement)

**Example:** A telemetry event is judged redundant and is
deprecated outright.

1. Mark Appendix A row `Deprecated = Y`; add `Deprecated in`
   column entry.
2. Producers MUST NOT publish the deprecated ordinal in new code.
3. Consumers MAY continue to subscribe (Spec #17 does not break
   their compile) — useful for replay-corpus consumers.
4. The ordinal is **NEVER reused** for a new event (KD-9 /
   FR-EVT-004).

## Appendix D — Glossary

| Term | Meaning |
|------|---------|
| Event ledger | Per-tick authoritative store of Tier A / B events. Owned by the `Events` phase per #16 §3.6.1 `TBD-NORMATIVE`. |
| Cosmetic channel | Out-of-band Tier C delivery path. Immediate synchronous dispatch (§3.2.3); never part of authoritative state. |
| `eventTypeOrdinal` | Byte-wide stable identifier; never reused after publication (KD-9). Globally unique across all specs. |
| `payloadVersion` | Byte-wide append-only version on each event struct (KD-9). |
| Second-order dispatch | Re-entrant Tier A/B publish from inside a handler during the same-tick `Events` phase. Bounded by `MAX_EVENT_DISPATCH_DEPTH` (§3.2.5). |
| Tier A / B / C | Vocabulary owned by #16 §1.3.1 `TBD-NORMATIVE`. Cited (not redefined) by Spec #17. |
| `intraPhaseDrawIndex` | `ushort` counter, scoped per-tick per-producingPhase; reset at producing-phase entry; monotonic across all subsystems within that phase (§3.2.4 normative counter scope). |
| `DOMAIN_TAG_EVENT_LEDGER` | Single-byte domain tag prefixed to `EventLedgerRecord` before `SerializeCanonical`. Numeric value `TBD-NORMATIVE` per ERR-017-001; promoted to `[CROSS]` at #16 approval. |
| Phase / digest | Vocabulary owned by #16 (not redefined here). |

## Appendix E — Failure-Mode Decision Table

Parallel structure to #16 §3.10 failure-mode table. Each
edge-case ID maps to a trigger, observable behaviour, and error
code.

| EC ID | Trigger | Behaviour | Error code |
|-------|---------|-----------|------------|
| EC-017-001 | Tier A `Publish<T>` called from a phase other than `Events` (raw call bypassing queue) | Debug build: assertion fires + tick halts; Release build: `ERR_DS_PHASE_OWNERSHIP` raised by phase scheduler | `ERR_DS_PHASE_OWNERSHIP` (alias; #16 §3.6.1 `TBD-NORMATIVE`) |
| EC-017-002 | Tier A/B publish exceeds `EVENT_QUEUE_CAPACITY` within a single tick | Hard fail; tick halted; pre-fail snapshot written via `event-system.overflow` trace channel | `ERR_EVT_QUEUE_OVERFLOW` (`0x1701`) |
| EC-017-003 | Fixture load encounters `eventTypeOrdinal` not in Appendix A | Load fails; no partial deserialisation | `ERR_EVT_ORDINAL_UNKNOWN` (`0x1703`) |
| EC-017-004 | Fixture load encounters `payloadVersion > currentRegistryVersion` | Load fails; no partial deserialisation | `ERR_EVT_VERSION_INCOMPATIBLE` (`0x1704`) |
| EC-017-005a | Subscriber registers with wrong tier marker constraint (authoritative code → Tier C, etc.) | Registration rejected immediately; subscriber not added | `ERR_EVT_TIER_MISMATCH` (`0x1702`) |
| EC-017-005b | Runtime Tier A/B register/unregister attempt after boot phase ended | Registration rejected immediately; subscriber not added | `ERR_EVT_REGISTRATION_PHASE` (`0x1705`) |
| EC-017-006 | Second-order BFS dispatch depth exceeds `MAX_EVENT_DISPATCH_DEPTH` (8) | Hard fail; tick halted; trace channel records depth at failure | `ERR_EVT_QUEUE_OVERFLOW` (`0x1701`) |

Cross-references for each row land in §3.8 (mechanics) and §2.5
(error-code table).

## Appendices Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial appendices draft from `outline-detailed.md` v1.1. Appendix A seeded with 11 rows + forward reference table; Appendices B / C / D / E published. Generic-template stub headings (Derivations / Numerical Verification / Sensitivity Analysis) replaced with spec-specific structure per outline. |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. Appendix A `0x09` row producer phase set to `AI_NoOp` (H2). Appendix A §A.1 added column-semantics note: Tier A/B Producer phase normative, Tier C informational (L7). Renamed `producerSubsystem` → `subsystemOrdinal` in Appendix B B.2 header table (M4). Appendix E EC-017-005 split into 005a (tier-marker mismatch) and 005b (post-boot registration → `ERR_EVT_REGISTRATION_PHASE`) (L3). |
