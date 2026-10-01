/* Created: 2026-09-30. Purpose: complete simulated S0 journey; never dispatches production commands. */
(() => {
  'use strict';
  const M = window.S0Model;
  const params = new URLSearchParams(location.search);
  const fixture = params.get('fixture') || 'ordinary';
  const captured = fixture === 'scoreline';
  const reference = captured ? window.S0Reference : window.S0Scenario;
  if (captured) document.querySelector('.prototype-notice').textContent = 'Design reference H v0.2 • Unmodified reference capture: unusual 19–9 match. All interactions are simulated; substitutions use an illustrative lineup overlay, never a recomputed match. Unity binding remains pending UX Gate I.';
  const app = document.getElementById('app');
  // Programmatic focus targets show their ring only after keyboard input (S0-H-005).
  const modality = mode => () => { document.documentElement.dataset.input = mode; };
  window.addEventListener('keydown', modality('keyboard'), true);
  window.addEventListener('pointerdown', modality('pointer'), true);
  const announcement = document.getElementById('announcement');
  const t = text => params.has('pseudo') ? `[${text} ${'~'.repeat(Math.ceil(text.length * .4))}]` : text;
  const escape = value => String(value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const text = value => escape(t(value));
  const button = (id, label, disabled = false, primary = false) => `<button id="${id}" type="button" ${disabled ? 'disabled' : ''} ${primary ? 'class="primary"' : ''}>${text(label)}</button>`;
  document.querySelector('.header-context').textContent = t('One match · Home manager');
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
  function withFixtureFault(current) {
    return fixture === 'fault' && current.faultMinute === null && current.minute >= 18
      ? M.reduce(current, { type: 'FAULT', minute: 18 }) : current;
  }
  state = withFixtureFault(state);
  if (fixture === 'limit') state.usedBench = [0, 1, 2, 3, 4];
  if (fixture === 'pending-end') state.requests = [{ kind: 'mentality', value: 5, status: 'Pending', minute: 89 }];
  function seedHistory() {
    if (fixture === 'events' && state.minute >= 15) state.requests = Array.from({ length: 15 }, (_, i) => ({ kind: 'mentality', value: state.mentality, status: 'Applied', minute: i + 1 }));
  }
  seedHistory();
  if (state.screen === 'MV-FT' || state.screen === 'PR') state = M.reduce(state, { type: 'FULL_TIME' });
  const identity = side => t(fixture === 'long-names' ? `${side} — Identity length stress fixture: a deliberately long football team identifier` : side);
  function sample(minute = state.minute) {
    const samples = reference.snapshots;
    return samples.reduce((result, frame) => frame.minute <= minute + .001 ? frame : result, samples[0]);
  }
  function send(action, message = '') {
    state = withFixtureFault(M.reduce(state, action));
    render();
    if (message) announcement.textContent = t(message);
  }
  function statTable(partial = false) {
    const data = sample(partial ? state.faultMinute : state.minute);
    const rows = [['Goals recorded', 'goals'], ['Possession %', 'possession'], ['Territorial %', 'territory'],
      ['Fouls', 'fouls'], ['Yellow cards', 'yellow'], ['Red cards', 'red'], ['Offsides', 'offsides'],
      ['Corners', 'corners'], ['Throw-ins', 'throwIns'], ['Goal kicks', 'goalKicks']];
    if (data.home.xgAvailable && data.away.xgAvailable) rows.push(['Expected goals (xG)', 'xg']);
    const format = value => Number.isInteger(value) ? value : value.toFixed(1);
    return `<table><caption>${text((captured ? 'Captured statistics snapshot' : 'Synthetic prototype statistics') + (partial ? ` — partial figures through minute ${state.faultMinute} — not full-match totals` : ''))}</caption>
      <thead><tr><th scope="col">${text('Statistic')}</th><th scope="col">${escape(identity('Home'))}</th><th scope="col">${escape(identity('Away'))}</th></tr></thead>
      <tbody>${rows.map(([label, key]) => `<tr><th scope="row">${text(label)}</th><td>${format(data.home[key])}</td><td>${format(data.away[key])}</td></tr>`).join('')}</tbody></table>
      <p>${text('Possession shares include loose-ball time; the two teams need not total 100%.')}</p>`;
  }
  function healthNotice(report = false) {
    return state.faultMinute === null ? '' : `<p class="warning" role="status">${text(report || state.screen === 'MV-FT'
      ? `Statistics incomplete — stopped at minute ${state.faultMinute}. Final score remains available.`
      : `Statistics stopped at minute ${state.faultMinute}. The match continues; these figures are incomplete.`)}</p>`;
  }
  function feedback() {
    // Every outcome carries an explicit label; colour and glyph only reinforce it (S0-H-007).
    const entry = r => {
      const change = r.kind === 'mentality' ? `Mentality: ${M.mentalities[r.value]}` : `Home shirt ${r.out + 1} → Home shirt ${12 + r.bench} (bench slot ${r.bench + 1})`;
      const expired = r.status.startsWith('Not applied');
      const [label, tone, detail] = r.status === 'Applied' ? ['Applied', 'success', `at minute ${r.minute}`]
        : r.status === 'Refused' ? ['Refused', 'failure', r.kind === 'mentality' ? 'current Mentality unchanged' : 'substitution count unchanged']
        : expired ? ['Not applied', 'expired', 'match ended before it could apply']
        : ['Pending', 'pending', state.screen === 'MV-P' ? 'waiting; resume to continue' : 'waiting to be applied'];
      return `<p class="feedback ${tone}" data-outcome="${label}"><span class="outcome">${text(label)}</span> <span class="outcome-detail">${text(`${change} — ${detail}`)}</span></p>`;
    };
    return state.requests.length ? `<div id="request-feedback" tabindex="-1" class="stack" aria-label="${text('Change request feedback')}">${state.requests.slice(-3).map(entry).join('')}
      ${state.requests.length > 3 ? `<details id="earlier-feedback"><summary id="earlier-feedback-toggle">${text(`Earlier change feedback (${state.requests.length - 3})`)}</summary>${state.requests.slice(0, -3).map(entry).join('')}</details>` : ''}</div>` : '';
  }
  function pitch() {
    if (state.screen === 'MV-0') return `<div class="pitch pitch-empty"><p class="pitch-waiting">${text('Pitch appears after the first frame.')}</p></div>`;
    const frame = sample();
    // Three metres of goal margin plus a one-metre touchline margin in the drawing.
    const position = (x, y) => `left:${(x + 3) / 111 * 100}%;top:${(y + 1) / 70 * 100}%`;
    // Illustrative identities occupy the outgoing player's captured position. No engine data is changed.
    const applied = state.requests.filter(r => r.kind === 'substitution' && r.status === 'Applied');
    const marks = frame.agents.filter(a => !a.sentOff).map(a => {
      const replacement = a.id < 11 ? applied.find(r => r.out === a.id) : null;
      const shirt = replacement ? 12 + replacement.bench : a.id % 11 + 1;
      const label = `${a.id >= 11 ? 'Away' : 'Home'} shirt ${shirt}${replacement ? ' — illustrative applied substitution' : ''}`;
      return `<span class="agent ${a.id >= 11 ? 'away' : ''} ${replacement ? 'replacement' : ''}" data-agent-id="${a.id}" data-x="${a.x}" data-y="${a.y}" title="${text(label)}" style="${position(a.x, a.y)}">${a.id >= 11 ? 'A' : 'H'}${shirt}${replacement ? ' <span class="sub-mark">↔</span>' : ''}</span>`;
    }).join('');
    const field = `<svg class="pitch-lines" viewBox="-3 -1 111 70" preserveAspectRatio="none" aria-hidden="true">
      <rect x="0" y="0" width="105" height="68"/><path d="M52.5 0V68"/><circle cx="52.5" cy="34" r="9.15"/>
      <rect x="0" y="13.84" width="16.5" height="40.32"/><rect x="88.5" y="13.84" width="16.5" height="40.32"/>
      <rect x="0" y="24.84" width="5.5" height="18.32"/><rect x="99.5" y="24.84" width="5.5" height="18.32"/>
      <rect class="goal" x="-2" y="30.34" width="2" height="7.32"/><rect class="goal" x="105" y="30.34" width="2" height="7.32"/>
      <circle cx="11" cy="34" r=".25"/><circle cx="94" cy="34" r=".25"/>
    </svg>`;
    return `<div class="pitch" role="img" aria-label="${text('Illustrative lineup on captured pitch positions. Home H attacks right; away A attacks left. Goals and penalty areas shown. Labels may move slightly to avoid overlap; ball dot.')}">${field}${marks}<span class="ball" style="${position(frame.ball[0], frame.ball[1])}" aria-hidden="true">●</span></div>
      <p class="pitch-caption">${text('Illustrative lineup on captured positions. H/A identify Home/Away shirt numbers; a white-outlined ↔ marker is an applied simulated substitution. Labels may move slightly to avoid overlap.')}</p>`;
  }
  function layoutPitchMarkers() {
    const field = document.querySelector('.pitch:not(.pitch-empty)');
    if (!field) return;
    const width = field.clientWidth, height = field.clientHeight;
    const placed = [];
    const ns = 'http://www.w3.org/2000/svg';
    field.querySelector('.marker-leaders')?.remove();
    const leaders = document.createElementNS(ns, 'svg');
    leaders.classList.add('marker-leaders');
    leaders.setAttribute('viewBox', `0 0 ${width} ${height}`);
    leaders.setAttribute('preserveAspectRatio', 'none');
    leaders.setAttribute('aria-hidden', 'true');
    field.prepend(leaders);
    for (const marker of field.querySelectorAll('.agent')) {
      const source = { x: (Number(marker.dataset.x) + 3) / 111 * width,
        y: (Number(marker.dataset.y) + 1) / 70 * height };
      const halfWidth = marker.offsetWidth / 2, halfHeight = marker.offsetHeight / 2;
      const clamp = (x, y) => ({ x: Math.max(halfWidth + 3, Math.min(width - halfWidth - 3, x)),
        y: Math.max(halfHeight + 3, Math.min(height - halfHeight - 3, y)) });
      const clear = p => placed.every(r => Math.abs(p.x - r.x) >= halfWidth + r.w + 4 || Math.abs(p.y - r.y) >= halfHeight + r.h + 4);
      let target = clamp(source.x, source.y);
      // Stable DOM/agent order; search nearest rings without moving the source data.
      for (let ring = 1; !clear(target) && ring <= 60; ring++) {
        const candidates = [];
        for (let i = -ring; i <= ring; i++) {
          candidates.push(clamp(source.x + i * 8, source.y - ring * 8), clamp(source.x + i * 8, source.y + ring * 8));
          candidates.push(clamp(source.x - ring * 8, source.y + i * 8), clamp(source.x + ring * 8, source.y + i * 8));
        }
        candidates.sort((a, b) => (a.x - source.x) ** 2 + (a.y - source.y) ** 2 - (b.x - source.x) ** 2 - (b.y - source.y) ** 2);
        const available = candidates.find(clear);
        if (available) { target = available; break; }
      }
      marker.style.left = `${target.x}px`; marker.style.top = `${target.y}px`;
      placed.push({ ...target, w: halfWidth, h: halfHeight });
      if (Math.hypot(target.x - source.x, target.y - source.y) > Math.max(halfWidth, halfHeight) + 4) {
        const line = document.createElementNS(ns, 'line');
        for (const [name, value] of Object.entries({ x1: source.x, y1: source.y, x2: target.x, y2: target.y })) line.setAttribute(name, value);
        leaders.append(line);
      }
    }
  }
  function render() {
    const oldFocus = document.activeElement?.id;
    const earlierOpen = document.getElementById('earlier-feedback')?.open || false;
    if (!reference?.snapshots?.length) {
      app.innerHTML = '<h1>Prototype data unavailable</h1><p>The captured reference file must be present beside this page.</p>';
      return;
    }
    if (state.screen === 'MM') {
      app.innerHTML = `<section class="entry stack menu-entry"><p class="eyebrow">${text('From the touchline')}</p><h1 id="page-heading" tabindex="-1">System XI</h1><p>${text('Manage the home side in one demo match. The opponent is AI-managed.')}</p><div>${button('open-setup', 'Play a demo match', false, true)}</div></section>`;
    } else if (state.screen === 'TS') {
      app.innerHTML = `<section class="entry stack"><h1 id="page-heading" tabindex="-1">${text('Tactics Setup')}</h1><p>${text('You manage Home. Away is AI-managed.')}</p>
        <fieldset><legend>${text('Home Mentality — choose one')}</legend>${M.mentalities.map((m, i) => `<label><input type="radio" name="mentality" value="${i}" ${state.draft === i ? 'checked' : ''}><span>${text(m)} — ${text(M.consequences[i])}</span></label>`).join('')}</fieldset>
        <p>${text(`Ready to start with ${M.mentalities[state.draft]}.`)}</p><div class="row">${button('start', 'Start match', false, true)}${button('back', 'Back')}</div></section>`;
    } else if (state.screen === 'PR') {
      const score = sample(90).score;
      app.innerHTML = `<section class="entry stack"><h1 id="page-heading" tabindex="-1">${text('Post-Match Report')}</h1><p class="context">${text('Full time — Home result: ' + (score[0] > score[1] ? 'Win' : score[0] < score[1] ? 'Loss' : 'Draw'))}</p>
        <div class="scoreboard"><span class="identity">${escape(identity('Home'))}</span><span class="score">${score[0]} – ${score[1]}</span><span class="identity away">${escape(identity('Away'))}</span></div>
        ${healthNotice(true)}${state.faultMinute === null ? statTable() : `<details><summary>${text('Show partial statistics — incomplete')}</summary>${statTable(true)}</details>`}
        <div>${button('return', 'Return to main menu', false, true)}</div></section>`;
    } else {
      const waiting = state.screen === 'MV-0';
      const ended = state.screen === 'MV-FT';
      const locked = waiting || ended;
      const score = sample().score;
      const period = waiting ? 'Waiting for first frame' : ended ? 'Full time' : state.minute < 45 ? 'First half' : 'Second half';
      const reason = waiting ? 'Match starting — controls unavailable until the first frame.' : ended ? 'Match ended — playback and team changes are unavailable.' : '';
      const pendingTactic = state.requests.some(r => r.kind === 'mentality' && r.status === 'Pending');
      app.innerHTML = `<section class="stack match-view"><h1 id="page-heading" tabindex="-1">${text('Match View')}</h1>
        <div class="panel stack"><div class="scoreboard"><span class="identity">${escape(identity('Home'))}</span><div class="score-block"><span class="score">${waiting ? '—' : `${score[0]} – ${score[1]}`}</span><p class="clock"><span>${text(period)}</span><span>${text(waiting ? 'Clock awaiting first frame' : `Minute ${state.minute}`)}</span></p></div><span class="identity away">${escape(identity('Away'))}</span></div>
        <p class="match-meta context"><span>${text(`Selected speed ${M.speeds[state.speed]}×${state.screen === 'MV-P' ? ' • Paused' : ''}`)}</span><span>${text(`Current Mentality: ${M.mentalities[state.mentality]}`)}</span></p>
        ${reason ? `<div class="reason-row"><p class="reason">${text(reason)}</p>${ended ? button('report', 'View match report', false, true) : ''}</div>` : ''}</div>
        <div class="layout"><section class="stack"><div class="control-bar">${button('slower', 'Slower', locked || state.speed === 0)}${button('pause', state.screen === 'MV-P' ? 'Resume' : 'Pause', locked)}${button('faster', 'Faster', locked || state.speed === 3)}
        ${!locked ? `<p>${text(state.speed === 0 ? 'Already at real time — slower is unavailable.' : state.speed === 3 ? 'Already at the fastest speed — faster is unavailable.' : 'Speed steps: 1×, 3×, 5×, 10×.')}</p>` : ''}
        ${state.screen === 'MV-0' ? '' : `<p class="pitch-direction">${text('Home attacks right → • Away attacks left ←')}</p>`}</div>
        ${pitch()}${params.has('captions') ? `<p class="caption-reservation">${text('Future caption region — layout stress fixture only (audio runtime unavailable)')}</p>` : ''}</section>
        <aside class="stack"><section class="panel stack"><h2>${text('Home team changes')}</h2><p>${text(`Current Mentality: ${M.mentalities[state.mentality]}`)}</p>${button('tactic', 'Change Mentality', locked || pendingTactic)}
        ${pendingTactic ? `<p>${text('Mentality request pending — another request is unavailable until resolved.')}</p>` : ''}
        ${button('substitute', 'Make substitution', locked || state.usedBench.length >= 5 || state.requests.some(r => r.kind === 'substitution' && r.status === 'Pending'))}
        <p>${text(`Substitutions used: ${state.usedBench.length} / 5${state.usedBench.length >= 5 ? ' — all substitutions used' : ''} (simulated requests)`)}</p>${feedback()}</section>
        <section class="panel stack"><h2>${text('Match statistics')}</h2>${healthNotice()}${ended ? `<p class="statistics-end">${text(state.statsOpen ? 'Statistics panel retained at full time.' : 'Statistics panel closed at full time.')}</p>` : button('stats', state.statsOpen ? 'Close statistics' : 'Open statistics', locked)}${ended && state.faultMinute === null ? `<p>${text('Final statistics are available in the match report.')}</p>` : ''}
        ${state.statsOpen ? statTable(state.faultMinute !== null) : ''}</section></aside></div></section>`;
    }
    bindActions();
    layoutPitchMarkers();
    const earlier = document.getElementById('earlier-feedback');
    if (earlier) earlier.open = earlierOpen;
    if (oldFocus && !dialog.open) {
      const target = document.getElementById(oldFocus);
      const fallback = ['tactic', 'substitute'].includes(oldFocus) ? 'request-feedback' : 'pause';
      const recovery = target && !target.disabled ? target : document.getElementById(state.screen === 'MV-FT' ? 'report' : fallback);
      recovery?.focus({ preventScroll: true });
    }
  }
  function on(id, handler) { document.getElementById(id)?.addEventListener('click', handler); }
  function navigate(action) { send(action); document.getElementById('page-heading')?.focus(); }
  function bindActions() {
    on('open-setup', () => navigate({ type: 'OPEN_SETUP' }));
    on('back', () => navigate({ type: 'CANCEL_SETUP' }));
    on('start', () => { initialTicks = 0; navigate({ type: 'START' }); });
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
      ${kind === 'mentality' ? `<p>${text(`Current Mentality: ${M.mentalities[state.mentality]}`)}</p><label for="new-mentality">${text('Requested Mentality')}</label><select id="new-mentality">${options()}</select><p id="choice-help" class="choice-help"></p><details id="mentality-comparison"><summary>${text('Compare all seven Mentalities')}</summary><ul class="choice-comparison">${M.mentalities.map((m, i) => `<li data-choice="${i}"><strong>${text(m)}</strong><span>${text(M.consequences[i])}</span><span class="choice-tags"></span></li>`).join('')}</ul></details>`
        : `<p>${text('Choose by shirt number and unused bench slot. The substitution takes effect when the request is applied; it does not wait for a stoppage.')}</p>
          <label for="outgoing">${text('Outgoing home player')}</label><select id="outgoing">${starters.map(i => `<option value="${i}">${text(`Home shirt ${i + 1}${i === 0 ? ' — goalkeeper' : ''}`)}</option>`).join('')}</select>
          <label for="incoming">${text('Incoming bench player')}</label><select id="incoming">${bench.map(i => `<option value="${i}">${text(`Home shirt ${12 + i} — bench slot ${i + 1}`)}</option>`).join('')}</select>`}
      <p>${text('Choose a change, then submit it. Your team changes only when you see Applied.')}</p>
      <div class="row">${button('submit-change', 'Submit change', !M.active(state) || (kind === 'substitution' && (!starters.length || !bench.length)), true)}${button('cancel-change', 'Cancel')}</div>`;
    if (kind === 'mentality') {
      const select = dialog.querySelector('select');
      select.value = state.mentality;
      const explain = () => {
        document.getElementById('choice-help').textContent = t(`${M.mentalities[select.value]}: ${M.consequences[select.value]}`);
        // Current is the applied value; Requested follows the dropdown and is not applied (S0-H-012).
        dialog.querySelectorAll('.choice-comparison li').forEach(item => {
          const index = Number(item.dataset.choice);
          const requested = index === Number(select.value);
          item.classList.toggle('requested', requested);
          item.querySelector('.choice-tags').innerHTML = `${index === state.mentality ? `<span class="choice-tag">${text('Current')}</span>` : ''}${requested ? `<span class="choice-tag requested-tag">${text('Requested')}</span>` : ''}`;
        });
      };
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
  window.addEventListener('resize', layoutPitchMarkers);
  render();
  document.getElementById('page-heading')?.focus();
  setInterval(() => {
    if (params.has('review')) return;
    if (state.screen === 'MV-0' && fixture !== 'waiting') {
      if (++initialTicks >= 2) {
        state = M.reduce(state, { type: 'FIRST_FRAME' });
        if (fixture === 'events') { state.minute = 15; seedHistory(); }
        render();
        announcement.textContent = t('First frame received. Match live.');
      }
    } else if (state.screen === 'MV-L') {
      send({ type: 'ADVANCE', minutes: M.speeds[state.speed], refuse: fixture === 'refusal' });
      if (state.screen === 'MV-FT') {
        if (dialog.open) closeDialog();
        announcement.textContent = t('Full time. Match ended. View match report is available.');
        document.getElementById('report')?.focus();
      }
    }
  }, 1000);
})();
