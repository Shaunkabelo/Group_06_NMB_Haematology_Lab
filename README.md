# Group_06_NMB_Haematology_Lab
Nelson Mandela Bay Haematology Diagnostic Laboratory MVC Project

## Database configuration for deployment

The application reads its SQL Server connection string from `ConnectionStrings:conn`.
Do not put a real database password in `appsettings.json` or source control.

- Local development: use .NET User Secrets from the web project directory with
  `dotnet user-secrets set "ConnectionStrings:conn" "<your SQL Server connection string>"`.
- IIS deployment: configure `ConnectionStrings__conn` as a protected environment
  variable for the application pool (or use the hosting provider's secret store),
  then recycle the pool. The app fails at startup with a clear configuration error
  if the value is missing.
- Rotate the SQL login password previously embedded in this repository. It should
  be treated as exposed. Give the application login only the database permissions
  required by its stored procedures.

Before publishing, select the target database in SSMS and run
`Database/Authentication_PasswordRecovery.sql` for the additive authentication
tables and procedures. Then run `Database/VerifySubsystemProcedures.sql`. This
preflight reports and fails if any procedure called by the application or its
test-request table type is missing. The other subsystem procedures must already
exist in the target database; they are not defined in this repository. The app
cannot be certified against the live database until this script passes there.
