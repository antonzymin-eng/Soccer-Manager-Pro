/* assets/training-tweaks.jsx — Tweaks panel for Training Screen */

function TrainingTweaks() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);

  /* density → CSS vars */
  React.useEffect(() => {
    const map = {
      compact:     { h: '24px', fs: '11px'   },
      comfortable: { h: '30px', fs: '12px'   },
      relaxed:     { h: '38px', fs: '12.5px' },
    };
    const d = map[t.density] || map.comfortable;
    document.documentElement.style.setProperty('--row-h',  d.h);
    document.documentElement.style.setProperty('--row-fs', d.fs);
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

  /* risk panel visibility */
  React.useEffect(() => {
    const els = document.querySelectorAll('.ts-section:last-child');
    els.forEach(el => { el.style.display = t.showRiskPanel ? '' : 'none'; });
  }, [t.showRiskPanel]);

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
      <TweakSection label="Panels" />
      <TweakToggle
        label="Injury Risk Panel"
        sublabel="Show/hide the risk alerts section"
        value={!!t.showRiskPanel}
        onChange={v => setTweak('showRiskPanel', v)}
      />
    </TweaksPanel>
  );
}

const tweaksRoot = document.getElementById('tweaks-root');
if (tweaksRoot) {
  ReactDOM.createRoot(tweaksRoot).render(React.createElement(TrainingTweaks));
}
