// File:     src/event-system/tests/EventBusWiringSmokeTests.cs
// Created:  2026-06-08
// Modified: 2026-06-08 (AR-2 fix pass)
// Author:   —
// Spec:     Event System #17 §4.4, Deterministic Simulation #16 §5, Code Standards #20
// Purpose:  Boot-time wiring smoke test for the EventBusRegistrar chain across specs.
//           Drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts
//           the SHA-256 digest of the serialized Tier A/B ledger is stable across runs.
//           Catches silent regressions in ordinal allocation, version byte, producer-phase
//           wiring, subsystem-ordinal embedding, struct-size registration, and canonical
//           byte layout before they reach a running Unity process. Stage 0 covers the 6
//           currently-wired registrars; Agent Movement (#2) is a [CROSS-PENDING] slot
//           pending the AM-side EventBusRegistrar (then 7 total).

using System;
using System.Security.Cryptography;
using System.Text;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.HeadingMechanics;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

// Type aliases (grouped at the end of the project-group usings per C# convention).
// Each spec exports an EventBusRegistrar from its own namespace; the aliases
// disambiguate the 6 sibling classes at the call sites in BOOT.
using PassRegistrar       = TacticalDirector.PassMechanics.EventBusRegistrar;
using ShotRegistrar       = TacticalDirector.ShotMechanics.EventBusRegistrar;
using PerceptionRegistrar = TacticalDirector.PerceptionSystem.EventBusRegistrar;
using DecisionRegistrar   = TacticalDirector.DecisionTree.EventBusRegistrar;
using HeadingRegistrar    = TacticalDirector.HeadingMechanics.EventBusRegistrar;
using GkRegistrar         = TacticalDirector.GoalkeeperMechanics.EventBusRegistrar;

