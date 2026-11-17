-- Test catalogue requirements do not reserve, consume or return physical stock.
-- Run the WHOLE script in SSMS. Set the exact database below.
-- Preview first (@Apply = 0), then use @Apply = 1 to save.
-- Preserves current stock and past audit entries. Does not repair historical deductions.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
DECLARE @ExpectedDatabase sysname = N'GRP-04-06';
DECLARE @Apply bit = 0;
IF DB_NAME() <> @ExpectedDatabase OR DB_ID() <= 4
    THROW 51000, 'Select the application database and set @ExpectedDatabase correctly.', 1;
IF @@TRANCOUNT <> 0 THROW 51000, 'Run without an existing transaction.', 1;
IF OBJECT_ID('dbo.TestType','U') IS NULL OR OBJECT_ID('dbo.TestTypeConsumable','U') IS NULL OR OBJECT_ID('dbo.Consumable','U') IS NULL
    THROW 51000, 'The existing catalogue tables are required.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
-- sp_TestType_Create
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestType_Create
    @TestCategoryID INT,
    @TestName NVARCHAR(150),
    @SampleTypeID INT,
    @UnitOfMeasurement NVARCHAR(50) = NULL,
    @NormalRangeMin DECIMAL(12,4) = NULL,
    @NormalRangeMax DECIMAL(12,4) = NULL,
    @TurnaroundTimeMinutes INT,
    @ConsumableID INT,
    @QuantityRequired INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF @QuantityRequired IS NULL OR @QuantityRequired <= 0 THROW 50001, ''Quantity required must be greater than zero.'', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Consumable WHERE ConsumableID = @ConsumableID AND IsActive = 1)
            THROW 50002, ''The selected consumable is not active.'', 1;

        INSERT dbo.TestType (TestCategoryID, TestName, SampleTypeID, UnitOfMeasurement, NormalRangeMin, NormalRangeMax, TurnaroundTimeMinutes, CreatedAt, IsActive)
        VALUES (@TestCategoryID, @TestName, @SampleTypeID, @UnitOfMeasurement, @NormalRangeMin, @NormalRangeMax, @TurnaroundTimeMinutes, SYSUTCDATETIME(), 1);

        DECLARE @TestTypeID INT = CONVERT(INT, SCOPE_IDENTITY());
        INSERT dbo.TestTypeConsumable (TestTypeID, ConsumableID, QuantityRequired)
        VALUES (@TestTypeID, @ConsumableID, @QuantityRequired);
        COMMIT TRANSACTION;
        SELECT @TestTypeID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
-- sp_TestType_Delete
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestType_Delete @TestTypeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.TestType WHERE TestTypeID = @TestTypeID AND IsActive = 1)
            THROW 50027, ''Active test type was not found.'', 1;

        UPDATE dbo.TestType SET IsActive = 0 WHERE TestTypeID = @TestTypeID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
-- sp_TestType_Reactivate
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestType_Reactivate @TestTypeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.TestType WHERE TestTypeID = @TestTypeID AND IsActive = 0)
            THROW 50028, ''Inactive test type was not found.'', 1;

        IF EXISTS (SELECT 1 FROM dbo.TestTypeConsumable r
            JOIN dbo.Consumable c ON c.ConsumableID = r.ConsumableID
            WHERE r.TestTypeID = @TestTypeID AND c.IsActive = 0)
            THROW 50029, ''All consumables must be active before reactivation.'', 1;
        UPDATE dbo.TestType SET IsActive = 1 WHERE TestTypeID = @TestTypeID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
