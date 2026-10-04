# System XI — S0 Binding Contracts

**Created:** October 3, 2026\
**Last Updated:** October 3, 2026\
**Version:** 0.2\
**Status:** CONTRACT REVIEW DRAFT — TO BUILD; Gate I IN PROGRESS\
**Purpose:** complete the S0-I-001/002/003 implementation inputs for the owner-approved directions.\
**Journey authority:** [S0 packet §14](ux-s0-pm1-journey.md#14-gate-i--p5b-implementation-handoff-draft)\
**Execution authority:** [detailed plan](ux-detailed-plan.md) §5, Gates I/J

## 1. Authority and audited boundary

This is a proposed client implementation contract against `main`
`3429fafdf998eaeee9b63bfb356c68c02679d761` (#477). It implements no API, fixture,
catalogue or screen. The owner approved distinct demo squads, 100–200% text size and
the client formatter on October 3 (journey §14.10). That approval supplies direction;
the concrete allocations here still need final handoff review. APPROVED specs remain
authoritative. Exact class/file names below are proposed landing names, not existing symbols.

Audited source: `MatchSetup` accepts both squads; `MatchSession.BootEngine` calls
`ConfigureSquads`; engine `LineupSelector` selects the XI/bench; `PlayerIdsByAgentId`
copies identity at boot/fixture cadence. `LiveMatchStreamer.TickOnce` drains commands,
runs the engine and observer, captures the frame under `_tickGate`, then publishes it
under `_lock`. `LiveAgentCue` carries discipline, bench-origin and keeper cues but no
player id. `MatchRoster`/`RosterShirtNumbers` supply slot ordinals. `ILocalizer` exists;
`Localizer`/`TemplateCatalogue` do not. #49 FR-LC-019 records a read-only presentation
settings boundary, without an implemented options store or a scale notification.

The four screens, five navigation moves, current immediate-substitution semantics,
command-outcome adapter and analytics-health contract stay in journey §§14.2–6.

## 2. S0-I-001 — demo fixture and synchronized identity

### 2.1 Immutable content and engine selection

P5b owns **TO BUILD `S0DemoFixture` revision 1**, in gate-compiled client content,
consumed by the setup builder in the same landing. It provides two `Squad` values
and a player-id lookup for authored demo shirt metadata. Home/Away remain the team
display names; no real club affiliation is implied. The following synthetic players
are the proposed fixture content, not professional-player data or approved image content.

Each row is a squad-local record index, never a pitch/bench command index. Home
`ClubId = 1`, away `ClubId = 2`; derive each id with #27's existing
`clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + localIndex` rule. Age is 25;
canonical attributes use `PlayerAttributes.CreateDefault()` (31 attributes at 10,
weak foot at 3). Position is the exact `PlayerPosition` enum below. Names are
`FirstName`/`LastName` in the existing records; only shirt metadata is new client content.
The shared defaults intentionally avoid an additional tuning pass or roster RNG draws.

| Local index | Position | Home first / last name | Away first / last name | Authored shirt (both teams) |
|---|---|---|---|---:|
| 0 | Goalkeeper | Alex / Rowan | Casey / Brooks | 1 |
| 1 | Defender | Ben / Calder | Drew / Ellis | 2 |
| 2 | Defender | Chris / Vale | Evan / Reed | 3 |
| 3 | Defender | Daniel / North | Finn / Hayes | 4 |
| 4 | Defender | Eli / Mercer | Gray / Nolan | 5 |
| 5 | Midfielder | Felix / Arden | Hugo / Wells | 6 |
| 6 | Midfielder | George / Lane | Isaac / Cole | 7 |
| 7 | Midfielder | Henry / Moss | Jamie / Quinn | 8 |
| 8 | Midfielder | Ian / Parker | Kai / Foster | 9 |
| 9 | Forward | Jordan / Bell | Leo / Marsh | 10 |
| 10 | Forward | Kit / Dawson | Morgan / Blake | 11 |
| 11 | Goalkeeper | Liam / Hart | Noah / Finch | 12 |
| 12 | Defender | Miles / West | Owen / Stone | 13 |
| 13 | Defender | Nico / Hale | Perry / Ward | 14 |
| 14 | Midfielder | Oliver / Shaw | Remy / Cross | 15 |
| 15 | Midfielder | Pat / Linden | Sam / Rivers | 16 |
| 16 | Forward | Robin / Ash | Taylor / Dale | 17 |
| 17 | Forward | Theo / Fox | Will / Heath | 18 |

The engine remains the only lineup selector. Do not derive a pitch or bench address
from these rows or shirts, or invoke a parallel selection algorithm in the client.
Validate unique ids across the pair, complete/nonblank names, valid attributes/positions,
and one positive, unique shirt per player within each team before session creation.
Copy mutable `PlayerRecord` values into the admitted fixture; expose no mutable content
to a presenter. Actual engine boot must prove that the fixture fills its required XI
and seven bench places. Configuration failures are developer/content admission failures,
not a new player-facing retry path. No squad editor, roster generation or save UI is added.

Keep seed 1, home Human, away AI/default profile, heading disabled and the existing
Balanced tactic with only the chosen home Mentality changed (journey §14.2).
Names/shirts do not enter gameplay. Supplying squads changes the setup and can change
simulation outcomes despite identical numeric defaults. Gate J records fixture revision,
content SHA-256, setup/head and repeatability, and compares that setup with the former
no-squad setup at the same seed/manager/tactic/heading inputs. Report changes; do not tune
gameplay or replace the frozen engine corpus as part of this UI landing.

### 2.2 Published identity and ownership

| Carrier / producer | Required fields and publication rule | Consumer |
|---|---|---|
| TO BUILD session boot roster descriptor | Copy `PlayerIdsByAgentId()` once after configured engine boot and before playback. Retain the team/bench-origin-to-player-id entries and fixture identity; do not treat the full mapping as permanently one-to-one. Arrays are privately owned immutable copies. | setup/session composition → host-free roster presenter; bench candidates |
| TO BUILD scalar engine observation | A read-only `AgentPlayerId(agentId)` observation over `_slotPlayerIds` for valid on-pitch slots, consumed only by the frame capturer under the tick gate. No allocation, writes, selection or change notification. Land producer and consumer together. Preserve the boot-only copy accessor's cadence. | `LiveMatchStreamer.CaptureFrame` |
| TO BUILD per-agent frame cue | Extend `LiveAgentCue` with the current `PlayerId`, sampled with its existing position/discipline/bench/keeper cues in the same captured frame. Neutral matches retain `NO_PLAYER_ID`; zero remains a valid id. Update every cue constructor/test producer explicitly. | `LiveFrameLatch` → host-free identity/render projection |
| TO BUILD resolved presentation identity | Session-scoped team id, command slot, player id, first/last name, authored shirt and current frame keeper/substitute cues; name/shirt lookup is immutable content. Pitch marker number and chooser label resolve from this same identity. | pitch, outgoing chooser, retained request records, report history |

The descriptor is a boot input, not a new live getter for Unity. A new session always
gets a new descriptor, latch and presenter. The UI acquires one accepted frame and
uses it throughout a presentation refresh; it never pairs a live engine copy or boot
occupant with cues from another tick. No additional mutable occupant store is admitted.
Extend the existing projection to use authored player shirts for the S0 fixture;
retain slot-ordinal behavior explicitly for neutral reference clients. `MatchRoster`'s
constant team metadata remains valid; its cached slot shirt is not S0 player metadata.
Do not globally change the browser reference client's identity policy incidentally.

Identity is discrete, never interpolated. If a slot's previous/current frame player ids
differ, use the current identity and snap that slot's position to the current sample
rather than blending two different players. At unchanged identity keep the existing
interpolation behavior. Sent-off/keeper/substitute decisions use that current frame.

An outgoing option's payload remains the engine's zero-based on-pitch slot. An incoming
payload remains the zero-based bench index; its player id comes from the boot descriptor,
and its displayed bench number is index + 1. Exclude used bench indices using current
`AgentCues.BenchSlot`, sent-off/replaced outgoing slots and the existing cap; do not
deduplicate by player id, because a used player's bench-origin id remains in the engine.

### 2.3 Pending, Applied and replacement

At Submit snapshot the resolved outgoing and incoming identities beside the full command
payload in the local request record. These immutable names/shirts label that request
through Pending/Applied/Refused/Not applied and report entry; later occupants cannot
rewrite history. Revalidate staging against the latest accepted frame before dispatch.
No predictive identity replacement occurs on selection, enqueue or Pending.

Applied comes from the driver log (§14.5). Current pitch/chooser identity changes only
when the accepted frame carries the engine's new occupant. A log may be observed before
its corresponding frame: keep the old current occupant until that frame arrives, while
showing the evidenced outcome with the request's retained identities. Do not patch the
frame or invent a second identity owner to erase that interval. Refusal/send failure
changes no occupant. Missing/mismatched identity in an admitted S0 fixture is an integration
failure to fix before acceptance, not permission to fabricate a name or borrow a slot shirt.

I-Q09/I-Q14 at J must exercise home/away mapping, used bench-origin duplication, keeper
replacement, skipped/repeated frames, immutable old request labels and a second fresh
session. The identity landing consumes every new read surface in that same landing.

## 3. S0-I-002 — read-only presentation scale

P5b/#38 owns **TO BUILD `S0PresentationConfiguration`**, passed at shell construction
to host-free presenters/layout decisions and thin Unity bindings. Its scale is a finite
`float` multiplier in inclusive [1.0, 2.0], default 1.0; 1.5 and 2.0 are required QA
points, not an options menu. Reject invalid supplied configuration at composition;
do not silently clamp or infer it from display zoom. Freeze it for the shell lifetime.
A new value constructs a new shell; no nonexistent #49 notification or persistence is used.
This consumes FR-LC-019's read-only boundary locally without implementing Wave-8 option
content, contrast/input-assist settings or a settings store. Never send scale to the sim.

For base text size `b` (client units) and scale `s` (unitless), apply `b * s` consistently:
e.g. 16 at 1.5 yields 24, and 16 at 2.0 yields 32. Keep text autoshrink off; do not cancel
the selected scale to make a panel fit. Measure laid-out text with the packaged fonts.
Padding/row heights must accommodate that measured text and the focus ring.

| Surface | Binding/reflow contract |
|---|---|
| MM / TS | Scale headings, instructions, Mentality names/effects, reasons and actions. Wrap labels; setup choices and Start/Back stay reachable with vertical scroll. No truncation of the selected effect. |
| MV header / playback | Scale team/score/period/minute/speed/current-value/reason text. Wrap context rows; preserve score/state and report/playback actions. Hide unavailable clock/score as already mapped. |
| Pitch | Scale marker text, identity descriptions and legend. Preserve pitch aspect ratio and world coordinates. Host-free collision/label placement uses measured bounds; permitted displacement must still identify the marker. Scroll/stack rather than shrink text or stretch the pitch. |
| MV rail / history / statistics | Scale action/reason/outcome/header/table text. Stack pitch then rail when measured available width cannot fit both. Wrap names/outcomes, keep retained disclosures and focus, and use a shared Home/Away table snapshot. |
| Both modals | Scale title, option/name/shirt/bench labels, effects, comparison, reasons, Submit/Cancel. Content scrolls inside the viewport; focused control scrolls into view. Full-viewport backdrop blocks the background regardless of content height. |
| PR | Scale result/score/health/table/partial disclosure/Return. Wrap and scroll; Return stays outside partial disclosure. |

At 100% and 1920×1080 / 2560×1440 retain H's whole-pitch-without-scroll target;
1366×768 permits vertical scroll. Enlarged/expanded text may scroll at any size.
No horizontal clipping, hidden primary action, focus-ring overlap or inaccessible modal
commit is acceptable. Reflow preserves journey §14.3's logical order and focus target;
frame refresh never resets scroll/disclosure. Display zoom is a separate recorded input.
Gate J verifies all three dimensions at 100/150/200%, expanded pseudo text, long names,
all states and both modals with actual fonts; H's browser evidence is not that result.

## 4. S0-I-003 — copy schema and fixed formatting context

### 4.1 Composition and validation

P5b owns **TO BUILD `S0CopyRole`, role schema and `S0CopyFormatter`** in gate-compiled
client code, consumed by all four presenters. The composition injects one L2 `ILocalizer`,
admitted immutable content and number-format provider for the entire shell. Initial shipped
content uses `LocaleId.BaseLocale` (`en`) and the single read-only provider
`NumberFormatInfo.InvariantInfo`, fixed for the shell lifetime and recorded in evidence.
No platform-culture lookup or provider fallback is required for S0; never parse
`LocaleId.Value` as a platform culture name. Future selected locales need an explicit locale-to-provider
admission mapping and valid base coverage before admission; no locale menu is added.

Each role has exactly one `ui.s0.*` key and one argument schema below. Static roles have
no arguments. Dynamic roles resolve the complete pattern with `ILocalizer.Resolve`,
then format with the bound provider. Resolved label/name strings are arguments, never
keys; format them once, not recursively. No `Render` request, producer tag, RNG draw or
English sentence assembly is used. Name components occupy distinct string arguments so
authored patterns can order them. A formatter receives typed values, not an untyped
caller-supplied format string. Status words are in each complete outcome pattern; separate
status badges must use the matching status key and never replace the sentence's status.

Client build lint validates every authored selected/base S0 pattern before publication:
exact required argument-index set (reordering/repetition allowed), escaped braces,
and permitted specifiers. Strings allow `{i}` only; nonnegative integers allow `{i}`
or `{i:D}`; display floats allow `{i:F1}`. No alignment/padding specifier or arbitrary
format is admitted. Zero-argument labels may contain only escaped literal braces.
Missing/extra indices, mismatched specifiers, malformed braces or missing base roles
fail the content build; a rejected candidate is never published. Absent translations
are allowed with valid base coverage and use L2's KD-5 fallback. Runtime invalid-pattern
retry is not allocated: `Resolve` exposes neither provenance nor a base lookup.
Client schemas/tests do not become dependencies of generic `localization`.

### 4.2 Static role/key register

All keys in this table have the prefix `ui.s0.`. Every listed suffix is a separate
zero-argument role and key; slash-separated entries below are exact suffix → base-copy
pairs, not runtime alternatives. System XI remains brand text. Diagnostic exception
messages, prototype controls/captions and synthetic/captured-data warnings do not ship.

| Copy family | Exact suffix → base label |
|---|---|
| headings | `heading.menu` → From the touchline; `heading.setup` → Tactics Setup; `heading.match` → Match View; `heading.report` → Post-Match Report; `heading.changes` → Home team changes; `heading.statistics` → Match statistics |
| entry/context | `context.menu` → Manage the home side in one demo match. The opponent is AI-managed.; `context.setup` → You manage Home. Away is AI-managed.; `context.mentality_choice` → Home Mentality — choose one |
| navigation | `action.demo` → Play a demo match; `action.start` → Start match; `action.back` → Back; `action.report` → View match report; `action.return` → Return to main menu |
| playback | `action.slower` → Slower; `action.pause` → Pause; `action.resume` → Resume; `action.faster` → Faster; `context.speed_steps` → Speed steps: 1×, 3×, 5×, 10×. |
| staging | `action.mentality` → Change Mentality; `action.substitution` → Make substitution; `action.submit` → Submit change; `action.cancel` → Cancel; `action.compare` → Compare all seven Mentalities; `dialog.mentality` → Change Home Mentality; `dialog.substitution` → Make Home substitution |
| fields/tags | `field.requested_mentality` → Requested Mentality; `field.outgoing` → Outgoing home player; `field.incoming` → Incoming bench player; `tag.current` → Current; `tag.requested` → Requested |
| staging instructions | `context.submit` → Choose a change, then submit it. Your team changes only when you see Applied.; `context.substitution` → Choose a player by name and shirt number, then an unused bench player. The substitution takes effect when the request is applied; it does not wait for a stoppage. |
| state | `state.waiting` → Waiting for first frame; `state.clock_waiting` → Clock awaiting first frame; `state.first_half` → First half; `state.second_half` → Second half; `state.full_time` → Full time; `state.paused` → Paused; `state.score_unavailable` → — |
| identity | `team.home` → Home; `team.away` → Away; `team.home_short` → H; `team.away_short` → A; `identity.goalkeeper` → goalkeeper |
| outcomes | `status.pending` → Pending; `status.applied` → Applied; `status.refused` → Refused; `status.not_applied` → Not applied; `status.send_failure` → Send failure; `heading.feedback` → Change request feedback |
| availability | `reason.waiting` → Match starting — controls unavailable until the first frame.; `reason.ended` → Match ended — playback and team changes are unavailable.; `reason.slowest` → Already at real time — slower is unavailable.; `reason.fastest` → Already at the fastest speed — faster is unavailable.; `reason.mentality_pending` → Mentality request pending — another request is unavailable until resolved.; `reason.substitution_pending` → Substitution request pending — another request is unavailable until resolved.; `reason.substitution_cap` → All substitutions used.; `reason.no_pair` → No legal outgoing player and unused bench player pair is available.; `reason.choice_changed` → This choice is no longer available. Choose an available player.; `reason.resume` → Resume to apply pending requests. |
| disclosures/end state | `action.statistics_open` → Open statistics; `action.statistics_close` → Close statistics; `action.partial` → Show partial statistics — incomplete; `statistics.retained` → Statistics panel retained at full time.; `statistics.closed` → Statistics panel closed at full time.; `statistics.report_available` → Final statistics are available in the match report. |
| statistics labels | `statistics.column` → Statistic; `statistics.goals` → Goals recorded; `statistics.possession` → Possession %; `statistics.territory` → Territorial %; `statistics.fouls` → Fouls; `statistics.yellow` → Yellow cards; `statistics.red` → Red cards; `statistics.offsides` → Offsides; `statistics.corners` → Corners; `statistics.throw_ins` → Throw-ins; `statistics.goal_kicks` → Goal kicks; `statistics.substitutions` → Substitutions; `statistics.xg` → Expected goals (xG); `statistics.caption` → Match statistics; `statistics.loose_ball` → Possession shares include loose-ball time; the two teams need not total 100%. |
| pitch/context | `pitch.waiting` → Pitch appears after the first frame.; `pitch.direction` → Home attacks right → • Away attacks left ←; `pitch.description` → Match pitch. Home H attacks right; Away A attacks left. Goals, penalty areas and ball shown.; `pitch.legend` → H/A identify Home/Away player shirts. A white-outlined substitute marker identifies an applied substitution. |
| home result | `result.win` → Win; `result.draw` → Draw; `result.loss` → Loss |

Mentality mapping is exhaustive over the existing seven-value enum, not enum ordinal
arithmetic or raw `ToString()`. Each row supplies two static roles under `ui.s0.`:

| Enum value | `mentality.<suffix>` label | `effect.<suffix>` base sentence |
|---|---|---|
| VeryDefensive | `very_defensive` → Very Defensive | `very_defensive` → Least risk in on-ball choices; deepest defensive line. |
| Defensive | `defensive` → Defensive | `defensive` → Less risk in on-ball choices; deeper defensive line. |
| Cautious | `cautious` → Cautious | `cautious` → Slightly less risk in on-ball choices; slightly deeper defensive line. |
| Balanced | `balanced` → Balanced | `balanced` → Standard risk in on-ball choices and standard defensive line. |
| Positive | `positive` → Positive | `positive` → Slightly more risk in on-ball choices; slightly higher defensive line. |
| Attacking | `attacking` → Attacking | `attacking` → More risk in on-ball choices; higher defensive line. |
| VeryAttacking | `very_attacking` → Very Attacking | `very_attacking` → Most risk in on-ball choices; highest defensive line. |

### 4.3 Dynamic role/key register

All keys again have prefix `ui.s0.`. `S` = resolved label or authored name string;
`I` = nonnegative integer; `F` = finite display float. Minute uses journey §14.4's
physics-tick helper. Speeds use the existing ladder; caps/bench counts use owner constants.
For identity below `P = (team label S, first name S, last name S, shirt I)`; a `P`
expands to four arguments, never an opaque string assembled by Unity.

| Suffix / consumer | Argument indices/types | Complete base pattern |
|---|---|---|
| `setup.ready` | 0 Mentality S | Ready to start with {0}. |
| `mentality.current` | 0 Mentality S | Current Mentality: {0} |
| `mentality.choice` | 0 Mentality S, 1 effect S | {0} — {1} |
| `scoreline` | 0 home I, 1 away I | {0} – {1} |
| `minute` | 0 minute I | Minute {0} |
| `speed` | 0 rung I | Selected speed {0}× |
| `speed_paused` | 0 rung I | Selected speed {0}× • Paused |
| `identity.player` | 0–3 P | {0}: {1} {2} — shirt {3} |
| `identity.player_keeper` | 0–3 P, 4 keeper label S | {0}: {1} {2} — shirt {3} — {4} |
| `identity.bench` | 0–3 P, 4 displayed bench I | {0}: {1} {2} — shirt {3} — bench slot {4} |
| `identity.bench_keeper` | 0–3 P, 4 displayed bench I, 5 keeper label S | {0}: {1} {2} — shirt {3} — bench slot {4} — {5} |
| `pitch.marker` | 0 short team S, 1 shirt I | {0}{1} |
| `pitch.marker_substitute` | 0 short team S, 1 shirt I | {0}{1} ↔ |
| `pitch.player_description` | 0–3 P | {0}: {1} {2} — shirt {3}. |
| `pitch.substitute_description` | 0–3 P | {0}: {1} {2} — shirt {3}. Applied substitute. |
| `pitch.keeper_description` | 0–3 P | {0}: {1} {2} — shirt {3}. Goalkeeper. |
| `pitch.keeper_substitute_description` | 0–3 P | {0}: {1} {2} — shirt {3}. Goalkeeper. Applied substitute. |
| `substitutions.used` | 0 used I, 1 cap I | Substitutions used: {0} / {1} |
| `feedback.earlier` | 0 earlier count I | Earlier change feedback ({0}) |
| `result.home` | 0 home result S | Full time — Home result: {0} |
| `statistics.incomplete_live` | 0 cutoff minute I | Statistics stopped at minute {0}. The match continues; these figures are incomplete. |
| `statistics.incomplete_final` | 0 cutoff minute I | Statistics incomplete — stopped at minute {0}. Final score remains available. |
| `statistics.partial_caption` | 0 cutoff minute I | Partial statistics — stopped at minute {0} — incomplete, not full-match totals. |
| `value.integer` | 0 statistic I | {0:D} |
| `value.decimal` | 0 statistic F | {0:F1} |
| `value.percentage` | 0 already-projected percentage F | {0:F1}% |

Outcome keys have complete sentences, not concatenated status/detail fragments.
Mentality keys take `0 Mentality S`, plus `1 minute I` only for Applied.
Home-only substitution keys take these **flat typed arguments** from the immutable
identity components captured at submission: `0 outgoing first name S`, `1 outgoing
last name S`, `2 outgoing shirt I`, `3 incoming first name S`, `4 incoming last name S`,
`5 incoming shirt I`, `6 displayed bench I`, plus `7 minute I` only for Applied.
No preformatted `identity.player` result is nested in an outcome pattern. Translators
can independently order both players' name components and every numeric field; no
repeated team prefix is needed in the home-team change context. Caches retain the
typed components and final role output without rewriting old request identities.

| Suffix | Complete base pattern |
|---|---|
| `feedback.mentality_pending` | Pending — Mentality: {0} — waiting to be applied. |
| `feedback.mentality_pending_paused` | Pending — Mentality: {0} — waiting; resume to continue. |
| `feedback.mentality_applied` | Applied — Mentality: {0} at minute {1}. |
| `feedback.mentality_refused` | Refused — Mentality: {0} — current Mentality unchanged. |
| `feedback.mentality_not_applied` | Not applied — Mentality: {0} — match ended before it could apply. |
| `feedback.mentality_send_failure` | Send failure — Mentality: {0} — request could not be sent; current Mentality unchanged. |
| `feedback.substitution_pending` | Pending — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); waiting to be applied. |
| `feedback.substitution_pending_paused` | Pending — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); waiting; resume to continue. |
| `feedback.substitution_applied` | Applied — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}) at minute {7}. |
| `feedback.substitution_refused` | Refused — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); substitution count unchanged. |
| `feedback.substitution_not_applied` | Not applied — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); match ended before it could apply. |
| `feedback.substitution_send_failure` | Send failure — {0} {1} (shirt {2}) → {3} {4} (shirt {5}, bench slot {6}); request could not be sent; substitution count unchanged. |

