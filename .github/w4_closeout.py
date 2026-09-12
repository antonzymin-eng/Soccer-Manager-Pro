from pathlib import Path
import re


def read(path):
    return Path(path).read_text()


def write(path, text):
    Path(path).write_text(text)


def replace_once(text, old, new, label):
    n = text.count(old)
    if n != 1:
        raise RuntimeError(f"{label}: expected one exact match, found {n}")
    return text.replace(old, new, 1)


def regex_once(text, pattern, repl, label, flags=0):
    out, n = re.subn(pattern, repl, text, count=1, flags=flags)
    if n != 1:
        raise RuntimeError(f"{label}: expected one regex match, found {n}")
    return out


def append_version_row(path, row):
    text = read(path)
    idx = text.rfind("#endregion")
    if idx < 0:
        raise RuntimeError(f"{path}: no trailing VersionHistory #endregion")
    text = text[:idx] + row + text[idx:]
    write(path, text)


# -----------------------------------------------------------------------------
# Source headers / version histories
# -----------------------------------------------------------------------------
path = "src/collision-system/CollisionSystem.cs"
text = read(path)
text = replace_once(
    text,
    "// Modified: 2026-07-27  [v1.8]",
    "// Modified: 2026-09-11  [v1.9] (W4: transient applied-deflection feedback; CollisionEvent ABI unchanged)",
    "CollisionSystem header")
write(path, text)
append_version_row(path,
    "// | 1.9     | 2026-09-11 | —      | W4: source-compatible UpdateCollisions overload reports whether any      |\n"
    "// |         |            |        | AGENT_BALL response actually changed ball flight. The signal is transient |\n"
    "// |         |            |        | per call; CollisionEvent and cross-tick state remain unchanged.           |\n")

path = "src/goalkeeper-mechanics/GoalkeeperMechanics.cs"
text = read(path)
anchor = "// Modified: 2026-08-04 (W1 AR-2: + ResetSlot — the per-GK arrays are indexed by TEAM, and the agent occupying that slot can change mid-match (dismissal + substitute keeper), so the slot needs a way to be disowned. See docs/tracking/gk-rush-trigger-design.md v1.3)\n"
text = replace_once(
    text,
    anchor,
    anchor + "// Modified: 2026-09-11 (W4: OnThreatDeflected restarts reaction timing for a changed live flight without setting the shot-event latch; no new state/schema)\n",
    "GoalkeeperMechanics header")
write(path, text)
append_version_row(path,
    "// | 1.13 | 2026-09-11 | — | W4: OnThreatDeflected explicitly restarts the detection / required-reaction |\n"
    "// |      |            |   | stamp after a real body deflection without setting _shotEventPending. A   |\n"
    "// |      |            |   | deflection is a changed threat, not a newly struck shot. No new state.     |\n")

path = "src/goalkeeper-mechanics/Tests/GoalkeeperConversionTests.cs"
text = read(path)
text = replace_once(
    text,
    "// Modified: 2026-08-03",
    "// Modified: 2026-09-11 (W4: explicit deflection reaction-reset lock)",
    "GoalkeeperConversionTests header")
write(path, text)
append_version_row(path,
    "// | 1.3     | 2026-09-11 | —      | W4: real deflection overwrites the reaction stamp while preserving    |\n"
    "// |         |            |        | ShotEventPending=false; locks the dedicated new-threat seam.          |\n")

path = "src/match-engine/MatchEngine.cs"
text = read(path)
created = "// Created:  2026-06-16\n"
text = replace_once(
    text,
    created,
    created + "// Modified: 2026-09-11, latest (W4 keeper perception — v1.73: DT SAVE uses live all-body LOS; same-Resolve applied deflections restart the threatened keeper's reaction timing; raw SaveArmed remains the W1 rush veto; no schema/RNG change).\n",
    "MatchEngine W4 header")
