USE [GRP_04_06_db]
GO

CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetActiveUserByEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserID,
        Name,
        Surname,
        Email,
        PasswordHash,
        PasswordSalt,
        Cellphone,
        HomeAddress,
        SA_ID_Number,
        DateOfBirth,
        [Role],
        IsActive,
        MustChangePassword,
        CreatedAt,
        UpdatedAt,
        LastLoginDate,
        SecurityStamp
    FROM dbo.[User]
    WHERE Email = @Email
      AND IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Auth_RecordLogin
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.[User]
    SET LastLoginDate = GETUTCDATE(),
        UpdatedAt = GETUTCDATE()
    WHERE UserID = @UserID
      AND IsActive = 1;
END
GO
