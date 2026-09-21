from __future__ import annotations

import copy
import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "check_w12_evidence.py"


def _load_checker():
    spec = importlib.util.spec_from_file_location("check_w12_evidence", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _governed_inputs(checker):
    pre_census = checker.extract_census(
        checker._read_member(
            ROOT,
            checker.PRE_ARCHIVE,
            checker.PRE_ARCHIVE_SHA256,
            "instrument-output.txt",
        )
    )
    post_census = checker.extract_census(
        checker._read_member(
            ROOT,
            checker.POST_ARCHIVE,
            checker.POST_ARCHIVE_SHA256,
            "instrument-output.txt",
        )
    )
    pre_records, pre_scores = checker.parse_census(pre_census)
    post_records, post_scores = checker.parse_census(post_census)
    return pre_records, pre_scores, post_records, post_scores


def _census(checker):
    return json.loads((ROOT / checker.CENSUS_JSON).read_text(encoding="utf-8"))


class W12EvidenceTests(unittest.TestCase):
    def test_committed_pre_post_record_reconciles_to_raw_artifacts(self) -> None:
        completed = subprocess.run(
            [sys.executable, str(SCRIPT), "--repo", str(ROOT)],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("W12 evidence reconciliation: PASS", completed.stdout)

    def test_latest_pass_bound_rejects_impossible_transcription(self) -> None:
        checker = _load_checker()
        records = [
            {
                "seed": "0xBAD",
                "team": 0,
                "samples": 100,
                "latestPass": 81,
                "exits": {
                    "InPossession": 20,
                    "Cooldown": 0,
                    "StaleTick": 0,
                    "InvariantRejected": 80,
                },
            }
        ]
        errors = checker.validate_accounting(records)
        self.assertTrue(
            any(
                "latestPass=81 exceeds eligible upper bound 80" in error
                for error in errors
            )
        )

    def test_missing_sweep_members_fails_closed(self) -> None:
        checker = _load_checker()
        census = _census(checker)
        del census["durable_evidence"]["sweep_members"]
        errors = checker.validate_census_payload(
            ROOT, census, *_governed_inputs(checker)
        )
        self.assertTrue(
            any("sweep_members must be an object" in error for error in errors),
            errors,
        )

    def test_malformed_sweep_member_reports_error_without_traceback(self) -> None:
        checker = _load_checker()
        census = _census(checker)
        census["durable_evidence"]["sweep_members"]["report.json"] = "nope"
        errors = checker.validate_census_payload(
            ROOT, census, *_governed_inputs(checker)
        )
        self.assertTrue(
            any("sweep_members.report.json must be an object" in error for error in errors),
            errors,
        )

    def test_row_mutation_names_seed_team_and_field(self) -> None:
        checker = _load_checker()
        census = _census(checker)
        row = census["post"]["rows"][0]
        row["raw"]["WeakReceiver"] += 1
        errors = checker.validate_census_payload(
            ROOT, census, *_governed_inputs(checker)
        )
        self.assertTrue(
            any(
                "post.rows[0] 0x0F1E2D3C4B5A6978 team 0 raw.WeakReceiver" in error
                for error in errors
            ),
            errors,
        )

    def test_provenance_member_hash_mutation_is_rejected(self) -> None:
        checker = _load_checker()
        census = _census(checker)
        census["provenance"]["post"]["measurement_sha256"] = "0" * 64
        errors = checker.validate_census_payload(
            ROOT, census, *_governed_inputs(checker)
        )
        self.assertTrue(
            any("provenance.post.measurement_sha256" in error for error in errors),
            errors,
        )

    def test_unrecognized_root_key_fails_closed(self) -> None:
        checker = _load_checker()
        census = _census(checker)
        census["notes"] = "fabricated claim"
        errors = checker.validate_census_payload(
            ROOT, census, *_governed_inputs(checker)
        )
        self.assertTrue(
            any("root keys" in error and "notes" in error for error in errors),
            errors,
        )

    def test_governance_free_text_is_pinned(self) -> None:
        checker = _load_checker()
        census = _census(checker)
        census["generated_from"] = "rewritten provenance claim"
        census["salvaged_from"]["branch"] = "fabricated/branch"
        errors = checker.validate_census_payload(
            ROOT, census, *_governed_inputs(checker)
        )
        self.assertTrue(any("generated_from" in error for error in errors), errors)
        self.assertTrue(any("salvaged_from" in error for error in errors), errors)

    def test_missing_declared_consumer_fails_closed(self) -> None:
        checker = _load_checker()
        pre_records, pre_scores, _, _ = _governed_inputs(checker)
        aggregate = checker._aggregate_for_census(pre_records, pre_scores)
        with tempfile.TemporaryDirectory() as tmp:
            errors = checker._validate_prereg_baseline(Path(tmp), aggregate)
        self.assertTrue(
            any("required census consumer is missing" in error for error in errors),
            errors,
        )

    def test_prereg_locked_baseline_is_bound_to_raw_pre_aggregate(self) -> None:
        checker = _load_checker()
        pre_records, pre_scores, _, _ = _governed_inputs(checker)
        aggregate = checker._aggregate_for_census(pre_records, pre_scores)
        source = (ROOT / checker.CENSUS_CONSUMERS[0]).read_text(encoding="utf-8")
        corrupted = source.replace(
            "| InvariantRejected | 139,309 | 84.845% |",
            "| InvariantRejected | 139,308 | 84.845% |",
            1,
        )
        self.assertNotEqual(source, corrupted)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            consumer = root / checker.CENSUS_CONSUMERS[0]
            consumer.parent.mkdir(parents=True)
            consumer.write_text(corrupted, encoding="utf-8")
            errors = checker._validate_prereg_baseline(root, aggregate)
        self.assertTrue(
            any("baseline row 'InvariantRejected'" in error for error in errors),
            errors,
        )

    def test_aggregate_keeps_phase_and_future_exit_keys(self) -> None:
        checker = _load_checker()
        pre_records, pre_scores, _, _ = _governed_inputs(checker)
        records = copy.deepcopy(pre_records)
        records[0]["exits"]["StaleTick"] = 0
        aggregate = checker._aggregate_for_census(records, pre_scores)
        self.assertIn("phase", aggregate)
        self.assertIn("StaleTick", aggregate["exits"])


if __name__ == "__main__":
    unittest.main()
