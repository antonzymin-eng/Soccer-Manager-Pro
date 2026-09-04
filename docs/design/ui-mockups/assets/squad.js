/* ============================================================================
   SQUAD SCREEN — sort · filter · search · view switching
   ============================================================================ */

/* ── fit 1920×1080 stage to viewport ──────────────────────────────────────── */
(function fitStage() {
  const stage = document.getElementById('stage');
  const badge = document.getElementById('resbadge');
  function fit() {
    const W = 1920, H = 1080;
    const s = Math.min(window.innerWidth / W, window.innerHeight / H);
    stage.style.transform = 'scale(' + s + ')';
    if (badge) badge.textContent =
      '1920 × 1080 · standard tier · scale ' + Math.round(s * 100) + '% · window ' +
      window.innerWidth + '×' + window.innerHeight;
  }
  fit();
  window.addEventListener('resize', fit);
})();

/* ── country flag map ──────────────────────────────────────────────────────── */
const FLAG_MAP = {
  ARG:'🇦🇷', BRA:'🇧🇷', CMR:'🇨🇲', CIV:'🇨🇮', DEN:'🇩🇰',
  ENG:'🏴󠁧󠁢󠁥󠁮󠁧󠁿', ESP:'🇪🇸', FRA:'🇫🇷', MAR:'🇲🇦', NED:'🇳🇱',
  NGA:'🇳🇬', NIR:'🏴󠁧󠁢󠁮󠁩󠁲󠁿', POR:'🇵🇹', TUR:'🇹🇷', URU:'🇺🇾'
};
function natFlag(code) { return FLAG_MAP[code] || code; }

/* ── contract helpers (game date: Mar 2025) ───────────────────────────────── */
function contractMonths(expYear) { return (expYear - 2025) * 12 + 9; }
function contractRemainingStr(expYear) {
  const m = contractMonths(expYear);
  if (m < 1)  return 'Expired';
  if (m < 12) return m + ' mo';
  const y = Math.floor(m / 12), mo = m % 12;
  return y + 'yr' + (mo > 0 ? ' ' + mo + 'mo' : '');
}
function contractStatusCls(expYear) {
  const m = contractMonths(expYear);
  if (m <= 9)  return 'exp-crit';
  if (m <= 21) return 'exp-warn';
  return 'exp-ok';
}
function contractStatusLabel(expYear) {
  const m = contractMonths(expYear);
  if (m <= 9)  return 'CRITICAL';
  if (m <= 21) return 'EXPIRING';
  return 'ACTIVE';
}
function annualWage(wageStr) {
  const m = String(wageStr).match(/([\d.]+)k/);
  if (!m) return '—';
  const yr = parseFloat(m[1]) * 1000 * 52;
  return yr >= 1e6 ? '£' + (yr/1e6).toFixed(2).replace(/\.00$/,'') + 'm' : '£' + Math.round(yr/1000) + 'k';
}

