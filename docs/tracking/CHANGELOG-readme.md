# README Change Log — the header chain moved out of `README.md`

> **Created:** September 3, 2026
> **Purpose:** Holds, **verbatim**, the `**Last Updated:**` / `**Last Updated (prior):**` entry chain
> that had accumulated in the header of the root `README.md`. Nothing here was rewritten, summarised,
> reordered, deduplicated or deleted in the move — every entry below is the exact text that stood in
> `README.md` on September 3, 2026, byte for byte, including its own internal corrections and
> annotations.
> **Author:** —

---

## Why this file exists

The root `README.md` is the first document a reader meets, and it had stopped being an orientation
document. Measured on September 3, 2026 the header chain was **55,578 of the file's 116,766 bytes
(47.6%)** across **564 lines and 38 entries** — a landing-by-landing development record sitting above
the project description, in the one file whose job is to say what this project is.

The weight was not the only cost. Because the chain was maintained by appending and the prose below it
was not, the two drifted apart until they contradicted each other outright: the header and the closing
line both claimed *"All 26 approved specs have active `src/` implementations"* while the body of the
same file correctly recorded 53 approved specs and 19 of them with no assembly at all. A reader who
skimmed got the wrong answer and a reader who persevered got the right one.

This is the third such split in this repository and it follows the two before it exactly:

- `CLAUDE.md`'s header chain → `docs/tracking/CHANGELOG.md` (July 31, 2026).
- `CLAUDE.md`'s OPEN ISSUES landing narratives → `docs/tracking/landing-history.md` (August 22, 2026).

## What this file is not

**This archive records history; it does not define current project state.** Every figure below was
true, or believed true, on the date of the entry that carries it, and many have since been superseded.
Nothing here may be cited as a current claim about the repository. For current state use the owning
document: `docs/specs/SPEC_INDEX.md` for specification status, `docs/tracking/path-to-playable-roadmap.md`
for the implementation sequence, and `docs/tracking/CHANGELOG.md` for the project's live landing record.

This file is closed to appending. New landings are recorded in `docs/tracking/CHANGELOG.md`; the root
`README.md` no longer carries a header chain, and `.claude/skills/landing-close-out/scripts/check_drift.sh`
now fails if one reappears there.

---

**Last Updated:** August 29, 2026 (*CI triage on PR #341; documentation only. Two corrections to this
file, no prose below rewritten. The design-supplement count read **60**; `ls docs/tracking/*-design.md`
measures **61**, and `adversarial-review/SKILL.md` carried the same stale 60. Four paragraph wraps had
put a `#NN` spec reference at the start of a line — `#29`, `#34`, `#41`, `#44` — where markdownlint
reads it as an ATX heading (MD018); each reference now ends the previous line and no wording changed.
The assembly-less figure this file states as **19 of the 53** is unchanged and is now the value all
eight scanned surfaces agree on: `.claude/agents/orienteer.md` alone had said "roughly 20". Full
account: `docs/tracking/CHANGELOG.md`, August 29, 2026.*)

