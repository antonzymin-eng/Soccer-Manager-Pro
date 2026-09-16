from pathlib import Path
import re


def read(path):
    return Path(path).read_text(encoding="utf-8")


def write(path, text):
    Path(path).write_text(text, encoding="utf-8")


def replace1(path, old, new):
    text = read(path)
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one occurrence, found {count}: {old[:100]!r}")
    write(path, text.replace(old, new, 1))


def add_before_last_endregion(path, row, token):
    text = read(path)
    if token in text:
        return
    index = text.rfind("#endregion")
    if index < 0:
        raise SystemExit(f"{path}: final #endregion missing")
    write(path, text[:index] + row + text[index:])


# --- Production activation lock -------------------------------------------------
path = "src/match-engine/MatchEngine.cs"
replace1(
    path,
    "// Created:  2026-06-16\n",
    "// Created:  2026-06-16\n"
    "// Modified: 2026-09-16 (W2 production activation — active tackle reach must be > 0 and <= loose-ball reclaim reach; zero remains only as a test/measurement negative-control override; no schema/RNG change)\n",
)
replace1(
    path,
    '            // DISABLED is disabled, not "reachable only at exactly zero separation". Shipping the\n'
    "            // constant at 0 must mean no challenge is ever resolved, and an explicit exit says so\n"
    "            // where a >= comparison on a zero radius would merely make it vanishingly unlikely.\n"
    "            if (radius <= 0f)\n"
    "            {\n"
    "                return;\n"
    "            }",
    "            // W2 is active in production. A non-positive catalogue value is a configuration\n"
    "            // violation and fails loudly; explicit zero remains available only through the\n"
    "            // test/measurement override as a negative control.\n"
    "            if (radius <= 0f)\n"
    "            {\n"
    "                if (_tackleContactRadiusOverrideM < 0f)\n"
    "                {\n"
    "                    throw new InvalidOperationException(\n"
    '                        "MatchEngine.TryResolveTackles: production TackleContactRadiusM must be > 0 when W2 is active.");\n'
    "                }\n\n"
    "                return;\n"
    "            }",
)
text = read(path)
signature = "        internal void TestOnly_ArmTackleChallenge(float radiusM) => _tackleContactRadiusOverrideM = radiusM;"
index = text.index(signature)
start = text.rfind("        /// <summary>", max(0, index - 1200), index)
if start < 0:
    raise SystemExit("MatchEngine.cs: tackle override XML summary not found")
new_doc = (
    "        /// <summary>Test-only/measurement seam: overrides the active W2 challenge reach.\n"
    "        /// Zero is the explicit disarmed negative control; a negative value restores the\n"
    "        /// production catalogue path. This override is not serialized.</summary>\n"
)
write(path, text[:start] + new_doc + text[index:])
text = read(path)
field = "        private float _tackleContactRadiusOverrideM = -1f;"
index = text.index(field)
start = text.rfind("        // The challenge's effective reach.", max(0, index - 900), index)
if start < 0:
    raise SystemExit("MatchEngine.cs: tackle override field comment not found")
field_doc = (
    "        // The challenge's effective reach. Negative means use the active production catalogue;\n"
    "        // zero is retained only as an explicit test/measurement negative control. GameplayConfig\n"
    "        // binding is one-shot per process, so tests use the override instead of rebinding config.\n"
)
write(path, text[:start] + field_doc + text[index:])
add_before_last_endregion(
    path,
    "// | 1.80    | 2026-09-16 | —      | W2 production activation: non-positive catalogue reach now fails loud; zero remains only for the explicit test/measurement override. Existing <= LooseBallPickupRadiusM guard unchanged; no schema/RNG change. |\n",
    "// | 1.80    |",
)

path = "src/match-engine/MatchEngineConstants.cs"
replace1(
    path,
    '        /// <para><b>It MUST NOT exceed <see cref="LooseBallPickupRadiusM"/>, and that is a correctness\n',
    '        /// <para><b>It MUST be greater than zero and MUST NOT exceed <see cref="LooseBallPickupRadiusM"/>, and those are correctness\n',
)
add_before_last_endregion(
    path,
    "// | 1.37    | 2026-09-16 | —      | W2 production activation: TackleContactRadiusM fallback now inherits LooseBallPickupRadiusM; durable contract is > 0 and <= reclaim radius; outcome/cooldown GTs unchanged. |\n",
    "// | 1.37    |",
)

