/* Created: 2026-09-30. Purpose: reproducible scripted Gates E/F walkthrough, never participant evidence. */
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { pathToFileURL } = require('node:url');
const { chromium } = require('playwright');

const root = __dirname;
const evidence = path.join(root, 'evidence', 'v0.5');
const base = pathToFileURL(path.join(root, 'index.html')).href;
const results = [];
let browser;
const record = (name, detail) => results.push({ name, result: 'PASS', detail });

async function open(query = '', viewport = { width: 1920, height: 1080 }) {
  const page = await browser.newPage({ viewport });
  page.on('pageerror', error => { throw error; });
  await page.clock.install({ time: new Date('2026-09-30T04:00:00Z') });
  await page.clock.pauseAt(new Date('2026-09-30T04:00:01Z'));
  await page.goto(base + query);
  return page;
}
async function has(page, phrase) { assert((await page.locator('main').innerText()).includes(phrase), phrase); }
async function keyTo(page, selector) {
  assert(await page.evaluate(() => document.activeElement !== document.body) || selector === '#open-setup',
    'Unexpected body focus before keyboard navigation to ' + selector);
  for (let index = 0; index < 150; index++) {
    if (await page.locator(selector).evaluate(el => el === document.activeElement)) return;
    await page.keyboard.press('Tab');
  }
  throw new Error('Keyboard could not reach ' + selector);
}
async function focused(page, id) {
  assert.equal(await page.evaluate(() => document.activeElement.id), id, 'focus destination: ' + id);
  assert(await page.locator('#' + id).evaluate(el => el.isConnected && !el.disabled), 'valid focus target');
}
async function dialogFocusLabels() {
  for (const width of [1366, 1920, 2560]) for (const scale of [1, 2]) {
    const p = await open(`?review=1&state=MV-L&scale=${scale}${scale === 2 ? '&pseudo=1' : ''}`,
      { width, height: width === 1366 ? 768 : 1080 });
    for (const [invoker, fields] of [['tactic', ['new-mentality']], ['substitute', ['outgoing', 'incoming']]]) {
      await p.locator('#' + invoker).click();
      for (const id of fields) {
        const label = await p.locator(`label[for="${id}"]`).innerText();
        assert.equal(label.startsWith('[') && label.includes('~'), scale === 2,
          'actual normal versus pseudo label rendering: ' + id);
        assert.equal(await p.evaluate(() => getComputedStyle(document.documentElement).fontSize), `${16 * scale}px`);
        if (id === 'incoming') await p.keyboard.press('Tab');
        await focused(p, id);
        const gap = await p.locator('#' + id).evaluate(el => {
          const label = document.querySelector(`label[for="${el.id}"]`);
          const style = getComputedStyle(el);
          return { visible: el.matches(':focus-visible'), width: parseFloat(style.outlineWidth),
            clearance: el.getBoundingClientRect().top - parseFloat(style.outlineOffset) -
              parseFloat(style.outlineWidth) - label.getBoundingClientRect().bottom };
        });
        assert(gap.visible && gap.width >= 3, 'visible dropdown focus: ' + id);
        assert(gap.clearance >= 4, `focus outline clear of label: ${id}, ${width}, ${scale}`);
      }
      await p.keyboard.press('Escape');
    }
    await p.close();
  }
  record('dialog focus and label separation', 'Both dialogs and all three dropdowns retain visible 3px focus outlines with at least 4px label clearance at all three widths, normal text and pseudo-locale/200% text.');
}
async function faultProgression() {
  for (const direct of [false, true]) {
    const p = await open(direct ? '?state=MV-0&fixture=fault' : '?fixture=fault');
    if (!direct) { await p.locator('#open-setup').click(); await p.locator('#start').click(); }
    await p.clock.runFor(2000);
    await has(p, 'Minute 0');
    await p.locator('#stats').click();
    const cells = () => p.locator('tbody td').allTextContents();
    const expected = minute => p.evaluate(minute => {
      const s = window.S0Scenario.snapshots.reduce((s, f) => f.minute <= minute ? f : s,
        window.S0Scenario.snapshots[0]);
      return ['goals', 'possession', 'territory', 'fouls', 'yellow', 'red', 'offsides', 'corners', 'throwIns', 'goalKicks']
        .flatMap(key => [s.home[key], s.away[key]]).map(v => Number.isInteger(v) ? String(v) : v.toFixed(1));
    }, minute);
    assert.equal(await p.locator('.warning').count(), 0, 'no future fault at first frame');
    assert.deepEqual(await cells(), await expected(0), 'first-frame statistics');
    await p.clock.runFor(17000);
    await has(p, 'Minute 17');
    assert.equal(await p.locator('.warning').count(), 0, 'healthy until cutoff');
    assert.deepEqual(await cells(), await expected(17), 'pre-cutoff statistics');
    await p.clock.runFor(1000);
    await has(p, 'Minute 18');
    await has(p, 'Statistics stopped at minute 18');
    const frozen = await cells();
    assert.deepEqual(frozen, await expected(18), 'fault begins at cutoff');
    await p.clock.runFor(3000);
    await has(p, 'Minute 21');
    assert.deepEqual(await cells(), frozen, 'statistics remain frozen after cutoff');
    await p.locator('#pause').click();
    await p.clock.runFor(2000);
    await has(p, 'Minute 21');
    assert.deepEqual(await cells(), frozen, 'paused fault snapshot retained');
    await p.locator('#pause').click();
    record('fault progression: ' + (direct ? 'direct first frame' : 'MM to match'),
      'Minute 0/17 healthy with current figures; fault activates exactly at 18; figures remain frozen while the clock reaches 21 and pauses.');
    if (!direct) {
      await p.clock.runFor(69000);
      await has(p, 'Full time');
      await has(p, 'Statistics incomplete — stopped at minute 18. Final score remains available.');
      assert(!(await p.locator('main').innerText()).includes('The match continues'));
      assert(!(await p.locator('main').innerText()).includes('Final statistics are available'));
      assert.deepEqual(await cells(), frozen, 'full-time figures remain partial');
      assert(await p.locator('#report').isEnabled());
      await geometry(p, 'full-time statistics fault');
      await exportWireframe(p, 'mv-ft-statistics-fault');
      await p.locator('#report').click();
      await has(p, 'Final score remains available');
      assert.equal(await p.locator('details').getAttribute('open'), null);
      record('full-time fault notice', 'Normal fault journey reaches full time with ended-state copy and the frozen open panel; report retains its incomplete disclosure and final score.');
    }
    await p.close();
  }
  const fast = await open('?fixture=fault');
  await fast.locator('#open-setup').click(); await fast.locator('#start').click();
  await fast.clock.runFor(2000);
  for (let i = 0; i < 3; i++) await fast.locator('#faster').click();
  await fast.clock.runFor(1000);
  await has(fast, 'Minute 10');
  assert.equal(await fast.locator('.warning').count(), 0);
  await fast.clock.runFor(1000);
  await has(fast, 'Minute 20'); await has(fast, 'Statistics stopped at minute 18');
  await fast.locator('#stats').click();
  await has(fast, 'partial figures through minute 18');
  record('fault cutoff at fast speed', '10× progression jumps 10→20; fault activates on the crossing tick and uses cutoff 18, never current minute 20.');
  await fast.close();
}
async function activate(page, selector, keyboard) {
  if (keyboard) { await keyTo(page, selector); await page.keyboard.press('Enter'); }
  else await page.locator(selector).click();
}
async function geometry(page, label) {
  const issues = await page.evaluate(() => {
    const failures = [];
    if (document.documentElement.scrollWidth > innerWidth + 1) failures.push('document horizontal overflow');
    for (const el of document.querySelectorAll('main button, main label, main th, main td, main h1, main .reason, main .feedback, main .warning, dialog[open] button, dialog[open] p')) {
      if (!el.getClientRects().length) continue;
      if (el.scrollWidth > el.clientWidth + 2) failures.push('clipped ' + el.tagName + ': ' + el.textContent.slice(0, 50));
    }
    const markers = [...document.querySelectorAll('.agent')].map(el => ({ label: el.textContent, box: el.getBoundingClientRect() }));
    for (let i = 0; i < markers.length; i++) for (let j = i + 1; j < markers.length; j++) {
      const a = markers[i], b = markers[j];
      if (Math.min(a.box.right, b.box.right) - Math.max(a.box.left, b.box.left) > .5 &&
          Math.min(a.box.bottom, b.box.bottom) - Math.max(a.box.top, b.box.top) > .5)
        failures.push('overlapping markers: ' + a.label + ' / ' + b.label);
    }
    return failures;
  });
  assert.deepEqual(issues, [], label);
  record('geometry: ' + label, 'No horizontal overflow, clipped critical labels/cells/buttons or overlapping marker labels. Vertical scrolling permitted.');
}
async function exportWireframe(page, name) {
  await page.emulateMedia({ media: 'screen' });
  // PDF page height must not enlarge vh-based regions and push the bottom onto another page.
  const frozen = await page.evaluate(() => {
    let css = '';
    const pitch = document.querySelector('.pitch');
    if (pitch) {
      const box = pitch.getBoundingClientRect();
      css += `.pitch { width:${box.width}px !important; height:${box.height}px !important; max-height:${box.height}px !important; aspect-ratio:auto !important; }`;
    }
    const entry = document.querySelector('.entry');
    if (entry) {
      const style = getComputedStyle(entry);
      css += `.entry { margin-top:${style.marginTop} !important; margin-bottom:${style.marginBottom} !important; }`;
    }
    return css;
  });
  if (frozen) await page.addStyleTag({ content: frozen });
  const size = await page.evaluate(() => ({ width: innerWidth, height: document.documentElement.scrollHeight }));
  await page.pdf({ path: path.join(evidence, name + '.pdf'), width: size.width + 'px',
    height: size.height + 'px', printBackground: true, margin: { top: 0, bottom: 0, left: 0, right: 0 } });
}
async function journey(query, viewport, keyboard, label) {
  const page = await open(query, viewport);
  await activate(page, '#open-setup', keyboard);
  await has(page, 'Tactics Setup');
  if (keyboard) { await keyTo(page, 'input[value="3"]'); await page.keyboard.press('ArrowDown'); }
  else await page.locator('input[value="4"]').click();
  await activate(page, '#back', keyboard);
  await has(page, 'System XI');
  await activate(page, '#open-setup', keyboard);
  assert(await page.locator('input[value="3"]').isChecked(), 'Cancel discards draft');
  if (keyboard) { await keyTo(page, 'input[value="3"]'); await page.keyboard.press('ArrowDown'); }
  else await page.locator('input[value="4"]').click();
  await activate(page, '#start', keyboard);
  await has(page, 'controls unavailable until the first frame.');
  assert(await page.locator('#pause').isDisabled());
  await page.clock.runFor(2000);
  await has(page, 'Current Mentality: Positive');
  await has(page, 'First half');
  await activate(page, '#faster', keyboard);
  await has(page, 'Selected speed 3×');
  await activate(page, '#pause', keyboard);
  await has(page, 'Paused');
  await activate(page, '#tactic', keyboard);
  if (keyboard) { await page.keyboard.press('ArrowDown'); await keyTo(page, '#submit-change'); await page.keyboard.press('Enter'); }
  else { await page.locator('#new-mentality').selectOption('5'); await page.locator('#submit-change').click(); }
  await has(page, 'Pending');
  await focused(page, 'request-feedback');
  await has(page, 'Current Mentality: Positive');
  await page.clock.runFor(2000);
  await has(page, 'Pending');
  await activate(page, '#pause', keyboard);
  await page.clock.runFor(1000);
  await has(page, 'Current Mentality: Attacking');
  await has(page, 'Applied at minute');
  await activate(page, '#substitute', keyboard);
  assert.equal(await page.locator('#incoming option').count(), 7, 'seven bench slots');
  if (keyboard) { await page.keyboard.press('Escape'); }
  else await page.locator('#cancel-change').click();
  assert.equal(await page.evaluate(() => document.activeElement.id), 'substitute', 'cancel restores invoker');
  await has(page, 'Substitutions used: 0 / 5');
  await activate(page, '#substitute', keyboard);
  await activate(page, '#submit-change', keyboard);
  await focused(page, 'request-feedback');
  await page.clock.runFor(1000);
  await focused(page, 'request-feedback');
  await has(page, 'Substitutions used: 1 / 5');
  await activate(page, '#stats', keyboard);
  await has(page, 'Possession %');
  assert.equal(await page.getByRole('rowheader', { name: 'Substitutions', exact: true }).count(), 0,
    'Captured zero must not contradict the simulated applied count');
  await has(page, 'Captured reference pitch — simulated substitutions do not replace these markers');
  assert.equal(await page.locator('th').filter({ hasText: /shots|xG/i }).count(), 0);
  await geometry(page, label + ' live statistics');
  await activate(page, '#stats', keyboard);
  await page.clock.runFor(30000);
  await has(page, 'Full time');
  assert(await page.locator('#tactic').isDisabled());
  assert(await page.locator('#pause').isDisabled());
  await activate(page, '#report', keyboard);
  await has(page, 'Post-Match Report');
  assert.equal(await page.getByRole('button', { name: /^(save|load|continue|settings|rematch|replay|quit)/i }).count(), 0);
  await geometry(page, label + ' report');
  await activate(page, '#return', keyboard);
  await has(page, 'System XI');
  record(label, 'S0-T1–T7 and PM-1 substitution completed; paused request stayed pending, resumed request applied, cancel reset Balanced, report gated by full time.');
  await page.close();
}
async function contrast() {
  const p = await open('?review=1&state=MV-0');
  const pairs = await p.evaluate(() => ['h1', '#pause', '.reason', '.prototype-notice'].map(selector => {
    const el = document.querySelector(selector), style = getComputedStyle(el);
    let surface = el;
    while (getComputedStyle(surface).backgroundColor === 'rgba(0, 0, 0, 0)' && surface.parentElement) surface = surface.parentElement;
    return { selector, foreground: style.color, background: getComputedStyle(surface).backgroundColor };
  }));
  const luminance = rgb => rgb.match(/[\d.]+/g).slice(0, 3).map(Number).map(c => c / 255).map(c => c <= .04045 ? c / 12.92 : ((c + .055) / 1.055) ** 2.4).reduce((s, v, i) => s + v * [.2126, .7152, .0722][i], 0);
  for (const pair of pairs) {
    const a = luminance(pair.foreground), b = luminance(pair.background);
    pair.ratio = Number(((Math.max(a, b) + .05) / (Math.min(a, b) + .05)).toFixed(2));
    assert(pair.ratio >= 4.5, pair.selector);
  }
  await p.locator('#page-heading').focus();
  const outline = await p.locator('#page-heading').evaluate(el => getComputedStyle(el).outline);
  assert(outline.includes('3px') && outline.includes('rgb(22, 22, 22)'));
  record('computed contrast', { pairs, focus: '3px #161616 outline against white; selection checked radio and text; disabled dashed border and persistent reason.' });
  await p.close();
}
async function fixtures() {
  for (const width of [1366, 1920, 2560]) {
    for (const fixture of ['ordinary', 'fault', 'events', 'long-names', 'scoreline', 'limit']) {
      const p = await open(`?review=1&state=MV-L&fixture=${fixture}&pseudo=1&scale=2&captions=1`, { width, height: width === 1366 ? 768 : 1080 });
      await p.locator('#stats').click();
      await geometry(p, `${width} / ${fixture} / pseudo / 200%`);
      if (width === 1366 && fixture === 'fault') await exportWireframe(p, 'stress-fault-1366');
      if (fixture === 'limit') { assert(await p.locator('#substitute').isDisabled()); await has(p, 'all substitutions used'); }
      if (fixture === 'fault') await has(p, 'Statistics stopped at minute 18');
      if (fixture !== 'limit') {
        await p.locator('#substitute').click();
        await geometry(p, `${width} / ${fixture} / substitution dialog`);
        for (let i = 0; i < 12; i++) {
          await p.keyboard.press('Tab');
          assert(await p.evaluate(() => document.activeElement.closest('dialog') !== null), 'modal focus trap');
        }
        for (let i = 0; i < 12; i++) {
          await p.keyboard.press('Shift+Tab');
          assert(await p.evaluate(() => document.activeElement.closest('dialog') !== null), 'reverse modal focus trap');
        }
        await p.keyboard.press('Escape');
        assert.equal(await p.evaluate(() => document.activeElement.id), 'substitute');
      }
      await p.close();
    }
  }
  const fault = await open('?review=1&state=PR&fixture=fault');
  await has(fault, 'Statistics incomplete — stopped at minute 18');
  await exportWireframe(fault, 'report-incomplete');
  assert.equal(await fault.locator('details').getAttribute('open'), null);
  assert(await fault.locator('#return').isEnabled());
  const score = await fault.locator('.score').innerText();
  await fault.locator('summary').click();
  await exportWireframe(fault, 'report-partial-open');
  await has(fault, 'partial figures through minute 18 — not full-match totals');
  record('statistics failure', `Final frame score ${score} remained visible, partial table hidden by default, frozen minute 18 stated, return enabled.`);
  await fault.close();
  const refusal = await open('?state=MV-L&fixture=refusal');
  await refusal.locator('#tactic').click();
  await refusal.locator('#new-mentality').selectOption('5');
  await refusal.locator('#submit-change').click();
  await refusal.clock.runFor(1000);
  await has(refusal, 'Refused. Current Mentality unchanged.'); await has(refusal, 'Current Mentality: Balanced');
  record('refused command', 'Persistent inline refusal; last applied value remained Balanced; Change Mentality enabled again.');
  await refusal.close();
  const pending = await open();
  await pending.locator('#open-setup').click();
  await pending.locator('#start').click();
  await pending.clock.runFor(91000); // First frame at 2 seconds, then 89 live minutes.
  await has(pending, 'Minute 89');
  await pending.locator('#pause').click();
  await pending.locator('#tactic').click();
  await pending.locator('#new-mentality').selectOption('5');
  await pending.locator('#submit-change').click();
  await focused(pending, 'request-feedback');
  await pending.clock.runFor(2000);
  await has(pending, 'Pending'); await has(pending, 'Minute 89');
  await pending.locator('#pause').click();
  await pending.clock.runFor(1000);
  await has(pending, 'Not applied — match ended');
  await has(pending, 'Current Mentality: Balanced');
  await focused(pending, 'report');
  await exportWireframe(pending, 'mv-ft-not-applied');
  record('end race', 'Normal MM→TS→MV journey reached minute 89; paused request stayed Pending; resume/next live tick reached the whistle before application. Not applied, Balanced retained, report focused.');
  await pending.close();
  const waiting = await open('?state=MV-0&fixture=waiting');
  await waiting.clock.runFor(10000);
  await has(waiting, 'controls unavailable until the first frame.');
  assert(await waiting.locator('#pause').isDisabled());
  record('no match frame', 'Waiting fixture remained non-live, score/clock withheld, all match actions disabled with persistent reason.');
  await waiting.close();
  const boundary = await open('?review=1&state=MV-L');
  for (let i = 0; i < 3; i++) await boundary.locator('#faster').click();
  await focused(boundary, 'pause');
  await has(boundary, 'Selected speed 10×');
  assert(await boundary.locator('#faster').isDisabled());
  await has(boundary, 'Already at the fastest speed');
  await boundary.locator('#pause').click();
  await has(boundary, 'Selected speed 10× • Paused');
  await boundary.locator('#slower').click();
  await has(boundary, 'Selected speed 5× • Paused');
  for (let i = 0; i < 2; i++) { await keyTo(boundary, '#slower'); await boundary.keyboard.press('Enter'); }
  await focused(boundary, 'pause');
  assert(await boundary.locator('#slower').isDisabled());
  for (let i = 0; i < 3; i++) { await keyTo(boundary, '#faster'); await boundary.keyboard.press('Enter'); }
  await focused(boundary, 'pause');
  record('speed boundaries', 'Mouse and keyboard reach 1×/10×; newly disabled Slower/Faster recover focus to Pause/Resume immediately. Pause retains rung.');
  await boundary.close();
  const history = await open('?state=MV-L&fixture=events');
  await keyTo(history, '#earlier-feedback-toggle');
  await history.keyboard.press('Enter');
  await focused(history, 'earlier-feedback-toggle');
  await history.clock.runFor(5000);
  assert(await history.locator('#earlier-feedback').evaluate(el => el.open));
  await focused(history, 'earlier-feedback-toggle');
  await keyTo(history, '#pause'); await history.keyboard.press('Enter');
  await has(history, 'Paused');
  assert(await history.locator('#earlier-feedback').evaluate(el => el.open));
  await history.keyboard.press('Enter');
  await history.clock.runFor(1000);
  assert(await history.locator('#earlier-feedback').evaluate(el => el.open));
  record('live earlier feedback', 'Keyboard-opened disclosure remains open and summary keeps focus over five live ticks; stays open across pause/resume and another tick. Timer not frozen by review=1.');
  await history.close();
  const chronology = await open('?fixture=events');
  await chronology.locator('#open-setup').click(); await chronology.locator('#start').click();
  await chronology.clock.runFor(2000);
  await has(chronology, 'Minute 15');
  const minutes = await chronology.locator('.feedback').allTextContents();
  assert(minutes.every(line => Number(line.match(/minute (\d+)/)[1]) <= 15));
  record('dense feedback chronology', 'Synthetic prior changes are seeded at the first live frame, minute 15; all 15 Applied timestamps are at or before the visible clock. Waiting state has no future Applied records.');
  await chronology.close();
  for (const [fixture, expected] of [['ordinary', '2 – 1'], ['scoreline', '19 – 9']]) {
    const score = await open('?review=1&state=PR&fixture=' + fixture);
    assert.equal(await score.locator('.score').innerText(), expected);
    const goals = score.getByRole('row', { name: /^Goals recorded/ });
    assert.deepEqual(await goals.locator('td').allTextContents(), expected.split(' – '));
    await score.close();
  }
  record('score/statistics coherence', 'Synthetic ordinary scenario finishes 2–1; unmodified unusual capture finishes 19–9. Each scoreboard agrees with its goals table; source types labelled explicitly.');
  const endDialog = await open('?state=MV-L');
  for (let i = 0; i < 3; i++) await endDialog.locator('#faster').click();
  await endDialog.locator('#tactic').click();
  await endDialog.clock.runFor(7000);
  assert.equal(await endDialog.locator('dialog').getAttribute('open'), null);
  assert.equal(await endDialog.evaluate(() => document.activeElement.id), 'report');
  record('full time while staging', 'Unsubmitted chooser closed at full time; no change requested; report received focus.');
  await endDialog.close();
}
async function reviewImages() {
  const p = await open('?state=MV-L');
  await p.locator('#tactic').click();
  await p.locator('#new-mentality').selectOption('5');
  await exportWireframe(p, 'mentality-dialog');
  await p.locator('#cancel-change').click();
  await p.locator('#substitute').click();
  await p.locator('#outgoing').selectOption('3');
  await p.locator('#incoming').selectOption('2');
  await exportWireframe(p, 'substitution-dialog');
  await p.locator('#cancel-change').click();
  await p.locator('#tactic').click();
  await p.locator('#new-mentality').selectOption('5');
  await p.locator('#submit-change').click();
  await has(p, 'Pending'); await focused(p, 'request-feedback');
  await exportWireframe(p, 'mv-live-pending');
  await p.locator('#pause').click();
  await has(p, 'waiting; resume to continue');
  await exportWireframe(p, 'mv-paused-pending');
  await p.locator('#pause').click();
  await p.clock.runFor(1000);
  await has(p, 'Applied at minute 25');
  await p.locator('#substitute').click();
  await p.locator('#outgoing').selectOption('3');
  await p.locator('#incoming').selectOption('2');
  await p.locator('#submit-change').click();
  await p.clock.runFor(1000);
  await has(p, 'Substitutions used: 1 / 5');
  await exportWireframe(p, 'mv-live-applied');
  await p.locator('#stats').click();
  await exportWireframe(p, 'mv-live-statistics');
  await p.clock.runFor(64000);
  await has(p, 'Full time');
  assert(await p.locator('#stats').isDisabled());
  assert(await p.locator('table').count(), 'Previously open statistics remain visible at full time');
  record('image interaction coverage', 'Exports both staged dialogs, live/paused Pending, Mentality plus substitution Applied and healthy live statistics, from exercised controls. Full-time Not applied and incomplete report disclosures exported by their existing fixtures.');
  await p.close();
  const refused = await open('?state=MV-L&fixture=refusal');
  await refused.locator('#tactic').click();
  await refused.locator('#new-mentality').selectOption('5');
  await refused.locator('#submit-change').click();
  await refused.clock.runFor(1000);
  await has(refused, 'Refused'); await has(refused, 'Current Mentality: Balanced');
  await exportWireframe(refused, 'mv-live-refused');
  await refused.close();
  const start = await open('?review=1&state=MV-0');
  const copy = await start.locator('main').innerText();
  assert.equal(copy.split('controls unavailable until the first frame').length - 1, 1);
  assert.equal(await start.locator('.pitch-lines').count(), 0);
  record('starting copy', 'One lock explanation; placeholder has no pitch markings crossing its text.');
  await start.close();
  const ended = await open('?review=1&state=MV-FT');
  assert(await ended.locator('#stats').isDisabled());
  await has(ended, 'Final statistics are available in the match report.');
  const caption = await ended.locator('main').innerText();
  assert(!caption.includes('Restart:') && !caption.includes('Possession: loose ball'));
  await has(ended, 'Home attacks right');
  assert.equal(await ended.locator('.pitch-lines .goal').count(), 2);
  record('pitch presentation', 'Raw restart/holder captions omitted; two goals, penalty/goal areas and Home-right/Away-left direction shown, matching fixed Stage-0 source convention.');
  record('full-time statistics proposal', 'Statistics cannot be reopened at full time; an already-open panel remains visible. Report destination explicit. This existing behavior remains a proposed owner decision, not approval.');
  await ended.close();
  const partial = await open('?review=1&state=PR&fixture=fault');
  await partial.locator('summary').click();
  const cutoff = await partial.getByRole('row', { name: /^Possession %/ }).locator('td').allTextContents();
  const ordinary = await open('?review=1&state=PR');
  const final = await ordinary.getByRole('row', { name: /^Possession %/ }).locator('td').allTextContents();
  assert.notDeepEqual(cutoff, final);
  assert(await partial.locator('thead th').nth(1).evaluate(el => getComputedStyle(el).textAlign === 'right'));
  record('partial statistics and alignment', `Synthetic minute-18 possession ${cutoff.join('/')} differs from final ${final.join('/')}; numeric Home/Away headers and cells right-aligned.`);
  await partial.close(); await ordinary.close();
}
async function fullTimeFaultDelta() {
  const healthy = await open('?review=1&state=MV-FT');
  await has(healthy, 'Final statistics are available in the match report.');
  record('healthy full-time report note', 'Healthy full-time state retains its final-statistics report note.');
  await healthy.close();
  const fault = await open('?fixture=fault');
  await fault.locator('#open-setup').click(); await fault.locator('#start').click();
  await fault.clock.runFor(2000);
  await fault.locator('#stats').click();
  await fault.clock.runFor(90000);
  await has(fault, 'Full time');
  await has(fault, 'Statistics incomplete — stopped at minute 18. Final score remains available.');
  assert(!(await fault.locator('main').innerText()).includes('Final statistics are available'));
  await has(fault, 'partial figures through minute 18 — not full-match totals');
  assert(await fault.locator('#report').isEnabled());
  await geometry(fault, 'full-time statistics fault delta');
  await exportWireframe(fault, 'mv-ft-statistics-fault');
  await fault.locator('#report').click();
  await has(fault, 'Statistics incomplete — stopped at minute 18');
  assert.equal(await fault.locator('details').getAttribute('open'), null);
  assert(await fault.locator('#return').isEnabled());
  record('faulted full-time report note', 'Normal fault journey reaches full time with partial figures and no promise of final statistics; report disclosure stays closed and Return enabled.');
  await fault.close();
}
function fingerprints() {
  const digest = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
  const files = ['index.html', 'prototype.css', 'model.js', 'prototype.js', 'reference-data.js', 'scenario-data.js', 'verify.cjs'];
  const hashes = Object.fromEntries(files.map(file => [file, digest(path.join(root, file))]));
  const imageHashes = Object.fromEntries(fs.readdirSync(evidence).filter(file => file.endsWith('.pdf')).sort().map(file => [file, digest(path.join(evidence, file))]));
  assert.equal(Object.keys(imageHashes).length, 19);
  return { hashes, imageHashes };
}
(async () => {
  fs.mkdirSync(evidence, { recursive: true });
  browser = await chromium.launch({ executablePath: process.env.UX_BROWSER, args: ['--no-sandbox'] });
  if (process.argv.includes('--full-time-fault-delta')) {
    await fullTimeFaultDelta();
    const baseline = fs.readFileSync(path.join(evidence, 'walkthrough.json'));
    const current = fingerprints();
    const unchangedImages = Object.keys(current.imageHashes).filter(file => file !== 'mv-ft-statistics-fault.pdf');
    for (const file of unchangedImages) assert.equal(current.imageHashes[file], JSON.parse(baseline).imageHashes[file],
      'delta must preserve other review images: ' + file);
    fs.writeFileSync(path.join(evidence, 'full-time-fault-delta.json'), JSON.stringify({
      created: '2026-09-30', purpose: 'Focused full-time fault-copy correction; not a new full walkthrough or owner approval',
      runId: 'UX-GE-S0-20260930-05-DELTA-01', prototype: 's0-prototype v0.5',
      baselineCommit: 'a859ea12b9fa5ae9cf366e86c10407d03a9cf885', baselineRunId: 'UX-GE-S0-20260930-05',
      baselineEvidenceSha256: crypto.createHash('sha256').update(baseline).digest('hex'),
      regeneratedImages: ['mv-ft-statistics-fault.pdf'], unchangedImages, browser: browser.version(), ...current, results
    }, null, 2) + '\n');
    console.log(`PASS: ${results.length} focused checks; only mv-ft-statistics-fault.pdf regenerated.`);
    return;
  }
  for (const state of ['MM', 'TS', 'MV-0', 'MV-L', 'MV-P', 'MV-FT', 'PR']) {
    const p = await open(`?review=1&state=${state}`);
    await geometry(p, 'wireframe ' + state);
    await exportWireframe(p, state.toLowerCase());
    await p.close();
  }
  for (const width of [1366, 1920, 2560]) {
    await journey('', { width, height: width === 1366 ? 768 : 1080 }, false, `mouse journey ${width}`);
  }
  await journey('', { width: 1366, height: 768 }, true, 'keyboard journey 1366');
  await journey('?pseudo=1&scale=2&fixture=fault', { width: 1366, height: 768 }, false, 'fault/pseudo/200% journey');
  for (const fixture of ['long-names', 'events', 'scoreline']) {
    await journey(`?fixture=${fixture}`, { width: 1366, height: 768 }, false, `${fixture} complete journey`);
  }
  await contrast();
  await dialogFocusLabels();
  await faultProgression();
  await fixtures();
  await reviewImages();
  const { hashes, imageHashes } = fingerprints();
  fs.writeFileSync(path.join(evidence, 'walkthrough.json'), JSON.stringify({ created: '2026-09-30', purpose: 'Executed S0 scripted resilience evidence; not participant results', runId: 'UX-GE-S0-20260930-05', supersedes: 'UX-GE-S0-20260930-04: preserved approved-v0.4 record; run 05 corrects fault timing, full-time notice and actual normal-text coverage', journey: 'S0', prototype: 's0-prototype v0.5', textScaleMethod: 'scale=2 doubles root font size from 16px to 32px; browser zoom remains 100%', date: '2026-09-30 (UTC)', runner: 'Codex scripted/self-walkthrough', browser: browser.version(), hashes, imageHashes, results }, null, 2) + '\n');
  console.log(`PASS: ${results.length} recorded checks; 19 revision-review PDFs including full-time statistics failure.`);
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(async () => { if (browser) await browser.close(); });
