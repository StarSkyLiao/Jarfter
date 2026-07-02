---
name: utf8-safe-editing
description: Safely edit repository text files as UTF-8 without BOM and with CRLF line endings. Use when replacing text, normalizing files, or avoiding PowerShell text-write encoding damage.
---

# UTF-8 Safe Editing

## Mandatory Rules

1. Do not use PowerShell `Get-Content`/`Set-Content`, `-replace`, or ad-hoc byte conversion pipelines to write repository text files.
2. Prefer `apply_patch` for small edits and explicit Python byte/text handling for generated or rewritten files.
3. Prefer targeted replacement over full-file rewriting when modifying existing content.
4. Full rewrites are allowed only when followed by validation for UTF-8 without BOM and CRLF line endings.
5. Treat source files as UTF-8. If visible terminal output looks garbled, verify with Python or bytes before changing content.

## Tool Commands

```powershell
# Literal replacement
python .codex/skills/utf8-safe-editing/scripts/text_safe_edit.py replace --file <path> --old <old> --new <new> --fail-if-missing

# Regex replacement
python .codex/skills/utf8-safe-editing/scripts/text_safe_edit.py regex-replace --file <path> --pattern <regex> --repl <text> --fail-if-missing

# Normalize blank lines and line endings
python .codex/skills/utf8-safe-editing/scripts/text_safe_edit.py normalize --file <path> --max-blank-lines 1

# Validate after writing
python .codex/skills/utf8-safe-editing/scripts/text_safe_check.py --file <path>
```

## Incorrect and Correct Patterns

### Pattern 1: PowerShell Text Rewrite

```powershell
# Wrong
Get-Content -Raw $file | ForEach-Object { $_ -replace 'A', 'B' } | Set-Content $file
```

```powershell
# Correct
python .codex/skills/utf8-safe-editing/scripts/text_safe_edit.py replace --file "<path>" --old "A" --new "B" --fail-if-missing
python .codex/skills/utf8-safe-editing/scripts/text_safe_check.py --file "<path>"
```

### Pattern 2: Uncontrolled Blank-Line Insertion

```python
# Wrong
text = text.replace("X", "X\n\n\n")
```

```python
# Correct
normalized = []
for line in text.splitlines():
    if line.strip() == "" and normalized and normalized[-1].strip() == "":
        continue
    normalized.append(line)
text = "\r\n".join(normalized).rstrip("\r\n") + "\r\n"
```

### Pattern 3: Continuing From Damaged Text

```python
# Correct: restore trusted UTF-8 content before editing.
import subprocess
from pathlib import Path

repo = Path(r"E:\Dotnet\Jarfter")
raw = subprocess.run([
    "git", "cat-file", "-p", "HEAD:.codex/skills/csharp-commenting-zh/SKILL.md"
], cwd=repo, capture_output=True, check=True).stdout
text = raw.decode("utf-8")
text = text.replace("old content", "new content")
Path("<target>").write_bytes(text.replace("\n", "\r\n").encode("utf-8"))
```

## Recommended Order

1. Apply a targeted replacement first.
2. Run `normalize` only when blank lines or line endings need cleanup.
3. Run `text_safe_check.py` after every write.
4. If validation fails, fix the encoding or line endings immediately before continuing.