/* ── player roster ─────────────────────────────────────────────────────────── */
const PLAYERS = [
  // GK
  { n:1,  xi:true, pos:'GK', posClass:'pos-gk', name:'A. Onana',       ca:4,   pa:4,   age:28, nat:'CMR', ft:'R', hgt:"6'4\"", second:'—',   fit:94, fitCls:'',     cnd:17, cndCls:'a17', form:'WWDWW', mor:[1,1,1,1,0], morKind:'pos', xg:0.02,xa:0.04,last:7.2, avg:7.04,apps:29,g:0, a:0, mins:'2,610',y:1,val:'£18m',  wage:'£120k',exp:2028,rl:'—',    loy:'£1.5m',agt:'G. Amodio'  },
  { n:22,          pos:'GK', posClass:'pos-gk', name:'T. Heaton',      ca:2.5, pa:2.5, age:38, nat:'ENG', ft:'R', hgt:"6'3\"", second:'—',   fit:99, fitCls:'',     cnd:14, cndCls:'a14', form:'-----', mor:[1,1,1,0,0], morKind:'neu', xg:0.00,xa:0.00,last:null,avg:6.80,apps:2, g:0, a:0, mins:'180',  y:0,val:'£325k',wage:'£40k', exp:2026,rl:'—',    loy:'£500k', agt:'SXI Sports' },
  { n:31,          pos:'GK', posClass:'pos-gk', name:'A. Bayindir',    ca:2.5, pa:3,   age:26, nat:'TUR', ft:'R', hgt:"6'5\"", second:'—',   fit:97, fitCls:'',     cnd:14, cndCls:'a14', form:'-----', mor:[1,1,1,0,0], morKind:'neu', xg:0.00,xa:0.00,last:null,avg:null,apps:0, g:0, a:0, mins:'0',    y:0,val:'£3.2m',wage:'£55k', exp:2027,rl:'—',    loy:'£600k', agt:'—'          },
  // DF
  { n:2,  xi:true, pos:'RB', posClass:'pos-df', name:'D. Dalot',       ca:3.5, pa:4,   age:25, nat:'POR', ft:'R', hgt:"6'0\"", second:'RWB', fit:88, fitCls:'',     cnd:17, cndCls:'a17', form:'WLWDW', mor:[1,1,1,0,0], morKind:'pos', xg:1.6, xa:4.1, last:7.0, avg:7.12,apps:27,g:2, a:5, mins:'2,318',y:4,val:'£42m',  wage:'£100k',exp:2028,rl:'£55m', loy:'£2m',  agt:'J. Veiga'   },
  { n:6,           pos:'CB', posClass:'pos-df', name:'L. Martínez',    ca:4,   pa:4,   age:27, nat:'ARG', ft:'L', hgt:"5'9\"", second:'DC',  fit:72, fitCls:'warn', cnd:14, cndCls:'a14', form:'DWWWD', mor:[1,1,1,1,1], morKind:'pos', xg:2.1, xa:0.8, last:7.4, avg:7.38,apps:25,g:3, a:1, mins:'2,200',y:3,val:'£60m',  wage:'£165k',exp:2027,rl:'£90m', loy:'£2.4m',agt:'M. Lopez',  inj:true, star:true },
  { n:5,           pos:'CB', posClass:'pos-df', name:'H. Maguire',     ca:3,   pa:3,   age:31, nat:'ENG', ft:'R', hgt:"6'4\"", second:'—',   fit:91, fitCls:'',     cnd:14, cndCls:'a14', form:'WDWDW', mor:[1,1,1,1,0], morKind:'pos', xg:1.4, xa:0.2, last:6.8, avg:6.92,apps:23,g:1, a:0, mins:'1,930',y:5,val:'£14m',  wage:'£190k',exp:2025,rl:'—',    loy:'£3m',  agt:'B. Clarke'  },
  { n:19, xi:true, pos:'CB', posClass:'pos-df', name:'R. Varane',      ca:3.5, pa:3.5, age:30, nat:'FRA', ft:'R', hgt:"6'3\"", second:'—',   fit:85, fitCls:'',     cnd:17, cndCls:'a17', form:'WWDWD', mor:[1,1,1,1,0], morKind:'pos', xg:1.8, xa:0.3, last:7.1, avg:7.18,apps:22,g:2, a:0, mins:'1,800',y:2,val:'£24m',  wage:'£220k',exp:2026,rl:'—',    loy:'£4m',  agt:'—'          },
  { n:23,          pos:'LB', posClass:'pos-df', name:'L. Shaw',        ca:3.5, pa:4,   age:28, nat:'ENG', ft:'L', hgt:"6'1\"", second:'LWB', fit:48, fitCls:'bad',  cnd:11, cndCls:'a11', form:'-----', mor:[1,1,0,0,0], morKind:'neg', xg:0.4, xa:2.6, last:null,avg:7.05,apps:14,g:0, a:3, mins:'1,260',y:1,val:'£28m',  wage:'£155k',exp:2027,rl:'—',    loy:'£2.5m',agt:'CAA Base',  inj:true },
  { n:12, xi:true, pos:'LB', posClass:'pos-df', name:'T. Malacia',     ca:3,   pa:3.5, age:25, nat:'NED', ft:'L', hgt:"5'8\"", second:'LWB', fit:92, fitCls:'',     cnd:14, cndCls:'a14', form:'DWWDW', mor:[1,1,1,1,0], morKind:'pos', xg:0.3, xa:1.7, last:7.0, avg:6.88,apps:18,g:0, a:2, mins:'1,490',y:3,val:'£18m',  wage:'£85k', exp:2027,rl:'—',    loy:'£1.5m',agt:'—'          },
  { n:20,          pos:'RB', posClass:'pos-df', name:'A. Wan-Bissaka', ca:3,   pa:3,   age:26, nat:'ENG', ft:'R', hgt:"6'0\"", second:'—',   fit:90, fitCls:'',     cnd:14, cndCls:'a14', form:'WDLWD', mor:[1,1,1,1,0], morKind:'neu', xg:0.2, xa:1.1, last:6.7, avg:6.75,apps:15,g:0, a:1, mins:'1,250',y:6,val:'£12m',  wage:'£85k', exp:2025,rl:'—',    loy:'£1.2m',agt:'—'          },
  { n:35, xi:true, pos:'CB', posClass:'pos-df', name:'W. Yoro',        ca:3.5, pa:5,   age:18, nat:'FRA', ft:'R', hgt:"6'3\"", second:'DM',  fit:95, fitCls:'',     cnd:17, cndCls:'a17', form:'WWWDW', mor:[1,1,1,1,1], morKind:'pos', xg:1.5, xa:0.6, last:7.5, avg:7.42,apps:18,g:2, a:1, mins:'1,560',y:1,val:'£48m',  wage:'£60k', exp:2029,rl:'£65m', loy:'£1m',  agt:'M. Barat',  star:true },
  { n:28,          pos:'CB', posClass:'pos-df', name:'J. Evans',       ca:2.5, pa:2.5, age:36, nat:'NIR', ft:'R', hgt:"6'2\"", second:'—',   fit:80, fitCls:'',     cnd:11, cndCls:'a11', form:'DD---', mor:[1,1,1,0,0], morKind:'neu', xg:0.5, xa:0.1, last:6.6, avg:6.70,apps:11,g:0, a:0, mins:'820',  y:2,val:'£550k', wage:'£60k', exp:2025,rl:'—',    loy:'£500k', agt:'—'          },
  // MF
  { n:18, xi:true, pos:'CDM',posClass:'pos-mf', name:'Casemiro',       ca:3.5, pa:4,   age:33, nat:'BRA', ft:'R', hgt:"6'1\"", second:'CM',  fit:68, fitCls:'warn', cnd:17, cndCls:'a17', form:'WWDDW', mor:[1,1,1,0,0], morKind:'neu', xg:2.4, xa:1.6, last:7.0, avg:7.05,apps:24,g:3, a:2, mins:'2,030',y:8,val:'£18m',  wage:'£350k',exp:2026,rl:'—',    loy:'£5m',  agt:'T. Silva'   },
  { n:37, xi:true, pos:'CM', posClass:'pos-mf', name:'K. Mainoo',      ca:4,   pa:5,   age:19, nat:'ENG', ft:'R', hgt:"5'10\"",second:'AM',  fit:90, fitCls:'',     cnd:20, cndCls:'a20', form:'WWWDW', mor:[1,1,1,1,1], morKind:'pos', xg:3.2, xa:2.8, last:7.6, avg:7.51,apps:26,g:4, a:3, mins:'2,180',y:2,val:'£55m',  wage:'£90k', exp:2029,rl:'£70m', loy:'£1.5m',agt:'SXI Sports', star:true },
  { n:8,  xi:true, pos:'AM', posClass:'pos-mf', name:'B. Fernandes',   ca:4.5, pa:4.5, age:30, nat:'POR', ft:'R', hgt:"5'11\"",second:'CM',  fit:93, fitCls:'',     cnd:20, cndCls:'a20', form:'WWDWW', mor:[1,1,1,1,1], morKind:'pos', xg:9.4, xa:10.2,last:7.9, avg:7.84,apps:30,g:12,a:11,mins:'2,690',y:5,val:'£72m',  wage:'£300k',exp:2028,rl:'£95m', loy:'£4m',  agt:'M. Veiga',  cap:true },
  { n:14,          pos:'CM', posClass:'pos-mf', name:'C. Eriksen',     ca:3,   pa:3,   age:32, nat:'DEN', ft:'R', hgt:"6'1\"", second:'AM',  fit:84, fitCls:'',     cnd:14, cndCls:'a14', form:'DWWDL', mor:[1,1,1,0,0], morKind:'neu', xg:1.8, xa:3.6, last:6.9, avg:6.95,apps:21,g:2, a:4, mins:'1,580',y:1,val:'£6m',   wage:'£155k',exp:2025,rl:'—',    loy:'£2m',  agt:'—'          },
  { n:25,          pos:'CM', posClass:'pos-mf', name:'M. Ugarte',      ca:3.5, pa:4,   age:23, nat:'URU', ft:'R', hgt:"5'11\"",second:'DM',  fit:87, fitCls:'',     cnd:17, cndCls:'a17', form:'WDWWW', mor:[1,1,1,1,0], morKind:'pos', xg:1.1, xa:0.9, last:7.2, avg:7.18,apps:22,g:1, a:1, mins:'1,810',y:9,val:'£42m',  wage:'£115k',exp:2029,rl:'£55m', loy:'£2.5m',agt:'J. Costa'    },
  { n:7,           pos:'AM', posClass:'pos-mf', name:'M. Mount',       ca:3,   pa:3.5, age:25, nat:'ENG', ft:'R', hgt:"5'10\"",second:'CM',  fit:91, fitCls:'',     cnd:14, cndCls:'a14', form:'DLDWD', mor:[1,1,0,0,0], morKind:'neu', xg:1.5, xa:1.8, last:6.4, avg:6.65,apps:12,g:1, a:2, mins:'820',  y:2,val:'£28m',  wage:'£150k',exp:2028,rl:'—',    loy:'£2m',  agt:'CAA Base'   },
  { n:39,          pos:'CM', posClass:'pos-mf', name:'S. Amrabat',     ca:3,   pa:3,   age:27, nat:'MAR', ft:'L', hgt:"6'0\"", second:'DM',  fit:89, fitCls:'',     cnd:14, cndCls:'a14', form:'WDDWD', mor:[1,1,1,1,0], morKind:'pos', xg:0.6, xa:1.0, last:6.9, avg:6.92,apps:19,g:0, a:1, mins:'1,310',y:5,val:'£18m',  wage:'£75k', exp:2026,rl:'—',    loy:'£1m',  agt:'—'          },
  { n:43,          pos:'AM', posClass:'pos-mf', name:'F. Pellistri',   ca:2.5, pa:3.5, age:22, nat:'URU', ft:'R', hgt:"5'9\"", second:'RW',  fit:93, fitCls:'',     cnd:14, cndCls:'a14', form:'DDWDD', mor:[1,1,1,0,0], morKind:'neu', xg:0.8, xa:0.4, last:6.7, avg:6.68,apps:11,g:1, a:0, mins:'540',  y:1,val:'£9m',   wage:'£32k', exp:2027,rl:'—',    loy:'£800k', agt:'—'          },
  // FW
  { n:9,           pos:'ST', posClass:'pos-fw', name:'R. Højlund',     ca:3.5, pa:4.5, age:21, nat:'DEN', ft:'R', hgt:"6'3\"", second:'—',   fit:54, fitCls:'bad',  cnd:8,  cndCls:'a8',  form:'LWDWL', mor:[1,1,1,0,0], morKind:'neg', xg:12.6,xa:1.4, last:6.3, avg:6.78,apps:24,g:14,a:2, mins:'1,910',y:3,val:'£48m',  wage:'£90k', exp:2029,rl:'£65m', loy:'£2m',  agt:'K. Jensen', inj:true },
  { n:10, xi:true, pos:'LW', posClass:'pos-fw', name:'M. Rashford',    ca:4,   pa:4.5, age:26, nat:'ENG', ft:'R', hgt:"6'0\"", second:'ST',  fit:85, fitCls:'',     cnd:17, cndCls:'a17', form:'DLWWW', mor:[1,1,1,0,0], morKind:'pos', xg:13.1,xa:5.4, last:7.1, avg:7.12,apps:28,g:15,a:6, mins:'2,310',y:4,val:'£68m',  wage:'£300k',exp:2028,rl:'—',    loy:'£5m',  agt:'Roc Nation'  },
  { n:11, xi:true, pos:'RW', posClass:'pos-fw', name:'A. Garnacho',    ca:3.5, pa:4.5, age:20, nat:'ARG', ft:'L', hgt:"5'11\"",second:'LW',  fit:89, fitCls:'',     cnd:17, cndCls:'a17', form:'WDWWW', mor:[1,1,1,1,0], morKind:'pos', xg:7.2, xa:6.1, last:7.3, avg:7.26,apps:27,g:9, a:7, mins:'2,180',y:5,val:'£50m',  wage:'£75k', exp:2028,rl:'£68m', loy:'£1.5m',agt:'J. Vigo'     },
  { n:21, xi:true, pos:'ST', posClass:'pos-fw', name:'J. Zirkzee',     ca:3.5, pa:4,   age:23, nat:'NED', ft:'B', hgt:"6'4\"", second:'AM',  fit:88, fitCls:'',     cnd:14, cndCls:'a14', form:'WDLWD', mor:[1,1,1,1,0], morKind:'pos', xg:6.4, xa:4.2, last:7.0, avg:7.03,apps:23,g:8, a:5, mins:'1,640',y:2,val:'£36m',  wage:'£95k', exp:2029,rl:'£80m', loy:'£2m',  agt:'P. Kluivert' },
  { n:15,          pos:'LW', posClass:'pos-fw', name:'A. Diallo',      ca:3,   pa:4,   age:21, nat:'CIV', ft:'B', hgt:"5'9\"", second:'RW',  fit:90, fitCls:'',     cnd:17, cndCls:'a17', form:'WWDWD', mor:[1,1,1,1,0], morKind:'pos', xg:4.1, xa:3.5, last:7.1, avg:7.04,apps:20,g:5, a:4, mins:'1,360',y:3,val:'£28m',  wage:'£48k', exp:2027,rl:'—',    loy:'£1m',  agt:'—'          },
  { n:24,          pos:'ST', posClass:'pos-fw', name:'C. Pelegrín',    ca:2.5, pa:2.5, age:30, nat:'ESP', ft:'R', hgt:"6'2\"", second:'AM',  fit:79, fitCls:'',     cnd:11, cndCls:'a11', form:'DWDDL', mor:[1,1,0,0,0], morKind:'neu', xg:2.8, xa:0.9, last:6.6, avg:6.74,apps:14,g:3, a:1, mins:'960',  y:1,val:'£6.5m', wage:'£72k', exp:2025,rl:'—',    loy:'£500k', agt:'—'          },
  // U21s
  { n:46,          pos:'CM', posClass:'pos-mf', name:'D. Gore',        ca:2,   pa:4,   age:19, nat:'ENG', ft:'R', hgt:"5'10\"",second:'DM',  fit:96, fitCls:'',     cnd:14, cndCls:'a14', form:'-----', mor:[1,1,1,0,0], morKind:'pos', xg:0.1, xa:0.2, last:null,avg:6.9, apps:3, g:0, a:0, mins:'95',   y:0,val:'£800k', wage:'£12k', exp:2027,rl:'—',    loy:'£200k', agt:'—'          },
  { n:52,          pos:'ST', posClass:'pos-fw', name:'C. Obi',         ca:1.5, pa:4.5, age:17, nat:'NGA', ft:'R', hgt:"6'1\"", second:'—',   fit:98, fitCls:'',     cnd:17, cndCls:'a17', form:'-----', mor:[1,1,1,1,1], morKind:'pos', xg:0.3, xa:0.0, last:null,avg:7.4, apps:1, g:0, a:0, mins:'14',   y:0,val:'£2.4m', wage:'£8k',  exp:2028,rl:'—',    loy:'£150k', agt:'—',          star:true },
];

