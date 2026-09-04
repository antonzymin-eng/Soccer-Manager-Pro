from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
CHECKLIST = ROOT / "tools" / "checklist-auditor.py"
SCHEMA = ROOT / "tools" / "spec5-schema-auditor.py"


class TestingStrategyAuditorHardeningTests(unittest.TestCase):
    def run_auditor(self, script: Path, repo: Path, spec: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(script),
                "--root",
                str(repo / "docs" / "specs"),
                "--repo-root",
                str(repo),
                "--changed-scope",
                "--enforce-dir",
                str(spec),
            ],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            timeout=15,
        )

    def write_index(self, repo: Path, *, folder: str, status: str) -> None:
        specs = repo / "docs" / "specs"
        specs.mkdir(parents=True, exist_ok=True)
        (specs / "SPEC_INDEX.md").write_text(
            "# SPEC_INDEX.md — Canonical Specification Registry\n\n"
            "| # | Specification | Folder | Priority | Status | Approved |\n"
            "|---|---|---|---|---|---|\n"
            f"| 9 | Spec Nine | `{folder}/` | 1 | {status} | — |\n",
            encoding="utf-8",
        )

    def test_blockquoted_backticked_approved_status_is_recognized_without_index(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "> **Status:** `APPROVED` (May 15, 2026)\n\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | moon is cheese | prose only |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)

    def test_spec_index_approved_overrides_local_in_review_status(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "**Status:** IN REVIEW\n\n"
                "| Row | Claim | Evidence |\n|---|---|---|\n"
                "| 9.1 | moon is cheese | prose only |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)

    def test_checkbox_rows_are_audited_and_section_must_support_claim(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-1.md").write_text(
                "# Spec #9 — Section 1\n"
                "## 1.1 Representation\n"
                "Q32.32 representation uses int64 raw storage.\n",
                encoding="utf-8",
            )
            checklist = spec / "section-9-approval-checklist.md"
            checklist.write_text(
                "# Spec #9 — Approval Checklist\n"
                "> **Status:** `APPROVED`\n\n"
                "## 9.1 Representation\n"
                "- [x] Q32.32 representation and int64 raw storage defined. Evidence: `section-1.md` §1.1.\n",
                encoding="utf-8",
            )
            good = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(good.returncode, 0, good.stdout)

            checklist.write_text(
                "# Spec #9 — Approval Checklist\n"
                "> **Status:** `APPROVED`\n\n"
                "## 9.1 Representation\n"
                "- [x] the moon is cheese. Evidence: `section-1.md` §1.1.\n",
                encoding="utf-8",
            )
            bad = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(bad.returncode, 1, bad.stdout)
            self.assertIn("does not contain concrete text or values supporting the claim", bad.stdout)

    def test_checkbox_without_resolved_evidence_blocks_approval(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-9-approval-checklist.md").write_text(
                "# Spec #9 — Approval Checklist\n"
                "**Status:** APPROVED\n\n"
                "## 9.1 Gate\n"
                "- [ ] implementation evidence deferred\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(CHECKLIST, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("prose only", proc.stdout)

    def test_appendix_c_taxonomy_requires_determinism_and_end_to_end_rows(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5: Test Plan\n"
                "**Status:** APPROVED\n\n"
                "## 5.1 Test Count by Taxonomy Layer\n"
                "| Layer | Count | Notes |\n"
                "|---|---:|---|\n"
                "| Unit | 3 | |\n"
                "| Integration | 1 | |\n"
                "| Simulation | 1 | |\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("missing Determinism (consumed from #16 §5) row", proc.stdout)
            self.assertIn("missing End-to-end / soak row", proc.stdout)

    def test_schema_uses_canonical_registry_status(self) -> None:
        with tempfile.TemporaryDirectory() as td:
            repo = Path(td)
            spec = repo / "docs" / "specs" / "spec-nine"
            spec.mkdir(parents=True)
            self.write_index(repo, folder="spec-nine", status="APPROVED")
            (spec / "section-5.md").write_text(
                "# Spec #9 — Section 5\n"
                "**Status:** IN REVIEW\n"
                "Unit only.\n",
                encoding="utf-8",
            )
            proc = self.run_auditor(SCHEMA, repo, spec)
            self.assertEqual(proc.returncode, 1, proc.stdout)
            self.assertIn("BLOCK", proc.stdout)


if __name__ == "__main__":
    unittest.main()