add_before_last_endregion(
    "src/match-engine/tests/MatchEngineTackleTests.cs",
    "// | 1.1     | 2026-09-16 | —      | W2 activation: composed locks exercise the shipping default; disabled-default lock becomes >0 / <= reclaim invariants; restore no longer arms the test seam. |\n",
    "// | 1.1     | 2026-09-16 |",
)
add_before_last_endregion(
    "src/discipline/DisciplineConstants.cs",
    "// | 1.6     | 2026-09-16 | —      | W2 is active; the cited foul/card calibration remains pre-tackle and must be re-measured before any separate retuning pass. |\n",
    "// | 1.6     |",
)

# --- Durable tracking -----------------------------------------------------------
# Avoid activation-branch commit SHAs so a future squash merge cannot repeat #417's reachability issue.
path = "docs/tracking/CHANGELOG.md"
current = "> **Last Updated:** September 16, 2026 — **W2 post-W6 paired-control closeout is durable and activation may proceed as a separate change.**"
prior = current.replace("**Last Updated:**", "**Last Updated (prior):**", 1)
replace1(path, current, prior)
text = read(path)
entry = (
    "> **Last Updated:** September 16, 2026 — **W2 tackle resolution is ACTIVE IN PRODUCTION.** "
    "`TackleContactRadiusM` now defaults relationally to `LooseBallPickupRadiusM` rather than `0`; the production contract is `> 0` and `<= LooseBallPickupRadiusM`, with both violations fail-loud on the production catalogue path. Explicit zero remains only as a test/measurement negative control, so the current 1.0 m fallback is not an eternal ceiling. Composed W2 locks now run against the shipping default. Pre-final focused activation validation run `35122576991` and non-certifying Linux capstone run `35123092565` succeeded before the final lower-bound hardening; final-head PR CI owns the merge verdict. No tackle-outcome `[GT]`, `TackleCooldownStrides`, discipline `[GT]`, RNG stream/domain/draw site, durable field, snapshot schema, or save format changed. **T-DA-DET-005 remains deferred and receives no W2 credit.** The ten tackle-outcome `[GT]` values remain accepted uncalibrated debt, foul/card calibration remains a separate next pass, and the owner-held `sim_match_engine_close_chance` RED is unchanged.\n>\n"
)
if prior not in text:
    raise SystemExit("CHANGELOG prior W2 closeout anchor missing after relabel")
write(path, text.replace(prior, entry + prior, 1))

path = "docs/tracking/CHANGELOG-src.md"
old = "> **Last Updated:** September 15, 2026 (v2.139 — **W6 Controlled Ball wiring / PR #412 review closure.**"
prior = old.replace("**Last Updated:**", "**Last Updated (prior):**", 1)
replace1(path, old, prior)
text = read(path)
entry = (
    "> **Last Updated:** September 16, 2026 (v2.140 — **W2 production activation.** "
    "`MatchEngineConstants.TackleContactRadiusM` now defaults to `LooseBallPickupRadiusM`; `MatchEngine.TryResolveTackles` fails loud on a non-positive production catalogue value and retains the existing upper fail-loud bound, while explicit zero remains only through the test/measurement override. `MatchEngineTackleTests` exercises the active shipping default and locks `> 0` / `<= LooseBallPickupRadiusM`; save/restore no longer re-arms a test seam. `DisciplineConstants` changes comments/version only. Ten tackle-outcome `[GT]` values and `TackleCooldownStrides` are unchanged; T-DA-DET-005 remains deferred; no schema/RNG/draw-site/save-format change. Full account: `docs/tracking/CHANGELOG.md`.)\n>\n"
)
if prior not in text:
    raise SystemExit("CHANGELOG-src prior W6 anchor missing after relabel")
text = text.replace(prior, entry + prior, 1)
if not re.search(r"^\| 2\.140\s+\|", text, re.M):
    match = re.search(r"^\| 2\.139\s+\|", text, re.M)
    if not match:
        raise SystemExit("CHANGELOG-src v2.139 table row missing")
    row = "| 2.140   | 2026-09-16 | —      | W2 production activation: active relational tackle reach; lower/upper fail-loud contract; shipping-default composed locks; no GT/schema/RNG retune. |\n"
    text = text[: match.start()] + row + text[match.start() :]
write(path, text)

