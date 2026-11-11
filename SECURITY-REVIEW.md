# Budget Desk antivirus review — source cleanup and history

Current status: **11 November 2026 — release resumed at the owner's request after clean retests**. See [ANTIVIRUS-STATUS.md](ANTIVIRUS-STATUS.md) for the dated Malwarebytes fix notice and local result. The retained 1.0.1 executable is unchanged.

The sections below record the investigation that started on 7 November 2026.

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

The original application included an optional `--capture` developer command that cycled through five pages and called `Graphics.CopyFromScreen` to create test screenshots. It was inactive during ordinary launches. VirusTotal's capability analysis identified screen capture. The 1.0.1 follow-up also has the same reported vendor labels after this capability was removed, so the screenshot helper is not an established cause. The vendors' exact detection triggers remain unknown.

The 1.0.1 candidate removes that command and capture code from the public executable. Existing screenshot helpers remain in separate UI test programs. Startup checks now drive the real application from a separate process. An executable-level check rejects screen-capture calls and native capture imports. Budgeting features, financial logic and the data format are unchanged.

This is a reduction of unnecessary application capabilities. It is not an antivirus bypass, a promise of changed scan results, or confirmation of a false positive. No packer, obfuscator, security exclusion or replacement trust certificate is introduced.

## Vendor resolution and release

On 10 November Malwarebytes Support reported a database fix. On 11 November the owner supplied a clean local five-object rescan and subsequently confirmed all versions clean and requested publication. The full record and the scope of that evidence are in ANTIVIRUS-STATUS.md.

Full version 1.0.1 retains every budgeting feature from 1.0.0. The five isolated component executables are separate diagnostic builds and are not the full release.

The released application retains SHA-256 `2b312bd79f3a835296d8f3f7b0386911a7c449e46f1b3106850de93c968fa7cd`, produced by [Windows run 42411149445](https://github.com/DimitrisLai/Home-Budget/actions/runs/42411149445) from commit `67a5e038329bb0430066a802e0821ebe7a7bf031`. The release workflow checks the reviewed archive hash and every current application source/configuration input, reruns native checks and repackages the same executable with updated documentation.
