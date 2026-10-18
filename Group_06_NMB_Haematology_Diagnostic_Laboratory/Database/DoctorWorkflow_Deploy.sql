/*
  ONE-FILE DOCTOR WORKFLOW DEPLOYMENT (SQL Server 2017 or later)

  1. In SSMS select the application database used by ConnectionStrings:conn.
  2. Set @ExpectedDatabase below to that database's EXACT name.
  3. Execute this ENTIRE file once. It is also safe to rerun on a compatible schema.

  Optional: set @ValidateOnly = 1 for prerequisite/schema checks without deployment.
  No USE statement, GO separators, SQLCMD mode, external files or credentials required.
  Run in a fresh query window with no existing transaction, using a deployment account.

  Combines DoctorWorkflow_EmailQueue.sql, spTestRequest_CancelByDoctor.sql and preflight.
  Changes only the email queue/indexes/FK and the two named Doctor modules below.
  Does not alter patient records, passwords, results, existing authentication modules,
  database permissions, or send emails. Existing queue rows are retained.
  Existing incompatible objects cause an error; they are never dropped/recreated.
  All deployment changes commit together or roll back on error.

  The cancellation trigger queues FUTURE cancellations (including technician actions).
  Once the application worker is running, it can deliver those queued notifications.
*/
SET NOCOUNT ON;

DECLARE @ExpectedDatabase SYSNAME = N'REPLACE_WITH_YOUR_DATABASE_NAME'; -- EDIT THIS LINE
DECLARE @ValidateOnly BIT = 0; -- 0 = deploy, 1 = check only

-- These checks are outside TRY/CATCH so another session transaction is never rolled back.
IF @@TRANCOUNT <> 0 OR (@@OPTIONS & 2) = 2
    THROW 51000, 'Use a fresh query window with no transaction and IMPLICIT_TRANSACTIONS OFF.', 1;
IF @ExpectedDatabase IS NULL OR @ExpectedDatabase = N'REPLACE_WITH_YOUR_DATABASE_NAME'
    OR LTRIM(RTRIM(@ExpectedDatabase)) = N''
    THROW 51001, 'Set @ExpectedDatabase at the top to your application database name, then execute the entire file.', 1;
IF DB_ID() <= 4 OR DB_NAME() <> @ExpectedDatabase
    THROW 51002, 'Wrong database selected. Select the application database in SSMS and match @ExpectedDatabase.', 1;
IF CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) < 14
    THROW 51003, 'These Doctor workflows require SQL Server 2017 or later.', 1;
IF DATABASEPROPERTYEX(DB_NAME(), 'Updateability') <> N'READ_WRITE'
    THROW 51004, 'The selected database is not writable.', 1;
