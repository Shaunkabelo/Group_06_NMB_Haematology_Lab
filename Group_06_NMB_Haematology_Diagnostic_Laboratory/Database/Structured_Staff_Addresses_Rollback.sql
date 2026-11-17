-- Reverses Structured_Staff_Addresses.sql before structured-address data is used.
-- Run this whole script against the affected database in SSMS.
-- It deliberately stops if any structured addresses exist, so no address data is deleted by accident.
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF DB_ID() <= 4 OR OBJECT_ID('dbo.[User]', 'U') IS NULL
    THROW 51010, 'Select the affected application database before running this rollback.', 1;
IF OBJECT_ID('dbo.UserAddress', 'U') IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.UserAddress)
    THROW 51011, 'Rollback stopped: UserAddress contains data. Export or migrate those addresses before removing the feature.', 1;
BEGIN TRANSACTION;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Doctor_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255), @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE, @HPCSA_Number NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT dbo.[User] (Name,Surname,Email,PasswordHash,PasswordSalt,Cellphone,HomeAddress,SA_ID_Number,DateOfBirth,Role,IsActive,MustChangePassword)
        VALUES (@Name,@Surname,@Email,@PasswordHash,@PasswordSalt,@Cellphone,@HomeAddress,@SA_ID_Number,@DateOfBirth,'Doctor',1,1);
        DECLARE @UserID INT = CONVERT(INT, SCOPE_IDENTITY());
        INSERT dbo.Doctor(UserID,HPCSA_Number) VALUES (@UserID,@HPCSA_Number);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Doctor_Update
    @DoctorID INT, @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255), @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE, @HPCSA_Number NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE u SET Name=@Name,Surname=@Surname,Email=@Email,Cellphone=@Cellphone,HomeAddress=@HomeAddress,
            SA_ID_Number=@SA_ID_Number,DateOfBirth=@DateOfBirth,UpdatedAt=GETUTCDATE()
        FROM dbo.[User] u JOIN dbo.Doctor d ON d.UserID=u.UserID WHERE d.DoctorID=@DoctorID;
        UPDATE dbo.Doctor SET HPCSA_Number=@HPCSA_Number WHERE DoctorID=@DoctorID;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabManager_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255), @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT dbo.[User] (Name,Surname,Email,PasswordHash,PasswordSalt,Cellphone,HomeAddress,SA_ID_Number,DateOfBirth,Role,IsActive,MustChangePassword)
        VALUES (@Name,@Surname,@Email,@PasswordHash,@PasswordSalt,@Cellphone,@HomeAddress,@SA_ID_Number,@DateOfBirth,'LabManager',1,1);
        DECLARE @UserID INT = CONVERT(INT, SCOPE_IDENTITY());
        INSERT dbo.LabManager(UserID) VALUES (@UserID);
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabManager_Update
    @LabManagerID INT, @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255), @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE u SET Name=@Name,Surname=@Surname,Email=@Email,Cellphone=@Cellphone,HomeAddress=@HomeAddress,
            SA_ID_Number=@SA_ID_Number,DateOfBirth=@DateOfBirth,UpdatedAt=GETUTCDATE()
        FROM dbo.[User] u JOIN dbo.LabManager lm ON lm.UserID=u.UserID WHERE lm.LabManagerID=@LabManagerID;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnician_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255), @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE,
    @EmployeeNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT dbo.[User] (Name,Surname,Email,PasswordHash,PasswordSalt,Cellphone,HomeAddress,SA_ID_Number,DateOfBirth,Role,IsActive,MustChangePassword)
        VALUES (@Name,@Surname,@Email,@PasswordHash,@PasswordSalt,@Cellphone,@HomeAddress,@SA_ID_Number,@DateOfBirth,'LabTechnician',1,1);
        DECLARE @UserID INT = CONVERT(INT,SCOPE_IDENTITY());
        INSERT dbo.LabTechnician(UserID,EmployeeNumber)
        VALUES (@UserID,COALESCE(NULLIF(LTRIM(RTRIM(@EmployeeNumber)),N''),CONCAT(N'EMP',@UserID)));
        DECLARE @LabTechnicianID INT = CONVERT(INT,SCOPE_IDENTITY());
        COMMIT;
        SELECT @LabTechnicianID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnician_Update
    @LabTechnicianID INT, @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255), @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE, @EmployeeNumber NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE u SET Name=@Name,Surname=@Surname,Email=@Email,Cellphone=@Cellphone,HomeAddress=@HomeAddress,
            SA_ID_Number=@SA_ID_Number,DateOfBirth=@DateOfBirth,UpdatedAt=GETUTCDATE()
        FROM dbo.[User] u JOIN dbo.LabTechnician lt ON lt.UserID=u.UserID WHERE lt.LabTechnicianID=@LabTechnicianID;
        UPDATE dbo.LabTechnician SET EmployeeNumber=@EmployeeNumber WHERE LabTechnicianID=@LabTechnicianID;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO
DROP PROCEDURE IF EXISTS dbo.sp_Address_GetAllUserAddresses;
DROP PROCEDURE IF EXISTS dbo.sp_Address_GetSuburbsByTownCity;
DROP PROCEDURE IF EXISTS dbo.sp_Address_GetTownCitiesByProvince;
DROP PROCEDURE IF EXISTS dbo.sp_Address_GetProvinces;
DROP TABLE IF EXISTS dbo.UserAddress;
DROP TABLE IF EXISTS dbo.Suburb;
DROP TABLE IF EXISTS dbo.TownCity;
DROP TABLE IF EXISTS dbo.Province;
COMMIT TRANSACTION;
GO
PRINT 'Structured staff address migration was rolled back.';
