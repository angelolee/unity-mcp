from __future__ import annotations

import json
from pathlib import Path


TEXT_SUFFIXES = {".cs", ".asmdef", ".uxml", ".uss", ".json", ".md"}


def transform_text(path: Path, text: str) -> str:
    if path.name == "package.json":
        package = json.loads(text)
        package["name"] = "com.tgs.mcp-for-unity"
        package["unity"] = "2020.3"
        package["required"] = False
        package["repository"] = {
            "type": "git",
            "url": "https://github.com/tgs-gaming/tools",
        }
        package["author"] = {"name": "Angelo Lee"}
        return json.dumps(package, indent=2) + "\n"

    transformed = text.replace("MCPForUnity.Editor", "com.tgs.mcpforunity.editor")
    transformed = transformed.replace("MCPForUnity.Runtime", "com.tgs.mcpforunity")

    if path.suffix == ".asmdef":
        assembly = json.loads(transformed)
        if assembly.get("name") == "com.tgs.mcpforunity.editor":
            assembly["name"] = "com.tgs.mcp-for-unity.editor"
        elif assembly.get("name") == "com.tgs.mcpforunity":
            assembly["name"] = "com.tgs.mcp-for-unity"
        assembly["references"] = [
            "com.tgs.mcp-for-unity" if value == "com.tgs.mcpforunity" else value
            for value in assembly.get("references", [])
        ]
        return json.dumps(assembly, indent=4) + "\n"

    return transformed


def transform_package(package_root: Path) -> list[Path]:
    changed = []
    for path in sorted(package_root.rglob("*")):
        if not path.is_file() or path.suffix not in TEXT_SUFFIXES:
            continue
        original = path.read_text(encoding="utf-8-sig")
        transformed = transform_text(path.relative_to(package_root), original)
        if transformed != original:
            path.write_text(transformed, encoding="utf-8")
            changed.append(path)
    return changed
