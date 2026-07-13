# TGS Unity 2020.3 Distribution Branch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Maintain a copy-ready TGS package on `features/tgs-unity-2020-3` while preserving normal merge ancestry with the canonical `features/unity-2020-3` port.

**Architecture:** Bootstrap the TGS branch from the pinned pre-upstream port commit, replace only `MCPForUnity/` with the current external TGS package, and record its identity/customizations as a downstream commit. After the canonical v10 port is verified, merge it into the TGS branch, normalize newly added package files to the TGS identity, and verify that `MCPForUnity/` can be copied directly to the TGS project.

**Tech Stack:** Git branches/worktrees, Unity 2020.3.40f1, C# 8, Unity Test Framework, Python source-policy tests.

## Global Constraints

- `features/unity-2020-3` remains the vendor-neutral canonical port.
- `features/tgs-unity-2020-3` contains the full repository and the copy-ready TGS package under `MCPForUnity/`.
- Keep TGS package identity and namespaces under `com.tgs.*` only on the TGS branch.
- Preserve independent package version, `mcpServerVersion`, `required`, repository metadata, TGS README/changelog/license presentation, and TGS-specific runtime or CI behavior.
- Do not import or merge the unrelated external TGS Git history.
- Use external SSD content only for the initial baseline and final deployment comparison.
- Do not modify the external TGS project during bootstrap or synchronization.

## File Map

- `MCPForUnity/**`: exact TGS distribution payload.
- `MCPForUnity/package.json`: `com.tgs.mcp-for-unity`, independent version, server pin, and TGS metadata.
- `MCPForUnity/Editor/com.tgs.mcp-for-unity.editor.asmdef`: TGS Editor assembly identity.
- `MCPForUnity/Runtime/com.tgs.mcp-for-unity.asmdef`: TGS Runtime assembly identity.
- `tools/tgs_identity.py`: deterministic validation/normalization of TGS identity for files added by future canonical merges.
- `tools/tests/test_tgs_identity.py`: regression tests for namespace, assembly, and package transformations.

---

### Task 1: Bootstrap the maintained TGS branch from the current package

**Files:**
- Replace on dedicated branch: `MCPForUnity/**`

**Interfaces:**
- Consumes: canonical baseline commit `94234fa7`; external package `/Volumes/PRO-G40/tgs/tgs-projects/tgs-template-all/project_template/Assets/TGSPackageManager/packages/tools`.
- Produces: `features/tgs-unity-2020-3` with the external TGS package at `MCPForUnity/` and shared ancestry with the canonical port.

- [ ] **Step 1: Create the branch and an isolated worktree at the pinned baseline**

Run:

```bash
git branch features/tgs-unity-2020-3 94234fa7
git worktree add /private/tmp/unity-mcp-tgs-distribution features/tgs-unity-2020-3
```

Expected: the branch points to `94234fa7`; the worktree opens on `features/tgs-unity-2020-3` without changing the canonical checkout.

- [ ] **Step 2: Replace only the package tree with the external baseline**

From the TGS worktree, remove the tracked canonical package and copy the external package while excluding repository-local files:

```bash
cd /private/tmp/unity-mcp-tgs-distribution
git rm -r MCPForUnity
rsync -a --exclude=.git --exclude=.DS_Store --exclude=.claude /Volumes/PRO-G40/tgs/tgs-projects/tgs-template-all/project_template/Assets/TGSPackageManager/packages/tools/ MCPForUnity/
```

Expected: files outside `MCPForUnity/` are unchanged; `MCPForUnity/package.json` names `com.tgs.mcp-for-unity` and pins `mcpServerVersion` to `9.6.8`.

- [ ] **Step 3: Verify the copied payload against the SSD source**

Run:

```bash
diff -qr --exclude=.git --exclude=.DS_Store --exclude=.claude /Volumes/PRO-G40/tgs/tgs-projects/tgs-template-all/project_template/Assets/TGSPackageManager/packages/tools MCPForUnity
git diff --name-only -- . ':!MCPForUnity'
```

Expected: both commands print nothing.

- [ ] **Step 4: Commit the baseline TGS customization**

Run:

```bash
git add MCPForUnity
git commit -m "feat: bootstrap TGS Unity 2020 package distribution"
```

Expected: one commit contains only `MCPForUnity/` identity, metadata, and downstream package differences.

---

### Task 2: Add deterministic TGS identity validation

**Files:**
- Create: `tools/tgs_identity.py`
- Create: `tools/tests/test_tgs_identity.py`

**Interfaces:**
- Consumes: a package root and source text.
- Produces: `transform_text(path: Path, text: str) -> str`, `transform_package(package_root: Path) -> list[Path]`, and validation tests used after canonical merges.

- [ ] **Step 1: Write failing transformation tests**

Create `tools/tests/test_tgs_identity.py`:

```python
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
```

- [ ] **Step 2: Run the tests and verify the missing module failure**

Run:

```bash
uv run pytest tools/tests/test_tgs_identity.py -v
```

Expected: collection fails with `ModuleNotFoundError: No module named 'tools.tgs_identity'`.

- [ ] **Step 3: Implement the identity transformation**

Create `tools/tgs_identity.py`:

```python
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
```

- [ ] **Step 4: Run the tests and fix only transformation defects**

Run:

```bash
uv run pytest tools/tests/test_tgs_identity.py -v
```

Expected: all three tests pass.

- [ ] **Step 5: Commit the identity tooling**

Run:

```bash
git add tools/tgs_identity.py tools/tests/test_tgs_identity.py
git commit -m "test: codify TGS package identity transformations"
```

