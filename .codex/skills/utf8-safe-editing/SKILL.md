---
name: utf8-safe-editing
description: 以 UTF-8 无 BOM + CRLF 安全修改仓库文本文件. 当需要批量替换, 规范化写回, 或避免 PowerShell 文本写回导致乱码时使用.
---

# UTF-8 安全编辑

## 强制规则

1. 编辑项目文件时, 禁止使用 PowerShell `Get-Content`/`Set-Content`, `-replace`, 或临时字节转换链路写回.
2. 优先使用 `apply_patch` 或 Python 显式 UTF-8 读写.
3. 修改现有内容时优先替换式编辑.
4. 允许全量重写, 但写回后必须校验 UTF-8 无 BOM 与 CRLF.
5. 始终将源码文件视为 UTF-8.

## 工具命令

```powershell
# 文本替换
python skills/utf8-safe-editing/scripts/text_safe_edit.py replace --file <path> --old <old> --new <new> --fail-if-missing

# 正则替换
python skills/utf8-safe-editing/scripts/text_safe_edit.py regex-replace --file <path> --pattern <regex> --repl <text> --fail-if-missing

# 规范化空行与换行
python skills/utf8-safe-editing/scripts/text_safe_edit.py normalize --file <path> --max-blank-lines 1

# 写回后校验
python skills/utf8-safe-editing/scripts/text_safe_check.py --file <path>
```

## 错误示例与正确命令

### 错误示例 1, PowerShell 文本写回

```powershell
# Wrong
Get-Content -Raw $file | ForEach-Object { $_ -replace 'A', 'B' } | Set-Content $file
```

```powershell
# Correct
python skills/utf8-safe-editing/scripts/text_safe_edit.py replace --file "<path>" --old "A" --new "B" --fail-if-missing
python skills/utf8-safe-editing/scripts/text_safe_check.py --file "<path>"
```

### 错误示例 2, 不受控插入空行

```python
# Wrong
text = text.replace('X', 'X\n\n\n')
```

```python
# Correct
normalized = []
for line in text.splitlines():
    if line.strip() == '' and normalized and normalized[-1].strip() == '':
        continue
    normalized.append(line)
text = '\r\n'.join(normalized).rstrip('\r\n') + '\r\n'
```

### 错误示例 3, 在损坏文本上继续修改

```python
# Correct, 先恢复可信 UTF-8 原文再编辑
import subprocess
from pathlib import Path

repo = Path(r'E:\C#Library\Jarfter\Jarfter')
raw = subprocess.run([
    'git', 'cat-file', '-p', 'HEAD:skills/csharp-commenting-zh/SKILL.md'
], cwd=repo, capture_output=True, check=True).stdout
text = raw.decode('utf-8')
text = text.replace('旧内容', '新内容')
Path('<target>').write_bytes(text.replace('\n', '\r\n').encode('utf-8'))
```

## 推荐执行顺序

1. 先执行替换式修改.
2. 再执行 `normalize` 或必要的全量改写.
3. 最后执行 `text_safe_check.py`.
4. 校验失败时立即修复, 不在异常状态继续编辑.
