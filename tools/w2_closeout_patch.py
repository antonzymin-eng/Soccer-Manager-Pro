#!/usr/bin/env python3
from pathlib import Path


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{label}: expected exactly one match, found {count}")
    return text.replace(old, new, 1)


# Authoritative W2 backlog -> v1.19.
p = Path("docs/tracking/match-engine-wiring-backlog.md")
s = p.read_text(encoding="utf-8")
s = replace_once(
    s,
    "> **UPDATED September 15, 2026 (v1.18):** W4, W5, W6, W7, and W12 are wired. W6 gives genuine open-play possession a physical `BallStateType.Controlled` producer/attachment/release path and closes the keeper-held carry defect at the central MatchEngine attachment seam; its focused locks and composed keeper claim are green. Four Class-A items remain: W3, W8, W9, W10. W2 remains built but shipping-disabled: the W6 prerequisite is now landed, and the owner-sequenced post-W6 W2 measurement is the next step before any activation decision. The owner-held close-chance RED is unchanged and remains a separate realism/calibration item.",
    "> **UPDATED September 15, 2026 (v1.19):** W4, W5, W6, W7, and W12 are wired. Four Class-A items remain: W3, W8, W9, W10. W2 remains built and shipping-disabled, but its historical `sim_match_engine_inposs_gate` stall blocker is now **cleared under pre-registered P-W6-1**: on exact post-W6 production head `e4335f7f`, measurement-only arming at `LooseBallPickupRadiusM` produced per-seed mirrored shares `0.983/0.983` and `0.985/0.985`, each above the unchanged strict `> 0.70` bound. The strongest like-for-like comparator is the pre-W6 same-radius result `0.369/0.963`; the known stall no longer reproduces at the intended activation radius. Production `TackleContactRadiusM` is still 0. Activation remains a separate change, preceded by a like-for-like post-W6 disarmed control and followed by the already-booked foul/card calibration. The owner-held close-chance RED is unchanged and remains a separate realism/calibration item.",
    "backlog summary",
)
s = replace_once(
    s,
    "### W2 — No player has ever made a tackle — ⚙️ **BUILT August 12, 2026; SHIPS DISABLED pending post-W6 measurement**",
    "### W2 — No player has ever made a tackle — ⚙️ **BUILT August 12, 2026; SHIPS DISABLED; P-W6-1 STALL BLOCKER CLEARED September 15, 2026**",
    "W2 heading",
)
anchor = "**W2 NOW GATES THE FOUL/CARD CALIBRATION — owner sequencing decision, August 15, 2026.**"
measurement = (
    "**POST-W6 P-W6-1 MEASUREMENT — stall blocker cleared September 15, 2026; production remains disabled.** "
    "Actions run `35047291977`, job `104639783769`, checked out exact merged W6 head "
    "`e4335f7ff059deb483b1aaa534f2190fd3762008` and armed the challenge only through the test seam at "
    "`LooseBallPickupRadiusM` (1.0 m). The two governed seeds were evaluated independently in both mirrored views: "
    "`0x0F1E2D3C4B5A6978` = **0.983 / 0.983** over 14,751 samples; `0x1A2B3C4D5E6F7081` = "
    "**0.985 / 0.985** over 14,203 samples. All four shares satisfy the unchanged strict predicate `> 0.70`. "
    "The strongest like-for-like comparison is the pre-W6 1.0 m run in `tackle-wiring-design.md` §3.4: "
    "**0.369 / 0.963**. W6 therefore removes the known tackle-created possession stall at the intended activation "
    "radius under the governed corpus. This clears **P-W6-1 only**; it is not a release-calibration verdict, does "
    "not calibrate the ten tackle-outcome `[GT]`s, and does not satisfy the separate W12 material-effect hypothesis. "
    "A same-head, same-seed disarmed control is being captured before the production activation change so the W6 "
    "baseline shift is measured rather than inferred.\n\n"
)
s = replace_once(s, anchor, measurement + anchor, "P-W6-1 insertion")
s = replace_once(
    s,
    "**Downstream boundary:** W6 does **not** arm W2. `TackleContactRadiusM` remains at its governed shipping-disabled value pending the separately pre-registered post-W6 W2 measurement. The owner-held `sim_match_engine_close_chance` also remains unchanged: W6 moves its sampled trajectory population but does not change the DRIBBLE direction scorer, so that calibration/disposition stays outside this wiring landing.",
    "**Downstream boundary:** W6 did **not** arm W2. The separately pre-registered post-W6 P-W6-1 measurement has now run and clears the historical stall blocker, but `TackleContactRadiusM` remains at its governed shipping-disabled value until the separate activation change lands. The owner-held `sim_match_engine_close_chance` also remains unchanged: W6 moves its sampled trajectory population but does not change the DRIBBLE direction scorer, so that calibration/disposition stays outside this wiring landing.",
    "W6 downstream boundary",
)
s = replace_once(
    s,
    "| 6 | ~~**W5**~~ / ~~**W7**~~ / ~~**W6**~~ ✅ **WIRED Sep 15, 2026** | W5/W7 are landed and W6 now gives open-play possession a physical Controlled carrier/attachment/release path. W6 itself does not arm W2; the separately pre-registered post-W6 W2 measurement is the next owner-sequenced step. |",
    "| 6 | ~~**W5**~~ / ~~**W7**~~ / ~~**W6**~~ ✅ **WIRED Sep 15, 2026** | W5/W7 are landed and W6 now gives open-play possession a physical Controlled carrier/attachment/release path. W6 itself did not arm W2. The separately pre-registered P-W6-1 run is complete and clears the historical stall blocker; a like-for-like disarmed control and then a separate production activation change remain before the post-W2 calibration pass. |",
    "sequence row",
)
old_block = """> **What blocks arming itself:** the historical armed run collapsed `sim_match_engine_inposs_gate`
> to 0.501 against its 0.70 bound — a stall, not a rate effect. W6 was the leading candidate and is
> now wired. The next owner-sequenced step is the pre-registered post-W6 W2 measurement, which must
> determine whether that stall survives before `TackleContactRadiusM` can move from 0.
>
> Read together, W4 → W12 → W6 is now complete. The unresolved question is empirical rather than a
> missing wiring prerequisite: rerun W2 armed against the W6 state, then decide activation separately."""
new_block = """> **The historical arming blocker is now cleared under P-W6-1.** Before W6, arming at the intended
> 1.0 m radius collapsed one governed seed to 0.369. Against exact post-W6 head `e4335f7f`, the same
> measurement-only arm produces 0.983/0.983 and 0.985/0.985 by seed, all above the unchanged `> 0.70`
> predicate. That evidence clears the known stall blocker; it does not itself calibrate the live tackle
> population or the ten un-calibrated tackle-outcome `[GT]`s.
>
> Read together, W4 → W12 → W6 → P-W6-1 is complete. Before production activation, retain a like-for-like
> post-W6 disarmed control on the same head/seeds, then land activation as a separate reviewed change.
> The already-recorded sequence remains **arm W2 first, then calibrate fouls/cards once**."""
s = replace_once(s, old_block, new_block, "arming blocker block")
marker = "| 1.18 | 2026-09-15 | — |"
row = "| 1.19 | 2026-09-15 | — | **Post-W6 P-W6-1 stall blocker cleared; W2 still ships disabled.** Exact W6 head `e4335f7f` with measurement-only 1.0 m arming yields per-seed mirrored shares `0.983/0.983` and `0.985/0.985`, all above unchanged `> 0.70`; the pre-W6 same-radius comparison was `0.369/0.963`. Production activation is deliberately separate and awaits the like-for-like post-W6 disarmed control. The foul/card sequence remains arm first, calibrate once. |\n"
s = replace_once(s, marker, row + marker, "v1.19 row")
p.write_text(s, encoding="utf-8", newline="\n")

