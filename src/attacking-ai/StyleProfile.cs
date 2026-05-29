// File:     src/attacking-ai/StyleProfile.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.10, §6.1.5, FR-AT-017, Code Standards #20
// Purpose:  Team-style profile multipliers selected at match init. The algorithm code is
//           identical across profiles; only these values differ (KD-8 / KD-12).
//           No enum branching on profile identity in algorithm code (FR-AT-017).

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Team-style profile constants loaded at match initialisation. Passed to
    /// <see cref="AttackingAITick"/> as a constructor parameter; never changes within a match.
    /// All multiplier fields are plain <c>float</c>/<c>int</c> — the algorithm code is
    /// identical across profiles (KD-12 / FR-AT-017). Attacking AI #15 §3.10 / §6.1.5.
    /// </summary>
    public readonly struct StyleProfile
    {
        /// <summary>Scale factor for <c>depthOffset_m</c> in §3.4.</summary>
        public float DepthMult           { get; }

        /// <summary>Scale factor for <c>runTriggerTick</c> delay in §3.4.</summary>
        public float TimingMult          { get; }

        /// <summary>Scale factor for effective support radius in §3.5.</summary>
        public float SupportMult         { get; }

        /// <summary>Per-profile MAX_RUNNERS cap used in §3.3 and anti-chaos invariant 1 (§3.11).</summary>
        public int   MaxRunners          { get; }

        /// <summary>Ticks to freeze the last IN_POSSESSION directive on possession loss (§3.9).</summary>
        public int   TransitionHoldTicks { get; }

        /// <summary>Constructs a StyleProfile.</summary>
        public StyleProfile(
            float depthMult, float timingMult, float supportMult,
            int   maxRunners, int transitionHoldTicks)
        {
            DepthMult           = depthMult;
            TimingMult          = timingMult;
            SupportMult         = supportMult;
            MaxRunners          = maxRunners;
            TransitionHoldTicks = transitionHoldTicks;
        }

        /// <summary>POSSESSION profile (Stage 0 default). Shorter runs, wider support, conservative.</summary>
        public static StyleProfile Possession => new StyleProfile(
            AttackingAIConstants.DepthMultPossession,
            AttackingAIConstants.TimingMultPossession,
            AttackingAIConstants.SupportMultPossession,
            AttackingAIConstants.MaxRunnersPossession,
            AttackingAIConstants.TransitionHoldTicksPossession);

        /// <summary>DIRECT profile. Deeper runs, tighter support.</summary>
        public static StyleProfile Direct => new StyleProfile(
            AttackingAIConstants.DepthMultDirect,
            AttackingAIConstants.TimingMultDirect,
            AttackingAIConstants.SupportMultDirect,
            AttackingAIConstants.MaxRunnersDirect,
            AttackingAIConstants.TransitionHoldTicksDirect);

        /// <summary>COUNTER_ATTACK profile. Deepest runs, immediate transition recovery.</summary>
        public static StyleProfile Counter => new StyleProfile(
            AttackingAIConstants.DepthMultCounter,
            AttackingAIConstants.TimingMultCounter,
            AttackingAIConstants.SupportMultCounter,
            AttackingAIConstants.MaxRunnersCounter,
            AttackingAIConstants.TransitionHoldTicksCounter);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
