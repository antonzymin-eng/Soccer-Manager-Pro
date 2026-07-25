/* ============================================================================
   SQUAD — Special sub-tab view renderers
   Medical Center · Dynamics · Status
   Depends on globals from squad.js: PLAYERS, natFlag()
   ============================================================================ */

/* ── supplementary data ───────────────────────────────────────────────────── */
const INJURY_DETAILS = {
  6:  { type:'Hamstring strain',     part:'Left hamstring', grade:1, started:'24 Feb', est:'Wk 32', wksLeft:1, wksTotal:3,  status:'Recovering',    note:'Light training resumed. Cleared for contact Wk 32.' },
  23: { type:'MCL ligament sprain',  part:'Right knee',     grade:2, started:'3 Jan',  est:'Wk 34', wksLeft:3, wksTotal:12, status:'Rehabilitation', note:'Gym programme complete. Track sessions from Wk 33.' },
  9:  { type:'Lateral ankle sprain', part:'Right ankle',    grade:1, started:'8 Mar',  est:'Wk 32', wksLeft:1, wksTotal:2,  status:'Recovering',    note:'Swelling resolved. Light running resumed Friday.' },
};

const DYN = {
  teamSpirit:82, harmony:79, tension:21,
  captainN:8, vcN:18,
  moraleCounts:{ Excellent:12, Good:8, Content:5, Unhappy:3 },
  cliques:[
    { name:'Leadership core',  members:[8,18,1,19],      mood:'pos', desc:'Strong dressing room influence · high cohesion' },
    { name:'Young lions',      members:[37,11,35,15,52], mood:'pos', desc:'Energetic & ambitious · excellent morale' },
    { name:'Senior bloc',      members:[14,6,28,22,39],  mood:'neu', desc:'Stable · some contract uncertainty noted' },
    { name:'Fringe / concern', members:[23,9,7],         mood:'neg', desc:'2 injured · 1 seeking more game time' },
  ],
  influence:[
    { n:8,  role:'Captain',       pct:95 },
    { n:18, role:'Vice-captain',  pct:82 },
    { n:10, role:'Senior pro',    pct:76 },
    { n:6,  role:'Club legend',   pct:71 },
    { n:37, role:'Rising voice',  pct:58 },
  ],
};

/* ── shared helpers ───────────────────────────────────────────────────────── */
function svCard(title, meta, body) {
  return '<div class="sv-card">' +
    '<div class="sv-card-head">' +
      '<span class="sv-card-title">' + title + '</span>' +
      (meta ? '<span class="sv-meta">' + meta + '</span>' : '') +
    '</div>' +
    '<div class="sv-card-body">' + body + '</div>' +
  '</div>';
}

function svKpi(label, val, sub, cls) {
  return '<div class="sv-kpi' + (cls ? ' ' + cls : '') + '">' +
    '<div class="sv-kpi-v">' + val + '</div>' +
    '<div class="sv-kpi-l">' + label + '</div>' +
    (sub ? '<div class="sv-kpi-s">' + sub + '</div>' : '') +
  '</div>';
}

function gradeCls(g)  { return g >= 3 ? 'sv-g3' : g >= 2 ? 'sv-g2' : 'sv-g1'; }
function gradeText(g) { return 'Grade ' + (g || 1); }

