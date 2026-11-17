-- Additive procedures for inline stock and reorder-level changes.
-- Run in the application's database. Existing stock transaction procedure is reused.
CREATE OR ALTER PROCEDURE dbo.sp_Consumable_AdjustLevel
    @ConsumableID INT, @Field NVARCHAR(20), @Mode NVARCHAR(10),
    @Value INT, @ExpectedValue INT, @PerformedByUserID INT,
    @Reason NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Field NOT IN ('Quantity', 'Reorder') OR @Field IS NULL
        THROW 50100, 'Invalid adjustment field.', 1;
    IF @Mode NOT IN ('Set', 'Increase', 'Decrease') OR @Mode IS NULL
        THROW 50100, 'Invalid adjustment mode.', 1;
    IF @Value IS NULL OR @Value < 0 OR @ExpectedValue IS NULL OR @ExpectedValue < 0
        THROW 50100, 'Enter a non-negative whole number.', 1;
    IF @Field = 'Reorder' AND @Mode <> 'Set'
        THROW 50100, 'Reorder level requires a new value.', 1;
    IF @Field = 'Quantity' AND NULLIF(LTRIM(RTRIM(@Reason)), '') IS NULL
        THROW 50100, 'Provide a reason for the stock adjustment.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE UserID = @PerformedByUserID
        AND IsActive = 1 AND Role IN ('LabManager', 'Admin'))
        THROW 50100, 'An active laboratory manager is required.', 1;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @Old INT, @New BIGINT, @Delta INT;
        SELECT @Old = CASE WHEN @Field = 'Quantity' THEN QuantityOnHand ELSE ReorderLevel END
        FROM dbo.Consumable WITH (UPDLOCK, HOLDLOCK)
        WHERE ConsumableID = @ConsumableID AND IsActive = 1;
        IF @Old IS NULL THROW 50101, 'Active consumable not found.', 1;
        IF @Old <> @ExpectedValue
            THROW 50102, 'This value changed since the page was loaded. Review the refreshed value and try again.', 1;
        SET @New = CASE @Mode WHEN 'Increase' THEN CONVERT(BIGINT, @Old) + @Value
            WHEN 'Decrease' THEN CONVERT(BIGINT, @Old) - @Value ELSE @Value END;
        IF @New < 0 OR @New > 2147483647
            THROW 50103, 'The resulting value must be between 0 and 2147483647.', 1;
        IF @New <> @Old
        BEGIN
            IF @Field = 'Quantity'
            BEGIN
                SET @Delta = CONVERT(INT, @New) - @Old;
                EXEC dbo.sp_Consumable_StockTransaction
                    @ConsumableID = @ConsumableID, @ChangeAmount = @Delta,
                    @Reason = @Reason, @PerformedByUserID = @PerformedByUserID;
            END
            ELSE
                UPDATE dbo.Consumable SET ReorderLevel = CONVERT(INT, @New)
                WHERE ConsumableID = @ConsumableID;
        END
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
-- Editing names/suppliers must never overwrite an inline adjustment.
CREATE OR ALTER PROCEDURE dbo.sp_Consumable_UpdateDetails
    @ConsumableID INT, @SupplierID INT, @Name NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Consumable SET SupplierID = @SupplierID, Name = @Name
    WHERE ConsumableID = @ConsumableID;
END
GO