Base-copy example: “Pending — Ben Calder (shirt 2) → Miles West (shirt 13, bench slot 2);
waiting to be applied.” This is a planned formatting fixture, not an executed outcome.
I-Q16 must cover all six substitution outcome roles, a translation pattern that reorders
each player's first/last names and shirt independently, and the Applied minute. Build
fixtures require indices 0–6 (0–7 for Applied), allow repetition, and reject a missing
name/number index, an extra index or a numeric specifier on a name. Name strings
containing braces remain literal argument data, never a second pattern to parse.
Use the fixed invariant provider in every S0 fixture; locale-provider mapping remains later work.

Accessible names match visible controls, using the same cached role output. Noninteractive
pitch descriptions use the complete name/shirt/cue roles; glyph-only status is forbidden.
This defines semantic text inputs, not an unimplemented Unity screen-reader bridge.
Presentation notifications, if consumed by the real binding, use these same outcome/state
roles; do not ship the prototype's separate unregistered announcement sentences.

### 4.4 Formatting lifetime and shipped-content proof

Resolve/cache static labels once per shell. Each presenter caches dynamic labels by
role and typed argument values; update only on actual argument changes. Formatting,
boxing and argument-array construction stay off unchanged render frames. Match teardown
clears identities, outcomes, match/report format caches and disclosure/focus state;
shell teardown additionally discards menu/setup caches and the formatting context.
No context replacement or revision signal is assumed. Do not retain one match's names
or outcome labels in the next match.

