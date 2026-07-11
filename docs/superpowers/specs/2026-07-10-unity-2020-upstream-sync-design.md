# Unity 2020.3 Upstream Sync Design

## Goal

Update `features/unity-2020-3` from `refs/remotes/upstream/main` while keeping `MCPForUnity` compatible with Unity 2020.3 and C# 8, then maintain a second branch containing the final TGS distribution.

## Current State

- `features/unity-2020-3` is based on Coplay MCP for Unity 9.6.8 and contains the existing Unity 2020.3 compatibility port.
- `refs/remotes/upstream/main` is 286 commits ahead of the current branch's merge base and represents Coplay MCP for Unity 10.0.0.
- The upstream delta includes coordinated changes across `MCPForUnity`, the Python server, tests, documentation, and automation.
- The TGS package is also based on 9.6.8. Its later commits change package distribution metadata, documentation, and TGS identity; they do not contain a newer Unity compatibility port.

## Source of Truth

`features/unity-2020-3` is the canonical, vendor-neutral Unity 2020.3 port. Upstream changes flow into this branch first.

`features/tgs-unity-2020-3` is the downstream TGS distribution branch. It shares ancestry with the canonical port, contains the full repository, and modifies `MCPForUnity` with the TGS package identity and TGS-specific behavior. Its `MCPForUnity/` directory is the copy-ready package for the external TGS project.

The update flow is:

1. Merge the full `refs/remotes/upstream/main` history into an isolated update branch created from `features/unity-2020-3`.
2. Preserve upstream changes outside `MCPForUnity` unless they conflict with the Unity 2020.3 port or break server/tool symmetry.
3. Resolve and adapt changes inside `MCPForUnity` for Unity 2020.3 and C# 8.
4. Validate the Python server and Unity package tests that are available locally.
5. Merge the completed update into `features/unity-2020-3` only after review.
6. Merge the updated canonical port into `features/tgs-unity-2020-3`.
7. Resolve the TGS branch by preserving the new canonical behavior while reapplying its package identity, distribution metadata, and TGS-specific customizations.
8. Validate the TGS branch, then copy its `MCPForUnity/` directory into the external TGS project when desired.

## Compatibility Policy

- Keep `MCPForUnity/package.json` at minimum Unity version `2020.3`.
- Keep the C# language level compatible with C# 8.
- Preserve `jillejr.newtonsoft.json-for-unity` unless a verified Unity 2020.3-compatible replacement is required.
- Use compile-time version guards for Unity APIs that exist only in newer editor versions.
- Use focused compatibility shims when multiple call sites require the same cross-version behavior.
- Do not silently remove an upstream feature solely because its first implementation uses a newer Unity API. Adapt it when Unity 2020.3 exposes an equivalent behavior.
- If a feature fundamentally depends on an unavailable Unity 2020.3 capability, keep the public contract when practical and return an explicit unsupported response for that editor version.
- Preserve paired Python server changes so MCP tool schemas and C# handlers remain symmetric.

## Integration Strategy

Use a real merge of `refs/remotes/upstream/main`, rather than copying files or cherry-picking selected commits. This retains upstream ancestry and makes future synchronization incremental.

Perform the merge on a dedicated `codex/` update branch. Resolve conflicts by behavior, not by automatically choosing one side. For files changed by both the port and upstream:

- start from the upstream behavior;
- restore the Unity 2020.3 compatibility constraint;
- retain existing port-specific fixes where the upstream implementation does not supersede them;
- add or update tests for each non-mechanical adaptation.

Do not merge the unrelated TGS repository history into the canonical port branch. TGS namespace, assembly-name, package-name, `required`, repository metadata, and `mcpServerVersion` changes belong only to `features/tgs-unity-2020-3`.

## TGS Distribution Branch

Bootstrap `features/tgs-unity-2020-3` from the current `features/unity-2020-3` head. Replace only its `MCPForUnity/` directory with the current package from the external TGS project and record that replacement as the initial TGS customization commit. Do not import the external repository's unrelated Git history.

After bootstrap, all TGS updates use normal Git merges:

1. Complete and validate an upstream port on `features/unity-2020-3`.
2. Merge `features/unity-2020-3` into `features/tgs-unity-2020-3`.
3. Resolve conflicts in favor of the new port behavior while retaining documented TGS deltas.
4. Commit any newly required TGS adaptations separately from the merge when practical.

The TGS branch must preserve these downstream deltas:

- package name and namespaces under `com.tgs.*`;
- TGS assembly names;
- independent package version plus `mcpServerVersion`;
- `required` and repository metadata;
- TGS-specific runtime, editor, or CI behavior;
- TGS README, changelog, and license presentation.

The branch is a maintained deliverable, not a disposable snapshot. The external SSD copy is a deployment target after bootstrap, not a required source for future comparisons.

## Work Decomposition

The implementation is split into independently reviewable phases:

1. Merge and inventory conflicts.
2. Restore package manifest and assembly compatibility.
3. Port shared runtime compatibility helpers.
4. Port Editor services, transports, clients, and setup UI.
5. Port MCP tools and resources, including new upstream features.
6. Port optional integrations and Asset Generation with explicit version/dependency handling.
7. Update Unity 2020.3 tests and resolve server/tool symmetry issues.
8. Run full available verification and document any tests that require an installed Unity 2020.3 Editor.
9. Bootstrap the TGS distribution branch and record its downstream deltas.
10. Merge the validated canonical update into the TGS branch and verify the copy-ready package.

Each phase must leave the branch buildable or have its incomplete state clearly contained in the update branch. Compatibility changes should be committed separately from unrelated upstream content where conflict resolution introduces new code.

## Validation

- Confirm the merge ancestry includes both the previous port head and `refs/remotes/upstream/main`.
- Run all Python tests from `Server/`.
- Run static searches for C# language constructs newer than C# 8 and direct references to Unity APIs unavailable in 2020.3.
- Open the Unity 2020.3 test project and run applicable EditMode tests when the editor is available.
- Verify `MCPForUnity/package.json` still declares Unity `2020.3` and compatible dependencies.
- Compare registered Python tools with C# command handlers after the port.
- Record any upstream feature intentionally unavailable in Unity 2020.3, including the exact technical limitation and user-visible behavior.

## Branch Verification

- Confirm `features/tgs-unity-2020-3` descends from `features/unity-2020-3` at bootstrap.
- Confirm future canonical updates appear as merge ancestry in the TGS branch.
- Compare `features/tgs-unity-2020-3:MCPForUnity` with the external package after deployment; differences must be limited to intentionally external project files.
- Ensure no `com.tgs.*` identity changes leak back into `features/unity-2020-3`.
- Keep canonical port verification and TGS distribution verification as separate checkpoints so downstream branding and deployment concerns do not obscure Unity compatibility failures.