/* ── grid templates ────────────────────────────────────────────────────────── */
const GENERAL_GRID   = '28px 30px 1fr 50px 50px 28px 32px 22px 40px 36px 80px 28px 64px 60px 36px 36px 38px 40px 32px 26px 26px 44px 22px 56px 52px 40px 22px';
const CONTRACTS_GRID = '28px 30px 1fr 28px 26px 68px 76px 50px 80px 60px 68px 68px 110px 82px 22px';

/* ── formation positions (4-3-3) ──────────────────────────────────────────── */
const FORM_POS = {
  1: {x:50,y:88},  // GK
  2: {x:83,y:72},  // RB
  19:{x:62,y:68},  // CB
  35:{x:38,y:68},  // CB
  12:{x:17,y:72},  // LB
  18:{x:32,y:50},  // CDM
  37:{x:50,y:46},  // CM
  8: {x:68,y:46},  // AM
  11:{x:82,y:21},  // RW
  21:{x:50,y:16},  // ST
  10:{x:18,y:21},  // LW
};

/* ── last match stats (vs Portsea Vale, D 1-1, 11 Mar) ───────────────────── */
const LM = {
  1:  { mins:90, g:0, a:0, shots:0,  sot:0,  saves:5,  keyp:3,  passpct:72, tkl:0, aerw:1, fouls:0, xg:null,  xa:null  },
  2:  { mins:90, g:0, a:0, shots:1,  sot:0,  saves:0,  keyp:2,  passpct:86, tkl:3, aerw:1, fouls:1, xg:0.08,  xa:0.22  },
  19: { mins:90, g:0, a:0, shots:1,  sot:0,  saves:0,  keyp:0,  passpct:91, tkl:4, aerw:5, fouls:1, xg:0.06,  xa:0.00  },
  35: { mins:90, g:0, a:0, shots:0,  sot:0,  saves:0,  keyp:0,  passpct:94, tkl:2, aerw:6, fouls:0, xg:0.04,  xa:0.00  },
  12: { mins:76, g:0, a:0, shots:1,  sot:0,  saves:0,  keyp:1,  passpct:84, tkl:2, aerw:1, fouls:2, xg:0.05,  xa:0.11  },
  18: { mins:90, g:0, a:0, shots:2,  sot:1,  saves:0,  keyp:1,  passpct:81, tkl:6, aerw:2, fouls:3, xg:0.14,  xa:0.08  },
  37: { mins:90, g:1, a:0, shots:3,  sot:2,  saves:0,  keyp:3,  passpct:90, tkl:4, aerw:0, fouls:1, xg:0.42,  xa:0.31  },
  8:  { mins:90, g:0, a:1, shots:4,  sot:2,  saves:0,  keyp:5,  passpct:88, tkl:1, aerw:0, fouls:2, xg:0.38,  xa:0.55  },
  11: { mins:74, g:0, a:0, shots:3,  sot:1,  saves:0,  keyp:2,  passpct:79, tkl:0, aerw:0, fouls:1, xg:0.29,  xa:0.18  },
  21: { mins:90, g:0, a:0, shots:3,  sot:1,  saves:0,  keyp:1,  passpct:77, tkl:0, aerw:2, fouls:1, xg:0.31,  xa:0.12  },
  10: { mins:90, g:0, a:0, shots:2,  sot:0,  saves:0,  keyp:2,  passpct:82, tkl:0, aerw:0, fouls:1, xg:0.18,  xa:0.20  },
};

/* ── interaction state ─────────────────────────────────────────────────────── */
let sortCol       = 'n';
let sortDir       = 'asc';
let posFilter     = 'ALL';
let statusFilters = new Set();
let searchQuery   = '';
let selectedNum   = 6;
let currentTab    = 'firstXI'; // firstXI | allPlayers | contracts

const tableHead = document.querySelector('.table-head');
const GENERAL_HEADER_HTML = tableHead.innerHTML;

/* ── sort key per column ──────────────────────────────────────────────────── */
function parseVal(s) {
  if (!s || s === '—') return 0;
  const m = String(s).match(/([\d.]+)/);
  if (!m) return 0;
  const n = parseFloat(m[0]);
  if (String(s).includes('m')) return n * 1e6;
  if (String(s).includes('k')) return n * 1e3;
  return n;
}

