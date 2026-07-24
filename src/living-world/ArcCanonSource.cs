// File:     src/living-world/ArcCanonSource.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Living World System #22 §3.4, FR-LW-017/020/031, arc-triggers-design KD-1/KD-2, Code Standards #20
// Purpose:  The nullable canon-input seam the ArcTriggerEvaluator reads — a single concrete Stage-0
//           producer of the minimal per-entity / per-board scalar signals arc triggers threshold on.

using System;
using System.Collections.Generic;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The concrete, nullable canon-input seam an <see cref="ArcTriggerEvaluator"/> reads (#22 §3.4,
    /// arc-triggers-design KD-1/KD-2). It exposes only the minimal scalar signals triggers threshold
    /// on, iterated in canonical entity-ID order (ascending) for entity-scoped triggers plus a
    /// per-<see cref="ArcKind"/> board/squad-level channel (FR-LW-017/021). Every reported value is a
    /// plain float that maps 1:1 onto <see cref="SpawnCause.Input"/> { short Key; float Value }.
    ///
    /// KD-2 — this is a <b>single concrete sealed class</b>, deliberately NOT an interface (nor an
    /// abstract base with one subclass): the project injects concrete nullable seams and an interface
    /// with one implementation is the phantom shape FR-LW-031 warns against. The Stage-0 in-code
    /// producer IS this class — a caller (or a future producer) constructs it from world state via the
    /// nested <see cref="Builder"/>, the "author the canon in code now, swap the producer later"
    /// pattern (the ScenarioIndex / TeamTacticConfig precedent). When the real vol-2/vol-3 producer
    /// lands it becomes a <i>second</i> concrete type; a base/interface is extracted only then.
    ///
    /// A <c>null</c> canon source is the default at the evaluator/WorldStore seam and means "skip
    /// phase-4 trigger evaluation" — byte-identical to a run with no arcs. Determinism: an instance is
    /// immutable and its signals are captured values, so an evaluator handed the same canon each tick
    /// (or a canon re-derived deterministically from the serialized world state on restore) sees the
    /// same signals. Day-cadence — not subject to the 60 Hz zero-alloc hot-path rules.
    /// </summary>
    public sealed class ArcCanonSource
    {
        private readonly int[] _entityIds;                       // ascending, unique, all >= 0
        private readonly Dictionary<long, float> _entitySignals; // key = pack(entityIndex, signalKey)
        private readonly Dictionary<int, float> _boardSignals;   // key = pack(ArcKind ordinal, signalKey)

        private ArcCanonSource(int[] entityIds, Dictionary<long, float> entitySignals,
            Dictionary<int, float> boardSignals)
        {
            _entityIds = entityIds;
            _entitySignals = entitySignals;
            _boardSignals = boardSignals;
        }

        /// <summary>Number of entity-scoped signal rows (contacts this canon reports on).</summary>
        public int EntitySignalCount => _entityIds.Length;

        /// <summary>The entity id at the given index; indices are ascending by entity id (FR-LW-017).</summary>
        public int EntityIdAt(int index) => _entityIds[index];

        /// <summary>
        /// The scalar for the given entity index + signal key, or 0 when the canon reports no such
        /// signal (an absent signal is "no pressure" — below any positive trigger threshold).
        /// </summary>
        public float EntitySignal(int index, short key)
            => _entitySignals.TryGetValue(PackEntity(index, key), out float v) ? v : 0f;

        /// <summary>
        /// The board/squad-level scalar for the given <see cref="ArcKind"/> + signal key, or 0 when the
        /// canon reports no such signal.
        /// </summary>
        public float BoardSignal(ArcKind kind, short key)
            => _boardSignals.TryGetValue(PackBoard(kind, key), out float v) ? v : 0f;

        private static long PackEntity(int entityIndex, short key)
            => ((long)entityIndex << 16) | (ushort)key;

        private static int PackBoard(ArcKind kind, short key)
            => ((int)kind << 16) | (ushort)key;

        /// <summary>
        /// Builds an immutable <see cref="ArcCanonSource"/>. Entity signals are registered by entity
        /// id; <see cref="Build"/> sorts the entities ascending and freezes the mapping. This is the
        /// in-code Stage-0 authoring surface (a construction helper, not an abstraction of the canon).
        /// </summary>
        public sealed class Builder
        {
            private readonly SortedSet<int> _entities = new SortedSet<int>();
            private readonly List<(int entityId, short key, float value)> _entitySignals =
                new List<(int, short, float)>();
            private readonly Dictionary<int, float> _boardSignals = new Dictionary<int, float>();

            /// <summary>Registers an entity (a contact) with no signal yet. Ids must be non-negative
            /// (real entity ids are >= 0; negatives are reserved for the board scope sentinel).</summary>
            public Builder AddEntity(int entityId)
            {
                if (entityId < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(entityId), entityId,
                        "ArcCanonSource.Builder: entity id must be non-negative.");
                }
                _entities.Add(entityId);
                return this;
            }

            /// <summary>Sets an entity-scoped signal value (implicitly registers the entity).</summary>
            public Builder SetEntitySignal(int entityId, short key, float value)
            {
                AddEntity(entityId);
                _entitySignals.Add((entityId, key, value));
                return this;
            }

            /// <summary>Sets a board/squad-level signal value for the given arc kind.</summary>
            public Builder SetBoardSignal(ArcKind kind, short key, float value)
            {
                _boardSignals[PackBoard(kind, key)] = value;
                return this;
            }

            /// <summary>Freezes the builder into an immutable <see cref="ArcCanonSource"/>.</summary>
            public ArcCanonSource Build()
            {
                int[] ids = new int[_entities.Count];
                _entities.CopyTo(ids);   // SortedSet enumerates ascending

                // entity id -> ascending index
                Dictionary<int, int> indexOf = new Dictionary<int, int>(ids.Length);
                for (int i = 0; i < ids.Length; i++)
                {
                    indexOf[ids[i]] = i;
                }

                Dictionary<long, float> signals = new Dictionary<long, float>(_entitySignals.Count);
                for (int i = 0; i < _entitySignals.Count; i++)
                {
                    (int entityId, short key, float value) = _entitySignals[i];
                    signals[PackEntity(indexOf[entityId], key)] = value;
                }

                return new ArcCanonSource(ids, signals, new Dictionary<int, float>(_boardSignals));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation (arc-triggers Slice 1): the concrete   |
// |         |            |        | KD-1/KD-2 nullable canon seam + nested Builder (the Stage-0    |
// |         |            |        | in-code producer). Reports per-entity (ascending id) + per-   |
// |         |            |        | ArcKind board scalar signals for the ArcTriggerEvaluator.     |
#endregion
