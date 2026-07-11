from pathlib import Path
import re

import pytest


ROOT = Path(__file__).resolve().parents[2]
CS_FILES = tuple(sorted((ROOT / "MCPForUnity").rglob("*.cs")))

FORBIDDEN = (
    ("target-typed new", re.compile(r"=\s*new\s*\((?![^)]*\)\s*\[)")),
    ("is not pattern", re.compile(r"\bis\s+not\b")),
    ("record declaration", re.compile(r"^\s*(?:public|internal|private|protected)?\s*record\b", re.MULTILINE)),
    ("init accessor", re.compile(r"\binit\s*;")),
)


def _blank_preserving_newlines(value: str) -> str:
    return "".join("\n" if char == "\n" else " " for char in value)


def csharp_code_without_strings_or_comments(text: str) -> str:
    result = []
    i = 0
    while i < len(text):
        char = text[i]
        next_char = text[i + 1] if i + 1 < len(text) else ""

        if char == "/" and next_char == "/":
            end = text.find("\n", i)
            if end == -1:
                result.append(_blank_preserving_newlines(text[i:]))
                break
            result.append(_blank_preserving_newlines(text[i:end]))
            i = end
            continue

        if char == "/" and next_char == "*":
            end = text.find("*/", i + 2)
            if end == -1:
                result.append(_blank_preserving_newlines(text[i:]))
                break
            result.append(_blank_preserving_newlines(text[i:end + 2]))
            i = end + 2
            continue

        is_verbatim = char == "@" and next_char == '"'
        is_interpolated_verbatim = char == "$" and next_char == "@" and i + 2 < len(text) and text[i + 2] == '"'
        is_verbatim_interpolated = char == "@" and next_char == "$" and i + 2 < len(text) and text[i + 2] == '"'
        if is_verbatim or is_interpolated_verbatim or is_verbatim_interpolated:
            start = i
            i += 2 if is_verbatim else 3
            while i < len(text):
                if text[i] == '"' and i + 1 < len(text) and text[i + 1] == '"':
                    i += 2
                    continue
                if text[i] == '"':
                    i += 1
                    break
                i += 1
            result.append(_blank_preserving_newlines(text[start:i]))
            continue

        if char == '"' or (char == "$" and next_char == '"'):
            start = i
            i += 2 if char == "$" else 1
            while i < len(text):
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == '"':
                    i += 1
                    break
                i += 1
            result.append(_blank_preserving_newlines(text[start:i]))
            continue

        if char == "'":
            start = i
            i += 1
            while i < len(text):
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == "'":
                    i += 1
                    break
                i += 1
            result.append(_blank_preserving_newlines(text[start:i]))
            continue

        result.append(char)
        i += 1

    return "".join(result)


@pytest.mark.parametrize("label,pattern", FORBIDDEN)
def test_mcp_sources_stay_within_csharp8(label: str, pattern: re.Pattern[str]) -> None:
    violations = []
    for path in CS_FILES:
        text = path.read_text(encoding="utf-8-sig")
        code = csharp_code_without_strings_or_comments(text)
        for match in pattern.finditer(code):
            line = text.count("\n", 0, match.start()) + 1
            violations.append(f"{path.relative_to(ROOT)}:{line}")

    assert violations == [], f"{label}: " + ", ".join(violations)
