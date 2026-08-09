// File:     src/testing-strategy/Tests/CrossSpecMechanicsScenarios.cs
// Created:  2026-07-04
// Modified: 2026-07-04
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.1 / §3.3.5 / Appendix A.1 / KD-8,
//           Positioning AI #12 §3.13 / §4.3, Pressing AI #13 §3.13 / §4.2,
//           Code Standards #20
// Purpose:  Cross-spec Mechanics-layer closed-loop scenario corpus (owned by Spec #19
//           per KD-8; manifest paths live under SCENARIO_PATH_CROSS_SPEC_PREFIX and
//           declare ≥ 2 owning specs per A.1). Chain: the REAL PositioningAITick (#12)
//           10 Hz orchestrator is driven live, and its per-tick formation-slot / line /
//           phase output is read into a REAL PressingAITick (#13) snapshot each tick —
//           the exact FillPositioningSnapshot → Tick → FillPressingSnapshot → Tick
//           handoff MatchEngine performs. This is the composition surface the two
//           per-spec suites never run together: the #12 corpus ticks positioning
//           against a static input, and the #13 corpus feeds pressing a STATIC
//           positioning state (SnapshotFactory baseline slots, never ticked). Here
//           both real orchestrators run in sequence with #12's live output wired into
//           #13's input each tick, and the composed result is checked for structural
//           well-formedness + two-instance determinism.
//
//           SCOPE NOTE: the handoff is exercised by construction (the Step() copy of
//           #12's slots/lines into the pressing snapshot), NOT asserted by a predicate
//           — the envelope checks below would still pass if that copy were removed and
//           pressing read only its static baseline. The value here is that the whole
//           chain executes end-to-end deterministically, not a consumption proof.

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.TestingStrategy.Tests
{
    /// <summary>
    /// Builds the cross-spec Mechanics scenario index (#19 §3.3.5 cross-spec/ layout;
    /// KD-8 ownership). Manifest paths under <see cref="TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX"/>
    /// declare every spec whose behaviour the envelope asserts — here #12 (Positioning
    /// AI) and #13 (Pressing AI). Neither orchestrator draws per-tick randomness (both
    /// pipelines are deterministic by construction — no System.Random), so the recorded
    /// seeds are canonical identifiers per FR-TS-025, not behaviour inputs. Tier B.
    /// </summary>
    internal static class CrossSpecMechanicsScenarios
    {
        public const string PositioningFeedsPressingPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "positioning-feeds-pressing";

        public const string DeterministicPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "positioning-pressing-two-instance-determinism";

        public const ulong PositioningFeedsPressingSeed = 1UL;
        public const ulong DeterministicSeed            = 2UL;

        private const int MaxEntity = 21;   // SQUAD_SIZE − 1 (both teams; mirrors MatchEngine)
        private const int OwnSquad  = 11;   // PLAYERS_PER_TEAM
        private const int Ticks     = 20;   // advances the #12 phase-hysteresis + #13 debounce/role dwell
        private const int CarrierEntityId = 14; // an opposing outfielder holds the ball

        // The pressing team is team 0 (own squad = entity ids 0..10; opponents 11..21).
        private static readonly int[] OwnEntities = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(PositioningFeedsPressingPath, "positioning-feeds-pressing",
                    PositioningFeedsPressingSeed, PositioningFeedsPressing),
                Entry(DeterministicPath, "positioning-pressing-two-instance-determinism",
                    DeterministicSeed, TwoInstanceDeterminism),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 12, 13 },   // cross-spec: ≥ 2 owning specs (A.1 arity)
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        // ── one composed #12 → #13 chain (mirrors MatchEngine's per-team construction) ──────

        /// <summary>
        /// Holds one team's live Positioning + Pressing orchestrators plus their reused
        /// input snapshots. Ticking <see cref="Step"/> drives the full cross-spec handoff.
        /// </summary>
        private sealed class MechanicsChain
        {
            private readonly PositioningAITick _positioning;
            private readonly PressingAITick    _pressing;
            private readonly PositioningPerceptionSnapshot _posSnap;
            private readonly PressingSnapshot  _pressSnap;
            private readonly ContextModifierInputs _modifiers;

            public PressingAITick Pressing => _pressing;
            public PositioningAITick Positioning => _positioning;

            public MechanicsChain()
            {
                _positioning = new PositioningAITick(FormationFamily.F442, MaxEntity);
                _posSnap     = BuildPositioningSnapshot();
                _positioning.SeedFromFormation(_posSnap);

                var view = new PositioningAIView(_positioning);
                _pressing = new PressingAITick(view, new PassEventRing(8), MaxEntity);
                _pressSnap = BuildPressingSnapshot();

                // Balanced Stage-0 context: no score swing, rested squad, neutral intensity
                // (the ctor seeds the width scalars to Standard = identity).
                _modifiers = new ContextModifierInputs(
                    scoreDiff: 0, teamMeanFatigue: 0f, tacticalIntensity: 0.5f);
            }

            /// <summary>
            /// One 10 Hz tick: drive Positioning, then read its live formation slot + line
            /// (and team phase) into the Pressing snapshot before driving Pressing — the
            /// #12 → #13 handoff. Both snapshots carry the monotonically-increasing tick
            /// index the F1 stale-detection guard on each orchestrator requires.
            /// </summary>
            public void Step(int tick)
            {
                _posSnap.TickIndex = tick;
                _positioning.Tick(_posSnap, _modifiers);

                _pressSnap.TickIndex = tick;
                for (int i = 0; i < OwnSquad; i++)
                {
                    Vector2 slot = _positioning.GetFormationSlot(i);
                    // A sentinel slot (inactive agent — none here) must not become the press
                    // baseline; fall back to the agent's own position, mirroring MatchEngine.
                    _pressSnap.Agents[i].BaselineSlot = PositioningAITick.IsSentinelSlot(slot)
                        ? _pressSnap.Agents[i].Position
                        : slot;
                    _pressSnap.Agents[i].Line = _positioning.GetLine(i);
                }
                _pressing.Tick(_pressSnap);
            }
        }

        // ── initial world (team 0 pressing, out of possession; opponent carries the ball) ───

        private static PositioningPerceptionSnapshot BuildPositioningSnapshot()
        {
            var snap = new PositioningPerceptionSnapshot(OwnSquad);
            FormationSlotRecord[] formation =
                PositioningAIConstants.GetFormationSlots(FormationFamily.F442);

            snap.BallPosition = new Vector3(52f, 34f, 0.11f);
            snap.BallVxFiltered = 0f;
            snap.PossessionOwnerEntityId  = CarrierEntityId; // opponent holds the ball
            snap.PossessionOwnerIsOwnTeam = false;            // ⇒ out-of-possession shape
            snap.HasTeamPossession        = true;             // ERR-012-011 — a team IS in possession
            snap.TeamPossessionIsOwnTeam  = false;            //   ...and it is not this one

            int activeOutfield = 0;
            for (int k = 0; k < OwnSquad; k++)
            {
                bool isGk = k == 0;
                snap.Agents[k] = new AgentPositioningData(
                    entityId:     k,
                    slotIndex:    k,
                    position:     OwnPosition(k),
                    isActive:     true,
                    role:         formation[k].Role,
                    isGoalkeeper: isGk);
                if (!isGk) activeOutfield++;
            }
            snap.ActiveOutfieldCount = activeOutfield;
            return snap;
        }

        private static PressingSnapshot BuildPressingSnapshot()
        {
            var snap = new PressingSnapshot();
            snap.BallPosition        = new Vector3(52f, 34f, 0.11f);
            snap.BallVelocity        = Vector3.zero;
            snap.BallCarrierEntityId = CarrierEntityId;
            snap.AttackingDirection  = new Vector2(1f, 0f); // pressing team attacks +X
            snap.PossessionTeamId    = 1;                   // opponents hold the ball
            snap.PressingTeamId      = 0;

            for (int i = 0; i <= MaxEntity; i++)
            {
                bool isOwn = i < OwnSquad;
                snap.Agents[i] = new PressingAgentSnapshot
                {
                    EntityId            = i,
                    TeamId              = isOwn ? 0 : 1,
                    Position            = isOwn ? OwnPosition(i) : OpponentPosition(i),
                    Velocity            = Vector2.zero,
                    Facing              = new Vector2(1f, 0f),
                    Fatigue             = 0f,          // fully rested
                    FirstTouchAttribute = 10f,
                    LastTouchQuality    = 1f,          // perfect ⇒ no BadTouch trigger
                    PostTouchBallSpeed  = 0f,
                    IsGoalkeeper        = i == 0 || i == OwnSquad,
                    HasBall             = i == CarrierEntityId,
                    IsActive            = true,
                    // BaselineSlot + Line are overwritten each tick from the live #12 output.
                    BaselineSlot        = isOwn ? OwnPosition(i) : OpponentPosition(i),
                    Line                = LineId.Midfield,
                };
            }
            return snap;
        }

        // Team 0 (pressing) in a 4-4-2 block in its own half; GK at index 0.
        private static Vector2 OwnPosition(int k)
        {
            switch (k)
            {
                case 0:  return new Vector2(6f, 34f);   // GK
                case 1:  return new Vector2(22f, 12f);
                case 2:  return new Vector2(20f, 27f);
                case 3:  return new Vector2(20f, 41f);
                case 4:  return new Vector2(22f, 56f);
                case 5:  return new Vector2(36f, 14f);
                case 6:  return new Vector2(34f, 28f);
                case 7:  return new Vector2(34f, 40f);
                case 8:  return new Vector2(36f, 54f);
                case 9:  return new Vector2(50f, 28f);
                default: return new Vector2(50f, 40f);  // k == 10
            }
        }

        // Team 1 (opponents) around and ahead of the ball; GK at index 11, carrier at 14.
        private static Vector2 OpponentPosition(int i)
        {
            switch (i)
            {
                case 11: return new Vector2(100f, 34f); // GK
                case 12: return new Vector2(62f, 18f);
                case 13: return new Vector2(62f, 50f);
                case 14: return new Vector2(52f, 34f);  // carrier
                case 15: return new Vector2(45f, 22f);
                case 16: return new Vector2(45f, 46f);
                case 17: return new Vector2(30f, 20f);
                case 18: return new Vector2(30f, 48f);
                case 19: return new Vector2(70f, 30f);
                case 20: return new Vector2(70f, 38f);
                default: return new Vector2(85f, 34f);  // i == 21
            }
        }

        // FNV-1a-64 over the INTEGER content of BOTH subsystems' composed output — the #12
        // discrete decisions (per-agent line + team phase) and the #13 directive + per-agent
        // assignments. Float-free, so a pure end-to-end determinism signal for the chain (the
        // pressing assignments are a downstream function of the #12 slots via BaselineSlot, so
        // slot determinism is transitively locked without hashing raw float bits).
        private static ulong Digest(MechanicsChain chain)
        {
            PositioningAITick pos = chain.Positioning;
            PressDirective d = chain.Pressing.LastDirective;

            ulong h = 14695981039346656037UL;
            h = Mix(h, (int)pos.GetPhase());
            for (int k = 0; k < OwnEntities.Length; k++)
                h = Mix(h, (int)pos.GetLine(OwnEntities[k]));

            h = Mix(h, d.PrimaryPresserId);
            h = Mix(h, d.CoverShadowCount);
            h = Mix(h, (int)d.ActiveTriggers);
            h = Mix(h, d.Shadow0.DefenderId); h = Mix(h, d.Shadow0.ReceiverId);
            h = Mix(h, d.Shadow1.DefenderId); h = Mix(h, d.Shadow1.ReceiverId);
            for (int k = 0; k < OwnEntities.Length; k++)
            {
                PressAssignment a = chain.Pressing.GetAssignment(OwnEntities[k]);
                h = Mix(h, a.EntityId);
                h = Mix(h, (int)a.Role);
            }
            return h;
        }

        private static ulong Mix(ulong h, int value)
        {
            unchecked // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                h ^= (uint)value;
                h *= 1099511628211UL;
            }
            return h;
        }

        // ── scenarios ─────────────────────────────────────────────────────────────────────

        // Runs the composed #12 → #13 chain across 20 ticks and checks both halves stay
        // well-formed: (a) Positioning produces a finite, non-sentinel, on-pitch formation
        // slot for every own agent (the geometry Step() wires into Pressing's BaselineSlot);
        // (b) the Pressing directive is bounded; (c) every own pressing agent carries a
        // defined PressRole. Robust to behaviour — no exact positions. NOTE: these predicates
        // check each half independently; they do NOT assert that #13's output depends on the
        // #12 slots (see the file-header SCOPE NOTE) — the handoff is run, not proven.
        private static void PositioningFeedsPressing(ScenarioContext context)
        {
            var chain = new MechanicsChain();
            for (int t = 0; t < Ticks; t++)
                chain.Step(t);

            // #12 half: every own slot the handoff forwards is finite, real, and roughly on-pitch.
            bool slotsUsable = true;
            for (int k = 0; k < OwnEntities.Length; k++)
            {
                Vector2 slot = chain.Positioning.GetFormationSlot(OwnEntities[k]);
                bool ok = !PositioningAITick.IsSentinelSlot(slot)
                          && !float.IsNaN(slot.x) && !float.IsInfinity(slot.x)
                          && !float.IsNaN(slot.y) && !float.IsInfinity(slot.y)
                          && slot.x >= -10f && slot.x <= 115f
                          && slot.y >= -10f && slot.y <= 78f;
                slotsUsable &= ok;
            }
            context.Envelope.CheckTrue("positioning-slots-finite-onpitch", slotsUsable,
                "every own agent's live #12 formation slot is finite, non-sentinel, and on-pitch");

            // #13 half: the directive built from those slots is well-formed.
            PressDirective d = chain.Pressing.LastDirective;
            context.Envelope.CheckInRange("cover-shadow-count-bounded", d.CoverShadowCount, 0f, 2f);
            context.Envelope.CheckTrue("primary-presser-id-valid", d.PrimaryPresserId >= -1,
                "PrimaryPresserId=" + d.PrimaryPresserId.ToString(CultureInfo.InvariantCulture));

            bool rolesValid = true;
            for (int k = 0; k < OwnEntities.Length; k++)
            {
                PressRole role = chain.Pressing.GetAssignment(OwnEntities[k]).Role;
                rolesValid &= (int)role >= (int)PressRole.HoldShape && (int)role <= (int)PressRole.CoverShadow;
            }
            context.Envelope.CheckTrue("all-press-roles-defined", rolesValid,
                "every own pressing agent's assignment carries a defined PressRole");
        }

        // Two independently-constructed #12 → #13 chains driven over the identical situation and
        // tick sequence must produce a byte-identical composite digest (Positioning phase + lines
        // AND the Pressing directive + assignments) — the Simulation-layer determinism lock across
        // the whole composed chain, which neither per-spec unit suite can express.
        private static void TwoInstanceDeterminism(ScenarioContext context)
        {
            var c1 = new MechanicsChain();
            var c2 = new MechanicsChain();
            for (int t = 0; t < Ticks; t++)
            {
                c1.Step(t);
                c2.Step(t);
            }

            ulong d1 = Digest(c1);
            ulong d2 = Digest(c2);
            context.Envelope.CheckTrue("chain-digest-equal", d1 == d2,
                "d1=" + d1.ToString(CultureInfo.InvariantCulture) +
                " d2=" + d2.ToString(CultureInfo.InvariantCulture));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-04 | —      | Initial implementation. Cross-spec Mechanics chain corpus       |
// |         |            |        | (KD-8): positioning-feeds-pressing drives the real #12          |
// |         |            |        | PositioningAITick live and feeds its slot/line/phase output     |
// |         |            |        | into the real #13 PressingAITick each tick (the MatchEngine     |
// |         |            |        | handoff) + a two-instance composite-digest determinism lock.    |
// |         |            |        | Owning specs {12, 13}; paths under SCENARIO_PATH_CROSS_SPEC_PREFIX.|
#endregion
