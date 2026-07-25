# Board & Ownership Dynamics #45 — Design Supplement

> **Created:** July 25, 2026
> **Last Updated:** July 25, 2026 (v0.3 — AR-2 sweep, CONVERGENCE; prior v0.2 AR-1, v0.1 initial)
> **Version:** 0.3
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row)
> **Candidate spec:** **#45** · **FR prefix:** `FR-BD` · **Wave:** 5 · **Tier:** S3
> **Promoted from:** `docs/tracking/spec-plans/spec-45-board-ownership-dynamics.md` v0.1

---

## 0. Purpose and posture

This supplement resolves the five key decisions the #45 plan defers, against **verified** upstream
source rather than assumption. It is the stage `youth-academy-intake-design.md`, `staff-backroom`, and
`tactical-instruction-layer-design.md` occupied before promotion: design only — no code, no section
files, no registry row.

Every claim about an upstream spec in §2 was checked against that spec's own section files; the
citations name file and requirement so a reviewer can re-verify without trusting this document.

## 1. Scope

**#45 owns:** a persistent per-club **board-confidence** scalar, the **ownership profile** (type + its
dials), and — at the deep tier — **takeover events**.

**#45 does not own** (each already has an owner, and duplicating it is the failure mode this section
exists to prevent):

| Not owned | Owner | How #45 relates |
|---|---|---|
| The season **objective** itself (`FinishAtOrAbove(P)`) and its pass/fail evaluation | #30 (FR-SN-014/015) | #45 **reads** the "on track?" projection as a committed value |
| Budget **numbers** | #40 (FR-FN-002/005/006) | #45 produces the `BoardModifier` #40 already accepts |
| The **sacking decision** | #30 | #45 supplies confidence; #30 decides (KD-3) |
| The **morale mechanics** | #33 | #45 borrows the *shape*, not the state (KD-1) |
| Club **finances** and the wage ledger | #40 | untouched |

## 2. What already exists (verified)

This is the load-bearing section: the five upstream facts below decide most of #45's architecture —
(a) and (b) between them settle both downstream seams, and (b) is the one that turns the plan's stated
risk into a concrete amendment.

**(a) #40 has already specified #45's budget seam.** `club-finances-economy/section-2.md` FR-FN-018/019
and `section-1.md` KD-4 define:

```csharp
public readonly struct BoardModifier
{
    public readonly int BudgetMultiplierMillPermille;   // 1000 = x1.0
    public static BoardModifier Identity => new(1000);
}
```

with `default(BoardModifier)` (all-zero, ×0) an explicit **fail-loud** F4, and FR-FN-019 stating in
terms: *"#45 becomes the producer of a non-identity `BoardModifier` when it exists."* FR-FN-027 pins the
direction `#45 → #40`, and `section-7.md` adds *"#45 MUST NOT add a second budget-multiplier path."*

**Consequence:** #45's budget seam needs **no new #40 back-prop** — the contract is already written, and
#45's obligation is to fit it rather than extend it. This is the cleanest downstream seam any Wave-5/6
spec has inherited.

**(b) #30 owns board state today, including a job-security scalar.** `season-competition-loop/section-2.md`:

- FR-SN-014 — *"`BoardState` MUST hold the literal Stage-0 objective (`FinishAtOrAbove(position P)`) and
  a job-security scalar / state."*
- FR-SN-015 — evaluation runs at the season boundary, **and** *"MUST expose a running 'on track?' read
  from the current table position (a projection, not a mutation of the objective)."*
- §2.2 — `BoardState` = `{ Objective (BoardObjective), JobSecurity (float/enum) }`, serialized by
  `WriteBoard(state.Board)` inside the season block (§3.6), evaluated at boundary-roll step (b).

**Consequence:** this is the **double-truth risk** the plan's §9 names, and it is real. KD-5 resolves it.
It also surfaces a second issue the plan did not anticipate — `JobSecurity` is typed *"float/enum"*,
while #33, #40, #41, #28 and #42 are all integer-per-mille throughout. See KD-5.

