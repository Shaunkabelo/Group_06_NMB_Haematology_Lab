# Deploy the database changes

Open Server_Deployment.sql in SSMS against the existing application database.

1. Back up the server database.
2. Select the correct database and set @ExpectedDatabase to its exact name.
3. Leave @Apply = 0 and execute the whole file. Confirm the preview succeeded and rolled back.
4. Set @Apply = 1 and run the whole file again. Confirm deployment committed successfully.

No SQLCMD mode is required. Requires SQL Server 2016 SP1 or later and compatibility level 130 or later.

## Existing procedures

Default @UpdateExisting = 1 creates missing procedures and updates existing definitions.
This is needed for changed assignment parameters and other fixes.

Set @UpdateExisting = 0 for create-missing-only mode. Existing procedures are skipped;
their old behaviour remains and may not support the updated application.

The script is rerunnable. Users, passwords, stock, orders and assignments are preserved.
SecurityStamp, PasswordResetToken and its index are added only when missing.
Procedure definitions are installed, not executed. Deployment sends no emails and makes no stock adjustments.

The script checks the database name and required base tables, then uses one transaction.
Caught errors roll back deployment. Preview mode also takes schema locks temporarily;
run during a quiet period.

## Included changes: 23 procedures

- Authentication: login lookup/recording, account state, password recovery and changes.
- Shared profile: user lookup and profile updates.
- Staff: doctor/manager creation and technician creation/update.
- Consumables: stock transaction, stock/reorder adjustment and metadata updates.
- Test types: create/update and consumable requirement create/update/delete.
- Technician assignments: lookup and replacement from either page.
- Management report: completed tests grouped by category.

Sources: seven checked-in SQL migrations plus earlier stock/test-type and doctor/manager
creation definitions read from the local database on 2026-09-23.
The older sp_Auth.sql is superseded by the password-recovery definitions and excluded.

This is an upgrade, not a complete initial schema. Unchanged original procedures for
orders, suppliers and other pages must already exist on the server.
Orders layout/filtering and validation changes require publishing the updated app.

## Verification

The update-existing rollback preview passed against the local development database.
Server execution has not been performed; its schema and permissions may differ.

After deployment, publish the app and configure its connection string, SMTP settings
and public HTTPS base URL. No credentials or SMTP settings are included in this SQL file.