/* ══════════════════════════════════════════════════════════════
   MEDICAL CENTER
══════════════════════════════════════════════════════════════ */
function renderMedicalView(el) {
  const injured      = PLAYERS.filter(p => p.inj);
  const avgFit       = Math.round(PLAYERS.reduce((s, p) => s + p.fit, 0) / PLAYERS.length);
  const totalWksLeft = injured.reduce((s, p) => s + (INJURY_DETAILS[p.n]?.wksLeft || 1), 0);

  const kpis =
    '<div class="sv-kpi-strip">' +
    svKpi('Injured',          injured.length,          'of 28 players',   'sv-kpi-neg') +
    svKpi('Weeks remaining',  totalWksLeft,            'combined est.',   '') +
    svKpi('Squad fitness',    avgFit + '%',            'all 28 players',  avgFit >= 85 ? 'sv-kpi-pos' : 'sv-kpi-neu') +
    svKpi('On full training', 28 - injured.length,     'fully available', 'sv-kpi-pos') +
    '</div>';

  /* injury cards */
  const injCards = injured.map(p => {
    const d      = INJURY_DETAILS[p.n] || {};
    const prog   = d.wksTotal ? Math.round((d.wksTotal - d.wksLeft) / d.wksTotal * 100) : 50;
    const urgCol = d.wksLeft <= 1 ? 'var(--pos-400)' : d.wksLeft <= 3 ? 'var(--neu-400)' : 'var(--neg-400)';
    return (
      '<div class="sv-inj-card">' +
        '<div class="sv-inj-top">' +
          '<div style="display:flex;align-items:center;gap:9px">' +
            '<span class="pos ' + p.posClass + '">' + p.pos + '</span>' +
            '<span class="sv-inj-name">' + p.name + '</span>' +
            '<span style="font-size:15px;line-height:1">' + natFlag(p.nat) + '</span>' +
          '</div>' +
          '<div style="display:flex;align-items:center;gap:6px">' +
            '<span class="sv-inj-grade ' + gradeCls(d.grade || 1) + '">' + gradeText(d.grade || 1) + '</span>' +
            '<span class="sv-inj-status-badge ' + (d.status === 'Recovering' ? 'sv-s-rec' : 'sv-s-rehab') + '">' + (d.status || 'Active') + '</span>' +
          '</div>' +
        '</div>' +
        '<div class="sv-inj-type">' + (d.type || 'Injury') + '<span class="sv-inj-part"> · ' + (d.part || '') + '</span></div>' +
        '<div class="sv-inj-dates">' +
          '<span>Started <b>' + (d.started || '—') + '</b></span>' +
          '<span>Est. return <b style="color:var(--brand-400)">' + (d.est || '—') + '</b></span>' +
          '<span style="color:' + urgCol + '"><b>' + (d.wksLeft || '?') + ' wk' + (d.wksLeft !== 1 ? 's' : '') + ' left</b></span>' +
        '</div>' +
        '<div class="sv-prog-track"><div class="sv-prog-fill" style="width:' + prog + '%"></div></div>' +
        '<div class="sv-inj-note">' + (d.note || '') + '</div>' +
      '</div>'
    );
  }).join('');

  /* fitness bars — sorted ascending (worst first) */
  const fitRows = [...PLAYERS].sort((a, b) => a.fit - b.fit).map(p => {
    const fc = p.fit >= 90 ? 'sv-fit-hi' : p.fit >= 75 ? 'sv-fit-md' : 'sv-fit-lo';
    return (
      '<div class="sv-fit-row">' +
        '<div class="sv-fit-n">' + p.n + '</div>' +
        '<span class="pos ' + p.posClass + '">' + p.pos + '</span>' +
        '<div class="sv-fit-name">' + p.name + (p.inj ? ' <span class="inj">INJ</span>' : '') + '</div>' +
        '<div class="sv-fit-bar-wrap">' +
          '<div class="sv-fit-bar"><div class="sv-fit-fill ' + fc + '" style="width:' + p.fit + '%"></div></div>' +
          '<span class="sv-fit-pct">' + p.fit + '%</span>' +
        '</div>' +
      '</div>'
    );
  }).join('');

  el.innerHTML =
    '<div class="sv-medical">' +
    kpis +
    '<div class="sv-med-cols">' +
      '<div class="sv-med-left">'  + svCard('Injury Room',    '3 active',                    '<div class="sv-inj-list">' + injCards + '</div>') + '</div>' +
      '<div class="sv-med-right">' + svCard('Squad Fitness',  'All 28 · low → high',         '<div class="sv-fit-list">' + fitRows  + '</div>') + '</div>' +
    '</div>' +
    '</div>';
}