function sortValue(p, col) {
  switch (col) {
    case 'n':         return p.n;
    case 'pos': { const po={'pos-gk':0,'pos-df':1,'pos-mf':2,'pos-fw':3}; return po[p.posClass] ?? 99; }
    case 'name':      return p.name.toLowerCase();
    case 'ca':        return p.ca;
    case 'pa':        return p.pa;
    case 'age':       return p.age;
    case 'nat':       return p.nat;
    case 'ft':        return p.ft === 'R' ? 0 : p.ft === 'L' ? 1 : 2;
    case 'hgt':       return p.hgt;
    case 'second':    return p.second;
    case 'fit':       return p.fit;
    case 'cnd':       return p.cnd;
    case 'form':      return (p.form.match(/W/g)||[]).length - (p.form.match(/L/g)||[]).length;
    case 'mor':       return p.mor.reduce((a,b) => a+b, 0);
    case 'xg':        return p.xg  ?? -1;
    case 'xa':        return p.xa  ?? -1;
    case 'last':      return p.last ?? -1;
    case 'avg':       return p.avg  ?? -1;
    case 'apps':      return p.apps;
    case 'g':         return p.g;
    case 'a':         return p.a;
    case 'mins':      return parseInt(String(p.mins).replace(/,/g,'')) || 0;
    case 'y':         return p.y;
    case 'val':       return parseVal(p.val);
    case 'wage':      return parseVal(p.wage);
    case 'wageann':   return parseVal(p.wage);
    case 'exp':       return p.exp;
    case 'remaining': return contractMonths(p.exp);
    case 'status':    return contractMonths(p.exp);
    case 'rl':        return parseVal(p.rl);
    case 'loy':       return parseVal(p.loy);
    case 'agt':       return p.agt.toLowerCase();
    default:          return 0;
  }
}

/* ── filter + sort ─────────────────────────────────────────────────────────── */
function getVisible() {
  const list = PLAYERS.filter(p => {
    if (currentTab === 'firstXI' && !p.xi) return false;
    if (posFilter === 'GK'  && p.posClass !== 'pos-gk') return false;
    if (posFilter === 'DF'  && p.posClass !== 'pos-df') return false;
    if (posFilter === 'MF'  && p.posClass !== 'pos-mf') return false;
    if (posFilter === 'FW'  && p.posClass !== 'pos-fw') return false;
    if (posFilter === 'U21' && p.age > 21) return false;
    if (statusFilters.size > 0) {
      const avail = !p.inj && !p.sus && !p.loan;
      const ok =
        (statusFilters.has('available')  && avail)   ||
        (statusFilters.has('injured')    && !!p.inj) ||
        (statusFilters.has('suspended')  && !!p.sus) ||
        (statusFilters.has('loan')       && !!p.loan);
      if (!ok) return false;
    }
    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      if (!p.name.toLowerCase().includes(q) &&
          !p.pos.toLowerCase().includes(q)  &&
          !p.nat.toLowerCase().includes(q)) return false;
    }
    return true;
  });

  list.sort((a, b) => {
    const av = sortValue(a, sortCol), bv = sortValue(b, sortCol);
    if (av === bv) return 0;
    return (av < bv ? -1 : 1) * (sortDir === 'asc' ? 1 : -1);
  });

  return list;
}

/* ── HTML builders ─────────────────────────────────────────────────────────── */
function ratingClass(v) { return v == null ? 'na' : v >= 7.2 ? 'hi' : v < 6.6 ? 'lo' : 'md'; }
function ratingTxt(v)   { return v == null ? '—' : v.toFixed(1); }
function avgTxt(v)      { return v == null ? '—' : v.toFixed(2); }
function footCls(f)     { return f === 'L' ? 'ft-l' : f === 'B' ? 'ft-b' : 'ft-r'; }
function xNum(v)        { return v == null ? '—' : v >= 10 ? v.toFixed(1) : v.toFixed(2); }

function formHtml(seq) {
  return [...seq].map(ch => {
    if (ch==='W') return '<span class="w">W</span>';
    if (ch==='D') return '<span class="d">D</span>';
    if (ch==='L') return '<span class="l">L</span>';
    return '<span class="x">—</span>';
  }).join('');
}

function morHtml(arr, kind) {
  return arr.map(v =>
    v ? '<span class="on ' + kind + '"></span>' : '<span></span>'
  ).join('');
}

function starsHtml(v, kind) {
  const full = Math.floor(v), half = (v - full) >= 0.5 ? 1 : 0;
  let s = '';
  for (let i = 0; i < 5; i++) {
    if (i < full)                s += '<span class="s on">★</span>';
    else if (i===full && half)   s += '<span class="s half">★</span>';
    else                         s += '<span class="s">★</span>';
  }
  return '<span class="stars ' + kind + '" title="' + v.toFixed(1) + '">' + s + '</span>';
}

function rowHtml(p) {
  const tags = [];
  if (p.inj)  tags.push('<span class="inj">INJ</span>');
  if (p.cap)  tags.push('<span class="cap">C</span>');
  if (p.star) tags.push('<span class="star">★</span>');
  const sel = p.n === selectedNum;
  return (
    '<div class="row' + (sel ? ' sel' : '') + '" data-num="' + p.n + '">' +
    '<div class="n">' + p.n + '</div>' +
    '<div><span class="pos ' + p.posClass + '">' + p.pos + '</span></div>' +
    '<div class="name"><span class="nm' + (sel ? ' sel-emph' : '') + '">' + p.name + '</span>' + tags.join('') + '</div>' +
    '<div>' + starsHtml(p.ca, 'ca') + '</div>' +
    '<div>' + starsHtml(p.pa, 'pa') + '</div>' +
    '<div class="num">' + p.age + '</div>' +
    '<div><span class="nat">' + natFlag(p.nat) + '</span></div>' +
    '<div><span class="ft ' + footCls(p.ft) + '">' + p.ft + '</span></div>' +
    '<div class="num hgt">' + p.hgt + '</div>' +
    '<div class="second">' + p.second + '</div>' +
    '<div class="fit"><div class="bar ' + p.fitCls + '"><i style="width:' + p.fit + '%"></i></div><div class="v">' + p.fit + '</div></div>' +
    '<div><span class="cnd ' + p.cndCls + '">' + p.cnd + '</span></div>' +
    '<div class="form">' + formHtml(p.form) + '</div>' +
    '<div class="mor">' + morHtml(p.mor, p.morKind) + '</div>' +
    '<div class="num x">' + xNum(p.xg) + '</div>' +
    '<div class="num x">' + xNum(p.xa) + '</div>' +
    '<div class="rating ' + ratingClass(p.last) + '">' + ratingTxt(p.last) + '</div>' +
    '<div class="rating ' + ratingClass(p.avg)  + '">' + avgTxt(p.avg)  + '</div>' +
    '<div class="num">' + p.apps + '</div>' +
    '<div class="num">' + p.g + '</div>' +
    '<div class="num">' + p.a + '</div>' +
    '<div class="num">' + p.mins + '</div>' +
    '<div class="num">' + p.y + '</div>' +
    '<div class="num">' + p.val + '</div>' +
    '<div class="num">' + p.wage + '</div>' +
    '<div class="num">' + p.exp + '</div>' +
    '<div class="more">⋯</div>' +
    '</div>'
  );
}

function contractsHeaderHtml() {
  return [
    ['n','#','right'],['pos','Pos',''],['name','Name',''],['age','Age','right'],
    ['nat','Nat',''],['wage','Wage/wk','right'],['wageann','Annual','right'],
    ['exp','Expires','right'],['remaining','Remaining',''],['val','Value','right'],
    ['rl','Release','right'],['loy','Loyalty','right'],['agt','Agent',''],
    ['status','Status',''],['','⋯','']
  ].map(([col, label, cls]) =>
    col
      ? '<div class="h' + (cls ? ' '+cls : '') + '" data-col="' + col + '">' + label + ' <span class="arr"></span></div>'
      : '<div class="h">⋯</div>'
  ).join('');
}

function contractRowHtml(p) {
  const sel = p.n === selectedNum;
  const tags = [];
  if (p.inj) tags.push('<span class="inj">INJ</span>');
  if (p.cap) tags.push('<span class="cap">C</span>');
  const rem = contractRemainingStr(p.exp);
  const stCls = contractStatusCls(p.exp);
  const stLbl = contractStatusLabel(p.exp);
  return (
    '<div class="row' + (sel ? ' sel' : '') + '" data-num="' + p.n + '">' +
    '<div class="n">' + p.n + '</div>' +
    '<div><span class="pos ' + p.posClass + '">' + p.pos + '</span></div>' +
    '<div class="name"><span class="nm' + (sel ? ' sel-emph' : '') + '">' + p.name + '</span>' + tags.join('') + '</div>' +
    '<div class="num">' + p.age + '</div>' +
    '<div><span class="nat">' + natFlag(p.nat) + '</span></div>' +
    '<div class="num">' + p.wage + '</div>' +
    '<div class="num">' + annualWage(p.wage) + '</div>' +
    '<div class="num">' + p.exp + '</div>' +
    '<div class="second">' + rem + '</div>' +
    '<div class="num">' + p.val + '</div>' +
    '<div class="num">' + (p.rl || '—') + '</div>' +
    '<div class="num">' + (p.loy || '—') + '</div>' +
    '<div class="second" style="font-size:10.5px">' + (p.agt || '—') + '</div>' +
    '<div><span class="exp-status ' + stCls + '">' + stLbl + '</span></div>' +
    '<div class="more">⋯</div>' +
    '</div>'
  );
}

