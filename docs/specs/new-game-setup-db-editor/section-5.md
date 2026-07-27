# New-Game Setup & Database Editor #47 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-ED-U-*` unit, `T-ED-I-*` integration, `T-ED-DET-*` determinism,
`T-ED-ID-*` identity, `T-ED-FAIL-*` fail-loud, `T-ED-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.6** or is a relational property.

## 5.1 The round-trip lock (KD-2)

| ID | Test |
|---|---|
| T-ED-U-001 | **`Parse(Write(squad)) == squad`, field-for-field**, over a corpus that includes **every boundary value the loader gates** — `age` at both bounds, every attribute at `[1,20]` / `[1,5]` extremes, every `PlayerPosition`, an empty squad, and a full 25-player one. This single property covers the encode/decode asymmetry class #30 T1 was bitten by (`SeasonState`: **constructible but not decodable**). |
| T-ED-U-002 | §3.6(c): **the club-scoping lock.** A writer emitting section-local indices instead of club-scoped ones produces different `PlayerId`s on parse and **fails** — the exact defect the round-trip caught at #27 T0. Asserted with a `ClubId` whose scoping makes the two formulas differ, so a coincidence cannot pass it. |
| T-ED-U-003 | §3.6(b): the boundary corpus specifically covers the loader's **`age`** bound — the defect that escaped to a later adversarial review rather than to a test, and therefore the case that argues for this lock existing at all. |
| T-ED-U-004 | **The writer emits nothing outside the documented grammar** (FR-ED-020): its output parses under the grammar as specified, not merely under the current parser's tolerance. The property that makes the Stage-0+1 parser swap free. |
| T-ED-U-005 | **The editor binds to types, not syntax** (FR-ED-021) — asserted structurally: no #47 type exposes or consumes the grammar's string form except `SquadFileWriter`. |

## 5.2 The generated-identity lock (KD-7)

| ID | Test |
|---|---|
| T-ED-ID-001 | **A generated game started through #47's setup flow is byte-identical to one started in code**: the same `League`, the same season, the same save frame. Same call, same parameters — #47 adds a front-end and nothing else. |
| T-ED-ID-002 | §3.6(g): **a generated game writes NO authored sub-blob — not an empty one** (FR-ED-012). Asserted on the frame, because "empty block present" and "block absent" are different bytes, and only the second preserves byte-identity. |
| T-ED-ID-003 | **`LeagueBootstrapGoldenVectorTests`' digest is unchanged by #47**, asserted **inside #47's own suite**. The #36 precedent: make the cost of touching generation visible in the consumer's tests too, so a contributor who "just tweaks the generator to accept authored data" fails here as well as in #27's suite. |
| T-ED-ID-004 | No RNG stream is registered and no draw is made (FR-ED-029/030): a full setup-and-start leaves **every** registered stream's cursor byte-identical. |

## 5.3 Validation authority (KD-2)

| ID | Test |
|---|---|
| T-ED-I-001 | §3.6(d): data `Parse` rejects **fails loud through the loader** (F1), and #47 surfaces the parser's own message rather than substituting one. |
| T-ED-I-002 | **A permissive editor-side check cannot admit bad data** (F2): with a deliberately wrong check that accepts an out-of-range value, the commit **still fails** at `Parse`. This is what makes FR-ED-019's "affordance, not authority" mechanically true rather than aspirational. |
| T-ED-I-003 | **A restrictive editor-side check that disagrees with the loader is detectable**: for every value in the boundary corpus, the check's verdict and `Parse`'s agree. A disagreement is a **bug in the check** (F2), and this is the test that finds it. |
| T-ED-I-004 | **#47 exposes no second validation path** (FR-ED-017) — asserted over the public surface, because "add a friendly validator" is the realistic way the second authority arrives. |

## 5.4 Integration — authored construction and persistence

| ID | Test |
|---|---|
| T-ED-I-005 | §3.6(h): **the self-containment lock.** An authored save loads correctly with the **source file deleted**, the editor absent, and on a different machine. **This is the property the rejected hash-plus-external-file design would have failed**, and it is why the artifact lives in the save. |
| T-ED-I-006 | §3.6(i): an authored save whose sub-blob is **missing** **throws** (F7) — and specifically does **not** fall back to generation, which would load a *wrong world* that looks merely odd rather than broken. The silent-failure case §1.4(a) identifies. |
| T-ED-I-007 | §3.6(j): an authored club carrying a non-zero `StrengthDelta` **throws** at the factory (FR-ED-009) — no ramp is applied to authored data, and a stray delta is not quietly accepted. |
| T-ED-I-008 | **No ramp is applied**, asserted positively: an authored league's shipped attributes equal the authored ones **exactly**, with no per-club scaling. The failure this catches is silent — every player slightly re-tuned away from what the author typed. |
| T-ED-I-009 | §3.6(k): non-ascending or duplicated `ClubId` / `PlayerId` **throws** at write and on decode (F5), so two equivalent databases cannot serialize differently. |
| T-ED-I-010 | §3.6(l): **pin precedence** — a player authored as one nationality and then transferred carries the **re-key** pin afterwards (FR-ED-026). The rule #36 left open, asserted rather than described. |
| T-ED-I-011 | State → `Encode` → `Decode` is **field-identical** for the artifact: clubs, squads, and authored pins. |
| T-ED-I-012 | Round-trip through a full `SeasonSaveCodec` frame: #47's sub-blob is **opaque** to the outer codec, and the world / season / match / sibling blobs are **byte-unchanged**. |
| T-ED-I-013 | **The flag and the blob agree** (F8): a generated save carrying an authored sub-blob, or an authored save flagged generated, **throws** on decode. |
| T-ED-I-014 | The two format versions move **independently**. |
| T-ED-I-015 | An authored `League` is **`ISquadProvider`-identical in shape** to a generated one (FR-ED-008): the same season path runs unchanged over either, with no downstream branch on origin. |

## 5.5 Structural (the boundaries #47 must not cross)

| ID | Test |
|---|---|
| T-ED-BOUND-001 | **#47's data layer references `player-database` and NOTHING else** — asserted from the reference set. **The one to expect is `season-save`**: constructing a `League` there is the natural implementation, and it transitively pulls `MatchEngine` and `LivingWorld`, so an editor would depend on the whole simulation to author a text file (FR-ED-001). |
| T-ED-BOUND-002 | #47 references no sim loop, no `MatchEngine`, and no `TacticalDirector.Localization`. |
| T-ED-BOUND-003 | **#47's data layer has no UI dependency** (FR-ED-005) — demonstrated by a **headless** authoring run that exercises load → edit → write → parse with no #38 present. Anything that only works through the screen is by definition in the wrong layer (FR-ED-028). |
| T-ED-BOUND-004 | **#47 declares no type named `Squad`, `PlayerRecord`, `PlayerAttributes`, `Club`, or `League`** — the parallel-surface lock (FR-ED-004). Note `AuthoredClub` is deliberately not `Club`; a duplicate model would diverge silently the moment #27 or `season-save` adds a field. |
| T-ED-BOUND-005 | **#47 constructs no `League`** (FR-ED-003) — asserted over the compiled surface. |
| T-ED-BOUND-006 | **No foreign writes:** a `Squad`, a `PlayerRecord[]`, and a `League` handed alongside every #47 entry point are **field-unchanged** after write, artifact encode/decode, and view-model projection. Asserted behaviourally — #27's types are reachable, so the reference graph cannot prove this (§4.6 standing item). |
| T-ED-BOUND-007 | **#47 declares no nationality store** (FR-ED-025): authored pins are entries in #36's table, and #47 exposes no parallel map. |

## 5.6 Setup-flow delegation (KD-3)

| ID | Test |
|---|---|
| T-ED-U-006 | §3.6(e): an out-of-range `clubCount` **throws from `LeagueBootstrap.Generate`**, not from #47 — asserted on the exception's origin, because a #47-side pre-check would pass a naive "it throws" test while creating the second authority KD-3 forbids. |
| T-ED-U-007 | An invalid `managedClubId` throws from **`League.CreateSeason`**, likewise. |
| T-ED-U-008 | §3.6(f): **every `ulong` is a valid `worldSeed`**, including `0` and `ulong.MaxValue` — #47 gates it not at all. |
| T-ED-U-009 | `NewGameConfig` carries **no** `AuthoredDatabase`; the artifact travels beside it (KD-5), so a generated setup carries nothing. |

## 5.7 Localization compliance (KD-6)

| ID | Test |
|---|---|
| T-ED-U-010 | §3.6(m): **an authored club name is stored as authored** — no locale baked, no `LocalizationKey` allocated (FR-ED-016/031). |
| T-ED-DET-001 | **Locale-independence of state** (FR-LC-006): the same authored database saved under two display locales produces **byte-identical** serialized state. |
| T-ED-U-011 | An authored name reaches the player **through** #49's seam as a `NamedSlotSet` slot value — routed, never translated. FR-LC-001 is satisfied **by routing**, which is why no exemption is claimed. |

## 5.8 Determinism

| ID | Test |
|---|---|
| T-ED-DET-002 | **An authored game is deterministic from its saved data**: the same authored database yields the same `League`, the same season, and **no dependence on generation order** — the property the rejected override design would have destroyed. |
| T-ED-DET-003 | Two runs over the same authored artifact produce **field-identical** state; `save@N → restore → advance` is field-identical to the uninterrupted run. |
| T-ED-DET-004 | **Canonical ordering is a function of state, not insertion**: an artifact built by adding clubs and players in permuted order serializes **byte-identically** (FR-ED-014). |

## 5.9 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-ED-FAIL-001 | Malformed authored data ⇒ throws **through `Parse`** (F1). |
| T-ED-FAIL-002 | A `Write` output that does not round-trip ⇒ a test failure, not a runtime path (F3 / T-ED-U-001). |
| T-ED-FAIL-003 | Out-of-range setup parameters ⇒ throw **from the consumer** (F4 / T-ED-U-006/007). |
| T-ED-FAIL-004 | Non-ascending or duplicated ids ⇒ throw at write and decode (F5). |
| T-ED-FAIL-005 | Decode: wrong `AUTHORED_DB_SAVE_FORMAT_VERSION` ⇒ throws, version read **first** (F6). |
| T-ED-FAIL-006 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound against `total − offset`, never wraps (F6). |
| T-ED-FAIL-007 | Decode: trailing bytes ⇒ throws (F6). |
| T-ED-FAIL-008 | A missing sub-blob on an authored save ⇒ throws (F7 / T-ED-I-006). |
| T-ED-FAIL-009 | A flag/blob mismatch in either direction ⇒ throws (F8 / T-ED-I-013). |

## 5.10 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `authored-database-plays-a-season`, owning specs
`{16, 19, 27, 30, 36, 47}`, registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

author a small league through the data layer; write it, parse it back, and assert **field-identity**;
start a season from it through the root's authored branch; assert the shipped attributes are **exactly the
authored ones** with no ramp applied; play a round; **save, delete the source file, and restore**; assert
the world matches an uninterrupted run; transfer an authored player and assert the **re-key pin wins**;
then start a **generated** game with the same flow and assert its save frame is **byte-identical to
pre-#47** with **no** authored sub-blob.

This is the composition-level proof that KD-1's two halves, KD-2's authority, KD-3's delegation and KD-7's
conditionality hold **together** — and the generated half is deliberately in the same scenario, because
#47's central claim is a statement about **both** branches at once.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.1 and §5.2 lead with the two locks the spec turns on — the writer round-trip (which covers the encode/decode asymmetry class and caught #27 T0's club-scoping defect) and the generated-identity lock (asserted on the *frame*, since "empty block" and "no block" are different bytes). T-ED-I-002/003 make FR-ED-019's "affordance, not authority" mechanically true in both directions; T-ED-U-006/007 assert the *origin* of a refusal, because a #47-side pre-check passes a naive "it throws" test while creating the second authority KD-3 forbids. Status IN REVIEW. |
#endregion
