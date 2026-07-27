# Audio & Sound Design #51 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

**Nothing below draws from a `deterministic-sim` stream** (FR-AU-033), and nothing below reads sim state
(FR-AU-015/034/035). Those two absences are what make observer neutrality **unconditional** (KD-6) rather
than a property that has to be argued per feature.

## 3.1 `Resolve` — the shell-owned join (FM-AU-01)

**This function is not in #51.** It lives in the composition root, and that placement is the entire
architectural content of KD-1.

```
# in the SHELL — the only layer that sees both id spaces
sealed class CueSinkAdapter : ICueSink                     # #48 DECLARES; the shell IMPLEMENTS
{
    void Emit(TacticalDirector.MatchPresentation.CueId cue,
              in TacticalDirector.MatchPresentation.CueParams p)      # FULLY QUALIFIED -- see §4.2
    {
        if (!map.TryGetValue(cue, out CueKey key))  return;           # F1: SILENT no-op at run time
        audio.Play(key, Translate(p));                                # #51's CueParams, not #48's
    }
}
```

**The mapping is a table, not code** (FR-AU-004). `CueId → CueKey` is data in the root, exactly as the
root already owns #49's boundary adapters and #50's generator registry. Neither spec's assembly learns the
other's type.

**An unmapped id is silent at run time and fatal at build time** (FR-AU-005 / F1), and both halves are
load-bearing. A shipped game must not crash over a missing sound; a missing sound must not ship unnoticed.
A design with only the first is negligent and a design with only the second is dangerous — which is why §5
asserts them as two separate tests rather than one.

**The completeness check belongs here, not in #51** (§5.1). #51 can prove only that its own catalogue is
internally coherent; **only the mapping knows whether every `CueId` #48 can emit resolves.** That is the
honest placement even though it is the less convenient one, and it is the direct consequence of splitting
the id spaces.

## 3.2 `Play` — routing and variation (FM-AU-02)

```
Play(in CueKey key, in CueParams p):
    entry := catalogue.Require(key)                      # F1 at the catalogue's own boundary
    bus   := entry.Bus                                   # exactly one, from a CLOSED enum (FR-AU-011)
    gain  := settings.Master.Gain * settings[bus].Gain * DuckGain(bus)
    if (settings.Master.Muted || settings[bus].Muted)  gain := 0

    asset := entry.Asset.VariantCount > 1
           ? entry.Asset.Variant(DisplayRandom.Next(entry.Asset.VariantCount))   # DISPLAY-SIDE ONLY
           : entry.Asset.Single

    host.Play(asset, bus, gain, p)                        # the Unity-gated half (KD-5)
    if (entry.Caption.HasCaption)  captions.Show(entry.Caption.Caption)   # #49 renders (KD-4)
```

**`DisplayRandom` is not a `deterministic-sim` stream, and this is the one determinism rule an audio
framework can plausibly break** (FR-AU-033 / F7). Alternating footfall samples is a natural thing to want
and a `DeterministicRngService` is a natural thing to reach for — but a draw **advances a cursor that is
serialized state**, so *what the player hears* would change *what is saved*. Display-side randomness has
no cursor and touches nothing.

**Routing cannot fail** (FR-AU-012). Because the bus set is a **closed enum**, `entry.Bus` is a member by
construction — *"a cue routed to a bus that does not exist"* is not a representable state, which is what
the fixed-over-data-driven decision bought.

**The caption is emitted as an identity, never as text** (FR-AU-028). `captions.Show` hands a #51-owned
`CaptionId` to #49's surface; #51 holds no localization type and formats no string.

## 3.3 `DuckGain` — bus activity, never a game event (FM-AU-03)

```
DuckGain(AudioBus target) -> gain:
    g := 1.0
    foreach row in duckingTable where row.Ducked == target:
        active := busActivity[row.Trigger]                  # BUS ACTIVITY -- not "a goal was scored"
        g := g * Envelope(active, row.AttenuationPerMille, row.AttackMs, row.ReleaseMs)
    return g
```

**The trigger is `busActivity`, and that single choice is what keeps audio out of the simulation's
dependency graph** (FR-AU-014). The alternative phrasing — *duck the crowd when a goal is scored* — reads
identically to a designer and is architecturally different: it requires #51 to know what a goal is, which
requires it to read sim state, which is FR-AU-015. **"The commentary bus is sounding" carries the same
mix intent with none of the coupling.**

**A mix state may still be selected from presentation context** (FR-AU-016). Menu vs. match vs. paused is
#38's navigation state, and music that never changes between the menu and a match is not something anyone
ships. The line is **who owns the value**, not whether any state is read at all — and the over-broad rule
("never read state") is worth avoiding precisely because it is unenforceable and gets routed around, often
via a sim read.

