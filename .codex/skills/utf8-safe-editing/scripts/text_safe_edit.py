#!/usr/bin/env python3
"""Safe text editing helpers for UTF-8 source files.

All write operations in this tool enforce:
- UTF-8 encoding without BOM
- CRLF line endings
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

UTF8_BOM = b"\xef\xbb\xbf"

def _read_text_utf8(path: Path) -> str:
    data = path.read_bytes()
    if data.startswith(UTF8_BOM):
        data = data[len(UTF8_BOM):]
    return data.decode("utf-8")

def _to_crlf(text: str) -> str:
    normalized = text.replace("\r\n", "\n").replace("\r", "\n")
    return normalized.replace("\n", "\r\n")

def _write_text_utf8_crlf(path: Path, text: str) -> None:
    path.write_bytes(_to_crlf(text).encode("utf-8"))

def _compress_blank_lines(text: str, max_blank_lines: int) -> str:
    if max_blank_lines < 0:
        raise ValueError("max_blank_lines must be >= 0")
    out: list[str] = []
    blank_run = 0
    for line in text.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        if line.strip() == "":
            blank_run += 1
            if blank_run <= max_blank_lines:
                out.append("")
        else:
            blank_run = 0
            out.append(line)
    return "\n".join(out)

def _parse_flags(flag_string: str) -> int:
    flags = 0
    for ch in flag_string:
        if ch == "i":
            flags |= re.IGNORECASE
        elif ch == "m":
            flags |= re.MULTILINE
        elif ch == "s":
            flags |= re.DOTALL
        else:
            raise ValueError(f"Unsupported regex flag: {ch}")
    return flags

def cmd_replace(args: argparse.Namespace) -> int:
    path = Path(args.file)
    text = _read_text_utf8(path)
    count = args.count if args.count is not None else -1
    replaced = text.replace(args.old, args.new, count)
    if args.fail_if_missing and replaced == text:
        print("No replacement was applied.", file=sys.stderr)
        return 2
    if args.max_blank_lines is not None:
        replaced = _compress_blank_lines(replaced, args.max_blank_lines)
    _write_text_utf8_crlf(path, replaced)
    return 0

def cmd_regex_replace(args: argparse.Namespace) -> int:
    path = Path(args.file)
    text = _read_text_utf8(path)
    flags = _parse_flags(args.flags)
    count = args.count if args.count is not None else 0
    replaced, n = re.subn(args.pattern, args.repl, text, count=count, flags=flags)
    if args.fail_if_missing and n == 0:
        print("No regex replacement was applied.", file=sys.stderr)
        return 2
    if args.max_blank_lines is not None:
        replaced = _compress_blank_lines(replaced, args.max_blank_lines)
    _write_text_utf8_crlf(path, replaced)
    return 0

def cmd_normalize(args: argparse.Namespace) -> int:
    path = Path(args.file)
    text = _read_text_utf8(path)
    text = _compress_blank_lines(text, args.max_blank_lines)
    _write_text_utf8_crlf(path, text)
    return 0

def cmd_write_stdin(args: argparse.Namespace) -> int:
    path = Path(args.file)
    text = sys.stdin.read()
    if args.max_blank_lines is not None:
        text = _compress_blank_lines(text, args.max_blank_lines)
    _write_text_utf8_crlf(path, text)
    return 0

def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Safe UTF-8 text editing helpers. Writes UTF-8(no BOM) + CRLF."
    )
    sub = parser.add_subparsers(dest="command", required=True)

    p_replace = sub.add_parser("replace", help="Literal string replacement.")
    p_replace.add_argument("--file", required=True, help="Target file path.")
    p_replace.add_argument("--old", required=True, help="Old literal text.")
    p_replace.add_argument("--new", required=True, help="New literal text.")
    p_replace.add_argument(
        "--count",
        type=int,
        help="Max replacement count. Omit for all occurrences.",
    )
    p_replace.add_argument(
        "--max-blank-lines",
        type=int,
        help="Compress consecutive blank lines to this max count before write.",
    )
    p_replace.add_argument(
        "--fail-if-missing",
        action="store_true",
        help="Return non-zero when no replacement happens.",
    )
    p_replace.set_defaults(func=cmd_replace)

    p_regex = sub.add_parser("regex-replace", help="Regex replacement.")
    p_regex.add_argument("--file", required=True, help="Target file path.")
    p_regex.add_argument("--pattern", required=True, help="Regex pattern.")
    p_regex.add_argument("--repl", required=True, help="Regex replacement text.")
    p_regex.add_argument(
        "--flags",
        default="",
        help="Regex flags: i (ignorecase), m (multiline), s (dotall).",
    )
    p_regex.add_argument(
        "--count",
        type=int,
        help="Max replacement count. Omit/0 for all occurrences.",
    )
    p_regex.add_argument(
        "--max-blank-lines",
        type=int,
        help="Compress consecutive blank lines to this max count before write.",
    )
    p_regex.add_argument(
        "--fail-if-missing",
        action="store_true",
        help="Return non-zero when no replacement happens.",
    )
    p_regex.set_defaults(func=cmd_regex_replace)

    p_norm = sub.add_parser(
        "normalize", help="Normalize line endings and compress blank lines."
    )
    p_norm.add_argument("--file", required=True, help="Target file path.")
    p_norm.add_argument(
        "--max-blank-lines",
        type=int,
        default=1,
        help="Max allowed consecutive blank lines.",
    )
    p_norm.set_defaults(func=cmd_normalize)

    p_stdin = sub.add_parser(
        "write-stdin",
        help="Read stdin and write to file as UTF-8(no BOM)+CRLF.",
    )
    p_stdin.add_argument("--file", required=True, help="Target file path.")
    p_stdin.add_argument(
        "--max-blank-lines",
        type=int,
        help="Compress consecutive blank lines to this max count before write.",
    )
    p_stdin.set_defaults(func=cmd_write_stdin)

    return parser

def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        return int(args.func(args))
    except Exception as exc:  # pragma: no cover - command-line guard
        print(f"Error: {exc}", file=sys.stderr)
        return 1

if __name__ == "__main__":
    raise SystemExit(main())
