/* assets/squad-tweaks.jsx — Tweaks panel for Squad Screen */

function SquadTweaks() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);

  /* ── apply density → CSS vars ── */
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

  /* ── apply accent color → CSS vars ── */
  React.useEffect(() => {
    const c = t.accentColor;
    document.documentElement.style.setProperty('--brand-400', c);
    const r = parseInt(c.slice(1,3),16), g = parseInt(c.slice(3,5),16), b = parseInt(c.slice(5,7),16);
    document.documentElement.style.setProperty('--brand-tint', 'rgba('+r+','+g+','+b+',.10)');
    document.documentElement.style.setProperty('--brand-tint-strong', 'rgba('+r+','+g+','+b+',.18)');
    document.documentElement.style.setProperty('--brand-glow', '0 0 24px rgba('+r+','+g+','+b+',.35)');
  }, [t.accentColor]);

  /* ── apply in-game editor → html class ── */
  React.useEffect(() => {
    document.documentElement.classList.toggle('in-game-editor', !!t.inGameEditor);
  }, [t.inGameEditor]);

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
      <TweakSection label="Editor" />
      <TweakToggle
        label="In-Game Editor"
        sublabel="Shows CA circle + hidden attributes"
        value={!!t.inGameEditor}
        onChange={v => setTweak('inGameEditor', v)}
      />
    </TweaksPanel>
  );
}

const tweaksMount = document.getElementById('tweaks-root');
if (tweaksMount) {
  ReactDOM.createRoot(tweaksMount).render(React.createElement(SquadTweaks));
}
