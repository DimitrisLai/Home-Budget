# Automatic Budget Desk releases

Repository: [DimitrisLai/Home-Budget](https://github.com/DimitrisLai/Home-Budget).

## What happens automatically

The **Build and publish Windows release** workflow compiles the source on Windows, runs the existing accounting, compatibility, UI and quick-entry/search suites, and starts the real executable with separate empty/demo data. It packages the tested app with source, documentation, screenshots and the repository's MIT license.

The workflow verifies all internal checksums and the completed ZIP. It creates a draft release, downloads and checks the uploaded assets, then publishes the release as latest. It does not overwrite an existing version. Failed builds or checks stop publication.

Users download the attached **BudgetDesk_v1.0.0_Windows.zip** from [Releases](https://github.com/DimitrisLai/Home-Budget/releases), extract it and run BudgetDesk.exe. The second asset, **SHA256SUMS.txt**, contains the ZIP checksum. GitHub also supplies source-code archives.

## Prepare the next version

1. Finish and test the intended application changes.
2. Update application/manifest version identifiers, launch labels, README, CHANGELOG and RELEASE-NOTES.md for that version.
3. Set **VERSION** to the matching three-part number, for example **1.0.1**, and commit the prepared changes to **main** together.
4. GitHub creates tag **v1.0.1**, builds the matching Windows ZIP, verifies its downloads and publishes the release. No manual release creation or ZIP upload is required.

The workflow also runs when its build/publishing files change, allowing a failed setup to be fixed. A manual **Run workflow** action is available for retries. An existing published release remains unchanged; publish a new version for changed application files.

## Build details

- Workflow: **.github/workflows/release.yml**.
- Build, checks and packaging: **tools/Build-WindowsRelease.ps1**.
- Stable version marker: **VERSION**.
- Native evidence included in each ZIP: **Tests/Windows-CI-*.txt**.
- Microsoft .NET Framework 4.5 reference package: version **1.0.3**, with the SHA-256 recorded in VALIDATION.md and checked before compilation.
- Build job: read access. Publishing job: repository contents write access through GitHub's built-in token. No personal access token is needed.

The existing Rebuild-Windows.cmd remains an optional local rebuild command for users who download source.

## License

The source and Windows ZIP include the same MIT LICENSE selected by the repository owner, with copyright (c) 2026 DimitrisLai. Retain that notice and the license with distributed copies.
