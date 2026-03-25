# Winget Package Submission

Test Package is published to the [Windows Package Manager Community Repository](https://github.com/microsoft/winget-pkgs).

## How to submit/update

### Option A: Automated (via wingetcreate)

After a GitHub release is published, use `wingetcreate` to generate and submit the manifest:

```powershell
# Install wingetcreate
winget install Microsoft.WingetCreate

# Update the manifest and submit a PR to microsoft/winget-pkgs
# Replace VERSION and HASH with values from the release
wingetcreate update RWKSystems.TestPackage `
  --version 1.0.1 `
  --urls https://github.com/rwk-systems/test-package/releases/download/v1.0.1/TestPackageSetup.exe `
  --submit `
  --token YOUR_GITHUB_PAT
```

The `--submit` flag automatically forks `microsoft/winget-pkgs` and creates a PR.

### Option B: Manual

1. Copy `RWKSystems.TestPackage.yaml` from this directory
2. Update the `InstallerSha256` with the hash from the GitHub release
3. Update the `PackageVersion` and download URL
4. Fork [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)
5. Place the manifest at: `manifests/r/RWKSystems/TestPackage/1.0.1/RWKSystems.TestPackage.yaml`
6. Submit a PR

### Option C: GitHub Actions (after initial acceptance)

Once the package is accepted into winget-pkgs for the first time, you can automate
future updates by adding a `WINGET_CREATE_PAT` secret to the repo and uncommenting
the winget update step in `.github/workflows/release.yml`.

## First-time submission

The first submission requires manual review by the winget-pkgs maintainers.
Expect 1-3 days for review. After acceptance, updates are typically faster.

## Testing locally before submission

```powershell
winget validate winget\RWKSystems.TestPackage.yaml
```
