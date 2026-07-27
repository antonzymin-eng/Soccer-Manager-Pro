# Manager Career, Reputation & Job Market #54 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-MC-U-*` unit, `T-MC-I-*` integration, `T-MC-DET-*` determinism,
`T-MC-ID-*` identity, `T-MC-FAIL-*` fail-loud, `T-MC-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.7** or is a relational property.

## 5.1 The lock this spec exists for

| ID | Test |
|---|---|
| T-MC-I-001 | **The unemployed-save lock — the whole floor.** A career survives a **mid-season** termination: the season advances to its boundary with **no managed club**, every fixture resolving through `RoundResolutionMode`'s model rather than the engine, and the save **round-trips byte-identically in that state**. This is the case the current codec **cannot even construct** (§1.4(b)), so it is simultaneously the acceptance test for #54 and the proof that ERR-030-021 landed. If this fails, nothing else in the spec matters. |

## 5.2 The lock that keeps reputation honest

| ID | Test |
|---|---|
| T-MC-BOUND-001 | **No reputation field exists** (FR-MC-013): asserted **structurally** over `ManagerCareer`, `Tenure`, and the serialized block — no field, no property, no cache, at any tier. A prose rule does not survive a contributor who notices the recomputation; this is what does. |
| T-MC-DET-001 | **Reputation cannot diverge** (KD-2): reputation after `save → restore → recompute` equals the uninterrupted value. Note the test is **satisfied by construction** — there is no stored field to compare — which is exactly the property `ERR-030-009` shows a stored scalar cannot offer, since *"both values are individually plausible"* and diverge *"with nothing to detect it"*. |
| T-MC-U-001 | **The projection depends only on the record** (FR-MC-014): varying current confidence, current club, and world day across their full ranges leaves `ReputationOf()` **bit-identical**. The lock that catches "make reputation respond to how things are going", which is the second-truth problem arrived at from the other direction. |

## 5.3 Unit — the termination rule (§3.1)

| ID | Test |
|---|---|
| T-MC-U-002 | §3.7(a): confidence 150, objective 800, past grace ⇒ **`Terminate`** — the floor overrides a good objective. |
| T-MC-U-003 | §3.7(b): confidence 150 **inside** the grace period ⇒ **`Continue`**. **The guard-ordering lock:** the grace check must short-circuit *before* the floor, or a manager appointed to a club at an inherited crisis confidence is terminated on his first evaluation — precisely the case KD-4 exists to prevent. |
| T-MC-U-004 | §3.7(c)/(d): the at-risk band — objective met ⇒ `Continue`, objective failed ⇒ `Terminate`. Both, so neither threshold can be dropped without a failure. |
| T-MC-U-005 | §3.7(e): a backed manager (confidence above the band) survives a failed objective. |
| T-MC-U-006 | **The rule is pure** (FR-MC-008): called a thousand times with identical inputs it yields identical verdicts and leaves #54's state **byte-identical**. |
| T-MC-U-007 | **Evaluation does not mutate #45's state** (FR-MC-009): a `BoardConfidence` handed alongside the call is **field-unchanged** — the direction `FR-BD-012` protects, asserted rather than assumed. |
| T-MC-U-008 | **Monotonicity**: the verdict is non-increasing in confidence (higher confidence never turns `Continue` into `Terminate`) and non-increasing in objective outcome. A shape lock that survives a `[GT]` retune. |

## 5.4 Unit — tenure lifecycle (§3.2 / §3.3)

| ID | Test |
|---|---|
| T-MC-U-009 | §3.7(g): `Terminate` closes the tenure, stamps the reason and day, and sets **`CurrentTenure = MC_UNEMPLOYED`** — the career **continues** (FR-MC-010). |
| T-MC-U-010 | §3.7(i): a later `Appoint` **appends**, and **tenure 0 is field-unchanged**. History is frozen (FR-MC-011) — the property the reputation projection's stability rests on. |
| T-MC-U-011 | §3.7(j): `Appoint` while a tenure is open **throws** (F1). Asserted because two open tenures **decode cleanly** and only break `CurrentTenure`'s meaning — nothing downstream would catch it. |
| T-MC-U-012 | §3.7(k): `Terminate` while unemployed **throws** (F2). |
| T-MC-U-013 | §3.7(l): **unemployment is `CurrentTenure == −1`, not "the last tenure happens to be closed"** (F6). A decoded career whose last tenure is closed but whose `CurrentTenure` points at it **throws**. Two representations of one state exist; only this one is checkable. |
| T-MC-U-014 | An incoherent tenure — `EndWorldDay < StartWorldDay`, or `Finishes` longer than `SeasonsServed` — **throws** at write and decode (F5); it would otherwise silently corrupt every reputation projection over it. |
| T-MC-U-015 | `EndReason.Open` on a tenure carrying an `EndWorldDay`, or an undefined ordinal, **throws** (F4). |

## 5.5 Unit — the reputation projection (§3.4)

| ID | Test |
|---|---|
| T-MC-U-016 | §3.7(h) exact: base 300, 2 seasons, 0 trophies, neutral finishes, `Sacked` ⇒ **280**. |
| T-MC-U-017 | §3.7(m): **open tenures count.** A manager three seasons into a successful spell has that reputation **now**; it must not jump on termination, which would read as a bug and be one. |
| T-MC-U-018 | **Monotonicity**: reputation is non-decreasing in seasons served and trophies, and non-increasing in the count of `Sacked` endings. |
| T-MC-U-019 | §3.7(o): **sign-symmetry** (§3.6) — `±N` terms move the projection by equal magnitudes in opposite directions. The lock that fails if `Math.Floor` or `Math.Round` is substituted for integer division. |
| T-MC-U-020 | The clamp is applied **once at the end**, not per term: a bad early spell can be recovered from rather than saturating the projection at zero. |
| T-MC-U-021 | The §3.4 overflow bound holds at `MC_MAX_TENURES` tenures with every term at `MC_REP_TERM_ABS_MAX`. |
| T-MC-U-022 | **`EndReason` ordinal stability** (FR-MC-015). §3.7(n) is why this is doubly load-bearing: the ordinal is **serialized** *and* **indexes the reputation weight table**, so a reorder re-reads every historical tenure **and** changes every historical reputation — neither with a version gate. |

## 5.6 Integration — the appointment join (§4.4)

| ID | Test |
|---|---|
| T-MC-I-002 | **Appointment does not start a career in crisis** (FR-MC-017): appointing to a new club yields a **factory** `BoardConfidence`, never `default`. Asserted at the appointment path, because `default` is field-in-range and reads as `Critical` / *"dismissal imminent"* — #45's own guard would throw, which is the *good* failure but still a crash on an ordinary career action. |
| T-MC-I-003 | **The companion lock:** `Appoint` **alone** performs **no** write into #45's store — the store is field-unchanged after a bare `Appoint`. This is what keeps #54 a leaf (FR-MC-004). |
| T-MC-I-004 | Appointment does **not** inherit the predecessor's standing — the new club's confidence is the honeymoon value regardless of what the previous manager left it at (FR-MC-017). |
| T-MC-I-005 | A newly appointed manager is **not terminated on his first evaluation** even at a low inherited confidence — the composed form of T-MC-U-003, exercising the grace period and the honeymoon value together. Each guards this alone; the test proves the pair does. |

## 5.7 Integration — save / restore

| ID | Test |
|---|---|
| T-MC-I-006 | State → `Encode` → `Decode` is **field-identical**: an empty career, one open tenure, a closed tenure with finishes and trophies, and the **unemployed** state (`CurrentTenure == −1`). |
| T-MC-I-007 | **The career survives a season-boundary roll** (FR-MC-029): #30's season state is replaced, and the career block is **unchanged** across it. The one structural difference from every neighbouring block, asserted directly. |
| T-MC-I-008 | Round-trip through a full `SeasonSaveCodec` frame: #54's sub-blob is **opaque** to the outer codec, and the world / season / match / sibling blobs are **byte-unchanged**. |
| T-MC-I-009 | The two #54-adjacent format versions move **independently**; and the **third** — #30's `SEASON_STATE_FORMAT_VERSION`, bumped by ERR-030-021 — is #30-owned and does not imply either. |
| T-MC-I-010 | A career spanning **multiple** appointments and terminations round-trips with its history **in order and unmodified**. |

## 5.8 Determinism and identity

| ID | Test |
|---|---|
| T-MC-DET-002 | Two runs over the same evaluation and command sequence produce **field-identical** state. |
| T-MC-DET-003 | `save@N → restore → advance to N+K` is **field-identical** to the uninterrupted run, **including across a termination**. |
| T-MC-DET-004 | **Draw-free** (FR-MC-024): a full career of evaluations, terminations and appointments leaves **every** registered RNG stream's cursor byte-identical. |
| T-MC-ID-001 | **Identity** (KD-8): a career with **one appointment and no vacancies** produces behaviour identical to today's single-club career — #54 records what already happens. |
| T-MC-ID-002 | **(T0/T1 only.)** The season save is byte-identical to the pre-#54 save. Scoped deliberately — at **T2** the frame gains #54's block **and** ERR-030-021 changes `ManagedClubId`'s representation, so the save is not byte-identical and the identity claim is about **behaviour**, never the frame. |

## 5.9 Structural (the boundaries #54 must not cross)

| ID | Test |
|---|---|
| T-MC-BOUND-002 | **#54's assembly references nothing**, at every tier — asserted from the reference set, so a future `using` of #45 / #30 / #40 / #53 / #27 / `SeasonSave` / `MatchEngine` fails the build's test gate (FR-MC-003). The #45 one is **the one to expect**: reading confidence directly is the natural implementation. |
| T-MC-BOUND-003 | **#54 declares no type named `ManagerProfile` or `ManagerMode`** (FR-MC-007) — #26 already ships both for in-match tactical adaptation. The **foreseen** third CS0104 instance, asserted mechanically rather than trusted to review. |
| T-MC-BOUND-004 | **#54 exposes no member that writes #45, #30, #40, #53 or #27 state** — asserted over the public surface, because the convenience is real and it would compile. |
| T-MC-BOUND-005 | **No foreign writes:** a `BoardConfidence`, a `SeasonState` and a club-value set handed alongside every #54 entry point are **field-unchanged** after evaluation, appointment, termination, projection and save/restore. |
| T-MC-BOUND-006 | **#54 models no rival manager** (FR-MC-020/021): no type, field, event or view represents a rival's tenure, and no vacancy carries a "previous manager". Asserted over the public surface, because inventing one is the tempting way to make S3 vacancies feel alive — and it would build the consumer #22's phase-5 is meant to produce. |
| T-MC-BOUND-007 | **No `RegisterStream` call exists at the minimal tier** (FR-MC-024), asserted over the compiled surface. |

## 5.10 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-MC-FAIL-001 | §3.7(f): an out-of-range `ConfidencePermille` or `ObjectiveOutcome` ⇒ **throws** (F3) — never a verdict from a corrupt routed value, and never a silent clamp. |
| T-MC-FAIL-002 | `Appoint` with an open tenure ⇒ throws (F1); `Terminate` with none ⇒ throws (F2). |
| T-MC-FAIL-003 | An undefined `EndReason`, or `Open` paired with an `EndWorldDay`, ⇒ throws (F4). |
| T-MC-FAIL-004 | An incoherent tenure ⇒ throws at write **and** decode (F5). |
| T-MC-FAIL-005 | A decoded `CurrentTenure` out of range, or pointing at a closed tenure, ⇒ throws (F6). |
| T-MC-FAIL-006 | A decoded career with **more than one open tenure** ⇒ throws (F1's decode half). |
| T-MC-FAIL-007 | Decode: wrong `CAREER_SAVE_FORMAT_VERSION` ⇒ throws, version read **first** (F9). |
| T-MC-FAIL-008 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound against `total − offset`, never wraps (F9). |
| T-MC-FAIL-009 | Decode: trailing bytes ⇒ throws (F9). |

## 5.11 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `career-survives-a-sacking`, owning specs `{16, 19, 27, 30, 40, 45, 54}`,
registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

start a career; drive #45's board confidence down through real season results until the tenure rule
fires **mid-season**; assert the manager is unemployed, the tenure is closed with `Sacked`, and reputation
reflects it; **advance the remainder of the season with no managed club**, every fixture resolving through
the round-resolution model; **save and restore in the unemployed state**; assert the world, the table and
the career all match an uninterrupted run; then appoint to a new club and assert the new board confidence
is the **factory** value, the old tenure is **unmodified**, and reputation is unchanged by the
appointment.

This is the composition-level proof that KD-1's ownership, KD-2's projection, KD-4's continue-unemployed
choice, KD-5's optional and KD-7's block hold **together** — and it is the only place the sacking, the
unemployed season and the re-appointment interact at once, which is exactly where a `-1` sentinel (KD-5)
would surface as a crash that no unit test can reach.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5. §5.1 and §5.2 lead with the two locks the spec exists for — the unemployed-save round-trip (the case the current codec cannot construct, so it is simultaneously the acceptance test and the proof the back-prop landed) and the **structural absence** of a reputation field (a prose rule does not survive a contributor who notices the recomputation). T-MC-U-003 is flagged as the guard-ordering lock and T-MC-I-005 as its composed form, since the grace period and the honeymoon value each guard the same case alone. Status IN REVIEW. |
#endregion
