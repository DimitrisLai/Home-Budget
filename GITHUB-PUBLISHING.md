# Publish Budget Desk 1.0.0 on GitHub

## Repository

The project repository is [DimitrisLai/Home-Budget](https://github.com/DimitrisLai/Home-Budget). The app is named Budget Desk. The repository owner selected the MIT license; keep the existing LICENSE file when uploading or updating the project.

If recreating the repository, use these settings:

| Field | Value |
| --- | --- |
| Owner | DimitrisLai |
| Repository name | Home-Budget |
| Description | Offline Windows budget manager with accounts, recurring payments, savings goals, split purchases, templates and monthly/yearly reports. |
| Visibility | Public, to share the uploaded files |
| Add README | Off — this package supplies README.md |
| Add .gitignore | No .gitignore — this package supplies one |
| Add license | MIT License |

The README and ignore file are supplied with the project.

## Upload the project files

Extract the Windows ZIP. Open its BudgetDesk folder and use the repository's upload-files action. Put the following at the repository root, rather than nesting the entire BudgetDesk folder:

- Source/, Tests/ and Preview/.
- README.md, CHANGELOG.md, IDEAS.md, VALIDATION.md, RELEASE-NOTES.md, WINDOWS-CHECKLIST.md and GITHUB-PUBLISHING.md.
- .gitignore, BudgetDesk.exe.config and the .cmd launch/build/test files.
- LICENSE, preserving the repository owner's existing MIT license.

Keep BudgetDesk.exe and the downloadable ZIP as release binaries. SHA256SUMS.txt is for the complete Windows package, so it belongs in that package rather than a partial source upload. The package contains mock previews and test results; real budget files stay in the separate local data folder and are not part of this ZIP.

The ignore file helps keep compiled files, real budget JSON, backups and runtime logs out of future Git commits. When uploading through the website, explicitly select the listed project files.

Commit the initial upload to the default branch.

## Create the downloadable release

Open [Draft a new release](https://github.com/DimitrisLai/Home-Budget/releases/new) after the source is uploaded.

| Field | Value |
| --- | --- |
| Tag | v1.0.0 |
| Target | The default branch containing the uploaded source |
| Release title | Budget Desk 1.0.0 |
| Description | Copy RELEASE-NOTES.md |
| Attached asset | BudgetDesk_v1.0.0_Windows.zip — attach the ZIP itself |
| Pre-release | Unchecked |
| Set as latest | Checked |

Review the files and description, then Publish release. The ready-to-run Windows ZIP is the asset users should download. GitHub also generates source-code archives.

## License

The source and Windows ZIP include the same MIT LICENSE as the repository, with copyright (c) 2026 DimitrisLai. Retain that notice and the license with distributed copies.

## Official references

- [Creating a repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)
- [Managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
- [Licensing a repository](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)
