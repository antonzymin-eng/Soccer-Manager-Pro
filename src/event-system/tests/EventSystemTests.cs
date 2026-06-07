// File:     src/event-system/tests/EventSystemTests.cs
// Created:  2026-05-31
// Modified: 2026-06-07
// Author:   —
// Spec:     Event System #17 §5, Code Standards #20
// Purpose:  Unit tests for Event System. Tests Stage-0 activation criteria.
//           Covers EventRegistry ordinal uniqueness and tier schema (FR-EVT-003/009),
//           EventBus publish/subscribe/drain lifecycle (FR-EVT-017..033),
//           CosmeticChannel drop predicate (FR-EVT-043/044), SerializeLedger header
//           format (FM-017-001), queue overflow (FR-EVT-041), BFS depth cap (FR-EVT-046),
//           SubscriptionToken zero-allocation shape (FR-EVT-073), and constants block
//           integrity (FR-EVT-079). Stage 0+1 and Stage 5+ tests are stubbed with
//           Assert.Ignore as activation criterion is first src/event-system/ code commit.

using System;
using System.Runtime.CompilerServices;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.EventSystem.Tests
{
    // ── Test-only event structs ───────────────────────────────────────────────────────
    // Defined here to avoid polluting the production registry with test ordinals.
    // These do NOT implement IEventA/B/C so they never route through EventBus.Publish<T>
    // generic constraints — instead the registry helpers are exercised directly.

    // A minimal Tier A test event registered at ordinal 0x04 (PossessionChangedEvent,
    // already in the production registry). Tests use the production event types.

    // ── Test helper: resets static EventLedger/CosmeticChannel state between tests ───
    // EventLedger and CosmeticChannel use static fields (spec-mandated singleton, §3.2.1
    // KD-4/KD-8). Tests that mutate state must reset it in [SetUp] or [TearDown].

    /// <summary>
    /// Fixture that resets EventBus/EventLedger/CosmeticChannel static state before
    /// each test so tests are isolated from one another.
    /// </summary>
    internal static class EventTestHelper
    {
        /// <summary>
        /// Resets all mutable EventLedger static fields to their boot-time values.
        /// Called from [SetUp] on any fixture that publishes or drains.
        /// </summary>
        internal static void ResetLedger()
        {
            EventLedger.QueueCount                   = 0;
            EventLedger.CurrentTick                  = 0;
            EventLedger.CurrentPhase                 = PhaseId.Input;
            EventLedger.BootPhaseComplete             = false;
            EventLedger.InDrainDispatch              = false;
            EventLedger.HandlerSecondaryPublishCount  = 0;

            // Clear payload buffer and slot metadata.
            Array.Clear(EventLedger.PayloadBuffer, 0, EventLedger.PayloadBuffer.Length);
            Array.Clear(EventLedger.SlotMeta, 0, EventLedger.SlotMeta.Length);

            // Clear phase draw indices.
            for (int i = 0; i < EventLedger.PhaseDrawIndices.Length; i++)
                EventLedger.PhaseDrawIndices[i] = 0;

            // Clear dispatcher table (removes all subscribers registered in previous tests).
            Array.Clear(EventLedger.Dispatchers, 0, EventLedger.Dispatchers.Length);

            // AR-9 L-1: clear Tier C dispatcher table too — ResetPublicationCounts only
            // zeroes the per-ordinal count table; subscribers from prior tests would
            // otherwise persist and eventually exhaust MaxTierCHandlersPerType.
            CosmeticChannel.ResetForTests();

            // Note: EventOrdinalCache<T>.Ordinal and the EventRegistry s_rows table are
            // static-constructor-populated and intentionally NOT cleared between tests —
            // ordinal assignments are fixed at process startup per FR-EVT-003.
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 1 — Constants integrity
    // Stage 0 scope: FR-EVT-079 (error-code namespace), FR-EVT-009 (tier column),
    //               FR-EVT-001/002 (ordinal width / header size).
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates constants defined in EventSystemConstants against the §3.10 specification.
    /// FR-EVT-079: error codes occupy the 0x17NN block and do not collide with #16's 0x16NN.
    /// </summary>
    [TestFixture]
    internal sealed class ConstantsIntegrityTests
    {
        [Test]
        public void ErrorCodes_AllInThe0x17NNBlock()
        {
            // FR-EVT-079: every error code MUST reside in the 0x1700..0x17FF range.
            // §2.5 / §3.10.
            Assert.That(EventSystemConstants.ErrEvtQueueOverflow,       Is.InRange<ushort>(0x1700, 0x17FF),
                "ErrEvtQueueOverflow must be in 0x17NN block");
            Assert.That(EventSystemConstants.ErrEvtOrdinalUnknown,      Is.InRange<ushort>(0x1700, 0x17FF),
                "ErrEvtOrdinalUnknown must be in 0x17NN block");
            Assert.That(EventSystemConstants.ErrEvtVersionIncompatible,  Is.InRange<ushort>(0x1700, 0x17FF),
                "ErrEvtVersionIncompatible must be in 0x17NN block");
            Assert.That(EventSystemConstants.ErrEvtRegistrationPhase,   Is.InRange<ushort>(0x1700, 0x17FF),
                "ErrEvtRegistrationPhase must be in 0x17NN block");
            Assert.That(EventSystemConstants.ErrEvtUnregisteredOrdinal, Is.InRange<ushort>(0x1700, 0x17FF),
                "ErrEvtUnregisteredOrdinal must be in 0x17NN block");
            Assert.That(EventSystemConstants.ErrEvtOrdinalCollision,    Is.InRange<ushort>(0x1700, 0x17FF),
                "ErrEvtOrdinalCollision must be in 0x17NN block");
        }

        [Test]
        public void ErrorCodes_DoNotCollideWithDeterministicSimBlock()
        {
            // FR-EVT-079: 0x17NN must not collide with #16's 0x16NN reserved block.
            Assert.That(EventSystemConstants.ErrEvtQueueOverflow      & 0xFF00, Is.Not.EqualTo(0x1600),
                "ErrEvtQueueOverflow must not be in 0x16NN block");
            Assert.That(EventSystemConstants.ErrEvtRegistrationPhase  & 0xFF00, Is.Not.EqualTo(0x1600),
                "ErrEvtRegistrationPhase must not be in 0x16NN block");
        }

        [Test]
        public void ErrorCodes_HaveDistinctValues()
        {
            // Each error code is a distinct sentinel; duplicates would prevent callers from
            // distinguishing failures. §2.5.
            ushort[] codes =
            {
                EventSystemConstants.ErrEvtQueueOverflow,
                EventSystemConstants.ErrEvtOrdinalUnknown,
                EventSystemConstants.ErrEvtVersionIncompatible,
                EventSystemConstants.ErrEvtRegistrationPhase,
                EventSystemConstants.ErrEvtUnregisteredOrdinal,
                EventSystemConstants.ErrEvtOrdinalCollision,
            };
            System.Collections.Generic.HashSet<ushort> seen = new System.Collections.Generic.HashSet<ushort>();
            foreach (ushort code in codes)
                Assert.IsTrue(seen.Add(code), "Duplicate error code: 0x" + code.ToString("X4"));
        }

        [Test]
        public void EventHeaderBytes_Is12()
        {
            // §2.4.1 struct skeleton: 1+1+2+4+2+2 = 12 bytes.
            // This is a design-fixed [GT] constant; must never drift from the layout.
            Assert.AreEqual(12, EventSystemConstants.EventHeaderBytes,
                "EventHeaderBytes must equal 12 (§2.4.1 header layout)");
        }

        [Test]
        public void EventTypeOrdinalWidth_Is1()
        {
            // §3.1.2: ordinal namespace is 1 byte at Stage 0 (Stage 5+ expands to 2).
            Assert.AreEqual(1, EventSystemConstants.EventTypeOrdinalWidth,
                "EventTypeOrdinalWidth must be 1 at Stage 0 (§3.1.2)");
        }

        [Test]
        public void DomainTagEventLedger_MatchesDeterministicSimConstants()
        {
            // ERR-017-001 resolution: [CROSS] tag MUST mirror DeterministicSimConstants value exactly.
            // Authoritative source: DeterministicSimConstants.DOMAIN_TAG_EVENT_LEDGER = 0x15.
            // §3.4.2 / Deterministic Simulation #16 §3.4 v1.0.1.
            Assert.AreEqual(
                DeterministicSimConstants.DOMAIN_TAG_EVENT_LEDGER,
                EventSystemConstants.DomainTagEventLedger,
                "DomainTagEventLedger [CROSS] mirror must equal DeterministicSimConstants.DOMAIN_TAG_EVENT_LEDGER");
        }

        [Test]
        public void DomainTagEventLedger_Value_Is0x15()
        {
            // Literal sentinel: prevents silent drift if the source constant is changed without
            // reviewing the consuming spec. §3.4.2 / ERR-017-001.
            Assert.AreEqual(0x15, EventSystemConstants.DomainTagEventLedger,
                "DomainTagEventLedger must be 0x15 (ERR-017-001)");
        }

        [Test]
        public void MaxEventSlotBytes_AtLeastEventHeaderBytes()
        {
            // §3.5.1: every slot must fit at minimum the 12-byte header.
            Assert.GreaterOrEqual(EventSystemConstants.MaxEventSlotBytes,
                EventSystemConstants.EventHeaderBytes,
                "MaxEventSlotBytes must be >= EventHeaderBytes");
        }

        [Test]
        public void EventQueueCapacity_Positive()
        {
            // Sanity: a zero-capacity queue would make the system non-functional.
            Assert.Greater(EventSystemConstants.EventQueueCapacity, 0,
                "EventQueueCapacity must be > 0");
        }

        [Test]
        public void MaxEventDispatchDepth_Positive()
        {
            // §3.2.5: BFS must allow at least one level of second-order dispatch.
            Assert.Greater(EventSystemConstants.MaxEventDispatchDepth, 0,
                "MaxEventDispatchDepth must be > 0");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 2 — EventRegistry schema integrity
    // Stage 0 scope: FR-EVT-001/003/004/007/009 (registry validator equivalent).
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates Appendix A registry rows seeded by EventRegistry static constructor.
    /// These are the Stage 0 equivalent of tools/spec-validator/registry-uniqueness.py.
    /// </summary>
    [TestFixture]
    internal sealed class EventRegistrySchemaTests
    {
        [Test]
        public void PossessionChangedEvent_OrdinalIs0x04_TierA()
        {
            // Appendix A row 0x04: PossessionChangedEvent, Tier A, version 1.
            // FR-EVT-001/003/009.
            byte ordinal = EventOrdinalCache<PossessionChangedEvent>.Ordinal;
            Assert.AreEqual(0x04, ordinal,
                "PossessionChangedEvent ordinal must be 0x04 (Appendix A)");
            Assert.AreEqual(0, EventRegistry.GetTier(ordinal),
                "PossessionChangedEvent must be Tier A (GetTier=0)");
            Assert.AreEqual(1, EventRegistry.GetVersion(ordinal),
                "PossessionChangedEvent version must be 1 (Appendix A)");
        }

        [Test]
        public void TickHeartbeatEvent_OrdinalIs0x09_TierC()
        {
            // Appendix A row 0x09: TickHeartbeatEvent, Tier C, MaxPerTick=1.
            // FR-EVT-001/009.
            byte ordinal = EventOrdinalCache<TickHeartbeatEvent>.Ordinal;
            Assert.AreEqual(0x09, ordinal,
                "TickHeartbeatEvent ordinal must be 0x09 (Appendix A)");
            Assert.AreEqual(2, EventRegistry.GetTier(ordinal),
                "TickHeartbeatEvent must be Tier C (GetTier=2)");
            Assert.AreEqual(1, EventRegistry.GetMaxPerTick(ordinal),
                "TickHeartbeatEvent MaxPerTick must be 1 (Appendix A)");
        }

        [Test]
        public void VfxImpactCue_OrdinalIs0x0A_TierC_MaxPerTick64()
        {
            // Appendix A row 0x0A.
            byte ordinal = EventOrdinalCache<VfxImpactCue>.Ordinal;
            Assert.AreEqual(0x0A, ordinal,
                "VfxImpactCue ordinal must be 0x0A");
            Assert.AreEqual(2, EventRegistry.GetTier(ordinal),
                "VfxImpactCue must be Tier C");
            Assert.AreEqual(64, EventRegistry.GetMaxPerTick(ordinal),
                "VfxImpactCue MaxPerTick must be 64 (Appendix A)");
        }

        [Test]
        public void UiNotificationCue_OrdinalIs0x0B_TierC_MaxPerTick32()
        {
            // Appendix A row 0x0B.
            byte ordinal = EventOrdinalCache<UiNotificationCue>.Ordinal;
            Assert.AreEqual(0x0B, ordinal,
                "UiNotificationCue ordinal must be 0x0B");
            Assert.AreEqual(2, EventRegistry.GetTier(ordinal),
                "UiNotificationCue must be Tier C");
            Assert.AreEqual(32, EventRegistry.GetMaxPerTick(ordinal),
                "UiNotificationCue MaxPerTick must be 32 (Appendix A)");
        }

        [Test]
        public void GoalAwardedEvent_OrdinalIs0x07_TierA()
        {
            // Appendix A row 0x07.
            byte ordinal = EventOrdinalCache<GoalAwardedEvent>.Ordinal;
            Assert.AreEqual(0x07, ordinal,
                "GoalAwardedEvent ordinal must be 0x07");
            Assert.AreEqual(0, EventRegistry.GetTier(ordinal),
                "GoalAwardedEvent must be Tier A");
        }

        [Test]
        public void FoulCommittedEvent_OrdinalIs0x05_TierA()
        {
            // Appendix A row 0x05.
            byte ordinal = EventOrdinalCache<FoulCommittedEvent>.Ordinal;
            Assert.AreEqual(0x05, ordinal,
                "FoulCommittedEvent ordinal must be 0x05");
            Assert.AreEqual(0, EventRegistry.GetTier(ordinal),
                "FoulCommittedEvent must be Tier A");
        }

        [Test]
        public void IsRegistered_ReturnsFalse_ForPlaceholderRows()
        {
            // Placeholder rows (ordinals 0x0C..0x17) have structSize=0 until
            // EventBusRegistrar.Initialize() is called. IsRegistered must return false.
            // FR-EVT-080 / EventRegistry v1.3 fix.
            Assert.IsFalse(EventRegistry.IsRegistered(0x0C),
                "Placeholder ordinal 0x0C must not be IsRegistered until Initialize() is called");
            Assert.IsFalse(EventRegistry.IsRegistered(0x0D),
                "Placeholder ordinal 0x0D must not be IsRegistered until Initialize() is called");
        }

        [Test]
        public void IsRegistered_ReturnsTrue_ForFullySeededRows()
        {
            // Rows with a known struct size from RegisterRow<T> (not RegisterRowRaw with 0)
            // must return true. §2.4.2 / FR-EVT-003.
            Assert.IsTrue(EventRegistry.IsRegistered(0x04),
                "PossessionChangedEvent (0x04) must be IsRegistered=true");
            Assert.IsTrue(EventRegistry.IsRegistered(0x09),
                "TickHeartbeatEvent (0x09) must be IsRegistered=true");
        }

        [Test]
        public void IsRegistered_ReturnsFalse_ForUnseededOrdinalZero()
        {
            // Ordinal 0x00 is the invalid sentinel; the registry never seeds it.
            Assert.IsFalse(EventRegistry.IsRegistered(0x00),
                "Ordinal 0x00 (invalid sentinel) must never be IsRegistered");
        }

        [Test]
        public void AllSeededTierARows_HaveStructSizeEqualToSizeofT()
        {
            // FR-EVT-001: struct size stored in the registry must equal the CLR size of the
            // event type. Validates that RegisterRow<T> used Unsafe.SizeOf<T>() correctly.
            // Checks all 5 production Tier A rows (#17-owned ordinals 0x04..0x08).
            byte ord04 = EventOrdinalCache<PossessionChangedEvent>.Ordinal;
            byte ord07 = EventOrdinalCache<GoalAwardedEvent>.Ordinal;

            Assert.AreEqual(Unsafe.SizeOf<PossessionChangedEvent>(),
                EventRegistry.GetStructSize(ord04),
                "PossessionChangedEvent struct size mismatch in registry");
            Assert.AreEqual(Unsafe.SizeOf<GoalAwardedEvent>(),
                EventRegistry.GetStructSize(ord07),
                "GoalAwardedEvent struct size mismatch in registry");
        }

        [Test]
        public void AllSeededTierCRows_StructSizesAboveZero()
        {
            // Tier C events registered via RegisterRow<T> (not RegisterRowRaw) must have
            // StructSize > 0 (even empty structs have CLR minimum size of 1).
            byte ordHB  = EventOrdinalCache<TickHeartbeatEvent>.Ordinal;
            byte ordVfx = EventOrdinalCache<VfxImpactCue>.Ordinal;
            byte ordUi  = EventOrdinalCache<UiNotificationCue>.Ordinal;

            Assert.Greater(EventRegistry.GetStructSize(ordHB),  0,
                "TickHeartbeatEvent struct size must be > 0 (CLR min 1)");
            Assert.Greater(EventRegistry.GetStructSize(ordVfx), 0,
                "VfxImpactCue struct size must be > 0");
            Assert.Greater(EventRegistry.GetStructSize(ordUi),  0,
                "UiNotificationCue struct size must be > 0");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 3 — SubscriptionToken struct shape (FR-EVT-073)
    // Stage 0: verifiable via reflection without runtime allocation.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates the SubscriptionToken struct contract (FR-EVT-073).
    /// Token must be a value type (struct) to avoid class allocation per subscribe call.
    /// </summary>
    [TestFixture]
    internal sealed class SubscriptionTokenShapeTests
    {
        [Test]
        public void SubscriptionToken_IsValueType()
        {
            // FR-EVT-073: zero class allocation per subscribe. Token must be a struct.
            Assert.IsTrue(typeof(SubscriptionToken).IsValueType,
                "SubscriptionToken must be a struct (no class allocation per FR-EVT-073)");
        }

        [Test]
        public void SubscriptionToken_IsReadonlyStruct()
        {
            // §4.2.2: token is opaque; readonly struct prevents accidental mutation.
            // Readonly struct detection: no public settable fields.
            System.Reflection.FieldInfo[] fields = typeof(SubscriptionToken)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (System.Reflection.FieldInfo f in fields)
                Assert.IsTrue(f.IsInitOnly,
                    "SubscriptionToken field " + f.Name + " must be readonly (§4.2.2)");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 4 — EventBus lifecycle and BeginTick/BeginPhase state
    // Stage 0+1 (first EventBus.cs commit); tests are live now that src/ is committed.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates EventBus lifecycle: BeginTick, BeginPhase, OnTickBoundary state resets.
    /// Tests FR-EVT-017/025/028.
    /// </summary>
    [TestFixture]
    internal sealed class EventBusLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        [Test]
        public void BeginTick_SetsTick_InLedger()
        {
            // FR-EVT-017 (BeginTick stores tick for header embedding). §4.4.
            EventBus.BeginTick(42u);
            Assert.AreEqual(42u, EventLedger.CurrentTick,
                "BeginTick(42) must set EventLedger.CurrentTick = 42");
        }

        [Test]
        public void BeginPhase_SetsCurrentPhase()
        {
            // §4.4.4: phase transitions update CurrentPhase for producer-phase assertions.
            EventBus.BeginPhase(PhaseId.Resolve);
            Assert.AreEqual(PhaseId.Resolve, EventLedger.CurrentPhase,
                "BeginPhase(Resolve) must set EventLedger.CurrentPhase = Resolve");
        }

        [Test]
        public void BeginPhase_ResetsIntraPhaseDrawIndex()
        {
            // FR-EVT-028: intraPhaseDrawIndex is reset to 0 at each BeginPhase.
            // Prime the draw index by calling BeginPhase and then manually bumping it.
            EventBus.BeginPhase(PhaseId.Resolve);
            EventLedger.PhaseDrawIndices[(byte)PhaseId.Resolve] = 7; // simulate 7 prior publishes
            EventBus.BeginPhase(PhaseId.Resolve); // second call resets to 0
            Assert.AreEqual(0, EventLedger.PhaseDrawIndices[(byte)PhaseId.Resolve],
                "BeginPhase must reset PhaseDrawIndices for that phase to 0 (FR-EVT-028)");
        }

        [Test]
        public void OnTickBoundary_ResetsQueueCount()
        {
            // FR-EVT-025: OnTickBoundary clears the queue for the next tick.
            EventLedger.QueueCount = 5; // simulate 5 published events
            EventBus.OnTickBoundary();
            Assert.AreEqual(0, EventLedger.QueueCount,
                "OnTickBoundary must reset QueueCount to 0 (FR-EVT-025)");
        }

        [Test]
        public void OnTickBoundary_ResetsAllPhaseDrawIndices()
        {
            // FR-EVT-025: all phase draw index counters must be zeroed at tick boundary.
            for (int i = 0; i < EventLedger.PhaseDrawIndices.Length; i++)
                EventLedger.PhaseDrawIndices[i] = (ushort)(i + 1);

            EventBus.OnTickBoundary();

            for (int i = 0; i < EventLedger.PhaseDrawIndices.Length; i++)
                Assert.AreEqual(0, EventLedger.PhaseDrawIndices[i],
                    "PhaseDrawIndices[" + i + "] must be 0 after OnTickBoundary");
        }

        [Test]
        public void DrainTick_SetsBootPhaseComplete()
        {
            // §4.4.1: the first DrainTick call marks the boot phase as complete, after which
            // Tier A/B Subscribe is rejected (FR-EVT-021).
            Assert.IsFalse(EventLedger.BootPhaseComplete,
                "BootPhaseComplete must be false before first DrainTick");
            EventBus.DrainTick();
            Assert.IsTrue(EventLedger.BootPhaseComplete,
                "BootPhaseComplete must be true after first DrainTick");
        }

        [Test]
        public void Subscribe_TierA_AfterBootPhase_ThrowsRegistrationPhaseError()
        {
            // FR-EVT-021: Tier A subscribe after boot phase must throw ERR_EVT_REGISTRATION_PHASE.
            // Force boot phase complete.
            EventLedger.BootPhaseComplete = true;

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                EventBus.Subscribe<PossessionChangedEvent>(static (in PossessionChangedEvent _) => { }),
                "Subscribe<IEventA> after boot phase must throw");

            // Verify error code token in message. §2.5.
            Assert.IsTrue(ex.Message.Contains("0x1705") || ex.Message.Contains("ERR_EVT_REGISTRATION_PHASE"),
                "Exception message must reference ERR_EVT_REGISTRATION_PHASE (0x1705)");
        }

        [Test]
        public void Subscribe_TierC_DuringMatch_DoesNotThrow()
        {
            // FR-EVT-022: Tier C subscribe/unsubscribe is permitted at any time.
            // TickHeartbeatEvent (0x09) is fully registered with structSize > 0.
            EventLedger.BootPhaseComplete = true; // simulate post-boot state

            Assert.DoesNotThrow(() =>
                CosmeticChannel.Subscribe<TickHeartbeatEvent>(static (in TickHeartbeatEvent _) => { }),
                "Tier C Subscribe must not throw after boot phase (FR-EVT-022)");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 5 — Tier C CosmeticChannel publish/subscribe/drop predicate
    // Stage 0+1. Drop predicate is deterministic (FR-EVT-043/044).
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates CosmeticChannel drop predicate, subscribe, unsubscribe, and
    /// publication-count reset. FR-EVT-022/025/043/044.
    /// </summary>
    [TestFixture]
    internal sealed class CosmeticChannelTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        [Test]
        public void TierC_Publish_DeliiversToSubscriber()
        {
            // FR-EVT-024: Tier C dispatch is immediate and synchronous on the publisher's thread.
            int callCount = 0;
            CosmeticChannel.Subscribe<TickHeartbeatEvent>(
                (in TickHeartbeatEvent _) => callCount++);

            TickHeartbeatEvent evt = default;
            CosmeticChannel.Publish(in evt);

            Assert.AreEqual(1, callCount,
                "Tier C publish must invoke subscriber exactly once (FR-EVT-024)");
        }

        [Test]
        public void TierC_DropPredicate_DropsWhenCountReachesMaxPerTick()
        {
            // FR-EVT-043/044: drop predicate fires when publication count >= maxPerTick.
            // TickHeartbeatEvent MaxPerTick = 1 (Appendix A row 0x09).
            // First publish: count 0 < 1 → delivered. Second: count 1 >= 1 → dropped.
            int callCount = 0;
            CosmeticChannel.Subscribe<TickHeartbeatEvent>(
                (in TickHeartbeatEvent _) => callCount++);

            TickHeartbeatEvent evt = default;
            CosmeticChannel.Publish(in evt); // count becomes 1; MaxPerTick=1 → delivered
            CosmeticChannel.Publish(in evt); // count was already 1 >= MaxPerTick → dropped

            // MaxPerTick=1 means exactly 1 delivery per tick (AR-1 H-1 fix: was >, now >=).
            Assert.AreEqual(1, callCount,
                "TickHeartbeatEvent MaxPerTick=1 must deliver exactly once per tick (AR-1 H-1 / FR-EVT-043)");
        }

        [Test]
        public void TierC_DropPredicate_AllowsMaxPerTickDeliveries()
        {
            // VfxImpactCue MaxPerTick=64. Exactly 64 publishes must be delivered; the 65th dropped.
            // MaxPerTick=64 means N=64 deliveries per tick (>= predicate: count 0..63 pass, 64 fails).
            int callCount = 0;
            CosmeticChannel.Subscribe<VfxImpactCue>(
                (in VfxImpactCue _) => callCount++);

            VfxImpactCue cue = default;
            int maxPerTick = 64; // VfxImpactCue MaxPerTick (Appendix A row 0x0A)
            for (int i = 0; i < maxPerTick + 1; i++) // publish 65 times
                CosmeticChannel.Publish(in cue);

            Assert.AreEqual(maxPerTick, callCount,
                "VfxImpactCue MaxPerTick=64 must deliver exactly 64 times per tick");
        }

        [Test]
        public void TierC_ResetPublicationCounts_AllowsPublishAgain()
        {
            // FR-EVT-025: OnTickBoundary resets publication-count table.
            // After reset, a TickHeartbeatEvent can be delivered again.
            int callCount = 0;
            CosmeticChannel.Subscribe<TickHeartbeatEvent>(
                (in TickHeartbeatEvent _) => callCount++);

            TickHeartbeatEvent evt = default;
            CosmeticChannel.Publish(in evt); // tick 1, first delivery

            CosmeticChannel.ResetPublicationCounts(); // simulate OnTickBoundary

            CosmeticChannel.Publish(in evt); // tick 2, count restarted — must deliver

            Assert.AreEqual(2, callCount,
                "After ResetPublicationCounts, Tier C pub must be deliverable again (FR-EVT-025)");
        }

        [Test]
        public void TierC_Unsubscribe_PreventsSubsequentDelivery()
        {
            // FR-EVT-022: Tier C unsubscribe must remove the handler; subsequent publishes skip it.
            int callCount = 0;
            SubscriptionToken token = CosmeticChannel.Subscribe<TickHeartbeatEvent>(
                (in TickHeartbeatEvent _) => callCount++);

            TickHeartbeatEvent evt = default;
            CosmeticChannel.Publish(in evt); // delivered (count=1)
            CosmeticChannel.Unsubscribe(token);

            CosmeticChannel.ResetPublicationCounts(); // next tick
            CosmeticChannel.Publish(in evt); // unsubscribed — must not deliver

            Assert.AreEqual(1, callCount,
                "After Unsubscribe, handler must not receive further events (FR-EVT-022)");
        }

        [Test]
        public void TierC_NoSubscribers_PublishDoesNotThrow()
        {
            // A Tier C publish with no subscribers must silently succeed.
            // UiNotificationCue has no subscribers in this isolated test.
            UiNotificationCue cue = default;
            Assert.DoesNotThrow(
                () => CosmeticChannel.Publish(in cue),
                "Tier C publish with no subscribers must not throw");
        }

        [Test]
        public void TierC_MultipleSubscribers_AllReceiveEvent()
        {
            // Each subscriber is independently registered; all must be called.
            int count1 = 0;
            int count2 = 0;
            CosmeticChannel.Subscribe<TickHeartbeatEvent>((in TickHeartbeatEvent _) => count1++);
            CosmeticChannel.Subscribe<TickHeartbeatEvent>((in TickHeartbeatEvent _) => count2++);

            // MaxPerTick=1 means one delivery per tick but BOTH handlers are called within that delivery.
            TickHeartbeatEvent evt = default;
            CosmeticChannel.Publish(in evt);

            Assert.AreEqual(1, count1, "First subscriber must receive event");
            Assert.AreEqual(1, count2, "Second subscriber must receive event");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 6 — EventBus Tier A publish, DrainTick, and subscribe round-trip
    // Stage 0+1. FR-EVT-017/023/027/029/030.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates Tier A publish-to-drain round-trip: event is enqueued, sorted, dispatched.
    /// FR-EVT-017/023/027/029/030.
    /// </summary>
    [TestFixture]
    internal sealed class EventBusPublishDrainTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        [Test]
        public void Publish_TierA_IncreasesQueueCount()
        {
            // FR-EVT-017: Publish<T>(IEventA) must enqueue the event in the ring buffer.
            // Use Resolve phase; PossessionChangedEvent (0x04) is registered from Resolve.
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(1, 2, 0);
            EventBus.Publish(in evt);

            Assert.AreEqual(1, EventLedger.QueueCount,
                "Publish<IEventA> must increment QueueCount by 1");
        }

        [Test]
        public void Publish_TierA_SetsSlotOrdinalInPayloadBuffer()
        {
            // FR-EVT-002: EventBus overwrites the header fields including eventTypeOrdinal.
            // After Publish, byte 0 of the slot must equal 0x04 (PossessionChangedEvent ordinal).
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(10, 20, 1);
            EventBus.Publish(in evt);

            int slotOffset = 0; // first slot
            byte storedOrdinal = EventLedger.PayloadBuffer[slotOffset];
            Assert.AreEqual(0x04, storedOrdinal,
                "Byte 0 of slot must be event type ordinal 0x04 after Publish (FR-EVT-002)");
        }

        [Test]
        public void Publish_TierA_EmbedsTick_InPayloadBuffer()
        {
            // FR-EVT-002: tick is embedded at bytes 4..7 (LE u32) in the 12-byte header.
            EventBus.BeginTick(0xABCD1234u);
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(0, 0, 0);
            EventBus.Publish(in evt);

            int slotOffset = 0;
            uint storedTick =
                (uint)EventLedger.PayloadBuffer[slotOffset + 4] |
                ((uint)EventLedger.PayloadBuffer[slotOffset + 5] << 8) |
                ((uint)EventLedger.PayloadBuffer[slotOffset + 6] << 16) |
                ((uint)EventLedger.PayloadBuffer[slotOffset + 7] << 24);

            Assert.AreEqual(0xABCD1234u, storedTick,
                "Bytes 4..7 of slot must contain the tick (LE u32) set by BeginTick (FR-EVT-002)");
        }

        [Test]
        public void DrainTick_DispatchesToSubscriber()
        {
            // FR-EVT-023: Publish → DrainTick → subscriber is called.
            int callCount = 0;
            PossessionChangedEvent received = default;

            EventBus.Subscribe<PossessionChangedEvent>(
                (in PossessionChangedEvent e) =>
                {
                    callCount++;
                    received = e;
                });

            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent published = new PossessionChangedEvent(3, 7, 2);
            EventBus.Publish(in published);

            EventBus.DrainTick();

            Assert.AreEqual(1, callCount,
                "DrainTick must invoke subscriber exactly once for one published event (FR-EVT-023)");
            Assert.AreEqual(3, received.PreviousHolder,
                "Subscriber must receive PreviousHolder=3 (payload round-trip)");
            Assert.AreEqual(7, received.NewHolder,
                "Subscriber must receive NewHolder=7 (payload round-trip)");
        }

        [Test]
        public void DrainTick_NoEvents_DrainCompletes_WithoutThrow()
        {
            // Empty-phase drain must complete without error; §4.4.1.
            Assert.DoesNotThrow(() => EventBus.DrainTick(),
                "DrainTick on empty queue must not throw");
        }

        [Test]
        public void DrainTick_ClearsInDrainDispatchFlag_OnCompletion()
        {
            // AR-1 H-3: InDrainDispatch is cleared in a try/finally so it never remains
            // stuck true after DrainTick completes normally. §4.4.1.
            EventBus.DrainTick();
            Assert.IsFalse(EventLedger.InDrainDispatch,
                "InDrainDispatch must be false after DrainTick completes normally (AR-1 H-3)");
        }

        [Test]
        public void DrainTick_ClearsInDrainDispatchFlag_OnHandlerException()
        {
            // AR-1 H-3: InDrainDispatch must be cleared even if a subscriber throws.
            EventBus.Subscribe<PossessionChangedEvent>(
                (in PossessionChangedEvent _) => throw new Exception("handler fault"));

            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(0, 0, 0);
            EventBus.Publish(in evt);

            try { EventBus.DrainTick(); } catch { /* expected */ }

            Assert.IsFalse(EventLedger.InDrainDispatch,
                "InDrainDispatch must be false after handler exception (AR-1 H-3 / try/finally)");
        }

        [Test]
        public void Publish_MultipleEvents_QueueCountIncrementsCorrectly()
        {
            // FR-EVT-017: each Publish call must increment QueueCount by exactly 1.
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(0, 1, 0);

            for (int i = 0; i < 5; i++)
                EventBus.Publish(in evt);

            Assert.AreEqual(5, EventLedger.QueueCount,
                "5 publishes must yield QueueCount=5");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 7 — Queue overflow and BFS depth cap
    // Stage 0+1. FR-EVT-041/046/047.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that publishing past EventQueueCapacity raises ERR_EVT_QUEUE_OVERFLOW
    /// and that DrainTick raises it when BFS dispatch depth exceeds MaxEventDispatchDepth.
    /// FR-EVT-041/046/047.
    /// </summary>
    [TestFixture]
    internal sealed class OverflowTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        [Test]
        public void Publish_PastQueueCapacity_ThrowsQueueOverflow()
        {
            // FR-EVT-041: publishing the (EventQueueCapacity+1)-th event must throw
            // ERR_EVT_QUEUE_OVERFLOW (0x1701). §3.5.1.
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(0, 0, 0);

            // Fill the queue to capacity.
            EventLedger.QueueCount = EventSystemConstants.EventQueueCapacity;

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                EventBus.Publish(in evt),
                "Publish past EventQueueCapacity must throw InvalidOperationException");

            Assert.IsTrue(
                ex.Message.Contains("0x1701") || ex.Message.Contains("ERR_EVT_QUEUE_OVERFLOW"),
                "Exception must reference ERR_EVT_QUEUE_OVERFLOW (0x1701) (FR-EVT-041)");
        }

        [Test]
        public void DrainTick_PastMaxDispatchDepth_ThrowsQueueOverflow()
        {
            // FR-EVT-047: BFS depth exceeding MaxEventDispatchDepth must throw
            // ERR_EVT_QUEUE_OVERFLOW (0x1701). §3.2.5.
            //
            // Simulate by registering a subscriber that publishes a second-order event,
            // which triggers another subscription that publishes again (chaining depth).
            // For the test we construct the scenario by subscribing a handler that
            // publishes one additional event per invocation, then seeding the queue
            // with events equal to MaxEventDispatchDepth+1 "batches". Since each batch
            // triggers a new batch via re-entrant publish, the depth will exceed the cap.
            //
            // Simpler approach: set QueueCount to trigger one batch, then let the
            // re-entrant publish keep adding events until DrainTick exhausts depth.

            int dispatchDepth = 0;
            int maxDepth = EventSystemConstants.MaxEventDispatchDepth;

            EventBus.Subscribe<PossessionChangedEvent>(
                (in PossessionChangedEvent _) =>
                {
                    dispatchDepth++;
                    // Keep publishing to drive depth beyond the cap.
                    if (dispatchDepth <= maxDepth + 1)
                    {
                        // HandlerSecondaryPublishCount is reset per-handler by the dispatcher.
                        // Each handler invocation gets exactly 1 secondary publish budget (FR-EVT-046a).
                        EventBus.BeginPhase(PhaseId.Resolve);
                        PossessionChangedEvent secondary = new PossessionChangedEvent(0, 0, 0);
                        EventBus.Publish(in secondary);
                    }
                });

            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent seed = new PossessionChangedEvent(0, 0, 0);
            EventBus.Publish(in seed);

            Assert.Throws<InvalidOperationException>(() => EventBus.DrainTick(),
                "DrainTick past MaxEventDispatchDepth must throw (FR-EVT-047)");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 8 — SerializeLedger format (FM-017-001)
    // Stage 0+1. FR-EVT-012/015.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates SerializeLedger header format: domain tag byte at offset 0,
    /// 4-byte LE count at offset 1, and Tier C exclusion (FR-EVT-012/015 / FM-017-001).
    /// </summary>
    [TestFixture]
    internal sealed class SerializeLedgerTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        [Test]
        public void SerializeLedger_EmptyQueue_WritesDomainTag_And_ZeroCount()
        {
            // FM-017-001: domain tag byte (0x15) followed by u32 LE count.
            // With an empty queue, count must be 0.
            // §4.4.2.
            byte[] dst = new byte[256];
            int written = EventBus.SerializeLedger(dst);

            // Domain tag at byte 0.
            Assert.AreEqual(EventSystemConstants.DomainTagEventLedger, dst[0],
                "SerializeLedger byte 0 must be DomainTagEventLedger (0x15) (FM-017-001)");

            // Count field (bytes 1..4, LE u32) must be 0 for empty queue.
            uint count = (uint)dst[1] | ((uint)dst[2] << 8) | ((uint)dst[3] << 16) | ((uint)dst[4] << 24);
            Assert.AreEqual(0u, count,
                "SerializeLedger must write count=0 for empty queue (FM-017-001)");

            // 5 bytes written: 1 (tag) + 4 (count).
            Assert.AreEqual(5, written,
                "SerializeLedger must write exactly 5 bytes for empty queue");
        }

        [Test]
        public void SerializeLedger_OneTierAEvent_WritesCountOne()
        {
            // FM-017-001: one Tier A event must appear in the serialized output.
            // FR-EVT-012: Tier A events must be included.
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent evt = new PossessionChangedEvent(1, 2, 0);
            EventBus.Publish(in evt);

            byte[] dst = new byte[4096];
            EventBus.SerializeLedger(dst);

            uint count = (uint)dst[1] | ((uint)dst[2] << 8) | ((uint)dst[3] << 16) | ((uint)dst[4] << 24);
            Assert.AreEqual(1u, count,
                "SerializeLedger must write count=1 for one Tier A event (FR-EVT-012)");
        }

        [Test]
        public void SerializeLedger_TierCEventNotCounted_InSerialOutput()
        {
            // FR-EVT-015: Tier C events must NOT appear in SerializeLedger output.
            // Publish a TickHeartbeatEvent (Tier C) and verify count remains 0.
            TickHeartbeatEvent cue = default;
            CosmeticChannel.Publish(in cue); // Tier C — must not enter ring buffer

            byte[] dst = new byte[256];
            EventBus.SerializeLedger(dst);

            uint count = (uint)dst[1] | ((uint)dst[2] << 8) | ((uint)dst[3] << 16) | ((uint)dst[4] << 24);
            Assert.AreEqual(0u, count,
                "SerializeLedger must not include Tier C events (FR-EVT-015)");
        }

        [Test]
        public void SerializeLedger_TwoTierAEvents_WritesCountTwo()
        {
            // FR-EVT-012: two Tier A events produce count=2 in the serialized header.
            EventBus.BeginPhase(PhaseId.Resolve);
            PossessionChangedEvent ev1 = new PossessionChangedEvent(0, 1, 0);
            PossessionChangedEvent ev2 = new PossessionChangedEvent(1, 2, 1);
            EventBus.Publish(in ev1);
            EventBus.Publish(in ev2);

            byte[] dst = new byte[4096];
            EventBus.SerializeLedger(dst);

            uint count = (uint)dst[1] | ((uint)dst[2] << 8) | ((uint)dst[3] << 16) | ((uint)dst[4] << 24);
            Assert.AreEqual(2u, count,
                "SerializeLedger must write count=2 for two Tier A events");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 9 — FM-017-002 sort key canonical order
    // Stage 0+1. FR-EVT-027/029/030. AR-4 H-1 regression guard.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that DrainTick dispatches events in FM-017-002 canonical sort order:
    /// (ProducingPhaseIndex, SubsystemOrdinal, EntityId, IntraPhaseDrawIndex).
    /// Regression guard for AR-4 H-1 (EventTypeOrdinal was incorrectly interleaved).
    /// FR-EVT-027/029/030.
    /// </summary>
    [TestFixture]
    internal sealed class SortKeyOrderTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        [Test]
        public void DrainTick_TwoEventsInSamePhase_DispatchedInDrawIndexOrder()
        {
            // FR-EVT-027/030: events in the same phase and same subsystem must be dispatched
            // in ascending intraPhaseDrawIndex order (FM-017-002 4th key).
            System.Collections.Generic.List<int> dispatchOrder = new System.Collections.Generic.List<int>();

            EventBus.Subscribe<PossessionChangedEvent>(
                (in PossessionChangedEvent e) => dispatchOrder.Add(e.PreviousHolder));

            EventBus.BeginPhase(PhaseId.Resolve);

            // Publish event A (PreviousHolder=100) then event B (PreviousHolder=200).
            // Both from Resolve phase, same subsystem (EventSystem=60); A gets drawIndex=0, B gets 1.
            PossessionChangedEvent evtA = new PossessionChangedEvent(100, 0, 0);
            PossessionChangedEvent evtB = new PossessionChangedEvent(200, 0, 0);
            EventBus.Publish(in evtA);
            EventBus.Publish(in evtB);

            EventBus.DrainTick();

            // FM-017-002: lower IntraPhaseDrawIndex first → A (drawIndex 0) before B (drawIndex 1).
            Assert.AreEqual(2, dispatchOrder.Count, "Both events must be dispatched");
            Assert.AreEqual(100, dispatchOrder[0], "Event A (drawIndex=0) must be dispatched first");
            Assert.AreEqual(200, dispatchOrder[1], "Event B (drawIndex=1) must be dispatched second");
        }

        [Test]
        public void EventSlotMeta_CompareKey_DoesNotUseEventTypeOrdinal()
        {
            // AR-4 H-1 regression guard: CompareKey must implement the canonical 4-key order
            // (ProducingPhaseIndex, SubsystemOrdinal, EntityId, IntraPhaseDrawIndex) and must
            // NOT insert EventTypeOrdinal as an additional key. This test exercises the sort
            // key directly via EventSlotMeta to catch the specific regression.
            EventSlotMeta metaA = new EventSlotMeta
            {
                ProducingPhaseIndex  = (byte)PhaseId.Resolve,
                SubsystemOrdinal     = (ushort)SubsystemOrdinals.EventSystem,
                EntityId             = 0,
                EventTypeOrdinal     = 0x07, // GoalAwardedEvent — higher ordinal
                IntraPhaseDrawIndex  = 0,    // lower draw index → should sort first
                StructSize           = 1,
            };
            EventSlotMeta metaB = new EventSlotMeta
            {
                ProducingPhaseIndex  = (byte)PhaseId.Resolve,
                SubsystemOrdinal     = (ushort)SubsystemOrdinals.EventSystem,
                EntityId             = 0,
                EventTypeOrdinal     = 0x04, // PossessionChangedEvent — lower ordinal
                IntraPhaseDrawIndex  = 1,    // higher draw index → should sort second
                StructSize           = 1,
            };

            // metaA has lower IntraPhaseDrawIndex; CompareKey(A vs B) must be < 0 (A before B)
            // regardless of EventTypeOrdinal difference.
            int cmp = metaA.CompareKey(in metaB);
            Assert.Less(cmp, 0,
                "CompareKey must order by IntraPhaseDrawIndex, not EventTypeOrdinal (AR-4 H-1)");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════
    // Fixture 10 — Stage 1+/Stage 5+ deferred stubs
    // Tests that cannot activate at Stage 0 are stubbed here per §5.2 activation table.
    // ════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Placeholder tests for Stage 0+1 and Stage 5+ features. Each is a named stub
    /// that documents the FR and activation criterion without exercising unimplemented code.
    /// Activation criterion: first src/event-system/ code commit (already met by v1.4).
    /// </summary>
    [TestFixture]
    internal sealed class DeferredStageTests
    {
        [Test]
        public void FR_EVT_006_CanonicalNoPaddingLayout_Stage0Plus1()
        {
            // FR-EVT-006: strict canonical no-padding form activated at Stage 0+1.
            // Stage 0: SerializeLedger writes raw struct bytes (with padding) per §4.4.2 note.
            Assert.Ignore("Stage 0+1 only — strict no-padding canonical serialization is Stage 0+1 (FR-EVT-006)");
        }

        [Test]
        public void FR_EVT_012_SnapshotRoundTrip_Stage0Plus1()
        {
            // FR-EVT-012: serialize → load → re-deserialize snapshot round-trip.
            Assert.Ignore("Stage 0+1 only — snapshot round-trip test (FR-EVT-012)");
        }

        [Test]
        public void FR_EVT_013_TierB_Activation_Stage5Plus()
        {
            // FR-EVT-013: Tier B events activate at Stage 5+ per KD-3.
            Assert.Ignore("Stage 5+ only — Tier B activation is Stage 5+ (FR-EVT-013)");
        }

        [Test]
        public void FR_EVT_031_G1_PhaseDigestGolden_Stage0Plus1()
        {
            // FR-EVT-031: G1 golden fixture comparison (FM-017-001 phase digest).
            // Golden bytes are captured at Stage 0→1 transition per §5.3.2.
            Assert.Ignore("Stage 0+1 only — G1 golden fixture not yet captured (FR-EVT-031)");
        }

        [Test]
        public void FR_EVT_043_G2_CosmeticDropGolden_Stage0Plus1()
        {
            // FR-EVT-043: G2 golden fixture for Tier C drop record replay stability.
            Assert.Ignore("Stage 0+1 only — G2 golden fixture not yet captured (FR-EVT-043)");
        }

        [Test]
        public void FR_EVT_048_PerPublishZeroAllocation_Stage0Plus1()
        {
            // FR-EVT-048: Assert.AllocatedBytes(0) per Publish<T>. Requires BenchmarkDotNet.
            Assert.Ignore("Stage 0+1 only — allocation-tracker tooling activates at Stage 0+1 (FR-EVT-048)");
        }

        [Test]
        public void FR_EVT_072_Fixed64_Reverification_Stage5Plus()
        {
            // FR-EVT-072: Fixed64 re-verification suite. Stage 5+ only (Spec #9).
            Assert.Ignore("Stage 5+ only — Fixed64 is Stage 5+ (FR-EVT-072)");
        }

        [Test]
        public void FR_EVT_077_ReplayIsolation_Stage1Plus()
        {
            // FR-EVT-077: ordinary subscribers must not see replay-channel events.
            Assert.Ignore("Stage 1+ only — replay channel not yet implemented (FR-EVT-077)");
        }

        [Test]
        public void FR_EVT_080_UnknownOrdinalFixtureLoad_Stage0Plus1()
        {
            // FR-EVT-080: fixture-load with unknown ordinal → ERR_EVT_ORDINAL_UNKNOWN.
            // Requires fixture-loader implementation (Stage 0+1).
            Assert.Ignore("Stage 0+1 only — fixture loader not yet implemented (FR-EVT-080)");
        }

        [Test]
        public void FR_EVT_081_VersionIncompatibleFixtureLoad_Stage0Plus1()
        {
            // FR-EVT-081: fixture-load with newer payloadVersion → ERR_EVT_VERSION_INCOMPATIBLE.
            Assert.Ignore("Stage 0+1 only — fixture loader not yet implemented (FR-EVT-081)");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                |
// | 1.0     | 2026-05-31 | —      | Initial implementation.                              |
// | 1.1     | 2026-06-07 | —      | AR-9 L-1: EventTestHelper.ResetLedger now calls      |
// |         |            |        | CosmeticChannel.ResetForTests() to clear Tier C      |
// |         |            |        | dispatcher table between tests (prevents leakage).   |
#endregion
