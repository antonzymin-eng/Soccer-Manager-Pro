# National Teams & International Management #36 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-NT-U-*` unit, `T-NT-I-*` integration, `T-NT-DET-*` determinism,
`T-NT-ID-*` identity / behaviour-neutrality, `T-NT-FAIL-*` fail-loud, `T-NT-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.8** or is a relational property. **No test pins a
specific `Derive` output** — that would be a fabricated hash; the derivation is asserted relationally
(§5.2).

## 5.1 The two locks this spec exists for

These are listed first because they are the tests that distinguish a correct implementation from one that
passes everything else.

| ID | Test |
|---|---|
| T-NT-I-001 | **The transfer lock.** A called-up player is transferred mid-season (a #31 re-key). `NationOf` **before and after are equal**, and the value carried is the **pre-transfer** nation — asserted against a `newPlayerId` whose *own* derivation differs (§3.8(e)/(f)). **A test asserting only "a pin exists after a transfer" passes against the F4 bug**, so this one must assert the value. Without the pin, this is the test that fails; with a mis-ordered hook, this is the test that fails. |
| T-NT-DET-001 | **The golden-vector lock.** `LeagueBootstrapGoldenVectorTests`' digest and every `RosterGenerator` digest are **unchanged** by #36 — asserted **explicitly inside #36's own suite**, not merely relied on from #27's. A later maintainer who "just adds the nationality field" then fails **#36's** tests as well as #27's, which is where the cost needs to be visible (§7.4 R-4). |

## 5.2 Unit — nationality (§3.1)

| ID | Test |
|---|---|
| T-NT-U-001 | **`Derive` is deterministic and pure**: the same `(worldSeed, playerId)` yields the same `NationId` across calls, across instances, and across a save/restore. Asserted relationally — no literal output is pinned. |
| T-NT-U-002 | §3.8(a)/(b)/(c): the inverse-transform walk lands in the band the `[GT]` weights define, swept across the full `[0, NT_WEIGHT_TOTAL)` acceptance range with a synthetic three-member catalogue. |
| T-NT-U-003 | **Distribution shape**: over a generated league the observed nation frequencies track the `[GT]` weights within a tolerance. Asserted as a *shape* property, never as exact counts, so a reweighting cannot invalidate a passing suite. |
| T-NT-U-004 | **`NT_WEIGHT_TOTAL` equals the catalogue's weight sum** (Appendix A.2's `[DERIVED]` contract) — the invariant that makes §3.1's final `throw` unreachable. Locked so a maintainer cannot set the total independently. |
| T-NT-U-005 | **A pin wins over the derivation** (FR-NT-007), and an **absent** pin falls through silently — absence is the normal state, not an error (§2.3). |
| T-NT-U-006 | **`NationId.None` and undefined ordinals fail loud** at every consuming seam and on decode (F1) — including a corrupt *pin*, which must throw rather than silently fall through to the derivation. |
| T-NT-U-007 | **Catalogue ordinal stability** (FR-NT-009): each member's ordinal equals its pinned value and the member count matches. §3.8(d) is why this is a **save-correctness** lock — inserting a member shifts every subsequent acceptance band and re-nationalises every unpinned player in every existing career. |

## 5.3 Unit — the re-key hook (§3.2)

| ID | Test |
|---|---|
| T-NT-U-008 | **Ordering** (F4): a hook that resolves **after** the re-key is detectable — the test constructs the case where the post-transfer derivation differs and asserts the pinned value is the **pre**-transfer one. This is T-NT-I-001's unit-level half. |
| T-NT-U-009 | §3.8(g): **a pin equal to its derivation is still written** (FR-NT-012). The "skip the redundant pin" optimisation fails here — and the test names why: the coincidence does not survive the *next* transfer. |
| T-NT-U-010 | The **old** id's pin is removed and the **new** id's is written in one operation; no state observes both. |
| T-NT-U-011 | A pin written for a player who has **not** been re-keyed or authored **throws** (F3) — the guard that keeps the table bounded by transfer volume rather than pool size. |
| T-NT-U-012 | §3.8(h): a **retired** player's pin is **dropped** (FR-NT-013), and across a full season of churn the pin table **does not grow monotonically**. The second half catches a partial implementation that stops iterating the player but leaves the row. |

## 5.4 Unit — selection, window, and filter (§3.3 / §3.4 / §3.5)

| ID | Test |
|---|---|
| T-NT-U-013 | §3.8(i): **the per-club cap is applied during the walk** — 5 eligible from one club with cap 3 yields 3 plus the next-best from *other* clubs, **not** a post-hoc trim. Asserted against the trimmed alternative, which produces a different squad. |
| T-NT-U-014 | §3.8(j): **the `PlayerId` tie-break makes the ranking total.** With identical mean attributes the lower id wins, and the selection is **independent of enumeration order** — asserted by permuting the input pool. Mean attributes tie constantly in a generated league, so this is the common case, not a corner. |
| T-NT-U-015 | Selection is **idempotent within a window**: re-running on the same `worldDay` rebuilds the identical canonical list. |
| T-NT-U-016 | Selection is **draw-free** (FR-NT-021): a full season of selections leaves every registered RNG cursor byte-identical. |
| T-NT-U-017 | §3.8(k): re-advancing the same `worldDay` is a **no-op**. |
| T-NT-U-018 | §3.8(l): a day **gap** **throws** (F5). Paired with T-NT-U-017 so the two halves cannot drift apart — and #36 needs this guard where #53 does not, because a skipped window *skips call-ups* (§3.3). |
| T-NT-U-019 | The window is a **read-only** derivation: `SeasonCalendar` is **field-unchanged** after any number of `CurrentWindow` / `IsWindowDay` / `AdvanceWindowDay` calls (FR-NT-015). |
| T-NT-U-020 | `FilterAvailable` **outside** a window is the **identity** — the squad is returned field-identical. |
| T-NT-U-021 | `FilterAvailable` is a **pure removal** (FR-NT-017): the output is a subset of the input, and no player is added or substituted. The property §3.5's order-independence proof rests on. |

## 5.5 Integration — filter composition and the seam

| ID | Test |
|---|---|
| T-NT-I-002 | §3.8(m): **order-independence.** With #44 suspending `{3,7}` and #36 calling up `{7,11}`, the composed squad is identical in **both** orders — asserted by running both, not by arguing set theory. |
| T-NT-I-003 | The overlap case is handled: a player both suspended **and** called up is removed once, and the result has no duplicate removal or double-count. |
| T-NT-I-004 | §3.8(i)/FR-NT-018: `NT_MAX_CALLUPS_PER_CLUB` bounds **#36's own** contribution to any one club's reduction, in every window. |
| T-NT-I-005 | **The empty-squad floor is observable but not #36's to resolve** (F7 / FR-NT-019): the test constructs a composition that reduces a squad below a fieldable eleven and asserts the **seam's** defined behaviour, whatever ERR-030-016 settles on — so #36's suite documents the shared obligation rather than inventing a private policy for it. |
| T-NT-I-006 | A call-up is **returned** after the window closes: the same squad, on a later day, is field-identical to the pre-window squad. |

## 5.6 Integration — save / restore and the roster lifecycle

| ID | Test |
|---|---|
| T-NT-I-007 | State → `Encode` → `Decode` is **field-identical**: an empty store, a mid-window selection, the window cursor at its sentinel and at a real day, an `IntlMinutes` set, and a populated `NationPin` table. |
| T-NT-I-008 | **`NationOf` survives a save/restore for a pinned player as well as an unpinned one** (FR-NT-032). The unpinned case is free — nothing is stored. **The pinned case is the one that can regress**, and it is exactly the case a transfer creates: omitting the pin table from the blob reverts a transferred player to the derivation of his new id on the next load. |
| T-NT-I-009 | Round-trip through a full `SeasonSaveCodec` frame: #36's sub-blob is **opaque** to the outer codec, and the world / season / match / sibling blobs are **byte-unchanged**. |
| T-NT-I-010 | The two format versions move **independently**. |
| T-NT-I-011 | **Canonical ordering** (FR-NT-024): a store built by inserting call-ups in permuted order serializes **byte-identically**, and a non-canonical or duplicated entry on decode **throws** (F8). |
| T-NT-I-012 | **`CallUp` migrates on a re-key but is dropped on retirement** (FR-NT-023) — asserted as **one** test over one scenario, alongside the `NationPin` rules, so a later "consistency" pass that unifies them the wrong way fails here rather than in play. The contrast is deliberate: #44's ban rule, not #32's knowledge-drop rule. |

## 5.7 Structural (the boundaries #36 must not cross)

| ID | Test |
|---|---|
| T-NT-BOUND-001 | **#36's assembly references only #27**, at every tier — asserted from the reference set, so a future `using` of #30 / #43 / #44 / #29 / #41 / `SeasonSave` / `MatchEngine` fails the build's test gate (FR-NT-003). **Unconditional**, because #36 is a leaf at the deep tier too. |
| T-NT-BOUND-002 | **#36 does not implement, name, or reference `ISquadProvider`** (FR-NT-004). Asserted over the compiled surface, and flagged as *the one a deep-tier implementer breaks first* — the move is natural, the precedent appears to endorse it, and it would silently collapse T-NT-BOUND-001. |
| T-NT-BOUND-003 | **No `PlayerRecord` write and no `Squad` mutation**: a `PlayerRecord[]` and a `Squad` handed alongside every #36 entry point are **field-unchanged** after derivation, selection, filtering, resolution and save/restore. Asserted behaviourally — #27 is referenced, so the reference graph cannot prove it (§4.6 standing item). |
| T-NT-BOUND-004 | **`SeasonCalendar` is never mutated** (FR-NT-015) — the same behavioural assertion for the value #36 derives its window from. |
| T-NT-BOUND-005 | **No `RegisterStream` call exists at any tier** (FR-NT-029), asserted over the compiled surface. |
| T-NT-BOUND-006 | **#36 declares no type named `ISquadProvider`, `Squad`, `PlayerRecord`, or `SeasonCalendar`** — the parallel-surface lock. |

## 5.8 Identity / behaviour-neutrality (KD-7)

| ID | Test |
|---|---|
| T-NT-ID-001 | **With no window configured**, no player is ever withdrawn: every squad reaching `ConfigureSquads` is **byte-identical** to pre-#36, and every registered RNG cursor is byte-identical. |
| T-NT-ID-002 | A season advanced with #36 present is byte-identical to the same season pre-#36 **except #36's own sub-blob** — the #44 FR-DC-018 formulation, adopted verbatim because it is the honest one. Stating it unqualified would claim a guarantee #36 does not offer. |
| T-NT-ID-003 | **Reading nationality moves no byte outside #36**: with eligibility evaluated for every player in the pool, `PlayerRecord`s, rosters and the golden vector are all unchanged. This is the true claim; *"nothing reads it"* would be **false**, since eligibility *is* the minimal tier. |

## 5.9 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-NT-FAIL-001 | `NationId.None` / an undefined ordinal at any seam or on decode ⇒ throws (F1). |
| T-NT-FAIL-002 | A `NationTeamId` **below** `NATION_TEAM_ID_BASE` ⇒ throws (F2) — on both sides of the seam, in #36 **and** at the root composite, so the guard does not rely on the router alone. |
| T-NT-FAIL-003 | A stray `NationPin` write ⇒ throws (F3). |
| T-NT-FAIL-004 | A re-key hook that cannot resolve the pre-transfer nation ⇒ **throws** rather than pinning the post-transfer derivation (F4). |
| T-NT-FAIL-005 | A day gap ⇒ throws (F5); the same day ⇒ no-op. |
| T-NT-FAIL-006 | A selection exceeding `NT_MAX_CALLUPS_PER_CLUB` ⇒ throws (F6). |
| T-NT-FAIL-007 | Decode: wrong `NATIONAL_TEAM_SAVE_FORMAT_VERSION` ⇒ throws, version read **first** (F9). |
| T-NT-FAIL-008 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound against `total − offset`, never wraps (F9). |
| T-NT-FAIL-009 | Decode: trailing bytes ⇒ throws (F9). |
| T-NT-FAIL-010 | §3.8(o): `TryResolveNationSquad` for a national team with **no call-ups** returns **`false`** — a named legal state, not a throw. |

## 5.10 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `international-window-across-a-season`, owning specs
`{16, 19, 27, 30, 31, 36, 44}`, registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

play a season with a window configured and #44's suspensions active; assert that a managed-club fixture
falling in the window fields a squad reduced by **both** filters and no more; **transfer a called-up
player mid-window**, then assert his nationality is unchanged and his call-up followed him; save
mid-window; restore; and assert the selection, the pin table and every subsequent squad match an
uninterrupted run — then assert `LeagueBootstrapGoldenVectorTests`' digest is still green.

This is the composition-level proof that KD-1's pin, KD-2's filter composition, KD-6's blob and KD-7's
identity hold **together**, and it is the only place the transfer, the window and the save interact at
once — which is precisely where a pin omitted from the blob (FR-NT-032) or a mis-ordered hook (F4) would
survive every unit test and still be wrong.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.1 leads with the two locks the spec exists for — the transfer lock (which a test asserting only *"a pin exists"* would pass against the F4 bug) and the golden-vector lock (asserted **inside #36's own suite**, so the cost of "just adding the field" is visible to whoever tries). T-NT-BOUND-002 is flagged as the assertion a deep-tier implementer breaks first, since implementing `ISquadProvider` is the natural move and the `League` precedent appears to endorse it. Status IN REVIEW. |
#endregion
