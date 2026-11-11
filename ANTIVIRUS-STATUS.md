# Windows antivirus review

**Status: 11 November 2026 — the owner authorized release of the complete 1.0.1 application after reporting clean antivirus retests.**

## Resolution

- On 10 November, Malwarebytes Support reported that its research team had released a database fix for the submitted detections.
- A local Custom Scan on 11 November at 22:07 Czech time completed with 5 objects scanned, 0 threats detected and 0 quarantined. Malwarebytes version: 5.7.3.376; components: 167.0.5780; update package: 1.0.116845. Heuristics were enabled and PUP/PUM were set to Detect.
- The clean export lists the scanned folder rather than individual clean filenames or hashes. It records the local result; it is not a new per-file hash manifest.
- The owner subsequently confirmed that all versions were clean and requested selection of the most complete version and a GitHub update. Individual new VirusTotal reports for every historical sample were not independently retrieved as part of this release.
- The release hold is removed. Full version 1.0.1 retains all 1.0.0 budgeting features and uses the same reviewed application executable. The five component-isolation builds remain investigation material rather than release builds.

## Retained release identity

| Item | Value |
| --- | --- |
| Full application version | 1.0.1 |
| Executable SHA-256 | `2b312bd79f3a835296d8f3f7b0386911a7c449e46f1b3106850de93c968fa7cd` |
| Original checked candidate ZIP SHA-256 | `7003cf18e4de56a5c6712adf8165fda70fe28c1357dac244409a8bb249dff390` |
| Original Windows checks | [Run 42411149445](https://github.com/DimitrisLai/Home-Budget/actions/runs/42411149445), 100 passed |

The final release ZIP has updated documentation and manual-test files, so its archive hash differs from the original candidate ZIP. Its application executable remains byte-identical. Packaging verifies all application source inputs, reruns the Windows checks and includes current test evidence and checksums.

## Investigation history

The original 1.0.0 and candidate 1.0.1 executables were reported by Malwarebytes, SecureAge and Trapmine in early November. Removing the developer screenshot helper did not by itself remove those reports. All five diagnostic controls were also reported locally, including a minimal window without the budgeting modules. Malwarebytes was supplied exact sample hashes, VirusTotal links, the original local report and a diagnostic manifest.

The original 1.0.0 release and its source history are retained as historical records. The current full download is 1.0.1. See SECURITY-REVIEW.md for the source cleanup and VALIDATION.md for functional test scope. Private support logs and device details are not included in this repository.
