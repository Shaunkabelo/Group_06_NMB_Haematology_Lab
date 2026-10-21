/* Run after Structured_Staff_Addresses.sql / Address_Lookup_Seed.sql. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_UpdateProfileWithAddress
    @UserID INT, @SecurityStamp UNIQUEIDENTIFIER,
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20) = NULL,
    @AddressLine1 NVARCHAR(150), @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@AddressLine1)), N'') IS NULL
        THROW 51001, 'Address line 1 is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID = @SuburbID AND IsActive = 1)
        THROW 51002, 'Select an active suburb.', 1;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.[User]
        SET Name=@Name, Surname=@Surname, Email=@Email, Cellphone=@Cellphone,
            HomeAddress=@AddressLine1, UpdatedAt=SYSUTCDATETIME(), SecurityStamp=NEWID()
        WHERE UserID=@UserID AND IsActive=1 AND SecurityStamp=@SecurityStamp;
        IF @@ROWCOUNT <> 1
        BEGIN
            COMMIT;
            SELECT CAST(0 AS BIT);
            RETURN;
        END
        MERGE dbo.UserAddress AS target
        USING (SELECT @UserID AS UserID) AS source ON target.UserID = source.UserID
        WHEN MATCHED THEN UPDATE SET AddressLine1=@AddressLine1, AddressLine2=NULLIF(LTRIM(RTRIM(@AddressLine2)), N''),
            SuburbID=@SuburbID, UpdatedAt=SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT(UserID, AddressLine1, AddressLine2, SuburbID)
            VALUES(@UserID, @AddressLine1, NULLIF(LTRIM(RTRIM(@AddressLine2)), N''), @SuburbID);
        COMMIT;
        SELECT CAST(1 AS BIT);
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