-- sp_TestType_Update
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestType_Update
    @TestTypeID INT,
    @TestCategoryID INT,
    @TestName NVARCHAR(150),
    @SampleTypeID INT,
    @UnitOfMeasurement NVARCHAR(50) = NULL,
    @NormalRangeMin DECIMAL(12,4) = NULL,
    @NormalRangeMax DECIMAL(12,4) = NULL,
    @TurnaroundTimeMinutes INT,
    @TestTypeConsumableID INT = NULL,
    @ConsumableID INT = NULL,
    @QuantityRequired INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- The MVC page updates multiple requirements through the dedicated
        -- TestTypeConsumable procedures, so the test details can be saved alone.
        IF @TestTypeConsumableID IS NULL
        BEGIN
            UPDATE dbo.TestType SET TestCategoryID = @TestCategoryID, TestName = @TestName, SampleTypeID = @SampleTypeID,
                UnitOfMeasurement = @UnitOfMeasurement, NormalRangeMin = @NormalRangeMin, NormalRangeMax = @NormalRangeMax,
                TurnaroundTimeMinutes = @TurnaroundTimeMinutes
            WHERE TestTypeID = @TestTypeID;
            COMMIT TRANSACTION;
            RETURN;
        END

        IF @QuantityRequired IS NULL OR @QuantityRequired <= 0 THROW 50022, ''Quantity required must be greater than zero.'', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.TestType WHERE TestTypeID = @TestTypeID AND IsActive = 1)
            THROW 50023, ''Active test type was not found.'', 1;

        DECLARE @OldConsumableID INT, @OldQuantityRequired INT;
        SELECT @OldConsumableID = ConsumableID, @OldQuantityRequired = QuantityRequired
        FROM dbo.TestTypeConsumable WITH (UPDLOCK, HOLDLOCK)
        WHERE TestTypeConsumableID = @TestTypeConsumableID AND TestTypeID = @TestTypeID;
        IF @OldConsumableID IS NULL THROW 50024, ''Test type consumable requirement was not found.'', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Consumable WHERE ConsumableID = @ConsumableID AND IsActive = 1)
            THROW 50025, ''The selected consumable is not active.'', 1;
        IF @OldConsumableID <> @ConsumableID AND EXISTS (
            SELECT 1 FROM dbo.TestTypeConsumable WHERE TestTypeID = @TestTypeID AND ConsumableID = @ConsumableID)
            THROW 50026, ''This consumable is already assigned to the test type.'', 1;

        UPDATE dbo.TestType SET TestCategoryID = @TestCategoryID, TestName = @TestName, SampleTypeID = @SampleTypeID,
            UnitOfMeasurement = @UnitOfMeasurement, NormalRangeMin = @NormalRangeMin, NormalRangeMax = @NormalRangeMax,
            TurnaroundTimeMinutes = @TurnaroundTimeMinutes
        WHERE TestTypeID = @TestTypeID;

        UPDATE dbo.TestTypeConsumable SET ConsumableID = @ConsumableID, QuantityRequired = @QuantityRequired
        WHERE TestTypeConsumableID = @TestTypeConsumableID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
-- sp_TestTypeConsumable_Create
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestTypeConsumable_Create
    @TestTypeID INT,
    @ConsumableID INT,
    @QuantityRequired INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF @QuantityRequired IS NULL OR @QuantityRequired <= 0 THROW 50030, ''Quantity required must be greater than zero.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.TestTypeConsumable WHERE TestTypeID = @TestTypeID AND ConsumableID = @ConsumableID)
            THROW 50031, ''This consumable is already assigned to the test type.'', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Consumable WHERE ConsumableID = @ConsumableID AND IsActive = 1)
            THROW 50032, ''The selected consumable is not active.'', 1;

        INSERT dbo.TestTypeConsumable (TestTypeID, ConsumableID, QuantityRequired)
        VALUES (@TestTypeID, @ConsumableID, @QuantityRequired);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
-- sp_TestTypeConsumable_Delete
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestTypeConsumable_Delete
    @TestTypeConsumableID INT,
    @TestTypeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @ConsumableID INT, @QuantityRequired INT;
        SELECT @ConsumableID = ConsumableID, @QuantityRequired = QuantityRequired
        FROM dbo.TestTypeConsumable WITH (UPDLOCK, HOLDLOCK)
        WHERE TestTypeConsumableID = @TestTypeConsumableID AND TestTypeID = @TestTypeID;
        IF @ConsumableID IS NULL THROW 50037, ''Consumable requirement was not found.'', 1;
        DELETE dbo.TestTypeConsumable WHERE TestTypeConsumableID = @TestTypeConsumableID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
-- sp_TestTypeConsumable_Update
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_TestTypeConsumable_Update
    @TestTypeConsumableID INT,
    @TestTypeID INT,
    @ConsumableID INT,
    @QuantityRequired INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF @QuantityRequired IS NULL OR @QuantityRequired <= 0 THROW 50033, ''Quantity required must be greater than zero.'', 1;
        DECLARE @OldConsumableID INT, @OldQuantityRequired INT;
        SELECT @OldConsumableID = ConsumableID, @OldQuantityRequired = QuantityRequired
        FROM dbo.TestTypeConsumable WITH (UPDLOCK, HOLDLOCK)
        WHERE TestTypeConsumableID = @TestTypeConsumableID AND TestTypeID = @TestTypeID;
        IF @OldConsumableID IS NULL THROW 50034, ''Consumable requirement was not found.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.TestTypeConsumable WHERE TestTypeID = @TestTypeID AND ConsumableID = @ConsumableID AND TestTypeConsumableID <> @TestTypeConsumableID)
            THROW 50035, ''This consumable is already assigned to the test type.'', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Consumable WHERE ConsumableID = @ConsumableID AND IsActive = 1)
            THROW 50036, ''The selected consumable is not active.'', 1;

        UPDATE dbo.TestTypeConsumable SET ConsumableID = @ConsumableID, QuantityRequired = @QuantityRequired
        WHERE TestTypeConsumableID = @TestTypeConsumableID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;';
    IF @Apply = 1
    BEGIN
        COMMIT;
        PRINT 'Catalogue procedures updated. Catalogue changes no longer adjust stock.';
    END
    ELSE
    BEGIN
        ROLLBACK;
        PRINT 'Preview passed. All changes rolled back. Set @Apply = 1 to install.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;
