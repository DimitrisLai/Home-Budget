# Budget Desk 1.0.1 review candidate

This candidate removes the developer-only screenshot command from the public Windows executable. Screenshot helpers remain in separate UI test programs, which are not included as executables in the download.

The actual app is built and tested on Windows. Startup checks now open and close it from a separate test process. Executable checks retain the .NET Framework 4.5 compatibility guard and reject screen-capture API calls or native imports.

All budgeting features, financial calculations, data paths and schema 4 are unchanged from 1.0.0. Close the previous app, export a JSON backup and extract the candidate into a new folder. Run BudgetDesk/BudgetDesk.exe, or Try-Demo.cmd for isolated sample data.

The original 1.0.0 antivirus report is under review. The exact detection trigger is unknown, and a successful functional build is not antivirus clearance. This candidate has not been published as a new stable release.

See SECURITY-REVIEW.md for sample details and VALIDATION.md for test scope. Licensed under the MIT License.
