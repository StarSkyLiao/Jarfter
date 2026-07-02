#!/usr/bin/env python3
"""Validate UTF-8/BOM/line-ending safety for a text file."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

def inspect_file(path: Path) -> dict[str, object]:
    data = path.read_bytes()
    has_bom = data.startswith(b"\xef\xbb\xbf")
    utf8_ok = True
    decode_error: str | None = None
    try:
        data.decode("utf-8")
    except UnicodeDecodeError as exc:
        utf8_ok = False
        decode_error = str(exc)

    has_lf_only = False
    for i, b in enumerate(data):
        if b == 0x0A and (i == 0 or data[i - 1] != 0x0D):
            has_lf_only = True
            break

    return {
        "file": str(path),
        "size_bytes": len(data),
        "utf8_ok": utf8_ok,
        "decode_error": decode_error,
        "has_bom": has_bom,
        "has_lf_only": has_lf_only,
        "safe_utf8_no_bom_crlf": utf8_ok and not has_bom and not has_lf_only,
    }

def main() -> int:
    parser = argparse.ArgumentParser(
        description="Check whether a file is UTF-8(no BOM) and CRLF."
    )
    parser.add_argument("--file", required=True, help="Target file path.")
    parser.add_argument(
        "--json",
        action="store_true",
        help="Print JSON output (single line).",
    )
    args = parser.parse_args()

    info = inspect_file(Path(args.file))
    if args.json:
        print(json.dumps(info, ensure_ascii=False))
    else:
        print(f"file: {info['file']}")
        print(f"size_bytes: {info['size_bytes']}")
        print(f"utf8_ok: {info['utf8_ok']}")
        print(f"has_bom: {info['has_bom']}")
        print(f"has_lf_only: {info['has_lf_only']}")
        print(f"safe_utf8_no_bom_crlf: {info['safe_utf8_no_bom_crlf']}")
        if info["decode_error"]:
            print(f"decode_error: {info['decode_error']}")

    return 0 if info["safe_utf8_no_bom_crlf"] else 2

if __name__ == "__main__":
    raise SystemExit(main())