function emptyStateHtml() {
  let icon = '⊘', title = 'No players match', sub = 'Clear filters or adjust your search';
  if (searchQuery) {
    icon = '⌕'; title = 'No results for "' + searchQuery + '"'; sub = 'Try a different name, position or nationality';
  } else if (statusFilters.size === 1) {
    if (statusFilters.has('injured'))   { icon='✓'; title='No injured players';  sub='Everyone is fit and available'; }
    if (statusFilters.has('suspended')) { icon='✓'; title='No suspensions';       sub='No active bans in the squad'; }
    if (statusFilters.has('loan'))      { icon='✓'; title='No outgoing loans';    sub='No players currently on loan'; }
  }
  return '<div class="empty-state"><div class="es-icon">' + icon + '</div><div class="es-title">' + title + '</div><div class="es-sub">' + sub + '</div></div>';
}

/* ── helper ───────────────────────────────────────────────────────────────── */
function mkCg(l, v, s) {
  return '<div class="cg"><div class="l">' + l + '</div><div class="v">' + v + '</div><div class="s">' + s + '</div></div>';
}

/* ── formation panel ───────────────────────────────────────────────────────── */
function renderFormationPanel() {
  const panel = document.getElementById('formationPanel');
  if (!panel) return;
  if (currentTab !== 'firstXI') { panel.style.display = 'none'; return; }
  panel.style.display = 'block';

  const FROWS = [[11,21,10],[8,37,18],[2,19,35,12],[1]];
  const xi = PLAYERS.filter(p => p.xi);

  function tacChip(n) {
    const p = PLAYERS.find(pl => pl.n === n); if (!p) return '';
    const r = p.last;
    const rc = r == null ? '' : r >= 7.2 ? 'hi' : r < 6.6 ? 'lo' : 'md';
    const nm = p.name.includes('.') ? p.name.split(' ').slice(1).join(' ') : p.name.split(' ')[0];
    const sel = p.n === selectedNum;
    return '<div class="tc-chip' + (sel ? ' tc-sel' : '') + '" data-num="' + n + '">' +
      '<div class="tc-pos">' + p.pos + '</div>' +
      '<div class="tc-name">' + nm + '</div>' +
      '<div class="tc-rat ' + rc + '">' + (r != null ? r.toFixed(1) : '—') + '</div>' +
      '</div>';
  }

  const pitchHtml = FROWS.map(row =>
    '<div class="tc-row">' + row.map(tacChip).join('') + '</div>'
  ).join('');

  const MG = '28px 30px 1fr 44px 38px 22px 22px 36px 30px 40px 40px 30px 38px 28px 28px 28px';

  const listHead =
    '<div class="fm-head" style="grid-template-columns:' + MG + '">' +
    '<div class="fh right">#</div><div class="fh">Pos</div><div class="fh">Name</div>' +
    '<div class="fh right">Rat</div><div class="fh right">Mins</div>' +
    '<div class="fh right">G</div><div class="fh right">A</div>' +
    '<div class="fh right">Shts</div><div class="fh right">SoT</div>' +
    '<div class="fh right">xG</div><div class="fh right">xA</div>' +
    '<div class="fh right">KP</div><div class="fh right">Pss%</div>' +
    '<div class="fh right">Tkl</div><div class="fh right">Aer</div>' +
    '<div class="fh right">Fls</div></div>';

  const listRows = xi.map(p => {
    const r   = p.last;
    const rc  = r == null ? 'na' : r >= 7.2 ? 'hi' : r < 6.6 ? 'lo' : 'md';
    const lm  = LM[p.n] || {};
    const sel = p.n === selectedNum;
    const gk  = p.posClass === 'pos-gk';

    const shots = gk ? (lm.saves != null ? lm.saves + ' sv' : '—') : (lm.shots ?? '—');
    const sot   = gk ? '—' : (lm.sot ?? '—');
    const xgv   = (!gk && lm.xg != null) ? lm.xg.toFixed(2) : '—';
    const xav   = (!gk && lm.xa != null) ? lm.xa.toFixed(2) : '—';

    return '<div class="fm-row' + (sel ? ' sel' : '') + '" style="grid-template-columns:' + MG + '" data-num="' + p.n + '">' +
      '<div class="n">' + p.n + '</div>' +
      '<div><span class="pos ' + p.posClass + '">' + p.pos + '</span></div>' +
      '<div class="name"><span class="nm' + (sel ? ' sel-emph' : '') + '">' + p.name + '</span>' +
      (p.inj ? '<span class="inj">INJ</span>' : '') +
      (p.cap ? '<span class="cap">C</span>' : '') + '</div>' +
      '<div class="rating ' + rc + '">' + (r != null ? r.toFixed(1) : '—') + '</div>' +
      '<div class="num">' + (lm.mins ?? '—') + '</div>' +
      '<div class="num">' + (lm.g ?? '—') + '</div>' +
      '<div class="num">' + (lm.a ?? '—') + '</div>' +
      '<div class="num x">' + shots + '</div>' +
      '<div class="num x">' + sot + '</div>' +
      '<div class="num x">' + xgv + '</div>' +
      '<div class="num x">' + xav + '</div>' +
      '<div class="num">' + (lm.keyp ?? '—') + '</div>' +
      '<div class="num">' + (lm.passpct != null ? lm.passpct + '%' : '—') + '</div>' +
      '<div class="num">' + (lm.tkl ?? '—') + '</div>' +
      '<div class="num">' + (lm.aerw ?? '—') + '</div>' +
      '<div class="num">' + (lm.fouls ?? '—') + '</div>' +
      '</div>';
  }).join('');

  panel.innerHTML =
    '<div class="form-panel-head">' +
    '<span>LAST MATCH &nbsp;·&nbsp; vs Portsea Vale (H) &nbsp;·&nbsp; <span style="color:var(--neu-400)">D 1–1</span> &nbsp;·&nbsp; 11 Mar &nbsp;·&nbsp; MD 31</span>' +
    '<span style="color:var(--ink-4)">4–3–3</span></div>' +
    '<div class="form-split">' +
    '<div class="tactics-col"><div class="tac-pitch">' + pitchHtml + '</div></div>' +
    '<div class="match-col">' + listHead + '<div class="fm-rows">' + listRows + '</div></div>' +
    '</div>';

  panel.querySelectorAll('[data-num]').forEach(el => {
    el.addEventListener('click', e => {
      e.stopPropagation();
      selectedNum = parseInt(el.dataset.num, 10);
      render();
    });
  });
}

