# Windows antivirus review

**Status: 7 November 2026 — Windows downloads and automatic publishing are paused.**

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
- Malwarebytes confirmed receipt of the support request. Updated VirusTotal details, exact hashes, public source and build links have been sent for researcher review. An approved sample-upload channel has been requested if those references are insufficient.
- Researcher assessment and any vendor reclassification remain pending. No vendor clearance is claimed.

## Publishing status

The `RELEASE-HOLD` file pauses the automatic release workflow before it compiles or publishes a new Windows package. The application and financial data format are unchanged by this repository-status update.

Release promotion can resume after the reported detections have been assessed, the same reviewed executable has been rechecked, and the owner has accepted the Windows candidate. The source, build evidence and package hashes remain the basis for that review.