S0 client build lint validates the in-memory/compiled base catalogue inputs and the
published representation if packaging changes them. I-Q16 records matching validation,
package and loaded-content SHA-256 identities through the actual shell composition path.
For compiled content, identify the loaded package/build and canonical role/key/pattern
manifest derived from that package; a separate synthetic in-memory fixture is not proof
of which content shipped. This allocates client evidence tooling, not a new L2 external
loader. Separate negative fixtures prove malformed authored locale rejection, missing
base rejection and selected-key absence with KD-5 fallback. The client's S0 coverage
proof cannot close #49 ERR-049-005 for unrelated admitted static keys.

## 5. Consumed landing sequence and review matrix

| Landing | Producer + real consumer | Required evidence / boundary |
|---|---|---|
| Gate I contract/reference PR (this draft) | This contract → journey component/QA mapping and implementer review | Review all three contracts and reference deltas below; record the final verdict in journey/protocol/plan/live tracking. No I PASS inferred here. |
| #49 L2 | Generic immutable in-memory catalogue + `ILocalizer` implementation → its seam behavior tests; P5b later consumes the named implementation | Owning plan §6 and approved #49 FR-LC-007–011; KD-5; full ERR-049-005 construction coverage or owner-approved terminal-result fix and executable proof. No client schema in core, external files, locale release, a11y store or live locale replacement. |
| #470 shell foundation after I release | Existing shell foundation decisions → Unity roots | Refresh onto then-current main, close tracking, fresh CI and pinned Unity 6000.4.9f1 compile on PR head before merge. Foundation does not claim complete S0 screens/localization. |
| P5b lifecycle/identity slice | Authored fixture + engine observation/frame cue + session roster descriptor → setup/session, pitch and chooser/feedback identity projections | The shell coordinator consumes `MatchSessionLifecycle.CreateSession` / `Current` / `ClearSession`; the pitch renderer `MatchClientBehaviour` gains the TO BUILD `Attach(MatchSession)` / detach binding (journey §14.2). Remove internal/opt-in demo ownership. Land new identity producers with consumers, boot/substitution/reset tests and pinned compile. Do not introduce another unconsumed lifecycle prerequisite before the authorized binding. |
| P5b copy/scale/screens slice | L2 localizer + admitted S0 content/provider/configuration → formatter, all four presenters and Unity bindings | Full §4 schema/build admission/cache/package proof, §3 reflow; live command and analytics adapters from journey §§14.5–6. May combine with the preceding slice; localized screens wait for L2. |
| Gate J / remaining B8/B9b/B10 acceptance | Real client → I-Q01–19, actual host/cert evidence | Compile on exact PR heads before merge; actual controls, keyboard, identity, observer fault, all scales and glyph fallback, repeat match, cert evidence. CI shim/browser images do not replace this lane. |