/* ── side panel ────────────────────────────────────────────────────────────── */
function renderSidePanel(p) {
  const headEl = document.querySelector('.side-head');
  const infoEl = document.querySelector('.side-info');
  const bodyEl = document.querySelector('.side-body');
  if (!headEl || !bodyEl || !p) return;

  /* ── shared helpers ── */
  const rCol   = v => v == null ? 'var(--ink-4)' : v >= 7.2 ? 'var(--brand-400)' : v < 6.6 ? 'var(--neg-400)' : 'var(--ink-1)';
  const fitBg  = f => f < 60 ? 'var(--neg-400)' : f < 80 ? 'var(--neu-400)' : 'var(--pos-400)';
  const flag   = natFlag(p.nat);
  const caVal  = Math.round(p.ca * 16);
  const minsNum = parseInt(String(p.mins).replace(/,/g,'')) || 0;
  const per90  = minsNum > 0 ? 90 / minsNum : 0;
  const p90g   = per90 > 0 ? (p.g * per90).toFixed(2) : '—';
  const p90a   = per90 > 0 ? (p.a * per90).toFixed(2) : '—';
  const avgStr  = p.avg  != null ? p.avg.toFixed(2)  : '—';
  const lastStr = p.last != null ? p.last.toFixed(1) : '—';
  const morText = {pos:'Excellent', neu:'Content', neg:'Unhappy'}[p.morKind] || 'Content';

  /* ── position accent colours ── */
  const ACCENT = {'pos-gk':'#f5b942','pos-df':'#4aa8ff','pos-mf':'#00ff88','pos-fw':'#ff5555'}[p.posClass] || '#00ff88';
  const PHOTO_BG = {
    'pos-gk': 'radial-gradient(ellipse at 50% 20%, #281c00 0%, #0e0800 100%)',
    'pos-df': 'radial-gradient(ellipse at 50% 20%, #001428 0%, #000810 100%)',
    'pos-mf': 'radial-gradient(ellipse at 50% 20%, #001a10 0%, #000808 100%)',
    'pos-fw': 'radial-gradient(ellipse at 50% 20%, #200800 0%, #0a0200 100%)',
  }[p.posClass] || 'radial-gradient(ellipse at 50% 20%, #111820, #060a10)';

  /* ════════════════════════════════════════
     1. PHOTO AREA
  ════════════════════════════════════════ */
  const ftCls = p.ft === 'L' ? 'ft-l' : p.ft === 'B' ? 'ft-b' : 'ft-r';
  headEl.innerHTML =
    /* bg */
    '<div style="position:absolute;inset:0;background:' + PHOTO_BG + '"></div>' +
    /* giant ghost jersey number */
    '<div style="position:absolute;top:50%;left:50%;transform:translate(-50%,-55%);' +
      'font-family:var(--font-display);font-weight:800;font-size:240px;' +
      'color:rgba(255,255,255,0.038);letter-spacing:-0.04em;line-height:1;' +
      'user-select:none;pointer-events:none;white-space:nowrap">' +
      String(p.n).padStart(2,'0') + '</div>' +
    /* silhouette placeholder */
    '<div style="position:absolute;bottom:56px;left:50%;transform:translateX(-50%);' +
      'width:130px;height:170px;' +
      'background:repeating-linear-gradient(45deg,rgba(255,255,255,0.016) 0px,' +
        'rgba(255,255,255,0.016) 2px,transparent 2px,transparent 9px);' +
      'border-radius:48% 48% 36% 36%/60% 60% 38% 38%;' +
      'border:1px dashed rgba(255,255,255,0.07)"></div>' +
    /* top-right: avg + last rating badges */
    '<div style="position:absolute;top:12px;right:12px;display:flex;gap:6px">' +
      (p.avg != null ?
        '<div style="width:46px;height:46px;border-radius:10px;background:rgba(0,0,0,0.68);' +
          'border:1px solid rgba(255,255,255,0.12);backdrop-filter:blur(6px);' +
          'display:flex;flex-direction:column;align-items:center;justify-content:center;gap:1px">' +
          '<div style="font-family:var(--font-display);font-size:17px;font-weight:700;line-height:1;color:' + rCol(p.avg) + '">' + avgStr + '</div>' +
          '<div style="font-family:var(--font-mono);font-size:7px;color:rgba(255,255,255,0.38);letter-spacing:0.06em">AVG</div>' +
        '</div>' : '') +
      (p.last != null ?
        '<div style="width:46px;height:46px;border-radius:10px;background:rgba(0,0,0,0.68);' +
          'border:1px solid rgba(255,255,255,0.12);backdrop-filter:blur(6px);' +
          'display:flex;flex-direction:column;align-items:center;justify-content:center;gap:1px">' +
          '<div style="font-family:var(--font-display);font-size:17px;font-weight:700;line-height:1;color:' + rCol(p.last) + '">' + lastStr + '</div>' +
          '<div style="font-family:var(--font-mono);font-size:7px;color:rgba(255,255,255,0.38);letter-spacing:0.06em">LAST</div>' +
        '</div>' : '') +
    '</div>' +
    /* top-left: PA star badge */
    (p.star ? '<div style="position:absolute;top:14px;left:14px;color:var(--gold-400);font-size:16px;text-shadow:0 0 10px rgba(245,185,66,0.6)">★</div>' : '') +
    /* bottom gradient overlay */
    '<div style="position:absolute;bottom:0;left:0;right:0;padding:22px 16px 14px;' +
      'background:linear-gradient(to top,rgba(0,0,0,0.9) 0%,rgba(0,0,0,0.5) 65%,transparent 100%)">' +
      '<div style="font-family:var(--font-display);font-size:26px;font-weight:700;' +
        'letter-spacing:-0.01em;line-height:1.1;color:#fff">' + p.name + '</div>' +
      '<div style="display:flex;gap:7px;align-items:center;margin-top:7px;flex-wrap:wrap">' +
        '<span class="pos ' + p.posClass + '">' + p.pos + '</span>' +
        (p.second && p.second !== '—' ? '<span class="pos ' + p.posClass + '" style="opacity:0.6">' + p.second + '</span>' : '') +
        '<span style="font-size:16px;line-height:1">' + flag + '</span>' +
        '<span style="font-family:var(--font-mono);font-size:10px;padding:2px 8px;border-radius:20px;' +
          'background:rgba(255,255,255,0.1);color:rgba(255,255,255,0.65);border:1px solid rgba(255,255,255,0.14)">' +
          'CA ' + caVal + '</span>' +
        (p.cap ? '<span class="cap">C</span>' : '') +
        (p.inj ? '<span style="font-family:var(--font-mono);font-size:9px;padding:2px 8px;border-radius:20px;' +
          'background:rgba(255,60,60,0.18);color:#ff7070;border:1px solid rgba(255,60,60,0.3)">INJURED</span>' : '') +
      '</div>' +
    '</div>';

  /* ════════════════════════════════════════
     2. CONDENSED INFO STRIP
  ════════════════════════════════════════ */
  const fitColor = p.fit < 60 ? 'var(--neg-400)' : p.fit < 80 ? 'var(--neu-400)' : 'var(--pos-400)';
  const morColor = {pos:'var(--brand-400)', neu:'var(--ink-1)', neg:'var(--neg-400)'}[p.morKind] || 'var(--ink-1)';
  const siItems = [
    ['Age',     p.age],
    ['Height',  p.hgt],
    ['Foot',    '<span class="ft ' + ftCls + '">' + p.ft + '</span>'],
    ['Fitness', '<span style="color:' + fitColor + '">' + p.fit + '%</span>'],
    ['Morale',  '<span style="color:' + morColor + ';font-size:11px">' + morText + '</span>'],
    ['Expires', p.exp],
  ];
  if (infoEl) {
    infoEl.innerHTML = siItems.map(function(row) {
      return '<div class="si-item"><span class="si-l">' + row[0] + '</span><span class="si-v">' + row[1] + '</span></div>';
    }).join('');
  }

  /* ════════════════════════════════════════
     3. BODY — stats + radar + sparkline
  ════════════════════════════════════════ */

  /* 3a. Compact season stats */
  var statsPanel =
    '<div class="panel">' +
    '<h4>2024/25 Season <span class="meta">PL · UCL R16</span></h4>' +
    '<div class="contract-grid c4">' +
    mkCg('Apps',    p.apps,  p.mins + ' mins') +
    mkCg('Goals',   p.g,     p90g + ' /90') +
    mkCg('Assists', p.a,     p90a + ' /90') +
    mkCg('Avg',     '<span style="color:' + rCol(p.avg) + '">' + avgStr + '</span>', 'season avg') +
    mkCg('xG',      p.xg != null ? p.xg.toFixed(1) : '—', 'expected') +
    mkCg('xA',      p.xa != null ? p.xa.toFixed(1) : '—', 'expected') +
    mkCg('Wage',    p.wage,  annualWage(p.wage) + '/yr') +
    mkCg('Value',   '<span style="color:var(--brand-400)">' + p.val + '</span>', 'market est.') +
    '</div></div>';

  /* 3b. Attribute values (seeded deterministic per player) */
  var ATTR_LABELS = {
    'pos-gk': ['REFLEXES','POSITNG','HANDLNG','DISTRBN','AERIAL','COMMAND'],
    'pos-df': ['DEFEND',  'AERIAL', 'PACE',   'PASSING','TACKLE','PHYSIC'],
    'pos-mf': ['PASSING', 'VISION', 'DRIBBLE','W-RATE', 'DEFEND','PHYSIC'],
    'pos-fw': ['PACE',    'SHOOT',  'DRIBBLE','PASSING','W-RATE','PHYSIC'],
  };
  var attrLabels = ATTR_LABELS[p.posClass] || ATTR_LABELS['pos-mf'];

  var BASE_PROFILES = {
    'pos-gk': [72, 68, 65, 55, 60, 62],
    'pos-df': [70, 68, 62, 60, 72, 68],
    'pos-mf': [74, 68, 72, 70, 56, 64],
    'pos-fw': [78, 76, 72, 62, 44, 66],
  };
  var baseP = BASE_PROFILES[p.posClass] || BASE_PROFILES['pos-mf'];
  var caScale = 0.48 + (p.ca / 5) * 0.72;
  var attrVals = baseP.map(function(v, i) {
    var seed = Math.sin(p.n * 13.7 + i * 7.3) * 10000;
    var jit = (seed - Math.floor(seed) - 0.5) * 14;
    return Math.min(97, Math.max(38, Math.round(v * caScale + jit)));
  });

  /* 3c. Radar SVG */
  function buildRadar(vals, lbls, accent) {
    var cx = 110, cy = 110, maxR = 76;
    var n = vals.length;
    var TAU = Math.PI * 2;

    function pt(i, r) {
      var a = TAU * i / n - Math.PI / 2;
      return [(cx + Math.cos(a) * r).toFixed(1), (cy + Math.sin(a) * r).toFixed(1)];
    }

    /* rings */
    var rings = [0.25, 0.5, 0.75, 1.0].map(function(f) {
      var pts = Array.from({length: n}, function(_, i) { return pt(i, maxR * f).join(','); });
      return '<polygon points="' + pts.join(' ') + '" fill="' +
        (f === 1 ? 'rgba(255,255,255,0.015)' : 'none') +
        '" stroke="rgba(255,255,255,' + (f === 1 ? '0.09' : '0.045') + ')" stroke-width="0.5"/>';
    }).join('');

    /* axes */
    var axes = Array.from({length: n}, function(_, i) {
      var end = pt(i, maxR);
      return '<line x1="' + cx + '" y1="' + cy + '" x2="' + end[0] + '" y2="' + end[1] + '" stroke="rgba(255,255,255,0.07)" stroke-width="0.5"/>';
    }).join('');

    /* data polygon */
    var dataPts = vals.map(function(v, i) { return pt(i, v / 100 * maxR).join(','); });
    var poly = '<polygon points="' + dataPts.join(' ') + '" fill="' + accent + '" fill-opacity="0.16" stroke="' + accent + '" stroke-width="1.5" stroke-linejoin="round"/>';

    /* vertex dots */
    var dots = vals.map(function(v, i) {
      var p2 = pt(i, v / 100 * maxR);
      return '<circle cx="' + p2[0] + '" cy="' + p2[1] + '" r="2.5" fill="' + accent + '" opacity="0.9"/>';
    }).join('');

    /* labels */
    var labelDist = maxR + 21;
    var textElems = lbls.map(function(lbl, i) {
      var a = TAU * i / n - Math.PI / 2;
      var lx = (cx + Math.cos(a) * labelDist).toFixed(1);
      var ly = (cy + Math.sin(a) * labelDist).toFixed(1);
      var cosA = Math.cos(a);
      var anchor = Math.abs(cosA) < 0.25 ? 'middle' : cosA > 0 ? 'start' : 'end';
      return '<text x="' + lx + '" y="' + ly + '" text-anchor="' + anchor +
        '" dominant-baseline="middle" font-family="\'JetBrains Mono\',monospace" font-size="7.5"' +
        ' fill="rgba(255,255,255,0.38)" letter-spacing="0.07em">' + lbl + '</text>';
    }).join('');

    return '<svg viewBox="0 0 220 220" width="186" height="186">' + rings + axes + poly + dots + textElems + '</svg>';
  }

  /* attribute bar list alongside radar */
  var attrBars = attrLabels.map(function(lbl, i) {
    var v = attrVals[i];
    var barColor = v >= 80 ? ACCENT : v >= 65 ? 'rgba(255,255,255,0.45)' : 'rgba(255,255,255,0.2)';
    return '<div style="display:grid;grid-template-columns:62px 1fr 22px;gap:5px;align-items:center;padding:2px 0">' +
      '<span style="font-family:var(--font-mono);font-size:8.5px;color:var(--ink-4);letter-spacing:0.06em">' + lbl + '</span>' +
      '<div style="height:3px;background:var(--bg-3);border-radius:2px;overflow:hidden">' +
        '<div style="height:100%;width:' + v + '%;background:' + barColor + ';border-radius:2px"></div></div>' +
      '<span style="font-family:var(--font-mono);font-size:10px;font-weight:600;color:var(--ink-1);text-align:right">' + v + '</span>' +
    '</div>';
  }).join('');

  var chartPanel =
    '<div class="panel">' +
    '<h4>Performance Profile</h4>' +
    '<div style="display:grid;grid-template-columns:186px 1fr;gap:14px;align-items:center">' +
      buildRadar(attrVals, attrLabels, ACCENT) +
      '<div style="display:flex;flex-direction:column;gap:4px">' + attrBars + '</div>' +
    '</div></div>';

  /* 3d. Match ratings sparkline */
  function genRatings(pl) {
    var avg2 = pl.avg || 6.8;
    var last2 = pl.last;
    var out = [];
    for (var i = 0; i < 12; i++) {
      var wave = Math.sin(pl.n * 0.31 + i * 0.88) * 0.38 + Math.cos(pl.n * 0.67 + i * 1.45) * 0.26;
      var v = avg2 + wave;
      if (i === 11 && last2 != null) v = last2;
      out.push(Math.min(9.5, Math.max(5.0, parseFloat(v.toFixed(1)))));
    }
    return out;
  }

  function buildSparkline(ratings, accent) {
    var W = 440, H = 58;
    var px = 18, py = 6;
    var minR = 5.0, maxR2 = 9.5;
    var n = ratings.length;
    var xStep = (W - px * 2) / (n - 1);
    function toY(v) { return py + (1 - (v - minR) / (maxR2 - minR)) * (H - py * 2); }
    function toX(i) { return px + i * xStep; }

    var pts = ratings.map(function(v, i) { return toX(i).toFixed(1) + ',' + toY(v).toFixed(1); });
    var line = 'M' + pts.join(' L');
    var fill = line + ' L' + toX(n-1).toFixed(1) + ',' + (H - py).toFixed(1) + ' L' + px + ',' + (H - py).toFixed(1) + ' Z';

    var grid = [6, 7, 8].map(function(v) {
      var y2 = toY(v).toFixed(1);
      return '<line x1="' + px + '" y1="' + y2 + '" x2="' + (W - px) + '" y2="' + y2 +
        '" stroke="rgba(255,255,255,0.045)" stroke-width="0.5" stroke-dasharray="2,4"/>' +
        '<text x="' + (px - 5) + '" y="' + y2 + '" text-anchor="end" dominant-baseline="middle"' +
        ' font-family="\'JetBrains Mono\',monospace" font-size="7.5" fill="rgba(255,255,255,0.24)">' + v + '</text>';
    }).join('');

    var dots = ratings.map(function(v, i) {
      var isLast = i === n - 1;
      return '<circle cx="' + toX(i).toFixed(1) + '" cy="' + toY(v).toFixed(1) + '"' +
        ' r="' + (isLast ? 3 : 2) + '" fill="' + accent + '" opacity="' + (isLast ? '1' : '0.5') + '"/>';
    }).join('');

    var allVals = ratings.slice();
    var hiVal = Math.max.apply(null, allVals).toFixed(1);
    var loVal = Math.min.apply(null, allVals).toFixed(1);
    var lastVal = ratings[ratings.length - 1];

    return {
      svg: '<svg viewBox="0 0 ' + W + ' ' + H + '" style="display:block;width:100%;height:auto">' +
        grid +
        '<path d="' + fill + '" fill="' + accent + '" opacity="0.07"/>' +
        '<path d="' + line + '" fill="none" stroke="' + accent + '" stroke-width="1.5" stroke-linejoin="round" stroke-linecap="round"/>' +
        dots + '</svg>',
      hi: hiVal, lo: loVal, last: lastVal
    };
  }

  var ratings = genRatings(p);
  var spark = buildSparkline(ratings, ACCENT);

  var sparkPanel =
    '<div class="panel">' +
    '<h4>Match Ratings <span class="meta">Last 12 appearances</span></h4>' +
    '<div style="display:flex;justify-content:space-between;align-items:baseline;' +
      'font-family:var(--font-mono);font-size:9px;color:var(--ink-4);letter-spacing:0.04em;margin-bottom:7px">' +
      '<span>Low <b style="color:var(--ink-2)">' + spark.lo + '</b></span>' +
      '<span>Season avg <b style="color:var(--ink-1)">' + avgStr + '</b></span>' +
      '<span>Last <b style="color:' + rCol(spark.last) + '">' + spark.last.toFixed(1) + '</b></span>' +
    '</div>' +
    spark.svg +
    '</div>';

  bodyEl.innerHTML = statsPanel + chartPanel + sparkPanel;
}

