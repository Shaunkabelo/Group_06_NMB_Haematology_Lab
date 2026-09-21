# Login and password recovery setup

## Database
Run Database/Authentication_PasswordRecovery.sql in SSMS against the database
named by ConnectionStrings:conn. The provided rebuild script uses GRP-04-06;
older project scripts use GRP_04_06_db. Select the correct database.
This migration adds SecurityStamp and PasswordResetToken and creates/updates the
authentication procedures. It does not delete users or rebuild the database.
Apply it before starting the updated app. Existing cookies will be rejected once.

## Email
Set these configuration keys through local user secrets or deployment environment
variables. Do not commit SMTP passwords. Environment variable equivalents:
- Email__Host: your SMTP host
- Email__Port: 587 (STARTTLS)
- Email__From: a permitted sender email address
- Email__Username: SMTP username
- Email__Password: SMTP/app password
- AccountSecurity__PublicBaseUrl: trusted HTTPS app URL, including the IIS application path

Examples of base URLs:
- Local HTTPS profile: https://localhost:7114
- IIS Express: https://localhost:44397
- University hosting: https://soit-iis.mandela.ac.za/GRP-04-06

Use the HTTPS launch profile. Cookies require HTTPS. SMTP uses STARTTLS and
username/password authentication; an OAuth-only provider requires another
IAccountEmailSender implementation. The recipient must be a real mailbox recorded
in dbo.User.Email. Setting an SMTP sender does not change the user's account email.

A bounded in-memory queue processes reset emails in the background. Pending requests
can be lost on app restart; request another link if needed. Monitor logs for delivery
failures or queue saturation. Use a durable queue for multi-instance hosting.
The app never displays or logs reset tokens. Configure IIS/proxy request logging
to omit reset query strings.

## Behavior
- Account forms use controller parameters and server validation, without view models.
- Existing salted SHA-256 hashes remain valid. New passwords use PBKDF2-SHA256
  with 600,000 iterations.
- Passwords require 8–128 characters, uppercase, number and a non-whitespace special character.
- Reset links expire in 30 minutes; only the newest link is valid and it works once.
- Reset/password change invalidates previous cookies and reset links.
- First-login changes are enforced before other application actions.
- Login/reset/change POSTs require anti-forgery tokens and share an IP rate limit
  of 10 requests per minute. Reset emails have a one-minute per-account cooldown.
- LabManagerController role checks remain in force. Other roles land on Home.
- Change password is at /Account/ChangePassword.
- Email/username changes and emailing generated staff passwords are outside this change.

## Verification
Build, apply the migration in a test database, and configure email.
Check valid, expired and reused reset links, inactive/unknown email, password rules,
confirmation mismatch, and invalidation of a previously signed-in browser.
Check direct navigation with MustChangePassword set. Verify keyboard password
toggles and mobile layout.

Run the executable regression checks with:
dotnet run --project Tests/AuthenticationChecks/AuthenticationChecks.csproj

The checks cover PBKDF2, legacy SHA-256 compatibility, malformed hashes, password
policy, generated password uniqueness, input validation, generic reset responses,
and rejection of invalid, mismatched and expired-token reset submissions.
They use a fake repository; live SQL and SMTP require the setup above.
