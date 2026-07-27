# Manager Career, Reputation & Job Market #54 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

All arithmetic is **integer** (FR-MC-005), and **no formula below makes a stochastic draw** (FR-MC-024) —
the minimal tier has no draw site, and §3.5's S3 job-market interest is specified but not built.

## 3.1 `EvaluateTenure` — the rule §1.4(a) leaves unowned (FM-MC-01)

Invoked by #30 at its boundary/day slot. `input` carries **committed integers** the root routes in — #54
names neither a #45 nor a #30 type (FR-MC-003).

```
EvaluateTenure(in TenureEvaluationInput input, uint worldDay) -> TenureVerdict:
    RequireInRange(input.ConfidencePermille, 0, 1000)                  # F3
    RequireInRange(input.ObjectiveOutcome, MC_OBJECTIVE_MIN, MC_OBJECTIVE_MAX)   # F3
    RequireOpenTenure()                                                # F2

    # 1. A grace period: a manager is not sacked in his first weeks whatever the board thinks.
    if worldDay - CurrentTenure().StartWorldDay < MC_GRACE_PERIOD_DAYS:  return Continue

    # 2. The board's standing is the primary term. Below the floor, the objective cannot save him.
    if input.ConfidencePermille < MC_TERMINATE_CONFIDENCE_FLOOR:         return Terminate

    # 3. In the band above the floor, a failed objective is what tips it.
    if input.ConfidencePermille < MC_AT_RISK_CONFIDENCE_MAX
       and input.ObjectiveOutcome < MC_OBJECTIVE_NEUTRAL:                return Terminate

    return Continue
```

**A pure rule, and that is a requirement rather than a style** (FR-MC-008). It mutates nothing — including
**#45's state** (FR-MC-009), which is the direction `FR-BD-012` exists to protect. #54 is a *reader*; if
evaluation could nudge confidence, #45 would have two writers and its single-truth guarantee would be
gone.

**The grace period is not a nicety.** Without it, appointing a manager to a club whose confidence is
already low — the realistic case, since clubs sack managers when things are going badly — would terminate
him on the **first evaluation after appointment**. FR-MC-017's honeymoon value addresses the same problem
from #45's side; the grace period is #54's own guard, and having both means neither is load-bearing alone.

**The two-band shape is deliberate.** A single threshold makes the objective irrelevant; a pure objective
rule makes confidence irrelevant. The floor is *"the board has lost patience regardless"*; the band above
it is *"the board is unconvinced, and the season settled it"*.

**Order of the guards matters:** range checks first (a corrupt routed value must never produce a verdict),
then the grace period (which short-circuits before either threshold), then the thresholds. Reordering the
grace check after the floor would terminate a newly-appointed manager at an inherited crisis confidence —
precisely the case KD-4 exists to prevent.

## 3.2 `Terminate` — the consequence (FM-MC-02)

```
Terminate(EndReason reason, uint worldDay):
    RequireOpenTenure()                                     # F2
    RequireDefined(reason)                                  # F4
    Require(reason != EndReason.Open)                       # F4 -- Open is a state, not an ending
    ref t := ref career.Tenures[career.CurrentTenure]
    Require(worldDay >= t.StartWorldDay)                    # F5

    t.EndWorldDay := worldDay
    t.Reason      := reason                                 # the tenure is now CLOSED and FROZEN
    career.CurrentTenure := MC_UNEMPLOYED                   # -1: the career CONTINUES (FR-MC-010)
```

**The career continues.** `CurrentTenure = MC_UNEMPLOYED` is the successor state §1.4(b) showed the save
format cannot currently express, and it is the whole reason ERR-030-021 exists. Ending the career instead
would make the game's answer to its own most dramatic event *"load your last save"*.

**The closed tenure is frozen, never rewritten** (FR-MC-011). A later appointment **appends**; it does not
edit history. That is what makes the reputation projection stable — a reputation computed today over a
record that could be retrospectively edited would be a projection in name only.

**Unemployment is `CurrentTenure == -1`, not "the last tenure happens to be closed"** (F6). The two are
distinguishable and only the first is checkable: a decoded career whose last tenure is closed but whose
`CurrentTenure` points at it is **incoherent**, and §5.4 asserts it.

## 3.3 `Appoint` — the mirror, and its documented trap (FM-MC-03)

```
Appoint(int clubId, uint worldDay):
    RequireNoOpenTenure()                                   # F1 -- a manager holds at most one club
    Append(career.Tenures, new Tenure {
        ClubId = clubId, StartWorldDay = worldDay,
        EndWorldDay = 0, Reason = EndReason.Open,
        SeasonsServed = 0, Finishes = [], Trophies = [] })
    career.CurrentTenure := career.Tenures.Length - 1
    # NOTE what is absent: NO BoardConfidence insertion. That is the command layer's (FR-MC-016).
```