/* ── render ────────────────────────────────────────────────────────────────── */
const rowsEl = document.getElementById('rows');

function render() {
  const visible     = getVisible();
  const isContracts = currentTab === 'contracts';
  const isSpecial   = ['medical', 'dynamics', 'status'].includes(currentTab);
  const mainEl      = document.querySelector('.main');
  const specialEl   = document.getElementById('special-view');

  /* ── special sub-tab views ── */
  if (isSpecial) {
    if (mainEl)    mainEl.style.display    = 'none';
    if (specialEl) specialEl.style.display = 'block';
    if (currentTab === 'medical'  && window.renderMedicalView)  window.renderMedicalView(specialEl);
    if (currentTab === 'dynamics' && window.renderDynamicsView) window.renderDynamicsView(specialEl);
    if (currentTab === 'status'   && window.renderStatusView)   window.renderStatusView(specialEl);
    syncStatusBar(PLAYERS.length);
    return;
  }

  /* ── table views ── */
  if (mainEl)    mainEl.style.display    = 'grid';
  if (specialEl) specialEl.style.display = 'none';

  document.documentElement.style.setProperty('--tbl-grid',
    isContracts ? CONTRACTS_GRID : GENERAL_GRID
  );

  if (isContracts) {
    tableHead.innerHTML = contractsHeaderHtml();
    rowsEl.innerHTML = visible.length ? visible.map(contractRowHtml).join('') : emptyStateHtml();
  } else {
    tableHead.innerHTML = GENERAL_HEADER_HTML;
    rowsEl.innerHTML = visible.length ? visible.map(rowHtml).join('') : emptyStateHtml();
  }

  renderFormationPanel();
  const sp = PLAYERS.find(p => p.n === selectedNum);
  if (sp) renderSidePanel(sp);

  syncHeaders();
  syncStatusBar(visible.length);
}