**(c) #33 anticipates a #45 read, and its shape is the one to borrow.**
`personalities-morale-dynamics/section-2.md` FR-HS-024 lists #45 among the read-only morale consumers
(all **deferred**, FR-LW-031 — no interface built ahead of the consumer). §3.1 gives the shape:

```
DriftPermille(cur, tgt, step) = cur + sign(tgt − cur) · min(step, |tgt − cur|)      # clamp [0,1000]
```

guarded by a `LastAdvancedWorldDay` idempotency cursor whose unadvanced sentinel is `uint.MaxValue`
(**not** `0` — FR-HS-008), with same-day re-advance a **no-op** and a day **gap** a **fail-loud** (F6).

**(d) #33 already carries a board input.** `HumanSystemsDayInput.BoardObjectiveDeltaPermille` — *"committed
board-state nudge"* — exists today with no producer. #45 is its natural deep-tier producer (§8.3).

**(e) The tick order and the ERR numbering.** `section-3.md` §3.3 `RunWorldTickInFixedOrder` currently
ends: … #34 staff = step 6, #42 academy = step 7, `WorldStore.AdvanceDay()` = step 8 (the only live
tick). `spec-error-log.md` shows `ERR-030-005` soft-reserved by #31, `-006` = #34, `-007` = #42 — so
**`ERR-030-008` is the next free number**.

## 3. Staging (minimal-first → deep)

| Tier | Content |
|---|---|
| **Minimal (the identity)** | One per-club confidence scalar, integer per-mille, drifting toward a target derived from #30's committed "on track?" projection. Ownership = a single generic profile whose dials are **exactly** identity. **No draw, no stream, no takeover.** `BoardModifier` = `Identity` ⇒ #40's budget is exactly `f(finalTablePosition, prizeMoney)`. |
| **Deep** | Differentiated ownership types (ambitious / frugal / absentee) as **dials on the same code path**; multi-factor confidence (finances, transfer activity, results); **takeover events** — the first and only stochastic surface; a #33 morale read as a confidence input. |

