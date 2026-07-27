# Save Migration & Versioning #50 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Loop classification

**#50 has no loop.** It is a **load-path** component (FR-MG-038): it runs once per file open, before any
subsystem is constructed, and never again for the life of that session. It does not appear in the 10 Hz
tactical loop, the 60 Hz physics loop, the world-day advance, or any per-tick tap.

That makes its performance section short, and makes the **one** cadence that is not once-per-load the only
interesting row: **classification runs once per save file on a load screen**, so a directory of thirty
careers is thirty classifications before the player has chosen one.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `Classify` | **once per save file listed**, then once on open | a bounded read of the frame version + each sub-blob's leading version — **no body parsed** (FR-MG-001) |
| `MigrationRunner.Run` — `Current` | the common case | **zero steps**; the file is not rewritten at all (FR-MG-019) |
| `MigrationRunner.Run` — one blob, one step | a typical update | one `Apply` over that blob; **every other blob copied byte-untouched** |
| `GenerationGate.Check` | once per open | an integer comparison plus a registry lookup |
| `Materialise` | **once, ever, per save** | runs a **frozen generator** over the whole league — the single expensive operation in the spec |
| `CommitMigration` | once per successful migration | write + fsync + a **full re-read through the current codec** + rename |

**Classification is cheap by construction, and that is a consequence of the format rather than of #50.**
Because the frame writes its version first and sub-blobs are length-prefixed and opaque (§1.4(d)), the
classifier seeks and reads a handful of integers. A load screen listing thirty saves therefore costs
thirty small reads, not thirty deserializations — which is the reason FR-MG-001's version-fields-only rule
is a performance property as well as a safety one.

**The `Current` path does no work at all**, and this is worth stating because it is what almost every load
does: no rewrite, no copy, no allocation beyond the bytes already read.

**`Materialise` is the outlier, and it is bounded by being once-only.** It runs a full league generation —
the same order of work as starting a new career — and then the save carries its rosters and never
regenerates again. A player sees it once, at the first load after an update that changed generation, on a
screen where a progress indicator is expected.

**`CommitMigration`'s re-read is deliberate cost.** `VerifyLoadable` parses the migrated file through the
real codec before the rename (§3.4). It roughly doubles the migration's I/O, and buys the property that a
migration whose output the codec would reject never reaches the save location.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `MG_BUDGET_CLASSIFY_MS` — one `Classify` | 5 ms | `[GT]` |
| `MG_BUDGET_STEP_MS` — one `IMigrationStep.Apply` over one blob | 50 ms | `[GT]` |
| `MG_BUDGET_RUN_MS` — one full `Run` over a whole save | 500 ms | `[GT]` |
| `MG_BUDGET_MATERIALISE_MS` — one generation materialisation | 5 000 ms | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #50 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #50 has no implementation to measure. They are generous so a first
measurement either passes comfortably or reveals something genuinely wrong — the `CertifiedPerfBaseline`
PENDING posture applied to a spec that has not been built.

**`MG_BUDGET_CLASSIFY_MS` is the one worth measuring first**, and not because it is the largest — it is by
far the smallest. It is the only one multiplied by **file count** rather than by a single user action, so
it is the only cost a player experiences without having asked for anything. An implementation that
accidentally deserializes a blob to classify it would blow through it immediately, which is exactly the
diagnostic FR-MG-001 wants.

**`MG_BUDGET_MATERIALISE_MS` is in seconds deliberately**, and the wide ceiling is honest rather than
lax: it performs a whole league generation, it happens once ever per save, and it happens on a screen
where waiting is expected. Pretending it belongs in a millisecond budget would misrepresent the operation.

**Nothing here touches the certified per-tick engine baseline.** `FR-PO-052`'s p50 = 0.4768 ms / p99 =
2.5669 ms is a tick figure; #50 has run to completion before the first tick exists.

## 6.4 Memory

| Quantity | Order |
|---|---|
| A classification | **tens of bytes** — a class, a few versions, a blob-kind |
| The registry | one entry per `(blobKind, fromVersion)` — **tens of entries**, static for the process |
| `GenerationRegistry` | one delegate per retained version — **single digits**, bounded by the floor (R-5) |
| A migration in flight | **the save file, twice** — the input bytes and the output bytes |
| After the load path | **0 bytes** — #50 holds nothing (FR-MG-038) |

**"The save file, twice" is the peak, and it is stated because it is the one place #50 is not free.** A
migration reads the whole file and builds a whole new one, so peak footprint is roughly double the save
size for the duration of one load — acceptable for a save measured in hundreds of kilobytes to low
megabytes, and unavoidable given FR-MG-024's non-destructive write. **Streaming the migration to reduce it
would trade the property that the original is untouched until the new file is verified**, which is the
trade R-3 exists to refuse.

**#50 holds nothing after the load path** and its own additions to the save are two integers
(Appendix B). Nothing in #50 grows with career length, with league size, or with the number of migrations
a save has been through — a migrated save carries no migration history, and deliberately so: a chain of
"was migrated from" records would be a second, unversioned format inside the version system.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (a load-path component with no loop at all, so the interesting cadence is the one multiplied by **file count** — classification on a load screen — rather than the largest operation; cost profile with the zero-work `Current` path named as what almost every load does; `[GT]` ceilings with `MG_BUDGET_CLASSIFY_MS` flagged as the first to measure and `MG_BUDGET_MATERIALISE_MS` deliberately in seconds; memory with the double-the-save-file peak stated as the one place #50 is not free, and the reason streaming it would trade away the non-destructive-write property). Status IN REVIEW. |
#endregion
