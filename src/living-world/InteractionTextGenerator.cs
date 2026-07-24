// File:     src/living-world/InteractionTextGenerator.cs
// Created:  2026-07-02
// Modified: 2026-07-24 (arc-triggers Slice 1: world.text entityId literal −1 → cataloged
//           WORLD_STREAM_ENTITY_TEXT sentinel; behaviour-identical, distinct from world.arcs)
// Modified: 2026-07-03 (slice-3 AR-1 L-3: DrawReserved-failure documented as a corruption abort)
// Author:   —
// Spec:     Living World System #22 §3.3 (KD-6), §3.6, FR-LW-011/012/013/016/020, Code Standards #20
// Purpose:  Deterministic §3.3 surface-text generation: template selection draws from the dedicated
//           aperiodic world.text RNG sub-stream; expansion fills slots from real facts + a cited
//           §3.2 episode. No model inference on any saved-state path (FR-LW-012).

using System;
using System.Globalization;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Produces interaction surface text from an <see cref="InteractionIntent"/> + slot facts
    /// (#22 §3.3): <c>SelectTemplate(intent, rng.DrawReserved(world.text))</c> then deterministic
    /// slot expansion. Registers the <b>aperiodic <c>world.text</c> sub-stream</b> on the injected
    /// <see cref="DeterministicRngService"/> (FR-LW-011/020) — separate from the tick-driven
    /// <c>world.arcs</c> stream (still the ArcEngine's KD-10 seam), so player-triggered generation
    /// never perturbs the arc/world cursor. This class is the stream's first and only draw site.
    ///
    /// DETERMINISM (T-LW-DET-003): same (intent, world.text cursor, slots) ⇒ identical string.
    /// All validation (intent roster, slot facts, the §3.2 SALIENCE_REF_THRESHOLD citation gate,
    /// clause lookup) runs BEFORE the draw, so a refused call consumes no cursor and replay parity
    /// holds across fail-loud paths. Provenance is implicit per FR-LW-016/§3.6 — a deterministic
    /// function of (intent, RNG cursor, slots); no interaction record is persisted.
    ///
    /// Construct ONE generator per world per service: each construction registers a new stream on
    /// the injected service (a second instance would draw from an independent cursor). Aperiodic /
    /// day-cadence — not subject to the 60 Hz zero-alloc string-formatting rules (FR-CS-031 targets
    /// the match hot path; MemoryStore precedent).
    /// </summary>
    public sealed class InteractionTextGenerator
    {
        private readonly DeterministicRngService _rng;
        private readonly int _streamIndex;

        /// <summary>
        /// The registered world.text stream index on the injected service. Exposed so the save/replay
        /// composition root can serialize/restore the stream's cursor via the #16
        /// <c>GetStreamState</c>/<c>RestoreStream</c> machinery, and so the T-LW-DET-004 separation
        /// test can read the cursor directly.
        /// </summary>
        public int StreamIndex => _streamIndex;

        /// <summary>
        /// Registers the <c>world.text</c> sub-stream (FR-LW-020) on the injected deterministic RNG
        /// service. <see cref="LivingWorldConstants.WORLD_STREAM_ENTITY_TEXT"/> (−1) = the world-scoped
        /// (non-entity) stream, matching the project's loose/none sentinel. This value is the cataloged
        /// sentinel — behaviour-identical to the prior literal −1, but distinct from the world.arcs
        /// stream's sentinel so the two world-scoped streams get distinct ComputeStreamKey values.
        /// </summary>
        public InteractionTextGenerator(DeterministicRngService rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _streamIndex = rng.RegisterStream(
                LivingWorldConstants.WORLD_TEXT_STREAM_SITE_ID,
                SubsystemOrdinals.LivingWorld,
                LivingWorldConstants.WORLD_STREAM_ENTITY_TEXT,
                LivingWorldConstants.WORLD_TEXT_STREAM_VERSION);
        }

        /// <summary>
        /// Generates the surface text for one interaction: exactly one world.text draw selects the
        /// template (draw mod template count), then slots are expanded and the cited episode's
        /// clause appended. A cited episode must satisfy the §3.2 referencing gate
        /// (salience ≥ SALIENCE_REF_THRESHOLD; the NaN-gated negated compare fails closed) — citing
        /// an ineligible episode is a caller contract violation and fails loud without consuming a
        /// draw.
        /// </summary>
        public string Generate(InteractionIntent intent, in InteractionSlots slots)
        {
            string[] templates = InteractionTextCorpus.TemplatesFor(intent);
            if (string.IsNullOrEmpty(slots.SubjectName) || string.IsNullOrEmpty(slots.OpponentName)
                || slots.HomeGoals < 0 || slots.AwayGoals < 0)
            {
                // Default-valued struct bypasses the InteractionSlots constructors — re-gate here.
                throw new ArgumentException("InteractionTextGenerator.Generate: malformed slots (construct via an InteractionSlots constructor).", nameof(slots));
            }

            string clause = null;
            if (slots.HasCitedEpisode)
            {
                if (!(slots.CitedEpisode.Salience >= LivingWorldConstants.SALIENCE_REF_THRESHOLD))
                {
                    throw new InvalidOperationException(
                        "InteractionTextGenerator.Generate: cited episode is below SALIENCE_REF_THRESHOLD — not citable (§3.2 referencing gate).");
                }
                clause = InteractionTextCorpus.EpisodeClause(slots.CitedEpisode.Kind);
            }

            // Exactly one reserved draw per generated interaction (FR-LW-020 Reserve/DrawReserved/
            // CloseReservation discipline). All refusals above run pre-draw so a failed call leaves
            // the cursor untouched.
            if (_rng.Reserve(_streamIndex, 1) != 0)
            {
                throw new InvalidOperationException("InteractionTextGenerator.Generate: a world.text reservation is already open (draw-site misuse).");
            }
            if (_rng.DrawReserved(_streamIndex, 0, out ulong draw) != 0)
            {
                // L-3: UNREACHABLE under the API contract — Reserve(1) above set DeclaredBudget = 1,
                // so index 0 is always in range. Reaching here means the RNG service's reservation
                // state is corrupt (an internal invariant violation), NOT one of the ordinary
                // pre-draw refusals (those all return BEFORE opening the reservation and consume no
                // cursor). We MUST close the now-open reservation so the stream stays usable for the
                // next caller; the resulting cursor advance is the deliberate cost of aborting a
                // corrupt draw, not a silent cursor burn on a normal refusal.
                _rng.CloseReservation(_streamIndex);
                throw new InvalidOperationException("InteractionTextGenerator.Generate: world.text draw failed — corrupt reservation state (internal invariant).");
            }
            _rng.CloseReservation(_streamIndex);

            string template = templates[(int)(draw % (ulong)templates.Length)];
            string text = Expand(template, in slots);
            return clause == null ? text : text + " " + clause;
        }

        private static string Expand(string template, in InteractionSlots slots)
        {
            string score = slots.HomeGoals.ToString(CultureInfo.InvariantCulture)
                + "-" + slots.AwayGoals.ToString(CultureInfo.InvariantCulture);
            return template
                .Replace("{subject}", slots.SubjectName)
                .Replace("{opponent}", slots.OpponentName)
                .Replace("{score}", score);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (slice 3): §3.3 deterministic template |
// |         |            |        | selection off the new world.text sub-stream (its first draw   |
// |         |            |        | site) + slot expansion + §3.2 citation gate. Validation runs  |
// |         |            |        | pre-draw so refusals consume no cursor.                       |
// | 1.1     | 2026-07-03 | —      | Slice-3 AR-1 L-3: the (unreachable) DrawReserved-failure       |
// |         |            |        | branch documented as an internal-invariant corruption abort — |
// |         |            |        | its CloseReservation cursor advance is deliberate (keeps the  |
// |         |            |        | stream usable), not a normal pre-draw refusal.                |
#endregion