function syncHeaders() {
  document.querySelectorAll('.table-head .h[data-col]').forEach(h => {
    const isSorted = h.dataset.col === sortCol;
    h.classList.toggle('sorted', isSorted);
    const arr = h.querySelector('.arr');
    if (arr) arr.textContent = isSorted ? (sortDir === 'asc' ? '↑' : '↓') : '';
  });
}

function syncStatusBar(n) {
  const el = document.getElementById('rowcount');
  if (el) el.textContent = n + ' rows visible · ' + PLAYERS.length + ' indexed';
}

/* ── wire: sub-tabs ─────────────────────────────────────────────────────────── */
document.querySelectorAll('.crumb .tab').forEach(tab => {
  tab.addEventListener('click', () => {
    const lbl = tab.textContent.trim().toLowerCase();
    if (lbl.startsWith('first'))    currentTab = 'firstXI';
    else if (lbl.startsWith('all')) currentTab = 'allPlayers';
    else if (lbl.startsWith('con')) currentTab = 'contracts';
    else if (lbl.startsWith('med')) currentTab = 'medical';
    else if (lbl.startsWith('dyn')) currentTab = 'dynamics';
    else if (lbl.startsWith('sta')) currentTab = 'status';
    else return;

    document.querySelectorAll('.crumb .tab').forEach(t => t.classList.remove('on'));
    tab.classList.add('on');
    // Reset position filter and show-all for contracts
    if (currentTab === 'firstXI') {
      posFilter = 'ALL';
      document.querySelectorAll('.seg-pos button').forEach(b => b.classList.toggle('on', b.dataset.pos === 'ALL'));
    }
    render();
  });
});

/* ── wire: column sort headers ─────────────────────────────────────────────── */
tableHead.addEventListener('click', e => {
  const h = e.target.closest('.h[data-col]');
  if (!h) return;
  const col = h.dataset.col;
  if (sortCol === col) {
    sortDir = sortDir === 'asc' ? 'desc' : 'asc';
  } else {
    sortCol = col;
    sortDir = (col==='name'||col==='pos'||col==='nat'||col==='agt') ? 'asc' : 'desc';
  }
  render();
});

/* ── wire: position filter ──────────────────────────────────────────────────── */
document.querySelectorAll('.seg-pos button').forEach(btn => {
  btn.addEventListener('click', () => {
    posFilter = btn.dataset.pos;
    document.querySelectorAll('.seg-pos button').forEach(b =>
      b.classList.toggle('on', b.dataset.pos === posFilter)
    );
    render();
  });
});

/* ── wire: status chips ─────────────────────────────────────────────────────── */
document.querySelectorAll('.chip[data-status]').forEach(chip => {
  chip.addEventListener('click', () => {
    const key = chip.dataset.status;
    if (statusFilters.has(key)) {
      statusFilters.delete(key);
      chip.classList.remove('on');
    } else {
      statusFilters.add(key);
      chip.dataset.wasWarn = chip.classList.contains('warn') ? '1' : '';
      chip.classList.remove('warn');
      chip.classList.add('on');
    }
    if (!statusFilters.has(key) && chip.dataset.wasWarn === '1') chip.classList.add('warn');
    render();
  });
});

/* ── wire: text search ──────────────────────────────────────────────────────── */
document.querySelector('.search input').addEventListener('input', e => {
  searchQuery = e.target.value.trim();
  render();
});

/* ── wire: row selection ────────────────────────────────────────────────────── */
rowsEl.addEventListener('click', e => {
  const r = e.target.closest('.row');
  if (!r) return;
  selectedNum = parseInt(r.dataset.num, 10);
  render();
});

/* ── keyboard shortcuts ─────────────────────────────────────────────────────── */
document.addEventListener('keydown', e => {
  if (e.key === '/' && document.activeElement.tagName !== 'INPUT') {
    e.preventDefault();
    document.querySelector('.search input').focus();
  }
  if (e.key === 'Escape') {
    document.querySelector('.search input').blur();
    searchQuery = '';
    document.querySelector('.search input').value = '';
    const calOverlay = document.getElementById('calOverlay');
    if (calOverlay) calOverlay.style.display = 'none';
    render();
  }
});

/* ── calendar modal ─────────────────────────────────────────────────────────── */
(function initCalendar() {
  const FIXTURES = {
    7: { opp:'@ Northcote', result:'W 2–0', cls:'w' },
    11:{ opp:'vs Portsea Vale', result:'D 1–1', cls:'d' },
    14:{ opp:'TODAY', result:'', cls:'today' },
    18:{ opp:'UCL 2nd Leg', result:'', cls:'upcoming fixture-ucl' },
    22:{ opp:'vs Kingsford', result:'', cls:'upcoming' },
    29:{ opp:'@ Westbrook', result:'', cls:'upcoming' },
  };

  const overlay = document.getElementById('calOverlay');
  const dateEl  = document.querySelector('.date');
  if (!overlay || !dateEl) return;

  dateEl.style.cursor = 'pointer';
  dateEl.title = 'Open calendar';

  function buildCal() {
    const grid = document.getElementById('calGrid');
    if (!grid) return;
    // March 2025: game-day March 1 = Sunday
    const days = 31, startDay = 0; // Sunday=0
    let html = '';
    for (let i = 0; i < startDay; i++) html += '<div class="cal-cell empty"></div>';
    for (let d = 1; d <= days; d++) {
      const f = FIXTURES[d];
      const cls = f ? f.cls : '';
      const tip = f ? f.opp : '';
      html += '<div class="cal-cell ' + cls + '" title="' + tip + '">' +
        '<span class="cal-day">' + d + '</span>' +
        (f && f.result ? '<span class="cal-res">' + f.result + '</span>' : '') +
        (f && !f.result && f.opp !== 'TODAY' ? '<span class="cal-fix">' + f.opp.replace('vs ','').replace('@ ','') + '</span>' : '') +
        '</div>';
    }
    grid.innerHTML = html;
  }

  dateEl.addEventListener('click', e => {
    e.stopPropagation();
    overlay.style.display = overlay.style.display === 'flex' ? 'none' : 'flex';
    buildCal();
  });

  overlay.addEventListener('click', e => {
    if (e.target === overlay) overlay.style.display = 'none';
  });

  document.getElementById('calClose')?.addEventListener('click', () => {
    overlay.style.display = 'none';
  });
})();

/* ── initial render ─────────────────────────────────────────────────────────── */
render();