path = "docs/tracking/file-manifest.md"
current = "**Last Updated:** September 16, 2026 — **W2 post-W6 paired evidence closeout; documentation/evidence only.**"
prior = current.replace("**Last Updated:**", "**Last Updated (prior):**", 1)
replace1(path, current, prior)
text = read(path)
entry = (
    "**Last Updated:** September 16, 2026 — **W2 production activation / PR #418.** **Modified production/source (3):** `src/match-engine/MatchEngineConstants.cs` (shipping fallback `0` → `LooseBallPickupRadiusM`; relational `> 0` / `<= reclaim` contract), `src/match-engine/MatchEngine.cs` (non-positive production catalogue reach now fails loud; test-only zero negative control retained; existing upper guard preserved), and `src/discipline/DisciplineConstants.cs` (comment/version only, no discipline value change). **Modified test (1):** `src/match-engine/tests/MatchEngineTackleTests.cs` (shipping default exercised directly; active/reclaim invariants locked; restore no longer re-arms). **Tracking:** both changelogs, backlog v1.20 → v1.21, tackle design, foul-discipline design, open issues, and this manifest. No assembly edge, durable field, snapshot/save schema, RNG stream/domain/draw site, or `[GT]` value changed. T-DA-DET-005 remains deferred; ten tackle-outcome `[GT]` values and `TackleCooldownStrides` remain uncalibrated; foul/card calibration remains separate.\n\n"
)
if prior not in text:
    raise SystemExit("file-manifest prior W2 closeout anchor missing after relabel")
write(path, text.replace(prior, entry + prior, 1))

path = "docs/tracking/match-engine-wiring-backlog.md"
text = read(path)
text, count = re.subn(
    r"> \*\*UPDATED September 16, 2026 \(v1\.20\):\*\*.*?(?=\n\n## 0\.)",
    "> **UPDATED September 16, 2026 (v1.21):** W2 is now **ACTIVE IN PRODUCTION** after the completed post-W6 paired-control obligation. `TackleContactRadiusM` defaults relationally to `LooseBallPickupRadiusM` (currently 1.0 m), with a durable contract `> 0` and `<= LooseBallPickupRadiusM`; 1.0 m is not an eternal invariant. The production path fails loud on a non-positive catalogue value and retains the existing upper guard; explicit zero remains only as a test/measurement negative control. Four Class-A items remain: W3, W8, W9, W10. The ten tackle-outcome `[GT]` values and `TackleCooldownStrides` remain uncalibrated, foul/card calibration remains separate, T-DA-DET-005 remains deferred with no W2 credit, and the owner-held close-chance RED is unchanged.",
    text,
    count=1,
    flags=re.S,
)
if count != 1:
    raise SystemExit("backlog current status block missing")
text, count = re.subn(
    r"^### W2 — .*$",
    "### W2 — No player had ever made a tackle — **ACTIVE IN PRODUCTION September 16, 2026; P-W6-1 STALL BLOCKER CLEARED**",
    text,
    count=1,
    flags=re.M,
)
if count != 1:
    raise SystemExit("backlog W2 heading missing")
owner = "**W2 NOW GATES THE FOUL/CARD CALIBRATION — owner sequencing decision, August 15, 2026.**"
if owner not in text:
    raise SystemExit("backlog W2 owner-sequence anchor missing")
note = (
    "**PRODUCTION ACTIVATION — September 16, 2026.** The shipping fallback is now `LooseBallPickupRadiusM` rather than `0`, and the composed suite exercises that production value directly. The durable contract is relational (`> 0`, `<= LooseBallPickupRadiusM`), with both violations fail-loud on the production catalogue path; explicit zero remains only for test/measurement negative controls. No tackle outcome/cooldown `[GT]` was calibrated, foul/card calibration remains separate, and no T-DA-DET-005 credit is claimed.\n\n"
)
text = text.replace(owner, note + owner, 1)
if not re.search(r"^\| 1\.21 \|", text, re.M):
    match = re.search(r"^\| 1\.20 \|", text, re.M)
    if not match:
        raise SystemExit("backlog v1.20 version row missing")
    row = "| 1.21 | 2026-09-16 | — | **W2 production activation.** Shipping fallback `0` → `LooseBallPickupRadiusM`; production contract `> 0` / `<= reclaim radius` is fail-loud; shipping-default composed locks replace the disabled-default posture. Outcome `[GT]`s/cooldown unchanged; foul/card calibration remains separate; no T-DA-DET-005 credit. |\n"
    text = text[: match.start()] + row + text[match.start() :]
