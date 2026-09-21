-- Additive migration. Select the application's database in SSMS before running.
-- Compatible with both GRP-04-06 and GRP_04_06_db. Never rebuilds existing data.
SET XACT_ABORT ON;
GO
IF OBJECT_ID('dbo.[User]', 'U') IS NULL
    THROW 50001, 'Select the application database containing dbo.User first.', 1;
GO
IF COL_LENGTH('dbo.User', 'SecurityStamp') IS NULL
    ALTER TABLE dbo.[User] ADD SecurityStamp UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_User_SecurityStamp DEFAULT NEWID() WITH VALUES;
GO
IF OBJECT_ID('dbo.PasswordResetToken', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetToken (
        TokenHash CHAR(64) COLLATE Latin1_General_100_BIN2 NOT NULL PRIMARY KEY,
        UserID INT NOT NULL REFERENCES dbo.[User](UserID),
        CreatedAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        UsedAt DATETIME2 NULL
    );
    CREATE INDEX IX_PasswordResetToken_User ON dbo.PasswordResetToken(UserID, CreatedAt);
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetActiveUserByEmail @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.[User] WHERE Email = @Email AND IsActive = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_RecordLogin @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.[User] SET LastLoginDate = SYSUTCDATETIME() WHERE UserID = @UserID AND IsActive = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetState @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserID, SecurityStamp, MustChangePassword, [Role]
    FROM dbo.[User] WHERE UserID = @UserID AND IsActive = 1;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_CreateReset @UserID INT, @TokenHash CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    -- Serialize all password operations for this user.
    DECLARE @ID INT;
    SELECT @ID = UserID FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
    WHERE UserID = @UserID AND IsActive = 1;
    IF @ID IS NULL OR EXISTS (
        SELECT 1 FROM dbo.PasswordResetToken WHERE UserID = @UserID
        AND CreatedAt > DATEADD(MINUTE, -1, SYSUTCDATETIME()))
    BEGIN
        COMMIT;
        SELECT CAST(0 AS BIT);
        RETURN;
    END
    UPDATE dbo.PasswordResetToken SET UsedAt = SYSUTCDATETIME()
    WHERE UserID = @UserID AND UsedAt IS NULL;
    INSERT dbo.PasswordResetToken(TokenHash, UserID, CreatedAt, ExpiresAt)
    VALUES(@TokenHash, @UserID, SYSUTCDATETIME(), DATEADD(MINUTE, 30, SYSUTCDATETIME()));
    COMMIT;
    SELECT CAST(1 AS BIT);
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_ResetPassword
    @TokenHash CHAR(64), @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @UserID INT, @ID INT;
    SELECT @UserID = UserID FROM dbo.PasswordResetToken WHERE TokenHash = @TokenHash;
    BEGIN TRANSACTION;
    SELECT @ID = UserID FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
    WHERE UserID = @UserID AND IsActive = 1;
    IF @ID IS NULL OR NOT EXISTS (
        SELECT 1 FROM dbo.PasswordResetToken WITH (UPDLOCK, HOLDLOCK)
        WHERE TokenHash = @TokenHash AND UsedAt IS NULL AND ExpiresAt > SYSUTCDATETIME())
    BEGIN
        COMMIT;
        SELECT CAST(0 AS BIT);
        RETURN;
    END
    UPDATE dbo.[User] SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt,
        MustChangePassword = 0, SecurityStamp = NEWID(), UpdatedAt = SYSUTCDATETIME()
    WHERE UserID = @UserID;
    UPDATE dbo.PasswordResetToken SET UsedAt = SYSUTCDATETIME()
    WHERE UserID = @UserID AND UsedAt IS NULL;
    COMMIT;
    SELECT CAST(1 AS BIT);
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Auth_ChangePassword
    @UserID INT, @OldHash NVARCHAR(255), @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.[User] WITH (UPDLOCK)
    SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt,
        MustChangePassword = 0, SecurityStamp = NEWID(), UpdatedAt = SYSUTCDATETIME()
    WHERE UserID = @UserID AND IsActive = 1
        AND PasswordHash COLLATE Latin1_General_100_BIN2 = @OldHash COLLATE Latin1_General_100_BIN2;
    IF @@ROWCOUNT = 0
    BEGIN
        COMMIT;
        SELECT CAST(0 AS BIT);
        RETURN;
    END
    UPDATE dbo.PasswordResetToken SET UsedAt = SYSUTCDATETIME()
    WHERE UserID = @UserID AND UsedAt IS NULL;
    COMMIT;
    SELECT CAST(1 AS BIT);
END
GO
