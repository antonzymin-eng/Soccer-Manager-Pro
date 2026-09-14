from pathlib import Path
import re

ROOT = Path('.')
TMP = Path('/tmp/d5-reconcile')


def read(path):
    return (ROOT / path).read_text(encoding='utf-8')


def write(path, text):
    (ROOT / path).write_text(text, encoding='utf-8')


def backup(name):
    return (TMP / name).read_text(encoding='utf-8')


def extract_record(text, needle, prefix):
    lines = text.splitlines(keepends=True)
    start = next(i for i, line in enumerate(lines) if needle in line and line.startswith(prefix))
    end = len(lines)
    for i in range(start + 1, len(lines)):
        if lines[i].startswith(prefix):
            end = i
            break
    return ''.join(lines[start:end]).rstrip('\n') + '\n\n'


def remove_record(text, needle, prefix):
    lines = text.splitlines(keepends=True)
    matches = [i for i, line in enumerate(lines) if needle in line and line.startswith(prefix)]
    for start in reversed(matches):
        end = len(lines)
        for i in range(start + 1, len(lines)):
            if lines[i].startswith(prefix):
                end = i
                break
        del lines[start:end]
    return ''.join(lines)


def prepend_changelog(path, backup_name, needle, version=None):
    src = backup(backup_name)
    record = extract_record(src, needle, '> **Last Updated')
    if version is not None:
        record = re.sub(r'\(v\d+\.\d+ —', f'(v{version} —', record, count=1)
    text = read(path)
    text = remove_record(text, needle, '> **Last Updated')
    marker = '## Header chain\n\n' if path.endswith('CHANGELOG-src.md') else '---\n\n'
    if marker not in text:
        raise SystemExit(f'missing changelog marker in {path}')
    head, rest = text.split(marker, 1)
    rest = rest.replace('> **Last Updated:**', '> **Last Updated (prior):**', 1)
    write(path, head + marker + record + rest)


def prepend_manifest():
    src = backup('file-manifest.md')
    record = extract_record(src, 'D5 / Transfers, Contracts & Negotiation #31 T0', '**Last Updated')
    text = read('docs/tracking/file-manifest.md')
    text = remove_record(text, 'D5 / Transfers, Contracts & Negotiation #31 T0', '**Last Updated')
    first = text.find('**Last Updated:**')
    if first < 0:
        raise SystemExit('file-manifest current head missing')
    text = text[:first] + text[first:].replace('**Last Updated:**', '**Last Updated (prior):**', 1)
    first_prior = text.find('**Last Updated (prior):**')
    text = text[:first_prior] + record + text[first_prior:]
    write('docs/tracking/file-manifest.md', text)


def heading_block(text, needle):
    lines = text.splitlines(keepends=True)
    hit = next(i for i, line in enumerate(lines) if needle in line)
    start = hit
    while start >= 0 and not lines[start].startswith(('### ', '## ')):
        start -= 1
    if start < 0:
        raise SystemExit(f'heading not found for {needle}')
    level = '### ' if lines[start].startswith('### ') else '## '
    end = len(lines)
    next_heading = None
    for i in range(start + 1, len(lines)):
        if lines[i].startswith(level):
            end = i
            next_heading = lines[i].strip()
            break
    return ''.join(lines[start:end]).rstrip('\n') + '\n\n', next_heading


def ensure_manifest_inventory():
    path = 'docs/tracking/file-manifest.md'
    text = read(path)
    if 'src/transfers/TransferCommands.cs' not in text:
        block, next_heading = heading_block(backup('file-manifest.md'), 'src/transfers/TransferCommands.cs')
        if next_heading and next_heading in text:
            text = text.replace(next_heading, block + next_heading, 1)
        else:
            text += '\n' + block
    # Restore the #31 status line from the branch if the merge chose the old main row.
    branch_lines = backup('file-manifest.md').splitlines()
    candidates = [line for line in branch_lines if ('#31' in line or '| 31 |' in line) and 'T0' in line and 'transfer' in line.lower()]
    if candidates:
        wanted = candidates[0]
        lines = text.splitlines()
        for i, line in enumerate(lines):
            if ('#31' in line or '| 31 |' in line) and 'transfer' in line.lower() and line != wanted:
                if line.startswith('|') and wanted.startswith('|'):
                    lines[i] = wanted
                    text = '\n'.join(lines) + ('\n' if text.endswith('\n') else '')
                    break
    write(path, text)


def ensure_open_issue_note():
    path = 'docs/tracking/open-issues.md'
    marker = '**UPDATE September 14, 2026 — #31 TRANSFERS T0 NOW EXISTS; S2 REMAINS `FUTURE-BLOCKED` AT THE UX LAYER.**'
    text = read(path)
    if marker in text:
        return
    src = backup('open-issues.md')
    start = src.find(marker)
    if start < 0:
        raise SystemExit('backup #31 UX update missing')
    end = src.find('\n- **', start)
    note = src[start:] if end < 0 else src[start:end]
    ux = text.find('- **UX workstream:')
    if ux < 0:
        raise SystemExit('UX issue missing')
    issue_end = text.find('\n- **', ux + 1)
    if issue_end < 0:
        issue_end = len(text)
    text = text[:issue_end].rstrip() + '\n\n' + note.strip() + '\n' + text[issue_end:]
    write(path, text)


def reconcile_src_version_table():
    path = 'docs/tracking/CHANGELOG-src.md'
    text = read(path)
    # Remove the old branch-local v2.135 row if it survived; main already owns v2.135/v2.136.
    lines = [line for line in text.splitlines() if not (line.startswith('| 2.135 |') and '#31 Transfers T0' in line) and not (line.startswith('| 2.137 |') and '#31 Transfers T0' in line)]
    text = '\n'.join(lines) + '\n'
    src_row = next(line for line in backup('CHANGELOG-src.md').splitlines() if line.startswith('| 2.135 |') and '#31 Transfers T0' in line)
    row = src_row.replace('| 2.135 |', '| 2.137 |', 1)
    rows = text.splitlines()
    header = next(i for i, line in enumerate(rows) if line.startswith('| Version | Date'))
    insert_at = header + 2
    rows.insert(insert_at, row)
    write(path, '\n'.join(rows) + '\n')


prepend_changelog('docs/tracking/CHANGELOG.md', 'CHANGELOG.md', 'D5 / Transfers, Contracts & Negotiation #31 T0')
prepend_changelog('docs/tracking/CHANGELOG-src.md', 'CHANGELOG-src.md', '#31 Transfers T0 / D5', version='2.137')
reconcile_src_version_table()
prepend_manifest()
ensure_manifest_inventory()
ensure_open_issue_note()

# Hard assertions for branch-specific close-out facts that must survive the mainline merge.
checks = {
    'docs/agent-guides/project-reference.md': ['**37 production assemblies**', '`transfers` | **#31**'],
    'docs/tracking/data-contract-index.md': ['Managed transfer state', 'ITransferRosterPort'],
    'docs/tracking/path-to-playable-roadmap.md': ['**D5** ◑', 'T0 IMPLEMENTED in PR #407'],
    'docs/tracking/football-judgment-proxy-review.md': ['34 / 7 / 27', '#31 T0'],
    'docs/tracking/file-manifest.md': ['src/transfers/TransferCommands.cs', 'v1.14'],
}
for path, needles in checks.items():
    text = read(path)
    for needle in needles:
        if needle not in text:
            raise SystemExit(f'{path}: missing required post-merge marker {needle!r}')
