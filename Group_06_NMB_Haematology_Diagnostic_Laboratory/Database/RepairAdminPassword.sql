-- Run against the application's database. Repairs placeholder credentials only.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.[User]
    SET PasswordHash = N'uMla/YTs+WrPbi6xyv7bppYOQo69lgEgq5NeTkpOKxawQNdNX8WyhYooGXqmdBBs+HtVjAR55a+I47YieyFgSg==',
        PasswordSalt = N'fdMgsl8vzYqcOg+UaYdzQkoF0LINylmmIcEcbzpdeoRgQEnhxYhf4Ms45eY/Nk6gLIzEcsQZ2YkMk9A57IfnquPPe0CIqJ3iQfo+Km1bxppJemDLf7mHazw2gvxFd0fUOz8JOamzIWvgevtC4miBDuZn4oDutYrmgP6Fyk3FgKg=',
        UpdatedAt = GETUTCDATE()
    WHERE Email = N'sabelo@gmail.com'
      AND Role = N'Admin'
      AND IsActive = 1
      AND PasswordHash = N'PUT_GENERATED_PASSWORD_HASH_HERE'
      AND PasswordSalt = N'PUT_GENERATED_PASSWORD_SALT_HERE'
      AND EXISTS (SELECT 1 FROM dbo.Admin A WHERE A.UserID = dbo.[User].UserID);

    IF @@ROWCOUNT <> 1
        THROW 50001, 'Expected one active admin with placeholder credentials. No changes were saved.', 1;

    COMMIT TRANSACTION;
    PRINT 'Admin password repaired. Sign in using the supplied password.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