and, at the **command layer**, the join #54 must not perform:

```
# root-side -- the root already references both #54 and #45
OnAppointCommand(managerId, clubId, worldDay):
    if (!board.HasEntry(clubId))
        board.Insert(clubId, BoardConfidence.Create(),          # the FACTORY, never default() -- F8
                             OwnershipProfile.Identity);        # #45 FR-BD-005a: a GUARDED PAIR
    career.Appoint(clubId, worldDay);                           # #54 records the tenure ONLY
```

**This ordering is the whole of KD-4's second half.** #45's `FR-BD-005a` requires the pair to be inserted
**factory-built and guarded at insertion**, because `default(BoardConfidence)` is *field-in-range yet
semantically severe*: confidence `0` is the `Critical` band — *"dismissal imminent"* — with a
`LastAdvancedWorldDay = 0` that no-ops the day-0 guard. A naive appointment that default-constructs it
hands the manager **a new job in crisis on day one**, and #45's insertion guard throws — the *good*
failure, but still a crash on an ordinary career action.

**Three things #54 deliberately does not do here**, each of which would be natural:

1. **It does not insert the confidence itself.** That would give #54 a **write into #45's store**,
   breaking both #54's leaf position (FR-MC-003) and #45's one-directional guarantee.
2. **It does not inherit the predecessor's standing.** Confidence is the board's view of the **current**
   manager, and the new manager has no record at that club yet. Inheriting a crisis is a defensible
   *design* — but it must be **chosen**, not arrived at by reusing whatever was in the store.
3. **It does not clear the record.** The career is APPEND-only; a new tenure is the `n+1`th entry.

## 3.4 `ReputationOf` — the projection (FM-MC-04)

```
ReputationOf() -> int:                                       # per-mille, [0, 1000]
    acc := MC_REPUTATION_BASE
    foreach t in career.Tenures:                             # ALL tenures, open and closed
        acc := acc + t.SeasonsServed * MC_REP_PER_SEASON
        acc := acc + t.Trophies.Length * MC_REP_PER_TROPHY
        foreach f in t.Finishes:
            acc := acc + FinishTerm(f)                       # positive for a good finish, negative for a bad one
        acc := acc + EndReasonTerm(t.Reason)                 # Sacked is the only negative row (Appendix C)
    return Clamp(acc, 0, 1000)
```

**Computed on read, stored nowhere** (FR-MC-012/013) — the whole of KD-2. A stored scalar beside a stored
history is two representations of one thing, and `ERR-030-009` documents exactly what happens: they
*"diverge at the first restore, with nothing to detect it"*, because both values are individually
plausible. **A projection cannot diverge from its source.**

**It depends only on the record** (FR-MC-014) — never on the current club, the current confidence, or the
world day. This is a stronger constraint than it looks: making reputation respond to *current* confidence
would make it move without the record changing, which is exactly the "second truth" behaviour KD-2
forbids, arrived at from the other direction.

**Open tenures count.** A manager three seasons into a successful spell has earned that reputation
already; excluding open tenures would make a reputation jump on termination, which reads as a bug and is
one.

**`EndReasonTerm` is why FR-MC-015's ordinal contract is doubly load-bearing** (Appendix C): the ordinal
is serialized **and** indexes a weight table, so reordering `EndReason` re-reads every historical tenure
**and** changes every historical reputation — neither with a version gate to catch it.

**Overflow and clamping.** With `MC_REP_TERM_ABS_MAX` bounding every per-term constant and a career
bounded by `MC_MAX_TENURES`, the accumulator is three orders of magnitude inside `int` before the final
clamp. The clamp is at the end, not per term, so a bad early spell can be recovered from rather than
saturating the projection at zero.

## 3.5 `Attractiveness` and the S3 job market (FM-MC-05, partly deferred)

```
Attractiveness(in VacancyInput v) -> int:                    # per-mille, [0, 1000]
    acc := ScaleDial(v.LeaguePositionPermille, MC_ATTR_W_POSITION)
         + ScaleDial(v.FinanceHealthPermille,  MC_ATTR_W_FINANCE)
         + ScaleDial(v.FacilityLevelPermille,  MC_ATTR_W_FACILITY)
         + ScaleDial(v.SquadStrengthPermille,  MC_ATTR_W_SQUAD)
    return Clamp(acc / MC_ATTR_W_TOTAL, 0, 1000)
```

**A read-only projection over root-supplied values** (FR-MC-022). #54 references neither #40, #53 nor #27
— the same value-input pattern #42, #29 and #53 already use, and the reason #54 stays a leaf.

**A vacancy is a property of a club, not the aftermath of a rival's sacking** (FR-MC-020/021). At the
minimal and S3 tiers a club is *"looking for a manager"* and **nobody was sacked from it, because there
was nobody there.** #54 must not emit an event implying otherwise. When #22's phase-5 `BackgroundTierSim`
lands, rival managers become real and the vacancy **source** is replaced behind this unchanged surface —
a producer swap, not a redesign (FR-MC-023).

