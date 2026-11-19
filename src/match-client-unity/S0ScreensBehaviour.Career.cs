// File:     src/match-client-unity/S0ScreensBehaviour.Career.cs
// Created:  2026-11-03
// Modified: 2026-11-19
// Modified (prior): 2026-11-05 (AR-3 M1: reuse coordinator's admitted career formatter.)
// Author:   —
// Spec:     Interactive Unity client §5-P7; shared S0 focus/layout; Code Standards #20 §12 rule 1
// Purpose:  Career UGUI adapter over cached coordinator snapshots; no worker or domain-state reads.

using System;
using System.Collections.Generic;

using UnityEngine;
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
        private Text _careerSummary, _careerTable, _careerChoice;
        private readonly List<Text> _careerNotices = new List<Text>();
        private readonly List<Button> _careerClubButtons = new List<Button>();
        private readonly List<Button> _careerIdleButtons = new List<Button>();
        private readonly List<Button> _careerLoadButtons = new List<Button>();
        private Button _careerContinue, _careerStart, _careerProgress, _careerRetry, _demoButton;
        private ScreenId _careerLastScreen;
        private string _careerUiError;
        private RectTransform _careerSchedule, _careerWeekControls;
        private Button _careerScheduleToggle, _careerFixtures, _careerCalendar, _careerPreviousWeek, _careerNextWeek;
        private Text _careerScheduleText;
        private bool _isCareerScheduleOpen, _isCareerCalendarSelected;
        private int _careerScheduleSeason = -1;
        private uint? _careerCalendarDay;
        private CareerCalendarWeek _careerWeek;

        private string C(string role) => _careerText.Label(role);
        private Button CareerControl(Transform parent, string text, Action action, ScrollRect scroll)
        {
            Button button = _ui.Button(parent, text, () =>
            {
                if (_career.IsBusy) return;
                try { action(); _careerUiError = null; }
                catch (InvalidOperationException exception) { _careerUiError = exception.Message; }
                _applyVisibility();
                RefreshCareer(true);
            });
            Register(button, scroll, S0FocusNavigation.Role.Action);
            _careerIdleButtons.Add(button);
            return button;
        }

        private void BuildCareer(ClientCareerCoordinator career, RectTransform menu, GameObject home,
            GameObject setup, GameObject load, Func<ulong> seed)
        {
            _career = career;
            _careerText = career.ScreenText;
            CareerControl(menu, C("new"), () => career.OpenNewCareer(seed()), _menuScroll);
            _careerContinue = CareerControl(menu, C("continue"), career.ContinueCareer, _menuScroll);
            CareerControl(menu, C("load"), career.OpenLoadCareer, _menuScroll);
            _careerRetry = CareerControl(menu, C("retry"), career.Initialize, _menuScroll);
            _careerNotices.Add(_ui.Reason(menu, true));
            RectTransform n = Page(setup, out _careerNewScroll, S0UiConstants.ENTRY_MAX_WIDTH);
            _careerNewHeading = Heading(n, C("new"), _careerNewScroll);
            _ui.Label(n, C("choose"));
            _careerChoice = _ui.Label(n, "");
            _careerClubs = _ui.Column("Career clubs", n);
            _careerStart = CareerControl(n, C("start"), career.StartCareer, _careerNewScroll);
            CareerControl(n, C("back"), career.CancelNewCareer, _careerNewScroll);
            _careerNotices.Add(_ui.Reason(n, true));
            RectTransform l = Page(load, out _careerLoadScroll, S0UiConstants.ENTRY_MAX_WIDTH);
            _careerLoadHeading = Heading(l, C("load"), _careerLoadScroll);
            for (int i = 0; i < CareerSaveSlots.SlotCount; i++)
            {
                CareerSaveSlot slot = (CareerSaveSlot)i;
                _careerLoadButtons.Add(CareerControl(l, "", () => career.LoadCareer(slot), _careerLoadScroll));
            }
            CareerControl(l, C("back"), career.CancelLoadCareer, _careerLoadScroll);
            _careerNotices.Add(_ui.Reason(l, true));
            RectTransform h = Page(home, out _careerHomeScroll);
            _careerHomeHeading = Heading(h, C("home"), _careerHomeScroll);
            _careerSummary = _ui.Label(h, "");
            _careerNotices.Add(_ui.Reason(h, true));
            _careerProgress = CareerControl(h, "", career.Progress, _careerHomeScroll);
            BuildCareerSchedule(h);
            _ui.Label(h, C("warning"));
            for (int i = 1; i <= ClientAppConstants.CAREER_MANUAL_SLOT_COUNT; i++)
            {
                CareerSaveSlot slot = (CareerSaveSlot)i;
                CareerControl(h, _careerText.Text.Format("save", "save", i), () => career.SaveTo(slot), _careerHomeScroll);
            }
            CareerControl(h, C("quit"), career.QuitToMainMenu, _careerHomeScroll);
            _ui.Label(h, C("table"), S0UiFactory.TextStyle.Subheading);
            _careerTable = _ui.Label(h, "");
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
            _careerWeekControls = _ui.Column("Calendar navigation", _careerSchedule, 0);
            _careerPreviousWeek = CareerControl(_careerWeekControls, C("schedule.previous"),
                () => _careerCalendarDay = _careerWeek.PreviousWeek, _careerHomeScroll);
            CareerControl(_careerWeekControls, C("schedule.current"),
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
            }

            _ui.SetSelected(_careerFixtures, !_isCareerCalendarSelected);
            _ui.SetSelected(_careerCalendar, _isCareerCalendarSelected);
            _careerWeekControls.gameObject.SetActive(_isCareerCalendarSelected);
            if (_isCareerCalendarSelected)
            {
                _careerWeek = CareerCalendarWeek.At(in home, _careerCalendarDay ?? home.WorldDay);
                bool canBrowse = _career.IsInitialized && !_career.IsBusy;
                _careerPreviousWeek.interactable = canBrowse && _careerWeek.PreviousWeek != _careerWeek.FirstDay;
                _careerNextWeek.interactable = canBrowse && _careerWeek.NextWeek != _careerWeek.FirstDay;
                S0UiFactory.Set(_careerScheduleText, _careerText.Calendar(in home, in _careerWeek));
            }
            else
            {
                S0UiFactory.Set(_careerScheduleText, _careerText.FixtureList(in home));
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
            _careerRetry.interactable = !_career.IsBusy;
            _careerContinue.interactable = _career.CanContinue;
            _careerStart.interactable = _career.CanStart;
            CareerHomeViewModel home = _career.Home;
            _careerProgress.interactable = home.HasCareer && !home.IsProgressBlocked && !_career.IsBusy && home.NextAction != CareerProgressAction.None;
            _ui.SetButton(_careerProgress, _careerText.Progress(home.NextAction));
            S0UiFactory.Set(_careerSummary, _careerText.Summary(in home));
            S0UiFactory.Set(_careerTable, _careerText.Table(in home));
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
                if (i < clubs) _ui.SetButton(button, _careerText.Text.Format("club." + i, preview.HasSelection &&
                    preview.SelectedClubId == preview.Clubs[i].ClubId ? "selected" : "select", preview.Clubs[i].Name));
            }
            S0UiFactory.Set(_careerChoice, preview.HasSelection ? C("selection.ready") : C("choose"));
            for (int i = 0; i < _careerLoadButtons.Count; i++)
            {
                bool present = _career.Load.Slots != null;
                _careerLoadButtons[i].interactable = present && !_career.IsBusy && _career.Load.Slots[i].IsLoadable;
                if (present) _ui.SetButton(_careerLoadButtons[i], _careerText.Slot(_career.Load.Slots[i]));
            }
            string notice = _career.IsBusy ? C("busy") : _careerUiError ?? _career.LastError ?? _career.LastProjectionError ??
                (home.AutosaveError != null ? _careerText.Text.Format("autosave", "autosave.error", home.AutosaveError) :
                screen == ClientScreens.MainMenu && !_career.Load.CanContinue ? C("no_save") : "");
            foreach (Text label in _careerNotices) S0UiFactory.SetBoxed(label, notice);
            if (changedScreen && CareerHeading(screen) != null) Focus(CareerHeading(screen));
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
// | 1.4 | 2026-11-19 | — | Bind a schedule disclosure to cached Career Home fixtures and calendar. |
#endregion
