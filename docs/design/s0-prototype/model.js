/* Created: 2026-09-30. Purpose: pure, simulated S0 state transitions, separate from rendering. */
(() => {
  'use strict';
  const mentalities = ['Very Defensive', 'Defensive', 'Cautious', 'Balanced', 'Positive', 'Attacking', 'Very Attacking'];
  const consequences = [
    'Least risk in on-ball choices; deepest defensive line.',
    'Less risk in on-ball choices; deeper defensive line.',
    'Slightly less risk in on-ball choices; slightly deeper defensive line.',
    'Standard risk in on-ball choices and standard defensive line.',
    'Slightly more risk in on-ball choices; slightly higher defensive line.',
    'More risk in on-ball choices; higher defensive line.',
    'Most risk in on-ball choices; highest defensive line.'
  ];
  const speeds = [1, 3, 5, 10];
  const initial = () => ({ screen: 'MM', mentality: 3, draft: 3, speed: 0, minute: 0,
    statsOpen: false, requests: [], usedBench: [], usedOut: [], faultMinute: null });
  const active = s => ['MV-L', 'MV-P'].includes(s.screen);
  function reduce(s, action) {
    switch (action.type) {
      case 'OPEN_SETUP': return s.screen === 'MM' ? { ...initial(), screen: 'TS' } : s;
      case 'CANCEL_SETUP': return s.screen === 'TS' ? initial() : s;
      case 'DRAFT': return { ...s, draft: action.value };
      case 'START': return s.screen === 'TS' ? { ...s, screen: 'MV-0', mentality: s.draft } : s;
      case 'FIRST_FRAME': return s.screen === 'MV-0' ? { ...s, screen: 'MV-L' } : s;
      case 'PAUSE': return active(s) ? { ...s, screen: s.screen === 'MV-L' ? 'MV-P' : 'MV-L' } : s;
      case 'SPEED': return active(s) ? { ...s, speed: Math.max(0, Math.min(3, s.speed + action.step)) } : s;
      case 'STATS': return s.screen.startsWith('MV-') && s.screen !== 'MV-0' ? { ...s, statsOpen: !s.statsOpen } : s;
      case 'REQUEST': return active(s) ? { ...s, requests: [...s.requests, { ...action.request, status: 'Pending', minute: s.minute }] } : s;
      case 'ADVANCE': {
        if (s.screen !== 'MV-L') return s;
        let next = { ...s, minute: Math.min(90, s.minute + action.minutes) };
        next.requests = s.requests.map(request => {
          if (request.status !== 'Pending') return request;
          if (action.refuse) return { ...request, status: 'Refused', minute: next.minute };
          if (request.kind === 'mentality') next.mentality = request.value;
          if (request.kind === 'substitution') {
            next.usedBench = [...next.usedBench, request.bench];
            next.usedOut = [...next.usedOut, request.out];
          }
          return { ...request, status: 'Applied', minute: next.minute };
        });
        return next.minute === 90 ? reduce(next, { type: 'FULL_TIME' }) : next;
      }
      case 'FULL_TIME': return s.screen.startsWith('MV-') ? { ...s, screen: 'MV-FT', minute: 90,
        requests: s.requests.map(r => r.status === 'Pending' ? { ...r, status: 'Not applied — match ended' } : r) } : s;
      case 'REPORT': return s.screen === 'MV-FT' ? { ...s, screen: 'PR' } : s;
      case 'RETURN': return s.screen === 'PR' ? initial() : s;
      case 'FAULT': return { ...s, faultMinute: action.minute };
      default: return s;
    }
  }
  window.S0Model = Object.freeze({ mentalities, consequences, speeds, initial, active, reduce });
})();
