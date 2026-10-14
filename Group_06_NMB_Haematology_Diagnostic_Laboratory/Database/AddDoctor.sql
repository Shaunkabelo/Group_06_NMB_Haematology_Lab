-- Select your application database in SSMS before running.
-- Replace the example details and supply a password hash and salt.
-- Generate them together using the application's C# PasswordHelper:
-- var salt = PasswordHelper.GenerateSalt();
-- var hash = PasswordHelper.HashPassword(temporaryPassword, salt);
-- Use a temporaryPassword that passes PasswordHelper.MeetsPolicy().
-- Paste salt and hash below; do not store the plaintext password here.
-- This template assumes identity keys UserID and DoctorID, and the
-- HPCSA_Number column convention used by the project's Doctor model.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Name NVARCHAR(100) = N'Example';
DECLARE @Surname NVARCHAR(100) = N'Doctor';
DECLARE @Email NVARCHAR(150) = N'doctor@example.com';
DECLARE @Cellphone NVARCHAR(20) = N'0821234567';
DECLARE @HomeAddress NVARCHAR(250) = N'Replace with address';
DECLARE @SA_ID_Number NVARCHAR(13) = NULL; -- Supply if required by your database.
DECLARE @DateOfBirth DATE = NULL;
DECLARE @HPCSA_Number NVARCHAR(50) = N'REPLACE_HPCSA_NUMBER';
DECLARE @PasswordHash NVARCHAR(255) = NULL; -- Paste the generated hash.
DECLARE @PasswordSalt NVARCHAR(255) = NULL; -- Paste its matching salt.

IF @PasswordHash IS NULL OR @PasswordSalt IS NULL
    THROW 50001, 'Supply a PasswordHelper-generated password hash and matching salt.', 1;

IF @HPCSA_Number = N'REPLACE_HPCSA_NUMBER'
    THROW 50002, 'Supply the doctor''s HPCSA number.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE Email = @Email)
        THROW 50003, 'A user with this email already exists.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Doctor WITH (UPDLOCK, HOLDLOCK) WHERE HPCSA_Number = @HPCSA_Number)
        THROW 50004, 'A doctor with this HPCSA number already exists.', 1;

    INSERT INTO dbo.[User]
        (Name, Surname, Email, PasswordHash, PasswordSalt, Cellphone,
         HomeAddress, SA_ID_Number, DateOfBirth, [Role], IsActive,
         MustChangePassword, CreatedAt, UpdatedAt)
    VALUES
        (@Name, @Surname, @Email, @PasswordHash, @PasswordSalt, @Cellphone,
         @HomeAddress, @SA_ID_Number, @DateOfBirth, N'Doctor', 1,
         1, GETUTCDATE(), GETUTCDATE());

    DECLARE @UserID INT = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Doctor (UserID, HPCSA_Number)
    VALUES (@UserID, @HPCSA_Number);

    DECLARE @DoctorID INT = CONVERT(INT, SCOPE_IDENTITY());

    COMMIT TRANSACTION;

    SELECT @UserID AS UserID, @DoctorID AS DoctorID, @Email AS Email;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
