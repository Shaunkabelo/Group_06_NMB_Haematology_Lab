CREATE OR ALTER PROCEDURE dbo.sp_UserLogin
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserID, Name, Surname, Email, PasswordHash, PasswordSalt,
        [Role], IsActive, MustChangePassword
    FROM dbo.[User]
    WHERE Email = @Email;
END;
GO
