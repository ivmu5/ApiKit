#!/usr/bin/env python3
"""Fail if any first-party C# source contains Cyrillic characters in comments.

The intentionally separate CryptoKit Git submodule is excluded; its upstream
source must not be rewritten by this project's formatting rules.
"""
from pathlib import Path
import re

repo_root = Path(__file__).resolve().parent.parent
cyrillic = re.compile(r"[\u0400-\u04ff]")
violations = []
for path in repo_root.rglob("*.cs"):
    relative = path.relative_to(repo_root)
    if relative.parts[:2] == ("external", "CryptoKit"):
        continue
    block_comment = False
    for index, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        stripped = line.strip()
        if "/*" in line and not line.lstrip().startswith("//"):
            block_comment = True
        if cyrillic.search(line) and (
            stripped.startswith("//") or
            block_comment or
            ("//" in line and cyrillic.search(line.split("//", 1)[-1]))
        ):
            violations.append(f"{relative}:{index}")
        if "*/" in line:
            block_comment = False

if violations:
    raise SystemExit("Cyrillic comments found:\n" + "\n".join(violations))
print("First-party C# comments: English-only check passed.")
