from __future__ import annotations

from pathlib import Path
import os
import re
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[2]


class PrecommitTaxonomyFilterTests(unittest.TestCase):
    def test_precommit_uses_anchored_nunit_method_selection(self) -> None:
        settings = (ROOT / "tools" / "dotnet-ci" / "precommit.runsettings").read_text(
            encoding="utf-8"
        )
        for prefix in ("int_", "sim_", "e2e_"):
            self.assertIn(f"method !~ '^{prefix}'", settings)

        excluded = (r"^int_", r"^sim_", r"^e2e_")
        for method in ("int_roster_loads", "sim_full_match", "e2e_new_game"):
            self.assertTrue(any(re.search(pattern, method) for pattern in excluded), method)

        # Regression for Claude review finding: these are ordinary unit-test names
        # containing the same character sequences away from position zero.
        for method in (
            "Point_ProjectsToPitch",
            "EnvironmentFingerprint_WorkerCountMismatch_ReturnsEnvMismatch",
            "Codec_TamperedFingerprint_RestoreFailsLoud",
            "MalformedInt_IsRejected",
            "QuickSim_Completes",
        ):
            self.assertFalse(any(re.search(pattern, method) for pattern in excluded), method)

    def test_policy_runner_passes_runsettings_not_bare_fqn_substrings(self) -> None:
        env = os.environ.copy()
        env["TD_PIPELINE_DRY_RUN"] = "1"
        proc = subprocess.run(
            ["bash", str(ROOT / "tools" / "run-tests-local.sh"), "--pre-commit"],
            cwd=ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )
        self.assertEqual(proc.returncode, 0, proc.stdout)
        self.assertIn("--settings", proc.stdout)
        self.assertIn("precommit.runsettings", proc.stdout)
        self.assertNotIn("FullyQualifiedName!~int_", proc.stdout)
        self.assertNotIn("FullyQualifiedName!~sim_", proc.stdout)
        self.assertNotIn("FullyQualifiedName!~e2e_", proc.stdout)


if __name__ == "__main__":
    unittest.main()
