# Unity 2020.3 Upstream Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Merge `refs/remotes/upstream/main` into the canonical Unity 2020.3 port and deliver MCP for Unity 10.0.0 with Unity 2020.3/C# 8 compatibility.

**Architecture:** Perform a real upstream merge on an isolated `codex/` branch so future updates retain Git ancestry. Resolve the three known textual conflicts by combining upstream behavior with the existing compatibility paths, then use a source-policy test and the installed Unity 2020.3.40f1 editor to find and verify non-conflicting compatibility regressions.

**Tech Stack:** Git, Unity 2020.3.40f1, C# 8, Unity Test Framework 1.1.31, Python 3, pytest, uv/FastMCP.

## Global Constraints

- Keep `MCPForUnity/package.json` at minimum Unity version `2020.3`.
- Keep the C# language level compatible with C# 8.
- Preserve `jillejr.newtonsoft.json-for-unity` unless a verified Unity 2020.3-compatible replacement is required.
- Preserve paired Python server changes so MCP tool schemas and C# handlers remain symmetric.
- Adapt upstream features when Unity 2020.3 exposes equivalent behavior.
- Return an explicit unsupported response only when Unity 2020.3 fundamentally lacks the required capability.
- Do not introduce `com.tgs.*` identity changes on the canonical branch.
- Do not modify the external TGS project during this plan.

## File Map

- `MCPForUnity/package.json`: Unity floor and package dependencies.
- `MCPForUnity/Editor/Tools/Build/BuildTargetMapping.cs`: cross-version build-target API boundary.
- `MCPForUnity/Editor/Tools/Build/BuildSettingsHelper.cs`: `BuildTargetGroup`/`NamedBuildTarget` settings access.
- `MCPForUnity/Editor/Tools/ManageBuild.cs`: guarded build platform, subtarget, and settings behavior.
- `MCPForUnity/Editor/Windows/MCPForUnityEditorWindow.cs`: Asset Generation UI plus Unity 2020.3 UPM fallback.
- `MCPForUnity/Editor/Helpers/DropdownFieldCompat.cs`: Unity 2020.3 UXML compatibility.
- `MCPForUnity/Runtime/Helpers/Unity*Compat.cs`: shared API-version shims.
- `MCPForUnity/Editor/Services/AssetGen/**/*.cs`: new Asset Generation implementation requiring C# 8 normalization.
- `MCPForUnity/Editor/Tools/AssetGen/**/*.cs`: new Asset Generation tool handlers.
- `MCPForUnity/Editor/Windows/Components/AssetGen/**/*.cs`: new Asset Generation editor UI.
- `tools/tests/test_csharp8_compat.py`: source-policy regression test.
- `TestProjects/UnityMCPTests/ProjectSettings/ProjectVersion.txt`: Unity 2020.3 test editor pin.
- `TestProjects/UnityMCPTests/Packages/manifest.json`: Unity 2020-compatible test dependencies.
- `TestProjects/UnityMCPTests/Assets/Tests/EditMode/**/*.cs`: upstream and compatibility EditMode tests.

---

### Task 1: Create the isolated integration branch and record the merge

**Files:**
- Merge: all files changed by `refs/remotes/upstream/main`
- Resolve: `MCPForUnity/package.json`
- Resolve: `MCPForUnity/Editor/Tools/Build/BuildTargetMapping.cs`
- Resolve: `MCPForUnity/Editor/Windows/MCPForUnityEditorWindow.cs`

**Interfaces:**
- Consumes: `features/unity-2020-3` at or after design commit `94234fa7`; `refs/remotes/upstream/main` at `11836003`.
- Produces: `codex/sync-upstream-main-unity-2020`, containing both histories and no conflict markers.

- [ ] **Step 1: Verify the canonical branch is clean and create the integration branch**

Run:

```bash
git status --short
git switch -c codex/sync-upstream-main-unity-2020 features/unity-2020-3
```

Expected: the first command prints nothing; the second reports the new branch.

- [ ] **Step 2: Merge upstream without committing**

Run:

```bash
git merge --no-commit --no-ff refs/remotes/upstream/main
```

Expected: Git stops with content conflicts only in:

```text
MCPForUnity/Editor/Tools/Build/BuildTargetMapping.cs
MCPForUnity/Editor/Windows/MCPForUnityEditorWindow.cs
MCPForUnity/package.json
```

- [ ] **Step 3: Resolve the package manifest with upstream modules and Unity 2020 dependencies**

