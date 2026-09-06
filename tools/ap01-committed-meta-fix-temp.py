from pathlib import Path

p = Path('work/tools/unity-ci/test-meta-integrity-gameart.sh')
text = p.read_text(encoding='utf-8')
old = '''# Mutation 1: tracked GameArt file without its meta.
mv "$probe_asset_meta" "$tmpdir/missing-probe.meta"
git rm --cached -q "$probe_asset_meta"
expect_fail "missing GameArt meta" "MISSING META: $probe_asset"
mv "$tmpdir/missing-probe.meta" "$probe_asset_meta"
git add -f "$probe_asset_meta"
run_clean "after restoring missing-meta mutation"
'''
new = '''# Mutation 1: the GameArt meta remains physically present but is removed from
# the temporary Git index. The checker must reject an uncommitted meta because
# it would disappear on checkout even though it exists in this working tree.
git rm --cached -q "$probe_asset_meta"
if [ ! -e "$probe_asset_meta" ]; then
  echo "::error::Uncommitted-meta mutation unexpectedly removed the working-tree meta"
  exit 1
fi
expect_fail "uncommitted GameArt meta" "MISSING META: $probe_asset"
git add -f "$probe_asset_meta"
run_clean "after restoring committed-meta mutation"
'''
assert old in text
p.write_text(text.replace(old, new, 1), encoding='utf-8')
