# Consent notification email setup

After consent is saved, the application sends one plain-text email to the selected doctor's database email address. It lists the patient's full name, the selected test names grouped by request number, and each saved grant date. Both the confirmation dialog and re-enabling access use this notification flow.

Configure these keys through environment variables or .NET user secrets for the web project:

| Configuration key | Environment variable | Value |
| --- | --- | --- |
| Email:Smtp:Host | Email__Smtp__Host | smtp.gmail.com |
| Email:Smtp:Port | Email__Smtp__Port | 587 (STARTTLS) |
| Email:Smtp:EnableSsl | Email__Smtp__EnableSsl | true |
| Email:Smtp:FromAddress | Email__Smtp__FromAddress | nmblabsystem@gmail.com |
| Email:Smtp:FromName | Email__Smtp__FromName | NMB-HLabSys |
| Email:Smtp:Username | Email__Smtp__Username | nmblabsystem@gmail.com |
| Email:Smtp:Password | Email__Smtp__Password | Google app password (keep in secrets) |

Do not commit credentials to appsettings.json. The provider must support SMTP authentication or an approved relay; this transport uses STARTTLS rather than implicit TLS on port 465. Restart the application after configuring environment variables.

If SMTP is missing or rejects the message, consent remains saved and the page reports that the email could not be sent. No automatic retries are currently scheduled. A successful send means the SMTP server accepted the message; it does not guarantee inbox delivery.

## Configured Gmail sender

The application now defaults to nmblabsystem@gmail.com through smtp.gmail.com:587 with STARTTLS. The password is deliberately absent from appsettings.json.

1. Sign in to the sender's Google account and enable 2-Step Verification.
2. Create an app password: https://myaccount.google.com/apppasswords
3. In Visual Studio, right-click the web project and select **Manage User Secrets**. Add `"Email:Smtp:Password": "YOUR_GOOGLE_APP_PASSWORD"` inside the existing JSON object, preserving any other secrets. Replace the placeholder locally; do not paste it into chat.
4. Restart the application under the Development environment. For deployed environments, set `Email__Smtp__Password` in the host's secret/environment settings instead.
5. Confirm a patient consent grant and check the on-page email status and the doctor's inbox.

Google setup documentation: https://support.google.com/mail/answer/7104828 and https://support.google.com/accounts/answer/185833
Revoking selected tests or disabling a doctor's access also sends one email, only to that doctor. It includes only tests whose active consent was revoked, with the patient name, request numbers and revocation date in South African time. Repeating a revoke with no active access remaining sends no email. Revocation remains effective if SMTP fails, and the page shows the failure.

Successful patient registration also sends an account-created confirmation to the new patient's email, using the same Gmail SMTP settings. The message includes the patient's full name, username and creation date/time in SAST; it does not include passwords. Email failure does not undo registration, and the login page displays the delivery status.
