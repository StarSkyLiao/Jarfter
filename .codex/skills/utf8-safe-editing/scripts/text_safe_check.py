#!/usr/bin/env python3
"""Validate UTF-8/BOM/line-ending safety for a text file."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

def count_line_endings(data: bytes) -> dict[str, int]:
    crlf = 0
    lf_only = 0
    cr_only = 0
    index = 0
    while index < len(data):
        byte = data[index]
        if byte == 0x0D:
            if index + 1 < len(data) and data[index + 1] == 0x0A:
                crlf += 1
                index += 2
            else:
                cr_only += 1
                index += 1
        elif byte == 0x0A:
            lf_only += 1
            index += 1
        else:
            index += 1
    return {
        "crlf": crlf,
        "lf_only": lf_only,
        "cr_only": cr_only,
    }

def inspect_file(path: Path) -> dict[str, object]:
    data = path.read_bytes()
    has_bom = data.startswith(b"\xef\xbb\xbf")
    has_nul = b"\x00" in data
    utf8_ok = True
    decode_error: str | None = None
    try:
        data.decode("utf-8")
    except UnicodeDecodeError as exc:
        utf8_ok = False
        decode_error = str(exc)

    line_endings = count_line_endings(data)
    has_lf_only = line_endings["lf_only"] > 0
    has_cr_only = line_endings["cr_only"] > 0
    has_mixed_line_endings = sum(
        1 for count in line_endings.values() if count > 0
    ) > 1
    has_non_crlf_line_endings = has_lf_only or has_cr_only

    return {
        "file": str(path),
        "size_bytes": len(data),
        "utf8_ok": utf8_ok,
        "decode_error": decode_error,
        "has_bom": has_bom,
        "has_nul": has_nul,
        "crlf_count": line_endings["crlf"],
        "has_lf_only": has_lf_only,
        "lf_only_count": line_endings["lf_only"],
        "has_cr_only": has_cr_only,
        "cr_only_count": line_endings["cr_only"],
        "has_mixed_line_endings": has_mixed_line_endings,
        "safe_utf8_no_bom_crlf": (
            utf8_ok and not has_bom and not has_nul and not has_non_crlf_line_endings
        ),
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
        print(f"has_nul: {info['has_nul']}")
        print(f"crlf_count: {info['crlf_count']}")
        print(f"has_lf_only: {info['has_lf_only']}")
        print(f"lf_only_count: {info['lf_only_count']}")
        print(f"has_cr_only: {info['has_cr_only']}")
        print(f"cr_only_count: {info['cr_only_count']}")
        print(f"has_mixed_line_endings: {info['has_mixed_line_endings']}")
        print(f"safe_utf8_no_bom_crlf: {info['safe_utf8_no_bom_crlf']}")
        if info["decode_error"]:
            print(f"decode_error: {info['decode_error']}")

    return 0 if info["safe_utf8_no_bom_crlf"] else 2

if __name__ == "__main__":
    raise SystemExit(main())
