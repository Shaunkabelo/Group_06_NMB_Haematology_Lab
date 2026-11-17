-- Preserve supplied employee numbers; retain generated numbers for older callers.
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnician_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255),
    @Cellphone NVARCHAR(20), @HomeAddress NVARCHAR(255),
    @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE,
    @EmployeeNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT dbo.[User]
            (Name, Surname, Email, PasswordHash, PasswordSalt, Cellphone, HomeAddress,
             SA_ID_Number, DateOfBirth, Role, IsActive, MustChangePassword)
        VALUES
            (@Name, @Surname, @Email, @PasswordHash, @PasswordSalt, @Cellphone, @HomeAddress,
             @SA_ID_Number, @DateOfBirth, 'LabTechnician', 1, 1);
        DECLARE @UserID INT = CONVERT(INT, SCOPE_IDENTITY());
        INSERT dbo.LabTechnician(UserID, EmployeeNumber)
        VALUES (@UserID, COALESCE(NULLIF(LTRIM(RTRIM(@EmployeeNumber)), ''), CONCAT('EMP', @UserID)));
        DECLARE @LabTechnicianID INT = CONVERT(INT, SCOPE_IDENTITY());
        COMMIT;
        SELECT @LabTechnicianID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
