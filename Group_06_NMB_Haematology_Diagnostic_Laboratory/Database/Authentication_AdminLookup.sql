-- Optional database procedure for deployments that prefer authentication lookups in SQL.
-- The application also has a parameterized table-query fallback, so this is not
-- required for the Admin login redirect to work.
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID('dbo.[User]', 'U') IS NULL OR OBJECT_ID('dbo.Admin', 'U') IS NULL
    THROW 51000, 'Select the application database containing dbo.[User] and dbo.Admin first.', 1;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetAdminIdByUserId
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.AdminID
    FROM dbo.Admin AS a
    INNER JOIN dbo.[User] AS u
        ON u.UserID = a.UserID
    WHERE a.UserID = @UserID
      AND u.IsActive = 1
      AND u.[Role] = 'Admin';
END;
GO
