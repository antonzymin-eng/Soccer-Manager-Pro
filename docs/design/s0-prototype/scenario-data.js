/* Created: 2026-09-30. Purpose: explicitly synthetic ordinary-session UX fixture; never analytics evidence. */
(() => {
  'use strict';
  // Fixed presentation data, independent of user actions. Pitch positions come from the reference capture.
  const goalsAt = minute => minute < 28 ? [0, 0] : minute < 56 ? [1, 0] : minute < 78 ? [1, 1] : [2, 1];
  const line = (minute, home) => ({
    goals: goalsAt(minute)[home ? 0 : 1], possession: minute < 1 ? 0 : home ? 46 + Math.floor(minute / 18) : 47 - Math.floor(minute / 22),
    territory: minute < 1 ? 0 : home ? 49 + Math.floor(minute / 18) : 51 - Math.floor(minute / 18), fouls: Math.floor(minute / (home ? 10 : 8)),
    yellow: minute >= (home ? 62 : 49) ? 1 : 0, red: 0, offsides: Math.floor(minute / (home ? 40 : 32)),
    corners: Math.floor(minute / (home ? 17 : 23)), throwIns: Math.floor(minute / (home ? 5 : 6)),
    goalKicks: Math.floor(minute / (home ? 13 : 11)), substitutions: 0, xgAvailable: false, xg: null
  });
  window.S0Scenario = Object.freeze({ kind: 'Synthetic ordinary-session scenario; captured pitch only',
    snapshots: window.S0Reference.snapshots.map(frame => ({ ...frame, score: goalsAt(frame.minute),
      home: line(frame.minute, true), away: line(frame.minute, false) })) });
})();
