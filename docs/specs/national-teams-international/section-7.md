# National Teams & International Management #36 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `NationId` + `NationCatalogue` + the pin-then-derive `NationOf` + `CallUpSelector` + the window derivation + `FilterAvailable`, and their tests — **including the golden-vector lock** (T-NT-DET-001). Nothing wired into #30. | **Inert** — no caller exists |
| **T1** | `NationalTeamSaveCodec` + the round-trip / fail-loud / canonical-ordering suite, **including the pin table** (FR-NT-032). Still not composed into the season save. | **Inert** |
| **T2** | **First non-inert phase.** Wire `FilterAvailable` as the **second** consumer of #30's FR-SN-013 seam; wire the window advance at #36's tick position; wire #31's re-key hook to `OnPlayerReKeyed`; compose the sub-blob (bumps `SEASON_SAVE_FORMAT_VERSION`). | **Live.** Behaviour-neutral **only while no window is configured** — the moment one is, players are withdrawn, which is the feature |
| **T3** | The Stage-5 tier: international fixtures and tournaments as **#43 instances**, `TryResolveNationSquad`, the root's composite `ISquadProvider`, qualification, the national-team job, and routing minutes into #29/#41. | **Gated on the global sim** — not on #36 |

**T2's neutrality is conditional, and the condition is a configuration rather than a code path.** With no
window configured nothing is withdrawn and every squad is byte-identical to pre-#36 (T-NT-ID-001). Configure
one and squads change — that is the whole point of the minimal tier, and it is why §5.8 states the identity
claim with its precondition attached rather than unqualified.

**The re-key hook must land at T2, not T3.** It is tempting to defer it with the rest of the deep tier, but
transfers happen from the moment #31 is live, and every transfer without the hook writes a **silently wrong
nationality** into a career (§3.1.1). If #31 is live before #36's T2, the hook is the *first* thing #36
wires, not the last.

## 7.2 The Stage-5 gate — what is and is not blocked

**Authorable now** (needs only the managed league's own player pool):

- eligibility derivation, call-up selection, the window schedule, withdrawal and return;
- the persistence and determinism contracts for all of it;
- the pin table and the re-key hook.

Every one is exercisable and testable against a single generated league.

**Gated on the Stage-5 global sim** (needs rosters for nations #30 does not simulate): playing an
international fixture, tournaments, qualification, and the national-team job. **Not because the machinery
is missing** — KD-3 shows #43 already has all of it — but because an **opponent nation has no players to
field**.

**Why the minimal tier is still worth shipping.** Withdrawal is the half that touches the player's actual
career: a squad losing three starters to an international window is a real, felt consequence with no
international match rendered anywhere, and it exercises the whole eligibility/selection/persistence path
the deep tier then reuses unchanged.

The alternative — defer #36 entirely to Stage 5 — leaves `_RESERVED_0x28_` and the **nationality
question** open across every intervening spec, and #47's database editor would land with **no owner for
the nationality field it must edit**. That is the argument that decides it: the gap is not neutral while
it stays open.

## 7.3 Deep-tier extensions (designed for, not built)

- **International fixtures and tournaments as #43 instances** — group stages, knockouts, qualification.
  A #36-side *use* of #43's existing API, not a #43 change (KD-3).
- **The manager's national-team job** — a second employer, which interacts with #54's tenure model rather
  than with #36's selection model.
- **Routing international minutes into #29/#41** — committed integers on their existing per-day inputs
  (KD-4 / FR-NT-028). Deliberately **not** built until minutes exist.
- **Injury-forced replacement call-ups** — the one plausible **#36-owned stochastic** surface. If it ever
  lands, it is the first draw site and `_RESERVED_0x28_` promotes **there**, as an explicit decision
  (FR-NT-030). Recorded so a future implementer does not treat the reserved slot as pre-authorised.
- **Multi-nation eligibility** (a player qualifying for two nations, choosing one) — an additive pin-like
  declaration over the same table, since a *chosen* nation is exactly a pin.
- **Youth / age-group national teams** — additional ids in the same reserved range, sharing the whole
  selection and withdrawal path.

## 7.4 Explicitly not planned

- **A `PlayerRecord` nationality field.** Not at any tier (FR-NT-001). See R-4 — this will be proposed
  again, and the cost lives in #27's *test* discipline rather than its API, which is what makes it easy
  to underestimate.
- **A nationality cache.** The derivation is four integer rounds; a cache keyed by `PlayerId` would go
  stale at exactly the event the pin table exists to handle, and **silently** (§6.2).
- **#36 implementing `ISquadProvider`.** Not at any tier (FR-NT-004 / T-NT-BOUND-002). The root composes.
- **A #36-owned fixture generator, table, bracket, or draw.** #43 owns all of it (FR-NT-025).
- **A #36 policy for the empty-squad floor.** It is a property of the **composition** of two filters, so
  it belongs to the seam (FR-NT-019). Each filter growing its own guard is how the two end up
  disagreeing.
- **A fake international match against synthesised opponents**, to make the minimal tier feel complete.
  That would be canon invented by a consumer — exactly what FR-LW-031 exists to prevent (R-5).

## 7.5 Risks carried

- **R-1 — the nationality distribution is a save-visible `[GT]`.** Changing the catalogue or its weighting
  changes `NationOf` for **every existing player in every existing career**. It is a `[GT]` whose edits
  behave like a schema change, and FR-NT-014 says so; if it ever needs pinning, #27's golden-vector
  discipline for rosters is the right model.
- **R-2 — #47's authored nationalities land in the table #36 already ships.** The `NationPin` table
  exists at approval (for re-keys), and an authored entry is a pin like any other — so **#47 needs no new
  #36 surface**. What #47 *will* need is a **precedence policy** for when both an authored edit and a
  re-key pin exist for one player. That is #47's decision, and it is cheap only because the surface is
  already one table rather than two.
- **R-3 — the empty-squad floor is genuinely shared** (KD-2). If it is not resolved at the seam, each
  filter will grow its own guard and they will disagree. ERR-030-016 files it for that reason, and
  T-NT-I-005 documents the obligation in #36's suite without inventing a policy.
- **R-4 — "just add the field" will be proposed again.** It is the obvious move, and it is expensive for
  reasons that live in #27's **test** discipline rather than its API — a `FIELDS_PER_PLAYER` bump, a
  golden-vector rebaseline, and a silent rewrite of every existing save's rosters. T-NT-DET-001 asserts
  the golden vector **inside #36's own suite** precisely so the cost is visible to whoever tries.
- **R-5 — Stage-5 scope creep.** The deep tier is genuinely blocked (§7.2); the risk is that its absence
  makes the minimal tier look pointless and it gets padded with a fake international match against
  synthesised opponents. That is canon invented by a consumer.
- **R-6 — the pin table is the one #36 collection that grows with career length.** It is bounded by
  transfer volume rather than pool size, and FR-NT-013's drop-on-retire is what keeps it from outliving
  its pool. Remove that rule — or implement it partially, dropping the player from iteration but leaving
  the row — and it grows monotonically across a twenty-season career. T-NT-U-012 asserts both halves.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with T2's conditional neutrality stated with its precondition, and with the **re-key hook pulled forward to T2** — every transfer without it writes a silently wrong nationality; the Stage-5 gate with the argument for shipping the minimal tier anyway; deep-tier extensions incl. the one plausible #36-owned draw site; the not-planned list; risks R-1..R-6). Status IN REVIEW. |
#endregion
