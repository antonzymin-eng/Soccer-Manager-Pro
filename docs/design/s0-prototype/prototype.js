/* Created: 2026-09-30. Purpose: complete simulated S0 journey; never dispatches production commands. */
(() => {
  'use strict';
  const M = window.S0Model;
  const reference = window.S0Reference;
  const params = new URLSearchParams(location.search);
  const fixture = params.get('fixture') || 'ordinary';
  const app = document.getElementById('app');
  const announcement = document.getElementById('announcement');
  const t = text => params.has('pseudo') ? `[${text} ${'~'.repeat(Math.ceil(text.length * .4))}]` : text;
  const escape = value => String(value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const text = value => escape(t(value));
  const button = (id, label, disabled = false, primary = false) => `<button id="${id}" type="button" ${disabled ? 'disabled' : ''} ${primary ? 'class="primary"' : ''}>${text(label)}</button>`;
  const options = () => M.mentalities.map((value, index) => `<option value="${index}">${text(value)}</option>`).join('');
  let state = M.initial();
  let invoker = null;
  let initialTicks = 0;
  const dialog = document.createElement('dialog');
  dialog.setAttribute('aria-labelledby', 'dialog-heading');
  document.body.append(dialog);
  const scale = Number(params.get('scale') || 1);
  if (scale >= 1 && scale <= 3) {
    document.documentElement.style.fontSize = `${16 * scale}px`;
    if (scale >= 1.5) document.documentElement.dataset.largeText = 'true';
  }
  if (params.has('state')) {
    const target = params.get('state');
    if (['MM', 'TS', 'MV-0', 'MV-L', 'MV-P', 'MV-FT', 'PR'].includes(target)) {
      state.screen = target;
      state.minute = ['MV-FT', 'PR'].includes(target) ? 90 : target.startsWith('MV-') && target !== 'MV-0' ? 24 : 0;
    }
  }
  if (fixture === 'fault') state.faultMinute = 18;
  if (fixture === 'limit') state.usedBench = [0, 1, 2, 3, 4];
  if (fixture === 'pending-end') state.requests = [{ kind: 'mentality', value: 5, status: 'Pending', minute: 89 }];
  function seedHistory() {
    if (fixture === 'events') state.requests = Array.from({ length: 15 }, (_, i) => ({ kind: 'mentality', value: state.mentality, status: 'Applied', minute: i + 1 }));
  }
  seedHistory();
  if (state.screen === 'MV-FT' || state.screen === 'PR') state = M.reduce(state, { type: 'FULL_TIME' });
  const identity = side => fixture === 'long-names' ? `${side} — ${t('Identity length stress fixture: a deliberately long football team identifier')}` : t(side);
  function sample(minute = state.minute) {
    const samples = reference.snapshots;
    return samples.reduce((result, frame) => frame.minute <= minute + .001 ? frame : result, samples[0]);
  }
  function send(action, message = '') {
    state = M.reduce(state, action);
    render();
    if (message) announcement.textContent = t(message);
  }
  function statTable(partial = false) {
    const data = sample(partial ? state.faultMinute : state.minute);
    const rows = [['Goals recorded', 'goals'], ['Possession %', 'possession'], ['Territorial %', 'territory'],
      ['Fouls', 'fouls'], ['Yellow cards', 'yellow'], ['Red cards', 'red'], ['Offsides', 'offsides'],
      ['Corners', 'corners'], ['Throw-ins', 'throwIns'], ['Goal kicks', 'goalKicks'], ['Substitutions', 'substitutions']];
    if (data.home.xgAvailable && data.away.xgAvailable) rows.push(['Expected goals (xG)', 'xg']);
    const format = value => Number.isInteger(value) ? value : value.toFixed(1);
    return `<table><caption>${text(partial ? `Partial figures through minute ${state.faultMinute} — not full-match totals` : 'Captured statistics snapshot')}</caption>
      <thead><tr><th scope="col">${text('Statistic')}</th><th scope="col">${escape(identity('Home'))}</th><th scope="col">${escape(identity('Away'))}</th></tr></thead>
      <tbody>${rows.map(([label, key]) => `<tr><th scope="row">${text(label)}</th><td>${format(data.home[key])}</td><td>${format(data.away[key])}</td></tr>`).join('')}</tbody></table>
      <p>${text('Possession shares include loose-ball time; the two teams need not total 100%.')}</p>`;
  }
  function healthNotice(report = false) {
    return state.faultMinute === null ? '' : `<p class="warning" role="status">${text(report
      ? `Statistics incomplete — stopped at minute ${state.faultMinute}. Final score remains available.`
      : `Statistics stopped at minute ${state.faultMinute}. The match continues; these figures are incomplete.`)}</p>`;
  }
  function feedback() {
    const entry = r => {
      const change = r.kind === 'mentality' ? `Mentality: ${M.mentalities[r.value]}` : `Home shirt ${r.out + 1} → bench slot ${r.bench}`;
      const refusal = r.status === 'Refused' ? ' — the request was refused. Last applied value is unchanged; choose again.' : '';
      const paused = r.status === 'Pending' && state.screen === 'MV-P' ? ' — waiting; resume to continue' : '';
      return `<p class="feedback">${text(`${change} — ${r.status}${r.status === 'Applied' ? ` at minute ${r.minute}` : ''}${refusal}${paused}`)}</p>`;
    };
    return state.requests.length ? `<div class="stack">${state.requests.slice(-3).map(entry).join('')}
      ${state.requests.length > 3 ? `<details><summary>${text(`Earlier change feedback (${state.requests.length - 3})`)}</summary>${state.requests.slice(0, -3).map(entry).join('')}</details>` : ''}</div>` : '';
  }
  function pitch() {
    if (state.screen === 'MV-0') return `<div class="pitch"><p class="pitch-waiting">${text('Match starting — waiting for the first frame')}</p></div>`;
    const frame = sample();
    const marks = frame.agents.filter(a => !a.sentOff).map(a => `<span class="agent ${a.id >= 11 ? 'away' : ''}" style="left:${Math.max(2, Math.min(98, a.x / 105 * 100))}%;top:${Math.max(3, Math.min(97, a.y / 68 * 100))}%">${a.id >= 11 ? 'A' : 'H'}${a.id % 11 + 1}</span>`).join('');
    return `<div class="pitch" role="img" aria-label="${text('Captured pitch snapshot. Home markers H, away markers A, ball dot; not an interactive selector.')}">${marks}<span class="ball" style="left:${frame.ball[0] / 105 * 100}%;top:${frame.ball[1] / 68 * 100}%" aria-hidden="true">●</span></div>
      <p>${text(`Possession: ${frame.possessing < 0 ? 'loose ball' : `${frame.possessing < 11 ? 'Home' : 'Away'} shirt ${frame.possessing % 11 + 1}`}. Restart: ${frame.restart}.`)}</p>`;
  }
  function render() {
    const oldFocus = document.activeElement?.id;
    if (!reference?.snapshots?.length) {
      app.innerHTML = '<h1>Prototype data unavailable</h1><p>The captured reference file must be present beside this page.</p>';
      return;
    }
    if (state.screen === 'MM') {
      app.innerHTML = `<section class="entry stack"><h1 id="page-heading" tabindex="-1">System XI</h1><p>${text('Manage the home side in one demo match. The opponent is AI-managed.')}</p><div>${button('open-setup', 'Play a demo match', false, true)}</div></section>`;
    } else if (state.screen === 'TS') {
      app.innerHTML = `<section class="entry stack"><h1 id="page-heading" tabindex="-1">${text('Tactics Setup')}</h1><p>${text('You manage Home. Away is AI-managed.')}</p>
        <fieldset><legend>${text('Home Mentality — choose one')}</legend>${M.mentalities.map((m, i) => `<label><input type="radio" name="mentality" value="${i}" ${state.draft === i ? 'checked' : ''}><span>${text(m)} — ${text(M.consequences[i])}</span></label>`).join('')}</fieldset>
        <p>${text(`Ready to start with ${M.mentalities[state.draft]}.`)}</p><div class="row">${button('start', 'Start match', false, true)}${button('back', 'Back')}</div></section>`;
    } else if (state.screen === 'PR') {
      const score = fixture === 'scoreline' ? [12, 10] : sample(90).score;
      app.innerHTML = `<section class="entry stack"><h1 id="page-heading" tabindex="-1">${text('Post-Match Report')}</h1><p class="context">${text('Full time — Home result: ' + (score[0] > score[1] ? 'Win' : score[0] < score[1] ? 'Loss' : 'Draw'))}</p>
        <div class="scoreboard"><span class="identity">${escape(identity('Home'))}</span><span class="score">${score[0]} – ${score[1]}</span><span class="identity away">${escape(identity('Away'))}</span></div>
        ${healthNotice(true)}${state.faultMinute === null ? statTable() : `<details><summary>${text('Show partial statistics — incomplete')}</summary>${statTable(true)}</details>`}
        <div>${button('return', 'Return to main menu', false, true)}</div></section>`;
    } else {
      const waiting = state.screen === 'MV-0';
      const ended = state.screen === 'MV-FT';
      const locked = waiting || ended;
      const score = fixture === 'scoreline' ? [12, 10] : sample().score;
      const period = waiting ? 'Waiting for first frame' : ended ? 'Full time' : state.minute < 45 ? 'First half' : 'Second half';
      const reason = waiting ? 'Match starting — controls unavailable until the first frame.' : ended ? 'Match ended — playback and team changes are unavailable.' : '';
      const pendingTactic = state.requests.some(r => r.kind === 'mentality' && r.status === 'Pending');
      app.innerHTML = `<section class="stack"><h1 id="page-heading" tabindex="-1">${text('Match View')}</h1>
        <div class="panel stack"><div class="scoreboard"><span class="identity">${escape(identity('Home'))}</span><span class="score">${waiting ? '—' : `${score[0]} – ${score[1]}`}</span><span class="identity away">${escape(identity('Away'))}</span></div>
        <div class="row context"><span>${text(period)}</span><span>${text(waiting ? 'Clock awaiting first frame' : `Minute ${state.minute}`)}</span><span>${text(`Selected speed ${M.speeds[state.speed]}×${state.screen === 'MV-P' ? ' • Paused' : ''}`)}</span><span>${text(`Current Mentality: ${M.mentalities[state.mentality]}`)}</span></div>
        ${reason ? `<p class="reason">${text(reason)}</p>` : ''}${ended ? button('report', 'View match report', false, true) : ''}</div>
        <div class="layout"><section class="stack"><div class="row">${button('slower', 'Slower', locked || state.speed === 0)}${button('pause', state.screen === 'MV-P' ? 'Resume' : 'Pause', locked)}${button('faster', 'Faster', locked || state.speed === 3)}</div>
        <p>${text(locked ? reason : state.speed === 0 ? 'Already at real time — slower is unavailable.' : state.speed === 3 ? 'Already at the fastest speed — faster is unavailable.' : 'Speed steps: 1×, 3×, 5×, 10×.')}</p>
        ${pitch()}${params.has('captions') ? `<p class="caption-reservation">${text('Future caption region — layout stress fixture only (audio runtime unavailable)')}</p>` : ''}</section>
        <aside class="stack"><section class="panel stack"><h2>${text('Home team changes')}</h2><p>${text(`Current Mentality: ${M.mentalities[state.mentality]}`)}</p>${button('tactic', 'Change Mentality', locked || pendingTactic)}
        ${pendingTactic ? `<p>${text('Mentality request pending — another request is unavailable until resolved.')}</p>` : ''}
        ${button('substitute', 'Make substitution', locked || state.usedBench.length >= 5 || state.requests.some(r => r.kind === 'substitution' && r.status === 'Pending'))}
        <p>${text(`Substitutions used: ${state.usedBench.length} / 5${state.usedBench.length >= 5 ? ' — all substitutions used' : ''}`)}</p>${feedback()}</section>
        <section class="panel stack"><h2>${text('Match statistics')}</h2>${healthNotice()}${button('stats', state.statsOpen ? 'Close statistics' : 'Open statistics', locked)}
        ${state.statsOpen ? statTable(state.faultMinute !== null) : ''}</section></aside></div></section>`;
    }
    bindActions();
    if (oldFocus && !dialog.open) document.getElementById(oldFocus)?.focus({ preventScroll: true });
  }
  function on(id, handler) { document.getElementById(id)?.addEventListener('click', handler); }
  function navigate(action) { send(action); document.getElementById('page-heading')?.focus(); }
  function bindActions() {
    on('open-setup', () => navigate({ type: 'OPEN_SETUP' }));
    on('back', () => navigate({ type: 'CANCEL_SETUP' }));
    on('start', () => { initialTicks = 0; navigate({ type: 'START' }); seedHistory(); render(); });
    on('report', () => navigate({ type: 'REPORT' }));
    on('return', () => navigate({ type: 'RETURN' }));
    on('pause', () => send({ type: 'PAUSE' }, state.screen === 'MV-L' ? 'Paused. Selected speed retained.' : 'Resumed.'));
    on('slower', () => send({ type: 'SPEED', step: -1 }, 'Playback speed changed.'));
    on('faster', () => send({ type: 'SPEED', step: 1 }, 'Playback speed changed.'));
    on('stats', () => send({ type: 'STATS' }));
    on('tactic', () => openDialog('mentality', 'tactic'));
    on('substitute', () => openDialog('substitution', 'substitute'));
    app.querySelectorAll('input[name="mentality"]').forEach(input => input.addEventListener('change', () => {
      state = M.reduce(state, { type: 'DRAFT', value: Number(input.value) });
      render();
      app.querySelector(`input[name="mentality"][value="${input.value}"]`).focus();
    }));
  }
  function openDialog(kind, id) {
    invoker = id;
    const frame = sample();
    const starters = Array.from({ length: 11 }, (_, i) => i).filter(i => !state.usedOut.includes(i) && !frame.agents[i].sentOff && frame.agents[i].benchSlot < 0);
    const bench = Array.from({ length: 7 }, (_, i) => i).filter(i => !state.usedBench.includes(i));
    dialog.innerHTML = `<h2 id="dialog-heading">${text(kind === 'mentality' ? 'Change Home Mentality' : 'Make Home substitution')}</h2>
      ${kind === 'mentality' ? `<label for="new-mentality">${text('Requested Mentality')}</label><select id="new-mentality">${options()}</select><p id="choice-help"></p>`
        : `<p>${text('Choose by shirt number and unused bench slot. The current engine applies the swap immediately when processed; it does not wait for a stoppage.')}</p>
          <label for="outgoing">${text('Outgoing home player')}</label><select id="outgoing">${starters.map(i => `<option value="${i}">${text(`Home shirt ${i + 1}${i === 0 ? ' — goalkeeper' : ''}`)}</option>`).join('')}</select>
          <label for="incoming">${text('Incoming bench player')}</label><select id="incoming">${bench.map(i => `<option value="${i}">${text(`Bench slot ${i}`)}</option>`).join('')}</select>`}
      <p>${text('Selection has no effect until submitted. Submission requests the change; feedback confirms the outcome.')}</p>
      <div class="row">${button('submit-change', 'Submit change', !M.active(state) || (kind === 'substitution' && (!starters.length || !bench.length)), true)}${button('cancel-change', 'Cancel')}</div>`;
    if (kind === 'mentality') {
      const select = dialog.querySelector('select');
      select.value = state.mentality;
      const explain = () => { document.getElementById('choice-help').textContent = t(`${M.mentalities[select.value]}: ${M.consequences[select.value]}`); };
      select.addEventListener('change', explain); explain();
    }
    document.getElementById('submit-change').addEventListener('click', () => {
      const request = kind === 'mentality' ? { kind, value: Number(document.getElementById('new-mentality').value) }
        : { kind, out: Number(document.getElementById('outgoing').value), bench: Number(document.getElementById('incoming').value) };
      closeDialog(); send({ type: 'REQUEST', request }, 'Change requested — pending.');
    });
    document.getElementById('cancel-change').addEventListener('click', closeDialog);
    dialog.showModal();
    dialog.querySelector('select')?.focus();
  }
  function closeDialog() { dialog.close(); document.getElementById(invoker)?.focus(); }
  dialog.addEventListener('cancel', event => { event.preventDefault(); closeDialog(); });
  dialog.addEventListener('keydown', event => {
    if (event.key !== 'Tab') return;
    const controls = [...dialog.querySelectorAll('select, button:not([disabled])')];
    const first = controls[0], last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  });
  render();
  setInterval(() => {
    if (params.has('review')) return;
    if (state.screen === 'MV-0' && fixture !== 'waiting') {
      if (++initialTicks >= 2) send({ type: 'FIRST_FRAME' }, 'First frame received. Match live.');
    } else if (state.screen === 'MV-L') {
      if (fixture === 'fault' && state.minute >= 18) state.faultMinute = 18;
      send({ type: 'ADVANCE', minutes: M.speeds[state.speed], refuse: fixture === 'refusal' });
      if (state.screen === 'MV-FT') {
        if (dialog.open) closeDialog();
        announcement.textContent = t('Full time. Match ended. View match report is available.');
        document.getElementById('report')?.focus();
      }
    }
  }, 1000);
})();
