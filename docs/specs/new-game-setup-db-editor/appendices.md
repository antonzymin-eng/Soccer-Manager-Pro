# New-Game Setup & Database Editor #47 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #47 has no `[EST]` constants and — because it takes **no determinism
reservation** (KD-6) — **no `[CROSS-PENDING]` constants either**, so neither region appears.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `AUTHORED_DB_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The authored sub-blob's own version gate (KD-1(ii)). Independent of `SEASON_SAVE_FORMAT_VERSION`. **The only version #47 owns** — and it governs a block that exists only for authored games. |
| `AUTHORED_CLUB_STRENGTH_DELTA` | `0` | `[FIXED]` | The `StrengthDelta` every authored club takes (FR-ED-009). **`[FIXED]`, emphatically not `[GT]`:** it is not a balance dial but the statement that **no ramp is applied**. Making it tunable would re-open exactly the silent re-tuning the rule exists to prevent, and `season-save`'s factory guards it (§3.3). |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `AUTHORED_DB_MAX_CLUBS` | `LeagueBootstrapConstants.MaxClubCount` | `[DERIVED]` | An authored league is subject to the **same** club-count ceiling as a generated one. Derived rather than duplicated: a second copy would drift the moment `MaxClubCount` moved, and the two origins must accept the same league sizes. |
| `AUTHORED_DB_SQUAD_SIZE` | `PlayerDatabaseConstants.CLUB_SQUAD_SIZE` | `[DERIVED]` | Same reasoning — the artifact stores #27's `Squad`, so #27's size contract governs. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `Squad`, `PlayerRecord`, `PlayerAttributes` | #27 | Stored **outright** in the artifact (FR-ED-004). |
| `SquadFileLoader.Parse` | #27 | **The single validation authority** (FR-ED-017) and the arbiter of the writer's correctness (FR-ED-018). |
| `PlayerDatabaseConstants` bounds (`[1,20]`, `[1,5]`, age range) | #27 | Enforced **by the loader**, never re-declared here — see the note below. |
| `League`, `Club`, `Club.StrengthDelta` | `season-save` | **Never constructed by #47** (FR-ED-003); `AuthoredClub` is deliberately not named `Club`. |
| `LeagueBootstrapConstants.MaxClubCount`, `MaxRngStreams` | `season-save` | Enforced **by `LeagueBootstrap.Generate`**, whose exceptions #47 surfaces rather than pre-checking (FR-ED-023). |
| `NationPin` | #36 | Authored entries in **#36's** table; no parallel store (FR-ED-025). |
| `NamedSlotSet` | #49 | Where the root/#38 renders an authored proper noun as a **slot value** (FR-ED-031). |

### A.4 GT (budget ceilings only)

| Constant | Value | Notes |
|---|---|---|
| `ED_BUDGET_WRITE_MS` | `5` | §6.3 ceiling for one `Write` over a full squad. A **ceiling, not a measurement** — no certified number exists for #47. |
| `ED_BUDGET_COMMIT_MS` | `20` | §6.3 ceiling for one commit **including** the `Parse` round-trip. Same caveat. In **milliseconds** deliberately: a human-cadence operation should not carry a loop-step budget. |
| `ED_BUDGET_CODEC_MS` | `200` | §6.3 ceiling for one authored-artifact encode or decode. Same caveat; the loosest, because it scales with the whole database and runs once per save. |

**#47 declares no `[GT]` constant that governs behaviour, and the absence is deliberate.** Every bound it
might be tempted to declare — attribute ranges, the age window, club-count limits — belongs to
`SquadFileLoader` or `LeagueBootstrap`, and a #47-side copy would be **the second authority KD-2 forbids**
(FR-ED-017/023). The three rows above are performance ceilings, which govern nothing the player can
observe.

**There is consequently no `[GT]` balance pass for #47** (§9.4). In a wave where every sibling carries
one, that absence is a classification rather than an omission.

## Appendix B — Authored sub-blob layout (KD-1(ii))

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** — the
outer codec sees a length-prefixed byte block and never parses it (FR-ED-011).

**This block is written only when `HasAuthoredDb`.** A generated game writes **no block at all — not an
empty one** (FR-ED-012), which is what preserves byte-identity with pre-#47 rather than approximating it.

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `AUTHORED_DB_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below it is interpreted (F6). |
| 2 | `ClubCount` | `i32` | Length prefix — read through the overflow-safe bound compared against `total − offset`, never `offset + need` (F6). Bounded by `AUTHORED_DB_MAX_CLUBS`. |
| 3 | per club × `ClubCount` | — | `ClubId` (`i32`); `Name` (length-prefixed UTF-8, **stored as authored** — no locale baked, no key allocated, FR-ED-016). **Ascending `ClubId`, no duplicates** (F5). |
| 4 | `SquadCount` | `i32` | Length prefix, same bound treatment. Must equal `ClubCount` — one squad per club. |
| 5 | per squad × `SquadCount` | — | `ClubId` (`i32`); `PlayerCount` (`i32`); then each `PlayerRecord` in **ascending `PlayerId`** — identity, name, age, position, and #27's full attribute set. |
| 6 | `NationPinCount` | `i32` | Length prefix, same bound treatment. |
| 7 | per pin × count | — | `PlayerId` (`i32`); `NationId` (`i32`). Ascending `PlayerId`, no duplicates. |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F6). |