IF ISNULL(HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION'), 0) <> 1
    THROW 51005, 'Use a deployment account with VIEW DEFINITION so prerequisite checks are reliable.', 1;

-- Required settings for filtered indexes and persisted module definitions.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult INT;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = N'NMB.DoctorWorkflow.Deployment', @LockMode = N'Exclusive',
        @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @LockResult < 0
        THROW 51006, 'Another Doctor deployment is running. Retry after it finishes.', 1;

    DECLARE @RequiredColumns TABLE (TableName SYSNAME, ColumnName SYSNAME);
    INSERT @RequiredColumns VALUES
        (N'User',N'UserID'), (N'User',N'SecurityStamp'), (N'User',N'MustChangePassword'),
        (N'Patient',N'PatientID'), (N'Patient',N'UserID'),
        (N'Doctor',N'DoctorID'), (N'Doctor',N'UserID'),
        (N'PatientConsent',N'PatientConsentID'), (N'PatientConsent',N'PatientID'),
        (N'PatientConsent',N'DoctorID'), (N'PatientConsent',N'IsActive'),
        (N'PatientConsent',N'TestRequestTestTypeID'),
        (N'PatientConsentRequest',N'PatientConsentID'), (N'PatientConsentRequest',N'TestRequestID'),
        (N'TestType',N'IsActive'), (N'TestType',N'SampleTypeID'),
        (N'TestType',N'NormalRangeMin'), (N'TestType',N'NormalRangeMax'),
        (N'TestRequest',N'TestRequestID'), (N'TestRequest',N'DoctorID'),
        (N'TestRequest',N'Status'), (N'TestRequest',N'ReleasedByDoctorID'),
        (N'TestRequest',N'DoctorReleaseNote'), (N'TestRequest',N'ReleaseDate'),
        (N'TestRequest',N'CancelledBy'), (N'TestRequest',N'CancelledDate'), (N'TestRequest',N'CancelReason'),
        (N'TestRequestTestType',N'TestRequestTestTypeID'), (N'TestRequestTestType',N'TestRequestID'),
        (N'TestRequestTestType',N'Status'), (N'Sample',N'BarcodeNumber'),
        (N'LabTest',N'LabTestID'), (N'LabTest',N'TestRequestTestTypeID'),
        (N'TestResult',N'TestResultID'), (N'TestResult',N'CapturedByLabTechnicianID'),
        (N'TestResult',N'CaptureDateTime'), (N'TestResultVerification',N'TestResultID'),
        (N'TestResultVerification',N'VerifiedByLabTechnicianID'),
        (N'TestResultVerification',N'VerificationDateTime'), (N'TestResultVerification',N'Status');
    IF EXISTS (SELECT 1 FROM @RequiredColumns
        WHERE COL_LENGTH(N'dbo.' + QUOTENAME(TableName), ColumnName) IS NULL)
    BEGIN
        SELECT TableName, ColumnName AS MissingColumn FROM @RequiredColumns
        WHERE COL_LENGTH(N'dbo.' + QUOTENAME(TableName), ColumnName) IS NULL;
        THROW 51007, 'Required application columns are missing. No changes were applied; review the listed columns.', 1;
    END;

    DECLARE @RequiredProcedures TABLE (ProcedureName SYSNAME);
    INSERT @RequiredProcedures VALUES
        (N'sp_Auth_GetActiveUserByEmail'), (N'sp_Auth_GetUserById'), (N'sp_Auth_UpdateProfile'),
        (N'sp_Auth_GetState'), (N'sp_Auth_RecordLogin'), (N'sp_Auth_ChangePassword'),
        (N'sp_Auth_CreateReset'), (N'sp_Auth_ResetPassword'), (N'spDoctor_GetByUserId');
    IF EXISTS (SELECT 1 FROM @RequiredProcedures
        WHERE OBJECT_ID(N'dbo.' + QUOTENAME(ProcedureName), N'P') IS NULL)
    BEGIN
        SELECT ProcedureName AS MissingProcedure FROM @RequiredProcedures
        WHERE OBJECT_ID(N'dbo.' + QUOTENAME(ProcedureName), N'P') IS NULL;
        THROW 51008, 'Required shared procedures are missing. This script will not overwrite shared authentication to repair them.', 1;
    END;

    IF OBJECT_ID(N'dbo.DoctorEmailQueue') IS NOT NULL AND OBJECT_ID(N'dbo.DoctorEmailQueue', N'U') IS NULL
        THROW 51009, 'dbo.DoctorEmailQueue exists but is not a user table. No changes were applied.', 1;
    IF OBJECT_ID(N'dbo.spTestRequest_CancelByDoctor') IS NOT NULL
        AND OBJECT_ID(N'dbo.spTestRequest_CancelByDoctor', N'P') IS NULL
        THROW 51010, 'The cancellation procedure name is used by a different object type.', 1;
    IF OBJECT_ID(N'dbo.tr_DoctorNotifyCancellation') IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM sys.triggers WHERE object_id = OBJECT_ID(N'dbo.tr_DoctorNotifyCancellation')
          AND parent_id = OBJECT_ID(N'dbo.TestRequest') AND is_instead_of_trigger = 0)
        THROW 51011, 'The notification trigger name is already used for a different purpose.', 1;

    -- Validate an existing queue instead of altering its columns or discarding pending mail.
    DECLARE @QueueId INT = OBJECT_ID(N'dbo.DoctorEmailQueue', N'U');
    IF @QueueId IS NOT NULL
    BEGIN
        DECLARE @QueueColumns TABLE
            (ColumnName SYSNAME, TypeName SYSNAME, MaxLength SMALLINT, Nullable BIT);
        INSERT @QueueColumns VALUES
            (N'Id',N'bigint',8,0), (N'TestRequestID',N'int',4,1), (N'Kind',N'nvarchar',60,0),
            (N'Payload',N'nvarchar',-1,1), (N'CreatedAt',N'datetime2',8,0),
            (N'SentAt',N'datetime2',8,1), (N'NextAttemptAt',N'datetime2',8,0),
            (N'Attempts',N'int',4,0), (N'LeaseId',N'uniqueidentifier',16,1), (N'LockedUntil',N'datetime2',8,1);
        IF EXISTS (SELECT 1 FROM @QueueColumns expected LEFT JOIN sys.columns c
            ON c.object_id = @QueueId AND c.name = expected.ColumnName
            WHERE c.column_id IS NULL OR TYPE_NAME(c.user_type_id) <> expected.TypeName
              OR c.max_length <> expected.MaxLength OR c.is_nullable <> expected.Nullable OR c.is_computed = 1)
            THROW 51012, 'Existing DoctorEmailQueue columns are incompatible. Preserve its data and review the schema manually.', 1;
        IF COLUMNPROPERTY(@QueueId, N'Id', 'IsIdentity') <> 1
            THROW 51013, 'Existing DoctorEmailQueue.Id must be an identity column.', 1;
        IF EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = @QueueId
            AND c.is_nullable = 0 AND c.is_identity = 0 AND c.is_computed = 0 AND c.default_object_id = 0
            AND c.name NOT IN (N'TestRequestID',N'Kind',N'Payload',N'SentAt',N'LeaseId',N'LockedUntil'))
            THROW 51014, 'Existing queue has missing defaults or additional required columns. No data was changed.', 1;
        IF EXISTS (SELECT 1 FROM (VALUES (N'CreatedAt',N'sysutcdatetime'),
                (N'NextAttemptAt',N'sysutcdatetime'), (N'Attempts',N'0')) expected(ColumnName, Definition)
            LEFT JOIN sys.columns c ON c.object_id = @QueueId AND c.name = expected.ColumnName
            LEFT JOIN sys.default_constraints d ON d.object_id = c.default_object_id
            WHERE ISNULL(LOWER(REPLACE(REPLACE(REPLACE(d.definition,N'(',N''),N')',N''),N' ',N'')),N'') <> expected.Definition)
            THROW 51015, 'Existing queue defaults differ from the required UTC/retry defaults. Review manually.', 1;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = @QueueId AND i.is_primary_key = 1 AND i.is_disabled = 0
            AND (SELECT COUNT(*) FROM sys.index_columns ic WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id AND ic.key_ordinal > 0) = 1
            AND EXISTS (SELECT 1 FROM sys.index_columns ic INNER JOIN sys.columns c
                ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 1 AND c.name = N'Id'))
            THROW 51016, 'Existing queue must have its primary key on Id alone.', 1;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            WHERE fk.parent_object_id = @QueueId AND COL_NAME(@QueueId,fkc.parent_column_id) = N'TestRequestID'
              AND (fk.referenced_object_id <> OBJECT_ID(N'dbo.TestRequest')
                OR COL_NAME(fk.referenced_object_id,fkc.referenced_column_id) <> N'TestRequestID'
                OR fk.is_disabled = 1 OR fk.is_not_trusted = 1
                OR fk.delete_referential_action <> 0 OR fk.update_referential_action <> 0))
            THROW 51020, 'Existing queue has an incompatible or disabled request foreign key. Review it manually.', 1;
    END;

    -- Guard named index collisions. Missing indexes are created only during deployment.
    IF EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = @QueueId AND i.name = N'IX_DoctorEmailQueue_Pending'
        AND (i.is_disabled = 1 OR i.is_unique = 1 OR i.has_filter = 1 OR i.type <> 2
            OR (SELECT COUNT(*) FROM sys.index_columns ic WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id AND ic.key_ordinal > 0) <> 3
            OR NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 1 AND c.name = N'SentAt')
            OR NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 2 AND c.name = N'NextAttemptAt')
            OR NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 3 AND c.name = N'LockedUntil')))
        THROW 51017, 'The existing pending index has a different definition. No indexes were dropped.', 1;
    IF EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = @QueueId AND i.name = N'UX_DoctorEmailQueue_Cancellation'
        AND (i.is_disabled = 1 OR i.is_unique = 0 OR i.has_filter = 0 OR i.type <> 2 OR i.ignore_dup_key = 1
            OR REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(i.filter_definition,N'(',N''),N')',N''),N'[',N''),N']',N''),N' ',N'') <> N'Kind=N''Cancellation'''
            OR (SELECT COUNT(*) FROM sys.index_columns ic WHERE ic.object_id = i.object_id
                AND ic.index_id = i.index_id AND ic.key_ordinal > 0) <> 1
            OR NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal = 1 AND c.name = N'TestRequestID')))
        THROW 51018, 'The existing cancellation index has a different definition. No indexes were dropped.', 1;

    IF @ValidateOnly = 1
    BEGIN
        COMMIT TRANSACTION;
        SELECT DB_NAME() AS CheckedDatabase, N'Prerequisite checks passed; no deployment changes applied.' AS Result,
            CASE WHEN @QueueId IS NULL THEN N'Queue will be created.' ELSE N'Existing queue will be retained.' END AS QueuePlan;
        RETURN;
    END;

    -- Dynamic DDL is compiled inside TRY so errors can roll back the complete deployment.
    IF @QueueId IS NULL
        EXEC sys.sp_executesql N'
            CREATE TABLE dbo.DoctorEmailQueue
            (
                Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorEmailQueue PRIMARY KEY,
                TestRequestID INT NULL,
                Kind NVARCHAR(30) NOT NULL,
                Payload NVARCHAR(MAX) NULL,
                CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_DoctorEmailQueue_CreatedAt DEFAULT SYSUTCDATETIME(),
                SentAt DATETIME2 NULL,
                NextAttemptAt DATETIME2 NOT NULL CONSTRAINT DF_DoctorEmailQueue_NextAttemptAt DEFAULT SYSUTCDATETIME(),
                Attempts INT NOT NULL CONSTRAINT DF_DoctorEmailQueue_Attempts DEFAULT 0,
                LeaseId UNIQUEIDENTIFIER NULL,
                LockedUntil DATETIME2 NULL
            );';
    SET @QueueId = OBJECT_ID(N'dbo.DoctorEmailQueue', N'U');

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        WHERE fk.parent_object_id = @QueueId AND fk.referenced_object_id = OBJECT_ID(N'dbo.TestRequest')
          AND COL_NAME(@QueueId,fkc.parent_column_id) = N'TestRequestID'
          AND COL_NAME(fk.referenced_object_id,fkc.referenced_column_id) = N'TestRequestID'
          AND fk.is_disabled = 0 AND fk.is_not_trusted = 0
          AND fk.delete_referential_action = 0 AND fk.update_referential_action = 0)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.DoctorEmailQueue WITH CHECK
            ADD CONSTRAINT FK_DoctorEmailQueue_TestRequest FOREIGN KEY(TestRequestID) REFERENCES dbo.TestRequest(TestRequestID);';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @QueueId AND name = N'IX_DoctorEmailQueue_Pending')
        EXEC sys.sp_executesql N'CREATE INDEX IX_DoctorEmailQueue_Pending ON dbo.DoctorEmailQueue(SentAt, NextAttemptAt, LockedUntil);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @QueueId AND name = N'UX_DoctorEmailQueue_Cancellation')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_DoctorEmailQueue_Cancellation
            ON dbo.DoctorEmailQueue(TestRequestID) WHERE Kind = N''Cancellation'';';

    EXEC sys.sp_executesql N'