**The table is validated before use** (FR-AU-017 / F5): no row names the same bus as trigger and target,
and no cycle can sustain indefinite attenuation. A mix that ducks itself into silence produces no error
and offers no recovery, and the table is exactly the artifact a content author edits without a compiler.

## 3.4 Settings apply and the reset policy (FM-AU-04)

```
ApplySettings(fragment):
    if (!TryReadFragment(out AudioSettingsFragment f))
        f := AudioSettingsFragment.Defaults        # RESET AND CONTINUE, silently (FR-AU-020)
    foreach bus: settings[bus] := Clamp(f[bus])    # a partially-invalid field takes its default
    settings.Master := Clamp(f.Master)
```

**This is deliberately the opposite of #50's policy, and the contrast is the design.** #50 refuses a save
it cannot classify because a career is irreplaceable; a volume slider is not. Applying save-grade refusal
to preferences would let a **corrupt settings byte block launch** — a mismatch of policy to stakes, and
one of the easier ways to turn a trivial defect into an unlaunchable build.

**A partially-invalid fragment resets only the invalid fields**, not the whole file: an out-of-range
`Crowd` volume must not silently discard the player's `Music` setting.

**#51 owns no file, no path and no serializer** (FR-AU-018). `TryReadFragment` is the client-settings
store's, and if ERR-038-004 is declined the fallback is **in-memory with persistence deferred**
(FR-AU-022) — never a private file, because a sixth store cannot be undone once shipped (F8).

## 3.5 Arithmetic convention

Gain composition is per-mille integer arithmetic multiplied into a float at the host boundary, and
**nothing here feeds a digest or a save** (FR-AU-037), so no rounding convention is pinned and none is
needed.

**The rule that matters instead is a layering one:** no #51 value may ever flow **into** the simulation
(FR-AU-034/035), and no sim value may be read to compute one (FR-AU-015). §5.6 asserts both as structural
properties rather than numeric ones.

## 3.6 Worked examples (hand-verifiable)

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | **#51 absent** | #48's no-op sink | **silence** — literally today's build, not an emulation (FR-AU-038) |
| (b) | S2, every bus at unity gain, no ducking triggered | `gain = 1 × 1 × 1` | the mix **equals the unrouted sum** — enabling the framework changes routing, not sound |
| (c) | A full-audio match run vs. an unobserved same-seed run | no stream, no sim read | **byte-identical digest chain** (FR-AU-036) — unconditional, not flag-conditioned |
| (d) | #48 emits a `CueId` with a mapping | shell resolves | the mapped `CueKey` plays on its catalogue bus |
| (e) | #48 emits a `CueId` with **no** mapping | `TryGetValue` fails | **silent no-op** at run time — and the **build** already failed (F1) |
| (f) | A cue on a muted bus | `muted ⇒ gain = 0` | inaudible; **no error** — muting is a setting, not a fault |
| (g) | Commentary bus sounding, a row ducking `Crowd` by 400‰ | `Envelope` over attack | `Crowd` attenuated while commentary sounds, released after |
| (h) | A ducking row with `Trigger == Ducked` | table validation | **refused** (F5) — a bus that ducks itself has no recovery |
| (i) | A catalogue entry authored with no caption decision | ctor refuses | **cannot be constructed** (F2) — the rule that cannot drift |
| (j) | An ambience loop | `NoCaption` + justification | **valid** — the obligation is that the decision was *made* (FR-AU-026) |
| (k) | `NoCaption` with no justification | registration gate | **refused** (F3) — otherwise the rule is satisfiable by reflex |
| (l) | A corrupt settings fragment | `TryReadFragment` fails | **defaults, silently, launch continues** (F4) — the deliberate inverse of #50 |
| (m) | One out-of-range bus volume | per-field clamp | that bus defaults; **every other setting is preserved** |
| (n) | A footfall cue with 4 variants | `DisplayRandom` | a varied sample, **no cursor advanced**, nothing saved changes (F7) |
| (o) | Music differing between menu and match | #38 navigation state | **permitted** (FR-AU-016) — presentation context, not sim state |

Examples (c), (e) and (n) are the three that matter most: (c) is the spec's headline claim, (e) is the
pair of behaviours that must both hold, and (n) is the one plausible way an audio framework breaks
determinism.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-AU-01..04: the shell-owned resolve — placed outside #51 deliberately, since the completeness check can only live where both id spaces are visible; playback routing with display-side variation and the argument for why a `deterministic-sim` draw would make what is heard change what is saved; ducking on **bus activity** with the argument that the game-event phrasing reads identically to a designer and is architecturally different; the settings reset policy as the deliberate inverse of #50's refusal. Fifteen worked examples). Status IN REVIEW. |
#endregion