text = replace_once(
    text,
    "// Modified: 2026-08-16, latest (reviewed findings pass, finding B — v1.72, DOC ONLY, no code change).",
    "// Modified: 2026-08-16, prior latest (reviewed findings pass, finding B — v1.72, DOC ONLY, no code change).",
    "MatchEngine prior-latest header")
text = text.replace("// | 1.72    | 2026-08-16, latest |", "// | 1.72    | 2026-08-16 |", 1)
write(path, text)
append_version_row(path,
    "// | 1.73    | 2026-09-11 | —      | W4 keeper perception. RunMechanicsAI gates only DT SAVE availability    |\n"
    "// |         |            |        | through KeeperPerceptionGate (raw SaveArmed + current-frame all-body   |\n"
    "// |         |            |        | LOS); raw SaveArmed remains the threat episode and W1 rush exclusion.  |\n"
    "// |         |            |        | CollisionSystem's transient applied-deflection result is consumed in   |\n"
    "// |         |            |        | the same Resolve call and restarts only the post-deflection-threatened  |\n"
    "// |         |            |        | keeper via OnThreatDeflected. No new cross-tick state/schema/RNG.      |\n")


# -----------------------------------------------------------------------------
# Wiring backlog — close W4 and correct the now-resolved invariant.
# -----------------------------------------------------------------------------
path = "docs/tracking/match-engine-wiring-backlog.md"
text = read(path)
text = replace_once(
    text,
    "### W4 — The keeper is never unsighted",
    "### W4 — The keeper is never unsighted — ✅ **WIRED September 11, 2026 (PR #403)**",
    "W4 heading")
text = regex_once(
    text,
    r"3\. \*\*The two call sites must not drift\.\*\*.*?(?=\n4\. \*\*Friendly screens are absent upstream\.\*\*)",
    "3. **Raw threat geometry stays shared; visibility deliberately does not.** The W1 safety invariant is\n"
    "   narrower than v1.14 stated: both consumers must agree on whether the ball is a raw goal-bound\n"
    "   save threat, so `GkHeadingIntentSource.SaveArmed` remains the shared geometry and the rush veto.\n"
    "   Only the Decision Tree's `SAVE` availability adds keeper visibility through\n"
    "   `KeeperPerceptionGate.SaveAvailable`. Applying LOS to the rush exclusion would be a regression:\n"
    "   an unsighted keeper would become eligible to charge at a goal-bound ball. The final W4 design\n"
    "   therefore shares the raw predicate and intentionally splits only at the perception layer.\n",
    "W4 constraint 3",
    flags=re.S)
text = regex_once(
    text,
    r"\*\*Consequence \(unchanged, now split into its two missing mechanisms\):\*\*.*?(?=\n### W5)",
    "**Resolved September 11, 2026.** PR #399 landed the pure all-body LOS primitive and the explicit\n"
    "applied-deflection signal. PR #403 consumes them: `RunMechanicsAI` sets DT `SaveAvailable` through\n"
    "`KeeperPerceptionGate.SaveAvailable` against the live current-frame body set; the independent W1\n"
    "rush veto remains raw `SaveArmed`; `CollisionSystem.UpdateCollisions` OR-reduces only actual\n"
    "`BallCollisionHandler` flight changes into a transient per-call result; and `RunResolvePhase`\n"
    "consumes that result immediately, evaluates the **post-deflection** flight, and calls the dedicated\n"
    "`GoalkeeperMechanics.OnThreatDeflected` only for the newly threatened keeper. That method overwrites\n"
    "the detection / required-reaction stamp without setting `_shotEventPending`, so a deflection is a\n"
    "changed threat rather than a fake new shot. No pending deflection latch, `CollisionEvent` ABI change,\n"
    "snapshot-schema bump, RNG stream, or draw-order change. Canonical Perception sent-off asymmetry remains\n"
    "separately tracked by #401. Regression locks: `CollisionDeflectionFeedbackTests`,\n"
    "`GoalkeeperConversionTests.OnThreatDeflected_OverwritesArmingStamp_WithoutShotPending`, and\n"
    "`MatchEngineKeeperPerceptionW4Tests` (screened DT SAVE + non-vacuous raw-rush-veto case).\n\n",
    "W4 resolved block",
    flags=re.S)
