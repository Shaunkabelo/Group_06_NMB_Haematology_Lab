-- Apply after Authentication_PasswordRecovery.sql in the application's database.
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserById @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.[User] WHERE UserID = @UserID AND IsActive = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_UpdateProfile
    @UserID INT, @SecurityStamp UNIQUEIDENTIFIER,
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @Cellphone NVARCHAR(20) = NULL, @HomeAddress NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.[User]
        SET Name = @Name, Surname = @Surname, Email = @Email,
            Cellphone = @Cellphone, HomeAddress = @HomeAddress,
            UpdatedAt = SYSUTCDATETIME(), SecurityStamp = NEWID()
        WHERE UserID = @UserID AND IsActive = 1 AND SecurityStamp = @SecurityStamp;
        IF @@ROWCOUNT <> 1
        BEGIN
            COMMIT;
            SELECT CAST(0 AS BIT);
            RETURN;
        END
        UPDATE dbo.PasswordResetToken SET UsedAt = SYSUTCDATETIME()
        WHERE UserID = @UserID AND UsedAt IS NULL;
        COMMIT;
        SELECT CAST(1 AS BIT);
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