/* ══════════════════════════════════════════════════════════════
   DYNAMICS
══════════════════════════════════════════════════════════════ */
function renderDynamicsView(el) {
  const morColor  = { pos:'var(--pos-400)', neu:'var(--neu-400)', neg:'var(--neg-400)' };
  const morText   = { pos:'Excellent',      neu:'Content',         neg:'Unhappy' };
  const moodColor = { pos:'var(--pos-400)', neu:'var(--neu-400)', neg:'var(--neg-400)' };
  const moodTint  = { pos:'rgba(0,255,136,0.06)', neu:'rgba(245,185,66,0.06)', neg:'rgba(255,80,80,0.06)' };

  /* ── LEFT: spirit ── */
  const spiritBody =
    '<div class="sv-spirit-big">' +
      '<div class="sv-spirit-num">' + DYN.teamSpirit + '</div>' +
      '<div class="sv-spirit-lbl">Team Spirit</div>' +
    '</div>' +
    ['Harmony', 'Tension'].map((lbl, i) => {
      const v = i === 0 ? DYN.harmony : DYN.tension;
      const c = i === 0 ? 'var(--pos-400)' : 'var(--neg-400)';
      return '<div class="sv-gauge-row">' +
        '<span class="sv-gauge-lbl">' + lbl + '</span>' +
        '<div class="sv-gauge"><div class="sv-gauge-fill" style="width:' + v + '%;background:' + c + '"></div></div>' +
        '<span class="sv-gauge-v"' + (i ? ' style="color:var(--neg-400)"' : '') + '>' + v + '</span>' +
      '</div>';
    }).join('') +
    '<div class="sv-mor-kpi-grid">' +
    Object.entries(DYN.moraleCounts).map(([k, n]) => {
      const c = k === 'Excellent' ? 'var(--brand-400)' : k === 'Good' ? 'var(--pos-400)' : k === 'Unhappy' ? 'var(--neg-400)' : 'var(--ink-2)';
      return '<div class="sv-mor-kpi">' +
        '<span style="color:' + c + ';font-family:var(--font-display);font-size:26px;font-weight:800;line-height:1">' + n + '</span>' +
        '<span class="sv-mor-kpi-lbl">' + k + '</span>' +
      '</div>';
    }).join('') +
    '</div>';

  /* ── CENTER: leadership ── */
  const cap = PLAYERS.find(p => p.n === DYN.captainN);
  const vc  = PLAYERS.find(p => p.n === DYN.vcN);
  const leaderBody =
    '<div class="sv-leader-row">' +
      '<div class="sv-leader-badge sv-badge-cap">C</div>' +
      '<div class="sv-leader-info">' +
        '<div class="sv-leader-name">' + (cap?.name || '—') + '</div>' +
        '<div class="sv-leader-sub">Captain &amp; dressing room leader · ' + (cap?.nat || '') + ' · ' + (cap?.pos || '') + '</div>' +
      '</div>' +
      '<div class="sv-leader-inf">95<span class="sv-inf-unit"> INF</span></div>' +
    '</div>' +
    '<div class="sv-leader-row">' +
      '<div class="sv-leader-badge sv-badge-vc">VC</div>' +
      '<div class="sv-leader-info">' +
        '<div class="sv-leader-name">' + (vc?.name || '—') + '</div>' +
        '<div class="sv-leader-sub">Vice-captain · ' + (vc?.nat || '') + ' · ' + (vc?.pos || '') + '</div>' +
      '</div>' +
      '<div class="sv-leader-inf">82<span class="sv-inf-unit"> INF</span></div>' +
    '</div>' +
    '<div class="sv-card-sep"></div>' +
    DYN.influence.slice(2).map(inf => {
      const pl = PLAYERS.find(p => p.n === inf.n);
      return '<div class="sv-inf-row">' +
        '<span class="sv-inf-name">' + (pl?.name || '—') + '</span>' +
        '<span class="sv-inf-role">' + inf.role + '</span>' +
        '<div class="sv-gauge sv-gauge-sm"><div class="sv-gauge-fill" style="width:' + inf.pct + '%;background:var(--brand-400)"></div></div>' +
        '<span class="sv-gauge-v">' + inf.pct + '</span>' +
      '</div>';
    }).join('');

  /* ── CENTER: cliques ── */
  const cliquesBody = DYN.cliques.map(cl => {
    const chips = cl.members.map(n => {
      const pl = PLAYERS.find(p => p.n === n);
      return '<span class="sv-clique-chip">' + (pl ? pl.name.split(' ').pop() : '?') + '</span>';
    }).join('');
    return '<div class="sv-clique" style="border-left-color:' + moodColor[cl.mood] + ';background:' + moodTint[cl.mood] + '">' +
      '<div class="sv-clique-head">' +
        '<span class="sv-clique-name">' + cl.name + '</span>' +
        '<span class="sv-clique-ct">' + cl.members.length + ' players</span>' +
      '</div>' +
      '<div class="sv-clique-members">' + chips + '</div>' +
      '<div class="sv-clique-desc">' + cl.desc + '</div>' +
    '</div>';
  }).join('');

  /* ── RIGHT: per-player morale ── */
  const sortedMor = [...PLAYERS].sort((a, b) => {
    const o = { pos:0, neu:1, neg:2 };
    return (o[a.morKind] || 1) - (o[b.morKind] || 1);
  });
  const morRows = sortedMor.map(p => {
    const mc = morColor[p.morKind] || 'var(--ink-2)';
    return '<div class="sv-mor-row">' +
      '<span class="pos ' + p.posClass + '">' + p.pos + '</span>' +
      '<span class="sv-mor-pname">' + p.name + '</span>' +
      '<div class="sv-mor-dots">' +
        p.mor.map(v => '<span class="sv-mor-dot' + (v ? ' sv-dot-' + p.morKind : '') + '"></span>').join('') +
      '</div>' +
      '<span class="sv-mor-text" style="color:' + mc + '">' + morText[p.morKind] + '</span>' +
    '</div>';
  }).join('');

  el.innerHTML =
    '<div class="sv-dynamics">' +
      '<div class="sv-dyn-col">' +
        svCard('Team Spirit', 'Wk 31', spiritBody) +
      '</div>' +
      '<div class="sv-dyn-col sv-dyn-center">' +
        svCard('Leadership &amp; Influence', '', leaderBody) +
        svCard('Squad Groups', DYN.cliques.length + ' groups', cliquesBody) +
      '</div>' +
      '<div class="sv-dyn-col">' +
        svCard('Player Morale', PLAYERS.length + ' players', '<div class="sv-mor-list">' + morRows + '</div>') +
      '</div>' +
    '</div>';
}