Set the manifest header and dependencies to:

```json
{
  "name": "com.coplaydev.unity-mcp",
  "version": "10.0.0",
  "displayName": "MCP for Unity",
  "description": "A bridge that connects AI assistants to Unity via the MCP (Model Context Protocol). Allows AI clients like Claude Code, Cursor, and VSCode to directly control your Unity Editor for enhanced development workflows.\n\nFeatures automated setup wizard, cross-platform support, and seamless integration with popular AI development tools.\n\nJoin Our Discord: https://discord.gg/y4p8KfzrN4",
  "unity": "2020.3",
  "documentationUrl": "https://github.com/CoplayDev/unity-mcp",
  "licensesUrl": "https://github.com/CoplayDev/unity-mcp/blob/main/LICENSE",
  "dependencies": {
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "jillejr.newtonsoft.json-for-unity": "13.0.102",
    "com.unity.test-framework": "1.1.31"
  },
```

Keep the upstream `keywords` and `author` sections unchanged below this block.

- [ ] **Step 4: Resolve build-target APIs with a Unity 2020 code path**

Keep upstream's validated enum parsing and VisionOS error messages. Guard `UnityEditor.Build` and `NamedBuildTarget` behind `UNITY_2021_2_OR_NEWER`, and expose the existing Unity 2020 method in the other branch:

```csharp
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

// Existing GetTargetGroup, GetUnknownBuildTargetMessage, and validation helpers remain shared.

#if UNITY_2021_2_OR_NEWER
public static NamedBuildTarget GetNamedBuildTarget(BuildTarget target)
{
    return NamedBuildTarget.FromBuildTargetGroup(GetTargetGroup(target));
}

public static string TryResolveNamedBuildTarget(string name, out NamedBuildTarget namedTarget)
{
    if (!TryResolveBuildTarget(name, out var buildTarget))
    {
        namedTarget = default;
        return GetUnknownBuildTargetMessage(name);
    }

    var targetGroup = GetTargetGroup(buildTarget);
    if (targetGroup == BuildTargetGroup.Unknown)
    {
        namedTarget = default;
        return $"Build target group could not be resolved for target '{buildTarget}'.";
    }

    namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
    return null;
}
#else
public static string TryResolveBuildTargetGroup(string name, out BuildTargetGroup targetGroup)
{
    if (!TryResolveBuildTarget(name, out var buildTarget))
    {
        targetGroup = BuildTargetGroup.Unknown;
        return GetUnknownBuildTargetMessage(name);
    }

    targetGroup = GetTargetGroup(buildTarget);
    return targetGroup == BuildTargetGroup.Unknown
        ? $"Build target group could not be resolved for target '{buildTarget}'."
        : null;
}
#endif
```

Retain the existing `UNITY_2021_2_OR_NEWER` guard in `ResolveSubtarget`; Unity 2020 returns `0`.

- [ ] **Step 5: Resolve the editor window with upstream Asset Generation and Unity 2020 UPM behavior**

Keep upstream's `ProductInfo`, `McpAssetGenSection`, Asset Generation panel/toggle, glTFast dependency row, and completion callbacks. Preserve these compatibility pieces:

```csharp
private static readonly HashSet<MCPForUnityEditorWindow> OpenWindows =
    new HashSet<MCPForUnityEditorWindow>();
```

Include `assetGenPanel` in the existing one-pass `SwitchPanel` implementation:

```csharp
if (clientsPanel != null) clientsPanel.style.display = panel == ActivePanel.Clients ? DisplayStyle.Flex : DisplayStyle.None;
if (depsPanel != null) depsPanel.style.display = panel == ActivePanel.Deps ? DisplayStyle.Flex : DisplayStyle.None;
if (advancedPanel != null) advancedPanel.style.display = panel == ActivePanel.Advanced ? DisplayStyle.Flex : DisplayStyle.None;
if (toolsPanel != null) toolsPanel.style.display = panel == ActivePanel.Tools ? DisplayStyle.Flex : DisplayStyle.None;
if (resourcesPanel != null) resourcesPanel.style.display = panel == ActivePanel.Resources ? DisplayStyle.Flex : DisplayStyle.None;
if (assetGenPanel != null) assetGenPanel.style.display = panel == ActivePanel.AssetGen ? DisplayStyle.Flex : DisplayStyle.None;
```

Guard upstream's `Client.AddAndRemove` implementation and retain the sequential Unity 2020 fallback:

```csharp
#if UNITY_2021_2_OR_NEWER
private static void BatchUpmAdd(string[] packageIds, Action onComplete = null)
{
    var request = UnityEditor.PackageManager.Client.AddAndRemove(packageIds, null);
    EditorUtility.DisplayProgressBar("Installing Packages", $"Installing {packageIds.Length} package(s)...", 0.5f);
    PollUpmRequest(request, "install", onComplete);
}
#else
private static void BatchUpmAdd(string[] packageIds, Action onComplete = null)
{
    if (packageIds == null || packageIds.Length == 0)
    {
        onComplete?.Invoke();
        return;
    }

    EditorUtility.DisplayProgressBar("Installing Packages", $"Installing {packageIds.Length} package(s)...", 0.5f);
    BatchUpmSequential(packageIds, 0, true, onComplete);
}
#endif
```

Apply the same guard to removal and retain `BatchUpmSequential` for both operations on Unity 2020.

- [ ] **Step 6: Confirm the merge is structurally resolved and commit it**

Run:

```bash
git diff --check
git diff --name-only --diff-filter=U
git add MCPForUnity/package.json MCPForUnity/Editor/Tools/Build/BuildTargetMapping.cs MCPForUnity/Editor/Windows/MCPForUnityEditorWindow.cs
git commit -m "merge: sync upstream main into Unity 2020 port"
```

Expected: both validation commands print nothing; the commit is a two-parent merge commit.

---

### Task 2: Add a C# 8 source-policy regression test

**Files:**
- Create: `tools/tests/test_csharp8_compat.py`
- Modify: C# files reported by the new test, especially `MCPForUnity/Editor/Services/AssetGen/**/*.cs`, `MCPForUnity/Editor/Tools/AssetGen/**/*.cs`, and `MCPForUnity/Editor/Windows/Components/AssetGen/**/*.cs`

**Interfaces:**
- Consumes: merged `MCPForUnity/**/*.cs` source tree.
- Produces: a pytest guard that rejects known C# 9+ syntax and a C# 8-clean source tree.

- [ ] **Step 1: Write the failing source-policy test**

Create:

```python
from pathlib import Path
import re

import pytest


ROOT = Path(__file__).resolve().parents[2]
CS_FILES = tuple(sorted((ROOT / "MCPForUnity").rglob("*.cs")))

FORBIDDEN = (
    ("target-typed new", re.compile(r"=\s*new\(\)")),
    ("is not pattern", re.compile(r"\bis\s+not\b")),
    ("record declaration", re.compile(r"^\s*(?:public|internal|private|protected)?\s*record\b", re.MULTILINE)),
    ("init accessor", re.compile(r"\binit\s*;")),
)


@pytest.mark.parametrize("label,pattern", FORBIDDEN)
def test_mcp_sources_stay_within_csharp8(label: str, pattern: re.Pattern[str]) -> None:
    violations = []
    for path in CS_FILES:
        for match in pattern.finditer(path.read_text(encoding="utf-8-sig")):
            line = path.read_text(encoding="utf-8-sig").count("\n", 0, match.start()) + 1
            violations.append(f"{path.relative_to(ROOT)}:{line}")

    assert violations == [], f"{label}: " + ", ".join(violations)
```

- [ ] **Step 2: Run the test and observe the C# 9 violations**

Run:

```bash
uv run pytest tools/tests/test_csharp8_compat.py -v
```

Expected: `target-typed new` fails and lists the newly merged Asset Generation files plus any automatically merged legacy files.

- [ ] **Step 3: Replace every target-typed constructor with its declared type**

Use explicit constructors. Representative required transformations are:

```csharp
private static readonly Dictionary<string, AssetGenJob> Jobs =
    new Dictionary<string, AssetGenJob>();
private static readonly Dictionary<string, Runner> Runners =
    new Dictionary<string, Runner>();
private static readonly List<string> _tickIds = new List<string>();
public CancellationTokenSource Cts = new CancellationTokenSource();
private readonly List<(string Id, Toggle Toggle)> modelEnableToggles =
    new List<(string Id, Toggle Toggle)>();
```

Apply the same declared-type expansion to every path printed by the failing test. Do not alter `where T : new()` generic constraints because they are valid before C# 8 and the test deliberately does not match them.

- [ ] **Step 4: Re-run the source-policy test**

Run:

```bash
uv run pytest tools/tests/test_csharp8_compat.py -v
```

Expected: all four parameterized cases pass.

