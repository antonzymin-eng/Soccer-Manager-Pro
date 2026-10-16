# Keeper-claim observation correction

Created October 15, 2026. Purpose: preserve the full-suite failure, exact lifecycle witness and test-only correction.

The e54335b0 production sweep reports one detached-while-held observation in the configured-squad two-seed keeper-claim scenario. A fail-fast test-only trace isolates seed 5EED000000000003, frame 30457: the away keeper (11) is Recovering, the ball is Stationary at the keeper position, and an explicit FreeKick restart has ended the hand episode. The same keeper is designated as taker, so identity alone does not delimit the old held-claim window. This is not an independently travelling held ball.

The correction records explicit restart epochs through the existing neutral W8 observer. An older claim window closes on restart even if the ID is retained, and a claim ending before a same-tick restart cannot open a new held window. It does not skip a state merely because attachment is bad. The unchanged attachment predicate still rejects a deliberately detached ball without any restart. The 0.001 m tolerance, 300-frame window, arrest-speed predicate, non-vacuity thresholds and own-goal predicate are unchanged. Score attribution still precedes window closure, so a restart accompanying a real own goal cannot conceal it.

Both mirrored real-claim/restart cases fail the old identity/time window rule at the intended closure assertion. The corrected cases and original full two-seed scenario pass (3 tests). These as-validated sources are test overlays on production-identical e54335b0; no production behavior, frozen corpus, policy or tuning changed. The frozen measurement observers already close on explicit restart, so their archived outcomes remain valid.
