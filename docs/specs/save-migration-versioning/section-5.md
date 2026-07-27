# Save Migration & Versioning #50 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-MG-U-*` unit, `T-MG-I-*` integration, `T-MG-DET-*` determinism,
`T-MG-GEN-*` generation, `T-MG-ID-*` identity, `T-MG-LOC-*` localization compliance, `T-MG-FAIL-*`
fail-loud, `T-MG-BOUND-*` structural.

Every value asserted below is hand-derivable from §3.7 or is a relational property.

## 5.1 The classification matrix, exhaustively (KD-1)

Appendix D is the authoritative matrix; this suite walks **every** cell, because a classification that is
right in four cases and wrong in the fifth is a spec that refuses valid saves or accepts damaged ones.

| ID | Test |
|---|---|
| T-MG-U-001 | Current at every level ⇒ `Current`. |
| T-MG-U-002 | Strictly older with a registered chain ⇒ `Migratable`. |
| T-MG-U-003 | Any version above the build's ⇒ `TooNew`. |
| T-MG-U-004 | Below the floor, or no chain reaches current ⇒ `Unsupported`. |
| T-MG-U-005 | An unreadable / unknown version value ⇒ `Corrupt` — **never** `Migratable` (FR-MG-006). |
| T-MG-U-006 | **`TooNew` and `Corrupt` are distinguishable** at the API, not merely both refusals (FR-MG-005). The messages differ, and telling a player the wrong one is how a recoverable situation becomes a deleted file. |
| T-MG-U-007 | **The most-severe fold** (FR-MG-008), every ordering: `Corrupt` beats `TooNew` beats `Unsupported` beats `Migratable` beats `Current`. §3.7(e) is the case that matters — a damaged file must not be reported as merely futuristic. |
| T-MG-U-008 | Classification **reads only version fields**: asserted by classifying a file whose blob **bodies are deliberately garbage** while the version fields are valid, and getting the correct class without an exception (FR-MG-001). |
| T-MG-U-009 | Classification neither locks nor writes: the file is byte-identical and unlocked afterwards (FR-MG-007). |

## 5.2 The two different determinism properties (KD-7)

**This split is the point.** A single "transforms are deterministic" suite would assert byte-purity for
generation migrations, which do not have it — and would pass, because a test that pins the wrong property
usually does.

| ID | Test |
|---|---|
| T-MG-DET-001 | **Format** transforms are **byte-pure**: the same input blob migrates to a byte-identical output, across runs and across processes, with no clock, filesystem or draw dependency (FR-MG-034). Golden-vectored byte→byte. |
| T-MG-DET-002 | **Generation** migrations are **deterministic by seed, not byte-pure** (FR-MG-035): run against a real `DeterministicRngService`, the frozen generator reproduces its pinned output for a pinned seed. The golden vector pins **the generator's output**, not a byte→byte mapping. |
| T-MG-DET-003 | A full chain over multiple blobs is order-independent **across blobs** and order-**dependent within** one (§3.2): permuting sub-blob migration order yields a byte-identical file; permuting steps within a blob fails. |
| T-MG-DET-004 | A step that claims a jump larger than `+1` **fails loud** at the runner's post-condition (§3.7(i)) — a lying step is a bug, not a variant. |

## 5.3 The generation lock — the one this spec exists to add (KD-2)

| ID | Test |
|---|---|
| T-MG-GEN-001 | **The headline lock.** Perturb a `[GT]` generation input (a strength-ramp constant, a catalogue row, a draw-order change), then load a save stamped with the **old** `WORLD_GENERATION_VERSION`: it MUST be **refused or materialised**, and MUST NOT load with silently different squads (F6). **Constructed as the reproduction of a failure that is currently possible and invisible** — on today's code the save loads, nothing errors, and the squads have changed. |
| T-MG-GEN-002 | An **equal** stamp proceeds with regeneration unchanged and materialises nothing (FR-MG-012) — the zero-cost common case. |
| T-MG-GEN-003 | An **older** stamp with a retained generator materialises **once**: the rosters are written into the save, the stamp is updated, and a second load runs no generator at all. |
| T-MG-GEN-004 | An older stamp whose generator is **past the floor** ⇒ `Refuse` (F5/FR-MG-015) — asserted as the **expected** outcome, not as an error path, so nobody later "fixes" it by falling back to the current generator. |
| T-MG-GEN-005 | A materialised career is **byte-identical in its rosters** to the career the old build had — asserted against the frozen generator's golden vector, which is what makes the repair meaningful rather than merely quiet. |
| T-MG-GEN-006 | A retained generator's golden vector **fires** when the retained code is edited: proven non-vacuous by perturbing it, the `LeagueBootstrapGoldenVectorTests` discipline applied to code that normal play never exercises (FR-MG-016). |

## 5.4 Non-destructive behaviour (KD-4)

| ID | Test |
|---|---|
| T-MG-FAIL-001 | **A refused save is byte-identical after the attempt** (FR-MG-023) — for every refusal class. |
| T-MG-FAIL-002 | A step that **throws** aborts pre-commit; the original is byte-identical and no partial file exists at the save location (F7/FR-MG-025). |
| T-MG-FAIL-003 | A step whose output the **current codec rejects** is caught by `VerifyLoadable` **before** the rename (F4/§3.4). Asserted with a deliberately-broken step, since this is the designed shape of a migration bug. |
| T-MG-FAIL-004 | A successful migration writes a **new file** and leaves the original present and unmodified (FR-MG-024). |
| T-MG-FAIL-005 | An I/O failure between `Write` and `Rename` leaves the original intact — the case the rejected migrate-in-place design loses a career on (R-3). |

