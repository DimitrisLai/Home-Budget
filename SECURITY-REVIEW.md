# Budget Desk antivirus review

Review started 7 November 2026. This records public sample facts and a source cleanup, without claiming antivirus clearance.

## Reported sample

| Item | Value |
| --- | --- |
| Original executable | BudgetDesk.exe v1.0.0, 215,552 bytes |
| Executable SHA-256 | `87b4f64d4043309f14286844873b943ac236b6f47d442eda291ee343632dcb79` |
| Original Windows ZIP SHA-256 | `2f21dc19e263827aa0783a0ac1c6860061c3344b140c809eed133fead111a0e4` |
| Public source/build | [v1.0.0 source](https://github.com/DimitrisLai/Home-Budget/tree/v1.0.0/Source), [Windows build](https://github.com/DimitrisLai/Home-Budget/actions/runs/41706705308) |

The user's [VirusTotal report](https://www.virustotal.com/gui/file/87b4f64d4043309f14286844873b943ac236b6f47d442eda291ee343632dcb79?nocache=1) matches the official GitHub executable hash exactly. The locally quarantined copy has not been independently hashed.

At inspection, VirusTotal showed 3/71 detections: Malwarebytes `MachineLearning/Anomalous.95%`, SecureAge `Malicious`, and Trapmine `Suspicious.low.ml.score`. Results can change. Other engines reporting undetected does not establish safety. Some sandbox analyses were still in progress.

The executable is an unsigned managed .NET application. The downloaded Windows artifact, archive checksums and tagged source were checked for consistency. This establishes the identity of the reviewed public sample, not its safety.

## Source finding and cleanup

The original application included an optional `--capture` developer command that cycled through five pages and called `Graphics.CopyFromScreen` to create test screenshots. It was inactive during ordinary launches. VirusTotal's capability analysis identified screen capture. That capability is a plausible contributor to suspicion, but the vendors' exact detection triggers are unknown.

The 1.0.1 candidate removes that command and capture code from the public executable. Existing screenshot helpers remain in separate UI test programs. Startup checks now drive the real application from a separate process. An executable-level check rejects screen-capture calls and native capture imports. Budgeting features, financial logic and the data format are unchanged.

This is a reduction of unnecessary application capabilities. It is not an antivirus bypass, a promise of changed scan results, or confirmation of a false positive. No packer, obfuscator, security exclusion or replacement trust certificate is introduced.

## Vendor status

A suspected false-positive request containing the original public sample hash, release/source/build links and this source finding was entered in the official Malwarebytes support form. Malwarebytes requires the account owner's email verification before it submits the request automatically. Researcher review and any reclassification remain pending.

The 1.0.1 build is a review candidate. It is not published as a new stable release, and no vendor clearance is claimed. After functional testing and vendor review, an accepted candidate can be promoted through the release workflow.