# Correct T-DA-DET-005 tracking prose only; no spec ERR.
p = Path("docs/tracking/tackle-wiring-design.md")
s = p.read_text(encoding="utf-8")
old = """4. **`DOMAIN_TAG_DEFENSIVE_AI` (0x1A) gains its first draw site**, which un-blocks #14's own
   T-DA-DET-005 — `Assert.Ignore`d since May with the message *\"activate when DOMAIN_TAG_DEFENSIVE_AI
   RNG draws are live\"*. Wiring it was not in this pass's scope; the test is now unblocked, not
   un-ignored."""
new = """4. **`DOMAIN_TAG_DEFENSIVE_AI` (0x1A) gains its first draw site, but this does NOT unblock #14's T-DA-DET-005.** The historical note quoted only the second half of the actual ignore reason. The live test requires #16 `DeterministicRngService` wired into **`DefensiveAITick`**, then adds \"activate when DOMAIN_TAG_DEFENSIVE_AI (0x1A) RNG draws are live.\" W2 added its draw in MatchEngine's tackle-resolution path, not in `DefensiveAITick`, so the first condition remains unsatisfied. Spec #14 defines the procedure/pass criterion but does not claim the test is active; this is a design-supplement tracking correction, **not** a spec defect, and no ERR is filed."""
s = replace_once(s, old, new, "T-DA-DET-005 §3.3")
old = """- **`DOMAIN_TAG_DEFENSIVE_AI = 0x1A` has no draw site anywhere in `src/`.** The tag is allocated
  (`DeterministicSimConstants.cs:105`) and already `[CROSS]`-mirrored into #14's own catalogue
  (`DefensiveAIConstants.cs:54` `DomainTagDefensiveAI`), and #14's own determinism test T-DA-DET-005 is
  `Assert.Ignore`d pending exactly this (*\"activate when `DOMAIN_TAG_DEFENSIVE_AI` (0x1A) RNG draws are
  live\"*). So a tackle draw would be its **first** draw site — legitimate, requiring no new allocation,
  and it un-ignores a test that has been waiting for it. Per the ERR-041-002 posture, do **not** also
  allocate a subsystem ordinal."""
