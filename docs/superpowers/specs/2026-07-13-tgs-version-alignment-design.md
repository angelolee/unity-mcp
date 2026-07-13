# TGS Version Alignment Design

## Goal

Keep the TGS Unity package and its Python server dependency on the same release version as `upstream/main`, with one authoritative version field.

## Decision

`MCPForUnity/package.json` will expose only its standard `version` field. During every completed integration from `upstream/main`, the TGS branch retains the upstream version value unchanged.

The Editor UI, prerelease detection, and Python server pinning will all read that same field. The package will not carry `mcpServerVersion` or any other second version source.

## Boundaries

- This does not re-enable upstream update checks. TGS remains synchronized through the documented Git merge flow.
- This does not add network checks, a script, or CI automation. The integrator updates the version as part of resolving each upstream merge.
- The TGS package name, namespaces, assembly names, and other downstream identity changes remain independent of the version policy.

## Changes

1. Remove `mcpServerVersion` from the TGS package manifest and set `version` to the current upstream release, `10.0.0`.
2. Make the server-version lookup delegate to the package-version lookup.
3. Add a focused Unity test proving the manifest has no separate server-version field and that its package version is `10.0.0`.
4. Update the branch-integration design and package README with the single-version rule.

## Validation

- Run the affected Unity EditMode test when Unity is available.
- Confirm no production reference to `mcpServerVersion` remains.
- Confirm the package manifest `version` matches `upstream/main:MCPForUnity/package.json`.
