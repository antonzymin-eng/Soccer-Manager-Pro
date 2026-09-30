/* Created: 2026-09-30. Purpose: reproducible scripted Gates E/F walkthrough, never participant evidence. */
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { pathToFileURL } = require('node:url');
const { chromium } = require('playwright');

const root = __dirname;
const evidence = path.join(root, 'evidence');
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
    return failures;
  });
  assert.deepEqual(issues, [], label);
  record('geometry: ' + label, 'No document horizontal overflow or clipped critical labels/cells/buttons. Vertical scrolling permitted.');
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
  await has(page, 'waiting for the first frame');
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
  await has(fault, 'partial figures through minute 18 — not full-match totals');
  record('statistics failure', `Final frame score ${score} remained visible, partial table hidden by default, frozen minute 18 stated, return enabled.`);
  await fault.close();
  const refusal = await open('?state=MV-L&fixture=refusal');
  await refusal.locator('#tactic').click();
  await refusal.locator('#new-mentality').selectOption('5');
  await refusal.locator('#submit-change').click();
  await refusal.clock.runFor(1000);
  await has(refusal, 'Refused'); await has(refusal, 'Current Mentality: Balanced');
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
  record('end race', 'Normal MM→TS→MV journey reached minute 89; paused request stayed Pending; resume/next live tick reached the whistle before application. Not applied, Balanced retained, report focused.');
  await pending.close();
  const waiting = await open('?state=MV-0&fixture=waiting');
  await waiting.clock.runFor(10000);
  await has(waiting, 'waiting for the first frame');
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
(async () => {
  fs.mkdirSync(evidence, { recursive: true });
  browser = await chromium.launch({ executablePath: process.env.UX_BROWSER, args: ['--no-sandbox'] });
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
  await fixtures();
  const files = ['index.html', 'prototype.css', 'model.js', 'prototype.js', 'reference-data.js', 'scenario-data.js', 'verify.cjs'];
  const hashes = Object.fromEntries(files.map(file => [file, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, file))).digest('hex')]));
  fs.writeFileSync(path.join(evidence, 'walkthrough.json'), JSON.stringify({ created: '2026-09-30', purpose: 'Executed S0 scripted resilience evidence; not participant results', runId: 'UX-GE-S0-20260930-02', supersedes: 'UX-GE-S0-20260930-01: Gate-E pass withdrawn after reproduced focus/disclosure Majors; 71 checks were insufficient', journey: 'S0', prototype: 's0-prototype v0.2', textScaleMethod: 'scale=2 doubles root font size from 16px to 32px; browser zoom remains 100%', date: '2026-09-30 (UTC)', runner: 'Codex scripted/self-walkthrough', browser: browser.version(), hashes, results }, null, 2) + '\n');
  console.log(`PASS: ${results.length} recorded checks; seven wireframe PDFs plus two resilience PDFs.`);
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(async () => { if (browser) await browser.close(); });
