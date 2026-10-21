/*
    Hosted database data reset for judging.

    What it does:
      - Deletes application data from all non-system user tables.
      - Preserves tables, columns, procedures, views, constraints and identity counters.
      - Preserves dbo.__EFMigrationsHistory if it exists, because it is schema metadata.

    Safe usage:
      1. Select the hosted application database.
      2. Run with @Apply = 0. This only shows the tables and row counts.
      3. Confirm the list is correct.
      4. Change only @Apply to 1 and run the entire script.

    This cannot restore deleted data. Take an SSMS backup or export before step 4.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
SET DEADLOCK_PRIORITY LOW;

DECLARE @ExpectedDatabase sysname = N'GRP-04-06';
DECLARE @Apply bit = 0; -- Keep 0 for preview. Set to 1 only after reviewing the preview.

IF DB_NAME() <> @ExpectedDatabase OR DB_ID() <= 4
    THROW 51000, 'Select GRP-04-06 before running this reset script.', 1;

IF @@TRANCOUNT <> 0
    THROW 51001, 'Run this script without an existing transaction.', 1;

IF OBJECT_ID(N'dbo.[User]', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Patient', N'U') IS NULL
   OR OBJECT_ID(N'dbo.LabManager', N'U') IS NULL
    THROW 51002, 'This does not appear to be the application database. No changes were made.', 1;

-- A table variable avoids any collision with a temporary table from another run.
DECLARE @TargetTables TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    RowsBefore bigint NULL,
    RowsAfter bigint NULL,
    PRIMARY KEY (SchemaName, TableName)
);

INSERT @TargetTables (SchemaName, TableName)
SELECT s.name, t.name
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND NOT (s.name = N'dbo' AND t.name = N'__EFMigrationsHistory');

IF NOT EXISTS (SELECT 1 FROM @TargetTables)
    THROW 51003, 'No application tables were found. No changes were made.', 1;

DECLARE @SchemaName sysname;
DECLARE @TableName sysname;
DECLARE @Sql nvarchar(max);
DECLARE @RowCount bigint;

DECLARE TableCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT SchemaName, TableName
    FROM @TargetTables
    ORDER BY SchemaName, TableName;

OPEN TableCursor;
FETCH NEXT FROM TableCursor INTO @SchemaName, @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'SELECT @RowCount = COUNT_BIG(*) FROM '
        + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName) + N';';

    EXEC sys.sp_executesql
        @Sql,
        N'@RowCount bigint OUTPUT',
        @RowCount = @RowCount OUTPUT;

    UPDATE @TargetTables
    SET RowsBefore = @RowCount
    WHERE SchemaName = @SchemaName AND TableName = @TableName;

    FETCH NEXT FROM TableCursor INTO @SchemaName, @TableName;
END;

CLOSE TableCursor;
DEALLOCATE TableCursor;

SELECT SchemaName, TableName, RowsBefore
FROM @TargetTables
ORDER BY SchemaName, TableName;

SELECT COUNT(*) AS TablesToClear, COALESCE(SUM(RowsBefore), 0) AS TotalRowsToDelete
FROM @TargetTables;

IF @Apply = 0
BEGIN
    PRINT 'Preview only. No data was changed. Set @Apply = 1 and rerun the whole script to delete the listed rows.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    /*
       Foreign keys prevent arbitrary delete order. Disable constraints inside
       this transaction, clear the rows, then validate and trust every
       constraint again before committing.
    */
    DECLARE ConstraintCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT SchemaName, TableName
        FROM @TargetTables
        ORDER BY SchemaName, TableName;

    OPEN ConstraintCursor;
    FETCH NEXT FROM ConstraintCursor INTO @SchemaName, @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sql = N'ALTER TABLE ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName)
            + N' NOCHECK CONSTRAINT ALL;';
        EXEC sys.sp_executesql @Sql;

        FETCH NEXT FROM ConstraintCursor INTO @SchemaName, @TableName;
    END;

    CLOSE ConstraintCursor;
    DEALLOCATE ConstraintCursor;

    DECLARE DeleteCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT SchemaName, TableName
        FROM @TargetTables
        ORDER BY SchemaName, TableName;

    OPEN DeleteCursor;
    FETCH NEXT FROM DeleteCursor INTO @SchemaName, @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sql = N'DELETE FROM ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName) + N';';
        EXEC sys.sp_executesql @Sql;

        FETCH NEXT FROM DeleteCursor INTO @SchemaName, @TableName;
    END;

    CLOSE DeleteCursor;
    DEALLOCATE DeleteCursor;

    DECLARE RecheckCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT SchemaName, TableName
        FROM @TargetTables
        ORDER BY SchemaName, TableName;

    OPEN RecheckCursor;
    FETCH NEXT FROM RecheckCursor INTO @SchemaName, @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sql = N'ALTER TABLE ' + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName)
            + N' WITH CHECK CHECK CONSTRAINT ALL;';
        EXEC sys.sp_executesql @Sql;

        FETCH NEXT FROM RecheckCursor INTO @SchemaName, @TableName;
    END;

    CLOSE RecheckCursor;
    DEALLOCATE RecheckCursor;

    DECLARE VerifyCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT SchemaName, TableName
        FROM @TargetTables
        ORDER BY SchemaName, TableName;

    OPEN VerifyCursor;
    FETCH NEXT FROM VerifyCursor INTO @SchemaName, @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sql = N'SELECT @RowCount = COUNT_BIG(*) FROM '
            + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName) + N';';

        EXEC sys.sp_executesql
            @Sql,
            N'@RowCount bigint OUTPUT',
            @RowCount = @RowCount OUTPUT;

        UPDATE @TargetTables
        SET RowsAfter = @RowCount
        WHERE SchemaName = @SchemaName AND TableName = @TableName;

        FETCH NEXT FROM VerifyCursor INTO @SchemaName, @TableName;
    END;

    CLOSE VerifyCursor;
    DEALLOCATE VerifyCursor;

    IF EXISTS (SELECT 1 FROM @TargetTables WHERE RowsAfter <> 0)
        THROW 51004, 'One or more tables still contain rows. The transaction was rolled back.', 1;

    COMMIT TRANSACTION;

    SELECT SchemaName, TableName, RowsBefore, RowsAfter
    FROM @TargetTables
    ORDER BY SchemaName, TableName;

    PRINT 'Reset completed. All application data rows were deleted; schema, procedures, constraints and identity counters were preserved.';
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'ConstraintCursor') >= 0
        CLOSE ConstraintCursor;
    IF CURSOR_STATUS('local', 'ConstraintCursor') >= -1
        DEALLOCATE ConstraintCursor;
    IF CURSOR_STATUS('local', 'DeleteCursor') >= 0
        CLOSE DeleteCursor;
    IF CURSOR_STATUS('local', 'DeleteCursor') >= -1
        DEALLOCATE DeleteCursor;
    IF CURSOR_STATUS('local', 'RecheckCursor') >= 0
        CLOSE RecheckCursor;
    IF CURSOR_STATUS('local', 'RecheckCursor') >= -1
        DEALLOCATE RecheckCursor;
    IF CURSOR_STATUS('local', 'VerifyCursor') >= 0
        CLOSE VerifyCursor;
    IF CURSOR_STATUS('local', 'VerifyCursor') >= -1
        DEALLOCATE VerifyCursor;

    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
