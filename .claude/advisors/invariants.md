# Advisor Invariant Routing

> **Created:** July 31, 2026
> **Status:** TOOLING CONFIG — not a specification, not a design supplement, not authoritative.
> **Purpose:** Route an advisor from *"the change in front of me"* to *"the rule that governs it and
> where that rule actually lives."*

## What this file is, and what it must never become

This is a **routing table**, not a rulebook. Each row names a trigger, the question it forces, and
the **authority** that answers it. The authority is always somewhere else — the root `CLAUDE.md`,
a spec section, a constants catalogue, or the source itself.

This distinction is load-bearing. A file that restated the rules would be a second copy of project
policy sitting next to the real one, drifting the moment either changed — the **parallel-surface
trap** this repo has now filed as a Medium finding at least four separate times (board policy on the
composition root; `POSITION_COUNT` duplicated across two assemblies; a second live-stats accumulator
beside #37's; a re-implemented `LineupSelector` inside `season-save`, refused for exactly this
reason). An advisor that quotes this file *as the rule* has already made the mistake it exists to
prevent.

So: **cite the authority, not this file.** If a row's authority contradicts this file, the authority
wins and the row is a defect — say so in the advice.

If a rule is missing here, that is not permission. Missing means unrouted, and an advisor that
notices an unrouted rule should name the gap.

## Which advisor owns which section

| Sections | Advisor | The question it asks |
|---|---|---|
| §1 Determinism · §2 Architecture · §3 Spec governance | `advisor-integrity` | Does this change respect the contracts the machine runs on? |
| §4 Test adequacy · §5 Football realism · §6 Sequencing | `advisor-evidence` | Is the claim actually proven, and is it the right claim to be making? |

The split is by **mindset**, not by topic count. Integrity reasons about rules the system must not
break; evidence reasons about whether reality has been consulted. A finding that sits on the seam —
"this test would pass on a layering violation" — belongs to whichever advisor sees it first; say so
and move on rather than deferring.

---

## 1. Determinism and snapshot state

| Trigger in the change | Question it forces | Authority |
|---|---|---|
| Any new field that survives across a tick | Does `SNAPSHOT_SCHEMA_VERSION` bump? | `src/match-engine/MatchEngineConstants.cs`; the exclusion proof in `MatchEngine.SerializeWorldState` |
| A field deliberately *not* serialized | Is it reconstructible, and is the exclusion proof updated to say how? | The exclusion-proof comment block; §5.Z Phase G |
| A new RNG draw | Does this need a new stream, or can inverse-transform partitioning split an existing draw? | #16 §3.2.5; the §5.Z.9 foul/card precedent (one draw, two decisions, no new stream) |
| A new domain tag or subsystem ordinal | Does a real draw site exist **in this commit**? | ERR-030-001; FR-LW-031. An ordinal with no stream behind it is a phantom |
| Draw order changed anywhere | Declared explicitly, with the digest movement stated as intended? | The landing's own record; every §5.Z entry states this |
| Resolution order must not matter | Is the draw **keyed**, not cursor-positioned? | ERR-030-012; #30 §3.4.1 |
| Any save/restore path touched | Does save@N → restore → tick to N+K equal the uninterrupted run, byte-for-byte? | `MatchEngineSnapshotRestoreTests`; snapshot-deserialize KD-5 |
| `System.Random`, `DateTime.Now`, `Math.Random` | Forbidden in game logic, no exceptions | Root `CLAUDE.md`, "When Writing Code" |
| Float behaviour, `Fixed64` | Stage 0 is `float`; Fixed64 is Stage 5+. Do not migrate opportunistically | Root `CLAUDE.md`; #9 §8.1 |

**The trap this section exists for:** state that is obviously cross-tick gets serialized; state that
is *incidentally* cross-tick does not. The GK/Heading Phase-2 landing found two trigger latches that
gated re-commits — engine-level, cross-tick, and nearly missed. Ask what a restore would *re-fire*,
not just what it would forget.

---

## 2. Architecture and layering