text = regex_once(
    text,
    r"\| 4 \| \*\*W4\*\* keeper perception \|.*?\|\n",
    "| 4 | ~~**W4** keeper perception~~ ✅ **WIRED Sep 11, 2026** (PR #403) | Live all-body LOS now gates DT `SAVE` without contaminating the W1 raw-`SaveArmed` rush veto; real body deflections restart reaction timing in the same Resolve call through a dedicated non-shot seam. No new serialized state or event ABI. **Next in sequence: W12.** |\n",
    "W4 sequence row")
version_anchor = "| 1.14 | 2026-09-10 | — |"
if version_anchor not in text:
    raise RuntimeError("backlog: 1.14 version row not found")
row = "| 1.15 | 2026-09-11 | — | **W4 WIRED (PR #403).** DT `SAVE` availability is now raw `SaveArmed` plus current-frame all-body physical LOS through `KeeperPerceptionGate`; the W1 rush exclusion intentionally remains raw `SaveArmed`, correcting v1.14 constraint 3 from a too-broad one-predicate rule to the actual shared-geometry invariant. Collision System surfaces only actually-applied AGENT_BALL flight changes as transient per-call feedback; Match Engine consumes it in the same Resolve phase, evaluates post-deflection threat geometry, and `GoalkeeperMechanics.OnThreatDeflected` overwrites reaction timing without setting the shot-event latch. Added composed screened-SAVE/raw-rush-veto, collision applied-vs-overlap, and GK reaction-reset locks. No new cross-tick state, `CollisionEvent` ABI, snapshot schema, RNG stream or draw order. #401 remains separate. Sequence row 4 is closed; W12 is next. |\n"
text = text.replace(version_anchor, row + version_anchor, 1)
write(path, text)


# -----------------------------------------------------------------------------
# src change log — v2.134
# -----------------------------------------------------------------------------
path = "docs/tracking/CHANGELOG-src.md"
text = read(path)
text = replace_once(text, "> **Last Updated:**", "> **Last Updated (prior):**", "CHANGELOG-src prior label")
chain_anchor = "## Header chain\n\n"
entry = (
    "> **Last Updated:** September 11, 2026 (v2.134 — **W4 keeper perception consumer wiring complete (PR #403).** "
    "`MatchEngine.cs` v1.73 gates Decision Tree `SAVE` through live all-body `KeeperPerceptionGate` LOS while preserving raw `SaveArmed` as the W1 rush veto and threat geometry; `CollisionSystem.cs` v1.9 exposes a source-compatible transient `out bool ballDeflected` only when `BallCollisionHandler` actually changes flight; `GoalkeeperMechanics.cs` v1.13 adds `OnThreatDeflected`, overwriting reaction timing without setting the shot-event latch. New `CollisionDeflectionFeedbackTests.cs` and `MatchEngineKeeperPerceptionW4Tests.cs` (+ metas); `GoalkeeperConversionTests.cs` v1.3 gains the deflection-reset lock. No new assembly, `CollisionEvent` ABI, cross-tick latch, snapshot schema, save format, RNG stream/domain/draw site or draw-order change. Canonical Perception sent-off asymmetry remains #401. Current-head CI is authoritative.)\n>\n"
)
text = replace_once(text, chain_anchor, chain_anchor + entry, "CHANGELOG-src entry")
anchor = "| 2.133 |"
if anchor not in text:
    raise RuntimeError("CHANGELOG-src: 2.133 VERSION HISTORY row not found")
