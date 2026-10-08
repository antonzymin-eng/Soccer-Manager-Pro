// File:     src/client-app/S0ScreenPresenter.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 journey §§14.3–14.8, binding contracts §§2–4, Code Standards #20
// Purpose:  Consumed four-screen presentation, staging, enablement, copy, statistics and disclosure state.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Profiling;
using TacticalDirector.DeterministicSim;
using TacticalDirector.MatchAnalytics;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Client-thread state for all four screens; Unity binds these decisions and forwards actions.</summary>
    public sealed class S0ScreenPresenter
    {
        private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("S0ScreenPresenter.Refresh");
        private readonly ClientMatchCoordinator _coordinator;
        private ClientMatchContext _context;
        private ulong _lastFrameTick;
        private ulong _statisticsTick;
        private bool _hasStatistics;
        private bool _lastPaused;
        private int _lastRequestRevision = -1;
        private readonly List<PlayerChoice> _outgoing = new List<PlayerChoice>();
        private readonly List<PlayerChoice> _incoming = new List<PlayerChoice>();
        private readonly List<string> _feedback = new List<string>();
        private readonly List<StatisticRow> _statistics = new List<StatisticRow>();
        private int _outgoingPlayerId = -1;
        private int _incomingPlayerId = -1;
        private int _speedIndex = PlaybackSpeedLadder.RealTimeIndex;
        private MatchAnalyticsPublication _analytics;
        private MatchFrameView _frame;
        private bool _isReport;
        private readonly string[] _markerText = new string[MatchEngineConstants.SQUAD_SIZE];
        private readonly string[] _playerDescriptions = new string[MatchEngineConstants.SQUAD_SIZE];
        private readonly LiveAgentCue[] _lastCues = new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE];
        private bool _hasChoices;
        private int _scoreHome = -1, _scoreAway = -1, _minute = -1, _usedSubs = -1;
        /// <summary>The seven explicit supported choices; no enum ordinal arithmetic.</summary>
        public static ReadOnlyCollection<Mentality> Mentalities { get; } = Array.AsReadOnly(new[] { Mentality.VeryDefensive, Mentality.Defensive, Mentality.Cautious, Mentality.Balanced, Mentality.Positive, Mentality.Attacking, Mentality.VeryAttacking });

        /// <summary>The staging dialog, separate from the fixed navigation graph.</summary>
        public enum DialogKind
        {
            /// <summary>No open draft.</summary>
            None,
            /// <summary>Home Mentality draft.</summary>
            Mentality,
            /// <summary>Home substitution draft.</summary>
            Substitution
        }

        /// <summary>One choice with a command index and identity resolved from the same accepted frame.</summary>
        public readonly struct PlayerChoice
        {
            /// <summary>Zero-based engine pitch slot or bench index.</summary>
            public readonly int Index;
            /// <summary>Resolved immutable identity.</summary>
            public readonly MatchPlayerIdentity Player;
            /// <summary>Complete localized name/shirt label.</summary>
            public readonly string Label;
            internal PlayerChoice(int index, in MatchPlayerIdentity player, string label)
            {
                Index = index;
                Player = player;
                Label = label;
            }
        }

        /// <summary>One localized comparison row from one analytics result.</summary>
        public readonly struct StatisticRow
        {
            /// <summary>Localized statistic name.</summary>
            public readonly string Label;
            /// <summary>Home value.</summary>
            public readonly string Home;
            /// <summary>Away value.</summary>
            public readonly string Away;
            internal StatisticRow(string label, string home, string away)
            {
                Label = label;
                Home = home;
                Away = away;
            }
        }

        /// <summary>Composes one fixed shell formatting/scale context with the validated coordinator.</summary>
        public S0ScreenPresenter(ClientMatchCoordinator coordinator, S0TextFormatter text, S0PresentationConfiguration configuration)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Outgoing = _outgoing.AsReadOnly();
            Incoming = _incoming.AsReadOnly();
            Feedback = _feedback.AsReadOnly();
            Statistics = _statistics.AsReadOnly();
            ResetMatch();
        }

        /// <summary>Fixed admitted formatting context.</summary>
        public S0TextFormatter Text { get; }
        /// <summary>Fixed presentation scale.</summary>
        public S0PresentationConfiguration Configuration { get; }
        /// <summary>Current named coordinator screen.</summary>
        public ScreenId Screen => _coordinator.Flow.Current;
        /// <summary>Current dialog, cancelled at full time.</summary>
        public DialogKind Dialog { get; private set; }
        /// <summary>Requested draft; never the last Applied Mentality.</summary>
        public Mentality RequestedMentality { get; private set; }
        /// <summary>Last Applied Mentality or authored setup choice.</summary>
        public Mentality CurrentMentality => _context?.Changes.CurrentMentality ?? _coordinator.DraftMentality;
        /// <summary>Whether a dialog has one currently legal substitution pair.</summary>
        public bool CanSubmitSubstitution { get; private set; }
        /// <summary>Selected zero-based outgoing slot, or -1.</summary>
        public int SelectedOutgoing { get; private set; } = -1;
        /// <summary>Selected zero-based bench origin, or -1.</summary>
        public int SelectedIncoming { get; private set; } = -1;
        /// <summary>Frame/owner/pending-based tactical availability.</summary>
        public bool CanChangeMentality { get; private set; }
        /// <summary>Frame/owner/pending-based substitution availability.</summary>
        public bool CanSubstitute { get; private set; }
        /// <summary>Playback availability from MatchControlAvailability.</summary>
        public bool CanPause { get; private set; }
        /// <summary>Playback plus ladder endpoint decision.</summary>
        public bool CanSlower { get; private set; }
        /// <summary>Playback plus ladder endpoint decision.</summary>
        public bool CanFaster { get; private set; }
        /// <summary>Report only after the coordinator completed its end barrier.</summary>
        public bool CanReport => _coordinator.Report.Project().IsAvailable;
        /// <summary>Current accepted ended state.</summary>
        public bool IsFullTime => !_frame.IsEmpty && _frame.MatchEnded;
        /// <summary>Streamer playback state; independent of the speed rung.</summary>
        public bool IsPaused => _context != null && _context.Session.Streamer.IsPaused;
        /// <summary>Current accepted frame snapshot.</summary>
        public MatchFrameView Frame => _frame;
        /// <summary>Initially closed and retained at full time.</summary>
        public bool IsStatisticsOpen { get; private set; }
        /// <summary>Independent report partial disclosure, initially closed.</summary>
        public bool IsPartialReportOpen { get; private set; }
        /// <summary>Retained history disclosure.</summary>
        public bool IsEarlierFeedbackOpen { get; private set; }
        /// <summary>Real statistics availability, never fabricated zeros.</summary>
        public bool HasStatistics => _analytics.Result.HasValue;
        /// <summary>Observer failure remains visible outside every disclosure.</summary>
        public bool IsStatisticsIncomplete => _analytics.IsIncomplete;
        /// <summary>Legal outgoing slots, including an unreplaced keeper.</summary>
        public ReadOnlyCollection<PlayerChoice> Outgoing { get; }
        /// <summary>Unused bench origins with one-based displayed labels.</summary>
        public ReadOnlyCollection<PlayerChoice> Incoming { get; }
        /// <summary>Persistent chronological localized request labels.</summary>
        public ReadOnlyCollection<string> Feedback { get; }
        /// <summary>Independent statistics from one synchronized publication.</summary>
        public ReadOnlyCollection<StatisticRow> Statistics { get; }
        /// <summary>Withheld before the first frame.</summary>
        public string Score { get; private set; } = "";
        /// <summary>Physics-tick minute, withheld before the first frame.</summary>
        public string Clock { get; private set; } = "";
        /// <summary>Frame-owned period label.</summary>
        public string Period { get; private set; } = "";
        /// <summary>Selected speed, with separate paused wording.</summary>
        public string Speed { get; private set; } = "";
        /// <summary>Incomplete or ended-disclosure notice, always outside the table.</summary>
        public string StatisticsNotice { get; private set; } = "";
        /// <summary>Caption explicitly marks incomplete figures.</summary>
        public string StatisticsCaption { get; private set; } = "";
        /// <summary>Frame-owned home result, never analytics-derived.</summary>
        public string Result { get; private set; } = "";
        /// <summary>Shared first-frame/full-time reason.</summary>
        public string ControlReason { get; private set; } = "";
        /// <summary>Endpoint explanation retained outside unavailable buttons.</summary>
        public string SlowerReason { get; private set; } = "";
        /// <summary>Endpoint explanation retained outside unavailable buttons.</summary>
        public string FasterReason { get; private set; } = "";
        /// <summary>Persistent request/limit/pair reason.</summary>
        public string MentalityReason { get; private set; } = "";
        /// <summary>Persistent request/limit/pair reason.</summary>
        public string SubstitutionReasonText { get; private set; } = "";
        /// <summary>Revalidation failure in the chooser, cleared on a valid new choice.</summary>
        public string ChoiceNotice { get; private set; } = "";
        /// <summary>Authoritative accepted-frame substitutions used.</summary>
        public string SubstitutionsUsed { get; private set; } = "";
        /// <summary>Presentation revision; bindings retain their roots and focus between revisions.</summary>
        public int Revision { get; private set; }

        /// <summary>Physics elapsed whole minutes, also used for feedback and statistics cutoff.</summary>
        public static int Minute(ulong tick) => checked((int)(tick / (DeterministicSimConstants.PHYSICS_TICK_HZ * 60UL)));
        /// <summary>Exhaustive enum-to-authored role mapping.</summary>
        public static string MentalitySuffix(Mentality mentality)
        {
            switch (mentality)
            {
                case Mentality.VeryDefensive:
                    return "very_defensive";
                case Mentality.Defensive:
                    return "defensive";
                case Mentality.Cautious:
                    return "cautious";
                case Mentality.Balanced:
                    return "balanced";
                case Mentality.Positive:
                    return "positive";
                case Mentality.Attacking:
                    return "attacking";
                case Mentality.VeryAttacking:
                    return "very_attacking";
                default:
                    throw new ArgumentOutOfRangeException(nameof(mentality));
            }
        }

        /// <summary>Resolved Mentality label.</summary>
        public string MentalityLabel(Mentality value) => Text.Label("mentality." + MentalitySuffix(value));
        /// <summary>Complete choice and consequence; cached by its stable role cell.</summary>
        public string MentalityChoice(Mentality value) => Text.Format("choice." + MentalitySuffix(value), "mentality.choice", MentalityLabel(value), Text.Label("effect." + MentalitySuffix(value)));
        /// <summary>Fresh Balanced setup via the named graph.</summary>
        public void OpenSetup()
        {
            _coordinator.OpenTacticsSetup();
            Revision++;
        }

        /// <summary>Setup-only selection.</summary>
        public void SelectSetupMentality(Mentality value)
        {
            _coordinator.SelectMentality(value);
            Revision++;
        }

        /// <summary>Named Cancel, with no session constructed.</summary>
        public void CancelSetup()
        {
            _coordinator.CancelTacticsSetup();
            Revision++;
        }

        /// <summary>Exactly one coordinator Start transaction.</summary>
        public void StartMatch()
        {
            _coordinator.StartMatch();
            Refresh();
        }

        /// <summary>Explicit report navigation, freezing the existing frame/report data.</summary>
        public void ShowReport()
        {
            _coordinator.ShowPostMatchReport();
            _isReport = true;
            IsPartialReportOpen = false;
            UpdateStatistics(_coordinator.Report.Project().Analytics);
            Revision++;
        }

        /// <summary>Named teardown/return; old match caches, choices and outcomes are discarded.</summary>
        public void ReturnToMenu()
        {
            _coordinator.ReturnToMainMenu();
            ResetMatch();
            Revision++;
        }

        /// <summary>Open a presentation-only Mentality draft.</summary>
        public void OpenMentality()
        {
            Refresh();
            if (!CanChangeMentality)
                throw new InvalidOperationException("Mentality control unavailable.");
            Dialog = DialogKind.Mentality;
            RequestedMentality = CurrentMentality;
            Revision++;
        }

        /// <summary>Stage only; no dispatch.</summary>
        public void SelectRequestedMentality(Mentality value)
        {
            if (Dialog != DialogKind.Mentality)
                throw new InvalidOperationException("No Mentality draft.");
            MentalitySuffix(value);
            RequestedMentality = value;
            Revision++;
        }

        /// <summary>Open current legal choices without predicting any substitution.</summary>
        public void OpenSubstitution()
        {
            Refresh();
            if (!CanSubstitute)
                throw new InvalidOperationException("Substitution control unavailable.");
            Dialog = DialogKind.Substitution;
            SelectedOutgoing = SelectedIncoming = -1;
            _outgoingPlayerId = _incomingPlayerId = -1;
            CanSubmitSubstitution = false;
            ChoiceNotice = "";
            Revision++;
        }

        /// <summary>Choose an outgoing engine slot and capture its current identity for revalidation.</summary>
        public void SelectOutgoing(int index)
        {
            RequireSubstitution();
            PlayerChoice choice = Find(_outgoing, index);
            SelectedOutgoing = index;
            _outgoingPlayerId = choice.Player.PlayerId;
            UpdatePair();
        }

        /// <summary>Choose an unused bench origin.</summary>
        public void SelectIncoming(int index)
        {
            RequireSubstitution();
            PlayerChoice choice = Find(_incoming, index);
            SelectedIncoming = index;
            _incomingPlayerId = choice.Player.PlayerId;
            UpdatePair();
        }

        /// <summary>Cancel/Escape discard the draft without enqueuing anything.</summary>
        public void CancelDialog()
        {
            Dialog = DialogKind.None;
            ChoiceNotice = "";
            CanSubmitSubstitution = false;
            Revision++;
        }

        /// <summary>Register before exactly one dispatch after consuming current suffixes.</summary>
        public ClientChangeRecord SubmitMentality()
        {
            Refresh();
            if (Dialog != DialogKind.Mentality || !CanChangeMentality)
                throw new InvalidOperationException("Mentality draft unavailable.");
            TeamTactic tactic = _context.Changes.WithMentality(RequestedMentality);
            ManagerCommand command = ManagerCommand.SetTeamTactic(0, tactic);
            ManagerIntent intent = ManagerIntent.SetTeamTactic(0, tactic);
            return Submit(command, intent, default, default);
        }

        /// <summary>Returns null when the staged identity/pair changed; requests no command in that case.</summary>
        public ClientChangeRecord SubmitSubstitution()
        {
            Refresh();
            RequireSubstitution();
            if (!CanSubstitute || !PairIsCurrent())
            {
                ChoiceNotice = Text.Label("reason.choice_changed");
                CanSubmitSubstitution = false;
                Revision++;
                return null;
            }

            PlayerChoice outgoing = Find(_outgoing, SelectedOutgoing);
            PlayerChoice incoming = Find(_incoming, SelectedIncoming);
            ManagerCommand command = ManagerCommand.Substitute(0, SelectedOutgoing, SelectedIncoming, SubstitutionReason.Tactical);
            ManagerIntent intent = ManagerIntent.Substitute(0, SelectedOutgoing, SelectedIncoming, SubstitutionReason.Tactical);
            return Submit(command, intent, outgoing.Player, incoming.Player);
        }

        private ClientChangeRecord Submit(ManagerCommand command, ManagerIntent intent, MatchPlayerIdentity outgoing, MatchPlayerIdentity incoming)
        {
            // Refresh consumed all current suffixes immediately before registration on this thread.
            ClientChangeRecord row = _context.Changes.Submit(in command, in intent, in outgoing, in incoming, _coordinator.MatchView);
            CancelDialog();
            Refresh();
            return row;
        }

        /// <summary>Playback only; a paused request stays Pending until a real drain after Resume.</summary>
        public void TogglePause()
        {
            Refresh();
            if (!CanPause)
                throw new InvalidOperationException("Playback unavailable.");
            if (IsPaused)
                _context.Session.Streamer.Resume();
            else
                _context.Session.Streamer.Pause();
            Refresh();
        }

        /// <summary>Owner ladder step, never an invented multiplier.</summary>
        public void Faster()
        {
            Refresh();
            if (!CanFaster)
                return;
            _speedIndex = PlaybackSpeedLadder.StepFaster(_speedIndex);
            SetSpeed();
        }

        /// <summary>Owner ladder step, clamped at real time.</summary>
        public void Slower()
        {
            Refresh();
            if (!CanSlower)
                return;
            _speedIndex = PlaybackSpeedLadder.StepSlower(_speedIndex);
            SetSpeed();
        }

        private void SetSpeed()
        {
            _context.Session.Streamer.SetSpeedMultiplier(PlaybackSpeedLadder.MultiplierAt(_speedIndex));
            UpdatePlayback();
            Revision++;
        }

        /// <summary>Live disclosure changes no observer sampling; unavailable at full time.</summary>
        public void ToggleStatistics()
        {
            Refresh();
            if (_frame.IsEmpty || IsFullTime)
                return;
            IsStatisticsOpen = !IsStatisticsOpen;
            Revision++;
        }

        /// <summary>Report-only incomplete partials; Return always remains outside this disclosure.</summary>
        public void TogglePartialReport()
        {
            if (!_isReport || !IsStatisticsIncomplete || !HasStatistics)
                return;
            IsPartialReportOpen = !IsPartialReportOpen;
            Revision++;
        }

        /// <summary>Retains disclosure through new ticks and outcomes.</summary>
        public void ToggleEarlierFeedback()
        {
            IsEarlierFeedbackOpen = !IsEarlierFeedbackOpen;
            Revision++;
        }

        /// <summary>Pure observation refresh; never TickOnce, ServiceOnce or an engine getter.</summary>
        public void Refresh()
        {
            using (RefreshMarker.Auto())
            {
                ClientMatchContext next = _coordinator.Context;
                if (!ReferenceEquals(next, _context))
                {
                    ResetMatch();
                    _context = next;
                    if (next != null)
                    {
                        _speedIndex = PlaybackSpeedLadder.RealTimeIndex;
                        UpdatePlayback();
                    }

                    Revision++;
                }

                if (_context == null)
                    return;
                // The end barrier already settled these frozen logs. Otherwise reads are separate snapshots.
                if (_context.IsFullTime)
                {
                    ClientMatchReport report = _coordinator.Report.Project();
                    _context.Changes.Reconcile(report.Applied, report.Refused, true);
                }
                else
                    _context.Changes.Reconcile(_context.Session.Driver.Log, _context.Session.Driver.FailedCommands, false);
                if (_context.TryGetLatestFrame(out LiveMatchFrame frame) && (_frame.IsEmpty || frame.Tick != _lastFrameTick))
                {
                    _frame = _coordinator.MatchView.Project();
                    _lastFrameTick = frame.Tick;
                    UpdateFrame(in frame);
                    if (_context.IsFullTime || !_hasStatistics || frame.Tick - _statisticsTick >= DeterministicSimConstants.PHYSICS_TICK_HZ || (!_analytics.IsIncomplete && _context.Session.Streamer.PostTickObserverFault != null))
                    {
                        UpdateStatistics(_context.IsFullTime ? _coordinator.Report.Project().Analytics : _context.Analytics.Publish());
                        _statisticsTick = frame.Tick;
                        _hasStatistics = true;
                    }

                    Revision++;
                }

                if (_lastPaused != IsPaused)
                {
                    _lastPaused = IsPaused;
                    UpdatePlayback();
                    _lastRequestRevision = -1;
                    Revision++;
                }

                if (_lastRequestRevision != _context.Changes.Revision)
                {
                    _lastRequestRevision = _context.Changes.Revision;
                    UpdateFeedback();
                    UpdateAvailability();
                    Revision++;
                }
            }
        }

        private void UpdateFrame(in LiveMatchFrame frame)
        {
            if (_scoreHome != frame.Score.Home || _scoreAway != frame.Score.Away)
            {
                _scoreHome = frame.Score.Home;
                _scoreAway = frame.Score.Away;
                Score = Text.Format("score", "scoreline", _scoreHome, _scoreAway);
            }

            int minute = Minute(frame.Tick);
            if (_minute != minute)
            {
                _minute = minute;
                Clock = Text.Format("clock", "minute", minute);
            }

            Period = Text.Label(frame.MatchEnded ? "state.full_time" : frame.Period == MatchPeriod.FirstHalf ? "state.first_half" : "state.second_half");
            if (_usedSubs != frame.SubstitutionsUsed[0])
            {
                _usedSubs = frame.SubstitutionsUsed[0];
                SubstitutionsUsed = Text.Format("subs.used", "substitutions.used", _usedSubs, MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM);
            }

            UpdateChoices(in frame);
            UpdateAvailability();
            if (frame.MatchEnded)
            {
                CancelDialog();
                string outcome = frame.Score.Home > frame.Score.Away ? "win" : frame.Score.Home < frame.Score.Away ? "loss" : "draw";
                Result = Text.Format("result", "result.home", Text.Label("result." + outcome));
            }
        }

        /// <summary>Compact marker already cached from the currently accepted player identity and cues.</summary>
        public string PitchMarker(int agentId) => _markerText[agentId] ?? "";
        /// <summary>Complete semantic name/shirt/cue text from that same accepted frame.</summary>
        public string PitchPlayerDescription(int agentId) => _playerDescriptions[agentId] ?? "";
        private void UpdateChoices(in LiveMatchFrame frame)
        {
            bool changed = !_hasChoices;
            for (int i = 0; i < frame.AgentCues.Length; i++)
            {
                LiveAgentCue a = frame.AgentCues[i], b = _lastCues[i];
                changed |= a.PlayerId != b.PlayerId || a.IsSentOff != b.IsSentOff || a.IsGoalkeeper != b.IsGoalkeeper || a.BenchSlot != b.BenchSlot;
                _lastCues[i] = a;
            }

            if (!changed)
                return;
            _hasChoices = true;
            for (int i = 0; i < frame.AgentCues.Length; i++)
            {
                LiveAgentCue cue = frame.AgentCues[i];
                MatchPlayerIdentity p = _context.Identity.Resolve(in frame, i);
                _markerText[i] = Text.Format("pitch.marker." + i, cue.IsSubstitute ? "pitch.marker_substitute" : "pitch.marker", Text.Label(p.TeamId == 0 ? "team.home_short" : "team.away_short"), p.ShirtNumber);
                string role = cue.IsGoalkeeper ? cue.IsSubstitute ? "pitch.keeper_substitute_description" : "pitch.keeper_description" : cue.IsSubstitute ? "pitch.substitute_description" : "pitch.player_description";
                _playerDescriptions[i] = Text.Format("pitch.description." + i, role, Text.Label(p.TeamId == 0 ? "team.home" : "team.away"), p.FirstName, p.LastName, p.ShirtNumber);
            }

            _outgoing.Clear();
            _incoming.Clear();
            int slot = 0;
            for (int i = 0; i < _context.Identity.Roster.AgentCount; i++)
            {
                if (_context.Identity.Roster.TeamId(i) != 0)
                    continue;
                LiveAgentCue cue = frame.AgentCues[i];
                if (!cue.IsSentOff && !cue.IsSubstitute)
                {
                    MatchPlayerIdentity p = _context.Identity.Resolve(in frame, i);
                    string label = cue.IsGoalkeeper ? Text.Format("out." + slot, "identity.player_keeper", Text.Label("team.home"), p.FirstName, p.LastName, p.ShirtNumber, Text.Label("identity.goalkeeper")) : Text.Format("out." + slot, "identity.player", Text.Label("team.home"), p.FirstName, p.LastName, p.ShirtNumber);
                    _outgoing.Add(new PlayerChoice(slot, in p, label));
                }

                slot++;
            }

            for (int b = 0; b < MatchEngineConstants.SUBSTITUTES_PER_TEAM; b++)
            {
                if (_context.Identity.IsBenchUsed(in frame, 0, b))
                    continue;
                MatchPlayerIdentity p = _context.Identity.Bench(0, b);
                bool keeper = _coordinator.Fixture.IsAuthoredGoalkeeper(p.PlayerId);
                string label = keeper ? Text.Format("bench." + b, "identity.bench_keeper", Text.Label("team.home"), p.FirstName, p.LastName, p.ShirtNumber, b + 1, Text.Label("identity.goalkeeper")) : Text.Format("bench." + b, "identity.bench", Text.Label("team.home"), p.FirstName, p.LastName, p.ShirtNumber, b + 1);
                _incoming.Add(new PlayerChoice(b, in p, label));
            }

            if (Dialog == DialogKind.Substitution && !PairIsCurrent())
                CanSubmitSubstitution = false;
        }

        private void UpdateAvailability()
        {
            MatchControlAvailability a = _frame.IsEmpty ? MatchControlAvailability.AwaitingFirstFrame : IsFullTime ? MatchControlAvailability.FullTime : MatchControlAvailability.Live;
            ControlReason = a.LockReason == MatchControlLockReason.AwaitingFirstFrame ? Text.Label("reason.waiting") : a.LockReason == MatchControlLockReason.MatchEnded ? Text.Label("reason.ended") : "";
            CanPause = a.PlaybackControlsEnabled;
            CanSlower = CanPause && _speedIndex != PlaybackSpeedLadder.SlowestIndex;
            CanFaster = CanPause && _speedIndex != PlaybackSpeedLadder.FastestIndex;
            SlowerReason = CanPause && !CanSlower ? Text.Label("reason.slowest") : "";
            FasterReason = CanPause && !CanFaster ? Text.Label("reason.fastest") : "";
            bool capped = !_frame.IsEmpty && _frame.SubstitutionsUsed[0] >= MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM;
            CanChangeMentality = a.TacticalInputEnabled && !_context.Changes.IsMentalityPending;
            CanSubstitute = a.SubstitutionEnabled && !_context.Changes.IsSubstitutionPending && !capped && _outgoing.Count != 0 && _incoming.Count != 0;
            MentalityReason = _context.Changes.IsMentalityPending ? Text.Label("reason.mentality_pending") : "";
            SubstitutionReasonText = _context.Changes.IsSubstitutionPending ? Text.Label("reason.substitution_pending") : capped ? Text.Label("reason.substitution_cap") : a.SubstitutionEnabled && (_outgoing.Count == 0 || _incoming.Count == 0) ? Text.Label("reason.no_pair") : "";
        }

        private void UpdatePlayback()
        {
            Speed = Text.Format("speed", IsPaused ? "speed_paused" : "speed", checked((int)PlaybackSpeedLadder.MultiplierAt(_speedIndex)));
            if (_context != null)
                UpdateAvailability();
        }

        private void UpdateFeedback()
        {
            _feedback.Clear();
            for (int i = 0; i < _context.Changes.Records.Count; i++)
            {
                ClientChangeRecord row = _context.Changes.Records[i];
                string suffix;
                switch (row.Status)
                {
                    case ClientChangeStatus.Pending:
                        suffix = IsPaused && !IsFullTime ? "pending_paused" : "pending";
                        break;
                    case ClientChangeStatus.Applied:
                        suffix = "applied";
                        break;
                    case ClientChangeStatus.Refused:
                        suffix = "refused";
                        break;
                    case ClientChangeStatus.NotApplied:
                        suffix = "not_applied";
                        break;
                    case ClientChangeStatus.SendFailure:
                        suffix = "send_failure";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                string cell = "feedback." + i;
                if (row.Command.Kind == ManagerCommandKind.SetTeamTactic)
                    _feedback.Add(row.Status == ClientChangeStatus.Applied ? Text.Format(cell, "feedback.mentality_" + suffix, MentalityLabel(row.Command.NewTeamTactic.Mentality), Minute(row.OutcomeTick)) : Text.Format(cell, "feedback.mentality_" + suffix, MentalityLabel(row.Command.NewTeamTactic.Mentality)));
                else
                {
                    MatchPlayerIdentity o = row.Outgoing, n = row.Incoming;
                    _feedback.Add(row.Status == ClientChangeStatus.Applied ? Text.Format(cell, "feedback.substitution_" + suffix, o.FirstName, o.LastName, o.ShirtNumber, n.FirstName, n.LastName, n.ShirtNumber, row.Command.BenchIndex + 1, Minute(row.OutcomeTick)) : Text.Format(cell, "feedback.substitution_" + suffix, o.FirstName, o.LastName, o.ShirtNumber, n.FirstName, n.LastName, n.ShirtNumber, row.Command.BenchIndex + 1));
                }
            }
        }

        private void UpdateStatistics(MatchAnalyticsPublication publication)
        {
            _analytics = publication;
            _statistics.Clear();
            StatisticsNotice = publication.IsIncomplete ? Text.Format("stats.notice", _isReport || IsFullTime ? "statistics.incomplete_final" : "statistics.incomplete_live", Minute(publication.ResultThroughTick)) : IsFullTime && !_isReport ? Text.Label(IsStatisticsOpen ? "statistics.retained" : "statistics.closed") : "";
            StatisticsCaption = publication.IsIncomplete ? Text.Format("stats.caption", "statistics.partial_caption", Minute(publication.ResultThroughTick)) : Text.Label("statistics.caption");
            if (!publication.Result.HasValue)
                return;
            MatchAnalyticsResult r = publication.Result.Value;
            AddInteger("goals", r.Home.Goals, r.Away.Goals);
            AddFloat("possession", r.Home.PossessionSharePercent, r.Away.PossessionSharePercent, true);
            AddFloat("territory", r.HomeAdvanced.TerritorialPercent, r.AwayAdvanced.TerritorialPercent, true);
            AddInteger("fouls", r.Home.Fouls, r.Away.Fouls);
            AddInteger("yellow", r.Home.YellowCards, r.Away.YellowCards);
            AddInteger("red", r.Home.RedCards, r.Away.RedCards);
            AddInteger("offsides", r.Home.Offsides, r.Away.Offsides);
            AddInteger("corners", r.Home.Corners, r.Away.Corners);
            AddInteger("throw_ins", r.Home.ThrowIns, r.Away.ThrowIns);
            AddInteger("goal_kicks", r.Home.GoalKicks, r.Away.GoalKicks);
            AddInteger("substitutions", r.Home.Substitutions, r.Away.Substitutions);
            if (r.HomeAdvanced.LiveXgAvailable && r.AwayAdvanced.LiveXgAvailable)
                AddFloat("xg", r.HomeAdvanced.XgSum, r.AwayAdvanced.XgSum, false);
        }

        private void AddInteger(string role, int home, int away) => _statistics.Add(new StatisticRow(Text.Label("statistics." + role), Text.Format("stats.h." + role, "value.integer", home), Text.Format("stats.a." + role, "value.integer", away)));
        private void AddFloat(string role, float home, float away, bool percentage) => _statistics.Add(new StatisticRow(Text.Label("statistics." + role), Text.Format("stats.h." + role, percentage ? "value.percentage" : "value.decimal", home), Text.Format("stats.a." + role, percentage ? "value.percentage" : "value.decimal", away)));
        private void RequireSubstitution()
        {
            if (Dialog != DialogKind.Substitution)
                throw new InvalidOperationException("No substitution draft.");
        }

        private static PlayerChoice Find(List<PlayerChoice> list, int index)
        {
            foreach (PlayerChoice c in list)
                if (c.Index == index)
                    return c;
            throw new ArgumentException("Choice is no longer legal.");
        }

        private bool PairIsCurrent()
        {
            bool o = false, n = false;
            foreach (PlayerChoice c in _outgoing)
                if (c.Index == SelectedOutgoing && c.Player.PlayerId == _outgoingPlayerId)
                    o = true;
            foreach (PlayerChoice c in _incoming)
                if (c.Index == SelectedIncoming && c.Player.PlayerId == _incomingPlayerId)
                    n = true;
            return o && n;
        }

        private void UpdatePair()
        {
            CanSubmitSubstitution = PairIsCurrent();
            ChoiceNotice = "";
            Revision++;
        }

        private void ResetMatch()
        {
            _hasChoices = false;
            _scoreHome = _scoreAway = _minute = _usedSubs = -1;
            Array.Clear(_markerText, 0, _markerText.Length);
            Array.Clear(_playerDescriptions, 0, _playerDescriptions.Length);
            _context = null;
            _frame = default;
            _analytics = default;
            _lastFrameTick = _statisticsTick = 0;
            _hasStatistics = _lastPaused = _isReport = false;
            _lastRequestRevision = -1;
            _outgoing.Clear();
            _incoming.Clear();
            _feedback.Clear();
            _statistics.Clear();
            Text.ClearMatch();
            Dialog = DialogKind.None;
            IsStatisticsOpen = IsPartialReportOpen = IsEarlierFeedbackOpen = CanSubmitSubstitution = false;
            CanChangeMentality = CanSubstitute = CanPause = CanSlower = CanFaster = false;
            SelectedOutgoing = SelectedIncoming = _outgoingPlayerId = _incomingPlayerId = -1;
            Score = Text.Label("state.score_unavailable");
            Clock = Text.Label("state.clock_waiting");
            Period = Text.Label("state.waiting");
            ControlReason = Text.Label("reason.waiting");
            Speed = StatisticsNotice = StatisticsCaption = Result = ChoiceNotice = SubstitutionsUsed = MentalityReason = SubstitutionReasonText = SlowerReason = FasterReason = "";
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Consumed localized screen state, owner availability, requests and analytics. |
#endregion