---

### Task 3: Merge the verified canonical v10 port into the TGS branch

**Files:**
- Merge: `features/unity-2020-3`
- Normalize: newly added or upstream-renamed files under `MCPForUnity/**`

**Interfaces:**
- Consumes: verified canonical port merged into `features/unity-2020-3` after completion of the canonical plan.
- Produces: TGS package with server version `10.0.0`, Unity `2020.3`, C# 8, and TGS identity.

- [ ] **Step 1: Merge the canonical port without committing**

Run from the TGS worktree:

```bash
git merge --no-commit --no-ff features/unity-2020-3
```

Expected: conflicts are limited to `MCPForUnity/` and documentation/plan files independently changed on both branches.

- [ ] **Step 2: Resolve behavioral conflicts before mechanical identity changes**

For each conflict under `MCPForUnity/`, keep the canonical v10 implementation and Unity 2020 compatibility behavior. Retain TGS-only code only when it is not equivalent to a canonical implementation. Remove all conflict markers, then run:

```bash
git diff --name-only --diff-filter=U
git diff --check
```

Expected: both commands print nothing.

- [ ] **Step 3: Apply identity transformation to new canonical files**

Run:

```bash
python3 -c 'from pathlib import Path; from tools.tgs_identity import transform_package; print("\n".join(str(p) for p in transform_package(Path("MCPForUnity"))))'
```

Expected: the command lists only files whose canonical identity was newly introduced by the merge.

- [ ] **Step 4: Ensure only TGS assembly-definition filenames remain**

Run each applicable rename when its canonical source path exists:

```bash
git mv MCPForUnity/Editor/MCPForUnity.Editor.asmdef MCPForUnity/Editor/com.tgs.mcp-for-unity.editor.asmdef
git mv MCPForUnity/Editor/MCPForUnity.Editor.asmdef.meta MCPForUnity/Editor/com.tgs.mcp-for-unity.editor.asmdef.meta
git mv MCPForUnity/Runtime/MCPForUnity.Runtime.asmdef MCPForUnity/Runtime/com.tgs.mcp-for-unity.asmdef
git mv MCPForUnity/Runtime/MCPForUnity.Runtime.asmdef.meta MCPForUnity/Runtime/com.tgs.mcp-for-unity.asmdef.meta
```

After the renames, verify the canonical paths are absent and the TGS paths exist:

```bash
test ! -e MCPForUnity/Editor/MCPForUnity.Editor.asmdef
test ! -e MCPForUnity/Runtime/MCPForUnity.Runtime.asmdef
test -e MCPForUnity/Editor/com.tgs.mcp-for-unity.editor.asmdef
test -e MCPForUnity/Runtime/com.tgs.mcp-for-unity.asmdef
```

Expected: all four checks exit `0`; no duplicate asmdefs remain.

- [ ] **Step 5: Verify identity and commit the merge**

Run:

```bash
rg -n 'namespace MCPForUnity|using MCPForUnity|"MCPForUnity\.(Editor|Runtime)"|com\.coplaydev\.unity-mcp' MCPForUnity
uv run pytest tools/tests/test_tgs_identity.py tools/tests/test_csharp8_compat.py -v
git diff --check
git add MCPForUnity tools
git commit -m "merge: update TGS distribution from Unity 2020 port"
```

Expected: the identity search prints nothing; all source-policy tests pass; the commit is a merge commit.

---

### Task 4: Validate the copy-ready TGS package under Unity 2020

**Files:**
- Verify: `MCPForUnity/**`
- Verify: `TestProjects/UnityMCPTests/**`

**Interfaces:**
- Consumes: TGS package identity and canonical Unity 2020 behavior.
- Produces: test evidence and a package tree ready for manual copy to the SSD project.

- [ ] **Step 1: Point the test project at the TGS package name**

On the TGS branch only, change the local dependency key in `TestProjects/UnityMCPTests/Packages/manifest.json`:

```json
"com.tgs.mcp-for-unity": "file:../../../MCPForUnity"
```

Update test asmdef references from `MCPForUnity.Editor`/`MCPForUnity.Runtime` to `com.tgs.mcp-for-unity.editor`/`com.tgs.mcp-for-unity`.

- [ ] **Step 2: Run the complete Unity 2020 EditMode suite**

Run:

```bash
/Applications/Unity/Hub/Editor/2020.3.40f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath TestProjects/UnityMCPTests -runTests -testPlatform EditMode -testResults /tmp/tgs-editmode-2020.xml -logFile /tmp/tgs-editmode-2020.log -quit
```

Expected: Unity exits `0`; XML reports zero failures; the log contains no `error CS` entries.

- [ ] **Step 3: Commit TGS test-harness identity changes**

Run:

```bash
git add TestProjects/UnityMCPTests
git commit -m "test: validate TGS package identity on Unity 2020"
```

- [ ] **Step 4: Compare against the external project without writing to it**

Run:

```bash
diff -qr --exclude=.git --exclude=.DS_Store --exclude=.claude MCPForUnity /Volumes/PRO-G40/tgs/tgs-projects/tgs-template-all/project_template/Assets/TGSPackageManager/packages/tools
```

Expected: differences represent the intentional v10 update and documented TGS changes. Save the output in the handoff; do not copy to the external project automatically.

- [ ] **Step 5: Verify branch ancestry and cleanliness**

Run:

```bash
git merge-base --is-ancestor features/unity-2020-3 HEAD
git branch --show-current
git diff --check
git status --short
```

Expected: ancestry exits `0`; branch is `features/tgs-unity-2020-3`; the last two commands print nothing.
