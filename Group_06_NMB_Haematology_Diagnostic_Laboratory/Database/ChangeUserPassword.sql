CREATE OR ALTER PROCEDURE dbo.sp_ChangeUserPassword
    @Email NVARCHAR(150),
    @CurrentPasswordHash NVARCHAR(255),
    @CurrentPasswordSalt NVARCHAR(255),
    @NewPassword NVARCHAR(255),
    @ConfirmPassword NVARCHAR(255),
    @NewPasswordHash NVARCHAR(255),
    @NewPasswordSalt NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Validate new password confirmation
    IF @NewPassword IS NULL
       OR @ConfirmPassword IS NULL
       OR @NewPassword <> @ConfirmPassword
    BEGIN
        THROW 50001,
            'New password and confirm password do not match.',
            1;
    END;

    -- Validate generated credentials
    IF NULLIF(@NewPasswordHash, '') IS NULL
       OR NULLIF(@NewPasswordSalt, '') IS NULL
    BEGIN
        THROW 50002,
            'New password hash and salt are required.',
            1;
    END;

    -- Update only if the account and current credentials match
    UPDATE dbo.[User]
    SET
        PasswordHash = @NewPasswordHash,
        PasswordSalt = @NewPasswordSalt,
        MustChangePassword = 0,
        UpdatedAt = GETUTCDATE()
    WHERE Email = @Email
      AND IsActive = 1
      AND PasswordHash = @CurrentPasswordHash
      AND PasswordSalt = @CurrentPasswordSalt;

    IF @@ROWCOUNT <> 1
    BEGIN
        THROW 50003,
            'Account not found or current credentials are incorrect.',
            1;
    END;

    SELECT
        1 AS Success,
        'Password updated successfully.' AS Message;
END;
GO