**Everything is canonically ordered** (FR-ED-014), so the blob is a function of **state** rather than of
the order the author happened to add things. Without it, two equivalent databases serialize differently
and the save stops being comparable.

**Decode validates, it does not trust** (FR-ED-015): ascending-unique ids at every level (F5); one squad
per club; `PlayerCount` within `AUTHORED_DB_SQUAD_SIZE`; and the block's presence agreeing with the
save's `HasAuthoredDb` marker (F8).

**Two load-side rules that are easy to get wrong, and both fail loud:**

1. **A missing block on an authored save throws** (F7). The natural fallback — regenerate — produces the
   **silent wrong world** §1.4(a) describes: the career loads, nothing errors, and the player's authored
   league is quietly replaced.
2. **A flag/blob mismatch in either direction throws** (F8). A generated save carrying a stale authored
   block would load the wrong rosters just as silently.

**Deliberately absent — three things, each for its own reason:**

1. **Any hash or path referencing an external file.** The rejected design (§4.5): smaller in the save, and
   it makes a career depend on a file the player can move, edit or lose, with a mismatch **stranding the
   save with no recovery path**. `MatchSaveManager`'s self-sufficiency is the decisive precedent.
2. **Any `StrengthDelta`.** Authored clubs take `0` and no ramp is applied (FR-ED-009). **This is the
   point of temptation** — a serialized delta field would look like a natural extension and would silently
   re-tune every authored player away from what the author typed.
3. **Any locale identifier or translated string.** Authored proper nouns are stored as authored
   (FR-ED-016/031); the sub-blob is locale-independent under FR-LC-006, so a save written in one language
   loads identically in another.

**APPEND-only** (FR-ED-015). New fields go at the **end** behind a version bump. Note that #27 adding an
attribute widens row 5 — which **is** a layout change and **does** require a bump, unlike the enum-append
cases elsewhere in this wave, because the record is serialized field-by-field rather than by ordinal.

## Appendix C — Authored vs generated: the two `League` origins

| | **Generated** | **Authored** |
|---|---|---|
| **Origin** | `LeagueBootstrap.Generate(worldSeed, clubCount)` | `season-save`'s `FromAuthored(Club[], Squad[])` (ERR-030-018) |
| **Rosters come from** | the world seed, **regenerated on every load** | the **saved artifact** (Appendix B) |
| **Generator runs?** | yes | **no** — source, not patch (FR-ED-007) |
| **`Club.StrengthDelta`** | the seeded ramp | **`0`, no ramp applied** (FR-ED-009) |
| **Differentiation between clubs** | the ramp over uniform draws | the **authored attributes themselves** |
| **Save footprint** | **0 bytes** — no block at all | ~100 KB for a 20-club league (§6.4) |
| **Save frame vs pre-#47** | **byte-identical** (T-ED-ID-002) | gains one opaque sub-blob |
| **Golden vector** | **unchanged** (T-ED-ID-003) | unchanged — generation is not touched |
| **`ISquadProvider` shape** | — | **identical** (FR-ED-008): nothing downstream branches on origin |
| **Determinism basis** | the seed | **the saved data**, with no dependence on generation order |
| **Grows with career length?** | n/a | **no** — written once at genesis and fixed thereafter |

**The right-hand column is entirely opt-in.** Everything #47 costs — the block, the version, the ~100 KB —
appears **only** when the player has authored something (KD-7). That conditionality is the spec's central
claim, and §5.2 asserts both halves of it.

**The one row that is not merely a difference but a rule** is `StrengthDelta`. A reviewer comparing the
two origins will notice authored clubs lack the ramp and may read it as an omission; it is the design
(FR-ED-009), the factory guards it (§3.3), and applying it would be a **silent** defect — every authored
player slightly re-tuned, with no error and no visible cause.

**Not tabulated: the authoring grammar.** It is **#27's**, it is explicitly *"NOT a determinism-pinned
wire format"*, and #47's writer is specified **against the parser** by round-trip (FR-ED-018). Documenting
the grammar here would create the second definition FR-ED-021 exists to prevent — and would break at the
Stage-0+1 parser swap, which #47's type-binding is designed to survive.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed with the `[FIXED]`-not-`[GT]` argument for the zero strength delta, A.2 Derived, A.3 Cross, A.4 GT budget ceilings only; B the conditional sub-blob with its two load-side fail-loud rules and three deliberately-absent items; C the authored-vs-generated comparison). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** the three `[GT]` budget ceilings declared in §6.3 were **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline (the #45 PASS-1 M-2 defect, now seen for the sixth time in this wave) — added to A.4. **L:** A.1 gained the reason `AUTHORED_CLUB_STRENGTH_DELTA` is `[FIXED]` rather than `[GT]` (it is not a dial but the statement that no ramp applies); A.2 added, deriving the club-count and squad-size ceilings from their owners rather than duplicating them; A.4 gained the explicit note that **#47 declares no behavioural `[GT]` at all**, and that this is why it carries no balance pass; B gained the decode-validates paragraph, the two load-side rules, the `StrengthDelta` *point of temptation*, and the note that a #27 attribute addition **does** require a version bump here (unlike the enum-append cases elsewhere in the wave); C added as a side-by-side, since the two origins' differences are exactly what a reviewer will check. |
#endregion
