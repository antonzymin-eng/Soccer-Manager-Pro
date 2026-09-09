from __future__ import annotations

import importlib.util
from pathlib import Path
import tempfile
import time
import unittest


ROOT = Path(__file__).resolve().parents[2]
GENERATOR = ROOT / "tools" / "dotnet-ci" / "generate_projects.py"


def load_generator():
    spec = importlib.util.spec_from_file_location("td_generate_projects", GENERATOR)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class GenerateProjectsIncrementalTests(unittest.TestCase):
    def test_byte_identical_generated_file_is_not_rewritten(self) -> None:
        module = load_generator()
        with tempfile.TemporaryDirectory() as td:
            path = Path(td) / "generated.csproj"
            path.write_text("same\n", encoding="utf-8")
            before = path.stat().st_mtime_ns
            time.sleep(0.02)
            changed = module.write_text_if_changed(path, "same\n")
            self.assertFalse(changed)
            self.assertEqual(path.stat().st_mtime_ns, before)

            time.sleep(0.02)
            changed = module.write_text_if_changed(path, "different\n")
            self.assertTrue(changed)
            self.assertEqual(path.read_text(encoding="utf-8"), "different\n")

    def test_project_and_solution_generation_route_through_write_if_changed(self) -> None:
        text = GENERATOR.read_text(encoding="utf-8")
        self.assertIn('write_text_if_changed(out, "\\n".join(lines) + "\\n")', text)
        self.assertIn('write_text_if_changed(SLN_PATH, "\\n".join(sln) + "\\n")', text)


if __name__ == "__main__":
    unittest.main()