new = """- **`DOMAIN_TAG_DEFENSIVE_AI = 0x1A` had no draw site anywhere in `src/` before W2.** The tag was already allocated and `[CROSS]`-mirrored, so the tackle draw was a legitimate first use requiring no new allocation. The earlier conclusion that this would un-ignore T-DA-DET-005 was wrong: the actual test requires `DeterministicRngService` wired into **`DefensiveAITick`**, while W2's draw site lives in MatchEngine's tackle-resolution path. The test therefore remains correctly ignored. No spec correction is required because #14 defines the test procedure without claiming active implementation. Per the ERR-041-002 posture, do **not** also allocate a subsystem ordinal."""
s = replace_once(s, old, new, "T-DA-DET-005 §5.1")
marker = "| 1.5 | 2026-08-12 | — |"
row = "| 1.6 | 2026-09-15 | — | **Tracking correction, no ERR:** W2 did give `DOMAIN_TAG_DEFENSIVE_AI` its first draw site, but in MatchEngine, not `DefensiveAITick`. T-DA-DET-005 remains `Assert.Ignore` because its first unsatisfied condition is #16 `DeterministicRngService` wired into `DefensiveAITick`; the older prose quoted only the second clause of the ignore reason and incorrectly called the test unblocked. Spec #14 itself is unchanged and correct. |\n"
s = replace_once(s, marker, row + marker, "tackle design v1.6 row")
p.write_text(s, encoding="utf-8", newline="\n")

# Prepend immutable CHANGELOG entry.
p = Path("docs/tracking/CHANGELOG.md")
s = p.read_text(encoding="utf-8")
needle = "> **Last Updated:** September 15, 2026 — **W6 Controlled Ball wiring / PR #412 review closure.**"
entry = (
    "> **Last Updated:** September 15, 2026 — **Post-W6 W2 P-W6-1 measurement closes the historical possession-stall blocker; production activation remains separate.** "
    "Actions run `35047291977` / job `104639783769` checked out exact W6 merge head `e4335f7ff059deb483b1aaa534f2190fd3762008` and armed W2 only through `TestOnly_ArmTackleChallenge(LooseBallPickupRadiusM)`. "
    "Governed seed `0x0F1E2D3C4B5A6978` produced 14,751 samples at **0.983 / 0.983** mirrored InPoss share; `0x1A2B3C4D5E6F7081` produced 14,203 at **0.985 / 0.985**. All satisfy the unchanged strict `> 0.70` per-seed predicate. "
    "The strongest like-for-like historical comparator is the pre-W6 **same 1.0 m radius** result `0.369 / 0.963`, so the known tackle-created stall no longer reproduces under W6. This clears the **P-W6-1 blocker only**: `TackleContactRadiusM` remains 0 on production `main`; the ten tackle-outcome `[GT]`s remain intentionally un-calibrated; foul/card calibration still follows activation under the standing **arm first, calibrate once** owner sequence. "
    "Before activation, a same-head/same-seed disarmed control is being captured with the parameterized measurement driver. Two conflicting W12 preregistration thresholds were also found before canonical post-W6 W12 interpretation; `w12-preregistration-reconciliation.md` preserves both historical files unchanged and requires the stricter 5 pp + suppression-mass conditions for any material-causation claim.\n>\n> **Last Updated (prior):** September 15, 2026 — **W6 Controlled Ball wiring / PR #412 review closure.**"
)
s = replace_once(s, needle, entry, "CHANGELOG newest entry")
p.write_text(s, encoding="utf-8", newline="\n")

# Record the pre-registered procedure deviation before W12 interpretation.
p = Path("docs/tracking/w12-preregistration-reconciliation.md")
s = p.read_text(encoding="utf-8")
if "## Procedure deviation recorded before interpretation" in s:
    raise SystemExit("procedure deviation already present")
addition = """

## Procedure deviation recorded before interpretation

`w6-post-wiring-measurement-preregistration.md` §5 recorded an intended sequence in which the W12 evidence repair would land before PR #412 was rebased/accepted and the post-W6 measurements were interpreted. The executed sequence differed: PR #412 merged first, and the stale `wiring/w12-evidence-repair` branch was audited afterward.

That audit found the branch superseded rather than missing: current `main` already retains the exact pre/post Actions ZIPs, `tools/dotnet-ci/check_w12_evidence.py`, regression coverage under `tools/tests/test_w12_evidence.py`, and enforcement through the required `Spec hygiene checks` context. The stale branch's Base64 reconstruction workflow and checker were therefore redundant, and its modified comparison document was deliberately not cherry-picked.

This is a **procedure deviation**, not a threshold change. It is recorded here before the canonical post-W6 W12 result is interpreted. Neither historical pre-registration is rewritten, and none of the support/falsifier bands is altered because the execution order differed.
"""
p.write_text(s.rstrip() + addition + "\n", encoding="utf-8", newline="\n")