write(path, text)

path = "docs/tracking/tackle-wiring-design.md"
text = read(path)
heading = "## 0. This is a wiring task, not a realism pass"
if text.count(heading) != 1:
    raise SystemExit("tackle design section 0 anchor missing")
overlay = (
    "## Current activation status — September 16, 2026\n\n"
    "W2 is **ACTIVE IN PRODUCTION**. `TackleContactRadiusM` defaults to `LooseBallPickupRadiusM` (currently 1.0 m); the durable contract is relational: positive reach, never beyond ordinary loose-ball reclaim reach. Production fails loudly on either violation. Zero remains only as an explicit test/measurement negative control, and 1.0 m is not a permanent conceptual ceiling.\n\n"
    "The activation rests on the already-durable paired evidence in `docs/tracking/evidence/w2/`; it does not reinterpret that evidence. The ten tackle-outcome `[GT]` values and `TackleCooldownStrides` remain unchanged and uncalibrated. T-DA-DET-005 still requires `DefensiveAITick`'s own `DeterministicRngService` path and receives no W2 credit. Foul/card calibration is a separate subsequent pass. Everything below is the chronological August investigation/landing record and is intentionally preserved as history.\n\n"
)
write(path, text.replace(heading, overlay + heading, 1))

path = "docs/tracking/open-issues.md"
text = read(path)
old_heading = "**Match-engine wiring backlog — W1/C1/W4/W5/W6/W7/W12 wired and W2 BUILT-BUT-DISABLED; 4 remaining Class-A wiring items (W3, W8–W10) still need production wiring/integration, and the `[GT]` freeze (KD-W1) that follows**"
new_heading = "**Match-engine wiring backlog — W1/C1/W2/W4/W5/W6/W7/W12 wired/active; 4 remaining Class-A wiring items (W3, W8–W10) still need production wiring/integration, and the `[GT]` freeze (KD-W1) that follows**"
if old_heading not in text:
    raise SystemExit("open-issues W2 backlog heading missing")
text = text.replace(old_heading, new_heading, 1)
old_pointer = "`docs/tracking/match-engine-wiring-backlog.md` v1.0, now v1.20."
if old_pointer not in text:
    raise SystemExit("open-issues backlog version pointer missing")
text = text.replace(old_pointer, "`docs/tracking/match-engine-wiring-backlog.md` v1.0, now v1.21.", 1)
anchor = "Full inventory, evidence and wire-order: `docs/tracking/match-engine-wiring-backlog.md` v1.0, now v1.21."
if anchor not in text:
    raise SystemExit("open-issues backlog pointer sentence missing")
text = text.replace(
    anchor,
    anchor + " **W2 production activation landed September 16, 2026: shipping reach is positive and bounded by `LooseBallPickupRadiusM`; tackle outcome/cooldown `[GT]` calibration, T-DA-DET-005, and foul/card calibration remain separate work.**",
    1,
)
write(path, text)

path = "docs/tracking/foul-discipline-balance-design.md"
text = read(path)
owner = "**✅ OWNER DECISION, August 17, 2026: hold the drift. Arm W2 first, then calibrate ONCE.**"
if owner not in text:
    raise SystemExit("foul-discipline owner decision anchor missing")
status = (
    "**CURRENT STATUS, September 16, 2026:** W2 is now active in production, satisfying the sequencing prerequisite below. This activation does **not** retune foul/card values; the post-W2 measurement and calibration remain a separate pass. The August 17 paragraph is retained as the historical owner decision that established that order.\n\n"
)
write(path, text.replace(owner, status + owner, 1))

# Completion-boundary sanity checks.
checks = {
    "docs/tracking/match-engine-wiring-backlog.md": ["ACTIVE IN PRODUCTION", "T-DA-DET-005", "uncalibrated"],
    "docs/tracking/tackle-wiring-design.md": ["ACTIVE IN PRODUCTION", "T-DA-DET-005", "Foul/card calibration"],
    "docs/tracking/CHANGELOG.md": ["W2 tackle resolution is ACTIVE IN PRODUCTION", "owner-held `sim_match_engine_close_chance` RED"],
}
for filename, needles in checks.items():
    body = read(filename)
    for needle in needles:
        if needle not in body:
            raise SystemExit(f"{filename}: required closeout text missing: {needle}")