row = "| 2.134 | 2026-09-11 | W4 keeper perception consumers: LOS-gated DT SAVE, raw SaveArmed rush veto, same-Resolve applied-deflection reaction reset; collision/GK/composed regressions; no schema/RNG/event-ABI change. |\n"
text = text.replace(anchor, row + anchor, 1)
write(path, text)


# -----------------------------------------------------------------------------
# Project change log — append-only header chain.
# -----------------------------------------------------------------------------
path = "docs/tracking/CHANGELOG.md"
text = read(path)
text = replace_once(text, "> **Last Updated:**", "> **Last Updated (prior):**", "CHANGELOG prior label")
sep = "---\n\n"
entry = (
    "> **Last Updated:** September 11, 2026 — **W4 keeper perception is wired on PR #403, completing the consumer half that PR #399 prepared.**\n>\n"
    "> Decision Tree `SAVE` now uses current-frame all-body physical LOS through `KeeperPerceptionGate.SaveAvailable`; raw `GkHeadingIntentSource.SaveArmed` deliberately remains the shared threat geometry and the independent W1 rush veto, so being screened never makes charging at a goal-bound ball legal. Collision System now returns one transient per-call fact only when `BallCollisionHandler` actually changes flight; Match Engine consumes it immediately in the same Resolve phase, evaluates the post-deflection trajectory, and restarts only the newly threatened keeper through `GoalkeeperMechanics.OnThreatDeflected`. That seam overwrites reaction timing without setting the shot-event latch. Added applied-vs-overlap collision, reaction-reset, screened-DT-SAVE, and non-vacuous raw-rush-veto regressions. No new serialized latch, snapshot schema, `CollisionEvent` ABI, save format, RNG stream/domain/draw site or draw order. Canonical Perception sent-off asymmetry remains separately tracked by #401. `match-engine-wiring-backlog.md` advances v1.14 → v1.15 and closes sequence item W4; W12 is next. Current-head CI is the gate authority.\n\n"
)
text = replace_once(text, sep, sep + entry, "CHANGELOG W4 entry")
write(path, text)


# -----------------------------------------------------------------------------
# Manifest landing record.
# -----------------------------------------------------------------------------
path = "docs/tracking/file-manifest.md"
text = read(path)
text = replace_once(text, "**Last Updated:**", "**Last Updated (prior):**", "manifest prior label")
title = "# File Manifest (Post-Migration Baseline)\n\n"
entry = (
    "**Last Updated:** September 11, 2026 — **W4 keeper perception consumer wiring / PR #403 close-out.**\n"
    "**NEW (4):** `src/collision-system/tests/CollisionDeflectionFeedbackTests.cs` (+ `.meta`) and `src/match-engine/tests/MatchEngineKeeperPerceptionW4Tests.cs` (+ `.meta`). **Modified production (3):** `src/collision-system/CollisionSystem.cs` v1.8 → v1.9, `src/goalkeeper-mechanics/GoalkeeperMechanics.cs` v1.12 → v1.13, `src/match-engine/MatchEngine.cs` v1.72 → v1.73. **Modified existing test (1):** `src/goalkeeper-mechanics/Tests/GoalkeeperConversionTests.cs` v1.2 → v1.3. **Modified tracking (4):** `docs/tracking/match-engine-wiring-backlog.md` v1.14 → v1.15, `docs/tracking/CHANGELOG-src.md` v2.133 → v2.134, `docs/tracking/CHANGELOG.md`, and this manifest. DT SAVE now consumes live all-body LOS; raw `SaveArmed` remains the keeper-rush veto; applied body deflections restart reaction timing in the same Resolve call through a dedicated non-shot seam. No new assembly/asmdef edge, cross-tick deflection latch, `CollisionEvent` ABI, snapshot schema, save format, RNG stream/domain/draw site/draw order, or `[GT]` change. Canonical Perception sent-off asymmetry remains #401. Current-head CI owns the gate verdict.\n\n"
)
text = replace_once(text, title, title + entry, "manifest W4 entry")
write(path, text)