**The S3 interest draw is specified here and not built** (FR-MC-024/026). When it lands: **one**
subsystem-wide stream with **keyed, position-independent** action ordinals over `(clubId, worldDay,
purpose)` at a fixed radix — never one stream per club or per vacancy, because `RegisterStream` appends
into a bounded, never-shrinking table (`MaxRngStreams = 64`, no unregister). That is the point at which
`_RESERVED_0x2E_` promotes, **on the record** (FR-MC-025).

## 3.6 Arithmetic convention (pinned)

Every expression above is exact integer arithmetic. The **two** divisions — `ScaleDial`'s per-mille scale
and §3.5's weight normalisation — are C# **integer division, truncating toward zero**, which is
**sign-symmetric**. Two substitutions would break that symmetry and are therefore forbidden:

- `Math.Floor` — rounds toward −∞, so a negative reputation term would move one per-mille further than
  its positive mirror;
- `Math.Round` — banker's rounding **and** it operates on `double`, violating FR-MC-005 outright.

§5.3 locks the symmetry directly (`±N` terms produce equal-magnitude opposite moves), so a future
"cleanup" fails a test rather than quietly skewing every negative case.

## 3.7 Worked examples (hand-verifiable)

At `MC_GRACE_PERIOD_DAYS = 90`, `MC_TERMINATE_CONFIDENCE_FLOOR = 200`, `MC_AT_RISK_CONFIDENCE_MAX = 400`,
`MC_OBJECTIVE_NEUTRAL = 500`, `MC_REPUTATION_BASE = 300`, `MC_REP_PER_SEASON = 10`,
`MC_REP_PER_TROPHY = 60`, `EndReasonTerm(Sacked) = −40`, `MC_UNEMPLOYED = -1`.

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Confidence 150, objective 800, day 400, tenure started day 0 | past grace; `150 < 200` | **`Terminate`** — the floor overrides a good objective |
| (b) | Confidence 150, day 30, tenure started day 0 | `30 < 90` | **`Continue`** — the grace period short-circuits **before** the floor |
| (c) | Confidence 350, objective 800 | past grace; `350 ≥ 200`; `350 < 400` but `800 ≥ 500` | **`Continue`** — unconvinced board, objective met |
| (d) | Confidence 350, objective 300 | `350 < 400` and `300 < 500` | **`Terminate`** — the band case |
| (e) | Confidence 700, objective 100 | `700 ≥ 400` | **`Continue`** — a backed manager survives a bad season |
| (f) | Confidence 1100 (corrupt routed value) | range check | **throws** (F3) — never a verdict from a corrupt input |
| (g) | `Terminate(Sacked, day 400)` on the open tenure | closes it | `EndWorldDay = 400`, `Reason = Sacked`, **`CurrentTenure = −1`** |
| (h) | (g) then `ReputationOf()` with 2 seasons, 0 trophies, finishes `[+0]` | `300 + 2×10 + 0 + 0 − 40` | **280** |
| (i) | (g) then `Appoint(club 9, day 500)` | appends | `Tenures.Length = 2`; **tenure 0 is unchanged** — history is frozen |
| (j) | `Appoint` while a tenure is open | `RequireNoOpenTenure` | **throws** (F1) — two open tenures decode cleanly and break `CurrentTenure` |
| (k) | `Terminate` while unemployed | `RequireOpenTenure` | **throws** (F2) |
| (l) | Decoded career: last tenure closed, `CurrentTenure = 1` pointing at it | F6 coherence gate | **throws** — unemployment is `−1`, not "the last one is closed" |
| (m) | An open tenure with 3 seasons, 1 trophy, mid-spell | open tenures **count** | reputation reflects it **now**, not on termination |
| (n) | `EndReason` reordered so `Sacked` takes another ordinal | serialized **and** a weight index | **every historical tenure re-reads AND every historical reputation changes** — why FR-MC-015 is APPEND-only |
| (o) | Reputation term `+40` vs `−40` | integer division, truncating | equal-magnitude opposite moves (§3.6) |

Examples (b), (l) and (n) are the three a plausible implementation gets wrong: putting the grace check
after the floor, treating "last tenure closed" as unemployment, and reordering an enum that looks like a
display concern.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-MC-01..05: the two-band termination rule with its grace period and guard ordering, the freezing termination, the appointment and the command-layer join it must not perform, the read-only reputation projection, attractiveness and the deferred S3 draw; arithmetic convention; fifteen worked examples). The grace period is argued rather than assumed — without it, appointment to a club whose confidence is already low (the realistic case) terminates the manager on his first evaluation. Status IN REVIEW. |
#endregion
