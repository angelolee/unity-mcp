import json
from pathlib import Path

from tools.tgs_identity import transform_text


def test_transforms_csharp_namespace_and_using() -> None:
    source = "using MCPForUnity.Editor.Helpers;\nnamespace MCPForUnity.Editor.Tools {}\n"

    result = transform_text(Path("Editor/Tools/NewTool.cs"), source)

    assert "using com.tgs.mcpforunity.editor.Helpers;" in result
    assert "namespace com.tgs.mcpforunity.editor.Tools" in result


def test_transforms_assembly_definition_identity() -> None:
    source = json.dumps({
        "name": "MCPForUnity.Editor",
        "rootNamespace": "MCPForUnity.Editor",
        "references": ["MCPForUnity.Runtime"],
    })

    result = json.loads(transform_text(Path("Editor/MCPForUnity.Editor.asmdef"), source))

    assert result["name"] == "com.tgs.mcp-for-unity.editor"
    assert result["rootNamespace"] == "com.tgs.mcpforunity.editor"
    assert result["references"] == ["com.tgs.mcp-for-unity"]


def test_transforms_package_metadata_preserving_upstream_version() -> None:
    source = json.dumps({
        "name": "com.coplaydev.unity-mcp",
        "version": "10.0.0",
        "unity": "2020.3",
        "dependencies": {},
    })

    result = json.loads(transform_text(Path("package.json"), source))

    assert result["name"] == "com.tgs.mcp-for-unity"
    assert result["version"] == "10.0.0"
    assert "mcpServerVersion" not in result
    assert result["required"] is False
    assert result["unity"] == "2020.3"
