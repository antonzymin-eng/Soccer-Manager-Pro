// File:     src/living-world/Tests/LivingWorldTests.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §5, Testing Strategy & Framework #19 §3.1.4, Code Standards #20
// Purpose:  T0 unit tests for the living-world data types and pure math. Locks the contracts the spec
//           can verify in isolation now (enum ordinal stability; the §3.1 update/decay worked examples;
//           the FR-LW-021 eviction tiebreak; ActiveLayers masking; episodeId-counter resume). The
//           remaining T-LW-* suites (text/arc/LOD/closed-loop) land as their upstreams are wired (KD-10).

using NUnit.Framework;

using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public class LivingWorldTests
    {
        // ── T-LW-U-001..004 — enum ordinal stability (APPEND-only; FR-LW-028) ──────────────

        [Test]
        public void TLWU001_RelationshipLayerOrdinals()
        {
            Assert.AreEqual(0, (byte)RelationshipLayer.PlayerEdge, "T-LW-U-001: PlayerEdge ordinal");
            Assert.AreEqual(1, (byte)RelationshipLayer.Affinity,   "T-LW-U-001: Affinity ordinal");
            Assert.AreEqual(2, (byte)RelationshipLayer.Trust,      "T-LW-U-001: Trust ordinal");
        }

        [Test]
        public void TLWU002_ArcKindOrdinals()
        {
            Assert.AreEqual(0, (byte)ArcKind.DressingRoomSplit,     "T-LW-U-002");
            Assert.AreEqual(1, (byte)ArcKind.MediaVendetta,         "T-LW-U-002");
            Assert.AreEqual(2, (byte)ArcKind.BoardPatienceCollapse, "T-LW-U-002");
            Assert.AreEqual(3, (byte)ArcKind.WonderkidVsVeteran,    "T-LW-U-002");
        }

        [Test]
        public void TLWU003_EventKindNoneIsZero()
        {
            Assert.AreEqual(0, (byte)EventKind.None, "T-LW-U-003: None must be ordinal 0");
        }

        [Test]
        public void TLWU004_InteractionIntentNoneIsZero()
        {
            Assert.AreEqual(0, (byte)InteractionIntent.None, "T-LW-U-004: None must be ordinal 0");
        }

        // ── T-LW-U-005..010 — §3.1 edge update / decay (FR-LW-004/005/034) ─────────────────

        [Test]
        public void TLWU005_ApplyEvent_WorkedExample_ReproducesPoint56()
        {
            // §3.1 worked example: Trust 0.50, δ=+0.4, v=0.3 → 0.56.
            float x = LivingWorldMath.ApplyEvent(0.50f, 0.40f, 0.30f);
            Assert.That(x, Is.EqualTo(0.56f).Within(1e-5f), "T-LW-U-005: §3.1 worked example");
        }

        [Test]
        public void TLWU006_ApplyEvent_NegativeDelta_MovesTowardZero()
        {
            float x = LivingWorldMath.ApplyEvent(0.50f, -0.40f, 0.30f);
            // 0.50 + 0.3·(−0.4)·0.50 = 0.50 − 0.06 = 0.44
            Assert.That(x, Is.EqualTo(0.44f).Within(1e-5f), "T-LW-U-006");
        }

        [Test]
        public void TLWU007_ApplyEvent_NeverOvershootsBounds()
        {
            float hi = LivingWorldMath.ApplyEvent(0.99f, 1.0f, 1.0f);
            float lo = LivingWorldMath.ApplyEvent(0.01f, -1.0f, 1.0f);
            Assert.That(hi, Is.LessThanOrEqualTo(1.0f), "T-LW-U-007: upper bound");
            Assert.That(lo, Is.GreaterThanOrEqualTo(0.0f), "T-LW-U-007: lower bound");
        }

        [Test]
        public void TLWU008_ApplyEvent_ZeroDelta_IsNoOp()
        {
            // FR-LW-034 additive-only identity: no event ⇒ no change.
            float x = LivingWorldMath.ApplyEvent(0.37f, 0f, 0.30f);
            Assert.That(x, Is.EqualTo(0.37f).Within(1e-6f), "T-LW-U-008");
        }

        [Test]
        public void TLWU009_ApplyDecay_GeometricRelaxation_30Days()
        {
            // §3.1 decay example: 0.56 → ~0.544 over 30 idle days (r=0.01, b=0.5); ~0.016 closed.
            float x = 0.56f;
            for (int day = 0; day < 30; day++)
                x = LivingWorldMath.ApplyDecay(x, 0.5f, 0.01f);
            Assert.That(x, Is.EqualTo(0.544f).Within(1e-3f), "T-LW-U-009: geometric decay");
            Assert.That(0.56f - x, Is.EqualTo(0.016f).Within(1e-3f), "T-LW-U-009: ~0.016 closed");
        }

        [Test]
        public void TLWU010_ApplyDecay_AtBaseline_IsNoOp()
        {
            float x = LivingWorldMath.ApplyDecay(0.5f, 0.5f, 0.01f);
            Assert.That(x, Is.EqualTo(0.5f).Within(1e-6f), "T-LW-U-010");
        }

        // ── Eviction tiebreak determinism (FR-LW-021) ──────────────────────────────────────

        [Test]
        public void Eviction_TiesBreakByOlderWorldTickThenLowerEpisodeId()
        {
            // Equal salience: older worldTick is more evictable.
            var older  = new MemoryEpisode(9u, EventKind.None, 0.10f, worldTick: 5u, managerChoiceId: 0);
            var newer  = new MemoryEpisode(2u, EventKind.None, 0.10f, worldTick: 8u, managerChoiceId: 0);
            Assert.That(LivingWorldMath.CompareEvictability(older, newer), Is.LessThan(0),
                "older worldTick evicts first on a salience tie");

            // Equal salience and worldTick: lower episodeId is more evictable.
            var lowId  = new MemoryEpisode(3u, EventKind.None, 0.10f, worldTick: 5u, managerChoiceId: 0);
            var highId = new MemoryEpisode(7u, EventKind.None, 0.10f, worldTick: 5u, managerChoiceId: 0);
            Assert.That(LivingWorldMath.CompareEvictability(lowId, highId), Is.LessThan(0),
                "lower episodeId evicts first on a full tie");

            // Lower salience always evicts first regardless of tick/id.
            var lowSal = new MemoryEpisode(99u, EventKind.None, 0.05f, worldTick: 9u, managerChoiceId: 0);
            var hiSal  = new MemoryEpisode(1u,  EventKind.None, 0.50f, worldTick: 1u, managerChoiceId: 0);
            Assert.That(LivingWorldMath.CompareEvictability(lowSal, hiSal), Is.LessThan(0),
                "lower salience evicts first");
        }

        // ── ActiveLayers masking (FR-LW-005) ───────────────────────────────────────────────

        [Test]
        public void ActiveLayers_MaskReflectsLayerBits()
        {
            var edge = new RelationshipEdge
            {
                ActiveLayers = (byte)((1 << (int)RelationshipLayer.Affinity)
                                    | (1 << (int)RelationshipLayer.Trust)),
            };
            Assert.IsFalse(edge.IsLayerActive(RelationshipLayer.PlayerEdge), "PlayerEdge inactive on a manager↔contact edge");
            Assert.IsTrue(edge.IsLayerActive(RelationshipLayer.Affinity),   "Affinity active");
            Assert.IsTrue(edge.IsLayerActive(RelationshipLayer.Trust),      "Trust active");
        }

        // ── episodeId-counter resume across cold-store (FR-LW-009) ──────────────────────────

        [Test]
        public void ColdSummary_RehydrationResumesEpisodeIdHighWaterMark()
        {
            // A demoted edge kept only the top-N salient episodes (e.g. ids 2 and 5) but had allocated
            // up to id 11; NextEpisodeId carries the true high-water mark so rehydration does not reuse
            // the compressed-away ids 6..10 (which deriving max(retained)+1 = 6 would wrongly do).
            var summary = new ColdSummary
            {
                EntityId = 42,
                ActiveLayers = (byte)(1 << (int)RelationshipLayer.Affinity),
                NetRelationship = 0.4f,
                NextEpisodeId = 11u,
                RetainedEpisodes = new[]
                {
                    new MemoryEpisode(2u, EventKind.ManagerDefence, 0.9f, 3u, 0),
                    new MemoryEpisode(5u, EventKind.Benching,       0.7f, 6u, 0),
                },
            };

            uint derivedWrong = 0u;
            foreach (var e in summary.RetainedEpisodes)
                if (e.EpisodeId + 1u > derivedWrong) derivedWrong = e.EpisodeId + 1u;

            Assert.That(derivedWrong, Is.EqualTo(6u), "max(retained)+1 underestimates");
            Assert.That(summary.NextEpisodeId, Is.GreaterThan(derivedWrong),
                "FR-LW-009: NextEpisodeId preserves monotonicity; rehydration must resume from it, not max(retained)+1");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
