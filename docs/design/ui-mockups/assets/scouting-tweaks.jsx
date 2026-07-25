/* assets/scouting-tweaks.jsx — Tweaks panel for Scouting Screen */

function ScoutingTweaks() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);

  /* density → CSS vars */
  React.useEffect(() => {
    const map = {
      compact:     { h: '27px', fs: '11px'   },
      comfortable: { h: '33px', fs: '12px'   },
      relaxed:     { h: '41px', fs: '12.5px' },
    };
    const d = map[t.density] || map.comfortable;
    document.documentElement.style.setProperty('--sc-row-h', d.h);
  }, [t.density]);

  /* accent color → CSS vars */
  React.useEffect(() => {
    const c = t.accentColor;
    document.documentElement.style.setProperty('--brand-400', c);
    const r = parseInt(c.slice(1,3), 16);
    const g = parseInt(c.slice(3,5), 16);
    const b = parseInt(c.slice(5,7), 16);
    document.documentElement.style.setProperty('--brand-tint',        `rgba(${r},${g},${b},.10)`);
    document.documentElement.style.setProperty('--brand-tint-strong', `rgba(${r},${g},${b},.18)`);
    document.documentElement.style.setProperty('--brand-glow',        `0 0 24px rgba(${r},${g},${b},.35)`);
    document.documentElement.style.setProperty('--brand-700',         c);
  }, [t.accentColor]);

  /* show/hide watching tier */
  React.useEffect(() => {
    const els = document.querySelectorAll('.tier-watching');
    els.forEach(el => { el.style.display = t.showWatching ? '' : 'none'; });
  }, [t.showWatching]);

  return (
    <TweaksPanel title="Tweaks">
      <TweakSection label="Table" />
      <TweakRadio
        label="Density"
        value={t.density}
        options={['compact', 'comfortable', 'relaxed']}
        onChange={v => setTweak('density', v)}
      />
      <TweakSection label="Style" />
      <TweakColor
        label="Accent"
        value={t.accentColor}
        options={['#00ff88', '#4aa8ff', '#ffc933', '#b066ff']}
        onChange={v => setTweak('accentColor', v)}
      />
      <TweakSection label="Filters" />
      <TweakToggle
        label="Show Watching Tier"
        sublabel="Players with &lt;50% report progress"
        value={!!t.showWatching}
        onChange={v => setTweak('showWatching', v)}
      />
    </TweaksPanel>
  );
}

const tweaksRoot = document.getElementById('tweaks-root');
if (tweaksRoot) {
  ReactDOM.createRoot(tweaksRoot).render(React.createElement(ScoutingTweaks));
}
