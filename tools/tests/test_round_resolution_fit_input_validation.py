import importlib.util
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "round-resolution-fit.py"
SPEC = importlib.util.spec_from_file_location("round_resolution_fit", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class RoundResolutionFitInputValidationTests(unittest.TestCase):
    def assert_invalid(self, contents, message):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "corpus.csv"
            path.write_text(contents, encoding="utf-8")
            with self.assertRaisesRegex(MODULE.CorpusError, message):
                MODULE.read_rows([path])

    def test_rejects_all_seven_missing_required_column_combinations(self):
        required = ("dSquad", "homeGoals", "awayGoals")
        for mask in range(1, 8):
            present = [name for index, name in enumerate(required) if not mask & (1 << index)]
            with self.subTest(missing_mask=mask):
                self.assert_invalid(",".join(present) + "\n", "missing required column")

    def test_rejects_each_blank_required_value(self):
        rows = (
            ",1,1",
            "0,,1",
            "0,1,",
        )
        for row in rows:
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "must not be blank")

    def test_rejects_each_non_finite_rating(self):
        for value in ("nan", "inf", "-inf"):
            with self.subTest(value=value):
                self.assert_invalid(
                    f"dSquad,homeGoals,awayGoals\n{value},1,1\n",
                    "dSquad must be finite",
                )

    def test_rejects_malformed_values_in_each_numeric_field(self):
        rows = (
            "not-a-rating,1,1",
            "0,not-a-score,1",
            "0,1,not-a-score",
        )
        for row in rows:
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "invalid|integer")

    def test_rejects_fractional_goal_counts_for_both_sides(self):
        for row in ("0,1.5,1", "0,1,1.5"):
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "must be an integer")

    def test_rejects_negative_goal_counts_for_both_sides(self):
        for row in ("0,-1,1", "0,1,-1"):
            with self.subTest(row=row):
                self.assert_invalid("dSquad,homeGoals,awayGoals\n" + row + "\n", "must be >= 0")

    def test_accepts_engine_scores_above_surrogate_model_cap(self):
        excessive = MODULE.MAX_GOALS_PER_SIDE + 1
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "corpus.csv"
            path.write_text(
                f"dSquad,homeGoals,awayGoals\n0,{excessive},{excessive + 1}\n",
                encoding="utf-8",
            )
            self.assertEqual(
                MODULE.read_rows([path]),
                [{"d": 0.0, "h": excessive, "a": excessive + 1}],
            )

    def test_rejects_rows_wider_than_header(self):
        self.assert_invalid("dSquad,homeGoals,awayGoals\n0,1,1,unexpected\n", "more values")

    def test_missing_file_is_a_corpus_error(self):
        with self.assertRaisesRegex(MODULE.CorpusError, "cannot read corpus"):
            MODULE.read_rows([Path("/definitely/missing/corpus.csv")])

    def test_rejects_empty_file(self):
        self.assert_invalid("", "empty or has no CSV header")

    def test_accepts_bom_and_canonical_rows(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "corpus.csv"
            path.write_text("\ufeffdSquad,homeGoals,awayGoals\n-0.5,0,20\n", encoding="utf-8")
            self.assertEqual(MODULE.read_rows([path]), [{"d": -0.5, "h": 0, "a": 20}])


class RoundResolutionFitHeldParamsTests(unittest.TestCase):
    """--hold-params scores the shipped constants without refitting (PM-2 quick-sim check, D-09)."""

    def test_parses_three_finite_values(self):
        self.assertEqual(MODULE.parse_held_params("1.2325, 0.2162,0.4996"), (1.2325, 0.2162, 0.4996))

    def test_rejects_malformed_held_params(self):
        cases = (
            ("1,2", "exactly three"),
            ("1,2,3,4", "exactly three"),
            ("1,x,3", "invalid number"),
            ("1,nan,3", "finite"),
            ("0,0.2,0.5", "BASE must be positive"),
        )
        for raw, message in cases:
            with self.subTest(raw=raw):
                with self.assertRaisesRegex(MODULE.CorpusError, message):
                    MODULE.parse_held_params(raw)

    def test_held_mode_scores_the_given_constants_and_labels_the_artifact(self):
        import contextlib
        import io
        import sys

        rows = ["dSquad,homeGoals,awayGoals"]
        for d in (-1, 0, 1):
            for i in range(4):
                rows.append(f"{d + 0.01 * i:.2f},{(i + d + 1) % 4},{(i + 2) % 3}")
        with tempfile.TemporaryDirectory() as directory:
            corpus = Path(directory) / "corpus.csv"
            corpus.write_text("\n".join(rows) + "\n", encoding="utf-8")
            out = Path(directory) / "artifact.md"
            argv = ["round-resolution-fit.py", str(corpus), "--hold-params", "0.9,0.3,0.7",
                    "--out", str(out)]
            buffer = io.StringIO()
            saved = sys.argv
            try:
                sys.argv = argv
                with contextlib.redirect_stdout(buffer):
                    self.assertEqual(MODULE.main(), 0)
            finally:
                sys.argv = saved
            printed = buffer.getvalue()
            self.assertIn("held (NOT refitted): BaseGoals=0.9000 GoalRatingSlope=0.3000 "
                          "HomeAdvantageRating=0.7000", printed)
            artifact = out.read_text(encoding="utf-8")
            self.assertIn("## Held parameters (scored, not refitted)", artifact)
            self.assertNotIn("## Fitted parameters", artifact)
            self.assertIn("| `QuickSimBaseGoals` | 0.9000 |", artifact)
            self.assertIn("| `QuickSimHomeAdvantageRating` | 0.7000 |", artifact)


if __name__ == "__main__":
    unittest.main()
