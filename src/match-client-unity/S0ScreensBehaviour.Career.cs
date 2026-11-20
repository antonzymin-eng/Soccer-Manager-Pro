// File:     src/match-client-unity/S0ScreensBehaviour.Career.cs
// Created:  2026-11-03
// Modified: 2026-11-20 (standings region Tab stop; focus revealed after a page change)
// Modified (prior): 2026-11-19
// Modified (prior): 2026-11-05 (AR-3 M1: reuse coordinator's admitted career formatter.)
// Author:   —
// Spec:     Interactive Unity client §5-P7; shared S0 focus/layout; Code Standards #20 §12 rule 1
// Purpose:  Career UGUI adapter over cached coordinator snapshots; no worker or domain-state reads.

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TacticalDirector.ClientApp;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientUnity
{
    public sealed partial class S0ScreensBehaviour
    {
        private ClientCareerCoordinator _career;
        private CareerScreenText _careerText;
        private int _careerRevision = -1;
        private Selectable _careerHomeHeading, _careerNewHeading, _careerLoadHeading;
        private ScrollRect _careerHomeScroll, _careerNewScroll, _careerLoadScroll;
        private RectTransform _careerClubs;
        private Text _careerSummary, _careerRoundResults, _careerChoice;
        private RectTransform _careerStandings;
        private string[] _careerTableCells;
        private readonly List<Text[]> _careerTableRows = new List<Text[]>();
        private readonly List<Text> _careerNotices = new List<Text>();
        private readonly List<Button> _careerClubButtons = new List<Button>();
        private readonly List<Button> _careerIdleButtons = new List<Button>();
        private readonly List<Button> _careerLoadButtons = new List<Button>();
        private Button _careerContinue, _careerStart, _careerProgress, _careerRetry, _demoButton;
        private ScreenId _careerLastScreen;
        private Selectable _careerFocusReturn;
        private string _careerUiError;
        private RectTransform _careerSchedule, _careerPageControls, _careerWeekControls;
        private Button _careerScheduleToggle, _careerFixtures, _careerCalendar;
        private Button _careerPreviousPage, _careerCurrentPage, _careerNextPage;
        private Button _careerPreviousWeek, _careerCurrentWeek, _careerNextWeek;
        private Text _careerScheduleText;
        private bool _isCareerScheduleOpen, _isCareerCalendarSelected;
        private int _careerScheduleSeason = -1;
        private RectTransform _careerStandingsRows;
        private Selectable _careerStandingsRegion;
        private Text _careerStandingsHeading;
        private Button _careerHigherPage, _careerOwnPage, _careerLowerPage;
        private int? _careerStandingsFirst;
        private int _careerStandingsSeason = -1;
        private CareerStandingsPage _careerStandingsPage;
        private int _careerStandingsShownFirst = -1, _careerStandingsShownCount = -1;
        private uint? _careerCalendarDay;
        private CareerCalendarWeek _careerWeek;
        private int? _careerFixtureFirst;
        private CareerFixturePage _careerPage;

        private string C(string role) => _careerText.Label(role);
        private Button CareerControl(Transform parent, string text, Action action, ScrollRect scroll,
            S0UiFactory.ControlKind kind = S0UiFactory.ControlKind.Secondary)
        {
            Button button = _ui.Button(parent, text, () =>
            {
                if (_career.IsBusy) return;
                try { action(); _careerUiError = null; }
                catch (InvalidOperationException exception) { _careerUiError = exception.Message; }
                if (_career.IsBusy && _view.Screen == _careerLastScreen)
                    _careerFocusReturn = EventSystem.current?.currentSelectedGameObject?.GetComponent<Selectable>();
                _applyVisibility();
                RefreshCareer(true);
            }, kind);
            Register(button, scroll, S0FocusNavigation.Role.Action);
            _careerIdleButtons.Add(button);
            return button;
        }

        private void BuildCareer(ClientCareerCoordinator career, RectTransform menu, GameObject home,
            GameObject setup, GameObject load, Func<ulong> seed)
        {
            _career = career;
            _careerText = career.ScreenText;
            _careerNotices.Add(_ui.Reason(menu, true));
            CareerControl(menu, C("new"), () => career.OpenNewCareer(seed()), _menuScroll, S0UiFactory.ControlKind.Primary);
            _careerContinue = CareerControl(menu, C("continue"), career.ContinueCareer, _menuScroll);
            CareerControl(menu, C("load"), career.OpenLoadCareer, _menuScroll);
            _careerRetry = CareerControl(menu, C("retry"), career.Initialize, _menuScroll);
            RectTransform n = Page(setup, out _careerNewScroll, S0UiConstants.ENTRY_MAX_WIDTH);
            _careerNewHeading = Heading(n, C("new"), _careerNewScroll);
            _careerNotices.Add(_ui.Reason(n, true));
            _careerChoice = _ui.Label(n, "");
            CareerControl(n, C("back"), career.CancelNewCareer, _careerNewScroll);
            _careerClubs = _ui.Column("Career clubs", n);
            _careerStart = CareerControl(n, C("start"), career.StartCareer, _careerNewScroll, S0UiFactory.ControlKind.Primary);
            RectTransform l = Page(load, out _careerLoadScroll, S0UiConstants.ENTRY_MAX_WIDTH);
            _careerLoadHeading = Heading(l, C("load"), _careerLoadScroll);
            _careerNotices.Add(_ui.Reason(l, true));
            for (int i = 0; i < CareerSaveSlots.SlotCount; i++)
            {
                CareerSaveSlot slot = (CareerSaveSlot)i;
                _careerLoadButtons.Add(CareerControl(l, "", () => career.LoadCareer(slot), _careerLoadScroll));
            }
            CareerControl(l, C("back"), career.CancelLoadCareer, _careerLoadScroll);
            RectTransform h = Page(home, out _careerHomeScroll);
            _careerHomeHeading = Heading(h, C("home"), _careerHomeScroll);
            _careerNotices.Add(_ui.Reason(h, true));
            _careerSummary = _ui.Label(h, "");
            _careerProgress = CareerControl(h, "", career.Progress, _careerHomeScroll);
            _careerRoundResults = _ui.Label(h, "");
            BuildCareerSchedule(h);
            _ui.Label(h, C("warning"));
            for (int i = 1; i <= ClientAppConstants.CAREER_MANUAL_SLOT_COUNT; i++)
            {
                CareerSaveSlot slot = (CareerSaveSlot)i;
                CareerControl(h, _careerText.Text.Format("save", "save", i), () => career.SaveTo(slot), _careerHomeScroll);
            }
            CareerControl(h, C("quit"), career.QuitToMainMenu, _careerHomeScroll);
            _careerStandings = _ui.Column("League standings", h, 0);
            _ui.Label(_careerStandings, C("table"), S0UiFactory.TextStyle.Subheading);
            // The page heading and rows form one Tab stop. Focus shows its top even when it is taller than the window,
            // and arrow keys scroll it, so a page is readable whatever the row heights.
            RectTransform region = _ui.Column("Standings page", _careerStandings, 0);
            _careerStandingsRegion = region.gameObject.AddComponent<Selectable>();
            _careerStandingsRegion.navigation = new Navigation { mode = Navigation.Mode.None };
            _careerStandingsRegion.transition = Selectable.Transition.None;
            Register(_careerStandingsRegion, _careerHomeScroll, S0FocusNavigation.Role.Region);
            _revealGroups[_careerStandingsRegion] = _careerStandings;
            _careerStandingsHeading = _ui.Label(region, "");
            _careerStandingsRows = _ui.Column("Standings rows", region, 0);
            _careerTableCells = new string[_careerText.TableHeaders.Count];
            Text[] headers = CreateCareerTableRow(S0UiFactory.TextStyle.Strong);
            for (int i = 0; i < headers.Length; i++)
                S0UiFactory.Set(headers[i], _careerText.TableHeaders[i]);
            // The paging controls follow the rows: they are the last Tab stops on Career Home, and Reveal scrolls
            // only far enough to show the focused control, so controls below the page bring the page into view.
            RectTransform paging = _ui.Column("Standings navigation", _careerStandings, 0);
            _careerHigherPage = CareerControl(paging, C("table.page.previous"),
                () => _careerStandingsFirst = _careerStandingsPage.PreviousFirst, _careerHomeScroll);
            _careerOwnPage = CareerControl(paging, C("table.page.current"),
                () => _careerStandingsFirst = null, _careerHomeScroll);
            _careerLowerPage = CareerControl(paging, C("table.page.next"),
                () => _careerStandingsFirst = _careerStandingsPage.NextFirst, _careerHomeScroll);
            _revealGroups[_careerHigherPage] = _careerStandings;
            _revealGroups[_careerOwnPage] = _careerStandings;
            _revealGroups[_careerLowerPage] = _careerStandings;
        }

        private Text[] CreateCareerTableRow(S0UiFactory.TextStyle style)
        {
            RectTransform row = _ui.Row("Standings row", _careerStandingsRows, TextAnchor.UpperLeft);
            var cells = new Text[_careerTableCells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                bool isClub = i == 1;
                Text cell = _ui.Label(row, "", style);
                cell.alignment = isClub ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
                var size = cell.gameObject.AddComponent<LayoutElement>();
                size.minWidth = size.preferredWidth = 0;
                size.flexibleWidth = isClub ? 5 : 1;
                cells[i] = cell;
            }
            return cells;
        }

        private void RefreshCareerResults(in CareerHomeViewModel home)
        {
            string scores = _careerText.RoundResults(in home);
            _careerRoundResults.gameObject.SetActive(scores.Length != 0);
            S0UiFactory.Set(_careerRoundResults, scores);
            _careerStandings.gameObject.SetActive(home.HasCareer);
            if (!home.HasCareer)
            {
                _careerStandingsFirst = null;
                _careerStandingsSeason = -1;
                return;
            }

            if (home.SeasonNumber != _careerStandingsSeason)
            {
                _careerStandingsSeason = home.SeasonNumber;
                _careerStandingsFirst = null;
            }

            _careerStandingsPage = _careerStandingsFirst.HasValue
                ? CareerStandingsPage.At(in home, _careerStandingsFirst.Value)
                : CareerStandingsPage.Current(in home);
            bool pageChanged = _careerStandingsPage.First != _careerStandingsShownFirst ||
                               _careerStandingsPage.Count != _careerStandingsShownCount;
            _careerStandingsShownFirst = _careerStandingsPage.First;
            _careerStandingsShownCount = _careerStandingsPage.Count;
            S0UiFactory.Set(_careerStandingsHeading, _careerText.StandingsHeading(in home, in _careerStandingsPage));
            RefreshSchedulePaging(_career.IsInitialized && !_career.IsBusy, _careerStandingsPage.HasPrevious,
                _careerStandingsPage.HasNext, _careerHigherPage, _careerOwnPage, _careerLowerPage);

            while (_careerTableRows.Count < home.Table.Count)
                _careerTableRows.Add(CreateCareerTableRow(S0UiFactory.TextStyle.Body));

            int pageEnd = _careerStandingsPage.First + _careerStandingsPage.Count;
            for (int i = 0; i < _careerTableRows.Count; i++)
            {
                Text[] cells = _careerTableRows[i];
                bool visible = i >= _careerStandingsPage.First && i < pageEnd;
                cells[0].transform.parent.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                _careerText.WriteTableRow(in home, i, _careerTableCells);
                for (int column = 0; column < cells.Length; column++)
                    S0UiFactory.Set(cells[column], _careerTableCells[column]);
            }

            // A new page changes the height above the paging controls, so reveal the focused control again once
            // the new rows are in place; a control that stays selected would otherwise be left off screen.
            Selectable focused = EventSystem.current?.currentSelectedGameObject?.GetComponent<Selectable>();
            if (pageChanged && focused != null && _revealGroups.TryGetValue(focused, out RectTransform group) &&
                group == _careerStandings)
                Focus(focused);
        }

        private void BuildCareerSchedule(RectTransform home)
        {
            _careerScheduleToggle = CareerControl(home, C("schedule"),
                () => _isCareerScheduleOpen = !_isCareerScheduleOpen, _careerHomeScroll);
            _careerSchedule = _ui.Panel("Career schedule", home);
            _careerFixtures = CareerControl(_careerSchedule, C("schedule.list"),
                () => _isCareerCalendarSelected = false, _careerHomeScroll);
            _careerCalendar = CareerControl(_careerSchedule, C("schedule.calendar"),
                () => _isCareerCalendarSelected = true, _careerHomeScroll);
            _careerPageControls = _ui.Column("Fixture navigation", _careerSchedule, 0);
            _careerPreviousPage = CareerControl(_careerPageControls, C("schedule.page.previous"),
                () => _careerFixtureFirst = _careerPage.PreviousFirst, _careerHomeScroll);
            _careerCurrentPage = CareerControl(_careerPageControls, C("schedule.page.current"),
                () => _careerFixtureFirst = null, _careerHomeScroll);
            _careerNextPage = CareerControl(_careerPageControls, C("schedule.page.next"),
                () => _careerFixtureFirst = _careerPage.NextFirst, _careerHomeScroll);
            _careerWeekControls = _ui.Column("Calendar navigation", _careerSchedule, 0);
            _careerPreviousWeek = CareerControl(_careerWeekControls, C("schedule.previous"),
                () => _careerCalendarDay = _careerWeek.PreviousWeek, _careerHomeScroll);
            _careerCurrentWeek = CareerControl(_careerWeekControls, C("schedule.current"),
                () => _careerCalendarDay = null, _careerHomeScroll);
            _careerNextWeek = CareerControl(_careerWeekControls, C("schedule.next"),
                () => _careerCalendarDay = _careerWeek.NextWeek, _careerHomeScroll);
            _careerScheduleText = _ui.Label(_careerSchedule, "");
            _careerSchedule.gameObject.SetActive(false);
        }

        private void RefreshCareerSchedule(ScreenId screen, in CareerHomeViewModel home)
        {
            if (!home.HasCareer)
            {
                _isCareerScheduleOpen = false;
                _isCareerCalendarSelected = false;
                _careerScheduleSeason = -1;
            }

            bool showSchedule = screen == ClientScreens.CareerHome && home.HasCareer && _isCareerScheduleOpen;
            _careerScheduleToggle.interactable = home.HasCareer && _career.IsInitialized && !_career.IsBusy;
            _ui.SetSelected(_careerScheduleToggle, _isCareerScheduleOpen);
            _careerSchedule.gameObject.SetActive(showSchedule);
            if (!showSchedule)
            {
                return;
            }

            if (home.SeasonNumber != _careerScheduleSeason)
            {
                _careerScheduleSeason = home.SeasonNumber;
                _careerCalendarDay = null;
                _careerFixtureFirst = null;
            }

            _ui.SetSelected(_careerFixtures, !_isCareerCalendarSelected);
            _ui.SetSelected(_careerCalendar, _isCareerCalendarSelected);
            _careerWeekControls.gameObject.SetActive(_isCareerCalendarSelected);
            _careerPageControls.gameObject.SetActive(!_isCareerCalendarSelected);
            bool canBrowse = _career.IsInitialized && !_career.IsBusy;
            if (_isCareerCalendarSelected)
            {
                _careerWeek = CareerCalendarWeek.At(in home, _careerCalendarDay ?? home.WorldDay);
                S0UiFactory.Set(_careerScheduleText, _careerText.Calendar(in home, in _careerWeek));
                RefreshSchedulePaging(canBrowse, _careerWeek.PreviousWeek != _careerWeek.FirstDay,
                    _careerWeek.NextWeek != _careerWeek.FirstDay,
                    _careerPreviousWeek, _careerCurrentWeek, _careerNextWeek);
            }
            else
            {
                _careerPage = _careerFixtureFirst.HasValue
                    ? CareerFixturePage.At(in home, _careerFixtureFirst.Value)
                    : CareerFixturePage.Current(in home);
                S0UiFactory.Set(_careerScheduleText, _careerText.FixtureList(in home, in _careerPage));
                RefreshSchedulePaging(canBrowse, _careerPage.HasPrevious, _careerPage.HasNext,
                    _careerPreviousPage, _careerCurrentPage, _careerNextPage);
            }
        }

        // Shared by week and fixture paging: a disabled end control keeps focus in the group by moving it to
        // the current-page control, so a keyboard user is never stranded on an unavailable button.
        private void RefreshSchedulePaging(bool canBrowse, bool hasPrevious, bool hasNext,
            Button previous, Button current, Button next)
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            previous.interactable = canBrowse && hasPrevious;
            next.interactable = canBrowse && hasNext;
            if (canBrowse && ((!hasPrevious && selected == previous.gameObject) ||
                              (!hasNext && selected == next.gameObject)))
            {
                Focus(current);
            }
        }

        private Selectable CareerHeading(ScreenId screen) => screen == ClientScreens.CareerHome ? _careerHomeHeading :
            screen == ClientScreens.NewCareer ? _careerNewHeading : screen == ClientScreens.LoadCareer ? _careerLoadHeading : null;

        private void RefreshCareer(bool force = false)
        {
            if (_career == null) return;
            ScreenId screen = _view.Screen;
            if (!force && _careerRevision == _career.Revision && _careerLastScreen == screen) return;
            bool changedScreen = _careerLastScreen != screen;
            _careerRevision = _career.Revision;
            _careerLastScreen = screen;
            foreach (Button button in _careerIdleButtons) button.interactable = _career.IsInitialized && !_career.IsBusy;
            _demoButton.interactable = !_career.IsBusy;
            _careerRetry.gameObject.SetActive(!_career.IsInitialized);
            _careerRetry.interactable = !_career.IsInitialized && !_career.IsBusy;
            _careerContinue.interactable = _career.CanContinue;
            _careerStart.interactable = _career.CanStart;
            CareerHomeViewModel home = _career.Home;
            _careerProgress.interactable = home.HasCareer && !home.IsProgressBlocked && !_career.IsBusy && home.NextAction != CareerProgressAction.None;
            _ui.SetButton(_careerProgress, _careerText.Progress(home.NextAction));
            S0UiFactory.Set(_careerSummary, _careerText.Summary(in home));
            RefreshCareerResults(in home);
            RefreshCareerSchedule(screen, in home);
            NewCareerViewModel preview = _career.NewCareer;
            int clubs = preview.Clubs == null ? 0 : preview.Clubs.Count;
            while (_careerClubButtons.Count < clubs)
            {
                int index = _careerClubButtons.Count;
                _careerClubButtons.Add(CareerControl(_careerClubs, "", () =>
                    _career.SelectClub(_career.NewCareer.Clubs[index].ClubId), _careerNewScroll));
            }
            for (int i = 0; i < _careerClubButtons.Count; i++)
            {
                Button button = _careerClubButtons[i];
                button.gameObject.SetActive(i < clubs);
                button.interactable = !_career.IsBusy;
                if (i >= clubs) continue;
                bool selected = preview.HasSelection && preview.SelectedClubId == preview.Clubs[i].ClubId;
                _ui.SetSelected(button, selected);
                _ui.SetButton(button, _careerText.Text.Format("club." + i, selected ? "selected" : "select", preview.Clubs[i].Name));
            }
            _careerChoice.gameObject.SetActive(clubs > 0);
            if (clubs > 0)
                S0UiFactory.Set(_careerChoice, preview.HasSelection ? C("selection.ready") : C("choose"));
            var slots = _career.Load.Slots;
            bool present = slots != null;
            for (int i = 0; i < _careerLoadButtons.Count; i++)
            {
                Button button = _careerLoadButtons[i];
                button.gameObject.SetActive(present);
                button.interactable = present && !_career.IsBusy && slots[i].IsLoadable;
                if (present) _ui.SetButton(button, _careerText.Slot(slots[i]));
            }
            string notice = _career.IsBusy ? C("busy") : _careerUiError ?? _career.LastError ?? _career.LastProjectionError ??
                (home.AutosaveError != null ? _careerText.Text.Format("autosave", "autosave.error", home.AutosaveError) :
                screen == ClientScreens.MainMenu && _career.IsInitialized && present && !_career.Load.CanContinue ? C("no_save") : "");
            foreach (Text label in _careerNotices) S0UiFactory.SetBoxed(label, notice);
            Selectable heading = screen == ClientScreens.MainMenu ? _menuHeading : CareerHeading(screen);
            if ((changedScreen || _career.IsBusy) && heading != null)
                Focus(heading);
            if (!_career.IsBusy && _careerFocusReturn != null)
            {
                if (!changedScreen && string.IsNullOrEmpty(notice) && heading != null &&
                    EventSystem.current?.currentSelectedGameObject == heading.gameObject)
                    Focus(_careerFocusReturn);
                _careerFocusReturn = null;
            }
            RecoverFocus();
        }
    }
}
#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-03 | — | Persistent career pages, menu entries, cached binding and shared keyboard focus. |
// | 1.1 | 2026-11-04 | — | Separate selected-club status from the Start action and cache club rows independently. |
// | 1.2 | 2026-11-05 | — | T2: visible projection errors and consistent failed-entry rollback. |
// | 1.3 | 2026-11-05 | — | AR-3 M1: display labels use the same admitted CareerScreenText instance as coordinator failure copy. |
// | 1.4 | 2026-11-18 | — | Keep career feedback and recovery controls near the heading and hide unpopulated slot rows. |
// | 1.5 | 2026-11-18 | — | Follow the club list with Start, retain the pending Retry control and restore focus after same-screen work. |
// | 1.6 | 2026-11-19 | — | Bind a schedule disclosure to cached Career Home fixtures and calendar. |
// | 1.7 | 2026-11-19 | — | Keep keyboard focus in the calendar at the first and last week. |
// | 1.8 | 2026-11-19 | — | Page the fixture list (Earlier / Upcoming / Later fixtures) so every row is reachable by keyboard at 200% text scale; share the boundary focus rule with week paging. |
// | 1.9 | 2026-11-19 | — | Render round scores and aligned league standings from the cached snapshot. |
// | 1.10 | 2026-11-20 | — | Page the standings (eight rows under a position heading) with Higher / Your / Lower position controls after the rows, sharing the fixture paging focus rule, so every row is reachable by keyboard at 200% text scale. |
// | 1.11 | 2026-11-20 | — | PR #557 review: the page heading and rows are one Region Tab stop (top shown on focus, arrow keys scroll it), the paging controls reveal the whole standings block when it fits, and a page change reveals the focused control after the new rows are laid out. |
#endregion
