Created October 15, 2026. Purpose: retain unsuccessful development validation.

The first handover replay fixture captured the last committed Snapshot before its staged claim, so its continuation compared different worlds. The genuine-claim cancellation assertions passed; the fixture now commits the handover tick before capturing the canonical continuation. No production change was made to satisfy that replay-fixture correction.

The early tooling sweep registered the new evidence directory before staging it. After staging, the integrity verifier passed; its unit test then exposed the expected directory count needing 17 → 18. Both were corrected before final verification. These attempts are not counted as passing final checks.

The initial replacement common-run command used a mistyped filter and selected zero tests. It was detected immediately from discovery output and restarted with the canonical catalog filter before interpreting any outcome. The stale zero-test TRX is retained here and is not measurement evidence. Completed arms must pass the catalog report / successful-test verifier.