### 5.1 Affected H/reference reconciliation

The following is the proposed **implementation delta** from approved H v0.2 at
`a1044d5`. Approved sources, 23 image pairs, original walkthrough and actual G/H
approval records are preserved byte-for-byte. This draft changes no image approval.

| Approved reference | Contract reconciliation / final-review input |
|---|---|
| H substitution dialog and live Applied example, shirt 4 → 14 | Layout, staging and outcome semantics retained. Replace illustrative shirt-only options with §2's actual first/last name, authored shirt and bench identity. Incoming shirts are player metadata; a particular bench index is not automatically shirt 14. Review the longer label treatment before final I verdict; I-Q09 proves actual mapping at J. |
| H pitch markers / substitute legend | Keep compact H/A shirt labels and explicit substitute cue. Resolve shirt by current frame player id; complete name/shirt/cue text uses §4, without slot-number fallback. New occupant snaps as §2.2 specifies. |
| H long-name/pseudo and 200% stress views | Remain geometry references. The admitted S0 range is now 100–200%; real player labels, provider and font metrics consume §§2–4. Review the label/reflow delta; I-Q16/17 supplies runtime proof. |
| H synthetic analytics and omitted Substitutions row | Production adds the already-allocated #37 Substitutions row and drops prototype/capture qualifiers; frame score and Goals recorded remain distinct, faults retain explicit cutoff wording (journey §14.6). |
| G/H no-name and unallocated-maximum notes | Historical prototype limitations, superseded for the S0 target by the October 3 choices and this proposed contract. They are not a reason to defer S0 names to S1 or certify Unity scaling from browser exports. |

