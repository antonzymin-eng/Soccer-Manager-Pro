import csv, glob, sys, collections
rows = []
for f in sorted(glob.glob(sys.argv[1])):
    rows += list(csv.DictReader(open(f)))
num = lambda r, k: float(r[k])
by = collections.defaultdict(dict)  # (seed,case) -> arm -> row
for r in rows:
    by[(r['seed'], int(r['case']))][r['arm']] = r
seeds = sorted({k[0] for k in by})
cases = sorted({k[1] for k in by})
print(f"rows={len(rows)} seeds={len(seeds)} cases={cases}")
bad = [r for r in rows if int(r['nonFinite']) != 0]
print("hard stop nonFinite rows:", len(bad))

def agg(arm, case, key):
    return sum(num(by[(s, case)][arm], key) for s in seeds if arm in by[(s, case)])

keys = ['goals0','goals1','shots0','shots1','passes0','passes1','poss0','poss1','tackleWon','tackleLoose','tackleFoul','tackleMissed','yellows','sentOff','markAgentStrides','groundedAgentTicks','guardLockEntries','wallSeconds']
print("\n== Per case, sum over seeds: off -> on ==")
print("case | " + " | ".join(keys))
for c in cases:
    print(f"C{c} | " + " | ".join(f"{agg('off',c,k):.0f}->{agg('on',c,k):.0f}" for k in keys))

print("\n== Totals over all cases and seeds (per match means) ==")
n = sum(1 for k in by if 'off' in by[k] and 'on' in by[k])
for k in keys:
    off = sum(agg('off', c, k) for c in cases); on = sum(agg('on', c, k) for c in cases)
    print(f"{k:20s} off {off/n:10.2f}  on {on/n:10.2f}  delta {100*(on-off)/off if off else float('nan'):+6.1f}%")

print("\n== #434 sensitivity: changed team's aggregate goals (6 seeds), case vs C0 base, per arm ==")
desc = {1:('home F433',0),2:('away F433',1),3:('home F4231',0),4:('away F4231',1),5:('home all-Defend',0),6:('away all-Defend',1),7:('home all-Attack',0),8:('away all-Attack',1),9:('both F4231',None)}
for c in cases:
    if c == 0: continue
    name, t = desc[c]
    for arm in ('off','on'):
        if t is None:
            b = agg(arm,0,'goals0')+agg(arm,0,'goals1'); v = agg(arm,c,'goals0')+agg(arm,c,'goals1')
        else:
            b = agg(arm,0,f'goals{t}'); v = agg(arm,c,f'goals{t}')
        ob = agg(arm,0,f'goals{1-t}') if t is not None else 0; ov = agg(arm,c,f'goals{1-t}') if t is not None else 0
        print(f"C{c} {name:16s} {arm:3s}: changed team goals {b:.0f} -> {v:.0f} (x{v/b if b else float('nan'):.2f}); opponent {ob:.0f} -> {ov:.0f}")

print("\n== Max goals by one team in one match, per arm ==")
for arm in ('off','on'):
    m = max(max(num(by[k][arm],'goals0'), num(by[k][arm],'goals1')) for k in by if arm in by[k])
    print(arm, m)

print("\n== Per-seed C0 scorelines off | on ==")
for s in seeds:
    o = by[(s,0)].get('off'); a = by[(s,0)].get('on')
    print(s, f"{o['goals0']}-{o['goals1']}" if o else '-', '|', f"{a['goals0']}-{a['goals1']}" if a else '-')