The deep tier **extends** the minimal identity; it never rewrites it (the #21 / #40 KD-8 posture).

## 4. Key decisions

### KD-1 — Board confidence is a morale-model **analogue**, not #33 state

Confidence reuses #33's *shape*: integer per-mille in `[0, 1000]`, `DriftPermille` toward a computed
target, a `LastAdvancedWorldDay` idempotency cursor with the `uint.MaxValue` unadvanced sentinel, the
same same-day-no-op / day-gap-fail-loud guard, and no float anywhere.

It is **club-scoped, keyed by `ClubId`**, and is **#45-owned state** — not a `MoraleState`, not a
`PairwiseRelationship`, and emphatically not a #33 `PlayerEdge`. #33's FR-HS-002 makes #33 the sole
writer of its per-`PlayerId` state; a club-scoped board relationship is a different entity keyed on a
different id, so putting it in #33 would violate that ownership, not honour it.

**Sub-decision — #45 declares its own drift helper rather than referencing #33 for it.**
`DriftPermille` is a three-operation pure integer function. Referencing the whole `#33` assembly to
borrow it would create a dependency whose only purpose is a one-liner, and would drag a per-player state
model into a club-scoped subsystem. #45 declares its own and pins **semantic equivalence** by test
(same inputs → same outputs as the #33 definition, asserted against the §3.1 formula as written).

*Rejected alternative:* extract a shared integer-math primitives assembly now. Rejected as premature —
two call sites do not justify a new bottom-of-graph assembly, and the extraction stays available later
(recorded in §11 as a standing option, not a debt).

**Deferred:** reading #33 morale as a confidence input. FR-HS-024 anticipates it, but building the read
before the deep tier needs it is the phantom-consumer class FR-LW-031 forbids.

### KD-2 — Takeovers are **deep-tier**, so `0x2D`/95 stays RESERVED, not promoted

The minimal tier is a deterministic projection with **no stochastic draw**, so #45 registers **no RNG
stream** and promotes **no domain tag** at approval. The roadmap-§6 `0x2D` / `SubsystemOrdinals 95`
allocation lands as a `_RESERVED_0x2D_` **placeholder** — the `_RESERVED_0x29_` (#40, ERR-040-001),
`_RESERVED_0x21_` (#29) and #33 FR-HS-013 precedent. It promotes to
`DOMAIN_TAG_BOARD_OWNERSHIP = 0x2D` at #45's first takeover draw.

**When it does promote, the draw is keyed and single-stream:**

- **One** stream for the whole subsystem — siteId `board.takeover`, `entityId = BOARD_STREAM_ENTITY_SENTINEL`
  (a fixed sentinel, the `world.text` / `world.arcs` precedent), registered **once** regardless of how
  many clubs exist. The sentinel's *value* needs no coordination with living-world's `-1` / `-2`: a
  stream key is computed over the subsystem ordinal **and** the entity id, and #45's ordinal (95) differs
  from living-world's (80), so the keys cannot collide even on an identical sentinel. The collision
  hazard that forced `world.arcs` onto `-2` was *within* one subsystem ordinal.
- The club is folded into a **position-independent keyed action ordinal** on
  `(clubId, worldDay, purpose)` with a **fixed** `DRAW_PURPOSE_RADIX` — the #41/#42/#28 idiom — so there
  is **no free-running cursor to persist** (§7).

This matters beyond #45: `DeterministicRngService.RegisterStream` appends into a bounded, never-shrinking
table (`MaxRngStreams` = 64, no unregister), and #42 §7.4 R-1 records that a per-club registration model
would exhaust it in a full-world career. **#45's single-stream + keyed-ordinal model does not contribute
to that bound at any tier** — one registration, forever. This is a deliberate contrast with a per-entity
`entityId` model, recorded here so the choice is not later "simplified" into one.

**What a takeover mutates:** #45-owned state **only** — the `OwnershipProfile` and the confidence/patience
dials. It writes **nothing** in #40 or #30. The budget effect happens *downstream and one-directionally*,
because the `BoardModifier` #45 projects is read by #40's `SettleFinances` at the next season boundary.
There is no takeover → budget write path, and therefore no coupling knot (the plan's §9 third risk).

### KD-3 — #45 supplies confidence; #30 decides the sacking

Strictly one-directional: **`#30 → #45`**. #45 exposes a read-only confidence accessor and **no** sacking
API, fires no event that terminates a manager, and **never references #30**. #30 reads the confidence
value and applies its own threshold at whatever cadence it owns.

The plan's phrasing — *"#45 never fires the manager itself"* — is adopted as a hard structural property,
asserted by assembly-reference absence (the #40 `T-FN-BOUND-002` posture), not merely by convention.

### KD-4 — Ownership types: one code path, dials

A single `OwnershipProfile` value type carries per-mille dials (expectation severity, patience/decay
rate, budget multiplier contribution). Minimal ships **one** generic profile whose dials are exactly
identity; deep-tier types are *values*, not new code paths or subclasses.

**Zero-value trap, handled up front:** `OwnershipProfile.Identity` is an **explicit factory**
(all dials `1000` per-mille), and `default(OwnershipProfile)` — all-zero, i.e. ×0 — is **not** a valid
runtime value and **fails loud** at the consuming seam. This is #40's `BoardModifier` F4 and #41's
`MedicalModifier` lesson applied proactively rather than discovered in a later AR round — the same move
#40 §1.6 made deliberately.

### KD-5 — Reconciliation with #30: split the **target** from the **relationship**

This is the plan's central risk ("objective double-truth with #30"), and the split is:

| Concern | Owner | Notes |
|---|---|---|
| `BoardObjective` — the *target* (`FinishAtOrAbove(P)`) | **#30**, sole writer | unchanged |
| Season-boundary pass/fail evaluation | **#30** | unchanged (roll step (b)) |
| The running **"on track?"** projection | **#30** (FR-SN-015 already mandates it) | #45's daily input |
| Persistent board **confidence** | **#45** | the new state |
| `BoardState.JobSecurity` | **#30 field → derived band over #45's confidence** at #45's T2 | the back-prop |

**#30 routes the "on track?" projection into #45 as a committed value**, exactly as it routes
`HumanSystemsDayInput` into #33 — so #45 references neither #30 nor the league table, and the
provenance is enforced at #30's call seam. #45's own input struct carries integers only, and — like
`HumanSystemsDayInput.Neutral` — has an explicit **neutral** value for a day on which nothing happened.
On a non-fixture day the on-track projection is unchanged, so confidence drifts toward a static target;
that is the intended behaviour (#33's morale does the same), not a missing input.

**The unmodelled-club contract (the seam that most needs one).** #30's boundary roll calls
`SettleFinances` **per club**, and #40's FR-FN-025 fails loud for a `ClubId` with no entry. #45's minimal
tier populates the **managed club only** (§5), so most clubs have no board entry — and #45 must not
force a choice between "fail loud on every AI club" and "silently invent an entry". Neither is right, so
the seam is explicit instead:

```
TryProjectBoardModifier(clubId, out BoardModifier) -> bool     # false = club not modelled by #45
```

#30 substitutes `BoardModifier.Identity` when it returns false. "Not modelled" becomes a **named, legal
state** rather than an error or a silent default, fail-loud is preserved for genuinely malformed input
(a present-but-corrupt profile still throws), and KD-8's identity claim holds for every club — the
modelled one because its dials are identity, the rest because #30 supplies identity. A single
`ProjectBoardModifier` that returned identity for an absent club would have conflated the two cases and
made a bootstrap bug indistinguishable from normal operation.

**The `JobSecurity` amendment (and why it is worth making).** FR-SN-014 types `JobSecurity` as a
*"float/enum"* scalar held in `BoardState` and serialized by `WriteBoard`. Once #45 owns a persistent
confidence scalar, an independent #30 job-security scalar is a **second truth for the same quantity** —
they would drift apart at the first restore, and nothing would detect it. At #45's T2, `JobSecurity`
becomes a **derived band** (an enum) projected from #45's per-mille confidence, not independent state.

Two things fall out, both improvements:

1. The double truth is eliminated by construction rather than by discipline.
2. The **float leaves the season board block.** #33 (FR-HS-004), #40 (FR-FN-011), #28, #41 and #42 are
   integer-only by requirement; `JobSecurity`'s float was the odd one out, and it sat inside a
   round-trip-deterministic save block. Replacing it with an enum band over an integer per-mille is
   strictly better for the FR-SN-022 byte-identity contract.

**The consequence this amendment carries, stated rather than discovered later:** `JobSecurity` is
serialized inside #30's season block by `WriteBoard` (§3.6). Changing its representation from a float
scalar to an enum band **changes that block's byte layout**, so it is a **`SEASON_STATE_FORMAT_VERSION`
bump** — #30's own version, owned and bumped by #30, and distinct from both `SEASON_SAVE_FORMAT_VERSION`
(the outer frame, §8.2) and #45's `BOARD_SAVE_FORMAT_VERSION`. Three independent versions, none implying
the others, exactly as #42 and #40 established. The bump lands with the amendment's *effect* at #45 T2,
not with the spec-text filing at approval.

This is a **#30-side change**, filed as a back-prop at #45's approval (§8.1), and it is the one place
#45's landing is not purely additive — recorded plainly rather than buried.

### KD-6 — Persistence: an opaque, independently version-gated sub-blob

`BOARD_SAVE_FORMAT_VERSION` [FIXED] = 1. #45's state (per-club confidence + cursor, ownership profile,
takeover history/pending) lands as its own sub-blob composed into #30's `SeasonSaveCodec` — **not** a
`WORLD_STORE_FORMAT_VERSION` bump. The outer codec never parses it (the #40/#42 pattern). Version gate
read **first**; overflow-safe `Require(offset, need, total)` length prefixes compared against
`total − offset`; trailing-byte guard. Fail loud on all three.

**Deliberately absent: any RNG cursor** (KD-2's keyed draws make the next takeover roll a pure function
of `(worldSeed, clubId, worldDay)`), and **no copy of #30's objective or #40's budget** — mirroring
either would re-introduce the double truth KD-5 just removed.

### KD-7 — Cadence: the world tick, at a new #30 slot

Confidence advances **once per world day** on `WorldClock` — never the 10 Hz/60 Hz match loops — at a
new #30 tick-order slot **8** (after #42's academy seam at 7), pushing `WorldStore.AdvanceDay()` to 9.
Filed as **ERR-030-008** (§8.1).

Unlike the #31/#34 deep-tier *position reservations*, and like #42's, this seam **goes live at #45's own
T2** — the daily confidence drift is #45's minimal tier. Its cost is one clamp and one comparison per
club per day; §6 of the spec will carry the budget.

**Ordering consequence — a pinned one-day-stale board→morale contract.** #33 occupies slot **3** and
#45 slot **8**, so when #45 becomes the deep-tier producer of
`HumanSystemsDayInput.BoardObjectiveDeltaPermille` (§2(d)), #33 will read the value #45 committed on the
**previous** day. Two ways out, and the choice is deliberate:

- *Move #45 ahead of #33.* Rejected — slots 1–7 are pre-declared positions that six approved specs
  (#28/#29/#33/#41/#31/#34/#42) already cite by number; renumbering them to spare a one-day lag would
  force a re-pin across every one of them, which is precisely the cost FR-SN-034's fixed order exists to
  avoid.
- *Accept and pin the staleness.* Chosen. A board-confidence nudge to morale is a slow-moving signal
  where one day of lag is semantically invisible, and the project already has this exact contract in
  #23's one-stride-stale dismark carriers.

The point is that it must be **pinned**, not implicit: an unstated lag is the kind of thing a later
maintainer "fixes" by reordering slots and silently breaks six specs' cited positions. #45's §3 will
state it as a contract, and its §5 will lock it with a test.

### KD-8 — Behaviour-neutral identity

With the generic ownership profile and identity dials: `BoardModifier` = `Identity` ⇒ #40's
`SettleFinances` yields **exactly** `budget = f(finalTablePosition, prizeMoney)`, unchanged from
pre-#45; no stream is registered ⇒ every existing stream's cursor is **byte-identical** (the
#22/#26/#28/#29/#40/#41/#42 stream-independence precedent); and a season advanced with the #30 board
seam null is byte-identical to the same season pre-#45 (the FR-SN-026 world-floor property).

## 5. Persistent state (shape)

```
BoardConfidence      per ClubId : { ConfidencePermille [0,1000], LastAdvancedWorldDay (uint, sentinel uint.MaxValue) }
OwnershipProfile     per ClubId : { Type (enum), ExpectationSeverityPermille, PatienceDecayPermille,
                                    BudgetContributionPermille }        # Identity = all 1000
TakeoverState        per ClubId : { LastTakeoverWorldDay (uint), TakeoverCount (int) }   # deep tier; empty at minimal
```

All integer. The store may hold any subset of clubs; the minimal tier populates the **managed club**
only, which keeps the shape world-ready without implying an AI-manager sacking model that does not exist.
Absence is a legal state with a defined contract, not a gap — see KD-5's `TryProjectBoardModifier`.

**Layout discipline.** The sub-blob is **APPEND-only**: `TakeoverState` is written at the minimal tier
with zeroed fields, and the deep tier's additional fields go at the **end** with a
`BOARD_SAVE_FORMAT_VERSION` bump — never inserted mid-block, which would shift every subsequent offset
(the #42 Appendix B rule).

**What #45 does *not* define:** the semantics of the "on track?" projection itself — including
degenerate cases such as an all-square table before any fixture is played. That projection is #30's
(FR-SN-015); #45 consumes whatever integer #30 commits. Recording the boundary here prevents a later
"which spec is wrong?" argument over a case neither had claimed.

## 6. Determinism posture

- World tick only; no match-loop coupling.
- Minimal tier: **draw-free**, deterministic projection ⇒ no stream, no tag promotion (KD-2).
- Deep tier: one stream, keyed position-independent draws, no persisted cursor (KD-2).
- All-integer arithmetic; no float enters #45 at any tier (and KD-5 removes one from #30's block).
- Same-day re-advance = no-op; day gap = fail loud (the #33 F6 guard, adopted verbatim).

## 7. Primary surfaces (proposed)

| Surface | Direction | Notes |
|---|---|---|
| `AdvanceBoardDay(ref BoardConfidence, in OwnershipProfile, in BoardDayInput, worldDay)` | #30 → #45 | the daily drift; committed values in |
| `ConfidenceOf(in BoardConfidence) → int` | #45 → #30 | read-only; #30 applies its own sacking threshold |
| `TryProjectBoardModifier(clubId, out BoardModifier) → bool` | #45 → #40 | the FR-FN-019 producer #40 already specified; `false` = club not modelled, caller supplies `Identity` (KD-5) |
| `AdvanceTakeovers(...)` | #30 → #45 | **deep tier only**; the sole `0x2D` draw site |
| A read-only board view-model | #45 → #38 | value copies (the `FinancesViewModel` posture) |

## 8. Cross-spec back-props

### 8.1 At approval (must land atomically with the status flip)

| ID | Target | Change |
|---|---|---|
| **ERR-030-008** | #30 §3.3 tick order | Board null seam as **step 8** (after #42's academy at 7); `AdvanceDay()` → step 9; FR-SN-034 enumeration extended. Goes live at #45 T2 (KD-7). |
| **ERR-030-009** | #30 §2 FR-SN-014 + §2.2 `BoardState` + §3.6 `WriteBoard` | `JobSecurity` becomes a **derived enum band** over #45's per-mille confidence rather than independent state (KD-5); removes a float from the season block. Carries a **`SEASON_STATE_FORMAT_VERSION` bump** (#30-owned) when the effect lands at T2 — spec-text-first at approval, the ERR-028-001 ◑ pattern. |
| **ERR-045-001** | #16 §3.4 | `_RESERVED_0x2D_` placeholder + `SubsystemOrdinals.BoardOwnership = 95`, **RESERVED not promoted** (KD-2). |

### 8.2 Deferred (land at the named tier, not at approval)

- Promotion of `_RESERVED_0x2D_` → `DOMAIN_TAG_BOARD_OWNERSHIP` at the first takeover draw (deep tier).
- The outer `SEASON_SAVE_FORMAT_VERSION` bump, at T2 when the sub-blob is first composed in.
- A #33 morale read as a confidence input (FR-HS-024 anticipates it; deferred per FR-LW-031). **When it
  lands it arrives as routed committed values, not an assembly reference** — the same posture by which
  #33 itself receives #30's inputs. This preserves §10's DAG at every tier; see the note there.

### 8.3 Explicitly **not** back-props

- **#40** — nothing to change. FR-FN-018/019/027 already specify the `BoardModifier` seam and the
  `#45 → #40` direction; #45 fits the existing contract (§2(a)).
- **#33** — `HumanSystemsDayInput.BoardObjectiveDeltaPermille` already exists with no producer. #45 may
  become that producer at the deep tier, which is a #45-side wiring change, not a #33 amendment.

## 9. Test focus

Identity (minimal confidence path is byte-identical to pre-#45 at the #40 and #30 seams); round-trip
determinism over the sub-blob including the cursor sentinel; the F6 no-op / gap-guard pair; drift
monotonicity and `[0,1000]` clamp; `default(OwnershipProfile)` fail-loud; **structural** assertion that
#45's assembly references neither #30 nor #33 (KD-3); and — at the deep tier — takeover
position-independence (a takeover preceded by a different number of prior draws yields the same result).

## 10. Reference DAG

```
root → {#30, #45}          #30 → {#28, #40, #45}          #45 → {#27, #16}          #45 → #40
```

**Acyclic.** #45 references neither #30, #33, `living-world`, `SeasonSave`, nor `MatchEngine`.

**This holds at every tier, including the deep one.** The deferred #33 morale input (§8.2) does not
weaken it: morale reaches #45 as **routed integer values** supplied by the caller, never by #45
referencing #33 — the identical mechanism by which #33 receives #30's match results without referencing
#30. So the structural assertion in KD-3 and §9 is unconditional, not "true until the deep tier", which
is what makes it testable by assembly-reference absence rather than by review vigilance.

## 11. Risks and standing options

- **R-1 — the KD-5 amendment is the one non-additive change.** If #30's `JobSecurity` has acquired a
  consumer by the time #45 lands, the band projection must preserve that consumer's observable
  behaviour. Re-verify at promotion.
- **R-2 — confidence/objective drift.** Mitigated by construction (KD-5: #45 holds no copy of the
  objective), but a future maintainer adding a cached objective to #45's blob would re-open it. Called
  out in the appendix note.
- **R-3 — standing option:** extract a shared integer-primitives assembly if a third `DriftPermille`
  call site appears (KD-1). Not a debt today.

## 12. Promotion pipeline

The steps from here to `APPROVED`, per `spec-plans/README.md`:

1. **This supplement, AR-converged** — done at v0.2 (AR-1 0H+4M+2L → AR-2 0H+0M+3L, an L-only round
   closing the cycle per the project convention).
2. **Author 11 section files** at `Status: IN REVIEW` under `docs/specs/board-ownership-dynamics/`
   (`outline`, `section-1`..`section-8`, `section-9-approval-checklist`, `appendices`), FR prefix
   `FR-BD`.
3. **Section-file PASS-1 adversarial review** + a v0.2 fix pass, recorded in §9.4.1 of the checklist.
4. **`SPEC_INDEX.md` registry row** added at promotion (never at supplement stage).
5. **Lead-developer R-01..R-05 sign-off** — a human authority, not self-grantable.
6. **Flip to `APPROVED`**, landing the three §8.1 back-props **atomically** with the flip.

## Version History

| Version | Date | Change |
|---|---|---|
| v0.1 | July 25, 2026 | Initial supplement promoted from the one-page plan. Resolves KD-1..KD-8 against verified upstream source; records the #40 pre-existing seam (no back-prop), the KD-5 `JobSecurity` reconciliation (ERR-030-009), and the single-stream keyed-draw model that keeps #45 off the `MaxRngStreams` bound. |
| v0.2 | July 25, 2026 | **AR-1 fix pass: 0H + 4M + 2L, all resolved.** **M-1** — the `ProjectBoardModifier` seam had no contract for a club #45 does not model, yet #30 settles finances **per club** and #40 fails loud on an unknown club; with the minimal tier modelling only the managed club, most clubs hit that seam every boundary. Replaced with `TryProjectBoardModifier(clubId, out …) → bool`, #30 substituting `Identity` on `false`, making "not modelled" a named legal state while preserving fail-loud for corrupt input. **M-2** — the KD-5 `JobSecurity` amendment changes a field serialized by #30's `WriteBoard`, so it carries a **`SEASON_STATE_FORMAT_VERSION` bump**; the supplement claimed the change without its save-format consequence. **M-3** — §10 asserted #45 references #33 at no tier, while §8.2 deferred a #33 morale read; resolved by pinning that input as **routed values, not an assembly reference**, so the DAG claim is unconditional and structurally testable. **M-4** — #33 sits at tick slot 3 and #45 at slot 8, so #45-produced board deltas reach #33 **one day stale**; pinned as an explicit contract (with the reorder alternative rejected: slots 1–7 are cited by number in six approved specs) rather than left implicit. **L-1** — added the non-fixture-day neutral-input semantics. **L-2** — noted the stream-entity sentinel needs no coordination with living-world's `-1`/`-2`, since stream keys are scoped by subsystem ordinal. |
| v0.3 | July 25, 2026 | **AR-2 sweep: 0H + 0M + 3L, all resolved — CONVERGENCE** (an L-only round closes the cycle). **L-1** — §2's preamble said "three upstream facts" over a five-item list. **L-2** — §5 did not state the sub-blob's APPEND-only discipline, so the deep tier's `TakeoverState` fields had no recorded placement rule or version-bump obligation. **L-3** — recorded that #45 does **not** define the "on track?" projection's semantics (including the degenerate pre-first-fixture table), since that is #30's FR-SN-015; stating the boundary now prevents a later ownership argument over a case neither spec had claimed. Added §12 promotion pipeline. |