**Last Updated (prior):** August 17, 2026 (*One correction to the entry below, which is left as written per
this file's no-edit rule: it says a suspended player is reinstated "only when the alternative is a
club that cannot take the field at all." That is a single-case claim, and `discipline-suspensions/
section-2.md` §2.3 states it as two: **benched**, the common case, where the reinstated player "is
not in `fieldedPlayerIds`" and "his ban advances normally" regardless of the crisis that put him
there; and **forced to start**, only when "no completing choice keeps every reinstated-suspended
player out of the eleven" — and only then does the ban stall, exactly as the entry below already
describes. The single-case wording undersold the game's own rule: most reinstatements do not stall
anything at all. Prior entry below.)*
**Last Updated (prior):** August 15, 2026 (**A suspended player forced onto the pitch by an injury crisis
no longer serves his ban by playing.** When bans and injuries between them leave a club unable to
field eleven players, the season code presses players back into selection until it can — injured
ones first, and a suspended one only when the alternative is a club that cannot take the field at
all. That safety rule shipped two days ago with a hole in it, recorded openly at the time as
something for the owner to decide: the same fixture that put the banned player on the pitch also
counted as one match of his ban served. So the suspension cost him nothing — he played, and his ban
got shorter anyway. A two-match red card could cost a badly hit club nothing whatsoever. That is now
decided and fixed. A ban is served by the club playing a match **without** you, so the code that
counts down bans is now told which eleven actually played and skips anyone in it. Nothing changes
for an ordinary match, by construction rather than by testing: suspended players are removed from
selection before the team is picked, so the only way a banned player can be in the eleven at all is
that same last-resort press-back. **The better answer is agreed and not built, and it is worth
naming because it is what a player would expect.** The Football Manager approach is a ladder: when a
club runs out of bodies, promote youth players first, and below them field generated stand-in
players with low attributes — so a banned man never takes the field at all. Neither rung can be
built yet. The youth system has a finished design but no code. And the stand-in players hit a
plainer wall: every player's identity number is derived from his club and his squad slot, squads are
capped at 25, and the numbering is completely full — a 26th player at one club would collide with
the first player at the next. Widening that is a real piece of design work touching every saved
game, not a follow-up commit. **Also decided this pass, about the foul rate:** the game calls about
35 fouls, 5 yellows and 1 red a match against real football's roughly 22, 3.5 and 0.25. Tempting to
turn the dial down now — deliberately not doing that. Tackling is built but switched off, and
switching it on will route tens of extra challenges per match through the same foul check, so any
number tuned today would be re-tuned immediately while sitting in the code looking finished. Turn
tackling on first, then tune once. The cost of waiting is stated rather than hidden: the foul and
card rates — and so the suspension rate — are knowingly wrong until then. *(One correction to the
entry below, which is left as written per this file's no-edit rule: it opens "Managers can now be
suspended for their bookings". It is **players** who are suspended, not managers; nothing in the
discipline module touches managers.)* Prior entry below.)

**Last Updated (prior):** August 13, 2026 (**Managers can now be suspended for their bookings — the game has
discipline and suspensions for the first time.** A new "discipline" module (`src/discipline/`, the
35th part of the game's codebase) watches every yellow and red card as a match is played and keeps
a running ledger per player, exactly the way the #37 match-statistics module already watches shots.
Season management now consults that ledger before picking each team: a player serving a ban is
pulled from selection the same way an injured player already was, using the same "never leave a
club unable to field a team" safety rule the injury system established — a banned player is only
ever fielded as the very last resort, and only after every injured player has already been pressed
back in ahead of him, which is stricter than what a strict reading of the rule would have allowed
and closer to how the actual Laws of the Game work. Building this against the season-save file
format (its seventh saved section) caught a genuine bug before it shipped: the worked example in the
specification described a substitute's identity using a number that, worked through by hand, points
at an on-pitch player rather than a substitute — every card issued after a substitution would have
been credited to the wrong man. Fixed by reading the substitute's real identity straight out of the
match engine instead of re-deriving it a second way, which is also the safer design. Also this pass:
81 new tests, all passing, and a five-day-old finding re-checked and confirmed still true — the
game currently calls about 35 fouls, 5 yellow cards and 1 red card a match, against real football's
roughly 22, 3.5 and 0.25, so both fouls and yellows are now about two-thirds too frequent (reds were
already about four times too frequent and are unchanged). **What's still missing:** only matches the
human manager actually plays out card up; matches simulated in the background for every other club
in the league do not yet generate any, so today a human's own club racks up roughly twenty times as
many bookings and suspensions as any rival — an honest and known gap, not a bug, and the next piece
of work names the fix. Prior entry below.)

**Last Updated (prior):** August 8, 2026 (**Players calmly passing the ball around were being read by the
positioning system as a team in chaos.** The AI that decides where every outfield player should
stand watches one signal to know what's happening right now: is my team in possession, is the
other team, or is the ball loose in a scramble? That signal was being read at exactly the wrong
moment. The match engine drops its notion of "who has the ball" the instant a player kicks it,
and only re-establishes it once someone actually receives the ball — correct for tracking the
ball itself, but the positioning system was reusing that same on/off flag to decide which team
has the ball. The result: for the whole time a pass was travelling through the air or rolling
across the grass, the positioning system believed nobody had the ball and treated a team calmly
knocking it around as though it were scrambling to reorganize after losing it. Measured over six
seeds of a full match, the share of time near the opponent's goal correctly read as settled
possession jumped from about a quarter to essentially all of it (24% → 97%), while the mistaken
"scrambling" reading collapsed from 59% to 3%. Fixed by teaching the system to track which TEAM
has the ball — the carrier's team, or, while a pass is in the air, the team of the player it's
headed to — rather than which single player is touching it this instant. As expected going in,
one part of the team's shape got measurably worse once the correct signal took over: the settled
formation this system already uses sits deeper than the "push forward, we're attacking" reading
it was replacing, so the front line now holds a couple of metres further back and fewer players
reach the penalty box — a known, predicted side effect rather than a new bug, and next in line to
address. Two more places where the engine already tracks information nobody reads yet were logged
for later work. `SNAPSHOT_SCHEMA_VERSION` 19 → 20. **The full test run finished RED, and the two
failures are this change's own doing.** Everything builds and every other suite passes, but two
long-standing match checks now fail: one measuring whether players dribble toward the goal rather
than away from it, and one that simply asks whether a fast ball ever deflected off a body during a
match — which now reads zero. Both pass on the code as it stood before this change, confirmed by
re-running them against it, so the cause is not in doubt. Neither threshold has been quietly
loosened to make the run green: the first has already been relaxed twice in the last fortnight and
a check relaxed a third time has stopped checking anything, and the second is not a threshold at
all but a "does this ever happen" test that has stopped happening. Both are left failing, with the
cause written down, for a human to decide. Prior entry below.)

**Last Updated (prior):** August 7, 2026, later same day (**Players now get injured — at realistic rates.**
The injury system built over the last three days had been wired but deliberately switched off,
because measurement showed its first-guess numbers were absurd: a new player had a 23% chance of
getting hurt on his first day, while a player on the default training regime could literally never
be injured at all. This landing is the balance pass that fixes and arms it. Three things changed.
First, match days now run in the right order: a player whose recovery ends on match day plays that
match instead of missing one extra game, and the injury check happens on match-day morning. Second,
the game now remembers who actually played: each club's starting eleven is recorded per fixture
(and saved), so playing matches genuinely raises injury risk in the following week — match load is
the dominant real-world driver. Third, the probabilities were refitted and then measured over eight
full simulated 20-club seasons: roughly 780 injuries per league season (about 39 per club, squarely
in the real-world 30–55 range), starters getting hurt about twice a season and reserves about once,
and around 9% of players unavailable at any given match day. Those numbers are now locked by tests
so they cannot silently drift. A design hazard was also closed along the way: the random draw's
denominator was previously a tunable value, meaning a config tweak would have silently re-rolled
every career's injury luck — it is now pinned. Prior entry below.)

**Last Updated (prior):** August 7, 2026, evening (**The screens work was fully build-and-test verified the
same day — and the check-up found two unrelated, pre-existing test failures on the main branch.**
For weeks, work in this environment could not be compiled locally because the .NET installer's
download sites are blocked; it turns out the standard Ubuntu package archive serves the same
toolkit and installs cleanly, a discovery now written into the build instructions. With a real
toolchain, the whole project was built (zero errors) and every test run: the new screens module
passed all fifteen of its tests on first execution, and a guard test that polices which modules may
touch the UI framework flagged the new module exactly as designed — it is now on the approved list.
The one red area is the match engine's realism checks: two long-running scenario tests fail, and
re-running exactly those two against the untouched main branch shows **they fail there identically**
— they are the current state of the main branch (most likely fallout from this week's shot-geometry
fixes), now logged as an open issue for the match-realism work, not something this change caused.
Prior entry below.)
**Last Updated (prior):** August 7, 2026 (**The game's four screens now have an official home in the
code, ending a layering question that was blocking the user interface work.** The interactive
client's screens — Main Menu, Tactics Setup, Match View, Post-Match Report — needed somewhere to
live, and no existing module was allowed to hold them: the UI framework is deliberately forbidden
from hard-coding any screen, and the Unity-only module is invisible to the automated build, which
this project treats as disqualifying for anything that makes decisions. The owner settled it: a
new, small module that sits above the UI framework and holds exactly two things — the list of the
four screens, and the map of which screen can lead to which (menu → setup → match → report →
menu, plus cancelling out of setup). The map is deliberately strict: once a match starts you
cannot "go back" into the setup screen for it, and once it ends you cannot return to a finished
match — pressing back from the report lands on the main menu. There is intentionally no
"abandon match" button, because no screen design calls for one yet, and this project does not
build doors that lead nowhere. Fifteen automated tests pin all of this down. Nothing about how
matches play out changes — this is purely about which screen you are looking at. The build could
not be run in this environment (no .NET toolchain); CI compiles it on push. Note the chain below
is drifted: two older entries both carry the bare "Last Updated" label and dates run out of
order; left as found, new entries stack here. Prior entry below.)
**Last Updated (prior):** August 7, 2026 (**The main branch went red after the shot-fix chain merged; both
failures are diagnosed, and the tests are re-anchored to the intended new behaviour.** This week's
chain of shooting fixes changed which shots players take. Two long-running acceptance tests were
still checking numbers measured against the old behaviour, and the first full test run on the merged
main branch caught them both. One is genuinely news: on one of the two measured seeds, dribbling in
the attacking third has drifted back to pointing away from goal — a regression the August 4 fix had
removed, partially returned as a side effect of the shooting changes. The tests now hold the line at
today's intended baseline, and the drift itself is queued for the planned calibration pass rather
than patched quietly. Two working-practice discoveries came out of the diagnosis: the full test gate
can be run inside Claude's remote sessions after all (the standard Ubuntu package archive carries
the .NET SDK — earlier sessions had only tried the blocked Microsoft installer), and the CI log
viewer's 5,000-line cap had been hiding one of the two failures from every previous session.
Prior entry below.)

**Last Updated (prior):** August 6, 2026 (**ERR-008-022 — the same shot calculation was throwing away
half the goal before it started.** Yesterday's fix (ERR-008-021 below) taught the shooter to
count a defender who stands *across* one of the posts rather than squarely between them. A
hostile review of that fix found it was being handed the wrong list of defenders to count. The
code decided who was "in the way" by asking whether an opponent stood nearer than the middle of
the goal — which, if you are shooting from anywhere except dead centre, draws a diagonal line
straight across the goalmouth. Everyone guarding the **far** post fell on the wrong side of it
and was simply deleted. On every one of 20,213 off-centre shooting positions tested, the far-post
defender was thrown away and the near-post one kept; a goalkeeper standing on his line in the
middle of his goal was thrown away from *every* position tried, so a striker could look at a
keeper directly in front of him and see a completely empty net. The mirror of the same mistake
counted an opponent standing *behind* the goal, in the net, as a keeper blocking the shot. Two
other yes/no switches in the same calculation turned out to be bigger cliffs than the one the
earlier fix removed — one centimetre of a defender's position could take the shooter from
"completely open goal" to "no shot on at all", and two centimetres could change whether the game
thought a man was a goalkeeper or an outfielder, which doubles the space he blocks. All three are
now gradual. Three things the earlier fix *claimed* to have verified turned out to be wrong and
are corrected in the record rather than quietly dropped, including the argument that had been the
stated reason the change needed no rebalancing. And the tests were weaker than advertised: a
deliberately broken version of the code passed all ten of them, and one test compared a value
with itself. The suite is now fifteen tests, all of which check the answer rather than merely
that an answer appeared. Nothing here has been compiled or run — this environment has no .NET
SDK — so every figure above comes from working the geometry out by hand against an independent
reference implementation.)

**Last Updated (prior):** August 5, 2026, latest same day (**ERR-008-021 — a defender standing across the
near post used to count for nothing.** When a player decides whether a shot is on, he estimates how
much of the goal he can actually see. The old rule asked one yes/no question of each opponent in the
way — *is the middle of his body between the two posts, from where I am standing?* — and if the
answer was yes it blocked out his whole width, and if no it blocked out nothing at all. So a
defender a few centimetres the wrong side of that line vanished from the calculation entirely, and
the shooter saw a completely open goal with a man standing squarely in front of his near post. Four
centimetres of defender movement could swing the shooter's read of the goal from 60% open to 100%
open — and that number decides whether he shoots at all, how good he thinks the chance is, and how
hard he hits it. The rule now measures how much of the goal each opponent's body *actually* covers,
which removes the jump and, incidentally, is simply the correct answer. Two related things also
changed: a defender's own qualities now matter — someone who reads the shot early and gets his body
into its line blocks more of the goal than someone who does neither — and how accurately the shooter
judges that depends on his own vision, so a poor reader of the game sees every defender as roughly
average, which is exactly how the game behaved before. The goalkeeper is deliberately left out of
that: his shot-stopping is already modelled elsewhere, and counting it twice would make him better
than he is. Average defenders in average positions block exactly what they blocked before, so match
balance is not shifted — the change redistributes rather than tightens. Nine tests lock the new
behaviour, three of which fail on the old rule. **Not compiled or run here — this environment has no
.NET toolchain; CI compiles on push.** **Prior entry below.**)
**Last Updated (prior):** August 6, 2026, latest same day (**Training and injuries are now wired into the
career loop — for the first time, a saved season carries what players have actually been doing.**

Two systems were built earlier this month but had no way to run: the code existed, the save format
existed, and nothing anywhere created a player to run them on, so every save file wrote two empty
blocks. That gap is closed. There is now a single object that owns every club's training and medical
state, and the season loop calls into it once per day, in a fixed order — training first, injuries
second, because the injury risk reads the conditioning that training just wrote. Injured players are
filtered out of team selection, and each player's accumulated training fatigue now follows him into a
match as a starting fatigue level.

**Nothing about an existing game changes.** Every player starts on a balanced training programme,
whose daily workload happens to exactly cancel the daily recovery, so nobody accumulates fatigue and
every match plays out identically to before — checked in both directions, including a test that
deliberately fatigues a side to confirm the new number really does reach the simulation rather than
just being stored.

**Injuries themselves are wired but switched off**, deliberately and on measurement rather than
nerves. An earlier review measured what the current tuning would actually do: a new player would have
roughly a one-in-four chance of being injured on his very first day, and a tired one nearly a
one-in-two chance every day — while a player on the default programme would never be injured at all.
Those numbers are two to three orders of magnitude away from real football, and the project's own
rules say tuning waits until the whole chain is connected. So the plumbing is in and the tap is
closed; turning it on later is a one-word change, and everything downstream of an injury is already
built and tested.

One design point worth recording: a club must never be unable to field a team. The obvious rule —
"put injured players back if you drop below eighteen" — is wrong, because the team selector refuses a
squad with no goalkeeper regardless of how many outfielders are fit. So the rule is instead "bring the
least-injured back, one at a time, until the club can actually field a legal side", asked of the
selector itself rather than guessed at.

Two specification errors were filed: both systems' written plans referred to functions and data types
in the player-progression system that do not exist yet, so one part of the wiring waits for that
system's own turn. The gate could not run in the authoring environment (no .NET SDK available); CI
runs it on push. This entry also skips over two same-day landings recorded in
`docs/tracking/CHANGELOG.md` — the save codecs and their gate run — which never reached this file.
Prior entry below.)
**Last Updated (prior):** August 7, 2026 (**The match screen's rules about what you can click, and when,
are now written down in testable code rather than left for the Unity layer to invent.** The interactive
client is built in two halves on purpose: everything that *decides* something lives in ordinary C# that
the automated build compiles and tests on every push, and the Unity half only draws what it is told.
That split was already done for the pitch view; it had not been done for the screen's controls, so this
pass did it. Two things came out of it. First, the four playback speeds (1×, 3×, 5×, 10×) were four
unrelated numbers in a settings file — nothing said they form a ladder, which one a match starts at, or
what "faster" should do when you are already at the fastest. They are now an ordered ladder that stops
at the top instead of wrapping around to the slowest, which would have looked like a bug. Second, and
more useful: the rule that you cannot change tactics after the final whistle existed only as a sentence
in a design document. It is now a value the screen reads, so a control that cannot do anything is shown
as unavailable rather than quietly swallowing your click — and it deliberately keeps **saving** switched
on after full time, since that is exactly when someone wants to save. One real gap turned up along the
way: a note said the speed cap must allow 10×, but nothing checked it, and because the engine rejects an
out-of-range speed rather than quietly capping it, a misconfigured setting would have shipped a 10×
button that crashed mid-match while the other three worked fine. That is now checked at startup, so the
game refuses to launch instead. Nothing here touches the simulation — no save-format change, no change
to how matches play out. The build could not be run in this environment (the .NET installer is blocked
by the network policy), so CI compiles it on push. Prior entry below — note it skips the August 6
save-codec landing, which was recorded in the tracking documents but not here.)

**Last Updated (prior):** August 6, 2026, later same day (**ERR-008-021's same-day review caught the fix
being switched off exactly where it matters.** The adversarial review over the shot-blocking change
found that "don't weight the goalkeeper" had been implemented as "don't weight anyone within six
metres of the goal line" — which is where most shot-blocking happens, so ordinary defenders making
last-ditch blocks were still being treated as identical bodies. The exemption now applies to a
single player, the one nearest the goal line (the keeper), and every other defender is weighted.
The review also tightened the tests so a silently disabled feature can't stay green, and corrected
an overclaim: an average-attribute defender reproduces the old behaviour almost exactly, not
bit-for-bit. One High, seven Medium, five Low findings — all fixed. CI on push remains the
compiler and test runner for this work.)

**Last Updated (prior):** August 6, 2026 (**ERR-008-021 — a shot is now harder to take past a good defender
than past a poor one.** The judgment-proxy review's third fix closes the follow-up deferred when the
pass-lane template landed: the check that measures how much of the goal an opponent blocks out no
longer treats every outfield body as the same obstacle — a blocker's Anticipation and Pace now scale
his effective cover, read through the shooter's Vision, using the exact constants the pass lane
already uses, so no new tuning dial was added. The goalkeeper's cover deliberately stays as pure
geometry, because his shot-stopping quality is already priced at the save itself and counting it
twice would double-punish shooters. An average or unknown blocker reproduces today's behaviour
exactly. Spec and code landed in the same commit with six new test locks, including the away-side
mirror. The gate cannot run in this environment; CI on push compiles and executes it.)

**Last Updated (prior):** August 5, 2026, end of same day (**ERR-008-019 — one recorded claim about the
long-shot ramp was wrong and is withdrawn.** A review of yesterday's landing found that the note
saying the change "moves no match digests" rested on a false assumption about how a player takes
possession of the ball: it assumed the player must be within half a metre of it, when the engine
actually hands possession to anyone within a metre of a stationary loose ball and leaves the ball
where it is — and nothing pulls it back to him afterwards. That extra half-metre is enough for a
shooter rated 19 (not only 20) to take a midfield shot, and at 19 the new ramp gives a slightly
different number from the old step. So the change **can** alter match results on some seeds. That
is fine — the wider ramp is what the owner asked for — but the "no effect on results" claim was
not true and has been retracted everywhere it was recorded. Nothing else changed: no formula, no
tunable, no test. One documentation fix went with it (the ramp's half-width is pinned at its
maximum by a test, so the range its comment advertised was misleading). Gate not runnable in the
authoring environment; CI runs it on push. Prior entry below.)

**Last Updated (prior):** August 5, 2026, later same day (**ERR-008-019 owner revision — every Long Shots
point now matters for midfield shooting.** At owner direction, the just-landed ramp widened from
its initial 8–13 band to the full 1–20 attribute range: a rating of 1 keeps the full suppression,
20 the full long-shot modifier, and every point in between moves the willingness smoothly — no
plateaus. One tunable changed (`LONG_SHOT_RAMP_HALF_WIDTH` to its maximum 0.25); the formula, the
midpoint anchor, and the population-average balance are untouched, and the change still moves no
match digests (only a maximum-rated shooter can even generate a midfield shot, and for him the
ramp equals the old value). Gate not runnable in the authoring environment; CI runs it on push.
Prior entry below.)

**Last Updated (prior):** August 5, 2026 (**ERR-008-019 — the second fix under the football-judgment
remediation doctrine, and the closing of the review's founding finding.** Decision Tree #8
§3.2.3.1's midfield long-shot gate — the original "11× jump for a 1-point attribute difference"
cliff the whole judgment-proxy review was named after, whose earlier "FIXED" record proved false —
is now a linear ramp in the same shifted attribute form, centred on the old threshold so endpoints
and the population-integrated modifier are preserved (doctrine P1/P5; spec, code, ERR entry and
five test locks in one commit; the soft-reserved id re-verified free at landing). The branch is
production-unreachable in the only band the fix changes (the ramp differs from the old step only
for LongShots values whose own range gate keeps the shooter ~5 m short of any midfield shot), so
no digest moves; the fix lands anyway because a wrong-shaped model cannot be repaired by later
tuning. Review tally: 2 fixed, 32 open.
Gate not runnable in the authoring environment (no .NET SDK); CI runs it on push. Prior entry
below.)

**Last Updated (prior):** August 4, 2026 (**ERR-008-020 — the first fix under the football-judgment
remediation doctrine.** The new `docs/tracking/football-judgment-proxy-review.md` swept all 53
APPROVED specs for continuous football judgments collapsed into thresholds or bare geometry — 34
findings across 19 specs (corrected Aug 21, 2026 from 24) — and its owner-converged §6 doctrine (P1 continuous-never-cliff, P2 skill
as discrimination fidelity, P3 the attribute ownership ledger, P4 intent as a first-class object,
P5 chain calibration pivoted on today's baseline) now governs every fix. The template landed same
day: Decision Tree #8 §3.1.3.3's binary pass-lane interceptor corridor became a continuous,
attribute-weighted threat model — a defender's Anticipation/Pace now prices the lane, read through
the passer's Vision — with spec, code, ERR entry, and tests in one commit. Also corrected at that
landing: the review's inherited claim that the ERR-008-019 long-shot cliff was already fixed was
false — the finding is re-opened. Gate not runnable in the authoring environment (no .NET SDK);
CI runs it on push. *Drift note: this file had also trailed the two August-4 W1 keeper-rush
landings — see `docs/tracking/CHANGELOG.md` for those; they are not reconstructed here.* Prior
entry below.)

**Last Updated (prior):** August 3, 2026, latest same day (**Interactive Unity client P4a LANDED — the
host-free render model, and P4 is split.** P4a is every render *decision*; P4b is the binding. That
split turns the standing "keep logic out of `MonoBehaviour`s" rule from a discipline into a phase
boundary, so what the pinned host is left to verify is binding — which a cert run genuinely checks —
rather than behaviour, which it checks only along the paths someone thought to click. New in
gate-compiled `src/match-client-core/`: `PitchViewProjection` (the one documented corner-origin ⇄
centre-origin adapter, both directions; centring is what makes a home position and its away mirror
differ only in sign), `PitchMarkings`/`PitchMarking`/`PitchMarkingKind` (the IFAB catalogue as shapes,
read from the *existing* `MatchViewerConstants` `[FIXED]` values, both ends emitted from one loop over
a sign; rectangles arrive corner-normalised so a binding cannot draw the away boxes inverted),
`MatchRoster` (match-constant per-slot data, with the shirt-numbering rule shared with the browser
viewer through `RosterShirtNumbers`), and `MatchRenderProjection` → `AgentRenderModel`/`BallRenderModel`
(positions from the P3 interpolator's buffer, discrete cues from the newest frame, possession ring,
ball shadow/lift/capped scale). Colour-free by design — a palette has no correct answer a test could
assert. **The finding (KD-P4a-1):** `LiveMatchStreamer` cached goalkeeper flags as immutable roster
metadata, but `MatchEngine.SubstitutePlayer` rewrites them — so a keeper substitution had been drawing
the keeper ring on the wrong player in the browser viewer since P1. **The view was then revised to a tilted, slightly off-centre perspective camera (KD-P4a-2)** — which deletes the faked ball-height cues and their three `[GT]` dials, since a tilted camera conveys altitude by itself, and adds a ray/ground-plane click inverse in their place. The keeper flag now rides `LiveAgentCue`
per tick and `MatchRoster` deliberately holds none, which fixes both surfaces. No
`SNAPSHOT_SCHEMA_VERSION` change, no engine-behaviour change. **Adversarially reviewed August 4,
2026 — 1 High, 5 Medium, 3 Low fixed, then re-run clean:** the High was `PitchMarking.Rectangle`
taking its corners in either order while `PitchMarkings` builds the end boxes goal-line-inwards, so
the two away boxes arrived with descending X and a binding using `B − A` as an extent would have
drawn exactly those two inverted — #8 ERR-008-002's home/away asymmetry class, in a `MonoBehaviour`
the gate can never see. **Next: P4b on the pinned host.** **UPDATE August 15, 2026: P4b landed as
code** — `src/match-client-unity/MatchClientBehaviour.cs`; host verification is still outstanding, see
below.)

**Last Updated (prior):** August 3, 2026, latest same day (**Owner decision — roadmap B6 reversed: the product
ships the full Unity UI, not the web-hosted viewer.** Doc-only. `src/match-client-web/` is retained and
reclassified as the host-free reference harness — the only surface exercising read/playback/intent in
CI on every push — while `src/match-client-unity/` (asmdef + README; P4 never started) becomes the
critical path. Nothing blocks P4: the whole substrate a UGUI skin binds is already gate-compiled and
unchanged. Standing rule recorded in `interactive-unity-client-design.md` §12 and
`path-to-playable-roadmap.md` §7/C2 — **keep logic out of `MonoBehaviour`s**, since the CI gate cannot
compile that assembly and faking `MonoBehaviour` in the Unity shim is explicitly refused. `PM-1`'s three
screen-facing exit criteria reopen against the Unity client; its determinism criterion is met head-lessly
and stays met. **UPDATE August 15, 2026: P4b has since landed as code** —
`src/match-client-unity/MatchClientBehaviour.cs`; see below.)

**Last Updated (prior):** August 3, 2026, later same day (**Interactive Unity client P6 — the head-less
closed-loop scenario LANDED, ahead of P4.** The client's input-determinism claim is now checked on
every push rather than asserted. Two closed-loop scenarios on the #19 `ScenarioRunner`, booted through
the real `MatchSession`: same `MatchSetup` + the same tick-stamped command log ⇒ digest-identical runs,
and save@90 → restore → replay the post-90 log to tick 180 == the uninterrupted run. Landed before the
Unity render skin for the reason the plan's §12 gives — `match-client-unity` is excluded from the
`tools/dotnet-ci` shim, so **every P4/P5 line is invisible to CI** while this is not; the skin now
arrives against an existing lock rather than ahead of one.

**The phase needed three production additions before any scenario could be written**, because
`MatchSession` could not be advanced head-lessly, saved, or restored: `TickOnce()` (driving the real
streamer seam, and refusing fail-loud once paced playback has started — two threads through one engine
is a data race), `CaptureSave()` (riding the `ServiceOnce` seam so it works running, paused and at full
time, with the drained-empty-before-capture invariant now held by *ordering* rather than asserted), and
`RestoreFrom()` (a session over a restored engine, re-applying no boot mutator). Plus
`TickStampedCommandReplay` — the log-replay mechanism the reproducibility invariant is *defined*
against, under which the log is a fixed point of its own replay.

**The predicate that carries it is the control run.** Both scenarios would pass on a command channel
that did nothing — a run reproducing itself says nothing about whether the commands are in the loop —
so a third session with the same setup and **no commands** must DIVERGE, in a bounded window around
the first command. That is the direct lesson of the capstone that asserted a match ticked while every
match was a 90-minute 0–0 deadlock. The script drives all three live mutators across **both** teams.

No engine behaviour changed; no schema, RNG-stream, domain-tag, draw-site or draw-order change. Gate
not runnable in this environment (the network policy blocks the .NET SDK download); verified by
exhaustive manual review and a project-generation run, and it runs in CI on the PR. **Prior entry
below.**)

**Last Updated (prior):** August 3, 2026 (**§5.Z.23 conversion at contact — ERR-011-008: a keeper's CATCH
never stopped the ball.** #11 §3.5.2's catch branch is two statements — the possession record AND
`ball.velocity = gkHandVelocity` ("parked at hand position") — and only the first was implemented.
Possession here is a flag, not a kinematic constraint (the ball integrates unconditionally; the goal
check adjudicates on ball POSITION), so a claimed shot flew on into the net. Measured per contact over
three full matches: ball speed in → out is **parried 10.8 → 0.0, deflected 10.3 → 4.2, spilled
13.9 → 9.0, missed 9.5 → 9.5 — and caught 11.1 → 10.8**, with **7 of 10 catches followed by a goal
within 5 s** (parries and spills: zero). This **refutes** §5.Z.22's recorded premise that the residual
lay in what marginal parries and spills do. Fixed with a new `IGoalkeeperBallSystem.ParkBall()` at both
claim sites. **Goals 5.0 → 3.7 per match — the closest this engine has measured to football's ~2.7 —
scorelines 2-2/2-0/6-3 → 1-0/2-2/4-2.** No schema / RNG / draw-order change. Both levers §5.Z.22
named are recorded NOT fixed with evidence: the `pointQuality` lottery is confirmed blind *and*
inverted, and the geometry-aware form was implemented, measured (catches 11 → 0 at every in-range
`[GT]`) and reverted — it is blocked on a design decision, not effort; parry placement produced zero
goals in either corpus. The creation residual is re-localized from "possession churn" to a measured
stage: **final third → penalty area converts at 6.5% against football's ~40%**, while shots per BOX
entry already run above football's rate.

> **Note on this file's currency:** the entry chain below trails the engine by several landings — the
> previous entry is the July 27 shot-outcome pass, so §5.Z.18–§5.Z.22 (shot speed and woodwork, the
> keeper's catch/parry conversion, shot volume, the keeper's contact rate) and the August 3 project-
> skills landing are recorded in `docs/tracking/CHANGELOG.md` and `docs/tracking/PROGRESS.md` but not
> here. Reconciling them is its own pass; this entry was appended rather than layered over a silently
> stale summary.)

**Last Updated (prior):** July 27, 2026, latest same day (**Shot-outcome distribution pass — §5.Z.17's residual,
the named A4a blocker, fixed and measured.** Shots can now miss (a genuine `tan(err) × distance` error
cone, ERR-006-003, plus the vertical placement/error half made live per #6's own §3.5.6/§3.5.7 —
ERR-006-002), **the goal has a crossbar** (the `z < 0.22 m` boundary gate removed per Law 9/10:
airborne crossings adjudicate at the crossing — ERR-001-004), and **shots are blocked** (the empty-TODO
agent-ball deflection is live via the new `BallCollision.ApplyAgentDeflection`, `BodyPartCoefficients`'
first consumer — ERR-003-007), with the shot pressure query wired and the vacuous goal-visibility gate
raised off its floor. Measured over three full matches, same seeds pre/post: **goals 15.3 → 12.3 per
match, goals/shot 0.24–0.29 → 0.14–0.25, fast-ball body deflections 0 → 560–612 per match.** The
remaining goal-rate mass is recorded, not fixed: shot volume (~2.5× football), shot speed (means
7–10 m/s vs ~25), keeper conversion. New `match-engine-shot-outcomes` acceptance scenario — 3 of 8
predicates fail on the pre-fix engine, verified by execution. Full dotnet gate PASSED; no
`SNAPSHOT_SCHEMA_VERSION` change. See `docs/tracking/shot-outcome-distribution-design.md`. Prior entry
below.)
**Last Updated (prior):** July 27, 2026, later same day (**Documentation sync pass — no code, no spec, no gate
run.** Cross-referenced this file, `CLAUDE.md`, `src/CLAUDE.md`, `SPEC_INDEX.md`, and `docs/tracking/`
against the actual repo state and found four discrepancies, all now corrected here and in the other root
docs: (1) **Match Analytics #37 T0** (`src/match-analytics/` — value types + `XgLocationModel`) landed
July 27 without updating this file or `CLAUDE.md`, so the assembly count (29 → **30**) and the
"APPROVED with no assembly" count (23 → **22**) were both stale, and #37's status row here still read
"none". (2) **Track C B1** (the interactive-Unity-client richer observation frame) landed the same way —
already recorded in `path-to-playable-roadmap.md` but not folded into either root doc. (3)
**`SNAPSHOT_SCHEMA_VERSION` was stale at 18**; the match-realism landing below (home/away asymmetry +
contact-rate fixes) bumped it to **19** the same day. (4) The home/away-asymmetry / goal-rate OPEN ISSUES
entry in `CLAUDE.md` was stale in the other direction — it described the asymmetry as an open blocker,
but the root cause (`GoalGeometryProvider` always returning the same `GoalLineX`, so both teams shot at
one goal) was found and fixed the same day; corrected there. `docs/tracking/file-manifest.md`'s
"Current Specification Folders" table was separately found stuck at 26 rows since July 8, 2026 (missing
the entire #27–#54 wave) and is fixed in the same pass. Prior entry below.)
**Last Updated (prior):** July 27, 2026, later same day (**The specification phase is CLOSED — all ten
approved.** `SPEC_INDEX.md` reads **53 APPROVED / 0 IN REVIEW / 0 NOT STARTED**. Lead-developer sign-off
granted on #53, #35, #46, #36, #54, #47, #48, #50, #51 and #39, with **23 back-props filed atomically**.
Docs only — no code, no `src/` change, no gate run. **The finding that justified landing them together:** #30's pinned tick order was not implementable, because `ERR-030-007` had been filed twice at two separate approvals — a defect neither approval could have seen alone. See VERSION HISTORY v1.37.)
**Last Updated (prior):** July 26, 2026 (**Root-doc reconciliation — this file re-based on the actual repo
state.** It had been pinned at July 14, 2026 and **26 specs**: twelve days and seventeen approved
specs stale, still describing `SNAPSHOT_SCHEMA_VERSION` 15 (actual **18**) and still listing the
Unity 6 recertification and the FR-PO-052 perf baseline as outstanding — both of which completed
July 19. Updated: CURRENT STATUS (**43 APPROVED / 0 IN REVIEW / 0 NOT STARTED**; 29 production
assemblies; the Phase-H possession bootstrap that makes a match actually play); the specification
schedule (a new table for the 17 management-layer specs #27–#49 approved July 22–25, each with its
real implementation status, plus the seven design supplements awaiting promotion); PROJECT STRUCTURE
(the real tree — `tools/`, `docs/design/`, the Unity shell, and a layer-grouped `src/` listing);
and NEXT IMMEDIATE STEPS (re-based on `path-to-playable-roadmap.md`, which is now the critical path).
**The load-bearing addition is the honest gap:** **13 APPROVED specs have no assembly at all**
(#29, #31–#34, #37, #40–#45, #49) — "approved" had become a misleading proxy for "a consumer
exists", and both root docs now say so plainly. Also recorded: assembly names do not reliably match
spec folder names (#27 → `player-database`, #28 → `player-progression`, #30 → `season-save`, and #38
→ `ui-framework`, with #23–#25 inside `positioning-ai` and #26 inside `tactical-instructions`), and the
42-file design-supplement governance class, which had appeared in no root document. The dotnet gate
was **not** re-run in the authoring environment (no SDK), so the gate claims restated here were quoted
from the last landing's record — but CI subsequently ran the full Linux shim gate green on this
branch, re-verifying them independently. See `CLAUDE.md` for the matching pass on the
AI-behavioural-rules side.)
**Last Updated (prior):** July 14, 2026 (**Match-flow model completion LANDED** — throw-ins, corners,
goal kicks, fouls/cards, offside, substitutions, half-time break, and full-time end (previously
only kickoff + goal-restart existed). Design doc adversarially reviewed to convergence, then
implemented, then the code itself adversarially reviewed to convergence (catching an
`OffsideEvaluator` bug where too few defenders left an accumulator at an `Infinity` sentinel
instead of `NaN`, inverting the offside rule for every finite attacker position). New
`src/match-engine/RestartResolver.cs`, `OffsideEvaluator.cs`, `SubstitutionReason.cs`; three new
Tier A events (`OffsideCalledEvent` 0x18, `RestartAwardedEvent` 0x19, `MatchPhaseChangedEvent`
0x1A). `MatchEngine.cs` v1.31 gains restart routing, per-tick foul/card detection with sent-off
tracking, offside evaluation on pass reception, a public `SubstitutePlayer` (bench-roster swap,
pending-event queue flushed at the next Resolve phase per an AR-5 fix), and half-time/full-time
transition handling. **`SNAPSHOT_SCHEMA_VERSION` 14 → 15.** New test suites for restarts, offside,
fouls/cards, substitutions, and match-flow transitions. Full dotnet gate not runnable in this
environment — verified by exhaustive manual code review in place of `dotnet test`. See
`docs/tracking/match-flow-completion-design.md`, `docs/tracking/match-engine-design.md` v2.0, and
`src/CLAUDE.md` v2.17.)
**Last Updated (prior):** July 13, 2026, later same day (**P1 real perf harness landed** —
`StopwatchPerfHarness` + `MatchEngineCapstonePerfHarness` boot the real capstone scenario and
Stopwatch-time each `RunTick`, superseding the synthetic `run.sh` stub that ran no `src/` code.
The Linux run is stamped non-certifying; a certified number still needs pinned-host access
(Windows 11 / Unity 6000.4.9f1) plus a real cert run. `CertifiedPerfBaseline.Stage0CertPlatformPin`
updated to the Unity-6 tuple string.)
**Last Updated (prior):** July 13, 2026 (**Unity engine version bumped: 2022.3.62f1 → Unity
6000.4.9f1, graphics API pinned DX11 — documentation-only, no recertification performed.**
`certification-platform.md` → v1.3, now v1.4, Status reverted `✅ PINNED` → `⏳ RECERT REQUIRED` per its own
Maintenance Rule; every downstream unblocker it previously closed (`FR-DS-009-GATE`, `FR-PO-052`,
the §7.5 D1 test-runner pin, `EnvironmentFingerprint`) is blocked again until a real cert run
executes against the new tuple. Historical `Unity 2022.3` citations inside already-`APPROVED`
spec section files deliberately left untouched — frozen approval-time records, per this project's
"historical rows preserved verbatim" convention.)
**Last Updated (prior):** July 11, 2026, latest same day (**Engine substrate landed** — goal
detection (`MatchEngine.cs` v1.30: Resolve-phase goal check + scoring + centre-spot restart +
`GoalAwardedEvent`) and the match-length/halves model (`MATCH_TICKS_TOTAL` = 324,000 ticks,
`HALF_TIME_BOUNDARY_TICK` = 162,000). **`SNAPSHOT_SCHEMA_VERSION` 13 → 14.** This activates
Tactical Presets **#26**'s half-time trigger and live goalDiff/clock ladder inputs — its
engine-substrate gates (§9.1) are now closed; only the §9.2 `[GT]` balance-pass review remains.
Full dotnet gate: PASSED, 0 failures.)
**Last Updated (prior):** July 11, 2026 (**Specs #23/#24/#25/#26 wiring landed**, all
default-behaviour-neutral — Balanced ⇒ Off/None/Off/Human are exact identities, byte-identical
default match: the #24 build-up overlay + #23 dismark offset `SlotComposer` stages;
`positioning-ai/RotationController.cs` (#25) wired into `PositioningAITick`; the #23 marked-pass-
target penalty in the Decision Tree's `UtilityScorer`; and **#26**'s full T1–T4 stack (preset→
config projection, kickoff/interval decision gate, kickoff scoring, mid-match adaptation ladder)
via `ManagerDecisionGate`/`ManagerProfile`/`ManagerAdaptation`. `SNAPSHOT_SCHEMA_VERSION` 11 → 12
→ 13 across the two commits. Full dotnet gate: PASSED, 0 failures both times.)
**Last Updated (prior):** July 10, 2026, later same day (**Specs #23–#26 — Dismarking &
Marker-Awareness AI, Scripted Build-Up Structures, Positional Rotations, Tactical Presets &
AI-Manager Selection — all advanced `IN REVIEW` → `APPROVED`.** PASS-1 section-file adversarial
reviews resolved same day; lead-developer R-01..R-05 sign-off granted on all four; the seven
cross-spec back-prop ERRs filed and landed atomically; the #26 Bradley citation VERIFIED. `docs/
specs/dismarking-ai/`, `build-up-structures/`, `positional-rotations/`, `tactical-presets/`
folders now exist. **`SPEC_INDEX.md`: 26 APPROVED / 0 IN REVIEW / 0 NOT STARTED.** T0 scaffolding
(behaviour-neutral data types + pure math) landed the same day.)
**Last Updated (prior):** July 10, 2026 (**#23–#26 post-PASS-1 gates closed where closable** — §8
citations verified/replaced/reclassified across all four specs (one #26 row remains pending with
a recorded environment-blocked attempt); #25 Appendix A completed for all three formation
families; #26 A.1 preset compositions pinned against the #21 enums. Remaining before `APPROVED`:
the #26 Bradley citation, back-prop ERRs, #26 engine-substrate gates, R-01..R-05 sign-off.)
**Last Updated (prior):** July 8, 2026 (Candidates **#23–#26 promoted to section files at `IN REVIEW`** —
`docs/specs/dismarking-ai/` (#23), `docs/specs/build-up-structures/` (#24),
`docs/specs/positional-rotations/` (#25), `docs/specs/tactical-presets/` (#26), each a full
11-file spec set (v0.1) authored from its July 7 design supplement per that supplement's §6
promotion pipeline. `SPEC_INDEX.md` now reads **22 APPROVED / 4 IN REVIEW**; the RESERVED entries
are retired. Section-file PASS-1 adversarial reviews are pending per each spec's §9.3 — no `src/`
code lands until each is `APPROVED`.)
**Last Updated (prior):** July 7, 2026, later same day (Two design supplements opened, scoping the four
items the same day's tactical-theory cross-reference flagged as too large for a cheap seam
reuse: `docs/tracking/advanced-positional-behaviors-design.md` (dismarking, scripted build-up
structures, positional rotations — candidate specs #23–#25) and
`docs/tracking/game-model-ai-manager-design.md` (tactical preset library + AI-manager
selection/adaptation — candidate #26). DESIGN SUPPLEMENT stage only, pre-promotion, no code —
see root `CLAUDE.md` OPEN ISSUES and `SPEC_INDEX.md` "RESERVED" section.)
**Last Updated (prior):** July 7, 2026 (Tactical-theory research cross-reference: four small tactical additions landed on top of #21/#12/#13/#14/#8 — a `MarkingOrientation` dial (ball- vs man-oriented marking, scales the #14 MAN_MARK radius), a Positioning AI #12 rest-defense coverage check (dampens risky PASS/SHOOT/DRIBBLE when insufficient cover is left behind while attacking), a half-spaces PASS bonus (routes each agent's existing #12 lane into the Decision Tree #8 utility scorer), and a curving-press blind-side bias (#13 nudges the primary presser's approach toward the ball carrier's blind side). All four default to today's exact behaviour (byte-identical) until a manager sets a non-default tactic. `SNAPSHOT_SCHEMA_VERSION` 10 → 11. Prior June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
**Last Updated (prior):** July 7, 2026, later same day (Tactical-theory research cross-reference follow-up: after user review, three of the four July 7 tactical additions were corrected. A `MarkingOrientation` dial (ball- vs man-oriented marking, scales the #14 MAN_MARK radius) stands unchanged. The rest-defense coverage check (Positioning AI #12) is redesigned so its PASS/SHOOT/DRIBBLE dampener only applies in proportion to the ball carrier's own tactical awareness — an unaware carrier is never silently corrected for what is really a manager-facing setup flaw. The half-spaces PASS bonus is reverted entirely — half-spaces are an exploitable spatial gap that requires tactical/player instructions to exploit, not a flat statistical bonus. The curving-press mechanic is redesigned from a flat "blind-side approach" bias into a cover-shadow curve (#13): a presser now bends their pursuit path toward denying a nearby passing option, scaled by their own defensive/physical/mental attributes, so a poor or low-effort defender barely curves at all. `SNAPSHOT_SCHEMA_VERSION` 10 → 11 (unaffected by the corrections — none of the reverted/redesigned items were ever serialized). Prior July 7, 2026 (initial landing, since corrected): Tactical-theory research cross-reference: four small tactical additions landed on top of #21/#12/#13/#14/#8. Prior June 29, 2026 (Tactical Instructions **#21** T0 data layer + T2 consumer seams (DecisionTree/Pressing/Positioning/Defensive/Attacking, behaviour-neutral) + runtime activation landed — `MatchEngine.SetTeamTactic`, the per-team Phase-D writers for #8/#13, and an in-code `TeamTacticConfig` source + boot applier; the on-disk tactic-file format is deferred to Stage 0+1 per the #19 D1 precedent. Prior June 20, 2026: Tactical Instructions **#21** APPROVED — first Stage-1 forward spec beyond the Stage-0 set of 20: the manager-facing tactical instruction layer (formation/mentality/team instructions/player roles+duties/individual instructions) that drives the existing AI subsystems #8/#11–#15. 11 section files at v0.3 after PASS-1 + PASS-2 adversarial reviews; lead-developer sign-off granted. Its `[GT]` balance-pass values are illustrative pending a non-blocking Stage-1 follow-up; no `src/` code yet (T0 scaffolding pending). `SPEC_INDEX.md` count now **21 APPROVED**. Prior June 7, 2026: AR-hardening sweep complete across all 18 coded sections; 350+ source files across 17 spec assemblies + Testing Strategy CI tooling. See `docs/tracking/PROGRESS.md` for per-spec AR-round tallies.)
