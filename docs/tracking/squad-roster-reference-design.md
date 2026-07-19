# Squad Roster-Reference — Design Supplement (Plan 4, #27 T3)

> **Created:** July 18, 2026
> **Status:** DESIGN SUPPLEMENT (pre-code — no section files, no `SPEC_INDEX.md` row).
> Companion to `docs/tracking/squad-player-data-design.md` (candidate spec **#27**, §4 "T3 — snapshot
> header roster-reference field for save/restore fidelity, KD-7") and
> `docs/tracking/player-attribute-projection-design.md` (KD-P10 / §7.1 — "distinct-squad restore needs
> the T3 roster reference"). This doc is the detailed design for that single deferred field.
> **Purpose:** Turn "add a roster reference to the snapshot" from a one-line deferral into a reviewed
> decision — what identifier, where it lives, what it does and does NOT enable, and the one genuine
> design fork it forces (a configured match is now digest-distinguishable from an unconfigured one).

---

## 0. Scope and why this is its own doc

The #27 T1/T2 landing wired `ConfigureSquads` to source every agent's attributes from a real `Squad`,
but proved its byte-identical + restore guarantees **only for the default (no-squad) path** (KD-P7).
A distinct-squad match is not restore-deterministic: the per-slot attribute surfaces
(`_canonicalAttrs`, `_attrs`, `_dtAttrs`, `_perceptionAttrs`, the bench records) are **not
serialized** — a fresh process has no `Squad` to re-project from (projection-design §7.1 / KD-P10).
Both #27 design docs record the fix as the same one line: a **roster-reference id in the snapshot**
so a restore path can re-project from the referenced squad, keyed by the already-serialized
`_activeBenchSlot` for substitution bench-swaps.

This doc is that field. It is small, but it forces one real decision — see §3.

**In scope:** the roster-reference field (what value, where, serialization); the neutrality-semantics
decision it forces; the honest restore scope (reference-only — no restore path exists to consume it
yet); the test plan; the key decisions.

**Out of scope (unchanged from #27 §0 / projection §0):** the actual restore re-projection path
(the match engine has **no** snapshot-deserialize path today — building one is a separate match-engine
deliverable, and building a re-projection consumer against a non-existent restore path would be the
phantom-consumer class the project forbids, ERR-001/ERR-004/FR-LW-031); a squad store / on-disk squad
persistence (Stage-1+, master plan §4.6); lineup selection (Plan-3); aging/transfers.

---

## 1. What exists vs. what this adds (grounded in source)

**The gap (projection-design §7.1, verified against `MatchEngine.cs` this session):**
`SerializeWorldState` writes a versioned, digest-load-bearing payload but has **no `Read`/deserialize
counterpart** — the snapshot is a write-only determinism digest chain consumed by the two-run /
capstone / neutrality tests. Every serialized field is justified as "cross-tick, digest-load-bearing."
The per-slot attribute records are deliberately **excluded** (the boot-deterministic `_attrs`/`_perfs`
exclusion proof) — correct on the default path (they re-seed from `CreateDefault()` at boot) but,
since T1, an incomplete story for a distinct squad: a fresh process cannot reconstruct
`_canonicalAttrs` because it does not know **which squad was loaded**.

**Boot-constant identity is already serialized.** `_teamIds[i]` and `_isGoalkeeper[i]` are set at boot
and never change mid-match, yet are serialized in the per-agent ancillary block ("all cross-tick").
The roster reference joins **exactly this class** — a boot-constant identity value that the digest
must capture so the save records the match's configuration. This is the precedent that makes the field
non-phantom: it feeds a digest that is genuinely consumed, the same standard `_teamIds` meets.

**This adds:** a per-team `_rosterClubId[TEAM_COUNT]` (the loaded `Squad.ClubId`, default sentinel),
set by `ConfigureSquads`, serialized at a new `SNAPSHOT_SCHEMA_VERSION` (15 → 16). Nothing else.

---

## 2. What identifier (KD-T3-1)

`TacticalDirector.PlayerDatabase.Squad` carries a caller-assigned `ClubId` (`int`, design KD-3 —
distinct from match `teamId`; `PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex`, so a real club id is
`≥ 0`). The **`ClubId` is the roster reference**: it is the stable handle a future restore path (or a
future squad store) resolves back to the roster's players, and combined with the serialized
`_activeBenchSlot` it is sufficient in principle to re-project `_canonicalAttrs` including a mid-match
substitution bench-swap (projection §7.1). Storing per-player attribute *values* in the snapshot is
explicitly rejected by #27 KD-7 ("a roster-reference id … not per-player attribute values") — the
values are large, re-derivable from the squad, and would bloat every tick's digest preimage.

- **Per-team**, indexed by match `teamId` (home 0 / away 1) — home and away are different clubs.
- **Sentinel** `NO_ROSTER_CLUB_ID = -1` (`[FIXED]`, `MatchEngineConstants`) = "no `Squad` configured
  for this team" (the default/neutral path). Matches the project's `-1` sentinel convention
  (`NO_POSSESSION`, `_activeBenchSlot`, `_lastHolderAgentId`). A real roster uses a non-negative
  `ClubId` (the `PlayerId` formula assumes it), so the sentinel does not collide in practice; a caller
  that deliberately passes `ClubId = -1` faithfully records "-1" and is indistinguishable from
  unconfigured — a documented, acceptable edge, not a hazard (the reference records what it is given).

---

## 3. The one real decision: configured ≠ unconfigured (KD-T3-2)

Serializing the roster reference into the digest-bearing payload means a match booted through
`ConfigureSquads` — **even with an all-`CreateDefault` squad** — is now digest-distinguishable from a
match that never called it, because the configured run carries `_rosterClubId = {7, 8}` while the
unconfigured run carries `{-1, -1}`.

This **supersedes** the T1 `AllDefaultSquads_AreBehaviourNeutral_DigestUnchanged` lock, whose premise
was "configuring with all-default squads is fully byte-identical to not configuring." That premise was
a **T1** property (KD-P7 — T1 added *no* serialized field, so the whole snapshot was byte-identical).
T3 exists precisely to add the roster field, so a digest change on the configured path is the point,
not a regression.

**Why configured must differ from unconfigured (rather than preserving KD-P7 strictly):** the roster
reference is **identity, not attributes**. Club 7 with all-neutral attributes today is *not* the same
as "neutral defaults": club 7 is a persistent roster whose players will be edited/aged/transferred, so
a save must record "this was club 7" for restore to reload club 7's (future) data instead of frozen
neutral values. If a configured-neutral match were digest-identical to unconfigured, a restore could
not tell them apart — the exact fidelity gap T3 closes. So the divergence is **by design**, the same
class as the T1 "distinct-squad diverges by design" decision.

**Rejected alternative — a non-digest save "header" (preserve KD-P7 exactly).** The design docs say
"snapshot *header*", suggesting metadata outside the per-tick world-state digest. But the match engine
has **no header-metadata surface distinct from the digest payload** — `SerializeWorldState` writes the
payload, `SnapshotCodec.Encode` hashes it into `CurrentSnapshotDigest`, and there is no save/restore
path that would write or read a separate header. Adding a header field no save writes and no restore
reads is a field with **zero consumer** — the phantom class the project forbids — and it would not make
configured matches digest-distinguishable, i.e. it would not do the job. The payload is the project's
established "record now, consume later" surface (every `_teamIds`/`_managerStates`/`_goals` field lives
there), so the roster reference belongs there. "Header" in the design docs is read as *conceptual*
(roster identity, not per-player values) — satisfied by a single per-team id, wherever in the payload.

**Behavioural neutrality still holds and is still locked.** An all-`CreateDefault` squad projects to
the pre-T1 neutral seeds (KD-P7 attribute half), so agents still *move* identically to unconfigured —
the **only** digest difference is the roster-reference field. §5 re-expresses the neutrality lock as:
(a) same-config runs are byte-identical (determinism), and (b) a configured-default run diverges from
unconfigured **from the first tick** — before any behavioural divergence could exist, since neutral
attributes cannot move agents differently — proving the divergence is the identity field alone.

---

## 4. Placement, serialization, restore scope

- **Field:** `private readonly int[] _rosterClubId;` `[TEAM_COUNT]`, boot-initialized to
  `NO_ROSTER_CLUB_ID`. Boot-constant (a boot-time `ConfigureSquads` sets it; nothing mutates it
  mid-match), the same lifecycle as `_teamIds`.
- **Writer:** `ConfigureSquads` sets `_rosterClubId[0] = homeSquad.ClubId; _rosterClubId[1] =
  awaySquad.ClubId` **after** both squads validate-and-apply (so a refused call leaves the reference at
  the sentinel — validate-before-write, consistent with the AR-1 M-1 both-squads-before-any-write rule).
- **Serialization:** append a v16 block at the end of `SerializeWorldState` (after the v15 match-flow
  block), `WriteI32` × `TEAM_COUNT`, in `teamId` order. Bump `SNAPSHOT_SCHEMA_VERSION` 15 → 16 with a
  version-history note; update the schema-pin test.
- **Exclusion-proof update:** the `_attrs`/`_canonicalAttrs` exclusion comment gains a line — the
  attribute *values* stay excluded (re-derivable), and the v16 roster reference is the **identity half**
  that a future restore path re-projects from (keyed by `_activeBenchSlot` for substitutions). This
  makes the exclusion proof honest post-T3: the reference is captured; only the re-projection code (and
  the restore path that would call it) is future work.

**Restore scope (KD-T3-3 — honest about what lands).** T3 lands the **reference**. It does **not** land
a restore that re-projects `_canonicalAttrs` — because the match engine has **no snapshot-deserialize
path at all**. Building the re-projection now would be a consumer with no caller (phantom). So T3's
deliverable is exactly the #27 KD-7 wording — "a roster-reference id in the snapshot" — and the
projection-design §7.1 re-projection ("restore-time re-projection keyed by `_activeBenchSlot`") remains
future work, now **unblocked on the data side**: whenever a restore path is built, the reference it
needs is in the payload. Nothing in T3 silently diverges — a distinct-squad match still has no restore
path to be wrong about; T3 only makes the save *record which squad it was*.

---

## 5. Test plan (T3)

- **Schema pin:** `SNAPSHOT_SCHEMA_VERSION == 16` (deliberate bump lock).
- **Roster reference feeds the digest:** a match configured with `ClubId {7,8}` diverges from an
  unconfigured baseline at tick 1 (schema-pin probe, parallel to the v14/v15 probes).
- **Configured-default: behaviour neutral, identity captured (supersedes the T1 KD-P7 lock).**
  - A configured all-`CreateDefault` run diverges from unconfigured — **and the divergence is present
    at tick 1**, before any behavioural difference could arise (neutral attributes ⇒ identical
    movement), so the difference is the roster reference alone.
  - Two configured runs with the **same** ClubIds are byte-identical over many ticks (same-config
    determinism / neutrality survives).
  - Configured `{7,8}` vs configured `{100,101}` diverge at tick 1 (the reference records the actual
    club id, not merely "configured vs not").
- **`TestOnly_RosterClubId(teamId)`:** returns the sentinel before `ConfigureSquads` and the squad's
  `ClubId` after (per team), incl. after a refused (invalid) call it stays the sentinel.
- **Distinct-squad determinism (unchanged):** the T1 distinct-squad two-run determinism lock still
  holds under v16.

No new closed-loop `ScenarioRunner` scenario — T3 rides the existing schema-pin + squad suites (their
digests are the oracle). No restore test — there is no restore path to test (see §4).

---

## 6. Key decisions

- **KD-T3-1 (identifier = per-team `Squad.ClubId`, sentinel `-1`).** The stable roster handle, not
  per-player values (#27 KD-7). `NO_ROSTER_CLUB_ID = -1` = unconfigured; a real roster is `≥ 0`.
- **KD-T3-2 (configured ≠ unconfigured, by design).** Serializing the reference into the digest makes a
  configured match — even all-neutral — digest-distinguishable from unconfigured. This supersedes the
  T1 KD-P7 all-default byte-identity lock (a T1-only property); behavioural neutrality still holds and
  is re-locked as "diverges at tick 1 = identity field alone." A non-digest header alternative is
  rejected (no save/restore surface exists to consume it — a phantom that also would not do the job).
- **KD-T3-3 (reference only; re-projection is future).** T3 lands the roster id in the payload; the
  restore-time re-projection is deferred until a snapshot-deserialize path exists (none does today —
  building the consumer now is a phantom). T3 unblocks that work on the data side without pretending to
  do it.
- **KD-T3-4 (payload, not a new header surface).** The reference lives in the versioned world-state
  payload alongside the other boot-constant identity (`_teamIds`/`_isGoalkeeper`), because that is the
  only serialized/consumed surface the match engine has. "Header" in the source docs is conceptual
  (identity, not values).

---

## 7. Self-adversarial review

**AR-1 (v0.1) — folded in at authoring:** (a) the phantom-consumer risk of building a restore
re-projection against a non-existent deserialize path — resolved by scoping T3 to the reference only
(§4/KD-T3-3), matching #27 KD-7's own wording. (b) The "header vs payload" tension the design docs'
"snapshot header" phrasing raises — resolved in §3/KD-T3-4 by grounding against source (no header
surface exists; the payload is the boot-constant-identity precedent). (c) The neutrality-semantics
change to an existing passing T1 test — surfaced explicitly as KD-T3-2 rather than silently breaking
it, with the re-locked behavioural-neutrality tests in §5.

**AR-2 (v0.1 → v0.2) — fresh-eyes sweep against source. 0 H + 0 M + 1 L, fixed:**
- **L-1 (sentinel collision edge):** `Squad.ClubId` is an unvalidated `int`, so a caller could pass
  `ClubId = -1` and be indistinguishable from unconfigured. Verified against `Squad.cs` (no `ClubId`
  sign gate) — documented in §2 as an acceptable edge (the reference records what it is given; a real
  roster is `≥ 0` by the `PlayerId` formula) rather than adding a separate `_squadsConfigured` bool
  (extra state for a non-hazard). No code change.
- Re-verified: no `Read`/`Deserialize` exists in `MatchEngine.cs` (the restore path really is absent,
  so KD-T3-3 is not over-cautious); `_teamIds`/`_isGoalkeeper` really are serialized boot-constants (the
  non-phantom precedent holds); `ConfigureSquads` already validates both squads before any write (so
  the "sentinel on refusal" property is free); `_activeBenchSlot` really is serialized at v15 (the
  substitution restore handle exists). No H/M.

**Cycle status: CONVERGED at AR-2** (an L-only round ends the cycle per the project convention —
match-viewer AR-4 / squad-player-data AR-2 precedent).

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-18 | Initial draft — #27 T3 snapshot roster-reference field. AR-1 self-review folded in. |
| 0.2 | 2026-07-18 | AR-2 (0H+0M+1L): sentinel-collision edge documented (§2); re-verified no restore path exists, boot-constant-identity precedent, validate-before-write, `_activeBenchSlot` at v15. CONVERGED. |
| 0.3 | 2026-07-18 | Post-landing **code** AR (fresh-eyes over the shipped diff, not a design-stage round — the projection-design v0.4 precedent): 0H+0M+1L. **L:** replacing the T1 `AllDefaultSquads_..._DigestUnchanged` byte-identity lock dropped the DIRECT match-level proof that a config-default match is *behaviourally* identical to unconfigured — the new tests prove the roster field feeds the digest and that configs diverge, but not that the divergence is non-behavioural (the KD-T3-2 "sole difference" claim). Fixed: added `ConfiguredDefaultSquad_IsBehaviourNeutral_ObservableStateMatchesUnconfigured` (ball + every agent position match tick-for-tick via the public observation surface — the observable level a digest comparison can no longer isolate from the roster field). Re-verified clean: serialization appended last (no offset move); no snapshot decoder anywhere reads the payload by offset (only the opaque digest / whole-payload SHA), so the schema bump breaks nothing beyond the digest; the `CROSS-TICK COVERAGE COMPLETE` comment's excluded-set claim survives (the roster ref is serialized, not excluded). Gate: PASSED, 0 failures (237 match-engine tests). |