- [ ] **Step 5: Commit the guard and syntax port**

Run:

```bash
git add tools/tests/test_csharp8_compat.py MCPForUnity
git commit -m "fix: keep merged Unity package compatible with C# 8"
```

---

### Task 3: Make the Unity test project exercise Unity 2020.3

**Files:**
- Modify: `TestProjects/UnityMCPTests/ProjectSettings/ProjectVersion.txt`
- Modify: `TestProjects/UnityMCPTests/Packages/manifest.json`
- Regenerate: `TestProjects/UnityMCPTests/Packages/packages-lock.json`

**Interfaces:**
- Consumes: installed editor `/Applications/Unity/Hub/Editor/2020.3.40f1/Unity.app`.
- Produces: a test project that resolves and compiles under Unity 2020.3.40f1.

- [ ] **Step 1: Pin the test project to the installed Unity 2020 editor**

Set `ProjectVersion.txt` to:

```text
m_EditorVersion: 2020.3.40f1
m_EditorVersionWithRevision: 2020.3.40f1 (ba48d4efcef1)
```

- [ ] **Step 2: Reduce the manifest to Unity 2020-compatible test dependencies**

Retain the local MCP package, Unity Test Framework, TextMeshPro, UGUI, and built-in modules. Remove editor integrations and feature packages pinned for newer editors, including `com.unity.ai.navigation`, `com.unity.feature.development`, `com.unity.visualscripting`, `com.unity.ide.windsurf`, and Timeline 1.7.5. The top of the dependency map must be:

```json
{
  "dependencies": {
    "com.coplaydev.unity-mcp": "file:../../../MCPForUnity",
    "com.unity.test-framework": "1.1.31",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.ugui": "1.0.0",
```

Keep the existing `com.unity.modules.*` entries required by the package and tests.

- [ ] **Step 3: Run Unity once to resolve packages and compile**

Run:

```bash
/Applications/Unity/Hub/Editor/2020.3.40f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath TestProjects/UnityMCPTests -logFile /tmp/unity-mcp-2020-compile.log
```

Expected: Unity resolves packages and writes `/tmp/unity-mcp-2020-compile.log`. Exit `0` means the merge already compiles; a non-zero exit must include concrete `error CS` diagnostics consumed by Task 4, not a 2021-only package-resolution failure.

- [ ] **Step 4: Record the compiler diagnostics before editing production code**

Run:

```bash
rg -n "error CS|Compilation failed|Package resolution failed" /tmp/unity-mcp-2020-compile.log
```

Expected: every remaining failure includes a file and line or a specific package-resolution message. Save this output in the task notes; do not suppress errors with broad assembly exclusions.

- [ ] **Step 5: Commit the Unity 2020 test harness configuration**

Run after package resolution has produced a lock file:

```bash
git add TestProjects/UnityMCPTests/ProjectSettings/ProjectVersion.txt TestProjects/UnityMCPTests/Packages/manifest.json TestProjects/UnityMCPTests/Packages/packages-lock.json
git commit -m "test: run package suite with Unity 2020.3"
```

---

### Task 4: Restore cross-version build and editor APIs

**Files:**
- Modify: `MCPForUnity/Editor/Tools/Build/BuildTargetMapping.cs`
- Modify: `MCPForUnity/Editor/Tools/Build/BuildSettingsHelper.cs`
- Modify: `MCPForUnity/Editor/Tools/Build/BuildRunner.cs`
- Modify: `MCPForUnity/Editor/Tools/ManageBuild.cs`
- Modify: `MCPForUnity/Editor/Windows/MCPForUnityEditorWindow.cs`
- Modify: `MCPForUnity/Editor/Windows/Components/ClientConfig/McpClientConfigSection.cs`
- Test: `TestProjects/UnityMCPTests/Assets/Tests/EditMode/Tools/BuildTargetMappingTests.cs`

**Interfaces:**
- Consumes: shared `BuildTargetMapping.GetTargetGroup(BuildTarget)` and version-specific settings-target types.
- Produces: identical MCP build behavior on Unity 2020.3 without compile-time references to `NamedBuildTarget`, `StandaloneBuildSubtarget`, or `AddAndRemoveRequest`.

- [ ] **Step 1: Add Unity 2020 assertions to the upstream build-target tests**

Add guarded assertions:

```csharp
#if !UNITY_2021_2_OR_NEWER
[Test]
public void TryResolveBuildTargetGroup_UsesLegacyBuildTargetGroup()
{
    string error = BuildTargetMapping.TryResolveBuildTargetGroup("windows64", out var group);

    Assert.IsNull(error);
    Assert.AreEqual(BuildTargetGroup.Standalone, group);
}

[Test]
public void ResolveSubtarget_ReturnsZeroWhenStandaloneBuildSubtargetIsUnavailable()
{
    Assert.AreEqual(0, BuildTargetMapping.ResolveSubtarget("server"));
}
#endif
```

- [ ] **Step 2: Run the build-target tests and verify the first compile/test failure**

Run:

```bash
/Applications/Unity/Hub/Editor/2020.3.40f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath TestProjects/UnityMCPTests -runTests -testPlatform EditMode -testFilter MCPForUnityTests.Editor.Tools.BuildTargetMappingTests -testResults /tmp/build-target-2020.xml -logFile /tmp/build-target-2020.log -quit
```

Expected before completing the port: compile failure or failing assertions tied to the newer build API path.

- [ ] **Step 3: Apply exact version boundaries at all build call sites**

In `BuildSettingsHelper` and `ManageBuild`, retain the existing pattern:

```csharp
#if UNITY_2021_2_OR_NEWER
string err = BuildTargetMapping.TryResolveNamedBuildTarget(targetName, out var settingsTarget);
#else
string err = BuildTargetMapping.TryResolveBuildTargetGroup(targetName, out var settingsTarget);
#endif
```

Use `settingsTarget` with the matching `PlayerSettings` overload. Guard `EditorUserBuildSettings.standaloneBuildSubtarget`, `StandaloneBuildSubtarget`, `BuildPlayerOptions.subtarget`, and `BuildOptions.CleanBuildCache` with `UNITY_2021_2_OR_NEWER`. Keep the Unity 2020 result deterministic (`subtarget = 0`, ignore `clean_build`).

In `MCPForUnityEditorWindow` retain `Client.Add`/`Client.Remove` sequential polling below 2021.2. In `McpClientConfigSection`, retain `DropdownFieldCompat` below 2021.2 and the real `DropdownField` on newer editors.

- [ ] **Step 4: Re-run the focused build tests**

Run the command from Step 2.

Expected: Unity exits `0`; `/tmp/build-target-2020.xml` contains zero failures.

- [ ] **Step 5: Commit the API compatibility restoration**

Run:

```bash
git add MCPForUnity/Editor/Tools/Build MCPForUnity/Editor/Tools/ManageBuild.cs MCPForUnity/Editor/Windows TestProjects/UnityMCPTests/Assets/Tests/EditMode/Tools/BuildTargetMappingTests.cs
git commit -m "fix: preserve Unity 2020 build and editor API paths"
```

---

### Task 5: Compile and test the new Asset Generation surface on Unity 2020

**Files:**
- Modify: `MCPForUnity/Editor/Helpers/AssetGenPaths.cs`
- Modify: `MCPForUnity/Editor/Helpers/AssetGenPrefs.cs`
- Modify: `MCPForUnity/Editor/Helpers/BlenderDetection.cs`
- Modify: `MCPForUnity/Editor/Security/SecureKeyStore/*.cs`
- Modify: `MCPForUnity/Editor/Services/AssetGen/**/*.cs`
- Modify: `MCPForUnity/Editor/Tools/AssetGen/*.cs`
- Modify: `MCPForUnity/Editor/Windows/Components/AssetGen/*.cs`
- Test: `TestProjects/UnityMCPTests/Assets/Tests/EditMode/AssetGen/*.cs`

**Interfaces:**
- Consumes: upstream Asset Generation tool contracts and `IHttpTransport`/provider interfaces.
- Produces: the same handlers on Unity 2020, with optional glTFast behavior discovered at runtime rather than required at compile time.

- [ ] **Step 1: Run the upstream Asset Generation tests under Unity 2020**

Run:

```bash
/Applications/Unity/Hub/Editor/2020.3.40f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath TestProjects/UnityMCPTests -runTests -testPlatform EditMode -testFilter AssetGen -testResults /tmp/assetgen-2020.xml -logFile /tmp/assetgen-2020.log -quit
```

Expected before API adaptation: either compilation errors with exact paths or failing tests that identify the unsupported behavior.

- [ ] **Step 2: Preserve optional dependency boundaries**

Keep glTFast access behind assembly/type discovery. No unguarded `using GLTFast;` or compile-time assembly reference may be added. Missing glTFast must return the upstream explicit message while FBX and image paths continue to work:

```csharp
Type gltfImportType = Type.GetType("GLTFast.GltfImport, glTFast");
if (gltfImportType == null)
{
    return new ErrorResponse(
        "glTFast is required to import .glb/.gltf files. Install com.unity.cloud.gltfast or use FBX.");
}
```

Retain the upstream `UnityWebRequest.result` path because the test floor is Unity 2020.3. Do not add a pre-2020.2 fallback that this branch cannot execute or verify.

- [ ] **Step 3: Keep secure storage fail-soft on older operating-system integrations**

The platform key stores must continue to implement `ISecureKeyStore`. When a native command is unavailable, return a failed capability result and allow `EncryptedFileKeyStore` fallback; do not throw during editor startup. Preserve upstream redaction tests and file-path validation unchanged unless a Unity 2020 API requires a guarded equivalent.

- [ ] **Step 4: Re-run Asset Generation tests**

Run the command from Step 1.

Expected: Unity exits `0`; `/tmp/assetgen-2020.xml` reports zero failures.

- [ ] **Step 5: Commit evidence-driven Asset Generation adaptations**

Run:

```bash
git add MCPForUnity/Editor/Helpers MCPForUnity/Editor/Security MCPForUnity/Editor/Services/AssetGen MCPForUnity/Editor/Tools/AssetGen MCPForUnity/Editor/Windows/Components/AssetGen TestProjects/UnityMCPTests/Assets/Tests/EditMode/AssetGen
git commit -m "fix: port asset generation to Unity 2020.3"
```

---

### Task 6: Verify server/tool symmetry and Python behavior

**Files:**
- Verify: `Server/src/services/tools/*.py`
- Verify: `Server/tests/*.py`
- Test: `Server/tests/test_tool_test_symmetry.py`

**Interfaces:**
- Consumes: upstream Python tools and merged C# handlers.
- Produces: matching registered MCP tools and Unity command handlers.

- [ ] **Step 1: Run the symmetry and new Asset Generation server tests**

Run:

```bash
cd Server
uv run pytest tests/test_tool_test_symmetry.py tests/test_asset_gen_image.py tests/test_asset_gen_import.py tests/test_asset_gen_import_file.py tests/test_asset_gen_model.py -v
```

Expected: all selected tests pass. A failure blocks the port; preserve the upstream Python contract and correct the paired C# handler before continuing.

- [ ] **Step 2: Run the complete Python suite**

Run:

```bash
cd Server
uv run pytest tests/ -v
```

Expected: zero failures.

- [ ] **Step 3: Record the passing server commands in the verification notes**

No Python production change is planned: the complete upstream server is merged unchanged. Record both commands and their pass counts for the final handoff; do not create an empty commit.

---

### Task 7: Run full canonical-port verification

**Files:**
- Verify: entire repository

**Interfaces:**
- Consumes: completed canonical port.
- Produces: fresh test evidence and a branch ready for review/merge into `features/unity-2020-3`.

- [ ] **Step 1: Run source-policy and Python tests**

Run:

```bash
uv run pytest tools/tests/test_csharp8_compat.py -v
cd Server
uv run pytest tests/ -v
```

Expected: zero failures in both commands.

- [ ] **Step 2: Run the full Unity 2020 EditMode suite**

Run:

```bash
/Applications/Unity/Hub/Editor/2020.3.40f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath TestProjects/UnityMCPTests -runTests -testPlatform EditMode -testResults /tmp/unity-mcp-editmode-2020.xml -logFile /tmp/unity-mcp-editmode-2020.log -quit
```

Expected: Unity exits `0`; the XML reports zero failed tests; the log has no `error CS` entries.

- [ ] **Step 3: Verify ancestry, manifest, and worktree**

Run:

```bash
git merge-base --is-ancestor refs/remotes/upstream/main HEAD
git merge-base --is-ancestor features/unity-2020-3 HEAD
rg -n '"unity": "2020.3"|jillejr.newtonsoft.json-for-unity' MCPForUnity/package.json
git diff --check
git status --short
```

Expected: both ancestry checks exit `0`; both manifest lines are present; `git diff --check` and `git status --short` print nothing.

- [ ] **Step 4: Present the verified integration branch for review**

Do not merge into `features/unity-2020-3` or push automatically. Report commit SHAs, test counts, any explicitly unsupported Unity 2020 features, and the exact integration branch name.
