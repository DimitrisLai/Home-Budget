# Windows antivirus review

**Status: 8 November 2026 — Windows downloads and automatic publishing are paused.**

Malwarebytes, SecureAge and Trapmine have reported detections for the original v1.0.0 executable and the v1.0.1 review candidate. A false positive has not been confirmed, and the exact detection triggers are unknown.

The v1.0.0 Windows release is being retained as a draft during review. Version 1.0.1 remains a [draft review candidate](https://github.com/DimitrisLai/Home-Budget/pull/1). The original source tags and build records identify each sample.

## Reported samples

| Version | Executable SHA-256 | Report |
| --- | --- | --- |
| 1.0.0 | `87b4f64d4043309f14286844873b943ac236b6f47d442eda291ee343632dcb79` | [VirusTotal](https://www.virustotal.com/gui/file/87b4f64d4043309f14286844873b943ac236b6f47d442eda291ee343632dcb79?nocache=1) |
| 1.0.1 review candidate | `2b312bd79f3a835296d8f3f7b0386911a7c449e46f1b3106850de93c968fa7cd` | [VirusTotal](https://www.virustotal.com/gui/file/2b312bd79f3a835296d8f3f7b0386911a7c449e46f1b3106850de93c968fa7cd?nocache=1) |

Reported labels at inspection:

| Vendor | Label |
| --- | --- |
| Malwarebytes | `MachineLearning/Anomalous.95%` |
| SecureAge | `Malicious` |
| Trapmine | `Suspicious.low.ml.score` |

Results can change. Undetected results from other engines and successful functional tests do not establish antivirus clearance.

## Investigation and vendor review

- The original application included a developer-only screenshot command. The 1.0.1 candidate removes that capability from the public executable and retains screenshot helpers in separate test programs.
- Removing the screenshot code did not resolve the reported detections. It is not an established explanation for the flags.
- The candidate passed [100 native Windows functional and compatibility checks](https://github.com/DimitrisLai/Home-Budget/actions/runs/42411149445). Its packaged executable hash matches the report above.
- Malwarebytes Support acknowledged the earlier sample/source details on 8 November and confirmed forwarding the case to the appropriate department. The new hash-matched diagnostic scan findings and a privacy-minimized text report have also been sent. An approved sample-upload channel has been requested if source/build references are insufficient.
- Researcher assessment and any vendor reclassification remain pending. No vendor clearance is claimed.

## Diagnostic scan — 8 November 2026

A completed local Malwarebytes Custom Scan detected all five component-isolation controls. Every reported SHA-256 matches the original Windows diagnostic build.

Scanner 5.7.3.376; components 167.0.5780; update package 1.0.116703. The scan covered five file objects with heuristics enabled and reported five detections, zero quarantines. Memory/startup scanning was disabled.

| Control | Malwarebytes label |
| --- | --- |
| `01_Window` | `MachineLearning/Anomalous.100%` |
| `02_Model` | `MachineLearning/Anomalous.96%` |
| `03_Storage` | `MachineLearning/Anomalous.96%` |
| `04_Demo_UI` | `MachineLearning/Anomalous.95%` |
| `05_Full_Startup` | `MachineLearning/Anomalous.95%` |

The minimal `01_Window` control contains no Budget Desk model, storage, full UI or production startup source. Its detection shows that adding those financial features is not required for a flag in this batch. It does not identify a specific shared resource, compiler characteristic or classifier rule as the cause.

These findings strengthen the suspected-false-positive investigation without establishing vendor clearance. The [hash-matched result summary and sanitized report](https://github.com/DimitrisLai/Home-Budget/blob/3f150ea2d20201bb0f09a7ae1265209efeae70f8/Diagnostics/Results/Results_2026-11-08.md) contain the exact hashes and documented limitations. The diagnostic executables have not been rebuilt or replaced.

## Publishing status

The `RELEASE-HOLD` file pauses the automatic release workflow before it compiles or publishes a new Windows package. The application and financial data format are unchanged by this repository-status update.

Release promotion can resume after the reported detections have been assessed, the same reviewed executable has been rechecked, and the owner has accepted the Windows candidate. The source, build evidence and package hashes remain the basis for that review.
