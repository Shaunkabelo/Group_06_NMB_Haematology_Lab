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
- Configure outbound email using protected `Email__Host`, `Email__Port`,
  `Email__From`, `Email__Username`, and `Email__Password` environment variables.
  Configure `AccountSecurity__PublicBaseUrl` to the public HTTPS site origin
  (for example `https://lab.example.org`, with no path, query, or fragment) for
  reset links and account creation emails.
- Rotate the SQL login password previously committed to this repository. It must
  be treated as exposed. Give the application login only the database permissions
  required by its stored procedures.

Publish the web project with:

```sh
dotnet publish Group_06_NMB_Haematology_Diagnostic_Laboratory/Group_06_NMB_Haematology_Diagnostic_Laboratory/Group_06_NMB_Haematology_Diagnostic_Laboratory.csproj -c Release -o publish
```

Deploy the contents of `publish` to the server, configure
the environment variables above for the app process, and enable HTTPS at the
server or trusted reverse proxy. On IIS, install the matching .NET 8 Hosting
Bundle, set `ASPNETCORE_ENVIRONMENT=Production`, and run the app pool under an
identity that can reach SQL Server. Confirm the hosting provider forwards HTTPS
correctly when TLS terminates at a reverse proxy.

Before publishing, select the target database in SSMS and run
`Database/Authentication_PasswordRecovery.sql` for the additive authentication
tables and procedures. Run `Database/spTestRequest_CancelByDoctor.sql` to enable
doctors to cancel their own submitted requests and record the authenticated doctor,
reason, and cancellation date. Then run
`Database/VerifySubsystemProcedures.sql`. This preflight reports and fails if any procedure called by the application or its
test-request table type is missing. The other subsystem procedures must already
exist in the target database; they are not defined in this repository. The app
cannot be certified against the live database until this script passes there.
