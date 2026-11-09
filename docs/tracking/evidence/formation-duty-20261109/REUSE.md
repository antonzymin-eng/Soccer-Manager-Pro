# Base reference reuse argument

Created November 9, 2026; source pinned to d4b026aa951432d342217314b8c4d49b53a19e7d.
`base-reuse-search.txt` retains the production-only repository search, including
construction, copies, whole-tactic consumers and `.Formation` reads.

The runner calls only MatchEngine constructor, EnableGkHeading, pre-match tactic
setters and RunTick. No file config binding, preset file validation, client command
queue/acknowledgment, quick-simulation or season path executes. Both managers are
asserted Human (the constructor's default), so ManagerAdaptation cannot issue presets.

The engine copies pending to active at the existing stride. DecisionTree translation
reads Mentality/Passing/Pressing and per-player fields; #12 reads widths and the
three optional dials; #13 reads line/press triggers; #14 reads line/marking/trap;
#15 reads mentality/transition/focus; #11 reads distribution. None reads Formation
on this pinned base. Constructor, FillPositioningSnapshot, ConfigureSquads, restore
selection and SquadRating use STAGE0_FORMATION. WriteTeamTactic alone reads the
stored requested Formation in the reachable simulation lane. It writes two int32s
(active/pending) per team; the pending value is present from frame 1 and active
becomes that value on frame 6. ReadTeamTactic belongs to restore, which this capture
does not execute. The input is valid, not a new enum ordinal.

The search includes consumer-owned TacticTranslation APIs, TeamTactic construction
and copying by UI/presets, TacticPresetFileLoader's complete-tactic `.Equals` against
Balanced, and ClientChangeRequests' complete-tactic `.Equals` for acknowledgment.
Those differences make generalized reuse unsound. This direct-engine proof excludes
those paths by construction; it does not claim formation is globally inert.

Conditional acceptance also requires the 36 six-seed 1000-frame base probes to match
all other aligned world bytes, trajectories and ordered ledgers while independently
validating each raw digest chain. They prove only the measured prefix. Full reference
reuse relies separately on this source nonconsumption argument and owner approval.
C5–C8 cannot reuse C0: existing non-positioning Duty consumers are active on base.
Retain and label real C0 traces; no full base C1–C4/C9 digest is claimed.
