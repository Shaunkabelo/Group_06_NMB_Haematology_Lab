# Doctor workflow changes

These changes address the Doctor audit on `subsystem/doctor`. They do not merge or rebuild teammates' subsystems. The existing local deletions of `LabManagerController.cs` and its dashboard were left untouched.

## Implemented

- Doctor authorization, no-store responses and antiforgery validation cover all Doctor endpoints. Same-origin page POST requests include the token.
- Doctor profile navigation uses the existing shared account/profile page. Password changes use the shared password policy, hash format, security-stamp update and sign-out. Existing hashes produced by the old incompatible Doctor password endpoint may need the shared Forgot password flow.
- Doctor-created patient passwords are generated on the server, stored as PBKDF2 hashes, and emailed from an encrypted queue. Registration sets `MustChangePassword = 1`. The account and its notification commit together. Capture medical conditions, allergies and medication on the Patients page after registration.
- Requests, details and patient history read SQL Server instead of browser storage. Reads enforce ownership or active consent. Both current per-test consent and legacy request grants are supported; PDFs and result reads filter individual tests. Old browser-cached request/results data is discarded.
- Creation validates urgency, distinct tests, sample type and barcode limits. Active test/sample matching and barcode reuse are checked inside a transaction; the request, every requested test, samples and notification are committed together.
- Results have PDF downloads and release/email controls. Only the requesting doctor can release a completed request. All tests must have results independently verified by another technician after their capture time. Release and its notification commit together. Releasing an already-released request is rejected; subsequent patient emails use the email action.
- The doctor chooses a results PDF email or an appointment invitation without an attachment, and supplies a patient note. The release makes results available to the patient in either case, as specified.
- Alerts use five calendar dates including today and an exclusive next-day end boundary. Request summary PDFs are still available and count STAT as urgent.
- Cancellation supports the database's `Sample(s) received` status as well as `Samples Received`. The queued cancellation notification includes the reason. The trigger covers all future cancellations, including those performed by a technician.

## Database deployment still required

No database objects or records were changed during implementation. Read-only checks inspected the actual schema and compiled the new read/verification queries using non-existent IDs.

1. Back up/review the intended database through the normal team process. Confirm it is the database configured by `ConnectionStrings:conn`.
2. Open **`Database/DoctorWorkflow_Deploy.sql`** in SSMS. Select the application database, set `@ExpectedDatabase` at the top to its exact name, and execute the entire file. This is the combined deployment entry point: you do **not** need to run the individual queue and cancellation scripts as well.
3. The combined script checks prerequisites, creates or validates the email queue/indexes/FK, installs the cancellation trigger, updates the cancellation procedure, and verifies the installed modules in one transaction. It preserves existing records and pending emails, can be rerun on a compatible schema, and rolls back deployment changes on failure. Incompatible pre-existing objects are reported rather than silently replaced. Optional `@ValidateOnly = 1` checks prerequisites/schema without deploying; change it back to `0` to install.
4. `Database/DoctorWorkflow_Preflight.sql` remains available for later read-only operational checks. Existing authentication, patient lookup and medical-history procedures must remain available; the older `VerifySubsystemProcedures.sql` lists their broader prerequisites. The new registration, creation and release workflows require the queue table and intentionally roll back if their notification cannot be queued.
5. Configure `Email:Host`, `Email:Port`, `Email:From`, and any SMTP username/password through user secrets or the hosting environment. SMTP uses TLS. Configure `AccountSecurity:PublicBaseUrl` to the trusted HTTPS deployment URL.
6. Preserve the ASP.NET Data Protection key ring and application identity across restarts/deployments. The queue's email bodies, including temporary passwords, are protected using those keys. Lost keys make pending messages unreadable. In a multi-instance deployment, share the key ring and application identity between instances.
7. Use HTTPS. The existing shared cookie configuration requires it. Existing incompatible passwords should be reset, not copied or converted manually.

The new repository uses parameterized Dapper SQL directly. The application database principal needs the corresponding SELECT/INSERT/UPDATE permissions on the workflow tables and queue in addition to its existing procedure permissions; an EXECUTE-only account will need a scoped permission update.

The worker leases a pending message for two minutes, retries failures after five minutes, and clears its encrypted payload after successful delivery. Delivery is at-least-once: a process crash after SMTP accepts an email but before the database acknowledgement can cause a duplicate. Monitor pending counts/attempts with the preflight query and application logs. Do not remove the queue while its cancellation trigger remains installed.

## Validation

Run from the solution directory:

```powershell
dotnet build Group_06_NMB_Haematology_Diagnostic_Laboratory.sln
dotnet run --project Tests/AuthenticationChecks/AuthenticationChecks.csproj
dotnet run --project Tests/DoctorWorkflowChecks/DoctorWorkflowChecks.csproj
```

The Doctor checks run a local HTTP server with fake repositories and transport. They verify role boundaries, antiforgery, request validation, patient onboarding, release ownership, PDF generation, date boundaries, password compatibility, email retry behavior and Razor rendering. They do not send email or modify the database.

Optional read-only database checks (using the configured connection):

```powershell
dotnet run --project Tests/DoctorWorkflowChecks/DoctorWorkflowChecks.csproj -- --database-schema
dotnet run --project Tests/DoctorWorkflowChecks/DoctorWorkflowChecks.csproj -- --database-read-checks
```

Before acceptance, use a non-production database and test mail recipient to exercise registration/first login, a multi-test request, technician receipt and verification, cancellation, result release, delivery/retry, and consent grant/revocation across two doctors. Those write/SMTP scenarios have not been run against the shared database.

## Group-project boundaries

The Patient UI must consume the existing released status and consent tables. The Technician subsystem must maintain individual test/verification records. Their full workflows and SICT publication still need group integration and deployment verification; they are not implemented by restoring files from another branch.
