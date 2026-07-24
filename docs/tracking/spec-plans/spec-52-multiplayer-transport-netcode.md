# Spec #52 — Multiplayer Transport & Deterministic Netcode — High-Level Plan

> **Created:** July 24, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#52** (proposed in `docs/planning/master-plan-amendment-01-audio-multiplayer-transport.md` §3, not reserved). **Stage-6 gated:** the design supplement is deliberately NOT authored before the Stage-5 Fixed64 migration (#9) — see §9 first risk; only the §5 guardrails bind before then.
> **Master-plan home:** §5 Stage 6 ("Deterministic netcode" bullet, owned by Amendment 01 §3) · **Tier:** S6 · **Wave:** 9 (post-roadmap) · **FR prefix (proposed):** FR-NET
> **Determinism:** transport — none (no RNG stream, no domain tag; the sim's existing streams are the only randomness, replicated implicitly by seed + intents)
> **Purpose:** Head-to-head online play as lockstep intent-replication over the unmodified deterministic sim — both peers run the full engine; only manager intents cross the wire.

## 1. Scope
The online transport layer: session establishment (connect, rejoin, NAT traversal/relay), the lockstep intent-exchange protocol (tick-stamped manager intents, input-delay/tick-window scheduling), desync detection via the existing per-tick chained snapshot digest, resync via the existing `MatchSaveManager.Encode`/`Restore` machinery, and join-time compatibility gating via `EnvironmentFingerprint` (+ the MXCSR live-mode gate). **Out of scope:** matchmaking, leaderboards, and competitive seasons (#30/#43 + platform services via #39); chat/social; the sim itself (unmodified by design); and **host-authoritative state replication — forbidden**, per Amendment 01 §3.2: multiplayer is intent replication over an unmodified deterministic sim, never state push.

## 2. Staging (minimal-first → deep)
Two modes, in order: **Mode A** — same-platform lockstep (both peers on the pinned cert tuple, fingerprint-match as the join gate) MAY precede Fixed64; **Mode B** — cross-platform lockstep, gated on the Stage-5 Fixed64 migration (#9) delivering cross-platform bit-exactness. Minimal deliverable = a 1v1 head-to-head single match (two humans, one fixture). Deep tier = competitive seasons/leagues riding the same transport (season structure owned by #30/#43, not here). The single-player game is byte-identical with the transport assembly absent — nothing sim-side references it.

## 3. Dependencies
- **Upstream (needs):** #9 Fixed64 at Stage 5 (Mode B); #16's fingerprint/digest machinery (exists); `MatchSaveManager.Encode`/`Restore` (exists); the public intent seams (exist) — `SetTeamTactic`/`SetPlayerTactic` stage pending and commit at the AI-stride boundary (FR-TI-027), while `SubstitutePlayer` **applies the roster swap immediately** (only its notification event queues to the next Resolve phase) — which is exactly why remote intents MUST enter through the tick-scheduled command layer at an agreed tick, never as direct seam calls: `match-client-core`'s `ManagerCommandQueue` (drained at the top of a sim tick) + `TickStampedCommand` (exists); #50 save migration (protocol/replay version discipline); #39 platform services (relay/NAT, session infrastructure).
- **Downstream (consumers):** Stage-6 competitive features (#30/#43 season structures online); nothing sim-side.

## 4. Persistent state & save impact
No sim-side persistent state. A protocol version constant (fail-loud on mismatch, the `MATCH_SAVE_FORMAT_VERSION` posture). Match replays become **seed + intent log** — a new durable artifact class, versioned under #50's migration discipline; it must round-trip through the same restore machinery a save does. No change to any existing save format.

## 5. Determinism
The transport owns no RNG stream and no domain tag: both peers run the full sim from the same seed, so the sim's existing streams are the only randomness and they replicate for free. The per-tick digest chain is the desync oracle; divergence is detected by digest comparison, never by state comparison. **Guardrails binding NOW** (Amendment 01 §3.3, restated as standing MUSTs): no wall-clock or network timing ever enters game logic; no new mutation surface may bypass the public intent seams, and remote intents apply only through a tick-scheduled command layer at an agreed tick; the digest chain stays per-tick. These cost nothing today and are what keeps this spec authorable later.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1:** input-delay model — fixed input delay vs. rollback. Rollback multiplies per-tick sim cost (the certified p50 budget is per single forward tick); a fixed-delay window is the presumptive answer, but the supplement must cost both against the 60 Hz loop.
- **KD-2:** desync recovery policy — automatic mid-match resync via snapshot transfer (`Encode`/`Restore`), vs. fail-loud abort with both digest chains as evidence; and who is authoritative for the resync snapshot in a peer-to-peer topology.
- **KD-3:** topology and relay — pure P2P vs. relay-assisted; reuse of #39's platform networking (e.g. Steam Datagram Relay) vs. owned infrastructure.
- **KD-4:** hidden information under lockstep — both peers compute everything, so opponent tactics/attributes exist in local memory by construction; scope what "anti-cheat" can honestly mean here (integrity of intents, not secrecy of state) and say so explicitly.
- **KD-5:** protocol/replay versioning boundary with #50 — one shared migration discipline or a transport-owned parallel one (presumption: shared; a parallel version ledger is the FR-FN-015 forbidden-parallel-total class).
- **KD-6:** the season-level seam shape — what the transport must expose so #30/#43 competitive seasons can ride it later without a transport rewrite (named seam, no phantom interface).

## 7. Primary surfaces (proposed)
- A transport session API (proposed) — connect/rejoin/teardown, fingerprint join gate.
- An intent-replication codec (proposed) over `TickStampedCommand` — the only data that crosses the wire in steady state.
- A desync monitor (proposed) — per-tick digest-chain exchange/comparison.
- A resync service (proposed) — snapshot transfer over `MatchSaveManager.Encode`/`Restore`.

## 8. Test focus
A two-engine in-process lockstep harness (no real network — the transport faked at the seam): same seed + same tick-stamped intents ⇒ byte-identical digest chains; an injected divergent intent is detected within one digest exchange; mid-match resync round-trips byte-identically (the G3 save@N contract, transported); simulated latency/jitter/reorder never perturbs sim output (only delivery timing); protocol-version mismatch fails loud at session establishment. The §5 guardrails are standing coding rules today, not automated locks — the supplement should add lock tests for them (no-wall-clock-in-game-logic sweep; intent seams as the only mutation path).

## 9. Open questions / risks
- **Phantom-interface hazard (the reason this plan stops here):** authoring the transport interface before Stage-5 cross-platform determinism exists would specify against a second side that doesn't exist — the exact ERR-001/ERR-004/FR-LW-031 class. The supplement waits for Stage 5; this plan + the §5 guardrails are the only pre-Stage-5 artifacts.
- Fixed64 is the hard gate for Mode B and is itself a whole-engine migration risk (#9 §8.1) — transport scope must not silently absorb it.
- Rollback infeasibility (KD-1) at real tick cost may force fixed-delay UX trade-offs (input latency at high ping).
- Lockstep's inherent information exposure (KD-4) — over-promising anti-cheat would be dishonest; under-scoping it invites trivial memory-reading exploits in competitive play.
- NAT/relay infrastructure is an operating cost, not just code — a #39/business decision the spec can only interface with.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 24, 2026 | Initial high-level plan, per Master Plan Amendment 01 §3. Stage-6 gate + pre-Stage-5 guardrails recorded. |
| v0.2 | July 24, 2026 | AR-1 fixes: §3 seam-commit contract corrected against source (`SubstitutePlayer` applies immediately — only its notification event queues; Set\*Tactic are the stride-committed pair) and the tick-scheduled-command-layer requirement made explicit here and in the §5 guardrails; §8 no longer overstates the guardrails as existing automated locks. |
