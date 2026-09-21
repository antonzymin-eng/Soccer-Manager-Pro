from __future__ import annotations

import hashlib
import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "dotnet-ci" / "check_evidence_manifests.py"


def _load_checker():
    spec = importlib.util.spec_from_file_location("check_evidence_manifests", SCRIPT)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


class EvidenceManifestTests(unittest.TestCase):
    def test_current_repository_evidence_manifests_pass(self) -> None:
        completed = subprocess.run(
            [sys.executable, str(SCRIPT), "--repo", str(ROOT)],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(0, completed.returncode, completed.stdout + completed.stderr)
        self.assertIn("Evidence SHA-256 manifests: PASS", completed.stdout)

    def test_full_manifest_fails_on_digest_mismatch(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            payload = b"expected"
            (evidence / "payload.txt").write_bytes(payload)
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{_digest(payload)}  payload.txt\n",
                encoding="utf-8",
            )
            (evidence / "payload.txt").write_bytes(b"tampered")
            errors = checker.validate(root)
        self.assertTrue(any("SHA-256 mismatch for payload.txt" in e for e in errors), errors)

    def test_full_manifest_fails_on_uncovered_file(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            payload = b"covered"
            (evidence / "covered.txt").write_bytes(payload)
            (evidence / "uncovered.txt").write_bytes(b"uncovered")
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{_digest(payload)}  covered.txt\n",
                encoding="utf-8",
            )
            errors = checker.validate(root)
        self.assertTrue(
            any("uncovered in-scope files" in e and "uncovered.txt" in e for e in errors),
            errors,
        )

    def test_full_manifest_fails_on_missing_listed_file(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / checker.FULL_MANIFEST).write_text(
                f"{'0' * 64}  missing.txt\n",
                encoding="utf-8",
            )
            errors = checker.validate(root)
        self.assertTrue(any("listed file is missing: missing.txt" in e for e in errors), errors)

    def test_manifest_rejects_path_escape(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            (evidence / checker.PARTIAL_MANIFEST).write_text(
                f"{'0' * 64}  ../outside.txt\n",
                encoding="utf-8",
            )
            errors = checker.validate(root)
        self.assertTrue(any("unsafe manifest path" in e for e in errors), errors)

    def test_artifact_manifest_checks_listed_digests_without_claiming_full_coverage(self) -> None:
        checker = _load_checker()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence = root / checker.EVIDENCE_ROOT / "case"
            evidence.mkdir(parents=True)
            artifact = b"artifact"
            (evidence / "artifact.bin").write_bytes(artifact)
            (evidence / "README.md").write_text("outside artifact scope\n", encoding="utf-8")
            (evidence / checker.PARTIAL_MANIFEST).write_text(
                f"{_digest(artifact)}  artifact.bin\n",
                encoding="utf-8",
            )
            errors = checker.validate(root)
        self.assertEqual([], errors)


if __name__ == "__main__":
    unittest.main()
