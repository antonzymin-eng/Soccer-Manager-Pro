from __future__ import annotations

from pathlib import Path
import os
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[2]


class PrecommitTaxonomyFilterTests(unittest.TestCase):
    def test_precommit_excludes_canonical_integration_prefix(self) -> None:
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
        self.assertIn("FullyQualifiedName!~int_", proc.stdout)
        self.assertIn("FullyQualifiedName!~sim_", proc.stdout)
        self.assertIn("FullyQualifiedName!~e2e_", proc.stdout)


if __name__ == "__main__":
    unittest.main()