| Trigger | Question | Authority |
|---|---|---|
| A new assembly reference | Does it run AI → Mechanics → Physics, never the reverse? | `src/CLAUDE.md` layer taxonomy |
| Two assemblies that must not see each other | Is a root **above both** composing them, rather than one referencing the other? | FR-LW-003; the `season-save` precedent above `match-engine` + `living-world` |
| A reference into `match-analytics` or `ui-framework` | Is the consumer sanctioned? No sim assembly may reference either | The mechanical layer-guard tests (allow-list **plus** explicit never-reference list) |
| A rule implemented in a second place | Is this a parallel surface? Move it to the type that owns the concept | `BoardState.EvaluateAtSeasonEnd` (the fix); `TrainingInput` (the seam-ownership precedent) |
| A new interface | Are **both** sides specified today? | Root `CLAUDE.md` "Interface Design Principle"; ERR-001, ERR-004 |
| A new projection, stream, ordinal, or seam | Does a consumer exist in this commit? | FR-LW-031; KD-P8 (GK/Heading projections held back until #10/#11 were wired) |
| A constructor taking an array or list | Is it snapshot-copied, or is the caller's live handle retained? | Repeated M finding: `TacticPreset`, `MatchReplay`, `SeasonState.Table`, arc pin arrays |
| A magic number in formula code | Constants live in catalogue files | Root `CLAUDE.md`; Spec #20 §4.2 |
| A seam type shared between two assemblies | The **consumer** owns it, so no reverse reference is forced | `TrainingInput` in `src/player-progression/`; injury-aging AR-2 |

**The trap:** "it compiles" is not the test. The `decision-tree` production assembly never compiled
for weeks, and six test suites were structurally dead, because nothing walked the whole tree. The
Linux gate closes that now — but layering violations still compile fine.

---

## 3. Spec governance

| Trigger | Question | Authority |
|---|---|---|
| Any new constant | Tagged exactly one of `[GT]` `[EST]` `[FIXED]` `[DERIVED]` `[CROSS]`? | Root `CLAUDE.md` "Constant Tags" |
| A constant copied from another spec | `[CROSS]` only if **verbatim**. Transformed ⇒ `[DERIVED]` | Same |
| The spec was wrong, not the code | Patch the spec in the **same commit**, and file the ERR | Every §5.Z landing; ERR-006-002/003, ERR-001-004, ERR-008-016/017 |
| Filing a new `ERR-` id | Verified free against `docs/tracking/spec-error-log.md` **now**? | The July-27 promotion wave: three supplements proposed ids already filed. A proposed id is a suggestion, never a reservation |
| A spec number appears in prose | Checked against `SPEC_INDEX.md`? | Renumbering cascades are this project's single most recurring bug class |
| A cross-spec obligation discovered | Does it need a back-prop, and does that land atomically? | #30 §3.3.1; the July-27 wave, where landing 23 back-props together exposed a defect no single filing could have |
| "The spec is APPROVED" used as evidence | APPROVED says nothing about whether code exists — 22 of 53 specs have no assembly | Root `CLAUDE.md` PROJECT IDENTITY. **Check `src/` first** |
| A `[GT]` magnitude introduced | Illustrative pending a balance pass, or actually fitted? Say which | #21 G2 precedent |

---

## 4. Test adequacy

| Trigger | Question | Authority |
|---|---|---|
| A new test asserting a fix | Has it been **executed against the pre-fix code** and observed to fail? | Every §5.Z scenario states its pre-fix failure count, verified in a worktree at the pre-fix commit — inferred is not verified |
| A composed / integration test | Does it assert the **outcome the system exists to produce**, or only that it ticks without throwing? | ERR-030-014. The 600-tick capstone asserted tick count, cadence, finiteness, bounds and digest advance — every one true of a match where nothing happens |
| A determinism test | Is it self-referential ("generate twice, compare")? Then a `[GT]` tweak rewrites every save with the suite green | League-bootstrap AR-5 H-1; the golden-vector fix |
| A test proving a fix works | Perturb the fix — does the test actually fail? | Non-vacuity: `SecondSeason_DiffersFromTheFirst` asserted an always-true disjunction |
| Team-relative geometry | Is the away side mirrored? | ERR-008-002: three asymmetry defects shipped because every example and fixture used the home team |
| A reachability predicate over a sampled window | Is the window sized so the event is reliably reached? | AR-4 class: 9-min windows thinned to 3 strikes; MatchAnalytics 30 s window; the P1 non-vacuity guard |
| A gate failure after a landing | Is this the mechanism, or an **instrument**? | §5.Z.22 AR-4/AR-5. An instrument break is not a defect in the change — but it must be fixed at the root, not worked around |

---

## 5. Football realism reference values

Measured behaviour is compared against the real sport, not against the previous build. Cite the
figure and the gap, in that order.

| Quantity | Football | Notes |
|---|---|---|
| Goals per match | ~2.7 | The engine's long-running headline gap; 4.7 as of §5.Z.21 |
| Shots per match | ~25 | |
| Mean shot distance | ~17 m | §5.Z.21 measured 30–34 m pre-fix |
| Shots blocked | ~30% | |
| Shots off target | ~30% | |
| Shot speed | ~20–25 m/s | |
| Fouls per 90 min | ~22 | |
| Yellow cards per 90 min | ~3.5 | |
| Red cards per 90 min | ~0.25 | |
| Shots per final-third entry | ~0.2 | The engine's ~0.05 is the standing creation gap |

**The trap:** a plausible-looking non-zero number is more dangerous than an obviously broken one.
A4a's Step 0 passed at 25–0 scorelines because it asked *"is there signal?"*, not *"is the signal
football?"*

---

## 6. Sequencing

| Trigger | Question | Authority |
|---|---|---|
| Picking the next thing to build | Does `path-to-playable-roadmap.md` already sequence it, and is it blocked upstream? | That file is the live critical path |
| A fix proposed before a measurement | Has the premise been measured? | §5.Z.17 and §5.Z.9 both **refuted their own briefs**. §5.Z.17's named lever moved the goal rate by zero |
| A T-phase landing planned | Budget 1–3 ERR-class findings surfacing from the spec | Roadmap C5. Six consecutive landings hit it |
| A consumer assumed available | Does `src/` contain it? | The assembly map in root `CLAUDE.md` — folder names do not map to spec numbers |
| A calibration proposed | Will an offline sweep give the value, or only the shape? | §5.Z.9: the sweep pointed at 0.025; a live run measured it wrong by 20× |
