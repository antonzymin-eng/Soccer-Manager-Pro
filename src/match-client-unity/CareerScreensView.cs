// File:     src/match-client-unity/CareerScreensView.cs
// Created:  2026-11-06
// Modified: 2026-11-10
// Author:   —
// Spec:     Interactive Unity client §5-P7, UI / Client Framework #38, Code Standards #20
// Purpose:  Persistent UGUI career pages using shared S0 text, layout, controls and keyboard focus.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using UnityEngine;
using UnityEngine.UI;

using TacticalDirector.ClientApp;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientUnity
{
    /// <summary>Thin binding over cached career snapshots; contains no domain access or worker callbacks.</summary>
    internal sealed class CareerScreensView
    {
        private readonly S0UiFactory _ui;
        private readonly CareerScreenText _text;
        private readonly CareerScreenPresenter _state;
        private readonly ClientScreenFlow _flow;
        private readonly Action _changed;
        private readonly Action<Selectable, ScrollRect, bool> _register;
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<Button> _clubs = new List<Button>();
        private readonly List<Text> _table = new List<Text>();
        private readonly Text[] _status = new Text[4], _errors = new Text[4];
        private readonly Text[] _loadRows = new Text[CareerSaveSlots.SlotCount], _saveRows = new Text[ClientAppConstants.CAREER_MANUAL_SLOT_COUNT];
        private readonly Button[] _loadButtons = new Button[CareerSaveSlots.SlotCount];
        private readonly Button[] _saveButtons = new Button[ClientAppConstants.CAREER_MANUAL_SLOT_COUNT];
        private readonly Button _continue, _start, _progress, _confirm, _cancel, _retry;
        private readonly Text _summary, _changedText, _attention, _next, _autosaveError, _selected, _confirmationText;
        private readonly RectTransform _clubList, _tableList, _confirmation;
        private readonly ScrollRect _newScroll, _homeScroll;
        private readonly Selectable _homeHeading, _newHeading, _loadHeading;
        private int _revision = -1;
        private Button _busyInvoker, _completedInvoker, _confirmationInvoker;
        private bool _wasBusy;

        internal CareerScreensView(S0UiFactory ui, Font font, CareerScreenPresenter state, ClientScreenFlow flow,
            Transform menu, ScrollRect menuScroll, GameObject home, GameObject setup, GameObject load,
            Action<GameObject> own, Action<Selectable, ScrollRect, bool> register, Action changed)
        {
            _ui = ui;
            _text = new CareerScreenText(S0TextFormatter.WithGlyphCoverage(font.HasCharacter, CareerScreenText.All));
            _state = state;
            _flow = flow;
            _changed = changed;
            _register = register;
            Button(menu, "action.new", () => _state.OpenNew(NewSeed()), menuScroll);
            _continue = Button(menu, "action.continue", _state.ContinueCareer, menuScroll);
            Button(menu, "action.load", _state.OpenLoad, menuScroll);
            Button(menu, "action.refresh", _state.RefreshSlots, menuScroll);
            _retry = Button(menu, "action.retry", _state.Initialize, menuScroll);
            Feedback(menu, 0);

            RectTransform n = Page(setup, out _newScroll, own);
            _newHeading = Heading(n, "heading.new", _newScroll);
            Label(n, "reason.setup");
            _selected = _ui.Label(n, "");
            Feedback(n, 1);
            Button(n, "action.refresh", _state.RefreshSlots, _newScroll);
            _clubList = _ui.Column("Club choices", n, 0);
            _start = Button(n, "action.start", _state.StartCareer, _newScroll, primary: true);
            Button(n, "action.back", _state.Back, _newScroll);

            RectTransform l = Page(load, out ScrollRect loadScroll, own);
            _loadHeading = Heading(l, "heading.load", loadScroll);
            Label(l, "reason.breakable");
            Feedback(l, 2);
            Button(l, "action.refresh", _state.RefreshSlots, loadScroll);
            Button(l, "action.back", _state.Back, loadScroll);
            for (int i = 0; i < _loadRows.Length; i++)
            {
                CareerSaveSlot slot = (CareerSaveSlot)i;
                RectTransform row = _ui.Panel("Save slot", l);
                _loadRows[i] = _ui.Label(row, "");
                _loadButtons[i] = Button(row, _text.Format("load.action." + i, "action.load_slot", _text.SlotName(slot)),
                    () => _state.LoadSlot(slot), loadScroll, true);
            }

            RectTransform h = Page(home, out _homeScroll, own);
            _homeHeading = Heading(h, "heading.home", _homeScroll);
            _summary = _ui.Label(h, "", S0UiFactory.TextStyle.Strong);
            Feedback(h, 3);
            Label(h, "heading.changed", true);
            _changedText = _ui.Label(h, "");
            Label(h, "heading.attention", true);
            _attention = _ui.Label(h, "");
            _autosaveError = _ui.Reason(h, true);
            Label(h, "heading.next", true);
            _next = _ui.Label(h, "");
            _progress = Button(h, "action.none", _state.Progress, _homeScroll, primary: true);
            Label(h, "heading.save", true);
            Label(h, "reason.autosave");
            Label(h, "reason.breakable");
            Button(h, "action.refresh", _state.RefreshSlots, _homeScroll);
            for (int i = 0; i < _saveRows.Length; i++)
            {
                CareerSaveSlot slot = (CareerSaveSlot)(i + 1);
                RectTransform row = _ui.Panel("Manual save", h);
                _saveRows[i] = _ui.Label(row, "");
                _saveButtons[i] = Button(row, _text.Format("save.action." + i, "action.save", _text.SlotName(slot)),
                    () => _state.RequestSave(slot), _homeScroll, true);
            }
            _confirmation = _ui.Panel("Confirm career action", h, true, true);
            _confirmationText = _ui.Label(_confirmation, "");
            _confirm = Button(_confirmation, "action.overwrite", () => { if (_state.PendingQuit) _state.ConfirmQuit(); else _state.ConfirmOverwrite(); }, _homeScroll);
            _cancel = Button(_confirmation, "action.cancel", () => { if (_state.PendingQuit) _state.CancelQuit(); else _state.CancelOverwrite(); }, _homeScroll);
            _confirmation.gameObject.SetActive(false);
            Label(h, "reason.quit");
            Button(h, "action.quit", _state.Back, _homeScroll);
            Label(h, "heading.table", true);
            _tableList = _ui.Column("League table", h, 0);
        }

        private RectTransform Page(GameObject root, out ScrollRect scroll, Action<GameObject> own)
        {
            RectTransform content = _ui.Page(root.transform, out scroll, S0UiConstants.ENTRY_MAX_WIDTH);
            own(scroll.gameObject);
            return _ui.Panel("Career page", content, true);
        }

        private Text Label(Transform parent, string role, bool heading = false) => _ui.Label(parent, _text.Label(role), heading);
        private Selectable Heading(Transform parent, string role, ScrollRect scroll)
        {
            Text label = Label(parent, role, true);
            Selectable anchor = label.gameObject.AddComponent<Selectable>();
            anchor.navigation = new Navigation { mode = Navigation.Mode.None };
            anchor.transition = Selectable.Transition.None;
            _register(anchor, scroll, false);
            return anchor;
        }

        private Button Button(Transform parent, string role, Action action, ScrollRect scroll, bool literal = false, bool primary = false)
        {
            Button button = null;
            button = _ui.Button(parent, literal ? role : _text.Label(role), () =>
            {
                try
                {
                    bool hadConfirmation = HasConfirmation;
                    action();
                    if (!hadConfirmation && HasConfirmation) _confirmationInvoker = button;
                    if (_state.IsBusy) _busyInvoker = hadConfirmation ? _confirmationInvoker : button;
                    if (hadConfirmation && !HasConfirmation)
                    {
                        if (!_state.IsBusy) _completedInvoker = _confirmationInvoker;
                        _confirmationInvoker = null;
                    }
                }
                catch (Exception exception)
                {
                    _state.ReportActionFailure(exception);
                    Debug.LogException(exception);
                }
                _changed();
            }, primary ? S0UiFactory.ControlKind.Primary : S0UiFactory.ControlKind.Secondary);
            _buttons.Add(button);
            _register(button, scroll, true);
            return button;
        }

        private void Feedback(Transform parent, int index)
        {
            _status[index] = _ui.Reason(parent);
            _errors[index] = _ui.Reason(parent, true);
        }

        internal static bool IsCareerScreen(ScreenId screen) => screen == ClientScreens.CareerHome ||
            screen == ClientScreens.NewCareer || screen == ClientScreens.LoadCareer;

        internal Selectable HeadingFor(ScreenId screen) => screen == ClientScreens.CareerHome ? _homeHeading :
            screen == ClientScreens.NewCareer ? _newHeading : _loadHeading;

        internal bool Allows(Selectable item) => !HasConfirmation || item.transform.IsChildOf(_confirmation);
        private bool HasConfirmation => _state.PendingOverwrite.HasValue || _state.PendingQuit;
        internal Selectable ConfirmationFocus => HasConfirmation ? _cancel : null;
        internal void CancelConfirmation()
        {
            if (_state.PendingQuit) _state.CancelQuit(); else _state.CancelOverwrite();
            _completedInvoker = _confirmationInvoker;
            _confirmationInvoker = null;
            _changed();
        }

        internal Selectable TakeCompletedInvoker()
        {
            Button completed = _completedInvoker;
            _completedInvoker = null;
            return completed;
        }

        internal void Refresh()
        {
            if (_revision == _state.Revision) return;
            if (_wasBusy && !_state.IsBusy)
            {
                _completedInvoker = _busyInvoker;
                _busyInvoker = null;
            }
            _wasBusy = _state.IsBusy;
            _revision = _state.Revision;
            bool idle = _state.IsReady && !_state.IsBusy;
            bool confirm = HasConfirmation;
            foreach (Button button in _buttons) button.interactable = idle && !confirm;
            _continue.interactable = idle && _state.Load.CanContinue;
            _retry.gameObject.SetActive(!_state.IsReady && !_state.IsBusy);
            _retry.interactable = !_state.IsBusy;
            _start.interactable = idle && _state.New.HasSelection;
            _progress.interactable = idle && !confirm && !_state.HasSnapshotWarning && _state.Home.HasCareer && !_state.Home.IsProgressBlocked &&
                _state.Home.NextAction != CareerProgressAction.None;
            foreach (Button save in _saveButtons) save.interactable = idle && !confirm && !_state.HasSnapshotWarning;
            _confirm.interactable = _cancel.interactable = idle;
            _confirmation.gameObject.SetActive(confirm);
            if (confirm)
            {
                S0UiFactory.Set(_confirmationText, _state.PendingQuit ? _text.Label("reason.discard") :
                    _text.Format("confirm", "reason.confirm", _text.SlotName(_state.PendingOverwrite.Value)));
                _ui.SetButton(_confirm, _text.Label(_state.PendingQuit ? "action.discard" : "action.overwrite"));
            }

            string status = _state.IsBusy ? _text.Label("reason.busy") : !_state.IsReady ? _text.Label("reason.unready") : "";
            if (idle && !_state.Load.CanContinue && _flow.Current == ClientScreens.MainMenu) status = _text.Label("reason.no_save");
            if (idle && _flow.Current == ClientScreens.NewCareer && !_state.New.HasSelection) status = _text.Label("reason.selection");
            if (_state.SavedSlot.HasValue) status = _text.Format("saved", "reason.saved", _text.SlotName(_state.SavedSlot.Value));
            if (_state.Home.NextAction == CareerProgressAction.PlayMatchday && _flow.Current == ClientScreens.CareerHome)
                status = status.Length == 0 ? _text.Label("reason.matchday") : status + "\n" + _text.Label("reason.matchday");
            string error = string.IsNullOrEmpty(_state.Error) ? "" : _text.Label(_state.HasSnapshotWarning && _state.LastOperationSucceeded ? "reason.refresh_error" : "reason.error");
            for (int i = 0; i < _status.Length; i++)
            {
                S0UiFactory.SetBoxed(_status[i], status);
                S0UiFactory.SetBoxed(_errors[i], error);
            }
            BindNew(idle);
            if (_state.Load.Slots != null)
                for (int i = 0; i < _loadRows.Length; i++)
                {
                    CareerSlotSummary row = _state.Load.Slots[i];
                    S0UiFactory.Set(_loadRows[i], _text.Slot(row));
                    _loadButtons[i].interactable = idle && row.IsLoadable;
                    if (i > 0) S0UiFactory.Set(_saveRows[i - 1], _text.Slot(row));
                }
            if (!_state.Home.HasCareer) return;
            CareerHomeViewModel home = _state.Home;
            S0UiFactory.Set(_summary, _text.Summary(home));
            S0UiFactory.Set(_changedText, _text.Changed(home));
            S0UiFactory.Set(_attention, _text.Attention(home));
            S0UiFactory.Set(_next, _text.Next(home));
            _ui.SetButton(_progress, _text.Progress(home.NextAction));
            S0UiFactory.SetBoxed(_autosaveError, string.IsNullOrEmpty(home.AutosaveError) ? "" :
                _text.Label("reason.autosave_error"));
            while (_table.Count < home.Table.Count) _table.Add(_ui.Label(_tableList, ""));
            for (int i = 0; i < _table.Count; i++)
            {
                _table[i].gameObject.SetActive(i < home.Table.Count);
                if (i < home.Table.Count) S0UiFactory.Set(_table[i], _text.TableRow(home, i));
            }
        }

        private void BindNew(bool idle)
        {
            NewCareerViewModel model = _state.New;
            int count = model.Clubs?.Count ?? 0;
            while (_clubs.Count < count)
            {
                int index = _clubs.Count;
                _clubs.Add(Button(_clubList, "", () => _state.SelectClub(_state.New.Clubs[index].ClubId), _newScroll, true));
            }
            string selected = "";
            for (int i = 0; i < _clubs.Count; i++)
            {
                _clubs[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                var club = model.Clubs[i];
                bool chosen = model.HasSelection && model.SelectedClubId == club.ClubId;
                _ui.SetButton(_clubs[i], _text.Format("club." + i, "new.club", club.Name));
                _ui.SetSelected(_clubs[i], chosen);
                _clubs[i].interactable = idle;
                if (chosen) selected = _text.Format("selected", "new.selected", club.Name, model.DefaultObjectivePosition);
            }
            S0UiFactory.Set(_selected, selected);
        }

        private static ulong NewSeed()
        {
            var bytes = new byte[sizeof(ulong)];
            using (RandomNumberGenerator entropy = RandomNumberGenerator.Create()) entropy.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-06 | —      | P7b shared wrapped/scrolled pages and retained controls. |
// | 1.1     | 2026-11-06 | —      | P7b review: retain focus/scroll, restore invokers, localize errors and confirm unsaved quit. |
// | 1.2     | 2026-11-10 | —      | Keep Continue secondary to the approved Demo match entry. |
#endregion
