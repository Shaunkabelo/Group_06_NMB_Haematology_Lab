-- Run in the application database (GRP-04-06).
-- Preserves records, IDs, foreign keys, and active/revoked states.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.PatientConsent', N'U') IS NULL
       OR COL_LENGTH(N'dbo.PatientConsent', N'TestRequestTestTypeID') IS NULL
        THROW 50010, 'Required consent table or column is missing. Check the database.', 1;

    DECLARE @RowCount BIGINT;
    SELECT @RowCount = COUNT_BIG(*) FROM dbo.PatientConsent WITH (TABLOCKX, HOLDLOCK);

    IF EXISTS (SELECT 1 FROM dbo.PatientConsent
               GROUP BY PatientID, DoctorID, TestRequestTestTypeID HAVING COUNT_BIG(*) > 1)
        THROW 50011, 'Duplicate patient-doctor-test grants exist. No changes were saved.', 1;

    -- Detect equivalent keys by columns, including system-generated names.
    DECLARE @Keys TABLE (IndexName SYSNAME, IsConstraint BIT, KeyCount INT, PairCount INT, TestCount INT);
    INSERT INTO @Keys
    SELECT i.name, i.is_unique_constraint, COUNT(*),
           SUM(CASE WHEN c.name IN (N'PatientID', N'DoctorID') THEN 1 ELSE 0 END),
           SUM(CASE WHEN c.name = N'TestRequestTestTypeID' THEN 1 ELSE 0 END)
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.PatientConsent')
      AND i.is_unique = 1 AND i.is_primary_key = 0
      AND i.is_disabled = 0 AND i.has_filter = 0 AND ic.key_ordinal > 0
    GROUP BY i.name, i.is_unique_constraint;

    -- Also preserves at most one legacy NULL-test record for each patient/doctor.
    IF NOT EXISTS (SELECT 1 FROM @Keys WHERE KeyCount = 3 AND PairCount = 2 AND TestCount = 1)
        ALTER TABLE dbo.PatientConsent
            ADD CONSTRAINT UQ_PatientConsent_Patient_Doctor_Test
            UNIQUE (PatientID, DoctorID, TestRequestTestTypeID);

    DECLARE @DropSql NVARCHAR(MAX) = N'';
    SELECT @DropSql = @DropSql + CASE WHEN IsConstraint = 1
        THEN N'ALTER TABLE dbo.PatientConsent DROP CONSTRAINT ' + QUOTENAME(IndexName) + N';'
        ELSE N'DROP INDEX ' + QUOTENAME(IndexName) + N' ON dbo.PatientConsent;'
        END
    FROM @Keys WHERE KeyCount = 2 AND PairCount = 2;
    IF @DropSql <> N'' EXEC sys.sp_executesql @DropSql;

    COMMIT TRANSACTION;
    PRINT 'Consent uniqueness repaired. Retry granting doctor access in the application.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