CREATE OR ALTER TRIGGER dbo.tr_DoctorNotifyCancellation ON dbo.TestRequest
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.DoctorEmailQueue(TestRequestID, Kind)
    SELECT i.TestRequestID, N''Cancellation''
    FROM inserted i INNER JOIN deleted d ON d.TestRequestID = i.TestRequestID
    WHERE i.Status = N''Cancelled'' AND (d.Status <> N''Cancelled'' OR d.Status IS NULL)
      AND NOT EXISTS (SELECT 1 FROM dbo.DoctorEmailQueue q
                      WHERE q.TestRequestID = i.TestRequestID AND q.Kind = N''Cancellation'');
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.spTestRequest_CancelByDoctor
    @TestRequestID INT,
    @DoctorID INT,
    @CancelledBy NVARCHAR(200),
    @CancelReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    IF @TestRequestID IS NULL OR @TestRequestID <= 0 OR @DoctorID IS NULL OR @DoctorID <= 0
        THROW 51100, ''A valid request and doctor are required.'', 1;
    IF NULLIF(LTRIM(RTRIM(@CancelReason)), N'''') IS NULL
        THROW 51101, ''A cancellation reason is required.'', 1;
    IF NULLIF(LTRIM(RTRIM(@CancelledBy)), N'''') IS NULL
        OR DATALENGTH(@CancelledBy) > COL_LENGTH(N''dbo.TestRequest'', N''CancelledBy'')
        THROW 51102, ''The cancellation actor is missing or exceeds the database column limit.'', 1;

    UPDATE dbo.TestRequest
    SET Status = N''Cancelled'', CancelledBy = @CancelledBy,
        CancelledDate = SYSDATETIME(), CancelReason = LTRIM(RTRIM(@CancelReason))
    WHERE TestRequestID = @TestRequestID AND DoctorID = @DoctorID
      AND Status IN (N''Submitted'', N''Samples Received'', N''Sample(s) received'');
    IF @@ROWCOUNT = 1
    BEGIN
        SELECT 1;
        RETURN;
    END;
    IF EXISTS (SELECT 1 FROM dbo.TestRequest WHERE TestRequestID = @TestRequestID AND DoctorID = @DoctorID)
        SELECT 2;
    ELSE
        SELECT 0;
END;';

    IF OBJECT_ID(N'dbo.spTestRequest_CancelByDoctor', N'P') IS NULL
        OR NOT EXISTS (SELECT 1 FROM sys.triggers WHERE object_id = OBJECT_ID(N'dbo.tr_DoctorNotifyCancellation')
            AND parent_id = OBJECT_ID(N'dbo.TestRequest') AND is_disabled = 0)
        THROW 51019, 'Final procedure/trigger verification failed. Deployment will be rolled back.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT DB_NAME() AS DeployedDatabase, N'Doctor workflow deployment completed successfully.' AS Result;
PRINT N'Existing patient records and queued emails were retained. SMTP and application configuration are separate.';