namespace TacticalDirector.EventSystem.Tests
{
    /// <summary>
    /// SMOKE-EVT-WIRING-001: end-to-end boot wiring across all registered specs.
    /// Exercises every EventBusRegistrar.Initialize() call site, publishes one Tier A/B
    /// event per spec from the correct producer phase, drains the ledger, serializes it,
    /// and pins the SHA-256 of the serialized bytes as a golden value.
    ///
    /// Stage 0 surface (6 currently-wired registrars; 7 once AM #2 lands):
    ///   - PassMechanics       (0x0C, 0x0D — Tier A — Resolve)
    ///   - ShotMechanics       (0x01, 0x0E — Tier A — Resolve)
    ///   - HeadingMechanics    (0x12       — Tier B — Physics)
    ///   - GoalkeeperMechanics (0x14, 0x15 — Tier A — Physics; 0x16 — Tier A — Resolve)
    ///   - PerceptionSystem    (0x10 Tier C — registry row updated by Initialize(); the
    ///                          CosmeticChannel routing path is NOT exercised here)
    ///   - DecisionTree        (0x11 Tier C — registry row updated by Initialize(); the
    ///                          CosmeticChannel routing path is NOT exercised here)
    ///
    /// [CROSS-PENDING] Agent Movement (#2) — once AM lands its own EventBusRegistrar,
    ///   add its Initialize() call below at the BOOT step and append its Tier A
    ///   producer-phase publish at the matching BeginPhase block. Then re-pin the
    ///   golden digest in one commit alongside the AM-side change.
    /// </summary>
    [TestFixture]
    internal sealed class EventBusWiringSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            EventTestHelper.ResetLedger();
        }

        /// <summary>
        /// SMOKE-EVT-WIRING-001: boot → publish-one-per-spec → drain → digest.
        /// Asserts the serialized ledger is non-empty and SHA-256 matches the pinned
        /// golden value. A mismatch means an ordinal allocation, producer phase, subsystem
        /// ordinal, struct size, or canonical-byte-order regression has occurred in one
        /// of the registered specs since the digest was pinned.
        /// </summary>
        [Test]
        public void Boot_PublishOnePerSpec_DrainTick_SerializeLedger_DigestStable()
        {
            // ── BOOT ─────────────────────────────────────────────────────────────────
            // Each Initialize() is idempotent (registrars carry an s_registered guard),
            // so re-running tests in the same process is safe.
            PassRegistrar.Initialize();
            ShotRegistrar.Initialize();
            PerceptionRegistrar.Initialize();
            DecisionRegistrar.Initialize();
            HeadingRegistrar.Initialize();
            GkRegistrar.Initialize();
            // TODO [CROSS-PENDING #2]: AgentMovementRegistrar.Initialize();

            // No BootPhaseComplete=true here: EventBus.Publish (Tier A/B) does not check
            // the flag — only Subscribe does, and this smoke test does not subscribe.
            // EventLedger.DrainTick will auto-set the flag on first call (EventLedger.cs).

            // ── TICK 42: PUBLISH ────────────────────────────────────────────────────
            // Fixed tick chosen so the embedded header bytes (offset 4..7) are
            // reproducible across runs. Events are default-constructed: EventBus
            // overwrites the 12-byte header at publish time, so the only struct content
            // contributing to the digest is the per-event payload bytes — which are all
            // zero from default(T). This intentionally keeps the digest a function of
            // registration metadata + publish order + canonical byte layout only,
            // isolating the wiring chain from event-payload semantics.
            const uint TestTick = 42u;
            EventBus.BeginTick(TestTick);

            // Physics-phase producers: Goalkeeper Save/BallClaimed (Tier A) +
            // Heading HeaderExecuted (Tier B).
            EventBus.BeginPhase(PhaseId.Physics);
            EventBus.Publish<SaveAttemptedEvent>(default);
            EventBus.Publish<BallClaimedEvent>(default);
            EventBus.Publish<HeaderExecutedEvent>(default);

            // Resolve-phase producers: Pass Attempt/Cancelled, Shot Executed/Cancelled,
            // Goalkeeper Distribution.
            EventBus.BeginPhase(PhaseId.Resolve);
            EventBus.Publish<PassAttemptEvent>(default);
            EventBus.Publish<PassCancelledEvent>(default);
            EventBus.Publish<ShotExecutedEvent>(default);
            EventBus.Publish<ShotCancelledEvent>(default);
            EventBus.Publish<DistributionExecutedEvent>(default);

            // TODO [CROSS-PENDING #2]: insert AM Tier A publishes here under the
            // appropriate BeginPhase block once AM's registrar lands.

            // ── DRAIN ────────────────────────────────────────────────────────────────
            EventBus.BeginPhase(PhaseId.Events);
            EventBus.DrainTick();

            // ── SERIALIZE ────────────────────────────────────────────────────────────
            // Buffer sized to the worst-case ledger footprint: fixed 5-byte header +
            // queue-capacity × per-slot bytes. Heap allocation is fine in tests
            // (FR-EVT-050 zero-alloc applies to the production SerializeLedger path,
            // not to the caller-supplied destination buffer).
            EventBus.BeginPhase(PhaseId.Snapshot);
            byte[] buf = new byte[
                5 + EventSystemConstants.EventQueueCapacity * EventSystemConstants.MaxEventSlotBytes];
            int n = EventBus.SerializeLedger(buf);

            Assert.GreaterOrEqual(n, 5,
                "SerializeLedger wrote fewer than 5 bytes — the FM-017-001 ledger header " +
                "(1-byte domain tag + 4-byte count) is the floor; anything less is a header-write bug.");

            // ── DIGEST ───────────────────────────────────────────────────────────────
            byte[] digest;
            using (SHA256 sha = SHA256.Create())
            {
                digest = sha.ComputeHash(buf, 0, n);
            }

            Assert.AreEqual(32, digest.Length,
                "SHA-256 must produce exactly 32 bytes — anything else means SHA256.ComputeHash " +
                "returned a wrong-shaped array (catches a degenerate test setup, not a real digest collision).");

            string actualHex = ToHex(digest);
            TestContext.Out.WriteLine(
                "[SMOKE-EVT-WIRING-001] tick={0} bytes={1} sha256={2}",
                TestTick, n, actualHex);

            // ── PIN ──────────────────────────────────────────────────────────────────
            // Mirror end-of-tick cleanup BEFORE the inconclusive throw so the canonical
            // tick lifecycle (BeginTick → BeginPhase × N → DrainTick → SerializeLedger
            // → OnTickBoundary) is fully exercised. Assert.Inconclusive throws
            // InconclusiveException, so any code after it is unreachable. AR-2 M-1 fix.
            EventBus.OnTickBoundary();

            // PIN INSTRUCTIONS (when AM #2's EventBusRegistrar lands):
            //   1. Add AgentMovementRegistrar.Initialize() at the BOOT block above.
            //   2. Add the AM Tier A publish under the matching BeginPhase block.
            //   3. Run this test once; copy the sha256=<hex> printed by TestContext.
            //   4. DELETE this entire comment block AND the Assert.Inconclusive call below
            //      (everything from "PIN INSTRUCTIONS" through the closing paren).
            //   5. REPLACE with:  Assert.AreEqual("<observed-hex>", actualHex,
            //                          "SMOKE-EVT-WIRING-001 digest regression — see registrar diffs.");
            // Inconclusive (not Pass) is used here so CI surfaces the unfilled pin instead
            // of false-positive green — AR-1 H-1 fix.
            Assert.Inconclusive(
                "Golden digest not yet pinned. Observed sha256=" + actualHex +
                " — pin atomically with the AM #2 registrar landing.");
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                 |
// | 0.1     | 2026-06-08 | —      | Initial skeleton. Boot→publish→drain→digest smoke test across the    |
// |         |            |        | 6 currently-wired registrars (Pass / Shot / Perception / Decision /  |
// |         |            |        | Heading / Goalkeeper). Agent Movement (#2) carved out as             |
// |         |            |        | [CROSS-PENDING]; golden digest pinned on the same commit that lands  |
// |         |            |        | the AM-side EventBusRegistrar.                                       |
// | 0.2     | 2026-06-08 | —      | AR-1 fix pass: 1H + 1M + 4L. H-1: tautological Assert.That on        |
// |         |            |        | placeholder const replaced with Assert.Inconclusive carrying the     |
// |         |            |        | observed hex — the prior compare-constant-to-its-own-literal always  |
// |         |            |        | passed regardless of actualHex, so the skeleton was silently green-  |
// |         |            |        | lighting any digest. Inconclusive surfaces the unfilled pin to CI.   |
// |         |            |        | M-1: removed dead EventLedger.BootPhaseComplete=true assignment —    |
// |         |            |        | Publish (Tier A/B) does not check the flag (only Subscribe does);    |
// |         |            |        | DrainTick auto-sets it on first call anyway. L-1: registrar count   |
// |         |            |        | corrected (6 currently wired, 7 once AM #2 lands; previously said   |
// |         |            |        | "5 of 6" while listing 6). L-2: Tier C entries reworded — only the  |
// |         |            |        | registry-row update is exercised; CosmeticChannel routing is not.   |
// |         |            |        | L-3: dropped redundant [TearDown] (SetUp already resets at start of |
// |         |            |        | next test; no parallel fixtures in this assembly). L-4: regression- |
// |         |            |        | surface enumeration in the file header + fixture XML doc now lists  |
// |         |            |        | `version` byte (header offset 1) alongside ordinal / phase / subsys |
// |         |            |        | / struct-size / canonical byte layout.                              |
// | 0.3     | 2026-06-08 | —      | AR-2 fix pass: 0H + 1M + 4L. M-1: OnTickBoundary() was unreachable  |
// |         |            |        | after Assert.Inconclusive (NUnit InconclusiveException). Moved      |
// |         |            |        | before the inconclusive so the canonical tick lifecycle (BeginTick |
// |         |            |        | → BeginPhase × N → DrainTick → SerializeLedger → OnTickBoundary)   |
// |         |            |        | is fully exercised. L-1: replaced IsAllZero defensive helper with  |
// |         |            |        | Assert.AreEqual(32, digest.Length) — SHA-256 producing all-zero   |
// |         |            |        | output is cryptographically infeasible; length check is the real   |
// |         |            |        | structural assertion. L-2: tightened Assert.Greater(n, 0) →        |
// |         |            |        | Assert.GreaterOrEqual(n, 5) — FM-017-001 ledger header is 5 bytes  |
// |         |            |        | minimum; anything less catches a header-write bug the > 0 check    |
// |         |            |        | missed. L-3: regrouped using-aliases at the end of the project     |
// |         |            |        | block per C# convention (regular namespace imports first, then     |
// |         |            |        | type aliases). L-4: pin instructions rewritten — explicit numbered |
// |         |            |        | steps including the exact line range to delete; "delete the        |
// |         |            |        | Inconclusive line" was ambiguous for a 3-line call + preceding     |
// |         |            |        | comment block.                                                     |
#endregion