## 5.5 Post-migration validity and the honest promise (KD-6)

| ID | Test |
|---|---|
| T-MG-I-001 | A migrated blob passes the **current codec's unmodified gates** (FR-MG-003) — including the trailing-byte and length-bound guards, which a hand-written transform is exactly the thing likely to violate. |
| T-MG-I-002 | A migrated career **advances deterministically**: two runs from the migrated save produce byte-identical digest chains (FR-MG-032). |
| T-MG-I-003 | **The test that must NOT be written, recorded so nobody writes it:** counterfactual identity with a career played natively through the same fixtures. FR-MG-033 declines to promise it, a synthesizing bump makes it unachievable, and asserting it would either fail permanently or force the transform to fake data it does not have. |
| T-MG-I-004 | **Sub-blob isolation** (FR-MG-019/021): a season-state bump migrates that blob and leaves **every other blob byte-untouched**, asserted per blob rather than over the file as a whole. |
| T-MG-I-005 | An empty-registry load over a **directory** of current saves classifies each cheaply and opens each unchanged — the load-screen path (§6.2). |

## 5.6 Structural and boundary locks

| ID | Test |
|---|---|
| T-MG-BOUND-001 | **#50 references only `TacticalDirector.DeterministicSim`** — asserted by the mechanical `.asmdef` scan. In particular it references **no** spec assembly whose blobs it migrates, and **not** `player-database` or `season-save` (§4.4). |
| T-MG-BOUND-002 | **No codec gate was weakened.** Every codec's version, length-bound and trailing-byte guard behaves identically with #50 present — asserted behaviourally, because the reference graph cannot prove it and a release-pressure "fix" would break it first (§4.6). |
| T-MG-BOUND-003 | **A step cannot reach a neighbouring blob** (F8): the runner hands each step only its own bytes, so the violation is **not representable**. Asserted over the surface, not by convention. |
| T-MG-BOUND-004 | **The FR-MG-022 build-time completeness check fires**: a deliberately-unregistered version between the floor and current **fails the build**, not the player's load. Non-vacuity proven by removing a real registration. |
| T-MG-BOUND-005 | `BlobKind` **ordinal stability** (FR-MG-017): it is the registry key, so a reorder silently re-points every registered step at the wrong blob. `SaveClass`, by contrast, is asserted to be **absent** from every serialized surface. |
| T-MG-BOUND-006 | **#50 registers no RNG stream and allocates no ordinal** (FR-MG-036): a full classify-migrate-load cycle leaves every registered stream's cursor byte-identical. The one draw in the system belongs to a **frozen generator**, on its own seeded service. |
| T-MG-BOUND-007 | Migration completes **before any subsystem is constructed** (FR-MG-038): no engine, world store or season loop exists during the run. |

## 5.7 Localization compliance (#49)

| ID | Test |
|---|---|
| T-MG-LOC-001 | **#50 emits no display string** (FR-MG-026): a source-level assertion over `src/save-migration/` finds no string field, no string return and no string formatting. |
| T-MG-LOC-002 | **Each refusal class carries its own intent** (FR-MG-027) — asserted per class, since collapsing them is the failure KD-1 names. |
| T-MG-LOC-003 | Version numbers reach #49 as **slot values** (FR-MG-028), never pre-formatted. |
| T-MG-LOC-004 | FR-LC-008a coverage over the full refusal-intent roster. |

## 5.8 Identity (KD-7)

| ID | Test |
|---|---|
| T-MG-ID-001 | **The minimal tier's identity.** With an **empty** registry, a current save classifies `Current`, runs zero transforms and loads **byte-identically to pre-#50** (FR-MG-037). |
| T-MG-ID-002 | With an empty registry, **every non-current save is refused exactly as today** — the same class of failure, at the same point. This is the half that makes the seam landable before there is anything to migrate: #50's minimal behaviour *is* the current behaviour. |
| T-MG-ID-003 | The `SaveOriginStamp` is the **only** addition to the save (§4.5): with #50 present, a save differs from a pre-#50 save in exactly that field and the frame version, and in nothing else. |

## 5.9 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `old-generation-save-is-never-silently-regenerated`, owning specs
`{16, 19, 27, 30, 50}`, registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

write a save at generation version *v*; **perturb a `[GT]` generation input** so the current generator
produces a different league from the same seed; then load the old save and assert it is **refused or
materialised** — and, in the materialised case, that its squads match the **old** generator's golden
output rather than the new one's, and that the career then advances deterministically.

This is the composition-level proof of the one claim that distinguishes #50 from a format-only migrator.
It is also the scenario that would **fail on today's code** in the most instructive way: the save loads,
nothing errors, and the squads are different — which is the failure §1.4(c) shows is currently possible
and entirely invisible.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.2 splits the two determinism properties, because a single suite asserting byte-purity would pass for generation migrations while pinning a property they do not have. §5.3 leads with T-MG-GEN-001, constructed as the reproduction of a failure that is currently possible and invisible. T-MG-I-003 records a test that must **not** be written (counterfactual identity), since asserting what KD-6 declines to promise would force a transform to fake data. T-MG-BOUND-002 asserts that no codec gate was weakened — behaviourally, because a reference graph cannot prove it. Status IN REVIEW. |
#endregion
