# Master Plan Amendment 01 — Audio & Sound Design; Multiplayer Transport & Deterministic Netcode

> **Created:** July 24, 2026
> **Status:** AMENDMENT (planning-level; amends `master-development-plan.md` v1.0)
> **Purpose:** The July 2026 master-plan feature-coverage review found exactly two feature areas
> named in the master plan but covered by no feature definition, no staging, and no spec mapping
> anywhere in the plan, the spec registry, or the management-layer spec roadmap: **audio/sound
> design** and the **Stage-6 multiplayer transport layer**. This amendment gives both a proper
> planning-level definition and assigns candidate spec numbers, so neither remains an
> unaccounted-for gap. It changes no spec, no code, and no registry status.

---

## 1. Gap statement (what the master plan says today)

**Audio** appears in the base plan only as scattered fragments:

- §3 Stage 1, Month 11–12 "UI & Polish" breakdown item: "Sound effects (crowd, whistle, ball
  kick)" (the month-by-month list under §3; NOT §3.4, which is "User Interface (Basic)").
- §7 Stage-1 specification list, item 29: "Sound Design (effects, music)" — listed as a required
  document, but never scoped, and absent from the #27–#50 management-layer roadmap numbering.
- §10 budget line: "Sound effects/music: $1,000".

**Multiplayer transport** appears only as Stage-6 feature bullets ("Head-to-head matches online",
"Cross-platform play", "Deterministic netcode (built on the Stage 5 Fixed64 migration)") with no
architecture, dependency, or spec mapping. The `management-layer-spec-roadmap.md` candidate set
(#27–#50) deliberately swept the master plan for spec gaps in its v0.2 review and covers
everything else; these two areas are the remainder.

---

## 2. Amendment A — Audio & Sound Design

### 2.1 Feature definition

Two distinct tiers, which the base plan conflated into one polish bullet:

1. **Match audio** — crowd model (ambient loop + reactive intensity), on-pitch effects (ball
   contact, whistle/officiating cues), goal/near-miss stingers, and (deep tier) commentary audio
   delivery. All of it derives from the match-engine **observation surface and event ledger,
   read-only** — the same observer-neutral contract `match-viewer` and candidate #48 already
   honour. Reactive crowd intensity is a pure function of observed state (score, ball position,
   ledger events), never a new sim input.
2. **Game-wide audio framework** — mixer/bus architecture (music / SFX / crowd / commentary /
   UI), music playback, UI/menu audio, per-channel client-local settings, ducking rules, and the
   audio-accessibility hooks (visual/subtitle equivalents for audio cues) that route through
   #49's accessibility content tier at Wave 8.

### 2.2 Staging

| Stage | Deliverable |
|-------|-------------|
| Stage 1 (Tactical Demo) | Minimal match audio: basic SFX + crowd loop over the live viewer — the base plan's existing Month-11–12 "UI & Polish" item, now formally owned. |
| Stage 2 (V1 release) | Full framework: mixer, music, UI audio, settings, complete match soundscape. |
| Stage 3+ | Commentary audio + presentation-depth integration alongside #48's 3D/animation tier. |

### 2.3 Determinism & architecture posture

Presentation-only. No RNG stream, no domain tag, no serialized state (the `match-viewer` /
#37 / #38 precedent). Audio settings are client-local and outside the determinism save. A match
rendered with full audio must be byte-identical to an unobserved same-seed run (the
`MatchViewerTests` digest-lock class extends to audio). Any procedural variation in audio cue
selection uses display-side, non-determinism-pinned randomness — never a `deterministic-sim`
stream. The actual playback binding is **Unity-host-gated**, like #38's rendering binding; the
trigger-mapping contract is authorable host-free.

### 2.4 Spec mapping

- **Match-audio slice → candidate #48 (Match Presentation Depth)** — already in that plan's
  scope ("audio (crowd, effects)", KD-4 read-only ledger triggering). No change needed there.
- **Game-wide audio framework → NEW candidate #51 "Audio & Sound Design"** — realizes the base
  plan's §7 item-29 "Sound Design" document inside the #27+ numbering scheme. Wave placement:
  Wave 8 — one wave after #48's Wave-7 match-audio slice; FR prefix proposed `FR-AU`;
  determinism: presentation — no domain tag/ordinal (the #37/#38/#49 class).
- Audio-accessibility content → #49 Wave-8 content tier (unchanged owner).

---

## 3. Amendment B — Multiplayer Transport & Deterministic Netcode (Stage 6)

### 3.1 Feature definition

The Stage-6 online layer, split into what the transport spec owns and what it does not:

- **Owns:** session establishment (connect, rejoin, NAT traversal/relay), the **lockstep
  intent-exchange protocol** (both clients run the full deterministic sim; only manager intents
  cross the wire), input-delay/tick-window scheduling, **desync detection** via the existing
  per-tick snapshot digest chain, and **resync** via the existing snapshot restore machinery
  (`MatchSaveManager.Encode`/`Restore`).
- **Does not own:** matchmaking/leaderboards/competitive seasons (platform-service + #30/#43
  concerns), chat/social, and the sim itself.

### 3.2 Architecture decision recorded now

The engine is already shaped for lockstep, and this amendment pins that as the intended model:

- All match mutation flows through a small set of public intent seams — `SetTeamTactic` /
  `SetPlayerTactic` (staged pending, committed at the AI-stride boundary per FR-TI-027) and
  `SubstitutePlayer` (**applies the roster swap immediately**, with only the notification event
  queued to the next Resolve phase). Because the seams' apply-timing differs, networked play MUST
  enter every remote intent through a tick-scheduled command layer that applies it at an agreed
  tick — never by direct seam calls at unagreed local ticks — and `match-client-core`'s
  `ManagerCommandQueue` (drained at the top of a sim tick) + `TickStampedCommand` is already that
  pattern.
- The per-tick chained snapshot digest is the desync detector; `EnvironmentFingerprint` +
  the MXCSR gate are the join-time compatibility check; snapshot save/restore is the resync path.
- Therefore multiplayer is **intent replication over an unmodified deterministic sim** — no
  host-authoritative state replication, ever.

### 3.3 Dependencies and gates

1. **Cross-platform bit-exactness — Stage 5 Fixed64 migration (#9)** for cross-platform play.
   A same-platform (pinned-tuple) lockstep mode MAY precede Fixed64, gated on the existing
   fingerprint match instead; the spec must scope both.
2. #16 determinism certification machinery (exists); #50 save migration (versioned protocol /
   replay compatibility); #30/#43 for competitive seasons; #39 for platform services.
3. Standing guardrails already in force that the transport depends on (and which this amendment
   makes explicit as MUSTs to preserve): no wall-clock or network timing in game logic; no new
   mutation surface that bypasses the public intent seams, and remote intents apply only through
   a tick-scheduled command layer at an agreed tick (§3.2); digest chain stays per-tick.

### 3.4 Spec mapping

- **NEW candidate #52 "Multiplayer Transport & Deterministic Netcode"** — Stage 6; FR prefix
  proposed `FR-NET`. **Deliberately NOT pulled forward** (unlike #27–#30): authoring the
  transport interface before Stage 5 cross-platform determinism exists would create exactly the
  phantom-interface class the Interface Design Principle and FR-LW-031 forbid — there is no
  second side to specify against until then. Only the §3.3 guardrails bind before Stage 5.

---

## 4. Base-plan deltas

The base `master-development-plan.md` stays v1.0 with historical text verbatim; it gains only a
header pointer to this amendment. Interpretive deltas this amendment establishes:

1. §3's Month-11–12 "UI & Polish" Sound-effects bullet and §7 item 29 "Sound Design" are owned
   by §2 above (candidates #48 + #51).
2. Stage 6's "Deterministic netcode" bullet is owned by §3 above (candidate #52), with the
   lockstep intent-replication model pinned as the intended architecture.

---

## 5. Registry impact

**None now.** #51/#52 are **proposed, not reserved** — per the `spec-plans/README.md` precedent,
`SPEC_INDEX.md` rows land only at design-supplement promotion. Next steps in the established
pipeline: author `spec-plans/spec-51-audio-sound-design.md` and
`spec-plans/spec-52-multiplayer-transport-netcode.md` one-page plans (template §1–§9), then
design supplements at their wave (#51 ~Wave 8; #52 not before Stage 5).

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| v0.1 | July 24, 2026 | Initial amendment: audio & sound design (§2, candidates #48 slice + new #51) and multiplayer transport / deterministic netcode (§3, new candidate #52, lockstep intent-replication model pinned, Stage-6 no-pull-forward decision recorded). |
| v0.2 | July 24, 2026 | AR-1 fixes (0H+5M+3L across the amendment + #51/#52 plans + README/roadmap): §3.2/§3.3 seam-commit contract corrected against source — `SubstitutePlayer` applies immediately (`MatchEngine.cs` pending-event queue holds only the notification event), Set\*Tactic are the stride-committed pair, and the tick-scheduled-command-layer requirement is now an explicit guardrail; §2.4 #48 wave corrected Wave 8 → Wave 7 ("one wave after"); the "Sound effects" bullet anchor corrected §3.4 → §3 Month-11–12 "UI & Polish" here and in the base-plan header pointer / spec-51 plan. |
