from __future__ import annotations

import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "parse_foul_card_characterization.py"
EVIDENCE = ROOT / "docs" / "tracking" / "evidence" / "foul-card-six-seed"
REPORT = EVIDENCE / "report.txt"


def _load_parser():
    spec = importlib.util.spec_from_file_location("parse_foul_card_characterization", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class FoulCardCharacterizationParserTests(unittest.TestCase):
    def setUp(self) -> None:
        self.parser = _load_parser()
        self.report = REPORT.read_text(encoding="utf-8")

    def _with_w3_census(self) -> str:
        extended = self.report
        per_seed = (
            "  W3 agentBallFanoutEvents=10 claimEligibilityEpisodes=2 "
            "registeredDuelParticipants=3 resolvedHandContactDuels=1 successfulKeeperClaims=1\n"
            "  aerialDelivery crossAttempts=4 crossCompletions=1 "
            "loftedAttempts=5 loftedCompletions=2 headerAttempts=6 headerContacts=4\n"
        )
        for seed in self.parser.FROZEN_SEEDS:
            marker = f"seed {seed}:\n"
            self.assertIn(marker, extended)
            extended = extended.replace(marker, marker + per_seed, 1)

        rate_line = (
            "SHIPPED per-90-min rates: fouls=8.33 yellows=0.83 straightReds=0.167 "
            "secondYellowDismissals=0.000 totalDismissals=0.167\n"
        )
        aggregate_w3 = (
            "\n--- aggregate #435 §6.2 W3 before/after census ---\n"
            "agentBallFanoutEvents=60 claimEligibilityEpisodes=12 "
            "registeredDuelParticipants=18 resolvedHandContactDuels=6 successfulKeeperClaims=6\n"
            "crossAttempts=24 crossCompletions=6 loftedAttempts=30 loftedCompletions=12 "
            "headerAttempts=36 headerContacts=24\n"
        )
        self.assertIn(rate_line, extended)
        return extended.replace(rate_line, rate_line + aggregate_w3, 1)

    def test_committed_report_reproduces_structured_evidence(self) -> None:
        parsed = self.parser.parse_report(self.report)
        with tempfile.TemporaryDirectory() as tmp:
            tsv = Path(tmp) / "results.tsv"
            self.parser.write_tsv(parsed, tsv)
            self.assertEqual(
                (EVIDENCE / "results.tsv").read_text(encoding="utf-8"),
                tsv.read_text(encoding="utf-8"),
            )
        self.assertEqual(
            json.loads((EVIDENCE / "results.json").read_text(encoding="utf-8")),
            parsed,
        )

    def test_rejects_replaced_or_duplicate_frozen_seed(self) -> None:
        corrupted = self.report.replace(
            "seed 0x00000000D1A6D05E:",
            "seed 0x0F1E2D3C4B5A6978:",
            1,
        )
        with self.assertRaisesRegex(ValueError, "seed corpus/order"):
            self.parser.parse_report(corrupted)

    def test_rejects_reordered_frozen_seed_corpus(self) -> None:
        first = "seed 0x0F1E2D3C4B5A6978:"
        second = "seed 0x00000000D1A6D05E:"
        corrupted = self.report.replace(first, "seed __SWAP__:", 1)
        corrupted = corrupted.replace(second, first, 1)
        corrupted = corrupted.replace("seed __SWAP__:", second, 1)
        with self.assertRaisesRegex(ValueError, "seed corpus/order"):
            self.parser.parse_report(corrupted)

    def test_rejects_aggregate_counter_not_equal_to_seed_sum(self) -> None:
        corrupted = self.report.replace(
            "totalDismissals=1 playedTicks=1943994 agentAgentContacts=22083",
            "totalDismissals=1 playedTicks=1943995 agentAgentContacts=22083",
            1,
        )
        with self.assertRaisesRegex(ValueError, "playedTicks=.*does not equal seed sum"):
            self.parser.parse_report(corrupted)

    def test_requires_shipped_per90_rate_record(self) -> None:
        rate_line = (
            "SHIPPED per-90-min rates: fouls=8.33 yellows=0.83 straightReds=0.167 "
            "secondYellowDismissals=0.000 totalDismissals=0.167\n"
        )
        self.assertIn(rate_line, self.report)
        corrupted = self.report.replace(rate_line, "", 1)
        with self.assertRaisesRegex(ValueError, "missing shipped per-90 rates"):
            self.parser.parse_report(corrupted)

    def test_rejects_priced_candidate_partition_mismatch(self) -> None:
        corrupted = self.report.replace(
            "fromBehindCalled=4 fromBehindWavedOn=135",
            "fromBehindCalled=4 fromBehindWavedOn=136",
            1,
        )
        corrupted = corrupted.replace(
            "fromBehindCalled=42 fromBehindWavedOn=703",
            "fromBehindCalled=42 fromBehindWavedOn=704",
            1,
        )
        with self.assertRaisesRegex(ValueError, "priced-candidate partition"):
            self.parser.parse_report(corrupted)

    def test_rejects_rate_that_disagrees_with_aggregate_count(self) -> None:
        corrupted = self.report.replace("fouls=8.33", "fouls=99.00", 1)
        with self.assertRaisesRegex(ValueError, "shipped rate foulsPer90"):
            self.parser.parse_report(corrupted)

    def test_w3_extension_parses_and_adds_tsv_columns(self) -> None:
        parsed = self.parser.parse_report(self._with_w3_census())
        aggregate = parsed["aggregate"]
        self.assertEqual(60, aggregate["agentBallFanoutEvents"])
        self.assertEqual(6, aggregate["resolvedHandContactDuels"])
        self.assertEqual(24, aggregate["crossAttempts"])
        self.assertEqual(24, aggregate["headerContacts"])

        with tempfile.TemporaryDirectory() as tmp:
            tsv = Path(tmp) / "results.tsv"
            self.parser.write_tsv(parsed, tsv)
            header = tsv.read_text(encoding="utf-8").splitlines()[0].split("\t")
            self.assertIn("agentBallFanoutEvents", header)
            self.assertIn("headerContacts", header)

    def test_w3_extension_requires_every_seed_field(self) -> None:
        corrupted = self._with_w3_census().replace(
            "successfulKeeperClaims=1",
            "successfulKeeperClaimsMissing=1",
            1,
        )
        with self.assertRaisesRegex(ValueError, "missing W3 fields"):
            self.parser.parse_report(corrupted)

    def test_w3_extension_rejects_aggregate_not_equal_to_seed_sum(self) -> None:
        corrupted = self._with_w3_census().replace(
            "agentBallFanoutEvents=60",
            "agentBallFanoutEvents=61",
            1,
        )
        with self.assertRaisesRegex(ValueError, "agentBallFanoutEvents=.*does not equal seed sum"):
            self.parser.parse_report(corrupted)

    def test_rejects_non_finite_rate_token(self) -> None:
        corrupted = self.report.replace("fouls=8.33", "fouls=nan", 1)
        with self.assertRaisesRegex(ValueError, "shipped rate foulsPer90"):
            self.parser.parse_report(corrupted)


if __name__ == "__main__":
    unittest.main()
