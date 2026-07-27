# New-Game Setup & Database Editor #47 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#47 is **tooling**. Nothing in it runs on the world tick, the season boundary, the 10 Hz tactical loop or
the 60 Hz physics loop; no #47 type is reachable from `MatchEngine.RunTick`; and #47 feeds no digest
(FR-ED-029, asserted structurally by T-ED-BOUND-001/002).

**It has no cadence at all.** Every #47 operation is triggered by a human: opening the editor, committing
an edit, starting a game, saving. There is no per-day, per-fixture or per-player call path anywhere in the
spec.

**So the performance question #47 actually poses is not time — it is save size** (§6.4). That is the
unusual thing about this spec's cost profile, and it is where the attention belongs.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `Write(squad)` | per commit / per export | O(players × attribute keys) string building |
| `Parse` *(#27's)* | per commit — **every** commit (FR-ED-017) | O(text length); #27's cost, not #47's |
| `BuildConfig` | once per new game | 4 field copies; **no validation** (FR-ED-022) |
| `FromAuthored` *(`season-save`'s)* | once per authored new game | O(clubs + players) validation + construction |
| `LeagueBootstrap.Generate` | once per generated new game | **unchanged** — #47 adds nothing to it |
| `AuthoredDbCodec.Encode` / `Decode` | once per save / load, **authored games only** | O(clubs + players + pins) |
| View-model projection | per screen refresh | O(visible rows), value copies |

**Parsing on every commit is a deliberate cost, and a small one.** FR-ED-017 routes every edit through
`SquadFileLoader.Parse` rather than through an editor-side check, which means re-parsing a squad's text on
each commit. At 25 players and a few dozen keys each that is a sub-millisecond operation at human cadence
— and the alternative is the second validation authority KD-2 exists to prevent. **The cost is the
mechanism**, not an inefficiency to optimise away later.

**Allocation is unbounded by design in one place and bounded everywhere else.** `Write` builds a string
proportional to the squad; the codec allocates its buffer once. Both run off any loop, at human cadence,
so neither is subject to #18's zero-allocation hot-path discipline — and stating that explicitly matters,
because a reviewer applying the game-loop rules to an editor would reject a correct design.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `ED_BUDGET_WRITE_MS` — one `Write` over a full squad | 5 ms | `[GT]` |
| `ED_BUDGET_COMMIT_MS` — one commit, **including** the `Parse` round-trip | 20 ms | `[GT]` |
| `ED_BUDGET_CODEC_MS` — one authored-artifact encode or decode | 200 ms | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #47 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #47 has no implementation to measure. The ceilings are generous so a
first measurement either passes comfortably or reveals something genuinely wrong — the
`CertifiedPerfBaseline` PENDING posture applied to a spec that has not been built.

**All three are in milliseconds, and that is correct rather than lax.** These are human-cadence
operations: a commit that takes 20 ms is imperceptible, and holding an editor to a loop-step microsecond
budget would be a false constraint imported from a different layer. `ED_BUDGET_CODEC_MS` is the loosest
because it scales with the whole database and runs once per save.

## 6.4 Memory and save size — the cost that actually matters

**The authored sub-blob is the only place #47 spends anything the player will notice.** A `Squad` of 25
players carries #27's full attribute set per player; across a 20-club league that is 500 player records in
the save.

| Quantity | Order |
|---|---|
| One `PlayerRecord` (31 attributes + identity + name strings) | ~150–250 bytes |
| One 25-player squad | ~5 KB |
| A 20-club authored league | **~100 KB** |
| A generated league's contribution | **0 bytes** |

**That last row is the point.** A generated career pays **nothing** — no block, not even an empty one
(FR-ED-012) — and the entire cost is conditional on the player having authored something. #47's
save-format footprint is opt-in in the strongest sense available.

**~100 KB is acceptable and is stated rather than assumed.** It sits alongside a season save that already
carries the living-world composite, the match blob and half a dozen management sub-blobs; #22 is the spec
that needed compression machinery, and #47 does not approach that scale. But it is **two orders of
magnitude larger than any other management block**, which is why it is called out here rather than left
for someone to discover.

**It does not grow with career length.** The artifact is written once at genesis and is thereafter a fixed
payload — unlike #46's inbox or #54's career record, both of which accumulate. Progression, transfers and
ageing move the *live* rosters, which are #27/#30's state; the authored database records the **starting**
world and never changes again.

**#47 therefore needs none of the `SAVE_SIZE_BUDGET` compression or cold-store machinery #22 carries** —
recorded so its absence is not read as an omission, and with the scale named so the judgement can be
re-checked if the roster model grows.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (tooling classification with no cadence at all, cost profile noting that per-commit parsing **is the mechanism** rather than an inefficiency, `[GT]` ceilings in milliseconds with the reason that is correct for human-cadence operations, and §6.4 identifying **save size** as the cost that actually matters — ~100 KB for an authored league, **0 bytes** for a generated one, and fixed rather than growing with career length). Status IN REVIEW. |
#endregion