/* ══════════════════════════════════════════════════════════════
   STATUS
══════════════════════════════════════════════════════════════ */
function renderStatusView(el) {
  const available = PLAYERS.filter(p => !p.inj && !p.sus && !p.loan);
  const injured   = PLAYERS.filter(p => p.inj);
  const suspended = PLAYERS.filter(p => p.sus);
  const onLoan    = PLAYERS.filter(p => p.loan);

  const kpis =
    '<div class="sv-kpi-strip">' +
    svKpi('Available',  available.length,  'fit &amp; ready',      'sv-kpi-pos') +
    svKpi('Injured',    injured.length,    'in medical centre',    'sv-kpi-neg') +
    svKpi('Suspended',  suspended.length,  'active bans',          suspended.length > 0 ? 'sv-kpi-neg' : '') +
    svKpi('On loan',    onLoan.length,     'playing elsewhere',    '') +
    svKpi('First XI fit', PLAYERS.filter(p => p.xi && !p.inj && !p.sus).length, 'of 11 starters', '') +
    '</div>';

  function grpHead(lbl, n, color) {
    return '<div class="sv-st-grp-head" style="border-left-color:' + color + '">' +
      '<span class="sv-st-dot" style="background:' + color + '"></span>' +
      '<span class="sv-st-grp-lbl">' + lbl + '</span>' +
      '<span class="sv-st-grp-ct">' + n + '</span>' +
    '</div>';
  }

  const avChips = available.map(p =>
    '<div class="sv-av-chip">' +
      '<span class="pos ' + p.posClass + '">' + p.pos + '</span>' +
      '<span class="sv-av-name">' + p.name.split(' ').pop() + '</span>' +
      '<span class="sv-av-n">' + p.n + '</span>' +
    '</div>'
  ).join('');

  function injMini(p) {
    const d = INJURY_DETAILS[p.n] || {};
    return '<div class="sv-inj-mini">' +
      '<div style="display:flex;align-items:center;gap:7px;margin-bottom:4px">' +
        '<span class="pos ' + p.posClass + '">' + p.pos + '</span>' +
        '<span class="sv-inj-mini-name">' + p.name + '</span>' +
        '<span class="sv-inj-grade ' + gradeCls(d.grade || 1) + '">' + gradeText(d.grade || 1) + '</span>' +
      '</div>' +
      '<div style="font-size:10.5px;color:var(--ink-3)">' + (d.type || 'Injury') + ' · ' + (d.part || '') + '</div>' +
      '<div style="font-family:var(--font-mono);font-size:10px;color:var(--ink-4);margin-top:3px">' +
        'Est. return <span style="color:var(--brand-400)">' + (d.est || '—') + '</span>' +
        ' · <span style="color:var(--neu-400)">' + (d.wksLeft || '?') + ' wk left</span>' +
      '</div>' +
    '</div>';
  }

  function emptyNote(msg) {
    return '<div class="sv-st-empty">' + msg + '</div>';
  }

  el.innerHTML =
    '<div class="sv-status-view">' +
    kpis +
    '<div class="sv-st-body">' +
      '<div class="sv-st-avail-col">' +
        grpHead('Available', available.length, 'var(--pos-400)') +
        '<div class="sv-av-grid">' + avChips + '</div>' +
      '</div>' +
      '<div class="sv-st-right-col">' +
        '<div class="sv-st-sect">' +
          grpHead('Injured', injured.length, 'var(--neg-400)') +
          (injured.length ? injured.map(injMini).join('') : emptyNote('No injuries · squad fully fit')) +
        '</div>' +
        '<div class="sv-st-sect">' +
          grpHead('Suspended', suspended.length, 'var(--neu-400)') +
          (suspended.length
            ? suspended.map(p => '<div class="sv-inj-mini"><span class="pos ' + p.posClass + '">' + p.pos + '</span> ' + p.name + '</div>').join('')
            : emptyNote('No active suspensions')) +
        '</div>' +
        '<div class="sv-st-sect">' +
          grpHead('On Loan', onLoan.length, 'var(--info-400)') +
          (onLoan.length
            ? onLoan.map(p => '<div class="sv-inj-mini"><span class="pos ' + p.posClass + '">' + p.pos + '</span> ' + p.name + '</div>').join('')
            : emptyNote('No players currently on loan')) +
        '</div>' +
      '</div>' +
    '</div>' +
    '</div>';
}

/* ── public API ───────────────────────────────────────────────────────────── */
window.renderMedicalView  = renderMedicalView;
window.renderDynamicsView = renderDynamicsView;
window.renderStatusView   = renderStatusView;