**Final review due:** check these deltas with the complete journey §§14.2–8, verify
that an implementer needs no additional product decision, and record the verdict and
reviewed commit. Explicit owner acceptance of §2.1's proposed 36 synthetic names is
also due; approval of the distinct-squad direction does not approve this particular list.
If a delta requires revised visual evidence, return only its affected
references for review and retain the earlier approval pins. S0-I-001/002/003 are now
defined proposals; none is closed by authoring this file. Gate I stays IN PROGRESS,
#470 stays blocked, and all P5b QA stays PLANNED until its appropriate lane runs.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | October 3, 2026 | Initial S0 binding-contract review draft against main 3429fafd: authored distinct-squad fixture, frame identity/bench/history publication, fixed 100–200% scale/reflow, static/dynamic typed copy register, fixed number-format context, L2/client consumption and packaging evidence, affected reference matrix. No source, approved assets, implementation proof or gate pass. |
| 0.2 | October 3, 2026 | PR #478 review corrections: flat typed substitution name/shirt/bench/minute arguments and planned I-Q16 reordered/negative build fixtures; one invariant S0 number-format provider; Attach assigned to the pitch renderer while the shell consumes lifecycle methods. Explicit acceptance of the proposed name list remains due. I IN PROGRESS; #470 blocked; no source, approved evidence or gate-pass change. |
