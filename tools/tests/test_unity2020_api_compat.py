from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def source(relative_path: str) -> str:
    return (ROOT / relative_path).read_text(encoding="utf-8-sig")


def test_secure_key_stores_avoid_unavailable_process_and_pbkdf_apis() -> None:
    key_store_paths = (ROOT / "MCPForUnity/Editor/Security/SecureKeyStore").glob("*.cs")
    key_store_source = "\n".join(path.read_text(encoding="utf-8-sig") for path in key_store_paths)

    assert "ArgumentList" not in key_store_source
    assert "Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256)" not in key_store_source
    assert "Pbkdf2Sha256(password, salt, Iterations, 64)" in key_store_source


def test_prefab_stage_resource_uses_the_unity_2020_alias() -> None:
    content = source("MCPForUnity/Editor/Resources/Editor/GetPrefabStage.cs")

    assert "using PrefabStageUtility = UnityEditor.Experimental.SceneManagement.PrefabStageUtility;" in content


def test_asset_generation_cancel_responses_are_explicitly_object_typed() -> None:
    for relative_path in (
        "MCPForUnity/Editor/Tools/AssetGen/GenerateImage.cs",
        "MCPForUnity/Editor/Tools/AssetGen/GenerateModel.cs",
        "MCPForUnity/Editor/Tools/AssetGen/ImportModel.cs",
    ):
        assert "? (object)new SuccessResponse" in source(relative_path)


def test_asset_generation_dropdown_uses_newer_ui_toolkit_apis_only_when_available() -> None:
    content = source("MCPForUnity/Editor/Windows/Components/AssetGen/McpAssetGenSection.cs")

    assert "#if UNITY_2021_2_OR_NEWER" in content
    assert "formatDropdown.RegisterValueChangedCallback" in content
    assert "formatDropdown?.SetValueWithoutNotify" in content


def test_ui_document_serialization_tests_require_unity_2021_1_or_newer() -> None:
    content = source("TestProjects/UnityMCPTests/Assets/Tests/EditMode/Tools/UIDocumentSerializationTests.cs")

    assert content.startswith("#if UNITY_2021_1_OR_NEWER\n")
