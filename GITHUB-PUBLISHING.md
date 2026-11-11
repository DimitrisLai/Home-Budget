# Automatic Budget Desk releases

Repository: [DimitrisLai/Home-Budget](https://github.com/DimitrisLai/Home-Budget).

## What happens automatically

The **Build and publish Windows release** workflow runs the existing accounting, compatibility, UI and quick-entry/search suites on Windows, and starts the real executable with separate empty/demo data and a log-fallback case. It packages the tested app with source, documentation, screenshots and the repository's MIT license. Pull requests run the checks without publishing; main-branch release pushes publish only after the checks pass.

For 1.0.1, it downloads the successful review artifact from run 42411149445, verifies the pinned ZIP and executable SHA-256 values, compares every application source file plus configuration and VERSION, and retains the exact reviewed executable. Later versions compile a new executable normally. Updating release documentation does not rebuild the retained 1.0.1 binary.

The workflow verifies all internal checksums and the completed ZIP. It creates a draft release, downloads and checks the uploaded assets, then publishes the release as latest. It does not overwrite an existing version. Failed builds or checks stop publication.

Users download the attached **BudgetDesk_v1.0.1_Windows.zip** from [Releases](https://github.com/DimitrisLai/Home-Budget/releases), extract it and run BudgetDesk.exe. The second asset, **SHA256SUMS.txt**, contains the ZIP checksum. GitHub also supplies source-code archives.

## Prepare the next version

1. Finish and test the intended application changes.
2. Update application/manifest version identifiers, launch labels, README, CHANGELOG and RELEASE-NOTES.md for that version.
3. Set **VERSION** to the matching unused three-part number, for example **1.0.2**, and commit the prepared changes to **main** together.
4. GitHub creates the matching version tag and Windows ZIP, verifies the downloads and publishes the release. No manual release creation or ZIP upload is required.

The workflow also runs when its build/publishing files change, allowing a failed setup to be fixed. A manual **Run workflow** action is available for retries. An existing published release remains unchanged; publish a new version for changed application files.

## Build details

- Workflow: **.github/workflows/release.yml**.
- Build, checks and packaging: **tools/Build-WindowsRelease.ps1**.
- Stable version marker: **VERSION**.
- Native evidence included in each ZIP: **Tests/Windows-CI-*.txt**.
- Microsoft .NET Framework 4.5 reference package: version **1.0.3**, with the SHA-256 recorded in VALIDATION.md and checked before compilation.
- Build job: contents/actions read access. Publishing job: repository contents write access through GitHub's built-in token. No personal access token is needed.

The optional RELEASE-HOLD marker still pauses publishing when deliberately present. It was removed for the owner-authorized 1.0.1 release on 11 November 2026.

The existing Rebuild-Windows.cmd remains an optional local rebuild command for users who download source.

## License

The source and Windows ZIP include the same MIT LICENSE selected by the repository owner, with copyright (c) 2026 DimitrisLai. Retain that notice and the license with distributed copies.
